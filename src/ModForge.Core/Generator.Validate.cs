namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Validate — semantic guardrail for (LLM-authored) specs: editorId presence +
    //  uniqueness, and referential integrity (dialogue→quest/npc, npc→faction,
    //  script→target, object-property→record, property types). Returns the list of
    //  problems (empty == valid) so an NL→spec front can self-correct before build.
    // -------------------------------------------------------------------------------
    /// <summary>Semantically validate a spec. Returns the list of problems; empty means valid.</summary>
    public static IReadOnlyList<string> Validate(ModSpec spec)
    {
        var problems = new List<string>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var npcIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var questIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var factionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cellIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var spellIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Reg(string ed, string what, HashSet<string>? typed = null)
        {
            if (string.IsNullOrWhiteSpace(ed)) { problems.Add($"{what}: empty editorId"); return; }
            if (!ids.Add(ed)) problems.Add($"duplicate editorId '{ed}' (at {what})");
            typed?.Add(ed);
        }

        foreach (var m in spec.MiscItems) Reg(m.EditorId, "miscItem");
        foreach (var b in spec.Books) Reg(b.EditorId, "book");
        foreach (var w in spec.Weapons) Reg(w.EditorId, "weapon");
        foreach (var n in spec.Npcs) Reg(n.EditorId, "npc", npcIds);
        foreach (var q in spec.Quests) Reg(q.EditorId, "quest", questIds);
        foreach (var s in spec.Spells) Reg(s.EditorId, "spell", spellIds);
        foreach (var p in spec.Potions) Reg(p.EditorId, "potion");
        foreach (var a in spec.Armors) Reg(a.EditorId, "armor");
        var placementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pl in spec.Placements) if (!string.IsNullOrWhiteSpace(pl.EditorId)) placementIds.Add(pl.EditorId);
        foreach (var f in spec.Factions) Reg(f.EditorId, "faction", factionIds);
        foreach (var msg in spec.Messages) Reg(msg.EditorId, "message");
        foreach (var d in spec.Dialogue) Reg(d.EditorId, "dialogue");
        foreach (var sc in spec.Scenes) Reg(sc.EditorId, "scene");
        foreach (var c in spec.Cells)
        {
            Reg(c.EditorId, "cell", cellIds);
            if (!string.IsNullOrWhiteSpace(c.Template) && !TryExternalRef(c.Template, out _))
                problems.Add($"cell '{c.EditorId}' template '{c.Template}' must be an external <master>:0xFORMID interior-cell ref");
        }
        foreach (var li in spec.LeveledItems) Reg(li.EditorId, "leveledItem");
        foreach (var ln in spec.LeveledNpcs) Reg(ln.EditorId, "leveledNpc");
        foreach (var ct in spec.Containers) Reg(ct.EditorId, "container");
        foreach (var i in spec.Ingredients) Reg(i.EditorId, "ingredient");
        foreach (var a in spec.Ammunitions) Reg(a.EditorId, "ammunition");
        foreach (var s in spec.Scrolls) Reg(s.EditorId, "scroll");
        foreach (var sg in spec.SoulGems) Reg(sg.EditorId, "soulGem");
        foreach (var k in spec.Keys) Reg(k.EditorId, "key");
        foreach (var kw in spec.Keywords) Reg(kw.EditorId, "keyword");
        foreach (var o in spec.Outfits) Reg(o.EditorId, "outfit");
        foreach (var st in spec.Statics) Reg(st.EditorId, "static");
        foreach (var ac in spec.Activators) Reg(ac.EditorId, "activator");
        foreach (var me in spec.MagicEffects) Reg(me.EditorId, "magicEffect");
        foreach (var co in spec.Recipes) Reg(co.EditorId, "recipe");
        foreach (var cl in spec.Classes) Reg(cl.EditorId, "class");
        foreach (var pk in spec.Packages) Reg(pk.EditorId, "package");
        foreach (var cs in spec.CombatStyles) Reg(cs.EditorId, "combatStyle");
        foreach (var rel in spec.Relationships) Reg(rel.EditorId, "relationship");
        foreach (var w in spec.WordsOfPower) Reg(w.EditorId, "wordOfPower");
        foreach (var sh in spec.Shouts) Reg(sh.EditorId, "shout");
        foreach (var e in spec.Enchantments) Reg(e.EditorId, "enchantment");
        foreach (var tx in spec.TextureSets) Reg(tx.EditorId, "textureSet");
        foreach (var w in spec.Weathers) Reg(w.EditorId, "weather");
        foreach (var cl in spec.Climates) Reg(cl.EditorId, "climate");
        foreach (var ws in spec.Worldspaces) Reg(ws.EditorId, "worldspace");
        foreach (var rg in spec.Regions) Reg(rg.EditorId, "region");
        foreach (var ez in spec.EncounterZones) Reg(ez.EditorId, "encounterZone");
        foreach (var pl in spec.Placements) if (!string.IsNullOrWhiteSpace(pl.EditorId)) Reg(pl.EditorId, "placement");

        // A ref must be an in-spec editorId OR a well-formed external "<master>:0xFORMID".
        void CheckRef(string r, string what)
        {
            if (string.IsNullOrWhiteSpace(r)) return;
            if (LooksExternalRef(r))
            { if (!TryExternalRef(r, out _)) problems.Add($"{what}: malformed external ref '{r}' (expect <master>:0xFORMID)"); }
            else if (!ids.Contains(r))
                problems.Add($"{what}: unresolved ref '{r}' (unknown in-spec editorId; for vanilla forms use <master>:0xFORMID)");
        }

        foreach (var n in spec.Npcs)
        {
            foreach (var fac in n.Factions)
                if (LooksExternalRef(fac))
                { if (!TryExternalRef(fac, out _)) problems.Add($"npc '{n.EditorId}' faction: malformed external ref '{fac}'"); }
                else if (!factionIds.Contains(fac))
                    problems.Add($"npc '{n.EditorId}' references unknown faction '{fac}' (in-spec, non-faction or typo; vanilla faction -> <master>:0xFORMID)");
            CheckRef(n.Race, $"npc '{n.EditorId}' race");
            CheckRef(n.Class, $"npc '{n.EditorId}' class");
            CheckRef(n.Outfit, $"npc '{n.EditorId}' outfit");
            CheckRef(n.VoiceType, $"npc '{n.EditorId}' voiceType");
            CheckRef(n.CrimeFaction, $"npc '{n.EditorId}' crimeFaction");
            CheckRef(n.CombatStyle, $"npc '{n.EditorId}' combatStyle");
            foreach (var s in n.Spells) CheckRef(s, $"npc '{n.EditorId}' spell");
            CheckEnum<Aggression>(n.Aggression, $"npc '{n.EditorId}' aggression");
            CheckEnum<Confidence>(n.Confidence, $"npc '{n.EditorId}' confidence");
            CheckEnum<Assistance>(n.Assistance, $"npc '{n.EditorId}' assistance");
            CheckEnum<Mood>(n.Mood, $"npc '{n.EditorId}' mood");
        }
        foreach (var cs in spec.CombatStyles)
            foreach (var f in cs.Flags)
                if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.CombatStyle.Flag>(f, true, out _))
                    problems.Add($"combatStyle '{cs.EditorId}' invalid flag '{f}' (Dueling|Flanking|AllowDualWielding)");
        foreach (var a in spec.Armors) foreach (var k in a.Keywords) CheckRef(k, $"armor '{a.EditorId}' keyword");
        foreach (var w in spec.Weapons) foreach (var k in w.Keywords) CheckRef(k, $"weapon '{w.EditorId}' keyword");
        // `enchantment` is a ref → an in-spec ENCH (enchantments[]) or a vanilla ObjectEffect.
        foreach (var w in spec.Weapons) CheckRef(w.Enchantment, $"weapon '{w.EditorId}' enchantment");
        foreach (var a in spec.Armors) CheckRef(a.Enchantment, $"armor '{a.EditorId}' enchantment");
        // `template` = a vanilla record to clone (model/anim) — must be a well-formed external ref.
        foreach (var w in spec.Weapons) if (!string.IsNullOrWhiteSpace(w.Template) && !TryExternalRef(w.Template, out _))
            problems.Add($"weapon '{w.EditorId}' template: malformed external ref '{w.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x012EB7)");
        foreach (var b in spec.Books)
        {
            if (!string.IsNullOrWhiteSpace(b.Template) && !TryExternalRef(b.Template, out _))
                problems.Add($"book '{b.EditorId}' template: malformed external ref '{b.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x0ED161)");
            foreach (var f in b.Flags)
                if (!Enum.TryParse<Book.Flag>(f, true, out _))
                    problems.Add($"book '{b.EditorId}' invalid flag '{f}' (e.g. CantBeTaken)");
            // A teaching book (spell tome / skill book) STILL needs a model or it crashes on read.
            // We don't carry a model inline, so require a `template` to clone one from.
            if (b.Teaches is { Kind: { Length: > 0 } } t)
            {
                if (string.IsNullOrWhiteSpace(b.Template))
                    problems.Add($"book '{b.EditorId}' teaches something but has no `template` — a takeable/readable book needs a model or it CRASHES on read (clone a vanilla book/tome, e.g. Skyrim.esm:0x10F7F4)");
                if (t.Kind.Equals("spell", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(t.Spell))
                        problems.Add($"book '{b.EditorId}' teaches.kind=spell but teaches.spell ref is empty");
                    else if (!LooksExternalRef(t.Spell) && !spellIds.Contains(t.Spell))
                        problems.Add($"book '{b.EditorId}' teaches.spell '{t.Spell}' is not an in-spec spell (it must be a SPEL — use an in-spec spell editorId or a vanilla <master>:0xFORMID)");
                    else CheckRef(t.Spell, $"book '{b.EditorId}' teaches.spell");
                }
                else if (t.Kind.Equals("skill", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(t.Skill))
                        problems.Add($"book '{b.EditorId}' teaches.kind=skill but teaches.skill is empty");
                    else if (!Enum.TryParse<Skill>(t.Skill, true, out _))
                        problems.Add($"book '{b.EditorId}' teaches.skill '{t.Skill}' is not a valid Skill (e.g. Destruction, OneHanded, Smithing)");
                }
                else
                    problems.Add($"book '{b.EditorId}' teaches.kind '{t.Kind}' invalid (spell|skill)");
            }
        }
        foreach (var m in spec.MiscItems) if (!string.IsNullOrWhiteSpace(m.Template) && !TryExternalRef(m.Template, out _))
            problems.Add($"miscItem '{m.EditorId}' template: malformed external ref '{m.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x063B42)");
        foreach (var p in spec.Potions) if (!string.IsNullOrWhiteSpace(p.Template) && !TryExternalRef(p.Template, out _))
            problems.Add($"potion '{p.EditorId}' template: malformed external ref '{p.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x039BE5)");
        foreach (var m in spec.MiscItems) foreach (var k in m.Keywords) CheckRef(k, $"miscItem '{m.EditorId}' keyword");

        var armorTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "light", "heavy", "clothing", "lightarmor", "heavyarmor" };
        foreach (var a in spec.Armors)
        {
            if (!string.IsNullOrEmpty(a.ArmorType) && !armorTypes.Contains(a.ArmorType))
                problems.Add($"armor '{a.EditorId}' has invalid armorType '{a.ArmorType}' (light|heavy|clothing)");
            foreach (var slot in a.Slots)
                if (!Enum.TryParse<BipedObjectFlag>(slot, ignoreCase: true, out _))
                    problems.Add($"armor '{a.EditorId}' has invalid slot '{slot}' (e.g. Body, Head, Hands, Feet, Forearms, Calves, Shield)");
        }

        // A spell/potion effect needs a MagicEffect ref (in-spec or, normally, a vanilla one).
        void CheckEffects(string ed, List<EffectSpec> effects, string kind)
        {
            foreach (var e in effects)
                if (string.IsNullOrWhiteSpace(e.MagicEffect)) problems.Add($"{kind} '{ed}' has an effect with empty magicEffect ref");
                else CheckRef(e.MagicEffect, $"{kind} '{ed}' effect magicEffect");
        }
        foreach (var s in spec.Spells) CheckEffects(s.EditorId, s.Effects, "spell");
        foreach (var s in spec.Spells) CheckRef(s.EquipType, $"spell '{s.EditorId}' equipType");
        foreach (var p in spec.Potions) CheckEffects(p.EditorId, p.Effects, "potion");

        // MagicEffect (MGEF): every authored enum string must parse; association (if set) is a ref.
        void CheckEnum<TEnum>(string val, string what) where TEnum : struct, Enum
        { if (!string.IsNullOrWhiteSpace(val) && !Enum.TryParse<TEnum>(val, ignoreCase: true, out _)) problems.Add($"{what} '{val}' invalid"); }
        foreach (var me in spec.MagicEffects)
        {
            CheckEnum<MagicEffectArchetype.TypeEnum>(me.Archetype, $"magicEffect '{me.EditorId}' archetype");
            CheckEnum<ActorValue>(me.ActorValue, $"magicEffect '{me.EditorId}' actorValue");
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

        foreach (var d in spec.Dialogue)
        {
            if (!questIds.Contains(d.QuestEditorId)) problems.Add($"dialogue '{d.EditorId}' references unknown quest '{d.QuestEditorId}'");
            if (!string.IsNullOrEmpty(d.SpeakerNpcEditorId) && !npcIds.Contains(d.SpeakerNpcEditorId))
                problems.Add($"dialogue '{d.EditorId}' references unknown speaker npc '{d.SpeakerNpcEditorId}'");
            if (string.IsNullOrEmpty(d.Prompt)) problems.Add($"dialogue '{d.EditorId}' has empty prompt");
            if (d.Responses.Count == 0) problems.Add($"dialogue '{d.EditorId}' has no response lines");
            if (!Enum.TryParse<Emotion>(d.Emotion, true, out _))
                problems.Add($"dialogue '{d.EditorId}' invalid emotion '{d.Emotion}' (Neutral|Anger|Disgust|Fear|Sad|Happy|Surprise)");
        }

        // SCENE (SCEN): host quest must exist; actors need a (unique) aliasId + an NPC; every phase must
        // name a speaker that is one of the scene's actors and carry at least one line.
        foreach (var sc in spec.Scenes)
        {
            if (!questIds.Contains(sc.QuestEditorId))
                problems.Add($"scene '{sc.EditorId}' references unknown quest '{sc.QuestEditorId}'");
            if (sc.Actors.Count == 0)
                problems.Add($"scene '{sc.EditorId}' has no actors (a scene needs at least two NPCs talking to each other)");
            var sceneAliasIds = new HashSet<int>();
            foreach (var a in sc.Actors)
            {
                if (a.AliasId < 0) problems.Add($"scene '{sc.EditorId}' actor has negative aliasId {a.AliasId}");
                else if (!sceneAliasIds.Add(a.AliasId)) problems.Add($"scene '{sc.EditorId}' duplicate actor aliasId {a.AliasId}");
                if (string.IsNullOrWhiteSpace(a.Npc)) problems.Add($"scene '{sc.EditorId}' actor (alias {a.AliasId}) has empty npc ref");
                else CheckRef(a.Npc, $"scene '{sc.EditorId}' actor (alias {a.AliasId}) npc");
            }
            if (sc.Phases.Count == 0)
                problems.Add($"scene '{sc.EditorId}' has no phases (nothing is spoken)");
            for (int i = 0; i < sc.Phases.Count; i++)
            {
                var ph = sc.Phases[i];
                if (!sceneAliasIds.Contains(ph.Speaker))
                    problems.Add($"scene '{sc.EditorId}' phase {i} speaker aliasId {ph.Speaker} is not one of the scene's actors");
                if (ph.Lines.Count == 0)
                    problems.Add($"scene '{sc.EditorId}' phase {i} has no lines");
                if (!Enum.TryParse<Emotion>(ph.Emotion, true, out _))
                    problems.Add($"scene '{sc.EditorId}' phase {i} invalid emotion '{ph.Emotion}' (Neutral|Anger|Disgust|Fear|Sad|Happy|Surprise)");
            }
        }

        var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "int", "float", "bool", "string", "object" };
        foreach (var sa in spec.Scripts)
        {
            if (string.IsNullOrEmpty(sa.ScriptName)) problems.Add($"script attach on '{sa.TargetEditorId}' has empty scriptName");
            if (!ids.Contains(sa.TargetEditorId)) problems.Add($"script '{sa.ScriptName}' targets unknown record '{sa.TargetEditorId}'");
            foreach (var p in sa.Properties)
            {
                if (!validTypes.Contains(p.Type)) problems.Add($"script '{sa.ScriptName}' prop '{p.Name}' has invalid type '{p.Type}'");
                if (string.Equals(p.Type, "object", StringComparison.OrdinalIgnoreCase))
                    CheckRef(p.ObjectEditorId, $"script '{sa.ScriptName}' prop '{p.Name}' object");
            }
        }

        foreach (var pl in spec.Placements)
        {
            CheckRef(pl.Base, "placement base");
            if (!string.IsNullOrWhiteSpace(pl.Worldspace))
            {
                // Exterior placement: worldspace + world position (cell is derived, not authored).
                // Worldspaces aren't built in-spec, so the ref must be a well-formed external one.
                if (!LooksExternalRef(pl.Worldspace) || !TryExternalRef(pl.Worldspace, out _))
                    problems.Add($"placement worldspace '{pl.Worldspace}' must be a well-formed external <master>:0xFORMID ref (find it: find <Skyrim.esm> <name> Worldspace)");
            }
            else if (string.IsNullOrWhiteSpace(pl.Cell)) problems.Add("placement has empty cell (and no worldspace — set one or the other)");
            else if (LooksExternalRef(pl.Cell))
            { if (!TryExternalRef(pl.Cell, out _)) problems.Add($"placement: malformed external cell ref '{pl.Cell}' (expect <master>:0xFORMID)"); }
            else if (!cellIds.Contains(pl.Cell)) problems.Add($"placement references unknown cell '{pl.Cell}' (in-spec cell editorId or <master>:0xFORMID)");
            if (!string.IsNullOrEmpty(pl.Kind) && !pl.Kind.Equals("npc", StringComparison.OrdinalIgnoreCase) && !pl.Kind.Equals("object", StringComparison.OrdinalIgnoreCase))
                problems.Add($"placement kind '{pl.Kind}' invalid (npc|object)");
            foreach (var lr in pl.LinkedRefs)
            {
                if (string.IsNullOrWhiteSpace(lr.Target)) problems.Add($"placement '{pl.EditorId}' linkedRef has empty target");
                else CheckRef(lr.Target, $"placement '{pl.EditorId}' linkedRef target");
                CheckRef(lr.Keyword, $"placement '{pl.EditorId}' linkedRef keyword");
            }
            if (pl.LinkedRefs.Count > 0 && string.IsNullOrWhiteSpace(pl.EditorId))
                problems.Add("placement has linkedRefs but no editorId (a linked-ref source must be named so the route can be wired)");
        }

        foreach (var li in spec.LeveledItems)
        {
            foreach (var e in li.Entries) CheckRef(e.Reference, $"leveledItem '{li.EditorId}' entry");
            foreach (var f in li.Flags) if (!Enum.TryParse<LeveledItem.Flag>(f, true, out _)) problems.Add($"leveledItem '{li.EditorId}' invalid flag '{f}'");
        }
        foreach (var ln in spec.LeveledNpcs)
        {
            foreach (var e in ln.Entries) CheckRef(e.Reference, $"leveledNpc '{ln.EditorId}' entry");
            foreach (var f in ln.Flags) if (!Enum.TryParse<LeveledNpc.Flag>(f, true, out _)) problems.Add($"leveledNpc '{ln.EditorId}' invalid flag '{f}'");
        }
        foreach (var ct in spec.Containers)
            foreach (var e in ct.Items) CheckRef(e.Item, $"container '{ct.EditorId}' item");

        // In-spec weapon/armor editorIds — a temper recipe's target must be one of these (or an
        // external <master>:0xID weapon/armor, which we can't type-check headlessly).
        var weaponArmorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in spec.Weapons) weaponArmorIds.Add(w.EditorId);
        foreach (var a in spec.Armors)  weaponArmorIds.Add(a.EditorId);

        foreach (var co in spec.Recipes)
        {
            var kind = NormalizeKind(co.Kind);
            if (!KnownRecipeKinds.Contains(kind))
                problems.Add($"recipe '{co.EditorId}' invalid kind '{co.Kind}' (use {string.Join("/", KnownRecipeKinds)})");

            if (string.IsNullOrWhiteSpace(co.CreatedObject)) problems.Add($"recipe '{co.EditorId}' has empty createdObject");
            else CheckRef(co.CreatedObject, $"recipe '{co.EditorId}' createdObject");

            // Workbench: a named selector OR a ref. A named selector is always valid; otherwise it
            // must be a resolvable ref. Empty -> the kind default (forge/wheel/smelter), also fine.
            if (!string.IsNullOrWhiteSpace(co.Workbench)
                && !KnownWorkbenchNames.Contains(co.Workbench.Trim()))
                CheckRef(co.Workbench, $"recipe '{co.EditorId}' workbench");

            if (co.Components.Count == 0) problems.Add($"recipe '{co.EditorId}' has no components (nothing to consume)");
            foreach (var comp in co.Components) CheckRef(comp.Item, $"recipe '{co.EditorId}' component");

            // Temper guardrail: the improved item must be an in-spec weapon/armor (or an external
            // ref, unverifiable here). A temper recipe whose createdObject is a misc item won't show
            // up as an improvement at the bench.
            if (kind == "temper" && !string.IsNullOrWhiteSpace(co.CreatedObject)
                && !LooksExternalRef(co.CreatedObject) && !weaponArmorIds.Contains(co.CreatedObject))
                problems.Add($"recipe '{co.EditorId}' kind=temper createdObject '{co.CreatedObject}' is not an in-spec weapon/armor (temper improves a weapon/armor)");

            // Conditions (shared ConditionSpec): function known + param present when the function
            // needs one. `param` is the form arg (perk/item/global); TemperIsEnchanted takes none.
            foreach (var cnd in co.Conditions)
            {
                if (!IsKnownRecipeFunction(cnd.Function))
                { problems.Add($"recipe '{co.EditorId}' condition: unknown function '{cnd.Function}' (HasPerk/GetItemCount/GetGlobalValue/TemperIsEnchanted)"); continue; }
                if (!IsValidCompareOp(cnd.Comparison))
                    problems.Add($"recipe '{co.EditorId}' condition: invalid comparison '{cnd.Comparison}' (== != > >= < <=)");
                if (RecipeFunctionNeedsRef(cnd.Function))
                {
                    if (string.IsNullOrWhiteSpace(cnd.Param))
                        problems.Add($"recipe '{co.EditorId}' condition '{cnd.Function}' needs a param (perk/item/global ref)");
                    else CheckRef(cnd.Param, $"recipe '{co.EditorId}' condition '{cnd.Function}' param");
                }
            }
        }

        foreach (var s in spec.Spells)
        {
            if (!string.IsNullOrEmpty(s.SpellType) && !Enum.TryParse<SpellType>(s.SpellType, true, out _)) problems.Add($"spell '{s.EditorId}' invalid spellType '{s.SpellType}'");
            if (!string.IsNullOrEmpty(s.CastType) && !Enum.TryParse<CastType>(s.CastType, true, out _)) problems.Add($"spell '{s.EditorId}' invalid castType '{s.CastType}'");
            if (!string.IsNullOrEmpty(s.TargetType) && !Enum.TryParse<TargetType>(s.TargetType, true, out _)) problems.Add($"spell '{s.EditorId}' invalid targetType '{s.TargetType}'");
        }

        // Enchantment (ENCH/ObjectEffect): enchantType in {weapon|apparel|staff}; ≥1 effect with a
        // resolvable MGEF ref; optional cast/target enum overrides. cost/charge are uint/float, so
        // non-negative by type. An item references one via weapon/armor `enchantment` (checked above).
        foreach (var e in spec.Enchantments)
        {
            if (string.IsNullOrWhiteSpace(e.EnchantType) || !EnchantTypes.Contains(e.EnchantType))
                problems.Add($"enchantment '{e.EditorId}' invalid enchantType '{e.EnchantType}' (weapon|apparel|staff)");
            if (e.Effects.Count == 0) problems.Add($"enchantment '{e.EditorId}' has no effects (an ENCH needs ≥1 MGEF-based effect)");
            CheckEffects(e.EditorId, e.Effects, "enchantment");
            if (!string.IsNullOrEmpty(e.CastType) && !Enum.TryParse<CastType>(e.CastType, true, out _))
                problems.Add($"enchantment '{e.EditorId}' invalid castType '{e.CastType}' (FireAndForget|Concentration|ConstantEffect)");
            if (!string.IsNullOrEmpty(e.TargetType) && !Enum.TryParse<TargetType>(e.TargetType, true, out _))
                problems.Add($"enchantment '{e.EditorId}' invalid targetType '{e.TargetType}' (Self|Touch|Aimed|TargetActor|TargetLocation)");
        }

        // --- long-tail record types: keyword/effect refs + enum fields ---
        foreach (var i in spec.Ingredients)
        {
            foreach (var k in i.Keywords) CheckRef(k, $"ingredient '{i.EditorId}' keyword");
            CheckEffects(i.EditorId, i.Effects, "ingredient");
        }
        foreach (var a in spec.Ammunitions)
            foreach (var k in a.Keywords) CheckRef(k, $"ammunition '{a.EditorId}' keyword");
        foreach (var s in spec.Scrolls)
        {
            foreach (var k in s.Keywords) CheckRef(k, $"scroll '{s.EditorId}' keyword");
            CheckEffects(s.EditorId, s.Effects, "scroll");
            if (!string.IsNullOrEmpty(s.SpellType) && !Enum.TryParse<SpellType>(s.SpellType, true, out _)) problems.Add($"scroll '{s.EditorId}' invalid spellType '{s.SpellType}'");
            if (!string.IsNullOrEmpty(s.CastType) && !Enum.TryParse<CastType>(s.CastType, true, out _)) problems.Add($"scroll '{s.EditorId}' invalid castType '{s.CastType}'");
            if (!string.IsNullOrEmpty(s.TargetType) && !Enum.TryParse<TargetType>(s.TargetType, true, out _)) problems.Add($"scroll '{s.EditorId}' invalid targetType '{s.TargetType}'");
        }
        foreach (var sg in spec.SoulGems)
        {
            foreach (var k in sg.Keywords) CheckRef(k, $"soulGem '{sg.EditorId}' keyword");
            if (!string.IsNullOrEmpty(sg.MaximumCapacity) && !Enum.TryParse<SoulGem.Level>(sg.MaximumCapacity, true, out _))
                problems.Add($"soulGem '{sg.EditorId}' invalid maximumCapacity '{sg.MaximumCapacity}' (None|Petty|Lesser|Common|Greater|Grand)");
        }
        foreach (var k in spec.Keys)
            foreach (var kw in k.Keywords) CheckRef(kw, $"key '{k.EditorId}' keyword");
        foreach (var ac in spec.Activators)
            foreach (var kw in ac.Keywords) CheckRef(kw, $"activator '{ac.EditorId}' keyword");
        foreach (var o in spec.Outfits)
            foreach (var it in o.Items) CheckRef(it, $"outfit '{o.EditorId}' item");

        // AI Packages (PACK): template is required (and must be a well-formed external ref —
        // there are no in-spec procedure templates); refs (template/combatStyle/ownerQuest +
        // sandbox.location) checked; enums (Flag/InterruptFlag/Speed/DayOfWeek) parse-checked.
        foreach (var pk in spec.Packages)
        {
            if (string.IsNullOrWhiteSpace(pk.Template))
                problems.Add($"package '{pk.EditorId}' has empty template (need <master>:0xFORMID of a procedure template, e.g. Skyrim.esm:0x01C254 = Sandbox)");
            else if (!LooksExternalRef(pk.Template) || !TryExternalRef(pk.Template, out _))
                problems.Add($"package '{pk.EditorId}' template '{pk.Template}' must be a well-formed external <master>:0xFORMID ref");
            CheckRef(pk.CombatStyle, $"package '{pk.EditorId}' combatStyle");
            CheckRef(pk.OwnerQuest,  $"package '{pk.EditorId}' ownerQuest");
            CheckRef(pk.Sandbox.Location, $"package '{pk.EditorId}' sandbox.location");
            CheckRef(pk.Sleep.Location,   $"package '{pk.EditorId}' sleep.location");   // optional ⇒ editor location
            CheckRef(pk.Travel.Place, $"package '{pk.EditorId}' travel.place");
            CheckRef(pk.UseMagic.Location, $"package '{pk.EditorId}' useMagic.location");
            CheckRef(pk.UseMagic.Target,   $"package '{pk.EditorId}' useMagic.target");
            CheckRef(pk.UseMagic.Spell,    $"package '{pk.EditorId}' useMagic.spell");
            CheckRef(pk.Patrol.Start,      $"package '{pk.EditorId}' patrol.start");
            if (LooksExternalRef(pk.Template) && TryExternalRef(pk.Template, out var ptfk) && ptfk == PackageTemplates.Patrol
                && string.IsNullOrWhiteSpace(pk.Patrol.Start))
                problems.Add($"package '{pk.EditorId}' uses Patrol template but patrol.start is empty — NPC has no route and won't patrol");
            CheckRef(pk.Follow.Target,     $"package '{pk.EditorId}' follow.target");   // empty ⇒ defaults to the player
            CheckRef(pk.Escort.Target,      $"package '{pk.EditorId}' escort.target");   // empty ⇒ defaults to the player
            CheckRef(pk.Escort.Destination, $"package '{pk.EditorId}' escort.destination");
            if (LooksExternalRef(pk.Template) && TryExternalRef(pk.Template, out var etfk) && etfk == PackageTemplates.Escort
                && string.IsNullOrWhiteSpace(pk.Escort.Destination))
                problems.Add($"package '{pk.EditorId}' uses Escort template but escort.destination is empty — NPC won't lead anywhere (falls back to NearSelf)");
            // useMagic.spell is required only when the template is UseMagic — `Resolve`-style
            // template-id check is in Build, so here just warn for UseMagic-template packages.
            if (LooksExternalRef(pk.Template) && TryExternalRef(pk.Template, out var tfk) && tfk == PackageTemplates.UseMagic
                && string.IsNullOrWhiteSpace(pk.UseMagic.Spell))
                problems.Add($"package '{pk.EditorId}' uses UseMagic template but useMagic.spell is empty — package will no-op in-game");
            foreach (var f in pk.Flags)
                if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.Package.Flag>(f, true, out _))
                    problems.Add($"package '{pk.EditorId}' invalid flag '{f}'");
            foreach (var f in pk.InterruptFlags)
                if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.Package.InterruptFlag>(f, true, out _))
                    problems.Add($"package '{pk.EditorId}' invalid interruptFlag '{f}' (e.g. HellosToPlayer, AllowIdleChatter, WorldInteractions)");
            if (!string.IsNullOrEmpty(pk.PreferredSpeed)
                && !Enum.TryParse<Mutagen.Bethesda.Skyrim.Package.Speed>(pk.PreferredSpeed, true, out _))
                problems.Add($"package '{pk.EditorId}' invalid preferredSpeed '{pk.PreferredSpeed}' (Walk|Jog|Run|FastWalk)");
            if (!string.IsNullOrEmpty(pk.Schedule.DayOfWeek)
                && !Enum.TryParse<Mutagen.Bethesda.Skyrim.Package.DayOfWeek>(pk.Schedule.DayOfWeek, true, out _))
                problems.Add($"package '{pk.EditorId}' invalid schedule.dayOfWeek '{pk.Schedule.DayOfWeek}' (Sunday|Monday|…|Weekdays|Weekends|Any)");
        }
        foreach (var rel in spec.Relationships)
        {
            CheckRef(rel.Parent, $"relationship '{rel.EditorId}' parent");
            CheckRef(rel.Child,  $"relationship '{rel.EditorId}' child");
            if (string.IsNullOrWhiteSpace(rel.Parent))
                problems.Add($"relationship '{rel.EditorId}' has no parent NPC");
            if (!Enum.TryParse<Relationship.RankType>(rel.Rank, true, out _))
                problems.Add($"relationship '{rel.EditorId}' invalid rank '{rel.Rank}' (Lover|Ally|Confidant|Friend|Acquaintance|Rival|Foe|Enemy|Archnemesis)");
        }
        foreach (var n in spec.Npcs)
            foreach (var pkgRef in n.Packages) CheckRef(pkgRef, $"npc '{n.EditorId}' package");

        // TextureSet (TXST): a texture-map path must be a `.dds` string RELATIVE TO Data\Textures\
        // (the slot's implicit root — exactly how vanilla TXSTs and ModForge's `model` field store
        // paths: `Clothes\Monk\Robes_d.dds`, NOT `Textures\Clothes\…`). So the leading `Textures\` is
        // an error (it would resolve to Data\Textures\Textures\…), as are absolute paths and non-.dds
        // files. At least one slot should be set (a TXST overriding nothing is a no-op).
        void CheckTexPath(string path, string what)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            var raw = path.Trim();
            var p = raw.Replace('/', '\\');
            // Reject absolute paths portably: a leading / or \ (Unix root / UNC), a drive letter
            // (C:\), or .NET's own rooted check on the verbatim string.
            bool rooted = raw.StartsWith('/') || raw.StartsWith('\\') || Path.IsPathRooted(raw)
                || (raw.Length >= 2 && char.IsLetter(raw[0]) && raw[1] == ':');
            if (rooted)
                problems.Add($"{what} path '{path}' must be RELATIVE to Data\\Textures (e.g. mymod\\sword_d.dds), not absolute");
            if (p.StartsWith("Textures\\", StringComparison.OrdinalIgnoreCase))
                problems.Add($"{what} path '{path}' must NOT start with 'Textures\\' — TXST slots are already relative to Data\\Textures (use e.g. mymod\\sword_d.dds)");
            if (!p.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                problems.Add($"{what} path '{path}' must be a .dds texture");
        }
        foreach (var tx in spec.TextureSets)
        {
            if (string.IsNullOrWhiteSpace(tx.Diffuse) && string.IsNullOrWhiteSpace(tx.Normal)
                && string.IsNullOrWhiteSpace(tx.Mask) && string.IsNullOrWhiteSpace(tx.Glow)
                && string.IsNullOrWhiteSpace(tx.Height) && string.IsNullOrWhiteSpace(tx.Environment)
                && string.IsNullOrWhiteSpace(tx.Multilayer) && string.IsNullOrWhiteSpace(tx.Backlight))
                problems.Add($"textureSet '{tx.EditorId}' sets no texture slots (at minimum set `diffuse`) — overrides nothing");
            else if (string.IsNullOrWhiteSpace(tx.Diffuse))
                problems.Add($"textureSet '{tx.EditorId}' has no `diffuse` slot (the base color map) — unusual; most retextures set it");
            CheckTexPath(tx.Diffuse,     $"textureSet '{tx.EditorId}' diffuse");
            CheckTexPath(tx.Normal,      $"textureSet '{tx.EditorId}' normal");
            CheckTexPath(tx.Mask,        $"textureSet '{tx.EditorId}' mask");
            CheckTexPath(tx.Glow,        $"textureSet '{tx.EditorId}' glow");
            CheckTexPath(tx.Height,      $"textureSet '{tx.EditorId}' height");
            CheckTexPath(tx.Environment, $"textureSet '{tx.EditorId}' environment");
            CheckTexPath(tx.Multilayer,  $"textureSet '{tx.EditorId}' multilayer");
            CheckTexPath(tx.Backlight,   $"textureSet '{tx.EditorId}' backlight");
            foreach (var f in tx.Flags)
                if (!Enum.TryParse<TextureSet.Flag>(f, true, out _))
                    problems.Add($"textureSet '{tx.EditorId}' invalid flag '{f}' (NoSpecularMap|FaceGenTextures|HasModelSpaceNormalMap)");
        }
        // alternateTextures (the TXST consumer) on Static/Activator: a base `model` to override, a
        // material `name` to target, and a TXST `textureSet` ref that resolves.
        void CheckAltTextures(string ed, string model, List<AlternateTextureSpec> alts, string kind)
        {
            if (alts.Count == 0) return;
            if (string.IsNullOrWhiteSpace(model))
                problems.Add($"{kind} '{ed}' has alternateTextures but no `model` — nothing to retexture");
            foreach (var alt in alts)
            {
                if (string.IsNullOrWhiteSpace(alt.Name))
                    problems.Add($"{kind} '{ed}' alternateTexture has empty `name` (must match a material/sub-mesh in the .nif)");
                if (string.IsNullOrWhiteSpace(alt.TextureSet))
                    problems.Add($"{kind} '{ed}' alternateTexture '{alt.Name}' has empty `textureSet` ref");
                else CheckRef(alt.TextureSet, $"{kind} '{ed}' alternateTexture '{alt.Name}' textureSet");
                if (alt.Index < 0) problems.Add($"{kind} '{ed}' alternateTexture '{alt.Name}' index must be >= 0");
            }
        }
        foreach (var st in spec.Statics) CheckAltTextures(st.EditorId, st.Model, st.AlternateTextures, "static");
        foreach (var ac in spec.Activators) CheckAltTextures(ac.EditorId, ac.Model, ac.AlternateTextures, "activator");

        ValidateWeather(spec, problems, ids, CheckRef);

        // Worldspaces (WRLD): refs (climate/water/parent/…) resolve; a climate is strongly advised
        // (without it the world has no sky/lighting cycle). Flags must parse.
        foreach (var ws in spec.Worldspaces)
        {
            if (string.IsNullOrWhiteSpace(ws.Climate))
                problems.Add($"worldspace '{ws.EditorId}' has no climate — the world will have no sky/lighting cycle (set a CLMT ref, e.g. Skyrim.esm:0x000812)");
            CheckRef(ws.Climate, $"worldspace '{ws.EditorId}' climate");
            CheckRef(ws.Water, $"worldspace '{ws.EditorId}' water");
            CheckRef(ws.LodWater, $"worldspace '{ws.EditorId}' lodWater");
            CheckRef(ws.Parent, $"worldspace '{ws.EditorId}' parent");
            CheckRef(ws.InteriorLighting, $"worldspace '{ws.EditorId}' interiorLighting");
            CheckRef(ws.Location, $"worldspace '{ws.EditorId}' location");
            CheckRef(ws.Music, $"worldspace '{ws.EditorId}' music");
            CheckRef(ws.EncounterZone, $"worldspace '{ws.EditorId}' encounterZone");
            foreach (var f in ws.Flags)
                if (!Enum.TryParse<Worldspace.Flag>(f, true, out _))
                    problems.Add($"worldspace '{ws.EditorId}' invalid flag '{f}' (SmallWorld|CannotFastTravel|NoLodWater|NoLandscape|NoSky|FixedDimensions|NoGrass)");
        }

        // Regions (REGN): must name a worldspace, enclose an area (≥3 points), and carry ≥1 weather
        // entry whose chances sum > 0 — that weather table is the climate hook the feature exists for.
        foreach (var rg in spec.Regions)
        {
            if (string.IsNullOrWhiteSpace(rg.Worldspace))
                problems.Add($"region '{rg.EditorId}' has no worldspace (a region must live inside a WRLD)");
            else CheckRef(rg.Worldspace, $"region '{rg.EditorId}' worldspace");

            if (rg.Area.Count == 0)
                problems.Add($"region '{rg.EditorId}' has no area (need a polygon of ≥3 world-space points)");
            else if (rg.Area.Count < 3)
                problems.Add($"region '{rg.EditorId}' area has only {rg.Area.Count} point(s) — need ≥3 to enclose an area");

            if (rg.Weather.Count == 0)
                problems.Add($"region '{rg.EditorId}' has no weather entries — add ≥1 Weather ref+chance (the point of a weather region)");
            else
            {
                int sum = 0;
                foreach (var we in rg.Weather)
                {
                    if (string.IsNullOrWhiteSpace(we.Weather))
                        problems.Add($"region '{rg.EditorId}' has a weather entry with empty weather ref");
                    else CheckRef(we.Weather, $"region '{rg.EditorId}' weather");
                    CheckRef(we.Global, $"region '{rg.EditorId}' weather global");
                    if (we.Chance < 0) problems.Add($"region '{rg.EditorId}' weather chance {we.Chance} is negative");
                    else sum += we.Chance;
                }
                if (sum <= 0)
                    problems.Add($"region '{rg.EditorId}' weather chances sum to {sum} — at least one entry needs a chance > 0");
            }

            if (!string.IsNullOrWhiteSpace(rg.MapColor) && !TryParseRgb(rg.MapColor, out _))
                problems.Add($"region '{rg.EditorId}' mapColor '{rg.MapColor}' is not a hex RGB (expect 0xRRGGBB)");
        }

        // EncounterZone (ECZN): level range sane (min<=max unless max==0 = uncapped), bytes in 0–255,
        // owner/location refs resolve, flags parse.
        foreach (var ez in spec.EncounterZones)
        {
            if (ez.MinLevel is < 0 or > 255) problems.Add($"encounterZone '{ez.EditorId}' minLevel {ez.MinLevel} out of range (0–255)");
            if (ez.MaxLevel is < 0 or > 255) problems.Add($"encounterZone '{ez.EditorId}' maxLevel {ez.MaxLevel} out of range (0–255)");
            // maxLevel 0 = "uncapped" (vanilla idiom), so only enforce min<=max when a real cap is set.
            if (ez.MaxLevel != 0 && ez.MinLevel > ez.MaxLevel)
                problems.Add($"encounterZone '{ez.EditorId}' minLevel {ez.MinLevel} > maxLevel {ez.MaxLevel} (set maxLevel 0 for an uncapped zone)");
            CheckRef(ez.Owner, $"encounterZone '{ez.EditorId}' owner");
            CheckRef(ez.Location, $"encounterZone '{ez.EditorId}' location");
            foreach (var f in ez.Flags)
                if (!Enum.TryParse<EncounterZone.Flag>(f, true, out _))
                    problems.Add($"encounterZone '{ez.EditorId}' invalid flag '{f}' (NeverResets|MatchPcBelowMinimumLevel|DisableCombatBoundary)");
        }
        // Cell / placement encounterZone refs must resolve to an in-spec ECZN or vanilla one.
        foreach (var c in spec.Cells)
            CheckRef(c.EncounterZone, $"cell '{c.EditorId}' encounterZone");
        foreach (var pl in spec.Placements)
            CheckRef(pl.EncounterZone, $"placement '{(string.IsNullOrWhiteSpace(pl.EditorId) ? pl.Base : pl.EditorId)}' encounterZone");

        // Vendor (merchant) faction data: hours sane (0..24, start<end), gold implied by the
        // merchant container, refs well-formed. The merchant container must be a PLACEMENT editorId
        // (the placed chest), not a bare in-spec Container — only a placed ref holds gold the engine
        // reads. A vendor faction with no member NPC trades to nobody.
        var vendorFactEds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in spec.Factions)
        {
            if (f.Vendor is not { } v) continue;
            if (!string.IsNullOrWhiteSpace(f.EditorId)) vendorFactEds.Add(f.EditorId);
            if (v.StartHour > 24) problems.Add($"faction '{f.EditorId}' vendor.startHour {v.StartHour} out of range (0..24)");
            if (v.EndHour > 24) problems.Add($"faction '{f.EditorId}' vendor.endHour {v.EndHour} out of range (0..24)");
            if (v.StartHour <= 24 && v.EndHour <= 24 && v.StartHour > v.EndHour)
                problems.Add($"faction '{f.EditorId}' vendor hours invalid: startHour {v.StartHour} > endHour {v.EndHour} (shop never opens)");
            CheckRef(v.SellBuyList, $"faction '{f.EditorId}' vendor.sellBuyList");
            if (string.IsNullOrWhiteSpace(v.SellBuyList) && !v.NotSellBuyList)
                problems.Add($"faction '{f.EditorId}' vendor has no sellBuyList and notSellBuyList=false — vendor trades no item categories (set a VendorItem FormList ref, e.g. Skyrim.esm:0x06CB48 VendorItemsMisc, or notSellBuyList=true)");
            CheckRef(v.MerchantContainer, $"faction '{f.EditorId}' vendor.merchantContainer");
            if (string.IsNullOrWhiteSpace(v.MerchantContainer))
                problems.Add($"faction '{f.EditorId}' vendor.merchantContainer is empty — a vendor needs a placed merchant chest (holds the gold + stock); reference a placement editorId");
            else if (!LooksExternalRef(v.MerchantContainer) && !placementIds.Contains(v.MerchantContainer))
                problems.Add($"faction '{f.EditorId}' vendor.merchantContainer '{v.MerchantContainer}' must be a PLACEMENT editorId (the placed chest), not a bare record — give the chest placement an editorId and reference it");
        }
        // An NPC member of an in-spec vendor faction becomes a shopkeeper; remind that it needs a
        // greeting (to be conversable) for the trade prompt to surface.
        foreach (var n in spec.Npcs)
        {
            bool isVendorNpc = n.Factions.Any(fr => !LooksExternalRef(fr) && vendorFactEds.Contains(fr));
            if (isVendorNpc && string.IsNullOrWhiteSpace(n.Greeting) && !spec.Dialogue.Any(d => d.SpeakerNpcEditorId == n.EditorId))
                problems.Add($"npc '{n.EditorId}' is a vendor (member of a vendor faction) but has no greeting and no dialogue — it won't be conversable, so the 'I'd like to trade' prompt can't appear (set a `greeting`)");
        }

        // Shouts (SHOU) + Words of Power (WOOP). A shout must have 1–3 word rows (vanilla shouts have
        // exactly 3). Each row's word + spell refs must resolve, recovery time can't be negative, and a
        // WOOP needs at least one of translation/name so the menu has something to show.
        foreach (var w in spec.WordsOfPower)
            if (string.IsNullOrWhiteSpace(w.Translation) && string.IsNullOrWhiteSpace(w.Name))
                problems.Add($"wordOfPower '{w.EditorId}' has empty translation and name (set at least one — the in-game word text)");
        foreach (var sh in spec.Shouts)
        {
            if (sh.Words.Count is < 1 or > 3)
                problems.Add($"shout '{sh.EditorId}' has {sh.Words.Count} word row(s) — a shout needs 1–3 (vanilla shouts have exactly 3)");
            CheckRef(sh.MenuDisplayObject, $"shout '{sh.EditorId}' menuDisplayObject");
            for (int i = 0; i < sh.Words.Count; i++)
            {
                var ws = sh.Words[i];
                if (string.IsNullOrWhiteSpace(ws.Word)) problems.Add($"shout '{sh.EditorId}' word[{i}] has empty word ref");
                else CheckRef(ws.Word, $"shout '{sh.EditorId}' word[{i}] word");
                if (string.IsNullOrWhiteSpace(ws.Spell)) problems.Add($"shout '{sh.EditorId}' word[{i}] has empty spell ref");
                else CheckRef(ws.Spell, $"shout '{sh.EditorId}' word[{i}] spell");
                if (ws.RecoveryTime < 0) problems.Add($"shout '{sh.EditorId}' word[{i}] recoveryTime {ws.RecoveryTime} is negative");
            }
        }

        return problems;
    }
}
