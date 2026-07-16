# Act 1 Side Quest 04 - Eye of Madness

Status: first redo slice. Source-grounded, link-first, no Gemini.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain context or conditions.
- Dialogue-driven quest with optional Meridia cult branch; no `SCEN` staging detected.

## Quest Record

[`0082EA zzzAoMMq04 "Eye of Madness"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:393)

CLI:
- `questdiag Vigilant.esm 0x0082EA`
- `infodiag Vigilant.esm 0x0082EA`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x0082EA`
- EditorID: `zzzAoMMq04`
- Name: `Eye of Madness`
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
| 40 | CompleteQuest | empty |
| 50 | none | empty |
| 255 | ShutDownStage | empty |

Objectives:

| Index | Source | Text |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:394) | Talk to Altano in the Candle Hearth Hall |
| 10 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:395) | Investigate about Mad eye |
| 20 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:396) | Report to Altano |
| 21 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:397) | Take advice from Meridia beliver (Option) |
| 30 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:398) | Kill Balor |
| 40 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:399) | Report to Altano |

Objective targets:
- Objective 0: 1 target with 0 conditions.
- Objective 10: 1 target with 0 conditions.
- Objective 20: 1 target with 0 conditions.
- Objective 21: 1 target (Meridia cult contact) with conditions.
- Objective 30: 1 target (Balor) with 0 conditions.
- Objective 40: 1 target with 0 conditions.
- Current CLI output does not print target cell/ref details; this needs a deeper QUST target dump if location targeting matters.

## Alias / Staging Backbone

No custom `SCEN` records detected by `infodiag`. Quest progression is dialogue-driven with a stage-gated branching point at stage 20 (Stendarr vs. Meridia paths).

Host quest:
- `0082EA zzzAoMMq04` "Eye of Madness"

Dialogue aliases from `infodiag`:
- Alias `#0`: expected to be `Altano` (main quest-giver, objectives 0, 20, 40).
- Alias `#1`: expected to be `Balor` the target NPC (objective 30).
- Alias `#2`: expected to be a Meridia cult contact (objective 21, optional branch).

(inference: alias roles inferred from dialogue conditions `GetIsAliasRef` indices; no explicit alias dump available from CLI)

## NPC Records

Main target NPC:
- [`0012D5 zzzAoMm04Thief` - Balor (Thief)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv) — the afflicted man with the mad eye, quest target at objective 30.
- [`0B161E zzzCHBalor` - Balor (secondary record)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv) — may be the Coldharbour Act 4 manifestation or a duplicate record.

## Custom Dialogue Branches

### Branch 1: Quest Opener — "You seem to contemplate...what happened?"

TOPIC `0x00934B zzAoMMq04B1Mission4`

Condition pattern:
- `GetStage < 10`: fires before player advances past the initial conversation.
- `GetIsAliasRef alias #0` (Altano).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x00934B zzAoMMq04B1Mission4` | `0x00934C` | none | `GetStage < 10`; `GetIsAliasRef alias #0` | Prompt: [`"You seem to contemplate...what happened?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:119) Response (Neutral): [`"I heard a strange rumor from gurads. There is a baleful man who has mad eye in Kynesgrove"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:120) Response (Neutral): [`"For Stendarr, I can not overlook. Istead of me, check whether the rumor was true or not."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:121) |

VMAD Fragment:
- (inference: likely sets stage 10+ to advance quest)

### Branch 2: Investigation — Balor Interrogation

TOPIC `0x00934E zzAoMMq04B2MadEye`

Condition pattern:
- `GetStage >= 10 && < 20`: investigation phase.
- `GetIsAliasRef alias #1` (Balor).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x00934E zzAoMMq04B2MadEye` | `0x00934F` | none | `GetStage >= 10 && < 20`; `GetIsAliasRef alias #1` | Prompt: [`"I heard you have mad eye. Is that true?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:123) Response (Tired): [`"Yes yes yes, so....what? I am very tired....leave me alone..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:124) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x009351 zzAoMMq04B3HowGet` | `0x009352` | none | `GetIsAliasRef alias #1` | Prompt: [`"Tell me about mad eye"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:126) Response: [`"I lost a bet to a woman. Then, she scooped out my right eye and embed a jewelry.."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:127) Response: [`"After that...people who see go mad...I am tired..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:128) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x009353 zzAoMMq04AboutWoman` | `0x009354` | none | `GetIsAliasRef alias #1` | Prompt: [`"Do you remember the woman?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:130) Response (Drunk): [`"I don't remember because I was drunken...I remember!!She has a sexy hip..haha.."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:131) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x009355 zzAoMMq04GiveMercy` | `0x009356` | none | `GetIsAliasRef alias #1` | Prompt: [`"Do you need the mercy of ..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:133) Response: [`"No, thank you. I don't want to die. Mad eye can be prevented by this bandage."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:134) Response: [`"I don't use this power unless fools attacks me. Do you understand? leave me alone..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:135) |

### Branch 3: Report Back to Altano — Stendarr Path

TOPIC `0x009358 zzAoMMq04B4RumorTrueTopic`

Condition pattern:
- `GetStage >= 20 && < 30`: report phase.
- `GetIsAliasRef alias #0` (Altano).
- Trigger stage 30 on specific response (kill Balor).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x009358 zzAoMMq04B4RumorTrueTopic` | `0x009359` | none | `GetStage >= 20 && < 30`; `GetIsAliasRef alias #0` | Prompt: [`"Rumor is true. but Balor is not hostil."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:137) Response: [`"The man, Balor must be sent to Stendarr."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:138) Response: [`"If Balor yield to the power, can you anticipate what will happen? Many people in Skyrim will be sufferd."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:139) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x00935A zzAoMMq04B4Yes` | `0x00935B` | none | `GetIsAliasRef alias #0` | Prompt: [`"OK,I will kill him under the name of Stendarr."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:141) Response: [`"Good, the mercy of Stendarr is not compassion"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:142) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x00935C zzAoMMq04B4No` | `0x00935D` | none | `GetIsAliasRef alias #0` | Prompt: [`"I can not..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:144) Response: [`"You must do. If not, you are not vigilant of Stendarr."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:145) |

VMAD Fragments:
- (inference: B4Yes and B4No choices likely branch to stage 30 SetStage on player response)

### Branch 4: Kill / Mercy Path — Balor Final Confrontation

TOPIC `0x00935F zzAoMMq04B5KillBalorTopic`

Condition pattern:
- `GetStage >= 30`: Balor final phase.
- `GetIsAliasRef alias #1` (Balor).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x00935F zzAoMMq04B5KillBalorTopic` | `0x009360` | none | `GetStage >= 30`; `GetIsAliasRef alias #1` | Response (Resignation): [`"Why don't you leave me alone?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:148) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x009361 zzAoMMq04B5MustDie` | `0x009362` | none | `GetIsAliasRef alias #1` | Prompt: [`"You must die, Stenndarr waiting for you"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:150) Response: [`"it is your answer...then you and I...must do one thing..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:151) |

### Branch 5: Quest Completion — Report Balor's Death

TOPIC `0x009364 zzAoMMq04B6Mission4Comp`

Condition pattern:
- `GetStage >= 40`: completion phase.
- `GetItemCount > 0` on Player for a possible Balor item (if game-state tracks it).
- `GetIsAliasRef alias #0` (Altano).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x009364 zzAoMMq04B6Mission4Comp` | `0x009365` | none | `GetStage >= 40`; `GetIsAliasRef alias #0` | Prompt: [`"I killed Balor..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:153) Response: [`"Don't let it get to you. so...have a drink?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:154) |

VMAD Fragment:
- (inference: likely completes quest or transitions to Stage 50)

## Optional Meridia Cult Branch

This branch appears as quest objective 21 "Take advice from Meridia believer (Option)". It offers an alternative approach to Balor's curse via Meridia worship and light magic, diverging from the Stendarr mercy-through-death path.

### Cult Topics: Hello / Goodbye

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA978 zzzAoMMq04Hello` | `0x4CA979` | none | none | [`"Do you believe  Meridia? I alway believe the brilliance of her."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2986) [`"Oh, Meridia. Wonderful brilliance."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2987) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA97B zzzAoMMq04GoodBye` | `0x4CA97C` | none | none | [`"Meridia's light with you"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2990) |

### Cult Branch 01: Recruitment & Persuasion

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA97F zzzAoMMq04CultB01T01` | `0x4CA980` | none | none | Prompt: [`"Sorry. I'm in the service of Stendarr now."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2992) Response: [`"Do not serve Stendaar. Justice has been blind since ancient times, a word that has been overused and is now worthless."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2993) Response: [`"Then trust in Meridia. Her light will be a spark that will illuminate your path."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2994) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA981 zzzAoMMq04CultB01T02` | `0x4CA982` | none | none | Prompt: [`"Oh, come on, you evil bastard. You want to get dimed, hmm?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2996) Response: [`"The pagans are the eight divines. And the pagans are those of you who worship them. Make no mistake about it."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2997) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA983 zzzAoMMq04CultB01T03` | `0x4CA984` | none | none | Prompt: [`"That's Lady Meridia for you."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2999) Response: [`"I know, I know. You seem very wise. You should be more diligent."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3000) |

### Cult Branch 02: Balor's Curse Solution via Light Magic

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA986 zzzAoMMq04CultB02T01` | `0x4CA987` | none | none | Prompt: [`"What do you think about the man with the mad eyes?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3002) Response: [`"It was a curse that if you looked into those eyes, you would go insane. Did you know that light is involved in seeing?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3003) Response: [`"Yes, it is the light of Meridia. With the worship of the goddess and more light, it should be easy to break the curse."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3004) Response: [`"I can help you if you want. That is, if you're willing to trust Meridia."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3005) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA988 zzzAoMMq04CultB02T02` | `0x4CA989` | none | none | Prompt: [`"Stop sales talk. I won't be so easy to get on board with"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3007) Response: [`"Hmm, but are you sure? Pride and morality will not break the curse, and will not save him."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3008) Response: [`"Now it's time to believe in Meridia. You have now found the right faith."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3009) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA98A zzzAoMMq04CultB02T03` | `0x4CA98B` | none | none | Prompt: [`"I'll believe Meridia (frustrated)"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3011) Response: [`"It's not very heartfelt, but okay. I will lend you the power of Meridia."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3012) Response: [`"There are two wands. A strong light and a weak light. Choose whichever you prefer."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3013) |

### Cult Branch 03: Light Wand Choice

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA98C zzzAoMMq04CultB02T04` | `0x4CA98D` | none | none | Prompt: [`"I need a strong light."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3015) Response: [`"Now, take it. Let the light of Meridia shine upon this world."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3016) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA98E zzzAoMMq04CultB02T05` | `0x4CA98F` | none | none | Prompt: [`"I need a weak light."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3018) Response: [`"You're humble. That's very Meridian. Go ahead, take it."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3019) |

### Cult Branch 04: Safety / Outcome Questions

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA991 zzzAoMMq04CultB03T01` | `0x4CA992` | none | none | Prompt: [`"Is it safe to shine this light on people?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3021) Response: [`"The important thing is to believe, and more importantly, to forgive. Come, let us worship Meridia together."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3022) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA993 zzzAoMMq04CultB03T02` | `0x4CA994` | none | none | Prompt: [`"That's not an answer for my question"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3024) Response: [`"If anything goes wrong, it's because he didn't have enough faith. I have no responsibility for that."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3025) |

### Cult Branch 05: Balor's Outcome Post-Death

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA996 zzzAoMMq04CultB04T01` | `0x4CA997` | none | none | Prompt: [`"Balor is dead. What's happened?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3027) Response: [`"He probably didn't believe in Meridia and her brilliance. Therefore, his body was burned to the ground."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3028) Response: [`"If he had believed in Meridia, none of this would have happened. It was all the fault of the eight divines who seduced mortals."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3029) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA998 zzzAoMMq04CultB04T02` | `0x4CA999` | none | none | Prompt: [`"I'm not a Meridia believer anymore."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3031) Response: [`"Once you're in, you can't get out. You have to give up. You're already on the list of followers in the Colored Room."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3032) |

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4CA99A zzzAoMMq04CultB04T03` | `0x4CA99B` | none | none | Prompt: [`"Oh, damn eight divines!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3034) Response: [`"Yes, that's the spirit. It will be our way of showing him that we can use this failure to our advantage tomorrow."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3035) |

## Reconstruction Notes

Source-grounded:
- This quest is represented by [`0082EA zzzAoMMq04`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:393) with the core objective chain: investigate Balor's mad eye curse (objective 10), then kill him (objective 30).
- The quest splits at stage 20 / objective 20: player can report to Altano (Stendarr mercy path, default) or seek a Meridia cult contact for an alternative light-magic solution (objective 21, optional).
- The Meridia cult branch topics (`4CA978-4CA99B`) are sourced from dialogue.md lines 2985–3035 and represent a parallel intervention narrative: persuading the player to use Meridia's light wands to "cure" Balor instead of killing him.
- If Balor dies (from either Stendarr mandate or Meridia light overexposure), the cult NPC remarks on his death (`4CA996`) and may lock the player into Meridia's faction (`4CA998`).
- No custom `SCEN` records recorded by `infodiag`, confirming this is a dialogue-only quest with no staged cutscenes.

Stage progression inference:
- Stage 0–10: Initial quest acceptance (B1Mission4 topic).
- Stage 10–20: Investigation phase (B2MadEye, B3HowGet, AboutWoman, GiveMercy topics with Balor).
- Stage 20–30: Decision point (B4RumorTrueTopic, B4Yes, B4No; may branch to Meridia path or stay Stendarr).
- Stage 30–40: Kill Balor (B5KillBalorTopic, B5MustDie conditions firing).
- Stage 40+: Completion (B6Mission4Comp topic).
- Stage 50 (inference): Post-completion or transition to next quest.

Translation notes:
- `"Stenndarr"` in original dialogue is likely a typo for `Stendarr` (the Vigil's divine patron).
- `"surged"` or `"sufferd"` in [`009358`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:139) reflects the source's non-native English phrasing; preserved literally.
- Meridia cult dialogue is substantially less polished than Stendarr branch, suggesting it may be a player-written or community-contributed subsystem.
- `"Colored Room"` in [`4CA998`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3032) is an Oblivion lore reference (Sheogorath's realm); likely metaphorical for Meridia's faction registry.

Open verification:
- Inspect QUST alias definitions directly if a richer alias dump is available (objective 21's Meridia cult contact FormID).
- Inspect VMAD fragments on B4Yes, B4No, B1Mission4, B6Mission4Comp if source/decompile path exists; these likely control stage advancement and branch routing.
- Determine whether light wand items (strong/weak) are actual in-game MISC/WEAP records or inventory flags set via Papyrus script.
- Verify Balor NPC record 0x0012D5 vs. 0x0B161E: confirm which is the Act 1 target and which (if any) is Act 4 related.
- Investigate whether the "light overexposure" mentioned in cult dialogue (obj 21) has in-game death script mechanics or is pure narrative flavor.
- Cross-check objective 21 ("Take advice from Meridia believer") trigger conditions: does it require a specific dialogue choice, or is it stage-gated?
