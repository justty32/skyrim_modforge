#include "log.h"
#include "SceneExporter.h"
#include "UI.h"

#include <chrono>

using namespace std::chrono_literals;

namespace {
    // DirectInput scancode, NOT a virtual-key code. 0x44 = F10.
    constexpr std::uint32_t kExportKey = 0x44;

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
                if (btn->GetIDCode() != kExportKey) continue;

                const auto now = std::chrono::steady_clock::now();
                if (now - g_lastPress < 200ms) continue;  // debounce
                g_lastPress = now;

                SceneExporter::ExportPlayerCellToFile();
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
