# 06 — Standalone At-Home Runbook (start here)

← [README](README.md) · prev: [05-modforge-integration.md](05-modforge-integration.md) · next: [07-skinned-characters.md](07-skinned-characters.md)

Copy-paste path for the home machine (Manjaro primary, Windows on reboot). Don't touch ModForge code — prove the pipeline by hand first ([05] automates it after). Each step proves one thing; don't advance until the current step works. Links jump to the *why*.

> Before you start: a ModForge-built test plugin (`.esp`) with **one `StaticSpec`** whose `Model` points at the nif you're about to make; MO2 + Skyrim SE via Proton; one generic source mesh (`crate.fbx`/`.obj`/`.gltf`) + its textures; one extracted **vanilla `.nif`** of known size as a ruler.

The whole static MVP is **native Manjaro — zero Wine, zero reboot.** The reboot only appears at the skinned step ([07]).

---

## Step 0 — Toolchain sanity (~10 min)

```bash
blender --version                                   # launches
compressonatorcli --version                         # native dds CLI present
# NifTools addon: Blender → Preferences → Add-ons → enable io_scene_niftools
#   confirm File → Export → NetImmerse/Gamebryo (.nif) exists
```
**Proves:** native exporter + dds compressor ready. → detail: [01](01-toolchain-setup.md).

---

## Step 1 — Import + calibrate transform (~30 min — the silent-failure guard)

1. New Blender scene. Import the **vanilla ruler** nif (NifTools) — note its real size.
2. Import your `crate.fbx`. Rotate to **Z-up / −Y forward**, scale to match the ruler.
3. **Record** the scale + rotation for this source convention.
4. `Ctrl+A → Rotation & Scale` (bake it). Triangulate, `Shift+N` normals, ensure a UV map exists.

**Proves:** mesh is correctly oriented/scaled — the #2 silent failure (after paths). → detail: [02](02-source-mesh-prep.md) §2.

---

## Step 2 — Textures → `.dds` (~10 min)

```bash
mkdir -p ~/model-work/out/textures
compressonatorcli -fd BC1 -miplevels 20 ~/model-work/src/crate_d.png  ~/model-work/out/textures/crate.dds
# normal: invert green if source is OpenGL ([03] §4), then:
compressonatorcli -fd BC7 -miplevels 20 ~/model-work/src/crate_n.png  ~/model-work/out/textures/crate_n.dds
```
**Proves:** valid BCn + mipmaps (the format Skyrim samples). → detail: [03](03-materials-textures.md).

---

## Step 3 — Material mapping in Blender (~15 min)

In the Blender material, point the slots at the `.dds` you just made (diffuse → slot 0, normal → slot 1; for True PBR, the RMAOS pack). Set the **Data-relative paths** exactly as they'll sit under `Textures/` (e.g. `Textures\Mine\crate.dds`).

**Proves:** the nif will carry correct texture paths. → detail: [03](03-materials-textures.md) §1,§3.

---

## Step 4 — Add collision (~15 min)

Duplicate the mesh → `Mesh → Convex Hull` (or a box). Name it per NifTools' collision convention, set a bhk material (stone/metal).

**Proves:** the static won't be walk-through. → detail: [04](04-nif-and-collision.md) §3.

---

## Step 5 — Export the nif (~10 min — native)

```
File → Export → NetImmerse/Gamebryo (.nif)
  Game: Skyrim Special Edition      (emits NiTriShape — valid in SSE)
  Apply Scaling: per your Step-1 constant
  → ~/model-work/out/crate.nif
```
Open `out/crate.nif` in NifSkope: confirm geometry, `BSLightingShaderProperty` + texture paths, and a `bhkConvexVerticesShape`/`bhkBoxShape`.

**Proves:** a shippable SSE static, **native, no Wine, no SSE-optimize.** → detail: [04](04-nif-and-collision.md) §1,§2.

---

## Step 6 — Place files at the deterministic paths (~20 min — ★ spine proof)

Highest-value step: the paths in the spec, on disk, and inside the nif must all agree. Wrong = invisible, **no error**.
```
<MO2 mod>/Meshes/Mine/crate.nif
<MO2 mod>/Textures/Mine/crate.dds
<MO2 mod>/Textures/Mine/crate_n.dds
```
…but per [[mo2-reinstall-reverts-manual-pex]], assemble these into your **build zip / mod folder** via your normal packaging flow — don't hand-edit the live MO2 folder. Confirm your `StaticSpec.Model` = `Meshes\Mine\crate.nif` (matches exactly, case included).

**Proves (in-game, manual):** launch via MO2/Proton, place the static (console `player.placeatme <FormID>` or a cell edit) → **it renders, textured, with collision.** This validates path mapping + conversion + packaging — the whole spine — **zero Wine.** → detail: [04](04-nif-and-collision.md) §4.

> If invisible: it's almost always a path (case / sub-folder / extension). Re-check all three against each other. That's why this step exists.

---

## Step 7 — Hand to ModForge (~later)

Once steps 1–6 are a reliable manual recipe, implement the `importmesh` CLI step + `convert.py` + `Mesh.cs` per [05]. The runbook *is* the spec: each manual step maps to one stage of [05] §2.

---

## Step 8 — Skinned escalation (reboot to Windows)

For a **character/armor** mesh, statics stop short — you need a skeleton + skin + `BSDismember`. That's the PyNifly path:
1. **Reboot to Windows.** Blender + PyNifly.
2. Retarget source skeleton → Skyrim skeleton (per-source bone map), clamp ≤4 weights/vertex, build `BSDismemberSkinInstance` partitions (Outfit Studio Copy-Bone-Weights).
3. PyNifly export the skinned nif. Copy back to the Manjaro build tree.

**Proves:** skinned mesh in Skyrim. The retargeting is the wall; animation (`.hkx`) is a separate pipeline. → detail: [07](07-skinned-characters.md).

---

## Quick reference — the whole static MVP on one screen

```
0  blender + compressonatorcli + NifTools addon            → tools ready          [native]
1  import ruler + mesh → Z-up/−Y, scale-match, apply        → correct transform    [native]
2  compressonatorcli -fd BC1/BC7 -miplevels 20              → .dds + mipmaps        [native]
3  point material slots at Data-relative .dds paths         → nif will carry paths  [native]
4  Mesh → Convex Hull, name it, bhk material                → collision             [native]
5  Export → Skyrim SE → NiTriShape .nif (valid in SSE)      → shippable static      [native]
6  paths agree (spec = disk = nif), package, in-game        → renders + collision   ★ spine [native]
7  implement importmesh + convert.py + Mesh.cs              → ModForge automation
8  (character) REBOOT → Windows → PyNifly skinned export    → skinned mesh         [Windows]
```
★ Step 6 is the invisible-on-wrong-path step that proves ModForge's determinism lever — give it the most care. Steps 0–6 never leave Manjaro.
