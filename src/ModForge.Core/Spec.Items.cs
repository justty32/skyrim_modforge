namespace ModForge;

// --- Items: gear, consumables, containers, leveled lists, recipes, and the long tail ----

public sealed class MiscSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<string> Keywords { get; set; } = new(); public string Template { get; set; } = ""; }
public sealed class BookSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Text { get; set; } = ""; public string Template { get; set; } = ""; }
public sealed class WeaponSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public ushort Damage { get; set; } public float Speed { get; set; } public float Reach { get; set; } public List<string> Keywords { get; set; } = new(); public string Template { get; set; } = ""; }
public sealed class PotionSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<EffectSpec> Effects { get; set; } = new(); public string Template { get; set; } = ""; }
public sealed class ArmorSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public float ArmorRating { get; set; } public string ArmorType { get; set; } = ""; public List<string> Slots { get; set; } = new(); public List<string> Keywords { get; set; } = new(); }
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
// is made in `count` copies at the `workbench` (a Keyword *ref*; defaults to the forge —
// Skyrim.esm:0x088105 CraftingSmithingForge) by consuming the `components`. Perk/skill gating
// (Conditions) is not yet a spec field — a recipe with components but no condition shows whenever
// you have the materials.
public sealed class RecipeSpec
{
    public string EditorId { get; set; } = "";
    public string CreatedObject { get; set; } = "";
    public int Count { get; set; } = 1;
    public string Workbench { get; set; } = "";   // bench keyword ref; empty -> forge
    public List<RecipeComponentSpec> Components { get; set; } = new();
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
