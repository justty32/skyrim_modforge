// Palette.Placed.cpp — the registry of refs WE placed (`sc pl`) that the export
// has to say something EXTRA about. Split from Palette.cpp (300-line convention).
//
// Most placements need no row here at all: a plain `sc pl` object is a dynamic
// ref, and the exporter's vanilla diff already turns it into a perfect
// `placements[]` entry straight off the ref (base + live transform). A row is
// only created when the placement carries one of the two 2026-07-12 riders:
//
//   noHavokSettle  (`sc pl py0`) -> the exported REFR gets the DontHavokSettle
//                  record flag (0x20000000). This is the ONLY half that ships:
//                  the in-session SetMotionType freeze dies with the savegame.
//   extra          (`sc pl ed1`) -> the slot carried an instance enchantment, so
//                  the export MINTS a capturedItems[] record for it and points
//                  the placement's `base` at that record's editorId.
//
// IDENTITY is the ObjectRefHandle, with a (base + position) fallback — a dynamic
// FormID is not reliably remapped across a full restart (the lesson Markers and
// Referrer both learned). The fallback runs lazily inside PlacedInfoFor, which
// the exporter calls while it is already walking the cell's refs, so it costs no
// extra sweep and needs no kPostLoadGame hook.

#include "Palette.h"

#include "SceneExporter.h"
#include "log.h"

#include <algorithm>
#include <cctype>

namespace {
    std::vector<Palette::PlacedInfo> g_placed;
    std::uint32_t g_nextSeq = 1;

    // Same tolerance Markers/Referrer use to re-pair an orphan: our objects do
    // not wander once placed (frozen or settled), so 16 units is generous.
    constexpr float kReacquireDist2 = 16.f * 16.f;
}

namespace Palette {

    std::vector<PlacedInfo>& Placed() { return g_placed; }

    void RegisterPlaced(RE::TESObjectREFR* ref, const Slot& slot, bool noHavokSettle) {
        if (!ref) return;
        PlacedInfo p;
        p.seq = g_nextSeq++;
        p.handle = ref->GetHandle();
        p.name = slot.name;
        p.baseId = slot.baseId;
        p.position = ref->GetPosition();
        p.noHavokSettle = noHavokSettle;
        p.extra = slot.extra;   // .present = false unless `sc pl ed1` carried it
        SKSE::log::info("Palette: placed ref #{} registered ('{}'{}{})", p.seq, p.name,
            p.noHavokSettle ? ", noHavokSettle" : "",
            p.extra.present ? ", extra -> " + MintedEditorIdOf(p) : "");
        g_placed.push_back(std::move(p));
    }

    const PlacedInfo* PlacedInfoFor(RE::TESObjectREFR* ref) {
        if (!ref) return nullptr;
        const auto h = ref->GetHandle();
        for (auto& p : g_placed)
            if (p.handle == h) return &p;

        // Handle miss. Either this ref is not ours, or the row came back from a
        // co-save whose dynamic FormID did not survive a full restart. Only OUR
        // refs (no durable id) can be a candidate — re-bind by base + position.
        if (SceneExporter::ResolveDurableId(ref)) return nullptr;
        auto base = SceneExporter::ResolveDurableId(ref->GetBaseObject());
        if (!base) return nullptr;
        const auto pos = ref->GetPosition();
        for (auto& p : g_placed) {
            if (p.handle.get()) continue;          // alive but bound to a different ref
            if (p.baseId != *base) continue;
            const auto d = pos - p.position;
            if (d.x * d.x + d.y * d.y + d.z * d.z > kReacquireDist2) continue;
            p.handle = ref->GetHandle();           // re-acquired
            SKSE::log::info("Palette: re-acquired placed ref #{} ('{}') after a restart",
                p.seq, p.name);
            return &p;
        }
        return nullptr;
    }

    std::string MintedEditorIdOf(const PlacedInfo& p) {
        // The slot name is free-form ("Ebony Sword of Fire"); an EditorID is not.
        // Fold everything an EditorID can't hold to '_' and suffix the seq, so two
        // slots that sanitise alike still get distinct records. The seq rides the
        // co-save, so the id is stable across exports — a rebuild keeps pointing
        // at the same minted record. (Same shape as Referrer::EditorIdOf.)
        std::string out = "MFPal_";
        for (const char ch : p.name) {
            const auto uc = static_cast<unsigned char>(ch);
            out.push_back(std::isalnum(uc) ? ch : '_');
        }
        return out + "_" + std::to_string(p.seq);
    }

    void DropAllPlaced() { g_placed.clear(); }

    void OnPlacedRegistryRestored() {
        std::uint32_t hi = 0;
        std::size_t orphans = 0;
        for (const auto& p : g_placed) {
            hi = std::max(hi, p.seq);
            if (!p.handle.get()) ++orphans;
        }
        g_nextSeq = hi + 1;
        SKSE::log::info("Palette: placed-ref registry restored — {} row(s){}", g_placed.size(),
            orphans ? std::format(", {} awaiting re-acquire (base+position, at export)", orphans)
                    : std::string{});
    }

}  // namespace Palette
