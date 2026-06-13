namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 1: Hazard (HAZD) scalar/model/flag fields. A radius effect applying `spell` every
        // `targetInterval`s to actors inside. FormLink refs (spell/light/sound/imad/impactDataSet) are
        // wired in pass 2 (WireHazards). Built before BuildFormKeyTable so a magicEffect `association`
        // and a placement `base` can resolve it by editorId. ---
        public void BuildHazards()
        {
            foreach (var hz in spec.Hazards)
            {
                var r = mod.Hazards.AddNew();
                r.EditorID = hz.EditorId;
                if (!string.IsNullOrEmpty(hz.Name)) r.Name = hz.Name;
                if (!string.IsNullOrWhiteSpace(hz.Model)) r.Model = new Mutagen.Bethesda.Skyrim.Model { File = hz.Model.Trim() };
                r.Radius = hz.Radius;
                r.Lifetime = hz.Lifetime;
                r.TargetInterval = hz.TargetInterval;
                r.Limit = hz.Limit;
                if (hz.Flags.Count > 0) r.Flags = ParseFlags<Mutagen.Bethesda.Skyrim.Hazard.Flag>(hz.Flags);
            }
        }

        // --- pass 2: Hazard FormLink refs (may point forward or at vanilla). ---
        public void WireHazards()
        {
            foreach (var hz in spec.Hazards)
            {
                if (!recordsByEd.TryGetValue(hz.EditorId, out var rec) || rec is not Mutagen.Bethesda.Skyrim.IHazard h) continue;
                Resolve($"hazard '{hz.EditorId}' spell",              hz.Spell,              fk => h.Spell.SetTo(fk));
                Resolve($"hazard '{hz.EditorId}' light",              hz.Light,              fk => h.Light.SetTo(fk));
                Resolve($"hazard '{hz.EditorId}' sound",              hz.Sound,              fk => h.Sound.SetTo(fk));
                Resolve($"hazard '{hz.EditorId}' impactDataSet",      hz.ImpactDataSet,      fk => h.ImpactDataSet.SetTo(fk));
                Resolve($"hazard '{hz.EditorId}' imageSpaceModifier", hz.ImageSpaceModifier, fk => h.ImageSpaceModifier.SetTo(fk));
            }
        }
    }
}
