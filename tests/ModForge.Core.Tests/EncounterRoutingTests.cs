using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// A組 #5 locationFilter (LocType routing → GetKeywordDataForCurrentLocation, OR'd) + #6 cooldownHours
// (LastFired GLOB + MFEncounterCooldown quest script). Condition functions reflection-verified.
public class EncounterRoutingTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static ModSpec EncounterSpec(float cooldown = 0f) => new()
    {
        Keywords = { new KeywordSpec { EditorId = "LocTypeBanditCamp" }, new KeywordSpec { EditorId = "LocTypeDungeon" } },
        Quests =
        {
            new QuestSpec
            {
                EditorId = "MFEnc", Name = "Enc",
                // SM-driven: the cooldown gate (and any spawn) fires from the OnStory<Event> handler, NOT
                // the startUpStage fragment (an SM-started quest does not run the startUpStage fragment).
                Stages = { new StageSpec { Index = 10, StartUpStage = true } },
                StoryEvent = new QuestStoryEventSpec
                {
                    Event = "ChangeLocation",
                    LocationFilter = { "LocTypeBanditCamp", "LocTypeDungeon" },
                    CooldownHours = cooldown,
                },
                Aliases = { new QuestAliasSpec { Name = "Loc", Fill = "fromEvent:newLocation" } },
            },
        },
    };

    private static IQuestGetter Quest(ModSpec s) =>
        Generator.Build(s, Mutagen.Bethesda.Plugins.ModKey.FromNameAndExtension("Test.esp")).Mod
            .Quests.Single(q => q.EditorID == "MFEnc");

    [Fact]
    public void LocationFilter_EmitsCurrentLocationKeywordConditions()
    {
        var q = Quest(EncounterSpec());
        var conds = q.EventConditions
            .Where(c => c.Data is IGetKeywordDataForCurrentLocationConditionDataGetter).ToList();
        Assert.Equal(2, conds.Count);                          // one per LocType keyword
        foreach (var c in conds)
            Assert.Equal(1f, ((IConditionFloatGetter)c).ComparisonValue);
    }

    [Fact]
    public void LocationFilter_OrsAllButLast()
    {
        var q = Quest(EncounterSpec());
        var conds = q.EventConditions
            .Where(c => c.Data is IGetKeywordDataForCurrentLocationConditionDataGetter).ToList();
        // First condition carries OR-with-next; the last closes the group (no OR).
        Assert.True(conds[0].Flags.HasFlag(Condition.Flag.OR));
        Assert.False(conds[^1].Flags.HasFlag(Condition.Flag.OR));
    }

    [Fact]
    public void CooldownHours_CreatesLastFiredGlobalAndAttachesScript()
    {
        var spec = EncounterSpec(cooldown: 12f);
        var mod = Generator.Build(spec, Mutagen.Bethesda.Plugins.ModKey.FromNameAndExtension("Test.esp")).Mod;
        var g = mod.Globals.Single(x => x.EditorID == "MFEnc_LastFired");
        Assert.IsAssignableFrom<IGlobalFloatGetter>(g);
        var q = mod.Quests.Single(x => x.EditorID == "MFEnc");
        var qa = (QuestAdapter)q.VirtualMachineAdapter!;
        var entry = qa.Scripts.Single(s => s.Name == "MFEncounterCooldown");
        Assert.Equal(12f, ((IScriptFloatPropertyGetter)entry.Properties.Single(p => p.Name == "CooldownHours")).Data);
        var lf = (IScriptObjectPropertyGetter)entry.Properties.Single(p => p.Name == "LastFired");
        Assert.Equal(g.FormKey, lf.Object.FormKey);
    }

    [Fact]
    public void NoCooldown_NoGlobalNoScript()
    {
        var mod = Generator.Build(EncounterSpec(), Mutagen.Bethesda.Plugins.ModKey.FromNameAndExtension("Test.esp")).Mod;
        Assert.DoesNotContain(mod.Globals, g => g.EditorID == "MFEnc_LastFired");
    }

    // ── SM-driven trigger lives on OnStory<Event>, NOT the startUpStage fragment ──────────────────────
    // In-game 2026-06-19: an SM-started quest fires OnInit + OnStory<Event> but never runs the
    // startUpStage Papyrus fragment, so a spawn/cooldown hung on the startUpStage never triggered.

    [Fact]
    public void StoryEncounter_StartupStageTrigger_IsNull()
    {
        // A storyEvent quest routes its trigger through OnStory<Event>, so the startUpStage fragment is
        // NOT the trigger site (StartupStageTrigger is only for non-storyEvent StartGameEnabled spawns).
        var q = EncounterSpec(cooldown: 12f).Quests[0];
        q.Spawn = new SpawnSpec { Form = "Skyrim.esm:0x0003DECD", Count = 3, MinDistance = 200, MaxDistance = 500 };
        Assert.Null(Generator.StartupStageTrigger(q));
        Assert.True(Generator.StoryTrigger(q));
    }

    [Fact]
    public void StoryEncounter_EmitsOnStoryHandler_WithCooldownSpawnAndStop()
    {
        var q = EncounterSpec(cooldown: 12f).Quests[0];
        q.Spawn = new SpawnSpec { Form = "Skyrim.esm:0x0003DECD", Count = 3, MinDistance = 200, MaxDistance = 500 };
        var src = Generator.GenerateQuestFragmentSource(q);
        // The ChangeLocation story handler carries the whole trigger; no Fragment_Stage drives the spawn.
        Assert.Contains("Event OnStoryChangeLocation(ObjectReference akActor, Location akOldLocation, Location akNewLocation)", src);
        var handler = src.Split("Event OnStoryChangeLocation")[1].Split("EndEvent")[0];
        Assert.Contains("self as MFEncounterCooldown", handler);
        Assert.Contains("__cd.TryFire()", handler);
        Assert.Contains("__spawn.SpawnNow()", handler);
        Assert.Contains("Stop()", handler);                       // re-arm so the SM relaunches it next time
        Assert.DoesNotContain("Function Fragment_Stage_0010_Item00000()", src);
    }

    [Fact]
    public void StoryEncounter_CooldownOnly_EmitsOnStoryHandler()
    {
        // Cooldown but no spawn (a rate-limited story quest with no dynamic actors) still hooks OnStory.
        var src = Generator.GenerateQuestFragmentSource(EncounterSpec(cooldown: 12f).Quests[0]);
        Assert.Contains("Event OnStoryChangeLocation", src);
        Assert.Contains("__cd.TryFire()", src);
    }

    [Fact]
    public void LocAliasHasKeyword_Condition_BindsAliasIndexAndKeyword()
    {
        // Hold detection: a condition on a stage testing whether a location alias holds a hold keyword.
        var spec = new ModSpec
        {
            Keywords = { new KeywordSpec { EditorId = "LocTypeHold" } },
            Quests =
            {
                new QuestSpec
                {
                    EditorId = "Q", Name = "Q", StartGameEnabled = true,
                    Aliases = { new QuestAliasSpec { Name = "TheHold", Fill = "findMatchingLocation:LocTypeHold" } },
                    Stages =
                    {
                        new StageSpec { Index = 10, LogEntry = "In the hold.", Conditions =
                        {
                            new ConditionSpec { Function = "LocAliasHasKeyword", Alias = "TheHold", Param = "LocTypeHold", Comparison = "==", Value = 1 },
                        } },
                    },
                },
            },
        };
        var q = Generator.Build(spec, Mutagen.Bethesda.Plugins.ModKey.FromNameAndExtension("Test.esp")).Mod
            .Quests.Single(x => x.EditorID == "Q");
        var le = q.Stages.Single(s => s.Index == 10).LogEntries.First();
        var data = Assert.IsAssignableFrom<ILocAliasHasKeywordConditionDataGetter>(le.Conditions.Single().Data);
        Assert.Equal(0, data.LocationAliasIndex);              // TheHold = alias index 0
    }

    [Fact]
    public void EncounterSpec_Validates_Clean()
    {
        Assert.DoesNotContain(Validate(EncounterSpec(12f)), p => p.Contains("locationFilter") || p.Contains("cooldownHours"));
    }

    [Fact]
    public void Validate_NegativeCooldown_Reported()
    {
        Assert.Contains(Validate(EncounterSpec(-1f)), p => p.Contains("cooldownHours") && p.Contains(">= 0"));
    }

    [Fact]
    public void Validate_EmptyLocationFilterKeyword_Reported()
    {
        var spec = new ModSpec
        {
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q",
                StoryEvent = new QuestStoryEventSpec { Event = "ChangeLocation", LocationFilter = { "" },
                    Conditions = { } },
                Aliases = { new QuestAliasSpec { Name = "L", Fill = "fromEvent:newLocation" } } } },
        };
        Assert.Contains(Validate(spec), p => p.Contains("locationFilter has an empty keyword"));
    }
}
