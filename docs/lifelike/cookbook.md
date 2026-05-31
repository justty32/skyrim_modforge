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

## "Recruitable follower" (hire → follow → dismiss), in-game confirmed

Hard-won lessons (It.27–It.30 — see the [follower gotchas](gotchas.md) and probe everything with
`infodiag` first):

- **You cannot reuse vanilla's PAID hireling line.** Every recruit INFO in `HirelingQuestTopic1` is
  gated `GetIsID==<a specific vanilla mercenary>`, so a custom NPC fails them all (and the topic even
  *vanishes* once you can afford it). `PotentialHireling` membership alone only buys the refusal line.
- **`SetPlayerTeammate(true)` ≠ follows.** It makes her fight for you and obey commands, but physical
  following needs a **Follow package** targeting the player.
- **Don't piggyback `CurrentFollowerFaction` for dismiss.** Vanilla's dismiss line is driven by the
  *DialogueFollower quest* and only releases followers it registered; a manually-faction'd NPC gets the
  "you're dismissed" notification but keeps following. **Manage follower state yourself.**

**Two paths that DO work for a custom NPC:**

**(a) Free "Follow me, I need your help"** — reuse vanilla's free-follow topic (`0x0B0EE6`), which gates
on relationship + follower voice, *not* GetIsID. Needs: a follower voice (e.g. `FemaleEvenToned
0x013ADD`), `PotentialFollowerFaction 0x05C84D`, **not** `PotentialHireling`, a `greeting` (so she's
conversable), and a tiny quest script setting the relationship (a static player RELA reads 0 at
runtime). See `examples/follower_hireable_spec.json` + `MFHireFollowerSetup.psc`.

**(b) Paid, fully self-managed** (recommended — reliable hire *and* dismiss). An OWN flag faction is the
"is my follower" state; OWN recruit + dismiss topics carry result fragments; the Follow package gates on
the flag. Skeleton (full: `examples/follower_paid_spec.json` + `MFHirePaidRecruit/Dismiss.psc`):

```jsonc
{ "factions": [ { "editorId": "MF_FollowerFlag", "name": "My Follower" } ],
  "packages": [
    { "editorId": "MF_FollowPkg", "template": "Skyrim.esm:0x019B2C",
      "follow": { "target": "" },                                   // ⇒ player
      "conditions": [ { "function": "GetInFaction", "comparison": "==", "value": 1,
                        "param": "MF_FollowerFlag", "runOn": "Subject" } ] }  // follow only while hired
  ],
  "npcs": [ { "editorId": "MF_Merc", "voiceType": "Skyrim.esm:0x013ADD", "greeting": "Coin talks.",
              "factions": [ "Skyrim.esm:0x0267EA", "Skyrim.esm:0x028172" ],   // citizenship, NOT a follower faction
              "packages": [ "MF_FollowPkg" ], "unique": true } ],
  "quests": [ { "editorId": "MF_Q", "startGameEnabled": true } ],
  "dialogue": [
    { "editorId": "MF_Hire", "questEditorId": "MF_Q", "speakerNpcEditorId": "MF_Merc",
      "prompt": "Here's 500 gold. Fight at my side.", "responses": [ "Lead the way." ], "goodbye": true,
      "conditions": [                                                  // hide unless affordable & not hired
        { "function": "GetItemCount", "comparison": ">=", "value": 500, "param": "Skyrim.esm:0x00000F",
          "runOn": "Reference", "reference": "Skyrim.esm:0x000014" },
        { "function": "GetInFaction", "comparison": "==", "value": 0, "param": "MF_FollowerFlag", "runOn": "Subject" } ],
      "resultScript": "MFHirePaidRecruit", "resultScriptSource": "scripts/MFHirePaidRecruit.psc",
      "resultProperties": [ { "name": "Gold001", "type": "object", "objectEditorId": "Skyrim.esm:0x00000F" },
        { "name": "FollowerFaction", "type": "object", "objectEditorId": "MF_FollowerFlag" },
        { "name": "GoldCost", "type": "int", "int": 500 }, { "name": "RelRank", "type": "int", "int": 3 } ] },
    { "editorId": "MF_Dismiss", "questEditorId": "MF_Q", "speakerNpcEditorId": "MF_Merc",
      "prompt": "Let's part ways.", "responses": [ "Aye." ], "goodbye": true,
      "conditions": [ { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "MF_FollowerFlag", "runOn": "Subject" } ],
      "resultScript": "MFHirePaidDismiss", "resultScriptSource": "scripts/MFHirePaidDismiss.psc",
      "resultProperties": [ { "name": "FollowerFaction", "type": "object", "objectEditorId": "MF_FollowerFlag" } ] }
  ] }
```
Recruit fragment: `AddToFaction(FollowerFaction)` + `SetPlayerTeammate(true)` (after taking gold).
Dismiss fragment: `RemoveFromFaction(FollowerFaction)` + `SetPlayerTeammate(false)` + `EvaluatePackage()`.

**Trade / wait / follow-again** (the full example wires these too — same fragment pattern):
- **Trade**: a topic gated on `FollowerFlag==1`, **not** `goodbye` (so the menu opens over the
  dialogue, like vanilla), fragment `akSpeaker.OpenInventory(true)`.
- **Wait / resume**: use the **`WaitingForPlayer` ActorValue** vanilla itself uses. Gate the Follow
  package on `GetActorValue WaitingForPlayer == 0` (added to `FollowerFlag==1`). A "wait here" topic
  (gated `WaitingForPlayer==0`) sets it to 1 (`SetActorValue("WaitingForPlayer", 1.0)` + `EvaluatePackage`)
  so she holds position; a "follow me again" topic (gated `WaitingForPlayer==1`) clears it. Dismiss
  clears it too. See `MFFollowerTrade/Wait/Follow.psc`.

## "Usable interior cell" (lighting + floor, not a black void)

A brand-new interior cell needs three things or it's a pitch-black void you fall through:

```jsonc
{ "cells": [
    { "editorId": "MF_Hall", "name": "Forged Hall",
      "template": "Skyrim.esm:0x0165A8" }   // Breezehome — inherits interior lighting via CopyCellEnv
  ],
  "statics": [ { "editorId": "MF_Floor", "model": "..." } ],  // or place vanilla WRIntFloorSTMid01Large 0x1044AA
  "placements": [
    // a 3×3 floor grid at 256 spacing, a non-PortalStrict omni key light, wall pieces
    { "base": "Skyrim.esm:0x1044AA", "cell": "MF_Hall", "position": { "x": 0,   "y": 0, "z": 0 } },
    { "base": "Skyrim.esm:0x0C82AE", "cell": "MF_Hall", "position": { "x": 0,   "y": 0, "z": 200 } } // WRShadowOmni key light
  ] }
```

Lighting comes from the `template` (code path `CopyCellEnv`); floor + light are just placements.
Use a non-PortalStrict omni light (`WRShadowOmni 0x0C82AE`) — a `PortalStrict` light lights nothing
in a portal-less cell.

## "Craftable item" (COBJ recipe)

Simpler than it looks: the workbench is a plain keyword FormLink (defaults to the forge), **not** a
CTDA condition; components reuse the container item/count shape; perk/skill gating (`Conditions`) is
optional and a basic recipe needs none.

```jsonc
{ "recipes": [
    { "editorId": "MF_ForgeSword", "createdObject": "<MF_MySword>", "count": 1,
      // "workbench": "Skyrim.esm:0x088105",   // forge — this is the default, can omit
      "components": [ { "item": "Skyrim.esm:0x05ACE4", "count": 3 },    // IngotIron
                      { "item": "Skyrim.esm:0x0800E4", "count": 1 } ] } // LeatherStrips
  ] }
```

## "Custom aimed combat spell" (MGEF + projectile + SPEL)

```jsonc
{ "magicEffects": [
    { "editorId": "MF_Firebolt", "archetype": "ValueModifier", "actorValue": "Health",
      "magicSkill": "Destruction", "resistValue": "ResistFire",
      "castType": "FireAndForget", "targetType": "Aimed", "baseCost": 12.0,
      "flags": [ "Hostile", "Detrimental", "NoArea" ],   // NOT Recover (it's instant)
      "projectile": "Skyrim.esm:0x10FBEA",               // reuse vanilla firebolt projectile (visible bolt + impact)
      "castingArt": "Skyrim.esm:0x01B211" }              // hands FX
  ],
  "spells": [
    { "editorId": "MF_FireboltSpell", "name": "Forged Firebolt",
      "spellType": "Spell", "castType": "FireAndForget", "targetType": "Aimed",
      "equipType": "Skyrim.esm:0x013F44",                // EitherHand — REQUIRED or the NPC can't equip/cast it
      "effects": [ { "magicEffect": "MF_Firebolt", "magnitude": 25, "area": 0, "duration": 0 } ] }
  ] }
```

Reusing a vanilla `projectile` + `castingArt` is what makes the bolt visible and lets it deliver the
hit. Without `equipType` the NPC melees / never casts — the #1 silent failure for a generated combat spell.

## "Conversational NPC" (custom player topics — IN-GAME CONFIRMED It.23)

Give the NPC a `greeting`, a host quest, and one `dialogue` entry per topic. From that the build emits
the whole vanilla chain (Quest→DialogView→Branch→Topic→INFO + a Hello), so the topic actually shows.

```jsonc
{ "quests": [ { "editorId": "MF_TalkQuest", "name": "...", "startGameEnabled": true } ],
  "npcs": [
    { "editorId": "MF_Talker", "name": "Aldric", "race": "Skyrim.esm:0x013746",
      "voiceType": "Skyrim.esm:0x013AE6",            // a real voice type — silent NPCs don't greet
      "greeting": "Welcome. What brings you here?",   // REQUIRED: emits the Hello that makes him conversable
      "factions": [ "Skyrim.esm:0x028172" ] } ],
  "dialogue": [
    { "editorId": "MF_AboutPlace", "questEditorId": "MF_TalkQuest", "speakerNpcEditorId": "MF_Talker",
      "prompt": "Tell me about this place.", "emotion": "Happy",
      "responses": [ "Everything here was forged on Linux.", "No Creation Kit needed." ] } ],
  "placements": [
    { "base": "MF_Talker", "cell": "Skyrim.esm:0x0133C6",
      "position": { "x": -350, "y": 180, "z": 0 } } ]   // a REAL in-room coord, NOT (0,0,0)
}
```

Three things that each independently make it silently fail (all are now handled by the build except
the last two, which are yours to get right) — see [gotchas.md](gotchas.md):
- **`greeting`** must be set, or the NPC isn't conversable (no dialogue camera). The build turns it into
  a Hello info; the topic chain (SNAM='CUST', DialogView, INFO ENAM/CNAM) is emitted automatically.
- **Placement** must be a real in-room coordinate — a no-package NPC at `(0,0,0)` lands off-navmesh and
  you can't reach it (you end up talking to a vanilla NPC).
- **Unvoiced lines** flash past — install **Fuz Ro D-oh** (or bundle silent `.fuz`) and turn on subtitles.
