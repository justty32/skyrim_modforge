// UI.Settings.cpp — the Settings page: mode switching, per-mode keybinds,
// marker gem visibility. Split from UI.cpp (300-line convention).

#include "UI.h"

#include "Editor.h"
#include "KeyIni.h"
#include "Markers.h"
#include "Modes.h"

#include "SKSEMenuFramework.h"

#include <string>

namespace {
    constexpr Modes::Mode kActionModes[] = {
        Modes::Mode::kMarker, Modes::Mode::kDelete, Modes::Mode::kPick,
        Modes::Mode::kPlace, Modes::Mode::kEdit, Modes::Mode::kCapture,
        Modes::Mode::kReferrer,
    };
    constexpr ImGuiMCP::ImVec4 kWarn{1.f, 0.55f, 0.25f, 1.f};
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

    // --- per-mode action key: READ-ONLY here, configured in the .ini ---
    //
    // In-game rebinding is GONE (user-decided 2026-07-12, after the second
    // attempt failed in-game). Capturing a key needs the panel to own the input
    // stream, and the panel does not pause the game: the stream is full of the
    // movement keys the player's hand is still on. Two designs (bind-first-key;
    // reserved-key blacklist + press-and-release) both lost that race. A file
    // has no race to lose — see KeyIni.cpp.
    ImGuiMCP::Text("Action keys (per mode) — edit SceneCaptureBridge.ini:");
    for (auto m : kActionModes) {
        ImGuiMCP::BulletText("%-9s %-12s %s", Modes::Name(m),
            Modes::KeyName(Modes::Bind(m)),
            Modes::BindFromIni(m) ? "(ini)" : "(save / default)");
    }
    if (ImGuiMCP::Button("reload keys from ini")) KeyIni::Load();
    ImGuiMCP::SameLine();
    const auto& ki = KeyIni::Last();
    if (!ki.loaded) {
        ImGuiMCP::TextColored(kWarn, "ini not readable — keys are the defaults");
    } else if (!ki.problem.empty()) {
        ImGuiMCP::TextColored(kWarn, "ini: %s", ki.problem.c_str());
    } else {
        ImGuiMCP::Text("%zu key(s) from the ini", ki.applied);
    }
    ImGuiMCP::TextWrapped("%s", KeyIni::PathString().c_str());
    ImGuiMCP::TextWrapped(
        "One line per mode (marker/delete/pick/place/edit/capture/referrer), e.g. "
        "`delete = F4`. Names, not scancodes: F1-F12, numpad 0-9, letters, arrows... "
        "The file lists them all. WASD/Space/Shift/Ctrl/Esc/Tab/Enter/console are "
        "refused. The ini WINS over the key stored in a savegame; a mode the ini "
        "doesn't mention keeps the save's key (or F11).");
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
