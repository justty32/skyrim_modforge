#pragma once

// Palette — the eyedropper (Idea #24 §E ①, plan P2 新增).
//
// Pick: capture the crosshair target's BASE + current rotation + scale into a
// named slot (the open palette — grab anything from any mod, no design-time
// catalogue). Place: spawn the selected slot's base at the aimed point and
// re-apply the captured pose. The spawned thing is an ordinary dynamic ref, so
// the vanilla diff exports it into `placements[]` unchanged — contract zero.

#include <cstdint>
#include <string>
#include <vector>

namespace Palette {

    struct Slot {
        std::string name;
        std::string baseId;               // durable "<plugin>:0x…" (display + master warning)
        RE::TESBoundObject* base = nullptr;  // session pointer; forms outlive the session
        RE::NiPoint3 angle;               // captured pose, radians
        float scale = 1.f;
        bool isActor = false;
        bool addsMaster = false;          // base not from the 5 base-game masters
    };

    bool PickCrosshair();   // new slot from the crosshair target; selects it
    bool PlaceSelected();   // spawn selected slot at the aimed point (feet fallback)

    [[nodiscard]] std::vector<Slot>& All();
    [[nodiscard]] std::size_t SelectedIndex();
    void Select(std::size_t index);
    void Rename(std::size_t index, const std::string& name);
    void Remove(std::size_t index);

}  // namespace Palette
