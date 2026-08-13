namespace ModForge;

public static partial class Generator
{
    // --- Settlement population macro-expansion (Idea #22) --------------------------------------
    // A settlement is sugar: it EXPANDS into the low-level records the validated build passes
    // already handle, so every battle-tested pass (placements/ACHR, packages, vendor FACT +
    // merchant container, RELA) does the real work. Called once at the very top of Build()
    // (before pass 1), right after ExpandSkillTrees. Idempotent (guarded on the spec).
    //
    // For each settlement, for each resident:
    //   * one ACHR placement (the NPC) at the spawn marker / position, in the settlement cell
    //   * 2–3 schedule packages bound to the author's anchor refs:
    //       - Sleep  (template 0x019717) at `home`, gated on the sleep window
    //       - Sandbox(template 0x01C254) small-radius at `work`, gated on the work window
    //       - Sandbox(template 0x01C254) large-radius "wander" at the spawn marker (always-on fallback)
    //     appended to npc.Packages in schedule order (wander last = lowest priority)
    //   * faction wiring: join the settlement faction; settlement.crimeFaction -> npc.CrimeFaction
    //   * if `vendor`: a Vendor-flagged FACT + a placed merchant chest (gold) + JobMerchantFaction
    // Plus one settlement FACT per settlement (auto "<editorId>_Faction" unless `settlementFaction`
    // is given), and — when `friendlyResidents` — pairwise Friend RELA between residents.
    //
    // EVERY target field already exists; this macro adds NO new record type and NO runtime script.
    public const string SandboxTemplateRef = "Skyrim.esm:0x01C254";
    public const string SleepTemplateRef = "Skyrim.esm:0x019717";
    public const string GoldRef = "Skyrim.esm:0x00000F";          // Gold001
    public const string JobMerchantFactionRef = "Skyrim.esm:0x051596"; // added automatically by Build.Vendor membership
    private const uint SettlementWorkRadius = 256;   // sandbox-near-workstation (stays at the forge/stall)
    private const uint SettlementWanderRadius = 1024; // roam the settlement when off-shift

    public static void ExpandSettlements(ModSpec spec)
    {
        if (spec.SettlementsExpanded) return;
        spec.SettlementsExpanded = true;
        if (spec.Settlements.Count == 0) return;

        // Index NPCs by editorId so the macro can attach packages/factions to the resident's NpcSpec.
        var npcByEd = new Dictionary<string, NpcSpec>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in spec.Npcs)
            if (!string.IsNullOrWhiteSpace(n.EditorId)) npcByEd[n.EditorId] = n;
        var placementByEd = new Dictionary<string, PlacementSpec>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in spec.Placements)
            if (!string.IsNullOrWhiteSpace(p.EditorId)) placementByEd[p.EditorId] = p;

        foreach (var st in spec.Settlements)
        {
            // Settlement "townsfolk" faction: reference the given one or auto-create it.
            string settlementFaction = st.SettlementFaction;
            if (string.IsNullOrWhiteSpace(settlementFaction))
            {
                settlementFaction = $"{st.EditorId}_Faction";
                spec.Factions.Add(new FactionSpec { EditorId = settlementFaction, Name = settlementFaction });
            }

            var residentNpcEds = new List<string>(); // for the friendly-residents RELA net

            foreach (var r in st.Residents)
            {
                if (!npcByEd.TryGetValue(r.Npc, out var npc)) continue; // validation reports the missing ref
                residentNpcEds.Add(r.Npc);

                // --- spawn point: marker coords (preferred) or explicit fallback position ---
                Vec3 spawnPos = new();
                if (!string.IsNullOrWhiteSpace(r.SpawnAt) && placementByEd.TryGetValue(r.SpawnAt, out var marker))
                    spawnPos = new Vec3 { X = marker.Position.X, Y = marker.Position.Y, Z = marker.Position.Z };
                else if (r.SpawnPosition is { } sp)
                    spawnPos = new Vec3 { X = sp.X, Y = sp.Y, Z = sp.Z };

                // --- ACHR placement (NPC base -> actor ref via BuildPlacements isNpc path) ---
                spec.Placements.Add(new PlacementSpec
                {
                    Base = r.Npc, EditorId = $"{st.EditorId}_{r.Npc}Ref", Cell = st.Cell, Position = spawnPos,
                });

                // --- schedule packages (effective routine = settlement default + resident override) ---
                var sleep = r.Routine?.Sleep ?? st.DailyRoutine.Sleep;
                var work = r.Routine?.Work ?? st.DailyRoutine.Work;
                var scheduled = new List<(int hour, string ed)>();

                if (IsActiveWindow(sleep))
                {
                    var ed = $"{st.EditorId}_{r.Npc}_Sleep";
                    spec.Packages.Add(new PackageSpec
                    {
                        EditorId = ed, Template = SleepTemplateRef,
                        Schedule = new PackageScheduleSpec { Hour = sleep!.From, DurationInMinutes = WindowDuration(sleep) },
                        Sleep = new SleepSpec { Location = r.Home },
                    });
                    scheduled.Add((sleep.From, ed));
                }
                if (IsActiveWindow(work) && !string.IsNullOrWhiteSpace(r.Work))
                {
                    var ed = $"{st.EditorId}_{r.Npc}_Work";
                    spec.Packages.Add(new PackageSpec
                    {
                        EditorId = ed, Template = SandboxTemplateRef,
                        Schedule = new PackageScheduleSpec { Hour = work!.From, DurationInMinutes = WindowDuration(work) },
                        Sandbox = new SandboxSpec { Location = r.Work, Radius = SettlementWorkRadius },
                    });
                    scheduled.Add((work.From, ed));
                }

                // Always-on wander fallback (lowest priority -> appended last). Anchored at the spawn
                // marker when given, else the actor's editor location (where the ACHR spawns).
                var wanderEd = $"{st.EditorId}_{r.Npc}_Wander";
                spec.Packages.Add(new PackageSpec
                {
                    EditorId = wanderEd, Template = SandboxTemplateRef,
                    Sandbox = new SandboxSpec { Location = r.SpawnAt, Radius = SettlementWanderRadius },
                });

                // npc.Packages: scheduled (by hour) first, wander last.
                foreach (var (_, ed) in scheduled.OrderBy(t => t.hour))
                    npc.Packages.Add(ed);
                npc.Packages.Add(wanderEd);

                // --- faction three-piece ---
                if (!npc.Factions.Contains(settlementFaction, StringComparer.OrdinalIgnoreCase))
                    npc.Factions.Add(settlementFaction);
                if (!string.IsNullOrWhiteSpace(st.CrimeFaction))
                    npc.CrimeFaction = st.CrimeFaction;

                // --- vendor: shopkeeper FACT + placed merchant chest ---
                if (r.Vendor is { } v)
                {
                    var chestEd = $"{st.EditorId}_{r.Npc}_MerchantChest";
                    var chestRef = chestEd + "Ref";
                    var merchantFaction = $"{st.EditorId}_{r.Npc}_Merchant";

                    spec.Containers.Add(new ContainerSpec
                    {
                        EditorId = chestEd, Name = $"{npc.Name} Merchant Chest",
                        Items = v.Gold > 0 ? new List<ContainerEntrySpec> { new() { Item = GoldRef, Count = v.Gold } } : new(),
                    });
                    spec.Placements.Add(new PlacementSpec
                    {
                        Base = chestEd, EditorId = chestRef, Cell = st.Cell, Position = spawnPos, Persistent = true,
                    });
                    spec.Factions.Add(new FactionSpec
                    {
                        EditorId = merchantFaction, Name = merchantFaction,
                        Vendor = new VendorSpec
                        {
                            StartHour = (ushort)v.StartHour, EndHour = (ushort)v.EndHour,
                            SellBuyList = v.SellBuyList, NotSellBuyList = v.NotSellBuyList,
                            MerchantContainer = chestRef,
                        },
                    });
                    if (!npc.Factions.Contains(merchantFaction, StringComparer.OrdinalIgnoreCase))
                        npc.Factions.Add(merchantFaction);
                }
            }

            // --- friendly-residents RELA net (opt-in) ---
            if (st.FriendlyResidents)
            {
                for (int a = 0; a < residentNpcEds.Count; a++)
                    for (int b = a + 1; b < residentNpcEds.Count; b++)
                        spec.Relationships.Add(new RelationshipSpec
                        {
                            EditorId = $"{st.EditorId}_Friend_{residentNpcEds[a]}_{residentNpcEds[b]}",
                            Parent = residentNpcEds[a], Child = residentNpcEds[b], Rank = "Friend",
                        });
            }
        }
    }

    private static bool IsActiveWindow(RoutineWindowSpec? w) => w is { } x && x.From >= 0 && x.To >= 0;

    // Window length in minutes, wrapping midnight (from > to). From==To is treated as a full 24h window.
    private static int WindowDuration(RoutineWindowSpec w)
    {
        int span = ((w.To - w.From) % 24 + 24) % 24;
        if (span == 0) span = 24;
        return span * 60;
    }
}
