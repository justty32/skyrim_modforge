#pragma once

// Markers — the unified marker system (Idea #24 P1, the MVP editing modality).
//
// A marker is a named world coordinate: the player drops one (hotkey/spell),
// renames it in the panel, and Export writes every marker into the spec's
// `annotations[]` — advisory anchors an AI agent reads to author the real spec
// sections ("place a goat at marker 'goat'"). The visible in-world proxy ref
// is EDITOR CHROME, never content: ExportCell must exclude it from
// `placements[]` via IsProxy().
//
// Registry is session memory (same model as the eraser list). The proxy's
// display name is set to the label — display names persist in the SAVEGAME,
// so a future adopt-scan can recover markers WITH labels after a reload.

#include <cstdint>
#include <string>
#include <vector>

namespace Markers {

    struct Entry {
        std::uint32_t seq = 0;       // placement order; ordered kinds (navmesh) rely on it
        std::string label;
        std::string kind = "note";   // note | navmesh | mapMarker | vfx | tag | ...
        RE::NiPoint3 position;       // fixed at placement time (not the proxy's live pose)
        float angleZDeg = 0.f;       // player facing at placement, degrees
        std::string cellOrWs;        // durable id of the containing cell/worldspace
        bool isInterior = false;
        RE::ObjectRefHandle proxy;
    };

    // Place a marker at the player's feet (the navmesh-vision primitive:
    // "record where I stand"). Returns false when no proxy base resolves.
    bool PlaceAtPlayer();

    // Place a marker where the player is LOOKING: havok ray from eye level
    // along the facing direction (range 4096). Falls back to the feet when
    // nothing is hit — one key serves both, no extra scancode risk.
    bool PlaceAimed();

    // Adopt proxies that exist in the player's cell but not in the registry —
    // markers from a previous session survive in the SAVEGAME (dynamic refs +
    // display names persist), while this registry lives in the DLL. Label is
    // recovered from the proxy's display name. Returns how many were adopted.
    std::size_t AdoptOrphans();

    [[nodiscard]] std::vector<Entry>& All();

    // True when `ref` is one of our proxies — by registry handle, or by base
    // (catches orphaned proxies from an earlier session after a reload).
    [[nodiscard]] bool IsProxy(RE::TESObjectREFR* ref);

    void Rename(std::uint32_t seq, const std::string& label);
    void SetKind(std::uint32_t seq, const std::string& kind);
    void Remove(std::uint32_t seq);  // destroys the proxy too — no trace

    // After a game load, proxies from the pre-load session are gone (dynamic
    // refs live in the save, our registry lives in the DLL). Drop entries
    // whose proxy no longer resolves so the panel doesn't list ghosts.
    void PruneDeadProxies();

}  // namespace Markers
