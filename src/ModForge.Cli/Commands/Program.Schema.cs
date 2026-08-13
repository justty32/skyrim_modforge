using System.Reflection;
using System.Text.Json.Serialization;

internal static partial class Program
{
    // Walk a JSON document against the C# type tree and collect any key that has no
    // corresponding property on the expected type.  Catches field-name typos that
    // System.Text.Json would otherwise silently swallow.
    internal static List<string> CheckUnknownFields(string json, Type rootType)
    {
        var problems = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            WalkElement(doc.RootElement, rootType, "$", problems);
        }
        catch (JsonException) { } // malformed JSON — the deserialiser will report it
        return problems;
    }

    private static void WalkElement(JsonElement el, Type type, string path, List<string> problems)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (el.ValueKind == JsonValueKind.Array)
        {
            var elemType = ListElementType(type);
            if (elemType == null) return;
            var i = 0;
            foreach (var item in el.EnumerateArray())
                WalkElement(item, elemType, $"{path}[{i++}]", problems);
            return;
        }

        if (el.ValueKind != JsonValueKind.Object) return;
        if (IsDictType(type)) return; // arbitrary keys are valid in Dictionary<,>

        var known = KnownProps(type);
        foreach (var kv in el.EnumerateObject())
        {
            if (kv.Name.StartsWith('_') || kv.Name.StartsWith("//")) continue; // inline comment conventions
            if (!known.TryGetValue(kv.Name, out var propType))
            {
                problems.Add($"unknown spec field '{kv.Name}' at {path} — check for typos");
                continue;
            }
            WalkElement(kv.Value, propType, $"{path}.{kv.Name}", problems);
        }
    }

    // Build a case-insensitive name→Type map for all public instance properties of a type,
    // honouring [JsonPropertyName] and skipping [JsonIgnore].
    private static Dictionary<string, Type> KnownProps(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
            .Select(p => (
                Name: p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name,
                p.PropertyType))
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().PropertyType, StringComparer.OrdinalIgnoreCase);

    private static Type? ListElementType(Type t)
    {
        if (t.IsArray) return t.GetElementType();
        if (t.IsGenericType) return t.GetGenericArguments().FirstOrDefault();
        return null;
    }

    private static bool IsDictType(Type t) =>
        t.IsGenericType && t.GetGenericTypeDefinition() is { } def &&
        (def == typeof(Dictionary<,>) || def == typeof(SortedDictionary<,>));
}
