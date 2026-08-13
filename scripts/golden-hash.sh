#!/usr/bin/env bash
# Byte-level regression guardrail for behaviour-preserving refactors.
#
# Builds every examples/*.json and prints "<sha256>  <artifact>" for each emitted
# .esp (and its .seq, when one is written). Run it BEFORE a refactor and AFTER,
# then diff the two files: any changed line means the output changed, i.e. the
# refactor was NOT behaviour-preserving.
#
#   scripts/golden-hash.sh /tmp/before.txt
#   ...refactor...
#   scripts/golden-hash.sh /tmp/after.txt
#   diff /tmp/before.txt /tmp/after.txt && echo "byte-identical"
#
# WHY this exists: 87% of the test suite only reaches the generator through
# Generator.Build/Validate and asserts on records, so it is blind to internal
# restructuring — see workflows/refactor/src-layout-plan.md § Batch 0.
#
# THE OUTPUT IS MACHINE-SPECIFIC — DO NOT COMMIT IT, AND DO NOT COMPARE ACROSS
# MACHINES. Without Skyrim.esm (the offline machine) every placement into a
# vanilla cell is skipped, so the plugin is a reduced build with different bytes
# than the same spec produces on Manjaro. Always compare before/after generated
# on the SAME machine.
#
# Usage: scripts/golden-hash.sh <out-file> [parallel-jobs, default 4]
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

out="${1:?usage: scripts/golden-hash.sh <out-file> [parallel-jobs]}"
jobs="${2:-4}"

dotnet build src/ModForge.Cli/ModForge.Cli.csproj -v q --nologo >/dev/null

cli="$repo_root/src/ModForge.Cli/bin/Debug/net10.0/ModForge.Cli"
[ -x "$cli" ] || cli="$cli.exe"
[ -x "$cli" ] || { echo "golden-hash: CLI binary not found next to $cli" >&2; exit 1; }

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
export WORK="$work" CLI="$cli"

# One spec -> zero or more "<sha256>  <artifact>" lines. A spec that fails to build
# emits a BUILD-FAIL line instead of aborting, so a broken example shows up as a
# diff rather than hiding every spec after it.
one_spec='
  set -eu
  spec="$1"
  name="$(basename "$spec" .json)"
  esp="$WORK/$name.esp"
  if ! "$CLI" build "$spec" "$esp" >"$WORK/$name.log" 2>&1; then
    printf "BUILD-FAIL%56s  %s\n" "" "$name"
    exit 0
  fi
  printf "%s  %s.esp\n" "$(sha256sum "$esp" | cut -d" " -f1)" "$name"
  seq="$WORK/Seq/$name.seq"
  if [ -f "$seq" ]; then
    printf "%s  %s.seq\n" "$(sha256sum "$seq" | cut -d" " -f1)" "$name"
  fi
'

find examples -maxdepth 1 -name '*.json' ! -name 'spec.schema.json' -print0 \
  | xargs -0 -P "$jobs" -I{} bash -c "$one_spec" _ {} \
  | sort -k2 > "$out"

specs=$(find examples -maxdepth 1 -name '*.json' ! -name 'spec.schema.json' | wc -l)
fails=$(grep -c '^BUILD-FAIL' "$out" || true)
echo "golden-hash: $specs spec(s) -> $(wc -l < "$out") artifact hash(es), $fails build failure(s) -> $out"
