namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 1: leveled lists, containers, recipes, combat styles, packages (scalar fields) ---
        // Entries/refs that point at other forms are wired in pass 2.
        public void BuildListsContainersStylesPackages()
        {
            // Leveled lists + containers: create the records now (scalar fields + flags); their
            // entries reference other forms, so they're wired in pass 2 with the ref resolver.
            foreach (var li in spec.LeveledItems)
            {
                var r = mod.LeveledItems.AddNew();
                r.EditorID = li.EditorId;
                r.ChanceNone = new Noggog.Percent(Math.Clamp(li.ChanceNone, 0, 100) / 100.0);
                r.Flags = ParseFlags<LeveledItem.Flag>(li.Flags);
                r.Entries = new();
            }
            foreach (var ln in spec.LeveledNpcs)
            {
                var r = mod.LeveledNpcs.AddNew();
                r.EditorID = ln.EditorId;
                r.ChanceNone = new Noggog.Percent(Math.Clamp(ln.ChanceNone, 0, 100) / 100.0);
                r.Flags = ParseFlags<LeveledNpc.Flag>(ln.Flags);
                r.Entries = new();
            }
            foreach (var ct in spec.Containers)
            {
                var r = mod.Containers.AddNew();
                r.EditorID = ct.EditorId; r.Name = ct.Name; r.Weight = ct.Weight;
                r.Items = new();
            }

            // ConstructibleObject (COBJ): created in pass 1 (editorId only, so it registers in the
            // formKey table); createdObject/workbench/component refs are wired in pass 2.
            foreach (var co in spec.Recipes)
            {
                var r = mod.ConstructibleObjects.AddNew();
                r.EditorID = co.EditorId;
                r.CreatedObjectCount = (ushort)Math.Clamp(co.Count, 1, ushort.MaxValue);
            }

            // CombatStyle (CSTY): no FormLinks (all floats + a Flag enum), so fully built in pass 1.
            // An npc's `combatStyle` ref can point at one (resolved in pass 2 alongside race/class/etc.).
            // The six EquipmentScoreMult* fields are the AI's weapon-preference scores — set Magic high
            // for a mage NPC. See cstydiag on csVampireMagic 0x02DFB5 for the gold-standard mage values.
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

            // AI Package (PACK): pass-1 sets scalar fields (flags, interrupt flags, speed, schedule).
            // Template/CombatStyle/OwnerQuest + Sandbox `Data` dictionary inputs are wired in pass 2.
            // Type is Package (=18), never PackageTemplate — those are vanilla-defined.
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

        // --- pass 2: leveled-list entries + container contents (each references an item/npc by ref) ---
        public void WireLeveledAndContainers()
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

        // --- pass 2: Recipes (COBJ) — wire createdObject + workbench keyword + component refs ---
        // Workbench defaults to the forge (CraftingSmithingForge) when unset, so a weapon/armor recipe
        // just works.
        public void WireRecipes()
        {
            foreach (var co in spec.Recipes)
            {
                if (!recordsByEd.TryGetValue(co.EditorId, out var rec) || rec is not IConstructibleObject cobj) continue;
                Resolve($"recipe '{co.EditorId}' createdObject", co.CreatedObject, fk => cobj.CreatedObject.SetTo(fk));
                var bench = string.IsNullOrWhiteSpace(co.Workbench) ? "Skyrim.esm:0x088105" : co.Workbench;
                Resolve($"recipe '{co.EditorId}' workbench", bench, fk => cobj.WorkbenchKeyword.SetTo(fk));
                cobj.Items ??= new();
                foreach (var comp in co.Components)
                    Resolve($"recipe '{co.EditorId}' component", comp.Item, fk =>
                    {
                        var ci = new ContainerItem { Count = comp.Count };
                        ci.Item.SetTo(fk);
                        cobj.Items!.Add(new ContainerEntry { Item = ci });
                    });
            }
        }
    }
}
