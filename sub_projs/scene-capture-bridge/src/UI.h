#pragma once

// UI — the in-game editor panel (Idea #24, the §B/§D/§E surface).
//
// Rendered by SKSE Menu Framework 3 (Dear ImGui over the game's D3D11). That
// framework is a SOFT dependency: SKSEMenuFramework::IsInstalled() is a
// GetModuleHandleW probe, so a player without it still gets the F10 hotkey.
//
// The panel lives here rather than in a separate sub-project because
// scene-capture-bridge already IS the consumer SKSE plugin the framework
// needs — see workflows/idea/tools/24-ingame-editor.md and
// sub_projs/mod-survey/findings/skse-menu-framework-3.md.

namespace UI {

    // Register the panel. Safe to call when the framework is absent (no-op).
    void Register();

    namespace Export {
        void __stdcall Render();
    }

    namespace MarkersPage {
        void __stdcall Render();
    }

    namespace EraserPage {
        void __stdcall Render();
    }

    namespace PalettePage {
        void __stdcall Render();
    }

    namespace EditorPage {
        void __stdcall Render();
    }

}  // namespace UI
