#include "Markers.h"

#include "Aim.h"
#include "SceneExporter.h"
#include "log.h"

#include <algorithm>
#include <cmath>

namespace {
    constexpr float kRadToDeg = 57.2957795f;

    std::vector<Markers::Entry> g_entries;
    std::uint32_t g_nextSeq = 1;
    bool g_display = true;  // sc mk dp0/dp1 — persists via co-save

    // The visible proxy base. Preferred: the tooling esp's MarkerACTI — model
    // is now Clutter\SoulGem\SoulGemGrand01.nif (glowing gem; read from
    // Skyrim.esm STAT 10D18B via houseCARL, collision + glow controllers
    // verified via nif_inspect). Collision matters: the old SummonTargetFX
    // model had NO bhk blocks, so the crosshair could never target a marker
    // and E-interaction was impossible. The gem's clutter rigidbody would
    // FALL, so PlaceAt freezes it (SetMotionType kKeyframed — the same
    // in-game-proven primitive the editor uses). Fallback: vanilla
    // SummonTargetFXActivator 0x0007CD55 (no collision -> no E, hotkeys only)
    // so the plugin still works without the tooling esp.
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

    static bool PlaceAt(const RE::NiPoint3& pos, const char* how) {
        auto* player = RE::PlayerCharacter::GetSingleton();
        auto* base = ProxyBase();
        if (!player || !base) {
            SKSE::log::error("Markers: no player or no proxy base — marker not placed");
            return false;
        }
        // Durable anchor of the player's current cell/worldspace.
        const auto a = SceneExporter::AnchorOf(nullptr);
        std::string anchor = a.id;
        bool interior = a.interior;

        RE::NiPointer<RE::TESObjectREFR> proxy = player->PlaceObjectAtMe(base, false);
        if (!proxy) {
            SKSE::log::error("Markers: PlaceObjectAtMe failed");
            return false;
        }
        proxy->SetPosition(pos);
        // The gem model has clutter havok — freeze it or it falls. Best-effort:
        // if the 3D is not loaded yet this fails silently and the gem settles
        // on the ground; harmless, the EXPORTED position is fixed right here.
        proxy->SetMotionType(RE::hkpMotion::MotionType::kKeyframed, false);

        Entry e;
        e.seq = g_nextSeq++;
        e.label = std::format("marker-{}", e.seq);
        e.position = pos;                               // fixed now, not the proxy's live pose
        e.angleZDeg = player->GetAngleZ() * kRadToDeg;  // engine radians -> contract degrees
        e.cellOrWs = std::move(anchor);
        e.isInterior = interior;
        e.proxy = proxy->GetHandle();

        // Label doubles as the proxy's display name — display names persist in
        // the savegame, which is what AdoptOrphans() recovers labels from.
        proxy->SetDisplayName(e.label.c_str(), true);
        if (!g_display) proxy->Disable();  // dp0 active: new gems follow it

        SKSE::log::info("Markers: placed #{} '{}' ({}) at ({:.1f}, {:.1f}, {:.1f}) in {}",
            e.seq, e.label, how, pos.x, pos.y, pos.z,
            e.cellOrWs.empty() ? "(unresolved)" : e.cellOrWs);
        g_entries.push_back(std::move(e));
        return true;
    }

    bool PlaceAtPlayer() {
        auto* player = RE::PlayerCharacter::GetSingleton();
        return player && PlaceAt(player->GetPosition(), "feet");
    }

    bool PlaceAimed() {
        auto* player = RE::PlayerCharacter::GetSingleton();
        if (!player) return false;
        RE::NiPoint3 hit;
        if (Aim::LookHit(hit)) return PlaceAt(hit, "aimed");
        SKSE::log::info("Markers: ray hit nothing within range — falling back to feet");
        return PlaceAt(player->GetPosition(), "feet-fallback");
    }

    std::uint32_t AdoptOne(RE::TESObjectREFR* ref) {
        if (!ref || ref->IsDeleted() || ref->IsDisabled()) return 0;
        if (ref->GetBaseObject() != ProxyBase() || !ProxyBase()) return 0;
        if (const auto seq = SeqOf(ref)) return seq;  // already ours

        Entry e;
        e.seq = g_nextSeq++;
        const char* dn = ref->GetDisplayFullName();
        e.label = (dn && *dn) ? dn : std::format("marker-{}", e.seq);
        // note is NOT recoverable: only the display name (= label) lives in
        // the savegame. Documented in the README persistence table.
        e.position = ref->GetPosition();
        e.angleZDeg = ref->GetAngleZ() * kRadToDeg;
        const auto a = SceneExporter::AnchorOf(ref);
        e.cellOrWs = a.id;
        e.isInterior = a.interior;
        e.proxy = ref->GetHandle();
        // Re-created from the save with its nif's clutter havok — freeze again.
        ref->SetMotionType(RE::hkpMotion::MotionType::kKeyframed, false);
        SKSE::log::info("Markers: adopted '{}' at ({:.1f}, {:.1f}, {:.1f})",
            e.label, e.position.x, e.position.y, e.position.z);
        const auto seq = e.seq;
        g_entries.push_back(std::move(e));
        return seq;
    }

    std::size_t AdoptOrphans() {
        auto* player = RE::PlayerCharacter::GetSingleton();
        RE::TESObjectCELL* cell = player ? player->GetParentCell() : nullptr;
        if (!cell || !ProxyBase()) return 0;

        std::size_t adopted = 0;
        cell->ForEachReference([&](RE::TESObjectREFR* ref) -> RE::BSContainer::ForEachResult {
            if (ref && ref->GetBaseObject() == ProxyBase() && !SeqOf(ref))
                if (AdoptOne(ref)) ++adopted;
            return RE::BSContainer::ForEachResult::kContinue;
        });
        return adopted;
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

    Entry* FindBySeq(std::uint32_t seq) {
        for (auto& e : g_entries)
            if (e.seq == seq) return &e;
        return nullptr;
    }

    std::uint32_t SeqOf(RE::TESObjectREFR* ref) {
        if (!ref) return 0;
        const auto h = ref->GetHandle();
        for (const auto& e : g_entries)
            if (e.proxy == h) return e.seq;
        return 0;
    }

    void Rename(std::uint32_t seq, const std::string& label) {
        if (auto* e = FindBySeq(seq)) {
            e->label = label;
            if (auto proxy = e->proxy.get())
                proxy->SetDisplayName(label.c_str(), true);
        }
    }

    void SetKind(std::uint32_t seq, const std::string& kind) {
        if (auto* e = FindBySeq(seq))
            e->kind = kind.empty() ? "note" : kind;
    }

    void SetNote(std::uint32_t seq, const std::string& note) {
        if (auto* e = FindBySeq(seq))
            e->note = note;
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
        // Survivors were re-created from the save with live clutter havok —
        // re-freeze so the gems don't drop after every load.
        for (auto& e : g_entries)
            if (auto proxy = e.proxy.get())
                proxy->SetMotionType(RE::hkpMotion::MotionType::kKeyframed, false);
    }

    void SetProxiesVisible(bool visible) {
        g_display = visible;
        std::size_t touched = 0;
        for (auto& e : g_entries) {
            if (auto proxy = e.proxy.get()) {
                if (visible) proxy->Enable(false);
                else         proxy->Disable();
                ++touched;
            }
        }
        SKSE::log::info("Markers: {} {} gem(s)", visible ? "showed" : "hid", touched);
        if (visible) {
            // Enable re-spawns the 3D with live clutter havok — re-freeze.
            for (auto& e : g_entries)
                if (auto proxy = e.proxy.get())
                    proxy->SetMotionType(RE::hkpMotion::MotionType::kKeyframed, false);
        }
    }

    bool ProxiesVisible() { return g_display; }

    void OnRegistryRestored() {
        std::uint32_t maxSeq = 0;
        for (const auto& e : g_entries) maxSeq = std::max(maxSeq, e.seq);
        g_nextSeq = maxSeq + 1;
        PruneDeadProxies();  // drops unresolvable, re-freezes the rest
        if (!g_display) {
            for (auto& e : g_entries)
                if (auto proxy = e.proxy.get())
                    proxy->Disable();
        }
        SKSE::log::info("Markers: registry restored from co-save — {} marker(s)",
            g_entries.size());
    }

}  // namespace Markers
