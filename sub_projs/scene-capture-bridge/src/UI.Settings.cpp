// UI.Settings.cpp — the Settings page: mode switching, per-mode keybinds,
// marker gem visibility. Split from UI.cpp (300-line convention).

#include "UI.h"

#include "Editor.h"
#include "Markers.h"
#include "Modes.h"

#include "SKSEMenuFramework.h"

namespace {
    constexpr Modes::Mode kActionModes[] = {
        Modes::Mode::kMarker, Modes::Mode::kDelete, Modes::Mode::kPick,
        Modes::Mode::kPlace, Modes::Mode::kEdit, Modes::Mode::kCapture,
        Modes::Mode::kReferrer,
    };
}

void UI::ModeLine() {
    ImGuiMCP::Text("Mode: %s", Modes::Name(Modes::Current()));
    ImGuiMCP::Separator();
}

void __stdcall UI::SettingsPage::Render() {
    // --- mode switching (button parity with the `sc` console command) ---
    ImGuiMCP::Text("Mode: %s", Modes::Name(Modes::Current()));
    if (ImGuiMCP::Button("off")) Modes::Set(Modes::Mode::kOff);
    for (auto m : kActionModes) {
        ImGuiMCP::SameLine();
        if (ImGuiMCP::Button(Modes::Name(m))) Modes::Set(m);
    }
    ImGuiMCP::TextWrapped(
        "Console: sc mk | del | pk | pl | ed | cap | ref | off — one mode at a time; "
        "the mode's action key does the work. sc mk dp0 / dp1 hides / shows the "
        "marker gems.");
    ImGuiMCP::Separator();

    // --- per-mode action key: fixed at F11 for now ---
    // Rebinding is temporarily hidden — the capture flow grabbed the wrong keys
    // in-game (e.g. movement W). The action key stays F11 for every mode until
    // it's reworked. (Modes::BeginRebind still exists, just not surfaced here.)
    ImGuiMCP::TextWrapped("Action key: F11 for every mode (rebinding disabled "
                          "pending a fix).");
    ImGuiMCP::Separator();

    // --- edit-mode step sizes (persist in the co-save SETT v2) ---
    ImGuiMCP::Text("Edit step sizes:");
    float mv = Editor::MoveStep();
    if (ImGuiMCP::InputFloat("move (units/tap)", &mv, 1.f, 10.f, "%.1f"))
        Editor::SetMoveStep(mv);
    float yaw = Editor::YawStep();
    if (ImGuiMCP::InputFloat("yaw (deg/tap)", &yaw, 1.f, 15.f, "%.1f"))
        Editor::SetYawStep(yaw);
    float sc = Editor::ScaleStep();
    if (ImGuiMCP::InputFloat("scale (per tap)", &sc, 0.01f, 0.1f, "%.3f"))
        Editor::SetScaleStep(sc);
    ImGuiMCP::Separator();

    // --- aim source + rotate axis (set via console; shown here for reference) ---
    ImGuiMCP::Text("Aim source (sc del|pk|ed|cap|ref er0/er1):");
    for (auto m : {Modes::Mode::kDelete, Modes::Mode::kPick, Modes::Mode::kEdit,
             Modes::Mode::kCapture, Modes::Mode::kReferrer}) {
        ImGuiMCP::BulletText("%-8s %s", Modes::Name(m),
            Modes::UseRay(m) ? "ray" : "crosshair");
    }
    ImGuiMCP::Text("Edit numpad mode (sc ed ax): %s",
        Editor::RotateMode() ? "ROTATE" : "move");
    ImGuiMCP::Separator();

    // --- physics (sc pl py0/py1, sc ed py0/py1) ---
    // Console-set, shown here (same convention as the aim source above).
    ImGuiMCP::Text("Physics (sc pl / sc ed  py0 = off, py1 = on):");
    ImGuiMCP::BulletText("place   %s",
        Modes::Physics(Modes::Mode::kPlace) ? "py1 — placed objects keep full physics"
                                            : "py0 — frozen on placement + exported noHavokSettle");
    ImGuiMCP::BulletText("edit    %s",
        Modes::Physics(Modes::Mode::kEdit) ? "py1 — physics keeps running while you edit"
                                           : "py0 — frozen while you control the object");
    ImGuiMCP::TextWrapped(
        "`sc pl py0` is the one that ships: the export flags the REFR DontHavokSettle, "
        "so the built esp's object survives the engine's load-time havok settle (what "
        "otherwise flings hand-placed clutter across the room). The in-game freeze alone "
        "would die with the savegame.");
    ImGuiMCP::Separator();

    // --- instance extra data (sc pk ed0/ed1, sc pl ed0/ed1) ---
    ImGuiMCP::Text("Extra data (sc pk / sc pl  ed0 = base only, ed1 = carry):");
    ImGuiMCP::BulletText("pick    %s",
        Modes::ExtraData(Modes::Mode::kPick) ? "ed1 — base + the instance's enchantment"
                                             : "ed0 — the durable base only");
    ImGuiMCP::BulletText("place   %s",
        Modes::ExtraData(Modes::Mode::kPlace) ? "ed1 — export mints the enchanted item"
                                              : "ed0 — the plain base");
    ImGuiMCP::TextWrapped(
        "A player-applied enchant lives on the REF, not the base — pick it with ed0 and "
        "you get a plain iron sword. With pick+place on ed1 the export writes a "
        "capturedItems[] record for the enchanted item and points the placement's base at "
        "it (mint + reference, in the same file).");
    ImGuiMCP::Separator();

    // --- marker gem visibility (mirrors sc mk dp0/dp1) ---
    bool show = Markers::ProxiesVisible();
    if (ImGuiMCP::Checkbox("marker gems visible", &show)) {
        Markers::SetProxiesVisible(show);
    }
}
