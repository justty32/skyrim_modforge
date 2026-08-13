namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
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
