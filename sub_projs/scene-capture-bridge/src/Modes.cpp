#include "Modes.h"

#include "Captures.h"
#include "Editor.h"
#include "Eraser.h"
#include "Markers.h"
#include "Palette.h"
#include "Referrer.h"
#include "log.h"

#include <chrono>

using namespace std::chrono_literals;

namespace {
    constexpr std::uint32_t kDefaultBind = 0x57;  // F11 — in-game verified free
    constexpr std::uint32_t kEsc = 0x01;

    Modes::Mode g_mode = Modes::Mode::kOff;
    // Index by Mode; slot 0 (kOff) exists but is never read.
    std::uint32_t g_binds[static_cast<std::size_t>(Modes::Mode::kTotal)] = {
        0, kDefaultBind, kDefaultBind, kDefaultBind, kDefaultBind, kDefaultBind, kDefaultBind,
        kDefaultBind};
    // Per-mode aim source (false = crosshair). Same indexing as g_binds.
    bool g_useRay[static_cast<std::size_t>(Modes::Mode::kTotal)] = {false};

    bool g_rebindArmed = false;
    Modes::Mode g_rebindTarget = Modes::Mode::kOff;

    std::chrono::steady_clock::time_point g_lastAction{};

    void RunAction(Modes::Mode m) {
        const bool ray = Modes::UseRay(m);
        switch (m) {
        case Modes::Mode::kMarker: Markers::PlaceAimed(); break;
        case Modes::Mode::kDelete: ray ? Eraser::MarkByRay() : Eraser::MarkCrosshair(); break;
        case Modes::Mode::kPick:   ray ? Palette::PickByRay() : Palette::PickCrosshair(); break;
        case Modes::Mode::kPlace:  Palette::PlaceSelected(); break;
        case Modes::Mode::kEdit:   ray ? Editor::SelectByRay() : Editor::EnterSelect(); break;
        case Modes::Mode::kCapture: ray ? Captures::CaptureByRay() : Captures::CaptureCrosshair(); break;
        // No label on the action key — the row gets "ref-<seq>" and is renamed in
        // the panel. `sc ref <Label>` is the one-shot labelled path.
        case Modes::Mode::kReferrer: ray ? Referrer::MarkByRay("") : Referrer::MarkCrosshair(""); break;
        default: break;
        }
    }
}

namespace Modes {

    Mode Current() { return g_mode; }

    void Set(Mode m) {
        if (m >= Mode::kTotal) return;
        g_mode = m;
        const auto msg = std::format("SCB mode: {}", Name(m));
        RE::DebugNotification(msg.c_str());
        SKSE::log::info("Modes: -> {}", Name(m));
    }

    const char* Name(Mode m) {
        switch (m) {
        case Mode::kMarker: return "marker";
        case Mode::kDelete: return "delete";
        case Mode::kPick:   return "pick";
        case Mode::kPlace:  return "place";
        case Mode::kEdit:   return "edit";
        case Mode::kCapture: return "capture";
        case Mode::kReferrer: return "referrer";
        default:            return "off";
        }
    }

    const char* Cmd(Mode m) {
        switch (m) {
        case Mode::kMarker: return "mk";
        case Mode::kDelete: return "del";
        case Mode::kPick:   return "pk";
        case Mode::kPlace:  return "pl";
        case Mode::kEdit:   return "ed";
        case Mode::kCapture: return "cap";
        case Mode::kReferrer: return "ref";
        default:            return "off";
        }
    }

    std::uint32_t Bind(Mode m) {
        return m < Mode::kTotal ? g_binds[static_cast<std::size_t>(m)] : 0;
    }

    void SetBind(Mode m, std::uint32_t scancode) {
        if (m == Mode::kOff || m >= Mode::kTotal || scancode == 0) return;
        g_binds[static_cast<std::size_t>(m)] = scancode;
        SKSE::log::info("Modes: bind {} -> {} (0x{:X})", Name(m), KeyName(scancode), scancode);
    }

    bool UseRay(Mode m) {
        return m < Mode::kTotal ? g_useRay[static_cast<std::size_t>(m)] : false;
    }

    void SetUseRay(Mode m, bool useRay) {
        if (m == Mode::kOff || m >= Mode::kTotal) return;
        g_useRay[static_cast<std::size_t>(m)] = useRay;
        SKSE::log::info("Modes: {} aim source -> {}", Name(m), useRay ? "ray" : "crosshair");
    }

    bool HandleKey(std::uint32_t scancode) {
        if (g_rebindArmed) {
            if (scancode == kEsc) {
                CancelRebind();
            } else {
                SetBind(g_rebindTarget, scancode);
                g_rebindArmed = false;
            }
            return true;  // the captured key must not also fire an action
        }
        if (g_mode == Mode::kOff || scancode != Bind(g_mode)) return false;

        const auto now = std::chrono::steady_clock::now();
        if (now - g_lastAction < 200ms) return true;  // debounce, still consumed
        g_lastAction = now;

        SKSE::log::info("Modes: action key (0x{:X}) in mode {}", scancode, Name(g_mode));
        RunAction(g_mode);
        return true;
    }

    void BeginRebind(Mode m) {
        if (m == Mode::kOff || m >= Mode::kTotal) return;
        g_rebindArmed = true;
        g_rebindTarget = m;
        SKSE::log::info("Modes: rebinding {} — press a key (Esc cancels)", Name(m));
    }

    void CancelRebind() {
        g_rebindArmed = false;
        SKSE::log::info("Modes: rebind cancelled");
    }

    bool RebindArmed() { return g_rebindArmed; }
    Mode RebindTarget() { return g_rebindTarget; }

    void ResetDefaults() {
        g_mode = Mode::kOff;
        for (std::size_t i = 1; i < static_cast<std::size_t>(Mode::kTotal); ++i) {
            g_binds[i] = kDefaultBind;
            g_useRay[i] = false;
        }
        g_rebindArmed = false;
    }

    const char* KeyName(std::uint32_t scancode) {
        // The keys a player is likely to bind; anything else shows as hex.
        switch (scancode) {
        case 0x3B: return "F1";  case 0x3C: return "F2";  case 0x3D: return "F3";
        case 0x3E: return "F4";  case 0x3F: return "F5";  case 0x40: return "F6";
        case 0x41: return "F7";  case 0x42: return "F8";  case 0x43: return "F9";
        case 0x44: return "F10"; case 0x57: return "F11"; case 0x58: return "F12";
        case 0x47: return "numpad 7"; case 0x48: return "numpad 8"; case 0x49: return "numpad 9";
        case 0x4B: return "numpad 4"; case 0x4C: return "numpad 5"; case 0x4D: return "numpad 6";
        case 0x4F: return "numpad 1"; case 0x50: return "numpad 2"; case 0x51: return "numpad 3";
        case 0x52: return "numpad 0"; case 0x53: return "numpad ."; case 0x37: return "numpad *";
        case 0x4A: return "numpad -"; case 0x4E: return "numpad +";
        case 0x2A: return "LShift"; case 0x1D: return "LCtrl"; case 0x38: return "LAlt";
        default: {
            static char buf[16];
            std::snprintf(buf, sizeof(buf), "0x%X", scancode);
            return buf;
        }
        }
    }

}  // namespace Modes
