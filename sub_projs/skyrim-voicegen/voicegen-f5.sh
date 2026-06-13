#!/usr/bin/env bash
# MODFORGE_TTS_BIN wrapper: run voicegen.py inside the f5 venv so f5_tts imports resolve.
# Point MODFORGE_TTS_BIN at the absolute path of this script.
#
# The f5 venv lives at the ModForge repo root (.venvs/f5, gitignored — venvs bake in
# absolute paths so they can't be relocated). Override with MODFORGE_VOICEGEN_VENV if
# you keep it elsewhere.
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$HERE/../.." && pwd)"
VENV="${MODFORGE_VOICEGEN_VENV:-$REPO_ROOT/.venvs/f5}"
exec "$VENV/bin/python" "$HERE/voicegen.py" "$@"
