#pragma once

// Preview — the browser's GHOST: the object you are about to place, standing in
// the world at your aim point, at real scale, in real light (user 2026-07-14).
//
// The Creation Kit gives you a little preview pane. We can do better: the game
// IS the preview pane. Pick an entry in the Browser page and it appears where
// you are looking; pick the next one and it swaps; press the place key and it
// becomes a real placement (through the SAME Palette::PlaceSlot path as `sc pl`,
// so the export contract is untouched — the ghost itself never exports).
//
// 🔴 THE ONE THING THAT CANNOT GO WRONG: a ghost must never reach the export.
// The exporter's whole discriminator is "dynamic ref = the player put it there"
// (SceneExporter.cpp), and a ghost is a dynamic ref — so without a gate it would
// ship as content. Two layers, and the second is the one that matters:
//
//   1. the live handle — cheap, exact, and gone the moment the session ends.
//   2. a SENTINEL on the ref itself (ExtraTextDisplayData, which the savegame
//      serializes along with every created ref). If the player quicksaves with a
//      ghost up and reloads that save tomorrow, our registry is empty but the
//      ghost is still standing there — and IsGhost() STILL recognises it,
//      because the evidence rides the ref, not our memory. SweepOrphans() then
//      deletes it on load. State that can be reconstructed from the world beats
//      state we have to remember.

#include <string>

namespace Preview {

    // Spawn (or swap to) the ghost for this base. `label` is what a commit names
    // the placement — the catalogue entry's display name.
    bool Show(RE::TESBoundObject* base, const std::string& label);
    void Clear();  // no-trace delete (disable + mark deleted), like the eraser's own-ref path

    [[nodiscard]] bool Active();
    [[nodiscard]] RE::TESBoundObject* Base();
    [[nodiscard]] const std::string& Label();

    // The export gate + every picker's gate. True for our live ghost AND for an
    // orphan ghost left in a savegame by an earlier session (see the header note).
    [[nodiscard]] bool IsGhost(RE::TESObjectREFR* ref);

    // Per frame (the panel's HUD element, which renders even with the panel
    // closed — that is the whole point: you close the panel, look around, and
    // the ghost follows your aim). Idempotent and cheap; a no-op with no ghost.
    void Update();

    // Follow the aim point (default on). Off = the ghost stays where it is, so
    // you can walk around it and look at it from the other side before placing.
    void SetFollow(bool on);
    [[nodiscard]] bool Follow();

    // Pose the ghost carries into the placement. Yaw in degrees (the panel's
    // unit), scale as a multiplier — both ride into Palette::PlaceSlot.
    void SetYaw(float degrees);
    [[nodiscard]] float Yaw();
    void SetScale(float scale);
    [[nodiscard]] float Scale();

    // Ghost -> real placement, at the ghost's exact pose (NOT re-aimed: what you
    // see is what you get). The ghost stays up, so a row of trees is one key,
    // pressed five times. Returns false when there is nothing to commit.
    bool Commit();

    // kPostLoadGame: delete any orphan ghost the loaded save is carrying. Cheap
    // (one cell walk) and it runs where Markers::AdoptOrphans already runs.
    std::size_t SweepOrphans();

    void DropState();  // co-save revert: forget the handle; the world is being replaced

}  // namespace Preview
