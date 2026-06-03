<!-- Part 4/4 — Advanced recipes -->
## "Spell tome for a custom spell" (MGEF → SPEL → BOOK that teaches it)

A spell tome is a BOOK whose `teaches` grants a SPEL on first read. The killer combo: author the
spell (custom MGEF + SPEL, as above) AND a tome that teaches it — all in one spec. Reading the tome
gives the player the spell.

```jsonc
{ "magicEffects": [ { "editorId": "MF_EmberLanceEffect", /* …archetype/projectile/castingArt… */ } ],
  "spells":       [ { "editorId": "MF_EmberLanceSpell", "name": "Ember Lance", /* …effects… */ } ],
  "books": [
    { "editorId": "MF_SpellTomeEmberLance", "name": "Spell Tome: Ember Lance",
      "text": "<p>Reading this grants the Ember Lance spell.</p>",
      "template": "Skyrim.esm:0x10F7F4",                 // clone SpellTomeIncinerate's MODEL (else CRASH on read)
      "value": 250, "flags": [ "CantBeTaken" ],          // vanilla spell-tome flag
      "teaches": { "kind": "spell", "spell": "MF_EmberLanceSpell" } },   // ← teaches OUR in-spec spell

    // also valid: teach a VANILLA spell by external ref…
    { "editorId": "MF_SpellTomeFirebolt", "name": "Spell Tome: Firebolt (copy)",
      "template": "Skyrim.esm:0x10F7F4",
      "teaches": { "kind": "spell", "spell": "Skyrim.esm:0x012FD0" } },

    // …or a SKILL book that raises a skill on read (no model crash if you keep a template)
    { "editorId": "MF_SkillBookDestruction", "name": "Pyromancy for Beginners",
      "template": "Skyrim.esm:0x0ED161",
      "teaches": { "kind": "skill", "skill": "Destruction" } }
  ] }
```

Gotcha: a teaching book is takeable/readable, so it STILL needs a `template` (a vanilla BOOK to clone
a `.nif` model from) — a model-less book CRASHES the reading view. The in-spec `spell` ref is wired in
build pass 2, so the tome can teach a spell defined later in the same spec. Discover a tome's model /
the `Teaches` shape with the CLI: `bookdiag <Skyrim.esm> 0x10F7F4` (a vanilla spell tome) or `0x01AFD2`
(a skill book). The actual *grant on read* is structurally wired but in-game-unconfirmed here.

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

## "Working merchant" (a shopkeeper who buys + sells — IN-GAME CONFIRMED 2026-05-31)

A vendor = a **Vendor-flagged FACT** (trade hours + a buy/sell category list + a merchant chest with
gold/stock) whose member NPC the engine treats as a shopkeeper. Make the NPC conversable and Build
auto-adds `JobMerchantFaction`, so the vanilla generic "I'd like to trade" topic surfaces — **but
only when `GetOffersServicesNow` returns 1**, which has two non-obvious requirements for a generated
NPC (the merchant must STAND on-duty in its shop, and the faction must name a sell-area CELL — see
below). With those, the barter menu opens in-game.

```jsonc
{ "factions": [
    { "editorId": "MF_ShopFaction", "name": "ModForge General Goods",
      "vendor": {
        "startHour": 8, "endHour": 20, "buysStolen": false,
        "sellBuyList": "Skyrim.esm:0x06CB48",     // VendorItemsMisc (a VendorItem-keyword FormList)
        "notSellBuyList": true,                    // NOT-sell list -> trades everything except those (general goods)
        "merchantContainer": "MF_ShopChestRef" } } ],   // -> the placed chest (below)
  "containers": [
    { "editorId": "MF_ShopChest", "name": "Merchant Chest",
      "items": [ { "item": "Skyrim.esm:0x072AE7", "count": 1 },     // VendorGoldMisc (the vendor's gold)
                 { "item": "Skyrim.esm:0x09AF0A", "count": 10 } ] } ],  // LItemMiscVendorMiscItems75 (stock)
  "npcs": [
    { "editorId": "MF_Shopkeeper", "name": "Marcurio the Merchant", "race": "Skyrim.esm:0x013746",
      "voiceType": "Skyrim.esm:0x013AE6", "unique": true,
      "factions": [ "MF_ShopFaction" ],          // membership = "this NPC is the vendor"
      "greeting": "Looking to buy?" } ],          // REQUIRED to be conversable (auto-Hello)
  "cells": [ { "editorId": "MF_Shop", "name": "Trading Post", "template": "Skyrim.esm:0x0165A8" } ],
  "placements": [
    { "base": "MF_Shopkeeper", "cell": "MF_Shop", "position": { "x": 0, "y": 128, "z": 0 }, "persistent": true },
    { "editorId": "MF_ShopChestRef", "base": "MF_ShopChest", "cell": "MF_Shop",
      "position": { "x": 0, "y": 256, "z": 0 }, "persistent": true } ] }
```
(Full worked spec: `examples/vendor_spec.json`.)

Why each piece:
- **`merchantContainer` is a PLACEMENT ref**, not the bare container — only a placed chest holds the
  gold the engine reads. Put `VendorGoldMisc` (`0x072AE7`) in it or the vendor has no money to buy with.
- **`JobMerchantFaction` is auto-added** to any NPC in an in-spec vendor faction — the vanilla
  `DialogueGeneric.OfferServicesTopic` ("I'd like to trade") is gated on `GetInFaction
  JobMerchantFaction` + `GetOffersServicesNow`. You never emit that topic; it's universal vanilla
  dialogue that appears on any conversable vendor-faction NPC during trade hours.
- **Conversable.** No `greeting`/`dialogue` ⇒ no dialogue menu ⇒ the trade prompt can't appear
  (`validate` flags this). Same load-only rule as all dialogue (new game or save+reload).
- **The merchant must STAND on-duty in its shop** — `GetOffersServicesNow` is 0 for an NPC that isn't
  settled at its shop. Two things this needs in a **new** cell: (a) a FLOOR (a `WRIntFloorSTMid01Large`
  `0x1044AA` grid + a light) so the NPC doesn't fall into the void perpetually, and (b) an on-duty
  **Sandbox** package (`0x01C254`, NearSelf) so it stands at the counter. Without a floor the merchant
  is in a falling state and never goes on-shift — the trade option silently never appears.
- **`VendorLocation` = the merchant's CELL.** Build sets the faction's `VendorLocation` to a
  `LocationCell` pointing at the merchant-container placement's cell (radius 0). This is the sell area
  `GetOffersServicesNow` tests the player against. A CK-authored merchant has an *editor location* the
  engine falls back on; a generated NPC has none, so **omitting this (or using a chest *reference* with
  radius 0, a degenerate point) breaks the trade menu**. Verified against vanilla Belethor, whose PLVD
  is `LocationCell → WhiterunBelethorsGeneralGoods`, radius 0 — not empty. (No CK-authored "sell
  package" `0x06C872` is required; VENV radius 0 is fine — Belethor's is 0 too.)

Verify structurally: `factdiag <plugin> 0x000804` and diff against vanilla Belethor `factdiag
<Skyrim.esm> 0x09CAF5` — same flags / VendorValues / buy-sell list / merchant container / `LocationCell`
VendorLocation shape. **Confirmed in-game 2026-05-31**: `coc MF_Shop`, `set GameHour to 12`, talk to
Marcurio → barter menu opens (the reachable-NPC + load-registration rules from the conversational-NPC
recipe still apply).

## "Passive perk on an NPC" (PERK — ability + entry-point)

Two perk shapes, both attachable to an NPC via `npcs[].perks` (the actor gains them at game start):

```jsonc
{ "magicEffects": [
    { "editorId": "MF_IronHideMgef", "archetype": "ValueModifier", "actorValue": "DamageResist",
      "castType": "ConstantEffect", "targetType": "Self",
      "flags": [ "Recover", "NoArea", "NoDuration", "HideInUI" ] }   // a constant-effect fortify
  ],
  "spells": [
    { "editorId": "MF_IronHideAbility", "name": "Iron Hide", "spellType": "Ability",
      "castType": "ConstantEffect", "targetType": "Self",
      "effects": [ { "magicEffect": "MF_IronHideMgef", "magnitude": 50 } ] }
  ],
  "perks": [
    // (a) ability perk — grants the constant-effect SPEL above
    { "editorId": "MF_IronHidePerk", "name": "Iron Hide", "numRanks": 1,
      "effects": [ { "kind": "ability", "spell": "MF_IronHideAbility" } ] },
    // (b) entry-point perk — +20% attack damage when wielding a sword, available at One-Handed 30
    { "editorId": "MF_DeadlyStrikesPerk", "name": "Deadly Strikes", "numRanks": 1,
      "conditions": [
        { "function": "GetBaseActorValue", "actorValue": "OneHanded",
          "comparison": "GreaterThanOrEqualTo", "value": 30 } ],
      "effects": [
        { "kind": "entryPoint", "entryPoint": "ModAttackDamage", "function": "Multiply", "value": 1.2,
          "conditions": [
            { "function": "WornHasKeyword", "param": "Skyrim.esm:0x01E711",   // WeapTypeSword
              "comparison": "EqualTo", "value": 1 } ] } ] }
  ],
  "npcs": [
    { "editorId": "MF_PerkGuard", "name": "Hardened Guard", "race": "Skyrim.esm:0x013746",
      "perks": [ "MF_IronHidePerk", "MF_DeadlyStrikesPerk" ] }
  ] }
```

- Discover entry-point names + a vanilla shape to copy: `perkdiag <Skyrim.esm> entrypoints` and
  `perkdiag <Skyrim.esm> 0x079343` (Armsman20). Verify your output with `dump` / `perkdiag <out.esp> <id>`.
- **Player perks** are NOT record-only: grant them with a Papyrus `AddPerk` call (a `scripts` quest
  fragment). The NPC path above is fully record-authorable.
- Structurally these emit exactly like vanilla perks; whether the modifier actually moves combat
  numbers in-game needs a real Skyrim launch to confirm (structural-only here). Full example:
  [`../../examples/perk_spec.json`](../../examples/perk_spec.json).

## "Multi-stage quest" (stages + journal log + objectives + dialogue-set-stage)

A quest that **progresses**: it advances through stages, writes journal text at each, shows/completes
objectives per stage, and closes itself. Pure record data (stages, log entries, `completeQuest`,
log-entry conditions) builds and `questdiag`s cleanly; the objective-display and dialogue-set-stage
*logic* is emitted as Papyrus fragment scaffolds for the CK (see the note at the end).

```jsonc
{ "quests": [ {
    "editorId": "MF_ErrandQuest", "name": "A Forged Errand",
    "startGameEnabled": true, "priority": 60,
    "stages": [
      { "index": 10, "logEntry": "Joren asked me to retrieve his lost hammer." },
      { "index": 20, "logEntry": "I agreed to help. Time to search the riverbank." },
      { "index": 30, "logEntry": "I returned the hammer. Done.", "completeQuest": true } ],
    "objectives": [
      { "index": 10, "text": "Agree to help Joren", "showStage": 10, "completeStage": 20 },
      { "index": 20, "text": "Find Joren's hammer",  "showStage": 20, "completeStage": 30 } ] } ],
  "dialogue": [
    { "editorId": "MF_AgreeToHelp", "questEditorId": "MF_ErrandQuest", "speakerNpcEditorId": "MF_Joren",
      "prompt": "I'll find your hammer.", "responses": [ "Good. It's by the mill." ],
      "setStage": 20 } ] }                                 // picking this advances the quest 10 → 20
```

Verify the records with `questdiag <plugin.esp> <questFormId>` (stages, log entries, `CompleteQuest`
flags, objectives). The full worked spec — with the NPC, Hello, and placement — is
[`examples/quest_stages_spec.json`](../../examples/quest_stages_spec.json).

**`package` handles Papyrus automatically — no CK needed (IN-GAME CONFIRMED It.36 2026-06-02).**
It generates, compiles, and VMAD-attaches everything:

- `Scripts/Source/MF_ErrandQuest_Stages.psc` — one `Fragment_Stage_XXXX_Item00000()` per stage that
  shows/completes objectives (`SetObjectiveDisplayed` / `SetObjectiveCompleted`). Engine calls this
  function by name when `SetStage()` fires.
- `Scripts/Source/TIF_MF_AgreeToHelp.psc` — `extends TopicInfo Hidden`, explicit `Quest Property
  OwningQuest Auto` (bound to the quest FormKey in VMAD), `OnBegin` calls `OwningQuest.SetStage(20)`.
  **Do not use `GetOwningQuest()` — returns None for StartGameEnabled quests** (see gotchas.md).
- Compiled `.pex` copied to `Scripts/`, VMAD attached to QUST and INFO.
- A `GetStage(quest) < 20` condition auto-added to the `setStage` line so Joren won't repeat it.

The `package` command requires `~/tools/papyrus-compiler` (Linux-native; falls back to Wine/CK).
One-time machine setup: write stub `TopicInfo.psc` + `ScriptObject.psc` to
`Data/Scripts/Source/` (see `Papyrus.cs` for details). Dialogue only registers on a game **LOAD**
— see [gotchas.md](gotchas.md).

## "Custom dragon shout" — SHOU + WOOP + word wall (IN-GAME CONFIRMED 2026-06-01)

A custom shout is `MGEF → Voice SPEL → WOOP → SHOU`, plus a way to **learn** it. Getting a shout to
actually fire in-game took four pieces beyond the bare records — each one was an invisible failure
until found in-game:

1. **Every Voice spell needs an equip slot.** A `spellType: "Voice"` SPEL with no `EquipmentType`
   has no slot → the player learns the shout but **pressing Shout does nothing**. Build now
   **auto-defaults** castable types (Spell/Voice/Power/LesserPower) to **EitherHand**
   (`Skyrim.esm:0x00013F44`) — same EQUP every vanilla shout word-spell uses. (You can override.)
2. **The MGEF needs a `projectile`** or the Thu'um fires invisibly + silently. The projectile carries
   the travelling model, the impact, and the impact sound. Match the theme — a frost shout wants a
   frost projectile (`0x02F774` FrostIcicle), a force shout the shockwave (`0x013DF4` VoicePush).
   Add `castingArt` for the cast-out flash.
3. **The `Release` sound is the EFFECT sound** (thunder/frost FX), via `magicEffects[].sounds`.
4. **The SHOU wants a `menuDisplayObject`** (`0x0A59AC`) so it previews in the shout menu.

```jsonc
{ "magicEffects": [
    { "editorId": "MF_ForgedVoiceEffect", "archetype": "Stagger",
      "castType": "FireAndForget", "targetType": "Aimed", "flags": [ "NoHitEvent" ],
      "projectile": "Skyrim.esm:0x00013DF4",               // VoicePush shockwave (model+impact+sound)
      "sounds": [ { "type": "Release", "sound": "Skyrim.esm:0x000A0F52" } ] } ],  // UnrelentingForce FX
  "spells": [   // one Voice spell per charge level — spellType MUST be "Voice"; equipType auto = EitherHand
    { "editorId": "MF_FV1", "name": "Forged Voice", "spellType": "Voice", "castType": "FireAndForget",
      "targetType": "Aimed", "effects": [ { "magicEffect": "MF_ForgedVoiceEffect", "magnitude": 1 } ] },
    { "editorId": "MF_FV2", "name": "Forged Voice", "spellType": "Voice", "castType": "FireAndForget",
      "targetType": "Aimed", "effects": [ { "magicEffect": "MF_ForgedVoiceEffect", "magnitude": 2 } ] },
    { "editorId": "MF_FV3", "name": "Forged Voice", "spellType": "Voice", "castType": "FireAndForget",
      "targetType": "Aimed", "effects": [ { "magicEffect": "MF_ForgedVoiceEffect", "magnitude": 3 } ] } ],
  "wordsOfPower": [
    { "editorId": "MF_Dov", "name": "Dov", "translation": "Dragon" },
    { "editorId": "MF_Ah",  "name": "Ah",  "translation": "Hunter" },
    { "editorId": "MF_Vul", "name": "Vul", "translation": "Forged" } ],
  "shouts": [
    { "editorId": "MF_ForgedVoice", "name": "Forged Voice", "menuDisplayObject": "Skyrim.esm:0x000A59AC",
      "words": [   // EXACTLY 3: word1 = tap, 1+2 = hold, 1+2+3 = full charge
        { "word": "MF_Dov", "spell": "MF_FV1", "recoveryTime": 12 },
        { "word": "MF_Ah",  "spell": "MF_FV2", "recoveryTime": 18 },
        { "word": "MF_Vul", "spell": "MF_FV3", "recoveryTime": 25 } ] } ],
  "wordWalls": [
    { "editorId": "MF_ForgedVoiceWall", "name": "Forged Voice Word Wall",
      "shout": "MF_ForgedVoice", "wordIndex": 1,           // teaches word 1 (MF_Dov, auto-derived)
      "scriptName": "ForgedVoiceWordWallScript",
      "cell": "Skyrim.esm:0x0371DE",                       // BleakFallsBarrow01 — see the wall caveat below
      "position": { "x": 0, "y": 0, "z": 0 } } ]
}
```

**Console test:** `help "Forged Voice" 0` → `player.addshout <SHOUT>`, then **`player.teachword <WORD>`**
for each word — `teachword` (not just `unlockword`) is what makes the glyph **show** in the shout menu.
Equip it, hold the Shout key: the bolt fires, deals its effect, with the FX sound + impact.

**In-game confirmed working:** castable shout, projectile + impact + effect sound, 3 charge levels.

**Two honest limits:**
- **No spoken-word voice.** The player yelling the dragon syllables ("FUS RO DAH") is a **recorded
  voice asset** (also a MGEF `Release` sound, but a *voiced* `.fuz`, e.g. `VOCShoutDragon01AFus`). A
  programmatic shout has none, so the word-voice is silent — only the effect FX plays. Supplying it
  needs a real voice file (see the voice-gen plan). For a 3-level *progressive* effect sound, use 3
  MGEFs (one per word-spell) each with its own A/B/C `Release` sound.
- **Word-wall learning is `OnInit`, not walk-up.** The teaching quest is start-game-enabled, so the
  shout + word 1 are granted **as soon as the plugin loads** — the placed `WordWallTrigger` is
  decorative (binding it to `OnTriggerEnter` for true walk-up learning is CK work). And the example
  cell `0x0371DE` is the **vanilla** Unrelenting Force room, so the wall you see there is vanilla, not
  ours (ours sits at origin). The blue word-glow VFX is CK/mesh + Imagespace — not emitted. A
  **vanilla** shout ref can't auto-derive its word — set `word` explicitly.

## "Custom sky" (WTHR + CLMT — atmosphere, not yet assigned)

An eerie green-tinted fog weather plus a climate that cycles it. Full worked spec:
[`../../examples/weather_spec.json`](../../examples/weather_spec.json).

```jsonc
{
  "pluginName": "ModForgeWeather.esp",
  "weathers": [{
    "editorId": "MF_EerieFog",
    "flags": ["Cloudy", "Rainy"],
    "skyUpperColor": { "day": { "r": 46, "g": 92, "b": 58 }, "night": { "r": 8, "g": 20, "b": 14 } },
    "fogNearColor":  { "day": { "r": 60, "g": 120, "b": 70 } },
    "sunlightColor": { "day": { "r": 120, "g": 170, "b": 110 } },   // sickly green light on the world
    "clouds": [{ "index": 0, "texture": "Sky\\SkyrimCloudsUpper04.dds",
                 "xSpeed": 0.012, "ySpeed": -0.006, "alphaNight": 0.8 }],
    "precipitation": "Skyrim.esm:0x10780F",                          // vanilla rain SPGD
    "windSpeed": 0.35, "windDirection": 210, "fogDayNear": 256, "fogDayFar": 9000
  }],
  "climates": [{
    "editorId": "MF_EerieClimate",
    "weathers": [ { "weather": "MF_EerieFog", "chance": 75 } ],
    "sunriseBegin": "06:00", "sunsetEnd": "20:00", "moons": ["Masser", "Secunda"]
  }]
}
```

Verify structurally: `validate` → `build` → `dump` (or `weatherdiag <esp> <0xFORMID>` /
`climatediag <esp> <0xFORMID>`). To find a precipitation SPGD, `weatherdiag` a vanilla rainy
weather (`find <Skyrim.esm> Rain Weather` → e.g. `SkyrimStormRain` `0x0C8220`, whose
`Precipitation = 0x10780F`).

**The one thing that makes it do nothing in-game:** a `WTHR`+`CLMT` is just data until something
*assigns* the climate. Vanilla does that through a **worldspace** (`WRLD` `Climate` field) or a
**region** (`REGN` weather-data) — both of which ModForge *can* now emit (next recipe). So this
recipe ships a valid, inspectable climate you'd then point a WRLD/REGN at. **Structurally verified
only; the sky actually rendering is in-game-unconfirmed.**

## "Custom exterior worldspace + weather region" (WRLD + REGN — RECORD LAYER ONLY)

Create a new exterior world, attach a climate (the sky/lighting cycle), and add a region whose
weather table drives which weathers play in an area. This is the hook for a custom Climate/Weather.

```jsonc
{ "worldspaces": [
    { "editorId": "MFTestWorld", "name": "ModForge Test Vale",
      "climate": "Skyrim.esm:0x000812",      // default climate — WITHOUT this the world has no sky cycle
      "water":   "Skyrim.esm:0x000018",      // DefaultWater (optional)
      "parent":  "Skyrim.esm:0x00003C",      // Tamriel (optional)
      "flags":   [ "SmallWorld", "CannotFastTravel" ],
      "defaultLandHeight": -27000, "defaultWaterHeight": -14000 }  // FLOOD-FIX — leave these
  ],
  "regions": [
    { "editorId": "MFTestWorldWeather", "worldspace": "MFTestWorld", "weatherPriority": 60,
      "mapColor": "0x3CA0F0", "edgeFallOff": 1024,
      "weather": [ { "weather": "Skyrim.esm:0x10E1F2", "chance": 60 },   // SkyrimClear
                   { "weather": "Skyrim.esm:0x10E1F1", "chance": 30 },   // SkyrimCloudy
                   { "weather": "Skyrim.esm:0x10E1F0", "chance": 10 } ], // SkyrimClearSN
      "area": [ { "x": -16384, "y": -16384 }, { "x": 16384, "y": -16384 },
                { "x": 16384, "y": 16384 }, { "x": -16384, "y": 16384 } ] }
  ] }
```

**Honest caveat — this is the RECORD layer, not a walkable world.** ModForge emits valid WRLD/REGN
records and wires every link, but a world you can actually *enter and walk* also needs **terrain
(LAND heightmap), LOD meshes, and navmesh** — all of which are **Creation-Kit** work ModForge does
not do. Treat this as: (a) attach a custom Climate to a world, and (b) define weather/spawn regions.
The `climate` (worldspace) and `weather` (region) refs are where a generated/chosen CLMT/WTHR plugs
in. Verified structurally (`build`/`dump`/`worlddiag`/`regndiag` round-trip) — **not in-game
confirmed**. Harvest vanilla values with `worlddiag <Skyrim.esm> 0x00003C` (Tamriel) and
`regndiag <Skyrim.esm> <0xFORMID>`. Full example: `examples/worldspace_spec.json`.

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
