// UI.Settings.cpp — the Settings page: mode switching, per-mode keybinds,
// marker gem visibility. Split from UI.cpp (300-line convention).

#include "UI.h"

#include "Editor.h"
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

    // --- per-mode action key: rebindable (reworked 2026-07-12) ---
    // The old flow bound whatever keyboard key arrived first once armed —
    // since the panel doesn't pause the game, that was often a leftover WASD
    // tap, not the key the player meant to press. The fix (Modes.cpp):
    // reserved keys (movement/console/Tab/Enter/Esc) are never accepted, and
    // the accepted key must be pressed AND released while armed before it
    // commits — so a stray tap can't half-finish a bind either.
    if (Modes::RebindArmed()) {
        const auto cand = Modes::RebindCandidate();
        if (cand) {
            ImGuiMCP::TextColored({1.f, 0.85f, 0.2f, 1.f},
                "Rebinding %s -- release %s to confirm (Esc cancels)",
                Modes::Name(Modes::RebindTarget()), Modes::KeyName(cand));
        } else {
            ImGuiMCP::TextColored({1.f, 0.85f, 0.2f, 1.f},
                "Rebinding %s -- press a key (Esc cancels; WASD/Space/Shift/"
                "Ctrl/console/Tab/Enter don't count)",
                Modes::Name(Modes::RebindTarget()));
        }
        ImGuiMCP::SameLine();
        if (ImGuiMCP::Button("Cancel rebind")) Modes::CancelRebind();
        ImGuiMCP::Separator();
    }
    ImGuiMCP::Text("Action keys (per mode):");
    for (auto m : kActionModes) {
        const bool armingThis = Modes::RebindArmed() && Modes::RebindTarget() == m;
        ImGuiMCP::Text("%-8s %s", Modes::Name(m), Modes::KeyName(Modes::Bind(m)));
        ImGuiMCP::SameLine();
        if (armingThis) {
            ImGuiMCP::TextDisabled("(waiting for key...)");
        } else {
            const std::string btnId = std::string("Rebind##") + Modes::Cmd(m);
            const bool disable = Modes::RebindArmed();  // one rebind at a time
            if (disable) ImGuiMCP::BeginDisabled(true);
            if (ImGuiMCP::Button(btnId.c_str())) Modes::BeginRebind(m);
            if (disable) ImGuiMCP::EndDisabled();
        }
    }
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
