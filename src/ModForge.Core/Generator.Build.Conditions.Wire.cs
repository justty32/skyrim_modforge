namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // Append spec conditions to each dialogue INFO (after the auto GetIsID speaker gate).
        public void WireDialogueConditions()
        {
            foreach (var d in spec.Dialogue)
            {
                // The owning quest (if any) supplies the alias-name→index map for GetIsAliasRef.
                IReadOnlyDictionary<string, int>? aliasIdx = null;
                Quest? questRec = null;
                if (!string.IsNullOrEmpty(d.QuestEditorId) && questsByEd.TryGetValue(d.QuestEditorId, out questRec))
                    aliasIdx = questRec.Aliases.ToDictionary(a => a.Name ?? "", a => (int)a.ID, StringComparer.OrdinalIgnoreCase);

                // The SHARED gate every INFO of this entry carries: inline `conditions`, then named
                // condition templates (M組, in listed order), then identity tags. Applied to the parent INFO
                // AND each variant INFO (a variant adds its own conditions after these).
                void ApplyShared(DialogResponses info)
                {
                    foreach (var c in d.Conditions)
                        if (BuildCondition(c, $"dialogue '{d.EditorId}' condition", aliasIdx) is { } cond) info.Conditions.Add(cond);
                    foreach (var tname in d.UseConditionTemplates)
                    {
                        var tmpl = spec.ConditionTemplates.FirstOrDefault(t => string.Equals(t.Name, tname, StringComparison.OrdinalIgnoreCase));
                        if (tmpl is null) { Warn($"  ! dialogue '{d.EditorId}': unknown conditionTemplate '{tname}'"); continue; }
                        foreach (var c in tmpl.Conditions)
                            if (BuildCondition(c, $"dialogue '{d.EditorId}' template '{tname}'", aliasIdx) is { } cond) info.Conditions.Add(cond);
                    }
                    foreach (var c in ExpandIdentityConditions(d.Identity, d.PrimaryIdentity, $"dialogue '{d.EditorId}' identity"))
                        if (BuildCondition(c, $"dialogue '{d.EditorId}' identity") is { } cond) info.Conditions.Add(cond);
                }

                // Parent INFO (skipped for a pure variant batch — see BuildDialogue).
                if (dialogResponsesByEd.TryGetValue(d.EditorId, out var info))
                {
                    // Auto-condition: if this line advances the quest to stage N, only show it when the quest
                    // is still below stage N — prevents the line from repeating after the player picked it.
                    if (d.SetStage >= 0 && questRec is not null)
                    {
                        var sc = new ConditionFloat { CompareOperator = CompareOperator.LessThan, ComparisonValue = d.SetStage };
                        var sd = new GetStageConditionData();
                        sd.Quest.Link.SetTo(questRec.FormKey);
                        sc.Data = sd;
                        info.Conditions.Add(sc);
                    }
                    ApplyShared(info);
                }
                else if ((d.Conditions.Count > 0 || d.SetStage >= 0) && d.Variants.Count == 0)
                    Warn($"  ! dialogue '{d.EditorId}' conditions: INFO not built");

                // M組 variant INFOs: the shared gate, then each variant's OWN extra conditions.
                for (int vi = 0; vi < d.Variants.Count; vi++)
                {
                    if (!dialogResponsesByEd.TryGetValue(DialogueVariantId(d.EditorId, vi), out var vinfo)) continue;
                    ApplyShared(vinfo);
                    foreach (var c in d.Variants[vi].Conditions)
                        if (BuildCondition(c, $"dialogue '{d.EditorId}' variant {vi} condition", aliasIdx) is { } cond) vinfo.Conditions.Add(cond);
                }
            }
        }

        // Set spec conditions on each AI package (the engine picks the first package whose conditions pass).
        public void WirePackageConditions()
        {
            foreach (var p in spec.Packages)
            {
                if (p.Conditions.Count == 0) continue;
                if (!recordsByEd.TryGetValue(p.EditorId, out var rec) || rec is not IPackage pkg)
                { Warn($"  ! package '{p.EditorId}' conditions: package not found"); continue; }
                foreach (var c in p.Conditions)
                    if (BuildCondition(c, $"package '{p.EditorId}' condition") is { } cond) pkg.Conditions.Add(cond);
            }
        }
    }
}
