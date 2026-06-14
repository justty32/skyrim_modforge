# Animation §6–8 — Linux reality / end-to-end / ModForge integration

← [animation index](README.md)

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

**Bottom line for Manjaro:** the *engine* problem is solved by **switching Nemesis → Pandora** (native, reads Nemesis format, headless). The *conversion* problem by **serde-hkx** (native). The *Blender→hkx export* problem is **not** cleanly solved on Linux (PyNifly Windows-only; Linux-capable Blender hkx addons are version-locked + require win32 intermediate). **Recommendation: re-baseline the assumed stack from Nemesis → Pandora** for Linux viability; treat Nemesis-under-Wine as a non-goal. *(This contradicts IDEAS §11-C's "Nemesis" baseline — flagged in [asset-pipelines README](../README.md).)*

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
