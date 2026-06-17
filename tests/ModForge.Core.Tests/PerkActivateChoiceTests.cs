using System;
using System.IO;
using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// B組 #1 — interactive perk entry points: addActivateChoice ("[E] <label>" → spell and/or Papyrus
// fragment) + setText (change the activation prompt). Mutagen shape reflection-verified; PerkAdapter
// byte fields + fragment signature pending main-machine xEdit compare vs Immersive Interactions.
public class PerkActivateChoiceTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static PerkSpec ChoicePerk(string fragment = "") => new()
    {
        EditorId = "MF_SitPerk", Name = "Sit Anywhere",
        Effects =
        {
            new PerkEffectSpec
            {
                Kind = "addActivateChoice", ButtonLabel = "Sit", Spell = "Skyrim.esm:0x0010D9A6",
                FragmentBody = fragment,
                Conditions = { new ConditionSpec { Function = "GetIsID", Comparison = "==", Value = 1, Param = "Skyrim.esm:0x000FbF" } },
            },
            new PerkEffectSpec { Kind = "setText", Text = "Sit here" },
        },
    };

    [Fact]
    public void AddActivateChoice_EmitsEntryPointWithLabelAndSpell()
    {
        var spec = new ModSpec { PluginName = "Test.esp", Perks = { ChoicePerk() } };
        var perk = TestBuild.Raw(spec).Mod.Perks.Single(p => p.EditorID == "MF_SitPerk");
        var ch = perk.Effects.OfType<PerkEntryPointAddActivateChoice>().Single();
        Assert.Equal(APerkEntryPointEffect.EntryType.Activate, ch.EntryPoint);
        Assert.Equal("Sit", ch.ButtonLabel.String);
        Assert.False(ch.Spell.IsNull);
        Assert.Single(ch.Conditions);                              // GetIsID filter wired
        Assert.NotEqual((byte)0, ch.PerkConditionTabCount);        // never 0 (CTD guard)
    }

    [Fact]
    public void SetText_EmitsEntryPointWithText()
    {
        var spec = new ModSpec { PluginName = "Test.esp", Perks = { ChoicePerk() } };
        var perk = TestBuild.Raw(spec).Mod.Perks.Single(p => p.EditorID == "MF_SitPerk");
        var st = perk.Effects.OfType<PerkEntryPointSetText>().Single();
        Assert.Equal(APerkEntryPointEffect.EntryType.Activate, st.EntryPoint);
        Assert.Equal("Sit here", st.Text.String);
    }

    [Fact]
    public void RecordOnlyChoice_NoFragment_NoVmad()
    {
        // A spell-only choice (no fragmentBody) needs no perk script VMAD.
        var spec = new ModSpec { PluginName = "Test.esp", Perks = { ChoicePerk() } };
        var perk = TestBuild.Raw(spec).Mod.Perks.Single(p => p.EditorID == "MF_SitPerk");
        Assert.Null(perk.VirtualMachineAdapter);
        Assert.False(Generator.PerkNeedsFragmentScript(ChoicePerk()));
    }

    [Fact]
    public void FragmentSource_EmitsFragmentFunctionWithBody()
    {
        var p = ChoicePerk("akActor.SetSitState(akTargetRef)\nDebug.Notification(\"Sitting\")");
        Assert.True(Generator.PerkNeedsFragmentScript(p));
        Assert.Equal("MF_SitPerk_Frags", Generator.PerkFragmentScriptName(p));
        var src = Generator.GeneratePerkFragmentSource(p);
        Assert.Contains("Scriptname MF_SitPerk_Frags extends Perk", src);
        Assert.Contains("Function Fragment_0(ObjectReference akTargetRef, Actor akActor)", src);
        Assert.Contains("akActor.SetSitState(akTargetRef)", src);
        Assert.Contains("Debug.Notification(\"Sitting\")", src);
    }

    [Fact]
    public void Build_WithCompiledPex_AttachesPerkAdapterAndBindsFragment()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-perk-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "MF_SitPerk_Frags.pex"), "");
            var spec = new ModSpec { PluginName = "Test.esp", Perks = { ChoicePerk("akActor.SetSitState(akTargetRef)") } };
            var r = TestBuild.OkWithCompiledScripts(spec, dir);
            var perk = r.Mod.Perks.Single(p => p.EditorID == "MF_SitPerk");
            var pa = (PerkAdapter)perk.VirtualMachineAdapter!;
            Assert.Contains(pa.Scripts, s => s.Name == "MF_SitPerk_Frags");
            var frag = pa.ScriptFragments!.Fragments.Single();
            Assert.Equal((ushort)0, frag.FragmentIndex);
            Assert.Equal("Fragment_0", frag.FragmentName);
            // The choice's flag now carries RunImmediately + the matching FragmentIndex.
            var ch = perk.Effects.OfType<PerkEntryPointAddActivateChoice>().Single();
            Assert.True(ch.Flags!.Flags.HasFlag(PerkScriptFlag.Flag.RunImmediately));
            Assert.Equal((ushort)0, ch.Flags.FragmentIndex);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void Build_FragmentChoice_NoPex_LeavesChoiceFragmentless()
    {
        // Without the compiled .pex, the choice still builds (spell-only) but no VMAD / RunImmediately.
        var spec = new ModSpec { PluginName = "Test.esp", Perks = { ChoicePerk("Debug.Notification(\"x\")") } };
        var perk = TestBuild.Raw(spec).Mod.Perks.Single(p => p.EditorID == "MF_SitPerk");
        Assert.Null(perk.VirtualMachineAdapter);
        var ch = perk.Effects.OfType<PerkEntryPointAddActivateChoice>().Single();
        Assert.False(ch.Flags!.Flags.HasFlag(PerkScriptFlag.Flag.RunImmediately));
    }

    [Fact]
    public void Validate_EmptyButtonLabel_Reported()
    {
        var spec = new ModSpec { Perks = { new PerkSpec { EditorId = "P", Name = "P",
            Effects = { new PerkEffectSpec { Kind = "addActivateChoice", Spell = "Skyrim.esm:0x1" } } } } };
        Assert.Contains(Validate(spec), p => p.Contains("empty buttonLabel"));
    }

    [Fact]
    public void Validate_DoNothingChoice_Reported()
    {
        var spec = new ModSpec { Perks = { new PerkSpec { EditorId = "P", Name = "P",
            Effects = { new PerkEffectSpec { Kind = "addActivateChoice", ButtonLabel = "Do" } } } } };
        Assert.Contains(Validate(spec), p => p.Contains("does nothing"));
    }

    [Fact]
    public void Validate_SetText_EmptyText_Reported()
    {
        var spec = new ModSpec { Perks = { new PerkSpec { EditorId = "P", Name = "P",
            Effects = { new PerkEffectSpec { Kind = "setText" } } } } };
        Assert.Contains(Validate(spec), p => p.Contains("setText has empty text"));
    }
}
