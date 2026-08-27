namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // --- quests, dialogue, scenes, script attachments ---
        // 這個方法原本是一個 271 行的四段流程（quest／conditionTemplate／dialogue／scene）。
        // 拆檔後這裡只留分派，四段各自成為 partial 方法；`stageIndexByQuest` 是唯一跨段的資料，
        // 由 quest 段產出、dialogue 段消費，所以用回傳值明著傳，不留隱藏狀態。
        public void ValidateQuestsAndDialogue()
        {
            var stageIndexByQuest = ValidateQuestRecords();
            ValidateConditionTemplates();
            ValidateDialogueRecords(stageIndexByQuest);
            ValidateScenes();
            ValidateScriptAttachments();
        }

        // Quest stage indices unique + ascending; objective↔stage refs; log-entry conditions valid.
        // Returns每個 quest 的 stage index 集合，供 dialogue 段檢查 `setStage`。
        private Dictionary<string, HashSet<int>> ValidateQuestRecords()
        {
            var stageIndexByQuest = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
            foreach (var q in spec.Quests)
            {
                var seen = new HashSet<int>();
                int prev = -1;
                int startUpStages = 0;
                foreach (var st in q.Stages)
                {
                    if (st.StartUpStage) startUpStages++;
                    if (!seen.Add(st.Index))
                        Problems.Add($"quest '{q.EditorId}' has duplicate stage index {st.Index}");
                    if (st.Index <= prev)
                        Problems.Add($"quest '{q.EditorId}' stage index {st.Index} is not ascending (must list stages in increasing order)");
                    prev = st.Index;
                    if (st.CompleteQuest && st.FailQuest)
                        Problems.Add($"quest '{q.EditorId}' stage {st.Index} sets both completeQuest and failQuest");
                    foreach (var cs in st.Conditions)
                    {
                        if (string.IsNullOrWhiteSpace(cs.Function))
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} condition has empty function");
                        else if (!Enum.TryParse<Condition.Function>(cs.Function, true, out _))
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} condition invalid function '{cs.Function}'");
                        if (!string.IsNullOrWhiteSpace(cs.Comparison)
                            && cs.Comparison is not ("==" or "=" or "!=" or ">" or ">=" or "<" or "<=")
                            && !Enum.TryParse<CompareOperator>(cs.Comparison, true, out _))
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} condition invalid comparison '{cs.Comparison}'");
                        CheckRef(cs.Param, $"quest '{q.EditorId}' stage {st.Index} condition param");
                    }
                    foreach (var ig in st.InstanceGlobals)
                    {
                        if (string.IsNullOrWhiteSpace(ig.Global))
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} instanceGlobal has empty 'global'");
                        else
                            CheckRef(ig.Global, $"quest '{q.EditorId}' stage {st.Index} instanceGlobal");
                        bool hasRandom = ig.RandomMin is not null || ig.RandomMax is not null;
                        if (hasRandom && (ig.RandomMin is null || ig.RandomMax is null))
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} instanceGlobal '{ig.Global}' needs both randomMin and randomMax (or neither)");
                        else if (ig.RandomMin is int lo && ig.RandomMax is int hi && lo > hi)
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} instanceGlobal '{ig.Global}' randomMin {lo} > randomMax {hi}");
                        if (hasRandom && ig.Value is not null)
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} instanceGlobal '{ig.Global}' sets both a random range and a fixed value (use one)");
                    }
                    // K組 plain global writes — each needs a resolvable GLOB ref.
                    foreach (var gw in st.GlobalWrites)
                    {
                        if (string.IsNullOrWhiteSpace(gw.Global))
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} globalWrite has empty 'global'");
                        else
                            CheckRef(gw.Global, $"quest '{q.EditorId}' stage {st.Index} globalWrite");
                    }
                    // Stage-fragment persist/syncPerks (Idea #20 Phase 0). A stage has no akSpeakerRef, so
                    // "speaker" is rejected — the key must be "player" or an arbitrary ref.
                    if (st.Persist is { } stp) ValidatePersistBlock(stp, $"quest '{q.EditorId}' stage {st.Index} persist", allowSpeaker: false);
                    if (st.SyncPerks is { } sts) ValidateSyncPerksBlock(sts, $"quest '{q.EditorId}' stage {st.Index} syncPerks", allowSpeaker: false);
                    ValidateStorageWrites(st.StorageWrites, $"quest '{q.EditorId}' stage {st.Index} storageWrite", allowSpeaker: false);
                }
                stageIndexByQuest[q.EditorId] = seen;
                if (startUpStages > 1)
                    Problems.Add($"quest '{q.EditorId}' marks {startUpStages} stages as startUpStage (at most one allowed — the engine auto-runs exactly one on quest start)");
                if (q.Spawn is { } sp)   // F組 #3 dynamic spawn
                {
                    if (string.IsNullOrWhiteSpace(sp.Form)) Problems.Add($"quest '{q.EditorId}' spawn has empty 'form' (an ActorBase/LeveledNpc to spawn)");
                    else CheckRef(sp.Form, $"quest '{q.EditorId}' spawn.form");
                    if (sp.Count < 1) Problems.Add($"quest '{q.EditorId}' spawn.count must be >= 1 (got {sp.Count})");
                    if (sp.MinDistance < 0 || sp.MaxDistance < 0) Problems.Add($"quest '{q.EditorId}' spawn distances must be >= 0");
                    else if (sp.MinDistance > sp.MaxDistance) Problems.Add($"quest '{q.EditorId}' spawn.minDistance {sp.MinDistance} > maxDistance {sp.MaxDistance}");
                }
                // spawn / cooldownHours fire from the startUpStage fragment on quest start (OnInit is
                // unreliable — it runs once per quest lifetime, not on every SM relaunch). Without a
                // startUpStage there is nothing to trigger them, so the encounter silently does nothing.
                bool needsStartup = q.Spawn is not null || (q.StoryEvent is { } cdSe && cdSe.CooldownHours > 0f);
                if (needsStartup && startUpStages == 0)
                    Problems.Add($"quest '{q.EditorId}' declares spawn/cooldownHours but has no startUpStage — these trigger from the startUpStage fragment on quest start; add a stage with startUpStage:true or nothing will fire");
                var objIdx = new HashSet<int>();
                var aliasNames = new HashSet<string>(q.Aliases.Select(a => a.Name), StringComparer.OrdinalIgnoreCase);
                foreach (var o in q.Objectives)
                {
                    if (!objIdx.Add(o.Index))
                        Problems.Add($"quest '{q.EditorId}' has duplicate objective index {o.Index}");
                    if (o.ShowStage >= 0 && !seen.Contains(o.ShowStage))
                        Problems.Add($"quest '{q.EditorId}' objective {o.Index} showStage {o.ShowStage} has no matching stage");
                    if (o.CompleteStage >= 0 && !seen.Contains(o.CompleteStage))
                        Problems.Add($"quest '{q.EditorId}' objective {o.Index} completeStage {o.CompleteStage} has no matching stage");
                    foreach (var t in o.Targets)
                        if (string.IsNullOrWhiteSpace(t.Alias))
                            Problems.Add($"quest '{q.EditorId}' objective {o.Index} has a target with no alias");
                        else if (!aliasNames.Contains(t.Alias))
                            Problems.Add($"quest '{q.EditorId}' objective {o.Index} target alias '{t.Alias}' is not an alias on this quest");
                }
            }
            return stageIndexByQuest;
        }
    }
}
