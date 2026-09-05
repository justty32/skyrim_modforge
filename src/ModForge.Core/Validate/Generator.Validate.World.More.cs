namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // Continues ValidateWorld (Generator.Validate.World.cs) past its 300-line budget.
        // Validates: regions, encounterZones, cells, placements (cell/zone refs), factions, npcs,
        // wordsOfPower, shouts, wordWalls.
        //
        // Those last five are not "world" at all; they landed here because this is simply where the
        // sweep continued. Moving them to their own domain files was considered and rejected during
        // the 2026-08 refactor: Validate returns problems IN ORDER, that order is printed to the
        // user and one test indexes problems[0], so re-homing checks changes observable output for
        // a purely cosmetic gain. See workflows/refactor/src-layout-plan.md, Batch 5.
        public void ValidateWorldMore()
        {
            // Regions (REGN): worldspace required, ≥3 area points, ≥1 weather entry with chance > 0.
            foreach (var rg in spec.Regions)
            {
                if (string.IsNullOrWhiteSpace(rg.Worldspace))
                    Problems.Add($"region '{rg.EditorId}' has no worldspace (a region must live inside a WRLD)");
                else CheckRef(rg.Worldspace, $"region '{rg.EditorId}' worldspace");
                if (rg.Area.Count == 0)
                    Problems.Add($"region '{rg.EditorId}' has no area (need a polygon of ≥3 world-space points)");
                else if (rg.Area.Count < 3)
                    Problems.Add($"region '{rg.EditorId}' area has only {rg.Area.Count} point(s) — need ≥3 to enclose an area");
                if (rg.Weather.Count == 0)
                    Problems.Add($"region '{rg.EditorId}' has no weather entries — add ≥1 Weather ref+chance (the point of a weather region)");
                else
                {
                    int sum = 0;
                    foreach (var we in rg.Weather)
                    {
                        if (string.IsNullOrWhiteSpace(we.Weather))
                            Problems.Add($"region '{rg.EditorId}' has a weather entry with empty weather ref");
                        else CheckRef(we.Weather, $"region '{rg.EditorId}' weather");
                        CheckRef(we.Global, $"region '{rg.EditorId}' weather global");
                        if (we.Chance < 0) Problems.Add($"region '{rg.EditorId}' weather chance {we.Chance} is negative");
                        else sum += we.Chance;
                    }
                    if (sum <= 0)
                        Problems.Add($"region '{rg.EditorId}' weather chances sum to {sum} — at least one entry needs a chance > 0");
                }
                if (!string.IsNullOrWhiteSpace(rg.MapColor) && !TryParseRgb(rg.MapColor, out _))
                    Problems.Add($"region '{rg.EditorId}' mapColor '{rg.MapColor}' is not a hex RGB (expect 0xRRGGBB)");
            }

            // EncounterZone (ECZN): level range sane, refs resolve, flags parse.
            foreach (var ez in spec.EncounterZones)
            {
                if (ez.MinLevel is < 0 or > 255) Problems.Add($"encounterZone '{ez.EditorId}' minLevel {ez.MinLevel} out of range (0–255)");
                if (ez.MaxLevel is < 0 or > 255) Problems.Add($"encounterZone '{ez.EditorId}' maxLevel {ez.MaxLevel} out of range (0–255)");
                if (ez.MaxLevel != 0 && ez.MinLevel > ez.MaxLevel)
                    Problems.Add($"encounterZone '{ez.EditorId}' minLevel {ez.MinLevel} > maxLevel {ez.MaxLevel} (set maxLevel 0 for an uncapped zone)");
                CheckRef(ez.Owner, $"encounterZone '{ez.EditorId}' owner");
                CheckRef(ez.Location, $"encounterZone '{ez.EditorId}' location");
                foreach (var f in ez.Flags)
                    if (!Enum.TryParse<EncounterZone.Flag>(f, true, out _))
                        Problems.Add($"encounterZone '{ez.EditorId}' invalid flag '{f}' (NeverResets|MatchPcBelowMinimumLevel|DisableCombatBoundary)");
            }
            foreach (var c in spec.Cells)
            {
                CheckRef(c.EncounterZone, $"cell '{c.EditorId}' encounterZone");
                CheckRef(c.Water ?? "", $"cell '{c.EditorId}' water");
                CheckRef(c.AcousticSpace ?? "", $"cell '{c.EditorId}' acousticSpace");
            }
            foreach (var ws in spec.Worldspaces)
            foreach (var c in ws.Cells)
            {
                CheckRef(c.Water ?? "", $"worldspace '{ws.EditorId}' cell ({c.X},{c.Y}) water");
                CheckRef(c.AcousticSpace ?? "", $"worldspace '{ws.EditorId}' cell ({c.X},{c.Y}) acousticSpace");
            }
            foreach (var pl in spec.Placements)
                CheckRef(pl.EncounterZone, $"placement '{(string.IsNullOrWhiteSpace(pl.EditorId) ? pl.Base : pl.EditorId)}' encounterZone");

            // Vendor (merchant) faction data: hours sane, merchant container must be a placed chest.
            var vendorFactEds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in spec.Factions)
            {
                if (f.Vendor is not { } v) continue;
                if (!string.IsNullOrWhiteSpace(f.EditorId)) vendorFactEds.Add(f.EditorId);
                if (v.StartHour > 24) Problems.Add($"faction '{f.EditorId}' vendor.startHour {v.StartHour} out of range (0..24)");
                if (v.EndHour > 24) Problems.Add($"faction '{f.EditorId}' vendor.endHour {v.EndHour} out of range (0..24)");
                if (v.StartHour <= 24 && v.EndHour <= 24 && v.StartHour > v.EndHour)
                    Problems.Add($"faction '{f.EditorId}' vendor hours invalid: startHour {v.StartHour} > endHour {v.EndHour} (shop never opens)");
                CheckRef(v.SellBuyList, $"faction '{f.EditorId}' vendor.sellBuyList");
                if (string.IsNullOrWhiteSpace(v.SellBuyList) && !v.NotSellBuyList)
                    Problems.Add($"faction '{f.EditorId}' vendor has no sellBuyList and notSellBuyList=false — vendor trades no item categories (set a VendorItem FormList ref, e.g. Skyrim.esm:0x06CB48 VendorItemsMisc, or notSellBuyList=true)");
                CheckRef(v.MerchantContainer, $"faction '{f.EditorId}' vendor.merchantContainer");
                if (string.IsNullOrWhiteSpace(v.MerchantContainer))
                    Problems.Add($"faction '{f.EditorId}' vendor.merchantContainer is empty — a vendor needs a placed merchant chest (holds the gold + stock); reference a placement editorId");
                else if (!LooksExternalRef(v.MerchantContainer) && !placementIds.Contains(v.MerchantContainer))
                    Problems.Add($"faction '{f.EditorId}' vendor.merchantContainer '{v.MerchantContainer}' must be a PLACEMENT editorId (the placed chest), not a bare record — give the chest placement an editorId and reference it");
            }
            foreach (var n in spec.Npcs)
            {
                bool isVendorNpc = n.Factions.Any(fr => !LooksExternalRef(fr) && vendorFactEds.Contains(fr));
                if (isVendorNpc && string.IsNullOrWhiteSpace(n.Greeting) && !spec.Dialogue.Any(d => d.SpeakerNpcEditorId == n.EditorId))
                    Problems.Add($"npc '{n.EditorId}' is a vendor (member of a vendor faction) but has no greeting and no dialogue — it won't be conversable, so the 'I'd like to trade' prompt can't appear (set a `greeting`)");
            }

            // Shouts (SHOU) + Words of Power (WOOP).
            foreach (var w in spec.WordsOfPower)
                if (string.IsNullOrWhiteSpace(w.Translation) && string.IsNullOrWhiteSpace(w.Name))
                    Problems.Add($"wordOfPower '{w.EditorId}' has empty translation and name (set at least one — the in-game word text)");
            foreach (var sh in spec.Shouts)
            {
                if (sh.Words.Count is < 1 or > 3)
                    Problems.Add($"shout '{sh.EditorId}' has {sh.Words.Count} word row(s) — a shout needs 1–3 (vanilla shouts have exactly 3)");
                CheckRef(sh.MenuDisplayObject, $"shout '{sh.EditorId}' menuDisplayObject");
                for (int i = 0; i < sh.Words.Count; i++)
                {
                    var ws = sh.Words[i];
                    if (string.IsNullOrWhiteSpace(ws.Word)) Problems.Add($"shout '{sh.EditorId}' word[{i}] has empty word ref");
                    else CheckRef(ws.Word, $"shout '{sh.EditorId}' word[{i}] word");
                    if (string.IsNullOrWhiteSpace(ws.Spell)) Problems.Add($"shout '{sh.EditorId}' word[{i}] has empty spell ref");
                    else CheckRef(ws.Spell, $"shout '{sh.EditorId}' word[{i}] spell");
                    if (ws.RecoveryTime < 0) Problems.Add($"shout '{sh.EditorId}' word[{i}] recoveryTime {ws.RecoveryTime} is negative");
                }
            }

            // WordWall: shout must resolve; wordIndex 1–3; trigger location validated like a placement.
            foreach (var ww in spec.WordWalls)
            {
                if (string.IsNullOrWhiteSpace(ww.Shout)) Problems.Add($"wordWall '{ww.EditorId}' has empty shout ref");
                else CheckRef(ww.Shout, $"wordWall '{ww.EditorId}' shout");
                if (ww.WordIndex is < 1 or > 3)
                    Problems.Add($"wordWall '{ww.EditorId}' wordIndex {ww.WordIndex} out of range (1, 2, or 3)");
                CheckRef(ww.Word, $"wordWall '{ww.EditorId}' word");
                CheckRef(ww.TriggerBase, $"wordWall '{ww.EditorId}' triggerBase");
                if (LooksExternalRef(ww.Shout) && string.IsNullOrWhiteSpace(ww.Word))
                    Problems.Add($"wordWall '{ww.EditorId}' teaches a vanilla/external shout but has no explicit `word` — set `word` to the WOOP it should teach (can't derive from an out-of-spec shout)");
                if (!LooksExternalRef(ww.Shout) && string.IsNullOrWhiteSpace(ww.Word))
                {
                    var sh = spec.Shouts.FirstOrDefault(s => string.Equals(s.EditorId, ww.Shout, StringComparison.OrdinalIgnoreCase));
                    if (sh is not null && (ww.WordIndex < 1 || ww.WordIndex > sh.Words.Count))
                        Problems.Add($"wordWall '{ww.EditorId}' wordIndex {ww.WordIndex} has no matching word in shout '{ww.Shout}' (it defines {sh.Words.Count}) — set `word` explicitly or add the word");
                }
                if (!string.IsNullOrWhiteSpace(ww.Worldspace))
                {
                    if (!LooksExternalRef(ww.Worldspace) || !TryExternalRef(ww.Worldspace, out _))
                        Problems.Add($"wordWall '{ww.EditorId}' worldspace '{ww.Worldspace}' must be a well-formed external <master>:0xFORMID ref");
                }
                else if (string.IsNullOrWhiteSpace(ww.Cell)) Problems.Add($"wordWall '{ww.EditorId}' has empty cell (and no worldspace — the trigger needs a location)");
                else if (LooksExternalRef(ww.Cell))
                { if (!TryExternalRef(ww.Cell, out _)) Problems.Add($"wordWall '{ww.EditorId}' malformed external cell ref '{ww.Cell}'"); }
                else if (!cellIds.Contains(ww.Cell)) Problems.Add($"wordWall '{ww.EditorId}' references unknown cell '{ww.Cell}' (in-spec cell editorId or <master>:0xFORMID)");
            }
        }
    }
}
