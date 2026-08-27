internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  package step 7 — action-system loose files (OAR / BDI / PIE / SPID / MCM / FLM /
    //  KID / BOS / AOS / SkyPatcher). Non-esp config trees plus the .hkx clips they point
    //  at. Split out of Package.cs (2026-08-27).
    // -------------------------------------------------------------------------------
    private static void WriteActionSystemLooseFiles(ModSpec spec, string outModDir, string specDir,
                                                    string? assetsSrc, string pluginName)
    {
        // 7) Action-system loose-file generation (OAR / BDI / PIE) — non-esp config + asset placing.
        //    The .hkx animations are user-supplied; ModForge writes the config tree and copies the
        //    clips it can find (missing clips are reported, not silently dropped).
        if (spec.AnimationReplacers.Count > 0 || spec.BehaviorData.Count > 0 || spec.PayloadMacros.Count > 0
            || spec.SpidDistributions.Count > 0 || spec.McmConfigs.Count > 0
            || spec.FormListInjects.Count > 0 || spec.KidDistributions.Count > 0
            || spec.ObjectSwaps.Count > 0 || spec.AnimObjectSwaps.Count > 0
            || spec.SkyPatchers.Count > 0)
        {
            string? ResolveHkx(string p)
            {
                if (Path.IsPathRooted(p) && File.Exists(p)) return p;
                if (!string.IsNullOrWhiteSpace(assetsSrc))
                {
                    var a = Path.Combine(assetsSrc, p);
                    if (File.Exists(a)) return a;
                }
                var s = Path.Combine(specDir, p);
                return File.Exists(s) ? s : null;
            }
            void WriteLoose(OarGen.OarFile f) => WriteLooseFile(f, outModDir);

            int oarSubmods = 0, hkxPlaced = 0; var hkxMissing = new List<string>();
            foreach (var r in spec.AnimationReplacers)
            {
                foreach (var f in OarGen.Generate(r)) WriteLoose(f);
                oarSubmods += r.Submods.Count(s => !s.ReplaceVanillaPath);
                foreach (var copy in OarGen.HkxPlacements(r))
                {
                    var src = ResolveHkx(copy.Source);
                    if (src is null) { hkxMissing.Add(copy.Source); continue; }
                    var dest = SafeOutputPath.ResolveUnder(outModDir, copy.DestRelPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(src, dest, overwrite: true);
                    hkxPlaced++;
                }
            }
            foreach (var b in spec.BehaviorData) WriteLoose(BdiGen.Generate(b));
            foreach (var p in spec.PayloadMacros) WriteLoose(PieGen.Generate(p));
            foreach (var s in spec.SpidDistributions) WriteLoose(SpidGen.Generate(s));
            // MCM Helper keys the config folder on the host plugin's filename stem (not the spec modName).
            var mcmIdentity = Path.GetFileNameWithoutExtension(pluginName);
            foreach (var m in spec.McmConfigs) foreach (var f in McmGen.Generate(m, mcmIdentity)) WriteLoose(f);
            foreach (var fl in spec.FormListInjects) WriteLoose(FlmGen.Generate(fl));
            foreach (var k in spec.KidDistributions) WriteLoose(KidGen.Generate(k));
            foreach (var os in spec.ObjectSwaps) WriteLoose(BosGen.Generate(os));
            foreach (var ao in spec.AnimObjectSwaps) WriteLoose(AosGen.Generate(ao));
            foreach (var sp in spec.SkyPatchers) WriteLoose(SkyPatcherGen.Generate(sp));

            Console.WriteLine($"action-system: {oarSubmods} OAR submod(s), {spec.BehaviorData.Count} BDI config(s), "
                + $"{spec.PayloadMacros.Count} PIE table(s), {spec.SpidDistributions.Count} SPID ini(s), "
                + $"{spec.McmConfigs.Count} MCM config(s), {spec.FormListInjects.Count} FLM ini(s), "
                + $"{spec.KidDistributions.Count} KID ini(s), {spec.ObjectSwaps.Count} BOS ini(s), "
                + $"{spec.AnimObjectSwaps.Count} AOS ini(s), {spec.SkyPatchers.Count} SkyPatcher ini(s), {hkxPlaced} hkx placed");
            if (hkxMissing.Count > 0)
                Console.WriteLine($"  ⚠ {hkxMissing.Count} hkx not found (config written, clip missing): {string.Join(", ", hkxMissing)}");
        }
    }
}