# Animation → Skyrim SE Asset Pipeline

← index: [README.md](README.md) · related: [IDEAS.md §14](../IDEAS.md) (Havok = "the wall"), memory `scene-playidle-recipe`, [03-3d-model-import.md](03-3d-model-import.md) (rigged-mesh handoff)

**Research date:** 2026-06-08. Turning an arbitrary animation clip (FBX/BVH/mocap/AI-generated) into a *triggerable Skyrim SE asset*, on Manjaro Linux (Blender native, SSE via MO2/Proton), personal/single-player. Focus is the **conversion + integration pipeline** — the user's framing: animation *content* is no longer scarce (free packs, AI motion gen, phone mocap); the missing thing is the **workflow to turn a clip into a usable asset.**

**Confidence note:** This is the single most fragile, version-sensitive corner of Skyrim modding. The record layer (ModForge's wheelhouse) is deterministic; the Havok layer is closed-SDK reverse-engineering held together by version-sensitive community tools. Uncertainty flagged inline. **Headline good news for Linux:** the historically-hardest walls are now solved natively — the *behavior-patch engine* by **Pandora** (.NET, cross-platform), the *hkx conversion* by **serde-hkx** (pure Rust). The remaining wall is the Blender→hkx *export* step (best tool PyNifly is Windows-only).

---

## 1. The Skyrim animation stack — what an "animation asset" actually is

A Skyrim "animation" is **four layers**, and dropping a file in a folder does nothing unless a higher layer references it. The core mental model:

1. **The clip — `.hkx` (Havok binary).** One motion under `Meshes\actors\character\animations\...`: per-bone transforms over time + **annotations**. Skyrim only honors **root Z-translation + X/Y-rotation** (rest of root motion stripped on import).
2. **The skeleton — `skeleton.nif` + `skeleton.hkx`.** Bone hierarchy + rest pose, `NPC <name> [tag]` naming. An animation is authored *against a skeleton*; mismatched bone names/hierarchy → T-pose/garbage. An animation + the skeleton it plays on **must be the same rig.**
3. **The behavior graph — Havok behavior `.hkx` (the hard part).** State machines (`0_master.hkx`, `defaultmale.hkx`…) deciding *when* a clip plays, transitions, conditions. **The gatekeeper.** A clip the graph never references is dead weight. These are "not designed to be modified" — the reason FNIS/Nemesis/Pandora exist (they *patch the graph for you*).
4. **Animation events / annotations.** Named events (`weaponSwing`, idle tags) the graph reacts to; the engine/Papyrus fire them (`Debug.SendAnimationEvent`, `PlayIdle`). **An IDLE record is essentially a named handle the graph already exposes** — which is why ModForge's existing `PlayIdle` works *only* for vanilla IDLEs the vanilla behavior already drives.

**Stated bluntly:** content is layer 1; *playability* lives in layer 3. The whole difficulty of "adding an animation" is **getting layer 3 to reference your layer-1 file** without hand-editing an undocumented binary state machine. Every framework in §5c exists to automate that one thing.

---

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

---

## 5. Getting a custom animation to actually PLAY — the integration layer (the real deliverable)

Three tiers, easiest→hardest:

### (a) Replace an existing animation (zero behavior edits)
Drop your `.hkx` at a **vanilla animation path** (e.g. `...\animations\mt_idle.hkx`). The graph already references that path → it plays your motion. **Pros:** no behavior editing, immediate. **Cons:** global override (every actor playing that idle now plays yours). Simplest win, perfect MVP.

### (b) IDLE record + existing behavior (what ModForge already does)
The graph exposes a finite set of **idle handles / animation events.** An **IDLE record** (`PlayIdle` / `Debug.SendAnimationEvent`) triggers a clip *through a handle the graph already has*. ModForge drives this via the SCEN SceneAdapter `PlayIdle` fragment. **Addressable space without touching behavior = the set vanilla already wires** (bows, gestures, furniture idles, the offset/IdleGive/IdleSilentBow family already decoded). You **cannot** introduce a genuinely new motion category this way — only ride existing handles (and, with (a), replace what a handle points at).

### (c) New animations via a framework (the modern answer)
To **add** animations without hand-editing Havok behavior, use a framework that *patches/generates the graph for you*:
- **FNIS** (legacy) / **Nemesis** (your baseline) — generate patched behavior `.hkx` from a mod-supplied list. Nemesis more capable but a Windows exe (Linux problem, §6).
- **DAR (deprecated) → OAR (Open Animation Replacer)** — SKSE-plugin frameworks doing **condition-based replacement at runtime**: register a folder of replacement clips + a condition set, OAR swaps them in-engine.
- **Pandora Behaviour Engine+** — the modern, *cross-platform .NET* Nemesis/FNIS replacement (§6).

**OAR is the pragmatic modern answer for a record-layer tool — its registration is pure folder + JSON, fully generatable.** Structure:
```
Data\Meshes\actors\character\animations\OpenAnimationReplacer\
  <ModName>\
     config.json                 ← {name, description}  (mod level)
     <SubmodName>\
        config.json              ← {name, description, priority, conditions[...]}
        <clip>.hkx               ← same filename as the vanilla anim being replaced
```
- The **submod `config.json`** carries `priority` (higher wins) + a **`conditions` array** (e.g. `IsActorBase` with plugin/formID, `Random`, `IsEquippedType`, comparisons), each with `negated` + `requiredVersion`. E.g. restrict an idle to the player via `IsActorBase("Skyrim.esm", 0x000007)`.
- OAR **matches by the replaced clip's path/filename** and applies the highest-priority submod whose conditions pass. `user.json` overrides `config.json`. Devs recommend the in-game editor, but the JSON **is** a stable documented schema — machine-generation is viable (the **DAR-to-OAR Converter** generates these JSONs programmatically, proving determinism).
- OAR **needs Nemesis or Pandora run once** to establish base behavior, but **OAR itself adds no behavior edits** — runtime condition-based swapping on top.

**Can ModForge generate the OAR structure? Yes — unambiguously.** Folder tree + `config.json` (name/description/priority/conditions) is exactly the deterministic record+asset artifact ModForge produces. **Highest-leverage integration target.**

---

## 6. Linux/Proton reality for the integration tools (the likely pain point)

| Tool | Role | Linux verdict |
|---|---|---|
| **Nemesis** (your baseline) | behavior-graph patch gen (Windows exe) | **⚠️ Problematic under Wine/Proton — documented thread-race bugs that hang/fail.** Do **not** assume it works on Manjaro. |
| **FNIS** (`GenerateFNISforUsers.exe`) | behavior gen (Windows exe) | **Runs under Wine with caveats** (pre-create the tools dir, can appear frozen on large loadorders, prune Languages to English). Workable, legacy. |
| **Pandora Behaviour Engine+** | modern Nemesis/FNIS replacement | **✅ Best Linux option.** Native .NET, Windows/Linux/macOS builds, consumes **both Nemesis and FNIS formats**, **headless CLI** (`--auto_run`, `--auto_close`, `-o <out>`, `--tesv <gamedir>`). Caveat: "only Windows extensively tested" (team suggests Proton-wrapping the Windows build if the native one misbehaves). But the only behavior engine *designed* for Linux. |
| **OAR / DAR runtime** | SKSE plugin loaded by the game | Runs **inside the game** → works wherever SSE+SKSE works under Proton (your baseline). No separate Linux step. |
| **serde-hkx (`hkxc`)** | hkx↔XML, win32↔amd64 | **✅ Native Linux (Rust).** |
| **ck-cmd / hkxcmd / HavokBehaviorPostProcess** | FBX→hkx, LE→SE | Windows; Wine, version-fiddly. |
| **PyNifly** | Blender direct hkx export | **❌ Windows-only.** The hardest Linux gap. |

**Bottom line for Manjaro:** the *engine* problem is solved by **switching Nemesis → Pandora** (native, reads Nemesis format, headless). The *conversion* problem by **serde-hkx** (native). The *Blender→hkx export* problem is **not** cleanly solved on Linux (PyNifly Windows-only; Linux-capable Blender hkx addons are version-locked + require win32 intermediate). **Recommendation: re-baseline the assumed stack from Nemesis → Pandora** for Linux viability; treat Nemesis-under-Wine as a non-goal. *(This contradicts IDEAS §11-C's "Nemesis" baseline — flagged in [README.md](README.md).)*

---

## 7. End-to-end workflow

Target: **FBX/mocap → Skyrim skeleton → SE hkx → ship as OAR-conditioned replacement (or vanilla-path replacer).** **[AUTO]** = scriptable / ModForge-ownable, **[MANUAL]** = human/Windows/Wine, **[WALL]** = fragile breakpoint.

1. **[AUTO]** Acquire clip (out of scope per your framing).
2. **[AUTO, write-once]** Import + **retarget to Skyrim skeleton** in headless Blender (`blender --background --python retarget.py`) with a per-source bone map. **Runs on Linux.**
3. **[MANUAL / WALL]** **Export to `.hkx`.** Best tool PyNifly is **Windows-only** → Windows VM/box, or fragile Wine-Blender+PyNifly, or **FBX export [AUTO Linux] → ck-cmd `importanimation` [Wine]**. *Wall #1.*
4. **[AUTO, Linux]** Normalize Havok: ensure `hk_2010`, convert win32→amd64 with **serde-hkx `hkxc`** (or `HavokBehaviorPostProcess --platformamd64` under Wine). *Wrong version = wall #2 (T-pose/CTD).*
5. **Ship via one tier:**
   - **(a) Replacer [AUTO]:** place `.hkx` at the vanilla path. Plays immediately, global override.
   - **(c) OAR set [AUTO]:** generate `OpenAnimationReplacer\<Mod>\<Submod>\config.json` (priority + conditions) + drop the `.hkx`. **ModForge-generatable.** Requires Pandora run once for base behavior.
6. **[MANUAL, Linux-OK]** Run **Pandora** (`--auto_run --auto_close -o <out>`) to (re)generate the behavior baseline. Native Linux. *(Skip for pure replacer (a) if behavior already generated.)*
7. **[MANUAL]** In-game test (you can't run the game → structural verification only; user tests via MO2/Proton).

**Walls:** (1) Blender→hkx export on Linux; (2) Havok version/bitness mismatch.

---

## 8. ModForge integration — the realistic split

ModForge is **record-layer (Mutagen) + asset-bundling.**

**ModForge OWNS (deterministic):**
- **(i) IDLE records + SCEN/scene wiring** to *trigger* anims via the existing `PlayIdle` mechanism — **already shipped.** Extend to reference newly-shipped clips that ride a vanilla handle.
- **(ii) OAR config-folder generation** — emit `OpenAnimationReplacer\<Mod>\<Submod>\config.json` (name/description/priority/`conditions[]`) + bundle the `.hkx` under the right `Meshes\...` path. Exactly the record+asset artifact ModForge produces; DAR→OAR Converter proves determinism. **Highest-leverage new capability.**
- **(iii) Vanilla-path replacer bundling** — trivially place a supplied `.hkx` at a vanilla path in the flat MO2 zip.
- **(iv) Shell-out orchestration** — like Papyrus/xLODGen: drive Blender headless (retarget), serde-hkx (`hkxc`), Pandora (`--auto_run --auto_close`).

**ModForge does NOT own (shell out or manual):** actual **Havok hkx encoding** (serde-hkx/ck-cmd/PyNifly), **behavior-graph patching** (Pandora/Nemesis), the **Blender→hkx export** (the Windows wall), authoring/retargeting *judgment* (Blender; the *invocation* is scriptable).

**Concrete spec/CLI proposals:**
- **New `animations[]` spec block:** `{ source: <hkx|fbx>, sourceSkeleton: <map-id>, target: <vanilla anim path | new clip name>, ship: "replacer" | "oar", oar?: { mod, submod, priority, conditions[] }, idleRecord?: {...} }`.
  - `ship: "replacer"` → bundle hkx at vanilla path.
  - `ship: "oar"` → generate OAR folder + config.json from `oar.conditions[]` (reuse ModForge's existing condition vocabulary where possible) + bundle hkx; optionally emit an **IDLE record** + scene `PlayIdle` wiring so it's also script-triggerable.
- **New CLI verb `importanim`** (mirrors compile/xLODGen shell-out): `importanim <clip> --skeleton-map <id> --out <hkx>` → headless Blender retarget → FBX/hkx export → serde-hkx convert to SE/amd64. Honest about the Windows wall at the export sub-step (flag it; allow a pre-exported `.hkx` to bypass).
- **Keep "don't self-author":** ModForge generates *config + records + bundling + orchestration*, never the Havok bytes.

---

## 9. MVP + gotchas + recommendation

**Smallest proving slice:** one mocap/FBX clip → retarget to Skyrim skeleton in Blender → convert to SE/amd64 hkx (serde-hkx) → ship as a **vanilla-path replacer for one idle (tier a)**, *or* a **single OAR submod replacing one idle with an `IsActorBase(player)` condition (tier c)**. The replacer is the absolute minimum (no Pandora); the OAR submod is the minimum that proves **ModForge can generate the integration layer.** Either can *also* be exposed via the **existing `PlayIdle` scene mechanism** if the replaced idle is one ModForge already triggers — closing the loop with shipped capability.

**Why:** exercises retarget (Linux) + hkx convert (Linux, serde-hkx) + ModForge folder/record generation, while **deferring the two walls** (PyNifly Windows export → one-time manual hand-off; OAR avoids the behavior graph).

**Gotchas (the fragile list):**
- **Havok version mismatch** (`hk_2014` FO4 vs `hk_2010` Skyrim) → CTD/T-pose. Verify 2010.
- **LE/win32 vs SE/amd64 bitness** → wrong format = no play/crash. Convert with serde-hkx or `--platformamd64`.
- **Skeleton mismatch** (names/hierarchy/proportions) → T-pose/explode/foot-skate. Retarget map must be exact.
- **Root motion** — only root Z + X/Y-rot honored; mismatch → sliding.
- **The behavior-graph wall** — genuinely *new* motion categories (not replacements) need behavior patching; stay in replacement-land (a/c) for the MVP.
- **Nemesis under Wine is broken** (thread races) — **use Pandora** (native, reads Nemesis format, headless). Re-baseline.
- **PyNifly is Windows-only** — the Blender→hkx export has no clean Linux path; budget a Windows box/VM/Wine-Blender, or pre-export hkx manually.
- **Can't test in-game** — structural verification only; rely on the user's MO2/Proton loop + the known stale-zip/MO2-reinstall traps in memory.

**Overall:** Build the OAR-set generator (`animations[]` → OAR folder + config.json + IDLE/scene wiring) + a thin `importanim` shell-out (Blender retarget + serde-hkx), **adopt Pandora over Nemesis as the Linux behavior engine**, and treat the Blender→hkx export as the one acknowledged manual/Windows wall.

---

### Sources
Arcane University: Implementation of Custom Animations / CK-CMD for Skyrim / Editing Animation Skeletons · PyNifly (GH BadDogSkyrim) + issue #384 · hkxcmd (GH figment) · hkxconv (GH ret2end) · serde-hkx (GH SARDONYX-sard) + CLI Nexus #126214 · Open Animation Replacer (Nexus #92109, GH ersh1) · DAR-to-OAR Converter (Nexus #93359) · Pandora Behaviour Engine+ (GH Monitor221hz, Nexus #133232) · Nemesis (Nexus #60033) · Step Mods forum (Nemesis Wine thread-race) · MO2 issue #1678 (FNIS on Linux) · HavokBehaviorPostProcess guide (Nexus #2970) · Rokoko / Auto-Rig Pro Remap docs.
