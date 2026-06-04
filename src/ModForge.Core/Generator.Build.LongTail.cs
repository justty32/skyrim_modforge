namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
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

        // --- pass 2: keywords on armor/weapon/misc/... (all implement the IKeyworded aspect) ---
        public void WireKeywords()
        {
            void Wire(string ed, List<string> kws)
            {
                if (kws.Count == 0) return;
                if (!recordsByEd.TryGetValue(ed, out var rec) || rec is not IKeyworded<IKeywordGetter> kw)
                { Warn($"  ! '{ed}' takes no keywords (or not found)"); return; }
                kw.Keywords ??= new();
                foreach (var kref in kws)
                    Resolve($"'{ed}' keyword", kref, fk => kw.Keywords!.Add(new FormLink<IKeywordGetter>(fk)));
            }
            foreach (var a in spec.Armors) Wire(a.EditorId, a.Keywords);
            foreach (var w in spec.Weapons) Wire(w.EditorId, w.Keywords);
            foreach (var m in spec.MiscItems) Wire(m.EditorId, m.Keywords);
            foreach (var i in spec.Ingredients) Wire(i.EditorId, i.Keywords);
            foreach (var a in spec.Ammunitions) Wire(a.EditorId, a.Keywords);
            foreach (var s in spec.Scrolls) Wire(s.EditorId, s.Keywords);
            foreach (var sg in spec.SoulGems) Wire(sg.EditorId, sg.Keywords);
            foreach (var k in spec.Keys) Wire(k.EditorId, k.Keywords);
            foreach (var ac in spec.Activators) Wire(ac.EditorId, ac.Keywords);
        }

        // --- pass 2: external-resource sound wiring ---
        // SNDR Category/OutputModel + the per-record sound FormLinks (activator activation/looping,
        // misc/weapon pick-up/put-down) point at a SoundDescriptor: an in-spec `sounds` editorId OR
        // a vanilla `<master>:0xFORMID`. The SNDR's Category/OutputModel default to vanilla SFX so an
        // authored .wav is actually audible.
        public void WireSounds()
        {
            const string DefaultSoundCategory = "Skyrim.esm:0x0172A1";    // AudioCategorySFX
            const string DefaultSoundOutputModel = "Skyrim.esm:0x0B4058"; // vanilla SFX output model (SOPM)
            foreach (var sd in spec.Sounds)
            {
                if (!recordsByEd.TryGetValue(sd.EditorId, out var rec) || rec is not ISoundDescriptor snd) continue;
                Resolve($"sound '{sd.EditorId}' category",
                    string.IsNullOrWhiteSpace(sd.Category) ? DefaultSoundCategory : sd.Category,
                    fk => snd.Category.SetTo(fk));
                Resolve($"sound '{sd.EditorId}' outputModel",
                    string.IsNullOrWhiteSpace(sd.OutputModel) ? DefaultSoundOutputModel : sd.OutputModel,
                    fk => snd.OutputModel.SetTo(fk));
            }
            void WireSound(string ownerEd, string soundRef, string slot, Action<FormKey> set)
            {
                if (string.IsNullOrWhiteSpace(soundRef)) return;
                Resolve($"'{ownerEd}' {slot}", soundRef, set);
            }
            foreach (var ac in spec.Activators)
            {
                if (!recordsByEd.TryGetValue(ac.EditorId, out var rec) || rec is not IActivator a) continue;
                WireSound(ac.EditorId, ac.ActivationSound, "activationSound", fk => a.ActivationSound.SetTo(fk));
                WireSound(ac.EditorId, ac.LoopingSound,    "loopingSound",    fk => a.LoopingSound.SetTo(fk));
            }
            foreach (var m in spec.MiscItems)
            {
                if (!recordsByEd.TryGetValue(m.EditorId, out var rec) || rec is not IMiscItem mi) continue;
                WireSound(m.EditorId, m.PickUpSound,  "pickUpSound",  fk => mi.PickUpSound.SetTo(fk));
                WireSound(m.EditorId, m.PutDownSound, "putDownSound", fk => mi.PutDownSound.SetTo(fk));
            }
            foreach (var w in spec.Weapons)
            {
                if (!recordsByEd.TryGetValue(w.EditorId, out var rec) || rec is not IWeapon wp) continue;
                WireSound(w.EditorId, w.PickUpSound,  "pickUpSound",  fk => wp.PickUpSound.SetTo(fk));
                WireSound(w.EditorId, w.PutDownSound, "putDownSound", fk => wp.PutDownSound.SetTo(fk));
            }
        }
    }
}
