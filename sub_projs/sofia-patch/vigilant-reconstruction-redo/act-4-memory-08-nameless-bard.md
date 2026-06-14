# Act 4 Memory 08 - The Nameless Bard

Status: redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text, not scene phases/actions.
- The English source is machine-translated from Japanese and is heavily garbled; garbled terms are kept verbatim and flagged `Note: 待驗證`, never overwritten with a clean guess.

## Quest Record

[`080E91 zzzCHMemoryQuest08 "The Nameless Bard"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)

CLI:
- `questdiag Vigilant.esm 0x080E91`
- `infodiag Vigilant.esm 0x080E91`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x080E91`
- EditorID: `zzzCHMemoryQuest08`
- Name: `The Nameless Bard`
- Flags: `RunOnce`
- Priority: `90`
- Type: `Misc`
- Filter: `CH\`

Stages from `questdiag` (26):

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 10 | none | empty (3 log entries, all empty) |
| 20 | none | empty |
| 25 | none | empty |
| 30 | none | empty |
| 40 | none | empty |
| 50 | none | empty |
| 60 | none | empty |
| 70 | none | empty |
| 80 | none | empty |
| 90 | CompleteQuest | empty |
| 100 | none | empty |
| 200 | none | empty |
| 210 | none | empty |
| 220 | none | empty |
| 230 | CompleteQuest | empty |
| 240 | none | empty |
| 300 | none | empty |
| 310 | none | empty |
| 320 | none | empty |
| 330 | none | empty |
| 340 | none | empty |
| 350 | CompleteQuest | empty |
| 360 | none | empty |
| 370 | CompleteQuest | empty |
| 999 | ShutDownStage, CompleteQuest | empty |

Objective:

| Index | Source | Translation |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:195) | 「滾石躺在火裡，無人拾起。」 |

Note: objective text `The rolling stones are in the fire and are not picked up` is garbled machine-English; the same "rolling stone / pebble" image recurs in scene topic [`0821F3`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:985) (`Let it kicked a pebble rolling`). 待驗證.

Objective targets:
- 3 targets in ESM.
- Target 1 has 1 condition.
- Target 2 has 1 condition.
- Target 3 has 0 conditions.
- Current CLI output does not print target refs; needs a deeper QUST target dump if target locations matter.

## Alias / Staging Backbone

All six `SCEN` records below share the same host quest and the same 19-alias list.

Host quest:
- [`080E91 zzzCHMemoryQuest08`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)

Host-quest aliases from `scenediag` (19):

| Alias | Name | Fill |
|---:|---|---|
| 0 | `StartMarker` | forcedRef `07FA2D:Vigilant.esm` |
| 1 | `EndMarker` | forcedRef `080E92:Vigilant.esm` |
| 2 | `Lamae` | uniqueActor [`080E93 zzzCHLamaeMemory "Lamae Beolfag"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1073) |
| 3 | `Facis` | uniqueActor [`080E98 zzzCHLamaeFollowerMemory "Facis"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1066) |
| 4 | `MolagTE` | uniqueActor [`080E96 zzzCHMolagBalInMemoryTE`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1067) |
| 5 | `TA01` | forcedRef `0821E9:Vigilant.esm` |
| 6 | `TEMarker` | forcedRef `080E95:Vigilant.esm` |
| 7 | `TA02` | forcedRef `08220E:Vigilant.esm` |
| 8 | `MolagBE` | uniqueActor [`0875EF zzzCHMolagBalInMemoryBE`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1062) |
| 9 | `BEMarker` | forcedRef `0875EB:Vigilant.esm` |
| 10 | `TA03` | forcedRef `0875F2:Vigilant.esm` |
| 11 | `TA04` | forcedRef `08B5A8:Vigilant.esm` |
| 12 | `TA05` | forcedRef `08B5AA:Vigilant.esm` |
| 13 | `WEMarker` | forcedRef `088BBC:Vigilant.esm` |
| 14 | `Volar` | uniqueActor [`088BC8 zzzCHDeathBringerMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1059) |
| 15 | `Laza` | uniqueActor [`2E47E5 zzzCHMemoryLaza "Laza"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:806) |
| 16 | `GuideBard` | forcedRef `42E0B7:Vigilant.esm` |
| 17 | `GuideStatue` | forcedRef `42F43E:Vigilant.esm` |
| 18 | `GuideTower` | forcedRef `4307C5:Vigilant.esm` |

Subject of the memory:
- The **Nameless Bard** = the player-controlled memory actor whose name the player chooses through the Molag dialogue branches (see Custom Dialogue branches below). Aliases `MolagTE`/`MolagBE` are the two Molag-Bal apparitions the bard speaks to; the player names himself (or refuses) to each.
- **Lamae** (alias `#2`, [`080E93`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1073)) is the girl the bard sang to. Lamae also appears across MeQ09 `From Beyond`; only the MeQ08-owned `080E93 zzzCHLamaeMemory` record is included here (cross-link the MeQ09 Lamae separately).

Inference:
- `TA01`–`TA05` (aliases `#5`,`#7`,`#10`,`#11`,`#12`) are the scene-monologue forcedRefs that carry the bard's narration lines across the five `*ESc` scenes (one TA actor per scene). Inferred from each `*ESc` scene using exactly one TA actor.
- `TEMarker`/`BEMarker`/`WEMarker` (aliases `#6`,`#9`,`#13`) are the staging markers for the **T**rue-**E**nding, **B**ad-**E**nding(?), and **W**est-**E**nding(?) scene blocks (inference from naming + which scenes reference which alias). 待驗證: the exact good/bad polarity of each marker — see stage-outcome map below.
- `Guide*` aliases (`#16`–`#18`) tie back to the `zzzCHMemoryGuide` hub (`42E0B1`), same as in MeQ07.

## Scene Records

Scene records are not present as full records in `game-data`; the text lines are linked to `dialogue.md`, while phases/actions are from `scenediag`.

### 080EA5 zzzCHMeQ08Sc01 — Lamae & Facis intro

CLI:
- `scenediag Vigilant.esm 0x080EA5`

Staging:
- Host quest: [`080E91 zzzCHMemoryQuest08`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)
- Flags: none
- Actors: alias `#3` (`Facis`, `DeathEnd NoPlayerActivation`) and alias `#2` (`Lamae`, `DeathEnd NoPlayerActivation`)
- Phases: 6, each 0 start conds / 1 complete cond.
- Actions: 10 total — Package movement on `#3`/`#2` interleaved with three Dialog actions:
  - index 3: `Dialog` actor `#3` (Facis), phase 1, topic [`080EA7`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:964), headtrack actor `#2`, emotion `Neutral`.
  - index 4: `Dialog` actor `#2` (Lamae), phase 2, topic [`080EA9`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:967), headtrack actor `#3`, emotion `Neutral`.
  - index 8: `Dialog` actor `#2` (Lamae), phase 4, topic [`080EAE`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:970), `FaceTarget HeadtrackPlayer`, emotion `Neutral`.

Translations:
- [`080EA7` / INFO `080EA8`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:964) (Facis, Surprise): 「夫人，原來您躲在這裡。Shorl 大人在等您。」
  - Note: `Lord Shorl` 為專有名詞，拼寫待驗證。`Did you fold here` 機翻不清，依語境譯為「躲在這裡」。待驗證。
- [`080EA9` / INFO `080EAA`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:967) (Lamae, Happy): 「好，我現在就過去。」
- [`080EAE` / INFO `080EAF`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:970) (Lamae, Happy): 「再會。」

### 0821E7 zzzCHMeQ08Sc02 — abduction narration (TA01)

CLI:
- `scenediag Vigilant.esm 0x0821E7`

Staging:
- Host quest: [`080E91`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)
- Flags: none
- Actor: alias `#5` (`TA01`), behavior `DeathEnd, CombatEnd, DialoguePause`
- Phases: 2, each 0 start / 1 complete cond.
- Actions:
  - index 1: `Dialog` actor `#5`, phase 0, topic [`0821EA`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:973), emotion `Neutral`.
  - index 2: `Dialog` actor `#5`, phase 1, topic [`0821EC`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:976), emotion `Neutral`.

Translations:
- [`0821EA` / INFO `0821EB`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:973): 「不久之後，她的尖叫聲在天霜的峽灣裡迴盪。我記得我抱著昏迷的她，朝村子走去。」
- [`0821EC` / INFO `0821ED`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:976): 「然而她再也沒有醒來；一道光輝就此自 Nirn 消失。」
  - Note: `shine one was was lost from Nirn`（重複 `was`）機翻破碎，依語境譯為「一道光輝自 Nirn 消失」。待驗證。

### 0821EE zzzCHMeQ08TESc — True-Ending(?) narration (TA02)

CLI:
- `scenediag Vigilant.esm 0x0821EE`

Staging:
- Host quest: [`080E91`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)
- Flags: none
- Actors: alias `#5` (`TA01`) and alias `#7` (`TA02`), both `DeathEnd, CombatEnd, DialoguePause`
- Phases: 4, each 0 start / 1 complete cond.
- Actions:
  - index 4: `Timer` actor `#7`, phase 0, `0.1` s.
  - index 1: `Dialog` actor `#7`, phase 1, topic [`0821EF`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:979), emotion `Neutral`.
  - index 2: `Dialog` actor `#7`, phase 2, topic [`0821F1`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:982), emotion `Neutral`.
  - index 3: `Dialog` actor `#7`, phase 3, topic [`0821F3`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:985), emotion `Neutral`.

Translations:
- [`0821EF` / INFO `0821F0`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:979): 「很久以前，Eldergleam 還年輕，尚未被深埋於黑暗大地之底。那時的世界滿是魔法、奇異與危險。」
- [`0821F1` / INFO `0821F2`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:982): 「即便如此，我仍不向殘酷的命運屈服。因為我知道，這份苦難終有一天也會像可憐的幻影般消散。」
  - Note: `cruel fate I` 的 `I` 為機翻贅字。待驗證。
- [`0821F3` / INFO `0821F4`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:985): 「即使流了血、犯下種種過錯，人也能得到救贖。就讓它像被踢動的石子一樣滾落，伴著餘下的歌聲。」
  - Note: 此句的「滾石／踢動的石子」對應任務目標 [`The rolling stones … not picked up`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:195)。

### 0875F1 zzzCHMeQ08BESc — Bad-Ending(?) narration (TA03)

CLI:
- `scenediag Vigilant.esm 0x0875F1`

Staging:
- Host quest: [`080E91`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)
- Flags: none
- Actor: alias `#10` (`TA03`), `DeathEnd`
- Phases: 4, each 0 start / 1 complete cond.
- Actions:
  - index 4: `Timer` actor `#10`, phase 0, `0.1` s.
  - index 1: `Dialog` actor `#10`, phase 1, topic [`0875F3`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1006), emotion `Neutral`.
  - index 2: `Dialog` actor `#10`, phase 2, topic [`0875F5`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1009), emotion `Neutral`.
  - index 3: `Dialog` actor `#10`, phase 3, topic [`0875F7`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1012), emotion `Neutral`.

Translations:
- [`0875F3` / INFO `0875F4`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1006): 「很久以前，Eldergleam 還年輕，尚未被深埋於黑暗大地之底。那時的世界滿是魔法、奇異與危險。」（與 `0821EF` 同一段開場，重複使用。）
- [`0875F5` / INFO `0875F6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1009): 「那一天，我在她已徹底改變的額頭上滴下一滴血。我只盼她能永遠安息，卻反而引來了不死的詛咒。」
  - Note: `just baiting the curse of immortality` 機翻不清，依語境譯為「反而引來不死的詛咒」。待驗證。
- [`0875F7` / INFO `0875F8`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1012): 「無法被救贖的血與淚……罪永不得赦免。石頭染上熱意，緩緩烤炙著我的身體。」
  - Note: `and Charles` 為機翻雜訊（疑似日文助詞誤譯），已略去。待驗證。

### 08B5AD zzzCHMeQ08WESc01 — West-Ending(?) narration (TA04)

CLI:
- `scenediag Vigilant.esm 0x08B5AD`

Staging:
- Host quest: [`080E91`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)
- Flags: none
- Actor: alias `#11` (`TA04`), `DeathEnd, CombatEnd, DialoguePause`
- Phases: 1, 0 start / 1 complete cond.
- Actions:
  - index 1: `Dialog` actor `#11`, phase 0, topic [`08B5AE`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1030), emotion `Neutral`.

Translations:
- [`08B5AE` / INFO `08B5AF`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1030): 「我是懦夫，只活在不斷重複的夢裡。從一開始，我就注定被自己親手養大的黑暗吞噬……」

### 08B5B6 zzzCHMeQ08WESc02 — Molag Bal self-naming (TA05)

CLI:
- `scenediag Vigilant.esm 0x08B5B6`

Staging:
- Host quest: [`080E91`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)
- Flags: none
- Actor: alias `#12` (`TA05`), `DeathEnd, CombatEnd, DialoguePause`
- Phases: 2, each 0 start / 1 complete cond.
- Actions:
  - index 1: `Dialog` actor `#12`, phase 0, topic [`08B5B7`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1039), emotion `Neutral`.
  - index 2: `Dialog` actor `#12`, phase 1, topic [`08B5B9`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1042), emotion `Neutral`.

Translations:
- [`08B5B7` / INFO `08B5B8`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1039): 「我的名字是……Molag Bal。奴役與屈辱之王，靈魂吞噬者，詛咒諸神之世界的存在。」
- [`08B5B9` / INFO `08B5BA`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1042): 「一切都會重演。認識黑暗，打破黑暗，超越死亡。」

## Custom Dialogue Branch: Lamae (zzzCHMeQ08LamaeB01)

Branch:
- `080E9C:Vigilant.esm` (`zzzCHMeQ08LamaeB01`), view `080E9B` (`zzzCHMeQ08LamaeView`)

Speaker condition pattern:
- All INFOs require `GetIsAliasRef == 1` on alias `#2` (`Lamae`).
- Opening line also requires `GetStage == 10` on quest `080E91`.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`080E9D zzzCHMeQ08LamaeB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:952) | `080E9E` | none | `GetStage == 10`; `GetIsAliasRef #2` | (Happy) 「能不能……再讓我聽聽那首歌剩下的部分？」 Note: `It is not you please let the rest of that song?` 機翻破碎。待驗證。 |
| [`080E9F zzzCHMeQ08LamaeB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:955) | `080EA0` | none | `GetIsAliasRef #2` | Prompt 「故事的後續？」 Response (Happy) 「對，後續。你想想——一個殘酷又悲傷的故事，會變成怎樣一個了不起、滿溢幸福的結局呢？」 Note: 原文 `What you I'm a story cruel sad story…` 文法破碎。待驗證。 |
| [`080EA1 zzzCHMeQ08LamaeB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:958) | `080EA2` | `Goodbye` | `GetIsAliasRef #2`; VMAD `CHMeq08_TIF__02080EA2.Fragment_0` on end | Prompt 「沒有什麼後續了。他們就那樣荒謬地被殺死。」 Response (Sad) 「這樣啊……我有點失望。」 |
| [`080EA3 zzzCHMeQ08LamaeB01T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:961) | `080EA4` | `Goodbye` | `GetIsAliasRef #2`; VMAD `CHMeq08_TIF__02080EA4.Fragment_0` on end | Prompt 「下次見面前，我會把剩下的編好。」 Response (Happy) 「我會好好想著的。說好了。」 |

Branch note (inference): the two player choices `080EA1` (no continuation / let her die in the tale) vs `080EA3` (I'll finish the song) are the first karma fork; both carry `CHMeq08_TIF__*` end-fragments that likely set stage. 待驗證 via the Papyrus fragments.

## Custom Dialogue Branch: Molag (TE) — refusal (zzzCHMeQ08MolagTB01)

Branch:
- `0821FA:Vigilant.esm` (`zzzCHMeQ08MolagTB01`), view `0821F9` (`zzzCHMeQ08MolagTView`)

Speaker condition pattern:
- INFOs require `GetIsAliasRef == 1` on alias `#4` (`MolagTE`).
- Opener requires `GetStage == 60`.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0821FB zzzCHMeQ08MolagTB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:988) | `0821FC` | `WalkAway` | `GetStage == 60`; `GetIsAliasRef #4` | (Neutral) 「我無法理解。你為何拒絕……只要你願望，就能讓那女孩復活，不是嗎？」 |
| [`0821FD zzzCHMeQ08MolagTB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:991) | `0821FE` | `Goodbye` | `GetIsAliasRef #4`; VMAD `CHMeq08_TIF__020821FE.Fragment_0` on end | Prompt 「你永遠不會懂。」 Response (Neutral) 「…………」 |

## Custom Dialogue Branch: Molag (TE) — naming (zzzCHMeQ08MolagTB02)

Branch:
- `0821FF:Vigilant.esm` (`zzzCHMeQ08MolagTB02`)

Speaker condition pattern:
- INFOs require `GetIsAliasRef == 1` on alias `#4` (`MolagTE`).
- Opener requires `GetStage == 70`.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`082200 zzzCHMeQ08MolagTB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:994) | `082201` | none | `GetStage == 70`; `GetIsAliasRef #4` | (Neutral) 「等等……我還沒聽到你的名字……」 |
| [`082202 zzzCHMeQ08MolagTB02T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:997) | `082203` | `Goodbye, SayOnce` | `GetIsAliasRef #4`; VMAD `…02082203.Fragment_0` | Prompt 「<Alias=Player>。是她給了我這名字。」 Response (Neutral) 「好名字……我會記在靈魂裡。」 |
| [`082204 zzzCHMeQ08MolagTB02T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1000) | `082205` | `Goodbye, SayOnce` | `GetIsAliasRef #4`; VMAD `…02082205.Fragment_0` | Prompt 「Stendll。我是哈芬納的 Stendll。」 Response (Neutral) 「奇怪的名字……我會記在靈魂裡。」 Note: `Stendll` / `Strange neme` 拼寫待驗證。 |
| [`082206 zzzCHMeQ08MolagTB02T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1003) | `082207` | `Goodbye, SayOnce` | `GetIsAliasRef #4`; VMAD `…02082207.Fragment_0` | Prompt 「我已捨棄了我的名字。」 Response (Neutral) 「……真是可悲。」 |

Inference: the three mutually-exclusive `SayOnce` choices here are the **naming fork** — accept the name Lamae gave / give your own name (`Stendll`) / discard your name — each ending the TE branch through a distinct fragment. This is the player-facing identity choice that gives the quest its title "The Nameless Bard". 待驗證 polarity via fragments.

## Custom Dialogue Branch: Molag (BE) (zzzCHMeQ08MolagBB01)

Branch:
- `0875FA:Vigilant.esm` (`zzzCHMeQ08MolagBB01`), view `0875F9` (`zzzCHMeQ08MolagBView`)

Speaker condition pattern:
- INFOs require `GetIsAliasRef == 1` on alias `#8` (`MolagBE`).
- Opener requires `GetStage == 210`.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0875FB zzzCHMeQ08MolagBB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1015) | `0875FC` | `InvisibleContinue` | `GetStage == 210`; `GetIsAliasRef #8`; VMAD `…020875FC.Fragment_0` | (Neutral) 「幹得好。這是給你的獎賞。」 |
| [`0875FD zzzCHMeQ08BB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1018) | `0875FE` | `WalkAway` | `GetIsAliasRef #8` | (Neutral) 「那麼，接下來你打算怎麼做？」 Note: EditorID 為 `zzzCHMeQ08BB01T02`（缺 `Molag`），原文如此。 |
| [`0875FF zzzCHMeQ08MolagBB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1021) | `087600` | `Goodbye, SayOnce` | `GetIsAliasRef #8`; VMAD `…02087600.Fragment_0` | Prompt 「捨棄名字，往西方去。這裡只剩悲傷。」 Response (Neutral) 「是嗎。我想無論你去哪都一樣。沒人會阻止你想做的事。」 |
| [`087601 zzzCHMeQ08MolagBB01T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1024) | `087602` | `Goodbye, SayOnce` | `GetIsAliasRef #8`; VMAD `…02087602.Fragment_0` | Prompt 「<Alias=Player>。記住，這是擊敗你的人之名。」 Response (Neutral) 「真有趣。你可得好好期待那一刻。」 |
| [`087603 zzzCHMeQ08MolagBB01T05`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1027) | `087604` | `Goodbye, SayOnce` | `GetIsAliasRef #8`; VMAD `…02087604.Fragment_0` | Prompt 「Stendll。記住，這是獵殺魔族者之名。」 Response (Neutral) 「非常有趣。雖是場拚死的掙扎，但很好。」 |

Inference: the BE branch repeats the naming fork at a later stage band (210+) — discard name / declare yourself as "the one who defeats you" / name yourself `Stendll` "who hunts daedra". 待驗證 which of these feeds the 230 vs 350/370 completions.

## Custom Dialogue Branch: Volar (zzzCHMeQ08VolarB01)

Branch:
- `08B5B1:Vigilant.esm` (`zzzCHMeQ08VolarB01`), view `08B5B0` (`zzzCHMeQ08VolarView`)

Speaker condition pattern:
- INFOs require `GetIsAliasRef == 1` on alias `#14` (`Volar`).
- Opener requires `GetStage == 310`.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`08B5B2 zzzCHMeQ08VolarB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1033) | `08B5B3` | `WalkAway` | `GetStage == 310`; `GetIsAliasRef #14` | (Puzzled) 原文 `The One is so come here soon, but if the squid you like?` — 機翻嚴重破碎，無法可靠還原。待驗證。 |
| [`08B5B4 zzzCHMeQ08VolarB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1036) | `08B5B5` | `Goodbye, SayOnce` | `GetIsAliasRef #14`; VMAD `…0208B5B5.Fragment_0` | Prompt 「玩得開心點，Volar。」 Response (Happy) 原文 `Let's show to meet the expectations definitely stuck!` — 機翻破碎。待驗證。 |

Volar-topic name-list handling:
- The quest prompt warned that `zzzCHMeQ08VolarB01T02` enumerates other memory subjects (Drozel, Hasaama, Johan, Martha). **`infodiag` does not bear this out**: the actual owned `VolarB01T02` response is a single garbled line (above), with no name list. The names Drozel/Hasaama/Martha/Johan live in **separate, non-owned** side-quest topics (`zzzCHsqMartha*` [dialogue.md:1356+], `zzzCHsqDrozel*` [dialogue.md:1380+]), confirmed not owned by `080E91`. They are therefore **excluded** from this slice. The Volar branch here is the **Deathbringer Volar** ([`088BC8 zzzCHDeathBringerMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1059)) confrontation, not a lore enumerator.

## Custom Dialogue Branch: Laza (zzzCHMeQ08LazaB01)

Branch:
- `2E47EA:Vigilant.esm` (`zzzCHMeQ08LazaB01`), view `2E47E9` (`zzzCHMeQ08LazaView`)

Speaker condition pattern:
- INFOs require `GetIsAliasRef == 1` on alias `#15` (`Laza`).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`2E47EB zzzCHMeQ08LazaB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2786) | `2E47EC` (SayOnce, WalkAway) / `2E47EF` (Goodbye) | — | `GetIsAliasRef #15` | INFO0 (Sad) 「你怎能這麼做……把他們還來……把我的家人……我的姐妹還來……」; INFO1 (Disgust) 「啊啊，Kyne……為什麼……為什麼你不救我們……」 |
| [`2E47ED zzzCHMeQ08LazaB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2790) | `2E47EE` | `Goodbye` | `GetIsAliasRef #15` | Prompt 「死者不會復生。」 Response (Anger) 「該死的，Sithis 之怪物……該死……該死……該死……」 |

Note: `Laza` ([`2E47E5 zzzCHMemoryLaza`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:806)) — Sithis/Kyne references suggest a villager confronting the bard over their dead family; this branch has no `GetStage` opener gate in the dump, so its stage tie is not yet pinned. 待驗證.

## CompleteQuest Stage → Outcome Map

Five stages carry `CompleteQuest`: **90 / 230 / 350 / 370 / 999**. Source-grounded shape from `questdiag` + branch-opener `GetStage` conditions; polarity marked where determinable, else TODO.

| Stage | Flag | Stage band | Tied branch (by `GetStage` opener) | Reading | Polarity |
|---:|---|---|---|---|---|
| 90 | CompleteQuest | first band (0–90) | Lamae B01 opener `==10`; Molag TE refusal `==60`; TE naming `==70` | The **Lamae / True-Ending (TE)** path: bard refuses to revive her via Molag, chooses how to name himself, ends the memory. A real ending. | **likely "good/mercy"** (refuses Molag's bargain) — TODO confirm via TE fragments |
| 230 | CompleteQuest | second band (100–230) | Molag BE opener `==210` | The **Bad-Ending (BE)** path: bard accepts Molag's "reward", takes the name as the one who will hunt/defeat. A real ending. | **likely "bad/corruption"** (accepts the bargain) — TODO confirm |
| 350 | CompleteQuest | third band (300–350) | Volar opener `==310` | The **West-Ending / Volar (WE)** path block (`WESc01/02`, Molag-Bal self-naming scene). A real ending. | TODO — Volar text too garbled to label |
| 370 | CompleteQuest | third band tail (360–370) | no own branch opener at 360–370 | Alternate completion just after 350; likely the **second outcome of the WE block** (or the Laza confrontation resolution). Real ending vs variant — undecided. | TODO |
| 999 | ShutDownStage + CompleteQuest | shutdown | — | **Shutdown only**, not a narrative ending. `ShutDownStage` flag present; mirrors the MeQ07 `255/999` shutdown pattern. Closes the quest after whichever real ending fired. | n/a (shutdown) |

Summary (source-grounded shape, polarity partly inferred):
- **Real endings: 90, 230, 350, 370** (four). **999 = engine shutdown**, not an outcome.
- The three stage bands (0–90 TE, 100–230 BE, 300–370 WE) map one-to-one onto the three Molag-apparition aliases the bard meets: `MolagTE` (#4), `MolagBE` (#8), and the `Volar`/`Molag Bal`-self-naming WE scenes (#14). This is the structural backbone; the **good↔bad polarity of 350 vs 370 is not decidable from `questdiag` + openers alone** and is left TODO pending the `CHMeq08_TIF__*` stage fragments.

## Related Records

Not necessarily owned by `080E91` per `infodiag`, but Lamae/Bard context for a full reconstruction:

NPCs:
- [`080E93 zzzCHLamaeMemory "Lamae Beolfag"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1073) — the memory Lamae (alias `#2`, owned)
- [`085FCA zzzCHLamaeMemoryDead`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1063) — dead-state Lamae (not in alias dump; cross-link)
- [`2C8784 zzzCHLamaeMemoryMad`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:821), [`2C8785 zzzCHLamaeFollowerMemoryDead "Facis"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:822) — alternate Lamae/Facis states
- [`037468 zzzBMLamaeBal "Lamae Bal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:891) — the Bloodmoon/base-game Lamae Bal (cross-mod link; not MeQ08)
- [`03D78A zzzCHBossDeathBringer "Deathbringer Volar"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:968) — combat Volar (memory alias `#14` is `088BC8 zzzCHDeathBringerMemory`)

Books (related, NOT owned by `080E91` per `infodiag`):
- [`0DB22D zzzCHBookBloodOfLamae "Blood of Lamae"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:2183) — the Lamae-pedigree vampire lore book (mentions `Ramae` = garbled Lamae); cross-link for the immortality-curse beat in scene `0875F5`.

## Reconstruction Notes

Source-grounded:
- This memory is [`080E91 zzzCHMemoryQuest08`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194), objective [`The rolling stones are in the fire and are not picked up`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:195).
- It contains **6 `SCEN` records** (`080EA5`, `0821E7`, `0821EE`, `0875F1`, `08B5AD`, `08B5B6`) staging the Lamae intro and the three ending-block narrations (TE/BE/WE) through aliases `TA01`–`TA05` and `Lamae`/`Facis`.
- It contains **6 custom dialogue branches**: Lamae (`080E9C`), Molag-TE refusal (`0821FA`), Molag-TE naming (`0821FF`), Molag-BE (`0875FA`), Volar (`08B5B1`), Laza (`2E47EA`).
- **0 books are owned** by the quest; `Blood of Lamae` is related context only.
- Stage-gated branch openers: Lamae `==10`, MolagTE-refusal `==60`, MolagTE-naming `==70`, MolagBE `==210`, Volar `==310`.
- VMAD `CHMeq08_TIF__*` fragments sit on every player-choice `Goodbye`/`SayOnce` INFO, so the choices advance state/route outcomes; exact Papyrus behaviour is not decoded here.

Garbled / flagged terms (machine-translated from Japanese, kept verbatim, 待驗證):
- `Lord Shorl` (`080EA8`) — proper noun, spelling unverified.
- `shine one was was lost from Nirn` (`0821ED`) — doubled `was`, broken grammar.
- `and Charles` (`0875F8`) — likely mistranslated Japanese particle/noise.
- `Stendll` / `Strange neme` (`082204`/`082205`, `087603`) — the player's chosen name; spelling unverified.
- `The One is so come here soon, but if the squid you like?` (`08B5B3`) and `Let's show to meet the expectations definitely stuck!` (`08B5B5`) — Volar lines, too broken to restore reliably.
- `Ramae` in `Blood of Lamae` book = garbled `Lamae`.

Open verification:
- decompile/inspect `CHMeq08_TIF__02080EA2`, `…02080EA4`, `…020821FE`, `…02082203/05/07`, `…020875FC/087600/02/04`, `…0208B5B5` to pin which fragment sets stage 90 vs 230 vs 350 vs 370 — this resolves the 350/370 polarity TODO;
- dump the QUST objective targets (`StartMarker`/`EndMarker`/`TE/BE/WEMarker`) to map each ending block to a worldspace location;
- pin the `Laza` branch's stage tie (no `GetStage` opener in the current dump);
- cross-link the MeQ09 `From Beyond` Lamae record when that slice is built (Lamae spans MeQ08/MeQ09).
