using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Headers;
using SkyrimRecordTypes = Mutagen.Bethesda.Skyrim.Internals.RecordTypes;

namespace ModForge;

public enum InstalledDependencyState { Enabled, Implicit, Disabled, Missing }

public sealed record InstalledDependency(string Plugin, InstalledDependencyState State);

public sealed record InstalledDependencyCheck(IReadOnlyList<InstalledDependency> Dependencies)
{
    public bool Satisfied => Dependencies.All(x =>
        x.State is InstalledDependencyState.Enabled or InstalledDependencyState.Implicit);
}

public sealed record PluginListing(
    IReadOnlySet<string> Enabled,
    IReadOnlySet<string> Disabled)
{
    public static PluginListing ReadPluginsFile(string path) => Parse(File.ReadAllLines(path), starred: true);

    public static PluginListing ReadImplicitFile(string path) => Parse(File.ReadAllLines(path), starred: false);

    internal static PluginListing Parse(IEnumerable<string> lines, bool starred)
    {
        var enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var disabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourceLine in lines)
        {
            var line = sourceLine.Trim().TrimStart('\uFEFF').Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            bool active = !starred || line.StartsWith('*');
            var name = active && starred ? line[1..].Trim() : line;
            ValidatePluginName(name);

            var destination = active ? enabled : disabled;
            var opposite = active ? disabled : enabled;
            if (opposite.Contains(name))
                throw new InvalidDataException($"plugin '{name}' appears as both enabled and disabled");
            destination.Add(name);
        }
        return new PluginListing(enabled, disabled);
    }

    private static void ValidatePluginName(string name)
    {
        const string windowsInvalid = "<>:\"/\\|?*";
        var stem = Path.GetFileNameWithoutExtension(name);
        if (name.Length == 0 || stem.Length == 0 || name.Any(char.IsControl)
            || name.IndexOfAny(windowsInvalid.ToCharArray()) >= 0
            || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidDataException($"invalid plugin listing entry '{name}'");
        try
        {
            var key = ModKey.FromNameAndExtension(name);
            if (key.Type is not (ModType.Master or ModType.Plugin or ModType.Light))
                throw new InvalidDataException($"invalid plugin listing entry '{name}'");
        }
        catch (ArgumentException e)
        {
            throw new InvalidDataException($"invalid plugin listing entry '{name}'", e);
        }
    }
}

public static class InstalledDependencies
{
    public static IReadOnlyList<string> ReadDirectMasters(string pluginPath)
    {
        var path = new ModPath(pluginPath);
        var header = ModHeaderFrame.FromPath(path, GameRelease.SkyrimSE);
        if (header.RecordType != SkyrimRecordTypes.TES4)
            throw new InvalidDataException($"'{pluginPath}' does not start with a TES4 plugin header");
        ValidateHeaderStructure(header, pluginPath);
        return header.Masters(path.ModKey).Select(x => x.Master.FileName.String).ToArray();
    }

    private static void ValidateHeaderStructure(ModHeaderFrame header, string pluginPath)
    {
        var subrecords = header.EnumerateSubrecords().ToArray();
        if (subrecords.Length == 0 || subrecords[0].RecordType != SkyrimRecordTypes.HEDR
            || subrecords[0].ContentLength != 12
            || subrecords.Count(x => x.RecordType == SkyrimRecordTypes.HEDR) != 1)
            throw new InvalidDataException($"'{pluginPath}' has no valid 12-byte HEDR subrecord");

        for (int i = 0; i < subrecords.Length; i++)
        {
            if (subrecords[i].RecordType == SkyrimRecordTypes.MAST)
            {
                if (i + 1 >= subrecords.Length || subrecords[i + 1].RecordType != SkyrimRecordTypes.DATA
                    || subrecords[i + 1].ContentLength != 8)
                    throw new InvalidDataException($"'{pluginPath}' has a MAST without its 8-byte DATA subrecord");
            }
            else if (subrecords[i].RecordType == SkyrimRecordTypes.DATA
                && (i == 0 || subrecords[i - 1].RecordType != SkyrimRecordTypes.MAST))
            {
                throw new InvalidDataException($"'{pluginPath}' has an orphan DATA master-size subrecord");
            }
        }
    }

    public static InstalledDependencyCheck Check(
        IEnumerable<string> directMasters,
        PluginListing plugins,
        PluginListing? implicitPlugins = null)
    {
        var rows = directMasters.Select(master => new InstalledDependency(master,
            implicitPlugins?.Enabled.Contains(master) == true ? InstalledDependencyState.Implicit
            : plugins.Enabled.Contains(master) ? InstalledDependencyState.Enabled
            : plugins.Disabled.Contains(master) ? InstalledDependencyState.Disabled
            : InstalledDependencyState.Missing)).ToArray();
        return new InstalledDependencyCheck(rows);
    }
}
