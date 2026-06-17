namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
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

        // Wire an alias's match-filter CTDA (shared by findMatching / findMatchingLocation / findInLocationAlias):
        // these conditions decide WHICH ref/location in scope the engine picks.
        private void WireAliasMatchConditions(QuestAlias alias, QuestSpec qs, QuestAliasSpec aSpec, string kindLabel)
        {
            foreach (var cs in aSpec.Conditions)
                if (BuildCondition(cs, $"quest '{qs.EditorId}' alias '{aSpec.Name}' {kindLabel} condition") is { } cond)
                    alias.Conditions.Add(cond);
        }
    }
}
