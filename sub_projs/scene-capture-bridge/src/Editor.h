#pragma once

// Editor — the numpad transform mode (Idea #24 細摳②「修改」, plan P2).
//
// Numpad 5 selects the crosshair target; the numpad then nudges it; numpad 0
// commits, numpad . (Del) cancels and restores the original transform.
//
// MVP scope is deliberately narrow: only OUR OWN dynamic refs are editable —
// their live pose is what the exporter already emits, so this is contract-
// zero. An AUTHORED ref is refused with a log line: editing those is the
// `overrides[]`-shape decision (plan 技術債), and this refusal is where the
// explicit-registration hook will land once that shape is picked.
//
// Key map (one deviation from the user spec, marked for review): 8/2 =
// forward/back (player-relative), 4/6 = left/right, 7/9 = yaw, +/- = scale,
// **1/3 = height down/up** — the spec gave 1379 to rotation and left no
// height axis; furniture placement needs Z far more than a second rotation
// axis. Remap is one constant per key.

#include <cstdint>

namespace Editor {

    [[nodiscard]] bool Active();

    // Feed a keyboard scancode (IsDown only). Returns true when consumed —
    // the caller's own hotkeys must not fire while edit mode is live.
    bool HandleKey(std::uint32_t scancode);

    void Cancel();  // restore the original transform and leave edit mode

    // For the panel: current target + live transform, or a hint when idle.
    struct Status {
        bool active = false;
        const char* name = "";
        RE::NiPoint3 pos;
        float yawDeg = 0.f;
        float scale = 1.f;
    };
    [[nodiscard]] Status Current();

}  // namespace Editor
