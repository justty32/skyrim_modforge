# Act 4 Memory 11 - After the Storm

Status: redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text, not scene phases/actions.

## Quest Record

[`2B9BAB zzzCHMemoryQuest11 "After the Storm"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)

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

## Alias / Staging Backbone

The three `SCEN` records below share the same host quest and aliases.

Host quest:
- [`2B9BAB zzzCHMemoryQuest11`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)

Host-quest aliases from `scenediag`:

| Alias | Name | Fill | Resolves to |
|---:|---|---|---|
| 0 | `Bull` | uniqueActor `2B8827:Vigilant.esm` | [`2B8827 zzzCHMemoryMorihaus01 "Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:782) |
| 1 | `Priest` | uniqueActor `2B882A:Vigilant.esm` | [`2B882A zzzCHMemorySthunPriest "Stuhn Priest"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:781) |
| 2 | `Akatosh` | uniqueActor `2DE6E3:Vigilant.esm` | [`2DE6E3 zzzCHMemoryAkatoshMorihaus` (no Name)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:809) |
| 3 | `PelinalMarker` | forcedRef `2E47DF:Vigilant.esm` | marker |
| 4 | `GateMarker` | forcedRef `2E47E0:Vigilant.esm` | marker |
| 5 | `Gardener` | uniqueActor `2E47F0:Vigilant.esm` | [`2E47F0 zzzCHMemoryGardener "King of Nenalata"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:805) |

Subject / speaker:
- The memory's subject and the through-line speaker is **Morihaus** (alias `#0` `Bull`). All Scene monologue lines are voiced by alias `#0` except where noted, and the custom branch openers are gated on the player standing before either alias `#2` `Akatosh` or alias `#5` `Gardener`.
- `Stuhn Priest` (alias `#1`) is the second on-stage actor (the priest who refuses the order in the bad branch).
- `Bull` = Morihaus, who in TES lore is the winged man-bull consort of Alessia; "Paravania" (named in topic [`2B9BBF`](#2b9bb4-zzzchmeq11goodscene)) is Alessia's man-bull aspect. (inference, cross-checked against [`51AE2D zzzCHAlessiaMntr "Paravania the Man-bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:428))

Trigger (source-grounded shape):
- `questdiag` does not print target refs and the QUST start condition is not dumped by the current CLI, so the exact in-world trigger ref is not decoded here.
- The scene actor `Bull`/`Priest`/`Akatosh`/`Gardener` are `uniqueActor` fills and `PelinalMarker`/`GateMarker` are `forcedRef` markers; the memory is entered by approaching the staged Pelinal-death tableau (inference, from alias names + scene staging). The walk packages confirm the staging: `zzzCHMeq11PriestWalkToPelinal`, `zzzCHMeq11MorihausWalkToPelinak [sic]`, `zzzCHMeq11MorihausPrayForPelinal` ([from `find`](#packages-from-find)).

## Scene Records

Scene records are not present as full records in `game-data`; the text lines are linked to `dialogue.md`, while phases/actions are from `scenediag`. All three scenes share the same 13 scene-category topics (each scene's `scenediag` lists the full 13, but only a subset is actually played per scene via its `Dialog` actions).

### 2B9BB5 zzzCHMeQ11Sc01

CLI:
- `scenediag Vigilant.esm 0x2B9BB5`

Staging:
- Host quest: [`2B9BAB zzzCHMemoryQuest11`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)
- Flags: none
- Actors: alias `#0` (`Bull`, `DeathEnd, DialoguePause`), alias `#1` (`Priest`, `DeathEnd, DialoguePause`)
- Phases: 2, each `0` start conditions and `1` complete condition.
- Actions: 6 total - `Package` for `#0` and `#1` per phase, plus `Dialog` actions for `#0` and `#1` with `Topic=<null>`.
- Note: this is the establishing/idle scene; the `Dialog` actions carry no topic, so no spoken line is bound here. Likely the silent walk-up to Pelinal's body (inference, from the walk/pray packages).

### 2B9BB4 zzzCHMeQ11GoodScene

CLI:
- `scenediag Vigilant.esm 0x2B9BB4`

Staging:
- Host quest: [`2B9BAB zzzCHMemoryQuest11`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)
- Flags: none
- Actors: alias `#0` (`Bull`, `DialoguePause`, `NoPlayerActivation, Optional`), alias `#1` (`Priest`, `DeathEnd, DialoguePause`, `NoPlayerActivation, Optional`), alias `#2` (`Akatosh`).
- Phases: 5.
- Actions (10): packages on `#0`/`#1`, a `Timer` (3s) on `#0` at phase 0, four `Dialog` lines voiced by `#0` (`Bull`/Morihaus) one per phase 1-4, plus topic-less `Dialog` actions on `#1` and `#2`.

Morihaus monologue (alias `#0`), played in phase order:

| Phase | Topic / INFO | Source | Translation |
|---:|---|---|---|
| 1 | `2B9BB9` / `2B9BBA` | [line](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2589) | 「你走了……這樣的結局，真像你的作風……」 |
| 2 | `2B9BBB` / `2B9BBC` | [line](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2592) | 「Pelinal，是你教我的。Ada 必須以愛來改變一切……」 |
| 3 | `2B9BBD` / `2B9BBE` | [line](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2595) | 「正因如此，我的心才這麼痛。陷入嗜血、向狂怒交出自己，反而還比較容易。」 |
| 4 | `2B9BBF` / `2B9BC0` | [line](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2598) | 「但即便如此，我仍守望著你所造的這個世界……為了她，為了 Paravania……」 |

- Note: `Ada` is the Ehlnofex term for a divine/original spirit; left untranslated. Reading is "the divine ones / the world must be changed through love"; kept literal pending verification - 待驗證.

### 2BAEFB zzzCHMeQ11BadScene

CLI:
- `scenediag Vigilant.esm 0x2BAEFB`

Staging:
- Host quest: [`2B9BAB zzzCHMemoryQuest11`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)
- Flags: none
- Actors: alias `#0` (`Bull`, `DeathEnd, DialoguePause`), alias `#1` (`Priest`, `DialoguePause`).
- Phases: 16.
- Actions (23): a long chain of `Package` + `Timer` + `Dialog`, voicing both Morihaus (`#0`) and the Stuhn Priest (`#1`). This is the violent branch (the `Morihaus...DrawWeapon` / `...SlayPriest` / `...GoToOblivion` packages from `find` are this scene's package set).

Branch dialogue (in scene-action phase order):

| Phase | Actor | Topic / INFO | Emotion | Source | Translation |
|---:|---|---|---|---|---|
| 1 | `#0` Morihaus | `2BAEFC` / `2BAEFD` | Neutral | [line](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2601) | 「Pelinal……究竟發生了什麼……」 |
| 2 | `#0` Morihaus | `2BAEFE` / `2BAEFF` | Neutral | [line](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2604) | 「Pelinal……說句話吧，拜託你。再像從前那樣鼓舞我們……」 |
| 3 | `#1` Priest | `2BAF00` / `2BAF01` | Sad(100) | [line](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2607) | 「Morihaus 大人，請振作起來……」 |
| 4 | `#0` Morihaus | `2BAF37` / `2BAF38` | Neutral | [line](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2622) | （沉默：「……………………」） |
| 5-6 | `#0` Morihaus | `2BAF03` / `2BAF04` | Neutral | [line](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2610) | 「……殺光俘虜。還有，殺光所有精靈居民，連同他們的牲口。」 |
| 7 | `#1` Priest | `2BAF05` / `2BAF06` | Puzzled | [line](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2613) | 「您……您說什麼？這是瘋了。」 |
| 8 | `#1` Priest | `2BAF07` / `2BAF08` | Anger | [line](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2616) | 「這違背了 Sthun 的教誨……」 |
| 13 | `#0` Morihaus | `2BAF0B` / `2BAF0C` | Neutral | [line](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2619) | 「Pelinal，我從你身上學會了。我陷入嗜血，向狂怒交出自己。」 |
| 14 | `#0` Morihaus | `2BAF0E` / `2BAF0F` | Neutral | [line](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2622) | 「Cyrod 已是我們的了。一切都被允許。」 |

- Note: `teaching of Sthun` ([`2BAF07`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2616)) vs the NPC EditorID `zzzCHMemorySthunPriest` / Name `Stuhn Priest` ([npcs.tsv:781](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:781)): the dialogue spells it `Sthun`, the NPC record `Stuhn`/`Sthun`. Stuhn is the Nordic/Aedric god of ransom; both spellings refer to the same deity. Garbled source spelling - kept as-is, 待驗證.
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
| [`2DE6E8 zzzCHMeQ11AkatoshB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2768) | `2DE6E9` | `Goodbye` | `GetIsAliasRef alias #2` | Sad | 「暴風雨過後，便是寧靜。那是何等的哀傷啊……」 |

- This is the player-facing closing line of the **GoodScene** path (Morihaus ascended / watched over by Akatosh). Title "After the Storm" comes directly from this line `After a storm comes a calm`.

## Custom Dialogue Branch: Gardener (Bad outcome)

Branch:
- `2E5B3E:Vigilant.esm` (`zzzCHMeQ11GardenerB01`), view `2E5B3D zzzCHMeQ11GardenerView`.

Speaker condition pattern:
- INFO requires `GetIsAliasRef == 1` on alias `#5` (`Gardener` = "King of Nenalata").

| Topic | INFO | Flags | Conditions | Emotion | Translation |
|---|---|---|---|---|---|
| [`2E5B3F zzzCHMeQ11GardenerB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2794) | `2E5B40` | `Goodbye` | `GetIsAliasRef alias #5` | Sad | 「精靈的時代逝去了，人類的時代來臨了……奈納拉塔之王是對的……」 |

- This is the player-facing closing line of the **BadScene** path (Morihaus slays the priest, "all is permitted"). The `Gardener` is `King of Nenalata` ([npcs.tsv:805](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:805)); cf. `Thannor the Gardener` elsewhere ([npcs.tsv:704](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:704)).
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

Polarity is **source-grounded by EditorID**, not just inferred:
- The scene records are literally named `GoodScene` and `BadScene`.
- Good = Morihaus grieves but chooses love / restraint (`Ada must change things through love`, `it is easier to go mad... [but he doesn't]`) and is closed by **Akatosh** (chief Aedra) with "after a storm comes a calm."
- Bad = Morihaus yields (`I go mad into blood, surrender myself to rage`, `all is permitted`), orders the massacre of the elven citizens, slays the protesting Stuhn Priest, and the path is closed by the **King of Nenalata** ("Mer Era is gone... the King of Nenalata is right").
- Stage 50 (low band) = Good completion; stage 340 (high band) = Bad completion (inference, by mapping the two `CompleteQuest` bands to the two scenes; consistent with the package set but the exact stage->scene wiring is in the un-dumped stage fragments).

## Reconstruction Notes

Source-grounded:
- This memory is represented by [`2B9BAB zzzCHMemoryQuest11 "After the Storm"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256), header-only (no objective text in ESM or `quests.md`).
- Subject/speaker: **Morihaus** (alias `#0` `Bull`, [`2B8827`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:782)), mourning the dead **Pelinal Whitestrake**.
- It contains **3 `SCEN` records**: `2B9BB5 Sc01` (silent establishing), `2B9BB4 GoodScene` (4 Morihaus lines), `2BAEFB BadScene` (16-phase massacre branch).
- It contains **2 custom dialogue branches** (the player-facing closers): Akatosh alias `#2` (Good) and Gardener alias `#5` (Bad), each a single `Goodbye` topic gated on `GetIsAliasRef`.
- **0 books** owned by / linked from this quest (`find` returns no BOOK; no booktext call needed).

Open verification:
- dump QUST aliases/targets + start condition to pin the exact in-world trigger ref (CLI does not print these);
- read the stage fragments / VMAD on stages 50 / 340 to confirm which band each scene completes and what each grants;
- the per-scene branch *choice point* (what the player does to route Good vs Bad) is encoded in the scene phase `completeConds` (not printed in detail by `scenediag`) and/or a karma global - needs a deeper dump;
- garbled source spellings to keep + flag (待驗證): `Sthun` vs `Stuhn` (priest's god), `WalkToPelinak`/`Moriaus` (package EditorIDs), `Ada` (untranslated Ehlnofex), `Cyrod` (period spelling of Cyrodiil).
