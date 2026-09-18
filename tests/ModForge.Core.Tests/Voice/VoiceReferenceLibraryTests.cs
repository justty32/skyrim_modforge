using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace ModForge.Core.Tests;

public sealed class VoiceReferenceLibraryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "mf_reference_" + Guid.NewGuid());
    private string ManifestPath => Path.Combine(root, "refs", "voice-annotations.json");
    private static VoiceTemplateSpec Template() => new()
    {
        ReferenceLibrary = "refs/voice-annotations.json",
        ReferenceWav = "fallback.wav", ReferenceText = "fallback transcript"
    };
    private static VoiceAnnotation Clip(string name, string emotion, int intensity, string id = "") => new()
    {
        Clip = name, Emotion = emotion, Intensity = intensity, InfoFormId = id, Text = "transcript " + name
    };
    private void WriteRaw(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath)!);
        File.WriteAllText(ManifestPath, json);
    }
    private void Write(params VoiceAnnotation[] entries) => WriteRaw(JsonSerializer.Serialize(entries));
    private List<string> Args(string? emotion = "Anger", int? intensity = 70) =>
        Voice.BuildTtsArgs("line", Template(), root, "out.wav", emotion, intensity);
    private static string Value(List<string> args, string flag) => args[args.IndexOf(flag) + 1];
    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    [Fact]
    public void NoLibrary_ExactLegacyArguments()
    {
        var template = new VoiceTemplateSpec
        {
            Engine = "fish-s2", ReferenceWav = "refs/ref.wav", ReferenceText = "old transcript",
            ModelPath = "models/fish", RvcModel = "rvc/voice.pth", Seed = 7,
            Speed = 0.8f, Exaggeration = 1.25f, Language = "en"
        };
        var args = Voice.BuildTtsArgs("hello", template, "spec", "out.wav", "Anger", 75);
        // Keep the literal legacy order and values; only normalize platform path separators.
        Assert.Equal("--engine|fish-s2|--text|hello|--out|out.wav|--emotion|Anger|--intensity|75|" +
            "--ref-wav|spec/refs/ref.wav|--ref-text|old transcript|--model|spec/models/fish|" +
            "--rvc|spec/rvc/voice.pth|--seed|7|--speed|0.8|--exaggeration|1.25|--language|en",
            string.Join("|", args).Replace('\\', '/'));
    }

    [Fact]
    public void Library_ReplacesBothReferenceArguments_RelativeToManifest()
    {
        Write(Clip("angry.wav", "Anger", 70));
        var args = Args();
        Assert.Equal(Path.Combine(root, "refs", "angry.wav"), Value(args, "--ref-wav"));
        Assert.Equal("transcript angry.wav", Value(args, "--ref-text"));
        Assert.Equal("Anger", Value(args, "--emotion"));
        Assert.Equal("70", Value(args, "--intensity"));
    }

    [Fact]
    public void MatchingEmotion_PrecedesIntensity_AndIgnoresCase()
    {
        var correct = Clip("angry.wav", "aNgEr", 0);
        Assert.Same(correct, Voice.SelectReferenceClip("ANGER", 100,
            [Clip("sad.wav", "Sad", 100), correct]));
    }

    [Fact]
    public void MatchingEmotion_SelectsNearestIntensity()
    {
        var nearest = Clip("near.wav", "Anger", 65);
        Assert.Same(nearest, Voice.SelectReferenceClip("Anger", 70,
            [Clip("far.wav", "Anger", 90), nearest]));
    }

    [Fact]
    public void Ties_ChooseOrdinalFormId_RegardlessOfInputOrder()
    {
        var first = Clip("first.wav", "Anger", 70, "0x00000001");
        var second = Clip("second.wav", "Anger", 70, "0x00000002");
        Assert.Same(first, Voice.SelectReferenceClip("Anger", 70, [second, first]));
        Assert.Same(first, Voice.SelectReferenceClip("Anger", 70, [first, second]));
    }

    [Fact]
    public void DuplicateFormIds_TieBreakByClipThenTranscript()
    {
        var first = Clip("a.wav", "Anger", 70);
        first.Text = "a";
        var samePath = Clip("a.wav", "Anger", 70);
        var last = Clip("z.wav", "Anger", 70);
        Assert.Same(first, Voice.SelectReferenceClip("Anger", 70, [last, samePath, first]));
        Assert.Same(first, Voice.SelectReferenceClip("Anger", 70, [first, samePath, last]));
    }

    [Fact]
    public void EmotionOverride_ReplacesSourceEmotion()
    {
        WriteRaw("""
            [{"clip":"corrected.wav","emotion":"Neutral","override":"Anger","intensity":70,"text":"corrected"},
             {"clip":"neutral.wav","emotion":"Neutral","intensity":70}]
            """);
        var entries = Voice.LoadReferenceLibrary(ManifestPath);
        Assert.Equal("Anger", entries[0].Emotion);
        Assert.Equal("corrected.wav", Voice.SelectReferenceClip("Anger", 70, entries)!.Clip);
        Assert.Equal("corrected", Value(Args(), "--ref-text"));
    }

    [Fact]
    public void IntensityOverride_ReplacesSourceIntensity_IncludingZero()
    {
        var corrected = Clip("corrected.wav", "Anger", 100);
        corrected.IntensityOverride = 0;
        Write(corrected, Clip("other.wav", "Anger", 10));
        var entries = Voice.LoadReferenceLibrary(ManifestPath);
        Assert.Equal(0, entries[0].Intensity);
        Assert.Equal("corrected.wav", Voice.SelectReferenceClip("Anger", 0, entries)!.Clip);
        Assert.Equal(Path.Combine(root, "refs", "corrected.wav"), Value(Args("Anger", 0), "--ref-wav"));
    }

    [Fact]
    public void MissingEmotion_FallsBackToNearestNeutral()
    {
        Write(Clip("sad.wav", "Sad", 70), Clip("neutral-far.wav", "Neutral", 0),
            Clip("neutral-near.wav", "neutral", 65));
        Assert.Equal(Path.Combine(root, "refs", "neutral-near.wav"), Value(Args(), "--ref-wav"));
    }

    [Fact]
    public void MissingEmotionAndNeutral_FallsBackToTemplate()
    {
        Write(Clip("sad.wav", "Sad", 70));
        Assert.Equal(Path.Combine(root, "fallback.wav"), Value(Args(), "--ref-wav"));
        Assert.Equal("fallback transcript", Value(Args(), "--ref-text"));
    }

    [Fact]
    public void MissingManifest_FailsClosed_WithPathAndCause()
    {
        var error = Assert.Throws<InvalidDataException>(() => Args());
        Assert.Contains("referenceLibrary", error.Message);
        Assert.Contains(ManifestPath, error.Message);
        Assert.IsAssignableFrom<IOException>(error.InnerException);
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{}]")]
    public void InvalidManifest_FailsClosed(string json)
    {
        WriteRaw(json);
        var error = Assert.Throws<InvalidDataException>(() => Args());
        Assert.Contains(ManifestPath, error.Message);
        Assert.IsType<JsonException>(error.InnerException);
    }

    [Fact]
    public void EmptyManifest_ReturnsNoClip_AndFallsBackToTemplate()
    {
        Write();
        Assert.Null(Voice.SelectReferenceClip("Anger", 70, Voice.LoadReferenceLibrary(ManifestPath)));
        Assert.Equal(Path.Combine(root, "fallback.wav"), Value(Args(), "--ref-wav"));
        Assert.Equal("fallback transcript", Value(Args(), "--ref-text"));
    }

    [Fact]
    public void OmittedTargets_DefaultToNeutralAndZero_WithoutMutatingEntries()
    {
        var neutral = Clip("neutral.wav", "Neutral", 0);
        var entries = new[] { Clip("other.wav", "Neutral", 70), neutral };
        var snapshot = JsonSerializer.Serialize(entries);
        Assert.Same(neutral, Voice.SelectReferenceClip(null, null, entries));
        Assert.Equal(snapshot, JsonSerializer.Serialize(entries));
    }

    [Fact]
    public void EmptyCorrection_RetainsSource_AndEmptySelectedTextDoesNotUseFallbackText()
    {
        var clip = Clip("angry.wav", "Anger", 70);
        clip.Override = " ";
        clip.Text = "";
        Write(clip);
        var entry = Voice.LoadReferenceLibrary(ManifestPath).Single();
        Assert.Equal("Anger", entry.Emotion);
        Assert.Equal(70, entry.Intensity);
        Assert.DoesNotContain("--ref-text", Args());
    }
}
