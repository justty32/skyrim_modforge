#!/usr/bin/env bash
# Build + package a spec, generate voice files ONTO the packaged plugin, verify the
# planned vs shipped voice list, then ship a FLAT zip to the delivery dir.
#
#   scripts/ship-voice.sh <spec.json> [zipName] [--assets <dir>] [--clean-prefix]
#
# Why a separate script: Skyrim voice files are loose assets at
# Sound/Voice/<PluginName.esp>/<VoiceType>/<name>.fuz — NOT plugin records. The folder
# name must equal the shipped plugin's name, so voice must be generated AFTER package,
# against the packaged .esp. `voicelines` writes Sound/Voice/... next to the esp, so we
# package into a staging dir, run voicelines there, then `voicediag` to confirm the
# planned files match what landed, then flat-zip the whole staging dir (esp + Scripts +
# Seq + Sound).
#
# Requires MODFORGE_TTS_BIN (and ideally MODFORGE_XWMAENCODE + MODFORGE_LIPGEN) — see
# docs/TOOLING.md. Without TTS, voicelines aborts and no zip is produced.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"
ship_dir="${MODFORGE_SHIP_DIR:-$HOME/skyrim_mods/mine}"

spec=""; zip_name=""; assets=""; clean_prefix=0
while [ $# -gt 0 ]; do
  case "$1" in
    --assets) assets="${2:?--assets needs a dir}"; shift 2 ;;
    --clean-prefix) clean_prefix=1; shift ;;
    -*) echo "unknown flag: $1" >&2; exit 2 ;;
    *) if [ -z "$spec" ]; then spec="$1"; elif [ -z "$zip_name" ]; then zip_name="$1"; else echo "unexpected arg: $1" >&2; exit 2; fi; shift ;;
  esac
done
[ -n "$spec" ] || { echo "usage: scripts/ship-voice.sh <spec.json> [zipName] [--assets <dir>] [--clean-prefix]" >&2; exit 2; }
[ -f "$spec" ] || { echo "spec not found: $spec" >&2; exit 2; }
[ -n "${MODFORGE_TTS_BIN:-}" ] || { echo "ERROR: MODFORGE_TTS_BIN not set — voice generation needs it (see docs/TOOLING.md)." >&2; exit 1; }

stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT

echo "=== packaging $spec ==="
if [ -n "$assets" ]; then
  dotnet run --project src/ModForge.Cli -- package "$spec" "$stage" --assets "$assets"
else
  dotnet run --project src/ModForge.Cli -- package "$spec" "$stage"
fi

esp="$(find "$stage" -maxdepth 1 -name '*.esp' -o -maxdepth 1 -name '*.esm' -o -maxdepth 1 -name '*.esl' | head -1)"
[ -n "$esp" ] || { echo "no plugin produced in $stage" >&2; exit 1; }
plugin_base="$(basename "${esp%.*}")"

echo
echo "=== generating voice onto $(basename "$esp") ==="
dotnet run --project src/ModForge.Cli -- voicelines "$spec" "$esp"

echo
echo "=== voicediag: planned vs shipped ==="
dotnet run --project src/ModForge.Cli -- voicediag "$spec" "$esp"

if [ ! -d "$stage/Sound" ]; then
  echo "!! WARNING: no Sound/ folder produced — voice generation may have degraded/skipped." >&2
fi

[ -n "$zip_name" ] || zip_name="$plugin_base"
zip_name="${zip_name%.zip}"
mkdir -p "$ship_dir"
dest="$ship_dir/$zip_name.zip"

shopt -s nullglob
siblings=()
for z in "$ship_dir/$plugin_base"*.zip; do
  [ "$z" = "$dest" ] && continue
  siblings+=("$z")
done
if [ ${#siblings[@]} -gt 0 ]; then
  if [ "$clean_prefix" -eq 1 ]; then
    echo "=== removing stale same-prefix zips ==="
    for z in "${siblings[@]}"; do echo "  rm $(basename "$z")"; rm -f "$z"; done
  else
    echo "!! WARNING: other '$plugin_base*' zips exist in $ship_dir — MO2 may install a stale one:" >&2
    for z in "${siblings[@]}"; do echo "     $(basename "$z")" >&2; done
    echo "   (pass --clean-prefix to remove them)" >&2
  fi
fi

rm -f "$dest"
( cd "$stage" && zip -qr -X "$dest" . )

echo
echo "=== shipped: $dest ==="
unzip -l "$dest" | grep -iE 'Sound/Voice|\.(esp|esm|esl)$|---' || unzip -l "$dest"
