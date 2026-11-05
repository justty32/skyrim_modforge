# 3D Model Import Pipeline (External Mesh → Skyrim SE `.nif`) — §6–7 解包與端到端流程

← [入口](../03-3d-model-import.md)

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
7. **Animation/behavior `.hkx`** → handoff to [05](../animation/README.md). ← **the real wall (Havok)**
8. ModForge `model`/NPC spec + `package`. *[Auto — existing]*
