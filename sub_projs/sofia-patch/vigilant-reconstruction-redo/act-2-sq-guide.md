# Act 2 SQ Guide - Stendarr Guide

Status: first redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue or a specific condition.
- CLI diagnostics provide definitive stage/objective/conditions data.

## Quest Record

[`43B81F zzzBMGuide "Stendarr Guide"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:83)

CLI:
- `questdiag Vigilant.esm 0x43B81F`
- `infodiag Vigilant.esm 0x43B81F`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x43B81F`
- EditorID: `zzzBMGuide`
- Name: `Stendarr Guide`
- Flags: `RunOnce`
- Priority: `90`
- Type: `Misc`
- Filter: `BM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 10 | none | empty |
| 20 | CompleteQuest | empty |
| 999 | CompleteQuest | empty |

Objectives:

| Index | Source | Quest Text |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:84) | Trace the blood |
| 10 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:85) | Trace the elder blood |

Objective targets:
- Each objective has 1 target in ESM.
- Target flags: `CompassMarkerIgnoresLocks` on both targets.
- Target conditions: none.
- (inference: targets are likely marker refs placed in Windhelm dungeons; exact locations require deeper dump if spatial staging matters.)

## No Dialogue or Scenes

Unlike the Act 2 main quests (`zzzBMMq01–03`), this quest has:
- No custom dialogue topics (verified via `infodiag 0x43B81F`: "no DialogTopic with that FormID, and no topics owned by a quest with that FormID").
- No scene records (verified via `scenediag 0x43B81F`: "0x43B81F is not a Scene").

This is a **navigation and tracking quest** — purely for objective markers and compass guidance during the Windhelm Underground investigation.

## Reconstruction Notes

Source-grounded:
- This Misc quest is represented by [`43B81F zzzBMGuide`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:83) with name `"Stendarr Guide"`.
- It contains 2 objectives (stage 0 and stage 10) that map to the two main investigation branches:
  - Objective 0: "Trace the blood" (initial investigation phase)
  - Objective 10: "Trace the elder blood" (escalated investigation phase, possibly linked to vampire discovery in `zzzBMMq02`)
- It completes at stage 20 (explicit `CompleteQuest` flag) and has a shutdown stage at 999.
- All dialogue and interaction occurs through the main Act 2 quests (`zzzBMMq01`, `zzzBMMq02`, `zzzBMMq03`).

Quest flow inference:
- Stage 0–10: Quest is active; player follows compass markers placed by objectives 0 and 10.
- Stage 20: Quest completes (both investigation branches resolved in main quests).
- Stage 999: Shutdown (cleanup).

Open verification:
- Exact target locations (cells/refs) of objectives 0 and 10 if spatial layout matters.
- Whether stage progression is driven by `zzzBMMq01`, `zzzBMMq02`, or a parent quest manager script.
- Whether this quest runs in parallel with the main Act 2 quests or is a parent/wrapper quest.
