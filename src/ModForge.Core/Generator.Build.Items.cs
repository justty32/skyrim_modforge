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
        // Armor: bring the template's Armature (ARMA addons = the actual worn mesh) + WorldModel (ground
        // model) + BodyTemplate. Skip the localized Name (set from spec). Description on ARMO is a nested
        // sub-mask, not a bool — vanilla armor templates have no DESC so there's no .STRINGS to resolve.
        private static readonly Armor.TranslationMask ArmorCopyMask = new(defaultOn: true) { Name = false };

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
                // Stats: a non-zero spec value OVERRIDES the template's; a left-default (0) value KEEPS
                // the cloned template's stat (a templated weapon must inherit the iron sword's damage —
                // clobbering it to 0 leaves a weapon NPCs rate below their fists and never draw). For a
                // NON-templated weapon BasicStats is null, so create it from the spec values as before.
                r.BasicStats ??= new WeaponBasicStats();
                if (w.Damage > 0) r.BasicStats.Damage = w.Damage;
                if (w.Value > 0) r.BasicStats.Value = w.Value;
                if (w.Weight > 0) r.BasicStats.Weight = w.Weight;
                r.Data ??= new WeaponData();
                r.Data.Speed = w.Speed > 0 ? w.Speed : (r.Data.Speed > 0 ? r.Data.Speed : 1.0f);
                r.Data.Reach = w.Reach > 0 ? w.Reach : (r.Data.Reach > 0 ? r.Data.Reach : 1.0f);
            }
        }

        // --- pass 1: Potion (ALCH) ---
        public void BuildPotions()
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
        }

        // --- pass 1: Armor (ARMO) — needs a `template` for an Armature or it equips invisible ---
        public void BuildArmors()
        {
            foreach (var a in spec.Armors)
            {
                var r = mod.Armors.AddNew();
                // A bare ARMO with only a BodyTemplate equips INVISIBLE — the worn mesh lives in the
                // Armature (ARMA addon records), not on the ARMO. Clone a vanilla armor of the desired
                // slot (`template`: "<master>:0xFORMID", e.g. Skyrim.esm:0x00012E49 = ArmorIronCuirass)
                // via DeepCopyIn — that brings its Armature (worn mesh per body part) + WorldModel (ground
                // model) + BodyTemplate — then override identity/stats below. DeepCopyIn keeps OUR FormKey;
                // the ARMA links stay pointed at the template's master, so the vanilla mesh is reused.
                if (!string.IsNullOrWhiteSpace(a.Template))
                {
                    if (TryResolveTemplate<IArmorGetter>(a.Template, out var tmpl) && tmpl is not null)
                        r.DeepCopyIn(tmpl, out _, ArmorCopyMask);
                    else
                        Warn($"  ! armor '{a.EditorId}': template '{a.Template}' not resolved — armor will have NO Armature and equip INVISIBLE; set template to a vanilla armor of the same slot (e.g. Skyrim.esm:0x00012E49 ArmorIronCuirass)");
                }
                else
                    Warn($"  ! armor '{a.EditorId}': no `template` — an ARMO without an Armature equips INVISIBLE (the worn mesh lives on ARMA addons); set template to a vanilla armor (e.g. Skyrim.esm:0x00012E49 ArmorIronCuirass)");
                r.EditorID = a.EditorId; r.Name = a.Name;
                r.Value = a.Value; r.Weight = a.Weight; r.ArmorRating = a.ArmorRating;
                // BodyTemplate = the armor class (light/heavy/clothing) + which biped slots it fills.
                // When templated, the clone already has a correct BodyTemplate; only override it if the
                // spec explicitly states armorType/slots (so the user's choice wins over the template's).
                if (!string.IsNullOrEmpty(a.ArmorType) || a.Slots.Count > 0)
                {
                    var bt = new BodyTemplate { ArmorType = ParseArmorType(a.ArmorType) };
                    foreach (var slot in a.Slots)
                        if (Enum.TryParse<BipedObjectFlag>(slot, ignoreCase: true, out var f)) bt.FirstPersonFlags |= f;
                        else Warn($"  ! armor '{a.EditorId}' unknown slot '{slot}' (e.g. Body, Head, Hands, Feet, Forearms, Calves, Shield)");
                    r.BodyTemplate = bt;
                }
                // External-resource pipeline: a `model` path overrides the cloned ground/world model .nif.
                // (The worn mesh still comes from the template's Armature — a user world model usually pairs
                // WITH a template so the equipped armor stays visible.)
                if (!string.IsNullOrWhiteSpace(a.Model))
                {
                    if (string.IsNullOrWhiteSpace(a.Template))
                        Warn($"  ! armor '{a.EditorId}': `model` set but no `template` — a ground mesh without a template's Armature still equips INVISIBLE; pair `model` with a `template`");
                    ArmorModel Mk() { var am = new ArmorModel { Model = new Model() }; am.Model.File.GivenPath = a.Model; return am; }
                    r.WorldModel = new GenderedItem<ArmorModel?>(Mk(), Mk());
                }
            }
        }
    }
}
