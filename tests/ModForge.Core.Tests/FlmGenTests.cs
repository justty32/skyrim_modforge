using ModForge;
using Xunit;

namespace ModForge.Tests;

// FLM _FLM.ini generation. Line format verified against FLM v1.8.1
// (sub_projs/mod-survey/findings/formlist-manipulator-config-core.md / -advanced.md).
public class FlmGenTests
{
    [Fact]
    public void Emits_FlmIni_AtModRoot()
    {
        var f = FlmGen.Generate(new FormListInjectSpec
        {
            File = "MyMod",
            Entries = { new FlmEntrySpec { Target = "BYOHGiftList", Forms = { "0x8246~HearthFires.esm" } } },
        });
        Assert.Equal("MyMod_FLM.ini", f.RelPath);   // mod root (= Data/), not under SKSE/
        Assert.Contains("[General]", f.Content);
        Assert.Contains("FormList = BYOHGiftList|0x8246~HearthFires.esm", f.Content);
    }

    [Fact]
    public void Entry_JoinsFormsWithCommaSpace_AndAppendsFilter()
    {
        var f = FlmGen.Generate(new FormListInjectSpec
        {
            File = "m",
            Entries = { new FlmEntrySpec { Target = "TargetList", Forms = { "A", "B", "#Dolls" }, Filter = "HFFilter" } },
        });
        Assert.Contains("FormList = TargetList|A, B, #Dolls|#HFFilter", f.Content);
    }

    [Fact]
    public void Filter_AuthorSuppliedHash_NotDoubled()
    {
        var f = FlmGen.Generate(new FormListInjectSpec
        {
            File = "m",
            Entries = { new FlmEntrySpec { Target = "L", Forms = { "X" }, Filter = "#Already" } },
        });
        Assert.Contains("|#Already\n", f.Content);
        Assert.DoesNotContain("##", f.Content);
    }

    [Fact]
    public void Definitions_EmittedBeforeFormListLines()
    {
        var f = FlmGen.Generate(new FormListInjectSpec
        {
            File = "m",
            Filters = { new FlmFilterSpec { Name = "HFFilter", Conditions = { "+HearthFires.esm", "-Vigilant.esm" } } },
            Aliases = { new FlmNamedListSpec { Name = "TestAlias", Items = { "0x8246~HearthFires.esm", "0x03008246~HearthFires.esm" } } },
            Groups = { new FlmNamedListSpec { Name = "Dolls", Items = { "BYOHChefDoll", "BYOHDBDoll" } } },
            Collections = { new FlmCollectionSpec { Name = "IronWarAxes", FormType = "Weapon", Keywords = { "WeapTypeWarAxe", "-MagicDisallowEnchanting" } } },
            Entries = { new FlmEntrySpec { Target = "#TestAlias", Forms = { "#Dolls" } } },
        });
        var c = f.Content;
        Assert.Contains("Filter = HFFilter|+HearthFires.esm, -Vigilant.esm", c);
        Assert.Contains("Alias = TestAlias|0x8246~HearthFires.esm, 0x03008246~HearthFires.esm", c);
        Assert.Contains("Group = Dolls|BYOHChefDoll, BYOHDBDoll", c);
        Assert.Contains("Collection = IronWarAxes|Weapon|WeapTypeWarAxe, -MagicDisallowEnchanting", c);
        Assert.Contains("FormList = #TestAlias|#Dolls", c);
        // a defined alias/group/collection must appear before the FormList line that references it
        Assert.True(c.IndexOf("Alias =") < c.IndexOf("FormList ="));
        Assert.True(c.IndexOf("Collection =") < c.IndexOf("FormList ="));
    }

    [Fact]
    public void Collection_WithFilter_AppendsFilterRef()
    {
        var f = FlmGen.Generate(new FormListInjectSpec
        {
            File = "m",
            Collections = { new FlmCollectionSpec { Name = "C", FormType = "Armor", Keywords = { "ArmorHeavy" }, Filter = "HFFilter" } },
        });
        Assert.Contains("Collection = C|Armor|ArmorHeavy|#HFFilter", f.Content);
    }
}
