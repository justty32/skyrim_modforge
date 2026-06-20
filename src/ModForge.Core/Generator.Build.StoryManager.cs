using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // 把 spec 裏每個帶 storyEvent 的 quest 變成可被 SM 啟動：設 Quest.Event/EventConditions、清
        // StartGameEnabled、建 aliases，並 additive 接到原版事件根下。在 BuildQuests() 後跑。
        public void BuildStoryManager()
        {
            // Structure decoded from vanilla (the WIKill tree): an event root has ONE branch, and ALL
            // the event's quests live as sibling QUEST NODES under that single branch — NOT one branch
            // per quest. Sibling branches under an event root are mutually-exclusive handlers (the engine
            // picks one), so N branches → only the head's quest fires; N quest nodes under ONE branch all
            // evaluate per event (vanilla runs 2 such kill quest nodes). Sibling nodes must be chained via
            // PreviousSibling (first = null head) or the engine's sibling walk misses all-but-one.
            // Keyed by "root|keyword" — engine-native events have an empty keyword (one branch per
            // root, as before); ScriptEvent quests sharing a keyword share one keyword-filtered branch,
            // and different keywords get different (mutually-exclusive) branches like vanilla WE does.
            var branchByKey = new Dictionary<string, StoryManagerBranchNode>();
            var lastQNodeByBranch = new Dictionary<FormKey, StoryManagerQuestNode>();
            foreach (var qs in spec.Quests)
            {
                if (qs.StoryEvent is not { } se) continue;
                if (string.IsNullOrEmpty(qs.EditorId) || !questsByEd.TryGetValue(qs.EditorId, out var quest)) continue;
                if (!StoryManagerEvents.TryGet(se.Event, out var def)) continue; // validator 已擋未知事件

                // The engine's passive CastMagicEvent SM root does NOT fire on normal player spell casts
                // (in-game 2026-06-20: an OnStoryCastMagic handler never ran). The wiring builds fine but
                // never triggers. For a cast trigger, use a scripted magic effect that calls
                // MFStoryEventDispatch.Fire -> a ScriptEvent quest (see examples/skill_cast_spec.json or
                // story-manager-magictrigger.json — that path is in-game confirmed).
                if (se.Event.Equals("CastMagic", StringComparison.OrdinalIgnoreCase))
                    Warn($"  ! quest '{qs.EditorId}' storyEvent 'CastMagic': the passive Cast Magic SM event does NOT fire on normal player casts — use a scripted magic effect -> MFStoryEventDispatch.Fire -> a ScriptEvent quest (see examples/skill_cast_spec.json)");

                quest.Event = def.Code;
                quest.Flags &= ~Quest.Flag.StartGameEnabled;
                foreach (var cs in se.Conditions)
                    if (BuildCondition(cs, $"quest '{qs.EditorId}' storyEvent condition") is { } cond)
                        quest.EventConditions.Add(cond);

                // #5 locationFilter: OR'd GetKeywordDataForCurrentLocation conditions (fires only when the
                // new location has ANY listed LocType keyword; the OR group ANDs after the event conditions).
                for (int li = 0; li < se.LocationFilter.Count; li++)
                {
                    var cs = new ConditionSpec
                    {
                        Function = "GetKeywordDataForCurrentLocation",
                        Param = se.LocationFilter[li],
                        Comparison = "==",
                        Value = 1,
                        Or = li < se.LocationFilter.Count - 1,   // OR within the group; last one closes it
                    };
                    if (BuildCondition(cs, $"quest '{qs.EditorId}' locationFilter[{li}]") is { } cond)
                        quest.EventConditions.Add(cond);
                }

                BuildQuestAliases(quest, qs, def);

                // #6 cooldownHours: anti-spam GLOB + reusable cooldown script (EE_WITimeout pattern).
                if (se.CooldownHours > 0f)
                    AttachEncounterCooldown(quest, qs, se.CooldownHours);

                // ScriptEvent quests are gated by a keyword; that keyword keys (and filters) the branch.
                bool isScriptEvent = se.Event.Equals("ScriptEvent", StringComparison.OrdinalIgnoreCase);
                string kw = isScriptEvent ? se.Keyword : "";
                string branchKey = $"{def.Root}|{kw}";

                // One shared branch per (event root, keyword), created on first use, hung under the root.
                if (!branchByKey.TryGetValue(branchKey, out var branch))
                {
                    branch = mod.StoryManagerBranchNodes.AddNew();
                    branch.EditorID = isScriptEvent ? $"MFSM_{se.Event}_{kw}_SMBranch" : $"MFSM_{se.Event}_SMBranch";
                    branch.Parent.SetTo(def.Root);
                    // ScriptEvent: filter to our keyword so only content firing THIS keyword starts the
                    // quest. CK: "GetEventData Keyword GetIsID <KYWD> == 1" (Mutagen native, no binary).
                    if (isScriptEvent && formKeyByEd.TryGetValue(kw, out var kwFk))
                    {
                        var cond = new ConditionFloat
                        {
                            CompareOperator = CompareOperator.EqualTo,
                            ComparisonValue = 1,
                            Data = new GetEventDataConditionData
                            {
                                Function = GetEventDataConditionData.EventFunction.GetIsID,
                                Member = GetEventDataConditionData.EventMember.Keyword,
                                RunOnType = Condition.RunOnType.Subject,
                            },
                        };
                        ((GetEventDataConditionData)cond.Data).Record.SetTo(kwFk);
                        branch.Conditions.Add(cond);
                    }
                    branchByKey[branchKey] = branch;
                }

                // One quest node per quest, all chained as siblings under the shared branch.
                var qnode = mod.StoryManagerQuestNodes.AddNew();
                qnode.EditorID = $"{qs.EditorId}_SMQuestNode";
                qnode.Parent.SetTo(branch);
                if (lastQNodeByBranch.TryGetValue(branch.FormKey, out var prevQNode))
                    qnode.PreviousSibling.SetTo(prevQNode);
                lastQNodeByBranch[branch.FormKey] = qnode;
                var entry = new StoryManagerQuest();
                entry.Quest.SetTo(quest);
                qnode.Quests.Add(entry);
            }
        }
    }
}
