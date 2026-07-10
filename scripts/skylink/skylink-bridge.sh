#!/usr/bin/env bash
# SkyLink AI bridge — Manjaro only. See workflows/tooling/skylink.md.
#
#   Skyrim (Proton prefix)                                    Linux
#   SKSE plugin --\\.\pipe\SkyrimMCP--> relay.exe --TCP--> socat --> /tmp/CoreFxPipe_SkyrimMCP --> SkyrimMCP.dll
#
# The plugin's named pipe lives inside the game's wineserver, so relay.exe must
# run in that same prefix. `up` refuses when the game is down: starting a
# wineserver on that prefix behind Steam's back makes the next launch hang.
set -uo pipefail

APPID=489830
PORT=${SKYLINK_PORT:-8770}
UDS=/tmp/CoreFxPipe_SkyrimMCP
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RELAY="$HERE/relay.exe"

game_running() { pgrep -f 'SkyrimSE\.ex[e]' >/dev/null; }
relay_running() { pgrep -f 'rela[y]\.exe' >/dev/null; }
socat_running() { pgrep -f "UNIX-LISTEN:${UDS}" >/dev/null; }

case "${1:-status}" in
build)
    x86_64-w64-mingw32-gcc "$HERE/relay.c" -o "$RELAY" -lws2_32 -static -O2 -Wall || exit 1
    echo "built $RELAY"
    ;;
up)
    game_running || { echo "refusing: SkyrimSE.exe is not running (start the game first)"; exit 1; }
    [ -x "$RELAY" ] || { echo "missing $RELAY -- run '$0 build'"; exit 1; }

    socat_running || {
        rm -f "$UDS"
        nohup socat "UNIX-LISTEN:${UDS},fork" "TCP:127.0.0.1:${PORT}" >/dev/null 2>&1 &
        sleep 1
    }
    relay_running || {
        nohup protontricks-launch --appid "$APPID" "$RELAY" '\\.\pipe\SkyrimMCP' "$PORT" \
            >/tmp/skylink-relay.log 2>&1 &
        sleep 6
    }

    socat_running && [ -S "$UDS" ] && echo "socat: up ($UDS)" || { echo "socat: DOWN"; exit 1; }
    relay_running && echo "relay: up (port $PORT)" || { echo "relay: DOWN -- see /tmp/skylink-relay.log"; exit 1; }
    ;;
down)
    pkill -f 'rela[y]\.exe' 2>/dev/null
    pkill -f "UNIX-LISTEN:/tmp/CoreFxPipe_SkyrimMC[P]" 2>/dev/null
    rm -f "$UDS"
    echo "bridge down"
    ;;
status)
    game_running  && echo "game:  running" || echo "game:  not running"
    relay_running && echo "relay: up"      || echo "relay: down"
    socat_running && echo "socat: up"      || echo "socat: down"
    ;;
*)
    echo "usage: $0 {build|up|down|status}"; exit 2;;
esac
