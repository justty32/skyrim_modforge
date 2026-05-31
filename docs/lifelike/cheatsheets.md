# Cheat sheets — diagnostics, console, workflow

← back to [lifelike hub](README.md)

## Diagnostic commands

```bash
cd ~/repo/ModForge
# run (no rebuild): dotnet run --project src/ModForge.Cli --no-build -- <cmd> ...

# Find vanilla forms by editorID substring (use [Type] to narrow — ~0.9s typed vs ~3.3s full-ESM scan)
dotnet run --project src/ModForge.Cli --no-build -- find <Skyrim.esm> "Ysolda" Npc
dotnet run --project src/ModForge.Cli --no-build -- find <Skyrim.esm> "CrimeFaction" Faction

# Inspect specific record types — used to diff vanilla vs our generated records
dotnet run ... -- packagediag    <plugin> <0xFORMID>   # PACK: template, flags, schedule, Data slots
dotnet run ... -- pkgsbytemplate <plugin> <0xFORMID>   # every package USING a given procedure template
dotnet run ... -- npcdiag        <plugin> <0xFORMID>   # NPC: race/class/voice/factions/CrimeFaction/AIData/packages/spells
dotnet run ... -- cstydiag       <plugin> <0xFORMID>   # CSTY: offensive/defensive/equip mults/flags
dotnet run ... -- eczndiag       <plugin> <0xFORMID>   # ECZN: level range (max 0 = uncapped)/rank/flags/owner/location
dotnet run ... -- mgefdiag       <plugin> <0xFORMID>   # MGEF: archetype/AV/flags/projectile/casting art
dotnet run ... -- lightdiag      <plugin> [0xFORMID]   # LIGH (no ID lists room-fill candidates)
dotnet run ... -- refpos         <plugin> <0xFORMID>   # REFR/ACHR: position+rotation+base (anchor placements on known navmesh)
dotnet run ... -- cellblk        <plugin> [0xFORMID]   # Cell block/sub-block by FormID
dotnet run ... -- infodiag       <plugin> <0xFORMID> [substr]  # INFO: responses + FULL CTDA conditions + OnEnd VMAD fragment, for a topic OR every topic a quest owns
dotnet run ... -- factdiag       <plugin> <0xFORMID>   # FACT: flags / ranks / inter-faction relations
dotnet run ... -- reladiag       <plugin> <0xFORMID>   # RELA: one record, or every RELA referencing the FormID as parent/child

# Build / inspect round-trip
dotnet run ... -- validate <spec.json>              # ALWAYS run first
dotnet run ... -- build    <spec.json> <out.esp>
dotnet run ... -- dump     <out.esp>                # see what we actually wrote
dotnet run ... -- extract  <out.esp> <strings.json> # read a plugin back to JSON (round-trip verify; distinct from dump)
dotnet run ... -- package  <spec.json> <outDir>     # esp + .pex
```

Tips:
- **`pkgsbytemplate` is how you harvest vanilla packages for a template** — `find` only matches
  EditorIDs, so template-based vanilla packages (e.g. `WhiterunTempleCastHealingSpellSoldier`)
  that don't carry the template name in their ID are invisible to `find`. Pass a template FormID
  (e.g. UseMagic `0x0504F5`) to list every concrete package using it, then `packagediag` one to
  copy its slot pattern.
- **`cellblk` against `Skyrim.esm`** cross-checks the interior block/sub-block formula
  (block = id%10, sub = (id/10)%10) — use it to confirm a vanilla-cell override lands in the
  right GRUP without an in-game cycle.
- **`infodiag` is THE probe before reusing any vanilla dialogue path.** Dump the topic's INFO CTDA
  stack to see what a generated NPC must satisfy — this is how the It.27 follower bug was cracked
  (every paid-recruit INFO is `GetIsID==<a specific vanilla mercenary>`, so a custom NPC can never
  pass; `infodiag Skyrim.esm 0x0BCC84`). It also prints each INFO's OnEnd VMAD fragment, so you can
  see whether a vanilla line runs a result script you'd need to replicate.
- **`MODFORGE_DEBUG=1`** prints the full stack trace on error (otherwise just `ERROR: Type: msg`).

## In-game console (for testing generated NPCs)

```
help "ModForge X" 0                # find an NPC's runtime FormID (FExx0XXX form for ESL)
prid <FormID>                       # select an NPC by FormID
player.moveto <FormID>              # teleport player to NPC
moveto player                       # teleport selected NPC to player
getCurrentPackage                   # what package is the engine running on this NPC?
evp                                 # force re-evaluate packages (alias for evaluatePackage)
placeatme <baseFormID> <count>      # spawn an enemy (e.g. placeatme 0x10F2A3 1 → wolf)
getav health|magicka|stamina        # read selected actor's stats
coc <cellEditorID>                  # teleport to a cell (no load screen → LOD may break briefly)
tcl                                 # toggle clip / no-clip
```

## Papyrus prerequisites (compile / package-with-scripts)

- Set `MODFORGE_PAPYRUS_COMPILER` (path to `PapyrusCompiler.exe`) and `MODFORGE_PAPYRUS_BASE`
  (dir with the base `.psc` + `TESV_Papyrus_Flags.flg`) if the CK isn't at the default Steam path.
- One-time: `unzip <CK>/Data/Scripts.zip "Source/Scripts/*" -d ~/.cache/modforge/papyrus/`
  (≈14,301 `.psc`). Add SKSE `.psc` to that dir if a script uses SKSE functions.
- The compiler **returns exit code 0 even on failure** — the tool scrapes stdout for `Failed on`
  and confirms the `.pex` exists; do the same if you ever invoke `wine PapyrusCompiler.exe` directly.

## CJK localization (Simplified-Chinese)

Simplified-Chinese SSE reads a Localized `<plugin>_chinese.STRINGS` in **UTF-8** (NOT GBK), with a
**lowercase** language suffix (Mutagen writes `_Chinese`; `applyloc` lowercases it — case matters on
Linux/Proton). Use `applyloc`, never `apply`/`build`, for CJK text (inline strings become `?` under
the engine's cp1252).
