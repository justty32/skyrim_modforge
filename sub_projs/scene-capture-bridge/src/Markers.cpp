#include "Markers.h"

#include "SceneExporter.h"
#include "log.h"

namespace {
    constexpr float kRadToDeg = 57.2957795f;

    std::vector<Markers::Entry> g_entries;
    std::uint32_t g_nextSeq = 1;

    // The visible proxy base. Preferred: the tooling esp's MarkerACTI (clean
    // identification — its base can ONLY be ours). Fallback: vanilla
    // SummonTargetFXActivator 0x0007CD55 (the glowing summon circle,
    // Magic\SummonTargetFX.nif — verified via houseCARL), so the hotkey path
    // works even without the tooling esp installed.
    RE::TESBoundObject* ProxyBase() {
        static RE::TESBoundObject* base = [] {
            auto* dh = RE::TESDataHandler::GetSingleton();
            RE::TESBoundObject* b = nullptr;
            if (dh) {
                b = dh->LookupForm<RE::TESObjectACTI>(0x800, "SceneCaptureTools.esp");
                if (b) {
                    SKSE::log::info("Markers: proxy base = SceneCaptureTools.esp MarkerACTI");
                } else {
                    b = dh->LookupForm<RE::TESObjectACTI>(0x07CD55, "Skyrim.esm");
                    SKSE::log::info(
                        "Markers: tooling esp absent — proxy base = vanilla "
                        "SummonTargetFXActivator ({})", b ? "ok" : "MISSING");
                }
            }
            return b;
        }();
        return base;
    }
}

namespace Markers {

    bool PlaceAtPlayer() {
        auto* player = RE::PlayerCharacter::GetSingleton();
        auto* base = ProxyBase();
        if (!player || !base) {
            SKSE::log::error("Markers: no player or no proxy base — marker not placed");
            return false;
        }

        RE::TESObjectCELL* cell = player->GetParentCell();
        std::string anchor;
        bool interior = false;
        if (cell && cell->IsInteriorCell()) {
            interior = true;
            if (auto id = SceneExporter::ResolveDurableId(cell)) anchor = *id;
        } else if (cell) {
            if (auto* ws = cell->GetRuntimeData().worldSpace)
                if (auto id = SceneExporter::ResolveDurableId(ws)) anchor = *id;
        }

        RE::NiPointer<RE::TESObjectREFR> proxy = player->PlaceObjectAtMe(base, false);
        if (!proxy) {
            SKSE::log::error("Markers: PlaceObjectAtMe failed");
            return false;
        }

        Entry e;
        e.seq = g_nextSeq++;
        e.label = std::format("marker-{}", e.seq);
        e.position = player->GetPosition();   // player feet, fixed now
        e.angleZDeg = player->GetAngleZ() * kRadToDeg;  // engine radians -> contract degrees
        e.cellOrWs = std::move(anchor);
        e.isInterior = interior;
        e.proxy = proxy->GetHandle();

        // Label doubles as the proxy's display name — display names persist in
        // the savegame, which keeps a label-recovery path open across reloads.
        proxy->SetDisplayName(e.label.c_str(), true);

        SKSE::log::info("Markers: placed #{} '{}' at ({:.1f}, {:.1f}, {:.1f}) in {}",
            e.seq, e.label, e.position.x, e.position.y, e.position.z,
            e.cellOrWs.empty() ? "(unresolved)" : e.cellOrWs);
        g_entries.push_back(std::move(e));
        return true;
    }

    std::vector<Entry>& All() { return g_entries; }

    bool IsProxy(RE::TESObjectREFR* ref) {
        if (!ref) return false;
        // Base check first: also catches orphaned proxies from a previous
        // session that are not in this session's registry.
        if (ref->GetBaseObject() == ProxyBase() && ProxyBase()) return true;
        const auto h = ref->GetHandle();
        for (const auto& e : g_entries)
            if (e.proxy == h) return true;
        return false;
    }

    static Entry* Find(std::uint32_t seq) {
        for (auto& e : g_entries)
            if (e.seq == seq) return &e;
        return nullptr;
    }

    void Rename(std::uint32_t seq, const std::string& label) {
        if (auto* e = Find(seq)) {
            e->label = label;
            if (auto proxy = e->proxy.get())
                proxy->SetDisplayName(label.c_str(), true);
        }
    }

    void SetKind(std::uint32_t seq, const std::string& kind) {
        if (auto* e = Find(seq))
            e->kind = kind.empty() ? "note" : kind;
    }

    void Remove(std::uint32_t seq) {
        for (auto it = g_entries.begin(); it != g_entries.end(); ++it) {
            if (it->seq != seq) continue;
            if (auto proxy = it->proxy.get()) {
                proxy->Disable();  // hide immediately; a dynamic disabled ref is
                                   // engine-collected (Delete() has no CommonLibSSE
                                   // surface — see plan verification list)
            }
            g_entries.erase(it);   // no trace: the true-deletion semantics
            return;
        }
    }

    void PruneDeadProxies() {
        std::size_t before = g_entries.size();
        std::erase_if(g_entries, [](const Entry& e) { return !e.proxy.get(); });
        if (before != g_entries.size())
            SKSE::log::info("Markers: pruned {} marker(s) whose proxy died with the old save",
                before - g_entries.size());
    }

}  // namespace Markers
