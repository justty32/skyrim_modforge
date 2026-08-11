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

Fixtures cover a linear opening, a two-way escape branch, and branch convergence.
The test also rejects missing identity, invalid stage/plugin/tag/link shapes, and
unknown properties. Cross-file link targets are checked by the test because JSON
Schema intentionally validates each node independently.
