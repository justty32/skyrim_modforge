// UI.Settings.cpp — the Settings page: mode switching, per-mode keybinds,
// marker gem visibility. Split from UI.cpp (300-line convention).

#include "UI.h"

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

    // --- per-mode action keys (duplicates allowed — one active mode) ---
    ImGuiMCP::Text("Action keys (stored in the savegame):");
    for (auto m : kActionModes) {
        ImGuiMCP::PushID(static_cast<int>(m));
        ImGuiMCP::Text("%-6s", Modes::Name(m));
        ImGuiMCP::SameLine();
        ImGuiMCP::Text("%s", Modes::KeyName(Modes::Bind(m)));
        ImGuiMCP::SameLine();
        if (Modes::RebindArmed() && Modes::RebindTarget() == m) {
            ImGuiMCP::Text("... press a key (Esc cancels)");
        } else if (ImGuiMCP::Button("rebind")) {
            Modes::BeginRebind(m);
        }
        ImGuiMCP::PopID();
    }
    ImGuiMCP::Separator();

    // --- marker gem visibility (mirrors sc mk dp0/dp1) ---
    bool show = Markers::ProxiesVisible();
    if (ImGuiMCP::Checkbox("marker gems visible", &show)) {
        Markers::SetProxiesVisible(show);
    }
}
