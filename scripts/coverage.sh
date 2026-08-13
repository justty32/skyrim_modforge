#!/usr/bin/env bash
# Line-coverage report for ModForge's own code.
#
#   scripts/coverage.sh                 # offline run (Category!=RequiresSkyrim), report to stdout
#   scripts/coverage.sh /tmp/cov.txt    # ...and save a copy
#   MODFORGE_COVERAGE_FILTER= scripts/coverage.sh   # no filter: include RequiresSkyrim too
#
# WHY: golden-hash.sh and cli-dispatch-snapshot.sh answer "did behaviour change?". Neither answers
# "what is not exercised at all", which is the question you want before writing tests. This wraps the
# collector that ships with Microsoft.NET.Test.Sdk (no extra package, so it works offline) and ranks
# ModForge's files by UNCOVERED lines — third-party source-linked dependencies (DynamicData,
# Humanizer, Mutagen) and generated obj/ sources are filtered out, or they bury the real result.
#
# ⚠️ READ THE NUMBER IN LIGHT OF THE FILTER. The default excludes Category=RequiresSkyrim, so any
# code reachable ONLY through those tests looks uncovered even when it is thoroughly tested. That is
# a real fact about the offline machine (it is what that machine can regress-test), but it is NOT a
# statement that the code lacks tests — check whether a *Tests.cs for it is entirely RequiresSkyrim
# before concluding anything. Generator.LivingNpcs.cs read as 4% for exactly this reason.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

out="${1:-}"
filter="${MODFORGE_COVERAGE_FILTER-Category!=RequiresSkyrim}"
# Probe by RUNNING each candidate, not by `command -v`: Windows ships a python3.exe App Execution
# Alias that resolves fine and then prints a Microsoft Store advert instead of interpreting anything.
py=""
for candidate in python3 python; do
  if "$candidate" -c "import sys; sys.exit(0)" >/dev/null 2>&1; then py="$candidate"; break; fi
done
[ -n "$py" ] || { echo "coverage.sh needs a working python3 (or python) on PATH" >&2; exit 1; }

# Under obj/ so it is already gitignored, and RELATIVE so the parser never has to reconcile a
# Windows python's idea of a temp path with the shell's (they differ under Git Bash / cygwin).
results="obj/coverage"
rm -rf "$results"

args=(test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj
      --collect:"Code Coverage;Format=cobertura" --results-directory "$results")
if [ -n "$filter" ]; then args+=(--filter "$filter"); fi   # plain `[ ] &&` would trip `set -e` here

echo "running tests with coverage${filter:+ (--filter \"$filter\")}..." >&2
dotnet "${args[@]}" >/dev/null 2>&1 || {
  echo "coverage.sh: the test run failed — fix the tests first, the report would be meaningless" >&2
  exit 1
}

report="$("$py" - "$results" <<'PY'
import glob, os, sys
import xml.etree.ElementTree as ET
from collections import defaultdict

hits = glob.glob(os.path.join(sys.argv[1], '**', '*.cobertura.xml'), recursive=True)
if not hits:
    sys.exit("coverage.sh: the collector produced no cobertura report")

per = defaultdict(lambda: [0, 0])          # source file -> [covered, total]
for cls in ET.parse(max(hits, key=os.path.getmtime)).getroot().iter('class'):
    name = (cls.get('filename') or '').replace('\\', '/')
    if ('/ModForge.Core/' not in name and '/ModForge.Cli/' not in name) or '/obj/' in name:
        continue                            # third-party source-linked deps and generated sources
    for line in cls.iter('line'):
        per[name][1] += 1
        if int(line.get('hits', '0')) > 0:
            per[name][0] += 1

rows = sorted(((tot - cov, cov, tot, f[f.rfind('/src/') + 1:] if '/src/' in f else f)
               for f, (cov, tot) in per.items()), reverse=True)
covered, total = sum(r[1] for r in rows), sum(r[2] for r in rows)

print("ModForge line coverage: %d/%d = %.1f%%   (%d files)"
      % (covered, total, 100.0 * covered / total if total else 0.0, len(rows)))
print()
print("%6s  %5s  %s" % ("UNCOV", "COV", "FILE"))
for uncov, cov, tot, name in rows:
    if uncov:
        print("%6d  %4.0f%%  %s" % (uncov, 100.0 * cov / tot, name))

zero = [r for r in rows if r[1] == 0]
if zero:
    print()
    print("zero coverage (%d files, %d lines):" % (len(zero), sum(r[2] for r in zero)))
    for _, _, tot, name in sorted(zero, key=lambda r: -r[2]):
        print("  %5d  %s" % (tot, name))
PY
)"

echo "$report"
if [ -n "$out" ]; then
  printf '%s\n' "$report" > "$out"
  echo "(saved to $out)" >&2
fi
