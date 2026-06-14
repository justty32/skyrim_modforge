# ModForge spec — index

The spec is a JSON file → `.esp` plugin. Choose a topic:

| File | Contents |
|------|----------|
| [SPEC-intro](SPEC-intro.md) | Cross-references & IDs, top-level shape, full record-type table |
| [SPEC-magic](SPEC-magic.md) | Gameplay stats, spell/potion effects, custom MGEFs, enchantments (ENCH) |
| [SPEC-dialogue](SPEC-dialogue.md) | Classes, dialogue, banter, scenes (SCEN), CTDA conditions |
| [SPEC-quests](SPEC-quests.md) | Quest stages & objectives, Story Manager event quests, Papyrus scripts |
| [SPEC-identities](SPEC-identities.md) | Lightweight class/identity system (book→faction+ability+greeting+merchant toggle) |
| [SPEC-world](SPEC-world.md) | Cells & placements, map markers, custom lights & lighting (LGTM/IMGS/DALC) |
| [SPEC-worldspaces](SPEC-worldspaces.md) | Worldspaces & regions, area music, leveled lists & containers, formLists, encounter zones, vendors |
| [SPEC-items](SPEC-items.md) | Recipes (COBJ), perks, external assets (meshes/sounds), texture sets (TXST) |
| [SPEC-packages](SPEC-packages.md) | AI packages (Sandbox/Travel/UseMagic/Follow/Sleep/Patrol/Escort), weathers & climates |
| [SPEC-workflow](SPEC-workflow.md) | CLI workflow (`validate` / `build` / `package`), voice cloning pipeline (`voicelines` / `extract-voices`) + not-yet-covered features |
| [SPEC-refs](SPEC-refs.md) | `$ref` / `$env` includes & parameterization (named preset library, file/pointer/same-doc refs, env vars) |

Quick CLI reference:
```bash
dotnet run --project src/ModForge.Cli -- validate myspec.json
dotnet run --project src/ModForge.Cli -- build    myspec.json out.esp
dotnet run --project src/ModForge.Cli -- voicediag myspec.json out.esp
dotnet run --project src/ModForge.Cli -- voicelines myspec.json out.esp --plan
dotnet run --project src/ModForge.Cli -- package  myspec.json OutModDir
```

See also: [lifelike hub](../lifelike/README.md) — NPC recipes, cookbook, gotchas, formid reference.
