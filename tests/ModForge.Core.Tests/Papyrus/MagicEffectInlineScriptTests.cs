using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// I組 — MagicEffectSpec inline scripts[]: attach a Papyrus script to the MGEF's own VMAD without
// hoisting it to the top-level scripts[] (targetEditorId implied). Functionality already worked via
// the generic path; this is the DX/co-location sugar.
public class MagicEffectInlineScriptTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static ModSpec WithInlineScript() => new()
    {
        PluginName = "Test.esp",
        MagicEffects =
        {
            new MagicEffectSpec
            {
                EditorId = "MF_Eff", Name = "Eff", Archetype = "Script",
                Scripts =
                {
                    new ScriptAttachSpec
                    {
                        ScriptName = "MF_EffScript",
                        Properties = { new PropertySpec { Name = "Power", Type = "int", Int = 3 } },
                    },
                },
            },
        },
    };

    [Fact]
    public void InlineScript_AttachedToMgefVmad()
    {
        var r = TestBuild.Ok(WithInlineScript());
        var mgef = r.Mod.MagicEffects.Single(m => m.EditorID == "MF_Eff");
        Assert.NotNull(mgef.VirtualMachineAdapter);
        var entry = mgef.VirtualMachineAdapter!.Scripts.Single(s => s.Name == "MF_EffScript");
        var prop = (IScriptIntPropertyGetter)entry.Properties.Single(p => p.Name == "Power");
        Assert.Equal(3, prop.Data);
    }

    [Fact]
    public void InlineScript_EmptyScriptName_Reported()
    {
        var s = new ModSpec
        {
            MagicEffects = { new MagicEffectSpec { EditorId = "E", Name = "E",
                Scripts = { new ScriptAttachSpec { ScriptName = "" } } } },
        };
        Assert.Contains(Validate(s), p => p.Contains("magicEffect 'E' inline script has empty scriptName"));
    }
}
