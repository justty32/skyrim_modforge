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

**Three paths — prefer the vanilla-integrated ones (a)/(c); they're compatible with follower-manager
mods (AFT/EFF/NFF) and need no custom command dialogue.**

**(a) Free "Follow me, I need your help"** — reuse vanilla's free-follow topic (`0x0B0EE6`), which gates
on relationship + follower voice, *not* GetIsID. Needs: a follower voice (e.g. `FemaleEvenToned
0x013ADD`), `PotentialFollowerFaction 0x05C84D`, **not** `PotentialHireling`, a `greeting` (so she's
conversable), and a tiny quest script setting the relationship (a static player RELA reads 0 at
runtime). See `examples/follower_hireable_spec.json` + `MFHireFollowerSetup.psc`.

**(c) Paid via vanilla `SetFollower` — RECOMMENDED for a paid follower (the user prefers this).** Author
your own paid recruit topic, but in its fragment hand the NPC straight to the vanilla follower system:
```papyrus
Quest Property DialogueFollower Auto   ; bound to Skyrim.esm:0x0750BA
...
player.RemoveItem(Gold001, 500)
(DialogueFollower as DialogueFollowerScript).SetFollower(akSpeaker)   ; compiles vs base scripts
```
`SetFollower` sets relationship + `SetPlayerTeammate` + `ForceRefTo`'s the follower alias (which carries
the follow package and adds `CurrentFollowerFaction`). After that, **vanilla's own trade/wait/follow/
dismiss dialogue all work** and AFT/EFF/NFF pick her up — no custom command topics needed. Gate the
recruit line on `GetGlobalValue PlayerFollowerCount (0x0BCC98) == 0` so it never stomps the single
follower slot. See `examples/follower_vanilla_spec.json` + `MFHireVanillaRecruit.psc`.

> **What survives vanilla follower status** (the worry about losing your lifelike work): a follower is
> just an alias stacking a high-priority *follow package* on top — it's additive, not destructive.
> **CombatStyle is preserved** (verified: `PlayerFollowerPackage`/the combat-override package set no
> CombatStyle, so the actor's base CSTY drives combat). **Your custom dialogue is preserved** and can be
> *conditioned on* follower state — e.g. a follower-only self-introduction gated on `GetInFaction
> CurrentFollowerFaction (0x05C84E) == 1` (see the vanilla example). **Sandbox/travel/schedule packages
> are only out-prioritized while she's actively trailing you** (you can't both follow and commute) and
> resume the moment she's dismissed or told to wait.

**(b) Paid, fully self-managed** — *the user found this less preferable than (c); kept for reference.* No
vanilla follower involvement: an OWN flag faction is the "is my follower" state; OWN recruit + dismiss +
trade + wait topics carry result fragments; the Follow package gates on the flag. Upside: zero conflicts,
no single-slot limit, runs alongside a real vanilla follower. Downside: you re-implement every command,
and follower-manager mods don't see her. Skeleton (full: `examples/follower_paid_spec.json` +
`MFHirePaidRecruit/Dismiss.psc`):

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

## "Lifelike follower" extras — downtime + situational lines (It.33, in-game confirmed)

Once the hire/follow plumbing works, two cheap additions make a follower feel alive. Both are in
`examples/follower_vanilla_spec.json`.

**Downtime behaviour** — give the follower NPC an *unconditioned* Sandbox package. It's her
lowest-priority fallback, so it runs exactly when the vanilla follow-alias package is NOT active:
before recruit, after dismiss, and while she's told to wait. Instead of standing frozen she
eats/sits/wanders wherever she's placed. While actively trailing you the alias package overrides it;
combat preempts it and she resumes after.
```jsonc
"packages": [ { "editorId": "MF_Sandbox", "template": "Skyrim.esm:0x01C254",
  "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter", "WorldInteractions" ],
  "sandbox": { "radius": 512, "allowEating": true, "allowSitting": true, "allowWandering": true } } ],
// ...and reference it on the npc: "packages": [ "MF_Sandbox" ]   (no condition needed)
```

**Daily routine — scheduled Sleep on top of the sandbox** (It.35, in-game confirmed). The downtime sandbox is the
*waking-hours* default; layer a **Sleep package** (template `0x019717`) above it to make her bed down
at night. Sleep is a specialized Sandbox that actively **seeks a bed** (built-in) and can lock doors.
The sleep window is the package **`schedule`** (not a data slot); the NPC's `packages` list is in
**priority order** — the engine runs the first package whose schedule + conditions match, so put the
scheduled Sleep *first* and the unconditioned sandbox *last* as the fallback for every other hour.
```jsonc
"packages": [
  { "editorId": "MF_NightSleep", "template": "Skyrim.esm:0x019717",
    "schedule": { "hour": 22, "durationInMinutes": 540 },          // 22:00–07:00
    "interruptFlags": [ "HellosToPlayer" ],
    "sleep": { "radius": 1024, "lockDoors": false } },             // lockDoors:false — shared inn, don't lock it
  { "editorId": "MF_Sandbox", "template": "Skyrim.esm:0x01C254", "sandbox": { ... } } ],
"packages": [ "MF_NightSleep", "MF_Sandbox" ]   // on the npc: Sleep FIRST (priority), sandbox fallback LAST
```
- **`lockDoors` defaults true** (an NPC locks its *own house* at night) — set **false** for a follower
  sleeping in a shared space (an inn), or she'll lock the building.
- Like vanilla `NearEditorLocation` slots, the bed search anchors on **`NearSelf`** (our generated NPCs
  have no CK Editor Location, which would silently no-op) — she finds a bed within `radius` of where
  she is, so keep her placed in a room that *has* beds and widen `radius` (~1024) to reach them.
- **More tiers** (a midday meal spot, a workbench shift) are the same pattern: add more scheduled
  packages *above* the fallback. Relocating her between zones needs a `location`/Travel `place` ref to
  a placed marker; without one each tier sandboxes/sleeps around her current spot.
- This whole routine only runs in downtime — while she's actively following, the alias package
  overrides every package in her list (Sleep included).

**Situational dialogue** — gate a *player-initiated* line on RUNTIME state, ANDed with the
follower gate, so the right line only appears in context. Uses the runtime CTDA functions:
```jsonc
// "You're hurt?" — only when she's below half health
"conditions": [
  { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x05C84E", "runOn": "Subject" },
  { "function": "GetActorValuePercent", "comparison": "<", "value": 0.5, "actorValue": "Health", "runOn": "Subject" } ]
// "Make camp?" — only after 7pm.  GetCurrentTime is no-arg (game hour 0..24); no param/ref.
"conditions": [
  { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x05C84E", "runOn": "Subject" },
  { "function": "GetCurrentTime", "comparison": ">=", "value": 19 } ]
```
Runtime condition functions available: `GetActorValuePercent` (0..1 fraction, AV arg),
`GetCurrentTime` (hour 0..24), `IsInInterior`, `IsInCombat`, `GetRandomPercent` (0..99 roll, for
line variety) — all in addition to the static gates (GetInFaction/GetItemCount/GetGlobalValue/…).
Follower-only **backstory** is the same pattern with just the `CurrentFollowerFaction==1` gate and
more response lines.

**Proactive banter** (It.34, in-game confirmed) — lines she says *unprompted*. Use the `banter` section (not `dialogue`):
all entries sharing a (speaker, quest) collapse into one ambient topic (Misc / SNAM=`IDLE`, no branch)
with Random-flagged INFOs; the engine plays a matching one on its own. **Requires idle chatter
enabled** — the Sandbox package above (or the vanilla follow package) provides it.
```jsonc
"banter": [
  { "editorId": "MF_BHurt", "questEditorId": "MF_Q", "speakerNpcEditorId": "MF_Npc",
    "responses": [ "I'm bleeding... give me a breath." ], "emotion": "Sad",
    "conditions": [
      { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x05C84E", "runOn": "Subject" },
      { "function": "GetActorValuePercent", "comparison": "<", "value": 0.4, "actorValue": "Health", "runOn": "Subject" } ] },
  { "editorId": "MF_BNight", "questEditorId": "MF_Q", "speakerNpcEditorId": "MF_Npc",
    "responses": [ "Quiet, this hour." ], "emotion": "Neutral",
    "conditions": [
      { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x05C84E", "runOn": "Subject" },
      { "function": "GetCurrentTime", "comparison": ">=", "value": 22 } ] }
]
```
Gate each on `CurrentFollowerFaction==1` (so she only banters while travelling with you) + a
situational function. NOTE: ambient/idle only — true combat shouts (Taunt/Attack subtype) aren't
supported yet. Vanilla reference probed for this: `HirelingIdles` (Skyrim.esm 0x055DEB).

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

## "Two NPCs arguing" (SCEN multi-actor conversation — STRUCTURAL ONLY, not yet in-game confirmed)

A `scene` is NPCs talking to **each other**, not the player. It's hosted by a quest whose **aliases**
are the participants; the build emits the alias-binding, the Scene record, and a Scene/`SCEN` topic per
spoken line. Place both NPCs **in the same cell, near each other**, so they're co-located to converse.

```jsonc
{ "quests": [ { "editorId": "MF_SceneQuest", "name": "...", "startGameEnabled": true } ],
  "npcs": [
    { "editorId": "MF_Borin", "name": "Borin", "greeting": "...", "race": "Skyrim.esm:0x013746",
      "voiceType": "Skyrim.esm:0x013AE6", "unique": true },
    { "editorId": "MF_Hilda", "name": "Hilda", "greeting": "...", "race": "Skyrim.esm:0x013746",
      "voiceType": "Skyrim.esm:0x013AE7", "unique": true } ],
  "scenes": [
    { "editorId": "MF_TavernArgument", "questEditorId": "MF_SceneQuest", "beginOnQuestStart": true,
      "actors": [ { "aliasId": 0, "npc": "MF_Borin" }, { "aliasId": 1, "npc": "MF_Hilda" } ],
      "phases": [                                    // played in order; one line per phase
        { "speaker": 0, "emotion": "Anger",   "lines": [ "You still owe me for the ale, Hilda." ] },
        { "speaker": 1, "emotion": "Disgust", "lines": [ "That swill wasn't worth a clipped septim." ] },
        { "speaker": 0, "emotion": "Anger",   "lines": [ "Watch your tongue, or there'll be trouble." ] },
        { "speaker": 1, "emotion": "Happy",   "lines": [ "Ha! Buy me a drink and we're even." ] } ] } ],
  "placements": [                                    // SAME cell, a few units apart
    { "base": "MF_Borin", "cell": "Skyrim.esm:0x0133C6", "position": { "x": -300, "y": 180, "z": 0 } },
    { "base": "MF_Hilda", "cell": "Skyrim.esm:0x0133C6", "position": { "x": -300, "y": 280, "z": 0 },
      "rotation": { "x": 0, "y": 0, "z": 180 } } ] }
```

How it maps to vanilla (verified with `scenediag <Skyrim.esm> <0xFORMID>` against
`dunIronbindBeemJaMourningScene` / `MQSkyHavenSparringScene`):
- the host quest gets one **QuestAlias** per `actor`, `UniqueActor`-bound to that NPC — the Scene's
  `SceneActors` reference the **alias index** (`aliasId`), never the NPC FormKey directly;
- each `phase` → one `ScenePhase` + one **Dialog `SceneAction`** (speaking alias, phase window,
  the other actor as headtrack target) + one **Scene/`SCEN` DialogTopic+INFO** holding the line;
- `beginOnQuestStart` plays the scene the moment the quest starts (i.e. on game load).

**Status / honesty:** `build`/`validate`/`dump` are clean and the record shape matches vanilla
byte-for-shape, but this has **not been confirmed in-game** (I can't run Skyrim). Likely follow-ups
before it plays reliably: the scene may need a **start trigger** beyond `beginOnQuestStart` (a quest
stage / script `Start()` call), the actor aliases may want **fill conditions** or a `GetIsAliasRef`
gate on each INFO, and the NPCs need to be **awake and reachable** (a Sandbox package keeps them
active). Probe any vanilla scene with `scenediag` to compare. See `examples/scene_spec.json`.
