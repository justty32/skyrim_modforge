# 09 — Extracting from Wuthering Waves (FModel / CUE4Parse)

← [README](README.md) · related: [02-source-mesh-prep.md §5](02-source-mesh-prep.md), [03-materials-textures.md](03-materials-textures.md), [07-skinned-characters.md](07-skinned-characters.md)

Wuthering Waves is an **Unreal Engine 5** title; its assets live in encrypted UE `.pak`/`.utoc`/`.ucas` archives. The extractor is **FModel** (GUI) built on **CUE4Parse** (the cross-platform UE parsing library). Given your **dual-boot**, the path of least resistance is: **extract on the Windows side with FModel (native), copy the glTF to Manjaro, resume the spine.** A Manjaro-native route exists (CUE4Parse CLI / Wine FModel) if you'd rather not reboot — §6.

> Legal (unchanged): you own the game; converted assets stay **local**, never redistributed. WuWa's archives are encrypted — extracting your own copy for personal use is the line we stay on.

---

## 1. Two prerequisites UE5 forces on you

Unlike FromSoft, UE5 extraction needs two moving pieces that **change with each game patch**:

1. **AES decryption key.** WuWa's paks are AES-encrypted. The key rotates per version. Don't hardcode a stale one — in FModel set the **AES key endpoint** to a community-maintained feed, e.g. `https://yarik0chka.github.io/wuwa-keys/keys.json`, and FModel pulls the current key. (A single static key also works for a fixed game version, e.g. patch-1.2.0's `0x4D65...6469`, but the endpoint survives patches.)
2. **`mappings.usmap`.** UE5 needs a type-mappings file to interpret asset properties. Get the current WuWa `.usmap` (community-provided, or dump with Dumper-7 on your install), then FModel → Settings → General → **Local Mapping File** → enable + point at it.

Mismatched usmap or stale AES = assets won't parse. These two are the real WuWa friction; the mesh export itself is easy.

---

## 2. Point FModel at the game

1. FModel → Settings → **Game's archive directory** = WuWa's `...\Client\Content\Paks` (the `pakchunk*.pak` + `.utoc`/`.ucas`).
2. Settings → **UE Versions** = **`GAME_WutheringWaves`**. **Use this game profile — don't hand-pick a raw UE version number;** the profile encodes WuWa's exact (modified) engine build. (Confirmed FModel setting, 2026.)
3. Load the archives; FModel decrypts with the AES key and parses with the usmap.

---

## 3. Find and export meshes

In the Archives/Folders tree, navigate to character or environment assets. Right-click a mesh asset → **Export**. Format options (Settings → Export):

| Format | Use | Notes |
|--------|-----|-------|
| **glTF 2.0** | **recommended** | mesh + skeleton + textures in one; FModel's glTF export was fixed in 2024. Feeds Blender [02] cleanly. |
| **PSK / PSKX** | ActorX | PSK = skeletal, PSKX = static. Older; needs the Blender PSK importer. |
| **UEFORMAT** | newest | richer than dead ActorX; needs the UEFormat Blender importer. |

FModel **exports the textures alongside** the mesh for most assets — so you get diffuse/normal/packed maps without a second pass. Static environment assets export as static meshes (PSKX/glTF) — those are your easy win; characters export with the UE skeleton.

---

## 4. Static vs character (which path)

- **Environment / props (static)** → glTF or PSKX → straight to the model-porting spine ([02]→[04]). Easiest WuWa win, no skeleton.
- **Characters (skeletal)** → glTF/PSK carries the **UE skeleton + weights** → the [07] retargeting path (UE skeletal → Skyrim skeleton bone-map). This is the wall; do statics first.

---

## 5. Two UE-specific traps that will bite

- **Nanite = very high poly.** WuWa environment meshes may be Nanite. FModel/CUE4Parse converts Nanite to a standard LOD mesh, but the result can be **far too dense for Skyrim** (no Nanite in Gamebryo). **Decimate in Blender** (Decimate modifier, or import a lower LOD) before [04], or Skyrim chokes. This is the #1 WuWa-to-Skyrim gotcha.
- **UE materials don't translate.** You get the *base textures*, not the material graph. Re-author the Skyrim material in [03]: identify diffuse / normal / packed (ORM-style) maps, channel-repack to True PBR (RMAOS) or legacy, BC-compress. UE normal maps are typically OpenGL-ish — check the green-channel convention ([03] §4).

---

## 6. Linux story (dual-boot makes this simple)

**FModel's GUI is Windows (WPF).** Your options, easiest first:

1. **Windows side (recommended for you).** Reboot to Windows, run FModel natively, export glTF, copy the `.glb` + textures to the Manjaro build tree. Since you dual-boot, this is friction-free and avoids Wine/usmap/AES quirks under emulation. The *conversion* (Blender→nif) is then native Manjaro — only the *extract* is Windows-side.
2. **Wine.** FModel runs under Wine for many users; AES-endpoint fetch + usmap still work. Test, timebox.
3. **CUE4Parse CLI (native).** CUE4Parse is cross-platform .NET; a headless CLI (e.g. UnrealExporter, or a small `dotnet` tool over CUE4Parse) extracts on native Linux. More setup than FModel, but no reboot and no Wine. **UModel** has a Linux CLI but lags on UE5 past ~5.4 — prefer CUE4Parse/FModel for a current UE5 title like WuWa.

Given your dual-boot, **(1)** is the clean answer: extract Windows-side, convert Manjaro-side.

---

## 7. Hand off to the model-porting spine

Once a static WuWa mesh is a `.glb` (+ textures) on Manjaro:
1. **[02]** — import glTF; **UE units are centimetres** → ÷100 then scale to Skyrim units (the classic cm-vs-m trap), rotate to Z-up/−Y, calibrate against a ruler. **Decimate if Nanite-dense.**
2. **[03]** — textures → `.dds`, channel-mapped (UE packed maps → RMAOS/True PBR or legacy).
3. **[04]** — NifTools export → `NiTriShape` static `.nif` + collision. Native.
4. **[05]/[06]** — `StaticSpec.Model` + `package` → in-game.

---

## 8. What "done" looks like

- FModel configured: WuWa Paks dir, `GAME_WutheringWaves`, current AES endpoint + matching `.usmap`.
- One **static WuWa mesh** exported as glTF + textures (Windows-side), copied to Manjaro.
- Decimated (if Nanite), transform calibrated (UE cm→Skyrim), handed to [04] → an in-game Skyrim static.

---

### Sources
[FModel (GH 4sval — UE archive explorer, glTF/PSK/PSKX/UEFormat export, textures, Nanite→LOD)](https://github.com/4sval/FModel) · [FModel](https://fmodel.app/) · [CUE4Parse mesh conversion & export (DeepWiki)](https://deepwiki.com/FabianFG/CUE4Parse/4.1-mesh-conversion-and-export) · [wuwa-keys AES endpoint (GH yarik0chka)](https://github.com/yarik0chka/wuwa-keys) · [TCRF — FModel UE5 usmap guide](https://tcrf.net/Help:Contents/Finding_Content/Game_Engines/Unreal_Engine_5/FModel). Confirmed 2026-06-09.
