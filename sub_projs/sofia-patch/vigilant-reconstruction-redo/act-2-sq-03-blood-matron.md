# Act 2 Sq03 - Blood Matron

Status: first redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue or a specific condition.
- CLI diagnostics provide definitive stage/objective/conditions data.

## Quest Record

[`038526 zzzBMMq03 "Blood Matron"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:260)

CLI:
- `questdiag Vigilant.esm 0x038526`
- `infodiag Vigilant.esm 0x038526`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x038526`
- EditorID: `zzzBMMq03`
- Name: `Blood Matron`
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
| 70 | none | empty |
| 80 | none | empty |
| 90 | none | empty |
| 100 | CompleteQuest | empty |
| 200 | none | empty |
| 210 | none | empty |
| 220 | CompleteQuest | empty |
| 9999 | CompleteQuest | empty |

Objectives:

| Index | Source | Quest Text |
|---:|---|---|
| 60 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:260) | Defeat `<Alias=LamaeBal>` |
| 90 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:260) | Break the Curse of `<Alias=MolagBal>` |

Objective targets:
- Objective 60 has 1 target.
- Objective 90 has 2 targets.
- (inference: target refs are likely the two main antagonists, Lamae Bal and Molag Bal, or their associated locations/items; exact refs require deeper dump if spatial staging matters.)

## Alias / Staging Backbone

The main quest has multiple dialogue branches targeting three primary aliases: `LamaeBal` (alias #1), `MolagBal` (alias #0), and `LoveBound` (alias #2). The `MolagBal` alias is associated with a vampire spawn that can be corrupted into a Daedroth, and `LamaeBal` appears to be Lamae Bal in her blood-curse form.

Host quest:
- [`038526 zzzBMMq03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:260)

Alias summary from `infodiag`:
- Alias #0: `MolagBal` (references found in branch `zzzBMMq03B01mbGreet`)
- Alias #1: `LamaeBal` (referenced in Hello topic `zzzBMMq03HelloVamp` via `GetIsID` checks)
- Alias #2: `LoveBound` (referenced in branch `zzzBMMq03B01LBGreet`, stage-gated at 50 and >=200)

Inference:
- The quest progresses through dialogue and choice points at stages 30, 50, 200.
- Stage 100 and 220 mark completion branches (two paths possible).
- Stage 9999 is likely a cleanup/shutdown stage.

## Scene Records

No scene (SCEN) records are directly attached to this quest according to `infodiag`. All staging is dialogue-driven.

Scene reference found:
- `TOPIC 0x03D77F` (Scene type, no EditorID, owned by quest `038526`)
  - `INFO[0] 0x03D780`: [Lamae, wake up. Happening, so ... if you bite the throat of the guy tearing, I dream I'll show you again](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:635)
  - This appears to be a narration or activation cue rather than a traditional scene; full `scenediag` on this topic FormID returns "not a Scene in Vigilant.esm", suggesting it may be a dialogue-only trigger.

## Hello Topic: Vampire Spawn Responses

[`03AE0F zzzBMMq03HelloVamp`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:573)

Speaker condition pattern:
- Each INFO is conditioned on `GetIsID` with a distinct NPC FormID (not alias-based).
- Speaker variants (5 total INFOs): each one addresses a different vampire spawn identity.

| INFO | NPC FormID (GetIsID) | Translation |
|---|---|---|
| `03AE10` | `0392A5:Vigilant.esm` | [There is no hesitation. You ... Cardinal, because it was chosen to Moragu Baru today.](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:573) |
| `03AE11` | `0392A6:Vigilant.esm` | [Free! Have the freedom in this destination!'ve Got an outlet that has been pursued all the way!](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:573) |
| `03AE12` | `039824:Vigilant.esm` | [And I'll welcome you. Well, I baptized. Lineage Ramae, and he put together in his name](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:573) |
| `03AE13` | `039825:Vigilant.esm` | [The Ukero the baptism. That way, it becomes a thing of you at night](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:573) |
| `03AE14` | `039828:Vigilant.esm` | [The drink the blood of Ramae. Her blood would promises eternity to you.](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:573) |

Translation notes:
- Extraction quality is poor; "Moragu Baru" likely corrupted from "Molag Bal".
- "Ramae" is likely "Lamae" (the Blood Matron).
- "Ukero" and related terms are unclear and may require ESM decode verification.

## Custom Dialogue Branch: Lamae Bal (Alias #1)

Branch:
- Root editorID: `zzzBMMq03B01lhGreet` branch (formID unspecified in CLI, inferred from topic structure)

Speaker condition pattern:
- INFOs require `GetIsAliasRef == 1` on alias #1 (`LamaeBal`).
- Some INFOs are stage-gated at 30.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`03BC14 zzzBMMq03B01lhGreet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:602) | `03BC15` | none | `GetStage EqualTo 30`; `GetIsAliasRef alias #1` | [Proceeding did wrong? Early, the day I would come to an end](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:602) |
| [`03BC16 zzzBMMq03B01WhoRU`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:605) | `03BC17` | Goodbye | `GetIsAliasRef alias #1` | Prompt: "Who Are you?" Response: [I Lamae, Lamae you. Did you forget?](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:605) |
| [`03BC18 zzzBMMq03B01lhDestination`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:608) | `03BC19` | none | `GetIsAliasRef alias #1` | Prompt: "Where do you go?" Response: [Wasuren monk? Probably the place to go to the castle of your father](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:608) |
| [`03BC1A zzzBMMq03B01lfFather`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:611) | `03BC1B` | Goodbye | `GetIsAliasRef alias #1` | Prompt: "Who is your father?" Response: [There is not a thing once? Did forget, I met another](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:611) |

Translation notes:
- "Wasuren monk" is unclear; may be a proper noun (place or faction) or corrupted text.
- "castle of your father" likely refers to a Daedric stronghold, possibly linked to Molag Bal's realm.

## Custom Dialogue Branch: Molag Bal (Alias #0)

Branch:
- Root editorID: `zzzBMMq03B01mbGreet` branch (formID unspecified)

Speaker condition pattern:
- Most INFOs require `GetIsAliasRef == 1` on alias #0 (`MolagBal`).
- Opening line requires `GetStage EqualTo 50`.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`03BC21 zzzBMMq03B01mbGreet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) | `03BC22` | Goodbye, SayOnce | `GetStage EqualTo 50`; `GetIsAliasRef alias #0` | Prompt: (implicit) Response (1): [Came well, son of Stendhal. Molag Bal will welcome this, the differents](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) Response (2): [It's what, and Minuka to live eternity with this daughter leave this? Happiness of this daughter was also the hope of Stendhal](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) Response (3): [There is no need to abandon their faith. Just choose, cleave, do it just](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) |
| [`03BC23 zzzBMMq03B01mbGreet (continued)`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) | `03BC23` | Goodbye | `GetStage EqualTo 50`; `GetIsAliasRef alias #0` | Prompt: (implicit) Response: [The clove will not hesitate should you know?, The road I'm sure if one](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) |
| Branch continues at stage 200 | `03BC24` | Goodbye, SayOnce | `GetStage EqualTo 200`; `GetIsAliasRef alias #0` | Prompt: (implicit) Response (1): [Departure of two people, Molag Bal will bless this. You are good beyond the management of Akei, but live forever](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) Response (2): [Now, into the castle. I'll have baptized](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) VMAD: `BM03_TIF__0103BC24.Fragment_0` on end |
| | `03BC25` | Goodbye | `GetStage EqualTo 210`; `GetIsAliasRef alias #0` | Prompt: (implicit) Response: [What I was placed into the castle? Baptism preparation is made](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) |

Translation notes:
- "Minuka" is unclear; may be a character name or concept.
- "Akei" likely refers to "Arkay", the divinity of death and mortality.
- "cleave, do it just" is awkward; may mean "choose, do it now" or similar.
- Multiple responses suggest player choice branching at stage 50.

## Custom Dialogue Branch: Love Bound / Lamae Vampire Form (Alias #2)

Branch:
- Root editorID: `zzzBMMq03B01LBGreet` branch (formID unspecified)

Speaker condition pattern:
- INFOs require `GetIsAliasRef == 1` on alias #2 (`LoveBound`).
- Opening lines are stage-gated at 50 and >=200.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`03C190 zzzBMMq03B01LBGreet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:623) | `03C191` | none | `GetStage EqualTo 50`; `GetIsAliasRef alias #2` | [Now, let's go together. Everyone, I'm blessed me that our](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:623) |
| | `03C196` | Goodbye | `GetStage EqualTo 60`; `GetIsAliasRef alias #2` | [I'll tear. Nasai prepared](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:623) |
| | `03C197` | Goodbye | `GetStage GreaterThanOrEqualTo 200`; `GetIsAliasRef alias #2` | [Forever, everywhere, there will be both](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:623) |
| [`03C192 zzzBMMq03lbNoMonster`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:628) | `03C193` | Goodbye, SayOnce | `GetIsAliasRef alias #2` | Prompt: "Get lost, monster" Response (1): [I just like him ... really cold eye. I wonder respond with a dagger to my love too?](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:628) Response (2): [I can not run away from, what it does not let go absolutely. I take even picked a limb](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:628) VMAD: `BM03_TIF__0103C193.Fragment_0` on end |
| [`03C194 zzzBMMq03B01LBletGo`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:632) | `03C195` | Goodbye | `GetIsAliasRef alias #2` | Prompt: "Ok, let's go" Response: [Happy. I always will be together. Forever and ever, forever ...](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:632) VMAD: `BM03_TIF__0103C195.Fragment_0` on end |

Translation notes:
- "Nasai" is unclear; possibly a name or corruption.
- The dialogue emphasizes emotional bonds ("love", "together", "forever"), suggesting romantic or obsessive themes.
- Player choice at "Get lost, monster" vs. "Ok, let's go" appears to branch outcomes.

## Related Records

These are not all part of quest `038526` according to `infodiag`, but they are contextual NPCs and items for the Blood Matron storyline.

NPCs (from game-data/npcs.tsv):
- [`037468 zzzBMLamaeBal`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:891) - Lamae Bal (the Blood Matron herself; alias #1 likely points here)
- [`0368E0 zzzBMMolagBalHuman`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:897) - Molag Bal (human form; alias #0 likely points here)
- [`036ECD zzzBMMolagBalSonBadEnd`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:895) - Daedroth Lord (possible transformation of Molag Bal spawn; formID `0x036ECD`)
- Vampire spawns (various forms): `zzzBMLamaeVampFeral`, `zzzBMLamaeBeolfag`, `zzzBMLamaeVampLich`, `zzzBMLamaeVampTroll`
- [`03748D zzzBMLamaeZombie`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:890) - Lamae Zombie form

Items:
- [`03B675 zzzBMMolagBalCurseofLamae`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:967) - Curse of Molag Bal (likely the object that objective 90 targets)

## Reconstruction Notes

Source-grounded:
- This SideQuest is represented by [`038526 zzzBMMq03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:260) with name `"Blood Matron"`.
- It has two main objectives: defeat Lamae Bal (objective 60) and break her curse (objective 90).
- It has no SCEN records; all staging is dialogue-driven across Hello topic and four custom branches.
- The quest has three principal dialogue branches:
  - Lamae Bal branch (alias #1), opening at stage 30 with identity and location questions.
  - Molag Bal branch (alias #0), opening at stage 50 with three-option greeting, resuming at stage 200 with completion dialogue.
  - Love Bound / Vampire Lamae branch (alias #2), gated at stages 50, 60, and >=200, with a player choice to reject ("Get lost, monster") or accept ("Ok, let's go").
- Multiple VMAD fragments exist on stage-completing INFOs, indicating Papyrus scripts drive progression or outcome branches.

Branch polarity:
- Molag Bal stage 50 greeting offers three responses, suggesting player agency in accepting/rejecting Molag's offer.
- Love Bound branch offers explicit "monster rejection" vs. "acceptance" choice paths.
- Completion stages 100, 220, and 9999 suggest multiple endings possible (defeat Lamae, or become corrupted/bound).

Open verification:
- Decode VMAD fragments on INFOs `03BC24`, `03C193`, `03C195` if Papyrus source is available.
- Verify NPC FormID links for aliases #0, #1, #2 in the quest record itself (CLI `questdiag` does not print full alias list).
- Inspect objective target refs (quest stage 60 / 90 targets) to confirm they point to Lamae Bal and curse item respectively.
- Confirm the nature of the scene-like topic `0x03D77F`; it may be a trigger for a follow-up dialogue or cinematic cue.
- Verify "Wasuren monk", "Minuka", "Akei", "Nasai", "Ukero" terms against Japanese source text if available, or note as unresolved extraction artifacts.
