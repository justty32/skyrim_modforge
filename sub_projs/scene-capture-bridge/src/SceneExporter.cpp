#include "SceneExporter.h"

#include "Eraser.h"
#include "Markers.h"
#include "Overrides.h"

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

    namespace {
        Stats g_last;
    }

    const Stats& LastExport() { return g_last; }

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
        // Light plugins (ESL/ESPFE) are 0xFExxxYYY -> 12-bit local id; full
        // plugins are 0xXXyyyyyy -> 24-bit. Verified offline against
        // ccBGSSSE037-Curios.esl, whose local ids top out at 0x88E (all < 0x1000),
        // and Skyrim.esm's 0x01605E (24-bit). Both round-trip through the
        // "{}:0x{:06X}" form ModForge expects.
        const std::uint32_t localId =
            file->IsLight() ? (rawId & 0x00000FFFu) : (rawId & 0x00FFFFFFu);

        return std::format("{}:0x{:06X}", file->fileName, localId);
    }

    Anchor AnchorOf(RE::TESObjectREFR* ref) {
        Anchor a;
        if (!ref) ref = RE::PlayerCharacter::GetSingleton();
        RE::TESObjectCELL* cell = ref ? ref->GetParentCell() : nullptr;
        if (!cell) return a;
        if (cell->IsInteriorCell()) {
            a.interior = true;
            if (auto id = ResolveDurableId(cell)) a.id = *id;
        } else if (auto* ws = cell->GetRuntimeData().worldSpace) {
            if (auto id = ResolveDurableId(ws)) a.id = *id;
        }
        return a;
    }

    namespace {
        // Running tallies while sweeping one or more cells for placements.
        struct PlacementCounters {
            std::size_t actors = 0;
            std::size_t preexisting = 0;
            std::size_t skipped = 0;
            std::size_t markerProxies = 0;
            std::size_t removalsPending = 0;   // in swept cells (log only)
            std::size_t overridesPending = 0;  // in swept cells (log only)
        };
    }

    // Sweep ONE cell's placed refs (the vanilla diff) and append the
    // player-added ones to scene["placements"]. No registry/global segments —
    // those are emitted once by AppendRegistries so export-all doesn't repeat
    // them per cell.
    static void AppendPlacements(RE::TESObjectCELL* cell, nlohmann::json& scene,
        PlacementCounters& counters) {
        if (!cell) return;
        const bool isInterior = cell->IsInteriorCell();

        // Cell / worldspace attribution (§契約 coordinate contract):
        //  - interior: `cell` = the cell's durable id, positions are cell-local.
        //  - exterior: `worldspace` = the worldspace's durable id, positions
        //    are world-space (ModForge finds the right sub-cell to override).
        //
        // These live on EACH PlacementSpec, not at the top level — ModSpec has
        // no top-level `cell`/`worldspace`, and a placement carrying neither is
        // dropped with "cell '' not found in spec — skipped"
        // (Generator.Build.Placements.cs:48).
        std::string cellId;
        std::string worldspaceId;
        if (isInterior) {
            if (auto id = ResolveDurableId(cell)) {
                cellId = *id;
            }
        } else if (auto* ws = cell->GetRuntimeData().worldSpace) {
            if (auto id = ResolveDurableId(ws)) {
                worldspaceId = *id;
            }
        }
        if (cellId.empty() && worldspaceId.empty()) {
            SKSE::log::warn(
                "AppendPlacements: cell/worldspace unresolved — placements here "
                "would be dropped by build; skipping this cell");
            return;
        }

        // ForEachReference hands the callback a POINTER, not a reference.
        cell->ForEachReference([&](RE::TESObjectREFR* refPtr) -> RE::BSContainer::ForEachResult {
            if (!refPtr) {
                return RE::BSContainer::ForEachResult::kContinue;
            }
            RE::TESObjectREFR& ref = *refPtr;

            RE::TESBoundObject* base = ref.GetBaseObject();
            if (!base || ref.IsDeleted()) {
                return RE::BSContainer::ForEachResult::kContinue;
            }
            // Skip the player and refs whose base cannot be durably referenced.
            if (ref.IsPlayerRef()) {
                return RE::BSContainer::ForEachResult::kContinue;
            }
            // Marker proxies are editor chrome, not content — without this they
            // are dynamic refs and the vanilla diff would export them as
            // player-placed objects.
            if (Markers::IsProxy(refPtr)) {
                ++counters.markerProxies;
                return RE::BSContainer::ForEachResult::kContinue;
            }

            // The vanilla diff. A cell sweep sees EVERY reference in it, so
            // exporting all of them would make ModForge re-place the whole
            // vanilla room on top of itself (Bannered Mare: 662 refs, every
            // chair doubled). What we want is only what the player ADDED.
            //
            // The discriminator is free: a ref authored in some plugin resolves
            // to a durable id, while a ref spawned at runtime (PlaceAtMe) lives
            // in the dynamic 0xFF...... range and has no source file. So an
            // authored ref is pre-existing; an unresolvable one is player-placed.
            //
            // LIMITATION: a vanilla ref the player MOVED/SCALED is skipped here.
            // Capturing that means emitting an override of the existing ref, not
            // a new placement — a different scene.json shape the contract does
            // not model yet (only `removals[]` touches existing refs). Deliberate
            // MVP cut, not an oversight.
            if (auto refId = ResolveDurableId(&ref)) {
                // A ref marked by the eraser is not "pre-existing kept as-is" —
                // it exports through removals[], counted separately. Same for a
                // ref moved through the editor: it exports through overrides[].
                if (Eraser::MarkedIds().contains(*refId)) ++counters.removalsPending;
                else if (Overrides::Contains(*refId)) ++counters.overridesPending;
                else ++counters.preexisting;
                return RE::BSContainer::ForEachResult::kContinue;
            }
            // A disabled dynamic ref is one of our own placements the player
            // erased — true deletion semantics: it leaves no trace.
            if (ref.IsDisabled()) {
                return RE::BSContainer::ForEachResult::kContinue;
            }

            auto baseId = ResolveDurableId(base);
            if (!baseId) {
                ++counters.skipped;  // dynamic / runtime-only base — not esp-referenceable
                return RE::BSContainer::ForEachResult::kContinue;
            }

            // Authored transform (data.location/angle), not live physics pose.
            // Angle is radians in-engine; contract wants degrees.
            const RE::NiPoint3& pos = ref.data.location;
            const RE::NiPoint3& ang = ref.data.angle;
            nlohmann::json entry;
            entry["base"] = *baseId;
            if (!cellId.empty()) {
                entry["cell"] = cellId;
            } else {
                entry["worldspace"] = worldspaceId;
            }
            entry["position"] = Vec3(pos);
            entry["rotation"] = nlohmann::json{
                {"x", ang.x * kRadToDeg},
                {"y", ang.y * kRadToDeg},
                {"z", ang.z * kRadToDeg},
            };

            // Actors and objects both land in `placements[]` — ModSpec has one
            // PlacementSpec list and no `npcRefs` member (an `npcRefs` key would
            // be silently dropped). An actor base makes ModForge emit an ACHR.
            // The §D role/backstory tagging is a separate `npcRoles[]` authored
            // by the editor UI, not by a raw sweep.
            const bool isActor = ref.GetFormType() == RE::FormType::ActorCharacter;
            if (isActor) {
                ++counters.actors;  // XSCL is ignored on actors, so emit no scale field.
                // ModForge's isNpc auto-detect only covers in-spec bases; an
                // external NPC base without explicit kind builds a REFR that
                // silently spawns nothing. Stamp it at the source.
                entry["kind"] = "npc";
                scene["placements"].push_back(std::move(entry));
            } else {
                // Carry scale + the InitiallyDisabled state so ModForge
                // reproduces both.
                entry["scale"] = ref.GetScale();
                if ((ref.GetFormFlags() & kInitiallyDisabled) != 0) {
                    entry["initiallyDisabled"] = true;
                }
                scene["placements"].push_back(std::move(entry));
            }
            return RE::BSContainer::ForEachResult::kContinue;
        });
    }

    // Append the three cell-independent registry segments ONCE. removals[],
    // overrides[] and annotations[] each span every cell (their registries do),
    // so ModForge resolves them via the master link cache regardless of which
    // cell was swept — exporting the player's cell already carries the lot.
    static void AppendRegistries(nlohmann::json& scene) {
        if (const auto& marked = Eraser::All(); !marked.empty()) {
            auto arr = nlohmann::json::array();
            for (const auto& e : marked) arr.push_back(e.id);
            scene["removals"] = std::move(arr);
        }

        if (const auto& moved = Overrides::All(); !moved.empty()) {
            auto arr = nlohmann::json::array();
            for (const auto& e : moved) {
                RE::NiPoint3 pos = e.pos, ang = e.angle;
                float scale = e.scale;
                if (auto live = e.handle.get()) {  // prefer the settled live pose
                    pos = live->GetPosition();
                    ang = live->data.angle;
                    scale = live->GetScale();
                }
                nlohmann::json o;
                o["ref"] = e.id;
                o["position"] = Vec3(pos);
                o["rotation"] = nlohmann::json{
                    {"x", ang.x * kRadToDeg}, {"y", ang.y * kRadToDeg}, {"z", ang.z * kRadToDeg},
                };
                if (!e.isActor) o["scale"] = scale;
                arr.push_back(std::move(o));
            }
            scene["overrides"] = std::move(arr);
        }

        if (const auto& marks = Markers::All(); !marks.empty()) {
            auto arr = nlohmann::json::array();
            for (const auto& m : marks) {
                nlohmann::json a;
                a["seq"] = m.seq;
                a["label"] = m.label;
                a["kind"] = m.kind;
                a["position"] = Vec3(m.position);
                a["angleZ"] = m.angleDeg.z;  // back-compat (== rotation.z)
                a["rotation"] = nlohmann::json{
                    {"x", m.angleDeg.x}, {"y", m.angleDeg.y}, {"z", m.angleDeg.z}};
                a["scale"] = m.scale;
                if (!m.note.empty()) a["note"] = m.note;  // free-form agent brief
                if (!m.cellOrWs.empty()) a[m.isInterior ? "cell" : "worldspace"] = m.cellOrWs;
                arr.push_back(std::move(a));
            }
            scene["annotations"] = std::move(arr);
        }
    }

    static void RecordStats(const nlohmann::json& scene, const PlacementCounters& c,
        const std::string& cellLabel) {
        g_last.valid = true;
        g_last.placements = scene["placements"].size();
        g_last.actors = c.actors;
        g_last.preexisting = c.preexisting;
        g_last.skipped = c.skipped;
        g_last.cell = cellLabel;
        g_last.markers = c.markerProxies;
        g_last.removals = Eraser::All().size();
        g_last.overrides = Overrides::All().size();
        SKSE::log::info(
            "Export[{}]: {} placements ({} actors), {} pre-existing, {} skipped "
            "(dynamic bases), {} marker proxies excluded, {} annotations, {} "
            "removals, {} overrides", cellLabel, scene["placements"].size(),
            c.actors, c.preexisting, c.skipped, c.markerProxies,
            Markers::All().size(), Eraser::All().size(), Overrides::All().size());
    }

    nlohmann::json ExportCell(RE::TESObjectCELL* cell) {
        // scene.json IS a ModSpec (see workflows/plans/ingame-scene-export.md):
        // `build scene.json out.esp` deserializes it straight into ModSpec, and
        // ModForge's reader SILENTLY IGNORES unknown keys, so every key must be
        // a real ModSpec member.
        nlohmann::json scene;
        scene["placements"] = nlohmann::json::array();
        if (!cell) {
            SKSE::log::warn("ExportCell: null cell, nothing to export");
            return scene;
        }
        PlacementCounters c;
        AppendPlacements(cell, scene, c);
        AppendRegistries(scene);
        std::string label;
        if (cell->IsInteriorCell()) {
            if (auto id = ResolveDurableId(cell)) label = *id;
        } else if (auto* ws = cell->GetRuntimeData().worldSpace) {
            if (auto wid = ResolveDurableId(ws)) label = *wid;
        }
        RecordStats(scene, c, label);
        return scene;
    }

    nlohmann::json ExportAll() {
        // Sweep every LOADED cell for placements (interior = just this one;
        // exterior = the whole streamed grid), then the global registries once.
        // Objects placed in cells that have since unloaded can't be recovered —
        // logged so "export all" never silently under-reports.
        nlohmann::json scene;
        scene["placements"] = nlohmann::json::array();
        PlacementCounters c;
        std::size_t cells = 0;
        if (auto* tes = RE::TES::GetSingleton()) {
            tes->ForEachCell([&](RE::TESObjectCELL* cell) {
                if (cell && cell->IsAttached()) { AppendPlacements(cell, scene, c); ++cells; }
            });
        }
        AppendRegistries(scene);
        RecordStats(scene, c, std::format("ALL/{} loaded cells", cells));
        SKSE::log::info("ExportAll: swept {} loaded cell(s) — placements in "
            "unloaded cells are not captured (visit them, or export per-cell)", cells);
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
        const auto out = *dir / "scene-export.json";
        if (WriteSceneFile(scene, out)) {
            g_last.path = out.string();
        }
    }

    void ExportAllToFile() {
        auto scene = ExportAll();
        auto dir = SKSE::log::log_directory();
        if (!dir) {
            SKSE::log::error("ExportAllToFile: no log_directory");
            return;
        }
        const auto out = *dir / "scene-export.json";
        if (WriteSceneFile(scene, out)) {
            g_last.path = out.string();
        }
    }

}  // namespace SceneExporter
