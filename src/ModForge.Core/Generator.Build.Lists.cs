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

    }
}
