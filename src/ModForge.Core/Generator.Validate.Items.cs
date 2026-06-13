namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // --- items (physical gear, magic, recipes, textures) ---
        public void ValidateItems()
        {
            foreach (var a in spec.Armors) foreach (var k in a.Keywords) CheckRef(k, $"armor '{a.EditorId}' keyword");
            foreach (var w in spec.Weapons) foreach (var k in w.Keywords) CheckRef(k, $"weapon '{w.EditorId}' keyword");
            foreach (var w in spec.Weapons) CheckRef(w.Enchantment, $"weapon '{w.EditorId}' enchantment");
            foreach (var a in spec.Armors) CheckRef(a.Enchantment, $"armor '{a.EditorId}' enchantment");
            foreach (var w in spec.Weapons) if (!string.IsNullOrWhiteSpace(w.Template) && !TryExternalRef(w.Template, out _))
                Problems.Add($"weapon '{w.EditorId}' template: malformed external ref '{w.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x012EB7)");
            foreach (var b in spec.Books)
            {
                if (!string.IsNullOrWhiteSpace(b.Template) && !TryExternalRef(b.Template, out _))
                    Problems.Add($"book '{b.EditorId}' template: malformed external ref '{b.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x0ED161)");
                foreach (var f in b.Flags)
                    if (!Enum.TryParse<Book.Flag>(f, true, out _))
                        Problems.Add($"book '{b.EditorId}' invalid flag '{f}' (e.g. CantBeTaken)");
                // A teaching book (spell tome / skill book) STILL needs a model or it crashes on read.
                // We don't carry a model inline, so require a `template` to clone one from.
                if (b.Teaches is { Kind: { Length: > 0 } } t)
                {
                    if (string.IsNullOrWhiteSpace(b.Template))
                        Problems.Add($"book '{b.EditorId}' teaches something but has no `template` — a takeable/readable book needs a model or it CRASHES on read (clone a vanilla book/tome, e.g. Skyrim.esm:0x10F7F4)");
                    if (t.Kind.Equals("spell", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(t.Spell))
                            Problems.Add($"book '{b.EditorId}' teaches.kind=spell but teaches.spell ref is empty");
                        else if (!LooksExternalRef(t.Spell) && !spellIds.Contains(t.Spell))
                            Problems.Add($"book '{b.EditorId}' teaches.spell '{t.Spell}' is not an in-spec spell (it must be a SPEL — use an in-spec spell editorId or a vanilla <master>:0xFORMID)");
                        else CheckRef(t.Spell, $"book '{b.EditorId}' teaches.spell");
                    }
                    else if (t.Kind.Equals("skill", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(t.Skill))
                            Problems.Add($"book '{b.EditorId}' teaches.kind=skill but teaches.skill is empty");
                        else if (!Enum.TryParse<Skill>(t.Skill, true, out _))
                            Problems.Add($"book '{b.EditorId}' teaches.skill '{t.Skill}' is not a valid Skill (e.g. Destruction, OneHanded, Smithing)");
                    }
                    else
                        Problems.Add($"book '{b.EditorId}' teaches.kind '{t.Kind}' invalid (spell|skill)");
                }
            }
            foreach (var m in spec.MiscItems) if (!string.IsNullOrWhiteSpace(m.Template) && !TryExternalRef(m.Template, out _))
                Problems.Add($"miscItem '{m.EditorId}' template: malformed external ref '{m.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x063B42)");
            foreach (var p in spec.Potions) if (!string.IsNullOrWhiteSpace(p.Template) && !TryExternalRef(p.Template, out _))
                Problems.Add($"potion '{p.EditorId}' template: malformed external ref '{p.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x039BE5)");
            foreach (var m in spec.MiscItems) foreach (var k in m.Keywords) CheckRef(k, $"miscItem '{m.EditorId}' keyword");

            // External-resource pipeline — model paths, sound file shapes, sound refs.
            foreach (var st in spec.Statics) CheckModelPath(st.Model, $"static '{st.EditorId}'");
            foreach (var ac in spec.Activators) CheckModelPath(ac.Model, $"activator '{ac.EditorId}'");
            foreach (var fn in spec.Furniture) CheckModelPath(fn.Model, $"furniture '{fn.EditorId}'");
            foreach (var m in spec.MiscItems) CheckModelPath(m.Model, $"miscItem '{m.EditorId}'");
            foreach (var w in spec.Weapons) CheckModelPath(w.Model, $"weapon '{w.EditorId}'");
            foreach (var fn in spec.Furniture) foreach (var k in fn.Keywords) CheckRef(k, $"furniture '{fn.EditorId}' keyword");
            foreach (var sd in spec.Sounds)
            {
                if (sd.Files.Count == 0)
                    Problems.Add($"sound '{sd.EditorId}' has no files (a SoundDescriptor needs at least one .wav/.xwm)");
                foreach (var f in sd.Files) CheckSoundFile(f, $"sound '{sd.EditorId}'");
                CheckRef(sd.Category, $"sound '{sd.EditorId}' category");
                CheckRef(sd.OutputModel, $"sound '{sd.EditorId}' outputModel");
            }
            foreach (var ac in spec.Activators)
            { CheckRef(ac.ActivationSound, $"activator '{ac.EditorId}' activationSound"); CheckRef(ac.LoopingSound, $"activator '{ac.EditorId}' loopingSound"); }
            foreach (var m in spec.MiscItems)
            { CheckRef(m.PickUpSound, $"miscItem '{m.EditorId}' pickUpSound"); CheckRef(m.PutDownSound, $"miscItem '{m.EditorId}' putDownSound"); }
            foreach (var w in spec.Weapons)
            { CheckRef(w.PickUpSound, $"weapon '{w.EditorId}' pickUpSound"); CheckRef(w.PutDownSound, $"weapon '{w.EditorId}' putDownSound"); }

            var armorTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "light", "heavy", "clothing", "lightarmor", "heavyarmor" };
            foreach (var a in spec.Armors)
            {
                if (!string.IsNullOrEmpty(a.ArmorType) && !armorTypes.Contains(a.ArmorType))
                    Problems.Add($"armor '{a.EditorId}' has invalid armorType '{a.ArmorType}' (light|heavy|clothing)");
                foreach (var slot in a.Slots)
                    if (!Enum.TryParse<BipedObjectFlag>(slot, ignoreCase: true, out _))
                        Problems.Add($"armor '{a.EditorId}' has invalid slot '{slot}' (e.g. Body, Head, Hands, Feet, Forearms, Calves, Shield)");
            }

            // MagicEffect (MGEF) enum + ref checks.
            foreach (var me in spec.MagicEffects)
            {
                CheckEnum<MagicEffectArchetype.TypeEnum>(me.Archetype, $"magicEffect '{me.EditorId}' archetype");
                CheckEnum<ActorValue>(me.ActorValue, $"magicEffect '{me.EditorId}' actorValue");
                CheckEnum<ActorValue>(me.SecondActorValue, $"magicEffect '{me.EditorId}' secondActorValue");
                CheckEnum<ActorValue>(me.MagicSkill, $"magicEffect '{me.EditorId}' magicSkill");
                CheckEnum<ActorValue>(me.ResistValue, $"magicEffect '{me.EditorId}' resistValue");
                CheckEnum<CastType>(me.CastType, $"magicEffect '{me.EditorId}' castType");
                CheckEnum<TargetType>(me.TargetType, $"magicEffect '{me.EditorId}' targetType");
                foreach (var f in me.Flags) CheckEnum<MagicEffect.Flag>(f, $"magicEffect '{me.EditorId}' flag");
                CheckRef(me.Association, $"magicEffect '{me.EditorId}' association");
                CheckRef(me.Projectile, $"magicEffect '{me.EditorId}' projectile");
                CheckRef(me.CastingArt, $"magicEffect '{me.EditorId}' castingArt");
                CheckRef(me.HitEffectArt, $"magicEffect '{me.EditorId}' hitEffectArt");
                CheckRef(me.Explosion, $"magicEffect '{me.EditorId}' explosion");
            }
            foreach (var cl in spec.Classes)
            {
                CheckEnum<Skill>(cl.Teaches, $"class '{cl.EditorId}' teaches");
                foreach (var sk in cl.SkillWeights.Keys) CheckEnum<Skill>(sk, $"class '{cl.EditorId}' skillWeight key");
            }
            foreach (var s in spec.Spells) CheckEffects(s.EditorId, s.Effects, "spell");
            foreach (var s in spec.Spells) CheckRef(s.EquipType, $"spell '{s.EditorId}' equipType");
            foreach (var p in spec.Potions) CheckEffects(p.EditorId, p.Effects, "potion");

            ValidateItems2();
        }
    }
}
