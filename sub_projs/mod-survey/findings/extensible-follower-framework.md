# Extensible Follower Framework — 100-slot follower controller

## Scope / sources

- Local archive: `~/skyrim_mods/hdd/Extensible Follower Framework v4-0-5-7003-4-0-5.7z`
- Files:
  - `EFFCore.esm`
  - `EFFDialogue.esp`
  - `EFFCore.bsa`
- Local `gamedata` on `EFFCore.esm`: 755 records, 17 quests, 192 dialogue lines, 16 magic records, 11 items, 1 interior cell.
- Public page describes EFF as extending the follower system with flexible follower features and plugin extensibility: <https://www.nexusmods.com/skyrim/mods/12933>.

## Classification

- Type: follower framework.
- Plugin: yes, ESM + ESP + BSA.
- Narrative value: low.
- Systems value: high for multi-follower architecture, command UI, per-follower storage, package override, and plugin feature modularity.

## Core record shape

Key records:

- Main quest: `FollowerExtension` (`0x000EFF`), priority 60, type Misc.
- Slot scale:
  - 100 visible objectives: `<Alias=Follower000> is waiting for you.` through `<Alias=Follower099>`.
  - 101 aliases total: a generic `Follower` alias plus `Follower000..Follower099`.
  - every follower alias has the same ALPS package override stack:
    - follow;
    - sandbox;
    - run to mount;
    - mounted;
    - passive;
    - sneak;
    - follow/run variants.
- Hidden storage:
  - `FollowerPluginCell` contains 100 placed `FollowerInventoryContainer` refs (`FollowerInventory000..099`).
  - one `FollowerGlobalContainer` for unclaimed/global items.
  - one train storage ref.
- Global switches:
  - `PlayerFollowerMaximum`
  - `PlayerFollowerCountEx`
  - `PlayerFollowerRideHorse`
  - `PlayerFollowerMenu`
  - plugin toggles for collect/outfit/train/combat/sandbox/importance/spells/aggression/etc.
- Main faction:
  - `CurrentFollowerExtendedFaction`
  - hidden/vendor/can-be-owner, with NOT-sell list and relations to player / itself.

## Plugin-module architecture

Feature quests:

- `FollowerPluginOutfit`
- `FollowerPluginCollect`
- `FollowerPluginTrain`
- `FollowerPluginCombat`
- `FollowerPluginSandbox`
- `FollowerPluginImportance`
- `FollowerPluginSpells`
- `FollowerPluginAggression`
- `FollowerPluginTattoo`
- `FollowerPluginNickname`

Menu/dialogue topics are also modular:

- command: wait/follow/relax/trade/stats/dismiss.
- group command: group wait/follow/relax/dismiss.
- plugin command pages: outfit, collect, train, etc.
- many outfit choices are represented as dialogue leaves, not one monolithic script UI.

## Package patterns

`PlayerFollowerFollowPackage`:

- template `0D530D:Skyrim.esm`;
- preferred speed Run;
- targets player `000014:Skyrim.esm`;
- near-self fallback and distance thresholds.

`PlayerFollowerSandboxAlias`:

- owner quest `FollowerPluginSandbox`;
- location target is `AliasForReference` with large radius `8192`;
- this is a concrete example of package location = quest alias.

`PlayerFollowerHarvestPackage`:

- collection behavior near player;
- target object type / radius / item category options encoded in package data.

## Magic/script entry points

EFF exposes command powers:

- `FollowerTelepathy`
- `FollowerFocusFire`
- `FollowerPortal`
- `FollowerTeleport`
- `MindControl`

These are SPEL → MGEF → script entry points, e.g. `AbFollowerTelepathy` is a scripted self-target magic effect with `EFFTelepathy` properties.

## ModForge relevance

Already supported:

- quest aliases and alias package override data are visible via `questdiag`; ModForge already has quest aliases and package alias targeting.
- package templates are already generated for follow/sandbox/collect-like behavior.
- hidden interior cell + placed containers are ordinary records.

Missing / useful convenience layer:

- A `slotFactory` / `aliasArray` generator:
  - create N aliases with identical flags and ALPS packages;
  - create N objectives with `<Alias=...>`;
  - optionally create N matching storage containers in a hidden cell.
- This is not required for small followers, but becomes important for M&B squads, follower memory, or generated party systems.

Design lesson:

- EFF scales by **fixed slots** rather than unbounded dynamic arrays.
- This is very Skyrim-native: alias slots, objective slots, and storage refs are concrete records. It avoids relying on Papyrus arrays for large state.
- For ModForge, the right abstraction is not “support EFF” but “generate EFF-like slot banks.”

Compatibility lesson:

- Framework followers and custom-voiced followers may not use the same controller path. EFF-style systems expand vanilla followers; custom followers with their own AI package stack may need import/export or explicit compatibility hooks.
