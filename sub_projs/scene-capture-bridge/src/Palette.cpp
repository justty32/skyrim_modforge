#include "Palette.h"

#include "Aim.h"
#include "Markers.h"
#include "SceneExporter.h"
#include "log.h"

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
}

namespace Palette {

    bool PickCrosshair() {
        auto* pick = RE::CrosshairPickData::GetSingleton();
        // NG layout: per-VR-device arrays; flat runtime reads device 0.
        RE::NiPointer<RE::TESObjectREFR> ref = pick ? pick->target[0].get() : nullptr;
        if (!ref) {
            SKSE::log::info("Palette: crosshair has no target");
            return false;
        }
        if (Markers::IsProxy(ref.get())) {
            SKSE::log::info("Palette: crosshair is a marker proxy — nothing to pick");
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

        Slot s;
        s.baseId = *baseId;
        s.base = base;
        s.angle = ref->data.angle;      // captured pose, radians
        s.scale = ref->GetScale();
        s.isActor = ref->GetFormType() == RE::FormType::ActorCharacter;
        s.addsMaster = AddsMaster(*baseId);
        const char* dn = ref->GetDisplayFullName();
        s.name = (dn && *dn) ? dn : *baseId;

        SKSE::log::info("Palette: picked '{}' ({}) rotZ={:.1f} scale={:.2f}{}",
            s.name, s.baseId, s.angle.z * kRadToDeg, s.scale,
            s.addsMaster ? " (adds a master!)" : "");
        g_slots.push_back(std::move(s));
        g_selected = g_slots.size() - 1;
        return true;
    }

    bool PlaceSelected() {
        if (g_selected >= g_slots.size()) {
            SKSE::log::info("Palette: no slot selected — pick something first (F6)");
            return false;
        }
        auto* player = RE::PlayerCharacter::GetSingleton();
        const auto& s = g_slots[g_selected];
        if (!player || !s.base) return false;

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

    std::vector<Slot>& All() { return g_slots; }
    std::size_t SelectedIndex() { return g_selected; }
    void Select(std::size_t index) { if (index < g_slots.size()) g_selected = index; }

    void Rename(std::size_t index, const std::string& name) {
        if (index < g_slots.size() && !name.empty()) g_slots[index].name = name;
    }

    void Remove(std::size_t index) {
        if (index >= g_slots.size()) return;
        g_slots.erase(g_slots.begin() + static_cast<std::ptrdiff_t>(index));
        if (g_selected >= g_slots.size() && g_selected > 0) g_selected = g_slots.size() - 1;
    }

}  // namespace Palette
