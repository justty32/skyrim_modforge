# ModForge

An AI-driven Skyrim mod **authoring** toolchain. You describe content (or hand it a
structured spec); ModForge produces a valid `.esp`/`.esl` — and, as a byproduct,
translates the text inside existing plugins. Built on
[Mutagen.Bethesda](https://github.com/Mutagen-Modding/Mutagen) (C#/.NET), runs on
Linux (no Creation Kit, no Windows needed for generation).

> Design principle: **the LLM only emits intent / text; a deterministic tool
> (Mutagen) emits the bytes.** Plugin records, FormIDs, masters and record sizes are
> never hand-written — Mutagen guarantees validity.

## Pillars (proven feasible on Linux, 2026-05-24)

1. **Generate** — spec/NL → `.esp`/`.esl` (NPCs, items, weapons, quests, dialogue…).
2. **Translate** (this first milestone) — read an existing plugin → extract every
   translatable string to JSON → an AI fills in translations → write a localized plugin.
3. **Papyrus** (next) — AI writes `.psc` → compile via the Creation Kit's
   `PapyrusCompiler.exe` (under Wine) → attach the `.pex` to forms via Mutagen's VMAD.

## CLI (src/ModForge.Cli)

```
dotnet run --project src/ModForge.Cli -- gen      <out.esp>
dotnet run --project src/ModForge.Cli -- extract  <in.esp>  <strings.json>
dotnet run --project src/ModForge.Cli -- apply    <in.esp>  <strings.json>  <out.esp>
```

**Translation workflow:** `extract` pulls all translatable strings into a reviewable
JSON (each entry has `source` + an empty `target`). An AI (or a human) fills in
`target`. `apply` writes those targets back into a new plugin. The JSON is the
contract — deterministic, diff-able, re-runnable.

### Translatable fields currently covered
`Name` (most records), `Book.BookText`, `Npc.ShortName`, quest objective text, and
native dialogue (`DialogTopic` prompt + spoken `DialogResponse` lines). Easy to extend
— add a slot in `Program.cs`'s `Slots(...)`.

## Status
Phase 1: the translate pipeline (extract/apply) + a `gen` demo plugin. Generation of
arbitrary content from a structured spec, and the Papyrus pipeline, build on top.
