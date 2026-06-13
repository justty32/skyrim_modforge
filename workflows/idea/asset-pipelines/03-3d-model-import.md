# 3D Model Import Pipeline (External Mesh → Skyrim SE `.nif`)

← index: [README.md](README.md) · related: [IDEAS.md §14](../ideas.md) (format conversion), [external_assets.md](../../../docs/external_assets.md), [04-map-scene-porting.md](04-map-scene-porting.md)

**Research date:** 2026-06-08. Personal/single-player only — ported commercial-game assets (Genshin/WuWa/Dark Souls, Unity-Store/Nexus packs) are converted and used **locally**, never redistributed.

**Headline for Linux:** the biggest tooling decision is **NifTools-addon vs PyNifly vs ck-cmd**, because **PyNifly is Windows-only** (ships a native `NiflyDLL`). This shifts the recommended Linux pipeline away from what IDEAS.md §14 assumes ("PyNifly / ck-cmd"). See §2.

---

## 1. The Skyrim `.nif` target format

NIF (NetImmerse/Gamebryo) = a node-graph binary scene: `NiNode` transforms with leaf geometry + property + collision blocks.

- **Geometry — `BSTriShape` (SSE) vs `NiTriShape` (LE).** SSE merges `NiTriShape`+`NiTriShapeData` into one `BSTriShape` with packed/half-precision verts. **Counter-intuitive verified fact: LE-format `NiTriShape` nifs load fine in SSE and don't strictly need conversion** — and being lower-precision, LE-style nifs are often *recommended* for SSE. **This is the Linux escape hatch** (§9): a Linux-exportable `NiTriShape` nif is a valid SSE asset.
- **"SSE-optimized"** = a tool (SSE NIF Optimizer / Cathedral Assets Optimizer) rewrote `NiTriShape→BSTriShape` + adjusted `NiSkinPartition`. An *optimization*, not a hard requirement for statics.
- **Materials — `BSLightingShaderProperty`** holds the lighting model + flags + a link to a **`BSShaderTextureSet`** (ordered slots: 0 diffuse, 1 normal `_n`, 2 glow/skin/subsurface, 7 specular…). Paths are **Data-relative** strings baked into the nif — wrong path → invisible object, no error (memory `vanilla-nif-paths-must-be-verified`).
- **Collision — `bhk*` / Havok** inside the nif: `bhkCollisionObject`→`bhkRigidBody`→`bhkShape` (`bhkConvexVerticesShape`, `bhkBoxShape`, or `bhkMoppBvTreeShape` for concave). Simple convex/box collision is authorable **without the Havok SDK**; MOPP trees for concave are the hard case.
- **Static vs skinned — the key fork:**
  - **Static prop:** geometry + shader + simple collision, no skeleton/skin → easiest, closest to fully automatable.
  - **Skinned/rigged:** needs a Skyrim **skeleton** (`NPC Spine [Spn1]` naming), **≤4 weights/vertex**, and a **`BSDismemberSkinInstance`** with body-part partitions. Semi-automatable (§4).

---

## 2. Mesh → NIF conversion tools (2026) and their Linux story

| Tool | SSE nif import/export? | Linux | Scriptable | Notes |
|---|---|---|---|---|
| **PyNifly** ([GH](https://github.com/BadDogSkyrim/PyNifly)) | Yes — best (shaders, collision, `_0`/`_1` weights, BSDismember) | **No — Windows only** (native `NiflyDLL`) | Blender addon → CLI via `blender --background --python` *if it ran* | Blender 4.4+. Most capable, but the Linux blocker is real; Wine-Blender path is fragile. |
| **Blender NifTools addon** (`io_scene_niftools`, [GH](https://github.com/niftools/blender_niftools_addon)) | **Yes for non-skinned SSE export**; skinned weaker | **Yes — pure Python, native Manjaro Blender** | Yes (`bpy`, headless) | **Your Linux-native exporter for statics.** Blender 2.83+, maintained late-2025. |
| **ck-cmd** ([GH aerisarn](https://github.com/aerisarn/ck-cmd)) | **Yes — `fbx → nif` one command**, materials→`BSLightingShaderProperty`, vertex colors, "95% game-ready" | Windows CLI; **Wine** candidate (Linux/Mono undocumented) | **Fully CLI, batch** | Strong shell-out candidate (xLODGen/Papyrus pattern). Also does the `.hkx` side. |
| **NifSkope** | view/edit, fix paths/flags, manual collision | Builds on Linux (Qt); also Wine | GUI (manual) | The inspection/repair bench. |
| **Outfit Studio / BodySlide** ([GH ousnius](https://github.com/ousnius/BodySlide-and-Outfit-Studio)) | **Yes** — nif, OBJ/FBX↔nif, **rigs to Skyrim skeleton**, auto partitions | Linux improving; commonly **Wine** | GUI; BodySlide some batch | The armor/outfit refit tool (§4). |
| **nifly** (C++ lib) | Yes (library) | Cross-platform source | embeddable | Only if you ever author in-process — not recommended (§14 "don't self-author"). |

**Bottom line:** on native Manjaro, **NifTools addon (static export) + ck-cmd-under-Wine (fbx→nif + materials) + Outfit Studio-under-Wine (rig/refit)**. PyNifly is gold-standard but costs a Windows VM or a Wine-Blender that abandons native Linux. **IDEAS.md §14 should be corrected:** on Linux the automatable pair is **NifTools-addon / ck-cmd**, not PyNifly.

---

## 3. Static-prop pipeline (the sweet spot)

`FBX/OBJ/glTF static model → working Skyrim static .nif`:
1. **Import to Blender** (built-in importers, native Linux). *Auto.*
2. **Fix scale & orientation.** Skyrim ≈ Z-up, -Y forward, ~64 units/m (see [04 §4](04-map-scene-porting.md)); sources vary (Unity Y-up/m, UE Z-up/cm, glTF Y-up/m). Apply + bake. *Auto once measured per source.*
3. **Set up `BSLightingShaderProperty` materials** (map source material → Skyrim shader + texture-slot set, §5). *Auto, rule-based.*
4. **Generate collision** — simple **convex-hull/box** `bhk` shape is enough and **programmatically generable** (hull of the mesh). ck-cmd/PyNifly emit collision; or generate hull verts in the Blender script. *Auto for convex/box; manual only for concave MOPP.*
5. **Export nif** — NifTools addon → `NiTriShape` (valid in SSE), or ck-cmd `fbx→nif` (Wine). *Auto/headless.*
6. **(Optional) SSE-optimize** (Wine) → `BSTriShape`. **Skippable** (LE-form works).
7. **Verify** in NifSkope (paths/flags/collision) or in-game. *Manual spot-check; path-correctness scriptable.*

**Fully automatable:** import, transform, materials, convex/box collision, export. **Manual:** concave collision, art QA, edge-case orientation. MVP target (§9).

---

## 4. Skinned/character mesh pipeline (harder — "semi-automatable")

Wall = the **skeleton + skin**, not the mesh.
- **Rig to the Skyrim skeleton** — exact bone names (`NPC Spine [Spn1]`, …), **≤4 weights/vertex**, wrapped in **`BSDismemberSkinInstance`** with correct body-part partition flags. Outfit Studio sets default partitions on FBX/OBJ import (80-bone/partition SSE cap).
- **Retarget from a source skeleton** — Genshin (Unity humanoid), UE skeletal, FromSoft rigs all differ. Build a **source→Skyrim bone-name map once per source skeleton** (Blender Rigify/retarget addons or manual), then reuse — the "write the mapping once" philosophy of IDEAS §13/§14. The per-rig map is human work; applying it is batchable → *semi*-automatable.
- **Weight transfer** — **Outfit Studio "Copy Bone Weights"** from a reference body (CBBE/UNP/vanilla); the standard armor-refit workflow, mostly point-and-click. Runs under Wine.
- **Handoff to animation:** a rigged character still needs `.hkx` to move → see [05-animation-pipeline.md](05-animation-pipeline.md). Stop here at "mesh skinned to Skyrim skeleton with valid partitions."

---

## 5. Texture / material conversion

Source PBR → Skyrim `.dds` + correct `BSShaderTextureSet` slot naming.

**Target:** `.dds` with **BCn block compression + mipmaps** (diffuse BC1/BC3, normals BC7/BC5). Skyrim wants **DirectX-convention normals** — invert the green/Y channel if the source is OpenGL.

**Tools (Linux-first):**
- **ImageMagick / GIMP+DDS / Krita** — native, basic DDS (good albedo, weaker BC7/mipmap control).
- **Compressonator** (AMD) — Linux builds + CLI (`compressonatorcli`), full BCn + mipmaps. **Strong native-Linux batch candidate.**
- **texconv** (MS DirectXTex) — de-facto Skyrim DDS CLI (resize→format→mipmaps→BCn in one call, GPU-accel for BC6H/BC7). **Windows → Wine** (shell-out pattern). Compressonator is the Linux-native fallback.

**glTF-PBR ↔ Skyrim shader mapping ("write once, batch apply"):**
- glTF metal/rough packs **roughness=G, metalness=B**; legacy Skyrim shader is **specular/gloss**, while **Community Shaders "True PBR"** uses an **RMAOS** pack (Roughness=R, Metallic=G, AO=B, Specular=A). **Your baseline already has Community Shaders (IDEAS §11-C)**, so **targeting True PBR makes the glTF→Skyrim mapping a clean channel-repack instead of a lossy spec/gloss conversion.** (TruePBR Manager automates the channel-pack/compress/slot/JSON — a model for what ModForge's converter should emit.)
- Deterministic per source convention: choose target (legacy vs True PBR), repack channels, BC-compress, mipmap, then **write the resulting Data-relative paths into the nif's `BSShaderTextureSet`.** Author once, batch over a texture set.

---

## 6. Extracting from your owned games

### Genshin Impact (Unity)
- AssetRipper/AssetStudio have **Linux/Mac releases**, **but Genshin encrypts its bundles** → can't read directly (needs a decrypt step). Community-standard route: **GIMI / 3DMigoto frame-dump** ([GI-Model-Importer](https://github.com/SilentNightSound/GI-Model-Importer)) — run through 3DMigoto, **F8 frame-dump** (vertex buffers + `.dds`), then a Python script + `blender_3dmigoto_gimi.py` reconstructs the mesh.
- **Get:** mesh, `.dds`, bone/weight data; Genshin skeleton → retarget (§4).
- **Linux:** AssetRipper native but encryption-blocked; **3DMigoto/GIMI is DX11/Windows** → dump under **Proton/Wine** (fiddly). Blender reconstruction is **native Linux**.

### Wuthering Waves (Unreal Engine)
- **FModel** ([fmodel.app](https://fmodel.app/)) — UE pak/utoc/ucas explorer + exporter, **.NET 8 → cross-platform**, CUE4Parse-powered. Headless: **UnrealExporter** ([luk-gg](https://github.com/luk-gg/UnrealExporter)), CUE4Parse CLI via `dotnet`. **UModel** has a Linux CLI build but **lacks UE5.x beyond ~5.4** → for a current UE5 title prefer FModel/CUE4Parse.
- **Get:** UE skeletal/static meshes → FBX/glTF (or `.psk`/`.pskx`) + textures; needs the game's `.usmap` mappings + AES key.
- **Linux:** **Good** — .NET cross-platform; feeds Blender natively.

### Dark Souls (FromSoftware)
- **Format:** **FLVER**, backed by **SoulsFormats** (C# lib). **Best for you: [soulstruct-blender](https://github.com/Grimrukh/soulstruct-blender)** — a Blender addon (Python `soulstruct`) importing FLVER directly (characters, objects, equipment, **map pieces** = statics), with armatures/weights/dummies. Blender 4.1+. Also: FLVER_Editor, FBX2FLVER, FbxImporter, Smithbox.
- **Get:** mesh + armature + weights + textures (`.tpf`/`.dds`). Map pieces = static (sweet spot); characters carry FromSoft skeletons → retarget.
- **Linux:** **Excellent** — soulstruct-blender is pure Python in native Linux Blender. **Cleanest of the three.**

**All three converge on Blender**, where the §3/§4 nif pipeline begins.

---

## 7. End-to-end workflow

### (a) Static prop
1. **Extract:** soulstruct-blender (DS) / FModel+UnrealExporter (WuWa) / 3DMigoto-dump or AssetRipper (Genshin) → mesh + `.dds`. *[Auto DS/UE; semi Genshin]*
2. **Import to Blender** (native). *[Auto]*
3. **Fix scale/orientation** per-source rule. *[Auto once calibrated]*
4. **Map materials → BSLighting/True-PBR** + texture slots. *[Auto]*
5. **Generate convex/box collision.** *[Auto]*
6. **Export nif** — NifTools (native) or ck-cmd (Wine). *[Auto]*
7. **Textures → `.dds`** (Compressonator native / texconv Wine), write paths into the nif. *[Auto]*
8. **Drop into Meshes/Textures tree**, feed ModForge `model` spec + `package`. *[Auto — existing]*

**Wall:** essentially none for a pure static — concave collision tuning is the only likely manual step.

### (b) Character / skinned mesh
1. Extract mesh + **source skeleton + weights**. *[Auto/semi]*
2. Import to Blender. *[Auto]*
3. **Retarget source→Skyrim skeleton** (per-rig map). *[Semi]* ← **first wall**
4. Clamp ≤4 weights/vertex, build `BSDismemberSkinInstance` partitions (Outfit Studio copy-bone-weights). *[Semi]*
5. Export skinned nif (PyNifly Windows / Outfit Studio Wine). *[Manual-ish — Linux skin export is the weak spot]*
6. Textures → dds. *[Auto]*
7. **Animation/behavior `.hkx`** → handoff to [05](05-animation-pipeline.md). ← **the real wall (Havok)**
8. ModForge `model`/NPC spec + `package`. *[Auto — existing]*

---

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
