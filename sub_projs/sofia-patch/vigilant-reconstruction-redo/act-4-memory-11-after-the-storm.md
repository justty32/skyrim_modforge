# Act 4 Memory 11 - After the Storm

Status: redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text, not scene phases/actions.

## Quest Record

[`2B9BAB zzzCHMemoryQuest11 "After the Storm"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)

CLI:
- `questdiag Vigilant.esm 0x2B9BAB`
- `infodiag Vigilant.esm 0x2B9BAB`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x2B9BAB`
- EditorID: `zzzCHMemoryQuest11`
- Name: `After the Storm`
- Flags: `RunOnce`
- Priority: `90`
- Type: `Misc`
- Filter: `CH\`

Stages from `questdiag` (16):

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 10 | none | empty |
| 20 | none | empty |
| 30 | none | empty |
| 40 | none | empty |
| 50 | CompleteQuest | empty |
| 60 | none | empty |
| 300 | none | empty |
| 305 | none | empty |
| 310 | none | empty |
| 315 | none | empty |
| 320 | none | empty |
| 330 | none | empty |
| 340 | CompleteQuest | empty |
| 350 | none | empty |
| 999 | ShutDownStage | empty |

Objective:
- `questdiag` reports `Objectives (0)`. The quest carries no objective text; `quests.md` line 256 is a header-only entry with no `[obj N]` lines.

Stage-band shape (source-grounded):
- Two `CompleteQuest` stages: **50** (low band, stages 0-60) and **340** (high band, stages 300-350).
- The two bands map to the two staged scenes named in `find`: [`2B9BB4 zzzCHMeQ11GoodScene`](#2b9bb4-zzzchmeq11goodscene) and [`2BAEFB zzzCHMeQ11BadScene`](#2baefb-zzzchmeq11badscene). The EditorID names `GoodScene` / `BadScene` are the source-grounded polarity labels (see Branch Outcomes).

Stage routing (verified from `qf_zzzchmemoryquest11_022b9bab.psc` + SF scripts):

| Stage | Fragment | Action |
|---:|---|---|
| 0 | `Fragment_0` | FadeOut → `Game.GetPlayer().MoveTo(StartMarker)` → `SetStage(20)` |
| 10 | `Fragment_2` | Disable `SoulsStream`; `Alias_Bull/Priest.SetAlpha(0.0)` |
| 20 | `Fragment_4` | `MusWar.Add()`; restore alpha; play `TeleportINEffect`; `Sc01.Start()`; register skip(40) |
| 40 | `Fragment_7` | **Branch gate**: `if !PelinalQuest.GetStageDone(300)` → `GoodScene.ForceStart()` else `BadScene.ForceStart()` + `SetStage(300)` |
| 50 | (GoodScene `Fragment_0`) | `SF_zzzCHMeQ11GoodScene`: `GetOwningQuest().SetStage(50)` — **Good CompleteQuest** |
| 60 | `Fragment_11` | `AkatoshQuest.Start()` + `AkatoshQuest.SetStage(0)` (post-good) |
| 300 | `Fragment_13` | Make Priest killable: `SetProtected/Essential/Invulnerable(false)`, `SetActorValue("Health",1)` |
| 305 | | `ISMDizzy` + `HeartBeat` + camera shake + `MusBad.Add()` + `OblivionRef.Enable()` |
| 310 | (BadScene `Fragment_0`) | `SF_zzzCHMeQ11BadScene`: `SetStage(310)` |
| 315 | (BadScene `Fragment_2`) | `SF_zzzCHMeQ11BadScene`: `SetStage(315)` |
| 320 | | — |
| 330 | (BadScene `Fragment_3`) | `SF_zzzCHMeQ11BadScene`: `SetStage(330)` |
| 340 | (BadScene `Fragment_5`) | `SF_zzzCHMeQ11BadScene`: `setStage(340)` — **Bad CompleteQuest** |
| 350 | `Fragment_21` | `TeleportOutEffect`; `Alias_Bull.TryToDisable()`; FadeOut; `MusBad.Remove()`; `MoveTo(ReturnMarker)`; `SetStage(350)` |
| 350 | `Fragment_23` | `AkatoshQuest.Start()` + `AkatoshQuest.SetStage(0)` (post-bad) |

Additional QF fragments (within Bad branch, stage inferred from behavior):
- `Fragment_17`: `ISMDizzy.ApplyCrossFade(0.1)` + heartbeat/camera shake + `MusBad.Add()` + `OblivionRef.Enable()` — fires around stage 305 (Bad branch entry)
- `Fragment_27`: `;Bad Scene` comment only (placeholder)
- `Fragment_29`: `Alias_Bull.SetDontMove()` + `debug.SendAnimationEvent("pa_KillMove2HWB")` + `PriestBloodMarker.Enable()` + `Alias_Priest.TryToKill()` — fires during the kill animation stage (~320–330)
- `Fragment_33`: `if qGuide.IsRunning() → qGuide.SetStage(110)`; `kmyQuest.ModRadiance(3.0)` — **hub progression** (Dream11 Finished); fires at one of the CompleteQuest stages (exact stage-to-fragment mapping requires VMAD binary read; inference: fires at stage 50 or 340)

`PelinalQuest` property = [`2A532E zzzCHMemoryQuest10 "Pelinal the Bloody"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401). `PelinalQuest.GetStageDone(300)` checks whether MeQ10 reached its **Bad CompleteQuest** stage (stage 300, verified via `questdiag 0x2A532E`). MeQ10 good CompleteQuest = stage 180; bad CompleteQuest = stage 300.

`AkatoshQuest` fires on both outcomes (stage 60 post-good; stage 350 post-bad). Identity of `AkatoshQuest` is not resolvable from PSC alone (property name only); likely a post-memory follow-on quest (inference: possibly `zzzCHMemoryQuest12 "Last Night"` which features Akatosh, or a dedicated bridge quest; unverified).

## Alias / Staging Backbone

The three `SCEN` records below share the same host quest and aliases.

Host quest:
- [`2B9BAB zzzCHMemoryQuest11`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)

Host-quest aliases from `scenediag Vigilant.esm 0x2B9BB4` (confirmed identical across all three scenes):

| Alias | Name | Fill | Resolves to |
|---:|---|---|---|
| 0 | `Bull` | uniqueActor `2B8827:Vigilant.esm` | [`2B8827 zzzCHMemoryMorihaus01 "Morihaus"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:782) |
| 1 | `Priest` | uniqueActor `2B882A:Vigilant.esm` | [`2B882A zzzCHMemorySthunPriest "Stuhn Priest"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:781) |
| 2 | `Akatosh` | uniqueActor `2DE6E3:Vigilant.esm` | [`2DE6E3 zzzCHMemoryAkatoshMorihaus` (no Name)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:809) |
| 3 | `PelinalMarker` | forcedRef `2E47DF:Vigilant.esm` | placed marker ref |
| 4 | `GateMarker` | forcedRef `2E47E0:Vigilant.esm` | placed marker ref |
| 5 | `Gardener` | uniqueActor `2E47F0:Vigilant.esm` | [`2E47F0 zzzCHMemoryGardener "King of Nenalata"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:805) |

Alias fills are all `uniqueActor` or `forcedRef` — no conditional or find-condition fills. The alias table is fully source-grounded from `scenediag` output.

RESOLVED: all 6 aliases confirmed present by `scenediag 0x2B9BB4`/`0x2B9BB5`/`0x2BAEFB`; no alias is unverified.

Subject / speaker:
- The memory's subject and the through-line speaker is **Morihaus** (alias `#0` `Bull`). All Scene monologue lines are voiced by alias `#0` except where noted, and the custom branch openers are gated on the player standing before either alias `#2` `Akatosh` or alias `#5` `Gardener`.
- `Stuhn Priest` (alias `#1`) is the second on-stage actor (the priest who refuses the order in the bad branch).
- `Bull` = Morihaus, who in TES lore is the winged man-bull consort of Alessia; "Paravania" (named in topic [`2B9BBF`](#2b9bb4-zzzchmeq11goodscene)) is Alessia's man-bull aspect. (inference, cross-checked against [`51AE2D zzzCHAlessiaMntr "Paravania the Man-bull"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:428))

Trigger (source-grounded shape):
- `questdiag` does not print target refs and the QUST start condition is not dumped by the current CLI, so the exact in-world trigger ref is not decoded here.
- The scene actor `Bull`/`Priest`/`Akatosh`/`Gardener` are `uniqueActor` fills and `PelinalMarker`/`GateMarker` are `forcedRef` markers; the memory is entered by approaching the staged Pelinal-death tableau (inference, from alias names + scene staging). The walk packages confirm the staging: `zzzCHMeq11PriestWalkToPelinal`, `zzzCHMeq11MorihausWalkToPelinak [sic]`, `zzzCHMeq11MorihausPrayForPelinal` ([from `find`](#packages-from-find)).

## Scene Records

Scene records are not present as full records in `game-data`; the text lines are linked to `dialogue.md`, while phases/actions are from `scenediag`. All three scenes share the same 13 scene-category topics (each scene's `scenediag` lists the full 13, but only a subset is actually played per scene via its `Dialog` actions).

### 2B9BB5 zzzCHMeQ11Sc01

CLI:
- `scenediag Vigilant.esm 0x2B9BB5` — RESOLVED

Staging (verified from `scenediag` + `sf_zzzchmeq11sc01_022b9bb5.psc`):
- Host quest: [`2B9BAB zzzCHMemoryQuest11`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)
- Flags: none
- Actors: alias `#0` (`Bull`, `behaviorFlags=DeathEnd,DialoguePause`, `flags=NoPlayerActivation,Optional`), alias `#1` (`Priest`, `behaviorFlags=DeathEnd,DialoguePause`, `flags=NoPlayerActivation,Optional`)
- Phases: 2; phase[0] `startConds=0 completeConds=1`; phase[1] `startConds=0 completeConds=1`
- Actions (6): `Package` on `#0` phase[0]; `Package` on `#1` phase[0]; `Package` on `#0` phase[1]; `Package` on `#1` phase[1]; `Dialog` on `#0` phases[0-1] `Topic=<null>`; `Dialog` on `#1` phases[0-1] `Topic=<null>`
- Scene completion fragment: `SF_zzzCHMeQ11Sc01_022B9BB5 Fragment_0` → `GetOwningQuest().SetStage(40)` (triggers the main branch gate)
- Note: establishing walk-up scene (silent); no spoken dialogue — all Dialog actions have `Topic=<null>`. The two walk packages (`MorihausWalkToPelinak`, `PriestWalkToPelinal`) and pray package (`MorihausPrayForPelinal`) run here. RESOLVED (inference note: "likely silent" confirmed by `Topic=<null>` on all Dialog actions).

### 2B9BB4 zzzCHMeQ11GoodScene

CLI:
- `scenediag Vigilant.esm 0x2B9BB4` — RESOLVED

Staging (verified from `scenediag` + `sf_zzzchmeq11goodscene_022b9bb4.psc`):
- Host quest: [`2B9BAB zzzCHMemoryQuest11`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)
- Flags: none
- Actors: alias `#0` (`Bull`, `behaviorFlags=DialoguePause`, `flags=NoPlayerActivation,Optional`), alias `#1` (`Priest`, `behaviorFlags=DeathEnd,DialoguePause`, `flags=NoPlayerActivation,Optional`), alias `#2` (`Akatosh`, behaviorFlags=0, flags=0)
- Phases: 5; phase[0] `completeConds=3` (3 conditions to exit, likely scene skip + NPC positions + timer); phases[1-4] `completeConds=1` each
- Actions (10): `Package` on `#0` phase[0]; `Package` on `#1` phases[0-4]; `Package` on `#0` phases[1-4]; four `Dialog` actions on `#0` (phases 1-4, topics `2B9BB9`/`2B9BBB`/`2B9BBD`/`2B9BBF`); `Dialog` on `#1` phases[1-4] `Topic=<null>`; `Timer` (3s) on `#0` phase[0]; `Dialog` on `#2` phases[0-4] `Topic=<null>`
- Scene completion fragments (`sf_zzzchmeq11goodscene_022b9bb4.psc`):
  - `Fragment_0`: `GetOwningQuest().SetStage(50)` — **fires at scene end, triggers Good CompleteQuest**
  - `Fragment_1`: `q.RegisterSceneSkip(GetOwningQuest(), self, 50, True)` — skip registration

Morihaus monologue (alias `#0`), played in phase order:

| Phase | Topic / INFO | Source | Translation |
|---:|---|---|---|
| 1 | `2B9BB9` / `2B9BBA` | [line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2589) | 「你走了……這樣的結局，真像你的作風……」 |
| 2 | `2B9BBB` / `2B9BBC` | [line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2592) | 「Pelinal，是你教我的。Ada 必須以愛來改變一切……」 |
| 3 | `2B9BBD` / `2B9BBE` | [line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2595) | 「正因如此，我的心才這麼痛。陷入嗜血、向狂怒交出自己，反而還比較容易。」 |
| 4 | `2B9BBF` / `2B9BC0` | [line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2598) | 「但即便如此，我仍守望著你所造的這個世界……為了她，為了 Paravania……」 |

- Note: `Ada` is the Ehlnofex term for a divine/original spirit; left untranslated. Reading is "the divine ones / the world must be changed through love"; kept literal pending verification - 待驗證.

### 2BAEFB zzzCHMeQ11BadScene

CLI:
- `scenediag Vigilant.esm 0x2BAEFB` — RESOLVED

Staging (verified from `scenediag` + `sf_zzzchmeq11badscene_022baefb.psc`):
- Host quest: [`2B9BAB zzzCHMemoryQuest11`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)
- Flags: none
- Actors: alias `#0` (`Bull`, `behaviorFlags=DeathEnd,DialoguePause`, `flags=NoPlayerActivation,Optional`), alias `#1` (`Priest`, `behaviorFlags=DialoguePause`, `flags=NoPlayerActivation,Optional`)
- Phases: 16; phase[0] `completeConds=2`; phases[1-15] `completeConds=1` each
- Actions (23): complex chain of `Package` + `Timer` + `Dialog` across 16 phases. Full action map from `scenediag`:
  - Phase[0]: `Package#0`, `Package#1`, `Timer#0` (3s)
  - Phase[1]: `Dialog#0` `Topic=2BAEFC` (Neutral) — "Why Pelinal...what happened..."
  - Phase[2]: `Dialog#0` `Topic=2BAEFE` (Neutral) — "Pelinal...say something..."
  - Phase[3]: `Dialog#1` `FaceTarget` `Topic=2BAF00` (Sad100) — "Lord Morihaus, get a hold..."
  - Phase[4]: `Dialog#0` `Topic=2BAF37` (Neutral) — silence beat `"........................"`
  - Phase[5-6]: `Package#0`, `Dialog#0` `FaceTarget#1` `Topic=2BAF03` — "Slay captives..."
  - Phase[7]: `Dialog#1` `FaceTarget#0` `Topic=2BAF05` (Puzzled) — "What...what do you say?"
  - Phase[8]: `Package#0`, `Dialog#1` `FaceTarget#0` `Topic=2BAF07` (Anger) — "That's against Sthun..."
  - Phase[9]: `Package#0`, `Timer#0` (2s)
  - Phase[10-11]: `Package#0`, `Timer#0` (2.5s) at phase[11]
  - Phase[12]: `Package#0`
  - Phase[13]: `Package#0`, `Dialog#0` `Topic=2BAF0B` — "I go mad into blood..."
  - Phase[14]: `Package#0`, `Dialog#0` `HeadtrackActorID=4` `Topic=2BAF0E` — "Cyrod is already ours..."
  - Phase[15]: `Package#0`, `Dialog#0` `HeadtrackActorID=4` `Topic=<null>`
- Scene completion fragments (`sf_zzzchmeq11badscene_022baefb.psc`):
  - `Fragment_0`: `GetOwningQuest().SetStage(310)` — phase progress
  - `Fragment_2`: `GetOwningQuest().SetStage(315)` — phase progress
  - `Fragment_3`: `GetOwningQuest().SetStage(330)` — phase progress
  - `Fragment_5`: `GetOwningQuest().setStage(340)` — **fires at scene end, triggers Bad CompleteQuest**
  - `Fragment_6`: `q.RegisterSceneSkip(GetOwningQuest(), self, 340, True)` — skip registration

Branch dialogue (in scene-action phase order):

| Phase | Actor | Topic / INFO | Emotion | Source | Translation |
|---:|---|---|---|---|---|
| 1 | `#0` Morihaus | `2BAEFC` / `2BAEFD` | Neutral | [line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2601) | 「Pelinal……究竟發生了什麼……」 |
| 2 | `#0` Morihaus | `2BAEFE` / `2BAEFF` | Neutral | [line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2604) | 「Pelinal……說句話吧，拜託你。再像從前那樣鼓舞我們……」 |
| 3 | `#1` Priest | `2BAF00` / `2BAF01` | Sad(100) | [line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2607) | 「Morihaus 大人，請振作起來……」 |
| 4 | `#0` Morihaus | `2BAF37` / `2BAF38` | Neutral | [line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2622) | （沉默：「……………………」） |
| 5-6 | `#0` Morihaus | `2BAF03` / `2BAF04` | Neutral | [line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2610) | 「……殺光俘虜。還有，殺光所有精靈居民，連同他們的牲口。」 |
| 7 | `#1` Priest | `2BAF05` / `2BAF06` | Puzzled | [line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2613) | 「您……您說什麼？這是瘋了。」 |
| 8 | `#1` Priest | `2BAF07` / `2BAF08` | Anger | [line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2616) | 「這違背了 Sthun 的教誨……」 |
| 13 | `#0` Morihaus | `2BAF0B` / `2BAF0C` | Neutral | [line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2619) | 「Pelinal，我從你身上學會了。我陷入嗜血，向狂怒交出自己。」 |
| 14 | `#0` Morihaus | `2BAF0E` / `2BAF0F` | Neutral | [line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2622) | 「Cyrod 已是我們的了。一切都被允許。」 |

- Note: `teaching of Sthun` ([`2BAF07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2616)) vs the NPC EditorID `zzzCHMemorySthunPriest` / Name `Stuhn Priest` ([npcs.tsv:781](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:781)): the dialogue spells it `Sthun`, the NPC record `Stuhn`/`Sthun`. Stuhn is the Nordic/Aedric god of ransom; both spellings refer to the same deity. Garbled source spelling - kept as-is, 待驗證.
- Note: `It is easier to go mad into blood and surrender myself to rage` (Good, `2B9BBD`) and `I go mad into blood, surrender myself to rage` (Bad, `2BAF0B`) are the deliberate mirror: in the good scene Morihaus *resists* the urge, in the bad scene he *yields* to it. Source-grounded polarity anchor.
- Note: `Cyrod` = Cyrodiil (period spelling); left as source.
- Note: phase 4's `2BAF37` is a line of pure ellipsis `........................` (silence beat); rendered as a silent pause.

## Custom Dialogue Branch: Akatosh (Good outcome)

Branch:
- `2DE6E7:Vigilant.esm` (`zzzCHMeQ11AkatoshB01`), view `2DE6E6 zzzCHMeQ11AkatoshView`.

Speaker condition pattern:
- INFO requires `GetIsAliasRef == 1` on alias `#2` (`Akatosh`).

| Topic | INFO | Flags | Conditions | Emotion | Translation |
|---|---|---|---|---|---|
| [`2DE6E8 zzzCHMeQ11AkatoshB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2768) | `2DE6E9` | `Goodbye` | `GetIsAliasRef alias #2` | Sad | 「暴風雨過後，便是寧靜。那是何等的哀傷啊……」 |

- This is the player-facing closing line of the **GoodScene** path (Morihaus ascended / watched over by Akatosh). Title "After the Storm" comes directly from this line `After a storm comes a calm`.

## Custom Dialogue Branch: Gardener (Bad outcome)

Branch:
- `2E5B3E:Vigilant.esm` (`zzzCHMeQ11GardenerB01`), view `2E5B3D zzzCHMeQ11GardenerView`.

Speaker condition pattern:
- INFO requires `GetIsAliasRef == 1` on alias `#5` (`Gardener` = "King of Nenalata").

| Topic | INFO | Flags | Conditions | Emotion | Translation |
|---|---|---|---|---|---|
| [`2E5B3F zzzCHMeQ11GardenerB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2794) | `2E5B40` | `Goodbye` | `GetIsAliasRef alias #5` | Sad | 「精靈的時代逝去了，人類的時代來臨了……奈納拉塔之王是對的……」 |

- This is the player-facing closing line of the **BadScene** path (Morihaus slays the priest, "all is permitted"). The `Gardener` is `King of Nenalata` ([npcs.tsv:805](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:805)); cf. `Thannor the Gardener` elsewhere ([npcs.tsv:704](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:704)).
- Note: `Man Era is come` / `Mer Era is gone` is source phrasing (Mer = elves); kept literal.

## Packages (from `find`)

These `zzzCHMeq11*` packages drive the scene actors (source: `find zzzCHMeQ11`; note the mixed `Meq`/`MEq` casing and `Pelinak [sic]` in the source EditorIDs):

- `2B9BB6 zzzCHMeq11MorihausWalkToPelinak` (sic)
- `2B9BB7 zzzCHMeq11PriestWalkToPelinal`
- `2B9BB8 zzzCHMeq11MorihausPrayForPelinal`
- `2B9BC1 zzzCHMeq11PriestFollowMoriaus` (sic, "Moriaus")
- `2BAF02 zzzCHMeq11MorihausStandUpToPriest`
- `2BAF09 zzzCHMEq11MorihausDrawWeapon`
- `2BAF0A zzzCHMeq11MorihausSlayPriest`
- `2BAF0D zzzCHMeq11MorihausGoToOblivion`
- `2BFDAF zzzCHMeq11MorihausStayFrontPelinal`

The package set confirms the two outcomes: pray (good) vs draw-weapon / slay-priest / go-to-Oblivion (bad).

## Branch Outcomes (source-grounded)

| Outcome | Scene | Completion stage | Closing speaker | Closing line |
|---|---|---:|---|---|
| **Good** | [`2B9BB4 zzzCHMeQ11GoodScene`](#2b9bb4-zzzchmeq11goodscene) | 50 (`CompleteQuest`) | Akatosh branch | "After a storm comes a calm" |
| **Bad** | [`2BAEFB zzzCHMeQ11BadScene`](#2baefb-zzzchmeq11badscene) | 340 (`CompleteQuest`) | Gardener branch | "Mer Era is gone, Man Era is come" |

Polarity is **source-grounded by EditorID + PSC verification**:
- The scene records are literally named `GoodScene` and `BadScene`.
- Good = Morihaus grieves but chooses love / restraint (`Ada must change things through love`, `it is easier to go mad... [but he doesn't]`) and is closed by **Akatosh** (chief Aedra) with "after a storm comes a calm."
- Bad = Morihaus yields (`I go mad into blood, surrender myself to rage`, `all is permitted`), orders the massacre of the elven citizens, slays the protesting Stuhn Priest, and the path is closed by the **King of Nenalata** ("Mer Era is gone... the King of Nenalata is right").
- Stage 50 = Good completion: **RESOLVED** — `SF_zzzCHMeQ11GoodScene Fragment_0`: `GetOwningQuest().SetStage(50)` (`sf_zzzchmeq11goodscene_022b9bb4.psc:18`).
- Stage 340 = Bad completion: **RESOLVED** — `SF_zzzCHMeQ11BadScene Fragment_5`: `GetOwningQuest().setStage(340)` (`sf_zzzchmeq11badscene_022baefb.psc:16`).

Branch gate: **RESOLVED** — `QF_zzzCHMemoryQuest11 Fragment_7` (`qf_zzzchmemoryquest11_022b9bab.psc:197–214`):

```papyrus
if !(PelinalQuest.GetStageDone(300))
    GoodScene.ForceStart()        ; MeQ10 ended GOOD (stage 180 CompleteQuest) → GoodScene
else
    BadScene.ForceStart()         ; MeQ10 ended BAD (stage 300 CompleteQuest) → BadScene
    SetStage(300)
endif
```

**The branch is NOT a player choice within MeQ11.** It is determined entirely by the outcome of [`2A532E zzzCHMemoryQuest10 "Pelinal the Bloody"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401):
- MeQ10 `GetStageDone(300)` = **false** (MeQ10 completed at stage 180 = Good outcome) → MeQ11 GoodScene
- MeQ10 `GetStageDone(300)` = **true** (MeQ10 completed at stage 300 = Bad outcome) → MeQ11 BadScene + immediate `SetStage(300)`

MeQ10 stage 180 = Good `CompleteQuest`; stage 300 = Bad `CompleteQuest` (verified via `questdiag Vigilant.esm 0x2A532E`).

## Reconstruction Notes

Source-grounded:
- This memory is represented by [`2B9BAB zzzCHMemoryQuest11 "After the Storm"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256), header-only (no objective text in ESM or `quests.md`).
- Subject/speaker: **Morihaus** (alias `#0` `Bull`, [`2B8827`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:782)), mourning the dead **Pelinal Whitestrake**.
- It contains **3 `SCEN` records**: `2B9BB5 Sc01` (silent establishing), `2B9BB4 GoodScene` (4 Morihaus lines), `2BAEFB BadScene` (16-phase massacre branch).
- It contains **2 custom dialogue branches** (the player-facing closers): Akatosh alias `#2` (Good) and Gardener alias `#5` (Bad), each a single `Goodbye` topic gated on `GetIsAliasRef`.
- **0 books** owned by / linked from this quest (`find` returns no BOOK; no booktext call needed).

## Karma and Hub Progression (source-grounded)

Source: `qf_zzzchmemoryquest11_022b9bab.psc Fragment_33`

```papyrus
If qGuide.IsRunning()
    qGuide.SetStage(110)
endif
kmyQuest.ModRadiance(3.0)
```

- `qGuide` = [`42E0B1 zzzCHMemoryGuide "Memory Guide"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:309) (hub quest property, verified via `qf_zzzchmemoryguide_0242e0b1.psc`)
- `qGuide.SetStage(110)` triggers hub `Fragment_22` (`qf_zzzchmemoryguide_0242e0b1.psc:29`): `;Dream11 Finished` → `SetObjectiveCompleted(110)`, `SetObjectiveDisplayed(120)` — marks the "Against the dying of the light" objective done and advances to objective 120.
- `ModRadiance(3.0)` is the AoM achievement point system (not the vanilla `zzzCHKarma` global `0x0B19F4`). **`zzzCHKarma` is NOT touched by MeQ11.**
- Exact stage at which `Fragment_33` fires: **cannot be determined from PSC alone** (requires VMAD binary stage→fragment mapping from ESM; CLI does not expose this). (inference: fires at one of the CompleteQuest stages 50 or 340, most likely fires for BOTH outcomes since hub progression must register regardless of polarity — consistent with MeQ07 pattern where `Fragment_22` fires at stage 150 (Bad path `CompleteQuest`) and presumably a parallel fragment fires at stage 70 (Good path))

Karma polarity for this quest: **RESOLVED** — MeQ11 has no independent player-choice karma. Its outcome mirrors MeQ10's: if MeQ10 was Good (stage 180), MeQ11 plays Good; if MeQ10 was Bad (stage 300), MeQ11 plays Bad. Both paths call `AkatoshQuest.Start()` + `AkatoshQuest.SetStage(0)` as a post-memory follow-on. `AkatoshQuest` identity: (unverified — property name only; inference: likely the quest that enables MeQ12 or the Akatosh encounter triggered by `2BC397 zzzCHAkatoshMemoryActTrigger`).

Open verification:
- **RESOLVED** — branch gate: `QF Fragment_7` confirms MeQ10 stage 300 determines GoodScene vs BadScene (PSC source-grounded).
- **RESOLVED** — stage routing: stage 50 = Good (`SF_GoodScene Fragment_0`), stage 340 = Bad (`SF_BadScene Fragment_5`) (PSC source-grounded).
- **RESOLVED** — alias fills: all 6 confirmed by `scenediag` (uniqueActor / forcedRef, no ambiguity).
- **RESOLVED** — SCEN staging: all 3 scenes fully diagnosed via `scenediag`; phase/action/topic tables complete.
- **Partial** — `Fragment_33` stage assignment (qGuide.SetStage(110) / ModRadiance): fires at completion but exact stage unknown without VMAD binary read. (unverified: which CompleteQuest stage triggers it)
- **Partial** — `AkatoshQuest` identity: fires post-completion on both paths but EditorID is a property name only. (unverified: actual quest FormID)
- **Kept as-is** — garbled source spellings (待驗證): `Sthun` vs `Stuhn` (priest's god), `WalkToPelinak`/`Moriaus` (package EditorIDs), `Ada` (Ehlnofex, untranslated), `Cyrod` (period spelling of Cyrodiil).
