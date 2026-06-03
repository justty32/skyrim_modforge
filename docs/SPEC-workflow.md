# ModForge spec — workflow & gaps

← [index](SPEC-index.md)

## Workflow

```bash
dotnet run --project src/ModForge.Cli -- validate myspec.json          # check first
dotnet run --project src/ModForge.Cli -- build    myspec.json out.esp   # just the plugin
dotnet run --project src/ModForge.Cli -- package  myspec.json OutModDir # esp + compiled scripts -> MO2 folder
```
`package` lays out `OutModDir/<pluginName>` + `Scripts/*.pex` + `Scripts/Source/*.psc`.

**NL → spec:** describe what you want to an AI agent (Claude Code); the agent emits a spec
conforming to this doc / `../examples/spec.schema.json` (per `for_agent.md`), runs `validate`
(self-correcting on problems), then `build`/`package`. This agent-driven loop **is** the
NL→spec layer — there is no in-tool LLM API (the once-planned `describe` command is dropped),
so there's no API key/provider to configure.

## Not yet covered (extend in `ModForge.Core` `Generator.Build` + a spec class)
World placement now covers new interior cells, vanilla interior cells, **and exterior/worldspace
cells** (via `worldspace` + world position), and ModForge can now **create** new worldspaces (WRLD)
+ regions (REGN) — see [SPEC-world](SPEC-world.md) (record layer only; terrain/LOD/navmesh stay
CK-side). Refs (in-spec or `<master>:0xFORMID`) and the `find` command are the building blocks for
the external ones. Remaining gaps are long-tail record types/fields and the CK-side terrain/LOD/
navmesh authoring — the record-side pattern is the same: add a spec class + a loop in `Build`.
