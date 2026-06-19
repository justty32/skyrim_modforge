using System.Linq;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class ValidateFlmTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    [Fact]
    public void ValidSpec_NoFlmProblems()
    {
        var s = new ModSpec
        {
            FormListInjects =
            {
                new FormListInjectSpec
                {
                    File = "MyMod",
                    Entries = { new FlmEntrySpec { Target = "SomeList", Forms = { "0x8246~HearthFires.esm" } } },
                },
            },
        };
        Assert.DoesNotContain(Validate(s), p => p.Contains("formListInject"));
    }

    [Fact]
    public void EmptyFile_Reported()
    {
        var s = new ModSpec { FormListInjects = { new FormListInjectSpec { File = "" } } };
        Assert.Contains(Validate(s), p => p.Contains("formListInject has empty 'file'"));
    }

    [Fact]
    public void EntryWithoutTarget_Reported()
    {
        var s = new ModSpec { FormListInjects = { new FormListInjectSpec { File = "m",
            Entries = { new FlmEntrySpec { Target = "", Forms = { "X" } } } } } };
        Assert.Contains(Validate(s), p => p.Contains("empty 'target'"));
    }

    [Fact]
    public void EntryWithoutForms_Reported()
    {
        var s = new ModSpec { FormListInjects = { new FormListInjectSpec { File = "m",
            Entries = { new FlmEntrySpec { Target = "L" } } } } };
        Assert.Contains(Validate(s), p => p.Contains("no forms to add"));
    }

    [Fact]
    public void Collection_UnknownFormType_Reported()
    {
        var s = new ModSpec { FormListInjects = { new FormListInjectSpec { File = "m",
            Collections = { new FlmCollectionSpec { Name = "C", FormType = "Banana" } } } } };
        Assert.Contains(Validate(s), p => p.Contains("unknown formType 'Banana'"));
    }

    [Fact]
    public void Filter_WithoutConditions_Reported()
    {
        var s = new ModSpec { FormListInjects = { new FormListInjectSpec { File = "m",
            Filters = { new FlmFilterSpec { Name = "F" } } } } };
        Assert.Contains(Validate(s), p => p.Contains("filter 'F' has no conditions"));
    }
}
