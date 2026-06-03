# ModForge spec — AI packages & weather

← [index](SPEC-index.md)

### packages — AI Packages (what an NPC DOES)
A `packages` entry is an AI package. Skyrim's PACK record is **template-driven**: you reference a
vanilla "procedure template" form via `template`, and that template defines the data input schema
(slot indices + types). Our package fills in the inputs for the slots the template defines.

ModForge currently implements seven templates — **Sandbox** (`Skyrim.esm:0x01C254`), **Sleep**
(`Skyrim.esm:0x019717`), **Travel** (`Skyrim.esm:0x016FAA`), **UseMagic** (`Skyrim.esm:0x0504F5`),
**Patrol** (`Skyrim.esm:0x017723`), **Follow** (`Skyrim.esm:0x019B2C`), and **Escort**
(`Skyrim.esm:0x023B73`). Author the matching subobject (`sandbox` / `sleep` / `travel` / `useMagic`
/ `patrol` / `follow` / `escort`) and the build will fill that template's Data slots. To target a
template ModForge doesn't yet handle (UseWeapon / …), still set `template`; the package emits
structurally valid but with no Data overrides (template defaults apply) and a warning. Use
`packagediag <Skyrim.esm> <0xFORMID>` to discover any template's named slot schema before adding support.

**Sandbox at a specific ref vs Travel:** Sandbox's `location` ref makes the NPC wander/eat/sit
**around** that ref (radius covers nearby furniture). Travel's `place` ref makes the NPC actually
**walk to** that ref and stop within `radius` of it. Common chain: a Travel package + a Sandbox
package on the same NPC's `packages` list (Travel first) — Travel runs until the NPC arrives,
then Sandbox takes over.

```jsonc
{ "editorId": "MF_HangAtSpotPackage",
  "template": "Skyrim.esm:0x01C254",        // Sandbox procedure template (find by EditorID "Sandbox")
  "preferredSpeed": "Walk",
  "interruptFlags": [                        // the lifelike-NPC switches — leave most ON
    "HellosToPlayer", "RandomConversations", "ObserveCombatBehavior",
    "GreetCorpseBehavior", "ReactionToPlayerActions", "FriendlyFireComments",
    "AggroRadiusBehavior", "AllowIdleChatter", "WorldInteractions" ],
  "schedule": { "hour": -1, "minute": -1, "durationInMinutes": 0, "dayOfWeek": "Any" },
  "sandbox": {
    "radius": 1024,                          // wander distance from the anchor
    "location": "",                           // empty -> LocationFallback (NPC's editor location);
                                              // a ref -> LocationTarget anchored at that placed ref
    "allowEating": true,  "allowSleeping": false,  "allowConversation": true,
    "allowIdleMarkers": true, "allowSitting": true, "allowWandering": true,
    "allowSpecialFurniture": true, "energy": 50.0 } }
```
Then attach to an NPC: `"npcs": [{ ..., "packages": [ "MF_HangAtSpotPackage" ] }]`.

**Why these inputs:** the Sandbox template names them (see `packagediag <Skyrim.esm> 0x01C254`).
`location: ""` is the safest default — the engine anchors the sandbox at wherever the NPC was placed.
A specific `location` ref (an REFR/ACHR FormID) anchors the sandbox at that reference's position.
`Allow Sleeping = false` keeps the NPC active 24/7 (good for visible-in-game testing); leave it true
for a normal day/night cycle. `Energy = 50` is the vanilla default (higher = more wandering).

**Travel template (`Skyrim.esm:0x016FAA`) — `travel` subobject:**
```jsonc
{ "editorId": "MF_GoToWhiterun",
  "template": "Skyrim.esm:0x016FAA",       // Travel
  "preferredSpeed": "Walk",
  "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter" ],
  "travel": {
    "place": "Skyrim.esm:0x0567F7",        // a ref to a placed REFR/ACHR (the destination)
    "radius": 256,                          // arrive within this many units (0 = exact point)
    "rideHorse": false,                     // template default
    "preferPath": false } }                 // template default
```
Travel has just 3 slots: `Place to Travel` / `Ride Horse if possible?` / `Prefer Preferred Path?`.
**Without a `place` ref the NPC won't actually travel** — the engine falls back to NearSelf
(degenerate: travel to where you already are) and the package no-ops. Chain a Sandbox package after
it (lower priority in the NPC's `packages` list) so the NPC has something to do on arrival.

**UseMagic template (`Skyrim.esm:0x0504F5`) — `useMagic` subobject:**
```jsonc
{ "editorId": "MF_AltarRitual",
  "template": "Skyrim.esm:0x0504F5",       // UseMagic
  "preferredSpeed": "Walk",
  "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter" ],
  // For CONTINUOUS casting BOTH knobs are required (see "It.18 gotchas" below):
  "schedule": { "hour": -1, "minute": -1, "durationInMinutes": 1440, "dayOfWeek": "Any" },
  "useMagic": {
    "spell":           "Skyrim.esm:0x043324",   // REQUIRED — FormLink to a SPEL record (Candlelight)
    "location":        "",                       // optional placed-ref (where to stand); empty -> NearSelf
    "radius":          256,                      // location radius (template default 500)
    "target":          "",                       // optional placed-ref (who to cast on); empty -> PackageTargetSelf
    "holdWhenBlocked": true,
    "castTimeMin":     1.5, "castTimeMax":     2.5,
    "cooldownTimeMin": 8.0, "cooldownTimeMax": 12.0,
    "numToCastMin":    1, "numToCastMax":    1000,
    "dualCast":        false } }
```
UseMagic has 11 active slots (2-12). The **"Spell" slot is a `PackageTargetObjectID` FormLink to
a specific SPEL record** — NOT a category enum. (`Spell` implements `IObjectId`.) Build writes
slot 4 (Target) as `PackageTargetSelf` when `target` is empty, matching vanilla self-cast packages
like `WCollegePracticeCastWard`; set `target` to a placed-ref for cast-at-X (vanilla
`WCollegeOnmundPracticeFlames12x4` points at a target dummy).

**It.18 gotchas (learned the hard way — 3 in-game rounds):**
1. **Slot 3 (Spell) must be `PackageTargetObjectID`, not `PackageTargetObjectType`.** The template
   default shows `PackageTargetObjectType` (a category enum), but all 46 vanilla UseMagic packages
   override it with `PackageTargetObjectID` (FormLink). The enum form builds, dumps fine, no-ops in-game.
2. **Slot 4 (Target) must be set** — `PackageTargetSelf` for self-cast, otherwise
   `PackageTargetSpecificReference`. Leaving it as the template's `PackageTargetLinkedReference`
   fallback also no-ops in practice.
3. **`numToCastMax` is total package-lifetime casts**, NOT per-cycle. With `schedule.durationInMinutes=0`
   (the default) the package completes the moment its quota's hit. For continuous casting use BOTH
   a high upper bound (1000 like vanilla Onmund) AND a non-zero `schedule.durationInMinutes`
   (e.g. 1440 = 24h).
4. **Combat preempts UseMagic.** Vanilla — for an idle ritual caster this is correct (NPC switches
   to attacking instead of standing & casting Candlelight). To force casting to continue (e.g. a
   boss ritual), add `flags: [ "IgnoreCombat" ]` like vanilla `SprigganCallOverride`.
5. **Use `pkgsbytemplate <plugin> <0xFORMID>`** to scan a master for all packages using a given
   template. Necessary because `find` matches EditorIDs only, and many template-based packages
   (e.g. `WhiterunTempleCastHealingSpellSoldier`) don't carry the template name in their EditorID.

**Flags (Package.Flag):** `OffersServices`, `MustComplete`, `MaintainSpeedAtGoal`, `ContinueIfPcNear`,
`OncePerDay`, `PreferredSpeed`, `AlwaysSneak`, `AllowSwimming`, `IgnoreCombat`, `WeaponsUnequipped`,
`WeaponDrawn`, `NoCombatAlert`, `WearSleepOutfit`.

**Interrupt flags (Package.InterruptFlag):** `HellosToPlayer`, `RandomConversations`,
`ObserveCombatBehavior`, `GreetCorpseBehavior`, `ReactionToPlayerActions`, `FriendlyFireComments`,
`AggroRadiusBehavior`, `AllowIdleChatter`, `WorldInteractions`. **These are the difference between
a silent statue and a lifelike NPC.** Vanilla DefaultSandbox enables all of them.

### weathers & climates — custom skies (WTHR) + weather cycles (CLMT)

A **weather** (`WTHR`) is one *sky*: cloud layers, per-time-of-day colours for the
sky/fog/clouds/sun, precipitation, wind, fog distances. A **climate** (`CLMT`) is a
*cycle*: which weathers occur (each with a relative `chance` weight) plus sunrise/sunset
timing and the sun/moon textures. A climate references weathers; together they give a
worldspace or region its atmosphere.

```jsonc
"weathers": [{
  "editorId": "MF_EerieFog",
  "flags": ["Cloudy", "Rainy"],          // default ["Pleasant"]
  "skyUpperColor": {                      // each colour: sunrise/day/sunset/night, RGB 0–255
    "day":   { "r": 46, "g": 92, "b": 58 },
    "night": { "r": 8,  "g": 20, "b": 14 }   // omitted times-of-day fall back to `day`
  },
  "fogNearColor": { "day": { "r": 60, "g": 120, "b": 70 } },
  "sunlightColor": { "day": { "r": 120, "g": 170, "b": 110 } },  // directional light on the world
  "clouds": [{ "index": 0, "texture": "Sky\\SkyrimCloudsUpper04.dds",
               "xSpeed": 0.012, "ySpeed": -0.006, "alphaDay": 1.0, "alphaNight": 0.8 }],
  "precipitation": "Skyrim.esm:0x10780F",  // a rain SPGD (find one via weatherdiag on a vanilla rainy WTHR)
  "windSpeed": 0.35, "windDirection": 210,  // speed 0–1 (or 0–100); direction in degrees
  "fogDayNear": 256, "fogDayFar": 9000
}],
"climates": [{
  "editorId": "MF_EerieClimate",
  "weathers": [ { "weather": "MF_EerieFog", "chance": 75 },
                { "weather": "MF_PlainClear", "chance": 25 } ],   // chances are relative weights
  "sunriseBegin": "06:00", "sunriseEnd": "09:30",
  "sunsetBegin": "17:00",  "sunsetEnd": "20:00",
  "moons": ["Masser", "Secunda"], "volatility": 40
}]
```

- **Minimal is valid.** A weather with just an `editorId` is a vanilla-sane clear-day sky;
  a climate needs only an `editorId` + at least one `weather`. Everything else defaults.
- **Colours** are 8-bit RGB (0–255). Any omitted time-of-day is seeded from `day`, so a
  partial colour is still valid. Validate flags out-of-range components.
- **Wind direction** is authored in **degrees** (0–360); it's stored on disk as a fraction
  of a full circle. **Wind speed** accepts a 0–1 fraction or a 0–100 percentage.
- **`precipitation`** is a *ref* to a shader-particle-geometry (`SPGD`). Discover a vanilla
  rain one with `weatherdiag <Skyrim.esm> <a-rainy-WTHR-formid>` (e.g. `SkyrimStormRain`
  → `Skyrim.esm:0x10780F`). The `Rainy`/`Snow` flags drive the engine's precip systems.
- **Inspect** a generated or vanilla record with `weatherdiag <esp> <0xFORMID>` /
  `climatediag <esp> <0xFORMID>`, or `dump` (which prints both).

> **Assigning the climate is a separate step.** Emitting a `WTHR`+`CLMT` does **not** by
> itself change any in-game sky. A vanilla game applies a climate via a **worldspace**
> (`WRLD` `Climate` field) or a **region** (`REGN` weather-data) record — neither is built
> here (worldspace/region authoring is out of scope). The records this produces are valid
> targets to point such a record at; doing so by hand (or via a future WRLD/REGN feature)
> is the hook. **IN-GAME CONFIRMED (It.36, 2026-06-02):** force weather via console `sw <XX>000800`
> where `XX` = plugin's load order slot in hex (see MO2 right panel). The `build` command prints
> the `sw` commands for all WTHR records after a successful build.
