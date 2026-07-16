# Act 4 Memory 12 - Last Night

Status: redo slice (mirrors the 07 template). Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text, not scene phases/actions.
- The mod's source English is heavily mistyped (e.g. `Wellcome`, `Perrif`, `Paravant`); kept verbatim with `Note:` flags rather than silently corrected.

## Quest Record

[`2BC395 zzzCHMemoryQuest12 "Last Night"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:307)

CLI:
- `questdiag Vigilant.esm 0x2BC395`
- `infodiag Vigilant.esm 0x2BC395`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x2BC395`
- EditorID: `zzzCHMemoryQuest12`
- Name: `Last Night`
- Flags: `RunOnce`
- Priority: `90`
- Type: `Misc`
- Filter: `CH\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 5 | none | empty |
| 10 | none | empty |
| 20 | none | empty |
| 30 | none | empty |
| 40 | none | empty |
| 50 | CompleteQuest | empty |
| 60 | none | empty |
| 300 | none | empty |
| 310 | CompleteQuest | empty |
| 320 | none | empty |
| 999 | ShutDownStage | empty |

Objectives:
- 0 objectives in ESM (`questdiag` prints `Objectives (0)`). No quest-log objective text; this memory is staged entirely through scenes + two custom branch lines.

## Alias / Staging Backbone

Both `SCEN` records below share the same host quest and the same four aliases. Alias fill confirmed via `scenediag 0x2BD6CB` host-quest alias dump (CLI).

Host quest:
- [`2BC395 zzzCHMemoryQuest12`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:307)

Host-quest aliases:

| Alias | Name | Fill | NPC record |
|---:|---|---|---|
| 0 | `Alessia` | uniqueActor `2BC383:Vigilant.esm` | [`2BC383 zzzCHMemoryStAlessiaOld "Alessia"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:814) |
| 1 | `Pelinal` | uniqueActor `2BC37F:Vigilant.esm` | [`2BC37F zzzCHMemoryPelinal02 "Pelinal"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:785) |
| 2 | `Akatosh` | uniqueActor `2BC376:Vigilant.esm` | [`2BC376 zzzCHMemoryAkatosh "???????"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:776) — display name deliberately blank in source; alias name `Akatosh` is the only naming evidence |
| 3 | `Bull` | uniqueActor `2BC389:Vigilant.esm` | [`2BC389 zzzCHMemoryMorihaus02 "Morihaus"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:815) |

All aliases use `uniqueActor` (hardcoded references); no condition-based fill.

Source-grounded mapping:
- Alias `Bull` is **Morihaus** (the winged Man-Bull, Alessia's consort) per the NPC record name.
- Alias `Akatosh` NPC record `2BC376` has display name `???????` (blank/redacted in extracted source); alias name is the only identifier.
- `Pelinal` (alias `#1`) and `Alessia` (alias `#0`) are the only actors in the good scene (`2BD6CB`); `Akatosh` (alias `#2`) is the sole actor in the bad scene (`2BD6F2`); `Bull` (alias `#3`) appears only in the custom branch dialogue, not in either SCEN. Confirmed by scene actor lists in `scenediag`.

Subject of the memory: **Pelinal Whitestrake ("Paravant") reunited with Alessia ("Perrif")** at the close of his story. The "Last Night" / farewell framing is confirmed by the good scene's `"....It is time to say goodbye, Perrif"` line (topic `2BD6DF`).

## Scene Records

Scene records are not present as full records in `game-data`; the text lines are linked to `dialogue.md`, while phases/actions are from `scenediag`. Both scenes are owned by quest `2BC395`.

### 2BD6CB zzzCHMeQ12Sc01  (good scene: Pelinal + Alessia reunion)

CLI:
- `scenediag Vigilant.esm 0x2BD6CB`

Staging:
- Host quest: [`2BC395 zzzCHMemoryQuest12`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:307)
- Flags: none
- Scene actors: alias `#0` (`Alessia`, `DeathEnd, DialoguePause`) and alias `#1` (`Pelinal`, `DialoguePause`).
- Phases: 14 (each 0 start conds, 1 complete cond; phases 9 and 11 have 2 complete conds).
- Actions: 18 total — Pelinal (actor `#1`) and Alessia (actor `#0`) alternate `Dialog` actions, framed by `Package` actions on Pelinal and one `Timer`.

Dialog actions (phase → speaker → topic → emotion):

| Phase | Speaker | Topic | Emotion | Source |
|---:|---|---|---|---|
| 1 | Pelinal `#1` | `2BD6CD` | Sad | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2627) |
| 2 | Alessia `#0` | `2BD6CF` | Happy | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2630) |
| 3 | Alessia `#0` | `2BD6D1` | Neutral | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2633) |
| 4 | Pelinal `#1` | `2BD6D3` | Neutral | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2636) |
| 5 | Alessia `#0` | `2BD6D5` | Happy | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2639) |
| 6 | Pelinal `#1` | `2BD6D7` | Fear | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2642) |
| 7 | Alessia `#0` | `2BD6D9` | Happy | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2645) |
| 8 | Pelinal `#1` | `2BD6DB` | Happy | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2648) |
| 9 | Alessia `#0` | `2BD6DD` | Sad | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2651) |
| 9 | (Timer 5s, actor `#0`) | — | — | — |
| 10 | Pelinal `#1` | `2BD6DF` | Neutral | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2654) |
| 11 | Alessia `#0` | `2BD6E2` | Happy | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2657) |
| 12 | Pelinal `#1` | `2BD6E5` | Sad | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2660) |

Translations (good scene, in phase order):
- [`2BD6CD` / INFO `2BD6CE`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2627): 「Perrif……」
  - Note: `Perrif` is the early-form name of Alessia (Paravania / Al-Esh); kept verbatim.
- [`2BD6CF` / INFO `2BD6D0`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2630): 「歡迎回來。我從沒想過，最後還能再見到你。」
  - Note: source `Wellcome` is a misspelling of `Welcome`; kept verbatim.
- [`2BD6D1` / INFO `2BD6D2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2633): 「所以……怎麼樣了？你找到她了嗎？」
- [`2BD6D3` / INFO `2BD6D4`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2636): 「終於，我找到她了。我這就去把她接來。」
- [`2BD6D5` / INFO `2BD6D6`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2639): 「我盼著你能見到她。」
- [`2BD6D7` / INFO `2BD6D8`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2642): 「還是不明白。」
  - Note: source `Still don't get it` is ambiguous (who/what is not understood); literal rendering. 待驗證。
- [`2BD6D9` / INFO `2BD6DA`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2645): 「我想是吧。」
- [`2BD6DB` / INFO `2BD6DC`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2648): 「你或許……」
- [`2BD6DD` / INFO `2BD6DE`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2651): 「…………」
- [`2BD6DF` / INFO `2BD6E0`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2654): 「……該道別了，Perrif。」
- [`2BD6E2` / INFO `2BD6E3`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2657): 「再見了。」
  - Note: source `See you again`; literal「後會有期」also plausible.
- [`2BD6E5` / INFO `2BD6E6`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2660): 「……再見。」

### 2BD6F2 zzzCHMeQ12BadScene  (bad scene: Akatosh turns the player away)

CLI:
- `scenediag Vigilant.esm 0x2BD6F2`

Staging:
- Host quest: [`2BC395 zzzCHMemoryQuest12`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:307)
- Flags: none
- Scene actors: alias `#2` only (`Akatosh`, `DeathEnd, DialoguePause`).
- Phases: 6 (phase 0 has 2 complete conds; rest 1 each).
- Actions: 6 total — one `Timer` (3s) then five `Dialog` actions, all actor `#2` (Akatosh), all `Neutral`, `HeadtrackPlayer`.

Dialog actions:

| Phase | Speaker | Topic | Source |
|---:|---|---|---|
| 1 | Akatosh `#2` | `2BD6F3` | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2669) |
| 2 | Akatosh `#2` | `2BD6F5` | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2672) |
| 3 | Akatosh `#2` | `2BD6F7` | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2675) |
| 4 | Akatosh `#2` | `2BD6F9` | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2678) |
| 5 | Akatosh `#2` | `2BD6FB` | [link](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2681) |

Translations (bad scene):
- [`2BD6F3` / INFO `2BD6F4`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2669): 「哦，你是從蜥蜴的腹中而來。真有意思。」
  - Note: `lizard's stomach` (inference) refers to the player's arrival via the Daedric/Coldharbour passage of VIGILANT's frame story; literal rendering. 待驗證。
- [`2BD6F5` / INFO `2BD6F6`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2672): 「你不屬於這一側。我能感覺到你身上的傷。」
- [`2BD6F7` / INFO `2BD6F8`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2675): 「你對我們無害，但你不該見到她。她終於能安息了。」
- [`2BD6F9` / INFO `2BD6FA`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2678): 「而且，我不知道是誰把你帶到這裡來的，但你不該這樣看著。」
  - Note: source `you should not see like this` is grammatically broken; literal rendering. 待驗證。
- [`2BD6FB` / INFO `2BD6FC`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2681): 「若你與此地相融，就再也回不去了。現在，回到你那一側去吧。」

## Custom Dialogue Branch: Bull (Morihaus)

Branch:
- `2BD6EA:Vigilant.esm` (`zzzCHMeQ12BullB01`), view `zzzCHMeQ12BullView` (`2BD6E9`).

Speaker condition pattern:
- The single INFO requires `GetIsAliasRef == 1` on alias `#3` (`Bull` = Morihaus).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`2BD6EB zzzCHMeQ12BullB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2663) | `2BD6EC` | `Goodbye` | `GetIsAliasRef alias #3` | 「我明白……她化作了星辰……但是，這很令人難過……太難過了……」 |

## Custom Dialogue Branch: Akatosh

Branch:
- `2BD6EE:Vigilant.esm` (`zzzCHMeQ12AkatoshB01`), view `zzzCHMeQ12AkatoshView` (`2BD6ED`).

Speaker condition pattern:
- The single INFO requires `GetStage <= 60` on quest `2BC395` and `GetIsAliasRef == 1` on alias `#2` (`Akatosh`).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`2BD6EF zzzCHMeQ12AkatoshB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2666) | `2BD6F0` | `Goodbye` | `GetStage <= 60`; `GetIsAliasRef alias #2` | 「Paravant，循著群星而行……我記得，你的雙眼曾如流星般燃燒。」 |

Translation notes:
- `Paravant` is the early-form name of Pelinal (Pelin-Al / Paravant); the source spells it `Paravant`. Kept verbatim. The line echoes the hub quest `zzzCHMemoryGuide` objective 120 [`Blind eyes could blaze like meteors and be`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:312) (Dylan Thomas), confirming the cross-quest linkage.
- The `GetStage <= 60` gate ties this Akatosh line to the **pre-310 (good-path) window**: it is only available while the quest has not advanced into the 300-band. (inference)

## Reconstruction Notes

Source-grounded:
- This memory is [`2BC395 zzzCHMemoryQuest12 "Last Night"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:307), 12 stages, **0 quest-log objectives** (header only in `quests.md`).
- It contains **two** `SCEN` records:
  - `2BD6CB zzzCHMeQ12Sc01` — the **good / reunion scene**: Pelinal ("Perrif"-caller) meets Alessia, 14 phases, ending on `"It is time to say goodbye, Perrif"` → `"See you again"` → `"........bye"`.
  - `2BD6F2 zzzCHMeQ12BadScene` — the **bad / turned-away scene**: Akatosh alone tells the player they do not belong, "you should not meet her", "back to your side".
- It contains **two** custom dialogue branches, each a single `Goodbye` INFO:
  - Bull (Morihaus) alias `#3` — laments Alessia becoming a star.
  - Akatosh alias `#2`, gated `GetStage <= 60` — addresses "Paravant" with the meteor-eyes line.
- **No books** are owned by or text-linked to this quest (`find zzzCHMeQ12` returns no BOOK records; none referenced in the scenes).

## Stage Fragment Routing (RESOLVED)

Source: `qf_zzzchmemoryquest12_022bc395.psc` (full decompile); `sf_zzzchmeq12badscene_022bd6f2.psc`; `scenediag 0x2BD6CB`; `scenediag 0x2BD6F2`.

The QF script (`NEXT FRAGMENT INDEX 22`) contains 11 active fragments. Mapping by content:

| Fragment | Stage | Code summary |
|---|---|---|
| `Fragment_0` | **0** | `SoulStream.Enable(); MemoryTRG.Enable()` — quest init, enable trigger |
| `Fragment_2` | **10** | FadeOut → `MoveTo(StartMarker)` → `SetStage(20)` — teleport player to memory start |
| `Fragment_4` | **30** | `if PelinalQuest.GetStageDone(300)` → bad branch (`SetStage(300)`) else good branch (`Pelinal.SetAlpha(0)`, continue) |
| `Fragment_7` | **40** (entry 1) | `Pelinal.SetAlpha(1.0)` + `TeleportINEffect` + `Sc01.ForceStart()` + `RegisterSceneSkip(self, Sc01, 40, True)` — fade Pelinal in, start good scene |
| `Fragment_9` | **40** (entry 2) | Scene-end: `TeleportOutEffect` on Pelinal, `Pelinal.TryToDisable()`, stop `Sc01` if playing, `SetStage(50)` — Pelinal exits after farewell |
| `Fragment_10` | **50** (entry 1) | FadeOut → `MoveTo(ReturnMarker)` → `SetStage(60)` — return player to normal world |
| `Fragment_20` | **50** (entry 2) | `qGuide.SetStage(120)` + `ModRadiance(3.0)` — **good karma reward**: advance hub objective 120, add 3 Radiance points |
| `Fragment_12` | **60** | `stop()` — shut down quest after good path return |
| `Fragment_14` | **300** | `BadScene.ForceStart()` — trigger Akatosh-turn-away scene |
| `Fragment_16` | **310** (entry 1) | `DisablePlayerControls` + `ForceFirstPerson` + `TimeWarpSound` + `TravelEffect01` + FadeOut → `MoveTo(ReturnMarker)` → `SetStage(320)` — eject player after bad scene |
| `Fragment_18` | **320** | `stop()` — shut down quest after bad path return |

Bad-scene end fragment (`sf_zzzchmeq12badscene_022bd6f2.psc`):
- `Fragment_0`: `GetOwningQuest().SetStage(310)` — when `BadScene` finishes naturally, sets stage 310 (bad `CompleteQuest`).
- `Fragment_1`: `q.RegisterSceneSkip(GetOwningQuest(), self, 310, True)` — if scene skipped, also routes to stage 310.

Branch polarity — RESOLVED via QF psc stage routing:

| Outcome | Path | CompleteQuest stage | Karma |
|---|---|---|---|
| **Good / Reunion** | stage 30 → good branch → stage 40 → `Sc01` (Pelinal+Alessia farewell) → stage 50 | **50** | `ModRadiance(+3.0)` + `qGuide.SetStage(120)` (hub objective 120 completed) |
| **Bad / Blocked** | stage 30 → `PelinalQuest.GetStageDone(300)` true → stage 300 → `BadScene` (Akatosh turns player away) → stage 310 | **310** | no Radiance, no guide advance |

Cross-quest gate at stage 30 (source: `qf_zzzchmemoryquest12_022bc395.psc` Fragment_4 line 93):
```papyrus
if PelinalQuest.GetStageDone(300) ;;Bad End to 300
    WallMarker.Enable()
    Alias_Bull.TryToDisable()
    Alias_Akatosh.TryToEvaluatePackage()
    SetStage(300)
else
    Alias_Pelinal.GetActorRef().SetAlpha(0)
    TimeWound.SetAnimationVariableFloat("fToggleBlend", 1)
endif
```
- `PelinalQuest` is `zzzCHMemoryQuest10 "Pelinal the Bloody"` (FormID `2A532E`; verified via `find`). Its stage 300 is one of its two `CompleteQuest` stages (bad / Mary-slave ending; confirmed `questdiag 0x2A532E`).
- **If the player completed MeQ10 on the bad path** (stage 300 done), MeQ12 routes straight to the bad path: Pelinal never appears, Akatosh blocks. The `;;Bad End to 300` comment is inline evidence.
- **If MeQ10 was completed on the good path** (stage 180 done, stage 300 not done), MeQ12 routes to the good path: Pelinal is faded in and the reunion scene begins.
- This is the only cross-quest state dependency in MeQ12. (source: psc Fragment_4)

Karma result (good path): `ModRadiance(3.0)` via `AoMAchievementPointQuestScript` cast (psc Fragment_20 lines 141–144); simultaneously `qGuide.SetStage(120)` completes the third (final) `zzzCHMemoryGuide` hub objective (`SetObjectiveCompleted(120)` in `qf_zzzchmemoryguide_0242e0b1.psc` Fragment_24 — comment `;;Dream12 Finished`). This identifies MeQ12 as **Dream12** in the hub.

`GetStage <= 60` gate on Akatosh custom branch (psc confirmed via `infodiag 0x2BC395`): this branch is only available while the quest stage has not entered the 300-band. Since the bad path jumps directly from 30→300 and the good path passes through 50→60→stop(), this gate is effectively "good-path only". Stage 60 is the final shutdown stage on the good path before `stop()`.

## SCEN Staging (RESOLVED)

Source: `scenediag 0x2BD6CB` (good scene) and `scenediag 0x2BD6F2` (bad scene); both confirmed via CLI.

Good scene `2BD6CB zzzCHMeQ12Sc01`:
- Host quest: `2BC395`; actors: alias `#0` Alessia (`DeathEnd, DialoguePause`), alias `#1` Pelinal (`DialoguePause`).
- 14 phases; 18 actions: 2 Package (Pelinal standby at phases 0–10), 12 Dialog (alternating Pelinal/Alessia, phases 1–12), 1 Timer (5s, phase 9), 3 Package (Pelinal stop/exit, phases 11–13).
- Phase 9 has 2 complete conditions (Timer 5s + dialog completion) — the silent `"............"` beat is held for 5 seconds.
- Phase 11 has 2 complete conditions — Pelinal Package action overlaps with Alessia dialog at `"See you again"`.
- Dialog action index 13 at phase 10: `HeadtrackActorID=-1` (Pelinal looks away, not at Alessia) for `"....It is time to say goodbye, Perrif"` — cinematically confirmed the farewell direction.

Bad scene `2BD6F2 zzzCHMeQ12BadScene`:
- Host quest: `2BC395`; actor: alias `#2` Akatosh only (`DeathEnd, DialoguePause`).
- 6 phases; 6 actions: 1 Timer (3s, phase 0, 2 complete conds), 5 Dialog (Akatosh, phases 1–5, all `HeadtrackPlayer Neutral`).
- Phase 0 has 2 complete conditions (Timer + something else); the 3-second hold before Akatosh speaks gives the player a moment to register the scene.
- No Pelinal, no Alessia, no Bull in this scene; aliases `#0`, `#1`, `#3` unused.

## Alias Fill (RESOLVED)

All 4 aliases confirmed via `scenediag 0x2BD6CB` host-quest alias dump:

| Alias | Name | Fill method | NPC |
|---:|---|---|---|
| 0 | `Alessia` | `uniqueActor 2BC383:Vigilant.esm` | `2BC383 zzzCHMemoryStAlessiaOld "Alessia"` |
| 1 | `Pelinal` | `uniqueActor 2BC37F:Vigilant.esm` | `2BC37F zzzCHMemoryPelinal02 "Pelinal"` |
| 2 | `Akatosh` | `uniqueActor 2BC376:Vigilant.esm` | `2BC376 zzzCHMemoryAkatosh "???????"` — display name redacted/blank in extracted source; alias name is the only naming evidence |
| 3 | `Bull` | `uniqueActor 2BC389:Vigilant.esm` | `2BC389 zzzCHMemoryMorihaus02 "Morihaus"` |

All are `uniqueActor` (hardcoded references), no condition-based fill. No alias fill is unresolved or marked unverified; the only open item is the display name of alias `#2` (NPC `2BC376` has `???????` in the extracted NPC list — this is a deliberate blank name in the source, not a data gap).

## Branch Outcome Mapping (50 vs 310) — RESOLVED

| Outcome | Trigger | CompleteQuest | Karma | Hub advance |
|---|---|---|---|---|
| **Good / Reunion** | MeQ10 bad-path (stage 300) NOT done → Sc01 plays → stage 50 | stage **50** | `ModRadiance(+3.0)` | `qGuide.SetStage(120)` → `SetObjectiveCompleted(120)` in hub |
| **Bad / Blocked** | MeQ10 bad-path (stage 300) done → BadScene plays → stage 310 | stage **310** | none | none |

No items, no factions, no world changes granted on either path — only the Radiance global point and hub objective on the good path.

Flagged source terms (kept verbatim, `Note:`/待驗證):
- `Perrif` (Alessia early name), `Paravant` (Pelinal early name) — period-correct lore names, NOT typos.
- `Wellcome` (= Welcome), `you should not see like this` (broken grammar), `Still don't get it` (ambiguous), `lizard's stomach` (frame-story arrival, not a typo — Akatosh observes the player arrived through the daedric passage) — flagged in-line above. No further lore verification needed for the routing reconstruction.
