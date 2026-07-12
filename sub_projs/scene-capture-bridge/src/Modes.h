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
        kCapture,  // action: eyedrop the aimed item enchant/effects (or NPC) into capturedItems[]
        kReferrer, // action: NAME the aimed existing ref (no world change) -> references[]
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
    // `sc del er0/er1`, `sc pk ...`, `sc ed ...`, `sc cap ...`, `sc ref ...`.
    // Only delete/pick/edit/capture/referrer read it (marker/place are
    // inherently aimed). Persists in the co-save.
    [[nodiscard]] bool UseRay(Mode m);
    void SetUseRay(Mode m, bool useRay);

    // Per-mode PHYSICS switch — `sc pl py0/py1` and `sc ed py0/py1`. The stored
    // value is "physics is KEPT", so it reads straight off the command: py1 =
    // true, py0 = false. Defaults differ per mode (that is the whole point):
    //
    //   kPlace  DEFAULT py1 (physics kept) — a placed object behaves normally.
    //           `sc pl py0` = physics OFF: the object is havok-frozen the moment
    //           it is placed AND the export carries `noHavokSettle` so the
    //           SHIPPED esp keeps it put (the engine's load-time havok settle
    //           pass is what launches a hand-placed cup across the room).
    //   kEdit   DEFAULT py0 (physics off while you drive it) — the existing P3
    //           freeze-on-select behaviour. `sc ed py1` leaves havok running.
    //
    // Only place/edit read this. Persists in the co-save (SETT v6).
    [[nodiscard]] bool Physics(Mode m);
    void SetPhysics(Mode m, bool keepPhysics);

    // Per-mode EXTRA-DATA switch — `sc pk ed0/ed1` and `sc pl ed0/ed1`. False
    // (the default) = the durable BASE only, the historic behaviour. True:
    //
    //   kPick   the eyedropper also records the INSTANCE's extra data (a
    //           player-applied ExtraEnchantment lives on the ref, not the base),
    //           so the palette slot carries it.
    //   kPlace  a slot placed with it on is exported through the MINT+REFERENCE
    //           path: the scene file gets a `capturedItems[]` row for the
    //           enchanted item and the placement's `base` points at that row's
    //           editorId (a file-internal dependency, same trick as the
    //           referrer's in-file `references[]`). With it off the same slot
    //           places/exports as the plain unenchanted base.
    //
    // Only pick/place read this. Persists in the co-save (SETT v6).
    [[nodiscard]] bool ExtraData(Mode m);
    void SetExtraData(Mode m, bool on);

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
