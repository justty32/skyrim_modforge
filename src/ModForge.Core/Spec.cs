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
    public List<NpcPatchSpec> NpcPatches { get; set; } = new();   // override existing NPCs' AI packages
    public List<QuestSpec> Quests { get; set; } = new();
    public List<DialogueSpec> Dialogue { get; set; } = new();
    public List<BanterSpec> Banter { get; set; } = new();
    public List<SceneSpec> Scenes { get; set; } = new();
    public List<SpellSpec> Spells { get; set; } = new();
    public List<MagicEffectSpec> MagicEffects { get; set; } = new();
    // Magic combat FX (Spec.MagicFx.cs): a Projectile (PROJ) is the flying bolt; an Explosion
    // (EXPL) is the boom on impact. A MagicEffect's projectile/explosion refs point at these.
    public List<ProjectileSpec> Projectiles { get; set; } = new();
    public List<ExplosionSpec> Explosions { get; set; } = new();
    public List<HazardSpec> Hazards { get; set; } = new();   // Hazard (HAZD) — radius effect / placed trap
    public List<MusicTrackSpec> MusicTracks { get; set; } = new();   // Music Track (MUST)
    public List<MusicTypeSpec> Music { get; set; } = new();          // Music Type (MUSC)
    // ImageSpace Modifiers (IMAD): screen-space post-process (brightness/tint) a magic-effect script
    // applies at runtime. Referenced by Explosions[].imageSpaceModifier or a Papyrus property.
    public List<ImageSpaceModifierSpec> ImageSpaceModifiers { get; set; } = new();
    public List<LightingTemplateSpec> LightingTemplates { get; set; } = new();   // LightingTemplate (LGTM)
    public List<ImageSpaceSpec> ImageSpaces { get; set; } = new();               // ImageSpace (IMGS) base record (≠ IMAD)
    public List<PotionSpec> Potions { get; set; } = new();
    public List<ArmorSpec> Armors { get; set; } = new();
    public List<FactionSpec> Factions { get; set; } = new();
    public List<MessageSpec> Messages { get; set; } = new();
    public List<ScriptAttachSpec> Scripts { get; set; } = new();
    public List<CellSpec> Cells { get; set; } = new();
    public List<PlacementSpec> Placements { get; set; } = new();
    public List<MapMarkerSpec> MapMarkers { get; set; } = new();   // world-map markers (XMRK on MapMarker static)
    public List<LeveledItemSpec> LeveledItems { get; set; } = new();
    public List<LeveledNpcSpec> LeveledNpcs { get; set; } = new();
    public List<FormListSpec> FormLists { get; set; } = new();
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
    public List<TextureSetSpec> TextureSets { get; set; } = new();
    // Custom light sources (LIGT): colour/radius/flicker. Placed via placements[] by editorId.
    public List<LightSpec> Lights { get; set; } = new();
    // Atmospheric records. A Weather (WTHR) is a sky; a Climate (CLMT) is a weather
    // cycle + sun/moon timing. See WeatherSpec / ClimateSpec in Spec.Weather.cs.
    public List<WeatherSpec> Weathers { get; set; } = new();
    public List<ClimateSpec> Climates { get; set; } = new();
    public List<WorldspaceSpec> Worldspaces { get; set; } = new();
    public List<RegionSpec> Regions { get; set; } = new();
    public List<EncounterZoneSpec> EncounterZones { get; set; } = new();
    public List<FurnitureSpec> Furniture { get; set; } = new();
    public List<SoundSpec> Sounds { get; set; } = new();
    public List<PerkSpec> Perks { get; set; } = new();
    public List<WordWallSpec> WordWalls { get; set; } = new();
    public List<GlobalSpec> Globals { get; set; } = new();   // GlobalVariable (GLOB) — shared flags/counters/constants
    public List<IdentitySpec> Identities { get; set; } = new(); // lightweight identity/class system (Spec.Identity.cs)
    public PresetCatalogSpec Presets { get; set; } = new(); // non-emitting cookbook fragments for copy/paste recipes
    public List<VoiceTemplateSpec> VoiceTemplates { get; set; } = new(); // named voice recipes (Spec.Voice.cs)
    public List<VoiceSpeakerSpec> VoiceSpeakers { get; set; } = new(); // bind an external speaker → voiceType + template
    // Action-system loose-file generation (Spec.AnimationReplacer.cs) — NON-esp config/asset
    // products: OAR replacer/moveset folders, BDI graph-variable injection, PIE macro tables.
    // `package` emits these next to the .esp; the .hkx animations themselves are user-supplied.
    public List<AnimationReplacerSpec> AnimationReplacers { get; set; } = new();
    public List<BehaviorDataSpec> BehaviorData { get; set; } = new();   // BDI graph var/event config
    public List<PayloadMacroSpec> PayloadMacros { get; set; } = new();  // PIE .ini macro table
    public List<SpidDistributionSpec> SpidDistributions { get; set; } = new(); // SPID _DISTR.ini (loose)
    public List<McmSpec> McmConfigs { get; set; } = new(); // MCM Helper config.json + settings.ini (loose; Spec.Mcm.cs)
    public List<FormListInjectSpec> FormListInjects { get; set; } = new(); // FLM <file>_FLM.ini (loose; Spec.FormListInject.cs)
    public List<KidDistributionSpec> KidDistributions { get; set; } = new(); // KID <file>_KID.ini (loose; Spec.KidDistribution.cs)
    public List<ObjectSwapSpec> ObjectSwaps { get; set; } = new();       // BOS <file>_SWAP.ini (loose; Spec.ObjectSwap.cs)
    public List<AnimObjectSwapSpec> AnimObjectSwaps { get; set; } = new(); // AOS <file>_ANIO.ini (loose; Spec.AnimObjectSwap.cs)
    public List<SkyPatcherSpec> SkyPatchers { get; set; } = new();       // SkyPatcher <recordType>/<file>.ini (loose; Spec.SkyPatcher.cs)
    public List<ConditionTemplateSpec> ConditionTemplates { get; set; } = new(); // named reusable CTDA blocks (M組; Spec.Dialogue.cs)
    public VoiceLineSpec? VoiceLine { get; set; } // global voice output settings
    // External-resource pipeline (see docs/external_assets.md): a source directory whose
    // `Meshes/`, `Textures/`, `Sounds/` (and loose `.hkx`) sub-trees `package` copies next to
    // the .esp so the packaged mod is self-contained / MO2-ready. ModForge REFERENCES + BUNDLES
    // user assets — it does NOT author meshes/anims. A path is relative to the spec file (or
    // absolute); a `package --assets <dir>` CLI arg overrides this.
    public string Assets { get; set; } = "";
}

/// <summary>
/// Non-emitting preset/cookbook catalog. The builder intentionally ignores these fragments; they
/// exist so specs can carry named, schema-valid copy/paste recipes next to the concrete records that
/// use them. Values are arbitrary JSON objects because each category maps to an existing spec family.
/// </summary>
public sealed class PresetCatalogSpec
{
    public Dictionary<string, JsonElement> Lighting { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, JsonElement> Weather { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, JsonElement> Packages { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, JsonElement> Identities { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
