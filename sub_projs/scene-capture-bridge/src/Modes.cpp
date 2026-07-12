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

    // Per-mode physics ("physics is KEPT" — py1 = true, so it reads straight off
    // the command). The defaults are NOT uniform, which is the whole point:
    // a PLACED object keeps its physics (py1), an EDITED one loses it while you
    // drive it (py0 — the P3 freeze-on-select behaviour, now switchable).
    // Indices: off, marker, delete, pick, place, edit, capture, referrer.
    void ApplyPhysicsDefaults(bool (&p)[static_cast<std::size_t>(Modes::Mode::kTotal)]) {
        for (auto& v : p) v = true;  // "keep physics" is the neutral value
        p[static_cast<std::size_t>(Modes::Mode::kEdit)] = false;  // py0 = freeze while editing
    }
    bool g_physics[static_cast<std::size_t>(Modes::Mode::kTotal)] = {
        true, true, true, true, true, /*kEdit*/ false, true, true};
    // Per-mode extra data (pick/place). Off = durable base only (historic).
    bool g_extraData[static_cast<std::size_t>(Modes::Mode::kTotal)] = {false};

    bool g_rebindArmed = false;
    Modes::Mode g_rebindTarget = Modes::Mode::kOff;
    std::uint32_t g_rebindCandidate = 0;  // key currently down-while-armed, awaiting release

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

    bool Physics(Mode m) {
        return m < Mode::kTotal ? g_physics[static_cast<std::size_t>(m)] : true;
    }

    void SetPhysics(Mode m, bool keepPhysics) {
        if (m == Mode::kOff || m >= Mode::kTotal) return;
        g_physics[static_cast<std::size_t>(m)] = keepPhysics;
        SKSE::log::info("Modes: {} physics -> {}", Name(m),
            keepPhysics ? "kept (py1)" : "OFF (py0)");
    }

    bool ExtraData(Mode m) {
        return m < Mode::kTotal ? g_extraData[static_cast<std::size_t>(m)] : false;
    }

    void SetExtraData(Mode m, bool on) {
        if (m == Mode::kOff || m >= Mode::kTotal) return;
        g_extraData[static_cast<std::size_t>(m)] = on;
        SKSE::log::info("Modes: {} extra data -> {}", Name(m),
            on ? "carried (ed1)" : "base only (ed0)");
    }

    bool IsBindable(std::uint32_t scancode) {
        switch (scancode) {
        case kEsc:              // Esc — cancels, never binds
        case 0x29:               // ` / ~ — opens the console
        case 0x0F:                // Tab — ImGui focus navigation
        case 0x1C:                // Enter — confirms ImGui widgets / console lines
        case 0x11: case 0x1E:      // W, A
        case 0x1F: case 0x20:       // S, D
        case 0x39:                   // Space — jump
        case 0x2A: case 0x36:         // LShift, RShift — sprint
        case 0x1D:                     // LCtrl — sneak
            return false;
        default:
            return true;
        }
    }

    bool HandleKey(std::uint32_t scancode) {
        if (g_rebindArmed) {
            if (scancode == kEsc) {
                CancelRebind();
                return true;
            }
            if (!IsBindable(scancode)) {
                // This is the historic bug (backlog: "rebind armed 當幀把移動鍵
                // 也吃進去"): the panel doesn't pause the game, so the player's
                // hand is often still on WASD (or the console/Tab/Enter fire
                // incidentally) the instant they click "Rebind". Swallow the
                // reserved key and stay armed instead of binding it.
                SKSE::log::info("Modes: rebind ignored reserved key 0x{:X}", scancode);
                RE::DebugNotification("SCB: that key is reserved, press another");
                return true;
            }
            // Don't bind yet — only remember the candidate. A key already
            // held when the rebind armed can never reach this branch at all
            // (ButtonEvent::IsDown() fires only on the up->down transition),
            // and requiring the matching key-UP (HandleKeyUp) below rejects
            // any stray double-tap before it commits.
            g_rebindCandidate = scancode;
            SKSE::log::info("Modes: rebind candidate {} (0x{:X}) — release to confirm",
                KeyName(scancode), scancode);
            return true;
        }
        if (g_mode == Mode::kOff || scancode != Bind(g_mode)) return false;

        const auto now = std::chrono::steady_clock::now();
        if (now - g_lastAction < 200ms) return true;  // debounce, still consumed
        g_lastAction = now;

        SKSE::log::info("Modes: action key (0x{:X}) in mode {}", scancode, Name(g_mode));
        RunAction(g_mode);
        return true;
    }

    bool HandleKeyUp(std::uint32_t scancode) {
        if (!g_rebindArmed) return false;
        if (g_rebindCandidate && scancode == g_rebindCandidate) {
            SetBind(g_rebindTarget, scancode);
            RE::DebugNotification(
                std::format("SCB: {} bound to {}", Name(g_rebindTarget), KeyName(scancode))
                    .c_str());
            g_rebindArmed = false;
            g_rebindCandidate = 0;
        }
        return true;  // swallow every key-up while armed, matched or not
    }

    void BeginRebind(Mode m) {
        if (m == Mode::kOff || m >= Mode::kTotal) return;
        g_rebindArmed = true;
        g_rebindTarget = m;
        g_rebindCandidate = 0;
        SKSE::log::info("Modes: rebinding {} — press a key (Esc cancels)", Name(m));
    }

    void CancelRebind() {
        g_rebindArmed = false;
        g_rebindCandidate = 0;
        SKSE::log::info("Modes: rebind cancelled");
    }

    bool RebindArmed() { return g_rebindArmed; }
    Mode RebindTarget() { return g_rebindTarget; }
    std::uint32_t RebindCandidate() { return g_rebindCandidate; }

    void ResetDefaults() {
        g_mode = Mode::kOff;
        for (std::size_t i = 1; i < static_cast<std::size_t>(Mode::kTotal); ++i) {
            g_binds[i] = kDefaultBind;
            g_useRay[i] = false;
            g_extraData[i] = false;
        }
        ApplyPhysicsDefaults(g_physics);  // place = py1, edit = py0
        g_rebindArmed = false;
        g_rebindCandidate = 0;
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
