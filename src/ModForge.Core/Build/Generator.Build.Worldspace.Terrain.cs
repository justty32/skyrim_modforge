namespace ModForge;

public static partial class Generator
{
    // Continues BuildWorldspaces (Generator.Build.Worldspace.cs)。那個方法原本 246 行，塞了
    // 「建 WRLD 記錄 → 解析貼圖層 → 逐格產 CELL+LAND → 走 heightmap」四件事。這裡收的是其中
    // 五段**沒有捕獲外層可變狀態**的段落，所以拆得出來而不用包 back-pointer：
    //   ApplyWorldDefaultsAndMap / ResolveTextureLayers / GetOrAddSubBlock / BuildCellLandscape /
    //   EmitHeightmapCells。搬移逐字，只調整縮排與參數化。

    // Land/water defaults — the flood-fix (a 0 default water height drowns sub-0 terrain).
    // Map-menu bounds + local-map camera.
    private static void ApplyWorldDefaultsAndMap(Worldspace w, WorldspaceSpec ws)
    {
        w.LandDefaults = new WorldspaceLandDefaults
        {
            DefaultLandHeight = ws.DefaultLandHeight,
            DefaultWaterHeight = ws.DefaultWaterHeight,
        };

        // Map-menu bounds + local-map camera.
        var m = ws.Map ?? new WorldMapDataSpec();
        w.MapData = new WorldspaceMap
        {
            NorthwestCellCoords = new Noggog.P2Int16((short)m.NorthwestX, (short)m.NorthwestY),
            SoutheastCellCoords = new Noggog.P2Int16((short)m.SoutheastX, (short)m.SoutheastY),
            UsableDimensions = new Noggog.P2Int(m.UsableWidth, m.UsableHeight),
            CameraInitialPitch = m.CameraInitialPitch,
            CameraMinHeight = m.CameraMinHeight,
            CameraMaxHeight = m.CameraMaxHeight,
        };
    }

    // Additional per-vertex alpha-blended texture layers (ATXT+VTXT). Resolve each LTEX and
    // load its splatmap PNG once here; EmitCell samples the splatmap per cell and stamps the
    // alpha layers. Stacking order = list order (base BTXT = layer 0, then 1, 2, …).
    private static List<(FormKey Tex, Splatmap Map)> ResolveTextureLayers(
        WorldspaceSpec ws, string specDir, Dictionary<string, FormKey> formKeyByEd,
        Action<string> warn, ref int links, ref int extLinks)
    {
        var texLayers = new List<(FormKey Tex, Splatmap Map)>();
        for (int li = 0; li < ws.TextureLayers.Count; li++)
        {
            var tl = ws.TextureLayers[li];
            if (string.IsNullOrWhiteSpace(tl.Texture) || string.IsNullOrWhiteSpace(tl.Splatmap.Path))
            {
                warn($"  ! worldspace '{ws.EditorId}' textureLayer[{li}] missing texture ref or splatmap path — skipped");
                continue;
            }
            if (!TryResolveRef(tl.Texture, formKeyByEd, out var tfk))
            {
                warn($"  ! worldspace '{ws.EditorId}' textureLayer[{li}] texture ref '{tl.Texture}' unresolved — skipped");
                continue;
            }
            links++;
            if (LooksExternalRef(tl.Texture)) extLinks++;
            texLayers.Add((tfk, Splatmap.Load(tl.Splatmap, specDir)));
        }
        return texLayers;
    }

    // Block/sub-block coords follow the same /32 and /8 floor-division the exterior placement
    // code uses (proven against vanilla Skyrim.esm cell groups). Creates either level on demand.
    private static WorldspaceSubBlock GetOrAddSubBlock(Worldspace w, int cx, int cy)
    {
        short bx = (short)FloorDiv(cx, 32), by = (short)FloorDiv(cy, 32);
        short sx = (short)FloorDiv(cx, 8),  sy = (short)FloorDiv(cy, 8);

        var block = w.SubCells.FirstOrDefault(b => b.BlockNumberX == bx && b.BlockNumberY == by);
        if (block is null)
        {
            block = new WorldspaceBlock { BlockNumberX = bx, BlockNumberY = by, GroupType = GroupTypeEnum.ExteriorCellBlock };
            w.SubCells.Add(block);
        }
        var sub = block.Items.FirstOrDefault(s => s.BlockNumberX == sx && s.BlockNumberY == sy);
        if (sub is null)
        {
            sub = new WorldspaceSubBlock { BlockNumberX = sx, BlockNumberY = sy, GroupType = GroupTypeEnum.ExteriorCellSubBlock };
            block.Items.Add(sub);
        }
        return sub;
    }

    // The LAND record for one cell: VHGT heights, VNML normals, the BTXT base texture layer and
    // the per-vertex ATXT/VTXT alpha layers. Pure — takes everything it needs by value.
    // Terrain is a flat 33×33-vertex heightmap at Z=0 with straight-up normals on the flat path —
    // no textures needed for collision; a cell spec gets a CELL + LAND so the player can enter the
    // world via `cow <editorId> X Y` without falling into the void. The VertexNormalsHeightMap flag
    // MUST be set or the engine skips VHGT/VNML and the player falls through.
    private static Landscape BuildCellLandscape(
        SkyrimMod mod, FormKey? baseTexFk, List<(FormKey Tex, Splatmap Map)> texLayers,
        int cx, int cy, float offset, Noggog.Array2d<byte> heightDeltas,
        Noggog.Array2d<Noggog.P3UInt8>? normals)
    {
        var land = new Landscape(mod);
        // VertexNormalsHeightMap = VNML/VHGT present (always). Layers = BTXT/ATXT texture layers
        // present — REQUIRED or the engine ignores the layers and renders untextured terrain
        // (byte-verified: vanilla cells set this bit; omitting it was why textures didn't show).
        land.Flags = Landscape.Flag.VertexNormalsHeightMap;
        if (baseTexFk is not null || texLayers.Count > 0)
            land.Flags |= Landscape.Flag.Layers;
        land.VertexHeightMap = new LandscapeVertexHeightMap
        {
            Offset = offset,           // VHGT scale: actual_Z = Offset * 8
            HeightMap = heightDeltas,
            Unknown = new Noggog.P3UInt8(0, 0, 0),
        };
        // Flat-cell default = straight up. Skyrim VNML is signed-byte; up = (0,0,127) (NOT 128,128,255).
        land.VertexNormals = normals ?? new Noggog.Array2d<Noggog.P3UInt8>(33, 33, new Noggog.P3UInt8(0, 0, 127));

        // Single-layer texture: one BTXT base layer per quadrant, all referencing the same
        // LTEX. The base layer's LayerNumber is 0xFFFF (-1) — vanilla convention for "this is
        // the quadrant base, not an alpha-blended layer" (byte-verified vs Tamriel cells).
        // Alpha (ATXT) layers are 0-indexed independently (0,1,2,…), NOT continuing from the base.
        if (baseTexFk is { } btk)
            foreach (var q in System.Enum.GetValues<Mutagen.Bethesda.Plugins.Records.Quadrant>())
            {
                var header = new LayerHeader { Quadrant = q, LayerNumber = BaseLayerNumber };
                header.Texture.SetTo(btk);
                land.Layers.Add(new BaseLayer { Header = header });
            }

        // Per-vertex alpha texture layers: sample each splatmap at this cell and stamp the
        // ATXT+VTXT layers (sparse; quadrants with no coverage emit nothing). Alpha layers are
        // 0-indexed (the BTXT base is the separate 0xFFFF layer), so splatmap i → layerNumber i.
        for (int li = 0; li < texLayers.Count; li++)
            if (texLayers[li].Map.TrySampleCell(cx, cy, out var alpha))
                foreach (var al in Vtxt.BuildLayers(alpha, texLayers[li].Tex, (ushort)li))
                    land.Layers.Add(al);

        return land;
    }

    // Non-flat terrain: derive the cell grid from PNG size and encode each cell's VHGT, then hand
    // each finished cell to `emit` (the caller's EmitCell, which owns the mod-side side effects).
    // The `navmesh: false` argument is positional here because a delegate parameter has no name.
    private static void EmitHeightmapCells(
        HeightmapSpec hmSpec, string specDir, Action<string> warn,
        Action<int, int, float, Noggog.Array2d<byte>, bool, Noggog.Array2d<Noggog.P3UInt8>?> emit)
    {
        var hm = Heightmap.Load(hmSpec, specDir);
        // Seam stitching: after encoding cell (cxi, cyi) we decode its east/north edge
        // and pass those reconstructed heights to the next cell as its west/south edge.
        // This ensures both sides of every shared boundary reconstruct to identical heights.
        // Without this, each cell encodes independently and rounding can differ by ±8 units.
        var stitchEast  = new float[hm.CellsX, 33]; // [cxi, row] — east col=32 of cell cxi
        var stitchNorth = new float[hm.CellsX, 33]; // [cxi, col] — north row=32 of cell (cxi,cyi)
        for (int cyi = 0; cyi < hm.CellsY; cyi++)
            for (int cxi = 0; cxi < hm.CellsX; cxi++)
            {
                int gx = hm.OriginX + cxi, gy = hm.OriginY + cyi;
                var grid = hm.SampleCell(cxi, cyi);
                // Stitch south edge first so SW corner is later overwritten by west stitch.
                if (cyi > 0)
                    for (int c = 0; c < 33; c++) grid[0, c] = stitchNorth[cxi, c];
                // Stitch west edge (overwrites [0,0] for consistent SW corner).
                if (cxi > 0)
                    for (int r = 0; r < 33; r++) grid[r, 0] = stitchEast[cxi - 1, r];
                var (offset, deltas) = Vhgt.Encode(grid, warn, $"cell({gx},{gy})");
                // Decode reconstructed boundary heights for next cells to stitch against.
                var recon = Vhgt.Decode(offset, deltas);
                for (int r = 0; r < 33; r++) stitchEast[cxi, r]  = recon[r, 32];
                for (int c = 0; c < 33; c++) stitchNorth[cxi, c] = recon[32, c];
                // Compute VNML from extended 35×35 sample (1px border for edge central difference).
                var normals = Vnml.Compute(hm.SampleCellExtended(cxi, cyi));
                emit(gx, gy, offset, deltas, /* navmesh: */ false, normals);
            }
    }
}
