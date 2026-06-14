# Particle-VFX §4·7·9 — EFSH record layer (the cheap win) + integration + MVP

← [particle-vfx index](README.md)

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

## 7. Proposed ModForge integration (ranked by value/effort)

All fit the existing `model`/MGEF/PROJ/EXPL/`package` patterns.

**① `effectShaders[]` → EFSH builder — HIGHEST value, LOW effort.** Pure Mutagen record, spec as §4, bundles only `.dds`. New effects, no nif wall. Wire into MGEF Hit/Enchant Shader + EXPL. **Build first.**

**② `artObjects[]` → ARTO builder — HIGH value, LOW effort (record) but asset-gated.** Trivial record: `editorId`, `model` (reuse the existing `model` field + bundling), `type` flag. Value depends on the user supplying/copying a real particle nif. Pair with bundling (④).

**③ Wiring into MGEF/SPEL/PROJ — MEDIUM value, LOW effort.** Add optional MGEF FormID fields: `hitEffectArt`, `enchantEffectArt`, `castingArt` (→ ARTO by editorId), `hitShader`, `enchantShader` (→ EFSH). Lets PROJ/EXPL reference new EFSH/ARTO. Makes ①/② actually show up.

**④ Particle-nif bundling from chosen mods — MEDIUM value, LOW effort.** Extend `package` to fold in an explicit nif+dds list (or source dir) standalone, plus a `referenceOnly` flag (adds source plugin as master). Default = copy/standalone. Add a build-time path-existence check.

**⑤ `hazards[]` → HAZD builder — LOWER value, MEDIUM effort.** Ties nif + spell/effect + imagespace + sound + radius/lifetime/limit; placed-hazard (PHZD) needs the worldspace/cell system (have it). Niche; do last. *(Note: HAZD has since landed — see [landed/items-magic](../../../feature-dev/landed/items-magic.md).)*

**Explicitly NOT recommended:** a particle-nif *generator*, or any "import from Niagara/Unity" feature ([§3](particle-nif-wall.md), [§6](particle-nif-wall.md) — the wall).

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
