using System.Linq;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class ValidateMcmTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static ModSpec With(McmControlSpec c) => new()
    {
        McmConfigs = { new McmSpec { ModName = "M", Pages = { new McmPageSpec { Name = "P", Content = { c } } } } },
    };

    [Fact]
    public void ValidSpec_NoMcmProblems()
    {
        var s = With(new McmControlSpec { Type = "toggle", Id = "bEnable:General", SourceType = "ModSettingBool" });
        Assert.DoesNotContain(Validate(s), p => p.Contains("mcm"));
    }

    [Fact]
    public void EmptyModName_Reported()
    {
        var s = new ModSpec { McmConfigs = { new McmSpec { Pages = { new McmPageSpec { Name = "P" } } } } };
        Assert.Contains(Validate(s), p => p.Contains("empty 'modName'"));
    }

    [Fact]
    public void NoPages_Reported()
    {
        var s = new ModSpec { McmConfigs = { new McmSpec { ModName = "M" } } };
        Assert.Contains(Validate(s), p => p.Contains("has no pages"));
    }

    [Fact]
    public void UnknownControlType_Reported()
    {
        Assert.Contains(Validate(With(new McmControlSpec { Type = "banana" })),
            p => p.Contains("unknown type 'banana'"));
    }

    [Fact]
    public void PropertyValueSourceType_RejectedAsOutOfScope()
    {
        var s = With(new McmControlSpec { Type = "toggle", Id = "a:S", SourceType = "PropertyValueBool" });
        Assert.Contains(Validate(s), p => p.Contains("out of scope"));
    }

    [Fact]
    public void GlobalBinding_RequiresBoolToggle_AndResolvableRef()
    {
        var badShape = With(new McmControlSpec { Type = "slider", Id = "a:S",
            SourceType = "ModSettingFloat", Min = 0, Max = 1, Global = "Missing" });
        var problems = Validate(badShape);
        Assert.Contains(problems, p => p.Contains("global binding requires"));
        Assert.Contains(problems, p => p.Contains("unresolved ref 'Missing'"));

        var good = With(new McmControlSpec { Type = "toggle", Id = "a:S",
            SourceType = "ModSettingBool", Global = "MF_Gate" });
        good.Globals.Add(new GlobalSpec { EditorId = "MF_Gate", Type = "short" });
        Assert.DoesNotContain(Validate(good), p => p.Contains("global binding") || p.Contains("MF_Gate"));

        good.Globals[0].Constant = true;
        Assert.Contains(Validate(good), p => p.Contains("constant") && p.Contains("MF_Gate"));

        good.Globals[0].Constant = false;
        good.McmConfigs[0].Pages[0].Content[0].DefaultBool = true;
        Assert.Contains(Validate(good), p => p.Contains("defaultBool") && p.Contains("initial value"));
    }

    [Fact]
    public void ValueControl_MalformedId_Reported()
    {
        // sourceType but id missing the ":Section" → can't map to an ini key.
        var s = With(new McmControlSpec { Type = "toggle", Id = "bEnable", SourceType = "ModSettingBool" });
        Assert.Contains(Validate(s), p => p.Contains("malformed id"));
    }

    [Fact]
    public void Slider_WithoutRange_Reported()
    {
        var s = With(new McmControlSpec { Type = "slider", Id = "f:S", SourceType = "ModSettingFloat", Min = 0 });
        Assert.Contains(Validate(s), p => p.Contains("needs both min and max"));
    }

    [Fact]
    public void Enum_WithoutOptions_Reported()
    {
        var s = With(new McmControlSpec { Type = "enum", Id = "i:S", SourceType = "ModSettingInt" });
        Assert.Contains(Validate(s), p => p.Contains("needs an options list"));
    }

    [Fact]
    public void BadCursorFillMode_Reported()
    {
        var s = new ModSpec { McmConfigs = { new McmSpec { ModName = "M",
            Pages = { new McmPageSpec { Name = "P", CursorFillMode = "diagonal" } } } } };
        Assert.Contains(Validate(s), p => p.Contains("cursorFillMode 'diagonal' invalid"));
    }
}
