using ModForge;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Core.Tests;

// TXST (TextureSet) + alternateTextures consumer. Master-free: every spec here builds entirely
// from in-spec records and external <master>:0xFORMID FormKeys (which are NOT resolved at build
// time), so no Skyrim.esm read is needed. Texture CONTENT/rendering is unverifiable headless —
// these assert only the record SLOTS and the consumer WIRING that the .esp stores.
public class TextureSetTests
{
    private static readonly ModKey Key = ModKey.FromNameAndExtension("Test.esp");

    private static (ISkyrimMod Mod, BuildResult Result) Build(ModSpec spec)
    {
        var r = Generator.Build(spec, Key);
        return (r.Mod, r);
    }

    private static ITextureSetGetter TxstOf(ISkyrimMod mod, string editorId) =>
        Assert.Single(mod.TextureSets, t => t.EditorID == editorId);

    [Fact]
    public void Txst_Slots_Are_Written()
    {
        var spec = new ModSpec
        {
            TextureSets =
            {
                new TextureSetSpec
                {
                    EditorId = "MF_Tex",
                    Diffuse = "mymod\\sword_d.dds",
                    Normal = "mymod\\sword_n.dds",
                    Mask = "mymod\\sword_m.dds",
                    Glow = "mymod\\sword_g.dds",
                    Height = "mymod\\sword_p.dds",
                    Environment = "mymod\\sword_e.dds",
                    Multilayer = "mymod\\sword_ml.dds",
                    Backlight = "mymod\\sword_b.dds",
                    Flags = { "NoSpecularMap", "HasModelSpaceNormalMap" },
                },
            },
        };

        var (mod, _) = Build(spec);
        var t = TxstOf(mod, "MF_Tex");

        Assert.Equal("mymod\\sword_d.dds", t.Diffuse?.GivenPath);
        Assert.Equal("mymod\\sword_n.dds", t.NormalOrGloss?.GivenPath);
        Assert.Equal("mymod\\sword_m.dds", t.EnvironmentMaskOrSubsurfaceTint?.GivenPath);
        Assert.Equal("mymod\\sword_g.dds", t.GlowOrDetailMap?.GivenPath);
        Assert.Equal("mymod\\sword_p.dds", t.Height?.GivenPath);
        Assert.Equal("mymod\\sword_e.dds", t.Environment?.GivenPath);
        Assert.Equal("mymod\\sword_ml.dds", t.Multilayer?.GivenPath);
        Assert.Equal("mymod\\sword_b.dds", t.BacklightMaskOrSpecular?.GivenPath);
        Assert.True(t.Flags!.Value.HasFlag(TextureSet.Flag.NoSpecularMap));
        Assert.True(t.Flags!.Value.HasFlag(TextureSet.Flag.HasModelSpaceNormalMap));
    }

    [Fact]
    public void Txst_Unset_Slots_Stay_Null()
    {
        var spec = new ModSpec
        {
            TextureSets = { new TextureSetSpec { EditorId = "MF_DiffuseOnly", Diffuse = "mymod\\rock_d.dds" } },
        };

        var (mod, _) = Build(spec);
        var t = TxstOf(mod, "MF_DiffuseOnly");

        Assert.Equal("mymod\\rock_d.dds", t.Diffuse?.GivenPath);
        // An omitted slot must leave the mesh's original map — i.e. write nothing.
        Assert.Null(t.NormalOrGloss);
        Assert.Null(t.GlowOrDetailMap);
        Assert.Null(t.Environment);
    }

    [Fact]
    public void Static_AlternateTextures_Wired_To_InSpec_Txst()
    {
        var spec = new ModSpec
        {
            TextureSets = { new TextureSetSpec { EditorId = "MF_Tex", Diffuse = "mymod\\rubble_d.dds" } },
            Statics =
            {
                new StaticSpec
                {
                    EditorId = "MF_Rubble",
                    Model = "Dungeons\\Nordic\\Rubble\\NorRubblePiece03.nif",
                    AlternateTextures =
                    {
                        new AlternateTextureSpec { Name = "NorRubblePiece03:0", Index = 0, TextureSet = "MF_Tex" },
                    },
                },
            },
        };

        var (mod, _) = Build(spec);
        var stat = Assert.Single(mod.Statics, s => s.EditorID == "MF_Rubble");
        var txst = TxstOf(mod, "MF_Tex");

        Assert.NotNull(stat.Model);
        Assert.Equal("Dungeons\\Nordic\\Rubble\\NorRubblePiece03.nif", stat.Model!.File.GivenPath);
        var alt = Assert.Single(stat.Model.AlternateTextures!);
        Assert.Equal("NorRubblePiece03:0", alt.Name);
        Assert.Equal(0, alt.Index);
        // The override must point at the in-spec TXST's actual FormKey.
        Assert.Equal(txst.FormKey, alt.NewTexture.FormKey);
    }

    [Fact]
    public void Activator_AlternateTextures_Wired_To_External_Txst()
    {
        // A vanilla TXST ref (resolved as a FormKey, no master read).
        var spec = new ModSpec
        {
            Activators =
            {
                new ActivatorSpec
                {
                    EditorId = "MF_Acti",
                    Name = "Retextured Lever",
                    Model = "Dungeons\\Caves\\Furniture\\CaveLever01.nif",
                    AlternateTextures =
                    {
                        new AlternateTextureSpec { Name = "CaveLever01:0", TextureSet = "Skyrim.esm:0x0140F1" },
                    },
                },
            },
        };

        var (mod, result) = Build(spec);
        var acti = Assert.Single(mod.Activators, a => a.EditorID == "MF_Acti");
        var alt = Assert.Single(acti.Model!.AlternateTextures!);

        Assert.Equal("CaveLever01:0", alt.Name);
        Assert.Equal(0x0140F1u, alt.NewTexture.FormKey.ID);
        Assert.Equal("Skyrim.esm", alt.NewTexture.FormKey.ModKey.FileName);
        Assert.Equal(1, result.Stats.ExternalLinks);   // the external TXST ref counts as one external link
    }

    [Fact]
    public void AlternateTextures_On_ModelLess_Record_Warns_And_Skips()
    {
        var spec = new ModSpec
        {
            TextureSets = { new TextureSetSpec { EditorId = "MF_Tex", Diffuse = "mymod\\x_d.dds" } },
            Statics =
            {
                // No `model` — there is nothing to retexture.
                new StaticSpec
                {
                    EditorId = "MF_NoModel",
                    AlternateTextures = { new AlternateTextureSpec { Name = "x:0", TextureSet = "MF_Tex" } },
                },
            },
        };

        var (mod, result) = Build(spec);
        var stat = Assert.Single(mod.Statics, s => s.EditorID == "MF_NoModel");

        Assert.Null(stat.Model);   // nothing wired
        Assert.Contains(result.Warnings, w => w.Contains("MF_NoModel") && w.Contains("no base model"));
    }

    [Fact]
    public void Build_Counts_Txst_As_TopLevel_Record()
    {
        var spec = new ModSpec
        {
            TextureSets =
            {
                new TextureSetSpec { EditorId = "MF_A", Diffuse = "a\\a_d.dds" },
                new TextureSetSpec { EditorId = "MF_B", Diffuse = "b\\b_d.dds" },
            },
        };

        var (_, result) = Build(spec);
        Assert.Equal(2, result.Stats.TopLevelRecords);
    }
}
