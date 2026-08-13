internal static partial class Program
{
    // Diagnostic: dump a worldspace LAND's texture-layer structure (Flags + each Base/Alpha layer's
    // Quadrant / LayerNumber / Texture + per-layer VTXT point count). Used to byte-verify ModForge's
    // generated BTXT/ATXT/VTXT against a real vanilla Tamriel cell — the exact comparison WAIT_USER
    // flags for the worldspace-texture deliverable (rendering itself stays an in-game check; this
    // confirms the record SHAPE the .esp stores matches vanilla).
    //   landdiag <plugin> [worldspaceEditorId] [maxCells]
    // With no worldspace, scans all; prints the first <maxCells> cells whose LAND has texture layers.
    private static int LandDiag(string inPath, string? wsEditorId, int maxCells)
    {
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        int shown = 0;
        foreach (var ws in mod.EnumerateMajorRecords<IWorldspaceGetter>())
        {
            if (wsEditorId is { } w && !string.Equals(ws.EditorID, w, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var block in ws.SubCells)
            foreach (var sub in block.Items)
            foreach (var cell in sub.Items)
            {
                var land = cell.Landscape;
                if (land is null) continue;
                var layers = land.Layers;
                if (layers is null || layers.Count == 0) continue;
                if (shown++ >= maxCells) return 0;

                Console.WriteLine($"[{ws.EditorID}] Cell 0x{cell.FormKey.ID:X6} grid=({cell.Grid?.Point.X},{cell.Grid?.Point.Y})");
                Console.WriteLine($"  LAND Flags = {land.Flags}");
                Console.WriteLine($"  VHGT offset = {land.VertexHeightMap?.Offset}  VNML={(land.VertexNormals is null ? "no" : "yes")}");
                Console.WriteLine($"  {layers.Count} texture layer(s):");
                // NOTE: Mutagen's AlphaLayer : BaseLayer, so check IAlphaLayerGetter FIRST.
                foreach (var layer in layers)
                {
                    if (layer is IAlphaLayerGetter al)
                    {
                        var h = al.Header;
                        int pts = al.AlphaLayerData?.Count ?? 0;
                        Console.WriteLine($"    ATXT  quad={h?.Quadrant} layer={h?.LayerNumber} tex=0x{h?.Texture.FormKey.ID:X6}:{h?.Texture.FormKey.ModKey}  VTXT pts={pts}");
                    }
                    else if (layer is IBaseLayerGetter bl)
                    {
                        var h = bl.Header;
                        Console.WriteLine($"    BTXT  quad={h?.Quadrant} layer={h?.LayerNumber} tex=0x{h?.Texture.FormKey.ID:X6}:{h?.Texture.FormKey.ModKey}");
                    }
                }
            }
        }
        if (shown == 0) Console.WriteLine($"no textured LAND found{(wsEditorId is { } w2 ? $" in worldspace '{w2}'" : "")}");
        return 0;
    }
}
