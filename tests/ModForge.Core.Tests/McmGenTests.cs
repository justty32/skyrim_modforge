using System.Linq;
using System.Text.Json;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// MCM Helper config.json + settings.ini generation. Format verified against
// sub_projs/mod-survey/findings/mcm-helper-config-json.md (MCM Helper 1.6.1).
public class McmGenTests
{
    private static McmSpec Sample() => new()
    {
        ModName = "MyMod",
        DisplayName = "$MyMod_Title",
        Pages =
        {
            new McmPageSpec
            {
                Name = "$General", CursorFillMode = "leftToRight",
                Content =
                {
                    new McmControlSpec { Type = "header", Text = "$Features" },
                    new McmControlSpec { Type = "toggle", Text = "Enable", Id = "bEnable:General",
                        SourceType = "ModSettingBool", DefaultBool = true },
                    new McmControlSpec { Type = "slider", Text = "Scale", Id = "fScale:General",
                        SourceType = "ModSettingFloat", Min = 0.5, Max = 3.0, Step = 0.1, DefaultNumber = 1.0,
                        FormatString = "{1}" },
                    new McmControlSpec { Type = "enum", Text = "Size", Id = "iSize:General",
                        SourceType = "ModSettingInt", Options = { "$Small", "$Large" }, ShortNames = { "S", "L" },
                        DefaultNumber = 1 },
                },
            },
        },
    };

    [Fact]
    public void Emits_TwoFiles_UnderMcmConfigDir()
    {
        var files = McmGen.Generate(Sample(), "MyMod");
        Assert.Equal(2, files.Count);
        Assert.Contains(files, f => f.RelPath == "MCM/Config/MyMod/config.json");
        Assert.Contains(files, f => f.RelPath == "MCM/Config/MyMod/settings.ini");
    }

    // MCM Helper keys the config folder on the host plugin's filename stem (FormUtil::GetModName =
    // path(plugin).stem()), NOT the spec's modName — so `identity` drives both the folder and the
    // config.json `modName` field; the spec's modName only feeds the displayName fallback. A mismatch
    // here makes MCM Helper read the wrong folder ("check json syntax" in-game, 2026-06-20).
    [Fact]
    public void Identity_DrivesFolderAndModNameField_NotSpecModName()
    {
        var files = McmGen.Generate(Sample(), "MyHostPlugin");
        Assert.Contains(files, f => f.RelPath == "MCM/Config/MyHostPlugin/config.json");
        Assert.DoesNotContain(files, f => f.RelPath.Contains("/MyMod/"));
        using var doc = JsonDocument.Parse(McmGen.BuildConfigJson(Sample(), "MyHostPlugin"));
        Assert.Equal("MyHostPlugin", doc.RootElement.GetProperty("modName").GetString());
        // displayName still falls back to the spec's modName/displayName for the menu label.
        Assert.Equal("$MyMod_Title", doc.RootElement.GetProperty("displayName").GetString());
    }

    [Fact]
    public void ConfigJson_HasTopLevelAndPageShape()
    {
        using var doc = JsonDocument.Parse(McmGen.BuildConfigJson(Sample(), "MyMod"));
        var root = doc.RootElement;
        Assert.Equal("MyMod", root.GetProperty("modName").GetString());
        Assert.Equal("$MyMod_Title", root.GetProperty("displayName").GetString());
        var page = root.GetProperty("pages")[0];
        Assert.Equal("$General", page.GetProperty("pageDisplayName").GetString());
        Assert.Equal("leftToRight", page.GetProperty("cursorFillMode").GetString());
        Assert.Equal(4, page.GetProperty("content").GetArrayLength());
    }

    [Fact]
    public void Header_HasNoValueOptionsOrId()
    {
        using var doc = JsonDocument.Parse(McmGen.BuildConfigJson(Sample(), "MyMod"));
        var header = doc.RootElement.GetProperty("pages")[0].GetProperty("content")[0];
        Assert.Equal("header", header.GetProperty("type").GetString());
        Assert.False(header.TryGetProperty("id", out _));
        Assert.False(header.TryGetProperty("valueOptions", out _));
    }

    [Fact]
    public void Toggle_DefaultValue_IsTypedBool()
    {
        using var doc = JsonDocument.Parse(McmGen.BuildConfigJson(Sample(), "MyMod"));
        var vo = doc.RootElement.GetProperty("pages")[0].GetProperty("content")[1].GetProperty("valueOptions");
        Assert.Equal("ModSettingBool", vo.GetProperty("sourceType").GetString());
        Assert.Equal(JsonValueKind.True, vo.GetProperty("defaultValue").ValueKind);
    }

    [Fact]
    public void Slider_CarriesRangeAndFloatDefault()
    {
        using var doc = JsonDocument.Parse(McmGen.BuildConfigJson(Sample(), "MyMod"));
        var vo = doc.RootElement.GetProperty("pages")[0].GetProperty("content")[2].GetProperty("valueOptions");
        Assert.Equal(0.5, vo.GetProperty("min").GetDouble());
        Assert.Equal(3.0, vo.GetProperty("max").GetDouble());
        Assert.Equal(0.1, vo.GetProperty("step").GetDouble());
        Assert.Equal(1.0, vo.GetProperty("defaultValue").GetDouble());
    }

    [Fact]
    public void Enum_DefaultValue_IsInt_NotFloat()
    {
        using var doc = JsonDocument.Parse(McmGen.BuildConfigJson(Sample(), "MyMod"));
        var vo = doc.RootElement.GetProperty("pages")[0].GetProperty("content")[3].GetProperty("valueOptions");
        // ModSettingInt default must serialize as an integer (1), not 1.0 — MCM reads it as an index.
        Assert.Equal(1, vo.GetProperty("defaultValue").GetInt32());
        Assert.Equal(2, vo.GetProperty("options").GetArrayLength());
        Assert.Equal("L", vo.GetProperty("shortNames")[1].GetString());
    }

    [Fact]
    public void SettingsIni_GroupsBySection_WithTypedDefaults()
    {
        var ini = McmGen.BuildSettingsIni(Sample());
        Assert.Contains("[General]", ini);
        Assert.Contains("bEnable=1", ini);     // bool → 1
        Assert.Contains("fScale=1.0", ini);    // float keeps a decimal
        Assert.Contains("iSize=1", ini);       // int plain
        // header carries no value → no ini line
        Assert.DoesNotContain("header", ini);
    }

    [Fact]
    public void GroupConditionNot_EmitsNotObject()
    {
        var m = new McmSpec
        {
            ModName = "M",
            Pages = { new McmPageSpec { Name = "P", Content =
            {
                new McmControlSpec { Type = "toggle", Id = "a:S", SourceType = "ModSettingBool", GroupControl = 1 },
                new McmControlSpec { Type = "toggle", Id = "b:S", SourceType = "ModSettingBool",
                    GroupCondition = 1, GroupConditionNot = true, GroupBehavior = "disable" },
            } } },
        };
        using var doc = JsonDocument.Parse(McmGen.BuildConfigJson(m, "MyMod"));
        var content = doc.RootElement.GetProperty("pages")[0].GetProperty("content");
        Assert.Equal(1, content[0].GetProperty("groupControl").GetInt32());   // first is the group toggle
        var second = content[1];
        Assert.Equal(1, second.GetProperty("groupCondition").GetProperty("NOT").GetInt32());  // {"NOT": 1}
        Assert.Equal("disable", second.GetProperty("groupBehavior").GetString());
    }
}
