namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // Reusable DeepCopyIn masks — a TranslationMask is just instructions, so build it ONCE instead of
        // per loop iteration. All skip the localized Name (we set it from the spec; copying it would resolve
        // .STRINGS via the headless-absent load-order listing); Book/Weapon also skip their long text field.
        private static readonly MiscItem.TranslationMask   MiscCopyMask   = new(defaultOn: true) { Name = false };
        private static readonly Book.TranslationMask       BookCopyMask   = new(defaultOn: true) { Name = false, BookText = false };
        private static readonly Weapon.TranslationMask     WeaponCopyMask = new(defaultOn: true) { Name = false, Description = false };
        private static readonly Ingestible.TranslationMask PotionCopyMask = new(defaultOn: true) { Name = false };

        // --- pass 1: Misc / Book / Weapon (model templating to avoid equip/read CRASHes) ---
        public void BuildItems()
        {
            foreach (var m in spec.MiscItems)
            {
                var r = mod.MiscItems.AddNew();
                // A model-less MISC doesn't crash (inventory is an icon) but has NO 3D mesh when dropped
                // in the world. Optional `template` clones a vanilla misc (e.g. Skyrim.esm:0x063B42
                // GemRuby) for its model + keywords. Mask out the localized Name (we set it below).
                if (!string.IsNullOrWhiteSpace(m.Template)
                    && TryResolveTemplate<IMiscItemGetter>(m.Template, out var tmpl) && tmpl is not null)
                    r.DeepCopyIn(tmpl, out _, MiscCopyMask);
                r.EditorID = m.EditorId; r.Name = m.Name; r.Value = m.Value; r.Weight = m.Weight;
                // External-resource pipeline: a `model` path string IS the .nif (overrides any cloned
                // template mesh). `model` + `template` together is ambiguous — warn, model wins.
                if (!string.IsNullOrWhiteSpace(m.Model))
                {
                    if (!string.IsNullOrWhiteSpace(m.Template))
                        Warn($"  ! miscItem '{m.EditorId}': both `template` and `model` set — `model` wins (the user mesh overrides the cloned template's)");
                    r.Model = new Model(); r.Model.File.GivenPath = m.Model;
                }
            }
            foreach (var b in spec.Books)
            {
                var r = mod.Books.AddNew();
                // A model-less BOOK CRASHES on read (the reading view loads the 3D book mesh). Clone a
                // vanilla book (`template`: "<master>:0xFORMID", e.g. Skyrim.esm:0x0ED161) so it gets a
                // model + sounds + keywords, then override identity + text. DeepCopyIn keeps OUR FormKey.
                if (!string.IsNullOrWhiteSpace(b.Template))
                {
                    if (TryResolveTemplate<IBookGetter>(b.Template, out var tmpl) && tmpl is not null)
                        // Skip the localized strings (Name/BookText) — we set them below, and copying
                        // them would resolve .STRINGS via the (headless-absent) load-order listing.
                        r.DeepCopyIn(tmpl, out _, BookCopyMask);
                    else
                        Warn($"  ! book '{b.EditorId}': template '{b.Template}' not resolved — book will lack a model and may CRASH on read");
                }
                else
                    Warn($"  ! book '{b.EditorId}': no `template` — a model-less book CRASHES on read; set template to a vanilla book (e.g. Skyrim.esm:0x0ED161 Book1CheapNordsArise)");
                r.EditorID = b.EditorId; r.Name = b.Name; r.BookText = b.Text;
                if (b.Value != 0) r.Value = b.Value;
                if (b.Weight != 0) r.Weight = b.Weight;
                foreach (var f in b.Flags)
                    if (Enum.TryParse<Book.Flag>(f, ignoreCase: true, out var bf)) r.Flags |= bf;
                    else Warn($"  ! book '{b.EditorId}': unknown flag '{f}'");
                // Teaches: a SKILL book carries an ActorValue inline (no FormLink) — set it here. A SPELL
                // tome's SPEL FormLink may point at an in-spec spell built later, so it's wired in pass 2.
                // No `teaches` (or kind="") leaves the default BookTeachesNothing — a plain readable book.
                if (b.Teaches is { } t && t.Kind.Equals("skill", StringComparison.OrdinalIgnoreCase))
                {
                    if (Enum.TryParse<Skill>(t.Skill, ignoreCase: true, out var sk))
                        r.Teaches = new BookSkill { Skill = sk };
                    else
                        Warn($"  ! book '{b.EditorId}': teaches.skill '{t.Skill}' is not a valid Skill (e.g. Destruction, OneHanded, Smithing)");
                }
            }
            foreach (var w in spec.Weapons)
            {
                var r = mod.Weapons.AddNew();
                // A bare WEAP (no model / first-person model / animation type / equip slot) CRASHES on
                // equip — structurally valid but not in-game functional. Clone a vanilla weapon of the
                // desired type (`template`: "<master>:0xFORMID", e.g. Skyrim.esm:0x012EB7 = IronSword)
                // via DeepCopyIn — that brings the model, 1st-person model, equip slot, animation type,
                // skill, sounds, impact + type/material keywords — then override identity + stats below.
                // DeepCopyIn keeps OUR FormKey (record stays in this plugin; the template's sub-forms
                // become FormLinks into its master).
                if (!string.IsNullOrWhiteSpace(w.Template))
                {
                    if (TryResolveTemplate<IWeaponGetter>(w.Template, out var tmpl) && tmpl is not null)
                        // Skip the localized strings (Name/Description) — we set Name below, and copying
                        // them would resolve .STRINGS via the (headless-absent) load-order listing.
                        r.DeepCopyIn(tmpl, out _, WeaponCopyMask);
                    else
                        Warn($"  ! weapon '{w.EditorId}': template '{w.Template}' not resolved — weapon will lack a model and may CRASH on equip");
                }
                else
                    Warn($"  ! weapon '{w.EditorId}': no `template` — a model-less weapon CRASHES on equip; set template to a vanilla weapon (e.g. Skyrim.esm:0x012EB7 IronSword)");
                r.EditorID = w.EditorId; r.Name = w.Name;
                // External-resource pipeline: a `model` path overrides the cloned world-model .nif (the
                // template still supplies first-person model / anim / equip data — a user mesh usually
                // pairs WITH a template so the weapon stays equip-safe). `model`+`template` is intended
                // here, but `model` alone (no template) likely CRASHES on equip — warn either way.
                if (!string.IsNullOrWhiteSpace(w.Model))
                {
                    if (string.IsNullOrWhiteSpace(w.Template))
                        Warn($"  ! weapon '{w.EditorId}': `model` set but no `template` — a custom world mesh without a template's 1st-person model/anim/equip data may CRASH on equip; pair `model` with a `template` of the same weapon type");
                    r.Model ??= new Model(); r.Model.File.GivenPath = w.Model;
                }
                // Stats override the template's. speed/reach default to 1.0 so the weapon is swingable;
                // when templated, keep the clone's Data (anim type/skill/stagger/flags) and only restate
                // speed/reach + the basic stats.
                r.BasicStats = new WeaponBasicStats { Damage = w.Damage, Value = w.Value, Weight = w.Weight };
                r.Data ??= new WeaponData();
                r.Data.Speed = w.Speed > 0 ? w.Speed : (r.Data.Speed > 0 ? r.Data.Speed : 1.0f);
                r.Data.Reach = w.Reach > 0 ? w.Reach : (r.Data.Reach > 0 ? r.Data.Reach : 1.0f);
            }
        }

        // --- pass 1: MagicEffect (MGEF) + Spell (SPEL) scalar/archetype fields ---
        public void BuildMagicAndSpells()
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
                r.MagicSkill = Enum.TryParse<ActorValue>(me.MagicSkill, ignoreCase: true, out var sk) ? sk : ActorValue.None;
                r.ResistValue = Enum.TryParse<ActorValue>(me.ResistValue, ignoreCase: true, out var rv) ? rv : ActorValue.None;
                if (Enum.TryParse<CastType>(me.CastType, ignoreCase: true, out var mct)) r.CastType = mct;
                if (Enum.TryParse<TargetType>(me.TargetType, ignoreCase: true, out var mtt)) r.TargetType = mtt;
                foreach (var f in me.Flags)
                    if (Enum.TryParse<MagicEffect.Flag>(f, ignoreCase: true, out var fl)) r.Flags |= fl;
            }
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

        // --- pass 1: Potion, Armor, Faction, Relationship, Class, Message ---
        public void BuildConsumablesGearAndMessages()
        {
            foreach (var p in spec.Potions)
            {
                var r = mod.Ingestibles.AddNew();
                // A model-less ALCH drinks fine (no model load) but has NO 3D mesh when dropped. Optional
                // `template` clones a vanilla potion (e.g. Skyrim.esm:0x039BE5 RestoreHealth06) for the
                // bottle model + keywords + consume sound. Mask out the localized Name (set below); CLEAR
                // the cloned effects so pass-2 WireEffects adds only THIS spec's effects (no duplicates).
                if (!string.IsNullOrWhiteSpace(p.Template)
                    && TryResolveTemplate<IIngestibleGetter>(p.Template, out var tmpl) && tmpl is not null)
                {
                    r.DeepCopyIn(tmpl, out _, PotionCopyMask);
                    r.Effects.Clear();
                }
                r.EditorID = p.EditorId; r.Name = p.Name; r.Value = p.Value; r.Weight = p.Weight;
            }
            foreach (var a in spec.Armors)
            {
                var r = mod.Armors.AddNew();
                r.EditorID = a.EditorId; r.Name = a.Name;
                r.Value = a.Value; r.Weight = a.Weight; r.ArmorRating = a.ArmorRating;
                // BodyTemplate = the armor class (light/heavy/clothing) + which biped slots it fills.
                if (!string.IsNullOrEmpty(a.ArmorType) || a.Slots.Count > 0)
                {
                    var bt = new BodyTemplate { ArmorType = ParseArmorType(a.ArmorType) };
                    foreach (var slot in a.Slots)
                        if (Enum.TryParse<BipedObjectFlag>(slot, ignoreCase: true, out var f)) bt.FirstPersonFlags |= f;
                        else Warn($"  ! armor '{a.EditorId}' unknown slot '{slot}' (e.g. Body, Head, Hands, Feet, Forearms, Calves, Shield)");
                    r.BodyTemplate = bt;
                }
            }
            foreach (var f in spec.Factions)
            {
                var r = mod.Factions.AddNew();
                r.EditorID = f.EditorId; r.Name = f.Name;
                if (f.Vendor is { } v)
                {
                    // Vendor flag = "this faction's members are merchants". CanBeOwner mirrors vanilla
                    // merchant factions (they own their shop cell/chest). VendorValues carries the hours,
                    // sell radius, buy-stolen flag, and whether the buy/sell list is a NOT-sell list.
                    r.Flags |= Faction.FactionFlag.Vendor | Faction.FactionFlag.CanBeOwner;
                    r.VendorValues = new VendorValues
                    {
                        StartHour = (ushort)Math.Clamp((int)v.StartHour, 0, 24),
                        EndHour = (ushort)Math.Clamp((int)v.EndHour, 0, 24),
                        Radius = v.Radius,
                        OnlyBuysStolenItems = v.BuysStolen,
                        NotSellBuy = v.NotSellBuyList,
                    };
                    if (!string.IsNullOrEmpty(f.EditorId)) vendorFactionEds.Add(f.EditorId);
                    // SellBuyList (FormList) + MerchantContainer (a placed ref) are FormLinks resolved
                    // in pass 2 (WireVendors) — the container placement is created in the placement loop.
                }
            }
            // Relationship (RELA): scalar Rank now; Parent/Child NPC refs wired in pass 2.
            foreach (var rel in spec.Relationships)
            {
                var r = mod.Relationships.AddNew();
                r.EditorID = rel.EditorId;
                r.Rank = Enum.TryParse<Relationship.RankType>(rel.Rank, ignoreCase: true, out var rk)
                    ? rk : Relationship.RankType.Ally;
            }
            // EncounterZone (ECZN): pass-1 sets the level range/rank/flags (all inline). Owner/Location
            // FormLinks are wired in pass 2. maxLevel 0 = "uncapped" (the vanilla scales-with-player
            // idiom). A cell's / placed-spawn's `encounterZone` ref points at one (resolved in pass 2).
            foreach (var ez in spec.EncounterZones)
            {
                var r = mod.EncounterZones.AddNew();
                r.EditorID = ez.EditorId;
                r.MinLevel = (byte)Math.Clamp(ez.MinLevel, 0, 255);
                r.MaxLevel = (byte)Math.Clamp(ez.MaxLevel, 0, 255);
                r.Rank = (byte)Math.Clamp(ez.Rank, 0, 255);
                r.Flags = ParseFlags<EncounterZone.Flag>(ez.Flags);
            }
            // Class (CLAS): no FormLinks (all enums/weight dicts), so fully built in pass 1. An npc's
            // `class` ref can point at one (resolved in pass 2 — it's in formKeyByEd by then). StatWeights
            // (Health/Magicka/Stamina) drive the actor's attribute distribution; SkillWeights favour skills.
            foreach (var cl in spec.Classes)
            {
                var r = mod.Classes.AddNew();
                r.EditorID = cl.EditorId;
                if (!string.IsNullOrEmpty(cl.Name)) r.Name = cl.Name;
                if (!string.IsNullOrEmpty(cl.Description)) r.Description = cl.Description;
                if (Enum.TryParse<Skill>(cl.Teaches, ignoreCase: true, out var teach)) r.Teaches = teach;
                r.MaxTrainingLevel = (byte)Math.Clamp(cl.MaxTrainingLevel, 0, 255);
                // All-zero stat weights would be a degenerate distribution; default to balanced.
                bool anyStat = cl.HealthWeight != 0 || cl.MagickaWeight != 0 || cl.StaminaWeight != 0;
                r.StatWeights[BasicStat.Health]  = (byte)Math.Clamp(anyStat ? cl.HealthWeight  : 1, 0, 255);
                r.StatWeights[BasicStat.Magicka] = (byte)Math.Clamp(anyStat ? cl.MagickaWeight : 1, 0, 255);
                r.StatWeights[BasicStat.Stamina] = (byte)Math.Clamp(anyStat ? cl.StaminaWeight : 1, 0, 255);
                foreach (var (skillName, w) in cl.SkillWeights)
                    if (Enum.TryParse<Skill>(skillName, ignoreCase: true, out var sk))
                        r.SkillWeights[sk] = (byte)Math.Clamp(w, 0, 255);
                    else Warn($"  ! class '{cl.EditorId}' skillWeight '{skillName}' is not a Skill — skipped");
            }
            foreach (var msg in spec.Messages)
            {
                var r = mod.Messages.AddNew();
                r.EditorID = msg.EditorId; r.Name = msg.Name; r.Description = msg.Description;
            }
        }

        // --- pass 1: long-tail record types (scalar fields; keywords/effects wired in pass 2) ---
        public void BuildLongTailItems()
        {
            foreach (var i in spec.Ingredients)
            {
                var r = mod.Ingredients.AddNew();
                r.EditorID = i.EditorId; r.Name = i.Name; r.Value = i.Value; r.Weight = i.Weight;
            }
            foreach (var a in spec.Ammunitions)
            {
                var r = mod.Ammunitions.AddNew();
                r.EditorID = a.EditorId; r.Name = a.Name; r.Value = a.Value; r.Weight = a.Weight; r.Damage = a.Damage;
            }
            foreach (var s in spec.Scrolls)
            {
                var r = mod.Scrolls.AddNew();
                r.EditorID = s.EditorId; r.Name = s.Name; r.Value = s.Value; r.Weight = s.Weight;
                if (Enum.TryParse<SpellType>(s.SpellType, ignoreCase: true, out var st)) r.Type = st;
                if (Enum.TryParse<CastType>(s.CastType, ignoreCase: true, out var ct)) r.CastType = ct;
                if (Enum.TryParse<TargetType>(s.TargetType, ignoreCase: true, out var tt)) r.TargetType = tt;
                if (s.BaseCost > 0) r.BaseCost = s.BaseCost;
            }
            foreach (var sg in spec.SoulGems)
            {
                var r = mod.SoulGems.AddNew();
                r.EditorID = sg.EditorId; r.Name = sg.Name; r.Value = sg.Value; r.Weight = sg.Weight;
                if (Enum.TryParse<SoulGem.Level>(sg.MaximumCapacity, ignoreCase: true, out var lv)) r.MaximumCapacity = lv;
            }
            foreach (var k in spec.Keys)
            {
                var r = mod.Keys.AddNew();
                r.EditorID = k.EditorId; r.Name = k.Name; r.Value = k.Value; r.Weight = k.Weight;
            }
            foreach (var kw in spec.Keywords)
            {
                var r = mod.Keywords.AddNew();
                r.EditorID = kw.EditorId;
            }
            foreach (var o in spec.Outfits)
            {
                var r = mod.Outfits.AddNew();
                r.EditorID = o.EditorId; r.Items = new();
            }
            foreach (var st in spec.Statics)
            {
                var r = mod.Statics.AddNew();
                r.EditorID = st.EditorId;
                if (!string.IsNullOrEmpty(st.Model)) { r.Model = new Model(); r.Model.File.GivenPath = st.Model; }
            }
            foreach (var ac in spec.Activators)
            {
                var r = mod.Activators.AddNew();
                r.EditorID = ac.EditorId; r.Name = ac.Name;
                if (!string.IsNullOrEmpty(ac.Model)) { r.Model = new Model(); r.Model.File.GivenPath = ac.Model; }
            }
            // FURN — a placeable interactive object (chairs/beds/benches). Like STAT/ACTI, a `model`
            // path string IS the .nif; an external resource pipeline writes a user mesh here directly.
            foreach (var fn in spec.Furniture)
            {
                var r = mod.Furniture.AddNew();
                r.EditorID = fn.EditorId; r.Name = fn.Name;
                if (!string.IsNullOrEmpty(fn.Model)) { r.Model = new Model(); r.Model.File.GivenPath = fn.Model; }
            }
            // SNDR — Sound Descriptor: wraps a user `.wav`/`.xwm` so records can FormLink to it.
            // `SoundFiles` holds Data-relative `Sound\...` paths (the package step bundles the audio).
            // Category/OutputModel are FormLinks resolved in pass 2 (default to vanilla SFX there).
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

        public void WireEffects()
        {
            void Wire(string ed, List<EffectSpec> effects) => WireEffectsFor(ed, effects);
            foreach (var s in spec.Spells) Wire(s.EditorId, s.Effects);
            // Spell EquipType (EQUP ref) — needed for a hand spell to be equippable/castable.
            foreach (var s in spec.Spells)
            {
                if (string.IsNullOrWhiteSpace(s.EquipType)) continue;
                if (recordsByEd.TryGetValue(s.EditorId, out var rec) && rec is ISpell sp)
                    Resolve($"spell '{s.EditorId}' equipType", s.EquipType, fk => sp.EquipmentType.SetTo(fk));
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
            }
        }
    }
}
