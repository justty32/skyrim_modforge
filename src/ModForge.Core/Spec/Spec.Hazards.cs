using System.Collections.Generic;

namespace ModForge;

// A Hazard (HAZD): a radius effect that periodically applies `spell` to actors inside it (a fire/frost/
// poison patch). Use it two ways: (1) a magicEffects[] entry with archetype "SpawnHazard" + association
// = this editorId → a castable spell that drops it; (2) a placements[] entry whose base is this editorId
// → a placed static trap (PlacedHazard). `lifetime` 0 = inherit from the spawning spell / permanent;
// `targetInterval` = seconds between applications; `limit` 0 = unlimited instances.
public sealed class HazardSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Model { get; set; } = "";
    public float Radius { get; set; }
    public float Lifetime { get; set; }
    public float TargetInterval { get; set; } = 1f;
    public uint Limit { get; set; }
    public string Spell { get; set; } = "";
    public List<string> Flags { get; set; } = new();   // Hazard.Flag names
    public string Light { get; set; } = "";
    public string Sound { get; set; } = "";
    public string ImageSpaceModifier { get; set; } = "";
    public string ImpactDataSet { get; set; } = "";
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<HazardSpec> Hazards { get; set; } = new();   // Hazard (HAZD) — radius effect / placed trap
}
