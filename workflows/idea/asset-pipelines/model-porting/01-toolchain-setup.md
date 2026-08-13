# 01 — Toolchain Setup (Manjaro + Windows side)

← [README](README.md) · next: [02-source-mesh-prep.md](02-source-mesh-prep.md)

Everything you install once, split by where it runs. The static spine ([02]–[06]) is **all in the left column** (native Manjaro). The Windows column is *only* the PyNifly escalation for skinned work ([07]). Wine is a small middle column for two LE-only / Windows-only converters you can defer.

---

## 0. The layout

```
~/model-work/
  src/        incoming .fbx/.obj/.gltf + source textures
  blender/    convert.py (headless script, repo-shipped later — [05])
  out/        exported .nif + .dds, staged into the build tree
  vanilla/    a few extracted vanilla .nif for size/format reference ([04])
```

---

## 1. Native Manjaro — the static spine (no Wine, no reboot)

```bash
# Blender (DCC + native importers + headless scripting)
sudo pacman -S --needed blender
# Texture compression (DDS / BCn / mipmaps) — native Linux CLI
yay -S compressonator-bin     # or build from GPUOpen-Tools/compressonator; provides compressonatorcli
# Inspection / repair bench (Qt, builds native; Wine also fine)
sudo pacman -S --needed nifskope   # AUR; if unavailable, run the Windows build under Wine
```

**Blender NifTools addon** (`io_scene_niftools`) — pure Python, native:
1. Download the latest release `.zip` from [github.com/niftools/blender_niftools_addon/releases](https://github.com/niftools/blender_niftools_addon/releases).
2. Blender → Edit → Preferences → Add-ons → Install from Disk → pick the zip → enable.
3. Confirm: `File → Export → NetImmerse/Gamebryo (.nif)` appears.

**What this column gives you:** import any FBX/OBJ/glTF, fix transforms, build convex/box collision, map materials, **export a `NiTriShape` `.nif` valid in SSE**, and compress textures to `.dds`. The entire static prop pipeline, native. ([04] confirms NifTools does convex-hull → `bhkConvexVerticesShape` and basic unweighted SSE export.)

---

## 2. Windows side (reboot) — PyNifly, the skinned escalation only

PyNifly is **Windows-only** (ships native `NiflyDLL.dll`; confirmed still Windows-only 2026). You reboot to Windows *only* when you hit skinned characters or collision PyNifly handles better. For statics you never leave Manjaro.

On the Windows partition:
1. Install **Blender 4.4+** (PyNifly tracks current Blender).
2. Install **PyNifly** from [github.com/BadDogSkyrim/PyNifly](https://github.com/BadDogSkyrim/PyNifly) (Add-ons → Install the release zip).
3. Confirm SSE export with shaders + `_0`/`_1` weights + `BSDismemberSkinInstance` is available.

PyNifly is the gold standard for: skin weights, `BSDismember` partitions, collision, and full `BSTriShape` SSE output. The dual-boot makes this a first-class backend, not a fallback — see [07].

> **Why not Wine-Blender-PyNifly?** PyNifly loads a native Windows DLL via Blender's bundled Python; under Wine that DLL-load is the fragile part with no maintained success path. A real reboot is more reliable than fighting it. You chose dual-boot — use it.

---

## 3. Wine middle column (optional, defer until needed)

Two converters are Windows binaries that run acceptably under Wine and cover gaps in the native column. **Skip both for the MVP** — the native column already produces a working static.

| Tool | Why you might want it | Wine status |
|------|----------------------|-------------|
| **ck-cmd** ([GH aerisarn](https://github.com/aerisarn/ck-cmd)) | one-command `importfbx … -e` → nif with materials→`BSLightingShaderProperty`, vertex colors, "95% game-ready". Useful if NifTools material mapping is fiddly for a given source. **LE-form nifs only** (load in SSE fine). | CLI; Wine candidate (Linux/Mono undocumented — test, timebox) |
| **texconv** ([MS DirectXTex](https://github.com/microsoft/DirectXTex)) | de-facto Skyrim DDS CLI; GPU-accel BC6H/BC7. | Windows → Wine (well-trodden). Compressonator is the native substitute, so this is optional. |
| **Outfit Studio** ([GH ousnius](https://github.com/ousnius/BodySlide-and-Outfit-Studio)) | armor refit / Copy-Bone-Weights for skinned ([07]). Has a "Building on Linux" path; else Wine. | native build *or* Wine — for [07], not statics |

Backend selection later mirrors the Papyrus native-vs-Wine split (`MODFORGE_CKCMD` can carry a `wine ` prefix) — see [05] §4.

---

## 4. The swappable-backend contract (one seam, many exporters)

Like the voice plan's `text+ref → wav` wrapper, define **one logical operation** and let the backend vary:

```
mesh_to_nif(blend_or_fbx, target_nif, opts) :
    backend = niftools (native)  |  ckcmd (wine)  |  pynifly (windows reboot)
```

- **`niftools`** — default, native, statics. The headless seam is `blender --background --python convert.py -- <args>` ([05] §3).
- **`ckcmd`** — alternate native-ish material path under Wine.
- **`pynifly`** — selected when `opts.skinned` is true; you run it Windows-side (manual for now; [07]).

Same idea for textures: `tex_to_dds(src, slot, profile)` → Compressonator (native) by default, texconv (Wine) as alternate. ModForge picks the backend by env var with graceful "missing → warn, skip" ([05] §4) — the existing conditional-tool posture (`Papyrus.cs`).

---

## 5. VRAM / CPU reality check (16 GB is plenty here)

Mesh porting is **not** GPU-bound the way voice cloning is — it's mostly CPU + disk:
- **Blender import/export, NifTools, collision hulls** — CPU/RAM; trivial for single props.
- **Compressonator BC7** — can use GPU/APU acceleration but CPU mode is fine for single textures; GPU helps only on large batches.
- **16 GB VRAM is overkill for this pipeline** — it matters for the *voice* plan and for any AI-upscale of textures, not for nif/dds conversion. Don't size anything around it here.

So: no VRAM bottleneck, no model downloads, no CUDA venvs. The only "heavy" dependency is Blender itself.

---

## 6. What "done" looks like

- `blender` launches; NifTools addon enabled; `.nif` export menu present (native).
- `compressonatorcli` runs and emits a BC7 `.dds` with mipmaps.
- NifSkope opens a vanilla `.nif` for reference.
- *(Deferred)* Windows partition has Blender + PyNifly ready for [07].

→ proceed to [02](02-source-mesh-prep.md) to bring a mesh in.

---

### Sources
[Blender NifTools addon releases](https://github.com/niftools/blender_niftools_addon/releases) · [PyNifly (GH, Windows-only)](https://github.com/BadDogSkyrim/PyNifly) · [Compressonator (GH GPUOpen-Tools, Win/Linux/Mac CLI)](https://github.com/GPUOpen-Tools/compressonator) · [ck-cmd (GH aerisarn)](https://github.com/aerisarn/ck-cmd) · [Outfit Studio (GH ousnius)](https://github.com/ousnius/BodySlide-and-Outfit-Studio). Internal: `src/ModForge.Core/Papyrus/Papyrus.cs` (backend env-var pattern).
