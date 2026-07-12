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
    //  ORDERING IS LOAD-BEARING: record `AddNew()` order assigns FormIDs, so the orchestrator
    //  (Generator.Build.cs) must call the steps in the exact original sequence — the output
    //  is byte-identical to the old single-method Build only because that order is preserved.
    // -------------------------------------------------------------------------------
    private sealed partial class BuildContext
    {
        private readonly ModSpec spec;
        private readonly SkyrimMod mod;
        private readonly List<string> warnings = new();
        private readonly string skyrimData;

        // Master link-caches (read-only overlays of Skyrim.esm etc.), lazily opened by name.
        private readonly Dictionary<string, ILinkCache<ISkyrimMod, ISkyrimModGetter>?> masterCaches
            = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<IDisposable> masterDisposables = new();

        // editorId -> record maps. npcs/quests are needed by the dialogue + pass-2 steps;
        // cells by placement; the formKey/records maps are the pass-2 ref table.
        private readonly Dictionary<string, Npc> npcsByEd = new();
        private readonly Dictionary<string, Quest> questsByEd = new();
        private readonly Dictionary<string, Cell> cellsByEd = new();
        private readonly Dictionary<string, Mutagen.Bethesda.Skyrim.Npc> npcPatchesByRef = new();   // npcPatches[] overrides, keyed by overrideOf ref
        // Custom LGTM/IMGS built in pass 1 (before cells), so a CELL can resolve them by editorId.
        private readonly Dictionary<string, LightingTemplate> lgtmByEd = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ImageSpace> imgsByEd = new(StringComparer.OrdinalIgnoreCase);
        // Built INFOs by dialogue editorId, so a pass-2 step can attach result-script fragments
        // (which need ref resolution from the formKey table that only exists in pass 2).
        private readonly Dictionary<string, DialogResponses> dialogResponsesByEd = new();
        // Player DialogTopics by editorId (topic & INFO share an editorId, so formKeyByEd collides —
        // this is the reliable way to resolve a dialogue's TOPIC, e.g. for an ENAM LinkTo target).
        private readonly Dictionary<string, DialogTopic> dialogTopicsByEd = new();
        // Proactive banter INFOs, kept so pass 2 can append their situational conditions (mirrors dialogResponsesByEd).
        private readonly List<(BanterSpec Spec, DialogResponses Info, string Label)> banterInfos = new();
        // Scene actor aliases, kept so pass 2 can bind each to the NPC that fills it (UniqueActor link —
        // the NPC ref may be forward or external, so it resolves only after the formKey table exists).
        private readonly List<(string SceneEd, int AliasId, string NpcRef, QuestAlias Alias)> sceneAliasWires = new();
        // Non-dialog scene Package actions: the PACK ref is a forward link resolved in pass 2 (WireScenes).
        private readonly List<(string SceneEd, SceneAction Action, string PackageRef)> sceneActionWires = new();
        // Scene controller GateGlobal: the GLOB ref is resolved in pass 2 (WireScenes).
        private readonly List<(string HostEd, ScriptObjectProperty Prop, string GlobalRef)> sceneGateWires = new();
        // Built scenes kept so pass 2 can attach scene-level + per-phase CTDA conditions (refs by
        // editorId, resolved only after the formKey table exists). `Phases` maps each spec-phase index
        // to the ScenePhase actually emitted (a phase with an invalid speaker is skipped in pass 1).
        private readonly List<(SceneSpec Spec, Scene Built, List<(int SpecIndex, ScenePhase Phase)> Phases)> sceneConditionWires = new();
        private readonly Dictionary<(int Block, int Sub), CellSubBlock> interiorSubs = new();
        private readonly Dictionary<string, FormKey> formKeyByEd = new();
        private readonly Dictionary<string, IMajorRecord> recordsByEd = new();
        // Placement-pass caches: vanilla interior cells we override (by FormKey), worldspace overrides
        // that host our exterior block tree (by FormKey), and exterior cells resolved per (worldspace, grid).
        private readonly Dictionary<FormKey, ICell> vanillaCellOverrides = new();
        private readonly Dictionary<FormKey, Worldspace> worldspaceOverrides = new();
        private readonly Dictionary<(FormKey Ws, int X, int Y), Cell> exteriorCells = new();

        // Package slot wiring deferred until placements register their editorIds (the target/
        // destination of a Patrol/Follow/Escort can be an authored marker created later).
        private readonly List<(IPackage Pack, sbyte Slot, string SlotName, string Ed, string Ref)> deferredTargetWires = new();
        private readonly List<(IPackage Pack, sbyte Slot, string SlotName, string Ed, string Ref, uint Radius)> deferredLocationWires = new();
        // Forced alias fills whose ref builds AFTER the alias passes (a placement/xmarker anchor or a map
        // marker). Resolved by WireDeferredForcedAliases once those records exist.
        private readonly List<(Mutagen.Bethesda.Skyrim.QuestAlias Alias, string Ref)> deferredForcedAliases = new();
        // Object (Form) script properties whose target builds AFTER the script is attached. An alias-script's
        // properties fill in BuildStandaloneQuestAliases (before placements), so a prop pointing at a
        // placement/xmarker editorId is queued here and resolved by WireDeferredScriptObjectProps.
        private readonly List<(Mutagen.Bethesda.Skyrim.ScriptObjectProperty Prop, string Ref, string Warn)> deferredScriptObjectProps = new();
        private readonly Dictionary<string, IPlaced> placementsByEd = new();
        // refs an anchor:"replace" reference stood in for — BuildRemovals disables+buries them alongside
        // the spec's own removals[] (our persistent copy took the vanilla original's place).
        private readonly List<string> referenceRemovals = new();
        // editorId → its source PlacementSpec, so a teleport partner's arrival position/rotation can be
        // read once all placements exist (XTEL stores where the player materialises = the partner's pos/rot).
        private readonly Dictionary<string, PlacementSpec> placementSpecByEd = new(StringComparer.OrdinalIgnoreCase);

        // Vendor (merchant) factions: editorIds of in-spec factions carrying vendor data (so an NPC
        // who joins one also gets JobMerchantFaction). The merchant chest is a PLACEMENT that doesn't
        // exist until the placement loop runs, so its FormLink is deferred like a package target.
        private readonly HashSet<string> vendorFactionEds = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<(IFaction Fact, string FactEd, string Ref)> deferredMerchantContainers = new();

        // Stats counters (accumulated across the steps, read by ToResult).
        private int dialogueBuilt, banterBuilt;
        private int scenesBuilt, scenePhasesBuilt;
        private int linksWired, extLinks;
        private int placed, vanillaCells;
        private int worldspaceCount, exteriorNewCells;
        private int scriptsAttached;
        private int worldspacesBuilt, regionsBuilt, terrainCellsBuilt, navmeshCellsBuilt;
        private int wordWallsBuilt;

        private readonly BuildOptions? options;

        public BuildContext(ModSpec spec, ModKey outputKey, BuildOptions? options)
        {
            this.spec = spec;
            this.options = options;
            mod = new SkyrimMod(outputKey, SkyrimRelease.SkyrimSE);
            skyrimData = options?.SkyrimDataPath
                ?? Environment.GetEnvironmentVariable("MODFORGE_SKYRIM_DATA")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                ".local", "share", "Steam", "steamapps", "common", "Skyrim Special Edition", "Data");
        }

        private void Warn(string message) => warnings.Add(message);

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
            if (spec.Esl) mod.IsSmallMaster = true;
            foreach (var d in masterDisposables) d.Dispose();
            return ToResult();
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
                        + spec.MagicEffects.Count + spec.Classes.Count + spec.Packages.Count
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
            return new BuildResult
            {
                Mod = mod,
                Warnings = warnings,
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
                    Regions = regionsBuilt,
                    EncounterZones = spec.EncounterZones.Count,
                    WordWalls = wordWallsBuilt,
                },
            };
        }
    }
}
