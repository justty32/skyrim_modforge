# Act 4 Memory 04 - Johan the fool

Status: redo slice, queue position #1. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text, not scene phases/actions.
- This mod's English is machine-translated from Japanese and is frequently broken. Garbled proper nouns / phrases are kept verbatim with a `Note:` 待驗證.

## Quest Record

[`140225 zzzCHMemoryQuest04 "Johan the fool"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)

CLI:
- `questdiag Vigilant.esm 0x140225`
- `infodiag Vigilant.esm 0x140225`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x140225`
- EditorID: `zzzCHMemoryQuest04`
- Name: `Johan the fool`
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
| 50 | none | empty |
| 60 | **CompleteQuest** | empty |
| 70 | none | empty |
| 95 | none | empty |
| 100 | **CompleteQuest** | empty |
| 110 | none | empty |
| 120 | none | empty |
| 121 | none | empty |
| 130 | none | empty |
| 140 | none | empty |
| 999 | ShutDownStage | empty |

Objective:

| Index | Source | Translation |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:297) | 「死者在地下做夢。」 |
| | | Note: source `Deads dream under the ground.` — `Deads` is broken English (死者複數誤拼)；譯為「死者」。待驗證。 |

Objective targets:
- 1 target in ESM, 0 conditions. Current CLI output does not print the target ref; needs a deeper QUST target dump if the target location matters.

## Alias / Staging Backbone

All three `SCEN` records below share the same host quest and the same 19-entry alias table (printed identically by `scenediag` for each scene). The scene action `ActorID=N` indexes into this table.

Host quest:
- [`140225 zzzCHMemoryQuest04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)

Host-quest aliases from `scenediag` (19):

| Alias | Name | Fill |
|---:|---|---|
| 0 | `Simon` | uniqueActor [`140211 zzzCHBigBrother01Memory` - Simon](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:593) |
| 1 | `Tlass` | uniqueActor [`140212 zzzCHBigBrother02Memory` - Tlass](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:594) |
| 2 | `Priest` | uniqueActor [`140220 zzzCHArkayPriestMemory` - Arkay Priest](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:599) |
| 3 | `Attendant01` | uniqueActor [`140215 zzzCHAttendantMMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:595) |
| 4 | `Attendant02` | uniqueActor [`140216 zzzCHAttendantFMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:596) |
| 5 | `Attendant03` | uniqueActor [`14021D zzzCHAttendantFElfMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:597) |
| 6 | `Attendant04` | uniqueActor [`140223 zzzCHAttendantFCatMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:602) |
| 7 | `Attendant05` | uniqueActor [`140222 zzzCHAttendantMMemory02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:600) |
| 8 | `Attendant06` | uniqueActor [`14021E zzzCHAttendantMAlikrMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:598) |
| 9 | `Bard` | not filled by `scenediag` (fill not printed) |
| 13 | `Martha` | uniqueActor [`140DF8 zzzCHMarthaGhoul` - Martha](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:574) |
| 16 | `Molag` | (alias filled at runtime; fill not printed) |
| 10 | `MemoryMarker01` | forcedRef `13FC5D:Vigilant.esm` |
| 11 | `ReturnMarker` | forcedRef `140226:Vigilant.esm` |
| 12 | `BadEndMarker` | forcedRef `140DE8:Vigilant.esm` |
| 14 | `BrotherMarker` | forcedRef `140DE9:Vigilant.esm` |
| 15 | `FireMarker` | forcedRef `1413C5:Vigilant.esm` |
| 17 | `SlaverMemory` | (collection alias; no fill printed) |
| 18 | `GuideMarker` | forcedRef `42E0B2:Vigilant.esm` |

Inference:
- The memory subject "Johan/Johann" is **not** a filled alias — he is the role the player inhabits inside this memory; every NPC addresses the player as "Johann". (inference, from the dialogue/scene lines vocatively naming Johann while no alias is named Johan.)
- `Simon` and `Tlass` are the "brother" speakers of the two `zzzCHMeQ04BrotherB01/B02` branches (alias `#0` = Simon, the lead brother; `#1` = Tlass). (inference, from branch INFO condition `GetIsAliasRef alias #0` / `#1`.)
- `Bard` (alias `#9`) is the disguised Molag-Bal envoy "Bal" who delivers the Mace; `Molag` (alias `#16`) is the Bad-End Molag Bal speaker. (inference, from branch INFO conditions + scene actor IDs.)
- `BadEndMarker` (alias `#12`) and `FireMarker` (alias `#15`) plus the bad-path packages [`1413BA zzzCHMeq4BrotherBacktoDark`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/) and `141F24` "burn ... your family to ashes" mark the bad outcome's arson finale. (inference.)

## Scene Records

Scene records are not present as full records in `game-data`; the text lines are linked to `dialogue.md`, while phases/actions are from `scenediag`. `find` returns three `SCEN` for this quest: `140235`, `1413AB`, `1413D0`.

### 140235 zzzCHMeQ4FuneralScene

CLI:
- `scenediag Vigilant.esm 0x140235`

Staging:
- Host quest: [`140225 zzzCHMemoryQuest04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)
- Actors (9): aliases `#0 Simon`, `#1 Tlass`, `#2 Priest`, `#3`-`#8` Attendant01-06; all `behaviorFlags=DeathEnd, DialoguePause`. Priest (`#2`) has `NoPlayerActivation`.
- Phases: 8, each 0 start conditions / 1 complete condition.
- Actions (26): the Priest (`#2`) speaks the eulogy line per phase 0-3 (`140238`, `14023A`, `14023C`, `14023E`); Simon (`#0`) responds at phases 4/6/7 (`140240`, `140243`, `140245`); the rest are `FaceTarget` headtrack actions onto the Priest then Simon (mourners). Martha's funeral.

Translations (Priest, then Simon):
- [`140238`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1886) (Neutral): 「Arkay，生與死之神，我們將 Martha 交付於你手中，她已走完這段以希望為定數的生命之旅。」
  - Note: source `the journey of life of the hope of life that is determined` 文法破碎；意譯。待驗證。
- [`14023A`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1889) (Neutral): 「願這女孩卸下一切重擔、離我們而去，並在 Aetherius 與聖者相會。」
  - Note: source `whether` 為贅字；`in addition to meeting the saint` 意譯為「與聖者相會」。待驗證。
- [`14023C`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1892) (Neutral): 「願我們在離別之悲中，仍能與這女兒一同被引入 Akei 的環中，共享永恆之喜。」
  - Note: `Akei` = Arkay 的另一拼法（本檔多處 Arkay/Arkei/Akei/Akei 混用）。待驗證。
- [`14023E`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1895) (Neutral): 「以那規定生命的 Arkei 之名……」
  - Note: source `Arkei of life that is prescribed. The under the name of...` 破碎且未完。待驗證。
- [`140240`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1898) (Sad, Simon): 「以那規定生命的 Arkei 之名……」（與 `14023E` 同文，由 Simon 哀傷複誦）
- [`140243`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1901) (Sad, Simon): 「各位，感謝你們今日為 Martha 而來。Martha 想必也會對 Aetherius 心懷感激。」
- [`140245`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1904) (Sad, Simon): 「我由衷感謝各位的慰問。非常、非常感謝。」

### 1413AB zzzCHMeQ4BadScene

CLI:
- `scenediag Vigilant.esm 0x1413AB`

Staging:
- Host quest: [`140225 zzzCHMemoryQuest04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)
- Actors (2): aliases `#0 Simon`, `#1 Tlass`, both `flags=NoPlayerActivation`.
- Phases: 7. Phase 0 has 2 complete conditions; the rest 1.
- Actions (13): Simon (`#0`) and Tlass (`#1`) trade lines over the Mace at phases 1-5 (`1413B0`, `1413B2`, `1413B4`, `1413B6`, `1413B8`); a `Timer` of 8s at phase 6 closes the scene. The brothers gloat over the acquired Mace — this is the **bad-path** brother scene. (inference, from `BadScene` EditorID + content.)

Translations:
- [`1413B0`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1940) (Happy, Simon): 「我做到了，我做到了。」
- [`1413B2`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1943) (Happy, Simon): 「Mace、Mace……找到了。就是這個，這把錘。」
- [`1413B4`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1946) (Happy, Tlass): 「我們辦到了，兄弟。只要有這個，我們什麼都做得到。」
- [`1413B6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1949) (Happy, Simon): 「沒錯。只要還記得那件事，剩下的就是這個。來，走吧。」
  - Note: source `This is what remains even think of that` 破碎；意譯。待驗證。
- [`1413B8`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1952) (Happy, Tlass): 「等等，兄弟。」

### 1413D0 zzzCHMeQ04MolagScene

CLI:
- `scenediag Vigilant.esm 0x1413D0`

Staging:
- Host quest: [`140225 zzzCHMemoryQuest04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)
- View: [`1413CA zzzCHMeQ04MolagView`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/) (find).
- Actor (1): alias `#16 Molag`.
- Phases: 4, each 0 start / 1 complete condition.
- Actions (5): two `Package` actions on Molag (phase 0; phases 1-3), then three `Dialog` lines at phases 1-3 (`141F22`, `141F24`, `141F26`), `HeadtrackPlayer` / `FaceTarget`. Molag Bal's final command to Johann — the **bad-end** payoff. (inference, from `Molag` alias + content.)

Translations (Molag Bal, all Neutral):
- [`141F22`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1955): 「Johann，你為我們效力甚善。我要提出最後一個願望。」
  - Note: source `I'll ask one last hope` — `hope` 疑為「請求/願望」。待驗證。
- [`141F24`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1958): 「燒光一切……？好。如你所願——你建造的一切、你的家人，盡化灰燼。」
- [`141F26`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1961): 「現在，安睡吧。Oblivion 應許永恆的安寧。」

## Custom Dialogue Branches

`find` returns three custom dialog branches for this quest plus a quest-owned greeting branch. Branch-level `infodiag` (by branch FormID) is unsupported; all INFO data below is from the quest-level `infodiag Vigilant.esm 0x140225`.

### Greeting branch: zzzCHMeQ4GreetB01 (`140228`)

Topic [`140229 zzzCHMeQ4GreetB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1874), 6 INFOs, each `Goodbye`, one per Attendant alias `#3`-`#8` (`GetIsAliasRef`). Funeral-mourner ambient barks. Linked, not fully re-translated here — sample: INFO `14022A` (Sad, alias #3): 「都已經十六歲了……唉。那 …… 不是嗎？前路還很長。」 (Note: source `the ...... is not it?` 破碎留白。待驗證。) INFO `14022E` (Anger, alias #7) names `Martha`: 「就因一個人 Martha 失明，你到底都跑哪去了？」 (Note: source `Nantes to one person Martha blind` 嚴重破碎；意譯。待驗證。)

### Brother branch B01: zzzCHMeQ04BrotherB01 (`140231`)

Speaker: brothers Simon (`#0`) / Tlass (`#1`). **Stage-gated** by `GetStage <=` on quest `140225` — these are the early, pre-completion consolation lines.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`140232 …BrotherB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1882) | `140233` | `Goodbye` | `GetStage <= 20`; `GetIsAliasRef alias #0` (Simon) | (Sad) 「Johan，別太過自責。」 |
| `140232` (T01) | `140234` | `Goodbye` | `GetStage <= 30`; `GetIsAliasRef alias #1` (Tlass) | (Sad) 「Johann。那天，酒館不算吵鬧，錯不只在你。我這做兄弟的也有責任。所以……」 |

### Brother branch B02: zzzCHMeQ04BrotherB02 (`140249`)

Speaker: Simon (`#0`), no stage gate. Later "let's go home" beat.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`14024A …BrotherB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1907) | `14024B` | none | `GetIsAliasRef alias #0` | (Fear) 「Johann，你還好嗎？」 |
| [`14024C …BrotherB02T02` prompt "Brother..."](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1910) | `14024E` | none | `GetIsAliasRef alias #0` | (Sad) 「我們回家吧。要下雨了。」 |
| [`14024D …BrotherB02T03` prompt "Leave me alone a bit"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1913) | `14024F` | `Goodbye` | `GetIsAliasRef alias #0`; VMAD `CHMeq4_TIF__0214024F.Fragment_0` on end | (Sad) 「儘快回來。Martha 縱使病著，你的悲傷想必也傳得到她那裡。」 Note: source `Martha Kanashimuzo you surely reaches even sick` 含未翻譯的日文羅馬字 `Kanashimuzo`(悲しむぞ)；意譯。待驗證。 |

### Bard branch B01: zzzCHMeQ04BardB01 (`140803`) — the choice branch

Speaker: alias `#9 Bard` (the Molag-Bal envoy "Bal"). Opener **stage-gated `GetStage == 50`**. This is the branch where Johann decides whether to take the Mace. The "Give me a Mace" choice carries a VMAD fragment that also fires `Fragment_1` `OnBegin` (state change), and "go away" carries its own end fragment — i.e. this branch routes the 60 vs 100 completion. (inference, from VMAD placement + the two `CompleteQuest` stages.)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`140804 …BardB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1916) | `140805` | none | `GetStage == 50`; `GetIsAliasRef alias #9` | (Happy) 「失去摯愛這種事，是非常令人哀傷的。Akei 真是殘酷。」 |
| [`140806 …BardB01T02` prompt "who are you?"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1919) | `140807` | none | `GetIsAliasRef alias #9` | (Happy) 「我只是個吟遊詩人。我名叫 Bal。要不要聽我唱一曲？你喜歡 Eroisa 與 Polydor 的故事嗎？」 Note: `Eroisa`/`Polydor` 為音譯人名，待驗證。 |
| [`140808 …BardB01T03` prompt "It is not in such a mood"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1922) | `140809` | none | `GetIsAliasRef alias #9` | (Neutral) 「真可惜。沒這個心情的話，那也沒辦法。」 |
| [`14080A …BardB01T04` prompt "What I do use a hell ?"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1925) | `14080B` | none | `GetIsAliasRef alias #9` | (Happy) 「錘。一把錘……我受我主之命，要把這把 Mace 交給你。」 Note: prompt `What I do use a hell ?` 破碎，疑為「我要這東西做什麼用？」。待驗證。 |
| [`14080C …BardB01T05` prompt "what do you want to let?"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1928) | `14080D` | none | `GetIsAliasRef alias #9` | (Happy) 「我希望你收集有罪之人的靈魂。我們需要數千個。你願意嗎？」 |
| [`14080E …BardB01T06` prompt "That way , sister will come back ?"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1931) | `14080F` | none | `GetIsAliasRef alias #9` | (Happy) 「是的，當然。我主能讓她復生，因為他在 Arkay 之環之外。」 Note: prompt 中 `sister` 與正文常稱 Martha；此處關係詞（妹/姊）待驗證。 |
| [`140810 …BardB01T07` prompt "Give me a Mace"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1934) | `140811` | `Goodbye` | `GetIsAliasRef alias #9`; VMAD `CHMeq4_TIF__02140811` (`Fragment_1` OnBegin, `Fragment_0` OnEnd) | (Happy) 「好的，好的。這把 Mace 從一開始就是你的了。親愛的 Johann。」 |
| [`140812 …BardB01T08` prompt "go away"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1937) | `140813` | `Goodbye` | `GetIsAliasRef alias #9`; VMAD `CHMeq4_TIF__02140813.Fragment_0` on end | (Happy) 「我明白了。那麼，若你改變心意，請到 Bravil 來。我會帶著錘等你。」 |

## Related Records

These are the actors/items this quest's aliases and dialogue reference (verified via `scenediag` alias fills and `infodiag` / item text). Johan himself has **no NPC record** — he is the player role.

NPCs (alias actors):
- [`140211 zzzCHBigBrother01Memory` - Simon](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:593)
- [`140212 zzzCHBigBrother02Memory` - Tlass](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:594)
- [`140220 zzzCHArkayPriestMemory` - Arkay Priest](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:599)
- [`140DF8 zzzCHMarthaGhoul` - Martha](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:574)
  - Note: EditorID `MarthaGhoul` (inference): Martha is later a ghoul / undead, consistent with the bad-end resurrection bargain. 待驗證 against the NPC record.
- Attendant01-06: [`140215`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:595), [`140216`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:596), [`14021D`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:597), [`140223`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:602), [`140222`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:600), [`14021E`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:598)
- `Bard` (alias #9) and `Molag` (alias #16): no `scenediag`-printed fill; runtime-filled disguise/Daedra. The Bard self-names "Bal" in INFO `140807`. (inference.)

Items:
- [`00D9FC zzzAoMMq07MaceofMolagBal` - Mace of Molag Bal](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1013)
  - The Mace delivered by the Bard (INFO `14080B`/`140811`) and gloated over in `BadScene` (`1413B2` "this mace"). EditorID prefix `Mq07` (not `MeQ04`) — it is the shared Mace-of-Molag-Bal artifact, reused here, not a quest-04-private item. (inference, from the EditorID.)

## Related Book Translation

None owned by this quest. `find zzzCHMeQ04` / `zzzCHMeQ4` returns no `BOOK` record, and `infodiag` lists no book topic. (The "Bravil was burnt" line in [`0B0825 zzzCHSlaverNote04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1319) is thematically adjacent to the Bravil arson ending but is a slaver-note record, **not** owned by MeQ04 — excluded per source policy.) `booktext` not run.

## Reconstruction Notes

Source-grounded:
- This memory is [`140225 zzzCHMemoryQuest04 "Johan the fool"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296), objective [`Deads dream under the ground.`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:297).
- It owns 3 `SCEN`: `140235` Funeral (Martha's funeral, Priest + Simon eulogy), `1413AB` Bad (Simon + Tlass gloat over the Mace), `1413D0` Molag (Molag Bal's "burn your family / sleep" command).
- It owns 4 dialog branches: a quest greeting (`140228`, 6 mourner barks) and three custom branches — Brother B01 (`140231`, stage-gated `<=20`/`<=30` console lines), Brother B02 (`140249`, "let's go home"), and Bard B01 (`140803`, the Mace-offer choice, opener gated `GetStage==50`).
- The player plays "Johann"; every speaker addresses Johann. No NPC record for Johan exists.

How the 60 vs 100 branch is chosen (inference, source-grounded shape):
- The Bard branch `140803` opens only at `GetStage == 50` and presents the Mace. The terminal choices carry VMAD fragments: `140811` "Give me a Mace" fires `Fragment_1` **OnBegin** plus `Fragment_0` OnEnd (a state change at line start, not just at end), while `140812` "go away" fires only `Fragment_0` OnEnd. These two terminal choices are the most likely split point feeding the two `CompleteQuest` stages (60 and 100).
- **Polarity (which is good vs bad):** the *content* is decodable even though the exact stage numbers per fragment are not in CLI output:
  - **Bad / corruption outcome** = accept the Mace ("Give me a Mace", `140811`) → collect sinful souls → `BadScene` (`1413AB`) brothers seize the Mace → `MolagScene` (`1413D0`) Molag Bal orders Johann to **burn his own family to ashes** (`141F24`) and "sleep" into Oblivion. This is the arson/damnation end (BadEndMarker `#12`, FireMarker `#15`). (inference, strong — content explicit.)
  - **Good / refusal outcome** = decline ("go away", `140813`/`140812`) → "come to Bravil if you change your mind"; no soul-harvest, no arson. (inference.)
  - **Stage-number assignment** (which of 60/100 is the bad end) is NOT decidable from `questdiag`/`infodiag` alone — needs the two TIF fragment scripts' `SetStage` calls. Marked TODO below.

Open verification:
- decompile / inspect fragment scripts `CHMeq4_TIF__02140811` (Bard accept), `CHMeq4_TIF__02140813` (Bard refuse), `CHMeq4_TIF__0214024F` (Brother B02 end) to read their `SetStage`/`CompleteQuest` targets and pin **60 vs 100 to good vs bad**;
- confirm alias `#9 Bard` and `#16 Molag` fills (uniqueActor vs forcedRef) via a direct QUST alias dump — `scenediag` did not print them;
- verify the `Bal` (`140807`) / `Eroisa` / `Polydor` proper nouns and the broken-English prompts (`What I do use a hell ?`, the `Kanashimuzo` romaji in `14024F`, `Deads`) against the Japanese source if a JP string table is available;
- confirm Martha's `zzzCHMarthaGhoul` (`140DF8`) record state (does she rise as a ghoul on the bad path?) and the Mace artifact `00D9FC` gameplay function;
- inspect `BadEndMarker` (`140DE8`), `FireMarker` (`1413C5`), `BrotherMarker` (`140DE9`) refs and the bad-path packages `1413BA zzzCHMeq4BrotherBacktoDark`, `1413D1-1413D3` Molag packages if spatial/arson staging matters.
