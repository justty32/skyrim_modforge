namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // Validate settlements at the HIGH level (before macro-expansion), so messages name the
        // settlement/resident fields the author wrote. The expanded records (ACHR/packages/FACT/
        // container/RELA) are deterministic from valid input.
        public void ValidateSettlements()
        {
            var settlementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var st in spec.Settlements)
            {
                if (string.IsNullOrWhiteSpace(st.EditorId)) { Problems.Add("settlement: missing editorId"); continue; }
                var who = $"settlement '{st.EditorId}'";
                if (!settlementIds.Add(st.EditorId)) Problems.Add($"{who}: duplicate editorId");
                if (string.IsNullOrWhiteSpace(st.Cell)) Problems.Add($"{who}: missing cell");
                if (!string.IsNullOrWhiteSpace(st.SettlementFaction)) CheckRef(st.SettlementFaction, $"{who} settlementFaction");
                if (!string.IsNullOrWhiteSpace(st.CrimeFaction)) CheckRef(st.CrimeFaction, $"{who} crimeFaction");
                CheckRoutine(st.DailyRoutine, $"{who} dailyRoutine");
                if (st.Residents.Count == 0) { Problems.Add($"{who}: has no residents"); continue; }

                var residentNpcs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var r in st.Residents)
                {
                    if (string.IsNullOrWhiteSpace(r.Npc)) { Problems.Add($"{who}: a resident is missing npc"); continue; }
                    var rwho = $"{who} resident '{r.Npc}'";
                    if (!residentNpcs.Add(r.Npc)) Problems.Add($"{rwho}: duplicate resident npc");
                    if (!npcIds.Contains(r.Npc))
                        Problems.Add($"{rwho}: npc must be an in-spec npcs[] editorId (the resident is placed from it); '{r.Npc}' is not a known npc");

                    // Spawn point: a marker editorId or an explicit fallback position is required.
                    if (string.IsNullOrWhiteSpace(r.SpawnAt) && r.SpawnPosition is null)
                        Problems.Add($"{rwho}: needs a spawn point — give `spawnAt` (a placed marker editorId) or `spawnPosition`");
                    CheckAnchor(r.SpawnAt, $"{rwho} spawnAt");
                    CheckAnchor(r.Home, $"{rwho} home");
                    CheckAnchor(r.Work, $"{rwho} work");

                    // A sleep window with no `home` anchor can't be bound (the Sleep package needs a bed ref).
                    var sleep = r.Routine?.Sleep ?? st.DailyRoutine.Sleep;
                    if (IsActiveWindow(sleep) && string.IsNullOrWhiteSpace(r.Home))
                        Problems.Add($"{rwho}: has a sleep window but no `home` bed anchor — the Sleep package has nothing to bind to");
                    CheckRoutine(r.Routine, $"{rwho} routine");

                    if (r.Vendor is { } v)
                    {
                        if (!string.IsNullOrWhiteSpace(v.SellBuyList)) CheckRef(v.SellBuyList, $"{rwho} vendor.sellBuyList");
                        if (v.StartHour < 0 || v.StartHour > 24) Problems.Add($"{rwho} vendor.startHour must be 0..24");
                        if (v.EndHour < 0 || v.EndHour > 24) Problems.Add($"{rwho} vendor.endHour must be 0..24");
                        if (v.Gold < 0) Problems.Add($"{rwho} vendor.gold must be >= 0");
                    }
                }
            }
        }

        // An anchor (home/work/spawnAt) must be a placed-ref editorId or an external vanilla ref.
        void CheckAnchor(string r, string what)
        {
            if (string.IsNullOrWhiteSpace(r)) return;
            if (LooksExternalRef(r))
            { if (!TryExternalRef(r, out _)) Problems.Add($"{what}: malformed external ref '{r}' (expect <master>:0xFORMID)"); }
            else if (!placementIds.Contains(r))
                Problems.Add($"{what}: '{r}' is not a placed ref editorId (place the anchor in placements[]/the editor and give it an editorId; for a vanilla ref use <master>:0xFORMID)");
        }

        static void CheckRoutineWindow(RoutineWindowSpec? w, string what, List<string> problems)
        {
            if (w is not { } x) return;
            if (x.From < 0 && x.To < 0) return; // both unset -> window ignored
            if (x.From < 0 || x.From > 24) problems.Add($"{what}.from must be 0..24");
            if (x.To < 0 || x.To > 24) problems.Add($"{what}.to must be 0..24");
        }

        void CheckRoutine(RoutineSpec? routine, string what)
        {
            if (routine is not { } rt) return;
            CheckRoutineWindow(rt.Sleep, $"{what}.sleep", Problems);
            CheckRoutineWindow(rt.Work, $"{what}.work", Problems);
        }
    }
}
