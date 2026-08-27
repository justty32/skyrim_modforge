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

# Fingerprint the CLI so a CONCURRENT rebuild can be told apart from a broken spec.
# Another process running `dotnet build` on this repo deletes and rewrites this exact
# binary mid-run; every spec still queued then fails to exec it. Without this check the
# run reports them as BUILD-FAIL, i.e. as "your refactor changed the output" — the one
# conclusion this script exists to make trustworthy. Measured: a rebuild started 3s into
# a run turned 143 passing specs into 135 BUILD-FAIL.
# Must never fail: under `set -e` + `pipefail` a plain sha256sum of a file that has just
# been deleted would abort the script silently, right where it is supposed to EXPLAIN
# itself. An empty fingerprint is a valid answer here and means "binary is gone".
cli_fingerprint() { sha256sum "$cli" 2>/dev/null | cut -d' ' -f1 || true; }
cli_fp_before="$(cli_fingerprint)" || true

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
  set +e
  "$CLI" build "$spec" "$esp" >"$WORK/$name.log" 2>&1
  rc=$?
  set -e
  # 126/127 = the binary could not be executed at all (deleted or replaced under us).
  # That is a HARNESS failure, not this spec failing to build — never conflate the two.
  if [ "$rc" -eq 126 ] || [ "$rc" -eq 127 ]; then
    printf "HARNESS-FAIL%54s  %s\n" "" "$name"
    exit 0
  fi
  if [ "$rc" -ne 0 ]; then
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
harness=$(grep -c '^HARNESS-FAIL' "$out" || true)
cli_fp_after="$(cli_fingerprint)" || true

# A disturbed run must NOT be diffed: its BUILD-FAIL/HARNESS-FAIL lines would read as
# output changes and condemn a refactor that is actually fine. Fail loudly instead.
if [ "$harness" -gt 0 ] || [ "$cli_fp_before" != "$cli_fp_after" ]; then
  echo "golden-hash: ABORTED — the CLI binary changed while the run was in flight" >&2
  echo "  ($harness spec(s) could not exec it; fingerprint before=${cli_fp_before:0:12} after=${cli_fp_after:0:12})" >&2
  echo "  Something else built this repo concurrently (another agent line, an IDE, a watcher)." >&2
  echo "  DO NOT diff $out — rerun with nothing else building." >&2
  exit 2
fi

echo "golden-hash: $specs spec(s) -> $(wc -l < "$out") artifact hash(es), $fails build failure(s) -> $out"
