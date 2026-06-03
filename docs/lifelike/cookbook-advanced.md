<!-- Advanced patterns -->
# Recipe cookbook — advanced

← [cookbook index](cookbook-index.md) | [lifelike hub](README.md)

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
