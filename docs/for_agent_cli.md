# Driving ModForge from the CLI (JSON spec → plugin)

The **default** workflow: you write a JSON **spec**, the CLI emits a valid `.esp`/`.esl`
(+ compiled `.pex`). The spec is the contract — you never hand-write plugin bytes or FormIDs.

← index: [for_agent.md](for_agent.md) · spec fields: [SPEC-index.md](spec/SPEC-index.md) · compute the spec in code instead: [for_agent_lib.md](for_agent_lib.md)
· lifelike NPCs: [lifelike/](lifelike/README.md) · engine mechanics: [engine-internals.md](engine-internals.md)

## Your job, in one loop

```
request (NL) ──▶ write spec.json (per SPEC-index.md) ──▶ validate ──(fix on errors)──▶ build|package ──▶ dump (verify) ──▶ report honestly
                                                  ▲___________________________|
```

A complete spec example is `../examples/sample_spec.json`.

## Commands

```bash
cd /home/lorkhan/repo/moddings/skyrim/projects/ModForge
dotnet build src/ModForge.Cli/ModForge.Cli.csproj -v q        # build once (and after any code change)
R="dotnet run --project src/ModForge.Cli --no-build --"       # then drive it fast

$R validate <spec.json>                      # ALWAYS run first; exits non-zero + lists problems
$R build    <spec.json> <out.esp>            # spec -> plugin (records, dialogue, FormLinks, VMAD)
$R package  <spec.json> <outModDir>          # build + compile each script `source` -> MO2-ready folder
$R dump     <plugin.esp>                     # read back: records, names, npc race/class/outfit/factions, weapon/armor stats, effects, cells/placements, keywords, scripts, dialogue, objectives, masters
$R find     <plugin.esp> <query> [type]      # search a master (e.g. Skyrim.esm) -> "Skyrim.esm:0xFORMID  Type  EditorID"  (query may be a 0xFORMID to reverse-resolve: "what record IS this?")
$R catalog build <out.db> <plugin> [plugin...] # replace/create an offline SQLite/FTS index of generic records
$R catalog query <db> <query> [--type Npc] [--plugin MyMod.esp] [--limit 50] [--json] # FTS name/EditorID search
$R catalog get <db> <Plugin.esp:0xFORMID> [--plugin MyPatch.esp] [--json] # exact identity lookup
$R catalog sources <db> [--json] # indexed files, hashes, localization and counts
$R compile  <script.psc> <outDir>            # .psc -> .pex via the CK PapyrusCompiler under Wine
$R extract  <plugin.esp> <strings.json>      # pull translatable strings -> JSON (source/target)
$R apply    <plugin.esp> <strings.json> <out.esp>     # write targets back (Latin scripts / inline)
$R applyloc <plugin.esp> <strings.json> <outModDir>   # CJK: Localized UTF-8 <plugin>_chinese.STRINGS
$R gen      <out.esp>                         # demo plugin (sanity check the toolchain)
$R smtree   <Skyrim.esm>                      # list Story Manager event roots (find an event root FormID)
$R navdiag  <plugin.esp>                      # every NAVM in the plugin + a BYTE-DIFF of each overridden mesh's NVNM against its master (IDENTICAL / DIFF). Run it on any plugin using navmeshOverrides[]
```

Run `$R` with no arguments for the full command list — there is a `*diag` probe for most record
families (`questdiag`, `packagediag`, `landdiag`, `navdiag`, …) that prints one record's fields so
you can compare what you generated against a vanilla record of the same kind.

`--no-build` requires a prior `dotnet build`; drop it (slower) if unsure.

## Offline catalog for agent lookups

`catalog` is a compact, generic record index for a story system or agent that needs to answer
"which FormKey is this wolf/bread/NPC?" without loading Skyrim. It accepts arbitrary `.esm`,
`.esp`, or `.esl` inputs; `Skyrim.esm` is optional, not a prerequisite.

```bash
$R catalog build ./catalog.db ./MyStoryMod.esp ./AnotherMod.esl
$R catalog query ./catalog.db forged --type Npc --plugin MyStoryMod.esp
$R catalog get ./catalog.db MyStoryMod.esp:0x000802 --json
$R catalog sources ./catalog.db --json
```

The `records` table stores the resolver-ready `form_key` (`Plugin.esp:0x000800`), FormKey
plugin, Mutagen record type, EditorID, and display name. Its FTS5 index covers `name` and
`editor_id`; `--type` and `--plugin` are exact case-insensitive filters (the plugin is the input
source plugin). `sources` records the absolute source path, SHA-256, localization flag, and record
count, so a result has clear provenance. Re-running `catalog build` replaces the destination only
after the new database is complete, so it never appends duplicate records.

`catalog get` bypasses FTS and matches the resolver-ready FormKey exactly (case-insensitive). It
returns every indexed occurrence, so when several source plugins contain an override you can see
each provenance row or narrow it with `--plugin`. `catalog query`, `get`, and `sources` accept
`--json`; JSON output contains no TSV header/summary and is the stable choice for another agent or
program to consume.

This is the stable generic layer, not a dump of every record's schema. Future record-specific
catalog tables can key off `records.id` without changing agent-facing identity/search fields.

## Referencing vanilla forms (race/class/outfit/keywords/factions)

Some spec fields are **refs** — they take an in-spec `editorId` OR an external vanilla form
`"<master>:0xFORMID"` (e.g. `"Skyrim.esm:0x013746"` = NordRace). The master is auto-added.
To find a vanilla FormID, search the game master:

```bash
SKYRIM_ESM="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data/Skyrim.esm"
$R find "$SKYRIM_ESM" nordrace Race        # -> Skyrim.esm:0x013746  Race  NordRace
$R find "$SKYRIM_ESM" blacksmith Class     # -> Skyrim.esm:0x013257  Class VendorBlacksmith
$R find "$SKYRIM_ESM" armorclothing Keyword
$R find "$SKYRIM_ESM" restorehealth MagicEffect  # -> Skyrim.esm:0x03EB15  AlchRestoreHealth (for a potion `effects`)
$R find "$SKYRIM_ESM" banneredmare Cell    # -> Skyrim.esm:0x01605E  WhiterunBanneredMare (interior `placement` cell)
$R find "$SKYRIM_ESM" tamriel Worldspace   # -> Skyrim.esm:0x00003C  Tamriel (exterior `placement` worldspace)
$R find "$SKYRIM_ESM" 0x000D4B52           # reverse: a 0xFORMID -> its record type (e.g. confirm a placement base is a STAT, not a REFR)
```
Always run `find` to get the real FormID — **never guess one**. Search is by EditorID
(descriptive, e.g. `NordRace`); localized display names aren't resolved headless. A standing
NPC needs at least `race` + `class` to act like a real actor; `outfit` clothes it.

## Referencing a MOD — it becomes a master (install requirement)

The same syntax takes any plugin: `"PROTEUS.esp:0x08073D"`. That makes **PROTEUS.esp a master of
your plugin** — and **Skyrim silently refuses to load a plugin whose masters are missing**: no error,
no log line, the records just aren't there in-game. This is not a bug and ModForge does not filter it
(a `sc capp` player clone that dropped every mod-given spell would not be *you* any more), but you
have to know it happened. So `build` **tells you**, and says which spec field is responsible:

```
7 non-vanilla master(s) — the plugin will NOT load for anyone missing them (Skyrim drops it silently):
  ImGladYoureHere.esp  (3 link(s))
      ← capturedNpcs[0].spells[10] = ImGladYoureHere.esp:0x18D2A1
      … +6 more
  PROTEUS.esp  (1 link(s))
      ← capturedNpcs[0].spells[17] = PROTEUS.esp:0x08073D
wrote MFCapHatak.requires.txt (the install requirements, with the spec field behind each one)
```

- **Vanilla masters** (`Skyrim.esm`, `Update.esm`, `Dawnguard.esm`, `HearthFires.esm`,
  `Dragonborn.esm`) are never listed — every install has them.
- **Creation Club** (`ccBGSSSE001-Fish.esm`, `_ResourcePack.esl`, …) **is** listed: it is owned per
  account, so a player who lacks it is just as stuck.
- **`<plugin>.requires.txt`** is written next to the .esp (deleted when nothing non-vanilla is left).
  Keep it with the plugin: it is the only record of what the build depends on. Under each master are
  the spec fields naming a form it links — remove **all** of them to drop that dependency.
- `package` prints the same summary but writes no sidecar (its output folder is the shipped mod).

Nothing about the .esp changes — this is pure visibility. If the plugin must be portable, don't
reference mod content: hand-write the spec against vanilla forms.

### `requires[]` — declare them, and build enforces it

Reporting doesn't stop **drift**: a mod is uninstalled, a capture is retaken, a line is deleted — and
the plugin's master list quietly changes, which is exactly the silent-no-load failure above. So a spec
can **declare** what it needs, and `build` refuses to write a plugin that disagrees:

```json
"requires": [
  "XPMSE.esp",
  { "plugin": "PROTEUS.esp", "version": "3.4+", "reason": "the captured player's spells" },
  { "name": "PapyrusUtil SE", "reason": "storageWrites (SKSE plugin — has no .esp)" }
]
```

- **linked but not declared → ERROR, the .esp is not written** (the message names the spec field, so
  you can delete that line or declare the master);
- **declared but never linked → warning** (stale line; a runtime-only mod with no master goes under
  `name`, which is documentation-only and never checked);
- **no `requires` section at all → nothing is checked** (every older spec is unaffected — writing the
  section is how you opt in). `"requires": []` opts in too: it means *vanilla-only*, so any mod ref fails.

**`build spec.json out.esp --sync-requires`** writes the real master set back into the spec's
`requires[]` (keeps your `reason`/`version`/`url`, drops stale entries, creates the section if absent).
Use it after a capture — it turns a dependency change into a reviewable line in the spec diff.

⚠️ **There is no version check and there cannot be one.** An `.esp` carries no mod version: `TES4`/`HEDR`
"version" is the file *format* version (1.71 for PROTEUS 3.4 and for a 2-record test plugin alike), and
`CNAM`/`SNAM` are free text (usually `DEFAULT`/empty). Only the mod *manager* knows the version (MO2
`meta.ini`, from Nexus). `version` in `requires[]` is a **label for humans**, printed and marked
unverified in `<plugin>.requires.txt`.

## Generate-content workflow

1. Read `SPEC-index.md` for the exact fields. Write `spec.json` (camelCase; property names are
   matched case-insensitively). For race/class/outfit/keywords/vanilla factions, `find` the
   FormID first and use the `"<master>:0xFORMID"` ref form.
2. `validate spec.json`. **If it reports problems, FIX the spec and re-validate** — do not
   build an invalid spec. It catches: empty/duplicate `editorId`, dialogue→unknown quest/npc,
   script→unknown target, object-property→unknown record, bad property type, **unknown spec
   fields** (typo guard — recursively checks every JSON key against the C# spec type; skips
   `_*` / `//*` comment keys).
3. `package spec.json OutDir` (or `build spec.json out.esp` for just the plugin).
4. `dump OutDir/<pluginName>` and **check the output matches the request** (names, faction
   membership, attached scripts + property counts, dialogue prompts, quest objectives).
5. Report what you produced and, honestly, what is structural-only (see Limits).

## Translate workflow

1. `extract some.esp strings.json` → each entry has `source` + empty `target`.
2. Fill every `target` with your translation (edit the JSON).
3. **Chinese (or any CJK):** `applyloc some.esp strings.json OutDir` → `OutDir/<plugin>.esp`
   + `OutDir/Strings/<plugin>_chinese.STRINGS` (UTF-8, lowercase suffix — verified against
   the official CHS mod). **Latin scripts:** `apply some.esp strings.json out.esp` (inline).
4. Verify: `dump` the result, or decode the `_chinese.STRINGS` (UTF-8) to confirm the text.

## Environment prerequisites

- **.NET 8 or 10 SDK** (`dotnet`). NuGet restores `Mutagen.Bethesda.Skyrim` on first build.
- **Game `Data` folder** — only for placing into a **vanilla** cell (it reads the master to
  override the cell). Defaults to the Steam path; override with `MODFORGE_SKYRIM_DATA`.
- **Papyrus** (`compile`, and `package` when a script has a `source`): needs `wine` + the
  Creation Kit's `PapyrusCompiler.exe` + the vanilla base script sources. Defaults assume the
  local CK Steam install and `~/.cache/modforge/papyrus/Source/Scripts`. Override with env:
  - `MODFORGE_PAPYRUS_COMPILER` = path to `PapyrusCompiler.exe`
  - `MODFORGE_PAPYRUS_BASE` = dir holding the base `.psc` + `TESV_Papyrus_Flags.flg`
  - One-time setup: `unzip <CK>/Data/Scripts.zip "Source/Scripts/*" -d ~/.cache/modforge/papyrus/`
    (≈14k `.psc`). Add SKSE `.psc` to that dir if a script uses SKSE functions.

## Gotchas (these will bite you)

- **`editorId`** is your in-spec reference key — non-empty, unique across the whole spec. It
  is NOT a FormID; Mutagen assigns FormIDs/masters. Records reference each other by `editorId`.
- A `dialogue` needs a real `questEditorId` (a quest in the spec); `speakerNpcEditorId` (if set)
  must be an npc. A `script` needs its `targetEditorId` to exist; an `object` property needs its
  `objectEditorId` to exist. `validate` enforces all of this.
- A `script`'s `scriptName` must equal the compiled `.pex`'s `Scriptname`, and the `.psc`
  filename must match the `Scriptname` too.
- **ESL** (`esl: true`, default): ≤ 2048 new records (FormIDs 0x800–0xFFF); a clear error at write if exceeded.
- **CJK**: only `applyloc` produces game-readable Chinese (Localized UTF-8). Plain inline
  strings turn Chinese into `?` (the engine's cp1252). Don't use `apply`/`build` for CJK text.
- The Papyrus compiler returns exit code 0 even on failure; the tool already scrapes stdout —
  if you ever call `wine PapyrusCompiler.exe` yourself, do the same, and check the `.pex` exists.

## Limits — be honest, do not over-claim

ModForge writes **structurally valid** records, which is NOT the same as **in-game functional**,
and you cannot confirm in-game behaviour from here (that needs a Proton/Skyrim launch). The full
breakdown of what IS vs ISN'T functional — and the honest reporting rule — lives in the index:
**[for_agent.md → Limits](for_agent.md#limits--be-honest-do-not-over-claim)**. Read it before
reporting results.
