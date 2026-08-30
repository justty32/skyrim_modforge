using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class WorldspaceCellLightingTests
{
    [Fact]
    public void ExteriorCell_WiresInSpecLightingTemplateAndExternalImageSpace()
    {
        var cellSpec = new WorldspaceCellSpec
        {
            X = 2,
            Y = -3,
            LightingTemplate = "MF_GloomLGTM",
            ImageSpace = "Skyrim.esm:0x10FEF7",
        };
        var spec = new ModSpec
        {
            Esl = false,
            LightingTemplates = { new LightingTemplateSpec { EditorId = "MF_GloomLGTM" } },
            Worldspaces =
            {
                new WorldspaceSpec
                {
                    EditorId = "GloomWorld",
                    Climate = "Skyrim.esm:0x000812",
                    Cells = { cellSpec },
                },
            },
        };

        var mod = Generator.Build(spec, ModKey.FromNameAndExtension("CellLighting.esp")).Mod;
        var lgtm = mod.LightingTemplates.Single(x => x.EditorID == "MF_GloomLGTM");
        var cell = mod.Worldspaces.Single(x => x.EditorID == "GloomWorld").SubCells
            .SelectMany(b => b.Items).SelectMany(s => s.Items).Single();

        Assert.Equal(lgtm.FormKey, cell.LightingTemplate.FormKey);
        Assert.Equal(FormKey.Factory("10FEF7:Skyrim.esm"), cell.ImageSpace.FormKey);
    }

    [Fact]
    public void ExteriorCell_OmittedLightingRefs_LeavesBothLinksNull()
    {
        var spec = new ModSpec
        {
            Esl = false,
            Worldspaces =
            {
                new WorldspaceSpec
                {
                    EditorId = "PlainWorld",
                    Climate = "Skyrim.esm:0x000812",
                    Cells = { new WorldspaceCellSpec { X = 0, Y = 0 } },
                },
            },
        };

        var cell = Generator.Build(spec, ModKey.FromNameAndExtension("PlainCell.esp")).Mod.Worldspaces
            .Single().SubCells.SelectMany(b => b.Items).SelectMany(s => s.Items).Single();

        Assert.True(cell.LightingTemplate.IsNull);
        Assert.True(cell.ImageSpace.IsNull);
    }

    [Fact]
    public void Validate_ExteriorCellRejectsWrongInSpecLightingRecordTypes()
    {
        var spec = new ModSpec
        {
            LightingTemplates = { new LightingTemplateSpec { EditorId = "OnlyLGTM" } },
            ImageSpaces = { new ImageSpaceSpec { EditorId = "OnlyIMGS" } },
            Worldspaces =
            {
                new WorldspaceSpec
                {
                    EditorId = "BadWorld",
                    Climate = "Skyrim.esm:0x000812",
                    Cells =
                    {
                        new WorldspaceCellSpec
                        {
                            LightingTemplate = "OnlyIMGS",
                            ImageSpace = "OnlyLGTM",
                        },
                    },
                },
            },
        };

        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("lightingTemplate 'OnlyIMGS' unresolved"));
        Assert.Contains(problems, p => p.Contains("imageSpace 'OnlyLGTM' unresolved"));
    }
}
