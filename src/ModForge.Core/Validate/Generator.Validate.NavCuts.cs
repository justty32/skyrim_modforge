namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // navCuts[] — L_NAVCUT volumes (Spec.NavCuts.cs). A navcut is a placed ref, so it needs the same
        // things a placement does: somewhere to live, and a box big enough to be a box.
        public void ValidateNavCuts()
        {
            var nm = spec.Navmesh;
            if (nm.MinFootprint < 0f) Problems.Add($"navmesh.minFootprint must be >= 0 (got {nm.MinFootprint})");
            if (nm.MinHeight < 0f) Problems.Add($"navmesh.minHeight must be >= 0 (got {nm.MinHeight})");
            if (nm.Padding < 0f) Problems.Add($"navmesh.padding must be >= 0 (got {nm.Padding}) — the engine tests actors as zero-volume points, so a navcut box wants padding, never negative");

            for (int i = 0; i < spec.NavCuts.Count; i++)
            {
                var nc = spec.NavCuts[i];
                string who = $"navCut[{i}]" + (string.IsNullOrWhiteSpace(nc.EditorId) ? "" : $" '{nc.EditorId}'");
                bool fromPlacement = !string.IsNullOrWhiteSpace(nc.Placement);

                if (fromPlacement)
                {
                    if (!placementIds.Contains(nc.Placement))
                        Problems.Add($"{who}: placement '{nc.Placement}' is not a placements[] editorId in this spec");
                    if (!string.IsNullOrWhiteSpace(nc.Cell) || !string.IsNullOrWhiteSpace(nc.Worldspace) || nc.Position is not null)
                        Problems.Add($"{who}: `placement` already fixes where the box goes — drop cell/worldspace/position");
                }
                else
                {
                    if (nc.Position is null)
                        Problems.Add($"{who}: needs a `position` (the CENTRE of the box, in the cell's frame) — or a `placement` to wrap");
                    if (nc.Size is null)
                        Problems.Add($"{who}: needs a `size` (the FULL box size w×d×h, not half-extents) — or a `placement` to wrap");
                    if (string.IsNullOrWhiteSpace(nc.Cell) && string.IsNullOrWhiteSpace(nc.Worldspace))
                        Problems.Add($"{who}: needs a `cell` or `worldspace` (a navcut volume is a placed ref — it must live somewhere)");
                }

                if (!string.IsNullOrWhiteSpace(nc.Cell) && !string.IsNullOrWhiteSpace(nc.Worldspace))
                    Problems.Add($"{who}: has BOTH cell and worldspace (a location is one or the other)");
                if (nc.Size is { } s && (s.X <= 0f || s.Y <= 0f || s.Z <= 0f))
                    Problems.Add($"{who}: size must be positive on all three axes (got {s.X}×{s.Y}×{s.Z})");
                if (nc.Padding is < 0f)
                    Problems.Add($"{who}: padding must be >= 0 (got {nc.Padding})");
                CheckRef(nc.Cell, $"{who} cell");
                CheckRef(nc.Worldspace, $"{who} worldspace");
                if (!string.IsNullOrWhiteSpace(nc.EditorId)) Reg(nc.EditorId, "navCut");
            }

            foreach (var pl in spec.Placements)
            {
                if (pl.NavCut is not { } h) continue;
                string who = $"placement '{(string.IsNullOrWhiteSpace(pl.EditorId) ? pl.Base : pl.EditorId)}'";
                if (h.Size is { } s && (s.X <= 0f || s.Y <= 0f || s.Z <= 0f))
                    Problems.Add($"{who} navCut.size must be positive on all three axes (got {s.X}×{s.Y}×{s.Z})");
                if (h.Padding is < 0f)
                    Problems.Add($"{who} navCut.padding must be >= 0 (got {h.Padding})");
                if (!h.Enabled && (h.Size is not null || h.Offset is not null || h.Padding is not null))
                    Problems.Add($"{who} navCut is disabled but still carries size/offset/padding — either drop those, or drop `enabled: false`");
                if (h.Enabled && pl.Kind.Equals("npc", StringComparison.OrdinalIgnoreCase))
                    Problems.Add($"{who} is an NPC (ACHR) — a navcut cuts the ground an actor WALKS on, it is never put on an actor");
            }
        }

        // navmeshOverrides[] — re-emit a VANILLA navmesh unchanged (Spec.NavmeshOverrides.cs). It only
        // ever targets something that already exists in a master, so every target must be an external
        // ref, and an exterior target must say WHICH grid cell.
        public void ValidateNavmeshOverrides()
        {
            for (int i = 0; i < spec.NavmeshOverrides.Count; i++)
            {
                var no = spec.NavmeshOverrides[i];
                string who = $"navmeshOverride[{i}]";
                bool ext = !string.IsNullOrWhiteSpace(no.Worldspace);
                bool inr = !string.IsNullOrWhiteSpace(no.Cell);

                if (ext && inr)
                    Problems.Add($"{who}: has BOTH cell and worldspace (a navmesh lives in one or the other)");
                else if (!ext && !inr)
                    Problems.Add($"{who}: needs a vanilla `cell` (<master>:0xFORMID, interior) or a `worldspace` + grid (exterior)");

                if (ext)
                {
                    if (!LooksExternalRef(no.Worldspace))
                        Problems.Add($"{who}: worldspace must be a vanilla <master>:0xFORMID ref — an in-spec worldspace's navmesh is authored by ModForge already, there is nothing to override");
                    if ((no.X is null || no.Y is null) && no.Position is null)
                        Problems.Add($"{who}: an exterior target needs `x`+`y` (cell grid coords) or a `position` inside the cell");
                    if (no.X is not null ^ no.Y is not null)
                        Problems.Add($"{who}: `x` and `y` are a pair — give both (they are CELL GRID coords, not world units)");
                }
                if (inr && !LooksExternalRef(no.Cell))
                    Problems.Add($"{who}: cell must be a vanilla <master>:0xFORMID ref — an in-spec cell has no vanilla navmesh to override");
                if (!string.IsNullOrWhiteSpace(no.Navmesh) && !LooksExternalRef(no.Navmesh))
                    Problems.Add($"{who}: navmesh must be a vanilla <master>:0xFORMID ref");

                CheckRef(no.Cell, $"{who} cell");
                CheckRef(no.Worldspace, $"{who} worldspace");
                CheckRef(no.Navmesh, $"{who} navmesh");
            }
        }
    }
}
