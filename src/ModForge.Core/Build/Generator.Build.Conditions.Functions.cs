namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        private ConditionData? BuildConditionData(ConditionSpec c, string label,
            IReadOnlyDictionary<string, int>? aliasIndexByName, FormKey? owningScene,
            FormKey paramFk, bool hasParam)
        {
            ConditionData? data;
            switch (c.Function.ToLowerInvariant())
            {
                case "hasperk":             { var d = new HasPerkConditionData();             if (hasParam) d.Perk.Link.SetTo(paramFk);      data = d; break; }
                case "getinfaction":        { var d = new GetInFactionConditionData();        if (hasParam) d.Faction.Link.SetTo(paramFk);   data = d; break; }
                case "getitemcount":        { var d = new GetItemCountConditionData();        if (hasParam) d.ItemOrList.Link.SetTo(paramFk); data = d; break; }
                case "getglobalvalue":      { var d = new GetGlobalValueConditionData();      if (hasParam) d.Global.Link.SetTo(paramFk);    data = d; break; }
                case "getstage":            { var d = new GetStageConditionData();            if (hasParam) d.Quest.Link.SetTo(paramFk);     data = d; break; }
                case "getisid":             { var d = new GetIsIDConditionData();             if (hasParam) d.Object.Link.SetTo(paramFk);    data = d; break; }
                case "getisrace":           { var d = new GetIsRaceConditionData();           if (hasParam) d.Race.Link.SetTo(paramFk);      data = d; break; }
                case "haskeyword":          { var d = new HasKeywordConditionData();          if (hasParam) d.Keyword.Link.SetTo(paramFk);   data = d; break; }
                case "wornhaskeyword":      { var d = new WornHasKeywordConditionData();      if (hasParam) d.Keyword.Link.SetTo(paramFk);   data = d; break; }
                case "isspelltarget":       { var d = new IsSpellTargetConditionData();       if (hasParam) d.MagicItem.Link.SetTo(paramFk); data = d; break; }
                case "getrelationshiprank": { var d = new GetRelationshipRankConditionData(); if (hasParam) d.TargetNpc.Link.SetTo(paramFk); data = d; break; }
                // Cross-quest / contextual gates (form arg). GetDistance is compared to the value as a
                // distance (units); GetIsVoiceType lets dialogue target a voice type / list.
                case "getquestcompleted":   { var d = new GetQuestCompletedConditionData();   if (hasParam) d.Quest.Link.SetTo(paramFk);          data = d; break; }
                case "getdistance":         { var d = new GetDistanceConditionData();         if (hasParam) d.Target.Link.SetTo(paramFk);         data = d; break; }
                case "getiscurrentpackage": { var d = new GetIsCurrentPackageConditionData(); if (hasParam) d.Package.Link.SetTo(paramFk);        data = d; break; }
                case "getisvoicetype":      { var d = new GetIsVoiceTypeConditionData();      if (hasParam) d.VoiceTypeOrList.Link.SetTo(paramFk); data = d; break; }
                // More cross-quest / location / state gates (form arg). GetInCell/GetInWorldspace gate by
                // place; GetEquipped by carried item; GetDeadCount counts dead instances of an NPC base.
                case "getquestrunning":     { var d = new GetQuestRunningConditionData();      if (hasParam) d.Quest.Link.SetTo(paramFk);          data = d; break; }
                // Two-param: GetStageDone(quest, stage) → 1 if that exact stage has been set. The quest is
                // `param`; the stage index is `stage` (a parameter, distinct from the comparison value).
                case "getstagedone":
                {
                    var d = new GetStageDoneConditionData();
                    if (hasParam) d.Quest.Link.SetTo(paramFk);
                    if (c.Stage < 0) { Warn($"  ! {label}: GetStageDone needs a 'stage' index (the stage to test)"); return null; }
                    d.Stage = c.Stage;
                    data = d; break;
                }
                case "getincell":           { var d = new GetInCellConditionData();           if (hasParam) d.Cell.Link.SetTo(paramFk);           data = d; break; }
                case "getincurrentloc":     { var d = new GetInCurrentLocConditionData();     if (hasParam) d.Location.Link.SetTo(paramFk);       data = d; break; }
                // Location-keyword tests (A組 #5 location-aware encounters). param = a LocType keyword
                // (LocTypeCity/LocTypeDungeon/…). GetKeywordDataForCurrentLocation = the player's CURRENT
                // location (the EE "PlayerLocation.HasKeyword(LocType)" routing); LocationHasKeyword = the
                // run-on ref's location. Keyword is IFormLinkOrIndex → set via .Keyword.Link.
                case "getkeyworddataforcurrentlocation":
                                            { var d = new GetKeywordDataForCurrentLocationConditionData(); if (hasParam) d.Keyword.Link.SetTo(paramFk); data = d; break; }
                case "locationhaskeyword":  { var d = new LocationHasKeywordConditionData();  if (hasParam) d.Keyword.Link.SetTo(paramFk);        data = d; break; }
                // LocAliasHasKeyword(aliasName, keyword): does the LOCATION held by alias <c.Alias> have the
                // keyword? The hold-detection mechanism (a myHoldImperial/… location alias). Needs the owning
                // quest (alias index), like GetIsAliasRef.
                case "localiashaskeyword":
                {
                    if (string.IsNullOrWhiteSpace(c.Alias))
                    { Warn($"  ! {label}: LocAliasHasKeyword needs an 'alias' (the location alias name)"); return null; }
                    if (aliasIndexByName is null || !aliasIndexByName.TryGetValue(c.Alias, out var laIdx))
                    { Warn($"  ! {label}: LocAliasHasKeyword alias '{c.Alias}' not found on the owning quest"); return null; }
                    var d = new LocAliasHasKeywordConditionData { LocationAliasIndex = laIdx };
                    if (hasParam) d.Keyword.Link.SetTo(paramFk);
                    data = d; break;
                }
                // Two-param: IsSceneActionComplete(scene, actionIndex) → 1 once that scene action finished.
                // The standard scene-phase "advance when the line is done" gate. Scene defaults to the
                // owning scene on a scene completion/start condition; the action index is author-supplied
                // (the action's position in the built SCEN — inspect with `scenediag`).
                case "issceneactioncomplete":
                {
                    var d = new IsSceneActionCompleteConditionData();
                    FormKey sceneFk;
                    if (!string.IsNullOrWhiteSpace(c.Scene))
                    {
                        if (!TryResolveRef(c.Scene, formKeyByEd, out sceneFk))
                        { Warn($"  ! {label}: IsSceneActionComplete scene '{c.Scene}' unresolved"); return null; }
                    }
                    else if (owningScene is { } os) sceneFk = os;
                    else { Warn($"  ! {label}: IsSceneActionComplete needs a 'scene' (only auto-defaulted on a scene condition)"); return null; }
                    d.Scene.Link.SetTo(sceneFk);
                    if (c.SceneActionIndex < 0) { Warn($"  ! {label}: IsSceneActionComplete needs a 'sceneActionIndex'"); return null; }
                    d.SceneActionIndex = c.SceneActionIndex;
                    data = d; break;
                }
                // Read a Papyrus script PROPERTY from a condition (L組): GetVMQuestVariable reads a
                // property on a quest's attached script (param = the quest), GetVMScriptVariable reads
                // one on an object's attached script (param = the object/ref). `variableName` is the
                // property name string (e.g. ITH's "PlayerInDialogue"); compared against `value`.
                case "getvmquestvariable":
                {
                    if (string.IsNullOrWhiteSpace(c.VariableName))
                    { Warn($"  ! {label}: GetVMQuestVariable needs a 'variableName' (the quest script property name)"); return null; }
                    if (!hasParam) { Warn($"  ! {label}: GetVMQuestVariable needs a 'param' (the quest whose script is read)"); return null; }
                    var d = new GetVMQuestVariableConditionData { VariableName = c.VariableName };
                    d.Quest.Link.SetTo(paramFk);
                    data = d; break;
                }
                case "getvmscriptvariable":
                {
                    if (string.IsNullOrWhiteSpace(c.VariableName))
                    { Warn($"  ! {label}: GetVMScriptVariable needs a 'variableName' (the script property name)"); return null; }
                    if (!hasParam) { Warn($"  ! {label}: GetVMScriptVariable needs a 'param' (the object whose attached script is read)"); return null; }
                    var d = new GetVMScriptVariableConditionData { VariableName = c.VariableName };
                    d.Target.Link.SetTo(paramFk);
                    data = d; break;
                }
                case "getinworldspace":     { var d = new GetInWorldspaceConditionData();     if (hasParam) d.WorldspaceOrList.Link.SetTo(paramFk); data = d; break; }
                case "getequipped":         { var d = new GetEquippedConditionData();         if (hasParam) d.ItemOrList.Link.SetTo(paramFk);     data = d; break; }
                case "getdeadcount":        { var d = new GetDeadCountConditionData();        if (hasParam) d.Npc.Link.SetTo(paramFk);            data = d; break; }
                // No-arg state functions. getsitting: furniture sit-state (0 none .. 3 sitting / 4 sleeping;
                // compare ==3); getgold: the run-on actor's gold; getmapmarkervisible: 1 if the run-on
                // marker is shown on the map (gate on RunOn=Reference to a placed map marker).
                case "getsitting":          data = new GetSittingConditionData();          break;
                case "getgold":             data = new GetGoldConditionData();             break;
                case "getmapmarkervisible": data = new GetMapMarkerVisibleConditionData(); break;
                case "getisaliasref":       // is the run-on actor the ref filling alias <c.Alias> on the owning quest?
                {
                    if (string.IsNullOrWhiteSpace(c.Alias))
                    { Warn($"  ! {label}: GetIsAliasRef needs an 'alias' (the quest alias name)"); return null; }
                    if (aliasIndexByName is null)
                    { Warn($"  ! {label}: GetIsAliasRef has no owning quest here (only valid on dialogue/scene/stage/objective conditions)"); return null; }
                    if (!aliasIndexByName.TryGetValue(c.Alias, out var aliasIdx))
                    { Warn($"  ! {label}: GetIsAliasRef alias '{c.Alias}' not found on the owning quest"); return null; }
                    data = new GetIsAliasRefConditionData { ReferenceAliasIndex = aliasIdx }; break;
                }
                case "getactorvalue":       // ActorValue arg (e.g. WaitingForPlayer), not a form ref
                {
                    var d = new GetActorValueConditionData();
                    if (Enum.TryParse<ActorValue>(c.ActorValue, ignoreCase: true, out var av)) d.ActorValue = av;
                    else { Warn($"  ! {label}: bad/missing actorValue '{c.ActorValue}'"); return null; }
                    data = d; break;
                }
                case "getbaseactorvalue":   // base (un-buffed) AV — perks gate on this (e.g. Destruction >= 30)
                {
                    var d = new GetBaseActorValueConditionData();
                    if (Enum.TryParse<ActorValue>(c.ActorValue, ignoreCase: true, out var av)) d.ActorValue = av;
                    else { Warn($"  ! {label}: bad/missing actorValue '{c.ActorValue}'"); return null; }
                    data = d; break;
                }
                case "getequippeditemtype": // CastSource arg (Left|Right|Voice|Instant), not a form ref
                {
                    var src = Enum.TryParse<CastSource>(c.ItemType, ignoreCase: true, out var s) ? s : CastSource.Right;
                    data = new GetEquippedItemTypeConditionData { ItemSource = src }; break;
                }
                case "getactorvaluepercent": // AV as a 0..1 fraction — use value 0.5 for "below half". ActorValue arg.
                {
                    var d = new GetActorValuePercentConditionData();
                    if (Enum.TryParse<ActorValue>(c.ActorValue, ignoreCase: true, out var av)) d.ActorValue = av;
                    else { Warn($"  ! {label}: bad/missing actorValue '{c.ActorValue}'"); return null; }
                    data = d; break;
                }
                // No-argument situational functions (no form ref, no actorValue). Compared against `value`:
                //   getcurrenttime   game hour 0..24 (e.g. >= 20 for "after 8pm")
                //   isininterior     1 indoors / 0 outdoors
                //   isincombat       1 if the run-on actor is fighting
                //   getrandompercent 0..99 roll (e.g. < 25 ⇒ ~25% of the time — adds line variety)
                case "getcurrenttime":   data = new GetCurrentTimeConditionData();   break;
                case "isininterior":     data = new IsInInteriorConditionData();     break;
                case "isincombat":       data = new IsInCombatConditionData();       break;
                case "getrandompercent": data = new GetRandomPercentConditionData(); break;
                case "getlevel":         data = new GetLevelConditionData();         break;
                // EPTemperingItemIsEnchanted — the vanilla temper guard so an enchanted item without
                // the Arcane-Blacksmith perk can't be improved. No form arg; pair with `or: true`.
                case "temperisenchanted":
                case "eptemperingitemisenchanted": data = new EPTemperingItemIsEnchantedConditionData(); break;
                default:
                    Warn($"  ! {label}: unsupported function '{c.Function}' "
                        + "(have HasPerk/GetInFaction/GetItemCount/GetGlobalValue/GetStage/GetIsID/GetRelationshipRank/"
                        + "GetActorValue/GetActorValuePercent/GetCurrentTime/IsInInterior/IsInCombat/GetRandomPercent/TemperIsEnchanted/"
                        + "GetQuestCompleted/GetDistance/GetIsCurrentPackage/GetIsVoiceType/GetIsAliasRef/"
                        + "GetQuestRunning/GetInCell/GetInWorldspace/GetEquipped/GetDeadCount/GetSitting/GetGold/GetMapMarkerVisible/GetStageDone/GetInCurrentLoc/IsSceneActionComplete/GetVMQuestVariable/GetVMScriptVariable)");
                    return null;
            }

            return data;
        }
    }
}
