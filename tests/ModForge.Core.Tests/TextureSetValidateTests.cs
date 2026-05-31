using ModForge;
using Xunit;

namespace ModForge.Core.Tests;

// Validate guardrails for TXST + alternateTextures. All master-free (Validate never reads a master).
public class TextureSetValidateTests
{
    private static bool HasProblem(ModSpec spec, string fragment) =>
        Generator.Validate(spec).Any(p => p.Contains(fragment, System.StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void Valid_Txst_With_Diffuse_Has_No_Problems()
    {
        var spec = new ModSpec
        {
            TextureSets = { new TextureSetSpec { EditorId = "MF_Tex", Diffuse = "mymod\\sword_d.dds", Normal = "mymod\\sword_n.dds" } },
        };
        Assert.Empty(Generator.Validate(spec));
    }

    [Fact]
    public void Txst_With_No_Slots_Is_Flagged()
    {
        var spec = new ModSpec { TextureSets = { new TextureSetSpec { EditorId = "MF_Empty" } } };
        Assert.True(HasProblem(spec, "sets no texture slots"));
    }

    [Fact]
    public void Txst_Path_With_Textures_Prefix_Is_Rejected()
    {
        // TXST slots are already relative to Data\Textures — a leading Textures\ would double it.
        var spec = new ModSpec
        {
            TextureSets = { new TextureSetSpec { EditorId = "MF_Tex", Diffuse = "Textures\\mymod\\sword_d.dds" } },
        };
        Assert.True(HasProblem(spec, "must NOT start with 'Textures\\'"));
    }

    [Fact]
    public void Txst_NonDds_Path_Is_Rejected()
    {
        var spec = new ModSpec
        {
            TextureSets = { new TextureSetSpec { EditorId = "MF_Tex", Diffuse = "mymod\\sword.png" } },
        };
        Assert.True(HasProblem(spec, "must be a .dds texture"));
    }

    [Fact]
    public void Txst_Absolute_Path_Is_Rejected()
    {
        var spec = new ModSpec
        {
            TextureSets = { new TextureSetSpec { EditorId = "MF_Tex", Diffuse = "/home/user/sword_d.dds" } },
        };
        Assert.True(HasProblem(spec, "must be RELATIVE"));
    }

    [Fact]
    public void Txst_Invalid_Flag_Is_Rejected()
    {
        var spec = new ModSpec
        {
            TextureSets = { new TextureSetSpec { EditorId = "MF_Tex", Diffuse = "mymod\\d.dds", Flags = { "Nonsense" } } },
        };
        Assert.True(HasProblem(spec, "invalid flag 'Nonsense'"));
    }

    [Fact]
    public void AltTexture_With_Unresolved_TextureSet_Ref_Is_Rejected()
    {
        var spec = new ModSpec
        {
            Statics =
            {
                new StaticSpec
                {
                    EditorId = "MF_Stat",
                    Model = "x.nif",
                    AlternateTextures = { new AlternateTextureSpec { Name = "x:0", TextureSet = "MF_DoesNotExist" } },
                },
            },
        };
        Assert.True(HasProblem(spec, "unresolved ref 'MF_DoesNotExist'"));
    }

    [Fact]
    public void AltTexture_Without_Base_Model_Is_Flagged()
    {
        var spec = new ModSpec
        {
            TextureSets = { new TextureSetSpec { EditorId = "MF_Tex", Diffuse = "mymod\\d.dds" } },
            Statics =
            {
                new StaticSpec
                {
                    EditorId = "MF_NoModel",
                    AlternateTextures = { new AlternateTextureSpec { Name = "x:0", TextureSet = "MF_Tex" } },
                },
            },
        };
        Assert.True(HasProblem(spec, "no `model`"));
    }

    [Fact]
    public void AltTexture_With_Empty_Name_Is_Flagged()
    {
        var spec = new ModSpec
        {
            TextureSets = { new TextureSetSpec { EditorId = "MF_Tex", Diffuse = "mymod\\d.dds" } },
            Statics =
            {
                new StaticSpec
                {
                    EditorId = "MF_Stat",
                    Model = "x.nif",
                    AlternateTextures = { new AlternateTextureSpec { Name = "", TextureSet = "MF_Tex" } },
                },
            },
        };
        Assert.True(HasProblem(spec, "empty `name`"));
    }

    [Fact]
    public void Duplicate_Txst_EditorId_Is_Flagged()
    {
        var spec = new ModSpec
        {
            TextureSets =
            {
                new TextureSetSpec { EditorId = "MF_Dup", Diffuse = "a\\a_d.dds" },
                new TextureSetSpec { EditorId = "MF_Dup", Diffuse = "b\\b_d.dds" },
            },
        };
        Assert.True(HasProblem(spec, "duplicate editorId 'MF_Dup'"));
    }
}
