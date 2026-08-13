// =====================================================================================
//  ModForge.Cli — thin CLI over ModForge.Core.
//
//  This project owns argv parsing, JSON read/write, console output and exit codes.
//  All generation/translation logic lives in ModForge.Core (Generator/Translator/Demo/
//  Papyrus) and works on objects — so it can also be referenced as a library.
//
//  THIS FILE IS DELIBERATELY THIN. Adding a command must NOT mean editing it. Each area
//  owns both halves of its own dispatch — the argv shapes and the help text — next to the
//  code that implements them:
//
//      Commands/Program.Dispatch.cs        build / package / catalog / voice / translate…
//      Diagnostics/Diagnostics.Dispatch.cs dump / find / *diag
//
//  A dispatcher returns null for an argv shape it does not recognise, which is what makes
//  an unmatched command fall through to Usage() — exactly like the old `default:` arm of
//  the single switch these were split out of.
// =====================================================================================

internal static partial class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0) { Usage(); return 1; }
        try
        {
            // Name spaces are disjoint between the two tables, so the order of these two
            // calls carries no meaning — the first one that recognises the shape wins.
            if (DispatchCore(args) is { } core) return core;
            if (DispatchDiagnostics(args) is { } diag) return diag;
            Usage();
            return 1;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"ERROR: {e.GetType().Name}: {e.Message}");
            if (Environment.GetEnvironmentVariable("MODFORGE_DEBUG") is not null)
                Console.Error.WriteLine(e.ToString());
            return 2;
        }
    }

    private static void Usage() => Console.WriteLine("ModForge.Cli\n" + CoreUsage + DiagnosticsUsage);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        // $env values arrive as JSON strings; allow them in numeric spec fields.
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
    };

    // Single chokepoint: read a spec file and resolve $ref/$env before any deserialize / field check.
    private static string ResolveSpecJson(string path) => SpecRefs.ResolveFile(path);

    private static ModSpec ReadSpec(string path)
    {
        var json = ResolveSpecJson(path);
        // ReadOpts leaves UnmappedMemberHandling at the default (Skip), so a
        // misspelled or invented field deserializes to nothing at all and the
        // build "succeeds" having silently dropped it. `validate` has always
        // reported these; surface them here too so `build` cannot swallow them.
        foreach (var u in CheckUnknownFields(json, typeof(ModSpec)))
            Console.Error.WriteLine($"  ! {u}");

        return JsonSerializer.Deserialize<ModSpec>(json, ReadOpts)
            ?? throw new InvalidOperationException("spec deserialized to null");
    }

    // Shared loader (also used by the diagnostic commands in Diagnostics/).
    private static ISkyrimMod Load(string path) => PluginIo.Load(path);
}
