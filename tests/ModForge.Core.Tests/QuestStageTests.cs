using ModForge;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Tests;

// Master-free regression tests for quest stages + log entries + objective↔stage wiring +
// dialogue-set-stage + the validate guardrails. These build entirely in memory (no Skyrim.esm,
// no placements/template-clones that would read a master), so they run anywhere `dotnet test` does.
public class QuestStageTests
{
    private static readonly ModKey Key = ModKey.FromNameAndExtension("Test.esp");

    private static IQuestGetter BuildQuest(QuestSpec q, ModSpec? extra = null)
    {
        var spec = extra ?? new ModSpec();
        spec.Quests.Add(q);
        var result = Generator.Build(spec, Key);
        return result.Mod.Quests.First(x => x.EditorID == q.EditorId);
    }

    [Fact]
    public void Stages_emitted_with_indices_and_log_text_in_order()
    {
        var q = new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages =
            {
                new StageSpec { Index = 10, LogEntry = "start" },
                new StageSpec { Index = 20, LogEntry = "mid" },
                new StageSpec { Index = 30, LogEntry = "done" },
            },
        };
        var quest = BuildQuest(q);

        Assert.Equal(new ushort[] { 10, 20, 30 }, quest.Stages.Select(s => s.Index).ToArray());
        Assert.Equal("start", quest.Stages.First(s => s.Index == 10).LogEntries.Single().Entry?.String);
        Assert.Equal("done", quest.Stages.First(s => s.Index == 30).LogEntries.Single().Entry?.String);
    }

    [Fact]
    public void Journal_quest_defaults_to_a_visible_type_so_it_shows_in_the_journal()
    {
        // type=None (Mutagen default) = a background quest the player never sees: log entries don't
        // surface, setstage shows nothing. A quest with journal content must default to a visible type.
        var withLog = BuildQuest(new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages = { new StageSpec { Index = 10, LogEntry = "start" } },
        });
        Assert.Equal(Quest.TypeEnum.SideQuest, withLog.Type);

        var withObjective = BuildQuest(new QuestSpec
        {
            EditorId = "MF_Q2", Name = "Q2",
            Objectives = { new ObjectiveSpec { Index = 10, Text = "do it" } },
        });
        Assert.Equal(Quest.TypeEnum.SideQuest, withObjective.Type);
    }

    [Fact]
    public void Journal_quest_has_DisplayedInHUD_flag_so_it_appears_in_the_journal()
    {
        // REGRESSION (It.36): 0x0010 = "Displayed In HUD" (DNAM bit 4). Without it a quest is running
        // but NEVER appears in the player's journal (not even the title), even with type=SideQuest and
        // valid log entries. Vanilla and third-party mods carry DNAM byte0=0x11; ours was 0x01.
        const Quest.Flag DisplayedInHud = (Quest.Flag)0x0010;
        var q = BuildQuest(new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages = { new StageSpec { Index = 10, LogEntry = "start" } },
        });
        Assert.True(q.Flags.HasFlag(DisplayedInHud));
    }

    [Fact]
    public void QuestFormVersion_is_pinned_to_vanilla_zero_not_the_mutagen_0xFF_default()
    {
        // Mutagen defaults QuestFormVersion to 255 (0xFF) — an unset sentinel no vanilla quest uses;
        // a 0xFF form version stops the engine registering the quest in the JOURNAL (stage flags still
        // fire, so it masquerades as "completeQuest works but the log never shows").
        var q = BuildQuest(new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages = { new StageSpec { Index = 10, LogEntry = "start" } },
        });
        Assert.Equal(0, q.QuestFormVersion);
    }

    [Fact]
    public void Controller_quest_with_no_journal_content_stays_type_None()
    {
        // A dialogue-only / silent-milestone quest must NOT clutter the journal: no objectives and no
        // stage log text -> stays None.
        var q = BuildQuest(new QuestSpec
        {
            EditorId = "MF_Ctrl", Name = "Ctrl",
            Stages = { new StageSpec { Index = 10 } },   // silent stage, no log entry
        });
        Assert.Equal(Quest.TypeEnum.None, q.Type);
    }

    [Fact]
    public void Explicit_quest_type_overrides_the_smart_default()
    {
        var q = BuildQuest(new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q", Type = "Misc",
            Stages = { new StageSpec { Index = 10, LogEntry = "start" } },
        });
        Assert.Equal(Quest.TypeEnum.Misc, q.Type);
    }

    [Fact]
    public void Every_log_entry_has_non_null_flags_so_the_QSDT_marker_is_written()
    {
        // REGRESSION (It.36 CTD): a QuestLogEntry with Flags=null makes Mutagen OMIT the QSDT subrecord,
        // leaving an orphan CNAM the SSE engine mis-parses -> the journal UI access-violates. Vanilla
        // always writes QSDT (flags 0) before a log entry's CNAM, so a plain log entry must carry 0,
        // not null.
        var q = BuildQuest(new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages =
            {
                new StageSpec { Index = 10, LogEntry = "plain" },          // no complete/fail
                new StageSpec { Index = 20, LogEntry = "end", CompleteQuest = true },
            },
        });
        foreach (var le in q.Stages.SelectMany(s => s.LogEntries))
            Assert.NotNull(le.Flags);   // non-null => QSDT emitted
        Assert.Equal((QuestLogEntry.Flag)0, q.Stages.First(s => s.Index == 10).LogEntries.Single().Flags);
    }

    [Fact]
    public void CompleteQuest_flag_set_on_the_log_entry()
    {
        var q = new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages =
            {
                new StageSpec { Index = 10, LogEntry = "go" },
                new StageSpec { Index = 20, LogEntry = "end", CompleteQuest = true },
            },
        };
        var quest = BuildQuest(q);

        var endLog = quest.Stages.First(s => s.Index == 20).LogEntries.Single();
        Assert.True(endLog.Flags!.Value.HasFlag(QuestLogEntry.Flag.CompleteQuest));
        // A non-flagged stage's entry stays unflagged (CompleteQuest must not bleed across stages).
        var goLog = quest.Stages.First(s => s.Index == 10).LogEntries.Single();
        Assert.False((goLog.Flags ?? 0).HasFlag(QuestLogEntry.Flag.CompleteQuest));
    }

    [Fact]
    public void FailQuest_flag_set_on_the_log_entry()
    {
        var q = new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages = { new StageSpec { Index = 99, LogEntry = "failed", FailQuest = true } },
        };
        var quest = BuildQuest(q);
        var le = quest.Stages.Single().LogEntries.Single();
        Assert.True(le.Flags!.Value.HasFlag(QuestLogEntry.Flag.FailQuest));
    }

    [Fact]
    public void Silent_stage_with_no_text_or_flag_emits_no_log_entry()
    {
        var q = new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages = { new StageSpec { Index = 5 } },   // no text, no flags
        };
        var quest = BuildQuest(q);
        Assert.Single(quest.Stages);
        Assert.Empty(quest.Stages.Single().LogEntries);
    }

    [Fact]
    public void Stage_log_entry_condition_is_built_as_a_CTDA()
    {
        var q = new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages =
            {
                new StageSpec
                {
                    Index = 10, LogEntry = "gated",
                    Conditions = { new ConditionSpec { Function = "GetStage", Comparison = "GreaterThanOrEqualTo", Value = 10, Param = "MF_Q" } },
                },
            },
        };
        var quest = BuildQuest(q);
        var le = quest.Stages.Single().LogEntries.Single();
        var cond = Assert.Single(le.Conditions);
        var cf = Assert.IsType<ConditionFloat>(cond);
        Assert.Equal(CompareOperator.GreaterThanOrEqualTo, cf.CompareOperator);
        Assert.Equal(10f, cf.ComparisonValue);
        // GetStage's data carries the quest form param resolved from targetRef.
        var data = Assert.IsType<GetStageConditionData>(cf.Data);
        Assert.False(data.Quest.Link.FormKey.IsNull);
    }

    [Fact]
    public void Objective_stage_wiring_generates_fragment_script_with_display_and_complete()
    {
        var q = new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages = { new StageSpec { Index = 10 }, new StageSpec { Index = 20 }, new StageSpec { Index = 30 } },
            Objectives =
            {
                new ObjectiveSpec { Index = 10, Text = "First", ShowStage = 10, CompleteStage = 20 },
                new ObjectiveSpec { Index = 20, Text = "Second", ShowStage = 20, CompleteStage = 30 },
            },
        };

        Assert.True(Generator.QuestNeedsFragmentScript(q));
        var src = Generator.GenerateQuestFragmentSource(q);
        Assert.Contains("Function ApplyStage_10()", src);
        Assert.Contains("SetObjectiveDisplayed(10)", src);
        Assert.Contains("Function ApplyStage_20()", src);
        Assert.Contains("SetObjectiveDisplayed(20)", src);
        Assert.Contains("SetObjectiveCompleted(10)", src);
        Assert.Contains("Function ApplyStage_30()", src);
        Assert.Contains("SetObjectiveCompleted(20)", src);

        // The QUST record carries the fragment script via VMAD so the CK can bind stage fragments.
        var quest = BuildQuest(q);
        var scriptName = Generator.QuestFragmentScriptName(q);
        Assert.Equal("MF_Q_Stages", scriptName);
        Assert.Contains(quest.VirtualMachineAdapter!.Scripts, s => s.Name == scriptName);
    }

    [Fact]
    public void Quest_without_stage_linked_objectives_gets_no_fragment_script()
    {
        var q = new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Objectives = { new ObjectiveSpec { Index = 10, Text = "static" } },   // no showStage/completeStage
        };
        Assert.False(Generator.QuestNeedsFragmentScript(q));
        Assert.Equal("", Generator.QuestFragmentScriptName(q));
        var quest = BuildQuest(q);
        Assert.Null(quest.VirtualMachineAdapter);
    }

    [Fact]
    public void Dialogue_set_stage_generates_a_TIF_fragment()
    {
        var d = new DialogueSpec
        {
            EditorId = "MF_Agree", QuestEditorId = "MF_Q",
            Prompt = "I'll help", Responses = { "Good." }, SetStage = 20,
        };
        var src = Generator.GenerateDialogueFragmentSource(d);
        Assert.Equal("TIF_MF_Agree", Generator.DialogueFragmentScriptName(d));
        Assert.Contains("extends TopicInfo", src);
        Assert.Contains("GetOwningQuest().SetStage(20)", src);

        // No setStage -> no fragment.
        var plain = new DialogueSpec { EditorId = "MF_Plain", QuestEditorId = "MF_Q", Prompt = "Hi", Responses = { "Hey" } };
        Assert.Equal("", Generator.GenerateDialogueFragmentSource(plain));
    }

    // ---- validate guardrails ----

    private static ModSpec OneQuestSpec(QuestSpec q)
    {
        var s = new ModSpec();
        s.Quests.Add(q);
        return s;
    }

    [Fact]
    public void Validate_flags_duplicate_stage_index()
    {
        var s = OneQuestSpec(new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages = { new StageSpec { Index = 10 }, new StageSpec { Index = 10 } },
        });
        Assert.Contains(Generator.Validate(s), p => p.Contains("duplicate stage index 10"));
    }

    [Fact]
    public void Validate_flags_non_ascending_stage_index()
    {
        var s = OneQuestSpec(new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages = { new StageSpec { Index = 20 }, new StageSpec { Index = 10 } },
        });
        Assert.Contains(Generator.Validate(s), p => p.Contains("not ascending"));
    }

    [Fact]
    public void Validate_flags_objective_referencing_missing_stage()
    {
        var s = OneQuestSpec(new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages = { new StageSpec { Index = 10 } },
            Objectives = { new ObjectiveSpec { Index = 10, Text = "x", ShowStage = 99 } },
        });
        Assert.Contains(Generator.Validate(s), p => p.Contains("showStage 99 has no matching stage"));
    }

    [Fact]
    public void Validate_flags_dialogue_setstage_with_no_matching_stage()
    {
        var s = OneQuestSpec(new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages = { new StageSpec { Index = 10 } },
        });
        s.Dialogue.Add(new DialogueSpec
        {
            EditorId = "MF_D", QuestEditorId = "MF_Q", Prompt = "p", Responses = { "r" }, SetStage = 50,
        });
        Assert.Contains(Generator.Validate(s), p => p.Contains("setStage 50 has no matching stage"));
    }

    [Fact]
    public void Validate_flags_invalid_condition_function()
    {
        var s = OneQuestSpec(new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages = { new StageSpec { Index = 10, LogEntry = "x", Conditions = { new ConditionSpec { Function = "NotARealFunction" } } } },
        });
        Assert.Contains(Generator.Validate(s), p => p.Contains("invalid function 'NotARealFunction'"));
    }

    [Fact]
    public void Validate_flags_stage_with_both_complete_and_fail()
    {
        var s = OneQuestSpec(new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages = { new StageSpec { Index = 10, LogEntry = "x", CompleteQuest = true, FailQuest = true } },
        });
        Assert.Contains(Generator.Validate(s), p => p.Contains("both completeQuest and failQuest"));
    }

    [Fact]
    public void Validate_clean_for_a_well_formed_multistage_quest()
    {
        var s = OneQuestSpec(new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages =
            {
                new StageSpec { Index = 10, LogEntry = "a" },
                new StageSpec { Index = 20, LogEntry = "b" },
                new StageSpec { Index = 30, LogEntry = "c", CompleteQuest = true },
            },
            Objectives =
            {
                new ObjectiveSpec { Index = 10, Text = "x", ShowStage = 10, CompleteStage = 20 },
                new ObjectiveSpec { Index = 20, Text = "y", ShowStage = 20, CompleteStage = 30 },
            },
        });
        Assert.Empty(Generator.Validate(s));
    }
}
