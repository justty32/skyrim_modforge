using ModForge;

namespace ModForge.Tests;

public class VoiceCacheTests
{
    [Fact]
    public void MatchingMetadataAndArtifact_IsCacheHit()
    {
        var fingerprint = Fingerprint();
        var artifact = VoiceCache.DescribeArtifact("fuz", [1, 2, 3]);
        var metadata = VoiceCache.SerializeMetadata(VoiceCache.CreateMetadata(fingerprint, artifact, null));

        Assert.Equal(fingerprint, Fingerprint());
        Assert.True(VoiceCache.Check(fingerprint, metadata, extension => extension == "fuz" ? artifact : null).IsHit);
    }

    [Theory]
    [MemberData(nameof(ChangedInputs))]
    public void GenerationInputChange_IsCacheMiss(Func<CacheInput, CacheInput> change)
    {
        var input = new CacheInput(Template(), Options(), new VoiceLineSpec { Format = "fuz", SkipLip = false }, "A line", "Anger", 50);
        var original = CreateFingerprint(input);
        var changed = change(input);
        var artifact = VoiceCache.DescribeArtifact("fuz", [1]);

        var check = VoiceCache.Check(CreateFingerprint(changed),
            VoiceCache.SerializeMetadata(VoiceCache.CreateMetadata(original, artifact, null)), extension => extension == "fuz" ? artifact : null);

        Assert.False(check.IsHit);
    }

    public static IEnumerable<object[]> ChangedInputs()
    {
        yield return [new Func<CacheInput, CacheInput>(x => x with { Text = x.Text + " changed" })];
        yield return [new Func<CacheInput, CacheInput>(x => x with { Emotion = "Fear" })];
        yield return [new Func<CacheInput, CacheInput>(x => x with { Intensity = x.Intensity + 1 })];
        yield return [new Func<CacheInput, CacheInput>(x => x with { Template = new VoiceTemplateSpec { Id = "other", Engine = "fish-s2", ReferenceWav = x.Template.ReferenceWav, ReferenceText = x.Template.ReferenceText, ModelPath = x.Template.ModelPath, RvcModel = x.Template.RvcModel, Language = x.Template.Language, Seed = x.Template.Seed, Speed = x.Template.Speed, Exaggeration = x.Template.Exaggeration } })];
        yield return [new Func<CacheInput, CacheInput>(x => x with { Line = new VoiceLineSpec { Format = "wav", SkipLip = x.Line.SkipLip } })];
        yield return [new Func<CacheInput, CacheInput>(x => x with { Line = new VoiceLineSpec { Format = x.Line.Format, SkipLip = true } })];
        yield return [new Func<CacheInput, CacheInput>(x => x with { Options = new VoiceOptions { TtsBin = "tts-v2", XwmaEncodeExe = x.Options.XwmaEncodeExe, LipGenExe = x.Options.LipGenExe, LipLanguage = x.Options.LipLanguage } })];
        yield return [new Func<CacheInput, CacheInput>(x => x with { Options = new VoiceOptions { TtsBin = x.Options.TtsBin, XwmaEncodeExe = "xwma-v2", LipGenExe = x.Options.LipGenExe, LipLanguage = x.Options.LipLanguage } })];
        yield return [new Func<CacheInput, CacheInput>(x => x with { Options = new VoiceOptions { TtsBin = x.Options.TtsBin, XwmaEncodeExe = x.Options.XwmaEncodeExe, LipGenExe = x.Options.LipGenExe, LipLanguage = "French" } })];
    }

    [Fact]
    public void MissingOrModifiedArtifactAndInvalidSidecar_AreCacheMisses()
    {
        var fingerprint = Fingerprint();
        var expected = VoiceCache.DescribeArtifact("wav", [0x01, 0x02]);
        var metadata = VoiceCache.SerializeMetadata(VoiceCache.CreateMetadata(fingerprint, expected, null));

        Assert.False(VoiceCache.Check(fingerprint, metadata, _ => null).IsHit);
        Assert.False(VoiceCache.Check(fingerprint, metadata, _ => VoiceCache.DescribeArtifact("wav", [0x01, 0x03])).IsHit);
        Assert.False(VoiceCache.Check(fingerprint, "{not json", _ => expected).IsHit);
    }

    [Fact]
    public void LooseWavFallback_IsBoundByMetadataAndOptionalLipContent()
    {
        var fingerprint = Fingerprint();
        var wav = VoiceCache.DescribeArtifact("wav", [1, 2]);
        var lip = VoiceCache.DescribeArtifact("lip", [3, 4]);
        var metadata = VoiceCache.SerializeMetadata(VoiceCache.CreateMetadata(fingerprint, wav, lip));

        Assert.True(VoiceCache.Check(fingerprint, metadata, extension => extension == "wav" ? wav : lip).IsHit);
        Assert.False(VoiceCache.Check(fingerprint, metadata, extension => extension == "wav" ? wav : VoiceCache.DescribeArtifact("lip", [4, 3])).IsHit);
    }

    [Fact]
    public void SamePathInputContentReplacement_IsCacheMiss()
    {
        var root = Path.Combine(Path.GetTempPath(), "modforge-voice-cache-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(root, "model"));
        try
        {
            File.WriteAllBytes(Path.Combine(root, "ref.wav"), [1, 2, 3]);
            File.WriteAllBytes(Path.Combine(root, "model", "weights.bin"), [4, 5, 6]);
            File.WriteAllBytes(Path.Combine(root, "voice.pth"), [7, 8, 9]);
            File.WriteAllBytes(Path.Combine(root, "tts"), [10, 11]);
            File.WriteAllBytes(Path.Combine(root, "xwma"), [12, 13]);
            File.WriteAllBytes(Path.Combine(root, "lipgen"), [14, 15]);
            var input = new CacheInput(new VoiceTemplateSpec { Id = "voice", ReferenceWav = "ref.wav", ModelPath = "model", RvcModel = "voice.pth" },
                new VoiceOptions { TtsBin = Path.Combine(root, "tts"), XwmaEncodeExe = Path.Combine(root, "xwma"), LipGenExe = Path.Combine(root, "lipgen") },
                new VoiceLineSpec { Format = "fuz" }, "A line", "Anger", 50);
            var original = VoiceCache.CreateFingerprint(input.Text, input.Template, root, input.Line.Format, input.Line.SkipLip, input.Options, input.Emotion, input.Intensity);
            File.WriteAllBytes(Path.Combine(root, "model", "weights.bin"), [9, 5, 6]); // same path and length
            var changed = VoiceCache.CreateFingerprint(input.Text, input.Template, root, input.Line.Format, input.Line.SkipLip, input.Options, input.Emotion, input.Intensity);
            var artifact = VoiceCache.DescribeArtifact("fuz", [1]);

            Assert.False(VoiceCache.Check(changed, VoiceCache.SerializeMetadata(VoiceCache.CreateMetadata(original, artifact, null)), _ => artifact).IsHit);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string Fingerprint() => CreateFingerprint(new CacheInput(Template(), Options(), new VoiceLineSpec { Format = "fuz" }, "A line", "Anger", 50));
    private static string CreateFingerprint(CacheInput input) => VoiceCache.CreateFingerprint(input.Text, input.Template, "/spec", input.Line.Format, input.Line.SkipLip, input.Options, input.Emotion, input.Intensity);
    private static VoiceTemplateSpec Template() => new() { Id = "voice", Engine = "f5", ReferenceWav = "ref.wav", ReferenceText = "reference", ModelPath = "model", RvcModel = "voice.pth", Language = "en", Seed = 7, Speed = 0.9f, Exaggeration = 1.2f };
    private static VoiceOptions Options() => new() { TtsBin = "tts", XwmaEncodeExe = "xwma", LipGenExe = "lipgen", LipLanguage = "USEnglish" };
    public sealed record CacheInput(VoiceTemplateSpec Template, VoiceOptions Options, VoiceLineSpec Line, string Text, string Emotion, int? Intensity);
}
