using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Plugins.Aspects;
using Noggog;

internal static partial class Program
{
    // nifexport — resolve a placeable base record (STAT/TREE/MSTT/FURN/ACTI/CONT/FLOR/…) to its
    // model .nif, and pull that .nif out of the game's mesh BSAs into <outDir>/<master>_<id>.nif.
    // The C# half of the Godot worldspace-editor's WYSIWYG object pipeline: the editor then runs
    // nif2gltf on the extracted .nif to show the real mesh in place of a box proxy.
    //   nifexport <dataDir> <outDir> <master:0xFORMID>[,…]
    private static int NifExport(string dataDir, string outDir, string refsCsv)
    {
        Directory.CreateDirectory(outDir);
        var bsas = Directory.GetFiles(dataDir, "*.bsa")
            .Where(p => Path.GetFileName(p).Contains("Mesh", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (bsas.Count == 0) { Console.Error.WriteLine($"no *Mesh*.bsa under {dataDir}"); return 2; }

        // Parse refs, grouped by master so each plugin is enumerated once.
        var wanted = new Dictionary<string, Dictionary<uint, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in refsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = raw.Split(':');
            if (parts.Length != 2) { Console.Error.WriteLine($"bad ref '{raw}' (want master:0xFORMID)"); continue; }
            uint id = Convert.ToUInt32(parts[1].Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
            (wanted.TryGetValue(parts[0], out var m) ? m : wanted[parts[0]] = new())[id] = parts[0];
        }

        int ok = 0, total = 0;
        foreach (var (master, ids) in wanted)
        {
            var mpath = Path.Combine(dataDir, master);
            if (!File.Exists(mpath)) { Console.Error.WriteLine($"master not found: {mpath}"); total += ids.Count; continue; }
            using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(mpath), SkyrimRelease.SkyrimSE);

            // One enumeration resolves every requested FormID's model path.
            var models = new Dictionary<uint, string?>();
            foreach (var r in mod.EnumerateMajorRecords())
            {
                if (!ids.ContainsKey(r.FormKey.ID)) continue;
                models[r.FormKey.ID] = (r as IModeledGetter)?.Model?.File.GivenPath;
                if (models.Count == ids.Count) break;
            }

            foreach (var id in ids.Keys)
            {
                total++;
                if (!models.TryGetValue(id, out var model) || string.IsNullOrEmpty(model))
                { Console.Error.WriteLine($"{master}:0x{id:X6} has no model (.nif) — not a placeable static?"); continue; }

                // BSA path = the model path, backslashed (vanilla model paths already start with "Meshes\").
                var bsaTail = model.Replace('/', '\\');
                if (!bsaTail.StartsWith("meshes\\", StringComparison.OrdinalIgnoreCase))
                    bsaTail = "meshes\\" + bsaTail;
                var outNif = Path.Combine(outDir, $"{master}_0x{id:X6}.nif");
                if (!ExtractFirst(bsas, bsaTail, outNif))
                { Console.Error.WriteLine($"{master}:0x{id:X6} model '{model}' not found in mesh BSAs"); continue; }

                Console.WriteLine($"{master}:0x{id:X6} -> {outNif}  ({model})");
                ok++;
            }
        }
        Console.WriteLine($"-- {ok}/{total} nif(s) extracted to {outDir}");
        return ok == total ? 0 : 1;
    }

    // Extract the first BSA file whose path matches <bsaTail> (full backslashed tail) to <outFile>.
    private static bool ExtractFirst(List<string> bsas, string bsaTail, string outFile)
    {
        foreach (var bsa in bsas)
        {
            var reader = Archive.CreateReader(GameRelease.SkyrimSE, bsa, IFileSystemExt.DefaultFilesystem);
            var hit = reader.Files.FirstOrDefault(f =>
                f.Path.Replace('/', '\\').EndsWith(bsaTail, StringComparison.OrdinalIgnoreCase));
            if (hit is null) continue;
            using var src = hit.AsStream();
            using var dst = File.Create(outFile);
            src.CopyTo(dst);
            return true;
        }
        return false;
    }
}
