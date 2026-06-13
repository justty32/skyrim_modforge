namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 1: MagicEffect (MGEF) scalar/archetype fields. Association/projectile/art/sound
        // refs are wired in pass 2 (WireMagicEffectRefs). Built before Spells (orchestrator order). ---
        public void BuildMagicEffects()
        {
            foreach (var me in spec.MagicEffects)
            {
                var r = mod.MagicEffects.AddNew();
                r.EditorID = me.EditorId;
                if (!string.IsNullOrEmpty(me.Name)) r.Name = me.Name;
                if (!string.IsNullOrEmpty(me.Description)) r.Description = me.Description;
                r.BaseCost = me.BaseCost;
                // Archetype: Type (what it does) + ActorValue (what it acts on). Association (summon/bound
                // form) is a ref, wired in pass 2. MagicSkill/ResistValue default to None (-1) when unset.
                var arch = new MagicEffectArchetype();
                if (Enum.TryParse<MagicEffectArchetype.TypeEnum>(me.Archetype, ignoreCase: true, out var at)) arch.Type = at;
                arch.ActorValue = Enum.TryParse<ActorValue>(me.ActorValue, ignoreCase: true, out var av) ? av : ActorValue.None;
                r.Archetype = arch;
                // DualValueModifier: the 2nd affected AV + how the magnitude splits to it.
                if (!string.IsNullOrWhiteSpace(me.SecondActorValue)
                    && Enum.TryParse<ActorValue>(me.SecondActorValue, ignoreCase: true, out var sav))
                {
                    r.SecondActorValue = sav;
                    r.SecondActorValueWeight = me.SecondActorValueWeight;
                }
                r.MagicSkill = Enum.TryParse<ActorValue>(me.MagicSkill, ignoreCase: true, out var sk) ? sk : ActorValue.None;
                r.ResistValue = Enum.TryParse<ActorValue>(me.ResistValue, ignoreCase: true, out var rv) ? rv : ActorValue.None;
                if (Enum.TryParse<CastType>(me.CastType, ignoreCase: true, out var mct)) r.CastType = mct;
                if (Enum.TryParse<TargetType>(me.TargetType, ignoreCase: true, out var mtt)) r.TargetType = mtt;
                foreach (var f in me.Flags)
                    if (Enum.TryParse<MagicEffect.Flag>(f, ignoreCase: true, out var fl)) r.Flags |= fl;
            }
        }

        // --- pass 1: Spell (SPEL) scalar fields. Effects + EquipType are wired in pass 2 (WireEffects). ---
        public void BuildSpells()
        {
            foreach (var s in spec.Spells)
            {
                var r = mod.Spells.AddNew();
                r.EditorID = s.EditorId; r.Name = s.Name;
                if (Enum.TryParse<SpellType>(s.SpellType, ignoreCase: true, out var st)) r.Type = st;
                if (Enum.TryParse<CastType>(s.CastType, ignoreCase: true, out var ct)) r.CastType = ct;
                if (Enum.TryParse<TargetType>(s.TargetType, ignoreCase: true, out var tt)) r.TargetType = tt;
                if (s.BaseCost > 0) r.BaseCost = s.BaseCost;
                if (s.ChargeTime > 0) r.ChargeTime = s.ChargeTime;
            }
        }

        // --- pass 2: magic effects on spell/potion/ingredient/scroll (IHasEffects) + spell EquipType ---
        // Wire a record's MGEF-based effects (shared by spell/potion/ingredient/scroll/enchantment —
        // anything that implements IHasEffects). Pulled out of WireEffects so the ENCH pass-2 step
        // (Generator.Build.Enchantments.cs) can reuse it.
        public void WireEffectsFor(string ed, List<EffectSpec> effects)
        {
            if (effects.Count == 0) return;
            if (!recordsByEd.TryGetValue(ed, out var rec) || rec is not IHasEffects he)
            { Warn($"  ! '{ed}' takes no magic effects (or not found)"); return; }
            foreach (var es in effects)
                Resolve($"'{ed}' effect", es.MagicEffect, fk =>
                {
                    var eff = new Effect();
                    eff.BaseEffect.SetTo(fk);
                    eff.Data = new EffectData { Magnitude = es.Magnitude, Area = es.Area, Duration = es.Duration };
                    he.Effects.Add(eff);
                });
        }

        // Castable = occupies an equip slot (gets a default EitherHand EQUP). Empty defaults to Spell
        // (Mutagen's SpellType 0). Passive types (Ability/Disease/Poison/Addiction) stay slot-less.
        private static bool IsCastableSpellType(string spellType) =>
            string.IsNullOrWhiteSpace(spellType) ||
            spellType.Equals("Spell", StringComparison.OrdinalIgnoreCase) ||
            spellType.Equals("Voice", StringComparison.OrdinalIgnoreCase) ||
            spellType.Equals("Power", StringComparison.OrdinalIgnoreCase) ||
            spellType.Equals("LesserPower", StringComparison.OrdinalIgnoreCase);

        public void WireEffects()
        {
            void Wire(string ed, List<EffectSpec> effects) => WireEffectsFor(ed, effects);
            foreach (var s in spec.Spells) Wire(s.EditorId, s.Effects);
            // Spell EquipType (EQUP ref) — REQUIRED for any castable spell to have an equip slot. This
            // covers hand spells (an NPC can't equip+cast without it) AND **Voice/shout charge-spells**:
            // every vanilla shout word-spell carries EitherHand (Skyrim.esm:0x013F44) too. With no EQUP
            // the player learns the shout but CAN'T SHOUT it (the word fires no spell → the Thu'um does
            // nothing). So when the spec omits equipType, default castable types to EitherHand; leave
            // passive types (Ability/Disease/Poison/Addiction) slot-less.
            foreach (var s in spec.Spells)
            {
                if (!(recordsByEd.TryGetValue(s.EditorId, out var rec) && rec is ISpell sp)) continue;
                if (!string.IsNullOrWhiteSpace(s.EquipType))
                    Resolve($"spell '{s.EditorId}' equipType", s.EquipType, fk => sp.EquipmentType.SetTo(fk));
                else if (IsCastableSpellType(s.SpellType))
                    Resolve($"spell '{s.EditorId}' default equipType", "Skyrim.esm:0x00013F44", fk => sp.EquipmentType.SetTo(fk));
            }
            foreach (var p in spec.Potions) Wire(p.EditorId, p.Effects);
            foreach (var i in spec.Ingredients) Wire(i.EditorId, i.Effects);
            foreach (var s in spec.Scrolls) Wire(s.EditorId, s.Effects);

            // Spell tome (BOOK teaches=spell): the SPEL FormLink resolved by editorId — the killer combo
            // is a tome teaching an IN-SPEC custom spell (MGEF→SPEL→tome in one spec, all forward refs
            // now in the table). Reading the book grants the spell in-game. (Skill books wired inline.)
            foreach (var b in spec.Books)
            {
                if (b.Teaches is not { } t || !t.Kind.Equals("spell", StringComparison.OrdinalIgnoreCase)) continue;
                if (!recordsByEd.TryGetValue(b.EditorId, out var rec) || rec is not IBook book) continue;
                if (string.IsNullOrWhiteSpace(t.Spell))
                { Warn($"  ! book '{b.EditorId}': teaches.kind=spell but teaches.spell ref is empty — book teaches nothing"); continue; }
                Resolve($"book '{b.EditorId}' teaches spell", t.Spell, fk =>
                    book.Teaches = new BookSpell { Spell = new FormLink<ISpellGetter>(fk) });
            }
        }

        // --- pass 2: MagicEffect refs (may point forward, or at vanilla forms): the archetype ---
        // `association` (summon/bound form) + the visual `projectile`/`castingArt`/`hitEffectArt`/
        // `explosion`. Resolve() skips empty refs, so only authored ones are wired.
        public void WireMagicEffectRefs()
        {
            foreach (var me in spec.MagicEffects)
            {
                if (!recordsByEd.TryGetValue(me.EditorId, out var rec) || rec is not IMagicEffect mgef) continue;
                if (mgef.Archetype is IMagicEffectArchetype a)
                    Resolve($"magicEffect '{me.EditorId}' association", me.Association, fk => a.Association.SetTo(fk));
                Resolve($"magicEffect '{me.EditorId}' projectile",   me.Projectile,   fk => mgef.Projectile.SetTo(fk));
                Resolve($"magicEffect '{me.EditorId}' castingArt",   me.CastingArt,   fk => mgef.CastingArt.SetTo(fk));
                Resolve($"magicEffect '{me.EditorId}' hitEffectArt", me.HitEffectArt, fk => mgef.HitEffectArt.SetTo(fk));
                Resolve($"magicEffect '{me.EditorId}' explosion",    me.Explosion,    fk => mgef.Explosion.SetTo(fk));
                // Sounds (esp. Release = the Thu'um voice for a shout). Each resolves its SNDR ref and
                // appends a MagicEffectSound of the named phase (default Release). Sounds is null on a
                // fresh MGEF — materialize the list before appending.
                if (me.Sounds.Count > 0)
                {
                    var sounds = mgef.Sounds ??= new();
                    foreach (var snd in me.Sounds)
                    {
                        if (string.IsNullOrWhiteSpace(snd.Sound)) continue;
                        if (!Enum.TryParse<MagicEffect.SoundType>(snd.Type, ignoreCase: true, out var phase))
                        {
                            Warn($"  ! magicEffect '{me.EditorId}' sound type '{snd.Type}' invalid (Release/Charge/Ready/SheathDraw/ConcentrationCastLoop/OnHit) — skipped");
                            continue;
                        }
                        Resolve($"magicEffect '{me.EditorId}' sound", snd.Sound, fk =>
                        {
                            var ms = new MagicEffectSound { Type = phase };
                            ms.Sound.SetTo(fk);
                            sounds.Add(ms);
                        });
                    }
                }
            }
        }
    }
}
