# Act 1 Subplot 03 - Legacy of Belharza

Status: first redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines linked back to extracted source files instead of copied in full.
- Quest FormID, EditorID, name, priority verified via ESM + `questdiag`.
- Dialogue topics, conditions, speakers from `infodiag` diagnostic output.
- Scene records: not present (quest has no SCEN records according to CLI check).

## Quest Record

[`51EAC1 zzzAoMSubQ03 "Legacy of Belharza"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:284)

CLI:
- `questdiag Vigilant.esm 0x51EAC1`
- `infodiag Vigilant.esm 0x51EAC1`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x51EAC1`
- EditorID: `zzzAoMSubQ03`
- Name: `Legacy of Belharza`
- Flags: `RunOnce`
- Priority: `90`
- Type: `SideQuest`
- Filter: `AoM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | StartUpStage | empty |
| 1 | none | empty |
| 5 | none | empty |
| 10 | none | empty |
| 20 | none | empty |
| 30 | CompleteQuest | empty |
| 40 | none | empty |
| 50 | none | empty |
| 51 | none | empty |
| 60 | none | empty |
| 255 | ShutDownStage | empty |

Objectives:

| Index | Source | Translation |
|---:|---|---|
| 1 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:285) | Talk to `<Alias=Mntr>` |
| 10 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:286) | Meet with Chief of Minotaur |

Objective targets:
- Objective 1 targets 1 ref; objective 10 targets 2 refs.
- Target details not printed by CLI; these are likely alias fill conditions in ESM (Mntr = Mordog alias, Chief = Horbahha alias).

## Alias / Stage Backbone

Host quest:
- [`51EAC1 zzzAoMSubQ03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:284)

Aliases inferred from dialogue conditions and speaker mappings in `infodiag` output:

| Alias | Speaker | Name | Fill (inferred) |
|---:|---|---|---|
| 0 | `51EAA8` | `Mordog the Man-Bull` (`zzzCHMntrFollower`) | Speaker in greeting and early dialogue |
| 1 | `51D895` | `Horbahha the Chief` (`zzzCHMntrLeader`) | Speaker in chief dialogue branch |

Inference:
- Alias 0 (Mordog) greets the player early (stage < 5), invites to village (stage 5), waits (stage 40), and offers to join.
- Alias 1 (Horbahha) appears at stage 20 in chief dialogue branch, speaking about Emperor Belharza's will.
- Both aliases tied to quest via `GetIsAliasRef` conditions in dialogue INFOs.

## Dialogue Branches

### Hello Greeting Topic

Topic: [`51EAC4 zzzAoMSubQ03Hello`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3448) (Misc/Hello category, priority 50)

| INFO FormID | Condition(s) | Translation | Flags |
|---|---|---|---|
| [`51EAC5`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3449) | `GetStage < 5`; `GetIsAliasRef alias #0` | "We have been waiting for you, old friend. I am honored to see you in my generation." | none |
| [`51EAC6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3450) | `GetStage == 5`; `GetIsAliasRef alias #0` | "Old friends, will you come to our village?" | none |
| [`51EAC7`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3451) | `GetStage == 10`; `GetIsAliasRef alias #0` | "This is our Minotaur village. The chief is waiting for us in the back." | Goodbye |
| [`51EAF6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3452) | `GetStage == 40`; `GetIsAliasRef alias #0` | "Wait for me, old friend." | none |
| [`51EADD`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3453) | `GetStage == 20`; `GetIsAliasRef alias #1` | "Our old friend, you have come well." | none |

### Mordog Branch 01 - Recognition

Topic: [`51EACA zzzAoMSubQ03MntrB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3455) (prompt="How did you know it was me?")

| INFO FormID | Condition(s) | Translation |
|---|---|---|
| [`51EACB`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3456) | `GetStage <= 5`; `GetIsAliasRef alias #0` | "Our family has inherited the memories bequeathed by Emperor Belharza. There is no mistaking us." |

### Mordog Branch 01 - Bloodline

Topic: [`51EACC zzzAoMSubQ03MntrB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3458) (prompt="You mean you are descended from Belharza?")

| INFO FormID | Condition(s) | Translation |
|---|---|---|
| [`51EACD`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3459) | `GetStage <= 5`; `GetIsAliasRef alias #0` | "No way, my family only served Emperor Belharza. The bloodline of the demigods is still missing." |

### Mordog Branch 02 - Initial Meeting

Topic: [`51EACF zzzAoMSubQ03MntrB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3461) (prompt="What do you want?")

| INFO FormID | Condition(s) | Translation | VMAD |
|---|---|---|---|
| [`51EAD0`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3462) | `GetStage < 5`; `GetIsAliasRef alias #0` | "In accordance with the will of Emperor Belharza, I have something to show you. Will you come to our village?" | OnBegin=`AomSq03_TIF__0251EAD0.Fragment_1`; OnEnd=`AomSq03_TIF__0251EAD0.Fragment_0` |

Inference: VMAD fragments likely manage stage progression or scene triggers on response.

### Mordog Branch 03 - Journey Preparation

Topic: [`51EAD2 zzzAoMSubQ03MntrB03T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3464) (prompt="Let's go.")

| INFO FormID | Condition(s) | Translation | VMAD |
|---|---|---|---|
| [`51EAD3`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3465) | `GetStage == 5`; `GetIsAliasRef alias #0` | "The Minotaur Village is a hidden village. We would like you to close your eyes for a moment." | OnEnd=`AomSq03_TIF__0251EAD3.Fragment_0` |

Flags: Goodbye (ends greeting/dialogue phase).

### Mordog Branch 04 - Delay

Topic: [`51EAD5 zzzAoMSubQ03MntrB04T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3467) (prompt="I can't do it now.")

| INFO FormID | Condition(s) | Translation |
|---|---|---|
| [`51EAD6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3468) | `GetStage == 5`; `GetIsAliasRef alias #0` | "Then I will wait here. Please let us know when you are ready." |

Flags: Goodbye (allows quest stalling).

### Horbahha (Chief) Branch 01 - Inquiry

Topic: [`51EAE0 zzzAoMSubQ03ChiefB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3470) (prompt="What is it you want to show me?")

| INFO FormID | Condition(s) | Translation | Responses |
|---|---|---|---|
| [`51EAE1`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3471) | `GetStage == 20`; `GetIsAliasRef alias #1` | Response 1: "Emperor Belharza has given us the word to show you this village. Unfortunately, we still do not know what his intentions are." Response 2: "Do our old friends have any idea of Emperor Belharza's intentions? I am anxious to know, even though I am the head of a tribe." | none |

### Horbahha Branch 02 - Timidness Comment

Topic: [`51EAE3 zzzAoMSubQ03ChiefB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3474) (prompt="Your timidness reminds me of Emperor Belharza.")

| INFO FormID | Condition(s) | Translation | Emotion |
|---|---|---|---|
| [`51EAE4`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3475) | `GetStage == 20`; `GetIsAliasRef alias #1` | "What an honor to have Emperor Belharza at ....... I am very impressed." | Happy |

Flags: SayOnce (blocks repeat).

Note: Source text contains ellipsis placeholder; likely text cutoff or encoding artifact in original ESM.

### Horbahha Branch 03 - Satisfaction

Topic: [`51EAE5 zzzAoMSubQ03ChiefB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3477) (prompt="Satisfied. Nothing to say.")

| INFO FormID | Condition(s) | Translation | VMAD |
|---|---|---|---|
| [`51EAE6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3478) | `GetStage == 20`; `GetIsAliasRef alias #1` | Response 1: "I am glad you are satisfied. We are pleased to have finally delivered Emperor Belharza's last wishes." Response 2: "Our clan is grateful to you. Without you, we would still be hunted as beasts by the people." | OnEnd=`AomSq03_TIF__0251EAE6.Fragment_0` |

### Mordog Branch 05 - Companion Offer (Stage 40)

Topic: [`51EAF8 zzzAoMSubQ03MntrB05T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3497) (prompt="You still wanted to see me?")

| INFO FormID | Condition(s) | Translation |
|---|---|---|
| [`51EAF9`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3498) | `GetStage == 40`; `GetIsAliasRef alias #0` | Response 1: "I wonder if I could accompany you on your journey. I have permission from the chief to leave the village." Response 2: "I will not slow you down. I will defeat your enemies by the Mor's horns" |

### Mordog Branch 05 - Accept Companion

Topic: [`51EAFA zzzAoMSubQ03MntrB05T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3501) (prompt="Fine, follow me.")

| INFO FormID | Condition(s) | Translation | VMAD |
|---|---|---|---|
| [`51EAFB`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3502) | `GetStage == 40`; `GetIsAliasRef alias #0` | "I appreciate it. I will surely be of service to you." | OnEnd=`AoMSq03_TIF__0251EAFB.Fragment_0` |

Flags: Goodbye (ends greeting/ends branch).

### Mordog Branch 05 - Defer Companion

Topic: [`51EAFC zzzAoMSubQ03MntrB05T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3504) (prompt="Now is not the time.")

| INFO FormID | Condition(s) | Translation | VMAD |
|---|---|---|---|
| [`51EAFD`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3505) | `GetStage == 40`; `GetIsAliasRef alias #0` | "I understand. I will hone my warrior skills until the time is right. Hoping one day to fight alongside you." | OnEnd=`AoMSq03_TIF__0251EAFD.Fragment_0` |

Flags: Goodbye (allows deferral without accepting).

## Related NPCs

NPCs referenced in this quest via dialogue aliases and spoken lines:

- [`51EAA8 zzzCHMntrFollower "Mordog the Man-Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:487) — Alias 0 in quest; first encounter, village guide, companion offer.
- [`51D895 zzzCHMntrLeader "Horbahha the Chief"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:482) — Alias 1 in quest; chief of Minotaur village, executor of Belharza's will.

Additional context NPCs (not direct dialogue in this quest):
- [`510B22 zzzCHMntrBelharza "Belharza the Man-Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:344) — Emperor Belharza, referenced throughout (bloodline legacy, demigod status).

## Reconstruction Notes

Quest structure:
- **Stage 0–5**: First encounter with Mordog; player learns of inherited memories and Belharza connection.
- **Stage 5–10**: Journey to hidden village (possible map quest marker or scene trigger).
- **Stage 10–20**: Arrival at village; meet with chief (Horbahha).
- **Stage 20–30**: Chief dialogue and exposition; possible branching outcomes based on player response.
- **Stage 30**: `CompleteQuest` flag — main quest line ends.
- **Stage 40–60**: Post-completion state; Mordog offers to join player as a companion, with deferral option.

Dialogue polarity:
- **No apparent good/bad branches** — all dialogue is expository; player responses influence companion offer (accept vs. defer) but do not alter quest completion.
- Main outcome: quest completes at stage 30 after chief encounter; companion flag at stage 40.

VMAD fragments present:
- `AomSq03_TIF__0251EAD0` (OnBegin/OnEnd on INFO `51EAD0`) — likely triggers stage progression into village.
- `AomSq03_TIF__0251EAD3` (OnEnd on INFO `51EAD3`) — likely triggers stage jump to village arrival.
- `AomSq03_TIF__0251EAE6` (OnEnd on INFO `51EAE6`) — likely triggers quest completion or companion setup.
- `AoMSq03_TIF__0251EAFB` (OnEnd on INFO `51EAFB`) — likely manages follower recruitment script.
- `AoMSq03_TIF__0251EAFD` (OnEnd on INFO `51EAFD`) — likely maintains deferred state.

Translation issues:
- INFO `51EAE4` contains ellipsis in source; check ESM byte dump if exact intent matters.
- "Mor's horns" in `51EAF9` — likely pagan/daedric curse reference; preserved as-is.

Open verification:
- inspect scripts in `AomSq03_TIF__0251EAD0`, `AomSq03_TIF__0251EAD3`, `AomSq03_TIF__0251EAE6`, `AoMSq03_TIF__0251EAFB`, `AoMSq03_TIF__0251EAFD` if decompiled source or CK export exists;
- inspect alias fill conditions in ESM directly (CLI does not print target refs);
- verify that stage 30 `CompleteQuest` flag has no explicit script trigger (inference: quest auto-closes on VMAD fragment end);
- verify map markers or cell transitions on stage jumps (stage 5→10, stage 10→20) if spatial staging is relevant;
- confirm Mordog's in-game appearance and dialogue delivery (voice-over, lip-sync) once packaged.
