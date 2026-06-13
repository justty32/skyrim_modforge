#!/usr/bin/env bash
# Build + package a spec into a FLAT, MO2-ready zip and drop it in the delivery dir.
#
#   scripts/ship.sh <spec.json> [zipName] [--assets <dir>] [--clean-prefix]
#
# FLAT = plugin.esp + Scripts/ + Seq/ at the zip root (no nested wrapper folder), which
# is what MO2 expects and avoids the stale-ESP-at-root trap. The plugin name is read
# from the built .esp (so it survives spec-side $ref/$env), and the zip is named after
# it unless [zipName] overrides. Delivery dir is $MODFORGE_SHIP_DIR (default
# ~/skyrim_mods/mine — ~/skyrim_mods root holds the user's Nexus downloads, keep separate).
#
# --clean-prefix removes any other <pluginBase>*.zip already in the delivery dir before
# shipping (e.g. ModForgeFoo-test3.zip when shipping ModForgeFoo.zip) so MO2 can't install
# a stale variant. Without it, such siblings are only warned about.
#
# For voice mods use scripts/ship-voice.sh instead (voice files must be generated onto the
# packaged plugin, not bundled at build time).
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
[ -n "$spec" ] || { echo "usage: scripts/ship.sh <spec.json> [zipName] [--assets <dir>] [--clean-prefix]" >&2; exit 2; }
[ -f "$spec" ] || { echo "spec not found: $spec" >&2; exit 2; }

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
[ -n "$zip_name" ] || zip_name="$plugin_base"
zip_name="${zip_name%.zip}"

mkdir -p "$ship_dir"
dest="$ship_dir/$zip_name.zip"

# stale-file guard: drop any prior same-prefix zips (or just warn).
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
unzip -l "$dest"
# sanity: plugin must sit at the zip root (FLAT)
if ! unzip -l "$dest" | awk '{print $4}' | grep -qiE "^$plugin_base\.(esp|esm|esl)$"; then
  echo "!! WARNING: plugin not at zip root — zip is NOT flat" >&2
fi
