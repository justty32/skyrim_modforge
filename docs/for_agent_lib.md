# Using ModForge as a library (ModForge.Core)

For when a static JSON spec isn't enough — you want to **compose the spec in code** (loops,
conditionals, data pulled from elsewhere), embed generation in a larger program, or have an AI
agent write C# that calls ModForge directly. `ModForge.Core` is the reusable engine; it works on
objects and never touches the console, argv, or hard-coded file paths.

← index: [for_agent.md](for_agent.md) · CLI path: [for_agent_cli.md](for_agent_cli.md) · spec fields: [SPEC.md](SPEC.md)

## CLI + JSON vs. library — which to use

| | CLI + JSON ([for_agent_cli.md](for_agent_cli.md)) | Library (`ModForge.Core`) |
|---|---|---|
| The spec is… | a static `.json` file you write | a `ModSpec` object you build in code |
| Best for | "describe a mod → generate it" (reviewable, diffable, re-runnable) | dynamic composition, embedding, reacting to build warnings programmatically |
| The agent | writes JSON, runs `validate`/`build` | writes C# that references Core |
| Cost | none (no compile step) | you compile + run a C# project |

Default to CLI + JSON. Reach for the library when the spec must be **computed**, not authored.

## Reference it

```bash
dotnet add <your.csproj> reference path/to/src/ModForge.Core/ModForge.Core.csproj
```

`Mutagen.Bethesda.Skyrim` flows in transitively, so `ISkyrimMod`, `ModKey`, etc. are available.
Everything public lives in `namespace ModForge`.

## The API surface

| Member | Shape |
|---|---|
| `Generator.Validate(ModSpec)` | → `IReadOnlyList<string>` problems (empty = valid). **Run this first** — `Build` does not auto-validate. |
| `Generator.Build(ModSpec, ModKey, BuildOptions?)` | → `BuildResult { ISkyrimMod Mod; IReadOnlyList<string> Warnings; BuildStats Stats }`. Builds in memory; **you write it**. |
| `Translator.Extract(ISkyrimMod)` | → `List<StringEntry>` (every translatable string; `Source` set, `Target` empty). |
| `Translator.Apply(ISkyrimMod, IEnumerable<StringEntry>)` | → `int` applied; mutates the mod inline. |
| `Translator.ApplyLocalized(ISkyrimMod, entries, outDir)` | → `(int Applied, int Renamed, string EspPath)`; writes a Localized UTF-8 `_chinese.STRINGS` set. |
| `Demo.CreateDemoPlugin(ModKey)` | → `ISkyrimMod` (toolchain sanity check). |
| `Papyrus.Compile(scriptPath, outDir, PapyrusOptions?)` | → `CompileResult { bool Success; int ExitCode; string? PexPath; string Message }` (never throws on a compile error). |
| `PluginIo.Load(path)` / `PluginIo.Write(mod, path)` | load a plugin / write one (`Write` uses `ModKeyOption.NoCheck`). |
| `ModSpec` + every `*Spec` + `StringEntry` | public data model (mutable; `List<>` collection-initializer friendly). |

`BuildOptions.SkyrimDataPath` overrides where master plugins (Skyrim.esm, for template clones /
vanilla-cell overrides) are read from; when null it falls back to `MODFORGE_SKYRIM_DATA`, then the
default Steam path. `spec.Esl` (default true) drives `IsSmallMaster`.

## Worked example — generate, validate, write

```csharp
using ModForge;
using Mutagen.Bethesda.Plugins;

var spec = new ModSpec
{
    PluginName = "MyMod.esp",
    Weapons =
    {
        new WeaponSpec
        {
            EditorId = "MF_Blade", Name = "Forged Blade",
            Template = "Skyrim.esm:0x012EB7",   // clone IronSword's model/anim
            Damage = 12, Value = 100, Weight = 9,
        },
    },
};

var problems = Generator.Validate(spec);
if (problems.Count > 0)
{
    foreach (var p in problems) Console.Error.WriteLine(p);
    return;   // fix the spec; Build assumes a valid spec
}

var result = Generator.Build(spec, ModKey.FromNameAndExtension("MyMod.esp"));
foreach (var w in result.Warnings) Console.WriteLine(w);   // non-fatal authoring notes
Console.WriteLine($"{result.Stats.TopLevelRecords} record(s), {result.Stats.LinksWired} link(s)");
PluginIo.Write(result.Mod, "MyMod.esp");                   // or keep editing result.Mod in memory
```

## Dynamic composition — the reason to use the library

```csharp
// Build N leveled-list entries from data the agent gathered at runtime.
var list = new LeveledItemSpec { EditorId = "MF_Loot" };
foreach (var (itemRef, lvl) in lootTable)              // lootTable computed elsewhere
    list.Entries.Add(new LeveledEntrySpec { Reference = itemRef, Level = (short)lvl, Count = 1 });
var spec = new ModSpec { LeveledItems = { list } };
```

The same loop in JSON would mean templating a file by hand; in code it's just a `foreach`.

## Warnings are data, not output

`BuildResult.Warnings` collects every non-fatal authoring problem (unresolved ref, missing model,
unsupported package template, …) that the CLI would print as `  ! …`. Inspect or assert on them
programmatically — a build with warnings still produces a mod; decide per-warning whether to abort.

## Same honesty rule

Building a structurally-valid mod is **not** the same as in-game-functional — see the
[Limits section in for_agent.md](for_agent.md#limits--be-honest-do-not-over-claim). The library
gives you the bytes; only a Proton/Skyrim launch confirms behaviour.
