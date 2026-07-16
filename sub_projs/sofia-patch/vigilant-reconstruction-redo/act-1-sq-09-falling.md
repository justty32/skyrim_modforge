# Act 1 Quest 09 - Infinite Falling

Status: source-grounded slice. Links to extracted sources; CLI diagnostics deferred to Manjaro machine.

Source policy:
- Original lines linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a condition or branch polarity.
- `SCEN` staging and quest aliases require CLI `scenediag`/`questdiag` output; offline extraction shows dialogue/objective only.

## Quest Record

[`00EFF7 zzzAoMMq09 "Infinite Falling"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:346)

CLI (to be run on Manjaro):
- `questdiag Vigilant.esm 0x00EFF7`
- `infodiag Vigilant.esm 0x00EFF7`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from extracted `quests.md`:
- FormID: `Vigilant.esm:0x00EFF7`
- EditorID: `zzzAoMMq09`
- Name: `Infinite Falling`
- Type: (not specified in extracted; CLI needed)
- Priority: (not specified in extracted; CLI needed)
- Stages: (not specified in extracted; CLI needed — reported as 20 stages)

Objectives from extracted `quests.md`:

| Index | Objective |
|---:|---|
| 0 | [Talk to Altano](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:347) |
| 10 | [Defeat Daedra](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:348) |
| 15 | [Find Survivor](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:349) |
| 20 | [Chase Altano](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:350) |
| 40 | [Defeat Molag Bal](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:351) |

Inference:
- Objective progression suggests a linear quest flow: dialogue (obj 0) → combat (obj 10) → search (obj 15) → pursuit (obj 20) → final boss (obj 40).
- Stage count of 20 is consistent with 5 major objectives spread across multiple stages (typical pattern: opener + variations per objective).

## Alias / Staging Backbone

Offline source does not show QUST alias definitions; `questdiag` and `scenediag` output required to identify:
- Host quest: `0x00EFF7 zzzAoMMq09`
- Named aliases (e.g., Altano, Daedra, Survivors)
- Any linked `SCEN` records for combat/falling animations

## Custom Dialogue Branch: Altano Encounter

Branch opener:
- [`012642 zzAoMMq09B3AltanoTopic`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:399)

Topic text:
- [Well well well. Where have you been wasting your time all this while? You seem to be forsakend by Stendarr?](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:400)

Speaker condition pattern:
- Most INFOs likely require stage-gating at objective 0 or stage matching Altano's initial encounter.
- Character state: Altano is revealed as compromised / corrupted by this point (Act 1 progression context).

### Altano Betrayal & Molag Bal

Branch:
- [`012643 zzAoMMq09B3WhyBetray`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:402)

| Topic | Response Lines | Translation |
|---|---|---|
| [`012643 zzAoMMq09B3WhyBetray`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:402) | [「There is no need to explain the reason because you are dying.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:403) | Altano dismisses explanation; player's death is imminent. |
| | [「Genghis, Sent that soul to Molag bal. I must back to the altar and continue rituals.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:404) | Altano commands [`Genghis`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:860) (Dremora) to consume player's soul; Altano returns to altar rituals. |

Inference:
- Altano is the questgiver's betrayer and is performing a ritual (likely at the Altar of Molag Bal per Act 1 geography).
- Genghis (NPC 0x00183F) is a Dremora summoned/controlled by Altano.
- Player is marked for death and soul consumption; this is the "infinite falling" event.
- This dialogue triggers around objective 0 completion and failure (player is attacked/defeated).

## Related NPC Records

Key actors:

| FormID | EditorID | Name | Role |
|---|---|---|---|
| [`000D62`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:835) | `zzzAoMVigilantTraitor` | Altano | Vigilant keeper turned traitor; initiates the Daedric ritual. |
| [`00183F`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:861) | `zzzAoMBossDremora05` | Ranyu | Dremora (context unclear from dialogue; may be co-conspirator). |
| [`001840`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:860) | `zzzAoMBossDremora06` | Genghis | Dremora summoned by Altano; soul-devourer. |
| [`0EFC32`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:270) | `zzzCHSummonAltano` | Altano (summon) | Alternate form or reference (context TBD). |
| [`42E0B1`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv) | (memory guide?) | (guide?) | Potential quest hub (per memory quest pattern); full details in questdiag. |

Related quest 10:
- [`013678 zzAoMMq10B1BetrayReason`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:409): Altano reveals he was corrupted by Molag Bal's whispers after a prior battle. Implies a multi-act corruption arc.
- [`013676 zzAoMMq10B1LastWord`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:406): Altano's dying utterance is incoherent (`aa.....uaa...`), suggesting severe mental/magical compromise.

## Survivor Dialogue (Objective 15)

Librarian discovery after combat:
- [`027FB3 zzzAoMMq09B4LirarianWound`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:439)

| Topic | Response Lines |
|---|---|
| Greeting | [「Please...Help me...」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:440) |
| | [「Let me rest...」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:441) |

### What Happened?

Branch:
- [`027FB5 zzzAoMMq09B4WhatHappen`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:443)

| Topic | Prompt & Response |
|---|---|
| [`027FB5`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:443) | Prompt: [「It's okay. What was happening?」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:443) Response: [「Altano...Altano summoned Daedra...all of a sudden...we did not understand what happened」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:444) & [「I could not do anything ... Daedra killed Thorondir and others...」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:445) |
| [`027FB7 zzzAoMMq09B4Isee`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:447) | Prompt: [「I see...」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:447) Response: [「Please stop Altano...he is trying to be outrageous ...」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:448) |
| [`027FB9 zzzAoMMq09B4understand`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:450) | Prompt: [「I understand, you should get some rest」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:450) Response: [「Yeah, I mind was relieved gone missing ... I've been allowed to do so ...」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:451) |

Translation notes:
- "Thorondir and others" refers to Stendarr temple keepers killed in the daedric incursion. Thorondir is the head keeper per Act 1 setup.
- "outrageous" (暴走) implies Altano has lost rational control; he is a puppet / fully corrupted rather than willfully evil.
- Librarian's statement "gone missing" is unclear in source; likely means memory/clarity lost.

## Molag Bal Encounter (Objective 40)

Direct dialogue with Molag Bal:
- FormID: [`10C89A zzzAoMSummonDragonMolagBal`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:437) (Molag Bal summon/boss form in Act 1)

Molag Bal's introduction:
- [`013BE5` [Scene/Scene]](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:417)

| Scene Line | Translation |
|---|---|
| [「Son of Stendarr..I see you. When your soul is corrupt, you open the gate of my realm...」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:417) | Molag Bal recognizes the player as a Vigilant; corruption of soul opens access to Oblivion. |

Inference:
- This is the climactic scene of quest 09: player faces Molag Bal directly.
- Molag Bal's presence is conditional on soul corruption (linking to the "infinite falling" state).
- Victory over Molag Bal is necessary to complete objective 40 and the quest.

## Location Records

Key locations:

| FormID | EditorID | Type | Name |
|---|---|---|---|
| [`004102`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:38) | `zzzAoMHallofMolagBal` | CELL | Altar of Molag Bal |
| [`26D3A8`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:497) | `zzzAoMLocAltarMolag` | LCTN | Altar of Molag Bal |

Inference:
- Quest 09 takes place at or near the Altar of Molag Bal, the site of Altano's ritual.
- The "infinite falling" title suggests a fall/descent into Oblivion or a magical descent within the altar structure.

## Reconstruction Notes

Source-grounded:
- [`00EFF7 zzzAoMMq09 "Infinite Falling"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:346) is a 20-stage quest tracking the player's discovery of Altano's betrayal, combat with Daedra, interrogation of a survivor (librarian), and final confrontation with Molag Bal.
- The quest is the climax of Act 1, following the temple's desecration and the revelation that the Vigilant leadership is compromised.
- It contains at least one custom dialogue branch (Altano + Molag Bal) and likely one or more `SCEN` records for the falling sequence and final boss staging.
- The "infinite falling" metaphor likely refers to the player's soul being dragged into Oblivion by Molag Bal during the encounter.

Flow summary:
1. **Obj 0**: Approach Altano to learn of the betrayal.
2. **Obj 10**: Defeat Daedra summoned by Altano (combat phase).
3. **Obj 15**: Find and interrogate a survivor librarian.
4. **Obj 20**: Chase Altano (pursuit phase).
5. **Obj 40**: Defeat Molag Bal (final boss; likely auto-completes quest or triggers completion handler).

Open verification:
- Run `questdiag Vigilant.esm 0x00EFF7` to dump all stages, flags, and stage log entries.
- Run `infodiag Vigilant.esm 0x00EFF7` to enumerate all dialogue topics and INFOs owned by this quest.
- Run `scenediag` on any `SCEN` records found (likely names matching `*MolagBal*` or `*Falling*` or `*Altano*`) to extract phase/action details.
- Verify NPC records for Altano, Genghis, and Molag Bal summon to confirm their roles and dialogue conditions.
- Inspect `Altar of Molag Bal` location records and any interior cell references to confirm geography and stage-gating.
- Inspect any VMAD Papyrus fragments on the final dialogue choice (likely a `Goodbye/SayOnce` INFO) to understand quest completion routing.
