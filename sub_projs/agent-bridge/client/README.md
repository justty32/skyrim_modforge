# client/ — the Linux half

`agent-bridge` (the DLL) is the eyes and hands inside the game. This directory is
everything on the Linux side of that socket. Kept in the same subproject on purpose:
the port number, the `/state` field names and the client that reads them are one
contract, and a two-sided edit should be one commit.

| Tool | Status | What it does |
|---|---|---|
| `mo2ctl.py` | ✅ verified end-to-end 2026-08-02 | Drive MO2 without its GUI: install / uninstall / enable / disable / launch / kill / status |
| MCP server | not built (plan 2.2) | Wrap `/state` + `/console` as `qa_state` / `qa_console` so Claude stops shelling out to curl |
| runner | not built (plan 3.2) | Execute a `qa.json` and produce a pass/fail report |

stdlib only, no venv. This has to keep working while the rest of the toolchain is
mid-rebuild, and a QA harness that needs its own install step before it can test
anything is a harness you stop using.

## mo2ctl

```bash
./mo2ctl.py status [--mod NAME]          # what's running, is the profile safe to edit
./mo2ctl.py install <dir-or-esp> [--name NAME] [--no-enable]
./mo2ctl.py uninstall <name> [--keep-files]
./mo2ctl.py enable|disable <name>
./mo2ctl.py launch [--wait 240]          # SKSE through MO2, waits for the bridge
./mo2ctl.py kill [--mo2]
```

`--json` on any subcommand (either side of it) for machine-readable output; that's
what the Phase 3 runner will use. Overrides: `MO2_ROOT`, `MO2_PROFILE` (otherwise
read from `ModOrganizer.ini`'s `selected_profile`).

`install` takes a mod folder, a folder whose only child is `Data/`, or a bare `.esp` —
that last one because `ModForge/out/` is exactly a pile of loose plugins. New mods go
in at modlist line 2 (top priority) and their plugins at the end of `plugins.txt`
(latest wins), which is where a thing under test wants to be on both counts.

### Verified end-to-end

Full install → launch → assert → uninstall cycle against the live 109-mod load order
on 2026-08-02, no GUI at any point:

1. `install ModForge/out/ModForgeNavmeshNoop.esp --name QaNoop`
2. `launch` → bridge answered `/ping` 30s later
3. `GET /state?include=plugins` → `{"name": "ModForgeNavmeshNoop.esp", "index": 26}`
4. `POST /console {"cmd": "load <baseline>"}` → `/state` reports `WhiterunExterior15`
5. `kill --mo2`, `uninstall QaNoop`
6. `modlist.txt`, `plugins.txt`, `loadorder.txt` all **byte-identical** to their
   pre-install backups

Step 6 is the one worth keeping. A QA loop that leaves residue in the profile is a
loop you can only run once.

## Three things that are not obvious

**The profile files disagree about line endings.** `modlist.txt` and `loadorder.txt`
are CRLF; `plugins.txt` is LF. Same directory, same program wrote all three. Normalising
them is not harmless: a `sed 's|^+AgentBridge$|...|'` against CRLF content silently
matches nothing, which is how an earlier manual edit appeared to succeed and did not.
So `read_file` carries each file's own ending along and `write_file` puts it back.

**Editing the profile while MO2 is running does not conflict — it silently reverts.**
MO2 holds the profile in memory and writes `modlist.txt` / `plugins.txt` back out on
exit or profile switch. Your edit lands, MO2 knows nothing about it, and minutes later
MO2 quits and overwrites it. No error, no warning, and the failure shows up as "the mod
I installed isn't loaded" long after the cause. Every mutating subcommand therefore
refuses while MO2 *or* the game is up. `--force` exists; the plan only anticipated the
game as a blocker, and MO2 turns out to be the one that actually bites.

**Process detection matches `argv[0]`, not the whole command line.** Substring matching
is wrong in both directions here. `protontricks-launch --appid 489830 .../ModOrganizer.exe
moshortcut://:SKSE` mentions MO2 in its arguments, so a `-f`-style match counted the
launcher, its wrapper and its python parent as three extra copies of MO2 — measured:
five "MO2 processes" when there was one. And the Steam/Proton chain around
`SkyrimSELauncher.exe` outlives the game for the whole session, so anything looser than
an exact filename compare would report the game as permanently running and wedge the
lock shut.

Related: this reads `/proc` directly instead of shelling out. `pkill -f <pattern>` killed
the invoking shell twice during this project, because the pattern matched the shell's own
command line. Scanning `/proc` and skipping our own pid cannot do that.

## launch

```
protontricks-launch --appid 489830 <MO2>/ModOrganizer.exe moshortcut://:SKSE
```

MO2 has to run inside the game's own Proton prefix — usvfs needs MO2 and the game in one
wine session, which is also why there is no separate MO2 wine prefix to point at.
`moshortcut://:SKSE` is MO2's own name for the `customExecutables` entry in
`ModOrganizer.ini`, so this is the same path the GUI's Run button takes. Use `--shortcut`
if that entry is renamed.

`launch` then polls `/ping` rather than guessing at a sleep, and gives up with
`bridge.reachable: false` instead of hanging. Observed cold start on this load order: 30s.
