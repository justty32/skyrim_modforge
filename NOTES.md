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
- [x] **It.3 — more record types** (done 2026-05-24): added Spell, Potion(Ingestible),
      Armor (value/weight/armorRating), Faction, Message(description) to spec + Build.
      Verified: sample_spec → 12 top-level records, extract shows all. Also fixed an
      extract cosmetic bug (`TrimStart('I')` was eating the 'I' in "Ingestible"; concrete
      record names have no interface prefix). Still TODO if wanted: MagicEffect, Container,
      Activator, Ammunition, Ingredient — same trivial pattern.
- [x] **It.4 — refs & FormLinks across records** (done 2026-05-24): two-pass build —
      pass 1 creates all records, then one editorId→FormKey table from
      `EnumerateMajorRecords()` resolves forward refs. Demo: NpcSpec.factions (list of
      faction editorIds) → `Npc.Factions` RankPlacement (FormLink<IFaction>, Rank 0).
      sample_spec wires 1 link (MF_Smith → MF_Guild). Build prints a "cross-ref link(s)"
      count. (Round-trip currently trusted from build output; a `dump` command — list
      records + key FormLinks — would let extract-style verify links. Good next helper.)
      More refs are the same pattern: container contents, leveled lists, npc CrimeFaction,
      keywords, npc→class/race/outfit.
- [~] **It.5 — Papyrus hook** (in progress):
  - [x] **5a — compile command** (done 2026-05-24): `compile <script.psc> <outDir>` drives
        the CK's `PapyrusCompiler.exe` under `wine` from C# (Process), parses stdout
        (`Failed on`) + checks the `.pex` exists (exit code is unreliable). Verified:
        examples/scripts/MFDemoQuestScript.psc → valid .pex (magic fa57c0de). Paths via
        env `MODFORGE_PAPYRUS_COMPILER` / `MODFORGE_PAPYRUS_BASE`, defaults to the local CK
        + `~/.cache/modforge/papyrus/Source/Scripts`.
        **PREREQ (one-time, already done on this box):** extracted base sources +
        TESV_Papyrus_Flags.flg from `<CK>/Data/Scripts.zip` (`Source/Scripts/*`, 14301 .psc)
        to `~/.cache/modforge/papyrus/`. (For SKSE functions, add the SKSE .psc to that dir.)
  - [ ] **5b — VMAD attach**: a spec form (e.g. Quest) names a script (+typed properties);
        Build adds `VirtualMachineAdapter` + `ScriptEntry{Name=...}` + `ScriptObjectProperty`
        etc., matching the compiled .pex's Scriptname. (See parent memory for the API.)
  - [ ] **5c — packaging**: place compiled `.pex` under the output mod's `Scripts/` (and
        sources under `Scripts/Source/`); a `build` run could compile+attach+package in one.
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
