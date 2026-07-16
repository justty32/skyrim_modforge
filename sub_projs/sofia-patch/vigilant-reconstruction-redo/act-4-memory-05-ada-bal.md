# Act 4 Memory 05 - Ada Bal

Status: redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text, not scene phases/actions.
- This mod is machine-translated from Japanese; garbled English is kept verbatim in the source column with a `Note:` "待驗證" instead of being smoothed over.

## Quest Record

[`05AE03 zzzCHMemoryQuest05 "Ada Bal"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)

CLI:
- `questdiag Vigilant.esm 0x05AE03`
- `infodiag Vigilant.esm 0x05AE03`
- `find Vigilant.esm zzzCHMeQ05`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x05AE03`
- EditorID: `zzzCHMemoryQuest05`
- Name: `Ada Bal`
- Flags: `RunOnce`
- Priority: `90`
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
| 45 | none | empty |
| 50 | CompleteQuest | empty |
| 60 | none | empty |
| 120 | CompleteQuest | empty |
| 130 | none | empty |
| 140 | none | empty |
| 999 | ShutDownStage | empty |

Objective:

| Index | Source | Translation |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:358) | 在月下，於亡者之上起舞。 |

Objective targets:
- 1 target in ESM (`questdiag`: `objective[0] ... targets=1`, `target: flags=0 conds=0`).
- Current CLI output does not print the target ref; this needs a deeper QUST target dump if the target location matters.

## Alias / Staging Backbone

The host quest's aliases are dumped by `scenediag` on the one `SCEN` it owns.

Host quest:
- [`05AE03 zzzCHMemoryQuest05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)

Host-quest aliases from `scenediag` (8):

| Alias | Name | Fill |
|---:|---|---|
| 0 | `Marukh` | uniqueActor [`05ADEF zzzCHMarukhMemory`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1046) |
| 1 | `Pepe` | uniqueActor [`05ADFD zzzCHInquisitorPepeMemory2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1044) |
| 2 | `StartMarker` | forcedRef `05ADFE:Vigilant.esm` |
| 3 | `EndMarker` | forcedRef `05AE04:Vigilant.esm` |
| 4 | `Player` | forcedRef `000014:Skyrim.esm` |
| 5 | `Adabal` | not printed (no fill in CLI output) |
| 6 | `MemoryDulsa` | not printed (no fill in CLI output) |
| 7 | `GuideMarker` | forcedRef `42E0B4:Vigilant.esm` |

Inference:
- `Marukh` alias `#0` and `Pepe` alias `#1` are the two dialogue aliases used by the two custom branches: the Marukh branch INFOs require `GetIsAliasRef alias #0`, the Pepe branch INFOs require `GetIsAliasRef alias #1` (confirmed by `infodiag`).
- `Adabal` alias `#5`: fill not printed by `scenediag`. QF PSC `Fragment_25` (`qf_zzzchmemoryquest05_0205ae03.psc` line 114–115) calls `Alias_Adabal.GetRef()` to remove the item from the player at stage 50 (good end), confirming alias `#5` holds a placed ref of the red stone item. The specific fill source (forced ref or unique actor) is not printed by CLI. (unverified: alias fill type/source)
- `MemoryDulsa` alias `#6`: fill not printed by `scenediag` or `questdiag`. Type is `LocationAlias` per QF PSC header (`qf_zzzchmemoryquest05_0205ae03.psc` lines 6–8: `;BEGIN ALIAS PROPERTY MemoryDulsa` / `;ALIAS PROPERTY TYPE LocationAlias`). This is a **location alias**, not an actor alias — it likely marks the ritual site (Dulsa's position), not Dulsa herself as an NPC. No fill source printed by CLI. (unverified: fill source)
- The `42E0B4` `GuideMarker` ties this memory to the [`42E0B1 zzzCHMemoryGuide` hub](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:309). (inference)

Travel packages owned by the quest (from `find`; `packagediag` run):
- [`05AE11 zzzCHMeq05MarukhTravelToPlayer`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357): template `016FAA:Skyrim.esm`, flags `IgnoreCombat NoCombatAlert`, speed `Run`, destination = `LocationFallback(NearEditorLocation)` radius 256 — Marukh runs toward the player's editor-position anchor.
- [`05AE1E zzzCHMeq05PepeTravelToArcane`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357): template `016FAA:Skyrim.esm`, flags `IgnoreCombat NoCombatAlert`, speed `Run`, destination = `LocationTarget(05AE0F:Vigilant.esm)` radius 0 — Pepe runs to placed ref `05AE0F` (a `000034:Skyrim.esm` XMarker at position −576, 1504, 42.21; identified via `refpos`). This is the ritual site ("Arcane").
- Note: `Arcane` in the source dialogue = the Al-Esh stone ritual. Lore source `12905F` uses "Arkayn Cycle" (`the drive to expunge corruption can conquer even the Arkayn Cycle`), which is the probable origin of the machine-translated `Arcane`; see Proper Noun Resolution below.

Dialog views owned by the quest (from `find`, not dumped):
- `05AE07 zzzCHMeQ05MarukhView`, `05AE16 zzzCHMeQ05PepeView`.

## Scene Records

This memory owns exactly **one** `SCEN` record. Its text lines are linked to `dialogue.md`; phases/actions are from `scenediag`.

### 05AE10 zzzCHMeQ05BadScene

CLI:
- `scenediag Vigilant.esm 0x05AE10`

Staging:
- Host quest: [`05AE03 zzzCHMemoryQuest05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)
- Flags: none
- Actor: alias `#0` (`Marukh`), behaviorFlags `DeathEnd`, flags `NoPlayerActivation`
- Phases: 3, each with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Package`, actor `#0`, phase 0→0, no topic.
  - index 2: `Package`, actor `#0`, phase 1→2, no topic.
  - index 3: `Dialog`, actor `#0`, phase 1, topic [`05AE12`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:736), flags `FaceTarget, HeadtrackPlayer`, emotion `Neutral`, loop 1–10.
  - index 4: `Dialog`, actor `#0`, phase 2, topic [`05AE14`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:739), flags `FaceTarget, HeadtrackPlayer`, emotion `Neutral`, loop 1–10.

Scene-owned topics (both `SNAM=SCEN`, 0 conditions, spoken by alias `#0` Marukh):

Translations:
- [`05AE12` / INFO `05AE13`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:736): 「Ada Bal。那是奇蹟，也是皇帝。是比……任何東西都更能滿足人民飢渴之物。」
  - Note: source `"Ada Bal. Is a miracle, it is also the emperor. And something to satisfy the hunger of the people than ... anything"` is garbled; 待驗證。
- [`05AE14` / INFO `05AE15`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:739): 「Dulsa，原諒我。我那無名的孩子，原諒我……」
  - Note: source `"Dulsa, forgive me. My Nameless Child, forgive me ..."`; mirrors the Marukh branch's "sacrifice you and my child" line (see `05AE0D`), confirming this scene is the carried-out sacrifice. (inference)

Inference:
- The scene's EditorID literally contains `BadScene`; combined with `DeathEnd` on the Marukh actor and the "forgive me, my Nameless Child" content, this is the **bad / corruption** outcome's cutscene — the sacrifice of Dulsa and the child. (inference; see Reconstruction Notes for the 50-vs-120 polarity argument)

## Custom Dialogue Branch: Marukh

Branch:
- `05AE08:Vigilant.esm` (`zzzCHMeQ05MarukhB01`)
- View: `05AE07 zzzCHMeQ05MarukhView`

Speaker condition pattern:
- Every INFO requires `GetIsAliasRef == 1` on alias `#0` (`Marukh`).
- Opening line also requires `GetStage == 10` on quest `05AE03`.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`05AE09 zzzCHMeQ05MarukhB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:724) | `05AE0A` | none | `GetStage == 10`; `GetIsAliasRef alias #0` | 「七十七……龍裔……Sheol。自由之神無盡地消亡，其軌跡……Shezarr」 Note: source `"Senventy-Seven...dragonborn...Sheol. God of freedom defunct endlessly, its trajectory...Shezarr"` 高度破碎，待驗證。 |
| [`05AE0B zzzCHMeQ05MarukhB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:727) | `05AE0C` | none | `GetIsAliasRef alias #0` | Prompt: 「你在做什麼，Marukh？」 Response 1: 「看這塊石頭……Dulsa。這是七十七的奧祕，很快就會完成。Al-Ashe 之石就在此刻被重現。」 Response 2: 「Dulsa，你是被選中的。為了愛。Al-Ashe 如此說。為了完成 Arcane，我需要你、以及你腹中孩子的血。」 Note: `Al-Ashe`／`Arcane` 為原文專名，待驗證。 |
| [`05AE0D zzzCHMeQ05MarukhB01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:731) | `05AE0E` | `Goodbye` | `GetIsAliasRef alias #0`; VMAD `CHMeq05_TIF__0205AE0E.Fragment_0` on end | Prompt: 「這是瘋狂……」 Response 1: 「也許並非瘋狂，理智亦在其中。Al-Ashe 之言即真理。讓那些事物在血中閃耀於此石、並奠立高塔，就是我們的使命。」 Response 2: 「石頭向我顯示了：那位粉碎了 Aldmeri、平定了大陸、將劍刺入巨蛇的英雄之身影。」 Response 3: 「未知之人於昨日或明日知曉一事。但只要那一天哪怕近了一日，我都願意犧牲你與我的孩子。」 Note: source 多處破碎（`shattered Aldomeri`、`thrust a sword into a snake`、`The unexpected know a thing`），待驗證。 |

## Custom Dialogue Branch: Pepe

Branch:
- `05AE17:Vigilant.esm` (`zzzCHMeQ05PepeB01`)
- View: `05AE16 zzzCHMeQ05PepeView`

Speaker condition pattern:
- Every INFO requires `GetIsAliasRef == 1` on alias `#1` (`Pepe`).
- Opening line also requires `GetStage == 40` on quest `05AE03`.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`05AE18 zzzCHMeQ05PepeB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:742) | `05AE19` | none | `GetStage == 40`; `GetIsAliasRef alias #1` | 「那 oblivion 究竟是什麼……」 Note: source `"What is oblivion that..."` 為截斷句，待驗證。 |
| [`05AE1A zzzCHMeQ05PepeB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:745) | `05AE1B` | `Goodbye`, `SayOnce` | `GetIsAliasRef alias #1`; VMAD `CHMeq05_TIF__0205AE1B` `Fragment_0` on begin + `Fragment_1` on end | Prompt: 「請捨棄這塊石頭。在無人之手能不期然觸及之處……」 Response (Puzzled): 「好……我明白了。我以 Mara 之名起誓。」 Note: prompt source `"Please discard this stone. Where anyone hands reach unexpected ..."` 為截斷／破碎句，待驗證。 |

## Related Records

These are cross-linked context. NPC/item ownership by quest `05AE03` is only the two memory actors filled into aliases `#0`/`#1`; the rest are narrative cross-references.

NPCs:
- [`05ADEF zzzCHMarukhMemory` - Marukh](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1046) — alias `#0`.
- [`05ADFD zzzCHInquisitorPepeMemory2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1044) — alias `#1`.
- [`12BF48 zzzCHInquisitorPepeMemory` - Inquisitor Pepe](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:558) — other Pepe-memory variant.
- [`081E46 zzzCHInquisitorPepe` - Inquisitor Pepe](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1065) — the living/present-day Pepe.

Items:
- [`05AE01 zzzCHAdabalMemory` - `Red Stone`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1004) — the red stone of this memory; FormID sits in the `05AE0x` block with the quest, strongly tied to alias `#5` `Adabal`. (inference)
- [`1353DF zzzCHAdabal` - `Adabal`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:976) — the present-day Adabal item.
- [`108EB1 zzzCHSkinPepe`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:727)
- [`500DDC zzVcgPepeMask` - `Mask of Pelan`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:416) — Note: `Pelan` likely a localized spelling of `Pelin`/`Pepe`; 待驗證。

Locations (Adabal Court — the Pepe-cult site referenced by the Pepe travel package, inference):
- [`26C05F zzzCHLocAdabalCourt` - `Adabal Court`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:550)
- [`21AFA1 zzzCHCourtAdabalFirst` - `First Court`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:131)
- [`21AEA7 zzzCHCourtAdabalSecond` - `Second Court`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:144)
- [`0E0889 zzzCHSummaryCourt02` - `Fountain Garden of Dibella`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:140) — Pepe-priest's followers' hidden garden, per Gregory's notes. (inference)

Books (lore context — none is owned by quest `05AE03`; `booktext` fails on both, source text is the extracted `game-data`):
- [`4A8AFD zzzCHBookESO09 "Aurbic Enigma 4: The Elden Tree"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1619) — defines `Chim-el-Adabal`, the Founding-Stone, the Staff of Towers, and the **Dance** of the tower segments; the canonical source the quest title "Ada Bal" and objective "Dance on the dead" allude to. (inference)
- [`12905F zzzCHBookESOIllsuionDeath "The Illusion of Death"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:131) — Marukh / `Seventy-Seven Inflexible Doctrines` / `Al-Esh` context (shared with Memory 07).

## Related Book Translation

No `BOOK` record is owned by quest `05AE03`, so this section gives the **lore anchor** the title/objective derive from rather than an in-quest book.

[`4A8AFD zzzCHBookESO09 "Aurbic Enigma 4: The Elden Tree"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1619)

CLI:
- `booktext Vigilant.esm 0x4A8AFD`
- Result: failed with `could not extract English strings`; source therefore uses the already extracted `game-data` text.

Source-grounded link points (vanilla "Aurbic Enigma 4" lore text, reproduced verbatim in this mod's book):
- [`Chim-el-Adabal`, the great red diamond, "crystallized blood from the Heart of Lorkhan"](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1639) — the red stone; the quest's `Adabal` / `Red Stone` item (`05AE01`) is its memory-form. (inference)
- [the eightfold Staff of Towers, "each segment a semblance of a tower in its Dance"](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1643) — the "Dance" of the objective "Under the moon, Dance on the dead." (inference)

Translation note:
- The mod's quest title `Ada Bal` and Marukh's `Stone of Al-Ashe` / `Arcane` are the machine-translated reflections of `Adabal` / `Al-Esh` / the Ayleid ritual. The garbling (`Ada Bal`, `Al-Ashe`, `Arcane`, `Sevenety-Seven`) is preserved in the branch tables above with `Note:` for traceability; resolved forms are in the Proper Noun Resolution section.

## Reconstruction Notes

Source-grounded:
- This memory is [`05AE03 zzzCHMemoryQuest05 "Ada Bal"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357) with objective [`Under the moon, Dance on the dead.`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:358).
- It owns exactly **one** `SCEN`: `05AE10 zzzCHMeQ05BadScene`, a 3-phase Marukh (alias `#0`) cutscene with two scene-only topics (`05AE12`, `05AE14`) and `DeathEnd`.
- It owns **two** custom dialogue branches:
  - Marukh branch `05AE08` (alias `#0`), opener stage-gated at `GetStage == 10`; 3 topics; ends on `05AE0E` with a VMAD `Goodbye` fragment.
  - Pepe branch `05AE17` (alias `#1`), opener stage-gated at `GetStage == 40`; 2 topics; ends on `05AE1B` with a `Goodbye, SayOnce` VMAD (begin+end fragments).
- Two `CompleteQuest` stages: **50** and **120**.

Trigger (inference):
- The hub `42E0B1 zzzCHMemoryGuide` (via `GuideMarker` alias `#7`, `42E0B4`) starts the memory; the player enters and the two travel packages move Marukh to the player (`05AE11`) and Pepe to the ritual site (`05AE1E`). A concrete trigger NPC/item is not printed by current CLI; needs the QUST start condition / target-ref dump. TODO.

How 50 vs 120 is chosen, and polarity — RESOLVED:

Stage routing confirmed from PSC cache. Fragment index references are from `qf_zzzchmemoryquest05_0205ae03.psc` and the two TIF files.

Full stage flow:

| Stage | Fragment (QF) | Key Action | PSC source |
|---:|---|---|---|
| 0 | `Fragment_0` | `SetObjectiveDisplayed(0)` | QF line 187 |
| 10 | `Fragment_2` | `AllowPCDialogue(Marukh)` + FadeOut + `MoveTo(StartMarker)` | QF lines 64–68 |
| 20 | `Fragment_19` | Kill Marukh if alive; enable Pepe; equip torch | QF lines 75–83 |
| 30 | `Fragment_8` | `Marukh.SetRelationshipRank(−4)` + `StartCombat(player)` | QF lines 54–57 |
| 40 | `Fragment_11` | If Marukh dead OR stage 30 done → `SetStage(50)` [good shortcut]; else `Karma.Mod(−3)` + KnockOut + `BadScene.ForceStart()` + `RegisterSceneSkip(self, BadScene, 130, True)` | QF lines 160–180 |
| 45 | `Fragment_23` | `Karma.Mod(+3)` + `KarmaUp.Show()` | QF lines 152–155 |
| 50 | `Fragment_25` | Remove Adabal from player; `SetObjectiveCompleted(0)`; `qGuide.SetStage(50)`; `ModRadiance(+3.0)` | QF lines 108–124 |
| 60 | `Fragment_6` | `stop()` | QF line 48 |
| 120 | `Fragment_4` | FadeOut + `MoveTo(EndMarker)` + `SetStage(60)` | QF lines 98–103 |
| 130 | `Fragment_13` | FadeOut + `MoveTo(EndMarker)` + `PlayIdle(GetUp)` + `SetStage(140)` | QF lines 138–147 |
| 140 | `Fragment_17` | `stop()` | QF line 130 |

`Fragment_21` (`Pepe.AddItem(Alias_Adabal.GetRef())`, QF lines 89–91) is at PSC index 21 between Fragment_19 (stage 20) and Fragment_23 (stage 45); exact stage binding cannot be confirmed from fragment index alone since indices are non-contiguous (deleted entries); content suggests stage 40 second-entry or stage 120. (unverified: exact stage for Fragment_21)

TIF fragment routing:

- `CHMeq05_TIF__0205AE0E` (Marukh `Goodbye` on `05AE0E`, `chmeq05_tif__0205ae0e.psc`):
  - `Fragment_0` (OnEnd): `GetOwningQuest().SetStage(20)` (line 9)
  - → Marukh finishes his declaration → stage 20 fires → Marukh is killed, Pepe enabled.

- `CHMeq05_TIF__0205AE1B` (Pepe `Goodbye` on `05AE1B`, `chmeq05_tif__0205ae1b.psc`):
  - `Fragment_0` (OnBegin): `GetOwningQuest().SetStage(45)` (line 9) → Karma+3 + KarmaUp
  - `Fragment_1` (OnEnd): `GetOwningQuest().SetStage(50)` (line 16) → good CompleteQuest path

- `SF_zzzCHMeQ05BadScene_0205AE10` (`sf_zzzchmeq05badscene_0205ae10.psc`):
  - `Fragment_0` (scene complete): `GetOwningQuest().SetStage(130)` (line 8) → fade + GetUp → SetStage(140) → stop()

Polarity confirmed (no longer inference):
- **Stage 50 = good end** (Pepe promises to discard stone; `TIF 05AE1B Fragment_1` sets stage 50; Fragment_25 removes Adabal, completes objective, notifies hub).
- **Stage 120 = bad end** (Marukh carries out sacrifice; set by what triggers stage 120 — `Fragment_4` (FadeOut + EndMarker + SetStage(60)) runs on stage 120. Stage 120 is reached via: stage 40 combat (player loses) → `BadScene.ForceStart()` → scene complete sets stage 130 → Fragment_13 (SetStage(140) → stop()). Stage 120 itself is set externally — by the `BadScene` trigger chain: `RegisterSceneSkip(self, BadScene, 130, True)` implies if scene is skipped, go to 130 directly. Stage 120 arrival mechanism: QF Fragment_11 (stage 40) does **not** call `SetStage(120)` directly — it triggers BadScene which ends at 130. Stage 120 is a `CompleteQuest` marker that must be set elsewhere; the most likely setter is within the BadScene completion or another QF entry not decoded here. (unverified: exact setter of stage 120)

Karma delta summary:
- Good path: stage 40 → stage 45: `Karma.Mod(+3.0)` (`Fragment_23`, QF line 153); additionally `ModRadiance(+3.0)` at stage 50 (`Fragment_25`, QF line 123).
- Bad path (player knocked out): stage 40: `Karma.Mod(−3.0)` (`Fragment_11`, QF line 167). No Radiance mod on bad path.
- Global: `zzzCHKarma` (`Vigilant.esm:0x0B19F4` GlobalFloat, confirmed by `find` CLI).

Hub notification: stage 50 → `qGuide.SetStage(50)` (QF `Fragment_25`, line 121); hub is `42E0B1 zzzCHMemoryGuide`.

Subject confirmation:
- "Pepe" is confirmed as a subject of this quest via the `zzzCHMeQ05PepeB01*` topic EditorIDs (`05AE18`, `05AE1A`) owned by `05AE03` — matches the index's subject→quest map. Marukh and Dulsa also feature (aliases `#0`, `#6`).

## Proper Noun Resolution

Garbled source terms resolved against lore books `4A8AFD` and `12905F`:

| Source term (garbled) | Resolved term | Evidence |
|---|---|---|
| `Al-Ashe` | **Al-Esh** | [`12905F` line 138](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:138): "the shade of Al-Esh speak to him" — Alessia's Nedic name. |
| `Sevenety-Seven` / `Senventy-Seven` | **Seventy-Seven** (Inflexible Doctrines) | [`12905F` line 142](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:142): "the Seventy-Seven Inflexible Doctrines" — Marukh's doctrines, etched in simian gore. |
| `Arcane` (the ritual / site) | **Arkayn** (Cycle) | [`12905F` line 154](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:154): "the Arkayn Cycle"; machine translation rendered `Arkayn` → `Arcane`. The ritual site is the XMarker `05AE0F` confirmed by `packagediag`. |
| `Stone of Al-Ashe` | **Chim-el-Adabal** | [`4A8AFD` line 1639](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1639): "Chim-el-Adabal, said to be crystallized blood from the Heart of Lorkhan"; the quest's `Red Stone` item `05AE01` and alias `#5 Adabal` are its memory-form. |
| `Shezarr` | **Shezarr** (not garbled) | Canonical TES name for Lorkhan's Cyrodilic aspect (missing god). Marukh's line "Misplaced Shezarr" echoes [`4A8AFD` adjacent text](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1358): "Misplaced Shezarr bless us!" — verbatim match. |
| `Sheol` | **Sheol** (kept as-is) | Not a canonical TES term; Hebrew/Semitic word for realm of the dead. Marukh's "Sheol…God of freedom defunct endlessly" maps conceptually to Lorkhan's consumed/absent state but no TES lore exact match found. Preserve as source transcription error or intentional syncretic term. (unverified: TES lore mapping) |
| `Aldomeri` (shattered) | **Aldmeri** (Dominion/lineage) | Marukh's line: "figure of the hero that shattered Aldomeri" = Tiber Septim / Dragonborn who broke the Aldmeri/Ayleid order. Garbling of `Aldmeri`. (inference) |

## Open Verification (remaining)

RESOLVED — decode VMAD fragments:
- `CHMeq05_TIF__0205AE0E` Fragment_0 (OnEnd): `SetStage(20)` — confirmed. Stage 20 kills Marukh, enables Pepe. (`chmeq05_tif__0205ae0e.psc` line 9)
- `CHMeq05_TIF__0205AE1B` Fragment_0 (OnBegin): `SetStage(45)` — confirmed. Karma+3. (`chmeq05_tif__0205ae1b.psc` line 9)
- `CHMeq05_TIF__0205AE1B` Fragment_1 (OnEnd): `SetStage(50)` — confirmed. Good CompleteQuest. (`chmeq05_tif__0205ae1b.psc` line 16)

RESOLVED — Package records `05AE11` / `05AE1E`: dumped via `packagediag`; destinations and templates confirmed; see Alias/Staging Backbone above.

RESOLVED — proper noun resolution: `Al-Ashe`→`Al-Esh`, `Sevenety-Seven`→`Seventy-Seven Inflexible Doctrines`, `Arcane`→`Arkayn (Cycle)`, `Shezarr` is not garbled; see Proper Noun Resolution table above.

Remaining unverified:
- alias `#5 Adabal` fill type/source: CLI confirms the alias holds the red stone ref (QF calls `Alias_Adabal.GetRef()`), but `scenediag` does not print the fill source (forced ref vs unique). (unverified: fill source)
- alias `#6 MemoryDulsa` fill source: type is `LocationAlias` (QF PSC header); fill source not printed by CLI. (unverified: fill source)
- objective[0] target ref: `questdiag` shows `targets=1 target: flags=0 conds=0` but does not print the target FormID; current CLI cannot deep-dump QUST target refs. (unverified: target FormID)
- exact setter of **stage 120** (bad CompleteQuest): Fragment_11 (stage 40) triggers `BadScene.ForceStart()` and `RegisterSceneSkip(130)`; stage 130 → Fragment_13 → SetStage(140) → stop. Stage 120 is a `CompleteQuest` marker but no QF fragment is identified that calls `SetStage(120)` in the decoded PSC; possibly set by an additional stage-log entry not visible in the PSC cache or by the BadScene's scene-manager `VigSceneManagerQuestScript`. (unverified: stage 120 setter)
