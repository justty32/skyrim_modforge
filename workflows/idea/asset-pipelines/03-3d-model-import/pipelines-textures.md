# 3D Model Import Pipeline (External Mesh → Skyrim SE `.nif`) — §3–5 靜態／蒙皮管線與貼圖

← [入口](../03-3d-model-import.md)

## 3. Static-prop pipeline (the sweet spot)

`FBX/OBJ/glTF static model → working Skyrim static .nif`:
1. **Import to Blender** (built-in importers, native Linux). *Auto.*
2. **Fix scale & orientation.** Skyrim ≈ Z-up, -Y forward, ~64 units/m (see [04 §4](../map-scene/layout-extraction.md)); sources vary (Unity Y-up/m, UE Z-up/cm, glTF Y-up/m). Apply + bake. *Auto once measured per source.*
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
- **Handoff to animation:** a rigged character still needs `.hkx` to move → see [animation/](../animation/README.md). Stop here at "mesh skinned to Skyrim skeleton with valid partitions."

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
