# Hazard (HAZD) record — design

**Date:** 2026-06-13
**Goal:** Let a spec define a custom environmental Hazard (HAZD) — a fire/frost/poison patch that
periodically applies a spell to actors in radius — and use it two ways: dropped by a castable spell
(reusing the existing MagicEffect pipeline) and placed in the world as a static trap.

This is sub-feature ① of the "new records: Music + Hazard" backlog item; Music (MUSC/MUST) is a
separate later spec → plan → build.

## Verified facts (Mutagen 0.49 + Skyrim.esm, this session)

- `Hazard` record fields: `Name` (TranslatedString), `Model`, `ObjectBounds`, `Radius` (float),
  `Lifetime` (float, 0 = inherit/permanent), `TargetInterval` (float, seconds between applications),
  `Limit` (uint, 0 = unlimited), `Spell` (`IFormLink<ISpellGetter>` — the effect applied),
  `Light`/`Sound`/`ImpactDataSet` (FormLinks), `ImageSpaceModifier` (`IFormLinkNullable<IImageSpaceAdapterGetter>`),
  `ImageSpaceRadius` (float), `Flags` (`Hazard.Flag`: AffectsPlayerOnly=1, InheritDurationFromSpawnSpell=2,
  AlignToImpactNormal=4, InheritRadiusFromSpawnSpell=8, DropToGround=16).
- Vanilla example: `TrapFirePlateFXHaz06` — model `Meshes/Traps/PressurePlateFire/NorTrapFirePlateFX.nif`,
  radius 4.3, lifetime 0, interval 0.1, spell `0x109CEB`, sound `0xF57E6`.
- `MagicEffectArchetype.TypeEnum` includes `SpawnHazard`. The existing MGEF builder
  (`Generator.Build.Magic.cs`) already parses `archetype` (Type) and wires `association` in pass 2 —
  so a `magicEffects[]` entry with `archetype:"SpawnHazard"` + `association:"<hazardEditorId>"` spawns
  the hazard with NO new code beyond the HAZD record existing.
- `PlacedHazard` is a distinct placed-ref type with `Hazard` (`IFormLink<IHazardGetter>` → the base
  HAZD), `Placement`, `Scale`, `EnableParent`, `EncounterZone`, etc. — parallel to `PlacedObject` but
  with `.Hazard` instead of `.Base`.
- The smallest existing record-builder pattern to mirror: `Generator.Build.Globals.cs`.

## The record: `HazardSpec` (`hazards[]`)

```csharp
public sealed class HazardSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";              // optional display name (HAZD FULL)
    public string Model { get; set; } = "";             // .nif path (visual); clone a vanilla hazard nif
    public float Radius { get; set; }                   // effect radius
    public float Lifetime { get; set; }                 // seconds (0 = inherit from spawn spell / permanent)
    public float TargetInterval { get; set; } = 1f;     // seconds between applying `spell` to actors in radius
    public uint Limit { get; set; }                     // max simultaneous instances (0 = unlimited)
    public string Spell { get; set; } = "";             // ref → SPEL applied periodically (in-spec or vanilla)
    public List<string> Flags { get; set; } = new();    // Hazard.Flag names
    public string Light { get; set; } = "";             // optional ref → LIGT
    public string Sound { get; set; } = "";             // optional ref → SNDR
    public string ImageSpaceModifier { get; set; } = ""; // optional ref → IMAD
    public string ImpactDataSet { get; set; } = "";     // optional ref → IPDS
}
```

Add `public List<HazardSpec> Hazards { get; set; } = new();` to `ModSpec` (`Spec.cs`).

## Build

- **`Generator.Build.Hazards.cs` — `BuildHazards()` (pass 1):** create each `Hazard` record, set
  EditorID/Name/Model/Radius/Lifetime/TargetInterval/Limit + parse `Flags` (`Enum.TryParse<Hazard.Flag>`),
  register the editorId in `formKeyByEd` so spells/placements can reference it. Run in pass 1 (before
  `BuildFormKeyTable`) like `BuildGlobals`, so a `magicEffects[].association` and a `placements[].base`
  can resolve it by editorId.
- **`WireHazards()` (pass 2):** resolve the FormLink refs — `Spell` (→ `r.Spell.SetTo`), `Light`,
  `Sound`, `ImpactDataSet`, `ImageSpaceModifier` — via the shared ref resolver (may point forward or at
  vanilla). Called from `Generator.Build.cs` alongside the other Wire* passes.

## Two ways to use it

1. **Spell-spawn (reuses MGEF — no new wiring):** a `magicEffects[]` entry with
   `archetype: "SpawnHazard"` and `association: "<hazardEditorId>"`. The existing
   `Generator.Build.Magic.cs` resolves the association to the HAZD FormKey. Put that MGEF on a
   `spells[]` entry → a castable spell that drops the hazard on the ground.

2. **Placement (new `PlacedHazard` support):** in `Generator.Build.Placements.cs`, when a placement's
   resolved `base` is an in-spec `IHazardGetter` (or `kind: "hazard"` is set explicitly), create a
   `PlacedHazard` with `.Hazard.SetTo(baseFk)` instead of a `PlacedObject` (`.Base`). Everything else
   (cell anchoring, position/rotation, persistent flag, editorId registration) is unchanged — factor
   the placed-ref creation so the hazard branch slots in beside the existing npc/object branches.

## Validate (`Generator.Validate` — items-magic side)

- Register `hazards[]` editorIds (uniqueness) up front (like other records).
- Each hazard: `spell` ref resolves; **warn** if `spell` is empty (a hazard with no spell applies
  nothing — valid but pointless). `light`/`sound`/`imad`/`impactDataSet` refs resolve if set.
- **warn** if `model` is empty (a model-less hazard is invisible — see
  vanilla-nif-paths-must-be-verified). Flags must be known `Hazard.Flag` names.
- Placement validation already checks `base` resolves; a HAZD base is now valid.

## Testing (offline)

- `BuildHazards`: a `hazards[]` entry → assert the HAZD record has the right Radius/Lifetime/
  TargetInterval/Limit/Flags and EditorID; after wiring, `Spell` FormKey matches the referenced spell.
- Spell-spawn: a `magicEffects[]` with `archetype:"SpawnHazard"` + `association:"MF_Haz"` → assert the
  built MGEF archetype Type == SpawnHazard and Association FormKey == the hazard's FormKey.
- Placement: a `placements[]` with `base:"MF_Haz"` → assert a `PlacedHazard` (not PlacedObject) lands
  in the cell with `.Hazard` == the hazard FormKey.
- Validate negatives: hazard with empty spell warns; unknown flag is a problem; placement base that is
  a hazard is accepted.

All offline — hazards reference forms by editorId/FormKey, no master load needed (external refs resolve
to FormKeys; the placement test uses an in-spec interior cell).

## Out of scope (v1)

- Music (MUSC/MUST) — separate later sub-feature.
- Hazard `ObjectBounds` authoring (leave default / copy nothing) and `ImageSpaceRadius` — add only if
  a use surfaces (YAGNI).
- Hazard spawned by an Explosion — Explosion has no Hazard field in Mutagen; not a path.

## Maintenance chain (per CLAUDE.md)

Code (+ example) → `CODE_MAP.items-magic.md` (HazardSpec + BuildHazards/WireHazards rows, Tests) and
`CODE_MAP.world.md` (PlacedHazard note on the placement row) → `SPEC-magic.md` (hazards section, +
the SpawnHazard archetype usage) and `SPEC-world.md` (placing a hazard) → `spec.schema.json`. HTML on
request only.
