# 10 — Extracting from Genshin Impact (3DMigoto frame-dump + GIMI)

← [README](README.md) · related: [02-source-mesh-prep.md §5](02-source-mesh-prep.md), [03-materials-textures.md](03-materials-textures.md), [07-skinned-characters.md](07-skinned-characters.md)

Genshin Impact is a **Unity** title, and its asset bundles are **encrypted** — so the clean Unity extractors (AssetRipper, stock AssetStudio) can't read the meshes directly. That makes Genshin the **hardest and least Linux-clean of the three game sources** here: [08] Dark Souls is native/pure-Python (cleanest), [09] WuWa is middle (Windows-side extract, native convert), and Genshin is the wall. The canonical route isn't a file unpacker at all — it's a **3DMigoto frame-dump** of the running game captured with **GIMI** (GI-Model-Importer), then reconstructed in Blender. Given your **dual-boot**, the path of least resistance mirrors [09]: **frame-dump on the Windows side, reconstruct in Blender (native Manjaro), resume the spine.**

> Legal (unchanged): you own the game; converted assets stay **local**, never redistributed. Genshin's bundles are encrypted — extracting your own copy for personal use is the line we stay on.

> **Anti-cheat / ToS warning (read this first).** Genshin runs a **kernel-level anti-cheat** (`mhyprot`) on Windows. Injecting tooling (3DMigoto) into the **live online client** is against HoYoverse's Terms of Service and carries a real **ban risk**. State this plainly to yourself before you start: this is the riskiest source in the set, and the risk is account-level, not just technical.

---

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

---

## 4. Static vs character (which path)

- **Most Genshin ports are characters (skinned).** Frame-dump on a character gives mesh + textures + partial blend weights/indices → the **[07] retargeting path** (Genshin skeleton ≠ Skyrim skeleton). This is the wall, and reconstruction here is **fiddlier than DS or WuWa** (you're rebuilding from raw GPU buffers, not a clean skeletal export).
- **Static prop (the easier first target).** If you just want to prove the pipeline, dump a **static object** (a prop, a piece of set dressing). No skeleton → straight to the model-porting spine ([02]→[04]), the same easy win as a DS map piece or a WuWa static. Do this first if a static is acceptable.

---

## 5. Traps that will bite

- **Frame-dump only captures what's rendered.** Anything off-screen, culled, or not currently drawn won't be in the dump. You must **pose/show the whole model** — rotate the camera, trigger the right state — to capture every component. **Transparent / effect meshes** (hair alpha, FX) are especially tricky and may need separate captures.
- **Encryption blocks the clean extractors.** Don't waste time trying to make AssetRipper read the bundles — it won't (§1). Frame-dump is the route.
- **Toon / NPR shading, not PBR.** Genshin uses **non-photoreal toon shading** — its texture convention is **Diffuse + Lightmap + Normalmap**, *not* PBR (no albedo/roughness/metallic/AO stack). So the materials **must be re-authored for Skyrim in [03]**, and the result **won't look "Skyrim-native"** — the flat-shaded anime look fights Skyrim's lighting. Budget for material work; this is more than a channel-repack.
- **Partial skeleton.** Weights/indices from a frame-dump are partial and raw — characters need real retargeting ([07]), not a drop-in skeleton.
- **Unity units/axis.** Genshin is Unity → **Y-up, metres**. Calibrate the [02] transform exactly as for a Unity FBX rip (rotate to Skyrim Z-up/−Y, scale against a vanilla ruler).

---

## 6. Hand off to the model-porting spine

Once a reconstructed mesh (+ textures) is in Blender on Manjaro:
1. **[02]** — calibrate transform; **Unity = Y-up, metres** → rotate to Skyrim Z-up/−Y, scale against a vanilla ruler. Record the Genshin→Skyrim constant once.
2. **[03]** — textures → `.dds`; **re-author the NPR Diffuse/Lightmap/Normalmap into a Skyrim material** (True PBR RMAOS or legacy). This is the heavy step for Genshin — toon → Skyrim lighting is not a free conversion.
3. **[04]** — NifTools export → `NiTriShape` static `.nif` + collision (static path), or the [07] skinned path for a character. Native.
4. **[05]/[06]** — `StaticSpec.Model` + `package` → in-game.

For a character, after Blender the path is **[07] retargeting** → reboot to Windows for PyNifly (skin/weights export) per the README's dual-boot decision.

---

## 7. What "done" looks like

- 3DMigoto-GIMI injected (Windows side), one model **frame-dumped** (F8) with the whole model on screen, dump folder (`.buf`/`.vb`/`.ib` + `.dds`) copied to Manjaro.
- GIMI reconstruction + Blender add-on **rebuilt the mesh natively** on Manjaro.
- Transform calibrated (Unity Y-up/m → Skyrim), NPR textures re-authored ([03]), handed to [04] (static) or [07] (character) → an in-game Skyrim asset.
- You went in knowing the **ToS / ban risk** and kept the converted assets **local**.

---

### Sources
[GI-Model-Importer / GIMI (GH SilentNightSound — 3DMigoto fork, blender_3dmigoto_gimi.py, frame-dump → buffers, weights)](https://github.com/SilentNightSound/GI-Model-Importer) · [GIMI on Nexus](https://www.nexusmods.com/genshinimpact/mods/89) · [GenshinStudio — modded AssetStudio (encrypted-bundle decrypt, XOR key + asset-index; mesh partial)](https://github.com/Xiaobin0860/GenshinStudio) · [AssetRipper](https://assetripper.org/) · [Analyzing Genshin Impact's Anti-cheat Module (mhyprot, kernel-level)](https://research.meekolab.com/analyzing-genshin-impacts-anticheat-module) · [Trend Micro — mhyprot2 driver abuse (kernel anti-cheat context)](https://www.trendmicro.com/en_us/research/22/h/ransomware-actor-abuses-genshin-impact-anti-cheat-driver-to-kill-antivirus.html). Confirmed 2026-06-09.
