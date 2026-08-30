namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Flat-navmesh authoring for custom exterior worldspaces.
    //
    //  Split out of Generator.Build.Worldspace.cs: the WRLD/cell/region RECORD layer lives there;
    //  the per-cell NVNM quad and the single shared NAVI (NavigationMeshInfoMap) override live here.
    //  A cell whose `navmesh` flag is set gets a 4-vertex/2-triangle quad at terrain height so NPCs
    //  can path across it; every such mesh's NVMI entry is then appended to the master 0x00012FB4
    //  NavMeshInfoMap (NOT a new record — that CTDs the engine; see WriteNaviInfoMap).
    // -------------------------------------------------------------------------------

    // One emitted navmesh + the geometry the NAVI map needs to index it.
    private readonly record struct NavmCellInfo(
        NavigationMesh Navm, int X, int Y, Noggog.P3Float Center,
        Noggog.P3Float Min, Noggog.P3Float Max, FormKey WorldspaceFk);

    // Build a flat quad navmesh for one terrain cell, attach it to `cell`, and record its NAVI info.
    // Vertices are world-space (not cell-local); GridDivisor=1 → trivial 1×1 grid.
    private static void AddFlatCellNavmesh(
        SkyrimMod mod, Cell cell, WorldspaceCellSpec cs, FormKey worldspaceFk, List<NavmCellInfo> navmInfos)
    {
        float wx0 = cs.X * 4096f, wy0 = cs.Y * 4096f;
        float wx1 = wx0 + 4096f,  wy1 = wy0 + 4096f;
        float h = cs.Height;

        var navm = new NavigationMesh(mod);
        var data = new NavigationMeshData();
        data.NavmeshVersion = NavigationMeshData.NavmeshVersionDefault;
        // NVNM offset-4 "Magic" (Mutagen calls it CrcHash). xEdit defaults new
        // navmeshes to bytes 3C A0 E9 A5; little-endian that is 0xA5E9A03C.
        data.CrcHash = 0xA5E9A03C;

        // Exterior cells use WorldspaceNavmeshParent (not CellNavmeshParent — that is for interiors).
        var navParent = new WorldspaceNavmeshParent();
        navParent.Parent.SetTo(worldspaceFk);
        data.Parent = navParent;

        // V0=SW, V1=SE, V2=NE, V3=NW (CCW winding from above)
        data.Vertices.Add(new Noggog.P3Float(wx0, wy0, h));
        data.Vertices.Add(new Noggog.P3Float(wx1, wy0, h));
        data.Vertices.Add(new Noggog.P3Float(wx1, wy1, h));
        data.Vertices.Add(new Noggog.P3Float(wx0, wy1, h));

        // T0: V0,V1,V2 | T1: V0,V2,V3. The two triangles share the V0–V2 diagonal,
        // so they must reference each other across that edge (a value of -1 means
        // "no neighbour" / a border edge). EdgeLink_n is the neighbouring triangle
        // index across the edge between local vertices n and n+1:
        //   T0 edge 2-0 (V2→V0) == diagonal → neighbour T1
        //   T1 edge 0-1 (V0→V2) == diagonal → neighbour T0
        data.Triangles.Add(new NavmeshTriangle
            { Vertices = new Noggog.P3Int16(0, 1, 2), EdgeLink_0_1 = -1, EdgeLink_1_2 = -1, EdgeLink_2_0 = 1 });
        data.Triangles.Add(new NavmeshTriangle
            { Vertices = new Noggog.P3Int16(0, 2, 3), EdgeLink_0_1 = 0, EdgeLink_1_2 = -1, EdgeLink_2_0 = -1 });

        // 1×1 NavmeshGrid: [count=2][idx=0][idx=1]
        data.NavmeshGrid = new byte[] { 2, 0, 0, 0,  0, 0,  1, 0 };
        data.NavmeshGridDivisor = 1;
        data.MaxDistanceX = 4096f;
        data.MaxDistanceY = 4096f;
        data.Min = new Noggog.P3Float(wx0, wy0, h);
        data.Max = new Noggog.P3Float(wx1, wy1, h);

        navm.Data = data;
        cell.NavigationMeshes.Add(navm);
        navmInfos.Add(new NavmCellInfo(navm, cs.X, cs.Y,
            new Noggog.P3Float((wx0 + wx1) / 2f, (wy0 + wy1) / 2f, h),
            new Noggog.P3Float(wx0, wy0, h), new Noggog.P3Float(wx1, wy1, h), worldspaceFk));
    }

    // Connect every shared east/west and north/south cell boundary in both directions. Skyrim's
    // triangle edge stores an index into this NAVM's EdgeLinks[] and sets the matching edge-link
    // flag; that table entry then names the neighbouring NAVM and triangle. (It is not a magic -2
    // value in the Mutagen 0.53 Skyrim model.) Heights deliberately do not participate in matching.
    private static void ConnectAdjacentCellNavmeshes(IReadOnlyList<NavmCellInfo> navmInfos)
    {
        var byCell = new Dictionary<(FormKey Ws, int X, int Y), NavmCellInfo>();
        foreach (var info in navmInfos)
            byCell.TryAdd((info.WorldspaceFk, info.X, info.Y), info);

        static short AddLink(NavigationMeshData from, NavigationMesh to, short targetTriangle)
        {
            var index = checked((short)from.EdgeLinks.Count);
            var link = new EdgeLink { Unknown = 0, TriangleIndex = targetTriangle };
            link.Mesh.SetTo(to.FormKey);
            from.EdgeLinks.Add(link);
            return index;
        }

        foreach (var info in navmInfos)
        {
            var data = info.Navm.Data!;

            // East seam: this T0 edge V1-V2 <-> east T1 edge V3-V0.
            if (byCell.TryGetValue((info.WorldspaceFk, info.X + 1, info.Y), out var east))
            {
                var eastData = east.Navm.Data!;
                data.Triangles[0].EdgeLink_1_2 = AddLink(data, east.Navm, 1);
                data.Triangles[0].Flags |= NavmeshTriangle.Flag.EdgeLink_1_2;
                eastData.Triangles[1].EdgeLink_2_0 = AddLink(eastData, info.Navm, 0);
                eastData.Triangles[1].Flags |= NavmeshTriangle.Flag.EdgeLink_2_0;
            }

            // North seam: this T1 edge V2-V3 <-> north T0 edge V0-V1.
            if (byCell.TryGetValue((info.WorldspaceFk, info.X, info.Y + 1), out var north))
            {
                var northData = north.Navm.Data!;
                data.Triangles[1].EdgeLink_1_2 = AddLink(data, north.Navm, 0);
                data.Triangles[1].Flags |= NavmeshTriangle.Flag.EdgeLink_1_2;
                northData.Triangles[0].EdgeLink_0_1 = AddLink(northData, info.Navm, 1);
                northData.Triangles[0].Flags |= NavmeshTriangle.Flag.EdgeLink_0_1;
            }
        }
    }

    // NAVI (NavigationMeshInfoMap): the engine keeps exactly ONE navmesh-info map for the whole
    // game — Skyrim.esm:0x00012FB4 — and MERGES every plugin's NVMI entries into it additively
    // (verified: Vigilant.esm's 0x12FB4 override lists only its own 897 navmeshes, not the
    // 15,462 vanilla ones). Creating a *new* NAVI record in our plugin produces a second, rogue
    // NavMeshInfoMap that the engine's runtime init dereferences into a null pathing cell →
    // CTD in NavMeshInfoMap::InitItemImpl. So we must OVERRIDE 0x00012FB4 and append our entry,
    // exactly as the CK does on "Finalize Navmesh".
    private static void WriteNaviInfoMap(SkyrimMod mod, IReadOnlyList<NavmCellInfo> navmInfos)
    {
        if (navmInfos.Count == 0) return;

        // Magic constant the CK stamps into both NVNM CrcHash and the NVMI trailing "Unknown"
        // field (0xA5E9A03C — bytes 3C A0 E9 A5). Observed on every vanilla/Vigilant navmesh.
        const int NavmeshMagic = unchecked((int)0xA5E9A03C);

        var navi = new NavigationMeshInfoMap(
            FormKey.Factory("012FB4:Skyrim.esm"), SkyrimRelease.SkyrimSE);
        navi.NavMeshVersion = NavigationMeshData.NavmeshVersionDefault;
        foreach (var info in navmInfos)
        {
            var mi = new NavigationMapInfo();
            mi.NavigationMesh.SetTo(info.Navm.FormKey);
            mi.Point = info.Center;
            mi.Unknown = 0;
            mi.Unknown2 = NavmeshMagic;        // CK stamps the navmesh magic here, not 0
            mi.PreferredMergesFlag = 0;
            // Real navmeshes are NOT marked as islands (verified against Vigilant) — leave
            // Island null (Is Island = 0). No merges/preferred-merges/doors for a standalone mesh.

            // Exterior navmeshes use NavigationMapInfoWorldParent (Cell variant is for interiors).
            var miParent = new NavigationMapInfoWorldParent();
            miParent.ParentWorldspace.SetTo(info.WorldspaceFk);
            miParent.ParentWorldspaceCoord = new Noggog.P2Int16(
                (short)Math.Round(info.Min.X / 4096f), (short)Math.Round(info.Min.Y / 4096f));
            mi.Parent = miParent;
            navi.MapInfos.Add(mi);
        }
        mod.NavigationMeshInfoMaps.Add(navi);   // additive override of the master 0x12FB4
    }
}
