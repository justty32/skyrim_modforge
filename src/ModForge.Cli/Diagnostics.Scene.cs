internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  scenediag — introspect a SCEN (Scene) record: its host quest, actors (each an
    //  alias index into the host quest), the ordered phases, and the per-phase actions
    //  (the dialogue-playing ones carry a topic FormID + speaking-actor alias index).
    //  This is the probe used to author the `scenes` spec section — it reveals the exact
    //  shape a vanilla two-NPC conversation scene uses (which alias an action speaks as,
    //  the SCEN-subtype dialogue topic each action plays, the phase ordering data).
    // -------------------------------------------------------------------------------
    private static int SceneDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);

        var scene = mod.EnumerateMajorRecords<ISceneGetter>().FirstOrDefault(s => s.FormKey.ID == id);
        if (scene is null)
        {
            Console.WriteLine($"0x{id:X6} is not a Scene in {Path.GetFileName(inPath)}");
            return 0;
        }

        Console.WriteLine($"SCENE 0x{scene.FormKey.ID:X6}  {scene.EditorID ?? "-"}");
        Console.WriteLine($"  quest = {scene.Quest.FormKey}   flags = {scene.Flags}");

        Console.WriteLine($"  actors ({scene.Actors.Count}):");
        foreach (var a in scene.Actors)
            Console.WriteLine($"    alias #{a.ID}  behaviorFlags={a.BehaviorFlags}  flags={a.Flags}");

        // The actors above are alias INDICES into the host quest — resolve them to the quest's
        // QuestAlias entries so the NPC each alias is bound to (UniqueActor / ForcedReference) shows.
        var hostQuest = mod.EnumerateMajorRecords<IQuestGetter>().FirstOrDefault(q => q.FormKey == scene.Quest.FormKey);
        if (hostQuest is not null)
        {
            Console.WriteLine($"  host-quest aliases ({hostQuest.Aliases.Count}):");
            foreach (var al in hostQuest.Aliases.OfType<IQuestAliasGetter>())
            {
                var ua = al.UniqueActor.FormKey;
                var fr = al.ForcedReference.FormKey;
                Console.WriteLine($"    alias #{al.ID}  name=\"{al.Name}\""
                    + (ua.IsNull ? "" : $"  uniqueActor={ua}")
                    + (fr.IsNull ? "" : $"  forcedRef={fr}"));
            }
        }

        Console.WriteLine($"  phases ({scene.Phases.Count}):");
        for (int i = 0; i < scene.Phases.Count; i++)
        {
            var p = scene.Phases[i];
            Console.WriteLine($"    phase[{i}]  name=\"{p.Name}\"  startConds={p.StartConditions.Count}  completeConds={p.CompletionConditions.Count}");
        }

        Console.WriteLine($"  actions ({scene.Actions.Count}):");
        foreach (var act in scene.Actions)
        {
            // Reflect the action's fields so the dump is robust to the exact Mutagen names — the
            // key ones for dialogue are Type, ActorID (which alias speaks), StartPhase/EndPhase
            // (which phase window), and Topic (the SCEN-subtype DialogTopic the action plays).
            const System.Reflection.BindingFlags bf = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
            var fields = new List<string>();
            foreach (var pr in act.GetType().GetProperties(bf))
            {
                if (pr.GetIndexParameters().Length != 0) continue;
                if (pr.Name is "MajorRecordFlagsRaw") continue;
                object? v; try { v = pr.GetValue(act); } catch { continue; }
                if (v is null) continue;
                if (v is Mutagen.Bethesda.Plugins.IFormLinkGetter fl) { if (!fl.FormKey.IsNull) fields.Add($"{pr.Name}={fl.FormKey}"); continue; }
                if (v is System.Collections.ICollection col) { if (col.Count > 0) fields.Add($"{pr.Name}=[{col.Count}]"); continue; }
                if (v is string or bool or System.Enum or System.IConvertible) fields.Add($"{pr.Name}={v}");
            }
            // The Topic link is the SCEN-subtype DialogTopic the Dialog action plays — render it explicitly
            // (it can be a plain FormLink or a null one when the action is a Package/Timer, not Dialog).
            if (act.Topic is { } tlink) fields.Add($"Topic={(tlink.FormKey.IsNull ? "<null>" : tlink.FormKey.ToString())}");
            Console.WriteLine($"    action: {string.Join("  ", fields)}");
        }

        // The spoken lines live in Scene-CATEGORY DialogTopics owned by the host quest; each INFO is
        // gated GetIsAliasRef(speaking alias). List them so the topic<->actor<->phase binding is visible.
        var sceneTopics = mod.EnumerateMajorRecords<IDialogTopicGetter>()
            .Where(t => t.Quest.FormKey == scene.Quest.FormKey && t.Category == DialogTopic.CategoryEnum.Scene)
            .ToList();
        Console.WriteLine($"  scene-category topics owned by quest ({sceneTopics.Count}):");
        static string? T(Func<string?> r) { try { return r(); } catch { return "<localized>"; } }
        foreach (var t in sceneTopics)
        {
            Console.WriteLine($"    TOPIC 0x{t.FormKey.ID:X6}  {t.EditorID ?? "-"}  sub={t.Subtype} SNAM={t.SubtypeName}  INFOs={t.Responses.Count}");
            foreach (var info in t.Responses)
            {
                Console.WriteLine($"      INFO 0x{info.FormKey.ID:X6}  flags={info.Flags?.Flags.ToString() ?? "-"}  conds={info.Conditions.Count}");
                foreach (var resp in info.Responses)
                    Console.WriteLine($"        response[{resp.ResponseNumber}] ({resp.Emotion}): \"{T(() => resp.Text?.String) ?? "<localized>"}\"");
                foreach (var c in info.Conditions) PrintCondition(c, "        cond: ");
            }
        }
        return 0;
    }
}
