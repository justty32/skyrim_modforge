# Quest markers — design

**Date:** 2026-06-13
**Goal:** Let a spec draw the three kinds of Skyrim markers it currently cannot: the dynamic
quest-objective compass/map arrow, a static XMarker anchor an objective can point at, and a
permanent world-map location marker (XMRK).

## Current state / gap

- `ObjectiveSpec` = `index` + `text` + `showStage`/`completeStage`. It builds a QOBJ that is **pure
  journal text** — no QSTA target — so the engine draws **no marker**. Aliases already exist
  (`QuestAliasSpec`), so the only missing layer is "objective → alias index" (QSTA).
- `PlacementSpec` already supports `editorId` + `persistent`, and `forced:<ref>` aliases resolve
  through `TryResolveRef`, which already accepts an in-spec placement editorId. So an XMarker anchor
  is *buildable today* with a raw placement — the gap is ergonomics (remembering the base formid and
  the persistent flag) only.
- There is **no XMRK / map-marker record support** at all.

## The three features

The three compose inside-out: **A** is the core (objective → QSTA → alias); **B** and **C** are two
kinds of persistent ref that **A** can point at. An objective can target ① a dynamically-filled
alias (NPC/location), ② a fixed XMarker anchor, or ③ a map marker.

### A. Objective → QSTA target (`ObjectiveSpec.targets`)

The QOBJ gets one QSTA per target. The compass/map arrow follows whatever the alias is **currently
filled with** — fill it with an actor to mark a person, a location/ref to mark a place. One objective
may carry several QSTA (vanilla "kill any of X/Y/Z").

Per the user's choice, v1 includes the QSTA flag and per-target CTDA conditions.

```csharp
// New on ObjectiveSpec:
public List<ObjectiveTargetSpec> Targets { get; set; } = new();

// One QSTA: the alias the arrow points at, plus optional gating.
public sealed class ObjectiveTargetSpec
{
    public string Alias { get; set; } = "";                    // alias NAME in the same quest → resolved to alias index
    public bool CompassIgnoresLocks { get; set; }              // QSTA flag 0x01 (CompassMarkerIgnoresLocks)
    public List<ConditionSpec> Conditions { get; set; } = new(); // per-target CTDA via the shared BuildCondition()
}
```

**Build:** in the quest pass, after aliases are created (so a name→index map exists), for each
objective target resolve `alias` → the quest alias index, build a `QuestTarget { Target = index,
Flags = CompassIgnoresLocks ? 0x01 : 0 }`, run each `ConditionSpec` through the shared
`BuildCondition()` into `QuestTarget.Conditions`, and add to `QuestObjective.Targets`.

**Validate:** every `target.alias` must name an alias defined on the same quest; condition functions
validated by the existing condition validator.

### B. XMarker anchor (`PlacementSpec.kind: "xmarker" | "xmarkerHeading"`)

A thin helper over the existing placement machinery. When `kind` is `xmarker`/`xmarkerHeading`:

- if `base` is empty, default it to the vanilla XMarker / XMarkerHeading static;
- force the placed ref **persistent** (a quest-target anchor must exist before its cell loads —
  otherwise the `forced:` alias resolves to a dropped temp ref). This closes the "forgot persistent"
  footgun.

Usage: place the anchor with an `editorId`, bind it with a `forced:<editorId>` alias, point an
objective `target` at that alias.

`kind` today only specials `"npc"` (forces ACHR); empty infers from base type. The two new values
slot in beside `"npc"` in the same dispatch with no change to existing behavior.

**Base formids to verify against the local Skyrim.esm at build time (do NOT hard-code unverified):**
XMarker `Skyrim.esm:0x0000003B`, XMarkerHeading `Skyrim.esm:0x00000034`. Confirm with the `find`
CLI before shipping the defaults.

### C. Map marker (`mapMarkers[]`)

A new top-level array. Each entry builds a `PlacedObject` (REFR) on the vanilla **MapMarker** static
base, carrying an XMRK `MapMarker` subrecord (name + type + flags). Independent of any quest, but —
being a persistent REFR with an editorId — it can itself be a `forced:` alias target, so it can
double as an objective target (C feeds A).

```csharp
public sealed class MapMarkerSpec
{
    public string EditorId { get; set; } = "";    // names the REFR so an alias / linked-ref can target it
    public string Name { get; set; } = "";        // map label (FULL inside XMRK)
    public string Worldspace { get; set; } = "";  // exterior worldspace ref; position is world-space
    public Vec3 Position { get; set; } = new();
    public string Type { get; set; } = "";        // MarkerType enum name: City|Town|Settlement|Cave|Camp|Fort|... (None if empty)
    public List<string> Flags { get; set; } = new(); // Visible | CanTravelTo | ShowAll
}
```

**Build:** resolve the worldspace → exterior cell at the position (reuse `ExteriorCell`/`PosToGrid`
as placements do), make a `PlacedObject` with base = MapMarker static, set `MapMarker = new
MapMarker { Name = name, Type = <enum>, Flags = <flags> }`, mark persistent (map markers persist),
set `EditorID`. Register the editorId in `formKeyByEd` so a `forced:` alias / linked ref can target
it.

**Base formid to verify:** MapMarker `Skyrim.esm:0x00000010`. Confirm with `find` before shipping.

**Validate:** `type` (if set) must be a known MarkerType; each flag a known MapMarker flag;
worldspace must resolve.

## Testing

All offline (no Skyrim.esm) where possible:

- A: build a tiny quest with an alias + an objective target → assert the built QOBJ has a QSTA with
  the right alias index, flag bit, and a condition.
- B: `kind:"xmarker"` placement with no `base` → assert base defaulted to the XMarker static and the
  ref is persistent; `forced:` alias → assert ForcedReference resolves to it.
- C: a `mapMarkers[]` entry → assert a PlacedObject on the MapMarker base with the right XMRK
  name/type/flags, persistent, editorId registered.
- Validate negatives: objective target naming a missing alias; bad MarkerType; bad flag.

Formid-dependent assertions (the exact `0x3B/0x34/0x10` base) are gated behind
`[Trait("Category","RequiresSkyrim")]` if they need the real master; the wiring/shape tests stay
offline.

## Out of scope (v1)

- No new alias *fill* kinds — markers reuse existing fills (`forced`/`findMatching`/`uniqueActor`/`fromEvent`).
- No map-marker icon/texture overrides beyond the type enum.
- No per-objective "marker radius"/data beyond QSTA flag + conditions.

## Maintenance chain (per CLAUDE.md)

Code (+ examples) → CODE_MAP.dialogue-quests.md (objective QSTA) + CODE_MAP.world.md (xmarker kind,
mapMarkers) → SPEC-dialogue-quests.md + SPEC-world.md → spec.schema.json. HTML only on request.
