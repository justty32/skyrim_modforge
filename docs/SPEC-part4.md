<!-- Part 4/5 — Recipes through Texture Sets -->
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
