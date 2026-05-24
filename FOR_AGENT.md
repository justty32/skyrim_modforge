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
$R dump     <plugin.esp>                     # read back: records, names, npc race/class/outfit/factions, weapon/armor stats, effects, cells/placements, keywords, scripts, dialogue, objectives, masters
$R find     <plugin.esp> <query> [type]      # search a master (e.g. Skyrim.esm) -> "Skyrim.esm:0xFORMID  Type  EditorID"
$R compile  <script.psc> <outDir>            # .psc -> .pex via the CK PapyrusCompiler under Wine
$R extract  <plugin.esp> <strings.json>      # pull translatable strings -> JSON (source/target)
$R apply    <plugin.esp> <strings.json> <out.esp>     # write targets back (Latin scripts / inline)
$R applyloc <plugin.esp> <strings.json> <outModDir>   # CJK: Localized UTF-8 <plugin>_chinese.STRINGS
$R gen      <out.esp>                         # demo plugin (sanity check the toolchain)
```

`--no-build` requires a prior `dotnet build`; drop it (slower) if unsure.

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
$R find "$SKYRIM_ESM" banneredmare Cell    # -> Skyrim.esm:0x01605E  WhiterunBanneredMare (for a `placement` cell)
```
Always run `find` to get the real FormID — **never guess one**. Search is by EditorID
(descriptive, e.g. `NordRace`); localized display names aren't resolved headless. A standing
NPC needs at least `race` + `class` to act like a real actor; `outfit` clothes it.

## Generate-content workflow

1. Read `SPEC.md` for the exact fields. Write `spec.json` (camelCase; property names are
   matched case-insensitively). For race/class/outfit/keywords/vanilla factions, `find` the
   FormID first and use the `"<master>:0xFORMID"` ref form.
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
- **ESL** (`esl: true`, default): keep new records ≤ 4096.
- **CJK**: only `applyloc` produces game-readable Chinese (Localized UTF-8). Plain inline
  strings turn Chinese into `?` (the engine's cp1252). Don't use `apply`/`build` for CJK text.
- The Papyrus compiler returns exit code 0 even on failure; the tool already scrapes stdout —
  if you ever call `wine PapyrusCompiler.exe` yourself, do the same, and check the `.pex` exists.

## Limits — be honest, do not over-claim

ModForge writes **structurally valid** records. That is NOT the same as **in-game functional**:

- **NPCs can now be functional actors** — set `race` + `class` (+ `outfit`) via vanilla refs
  and the NPC behaves like a real actor.
- **Placement works for new AND vanilla interior cells:** `cells` + `placements` put an
  NPC/object into a new in-spec interior cell (reach it with `coc <editorId>`) **or** into a
  **vanilla interior cell** by ref (`"Skyrim.esm:0xFORMID"`, e.g. `0x01605E` = Bannered Mare —
  `find <Skyrim.esm> <name> Cell`). Vanilla placement overrides the cell to *add* your ref
  (vanilla contents untouched). **Exterior/worldspace** cells aren't supported yet. Vanilla
  placement reads the game `Data` folder — set `MODFORGE_SKYRIM_DATA` if not at the Steam default.
- **Items/spells now carry gameplay stats:** weapons take `damage`/`speed`/`reach`, armor
  takes `armorType` + biped `slots`, **spells/potions take `effects`** (a MagicEffect *ref* +
  magnitude/area/duration), and spells take `spellType`/`castType`/`targetType`/`baseCost`. A
  potion with one effect is fully functional; a spell wants an effect + the cast fields.
- **Leveled lists + containers:** `leveledItems`/`leveledNpcs` (weighted level-gated entries,
  each a *ref*) and `containers` (item *refs* + counts) — loot tables, merchant chests, etc.
- **External/vanilla forms CAN be referenced** (race/class/outfit/keywords/factions/
  magicEffect/placement base+cell/leveled+container entries, via `"<master>:0xFORMID"`). The
  main thing still NOT covered: **placement into an exterior/worldspace vanilla cell** (interior
  vanilla cells and new in-spec cells both work).
- **Dialogue** records are valid, but a line actually appearing in conversation can need
  quest-flag/branch tuning, and there is **no voice** (subtitle only).
- You cannot confirm anything works **in-game** from here — that needs a Proton/Skyrim launch.
  Say "generated and structurally verified (dump)", not "works in-game", unless a human tested it.

When a request needs something in the Limits list, say so plainly and offer what IS possible
(or note it as blocked on It.7b). See `NOTES.md` for the iteration backlog.
