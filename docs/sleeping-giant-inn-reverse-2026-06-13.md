# Reversing a vanilla interior cell into `placements[]` — RiverwoodSleepingGiantInn (2026-06-13)

Reference write-up for the "reverse a vanilla cell's object layout into a ModForge spec" workflow,
using the Sleeping Giant Inn in Riverwood as the worked example. This is the template for any future
"clone/relight/repopulate a vanilla cell" task.

## Target cell

| | |
|---|---|
| EditorID | `RiverwoodSleepingGiantInn` |
| FormID | `0x0133C6` (dec 78790) |
| Block / sub-block | block 0, sub 9 (`id%10=0`, `(id/10)%10=9` — Skyrim interior cells are grouped BY FormID) |
| Master | `Skyrim.esm` |
| Type | interior |

Confirm the FormID resolves to the cell name with `cellblk`:

```
dotnet run --project src/ModForge.Cli -- cellblk "<…>/Skyrim.esm" 0x0133C6
# 0x0133C6 (dec 78790)  block=0 sub=9  RiverwoodSleepingGiantInn  [id%10=0, (id/10)%10=9]
```

## Ref count

**480 placed refs total** in the cell child group:

| group | count |
|---|---|
| placed objects (REFR) | 477 |
| placed NPCs (ACHR) | 3 (`DelphineREF`, `OrgnarREF`, `EmbryRef`) |
| disabled-skipped (InitiallyDisabled flag) | 4 |

Of the 477 objects, **~57 are invisible markers** — `XMarker` (`0x000034`) and `XMarkerHeading`
(`0x00003B`) anchors used by the Bard-audience routine and the MQ106 / MQ201 / MQ203 main-quest
scenes (e.g. `MQ201DelphineWaitMarker`, `MQ203EsbernTableMarker`, `RiverwoodBardAudienceMarker1-5`).
The remaining **420 objects are the visible static / furniture / clutter layout** — walls, the bar
counter, tables, benches, chairs, beds, barrels, food, bottles, etc.

## The `cellrefs` diagnostic (new)

There was no command to dump a cell's placed objects (`cellblk` only prints the block location). Added
`cellrefs <in.esp> <0xFORMID>` in `src/ModForge.Cli/Diagnostics.Records.cs` (+ dispatch + help in
`Program.cs`). It follows the sanctioned **lazy-overlay** memory pattern:

- `SkyrimMod.CreateFromBinaryOverlay(...)` — the 250 MB master is NOT fully materialized.
- Walks `mod.Cells.Records → block.SubBlocks → sub.Cells`, and the instant `c.FormKey.ID == target`
  it processes ONLY that cell's `Persistent` + `Temporary` child lists and `return`s — it never
  enumerates every cell's children (that would materialize the whole master and get the process
  killed).
- Per ref it prints CSV: `kind,base,posX,posY,posZ,rotX,rotY,rotZ,scale,editorId`. `kind` is
  `objP`/`objT` (PlacedObject persistent/temporary) or `npcP`/`npcT` (PlacedNpc). Refs flagged
  InitiallyDisabled (`0x800`) are skipped. Localized Name is never resolved.

```
dotnet run --project src/ModForge.Cli -- cellrefs "<…>/Skyrim.esm" 0x0133C6 > sgi_refs.csv
```

`position` is **cell-local** for interiors (world coords for exteriors). `rotation` is printed in
**RADIANS** (as stored in the esm DATA subrecord). `scale` is the ref's float scale (default 1.0).

## Rotation unit conversion — esm RADIANS → ModForge DEGREES

This was flagged as the #1 risk. Verified in `src/ModForge.Core/Generator.Build.Placements.cs`:
the build does `Rotation = new P3Float(Deg2Rad(pl.Rotation.X), …)`, and
`Generator.Helpers.cs` defines `Deg2Rad(deg) => deg * pi / 180`.

→ **ModForge `placements[].rotation` is in DEGREES** and is converted to radians at build time. The
esm stores radians, so the reverse conversion is:

```
spec_degrees = esm_radians * 180 / pi
```

(e.g. `OrgnarServeInnMarkerRef` rotZ `1.570796` rad → `90.0°`; `RiverwoodInnDelphineRoomDoor` rotZ
`3.141593` rad → `180.0°`.)

## How `placements[]` targets a vanilla interior cell

Set the placement's `cell` to `"<master>:0xFORMID"` (here `"Skyrim.esm:0x0133C6"`). The build
(`VanillaCellOverride` in `Generator.Build.Placements.cs`) resolves the cell's context from a
link-cache over its master and creates a same-FormKey **additive override** in the new mod, copying
only the inline environment data via `CopyCellEnv` (NOT `GetOrAddAsOverride`, which would deep-copy
the localized Name and need BSA/load-order string lookup — unavailable headless). The vanilla
references are NOT re-listed; they still load from the master. **We only ADD our new refs.**

Caveats from the code:
- Only **interior** vanilla cells are supported for override (phase 2); an exterior cell is skipped.
- The override must land in the correct FormID-derived GRUP (block 0 / sub 9 here); `CopyCellEnv` +
  `InteriorSubFor(fk)` handle this — verify with `cellblk` on the built esp if in doubt.
- A placement with `kind:"npc"` (or an in-spec NPC_ base) becomes an ACHR; anything else a REFR.

## Base-object → role mapping (the high-count clusters)

The CSV gives base FormKeys verbatim (used as `Skyrim.esm:0x…`). The densest bases in this cell:

| base | count | role (by position cluster / vanilla convention) |
|---|---|---|
| `0x03133C` / `0x03133B` | 26 / 22 | inn wood-architecture pieces (wall/floor planks) |
| `0x0319E3` | 24 | clutter on tables (food/dishes cluster around the dining area) |
| `0x000034` / `0x00003B` | 19 / 7 | XMarkerHeading / XMarker — **invisible**, omitted |
| `0x065C97` | 16 | repeated clutter |
| `0x01F24A` / `0x01F248` | 14 / 12 | bench / chair furniture rows |
| `0x031941` | 11 | tankards / mugs on the bar |
| `0x064B3x` family | 1 each | the bar-counter food/drink set (around x≈-1150, y≈0) |

(The named persistent refs — `RiverwoodInnDelphineRoomDoor` `0x0C78A6`, `MQ201PlayerGearContainer`
`0x0A0DB5`, the bard markers — locate the back room, the player-gear chest, and the bard stage.)
Identifying every base by EditorID requires `find`, which enumerates the WHOLE master (memory-risky)
— not needed for a faithful reverse, since the FormKey IS the stable reference ModForge consumes.

## Gotchas

1. **No scale field in `PlacementSpec`.** ModForge places everything at scale 1.0; there is no per-
   placement scale. The CSV captures vanilla scales (e.g. `0x0F13C4` at 3.95, `0x0A0DB5` at 1.2), and
   the JSON records non-1.0 values in a `_scale_in_vanilla` note field only — they are NOT applied.
   If a future task needs scale, `PlacementSpec` must gain a `Scale` field wired into the `Placement`.
2. **Persistent vs Temporary.** `cellrefs` dumps both; the visible layout is in `Temporary`. The
   `Persistent` group here is almost all markers + the back-room door + the player-gear container.
   ModForge's build decides persistence itself (xmarker/linkedRefs/teleport/anchor) — for a plain
   static reverse, leaving placements as default (Temporary) matches vanilla.
3. **NPC overrides DUPLICATE.** Placing `DelphineREF`/`OrgnarREF`/`EmbryRef` into the vanilla cell
   override ADDS new ACHR copies on top of the still-loading vanilla actors. For a clutter-only test,
   drop the 3 `kind:"npc"` entries.
4. **Markers are invisible.** The 57 XMarker/XMarkerHeading anchors are quest-scene helpers, not
   layout — omitted from the visual reverse (re-include only if porting the MQ scenes).
5. **Disabled refs.** 4 refs carry the InitiallyDisabled flag (`0x800`) and are skipped — they aren't
   part of the visible layout.

## Produced spec

`examples/sleeping_giant_inn.json` — a **near-complete visible-layout reverse**: all **420**
non-marker static/furniture/clutter refs + the 3 unique NPCs (423 placements), targeting the vanilla
cell as an override. Rotations converted radians→degrees. `validate` passes clean:

```
dotnet run --project src/ModForge.Cli -- validate examples/sleeping_giant_inn.json
# valid: sleeping_giant_inn.json — no problems
```

Only `validate` (offline) was run, not `build` — building would resolve every base FormKey against
the master link cache, which is the memory-risky path to avoid in a reverse-engineering pass.
