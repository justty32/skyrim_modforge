#!/usr/bin/env bash
# Extract human-readable game content (books / dialogue / quests / npc・item・location・magic lists)
# from vanilla+DLC+CC masters AND downloaded-mod plugins into ./vanilla/ and ./mods/.
# Re-runnable. Memory-safe (CLI uses lazy overlays). Output is gitignored (regenerable, large).
#
# Usage:  ./extract.sh            # masters + every plugin already unpacked under ~/skyrim_mods/unzip
#         ./extract.sh <plugin>   # one extra plugin -> mods/<basename>/
set -uo pipefail
cd "$(dirname "$0")"
REPO="$(cd ../.. && pwd)"
DLL="$REPO/src/ModForge.Cli/bin/Release/net10.0/ModForge.Cli.dll"
DATA="${MODFORGE_SKYRIM_DATA:-$HOME/.steam/steam/steamapps/common/Skyrim Special Edition/Data}"
UNZIP="$HOME/skyrim_mods/unzip"

run() { # <plugin-path> <out-subdir>
  local plugin="$1" out="$2"
  [ -f "$plugin" ] || { echo "  skip (missing): $plugin"; return; }
  dotnet "$DLL" gamedata "$plugin" "$out"
}

if [ "$#" -eq 1 ]; then run "$1" "mods/$(basename "${1%.*}")"; exit 0; fi

echo "== masters (vanilla + DLC + CC) =="
shopt -s nullglob nocaseglob
for p in "$DATA"/*.esm "$DATA"/*.esl; do
  run "$p" "vanilla/$(basename "${p%.*}")"
done

echo "== downloaded-mod plugins (already unpacked under unzip/) =="
# One folder per UNIQUE plugin filename; first match wins (skips duplicate appearance/patch copies).
# Process *English* FOMOD paths FIRST so a multi-language mod (e.g. VIGILANT 10 English / 10 Japanese)
# claims the name with its English strings; the other-language copy is then deduped away.
declare -A seen
take() {
  while IFS= read -r -d '' p; do
    base="$(basename "$p")"
    [ -n "${seen[$base]:-}" ] && continue
    seen[$base]=1
    run "$p" "mods/$(basename "${p%.*}")"
  done
}
PLUGS=( -type f \( -iname '*.esp' -o -iname '*.esm' -o -iname '*.esl' \) )
take < <(find "$UNZIP" "${PLUGS[@]}" -ipath '*english*' -print0 2>/dev/null)
take < <(find "$UNZIP" "${PLUGS[@]}" -print0 2>/dev/null)

echo "== done. See */*/summary.txt =="
