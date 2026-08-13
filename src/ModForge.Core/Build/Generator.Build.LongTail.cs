namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- pass 1: long-tail record types (scalar fields; keywords/effects wired in pass 2). Split
        // per-record-type below; the orchestrator calls them in this same order (FormID order is
        // load-bearing). Each is a flat AddNew + scalar-copy loop — no cross-record refs here. ---

        // --- pass 1: Ingredient (IGRE) — keywords/effects wired in pass 2 ---
        public void BuildIngredients()
        {
            foreach (var i in spec.Ingredients)
            {
                var r = mod.Ingredients.AddNew();
                r.EditorID = i.EditorId; r.Name = i.Name; r.Value = i.Value; r.Weight = i.Weight;
            }
        }

        // --- pass 1: Ammunition (AMMO) ---
        public void BuildAmmunition()
        {
            foreach (var a in spec.Ammunitions)
            {
                var r = mod.Ammunitions.AddNew();
                r.EditorID = a.EditorId; r.Name = a.Name; r.Value = a.Value; r.Weight = a.Weight; r.Damage = a.Damage;
            }
        }

        // --- pass 1: Scroll (SCRL) — magic effects wired in pass 2 (WireEffects) ---
        public void BuildScrolls()
        {
            foreach (var s in spec.Scrolls)
            {
                var r = mod.Scrolls.AddNew();
                r.EditorID = s.EditorId; r.Name = s.Name; r.Value = s.Value; r.Weight = s.Weight;
                if (Enum.TryParse<SpellType>(s.SpellType, ignoreCase: true, out var st)) r.Type = st;
                if (Enum.TryParse<CastType>(s.CastType, ignoreCase: true, out var ct)) r.CastType = ct;
                if (Enum.TryParse<TargetType>(s.TargetType, ignoreCase: true, out var tt)) r.TargetType = tt;
                if (s.BaseCost > 0) r.BaseCost = s.BaseCost;
            }
        }

        // --- pass 1: SoulGem (SLGM) ---
        public void BuildSoulGems()
        {
            foreach (var sg in spec.SoulGems)
            {
                var r = mod.SoulGems.AddNew();
                r.EditorID = sg.EditorId; r.Name = sg.Name; r.Value = sg.Value; r.Weight = sg.Weight;
                if (Enum.TryParse<SoulGem.Level>(sg.MaximumCapacity, ignoreCase: true, out var lv)) r.MaximumCapacity = lv;
            }
        }

        // --- pass 1: Key (KEYM) ---
        public void BuildKeys()
        {
            foreach (var k in spec.Keys)
            {
                var r = mod.Keys.AddNew();
                r.EditorID = k.EditorId; r.Name = k.Name; r.Value = k.Value; r.Weight = k.Weight;
            }
        }

        // --- pass 1: Keyword (KYWD) — the editorId IS the record; everything else FormLinks to it ---
        public void BuildKeywords()
        {
            foreach (var kw in spec.Keywords)
            {
                var r = mod.Keywords.AddNew();
                r.EditorID = kw.EditorId;
            }
        }

        // --- pass 1: Outfit (OTFT) — contents wired in pass 2 (WireOutfits) ---
        public void BuildOutfits()
        {
            foreach (var o in spec.Outfits)
            {
                var r = mod.Outfits.AddNew();
                r.EditorID = o.EditorId; r.Items = new();
            }
        }

        // --- pass 1: Static (STAT) — a `model` path string IS the .nif (external-resource pipeline) ---
        public void BuildStatics()
        {
            foreach (var st in spec.Statics)
            {
                var r = mod.Statics.AddNew();
                r.EditorID = st.EditorId;
                if (!string.IsNullOrEmpty(st.Model)) { r.Model = new Model(); r.Model.File.GivenPath = st.Model; }
            }
        }

        // --- pass 1: Activator (ACTI) — a `model` path string IS the .nif; sounds wired in pass 2 ---
        public void BuildActivators()
        {
            foreach (var ac in spec.Activators)
            {
                var r = mod.Activators.AddNew();
                r.EditorID = ac.EditorId; r.Name = ac.Name;
                if (!string.IsNullOrEmpty(ac.Model)) { r.Model = new Model(); r.Model.File.GivenPath = ac.Model; }
            }
        }

        // --- pass 1: Furniture (FURN) — a placeable interactive object (chairs/beds/benches). Like
        // STAT/ACTI, a `model` path string IS the .nif; an external-resource pipeline writes a user mesh here. ---
        public void BuildFurniture()
        {
            foreach (var fn in spec.Furniture)
            {
                var r = mod.Furniture.AddNew();
                r.EditorID = fn.EditorId; r.Name = fn.Name;
                if (!string.IsNullOrEmpty(fn.Model)) { r.Model = new Model(); r.Model.File.GivenPath = fn.Model; }
            }
        }

        // --- pass 1: Sound Descriptor (SNDR) — wraps a user `.wav`/`.xwm` so records can FormLink to it.
        // `SoundFiles` holds Data-relative `Sound\...` paths (the package step bundles the audio).
        // Category/OutputModel are FormLinks resolved in pass 2 (default to vanilla SFX there). ---
        public void BuildSounds()
        {
            foreach (var sd in spec.Sounds)
            {
                var r = mod.SoundDescriptors.AddNew();
                r.EditorID = sd.EditorId;
                r.Priority = sd.Priority;
                r.StaticAttenuation = sd.StaticAttenuation;
                foreach (var f in sd.Files)
                    if (!string.IsNullOrWhiteSpace(f))
                        r.SoundFiles.Add(new Mutagen.Bethesda.Plugins.Assets.AssetLink<Mutagen.Bethesda.Skyrim.Assets.SkyrimSoundAssetType>(f));
            }
        }

    }
}
