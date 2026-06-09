# 03 — Materials & Textures (→ `BSLightingShaderProperty` + `.dds`)

← [README](README.md) · prev: [02-source-mesh-prep.md](02-source-mesh-prep.md) · next: [04-nif-and-collision.md](04-nif-and-collision.md)

Two jobs: (1) map the source material onto Skyrim's shader + texture-slot model, (2) compress the source textures to `.dds`. You chose **both texture profiles, build-time selectable** — so this documents the legacy and True-PBR channel mappings, with True PBR as the recommended default (your baseline ships Community Shaders).

The output of this chapter is: a set of correctly-named `.dds` files **and** the Data-relative paths that [04] bakes into the nif's `BSShaderTextureSet`.

---

## 1. The Skyrim material model

A shape's material is a **`BSLightingShaderProperty`** (lighting model + flags) linking a **`BSShaderTextureSet`** — an ordered list of texture slots:

| Slot | Content | Suffix | Notes |
|------|---------|--------|-------|
| 0 | Diffuse / albedo | (none) | BC1 (opaque) or BC3 (alpha) |
| 1 | Normal | `_n` | **DirectX-convention** (Y/green inverted vs OpenGL); BC7 or BC5 |
| 2 | Glow / skin / subsurface | `_g`/`_sk` | optional |
| 3 | Height/parallax | `_p` | optional |
| 4 | Environment (cubemap) | `_e` | optional |
| 5 | Environment mask | `_em`/`_m` | optional |
| 7 | Specular / (True PBR: RMAOS) | `_s` | see §3 |

Paths in the set are **Data-relative strings baked into the nif** (e.g. `Textures\Mine\crate.dds`). Wrong path → invisible/untextured, **no error** ([[vanilla-nif-paths-must-be-verified]]). ModForge owns these strings ([04] §4, [05]).

---

## 2. Textures → `.dds` (native: Compressonator)

Target: **BCn block compression + mipmaps**. Native Linux CLI is Compressonator (`compressonatorcli`), confirmed Win/Linux/Mac with BC1–BC7 + mipmap generation + folder-batch.

```bash
# diffuse → BC1 (opaque) or BC3 (alpha), full mip chain
compressonatorcli -fd BC1 -miplevels 20 src/crate_diffuse.png out/textures/crate.dds
# normal map → BC7 (or BC5 for 2-channel); INVERT GREEN if source is OpenGL (see §4)
compressonatorcli -fd BC7 -miplevels 20 src/crate_normal_dx.png out/textures/crate_n.dds
```
`-miplevels 20` = "as many as the size allows" (it stops at 1×1). Batch a folder by passing a directory + file filter; the CLI names outputs after inputs.

**texconv (Wine) is the alternate** — GPU-accel BC6H/BC7, de-facto Skyrim DDS tool, but Compressonator native removes the Wine dependency for the MVP. Use texconv only if you hit a BC7-quality edge case ([01] §3).

> **Never use ffmpeg/ImageMagick for the final BCn** unless you've verified the output loads in NifSkope — basic DDS writers often skip mipmaps or use formats Skyrim won't sample. Compressonator is the reliable native path.

---

## 3. PBR channel mapping (the "write once, batch apply" lever)

Source PBR (glTF metal/rough) and Skyrim shaders disagree on what lives in which channel. Map once per source convention, then batch.

**Source — glTF metal/rough:**
- Base color (albedo) → slot 0 diffuse
- A packed ORM/MR texture: **Occlusion=R, Roughness=G, Metalness=B**
- Normal → slot 1

**Target A — Legacy spec/gloss (`materialProfile: legacy`, vanilla-compatible):**
- diffuse → slot 0
- normal → slot 1 (DirectX); **gloss often packed into the normal map's alpha**
- a specular map → slot 7
- Lossy: metal/rough must be *converted* to spec/gloss (invert roughness→gloss, derive specular). Acceptable, not pretty.

**Target B — True PBR / Community Shaders (`materialProfile: truepbr`, recommended):**
- diffuse → slot 0
- normal → slot 1
- **RMAOS pack** → a single texture with **Roughness=R, Metallic=G, AO=B, Specular=A** (slot used by the PBR shader) + a small per-material JSON the CS PBR system reads.
- glTF metal/rough → RMAOS is a **clean channel repack** (R←roughness, G←metalness, B←occlusion, A←specular const), *not* a lossy conversion. This is why True PBR is the default: your baseline already has Community Shaders, and the mapping is deterministic.

**Channel-repack with Compressonator/ImageMagick** is a per-source rule: read source channels, write target packing, BC-compress, mipmap, then hand the resulting Data-relative path to [04]. (TruePBR Manager automates exactly this on Windows — a model for what ModForge's converter emits, [05].)

---

## 4. Normal-map convention (one flag, easy to miss)

Skyrim wants **DirectX-convention normals** (green/Y channel pointing down). glTF/Unity/UE sources are often **OpenGL-convention** (green up). If the surface looks "inverted" / lit wrong:

```bash
# invert green channel before compressing (ImageMagick), then Compressonator
convert src/crate_normal_gl.png -channel G -negate +channel src/crate_normal_dx.png
```
Decide this per source convention (same as scale in [02]) and bake it into the rule. Wrong convention = wrong lighting, but *visible* — less dangerous than a wrong path, easy to spot and fix.

---

## 5. Doing it inside Blender (the headless path)

For the automated [05] flow, the material mapping happens in the same `convert.py` that exports the nif:
- Read the imported material's nodes (Principled BSDF for glTF: Base Color, Metallic, Roughness, Normal inputs).
- Resolve each to a source image; emit the `.dds` (shell to Compressonator) under the target naming; set the NifTools material's texture-slot paths to the Data-relative strings.
- NifTools writes those into the `BSShaderTextureSet` on export ([04]).

So [03] is not a separate manual stage in the end — it folds into the Blender export script. The manual runbook ([06]) does it stepwise the first time to prove each piece.

---

## 6. What "done" looks like

- `.dds` files with **mipmaps**, correct BCn per slot, normals in **DirectX** convention.
- A decided **profile** (legacy or truepbr) and its channel-repack rule for the source.
- The exact Data-relative paths recorded for [04] to bake into the nif.

→ [04](04-nif-and-collision.md) exports the nif and writes these paths in.

---

### Sources
[Compressonator command-line docs (BC1–7, mipmaps, batch)](https://compressonator.readthedocs.io/en/latest/command_line_tool/commandline.html) · [DirectXTex texconv](https://github.com/microsoft/DirectXTex) · Community Shaders True PBR / RMAOS convention · Skyrim `BSShaderTextureSet` slot order (Beyond Skyrim NIF Data Format). Internal: `StaticSpec.AlternateTextures` (`Spec.Items.cs`) for per-instance texture-set swaps.
