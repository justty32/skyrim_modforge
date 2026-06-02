namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // --- placements, leveled lists, encounter zones, vendors, worldspaces, regions, shouts, word walls ---
        public void ValidateWorld()
        {
            foreach (var pl in spec.Placements)
            {
                CheckRef(pl.Base, "placement base");
                if (!string.IsNullOrWhiteSpace(pl.Worldspace))
                {
                    if (!LooksExternalRef(pl.Worldspace) || !TryExternalRef(pl.Worldspace, out _))
                        Problems.Add($"placement worldspace '{pl.Worldspace}' must be a well-formed external <master>:0xFORMID ref (find it: find <Skyrim.esm> <name> Worldspace)");
                }
                else if (string.IsNullOrWhiteSpace(pl.Cell)) Problems.Add("placement has empty cell (and no worldspace — set one or the other)");
                else if (LooksExternalRef(pl.Cell))
                { if (!TryExternalRef(pl.Cell, out _)) Problems.Add($"placement: malformed external cell ref '{pl.Cell}' (expect <master>:0xFORMID)"); }
                else if (!cellIds.Contains(pl.Cell)) Problems.Add($"placement references unknown cell '{pl.Cell}' (in-spec cell editorId or <master>:0xFORMID)");
                if (!string.IsNullOrEmpty(pl.Kind) && !pl.Kind.Equals("npc", StringComparison.OrdinalIgnoreCase) && !pl.Kind.Equals("object", StringComparison.OrdinalIgnoreCase))
                    Problems.Add($"placement kind '{pl.Kind}' invalid (npc|object)");
                foreach (var lr in pl.LinkedRefs)
                {
                    if (string.IsNullOrWhiteSpace(lr.Target)) Problems.Add($"placement '{pl.EditorId}' linkedRef has empty target");
                    else CheckRef(lr.Target, $"placement '{pl.EditorId}' linkedRef target");
                    CheckRef(lr.Keyword, $"placement '{pl.EditorId}' linkedRef keyword");
                }
                if (pl.LinkedRefs.Count > 0 && string.IsNullOrWhiteSpace(pl.EditorId))
                    Problems.Add("placement has linkedRefs but no editorId (a linked-ref source must be named so the route can be wired)");
                if (!string.IsNullOrWhiteSpace(pl.Teleport))
                {
                    CheckRef(pl.Teleport, $"placement '{pl.EditorId}' teleport partner");
                    if (string.IsNullOrWhiteSpace(pl.EditorId))
                        Problems.Add("placement has a teleport but no editorId (a teleport door must be named so its partner can link back)");
                    if (string.Equals(pl.Teleport, pl.EditorId, StringComparison.OrdinalIgnoreCase))
                        Problems.Add($"placement '{pl.EditorId}' teleport points at itself");
                }
            }
            // Teleport reciprocity: when the partner is ALSO an in-spec placement, it should teleport
            // back at this door — a one-way in-spec link is almost always an authoring slip.
            var teleportByEd = spec.Placements
                .Where(p => !string.IsNullOrWhiteSpace(p.EditorId) && !string.IsNullOrWhiteSpace(p.Teleport))
                .GroupBy(p => p.EditorId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Teleport, StringComparer.OrdinalIgnoreCase);
            foreach (var pl in spec.Placements)
            {
                if (string.IsNullOrWhiteSpace(pl.Teleport) || LooksExternalRef(pl.Teleport)) continue;
                if (!teleportByEd.TryGetValue(pl.Teleport, out var back)
                    || !string.Equals(back, pl.EditorId, StringComparison.OrdinalIgnoreCase))
                    Problems.Add($"placement '{pl.EditorId}' teleports to in-spec '{pl.Teleport}' but that door does not teleport back (one-way link — set its teleport to '{pl.EditorId}')");
            }

            foreach (var li in spec.LeveledItems)
            {
                foreach (var e in li.Entries) CheckRef(e.Reference, $"leveledItem '{li.EditorId}' entry");
                foreach (var f in li.Flags) if (!Enum.TryParse<LeveledItem.Flag>(f, true, out _)) Problems.Add($"leveledItem '{li.EditorId}' invalid flag '{f}'");
            }
            foreach (var ln in spec.LeveledNpcs)
            {
                foreach (var e in ln.Entries) CheckRef(e.Reference, $"leveledNpc '{ln.EditorId}' entry");
                foreach (var f in ln.Flags) if (!Enum.TryParse<LeveledNpc.Flag>(f, true, out _)) Problems.Add($"leveledNpc '{ln.EditorId}' invalid flag '{f}'");
            }
            foreach (var ct in spec.Containers)
                foreach (var e in ct.Items) CheckRef(e.Item, $"container '{ct.EditorId}' item");

            // Worldspaces (WRLD): refs resolve; climate strongly advised.
            foreach (var ws in spec.Worldspaces)
            {
                if (string.IsNullOrWhiteSpace(ws.Climate))
                    Problems.Add($"worldspace '{ws.EditorId}' has no climate — the world will have no sky/lighting cycle (set a CLMT ref, e.g. Skyrim.esm:0x000812)");
                CheckRef(ws.Climate, $"worldspace '{ws.EditorId}' climate");
                CheckRef(ws.Water, $"worldspace '{ws.EditorId}' water");
                CheckRef(ws.LodWater, $"worldspace '{ws.EditorId}' lodWater");
                CheckRef(ws.Parent, $"worldspace '{ws.EditorId}' parent");
                CheckRef(ws.InteriorLighting, $"worldspace '{ws.EditorId}' interiorLighting");
                CheckRef(ws.Location, $"worldspace '{ws.EditorId}' location");
                CheckRef(ws.Music, $"worldspace '{ws.EditorId}' music");
                CheckRef(ws.EncounterZone, $"worldspace '{ws.EditorId}' encounterZone");
                foreach (var f in ws.Flags)
                    if (!Enum.TryParse<Worldspace.Flag>(f, true, out _))
                        Problems.Add($"worldspace '{ws.EditorId}' invalid flag '{f}' (SmallWorld|CannotFastTravel|NoLodWater|NoLandscape|NoSky|FixedDimensions|NoGrass)");
            }

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
                CheckRef(c.EncounterZone, $"cell '{c.EditorId}' encounterZone");
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
