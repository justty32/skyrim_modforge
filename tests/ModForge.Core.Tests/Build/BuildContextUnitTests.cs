using Mutagen.Bethesda.Plugins;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// The first tests in this repo that drive a build STEP instead of the whole pipeline.
//
// Until BuildContext became `internal` (it was a `private` nested class), the only way into the
// generator from a test was Generator.Build(), which runs ~150 ordered steps — so 965 of the
// suite's 1107 test methods are end-to-end by necessity, not by choice. These exist to hold that
// seam open: they construct a BuildContext directly, run ONE step, and assert on just that step's
// output. A regression in Generator.Build's step ORDER cannot make them pass or fail, which is
// exactly the point — order is covered by the golden hash instead.
public class BuildContextUnitTests
{
    private static readonly ModKey Key = ModKey.FromNameAndExtension("UnitTest.esp");

    private static Generator.BuildContext Ctx(ModSpec spec) => new(spec, Key, null);

    [Fact]
    public void BuildItems_AloneEmitsItsRecords_WithoutTheRestOfThePipeline()
    {
        var spec = new ModSpec
        {
            MiscItems = { new MiscSpec { EditorId = "UT_Coin", Name = "Coin", Value = 3, Weight = 0.1f } },
            Books = { new BookSpec { EditorId = "UT_Tome", Name = "Tome" } },
            Weapons = { new WeaponSpec { EditorId = "UT_Blade", Name = "Blade" } },
        };

        var ctx = Ctx(spec);
        ctx.BuildItems();
        var mod = ctx.Finish().Mod;

        var misc = Assert.Single(mod.MiscItems);
        Assert.Equal("UT_Coin", misc.EditorID);
        Assert.Equal(3u, misc.Value);
        Assert.Equal("UT_Tome", Assert.Single(mod.Books).EditorID);
        Assert.Equal("UT_Blade", Assert.Single(mod.Weapons).EditorID);

        // Nothing else ran, so nothing else exists — that is what makes this a unit test.
        Assert.Empty(mod.Npcs);
        Assert.Empty(mod.Quests);
    }

    [Fact]
    public void BuildLightingTemplates_IsSelfContained()
    {
        var spec = new ModSpec
        {
            LightingTemplates = { new LightingTemplateSpec { EditorId = "UT_Lgtm" } },
            ImageSpaces = { new ImageSpaceSpec { EditorId = "UT_Imgs" } },
        };

        var ctx = Ctx(spec);
        ctx.BuildLightingTemplates();
        ctx.BuildImageSpaces();
        var mod = ctx.Finish().Mod;

        Assert.Equal("UT_Lgtm", Assert.Single(mod.LightingTemplates).EditorID);
        Assert.Equal("UT_Imgs", Assert.Single(mod.ImageSpaces).EditorID);
    }

    [Fact]
    public void BuildFormKeyTable_IndexesOnlyWhatHasBeenBuiltSoFar()
    {
        var spec = new ModSpec
        {
            Globals = { new GlobalSpec { EditorId = "UT_Flag", Value = 1 } },
            Messages = { new MessageSpec { EditorId = "UT_Msg", Description = "hi" } },
        };

        var ctx = Ctx(spec);
        ctx.BuildGlobals();
        ctx.BuildFormKeyTable();     // pass-2 setup: index every record that exists RIGHT NOW
        ctx.BuildMessages();         // built after the table, so deliberately not in it

        var mod = ctx.Finish().Mod;
        Assert.Equal("UT_Flag", Assert.Single(mod.Globals).EditorID);
        Assert.Equal("UT_Msg", Assert.Single(mod.Messages).EditorID);
    }

    [Fact]
    public void Finish_ReportsStatsForTheStepsThatActuallyRan()
    {
        var spec = new ModSpec
        {
            MiscItems = { new MiscSpec { EditorId = "UT_Coin" } },
            // Present in the spec but never built, because BuildNpcs is not called below.
            Npcs = { new NpcSpec { EditorId = "UT_Npc" } },
        };

        var ctx = Ctx(spec);
        ctx.BuildItems();
        var result = ctx.Finish();

        Assert.Empty(result.Mod.Npcs);
        Assert.True(result.Stats.Esl);
        Assert.Empty(result.Warnings);
    }
}
