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
        ctx.BuildNpcsAndQuests();                  // Npc, Quest (kept in editorId maps for dialogue)
        ctx.BuildDialogue();                        // Quest->Branch->Topic->INFO, DialogView, Hellos
        ctx.BuildBanter();                          // proactive Idle banter topics (unprompted NPC lines)
        ctx.BuildMagicAndSpells();                 // MagicEffect, Spell
        ctx.BuildConsumablesGearAndMessages();     // Potion, Armor, Faction, Relationship, Class, Message
        ctx.BuildLongTailItems();                  // Ingredient..Activator (scalar fields)
        ctx.BuildListsContainersStylesPackages();  // LeveledItem/Npc, Container, Recipe, CombatStyle, Package
        ctx.BuildCells();                          // interior cells (block/sub GRUP by FormID)

        // --- index editorId -> FormKey, then pass 2: wire cross-record references ---
        ctx.BuildFormKeyTable();
        ctx.WireNpcs();                            // race/class/outfit/voice/crime/combatStyle/spells/factions
        ctx.WireRelationships();                   // RELA Parent/Child NPC refs
        ctx.WireKeywords();                        // keywords on armor/weapon/misc/...
        ctx.WireEffects();                         // magic effects on spell/potion/ingredient/scroll (+ spell equipType)
        ctx.WireMagicEffectRefs();                 // MGEF association/projectile/art/explosion
        ctx.BuildPackageData();                    // PACK template dispatch (sandbox/travel/usemagic/patrol/follow/escort)
        ctx.WireNpcPackages();                     // NPC.Packages list
        ctx.WireOutfits();                         // OTFT contents
        ctx.BuildPlacements();                     // world placement (interior/vanilla/exterior cells)
        ctx.WireLinkedRefs();                      // XLKR between placements (patrol routes)
        ctx.WireDeferredTargets();                 // package SingleRef slot-0 targets (now placements exist)
        ctx.WireDeferredLocations();               // package Destination location slots
        ctx.WireLeveledAndContainers();            // leveled-list entries + container contents
        ctx.WireRecipes();                         // COBJ createdObject/workbench/components
        ctx.AttachScripts();                       // VMAD Papyrus script attachment
        ctx.AttachDialogueResultScripts();         // INFO OnEnd result fragments (dialogue-pick scripts)
        ctx.WireDialogueConditions();              // extra CTDA gates on dialogue INFOs
        ctx.WireBanterConditions();                // situational CTDA gates on banter INFOs
        ctx.WirePackageConditions();               // CTDA gates on AI packages (runtime behaviour switch)

        return ctx.Finish();
    }
}
