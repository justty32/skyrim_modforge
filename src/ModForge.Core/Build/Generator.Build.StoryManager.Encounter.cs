namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // #6: create the per-quest LastFired float GLOB + attach the reusable MFEncounterCooldown quest
        // script (merged into any existing QuestAdapter so alias/stage fragments coexist). Unconditional
        // attach (the prebuilt .pex ships with the mod, like the scene/identity controllers).
        private void AttachEncounterCooldown(Quest quest, QuestSpec qs, float hours)
        {
            var g = mod.Globals.AddNewFloat();
            g.EditorID = $"{qs.EditorId}_LastFired";
            g.Data = 0f;

            var qad = quest.VirtualMachineAdapter as QuestAdapter ?? new QuestAdapter { Version = 5, ObjectFormat = 2 };
            var entry = new ScriptEntry { Name = Generator.EncounterCooldownScript, Flags = ScriptEntry.Flag.Local };
            var lp = new ScriptObjectProperty { Name = "LastFired", Flags = ScriptProperty.Flag.Edited };
            lp.Object.SetTo(g.FormKey);
            entry.Properties.Add(lp);
            entry.Properties.Add(new ScriptFloatProperty { Name = "CooldownHours", Data = hours, Flags = ScriptProperty.Flag.Edited });
            qad.Scripts.Add(entry);
            quest.VirtualMachineAdapter = qad;
            scriptsAttached++;
        }

        // F組 #3: a quest declaring `spawn` gets the reusable MFDynamicSpawn quest script (dynamic
        // near-player navmesh spawn on quest start). Runs after BuildStoryManager/BuildStandaloneQuestAliases
        // and before WireQuestStages so the QuestAdapter merge keeps alias/stage fragments intact.
        public void BuildQuestSpawns()
        {
            foreach (var qs in spec.Quests)
            {
                if (qs.Spawn is not { } sp) continue;
                if (string.IsNullOrEmpty(qs.EditorId) || !questsByEd.TryGetValue(qs.EditorId, out var quest)) continue;

                var qad = quest.VirtualMachineAdapter as QuestAdapter ?? new QuestAdapter { Version = 5, ObjectFormat = 2 };
                var entry = new ScriptEntry { Name = Generator.DynamicSpawnScript, Flags = ScriptEntry.Flag.Local };
                var fp = new ScriptObjectProperty { Name = "SpawnForm", Flags = ScriptProperty.Flag.Edited };
                if (TryResolveRef(sp.Form, formKeyByEd, out var ffk)) { fp.Object.SetTo(ffk); linksWired++; if (LooksExternalRef(sp.Form)) extLinks++; }
                else Warn($"  ! quest '{qs.EditorId}' spawn.form '{sp.Form}' unresolved — spawn will no-op");
                entry.Properties.Add(fp);
                entry.Properties.Add(new ScriptIntProperty   { Name = "Count",       Data = Math.Max(1, sp.Count), Flags = ScriptProperty.Flag.Edited });
                entry.Properties.Add(new ScriptFloatProperty { Name = "MinDistance", Data = sp.MinDistance, Flags = ScriptProperty.Flag.Edited });
                entry.Properties.Add(new ScriptFloatProperty { Name = "MaxDistance", Data = sp.MaxDistance, Flags = ScriptProperty.Flag.Edited });
                entry.Properties.Add(new ScriptBoolProperty  { Name = "SnapToNavmesh", Data = sp.SnapToNavmesh, Flags = ScriptProperty.Flag.Edited });
                qad.Scripts.Add(entry);
                quest.VirtualMachineAdapter = qad;
                scriptsAttached++;
            }
        }

        // Wire an alias's match-filter CTDA (shared by findMatching / findMatchingLocation / findInLocationAlias):
        // these conditions decide WHICH ref/location in scope the engine picks. Both callers
        // (BuildStoryManager / BuildStandaloneQuestAliases) run BEFORE placements and references[], so the
        // conditions are DEFERRED — a match filter naming a placement or a label ("the ref nearest THAT
        // marker") could not otherwise resolve. See the build-order rule on BuildCondition.
        // Queued AFTER whatever fill-shape conditions the caller already appended (findMatchingLocation's
        // LocationHasKeyword / GetInCurrentLocAlias), and nothing else touches alias.Conditions in between,
        // so the emitted order is unchanged.
        private void WireAliasMatchConditions(QuestAlias alias, QuestSpec qs, QuestAliasSpec aSpec, string kindLabel)
        {
            foreach (var cs in aSpec.Conditions)
                DeferCondition(alias.Conditions, cs, $"quest '{qs.EditorId}' alias '{aSpec.Name}' {kindLabel} condition");
        }
    }
}
