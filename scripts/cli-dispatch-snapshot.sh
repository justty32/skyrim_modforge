#!/usr/bin/env bash
# Behavioural snapshot of the CLI's argv dispatcher — the golden-hash equivalent for
# ModForge.Cli, which the test suite barely covers and `golden-hash.sh` only exercises
# through `build`.
#
# For every command name, invokes it with 0..5 placeholder arguments and records
#   <name> <argc> -> exit=<code> usage=<yes|no>
# "usage=yes" means the dispatcher did NOT accept that shape and fell through to Usage().
# Diffing a before/after pair therefore proves the dispatch tables still accept exactly
# the same argv shapes.
#
#   scripts/cli-dispatch-snapshot.sh /tmp/before.txt
#   ...refactor...
#   scripts/cli-dispatch-snapshot.sh /tmp/after.txt
#   diff /tmp/before.txt /tmp/after.txt && echo "dispatch unchanged"
#
# Placeholder arguments name nothing real, so a command that DOES match its shape fails
# on a missing file — that is fine and is exactly what distinguishes it from a fall-through.
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"
out="${1:?usage: scripts/cli-dispatch-snapshot.sh <out-file>}"

dotnet build src/ModForge.Cli/ModForge.Cli.csproj -v q --nologo >/dev/null
cli="$repo_root/src/ModForge.Cli/bin/Debug/net10.0/ModForge.Cli"
[ -x "$cli" ] || cli="$cli.exe"

# Same concurrency trap as golden-hash.sh: a `dotnet build` run by anything else on this
# repo deletes and rewrites this binary mid-loop. Every shape probed after that records
# exit=127 usage=no, which diffs as "the dispatch table changed" — the exact false alarm
# this script exists to rule out. Fingerprint it and refuse to emit a diffable file if it
# moved. Never fails on its own (empty = binary gone), so the report below always runs.
cli_fingerprint() { sha256sum "$cli" 2>/dev/null | cut -d' ' -f1 || true; }
cli_fp_before="$(cli_fingerprint)" || true

names=$(grep -rhoE '^\s+"[a-z-]+" when ' \
          src/ModForge.Cli/Commands/Program.Dispatch.cs \
          src/ModForge.Cli/Diagnostics/Diagnostics.Dispatch.cs \
        | grep -oE '"[a-z-]+"' | tr -d '"' | sort -u)
[ -n "$names" ] || { echo "cli-dispatch-snapshot: found no command names" >&2; exit 1; }

: > "$out"
for n in $names; do
  for argc in 0 1 2 3 4 5; do
    argv=("$n")
    for i in $(seq 1 "$argc"); do argv+=("zzz$i"); done
    o=$("$cli" "${argv[@]}" 2>&1); rc=$?
    case "$o" in *"ModForge.Cli"$'\n'*) u=yes ;; *) u=no ;; esac
    printf '%-16s %s -> exit=%s usage=%s\n' "$n" "$argc" "$rc" "$u" >> "$out"
  done
done
cli_fp_after="$(cli_fingerprint)" || true
if [ "$cli_fp_before" != "$cli_fp_after" ]; then
  echo "cli-dispatch-snapshot: ABORTED — the CLI binary changed while the run was in flight" >&2
  echo "  (fingerprint before=${cli_fp_before:0:12} after=${cli_fp_after:0:12})" >&2
  echo "  Something else built this repo concurrently (another agent line, an IDE, a watcher)." >&2
  echo "  DO NOT diff $out — rerun with nothing else building." >&2
  exit 2
fi

echo "cli-dispatch-snapshot: $(printf '%s\n' "$names" | wc -l) command(s), $(wc -l < "$out") shape(s) -> $out"
