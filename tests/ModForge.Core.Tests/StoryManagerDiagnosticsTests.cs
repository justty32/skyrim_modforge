using ModForge;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge.Tests;

public sealed class StoryManagerDiagnosticsTests
{
    [Fact]
    public void Generated_story_manager_tree_is_clean()
    {
        var spec = new ModSpec();
        foreach (var editorId in new[] { "FirstQuest", "SecondQuest" })
            spec.Quests.Add(new QuestSpec
            {
                EditorId = editorId,
                StoryEvent = new QuestStoryEventSpec { Event = "KillActor" },
                Aliases = { new QuestAliasSpec { Name = "Victim", Fill = "fromEvent:victim" } },
            });

        var mod = Generator.Build(spec, ModKey.FromNameAndExtension("SmCheck.esp")).Mod;

        Assert.Empty(StoryManagerDiagnostics.Analyze(mod));
    }

    [Fact]
    public void Generated_story_manager_tree_stays_clean_after_binary_roundtrip()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "RoundtripQuest",
            StoryEvent = new QuestStoryEventSpec { Event = "KillActor" },
            Aliases = { new QuestAliasSpec { Name = "Victim", Fill = "fromEvent:victim" } },
        });
        var mod = Generator.Build(spec, ModKey.FromNameAndExtension("SmRoundtrip.esp")).Mod;
        var dir = Path.Combine(Path.GetTempPath(), "mf-smcheck-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "SmRoundtrip.esp");
        Directory.CreateDirectory(dir);
        try
        {
            PluginIo.Write(mod, path);
            using var overlay = SkyrimMod.CreateFromBinaryOverlay(
                new ModPath(path), SkyrimRelease.SkyrimSE);
            Assert.Empty(StoryManagerDiagnostics.Analyze(overlay));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Reports_parent_cycles_orphans_duplicates_and_broken_sibling_chains()
    {
        var mod = Demo.CreateDemoPlugin(ModKey.FromNameAndExtension("BrokenSm.esp"));
        var first = mod.StoryManagerBranchNodes.AddNew();
        var second = mod.StoryManagerBranchNodes.AddNew();
        first.EditorID = "DuplicateNode";
        second.EditorID = "duplicatenode";
        first.Parent.SetTo(second);
        second.Parent.SetTo(first);
        first.PreviousSibling.SetTo(second);
        second.PreviousSibling.SetTo(first);

        var orphan = mod.StoryManagerQuestNodes.AddNew();
        orphan.Parent.SetTo(new FormKey(mod.ModKey, 0x00FF00));
        var wrongParent = mod.Quests.AddNew();
        var wrongType = mod.StoryManagerQuestNodes.AddNew();
        wrongType.Parent.SetTo(wrongParent.FormKey);

        var root = new FormKey(ModKey.FromNameAndExtension("Skyrim.esm"), 0x013010);
        var head = mod.StoryManagerBranchNodes.AddNew();
        var anotherHead = mod.StoryManagerBranchNodes.AddNew();
        var forkOne = mod.StoryManagerBranchNodes.AddNew();
        var forkTwo = mod.StoryManagerQuestNodes.AddNew();
        head.Parent.SetTo(root);
        anotherHead.Parent.SetTo(root);
        forkOne.Parent.SetTo(root);
        forkTwo.Parent.SetTo(root);
        forkOne.PreviousSibling.SetTo(head);
        forkTwo.PreviousSibling.SetTo(head);

        var codes = StoryManagerDiagnostics.Analyze(mod).Select(x => x.Code).ToHashSet();

        Assert.Contains("duplicate-editor-id", codes);
        Assert.Contains("parent-cycle", codes);
        Assert.Contains("sibling-cycle", codes);
        Assert.Contains("orphan-parent", codes);
        Assert.Contains("invalid-parent-type", codes);
        Assert.Contains("sibling-head-count", codes);
        Assert.Contains("duplicate-sibling-link", codes);
    }

    [Fact]
    public void Reports_duplicate_quest_routes_alias_ids_and_local_lvln_alias_targets()
    {
        var mod = Demo.CreateDemoPlugin(ModKey.FromNameAndExtension("AliasSm.esp"));
        var root = new FormKey(ModKey.FromNameAndExtension("Skyrim.esm"), 0x013010);
        var branch = mod.StoryManagerBranchNodes.AddNew();
        branch.Parent.SetTo(root);
        var quest = mod.Quests.AddNew();
        quest.EditorID = "RoutedQuest";
        var leveledNpc = mod.LeveledNpcs.AddNew();
        leveledNpc.EditorID = "BadAliasTarget";
        var forced = new QuestAlias { ID = 7, Name = "ForcedLvln" };
        forced.ForcedReference.SetTo(leveledNpc.FormKey);
        quest.Aliases.Add(forced);
        var unique = new QuestAlias { ID = 7, Name = "UniqueLvln" };
        unique.UniqueActor.SetTo(leveledNpc.FormKey);
        quest.Aliases.Add(unique);
        var location = new QuestAlias { ID = 8, Name = "SpawnLocation", Type = QuestAlias.TypeEnum.Location };
        quest.Aliases.Add(location);
        var create = new QuestAlias
        {
            ID = 9,
            Name = "SpawnedLvln",
            CreateReferenceToObject = new CreateReferenceToObject
            {
                AliasID = 8,
                Create = CreateReferenceToObject.CreateEnum.At,
                Level = Level.Easy,
            },
        };
        create.CreateReferenceToObject.Object.SetTo(leveledNpc.FormKey);
        quest.Aliases.Add(create);

        var firstNode = mod.StoryManagerQuestNodes.AddNew();
        firstNode.Parent.SetTo(branch);
        firstNode.Quests.Add(new StoryManagerQuest { Quest = { FormKey = quest.FormKey } });
        firstNode.Quests.Add(new StoryManagerQuest { Quest = { FormKey = quest.FormKey } });
        var secondNode = mod.StoryManagerQuestNodes.AddNew();
        secondNode.Parent.SetTo(branch);
        secondNode.PreviousSibling.SetTo(firstNode);
        secondNode.Quests.Add(new StoryManagerQuest { Quest = { FormKey = quest.FormKey } });

        var codes = StoryManagerDiagnostics.Analyze(mod).Select(x => x.Code).ToHashSet();

        Assert.Contains("duplicate-quest-entry", codes);
        Assert.Contains("duplicate-quest-route", codes);
        Assert.Contains("duplicate-alias-id", codes);
        Assert.Contains("lvln-forced-reference", codes);
        Assert.Contains("lvln-unique-actor", codes);
        Assert.Contains("lvln-create-target-type", codes);
    }

    [Fact]
    public void External_parents_and_quest_references_are_not_guessed_at()
    {
        var mod = Demo.CreateDemoPlugin(ModKey.FromNameAndExtension("ExternalSm.esp"));
        var node = mod.StoryManagerQuestNodes.AddNew();
        node.Parent.SetTo(new FormKey(ModKey.FromNameAndExtension("Skyrim.esm"), 0x013010));
        node.PreviousSibling.SetTo(
            new FormKey(ModKey.FromNameAndExtension("SomeMaster.esm"), 0x005678));
        node.Quests.Add(new StoryManagerQuest
        {
            Quest = { FormKey = new FormKey(ModKey.FromNameAndExtension("SomeMaster.esm"), 0x001234) },
        });

        Assert.Empty(StoryManagerDiagnostics.Analyze(mod));
    }

    [Fact]
    public void Empty_quest_link_is_reported()
    {
        var mod = Demo.CreateDemoPlugin(ModKey.FromNameAndExtension("EmptyQuestLink.esp"));
        var node = mod.StoryManagerQuestNodes.AddNew();
        node.Parent.SetTo(new FormKey(ModKey.FromNameAndExtension("Skyrim.esm"), 0x013010));
        node.Quests.Add(new StoryManagerQuest());

        var issue = Assert.Single(StoryManagerDiagnostics.Analyze(mod));
        Assert.Equal("missing-quest-link", issue.Code);
    }

    [Fact]
    public void Next_alias_id_must_be_above_local_alias_ids()
    {
        var mod = Demo.CreateDemoPlugin(ModKey.FromNameAndExtension("AliasCounter.esp"));
        var branch = mod.StoryManagerBranchNodes.AddNew();
        branch.Parent.SetTo(new FormKey(ModKey.FromNameAndExtension("Skyrim.esm"), 0x013010));
        var quest = mod.Quests.AddNew();
        quest.Aliases.Add(new QuestAlias { ID = 4, Name = "ReservedGap" });
        quest.NextAliasID = 4;
        var node = mod.StoryManagerQuestNodes.AddNew();
        node.Parent.SetTo(branch);
        node.Quests.Add(new StoryManagerQuest { Quest = { FormKey = quest.FormKey } });

        var issue = Assert.Single(StoryManagerDiagnostics.Analyze(mod));
        Assert.Equal("invalid-next-alias-id", issue.Code);
    }
}
