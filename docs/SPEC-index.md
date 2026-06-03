# ModForge spec — index

The spec is a JSON file → `.esp` plugin. Choose a topic:

| File | Contents |
|------|----------|
| [SPEC-intro](SPEC-intro.md) | Cross-references & IDs, top-level shape, full record-type table |
| [SPEC-magic](SPEC-magic.md) | Gameplay stats, spell/potion effects, custom MGEFs, enchantments (ENCH) |
| [SPEC-dialogue-quests](SPEC-dialogue-quests.md) | Classes, dialogue, banter, scenes, CTDA conditions, quest stages, Papyrus scripts |
| [SPEC-world](SPEC-world.md) | Cells & placements, worldspaces & regions, leveled lists, encounter zones, vendors |
| [SPEC-items](SPEC-items.md) | Recipes (COBJ), perks, external assets (meshes/sounds), texture sets (TXST) |
| [SPEC-packages](SPEC-packages.md) | AI packages (Sandbox/Travel/UseMagic/Follow/Sleep/Patrol/Escort), weathers & climates |
| [SPEC-workflow](SPEC-workflow.md) | CLI workflow (`validate` / `build` / `package`) + not-yet-covered features |

Quick CLI reference:
```bash
dotnet run --project src/ModForge.Cli -- validate myspec.json
dotnet run --project src/ModForge.Cli -- build    myspec.json out.esp
dotnet run --project src/ModForge.Cli -- package  myspec.json OutModDir
```

See also: [lifelike hub](lifelike/README.md) — NPC recipes, cookbook, gotchas, formid reference.
