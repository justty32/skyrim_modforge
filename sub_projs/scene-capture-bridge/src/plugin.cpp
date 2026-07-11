#include "log.h"
#include "Console.h"
#include "CoSave.h"
#include "Editor.h"
#include "Markers.h"
#include "Modes.h"
#include "Palette.h"
#include "SceneExporter.h"
#include "UI.h"

namespace {
    // E on a marker gem -> its edit window. TESActivateEvent is a NOTIFICATION
    // (fires after the fact, kStop only stops other sinks) — that is fine: the
    // proxy ACTI has no script/sound/name, so default activation is a no-op
    // and there is nothing to suppress. An orphaned proxy (previous session)
    // is adopted on the spot so the window can still open on it.
    class ActivateSink : public RE::BSTEventSink<RE::TESActivateEvent>
    {
    public:
        static ActivateSink* GetSingleton() { static ActivateSink s; return &s; }

        RE::BSEventNotifyControl ProcessEvent(const RE::TESActivateEvent* e,
            RE::BSTEventSource<RE::TESActivateEvent>*) override
        {
            if (!e || !e->objectActivated || !e->actionRef ||
                !e->actionRef->IsPlayerRef()) {
                return RE::BSEventNotifyControl::kContinue;
            }
            auto* ref = e->objectActivated.get();
            if (!Markers::IsProxy(ref)) {
                return RE::BSEventNotifyControl::kContinue;
            }
            auto seq = Markers::SeqOf(ref);
            if (!seq) seq = Markers::AdoptOne(ref);
            if (seq) {
                SKSE::log::info("Markers: proxy #{} activated -> edit window", seq);
                UI::MarkerEditor::Open(seq);
            }
            return RE::BSEventNotifyControl::kContinue;
        }
    };

    // The P5 input surface — three layers, nothing else (the classic F6/F7/
    // F8/F10/F11 direct hotkeys are GONE, user-decided, not toggled off):
    //   1. an armed panel rebind captures the next key,
    //   2. edit-mode numpad internals (+ numpad * ray-select entry),
    //   3. the current mode's action key (per-mode binding, default F11).
    // Sink shape lifted from my_skyrim_plugin_1's FollowLight::HotkeySink
    // (in-game proven). One poll can carry several events chained through
    // `next`, so walk the list rather than reading only the head.
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
                if (Modes::RebindArmed()) {  // capture beats everything
                    Modes::HandleKey(code);
                    continue;
                }
                if (Editor::HandleKey(code)) continue;
                Modes::HandleKey(code);
            }
            return RE::BSEventNotifyControl::kContinue;
        }
    };
}

void OnDataLoaded() {
    if (auto* idm = RE::BSInputDeviceManager::GetSingleton()) {
        idm->AddEventSink(HotkeySink::GetSingleton());
        SKSE::log::info("SceneCaptureBridge: input sink registered (mode system, "
            "per-mode binds default F11)");
    } else {
        SKSE::log::error(
            "SceneCaptureBridge: BSInputDeviceManager null — action keys NOT registered");
    }
    if (auto* holder = RE::ScriptEventSourceHolder::GetSingleton()) {
        holder->AddEventSink<RE::TESActivateEvent>(ActivateSink::GetSingleton());
    }
    Console::Install();  // the `sc` console command (mode switching)
    UI::Register();      // no-op when SKSE Menu Framework is absent
    Palette::Load();     // slots persist on disk, across saves and sessions
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
    CoSave::Register();  // settings + registries ride along with every save
    return true;
}
