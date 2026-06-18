using System.Diagnostics;
using Mutagen.Bethesda.Archives;
using Noggog;

internal static partial class Program
{
    // texexport — resolve LandscapeTexture (LTEX) FormID(s) to their diffuse .dds, pull the .dds
    // out of the game's texture BSAs, and convert each to a PNG named "<master>_<id>.png" in <outDir>.
    // Feeds the Godot worldspace-editor's WYSIWYG terrain shader: the editor calls this so a layer's
    // real vanilla ground texture (dirt/grass/…) renders under the splat brush instead of a flat tint.
    //   texexport <dataDir> <outDir> <master:0xFORMID>[,<master:0xFORMID>…]
    private static int TexExport(string dataDir, string outDir, string refsCsv)
    {
        Directory.CreateDirectory(outDir);
        var bsas = Directory.GetFiles(dataDir, "*.bsa")
            .Where(p => Path.GetFileName(p).Contains("Texture", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (bsas.Count == 0) { Console.Error.WriteLine($"no *Texture*.bsa under {dataDir}"); return 2; }

        // Cache one overlay per master so a CSV of refs from the same plugin opens it once.
        var mods = new Dictionary<string, ISkyrimModGetter>(StringComparer.OrdinalIgnoreCase);
        int ok = 0, total = 0;
        foreach (var raw in refsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            total++;
            var parts = raw.Split(':');
            if (parts.Length != 2) { Console.Error.WriteLine($"bad ref '{raw}' (want master:0xFORMID)"); continue; }
            var master = parts[0];
            uint id = Convert.ToUInt32(parts[1].Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;

            if (!mods.TryGetValue(master, out var mod))
            {
                var mpath = Path.Combine(dataDir, master);
                if (!File.Exists(mpath)) { Console.Error.WriteLine($"master not found: {mpath}"); continue; }
                mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(mpath), SkyrimRelease.SkyrimSE);
                mods[master] = mod;
            }

            // LTEX → TextureSet → diffuse .dds path (e.g. "Landscape\FieldGrass01.dds").
            var ltex = mod.LandscapeTextures.FirstOrDefault(l => l.FormKey.ID == id);
            if (ltex is null) { Console.Error.WriteLine($"0x{id:X6} not a LandscapeTexture in {master}"); continue; }
            var txstFk = ltex.TextureSet.FormKey;
            var txst = mod.TextureSets.FirstOrDefault(t => t.FormKey == txstFk);
            var diffuse = txst?.Diffuse?.GivenPath;
            if (string.IsNullOrEmpty(diffuse)) { Console.Error.WriteLine($"0x{id:X6} ({ltex.EditorID}) has no diffuse texture"); continue; }

            // BSA stores textures under "textures\<diffuse>"; match case-insensitively on the tail.
            var bsaTail = ("textures\\" + diffuse).Replace('/', '\\');
            var ddsPng = Path.Combine(outDir, $"{master}_0x{id:X6}.png");
            if (!ExtractAndConvert(bsas, bsaTail, ddsPng))
            { Console.Error.WriteLine($"0x{id:X6} ({ltex.EditorID}) diffuse '{diffuse}' not found in texture BSAs"); continue; }

            Console.WriteLine($"{master}:0x{id:X6}  {ltex.EditorID,-22} -> {ddsPng}  ({diffuse})");
            ok++;
        }
        Console.WriteLine($"-- {ok}/{total} texture(s) exported to {outDir}");
        return ok == total ? 0 : 1;
    }

    // Pull one .dds out of whichever texture BSA holds it, convert to PNG via ImageMagick.
    private static bool ExtractAndConvert(List<string> bsas, string bsaTail, string outPng)
    {
        foreach (var bsa in bsas)
        {
            var reader = Archive.CreateReader(GameRelease.SkyrimSE, bsa, IFileSystemExt.DefaultFilesystem);
            var hit = reader.Files.FirstOrDefault(f =>
                f.Path.EndsWith(bsaTail, StringComparison.OrdinalIgnoreCase) ||
                f.Path.Replace('/', '\\').EndsWith(bsaTail, StringComparison.OrdinalIgnoreCase));
            if (hit is null) continue;

            var tmpDds = Path.Combine(Path.GetTempPath(), $"mf_tex_{Guid.NewGuid():N}.dds");
            using (var src = hit.AsStream())
            using (var dst = File.Create(tmpDds))
                src.CopyTo(dst);

            var psi = new ProcessStartInfo
            {
                FileName = "magick",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            // `magick <in.dds> <out.png>` — flattens mips, decodes BC compression.
            psi.ArgumentList.Add(tmpDds);
            psi.ArgumentList.Add(outPng);
            using var proc = Process.Start(psi);
            proc?.WaitForExit();
            try { File.Delete(tmpDds); } catch { /* temp cleanup best-effort */ }
            return proc?.ExitCode == 0 && File.Exists(outPng);
        }
        return false;
    }
}
