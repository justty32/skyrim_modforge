# OBIS SE Patrols Addon — quest alias route factory

## Scope / sources

- Local archive: `~/skyrim_mods/hdd/OBIS SE Patrols Addon-4145-2-5-1546523796.7z`
- Plugin inspected: `OBIS SE Patrols Addon.esp`
- Local `gamedata`: 5 quests / 0 NPCs / 3 locations / 2 books / 0 dialogue / 0 items / 0 magic.
- Depends on OBIS main plugin for the actual bandit pools.

## Classification

- Type: patrol route / generated bandit group addon.
- Narrative value: low.
- Systems value: high for route-driven spawn groups.

## Record shape

Quests:

- `OBIS_Patrol_Quest`
- `OBIS_Patrol_StableRaids`
- `OBIS_Patrol_BookMenu`
- `OBIS_Patrol_MCMenu`
- vanilla `dunNilheimQST` touched/overridden.

Globals:

- `OBIS_Patrol_Enable`
- `OBIS_Patrol_Difficulty`
- `OBIS_Patrol_RespawnTime`
- `OBIS_Patrol_RespawnEnable`
- `OBIS_Patrol_BookOrMCM`
- `OBIS_Patrol_Potions`
- `OBIS_Patrol_PotionChance`
- `OBIS_Patrol_StableRaidsEnable`
- `OBIS_Patrol_StableRaidsTime`
- `OBIS_Patrol_StableCity`
- `OBIS_Patrol_NumFollowers`

Books:

- `OBIS_Patrol_Menu`
- `OBIS_Patrol_MenuToken`

Leveled NPC refs:

- `OBIS_Patrol_LL_Leader`
- `OBIS_Patrol_LL_Follower`
- `OBIS_Patrol_HorseList`

Placed markers:

- dozens of `OBIS_Patrol_Marker_<route>_<name/waypoint>` refs across Skyrim/Solstheim;
- stable raid start/destination markers.

## Core quest pattern

`OBIS_Patrol_Quest` has 100 aliases and no stages/objectives.

Repeated route shape:

- one forced start marker alias;
- one optional leader alias;
- three optional follower aliases;
- each actor alias uses `CreateReferenceToObject`;
- leader object is `OBIS_Patrol_LL_Leader`;
- follower object is `OBIS_Patrol_LL_Follower`;
- aliases are gated by globals:
  - main enable;
  - follower count (`OBIS_Patrol_NumFollowers >= N`);
  - race filters to avoid invalid generated actors.
- each actor alias has ALPS alias package override pointing to a route-specific package.

Example package `OBIS_Patrol_00_Package`:

- owner quest: `OBIS_Patrol_Quest`;
- package template: vanilla `0x06DE44`;
- preferred speed: jog;
- package data targets route aliases: leader + followers + start marker;
- target includes player as a package target data entry;
- package data is alias-index based, so the route package and quest alias layout are tightly coupled.

`OBIS_Patrol_StableRaids` is the same idea with 9 aliases:

- horse aliases;
- stables/start/destination forced marker aliases;
- leader + followers created from leveled lists;
- enable gated by stable raid globals;
- package `OBIS_Patrol_StableRaid_00_Package` runs instead of jogs and uses alias locations for stable/destination.

## Mechanism pattern

This is the cleanest of this batch for ModForge:

1. author route markers;
2. define a route quest with repeated alias slots;
3. create actors from leveled lists into aliases;
4. attach route-specific packages through ALPS alias override;
5. expose global settings through book/MCM.

It avoids creating new NPC records in the addon. Actor variety comes from OBIS leveled lists.

## ModForge relevance

Useful abstraction:

- `routeSpawnQuest`
  - global enable;
  - route id;
  - start marker alias;
  - optional destination marker alias;
  - leader leveled list;
  - follower leveled list;
  - follower count global;
  - package template and alias target mapping.

This is probably a better first implementation target than WARZONES:

- bounded alias count;
- inspectable quest handles;
- no central campaign complexity;
- route behavior is explicit and generated.

Gap / risk:

- ModForge must preserve ALPS package override and package target=alias mapping correctly.
- The alias index layout becomes API. A generator should emit package and alias definitions together, not let users hand-maintain alias numbers.
