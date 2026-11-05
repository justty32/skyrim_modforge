# 3D Model Import Pipeline (External Mesh → Skyrim SE `.nif`) — §8–9 ModForge 整合與 MVP

← [入口](../03-3d-model-import.md)

## 8. ModForge integration

An **asset-layer pipeline parallel to the record-layer Mutagen axis** (IDEAS §14's framing). Bolts onto `model` + `package` + shell-out (Papyrus-Wine, xLODGen) without touching Mutagen core.

**Proposed CLI step `importmesh` (or `convertasset`):** takes a small spec (source file, source type, target nif path, texture mapping, collision mode) and:
1. Shells out to **`blender --background --python convert.py -- <args>`** — repo-shipped headless script using the **NifTools addon** (native Linux) to import, apply the per-source transform + material mapping, generate collision, export the nif. (ck-cmd-under-Wine as an alternate backend, selected like the Papyrus-compiler backends.)
2. Shells out to **Compressonator (native) or texconv (Wine)** for `.dds` + mipmaps, applying the glTF→Skyrim/True-PBR channel mapping.
3. Writes correct **Data-relative paths into the nif's `BSShaderTextureSet`** (Blender script or NifSkope/`nifly` post-step).
4. **Drops nif + dds into the `Meshes/`…`Textures/` tree that `package` already bundles** — so the existing `model` field + copy-trees pick them up unchanged.

**Spec convention:** extend `model` with an optional sibling block, e.g. `modelSource: { file, sourceType: ds|ue|unity|gltf|fbx|obj, collision: convex|box|none, materialProfile: legacy|truepbr }`. Build resolves `modelSource` → runs `importmesh` → produces the `.nif` that `model` references. Optional field = no breaking change (per CLAUDE.md spec-evolution rule).

**Stay "don't self-author":** ModForge orchestrates Blender/ck-cmd/texconv; it does **not** embed a nif writer (nifly is the fallback only if you ever decide to author in-process — not recommended).

**Backend selection** mirrors the native-vs-Wine Papyrus split: `MODFORGE_BLENDER`, `MODFORGE_CKCMD` (Wine prefix), `MODFORGE_TEXCONV`/`MODFORGE_COMPRESSONATOR`, with graceful "tool missing → warn, skip."

---

## 9. MVP + gotchas

**Smallest viable slice:** **one static prop, one game → one in-game Skyrim static.** Concretely a **Dark Souls map-piece** (soulstruct-blender, lowest friction, lands in native Linux Blender) → NifTools export to `NiTriShape` nif → Compressonator dds → hand-place via a ModForge `staticSpec` + `model` path → `package` → load in-game, confirm it renders with collision. No skeleton/Havok/retargeting — proves the asset-layer shell-out end to end before §4.

**Gotchas (mostly already in memory):**
- **Wrong nif/texture path = invisible, no error** (memory `vanilla-nif-paths-must-be-verified`; pair with `packaging-zip-stale-file-trap`).
- **Scale/orientation/units** — Z-up, -Y forward, ~0.0142×; Unity Y-up, UE cm/Z-up, glTF Y-up/m. Calibrate one constant per source.
- **Missing/bad collision → fall-through.** Statics need ≥ a convex/box `bhk`; concave MOPP is the hard upgrade.
- **SSE vs LE nif version** — **LE-form `NiTriShape` works in SSE** (makes the NifTools-addon path viable); SSE-optimization is optional polish. For *armor with skin*, partition/skin-partition correctness matters more.
- **Normal-map convention** — invert green channel for OpenGL-source normals.
- **PyNifly Windows-only** — don't architect the Linux pipeline around it.
- **Genshin encryption** blocks AssetRipper → Proton-side 3DMigoto frame-dump (least Linux-clean of the three).
- **Non-redistribution** — converted commercial assets are private-install only.

---

### Sources
PyNifly (GH BadDogSkyrim) · Blender NifTools addon (GH niftools) · ck-cmd (GH aerisarn) · hkxcmd (GH figment) · Beyond Skyrim NIF Data Format · SSE NIF Optimizer (Nexus #4089) · BodySlide & Outfit Studio (GH ousnius) · AssetRipper (GH) + GI-Model-Importer/GIMI · FModel + CUE4Parse + UnrealExporter (luk-gg) + UModel · soulstruct-blender (GH Grimrukh) + FLVER_Editor + FBX2FLVER + Smithbox · DirectXTex texconv · Community Shaders True PBR.

**Two flags for IDEAS.md §14:** (1) PyNifly is Windows-only — on Linux the automatable exporter is **NifTools addon** (+ ck-cmd under Wine). (2) Targeting **True PBR** (already in the CS baseline) makes the glTF→Skyrim texture mapping a clean channel-repack — the lever that makes §5's "write once, batch apply" actually clean.
