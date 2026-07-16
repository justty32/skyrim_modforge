# Act 1 Side Quest 02 - The Untouchable One

Status: first redo slice. Source-grounded, link-first, no Gemini.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain context or conditions.
- `SCEN` staging comes from CLI diagnostics if present.

## Quest Record

[`006271 zzzAoMMq02 "The Untouchable One"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:TBD)

CLI:
- `questdiag Vigilant.esm 0x006271`
- `infodiag Vigilant.esm 0x006271`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x006271`
- EditorID: `zzzAoMMq02`
- Name: `The Untouchable One`
- Flags: `RunOnce`
- Priority: `90`
- Type: `SideQuest`
- Filter: `AoM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 10 | none | empty |
| 13 | none | empty |
| 15 | none | empty |
| 17 | none | empty |
| 20 | none | empty |
| 30 | CompleteQuest | empty |
| 255 | ShutDownStage | empty |
| 9999 | CompleteQuest | empty |

Objectives:

| Index | Source | Text |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:TBD) | Talk to Altano in The Bannered Mare |
| 10 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:TBD) | Defeat Daedra |
| 20 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:TBD) | Report to Altano |

Objective targets:
- Objective 0: 1 target with 0 conditions.
- Objective 10: 2 targets with 1 condition each.
- Objective 20: 1 target with 0 conditions.
- Current CLI output does not print target cell/ref details; this needs a deeper QUST target dump if location targeting matters.

## Alias / Staging Backbone

No custom `SCEN` records detected by `infodiag`. Stage progression appears linear through dialogue conditions.

Host quest:
- `006271 zzzAoMMq02` "The Untouchable One"

Dialogue aliases from `infodiag`:
- Alias `#0`: expected to be `Altano` (main quest-giver)
- Alias `#1`: expected to be `Vernaccus` (the daedra boss)

(inference: alias names and roles inferred from dialogue conditions `GetIsAliasRef` index 0 and 1; no explicit alias dump available from CLI)

## Custom Dialogue Branches

### Branch 1: Quest Opener — "Is there something unusual?"

TOPIC `0x006274 zzAoMMq02B1Mission2`

Condition pattern:
- `GetStage < 10`: fires before player advances past the initial conversation.
- `GetInCell 0x01605E` (Skyrim.esm, presumed to be The Bannered Mare).
- `GetIsAliasRef alias #0` (Altano).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x006274 zzAoMMq02B1Mission2` | `0x006275` | none | `GetStage < 10`; `GetInCell 0x01605E`; `GetIsAliasRef alias #0` | Prompt: [`"Is there something unusual?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:45) Response (Fear): [`"Few hours ago, one house out of castle wall was broken by Daedra. Daedra was loud laughter. He has staeyed at broken house even now."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:46) Response (Puzzled): [`"A woman who maybe summoner of Daedra was witnessd. Our missions are defeating Daedra and catch the summoner."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:47) |

VMAD Fragment:
- `AoM02_TIF__01006275` (triggers `OnEnd` fragment)
- (inference: fragment likely sets stage 10+ to advance quest)

Translation notes:
- `staeyed` is a typo in the original source, appears as "stayed" in the intended meaning.

### Branch 2: Vernaccus Encounter — Boss Taunt

TOPIC `0x006277 zzAoMMq02B2Vernaccus`

Condition pattern:
- `GetStage < 15`: fires before defeating Vernaccus.
- `GetIsAliasRef alias #1` (Vernaccus).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x006277 zzAoMMq02B2Vernaccus` | `0x006278` | WalkAway | `GetStage < 15`; `GetIsAliasRef alias #1` | [`"I am Vernaccus! I am reputed the untouchable one!! Pittful mortal, bend to my force!!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:50) |

(inference: WalkAway flag indicates NPC dismisses player after line; combat likely triggers after)

### Branch 3: Combat Dialogue — Courage Challenge

TOPIC `0x008DD5 zzAoMMq02B2Fight`

Condition pattern:
- `GetIsAliasRef alias #1` (Vernaccus).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x008DD5 zzAoMMq02B2Fight` | `0x008DD6` | Goodbye | `GetIsAliasRef alias #1` | Prompt: [`"We are vigilants of Stendarr. Are you ready to return to Oblivion?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:TBD) Response: [`"Geee!! but,I am Untouchable One!! I never fall behind you no matter how you are conscious of your powers!!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:TBD) |

VMAD Fragment:
- `AoM02_TIF__01008DD6` (triggers `OnEnd` fragment)
- (inference: fragment likely manages combat state or stage advancement)

### Branch 4: Combat Taunts — Daedra Confidence

TOPIC `0x2D5C61 zzzAoMMq02B203`

Condition pattern:
- `GetIsAliasRef alias #1` (Vernaccus).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x2D5C61 zzzAoMMq02B203` | `0x2D5C62` | Goodbye | `GetIsAliasRef alias #1` | Response (Happy): [`"Ha,Ha,Haaaa!!You are scared!! I get your fear clearly!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:TBD) Response (Happy): [`"I never suffer you. True strong do with all might anytime. Make a shriek of pain!!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:TBD) |

VMAD Fragment:
- `AoM02_TIF__022D5C62` (triggers `OnEnd` fragment)

### Branch 5: Mission Complete — Report to Altano

TOPIC `0x00627C zzAoMMq02B3MissionComplete`

Condition pattern:
- `GetStage == 20`: fires after defeating Vernaccus (stage 20 is post-combat).
- `GetIsAliasRef alias #0` (Altano).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x00627C zzAoMMq02B3MissionComplete` | `0x00627D` | none | `GetStage == 20`; `GetIsAliasRef alias #0` | Prompt: [`"Where is the summoner?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:52) Response (Anger): [`"The Summoner has already escaped from here. She summoned Vernaccus, Higher Daedra. Her ability of Conjuring is master class."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:53) Response (Happy): [`"I will be back to Inn and gather informaiton about the summoner. If you are ready, come to me."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:54) |

VMAD Fragment:
- `AoM02_TIF__0100627D` (triggers `OnEnd` fragment)
- (inference: fragment likely completes objective 20 and advances toward stage 30)

Translation notes:
- `informaiton` is a typo in the original source; intended "information".

## Stage Flow Inference

Based on dialogue condition gates and objective progression:

1. **Stage 0**: Quest starts. Player receives initial mission briefing.
2. **Stage 10**: Triggered after listening to Altano at The Bannered Mare (Branch 1 dialogue + VMAD fragment).
   - Objective 0 complete.
   - Objective 10 (Defeat Daedra) becomes active.
3. **Stages 13, 15, 17**: Intermediate checkpoints during combat. Purpose unknown without VMAD inspection.
4. **Stage 20**: Triggered after defeating Vernaccus (post-combat state).
   - Objective 10 complete.
   - Objective 20 (Report to Altano) becomes active.
   - Branch 5 dialogue becomes available.
5. **Stage 30**: Quest completion flag (`CompleteQuest` in questdiag output).
   - Triggered after player reports to Altano (Branch 5 dialogue + VMAD fragment).
6. **Stage 255**: ShutDownStage (engine cleanup).
7. **Stage 9999**: Fallback completion flag (redundant with stage 30).

## Branch Polarity Analysis

**Single linear path** (no branching):
- All dialogue branches are conditioned on quest stages and alias refs, not on player choices.
- The "Anger" and "Happy" response variants in Branch 5 suggest Altano's emotional state or contextual variations, but do not fork the quest outcome.
- No dialogue conditions implement conditional quest failure or alternative endings.
- **Conclusion**: This is a **linear quest with forced progression**. The summoner escape and Daedra defeat are mandatory waypoints leading to the next act quest.

## Related Records

NPCs:
- `zzzAoMMq01` Altano (main quest-giver for Acts 1–2)
  - (inference: Altano carries forward as quest-giver; verify NPC record in Vigilant npc.tsv)
- Vernaccus (Higher Daedra boss, summoned by the unknown summoner)
  - (inference: name inferred from dialogue topic "zzAoMMq02B2Vernaccus" and boss dialogue; verify NPC record)

Locations:
- The Bannered Mare, Whiterun (cell 0x01605E in Skyrim.esm)
  - (inference: The Bannered Mare is a vanilla Whiterun inn; cell ID 0x01605E confirms location)
- Broken house location (unnamed in dialogue, awaits cell/ref verification)

Creatures/Daedra:
- Vernaccus (Higher Daedra of unspecified type; not a standard Daedra in Skyrim.esm)
  - (inference: "Higher Daedra" suggests rank or power tier; exact Daedra type TBD via NPC record)

Spells/Conjuration:
- Conjure Vernaccus spell or equivalent (cast by the unknown summoner)
  - (inference: spellcasting implied by "Her ability of Conjuring is master class"; spell name TBD via spell record search)

## Open Verification

- [ ] Verify Altano NPC record (FormID, location, faction) in Vigilant npc.tsv.
- [ ] Inspect Vernaccus creature/NPC record: type, level, AI package, magic resist.
- [ ] Verify broken house location: cell ID, cell exterior, coordinate range where combat occurs.
- [ ] Decode VMAD fragments (`AoM02_TIF__0100627[5D]`, `AoM02_TIF__01008DD6`, `AoM02_TIF__022D5C62`) if source exists:
  - [ ] Do any fragments set stage 13, 15, 17 during combat?
  - [ ] Do any fragments depend on Vernaccus death state?
  - [ ] Do any fragments advance to stage 30, or is that handled by quest system?
- [ ] Verify spell/ability used by summoner (search Vigilant.esm for "Conjure Vernaccus" or equivalent).
- [ ] Verify cell 0x01605E is indeed The Bannered Mare in Skyrim.esm.
- [ ] Check whether stage 20 is set by Vernaccus death trigger or by player-initiated dialogue.
- [ ] Determine karma outcome: is defeating a summoner's conjured daedra considered "good" alignment in Stendarr context?
- [ ] Inspect objective 10 targets for combat location or encounter staging (CLI output shows 2 targets with conditions; what are they?).

## Reconstruction Notes

This quest represents **Act 1 Side Quest 02** in the Vigilant of Stendarr questline, following Act 1 Quest 01 (vampire investigation). It introduces a higher-tier daedric threat and presages the summoner storyline that extends into Acts 2–3.

Key staging elements:
- The quest is linear with no player choice fork.
- Dialogue is stage-gated at entry (stage < 10), combat engagement (stage < 15), and completion (stage == 20).
- The unknown summoner escapes; Vernaccus is defeated but represents a tipping point in quest complexity.
- Objectives are synchronized with dialogue: each branch corresponds to an objective phase.

Source-grounded links:
- [`006271 zzzAoMMq02` quest record](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:TBD)
- [`006274`, `006277`, `008DD5`, `0x2D5C61`, `00627C` dialogue topics](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:45–54)
- Aliases assumed to exist but not explicitly named in CLI output; await QUST alias dump for confirmation.
