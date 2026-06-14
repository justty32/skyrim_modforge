# Animation §6–8 — Linux reality / end-to-end / ModForge integration

← [animation index](README.md)

> **2026-06-14 status correction:** Nemesis and FNIS are now **legacy/deprecated**; **Pandora Behaviour Engine+** is the de-facto standard behavior patcher and **the baseline these notes assume.** Prior wording that treated Nemesis as the baseline has been replaced. Sources: [Pandora wiki](https://github.com/Monitor221hz/Pandora-Behaviour-Engine-Plus/wiki) + [README](https://github.com/Monitor221hz/Pandora-Behaviour-Engine-Plus) (researched 2026-06-14).

## 6. Linux/Proton reality for the integration tools (the likely pain point)

### 6.0 Pandora Behaviour Engine+ — the 2026 baseline (what replaced Nemesis/FNIS)

**What it is.** The modern, **open-source (GPL v3), .NET 8, cross-platform** behavior patcher — the successor to FNIS and Nemesis. Author Monitor221hz; Nexus #133232, GH `Monitor221hz/Pandora-Behaviour-Engine-Plus`. Why it displaced the old tools: open source + actively maintained, **faster patching**, cleaner UI/logging, **and broad compatibility — works with all Nemesis mods and most FNIS mods**, so adopting it does not strand existing animation mods. It explicitly *drops* FNIS/Nemesis features that already have a modern replacement (e.g. PCEA → OAR, baked motion → AMR), so it is "Nemesis/FNIS minus the obsolete bits," not a strict superset.

**How it works.** Same job as Nemesis: it is a **behavior patcher** — reads the per-mod edits to Skyrim's Havok behavior graph and *merges* them into a single conflict-free output, so multiple animation mods that touch the same graph coexist. It consumes patch data in **two formats**: (1) the **Nemesis patch structure** (backwards-compatible — existing Nemesis-format mods just work), and (2) a **native Pandora format** — a single XML file per behavior graph using `replace`/`insert`/`append`/`loose` operations with XPath-style element targeting (e.g. `#xxxx\path\to\element`). Output is **game-ready Havok 2010 binary `.hkx`** (exported via the bundled HKX2E, not hkxcmd) — i.e. patched behavior graphs functionally equivalent to Nemesis/FNIS output, written to a **Pandora Output** folder you point at the game's `Data` (or an MO2 mod). OAR/DAR are runtime *replacers* layered on top of whatever behavior baseline Pandora generates — Pandora builds the graph, OAR swaps clips within it at runtime.

**Linux / headless — the key facts (per wiki+README, researched 2026-06-14):**
- **Runtime:** requires the **.NET 8 Desktop Runtime**. The UI is a desktop GUI app (not a pure console tool).
- **Platforms:** officially **Windows / Linux / macOS**, but **"only Windows is extensively tested."** For SteamOS/Linux the team's *recommended* path is to **Proton-wrap the self-contained Windows build** rather than rely on the native Linux build. So "native Linux" exists but is the less-trodden path; budget hands-on verification on the Manjaro box.
- **Automation args (NOT true headless):** `--auto_run` (run with the mods cached from the last successful run, via `ActiveMods.txt`), `--auto_close` (close after one run), `-o`/`--output "<dir>"` (output dir), `--tesv:"<gamedir>"` (game/Data path for MO2/Wabbajack/multi-install), `--skyrim_debug64` (emit debug XML). **Caveat:** these automate the GUI; a fully **GUI-less / headless mode is still an OPEN feature request** (GH issue #114, unresolved). So on a headless box you still need a display (real or virtual, e.g. `xvfb-run` / a Proton-provided surface) — Pandora is "GUI you can auto-drive," not a console binary. **Mark this for hands-on verification on Manjaro.**
- **Library use:** there is a **.NET plugin API** (`IEngineConfigurationPlugin` + `plugin.json`, dropped in a `Plugins/` folder) for *extending* Pandora, but it is explicitly **unstable / breaking without notice**, and Pandora ships as an app, not a referenceable NuGet. So ModForge driving Pandora as an embedded library is **not** a supported path today — treat integration as **shell-out**, same as Papyrus/xLODGen.

| Tool | Role | Linux verdict |
|---|---|---|
| **Pandora Behaviour Engine+** (**the baseline**) | modern Nemesis/FNIS replacement; behavior-graph patch gen | **✅ Best Linux option, and now the default.** Open-source .NET 8 (needs .NET 8 Desktop Runtime), Windows/Linux/macOS, consumes **Nemesis format + native Pandora XML**, automatable via `--auto_run`/`--auto_close`/`-o`/`--tesv`. Caveats: "only Windows extensively tested" (team suggests Proton-wrapping the Windows build); **true headless is still open (issue #114)** → needs a display even when automated. See §6.0. |
| **Nemesis** (legacy) | behavior-graph patch gen (Windows exe) | **⚠️ Legacy + problematic under Wine/Proton** — documented thread-race bugs that hang/fail. Superseded by Pandora (which reads Nemesis-format mods). Do **not** baseline on it. |
| **FNIS** (`GenerateFNISforUsers.exe`, legacy) | behavior gen (Windows exe) | **Legacy.** Runs under Wine with caveats (pre-create the tools dir, can appear frozen on large loadorders, prune Languages to English). Superseded by Pandora (compatible with most FNIS mods). |
| **OAR / DAR runtime** | SKSE plugin loaded by the game | Runs **inside the game** → works wherever SSE+SKSE works under Proton (your baseline). No separate Linux step. Layers on top of Pandora's behavior baseline. |
| **serde-hkx (`hkxc`)** | hkx↔XML, win32↔amd64 | **✅ Native Linux (Rust).** |
| **ck-cmd / hkxcmd / HavokBehaviorPostProcess** | FBX→hkx, LE→SE | Windows; Wine, version-fiddly. |
| **PyNifly** | Blender direct hkx export | **❌ Windows-only.** The hardest Linux gap. |

**Bottom line for Manjaro:** the *engine* problem is solved by **standardizing on Pandora** (open .NET 8, reads Nemesis format, automatable) — Nemesis/FNIS are legacy and Nemesis-under-Wine is a non-goal. The *conversion* problem is solved by **serde-hkx** (native). The *Blender→hkx export* problem is **not** cleanly solved on Linux (PyNifly Windows-only; Linux-capable Blender hkx addons are version-locked + require a win32 intermediate). **One open Linux unknown to verify hands-on:** whether Pandora's native Linux build runs cleanly on Manjaro, or whether we must Proton-wrap the Windows build, and whether `--auto_run/--auto_close` can be driven without a real display.

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
6. **[SHELL-OUT, Linux unverified]** Run **Pandora** (`--auto_run --auto_close -o "<out>" --tesv:"<gamedir>"`) to (re)generate the behavior baseline. .NET 8; native Linux build *exists but is under-tested* — may need Proton-wrapping the Windows build, and automated runs may still need a display (true headless is open issue #114). **Verify on Manjaro.** *(Skip for pure replacer (a) if behavior already generated.)* See [§6.0](#60-pandora-behaviour-engine--the-2026-baseline-what-replaced-nemesisfnis).
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
