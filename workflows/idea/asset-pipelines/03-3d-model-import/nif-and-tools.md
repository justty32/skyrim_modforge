# 3D Model Import Pipeline (External Mesh → Skyrim SE `.nif`) — §1–2 `.nif` 目標格式與轉換工具

← [入口](../03-3d-model-import.md)

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
