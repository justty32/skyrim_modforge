internal static partial class Program
{
    // Diagnostic: print a TextureSet's eight texture-map slots + flags (one 0xFORMID) — or, with no
    // FormID, list every TXST in the plugin (editorId + diffuse path). Use it to (a) learn how a
    // vanilla TXST fills its slots before authoring a retexture, and (b) verify a GENERATED TXST got
    // the right Data-relative paths without an in-game cycle (texture CONTENT/rendering is NOT
    // verifiable headless — this only confirms the references/paths the .esp stores).
    private static int TxstDiag(string inPath, string? formIdHex)
    {
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        uint? target = formIdHex is null ? null
            : Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        int shown = 0;
        foreach (var t in mod.EnumerateMajorRecords<ITextureSetGetter>())
        {
            if (target is { } id) { if (t.FormKey.ID != id) continue; }
            else
            {
                if (shown++ >= 60) { Console.WriteLine("…(capped at 60; pass a 0xFORMID for one TXST's full slots)"); break; }
                Console.WriteLine($"0x{t.FormKey.ID:X6}  {t.EditorID,-34} diffuse={t.Diffuse?.GivenPath ?? "-"}");
                continue;
            }
            string S(IAssetLinkGetter? a) => a?.GivenPath ?? "-";
            Console.WriteLine($"0x{t.FormKey.ID:X6}  EditorID={t.EditorID}");
            Console.WriteLine($"  Flags = {t.Flags?.ToString() ?? "-"}");
            Console.WriteLine($"  [0] diffuse     = {S(t.Diffuse)}");
            Console.WriteLine($"  [1] normal      = {S(t.NormalOrGloss)}");
            Console.WriteLine($"  [2] mask        = {S(t.EnvironmentMaskOrSubsurfaceTint)}");
            Console.WriteLine($"  [3] glow        = {S(t.GlowOrDetailMap)}");
            Console.WriteLine($"  [4] height      = {S(t.Height)}");
            Console.WriteLine($"  [5] environment = {S(t.Environment)}");
            Console.WriteLine($"  [6] multilayer  = {S(t.Multilayer)}");
            Console.WriteLine($"  [7] backlight   = {S(t.BacklightMaskOrSpecular)}");
            return 0;
        }
        if (target is { } tid) Console.WriteLine($"0x{tid:X6} not a TextureSet in {Path.GetFileName(inPath)}");
        return 0;
    }
}
