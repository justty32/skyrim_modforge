# Act 3 Side Quest - Stendarr Guide

Status: source-grounded slice. No dialogue or scene records found; quest structure only.

Source policy:
- FormID, EditorID, objectives extracted from ESM via CLI questdiag.
- Extracted `quests.md` link for objective reference.
- No dialogue topics owned by this quest (confirmed via infodiag).
- No scene records (confirmed via scenediag).

## Quest Record

[`43CBAE zzzCOGuide "Stendarr Guide"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:360)

CLI:
- `questdiag Vigilant.esm 0x43CBAE`
- `infodiag Vigilant.esm 0x43CBAE` — result: no dialogue topics found
- `scenediag Vigilant.esm 0x43CBAE` — result: not a scene record

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x43CBAE`
- EditorID: `zzzCOGuide`
- Name: `Stendarr Guide`
- Flags: `RunOnce`
- Priority: `90`
- Type: `Misc`
- Filter: `CO\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 1 | none | empty |
| 10 | none | empty |
| 20 | none | empty |
| 22 | none | empty |
| 24 | none | empty |
| 30 | none | empty |
| 35 | none | empty |
| 40 | none | empty |
| 50 | none | empty |
| 60 | none | empty |
| 70 | CompleteQuest | empty |
| 999 | CompleteQuest | empty |

Objectives:

| Index | Source | Text |
|---:|---|---|
| 10 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:361) | Breack the curse of Shivering |
| 20 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:362) | Breack the curse of Depravity |
| 22 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:363) | Go to Julius's Room |
| 24 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:364) | Go To Basement |
| 30 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:365) | Breack the curse of Foamy |
| 35 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:366) | Gain the Key of Bartolo's Room |
| 40 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:367) | Breack the curse of Chain |
| 50 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:368) | Breack the curse of Envy |
| 60 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:369) | Release Julius |

Objective targets:
- 9 objectives, each with 1 target.
- All targets carry flag `CompassMarkerIgnoresLocks`.
- Target refs not dumped by CLI; require deeper QUST target inspection if placement refs matter.

## Dialogue Records

No dialogue topics owned by quest `0x43CBAE`. Inference:
- This is a **pure objective quest** — no NPC-driven dialogue.
- Objectives are likely triggered by environmental actions (interacting with cursed objects, navigating to locations, defeating enemies, rescuing NPCs).
- Stage progression is not explicitly logged (`CompleteQuest` appears at stages 70 and 999, but log entry is empty).

Hypothesis:
- (inference) Stage 70 or 999 marks quest completion, likely triggered programmatically (e.g., via script effect or quest alias update) when the player satisfies all major objectives.

## Scene Records

No scene records found with `0x43CBAE` as host quest. This quest contains no scene staging.

## Act 3 Context

zzzCOGuide is a **side quest within Act 3 (Mansion arc)**. Act 3 main quest is [`065932 zzzCOMq01 "Child of Oblivion"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:276), which involves:
- Investigating the Noble Mansion (obj 30)
- Solving a case (related to the mansion's curse)
- Defeating Julius (obj 60)
- Escaping the mansion (obj 70)

The "Stendarr Guide" quest has several overlapping location/NPC references:
- "Go to Julius's Room" (obj 22) — maps to zzzCOMq01's main antagonist location
- "Release Julius" (obj 60) — directly references Julius from main quest
- Curse-breaking objectives suggest the quest involves breaking five distinct curses:
  - Shivering, Depravity, Foamy, Chain, Envy

This suggests Stendarr Guide is an **optional puzzle/challenge quest** that the player can undertake while in the mansion, involving breaking supernatural curses on objects or people as side objectives.

## Objective Translation Notes

- "Breack" in all five curse objectives is a typo for "Break" (typo preserved from source).
- Curse names suggest symbolic/emotional themes: Shivering (fear), Depravity (vice), Foamy (madness?), Chain (bondage/servitude), Envy (greed).
- "Go To Basement" uses American capitalization style (`To` rather than `to`).

## Related Records

Main quest of Act 3:
- [`065932 zzzCOMq01 "Child of Oblivion"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:276)

Other Act 3 side quests:
- [`324E7E zzzCOSubQ01 "Successor"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:32)
- [`444115 zzzCOqOwl "Weaver's Needle 2"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:321)

Generic dialogue:
- [`065EF0 zzzCOGenericDialogue "CO Generic Dialogue"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:355)

## Reconstruction Notes

Source-grounded:
- This quest is represented by FormID `0x43CBAE` with EditorID `zzzCOGuide` in Vigilant.esm.
- It contains 9 objectives tied to 9 compass targets in the mansion.
- It has no dialogue branches and no scene records, indicating it is driven by pure environmental mechanics (object interaction, NPC encounter, location discovery) rather than NPC dialogue trees or staged scenes.
- Stages 0, 1, 10–60 appear to be intermediate progression; stages 70 and 999 both carry `CompleteQuest` flag.

Open verification:
- Locate the actual cursed objects/NPCs referenced by compass targets (Shivering curse, Depravity curse, etc.) via cell/reference inspection.
- Determine whether stage 70 or 999 is the actual completion trigger (both are marked `CompleteQuest`; likely 70 is intended and 999 is failsafe).
- Check if any quest alias or script-driven logic (not exposed in QUST record) gates the progression through objectives 10 → 20 → 22 → 24 → 30 → 35 → 40 → 50 → 60 → 70.
- Verify whether Stendarr Guide is optional (player can skip entirely and finish zzzCOMq01) or mandatory to progress Act 3.
- Inspect Act 3 location cells for NPCs named Julius, Bartolo, or related characters if fuller context is needed for narrative reconstruction.
