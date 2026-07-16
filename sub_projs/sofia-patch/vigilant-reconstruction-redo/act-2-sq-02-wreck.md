# Act 2 Sq02 - The Wreck

Status: first redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue or a specific condition.
- CLI diagnostics provide definitive stage/objective/conditions data.
- Scene staging comes from `scenediag` CLI output (phases, actions, actor aliases).

## Quest Record

[`038525 zzzBMMq02 "The Wreck"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:108)

CLI:
- `questdiag Vigilant.esm 0x038525`
- `infodiag Vigilant.esm 0x038525`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x038525`
- EditorID: `zzzBMMq02`
- Name: `The Wreck`
- Flags: `RunOnce`
- Priority: `90`
- Type: `SideQuest`
- Filter: `BM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 10 | none | empty |
| 20 | none | empty |
| 30 | none | empty |
| 40 | none | empty |
| 50 | none | empty |
| 60 | none | empty |
| 65 | none | empty |
| 70 | none | empty |
| 80 | none | empty |
| 90 | none | empty |
| 100 | CompleteQuest | empty |
| 9999 | CompleteQuest | empty |

Objectives:

| Index | Source | Quest Text |
|---:|---|---|
| 10 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:109) | Defeat `<Alias=Vamp01>` |
| 30 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:110) | Defeat `<Alias=Vamp02>` |
| 50 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:111) | Defeat `<Alias=Vamp03>` |
| 60 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:112) | Talk to `<Alias=Vamp04Ess>` |
| 70 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:113) | Give `<Alias=Vamp04>` Mercy of Stendarr |
| 90 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:114) | Defeat `<Alias=Vamp05>` |

Objective targets:
- Objectives 10, 30, 50, 70 each have 1 target in ESM.
- Objectives 60, 90 have 0 targets.
- (inference: targets are likely vampire combat refs; exact locations require deeper dump if spatial staging matters.)

## Alias / Staging Backbone

From `infodiag` output, the quest owns 14 dialogue topics. Two of these are scene topics (no EditorID):
- Topic `0x03A0C4` (scene action)
- Topic `0x03A0C6` (scene action)
- Topic `0x03A0C8` (scene action)

The remaining 11 topics are custom dialogue branches organized by alias indices referenced in INFO conditions:
- Alias `#1` (inferred: `Vamp04Ess` — the Essential survivor)
- Alias `#5` (inferred: `Vamp04` — the non-essential vampire)

(inference: Other alias indices #2, #3, #4 likely represent the three vampires `Vamp01`, `Vamp02`, `Vamp03` referenced in objectives 10, 30, 50; alias indexing may be implicit from FormID order or quest creation order.)

## Custom Dialogue Branches

This quest has two main dialogue branches, both stage-gated and associated with specific vampire aliases.

### Branch 1: Vamp04Ess (Alias #1, Possible Survivor)

Branch:
- `039B39:Vigilant.esm` (root, inferred from topic ownership pattern)

Speaker condition pattern:
- INFOs require `GetIsAliasRef == 1` on alias `#1` (`Vamp04Ess`).
- Stage gates vary per topic.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`039B3A zzzBMMq02B01v2FearGreet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:527) | `039B3B` | WalkAway | `GetStage EqualTo 20`; `GetIsAliasRef alias #1` | [I'm not angry anymore? Aredhel I finally went back to sanity ... do we Gwaji. Well, let's go back ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:528) |
| [`039B3C zzzBMMq02B01v2AreUOK`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:530) | `039B3D` | Goodbye, CanMoveWhileGreeting | `GetIsAliasRef alias #1` | Prompt: "Are you OK?" Response: [If you go first monster! Come near! Away!! Come! Come! Ah Ah Come!](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:531) VMAD: `BM02_TIF__01039B3D.Fragment_0` on end. |

Translation notes:
- "Aredhel" and "Gwaji" are possibly names or terms untranslated in the extracted text; may require NPC/place verification.
- "go back to sanity" suggests a character regaining consciousness or normal state after a trauma/possession.

### Branch 2: Vamp04 (Alias #5, Non-Essential Vampire)

Branch:
- `039B41:Vigilant.esm` (root, inferred from topic ownership pattern)

Speaker condition pattern:
- INFOs require `GetIsAliasRef == 1` on alias `#5` (`Vamp04`).
- Heavy stage gating (20, 60, 65, 70 ranges).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`039B42 zzzBMMq02B01v4Greet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:533) | `039B43` | Goodbye | `GetStage LessThanOrEqualTo 40`; `GetIsAliasRef alias #5` | [Bad now, I do not know when Jericho is coming](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:534) VMAD: `BM02_TIF__01039B43.Fragment_0` on end. |
| [`039B45 zzzBMMq02B02v4Happen`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:537) | `039B46` | SayOnce | `GetStage EqualTo 60`; `GetIsAliasRef alias #5` | Prompt: "What happened here?" Response (1): [Her. She ached. There was a lady of the blood ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:538) Response (2): [We have to defeat her. I can not do anything. All, a vampire ... been Nomasa those who survived, and ... the blood of her](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:539) VMAD: `BM02_TIF__01039B46.Fragment_0` on end. |
| [`039B48 zzzBMMq02B03v4Vampirism`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:541) | `039B49` | none | `GetStage EqualTo 65`; `GetIsAliasRef alias #5` | Prompt: "Do not you Naose are blood-sucking disease?" Response (1): [It is too late. While it's thirst for blood is strong. And above all she is with joy that has become strong ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:542) Response (2): [Every night, a dream that she shed tears of blood. From that day she was crying she was polluted much to ... Molag Bal](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:543) Response (3): [I know her grief is still sleeping to take in hand. In me has become irreplaceable she ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:544) |
| [`039B4B zzzBMMq02B04v4Imprison`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:546) | `039B4C` | none | `GetStage EqualTo 65`; `GetIsAliasRef alias #5` | Prompt: "Why are you being imprisoned?" Response: [Which refused her blood is imprisoned here. It's all the way to accept the blood ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:547) |
| [`039B4E zzzBMMq02B05v4OtherVigilant`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:549) | `039B4F` | none | `GetStage EqualTo 65`; `GetIsAliasRef alias #5` | Prompt: "What was the other keeper?" Response: [Everyone survive had been a vampire. Everyone has been in her blood Kuruwasu](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:550) |
| [`039B51 zzzBMMq02B06v4AboutMatron`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:552) | `039B52` | none | `GetStage EqualTo 65`; `GetIsAliasRef alias #5` | Prompt: "Blood Matron?" Response: [Nede maiden soiled to Molag Bal. It is the founder of the vampire universally.](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:553) |
| [`039B54 zzzBMMq02B07v4Help`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:555) | `039B55` | WalkAway | `GetStage EqualTo 65`; `GetIsAliasRef alias #5` | Prompt: "Is there anything I can do?" Response: [I want you to kill me. I want to die before you become exhausted bloodthirsty beast ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:556) |
| [`039B56 zzzBMMq02B07v4Kill`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:558) | `039B57` | Goodbye | `GetStage EqualTo 65`; `GetIsAliasRef alias #5` | Prompt: "OK.I kill you" Response: [Well ... thank you, kill me soon. I'm not likely to endure the thirst of blood anymore ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:559) VMAD: `BM02_TIF__01039B57.Fragment_1` on begin, `BM02_TIF__01039B57.Fragment_0` on end. |
| [`039B58 zzzBMMq02B07v4NotKill`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:561) | `039B59` | none | `GetStage EqualTo 65`; `GetIsAliasRef alias #5` | Prompt: "I can not" Response: [I beg you ... I beg. I want to be free from this suffering](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:562) |
| [`03A0C2 zzzBMMq02B01v4GreetEnd`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:?) | `03A0C2` | Goodbye | `GetStage EqualTo 70`; `GetIsAliasRef alias #5` | Response: [Kill me...plaese, kill me...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:?) VMAD: `BM02_TIF__0203A0C2.Fragment_0` on end. |

Translation notes:
- "Naose" in the "blood-sucking disease" prompt is unclear; may be a mistranslation or transliteration of a creature name.
- "Nomasa" in the "What happened here?" response is unclear; possibly a name or dialect term.
- "Kuruwasu" in the "other keeper" response is unclear; possibly a distorted name or term.
- "Jericho" in the "Greet" response may be a reference to a Biblical or in-lore figure (Jericho Lord, quest giver?); requires verification.

## Scene Records

Three scene action topics are part of this quest. Their underlying SCEN records are not present in the extracted text; only the dialogue actions and translations are available.

Scene topics:
- `0x03A0C4` — first scene monologue (INF0 `0x03A0C5`)
- `0x03A0C6` — second scene monologue (INFO `0x03A0C7`)
- `0x03A0C8` — third scene monologue (INFO `0x03A0C9`)

| Topic | INFO | Emotion | Translation |
|---|---|---|---|
| `0x03A0C4` (scene) | `0x03A0C5` | Sad | [I missed everyone ... everyone ... died, had been eating ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:?) |
| `0x03A0C6` (scene) | `0x03A0C7` | Fear | [And she and I ... are you here now just Ganzen monster ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:?) |
| `0x03A0C8` (scene) | `0x03A0C9` | Anger | [you monster Molag Bal, you'll only have to die for the peace of soul not forgive! Friend of late!](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:?) |

(inference: These are likely scene monologue actions, one per phase or actor, representing the survivor's anguished memories or present state during the encounter with the vampire and the wreck site.)

## Related Records

NPCs:
- `Vamp01`, `Vamp02`, `Vamp03` — three vampires (aliases #2, #3, #4); FormIDs unknown.
- `Vamp04` (alias #5) — non-essential vampire, the primary speaker; FormID `03A0C2` or nearby?
- `Vamp04Ess` (alias #1) — essential survivor; FormID unknown.

Items:
- [`Mercy of Stendarr`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:?) — quest item to be given at objective 70.

Locations (inferred):
- "The Wreck" (unnamed location; quest name suggests a destroyed ship or building site).
- Windhelm area (continuation of Act 2 geography from sq01).

## Reconstruction Notes

Source-grounded:
- This SideQuest is represented by [`038525 zzzBMMq02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:108) with name `"The Wreck"`.
- It contains 6 objectives spanning stages 10–90 with two CompleteQuest gates at stages 100 and 9999.
- It has three scene action topics (`0x03A0C4`, `0x03A0C6`, `0x03A0C8`); exact SCEN staging is not yet available from extracted text.
- It contains two distinct dialogue branches:
  - Vamp04Ess branch (alias #1), opening at stage 20 with fear-based greeting.
  - Vamp04 branch (alias #5), spanning stages 0 (initial greet) → 60 (exposition) → 65 (vampire condition dialogue) → 70+ (final state or death choice).
- Multiple dialogue INFOs carry VMAD scripts (fragments `BM02_TIF__01039B3D`, `BM02_TIF__01039B43`, `BM02_TIF__01039B46`, `BM02_TIF__01039B57`, `BM02_TIF__0203A0C2`), indicating stage progression, choice routing, or outcome logic.

Stage progression (inference):
- **Stage 0**: Initial encounter at the wreck site; first greeting from Vamp04 available.
- **Stages 10–50**: Combat objectives (Vamp01, Vamp02, Vamp03) activated and defeated in sequence.
- **Stage 60**: Final exposition dialogue; Vamp04 reveals the "lady of blood" and the vampire origin story. Objective 60 (Talk to Vamp04Ess) activates.
- **Stage 65**: Vampire condition dialogue becomes available; player can choose to kill Vamp04 or show mercy. Objectives 65–70 gated.
- **Stage 70**: Aftermath; Vamp04's final state (dead or transformed?). Objective 90 (Defeat Vamp05) may activate.
- **Stage 80**: Unclear; no objectives gated here.
- **Stage 90**: Final defeat objective activated.
- **Stages 100 / 9999**: CompleteQuest flags; exact routing requires VMAD decompilation.

Open verification:
- Decompile VMAD fragments `BM02_TIF__01039B3D`, `BM02_TIF__01039B43`, `BM02_TIF__01039B46`, `BM02_TIF__01039B57`, `BM02_TIF__0203A0C2` to understand choice → SetStage → objective/CompleteQuest routing.
- Run `scenediag Vigilant.esm 0x<SCEN_FormID>` for each scene topic `0x03A0C4`, `0x03A0C6`, `0x03A0C8` if their FormIDs can be extracted from the ESM; will reveal phases, actions, actor aliases, and timing.
- Verify alias FormIDs (#1 Vamp04Ess, #5 Vamp04, #2–#4 Vamp01–Vamp03) and their NPC dialogue traits.
- Verify locations: is "The Wreck" a named cell/worldspace in the Windhelm area? Check cell records.
- Verify the "Mercy of Stendarr" item FormID and its use case (blessing? removal? generic quest item?).
- Clarify untranslated terms: "Jericho," "Naose," "Nomasa," "Kuruwasu," "Ganzen."
