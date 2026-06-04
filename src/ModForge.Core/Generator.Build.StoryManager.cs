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
            var branchByRoot = new Dictionary<FormKey, StoryManagerBranchNode>();
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
                    quest.Aliases.Add(alias);
                    nextId++;
                }
                quest.NextAliasID = nextId;

                // One shared branch per event root (created on first use), hung under the vanilla root.
                if (!branchByRoot.TryGetValue(def.Root, out var branch))
                {
                    branch = mod.StoryManagerBranchNodes.AddNew();
                    branch.EditorID = $"MFSM_{se.Event}_SMBranch";
                    branch.Parent.SetTo(def.Root);
                    branchByRoot[def.Root] = branch;
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
