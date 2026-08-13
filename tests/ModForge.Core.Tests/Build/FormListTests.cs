using System.Linq;
using ModForge;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Tests;

// FLST (FormList) builder. Master-free: items are in-spec editorIds + external <master>:0xFORMID
// FormKeys (not resolved at build time). Asserts the FLST exists and its Items resolve, in order.
public class FormListTests
{
    private static readonly ModKey Key = ModKey.FromNameAndExtension("Test.esp");

    [Fact]
    public void FormList_Builds_With_Resolved_Items_In_Order()
    {
        var spec = new ModSpec
        {
            // an in-spec weapon to reference by editorId, plus two vanilla refs
            Weapons = { new WeaponSpec { EditorId = "MF_Sword", Name = "Blade" } },
            FormLists =
            {
                new FormListSpec
                {
                    EditorId = "MF_GearList",
                    Items = { "MF_Sword", "Skyrim.esm:0x0001397E", "Skyrim.esm:0x00012EB7" },
                },
            },
        };
        var mod = Generator.Build(spec, Key).Mod;
        var flst = Assert.Single(mod.FormLists, f => f.EditorID == "MF_GearList");
        Assert.Equal(3, flst.Items.Count);
        // first item resolves to the in-spec weapon's FormKey
        var sword = mod.Weapons.First(w => w.EditorID == "MF_Sword");
        Assert.Equal(sword.FormKey, flst.Items[0].FormKey);
        // the two vanilla refs keep their order + 24-bit ids
        Assert.Equal(0x0001397Eu, flst.Items[1].FormKey.ID);
        Assert.Equal(0x00012EB7u, flst.Items[2].FormKey.ID);
    }

    [Fact]
    public void Validate_FormList_Flags_DuplicateEditorId_And_BadItemRef()
    {
        var spec = new ModSpec
        {
            FormLists =
            {
                new FormListSpec { EditorId = "Dup", Items = { "NotAnEditorIdOrRef" } },
                new FormListSpec { EditorId = "Dup" },
            },
        };
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("duplicate editorId 'Dup'"));
        Assert.Contains(problems, p => p.Contains("formList 'Dup' item"));
    }
}
