# FOR_AGENT.md — operating ModForge as an AI agent

You (an AI agent) drive ModForge to turn a content request into a Skyrim plugin, and to
translate plugin text. ModForge is the deterministic half; **you are the NL→spec half.**
Read this once, then follow the workflow. Field reference for the spec is in `SPEC.md`;
a complete example is `examples/sample_spec.json`.

## Your job, in one loop

```
request (NL) ──▶ write spec.json (per SPEC.md) ──▶ validate ──(fix on errors)──▶ build|package ──▶ dump (verify) ──▶ report honestly
                                                  ▲___________________________|
```

You never hand-write plugin bytes or FormIDs — you emit a JSON **spec**; the tool emits a
valid `.esp`/`.esl` (+ compiled `.pex`). The spec is the contract.

## Commands

```bash
cd /home/lorkhan/repo/ModForge
dotnet build src/ModForge.Cli/ModForge.Cli.csproj -v q        # build once (and after any code change)
R="dotnet run --project src/ModForge.Cli --no-build --"       # then drive it fast

$R validate <spec.json>                      # ALWAYS run first; exits non-zero + lists problems
$R build    <spec.json> <out.esp>            # spec -> plugin (records, dialogue, FormLinks, VMAD)
$R package  <spec.json> <outModDir>          # build + compile each script `source` -> MO2-ready folder
$R dump     <plugin.esp>                     # read back: records, names, npc factions, scripts, dialogue, objectives
$R compile  <script.psc> <outDir>            # .psc -> .pex via the CK PapyrusCompiler under Wine
$R extract  <plugin.esp> <strings.json>      # pull translatable strings -> JSON (source/target)
$R apply    <plugin.esp> <strings.json> <out.esp>     # write targets back (Latin scripts / inline)
$R applyloc <plugin.esp> <strings.json> <outModDir>   # CJK: Localized UTF-8 <plugin>_chinese.STRINGS
$R gen      <out.esp>                         # demo plugin (sanity check the toolchain)
```

`--no-build` requires a prior `dotnet build`; drop it (slower) if unsure.

## Generate-content workflow

1. Read `SPEC.md` for the exact fields. Write `spec.json` (camelCase; property names are
   matched case-insensitively).
2. `validate spec.json`. **If it reports problems, FIX the spec and re-validate** — do not
   build an invalid spec. It catches: empty/duplicate `editorId`, dialogue→unknown quest/npc,
   script→unknown target, object-property→unknown record, bad property type.
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
- **ESL** (`esl: true`, default): keep new records ≤ 4096.
- **CJK**: only `applyloc` produces game-readable Chinese (Localized UTF-8). Plain inline
  strings turn Chinese into `?` (the engine's cp1252). Don't use `apply`/`build` for CJK text.
- The Papyrus compiler returns exit code 0 even on failure; the tool already scrapes stdout —
  if you ever call `wine PapyrusCompiler.exe` yourself, do the same, and check the `.pex` exists.

## Limits — be honest, do not over-claim

ModForge writes **structurally valid** records. That is NOT the same as **in-game functional**:

- **NPCs are not yet functional actors** — there is no race/class/outfit support yet
  (needs external/vanilla form references, the pending It.7b). A generated npc record exists
  but won't behave like a real NPC in-game until that lands.
- **External/vanilla forms can't be referenced yet** — you cannot point at Skyrim.esm content
  (vanilla items, races, keywords, magic effects, leveled lists, cells/placement). So no
  spell/potion effects, no armor keywords, no placing things in the world yet.
- **Dialogue** records are valid, but a line actually appearing in conversation can need
  quest-flag/branch tuning, and there is **no voice** (subtitle only).
- You cannot confirm anything works **in-game** from here — that needs a Proton/Skyrim launch.
  Say "generated and structurally verified (dump)", not "works in-game", unless a human tested it.

When a request needs something in the Limits list, say so plainly and offer what IS possible
(or note it as blocked on It.7b). See `NOTES.md` for the iteration backlog.
