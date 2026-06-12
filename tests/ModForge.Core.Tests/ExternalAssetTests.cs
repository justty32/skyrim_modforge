using ModForge;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Tests;

// Regression coverage for the EXTERNAL RESOURCE PIPELINE (model-path wiring + custom sounds +
// asset bundling). All of this is master-free: building a model path or a SoundDescriptor needs
// no Skyrim.esm read (no template clone), so these run fully offline/headless.
//
// HONESTY NOTE: these assert the WIRING — that the right record fields hold the right paths/links,
// that validation rejects malformed paths, and that `package` copies the right files. They do NOT
// (and cannot here) verify that the referenced .nif/.wav are valid renderable/audible assets:
// ModForge references + bundles user content, it does not author or validate the asset bytes.
public class ExternalAssetTests
{
    private static readonly ModKey Key = ModKey.FromNameAndExtension("Test.esp");

    private static (BuildResult Result, ISkyrimMod Mod) Build(ModSpec spec, string? skyrimDataPath = null)
    {
        var options = skyrimDataPath is null ? null : new BuildOptions { SkyrimDataPath = skyrimDataPath };
        var r = Generator.Build(spec, Key, options);
        return (r, r.Mod);
    }

    // ---- model-path wiring ---------------------------------------------------------------

    [Fact]
    public void Static_model_path_is_written_to_Model_subrecord()
    {
        var spec = new ModSpec { Statics = { new StaticSpec { EditorId = "S1", Model = @"MyMod\rock.nif" } } };
        var (_, mod) = Build(spec);
        var st = Assert.Single(mod.Statics);
        Assert.Equal(@"MyMod\rock.nif", st.Model!.File.GivenPath);
    }

    [Fact]
    public void Furniture_record_is_emitted_with_name_and_model()
    {
        var spec = new ModSpec { Furniture = { new FurnitureSpec { EditorId = "F1", Name = "Throne", Model = @"MyMod\throne.nif" } } };
        var (_, mod) = Build(spec);
        var fn = Assert.Single(mod.Furniture);
        Assert.Equal("F1", fn.EditorID);
        Assert.Equal("Throne", fn.Name?.String);
        Assert.Equal(@"MyMod\throne.nif", fn.Model!.File.GivenPath);
    }

    [Fact]
    public void MiscItem_model_overrides_and_warns_when_template_also_set()
    {
        var spec = new ModSpec
        {
            MiscItems = { new MiscSpec { EditorId = "M1", Name = "Relic", Template = "Skyrim.esm:0x063B42", Model = @"MyMod\relic.nif" } },
        };
        var emptyDataDir = Directory.CreateTempSubdirectory("mf_no_master_").FullName;
        try
        {
            var (result, mod) = Build(spec, emptyDataDir);
            var mi = Assert.Single(mod.MiscItems);
            Assert.Equal(@"MyMod\relic.nif", mi.Model!.File.GivenPath);
            Assert.Contains(result.Warnings, w => w.Contains("M1") && w.Contains("both `template` and `model`"));
        }
        finally
        {
            Directory.Delete(emptyDataDir, recursive: true);
        }
    }

    [Fact]
    public void Weapon_model_without_template_warns_about_equip_crash()
    {
        var spec = new ModSpec { Weapons = { new WeaponSpec { EditorId = "W1", Name = "Blade", Damage = 10, Model = @"MyMod\blade.nif" } } };
        var (result, mod) = Build(spec);
        var w = Assert.Single(mod.Weapons);
        Assert.Equal(@"MyMod\blade.nif", w.Model!.File.GivenPath);
        Assert.Contains(result.Warnings, x => x.Contains("W1") && x.Contains("no `template`"));
    }

    // ---- custom sound records (SNDR) -----------------------------------------------------

    [Fact]
    public void Sound_descriptor_holds_file_paths_and_default_category_outputmodel()
    {
        var spec = new ModSpec
        {
            Sounds =
            {
                new SoundSpec
                {
                    EditorId = "SND1",
                    Files = { @"Sound\fx\mymod\a.wav", @"Sound\fx\mymod\b.xwm" },
                    Priority = 100, StaticAttenuation = 3.5f,
                },
            },
        };
        var (_, mod) = Build(spec);
        var sd = Assert.Single(mod.SoundDescriptors);
        Assert.Equal("SND1", sd.EditorID);
        Assert.Equal(100, sd.Priority);
        Assert.Equal(3.5f, sd.StaticAttenuation);
        Assert.Equal(new[] { @"Sound\fx\mymod\a.wav", @"Sound\fx\mymod\b.xwm" },
                     sd.SoundFiles.Select(f => f.GivenPath).ToArray());
        // Default vanilla category + output model so the sound is actually audible.
        Assert.Equal(0x0172A1u, sd.Category.FormKey.ID);   // AudioCategorySFX
        Assert.Equal(0x0B4058u, sd.OutputModel.FormKey.ID);
    }

    [Fact]
    public void Custom_category_and_outputmodel_refs_override_defaults()
    {
        var spec = new ModSpec
        {
            Sounds =
            {
                new SoundSpec { EditorId = "SND2", Files = { @"Sound\fx\x.wav" },
                                Category = "Skyrim.esm:0x000E9E", OutputModel = "Skyrim.esm:0x0B4058" },
            },
        };
        var (_, mod) = Build(spec);
        var sd = Assert.Single(mod.SoundDescriptors);
        Assert.Equal(0x000E9Eu, sd.Category.FormKey.ID);
    }

    // ---- record -> sound FormLink wiring -------------------------------------------------

    [Fact]
    public void Activator_and_misc_sound_links_resolve_to_in_spec_sounds()
    {
        var spec = new ModSpec
        {
            Sounds = { new SoundSpec { EditorId = "Chime", Files = { @"Sound\fx\chime.wav" } } },
            Activators = { new ActivatorSpec { EditorId = "Bell", Name = "Bell", ActivationSound = "Chime" } },
            MiscItems = { new MiscSpec { EditorId = "Coin", Name = "Coin", PickUpSound = "Chime" } },
        };
        var (_, mod) = Build(spec);
        var snd = Assert.Single(mod.SoundDescriptors);
        var acti = Assert.Single(mod.Activators);
        var misc = Assert.Single(mod.MiscItems);
        Assert.Equal(snd.FormKey, acti.ActivationSound.FormKey);
        Assert.Equal(snd.FormKey, misc.PickUpSound.FormKey);
    }

    // ---- validation ----------------------------------------------------------------------

    [Fact]
    public void Validate_accepts_well_formed_asset_spec()
    {
        var spec = new ModSpec
        {
            Sounds = { new SoundSpec { EditorId = "S", Files = { @"Sound\fx\mymod\a.wav" } } },
            Statics = { new StaticSpec { EditorId = "St", Model = @"MyMod\a.nif" } },
            Activators = { new ActivatorSpec { EditorId = "Ac", Name = "A", Model = @"MyMod\b.nif", ActivationSound = "S" } },
        };
        Assert.Empty(Generator.Validate(spec));
    }

    [Theory]
    [InlineData(@"MyMod\a.obj", "must be a .nif")]
    [InlineData(@"Meshes\MyMod\a.nif", "must NOT start with 'Meshes")]
    [InlineData(@"C:\stuff\a.nif", "absolute")]
    public void Validate_rejects_bad_model_paths(string model, string fragment)
    {
        var spec = new ModSpec { Statics = { new StaticSpec { EditorId = "St", Model = model } } };
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains(fragment));
    }

    [Fact]
    public void Validate_rejects_sound_without_files_and_bad_extension()
    {
        var spec = new ModSpec
        {
            Sounds =
            {
                new SoundSpec { EditorId = "Empty" },
                new SoundSpec { EditorId = "BadExt", Files = { "song.mp3" } },
            },
        };
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("Empty") && p.Contains("no files"));
        Assert.Contains(problems, p => p.Contains("BadExt") && p.Contains(".wav or .xwm"));
    }

    [Fact]
    public void Validate_rejects_unresolved_sound_ref()
    {
        var spec = new ModSpec { Activators = { new ActivatorSpec { EditorId = "Bell", Name = "B", ActivationSound = "Missing" } } };
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("activationSound") && p.Contains("Missing"));
    }

    // ---- asset bundling ------------------------------------------------------------------

    [Fact]
    public void Bundle_copies_recognised_subtrees_and_skips_others()
    {
        // Fabricate a tiny placeholder asset tree (asset CONTENT is unverifiable here — we only
        // assert the copy/wiring). Use a unique temp dir.
        var src = Directory.CreateTempSubdirectory("mf_assets_src_").FullName;
        var outDir = Directory.CreateTempSubdirectory("mf_assets_out_").FullName;
        try
        {
            WriteFile(Path.Combine(src, "Meshes", "Mine", "bell.nif"), "nif-placeholder");
            WriteFile(Path.Combine(src, "Textures", "Mine", "bell.dds"), "dds-placeholder");
            WriteFile(Path.Combine(src, "Sound", "fx", "mine", "chime.wav"), "wav-placeholder");
            WriteFile(Path.Combine(src, "ReadMe.txt"), "ignored — not a recognised asset folder");
            WriteFile(Path.Combine(src, "Docs", "notes.md"), "ignored — not a recognised asset folder");

            var br = Assets.Bundle(src, outDir);

            Assert.Equal(3, br.FilesCopied);
            Assert.True(File.Exists(Path.Combine(outDir, "Meshes", "Mine", "bell.nif")));
            Assert.True(File.Exists(Path.Combine(outDir, "Textures", "Mine", "bell.dds")));
            Assert.True(File.Exists(Path.Combine(outDir, "Sound", "fx", "mine", "chime.wav")));
            // Non-asset top-level files/dirs are NOT bundled.
            Assert.False(File.Exists(Path.Combine(outDir, "ReadMe.txt")));
            Assert.False(Directory.Exists(Path.Combine(outDir, "Docs")));
        }
        finally
        {
            Directory.Delete(src, recursive: true);
            Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public void Bundle_warns_and_copies_nothing_for_missing_source()
    {
        var outDir = Directory.CreateTempSubdirectory("mf_assets_out_").FullName;
        try
        {
            var br = Assets.Bundle(Path.Combine(outDir, "does-not-exist"), outDir);
            Assert.Equal(0, br.FilesCopied);
            Assert.Contains(br.Warnings, w => w.Contains("not found"));
        }
        finally { Directory.Delete(outDir, recursive: true); }
    }

    [Fact]
    public void Bundle_matches_subfolders_case_insensitively()
    {
        var src = Directory.CreateTempSubdirectory("mf_assets_src_").FullName;
        var outDir = Directory.CreateTempSubdirectory("mf_assets_out_").FullName;
        try
        {
            // lower-case "meshes" on a case-sensitive FS still gets bundled under canonical "Meshes".
            WriteFile(Path.Combine(src, "meshes", "x.nif"), "nif");
            var br = Assets.Bundle(src, outDir);
            Assert.Equal(1, br.FilesCopied);
            Assert.True(File.Exists(Path.Combine(outDir, "Meshes", "x.nif")));
        }
        finally
        {
            Directory.Delete(src, recursive: true);
            Directory.Delete(outDir, recursive: true);
        }
    }

    private static void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
