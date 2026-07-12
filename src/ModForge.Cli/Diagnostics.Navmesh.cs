using System.IO.Compression;
using System.Text;

internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  navdiag — the GO/NO-GO gate for the whole navmesh route (workflows/plans/navmesh.md, T0.1).
    //
    //  ModForge's navmeshOverrides[] re-emits a VANILLA navmesh from our plugin without touching a
    //  single vertex. "Without touching" is a claim, and the only honest way to check it is to
    //  compare the NVNM subrecord our .esp actually wrote against the NVNM subrecord that sits in
    //  Skyrim.esm — raw bytes, both sides decompressed, no Mutagen in the middle on the vanilla side.
    //  If they differ, Mutagen's parse/serialize lost something (the opaque NavmeshGrid blob, the
    //  cover-triangle table, an EdgeLink into a neighbouring mesh) and every later navmesh phase is
    //  built on sand.
    //
    //    navdiag <in.esp>                       list the plugin's NAVMs; byte-diff each one that
    //                                           overrides a master record against that master
    //    navdiag <in.esp> <0xCELL>              list one cell's NAVMs (point it at Skyrim.esm to scout)
    //    navdiag <in.esp> <0xWRLD> <x> <y>      … the exterior cell at grid (x,y) of a worldspace
    // -------------------------------------------------------------------------------
    private static int NavDiag(string inPath, string? formId, int? gx, int? gy)
    {
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);

        if (formId is not null)
        {
            uint id = NavFormId(formId);
            foreach (var cell in mod.EnumerateMajorRecords<ICellGetter>())
            {
                if (gx is null)
                {
                    if (cell.FormKey.ID != id) continue;
                    PrintCellNavmeshes(cell, null);
                    return 0;
                }
            }
            if (gx is { } cx && gy is { } cy)
            {
                foreach (var ws in mod.EnumerateMajorRecords<IWorldspaceGetter>())
                {
                    if (ws.FormKey.ID != id) continue;
                    foreach (var block in ws.SubCells)
                    foreach (var sub in block.Items)
                    foreach (var cell in sub.Items)
                    {
                        if (cell.Grid?.Point is not { } p || p.X != cx || p.Y != cy) continue;
                        PrintCellNavmeshes(cell, null);
                        return 0;
                    }
                    Console.WriteLine($"worldspace 0x{id:X6} has no cell at grid ({cx},{cy})");
                    return 1;
                }
            }
            Console.WriteLine($"no cell/worldspace 0x{id:X6} in {Path.GetFileName(inPath)}");
            return 1;
        }

        // Whole-plugin mode: list every NAVM, and byte-diff the ones that override a master.
        var own = mod.ModKey;
        var masterBytes = new Dictionary<string, byte[]?>(StringComparer.OrdinalIgnoreCase);
        var ourBytes = File.ReadAllBytes(inPath);
        int seen = 0, identical = 0, diff = 0, unchecked_ = 0;

        foreach (var cell in mod.EnumerateMajorRecords<ICellGetter>())
        {
            if (cell.NavigationMeshes.Count == 0) continue;
            seen += cell.NavigationMeshes.Count;
            PrintCellNavmeshes(cell, navm =>
            {
                if (navm.FormKey.ModKey == own)
                { Console.WriteLine("      NVNM vs master: n/a (a NEW mesh, not an override)"); unchecked_++; return; }

                var masterName = navm.FormKey.ModKey.FileName.String;
                if (!masterBytes.TryGetValue(masterName, out var mb))
                {
                    var path = Path.Combine(SkyrimDataDir(), masterName);
                    mb = File.Exists(path) ? File.ReadAllBytes(path) : null;
                    masterBytes[masterName] = mb;
                }
                if (mb is null)
                { Console.WriteLine($"      NVNM vs master: SKIPPED ({masterName} not found — set MODFORGE_SKYRIM_DATA)"); unchecked_++; return; }

                var vanilla = Nvnm(mb, navm.FormKey.ID);
                var ours = Nvnm(ourBytes, navm.FormKey.ID);
                if (vanilla is null || ours is null)
                { Console.WriteLine($"      NVNM vs master: SKIPPED (NVNM not located: master={(vanilla is null ? "no" : "ok")} ours={(ours is null ? "no" : "ok")})"); unchecked_++; return; }

                if (vanilla.AsSpan().SequenceEqual(ours))
                { Console.WriteLine($"      NVNM vs {masterName}: IDENTICAL ({ours.Length} bytes)"); identical++; }
                else
                {
                    int at = FirstDiff(vanilla, ours);
                    Console.WriteLine($"      NVNM vs {masterName}: ⚠ DIFF (master {vanilla.Length}B, ours {ours.Length}B, first difference at byte {at})");
                    diff++;
                }
            });
        }

        if (seen == 0) { Console.WriteLine($"no NAVM in {Path.GetFileName(inPath)}"); return 0; }
        Console.WriteLine($"-- {seen} navmesh(es): {identical} byte-identical to master, {diff} DIFFERENT, {unchecked_} unchecked");
        return diff == 0 ? 0 : 1;
    }

    private static void PrintCellNavmeshes(ICellGetter cell, Action<INavigationMeshGetter>? extra)
    {
        Console.WriteLine($"Cell 0x{cell.FormKey.ID:X6}:{cell.FormKey.ModKey} '{cell.EditorID}' "
            + $"grid=({cell.Grid?.Point.X},{cell.Grid?.Point.Y}) interior={cell.Flags.HasFlag(Cell.Flag.IsInteriorCell)} "
            + $"— {cell.NavigationMeshes.Count} navmesh(es)");
        foreach (var navm in cell.NavigationMeshes)
        {
            var d = navm.Data;
            if (d is null) { Console.WriteLine($"  NAVM 0x{navm.FormKey.ID:X6}:{navm.FormKey.ModKey} — NO NVNM data"); continue; }
            string parent = d.Parent switch
            {
                IWorldspaceNavmeshParentGetter w => $"worldspace 0x{w.Parent.FormKey.ID:X6}",
                ICellNavmeshParentGetter c => $"cell 0x{c.Parent.FormKey.ID:X6}",
                _ => "none",
            };
            // EdgeLinks = the CROSS-MESH links: a NEIGHBOURING cell's mesh holds an index into OUR
            // triangle array. That is precisely why renumbering a triangle is forbidden (plan §2).
            int deleted = d.Triangles.Count(t => (t.Flags & NavmeshTriangle.Flag.Deleted) != 0);
            int cover = d.Triangles.Count(t => t.IsCover);
            Console.WriteLine($"  NAVM 0x{navm.FormKey.ID:X6}:{navm.FormKey.ModKey}  recFlags=0x{navm.MajorRecordFlagsRaw:X}  parent={parent}");
            Console.WriteLine($"      v={d.Vertices.Count} tri={d.Triangles.Count} (deleted={deleted}) "
                + $"edgeLinks={d.EdgeLinks.Count} doorTri={d.DoorTriangles.Count} cover={cover} "
                + $"grid={d.NavmeshGrid.Length}B div={d.NavmeshGridDivisor} ver={d.NavmeshVersion}");
            Console.WriteLine($"      min=({d.Min.X:F0},{d.Min.Y:F0},{d.Min.Z:F0}) max=({d.Max.X:F0},{d.Max.Y:F0},{d.Max.Z:F0})");
            extra?.Invoke(navm);
        }
    }

    private static int FirstDiff(byte[] a, byte[] b)
    {
        int n = Math.Min(a.Length, b.Length);
        for (int i = 0; i < n; i++) if (a[i] != b[i]) return i;
        return n;
    }

    // --- raw plugin bytes: pull one NAVM's NVNM subrecord out of an .esm/.esp -------------------
    // Deliberately does NOT go through Mutagen: on the vanilla side that would compare Mutagen's
    // output with Mutagen's output and prove nothing. We walk the file for the NAVM record header
    // with this FormID, zlib-decompress it if the record is flagged Compressed (vanilla navmeshes
    // are), then walk its subrecords for NVNM. A NAVM's FormID is unchanged by an override (the
    // master keeps index 0), so the same lookup works on both files.
    private const uint RecordFlagCompressed = 0x0004_0000;

    private static byte[]? Nvnm(byte[] file, uint formId)
    {
        var data = RecordData(file, "NAVM", formId);
        return data is null ? null : Subrecord(data, "NVNM");
    }

    private static byte[]? RecordData(byte[] file, string type, uint formId)
    {
        byte t0 = (byte)type[0], t1 = (byte)type[1], t2 = (byte)type[2], t3 = (byte)type[3];
        for (int i = 0; i + 24 <= file.Length; i++)
        {
            if (file[i] != t0 || file[i + 1] != t1 || file[i + 2] != t2 || file[i + 3] != t3) continue;
            if (BitConverter.ToUInt32(file, i + 12) != formId) continue;   // FormID at header+12
            uint size = BitConverter.ToUInt32(file, i + 4);                // data size (after the 24B header)
            uint flags = BitConverter.ToUInt32(file, i + 8);
            if (size == 0 || i + 24L + size > file.Length) continue;       // a chance hit inside some blob
            var body = new byte[size];
            Array.Copy(file, i + 24, body, 0, (int)size);
            if ((flags & RecordFlagCompressed) == 0) return body;

            // Compressed record: [uint32 decompressedSize][zlib stream]
            using var src = new MemoryStream(body, 4, body.Length - 4);
            using var z = new ZLibStream(src, CompressionMode.Decompress);
            using var outp = new MemoryStream((int)BitConverter.ToUInt32(body, 0));
            z.CopyTo(outp);
            return outp.ToArray();
        }
        return null;
    }

    // Subrecords are [char[4] type][uint16 size][data]. A subrecord larger than 0xFFFF is preceded by
    // an XXXX subrecord holding the real uint32 size (the next subrecord's own size field reads 0) —
    // a big cell's navmesh does exceed 64 KB, so this is not theoretical.
    private static byte[]? Subrecord(byte[] data, string type)
    {
        int p = 0;
        uint oversize = 0;
        while (p + 6 <= data.Length)
        {
            string t = Encoding.ASCII.GetString(data, p, 4);
            uint size = BitConverter.ToUInt16(data, p + 4);
            p += 6;
            if (t == "XXXX") { oversize = BitConverter.ToUInt32(data, p); p += (int)size; continue; }
            if (oversize != 0) { size = oversize; oversize = 0; }
            if (p + size > data.Length) return null;
            if (t == type)
            {
                var r = new byte[size];
                Array.Copy(data, p, r, 0, (int)size);
                return r;
            }
            p += (int)size;
        }
        return null;
    }

    // Where Skyrim.esm lives — same resolution order the generator uses (BuildOptions is not in play
    // for a diag command, so: env var, else the default Steam path).
    private static string SkyrimDataDir() =>
        Environment.GetEnvironmentVariable("MODFORGE_SKYRIM_DATA")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".local", "share", "Steam", "steamapps", "common", "Skyrim Special Edition", "Data");

    private static uint NavFormId(string s) =>
        Convert.ToUInt32(s.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
}
