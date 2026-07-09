#include "log.h"
#include "SceneExporter.h"

void OnDataLoaded() {
    // TODO(M4): register the export trigger. Options, cheapest → richest:
    //   1. SKSE input event sink → hotkey → SceneExporter::ExportPlayerCellToFile()
    //   2. console command / Papyrus-callable native
    //   3. ImGui panel (SKSE Menu Framework 3) for §B/§D/§E semantic markup
    // The spike (M4) only needs option 1 to prove cell-walk → scene.json.
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
