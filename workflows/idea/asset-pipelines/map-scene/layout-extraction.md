# Map-scene §3–4 — Layout extraction + coordinate conversion

← [map-scene index](README.md)

## 3. Extracting the scene LAYOUT (not just meshes)

Make-or-break question per engine: **can I get a structured list of `(asset reference + world transform)`?** All three store levels as instance-lists; tooling exists to recover them.

### (c) FromSoftware (Dark Souls / Elden Ring) — MSB — *easiest, do first*
The **MSB (Map Studio Binary)** file *literally is the placement list.* Verified structure ([SoulsFormats](https://github.com/JKAnderson/SoulsFormats)):
- **Models** section: declares every asset id (mesh/`FLVER`) loaded.
- **Parts** section: **every visible entity — map pieces, collisions, objects, enemies — each with a transform** (position, rotation, scale). *This Parts list IS the {asset, transform} table.*
- **Regions** section: invisible triggers (ignore for geometry).

**Extraction:** SoulsFormats is a **.NET (C#) library** — huge, because **ModForge is itself C#/Mutagen.** Reference it directly, read MSB in-process: `MSB1.Read(path)` (DS1) / `MSB3` (DS3) / `MSBE` (Elden Ring) → iterate `msb.Parts.MapPieces`, each exposing `.ModelName`, `.Position`, `.Rotation`, `.Scale`. No file round-trip, no Wine — pure managed code on Linux. Smithbox/DSMapStudio (GUI, same lib) render this exact data for a visual cross-check. (Note: those GUIs do **not** export meshes — mesh side is separate, [geometry §5](geometry.md).) The `ModelName → mesh file` mapping is the join.

### (a) Unreal Engine (e.g. Wuthering Waves) — `.umap` — viable, two routes
UE stores a level as a `.umap` whose actors carry `Transform` (Location/Rotation/Scale3D):
1. **[FModel](https://fmodel.app/)** opens pak/ucas → **right-click `.umap` → export JSON / "Export Raw Data"** → actors + transforms. .NET; Wine on Linux.
2. **[umodel_tools](https://github.com/skarndev/umodel_tools)** (Blender addon) — *verified workflow*: FModel `.umap` JSON → Blender `Import Unreal Map` → reconstructs **static-mesh and light placements with transforms.** **Documented limit: only *static* placements recovered — Blueprint/C++-spawned actors are lost.** Fine for level geometry.
3. **[BlenderUmap](https://github.com/Amrsatrio/BlenderUmap)** — reads transforms directly from the umap, rebuilds with placements.

So UE **can** give `{asset, world-transform}` — as FModel JSON (machine-parseable, the ModForge-shaped path) or reconstructed in Blender. *(JSON schema varies by UE version — inspect per game.)*

### (b) Unity (e.g. Genshin) — bundles — viable via AssetRipper
**[AssetRipper](https://github.com/AssetRipper/AssetRipper)** extracts Unity serialized files/bundles and — critically — **reconstructs the scene/GameObject hierarchy with Transform components (pos/rot/scale)**, not just loose meshes; rebuilds `.unity` scenes and re-derives prefabs by detecting repeated sub-hierarchies (i.e. **recovers instancing**, [geometry §5](geometry.md)). AssetStudio (2025 CLI fork) exports models from the Scene Hierarchy with placement. .NET; Wine on Linux (verify current release runtime). *(Genshin bundle encryption may block direct reads — see [03 §6](../03-3d-model-import.md).)*

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

**2026-08-11 進度：pure transform library 已落地。** `SceneCoordinates.ToSkyrim` 接 position/quaternion/scale3，以完整 basis conjugation 輸出 Skyrim position/Euler XYZ/uniform scale，內建明確命名的 Unity LH Y-up、Unreal LH Z-up profile，另有 custom basis/unit/fudge；non-uniform scale 會 flag + diagnostic。7 個單測含非對稱 custom basis，避免 `System.Numerics` row-vector 乘法順序假綠。FromSoft exact profile 刻意不猜，仍需下一階段 cube 實測校準。手冊 `docs/spec/SPEC-scene-coordinates.md`。
