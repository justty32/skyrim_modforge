# ModForge documentation

Start here when you need to use ModForge. These guides describe the supported interface; design proposals and work plans live under [`../workflows/`](../workflows/README.md).

| Need | Read |
|---|---|
| Turn a request into a plugin | [for_agent.md](for_agent.md) |
| Drive the JSON CLI | [for_agent_cli.md](for_agent_cli.md) |
| Use `ModForge.Core` from C# | [for_agent_lib.md](for_agent_lib.md) |
| Find every JSON spec field | [spec/SPEC-index.md](spec/SPEC-index.md) |
| Build more lifelike NPCs | [lifelike/README.md](lifelike/README.md) |
| Understand generator mechanics | [engine-internals.md](engine-internals.md) |
| Package external assets | [external_assets.md](external_assets.md) |
| Extract local Skyrim reference data | [local-skyrim-extraction.md](local-skyrim-extraction.md) |


## CLI command reference

Run commands as `dotnet run --project src/ModForge.Cli -- <command> ...`. The complete syntax and examples stay in [for_agent_cli.md](for_agent_cli.md); the command groups are:

| Work | Commands |
|---|---|
| Build and package | `validate`, `build`, `package`, `compile`, `gen` |
| Inspect plugins and game data | `dump`, `find`, `catalog build/query/get/sources`, `gamedata`, `questnodes`, and the `*diag` commands |
| Translate plugins | `extract`, `apply`, `applyloc` |
| Create dialogue voice assets | `voicediag`, `voicelines`, `extract-voices`; setup and limits are in [SPEC-workflow.md](spec/SPEC-workflow.md#voice-tts-voice-cloning--fuz) |

Traditional Chinese source documents are in [`zh-TW/`](zh-TW/README.md). Its browser bundle is generated from that Markdown; do not edit `zh-TW/html/` by hand.
