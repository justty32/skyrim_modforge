#include "Palette.h"

#include "Aim.h"
#include "Markers.h"
#include "SceneExporter.h"
#include "log.h"

#include <fstream>

namespace {
    constexpr float kRadToDeg = 57.2957795f;

    std::vector<Palette::Slot> g_slots;
    std::size_t g_selected = 0;

    bool AddsMaster(const std::string& id) {
        static const char* kBase[] = {
            "Skyrim.esm", "Update.esm", "Dawnguard.esm",
            "HearthFires.esm", "Dragonborn.esm",
        };
        for (const auto* b : kBase)
            if (id.starts_with(b)) return false;
        return true;
    }

    std::filesystem::path StorePath() {
        auto dir = SKSE::log::log_directory();
        return dir ? (*dir / "scene-capture-palette.json") : std::filesystem::path{};
    }

    // "<plugin>:0xHEX" -> live form. The stored id is already the LOCAL id
    // (ESL 12-bit / full 24-bit — ResolveDurableId's output), which is exactly
    // what TESDataHandler::LookupForm composes against the current load order.
    RE::TESBoundObject* ResolveBase(const std::string& id) {
        const auto colon = id.rfind(':');
        if (colon == std::string::npos) return nullptr;
        const std::string plugin = id.substr(0, colon);
        std::uint32_t local = 0;
        try {
            local = static_cast<std::uint32_t>(std::stoul(id.substr(colon + 1), nullptr, 16));
        } catch (...) { return nullptr; }
        auto* dh = RE::TESDataHandler::GetSingleton();
        RE::TESForm* form = dh ? dh->LookupForm(local, plugin) : nullptr;
        return form ? form->As<RE::TESBoundObject>() : nullptr;
    }

    // The file lists slots in PANEL order — newest (top of the list) first —
    // so a palette json reads like what the panel shows. The vector keeps the
    // opposite order (index 0 = oldest = bottom), hence the reverse walk here
    // and the reverse insert in Adopt().
    nlohmann::json SlotsJson() {
        nlohmann::json j = nlohmann::json::array();
        for (auto it = g_slots.rbegin(); it != g_slots.rend(); ++it) {
            const auto& s = *it;
            j.push_back({
                {"name", s.name}, {"base", s.baseId},
                {"angle", {{"x", s.angle.x}, {"y", s.angle.y}, {"z", s.angle.z}}},
                {"scale", s.scale}, {"isActor", s.isActor},
            });
        }
        return j;
    }

    // Read a palette json into slots, in FILE order (= panel order, top first).
    // Bases are re-resolved against the current load order; a slot whose plugin
    // is gone stays listed but unavailable (base == nullptr).
    std::vector<Palette::Slot> ParseSlots(const std::filesystem::path& path) {
        std::vector<Palette::Slot> out;
        std::ifstream in(path);
        nlohmann::json j;
        try { in >> j; } catch (const std::exception& e) {
            SKSE::log::warn("Palette: {} unreadable ({})", path.string(), e.what());
            return out;
        }
        if (!j.is_array()) {
            SKSE::log::warn("Palette: {} is not a slot array", path.string());
            return out;
        }
        for (const auto& item : j) {
            Palette::Slot s;
            s.name = item.value("name", "");
            s.baseId = item.value("base", "");
            if (s.baseId.empty()) continue;
            if (auto a = item.find("angle"); a != item.end())
                s.angle = {a->value("x", 0.f), a->value("y", 0.f), a->value("z", 0.f)};
            s.scale = item.value("scale", 1.f);
            s.isActor = item.value("isActor", false);
            s.addsMaster = AddsMaster(s.baseId);
            s.base = ResolveBase(s.baseId);  // null when the plugin isn't loaded
            out.push_back(std::move(s));
        }
        return out;
    }

    // Push parsed (file/panel-order) slots onto the vector so they land ON TOP
    // of whatever is already there, in the file's own order — the panel's
    // newest-first convention (same as a fresh pick).
    void Adopt(std::vector<Palette::Slot>& parsed) {
        for (auto it = parsed.rbegin(); it != parsed.rend(); ++it)
            g_slots.push_back(std::move(*it));
        g_selected = g_slots.empty() ? 0 : g_slots.size() - 1;
    }

    std::size_t Unavailable() {
        std::size_t n = 0;
        for (const auto& s : g_slots) if (!s.base) ++n;
        return n;
    }

    void Save() {
        const auto path = StorePath();
        if (path.empty()) return;
        std::ofstream out(path, std::ios::trunc);
        if (out) out << SlotsJson().dump(2);
    }

    // Refactored core: both pick entries land here. Rejections are loud —
    // a slot that can never build is worse than no slot.
    bool PickRef(RE::NiPointer<RE::TESObjectREFR> ref, const char* how) {
        if (!ref) {
            SKSE::log::info("Palette: {} has no target", how);
            return false;
        }
        if (Markers::IsProxy(ref.get())) {
            SKSE::log::info("Palette: {} target is a marker proxy — nothing to pick", how);
            return false;
        }
        RE::TESBoundObject* base = ref->GetBaseObject();
        if (!base) return false;
        auto baseId = SceneExporter::ResolveDurableId(base);
        if (!baseId) {
            // A runtime-only base cannot be named in an esp — placing copies of
            // it would export placements that never build. Refuse loudly.
            SKSE::log::warn("Palette: target's base is runtime-only (no durable id) — not pickable");
            return false;
        }

        Palette::Slot s;
        s.baseId = *baseId;
        s.base = base;
        s.angle = ref->data.angle;      // captured pose, radians
        s.scale = ref->GetScale();
        s.isActor = ref->GetFormType() == RE::FormType::ActorCharacter;
        s.addsMaster = AddsMaster(*baseId);
        const char* dn = ref->GetDisplayFullName();
        s.name = (dn && *dn) ? dn : *baseId;

        SKSE::log::info("Palette: picked '{}' ({}, {}) rotZ={:.1f} scale={:.2f}{}",
            s.name, s.baseId, how, s.angle.z * kRadToDeg, s.scale,
            s.addsMaster ? " (adds a master!)" : "");
        g_slots.push_back(std::move(s));
        g_selected = g_slots.size() - 1;
        Save();
        return true;
    }
}

namespace Palette {

    bool PickCrosshair() { return PickRef(Aim::CrosshairRef(), "crosshair"); }
    bool PickByRay() { return PickRef(Aim::RayRef(), "ray"); }

    bool PlaceSelected() {
        if (g_selected >= g_slots.size()) {
            SKSE::log::info("Palette: no slot selected — pick something first (F6)");
            return false;
        }
        auto* player = RE::PlayerCharacter::GetSingleton();
        const auto& s = g_slots[g_selected];
        if (!player) return false;
        if (!s.base) {
            SKSE::log::warn("Palette: '{}' ({}) is unavailable — its plugin is "
                "not in the load order", s.name, s.baseId);
            return false;
        }

        RE::NiPoint3 pos;
        const bool aimed = Aim::LookHit(pos);
        if (!aimed) pos = player->GetPosition();

        RE::NiPointer<RE::TESObjectREFR> placed = player->PlaceObjectAtMe(s.base, false);
        if (!placed) {
            SKSE::log::error("Palette: PlaceObjectAtMe failed for {}", s.baseId);
            return false;
        }
        placed->SetPosition(pos);
        placed->SetAngle(s.angle);   // re-apply the captured pose
        if (!s.isActor && s.scale != 1.f) placed->SetScale(s.scale);

        SKSE::log::info("Palette: placed '{}' ({}) at ({:.1f}, {:.1f}, {:.1f})",
            s.name, aimed ? "aimed" : "feet", pos.x, pos.y, pos.z);
        return true;   // a plain dynamic ref — the vanilla diff exports it
    }

    void Load() {
        const auto path = StorePath();
        if (path.empty() || !std::filesystem::exists(path)) return;
        auto parsed = ParseSlots(path);
        g_slots.clear();
        Adopt(parsed);
        const auto unavailable = Unavailable();
        SKSE::log::info("Palette: loaded {} slot(s) from disk{}", g_slots.size(),
            unavailable ? std::format(" ({} unavailable)", unavailable) : "");
    }

    std::size_t LoadFromFile(const std::string& filename) {
        auto dir = SKSE::log::log_directory();
        if (!dir || filename.empty()) return 0;
        const auto path = *dir / filename;
        if (!std::filesystem::exists(path)) {
            SKSE::log::warn("Palette: load-from-file '{}' not found", path.string());
            return 0;
        }
        auto parsed = ParseSlots(path);
        const std::size_t added = parsed.size();
        if (!added) {
            SKSE::log::warn("Palette: '{}' has no usable slot", filename);
            return 0;
        }
        Adopt(parsed);   // appended ON TOP, in the file's order
        Save();          // fold the import into the persistent store
        SKSE::log::info("Palette: appended {} slot(s) from '{}' on top ({} total)",
            added, filename, g_slots.size());
        return added;
    }

    std::size_t ReplaceFromFile(const std::string& filename) {
        auto dir = SKSE::log::log_directory();
        if (!dir || filename.empty()) return 0;
        const auto path = *dir / filename;
        if (!std::filesystem::exists(path)) {
            SKSE::log::warn("Palette: replace-from-file '{}' not found — palette untouched",
                path.string());
            return 0;
        }
        auto parsed = ParseSlots(path);
        if (parsed.empty()) {
            // Never wipe on a bad read: an unreadable or slot-less file would
            // silently destroy the whole (disk-persisted) palette.
            SKSE::log::warn("Palette: '{}' has no usable slot — palette untouched", filename);
            return 0;
        }
        const std::size_t dropped = g_slots.size();
        g_slots.clear();
        Adopt(parsed);
        Save();
        SKSE::log::info("Palette: replaced {} slot(s) with {} from '{}'",
            dropped, g_slots.size(), filename);
        return g_slots.size();
    }

    bool SaveToFile(const std::string& filename) {
        auto dir = SKSE::log::log_directory();
        if (!dir || filename.empty()) return false;
        std::ofstream out(*dir / filename, std::ios::trunc);
        if (!out) {
            SKSE::log::warn("Palette: cannot write '{}'", filename);
            return false;
        }
        out << SlotsJson().dump(2);
        SKSE::log::info("Palette: saved {} slot(s) to '{}'", g_slots.size(), filename);
        return true;
    }

    std::vector<Slot>& All() { return g_slots; }
    std::size_t SelectedIndex() { return g_selected; }
    void Select(std::size_t index) { if (index < g_slots.size()) g_selected = index; }

    void Rename(std::size_t index, const std::string& name) {
        if (index < g_slots.size() && !name.empty()) {
            g_slots[index].name = name;
            Save();
        }
    }

    void Remove(std::size_t index) {
        if (index >= g_slots.size()) return;
        g_slots.erase(g_slots.begin() + static_cast<std::ptrdiff_t>(index));
        if (g_selected >= g_slots.size() && g_selected > 0) g_selected = g_slots.size() - 1;
        Save();
    }

}  // namespace Palette
