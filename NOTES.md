# ModForge — working notes (autonomous-loop anchor)

> Scratch/handoff file. Each loop iteration: read this, do the next unchecked item,
> update it, commit. Build/test commands at the bottom.

## Where we are (2026-05-24)
- New standalone repo (C#/.NET/Mutagen), separate from the SKSE `my_skyrim_plugin_1` repo.
- Foundation proven on this Linux box: Mutagen generates valid `.esp`/`.esl`; Papyrus
  compiles via Wine; Mutagen VMAD bridges scripts↔forms (see the parent repo's memory
  `project_authoring_toolchain_roadmap`).
- **Translate pipeline DONE incl. CJK** (2026-05-24): `extract` → fill JSON `target` →
  `apply` (Latin/inline) **or** `applyloc` (CJK). CJK was unblocked by the user's official
  CHS mod: inspection showed Simplified-Chinese SSE uses **Localized `<plugin>_chinese.STRINGS`
  in UTF-8** (NOT GBK). `applyloc` writes exactly that: sets `TranslatedString.DefaultLanguage
  = Language.Chinese`, `UsingLocalization = true`, a `StringsWriter` with a UTF-8
  `IMutagenEncodingProvider`, then lowercases `_Chinese`→`_chinese` (Mutagen capitalizes it;
  case matters on Linux/Proton). Verified: CnDemo → Strings/CnDemo_chinese.STRINGS with valid
  UTF-8 Chinese. Official CHS mod reference extracted at /tmp/chs-mod (Strings/*_chinese.*).

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
  - [x] **5b — VMAD attach** (done 2026-05-24): spec.scripts[] attaches a script (by
        Scriptname) to any record by editorId, with typed properties (int/float/bool/
        string/object; object resolves an editorId→FormLink). The VMAD setter isn't on
        IHaveVirtualMachineAdapter (get-only) and its type varies (Quest→QuestAdapter,
        else VirtualMachineAdapter), so Build REFLECTS the concrete `VirtualMachineAdapter`
        property + `System.Activator.CreateInstance`s the right adapter, then adds a
        ScriptEntry to `.Scripts`. Properties get `ScriptProperty.Flag.Edited`. Verified:
        sample attaches MFDemoQuestScript to MF_Q1 (int GreetingCount=3 + object PlayerRef
        →MF_Smith); `strings` confirms script+prop names in the esp; re-read OK.
  - [x] **5c — packaging** (done 2026-05-24): `package <spec.json> <outModDir>` = build esp
        + compile each script `source` + lay out a MO2/Vortex-ready folder
        (`<PluginName>` at root, `Scripts/*.pex`, `Scripts/Source/*.psc`). ScriptAttachSpec
        gained an optional `source` (.psc path rel. to spec). Verified: sample_spec →
        SampleMod/ with SampleMod.esp + Scripts/MFDemoQuestScript.pex + Source/.psc.
        **It.5 COMPLETE — full spec→packaged-mod pipeline works on Linux.**
- [~] **It.6 — NL→spec layer** (the "AI agent" front):
  - [x] **6a — `validate <spec.json>`** (done 2026-05-24): semantic guardrail — editorId
        presence/uniqueness + referential integrity (dialogue→quest/npc, npc→faction,
        script→target, object-prop→record, property types). Exit non-zero on any problem
        so an NL→spec front can self-correct. Verified: good sample passes; a broken spec
        surfaces all 4 seeded errors.
  - [x] **6b — SPEC.md + spec.schema.json** (done 2026-05-24): `SPEC.md` full field
        reference + NL→spec workflow; `examples/spec.schema.json` (draft 2020-12, enum on
        property type, object-prop conditional-required); README CLI list refreshed to all
        7 commands. Both JSON files parse-validated.
  - [ ] **6c — live LLM hook** (BLOCKED on user): a `describe "<NL>"` command calling an
        LLM API to emit a spec. Needs the user's API key / provider preference. Until then
        the NL→spec step is done by Claude in-session (produce spec.json → validate → package).

- [ ] **It.7 — gameplay-complete fields** (next autonomous track, since 6c is blocked):
      deepen records so generated content actually works in-game, not just structurally:
      spell/potion magic effects, armor armorType/keywords/biped slots, npc race/class/
      outfit (an npc needs these to be a functional actor), container contents, leveled
      lists. Same pattern: add fields to the spec class + Build, verify via build/extract/
      strings. Also worth: a `dump <esp>` command (list records + key FormLinks + scripts)
      as a round-trip verification helper.

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
