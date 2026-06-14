using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ModForge;

// Generates the OAR replacer-mod folder tree as loose files: root config.json + per-submod
// config.json + the list of .hkx files to place. Pure functions (no I/O) — package writes them.
// Layout verified against real configs (Holmgang/NAMC/BFCO):
//   Meshes/actors/character/animations/OpenAnimationReplacer/<Mod>/config.json   (root)
//   Meshes/.../OpenAnimationReplacer/<Mod>/<Submod>/config.json + <clip>.hkx     (submod)
public static class OarGen
{
    // One generated text file: forward-slash RelPath under the mod folder + its content.
    public record OarFile(string RelPath, string Content);
    // One .hkx to copy: the user's source path (rel. to assets/spec dir) + dest RelPath under the mod folder.
    public record HkxCopy(string Source, string DestRelPath);

    private const string OarBase = "Meshes/actors/character/animations/OpenAnimationReplacer";
    private static readonly JsonSerializerOptions Pretty =
        new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // Windows-illegal filename chars (OAR folder names may contain spaces/&; only these are stripped).
    private static string SanitizeFolder(string name)
    {
        var chars = (name ?? "").Trim();
        foreach (var bad in new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' })
            chars = chars.Replace(bad, '_');
        return chars.Length == 0 ? "Submod" : chars;
    }

    private static string BaseName(string path) =>
        (path ?? "").Replace('\\', '/').TrimEnd('/').Split('/').LastOrDefault() ?? "";

    public static List<OarFile> Generate(AnimationReplacerSpec r)
    {
        var files = new List<OarFile>();
        var modDir = $"{OarBase}/{SanitizeFolder(r.Mod)}";

        // root config — name/author/description only (no priority/conditions).
        var root = new JsonObject { ["name"] = r.Mod };
        if (!string.IsNullOrEmpty(r.Author)) root["author"] = r.Author;
        root["description"] = r.Description ?? "";
        files.Add(new OarFile($"{modDir}/config.json", root.ToJsonString(Pretty)));

        foreach (var s in r.Submods)
        {
            if (s.ReplaceVanillaPath) continue; // plain replacer: no OAR config (hkx placed at vanilla path)

            var conditions = s.NpcMoveset is not null
                ? OarConditions.Expand(s.NpcMoveset)
                : s.Conditions;

            var sub = new JsonObject
            {
                ["name"] = s.Name,
                ["description"] = s.Description ?? "",
                ["priority"] = s.Priority,
                ["conditions"] = OarConditions.EmitAll(conditions),
            };
            files.Add(new OarFile($"{modDir}/{SanitizeFolder(s.Name)}/config.json", sub.ToJsonString(Pretty)));
        }
        return files;
    }

    public static List<HkxCopy> HkxPlacements(AnimationReplacerSpec r)
    {
        var copies = new List<HkxCopy>();
        var modDir = $"{OarBase}/{SanitizeFolder(r.Mod)}";

        foreach (var s in r.Submods)
        {
            // Plain vanilla-path replacer: drop the clip at the vanilla path under Meshes/, no OAR block.
            if (s.ReplaceVanillaPath)
            {
                foreach (var hkx in s.Hkx)
                    copies.Add(new HkxCopy(hkx, $"Meshes/{(s.Replaces.Length > 0 ? s.Replaces : BaseName(hkx))}".Replace('\\', '/')));
                continue;
            }

            var subDir = $"{modDir}/{SanitizeFolder(s.Name)}";

            // Main clips: placed directly in the submod folder. With exactly one clip + a Replaces
            // target, rename to the vanilla basename; otherwise keep the source basename (author-named).
            if (s.Hkx.Count == 1 && s.Replaces.Length > 0)
                copies.Add(new HkxCopy(s.Hkx[0], $"{subDir}/{BaseName(s.Replaces)}"));
            else
                foreach (var hkx in s.Hkx)
                    copies.Add(new HkxCopy(hkx, $"{subDir}/{BaseName(hkx)}"));

            // Variants: _variants_<animName>/ with numeric filenames (OAR 1.2.0+).
            if (s.Variants.Count > 0)
            {
                var animName = s.Replaces.Length > 0 ? BaseName(s.Replaces) : BaseName(s.Hkx.FirstOrDefault() ?? "anim.hkx");
                var stem = animName.Contains('.') ? animName[..animName.LastIndexOf('.')] : animName;
                int i = 1;
                foreach (var v in s.Variants)
                    copies.Add(new HkxCopy(v, $"{subDir}/_variants_{stem}/{i++}.hkx"));
            }
        }
        return copies;
    }
}
