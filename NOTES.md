# ModForge — working notes (autonomous-loop anchor)

> Scratch/handoff file. Each loop iteration: read this, do the next unchecked item,
> update it, commit. Build/test commands at the bottom.

## Where we are (2026-05-24)
- New standalone repo (C#/.NET/Mutagen), separate from the SKSE `my_skyrim_plugin_1` repo.
- Foundation proven on this Linux box: Mutagen generates valid `.esp`/`.esl`; Papyrus
  compiles via Wine; Mutagen VMAD bridges scripts↔forms (see the parent repo's memory
  `project_authoring_toolchain_roadmap`).
- **Translate pipeline DONE** (`gen`/`extract`/`apply`, 8/8 round-trip incl. dialogue).
  Latin targets work; **CJK output blocked on string encoding** — PAUSED pending the
  user's official Chinese localization mod as the encoding reference. Do NOT pursue CJK
  encoding until that arrives.

## Current focus: the ESP GENERATOR (spec → plugin)
Generalize the hardcoded `gen` demo into a data-driven `build <spec.json> <out.esp>`.
Layered design: structured spec (JSON IR, human/AI-reviewable) → Mutagen → plugin.
(The NL→spec LLM layer comes later; the spec IS the contract.)

### Iterations
- [x] **It.1 — basic records**: spec for MiscItem / Book / Weapon / Npc; `build` command;
      sample spec; round-trip test (build → extract verifies names/text).
- [x] **It.2 — quest + dialogue in spec** (done 2026-05-24): spec now has `quests`
      (+objectives) and `dialogue` (topic prompt + responses, referencing quest &
      speaker NPC by editorId; GetIsID condition). Verified: sample_spec → 7 records +
      1 dialogue topic; extract shows quest name/objective + prompt + response line.
      (Gotcha: `DialogResponse.ResponseNumber` is `byte`.)
- [ ] **It.3 — more record types**: Spell/MagicEffect, Ingestible(potion), Armor, Container,
      Activator, Message, Faction. Add each to the spec model + `Build`.
- [ ] **It.4 — refs & FormLinks across records**: let spec entries reference each other by
      editorId (e.g. npc→faction, leveled lists, container contents); resolve in two passes.
- [ ] **It.5 — Papyrus hook**: spec entry can name a `.psc` to compile (wine PapyrusCompiler)
      + attach via VMAD ScriptEntry/properties. (Compiler at the CK install; parse stdout
      for success, NOT exit code.)
- [ ] **It.6 — NL→spec layer**: prompt → structured spec (the "AI agent" front).

## Build / test
```
cd /home/lorkhan/repo/ModForge
export PATH="$PATH"   # .NET 8/10 already on PATH
dotnet build src/ModForge.Cli/ModForge.Cli.csproj -v q
# run (no rebuild): dotnet run --project src/ModForge.Cli --no-build -- <cmd> ...
dotnet run --project src/ModForge.Cli --no-build -- build examples/sample_spec.json /tmp/mf-test/Built.esp
dotnet run --project src/ModForge.Cli --no-build -- extract /tmp/mf-test/Built.esp /tmp/mf-test/built.json
```
Mutagen 0.53.1 gotchas: `AddNew()` needs `using Mutagen.Bethesda;`; write with
`BinaryWriteParameters { ModKey = ModKeyOption.NoCheck }` when out-filename ≠ ModKey;
`DialogBranch.CategoryType` = {Player, Command}; API discovery via
`ilspycmd -t <Type> ~/.nuget/packages/mutagen.bethesda.*/0.53.1/lib/net9.0/*.dll`.
