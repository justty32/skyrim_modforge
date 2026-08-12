# ModForge

An AI-driven Skyrim mod **authoring** toolchain. You describe content to an AI agent
(Claude Code), which writes a structured spec; ModForge turns the spec into a valid
`.esp`/`.esl` — and, as a byproduct, translates the text inside existing plugins. Built on
[Mutagen.Bethesda](https://github.com/Mutagen-Modding/Mutagen) (C#/.NET) — generation is
pure .NET and runs on Windows or Linux, no Creation Kit needed.

> Design principle: **the AI agent only emits intent / text (a reviewable JSON spec); a
> deterministic tool (Mutagen) emits the bytes.** Plugin records, FormIDs, masters and record
> sizes are never hand-written — Mutagen guarantees validity. The agent drives the tool per
> `docs/for_agent.md`; there is no in-tool LLM API.

## Pillars (all three operational)

1. **Generate** — spec → `.esp`/`.esl` (NPCs, items, magic, perks, quests, dialogue,
   scenes, AI packages, weather/climate, interior cells, custom worldspaces…).
   Optional post-build `voicelines` can synthesize dialogue voice assets into
   `Sound/Voice/<plugin>/<voiceType>/`.
2. **Translate** — read an existing plugin → extract every translatable string to JSON →
   an AI fills in translations → write back inline, or as a localized plugin + `.STRINGS`.
3. **Papyrus** — AI writes `.psc` → compiled via the open-source native compiler, or the
   CK's `PapyrusCompiler.exe` under Wine on Linux → attached to forms via Mutagen's VMAD.

## Two projects

- **`src/ModForge.Core`** (class library, namespace `ModForge`) — the reusable engine. Works on
  objects, never the console: `Generator.Build(spec, key) → BuildResult{Mod, Warnings, Stats}`,
  `Generator.Validate(spec) → problems`, plus `Translator`, `Demo`, `Papyrus`, `PluginIo`, and the
  public `ModSpec`/`*Spec`/`StringEntry` model. Reference it to compose specs in code (e.g. an AI
  agent generating a spec dynamically) and get an `ISkyrimMod` back.
- **`src/ModForge.Cli`** (the `dotnet run` entry point below) — a thin wrapper: argv + JSON read/write
  + console output + exit codes, plus the diagnostic (`find`/`dump`/`*diag`) commands.

```csharp
// Library use: build a plugin from a spec composed in code, then write it.
var spec = new ModSpec { PluginName = "MyMod.esp", Weapons = { /* … */ } };
var problems = Generator.Validate(spec);
if (problems.Count == 0)
{
    var result = Generator.Build(spec, ModKey.FromNameAndExtension("MyMod.esp"));
    foreach (var w in result.Warnings) Console.WriteLine(w);
    PluginIo.Write(result.Mod, "MyMod.esp");
}
```

Full library guide (API surface, dynamic composition, when to prefer it over CLI + JSON):
[`docs/for_agent_lib.md`](docs/for_agent_lib.md).

## CLI (src/ModForge.Cli)

```
dotnet run --project src/ModForge.Cli -- <command> ...

  build    <spec.json> <out.esp>             spec -> plugin (records, dialogue, FormLinks, VMAD)
  voicelines <spec.json> <built.esp> [--dry-run|--plan]
                                             plan or generate dialogue voice files under Sound/Voice/<plugin>/<voiceType>/
  voicediag <spec.json> <built.esp>          offline speaker/template/path check for every dialogue INFO
  extract-voices <bsaPath> <voiceType> <outDir>
                                             extract vanilla .fuz voices from BSA and convert to WAV reference clips
  package  <spec.json> <outModDir> [--assets <dir>]
                                             build + compile scripts + bundle Meshes/Textures/… -> MO2-ready mod folder
  validate <spec.json>                       semantic check (ids, refs, types) before building
  compile  <script.psc> <outDir>             .psc -> .pex (native compiler, or CK PapyrusCompiler under Wine)
  gen      <out.esp>                         write a demo plugin (for testing)
  dump     <in.esp>                          read a plugin back (records, refs, stats, effects, keywords, masters)
  find     <in.esp> <query> [type]           search a master -> "Skyrim.esm:0xFORMID  Type  EditorID"
  catalog build <out.db> <plugin> [plugin...] build a replace-only offline SQLite/FTS generic-record index
  catalog query <db> <query> [--type Npc] [--plugin MyMod.esp] [--json]
  catalog get <db> <Plugin.esp:0xFORMID> [--plugin <source>] [--json]
  catalog sources <db> [--json]
  gamedata <plugin> <outDir> [--strings <dir>]
                                             extract agent-readable game text and record lists
  questnodes <plugin> <outDir> [--strings <dir>]
                                             non-empty QUST stage logs -> schema-valid quest-node JSON
  extract  <in.esp> <strings.json>           pull translatable strings -> JSON
  apply    <in.esp> <strings.json> <out.esp> write translated strings back (Latin/inline)
  applyloc <in.esp> <strings.json> <outDir>  write a LOCALIZED plugin + UTF-8
                                             <plugin>_chinese.STRINGS (Simplified-Chinese SSE)
```

Plus diagnostic commands (compare generated vs vanilla records, find FormIDs, verify
structure) — most take `<in.esp> <0xFORMID>`; run the CLI with no args for exact usage:

```
  cellblk, refpos                            cell block/sub-block grouping; placed-ref position/rotation/base
  mgefdiag, enchdiag, perkdiag, shoutdiag    magic effects, enchantments, perks, shouts
  npcdiag, cstydiag, factdiag, reladiag      NPCs, combat styles, factions, relationships
  packagediag, pkgsbytemplate                AI packages; every package using a given procedure template
  questdiag, infodiag, scenediag             quest stages/objectives, dialogue INFO + conditions, scenes
  worlddiag, regndiag, eczndiag              worldspaces, regions, encounter zones
  weatherdiag, climatediag, lightdiag        weather, climates, lights
  txstdiag, cobjdiag, bookdiag               texture sets, crafting recipes, books
```

The **spec** format (the JSON the generator consumes) is documented in
[`docs/spec/SPEC-index.md`](docs/spec/SPEC-index.md) with a JSON Schema at [`examples/spec.schema.json`](examples/spec.schema.json);
[`examples/sample_spec.json`](examples/sample_spec.json) is a complete working example.
The agent workflow is in [`docs/for_agent.md`](docs/for_agent.md) (CLI path + library path).
For local Manjaro/Steam Proton master reference generation, see
[`docs/local-skyrim-extraction.md`](docs/local-skyrim-extraction.md).

Voice workflow: [`docs/SPEC-workflow.md`](docs/spec/SPEC-workflow.md#voice-tts-voice-cloning--fuz)
documents `voiceTemplates[]`, `npcs[].voiceTemplate`, `voiceLine`, `MODFORGE_TTS_BIN`,
`MODFORGE_XWMAENCODE`, `MODFORGE_FACEFX`, `voicediag`, and packaging. Voice files are
loose Skyrim assets, not bytes embedded inside the `.esp`/`.esm`; either run `voicelines`
against the final packaged plugin, or bundle the generated `Sound/` tree with `package --assets`.

## Tests

Run the offline-safe harness with:

```bash
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"
```

Tests that clone vanilla templates or copy vanilla cell/worldspace context are marked
`Category=RequiresSkyrim`; run them with `MODFORGE_SKYRIM_DATA` pointing at the Skyrim
Special Edition `Data` folder. See [`workflows/testing.md`](workflows/testing.md).

**Building lifelike NPCs?** See [`docs/lifelike/`](docs/lifelike/README.md) — distilled recipe + the
two-systems insight (CombatStyle vs AIData) + vanilla FormID reference + diagnostic commands +
gotchas. The engine mechanics behind the generator are in
[`docs/engine-internals.md`](docs/engine-internals.md).

Papyrus prereq (one-time): extract the CK's `Data/Scripts.zip` `Source/Scripts/*` to
`~/.cache/modforge/papyrus/` (or set `MODFORGE_PAPYRUS_BASE`); set
`MODFORGE_PAPYRUS_COMPILER` if the CK isn't at the default Steam path.

**Translation workflow:** `extract` pulls all translatable strings into a reviewable
JSON (each entry has `source` + an empty `target`). An AI (or a human) fills in
`target`. `apply` writes those targets back into a new plugin. The JSON is the
contract — deterministic, diff-able, re-runnable.

### Translatable fields currently covered
`Name` (most records), `Book.BookText`, `Npc.ShortName`, quest objective text, and
native dialogue (`DialogTopic` prompt + spoken `DialogResponse` lines). Easy to extend
— add a slot in `ModForge.Core`'s `Translator.Slots(...)`.

## Status
All three pillars are operational. Generation covers NPCs/items/magic/enchantments/perks,
dialogue/quests/scenes/word walls, AI packages/combat styles, weather/climate/regions,
interior cells and custom worldspaces (heightmap-driven terrain — VHGT/VNML with seam stitching,
BTXT/VTXT texture layers from splatmaps, object placement, plus loadable navmesh), with script
attachment (VMAD), SEQ files and MO2-ready packaging. The voice pipeline can plan and generate
dialogue voice assets from built INFOs when external TTS/xWMA/lip tools are configured; fake-TTS +
xWMA FUZ packaging is structurally verified, and an offline live contract executes sibling
`skyrim-voicegen/voicegen.py` through the production `GenerateWav()` process boundary (using only a
deterministic fake final engine). Real model quality and in-game playback still
need local Skyrim/Proton confirmation. Translation supports inline and
localized (`.STRINGS`) output. The full spec surface is documented in
[`docs/spec/SPEC-index.md`](docs/spec/SPEC-index.md).
