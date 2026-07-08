# Immersive Patrols SE/AE — patrols + fixed battle routes

## Scope / sources

- Local archives:
  - `~/skyrim_mods/hdd/Immersive Patrols (Lite)-718-3-0b-1710611136.zip`
  - `~/skyrim_mods/hdd/Immersive Patrols Heavy (16 vs 16 Battles)-718-3-0a-1652714196.7z`
- Plugin inspected:
  - Lite: `Immersive Patrols II.esp`, 114,735 bytes, `gamedata`: 37 NPC / 7 locations / 0 quest / 0 dialogue.
  - Heavy: `Immersive Patrols II.esp`, 156,095 bytes, `gamedata`: 57 NPC / 10 locations / 0 quest / 0 dialogue.
- Public page confirms the design goal: patrols for Stormcloak / Thalmor / Imperial / Dawnguard plus Solstheim roamers, with factions crossing paths and creating encounters. Nexus files distinguish Lite as patrols-only and Heavy as 16v16 battles: <https://www.nexusmods.com/skyrimspecialedition/mods/718>.

## Classification

- Type: world encounter / patrol population mod.
- Plugin: yes, single ESP.
- Narrative value: low.
- Systems value: high for `world-building/11-mount-and-blade.md` because it is the smallest concrete example of road patrols and warzone battles without a quest controller.

## Record shape

Heavy plugin:

- 468 records.
- Masters: `Skyrim.esm`, `Update.esm`, `Dawnguard.esm`, `Dragonborn.esm`.
- No QUST / INFO / SCEN pipeline. This is deliberate: encounters emerge from placed actors, packages, factions, and world layout.
- Custom aggro factions:
  - `ImmersiveImperialAggroFaction`
  - `ImmersiveSonsAggroFaction`
  - `ImmersiveThalmorAggroFaction`
  - `ImmersiveDawnguardSoldierFaction`
- Static marker bases use `MarkerX.nif`:
  - `ImmersiveCivilWarPatrolXMarker`
  - `ImmersiveDawnguardPatrolXMarker`
  - enable-marker variants for Civil War / Dawnguard / DLC2 routes.
- NPC bases are route-specific rather than generic:
  - leader + soldier pairs such as `ImmersiveHalfMoonImperialLeader` / `ImmersiveHalfMoonImperialSoldier`.
  - battle variants such as `ImmersiveBattlePinefrostImperialLeader`, `ImmersiveBattlePinefrostSonsLeader`.

## Mechanism pattern

Observed NPC pattern:

- Leader:
  - vanilla race/class/outfit/combat style;
  - vanilla faction membership for broad hostility / identity;
  - custom aggro faction for cross-mod patrol hostility;
  - patrol package.
- Soldiers:
  - same faction stack;
  - follow-leader package plus the same patrol package.
- Battle version:
  - more placed actors and battle-specific NPC bases.
  - public file metadata labels Heavy as 16v16 battles; local heavy plugin has 57 NPC bases vs 37 in Lite.

The important design choice is **static world placement**:

- There is no radiant quest, no SM trigger, no dynamic spawn script.
- Encounters happen because routes physically intersect and factions are mutually hostile.
- This is stable and cheap, but it burns placed refs and cannot adapt to campaign state unless enable parents / globals are added.

## ModForge relevance

Already supported:

- NPC bases with faction/outfit/combat style/packages.
- placed NPC refs in exterior cells.
- patrol / follow packages.
- custom factions and inter-faction hostility.

Useful generator idea:

- `patrolGroups[]` convenience layer:
  - group id;
  - faction stack;
  - leader NPC template;
  - follower NPC templates;
  - route marker chain;
  - optional enable parent / gate global;
  - optional hostileTo group ids.
- This would generate the Immersive Patrols pattern from compact data, without needing a quest.

Design lesson for M&B prototype:

- First vertical slice can be static:
  - 2 factions;
  - 2 patrol groups;
  - one route intersection;
  - no strategy layer.
- This proves travel/package/combat density before building the Civil War Overhaul-style campaign simulation.

Risks:

- Heavy battles are actor-density risk. The local Heavy file’s 16v16 design matches the existing M&B note that large live battles must be bounded.
- Static battles have poor strategic persistence unless controlled by enable parents or scripted ownership state.
