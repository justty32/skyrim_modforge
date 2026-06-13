# 08 — Extracting from Dark Souls / FromSoft (soulstruct-blender)

← [README](README.md) · related: [02-source-mesh-prep.md §5](02-source-mesh-prep.md), [07-skinned-characters.md](07-skinned-characters.md)

The generic MVP ([02]–[06]) is source-agnostic; this chapter makes Dark Souls a *real* source. **FromSoft is the cleanest Linux source there is** — `soulstruct` + `soulstruct-blender` are pure Python, so the entire extract→Blender path is **native Manjaro, zero Wine**. A DS **map piece** is exactly the survey's recommended first MVP: a static prop that lands straight on the [04] nif spine.

> Legal (unchanged): you own the game; converted assets stay **local**, never redistributed.

---

## 1. Why this is the easy one

`soulstruct` reads FromSoft's container formats (DCX/BND/BHD/BDT/TPF/FLVER) **in Python** — no Windows tools in the import path. `soulstruct-blender` wraps it as a Blender add-on that imports FLVER directly with materials, UVs, and (for characters) armature + weights. So:

- **Map pieces / objects (static)** → straight to [04] §1, the sweet spot.
- **Characters** → carry a FromSoft skeleton → retarget ([07]).

The only place a Windows tool *may* appear is a one-time archive unpack for some games (§3) — and even that is often avoidable.

---

## 2. Pick a game (cleanest first)

| Game | soulstruct-blender | DCX compression | Notes |
|------|-------------------|-----------------|-------|
| **DS1: Remastered (DSR)** | full (DS1 is the original target) | DEFLATE | **cleanest** — files readily accessible, no Oodle |
| **DS3** | full | DEFLATE | clean; UXM unpack once |
| **Sekiro / Elden Ring** | ER **experimental import** (FLVER, anims, navmesh); no ER *export* yet | **Oodle** (needs `oo2core` DLL) | heaviest; Oodle is a Wine/Windows wrinkle |
| DS2 | partial | — | least supported |

**Recommendation:** start with **DS1 Remastered or DS3**. Avoid Elden Ring/Sekiro for the first run — Oodle decompression needs the proprietary `oo2core_*.dll` (Wine/Windows), an avoidable friction for proving the pipeline.

---

## 3. Get the files readable (mostly native)

FromSoft games store data in big `bhd`/`bdt` pairs (dvdbnds) + `*.bnd.dcx` containers. Two routes:

**Route A — let soulstruct read it (native, preferred).** `soulstruct` decompresses DCX and walks BND/BHD/BDT/TPF in Python. For DSR/DS3, point soulstruct-blender's **Game Directory** at the install and it navigates the archives directly — no separate unpack, no Wine, for most map/chr/obj content.

**Route B — one-time bulk unpack (only if needed).** If a game's main archive must be expanded first:
- **DS1 PTDE** → **UDSFM** (Unpack Dark Souls For Modding) — patches the exe to read loose files. *(DSR usually doesn't need this.)*
- **DS2 / DS3 / Sekiro / ER** → **UXM Selective Unpacker**.
- **Yabber** unpacks individual `bnd`/`bhd`/`tpf` (not the giant dvdbnds).

These three are .NET/Windows tools → run under **Wine**, or on your **Windows partition** (dual-boot) once, then point Manjaro soulstruct-blender at the unpacked folder. Prefer Route A; reach for B only if soulstruct can't see the files directly.

---

## 4. Install soulstruct-blender (native Manjaro)

1. Blender **4.1+** (you have it from [01]). soulstruct-blender tracks Blender 4.1–5.0.
2. Download the latest release zip from [github.com/Grimrukh/soulstruct-blender/releases](https://github.com/Grimrukh/soulstruct-blender/releases). It bundles **`io_soulstruct_lib`** (the correct `soulstruct` + `soulstruct-havok` versions) — install that alongside the `io_soulstruct` add-on, into Blender's scripts/add-ons folder (the release README gives the exact path; manual copy, not the standard installer, because of the lib folder).
3. Enable **`io_soulstruct`** in Preferences → Add-ons.
4. In the add-on's **General Settings**: set **Game** (e.g. DSR/DS3), **Game Directory** (the install), and an **Image Cache Directory** (where extracted textures get cached as `.tga`/`.dds`).

---

## 5. Import (the actual work)

In the Soulstruct panel:
- **Map Piece** import → a static building/prop/terrain chunk. *This is your MVP asset.* Map pieces use static posing (no skeleton) → goes straight to [04].
- **Object (OBJBND)** import → props; textures pulled from the appropriate map texture folder.
- **Character (CHRBND)** import → mesh + **FromSoft armature + weights** → the [07] retargeting path.

**Textures:** enable "import textures" on FLVER import. Soulstruct finds the TPF (in the FLVER's BND or the map's texture folder), and caches them in your Image Cache Directory as `.tga`/`.dds`. So you get the source textures *for free* alongside the mesh — feed them to [03] (channel-repack to True PBR / legacy, BC-compress).

**Materials:** Soulstruct reads the **MTD** (DS1/DS3) / **MATBIN** (ER) that defines each FLVER material, builds a faithful Blender node tree, and — crucially — assigns the tightly-packed FLVER UV layers to named layers correctly. You don't hand-untangle UVs.

---

## 6. Hand off to the model-porting spine

Once a **map piece** is in Blender with materials + textures:
1. **[02] §2** — calibrate transform. FromSoft uses **metres**; rotate to Skyrim Z-up/−Y and scale against a vanilla ruler. Record the FromSoft→Skyrim constant once, reuse for all DS assets.
2. **[03]** — its TPF textures → `.dds` (Compressonator), channel-mapped to your chosen profile.
3. **[04]** — NifTools export → `NiTriShape` static `.nif` + convex/box collision. **Native.**
4. **[05]/[06]** — `StaticSpec.Model` + `package` → in-game.

A DS map piece is the lowest-friction end-to-end proof of the whole asset layer — entirely native Manjaro.

---

## 7. Gotchas (DS-specific)

- **Oodle (ER/Sekiro only)** — DCX uses Oodle there; needs `oo2core_*.dll` (copy from the game, Wine/Windows). DSR/DS3 use DEFLATE → no issue. Another reason to start with DSR/DS3.
- **Scale** — metres; the cm-vs-m trap doesn't bite (unlike UE), but still calibrate against a ruler ([02]).
- **Map pieces are static-posed** — perfect for statics; some "plants/building parts" rely on that static pose, so don't expect rigging.
- **Characters need retargeting** — FromSoft skeleton ≠ Skyrim skeleton; that's [07]'s wall, not a static-MVP concern.
- **ER export not supported** — you can *import* ER experimentally but not round-trip; irrelevant for porting *out* to Skyrim (you only need import).
- **`soulstruct-havok`** ships in the lib — relevant only if you later pull FromSoft *animations* (a different pipeline, survey [05]).

---

## 8. What "done" looks like

- soulstruct-blender installed, Game + Game Directory + Image Cache set.
- One **DS map piece** imported with materials + cached textures, **native, no Wine**.
- Transform calibrated (FromSoft→Skyrim constant recorded), handed to [04] → an in-game Skyrim static.

---

### Sources
[soulstruct-blender (GH Grimrukh — install/lib, FLVER+TPF+MTD/MATBIN import, map pieces, Blender 4.1–5.0)](https://github.com/Grimrukh/soulstruct-blender) · [soulstruct-blender README](https://github.com/Grimrukh/soulstruct-blender/blob/main/README.md) · [Yabber (GH JKAnderson — bnd/bhd/tpf/dcx, not dvdbnds)](https://github.com/JKAnderson/Yabber) · [UnpackDarkSoulsForModding (Nexus #1304)](https://www.nexusmods.com/darksouls/mods/1304) · [Souls Modding Wiki — Game Engine & File Formats](http://soulsmodding.wikidot.com/game-engine-file-formats). Confirmed 2026-06-09.
