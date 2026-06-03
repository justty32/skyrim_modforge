<!-- Magic patterns -->
# Recipe cookbook — magic

← [cookbook index](cookbook-index.md) | [lifelike hub](README.md)

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

## "Enchanted weapon for a custom effect" (MGEF + ENCH + WEAP + COBJ)

Three layers: a custom **MGEF** (what happens on hit) → an **enchantment** / ENCH (the reusable
"object effect", `enchantType: weapon`) → a **weapon** that references it and carries a charge pool.
Add a COBJ so the player can craft it. (For a passive **apparel** enchant, use `enchantType: apparel`
and put `enchantment` on an `armor` instead — no `enchantmentAmount`, it's always-on while worn.)

> **Armor must carry a `template` or it equips INVISIBLE** (IN-GAME CONFIRMED 2026-06-01: the
> templated cuirass shows the iron-armor mesh when worn). An ARMO's worn mesh lives on its
> Armature (ARMA addon records), not the ARMO — a spec armor with only `armorType`+`slots` renders
> nothing when worn (it does *not* crash). Set `template` to a vanilla armor of the same slot, e.g.
> `"template": "Skyrim.esm:0x00012E49"` (ArmorIronCuirass); the clone brings the Armature (worn mesh),
> the WorldModel (ground model), and the BodyTemplate. Build warns if a `template` is missing.

```jsonc
{ "magicEffects": [
    { "editorId": "MF_FrostDamageEnchEffect", "name": "Frost Damage",
      "archetype": "ValueModifier", "actorValue": "Health",
      "magicSkill": "Destruction", "resistValue": "ResistFrost",
      "castType": "FireAndForget", "targetType": "Touch", "baseCost": 1.5,
      "flags": [ "Hostile", "Detrimental", "NoArea" ] }
  ],
  "enchantments": [
    { "editorId": "MF_FrostWeaponEnch", "name": "Frost Damage",
      "enchantType": "weapon",          // → EnchantType=Enchantment, cast=FireAndForget, target=Touch
      "enchantmentCost": 15,            // per-strike charge drained from the weapon's pool
      "effects": [ { "magicEffect": "MF_FrostDamageEnchEffect", "magnitude": 10 } ] }
  ],
  "weapons": [
    { "editorId": "MF_FrostIronSword", "name": "Frostbite Iron Sword",
      "template": "Skyrim.esm:0x012EB7", "damage": 8,   // template = model (else CRASH on equip)
      "enchantment": "MF_FrostWeaponEnch", "enchantmentAmount": 1500 }   // 1500 = charge pool
  ],
  "recipes": [
    { "editorId": "MF_FrostIronSwordRecipe", "createdObject": "MF_FrostIronSword",
      "components": [ { "item": "Skyrim.esm:0x05ACE4", "count": 2 },     // IngotIron
                      { "item": "Skyrim.esm:0x02E4FC", "count": 1 } ] }  // SoulGemGrand
  ] }
```

Full file: [`examples/enchantment_spec.json`](../../examples/enchantment_spec.json). Verify with
`enchdiag <out.esp> <0xFORMID>` (ENCH type/cost/effects) and `dump` (the weapon's `enchantment ->`
link + charge). **Note — structurally verified only:** the records build, validate, link and round-trip
correctly and mirror vanilla ENCH structure exactly, but the enchantment actually *firing* in-game has
not been confirmed (no in-game test was run). The `enchantmentCost` ↔ `enchantmentAmount` tuning and
whether the engine auto-prices the charge are the most likely things to verify in-game.

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
