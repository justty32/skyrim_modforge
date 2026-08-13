namespace ModForge;

// SkyPatcher (Nexus 108591) config spec DTOs.
// Produces a LOOSE FILE (no esp record):
//   SKSE/Plugins/SkyPatcher/<recordType>/<File>.ini
// SkyPatcher applies in-memory runtime edits to records by filter, with no ESP override — the
// no-conflict way to mass-edit vanilla NPCs/armor/weapons or inject into leveled lists.
// Syntax verified against SkyPatcher (sub_projs/mod-survey/findings/skypatcher-records-and-config.md):
//   flat lines, NO [section] headers: filterKey=value:filterKey=value:modKey=value:modKey=value
// Filters (must all match) come first, then the modifications. A field may repeat (e.g. two
// spellsToAdd) — model that as two entries in the list.
public sealed class SkyPatcherSpec
{
    public string File { get; set; } = "";        // output stem → SkyPatcher/<recordType>/<File>.ini
    // The record-type folder: npc | armor | weapon | ammo | leveledList | formList | race | container.
    public string RecordType { get; set; } = "";
    public List<SkyPatcherLineSpec> Patches { get; set; } = new();
}

// One config line: all `filters` (AND) then all `mods`, emitted "k=v:k=v:...".
public sealed class SkyPatcherLineSpec
{
    public List<SkyPatcherFieldSpec> Filters { get; set; } = new();  // filterByRaces=…, filterByKeywords=…
    public List<SkyPatcherFieldSpec> Mods { get; set; } = new();     // spellsToAdd=…, perksToAdd=…, objectsToAdd=…
}

public sealed class SkyPatcherFieldSpec
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<SkyPatcherSpec> SkyPatchers { get; set; } = new();       // SkyPatcher <recordType>/<file>.ini (loose; Spec.SkyPatcher.cs)
}
