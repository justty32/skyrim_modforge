using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Idea #20 Phase 3 — the `skillTrees:` generator macro-expands a compact tree into the low-level
// records the IN-GAME-CONFIRMED hand-authored tree used (globals, node/line activators, placements,
// MFSkillNode script attach). MVP = vertical linear chain.
public class SkillTreeTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static ModSpec TreeSpec() => new()
    {
        PluginName = "MFTree.esp",
        Spells =
        {
            new SpellSpec { EditorId = "AbA" }, new SpellSpec { EditorId = "AbB" }, new SpellSpec { EditorId = "AbC" },
        },
        SkillTrees =
        {
            new SkillTreeSpec
            {
                EditorId = "T", Name = "Tree", Cell = "Skyrim.esm:0x01605E",
                Origin = new Vec3 { X = 10, Y = 20, Z = 100 }, Spacing = 65, StartingPoints = 3,
                Nodes =
                {
                    new SkillNodeSpec { EditorId = "A", Name = "Node A", Ability = "AbA" },
                    new SkillNodeSpec { EditorId = "B", Name = "Node B", Ability = "AbB" },
                    new SkillNodeSpec { EditorId = "C", Name = "Node C", Ability = "AbC" },
                },
            },
        },
    };

    [Fact]
    public void Expand_CreatesPointsAndPerNodeRankGlobals()
    {
        var s = TreeSpec();
        Generator.ExpandSkillTrees(s);
        var ids = s.Globals.Select(g => g.EditorId).ToList();
        Assert.Contains("T_Points", ids);
        Assert.Contains("T_A_Rank", ids);
        Assert.Contains("T_B_Rank", ids);
        Assert.Contains("T_C_Rank", ids);
        Assert.Equal(3, s.Globals.Single(g => g.EditorId == "T_Points").Value);   // startingPoints
    }

    [Fact]
    public void Expand_StacksNodesAndPlacesLinesAtMidpoints()
    {
        var s = TreeSpec();
        Generator.ExpandSkillTrees(s);
        // 3 node activators + 2 line activators
        Assert.Equal(5, s.Activators.Count);
        var nodeA = s.Placements.Single(p => p.EditorId == "T_ARef");
        var nodeC = s.Placements.Single(p => p.EditorId == "T_CRef");
        Assert.Equal(100f, nodeA.Position.Z);            // origin.z + 0*spacing
        Assert.Equal(230f, nodeC.Position.Z);            // origin.z + 2*spacing
        var line1 = s.Placements.Single(p => p.EditorId == "T_Line1Ref");
        Assert.Equal(132.5f, line1.Position.Z);          // midpoint between A(100) and B(165)
        Assert.Equal(90f, line1.Rotation.X);             // Frostfall vertical-line rotation
        Assert.Equal(180f, line1.Rotation.Z);
        Assert.True(nodeA.Persistent);
    }

    [Fact]
    public void Expand_WiresGatingChain_RootHasNoPrereq()
    {
        var s = TreeSpec();
        Generator.ExpandSkillTrees(s);
        var rootScript = s.Scripts.Single(x => x.TargetEditorId == "T_A");
        var midScript = s.Scripts.Single(x => x.TargetEditorId == "T_B");
        Assert.Equal(Generator.SkillNodeScript, rootScript.ScriptName);
        // root: no prereq / no downLine; ability + rank + points + name
        Assert.DoesNotContain(rootScript.Properties, p => p.Name == "prereqGlobal");
        Assert.DoesNotContain(rootScript.Properties, p => p.Name == "downLine");
        Assert.Equal("AbA", rootScript.Properties.Single(p => p.Name == "nodeAbility").ObjectEditorId);
        // mid: prereq = previous node's rank; downLine set
        Assert.Equal("T_A_Rank", midScript.Properties.Single(p => p.Name == "prereqGlobal").ObjectEditorId);
        Assert.Equal("T_Line1Ref", midScript.Properties.Single(p => p.Name == "downLine").ObjectEditorId);
    }

    [Fact]
    public void Expand_IsIdempotent()
    {
        var s = TreeSpec();
        Generator.ExpandSkillTrees(s);
        var globals = s.Globals.Count;
        Generator.ExpandSkillTrees(s);   // guard: second call is a no-op
        Assert.Equal(globals, s.Globals.Count);
    }

    [Fact]
    public void Build_ProducesNodesAndLines_WithNodeScripts()
    {
        var s = TreeSpec();
        var result = Generator.Build(s, ModKey.FromNameAndExtension("MFTree.esp"));
        var mod = result.Mod;
        Assert.Equal(5, mod.Activators.Count());          // 3 node + 2 line
        // every node activator carries the MFSkillNode script; the 2 lines carry none
        var scripted = mod.Activators.Count(a => a.VirtualMachineAdapter?.Scripts.Any(sc => sc.Name == Generator.SkillNodeScript) == true);
        Assert.Equal(3, scripted);
        // the 3 per-node rank globals + the points global landed as GLOB records
        Assert.True(mod.Globals.Count() >= 4);
    }

    [Fact]
    public void Validate_FlagsMissingAbility_DuplicateNode_AndMissingCell()
    {
        var s = new ModSpec
        {
            SkillTrees =
            {
                new SkillTreeSpec
                {
                    EditorId = "T", Cell = "",
                    Nodes =
                    {
                        new SkillNodeSpec { EditorId = "A", Name = "A", Ability = "" },
                        new SkillNodeSpec { EditorId = "A", Name = "A2", Ability = "Skyrim.esm:0x0003EB18" },
                    },
                },
            },
        };
        var probs = Validate(s);
        Assert.Contains(probs, p => p.Contains("missing cell"));
        Assert.Contains(probs, p => p.Contains("missing ability"));
        Assert.Contains(probs, p => p.Contains("duplicate node editorId"));
    }

    [Fact]
    public void Validate_CleanTreePasses()
    {
        Assert.Empty(Validate(TreeSpec()));
    }
}
