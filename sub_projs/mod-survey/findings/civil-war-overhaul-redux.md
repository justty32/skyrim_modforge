# Civil War Overhaul Redux — campaign state machine + reinforcement battles

## Scope / sources

- Local archive: `~/skyrim_mods/hdd/Civil War Overhaul-37906-0-4-7-1613327163.rar`
- Plugin: `Civil War Overhaul.esp`
- Extracted script sources for targeted inspection:
  - `source/scripts/cwscript.psc`
  - `source/scripts/CWCampaignScript.psc`
  - `source/scripts/CWReinforcementControllerScript.psc`
  - `source/scripts/cwfortsiegescript.psc`
  - `source/scripts/CWOMCMScript.psc`
- Local `gamedata`: 754 records, 46 quests, 135 dialogue lines, 164 NPCs, 47 magic records, 29 items, 4 books.
- Public Nexus description says Redux rebuilds the original Civil War Overhaul with support/stability enhancements while preserving the original vision: <https://www.nexusmods.com/skyrimspecialedition/mods/37906>.

## Classification

- Type: Civil War questline/system overhaul.
- Plugin: yes, ESP plus many scripts/assets.
- Narrative value: medium.
- Systems value: very high for M&B / Three-Kingdoms-style campaign design, but too invasive to copy directly.

## High-level architecture

This is not a patrol mod. It is a full campaign state machine that reuses and overrides vanilla Civil War infrastructure:

- Overrides / extends vanilla `CW`, `CWObj`, `CWCampaign`, `CWSiege`, `CWFortSiege*`, `CWAttackCity`, `MQ302`, etc.
- Adds new CWO quest controllers:
  - `CWOQuestMonitor`
  - `CWOMCMQuest`
  - `CWOBAController` / `CWOBAQuest`
  - `CWOSendForPlayer`
  - `CWMission05`
  - `CWOPatrollerDerby`
  - reinforcement increment quests.
- Adds many global variables for war/campaign state.
- Adds many soldier/monster/specialist NPC bases used as pools.
- Adds scripted magic/items mostly as special rewards / powers, not as the campaign core.

## Campaign state globals

Important globals observed:

- Current campaign position:
  - `CWOCurrentHold`
  - `XXX_CWOPreviousHold`
  - `CWOWarBegun`
  - `CWOCapitalQuestRunning`
- Reinforcement / troop pool:
  - `CWOSiegeReinforcements`
  - `CWOCapitalReinforcements`
  - `CWOFortReinforcements`
  - `CWOImperialReinforcements`
  - `CWOSonsReinforcements`
  - `CWOAttackerReinforcements`
  - `CWODefenderReinforcements`
  - vanilla-linked `CWPercentPoolRemainingAttacker` / `CWPercentPoolRemainingDefender`
- Behavior/config:
  - `CWODefendingActive`
  - `CWODontRunQuests`
  - `CWOFirstMissionGlobal`
  - `CWODisguiseGlobal`
  - `CWOCourierSentGlobal`
  - `CWOTroopPoolGameType`
  - `CWODisguiseGameType`
  - `CWOBAChance`, `CWOBATime*`, `CWOSIChance`

The model is mostly `GLOB + quest stage + script state`, not external JSON.

## Battle / siege alias pattern

`CWSiege`:

- 18 stages.
- 353 aliases.
- repeated attacker/defender aliases with package override stacks.
- city/location aliases drive marker lookup.
- many aliases are `LocationAliasReference` under the city alias with specific `RefType`.
- Finds:
  - gates;
  - barricades;
  - attack/defend triggers;
  - catapults;
  - garrison enable markers;
  - objective markers;
  - battle center markers.

`CWFortSiegeCapital`:

- 219 aliases.
- Fort location alias.
- 10 attacker + 10 defender visible combat aliases per side/faction.
- Objective 100 / 200 each target 10 enemy aliases.
- Stage flow includes battle start, phase gates, win/fail, shutdown.

This is the concrete record-side pattern for “20v20 wave battle”:

- fixed aliases;
- location/ref-type marker resolution;
- alias package override;
- objective count display;
- script controls enable / disable / stage progression.

## Reinforcement controller

`CWReinforcementControllerScript` is the key script:

- Quest script with ticket pools:
  - `PoolAttacker`, `PoolDefender`
  - `StartingPoolAttacker`, `StartingPoolDefender`
  - `InfiniteRespawnAttacker`, `InfiniteRespawnDefender`
- Wait-based wave interval:
  - `ReinforcementInterval = 5`
- Threshold objective updates:
  - threshold counters and stage-to-set properties for 25% remaining / wiped out.
- Spawn points:
  - 4 attacker spawn refs + failsafe;
  - 4 defender spawn refs + failsafe.
- Alias arrays are hard-coded as `A1..A20` and `D1..D20`.
- On actor death, `registerDeath()` waits, then iterates aliases to respawn where tickets remain.

Design implication:

- The system caps live combatants with fixed aliases and refills them from reinforcement pools.
- This directly matches the M&B note that large battles must be wave-based.
- The hard-coded alias array is ugly but engine-native and reliable.

## Fort siege script pattern

`cwfortsiegescript.psc` shows:

- phase trigger refs;
- barricade aliases;
- interior spawner aliases;
- `EnableBarricades()` / `DisableBarricades()`;
- `CheckBarricadesDestroyedThenSetStage()`;
- `MoveToPackageLocation()` for actor aliases;
- `tryToCreateInteriorDefender()`:
  - gets an interior spawner ref;
  - chooses defender base by faction;
  - `PlaceActorAtMe()`;
  - force-ref result into defender alias.

This is a hybrid of:

- pre-authored markers;
- runtime spawning at marker aliases;
- aliases used as stable handles for later cleanup / enable / disable.

## ModForge relevance

Already supported / recently landed:

- location alias + ref-type lookup is the same family as `findInLocationAlias`.
- package target/location alias indirection exists.
- `UpdateCurrentInstanceGlobal` exists for objective text like `<Global=CWPercentPoolRemainingAttacker>`.
- dynamic spawn template exists but is currently simpler than CWO's per-alias wave/ticket system.

Not a good direct target yet:

- Full CWO-style campaign rewrite depends on large custom Papyrus controllers, heavy vanilla overrides, and many hard-coded aliases.
- This is not a “single spec feature”; it is a project-scale system.

Useful abstractions to steal:

1. `battleScenario` high-level spec:
   - attacker/defender actor pools;
   - fixed live alias count per side;
   - reinforcement pool tickets;
   - spawn marker aliases;
   - win/fail stage thresholds;
   - objective global display.
2. `phaseTrigger` / barricade workflow:
   - trigger ref type → stage;
   - destructible refs → stage when all reach destruction stage.
3. `holdCampaignState`:
   - `currentHold`;
   - owner faction;
   - attacker/defender pools;
   - next mission choice.

Recommendation for M&B prototype:

- Do **not** start with CWO scale.
- First slice should be:
  - one cell / arena;
  - 10 attacker aliases + 10 defender aliases;
  - two spawn markers per side;
  - ticket pool globals;
  - objective globals;
  - one reusable `MFBattleReinforcementController.psc`.
- Only after that works should it grow into hold ownership / campaign AI.

Open follow-up:

- Compare Open Civil War separately after obtaining the plugin. Public descriptions say it exposes cut city battles through a war-map strategy layer (<https://www.nexusmods.com/skyrim/mods/82128>), which may be closer to the desired strategic UI than CWO, but this local pass did not have the plugin, so no record-level claims are made here.
