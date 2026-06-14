# Act 4 Memory 01 - The Grand Inquisitor

Status: source-grounded, link-first redo slice. Not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- This memory's English is heavily machine-translated from Japanese; many lines are garbled. Garbled source phrases are kept verbatim with a `Note:` 待驗證 flag rather than smoothed over in zh-TW.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text, not scene phases/actions.

## Quest Record

[`12C4F4 zzzCHMemoryQuest01 "The Grand Inquisitor"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:140)

CLI:
- `questdiag Vigilant.esm 0x12C4F4`
- `infodiag Vigilant.esm 0x12C4F4`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x12C4F4`
- EditorID: `zzzCHMemoryQuest01`
- Name: `The Grand Inquisitor`
- Flags: `RunOnce`
- Priority: `90`
- Type: `Misc`
- Filter: `CH\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 1 | none | empty |
| 10 | none | empty |
| 20 | CompleteQuest | empty |
| 30 | none | empty |
| 40 | none | empty |
| 100 | CompleteQuest | empty |
| 110 | none | empty |
| 120 | none | empty |
| 999 | ShutDownStage | empty |

Objective:

| Index | Source | Translation |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:141) | 未獲回應的夢，在沉默中沉沒。 |

- Note: source objective `Unanswered Dream sink in silence.` is itself ungrammatical (subject/verb mismatch); translated literally. 待驗證.

Objective targets:
- 1 target in ESM (`questdiag`: `objective[0] ... targets=1`).
- Target has 0 conditions.
- Current CLI output does not print the target ref; this needs a deeper QUST target dump if the target location matters.

## Alias / Staging Backbone

The two `SCEN` records below share the same host quest and aliases.

Host quest:
- [`12C4F4 zzzCHMemoryQuest01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:140)

Host-quest aliases from `scenediag`:

| Alias | Name | Fill |
|---:|---|---|
| 0 | `Mara` | uniqueActor [`0F9649 zzzCHBossShoggothMother "Mary the Dark Virgin"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:262) |
| 2 | `Inquisitor` | uniqueActor [`12BF48 zzzCHInquisitorPepeMemory "Inquisitor Pepe"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:558) |
| 3 | `Molag` | not printed by CLI (no static fill) |
| 4 | `Door` | forcedRef `12BED7:Vigilant.esm` |
| 5 | `TravelMarker` | forcedRef `12BF4C:Vigilant.esm` |
| 6 | `MaraMemory` | not printed by CLI (no static fill) |

Inference:
- The **subject / speaker** of this memory is alias `#2` `Inquisitor` = `Inquisitor Pepe`. Every custom branch INFO is gated on `GetIsAliasRef == 1` for alias `#2`, so the Grand Inquisitor monologue is his.
- The addressee throughout the dialogue is "Mara" — alias `#0`, statically filled by `0F9649` ("Mary the Dark Virgin"). (inference) "Mara" here is the figure the Inquisitor interrogates; this is the in-memory player-stand-in / accused "witch", not the goddess.
- Alias `#3` `Molag` has no static fill and is presumed filled at runtime; it is a second scene actor in Scene02 (see below). (inference)
- This is the **Dostoevsky "Grand Inquisitor" scene** reframed in Alessian terms: the Inquisitor accuses "Mara" of being Mara/the saviour, threatens to burn her as a witch tomorrow, and justifies the Alessian Order's tower, stone, mystery, and miracle. (inference, from the branch text below)

## Scene Records

Scene records are not present as full records in `game-data`; the one scene topic line is linked to `dialogue.md`, while phases/actions are from `scenediag`. Both scenes drive their actors purely via `Package` actions (no per-phase `Dialog` topic), except Scene02's final action.

### 12DBA7 zzzCHMeQ01Scene01

CLI:
- `scenediag Vigilant.esm 0x12DBA7`

Staging:
- Host quest: [`12C4F4 zzzCHMemoryQuest01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:140)
- Flags: none
- Actor: alias `#2` (`Inquisitor`), behaviorFlags 0
- Phases: 3, each with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Package`, actor `#2`, phase 0.
  - index 2: `Package`, actor `#2`, phase 1.
  - index 3: `Package`, actor `#2`, phase 2.
- No `Dialog` action; this scene only walks/positions the Inquisitor via packages. (inference) The interrogation lines play through the custom branch (below), not as scene-embedded `Dialog` actions.

### 12DBAD zzzCHMeQ01Scene02

CLI:
- `scenediag Vigilant.esm 0x12DBAD`

Staging:
- Host quest: [`12C4F4 zzzCHMemoryQuest01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:140)
- Flags: none
- Actors: alias `#2` (`Inquisitor`) and alias `#3` (`Molag`), both `NoPlayerActivation`.
- Phases: 3, each with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Package`, actor `#2`, phase 0.
  - index 2: `Package`, actor `#2`, phases 1-2.
  - index 3: `Package`, actor `#3`, phase 1.
  - index 4: `Package`, actor `#3`, phase 2.
  - index 5: `Dialog`, actor `#3` (`Molag`), phase 2, flags `HeadtrackPlayer`, topic [`12DBB0`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1467), emotion `Neutral`, loop 1-10.

Scene-owned topic (`SCEN` category, owned by quest, played in Scene02 action 5):
- [`12DBB0` / INFO `12DBB1`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1467) (Happy): 「Mara，是不同的，我為這樣的結局致意。實在很可惜。」
  - Note: source `Mara, is differents and I greet the consequences of such. It is a pity that very` is badly garbled; translation is best-effort. 待驗證.
  - Inference: spoken by alias `#3` `Molag` (the `Dialog` action assigns ActorID=3). This is the only spoken scene line; the Inquisitor's whole monologue lives in the custom branch instead.

## Custom Dialogue Branch: Inquisitor Pepe

Branch:
- `12CA9F:Vigilant.esm` (per `infodiag` `branch=12CA9F` on every INFO below)

Speaker condition pattern:
- **Every** INFO requires `GetIsAliasRef == 1` on alias `#2` (`Inquisitor`).
- No `GetStage` gate appears on these INFOs (unlike MeQ07). The branch is one long single-speaker monologue, ordered by topic, not split into two stage-gated openers.
- Topic EditorIDs use the prefix `zzzCHMeQPepeB01T*` (Pepe = the Inquisitor's name), not `zzzCHMeQ01*`.
- The whole branch is the Inquisitor monologue; the player only feeds it the prompts (`It...`, `......(Silence)`, `......(Stare)`).

| Topic | INFO | Prio | Flags | Conditions | Translation |
|---|---|---:|---|---|---|
| [`12CAA0 zzzCHMeQPepeB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1407) | `12CAA1` | 90 | `SayOnce`, `WalkAway` | `GetIsAliasRef alias #2` | (Fear)「你究竟是不是 Mara……Mara？」 (Puzzled)「你來到這裡的諷刺……我們竟想以 Alessia 的樣貌、甚至以更多的樣貌現身？」 Note: 兩句皆 garbled，待驗證。 |
| [`12CAA2 zzzCHMeQPepeB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1411) | `12CAA3` | 45 | `Goodbye`, `SayOnce` | `GetIsAliasRef alias #2`; VMAD `CHMeq1_TIF__0212CAA3.Fragment_0` on end | Prompt:「It...」(Anger)「女巫，閉嘴……閉嘴，就算群眾是愚人，愚人也不會把老鷹當成老鷹……」(Anger)「明天早上，你會被綁在火刑柱上燒死。你冒充聖 Alessia，要以女巫之名付之一炬。」(Anger)「你，但這種事我當然知道！」 Note: garbled，待驗證。 |
| [`12D04A zzzCHMeQPepeB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1416) | `12D04B` | 55 | `WalkAway` | `GetIsAliasRef alias #2` | Prompt:「......(Silence)」(Neutral)「很好，我也保持沉默。因為反正你也沒有那樣的權利。」(Neutral)「為什麼，你要在我們此刻於世上成就大業之時來礙事？你不知道明天會是身、還是別的嗎？」(Neutral)「我們知道你是什麼。但那種事無關緊要。無論如何，明天我們把你當女巫燒掉。」(Neutral)「明天，今天親吻你雙足的那些人，會往火裡丟柴薪——這是我的一點暗示。」 Note: 多句 garbled，待驗證。 |
| [`12D04C zzzCHMeQPepeB01T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1422) | `12D04D` (INFO[0]) | 55 | `SayOnce`, `WalkAway` | `GetIsAliasRef alias #2` | Prompt:「......(Silence)」(Neutral)「看這塊石頭。你能把這石頭……？這種事當然不會太遠……」(Neutral)「『人活著不是單靠食物』——這就是我給你的回答。」(Neutral)「如同 Shezarr 從前的造物，Deidre 曾以麵包之名反叛它，對你而言也是。」(Happy)「結果，你們大概不知緣由——那 Deidre 之後成群湧出、走向公開的身影。」 Note: garbled，待驗證。 |
| (cont.) | `12DBA6` (INFO[1]) | 55 | `WalkAway` | `GetIsAliasRef alias #2` | (Neutral)「總之，人不過是即將到來的飢餓。而那些在麵包之後高喊善行的人，毀掉了你的塔。」(Neutral)「你們必定要建一座新塔。但那是徒勞。連命運之塔的地基都建不成。」(Neutral)「若你不打算建塔，或許能稍稍緩解人們的痛苦。但你沒有。」(Anger)「人們怎麼做？他們來到我們、Alessia 教團這裡。那些曾允諾要偷走 Shezarr 之心的人在說謊！！」 Note: garbled，待驗證。同一 topic 的第二則 INFO。 |
| [`12D04E zzzCHMeQPepeB01T05`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1432) | `12D04F` | 55 | `WalkAway` | `GetIsAliasRef alias #2` | Prompt:「......(Silence)」(Neutral)「如果你是 Mara，就敢吞下這塊石頭。火焰平息，一切可憎的喜劇都將慶祝終結。」(Neutral)「但你不會吞。我把它收起，因為他一否認奇蹟，也就否認了 Edora。」(Neutral)「人寧可相信奇蹟、勝於相信 Edora。在自己身上造出奇蹟，就會去相信像我這樣的審判官。」(Neutral)「重要的是不要把人變成奇蹟的奴隸——那才是自由的信仰，你，我本以為你也會這樣想。」(Sad)「並非真的深愛人們。你們太深地愛了什麼樣的人們啊。那樣人就不會挨餓了。」 Note: garbled，待驗證。 |
| [`12D050 zzzCHMeQPepeB01T06`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1439) | `12D051` | 55 | `WalkAway` | `GetIsAliasRef alias #2` | Prompt:「......(Silence)」(Neutral)「重要的既不是你的愛，也不是自由。而是奧秘——這塊石頭。所有人都必須違背良心向它屈服。」(Neutral)「我們在街上有 Alessia 教團。那……卻終究改寫了你大大的一切。」(Neutral)「然後在教會、權柄、奧秘與奇蹟之上，我建起這座塔。把無盡的人從『自由』的痛苦中解放。」(Neutral)「如此一來，若我們得到 Alessia 教團的寬恕——他們顧念弱者，甚至容忍惡行。」(Happy)「這不正是你愛人類的證據嗎？」 Note: garbled，待驗證。 |
| [`12D052 zzzCHMeQPepeB01T07`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1446) | `12D053` | 55 | `WalkAway` | `GetIsAliasRef alias #2` | Prompt:「......(Silence)」(Neutral)「再說一次，但你究竟為何……此刻來礙我們成就大業？」(Neutral)「該屬於 Edora 的歸 Edora。對皇帝，皇帝該說的話——你們把這石頭從我們這裡奪走了嗎？」(Neutral)「這石頭在大地上的力量。我們緊貼著、持續握有這石頭。捨棄你們，我得去崇拜那低賤奴隸的女王。」(Neutral)「那是從那時起，我也早在兩千年前，Imuga 的先知在 Colovia 的叢林裡找到了這石頭。」(Happy)「塔我們還未臻完美。但它遲早會完成。黎明時所有人都會幸福至極。」 Note: garbled，待驗證；`Imuga`/`Colovia` 為專名待驗證。 |
| [`12D054 zzzCHMeQPepeB01T08`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1453) | `12D055` | 55 | `WalkAway` | `GetIsAliasRef alias #2` | Prompt:「......(Silence)」(Neutral)「你為何拒絕這塊石頭？若你懷著這石頭的一點希望……若不在 Mundus 的至福之境？」(Neutral)「本該由那一個人——該背負良心與統治者之責、所有人之責的人——來承擔。」(Neutral)「你做不到，但我們 Alessia 教團能。我們要靠這石頭在大陸上建起一個大帝國。」(Neutral)「只要有這石頭，我們這帝國，甚至不必等待 Shezarr 的歸來。」 Note: garbled，待驗證；`Mundasu` 推為 `Mundus`，待驗證。 |
| [`12D056 zzzCHMeQPepeB01T09`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1459) | `12D057` | 55 | `WalkAway` | `GetIsAliasRef alias #2` | Prompt:「......(Silence)」(Neutral)「既然我所說的都已實行，大帝國就會被建起。一而再，你明天會看見那可悲而馴服的羊群。」(Neutral)「只要打個手勢，他們就會心甘情願為你搬柴。你知道為什麼嗎？因為你來礙了我們的事。」(Sad)「若說有誰配在 Mundus 被焚，那肯定就是你。明天我們把你燒掉。到此為止！！」 Note: garbled，待驗證。 |
| [`12D058 zzzCHMeQPepeB01T10`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1464) | `12D059` | 55 | `Goodbye`, `SayOnce` | `GetIsAliasRef alias #2`; VMAD `CHMeq1_TIF__0212D059.Fragment_0` on end | Prompt:「......(Stare)」(Fear)「……即便如此又如何……別再出現了，快走，滾出去。」 Note: garbled，待驗證。 |

Translation notes:
- The entire branch is machine-translated from Japanese; sentence boundaries, pronouns ("you/your guys"), and proper nouns are unreliable. Every cell above carries 待驗證.
- Recurring proper nouns to verify: `Shezarr` (source `Shezaru`), `Deidre` (likely `Daedra`? — 待驗證), `Edora` (待驗證), `Imuga` (cf. `Imga`/`Imga monk` in MeQ07 — 待驗證), `Colovia`, `Mundus` (source `Mundasu`), `Alessia order/meeting/Association` (all the same `Alessia 教團`, source inconsistent).
- `T02` prompt source `"It..."`; `T03`–`T09` prompt source `"......(Silence)"`; `T10` prompt source `"......(Stare)"`. These are the player's only inputs — silence — fitting the objective "sink in silence".

## Related Records

These are referenced by this quest's aliases or by the Inquisitor's monologue and should be cross-linked in a full reconstruction. Aliases confirmed via `scenediag`; NPC names via `npcs.tsv`.

NPCs:
- [`12BF48 zzzCHInquisitorPepeMemory "Inquisitor Pepe"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:558) — the Grand Inquisitor / speaker, alias `#2`.
- [`0F9649 zzzCHBossShoggothMother "Mary the Dark Virgin"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:262) — fills alias `#0 Mara`, the interrogated "Mara".
- Inquisitor Pepe also appears elsewhere: [`081E46 zzzCHInquisitorPepe "Inquisitor Pepe"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1065), [`1363DC zzzCHInquisitorPepeGhost`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:546), [`05ADFD zzzCHInquisitorPepeMemory2`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1044), [`06A230 zzzCHInquisitorPepeMemory3`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1069). (cross-quest — not owned by `12C4F4`; Pepe is the central figure of the Pepe/Ada-Bal memory cluster MeQ05/MeQ06 per the index.)

Refs (forcedRef aliases, not in `npcs.tsv`):
- `12BED7:Vigilant.esm` — alias `#4 Door`.
- `12BF4C:Vigilant.esm` — alias `#5 TravelMarker`.

Packages (from `find zzzCHMeQ01`) — drive the scene actors:
- `12DBA8 zzzCHMeQ01PepeTravel01`
- `12DBA9 zzzCHMeQ01PepeFrocGreet`
- `12DBAF zzzCHMeQ01PepeGetOut`
- `12DBB2 zzzCHMeQ01PepeStandbyPrison`
- Inference: package names (`Travel`, `Greet`, `GetOut`, `StandbyPrison`) match the prison-interrogation staging — the Inquisitor stands by in the prison, greets, monologues, then orders "get out" (matching `T10` "get out").

Books:
- No book is owned by this quest. The Pepe/Mara/Alessian-Order theme recurs in several Vigilant in-game notes (e.g. [books.md mentions of Pepe priest + Mary statue](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:9), [Mara burning narrative](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1274)), but those are not `12C4F4` records; cross-link only, do not attribute. (inference)

## Reconstruction Notes

Source-grounded:
- This memory is represented by [`12C4F4 zzzCHMemoryQuest01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:140) with objective [`Unanswered Dream sink in silence.`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:141).
- It contains **two `SCEN` records** (`12DBA7 Scene01`, `12DBAD Scene02`), both staged with `Package` actions; only Scene02 has a single `Dialog` action (alias `#3 Molag`, topic `12DBB0`).
- It contains **one custom dialogue branch** (`12CA9F`), a 10-topic / 11-INFO single-speaker monologue by `Inquisitor` (alias `#2` = Inquisitor Pepe), all gated on `GetIsAliasRef alias #2` with **no `GetStage` gate**.
- The subject/speaker is **Inquisitor Pepe**; the addressee is **"Mara"** (alias `#0`). The objective "sink in silence" matches the player's prompts being silence/stare only.
- VMAD fragments exist on the two `Goodbye/SayOnce` choices (`12CAA3` → `CHMeq1_TIF__0212CAA3.Fragment_0`; `12D059` → `CHMeq1_TIF__0212D059.Fragment_0`), indicating these likely advance quest state / route the completion. Exact Papyrus behavior is not decoded here.

How 20 vs 100 is chosen (branch-stage analysis):
- `questdiag` shows two `CompleteQuest` stages: **20** and **100** — the index's recurring two-band karma signature.
- **Unlike MeQ07, the branch INFOs here carry NO `GetStage` gate**, so the 20-vs-100 routing is NOT visible as two stage-gated dialogue openers. It must instead be set by the **VMAD fragments** on the two `Goodbye` exits (`12CAA3` = the early angry "burn you tomorrow" exit at `T02`, prio 45; `12D059` = the final "get out" exit at `T10`, prio 55). (inference)
  - `12CAA3` (`T02`, `Goodbye/SayOnce`, prio 45) is the **early walk-away**: the player breaks silence ("It...") and the Inquisitor cuts it off → plausibly routes the **early completion (stage 20)**. (inference)
  - `12D059` (`T10`, `Goodbye/SayOnce`, prio 55) is the **end-of-monologue exit** after the player stays silent through the whole speech → plausibly routes the **late completion (stage 100)**. (inference)
- **Good/bad polarity: UNRESOLVED from condition data.** There is no `GetStage` or karma-global condition on these INFOs to read polarity off. Decoding the two TIF fragment scripts (which stage each calls `SetStage` with) is required to assign which completion is the "endure-in-silence" (likely good) vs the "break/answer" (likely bad) outcome. TODO.

Open verification:
- decompile / inspect the fragment scripts `CHMeq1_TIF__0212CAA3` and `CHMeq1_TIF__0212D059` to confirm which `SetStage` (20 vs 100) each calls — this resolves both the 20/100 routing AND the good/bad polarity;
- dump the QUST aliases directly to confirm `#3 Molag` and `#6 MaraMemory` runtime fills, and the objective[0] target ref;
- confirm the runtime fill of alias `#3 Molag` in Scene02 (the only spoken scene line `12DBB1` is his);
- verify the heavily garbled proper nouns (`Shezarr`/`Shezaru`, `Deidre`, `Edora`, `Imuga`, `Mundasu`→`Mundus`, `Colovia`) against the original Japanese or a cleaner localization before any narrative use — every dialogue cell is flagged 待驗證;
- inspect refs `12BED7` (Door) and `12BF4C` (TravelMarker) + the four `Pepe*` packages if the prison spatial staging matters.

## Open verification

- The two TIF fragments are the single most important unknown: they gate both the 20/100 completion split and the unresolved good/bad polarity.
- All dialogue translations are best-effort over machine-garbled English (Japanese origin); treat as provisional.
