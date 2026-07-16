# Act 1 Side Quest 08 - No Mercy

Status: first redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain context or translation issues.
- Scene topics are extracted from dialogue.md; scene phases/actions come from `infodiag` CLI.
- Branch text quality is highly degraded (OCR/translation artifacts); marked explicitly where inference is needed.

## Quest Record

[`00EA8A zzzAoMMq08 "No Mercy"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:132)

CLI:
- `questdiag Vigilant.esm 0x00EA8A`
- `infodiag Vigilant.esm 0x00EA8A`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x00EA8A`
- EditorID: `zzzAoMMq08`
- Name: `No Mercy`
- Flags: `RunOnce`
- Priority: `90`
- Type: `SideQuest`
- Filter: `AoM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 10 | none | empty |
| 15 | none | empty |
| 18 | none | empty |
| 20 | none | empty |
| 30 | CompleteQuest | empty |
| 200 | none | empty |
| 200 | none | 3 conditions |
| 210 | none | empty |
| 220 | none | empty |
| 230 | CompleteQuest | empty |
| 300 | none | empty |
| 310 | CompleteQuest | empty |
| 999 | ShutDownStage | empty |
| 9999 | none | CompleteQuest |

Objectives:

| Index | Source | Text |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:133) | Hunt Witches |
| 200 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:134) | Negotiate with Altano (Option) |
| 210 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:135) | Defeat Altano (Option) |
| 300 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:136) | Defeat Ugly One |

Objective targets:
- Objective 0 (Hunt Witches): 2 targets with 2 conditions each.
- Objective 200 (Negotiate with Altano): 1 target with 0 conditions.
- Objective 210 (Defeat Altano): 1 target with 0 conditions.
- Objective 300 (Defeat Ugly One): 1 target with 0 conditions.
- Current CLI output does not print target cell/ref details; this needs deeper QUST target dump if location targeting matters.

## Alias / Staging Backbone

Host quest:
- `00EA8A zzzAoMMq08` "No Mercy"

Dialogue aliases from `infodiag`:

| Alias | Name (inferred) | Fill |
|---:|---|---|
| 0 | `Lilian` | (witch NPC; alias for dialogue conditions) |
| 4 | `Altano` | (quest giver; closing dialogue partner) |

(inference: aliases 0 and 4 inferred from `GetIsAliasRef` conditions in dialogue; no explicit alias dump available from CLI. Alias 0 handles the witch NPC Lilian; alias 4 is Altano, repeating from sq07.)

## Quest Narrative Backbone

**Stage progression inferred from dialogue conditions:**

- **Stages 0–15**: Arrival and greeting. Player encounters Altano and Lilian at the witch encampment. Initial task: assess the situation (stages 10–15).
- **Stages 18–20**: Dialogue investigation phase. Player gathers intel from Lilian about the witches and the curse on her husband (stage 20).
- **Stage 30**: Completion of "Hunt Witches" objective (flag: `CompleteQuest`). Normal witch-hunt completion path.
- **Stages 200–220**: Alternative path opens (stage-gated by `200 ≤ stage < 230`). Topics like [`0x0423C3 zzzAoMMq08B1NoWitch`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:653) branch here, indicating a divergence where player questions the witches' presence.
- **Stage 210**: Represents a refusal path or continued resistance (objective "Defeat Altano (Option)" at this stage).
- **Stage 230**: `CompleteQuest` — alternative completion path (defeat witches via dialogue refusal or confront Altano).
- **Stage 300**: Late objective ("Defeat Ugly One") — suggests a final enemy encounter beyond Lilian.
- **Stage 310**: `CompleteQuest` — completion of the "Defeat Ugly One" path.
- **Stage 999**: `ShutDownStage` — cleanup.
- **Stage 9999**: Final completion catch-all.

## Scene Topics and Dialogue Branches

Scene topics from `infodiag` are topic records (not formal SCEN records) with cat=Scene. Listed by topic FormID and dialogue cues:

### 0x042937 zzzAoMMq08SceneKill (Scene marker)

Extracted text (1 line):
- [`"you will kill the withes. By yourself..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:674)

Context: Altano's command for the player to execute the witch hunt solo. Marks the acceptance/assignment stage.

## Custom Dialogue Branch: Lilian (Witch NPC alias #0)

Branch:
- [`00EFF0:Vigilant.esm`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:642) (quest branch root; filter=AoM\; SNAM=CUST)

Condition pattern:
- Stage-gated at `GetStage < 200` for most topics (initial encounter).
- Alias #0 condition (`GetIsAliasRef alias #0`) identifies speaker as Lilian.

### 0x00EFF4 zzAoMMq08B1RunAway (Lilian panic)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EFF4 zzAoMMq08B1RunAway`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:642) | `0x00EFF5` | Goodbye | `GetStage < 200`; `GetIsAliasRef alias #0` | Prompt: `"I am a Vigilant of Stendarr. I heard there are witches...."` Response (Fear): `"......!Lilian!!Run! Run away!!"` |

VMAD Fragment:
- `AoM08_TIF__0100EFF5` (triggers `OnEnd` fragment; likely advances stage or triggers combat)

(inference: the prompt is generic Vigilant greeting; response shows Lilian warning someone named Lilian to flee, suggesting either an alter-ego or a child alias that Lilian herself is shouting to.)

### 0x0423BD zzzAoMMq08B1WhatHere (Lilian's occupation)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x0423BD zzzAoMMq08B1WhatHere`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:642) | `0x0423BE` | none | `GetStage < 200`; `GetIsAliasRef alias #0` | Prompt: `"What are you doing here?"` Response (Fear): `"I have a formulation of the drug. Because I make a living by Alchemy .."` |

Context: Lilian explains she is an alchemist, not necessarily a witch herself. Phrase "formulation of the drug" is unclear (OCR artifact); likely meant "drug" as in "potion".

### 0x0423BF zzzAoMMq08B1Alchemy (Lilian's teacher)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x0423BF zzzAoMMq08B1Alchemy`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:645) | `0x0423C0` | none | `GetIsAliasRef alias #0` | Prompt: `"Where did you learn Alchemy?"` Response 1 (Fear): `"from the witch ... but that's ... Gurenmoriru, but I am not a friend of the girls."` Response 2 (Sad): `"In desperate hope ... so .. and want to solve a curse of her husband"` |

Translation notes:
- "Gurenmoriru" is likely a mistranscribed name (OCR artifact); appears to be the witch teacher's name, possibly Garenmormire or similar.
- Second response indicates Lilian learned alchemy from the witch(es) in hopes of breaking a curse on her own husband.

### 0x0423C1 zzzAoMMq08B1GoMove (Lilian's escape offer)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x0423C1 zzzAoMMq08B1GoMove`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:649) | `0x0423C2` | Goodbye | `GetIsAliasRef alias #0` | Prompt: `"I do not mind, here is dangerous now. You had better move the location"` Response 1 (Neutral): `"Goes to show you that you came ... Well. you found ..."` Response 2 (Happy): `"I decided to leave here as soon as possible. Stendhal with you"` |

VMAD Fragment:
- `AoM08_TIF__010423C2` (triggers `OnEnd` fragment; likely advances toward stage 30 or quest completion)

Translation notes:
- "Stendhal" is a mistranscription; should be "Stendarr" (the god).
- Second response shows Lilian accepting an escape, offered as a mercy/humanitarian choice.

## Custom Dialogue Branch: Altano (Quest giver alias #4)

Branch:
- [`00EFF0:Vigilant.esm`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:638) (quest branch root)

Condition pattern:
- Most topics stage-gated at `GetStage < 200` or `200 ≤ stage < 210` for escalation.
- Alias #4 condition identifies speaker as Altano.

### 0x00EFF1 zzAoMMq08B1WitchHunt (Altano's command)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EFF1 zzAoMMq08B1WitchHunt`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:638) | `0x00EFF6` | none | `GetIsAliasRef alias #4` | Response (Anger): `"Witch is a serious threat to peace of Skyrim. Kill them all."` |

Context: Altano's primary directive; no stage condition here, so always available as opening dialogue.

### 0x00EFF3 zzAoMMq08B1Unknown01 (Unknown speaker greeting)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EFF1 (continued)`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:638) | `0x00EFF3` | SayOnce | `GetIsAliasRef alias #0` | Response (Puzzled): `"Who...who are you?Please....leave us alone...?"` |

(inference: alias #0 here suggests this response is from Lilian, not Altano; likely a second INFO under the same topic, alternating speakers.)

### 0x0423BA zzzAoMMq08B1AboutWitch (About the witches)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x0423BA zzzAoMMq08B1AboutWitch`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:638) | `0x0423BB` | Goodbye | `GetStage < 200`; `GetIsAliasRef alias #4` | Prompt: `"Tell me about witches of Ivasted"` Response 1 (Disgust): `"Val Lee is a parent-child witch Homestead. Do not be fooled just because they pretend Alchemist"` Response 2 (Disgust): `"Do not pardon opponent but women and children. If you cut corners, I fall prey to witch"` |

VMAD Fragment: (implicit from INFO)

Translation notes:
- "Val Lee" is the witch homestead location / family name (or possibly a mistranscription).
- Second response: "Do not pardon opponent but women and children" is ambiguous; likely means "spare the women and children, but not the others" or "do not show mercy except to women and children".
- "fall prey to witch" likely means "I will be exposed to witchcraft" or "I will be at their mercy".

### 0x0423C3 zzzAoMMq08B1NoWitch (Player denies witches exist)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x0423C3 zzzAoMMq08B1NoWitch`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:653) | `0x0423C4` | none | `200 ≤ GetStage < 210`; `GetIsAliasRef alias #4` | Prompt: `"There are no witch in Ivasted"` Response 1 (Anger): `"...So? Seems to have been well Marumekoma? you came back blindly?"` Response 2 (Anger): `"Kill! Whether or not! You must kill them!!"` |

Translation notes:
- "Marumekoma" is a mistranscription; unclear referent. Possibly a curse word or a garbled name.
- "came back blindly" likely means "returned without seeing the truth".
- Second response: Altano's escalation into a mandate—kill regardless of mercy.

(inference: This topic branches at stage 200+, indicating the player has questioned or resisted the witch hunt. Altano doubles down with fury.)

### 0x0423C5 zzzAoMMq08B1OkOk (Player acquiesces)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x0423C5 zzzAoMMq08B1OkOk`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:657) | `0x0423C6` | none | `GetIsAliasRef alias #4` | Prompt: `"Ok..."` Response 1 (Sad): `"I'm sorry, I was standing a little ... but you understands"` Response 2 (Happy): `"Whatever possible you MUST be eliminated them"` |

Translation notes:
- First response: "I was standing a little" is garbled; likely "I was overstepping" or "I was out of line".
- Second response: "Whatever possible" may mean "by any means necessary" or "as much as possible".

### 0x0423C7 zzzAoMMq08B1Crazy (Player questions Altano's sanity)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x0423C7 zzzAoMMq08B1Crazy`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:661) | `0x0423C8` | none | `GetIsAliasRef alias #4` | Prompt: `"you A little crazy, Altano ..."` Response 1 (Anger): `"Will ya just do something, such as funny ...! Are you! Can not understand you!"` Response 2 (Anger): `"Listed in our victimization witch! Soon go! But you should now that it is!"` |

Translation notes:
- "such as funny" likely means "don't act funny" or "stop joking around".
- Second response: "Listed in our victimization witch" is unclear; possibly "We are listed among the witches' victims" or "Witches have victimized our people".

### 0x0423C9 zzzAoMMq08B1Wrong (Player morally objects)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x0423C9 zzzAoMMq08B1Wrong`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:665) | `0x0423CA` | none | `GetIsAliasRef alias #4` | Prompt: `"This is wrong..."` Response 1 (Anger): `"The cold just as wrong? What not have been killed so far too late! Now, in this I was told!"` Response 2 (Anger): `"It's the same this time. I hope it'll kill it in the name of Stendhal as the guys that were just said!"` |

Translation notes:
- First response is severely garbled; "The cold just as wrong" is unintelligible. Likely tries to say "Your hesitation is wrong" or similar.
- Second response: "in the name of Stendhal" (mistranscription of Stendarr) suggests religious justification.

### 0x0423CB zzzAoMMq08B1Never (Player refuses categorically)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x0423CB zzzAoMMq08B1Never`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:669) | `0x0423CC` | Goodbye | `GetIsAliasRef alias #4` | Prompt: `"I never do that!!"` Response 1 (Neutral): `"I see that intention ... It looks like hard. I'm sorry. I'm so sorry ..."` Response 2 (Happy): `"Such must change the way a little rough but ..."` |

VMAD Fragment:
- `AoM08_TIF__010423CC` (triggers `OnEnd` fragment; likely advances toward stage 210 or the "Defeat Altano" path)

Translation notes:
- First response: "I see that intention ... It looks like hard" suggests Altano recognizes the player's resolve but finds it difficult.
- Second response: "Such must change the way" is garbled; possibly "We must change our methods" or "There is another way".

(inference: This branch leads to stage 210+ where the player actively opposes Altano, possibly triggering combat or the "Defeat Altano" objective.)

## Related Records

NPCs:
- [`0012D2 (Lilian from extracted dialogue.md:696)` - Lilian ("I am Alchemist. I will become like my mom...")](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:696)
  - Lilian is an alchemist NPC at the witch encampment, seemingly a daughter figure to the primary witch.
  - No dedicated NPC record lookup in game-data yet; extracted text only.
- [`000D66 zzzAoMVigilantElder` - Altano](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1) (carried forward from sq07)

Items:
- (None explicitly flagged as quest-gated in this quest; objective "Defeat Ugly One" suggests an end-boss item drop, but unidentified in current CLI output.)

Locations:
- Ivarstead (witch shack location; mentioned as "Ivarstead" or "Ivasted" in dialogue; canonically "Ivarstead" per Skyrim geography).

## Reconstruction Notes

Source-grounded:
- Quest `00EA8A zzzAoMMq08` continues from sq07, where Jacob sent the player to hunt witches at an Ivarstead shack.
- The quest structure presents a moral choice: normal witch hunt (stage 30 completion), dialogue-based refusal path (stages 200–210, leading to "Defeat Altano" objective), or discovery of a larger threat ("Defeat Ugly One" at stage 300).
- Two primary dialogue branches exist:
  - Lilian (alias #0): a sympathetic alchemist at the witch encampment, offering escape or mercy routes.
  - Altano (alias #4): increasingly fanatic demand for total destruction, with branching responses to player pushback.
- Dialogue text quality is poor (OCR artifacts, mistranscriptions, grammatical errors), suggesting the extraction pipeline had encoding or parsing issues.

Branch polarity (inferred):
- **Mercy path**: Listen to Lilian, consider escape (stage 30 completion, implied good karma).
- **Fanaticism path**: Follow Altano's directive to kill all witches; dialogue branches escalate through stages 200–210.
- **Rebellion path**: Refuse Altano; trigger stage 210+ and "Defeat Altano" objective.
- **Unknown path**: "Defeat Ugly One" at stage 300, suggesting a hidden enemy beyond the witch encampment (possibly the mother/coven leader).

Karma outcome:
- Unclear from current source; depends heavily on which dialogue path is taken and which objective is completed (hunt vs. negotiate vs. defeat Altano vs. defeat Ugly One).

Release state:
- No incomplete fragments detected; all dialogue with VMAD flags likely route to stage advances.
- Lilian's panicked greeting ("Run away!!") and escape offer suggest a peaceful resolution is mechanically supported.

Open verification:
- Inspect aliases directly (alias #0 = Lilian, alias #4 = Altano confirmed via condition patterns; other aliases may exist).
- Inspect SCEN records if formal scene hosting exists for the witch encampment encounter.
- Decompile VMAD fragments `AoM08_TIF__*` to confirm stage routing and branching logic.
- Resolve OCR/translation artifacts in dialogue text (e.g., "Gurenmoriru", "Marumekoma", "Marumekoma", "Stendhal" → Stendarr).
- Identify the "Ugly One" NPC (stage 300 objective) — likely the witch mother/coven leader; may require ESM alias or location cell lookup.
- Verify target locations for objectives 0 (Hunt Witches), 200 (Negotiate), 210 (Defeat Altano), 300 (Defeat Ugly One) via deeper QUST target dump.
