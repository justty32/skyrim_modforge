# Act 1 Hub - Vigilant of Stendarr

Status: first redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- Dialogue conditions and branching are from CLI diagnostics, as extracted text only preserves topic text, not condition chains.

## Quest Record

[`005CE2 zzzAoMMq00 "Vigilant of Stendarr"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:L1)

CLI:
- `questdiag Vigilant.esm 0x005CE2`
- `infodiag Vigilant.esm 0x005CE2`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x005CE2`
- EditorID: `zzzAoMMq00`
- Name: `Vigilant of Stendarr`
- Flags: `273` (RunOnce, Repeatable flags)
- Priority: `90`
- Type: `SideQuest`
- Filter: `AoM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | StartUpStage | empty |
| 5 | none | empty (objective log present, 3 conds on second entry) |
| 10 | none | empty (2 conds on second entry) |
| 15 | none | empty |
| 20 | none | empty |
| 30 | none | empty |
| 40 | CompleteQuest | empty |
| 999 | CompleteQuest | empty |
| 9999 | CompleteQuest | empty |

Objectives:

| Index | Source | Translation |
|---:|---|---|
| 5 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md#L2) | Join the Vigilant of Stendarr |
| 10 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md#L3) | Follow Altano or Join Altano at Temple of Stendarr |
| 15 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md#L4) | Talk to Altano |
| 20 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md#L5) | Talk to Thorondir |
| 30 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md#L6) | Talk to Altano |

Objective targets:
- 1 target at objective 5 (0 conditions)
- 2 targets at objective 10 (both 0 conditions)
- 1 target at objective 15 (0 conditions)
- 1 target at objective 20 (0 conditions)
- 1 target at objective 30 (0 conditions)
- Current CLI output does not print target locations; this needs a deeper QUST target dump if exact locations matter.

## Dialogue Topics

### Initial Recruitment (Branch 005CE5)

**Stage gate**: stage < 10 for first recruitment topic; stage >= 10 accepts follow-up dialogue at the temple.

#### Topic 1: Recruitment pitch (005CE6)

[`005CE6 zzAoMMq0B1Tvigilant` prompt: "Let me join the vigilant of Stendarr."](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L1)

| INFO | Flags | Conditions | Translation |
|---|---|---|---|
| [`005CE7`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L2) | `SayOnce`, `WalkAway` | `GetStage < 10`; `GetIsAliasRef alias #0` | ["You have good eyes.Why do't you join the vigilant of Stendarr?Fill Skyrim with the Mercy of Stendarr together?"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L2) |
| [`005CEC`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L3) | `WalkAway` | `GetStage < 10`; `GetIsAliasRef alias #0` | ["Chage your mind?We welcome you."](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L3) |

Inference:
- Alias #0 is Altano (recruiter).
- `SayOnce` on the first response suggests this is the initial offer; subsequent visits reuse the second INFO.

#### Topic 2: Yes, accept recruitment (005CE8)

[`005CE8 zzAoMMq0B1Yes` prompt: "Yes, let me join."](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L4)

| INFO | Flags | Conditions | Responses | VMAD |
|---|---|---|---|---|
| [`005CE9`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L5) | none | `GetIsAliasRef alias #0` | (1) ["I am glad to receive a favorable reply. Stendarr belss you."](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L5) / (2) ["I will guide you to Temple of Stendarr. Come with me"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L6) | `AoM00_TIF__01005CE9.Fragment_0` on end |

Inference:
- Multi-line response suggests dialogue flavor before the teleport.
- VMAD fragment likely stages the player to stage 5+ and issues travel package.

#### Topic 3: Decline recruitment (005CEA)

[`005CEA zzAoMMq0B1No` prompt: "No,Not interested."](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L7)

| INFO | Flags | Conditions | Translation | VMAD |
|---|---|---|---|---|
| [`005CEB`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L8) | `Goodbye` | `GetIsAliasRef alias #0` | ["Oh...I am here. If you change your mind...Come here again."](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L8) | `AoM00_TIF__01005CEB.Fragment_0` on end |

Inference:
- `Goodbye` flag indicates this ends the conversation branch.
- VMAD fragment likely resets dialogue availability or logs the refusal.

### At the Temple (Branch 027A3B, 027A40, 027A43)

**Stage gate**: stage >= 15 for temple greeting; stage = 20 for Thorondir greeting; stage = 30 for explanation.

#### Topic 4: Arrival at Temple (027A3C)

[`027A3C zzzAoMMq00B2ArriveTemple`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L9)

| INFO | Flags | Conditions | Responses | VMAD |
|---|---|---|---|---|
| [`027A3F`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L10-11) | `Goodbye` | `GetInCell == 025091:Vigilant.esm`; `GetStage == 15`; `GetIsAliasRef alias #0` | (1) ["This is Temple of Stendarr, one of the bases of the Vigilants."](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L10) / (2) ["You should greet with  Thorondir. He is Keeper of Stendarr."](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L11) | `AoM00_TIF__01027A3F.Fragment_0` on end |

Inference:
- `GetInCell` confirms location at Temple of Stendarr interior (025091).
- Stage 15 gate: player should reach this after stage 5–10 travel sequence.
- VMAD likely advances stage to 20 (Thorondir greeting prep).

#### Topic 5: Thorondir greeting (027A41)

[`027A41 zzzAoMMq00B3NiceToMeet` prompt: "Nice to meet you, I am <Alias=Player>"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L12)

| INFO | Flags | Conditions | Responses | VMAD |
|---|---|---|---|---|
| [`027A42`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L13-15) | `Goodbye` | `GetStage == 20`; `GetIsAliasRef alias #4` | (1) ["Are you the rookie who Altano talk about. You have a good eye. I feel very strong will."](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L13) / (2) ["Small talk is So much. Because such would be boring old story"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L14) / (3) ["Altano will Take care of you for a while. If you have something, say to Altano."](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L15) | `AoM00_TIF__01027A42.Fragment_0` on end |

Inference:
- Alias #4 is Thorondir (keeper).
- Multi-response suggests greeting + instruction flow.
- VMAD likely advances stage to 30 (explanation).

#### Topic 6: Temple explanation (027A44)

[`027A44 zzzAoMMq00B04Explanation` prompt: "Tell me about this temple"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L16)

| INFO | Flags | Conditions | Responses | VMAD |
|---|---|---|---|---|
| [`027A45`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L17-22) | `Goodbye` | `GetStage == 30`; `GetIsAliasRef alias #0` | (1) ["I'll keep a brief description of facilities available."](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L17) / (2) ["Where you are standing now between Stendhal's. It's a place to pray to Stendhal."](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L18) / (3) ["There is a library that stores the books that our ancestors have gathered in the basement. I hope you go to see when you have time"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L19) / (4) ["On the first floor right near smelting units, the second floor is in the break room seen from the entrance"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L20) / (5) ["The second floor is a dining room on the left. You are there to eat me feel hungry. Such is not a feast"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L21) / (6) ["Description So much. You may be tired. I hope you get some rest"](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L22) | `AoM00_TIF__01027A45.Fragment_0` on end |

Inference:
- Alias #0 switches context here (likely back to Altano for flavor description).
- Six-part response is a monologue covering: intro → prayer hall → library → ground floor facilities → dining room → rest suggestion.
- VMAD likely advances to stage 40 (quest complete).

## Related Records

NPCs (quest-affiliated):
- [`0274A6 zzzAoMVigilantKeeper` - Thorondir (Keeper of Stendarr)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv#L1)
- [`000D62 zzzAoMVigilantTraitor` - Altano (Recruiter)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv#L1)
- [`02748B zzzAoMVigilantKeeper` - Thorondir (alternate record)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv#L1)

Locations:
- [`025091 zzzAoMTempleInteriorStendarr` - Temple of Stendarr (interior cell)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant)

## Reconstruction Notes

Source-grounded:
- This hub quest (`005CE2 zzzAoMMq00`) is the entry point for Act 1: the player is recruited by Altano, travels to the Temple of Stendarr, and meets Thorondir.
- It contains 6 dialogue topics (`005CE6`, `005CE8`, `005CEA`, `027A3C`, `027A41`, `027A44`) with 7 INFO records total.
- Stage progression: stage 0 → stage 5 (join offer) → stage 10 (travel) → stage 15 (arrive) → stage 20 (meet keeper) → stage 30 (explanation) → stage 40 (complete).
- All INFOs are speaker-bound via `GetIsAliasRef` on either alias #0 (Altano) or alias #4 (Thorondir).
- VMAD fragments on Goodbye/SayOnce responses suggest dialogue script advancement; exact Papyrus behavior is not decoded here.

Translation notes:
- [005CE7]: "Why do't you" = "Why don't you" (typo in source).
- [027A41]: "<Alias=Player>" is a dialogue placeholder; actual in-game substitution uses player name.
- [027A44]: Multi-response lines are from a single INFO record, likely played as a monologue sequence.

Open verification:
- Inspect scripts `AoM00_TIF__01005CE9`, `AoM00_TIF__01005CEB`, `AoM00_TIF__01027A3F`, `AoM00_TIF__01027A42`, `AoM00_TIF__01027A45` if source or decompile path exists;
- Inspect QUST aliases directly (alias #0 = Altano, alias #4 = Thorondir) if a richer alias dump is available;
- Inspect stage 5 condition chain (questdiag showed 3 conds on second entry) to verify exact requirements for joining;
- Verify cell location 025091 if spatial staging matters;
- Cross-link with Act 1 chapter quests (`act-1-sq-01-squeezer.md` onwards) if the recruitment→chapter sequence is contingent.
