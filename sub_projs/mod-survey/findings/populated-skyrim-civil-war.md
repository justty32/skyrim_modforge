# Populated Skyrim Civil War — placed civil-war actor population

## Scope / sources

- Local archive: `~/skyrim_mods/hdd/Populated Skyrim Civil War Legendary Edition-5288-3-0.7z`
- Plugin inspected: `Data/Populated Skyrim Civil War.esp`
- Local `gamedata`: 430 NPCs / 5 locations / 0 quests / 0 dialogue / 0 items / 0 magic.
- Archive also ships many FaceGen meshes/textures.

## Classification

- Type: world population / civil-war patrol and battle placement mod.
- Narrative value: low.
- Systems value: medium. It is useful as the simplest “fill the world with war actors” pattern.

## Record shape

The plugin is almost entirely NPC bases plus placed references:

- custom factions:
  - `ssssPSCWImperialfaction`
  - `ssssPSCWStormcloakfaction`
- leveled patrol pools:
  - `rrrrLCharSonsPatrolCaptain`
  - `rrrrLCharImperialPatrolCaptain`
  - `rrrrLCharSonsPatrolSoldier01/02/03`
  - `rrrrLCharImperialPatrolSoldier01/02/03`
- many route/battle-specific NPC bases:
  - `ssssTheReachPatrol*`
  - `ssssHaafingarPatrol*`
  - `ssssFalkreath*`
  - `ssssHjaalmarch*`
  - `iiiiGB*` / `llllGB*` large battle groups.
- many placed NPCs and placed marker objects, including named battle markers such as `GREATBATTLE2`, `MzinchaleftGREATBATTLE`, `AlchemistShackBATTLE`.

No QUST / INFO layer was observed. This is not a controller-driven mod.

## Mechanism pattern

The pattern is direct and content-heavy:

1. define many custom soldier NPC bases;
2. group them by faction/hold/route/battle;
3. place them directly into exterior cells;
4. rely on faction hostility and package/default AI for emergent fights;
5. ship FaceGen because many new NPC records need matching generated faces.

Compared with Immersive Patrols:

- Populated Skyrim Civil War is larger and more brute-force.
- It creates many more NPC bases.
- It is less interesting as a systems design, but useful as a density baseline.

Compared with WARZONES:

- no random activator/spawnometer system observed;
- less configurable;
- more static placed population.

## ModForge relevance

Already supported in principle:

- NPC bases;
- faction assignment;
- placed refs;
- package assignment;
- leveled NPC lists.

Generator lesson:

- A compact `civilWarPopulation` convenience layer could emit:
  - faction templates;
  - route-specific soldier variants;
  - leveled patrol lists;
  - placed groups at named markers;
  - optional FaceGen reminder/checklist.

Risk:

- This pattern can inflate plugin size quickly and requires navmesh-safe placement discipline.
- It creates actor density without strategic state, so it should not be mistaken for a campaign system.

M&B relevance:

- Good for background life: camps, roads, bridges, border skirmish dressing.
- Not enough for conquest mechanics.
