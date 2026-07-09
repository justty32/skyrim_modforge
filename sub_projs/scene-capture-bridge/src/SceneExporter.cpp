#include "SceneExporter.h"

#include "log.h"

#include <fstream>

namespace {
    constexpr float kRadToDeg = 57.2957795f;

    // InitiallyDisabled record flag (bit 0x800) — a ref authored to spawn
    // disabled. We export this so ModForge round-trips the enable state.
    constexpr std::uint32_t kInitiallyDisabled = 0x00000800u;

    // Emit {x,y,z} as a json object matching PlacementSpec.Position/Rotation.
    nlohmann::json Vec3(const RE::NiPoint3& v) {
        return nlohmann::json{{"x", v.x}, {"y", v.y}, {"z", v.z}};
    }
}

namespace SceneExporter {

    std::optional<std::string> ResolveDurableId(const RE::TESForm* form) {
        if (!form) {
            return std::nullopt;
        }
        // GetFile(0) = the file that first DEFINES this form (origin master),
        // not the last override — that is the reference ModForge needs as a
        // master. Runtime-only forms (PlaceAtMe dynamic refs) have no file.
        const RE::TESFile* file = form->GetFile(0);
        if (!file) {
            return std::nullopt;
        }

        const std::uint32_t rawId = form->GetFormID();
        // TODO(runtime-verify): confirm light-plugin (ESL, 0xFExxxYYY) local-id
        // width is 12 bits here and full-plugin is 24 bits. IsLight() should key
        // off TESFile kSmallFile; verify against a known CC/ESL ref in-game.
        const std::uint32_t localId =
            file->IsLight() ? (rawId & 0x00000FFFu) : (rawId & 0x00FFFFFFu);

        return std::format("{}:0x{:06X}", file->fileName, localId);
    }

    nlohmann::json ExportCell(RE::TESObjectCELL* cell) {
        nlohmann::json scene;
        scene["placements"] = nlohmann::json::array();
        scene["npcRefs"] = nlohmann::json::array();

        if (!cell) {
            SKSE::log::warn("ExportCell: null cell, nothing to export");
            return scene;
        }

        const bool isInterior = cell->IsInteriorCell();

        // Cell / worldspace header (§契約 coordinate contract):
        //  - interior: `cell` = the cell's durable id, positions are cell-local.
        //  - exterior: `worldspace` = the worldspace's durable id, positions
        //    are world-space (ModForge finds the right sub-cell to override).
        if (isInterior) {
            if (auto id = ResolveDurableId(cell)) {
                scene["cell"] = *id;
            }
        } else if (auto* ws = cell->GetRuntimeData().worldSpace) {
            if (auto id = ResolveDurableId(ws)) {
                scene["worldspace"] = *id;
            }
        }

        std::size_t skipped = 0;
        cell->ForEachReference([&](RE::TESObjectREFR& ref) -> RE::BSContainer::ForEachResult {
            RE::TESBoundObject* base = ref.GetBaseObject();
            if (!base || ref.IsDeleted()) {
                return RE::BSContainer::ForEachResult::kContinue;
            }
            // Skip the player and refs whose base cannot be durably referenced.
            if (ref.IsPlayerRef()) {
                return RE::BSContainer::ForEachResult::kContinue;
            }
            auto baseId = ResolveDurableId(base);
            if (!baseId) {
                ++skipped;  // dynamic / runtime-only base — not esp-referenceable
                return RE::BSContainer::ForEachResult::kContinue;
            }

            // Authored transform (data.location/angle), not live physics pose.
            // Angle is radians in-engine; contract wants degrees.
            const RE::NiPoint3& pos = ref.data.location;
            const RE::NiPoint3& ang = ref.data.angle;
            nlohmann::json entry;
            entry["base"] = *baseId;
            entry["position"] = Vec3(pos);
            entry["rotation"] = nlohmann::json{
                {"x", ang.x * kRadToDeg},
                {"y", ang.y * kRadToDeg},
                {"z", ang.z * kRadToDeg},
            };

            const bool isActor = ref.GetFormType() == RE::FormType::ActorCharacter;
            if (isActor) {
                // §契約 npcRefs[]: a placed actor (e.g. PROTEUS clone). XSCL does
                // not apply to actors, so no scale field. role/backstory are
                // layered in by the editor UI (§D), not by a raw sweep.
                scene["npcRefs"].push_back(std::move(entry));
            } else {
                // §契約 placements[]: static/furniture/light. Carry scale + the
                // InitiallyDisabled state so ModForge reproduces both.
                entry["scale"] = ref.GetScale();
                if ((ref.GetFormFlags() & kInitiallyDisabled) != 0) {
                    entry["initiallyDisabled"] = true;
                }
                scene["placements"].push_back(std::move(entry));
            }
            return RE::BSContainer::ForEachResult::kContinue;
        });

        SKSE::log::info(
            "ExportCell: {} placements, {} npcRefs, {} skipped (dynamic bases)",
            scene["placements"].size(), scene["npcRefs"].size(), skipped);
        return scene;
    }

    nlohmann::json ExportPlayerCell() {
        auto* player = RE::PlayerCharacter::GetSingleton();
        RE::TESObjectCELL* cell = player ? player->GetParentCell() : nullptr;
        return ExportCell(cell);
    }

    bool WriteSceneFile(const nlohmann::json& scene, const std::filesystem::path& path) {
        try {
            std::error_code ec;
            std::filesystem::create_directories(path.parent_path(), ec);
            std::ofstream out(path, std::ios::trunc);
            if (!out) {
                SKSE::log::error("WriteSceneFile: cannot open {}", path.string());
                return false;
            }
            out << scene.dump(2);
            SKSE::log::info("WriteSceneFile: wrote {}", path.string());
            return true;
        } catch (const std::exception& e) {
            SKSE::log::error("WriteSceneFile: {}", e.what());
            return false;
        }
    }

    void ExportPlayerCellToFile() {
        auto scene = ExportPlayerCell();
        auto dir = SKSE::log::log_directory();
        if (!dir) {
            SKSE::log::error("ExportPlayerCellToFile: no log_directory");
            return;
        }
        WriteSceneFile(scene, *dir / "scene-export.json");
    }

}  // namespace SceneExporter
