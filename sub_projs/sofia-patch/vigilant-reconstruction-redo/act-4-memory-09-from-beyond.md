# Act 4 Memory 09 - From Beyond

Status: redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text, not scene phases/actions.
- Subject is **Lamae**; this memory cross-links MeQ08 (also Lamae), but only MeQ09-owned records (`infodiag` owner = `2CAE30`) are reconstructed here.

## Quest Record

[`2CAE30 zzzCHMemoryQuest09 "From Beyond"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)

CLI:
- `questdiag Vigilant.esm 0x2CAE30`
- `infodiag Vigilant.esm 0x2CAE30`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x2CAE30`
- EditorID: `zzzCHMemoryQuest09`
- Name: `From Beyond`
- Flags: `RunOnce`
- Priority: `95` (higher than the usual `90` for the memory quests)
- Type: `Misc`
- Filter: `CH\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 10 | none | empty |
| 20 | none | empty |
| 30 | none | empty |
| 40 | none | empty |
| 50 | none | empty |
| 100 | none | empty |
| 110 | none | empty |
| 120 | none | empty |
| 130 | none | empty |
| 140 | none | empty |
| 150 | CompleteQuest | empty |
| 200 | CompleteQuest | empty |
| 999 | ShutDownStage + CompleteQuest | empty |

Objective:

| Index | Source | Translation |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:266) | `Aah, fetus. Alas, Fetus`（保留原文：英文本身為破碎/雙關句，疑為 "Aah, fetus" / "Alas, Fetus" 的刻意重複；待驗證） |
- Note: objective text `Aah, fetus. Alas, Fetus` is kept verbatim. It is a deliberately broken/garbled line (the quest's "stillborn lord / fetus" motif, see scene topic `2CC1F6`), not a normal objective; do not normalize it.
- The objective has 1 target with 0 conditions (`questdiag`); the CLI does not print the target ref.

## CompleteQuest Stage Map (150 / 200 / 999)

The quest has **three** `CompleteQuest` stages. Polarity is confirmed by PSC decompile of the two TIF fragments.

PSC sources:
- `chmeq09_tif__022cc214.psc` line 9: `GetOwningQuest().SetStage(200)`
- `chmeq09_tif__022cc216.psc` line 9: `GetOwningQuest().SetStage(100)`

| Stage | Flags | Mapped outcome |
|---:|---|---|
| 100 | none | Intermediate stage set by `CHMeq09_TIF__022CC216.Fragment_0` (PSC `chmeq09_tif__022cc216.psc:9`) — "Enough......." choice ("Farewell, Forgotten Brother. Sleep once again"). Leads into the 100→130→140→150 chain (stages 130/140 are set by `SF_zzzCHMeQ09WGBardSc02` Fragment_0/Fragment_2). |
| 130 | none | Set by `SF_zzzCHMeQ09WGBardSc02_022E47D1.Fragment_0` (`sf_zzzchmeq09wgbardsc02_022e47d1.psc:9`): part of the "sleep again" chain. |
| 140 | none | Set by `SF_zzzCHMeQ09WGBardSc02_022E47D1.Fragment_2` (`sf_zzzchmeq09wgbardsc02_022e47d1.psc:6`): continuation of the "sleep again" chain. |
| 150 | CompleteQuest | **"Sleep again" / rejection branch** — reached after the 100→130→140→150 stage chain. Player chose "Enough......." (topic `2CC215`, INFO `2CC216`). Sheogorath says "Farewel, Forgotten Brother. Sleep once again." |
| 200 | CompleteQuest | **"Accept / become Sithis's new brother" branch** — set directly by `CHMeq09_TIF__022CC214.Fragment_0` (`chmeq09_tif__022cc214.psc:9`). Player chose "Nevertheless......." (topic `2CC213`, INFO `2CC214`). Sheogorath says "Aaah, Black soul reach Sithis now. Welcome, our new brother." |
| 999 | ShutDownStage + CompleteQuest | Shutdown / cleanup stage (standard memory-quest teardown). Not a player-choice outcome. |

Stage routing summary (PSC-confirmed):
- `2CC213` "Nevertheless......." → `CHMeq09_TIF__022CC214.Fragment_0` → `SetStage(200)` → CompleteQuest directly ("Sithis/black-soul acceptance" branch).
- `2CC215` "Enough......." → `CHMeq09_TIF__022CC216.Fragment_0` → `SetStage(100)` → `WGBardSc02` Fragment_0 → `SetStage(130)` → Fragment_2 → `SetStage(140)` → (advance) → stage 150 CompleteQuest ("sleep again / rejection" branch). The WGBardSc02 scene (Jacob/WGBardTA02) plays during this post-choice sequence before completion.

## Alias / Staging Backbone

All seven `SCEN` records share the same host quest and 14-alias list.

Host quest:
- [`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)

Host-quest aliases from `scenediag`:

| Alias | Name | Fill |
|---:|---|---|
| 0 | `Sheogorath` | uniqueActor [`2C8797 zzzCHSheogorathMemoryMad`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:823) |
| 1 | `Lamae` | uniqueActor [`2C8784 zzzCHLamaeMemoryMad`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:821) |
| 3 | `BardTA01` | forcedRef `2CAE31:Vigilant.esm` |
| 4 | `BardTA02` | forcedRef `2CAE36:Vigilant.esm` |
| 5 | `SheoTA01` | forcedRef `2C9AF0:Vigilant.esm` |
| 6 | `SheoTA02` | forcedRef `2C9AF1:Vigilant.esm` |
| 7 | `SheoTA03` | forcedRef `2C9AF2:Vigilant.esm` |
| 8 | `MolagBal` | uniqueActor [`2BC374 zzzCHMemoryMolagBalMad` - Molag Bal](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:777) |
| 9 | `Jacob` | uniqueActor [`2DD387 zzzCHVigilantElderMemory`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:810) |
| 10 | `WGBardTA01` | forcedRef `2E3487:Vigilant.esm` |
| 11 | `WGBardTA02` | forcedRef `2E3486:Vigilant.esm` |
| 12 | `Fox` | uniqueActor [`2E3483 zzzCHMemoryFox` - Shor](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:807) |
| 13 | `Tsun` | uniqueActor [`2DE6ED zzzCHMemoryTsun` - Tsun](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:808) |
| 14 | `GuideMarker` | forcedRef `2C9AE7:Vigilant.esm` |

Inference:
- `BardTA0x`, `SheoTA0x`, `WGBardTA0x` are scene-monologue "talking actor" refs (carry the wandering / hallucination monologue lines).
- `Sheogorath` (alias `#0`), `Tsun` (alias `#13`) and `Lamae` (alias `#1`) are the dialogue speakers used by the three custom branches.
- The `Fox`/`Shor` alias (`#12`) and `Jacob` alias (`#9`) appear in scene `WGBardSc02` packages; `MolagBal` (`#8`) has a "do nothing" package ([`2CE891 zzzCHMeQ09MolagBalDoNoting`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2719)) — staging-only, no owned topic.
- This is inferred from alias names plus INFO conditions `GetIsAliasRef` on alias `#0` (Sheogorath), alias `#1` (Lamae), alias `#13` (Tsun). See branches below.
- `Fox` is named `Shor` in `npcs.tsv` — "Fox"/"Shor" identity is the source's own naming, not a translation choice.

Support records owned by the quest (from `find zzzCHMeQ09`, staging-only, no dialogue):
- Activator [`2CFBCF zzzCHMeq09MovePlayerTRG`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2719) — player-move trigger.
- Packages: `2CE891 zzzCHMeQ09MolagBalDoNoting`, `2E3488 zzzCHMeq09FoxEscortPlayer`, `2E47D5 zzzCHMeq09JacobFindYou`, `2E6E97 zzzCHMeq09FoxAvoidPlayer`, `2E6EA8 zzzCHMeq09JacobSearchBody`.
- Note: the activator + package FormIDs above are not in `dialogue.md`; the link is a placeholder anchor. They come from `find`/`scenediag` only.

## Scene Records

Scene records are not present as full records in `game-data`; the text lines are linked to `dialogue.md`, while phases/actions are from `scenediag`. All scene INFOs have `conds=0` (unconditional monologue).

### 2CC1E3 zzzCHMeQ09BardSc01

CLI:
- `scenediag Vigilant.esm 0x2CC1E3`

PSC:
- `sf_zzzchmeq09bardsc01_022cc1e3.psc` — Fragment_0 (on complete): `GetOwningQuest().SetStage(20)` (line 9).

Staging:
- Host quest: [`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- Flags: none
- Actor: alias `#3` (`BardTA01`)
- Phases: 3, each with 0 start conditions and 1 complete condition.
- On-complete fragment: sets stage 20.
- Actions (all `Dialog`, `Neutral`):
  - index 1: phase 0, topic [`2CC1E4`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2695).
  - index 2: phase 1, topic [`2CC1E6`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2698).
  - index 3: phase 2, topic [`2CC1E8`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2701).

Translations:
- [`2CC1E4` / INFO `2CC1E5`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2695): 「很久……很久以前，當古老的微光仍是一棵年幼的樹。它沉睡在 Kyne 的搖籃裡。」
  - Note: source `sicnce elder gleam was still young tree`（`sicnce`=`since` 拼錯；`elder gleam`=「古老的微光」待驗證，疑指世界之樹/起源之光）。
- [`2CC1E6` / INFO `2CC1E7`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2698): 「沒有人盼望它甦醒。但它穿越成千上萬的根，向著光前進，如今已爬出地面。」
  - Note: source `crawl out`（時態/語法破碎，原文如此）。
- [`2CC1E8` / INFO `2CC1E9`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2701): 「生鏽的鐘聲正呼喚我的名字。赤紅的雙眼看進我的心。一切沒入黑暗……如其所是。」

### 2CC1EA zzzCHMeQ09BardSc02

CLI:
- `scenediag Vigilant.esm 0x2CC1EA`

PSC:
- `sf_zzzchmeq09bardsc02_022cc1ea.psc` — Fragment_0 (on complete): `GetOwningQuest().SetStage(50)` (line 9).

Staging:
- Host quest: [`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- Flags: none
- Actor: alias `#4` (`BardTA02`)
- Phases: 2, each with 0 start conditions and 1 complete condition.
- On-complete fragment: sets stage 50 — this is the stage gate that enables the Sheogorath branch opener `2CC20F` (`GetStage == 50` condition).
- Actions (all `Dialog`, `Neutral`):
  - index 1: phase 0, topic [`2CC1EB`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2704).
  - index 2: phase 1, topic [`2CC1ED`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2707).

Translations:
- [`2CC1EB` / INFO `2CC1EC`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2704): 「瘋狂是從何時開始俯視我的？我在表面的身影，又是從何時變成醜陋的怪物？」
  - Note: source `maddness`（=`madness` 拼錯）、`over look`（=`overlook`，原文如此）。
- [`2CC1ED` / INFO `2CC1EE`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2707): 「我是誰？我要去往何方？為了什麼……我為何要尋找這個……」

### 2CC1EF zzzCHMeQ09SheoSc01

CLI:
- `scenediag Vigilant.esm 0x2CC1EF`

Staging:
- Host quest: [`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- Flags: none
- Actor: alias `#5` (`SheoTA01`)
- Phases: 1, with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Dialog`, phase 0, topic [`2CC1F2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2710), emotion `Neutral`.

Translation:
- [`2CC1F2` / INFO `2CC1F3`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2710): 「那不可能是 Shezarr。也不可能是它的先知。」
  - Note: `Shezarr`＝消失的 Shezarrine/Lorkhan 對應神格（原文如此，非錯字）。

### 2CC1F0 zzzCHMeQ09SheoSc02

CLI:
- `scenediag Vigilant.esm 0x2CC1F0`

Staging:
- Host quest: [`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- Flags: none
- Actor: alias `#6` (`SheoTA02`)
- Phases: 1, with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Dialog`, phase 0, topic [`2CC1F4`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2713), emotion `Neutral`.

Translation:
- [`2CC1F4` / INFO `2CC1F5`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2713): 「它是仿造之物，不過是 Sithis 的蒼白野獸。」
  - Note: `mimic`（仿造／擬態）、`Pale beast of Sithis`（Sithis 的蒼白野獸）原文如此。

### 2CC1F1 zzzCHMeQ09SheoSc03

CLI:
- `scenediag Vigilant.esm 0x2CC1F1`

Staging:
- Host quest: [`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- Flags: none
- Actor: alias `#7` (`SheoTA03`)
- Phases: 1, with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Dialog`, phase 0, topic [`2CC1F6`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2716), emotion `Neutral`.

Translation:
- [`2CC1F6` / INFO `2CC1F7`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2716): 「死產之主的呼喚如鐘聲般響起，而一切渴望都將以鮮血實現。」
  - Note: `stillborn lord`（死產之主）呼應任務目標的 `fetus` 母題；原文如此。

### 2E47CE zzzCHMeQ09WGBardSc01

CLI:
- `scenediag Vigilant.esm 0x2E47CE`

Staging:
- Host quest: [`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- Flags: none
- Actor: alias `#10` (`WGBardTA01`), actor flags `NoPlayerActivation`, `Optional`
- Phases: 1, with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Dialog`, phase 0, topic [`2E47CF`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2770), emotion `Neutral`.

Translation:
- [`2E47CF` / INFO `2E47D0`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2770): 「我曾是誰？我身在何處？燒焦的日記什麼也教不了我……」
  - Note: `Burned Diary`（燒焦的日記）疑與 [`01C7F6 zzzAoMDiaryAltano "Altano's Diary"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:644)（同樣「燒毀／撕碎」的日記）相關，但非本任務所屬，待驗證。

### 2E47D1 zzzCHMeQ09WGBardSc02

CLI:
- `scenediag Vigilant.esm 0x2E47D1`

PSC:
- `sf_zzzchmeq09wgbardsc02_022e47d1.psc` — two fragments:
  - Fragment_0 (phase 0 complete): `GetOwningQuest().SetStage(130)` (line 9).
  - Fragment_2 (phase 2 complete): `GetOwningQuest().SetStage(140)` (line 6).
- Note: NEXT FRAGMENT INDEX=3, fragments are indexed 0 and 2 (no Fragment_1 present in PSC).

Staging:
- Host quest: [`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- Flags: none
- Actors: alias `#11` (`WGBardTA02`) and alias `#9` (`Jacob`), both `NoPlayerActivation`, `Optional`
- Phases: 3, each with 0 start conditions and 1 complete condition.
- On-complete fragments: phase 0 → stage 130; phase 2 → stage 140. This scene runs as part of the "sleep again" (stage 100→150) chain after the player picks "Enough.......".
- Actions:
  - index 1: `Dialog`, actor `#11`, phase 0, topic [`2E47D2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2773), `Neutral`.
  - index 2: `Package`, actor `#9` (Jacob), phase 1 (no topic).
  - index 3: `Package`, actor `#9` (Jacob), phase 2 (no topic).
  - index 4: `Dialog`, actor `#9` (Jacob), phase 1, flags `HeadtrackPlayer`, no topic (silent / headtrack beat).
  - index 5: `Dialog`, actor `#9` (Jacob), phase 2, topic [`2E47D6`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2776), `Neutral`.

Translations:
- [`2E47D2` / INFO `2E47D3`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2773): 「我……我失去了某種珍貴的東西……但如今一切都……」
  - Note: source `prescious`（=`precious` 拼錯）。
- [`2E47D6` / INFO `2E47D7`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2776): 「醒來……醒來……不要睡去……」
  - 由 Jacob（alias `#9`）說出，呼應 Tsun 分支的「Souless」主題。

## Custom Dialogue Branch: Lamae

Branch:
- [`2CC20A zzzCHMeQ09LamaeB01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2719) (DialogView [`2CC209 zzzCHMeQ09LamaeView`])

Speaker condition pattern:
- INFO requires `GetIsAliasRef == 1` on alias `#1` (`Lamae`).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`2CC20B zzzCHMeQ09LamaeB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2719) | `2CC20C` | `Goodbye` | `GetIsAliasRef alias #1` | (Fear) 「你這怪物……離我遠一點……」 |

Inference:
- Single-line, `Goodbye`, emotion `Fear`: Lamae recoils from the awakened player-as-revenant. This is the Lamae cross-link to MeQ08 (also Lamae), but the INFO is owned by `2CAE30`.

## Custom Dialogue Branch: Sheogorath

Branch:
- [`2CC20E zzzCHMeQ09SheoB01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2722) (DialogView [`2CC20D zzzCHMeQ09SheogorathView`])

Speaker condition pattern:
- Most INFOs require `GetIsAliasRef == 1` on alias `#0` (`Sheogorath`).
- Opening line `2CC20F` also requires `GetStage == 50` on quest `2CAE30`.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`2CC20F zzzCHMeQ09SheoB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2722) | `2CC210` | none | `GetStage == 50`; `GetIsAliasRef alias #0` | (Happy) resp1：「你早就知道。你本該知道那件事。如今一切都已超越遺忘。」 resp2：「是這樣嗎？你再也無法知道自己是誰了。」 |
| [`2CC211 zzzCHMeQ09SheoB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2726) | `2CC212` | none | `GetIsAliasRef alias #0` | Prompt：「我……我是……」 Response：(Surprise)「你明明被它灼燒，卻仍為了某事而死去？黑色的靈魂將永無安寧地燃燒下去？」 |
| [`2CC213 zzzCHMeQ09SheoB01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2729) | `2CC214` | `Goodbye` | `GetIsAliasRef alias #0`; VMAD `CHMeq09_TIF__022CC214.Fragment_0` on end | Prompt：「即便如此……」 Response：resp1 (Happy)「那是 Molag Bal。遠離 Shezarr 的野獸。只是孱弱、只是粗鄙、只是醜陋。」 resp2 (Surprise)「啊啊，黑色的靈魂如今抵達 Sithis 了。歡迎你，我們的新兄弟。」 |
| [`2CC215 zzzCHMeQ09SheoB01T04`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2733) | `2CC216` | `Goodbye` | `GetIsAliasRef alias #0`; VMAD `CHMeq09_TIF__022CC216.Fragment_0` on end | Prompt：「夠了……」 Response：resp1 (Disgust)「甦醒的靈魂忘卻一切。所以一切才都在夢中。」 resp2 (Sad)「永別了，被遺忘的兄弟。再次沉睡吧。」 |

Branch structure (PSC-confirmed):
- `2CC213` ("Nevertheless.......") → `CHMeq09_TIF__022CC214.Fragment_0` → `SetStage(200)` → CompleteQuest at stage 200. **"Accept / Sithis's new brother" branch.** (`chmeq09_tif__022cc214.psc:9`)
- `2CC215` ("Enough.......") → `CHMeq09_TIF__022CC216.Fragment_0` → `SetStage(100)` → WGBardSc02 scene chain (130→140) → stage 150 CompleteQuest. **"Reject / sleep again" branch.** (`chmeq09_tif__022cc216.psc:9`)

Translation notes:
- `beyond oblivion` in [`2CC20F`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2722)：直譯「超越遺忘」；`Oblivion` 可能雙關（湮滅位面），待驗證。
- `Black soul` / `Pale beast of Sithis` / `our new brother`：Sithis/虛無母題的反覆用語，原文如此。

## Custom Dialogue Branch: Tsun

Branch:
- [`2E47D9 zzzCHMeQ09TsunB01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2779) (DialogView [`2E47D8 zzzCHMeQ09TsunView`])

Speaker condition pattern:
- All INFOs require `GetIsAliasRef == 1` on alias `#13` (`Tsun`).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`2E47DA zzzCHMeQ09TsunB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2779) | `2E47DB` | `SayOnce` | `GetIsAliasRef alias #13` | (Neutral)「Stuhn……不，你不是……你是誰……？」 |
| (same topic) | `2E47DE` | `Goodbye` | `GetIsAliasRef alias #13` | (Neutral)「退下吧，無魂者……」 |
| [`2E47DC zzzCHMeQ09TsunB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2783) | `2E47DD` | `Goodbye` | `GetIsAliasRef alias #13` | Prompt：「我不知道……」 Response：(Neutral)「不過是你自己的殘影。無魂、無心。可悲……何等可悲……」 |

Translation notes:
- `Stuhn`：Tsun 的前身/古諾德神名（Stuhn），原文如此；Tsun 認錯了對象。
- `Souless` (×2) 與 `Hearless`：原文拼錯（=`Soulless` / `Heartless`），保留語意翻為「無魂」「無心」。
- `Get thee hence`：古體英語「退下／離開此地」，原文如此。

## Related Records

These are owned by quest `2CAE30` per `find`/`scenediag` but carry no dialogue; listed for a full reconstruction.

NPCs (alias fills):
- [`2C8797 zzzCHSheogorathMemoryMad` - Sheogorath](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:823)
- [`2C8784 zzzCHLamaeMemoryMad` - Lamae](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:821)
- [`2BC374 zzzCHMemoryMolagBalMad` - Molag Bal](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:777)
- [`2DD387 zzzCHVigilantElderMemory` - Jacob](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:810)
- [`2E3483 zzzCHMemoryFox` - Shor](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:807)
- [`2DE6ED zzzCHMemoryTsun` - Tsun](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:808)

Staging records (no dialogue; from `find zzzCHMeQ09`):
- Activator `2CFBCF zzzCHMeq09MovePlayerTRG`.
- Packages `2CE891 zzzCHMeQ09MolagBalDoNoting`, `2E3488 zzzCHMeq09FoxEscortPlayer`, `2E47D5 zzzCHMeq09JacobFindYou`, `2E6E97 zzzCHMeq09FoxAvoidPlayer`, `2E6EA8 zzzCHMeq09JacobSearchBody`.

Books:
- No `zzzCHMeQ09…` book found in `books.md` or via `find`. The "Burned Diary" referenced in scene `2E47CF` is **not** an MeQ09-owned BOOK record; closest extracted analogue is [`01C7F6 zzzAoMDiaryAltano`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:644) — cross-reference only,待驗證.

## Reconstruction Notes

Source-grounded:
- This memory is [`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265), priority `95`, objective [`Aah, fetus. Alas, Fetus`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:266).
- It contains **seven** `SCEN` records (`2CC1E3`, `2CC1EA`, `2CC1EF`, `2CC1F0`, `2CC1F1`, `2E47CE`, `2E47D1`) staging short monologues through the `BardTA`, `SheoTA`, and `WGBardTA` aliases; `WGBardSc02` additionally runs two Jacob packages plus a silent headtrack beat.
- It contains **three** custom dialogue branches:
  - Lamae branch (`2CC20A`), 1 topic, alias `#1`, `Fear`/`Goodbye` recoil line.
  - Sheogorath branch (`2CC20E`), 4 topics, alias `#0`, opener stage-gated at `GetStage==50` (set by BardSc02 on-complete); two VMAD-bearing terminal choices = stages 200 (accept) and 100→150 (reject).
  - Tsun branch (`2E47D9`), 2 topics, alias `#13`, the "Souless" rejection.
- Branch count: 3 dialogue branches; Scene count: 7; Book count: 0 owned.

## Stage → Scene chain (PSC-confirmed)

Full stage flow from PSC fragments and `questdiag`:

```
stage 0  (start)
→ BardSc01 completes → SetStage(20)      [sf_zzzchmeq09bardsc01_022cc1e3.psc:9]
→ BardSc02 completes → SetStage(50)      [sf_zzzchmeq09bardsc02_022cc1ea.psc:9]
  → unlocks Sheogorath branch opener 2CC20F (GetStage==50 condition)
    → player choice:
      "Nevertheless......." (2CC213/2CC214)
        → CHMeq09_TIF__022CC214.Fragment_0 → SetStage(200)   [chmeq09_tif__022cc214.psc:9]
        → CompleteQuest (stage 200) = "Sithis/black-soul acceptance"
      "Enough......." (2CC215/2CC216)
        → CHMeq09_TIF__022CC216.Fragment_0 → SetStage(100)   [chmeq09_tif__022cc216.psc:9]
        → WGBardSc02 phase 0 completes → SetStage(130)       [sf_zzzchmeq09wgbardsc02_022e47d1.psc:9]
        → WGBardSc02 phase 2 completes → SetStage(140)       [sf_zzzchmeq09wgbardsc02_022e47d1.psc:6]
        → (stage 150) → CompleteQuest = "sleep again / rejection"
stage 999 = ShutDownStage (teardown)
```

Stages 10, 30, 40 are present in `questdiag` but have no corresponding PSC fragments in the cache — no scene fragment sets them directly. They may be set by quest alias conditions or world-space triggers not captured in the PSC cache. (unverified: no matching PSC fragment found)

## Open verification (remaining)

- **Trigger / quest-start**: the `zzzCHMeq09MovePlayerTRG` activator (`chmeq09moveplayertriggerscript.psc`) is **marked unused** in v1.8.1 (PSC line 2: `;Unused Script at v1.6.0` — only fires a version-check MessageBox). The `FoxEscortPlayer` and `JacobFindYou` packages suggest an NPC-guided walk-in trigger, but the exact quest-start condition (what starts `zzzCHMemoryQuest09` and populates aliases) is not captured in the PSC cache. (unverified: would need `questdiag` alias-target dump or direct ESM read of quest start conditions)
- **Karma global / hub wiring**: MeQ09 completion is `Fragment_18` in `qf_zzzchmemoryguide_0242e0b1.psc` (line 21–24), which has an empty body (`;Dream09 Finished` comment only — no `SetObjectiveCompleted`, no karma global call). MeQ09 does NOT contribute to the guide's objective chain (objectives 100/110/120 are set only by Dream10/11/12 per the PSC). There is no karma global found for MeQ09. (confirmed: hub PSC `qf_zzzchmemoryguide_0242e0b1.psc` Fragment_18)
- **Stages 10/30/40**: present in `questdiag` but no PSC fragment sets them; likely set by world-space or alias conditions outside the PSC cache. (unverified: no matching fragment)
- **Objective target ref**: `questdiag` prints `target: flags=0 conds=0` with no ref ID; the target FormID is not accessible via `questdiag`. (unverified: CLI does not expose target ref)
- **Cross-link to MeQ08**: resolve once MeQ08 is sliced; keep MeQ09 records here, MeQ08 records there.
