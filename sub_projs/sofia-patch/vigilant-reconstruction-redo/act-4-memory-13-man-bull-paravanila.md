# Act 4 Memory 13 - Man-Bull Paravanila

Status: redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text, not scene phases/actions.

## Structural note: shell quest vs content quest

This memory is unusual. The memory wrapper [`51C038 zzzCHMemoryQuest13 "Man-Bull Paravanila"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:214) is a **header-only shell**: `infodiag 0x51C038` returns **no owned topics**, `scenediag 0x51C038` reports it is not a Scene, and `questdiag` shows it has **no objective** and only 7 stages. `find` for the usual `zzzCHMeQ13` topic prefix returns **0 matches** — this memory does not use a `MeQ13` dialogue namespace at all.

All actual content (objective, dialogue, scene, branches) lives in a separate content quest:
- [`51ADBF zzzCHSubQuest13 "Broken Horn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171) — EditorID prefix `zzzCHSq13` / `zzzCHSubQuest13`.
- Ownership confirmed: `scenediag 0x51D636` reports `quest = 51ADBF`, and `infodiag 0x51ADBF` lists all 6 topics. The `zzzCHMeQ13`-prefixed records the prompt suggested do not exist.

Inference: `51C038` (the memory shell, priority 99) frames/launches the in-world replay; `51ADBF` (`zzzCHSubQuest13`, priority 90) drives the playable scene. Confirm the start link by dumping the shell's aliases / start conditions (TODO — `questdiag` does not print them).

## Shell Quest Record

[`51C038 zzzCHMemoryQuest13 "Man-Bull Paravanila"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:214)

CLI:
- `questdiag Vigilant.esm 0x51C038`
- `infodiag Vigilant.esm 0x51C038` → no owned topics
- `scenediag Vigilant.esm 0x51C038` → not a Scene

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x51C038`
- EditorID: `zzzCHMemoryQuest13`
- Name: `Man-Bull Paravanila`
- Flags: `RunOnce`
- Priority: `99` (highest of the memory quests)
- Type: `Misc`
- Filter: `CH\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | StartUpStage | empty |
| 10 | none | empty |
| 20 | none | empty |
| 30 | CompleteQuest | empty |
| 40 | CompleteQuest | empty |
| 255 | ShutDownStage | empty |
| 999 | CompleteQuest | empty |

Objective: none on the shell (header-only; the objective lives on `51ADBF`).

Stage outcome mapping (disambiguation of 30 / 40 / 999):
- This shell carries **three** `CompleteQuest` stages: `30`, `40`, `999`.
- `999` sits next to `255 ShutDownStage` and is the recurring **end-of-memory shutdown** completion seen across the memory quests (e.g. MeQ08/09 also `CompleteQuest` at 999). Treat `999` as the **memory-shutdown** completion, not a story branch.
- `30` and `40` are the early-band completions and are the candidate **two real outcomes**. On the **content** quest `51ADBF`, the two playable gift branches are both gated `GetStage == 40` (see below), so stage 40 on the shell aligns with "a gift was given / accepted" — i.e. the resolved/mercy path. Stage 30 is the other early completion (player leaves / does not gift). Exact polarity per stage on the shell is **TODO** — the shell's own stage fragments were not decoded; the karma read is taken from the content quest's branch conditions below.

Name note: "Paravanila" in the shell quest Name is a **misspelling of "Paravania"**, the Man-bull NPC [`51AE2D zzzCHAlessiaMntr "Paravania the Man-bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:428). Keep the source spelling in titles; the subject is Paravania. Note: 待驗證 — the title says "Paravania" but the on-screen speaker alias is `BelharzaBull` (see Cast).

## Content Quest Record

[`51ADBF zzzCHSubQuest13 "Broken Horn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171)

CLI:
- `questdiag Vigilant.esm 0x51ADBF`
- `infodiag Vigilant.esm 0x51ADBF`
- `scenediag Vigilant.esm 0x51D636`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x51ADBF`
- EditorID: `zzzCHSubQuest13`
- Name: `Broken Horn`
- Flags: `RunOnce`
- Priority: `90`
- Type: `Misc`
- Filter: `CH\`

Stages from `questdiag` (14): `0 (StartUpStage)`, `1`, `2`, `5`, `10`, `20`, `30`, `40`, `45`, `46`, `50`, `60 (CompleteQuest)`, `255`, `999 (CompleteQuest)`.

Objective:

| Index | Source | Translation |
|---:|---|---|
| 0 | [`Broken horns, sky incarnate.`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:172) | 斷裂之角，蒼穹化身。 |

Objective targets:
- 1 objective, **6 targets** in ESM.
- Targets 1-4 and 6 have 1 condition each; target 5 has 2 conditions.
- Current CLI output does not print target refs; a deeper QUST target dump is needed if target locations matter (TODO).

## Cast / Alias Backbone

Host-quest aliases from `scenediag 0x51D636` (host = `51ADBF`):

| Alias | Name | Fill |
|---:|---|---|
| 1 | `Container` | forcedRef `51ADC1:Vigilant.esm` |
| 2 | `QIHorn` | not filled by CLI print |
| 3 | `QIRing` | not filled by CLI print |
| 4 | `QIScroll` | not filled by CLI print |
| 5 | `BelharzaMan` | uniqueActor [`0E5E2E zzzCHBelharza "Belharza the Man"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:252) |
| 7 | `Boss` | uniqueActor [`51D68A zzzCHBossAmicusTharn "Amicus Tharn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:477) |
| 8 | `BelharzaBull` | uniqueActor [`51D61C zzzCHBelharzaBull "Belharza the Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:466) |
| 9 | `BelharzaMntr` | uniqueActor [`510B22 zzzCHMntrBelharza "Belharza the Man-Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:344) |
| 10 | `Morihaus` | uniqueActor [`0B253B zzzCHBossMorihaus "Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1106) |
| 11 | `MarkerMem` | forcedRef `51D63C:Vigilant.esm` |
| 12 | `MarkerQuiz` | forcedRef `51D63D:Vigilant.esm` |
| 13 | `MarkerES` | forcedRef `51D63E:Vigilant.esm` |
| 14 | `Dragon` | uniqueActor [`51D69A zzzCHMemKahKaanKrein`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:479) |

Inference:
- Three different forms of Belharza are aliased simultaneously: `BelharzaMan` (#5), `BelharzaBull` (#8), `BelharzaMntr` (the Man-Bull, #9). This stages a transformation/lifecycle (man → bull → man-bull), matching the "Broken Horn" theme.
- The **dialogue speaker** throughout is alias `#8 BelharzaBull` (all 4 custom/Hello INFOs condition `GetIsAliasRef alias #8`). The bull cannot speak — every player-facing line is rendered as silent pantomime `"............(…)"`.
- The **scene** speaker is alias `#9 BelharzaMntr` (the Man-Bull), who does speak.

## Scene Records

Scene records are not present as full records in `game-data`; the text lines are linked to `dialogue.md`, while phases/actions are from `scenediag`.

### 51D636 zzzCHSq13Sc01

CLI:
- `scenediag Vigilant.esm 0x51D636`

Staging:
- Host quest: [`51ADBF zzzCHSubQuest13`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171)
- Actor: alias `#9` (`BelharzaMntr`), behaviorFlags `NoPlayerActivation, Optional`
- Phases: 3, each with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Timer`, actor `#9`, phase 0, `0.5` seconds.
  - index 2: `Dialog`, actor `#9`, phase 1, topic [`51D637`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3349), emotion `Neutral`.
  - index 3: `Dialog`, actor `#9`, phase 2, flags `FaceTarget, HeadtrackPlayer`, topic [`51D639`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3352), emotion `Neutral`.
  - index 4: `Package`, actor `#9`, phases 0-2.

Translations:
- [`51D637` / INFO `51D638`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3349): 「我沒想到阿卡托什會派來祂的使者……看來眾神還沒有放棄我。」
- [`51D639` / INFO `51D63A`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3352): 「我早已認命，以為自己再也無法活著、無法變回原本的樣子。謝謝你。」
  - Note: source ends with a stray double period `again. Thank you..`; kept as-is, not a dropped line.

Inference: this scene fires after the player resolves the bull (gives a gift), restoring Belharza the Man-Bull's voice — the "return to my former self" line. It reads as the **mercy/resolution** payoff. Polarity confirmation is TODO (no karma global checked).

## Custom Dialogue Branch: Belharza the Bull (silent)

Host quest: [`51ADBF zzzCHSubQuest13`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171)

Speaker condition pattern:
- Every INFO requires `GetIsAliasRef == 1` on alias `#8` (`BelharzaBull`).
- The two gift branches additionally require `GetStage == 40` on quest `51ADBF` and a player `GetItemCount > 0` for the gift item.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`51D62A zzzCHSubQuest13Hello`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3337) | `51D62B` | none | `GetIsAliasRef alias #8` | 「............（牠望著我，彷彿在懇求著什麼。）」 |
| [`51D62E zzzCHSq13BullB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3340) | `51D62F` | none | `GetIsAliasRef alias #8` | Prompt: 「好可愛的小牛，來摸摸牠吧。」 Response: 「............（牠不喜歡被摸。）」 |
| [`51D631 zzzCHSq13BullB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3343) | `51D632` | `Goodbye` | `GetItemCount > 0` on Player for [`51AD83 zzzCHHornBelhaza "Horn of Belharza"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:990); `GetIsAliasRef alias #8`; `GetStage == 40`; VMAD `CHSq13_TIF__0251D632.Fragment_0` on end | Prompt: 「陛下，這個給您（獻上貝爾哈扎之角）。」 Response: 「............（牠看起來很滿意。）」 |
| [`51D634 zzzCHSq13BullB03T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3346) | `51D635` | `Goodbye` | `GetItemCount > 0` on Player for [`51AD84 zzzCHRingMorihaus "Nosering of Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:991); `GetIsAliasRef alias #8`; `GetStage == 40`; VMAD `CHSq13_TIF__0251D635.Fragment_0` on end | Prompt: 「陛下，這個給您（獻上莫里豪斯的鼻環）。」 Response: 「............（牠看起來很滿意。）」 |

Translation notes:
- All responses are silent pantomime stage directions (the bull has no speech); the meaning is in the parenthetical, kept literal.
- `Majosty` in both gift prompts is a **typo for "Majesty"** in the source. Kept the intent (陛下). Note: 待驗證 (源文拼字).
- DialogBranch records: `B01 = 51D62D`, `B02 = 51D630`, `B03 = 51D633`; DialogView `51D62C`.

## Two-outcome (branch) structure

Both gift branches (`B02` Horn of Belharza, `B03` Nosering of Morihaus) are `Goodbye` + carry a VMAD fragment on end, and both require `GetStage == 40`. These are the two interactive resolutions of the memory:
- Give the **Horn of Belharza** (`51AD83`) — the bull's own heritage relic.
- Give the **Nosering of Morihaus** (`51AD84`) — relic of Morihaus, the Bull of Heaven / Belharza's father.

Polarity: **unresolved from conditions alone.** Both responses are identical ("He seem pleased with it."), both gate the same stage 40 and both have a fragment; `questdiag` does not reveal which fragment routes to which completion. The post-gift scene `51D636` (voice restored, "the gods haven't given up on me") reads as the **mercy/resolution** payoff regardless of which relic is chosen. Decode `CHSq13_TIF__0251D632` and `CHSq13_TIF__0251D635` to label any good/bad split (TODO).

## Related Records

These are not all owned by `51ADBF` per `infodiag`, but they are Belharza/Minotaur/Alessian context for a full reconstruction.

NPCs:
- [`51AE2D zzzCHAlessiaMntr "Paravania the Man-bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:428) — the title subject "Paravania".
- [`51D61C zzzCHBelharzaBull "Belharza the Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:466) — dialogue speaker (alias #8).
- [`510B22 zzzCHMntrBelharza "Belharza the Man-Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:344) — scene speaker (alias #9).
- [`0E5E2E zzzCHBelharza "Belharza the Man"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:252) — alias #5.
- [`0B253B zzzCHBossMorihaus "Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1106) — alias #10, the father.
- [`511D2D zzzCHMemoryAncientMinotaur "Man-Bull of Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:349)
- [`51D68A zzzCHBossAmicusTharn "Amicus Tharn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:477) — alias #7 `Boss`.
- [`51EAA8 zzzCHMntrFollower "Mordog the Man-Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:487)
- [`51D895 zzzCHMntrLeader "Horbahha the Chief"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:482)

Gift items (quest items):
- [`51AD83 zzzCHHornBelhaza "Horn of Belharza"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:990) (EditorID typo: `Belhaza`).
- [`51AD84 zzzCHRingMorihaus "Nosering of Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:991).

Activators / triggers (start + scene hooks, from `find`):
- `51C036 zzzCHManbullMemoryActTrigger` — the memory entry activator (inference: launches the shell `51C038`).
- `51C037 CHMem13ActTriggerRef` (PlacedObject of the above), `51C034 CHMem13StartMarkerRef`, `51C03A CHMem13ReturnMarkerRef`.
- `51C03D zzzCHMem13BabyTrigger "Belharza Shard"`, `51C3D9 zzzCHMem13BullESTrigger "Well of Star Reading"`.
- `51ADBE zzzCHBelharzaQuizActTrigger "Belharza's Monument"` + `51C040 zzzCHMsgBelharzaQuiz` (a quiz message), `51C03F zzzCHBelharzaMonument`.

Locations:
- [`51ADC4 zzzCHMemoryMntrCave "Cradle Cave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:2) / [`51ADC5 zzzCHMemMntrCave "Cave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:549).
- [`51C043 zzzCHCharnelBelharza01 "Concealed Charnel of Belharza"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:78) / [`51C044 zzzCHLocCharnelBelharza`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:612).
- [`51D6B2 zzzAoMManbullCave "Hidden Village of Minotaur"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:125) (AoM-prefixed, the related "Legacy of Belharza" sub-quest world).

## Reconstruction Notes

Source-grounded:
- The memory shell [`51C038 zzzCHMemoryQuest13`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:214) owns no topics/scenes; it is a priority-99 wrapper. Its `30 / 40 / 999` completions: `999` = memory shutdown (next to `255 ShutDownStage`); `30` and `40` are the two early-band outcomes.
- All playable content is in [`51ADBF zzzCHSubQuest13 "Broken Horn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171), objective `Broken horns, sky incarnate.`, with one `SCEN` (`51D636 zzzCHSq13Sc01`) and a 4-INFO bull dialogue set (1 Hello + 3 custom).
- Two interactive gift branches both gated `GetStage == 40` (alias #8 `BelharzaBull`): give Horn of Belharza (`51AD83`) or Nosering of Morihaus (`51AD84`); each carries a `CHSq13_TIF__…` end fragment.
- The dialogue subject (alias #8) is a non-speaking bull; the scene subject (alias #9) is the Man-Bull who speaks the two restored-voice lines.

Garbled / flagged terms:
- Shell Name `Paravanila` → `Paravania` (NPC `51AE2D`). 待驗證.
- Gift prompts `Majosty` → `Majesty`. 待驗證.
- Item EditorID `zzzCHHornBelhaza` (`Belhaza` typo). Source-as-is.
- Scene `51D639` source `Thank you..` (double period). Kept as source.

Quarantine cross-check (≤60% nav only, NOT cited as fact):
- `_gemini-quarantine/.../act-4-exhaustive/memory-13.md` is empty beyond the header. `memory-12-13-final.md` invents topics `zzzCHMeQ13BelharzaB01T01` and a "Belharza" speech ("My mother was the Queen of Slaves…") that **do not exist** in the ESM (`find zzzCHMeQ13` = 0 matches; `infodiag 0x51ADBF` lists only the 6 real silent topics). Those gemini lines are fabricated and are NOT used. Only the objective "Broken horns, sky incarnate." overlaps and is independently verified by `questdiag`.

Open verification:
- decompile `CHSq13_TIF__0251D632` and `CHSq13_TIF__0251D635` to assign which gift routes to which completion + good/bad polarity;
- dump the shell `51C038` aliases / start conditions to confirm it launches `51ADBF` and to label its own `30` vs `40` stage fragments;
- dump `51ADBF` QUST target refs (6 targets) if spatial staging matters;
- inspect the `BelharzaQuiz` activator + `zzzCHMsgBelharzaQuiz` message if the monument quiz is part of this memory's progression.
