# ModForge

ModForge is an AI-driven Skyrim mod **authoring** toolchain. An agent writes a reviewable JSON spec; ModForge uses Mutagen.Bethesda to turn it into a valid `.esp` or `.esl`. Generation is pure .NET on Windows or Linux and does not need the Creation Kit.

> The agent emits intent and text. ModForge emits plugin bytes, FormIDs, masters, and record sizes deterministically. It has no in-tool LLM API.

## Pillars (all three operational)

1. **Generate** — JSON spec → `.esp`/`.esl`: NPCs, items, magic, perks, quests, dialogue, scenes, AI packages, weather, interior cells, and worldspaces. `voicelines` can add dialogue voice assets after the build.
2. **Translate** — extract plugin strings to JSON, fill translations, then write them inline or as a localized plugin plus `.STRINGS` files.
3. **Papyrus** — compile agent-written `.psc` with the native compiler or CK compiler under Wine, then attach it through VMAD.

## Two projects

- **`src/ModForge.Core`** is the reusable C# engine: validate and build a `ModSpec`, translate strings, compile Papyrus, and read or write plugins. Its supported public API is in [`docs/for_agent_lib.md`](docs/for_agent_lib.md).
- **`src/ModForge.Cli`** is the command-line wrapper for JSON input, plugin output, translation, packaging, diagnostics, and game-data extraction. Use the [CLI guide](docs/for_agent_cli.md) for commands and examples.

## CLI (src/ModForge.Cli)

The usual loop is `validate` → `build` or `package` → `dump`. Start with the [agent guide](docs/for_agent.md), then choose [CLI + JSON](docs/for_agent_cli.md) or the [library API](docs/for_agent_lib.md). Run the CLI without arguments for the complete command list and diagnostic commands.

The JSON field manual is [`docs/spec/SPEC-index.md`](docs/spec/SPEC-index.md); the schema is [`examples/spec.schema.json`](examples/spec.schema.json), and [`examples/sample_spec.json`](examples/sample_spec.json) is a working example. For local Manjaro and Steam Proton master extraction, see [`docs/local-skyrim-extraction.md`](docs/local-skyrim-extraction.md).

Voice work uses [`docs/spec/SPEC-workflow.md`](docs/spec/SPEC-workflow.md#voice-tts-voice-cloning--fuz): `voiceTemplates[]`, `npcs[].voiceTemplate`, `voiceLine`, `MODFORGE_TTS_BIN`, `MODFORGE_XWMAENCODE`, `MODFORGE_FACEFX`, `voicediag`, and packaging. Voice files are loose Skyrim assets, not bytes inside a plugin.

## Tests

Run the offline-safe suite:

```bash
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"
```

Tests needing vanilla templates or cell/worldspace context use `Category=RequiresSkyrim` and require `MODFORGE_SKYRIM_DATA`. See [`workflows/testing.md`](workflows/testing.md). Papyrus needs extracted CK `Data/Scripts.zip` sources under `~/.cache/modforge/papyrus/`, or `MODFORGE_PAPYRUS_BASE`; set `MODFORGE_PAPYRUS_COMPILER` when CK is elsewhere.

## Translatable fields currently covered

`Name` for most records, `Book.BookText`, `Npc.ShortName`, quest objectives, and native dialogue prompts and responses. The JSON translation contract is reviewable, deterministic, diffable, and repeatable; see [the agent guide](docs/for_agent.md).

## Status

All three pillars work. The supported spec surface, limits, and in-game verification boundaries live in [the documentation hub](docs/README.md), [SPEC-index](docs/spec/SPEC-index.md), and [the agent guide](docs/for_agent.md). For lifelike NPC recipes, use [`docs/lifelike/`](docs/lifelike/README.md); generator mechanics are in [`docs/engine-internals.md`](docs/engine-internals.md).
