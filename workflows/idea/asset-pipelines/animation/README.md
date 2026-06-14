# Animation → Skyrim SE Asset Pipeline

← index: [asset-pipelines](../README.md) · related: [IDEAS.md §14](../../ideas.md) (Havok = "the wall"), memory `scene-playidle-recipe`, [03-3d-model-import](../03-3d-model-import.md) (rigged-mesh handoff)

**Research date:** 2026-06-08. Turning an arbitrary animation clip (FBX/BVH/mocap/AI-generated) into a *triggerable Skyrim SE asset*, on Manjaro Linux (Blender native, SSE via MO2/Proton), personal/single-player. Focus is the **conversion + integration pipeline** — the user's framing: animation *content* is no longer scarce (free packs, AI motion gen, phone mocap); the missing thing is the **workflow to turn a clip into a usable asset.**

**Confidence note:** This is the single most fragile, version-sensitive corner of Skyrim modding. The record layer (ModForge's wheelhouse) is deterministic; the Havok layer is closed-SDK reverse-engineering held together by version-sensitive community tools. Uncertainty flagged inline. **Headline good news for Linux:** the historically-hardest walls are now solved natively — the *behavior-patch engine* by **Pandora** (.NET, cross-platform), the *hkx conversion* by **serde-hkx** (pure Rust). The remaining wall is the Blender→hkx *export* step (best tool PyNifly is Windows-only).

## 子頁

| 檔 | 內容 |
|----|------|
| [havok-blender.md](havok-blender.md) | §2 Havok 格式牆 + §3 Blender→Skyrim 路徑 + §4 mocap/AI 重定向 |
| [integration-layer.md](integration-layer.md) | §5 讓動畫真的「播得出來」的整合層（replacer / IDLE / **OAR**——真正的交付物）|
| [linux-workflow-modforge.md](linux-workflow-modforge.md) | §6 Linux/Proton 工具現實 + §7 端到端流程 + §8 ModForge 整合（`animations[]` / `importanim`）|

---

## 1. The Skyrim animation stack — what an "animation asset" actually is

A Skyrim "animation" is **four layers**, and dropping a file in a folder does nothing unless a higher layer references it. The core mental model:

1. **The clip — `.hkx` (Havok binary).** One motion under `Meshes\actors\character\animations\...`: per-bone transforms over time + **annotations**. Skyrim only honors **root Z-translation + X/Y-rotation** (rest of root motion stripped on import).
2. **The skeleton — `skeleton.nif` + `skeleton.hkx`.** Bone hierarchy + rest pose, `NPC <name> [tag]` naming. An animation is authored *against a skeleton*; mismatched bone names/hierarchy → T-pose/garbage. An animation + the skeleton it plays on **must be the same rig.**
3. **The behavior graph — Havok behavior `.hkx` (the hard part).** State machines (`0_master.hkx`, `defaultmale.hkx`…) deciding *when* a clip plays, transitions, conditions. **The gatekeeper.** A clip the graph never references is dead weight. These are "not designed to be modified" — the reason FNIS/Nemesis/Pandora exist (they *patch the graph for you*).
4. **Animation events / annotations.** Named events (`weaponSwing`, idle tags) the graph reacts to; the engine/Papyrus fire them (`Debug.SendAnimationEvent`, `PlayIdle`). **An IDLE record is essentially a named handle the graph already exposes** — which is why ModForge's existing `PlayIdle` works *only* for vanilla IDLEs the vanilla behavior already drives.

**Stated bluntly:** content is layer 1; *playability* lives in layer 3. The whole difficulty of "adding an animation" is **getting layer 3 to reference your layer-1 file** without hand-editing an undocumented binary state machine. Every framework in [integration §5c](integration-layer.md) exists to automate that one thing.

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
