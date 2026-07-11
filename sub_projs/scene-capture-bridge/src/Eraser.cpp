#include "Eraser.h"

#include "Aim.h"
#include "Markers.h"
#include "SceneExporter.h"
#include "log.h"

namespace {
    constexpr std::uint32_t kInitiallyDisabled = 0x00000800u;

    std::vector<Eraser::Entry> g_entries;
    std::unordered_set<std::string> g_ids;
    std::vector<Eraser::Candidate> g_candidates;

    bool AddsMaster(const std::string& plugin) {
        // The 5 base-game masters every load order has. CC .esl content is
        // deliberately NOT in this set: erasing a CC ref really does add that
        // esl as a master of the patch.
        static const std::unordered_set<std::string> kBase = {
            "Skyrim.esm", "Update.esm", "Dawnguard.esm",
            "HearthFires.esm", "Dragonborn.esm",
        };
        return !kBase.contains(plugin);
    }

    std::string PluginOf(const std::string& id) {
        const auto colon = id.find(':');
        return colon == std::string::npos ? id : id.substr(0, colon);
    }
}

namespace Eraser {

    static MarkResult MarkRef(RE::NiPointer<RE::TESObjectREFR> ref, const char* how) {
        if (!ref) {
            SKSE::log::info("Eraser: {} has no target", how);
            return MarkResult::kNone;
        }
        // A marker proxy is editor chrome — route to the marker system so it
        // vanishes from the registry too, instead of half-erasing it here.
        if (Markers::IsProxy(ref.get())) {
            for (const auto& m : Markers::All())
                if (m.proxy == ref->GetHandle()) {
                    Markers::Remove(m.seq);
                    SKSE::log::info("Eraser: crosshair was a marker proxy — removed marker instead");
                    return MarkResult::kMarkerProxy;
                }
            ref->Disable();  // orphan proxy (not in registry): just hide it
            return MarkResult::kMarkerProxy;
        }

        if (auto id = SceneExporter::ResolveDurableId(ref.get())) {
            if (g_ids.contains(*id)) return MarkResult::kDuplicate;
            ref->Disable();  // the visual feedback: it vanishes right now
            Entry e{*id, PluginOf(*id), false,
                    SceneExporter::AnchorOf(ref.get()).id, ref->GetHandle()};
            e.addsMaster = AddsMaster(e.plugin);
            SKSE::log::info("Eraser: marked {} for removal ({}){}", e.id, how,
                e.addsMaster ? " (adds a master!)" : "");
            g_ids.insert(e.id);
            g_entries.push_back(std::move(e));
            return MarkResult::kMarked;
        }

        // Dynamic ref = something the player placed this session. True
        // deletion semantics: disable, drop from every registry, no trace.
        ref->Disable();
        SKSE::log::info("Eraser: own dynamic ref erased (no trace)");
        return MarkResult::kOwnDeleted;
    }

    MarkResult MarkCrosshair() { return MarkRef(Aim::CrosshairRef(), "crosshair"); }
    MarkResult MarkByRay() { return MarkRef(Aim::RayRef(), "ray"); }

    std::vector<Entry>& All() { return g_entries; }
    const std::unordered_set<std::string>& MarkedIds() { return g_ids; }

    bool Undo() {
        if (g_entries.empty()) return false;
        Entry e = std::move(g_entries.back());
        g_entries.pop_back();
        g_ids.erase(e.id);
        if (auto ref = e.handle.get()) {
            ref->Enable(false);
            SKSE::log::info("Eraser: undo — {} re-enabled", e.id);
        } else {
            SKSE::log::info("Eraser: undo — {} unmarked (ref not loaded)", e.id);
        }
        return true;
    }

    void Clear() {
        while (!g_entries.empty()) Undo();
    }

    std::size_t ScanDisabled() {
        g_candidates.clear();
        auto* player = RE::PlayerCharacter::GetSingleton();
        RE::TESObjectCELL* cell = player ? player->GetParentCell() : nullptr;
        if (!cell) return 0;

        const std::string anchor = SceneExporter::AnchorOf(nullptr).id;
        cell->ForEachReference([&](RE::TESObjectREFR* ref) -> RE::BSContainer::ForEachResult {
            if (!ref || ref->IsDeleted() || !ref->IsDisabled())
                return RE::BSContainer::ForEachResult::kContinue;
            // Authored to spawn disabled = vanilla design (enable-parent
            // chains etc.), not an erased ref — never a candidate.
            if ((ref->GetFormFlags() & kInitiallyDisabled) != 0)
                return RE::BSContainer::ForEachResult::kContinue;
            auto id = SceneExporter::ResolveDurableId(ref);
            if (!id || g_ids.contains(*id))
                return RE::BSContainer::ForEachResult::kContinue;

            Candidate c;
            c.id = *id;
            const char* dn = ref->GetDisplayFullName();
            c.name = (dn && *dn) ? dn : "(unnamed)";
            c.addsMaster = AddsMaster(PluginOf(*id));
            c.cellOrWs = anchor;
            c.handle = ref->GetHandle();
            g_candidates.push_back(std::move(c));
            return RE::BSContainer::ForEachResult::kContinue;
        });
        SKSE::log::info("Eraser: scan found {} disabled candidate(s) in this cell",
            g_candidates.size());
        return g_candidates.size();
    }

    std::vector<Candidate>& Candidates() { return g_candidates; }

    void AdoptCandidate(std::size_t index) {
        if (index >= g_candidates.size()) return;
        auto& c = g_candidates[index];
        if (!g_ids.contains(c.id)) {
            // Already disabled in-world; adopting only records the intent.
            g_ids.insert(c.id);
            g_entries.push_back(Entry{c.id, PluginOf(c.id), c.addsMaster, c.cellOrWs, c.handle});
            SKSE::log::info("Eraser: adopted {}", c.id);
        }
        g_candidates.erase(g_candidates.begin() + static_cast<std::ptrdiff_t>(index));
    }

    void DismissCandidates() { g_candidates.clear(); }

    void DropAll() {
        g_entries.clear();
        g_ids.clear();
        g_candidates.clear();
    }

    void OnRegistryRestored() {
        g_ids.clear();
        for (const auto& e : g_entries) g_ids.insert(e.id);
        SKSE::log::info("Eraser: registry restored from co-save — {} mark(s)",
            g_entries.size());
    }

}  // namespace Eraser
