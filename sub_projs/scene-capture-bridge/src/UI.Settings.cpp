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
        Modes::Mode::kPlace, Modes::Mode::kEdit,
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
        "Console: sc mk | del | pk | pl | ed | off — one mode at a time; the "
        "mode's action key does the work. sc mk dp0 / dp1 hides / shows the "
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
    ImGuiMCP::Text("Aim source (sc del|pk|ed er0/er1):");
    for (auto m : {Modes::Mode::kDelete, Modes::Mode::kPick, Modes::Mode::kEdit}) {
        ImGuiMCP::BulletText("%-6s %s", Modes::Name(m),
            Modes::UseRay(m) ? "ray" : "crosshair");
    }
    ImGuiMCP::Text("Edit numpad mode (sc ed ax): %s",
        Editor::RotateMode() ? "ROTATE" : "move");
    ImGuiMCP::Separator();

    // --- marker gem visibility (mirrors sc mk dp0/dp1) ---
    bool show = Markers::ProxiesVisible();
    if (ImGuiMCP::Checkbox("marker gems visible", &show)) {
        Markers::SetProxiesVisible(show);
    }
}
