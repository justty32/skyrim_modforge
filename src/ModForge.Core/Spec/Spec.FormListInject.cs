namespace ModForge;

// FormList Manipulator (FLM, MaskedRPGFan, Nexus 74037) config spec DTOs.
// Produces a LOOSE FILE (no esp record): <File>_FLM.ini at the mod folder root (= Data/).
// FLM appends forms to ANY already-loaded FormList (vanilla / another mod's) at runtime with NO
// ESP override → zero conflict. This is the no-conflict alternative to overriding someone else's
// FLST; for a FLST you OWN, build it ESP-side instead (formLists[]).
// Syntax verified against FLM v1.8.1 (sub_projs/mod-survey/findings/formlist-manipulator-*.md):
//   FormList = <FList>|<Form>, <Form>, *<FormList>, #<Group>, #<Collection>|<Filter>
// MVP covers the FormList operation line + Filter/Alias/Group/Collection definitions. The ModEvent
// (runtime-dynamic, needs a Papyrus sender) and the specialized shortcut lines (Plant/BToys/GToys/
// HairColors/AtronachForge/…) are intentionally out of scope.
public sealed class FormListInjectSpec
{
    public string File { get; set; } = "";                       // output stem → <File>_FLM.ini at the mod root
    public List<FlmFilterSpec> Filters { get; set; } = new();    // Filter = name|+A.esp, -B.esp, ...
    public List<FlmNamedListSpec> Aliases { get; set; } = new(); // Alias = name|FList, FList, ...   (group target FLSTs)
    public List<FlmNamedListSpec> Groups { get; set; } = new();  // Group = name|Form, Form, ...     (reusable form set)
    public List<FlmCollectionSpec> Collections { get; set; } = new(); // Collection = name|FormType|kw,-kw|filter
    public List<FlmEntrySpec> Entries { get; set; } = new();     // FormList = FList|forms|filter    (the operations)
}

// Filter = <name>|<cond>, <cond>, ...   conditions are OR'd; each is "+Plugin.esp" (must be active),
// "-Plugin.esp" (must be inactive), or "+A.esp&-B.esp" (AND within one condition).
public sealed class FlmFilterSpec
{
    public string Name { get; set; } = "";
    public List<string> Conditions { get; set; } = new();
}

// Shared shape for Alias (items = target FormLists) and Group (items = forms). Items are raw tokens
// (EditorID / "0xFormID~Plugin.esp" / "*FormList" / "#Group" / "#Collection") emitted comma-joined.
public sealed class FlmNamedListSpec
{
    public string Name { get; set; } = "";
    public List<string> Items { get; set; } = new();
}

// Collection = <name>|<FormType>|<Keyword>, -<ExcludeKeyword>, ...|<Filter>
// Batch-selects forms of one FormType carrying ALL listed keywords (AND); "-kw" excludes.
public sealed class FlmCollectionSpec
{
    public string Name { get; set; } = "";
    public string FormType { get; set; } = "";              // Armor|Weapon|Ammo|…|Spell (FLM FormType set)
    public List<string> Keywords { get; set; } = new();     // each "kw" required; "-kw" excludes
    public string Filter { get; set; } = "";                // optional "#FilterName"
}

// FormList = <FList>|<Form>, <Form>, *<FormList>, #<Group>, #<Collection>|<Filter>
// The actual operation: append the forms to the target FormList (optionally gated by a Filter).
public sealed class FlmEntrySpec
{
    // Target FormList: EditorID / "0xFormID~Plugin.esp" / "#Alias" (a defined alias of multiple FLSTs).
    public string Target { get; set; } = "";
    // Raw form tokens: a ref, "*FormList" (expand its contents), "#Group", or "#Collection".
    public List<string> Forms { get; set; } = new();
    // Optional "#FilterName" — only apply this line if the filter passes (plugin presence check).
    public string Filter { get; set; } = "";
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<FormListInjectSpec> FormListInjects { get; set; } = new(); // FLM <file>_FLM.ini (loose; Spec.FormListInject.cs)
}
