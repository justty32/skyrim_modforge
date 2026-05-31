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
        foreach (var s in spec.Spells) Reg(s.EditorId, "spell");
        foreach (var p in spec.Potions) Reg(p.EditorId, "potion");
        foreach (var a in spec.Armors) Reg(a.EditorId, "armor");
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
        // `template` = a vanilla record to clone (model/anim) — must be a well-formed external ref.
        foreach (var w in spec.Weapons) if (!string.IsNullOrWhiteSpace(w.Template) && !TryExternalRef(w.Template, out _))
            problems.Add($"weapon '{w.EditorId}' template: malformed external ref '{w.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x012EB7)");
        foreach (var b in spec.Books) if (!string.IsNullOrWhiteSpace(b.Template) && !TryExternalRef(b.Template, out _))
            problems.Add($"book '{b.EditorId}' template: malformed external ref '{b.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x0ED161)");
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

        foreach (var co in spec.Recipes)
        {
            if (string.IsNullOrWhiteSpace(co.CreatedObject)) problems.Add($"recipe '{co.EditorId}' has empty createdObject");
            else CheckRef(co.CreatedObject, $"recipe '{co.EditorId}' createdObject");
            CheckRef(co.Workbench, $"recipe '{co.EditorId}' workbench");   // empty -> defaults to forge
            if (co.Components.Count == 0) problems.Add($"recipe '{co.EditorId}' has no components (nothing to consume)");
            foreach (var comp in co.Components) CheckRef(comp.Item, $"recipe '{co.EditorId}' component");
        }

        foreach (var s in spec.Spells)
        {
            if (!string.IsNullOrEmpty(s.SpellType) && !Enum.TryParse<SpellType>(s.SpellType, true, out _)) problems.Add($"spell '{s.EditorId}' invalid spellType '{s.SpellType}'");
            if (!string.IsNullOrEmpty(s.CastType) && !Enum.TryParse<CastType>(s.CastType, true, out _)) problems.Add($"spell '{s.EditorId}' invalid castType '{s.CastType}'");
            if (!string.IsNullOrEmpty(s.TargetType) && !Enum.TryParse<TargetType>(s.TargetType, true, out _)) problems.Add($"spell '{s.EditorId}' invalid targetType '{s.TargetType}'");
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

        return problems;
    }
}
