namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 1: leveled lists, containers, recipes, combat styles, packages (scalar fields). Split
        // per-record-type below; the orchestrator calls them in this same order (FormID order is
        // load-bearing). Entries/refs that point at other forms are wired in pass 2. ---

        // --- pass 1: LeveledItem (LVLI) — entries wired in pass 2 (WireLeveledItems) ---
        public void BuildLeveledItems()
        {
            foreach (var li in spec.LeveledItems)
            {
                var r = mod.LeveledItems.AddNew();
                r.EditorID = li.EditorId;
                r.ChanceNone = new Noggog.Percent(Math.Clamp(li.ChanceNone, 0, 100) / 100.0);
                r.Flags = ParseFlags<LeveledItem.Flag>(li.Flags);
                r.Entries = new();
            }
        }

        // --- pass 1: LeveledNpc (LVLN) — entries wired in pass 2 (WireLeveledNpcs) ---
        public void BuildLeveledNpcs()
        {
            foreach (var ln in spec.LeveledNpcs)
            {
                var r = mod.LeveledNpcs.AddNew();
                r.EditorID = ln.EditorId;
                r.ChanceNone = new Noggog.Percent(Math.Clamp(ln.ChanceNone, 0, 100) / 100.0);
                r.Flags = ParseFlags<LeveledNpc.Flag>(ln.Flags);
                r.Entries = new();
            }
        }

        // --- pass 1: Container (CONT) — contents wired in pass 2 (WireContainers) ---
        public void BuildContainers()
        {
            foreach (var ct in spec.Containers)
            {
                var r = mod.Containers.AddNew();
                r.EditorID = ct.EditorId; r.Name = ct.Name; r.Weight = ct.Weight;
                r.Items = new();
            }
        }

        // --- pass 1: ConstructibleObject (COBJ): editorId only (so it registers in the formKey
        // table); createdObject/workbench/component refs are wired in pass 2 (WireRecipes). ---
        public void BuildRecipes()
        {
            foreach (var co in spec.Recipes)
            {
                var r = mod.ConstructibleObjects.AddNew();
                r.EditorID = co.EditorId;
                r.CreatedObjectCount = (ushort)Math.Clamp(co.Count, 1, ushort.MaxValue);
            }
        }

        // --- pass 1: CombatStyle (CSTY): no FormLinks (all floats + a Flag enum), so fully built in pass 1.
        // An npc's `combatStyle` ref can point at one (resolved in pass 2 alongside race/class/etc.).
        // The six EquipmentScoreMult* fields are the AI's weapon-preference scores — set Magic high
        // for a mage NPC. See cstydiag on csVampireMagic 0x02DFB5 for the gold-standard mage values. ---
        public void BuildCombatStyles()
        {
            foreach (var cs in spec.CombatStyles)
            {
                var r = mod.CombatStyles.AddNew();
                r.EditorID = cs.EditorId;
                r.OffensiveMult = cs.OffensiveMult;
                r.DefensiveMult = cs.DefensiveMult;
                r.GroupOffensiveMult = cs.GroupOffensiveMult;
                r.EquipmentScoreMultMelee   = cs.EquipMultMelee;
                r.EquipmentScoreMultMagic   = cs.EquipMultMagic;
                r.EquipmentScoreMultRanged  = cs.EquipMultRanged;
                r.EquipmentScoreMultShout   = cs.EquipMultShout;
                r.EquipmentScoreMultUnarmed = cs.EquipMultUnarmed;
                r.EquipmentScoreMultStaff   = cs.EquipMultStaff;
                r.AvoidThreatChance = cs.AvoidThreatChance;
                r.Flags = ParseFlags<Mutagen.Bethesda.Skyrim.CombatStyle.Flag>(cs.Flags);
            }
        }

        // --- pass 1: AI Package (PACK): scalar fields (flags, interrupt flags, speed, schedule).
        // Template/CombatStyle/OwnerQuest + Sandbox `Data` dictionary inputs are wired in pass 2.
        // Type is Package (=18), never PackageTemplate — those are vanilla-defined. ---
        public void BuildPackages()
        {
            foreach (var pk in spec.Packages)
            {
                var r = mod.Packages.AddNew();
                r.EditorID = pk.EditorId;
                r.Type = Mutagen.Bethesda.Skyrim.Package.Types.Package;
                // Flags + interrupt flags (lifelike-NPC switches: HellosToPlayer / AllowIdleChatter /
                // WorldInteractions / RandomConversations / ReactionToPlayerActions / …)
                r.Flags = ParseFlags<Package.Flag>(pk.Flags);
                r.InterruptFlags = ParseFlags<Package.InterruptFlag>(pk.InterruptFlags);
                if (Enum.TryParse<Package.Speed>(pk.PreferredSpeed, ignoreCase: true, out var spd))
                    r.PreferredSpeed = spd;
                // Schedule — -1/0 = "any" (vanilla DefaultSandbox uses month=-1 hour=-1 minute=-1).
                r.ScheduleMonth = (sbyte)Math.Clamp(pk.Schedule.Month, -1, 11);
                if (Enum.TryParse<Package.DayOfWeek>(pk.Schedule.DayOfWeek, ignoreCase: true, out var dow))
                    r.ScheduleDayOfWeek = dow;
                r.ScheduleDate = (byte)Math.Clamp(pk.Schedule.Date, 0, 31);
                r.ScheduleHour = (sbyte)Math.Clamp(pk.Schedule.Hour, -1, 23);
                r.ScheduleMinute = (sbyte)Math.Clamp(pk.Schedule.Minute, -1, 59);
                r.ScheduleDurationInMinutes = Math.Max(0, pk.Schedule.DurationInMinutes);
            }
        }

        // --- pass 2: LeveledItem (LVLI) entries — each entry references an item by ref ---
        public void WireLeveledItems()
        {
            foreach (var li in spec.LeveledItems)
            {
                if (!recordsByEd.TryGetValue(li.EditorId, out var rec) || rec is not ILeveledItem lvl) continue;
                lvl.Entries ??= new();
                foreach (var e in li.Entries)
                    Resolve($"leveledItem '{li.EditorId}' entry", e.Reference, fk =>
                    {
                        var entry = new LeveledItemEntry { Data = new LeveledItemEntryData { Level = e.Level, Count = e.Count } };
                        entry.Data!.Reference.SetTo(fk);
                        lvl.Entries!.Add(entry);
                    });
            }
        }

        // --- pass 2: LeveledNpc (LVLN) entries — each entry references an npc/list by ref ---
        public void WireLeveledNpcs()
        {
            foreach (var ln in spec.LeveledNpcs)
            {
                if (!recordsByEd.TryGetValue(ln.EditorId, out var rec) || rec is not ILeveledNpc lvl) continue;
                lvl.Entries ??= new();
                foreach (var e in ln.Entries)
                    Resolve($"leveledNpc '{ln.EditorId}' entry", e.Reference, fk =>
                    {
                        var entry = new LeveledNpcEntry { Data = new LeveledNpcEntryData { Level = e.Level, Count = e.Count } };
                        entry.Data!.Reference.SetTo(fk);
                        lvl.Entries!.Add(entry);
                    });
            }
        }

        // --- pass 2: EncounterZone (ECZN) owner/location refs. Owner is a FACT or NPC (IOwner); Location an LCTN. ---
        public void WireEncounterZones()
        {
            foreach (var ez in spec.EncounterZones)
            {
                if (!recordsByEd.TryGetValue(ez.EditorId, out var rec) || rec is not IEncounterZone zone) continue;
                Resolve($"encounterZone '{ez.EditorId}' owner",    ez.Owner,    fk => zone.Owner.SetTo(fk));
                Resolve($"encounterZone '{ez.EditorId}' location", ez.Location, fk => zone.Location.SetTo(fk));
            }
        }

        // --- pass 2: Cell encounterZone (XEZN): a cell's level scaling / respawn zone. In-spec cells were
        // created in pass 1 (cellsByEd); a vanilla-cell override carries the master's zone via CopyCellEnv,
        // but an explicit `encounterZone` here overrides it. Resolves an in-spec ECZN or a vanilla one. ---
        public void WireCellZones()
        {
            foreach (var c in spec.Cells)
            {
                if (string.IsNullOrWhiteSpace(c.EncounterZone)) continue;
                if (!cellsByEd.TryGetValue(c.EditorId, out var cell)) continue;
                Resolve($"cell '{c.EditorId}' encounterZone", c.EncounterZone, fk => cell.EncounterZone.SetTo(fk));
            }
        }

        // --- pass 2: Container (CONT) contents — each entry references an item by ref ---
        public void WireContainers()
        {
            foreach (var ct in spec.Containers)
            {
                if (!recordsByEd.TryGetValue(ct.EditorId, out var rec) || rec is not IContainer cont) continue;
                cont.Items ??= new();
                foreach (var e in ct.Items)
                    Resolve($"container '{ct.EditorId}' item", e.Item, fk =>
                    {
                        var ci = new ContainerItem { Count = e.Count };
                        ci.Item.SetTo(fk);
                        cont.Items!.Add(new ContainerEntry { Item = ci });
                    });
            }
        }

        // --- pass 2: Recipes (COBJ) — wire createdObject + workbench keyword + component refs + CTDA ---
        // `kind` (craft/temper/smelt/breakdown) picks the default bench; `workbench` is a NAMED selector
        // (forge/sharpeningWheel/armorTable/smelter/tanningRack/skyforge) or a raw ref. For a TEMPER
        // recipe the createdObject IS the item being improved. Conditions perk/item/skill gate the
        // recipe — built via the SHARED BuildCondition (Generator.Build.Conditions.cs).
        public void WireRecipes()
        {
            foreach (var co in spec.Recipes)
            {
                if (!recordsByEd.TryGetValue(co.EditorId, out var rec) || rec is not IConstructibleObject cobj) continue;
                Resolve($"recipe '{co.EditorId}' createdObject", co.CreatedObject, fk => cobj.CreatedObject.SetTo(fk));
                var bench = ResolveWorkbenchRef(co.Kind, co.Workbench);
                Resolve($"recipe '{co.EditorId}' workbench", bench, fk => cobj.WorkbenchKeyword.SetTo(fk));
                cobj.Items ??= new();
                foreach (var comp in co.Components)
                    Resolve($"recipe '{co.EditorId}' component", comp.Item, fk =>
                    {
                        var ci = new ContainerItem { Count = comp.Count };
                        ci.Item.SetTo(fk);
                        cobj.Items!.Add(new ContainerEntry { Item = ci });
                    });
                foreach (var cs in co.Conditions)
                    if (BuildCondition(cs, $"recipe '{co.EditorId}' condition") is { } cond)
                        cobj.Conditions.Add(cond);
            }
        }

        // --- pass 1: EncounterZone (ECZN): level range/rank/flags (all inline). Owner/Location
        // FormLinks are wired in pass 2 (WireEncounterZones). maxLevel 0 = "uncapped" (the vanilla
        // scales-with-player idiom). A cell's / placed-spawn's `encounterZone` ref points at one. ---
        public void BuildEncounterZones()
        {
            foreach (var ez in spec.EncounterZones)
            {
                var r = mod.EncounterZones.AddNew();
                r.EditorID = ez.EditorId;
                r.MinLevel = (byte)Math.Clamp(ez.MinLevel, 0, 255);
                r.MaxLevel = (byte)Math.Clamp(ez.MaxLevel, 0, 255);
                r.Rank = (byte)Math.Clamp(ez.Rank, 0, 255);
                r.Flags = ParseFlags<EncounterZone.Flag>(ez.Flags);
            }
        }
    }
}
