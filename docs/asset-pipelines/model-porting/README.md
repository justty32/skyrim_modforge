# Model Porting — Detailed Implementation Plan (External Mesh → Skyrim SE `.nif`)

Deep-dive companion to the landscape survey [`../03-3d-model-import.md`](../03-3d-model-import.md). That file is the *what exists*; this folder is the *how I build it at home* — exhaustive enough that on a Manjaro session you copy-paste a runbook instead of re-deciding.

**Research/plan date:** 2026-06-09. **Home rig:** dual-boot — **Manjaro (primary)** + **Windows (reboot on demand)**, NVIDIA 16 GB VRAM. **Status:** plan only, no code touched. This folder is **research** — it does not move the maintenance chain (code → CODE_MAP → docs); landing any of it later follows CLAUDE.md Workflow 1.

> **Legal / scope (standing constraint, unchanged from the survey):** personal, single-player, **non-redistributed** use only. Ported commercial-game assets (Genshin/WuWa/FromSoft, Unity-Store/Nexus packs) are converted and used **locally**; never publish converted meshes or textures. They remain the original rights-holders' property.

---

## Locked decisions (your selections, 2026-06-09)

| Fork | Decision | Consequence for this plan |
|------|----------|---------------------------|
| **nif export backend** | **Dual-boot layered.** NifTools addon (native Manjaro) for the static path; **reboot to Windows for PyNifly** when skinned/complex collision is needed. | The static MVP is 100% native Linux. The skinned escalation is a *clean reboot*, not a fragile Wine-Blender or VM — so PyNifly's gold-standard skin/collision/BSDismember is genuinely on the table ([07](07-skinned-characters.md)). |
| **MVP source** | **Generic FBX / OBJ / glTF** (not game-tied). | Extraction chapter is thin — Blender's built-in importers are the front door ([02](02-source-mesh-prep.md)). Game-specific rippers (DS/WuWa/Genshin) are an appendix, deferred until you pick a title. |
| **Scope** | **Static first** (deep, runnable), **skinned designed but deferred.** | [02]–[06] are the static spine you can run end-to-end. [07] is the skinned design — walls named, not yet a runbook. |
| **Texture target** | **Both, build-time selectable** (`materialProfile: legacy \| truepbr`). | [03] documents both channel mappings; True PBR is the recommended default (your baseline already ships Community Shaders). |

---

## The spine (static prop, the sweet spot)

```
source .fbx/.obj/.gltf  (+ source textures)
        │  [02] Blender built-in import (native)
        ▼
   Blender scene  ──[02]── fix scale/orientation (one constant per source convention)
        │
        ├─[03]─ map material → BSLightingShaderProperty / True PBR  +  textures → .dds (Compressonator native)
        │
        ├─[04]─ generate convex/box bhk collision (Blender, native — bhkConvexVerticesShape)
        ▼
   NifTools addon export  ──[04]──  NiTriShape .nif  (LE-form, VALID in SSE — the Linux escape hatch)
        │
        ▼
   Data/Meshes/...nif  +  Data/Textures/...dds   (Data-relative paths baked into BSShaderTextureSet)
        │  [05] ModForge: StaticSpec.Model → package copies Meshes/Textures trees
        ▼
   in-game static with collision
```

Everything on this spine is **native Manjaro, zero Wine, zero reboot.** That is the whole point of the static-first choice: prove the asset-layer shell-out before touching the Havok/skeleton walls.

---

## Build sequence (what to do, in order)

1. **[06] runbook step 0–2** — toolchain sanity + import + transform a generic mesh. Proves Blender↔NifTools native.
2. **[06] step 3–5 / [04]** — export a `NiTriShape` static `.nif` with convex collision; verify in NifSkope. Proves the format target.
3. **[06] step 6 / [03]** — textures → `.dds`, paths written into the nif. Proves materials render.
4. **[06] step 7 / [05]** — hand-place via a ModForge `StaticSpec` + `package`; load in-game. ★ **Proves the spine.**
5. **[05]** — fold the manual recipe into an `importmesh` CLI step.
6. **[07]** — *(later)* skinned characters: reboot to Windows, PyNifly, retargeting.

---

## File index

| File | Covers |
|------|--------|
| [01-toolchain-setup.md](01-toolchain-setup.md) | Manjaro install (Blender + NifTools + Compressonator) + Windows side (PyNifly) + Wine tools + the swappable-backend contract + a VRAM/CPU reality check |
| [02-source-mesh-prep.md](02-source-mesh-prep.md) | Generic FBX/OBJ/glTF import; scale/orientation/units calibration; mesh hygiene; (appendix) game extractors |
| [03-materials-textures.md](03-materials-textures.md) | Material → BSLightingShaderProperty / True PBR; texture → `.dds` (Compressonator/texconv); legacy vs RMAOS channel repack; normal-map convention |
| [04-nif-and-collision.md](04-nif-and-collision.md) | The `.nif` target; NifTools static export (`NiTriShape`); convex/box `bhk` collision (native) vs MOPP (wall); Data-relative path correctness — ModForge's determinism lever |
| [05-modforge-integration.md](05-modforge-integration.md) | `modelSource` spec block; `importmesh` CLI step; `Mesh.cs` shell-out (Papyrus.cs pattern); env-var backends; package wiring; maintenance-chain landing |
| [06-standalone-runbook.md](06-standalone-runbook.md) | Copy-paste at-home runbook, step 0→8, dual-boot-aware; one-screen quick reference |
| [07-skinned-characters.md](07-skinned-characters.md) | *(deferred design)* skeleton bone-map, retarget per source, ≤4 weights, `BSDismemberSkinInstance`, Outfit Studio copy-bone-weights, PyNifly export (Windows reboot), handoff to `.hkx` |
| [08-extract-darksouls.md](08-extract-darksouls.md) | **Dark Souls / FromSoft source** — soulstruct-blender (native Manjaro, pure Python); DCX/BND/TPF/FLVER; map pieces (static MVP) vs characters; DSR/DS3 cleanest |
| [09-extract-wuwa.md](09-extract-wuwa.md) | **Wuthering Waves source** — FModel/CUE4Parse; UE5 AES key + `.usmap`; glTF export (Windows-side, dual-boot); Nanite decimation + UE-material re-author traps |
| [10-extract-genshin.md](10-extract-genshin.md) | **Genshin Impact source** — 3DMigoto F8 frame-dump + GIMI; encrypted Unity bundles (AssetRipper can't read); Windows-side dump → Blender reconstruct (native); toon/NPR re-author + ToS/anti-cheat risk |

---

## Top risks (carried from memory + survey)

- **Wrong nif/texture path = invisible object, no error** — the dominant failure mode. Data-relative strings baked into the nif; ModForge owns and can verify them ([[vanilla-nif-paths-must-be-verified]], and the `model` field already warns "wrong = invisible" in `Spec.MagicFx.cs`). [04] §4 is the mitigation.
- **Scale/orientation** — Skyrim Z-up, −Y forward; sources vary (Unity Y-up/m, UE Z-up/cm, glTF Y-up/m). One miscalibrated constant = giant/tiny/sideways. Calibrate per source against a vanilla nif of known size ([02] §2).
- **Missing/bad collision → fall-through.** Statics need ≥ a convex/box `bhk`; NifTools does convex/box natively, **MOPP (concave) is the wall** — decompose into convex pieces or accept a box ([04] §3).
- **MO2 reinstall reverts hand-placed files** — always rebuild into the zip, never hand-drop into the live mod folder ([[mo2-reinstall-reverts-manual-pex]]).
- **Skinned export on Linux is the weak spot** — hence the reboot-to-Windows-for-PyNifly decision ([07]).

---

### Sources
Tool facts confirmed 2026-06-09 (see per-file Sources): PyNifly (GH BadDogSkyrim, Windows-only/NiflyDLL) · Blender NifTools addon (GH niftools — convex-hull→`bhkConvexVerticesShape`, basic Skyrim SE unweighted export, no MOPP) · ck-cmd (GH aerisarn — fbx→nif, LE-form) · soulstruct-blender (GH Grimrukh — Blender 4.1–5.0) · Compressonator (GH GPUOpen-Tools — Win/Linux/Mac CLI, BC1–7, mipmaps) · Outfit Studio (GH ousnius — has "Building on Linux", Copy Bone Weights) · Community Shaders True PBR. Internal facts from `Spec.Items.cs`, `Assets.cs`, the survey, and CLAUDE.md.
