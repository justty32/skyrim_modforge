#!/usr/bin/env python3
"""Drive SkyLink's SkyrimMCP.dll over MCP stdio without registering it as an MCP server.

Useful when the bridge is up but the current Claude Code session predates the
`claude mcp add` registration. See workflows/skylink/README.md.

usage: skylink-call.py --list
       skylink-call.py <tool_name> [json_args]
"""
import json
import os
import subprocess
import sys
import threading

DEFAULT_SRV = ("/home/lorkhan/games/mod-organizer-2-skyrimspecialedition/modorganizer2/"
               "mods/SkyLinkAI/SKSE/Plugins/SkyLinkAI_Server/SkyrimMCP.dll")
SRV = os.environ.get("SKYLINK_SERVER_DLL", DEFAULT_SRV)

HANDSHAKE = [
    {"jsonrpc": "2.0", "id": 1, "method": "initialize",
     "params": {"protocolVersion": "2024-11-05", "capabilities": {},
                "clientInfo": {"name": "skylink-call", "version": "1"}}},
    {"jsonrpc": "2.0", "method": "notifications/initialized"},
]


def rpc(requests, want_id=2, timeout=30):
    """Send requests, read replies until `want_id` lands.

    stdin must stay open: closing it makes the server shut down before it
    flushes its responses to stdout.
    """
    p = subprocess.Popen(["dotnet", SRV], stdin=subprocess.PIPE,
                         stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True)
    for r in requests:
        p.stdin.write(json.dumps(r) + "\n")
    p.stdin.flush()

    msgs, timer = [], threading.Timer(timeout, p.kill)
    timer.start()
    try:
        for line in p.stdout:
            line = line.strip()
            if not line:
                continue
            m = json.loads(line)
            msgs.append(m)
            if m.get("id") == want_id:
                break
    finally:
        timer.cancel()
        p.kill()
        p.wait()
    return msgs


def main():
    if len(sys.argv) < 2 or sys.argv[1] == "--list":
        for m in rpc(HANDSHAKE + [{"jsonrpc": "2.0", "id": 2, "method": "tools/list"}]):
            if m.get("id") == 2:
                for t in m["result"]["tools"]:
                    print(f"{t['name']:32} {t.get('description', '')[:90]}")
        return 0

    tool = sys.argv[1]
    args = json.loads(sys.argv[2]) if len(sys.argv) > 2 else {}
    for m in rpc(HANDSHAKE + [{"jsonrpc": "2.0", "id": 2, "method": "tools/call",
                               "params": {"name": tool, "arguments": args}}]):
        if m.get("id") != 2:
            continue
        if "error" in m:
            print("ERROR:", json.dumps(m["error"], ensure_ascii=False, indent=2))
            return 1
        # A failed tool call is a normal result carrying isError, not a JSON-RPC
        # error. Scripts polling this must see a nonzero exit.
        for c in m["result"].get("content", []):
            print(c.get("text", json.dumps(c, ensure_ascii=False)))
        return 1 if m["result"].get("isError") else 0

    print("no response (is the bridge up? is the game running?)", file=sys.stderr)
    return 1


if __name__ == "__main__":
    sys.exit(main())
