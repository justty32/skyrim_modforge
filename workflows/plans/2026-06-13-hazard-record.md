# Hazard (HAZD) record Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a custom Hazard (HAZD) record — a radius effect that periodically applies a spell to actors in it — usable both as a spell-spawned ground effect (via the existing MagicEffect `SpawnHazard` archetype) and as a placed static trap (new `PlacedHazard`).

**Architecture:** A pass-1 record builder (`BuildHazards`, mirroring `BuildExplosions`) + a pass-2 ref wirer (`WireHazards`, using the shared `Resolve` helper). Spell-spawn reuses the existing MGEF archetype/association wiring (no new code). Placement adds a `PlacedHazard` branch to `BuildPlacements` keyed on the base being an in-spec `IHazardGetter` (or `kind:"hazard"`).

**Tech Stack:** C#/.NET 10, Mutagen.Bethesda.Skyrim 0.49, xUnit. All offline.

**Verified facts (this session):** `Hazard` has `Name`/`Model`/`ObjectBounds`/`Radius`/`Lifetime`/`TargetInterval`/`Limit`/`Spell`(`IFormLink<ISpellGetter>`)/`Light`/`Sound`/`ImpactDataSet`/`ImageSpaceModifier`/`Flags`(`Hazard.Flag`: AffectsPlayerOnly=1, InheritDurationFromSpawnSpell=2, AlignToImpactNormal=4, InheritRadiusFromSpawnSpell=8, DropToGround=16). `MagicEffectArchetype.TypeEnum.SpawnHazard` exists; `Generator.Build.Magic.cs` already wires `archetype`+`association`. `PlacedHazard(mod)` has `.Hazard`(`IFormLink<IHazardGetter>`)+`.Placement`. Shared helpers: `mod.Hazards.AddNew()`, `ParseFlags<T>(List<string>)`, `Resolve(string what, string refStr, Action<FormKey> set)` (BuildContext.cs:117, skips empty refs), `recordsByEd`, `TryResolveRef`. Build invocation in tests: `Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod`.

---

### Task 1: HazardSpec + BuildHazards + WireHazards

**Files:**
- Create: `src/ModForge.Core/Spec.Hazards.cs`
- Modify: `src/ModForge.Core/Spec.cs` (add `Hazards` list to `ModSpec`)
- Create: `src/ModForge.Core/Generator.Build.Hazards.cs`
- Modify: `src/ModForge.Core/Generator.Build.cs` (call both passes)
- Test: `tests/ModForge.Core.Tests/HazardTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class HazardTests
{
    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    [Fact]
    public void Hazard_record_builds_scalars_flags_and_wires_spell()
    {
        var spec = new ModSpec();
        spec.Spells.Add(new SpellSpec { EditorId = "MF_Burn", Name = "Burn" });
        spec.Hazards.Add(new HazardSpec
        {
            EditorId = "MF_FireHaz", Name = "Flames", Model = "Meshes/x.nif",
            Radius = 150f, Lifetime = 5f, TargetInterval = 1f, Limit = 3,
            Spell = "MF_Burn", Flags = { "DropToGround", "AffectsPlayerOnly" },
        });
        var mod = Build(spec);
        var h = mod.Hazards.Single(x => x.EditorID == "MF_FireHaz");
        Assert.Equal(150f, h.Radius);
        Assert.Equal(5f, h.Lifetime);
        Assert.Equal(1f, h.TargetInterval);
        Assert.Equal(3u, h.Limit);
        Assert.Equal("Meshes/x.nif", h.Model!.File);
        Assert.True(h.Flags.HasFlag(Hazard.Flag.DropToGround));
        Assert.True(h.Flags.HasFlag(Hazard.Flag.AffectsPlayerOnly));
        var spell = mod.Spells.Single(s => s.EditorID == "MF_Burn");
        Assert.Equal(spell.FormKey, h.Spell.FormKey);     // wired in pass 2
    }
}
```

- [ ] **Step 2: Run it — expect FAIL** (no `HazardSpec`/`Hazards`).

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~HazardTests"`
Expected: compile error — `HazardSpec` / `ModSpec.Hazards` don't exist.

- [ ] **Step 3: Add the spec type** — create `src/ModForge.Core/Spec.Hazards.cs`:

```csharp
using System.Collections.Generic;

namespace ModForge;

// A Hazard (HAZD): a radius effect that periodically applies `spell` to actors inside it (a fire/frost/
// poison patch). Use it two ways: (1) a magicEffects[] entry with archetype "SpawnHazard" + association
// = this editorId → a castable spell that drops it; (2) a placements[] entry whose base is this editorId
// → a placed static trap (PlacedHazard). `lifetime` 0 = inherit from the spawning spell / permanent;
// `targetInterval` = seconds between applications; `limit` 0 = unlimited instances.
public sealed class HazardSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Model { get; set; } = "";
    public float Radius { get; set; }
    public float Lifetime { get; set; }
    public float TargetInterval { get; set; } = 1f;
    public uint Limit { get; set; }
    public string Spell { get; set; } = "";
    public List<string> Flags { get; set; } = new();   // Hazard.Flag names
    public string Light { get; set; } = "";
    public string Sound { get; set; } = "";
    public string ImageSpaceModifier { get; set; } = "";
    public string ImpactDataSet { get; set; } = "";
}
```

Add to `ModSpec` in `Spec.cs` (next to the other record lists, e.g. after `Explosions`):
```csharp
    public List<HazardSpec> Hazards { get; set; } = new();
```

- [ ] **Step 4: Add the builder** — create `src/ModForge.Core/Generator.Build.Hazards.cs`:

```csharp
namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 1: Hazard (HAZD) scalar/model/flag fields. A radius effect applying `spell` every
        // `targetInterval`s to actors inside. FormLink refs (spell/light/sound/imad/impactDataSet) are
        // wired in pass 2 (WireHazards). Built before BuildFormKeyTable so a magicEffect `association`
        // and a placement `base` can resolve it by editorId. ---
        public void BuildHazards()
        {
            foreach (var hz in spec.Hazards)
            {
                var r = mod.Hazards.AddNew();
                r.EditorID = hz.EditorId;
                if (!string.IsNullOrEmpty(hz.Name)) r.Name = hz.Name;
                if (!string.IsNullOrWhiteSpace(hz.Model)) r.Model = new Mutagen.Bethesda.Skyrim.Model { File = hz.Model.Trim() };
                r.Radius = hz.Radius;
                r.Lifetime = hz.Lifetime;
                r.TargetInterval = hz.TargetInterval;
                r.Limit = hz.Limit;
                if (hz.Flags.Count > 0) r.Flags = ParseFlags<Mutagen.Bethesda.Skyrim.Hazard.Flag>(hz.Flags);
            }
        }

        // --- pass 2: Hazard FormLink refs (may point forward or at vanilla). ---
        public void WireHazards()
        {
            foreach (var hz in spec.Hazards)
            {
                if (!recordsByEd.TryGetValue(hz.EditorId, out var rec) || rec is not Mutagen.Bethesda.Skyrim.IHazard h) continue;
                Resolve($"hazard '{hz.EditorId}' spell",         hz.Spell,              fk => h.Spell.SetTo(fk));
                Resolve($"hazard '{hz.EditorId}' light",         hz.Light,              fk => h.Light.SetTo(fk));
                Resolve($"hazard '{hz.EditorId}' sound",         hz.Sound,              fk => h.Sound.SetTo(fk));
                Resolve($"hazard '{hz.EditorId}' impactDataSet", hz.ImpactDataSet,      fk => h.ImpactDataSet.SetTo(fk));
                Resolve($"hazard '{hz.EditorId}' imageSpaceModifier", hz.ImageSpaceModifier, fk => h.ImageSpaceModifier.SetTo(fk));
            }
        }
    }
}
```

(`ParseFlags` and `Resolve` are existing BuildContext helpers; `recordsByEd` maps editorId → built record. Confirm `IHazard.ImageSpaceModifier` is settable via `SetTo` — it's `IFormLinkNullable`, which has `SetTo(FormKey)`; if the compiler objects, use `h.ImageSpaceModifier.SetTo(fk)` exactly as written, matching how other nullable FormLinks are set in `WireMagicFxRefs`.)

- [ ] **Step 5: Wire both passes into `Generator.Build.cs`.**

After `ctx.BuildExplosions();` (line ~33) add:
```csharp
        ctx.BuildHazards();                        // Hazard (HAZD) — built before BuildFormKeyTable so MGEF association / placement base resolve it
```
After `ctx.WireMagicFxRefs();` (line ~89) add:
```csharp
        ctx.WireHazards();                         // HAZD spell/light/sound/imad/impactDataSet FormLinks
```

- [ ] **Step 6: Run the test — expect PASS.**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~HazardTests"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/ModForge.Core/Spec.Hazards.cs src/ModForge.Core/Spec.cs src/ModForge.Core/Generator.Build.Hazards.cs src/ModForge.Core/Generator.Build.cs tests/ModForge.Core.Tests/HazardTests.cs
git commit -m "feat(magic): Hazard (HAZD) record — scalar/model/flags + pass-2 spell/light/sound/imad wiring" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Spell-spawn (SpawnHazard archetype → hazard)

This reuses the existing MGEF archetype/association wiring — the only "new" thing is proving a HAZD is a valid `association` target. No production code beyond Task 1.

**Files:**
- Test: `tests/ModForge.Core.Tests/HazardTests.cs` (add a case)

- [ ] **Step 1: Write the failing test** (append to the class)

```csharp
    [Fact]
    public void Magic_effect_spawn_hazard_associates_the_hazard()
    {
        var spec = new ModSpec();
        spec.Hazards.Add(new HazardSpec { EditorId = "MF_Haz", Model = "Meshes/x.nif", Radius = 100f });
        spec.MagicEffects.Add(new MagicEffectSpec
        {
            EditorId = "MF_DropHaz", Name = "Drop Hazard",
            Archetype = "SpawnHazard", Association = "MF_Haz",
        });
        var mod = Build(spec);
        var haz = mod.Hazards.Single(h => h.EditorID == "MF_Haz");
        var mgef = mod.MagicEffects.Single(m => m.EditorID == "MF_DropHaz");
        Assert.Equal(MagicEffectArchetype.TypeEnum.SpawnHazard, mgef.Archetype.Type);
        Assert.Equal(haz.FormKey, mgef.Archetype.Association.FormKey);
    }
```

- [ ] **Step 2: Run it.**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~HazardTests"`
Expected: PASS already (Task 1 makes the HAZD resolvable; `Generator.Build.Magic.cs` wires the association). If it FAILS because the association is null, the MGEF must build before the HAZD FormKey is registered — verify `BuildHazards` runs before `BuildFormKeyTable` (it does, per Task 1 Step 5) and `WireMagicEffectRefs` runs in pass 2 (it does). No code change expected.

- [ ] **Step 3: Commit**

```bash
git add tests/ModForge.Core.Tests/HazardTests.cs
git commit -m "test(magic): SpawnHazard magic-effect associates a HAZD (spell-spawn path)" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: PlacedHazard placement

**Files:**
- Modify: `src/ModForge.Core/Generator.Build.Placements.cs`
- Test: `tests/ModForge.Core.Tests/HazardTests.cs` (add a case)

- [ ] **Step 1: Write the failing test** (append)

```csharp
    [Fact]
    public void Placement_with_a_hazard_base_makes_a_PlacedHazard()
    {
        var spec = new ModSpec();
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        spec.Hazards.Add(new HazardSpec { EditorId = "MF_Haz", Model = "Meshes/x.nif", Radius = 100f });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trap", Base = "MF_Haz", Cell = "Room",
            Position = new Vec3 { X = 1, Y = 2, Z = 3 },
        });
        var mod = Build(spec);
        var haz = mod.Hazards.Single(h => h.EditorID == "MF_Haz");
        var cell = mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(s => s.Cells).Single(c => c.EditorID == "Room");
        var placed = cell.Temporary.Concat(cell.Persistent).OfType<IPlacedHazardGetter>().Single(r => r.EditorID == "Trap");
        Assert.Equal(haz.FormKey, placed.Hazard.FormKey);
    }
```

- [ ] **Step 2: Run it — expect FAIL** (the base resolves but a `PlacedObject` is made, not `PlacedHazard`; the `OfType<IPlacedHazardGetter>()` finds nothing).

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~HazardTests"`

- [ ] **Step 3: Implement** in `Generator.Build.Placements.cs`. Find the placed-ref creation block (the `bool isNpc = ...` line and the `IPlaced placedRec; if (isNpc) {...} else {...}` that follows). Replace it with:

```csharp
                bool isNpc = pl.Kind.Equals("npc", StringComparison.OrdinalIgnoreCase)
                    || (string.IsNullOrEmpty(pl.Kind) && recordsByEd.TryGetValue(pl.Base, out var br) && br is INpc);
                bool isHazard = pl.Kind.Equals("hazard", StringComparison.OrdinalIgnoreCase)
                    || (string.IsNullOrEmpty(pl.Kind) && recordsByEd.TryGetValue(pl.Base, out var hr) && hr is IHazard);

                IPlaced placedRec;
                if (isHazard)  { var hz = new PlacedHazard(mod); hz.Hazard.SetTo(baseFk); hz.Placement = placement; placedRec = hz; }
                else if (isNpc) { var a = new PlacedNpc(mod); a.Base.SetTo(baseFk); a.Placement = placement; placedRec = a; }
                else            { var o = new PlacedObject(mod); o.Base.SetTo(baseFk); o.Placement = placement; placedRec = o; }
```

(Keep the existing LVLN-base warning above this block unchanged. The hazard branch wins over npc/object; `baseFk` already resolved `pl.Base` → the HAZD FormKey.)

- [ ] **Step 4: Run the test — expect PASS.**

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/Generator.Build.Placements.cs tests/ModForge.Core.Tests/HazardTests.cs
git commit -m "feat(world): place a hazard base as a PlacedHazard (static trap)" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Validation

**Files:**
- Modify: `src/ModForge.Core/Generator.Validate.cs` (register hazard editorIds + call ValidateHazards)
- Modify: `src/ModForge.Core/Generator.Validate.MagicFx.cs` (ValidateHazards)
- Test: `tests/ModForge.Core.Tests/HazardTests.cs` (add a case)

- [ ] **Step 1: Write the failing test** (append)

```csharp
    [Fact]
    public void Hazard_validation_flags_missing_spell_and_bad_flag()
    {
        var spec = new ModSpec();
        spec.Hazards.Add(new HazardSpec { EditorId = "MF_Bad", Model = "Meshes/x.nif", Flags = { "Glowing" } });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("MF_Bad") && p.Contains("Glowing"));
        Assert.Contains(problems, p => p.Contains("MF_Bad") && p.Contains("spell"));   // warn: no spell = no effect
    }
```

- [ ] **Step 2: Run it — expect FAIL.**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~HazardTests"`

- [ ] **Step 3: Register editorIds.** In `Generator.Validate.cs`, in the registration block (where `spec.Placements` / `spec.Globals` are `Reg`'d, ~line 119), add:
```csharp
            foreach (var hz in spec.Hazards) Reg(hz.EditorId, "hazard");
```
And in the orchestration list (after `ctx.ValidateExplosions();`, ~line 28) add:
```csharp
        ctx.ValidateHazards();
```

- [ ] **Step 4: Add `ValidateHazards`** to `Generator.Validate.MagicFx.cs` (inside the `ValidateContext` partial):

```csharp
        public void ValidateHazards()
        {
            foreach (var hz in spec.Hazards)
            {
                if (string.IsNullOrWhiteSpace(hz.Model))
                    Problems.Add($"hazard '{hz.EditorId}' has no model — it will be invisible (clone a vanilla hazard nif)");
                if (string.IsNullOrWhiteSpace(hz.Spell))
                    Problems.Add($"hazard '{hz.EditorId}' has no spell — it applies no effect (set `spell` to the SPEL it should apply)");
                else CheckRef(hz.Spell, $"hazard '{hz.EditorId}' spell");
                CheckRef(hz.Light, $"hazard '{hz.EditorId}' light");
                CheckRef(hz.Sound, $"hazard '{hz.EditorId}' sound");
                CheckRef(hz.ImageSpaceModifier, $"hazard '{hz.EditorId}' imageSpaceModifier");
                CheckRef(hz.ImpactDataSet, $"hazard '{hz.EditorId}' impactDataSet");
                foreach (var f in hz.Flags)
                    if (!System.Enum.TryParse<Mutagen.Bethesda.Skyrim.Hazard.Flag>(f, true, out _))
                        Problems.Add($"hazard '{hz.EditorId}' has unknown flag '{f}' (AffectsPlayerOnly | InheritDurationFromSpawnSpell | AlignToImpactNormal | InheritRadiusFromSpawnSpell | DropToGround)");
            }
        }
```

(`CheckRef` skips empty refs and validates in-spec editorId / external `<master>:0xFORMID`; it's the shared validator used everywhere.)

- [ ] **Step 5: Run the test — expect PASS** (both asserts).

- [ ] **Step 6: Commit**

```bash
git add src/ModForge.Core/Generator.Validate.cs src/ModForge.Core/Generator.Validate.MagicFx.cs tests/ModForge.Core.Tests/HazardTests.cs
git commit -m "feat(magic): validate hazards (model/spell warnings, ref + flag checks)" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Worked example

**Files:**
- Create: `examples/hazard.json`

- [ ] **Step 1: Write the example** — a HAZD used BOTH ways: a SpawnHazard spell that drops it, and a placed trap. Clone a vanilla hazard model path; the spell applies a fire-damage effect.

```jsonc
{
  "pluginName": "ModForgeHazard.esp",
  "esl": true,
  "cells": [ { "editorId": "MFHZ_Room", "name": "Hazard Test Room" } ],
  "magicEffects": [
    { "editorId": "MFHZ_BurnEffect", "name": "Burning", "archetype": "ValueModifier",
      "actorValue": "Health", "baseCost": 5 },
    { "editorId": "MFHZ_DropFire", "name": "Drop Fire", "archetype": "SpawnHazard", "association": "MFHZ_Fire" }
  ],
  "spells": [
    { "editorId": "MFHZ_BurnSpell", "name": "Burn", "effects": [ { "effect": "MFHZ_BurnEffect", "magnitude": 5, "duration": 1 } ] },
    { "editorId": "MFHZ_DropFireSpell", "name": "Conjure Fire Patch", "type": "Spell",
      "castType": "FireAndForget", "delivery": "TargetLocation",
      "effects": [ { "effect": "MFHZ_DropFire", "magnitude": 1, "duration": 0 } ] }
  ],
  "hazards": [
    { "editorId": "MFHZ_Fire", "name": "Flames",
      "model": "Meshes/Traps/PressurePlateFire/NorTrapFirePlateFX.nif",
      "radius": 150, "lifetime": 8, "targetInterval": 1, "limit": 0,
      "spell": "MFHZ_BurnSpell", "flags": ["DropToGround"], "sound": "Skyrim.esm:0x000F57E6" }
  ],
  "placements": [
    { "editorId": "MFHZ_Trap", "base": "MFHZ_Fire", "cell": "MFHZ_Room", "position": { "x": 0, "y": 0, "z": 0 } }
  ]
}
```

(Verify `magicEffects`/`spells` field names against `SPEC-magic.md` before finalizing — `effect`/`magnitude`/`duration`/`castType`/`delivery`/`actorValue`/`baseCost` are the existing spec fields; adjust to match. The `sound` `0x000F57E6` is the vanilla fire-trap sound decoded this session — confirm with `find Skyrim.esm` if unsure, or drop the `sound` line.)

- [ ] **Step 2: Validate**

Run: `dotnet run --project src/ModForge.Cli -- validate examples/hazard.json`
Expected: `no problems`.

- [ ] **Step 3: Build**

Run: `dotnet run --project src/ModForge.Cli -- build examples/hazard.json /tmp/mfhz.esp`
Expected: build succeeds; output mentions the hazard, the SpawnHazard MGEF, and the placement.

- [ ] **Step 4: Commit**

```bash
git add examples/hazard.json
git commit -m "examples: hazard — SpawnHazard spell that drops a fire patch + a placed trap" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Maintenance chain — CODE_MAP + SPEC + schema

**Files:**
- Modify: `docs/CODE_MAP.items-magic.md` (HazardSpec, BuildHazards/WireHazards, ValidateHazards, HazardTests)
- Modify: `docs/CODE_MAP.world.md` (PlacedHazard note on the placement row)
- Modify: `docs/SPEC-magic.md` (hazards section + SpawnHazard archetype usage)
- Modify: `docs/SPEC-world.md` (placing a hazard = PlacedHazard trap)
- Modify: `examples/spec.schema.json` (HazardSpec def + `hazards` array + `kind:"hazard"`)

- [ ] **Step 1: CODE_MAP.items-magic.md** — add rows for `Spec.Hazards.cs` (`HazardSpec`), `Generator.Build.Hazards.cs` (`BuildHazards` pass 1 + `WireHazards` pass 2), and the `ValidateHazards` check in `Generator.Validate.MagicFx.cs`; add `HazardTests.cs` to the Tests column. Note the spell-spawn reuses the MGEF `archetype:"SpawnHazard"`+`association` path.

- [ ] **Step 2: CODE_MAP.world.md** — on the `Generator.Build.Placements.cs` row, note that a `base` resolving to an in-spec HAZD (or `kind:"hazard"`) emits a `PlacedHazard`.

- [ ] **Step 3: SPEC-magic.md** — document `hazards[]`: every field, the two usage paths (SpawnHazard spell vs placement), the flag list, and that `lifetime 0` = inherit/permanent, `limit 0` = unlimited, `targetInterval` = seconds between applications. Cross-reference `examples/hazard.json`.

- [ ] **Step 4: SPEC-world.md** — under placements, note `base` may be a hazard editorId (or `kind:"hazard"`) → a placed static hazard trap (`PlacedHazard`).

- [ ] **Step 5: spec.schema.json** — add a `hazard` `$def` (editorId/name/model/radius/lifetime/targetInterval/limit/spell/flags[Hazard.Flag enum]/light/sound/imageSpaceModifier/impactDataSet), a `hazards` array on the root, and add `"hazard"` to the placement `kind` enum.

- [ ] **Step 6: Full offline regression**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"`
Expected: all green (prior 471 + the new Hazard tests).

- [ ] **Step 7: Commit**

```bash
git add docs/CODE_MAP.items-magic.md docs/CODE_MAP.world.md docs/SPEC-magic.md docs/SPEC-world.md examples/spec.schema.json
git commit -m "docs(hazard): CODE_MAP + SPEC + schema for the HAZD record + PlacedHazard" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Self-review notes

- **Spec coverage:** HazardSpec + BuildHazards/WireHazards (Task 1), spell-spawn via existing MGEF (Task 2), PlacedHazard placement (Task 3), validation (Task 4), example exercising both paths (Task 5), docs/schema (Task 6). All design sections covered.
- **Type consistency:** `HazardSpec.{EditorId,Name,Model,Radius,Lifetime,TargetInterval,Limit,Spell,Flags,Light,Sound,ImageSpaceModifier,ImpactDataSet}` used identically across Tasks 1/3/4/5; `Hazard.Flag`, `IHazard`, `PlacedHazard.Hazard`, `MagicEffectArchetype.TypeEnum.SpawnHazard` all verified against Mutagen 0.49 this session.
- **Placeholder scan:** none — every code step is concrete. The two "verify field names" notes (Task 1 Step 4 `ImageSpaceModifier.SetTo`; Task 5 magic/spell field names) are deliberate guards to match live signatures, not gaps.
- **Offline:** every task is offline (forms reference by editorId/FormKey; the placement test uses an in-spec interior cell; external refs resolve to FormKeys without a master). No `RequiresSkyrim` tests needed.
