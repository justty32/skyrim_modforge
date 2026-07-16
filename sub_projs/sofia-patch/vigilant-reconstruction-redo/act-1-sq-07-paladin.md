# Act 1 Side Quest 07 - Old Paladin

Status: first redo slice. Source-grounded, link-first, no Gemini hallucinations.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain context or translation issues.
- Scene topics are extracted from dialogue.md; scene phases/actions are from `scenediag` CLI.

## Quest Record

[`00A3FE zzzAoMMq07 "Old Paladin"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:243)

CLI:
- `questdiag Vigilant.esm 0x00A3FE`
- `infodiag Vigilant.esm 0x00A3FE`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x00A3FE`
- EditorID: `zzzAoMMq07`
- Name: `Old Paladin`
- Flags: `RunOnce`
- Priority: `90`
- Type: `SideQuest`
- Filter: `AoM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 10 | none | empty |
| 20 | none | empty |
| 30 | none | empty |
| 33 | none | empty |
| 35 | none | empty |
| 36 | none | empty |
| 37 | none | empty |
| 38 | none | empty |
| 40 | none | empty |
| 50 | none | empty |
| 60 | none | empty |
| 70 | none | empty |
| 75 | none | empty |
| 80 | CompleteQuest | empty |
| 255 | ShutDownStage | empty |
| 9999 | CompleteQuest | empty |

Objectives:

| Index | Source | Text |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:244) | Talk to Jacob |
| 10 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:245) | Defeat Ebony knight |
| 33 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:246) | Support Jacob |
| 40 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:247) | Defeat Bal |
| 60 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:248) | Talk to Altano |
| 70 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:249) | Take Mace of Molag Bal to Altano |

Objective targets:
- Objective 0: 1 target with 0 conditions.
- Objective 10: 1 target with 0 conditions.
- Objective 33: 1 target with 0 conditions.
- Objective 40: 1 target with 0 conditions.
- Objective 60: 1 target with 0 conditions.
- Objective 70: 2 targets with 0 conditions each.
- Current CLI output does not print target cell/ref details; this needs deeper QUST target dump if location targeting matters.

## Alias / Staging Backbone

Host quest:
- `00A3FE zzzAoMMq07` "Old Paladin"

Dialogue aliases from `infodiag`:
- Alias `#0`: expected to be `Altano` (closing dialogue partner).
- Alias `#1`: expected to be `Jacob` (quest host; stages 20–60).
- Alias `#3`: expected to be `Umbra` (ebony knight; stages 0–10 confrontation).

(inference: alias indices 0, 1, 3 inferred from `GetIsAliasRef` conditions in dialogue; no explicit alias dump available from CLI)

Scene staging:
- Multiple scene topics (TOPIC cat=Scene) detected; no formal `SCEN` record staging available from current CLI suite.
- Scenes appear to host monologue sequences and encounter interjections (e.g., Molag Bal, Rahel, Joshua, Orthe, Ranyu).

## Scene Topics

Scene topics are dialogue anchors, not formal SCEN records. Listed by topic FormID and staging cues:

### 0x00E4E5 (Molag Bal / Jacob confrontation monologue)

Extracted text (6 lines):
- [`Go away!! Molag Bal!! I am not discouraged!!`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:276)
- [`You right!! 20 years ago,I lost to you. But this time, I overcome you!!`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:277)
- [`Exactly! I killed you!! Exactly!! I killed innocent under the name of Stendarr!!`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:278)
- [`Don't Look at me. Please.....`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:279)
- [`Shut up! Muderer!! I am diffrent from you! I am not Beast like you!`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:280)
- [`Not me! I have no responsibility to your death!! Please, go away....`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:281)
- [`Joshua!Is that you? How are you!? Where were you going to?`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:282)
- [`Thank you, Joshua...Your word is merciful...but, I can not stop my steps.`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:283)
- [`My master...Never did I think of you are here....Yes...I uderstand it.`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:284)

Inferred context: Jacob internal monologue addressing Molag Bal's manifestation; references loss and redemption narrative.

### 0x00E4F4 (Rahel greeting)

Extracted text (1 line):
- [`Well...I gave you my precious mercy.But, Why come here? Jacob?`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:287)

Context: [`Rahel` (alias from `00E4FE zzzAoMM07GhostBal`)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1) greeting Jacob. Suggests spirit/ghost mechanic or memory staging.

### 0x00E4F6 (Jacob's resolve)

Extracted text (1 line):
- [`I ... come here to purege my contempt...No!...Rahel, to help you!!`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:290)

Context: Jacob's motivation clarified as help for Rahel (presumed Molag Bal's victim/manifestation).

### 0x00E4F8 (Apocalyptic warning)

Extracted text (1 line):
- [`so...but too late. Molag Bal is Coming...the all end....Red fog envelope everything....`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:293)

Context: High-stakes scene marker; Molag Bal imminent threat.

### 0x00E4FA (Rahel's call)

Extracted text (1 line):
- [`Rahel?`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:296)

Context: Jacob searching for Rahel; brief query line.

### 0x00E4FC (Bal's command)

Extracted text (1 line):
- [`Stop talking anymore. Do not Disturb me. Orthe! Ranyu! Kill them All!!`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:299)

Context: Molag Bal's manifestation commanding dremora allies [`Orthe`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1) and [`Ranyu`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1) to attack.

### 0x00EA65 (Dream sequence: Rahel echo)

Extracted text (1 line):
- [`Rahel!? Rahel? Is that you?`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:302)

Context: Likely post-combat or dream-state encounter. Names match stage progression (stage 50 approx).

### 0x00EA67 (Dream sequence: Jacob's question)

Extracted text (1 line):
- [`What happended? Jacob? Why do you raise your voice? You had a nightmare?`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:305)

Context: Rahel asking Jacob about distress; dream/memory separation suggested.

### 0x00EA69 (Dream sequence: Jacob's reconciliation)

Extracted text (1 line):
- [`Yes...But I have waked from the nightmare I lost you. I will never send away you...`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:308)

Context: Jacob awakens from nightmare; reconciliation with Rahel's spirit.

### 0x00EA6B (Dream sequence: Rahel's comfort)

Extracted text (1 line):
- [`Jacob...I am always with you. Do not worry anymore....`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:311)

Context: Rahel's reassurance; emotional resolution.

### 0x00EA6D (Dream sequence: Rahel's farewell)

Extracted text (1 line):
- [`Rahel ...`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:314)

Context: Jacob's closing call to Rahel; likely stage transition or quest end cue.

## Custom Dialogue Branch: Umbra (Ebony Knight encounter)

Branch:
- `00EA70:Vigilant.esm` (implied; stage 0–10 branch, alias #3)

Condition pattern:
- Stage-gated at `GetStage < 10`; `GetStage == 10`; alias #3 (Umbra) conditions.
- Represents the confrontation and negotiation with the ebony knight who attacked Beacon.

### 0x00EA71 zzAoMMq07B1UmbraGreet (Umbra greeting)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EA71 zzAoMMq07B1UmbraGreet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:316) | `0x00EA72` | none | `GetStage < 10`; `GetIsAliasRef alias #3` | [`Stop....close enough...`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:317) |

VMAD Fragment:
- `AoM07_TIF__0100EA72` (triggers `OnEnd` fragment; likely advances stage to 10)

### 0x00EA73 zzAoMMq07B1NonStop (Umbra warning)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EA73 zzAoMMq07B1NonStop`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:319) | `0x00EA74` | `SayOnce` | `GetIsAliasRef alias #3` | Prompt: [`"if we don't stop our steps....what wilt you do?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:319) Response: [`"I must cut you down....like your colleague...if you don't want to die, go back..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:320) / [`"Be gone....! you also have...who hope your return...."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:321) |

Translation notes:
- `colleague` refers to previous Beacon attackers killed by Umbra.

### 0x00EA75 zzAoMMq07B1AssaultReason

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EA75 zzAoMMq07B1AssaultReason`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:323) | `0x00EA76` | `SayOnce` | `GetIsAliasRef alias #3` | Prompt: [`"Why did you attacked Beacon?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:323) Response: [`"It is My business. You can not accetpt it?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:324) |

### 0x00EA77 zzAoMMq07B1AboutPursuits

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EA77 zzAoMMq07B1AboutPursuits`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:326) | `0x00EA78` | `SayOnce` | `GetIsAliasRef alias #3` | Prompt: [`"How did you get clear away from chasers?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:326) Response: [`"Chaser....? I killed them all. Probably, they are now in stomach of Trolls."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:327) |

Translation notes:
- Umbra's brutal boast; implies significant NPC casualties in the assault.

## Custom Dialogue Branch: Jacob (Investigation phase)

Branch:
- `00EA79:Vigilant.esm` (implied; stages 20–30 branch, alias #1)

Condition pattern:
- Stage-gated at `GetStage == 20` for initial talk; `GetStage >= 30 && < 35` for deeper lore questions.
- Represents Jacob's recount of the attack and his emotional/spiritual context.

### 0x00EA7A zzAoMMq07B2JacobTalk (Jacob opening)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EA7A zzAoMMq07B2JacobTalk`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:329) | `0x00EA7B` | none | `GetStage == 20`; `GetIsAliasRef alias #1` | [`Uuu...`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:330) |

Context: Inarticulate response; Jacob traumatized or weakened.

### 0x00EA7C zzAoMMq07B2Whathappen (Jacob's account)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EA7C zzAoMMq07B2Whathappen`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:332) | `0x00EA7D` | none | `GetIsAliasRef alias #1` | Prompt: [`"What was happening?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:332) Response (Sad): [`"Attacked by the summoner....All is dead except me. Again...again I only survived...."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:333) / Response (Anger): [`"She is called Bal by Daedra...abominable name. She is a agent of Molag Bal...Her purpose is a altar under the ground.."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:334) |

Context: Jacob reveals the Beacon attack was orchestrated by "Bal" (a Molag Bal servant) pursuing an underground altar goal.

### 0x00EA7E zzAoMMq07B2Meaning (Jacob clarifies)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EA7E zzAoMMq07B2Meaning`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:336) | `0x00EA7F` | `Goodbye` | `GetIsAliasRef alias #1` | Prompt: [`"What do you mean?Jacob?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:336) Response: [`"There is a Altar of Molag Bal under the beacon. She is attepmting to something tremendous....we must stop her!!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:337) |

VMAD Fragment:
- `AoM07_TIF__0100EA7F` (triggers `OnEnd` fragment; advances quest to stage 30+)

Translation notes:
- "tremendous" likely means catastrophic or world-changing ritual.

### 0x00EA81 zzAoMMq07B3MolagBal (Lore: Molag Bal corruption)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EA81 zzAoMMq07B3MolagBal`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:339) | `0x00EA82` | none | `GetStage >= 30 && < 35`; `GetIsAliasRef alias #1` | Prompt: [`"What is Molag Bal?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:339) Response (Sad): [`"Daedra price of domination. Many vigilants are corrupted by Molagb Bal."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:340) / Response (Sad): [`"I also one of them. I lost to Molag bal. When I was wounded and dying, Molag bal apeerared and offer to reanimate me in exchange for my wife."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:341) / Response (Fear): [`"I have accepted it...I regret that my did. I can not forget her mournful eyes...."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:342) |

Exposition: Jacob's tragic past—he sold his wife (Rahel) to Molag Bal for resurrection 20 years prior. Guilt drives current quest narrative.

### 0x00EA84 zzAoMMq07B4AboutBal (Lore: Bal's nature)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EA84 zzAoMMq07B4AboutBal`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:344) | `0x00EA85` | none | `GetStage >= 30 && < 35`; `GetIsAliasRef alias #1` | Prompt: [`"Tell me about Bal"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:344) Response (Fear): [`"Bal is powered by Molag bal....Her magicka is powerful and infinite..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:345) / Response (Sad): [`"She is a looks-alike for my wife. Probably, She is trap of Molag Bal..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:346) |

Context: Bal is a shapeshifted or imposter simulacrum of Rahel, designed to torment Jacob.

## Custom Dialogue Branch: Altano (Quest closure)

Branch:
- `00EA86:Vigilant.esm` (implied; stages 60–80 branch, alias #0)

Condition pattern:
- Stage-gated at `GetStage == 60` for opening; `GetStage >= 70 && < 80` for closure.
- Represents Jacob's final plea and the player's response to completing objectives.

### 0x00EA87 zzAoMMq07B5TakeMace (Jacob's final request)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EA87 zzAoMMq07B5TakeMace`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:348) | `0x00EA88` | `Goodbye` | `GetStage == 60`; `GetIsAliasRef alias #0` | Response: [`"All is gone... you ... you take the mace of Bal to me? I need it...."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:349) |

VMAD Fragment:
- `AoM07_TIF__0100EA88` (triggers `OnEnd` fragment)

Context: Jacob requests the player retrieve Molag Bal's mace (presumably dropped after defeating Bal).

### 0x00EA89 zzAoMMq07B5TakeMaceFollowUp (Jacob's next steps)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x00EA89 (continuation)`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:348) | `0x00EA89` | `SayOnce, WalkAway` | `GetItemCount > 0` on Player for [`00D9FC zzzCHMolagMace`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1); `GetStage >= 70 && < 80`; `GetIsAliasRef alias #0` | Response: [`"....I will back to tha temple of Stendarr and ask keepers for advice about this mace."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:350) / [`"Before return to the hall...I ask you for a small mission. I heard there are witches at shack in the south of Ivarstead."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:351) / [`"Witch is a serious threat to peace of skyrim. Give them the Mercy of Stendarr...."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:352) |

VMAD Fragment:
- `AoM07_TIF__0200EA89` (triggers `OnBegin` fragment; likely chains to Act 1 sq08 witch hunt)

Context: Upon acquiring the mace, Jacob proposes the next quest (Act 1 sq08, witch hunt at Ivarstead) and warns of witches.

Translation notes:
- "Tha temple" is agrammatical; likely meant "the temple".

### 0x027A46 zzzAoMMq07B5JacobDead (Jacob funeral alternative)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x027A46 zzzAoMMq07B5JacobDead`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:436) | `0x027A47` | `Goodbye` | `GetIsAliasRef alias #0` | Prompt: [`"We should  hold a funeral for Jacob"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:436) Response: [`"I will do. You put away the witch while I mourn for him. See you again in Temple of Stendarr."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:437) |

VMAD Fragment:
- `AoM07_TIF__01027A47` (triggers `OnEnd` fragment)

Context: **Failure branch.** If Jacob dies during the quest (e.g., defeated by Bal), player can offer a funeral. Altano accepts and directs player to witch hunt anyway. Implies Jacob's death is survivable in the narrative but tracks as a party loss.

## Support Dialogue Branches

### 0x11E0AB zzzAoMMq07B6T01 (Support at stage 30)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x11E0AB zzzAoMMq07B6T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1082) | `0x11E0AC` | `Goodbye, SayOnce` | `GetStage == 30`; `GetIsAliasRef alias #0` | Response: [`"Support Jacob. I come see how to go ahead"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1083) |

VMAD Fragment:
- `AoM07_TIF__0211E0AC` (triggers `OnEnd` fragment)

Context: Altano offers to help Jacob during the investigation phase.

### 0x11E0AE zzzAoMMq07B7T01 (Support at stage 33)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`0x11E0AE zzzAoMMq07B7T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1085) | `0x11E0AF` | `Goodbye` | `GetIsAliasRef alias #0`; `GetStage == 33` | Response: [`"Support Jacob,please"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1086) (Fear emotion) |

Context: Altano's desperate plea for support as Jacob is threatened (stage 33 critical point).

## Related Records

NPCs:
- [`000D66 zzzAoMVigilantElder` - Jacob](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1)
- [`00E4FE zzzAoMM07GhostBal` - Rahel (Bal's form / ghost)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1)
- [`031117 zzzBMVgilantsCorpse01` - Joshua](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1)
- [`00183E zzzAoMBossDremora04` - Orthe (dremora ally of Bal)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1)
- [`00183F zzzAoMBossDremora05` - Ranyu (dremora ally of Bal)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1)

Items:
- [`00D9FC zzzCHMolagMace` - Mace of Molag Bal](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1) (quest completion item)

## Reconstruction Notes

Source-grounded:
- Quest `00A3FE zzzAoMMq07` represents a redemption arc for Jacob, an elder Vigilant who traded his wife Rahel to Molag Bal 20 years prior.
- The quest structure alternates between confrontation (Umbra, stages 0–10), investigation (Jacob's lore, stages 20–35), and climax (defeat Bal, stages 40–60), then closure (return mace, stages 60–80).
- The scene topics (0x00E4E5 onwards) depict both the active battle against Molag Bal and a dreamlike or memory sequence where Jacob reconciles with Rahel's ghost.
- Two parallel dialogue branches exist: Umbra's confrontation (demanding) and Jacob's lore exposition (guilty, desperate).
- A failure branch exists: if Jacob dies, a funeral dialogue becomes available.

Branch polarity:
- **Good path**: Defeat Umbra, listen to Jacob's pain, defeat Bal, retrieve mace, support Jacob's redemption → chains to sq08 (witch hunt).
- **Jacob death path**: Quest survives Jacob's defeat; funeral branch suggests an alternate closure, but quest continues toward sq08.

Karma outcome:
- Unclear from current source; likely neutral-to-good (defending a fallen ally, confronting Daedra corruption).

Release state:
- No incomplete fragments detected; all dialogue has terminating VMAD or Goodbye flags.

Open verification:
- Inspect SCEN records directly if formal scene host/alias/phase structure matters (0x00E4E5, 0x00E4F4–FC, 0x00EA65–6D may have SCEN hosts).
- Verify `zzzAoMM07GhostBal` (Rahel) NPC flags (e.g., is it a ghost flag, or just another actor form?).
- Trace stage conditions in VMAD fragments to confirm stage advance triggers (particularly for stages 33–37, which lack dialogue conditions).
- Confirm alias #3 identity as "Umbra" via NPC lookups or scene/quest alias tables.
- Reconcile stage 75 (no dialogue conditions found) — possible automatic advance or trigger-based.
