# 10 — Extracting from Genshin Impact (3DMigoto frame-dump + GIMI) — §4–7 路徑選擇、坑、交接、完成定義

← [入口](../10-extract-genshin.md)

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
