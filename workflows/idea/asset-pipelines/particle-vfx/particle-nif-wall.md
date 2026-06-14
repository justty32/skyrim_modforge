# Particle-VFX §2·3·5·6·8 — Reuse / the nif wall / tools / external interop / workflow

← [particle-vfx index](README.md)

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

## 8. End-to-end workflow: "cool fire-swirl in mod X → my custom spell"

1. **Find it** *(manual, xEdit):* locate the MGEF/ARTO/EFSH; note the ARTO `MODL` nif path + EFSH FormID. *(Optional in-game preview: Apply Visual Effect/Director's Tools.)*
2. **Inspect the nif** *(manual, NifSkope/Wine):* confirm particle system; read its `BSShaderTextureSet` `.dds` paths.
3. **Copy assets** *(auto):* copy nif + every referenced `.dds` into `Meshes/MFVfx/` + `Textures/MFVfx/`. *(If you moved textures, fix the in-nif paths — manual NifSkope or a pyffi script.)*
4. **Author records** *(auto, ModForge):* add an `artObjects[]` entry pointing at your copied nif; optionally an `effectShaders[]` for a membrane glow.
5. **Wire to spell** *(auto):* set MGEF `hitEffectArt`/`castingArt` to the ARTO editorId + `hitShader` to the EFSH; attach MGEF to your SPEL.
6. **Build + package** *(auto):* emit ESP + bundle Meshes/Textures → flat MO2 zip. No master (standalone).
7. **Verify** *(structural now, in-game later):* re-open in xEdit, confirm ARTO MODL path + FormIDs resolve; confirm the zip has the nif+dds at the exact referenced paths. In-game: cast; if invisible, almost always a wrong path ([§9](efsh-record-layer.md)).

Auto: 3–6. Manual: 1–2 (discovery), nif texture-path fix in 3, in-game verify in 7.
