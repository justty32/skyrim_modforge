#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

data_dir="${MODFORGE_SKYRIM_DATA:-$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data}"
out_dir="${MODFORGE_REFERENCE_OUT:-$repo_root/docs/reference/skyrim-masters-local}"

if [[ -n "${MODFORGE_SKYRIM_MASTERS:-}" ]]; then
  read -r -a masters <<< "$MODFORGE_SKYRIM_MASTERS"
else
  declare -a masters=(
    "Skyrim.esm"
    "Dawnguard.esm"
    "HearthFires.esm"
    "Dragonborn.esm"
  )
fi

declare -a queries=(
  "Race|nord"
  "Class|blacksmith"
  "Keyword|armorclothing"
  "MagicEffect|restorehealth"
  "Cell|banneredmare"
  "Worldspace|tamriel"
  "Npc|serana"
  "Weapon|crossbow"
  "Book|spelltome"
  "Location|solstheim"
)

json_escape() {
  local s="$1"
  s="${s//\\/\\\\}"
  s="${s//\"/\\\"}"
  s="${s//$'\n'/\\n}"
  printf '%s' "$s"
}

mkdir -p "$out_dir/find" "$out_dir/logs"

echo "Data dir: $data_dir"
echo "Output:   $out_dir"

for master in "${masters[@]}"; do
  path="$data_dir/$master"
  if [[ ! -f "$path" ]]; then
    echo "Missing required master: $path" >&2
    exit 2
  fi
done

dotnet build src/ModForge.Cli/ModForge.Cli.csproj -v q | tee "$out_dir/logs/dotnet-build.log"

cli=(dotnet run --project src/ModForge.Cli --no-build --)
run_status="$out_dir/run-status.tsv"
: > "$run_status"

{
  printf '{\n'
  printf '  "generatedAt": "%s",\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  printf '  "dataDir": "%s",\n' "$(json_escape "$data_dir")"
  printf '  "masters": [\n'
  for i in "${!masters[@]}"; do
    master="${masters[$i]}"
    path="$data_dir/$master"
    comma=","
    [[ "$i" == "$((${#masters[@]} - 1))" ]] && comma=""
    printf '    { "name": "%s", "path": "%s", "sizeBytes": %s }%s\n' \
      "$master" "$(json_escape "$path")" "$(stat -c '%s' "$path")" "$comma"
  done
  printf '  ],\n'
  printf '  "outputs": {\n'
  printf '    "findDirectory": "find",\n'
  printf '    "logsDirectory": "logs",\n'
  printf '    "runStatus": "run-status.tsv"\n'
  printf '  }\n'
  printf '}\n'
} > "$out_dir/manifest.json"

for master in "${masters[@]}"; do
  path="$data_dir/$master"
  master_slug="${master%.esm}"
  for entry in "${queries[@]}"; do
    type="${entry%%|*}"
    query="${entry#*|}"
    out="$out_dir/find/${master_slug}-${type}-${query}.txt"
    log="$out_dir/logs/${master_slug}-${type}-${query}.err"
    echo "find $master $query $type"
    if "${cli[@]}" find "$path" "$query" "$type" > "$out" 2> "$log"; then
      printf '%s\tfind\t%s\t%s\t%s\tOK\t%s\n' "$master" "$type" "$query" "$out" "$log" >> "$run_status"
    else
      code=$?
      printf '%s\tfind\t%s\t%s\t%s\tFAIL:%s\t%s\n' "$master" "$type" "$query" "$out" "$code" "$log" >> "$run_status"
    fi
  done
done

skyrim_path="$data_dir/Skyrim.esm"
echo "smtree Skyrim.esm"
if "${cli[@]}" smtree "$skyrim_path" > "$out_dir/skyrim-smtree.txt" 2> "$out_dir/logs/skyrim-smtree.err"; then
  printf '%s\tsmtree\t-\t-\t%s\tOK\t%s\n' "Skyrim.esm" "$out_dir/skyrim-smtree.txt" "$out_dir/logs/skyrim-smtree.err" >> "$run_status"
else
  code=$?
  printf '%s\tsmtree\t-\t-\t%s\tFAIL:%s\t%s\n' "Skyrim.esm" "$out_dir/skyrim-smtree.txt" "$code" "$out_dir/logs/skyrim-smtree.err" >> "$run_status"
fi

echo "Wrote $out_dir"
