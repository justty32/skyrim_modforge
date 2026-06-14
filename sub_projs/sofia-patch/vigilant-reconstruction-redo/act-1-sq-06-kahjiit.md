# Act 1 Quest 06 - Also sprach Kahjiit

Status: first redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue or stage-gating.
- `SCEN` staging comes from CLI diagnostics (none found for this quest).

## Quest Record

[`009E68 zzzAoMMq06 "Also sprach Kahjiit"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:69)

CLI:
- `questdiag Vigilant.esm 0x009E68`
- `infodiag Vigilant.esm 0x009E68`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x009E68`
- EditorID: `zzzAoMMq06`
- Name: `Also sprach Kahjiit`
- Flags: `RunOnce`
- Priority: `90`
- Type: `SideQuest`
- Filter: `AoM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 10 | none | empty |
| 11 | none | empty |
| 20 | none | empty |
| 21 | none | empty |
| 22 | none | empty |
| 25 | none | empty |
| 30 | none | empty |
| 35 | none | empty |
| 40 | none | empty |
| 50 | none | empty |
| 60 | none | empty |
| 65 | none | empty |
| 70 | none | empty |
| 79 | none | empty |
| 80 | none | empty |
| 90 | CompleteQuest | "Thank you. Jo'vanni thank you very much." |
| 255 | ShutDownStage | empty |
| 999 | FailQuest | empty |
| 9999 | CompleteQuest | empty |

Objectives:

| Index | Source | Translation |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:71) | Talk to Altano |
| 10 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:72) | Meet Altano in the Ragged Flagon |
| 20 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:73) | Find Jo'vanni |
| 21 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:74) | Take advice from Meridia believer (Option) |
| 25 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:75) | You kill me, Jo'vanni? Why? Jo'vanni just want to meet Campaner'Ra!! Why!? |
| 50 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:76) | It is in me, Jo'vanni!! Get out it from Jo'vanni, Please!! |
| 60 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:77) | Retrieve Campaner'Ra from Mar'so instead of Jo'vanni!! Please, grant Jo'vanni's last request!! |
| 70 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:78) | Jo'vanni can not wait!! Hurry Up!! Jo'vanni want to meet Campaner'Ra ASAP |
| 80 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:79) | Report to Altano |

Objective targets:
- 9 objectives in ESM, each with 1 target, no explicit conditions listed by questdiag.
- Exact target refs require deeper QUST target dump if target locations matter.

## Alias / Staging Backbone

The quest defines at least 17 aliases (indices 0–16, inferred from infodiag condition references `GetIsAliasRef`).

Host quest:
- [`009E68 zzzAoMMq06`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:69)

Alias mapping (inferred from `infodiag` alias conditions):

| Alias | Role | Inferred fill type |
|---:|---|---|
| 0 | Quest-partner (Altano) | forcedRef to NPC alias |
| 3 | Jo'vanni (crazy Khajiit) | forcedRef or uniqueActor |
| 4 | Mar'so (Khajiit, possesses Campaner'Ra) | forcedRef or uniqueActor |
| 5 | Campaner'Ra (NPC / memory) | forcedRef or uniqueActor |
| 16 | Meridia-cult member (dialogue partner) | forcedRef to NPC |

Inference:
- Alias `#0` (Altano) opens all mission branches via `GetIsAliasRef == 1` check.
- Alias `#3` (Jo'vanni) is stage-gated at stages 20–25 (finding + talking to crazy Khajiit).
- Alias `#4` (Mar'so) and `#5` (Campaner'Ra memory) are stage-gated at stages 30–60 (memory / domestic scene).
- Alias `#16` (Meridia cult) opens cult-branch topics and dialogue.
- No explicit SCEN records are listed in infodiag output; dialogue-only quest.

## Main Quest Branch: Ratway Daedra Conjuring

Opening topic sequence, stages 0–25, partner dialogue with Altano.

### `00A3D6 zzAoMMq06B1AboutMission6` (Topic/Custom)

Opened by: `GetStage LessThan 10` + `GetIsAliasRef == 1` (alias #0, Altano)

Prompt: [About Ratway](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:221)

| Response | Emotion | Response text |
|---|---|---|
| 1 | Happy | [Ratway...I have Friends in Ragged Flagon. I will get information from them.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:222) |
| 2 | Happy | [I will go forward. If you are ready, come on.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:223) |

VMAD: `AoM06_TIF__0100A3D7.Fragment_0` (OnEnd)

Inference: INFO 0x00A3D7 likely advances stage to 10 at fragment end.

### `00A3D9 zzAoMMq06B2AboutRitual` (Topic/Custom)

Opened by: `GetStage >= 10` and `GetStage < 20` + `GetInCell == 1` (Skyrim.esm 0x016BCF, Ratway) + `GetIsAliasRef == 1` (alias #0, Altano)

Prompt: [What did you discover about Daedra conjuring in Ratway?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:225)

| Response | Emotion |
|---|---|
| 1 | Disgust |

Response text: [I heard that Kajiit called Jo'vanni summon Daedra. You examine that Khajiit, I will search Daedra.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:226)

VMAD: `AoM06_TIF__0100A3DA.Fragment_0` (OnEnd)

Inference: Advances stage from ~10–20 to stage 20 (finding Jo'vanni).

## Jo'vanni Encounter: Crazy Khajiit Branch

Stages 20–25, dialogue with Jo'vanni (alias #3, stage-gated).

### `00A3DC zzAoMMq06B3Crazycat` (Topic/Custom)

Opened by: `GetStage >= 20` and `GetStage < 25` + `GetIsAliasRef == 1` (alias #3, Jo'vanni)

Prompt: (none listed; greeting)

| Response | Emotion |
|---|---|
| 1 | Puzzled |

Response text: [Jo'vanni is looking for Campaner'Ra. My Prescious Campaner'Ra!! Where are you!?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:229)

Inference: Opening monologue of Jo'vanni, no stage advance.

### `00A3DE zzAoMMq06B3WhoWoman` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #3)

Prompt: [Who is golden woman?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:231)

| Response | Emotion |
|---|---|
| 1 | Puzzled |
| 2 | Happy |

Response texts:
- [Jo'vanni noticed....Jo'vanni is very smart. The liver of triangular rat is not good...Jo'vanni noticed!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:232)
- [Your liver....Septim by your liver !! Jo'vanni say like Jo'vanni said!! Campaner'Ra will also say!!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:233)

VMAD: `AoM06_TIF__0100A3DF.Fragment_0` (OnEnd)

Inference: Player choice determines branch (likely separate outcomes later).

### `00A3E0 zzAoMMq06B3CarzyTalk` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #3)

Prompt: [You summoned Daedra, is that true?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:235)

| Response | Emotion |
|---|---|
| 1 | Happy |
| 2 | Sad |

Response texts:
- [Of course!!Jo'vaannin knows becouse Jo'vanni septimed! Septimed By Round Skooma and a liver of triangular rat!!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:236)
- [But..but..Nothing is come!! Jo'vanni septimed as golden woman tell me, Jo'vanni!! Why?Jo'vanni?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:237)

Inference: Exposition of Jo'vanni's obsession with Daedra summoning and Campaner'Ra. No stage advance; dialogue-only.

Translation notes:
- "Liver of triangular rat" is unclear; may refer to skooma ingredients or a symbolic insult. Kept literal.
- "Septimed" appears to be a neologism (from Septim) meaning "enchanted" or "imbued with power."

## Memory / Domestic Scene Branch

Stages 30–60, dialogue with Mar'so (alias #4) and Campaner'Ra (alias #5) in a domestic setting (likely a memory sequence).

### `00A3E3 zzAoMMq06B4Memory` (Topic/Custom)

Opened by: Stage-gated conditions (30, 35, 40, 60); `GetIsAliasRef == 1` (alias #5, Campaner'Ra, OR alias #4, Mar'so)

Prompt: (none listed; greeting/ambient dialogue)

| INFO | Stage | Conditions | Response | Emotion |
|---|---|---|---|---|
| 0x00A3E4 | 30 | `GetIsAliasRef == 1` alias #5 | [Jo'vanni! Wake up, Jo'vanni! It is morning!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:240) | Anger |
| 0x00A3E9 | — | `GetSitting NotEqualTo 3` + alias #5 | [Sit down, Jo'vanni. A stand-up meal is bad manner.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:241) | Anger |
| 0x00A3EA | 35 | `GetIsAliasRef == 1` alias #5 | [This is self confident soup today. you will be encahnted.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:242) | Happy |
| 0x00A3F1 | 40 | `GetIsAliasRef == 1` alias #4 | [Beautiful pelt. Very beautiful pelt. very...very....very...](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:243) | Happy |
| 0x00A3F8 | 60 | `GetIsAliasRef == 1` alias #4 | [Campaner'Ra is warm. Mar'so is happy. Very happy.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:244) | Happy |

Inference:
- Stages 30–35: Campaner'Ra wakes Jo'vanni, offers breakfast.
- Stage 40–60: Mar'so and Campaner'Ra domestic intimacy; Mar'so becoming possessive over Campaner'Ra's pelt/skin.
- No explicit `SCEN` record found, so this is ambient topic dialogue without a scene backbone.
- Flags `GetSitting` suggest furniture-gated action during stage ~35.

### `00A3E5 zzAoMMq06B4WakeUP` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #5)

Prompt: [What...? Campaner'Ra?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:246)

| Response | Emotion |
|---|---|
| 1 | Happy |

Response text: [Wake up! Jo'vanni! Breakfast is ready.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:247)

### `00A3E7 zzAoMMq06B4GotIt` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #5)

Prompt: [Jo'vanni got it.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:249)

Flags: `Goodbye`

| Response | Emotion |
|---|---|
| 1 | Happy |

Response text: [Today, I made tomato soup you like. Do go ahead with your soup before it gets cold.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:250)

VMAD: `AoM06_TIF__0100A3E8.Fragment_0` (OnEnd)

Inference: Exits dialogue loop at this point; likely advances stage.

### `00A3EB zzAoMMq06B4Skoom` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #5)

Prompt: [Where is my skooma? Campaner'Ra?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:252)

Priority: 55 (higher than default)

| Response | Emotion |
|---|---|
| 1 | Disgust |

Response text: [Are you half asleep? You promised me to stop skooma?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:253)

### `00A3ED zzAoMMq06B4kidding` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #5)

Prompt: [Just kidding, Campaner'Ra.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:255)

Flags: `Goodbye`

| Response | Emotion |
|---|---|
| 1 | Happy |

Response text: [Anymore!Soup is getting cold.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:256)

VMAD: `AoM06_TIF__0100A3EE.Fragment_0` (OnEnd)

### `00A3F2 zzAoMMq06B4Skin01` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #4, Mar'so)

Prompt: [Mar'so...the pelt of what...?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:258)

Flags: `WalkAway`

Priority: 55

| Response | Emotion |
|---|---|
| 1 | Happy |

Response text: [This is Campaner'Ra...My precious Campaner'Ra. Mar'so and Campaner'Ra become one soon.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:259)

Inference: Mar'so possessively refers to Campaner'Ra's pelt/skin. "Become one" suggests an intimate or transformative act (possibly skinning, or metaphorical union).

### `00A3F4 zzAoMMq06B4Skin02` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #4)

Prompt: [Why....Mar'so...Why!?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:261)

Flags: `WalkAway`

Priority: 54

| Response | Emotion |
|---|---|
| 1 | Sad |

Response text: [Campaner'Ra won't look Mar'so. but, Mar'so wants to be with Campaner'Ra.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:262)

Inference: Campaner'Ra (the character) rejects Mar'so's advances.

### `00A3F6 zzAoMMq06B4Skin03` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #4)

Prompt: [Jo'vanni never excuse you, Mar'so.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:264)

Flags: `Goodbye`

| Response | Emotion |
|---|---|
| 1 | Sad |
| 2 | Happy |

Response texts:
- [Jelaousy? Jo'vanni? Envy is ugly....Mar'so was also ugly...But now, Mar'so is not because Campaner'Ra with Mar'so.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:265)
- [Goodbye, Jo'vanni. Mar'so and Campaner'Ra set off on our journey. With Campaner'Ra, Mar'so is not cold in winter Skyrim.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:266)

VMAD: `AoM06_TIF__0100A3F7.Fragment_0` (OnEnd)

Inference: Ends the memory sequence; Mar'so leaves with Campaner'Ra. Player choice may affect stage transition.

### `00A3F9 zzAoMMq06B4BackSkin` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #4)

Prompt: [Return Campaner'Ra, Mar'so.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:268)

Flags: `Goodbye`

| Response | Emotion |
|---|---|
| 1 | Sad |
| 2 | Anger |

Response texts:
- [No!No No No!! With difficulty! Campaner'Ra and Mar'so become one!! Why do you disturb us!! Mar'so hate you!!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:269)
- [...smell bad....like envy...from you!!  you smell like Jo'vanni!! I hate Jo'vanni! ](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:270)

VMAD: `AoM06_TIF__0100A3FA.Fragment_0` (OnEnd)

Inference: Alternative ending if player tries to recover Campaner'Ra by force.

## Quest Completion Branch

Stage 80–90, final dialogue with Altano to report back.

### `00A3FC zzAoMMq06B5Mission6Comp` (Topic/Custom)

Opened by: `GetStage == 80` + `GetIsAliasRef == 1` (alias #0, Altano)

Prompt: [The matter about Khajiit is done. Also defeated Daedra.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:272)

Flags: `Goodbye`

| Response | Emotion |
|---|---|
| 1 | Happy |

Response text: [Really? I am very glad to have a excellent partner like you. Return to our base. Summoner may be caught.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:273)

VMAD: `AoM06_TIF__0100A3FD.Fragment_0` (OnEnd)

Inference: Advances stage from 80 to 90 (`CompleteQuest`), ending the quest.

## Ambient / Greeting Topics

### `4CCD81 zzzAoMMq06Hello` (Misc/Hello)

Opened by: `GetIsAliasRef == 1` (alias #16, Meridia-cult dialogue partner) + stage-gated

Prompt: (none; Hello/greeting)

| INFO | Stage cond | Response | Emotion |
|---|---|---|---|
| 0x4CCD82 | `GetStage < 80` | [It's dark here. We need more light. Yes, the light of Meridia!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3041) | Happy |
| 0x4CCD83 | (none) | [What lights up the darkness is the light of Meridia!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3041) | Happy |

Inference: Meridia-cult member's greeting. Repeats obsession with Meridia's light.

### `4CCD84 zzzAoMMq06Goodbye` (Misc/Goodbye)

Opened by: `GetIsAliasRef == 1` (alias #16)

Prompt: (none; Goodbye)

| Response | Emotion |
|---|---|
| 1 | Neutral |

Response text: [Believe Meridia. It is the only salvation.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3043)

## Optional Cult-Member Dialogue Branch

Stages 20–25, conditional dialogue with a Meridia-cult member (alias #16). Stage-gated to occur after finding Jo'vanni but before confronting Mar'so.

### `4CDF78 zzzAoMMq06CultB01T01` (Topic/Custom)

Opened by: `GetStage < 25` + `GetIsAliasRef == 1` (alias #16)

Prompt: [This place is a cesspool. It suits the Meridian faithful.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3044)

| Response | Emotion |
|---|---|
| 1 | Happy |
| 2 | Happy |

Response texts:
- [I don't like the sound of that, but that's exactly what it is! This place is full of people who don't appreciate the light of Meridia.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3045)
- [I must have been guided by Meridia. Give light to these men. ...... Oh, how merciful Meridia is!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3046)

### `4CDF7A zzzAoMMq06CultB01T02` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #16)

Prompt: [You're a tough opponent if you can't handle sarcasm.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3048)

| Response | Emotion |
|---|---|
| 1 | Happy |

Response text: [There are no enemies to those who believe in Meridia. In other words, we are invincible.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3049)

### `4CDF7D zzzAoMMq06CultB02T01` (Topic/Custom)

Opened by: `GetStageDone NotEqualTo 1` (stage 21 not done) + `GetStage >= 20` + `GetStage < 25` + `GetIsAliasRef == 1` (alias #16)

Prompt: [Can you think of anything that could have summoned Daedra here?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3051)

| Response | Emotion |
|---|---|
| 1 | Sad |
| 2 | Happy |

Response texts:
- [None. Not even close. I'm sorry I can't help you. I'm sorry.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3052)
- [But I will help you, my friend, if you will say a few words of Hail Meridia.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3053)

VMAD: `AoMMq06_TIF__024CDF7E.Fragment_0` (OnBegin)

Inference: Player choice between refusing cult help or accepting cult blessing (stage 21 completion marks this choice).

### `4CDF7F zzzAoMMq06CultB02T02` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #16)

Prompt: [I don't remember being friends with you.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3055)

Flags: `Goodbye`

| Response | Emotion |
|---|---|
| 1 | Neutral |

Response text: [Don't be lonely. For me, everything is my friend. And you, of course.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3056)

### `4CDF81 zzzAoMMq06CultB02T03` (Topic/Custom)

Opened by: `GetStage < 25` + `GetIsAliasRef == 1` (alias #16)

Prompt: [Long live Meridia.(bullshit)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3058)

Flags: `Goodbye`

| Response | Emotion |
|---|---|
| 1 | Happy |

Response text: [Now, take it. In front of Meridia's light, everything is dazzling. Even dreams.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3059)

VMAD: `AoMMq06_TIF__024CDF82.Fragment_0` (OnEnd)

### `4CDF84 zzzAoMMq06CultB03T01` (Topic/Custom)

Opened by: `GetStageDone == 1` (stage 21 done) + `GetStage >= 20` + `GetStage < 25` + `GetIsAliasRef == 1` (alias #16)

Prompt: [Is this light really safe to shine on people?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3061)

| Response | Emotion |
|---|---|
| 1 | Happy |

Response text: [Only Meridia knows that. The important thing is to believe, and more importantly, to forgive.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3062)

Inference: Opened only if stage 21 (cult blessing) has been completed.

## Post-Jo'vanni-Death Cult Topics (Stage 79+)

Topics that open after Jo'vanni's death (stage 79, likely inferred from title or daemon possession outcome).

### `4CDF87 zzzAoMMq06CultB04T01` (Topic/Custom)

Opened by: `GetStageDone == 1` (stage 79 done) + `GetIsAliasRef == 1` (alias #16)

Prompt: [Jo'vanni is dead. What's happened?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3064)

| Response | Emotion |
|---|---|
| 1 | Sad |
| 2 | Happy |

Response texts:
- [A dream is an inner light, and ephemeral. The light is too strong for those who live in dreams. His existence is dazzled along with dreams.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3065)
- [It's sad. But do not be sad. His death will be followed by the next salvation. Now, chant. For the Meridia.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3066)

Inference: Cult's twisted interpretation of Jo'vanni's death—framing it as enlightenment ("dazzled") rather than tragedy.

### `4CDF89 zzzAoMMq06CultB04T02` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #16)

Prompt: [You, come on, man.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3068)

| Response | Emotion |
|---|---|
| 1 | (unspecified) |

Response text: [The thought of saving someone with mortal is unthinkable. It is this conceit that leads to tragedy. You should know that.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3069)

### `4CDF8B zzzAoMMq06CultB04T03` (Topic/Custom)

Opened by: `GetIsAliasRef == 1` (alias #16)

Prompt: [Long live Meridia (frustrated).](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3071)

| Response | Emotion |
|---|---|
| 1 | (unspecified) |

Response text: [M, E, R, I, D, I, A!! Glory to the great Meridia, For the Meridia, For the Meridia!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3072)

Inference: Cult member's chant after Jo'vanni's death.

## Bad-End Branch: Mar'so Suicide

(Referenced in index but not detailed here; see separate slice `act-1-sq-06-badend.md`)

Greeting line from bad-end Hello topic (preview):

[`4CDF8E zzzAoMMq06BadEndHello`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3074):
- [No more interruptions. It's just you and me now, Campaner'Ra.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3075)
- [I'll be here forever, Campanella. Always and forever.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3076)
- [Here in the deep end of the pond, no one can disturb us anymore. Even Jo'vanni wouldn't be able to come here.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3077)
- [Hail Meridia in hard times and sad times, oh, hail Meridia!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3078)

## Related Records

These are associated NPCs / items referenced in quest dialogue but not explicitly listed in `infodiag` output.

NPCs:
- [`0EFC32 zzzCHSummonAltano` - Altano](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:270) (summon variant)
- [`001841 zzzAoMCatMale01` - Jo'vanni](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:859) (crazy Khajiit)
- [`001844 zzzAoMCatFemale01` - Campaner'Ra](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:884) (Khajiit female, Jo'vanni's obsession)

## Reconstruction Notes

Source-grounded:
- This quest is represented by [`009E68 zzzAoMMq06`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:69) with name "Also sprach Kahjiit."
- It contains **no SCEN records**; all staging is dialogue-based, with stage gates controlling topic availability.
- The quest is structured around **three main sub-narratives**:
  1. Ratway investigation (stages 0–25): Player + Altano uncover Daedra summoning; optional Meridia-cult subplot.
  2. Memory sequence (stages 30–60): Jo'vanni's domestic life with Mar'so and Campaner'Ra unfolds; player witnesses/intervenes.
  3. Jo'vanni's fate (stages 65–90): Implied death or possession (stage 79); player reports back to Altano.
- **Cult-alternative path**: Player can accept Meridia-cult blessing (stage 21) which opens different dialogue branches and reframes events.
- **Bad-end marker**: Stage 999 (`FailQuest`) and separate quest record `4CDF8D zzzAoMMq06BadEnd` suggest an alternate ending where Mar'so keeps Campaner'Ra.

Branch polarity (inferred):
- **Main path**: Rescue Campaner'Ra from Mar'so; defeat Jo'vanni's possession or madness; return to Altano (stage 90, `CompleteQuest`).
- **Cult-aligned path**: Blessing from Meridia-cult (stage 21), leading to Jo'vanni's death reinterpreted as enlightenment.
- **Bad-end path** (stage 999, separate quest): Mar'so + Campaner'Ra + Campaner'Ra's skin; player cannot recover her.

Open verification:
- Decompile `AoM06_TIF__*` VMAD fragments (OnEnd scripts) to pin exact stage transitions for each dialogue choice.
- Dump QUST aliases directly to confirm fills: which are forcedRef (specific NPCs) vs uniqueActor (persistent) vs other.
- Verify Jo'vanni's trigger at stage 50 ("It is in me, Jo'vanni!!") — inference: possession by Daedra; confirm via VMAD or quest trigger.
- Verify Mar'so/Campaner'Ra "skin" mechanics: is it a model substitution, an item exchange, or metaphorical in the narrative?
- Inspect locations referenced in objectives (Ratway, Ragged Flagon) and whether aliases place NPCs in those cells or if player must navigate.
- Check if stages 65–79 have explicit log entries or CompleteQuest flags (questdiag output shows stage 79 empty; stage 90 has CompleteQuest).
