#include "log.h"
#include "Eraser.h"
#include "Palette.h"
#include "Markers.h"
#include "SceneExporter.h"
#include "UI.h"

#include <chrono>

using namespace std::chrono_literals;

namespace {
    // DirectInput scancodes, NOT virtual-key codes. 0x44 = F10 (in-game
    // confirmed). Marker key was F9 (0x43, fired correctly) but F9 is
    // vanilla QUICKLOAD — the game acted on it too. F11 = 0x57 (the DIK
    // table jumps after F10; NOT contiguous) is unbound in vanilla; the log
    // line below verifies on first use.
    constexpr std::uint32_t kExportKey = 0x44;
    constexpr std::uint32_t kMarkerKey = 0x57;
    // F8 = 0x42, from the same confirmed contiguous DIK F1..F10 block as F10.
    // Vanilla binds F5 (quicksave) and F9 (quickload); F6/F7/F8 are free.
    constexpr std::uint32_t kEraseKey = 0x42;
    constexpr std::uint32_t kPickKey = 0x40;    // F6 — eyedropper: pick crosshair base
    constexpr std::uint32_t kPlaceKey = 0x41;   // F7 — place selected slot at aim

    std::chrono::steady_clock::time_point g_lastPress{};

    // Shape lifted from my_skyrim_plugin_1's FollowLight::HotkeySink (in-game
    // proven). One poll can carry several events chained through `next`, so walk
    // the list rather than reading only the head.
    class HotkeySink : public RE::BSTEventSink<RE::InputEvent*>
    {
    public:
        static HotkeySink* GetSingleton() { static HotkeySink s; return &s; }

        RE::BSEventNotifyControl ProcessEvent(RE::InputEvent* const* a_events,
            RE::BSTEventSource<RE::InputEvent*>*) override
        {
            if (!a_events) {
                return RE::BSEventNotifyControl::kContinue;
            }
            for (auto* e = *a_events; e; e = e->next) {
                auto* btn = e->AsButtonEvent();
                if (!btn || !btn->IsDown()) continue;
                if (btn->GetDevice() != RE::INPUT_DEVICE::kKeyboard) continue;
                const auto code = btn->GetIDCode();
                if (code != kExportKey && code != kMarkerKey && code != kEraseKey &&
                    code != kPickKey && code != kPlaceKey) continue;

                const auto now = std::chrono::steady_clock::now();
                if (now - g_lastPress < 200ms) continue;  // debounce
                g_lastPress = now;

                if (code == kExportKey) {
                    SceneExporter::ExportPlayerCellToFile();
                } else if (code == kMarkerKey) {
                    SKSE::log::info("hotkey: scancode 0x{:X} -> place marker (aimed)", code);
                    Markers::PlaceAimed();
                } else if (code == kEraseKey) {
                    SKSE::log::info("hotkey: scancode 0x{:X} -> erase crosshair target", code);
                    Eraser::MarkCrosshair();
                } else if (code == kPickKey) {
                    SKSE::log::info("hotkey: scancode 0x{:X} -> pick into palette", code);
                    Palette::PickCrosshair();
                } else {
                    SKSE::log::info("hotkey: scancode 0x{:X} -> place selected slot", code);
                    Palette::PlaceSelected();
                }
            }
            return RE::BSEventNotifyControl::kContinue;
        }
    };
}

void OnDataLoaded() {
    // M4 spike: option 1 (hotkey) only — enough to prove cell-walk → scene.json.
    // Richer triggers (console command / Papyrus native / ImGui panel for the
    // §B/§D/§E semantic markup) come later.
    if (auto* idm = RE::BSInputDeviceManager::GetSingleton()) {
        idm->AddEventSink(HotkeySink::GetSingleton());
        SKSE::log::info(
            "SceneCaptureBridge: export hotkey registered (scancode 0x{:X})",
            kExportKey);
    } else {
        SKSE::log::error(
            "SceneCaptureBridge: BSInputDeviceManager null — export hotkey NOT registered");
    }
    UI::Register();  // no-op when SKSE Menu Framework is absent
    SKSE::log::info("SceneCaptureBridge: data loaded, exporter ready");
}

void MessageHandler(SKSE::MessagingInterface::Message* a_msg) {
    switch (a_msg->type) {
    case SKSE::MessagingInterface::kDataLoaded:
        SKSE::log::info("kDataLoaded: game data loaded");
        OnDataLoaded();
        break;
    case SKSE::MessagingInterface::kPostLoadGame:
        // A load wipes pre-load dynamic refs; drop registry ghosts.
        Markers::PruneDeadProxies();
        break;
    default:
        break;
    }
}

SKSEPluginLoad(const SKSE::LoadInterface* skse) {
    SKSE::Init(skse);
    SetupLog();
    SKSE::log::info("SceneCaptureBridge loaded");

    auto* messaging = SKSE::GetMessagingInterface();
    if (!messaging->RegisterListener("SKSE", MessageHandler)) {
        SKSE::log::error("Failed to register SKSE message listener");
        return false;
    }
    return true;
}
