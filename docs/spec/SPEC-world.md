# ModForge spec — cells, placements & navmesh

← [index](SPEC-index.md) · lighting → [SPEC-lighting](SPEC-lighting.md) · in-world macros →
[SPEC-world-macros](SPEC-world-macros.md) · exterior worlds, lists, spawns & vendors →
[SPEC-worldspaces](SPEC-worldspaces.md)

Interior cells, object placement (interior & exterior), and the navmesh mechanics that keep NPCs
walking correctly. Custom light sources and interior/exterior lighting (LGTM/IMGS/DALC) moved to
[SPEC-lighting](SPEC-lighting.md); the high-level population macros (skill trees, settlements,
living-world NPCs) moved to [SPEC-world-macros](SPEC-world-macros.md). For exterior worldspaces,
leveled lists, encounter zones and vendors see [SPEC-worldspaces](SPEC-worldspaces.md).

### cells & placements — putting things in the world
```jsonc
"cells": [
  { "editorId": "MF_TestRoom", "name": "ModForge Test Room",     // a new interior cell
    "template": "Skyrim.esm:0x0165A8" }                          //   copy lighting from Breezehome (else BLACK)
],
"placements": [
  { "base": "MF_Smith", "cell": "MF_TestRoom",                   // an in-spec NPC ...
    "position": { "x": 0, "y": 0, "z": 0 },
    "rotation": { "x": 0, "y": 0, "z": 0 } },                    //   rotation in degrees
  { "base": "MF_Chest", "cell": "Skyrim.esm:0x01605E",          // ... into a VANILLA INTERIOR cell
    "position": { "x": 100, "y": 0, "z": 0 } },                  //   (Skyrim.esm WhiterunBanneredMare)
  { "base": "MF_Coin", "worldspace": "Skyrim.esm:0x00003C",     // ... into the OPEN WORLD (Tamriel);
    "position": { "x": 22528, "y": 22528, "z": 200 } }           //   position is WORLD coords
]
```
- A `placement` targets **either** an interior `cell` **or** an exterior `worldspace` (set one):
  - **interior** — `cell` is a new in-spec interior cell's `editorId`, **or** an external/vanilla
    interior cell `"<master>:0xFORMID"` (find with `find <Skyrim.esm> <name> Cell`). A new cell
    with no `template` renders **pitch-black** and has **no floor** (you fall into the void): set
    the cell's `template` to a vanilla interior (copies its lighting) and place a floor static in
    it. `position` is local to the cell.
  - **exterior** — `worldspace` is a worldspace ref `"<master>:0xFORMID"` (Tamriel =
    `Skyrim.esm:0x00003C`; find with `find <Skyrim.esm> <name> Worldspace`). `position` is the
    **world** position; the exterior cell at `floor(x/4096), floor(y/4096)` is found in the master
    and overridden to add your ref. If that grid has no master cell, a new exterior cell is made
    there (structural only — not in-game verified). `worldspace` wins if both it and `cell` are set.
- `base` is a *ref* (in-spec or external); NPCs become `PlacedNpc`, anything else `PlacedObject`
  (`kind` overrides the guess). `rotation` is **degrees**. `persistent: true` puts it in the
  cell's persistent list (needed if a quest/script references it).
- **Placing a hazard:** when `base` is an in-spec `hazards[]` editorId (or `kind: "hazard"`), the ref
  is a `PlacedHazard` — a static environmental trap (fire/frost/poison patch). See the HAZD record in
  `SPEC-magic.md § hazards`.
- **`kind: "xmarker"` / `"xmarkerHeading"`** — a helper for placing an **invisible anchor**. With an
  empty `base` it defaults to the vanilla XMarker (`Skyrim.esm:0x0000003B`) / XMarkerHeading
  (`0x00000034`) static, and the ref is **forced persistent** (a quest-target anchor must persist or a
  `forced:` alias resolves to a dropped temp ref). Give it an `editorId`, bind it with a
  `forced:<editorId>` alias, and point an `objectives[].targets[]` at that alias to put a quest marker
  on a fixed spot that has no NPC.
- **Vanilla placement** (interior cell or exterior worldspace) overrides the cell/worldspace to
  *add* your reference (vanilla contents are untouched — they come from the master). Needs the
  game's `Data` folder — set `MODFORGE_SKYRIM_DATA` if it isn't at the default Steam path. (Placing
  into a vanilla worldspace also additively carries its persistent cell, so vanilla map markers and
  the world map stay intact.)

#### placement extra fields
```jsonc
"placements": [
  { "base": "MF_GoldCoins", "cell": "MF_Room",
    "position": { "x": 0, "y": 50, "z": 80 },
    "count": 50 },                                      // XCNT: 50-coin stack

  { "base": "MF_LockedChest", "cell": "MF_Room",
    "position": { "x": 200, "y": 0, "z": 0 },
    "lock": { "level": "master" },                      // XLOC: master-locked
    "ownership": { "owner": "MF_BanditFaction" } },     // XOWN: belongs to this faction

  { "base": "MF_Trophy", "cell": "MF_Room",
    "position": { "x": -100, "y": 0, "z": 100 },
    "scale": 1.5 },                                     // XSCL: 1.5× size

  { "base": "MF_SecretDoor", "cell": "MF_Room",        // hidden until quest stage fires
    "editorId": "MF_SecretDoorRef",
    "initiallyDisabled": true,                          // invisible + non-collidable
    "enableParent": {                                   // XESP: follows quest marker
      "ref": "MF_QuestTrigger",
      "flag": "SetEnable" } }                           //   appears when trigger is enabled
]
```
- **`scale`** (XSCL): uniform scale multiplier. `1.0` = default (subrecord omitted). Valid for
  statics, furniture, lights; actors ignore it in-game. Must be > 0.
- **`initiallyDisabled`** (record flag `0x800`): the ref exists in the cell but is invisible and
  non-collidable until explicitly enabled (via script, quest stage, or `enableParent`). Common
  pattern: hidden object + `enableParent` pointing at a quest-trigger XMarker.
- **`enableParent`** (XESP): this ref's enabled state follows another placed ref (`ref` =
  placement editorId, a `references[]` label, or external ref). Resolved after every
  `placements[]` entry and `references[]` label exists, so `ref` may point at a placement
  declared earlier OR later in the list — order doesn't matter.
  - `flag`: `SetEnable` (I enable when my parent enables — default), `SetDisable` (inverted),
    `PopIn` (appear without the fade-in flash).
- **`lock`** (XLOC): lock a door or container (`PlacedObject` only).
  - `level`: `novice` | `apprentice` | `adept` | `expert` | `master` | `requiresKey` |
    `inaccessible`, or a raw byte value as a string (e.g. `"50"`).
  - `key` (optional): item ref that bypasses the lock.
- **`ownership`** (XOWN): who owns this object — picking it up counts as theft.
  - `owner`: a FACT or NPC ref.
  - `rank` (optional, int ≥ 0): faction rank required (ignored for NPC owners; `0` = any member).
- **`count`** (XCNT): item stack count for a placed item (e.g. 50 gold coins). `0` = single
  instance (subrecord omitted). Not meaningful for actors or statics.

### navmesh — why NPCs walk into your house (and how to stop them)

Skyrim NPCs move **only** on the navmesh. Sandbox, travel, follow, patrol and combat all path through
it; an actor with no triangle under its feet does literally nothing, forever, with **no error message
anywhere**. And a vanilla navmesh doesn't know about the house you just placed on top of it — so NPCs
walk straight through your walls. Two knobs address this.

**1. `navCuts[]` — switch vanilla navmesh OFF inside a box, at runtime.**

This is Bethesda's own mechanism (`HearthFires.esm` uses it **1003 times** — it's how building a house
carves the navmesh): one placed ref on the engine's hardcoded `CollisionMarker` base, on collision
layer **49 (L_NAVCUT)**, carrying a box primitive. No NAVM edit, no NAVI edit, no NIF, no navmesh
conflict with other mods.

```jsonc
"navCuts": [
  { "editorId": "MF_CutUnderHouse",
    "worldspace": "Skyrim.esm:0x00003C",
    "position": { "x": 100, "y": 200, "z": -3510 },   // the CENTRE of the box, in all three axes
    "size":     { "x": 520, "y": 140, "z": 220 },     // the FULL size (w × d × h), NOT half-extents
    "rotationZ": 45,                                   // degrees
    "padding": 32 },                                   // grows the box outward on every side

  { "placement": "MF_MyHouse" }    // or: just wrap that placement's own OBND footprint
]
```

- **`size` is the full box size**, written straight into XPRM `Bounds`. Verified against vanilla:
  HearthFires' `00410D` box is 116×52.8×46.9 around a chest whose OBND is 96×49×48.
- **Always keep some `padding`.** The engine compares **actors as zero-volume points** against the
  volume, so a box that stops exactly at the wall leaves a seam NPCs squeeze through. The default 32
  ≈ half an actor's width. (Z is padded too, so the box straddles the navmesh plane rather than
  resting on it.)
- Three more engine limits worth knowing: a navcut only applies **in the cell the player is in**; it
  only affects paths **started after** it switched on (an NPC already walking through finishes the
  walk); and the `CollisionMarker` base is invisible and non-blocking, so the volume itself never
  gets in anyone's way.
- 🔴 **The trap:** the `Obstacle` record flag on its own does **nothing**. The engine gates navmesh
  cutting on the **collision layer**, and only six of the 55 vanilla layers cut navmesh — L_NAVCUT
  among them, **L_STATIC (what houses, walls and rocks collide on) not**. So "clone a vanilla static
  and set the Obstacle flag" cannot work, no matter how right it looks.

**2. `placements[].navCut` — per-placement control of the automatic cut.**

By default a placement that is **big enough to block a path** (its base OBND clears
`navmesh.minFootprint` and `minHeight`) **and actually covers vanilla navmesh** gets a navcut box for
free, sized from its own footprint. Override it per placement:

```jsonc
"navCut": false                                      // never cut this one — scenery NPCs may walk through
"navCut": true                                       // cut it even though it's under the size thresholds
"navCut": { "size": {…}, "offset": {…}, "padding": 48 }   // hand-tuned box
```

Tune the whole thing with the top-level `navmesh` object: `autoNavCuts`, `minFootprint` (10000 units²
— a chair is 3600, so clutter is never cut), `minHeight` (100), `padding` (32), `warnings` (see
below), `warnEmptyCells`.

> `autoNavCuts` currently defaults to **`false`**, and you must opt in. The design is "automatic, with
> an opt-out", but the default stays off until the in-game spike confirms an L_NAVCUT volume really
> does divert an NPC — turning it on before that would inject unproven records into every existing
> spec's output. Set `"navmesh": {"autoNavCuts": true}` to use it today.

**3. The build warnings.** `build` now reads the vanilla navmesh and tells you what the game never
will:

```
! navmesh: NPC 'MyMerchant' is off the navmesh — the nearest walkable triangle is 420 units away.
  It will NOT move …
! navmesh: NPC 'MyGuard' is 560 units ABOVE the navmesh under it (floor z=-3562, placed z=-3000) …
! navmesh: placement 'MyHouse' covers 12 vanilla navmesh triangle(s) but nothing cuts them …
```

These need `Skyrim.esm` (they read its navmesh geometry). **Offline they simply stay silent** — an
unknown answer is never reported as a problem. Set `"navmesh": {"warnings": false}` to silence them,
and `navmeshCheck: false` on a single placement for the one legitimate exception: an actor you
*deliberately* park off-mesh for a script to `MoveTo` into the world later.

**4. `navmeshOverrides[]` — re-emit a vanilla navmesh from your plugin, unchanged.**

This one authors a **no-op**: every NAVM of the cell you name is copied into your plugin under its own
FormID, with not one vertex, triangle or index touched. It exists to answer a single question — *does
the engine accept a navmesh that arrives from a patch rather than from `Skyrim.esm`?* — because the
answer gates every future navmesh edit (cutting triangles, adding a walkable platform). Ship it,
confirm the NPCs in those cells still walk, and the ground under the rest of the roadmap is solid.

```jsonc
"navmeshOverrides": [
  { "cell": "Skyrim.esm:0x01605E" },                         // an interior cell: all of its navmeshes
  { "worldspace": "Skyrim.esm:0x01A26F", "x": 5, "y": -2 },  // one exterior CELL GRID square
  { "worldspace": "Skyrim.esm:0x01A26F",                     // …or name that square by a point inside it
    "position": { "x": 21750, "y": -7625, "z": 0 } },
  { "cell": "Skyrim.esm:0x01605E",                           // narrow it to a single mesh
    "navmesh": "Skyrim.esm:0x0C9064" }
]
```

- **Verify with `navdiag <plugin>`**: it byte-compares each overridden mesh's NVNM against the raw
  bytes in the master and prints `IDENTICAL` or `DIFF`. If it ever says DIFF, the copy is not a
  copy — stop.
- **`x`/`y` are CELL GRID coordinates**, not world units (world unit / 4096, rounded down).
- **NAVI is not touched.** The mesh keeps its FormID, so the master's own navmesh-info-map entry still
  describes it.
- 🔴 **Triangles are never renumbered.** A neighbouring cell's mesh stores indices *into your triangle
  array*; renumbering silently breaks the seam between cells. That constraint is why this primitive
  copies the array verbatim, and it binds every future navmesh feature too.
- **Last plugin wins** — navmesh has no additive merge. If another mod (USSEP overrides 807 vanilla
  navmeshes) already patched the mesh you name, whichever of you loads later replaces the other
  outright. Check before you override a busy cell.
- Needs `Skyrim.esm`. **Offline it emits nothing and says nothing** — the build is byte-identical to
  one without the section.

> ⚠️ ModForge cannot author **interior** navmesh yet, so an NPC in a brand-new interior cell has
> nothing to path on. Put NPCs that need to walk in a vanilla cell (or a custom worldspace cell with
> `navmesh: true`). Opt into `"navmesh": {"warnEmptyCells": true}` for a reminder.

### map markers (XMRK) — permanent world-map icons

`mapMarkers[]` adds discoverable/fast-travel **location markers** to the world map — independent of any
quest:

```jsonc
"mapMarkers": [
  { "editorId": "MF_HiddenCamp", "name": "Hidden Camp",
    "worldspace": "Skyrim.esm:0x00003C",                 // Tamriel
    "position": { "x": 0, "y": -9000, "z": 0 },
    "type": "Camp",                                       // MarkerType: City/Town/Settlement/Cave/Camp/Fort/Landmark/…
    "flags": ["Visible", "CanTravelTo"] }                 // empty = hidden until the player discovers it
]
```

- Each entry builds a `PlacedObject` on the vanilla **MapMarker** static (`0x10`) carrying an XMRK
  `MapMarker` (name + type + flags), added to the worldspace's **persistent cell** alongside the
  vanilla markers. `type` is a `MapMarker.MarkerType` name; `flags` are `Visible | CanTravelTo |
  ShowAllIsHidden`.
- Because it's a persistent named ref, a map marker can **also** be an `objectives[].targets[]` source
  (bind it with a `forced:<editorId>` alias) — a quest arrow that points at a map location. Worked
  example combining objective markers + an xmarker anchor + a map marker: `examples/quest-markers.json`.

