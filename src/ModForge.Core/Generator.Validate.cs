namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Validate — semantic guardrail for (LLM-authored) specs: editorId presence +
    //  uniqueness, and referential integrity (dialogue→quest/npc, npc→faction,
    //  script→target, object-property→record, property types). Returns the list of
    //  problems (empty == valid) so an NL→spec front can self-correct before build.
    //
    //  The validation state lives in ValidateContext (a partial nested class).
    //  Domain-specific checks are in Generator.Validate.Npcs.cs / Items.cs /
    //  Quests.cs / World.cs. Weather is in Generator.Validate.Weather.cs.
    // -------------------------------------------------------------------------------
    /// <summary>Semantically validate a spec. Returns the list of problems; empty means valid.</summary>
    public static IReadOnlyList<string> Validate(ModSpec spec)
    {
        var ctx = new ValidateContext(spec);
        ctx.ValidateNpcs();
        ctx.ValidateItems();
        ctx.ValidateQuestsAndDialogue();
        ctx.ValidateWorld();
        ctx.ValidateStoryManager();
        ValidateWeather(spec, ctx.Problems, ctx.Ids, ctx.CheckRef);
        return ctx.Problems;
    }

    private sealed partial class ValidateContext
    {
        // Shared state across all domain validators.
        readonly ModSpec spec;
        public readonly List<string> Problems = [];
        public readonly HashSet<string> Ids = new(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> npcIds = new(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> questIds = new(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> factionIds = new(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> cellIds = new(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> spellIds = new(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> placementIds = new(StringComparer.OrdinalIgnoreCase);

        public ValidateContext(ModSpec spec) { this.spec = spec; RegisterAll(spec); }

        // Pass 0: register every editorId so forward-ref checks in domain passes work.
        void RegisterAll(ModSpec spec)
        {
            foreach (var m in spec.MiscItems) Reg(m.EditorId, "miscItem");
            foreach (var b in spec.Books) Reg(b.EditorId, "book");
            foreach (var w in spec.Weapons) Reg(w.EditorId, "weapon");
            foreach (var n in spec.Npcs) Reg(n.EditorId, "npc", npcIds);
            foreach (var q in spec.Quests) Reg(q.EditorId, "quest", questIds);
            foreach (var s in spec.Spells) Reg(s.EditorId, "spell", spellIds);
            foreach (var p in spec.Potions) Reg(p.EditorId, "potion");
            foreach (var a in spec.Armors) Reg(a.EditorId, "armor");
            foreach (var pl in spec.Placements) if (!string.IsNullOrWhiteSpace(pl.EditorId)) placementIds.Add(pl.EditorId);
            foreach (var f in spec.Factions) Reg(f.EditorId, "faction", factionIds);
            foreach (var msg in spec.Messages) Reg(msg.EditorId, "message");
            foreach (var d in spec.Dialogue) Reg(d.EditorId, "dialogue");
            foreach (var sc in spec.Scenes) Reg(sc.EditorId, "scene");
            foreach (var c in spec.Cells)
            {
                Reg(c.EditorId, "cell", cellIds);
                if (!string.IsNullOrWhiteSpace(c.Template) && !TryExternalRef(c.Template, out _))
                    Problems.Add($"cell '{c.EditorId}' template '{c.Template}' must be an external <master>:0xFORMID interior-cell ref");
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
            // Word walls emit a teaching quest under their editorId AND a trigger REFR under
            // <editorId>Trigger (or triggerEditorId) — both must be unique across the spec.
            foreach (var ww in spec.WordWalls)
            {
                Reg(ww.EditorId, "wordWall (teaching quest)");
                var triggerEd = string.IsNullOrWhiteSpace(ww.TriggerEditorId) ? ww.EditorId + "Trigger" : ww.TriggerEditorId;
                if (!string.IsNullOrWhiteSpace(triggerEd)) Reg(triggerEd, "wordWall trigger placement");
            }
            foreach (var e in spec.Enchantments) Reg(e.EditorId, "enchantment");
            foreach (var tx in spec.TextureSets) Reg(tx.EditorId, "textureSet");
            foreach (var w in spec.Weathers) Reg(w.EditorId, "weather");
            foreach (var cl in spec.Climates) Reg(cl.EditorId, "climate");
            foreach (var ws in spec.Worldspaces) Reg(ws.EditorId, "worldspace");
            foreach (var rg in spec.Regions) Reg(rg.EditorId, "region");
            foreach (var ez in spec.EncounterZones) Reg(ez.EditorId, "encounterZone");
            foreach (var fn in spec.Furniture) Reg(fn.EditorId, "furniture");
            foreach (var sd in spec.Sounds) Reg(sd.EditorId, "sound");
            foreach (var pk in spec.Perks) Reg(pk.EditorId, "perk");
            foreach (var pl in spec.Placements) if (!string.IsNullOrWhiteSpace(pl.EditorId)) Reg(pl.EditorId, "placement");
        }

        void Reg(string ed, string what, HashSet<string>? typed = null)
        {
            if (string.IsNullOrWhiteSpace(ed)) { Problems.Add($"{what}: empty editorId"); return; }
            if (!Ids.Add(ed)) Problems.Add($"duplicate editorId '{ed}' (at {what})");
            typed?.Add(ed);
        }

        // A ref must be an in-spec editorId OR a well-formed external "<master>:0xFORMID".
        public void CheckRef(string r, string what)
        {
            if (string.IsNullOrWhiteSpace(r)) return;
            if (LooksExternalRef(r))
            { if (!TryExternalRef(r, out _)) Problems.Add($"{what}: malformed external ref '{r}' (expect <master>:0xFORMID)"); }
            else if (!Ids.Contains(r))
                Problems.Add($"{what}: unresolved ref '{r}' (unknown in-spec editorId; for vanilla forms use <master>:0xFORMID)");
        }

        void CheckEnum<TEnum>(string val, string what) where TEnum : struct, Enum
        { if (!string.IsNullOrWhiteSpace(val) && !Enum.TryParse<TEnum>(val, ignoreCase: true, out _)) Problems.Add($"{what} '{val}' invalid"); }

        void CheckEffects(string ed, List<EffectSpec> effects, string kind)
        {
            foreach (var e in effects)
                if (string.IsNullOrWhiteSpace(e.MagicEffect)) Problems.Add($"{kind} '{ed}' has an effect with empty magicEffect ref");
                else CheckRef(e.MagicEffect, $"{kind} '{ed}' effect magicEffect");
        }

        bool ValidComparison(string cmp) => string.IsNullOrEmpty(cmp)
            || cmp is "==" or "=" or "!=" or ">" or ">=" or "<" or "<="
            || Enum.TryParse<CompareOperator>(cmp, true, out _);

        void CheckCondition(ConditionSpec cs, string what)
        {
            if (string.IsNullOrWhiteSpace(cs.Function)) { Problems.Add($"{what}: condition has empty function"); return; }
            if (!SupportedConditionFunctions.Contains(cs.Function))
                Problems.Add($"{what}: unsupported condition function '{cs.Function}'");
            if (!ValidComparison(cs.Comparison))
                Problems.Add($"{what}: invalid comparison '{cs.Comparison}' (== != > >= < <= or the CompareOperator enum names)");
            if (!string.IsNullOrEmpty(cs.RunOn) && !Enum.TryParse<Condition.RunOnType>(cs.RunOn, true, out _))
                Problems.Add($"{what}: invalid runOn '{cs.RunOn}' (Subject|Target|Reference|CombatTarget|...)");
            switch (cs.Function.ToLowerInvariant())
            {
                case "getbaseactorvalue":
                case "getactorvalue":
                case "getactorvaluepercent":
                    if (string.IsNullOrWhiteSpace(cs.ActorValue)) Problems.Add($"{what}: {cs.Function} needs an actorValue");
                    else CheckEnum<ActorValue>(cs.ActorValue, $"{what} actorValue");
                    break;
                case "haskeyword": case "wornhaskeyword": case "hasperk": case "getisid":
                case "getisrace": case "getitemcount": case "isspelltarget": case "getinfaction":
                case "getglobalvalue": case "getstage": case "getrelationshiprank":
                    if (string.IsNullOrWhiteSpace(cs.Param)) Problems.Add($"{what}: {cs.Function} needs a param ref");
                    else CheckRef(cs.Param, $"{what} param");
                    break;
                case "getequippeditemtype":
                    if (!string.IsNullOrEmpty(cs.ItemType)) CheckEnum<CastSource>(cs.ItemType, $"{what} itemType");
                    break;
            }
        }

        void CheckModelPath(string model, string what)
        {
            if (string.IsNullOrWhiteSpace(model)) return;
            var p = model.Replace('/', '\\');
            if (!p.EndsWith(".nif", StringComparison.OrdinalIgnoreCase))
                Problems.Add($"{what} model '{model}' must be a .nif path (Data-relative, rooted at Meshes\\, e.g. Weapons\\Iron\\LongSword.nif)");
            if (p.StartsWith("Meshes\\", StringComparison.OrdinalIgnoreCase))
                Problems.Add($"{what} model '{model}' must NOT start with 'Meshes\\' — the engine roots model paths at Meshes\\ already (drop the prefix)");
            if (System.IO.Path.IsPathRooted(model) || p.StartsWith('\\') || p.Contains(':'))
                Problems.Add($"{what} model '{model}' must be a relative path, not absolute/drive-qualified");
        }

        void CheckSoundFile(string file, string what)
        {
            if (string.IsNullOrWhiteSpace(file)) return;
            var p = file.Replace('/', '\\');
            if (!(p.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".xwm", StringComparison.OrdinalIgnoreCase)))
                Problems.Add($"{what} file '{file}' must be a .wav or .xwm path (Data-relative under Sound\\, e.g. Sound\\fx\\mymod\\bell.wav)");
            if (System.IO.Path.IsPathRooted(file) || p.StartsWith('\\') || p.Contains(':'))
                Problems.Add($"{what} file '{file}' must be a relative path, not absolute/drive-qualified");
        }

        void CheckTexPath(string path, string what)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            var raw = path.Trim();
            var p = raw.Replace('/', '\\');
            bool rooted = raw.StartsWith('/') || raw.StartsWith('\\') || Path.IsPathRooted(raw)
                || (raw.Length >= 2 && char.IsLetter(raw[0]) && raw[1] == ':');
            if (rooted)
                Problems.Add($"{what} path '{path}' must be RELATIVE to Data\\Textures (e.g. mymod\\sword_d.dds), not absolute");
            if (p.StartsWith("Textures\\", StringComparison.OrdinalIgnoreCase))
                Problems.Add($"{what} path '{path}' must NOT start with 'Textures\\' — TXST slots are already relative to Data\\Textures (use e.g. mymod\\sword_d.dds)");
            if (!p.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                Problems.Add($"{what} path '{path}' must be a .dds texture");
        }

        void CheckAltTextures(string ed, string model, List<AlternateTextureSpec> alts, string kind)
        {
            if (alts.Count == 0) return;
            if (string.IsNullOrWhiteSpace(model))
                Problems.Add($"{kind} '{ed}' has alternateTextures but no `model` — nothing to retexture");
            foreach (var alt in alts)
            {
                if (string.IsNullOrWhiteSpace(alt.Name))
                    Problems.Add($"{kind} '{ed}' alternateTexture has empty `name` (must match a material/sub-mesh in the .nif)");
                if (string.IsNullOrWhiteSpace(alt.TextureSet))
                    Problems.Add($"{kind} '{ed}' alternateTexture '{alt.Name}' has empty `textureSet` ref");
                else CheckRef(alt.TextureSet, $"{kind} '{ed}' alternateTexture '{alt.Name}' textureSet");
                if (alt.Index < 0) Problems.Add($"{kind} '{ed}' alternateTexture '{alt.Name}' index must be >= 0");
            }
        }
    }
}
