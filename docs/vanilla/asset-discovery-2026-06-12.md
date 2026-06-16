# Skyrim SE vanilla asset discovery notes

Date: 2026-06-12T10:40:24+08:00

Goal: prepare safe extraction/diagnostics for Skyrim SE vanilla masters and DLC
archives without writing to the Steam game directory.

## Current execution environment

- Workspace: `C:\code\mine\skyrim_modforge`
- Shell visible to Codex: Windows PowerShell 5.1
- Home visible to Codex: `C:\Users\user`
- WSL status from `wsl.exe -l -v`: no installed WSL distributions were listed.
- `\\wsl.localhost\Manjaro\...` checks were blocked with access denied from this sandbox.

The user reported the real machine is Manjaro + Steam Proton, but this session can
only see the Windows-side filesystem exposed to the workspace sandbox.

## Paths checked

No Skyrim SE Data directory was found at these visible paths:

- `C:\Users\user\.steam\steam\steamapps\common\Skyrim Special Edition\Data`
- `C:\Users\user\.local\share\Steam\steamapps\common\Skyrim Special Edition\Data`
- `C:\Program Files (x86)\Steam\steamapps\common\Skyrim Special Edition\Data`
- `C:\Program Files\Steam\steamapps\common\Skyrim Special Edition\Data`
- `D:\SteamLibrary\steamapps\common\Skyrim Special Edition\Data`
- `E:\SteamLibrary\steamapps\common\Skyrim Special Edition\Data`
- `F:\SteamLibrary\steamapps\common\Skyrim Special Edition\Data`

No `libraryfolders.vdf` was found under the visible common roots:

- `C:\Users\user`
- `C:\Program Files (x86)\Steam`
- `C:\Program Files\Steam`
- `C:\Steam`
- `C:\SteamLibrary`
- `C:\Games`
- `C:\store`

No target master or archive files were found under those visible common roots.

## Target files to inventory once Data is visible

Masters:

- `Skyrim.esm`
- `Update.esm`
- `Dawnguard.esm`
- `HearthFires.esm`
- `Dragonborn.esm`

Core/resource archives of interest:

- `Skyrim - Misc.bsa`
- `Skyrim - Patch.bsa`
- `Skyrim - Sounds.bsa`
- `Skyrim - Voices_en0.bsa`
- `Skyrim - Voices_en1.bsa`
- `Skyrim - Voices_en2.bsa`
- `Skyrim - Voices_en3.bsa`
- `Skyrim - Voices_en4.bsa`
- `Dawnguard.bsa`
- `Dragonborn.bsa`
- `HearthFires.bsa`

## Repo CLI capabilities

Existing code can read ESM/ESP via Mutagen:

- `src/ModForge.Core/PluginIo.cs` loads plugins with `SkyrimMod.CreateFromBinary(..., SkyrimRelease.SkyrimSE)`.
- `src/ModForge.Cli/Diagnostics.cs` provides lazy overlay `find <in.esp> <query> [type]`.
- `src/ModForge.Cli/Diagnostics.Dump.cs` provides lazy overlay `dump <in.esp>`.
- `src/ModForge.Cli/Diagnostics.StoryManager.cs` provides `smtree <Skyrim.esm>`.

Existing code can read/extract BSA/BA2 via Mutagen archives:

- `src/ModForge.Core/Archives.cs` has `Archives.List` and `Archives.Extract`.
- `src/ModForge.Cli/Program.Build.Voice.cs` exposes `extract-voices <bsaPath> <voiceType> <outDir>`.

The public CLI currently has targeted voice extraction, not a general archive-list
or general archive-extract command.

## Safe commands once a Data path is known

Set a shell variable first. On Manjaro this is commonly one of:

```sh
DATA="$HOME/.steam/steam/steamapps/common/Skyrim Special Edition/Data"
DATA="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data"
```

Inventory metadata without writing to the game directory:

```sh
stat "$DATA/Skyrim.esm" "$DATA/Update.esm" "$DATA/Dawnguard.esm" "$DATA/HearthFires.esm" "$DATA/Dragonborn.esm"
stat "$DATA"/*.bsa
```

Run read-only ModForge diagnostics:

```sh
dotnet run --project src/ModForge.Cli -- find "$DATA/Skyrim.esm" Riverwood Cell
dotnet run --project src/ModForge.Cli -- smtree "$DATA/Skyrim.esm"
dotnet run --project src/ModForge.Cli -- dump "$DATA/Update.esm" > tmp/vanilla-extract/Update.dump.txt
```

Extract vanilla voice references into the workspace only:

```sh
mkdir -p tmp/vanilla-extract/voices/MaleNord
dotnet run --project src/ModForge.Cli -- extract-voices "$DATA/Skyrim - Voices_en0.bsa" MaleNord tmp/vanilla-extract/voices/MaleNord
```

`extract-voices` requires `ffmpeg` on PATH for `.fuz` audio conversion.

## Needed from user if this session remains Windows-only

Provide the absolute Manjaro Data path, or expose/mount it into this workspace. The
most likely paths are:

- `/home/<user>/.steam/steam/steamapps/common/Skyrim Special Edition/Data`
- `/home/<user>/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data`

