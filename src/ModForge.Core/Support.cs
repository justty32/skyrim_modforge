namespace ModForge;

// UTF-8 for every language — Simplified-Chinese SSE reads UTF-8 .STRINGS (not GBK).
internal sealed class Utf8EncodingProvider : IMutagenEncodingProvider
{
    public IMutagenEncoding GetEncoding(GameRelease release, Language language) => MutagenEncoding._utf8;
}

// A translatable text slot: where it lives + accessors. extract reads Get(); apply calls Set(target).
internal sealed class Slot
{
    public string FormKey { get; }
    public string Type { get; }
    public string Field { get; }
    public int Index { get; }
    public Func<string?> Get { get; }
    public Action<string> Set { get; }

    public Slot(string formKey, string type, string field, int index, Func<string?> get, Action<string> set)
    {
        FormKey = formKey; Type = type; Field = field; Index = index; Get = get; Set = set;
    }
}

public sealed class StringEntry
{
    public string FormKey { get; set; } = "";
    public string Type { get; set; } = "";
    public string Field { get; set; } = "";
    public int Index { get; set; }
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
}
