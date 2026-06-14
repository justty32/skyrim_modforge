# Act 1 Side Quest Sub 01 - Witch of Ivarstead

Status: first redo slice. Source-grounded, link-first, no Gemini.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain context or conditions.
- Four scene topic records preserved as dialogue branches (stage-gated monologues).

## Quest Record

[`17576E zzzAoMSubQ01 "Witch of Ivarstead"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:169)

CLI:
- `questdiag Vigilant.esm 0x17576E`
- `infodiag Vigilant.esm 0x17576E`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x17576E`
- EditorID: `zzzAoMSubQ01`
- Name: `Witch of Ivarstead`
- Flags: `RunOnce`
- Priority: `60`
- Type: `Misc`
- Filter: `AoM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 5 | none | empty |
| 10 | none | empty |
| 20 | none | empty |
| 22 | none | empty |
| 24 | none | empty |
| 26 | none | empty |
| 28 | none | empty |
| 30 | none | empty |
| 40 | none | empty |
| 50 | CompleteQuest | empty (×2) |
| 200 | none | empty |
| 210 | none | empty |
| 220 | none | empty |
| 230 | CompleteQuest | empty (×2) |
| 300 | CompleteQuest | empty |

Objectives:
- None recorded in quest log via `questdiag`.

Inference:
- Stages 5, 20, 22–28 appear to be fine-grained progression states with no quest objective logging (likely internal to dialogue/script logic).
- Multiple `CompleteQuest` flags at stages 50, 230, 300 suggest branching outcomes: standard victory at stage 50, possible mercy endings at stage 230, alternate resolution at stage 300.

## Alias / Staging Backbone

No custom `SCEN` records detected as discrete records by `infodiag`. Dialogue is quest-direct via alias reference `#3` (Reyda).

Host quest:
- `17576E zzzAoMSubQ01` "Witch of Ivarstead"

Dialogue alias from `infodiag` conditions:
- Alias `#3`: expected to be [`16685A zzzAoMBossReyda "Reyda"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1164).

(inference: alias roles inferred from dialogue conditions `GetIsAliasRef` index 3; no explicit alias dump available from CLI)

## Custom Dialogue Branches

### Branch 1: Opening Diagnostic — Stendarr's Curse

TOPIC `0x177DED zzzAoMsq01WitchB01T01`

Condition pattern:
- `GetStage == 10`: fires when player first encounters Reyda.
- `GetIsAliasRef alias #3` (Reyda).
- Splits on `GetQuestCompleted` for a related quest (0x011B75, presumed to be another quest in the Act 1 chain).

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177DED` | `0x177DEE` | `Goodbye`, `SayOnce` | `GetStage == 10`; `GetQuestCompleted == 0` (quest `011B75`); `GetIsAliasRef alias #3` | [`"Stendarr become old ......His eyes is weaked, his mental is in insane now. That's because you are cursed...."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2037) |
| `0x177DED` | `0x177DEF` | `Goodbye`, `SayOnce` | `GetStage == 10`; `GetQuestCompleted == 1` (quest `011B75`); `GetIsAliasRef alias #3` | [`"Well well well, you have solved the curse? Old Fool become quite kind as he once was"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2038) |

Inference:
- The witch greets the player with diagnosis based on a related quest state. If player has completed quest 0x011B75 (not yet identified), the witch acknowledges the resolution and calls Stendarr "Old Fool".
- The greeting suggests Reyda can see the player is cursed and ties it to Stendarr's affliction.

### Branch 2: Witch Introduction — "Who are you?"

TOPIC `0x177DF1 zzzAoMsq01WitchB02T01` prompt="Who are you?"

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177DF1` | `0x177DF2` | none | `GetStage == 10`; `GetIsAliasRef alias #3` | Responses: [`"Me? I am  Reyda. Witch of Glenmoril"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2041) / [`"Ivalstead is my territory. All of people and beasts around here is mine"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2042) |

### Branch 3: Curse Analysis — "Why is my body so heavy?"

TOPIC `0x177DF4 zzzAoMsq01WitchB03T01` prompt="Why is my body so heavy ...... you did something?"

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177DF4` | `0x177DF5` | none | `GetStage == 10`; `GetQuestCompleted == 0` (quest `011B75`); `GetIsAliasRef alias #3` | Responses: [`"I do not anything. I just look, just lookin from the beginning"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2045) / [`"You was a really terrible. You killed child's life not only innocent person."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2046) / [`"needless to say,you are cursed. so much worse If you serve the God of Justice. You are alredy over"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2047) |

Translation notes:
- Source phrase "You was a really terrible" is agrammatical; implies the player's past actions (killing innocents/children) are the source of the curse.

### Branch 4: Curse Solution — "How can I solve this curse?"

TOPIC `0x177DF6 zzzAoMSqQ01WitchB03T02` prompt="How can I solve this curse?"

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177DF6` | `0x177DF7` | none | `GetStage == 10`; `GetQuestCompleted == 0` (quest `011B75`); `GetIsAliasRef alias #3` | [`"You listen to me? you are useless. I said you are over, You are over."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2050) |

### Branch 5: Family Backstory — "Why was family staying here?"

TOPIC `0x177DF9 zzzAoMSQ01WitchB04T01` prompt="Why was family staying here?"

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177DF9` | `0x177DFA` | none | `GetStage == 10`; `GetQuestCompleted == 0` (quest `011B75`); `GetIsAliasRef alias #3` | Responses: [`"They are in troubled by cursed sword. How poor thing? So, Kind Witch decided to help them they solve the curse"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2053) / [`"Now The curse is gone, So they heve to went out here. But You did clean up here luckily. It was save time thanks to you."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2054) |

Inference:
- Reyda indicates a family was cursed by a sword and she helped them. This establishes Reyda as a complex figure: outwardly helpful but morally ambiguous.

### Branch 6: Master Question — "Who is your master?"

TOPIC `0x177DFC zzzAoMSQ01WitchB05T01` prompt="Who is your master?"

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177DFC` | `0x177DFD` | none | `GetStage == 10`; `GetQuestCompleted == 0` (quest `011B75`); `GetIsAliasRef alias #3` | [`"Come on, somebody? Witch open the crotch anyone if they have power. Nfufufu"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2057) |

Translation notes:
- Phrase is crude and agrammatical; likely implies Reyda is amoral and will serve any master with power.

### Branch 7: Moral Accusation — "You know everything. About me, About the family..."

TOPIC `0x177DFF zzzAoMSQ01WitchB06T01` prompt="You know everthing. About me, About the family..."

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177DFF` | `0x177E00` | none | `GetStage == 10`; `GetQuestCompleted == 1` (quest `011B75`); `GetIsAliasRef alias #3` | Responses: [`"Oh, yes. So shat? So you say I am evil? Murderer is you. Not me, You are Murderer"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2060) / [`"I am just looking you as promised, and make fog thicken. Well, but it looks like there was no need for Old Fool"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2061) |

Translation notes:
- "So shat" is a typo; intended "So what".
- "looking you as promised" suggests a prior agreement or pact.

### Branch 8: Daedric Affiliation — "Your master is Molag Bal?"

TOPIC `0x177E02 zzzAoMSQ01WitchB07T01` prompt="Your master is Molag Bal?"

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177E02` | `0x177E03` | none | `GetStage == 10`; `GetQuestCompleted == 1` (quest `011B75`); `GetIsAliasRef alias #3` | [`"Now, what was that? I do dance with anybody. Sexy woman like me is so hard, Nfufufufu"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2064) |

### Branch 9: Combat Confrontation — "Witch must die"

TOPIC `0x177E05 zzzAoMSQ01WitchB08T01` prompt="Witch must die"

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177E05` | `0x177E06` | none | `GetStage == 10`; `GetQuestCompleted == 1` (quest `011B75`); `GetIsAliasRef alias #3` | Responses: [`"You want to kill more? After Killing women and child, your fellows. You want to kill to shabby old woman the next?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2067) / [`"Well, good. Try baby. You will be die while lamented your own powerlessness"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2068) |

VMAD Fragment:
- `AoMSq01_TIF__02177E06` (triggers `OnEnd` fragment)
- (inference: fragment likely triggers combat or advances quest stage on hostility)

### Branch 10: Surrender / Despair Path — "Oh, My God. Come on."

TOPIC `0x177E09 zzzAoMSQ01Witch2B01T01`

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177E09` | `0x177E0A` | none | `GetStage == 30`; `GetIsAliasRef alias #3` | Responses: [`"Oh, My God. Come on. Please, help me, I will do anything"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2071) / [`"I was deceived in Molag Bal. I did not think to become a thing. So,please"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2072) |

Inference:
- Reyda's tone shifts to desperation at stage 30, suggesting either: (a) combat damage/loss, or (b) a scripted state change mid-encounter.
- Reyda explicitly mentions being deceived by Molag Bal, implicating daemonic coercion.

### Branch 11: Corrupted Soul Question — "What is Corrupted Soul?"

TOPIC `0x177E0C zzzAoMSQ01Witch2B02T01` prompt="What is Corrupted Soul?"

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177E0C` | `0x177E0D` | `SayOnce` | `GetStage == 30`; `GetIsAliasRef alias #3` | Responses: [`"Black soul found the gates of Oblivion. Gates will swallow you from the inner sooner or later"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2075) / [`"You are aleady trapped in Oblivion. No one can not get away, You are over"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2076) |

### Branch 12: The Stone Lore — "What is the Stone?"

TOPIC `0x177E0F zzzAoMSQ01Witch2B03T01` prompt="What is the Stone?"

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177E0F` | `0x177E10` | `SayOnce` | `GetStage == 30`; `GetIsAliasRef alias #3` | Responses: [`"Your fellow teach you nothing. How poor you are, I can not stop laughing you"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2079) / [`"Molag Bal. Don't you know the demon committed the bitch of Nede? To beast from people, the oldest of the stragglers"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2080) |

Translation notes:
- "committed the bitch of Nede" is unclear; likely refers to a historical atrocity involving Molag Bal and Alessia (Nede ancestry). May be a mistranslation of a proper noun reference.

### Branch 13: Mercy Rejection — "No, Witch must die"

TOPIC `0x177E12 zzzAoMSQ01Witch2B04T01` prompt="No,Witch must die"

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177E12` | `0x177E13` | `Goodbye`, `SayOnce` | `GetStage == 30`; `GetIsAliasRef alias #3` | [`"Don't you have any mercy? You fucking bastard!! I wrench your head."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2083) |

VMAD Fragment:
- `AoMSq01_TIF__02177E13` (triggers `OnEnd` fragment)
- (inference: fragment triggers death or final combat state)

### Branch 14: Mercy Acceptance — "Get lost. never come back here"

TOPIC `0x177E15 zzzAoMSQ01Witch2B05T01` prompt="Get lost. never come back here"

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177E15` | `0x177E16` | `Goodbye` | `GetStage == 30`; `GetIsAliasRef alias #3` | [`"Oh, thank you. you are so friendly. I promise to live humbly in deep forest"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2086) |

VMAD Fragment:
- `AoMSq01_TIF__02177E16` (triggers `OnEnd` fragment)
- (inference: fragment marks quest completion via mercy path)

### Branch 15: Death Dialogue — Reyda's Final Words

TOPIC `0x177E17 zzzAoMSQ01WitchDeath` [Combat/Death]

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x177E17` | `0x177E18` | none | `GetIsID == 1` (NPC `16685A:Vigilant.esm` = Reyda) | [`"You are monster...Laza will eat you..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2089) |

(speaker: 16685A Reyda)

Inference:
- "Laza" (possibly "Laza" or a variant) is unknown; may refer to a creature, curse, or daemonic entity associated with Molag Bal's revenge.

## Scene Monologues (Late-Stage / Contemplation)

Four scene-topic records attached to this quest provide monologue-like responses. These appear to fire during stage 30 (desperation phase) via scene dialogue conditions.

### Scene 1: Cold Eyes

TOPIC `0x179185` [Scene/Scene]

| FormID | INFO | Conditions | Responses |
|---|---|---|---|
| `0x179185` | `0x179186` | (none) | [`"Your Eyes are so Cold, But hatred is burning under the thick ice"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2092) / [`"It is the same with the Old Fool. Oh, it let me hot. I want to put your eyes to decorate the shelves."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2093) |

### Scene 2: Counterfactual Curse Lifting

TOPIC `0x179188` [Scene/Scene]

| FormID | INFO | Conditions | Responses |
|---|---|---|---|
| `0x179188` | `0x179189` | (none) | [`"If you did not come here, that family never die. Their curse will be solved. They have been living happily in his hometown of High Rock ......"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2096) / [`"So Poor, because of all you. If you did nothing, nothing happens."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2097) |

Inference:
- Reyda blames the player's arrival for the family's doom, implying her role was initially benign or neutral.

### Scene 3: Justice Skepticism

TOPIC `0x17918B` [Scene/Scene]

| FormID | INFO | Conditions | Responses |
|---|---|---|---|
| `0x17918B` | `0x17918C` | (none) | [`"You believe the old fool yet? Although There have not exit true justice in  in this world?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2100) / [`"If it existed. Why is innocent people suffered, sinful people batten?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2101) |

Inference:
- Reyda philosophically challenges Stendarr's notion of justice, suggesting moral relativism or nihilism.

### Scene 4: Touched by Stone

TOPIC `0x17918E` [Scene/Scene]

| FormID | INFO | Conditions | Responses |
|---|---|---|---|
| `0x17918E` | `0x17918F` | (none) | [`"The identity of the flame burning in your eyes. You've touched the stone. That's why your are stubborn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2104) / [`"Smell of Corrupted Soul...... You are not already human, You are monster"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2105) |

Inference:
- This explicitly references the "stone" (daemonic artifact from Marukh path) and suggests the player has been contaminated by it.

## Alternate Ending Phase (Stage 210+)

After the initial confrontation, if the player survives or returns, stage 210 dialogue unlocks:

### Branch 16: Renewed Malice — "You are so stupid"

TOPIC `0x17B7F6 zzzAoMSQ01Witch3B01T01`

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x17B7F6` | `0x17B7F7` | `WalkAway` | `GetStage == 210`; `GetIsAliasRef alias #3` | Responses: [`"You are so stupid. You are like Old fool. It's just like you to that decrepit until the tail club"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2108) / [`"You're not a record of death way. Looks fell to die dripping field in the wilderness, Ahahahahaha"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2109) |

Translation notes:
- "decrepit until the tail club" is unclear; possibly a mangled reference to decay or degradation.

### Branch 17: Final Threat — "What are you trying to do?"

TOPIC `0x17B7F8 zzzAoMSQ01Witch3B01T02` prompt="What are you trying to do?"

| FormID | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| `0x17B7F8` | `0x17B7F9` | `Goodbye`, `SayOnce` | `GetStage == 210`; `GetIsAliasRef alias #3` | Responses: [`"I can not kill you. So I wreak my anger by killin ivasterd's people"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2112) / [`"his is Humble life of the witch. It is to get all I see into honey bucket "`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2113) |

VMAD Fragment:
- `AoMSq01_TIF__0217B7F9` (triggers `OnEnd` fragment)
- (inference: fragment likely marks a revenge cycle or final completion state)

Translation notes:
- "killin ivasterd's people" = "killing Ivarstead's people"
- "honey bucket" likely a crude metaphor for degradation or subservience.

## Related Records

NPCs:
- [`16685A zzzAoMBossReyda "Reyda"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1164) (witch; quest-tied via alias #3)
- [`0DC68D zzzCHEnchanter "Hilda the witch"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:829) (related NPC; dialogues in Act 4 memory chain reference her knowledge of Reyda)

Related Quests:
- `011B75:Vigilant.esm` (referenced quest; completion state gates several Reyda opening lines; identity TBD)
- Likely connected to Act 1 early quest chain via parent Altano/Stendarr storyline.

## Reconstruction Notes

Source-grounded:
- This quest is a stage-branching encounter with Reyda, a Glenmoril witch entangled with Molag Bal and cursed/cursing NPCs in Ivarstead.
- The quest has three outcome paths:
  1. **Combat death** (stage 10→50 if player fights): Reyda is slain; stage 50 `CompleteQuest`.
  2. **Mercy / Exile** (stage 10→30, then accept mercy): Reyda is spared; stage 230 `CompleteQuest` (inference from stage flags).
  3. **Revenge cycle** (stage 10→30, then reject mercy): Reyda retaliates via Ivarstead; stage 210+300 `CompleteQuest` (final vendetta).
- The quest involves a family cursed by a daemonic sword, Reyda's ambiguous rescue attempt, and metaphysical exposition about the "stone" (daemonic artifact), "corrupted soul", and Molag Bal's role.
- Scene monologues at stage 30 provide philosophical commentary and confirm the player's contamination by the daemonic stone.

Stage transitions:
- Stage 0: initial
- Stage 5–28: unclear (no dialogue gating; internal script progression likely)
- Stage 10: player talks to Reyda (opening line variant on quest 011B75 completion)
- Stage 20–28: presumed investigation or preparation
- Stage 30: desperation/mercy point (branch on player choice)
- Stage 50: quest complete if player chooses combat
- Stage 200–230: alternate outcome phases
- Stage 300: final completion state

Daedric / Lore Links:
- Reyda is coerced by Molag Bal; this is consistent with Act 4 Memory quests and the broader Vigilant narrative of daemonic entrapment.
- References to "the Stone" and "Corrupted Soul" tie to the Marukh-adjacent motive (Eye of Marukh, contamination theology).
- The family curse ties to a broader Ivarstead subplot involving the Glenmoril coven.

Open verification:
- inspect scripts `AoMSq01_TIF__02177E06`, `AoMSq01_TIF__02177E13`, `AoMSq01_TIF__02177E16`, `AoMSq01_TIF__0217B7F9` for exact stage advancement, outcome gating, and mercy path logic;
- inspect QUST aliases directly (via a richer alias dump) to confirm alias #3 = Reyda;
- inspect quest `011B75` to confirm its identity and role in Reyda's greeting logic;
- inspect NPC Reyda (`16685A`) for combat flags, behavior packages, and daemonic corruption markers;
- inspect stage 5–28 progression in quest script/stage log entries if available, as they are currently opaque;
- cross-reference Hilda the witch (`0DC68D`) dialogue to confirm backstory (she appears in Act 4 memory and may provide lore context);
- verify "Laza" reference in death dialogue if a known entity.
