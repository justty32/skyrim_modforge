# Civil War Lines Expansion — event dialogue bark layer

## Scope / sources

- Local archive: `~/skyrim_mods/hdd/Civil War Lines Expansion-77566-1-06-1676572611.zip`
- Plugin inspected: `Civil War Lines Expansion.esp`
- Local `gamedata`: 1 quest / 415 dialogue lines / 0 NPCs / 0 locations / 0 items / 0 magic.
- Archive ships:
  - `Seq/Civil War Lines Expansion.seq`
  - many voiced `.wav`, `.lip`, and `.fuz` files under `Sound/Voice/Civil War Lines Expansion.esp/...`.

## Classification

- Type: dialogue content expansion.
- Narrative value: medium.
- Systems value: medium for combat/event bark generation and voice pipeline proof.

## Record shape

Single quest:

- `CivilWarLinesExpansion_DialogueQuest`

Dialogue topic categories observed:

- `Misc/Hello`
- `Misc/Idle`
- `Misc/Goodbye`
- `Combat/Attack`
- `Combat/Hit`
- `Combat/Block`
- `Combat/AllyKilled`
- `Combat/Death`
- `Combat/Yield`
- `Combat/Taunt`
- `Combat/Trespass`
- `Combat/Bleedout`
- `Detection/NormalToCombat`
- `Detection/CombatToNormal`
- `Detection/NormalToAlert`
- `Detection/CombatToLost`
- healing spell reaction topic.

There are no new actors or quests beyond the dialogue quest. The mod is a response layer over vanilla civil-war actors, guards, voice types, factions, equipment, and locations.

## Condition pattern

Common INFO conditions:

- faction:
  - Imperial / Stormcloak / guard factions.
- voice type:
  - repeated `GetIsVoiceType` branches.
- target state:
  - combat target faction;
  - target armor/equipment;
  - combat target keyword.
- randomness:
  - `GetRandomPercent <= N`.
- location/world:
  - Skyrim worldspace;
  - exterior-only;
  - hold ownership via `GetKeywordDataForLocation`.
- player proximity/state:
  - `GetDetected player`;
  - player sprinting;
  - player equipped armor.
- actor state:
  - health/stamina percentage;
  - group member count;
  - sex/race.

The important design is not the line text; it is the condition matrix over existing engine dialogue events.

## Mechanism pattern

This mod proves a high-leverage content path:

1. one always-available dialogue quest;
2. many event topics;
3. many random INFOs per topic;
4. vanilla conditions narrow each bark to faction/voice/combat state;
5. voiced files and `.seq` shipped with plugin.

It is compatible with static patrols, WARZONES, OBIS routes, or CWO battles because it does not own the actors or battle lifecycle.

## ModForge relevance

Useful generator idea:

- `barkPack`
  - topic event category;
  - faction/voice/equipment/location filters;
  - random chance;
  - response text;
  - emotion;
  - optional voice asset mapping.

This is an ideal LLM-assisted content batch target:

- generate candidate barks by faction/voice/event;
- compile into INFO records;
- later attach generated voice assets.

Risks:

- Combat dialogue categories are crowded. Conditions must be narrow enough to avoid inappropriate lines.
- `SayOnce` and low random chances need deliberate use or the player will hear repetition.
- Voice asset naming/packaging and `.seq` generation must be treated as first-class output, not an afterthought.

M&B relevance:

- Use this after the battle mechanics exist.
- It makes patrols and sieges feel alive without adding more AI complexity.
