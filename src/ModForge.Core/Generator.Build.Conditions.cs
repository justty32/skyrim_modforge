namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 2: CTDA conditions on dialogue INFOs and AI packages (shared builder) ---
        // A condition is static gate data, so it belongs in the spec (not in Papyrus). The form
        // argument and run-on reference are resolved against the formKey table, so this runs in pass 2.

        // Build one ConditionFloat from a spec entry, or null (with a warning) if it's malformed.
        private ConditionFloat? BuildCondition(ConditionSpec c, string label)
        {
            CompareOperator op;
            switch (c.Comparison)
            {
                case "==": case "=": op = CompareOperator.EqualTo; break;
                case "!=":           op = CompareOperator.NotEqualTo; break;
                case ">":            op = CompareOperator.GreaterThan; break;
                case ">=":           op = CompareOperator.GreaterThanOrEqualTo; break;
                case "<":            op = CompareOperator.LessThan; break;
                case "<=":           op = CompareOperator.LessThanOrEqualTo; break;
                default: Warn($"  ! {label}: bad comparison '{c.Comparison}' (use == != > >= < <=)"); return null;
            }

            // The function's form argument (faction/item/global/quest/npc). Optional only in theory —
            // every function we support needs one; warn if it's missing or unresolved.
            FormKey paramFk = default;
            bool hasParam = false;
            if (!string.IsNullOrWhiteSpace(c.Param))
            {
                if (!TryResolveRef(c.Param, formKeyByEd, out paramFk))
                { Warn($"  ! {label}: param ref '{c.Param}' unresolved"); return null; }
                hasParam = true;
            }

            ConditionData? data;
            switch (c.Function.ToLowerInvariant())
            {
                case "getinfaction":        { var d = new GetInFactionConditionData();        if (hasParam) d.Faction.Link.SetTo(paramFk);   data = d; break; }
                case "getitemcount":        { var d = new GetItemCountConditionData();        if (hasParam) d.ItemOrList.Link.SetTo(paramFk); data = d; break; }
                case "getglobalvalue":      { var d = new GetGlobalValueConditionData();      if (hasParam) d.Global.Link.SetTo(paramFk);    data = d; break; }
                case "getstage":            { var d = new GetStageConditionData();            if (hasParam) d.Quest.Link.SetTo(paramFk);     data = d; break; }
                case "getisid":             { var d = new GetIsIDConditionData();             if (hasParam) d.Object.Link.SetTo(paramFk);    data = d; break; }
                case "getrelationshiprank": { var d = new GetRelationshipRankConditionData(); if (hasParam) d.TargetNpc.Link.SetTo(paramFk); data = d; break; }
                default:
                    Warn($"  ! {label}: unsupported function '{c.Function}' "
                        + "(have GetInFaction/GetItemCount/GetGlobalValue/GetStage/GetIsID/GetRelationshipRank)");
                    return null;
            }

            if (Enum.TryParse<Condition.RunOnType>(c.RunOn, ignoreCase: true, out var runOn)) data.RunOnType = runOn;
            else if (!string.IsNullOrWhiteSpace(c.RunOn)) Warn($"  ! {label}: bad runOn '{c.RunOn}'");

            if (!string.IsNullOrWhiteSpace(c.Reference))
            {
                if (TryResolveRef(c.Reference, formKeyByEd, out var rfk)) data.Reference.SetTo(rfk);
                else Warn($"  ! {label}: reference ref '{c.Reference}' unresolved");
            }

            var cond = new ConditionFloat { CompareOperator = op, ComparisonValue = c.Value, Data = data };
            if (c.Or) cond.Flags |= Condition.Flag.OR;
            return cond;
        }

        // Append spec conditions to each dialogue INFO (after the auto GetIsID speaker gate).
        public void WireDialogueConditions()
        {
            foreach (var d in spec.Dialogue)
            {
                if (d.Conditions.Count == 0) continue;
                if (!dialogResponsesByEd.TryGetValue(d.EditorId, out var info))
                { Warn($"  ! dialogue '{d.EditorId}' conditions: INFO not built"); continue; }
                foreach (var c in d.Conditions)
                    if (BuildCondition(c, $"dialogue '{d.EditorId}' condition") is { } cond) info.Conditions.Add(cond);
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
