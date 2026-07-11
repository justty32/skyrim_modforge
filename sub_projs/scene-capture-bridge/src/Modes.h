#pragma once

// Modes — the P5 mode system (user-decided 2026-07-11).
//
// One mode is active at a time; each mode has its OWN action-key binding
// (panel-configurable, duplicates allowed — with a single active mode the
// same key can serve every mode, which is the default: everything on F11).
// `sc <cmd>` in the console switches modes (Console.cpp); the panel Settings
// page switches and rebinds too.
//
// There are NO classic direct hotkeys (user-decided: removed entirely, not
// toggled off). The whole input surface is: per-mode action key + the numpad
// keys INSIDE the editor's edit mode + numpad * ray-select. Export is a panel
// button only.
//
// Bindings and the current mode persist in the SAVEGAME via the SKSE co-save
// (CoSave.cpp) — the user's no-ini decision.

#include <cstdint>

namespace Modes {

    enum class Mode : std::uint8_t {
        kOff = 0,
        kMarker,   // action: place a marker at the aimed point
        kDelete,   // action: erase the crosshair target
        kPick,     // action: eyedrop the crosshair target into the palette
        kPlace,    // action: place the selected palette slot at the aimed point
        kEdit,     // action: select the crosshair target into numpad edit mode
        kTotal
    };

    [[nodiscard]] Mode Current();
    void Set(Mode m);  // DebugNotification + log

    [[nodiscard]] const char* Name(Mode m);  // "off" / "marker" / ...
    [[nodiscard]] const char* Cmd(Mode m);   // "off" / "mk" / "del" / "pk" / "pl" / "ed"

    // Per-mode action key (DIK scancode). kOff has no binding.
    [[nodiscard]] std::uint32_t Bind(Mode m);
    void SetBind(Mode m, std::uint32_t scancode);

    // Per-mode aim source: false = the interaction crosshair (classic feel),
    // true = a physics ray (trees / non-activatable statics). Toggled by
    // `sc del er0/er1`, `sc pk ...`, `sc ed ...`. Only delete/pick/edit read
    // it (marker/place are inherently aimed). Persists in the co-save.
    [[nodiscard]] bool UseRay(Mode m);
    void SetUseRay(Mode m, bool useRay);

    // Feed a key-down. Returns true when consumed: either it completed a
    // pending rebind, or it matched the current mode's binding and ran the
    // mode's action (debounced).
    bool HandleKey(std::uint32_t scancode);

    // Panel rebind flow: arm, then the next key pressed becomes the binding
    // (Esc cancels). While armed HandleKey consumes everything.
    void BeginRebind(Mode m);
    void CancelRebind();
    [[nodiscard]] bool RebindArmed();
    [[nodiscard]] Mode RebindTarget();

    void ResetDefaults();  // off + every binding back to F11 (new game / no co-save)

    // Short scancode label for the panel ("F11", "numpad 5", "0x2A").
    [[nodiscard]] const char* KeyName(std::uint32_t scancode);

}  // namespace Modes
