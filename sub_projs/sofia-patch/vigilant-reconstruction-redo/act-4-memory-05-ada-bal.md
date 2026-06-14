# Act 4 Memory 05 - Ada Bal

Status: redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text, not scene phases/actions.
- This mod is machine-translated from Japanese; garbled English is kept verbatim in the source column with a `Note:` "待驗證" instead of being smoothed over.

## Quest Record

[`05AE03 zzzCHMemoryQuest05 "Ada Bal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)

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
| 0 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:358) | 在月下，於亡者之上起舞。 |

Objective targets:
- 1 target in ESM (`questdiag`: `objective[0] ... targets=1`, `target: flags=0 conds=0`).
- Current CLI output does not print the target ref; this needs a deeper QUST target dump if the target location matters.

## Alias / Staging Backbone

The host quest's aliases are dumped by `scenediag` on the one `SCEN` it owns.

Host quest:
- [`05AE03 zzzCHMemoryQuest05`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)

Host-quest aliases from `scenediag` (8):

| Alias | Name | Fill |
|---:|---|---|
| 0 | `Marukh` | uniqueActor [`05ADEF zzzCHMarukhMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1046) |
| 1 | `Pepe` | uniqueActor [`05ADFD zzzCHInquisitorPepeMemory2`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1044) |
| 2 | `StartMarker` | forcedRef `05ADFE:Vigilant.esm` |
| 3 | `EndMarker` | forcedRef `05AE04:Vigilant.esm` |
| 4 | `Player` | forcedRef `000014:Skyrim.esm` |
| 5 | `Adabal` | not printed (no fill in CLI output) |
| 6 | `MemoryDulsa` | not printed (no fill in CLI output) |
| 7 | `GuideMarker` | forcedRef `42E0B4:Vigilant.esm` |

Inference:
- `Marukh` alias `#0` and `Pepe` alias `#1` are the two dialogue aliases used by the two custom branches: the Marukh branch INFOs require `GetIsAliasRef alias #0`, the Pepe branch INFOs require `GetIsAliasRef alias #1` (confirmed by `infodiag`).
- `Adabal` alias `#5` is the red-stone object the memory revolves around (see [`05AE01 zzzCHAdabalMemory "Red Stone"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1004) in Related Records). Its fill is not printed by the CLI. (inference)
- `MemoryDulsa` alias `#6` is Dulsa, the woman Marukh addresses in his branch; its fill is not printed by the CLI. (inference)
- The `42E0B4` `GuideMarker` ties this memory to the [`42E0B1 zzzCHMemoryGuide` hub](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:309). (inference)

Travel packages owned by the quest (from `find`):
- [`05AE11 zzzCHMeq05MarukhTravelToPlayer`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357) — Marukh walks to the player. (Package record; not dumped here.)
- [`05AE1E zzzCHMeq05PepeTravelToArcane`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357) — Pepe walks to the "Arcane" (the stone ritual site). (Package record; not dumped here.)
- Note: `Arcane` is the mod's recurring term for the Al-Ashe stone ritual (see branch text below); kept as source phrase, 待驗證.

Dialog views owned by the quest (from `find`, not dumped):
- `05AE07 zzzCHMeQ05MarukhView`, `05AE16 zzzCHMeQ05PepeView`.

## Scene Records

This memory owns exactly **one** `SCEN` record. Its text lines are linked to `dialogue.md`; phases/actions are from `scenediag`.

### 05AE10 zzzCHMeQ05BadScene

CLI:
- `scenediag Vigilant.esm 0x05AE10`

Staging:
- Host quest: [`05AE03 zzzCHMemoryQuest05`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)
- Flags: none
- Actor: alias `#0` (`Marukh`), behaviorFlags `DeathEnd`, flags `NoPlayerActivation`
- Phases: 3, each with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Package`, actor `#0`, phase 0→0, no topic.
  - index 2: `Package`, actor `#0`, phase 1→2, no topic.
  - index 3: `Dialog`, actor `#0`, phase 1, topic [`05AE12`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:736), flags `FaceTarget, HeadtrackPlayer`, emotion `Neutral`, loop 1–10.
  - index 4: `Dialog`, actor `#0`, phase 2, topic [`05AE14`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:739), flags `FaceTarget, HeadtrackPlayer`, emotion `Neutral`, loop 1–10.

Scene-owned topics (both `SNAM=SCEN`, 0 conditions, spoken by alias `#0` Marukh):

Translations:
- [`05AE12` / INFO `05AE13`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:736): 「Ada Bal。那是奇蹟，也是皇帝。是比……任何東西都更能滿足人民飢渴之物。」
  - Note: source `"Ada Bal. Is a miracle, it is also the emperor. And something to satisfy the hunger of the people than ... anything"` is garbled; 待驗證。
- [`05AE14` / INFO `05AE15`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:739): 「Dulsa，原諒我。我那無名的孩子，原諒我……」
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
| [`05AE09 zzzCHMeQ05MarukhB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:724) | `05AE0A` | none | `GetStage == 10`; `GetIsAliasRef alias #0` | 「七十七……龍裔……Sheol。自由之神無盡地消亡，其軌跡……Shezarr」 Note: source `"Senventy-Seven...dragonborn...Sheol. God of freedom defunct endlessly, its trajectory...Shezarr"` 高度破碎，待驗證。 |
| [`05AE0B zzzCHMeQ05MarukhB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:727) | `05AE0C` | none | `GetIsAliasRef alias #0` | Prompt: 「你在做什麼，Marukh？」 Response 1: 「看這塊石頭……Dulsa。這是七十七的奧祕，很快就會完成。Al-Ashe 之石就在此刻被重現。」 Response 2: 「Dulsa，你是被選中的。為了愛。Al-Ashe 如此說。為了完成 Arcane，我需要你、以及你腹中孩子的血。」 Note: `Al-Ashe`／`Arcane` 為原文專名，待驗證。 |
| [`05AE0D zzzCHMeQ05MarukhB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:731) | `05AE0E` | `Goodbye` | `GetIsAliasRef alias #0`; VMAD `CHMeq05_TIF__0205AE0E.Fragment_0` on end | Prompt: 「這是瘋狂……」 Response 1: 「也許並非瘋狂，理智亦在其中。Al-Ashe 之言即真理。讓那些事物在血中閃耀於此石、並奠立高塔，就是我們的使命。」 Response 2: 「石頭向我顯示了：那位粉碎了 Aldmeri、平定了大陸、將劍刺入巨蛇的英雄之身影。」 Response 3: 「未知之人於昨日或明日知曉一事。但只要那一天哪怕近了一日，我都願意犧牲你與我的孩子。」 Note: source 多處破碎（`shattered Aldomeri`、`thrust a sword into a snake`、`The unexpected know a thing`），待驗證。 |

## Custom Dialogue Branch: Pepe

Branch:
- `05AE17:Vigilant.esm` (`zzzCHMeQ05PepeB01`)
- View: `05AE16 zzzCHMeQ05PepeView`

Speaker condition pattern:
- Every INFO requires `GetIsAliasRef == 1` on alias `#1` (`Pepe`).
- Opening line also requires `GetStage == 40` on quest `05AE03`.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`05AE18 zzzCHMeQ05PepeB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:742) | `05AE19` | none | `GetStage == 40`; `GetIsAliasRef alias #1` | 「那 oblivion 究竟是什麼……」 Note: source `"What is oblivion that..."` 為截斷句，待驗證。 |
| [`05AE1A zzzCHMeQ05PepeB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:745) | `05AE1B` | `Goodbye`, `SayOnce` | `GetIsAliasRef alias #1`; VMAD `CHMeq05_TIF__0205AE1B` `Fragment_0` on begin + `Fragment_1` on end | Prompt: 「請捨棄這塊石頭。在無人之手能不期然觸及之處……」 Response (Puzzled): 「好……我明白了。我以 Mara 之名起誓。」 Note: prompt source `"Please discard this stone. Where anyone hands reach unexpected ..."` 為截斷／破碎句，待驗證。 |

## Related Records

These are cross-linked context. NPC/item ownership by quest `05AE03` is only the two memory actors filled into aliases `#0`/`#1`; the rest are narrative cross-references.

NPCs:
- [`05ADEF zzzCHMarukhMemory` - Marukh](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1046) — alias `#0`.
- [`05ADFD zzzCHInquisitorPepeMemory2`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1044) — alias `#1`.
- [`12BF48 zzzCHInquisitorPepeMemory` - Inquisitor Pepe](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:558) — other Pepe-memory variant.
- [`081E46 zzzCHInquisitorPepe` - Inquisitor Pepe](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1065) — the living/present-day Pepe.

Items:
- [`05AE01 zzzCHAdabalMemory` - `Red Stone`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1004) — the red stone of this memory; FormID sits in the `05AE0x` block with the quest, strongly tied to alias `#5` `Adabal`. (inference)
- [`1353DF zzzCHAdabal` - `Adabal`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:976) — the present-day Adabal item.
- [`108EB1 zzzCHSkinPepe`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:727)
- [`500DDC zzVcgPepeMask` - `Mask of Pelan`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:416) — Note: `Pelan` likely a localized spelling of `Pelin`/`Pepe`; 待驗證。

Locations (Adabal Court — the Pepe-cult site referenced by the Pepe travel package, inference):
- [`26C05F zzzCHLocAdabalCourt` - `Adabal Court`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:550)
- [`21AFA1 zzzCHCourtAdabalFirst` - `First Court`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:131)
- [`21AEA7 zzzCHCourtAdabalSecond` - `Second Court`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:144)
- [`0E0889 zzzCHSummaryCourt02` - `Fountain Garden of Dibella`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:140) — Pepe-priest's followers' hidden garden, per Gregory's notes. (inference)

Books (lore context — none is owned by quest `05AE03`; `booktext` fails on both, source text is the extracted `game-data`):
- [`4A8AFD zzzCHBookESO09 "Aurbic Enigma 4: The Elden Tree"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1619) — defines `Chim-el-Adabal`, the Founding-Stone, the Staff of Towers, and the **Dance** of the tower segments; the canonical source the quest title "Ada Bal" and objective "Dance on the dead" allude to. (inference)
- [`12905F zzzCHBookESOIllsuionDeath "The Illusion of Death"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:131) — Marukh / `Seventy-Seven Inflexible Doctrines` / `Al-Esh` context (shared with Memory 07).

## Related Book Translation

No `BOOK` record is owned by quest `05AE03`, so this section gives the **lore anchor** the title/objective derive from rather than an in-quest book.

[`4A8AFD zzzCHBookESO09 "Aurbic Enigma 4: The Elden Tree"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1619)

CLI:
- `booktext Vigilant.esm 0x4A8AFD`
- Result: failed with `could not extract English strings`; source therefore uses the already extracted `game-data` text.

Source-grounded link points (vanilla "Aurbic Enigma 4" lore text, reproduced verbatim in this mod's book):
- [`Chim-el-Adabal`, the great red diamond, "crystallized blood from the Heart of Lorkhan"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1639) — the red stone; the quest's `Adabal` / `Red Stone` item (`05AE01`) is its memory-form. (inference)
- [the eightfold Staff of Towers, "each segment a semblance of a tower in its Dance"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1643) — the "Dance" of the objective "Under the moon, Dance on the dead." (inference)

Translation note:
- The mod's quest title `Ada Bal` and Marukh's `Stone of Al-Ashe` / `Arcane` are the machine-translated reflections of `Adabal` / `Al-Esh` / the Ayleid ritual. The garbling (`Ada Bal`, `Al-Ashe`, `Arcane`, `Sevenety-Seven`) is preserved in the branch tables above with `Note:` 待驗證 rather than silently corrected.

## Reconstruction Notes

Source-grounded:
- This memory is [`05AE03 zzzCHMemoryQuest05 "Ada Bal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357) with objective [`Under the moon, Dance on the dead.`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:358).
- It owns exactly **one** `SCEN`: `05AE10 zzzCHMeQ05BadScene`, a 3-phase Marukh (alias `#0`) cutscene with two scene-only topics (`05AE12`, `05AE14`) and `DeathEnd`.
- It owns **two** custom dialogue branches:
  - Marukh branch `05AE08` (alias `#0`), opener stage-gated at `GetStage == 10`; 3 topics; ends on `05AE0E` with a VMAD `Goodbye` fragment.
  - Pepe branch `05AE17` (alias `#1`), opener stage-gated at `GetStage == 40`; 2 topics; ends on `05AE1B` with a `Goodbye, SayOnce` VMAD (begin+end fragments).
- Two `CompleteQuest` stages: **50** and **120**.

Trigger (inference):
- The hub `42E0B1 zzzCHMemoryGuide` (via `GuideMarker` alias `#7`, `42E0B4`) starts the memory; the player enters and the two travel packages move Marukh to the player (`05AE11`) and Pepe to the ritual site (`05AE1E`). A concrete trigger NPC/item is not printed by current CLI; needs the QUST start condition / target-ref dump. TODO.

How 50 vs 120 is chosen, and polarity (inference, source-grounded shape):
- Two `CompleteQuest` stages = the index's recurring two-band good/bad (karma) memory signature.
- The branch openers gate on stage: Marukh branch needs `GetStage == 10` (early), Pepe branch needs `GetStage == 40` (later). The player therefore can hear Marukh's justification first, then later reach Pepe.
- **Polarity is resolvable from the ESM here** (unlike MeQ07, which the index left as "two outcomes exist"):
  - The Pepe branch terminal line `05AE1B` is the player begging Pepe to **discard the stone**, and Pepe swears **"I promise by Mara"** — a mercy / abort-the-ritual outcome. → **good**.
  - The Marukh branch terminal line `05AE0E` is Marukh declaring he **"is willing to sacrifice you and my child"** to complete the stone; the only owned scene is literally `zzzCHMeQ05BadScene` and plays the "Dulsa, forgive me, my Nameless Child" sacrifice (`05AE14`, `DeathEnd`). → **bad**.
  - Mapping to stages (inference, to confirm via fragments): **stage 50 = the Pepe / good (ritual averted) completion**, **stage 120 = the Marukh / bad (sacrifice carried out, BadScene plays) completion**. The fragment scripts `CHMeq05_TIF__0205AE0E` (Marukh end) and `CHMeq05_TIF__0205AE1B` (Pepe begin+end) are the most likely setters of these stages; decode them to confirm the exact stage each sets.

Subject confirmation:
- "Pepe" is confirmed as a subject of this quest via the `zzzCHMeQ05PepeB01*` topic EditorIDs (`05AE18`, `05AE1A`) owned by `05AE03` — matches the index's subject→quest map. Marukh and Dulsa also feature (aliases `#0`, `#6`).

Open verification:
- decode VMAD fragments `CHMeq05_TIF__0205AE0E` (Marukh Goodbye) and `CHMeq05_TIF__0205AE1B` (Pepe begin+end) to confirm which sets stage 50 vs 120 and any item/global granted;
- dump QUST aliases `#5 Adabal` and `#6 MemoryDulsa` fill (not printed by `scenediag`) and the objective[0] target ref;
- dump the two `Package` records `05AE11` / `05AE1E` if travel staging matters;
- confirm `05AE01 zzzCHAdabalMemory "Red Stone"` is the object filled into alias `#5` and whether the good ending removes/keeps it;
- resolve the garbled proper nouns `Al-Ashe` (= `Al-Esh`?), `Arcane`, `Sheol`, `Shezarr`, `Sevenety-Seven` against the lore book `4A8AFD` and Memory 07's `12905F`.
