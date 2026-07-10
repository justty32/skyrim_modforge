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
CALL="$HERE/skylink-call.py"
MO2="/home/lorkhan/games/mod-organizer-2-skyrimspecialedition/modorganizer2/ModOrganizer.exe"
DOCS="/home/lorkhan/.steam/steam/steamapps/compatdata/$APPID/pfx/drive_c/users/steamuser/Documents/My Games/Skyrim Special Edition"

game_running() { pgrep -f 'SkyrimSE\.ex[e]' >/dev/null; }
mo2_running() { pgrep -f 'ModOrganize[r]\.exe' >/dev/null; }
relay_running() { pgrep -f 'rela[y]\.exe' >/dev/null; }
socat_running() { pgrep -f "UNIX-LISTEN:${UDS}" >/dev/null; }

newest() { find "$1" -name "$2" -printf '%T@ %p\n' 2>/dev/null | sort -rn | head -1 | cut -d' ' -f2-; }

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
crashlog)
    CL="$(newest "$DOCS/SKSE" 'crash-*.log')"
    [ -n "$CL" ] || { echo "no crash log found"; exit 1; }
    echo "$CL"
    ;;
game-restart)
    # MO2 survives a game CTD, so recovery never touches Steam or wineserver --
    # which is what keeps us clear of the zombie-wineserver hang.
    mo2_running || { echo "MO2 is not running; relaunch the game through Steam by hand"; exit 1; }
    game_running && { echo "game already running"; exit 0; }

    nohup protontricks-launch --appid "$APPID" "$MO2" 'moshortcut://:SKSE' \
        >/tmp/skylink-mo2launch.log 2>&1 &
    for _ in $(seq 1 60); do game_running && break; sleep 1; done
    game_running || { echo "game did not start -- see /tmp/skylink-mo2launch.log"; exit 1; }

    # The SKSE pipe comes back on its own; relay/socat outlive the crash.
    for _ in $(seq 1 45); do "$CALL" get_game_safety >/dev/null 2>&1 && break; sleep 2; done
    "$CALL" get_game_safety >/dev/null 2>&1 \
        && echo "game up, pipe alive (main menu)" \
        || { echo "game up but pipe unreachable"; exit 1; }
    ;;
game-load-latest)
    # load_most_recent_save is broken upstream (always returns loading:false),
    # so pick the newest .ess ourselves and load it by name.
    SAVE="$(newest "$DOCS/Saves" '*.ess')"
    [ -n "$SAVE" ] || { echo "no save found"; exit 1; }
    STEM="$(basename "$SAVE" .ess)"
    echo "loading $STEM"
    "$CALL" load_save "{\"saveName\":\"$STEM\"}" || exit 1
    for _ in $(seq 1 45); do "$CALL" get_cell_info >/dev/null 2>&1 && break; sleep 2; done
    "$CALL" get_cell_info >/dev/null 2>&1 && echo "save loaded" || { echo "save did not load"; exit 1; }
    ;;
status)
    game_running  && echo "game:  running" || echo "game:  not running"
    mo2_running   && echo "mo2:   running" || echo "mo2:   not running"
    relay_running && echo "relay: up"      || echo "relay: down"
    socat_running && echo "socat: up"      || echo "socat: down"
    ;;
*)
    echo "usage: $0 {build|up|down|status|crashlog|game-restart|game-load-latest}"; exit 2;;
esac
