namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 1: Word-wall teaching quests: one start-enabled QUEST per word wall, hosting the
        // generated teaching fragment (the shout/word are bound on it in the script pass). StartGameEnabled
        // so its OnInit-driven fragment is live the moment the trigger starts it. Built after the spec
        // quests so its FormIDs follow them (the orchestrator preserves this order). ---
        public void BuildWordWallQuests()
        {
            foreach (var ww in spec.WordWalls)
            {
                if (string.IsNullOrWhiteSpace(ww.EditorId)) continue;
                var r = mod.Quests.AddNew();
                r.EditorID = ww.EditorId;
                if (!string.IsNullOrEmpty(ww.Name)) r.Name = ww.Name;
                r.Flags |= Quest.Flag.StartGameEnabled;
                questsByEd[ww.EditorId] = r;
            }
        }

        // --- pass 1: Relationship (RELA) — scalar Rank now; Parent/Child NPC refs wired in pass 2 ---
        public void BuildRelationships()
        {
            foreach (var rel in spec.Relationships)
            {
                var r = mod.Relationships.AddNew();
                r.EditorID = rel.EditorId;
                r.Rank = Enum.TryParse<Relationship.RankType>(rel.Rank, ignoreCase: true, out var rk)
                    ? rk : Relationship.RankType.Ally;
            }
        }

        // --- pass 2: Relationship Parent/Child NPC refs (Parent usually the in-spec NPC, Child the player) ---
        public void WireRelationships()
        {
            foreach (var rel in spec.Relationships)
            {
                if (!recordsByEd.TryGetValue(rel.EditorId, out var rec) || rec is not IRelationship r) continue;
                Resolve($"relationship '{rel.EditorId}' parent", rel.Parent, fk => r.Parent.SetTo(fk));
                Resolve($"relationship '{rel.EditorId}' child",  rel.Child,  fk => r.Child.SetTo(fk));
            }
        }

        // --- pass 2: Outfit (OTFT) contents — each item is a ref (in-spec armor/weapon or external) ---
        public void WireOutfits()
        {
            foreach (var o in spec.Outfits)
            {
                if (!recordsByEd.TryGetValue(o.EditorId, out var rec) || rec is not IOutfit outfit) continue;
                outfit.Items ??= new();
                foreach (var itemRef in o.Items)
                    Resolve($"outfit '{o.EditorId}' item", itemRef, fk => outfit.Items!.Add(new FormLink<IOutfitTargetGetter>(fk)));
            }
        }

        // --- pass 1: Class (CLAS): no FormLinks (all enums/weight dicts), so fully built in pass 1. An
        // npc's `class` ref can point at one (resolved in pass 2 — it's in formKeyByEd by then). StatWeights
        // (Health/Magicka/Stamina) drive the actor's attribute distribution; SkillWeights favour skills. ---
        public void BuildClasses()
        {
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
        }
    }
}
