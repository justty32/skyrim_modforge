using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ModForge;

/// <summary>One content-addressed voice output named by its extension.</summary>
public sealed record VoiceCacheArtifact(string Extension, long ByteLength, string Sha256);

/// <summary>Versioned sidecar contract for one generated voice payload.</summary>
public sealed record VoiceCacheMetadata(int Version, string Fingerprint, IReadOnlyList<VoiceCacheArtifact> Artifacts);

/// <summary>Pure cache decision result; a miss is always safe to regenerate.</summary>
public sealed record VoiceCacheCheck(bool IsHit, string Reason);

/// <summary>Per-command memoizer for the deterministic content identity of configured voice inputs.</summary>
public sealed class VoiceContentIdentityCache
{
    private readonly Dictionary<string, string> identities = new(StringComparer.Ordinal);

    public string Get(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "not-configured";
        try
        {
            var fullPath = Path.GetFullPath(path);
            if (!identities.TryGetValue(fullPath, out var identity))
            {
                identity = File.Exists(fullPath) ? HashFile(fullPath)
                    : Directory.Exists(fullPath) ? HashDirectory(fullPath)
                    : "missing";
                identities.Add(fullPath, identity);
            }
            return identity;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return "unreadable:" + ex.GetType().Name;
        }
    }

    private static string HashFile(string path)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendFile(hash, path);
        return "file:" + Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    private static string HashDirectory(string root)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendFrame(hash, "voice-cache-directory-v1");
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => new { Path = path, RelativePath = Path.GetRelativePath(root, path).Replace('\\', '/') })
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal);
        foreach (var file in files)
        {
            AppendFrame(hash, file.RelativePath);
            AppendFile(hash, file.Path);
        }
        return "directory:" + Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    // Directory entries are ordinal-normalized relative paths followed by length-framed raw bytes.
    private static void AppendFile(IncrementalHash hash, string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        Span<byte> length = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(length, stream.Length);
        hash.AppendData(length);
        var buffer = new byte[81920];
        for (var read = stream.Read(buffer, 0, buffer.Length); read > 0; read = stream.Read(buffer, 0, buffer.Length))
            hash.AppendData(buffer, 0, read);
    }

    private static void AppendFrame(IncrementalHash hash, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(length, bytes.Length);
        hash.AppendData(length);
        hash.AppendData(bytes);
    }
}

/// <summary>
/// Deterministic voice-output cache contract. The fingerprint covers every ModForge input that can
/// affect the TTS, encoding, or lip payload; the sidecar records content identities for actual output.
/// </summary>
public static class VoiceCache
{
    public const int MetadataVersion = 2;
    private static readonly string[] MainArtifactExtensions = ["fuz", "wav", "xwm"];
    private static readonly JsonSerializerOptions MetadataJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string SidecarPath(string stemPath) => stemPath + ".voice-cache.json";

    public static string CreateFingerprint(string text, VoiceTemplateSpec template, string specDirectory,
        string format, bool skipLip, VoiceOptions options, string? emotion, int? intensity,
        VoiceContentIdentityCache? contentIdentities = null)
    {
        contentIdentities ??= new VoiceContentIdentityCache();
        var bytes = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(bytes))
        {
            json.WriteStartObject();
            json.WriteNumber("version", MetadataVersion);
            json.WriteString("text", text);
            json.WriteString("emotion", NormalizeOptional(emotion));
            if (intensity.HasValue) json.WriteNumber("intensity", intensity.Value); else json.WriteNull("intensity");
            json.WriteString("format", NormalizeFormat(format));
            json.WriteBoolean("skipLip", skipLip);
            json.WriteStartObject("template");
            json.WriteString("id", template.Id);
            json.WriteString("engine", template.Engine);
            WriteInput(json, "referenceWav", ResolveTemplatePath(template.ReferenceWav, specDirectory), contentIdentities);
            json.WriteString("referenceText", NormalizeOptional(template.ReferenceText));
            WriteInput(json, "modelPath", ResolveTemplatePath(template.ModelPath, specDirectory), contentIdentities);
            WriteInput(json, "rvcModel", ResolveTemplatePath(template.RvcModel, specDirectory), contentIdentities);
            json.WriteString("language", NormalizeOptional(template.Language));
            if (template.Seed.HasValue) json.WriteNumber("seed", template.Seed.Value); else json.WriteNull("seed");
            if (template.Speed.HasValue) json.WriteNumber("speed", template.Speed.Value); else json.WriteNull("speed");
            if (template.Exaggeration.HasValue) json.WriteNumber("exaggeration", template.Exaggeration.Value); else json.WriteNull("exaggeration");
            json.WriteEndObject();
            json.WriteStartObject("options");
            WriteInput(json, "ttsBin", NormalizeOptional(options.ResolvedTtsBin), contentIdentities);
            if (NeedsXwma(format)) WriteInput(json, "xwmaEncodeExe", NormalizeOptional(options.ResolvedXwmaEncodeExe), contentIdentities);
            if (NeedsLip(format, skipLip))
            {
                WriteInput(json, "lipGenExe", NormalizeOptional(options.ResolvedLipGenExe), contentIdentities);
                WriteInput(json, "faceFxExe", NormalizeOptional(options.ResolvedFaceFxExe), contentIdentities);
                WriteInput(json, "fonixDataCdf", NormalizeOptional(options.ResolvedFonixDataCdf), contentIdentities);
                json.WriteString("lipLanguage", options.ResolvedLipLanguage);
            }
            json.WriteEndObject();
            json.WriteEndObject();
        }
        return Convert.ToHexString(SHA256.HashData(bytes.WrittenSpan)).ToLowerInvariant();
    }

    public static VoiceCacheArtifact DescribeArtifact(string extension, ReadOnlySpan<byte> data) =>
        new(NormalizeArtifactExtension(extension, allowLip: true), data.Length,
            Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant());

    public static VoiceCacheMetadata CreateMetadata(string fingerprint, VoiceCacheArtifact mainArtifact, VoiceCacheArtifact? looseLip) =>
        new(MetadataVersion, fingerprint, looseLip is null ? [mainArtifact] : [mainArtifact, looseLip]);

    /// <summary>Decide from metadata and a content-reader seam; never throws.</summary>
    public static VoiceCacheCheck Check(string expectedFingerprint, string? metadataJson,
        Func<string, VoiceCacheArtifact?> readArtifact)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return new(false, "missing sidecar metadata");
        try
        {
            var metadata = JsonSerializer.Deserialize<VoiceCacheMetadata>(metadataJson, MetadataJson);
            if (metadata is null || metadata.Version != MetadataVersion || string.IsNullOrWhiteSpace(metadata.Fingerprint))
                return new(false, "invalid sidecar metadata");
            if (!SameText(expectedFingerprint, metadata.Fingerprint)) return new(false, "fingerprint mismatch");
            if (!ValidArtifacts(metadata.Artifacts)) return new(false, "invalid sidecar artifacts");
            foreach (var expected in metadata.Artifacts)
            {
                var actual = readArtifact(expected.Extension);
                if (actual is null) return new(false, $"missing artifact .{expected.Extension}");
                if (actual.Extension != expected.Extension || actual.ByteLength != expected.ByteLength || !SameText(actual.Sha256, expected.Sha256))
                    return new(false, $"artifact .{expected.Extension} content mismatch");
            }
            return new(true, "matching metadata and artifacts");
        }
        catch (JsonException)
        {
            return new(false, "invalid sidecar metadata");
        }
    }

    public static string SerializeMetadata(VoiceCacheMetadata metadata) => JsonSerializer.Serialize(metadata, MetadataJson);

    private static void WriteInput(Utf8JsonWriter json, string name, string? path, VoiceContentIdentityCache contentIdentities)
    {
        json.WriteStartObject(name);
        json.WriteString("path", path);
        json.WriteString("content", contentIdentities.Get(path));
        json.WriteEndObject();
    }

    private static bool ValidArtifacts(IReadOnlyList<VoiceCacheArtifact>? artifacts) =>
        artifacts is { Count: 1 or 2 }
        && artifacts.All(artifact => artifact is not null && artifact.ByteLength >= 0 && IsSha256(artifact.Sha256))
        && MainArtifactExtensions.Contains(artifacts[0].Extension, StringComparer.Ordinal)
        && (artifacts.Count == 1 || artifacts[1].Extension == "lip");

    private static bool IsSha256(string value) => value.Length == 64 && value.All(char.IsAsciiHexDigit);
    private static bool SameText(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
    private static string NormalizeArtifactExtension(string extension, bool allowLip)
    {
        var normalized = extension.TrimStart('.').ToLowerInvariant();
        if (!MainArtifactExtensions.Contains(normalized, StringComparer.Ordinal) && (!allowLip || normalized != "lip"))
            throw new ArgumentOutOfRangeException(nameof(extension), "Voice cache artifacts must be .fuz, .wav, .xwm, or .lip.");
        return normalized;
    }

    private static bool NeedsXwma(string format) => NormalizeFormat(format) is "fuz" or "xwm";
    private static bool NeedsLip(string format, bool skipLip) => NormalizeFormat(format) == "fuz" && !skipLip;
    private static string NormalizeFormat(string format) => string.IsNullOrWhiteSpace(format) ? "fuz" : format.Trim().TrimStart('.').ToLowerInvariant();
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static string? ResolveTemplatePath(string? path, string specDirectory) =>
        string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(Path.Combine(specDirectory, path));
}
