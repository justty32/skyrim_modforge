# Act 1 Quest 11 - Art of Mercy (Good Ending)

Status: first redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a dialogue condition.
- `SCEN` records come from CLI diagnostics; the extracted `dialogue.md` only preserves scene monologue text, not phase/action details.

## Quest Record

[`4D0376 zzzAoMMqGoodEnd "Art of Mercy"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600)

CLI:
- `questdiag Vigilant.esm 0x4D0376`
- `infodiag Vigilant.esm 0x4D0376`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x4D0376`
- EditorID: `zzzAoMMqGoodEnd`
- Name: `Art of Mercy`
- Flags: (to be extracted via CLI)
- Priority: (to be extracted via CLI)
- Type: (to be extracted via CLI)
- Filter: `AoM\`

Stages from quest extraction:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 10 | none | empty |
| 20 | none | empty |
| 29 | none | empty |
| 30 | none | empty |
| 110 | CompleteQuest | empty |
| 255 | ShutDownStage | empty |

Objectives:

| Index | Source | Translation |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600) | Talk to Carene |
| 10 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600) | Stop Carene |
| 20 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600) | Talk to Carene |
| 29 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600) | Eliminate Caren (Option) |
| 30 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600) | Go away from here |
| 110 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600) | Report to Thorondir |

Objective targets:
- Current CLI output does not print target refs; deeper QUST target dump needed if location targeting matters.

## Alias / Staging Backbone

Host quest:
- [`4D0376 zzzAoMMqGoodEnd`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600)

Dialogue aliases from `infodiag`:
- Alias (expected): `Carene` (widow/grieving mother, main speaker in this branch).
- Alias (expected): `Thorondir` (Temple priest, final report target).

(inference: alias roles inferred from dialogue conditions and quest objectives; explicit alias dump from CLI needed)

## Scene Records

Two `SCEN` records exist for the Good Ending finale.

### 0x4D039B zzzAoMMqGESceneChild

(inference: scene title from dialogue content; FormID from extracted dialogue.md structure)

Staging:
- Host quest: [`4D0376 zzzAoMMqGoodEnd`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600)
- Actor: (likely Carene's child, based on monologue content)

Monologue from extracted dialogue:
- [`4D039B` scene line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md): 「We did it! I did it, Mom! I beat the bad guys!」

### 0x4D039D zzzAoMMqGESceneCarene

(inference: scene title from dialogue content; FormID from extracted dialogue.md structure)

Staging:
- Host quest: [`4D0376 zzzAoMMqGoodEnd`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600)
- Actor: `Carene` (mother)

Monologue from extracted dialogue:
- [`4D039D` scene line](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md): 「Mom, hey, Mom, ...... answer me, please, ......」

## Custom Dialogue Branch: Carene (Mother) — Confrontation & Redemption

Branch:
- Host topic: [`4D0379 zzzAoMMqGoodEndHello`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1) [Misc/Hello]

Hello opener (stage-gated):
- [`4D0379` Hello line 1](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1): 「I had no idea that you were really here. I knew that story was true.......」
- [`4D0379` Hello line 2](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1): 「Why is my husband dead? You're alive. ...... It was just the beginning. ......」
- [`4D0379` Hello line 3](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1): 「Welcome back. How did it go with the summoner?」
- [`4D0379` Hello line 4](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1): 「This will be the last time I talk to you. Take care of the sanctuary.」

### Branch 1: Quest Opener — "Why are you here?"

TOPIC `0x4D037E zzzAoMMqGEMomB01T01`

Condition pattern:
- (inference: stage-gated at 0–10, speaker `GetIsAliasRef Carene`)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D037E zzzAoMMqGEMomB01T01` | (INFO ID TBD) | none | (stage < 10; speaker) | Prompt: [`"Why are you here?I told you to run."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2) Response (Neutral): [`"I am grateful that you overlook us, my daughter and I. But I can't overlook ...... you."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2) |

### Branch 2: Dialogue Tree — "What's the story?"

TOPIC `0x4D0381 zzzAoMMqGEMomB02T01`

Condition pattern:
- (inference: stage-gated at 10+, `GetIsAliasRef Carene`)

Dialogue structure (multi-response tree):

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D0381 zzzAoMMqGEMomB02T01` | (ID TBD) | none | (stage >= 10; speaker) | Prompt: [`"What's the story?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3) Response: [`"A kind person told me about you. He told me that you had taken my husband, Taranis. ......"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3) |
| `0x4D0383 zzzAoMMqGEMomB02T02` | (ID TBD) | none | (speaker) | Prompt: [`"Kind ...... Didn't it call itself Orlando?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:4) Response: [`"Yes, that may have been the name. But that doesn't matter now, does it?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:4) |

### Branch 3: Dialogue Branches — Player Choice Points (Defense / Mercy / Justice)

Multi-path branching based on player response:

#### Path 3a: Defense Argument

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D0385 zzzAoMMqGEMomB02T03` | (ID TBD) | none | (speaker) | Prompt: [`"It was a legitimate defense. We both had things we couldn't give up."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:5) Response 1: [`"I'm sure you're right. Maybe there was a reason. Still, I can't forgive you for killing Taranis."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:5) Response 2: [`"Take your weapons. I will avenge my husband here and now."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:5) |

#### Path 3b: Mercy Appeal (Orphan Argument)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D0387 zzzAoMMqGEMomB02T04` | (ID TBD) | none | (speaker) | Prompt: [`"Don't you dare take revenge. You're going to make your daughter an orphan."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:6) Response 1: [`"Are you trying to scare me? Do you think you're the only one who won't die? Why does a rogue like you call yourself the Vigil!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:6) Response 2: [`"I'm ...... going to kill you, right here. I'm not going to live in mourning over the murder of my husband!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:6) |

#### Path 3c: Pacifism Appeal

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D0389 zzzAoMMqGEMomB02T05` | (ID TBD) | none | (speaker) | Prompt: [`"I don't want to see any more blood today."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:7) Response 1: [`"I'm sure you are. You've killed so many people in your life, you're tired of looking at them!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:7) Response 2: [`"I'll show you your blood once and for all! Let that be your atonement!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:7) |

#### Path 3d: Justice Doctrine

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D038B zzzAoMMqGEMomB02T06` | (ID TBD) | none | (speaker) | Prompt: [`"Everything the Vigils do is justice. Taranis' death is also undeniable justice."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:8) Response: [`"You crazy son of a bitch! I'll kill you!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:8) |

#### Path 3e: Provocation (Accept Vengeance)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D038D zzzAoMMqGEMomB02T07` | (ID TBD) | none | (speaker) | Prompt: [`"Vengeance...good, come on."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:9) Response: [`"You don't have to tell me what to do! Prepare yourself!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:9) |

### Branch 4: Post-Combat (Carene Defeated / Pacified)

#### Path 4a: Delayed Retribution (Player survives without killing)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D0390 zzzAoMMqGEMomB03T01` | (ID TBD) | none | (speaker; stage >= 20) | Prompt: [`"I'll deal with you anytime. Until you're ready. ......"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:10) Response: [`"Someday ...... someday I will definitely, definitely kill you. ...... I won't forgive you, I won't forgive you. ......"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:10) |

(inference: stage 20 likely marks end of combat; Carene survives, vows future revenge)

#### Path 4b: Moral Argument (Player justifies via doctrine)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D0393 zzzAoMMqGEMomB04T01` | (ID TBD) | none | (speaker; stage >= 20) | Prompt: [`"I don't care if I have to be forgiven to be right. Iget dirty willingly."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:11) Response: [`"I hope you fall into Oblivion without your sanctimonious preaching. ......"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:11) |

#### Path 4c: Final Mercy Offer (Quest End Path — Redemption)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D0396 zzzAoMMqGEMomB05T01` | (ID TBD) | none | (speaker; stage >= 29) | Prompt: [`"this is last assistance for you"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:12) Response: [`"If you're going to do it, ...... get on with it. ...... I'm not going to beg for my life. ......"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:12) |

(inference: stage 29 marks final choice point; response suggests willingness to accept death; triggers good ending path to stage 30)

## Custom Dialogue Branch: Thorondir (Temple Priest) — Report & Closure

Branch:
- (expected INFO on a generic Hello or custom quest topic owned by quest 0x4D0376)

Dialogue addresses the summoner's fate and the quest outcome:

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D03A3 zzzAoMMqGEKeepB01T01` | (ID TBD) | none | (stage >= 30; speaker) | Prompt: [`"The summoner was vanquished by the Stendarr beacon."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:13) Response 1: [`"Well, I'm glad to hear that. You can relax from your journey here in this cathedral for a while."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:13) Response 2: [`"That said, I don't see any sign of Altano. Where is he now?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:13) |

### Thorondir Report Paths (Outcome Variations)

Player outcome reports trigger different priest reactions:

#### Path A: Altano Dead

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D03A5 zzzAoMMqGEKeepB01T02` | (ID TBD) | none | (speaker; Altano death condition?) | Prompt: [`"By the beacon of Stendhal, Altano was martyred."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:14) Response 1: [`"There are many bloody stories in the basement of Stendhal's beacon. Has he become a victim of this?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:14) Response 2: [`"Well, he was quite good, wasn't he? It's a pitty."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:14) |

#### Path B: Altano Corrupted

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D03A7 zzzAoMMqGEKeepB01T03` | (ID TBD) | none | (speaker; Daedra manipulation condition) | Prompt: [`"Altano was manipulated by Daedra, so I had no choice."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:15) Response 1: [`"He must have gotten too close to the altar in the basement. Because Morag Bal will kill people together for fun. ......"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:15) Response 2: [`"I know you have a lot on your mind, but don't get too worked up about it. That's exactly what Morag bal would want you to do."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:15) |

#### Thorondir's Future Plan

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D03A9 zzzAoMMqGEKeepB01T04` | (ID TBD) | none | (speaker) | Prompt: [`"What do you plan to do now?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:16) Response 1: [`"I will go to Stendarr's Beacon to destroy the altar. I've learned the hard way that it's not enough to keep people away."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:16) Response 2: [`"We'll be away from this temple for a while. In the meantime, I'd like you to take care of it. Will you do me a favor?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:16) |

#### Player Hesitation

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D03AB zzzAoMMqGEKeepB01T05` | (ID TBD) | none | (speaker) | Prompt: [`"I'm not strong enough."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:17) Response: [`"Don't be modest. Just escaping Morag Bal's schemes has shown you to be of sufficient caliber."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:17) |

#### Final Task Assignment (Quest End)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x4D03AD zzzAoMMqGEKeepB01T06` | (ID TBD) | none | (speaker; stage >= 110 or end) | Prompt: [`"I'll take care of it."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:18) Response: [`"I entrust you with this horn. It is a token of your protection. Pray to it when you are lost."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:18) |

(inference: reward or capstone dialogue; likely stage 110 marks quest completion)

## Reconstruction Notes

Source-grounded:
- This quest (`0x4D0376 zzzAoMMqGoodEnd`) represents the **Good Ending** path of the Act 1 finale, centered on a **confrontation with Carene** (widow of Taranis, a fallen ally).
- It contains two `SCEN` records (`0x4D039B`, `0x4D039D`) staging dialogue monologues from Carene and her child during the finale.
- The central mechanic is a **dialogue-based choice tree** that determines whether Carene attacks, accepts mercy, or achieves redemption:
  - Path 1: Player provokes / justifies → Carene fights → stage 20 (defeated but survives).
  - Path 2: Player shows mercy / appeals → Carene spares the player → stage 29 (redemption offered).
  - Path 3: Player refuses → stage 30 (Carene accepted her fate) → quest complete via Thorondir report.
- **Thorondir** (Temple priest at Stendarr's Beacon) serves as the **quest closure NPC**, debriefing the player on Altano's fate and future temple defenses.

Branch polarity (Good vs. other endings):
- **Good Ending** = Carene survives, accepts mercy or at least reconciliation; player maintains moral high ground; Thorondir entrusts the player with a sacred horn (reward item).
- (contrast with **Bad Ending** if such a quest variant exists; not yet examined in this slice).

Open verification:
- Extract VMAD scripts / conditions on Carene dialogue topics (many conditions implied but not yet inspected via `infodiag`).
- Inspect SCEN phase/action structure via `scenediag 0x4D039B` and `scenediag 0x4D039D` for staging details (timer, emotion, interruption flags).
- Verify objective targets if location-specific tasks exist (stage 30 "Go away from here" may have target references).
- Inspect the reward item (sacred horn) record if `0x4D03AD` grants a tangible object on quest completion.
- Cross-reference NPC aliases for Carene and Thorondir via deep QUST alias dump (not printed by CLI).
- Verify whether quest paths branch into separate `SCEN` or share the same scene records (child monologue `0x4D039B` appears in both outcomes based on dialogue, but exact trigger conditions unknown).
