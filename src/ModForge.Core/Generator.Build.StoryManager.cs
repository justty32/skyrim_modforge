using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // 把 spec 裏每個帶 storyEvent 的 quest 變成可被 SM 啟動：設 Quest.Event/EventConditions、清
        // StartGameEnabled、建 aliases，並 additive 生 SMBN→SMQN 掛到原版事件根下。在 BuildQuests() 後跑。
        public void BuildStoryManager()
        {
            // Sibling SM nodes under one parent form a linked list via PreviousSibling (decoded from
            // vanilla: every SMQN/SMBN child of a shared parent points at its previous sibling). With a
            // SINGLE child a null head works, but MULTIPLE unchained children make the engine's sibling
            // traversal miss all-but-one — so chain each new branch to the prior one under the same root.
            var lastBranchByParent = new Dictionary<FormKey, StoryManagerBranchNode>();
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
                    }
                    if (aSpec.Optional) alias.Flags |= QuestAlias.Flag.Optional;
                    quest.Aliases.Add(alias);
                    nextId++;
                }
                quest.NextAliasID = nextId;

                var branch = mod.StoryManagerBranchNodes.AddNew();
                branch.EditorID = $"{qs.EditorId}_SMBranch";
                branch.Parent.SetTo(def.Root);
                if (lastBranchByParent.TryGetValue(def.Root, out var prevBranch))
                    branch.PreviousSibling.SetTo(prevBranch);
                lastBranchByParent[def.Root] = branch;

                var qnode = mod.StoryManagerQuestNodes.AddNew();
                qnode.EditorID = $"{qs.EditorId}_SMQuestNode";
                qnode.Parent.SetTo(branch);
                var entry = new StoryManagerQuest();
                entry.Quest.SetTo(quest);
                qnode.Quests.Add(entry);
            }
        }
    }
}
