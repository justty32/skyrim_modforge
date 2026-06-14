# Animation §2–4 — Havok wall / Blender path / mocap retarget

← [animation index](README.md)

## 2. The Havok format wall

- **Closed SDK** (Havok proprietary, pulled after the Microsoft acquisition). All tooling reverse-engineers the binary or wraps the old SDK.
- **Version sensitivity.** Skyrim uses Havok **`hk_2010.2.0`** for animations (confirmed; PyNifly had to *fix* a bug where it used Fallout 4's `hk_2014` class hashes for Skyrim). **Wrong Havok class hashes → CTD or T-pose.** FO4 = `hk_2014`; Skyrim LE/SE = `hk_2010`.
- **Packfile/tagfile, 32-bit vs 64-bit.** Skyrim LE hkx = 32-bit (win32); SE hkx = 64-bit (amd64). Not interchangeable. Classic path: author in LE/32-bit, convert to 64-bit for SE.

**Tools (Linux status):**

| Tool | What it does | Linux? |
|---|---|---|
| **hkxcmd** (figment) | hkx↔XML, hkx→KF; Havok 2010 but **cannot write amd64/SE** — LE-only | Windows; Wine; legacy |
| **ck-cmd** (Caprica) | workhorse: `importanimation` (FBX→hkx), `convert` XML→hkx for win32 **and amd64**, exportanimation; wraps Havok SDK | Windows; Wine, fiddly |
| **hkxconv** (ret2end) | SE (amd64) hkx → XML (direction hkxcmd lacks) | Windows |
| **serde-hkx / `hkxc`** (SARDONYX-sard) | **Pure Rust, cross-platform** (de)serializer for `hk_2010.2.0`; bidirectional win32↔amd64↔XML | **✅ Native Linux** |
| **HavokBehaviorPostProcess.exe** (Bethesda, CK Tools) | official LE→SE: `--platformamd64` rewrites 32-bit→64-bit | Windows; Wine |
| **Havok Content Tools 2014** | the legit but defunct Maya/Max exporter | Windows; abandonware — avoid |
| **HKXPack** | Java hkx↔XML (older) | JVM; superseded by serde-hkx |

**Linux takeaway:** the most fragile historic dependency (Windows hkx converter) is now covered natively by **serde-hkx (`hkxc`)** for conversion/packfile. ck-cmd's `importanimation` remains the richest single-shot tool but is Windows/Wine.

---

## 3. The realistic Blender → Skyrim animation path (2026)

**Blender exporters:**
- **PyNifly** (BadDogSkyrim) — current best; **direct import/export of `.hkx` animations** (FO4/SE/LE), and as of 2025 **writes skeleton+animation hkx directly in binary, without hkxcmd**; fixed the Blender 5.0 layered-action bug; honors export FPS. **BUT Windows-only** (Nifly/Bodyslide native layer), Blender 4.4+, with open issues (#384) on kf/hkx export on 4.4 SE — not bulletproof even on Windows. **The single biggest Linux friction point.**
- **io_scene_niftools** — older; nif/kf but **not modern SE hkx animation** well; superseded by PyNifly for animation.
- **Bethesda Animation Tools / "Bethesda Havok" / armaToHKX / jgernandt's blender-hkx / opparco's io_anim_hkx** — community addons, tend to be **Blender-version-locked** and require converting SE/amd64 down to win32/Oldrim first.

**Typical community chain (and what's deprecated):**
```
animate/import on the Skyrim skeleton in Blender
  → export .hkx (PyNifly direct)   [Windows]
       OR  export FBX → ck-cmd importanimation → .hkx   [Windows/Wine]
  → ensure Havok hk_2010, win32 first
  → HavokBehaviorPostProcess --platformamd64  (win32→amd64)
       OR  serde-hkx hkxc  (win32→amd64, Linux-native)
```
**Fragility:** every box breaks on a version mismatch (PyNifly↔Blender, 2010-vs-2014 hashes, win32-vs-amd64, old addons locked to ancient Blender). **Deprecated/avoid:** HCT 2014, pure-hkxcmd for SE output. **Works in 2026:** PyNifly (Windows) for direct hkx; serde-hkx (Linux) for conversion; ck-cmd (Windows/Wine) for FBX→hkx import.

---

## 4. Mocap & AI-motion ingestion — the retarget problem

Phone mocap / AI generators output **BVH/FBX/glTF** on *their* skeleton (Mixamo, Rokoko, SMPL, UE5 Mannequin…). Getting onto Skyrim's `NPC <name> [tag]` skeleton is a **retarget** — the "write the retarget rule once per source skeleton" philosophy of IDEAS §13/§14.

**Three sub-problems:** (1) **bone-name mapping** (`mixamorig:LeftArm` → `NPC L UpperArm [LUar]`) — deterministic per source, a JSON table written once; (2) **proportions/rest-pose** difference (why naive copy breaks); (3) **root motion** (Skyrim honors only root Z + X/Y-rot; in-place vs root-driven must reconcile or you get foot-skate).

**Tools (all Blender-native, run on Linux):** **Rokoko Studio Live** (free retarget panel, save preset, reuse), **Auto-Rig Pro Remap** (most robust, Mixamo/Rokoko/Xsens presets, paid), **Blender Rigify / native bone-constraint** (free, more manual), **Mixamo** (route through to get a known skeleton, retarget once).

**ModForge-relevant insight:** the Skyrim-skeleton bone map is a **write-once artifact** per provider → *retarget* is far more automatable than *hkx conversion.*
