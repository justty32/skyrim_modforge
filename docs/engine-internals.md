# Engine internals — the "why" behind ModForge's generation code

The non-obvious Skyrim/Mutagen mechanics that the generator (`src/ModForge.Cli/Build.cs`) has to
respect. This is the evergreen design knowledge distilled from the (now archived) iteration log;
for symptom→fix lookups see [lifelike/gotchas](lifelike/gotchas.md), for field-by-field spec docs
see [SPEC.md](SPEC.md).

## The core principle: an override does NOT inherit omitted subrecords

When you override a vanilla record (same FormKey), the engine does **not** merge your sparse
override onto the master — it takes your record as-authored and **defaults every field you left
out**. This single fact is behind most of the cell/worldspace bugs:

- Drop a worldspace's `LandDefaults` → `DefaultWaterHeight` resets from Tamriel's real `-14000`
  to `0` → all terrain below sea level floods ("the whole world is underwater").
- Drop an interior cell's `LightingTemplate` → pitch-black room.

So overrides must **re-state the inline environment data**. `Build` does this via `CopyCellEnv`
(water height/type/textures, lighting + template, regions, imagespace, music, acoustic space,
encounter zone, location, ownership, sky/weather) and `CopyWorldspaceEnv` (land/water defaults,
water forms, climate, map, bounds, parent, lighting). Both deliberately **skip** the localized
Name and the giant child structures (the cell/worldspace block trees — we build our own; vanilla
refs stay in the master and aren't re-stated, so no bloat and no conflict).

> `WaterHeight = FLT_MAX` on a cell is **not** a bug — it's a "use the worldspace default" sentinel.

## The localized-string landmine (headless on Linux)

Skyrim.esm is **localized**: `TranslatedString` fields (Name / Description / BookText) are string
indices whose text lives in `.STRINGS` inside a BSA. Resolving them needs the game's
plugins.txt / load-order archive listing — **absent when running headless on Linux**.

Any operation that touches those strings throws *"Could not determine plugin listings path"*:
- `DeepCopyIn` of a vanilla record → pass a `TranslationMask { Name=false, Description=false,
  BookText=false }` (we override those anyway).
- `GetOrAddAsOverride` on a cell → instead build a **manual same-FormKey override**
  (`new Cell(vanillaFk, SkyrimRelease)`), copy only the inline fields, and leave Name/Lighting
  null so they're inherited from the master.
- `find`'s Name resolution → resolve best-effort, stop on the first failure, search EditorID only.

EditorID and FormID are stored inline and are always readable — that's why every `find`/`*diag`
keys off EditorID, never the display name.

## Cell GRUP placement is keyed by FormID/grid

### Interior cell GRUP formula

Skyrim nests interior cells `CellBlock(type 2) → CellSubBlock(type 3) → Cell`, and groups them
**by FormID**:

```
block = id % 10
sub   = (id / 10) % 10          # decimal, 24-bit ID
```

(Verified by walking Skyrim.esm: WhiterunBanneredMare `0x01605E` = dec 90206 → block 6, sub 0.)
**Critical for overrides:** a vanilla-cell override placed in the wrong block GRUP is never matched
against the master cell, so the engine **silently ignores it** (placed objects + lighting don't
apply). Confirm with `cellblk`.

### Exterior cell grid → GRUP

Exterior cells nest `WorldspaceBlock(type 4, /32 grid) → WorldspaceSubBlock(type 5, /8 grid) →
Cell(grid x,y)`:

```
cellGrid = floor(worldPos / 4096)        # CellSize = 4096
block    = FloorDiv(cellGrid, 32)
sub      = FloorDiv(cellGrid, 8)
```

The division **must floor toward −∞**, not truncate like C#'s `/` (e.g. `-41 / 8 == -5` in C#, but
the floor is `-6`). Negative coordinates land in the wrong GRUP otherwise. (Verified against
Tamriel: cell (7,−41) → block (0,−2), sub (0,−6).)

### LAND record gotchas (flat terrain generation)

Three bugs confirmed in-game (2026-06-03) when generating flat LAND records:

1. **`Landscape.Flags` must include `VertexNormalsHeightMap` (0x01).** Without this DATA flag the
   engine skips the entire VHGT/VNML payload — the cell has no terrain collision and the player
   falls through.

2. **Z=0 ≈ Skyrim's sea level.** VHGT `Offset=0` → terrain at Z=0 = ocean surface. Use
   `Offset = height / 8` (e.g. height=4000 → Offset=500 → Z=4000, safely above water).

3. **ESL plugins cannot contain LAND records.** Skyrim's engine silently ignores terrain data
   loaded from ESL (light) plugins — the exterior terrain loading path only reads from full ESP/ESM.
   Specs with `cells` must use `"esl": false`. The `validate` command enforces this.

4. **Exterior NAVM uses `WorldspaceNavmeshParent`, not `CellNavmeshParent`.** Using the wrong
   parent type CTDs the engine with `PathingStreamMasterFileRead` + garbage vertex count (the
   engine reads the vertex count from the wrong binary offset). Rule: exterior cells →
   `WorldspaceNavmeshParent { Parent = worldspace.FormKey }`; interior cells →
   `CellNavmeshParent { Parent = cell.FormKey }`. Same rule applies to the NAVI
   `NavigationMapInfo.Parent`: exterior → `NavigationMapInfoWorldParent { ParentWorldspace = ws.FormKey }`;
   interior → `NavigationMapInfoCellParent { ParentCell = cell.FormKey }`.
   Either parent set to null also CTDs (`NullReferenceException` on write from Mutagen).

5. **NavmeshGrid format:** `[uint32 triCount][ushort idx0]...[ushort idxN]` per grid sub-cell in
   row-major order. `GridDivisor` = N means an N×N grid; `MaxDistanceX/Y` = cellWidth/N (game
   units per sub-cell). For a standalone 2-triangle flat cell: `GridDivisor=1`, `MaxDistance=4096`,
   grid bytes = `02 00 00 00 00 00 01 00` (8 bytes, one cell containing both triangles).

## AI Packages are template-driven

Every concrete `Package` references a vanilla **procedure template** via `PackageTemplate`
(`IFormLink<IPackageGetter>`), and its `Data` is an `IDictionary<sbyte, APackageData>` keyed by the
template's **named slot indices**. Concrete packages have `Type = Package`; the templates themselves
have `Type = PackageTemplate` (never author the latter). Discover any template's slot schema with
`packagediag <Skyrim.esm> <templateFormId>`; find concrete examples with `pkgsbytemplate`.

A few slot subtleties that bite:
- `LocationFallback`'s binary shape is chosen by its **`Type` enum, not the C# class** —
  `new LocationFallback()` with `Type = 0` silently writes as a `LocationTarget`. Always set
  `Type = NearSelf` (anchors at the actor's current position; needs no external link).
  Never `NearEditorLocation` — it needs a CK-set Editor Location that Mutagen NPCs lack.
- UseMagic slots 0/1 are inherited `APackageData` placeholders — leave them untouched (all 46
  vanilla concrete UseMagic packages do).

### PACK Data slot maps

| Template | Slot map (index → meaning, vanilla default) |
|---|---|
| **Sandbox** `0x01C254` | 0 Location · 1 AllowEating · 3 AllowSleeping · 4 AllowConversation · 5 AllowIdleMarkers · 6 AllowSitting · 7 AllowWandering · 14 UnlockOnArrival · 25 PreferredPathOnly · 27 RideHorseIfPossible · 29 Energy · 31 AllowSpecialFurniture |
| **Travel** `0x016FAA` | 0 Place (Location) · 2 RideHorse · 4 PreferPath |
| **Patrol** `0x017723` | 0 Start (SingleRef) · 1 Radius (150) · 2 Repeatable · 4 StartAtNearest · 6 RideHorse · 8 StaticPathing |
| **Follow** `0x019B2C` | 0 Target (SingleRef → player) · 1 MinRadius (128) · 2 MaxRadius (256) · 4 Accompany · 6 RideHorse · 8 NeedLOS |
| **Escort** `0x023B73` | 11 Target (SingleRef → player) · 3 Destination (Location) · 2 NumFollowers (1) · 4 WaitDistance (512) · 5 FollowerMin (120) · 6 FollowerMax (256) · 13 RideHorse · 15 PreferPath · 17 RunIfBehind (500) |

### Patrol route topology lives in placed-ref linked references

The route is **not** in the package — each marker REFR has a Linked Ref (null keyword) to the next;
loop by linking the last back to the first. `LinkedReferences` lives on `IPlacedObject` /
`IPlacedNpc` **separately** (no shared settable interface — cast to the concrete type). Any
linked-ref source, and any placement a package's deferred anchor points at, must be forced
**Persistent** — the engine can drop a temporary ref that something else anchors to.

### Cross-cell Travel is a content gate, not a records gate

A Travel package byte-identical to vanilla is silently rejected at door teleports unless the NPC
has a **citizen identity** (`crimeFaction` + town-faction membership + `unique: true`). Honest
caveat: those three were added together; which one is individually load-bearing is unproven
(hypothesis: CrimeFaction primary, Unique helps the engine track AI state across cell transitions).

## Magic effect timing

- An **instant** effect (duration 0) must use `["NoDuration","NoArea"]` and **no `Recover`** —
  `Recover` reverts the value when the effect ends, which for an instant effect is *immediately*,
  netting zero (the "heal that doesn't heal" bug).
- `Recover` is correct only for a **timed** fortify effect (e.g. +50 Health for 60s).
- Keep `baseCost` low: autocalc multiplies baseCost × magnitude, so a high baseCost yields an
  absurd magicka cost.
- A combat SPEL needs an `equipType` (EitherHand `0x013F44`) or the NPC can't equip it to a hand
  and silently never casts.

## Cloning a vanilla model

A record with no `.nif` sits fine in inventory but **crashes** on any interaction that attaches a
model to the scene (weapon equip, book 3D-read); `additem`/drink are safe (no model load). Give
weapons/books/misc/potions a `template` ref; `Build` does `DeepCopyIn` (with the localized-string
mask off), which **preserves your own FormKey** (the record stays in your plugin; the template's
sub-forms become FormLinks into its master), then overrides identity/stats. For potions it clears
the cloned `Effects` first so spec effects don't stack with the template's.

## Custom dialogue surfacing

Two flags or the topic never appears: the host **Quest** needs `StartGameEnabled` (+ a `Priority`
byte that orders competing dialogue) or it stays dormant and its dialogue never loads; the
**DialogBranch** needs `Flag.TopLevel` or the topic is a sub-branch, not a menu option. Leave the
INFO's `ResponseData` null (so it uses your own Responses) and `Prompt` null — the menu line comes
from `topic.Name`; a hardcoded Prompt mislabels the menu.

## Perk entry points carry a hidden tab-count byte

An entry-point perk effect (`PerkEntryPointModifyValue`, e.g. ModAttackDamage ×1.2) has a
`PerkConditionTabCount` byte (the 3rd byte of the PERK entry-point `DATA` subrecord). It is the
entry point's **intrinsic number of condition tabs** — the attacker / target / weapon contexts the
function evaluates — **not** how many conditions you author. The engine sizes a per-tab condition
array from it; a `PRKC` condition tab authored on index 0 while the count is **0** overflows that
array, corrupts a pointer, and **hard-CTDs during "Loading Files"** (an access violation in the
TESForm lookup hash map on a garbage FormID).

This is a pure **"Mutagen-tolerant, engine-fatal"** binary bug: Mutagen reads back the real
`Conditions` list and ignores the count byte, so `dump` / round-trip / link-resolution / ESL-header
all look clean — only the runtime parser crashes. It hides until a second plugin shifts the memory
layout, then surfaces as a "two mods crash together" report (root-caused 2026-05-31 from a
CrashLoggerSSE log; see [lifelike/gotchas](lifelike/gotchas.md)).

The count is fixed per entry point and is always `1`/`2`/`3`, never `0` — and always ≥ the number of
PRKC tabs present (vanilla freely has e.g. count 3 with one or zero tabs). `Build` sets it from a
table extracted from Skyrim.esm's 375 PERK records (`ModAttackDamage`/`ModSpellMagnitude`/
`CalculateWeaponDamage` = 3; `ModArmorRating`/`ModBuyPrices` = 2; `ModSkillUse`/`ModFallingDamage` =
1; unlisted → 2). Regenerate the table by scanning vanilla: read every `IAPerkEntryPointEffectGetter`
and group `EntryPoint → PerkConditionTabCount`.

## Mutagen API traps

- `AddNew()` needs `using Mutagen.Bethesda;`.
- Write with `BinaryWriteParameters { ModKey = ModKeyOption.NoCheck }` when the output filename
  differs from the mod's ModKey.
- Type surprises: `DialogResponse.ResponseNumber` is `byte`; `PackageDataInt.Data` is `uint`;
  `LeveledItem.ChanceNone` is a `Noggog.Percent` (0–100); cell grid is `Noggog.P2Int`; rotations are
  authored in degrees but stored in radians.
- External refs (`<master>:0xFORMID`) auto-add the master on write (`MastersListContent = Iterate`);
  mask the master-index byte with `& 0x00FFFFFF` when constructing the FormKey.
- API discovery: `ilspycmd -t <Type> ~/.nuget/packages/mutagen.bethesda.*/0.53.1/lib/net9.0/*.dll`.
