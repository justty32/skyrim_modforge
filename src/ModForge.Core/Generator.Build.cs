namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Build — generate a plugin from a structured spec (the data-driven generator).
    //  Layer between an LLM (NL -> spec) and Mutagen (spec -> valid plugin). Extend by
    //  adding a list to ModSpec + a build step here. Object in, object out: the caller
    //  owns reading the spec and writing the result; warnings are collected, never printed.
    //
    //  This method is only the ORCHESTRATOR — the per-record-type work lives in BuildContext
    //  (Generator.BuildContext.cs) and the Generator.Build.*.cs partials. The call order below
    //  is load-bearing: record AddNew() order assigns FormIDs, and several pass-2 steps depend
    //  on earlier ones (placements register editorIds the deferred package wires resolve, etc.).
    // -------------------------------------------------------------------------------
    /// <summary>
    /// Build a mod from a spec. The result holds the in-memory <see cref="ISkyrimMod"/> (caller
    /// writes it), the non-fatal warnings, and build stats. Run <see cref="Validate"/> first.
    /// </summary>
    public static BuildResult Build(ModSpec spec, ModKey outputKey, BuildOptions? options = null)
    {
        var ctx = new BuildContext(spec, outputKey, options);

        // --- pass 1: create every record (so all FormKeys exist before any ref is wired) ---
        ctx.BuildItems();                          // Misc / Book / Weapon
        ctx.BuildNpcs();                           // Npc (ACTOR) — kept in npcsByEd for dialogue/packages
        ctx.BuildQuests();                         // Quest (QUST) — kept in questsByEd for dialogue
        ctx.BuildWordWallQuests();                 // one start-enabled QUST per word wall (after spec quests)
        ctx.BuildDialogue();                        // Quest->Branch->Topic->INFO, DialogView, Hellos
        ctx.BuildBanter();                          // proactive Idle banter topics (unprompted NPC lines)
        ctx.BuildScenes();                          // SCEN multi-actor conversations (quest aliases + phases + Scene topics)
        ctx.BuildMagicEffects();                   // MagicEffect (MGEF)
        ctx.BuildSpells();                         // Spell (SPEL)
        ctx.BuildEnchantments();                   // ObjectEffect (ENCH) scalar records
        ctx.BuildShouts();                         // WordOfPower (WOOP) + Shout (SHOU) scalar records
        ctx.BuildPotions();                        // Potion (ALCH)
        ctx.BuildArmors();                         // Armor (ARMO)
        ctx.BuildFactions();                       // Faction (FACT) incl. vendor data
        ctx.BuildRelationships();                  // Relationship (RELA)
        ctx.BuildEncounterZones();                 // EncounterZone (ECZN)
        ctx.BuildClasses();                        // Class (CLAS)
        ctx.BuildMessages();                       // Message (MESG)
        ctx.BuildPerks();                          // Perk (PERK) trunk scalar fields
        ctx.BuildIngredients();                    // Ingredient (IGRE)
        ctx.BuildAmmunition();                     // Ammunition (AMMO)
        ctx.BuildScrolls();                        // Scroll (SCRL)
        ctx.BuildSoulGems();                       // SoulGem (SLGM)
        ctx.BuildKeys();                           // Key (KEYM)
        ctx.BuildKeywords();                       // Keyword (KYWD)
        ctx.BuildOutfits();                        // Outfit (OTFT) — contents wired in pass 2
        ctx.BuildStatics();                        // Static (STAT)
        ctx.BuildActivators();                     // Activator (ACTI)
        ctx.BuildFurniture();                      // Furniture (FURN)
        ctx.BuildSounds();                         // Sound Descriptor (SNDR)
        ctx.BuildTextureSets();                    // TextureSet (TXST) retexture map paths
        ctx.BuildLeveledItems();                   // LeveledItem (LVLI)
        ctx.BuildLeveledNpcs();                    // LeveledNpc (LVLN)
        ctx.BuildContainers();                     // Container (CONT)
        ctx.BuildRecipes();                        // ConstructibleObject (COBJ)
        ctx.BuildCombatStyles();                   // CombatStyle (CSTY)
        ctx.BuildPackages();                       // AI Package (PACK) scalar fields
        ctx.BuildCells();                          // interior cells (block/sub GRUP by FormID)
        ctx.BuildWeatherRecords();                 // Weather (WTHR) scalar fields (links wired in pass 2)
        ctx.BuildClimateRecords();                 // Climate (CLMT) scalar fields (weathers wired in pass 2)

        // --- index editorId -> FormKey, then pass 2: wire cross-record references ---
        ctx.BuildFormKeyTable();
        ctx.BuildStoryManager();                   // Story Manager: storyEvent quests → Event/aliases + SMBN/SMQN (pass 2 so forced/condition refs resolve via formKeyByEd)
        ctx.WireNpcs();                            // race/class/outfit/voice/crime/combatStyle/spells/factions
        ctx.WireVendors();                         // FACT vendor sellBuyList + queue deferred merchant-chest links
        ctx.WireRelationships();                   // RELA Parent/Child NPC refs
        ctx.WireScenes();                          // SCEN actor aliases -> the NPC that fills each (UniqueActor)
        ctx.WireKeywords();                        // keywords on armor/weapon/misc/...
        ctx.WireSounds();                          // SNDR category/output + per-record sound FormLinks
        ctx.WireAlternateTextures();               // TXST alt-textures on static/activator meshes
        ctx.WireEffects();                         // magic effects on spell/potion/ingredient/scroll (+ spell equipType)
        ctx.WireEnchantments();                    // ENCH effects + weapon/armor enchantment FormLinks
        ctx.WireMagicEffectRefs();                 // MGEF association/projectile/art/explosion
        ctx.WirePerks();                           // PERK effects/conditions/nextPerk + npc perk grants
        ctx.WireWeatherAndClimateLinks();          // WTHR precipitation + CLMT weather FormLinks
        ctx.WireShouts();                          // SHOU MenuDisplayObject + per-row Word (WOOP) + Spell (SPEL)
        ctx.BuildPackageData();                    // PACK template dispatch (sandbox/travel/usemagic/patrol/follow/escort)
        ctx.WireNpcPackages();                     // NPC.Packages list
        ctx.WireOutfits();                         // OTFT contents
        ctx.BuildWorldspacesAndRegions();          // WRLD + REGN (+ NAVM/NAVI) — BEFORE placements so a
                                                   // placement can target a custom in-spec worldspace's
                                                   // cell, and its patrol markers/linked-refs/package
                                                   // start can be wired by the phases that follow.
        ctx.BuildPlacements();                     // world placement (interior/vanilla/exterior cells)
        ctx.WireLinkedRefs();                      // XLKR between placements (patrol routes)
        ctx.WireTeleportDoors();                   // load-door XTEL teleport pairs (player walk-through links)
        ctx.WireDeferredTargets();                 // package SingleRef slot-0 targets (now placements exist)
        ctx.WireDeferredLocations();               // package Destination location slots
        ctx.WireDeferredMerchantContainers();      // FACT merchant chest + VendorLocation (now placements exist)
        ctx.WireLeveledItems();                    // LVLI entries
        ctx.WireLeveledNpcs();                     // LVLN entries
        ctx.WireEncounterZones();                  // ECZN owner/location refs
        ctx.WireCellZones();                       // cell XEZN (encounterZone) refs
        ctx.WireContainers();                      // CONT contents
        ctx.WireRecipes();                         // COBJ createdObject/workbench/components
        ctx.AttachScripts();                       // VMAD Papyrus script attachment
        ctx.AttachWordWallScripts();               // word-wall teaching-quest fragment (Shout/Word props)
        ctx.AttachDialogueResultScripts();         // INFO OnEnd result fragments (dialogue-pick scripts)
        ctx.WireDialogueConditions();              // extra CTDA gates on dialogue INFOs
        ctx.WireQuestStages();                     // QSDT log-entry CTDA + stage→objective fragment VMAD
        ctx.WireBanterConditions();                // situational CTDA gates on banter INFOs
        ctx.WirePackageConditions();               // CTDA gates on AI packages (runtime behaviour switch)

        return ctx.Finish();
    }
}
