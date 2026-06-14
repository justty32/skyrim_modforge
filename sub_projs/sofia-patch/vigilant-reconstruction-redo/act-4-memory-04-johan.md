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

RESOLVED: `scenediag` output confirms `uniqueActor` fills for aliases 0-8 and 13, `forcedRef` for markers 10-12/14-15/18. Aliases `#9 Bard` and `#16 Molag` print name only — no fill field — confirming they are **not** uniqueActor/forcedRef in the alias record itself; they are filled by some other mechanism (external NPC reference placed in the cell, or a script-side `ForceRefTo` not visible in alias QUST records). `scenediag` CLI does not expose the fill source for these two. (Source: `scenediag Vigilant.esm 0x1413D0`; identical result for `0x140235` and `0x1413AB`.)

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
| 9 | `Bard` | fill not exposed by `scenediag` (no uniqueActor/forcedRef in alias record) — NPC `13909B zzzCHBardMemory` exists; likely the cell-placed bard ref enabled by QF `Fragment_8` `Alias_Bard.TryToEnable()`. (inference; `find zzzCHBardMemory` → `Vigilant.esm:0x13909B Npc`) |
| 13 | `Martha` | uniqueActor [`140DF8 zzzCHMarthaGhoul` - Martha](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:574) |
| 16 | `Molag` | fill not exposed by `scenediag` — enabled by QF `Fragment_20` `Alias_Molag.TryToEnable()` before `MolagScene.ForceStart()`. NPC `12339D zzzCHMolagBal "Molag Bal"` is the main Vigilant Molag Bal NPC; alias fill via in-cell ref or ForceRefTo not visible in alias record. (inference; `find zzzCHMolagBal` → `Vigilant.esm:0x12339D Npc`) |
| 10 | `MemoryMarker01` | forcedRef `13FC5D:Vigilant.esm` |
| 11 | `ReturnMarker` | forcedRef `140226:Vigilant.esm` |
| 12 | `BadEndMarker` | forcedRef `140DE8:Vigilant.esm` |
| 14 | `BrotherMarker` | forcedRef `140DE9:Vigilant.esm` |
| 15 | `FireMarker` | forcedRef `1413C5:Vigilant.esm` |
| 17 | `SlaverMemory` | LocationAlias (from QF psc `LocationAlias Property Alias_SlaverMemory Auto`; fill source not in `scenediag`) |
| 18 | `GuideMarker` | forcedRef `42E0B2:Vigilant.esm` |

Notes (source-grounded):
- The memory subject "Johan/Johann" is **not** a filled alias — he is the role the player inhabits inside this memory; every NPC addresses the player as "Johann". (inference, from the dialogue/scene lines vocatively naming Johann while no alias is named Johan.)
- `Simon` and `Tlass` are the "brother" speakers of the two `zzzCHMeQ04BrotherB01/B02` branches (alias `#0` = Simon, the lead brother; `#1` = Tlass). Confirmed: `infodiag` shows `GetIsAliasRef ReferenceAliasIndex=0` (Simon) and `=1` (Tlass) on those INFO conditions.
- `Bard` (alias `#9`) is the disguised Molag-Bal envoy "Bal": confirmed by `infodiag` showing `GetIsAliasRef ReferenceAliasIndex=9` on all Bard-branch INFOs, and INFO `140807` self-naming "Bal". `Molag` (alias `#16`) is the Bad-End Molag Bal speaker in `MolagScene` (`1413D0`), where `ActorID=16`.
- `BadEndMarker` (alias `#12` forcedRef `140DE8`) and `FireMarker` (alias `#15` forcedRef `1413C5`) are activated on the bad path: QF `Fragment_18` enables `Alias_FireMarker`, moves Simon/Tlass to `BrotherMarker`, and starts `BadScene`. Package `1413BA zzzCHMeq4BrotherBacktoDark` (confirmed: `find zzzCHMeq4BrotherBacktoDark` → `Vigilant.esm:0x1413BA Package`) handles their movement.

## Scene Records

Scene records are not present as full records in `game-data`; the text lines are linked to `dialogue.md`, while phases/actions are from `scenediag`. `find` returns three `SCEN` for this quest: `140235`, `1413AB`, `1413D0`.

### 140235 zzzCHMeQ4FuneralScene

CLI:
- `scenediag Vigilant.esm 0x140235`

psc:
- `sf_zzzchmeq4funeralscene_02140235.psc` — `Fragment_0`: `GetOwningQuest().SetStage(30)` (scene end → stage 30)

Staging:
- Host quest: [`140225 zzzCHMemoryQuest04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)
- ForceStart: QF `Fragment_2` (stage 10): `FuneralScene.ForceStart()` + `RegisterSceneSkip(self, FuneralScene, 30, True)` — scene completes at stage 30 or is skip-registered to jump to 30 if player skips.
- Scene end → `SetStage(30)` (from `sf_zzzchmeq4funeralscene_02140235.psc:8`).
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

psc:
- `sf_zzzchmeq4badscene_021413ab.psc` — `Fragment_0`: `GetOwningQuest().SetStage(120)` (scene end → stage 120)

Staging:
- Host quest: [`140225 zzzCHMemoryQuest04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)
- ForceStart: QF `Fragment_18` (stage 110): `BadScene.ForceStart()` + `RegisterSceneSkip(self, BadScene, 120, True)`.
- Scene end → `SetStage(120)` (from `sf_zzzchmeq4badscene_021413ab.psc:8`).
- Actors (2): aliases `#0 Simon`, `#1 Tlass`, both `flags=NoPlayerActivation`.
- Phases: 7. Phase 0 has 2 complete conditions; the rest 1.
- Actions (13): Simon (`#0`) and Tlass (`#1`) trade lines over the Mace at phases 1-5 (`1413B0`, `1413B2`, `1413B4`, `1413B6`, `1413B8`); a `Timer` of 8s at phase 6 closes the scene. The brothers gloat over the acquired Mace — this is the **bad-path** brother scene. (EditorID `BadScene`; confirmed by QF `Fragment_18` which also calls `Alias_Martha.TryToDisable()` and `Alias_Simon.TryToMoveTo(Alias_BrotherMarker)` before starting this scene.)

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

psc:
- `sf_zzzchmeq04molagscene_021413d0.psc`:
  - `Fragment_0`: `GetOwningQuest().SetStage(130)` (scene end → stage 130 → `stop()`)
  - `Fragment_1`: `GetOwningQuest().SetStage(121)` (earlier phase → stage 121 → player moves back to ReturnMarker and `SetStage(140)`)

Staging:
- Host quest: [`140225 zzzCHMemoryQuest04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)
- ForceStart: QF `Fragment_20` (stage 120): `Alias_Simon.TryToDisable()`, `Alias_Tlass.TryToDisable()`, `Alias_FireMarker.TryToEnable()`, `Alias_Molag.TryToEnable()`, `MolagScene.ForceStart()` + `RegisterSceneSkip(self, MolagScene, 130, True)` (QF `Fragment_29`).
- Scene phase outcomes:
  - Phase that triggers `Fragment_1` → `SetStage(121)`: QF `Fragment_22` (stage 121) moves player to `ReturnMarker`, plays `GetUp` idle, `EnablePlayerControls`, then `SetStage(140)`. (inference: `Fragment_22` at stage 121 = escape hatch letting player leave after Molag speaks, good-end-bad-path hybrid.)
  - Phase that triggers `Fragment_0` → `SetStage(130)`: QF `Fragment_23` (stage 130) = `stop()`. Full shutdown.
- Actor (1): alias `#16 Molag`.
- Phases: 4, each 0 start / 1 complete condition.
- Actions (5): two `Package` actions on Molag (phase 0; phases 1-3), then three `Dialog` lines at phases 1-3 (`141F22`, `141F24`, `141F26`), `HeadtrackPlayer` / `FaceTarget`. Molag Bal's final command to Johann — the **bad-end** payoff. (`MolagScene` EditorID `zzzCHMeQ04MolagScene`; content confirmed by `scenediag` scene-category topics.)

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

## Branch Routing and Karma (RESOLVED)

Source: TIF psc files + QF psc; all `SetStage` calls are literal psc text.

### Complete stage routing

```
stage 0   QF Fragment_0  → MoveTo Alias_MemoryMarker01 (player teleports in)
stage 10  QF Fragment_2  → FuneralScene.ForceStart() + RegisterSceneSkip(30)
stage 20  QF Fragment_4  → TryToEvaluatePackage (all NPCs)
stage 30  (FuneralScene end, SF Fragment_0 → SetStage(30))
          QF Fragment_6  → Alias_Simon.TryToEvaluatePackage
stage 40  (TIF 0214024F.Fragment_0 → SetStage(40), from INFO 14024F "Leave me alone")
          QF Fragment_8  → Alias_Bard.TryToEnable() + TeleportIn.Play(Bard)
stage 50  QF Fragment_27 → SetObjectiveDisplayed(0)
          [Bard branch 140803 opener: GetStage==50 → player speaks to Bard "Bal"]

── GOOD PATH ───────────────────────────────────────────────────────────
  TIF 02140813.Fragment_0 (INFO 140813 "go away", OnEnd) → SetStage(60)
  stage 60  CompleteQuest
            QF Fragment_10 → Karma.Mod(+3.0), KarmaUp.Show(),
                              TeleportOut.Play(Bard), wait 3s,
                              Alias_Bard.TryToDisable(), SetStage(70)
  stage 70  QF Fragment_12 → FadeOutGame, MoveTo Alias_ReturnMarker, Stop()
  [quest ends; zzzCHKarma global += 3.0]

── BAD PATH ────────────────────────────────────────────────────────────
  TIF 02140811.Fragment_1 (INFO 140811 "Give me a Mace", OnBegin) → SetStage(95)
  stage 95  QF Fragment_14 → Karma.Mod(-3.0), KarmaDown.Show()
  TIF 02140811.Fragment_0 (INFO 140811 "Give me a Mace", OnEnd) → SetStage(100)
  stage 100 CompleteQuest
            QF Fragment_16 → FadeOutGame, MoveTo Alias_BadEndMarker
  stage 110 QF Fragment_18 → Alias_Simon.TryToDisable(), Alias_Tlass.TryToDisable(),
                               Alias_FireMarker.TryToEnable(),
                               Heartbeat.Play(player), ForceFirstPerson,
                               DisablePlayerControls, player.PlayIdle(Knockdown),
                               Alias_Martha.TryToDisable(),
                               BadScene.ForceStart() + RegisterSceneSkip(120)
  stage 120 (BadScene end, SF Fragment_0 → SetStage(120))
            QF Fragment_20 (via MolagScene launch):
                               Alias_Simon.TryToDisable(), Alias_Tlass.TryToDisable(),
                               Alias_FireMarker.TryToEnable(), wait 1s,
                               Alias_Molag.TryToEnable(), MolagScene.ForceStart()
            QF Fragment_29 → RegisterSceneSkip(MolagScene, 130)
  stage 121 (MolagScene Fragment_1 → SetStage(121))
            QF Fragment_22 → FadeOutGame, MoveTo Alias_ReturnMarker,
                               player.PlayIdle(GetUp), wait 7s,
                               EnablePlayerControls, SetStage(140)
  stage 130 (MolagScene Fragment_0 → SetStage(130))
            QF Fragment_23 → stop()
  stage 140 QF Fragment_25 → SetObjectiveCompleted(0), qGuide.SetStage(40),
                               kmyQuest.ModRadiance(3.0)
  [quest ends via stage 130 Stop or stage 140 path]
  [zzzCHKarma global −= 3.0 at stage 95]
```

### Karma polarity (RESOLVED)

- **Good / refusal** = "go away" (INFO `140813`, TIF `02140813`) → `SetStage(60)` → `CompleteQuest` → `Karma.Mod(+3.0)`.
  Source: `chmeq4_tif__02140813.psc:9`; `qf_zzzchmemoryquest04_02140225.psc:178-183` (Fragment_10).
- **Bad / corruption** = "Give me a Mace" (INFO `140811`, TIF `02140811`) → `SetStage(95)` (OnBegin) → `Karma.Mod(-3.0)` → `SetStage(100)` (OnEnd) → `CompleteQuest`.
  Source: `chmeq4_tif__02140811.psc:8,18`; `qf_zzzchmemoryquest04_02140225.psc:169-173` (Fragment_14).
- Karma global: `0x0B19F4 zzzCHKarma` (`GlobalFloat`, confirmed by `find zzzCHKarma`).

### Hub guide wiring (RESOLVED — partial)

QF `Fragment_25` (stage 140, bad path late) calls `qGuide.SetStage(40)` and `ModRadiance(3.0)`.
Good path ends at stage 70 via `Stop()`; no explicit `qGuide.SetStage` call in `Fragment_10` or `Fragment_12` — the guide hub tracks quest completion via `CHMemoryGuideQuestScript.TraceOFF()` on `AllowRepeatedStages` polling (source: `chmemoryguidequestscript.psc`, `qf_zzzchmemoryguide_0242e0b1.psc:Fragment_26`). Per-dream `Fragment_N` in the guide psc are empty stubs (`;Dream04 Finished`) for MeQ04's slot — the hub reacts to the quest's completion state, not a dedicated callback. (inference from guide psc structure; Dream04 slot = MeQ04 by quest numbering, unconfirmed which `DreamNN` property maps to `140225`.)

### TIF wiring for Brother B02 (RESOLVED)

INFO `14024F` "Leave me alone a bit" (TIF `0214024F`): `Fragment_0` OnEnd → `SetStage(40)` (source: `chmeq4_tif__0214024f.psc:9`). Stage 40 enables the Bard (`QF Fragment_8`). This is the trigger that summons the Mace-bearing Bard after Johann asks to be alone at the grave.

## Reconstruction Notes

Source-grounded:
- This memory is [`140225 zzzCHMemoryQuest04 "Johan the fool"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296), objective [`Deads dream under the ground.`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:297).
- It owns 3 `SCEN`: `140235` Funeral (Martha's funeral, Priest + Simon eulogy), `1413AB` Bad (Simon + Tlass gloat over the Mace), `1413D0` Molag (Molag Bal's "burn your family / sleep" command).
- It owns 4 dialog branches: a quest greeting (`140228`, 6 mourner barks) and three custom branches — Brother B01 (`140231`, stage-gated `<=20`/`<=30` console lines), Brother B02 (`140249`, "let's go home"), and Bard B01 (`140803`, the Mace-offer choice, opener gated `GetStage==50`).
- The player plays "Johann"; every speaker addresses Johann. No NPC record for Johan exists.

How the 60 vs 100 branch is chosen (RESOLVED):
- "go away" (INFO `140813`) → TIF `02140813.Fragment_0` → `SetStage(60)` → CompleteQuest → `Karma+3`. Source: `chmeq4_tif__02140813.psc:9`.
- "Give me a Mace" (INFO `140811`) → TIF `02140811.Fragment_1` (OnBegin) → `SetStage(95)` → `Karma−3`; then TIF `02140811.Fragment_0` (OnEnd) → `SetStage(100)` → CompleteQuest. Source: `chmeq4_tif__02140811.psc:8,18`.

Open verification:
- RESOLVED: 60 vs 100 polarity pinned — stage 60 = good/refusal (Karma+3), stage 100 = bad/corruption (Karma−3 at 95, CompleteQuest at 100). Source: TIF psc files.
- RESOLVED: Brother B02 T03 TIF wiring — `SetStage(40)` summons the Bard. Source: `chmeq4_tif__0214024f.psc:9`.
- RESOLVED (partial): alias `#9 Bard` and `#16 Molag` fills — `scenediag` confirms no uniqueActor/forcedRef in alias record for these two; NPC candidates `13909B zzzCHBardMemory` and `12339D zzzCHMolagBal` identified by `find`; exact fill mechanism (cell-placed ref vs `ForceRefTo`) not readable from current CLI tooling. Marked `(inference)` in alias table above.
- RESOLVED (partial): SCEN staging for all 3 scenes — phase fragment → stage targets now sourced from scene psc files. MolagScene Fragment_1/Fragment_0 split (SetStage 121 vs 130) gives two bad-path exits; QF stages 121 and 130 both handled.
- RESOLVED: karma global confirmed `0x0B19F4 zzzCHKarma GlobalFloat`.
- Remaining unverified:
  - verify the `Bal` (`140807`) / `Eroisa` / `Polydor` proper nouns and the broken-English prompts (`What I do use a hell ?`, the `Kanashimuzo` romaji in `14024F`, `Deads`) against the Japanese source if a JP string table is available;
  - confirm Martha's `zzzCHMarthaGhoul` (`140DF8`) record state (does she rise as a ghoul on the bad path?) — EditorID is `MarthaGhoul` but a live CREA/NPC record dump would confirm;
  - confirm which `DreamNN` property in `qf_zzzchmemoryguide_0242e0b1.psc` maps to quest `140225` (MeQ04), and whether the good path (stage 70 `Stop()`) triggers any guide callback vs. the bad path (stage 140 `qGuide.SetStage(40)`).
