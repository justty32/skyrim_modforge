namespace ModForge;

// Smithing / crafting depth (COBJ): named workbench selectors + recipe-kind defaults. The CTDA
// condition gating (HasPerk / GetItemCount / GetGlobalValue / TemperIsEnchanted) reuses the SHARED
// condition builder (Generator.Build.Conditions.cs / ConditionSpec) rather than a feature-local one.
// FormIDs were discovered from Skyrim.esm via `cobjdiag` (CLI) — see docs/SPEC.md, never invented.
public static partial class Generator
{
    // Named workbench selector -> vanilla CraftingSmithing* keyword "<master>:0xID". Discovered via
    // `find Skyrim.esm CraftingSmithing Keyword` / `cobjdiag` on vanilla recipes.
    private static readonly Dictionary<string, string> WorkbenchByName =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["forge"]           = "Skyrim.esm:0x088105",  // CraftingSmithingForge
        ["sharpeningWheel"] = "Skyrim.esm:0x088108",  // CraftingSmithingSharpeningWheel (weapon temper)
        ["grindstone"]      = "Skyrim.esm:0x088108",  // alias of sharpeningWheel
        ["armorTable"]      = "Skyrim.esm:0x0ADB78",  // CraftingSmithingArmorTable (armor temper)
        ["workbench"]       = "Skyrim.esm:0x0ADB78",  // alias of armorTable
        ["smelter"]         = "Skyrim.esm:0x0A5CCE",  // CraftingSmelter
        ["tanningRack"]     = "Skyrim.esm:0x07866A",  // CraftingTanningRack
        ["skyforge"]        = "Skyrim.esm:0x0F46CE",  // CraftingSmithingSkyforge
    };

    private const string ForgeKeyword          = "Skyrim.esm:0x088105";
    private const string SharpeningWheelKeyword = "Skyrim.esm:0x088108";
    private const string SmelterKeyword         = "Skyrim.esm:0x0A5CCE";

    // The bench a recipe uses when `workbench` is empty, chosen by kind. Temper defaults to the
    // sharpening wheel (the common weapon-temper bench; armor authors set workbench: armorTable).
    private static string DefaultWorkbenchFor(string kind) => NormalizeKind(kind) switch
    {
        "temper"               => SharpeningWheelKeyword,
        "smelt" or "breakdown" => SmelterKeyword,
        _                      => ForgeKeyword,
    };

    // Resolve a recipe's `workbench` field to a keyword ref: a named selector -> vanilla keyword,
    // else a raw ref (in-spec/external) passed through, else (empty) the kind default.
    internal static string ResolveWorkbenchRef(string kind, string workbench)
    {
        if (string.IsNullOrWhiteSpace(workbench)) return DefaultWorkbenchFor(kind);
        return WorkbenchByName.TryGetValue(workbench.Trim(), out var kw) ? kw : workbench;
    }

    internal static string NormalizeKind(string kind) =>
        string.IsNullOrWhiteSpace(kind) ? "craft" : kind.Trim().ToLowerInvariant();

    internal static readonly string[] KnownRecipeKinds = { "craft", "temper", "smelt", "breakdown" };
    internal static IReadOnlyCollection<string> KnownWorkbenchNames => WorkbenchByName.Keys;

    // The condition functions a recipe gate may use (a subset of the shared BuildCondition set, the
    // ones that make sense on a COBJ). TemperIsEnchanted takes no form arg; the rest need a `param`.
    private static readonly Dictionary<string, bool> RecipeCondFns =   // function -> needs a param ref
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["hasperk"] = true, ["getitemcount"] = true, ["getglobalvalue"] = true,
        ["temperisenchanted"] = false, ["eptemperingitemisenchanted"] = false,
    };
    internal static bool IsKnownRecipeFunction(string fn) => RecipeCondFns.ContainsKey((fn ?? "").Trim());
    internal static bool RecipeFunctionNeedsRef(string fn) =>
        RecipeCondFns.TryGetValue((fn ?? "").Trim(), out var needs) && needs;

    // CTDA comparison token -> bool valid. Accepts the symbol forms the shared ConditionSpec uses.
    internal static bool IsValidCompareOp(string op) => (op ?? "").Trim() switch
    {
        "==" or "=" or "!=" or ">" or ">=" or "<" or "<=" => true,
        _ => false,
    };
}
