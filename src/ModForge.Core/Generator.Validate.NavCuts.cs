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
    }
}
