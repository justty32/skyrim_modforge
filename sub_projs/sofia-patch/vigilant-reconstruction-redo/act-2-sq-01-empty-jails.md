# Act 2 Sq01 - Empty Jails

Status: first redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue or a specific condition.
- CLI diagnostics provide definitive stage/objective/conditions data.

## Quest Record

[`038524 zzzBMMq01 "Empty Jails"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?)

CLI:
- `questdiag Vigilant.esm 0x038524`
- `infodiag Vigilant.esm 0x038524`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x038524`
- EditorID: `zzzBMMq01`
- Name: `Empty Jails`
- Flags: `RunOnce`
- Priority: `90`
- Type: `SideQuest`
- Filter: `BM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | StartUpStage | empty |
| 10 | none | empty |
| 20 | none | empty |
| 30 | none | empty |
| 40 | none | empty |
| 50 | none | empty |
| 60 | none | empty |
| 100 | none | empty |
| 110 | CompleteQuest | empty |
| 110 | none | empty |
| 255 | CompleteQuest | empty |
| 999 | ShutDownStage | empty |
| 9999 | CompleteQuest | empty |

Objectives:

| Index | Source | Quest Text |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | Talk to `<Alias=Courier>` |
| 10 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | Talk to `<Alias=Steward>` |
| 20 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | Search Windhelm Dungeon |
| 30 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | Talk to `<Alias=Steward>` about Maiden Statue |
| 40 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | Find Windhelm Report in Temple of Stendarr |
| 50 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | Talk to `<Alias=Steward>` |
| 60 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | Defeat Vampires under Windhelm |
| 100 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | Report to `<Alias=Steward>` |

Objective targets:
- Each objective has 1 target in ESM.
- Target conditions: objectives 20 and 60 have 1 condition each; others have 0.
- (inference: target refs are likely placed in Windhelm dungeon and a vampire location; exact locations require deeper dump if spatial staging matters.)

## Custom Dialogue Branches

This quest has multiple dialogue branches. The two main NPCs are identified by alias indices: `Courier` (alias #2) and `Steward` (alias #4, presumably `zzzBMMq01Steward`). A third alias (#7, `Library Guard` or similar) appears in the Mage's College library context. All branches are owned by quest `0x038524`.

### Branch 1: Courier Introduction (Alias #2)

Branch:
- `038AB2:Vigilant.esm` (inferred; branch root not printed by CLI but aliases reference it)

Speaker condition pattern:
- Most INFOs require `GetIsAliasRef == 1` on alias `#2` (Courier).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`038AB3 zzzBMMq01B01gStart`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:474) | `038AB4` | none | `GetStage EqualTo 0`; `GetIsAliasRef alias #2` | [Or are you just here ... you are the keeper of Stendhal's like here?](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:475) |
| [`038AB5 zzzBMMq01B01gMatter`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:478) | `038AB6` | none | `GetIsAliasRef alias #2` | Prompt: "What's the Matter?" Response: [I came in the life of the consul Windhelm. Although I want to help the keeper of Stendhal inquiry from YOU ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:479) |
| [`038AB7 zzzBMMq01B01gNO`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:481) | `038AB8` | none | `GetIsAliasRef alias #2` | Prompt: "I'm Busy now" Response: [This is instruction. Likely to restrict the activities of these around Eastmarch if YOU refuse](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:482) |
| [`038AB9 zzzBMMq01B01gOK`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:484) | `038ABA` | Goodbye, SayOnce | `GetIsAliasRef alias #2`; VMAD `BM01_TIF__01038ABA.Fragment_0` on end | Prompt: "With the incident?" Response: [More likely to speak their own consul. This matter with the thing I want you to keep quiet with it](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:485) |
| [`038ABB zzzBMMq01B01gGoOn`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:487) | `038ABC` | none | `GetIsAliasRef alias #2` | Prompt: "Does not matter, they continue" Response: [Disappearances of prisoners occurred in Windhelm. I'd like to request an investigation of this single house from YOU](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:488) |

Translation notes:
- The original Courier dialogue is unclear in extraction (poor translation quality in source); this may warrant verification against the ESM itself if the topic becomes plot-critical.

### Branch 2: Steward / Guard Chief (Alias #4)

Branch:
- `038ABF:Vigilant.esm` (inferred root from INFO `038AC1` onward)

Speaker condition pattern:
- Most INFOs require `GetIsAliasRef == 1` on alias `#4` (Steward / Guard Chief).
- Some INFOs are stage-gated.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`038AC0 zzzBMMq01B01stMissionStart`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:490) | `038AC1` | none | `GetStage EqualTo 10`; `GetIsAliasRef alias #4` | Prompt: "I am `<Alias=Player>`, Vigilant of Stendarr" Response (1): [Welcome, I've been waiting. One of Stendhal in the Cathedral is based on ear](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:491) Response (2): [Do not need a lot of places, things are urgent. You want to leave the investigation of the incident that](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:492) |
| [`038AC2 zzzBMMq01B01stDetail`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:494) | `038AC3` | none | `GetIsAliasRef alias #4` | Prompt: "So the details of the case?" Response (1): [It's a hard-working such. Immediately, the jailing story.](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:495) Response (2): [Prisoners have disappeared one after another from three days ago. I do not ask that offense. Had been left as the glue of numerous blood to jail ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:496) Response (3): [Even last night ... The guards had vanished Madashimo if I only prisoner. Uncontrollable to us. I want to extend a helping hand](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:497) |
| [`038AC4 zzzBMMq01B01stEntrust`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:499) | `038AC5` | none | `GetIsAliasRef alias #4`; VMAD `BM01_TIF__01038AC5.Fragment_0` on end | Prompt: "Entrust to me" Response: [It is a reliable words. Many disappearances have occurred in the prison of the castle. I want to examine it first.](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:500) |
| [`039597 zzzBMMq01B02stAgain`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:502) | `039598` | none | `GetStage EqualTo 20`; `GetIsAliasRef alias #4` | Prompt: "Tell me again about the incident" Response (1): [Prisoners have disappeared one after another from three days ago. Do not leave a lot of bloodstains. Even last night jailer has disappeared](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:503) Response (2): [Dungeon of the castle is the back of the barracks. I want to examine carefully](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:504) |
| [`03959B zzzBMMq01B03stStoneFace`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:506) | `03959C` | none | `GetStage GreaterThanOrEqualTo 30`; `GetStage LessThanOrEqualTo 50`; `GetIsAliasRef alias #4` | Prompt: "Tell me about Maiden Statue" Response (1): [Huh? Are you talking statue carved into stone wall? ... Is from the old days of solitary confinement.](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:507) Response (2): [According to the old records ... When there was a castle was built or made ??of what purpose is unknown.](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:508) |
| [`03959E zzzBMMq01B04stThePast`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:510) | `03959F` | none | `GetStage GreaterThanOrEqualTo 30`; `GetStage LessThanOrEqualTo 40`; `GetIsAliasRef alias #4` | Prompt: "Did not a similar incident happened in the past?" Response (1): [Once some years ago ... 20. There was a prisoner that is gone all ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:511) Response (2): [At that time, the maintenance of security is such because it was left to long Just You guard in turmoil after the war. Do not know the details of the](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:512) Response (3): [I remember that the keeper of Stendhal's about were sent several people from church ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:513) |
| [`0395A0 zzzBMMq01B04stReport`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:515) | `0395A1` | SayOnce | `GetIsAliasRef alias #4`; VMAD `BM01_TIF__010395A1.Fragment_0` on end | Prompt: "Do not document the incident left?" Response: [Not here. One such article reviews keeper Jacob is such that I have brought back from all ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:516) |
| [`0395A3 zzzBMMq01B05stVampire`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:518) | `0395A4` | SayOnce | `GetStage EqualTo 50`; `GetIsAliasRef alias #4`; VMAD `BM01_TIF__010395A4.Fragment_0` on end | Prompt: "Vampire appeared in the past. This also would be the work of a vampire" Response (1): [The vampires? ... Never report such a length guards ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:519) Response (2): [Whatever happens in the vampire opponent can not be helped for us. Left up to you experts](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:520) |
| [`0395A6 zzzBMMq01B06stDefeated`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:522) | `0395A7` | SayOnce | `GetStage EqualTo 100`; `GetIsAliasRef alias #4`; VMAD `BM01_TIF__010395A7.Fragment_0` on end | Prompt: "Defeated vampires" Response (1): [Yeah, such must be grateful to you. This is a reward. I want to receive a lesser](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:523) Response (2): [Also, in our keeper visited Windhelm, I keep talking about the cathedral of you and Stendhal. They should have already arrived at the Cathedral](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:524) Response (3): [We look forward to your future success, and that there is no guidance of Stendhal to you](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:525) |

### Branch 3: Library Guard (Alias #7)

Branch:
- `043F01:Vigilant.esm` (inferred from INFO `043F03`) and `043F04:Vigilant.esm` (inferred from INFO `043F06`)

Speaker condition pattern:
- INFOs require `GetIsAliasRef == 1` on alias `#7` (Library Guard, likely `zzzBMMq01LibraryGuard`).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`043F02 zzzBMMq01B1LibGoWindhelm`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:703) | `043F03` | Goodbye, SayOnce | `GetStage LessThanOrEqualTo 10`; `GetIsAliasRef alias #7` | Prompt: "I am going to Windhelm. Are you OK?" Response: [Take care of yourself. I'll be fine, it seems he is living me Mr. guards](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:704) |
| [`043F05 zzzBMMq01B2LibReport`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:706) | `043F06` | none | `GetStage EqualTo 40`; `GetIsAliasRef alias #7` | Prompt: "Where is Windhelm report?" Response: [180's I would say 20 years ago. I think there was a shelf in the middle of the room](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:707) |

Translation notes:
- The Library Guard's dialogue has poor extraction quality; "180's" may be a year (1E180 in lore years?) and needs ESM verification.
- Response in `043F06` suggests the guard is describing where to find records, implying this dialogue happens in or near the Mage's College library, not the Temple of Stendarr proper.

## Related Records

These are not all part of quest `038524` according to `infodiag`, but they are contextual to the Windhelm dungeon investigation and vampire storyline.

NPCs:
- `zzzBMMq01Courier` (alias #2) - editorID not yet verified; FormID TBD
- `zzzBMMq01Steward` (alias #4) - likely Windhelm Guard Chief or similar; FormID TBD
- `zzzBMMq01LibraryGuard` (alias #7) - likely a Mage's College member or Stendarr priest; FormID TBD

Locations (inferred from objectives):
- Windhelm Dungeon (objective 20: "Search Windhelm Dungeon")
- Temple of Stendarr, Windhelm (objective 40: "Find Windhelm Report in Temple of Stendarr")
- Unspecified vampire location (objective 60: "Defeat Vampires under Windhelm")

## Reconstruction Notes

Source-grounded:
- This SideQuest is represented by [`038524 zzzBMMq01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) with name `"Empty Jails"`.
- It contains 8 objectives spanning stage 0 to 100.
- It has no SCEN records; all storytelling is via dialogue INFOs.
- It contains three distinct dialogue branches:
  - Courier branch (alias #2), opening at stage 0.
  - Steward/Guard Chief branch (alias #4), opening at stage 10 and stage-gated through stages 20–50–100.
  - Library Guard branch (alias #7), appearing early (stage ≤10) and at stage 40 (report quest).
- Multiple dialogue INFOs carry VMAD scripts (fragments `BM01_TIF__01038ABA`, `BM01_TIF__01038AC5`, `BM01_TIF__010395A1`, `BM01_TIF__010395A4`, `BM01_TIF__010395A7`), indicating that some choices likely trigger SetStage or CompleteQuest logic. Exact Papyrus behavior is not decoded here.

Stage progression (inference):
- **Stage 0**: StartUpStage. Courier greeting dialogue available.
- **Stage 10**: Steward introduction; objective 0 (Talk to Courier) likely complete. Objective 10 (Talk to Steward) activates. Library Guard's farewell dialogue available.
- **Stage 20**: Dungeon search begins; objective 20 activates.
- **Stage 30–40**: Investigation deepens; Maiden Statue dialogue available (stage 30–50 range). Past incident dialogue available (stage 30–40 range). Report quest dialogue available (stage 40). Objective 30 and 40 activate.
- **Stage 50**: Vampire dialogue becomes available (stage 50). Objective 50 (Talk to Steward again) activates.
- **Stage 60**: Vampire combat begins; objective 60 activates.
- **Stage 100**: Final report dialogue available. Objective 100 activates. Vampire defeat dialogue available.
- **Stage 110, 255, 9999**: CompleteQuest flags suggest multiple possible completion paths; exact routing requires VMAD fragment decompilation.

Open verification:
- Decompile VMAD fragments `BM01_TIF__01038ABA`, `BM01_TIF__01038AC5`, `BM01_TIF__010395A1`, `BM01_TIF__010395A4`, `BM01_TIF__010395A7` to understand choice → SetStage → CompleteQuest routing and whether stage progression is linear or branching.
- Verify alias FormIDs (#2 Courier, #4 Steward, #7 Library Guard) and their NPC records if their dialogue behavior is complex.
- Verify objective target refs (especially objectives 20 and 60) if spatial staging matters for quest markers or combat arenas.
- Verify the meaning of stage 110, 255, and 9999 CompleteQuest flags if multiple endings exist (good, bad, neutral).
- Extract and review the Windhelm Dungeon cell layout and the vampire lair location from ESM cell records if quest flow requires spatial knowledge.
