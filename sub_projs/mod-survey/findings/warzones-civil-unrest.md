# WARZONES - Civil Unrest — marker/activator spawn warzones

## Scope / sources

- Local archives:
  - `~/skyrim_mods/hdd/WARZONES - Civil Unrest for SSE 0.95-2360-0-95-1577156818.7z`
  - `~/skyrim_mods/hdd/WARZONES - Civil Unrest for SSE - MCM 0.94-2360-0-94-1576482422.7z`
- Plugin inspected:
  - `WARZONES - SSE - Civil Unrest.esp`
  - `WARZONES - SSE - Civil Unrest MCM.esp`
  - source script `Source/Scripts/WZones_MCM.psc`
- Local `gamedata`: 1 quest / 160 NPCs / 631 locations / 1 item / 0 dialogue / 0 books.

## Classification

- Type: large exterior encounter / battlefield population system.
- Narrative value: low.
- Systems value: high for M&B-style warzone density, especially as a contrast to Immersive Patrols and CWO.

## Record shape

Core plugin has one visible quest:

- `WZones_MenuQuest`
- one forced player alias.
- no stages, objectives, or dialogue.

The actual system lives in placed activators, global variables, leveled NPC lists, and edited exterior cells:

- activator families:
  - `WZONESxMarkerActivatorRandomEncounterParent`
  - `WZONESxMarkerActivatorNoCWRandomEncounterParent`
  - `WZONESxMarkerActivator_rAmbush_*`
  - `WZONESxMarkerActivatorRandomEnabler`
  - `WZONESxMarker_Activator_PerformanceMode`
  - `WZONESxMarker_Activator_Cooldown`
  - `WZONESspawnometer_*`
  - `WZONESparent_spawnometer_*`
- leveled NPC pools:
  - `WZONESallSONSofSKYRIM`
  - `WZONESallIMPERIALrespawn`
  - `WZONESallSONSofSKYRIMrespawn`
  - `WZONESallFORESWORN`
  - `WZONESallBANDITS`
  - `WZONESallDRAUGHER`
  - `WZONESallSKELETONS`
  - `WZONESallTHALMOR`
  - `WZONESallMAGES`
  - `WZONESallDWEMERARMY`
  - `WZONESallMONSTERS`
  - faction commander pools.
- edited exterior placement names include fixed battle/selectors such as `WZonesSelectBattleSkyTemple`, `WZonesParentImperialPaleCamp*`, `WZonesParentBanditsDunstadFellhammer*`, `WZxRandomEnc_WRGuard_v_Bandit*`.

## Global / MCM control plane

The MCM script is mostly a thin editor over plugin globals:

- spawn density:
  - `WZonesSpawnCount`
  - `WZonesMonsters`
  - `WZonesBodyCount`
  - `WZonesCleanupTime`
- random systems:
  - `WZonesRandomEncounterOdds`
  - `WZonesRandomEncounters`
  - `WZonesRandomWZs`
  - `WZonesRandomAmbush`
- throttling:
  - `WZonesCooldown`
  - `WZonesPerformanceMode`
- per-site enable toggles:
  - dozens of `WZonesSelect*` globals grouped by hold: Eastmarch, Falkreath, Haafingar, Hjaalmarch, Pale, Reach, Rift, Whiterun, Winterhold, plus Sovngarde.

Important detail: the menu does not own the battle logic. It sets globals; placed activator scripts and enable parents interpret them in world.

## Mechanism pattern

WARZONES is not a CWO-style campaign controller:

- no fixed attacker/defender alias bank;
- no ticket-pool reinforcement quest;
- no war objective stages;
- no dialogue layer.

It is closer to:

1. place many world encounter sites;
2. attach activator/spawnometer bases to those sites;
3. let globals decide whether the site can fire;
4. spawn actors from leveled NPC pools;
5. throttle by cooldown/performance globals;
6. expose the knobs through MCM.

This is a “distributed encounter-site factory” rather than a central campaign.

## ModForge relevance

Useful abstraction:

- `encounterSite[]`
  - site id;
  - enable/global gate;
  - random chance;
  - cooldown;
  - spawn pools per faction/creature type;
  - max live actors;
  - optional performance-mode disable.
- `warzoneSettings`
  - global density;
  - body cleanup;
  - random encounter family toggles;
  - per-site toggles.

Good M&B lesson:

- If the goal is “world feels at war,” WARZONES pattern scales better than CWO because it does not need a giant quest graph.
- If the goal is “capture fort / win siege / change ownership,” WARZONES is insufficient by itself; it needs a campaign layer above it.

Recommendation:

- Use WARZONES as reference for ambient war density and random ambushes.
- Use CWO only for bounded decisive battles.
- Do not mix both at first vertical slice; start with one site factory and hard caps.
