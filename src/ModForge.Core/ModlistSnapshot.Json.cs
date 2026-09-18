using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModForge;

public sealed partial record ModlistSnapshot
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
    };

    public string ToJson()
    {
        Validate();
        return JsonSerializer.Serialize(this, JsonOptions) + "\n";
    }

    public static ModlistSnapshot FromJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        RejectDuplicateProperties(document.RootElement);
        var snapshot = JsonSerializer.Deserialize<ModlistSnapshot>(json, JsonOptions)
            ?? throw new InvalidDataException("snapshot must be an object");
        snapshot.Validate();
        return snapshot;
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new InvalidDataException($"duplicate JSON property '{property.Name}'");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray()) RejectDuplicateProperties(child);
    }

    private void Validate()
    {
        if (SchemaVersion != 1) throw new InvalidDataException("unsupported snapshot schemaVersion");
        if (Sources is null || Mods is null || Plugins is null)
            throw new InvalidDataException("missing snapshot sources or entries");
        foreach (var source in new[] { Sources.Modlist, Sources.Plugins, Sources.Loadorder })
            if (source is null || source.LineCount < 0 || source.Sha256 is null
                || source.Sha256.Length != 64 || source.Sha256.Any(c => !char.IsAsciiHexDigitLower(c)))
                throw new InvalidDataException("invalid snapshot source hash or line count");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < Mods.Count; i++)
        {
            var mod = Mods[i];
            if (mod is null || string.IsNullOrWhiteSpace(mod.Name) || mod.Name.Any(char.IsControl)
                || mod.PriorityIndex != i || !names.Add(mod.Name))
                throw new InvalidDataException("invalid mod name, duplicate or priority index");
        }
        names.Clear();
        var indices = new List<int>();
        foreach (var plugin in Plugins)
        {
            if (plugin is null || string.IsNullOrWhiteSpace(plugin.Name) || !names.Add(plugin.Name)
                || plugin.LoadOrderIndex < 0 || (plugin.Enabled is null && plugin.LoadOrderIndex is null))
                throw new InvalidDataException("invalid plugin entry, duplicate or load order index");
            _ = PluginListing.Parse(new[] { plugin.Name }, starred: false);
            if (plugin.LoadOrderIndex is { } index) indices.Add(index);
        }
        if (!indices.Order().SequenceEqual(Enumerable.Range(0, indices.Count)))
            throw new InvalidDataException("load order indices must be unique and contiguous from zero");
    }
}
