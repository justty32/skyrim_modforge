using System.IO;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class CellWaterTests
{
    private static readonly ModKey Key = ModKey.FromNameAndExtension("CellWaterTest.esp");
    private static readonly ModKey TemplateMaster = ModKey.FromNameAndExtension("MFCellTemplate.esm");
    private static readonly FormKey TemplateCell = FormKey.Factory("000801:MFCellTemplate.esm");

    [Fact]
    public void CellWaterFields_AreWritten()
    {
        var spec = new ModSpec { PluginName = Key.FileName };
        spec.Cells.Add(new CellSpec
        {
            EditorId = "WetRoom",
            WaterHeight = -321.5f,
            Water = "Skyrim.esm:0x000018",
            AcousticSpace = "Skyrim.esm:0x0C5ABC",
        });

        var cell = Assert.Single(TestBuild.Ok(spec).Mod.EnumerateMajorRecords<ICellGetter>());
        Assert.Equal(-321.5f, cell.WaterHeight);
        Assert.Equal(FormKey.Factory("000018:Skyrim.esm"), cell.Water.FormKey);
        Assert.Equal(FormKey.Factory("0C5ABC:Skyrim.esm"), cell.AcousticSpace.FormKey);
    }

    [Fact]
    public void ExplicitWaterHeight_OverridesTemplateWaterHeight()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-cell-water-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            WriteTemplateMaster(dir, waterHeight: 10f);
            var spec = new ModSpec { PluginName = Key.FileName };
            spec.Cells.Add(new CellSpec
            {
                EditorId = "WetRoom",
                Template = TemplateMaster.FileName + ":0x000801",
                WaterHeight = 42.25f,
            });

            var result = Generator.Build(spec, Key, new BuildOptions { SkyrimDataPath = dir });
            Assert.Empty(result.Warnings);
            var cell = Assert.Single(result.Mod.EnumerateMajorRecords<ICellGetter>());
            Assert.Equal(42.25f, cell.WaterHeight);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Validate_ChecksWaterAndAcousticSpaceRefs()
    {
        var spec = new ModSpec { PluginName = Key.FileName };
        spec.Cells.Add(new CellSpec
        {
            EditorId = "WetRoom",
            Water = "MissingWater",
            AcousticSpace = "MissingAcousticSpace",
        });

        var problems = Generator.Validate(spec).ToArray();
        Assert.Contains(problems, p => p.Contains("cell 'WetRoom' water") && p.Contains("unresolved ref"));
        Assert.Contains(problems, p => p.Contains("cell 'WetRoom' acousticSpace") && p.Contains("unresolved ref"));
    }

    private static void WriteTemplateMaster(string dir, float waterHeight)
    {
        var master = new SkyrimMod(TemplateMaster, SkyrimRelease.SkyrimSE);
        var cell = new Cell(TemplateCell, SkyrimRelease.SkyrimSE)
        {
            EditorID = "TemplateRoom",
            Flags = Cell.Flag.IsInteriorCell,
            WaterHeight = waterHeight,
        };
        var sub = new CellSubBlock { BlockNumber = 4, GroupType = GroupTypeEnum.InteriorCellSubBlock };
        sub.Cells.Add(cell);
        var block = new CellBlock { BlockNumber = 9, GroupType = GroupTypeEnum.InteriorCellBlock };
        block.SubBlocks.Add(sub);
        master.Cells.Records.Add(block);
        master.WriteToBinary(Path.Combine(dir, TemplateMaster.FileName), new BinaryWriteParameters
        {
            ModKey = ModKeyOption.NoCheck,
            MastersListContent = MastersListContentOption.Iterate,
        });
    }
}
