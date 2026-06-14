# Act 4 Memory 06 - Remain of Miracle

Status: redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- This quest owns **no `SCEN` records** (see Reconstruction Notes); it is a pure interrogation-dialogue memory, so there is no scene-staging section.

## Quest Record

[`06A23B zzzCHMemoryQuest06 "Remain of Miracle"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:371)

CLI:
- `questdiag Vigilant.esm 0x06A23B`
- `infodiag Vigilant.esm 0x06A23B`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x06A23B`
- EditorID: `zzzCHMemoryQuest06`
- Name: `Remain of Miracle`
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
| 30 | CompleteQuest | empty |
| 40 | none | empty |
| 999 | ShutDownStage | empty |

Objective:

| Index | Source | Translation |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:372) | 祭司在斷裂的塔中發笑 |

Objective targets:
- 1 target in ESM, 0 conditions.
- Current CLI output does not print the target ref; needs a deeper QUST target dump if the target location matters.

## Subject

- Subject confirmed via topic EditorIDs: every owned topic is `zzzCHMeQ06Pepe…` and the opener prompt is [`"Are you Pepe, Inquisitor of Alessian Order?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:862). This is the **Pepe** memory.
- Speaker NPC (inference): [`06A230 zzzCHInquisitorPepeMemory3`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1069) — FormID sits one record before the quest `06A23B`, name "Inquisitor Pepe". The speaker condition `GetIsAliasRef == 1` on alias `#0` (see below) targets this actor; the alias→ref fill is not printed by current CLI, so the exact alias-0 ref is **(inference)** pending a QUST alias dump.
- Related Pepe NPC variants: [`12BF48 zzzCHInquisitorPepeMemory "Inquisitor Pepe"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:558), [`05ADFD zzzCHInquisitorPepeMemory2`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1044), [`081E46 zzzCHInquisitorPepe "Inquisitor Pepe"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1065).
- Per the index, Pepe also appears in **MeQ05 Ada Bal** ([`05AE03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)); this slice covers MeQ06 only.

## Dialogue View / Branches

The quest owns one `DialogView` and two `DialogBranch` records (from `find zzzCHMeQ06`):

- View: `06B54C zzzCHMeq06PepeView`
- Branch B01: `06B54D zzzCHMeq06PepeB01` — the 7-topic interrogation tree.
- Branch B02: `06B55C zzzCHMeQ06PepeB02` — a single one-line branch.

`infodiag 0x06A23B` confirms all 8 INFOs are owned by quest `06A23B`. The speaker condition on **every** INFO is `GetIsAliasRef == 1` on alias `#0`; this is a single-speaker (Pepe) memory with no second-speaker alias and no good/bad alias split.

### Branch B01: Interrogation (`06B54D zzzCHMeq06PepeB01`)

Player-driven interrogation. Topic priority is `50` on all. Conditions per `infodiag`.

| Topic | INFO | Flags | Conditions | Prompt → Response (translation) |
|---|---|---|---|---|
| [`06B54E zzzCHMeq06PepeB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:862) | `06B54F` | none | `GetStage == 10`; `GetIsAliasRef alias #0` | Prompt: 「你就是阿萊西亞教團的審判官 Pepe 嗎？」 Response: 「沒錯。我的樣貌已經變了不少。那麼，野蠻的科洛維亞人想從這老頭身上問出什麼？」 |
| [`06B550 zzzCHMeQ06PepeB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:865) | `06B551` | none | `GetIsAliasRef alias #0` | Prompt: 「石頭呢。你把它藏在哪裡？」 Response 1: 「石頭，還是當時的傭兵。過了數百年，唯獨人們的愚蠢似乎一成不變。」 Response 2: 「石頭已經不在這世上了。它早就被帶走了。這全是拜你們所賜。」 Note: Response 1 原文 `Stone or was still mercenary. Hundreds of years passed since it will` 文法崩壞，譯文為近似；待驗證。 |
| [`06B552 zzzCHMeQ06PepeB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:869) | `06B553` | none | `GetIsAliasRef alias #0` | Prompt: 「什麼？」 Response 1: 「這是由你們引起的戰爭。成千上萬人的血流遍東西兩方，那石頭終於得到了滿足。」 Response 2: 「那是耗費了漫長時間、令人畏懼之物。瘟疫……如今在內戰中，也已將數十億的靈魂折進那塊石頭裡。」 Response 3: 「你們的王也會無法忍受、想要它吧？石頭沒了。它去到了無人能及之處。」 Note: 原文 `Plague, would have folded also go billion of the soul` 與 `civil war n` 為破碎機翻；譯文取其大意，待驗證。 |
| [`06B554 zzzCHMeQ06PepeB01T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:874) | `06B555` | none | `GetIsAliasRef alias #0` | Prompt: 「把它帶走的人，叫什麼名字？」 Response: 「Molag Bal。他是 Spooky Togake 之王。他在日蝕之日降臨此塔，從我們手中奪走了石頭。」 Note: `Spooky Togake` 疑為被誤音譯/在地化的專有名詞（可能是 Coldharbour 之類），待驗證。 |
| [`06B556 zzzCHMeQ06PepeB01T05`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:877) | `06B557` | none | `GetIsAliasRef alias #0` | Prompt: 「就算那是真相——石頭究竟在哪？」 Response 1: 「真相就是真相。Adabaru，連同它的仿製品我都失去了。人們因此得到了自由，他們被解放了。」 Response 2: 「Shezaru 出現了……」 Note: Response 2 原文 `Shezaru appeared, but should give me bouncing the neck of you guys are after. Ikanu anything he wanted in the other` 嚴重崩壞、無法可靠重建；保留原文，待驗證。`Adabaru`、`Shezaru`、`Ikanu` 為專有名詞。 |
| [`06B558 zzzCHMeQ06PepeB01T06`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:881) | `06B559` | none | `GetIsAliasRef alias #0` | Prompt: 「石頭在哪？明早我們就要處決你。」 Response 1: 「我們守著那石頭太久了。如今擺在你們面前的，不過是一具沒有靈魂的空殼。」 Response 2: 「我不會說壞話。……無論如何，那石頭都成了眼前這怪物的教訓。」 Note: 原文 `The Tasukaru to be willing to do so` 與 Response 2 整句機翻崩壞；譯文取近似大意，待驗證。`Tasukaru` 疑為專有名詞或誤譯。 |
| [`06B55A zzzCHMeQ06PepeB01T07`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:885) | `06B55B` | `Goodbye` | `GetIsAliasRef alias #0`; VMAD `CHMeq06_TIF__0206B55B.Fragment_0` on end | Prompt: 「審問到此為止。」 Response: 「結束，還是另一回事？這樣也好。從此我不必再聞科洛維亞人那帶著敵意的臭氣了。」 Note: 原文 `The end or the other? It was good. From time I no enemy smelly breath Colovian people` 文法崩壞，譯文為近似，待驗證。 |

### Branch B02: Re-entry guard (`06B55C zzzCHMeQ06PepeB02`)

| Topic | INFO | Flags | Conditions | Response (translation) |
|---|---|---|---|---|
| [`06B55D zzzCHMeQ06PepeB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:888) | `06B55E` | `Goodbye` | `GetStage == 20`; `GetIsAliasRef alias #0` | 「什麼？審問不是已經結束了嗎？」 |

- This single-line, no-prompt `Goodbye` INFO is gated at `GetStage == 20` — it fires when the player re-talks to Pepe after the interrogation has already concluded. It is a state guard, not an alternate story branch.

## Linear-or-branched verdict

**Verdict: LINEAR (single completion at stage 30), not karma-branched.**

Evidence:
- `questdiag` shows exactly one `CompleteQuest` stage (30); no second `CompleteQuest` in the 100–350 band. This breaks the two-band good/bad signature documented for branched memories in the [index](act-4-memory-index.md) (e.g. MeQ07's 70/150 split).
- No second-speaker alias and no stage-gated alternate-speaker branch exist. In MeQ07 the branch split is implemented as `GetStage==40`→Alessia alias `#6` vs `GetStage==50`→Molag Bal alias `#5`. Here **all 8 INFOs share one alias (`#0`)** and one speaker (Pepe); the only stage conditions are `GetStage==10` (B01 opener) and `GetStage==20` (B02 re-entry guard) — sequential gates, not mutually-exclusive outcome branches.
- The two `DialogBranch` records are not good/bad alternatives: B01 is the interrogation tree, B02 is a post-completion re-talk guard.
- No branch is implemented elsewhere via dialogue conditions for this quest: `infodiag 0x06A23B` returns only these 8 owned INFOs, and `find zzzCHMeQ06` returns no additional topics, scenes, or views.

Caveat (inference): the VMAD fragment on the `Goodbye` topic `06B55A`/`06B55B` (`CHMeq06_TIF__0206B55B.Fragment_0`) is not decoded here; its Papyrus likely advances the quest to its single completion. There is no evidence it routes to a second outcome. Confirm by decompiling the fragment if branch polarity ever matters.

## Reconstruction Notes

Source-grounded:
- This memory is [`06A23B zzzCHMemoryQuest06 "Remain of Miracle"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:371), objective [`Priest laughs in the broken tower`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:372).
- It is the shortest memory quest: 6 stages, single `CompleteQuest` at 30, `ShutDownStage` at 999.
- It owns **no `SCEN` records**: `find Vigilant.esm zzzCHMeQ06` returns 8 topics + 2 branches + 1 view and nothing else; `scenediag` on the view FormID confirms "is not a Scene". This is a pure player-vs-Pepe interrogation, not a staged monologue memory like MeQ07.
- The whole quest is one custom DialogView (`06B54C zzzCHMeq06PepeView`) with B01 (7-topic interrogation) and B02 (1-line re-entry guard).
- Lore content (from the INFO responses): Pepe was an Alessian Order Inquisitor who guarded a soul-devouring stone (`Adabaru`); the war fed it tens of thousands of souls; on the day of an eclipse Molag Bal descended to the tower and took the stone away, leaving Pepe a soulless "empty shell".

Open verification:
- dump QUST aliases to confirm alias `#0` fills [`06A230 zzzCHInquisitorPepeMemory3`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1069) and to print the objective-0 target ref / location (the "broken tower");
- decompile fragment `CHMeq06_TIF__0206B55B` (on `06B55B`) to confirm it sets the single completion and grants no alternate outcome;
- cross-check the Pepe overlap with **MeQ05 Ada Bal** ([`05AE03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)) to determine which is the primary Pepe memory;
- resolve the flagged garbled proper nouns: `Adabaru`, `Shezaru`, `Ikanu`/`Ikanuzo`, `Spooky Togake`, `Tasukaru` — likely mistranscribed/localized names needing a lore or in-game cross-reference;
- no book is associated with this quest in `find`/`infodiag`; `booktext` not run (no BOOK FormID owned).
