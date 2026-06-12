# Local Skyrim Master Extraction

This repo already has master-inspection commands in `src/ModForge.Cli`: `find` searches
records by EditorID/name, and `smtree` lists Story Manager event roots. The local extraction
wrapper in `scripts/extract-skyrim-masters.sh` runs those existing commands against the Steam
Proton Skyrim Special Edition masters on Manjaro and writes small, reproducible reference
artifacts.

Default Data path:

```bash
$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data
```

Expected masters:

```text
Skyrim.esm
Dawnguard.esm
HearthFires.esm
Dragonborn.esm
```

Run:

```bash
cd /home/lorkhan/repo/ModForge
scripts/extract-skyrim-masters.sh
```

Override paths when needed:

```bash
MODFORGE_SKYRIM_DATA="/path/to/Skyrim Special Edition/Data" \
MODFORGE_REFERENCE_OUT="/tmp/modforge-skyrim-reference" \
scripts/extract-skyrim-masters.sh
```

Outputs go to `reference/skyrim-masters-local/` by default. `reference/` is gitignored, so
large or machine-local extracted data stays out of source control. The generated files are:

- `manifest.json`: input paths, sizes, and output layout.
- `run-status.tsv`: each CLI probe and whether it succeeded.
- `find/*.txt`: sampled FormID reference searches for common record types across Skyrim.esm
  and DLC masters.
- `skyrim-smtree.txt`: Story Manager event roots from Skyrim.esm.
- `logs/*.log` and `logs/*.err`: build output and stderr for failed or noisy probes.

Navigation map:

```text
reference/INDEX-skyrim-masters-local.md
```

Start there when an agent needs vanilla or DLC FormIDs. It is organized like the ModForge
`CODE_MAP` files: output folders, Data-folder map, voice locations, record-family lookup tables,
cached query naming, status format, direct CLI lookup commands, and rules for extending the cache
without blindly listing directories.

Split outputs from the 2026-06-12 Manjaro run:

- `reference/skyrim-esm-local/`: `Skyrim.esm` only.
- `reference/skyrim-dlc-local/`: `Dawnguard.esm`, `HearthFires.esm`, and `Dragonborn.esm` only.
- `reference/skyrim-masters-local/`: combined all-master run.

To run only selected masters, set `MODFORGE_SKYRIM_MASTERS` to a space-separated list:

```bash
MODFORGE_SKYRIM_MASTERS="Skyrim.esm" \
MODFORGE_REFERENCE_OUT="/home/lorkhan/repo/ModForge/reference/skyrim-esm-local" \
scripts/extract-skyrim-masters.sh
```

This is a reference-generation workflow, not a full master dump. For a specific record, use the
FormID from `find/*.txt` with an existing diagnostic command, for example:

```bash
R="dotnet run --project src/ModForge.Cli --no-build --"
SK="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data/Skyrim.esm"
$R weatherdiag "$SK" 0x10E1F2
```
