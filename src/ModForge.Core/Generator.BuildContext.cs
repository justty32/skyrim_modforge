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
        // Built INFOs by dialogue editorId, so a pass-2 step can attach result-script fragments
        // (which need ref resolution from the formKey table that only exists in pass 2).
        private readonly Dictionary<string, DialogResponses> dialogResponsesByEd = new();
        // Proactive banter INFOs, kept so pass 2 can append their situational conditions (mirrors dialogResponsesByEd).
        private readonly List<(BanterSpec Spec, DialogResponses Info, string Label)> banterInfos = new();
        // Scene actor aliases, kept so pass 2 can bind each to the NPC that fills it (UniqueActor link —
        // the NPC ref may be forward or external, so it resolves only after the formKey table exists).
        private readonly List<(string SceneEd, int AliasId, string NpcRef, QuestAlias Alias)> sceneAliasWires = new();
        private readonly Dictionary<(int Block, int Sub), CellSubBlock> interiorSubs = new();
        private readonly Dictionary<string, FormKey> formKeyByEd = new();
        private readonly Dictionary<string, IMajorRecord> recordsByEd = new();

        // Package slot wiring deferred until placements register their editorIds (the target/
        // destination of a Patrol/Follow/Escort can be an authored marker created later).
        private readonly List<(IPackage Pack, sbyte Slot, string SlotName, string Ed, string Ref)> deferredTargetWires = new();
        private readonly List<(IPackage Pack, sbyte Slot, string SlotName, string Ed, string Ref, uint Radius)> deferredLocationWires = new();
        private readonly Dictionary<string, IPlaced> placementsByEd = new();

        // Stats counters (accumulated across the steps, read by ToResult).
        private int dialogueBuilt, banterBuilt;
        private int scenesBuilt, scenePhasesBuilt;
        private int linksWired, extLinks;
        private int placed, vanillaCells;
        private int worldspaceCount, exteriorNewCells;
        private int scriptsAttached;

        public BuildContext(ModSpec spec, ModKey outputKey, BuildOptions? options)
        {
            this.spec = spec;
            mod = new SkyrimMod(outputKey, SkyrimRelease.SkyrimSE);
            skyrimData = options?.SkyrimDataPath
                ?? Environment.GetEnvironmentVariable("MODFORGE_SKYRIM_DATA")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                ".local", "share", "Steam", "steamapps", "common", "Skyrim Special Edition", "Data");
        }

        private void Warn(string message) => warnings.Add(message);

        // Open (and cache) a master's link-cache by file name; warns + caches null if missing.
        // NOTE: Skyrim.esm is LOCALIZED, so its TranslatedString fields (Name/Description/
        // BookText) live in .STRINGS inside a BSA. We must NOT DeepCopy those (it triggers
        // an all-string-source resolve that needs the plugins.txt/load-order listings path,
        // absent headless on Linux). The weapon/book clone uses a TranslationMask to skip
        // exactly those fields (we override them anyway), so no string resolution happens.
        private ILinkCache<ISkyrimMod, ISkyrimModGetter>? MasterCache(string masterName)
        {
            if (masterCaches.TryGetValue(masterName, out var cached)) return cached;
            var path = Path.Combine(skyrimData, masterName);
            ILinkCache<ISkyrimMod, ISkyrimModGetter>? cache = null;
            if (!File.Exists(path))
                Warn($"  ! master '{masterName}' not found at {path} (set MODFORGE_SKYRIM_DATA to your Data folder)");
            else
            {
                var getter = SkyrimMod.CreateFromBinaryOverlay(new ModPath(path), SkyrimRelease.SkyrimSE);
                masterDisposables.Add(getter);
                cache = getter.ToImmutableLinkCache<ISkyrimMod, ISkyrimModGetter>();
            }
            masterCaches[masterName] = cache;
            return cache;
        }

        // Resolve a vanilla record (by "<master>:0xFORMID" ref) to clone from. False (caller warns)
        // if the ref is malformed or the master/record can't be found.
        private bool TryResolveTemplate<T>(string templateRef, out T? tmpl) where T : class, ISkyrimMajorRecordGetter
        {
            tmpl = null;
            if (string.IsNullOrWhiteSpace(templateRef)) return false;
            int colon = templateRef.IndexOf(':');
            if (colon <= 0 || !TryExternalRef(templateRef, out var fk)) return false;
            var cache = MasterCache(templateRef[..colon].Trim());
            return cache is not null && cache.TryResolve<T>(fk, out tmpl);
        }

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

        // Build a PackageDataLocation: an authored placed-ref → LocationTarget anchored at that
        // ref, else LocationFallback(NearSelf) — anchors at the actor's current position with no
        // external dependency. NEVER use NearEditorLocation: it needs a CK-set Editor Location on
        // the NPC; Mutagen-generated NPCs don't have one, so sandbox/travel silently no-ops in-game.
        private PackageDataLocation MakeLocationSlot(string slotName, string ownerLabel, string refStr, uint radius)
        {
            if (!string.IsNullOrWhiteSpace(refStr)
                && TryResolveRef(refStr, formKeyByEd, out var fk))
            {
                linksWired++;
                if (LooksExternalRef(refStr)) extLinks++;
                return new PackageDataLocation
                {
                    Name = slotName,
                    Location = new LocationTargetRadius
                    {
                        Target = new LocationTarget { Link = new FormLink<IPlacedGetter>(fk) },
                        Radius = radius,
                    }
                };
            }
            if (!string.IsNullOrWhiteSpace(refStr))
                Warn($"  ! {ownerLabel} location '{refStr}' unresolved — falling back to NearSelf");
            return new PackageDataLocation
            {
                Name = slotName,
                Location = new LocationTargetRadius
                {
                    Target = new LocationFallback { Type = LocationTargetRadius.LocationType.NearSelf },
                    Radius = radius,
                }
            };
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
                        + spec.WordsOfPower.Count + spec.Shouts.Count
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
                    DialogueTopics = dialogueBuilt,
                    Scenes = scenesBuilt,
                    ScenePhases = scenePhasesBuilt,
                    LinksWired = linksWired,
                    ExternalLinks = extLinks,
                    ScriptsAttached = scriptsAttached,
                    Placements = placed,
                    NewInteriorCells = spec.Cells.Count,
                    VanillaInteriorCells = vanillaCells,
                    Worldspaces = worldspaceCount,
                    NewExteriorCells = exteriorNewCells,
                },
            };
        }
    }
}
