using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// OarGen produces the loose-file tree (root config + per-submod config + hkx placements) without I/O.
public class OarGenTests
{
    private static AnimationReplacerSpec Sample() => new()
    {
        Mod = "Sofia Katana",
        Author = "ModForge",
        Description = "test moveset",
        Submods = new List<OarSubmodSpec>
        {
            new()
            {
                Name = "Attack - Sword & Shield", Priority = 100008, Replaces = "actors/character/animations/atk.hkx",
                Hkx = new List<string> { "anims/ss_atk.hkx" },
                Conditions = new List<OarConditionSpec>
                {
                    new() { Condition = "IsEquippedType", Type = 1, LeftHand = false },
                },
            },
            new()
            {
                Name = "Idle - base", Priority = 5, Hkx = new List<string> { "anims/idle.hkx" },
                // empty conditions → applies always
            },
        },
    };

    [Fact]
    public void Generate_RootConfig_HasNoPriorityOrConditions()
    {
        var files = OarGen.Generate(Sample());
        var root = files.Single(f => f.RelPath.EndsWith("OpenAnimationReplacer/Sofia Katana/config.json"));
        var o = (JsonObject)JsonNode.Parse(root.Content)!;
        Assert.Equal("Sofia Katana", (string?)o["name"]);
        Assert.Equal("ModForge", (string?)o["author"]);
        Assert.False(o.ContainsKey("priority"));
        Assert.False(o.ContainsKey("conditions"));
    }

    [Fact]
    public void Generate_Submod_HasPriorityAndConditions()
    {
        var files = OarGen.Generate(Sample());
        var sub = files.Single(f => f.RelPath.EndsWith("Sofia Katana/Attack - Sword & Shield/config.json"));
        var o = (JsonObject)JsonNode.Parse(sub.Content)!;
        Assert.Equal(100008, (int)o["priority"]!);
        var conds = (JsonArray)o["conditions"]!;
        Assert.Single(conds);
        Assert.Equal("IsEquippedType", (string?)conds[0]!["condition"]);
    }

    [Fact]
    public void Generate_EmptyConditions_EmitsEmptyArray()
    {
        var files = OarGen.Generate(Sample());
        var sub = files.Single(f => f.RelPath.EndsWith("Sofia Katana/Idle - base/config.json"));
        var o = (JsonObject)JsonNode.Parse(sub.Content)!;
        Assert.Empty((JsonArray)o["conditions"]!);
    }

    [Fact]
    public void Generate_NpcMovesetSugar_ExpandsIntoConditions()
    {
        var r = new AnimationReplacerSpec
        {
            Mod = "M",
            Submods = new List<OarSubmodSpec>
            {
                new() { Name = "atk", Priority = 10, Hkx = { "a.hkx" },
                        NpcMoveset = new NpcMovesetSpec { RightWeapon = "sword", LeftWeapon = "shield", PlayerOnly = false } },
            },
        };
        var sub = OarGen.Generate(r).Single(f => f.RelPath.EndsWith("M/atk/config.json"));
        var conds = (JsonArray)((JsonObject)JsonNode.Parse(sub.Content)!)["conditions"]!;
        Assert.Single(conds);
        Assert.Equal("AND", (string?)conds[0]!["condition"]);
    }

    [Fact]
    public void HkxPlacements_SingleClipWithReplaces_RenamesToVanillaBasename()
    {
        var copies = OarGen.HkxPlacements(Sample());
        var c = copies.Single(x => x.Source == "anims/ss_atk.hkx");
        Assert.Equal("Meshes/actors/character/animations/OpenAnimationReplacer/Sofia Katana/Attack - Sword & Shield/atk.hkx", c.DestRelPath);
    }

    [Fact]
    public void HkxPlacements_Variants_GoUnderVariantsFolder()
    {
        var r = new AnimationReplacerSpec
        {
            Mod = "M",
            Submods = new List<OarSubmodSpec>
            {
                new() { Name = "idle", Priority = 1, Replaces = "x/mt_idle.hkx",
                        Hkx = { "a.hkx" }, Variants = { "v1.hkx", "v2.hkx" } },
            },
        };
        var copies = OarGen.HkxPlacements(r);
        Assert.Contains(copies, c => c.DestRelPath.EndsWith("M/idle/_variants_mt_idle/1.hkx"));
        Assert.Contains(copies, c => c.DestRelPath.EndsWith("M/idle/_variants_mt_idle/2.hkx"));
    }

    [Fact]
    public void ReplaceVanillaPath_PlacesAtVanillaPath_NoConfig()
    {
        var r = new AnimationReplacerSpec
        {
            Mod = "M",
            Submods = new List<OarSubmodSpec>
            {
                new() { Name = "raw", ReplaceVanillaPath = true, Replaces = "actors/character/animations/male/mt_idle.hkx", Hkx = { "my_idle.hkx" } },
            },
        };
        var files = OarGen.Generate(r);
        Assert.DoesNotContain(files, f => f.RelPath.Contains("/raw/"));
        var copy = OarGen.HkxPlacements(r).Single();
        Assert.Equal("Meshes/actors/character/animations/male/mt_idle.hkx", copy.DestRelPath);
    }
}
