namespace ModForge;

// --- Items: gear, consumables, containers, leveled lists, recipes, and the long tail ----

public sealed class MiscSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<string> Keywords { get; set; } = new(); public string Template { get; set; } = ""; }
// Book (BOOK): plain readable book, OR a *teaching* book. A spell tome (`teaches.kind="spell"`) grants
// a Spell when first read; a skill book (`teaches.kind="skill"`) raises an ActorValue/skill on first
// read. `value`/`weight` override the (cloned-template) stats; `flags` are Book.Flag names (e.g.
// CantBeTaken). A takeable book STILL needs a `template` (a vanilla book to clone its model from) or
// it CRASHES on read — clone a matching tome (e.g. Skyrim.esm:0x10F7F4 SpellTomeIncinerate's model).
public sealed class BookSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Text { get; set; } = "";
    public string Template { get; set; } = "";
    public uint Value { get; set; }                 // 0 = keep template's value
    public float Weight { get; set; }                // 0 = keep template's weight
    public List<string> Flags { get; set; } = new(); // Book.Flag names (e.g. CantBeTaken)
    public BookTeachesSpec? Teaches { get; set; }    // null/kind="" => teaches nothing (plain book)
}
// What a book teaches on first read. kind="spell" => grant `spell` (an in-spec or vanilla SPEL ref);
// kind="skill" => raise `skill` (an ActorValue name, e.g. Destruction, OneHanded, Smithing). Anything
// else (or null) => teaches nothing.
public sealed class BookTeachesSpec
{
    public string Kind { get; set; } = "";   // "spell" | "skill" | "" (nothing)
    public string Spell { get; set; } = "";  // ref → SPEL (when kind="spell")
    public string Skill { get; set; } = "";  // Skill name (when kind="skill"), e.g. Destruction, OneHanded, Smithing
}
// Weapon `enchantment` is a ref → an in-spec ENCH (enchantments[]) or a vanilla ObjectEffect
// (e.g. Skyrim.esm:0x10FB96 EnchWeaponFrostDamageBase). `enchantmentAmount` is the weapon's CHARGE
// POOL (how many casts before it must be recharged with a soul gem) — vanilla enchanted weapons use
// 1500–3000; 0 leaves the engine's auto-calc. Only meaningful for weapon/staff (Enchantment/
// StaffEnchantment) enchants; ignored (no charge) for constant-effect apparel.
public sealed class WeaponSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public ushort Damage { get; set; } public float Speed { get; set; } public float Reach { get; set; } public List<string> Keywords { get; set; } = new(); public string Template { get; set; } = ""; public string Enchantment { get; set; } = ""; public ushort EnchantmentAmount { get; set; } }
public sealed class PotionSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<EffectSpec> Effects { get; set; } = new(); public string Template { get; set; } = ""; }
// Armor `enchantment` is a ref → an in-spec ENCH (enchantments[], normally an `apparel` constant-
// effect one) or a vanilla ObjectEffect. Apparel enchants are passive/always-on while worn, so
// there's no charge pool (no enchantmentAmount).
public sealed class ArmorSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public float ArmorRating { get; set; } public string ArmorType { get; set; } = ""; public List<string> Slots { get; set; } = new(); public List<string> Keywords { get; set; } = new(); public string Enchantment { get; set; } = ""; }
public sealed class MessageSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Description { get; set; } = ""; }
// One entry in a leveled list: a ref (item or npc) that appears at >= Level, Count copies.
public sealed class LeveledEntrySpec { public string Reference { get; set; } = ""; public short Level { get; set; } = 1; public short Count { get; set; } = 1; }
// LeveledItem (LVLI) / LeveledNpc (LVLN): chanceNone (0-100), flag names, weighted entries.
public sealed class LeveledItemSpec { public string EditorId { get; set; } = ""; public int ChanceNone { get; set; } public List<string> Flags { get; set; } = new(); public List<LeveledEntrySpec> Entries { get; set; } = new(); }
public sealed class LeveledNpcSpec { public string EditorId { get; set; } = ""; public int ChanceNone { get; set; } public List<string> Flags { get; set; } = new(); public List<LeveledEntrySpec> Entries { get; set; } = new(); }
// Container (CONT): named, with a list of item refs + counts.
public sealed class ContainerEntrySpec { public string Item { get; set; } = ""; public int Count { get; set; } = 1; }
public sealed class ContainerSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public float Weight { get; set; } public List<ContainerEntrySpec> Items { get; set; } = new(); }
// One required ingredient in a recipe: a *ref* (in-spec or vanilla) + how many are consumed.
public sealed class RecipeComponentSpec { public string Item { get; set; } = ""; public int Count { get; set; } = 1; }
// ConstructibleObject (COBJ): a crafting recipe. `createdObject` (a *ref*, usually an in-spec item)
// is made in `count` copies at the `workbench` by consuming the `components`, optionally gated by
// `conditions` (the SHARED CTDA ConditionSpec — perk/item/skill, e.g. function "HasPerk", param a
// smithing-perk ref; or "TemperIsEnchanted" + or:true for the vanilla temper guard).
//
// `kind` picks the recipe flavour (default "craft"):
//   craft     — make `createdObject` from components at a bench (default forge).
//   temper    — IMPROVE an existing weapon/armor: createdObject = the item itself, default bench is
//               the sharpening wheel (weapons) / armor table; component = the temper material.
//   smelt     — ore -> ingot (default bench: smelter), or break an item down into materials.
//   breakdown — alias of smelt (break an item into components at the smelter).
//
// `workbench` is a NAMED selector — forge | sharpeningWheel | armorTable | smelter | tanningRack |
// skyforge — resolved to the right vanilla CraftingSmithing* keyword. A raw <master>:0xID or in-spec
// keyword ref still works (overrides the kind default). Empty -> the kind's default bench.
public sealed class RecipeSpec
{
    public string EditorId { get; set; } = "";
    public string Kind { get; set; } = "craft";      // craft | temper | smelt | breakdown
    public string CreatedObject { get; set; } = "";
    public int Count { get; set; } = 1;
    public string Workbench { get; set; } = "";       // named selector OR ref; empty -> kind default
    public List<RecipeComponentSpec> Components { get; set; } = new();
    public List<ConditionSpec> Conditions { get; set; } = new();   // shared CTDA gates (HasPerk/…)
}

// --- Long-tail record types (same spec-class + build-loop pattern) ---------------------
// Ingredient (INGR): an alchemy reagent — value/weight + `effects` (reuses the spell/potion
// effect pipeline) + keywords.
public sealed class IngredientSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<EffectSpec> Effects { get; set; } = new(); public List<string> Keywords { get; set; } = new(); }
// Ammunition (AMMO): arrow/bolt — value/weight + `damage` (float) + keywords.
public sealed class AmmunitionSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public float Damage { get; set; } public List<string> Keywords { get; set; } = new(); }
// Scroll (SCRL): a one-shot spell-as-item — value/weight + `effects` + spell cast fields.
public sealed class ScrollSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<EffectSpec> Effects { get; set; } = new(); public string SpellType { get; set; } = ""; public string CastType { get; set; } = ""; public string TargetType { get; set; } = ""; public uint BaseCost { get; set; } public List<string> Keywords { get; set; } = new(); }
// SoulGem (SLGM): value/weight + `maximumCapacity` (None|Petty|Lesser|Common|Greater|Grand) + keywords.
public sealed class SoulGemSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public string MaximumCapacity { get; set; } = ""; public List<string> Keywords { get; set; } = new(); }
// Key (KEYM): value/weight + keywords.
public sealed class KeySpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<string> Keywords { get; set; } = new(); }
// Keyword (KYWD): just an editorId — define your own so in-spec records can reference it in
// their `keywords` lists (e.g. a custom "VendorItemFood" category).
public sealed class KeywordSpec { public string EditorId { get; set; } = ""; }
// Outfit (OTFT): a named set of item *refs* (armors/weapons) an NPC can wear; an npc `outfit`
// ref can point at an in-spec outfit's editorId.
public sealed class OutfitSpec { public string EditorId { get; set; } = ""; public List<string> Items { get; set; } = new(); }
// Static (STAT): a world mesh — just `model` (a .nif path; reference a vanilla mesh in the BSA).
// A placement base for scenery; no Name (statics are nameless).
public sealed class StaticSpec { public string EditorId { get; set; } = ""; public string Model { get; set; } = ""; }
// Activator (ACTI): an interactable world object — name + `model` + keywords (+ a script via
// `scripts`). A placement base you can walk up to / attach behaviour to.
public sealed class ActivatorSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Model { get; set; } = ""; public List<string> Keywords { get; set; } = new(); }
