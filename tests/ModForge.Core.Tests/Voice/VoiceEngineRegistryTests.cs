using Xunit;

namespace ModForge.Core.Tests;

[CollectionDefinition("Voice engine validation console", DisableParallelization = true)]
public class VoiceEngineValidationConsoleCollection { }

[Collection("Voice engine validation console")]
public class VoiceEngineRegistryTests
{
    [Fact]
    public void Registry_ListsExactlyFiveEnginesWithExplicitStatus()
    {
        Assert.Equal(new[] { "f5", "fish-s2", "chatterbox", "gptsovits", "xtts" },
            VoiceEngines.All.Select(e => e.Name));
        Assert.All(VoiceEngines.All.Take(2), e => Assert.Equal(VoiceEngineStatus.Wired, e.Status));
        Assert.All(VoiceEngines.All.Skip(2), e => Assert.Equal(VoiceEngineStatus.Reserved, e.Status));
    }

    [Theory]
    [InlineData("fish")]
    [InlineData("fishspeech")]
    [InlineData("fish-speech")]
    [InlineData("FISH")]
    [InlineData("FiShSpEeCh")]
    [InlineData("FISH-SPEECH")]
    public void FishAliases_ResolveCaseInsensitively(string alias)
    {
        Assert.Same(VoiceEngines.Resolve("fish-s2"), VoiceEngines.Resolve(alias));
        Assert.Empty(Generator.Validate(Spec(alias)));
    }

    [Fact]
    public void Validate_UnknownEngineIsAnError()
    {
        Assert.Null(VoiceEngines.Resolve("bogus"));
        Assert.Contains("unknown engine 'bogus'", Assert.Single(Generator.Validate(Spec("bogus"))));
    }

    [Theory]
    [InlineData("chatterbox", "MODFORGE_CHATTERBOX_BIN")]
    [InlineData("gptsovits", "MODFORGE_GPTSOVITS_BIN")]
    [InlineData("xtts", "MODFORGE_XTTS_BIN")]
    public void Validate_ReservedWarningContainsWrapperContractWithoutBlockingBuild(string engine, string variable)
    {
        var spec = Spec(engine);
        // Validate's return value is the CLI's fatal-error gate. Warnings must stay out of it.
        var previousError = Console.Error;
        using var diagnostic = new StringWriter();
        try
        {
            Console.SetError(diagnostic);
            Assert.Empty(Generator.Validate(spec));
        }
        finally { Console.SetError(previousError); }
        var warning = VoiceEngines.ValidationWarning(spec.VoiceTemplates[0]);
        Assert.NotNull(warning);
        Assert.Equal(warning + Environment.NewLine, diagnostic.ToString());
        Assert.StartsWith("WARNING:", warning);
        Assert.Contains(engine, warning);
        Assert.Contains("not wired", warning);
        Assert.Contains("voicelines will skip all of its voice lines", warning);
        Assert.Contains(variable, warning);
        Assert.Contains("--engine/--text/--out", warning);
        Assert.Contains("--ref-wav/--ref-text", warning);
        Assert.Contains("WAV", warning);
        Assert.Contains("nonzero on failure", warning);
        Assert.Contains("setting the variable alone does not enable it", warning);
    }

    [Theory]
    [InlineData("f5")]
    [InlineData("F5")]
    [InlineData("fish-s2")]
    [InlineData("FISH-S2")]
    [InlineData("fish")]
    [InlineData("fishspeech")]
    [InlineData("fish-speech")]
    public void WiredEngine_HasNoReservedWarningOrSkip(string engine)
    {
        var spec = Spec(engine);
        Assert.Empty(Generator.Validate(spec));
        Assert.Null(VoiceEngines.ValidationWarning(spec.VoiceTemplates[0]));
        Assert.Null(VoiceEngines.GenerationSkipReason(spec.VoiceTemplates[0]));
    }

    [Theory]
    [InlineData("f5")]
    [InlineData("fish-s2")]
    [InlineData("fish")]
    [InlineData("fishspeech")]
    [InlineData("fish-speech")]
    [InlineData("FISH-S2")]
    public void BuildTtsArgs_WiredContractRemainsExactlyTheSame(string engine)
    {
        var template = new VoiceTemplateSpec
        {
            Id = "voice", Engine = engine, ReferenceWav = "ref clip.wav", ReferenceText = "reference text",
            ModelPath = "model dir", RvcModel = "rvc.pth", Seed = 7,
            Speed = 0.8f, Exaggeration = 1.25f, Language = "ja",
        };
        var specDir = Path.Combine(Path.GetTempPath(), "spec folder");
        string[] expected = ["--engine", engine, "--text", "line text", "--out", "out file.wav",
            "--emotion", "Anger", "--intensity", "75", "--ref-wav", Path.Combine(specDir, "ref clip.wav"),
            "--ref-text", "reference text", "--model", Path.Combine(specDir, "model dir"),
            "--rvc", Path.Combine(specDir, "rvc.pth"), "--seed", "7", "--speed", "0.8",
            "--exaggeration", "1.25", "--language", "ja"];
        Assert.Equal(expected, Voice.BuildTtsArgs("line text", template, specDir, "out file.wav", "Anger", 75));
        Assert.Equal(new[] { "--engine", engine, "--text", "hello", "--out", "out.wav", "--language", "en" },
            Voice.BuildTtsArgs("hello", new VoiceTemplateSpec { Engine = engine }, specDir, "out.wav"));
    }

    [Theory]
    [InlineData("chatterbox")]
    [InlineData("gptsovits")]
    [InlineData("xtts")]
    [InlineData("CHATTERBOX")]
    public void ReservedTemplate_DecisionSkipsBeforeAnyExternalProcess(string engine)
    {
        var template = new VoiceTemplateSpec { Id = "reserved", Engine = engine };
        Assert.Equal("skipped: engine reserved", VoiceEngines.GenerationSkipReason(template));
        var plan = VoiceEngines.ApplyToPlan([Entry("reserved")], [template]);
        Assert.Equal("skipped: engine reserved", Assert.Single(plan).SkipReason);
        Assert.Equal(3, VoiceEngines.ExitCode(plan.Count(e => e.SkipReason == VoiceEngines.ReservedSkipReason)));
    }

    [Fact]
    public void Plan_CountsOnlyReservedLineTargetsAndPreservesOtherSkips()
    {
        VoiceTemplateSpec[] templates = [new() { Id = "r", Engine = "xtts" }, new() { Id = "w", Engine = "fish" }];
        var plan = VoiceEngines.ApplyToPlan([Entry("r"), Entry("r"), Entry("w"),
            Entry("r") with { SkipReason = "empty response text", Text = "" },
            Entry(null) with { SkipReason = "speaker unresolved: no speaker" }], templates);
        Assert.Equal(2, plan.Count(e => e.SkipReason == VoiceEngines.ReservedSkipReason));
        Assert.Null(plan[2].SkipReason);
        Assert.Equal("empty response text", plan[3].SkipReason);
        Assert.Equal("speaker unresolved: no speaker", plan[4].SkipReason);
        Assert.Equal(0, VoiceEngines.ExitCode(0));
        Assert.Equal(3, VoiceEngines.ExitCode(2));
    }

    private static ModSpec Spec(string engine) => new()
    {
        VoiceTemplates = [new() { Id = "voice", Engine = engine }],
    };

    private static VoiceLinePlanEntry Entry(string? template) => new(
        "quest", "topic", "info", 0x800, 1, "hello", "GetIsID", ["npc"], "voiceType",
        "file.fuz", "Sound/Voice/file.fuz", template is not null, template, null);
}
