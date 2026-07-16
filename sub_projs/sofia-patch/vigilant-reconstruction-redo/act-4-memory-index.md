# Act 4 Memory Quest Index

Status: source-grounded index of all Act IV "memory fragment" quests. Link-first, not a plot summary.

## Source policy

- **Verified backbone** (FormID, EditorID, name, objective text, priority, stage count, `CompleteQuest` branch stages) comes directly from `questdiag` against `Vigilant.esm` and from `game-data/.../quests.md`. These are stated as fact.
- **Per-slice fields** (trigger NPC/item, full `SCEN` list, karma polarity, release/result state) are now ESM/PSC-verified for all 13 (2026-06-14, see Status). Residual `(unverified)` items are CLI structural limits (runtime alias fill, objective target refs), noted per slice.
- `references/` is **≤60% navigation only** — a verification roadmap, never cited as the claim. (The former `_gemini-quarantine/` line-dumps were deleted 2026-06-14 as low-trust; they over-included related-but-not-owned topics.)

CLI:
- `questdiag <ESM> 0x<FormID>` — stages + objectives
- `infodiag <ESM> 0x<FormID> [substr]` — topics a quest owns + INFO conditions
- `scenediag <ESM> 0x<FormID>` — SCEN host/aliases/phases/actions

ESM: `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

## Framing quest (hub)

[`42E0B1 zzzCHMemoryGuide "Memory Guide"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:309)
- flags `AllowRepeatedStages`, priority `99`, type `Misc`, filter `CH\`, 14 stages, single `CompleteQuest` at stage 999.
- 3 objectives (Dylan Thomas, *Do not go gentle into that good night*):
  - obj 100 "Like when the dream no longer needs its dreamer"
  - obj 110 "Against the dying of the light"
  - obj 120 "Blind eyes could blaze like meteors and be"
- Role (inference, TODO verify): the repeatable hub that gates / awards the individual `zzzCHMemoryQuestNN`. Confirm by dumping its aliases + start conditions and which memory quest each stage band starts.

## Master table (verified backbone)

| # | FormID | EditorID | Name | Obj | Pri | Stages | `CompleteQuest` @ | Slice |
|---:|---|---|---|---|---:|---:|---|---|
| 01 | `12C4F4` | zzzCHMemoryQuest01 | The Grand Inquisitor | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:141) | 90 | 10 | 20 / 100 | [done](act-4-memory-01-grand-inquisitor.md) |
| 02 | `13712B` | zzzCHMemoryQuest02 | The Mad King | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38) | 90 | 12 | 30 / 130 | [done](act-4-memory-02-mad-king.md) |
| 03 | `13965A` | zzzCHMemoryQuest03 | Knight of Hound | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:154) | 90 | 12 | 30 / 130 | [done](act-4-memory-03-knight-of-hound.md) |
| 04 | `140225` | zzzCHMemoryQuest04 | Johan the fool | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:297) | 90 | 16 | 60 / 100 | [done](act-4-memory-04-johan.md) |
| 05 | `05AE03` | zzzCHMemoryQuest05 | Ada Bal | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:358) | 90 | 12 | 50 / 120 | [done](act-4-memory-05-ada-bal.md) |
| 06 | `06A23B` | zzzCHMemoryQuest06 | Remain of Miracle | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:372) | 90 | 6 | 30 (single) | [done](act-4-memory-06-remain-of-miracle.md) |
| 07 | `06F53C` | zzzCHMemoryQuest07 | Temptation of Marukh | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:102) | 90 | 13 | 70 / 150 | [done](act-4-memory-07-marukh.md) |
| 08 | `080E91` | zzzCHMemoryQuest08 | The Nameless Bard | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:195) | 90 | 26 | 90 / 230 / 350 / 370 / 999 | [done](act-4-memory-08-nameless-bard.md) |
| 09 | `2CAE30` | zzzCHMemoryQuest09 | From Beyond | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265) | 95 | 14 | 150 / 200 / 999 | [done](act-4-memory-09-from-beyond.md) |
| 10 | `2A532E` | zzzCHMemoryQuest10 | Pelinal the Bloody | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401) | 90 | 40 | 180 / 300 | [done](act-4-memory-10-pelinal.md) |
| 11 | `2B9BAB` | zzzCHMemoryQuest11 | After the Storm | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256) | 90 | 16 | 50 / 340 | [done](act-4-memory-11-after-the-storm.md) |
| 12 | `2BC395` | zzzCHMemoryQuest12 | Last Night | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:307) | 90 | 12 | 50 / 310 | [done](act-4-memory-12-last-night.md) |
| 13 | `51C038`† | zzzCHMemoryQuest13 | Man-Bull Paravanila | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:214) | 99 | 7 | 30 / 40 / 999 | [done](act-4-memory-13-man-bull-paravanila.md) |

† **MeQ13 is a header-only shell**: `51C038` owns no topics/scenes (`find zzzCHMeQ13` = 0). The actual content lives in the content-quest **`51ADBF zzzCHSubQuest13 "Broken Horn"`** (obj "Broken horns, sky incarnate.", quests.md:171). See the slice.

All 13 share: flags `RunOnce`, type `Misc`, filter `CH\`. Stage 999 is `ShutDownStage` where present.

### Branch structure (inference, source-grounded shape only)

The recurring **two-band `CompleteQuest`** pattern — an early stage in the 20–90 band and a later stage in the 100–350 band — is the structural signature of the **good/bad (karma) memory outcome**: the player's choice inside the memory routes to one of two completions. Worked example, verified for MeQ07:
- stage 40 gates the **Alessia** branch opener (`GetStage==40`, alias `#6`); stage 50 gates the **Molag Bal** branch opener (`GetStage==50`, alias `#5`); these feed the 70 vs 150 completions. See [`act-4-memory-07-marukh.md`](act-4-memory-07-marukh.md).

**Which completion is "good" vs "bad" is NOT decidable from `questdiag` alone** — it must be read off the branch dialogue/conditions per quest. Treat the branch-stage column as "two outcomes exist here", not as a polarity assignment.

Exceptions to verify: MeQ06 has a single completion (short, 6 stages) — **confirmed LINEAR** (single speaker/alias, no karma split; see [`act-4-memory-06-remain-of-miracle.md`](act-4-memory-06-remain-of-miracle.md)). MeQ08/09/13 have 3+ `CompleteQuest` stages (multi-outcome, or `CompleteQuest` doubling as shutdown) — needs per-slice disambiguation.

## Per-quest fields (TODO = pull from ESM when slicing)

For every quest below: **Source** = quests.md link above. **Trigger NPC/item**, **SCEN records**, **Karma/good-bad polarity**, **Release/result state** are the per-slice deliverables.

- **07 Temptation of Marukh** — DONE, the format template. Trigger/SCEN/branches reconstructed in [`act-4-memory-07-marukh.md`](act-4-memory-07-marukh.md): 4 SCEN (`0708C7`, `0708CC`, `0708D1`, `0708D6`), Alessia branch (`0731F4`) vs Molag Bal branch (`073200`), Eye of Marukh item gate (`071CE2`). Priority queue below excludes it.

### Subject → quest (ESM-verified per slice)

| Subject(s) | Quest | Slice |
|---|---|---|
| Inquisitor Pepe → "Mary the Dark Virgin" (Dostoevsky Grand Inquisitor) | **MeQ01** The Grand Inquisitor | [01](act-4-memory-01-grand-inquisitor.md) |
| **Dro'zel** the Mad King (actor `137126 zzzCHDrozelMemory`) | **MeQ02** The Mad King | [02](act-4-memory-02-mad-king.md) |
| Knight **Varla** + Emperor **Belharza** + child **Enola** | **MeQ03** Knight of Hound | [03](act-4-memory-03-knight-of-hound.md) |
| **Johann** (player role) + Bard "Bal" (Molag Bal envoy) | **MeQ04** Johan the fool | [04](act-4-memory-04-johan.md) |
| **Marukh** + **Pepe** | **MeQ05** Ada Bal | [05](act-4-memory-05-ada-bal.md) |
| **Pepe** (Inquisitor interrogation) | **MeQ06** Remain of Miracle | [06](act-4-memory-06-remain-of-miracle.md) |
| **Marukh / Alessia / Dulsa** | **MeQ07** Temptation of Marukh | [07](act-4-memory-07-marukh.md) |
| The Nameless **Bard** + **Lamae** + Volar | **MeQ08** The Nameless Bard | [08](act-4-memory-08-nameless-bard.md) |
| **Lamae** + **Sheogorath** + Tsun | **MeQ09** From Beyond | [09](act-4-memory-09-from-beyond.md) |
| **Pelinal / Mary / Umaril** | **MeQ10** Pelinal the Bloody | [10](act-4-memory-10-pelinal.md) |
| **Morihaus** mourning Pelinal + Stuhn priest | **MeQ11** After the Storm | [11](act-4-memory-11-after-the-storm.md) |
| **Pelinal (Paravant) + Alessia (Perrif)** reunion + Akatosh | **MeQ12** Last Night | [12](act-4-memory-12-last-night.md) |
| **Paravania the Man-bull** + Belharza + Morihaus | **MeQ13** (shell → `zzzCHSubQuest13` Broken Horn) | [13](act-4-memory-13-man-bull-paravanila.md) |

Corrections to earlier guesses: **Dro'zel is MeQ02's confirmed subject** (he also appears in `zzzCHsq*` side-quests — both true). **Hasaama / Martha** are `zzzCHsq*` side-quest subjects, not memory quests. **Mary** appears in both MeQ01 (addressee) and MeQ10 (Umaril's slave).

### Naming gotchas (the `find zzzCHMeQNN` recipe is not uniform)

- **MeQ02** records use prefix `zzzCHMeQ2King…` (single digit, no zero-pad); `find zzzCHMeQ02` returns nothing.
- **MeQ13** `51C038` is a header-only shell; content lives under `zzzCHSq13…` / quest `51ADBF zzzCHSubQuest13`.
- When `find zzzCHMeQNN` is empty, try `zzzCHMeQ<n>` (no pad), `zzzCHSq<NN>`, or grep the scene/topic owner via `infodiag` on the quest FormID.

## Status: all 13 sliced + PSC-verified (2026-06-14)

Every memory quest + the template (07) has a source-grounded slice (see Slice column), and the cross-cutting **Open verification** items are now resolved per-slice via the BSA PSC source cache.

**Method breakthrough (2026-06-14)**: the ModForge CLI has no VMAD/pex decompiler, but **`Vigilant.bsa` ships uncompressed `scripts/source/*.psc` plaintext**. Extracted into `_bsa-psc-cache/` (gitignored) by `_tools/bsa_reader.py` (a from-scratch SSE v105 BSA reader — no `bsab`/`bsarch` on the machine, files are uncompressed so no LZ4 needed). Reading the `qf_*` / `chmeqNN_tif__*` / `sf_*` fragments directly gives `choice → SetStage → CompleteQuest`, `Karma.Mod`, `ModRadiance`, and `qGuide.SetStage` wiring — far more direct than decompiling pex.

Resolved across all 13:
- **Branch polarity / stage routing** — RESOLVED via TIF + QF PSC per quest.
- **Karma polarity / result state** — RESOLVED; global = `0x020B19F4 zzzCHKarma` (GlobalFloat). MeQ06 is linear (no karma); the rest award ±3 (MeQ08 uses ±5) on good/bad branches. MeQ13 awards two independent +3 (shell Paravania dream + SubQuest13 Belharza gift), no bad branch.
- **SCEN staging** — RESOLVED via `scenediag` + `sf_*` fragments.
- **Hub wiring** — `qf_zzzchmemoryguide_0242e0b1.psc` + `chmemoryguidequestscript.psc` read; per-dream completion via `qGuide.SetStage(NN)` + TraceON/OFF polling. Song-of-Pelinal (Dream10–12) drives hub objectives 100/110/120.

Still open (CLI structural limits, per-slice noted as `(unverified)`):
- **Runtime alias fill**: `scenediag`/`questdiag` do not print forcedRef on non-unique aliases or objective target refs — needs a direct ESM QUST alias/CTDA dump.
- A few quest-specific items (MeQ13 shell scene SF, quiz message bodies) — see each slice's Open verification.

## Verification backlog (method per field)

- **Trigger NPC/item**: dump the quest's QUST aliases + alias fill (forcedRef / uniqueActor / find-condition) and the start-game-enabled refs. `questdiag` currently does not print target refs (noted in the 07 slice); needs a richer alias/target dump or direct ESM read.
- **SCEN records**: find the quest's scene FormIDs (`infodiag` the quest to surface scene-owned topics, confirm owner), then `scenediag` each for host/aliases/phases/actions/timer/topic — per the README standard. Cross-check the `sf_*` scene fragment PSC in cache for `SetStage` on scene end.
- **Karma polarity**: read the branch-opener INFO conditions (`GetStage==`, `GetIsAliasRef`) and the branch content to label which completion is the "mercy/good" vs "corruption/bad" outcome. Cross-check the karma global if one exists.
- **Release/result state**: what the memory grants on each completion (item, faction, global, world change) — read the stage fragments / `CompleteQuest` stage effects.

## Navigation pointers (≤60%, verify everything)

Primary working source: the PSC plaintext cache `_bsa-psc-cache/` (gitignored, regenerate with `_tools/bsa_reader.py`) — `qf_*` quest fragments, `chmeqNN_tif__*` / `chsq*_tif__*` dialogue choice fragments, `sf_*` scene fragments.

Secondary references (verification roadmap only): [`references/zhihu-vigilant-review-notes.md`](references/zhihu-vigilant-review-notes.md), [`references/video-transcript-notes.md`](references/video-transcript-notes.md).
