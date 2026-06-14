# Map / Scene Porting Pipeline (External Level → Skyrim SE Worldspace/Cell)

← index: [asset-pipelines](../README.md) · related: [IDEAS.md §5](../../ideas.md) (resource porting), [03-3d-model-import](../03-3d-model-import.md) (mesh handoff), `CODE_MAP.world.md`

**Research date:** 2026-06-08. Personal/single-player only — ported commercial assets converted/used **locally**, never redistributed. *(This is the topic the user is most excited about.)*

**Confidence:** several pipeline details are inferred from how the constituent tools work rather than from a single end-to-end "ported game X into Skyrim" writeup, because that combined pipeline is bespoke. Flagged inline.

## 子頁

| 檔 | 內容 |
|----|------|
| [layout-extraction.md](layout-extraction.md) | §3 抽 layout（FromSoft MSB / UE umap / Unity bundle）+ §4 座標/縮放轉換 |
| [geometry.md](geometry.md) | §5 mesh 交接 + §6 terrain/heightmap + §7 navmesh/collision/LOD |
| [workflow-modforge.md](workflow-modforge.md) | §8 端到端流程 + §9 ModForge 整合（`importscene`）+ §10 MVP/gotchas/結論 |

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

### Sources
SoulsFormats (GH JKAnderson) + SoulsFormatsNEXT · DSMapStudio · Smithbox · MSB format wiki · FModel + umodel_tools + BlenderUmap + UE Viewer/UModel · AssetRipper + AssetStudio · UE/Unity coordinate docs · CK Wiki Unit / Heightmap Editing / Navmesh / Custom Worldspace with LOD · TESAnnwyn · Beyond Skyrim World Heightmap Creation · xLODGen · Skyrim-SE-on-Linux (xEdit under Wine) / AFK Mods Linux guide.
