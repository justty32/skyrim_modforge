#pragma once

// Captures — the eyedropper for DEFINITIONS (Idea #24 addendum, 2026-07-11).
//
// The Palette eyedropper captures a durable BASE so ModForge can re-PLACE copies
// (a reusable stamp). Captures is its sibling for content that has NO durable
// base to reference — a player-enchanted weapon, a home-brewed potion, or (a
// follow-up increment) a PROTEUS-cloned NPC. It reads the live form's SEMANTIC
// content (enchantment effects, alchemy effects) and records it so ModForge can
// MINT A FRESH authored record (a new ENCH + WEAP, an ALCH, …).
//
// This IS scene content, not a reusable library: an entry exports into
// `capturedItems[]` (a net-new scene.json section ModForge consumes), so the
// registry rides the co-save exactly like Eraser/Overrides/Markers. Entries hold
// only durable ids (MGEF/ENCH/base) + plain data — no ObjectRefHandles — so they
// restore across saves with no re-resolution.
//
// Scope note: increment ① is ITEMS (weapon/armour enchant + potion/ingredient
// effects). NPC appearance capture (the PROTEUS payoff) is increment ②; an
// actor target is routed out with a clear message, never a silent no-op.

#include <cstdint>
#include <string>
#include <vector>

namespace Captures {

    // One magic effect, shaped to ModForge's EffectSpec {MagicEffect, Magnitude,
    // Area, Duration}. `magicEffect` is a durable MGEF id "<plugin>:0xLOCALID"
    // (MGEFs are practically always authored, so this resolves).
    struct Effect {
        std::string magicEffect;
        float magnitude = 0.f;
        std::int32_t area = 0;
        std::int32_t duration = 0;
    };

    // Which scene.json shape the entry serialises to and which fields matter.
    enum class Kind : std::uint8_t { kWeapon, kArmor, kPotion, kIngredient };

    struct Entry {
        std::uint32_t seq = 0;
        Kind kind = Kind::kWeapon;
        std::string name;         // display name at capture time (row label)
        std::string base;         // origin base durable id (physical template); "" if runtime-only
        std::string enchantBase;  // durable ENCH id when the enchant itself is authored; else ""
        std::uint16_t enchantAmount = 0;  // enchant charge / amount (weapon/armour)
        std::vector<Effect> effects;      // enchant effects, or the alchemy effect list
    };

    // Outcome of a capture attempt — the console/panel word things by this.
    enum class Result { kNone, kCaptured, kNothing, kNotItem, kMarkerProxy, kIsNpc };

    Result CaptureCrosshair();  // `sc cap`   — the activatable crosshair target
    Result CaptureByRay();      // `sc cap r` — the look-ray target (statics/trees)

    [[nodiscard]] std::vector<Entry>& All();
    [[nodiscard]] const char* KindName(Kind k);  // "weapon"/"armor"/"potion"/"ingredient"

    bool Undo();                        // drop the most recent capture
    bool UndoEntry(std::uint32_t seq);  // per-row undo (panel button)
    void Clear();

    // Co-save plumbing (CoSave.cpp), mirroring Eraser/Overrides.
    void DropAll();             // clear the registry (no world touch on revert)
    void OnRegistryRestored();  // reseed the seq counter after entries are loaded

}  // namespace Captures
