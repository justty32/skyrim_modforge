#!/usr/bin/env bash
# Compile every reusable dispatcher/controller .psc under assets/papyrus/ to .pex.
# These .pex are embedded as conditional EmbeddedResource in ModForge.Cli but are
# gitignored, so a fresh clone must run this once before `dotnet build` ships them.
# Re-run after editing any of those .psc. Needs a Papyrus toolchain (Wine+CK or the
# native compiler — see docs/TOOLING.md); a missing toolchain makes each compile fail
# (the CLI build still succeeds, it just warns at runtime that the feature is inert).
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

shopt -s nullglob
pscs=(assets/papyrus/*.psc)
if [ ${#pscs[@]} -eq 0 ]; then
  echo "No .psc found under assets/papyrus/ — nothing to compile." >&2
  exit 1
fi

ok=0; fail=0
for psc in "${pscs[@]}"; do
  echo "=== compiling $(basename "$psc") ==="
  if dotnet run --project src/ModForge.Cli -- compile "$psc" assets/papyrus/; then
    ok=$((ok + 1))
  else
    fail=$((fail + 1))
    echo "  ! compile failed: $psc" >&2
  fi
done

echo
echo "bootstrap-pex: $ok compiled, $fail failed (of ${#pscs[@]})."
[ "$fail" -eq 0 ]
