# Skyrim Particle / VFX Import-and-Reuse Pipeline

← index: [asset-pipelines](../README.md) · related: existing MGEF/SPEL/PROJ/EXPL builders, [external_assets.md](../../../../docs/external_assets.md)

**Research date:** 2026-06-08. Scope: personal/single-player SSE modding on Manjaro Linux, ModForge (C#/Mutagen ESP generator). No in-game testing during dev; Wine/Blender/NifSkope available.

**Bottom line up front:** Two cleanly separable layers. The **record layer** (EFSH effect shaders, ARTO art objects, HAZD hazards, and the FormID wiring inside MGEF) is exactly what a Mutagen record-generator is built for — high-value, low-effort. The **asset layer** (the `.nif` particle systems themselves) is a hard wall: no procedural generator, no Blender export path; particle nifs are NifSkope-authored or copied-from-existing-mods, period. The realistic ModForge feature is "reference/bundle existing nifs + author EFSH records from JSON," **not** "generate particles."

## 子頁

| 檔 | 內容 |
|----|------|
| [efsh-record-layer.md](efsh-record-layer.md) | §4 EFSH 細節（便宜贏，純記錄）+ §7 ModForge 整合排序 + §9 MVP/gotchas |
| [particle-nif-wall.md](particle-nif-wall.md) | §2 從既有 mod 複用 + §3 nif 編輯牆 + §5 VFX 瀏覽工具 + §6 外部工具互通 + §8 端到端流程 |

---

## 1. How Skyrim represents particle / visual effects

A "visual effect" = a **record** (data row in the ESP) pointing at an **asset** (`.nif` + `.dds`). Key record types:

| Record | Sig | What it is | Needs custom mesh? |
|---|---|---|---|
| **Effect Shader** | `EFSH` | A *membrane shader* (projected onto target's mesh) + a *particle shader* (sprites), defined by **texture paths + numeric/color/blend params** | **No** — pure record + `.dds` |
| **Art Object** | `ARTO` | Wrapper whose payload is a **`.nif` model path** (MODL) + type flag (DNAM: Magic Casting / Hit Effect / Enchantment Effect). The nif holds the particle system | **Yes** — the nif *is* the effect |
| **Hazard** | `HAZD` | Lingering AoE (fire patch, gas cloud): nif + spell/effect + IMAD + sound + lifetime/radius/limit | Usually yes |
| **Impact Data Set** | `IPDS`→`IPCT` | Surface-hit reactions (decals, sounds, impact art) by material | IPCT references nif/effect art |

**The critical distinction (the deliverable):**
- **EFSH is pure-record.** Membrane = texture projected onto the target's existing mesh with blend modes + animated color/alpha keys; particle shader emits flat **2D sprites** of a texture — no mesh. You can build a new fire-glow/frost-shimmer entirely from a `.dds` + numbers. **Record-generator territory.**
- **ARTO is mesh-dependent.** The record is trivial (model path + type flag), meaningful only if a `.nif` with a `NiParticleSystem`/`BSStripParticleSystem` exists at that path. ModForge can create the ARTO and bundle/reference the nif, but cannot create the nif's particle content.

**How MGEF references this** (the "Visual Effects" tab = FormID fields in MGEF `DATA`):
- **Hit Effect Art** → `ARTO` · **Enchant Effect Art** → `ARTO` · **Casting Art** → `ARTO`
- **Hit Shader** → `EFSH` · **Enchant Shader** → `EFSH`
- **Image Space Modifier** → `IMAD` · **Impact Data Set** → `IPDS`
- **Light** → `LIGH` · **Projectile** → `PROJ` · **Explosion** → `EXPL` · **Hazard** → `HAZD`

A fireball's "look" = MGEF → (Casting ARTO + Hit ARTO + Hit EFSH) + (PROJ → its own trail nif) + (EXPL → EFSH/IMAD/light) + optional HAZD. ModForge already builds MGEF/PROJ/EXPL; the gap is **EFSH/ARTO/HAZD + the FormID wiring**.

> The CK term **RFCT** (Visual Effect) is a small record pairing an EFSH + an ARTO as a reusable unit; "apply visual effect" tools take an RFCT/EFSH/ARTO FormID.

Sources: UESP MGEF/ARTO/EFSH/HAZD format pages; CK wiki EffectShader.

---

### Verified-tool summary (Linux/Wine)
- **SSEEdit/xEdit** — Wine ✅ (primary discovery + verification)
- **NifSkope** — Wine ✅ (only practical particle authoring/inspection)
- **PyNifly** — Blender 4.4+, **Windows-only (Wine)**, **no particle support** ⚠️
- **Apply Visual Effect / Director's Tools / More Informative Console** — in-game (Proton), manual verify only
- **"Effect Seeker"** — does not exist ❌
- **Niagara/Unity/EmberGen → nif** — does not exist ❌ (textures only)

*Mutagen's `EffectShader`/`ArtObject`/`Hazard` classes expose the fields by name, so exact EFSH byte offsets aren't needed for implementation; verify field semantics against a vanilla EFSH in xEdit before finalizing the builder.*
