using ModForge;
using Mutagen.Bethesda.Skyrim;

namespace ModForge.Tests;

public class McmGlobalWiringTests
{
    [Fact]
    public void McmVmProperty_AndPerkCondition_LinkTheSameGlobal()
    {
        var spec = new ModSpec
        {
            PluginName = "McmGlobal.esp",
            Globals = { new GlobalSpec { EditorId = "MF_BarterEnabled", Type = "short", Value = 1 } },
            McmConfigs = { new McmSpec { ModName = "Barter Menu", Pages = { new McmPageSpec { Name = "General",
                Content = { new McmControlSpec { Type = "toggle", Id = "bEnabled:General",
                    SourceType = "ModSettingBool", DefaultBool = true, Global = "MF_BarterEnabled" } } } } } },
            Perks = { new PerkSpec { EditorId = "MF_BarterPerk", Name = "Configurable Barter", Hidden = true,
                Effects = { new PerkEffectSpec { Kind = "entryPoint", EntryPoint = "ModBuyPrices",
                    Function = "Multiply", Value = 0.9f,
                    Conditions = { new ConditionSpec { Function = "GetGlobalValue", Param = "MF_BarterEnabled",
                        Comparison = "==", Value = 1 } } } } } },
        };

        Assert.Empty(Generator.Validate(spec));
        var mod = Generator.Build(spec, Mutagen.Bethesda.Plugins.ModKey.FromNameAndExtension(spec.PluginName)).Mod;
        var global = mod.EnumerateMajorRecords<IGlobalGetter>().Single(g => g.EditorID == "MF_BarterEnabled");

        var mcmScript = mod.Quests.Single(q => q.EditorID!.StartsWith("MF_MCM_"))
            .VirtualMachineAdapter!.Scripts.Single(s => s.Name == Generator.McmGlobalScriptName(spec.McmConfigs[0]));
        var fallbackName = Assert.IsAssignableFrom<IScriptStringPropertyGetter>(
            mcmScript.Properties.Single(p => p.Name == "ModName"));
        // Config lookup uses the owning plugin stem (McmGenTests covers that). This VMAD value is
        // intentionally the human-facing spec label used only as MCM Helper's display fallback.
        Assert.Equal("Barter Menu", fallbackName.Data);
        var property = Assert.IsAssignableFrom<IScriptObjectPropertyGetter>(
            mcmScript.Properties.Single(p => p.Name == Generator.McmGlobalPropertyName(0)));
        Assert.Equal(global.FormKey, property.Object.FormKey);

        var effect = Assert.IsAssignableFrom<IPerkEntryPointModifyValueGetter>(mod.Perks.Single().Effects.Single());
        var condition = Assert.IsAssignableFrom<IGetGlobalValueConditionDataGetter>(
            ((IConditionFloatGetter)effect.Conditions.Single().Conditions.Single()).Data);
        Assert.Equal(global.FormKey, condition.Global.Link.FormKey);
    }
}
