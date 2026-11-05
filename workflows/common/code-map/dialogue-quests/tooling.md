# CODE_MAP.dialogue-quests — Quest-node schema・SM 離線診斷

← [CODE_MAP.dialogue-quests](../CODE_MAP.dialogue-quests.md)

## Quest-node extraction schema

| Artifact | Responsibility |
|---|---|
| `schemas/quest-node.schema.json` | Draft 2020-12 semantic node contract for quest/stage, location, NPCs, graph links, major moments, and reaction tags |
| `src/ModForge.Core/QuestNodes.cs` | QUST stage-log → deterministic schema DTO extraction; terminal flag tags, safe quest identity, no invented graph/location/NPC semantics |
| `src/ModForge.Cli/Commands/QuestNodeCmd.cs` | `questnodes` command + camelCase JSON directory writer, generated-file manifest cleanup, and pre-write symlink/reparse rejection |
| `tests/ModForge.Core.Tests/Catalog/QuestNodeExtractionTests.cs` | Mechanical mapping, schema-facing JSON shape, fallback identity, duplicate/stale-file safety, and file/output-directory/parent junction guards |
| `tests/ModForge.Core.Tests/Catalog/GameDataInputTests.cs` | Localized quest-node `--strings` and plugin→BSA→JSON roundtrips, metadata-driven BSA refresh, plus per-source cache-key behavior |
| `tests/quest_node_schema_test.py` | Offline fixture validation, cross-file link checks, and invalid-shape cases |
| `docs/spec/SPEC-quest-nodes.md` | Agent-facing usage and design notes |

---

## Story Manager offline diagnostics

| Artifact | Responsibility |
|---|---|
| `src/ModForge.Core/StoryManagerDiagnostics.cs` | Deterministic local SMBN/SMQN parent + sibling graph checks, routed-quest/alias duplicate checks, and local LVLN alias-shape checks; external links stay unresolved |
| `src/ModForge.Cli/Diagnostics/Diagnostics.StoryManager.cs` | `smtree`/`smsub` probes plus `smcheck <plugin>` output and exit-code facade |
| `tests/ModForge.Core.Tests/Build/StoryManagerDiagnosticsTests.cs` | Clean generated/binary-roundtrip trees and synthetic orphan/cycle/duplicate/LVLN failures |
