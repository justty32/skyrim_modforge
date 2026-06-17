using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Single-layer terrain texture (WorldspaceSpec.BaseTexture → BTXT base layers). Runs Build
// in-memory (LTEX is an external FormKey, resolved structurally — no Skyrim.esm). Asserts the
// generated LAND carries one base layer per quadrant pointing at the LTEX. Byte-level parity
// against vanilla LAND is a separate xEdit check on the main machine (see WAIT_USER).
public class WorldspaceBaseTextureTests
{
    private const string Climate = "Skyrim.esm:0x000812";
    private const string Ltex    = "Skyrim.esm:0x000C16";  // an LTEX ref (LDirtPath-ish placeholder)

    private static ModKey Out => ModKey.FromNameAndExtension("Test.esp");

    private static WorldspaceSpec MakeWorld(string? baseTex) => new()
    {
        EditorId = "TexWorld",
        Name = "Tex World",
        Climate = Climate,
        Flags = { "SmallWorld" },
        BaseTexture = baseTex ?? "",
        Cells = { new WorldspaceCellSpec { X = 0, Y = 0, Height = 4000f } },
    };

    private static ILandscapeGetter BuildLand(WorldspaceSpec ws)
    {
        var result = Generator.Build(new ModSpec { Worldspaces = { ws } }, Out);
        return result.Mod.Worldspaces.First().SubCells
            .SelectMany(b => b.Items).SelectMany(s => s.Items).First().Landscape!;
    }

    [Fact]
    public void BaseTexture_StampsOneBaseLayerPerQuadrant()
    {
        var land = BuildLand(MakeWorld(Ltex));

        Assert.Equal(4, land.Layers.Count);
        foreach (var layer in land.Layers)
        {
            Assert.Equal(0x000C16u, layer.Header.Texture.FormKey.ID);
            Assert.Equal(0, layer.Header.LayerNumber);
        }
        // All four quadrants are covered exactly once.
        var quadrants = land.Layers.Select(l => l.Header.Quadrant).OrderBy(q => q).ToList();
        Assert.Equal(
            new[] { Quadrant.BottomLeft, Quadrant.BottomRight, Quadrant.TopLeft, Quadrant.TopRight }
                .OrderBy(q => q).ToList(),
            quadrants);
    }

    [Fact]
    public void BaseTexture_Omitted_LeavesLandUntextured()
    {
        var land = BuildLand(MakeWorld(null));
        Assert.Empty(land.Layers);
    }
}
