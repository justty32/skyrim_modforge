# LIFELIKE_NPC.md — distilled recipes for making generated NPCs feel alive

> The evergreen knowledge from `NOTES.md` It.16–It.17, refactored for lookup. NOTES.md still has
> the chronological iteration log + the bug-by-bug discovery story; this doc is "what we know now."

## TL;DR — the complete recipe

```jsonc
{
  "race":         "Skyrim.esm:0x013746",     // NordRace (or other)
  "class":        "<MF_YourClass>",
  "voiceType":    "Skyrim.esm:0x013AE6",     // MaleNord — hello/idle audio
  "outfit":       "Skyrim.esm:0x09D5DF",     // BlacksmithOutfit01 (any vanilla outfit)
  "level":        25,
  "autoCalcStats": true,                       // class drives H/M/S + skill values

  // CITIZENSHIP — required for cross-cell Travel (engine refuses door teleports without it)
  "crimeFaction": "Skyrim.esm:0x0267EA",       // CrimeFactionWhiterun
  "factions":     [ "Skyrim.esm:0x0267EA",     // (reinforcing)
                    "Skyrim.esm:0x028172" ],   // TownWhiterunFaction
  "unique":       true,                         // engine AI tracking — vanilla cross-cell NPCs all have this

  // COMBAT — both systems must be authored
  "combatStyle":  "<MF_YourCS>",               // HOW he fights (weapon-class preference)
  "spells":       [ "Skyrim.esm:0x0C969A" ],   // WHAT he casts (FlamesRightHand)
  "aggression":   "Aggressive",                 // WHETHER he fights — default Unaggressive = won't even defend
  "confidence":   "Brave",                      // default Cowardly = flees any threat
  "assistance":   "HelpsFriendsAndAllies",
  "energyLevel":  50,

  // BEHAVIOUR — engine evaluates in list order
  "packages":     [ "<MF_TravelPkg>",          // first priority — go somewhere
                    "<MF_SandboxPkg>" ]        // fallback — what to do once arrived
}
```

Drop any line above and the actor degrades visibly in-game (see the gotchas table below for the
exact failure mode each omission produces).

## The KEY insight — Skyrim NPC AI has two independent systems

| System | Decides | Authored via | Default if unset |
|---|---|---|---|
| **CombatStyle** (CSTY) | **HOW** the NPC fights (magic vs melee vs staff vs ranged) | `combatStyle` ref → CSTY record (`equipMult*` fields) | Flat default — picks whatever weapon the actor happens to hold |
| **AIData.Aggression + Confidence** | **WHETHER** the NPC fights at all | `aggression` / `confidence` on the NPC | `Unaggressive + Cowardly` → **flees any threat**, regardless of CombatStyle |

A CSTY-only setup gives you "wants to use magic but flees the moment it sees a wolf" — the It.17
round-1 failure mode. **Both systems must be authored.**

## Gotchas — the traps that bit us, with the fix

| Symptom | Root cause | Fix |
|---|---|---|
| Generated package's `target` reads as `LocationTarget` instead of `LocationFallback` even though we used `new LocationFallback()` | Mutagen picks the binary shape from `LocationFallback.Type` enum, NOT the C# class | `new LocationFallback { Type = LocationTargetRadius.LocationType.NearSelf }` |
| Sandbox-equipped NPC stands still indefinitely doing nothing | Used `Type = NearEditorLocation` — needs CK-set Editor Location which Mutagen-generated NPCs lack | Use `Type = NearSelf` — anchors at current position, no external link |
| NPC doesn't move, just stands at spawn (after ~1 min delay still nothing) | Sandbox finds no furniture / idle markers / other NPCs nearby — barren area | Move spawn to a populated cell (Bannered Mare, Sleeping Giant Inn). Sandbox **needs** content to interact with |
| Travel package in spec but NPC ignores it (sandboxes locally instead) | Engine silently rejected cross-cell Travel — NPC has no "citizen" identity to traverse city gates | Set `crimeFaction` + add the town's faction to `factions` + `unique: true` |
| NPC walks but never speaks; only mumbles "嗯/啊" when approached | No voiceType set, OR voice set but no faction-conditioned dialogue topics match | Set `voiceType: MaleNord` (or similar). For more chatter, add a town faction so faction-conditioned dialogue topics apply |
| Mage NPC just runs away from any threat | `Aggression=Unaggressive + Confidence=Cowardly` (Mutagen defaults) — flees regardless of CombatStyle | `aggression: "Aggressive"` + `confidence: "Brave"` |
| UseMagic NPC stands at location but NEVER casts | Slot 3 "Spell" authored as `PackageTargetObjectType` (category enum). Template default is misleading — all 46 vanilla UseMagic packages use `PackageTargetObjectID` with a FormLink to a specific SPEL | Spec field `useMagic.spell: "<master>:0xFORMID"` of the SPEL; Build writes `PackageTargetObjectID`. Spell IS an `IObjectId` so the FormLink works |
| UseMagic NPC casts 1-2 times then stops forever | `numToCastMax` is **total package-lifetime casts**, not per-cycle. With `schedule.durationInMinutes=0` (default) the package completes the moment its quota's hit | `numToCastMax: 1000` + `schedule.durationInMinutes: 1440` (24h continuous), mirroring vanilla `WCollegeOnmundPracticeFlames12x4` |
| UseMagic NPC stops casting when combat starts | Vanilla behaviour — combat AI preempts idle packages | If you want casting to continue (e.g. boss ritual), add `flags: [ "IgnoreCombat" ]` like vanilla `SprigganCallOverride` |
| Generated NPC appears with no model when dropped (or crashes on equip/read) | Weapon/Book/Misc/Potion need a `template` ref to clone a vanilla model from | Set `template: "Skyrim.esm:0x012EB7"` (IronSword) etc. — see SPEC.md item types |
| Custom MGEF heal spell casts but doesn't heal | `Recover` flag on instant effect — reverts the heal when the effect "ends" (immediately) | `["NoDuration","NoArea"]` instant effects must NOT use `Recover` |
| Sandbox NPC stands still for the first 30–90 seconds after cell load | Engine sandbox cold-start delay; normal | **Wait the full minute** before declaring failure — vanilla NPCs hide this because they're initialised long before the player arrives |
| `coc <interior>` then walk out → terrain LOD breaks at city gate | `coc` skips the normal load screen, exterior LOD doesn't preload | Fast-travel away + back, OR `coc <exteriorMarker>` directly |
| Patrol NPC (or any path-to-marker behaviour) stands still, never moves | A **static REFR does NOT snap to the floor** like an actor does — a marker placed at a guessed exterior z lands off-navmesh, so pathing to it silently fails (It.19 round 1, open wilderness) | Anchor markers on coords PROVEN walkable: `refpos <plugin> <0xFORMID>` to copy a vanilla reachable ref's position, or place inside a hand-navmeshed interior. Actors (ACHR) snap to ground so their spawn z is forgiving; markers (REFR) do not |

## Vanilla FormID reference

### Procedure templates (for `packages[].template`)

| Template | FormID | Slots used | Use when |
|---|---|---|---|
| Sandbox | `Skyrim.esm:0x01C254` | 12 | NPC hangs around a location, interacts with furniture/idle markers/other NPCs |
| Travel | `Skyrim.esm:0x016FAA` | 3 | NPC walks to a specific REFR/cell |
| Patrol | `Skyrim.esm:0x017723` | 6 | Guard route. Wired in `packages[].patrol` (`start` → first marker placement); the route is the markers' `linkedRefs` chain (m1→m2→m3→m1 looped, null keyword). Markers must be on navmesh — see the static-marker gotcha below |
| UseMagic | `Skyrim.esm:0x0504F5` | 11 | Scheduled non-combat spell casting (priest at altar, mage self-buffing). NPC picks one spell from `spells` matching `spellType` (TargetObjectType enum). Wired in `packages[].useMagic` |
| Follow | `Skyrim.esm:0x019B2C` | 6 | NPC physically follows the player (or another actor). Wired in `packages[].follow` (`target` defaults to the player `0x000014`; `minRadius`/`maxRadius`/`accompany`). Raw tag-along movement only — a hireable follower also needs a managing quest + follow faction + dialogue |
| Escort | `Skyrim.esm:0x023B73` | 9 | **Dual of Follow** — NPC LEADS an escorted target to a destination, pausing if they lag. Wired in `packages[].escort` (`target` defaults to the player; `destination` → a location ref / authored marker; `waitDistance`/`followerMin/MaxDistance`). Same navmesh rules as Patrol/Travel; the destination marker is auto-persisted |
| UseWeapon | `Skyrim.esm:0x01C338` | — | Practice attacks at a target — not yet ModForge-supported |

### Voice types (for `voiceType`)

| Editor ID | FormID |
|---|---|
| MaleNord | `Skyrim.esm:0x013AE6` |
| FemaleNord | `Skyrim.esm:0x013AE7` |
| MaleNordCommander | `Skyrim.esm:0x0E5003` |

Without a voice type, NPC is silent — no hello/idle audio, no subtitles.

### Factions for "city citizenship" (for `crimeFaction` + `factions`)

| Editor ID | FormID | Use for |
|---|---|---|
| CrimeFactionWhiterun | `Skyrim.esm:0x0267EA` | Whiterun |
| TownWhiterunFaction | `Skyrim.esm:0x028172` | Whiterun (reinforcing) |

Other hold crime/town factions follow the same naming pattern; `find <Skyrim.esm> CrimeFaction Faction`.

### CombatStyle profiles (harvest via `cstydiag`)

| Editor ID | FormID | OffMult | DefMult | EquipMult (M/Mg/R/Sh/U/St) | Avoid | Flags | Use for |
|---|---|---|---|---|---|---|---|
| csVampireMagic | `Skyrim.esm:0x02DFB5` | 0.77 | 0.3 | 0.51 / 8.1 / 0.55 / 0.21 / 0.98 / 2.15 | 0.2 | Dueling | Strong mage |
| csSoldierMagic | `Skyrim.esm:0x046B9E` | 0.5 | 0.5 | 1 / 3 / 1 / 1 / 1 / 0 | 0 | — | Battlemage (balanced lean) |
| csForswornMagic | `Skyrim.esm:0x0442CD` | 0.5 | 0.5 | 1 / 1 / 1 / 1 / 1 / 1 | 0.2 | Dueling | Balanced — NAME IS MISLEADING |

### Markers for placement / Travel destinations

| Editor ID | FormID | Worldspace | Notes |
|---|---|---|---|
| WhiterunBanneredMare (cell) | `Skyrim.esm:0x01605E` | interior | `coc` target |
| RiverwoodSleepingGiantInn (cell) | `Skyrim.esm:0x0133C6` | interior | `coc` target |
| RiverwoodInnCenterMarker | `Skyrim.esm:0x01DC0A` | inside the inn | In-cell Travel target (It.16b) |
| debugWhiterunOrigin | `Skyrim.esm:0x0567F7` | WhiterunWorld | `coc whiterun` target — inside city walls |
| debugRiverwood | `Skyrim.esm:0x0567F6` | Tamriel | Riverwood exterior |
| WhiterunStablesHorseMarker | `Skyrim.esm:0x109826` | Tamriel | Just outside Whiterun's main gate (It.16d) |
| Tamriel (worldspace) | `Skyrim.esm:0x00003C` | — | Worldspace ref for exterior `placements` |

### Test enemy bases (for in-game `placeatme <id> 1`)

| Editor ID | FormID |
|---|---|
| EncWolfIce_Indoor | `Skyrim.esm:0x10F2A3` |
| EncWolf_Indoor | `Skyrim.esm:0x10F2A2` |
| EncBandit05MagicArgonianM | `Skyrim.esm:0x0C3CA7` |

### Vanilla spells / effects for `spells` list

| Editor ID | FormID | Notes |
|---|---|---|
| FlamesRightHand | `Skyrim.esm:0x0C969A` | Novice destruction cone — good for first mage test |
| SparksRightHand | `Skyrim.esm:0x0C96A1` | Shock variant |
| FireboltStormBasic | `Skyrim.esm:0x0D07CD` | Apprentice fire projectile |

### Outfits

| Editor ID | FormID |
|---|---|
| BlacksmithOutfit01 | `Skyrim.esm:0x09D5DF` |

## Diagnostic commands cheat sheet

```bash
cd ~/repo/ModForge
R="dotnet run --project src/ModForge.Cli --no-build --"   # NOT shell-safe as one var; expand inline

# Find vanilla forms by editorID substring (use [Type] to narrow)
dotnet run --project src/ModForge.Cli --no-build -- find <Skyrim.esm> "Ysolda" Npc
dotnet run --project src/ModForge.Cli --no-build -- find <Skyrim.esm> "CrimeFaction" Faction

# Inspect specific record types — used to diff vanilla vs our generated records
dotnet run --project src/ModForge.Cli --no-build -- packagediag <plugin> <0xFORMID>   # PACK: template, flags, schedule, Data slots
dotnet run --project src/ModForge.Cli --no-build -- npcdiag     <plugin> <0xFORMID>   # NPC: race/class/voice/factions/CrimeFaction/AIData/packages/spells
dotnet run --project src/ModForge.Cli --no-build -- cstydiag    <plugin> <0xFORMID>   # CSTY: offensive/defensive/equip mults/flags
dotnet run --project src/ModForge.Cli --no-build -- mgefdiag    <plugin> <0xFORMID>   # MGEF: archetype/AV/flags/projectile/casting art
dotnet run --project src/ModForge.Cli --no-build -- lightdiag   <plugin> [0xFORMID]   # LIGH (no ID lists candidates)
dotnet run --project src/ModForge.Cli --no-build -- refpos      <plugin> <0xFORMID>   # REFR/ACHR: position+rotation+base (anchor new placements on known navmesh)
dotnet run --project src/ModForge.Cli --no-build -- cellblk     <plugin> [0xFORMID]   # Cell block/sub-block by FormID

# Build / dump round-trip
dotnet run --project src/ModForge.Cli --no-build -- validate <spec.json>              # ALWAYS run first
dotnet run --project src/ModForge.Cli --no-build -- build    <spec.json> <out.esp>
dotnet run --project src/ModForge.Cli --no-build -- dump     <out.esp>                # See what we actually wrote
dotnet run --project src/ModForge.Cli --no-build -- package  <spec.json> <outDir>     # esp + .pex
```

## In-game console cheat sheet (for testing generated NPCs)

```
help "ModForge X" 0                # find an NPC's runtime FormID (FExx0XXX form for ESL)
prid <FormID>                       # select an NPC by FormID
player.moveto <FormID>              # teleport player to NPC
moveto player                       # teleport selected NPC to player
getCurrentPackage                   # what package is the engine running on this NPC?
evp                                 # force re-evaluate packages (shortcut for evaluatePackage)
placeatme <baseFormID> <count>      # spawn an enemy (e.g. placeatme 0x10F2A3 1 → wolf)
getav health|magicka|stamina        # read selected actor's stats
coc <cellEditorID>                  # teleport to a cell (no load screen → LOD may break briefly)
tcl                                 # toggle clip / no-clip
```

## Recipe cookbook

### "Inn patron" (Sandbox only)

```jsonc
{ "packages": [
    { "editorId": "MF_InnSandbox", "template": "Skyrim.esm:0x01C254",
      "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter", "WorldInteractions" ],
      "sandbox": { "radius": 512, "allowEating": true, "allowSleeping": false,
                    "allowConversation": true, "allowIdleMarkers": true,
                    "allowSitting": true, "allowWandering": true,
                    "allowSpecialFurniture": true, "energy": 50.0 } }
  ],
  "npcs": [
    { "editorId": "MF_Patron", "race": "Skyrim.esm:0x013746", "class": "<...>",
      "voiceType": "Skyrim.esm:0x013AE6", "level": 5, "autoCalcStats": true,
      "packages": [ "MF_InnSandbox" ] }
  ],
  "placements": [
    { "base": "MF_Patron", "cell": "Skyrim.esm:0x01605E",   // Bannered Mare
      "position": { "x": 0, "y": 0, "z": 0 } }
  ] }
```

### "Cross-city commuter" (Travel + Sandbox + citizenship)

Add to the inn-patron above:
```jsonc
{ "packages": [
    { "editorId": "MF_GoOut", "template": "Skyrim.esm:0x016FAA",
      "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter" ],
      "travel": { "place": "Skyrim.esm:0x109826", "radius": 256 } },  // stables
    { "editorId": "MF_InnSandbox", ... }                                 // as above
  ],
  "npcs": [
    { "editorId": "MF_Commuter", ...,
      "crimeFaction": "Skyrim.esm:0x0267EA",
      "factions":     [ "Skyrim.esm:0x0267EA", "Skyrim.esm:0x028172" ],
      "unique":        true,
      "packages": [ "MF_GoOut", "MF_InnSandbox" ] }  // order matters: Travel first
  ] }
```

### "Combat-capable mage"

```jsonc
{ "combatStyles": [
    { "editorId": "MF_MageCS",
      "offensiveMult": 0.77, "defensiveMult": 0.3, "groupOffensiveMult": 0.74,
      "equipMultMelee": 0.51, "equipMultMagic": 8.1, "equipMultRanged": 0.55,
      "equipMultShout": 0.21, "equipMultUnarmed": 0.98, "equipMultStaff": 2.15,
      "avoidThreatChance": 0.2, "flags": [ "Dueling" ] }
  ],
  "npcs": [
    { "editorId": "MF_Mage", ..., "level": 25, "autoCalcStats": true,
      "combatStyle": "MF_MageCS",
      "spells":     [ "Skyrim.esm:0x0C969A" ],   // Flames
      "aggression": "Aggressive",                 // CRITICAL — without this he flees
      "confidence": "Brave",                      // CRITICAL — without this he flees
      "assistance": "HelpsFriendsAndAllies", "energyLevel": 50 }
  ] }
```

Class should be magicka-heavy with Destruction-favouring skill weights.

### "Ritual caster" (UseMagic — non-combat scheduled spellcasting)

```jsonc
{ "packages": [
    { "editorId": "MF_Ritual", "template": "Skyrim.esm:0x0504F5",
      "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter" ],
      // Both knobs needed for CONTINUOUS casting (see gotchas table) — without them the
      // package completes after numToCastMax casts and the NPC goes idle.
      "schedule": { "hour": -1, "minute": -1, "durationInMinutes": 1440, "dayOfWeek": "Any" },
      "useMagic": {
        "spell":         "Skyrim.esm:0x043324",   // SPEL FormLink — NOT a category enum
        "radius":        256,
        "target":        "",                      // optional placed-ref; omit ⇒ PackageTargetSelf
        "castTimeMin":   1.5, "castTimeMax":   2.5,
        "cooldownTimeMin": 8.0, "cooldownTimeMax": 12.0,
        "numToCastMin":  1, "numToCastMax":  1000,
        "dualCast":      false } }
  ],
  "npcs": [
    { "editorId": "MF_Priest", ..., "level": 15, "autoCalcStats": true,
      "spells":   [ "Skyrim.esm:0x043324" ],   // Candlelight (self-cast, visible orb)
      "aggression": "Aggressive", "confidence": "Brave",
      "packages": [ "MF_Ritual" ] }
  ] }
```

The "Spell" slot is a `PackageTargetObjectID` FormLink to a specific SPEL record — NOT a category
enum. The target slot defaults to `PackageTargetSelf` (correct for self-cast spells like
Candlelight/Healing/Ward); set `target` to a placed-ref for cast-at-X. Combat preempts UseMagic
unless you add `flags: [ "IgnoreCombat" ]`.

## Related docs in this repo

- **`SPEC.md`** — full per-field spec reference (all record types)
- **`FOR_AGENT.md`** — how an AI agent operates ModForge (NL → spec → build loop)
- **`README.md`** — quickstart + CLI summary
- **`NOTES.md`** — the chronological iteration log; consult for "why did we make this decision?"
