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

                quest.Event = def.Code;
                quest.Flags &= ~Quest.Flag.StartGameEnabled;
                foreach (var cs in se.Conditions)
                    if (BuildCondition(cs, $"quest '{qs.EditorId}' storyEvent condition") is { } cond)
                        quest.EventConditions.Add(cond);

                uint nextId = 0;
                foreach (var aSpec in qs.Aliases)
                {
                    var alias = new QuestAlias { ID = nextId, Name = aSpec.Name };
                    if (StoryManagerEvents.TryParseFill(aSpec.Fill, out var kind, out var arg))
                    {
                        if (kind.Equals("fromEvent", StringComparison.OrdinalIgnoreCase)
                            && def.Slots.TryGetValue(arg, out var slot))
                        {
                            alias.FindMatchingRefFromEvent = new FindMatchingRefFromEvent
                            {
                                FromEvent = def.Code,
                                EventData = (byte[])slot.Clone(),
                            };
                            // The alias TYPE must match the slot's payload kind or the engine fills
                            // null: a Location slot ("L1"/"L2", first byte 'L'=0x4C) needs a LOCATION
                            // alias; a ref slot ("R1"/"R2") a REFERENCE alias. (In-game: a ChangeLocation
                            // L2 fill returned null because the alias defaulted to Reference type.)
                            alias.Type = slot.Length > 0 && slot[0] == (byte)'L'
                                ? QuestAlias.TypeEnum.Location
                                : QuestAlias.TypeEnum.Reference;
                        }
                        else if (kind.Equals("forced", StringComparison.OrdinalIgnoreCase)
                            && TryResolveRef(arg, formKeyByEd, out var fk))
                        {
                            alias.ForcedReference.SetTo(fk);
                        }
                        else if (kind.Equals("uniqueActor", StringComparison.OrdinalIgnoreCase)
                            && TryResolveRef(arg, formKeyByEd, out var uaFk))
                        {
                            // QuestAlias.UniqueActor (ALUA) = a unique NPC base record this alias
                            // resolves to. <ref> is an in-spec NPC editorId or Plugin.esm:0xID.
                            alias.UniqueActor.SetTo(uaFk);
                            // AllowReserved is REQUIRED here (vanilla sets it on EVERY unique-actor
                            // alias): a unique NPC's persistent ref is usually already reserved by
                            // other quests, and without this flag the fill fails — which, for a
                            // non-optional alias, blocks the whole quest from starting. (In-game:
                            // Ulfric uniqueActor alias kept the quest stopped until this was set.)
                            // NB: QuestAlias.Flags defaults to null, and `|=` on a null lifts to null
                            // (no-op) — must seed from GetValueOrDefault() or the flag never sticks.
                            alias.Flags = alias.Flags.GetValueOrDefault() | QuestAlias.Flag.AllowReserved;
                        }
                    }
                    if (aSpec.Optional)
                        alias.Flags = alias.Flags.GetValueOrDefault() | QuestAlias.Flag.Optional;
                    // AllowReserved lets the fill grab a ref another quest has reserved (via
                    // ReservesLocationOrReference). Without it, killing/targeting an actor a running
                    // quest holds (e.g. a Riverwood NPC reserved by a Freeform quest) fails to fill —
                    // and a required alias that can't fill blocks the whole quest from starting.
                    if (aSpec.AllowReserved)
                        alias.Flags = alias.Flags.GetValueOrDefault() | QuestAlias.Flag.AllowReserved;
                    quest.Aliases.Add(alias);
                    nextId++;
                }
                quest.NextAliasID = nextId;

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
