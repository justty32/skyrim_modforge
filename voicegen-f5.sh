#!/usr/bin/env bash
# MODFORGE_TTS_BIN wrapper: run voicegen.py inside the f5 venv so f5_tts imports resolve.
# Set MODFORGE_TTS_BIN to the absolute path of this script.
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec "$HERE/.venvs/f5/bin/python" "$HERE/voicegen.py" "$@"
