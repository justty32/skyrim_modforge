namespace ModForge;

// KID (Keyword Item Distributor, powerofthree, Nexus 55728) distribution config spec DTOs.
// Produces a LOOSE FILE (no esp record): <File>_KID.ini (KID scans Data/ for every *_KID.ini).
// KID attaches a Keyword to matching records (Weapon/Armor/MGEF/…) by filter at start-up, with no
// ESP patch — used for batch item tagging (quality classes, SPID/OAR condition keywords). If the
// keyword's EditorID isn't found in any loaded plugin, KID CREATES a new KYWD on the fly.
// Syntax verified against KID (sub_projs/mod-survey/findings/keyword-item-distributor-config-*.md):
//   Keyword = <keyword>|<type>|<strings_or_formIDs>|<traits>|<chance>
// Trailing omitted fields = NONE; a middle gap is held open with NONE (same shape as SPID).
public sealed class KidDistributionSpec
{
    public string File { get; set; } = "";              // output stem → <File>_KID.ini
    public List<KidEntrySpec> Entries { get; set; } = new();
}

public sealed class KidEntrySpec
{
    // Field 1 (keyword, required): the KYWD to distribute. EditorID / "0xFormID~Plugin.esp" /
    // bare "0x…" for Skyrim/DLC. An unknown EditorID makes KID create a new KYWD.
    public string Keyword { get; set; } = "";
    // Field 2 (type, required): record type — Weapon|Armor|Ammo|Magic Effect|Potion|Scroll|Location|
    // Ingredient|Book|Misc Item|Key|Soul Gem|Spell|Activator|Flora|Furniture|Race|Talking Activator|Enchantment.
    public string Type { get; set; } = "";
    // Field 3 (strings/formIDs filter): array, comma-joined (OR). '+x' AND-requires, '-x' excludes,
    // '*x' wildcard, "0x…~esp" FormID, "*x.nif" model path. Empty → NONE (all of that type).
    public List<string> Filters { get; set; } = new();
    // Field 4 (traits): raw passthrough — type-specific (e.g. Armor "AR(10/50)", Weapon "OneHandSword",
    // MGEF "20(0/25)", Book "S,20"). Empty → NONE.
    public string Traits { get; set; } = "";
    // Field 5 (chance): 0.0–100.0 distribution chance. Null → omitted (KID defaults to 100).
    public double? Chance { get; set; }
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<KidDistributionSpec> KidDistributions { get; set; } = new(); // KID <file>_KID.ini (loose; Spec.KidDistribution.cs)
}
