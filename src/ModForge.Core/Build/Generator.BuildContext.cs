namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  BuildContext — the shared mutable state of one Build() run. The Build method is a
    //  strict two-pass pipeline (pass 1 creates every record so FormKeys exist, then we
    //  index editorId -> FormKey, then pass 2 wires cross-record refs). Both passes share
    //  a lot of state: the in-progress mod, the warnings list, master link-caches, and the
    //  editorId lookup maps. Rather than thread all that through dozens of parameters we
    //  hold it here and let the per-record-type build steps (defined across the
    //  Generator.Build.*.cs partial files) run as instance methods that mutate it.
    //
    //  WHICH FIELDS BELONG IN THIS FILE: only state that more than one domain reads — the mod
    //  itself, the editorId lookup tables, the deferred-wire queues the placement pass drains,
    //  and the cross-cutting counters. State only one build step touches is declared in THAT
    //  step's own Generator.Build.*.cs partial (a partial class sees its fields from every
    //  part, so this is purely about where the declaration lives). Put a new single-owner
    //  field next to the code that owns it, not here — this file used to be the one every
    //  feature had to edit.
    //
    //  ORDERING IS LOAD-BEARING: record `AddNew()` order assigns FormIDs, so the orchestrator
    //  (Generator.Build.cs) must call the steps in the exact original sequence — the output
    //  is byte-identical to the old single-method Build only because that order is preserved.
    // -------------------------------------------------------------------------------
    private sealed partial class BuildContext
    {
        private readonly ModSpec spec;
        // Per-build placement view. Godot imports are expanded into this copy so Build() never
        // mutates the caller's Placements list (and repeated builds of the same spec stay stable).
        private readonly List<PlacementSpec> placements;
        // Godot instanceId -> source diagnostic. Unlike authored spec records, these IDs enter after
        // pass 1, so a record generated later in pass 2 (for example a bare exterior CELL or an MCM
        // registration quest) can still collide with them. Finish() checks the final plugin, when every
        // late-generated record exists.
        private readonly Dictionary<string, string> godotImportedIdSources =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly SkyrimMod mod;
        private readonly List<string> warnings = new();
        private readonly List<string> notes = new();      // advisory INFO lines (never warnings — see Note())

        // Master link-caches (read-only overlays of Skyrim.esm etc.), lazily opened by name.
        private readonly Dictionary<string, ILinkCache<ISkyrimMod, ISkyrimModGetter>?> masterCaches
            = new(StringComparer.OrdinalIgnoreCase);

        // editorId -> record maps. npcs/quests are needed by the dialogue + pass-2 steps;
        // cells by placement; the formKey/records maps are the pass-2 ref table.
        private readonly Dictionary<string, Npc> npcsByEd = new();
        private readonly Dictionary<string, Quest> questsByEd = new();
        private readonly Dictionary<string, Cell> cellsByEd = new();
        // Built INFOs by dialogue editorId, so a pass-2 step can attach result-script fragments
        // (which need ref resolution from the formKey table that only exists in pass 2).
        private readonly Dictionary<string, DialogResponses> dialogResponsesByEd = new();
        private readonly Dictionary<string, FormKey> formKeyByEd = new();
        private readonly Dictionary<string, IMajorRecord> recordsByEd = new();
        // Placement-pass caches: vanilla interior cells we override (by FormKey), worldspace overrides
        // that host our exterior block tree (by FormKey), and exterior cells resolved per (worldspace, grid).
        private readonly Dictionary<FormKey, ICell> vanillaCellOverrides = new();

        // Package slot wiring deferred until placements register their editorIds (the target/
        // destination of a Patrol/Follow/Escort can be an authored marker created later) — and, since
        // BuildReferences runs in the same window, until references[] labels exist. EVERY ref slot in
        // PackageRefSlots goes through here; resolving one eagerly in BuildPackageData means it can only
        // ever see base records, never a placement editorId or a label.
        // SelfOnUnresolved: the slot's builder already wrote PackageTargetSelf (UseMagic's "cast on
        // whom" — a self-cast package is meaningful), so an unresolved ref keeps Self, not a no-op.
        private readonly List<(IPackage Pack, sbyte Slot, string SlotName, string Ed, string Ref, bool SelfOnUnresolved)> deferredTargetWires = new();
        private readonly List<(IPackage Pack, sbyte Slot, string SlotName, string Ed, string Ref, uint Radius)> deferredLocationWires = new();
        // Enable Parent (XESP) target refs: deferred like a package SingleRef target — the parent may be
        // an in-spec placement editorId defined LATER in placements[] (BuildPlacements resolves top-to-
        // bottom, so a forward pointer misses) or a references[] label (BuildReferences runs entirely
        // after BuildPlacements). WireDeferredEnableParents fills the XESP (Reference + Flags together)
        // once both passes are done; an unresolved ref still leaves NO EnableParent at all (there is no
        // "self" fallback for XESP, unlike a package's selfOnUnresolved target) — matching the pre-fix
        // eager-resolve behaviour exactly.
        private readonly List<(IPlaced Placed, string Ed, string Ref, string Flag)> deferredEnableParentWires = new();
        // Forced alias fills whose ref builds AFTER the alias passes (a placement/xmarker anchor or a map
        // marker). Resolved by WireDeferredForcedAliases once those records exist.
        private readonly List<(Mutagen.Bethesda.Skyrim.QuestAlias Alias, string Ref)> deferredForcedAliases = new();
        // CTDA conditions authored by a step that runs BEFORE BuildPlacements/BuildReferences (perk, Story
        // Manager, quest-alias match filters, scene/phase). A condition's `param`/`reference` may name a
        // PLACEMENT editorId or a references[] label, so it can only be built once those exist —
        // see the build-order rule on BuildCondition (Generator.Build.Conditions.cs). Queued via
        // DeferCondition, drained in enqueue order by WireDeferredConditions (so each target list keeps the
        // exact order the eager code produced). Finalizers run after the drain, for a container that is only
        // attached when at least one of its conditions actually built (the perk effect's PerkCondition tab).
        private readonly List<(IList<Condition> Target, ConditionSpec Spec, string Label,
                               IReadOnlyDictionary<string, int>? AliasIdx, FormKey? OwningScene)> deferredConditionWires = new();
        private readonly Dictionary<string, IPlaced> placementsByEd = new();
        // Every placement we actually built, paired with the CELL it landed in. The navmesh steps
        // (auto navCut + the P1 coverage diagnostics) need the resolved cell — a placement's spec only
        // carries a ref STRING, and re-resolving it would double the master-cache work.
        private readonly List<(PlacementSpec Spec, IPlaced Rec, ICell Cell)> builtPlacements = new();
        // editorId → its source PlacementSpec, so a teleport partner's arrival position/rotation can be
        // read once all placements exist (XTEL stores where the player materialises = the partner's pos/rot).
        private readonly Dictionary<string, PlacementSpec> placementSpecByEd = new(StringComparer.OrdinalIgnoreCase);

        private int linksWired, extLinks;
        private int placed, vanillaCells;
        private int scriptsAttached;

        private readonly BuildOptions? options;

        public BuildContext(ModSpec spec, ModKey outputKey, BuildOptions? options)
        {
            this.spec = spec;
            placements = spec.Placements.ToList();
            this.options = options;
            mod = new SkyrimMod(outputKey, SkyrimRelease.SkyrimSE);
            skyrimData = options?.SkyrimDataPath
                ?? Environment.GetEnvironmentVariable("MODFORGE_SKYRIM_DATA")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                ".local", "share", "Steam", "steamapps", "common", "Skyrim Special Edition", "Data");
        }

        private void Warn(string message) => warnings.Add(message);

        // An INFO line: nothing is wrong, but the spec says something whose in-game meaning is easy to
        // misread (see BuildReferences' area-anchor hint). Kept OUT of `warnings` on purpose — a note
        // must not turn a clean build yellow, and "zero warnings" must stay a meaningful assertion.
        private void Note(string message) => notes.Add(message);

        // --- pass 2 setup: index every record by editorId so refs (possibly forward) resolve. ---
        // All records exist now, so build one editorId -> FormKey table and wire links
        // that may point forward (e.g. an NPC listed before the faction it belongs to).
        public void BuildFormKeyTable()
        {
            foreach (var r in mod.EnumerateMajorRecords())
                if (!string.IsNullOrEmpty(r.EditorID))
                { formKeyByEd[r.EditorID!] = r.FormKey; recordsByEd[r.EditorID!] = r; }
        }

        // Resolve a ref (in-spec editorId OR external <master>:0xFORMID) and run `set`.
        private void Resolve(string what, string refStr, Action<FormKey> set)
        {
            if (string.IsNullOrWhiteSpace(refStr)) return;
            if (TryResolveRef(refStr, formKeyByEd, out var fk))
            {
                set(fk);
                linksWired++;
                if (LooksExternalRef(refStr)) extLinks++;
            }
            else Warn($"  ! {what} ref '{refStr}' unresolved (need in-spec editorId or <master>:0xFORMID)");
        }

        // Resolve a CELL's lightingTemplate / imageSpace ref: a custom in-spec record (by editorId)
        // wins, else a vanilla "<master>:0xFORMID". Runs in pass 1 (formKeyByEd not built yet), so we
        // use the custom maps + the external link cache directly. Returns false (caller warns) if neither.
        private bool ResolveLightingRef(string refStr, out FormKey fk)
        {
            fk = default;
            if (string.IsNullOrWhiteSpace(refStr)) return false;
            if (lgtmByEd.TryGetValue(refStr, out var lt)) { fk = lt.FormKey; return true; }
            if (imgsByEd.TryGetValue(refStr, out var img)) { fk = img.FormKey; return true; }
            if (TryExternalRef(refStr, out var ext)) { fk = ext; return true; }
            return false;
        }

        // Finalize the run: apply the ESL flag, release the master overlays, return the result.
        // Release is safe here because every template clone / cell-env copy is eager (DeepCopyIn /
        // CopyCellEnv) and FormLinks only hold FormKeys, so nothing the write needs depends on the
        // caches still being open. The caller writes the returned mod.
        public BuildResult Finish()
        {
            try
            {
                EnsureUniqueGodotImportedEditorIds(mod, godotImportedIdSources);
                if (spec.Esl) mod.IsSmallMaster = true;
                return ToResult();
            }
            finally
            {
                foreach (var d in masterDisposables) d.Dispose();
            }
        }

        // Assemble the final BuildResult (the in-memory mod + warnings + stats).
        private BuildResult ToResult()
        {
            int total = spec.MiscItems.Count + spec.Books.Count + spec.Weapons.Count + spec.Npcs.Count
                        + spec.Quests.Count + dialogueBuilt + banterBuilt
                        + spec.Spells.Count + spec.Potions.Count + spec.Armors.Count
                        + spec.Factions.Count + spec.Messages.Count + spec.Cells.Count
                        + spec.LeveledItems.Count + spec.LeveledNpcs.Count + spec.Containers.Count
                        + spec.Ingredients.Count + spec.Ammunitions.Count + spec.Scrolls.Count
                        + spec.SoulGems.Count + spec.Keys.Count + spec.Keywords.Count
                        + spec.Outfits.Count + spec.Statics.Count + spec.Activators.Count
                        + spec.MagicEffects.Count + spec.EffectShaders.Count + spec.Classes.Count + spec.Packages.Count
                        + spec.CombatStyles.Count + spec.Relationships.Count + spec.Recipes.Count
                        + spec.WordsOfPower.Count + spec.Shouts.Count + spec.WordWalls.Count
                        + spec.Enchantments.Count + spec.TextureSets.Count
                        + spec.Projectiles.Count + spec.Explosions.Count + spec.ImageSpaceModifiers.Count
                        + spec.Weathers.Count + spec.Climates.Count
                        + spec.EncounterZones.Count
                        + spec.Furniture.Count + spec.Sounds.Count + spec.Perks.Count
                        + worldspacesBuilt + regionsBuilt
                        + scenesBuilt;
                        // (Placements are reported separately in stats, so not folded into `total`.)
            // Read-only over the finished mod + the spec — authors nothing (see Generator.Dependencies.cs).
            var deps = AnalyzeDependencies(mod, spec);
            return new BuildResult
            {
                Mod = mod,
                Warnings = warnings,
                Notes = notes,
                Dependencies = deps,
                // …and the spec's own declaration checked against them (Generator.Requires.cs). Says nothing
                // when the spec has no requires[] section — the caller decides what to do with an error.
                Requires = CheckRequires(spec, deps),
                Stats = new BuildStats
                {
                    Esl = spec.Esl,
                    TopLevelRecords = total,
                    Perks = spec.Perks.Count,
                    DialogueTopics = dialogueBuilt,
                    Scenes = scenesBuilt,
                    ScenePhases = scenePhasesBuilt,
                    LinksWired = linksWired,
                    ExternalLinks = extLinks,
                    ScriptsAttached = scriptsAttached,
                    Placements = placed,
                    NewInteriorCells = spec.Cells.Count,
                    VanillaInteriorCells = vanillaCells,
                    Worldspaces = worldspaceCount + worldspacesBuilt,
                    NewExteriorCells = exteriorNewCells + terrainCellsBuilt,
                    NavmeshCells = navmeshCellsBuilt,
                    NavCuts = navCutsBuilt,
                    NavmeshOverrides = navmeshOverridesBuilt,
                    NavPatches = navPatchesBuilt,
                    Regions = regionsBuilt,
                    EncounterZones = spec.EncounterZones.Count,
                    WordWalls = wordWallsBuilt,
                },
            };
        }
    }
}
