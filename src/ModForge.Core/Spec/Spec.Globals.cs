namespace ModForge;

// A GlobalVariable (GLOB): one named number shared across the WHOLE game, persisted in the save and
// readable by CONDITIONS with zero Papyrus (GetGlobalValue) as well as by Papyrus (GetValue/SetValue/Mod)
// and the console (set/show). Dialogue `setGlobal` emits a result fragment that can mutate one at
// runtime. The plugin's `value` is the INITIAL value only — an existing save keeps
// its own runtime value (the classic GLOB save gotcha; a new game picks up the new initial value).
//
// Use as: a FLAG / re-arm token (0/1 — set after an event, cleared by another to re-enable it), a
// COUNTER / reputation score, a chance/weight (region/leveled-list), or a tuning CONSTANT (mark `constant`
// for read-only).
public sealed class GlobalSpec
{
    public string EditorId { get; set; } = "";
    // short | long | float. Skyrim stores every global as a float on disk; the type truncates on read
    // (short/long = integer). "int" is accepted as an alias for "long". Default short (the common flag/counter).
    public string Type { get; set; } = "short";
    public float Value { get; set; }       // initial value
    public bool Constant { get; set; }     // Constant major flag — a read-only tuning value (can't be SetValue'd)
}
