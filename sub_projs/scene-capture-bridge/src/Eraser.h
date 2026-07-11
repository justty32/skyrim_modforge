#pragma once

// Eraser — mark existing placed refs for removal (Idea #24 §E ③ / plan P2).
//
// Marking an AUTHORED ref (vanilla or any mod) disables it on the spot as the
// visual feedback and records its durable id; Export writes the list into the
// spec's `removals[]`, which ModForge's landed BuildRemovals turns into an
// InitiallyDisabled+buried override — the standard, reversible "disable vanilla
// clutter" patch. Marking one of OUR OWN dynamic refs is true deletion: it is
// disabled and leaves no trace anywhere (user-decided semantics).
//
// State model (user-decided): session memory + an explicit adopt scan — no
// silent inference. ScanDisabled() only proposes CANDIDATES (already-disabled
// authored refs whose record is not InitiallyDisabled); each one must be
// confirmed by hand in the panel, so quest-disabled clutter is never adopted
// by accident.

#include <cstdint>
#include <string>
#include <unordered_set>
#include <vector>

namespace Eraser {

    struct Entry {
        std::string id;      // "Skyrim.esm:0x0D1991" — durable ref id
        std::string plugin;  // "Skyrim.esm"
        bool addsMaster;     // not one of the 5 base-game masters (CC counts as adding)
        std::string cellOrWs;  // durable anchor at mark time — panel cell filter
        RE::ObjectRefHandle handle;
    };

    // What the marking did — the panel and log word things by this.
    enum class MarkResult { kNone, kMarked, kOwnDeleted, kDuplicate, kMarkerProxy };

    MarkResult MarkCrosshair();  // F8 — the activatable crosshair target, old feel
    // Explicit physics-ray erase (panel button) for trees/non-activatable
    // statics. NOT a fallback of F8: the ray always hits some ref (walls),
    // so F8-on-empty must stay a no-op, not "erased the wall behind".
    MarkResult MarkByRay();

    [[nodiscard]] std::vector<Entry>& All();
    [[nodiscard]] const std::unordered_set<std::string>& MarkedIds();

    bool Undo();   // re-enable the most recent mark
    void Clear();  // re-enable everything

    // Candidates for adoption: authored + currently disabled + record NOT
    // InitiallyDisabled (i.e. someone disabled it at runtime — possibly a past
    // eraser session whose registry died with the DLL).
    struct Candidate {
        std::string id;
        std::string name;    // display name, for the human deciding
        bool addsMaster;
        std::string cellOrWs;  // scan cell's anchor — kept on adopt
        RE::ObjectRefHandle handle;
    };
    std::size_t ScanDisabled();                  // fills Candidates() from the player's cell
    [[nodiscard]] std::vector<Candidate>& Candidates();
    void AdoptCandidate(std::size_t index);      // move one candidate into the marked list
    void DismissCandidates();                    // drop the current scan results

    // Co-save plumbing (CoSave.cpp). DropAll clears the REGISTRY ONLY — no
    // Enable() calls, unlike Clear(): on a save-load revert the incoming save
    // carries its own disable states and the old world must not be touched.
    void DropAll();
    void OnRegistryRestored();  // rebuild the id set after entries are loaded

}  // namespace Eraser
