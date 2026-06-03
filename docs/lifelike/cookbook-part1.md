<!-- Part 1/5 — Basic NPC patterns -->
# Recipe cookbook

Copy-paste starting points. Combine with the [TL;DR NPC recipe](README.md#tldr--the-complete-npc-recipe)
and resolve every `<...>` / FormID against [formid-reference](formid-reference.md).

← back to [lifelike hub](README.md)

## "Inn patron" (Sandbox only)

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

## "Cross-city commuter" (Travel + Sandbox + citizenship)

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

## "Combat-capable mage"

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

## "Friendly self-defender" (townsperson who fights only when attacked)

A deliberate contrast to the combat-mage: do **not** use `Aggressive` (it risks treating the
player as hostile). Aggression governs *initiation*; `Brave` governs flee-vs-stand once attacked.

```jsonc
{ "npcs": [
    { "editorId": "MF_Townsperson", ...,
      "combatStyle": "<MF_BalancedCS>",
      "aggression": "Unaggressive",   // never starts a fight
      "confidence": "Brave",          // but stands and fights once attacked
      "assistance": "HelpsFriendsAndAllies" }
  ] }
```

## "Ritual caster" (UseMagic — non-combat scheduled spellcasting)

```jsonc
{ "packages": [
    { "editorId": "MF_Ritual", "template": "Skyrim.esm:0x0504F5",
      "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter" ],
      // Both knobs needed for CONTINUOUS casting (see gotchas) — without them the
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

## "Companion that trails the player" (Follow — movement layer only)

A Follow package targeting the player + the citizenship recipe makes a generated NPC trail the
player **and persist across fast travel** (the engine fast-travels actors running a follow-the-player
package). No managing quest is needed for the *movement* layer — the hire/dismiss dialogue + follow
faction (see the [hireable-follower gotcha](gotchas.md)) are only what make it player-toggleable.

```jsonc
{ "packages": [
    { "editorId": "MF_FollowPlayer", "template": "Skyrim.esm:0x019B2C",
      "follow": { "target": "", "minRadius": 128, "maxRadius": 256, "accompany": true } }
  ],                                  // target "" ⇒ defaults to the player
  "npcs": [ { "editorId": "MF_Companion", ..., "packages": [ "MF_FollowPlayer" ] } ] }
```

