# Animation §6–8 — Linux reality / end-to-end / ModForge integration — §7–8 端到端流程與 ModForge 整合

← [入口](../linux-workflow-modforge.md)

## 7. End-to-end workflow

Target: **FBX/mocap → Skyrim skeleton → SE hkx → ship as OAR-conditioned replacement (or vanilla-path replacer).** **[AUTO]** = scriptable / ModForge-ownable, **[MANUAL]** = human/Windows/Wine, **[WALL]** = fragile breakpoint.

1. **[AUTO]** Acquire clip (out of scope per your framing).
2. **[AUTO, write-once]** Import + **retarget to Skyrim skeleton** in headless Blender (`blender --background --python retarget.py`) with a per-source bone map. **Runs on Linux.**
3. **[MANUAL / WALL]** **Export to `.hkx`.** Best tool PyNifly is **Windows-only** → Windows VM/box, or fragile Wine-Blender+PyNifly, or **FBX export [AUTO Linux] → ck-cmd `importanimation` [Wine]**. *Wall #1.*
4. **[AUTO, Linux]** Normalize Havok: ensure `hk_2010`, convert win32→amd64 with **serde-hkx `hkxc`** (or `HavokBehaviorPostProcess --platformamd64` under Wine). *Wrong version = wall #2 (T-pose/CTD).*
5. **Ship via one tier:**
   - **(a) Replacer [AUTO]:** place `.hkx` at the vanilla path. Plays immediately, global override.
   - **(c) OAR set [AUTO]:** generate `OpenAnimationReplacer\<Mod>\<Submod>\config.json` (priority + conditions) + drop the `.hkx`. **ModForge-generatable.** Requires Pandora run once for base behavior.
6. **[SHELL-OUT, Linux unverified]** Run **Pandora** (`--auto_run --auto_close -o "<out>" --tesv:"<gamedir>"`) to (re)generate the behavior baseline. .NET 8; native Linux build *exists but is under-tested* — may need Proton-wrapping the Windows build, and automated runs may still need a display (true headless is open issue #114). **Verify on Manjaro.** *(Skip for pure replacer (a) if behavior already generated.)* See [§6.0](linux-reality.md#60-pandora-behaviour-engine--the-2026-baseline-what-replaced-nemesisfnis).
7. **[MANUAL]** In-game test (you can't run the game → structural verification only; user tests via MO2/Proton).

**Walls:** (1) Blender→hkx export on Linux; (2) Havok version/bitness mismatch.

---

## 8. ModForge integration — the realistic split

ModForge is **record-layer (Mutagen) + asset-bundling.**

**ModForge OWNS (deterministic):**
- **(i) IDLE records + SCEN/scene wiring** to *trigger* anims via the existing `PlayIdle` mechanism — **already shipped.** Extend to reference newly-shipped clips that ride a vanilla handle.
- **(ii) OAR config-folder generation** — emit `OpenAnimationReplacer\<Mod>\<Submod>\config.json` (name/description/priority/`conditions[]`) + bundle the `.hkx` under the right `Meshes\...` path. Exactly the record+asset artifact ModForge produces; DAR→OAR Converter proves determinism. **Highest-leverage new capability.**
- **(iii) Vanilla-path replacer bundling** — trivially place a supplied `.hkx` at a vanilla path in the flat MO2 zip.
- **(iv) Shell-out orchestration** — like Papyrus/xLODGen: drive Blender headless (retarget), serde-hkx (`hkxc`), **Pandora** (`--auto_run --auto_close -o "<out>" --tesv:"<gamedir>"`). Pandora is a .NET 8 GUI app driven by startup args (not a referenceable library — the plugin API is explicitly unstable), so the integration model is **shell-out**, identical to how ModForge already drives Papyrus/xLODGen. Open question for the spike: whether the native Linux build runs on Manjaro or must be Proton-wrapped, and whether the automated run needs a display (`xvfb-run`?). **Both being .NET does *not* mean library-embedding is on the table today.**

**ModForge does NOT own (shell out or manual):** actual **Havok hkx encoding** (serde-hkx/ck-cmd/PyNifly), **behavior-graph patching** (**Pandora** — Nemesis/FNIS are legacy), the **Blender→hkx export** (the Windows wall), authoring/retargeting *judgment* (Blender; the *invocation* is scriptable).

**Concrete spec/CLI proposals:**
- **New `animations[]` spec block:** `{ source: <hkx|fbx>, sourceSkeleton: <map-id>, target: <vanilla anim path | new clip name>, ship: "replacer" | "oar", oar?: { mod, submod, priority, conditions[] }, idleRecord?: {...} }`.
  - `ship: "replacer"` → bundle hkx at vanilla path.
  - `ship: "oar"` → generate OAR folder + config.json from `oar.conditions[]` (reuse ModForge's existing condition vocabulary where possible) + bundle hkx; optionally emit an **IDLE record** + scene `PlayIdle` wiring so it's also script-triggerable.
- **New CLI verb `importanim`** (mirrors compile/xLODGen shell-out): `importanim <clip> --skeleton-map <id> --out <hkx>` → headless Blender retarget → FBX/hkx export → serde-hkx convert to SE/amd64. Honest about the Windows wall at the export sub-step (flag it; allow a pre-exported `.hkx` to bypass).
- **Keep "don't self-author":** ModForge generates *config + records + bundling + orchestration*, never the Havok bytes.
