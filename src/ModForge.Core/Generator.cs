namespace ModForge;

/// <summary>
/// The data-driven plugin generator: turns a <see cref="ModSpec"/> into a valid Skyrim mod
/// (records, dialogue, FormLinks, placements, VMAD), and validates a spec before building.
/// This is the library entry point — it works on objects, never files or the console, so a
/// caller (CLI, or an AI agent composing specs in code) decides how to source the spec and
/// where to write the result.
/// </summary>
public static partial class Generator
{
    // Build and Validate live in the partial files Generator.Build.cs / Generator.Validate.cs;
    // the shared ref/flag/grid helpers are private in Generator.Helpers.cs.
}

/// <summary>Tunables for <see cref="Generator.Build"/>.</summary>
public sealed class BuildOptions
{
    /// <summary>
    /// Path to the Skyrim <c>Data</c> folder, used to read master plugins (Skyrim.esm, …) when a
    /// spec clones a vanilla template or overrides a vanilla cell/worldspace. When null, falls back
    /// to the <c>MODFORGE_SKYRIM_DATA</c> env var, then the default Steam install path.
    /// </summary>
    public string? SkyrimDataPath { get; set; }
}

/// <summary>The outcome of <see cref="Generator.Build"/>: the in-memory mod plus warnings and stats.</summary>
public sealed class BuildResult
{
    /// <summary>The generated mod, ready to write (<c>PluginIo.Write</c> or <c>mod.WriteToBinary</c>).</summary>
    public required ISkyrimMod Mod { get; init; }

    /// <summary>Non-fatal authoring problems surfaced during build (unresolved refs, missing models, …).</summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>Counts describing what was built.</summary>
    public required BuildStats Stats { get; init; }
}

/// <summary>Record/link/placement counts from a build.</summary>
public sealed class BuildStats
{
    public bool Esl { get; init; }
    public int TopLevelRecords { get; init; }
    public int DialogueTopics { get; init; }
    public int LinksWired { get; init; }
    public int ExternalLinks { get; init; }
    public int ScriptsAttached { get; init; }
    public int Placements { get; init; }
    public int NewInteriorCells { get; init; }
    public int VanillaInteriorCells { get; init; }
    public int Worldspaces { get; init; }
    public int NewExteriorCells { get; init; }
}
