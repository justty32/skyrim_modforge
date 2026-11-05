# 10 — Extracting from Genshin Impact (3DMigoto frame-dump + GIMI) — §1–3 難點與正規路線

← [入口](../10-extract-genshin.md)

## 1. Why Genshin is the hard one

The other two sources hand you a file you can parse offline. Genshin doesn't:

- **Encrypted Unity bundles.** Genshin's `.blk` containers are encrypted, so **AssetRipper / stock AssetStudio fail outright** on the meshes. Modified forks (the various "GenshinStudio" / AnimeStudio AssetStudio branches) can decrypt *some* assets with an XOR key + an asset-index json, but mesh recovery is **partial and version-fragile**, and animation clips routinely don't come out at all. This is not a clean, repeatable extractor path.
- **So the community standard is rendering-time capture, not file unpacking.** You run the game through a 3DMigoto wrapper and dump the geometry the GPU is actually drawing.

The upshot: there is no soulstruct-style "point a Python library at the install" route, and no FModel-style "decrypt the pak and export glTF" route. You capture frames.

---

## 2. The canonical route — 3DMigoto + GIMI

**GIMI** ([github.com/SilentNightSound/GI-Model-Importer](https://github.com/SilentNightSound/GI-Model-Importer)) is a fork of **3DMigoto** modified for Genshin. 3DMigoto is a **DX11 shader-debugging wrapper** that injects into the game; its *frame-analysis dump* captures the vertex/index buffers and textures of whatever is on screen. GIMI adds the Genshin-specific Blender reconstruction script.

The current ecosystem note: GIMI is the original, and a newer **XXMI / migoto-GIMI** family (a unified "X" model-importer launcher covering Genshin and its sibling HoYo titles) has grown around the same 3DMigoto core. The capture + reconstruct mechanic below is the same either way.

The flow:

1. **Inject 3DMigoto-GIMI** into the running game (the `3dmigoto-GIMI-for-playing-mods` build, or the dev build with frame-analysis enabled).
2. **Pose / show the whole model** on screen — frame-dump only captures what's being rendered (§5).
3. **Press F8** (frame-analysis / frame-dump). 3DMigoto writes a dump folder: **vertex buffers (`.buf` / `.vb`), index buffers (`.ib`), and textures (`.dds`)**.
4. Run GIMI's reconstruction (a Python step) and import via the **`blender_3dmigoto_gimi.py`** Blender add-on (Edit → Preferences → Add-ons → Install). It reassembles the mesh in Blender — geometry, UVs, and (for characters) **blend weights + indices** — from the raw buffers.

The reconstruction step (GIMI script + Blender) is **native Linux** once the dump folder exists. Only the dump itself is Windows/DX11-bound.

---

## 3. Dual-boot is the clean answer

3DMigoto is **DX11/Windows**, and injecting it into Genshin under **Proton/Wine is very fiddly** — the wrapper, the overlay, and the game's own launcher fight each other under emulation, and you're doing all of it on top of a kernel anti-cheat. Don't burn days on the Wine path.

Because you **dual-boot**, the friction disappears the same way it did for [09]:

1. **Windows side (recommended).** Reboot to Windows, run 3DMigoto-GIMI natively, frame-dump (F8), and copy the dump folder (`.buf`/`.vb`/`.ib` + `.dds`) to the Manjaro build tree.
2. **Manjaro side.** Run GIMI's reconstruction + the Blender add-on **natively** to rebuild the mesh, then continue the model-porting spine.

So, exactly like WuWa: **extract Windows-side, convert Manjaro-side.** The *only* Windows-bound step is the capture; everything downstream is native.
