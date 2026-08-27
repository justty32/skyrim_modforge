namespace ModForge;

public static partial class Generator
{
    // The CTDA condition-function names BuildCondition understands (validate checks against this).
    internal static readonly HashSet<string> SupportedConditionFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "HasPerk", "GetInFaction", "GetItemCount", "GetGlobalValue", "GetStage", "GetIsID",
        "GetIsRace", "HasKeyword", "WornHasKeyword", "IsSpellTarget", "GetRelationshipRank",
        "GetActorValue", "GetActorValuePercent", "GetBaseActorValue", "GetEquippedItemType",
        "GetCurrentTime", "IsInInterior", "IsInCombat", "GetRandomPercent", "GetLevel",
        "TemperIsEnchanted",
        "GetQuestCompleted", "GetDistance", "GetIsCurrentPackage", "GetIsVoiceType",
        "GetIsAliasRef",
        "GetQuestRunning", "GetInCell", "GetInWorldspace", "GetEquipped", "GetDeadCount",
        "GetSitting", "GetGold", "GetMapMarkerVisible", "GetStageDone", "GetInCurrentLoc",
        "IsSceneActionComplete",
        "GetVMQuestVariable", "GetVMScriptVariable",
    };

    internal sealed partial class BuildContext
    {
        // --- pass 2: CTDA conditions (the SHARED builder — dialogue, banter, packages, perks, ---
        // Story Manager, quest-alias match filters, scenes, quest stages, objectives, recipes).
        // A condition is static gate data, so it belongs in the spec (not in Papyrus). The form
        // argument and run-on reference are resolved against the formKey table, so this runs in pass 2.
        //
        // 🔑 INVARIANT (BUILD ORDER — the reason this comment is here): a condition's `param` and
        // `reference` are ARBITRARY refs. They may legitimately name a PLACED ref — an in-spec
        // placements[] editorId or a references[] label ("GetDistance <that chair>", "GetInSameCell
        // <that marker>", a GetMapMarkerVisible run-on). Those editorIds only enter the ref table in
        // BuildPlacements / BuildMapMarkers / BuildReferences (Generator.Build.cs pass-2 lines ~115-117).
        // Therefore:
        //
        //     ANY step that runs BEFORE BuildReferences MUST NOT call BuildCondition directly —
        //     it must queue the ConditionSpec with DeferCondition(), which WireDeferredConditions
        //     drains after placements and labels exist.
        //
        // Resolving eagerly in an early step means the ref table holds BASE RECORDS ONLY, so a
        // placement/label param silently drops the whole condition (and a placement/label `reference`
        // silently drops the run-on) — the gate then passes/fails on nothing. This was a live bug in
        // WirePerks / BuildStoryManager / BuildStandaloneQuestAliases / WireScenes; dialogue, banter and
        // package conditions were already deferred past the placement passes on purpose, which is exactly
        // the same rule stated by ordering instead of by comment. The `refsIndexed` guard below fails
        // LOUDLY (a build warning) if a new early call site ever appears — do not silence it, defer it.
        //
        // Build one ConditionFloat from a spec entry, or null (with a warning) if it's malformed.
        // aliasIndexByName (optional) lets GetIsAliasRef resolve an alias NAME → the owning quest's
        // alias index. Only the quest-scoped call sites (dialogue/scene/stage/objective) pass it;
        // package/perk/recipe have no owning quest, so a GetIsAliasRef there warns and is dropped.
        private ConditionFloat? BuildCondition(ConditionSpec c, string label,
            IReadOnlyDictionary<string, int>? aliasIndexByName = null, FormKey? owningScene = null)
        {
            if (!refsIndexed)
                Warn($"  ! {label}: BUILD-ORDER BUG — condition built before placements/references[] are in "
                    + "the ref table, so a param/reference naming a placement or label cannot resolve. "
                    + "Queue it with DeferCondition() instead (see the rule on BuildCondition).");

            CompareOperator op;
            switch (c.Comparison)
            {
                case "==": case "=": op = CompareOperator.EqualTo; break;
                case "!=":           op = CompareOperator.NotEqualTo; break;
                case ">":            op = CompareOperator.GreaterThan; break;
                case ">=":           op = CompareOperator.GreaterThanOrEqualTo; break;
                case "<":            op = CompareOperator.LessThan; break;
                case "<=":           op = CompareOperator.LessThanOrEqualTo; break;
                // Also accept the CompareOperator enum names (EqualTo/GreaterThanOrEqualTo/…) so a
                // perk spec can author either form.
                default:
                    if (Enum.TryParse<CompareOperator>(c.Comparison, ignoreCase: true, out var named)) op = named;
                    else { Warn($"  ! {label}: bad comparison '{c.Comparison}' (use == != > >= < <= or the enum names)"); return null; }
                    break;
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

            var data = BuildConditionData(c, label, aliasIndexByName, owningScene, paramFk, hasParam);
            if (data is null) return null;
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

    }
}
