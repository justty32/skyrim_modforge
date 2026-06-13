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
        ctx.RegisterIdentityFactions();   // auto-built identity FACTs, so conditions can reference them
        ctx.ValidateNpcs();
        ctx.ValidateNpcPatches();
        ctx.ValidateItems();
        ctx.ValidateQuestsAndDialogue();
        ctx.ValidateWorld();
        ctx.ValidateStoryManager();
        ctx.ValidateGlobals();
        ctx.ValidateProjectiles();
        ctx.ValidateExplosions();
        ctx.ValidateHazards();
        ctx.ValidateMusic();
        ctx.ValidateImageSpaceModifiers();
        ctx.ValidateIdentities();
        ctx.ValidateVoice();
        ValidateWeather(spec, ctx.Problems, ctx.Ids, ctx.CheckRef);
        ValidateLights(spec, ctx.Problems);
        ValidateLighting(spec, ctx.Problems);
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
            foreach (var lt in spec.Lights) Reg(lt.EditorId, "light");
            foreach (var lt in spec.LightingTemplates) Reg(lt.EditorId, "lightingTemplate");
            foreach (var img in spec.ImageSpaces) Reg(img.EditorId, "imageSpace");
            foreach (var pr in spec.Projectiles) Reg(pr.EditorId, "projectile");
            foreach (var ex in spec.Explosions) Reg(ex.EditorId, "explosion");
            foreach (var im in spec.ImageSpaceModifiers) Reg(im.EditorId, "imageSpaceModifier");
            foreach (var w in spec.Weathers) Reg(w.EditorId, "weather");
            foreach (var cl in spec.Climates) Reg(cl.EditorId, "climate");
            foreach (var ws in spec.Worldspaces) Reg(ws.EditorId, "worldspace");
            foreach (var rg in spec.Regions) Reg(rg.EditorId, "region");
            foreach (var ez in spec.EncounterZones) Reg(ez.EditorId, "encounterZone");
            foreach (var fn in spec.Furniture) Reg(fn.EditorId, "furniture");
            foreach (var sd in spec.Sounds) Reg(sd.EditorId, "sound");
            foreach (var pk in spec.Perks) Reg(pk.EditorId, "perk");
            foreach (var g in spec.Globals) Reg(g.EditorId, "global");
            foreach (var pl in spec.Placements) if (!string.IsNullOrWhiteSpace(pl.EditorId)) Reg(pl.EditorId, "placement");
            foreach (var mm in spec.MapMarkers) if (!string.IsNullOrWhiteSpace(mm.EditorId)) Reg(mm.EditorId, "mapMarker");
            foreach (var hz in spec.Hazards) Reg(hz.EditorId, "hazard");
            foreach (var mt in spec.MusicTracks) Reg(mt.EditorId, "musicTrack");
            foreach (var m in spec.Music) Reg(m.EditorId, "music");
        }

        // GlobalVariable (GLOB): type must be one of the three Skyrim subtypes (short/long/int/float).
        public void ValidateGlobals()
        {
            foreach (var g in spec.Globals)
            {
                var t = (g.Type ?? "").Trim().ToLowerInvariant();
                if (t is not ("short" or "long" or "int" or "float"))
                    Problems.Add($"global '{g.EditorId}': unknown type '{g.Type}' (use short | long | float)");
            }
        }

        // An identity's `faction`, when a bare in-spec editorId, becomes an auto-built FACT (no factions[]
        // entry). Register those editorIds up front so dialogue/scene conditions (GetInFaction param) and
        // other refs resolve. Skip external refs and any faction the user also declared in factions[].
        public void RegisterIdentityFactions()
        {
            var declared = new HashSet<string>(spec.Factions.Select(f => f.EditorId), StringComparer.OrdinalIgnoreCase);
            foreach (var idn in spec.Identities)
                if (!string.IsNullOrWhiteSpace(idn.Faction) && !LooksExternalRef(idn.Faction) && !declared.Contains(idn.Faction))
                    Ids.Add(idn.Faction);
        }

        public void ValidateIdentities()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var idn in spec.Identities)
            {
                if (string.IsNullOrWhiteSpace(idn.Id)) { Problems.Add("identity: empty id"); continue; }
                if (!seen.Add(idn.Id)) Problems.Add($"duplicate identity id '{idn.Id}'");
                if (string.IsNullOrWhiteSpace(idn.Faction))
                    Problems.Add($"identity '{idn.Id}': empty faction");
                else if (LooksExternalRef(idn.Faction))
                    CheckRef(idn.Faction, $"identity '{idn.Id}' faction");   // bare editorId = auto-built FACT, fine
                foreach (var g in idn.Grants) CheckRef(g, $"identity '{idn.Id}' grant");
                foreach (var pk in idn.GrantPerks) CheckRef(pk, $"identity '{idn.Id}' grantPerk");
                if (idn.AutoGrantWhen is { } ag && string.IsNullOrWhiteSpace(ag.ActorValue))
                    Problems.Add($"identity '{idn.Id}' autoGrantWhen: empty actorValue");
                if (!string.IsNullOrWhiteSpace(idn.AcquireBook)) CheckRef(idn.AcquireBook, $"identity '{idn.Id}' acquireBook");
                foreach (var c in idn.ActiveWhen) CheckRef(c.Param, $"identity '{idn.Id}' activeWhen param");
            }
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

    }
}
