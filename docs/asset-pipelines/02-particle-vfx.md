# Skyrim Particle / VFX Import-and-Reuse Pipeline

← index: [README.md](README.md) · related: existing MGEF/SPEL/PROJ/EXPL builders, [external_assets.md](../external_assets.md)

**Research date:** 2026-06-08. Scope: personal/single-player SSE modding on Manjaro Linux, ModForge (C#/Mutagen ESP generator). No in-game testing during dev; Wine/Blender/NifSkope available.

**Bottom line up front:** Two cleanly separable layers. The **record layer** (EFSH effect shaders, ARTO art objects, HAZD hazards, and the FormID wiring inside MGEF) is exactly what a Mutagen record-generator is built for — high-value, low-effort. The **asset layer** (the `.nif` particle systems themselves) is a hard wall: no procedural generator, no Blender export path; particle nifs are NifSkope-authored or copied-from-existing-mods, period. The realistic ModForge feature is "reference/bundle existing nifs + author EFSH records from JSON," **not** "generate particles."

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

## 2. Reusing particle effects from other installed mods (personal use)

**Identify what's behind an effect you like:**
1. In **SSEEdit/xEdit** (Wine), find the MGEF; read its `ARTO`/`EFSH`/`IPDS` FormIDs + which plugin.
2. Open the ARTO's `MODL` for the nif path (e.g. `meshes\magic\firefxnimble01.nif`).
3. Inspect that nif in **NifSkope** (Wine) to confirm `NiParticleSystem`/`BSStripParticleSystem` and read its `BSShaderTextureSet` `.dds` paths.

**Reference vs. bundle — the core tradeoff:**
- **Reference (dependency):** point your ARTO/MGEF at the other mod's nif path + add that mod as a **master**. Smallest footprint, permanent load-order dependency. Usually unnecessary friction.
- **Copy/bundle (standalone):** copy the `.nif` + its `.dds` into your own `Meshes/`/`Textures/` (mod-named subfolder), point your ARTO at *your* path, **add no master**. Self-contained, no load-order risk. **Recommended default** — matches the existing `model`+`package` philosophy.

**Records vs. assets are independent masters:** copying the *nif file* never creates a master (assets aren't records). A master is only created if you reference another plugin's **record FormID**. Clean standalone recipe: **copy the nif, make fresh ARTO/EFSH records in ModForge, zero masters beyond Skyrim.esm.**

> Personal-use legality: copying another author's assets into a *private, single-player, never-shared* plugin is a non-issue.

---

## 3. Authoring / editing particle nifs — the wall

**NifSkope is the only practical authoring path.** Edit `NiParticleSystem`/`BSStripParticleSystem` + `NiPSysData` + the `NiPSysModifier` chain (emitters, gravity, age-death, color/size) directly as block fields. Viable for *tuning* an existing effect (recolor/rescale/retexture/birth-rate) but painful from nothing.

**Blender export does NOT support particle systems.** Verified from the **PyNifly** README (2026, Blender 4.4+, **Windows-only / Wine on Linux**): supported = meshes/shaders/collisions/skinning/animations(HKX)/connect-points — **particle systems not supported.** The older `io_scene_niftools` is the same. So you **cannot** model a fire swirl in Blender and export a working Skyrim particle nif.

**Procedural generation feasibility:** theoretically a nif is structured binary emittable via **pyffi**/**nifly**, but building a *correct, engine-accepted* particle nif from scratch is a large research project (modifier ordering, controller links, shader flags, bounding data — wrong field = silent failure). **Honest verdict: don't build a particle-nif generator.** The leverage is *parameterizing copies of known-good nifs* (swap the texture-set `.dds`, scale birth rate) — better left to NifSkope or a tiny pyffi field-patch, not ModForge core.

---

## 4. EFSH effect shaders — the cheap win, in detail

EFSH is 100% record-layer: texture paths + numbers, no mesh. CK splits into **Membrane Shader** (projected onto target's existing mesh) and **Particle Shader** (flat sprites).

**Texture path fields** (`.dds`, the only assets EFSH needs): fill/base texture, particle texture, **holes/gradient ("palette") texture** — *CK warning: if the palette texture isn't defined, the shader may silently not render.*

**Membrane params:** source/dest blend modes + blend op; fill color with animated color keys (3 RGB stops) + alpha ratio/amplitude/frequency/phase over fade-in/hold/fade-out; edge color + edge falloff; fade-in/full/fade-out times.

**Particle params:** birth rate (+ramp); lifetime (+delta); initial speed/acceleration/rotation; scale keys over life; color-key animation; flags (grayscale→color/alpha, additive).

> CK gotcha: particle-shader "Time" fields are normalized **0–1 over the effect duration**; membrane "Time" is in seconds. Document this in a spec.

**Proposed `effectShaders[]` spec entry:**
```jsonc
{
  "editorId": "MFEffShFireGlow",
  "fillTexture":     "Textures/MFVfx/firefill.dds",
  "particleTexture": "Textures/MFVfx/spark.dds",
  "paletteTexture":  "Textures/MFVfx/grad.dds",   // don't omit — silent fail
  "membrane": {
    "srcBlend": "SrcAlpha", "destBlend": "One",    // additive glow
    "fillColor": [255,140,40], "edgeColor": [255,80,0],
    "fadeInTime": 0.25, "fullTime": 1.0, "fadeOutTime": 0.5,
    "alphaKeys": [{ "t":0.0,"a":0.0 },{ "t":0.2,"a":1.0 },{ "t":1.0,"a":0.0 }]
  },
  "particle": {
    "birthRate": 80, "lifetime": 1.2, "initialSpeed": 30, "acceleration": -10,
    "scaleKeys": [{ "t":0.0,"s":0.4 },{ "t":1.0,"s":1.2 }],
    "colorKeys": [{ "t":0.0,"rgba":[255,200,50,255] },{ "t":1.0,"rgba":[120,20,0,0] }]
  }
}
```
Straightforwardly buildable with Mutagen (`EffectShader`), needs only `.dds` bundling (package already does textures), gives genuinely new effects without touching a nif.

---

## 5. "Effect Seeker" and VFX-browsing tools

**No tool named "Effect Seeker" exists** (verified). You're likely thinking of one of these real tools:
- **Apply Visual Effect** (SE #45603) — *closest match.* SKSE lesser power; enter a **FormID** of an RFCT/EFSH/ARTO and it applies it to the player; list/clear; **ships an info file of vanilla EditorID↔FormID**; SE version saves/loads applied sets to **JSON**. Best in-game "find/preview an effect" tool.
- **Director's Tools** (SE #61996) — cast hundreds of effect shaders/visual effects on actors + imagespace + weather. Can't auto-detect a stuck effect's FormID — find it in xEdit first.
- **More Informative Console** (SE #19250) — FormID/EditorID/record details for whatever you click (needs Address Library).
- **xEdit/SSEEdit** — the real "catalog": filter to `EFSH`/`ARTO`/`IPDS`/`MGEF` across loaded mods.

**Practical for you (no in-game testing):** lean on **xEdit (browse records) + NifSkope (preview the particle nif)** offline. The in-game appliers are the manual verification step for when you *can* run the game.

---

## 6. External VFX tool interop — the reality

**There is no export path from any modern VFX tool to Skyrim's particle format.** Unreal Niagara, Unity VFX Graph/Shuriken, EmberGen, Houdini, After Effects — **none** export to Gamebryo/NetImmerse `.nif` particle systems. Their architectures (GPU compute, node graphs, VAT/flipbook) have no mapping to `NiParticleSystem` + `NiPSysModifier`.

**The one thing that crosses over: flipbook/sprite-sheet `.dds` textures.** Author an animated texture (or render a sprite sheet in EmberGen/AE), save as `.dds`, feed it as the **particle/fill texture of an EFSH** or the texture-set of a copied particle nif. That's the *one* legitimate external-tool contribution.

**Frame accordingly:** Skyrim particles are NifSkope-authored or copied-from-existing-mods. External tools contribute **textures**, not particle systems. Don't promise a Niagara/Unity import feature — it doesn't exist and can't be reasonably built.

---

## 7. Proposed ModForge integration (ranked by value/effort)

All fit the existing `model`/MGEF/PROJ/EXPL/`package` patterns.

**① `effectShaders[]` → EFSH builder — HIGHEST value, LOW effort.** Pure Mutagen record, spec as §4, bundles only `.dds`. New effects, no nif wall. Wire into MGEF Hit/Enchant Shader + EXPL. **Build first.**

**② `artObjects[]` → ARTO builder — HIGH value, LOW effort (record) but asset-gated.** Trivial record: `editorId`, `model` (reuse the existing `model` field + bundling), `type` flag. Value depends on the user supplying/copying a real particle nif. Pair with bundling (④).

**③ Wiring into MGEF/SPEL/PROJ — MEDIUM value, LOW effort.** Add optional MGEF FormID fields: `hitEffectArt`, `enchantEffectArt`, `castingArt` (→ ARTO by editorId), `hitShader`, `enchantShader` (→ EFSH). Lets PROJ/EXPL reference new EFSH/ARTO. Makes ①/② actually show up.

**④ Particle-nif bundling from chosen mods — MEDIUM value, LOW effort.** Extend `package` to fold in an explicit nif+dds list (or source dir) standalone, plus a `referenceOnly` flag (adds source plugin as master). Default = copy/standalone. Add a build-time path-existence check.

**⑤ `hazards[]` → HAZD builder — LOWER value, MEDIUM effort.** Ties nif + spell/effect + imagespace + sound + radius/lifetime/limit; placed-hazard (PHZD) needs the worldspace/cell system (have it). Niche; do last.

**Explicitly NOT recommended:** a particle-nif *generator*, or any "import from Niagara/Unity" feature (§3, §6 — the wall).

---

## 8. End-to-end workflow: "cool fire-swirl in mod X → my custom spell"

1. **Find it** *(manual, xEdit):* locate the MGEF/ARTO/EFSH; note the ARTO `MODL` nif path + EFSH FormID. *(Optional in-game preview: Apply Visual Effect/Director's Tools.)*
2. **Inspect the nif** *(manual, NifSkope/Wine):* confirm particle system; read its `BSShaderTextureSet` `.dds` paths.
3. **Copy assets** *(auto):* copy nif + every referenced `.dds` into `Meshes/MFVfx/` + `Textures/MFVfx/`. *(If you moved textures, fix the in-nif paths — manual NifSkope or a pyffi script.)*
4. **Author records** *(auto, ModForge):* add an `artObjects[]` entry pointing at your copied nif; optionally an `effectShaders[]` for a membrane glow.
5. **Wire to spell** *(auto):* set MGEF `hitEffectArt`/`castingArt` to the ARTO editorId + `hitShader` to the EFSH; attach MGEF to your SPEL.
6. **Build + package** *(auto):* emit ESP + bundle Meshes/Textures → flat MO2 zip. No master (standalone).
7. **Verify** *(structural now, in-game later):* re-open in xEdit, confirm ARTO MODL path + FormIDs resolve; confirm the zip has the nif+dds at the exact referenced paths. In-game: cast; if invisible, almost always a wrong path (§9).

Auto: 3–6. Manual: 1–2 (discovery), nif texture-path fix in 3, in-game verify in 7.

---

## 9. MVP recommendation + gotchas

**MVP:** ship **`effectShaders[]` (EFSH) + MGEF wiring (hitShader/enchantShader)** first — only VFX feature with zero nif dependency, fully Mutagen, reuses texture-bundling. Add **`artObjects[]` (ARTO) + nif bundling** second for reuse-from-mods. Defer **HAZD**. **Never** attempt particle-nif generation or external-VFX import.

**Gotchas (flag in docs):**
- **Wrong nif/texture path = invisible, no error** — identical to memory `vanilla-nif-paths-must-be-verified`. Add a build-time file-existence check for every EFSH texture path + ARTO model path against the bundled tree (warn, don't fail).
- **EFSH palette/holes texture omission = silent non-render** (CK-confirmed). Treat palette as effectively required; warn if absent.
- **Texture paths live *inside* the nif** — copying an ARTO's nif standalone is incomplete unless its `.dds` are copied *and* the in-nif texture-set paths still resolve. Simplest safe default: **bundle textures at their original relative paths** so the unmodified nif finds them.
- **Master dependencies:** copying assets never adds a master; referencing a record FormID does. Default copy/standalone; only `referenceOnly` adds a master. Missing master = CTD/load-fail; missing asset = invisible-but-loads.
- **BSStripParticleSystem vs NiParticleSystem:** with ENB complex lights only `NiParticleSystem` emits ENB light; strip particles don't. Cosmetic doc note.
- **EFSH particle shader only emits from Actors** (CK): hit/cast-shader particles won't fire from inanimate placed STATs — fine for spells.
- **Existing-save fixation:** EFSH/ARTO are static records (apply fine to existing saves, no `.seq` concern), but a spell already known on a save uses its baked MGEF; re-learn/re-equip to see changes.

---

### Verified-tool summary (Linux/Wine)
- **SSEEdit/xEdit** — Wine ✅ (primary discovery + verification)
- **NifSkope** — Wine ✅ (only practical particle authoring/inspection)
- **PyNifly** — Blender 4.4+, **Windows-only (Wine)**, **no particle support** ⚠️
- **Apply Visual Effect / Director's Tools / More Informative Console** — in-game (Proton), manual verify only
- **"Effect Seeker"** — does not exist ❌
- **Niagara/Unity/EmberGen → nif** — does not exist ❌ (textures only)

*Mutagen's `EffectShader`/`ArtObject`/`Hazard` classes expose the fields by name, so exact EFSH byte offsets aren't needed for implementation; verify field semantics against a vanilla EFSH in xEdit before finalizing the builder.*
