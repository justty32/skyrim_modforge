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

    [[nodiscard]] std::vector<Entry>& All();

    // True when `ref` is one of our proxies — by registry handle, or by base
    // (catches orphaned proxies from an earlier session after a reload).
    [[nodiscard]] bool IsProxy(RE::TESObjectREFR* ref);

    void Rename(std::uint32_t seq, const std::string& label);
    void SetKind(std::uint32_t seq, const std::string& kind);
    void Remove(std::uint32_t seq);  // destroys the proxy too — no trace

}  // namespace Markers
