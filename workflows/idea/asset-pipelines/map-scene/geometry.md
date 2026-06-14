# Map-scene §5–7 — Mesh / terrain / navmesh·collision·LOD

← [map-scene index](README.md)

## 5. The mesh side (handoff — note only)

Each unique asset must become a Skyrim `.nif` (+`.dds`, +collision). **See [03-3d-model-import](../03-3d-model-import.md).** Pipeline: source mesh (FLVER / psk/pskx / FBX / glTF) → Blender → Skyrim nif exporter + dds. ModForge's `model` + `package` bundle the result.

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
