namespace ModForge;

/// <summary>A hand-built demo plugin (general content + dialogue + ESL) — toolchain sanity check
/// and a ready-made translate target.</summary>
public static class Demo
{
    // -------------------------------------------------------------------------------
    //  CreateDemoPlugin — demo plugin (general content + dialogue + ESL); also a translate target
    // -------------------------------------------------------------------------------
    /// <summary>Build the demo mod in memory. The caller writes it (<c>mod.WriteToBinary</c>).</summary>
    public static ISkyrimMod CreateDemoPlugin(ModKey key)
    {
        var mod = new SkyrimMod(key, SkyrimRelease.SkyrimSE);

        var misc = mod.MiscItems.AddNew();
        misc.EditorID = "MF_DemoMisc"; misc.Name = "Forged Trinket"; misc.Value = 10; misc.Weight = 0.5f;

        var book = mod.Books.AddNew();
        book.EditorID = "MF_DemoBook"; book.Name = "Forged Note";
        book.BookText = "A short note, here in English, ready to be translated.";

        var npc = mod.Npcs.AddNew();
        npc.EditorID = "MF_DemoNpc"; npc.Name = "Aldric the Forged";

        var quest = mod.Quests.AddNew();
        quest.EditorID = "MF_DemoQuest"; quest.Name = "A Forged Errand";
        quest.Flags |= Quest.Flag.StartGameEnabled;  // must run for its dialogue to load
        quest.Priority = 50;
        quest.Objectives.Add(new QuestObjective { Index = 10, DisplayText = "Speak with Aldric" });

        var branch = mod.DialogBranches.AddNew();
        branch.EditorID = "MF_DemoBranch"; branch.Quest.SetTo(quest);
        branch.Category = DialogBranch.CategoryType.Player;
        branch.Flags = DialogBranch.Flag.TopLevel;   // top-level menu option when you talk to the NPC

        var topic = mod.DialogTopics.AddNew();
        topic.EditorID = "MF_DemoTopic"; topic.Quest.SetTo(quest); topic.Branch.SetTo(branch);
        topic.Category = DialogTopic.CategoryEnum.Topic;
        topic.Subtype = DialogTopic.SubtypeEnum.Custom;
        topic.SubtypeName = RecordType.Null;
        topic.Name = "Tell me about this place.";
        topic.Priority = 50f;
        branch.StartingTopic.SetTo(topic);

        var info = new DialogResponses(mod);
        info.Responses.Add(new DialogResponse
        {
            Text = "Welcome, traveler. Everything here was forged on Linux.",
            ResponseNumber = 1,
            Emotion = Emotion.Neutral,
        });
        var cond = new ConditionFloat
        {
            CompareOperator = CompareOperator.EqualTo,
            ComparisonValue = 1f,
            Data = new GetIsIDConditionData(),
        };
        ((GetIsIDConditionData)cond.Data).Object.Link.SetTo(npc);
        info.Conditions.Add(cond);
        topic.Responses.Add(info);

        mod.IsSmallMaster = true; // ESL flag (≤2048 new records — FormIDs 0x800–0xFFF)
        return mod;
    }
}
