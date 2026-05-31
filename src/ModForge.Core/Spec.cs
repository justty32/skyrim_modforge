namespace ModForge;

// --- ESP generator spec (the structured IR; deserialized case-insensitively) ---------
// The root document. Each `List<XSpec>` is a record family the build emits; the per-family
// spec DTOs live in the Spec.*.cs partials (Spec.Items / Spec.Magic / Spec.Actors /
// Spec.Dialogue / Spec.Packages / Spec.World).
//
// "ref" fields throughout accept EITHER an in-spec editorId OR an external "<master>:0xFORMID"
// (e.g. "Skyrim.esm:0x013746" — find them with the `find` command). External refs auto-add
// the master on write (Mutagen MastersListContent=Iterate).
public sealed class ModSpec
{
    public string PluginName { get; set; } = "Generated.esp";
    public bool Esl { get; set; } = true;
    public List<MiscSpec> MiscItems { get; set; } = new();
    public List<BookSpec> Books { get; set; } = new();
    public List<WeaponSpec> Weapons { get; set; } = new();
    public List<NpcSpec> Npcs { get; set; } = new();
    public List<QuestSpec> Quests { get; set; } = new();
    public List<DialogueSpec> Dialogue { get; set; } = new();
    public List<BanterSpec> Banter { get; set; } = new();
    public List<SceneSpec> Scenes { get; set; } = new();
    public List<SpellSpec> Spells { get; set; } = new();
    public List<MagicEffectSpec> MagicEffects { get; set; } = new();
    public List<PotionSpec> Potions { get; set; } = new();
    public List<ArmorSpec> Armors { get; set; } = new();
    public List<FactionSpec> Factions { get; set; } = new();
    public List<MessageSpec> Messages { get; set; } = new();
    public List<ScriptAttachSpec> Scripts { get; set; } = new();
    public List<CellSpec> Cells { get; set; } = new();
    public List<PlacementSpec> Placements { get; set; } = new();
    public List<LeveledItemSpec> LeveledItems { get; set; } = new();
    public List<LeveledNpcSpec> LeveledNpcs { get; set; } = new();
    public List<ContainerSpec> Containers { get; set; } = new();
    public List<IngredientSpec> Ingredients { get; set; } = new();
    public List<AmmunitionSpec> Ammunitions { get; set; } = new();
    public List<ScrollSpec> Scrolls { get; set; } = new();
    public List<SoulGemSpec> SoulGems { get; set; } = new();
    public List<KeySpec> Keys { get; set; } = new();
    public List<KeywordSpec> Keywords { get; set; } = new();
    public List<OutfitSpec> Outfits { get; set; } = new();
    public List<StaticSpec> Statics { get; set; } = new();
    public List<ActivatorSpec> Activators { get; set; } = new();
    public List<RecipeSpec> Recipes { get; set; } = new();
    public List<ClassSpec> Classes { get; set; } = new();
    public List<PackageSpec> Packages { get; set; } = new();
    public List<CombatStyleSpec> CombatStyles { get; set; } = new();
    public List<RelationshipSpec> Relationships { get; set; } = new();
    public List<WordOfPowerSpec> WordsOfPower { get; set; } = new();
    public List<ShoutSpec> Shouts { get; set; } = new();
    public List<EnchantmentSpec> Enchantments { get; set; } = new();
}
