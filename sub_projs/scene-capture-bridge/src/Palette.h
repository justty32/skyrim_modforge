#pragma once

// Palette — the eyedropper (Idea #24 §E ①, plan P2 新增).
//
// Pick: capture the crosshair target's BASE + current rotation + scale into a
// named slot (the open palette — grab anything from any mod, no design-time
// catalogue). Place: spawn the selected slot's base at the aimed point and
// re-apply the captured pose. The spawned thing is an ordinary dynamic ref, so
// the vanilla diff exports it into `placements[]` unchanged — contract zero.
//
// PERSISTENT and save-agnostic (user request 2026-07-11): slots hold durable
// base ids, nothing savegame-bound, so the whole palette serializes to
// scene-capture-palette.json next to the export and loads back on startup —
// pick in one playthrough, place in another. A slot whose base no longer
// resolves (plugin removed from the load order) stays listed but unavailable.

#include <cstdint>
#include <string>
#include <vector>

namespace Palette {

    struct Slot {
        std::string name;
        std::string baseId;               // durable "<plugin>:0x…" (display + master warning)
        RE::TESBoundObject* base = nullptr;  // session pointer; null = unavailable
        RE::NiPoint3 angle;               // captured pose, radians
        float scale = 1.f;
        bool isActor = false;
        bool addsMaster = false;          // base not from the 5 base-game masters
    };

    bool PickCrosshair();   // F6 — the activatable crosshair target, old feel
    // Explicit physics-ray pick (panel button) for trees/non-activatable
    // statics. NOT a fallback of F6 — same no-silent-fallback rule as the
    // editor/eraser: the ray always hits some ref (walls/floors).
    bool PickByRay();
    bool PlaceSelected();   // spawn selected slot at the aimed point (feet fallback)

    void Load();  // kDataLoaded: read scene-capture-palette.json, re-resolve bases
                  // (writes happen automatically on pick/rename/remove)

    // Panel "load from file": read another palette json (by filename, resolved
    // next to scene-capture-palette.json) and APPEND its slots. Returns how
    // many were added; the merged set is then saved to the default store.
    std::size_t LoadFromFile(const std::string& filename);

    [[nodiscard]] std::vector<Slot>& All();
    [[nodiscard]] std::size_t SelectedIndex();
    void Select(std::size_t index);
    void Rename(std::size_t index, const std::string& name);
    void Remove(std::size_t index);

}  // namespace Palette
