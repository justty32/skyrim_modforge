namespace ModForge;

// MCM Helper (Parapets, Nexus 53000) settings-menu config spec DTOs.
// Produces TWO LOOSE FILES (no esp record) under the mod folder:
//   MCM/Config/<modName>/config.json    (required — the menu layout)
//   MCM/Config/<modName>/settings.ini   (the mod's default values)
// MVP is the ini-backed path: controls whose sourceType is ModSettingBool/Int/Float/String are
// fully handled by the MCMHelper.dll with NO Papyrus and NO Quest record — the player's edits land
// in MCM/Settings/<modName>.ini at runtime. The advanced PropertyValue*/action.CallFunction path
// (which DOES need a Quest script extending MCM_ConfigBase) is intentionally out of scope here.
// Format verified against sub_projs/mod-survey/findings/mcm-helper-config-json.md (MCM Helper 1.6.1).
public sealed class McmSpec
{
    public string ModName { get; set; } = "";       // → MCM/Config/<ModName>/ dir + the MCM identity key
    public string DisplayName { get; set; } = "";    // left-list label (supports a $TranslationKey)
    public List<McmPageSpec> Pages { get; set; } = new();
}

public sealed class McmPageSpec
{
    public string Name { get; set; } = "";           // pageDisplayName (supports a $TranslationKey)
    public string CursorFillMode { get; set; } = ""; // "" → topToBottom (default) | leftToRight (two-column)
    public List<McmControlSpec> Content { get; set; } = new();
}

// One control. `type` ∈ toggle|slider|stepper|enum|keymap|header|empty|hiddenToggle.
// header/empty carry no value (no id/sourceType). Value controls need `id` ("key:Section") + a
// `sourceType`; their default goes into config.json valueOptions.defaultValue AND settings.ini.
public sealed class McmControlSpec
{
    public string Type { get; set; } = "";
    public string Id { get; set; } = "";             // "key:Section" — the ini key + section the value is stored under
    public string Text { get; set; } = "";           // display label (supports $Key and {value} interpolation)
    public string Help { get; set; } = "";           // hover tooltip
    // --- valueOptions ---
    public string SourceType { get; set; } = "";     // ModSettingBool | ModSettingInt | ModSettingFloat | ModSettingString
    public double? Min { get; set; }                 // slider range/step
    public double? Max { get; set; }
    public double? Step { get; set; }
    public string FormatString { get; set; } = "";   // slider display, e.g. "{0} s" / "{1}"
    public List<string> Options { get; set; } = new();     // stepper/enum option labels
    public List<string> ShortNames { get; set; } = new();  // enum short display names
    // Default value — which field is read is decided by SourceType (Bool→DefaultBool, Int/Float→
    // DefaultNumber, String→DefaultString). Drives both config.json defaultValue and the ini line.
    public bool DefaultBool { get; set; }
    public double DefaultNumber { get; set; }
    public string DefaultString { get; set; } = "";
    // --- grouping / layout (all optional) ---
    public int? GroupControl { get; set; }           // marks this control as a group toggle (an int id)
    public int? GroupCondition { get; set; }          // show/hide driven by the group toggle with this id
    public bool GroupConditionNot { get; set; }        // emit groupCondition as {"NOT": id}
    public string GroupBehavior { get; set; } = "";    // "disable" (grey out) | "skip" (hide)
    public int? Position { get; set; }                 // two-column forced column: 0 left | 1 right
}
