#!/usr/bin/env bash
# Behavioural snapshot of the `package` shipping path, which golden-hash.sh does not run.
#
# For every examples/*.json, records normalised stdout/stderr and the produced file tree:
#   * deterministic files are identified by SHA-256;
#   * .pex files are identified by path only because Papyrus embeds a build timestamp.
# Each parallel worker writes a private report; reports are sorted and joined only after all
# workers finish, so xargs -P cannot interleave lines.
#
#   scripts/package-snapshot.sh /tmp/before.txt
#   ...refactor...
#   scripts/package-snapshot.sh /tmp/after.txt
#   diff /tmp/before.txt /tmp/after.txt && echo "package unchanged"
#
# The output is machine-specific. Compare snapshots only on the same machine.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

out="${1:?usage: scripts/package-snapshot.sh <out-file> [parallel-jobs]}"
jobs="${2:-4}"

dotnet build src/ModForge.Cli/ModForge.Cli.csproj --no-restore -v q --nologo >/dev/null

cli="$repo_root/src/ModForge.Cli/bin/Debug/net10.0/ModForge.Cli"
[ -x "$cli" ] || cli="$cli.exe"
[ -x "$cli" ] || { echo "package-snapshot: CLI binary not found next to $cli" >&2; exit 1; }

# A concurrent build deletes and rewrites this binary while workers are still executing it.
# As in golden-hash.sh and cli-dispatch-snapshot.sh, an empty fingerprint is a valid result
# meaning "the binary vanished"; the explicit report below must still run under pipefail.
cli_fingerprint() { sha256sum "$cli" 2>/dev/null | cut -d' ' -f1 || true; }
cli_fp_before="$(cli_fingerprint)" || true

work="$(mktemp -d)"
snap="$(mktemp -d)"
trap 'rm -rf "$work" "$snap"' EXIT
export WORK="$work" SNAP="$snap" CLI="$cli" REPO="$repo_root"

one_spec='
  set -uo pipefail
  spec="$1"
  name="$(basename "$spec" .json)"
  dest="$WORK/$name"
  report="$SNAP/$name.txt"
  mkdir -p "$dest"

  output=$("$CLI" package "$spec" "$dest" 2>&1)
  rc=$?
  {
    echo "### $name rc=$rc"
    printf "%s\n" "$output" \
      | sed -e "s|$dest|<OUT>|g" -e "s|$WORK|<WORK>|g" \
            -e "s|/tmp/[A-Za-z0-9._/-]*|<TMP>|g" -e "s|$REPO|<REPO>|g" \
      | grep -vE "(ms|seconds)$"
    find "$dest" -type f -print0 | sort -z | while IFS= read -r -d "" file; do
      relative="${file#$dest/}"
      case "$relative" in
        *.pex) printf "  <pex-nondeterministic>  %s\n" "$relative" ;;
        *)     printf "  %s  %s\n" "$(sha256sum "$file" | cut -d\  -f1)" "$relative" ;;
      esac
    done
  } > "$report" 2>&1
'

find examples -maxdepth 1 -name '*.json' ! -name 'spec.schema.json' -print0 \
  | xargs -0 -P "$jobs" -I{} bash -c "$one_spec" _ {}

combined="$work/package-snapshot.txt"
: > "$combined"
while IFS= read -r -d '' report; do
  cat "$report" >> "$combined"
done < <(find "$snap" -name '*.txt' -print0 | sort -z)

specs="$(grep -c '^### ' "$combined" || true)"
lines="$(wc -l < "$combined")"
harness="$(grep -Ec '^### .* rc=(126|127)$' "$combined" || true)"
cli_fp_after="$(cli_fingerprint)" || true

# A disturbed run must not produce a snapshot that looks like a package behaviour change.
if [ "$harness" -gt 0 ] || [ "$cli_fp_before" != "$cli_fp_after" ]; then
  echo "package-snapshot: ABORTED — the CLI binary changed while the run was in flight" >&2
  echo "  ($harness spec(s) could not exec it; fingerprint before=${cli_fp_before:0:12} after=${cli_fp_after:0:12})" >&2
  echo "  Something else built this repo concurrently (another agent line, an IDE, a watcher)." >&2
  echo "  DO NOT diff $out — rerun with nothing else building." >&2
  exit 2
fi

cp "$combined" "$out"
echo "package-snapshot: $specs spec(s), $lines line(s)"
echo "package-snapshot: CLI fingerprint ${cli_fp_after:0:12} unchanged"
echo "package-snapshot: snapshot -> $out"
