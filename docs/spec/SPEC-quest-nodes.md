# Quest-node JSON schema (MVP)

Quest nodes are the semantic hand-off between `gamedata` QUST/DIAL extraction and
follower reaction generation (followers idea §17). Each JSON file describes one
`questId + stage` node; the `next`/`previous` arrays form a graph without requiring
the extractor to embed the entire quest.

The contract is [`schemas/quest-node.schema.json`](../../schemas/quest-node.schema.json)
(JSON Schema draft 2020-12). Required identity and semantic fields are `questId`,
`plugin`, `stage`, `summary`, `major`, and one or more `reactionTags`. `location`
and `npcs` are structured so later passes can preserve names plus optional roles,
while links use stable keys such as `MQ101@20`. `major` marks a high-value moment
for batch generation; tags are intentionally open vocabulary (lower kebab-case)
so extraction can add domain-specific reaction semantics without changing the schema.

Run the offline contract test from the repository root:

```text
python -m pip install jsonschema  # once, if the dev environment does not already provide it
python -m unittest tests/quest_node_schema_test.py
```

## Mechanical import from a plugin

```text
dotnet run --project src/ModForge.Cli -- questnodes Skyrim.esm ./quest-nodes
dotnet run --project src/ModForge.Cli -- questnodes Skyrim.esm ./quest-nodes --strings ./Strings
```

The importer emits one file per QUST stage that has at least one resolvable, non-empty journal log.
Multiple conditional log entries at the same stage are preserved in one summary, separated by a
blank line. Missing/unsafe EditorIDs fall back to `QUST_<FORMID>`. Complete/fail log flags are the
only mechanical `major` signal and yield `quest-complete`/`quest-failed`; when conditional entries
at one stage contain both outcomes, the tag is `conditional-outcome` and each summary paragraph is
prefixed with its outcome. All other nodes start with `unclassified`.

QUST records do not reliably encode narrative branch edges, scene location, or participating NPC
roles. The importer therefore omits `previous`, `next`, `location`, and `npcs` instead of inventing
them. Add those fields—and replace `unclassified`—during the AI semantic pass or human review.

Fixtures cover a linear opening, a two-way escape branch, and branch convergence.
The test also rejects missing identity, invalid stage/plugin/tag/link shapes, and
unknown properties. Cross-file link targets are checked by the test because JSON
Schema intentionally validates each node independently.
