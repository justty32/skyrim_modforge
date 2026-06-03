<!-- Part 3/3 — Recipes through Workflow -->
### recipes (crafting / COBJ)
Make an item craftable, temperable, or smeltable at a workbench. A recipe's `kind` picks the
flavour (default `craft`) and the **default bench**; `workbench` is a **named selector** (`forge` /
`sharpeningWheel` (=`grindstone`) / `armorTable` (=`workbench`) / `smelter` / `tanningRack` /
`skyforge`) — or a raw `<master>:0xID` keyword ref, which overrides the kind default. Omit
`workbench` to take the kind's default.

```jsonc
{ "editorId": "MF_ForgedBladeRecipe",
  "kind": "craft",                      // craft | temper | smelt | breakdown   (default craft)
  "createdObject": "MF_ForgedBlade",    // a ref — usually an in-spec weapon/armor
  "count": 1,
  "workbench": "forge",                 // named selector OR a keyword ref; OMIT -> kind default
  "components": [                        // consumed on craft (ref + count)
    { "item": "Skyrim.esm:0x05ACE5", "count": 2 },   // SteelIngot
    { "item": "Skyrim.esm:0x0800E4", "count": 1 } ], // LeatherStrips
  "conditions": [                        // perk/item/skill gating (shared CTDA) — optional
    { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] }
```

**`kind` defaults** — `craft` → forge, `temper` → sharpening wheel, `smelt`/`breakdown` → smelter.

**`kind: "temper"`** — IMPROVE an existing weapon/armor at a grindstone (weapons) / armor table
(armor). The `createdObject` IS the item being improved (must be an in-spec weapon/armor or an
external ref); the component is the temper material. Mirror vanilla by adding the enchanted-item
guard `TemperIsEnchanted` (`or: true`) before the smithing `HasPerk`:
```jsonc
{ "editorId": "MF_ForgedBladeTemper", "kind": "temper",
  "createdObject": "MF_ForgedBlade", "workbench": "sharpeningWheel",
  "components": [ { "item": "Skyrim.esm:0x05ACE5", "count": 1 } ],
  "conditions": [
    { "function": "TemperIsEnchanted", "comparison": "!=", "value": 1, "or": true },
    { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] }
```

**`kind: "smelt"` / `"breakdown"`** — ore → ingot, or break an item down into materials at the
smelter (`createdObject` = the output ingot, component = the ore/item consumed).

**`conditions`** — each is a shared CTDA (the same `ConditionSpec` as dialogue/package gates — see the *conditions — CTDA gates* section above).
`function` ∈ `HasPerk` | `GetItemCount` | `GetGlobalValue` (each needs a `param` ref) |
`TemperIsEnchanted` (no param). `comparison` is the operator (`==` `!=` `>` `>=` `<` `<=`, default
`>=`), `value` the test value, `or: true` OR-chains with the NEXT condition. Use `find Skyrim.esm
<name> Perk` to discover perk FormIDs; `cobjdiag <esp> <0xID>` prints any recipe's full shape.

Common bench keyword FormIDs (probed from Skyrim.esm): `0x088105` forge, `0x0ADB78` armor table,
`0x088108` sharpening wheel, `0x0A5CCE` smelter, `0x07866A` tanning rack, `0x0F46CE` Skyforge.

### perks (PERK)
A perk is a passive ability or a quantitative stat/combat modifier — the building block of the skill
trees, race abilities, and quest-reward bonuses. The trunk carries `name`/`description`, the
`playable`/`hidden`/`trait` flags, `level` + `numRanks` (≥1), optional player-facing `conditions`
(perk-level CTDA gates), and a list of `effects`. Two effect kinds are supported:

```jsonc
{ "editorId": "MF_IronHidePerk", "name": "Iron Hide", "numRanks": 1,
  "effects": [
    // (a) ABILITY — grant a SPEL. Pair with an in-spec Ability/constant-effect spell + MGEF.
    { "kind": "ability", "spell": "MF_IronHideAbility" } ] }

{ "editorId": "MF_DeadlyStrikesPerk", "name": "Deadly Strikes", "numRanks": 1,
  "conditions": [   // perk-level gate (when the perk applies at all)
    { "function": "GetBaseActorValue", "actorValue": "OneHanded",
      "comparison": "GreaterThanOrEqualTo", "value": 30 } ],
  "effects": [
    // (b) ENTRY-POINT — a quantitative modifier on a named EntryPoint.
    { "kind": "entryPoint",
      "entryPoint": "ModAttackDamage",      // an EntryType name
      "function": "Multiply",               // Set | Add | Multiply
      "value": 1.2,                          // ×1.2 = +20%
      "conditions": [                        // effect-level gate (when the bonus fires)
        { "function": "WornHasKeyword", "param": "Skyrim.esm:0x01E711",  // WeapTypeSword
          "comparison": "EqualTo", "value": 1 } ] } ] }
```

- **`entryPoint`** is one of Skyrim's `EntryType` values — `ModAttackDamage`, `ModSpellMagnitude`,
  `CalculateMyCriticalHitChance`, `ModArmorRating`, `GetMaxCarryWeight`, … Discover the full set with
  `perkdiag <Skyrim.esm> entrypoints`, or dump a vanilla perk to copy a working shape:
  `perkdiag <Skyrim.esm> 0x079343` (Armsman20 = ModAttackDamage ×1.4).
- **`conditions`** (both perk-level and per-effect) use the shared CTDA builder (the same
  `ConditionSpec` as dialogue/package/recipe gates). Perk-relevant functions:
  `GetBaseActorValue`/`GetActorValue` (need `actorValue`), `HasKeyword`/`WornHasKeyword`/`HasPerk`/
  `GetIsID`/`GetIsRace`/`GetItemCount`/`IsSpellTarget` (need a `param` ref), `GetEquippedItemType`
  (`itemType` = `Left`/`Right`/`Voice`/`Instant`), `GetRandomPercent`, `GetLevel`. Each takes a
  `comparison` (`EqualTo`/`GreaterThanOrEqualTo`/… or the symbol forms) vs `value`, an optional
  `runOn` (`Subject` default / `Target`), and `or` (OR with the next condition).
- **Attach to an NPC** via `npcs[].perks: ["MF_IronHidePerk", …]` — the actor gains the perk(s)
  passively at game start (each placement carries the perk's `numRanks`). **Granting a perk to the
  PLAYER needs a Papyrus `AddPerk` call** (`scripts` + a quest fragment) — there is no record-only way
  to put a perk on the player at game start; that's a CK/script route, documented honestly here.
- **In-game caveat:** structurally these emit exactly like vanilla perks (verify with `dump` /
  `perkdiag`), but whether an entry-point modifier actually changes combat numbers, or an ability
  perk's SPEL applies, can only be confirmed by a real Skyrim launch. Worked example:
  `examples/perk_spec.json`.

### external assets — your own meshes / textures / sounds (`model`, `sounds`, `assets`)
Instead of cloning a vanilla record's mesh via `template`, bring your OWN assets. ModForge
**references** them (writes the Data-relative path into the record) and **bundles** them (copies the
files next to the `.esp` on `package`). It does NOT author meshes/sounds — full contract +
path rules in **[external_assets.md](external_assets.md)**.
```jsonc
"assets": "my_assets",          // source dir; package copies its Meshes/Textures/Sound/… into the mod
"sounds": [ { "editorId": "MFChimeSD", "files": [ "Sound\\fx\\mymod\\chime.wav" ] } ],
"statics":    [ { "editorId": "MFStone",  "model": "MyMod\\stone.nif" } ],
"furniture":  [ { "editorId": "MFThrone", "name": "Throne", "model": "MyMod\\throne.nif" } ],
"activators": [ { "editorId": "MFBell", "name": "Bell", "model": "MyMod\\bell.nif",
                  "activationSound": "MFChimeSD" } ]
```
- **`model`** (on statics/activators/furniture/miscItems/weapons) is a Data-relative `.nif` path
  rooted at `Meshes\` — so **omit the `Meshes\` prefix** (write `MyMod\bell.nif`, not
  `Meshes\MyMod\bell.nif`). `validate` enforces this. On a `miscItem`, `model` overrides `template`
  (warns); on a `weapon`, pair `model` WITH a `template` (a model-less/template-less weapon CRASHES
  on equip).
- **`sounds`** emit Sound Descriptors (SNDR). A record points at one by *ref* (in-spec `editorId` or
  vanilla `<master>:0xFORMID`): activator `activationSound`/`loopingSound`, misc/weapon
  `pickUpSound`/`putDownSound`. `category`/`outputModel` default to the vanilla SFX category/output.
- **`assets`** names a source dir laid out like `Data/` (`Meshes/`, `Textures/`, `Sound/`, `Music/`,
  `Seq/`); `package` copies those sub-trees into the output mod folder. Override per-run with
  `package <spec> <outDir> --assets <dir>`. Worked example: `../examples/custom_asset_spec.json`.

### textureSets (TXST) — retexture without a new mesh
A huge class of mods just **swaps the textures** of an existing mesh (a recolored sword, a reskinned
creature, a Markarth-painted banner reusing the Jorrvaskr banner `.nif`) without authoring a new
`.nif`. That's a **TextureSet (TXST)** record: a set of texture-map paths plus a consumer that points
a named material on a base mesh at it.

A TXST has up to eight optional slots; set only the ones you replace (an omitted slot keeps the
mesh's original map for that channel). Every path is **relative to `Data\Textures\`** — exactly like
a `model` path is relative to `Data\Meshes\` — so you **omit** the leading `Textures\`:

```jsonc
"textureSets": [
  { "editorId": "MF_GildedRubbleTexture",
    "diffuse": "ModForge\\rubble\\gilded_rubble_d.dds",   // slot 0 — color/albedo (_d)
    "normal":  "ModForge\\rubble\\gilded_rubble_n.dds",   // slot 1 — normal + gloss (_n)
    // mask(_m)/glow(_g)/height(_p)/environment(_e)/multilayer/backlight also available — all optional
    "flags": [ "NoSpecularMap" ] }                         // NoSpecularMap|FaceGenTextures|HasModelSpaceNormalMap
]
```

Wire it into a consumer with `alternateTextures` on a `statics` or `activators` record (any record
with a `model`). Each entry overrides one **named material/sub-mesh** inside the base `.nif`:

```jsonc
"statics": [
  { "editorId": "MF_GildedRubble",
    "model": "Dungeons\\Nordic\\Rubble\\NorRubblePiece03.nif",   // a VANILLA mesh, reused as-is
    "alternateTextures": [
      { "name": "NorRubblePiece03:0",        // MUST match a material/3D-name in the .nif (CK "AltTex" dialog)
        "index": 0,                           // 3D sub-mesh index (the trailing number in `name`)
        "textureSet": "MF_GildedRubbleTexture" } ] }              // ref → a TXST (in-spec or <master>:0xFORMID)
]
```

The `name`/`index` convention (`<MeshName>:<index>`) mirrors vanilla — inspect a real one with
`txstdiag` (a TXST's slots) or `dump` (a record's `altTexture` lines), e.g. vanilla STAT
`NorExtRubblePiece03_HeavySN` uses `name="NorRubblePiece03:0" index=0`. Get the material names from
the CK's *Model Data → Edit → 3D Name* list (NifSkope shows them as `BSLightingShaderProperty`
names); a wrong `name` silently swaps nothing.

**Honest limit:** ModForge writes the TXST record + the `alternateTextures` references only. The
`.dds` files themselves are **user-authored** — ModForge cannot create or render texture content, and
the headless toolchain cannot verify that a swap looks right in-game. Put your authored `.dds` files
under `Data/Textures/<your path>/` in the packaged mod folder. See `examples/texture_set_spec.json`
(with a placeholder `examples/textures/ModForge/rubble/` tree) and the cookbook recipe.

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

## Workflow

```bash
dotnet run --project src/ModForge.Cli -- validate myspec.json          # check first
dotnet run --project src/ModForge.Cli -- build    myspec.json out.esp   # just the plugin
dotnet run --project src/ModForge.Cli -- package  myspec.json OutModDir # esp + compiled scripts -> MO2 folder
```
`package` lays out `OutModDir/<pluginName>` + `Scripts/*.pex` + `Scripts/Source/*.psc`.

**NL → spec:** describe what you want to an AI agent (Claude Code); the agent emits a spec
conforming to this doc / `../examples/spec.schema.json` (per `for_agent.md`), runs `validate`
(self-correcting on problems), then `build`/`package`. This agent-driven loop **is** the
NL→spec layer — there is no in-tool LLM API (the once-planned `describe` command is dropped),
so there's no API key/provider to configure.

## Not yet covered (extend in `ModForge.Core` `Generator.Build` + a spec class)
World placement now covers new interior cells, vanilla interior cells, **and exterior/worldspace
cells** (via `worldspace` + world position), and ModForge can now **create** new worldspaces (WRLD)
+ regions (REGN) — see *worldspaces & regions* above (record layer only; terrain/LOD/navmesh stay
CK-side). Refs (in-spec or `<master>:0xFORMID`) and the `find` command are the building blocks for
the external ones. Remaining gaps are long-tail record types/fields and the CK-side terrain/LOD/
navmesh authoring — the record-side pattern is the same: add a spec class + a loop in `Build`.

See `../examples/sample_spec.json` for a complete working example.
