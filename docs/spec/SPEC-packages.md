# ModForge spec — AI packages & weather

← [index](SPEC-index.md)

### packages — AI Packages (what an NPC DOES)
A `packages` entry is an AI package. Skyrim's PACK record is **template-driven**: you reference a
vanilla "procedure template" form via `template`, and that template defines the data input schema
(slot indices + types). Our package fills in the inputs for the slots the template defines.

ModForge currently implements ten templates — **Sandbox** (`Skyrim.esm:0x01C254`), **Sleep**
(`Skyrim.esm:0x019717`), **Travel** (`Skyrim.esm:0x016FAA`), **UseMagic** (`Skyrim.esm:0x0504F5`),
**Patrol** (`Skyrim.esm:0x017723`), **Follow** (`Skyrim.esm:0x019B2C`), **Escort**
(`Skyrim.esm:0x023B73`), **SitTarget** (`Skyrim.esm:0x0A9277`), **Activate** (`Skyrim.esm:0x019B2D`)
and **Eat** (`Skyrim.esm:0x019714`). Author the matching subobject
(`sandbox` / `sleep` / `travel` / `useMagic` / `patrol` / `follow` / `escort` / `sitTarget` /
`activate` / `eat`) and the build will fill that template's Data slots. To target a
template ModForge doesn't yet handle (UseWeapon / …), still set `template`; the package emits
structurally valid but with no Data overrides (template defaults apply) and a warning. Use
`packagediag <Skyrim.esm> <0xFORMID>` to discover any template's named slot schema before adding support.

**What a ref slot may name.** Every target/location slot (the 12 rows of the table below) is filled
*after* placements and `references[]` exist, so any of these works in any of them:
a vanilla `<master>:0xFORMID` placed ref · an **in-spec `placements[]` editorId** · a
**`references[]` label** · an `alias:` / `aliasLoc:` quest alias (next paragraph). A ref that resolves
to none of those warns and falls back (location → `NearSelf`; `useMagic.target` → `PackageTargetSelf`;
other SingleRef slots → the package no-ops).

**Radiant alias targets (`alias:` / `aliasLoc:`).** Any target/location slot ref (`sandbox`/`sleep`/
`eat` `location`, `travel` `place`, `escort` `destination`/`target`, `follow` `target`,
`sitTarget`/`activate` `target`, `useMagic` `location`/`target`, `patrol` `start`) may name a quest
**alias** instead of a placed reference — the core of a radiant **performance package** that acts on
whatever an alias was filled with at runtime:
- **`"alias:<name>"`** → the ref/actor the ownerQuest's alias `<name>` holds. On a *target* slot →
  `PackageTargetAlias`; on a *location* slot → `LocationFallback(AliasForReference)`.
- **`"aliasLoc:<name>"`** → the LOCATION a location-alias holds → `LocationFallback(AliasForLocation)`.
  (Location slots only — not valid as a target.)

The package's **`ownerQuest` must be an in-spec quest** so its alias indices can be resolved (validate
errors otherwise). E.g. a quest fills `Victim`/`Dungeon` aliases (`findMatchingLocation`,
`findInLocationAlias`, `forced`, …) and its packages do `travel.place: "aliasLoc:Dungeon"` +
`escort.target: "alias:Victim"`. Demo `examples/radiant_package_spec.json`. ⚠ **Offline limit:** the
Mutagen field *shape* is reflection-verified, but the `AliasForReference`/`AliasForLocation` choice and
`PackageTargetAlias` byte layout need a main-machine xEdit byte-compare vs a real radiant package — see
`WAIT_USER.md`.

**Sandbox at a specific ref vs Travel:** Sandbox's `location` ref makes the NPC wander/eat/sit
**around** that ref (radius covers nearby furniture). Travel's `place` ref makes the NPC actually
**walk to** that ref and stop within `radius` of it. Common chain: a Travel package + a Sandbox
package on the same NPC's `packages` list (Travel first) — Travel runs until the NPC arrives,
then Sandbox takes over.

> 🔴 **A location slot anchors an AREA, not an OBJECT.** Which slot you put a ref in decides whether
> the engine locks onto **that one object**. The complete split (source of truth in code:
> `src/ModForge.Core/PackageRefSlots.cs`, kept honest by an anti-rot test):
>
> | slot kind | slots | builds | meaning |
> |---|---|---|---|
> | **SingleRef target** | `patrol.start`, `follow.target`, `escort.target`, `sitTarget.target`, `activate.target`, `useMagic.target` | `PackageTargetSpecificReference(FormKey)` | **that ref and no other** |
> | **location** | `sandbox.location`, `sleep.location`, `travel.place`, `escort.destination`, `eat.location`, `useMagic.location` | `LocationTarget(FormKey)` + radius | an **AREA** at that ref's position; the engine then picks whatever furniture/bed/food it likes **inside the radius** |
>
> So `sandbox.location: "<a chair>"` does **not** mean "sit in that chair" — the NPC may sit in a
> *different* chair nearby, **with no warning and no error** (the plugin builds clean and dumps
> clean; only the in-game behaviour is wrong). To pin an NPC to **one specific reference**, use a
> SingleRef target slot (no quest alias needed). Worked example: `examples/referrer-chair-anchor.json`.
>
> **Guardrail:** when a `references[]` **label** (which declares "I care about *this object*") lands in
> a *location* slot, `build` prints an **info** line (`  i …`, never a warning — "wander near that
> chair" is a legal intent) naming the slot, the radius and the SingleRef slots that would lock on.
> A plain vanilla FormID or an in-spec placement editorId in a location slot is the ordinary area
> case and says nothing.
>
> **`area:` opt-out.** If the area behaviour *is* what you want, prefix the location ref with `area:`
> — `"sandbox.location": "area:sofia's chair"` — to declare the intent explicitly and silence the
> guardrail note. The prefix is stripped before the ref resolves, so it binds the exact same
> `LocationTarget` + radius as the bare ref would. It is only meaningful on the six **location** slots;
> on a SingleRef target slot `area:` is not understood and the ref fails to resolve.

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
    "location":        "",                       // optional ref (where to stand); empty -> NearSelf
    "radius":          256,                      // location radius (template default 500)
    "target":          "",                       // optional ref (who to cast on); empty -> PackageTargetSelf
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

**SitTarget template (`Skyrim.esm:0x0A9277`) — `sitTarget` subobject:**
```jsonc
{ "editorId": "MF_BorinSit",
  "template": "Skyrim.esm:0x0A9277",       // SitTarget ("go use that furniture")
  "preferredSpeed": "Walk",
  "sitTarget": {
    "target":       "InnChair",            // REQUIRED — ref to a placed FURNITURE reference
                                           //   (vanilla REFR or an in-spec placement editorId)
    "waitTime":     0,                     // seconds to stay seated (0 = until the package/phase ends)
    "stopMovement": false } }
```
SitTarget is the "walk to and sit/use a piece of furniture" routine (decoded from vanilla
`MQ306EsbernSit`). It fills 3 author slots: **16** `Target` (SingleRef → the furniture ref, REQUIRED),
**3** `Wait Time` (float), **4** `Stop Movement Flag` (bool). The engine paths the NPC to the furniture
**and** seats him, so **one SitTarget action covers both the walk and the sit** (no separate Travel
needed). Same navmesh rule as Travel: the furniture must be a placed ref reachable on the NPC's navmesh
(keep it in the same interior cell). The furniture ref is forced **persistent** automatically (it's a
package SingleRef target). Without `target` the package no-ops. Primary use: a **scene performance beat**
— a scene Package action references a SitTarget package so an actor takes a seat mid-conversation
(see `examples/scene-sit-performance.json`).

**Activate template (`Skyrim.esm:0x019B2D`) — `activate` subobject:**
```jsonc
{ "editorId": "MF_PullLever",
  "template": "Skyrim.esm:0x019B2D",       // Activate
  "preferredSpeed": "Walk",
  "activate": {
    "target":           "MF_Lever",        // REQUIRED — ref to the object to activate (placement editorId or vanilla ref)
    "numberToActivate": 1 } }              // default 1
```
The NPC walks to and ACTIVATES `target` (a lever/door/activator — triggers its OnActivate). Slot 0
is the SingleRef target (deferred-wired like Patrol/Follow slot 0, so it may be an in-spec placement;
forced **persistent** automatically), slot 2 is "Number to Activate". Decoded from vanilla
`dunHillgrundsUnlockExteriorDoorActivate`. Without `target` the package no-ops. Pairs well as a scene
Package-action beat (an actor pulls a chain / opens a door mid-scene). Same navmesh reachability rule
as Travel/SitTarget.

**Eat template (`Skyrim.esm:0x019714`) — `eat` subobject:**
```jsonc
{ "editorId": "MF_TavernMeal",
  "template": "Skyrim.esm:0x019714",       // Eat
  "schedule": { "hour": 19, "durationInMinutes": 60 },
  "eat": {
    "location":          "",               // optional ref (where to eat); empty -> NearSelf
    "radius":            500,
    "allowSitting":      true,
    "allowWandering":    true,
    "numFoodItems":      1,
    "energy":            0,
    "minWanderDistance": 300 } }
```
Eat is a LOCATION-based Sandbox variant: the NPC goes to `location`, finds food + a chair (a fixed
engine search the builder emits — slot 1 Food Criteria, 4 Found Food, 5 Chair Target, 6 Found Chair),
sits and eats. Modelled on the Sleep template's slot-filling. Gate the meal window with `schedule`.
"Go to the tavern and have a meal." (Note: this is an ambient routine — for a precise scene "sit on
THIS chair" beat use **SitTarget** instead.)

**Flags (Package.Flag):** `OffersServices`, `MustComplete`, `MaintainSpeedAtGoal`, `ContinueIfPcNear`,
`OncePerDay`, `PreferredSpeed`, `AlwaysSneak`, `AllowSwimming`, `IgnoreCombat`, `WeaponsUnequipped`,
`WeaponDrawn`, `NoCombatAlert`, `WearSleepOutfit`.

**Interrupt flags (Package.InterruptFlag):** `HellosToPlayer`, `RandomConversations`,
`ObserveCombatBehavior`, `GreetCorpseBehavior`, `ReactionToPlayerActions`, `FriendlyFireComments`,
`AggroRadiusBehavior`, `AllowIdleChatter`, `WorldInteractions`. **These are the difference between
a silent statue and a lifelike NPC.** Vanilla DefaultSandbox enables all of them.

### npcPatches — override an EXISTING NPC's AI schedule

Re-stage a **vanilla (or other-master) NPC** by overriding its record and swapping its AI
package list — the core move behind mods like *AI Overhaul* (send a townsperson to the tavern
at night, give a guard a patrol, etc.). You don't recreate the NPC; you carry their whole
record forward and change only the packages.

```jsonc
"packages": [ { "editorId": "MFCarlottaStayHome", "template": "Skyrim.esm:0x01C254" } ],
"npcPatches": [
  { "overrideOf": "Skyrim.esm:0x013B99",   // the existing NPC ref (Carlotta Valentia)
    "packages": [ "MFCarlottaStayHome" ],   // PACK refs: in-spec editorId or vanilla <master>:0xFORMID
    "mode": "replace" }                      // replace | prepend | append  (default replace)
]
```

- **`overrideOf`** — the existing NPC as `<master>:0xFORMID`. Resolving it needs the master on
  disk (`MODFORGE_SKYRIM_DATA`, or the default Steam Data path); if it can't be resolved the
  patch is skipped with a warning (the build still completes).
- **`mode`** — `replace` uses ONLY your packages; `prepend`/`append` keep the NPC's existing
  packages and add yours before/after. **Package order matters** — specific time/place packages
  must sit above the broad sandbox fallback, so for a "go somewhere at a time" overlay use
  `prepend`.
- **The override is a full record override** (a Skyrim override REPLACES the master record), so
  the build deep-copies the NPC — name, stats, factions, outfit, voice all carried forward — and
  only the package list changes. The NPC's **real English name** is preserved inline: ModForge
  extracts the vanilla English `.STRINGS` (from `Skyrim - Interface.bsa`) so the localized name
  resolves headless. This matches modern practice — players run the **English** game and layer a
  **translation mod** on top, so shipping the English name inline is correct (a Chinese-translation
  mod loaded after will re-override it).
- **Inspect** the result with `npcdiag <esp> <0xFORMID>` (same FormID as `overrideOf`); the
  `Packages` line shows your new list. See `examples/npc_patch.json`.
- ⚠️ **Load order** — an `npcPatches` override conflicts with any other mod overriding the same
  NPC (USSEP, AI Overhaul itself). Last-loaded wins; sort accordingly.

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
