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
    // Master -> the spec fields that named it, snapshotted by ExpandMacros BEFORE it expands anything
    // (Generator.Dependencies.cs). INTERNAL on purpose: not a spec field — it must not deserialize, must
    // not show up in the unknown-field check, and must not be re-walked as spec content.
    internal IReadOnlyDictionary<string, IReadOnlyList<string>>? AuthoredRefSources { get; set; }

    public string PluginName { get; set; } = "Generated.esp";
    public bool Esl { get; set; } = true;
    // DECLARED install requirements (Spec.Requires.cs / Generator.Requires.cs). Compared both ways
    // against the masters the build actually links: an undeclared master FAILS the build, a declared
    // plugin nothing links warns. NULL (absent) = the section was never written = nothing is checked
    // (every spec predating this feature). An EMPTY list is an opt-in too: "this mod stays vanilla-only".
    public List<RequirementSpec>? Requires { get; set; }
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
    public List<EffectShaderSpec> EffectShaders { get; set; } = new();   // EffectShader (EFSH), texture-only VFX
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
    public List<RemovalSpec> Removals { get; set; } = new(); // refs "<master>:0xFORMID" of EXISTING vanilla placed refs to remove (disable + bury); a bare string, or an object carrying an optional `label`/`note` (inert documentation — see Spec.Removals.cs). The in-game eraser spell (Idea #24 §E) feeds this. See Generator.Build.Removals.cs
    public List<OverrideSpec> Overrides { get; set; } = new(); // transform overrides of EXISTING placed refs (move/rotate/rescale in place). The in-game numpad editor feeds this. See Spec.Overrides.cs / Generator.Build.Overrides.cs
    public List<ReferenceSpec> References { get; set; } = new(); // NAME an EXISTING placed ref (in-file placements[] editorId, or a vanilla <master>:0xFORMID) so any other ref field can point at it by `label`. The in-game referrer (`sc ref`) feeds this. See Spec.References.cs / Generator.Build.References.cs
    public List<AnnotationSpec> Annotations { get; set; } = new(); // in-game editor marker anchors (Idea #24 P1; Spec.Annotations.cs) — ADVISORY ONLY, build never turns these into records; a human/agent reads them to author the next round
    public List<NavCutSpec> NavCuts { get; set; } = new(); // L_NAVCUT collision volumes: cut vanilla navmesh at runtime so NPCs path AROUND what you placed (Spec.NavCuts.cs / Generator.Build.NavCuts.cs)
    public List<NavmeshOverrideSpec> NavmeshOverrides { get; set; } = new(); // re-emit a VANILLA cell's NAVM(s) from our plugin, unchanged (no-op override). P0 of the navmesh plan: proves the engine accepts a navmesh that arrives from a patch. See Spec.NavmeshOverrides.cs / Generator.Build.NavmeshOverrides.cs
    public List<NavPatchSpec> NavPatches { get; set; } = new(); // append a convex walkable polygon to one vanilla interior NAVM, preserving every existing triangle index (P3 MVP). See Spec.NavPatches.cs / Generator.Build.NavPatches.cs
    public NavmeshSpec Navmesh { get; set; } = new();      // knobs for the navmesh diagnostics + the auto navcut (Spec.NavCuts.cs)
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
    public List<SkillTreeSpec> SkillTrees { get; set; } = new(); // in-world clickable perk tree (Idea #20; Spec.SkillTree.cs) — macro-expands to globals/activators/placements/scripts
    public List<SettlementSpec> Settlements { get; set; } = new(); // populated settlement (Idea #22; Spec.Settlement.cs) — macro-expands to npcs/packages/placements/factions/containers
    public LivingNpcsSpec? LivingNpcs { get; set; } // living-world NPCs (Idea #23; Spec.LivingNpc.cs) — macro-expands to a controller quest + per-NPC alias/markers/global/rumor + ships MFLivingNpc* .pex
    public List<SceneNpcRoleSpec> NpcRoles { get; set; } = new(); // in-game scene export: tag an external NPC with a job role (Idea #24 §D; Spec.SceneExport.cs) — macro-expands to a host quest + conditioned greeting + sandbox package. NOT the player-facing Identities.
    public List<CapturedItemSpec> CapturedItems { get; set; } = new(); // in-game "definition eyedropper" (Idea #24; Spec.CapturedItems.cs) — macro-expands to WEAP/ARMO(+minted ENCH)/ALCH/INGR
    public List<CapturedNpcSpec> CapturedNpcs { get; set; } = new(); // eyedropped live actors (Idea #24; Spec.CapturedNpcs.cs) — macro-expands to an NpcSpec (identity + face/body recipe) + an ACHR placement at the capture spot
    public VoiceLineSpec? VoiceLine { get; set; } // global voice output settings

    // Guard so the skillTree macro-expansion (Generator.ExpandSkillTrees) runs at most once per spec
    // object even if Build is invoked twice in one process. Not serialized.
    [System.Text.Json.Serialization.JsonIgnore] internal bool SkillTreesExpanded { get; set; }
    // Guard so the settlement macro-expansion (Generator.ExpandSettlements) runs at most once. Not serialized.
    [System.Text.Json.Serialization.JsonIgnore] internal bool SettlementsExpanded { get; set; }
    // Guard so the npc-role macro-expansion (Generator.ExpandNpcRoles) runs at most once. Not serialized.
    [System.Text.Json.Serialization.JsonIgnore] internal bool NpcRolesExpanded { get; set; }
    // Guard so the living-NPC macro-expansion (Generator.ExpandLivingNpcs) runs at most once. Not serialized.
    [System.Text.Json.Serialization.JsonIgnore] internal bool LivingNpcsExpanded { get; set; }
    // Guard so the captured-item macro-expansion (Generator.ExpandCapturedItems) runs at most once. Not serialized.
    [System.Text.Json.Serialization.JsonIgnore] internal bool CapturedItemsExpanded { get; set; }
    // Guard so the captured-NPC macro-expansion (Generator.ExpandCapturedNpcs) runs at most once. Not serialized.
    [System.Text.Json.Serialization.JsonIgnore] internal bool CapturedNpcsExpanded { get; set; }
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
