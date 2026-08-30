namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // --- placements, leveled lists, encounter zones, vendors, worldspaces, regions, shouts, word walls ---
        // Validates: placements, mapMarkers, leveledItems, leveledNpcs, formLists, containers,
        // worldspaces. Continues in Generator.Validate.World.More.cs, which this method tail-calls.
        // Note that "World.More" also carries factions/npcs/shouts/wordWalls — it is where the
        // per-collection sweep spilled over, not a second world-shaped concern.
        public void ValidateWorld()
        {
            foreach (var pl in spec.Placements)
            {
                CheckRef(pl.Base, "placement base");
                // LVLN (LeveledNpc list) CANNOT be a placement base of any kind — as an ACHR base Skyrim
                // CTDs at load, and as a REFR base it's an un-placeable form. Flag any in-spec LVLN base
                // regardless of kind. Use an NPC_ whose template chain references the LVLN (e.g.
                // LvlBanditMeleeAny=0x01E79C not LCharBanditMeleeAny=0x03DECD). We can only detect in-spec
                // LVLNs here; external refs must be verified manually with "npcdiag <Skyrim.esm> 0xFORMID".
                if (spec.LeveledNpcs.Any(l => l.EditorId.Equals(pl.Base, StringComparison.OrdinalIgnoreCase)))
                    Problems.Add($"placement '{pl.EditorId ?? pl.Base}' base '{pl.Base}' is a LeveledNpc list (LVLN) — LVLN bases cause CTD at load; use an NPC_ actor whose template references the list");
                if (!string.IsNullOrWhiteSpace(pl.Worldspace))
                {
                    // Accept either an external <master>:0xFORMID ref OR an in-spec worldspace editorId
                    // (placements into a custom worldspace land in its generated, navmeshed cell).
                    bool inSpecWs = spec.Worldspaces.Any(w =>
                        string.Equals(w.EditorId, pl.Worldspace, StringComparison.OrdinalIgnoreCase));
                    if (!inSpecWs && (!LooksExternalRef(pl.Worldspace) || !TryExternalRef(pl.Worldspace, out _)))
                        Problems.Add($"placement worldspace '{pl.Worldspace}' must be an in-spec worldspace editorId or a well-formed external <master>:0xFORMID ref (find it: find <Skyrim.esm> <name> Worldspace)");
                }
                else if (string.IsNullOrWhiteSpace(pl.Cell)) Problems.Add("placement has empty cell (and no worldspace — set one or the other)");
                else if (LooksExternalRef(pl.Cell))
                { if (!TryExternalRef(pl.Cell, out _)) Problems.Add($"placement: malformed external cell ref '{pl.Cell}' (expect <master>:0xFORMID)"); }
                else if (!cellIds.Contains(pl.Cell)) Problems.Add($"placement references unknown cell '{pl.Cell}' (in-spec cell editorId or <master>:0xFORMID)");
                if (!string.IsNullOrEmpty(pl.Kind)
                    && !pl.Kind.Equals("npc", StringComparison.OrdinalIgnoreCase)
                    && !pl.Kind.Equals("object", StringComparison.OrdinalIgnoreCase)
                    && !pl.Kind.Equals("xmarker", StringComparison.OrdinalIgnoreCase)
                    && !pl.Kind.Equals("xmarkerHeading", StringComparison.OrdinalIgnoreCase))
                    Problems.Add($"placement kind '{pl.Kind}' invalid (npc|object|xmarker|xmarkerHeading)");
                if (pl.Scale <= 0f)
                    Problems.Add($"placement '{pl.EditorId ?? pl.Base}' scale must be > 0 (got {pl.Scale})");
                if (pl.EnableParent is { } ep)
                {
                    if (string.IsNullOrWhiteSpace(ep.Ref))
                        Problems.Add($"placement '{pl.EditorId}' enableParent has empty ref");
                    else
                        CheckRef(ep.Ref, $"placement '{pl.EditorId}' enableParent ref");
                    if (!string.IsNullOrWhiteSpace(ep.Flag)
                        && ep.Flag is not ("SetEnable" or "SetDisable" or "PopIn"))
                        Problems.Add($"placement '{pl.EditorId}' enableParent flag '{ep.Flag}' invalid (SetEnable|SetDisable|PopIn)");
                }
                if (pl.Lock is { } lk)
                {
                    if (string.IsNullOrWhiteSpace(lk.Level))
                        Problems.Add($"placement '{pl.EditorId}' lock has empty level");
                    else if (!IsValidLockLevel(lk.Level))
                        Problems.Add($"placement '{pl.EditorId}' lock level '{lk.Level}' invalid (novice|apprentice|adept|expert|master|requiresKey|inaccessible or 0–255)");
                    CheckRef(lk.Key, $"placement '{pl.EditorId}' lock key");
                }
                if (pl.Ownership is { } own)
                {
                    if (string.IsNullOrWhiteSpace(own.Owner))
                        Problems.Add($"placement '{pl.EditorId}' ownership has empty owner");
                    else
                        CheckRef(own.Owner, $"placement '{pl.EditorId}' ownership owner");
                    if (own.Rank < 0)
                        Problems.Add($"placement '{pl.EditorId}' ownership rank must be >= 0");
                }
                if (pl.Count < 0)
                    Problems.Add($"placement '{pl.EditorId ?? pl.Base}' count must be >= 0");
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

            foreach (var mm in spec.MapMarkers)
            {
                if (string.IsNullOrWhiteSpace(mm.EditorId)) Problems.Add("mapMarker: empty editorId");
                if (string.IsNullOrWhiteSpace(mm.Worldspace))
                    Problems.Add($"mapMarker '{mm.EditorId}' has no worldspace (set an exterior worldspace, e.g. Skyrim.esm:0x00003C)");
                else
                {
                    bool inSpecWs = spec.Worldspaces.Any(w =>
                        string.Equals(w.EditorId, mm.Worldspace, StringComparison.OrdinalIgnoreCase));
                    if (!inSpecWs && (!LooksExternalRef(mm.Worldspace) || !TryExternalRef(mm.Worldspace, out _)))
                        Problems.Add($"mapMarker '{mm.EditorId}' worldspace '{mm.Worldspace}' must be an in-spec worldspace editorId or a well-formed external <master>:0xFORMID ref");
                }
                if (!string.IsNullOrWhiteSpace(mm.Type) && !Enum.TryParse<MapMarker.MarkerType>(mm.Type, true, out _))
                    Problems.Add($"mapMarker '{mm.EditorId}' has unknown type '{mm.Type}' (e.g. City/Town/Settlement/Cave/Camp/Fort/Landmark)");
                foreach (var f in mm.Flags)
                    if (!Enum.TryParse<MapMarker.Flag>(f, true, out _))
                        Problems.Add($"mapMarker '{mm.EditorId}' has unknown flag '{f}' (Visible | CanTravelTo | ShowAllIsHidden)");
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
            foreach (var fl in spec.FormLists)
                foreach (var item in fl.Items) CheckRef(item, $"formList '{fl.EditorId}' item");
            foreach (var ct in spec.Containers)
                foreach (var e in ct.Items) CheckRef(e.Item, $"container '{ct.EditorId}' item");

            // Worldspaces (WRLD): refs resolve; climate strongly advised.
            if (spec.Esl && spec.Worldspaces.Any(ws => ws.Cells.Count > 0 || ws.Heightmap is not null))
                Problems.Add("spec has esl=true but worldspace(s) define terrain (cells or heightmap) — Skyrim's engine does not load LAND records from ESL (light) plugins; set esl=false for any spec that generates terrain");
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

                if (ws.Heightmap is { } hm)
                {
                    if (string.IsNullOrWhiteSpace(hm.Path))
                        Problems.Add($"worldspace '{ws.EditorId}' heightmap has empty path");
                    if (hm.MinHeight >= hm.MaxHeight)
                        Problems.Add($"worldspace '{ws.EditorId}' heightmap minHeight ({hm.MinHeight}) must be < maxHeight ({hm.MaxHeight})");
                }

                CheckRef(ws.BaseTexture, $"worldspace '{ws.EditorId}' baseTexture");
                for (int li = 0; li < ws.TextureLayers.Count; li++)
                {
                    var tl = ws.TextureLayers[li];
                    if (string.IsNullOrWhiteSpace(tl.Texture))
                        Problems.Add($"worldspace '{ws.EditorId}' textureLayer[{li}] has empty texture (LTEX ref)");
                    CheckRef(tl.Texture, $"worldspace '{ws.EditorId}' textureLayer[{li}] texture");
                    if (string.IsNullOrWhiteSpace(tl.Splatmap.Path))
                        Problems.Add($"worldspace '{ws.EditorId}' textureLayer[{li}] has empty splatmap path");
                }
            }

            ValidateWorldMore();
        }

        private static bool IsValidLockLevel(string level)
        {
            var s = level.Trim().ToLowerInvariant();
            return s is "novice" or "apprentice" or "adept" or "expert" or "master" or "requireskey" or "inaccessible"
                || (int.TryParse(s, out var n) && n >= 0 && n <= 255);
        }
    }
}
