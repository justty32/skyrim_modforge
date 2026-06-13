# Map / Scene Porting Pipeline (External Level → Skyrim SE Worldspace/Cell)

← index: [README.md](README.md) · related: [IDEAS.md §5](../ideas.md) (resource porting), [03-3d-model-import.md](03-3d-model-import.md) (mesh handoff), `CODE_MAP.world.md`

**Research date:** 2026-06-08. Personal/single-player only — ported commercial assets converted/used **locally**, never redistributed. *(This is the topic the user is most excited about.)*

**Confidence:** several pipeline details are inferred from how the constituent tools work rather than from a single end-to-end "ported game X into Skyrim" writeup, because that combined pipeline is bespoke. Flagged inline.

---

## 1. What "map porting" means at the Skyrim data level

Skyrim has two ways to represent a "place," and the distinction is everything:

**(a) Modular static meshes placed as references (the vanilla way).** Every vanilla city/dungeon is a *kit*: a small library of unique `.nif` meshes each placed *many times* as **placed references** (`REFR`/`ACHR` pointing at a base `STAT`/`ACTI`). Each ref carries exactly **base object (which mesh), position (X/Y/Z), rotation (Euler X/Y/Z), and uniform scale.** Whiterun is hundreds of `STAT` refs of a few dozen base nifs. Canonical, engine-blessed.

**(b) One giant mesh.** Possible but a trap: no per-piece collision, hits BSTriShape vertex limits + filesize/streaming, no instancing, no LOD granularity, any edit = full re-export.

**Why modular fits ModForge perfectly.** ModForge already emits `STAT`/`ACTI` placed refs with position/rotation/scale. The data it needs from an external level is *exactly*:
```
{ mesh (base-object id), position (x,y,z), rotation (euler x,y,z), scale }
```
That list IS a set of placed references. **So the entire porting problem reduces to: produce that list (the "placement list"/"layout") + produce the unique meshes it references.** And every real game engine *already stores its levels as exactly this same structure* (an instance list referencing a small set of unique assets) — which is the central reason this is tractable.

---

## 2. Interior (cell) vs exterior (worldspace)

**Interiors are dramatically easier — the correct first target.** A bounded interior `CELL` needs no terrain, LOD, regions, heightmap, climate/weather. Only:
- **Placed references** — kit pieces (ModForge ✅)
- **A navmesh** bounded to the room (ModForge ✅ programmatic navmesh, in-game confirmed)
- **Lighting** — `LIGT` refs / `CELL` lighting template (ModForge `LightSpec`/`LIGT` ✅)
- (optional) image space / fog / acoustic space — skippable for MVP

**Exteriors add four hard requirements, three of which hit ModForge's known gaps:**

| Need | ModForge today |
|------|----------------|
| Settlement-scale placed refs (thousands) | ⚠️ placement works; **mass production is a known gap** |
| Terrain heightmap (`LAND`, real elevation) | ❌ **flat terrain only** |
| LOD (terrain + object) | ❌ **not built — plan to shell out to xLODGen** |
| Larger navmesh over uneven terrain | ⚠️ works; over-terrain at scale unproven |
| Regions / climate / water | partial |

**Recommendation: interiors first.** An interior port exercises only already-working capabilities. Exteriors are phase-2, gated on closing heightmap + LOD.

---

## 3. Extracting the scene LAYOUT (not just meshes)

Make-or-break question per engine: **can I get a structured list of `(asset reference + world transform)`?** All three store levels as instance-lists; tooling exists to recover them.

### (c) FromSoftware (Dark Souls / Elden Ring) — MSB — *easiest, do first*
The **MSB (Map Studio Binary)** file *literally is the placement list.* Verified structure ([SoulsFormats](https://github.com/JKAnderson/SoulsFormats)):
- **Models** section: declares every asset id (mesh/`FLVER`) loaded.
- **Parts** section: **every visible entity — map pieces, collisions, objects, enemies — each with a transform** (position, rotation, scale). *This Parts list IS the {asset, transform} table.*
- **Regions** section: invisible triggers (ignore for geometry).

**Extraction:** SoulsFormats is a **.NET (C#) library** — huge, because **ModForge is itself C#/Mutagen.** Reference it directly, read MSB in-process: `MSB1.Read(path)` (DS1) / `MSB3` (DS3) / `MSBE` (Elden Ring) → iterate `msb.Parts.MapPieces`, each exposing `.ModelName`, `.Position`, `.Rotation`, `.Scale`. No file round-trip, no Wine — pure managed code on Linux. Smithbox/DSMapStudio (GUI, same lib) render this exact data for a visual cross-check. (Note: those GUIs do **not** export meshes — mesh side is separate, §5.) The `ModelName → mesh file` mapping is the join.

### (a) Unreal Engine (e.g. Wuthering Waves) — `.umap` — viable, two routes
UE stores a level as a `.umap` whose actors carry `Transform` (Location/Rotation/Scale3D):
1. **[FModel](https://fmodel.app/)** opens pak/ucas → **right-click `.umap` → export JSON / "Export Raw Data"** → actors + transforms. .NET; Wine on Linux.
2. **[umodel_tools](https://github.com/skarndev/umodel_tools)** (Blender addon) — *verified workflow*: FModel `.umap` JSON → Blender `Import Unreal Map` → reconstructs **static-mesh and light placements with transforms.** **Documented limit: only *static* placements recovered — Blueprint/C++-spawned actors are lost.** Fine for level geometry.
3. **[BlenderUmap](https://github.com/Amrsatrio/BlenderUmap)** — reads transforms directly from the umap, rebuilds with placements.

So UE **can** give `{asset, world-transform}` — as FModel JSON (machine-parseable, the ModForge-shaped path) or reconstructed in Blender. *(JSON schema varies by UE version — inspect per game.)*

### (b) Unity (e.g. Genshin) — bundles — viable via AssetRipper
**[AssetRipper](https://github.com/AssetRipper/AssetRipper)** extracts Unity serialized files/bundles and — critically — **reconstructs the scene/GameObject hierarchy with Transform components (pos/rot/scale)**, not just loose meshes; rebuilds `.unity` scenes and re-derives prefabs by detecting repeated sub-hierarchies (i.e. **recovers instancing**, §5). AssetStudio (2025 CLI fork) exports models from the Scene Hierarchy with placement. .NET; Wine on Linux (verify current release runtime). *(Genshin bundle encryption may block direct reads — see [03 §6](03-3d-model-import.md).)*

**Ranking for ModForge:** **FromSoft MSB ≫ Unity AssetRipper ≈ UE FModel-umap.** MSB wins: (1) layout is the native file, no reconstruction guesswork; (2) read **in-process from C#** via SoulsFormats — zero impedance with ModForge's stack.

---

## 4. Coordinate-system & scale conversion

The deterministic, scriptable, ModForge-shaped core. Each engine differs in handedness/up-axis/unit; convert to Skyrim.

**Target — Skyrim:** Z-up, **128 game units = 6 feet → ~64 units ≈ 1 m** (≈21.33 u/ft); an exterior cell = **4096×4096 units = 192×192 ft ≈ 58.5 m** ([CK Wiki: Unit](https://ck.uesp.net/wiki/Unit)). Rotations are Euler XYZ. Gamebryo+Havok is right-handed Z-up. *(High confidence on 128u=6ft; exact handedness has historically bitten porters — verify empirically.)*

**Sources (verified):**
- **Unreal:** left-handed, **Z-up, centimeters.** X=fwd, Y=right, Z=up.
- **Unity:** left-handed, **Y-up, meters.** X=right, Y=up, Z=fwd.
- **FromSoft (Havok):** practically Y-up, metric-ish — **verify per game**.

**Conversion recipe (per placed object):**
1. **Unit scale.** UE: `Skyrim ≈ cm × 0.64`. Unity/FromSoft: `Skyrim ≈ m × 64`. Then a **global art-scale fudge** (tune so a doorway ≈ 128 units tall).
2. **Axis swap to Z-up.** Unity (Y-up→Z-up): `(x,y,z) → (x,z,y)` then handedness. UE already Z-up; only handedness differs.
3. **Handedness flip (LH→RH).** UE/Unity are left-handed, Skyrim right-handed → one axis negation (commonly negate Y or X) **plus negating the matching rotation components.** Wrong = mirror-imaged level (fix by negating one axis).
4. **Rotation: quat/matrix → Skyrim Euler.** Compose the full source→Skyrim basis change `B`, apply `R_skyrim = B · R_source · B⁻¹`, decompose to Euler XYZ in Skyrim's order. **Do NOT Euler-convert axis-by-axis** (#1 source of subtly-wrong rotations).
5. **Scale: Skyrim refs carry a single uniform scale float.** Non-uniform source scale can't be represented → bake into a unique mesh variant (kills instancing) or approximate uniformly. **Flag any non-uniform-scaled instance** during conversion.

Belongs in a small library fn: `(sourceEngine, position, quat, scale3) → (skyrimPos, skyrimEulerXYZ, skyrimUniformScale)`. **Get it provably right once on a known reference** (place one cube, compare) before mass-converting. Routing through Blender (umodel_tools/AssetRipper-FBX) de-risks the math, since Blender's importers already encode many conventions. *(Determine the exact sign/axis to negate for Skyrim empirically.)*

---

## 5. The mesh side (handoff — note only)

Each unique asset must become a Skyrim `.nif` (+`.dds`, +collision). **See [03-3d-model-import.md](03-3d-model-import.md).** Pipeline: source mesh (FLVER / psk/pskx / FBX / glTF) → Blender → Skyrim nif exporter + dds. ModForge's `model` + `package` bundle the result.

**The instancing win is the headline.** A level has *thousands of instances* but only *tens of unique meshes* (a modular kit — how both vanilla Skyrim and the source engines structure levels). **Convert each unique mesh to nif+collision exactly once**, register as one `STAT`, emit N refs pointing at it. AssetRipper's prefab-rediscovery and MSB's Models section hand you the unique-asset set for free. The expensive manual step (mesh→nif+collision) scales with *unique meshes* (dozens), not *instances* (thousands).

---

## 6. Terrain / heightmap (exterior only)

**How Skyrim terrain works:** each exterior `CELL` has a `LAND` record — a **33×33 vertex height grid** (gradient-encoded), per-vertex normals/colors, up to ~6 quadrant textures (`LTEX`) blended via alpha. Worldspace stitches cells; heights must match at shared borders or you get seams.

**ModForge gap:** flat terrain only.

**Heightmap import tooling:** **TESAnnwyn** (raw heightmap → `LAND`/worldspace), **CK Heightmap Editor** (1024×1024 16-bit RAW → `Data\HeightField\` → bake LAND; Wine), xEdit/xLODGen LAND editing, Beyond Skyrim's World Heightmap workflow.

**Recommendation — the pragmatic path: don't port the heightmap; *fake the ground*.** Place the level's ground/landscape as static `.nif` meshes (they're in the placement list anyway) + a **flat, low `LAND` underneath** as a collision/water-floor backstop. Sidesteps the heightmap-import gap and cell-border stitching, and matches how many custom worldspaces (and the decoded Vigilant.esm reference) actually do it — the "terrain" you walk on is custom static meshes, not LAND. Only invest in real `LAND` import if you need vanilla terrain-LOD blending. *(High confidence this is the right call; keeps work inside ModForge's placed-ref strength.)*

---

## 7. Navmesh, collision, LOD for ported scenes

**Navmesh — partially solved, with a real question.** ModForge's programmatic `NAVM` generates over *flat ground*; generating one that conforms to *placed static geometry* (real floor heights) requires sampling walkable surfaces (Recast-style voxelization) — exactly what the **CK's Recast auto-generate** does (place statics → Recast-generate → **Finalize**). Realistic split for ported interiors:
- MVP: ModForge flat navmesh at floor level (works if roughly single-level/flat-floored).
- Robust: open the built ESP in **CK (Wine)**, NavMesh mode, Recast-generate over placed geometry, Finalize — a **manual** step, the honest answer for multi-level/stairs. A Recast-equivalent inside ModForge is a substantial open task.

**Collision — open, hard, handoff to mesh pipeline.** Each `.nif` needs **Havok collision** (`bhkCollisionObject`/`bhkCompressedMeshShape`) or **player/NPCs fall through.** Source meshes don't bring it; generating it is per-unique-mesh (instancing win applies). **The single biggest practical wall after coordinate math.**

**LOD — shell-out, Wine-able.** [xLODGen](https://github.com/sheson/xLODGen) (a build of xEdit) does terrain/object/tree LOD + occlusion for custom worldspaces via CLI. xEdit runs under Wine/Proton → xLODGen should too *(verify)*. **Interiors need no LOD** — non-issue for MVP.

**Solved vs open today:** navmesh-flat ✅ / navmesh-over-static ❌. Collision ❌ (mesh pipeline). LOD — not needed for interiors, Wine shell-out for exteriors ⚠️.

---

## 8. End-to-end workflow (recommended: FromSoft MSB interior/arena → Skyrim interior cell)

1. **Extract layout.** Read MSB with **SoulsFormats (C#)** → `Parts.MapPieces` → `{ModelName, Position, Rotation, Scale}`. **AUTO (in-process, no Wine).**
2. **Extract unique meshes.** Per distinct `ModelName`, pull FLVER → Blender → Skyrim nif + dds + **collision** (handoff [03](03-3d-model-import.md)). **SEMI — this is the wall** (collision per unique mesh).
3. **Convert transforms** (§4: unit×~64 + fudge, up-axis Z, handedness flip, quat→Euler, uniform-scale check). **AUTO.**
4. **Map ModelName → STAT base.** **AUTO.**
5. **Emit a ModForge placement spec** — a `CELL` (interior) with N refs + `LIGT` lighting. **AUTO (the new converter, §9).**
6. **Build** → ESP. **AUTO (existing).**
7. **Navmesh** — flat (auto) or CK Recast+Finalize (manual, Wine). **AUTO/MANUAL.**
8. **Package** → flat MO2 zip with nifs/dds. **AUTO (existing).**

**Wall:** step 2 (mesh→nif **with collision**) and step 7-robust (navmesh over non-flat). Steps 1, 3–6, 8 are cleanly automatable — where ModForge already lives.

---

## 9. ModForge integration

**The natural new piece is a `layout → placement-spec converter`** — turns an extracted scene dump into ModForge's existing placed-ref JSON with §4-converted transforms.

- **Add an `importscene` CLI step** (sibling to `build`/`package`). Input: scene source + engine flag, e.g. `importscene --engine ds1 m10_00_00_00.msb` / `--engine ue level.umap.json` / `--engine unity scene-dump`. Output: a ModForge spec JSON (placement list as `CELL` + refs), **not** a built ESP — so you inspect/tweak before `build`.
- **For MSB, do it in-process:** reference **SoulsFormats** (NuGet/submodule, .NET — same stack). No external converter, no Wine, no round-trip. Lowest-friction → argues again for **MSB-first**.
- **For UE/Unity, external front-end → same intermediate JSON:** FModel→umap-JSON / AssetRipper scene-dump → a small parser (same `importscene` step with an engine adapter). Keep **one intermediate placement schema** (`[{model, pos, rot, scale}]`) → one converter back-end, N engine front-ends.
- **Lean on existing builders** for everything downstream: worldspace/`CELL`, placement, `LIGT`, programmatic navmesh all exist — `importscene` just feeds them.

**Gaps to close for *exterior* porting** (interiors need none): (1) heightmap `LAND` — or adopt the §6 fake-the-ground approach; (2) settlement-scale placed-ref mass production (the converter emits thousands of refs — emit path must stay performant); (3) LOD → xLODGen (Wine); (4) navmesh-over-static (flat-only today).

---

## 10. MVP + gotchas + recommendation

**Recommended MVP:** **one Dark Souls (DS1) MSB arena/room → a single Skyrim interior `CELL`** — a handful of unique kit meshes as `STAT` refs, basic lighting, flat navmesh. Why DS over UE/Unity: MSB read **in-process from C#** (zero Wine/round-trip, same stack), native layout (no Blueprint/prefab guesswork), interior needs only already-working capabilities. Pick a **small, flat-floored, single-level room** so the flat-navmesh MVP suffices and you never touch CK.

**Prove it in two stages to isolate the two risks:**
1. **Layout-only smoke test:** convert the MSB to N refs but point every ref at a *vanilla* mesh (a known cube/wall). Proves the **coordinate conversion** (§4) end-to-end with zero mesh-pipeline risk — you immediately see mirrored/mis-scaled/mis-rotated. **Do this first.**
2. **Then** swap in the real ported `.nif`s (with collision) for the few unique meshes.

**Gotchas (ranked by likelihood to bite):**
- **Coordinate conversion errors** — mirrored (handedness), wrong scale (forgot ×64 / fudge), garbage rotations (naive axis-by-axis Euler). Nail with the stage-1 vanilla-mesh test.
- **Collision fall-through** — a nif without Havok collision = fall through, *no error*. Biggest post-math wall.
- **Non-uniform scale** — refs carry only a uniform float; flag and handle.
- **Instancing discipline** — convert each unique mesh once, place many refs; don't mint a unique base per instance (kills the economy).
- **Navmesh over non-flat geometry** — flat only works for flat floors; stairs/multi-level need CK Recast (manual, Wine).
- **You can't test in-game** — verify structurally first: build the ESP, open in xEdit (Wine) / your `*diag` tools, confirm the `CELL`, the N `REFR`s with sane pos/rot/scale, the navmesh, the lights — *before* the manual MO2/Proton pass.
- **Non-redistribution** — ported commercial meshes/textures stay local; the shippable artifact pattern would be the *placement spec + converter*, never the bundled assets.

**Bottom line:** well-posed, and the hard part is smaller than it looks, because *every* source engine stores levels as exactly the `{asset, transform}` instance-list ModForge already emits as placed refs. Start with **DS1 MSB → interior cell**, build the **`importscene` converter on SoulsFormats (in-process C#)**, **prove the coordinate math with vanilla meshes first**, and treat **mesh→nif-with-collision** as the known wall to attack second. Defer heightmap/LOD/exterior until the interior loop is solid.

---

### Sources
SoulsFormats (GH JKAnderson) + SoulsFormatsNEXT · DSMapStudio · Smithbox · MSB format wiki · FModel + umodel_tools + BlenderUmap + UE Viewer/UModel · AssetRipper + AssetStudio · UE/Unity coordinate docs · CK Wiki Unit / Heightmap Editing / Navmesh / Custom Worldspace with LOD · TESAnnwyn · Beyond Skyrim World Heightmap Creation · xLODGen · Skyrim-SE-on-Linux (xEdit under Wine) / AFK Mods Linux guide.
