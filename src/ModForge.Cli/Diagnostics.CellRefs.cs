internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  cellrefs — dump ONE interior cell's placed references, to reverse a vanilla cell's
    //  object layout into a ModForge placements[] spec. Same lazy-overlay memory discipline
    //  as the other targeted diagnostics (Diagnostics.Records.cs): a 250 MB master is NOT
    //  fully materialized — we walk the CELL block tree lazily and, the instant the target
    //  FormID is found, parse ONLY that cell's child group (its Temporary + Persistent lists,
    //  a few hundred refs) and return. NEVER enumerate every cell's children.
    // -------------------------------------------------------------------------------

    // base FormKey + cell-LOCAL position + rotation (RADIANS, as stored) + scale, as CSV
    // (kind,base,px,py,pz,rx,ry,rz,scale,editorId). ModForge's placements[] rotation is in
    // DEGREES, so the spec value is the printed radians * 180/pi. Does not resolve localized Name.
    private static int CellRefs(string inPath, string formIdHex)
    {
        uint target = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var block in mod.Cells.Records)
        foreach (var sub in block.SubBlocks)
        foreach (var c in sub.Cells)
        {
            if (c.FormKey.ID != target) continue;
            Console.WriteLine($"# cell 0x{target:X6} {c.EditorID}  (position = cell-local; rotation = RADIANS)");
            Console.WriteLine("kind,base,posX,posY,posZ,rotX,rotY,rotZ,scale,editorId");
            int obj = 0, npc = 0, skipped = 0;
            // Persistent first, then Temporary — both bounded to THIS cell's child group.
            DumpPlacedGroup(c.Persistent, "P", ref obj, ref npc, ref skipped);
            DumpPlacedGroup(c.Temporary, "T", ref obj, ref npc, ref skipped);
            Console.WriteLine($"# {obj} placed object(s), {npc} placed npc(s), {skipped} disabled-skipped");
            return 0;
        }
        Console.WriteLine($"0x{target:X6} not found as an interior cell in {Path.GetFileName(inPath)}");
        return 0;
    }

    // Print every placed REFR/ACHR in one cell child list. `grp` tags Persistent (P) vs Temporary (T).
    // Skips refs flagged Initially-Disabled (those aren't part of the visible layout).
    private static void DumpPlacedGroup(
        IReadOnlyList<IPlacedGetter> placed, string grp, ref int obj, ref int npc, ref int skipped)
    {
        foreach (var r in placed)
        {
            bool disabled = (r.MajorRecordFlagsRaw & 0x800) != 0; // InitiallyDisabled
            if (disabled) { skipped++; continue; }
            switch (r)
            {
                case IPlacedObjectGetter o:
                    obj++;
                    EmitRef($"obj{grp}", o.Base.FormKey, o.Placement, o.Scale, o.EditorID);
                    break;
                case IPlacedNpcGetter a:
                    npc++;
                    EmitRef($"npc{grp}", a.Base.FormKey, a.Placement, a.Scale, a.EditorID);
                    break;
            }
        }
    }

    private static void EmitRef(string kind, FormKey baseFk, IPlacementGetter? pl, float? scale, string? edid)
    {
        var p = pl?.Position; var ro = pl?.Rotation;
        Console.WriteLine($"{kind},{baseFk},"
            + $"{p?.X ?? 0:0.####},{p?.Y ?? 0:0.####},{p?.Z ?? 0:0.####},"
            + $"{ro?.X ?? 0:0.######},{ro?.Y ?? 0:0.######},{ro?.Z ?? 0:0.######},"
            + $"{scale ?? 1f:0.###},{edid ?? "-"}");
    }
}
