namespace ModForge;

// Action-system asset/config spec DTOs (see workflows/specs/action-system-asset-generation-design.md).
// These produce LOOSE FILES (no esp record): OAR replacer folders + config.json, BDI config JSON,
// PIE .ini macro tables. The .hkx animations are user-supplied — ModForge only places them.
// Schemas are real-file verified against sub_projs/mod-survey/action-system/findings/.

// --- OAR (Open Animation Replacer) ---------------------------------------------------------
// One replacer-mod (root config = {name,author,description}) with N named submods, each its own
// config.json {name,description,priority,conditions[]} + the .hkx files it ships. Mirrors the real
// layout (Holmgang / NAMC / BFCO). Folder tree: Meshes/actors/character/animations/
//   OpenAnimationReplacer/<Mod>/<Submod>/config.json + <hkx>.
public sealed class AnimationReplacerSpec
{
    public string Mod { get; set; } = "";          // replacer-mod folder + config name
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    // OAR 2.2.0+ reusable condition blocks. They emit in the root config's conditionPresets array;
    // submods refer to one with conditions[] { condition: "PRESET", preset: "..." }.
    public List<OarConditionPresetSpec> ConditionPresets { get; set; } = new();
    public List<OarSubmodSpec> Submods { get; set; } = new();
}

public sealed class OarConditionPresetSpec
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<OarConditionSpec> Conditions { get; set; } = new();
}

public sealed class OarSubmodSpec
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Priority { get; set; }              // higher wins; required (>0)
    // The vanilla/MCO animation path being replaced (relative, under actors/character/animations/...).
    // The shipped .hkx land in the submod folder under the rebuilt vanilla path; basename matches.
    public string Replaces { get; set; } = "";
    public List<string> Hkx { get; set; } = new();      // user-supplied clips (rel. to Assets/spec dir)
    public List<string> Variants { get; set; } = new(); // optional; emitted under _variants_<anim>/
    // Optional OAR 2.2+ metadata for the files in Variants. The legacy string list stays intact so
    // existing specs deserialize unchanged; settings identify OAR's runtime replacement path explicitly.
    public List<OarReplacementAnimationSpec> ReplacementAnimations { get; set; } = new();
    public List<OarConditionSpec> Conditions { get; set; } = new();  // empty = applies always
    public NpcMovesetSpec? NpcMoveset { get; set; }     // sugar → expands into Conditions at build
    public List<OarFunctionSpec> FunctionsOnActivate { get; set; } = new();
    public List<OarFunctionSpec> FunctionsOnDeactivate { get; set; } = new();
    public List<OarFunctionSpec> FunctionsOnTrigger { get; set; } = new();
    // If true: this is a plain vanilla-path replacer — drop the .hkx at the vanilla path, no
    // config.json, no conditions (the simplest "tier a" replacer).
    public bool ReplaceVanillaPath { get; set; }
}

// A single OAR condition. Mirrors OAR's JSON shape (NOT Skyrim CTDA function names). Container
// conditions ("AND"/"OR") nest children in Conditions. Form refs use "Plugin.esp|0xFormID".
public sealed class OarConditionSpec
{
    public string Condition { get; set; } = "";    // IsActorBase | IsEquippedType | IsFemale | IsRace | Random | CompareValues | PRESET | AND | OR
    public bool Negated { get; set; }
    public string Form { get; set; } = "";          // IsActorBase / IsRace: "Plugin.esp|0xFormID"
    public int Type { get; set; }                   // IsEquippedType: weapon-type enum (see OarConditions.WeaponType)
    public bool LeftHand { get; set; }              // IsEquippedType: which hand
    public string GraphVariable { get; set; } = ""; // CompareValues: behavior graph variable name
    public string GraphVariableType { get; set; } = "Int"; // Int | Float | Bool
    public string Comparison { get; set; } = "==";  // Random / CompareValues: == | != | > | >= | < | <=
    public float Value { get; set; }                // CompareValues: the compared static value
    public float RandomMin { get; set; }            // Random: range min (default 0)
    public float RandomMax { get; set; } = 1f;      // Random: range max (default 1)
    public string Preset { get; set; } = "";        // PRESET: root conditionPresets[].name (OAR 2.2.0+)
    public List<OarConditionSpec> Conditions { get; set; } = new(); // AND/OR children
}

// OAR serializes per-animation variant settings in submod config.json/replacementAnimDatas. Path is
// deliberately explicit: OAR matches it against its discovered runtime replacement path, so deriving
// it from a build-machine path would make a distributed mod non-portable.
public sealed class OarReplacementAnimationSpec
{
    public string ProjectName { get; set; } = "";
    public string Path { get; set; } = "";
    public string VariantMode { get; set; } = "random"; // random (0) | sequential (1), OAR 2.2.0+
    public string VariantStateScope { get; set; } = "local"; // local (1) | submod (2) | replacerMod (4) | reference (8)
    public bool BlendBetweenVariants { get; set; } = true;
    public bool ResetRandomOnLoopOrEcho { get; set; } = true;
    public bool SharePlayedHistory { get; set; }
    public List<OarVariantMetadataSpec> Variants { get; set; } = new();
}

public sealed class OarVariantMetadataSpec
{
    // Filename in the generated _variants_ folder (normally 1.hkx, 2.hkx, ...), not the source path.
    public string Filename { get; set; } = "";
    public float Weight { get; set; } = 1f;
    public bool Disabled { get; set; }
    public bool PlayOnce { get; set; }
}

// Minimal built-in OAR function surface verified against OAR's serializer: CONDITION, RANDOM, ONE,
// and PlaySound. CONDITION/RANDOM/ONE are multifunctions and may recursively contain Functions.
public sealed class OarFunctionSpec
{
    public string Function { get; set; } = ""; // CONDITION | RANDOM | ONE | PlaySound
    public string RequiredVersion { get; set; } = "3.0.0";
    public List<OarFunctionSpec> Functions { get; set; } = new();
    public List<OarConditionSpec> Conditions { get; set; } = new(); // CONDITION only
    public List<float> Weights { get; set; } = new();               // RANDOM only; one per function
    public string SoundForm { get; set; } = "";                      // PlaySound only: Plugin.esp|0xFormID
    public List<OarFunctionTriggerSpec> Triggers { get; set; } = new(); // functionsOnTrigger only
}

public sealed class OarFunctionTriggerSpec
{
    public string Event { get; set; } = "";
    public string Payload { get; set; } = "";
}

// Sugar for the recurring NPC-moveset condition recipe (verified across the Animatecc library):
// right+left IsEquippedType + (optional) exclude-player IsActorBase + (optional) IsRace + Random.
public sealed class NpcMovesetSpec
{
    public string RightWeapon { get; set; } = "";   // weapon-type name (sword/dagger/shield/...): see OarConditions.WeaponType
    public string LeftWeapon { get; set; } = "";
    public bool PlayerOnly { get; set; }            // false → adds IsActorBase ¬player(Skyrim.esm|0x7)
    public string Race { get; set; } = "";          // optional → IsRace "Plugin.esp|0xFormID"
    public float? RandomPick { get; set; }          // optional → Random{0..1} < RandomPick (combo variety)
}

// --- BDI (Behavior Data Injector) ----------------------------------------------------------
// Flat JSON array → SKSE/Plugins/BehaviorDataInjector/<File>.json. Injects graph variables /
// events into a behavior project with no behavior patch. Schema verified vs DMK/BFCO BDI json.
public sealed class BehaviorDataSpec
{
    public string File { get; set; } = "";          // output filename stem (<File>.json)
    public List<BdiEntrySpec> Entries { get; set; } = new();
}

public sealed class BdiEntrySpec
{
    public string ProjectPath { get; set; } = "Actors";  // behavior project (Actors, actors\\Character, ...)
    public string Type { get; set; } = "";          // kInt | kBool | kFloat | kEvent
    public string Name { get; set; } = "";
    public float Value { get; set; }                // initial value; omitted in output for kEvent
}

// --- PIE (Payload Interpreter) macro table -------------------------------------------------
// SKSE/PayloadInterpreter/Config/<File>.ini: named macros → payload commands. Verified vs
// Stormcloaks VikingAxe.ini ($enableIframe = @SETGHOST|1).
public sealed class PayloadMacroSpec
{
    public string File { get; set; } = "";          // output filename stem (<File>.ini)
    public string Section { get; set; } = "";       // [Section] scope (e.g. behavior/anim project)
    public List<PieMacroSpec> Macros { get; set; } = new();
}

public sealed class PieMacroSpec
{
    public string Name { get; set; } = "";          // macro name (the $ is added on emit)
    public string Command { get; set; } = "";       // payload command (e.g. @SETGHOST|1)
}
