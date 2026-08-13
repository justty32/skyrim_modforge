using System.Text.Json.Serialization;

namespace ModForge;

// -------------------------------------------------------------------------------
//  removals[] entries — a "<master>:0xFORMID" ref of an EXISTING placed ref to disable
//  (see Generator.Build.Removals.cs). A BARE STRING is shorthand for {"ref": "<that>"}; the
//  object form additionally carries a free-form `label`/`note` for the agent reading the scene
//  json back later. Both are INERT documentation — the build never changes behaviour because of
//  them (Generator.Build.Removals.cs only ever reads `.Ref`).
// -------------------------------------------------------------------------------
[JsonConverter(typeof(RemovalConverter))]
public sealed class RemovalSpec
{
    /// <summary>"<master>:0xFORMID" of the existing placed ref (REFR/ACHR) to disable + bury.</summary>
    public string Ref { get; set; } = "";

    /// <summary>Short human label for the removed thing ("the barrel"). Documentation only.</summary>
    public string Label { get; set; } = "";

    /// <summary>Free-form note (why it was removed). Documentation only.</summary>
    public string Note { get; set; } = "";

    /// <summary>Lets existing C# collection-initializers (<c>Removals = { "Skyrim.esm:0x..." }</c>) keep compiling.</summary>
    public static implicit operator RemovalSpec(string refStr) => new() { Ref = refStr };
}

/// <summary>
/// Accepts <c>"removals": ["Skyrim.esm:0x0D1991"]</c> (the shorthand) as well as the object form
/// carrying <c>label</c>/<c>note</c>. Write() collapses back to the bare string when neither is set.
/// </summary>
public sealed class RemovalConverter : JsonConverter<RemovalSpec>
{
    public override RemovalSpec? Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return null;
        if (r.TokenType == JsonTokenType.String) return new RemovalSpec { Ref = r.GetString() ?? "" };

        var inner = new JsonSerializerOptions(o);
        for (int i = inner.Converters.Count - 1; i >= 0; i--)
            if (inner.Converters[i] is RemovalConverter) inner.Converters.RemoveAt(i);
        using var doc = JsonDocument.ParseValue(ref r);
        return doc.RootElement.Deserialize<RemovalBody>(inner) is { } b
            ? new RemovalSpec { Ref = b.Ref, Label = b.Label, Note = b.Note }
            : null;
    }

    // Round-trips to the shorthand when there is nothing else to say.
    public override void Write(Utf8JsonWriter w, RemovalSpec v, JsonSerializerOptions o)
    {
        if (v.Label.Length == 0 && v.Note.Length == 0)
        {
            w.WriteStringValue(v.Ref);
            return;
        }
        w.WriteStartObject();
        if (v.Ref.Length > 0) w.WriteString("ref", v.Ref);
        if (v.Label.Length > 0) w.WriteString("label", v.Label);
        if (v.Note.Length > 0) w.WriteString("note", v.Note);
        w.WriteEndObject();
    }

    // Plain mirror of RemovalSpec, free of the converter attribute (else Read recurses).
    private sealed class RemovalBody
    {
        public string Ref { get; set; } = "";
        public string Label { get; set; } = "";
        public string Note { get; set; } = "";
    }
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<RemovalSpec> Removals { get; set; } = new(); // refs "<master>:0xFORMID" of EXISTING vanilla placed refs to remove (disable + bury); a bare string, or an object carrying an optional `label`/`note` (inert documentation — see Spec.Removals.cs). The in-game eraser spell (Idea #24 §E) feeds this. See Generator.Build.Removals.cs
}
