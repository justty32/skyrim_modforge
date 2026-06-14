# Map-scene §8–10 — End-to-end workflow / ModForge integration / MVP

← [map-scene index](README.md)

## 8. End-to-end workflow (recommended: FromSoft MSB interior/arena → Skyrim interior cell)

1. **Extract layout.** Read MSB with **SoulsFormats (C#)** → `Parts.MapPieces` → `{ModelName, Position, Rotation, Scale}`. **AUTO (in-process, no Wine).**
2. **Extract unique meshes.** Per distinct `ModelName`, pull FLVER → Blender → Skyrim nif + dds + **collision** (handoff [03](../03-3d-model-import.md)). **SEMI — this is the wall** (collision per unique mesh).
3. **Convert transforms** ([layout §4](layout-extraction.md): unit×~64 + fudge, up-axis Z, handedness flip, quat→Euler, uniform-scale check). **AUTO.**
4. **Map ModelName → STAT base.** **AUTO.**
5. **Emit a ModForge placement spec** — a `CELL` (interior) with N refs + `LIGT` lighting. **AUTO (the new converter, §9).**
6. **Build** → ESP. **AUTO (existing).**
7. **Navmesh** — flat (auto) or CK Recast+Finalize (manual, Wine). **AUTO/MANUAL.**
8. **Package** → flat MO2 zip with nifs/dds. **AUTO (existing).**

**Wall:** step 2 (mesh→nif **with collision**) and step 7-robust (navmesh over non-flat). Steps 1, 3–6, 8 are cleanly automatable — where ModForge already lives.

---

## 9. ModForge integration

**The natural new piece is a `layout → placement-spec converter`** — turns an extracted scene dump into ModForge's existing placed-ref JSON with [§4](layout-extraction.md)-converted transforms.

- **Add an `importscene` CLI step** (sibling to `build`/`package`). Input: scene source + engine flag, e.g. `importscene --engine ds1 m10_00_00_00.msb` / `--engine ue level.umap.json` / `--engine unity scene-dump`. Output: a ModForge spec JSON (placement list as `CELL` + refs), **not** a built ESP — so you inspect/tweak before `build`.
- **For MSB, do it in-process:** reference **SoulsFormats** (NuGet/submodule, .NET — same stack). No external converter, no Wine, no round-trip. Lowest-friction → argues again for **MSB-first**.
- **For UE/Unity, external front-end → same intermediate JSON:** FModel→umap-JSON / AssetRipper scene-dump → a small parser (same `importscene` step with an engine adapter). Keep **one intermediate placement schema** (`[{model, pos, rot, scale}]`) → one converter back-end, N engine front-ends.
- **Lean on existing builders** for everything downstream: worldspace/`CELL`, placement, `LIGT`, programmatic navmesh all exist — `importscene` just feeds them.

**Gaps to close for *exterior* porting** (interiors need none): (1) heightmap `LAND` — or adopt the [§6](geometry.md) fake-the-ground approach; (2) settlement-scale placed-ref mass production (the converter emits thousands of refs — emit path must stay performant); (3) LOD → xLODGen (Wine); (4) navmesh-over-static (flat-only today).

---

## 10. MVP + gotchas + recommendation

**Recommended MVP:** **one Dark Souls (DS1) MSB arena/room → a single Skyrim interior `CELL`** — a handful of unique kit meshes as `STAT` refs, basic lighting, flat navmesh. Why DS over UE/Unity: MSB read **in-process from C#** (zero Wine/round-trip, same stack), native layout (no Blueprint/prefab guesswork), interior needs only already-working capabilities. Pick a **small, flat-floored, single-level room** so the flat-navmesh MVP suffices and you never touch CK.

**Prove it in two stages to isolate the two risks:**
1. **Layout-only smoke test:** convert the MSB to N refs but point every ref at a *vanilla* mesh (a known cube/wall). Proves the **coordinate conversion** ([§4](layout-extraction.md)) end-to-end with zero mesh-pipeline risk — you immediately see mirrored/mis-scaled/mis-rotated. **Do this first.**
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
