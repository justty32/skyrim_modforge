# Act 1 Side Quest 01 - Squeezer

Status: first redo slice. Source-grounded, link-first, no Gemini.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain context or conditions.
- No `SCEN` staging present; dialogue-driven quest with linear stage progression.

## Quest Record

[`005CE3 zzzAoMMq01 "Squeezer"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:288)

CLI:
- `questdiag Vigilant.esm 0x005CE3`
- `infodiag Vigilant.esm 0x005CE3`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x005CE3`
- EditorID: `zzzAoMMq01`
- Name: `Squeezer`
- Flags: `RunOnce`
- Priority: `90`
- Type: `SideQuest`
- Filter: `AoM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 10 | none | empty |
| 11 | none | empty |
| 15 | none | empty |
| 20 | none | empty |
| 30 | none | empty |
| 40 | none | empty |
| 50 | CompleteQuest | empty |
| 255 | ShutDownStage | empty |
| 9999 | CompleteQuest | empty |

Objectives:

| Index | Source | Text |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:289) | Talk to Altano |
| 10 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:290) | Follow Altano or Join Altano at Hall of Dead |
| 15 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:291) | Talk to Altano |
| 20 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:292) | Search Vampire |
| 30 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:293) | Defeat Vampire |
| 40 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:294) | Report to Altano |

Objective targets:
- Objective 0: 1 target with 0 conditions.
- Objective 10: 2 targets with 0 conditions each.
- Objective 15: 1 target with 0 conditions.
- Objective 20: 0 targets.
- Objective 30: 1 target with 0 conditions.
- Objective 40: 1 target with 0 conditions.
- Current CLI output does not print target cell/ref details; this needs a deeper QUST target dump if location targeting matters.

## Alias / Staging Backbone

No custom `SCEN` records detected by `infodiag`. Stage progression appears linear through dialogue conditions.

Host quest:
- `005CE3 zzzAoMMq01` "Squeezer"

Dialogue aliases from `infodiag`:
- Alias `#0`: expected to be `Altano` (main quest-giver).
- Alias `#1`: expected to be a prostitute NPC (target/suspect in the vampire plot).

(inference: alias roles inferred from dialogue conditions `GetIsAliasRef` indices 0 and 1; no explicit alias dump available from CLI)

## Custom Dialogue Branches

### Branch 1: Quest Opener — "Can I help you?"

TOPIC `0x006258 zzAoMMq01B1Mission1`

Condition pattern:
- `GetStage < 10`: fires before player advances past the initial conversation.
- `GetIsAliasRef alias #0` (Altano).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x006258 zzAoMMq01B1Mission1` | `0x006259` | none | `GetStage < 10`; `GetIsAliasRef alias #0` | Prompt: [`"Can I help you?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:14) Response (Neutral): [`"There is the request of Arkay priest in Whiterun. Vampire appears."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:15) Response (Neutral): [`"Can you assist me? Get ready for a journey immediately, vampire do not wait for us."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:16) |

VMAD Fragment:
- `AoM01_TIF__01006259` (triggers `OnEnd` fragment)
- (inference: fragment likely sets stage 10+ to advance quest)

### Branch 2: Investigation Phase — "Tell me about the incident"

TOPIC `0x00625B zzAoMMq01B2AboutCrime`

Condition pattern:
- `GetStage == 15`: fires during investigation phase.
- `GetInCell 0x0165AA` (Skyrim.esm, presumed to be a crime scene in Whiterun).
- `GetIsAliasRef alias #0` (Altano).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x00625B zzAoMMq01B2AboutCrime` | `0x00625C` | none | `GetStage == 15`; `GetInCell 0x0165AA`; `GetIsAliasRef alias #0` | Prompt: [`"Tell me about the incident."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:18) Response (Happy): [`"Every victims are squeeze all blood. This is act of vampire, Novice Vampire"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:19) Response (Neutral): [`"I'll try to examine the documents in here a little longer. You are looking for suspicious person in the town."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:20) |

VMAD Fragment:
- `AoM01_TIF__0100625C` (triggers `OnEnd` fragment)
- (inference: fragment advances stage to 20)

Translation notes:
- `squeeze all blood` is idiomatic; English text is agrammatical, but intent is "drained all blood" or "sucked all blood".

### Branch 3: Victim Analysis — "Tell me about victims"

TOPIC `0x00625E zzAoMMq01B3AboutVictims`

Condition pattern:
- `GetStage == 20`: fires during victim analysis phase.
- `GetIsAliasRef alias #0` (Altano).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x00625E zzAoMMq01B3AboutVictims` | `0x00625F` | none | `GetStage == 20`; `GetIsAliasRef alias #0` | Prompt: [`"Tell me about victims"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:22) Response (Sad): [`"There is nothing in common among victims magically....hmm...something in common is male."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:23) |

### Branch 4: Authority Question — "We do not need Jarl's permission?"

TOPIC `0x006261 zzAoMMq01B4AboutAuthority`

Condition pattern:
- `GetStage == 20`: fires during victim analysis phase.
- `GetIsAliasRef alias #0` (Altano).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x006261 zzAoMMq01B4AboutAuthority` | `0x006262` | none | `GetStage == 20`; `GetIsAliasRef alias #0` | Prompt: [`"We do not need Jarl's permission?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:25) Response (Happy): [`"Our Activities are accepted in Skyrim. one of reason is a shorthanded by Cuvil War. Anyway, we are welcomed now."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:26) |

Translation notes:
- `Cuvil War` is presumed to be a mistranscription or localized name; likely refers to the "Civil War" in Skyrim.

### Branch 5: Vampire Type Question — "You said Novice vampire...Why?"

TOPIC `0x006264 zzAoMMq01B5WhyNovice`

Condition pattern:
- `GetStage == 20`: fires during victim analysis phase.
- `GetIsAliasRef alias #0` (Altano).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x006264 zzAoMMq01B5WhyNovice` | `0x006265` | none | `GetStage == 20`; `GetIsAliasRef alias #0` | Prompt: [`"You said Novice vampire...Why?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:28) Response (Happy): [`"Victims are found everyday. the most of vampire behave flamboyantly is novice."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:29) |

### Branch 6: Suspect Branch — Prostitute Encounter

TOPIC `0x006267 zzAoMMq01B6Whore`

Condition pattern:
- `GetStage == 20`: fires during investigation.
- `GetGlobalValue 0x000038 >= 6` AND `<= 21` (Skyrim.esm; global appears to be time-of-day or racial time check).
- `GetIsAliasRef alias #1` (prostitute NPC; suspect/vampire in disguise).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x006267 zzAoMMq01B6Whore` | `0x006268` | Goodbye | `GetStage == 20`; `GetGlobalValue 0x000038` [6–21]; `GetIsAliasRef alias #1` | Prompt: (none) Response (Neutral): [`"Come again in the night. I will give you a delicious sweetroll."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:32) |
| `0x006267 zzAoMMq01B6Whore` | `0x006269` | WalkAway | `GetStage == 20`; `GetIsAliasRef alias #1` | Prompt: (none) Response (Neutral): [`"Do you like sweetroll? My sweetroll is delicious?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:33) |

### Branch 7: Sweetroll Accept — "Take one"

TOPIC `0x00626A zzAoMMq01B6Yes`

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x00626A zzAoMMq01B6Yes` | `0x00626B` | Goodbye | `GetIsAliasRef alias #1` | Prompt: [`"Take one"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:35) Response (Neutral): [`"Thank you. Could you close youe eye for a while? I am very shy...."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:36) |

VMAD Fragment:
- `AoM01_TIF__0100626B` (triggers `OnEnd` fragment)
- (inference: fragment executes vampire attack or transition to stage 30)

Translation notes:
- `youe` is a typo in the original source, intended as "your".

### Branch 8: Sweetroll Reject — "unnecessary"

TOPIC `0x00626C zzAoMMq01B6No`

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x00626C zzAoMMq01B6No` | `0x00626D` | Goodbye | `GetIsAliasRef alias #1` | Prompt: [`"unnecessary"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:38) Response (Neutral): [`"That's too bad...Don't you want to see my face in Hood?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:39) |

Translation notes:
- `in Hood` is likely a mistranslation or flavor text for the suspect's disguise (hood/cloak).

### Branch 9: Mission Complete — "Defeated vampire, she disguised as a prostitute"

TOPIC `0x00626F zzAoMMq01B7MissionComplete`

Condition pattern:
- `GetStage == 40`: fires after defeating the vampire.
- `GetIsAliasRef alias #0` (Altano).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x00626F zzAoMMq01B7MissionComplete` | `0x006270` | none | `GetStage == 40`; `GetIsAliasRef alias #0` | Prompt: [`"Defeated vampire, she disguised as a prostitute."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:41) Response (Happy): [`"Well...that's why you are refreshed. You will make a name for yourself in future."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:42) Response (Happy): [`"Anyway...prostitute...Our work is done. I will also play in the night."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:43) |

VMAD Fragment:
- `AoM01_TIF__01006270` (triggers `OnEnd` fragment)
- (inference: fragment advances stage to 50, completing quest via `CompleteQuest` flag)

## Related Records

These are not all part of quest `005CE3` according to `infodiag`, but they are essential context:

NPCs:
- [`000D62 zzzAoMVigilantTraitor` - Altano](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:835) (quest-giver)
- Vampire prostitute: (alias #1; NPC identity TBD via deeper QUST alias dump)

Globals:
- `000038` (Skyrim.esm): Presumed to be `GameHour` or similar time-of-day global (sweetroll availability gated at values 6–21).

## Reconstruction Notes

Source-grounded:
- This quest is a straightforward vampire hunt triggered by an Arkay priest's request in Whiterun.
- The stage progression is linear: stage 0–10 (contact Altano) → stage 15 (investigate crime scene) → stage 20 (analyze victims, identify suspect as "novice vampire") → stage 30 (defeat vampire disguised as prostitute) → stage 40 (report to Altano) → stage 50 (quest complete).
- The "sweetroll" interaction is a flavor moment where the vampire suspect offers the player a treat, presumably leading into combat once the Goodbye dialogue closes.
- All dialogue is conditioned on alias refs, suggesting Altano and the prostitute/vampire are quest-tied NPCs.
- No custom scene records; all staging is via dialogue conditions and VMAD fragments.

Stage 11 and stage 9999 exist in the quest but are not referenced by dialogue; their purpose requires deeper Papyrus fragment inspection.

Open verification:
- inspect scripts `AoM01_TIF__01006259`, `AoM01_TIF__0100625C`, `AoM01_TIF__0100626B`, `AoM01_TIF__01006270` for exact stage advancement and aliases;
- inspect QUST aliases directly (via a richer alias dump) to confirm alias #0 = Altano, alias #1 = prostitute/vampire;
- inspect global `000038` (Skyrim.esm) to confirm time-of-day gating;
- inspect cell `0x0165AA` (Skyrim.esm) if crime scene location targeting matters;
- inspect NPC form data for the prostitute alias (#1) to confirm vampire disguise mechanics (e.g., leveled actor, disguise kit, behavior flags).
