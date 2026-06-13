# Music (MUSC + MUST) records Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add custom Music Tracks (MUST) + Music Types (MUSC) and assign a MUSC to interior cells and worldspaces.

**Architecture:** Two pass-1 record builders (`BuildMusicTracks`/`BuildMusicTypes`, mirroring `BuildHazards`) + a pass-2 `WireMusic` (MUSC→MUST track refs and Palette MUST→MUST sub-refs) + a pass-2 `WireCellMusic` (`cells[].music` → `cell.Music`). `worldspaces[].music` already exists and is already wired.

**Tech Stack:** C#/.NET 10, Mutagen.Bethesda.Skyrim 0.49, xUnit. All offline.

**Verified facts (this session):** `mod.MusicTracks.AddNew()`/`mod.MusicTypes.AddNew()`. `MusicTrack`: `Type` (`MusicTrack.TypeEnum`: SingleTrack/Palette/SilentTrack), `TrackFilename` (`AssetLink<SkyrimMusicAssetType>` — **assign a string directly**, e.g. `t.TrackFilename = "Music\\X\\y.xwm"`, via implicit conversion), `FadeOut`, `Duration`, `LoopData` (`MusicTrackLoopData { Begins, Ends, Count }`), `Tracks` (`ExtendedList<IFormLink<IMusicTrackGetter>>`). `MusicType`: `Flags` (`MusicType.Flag`: PlaysOneSelection/AbruptTransition/CycleTracks/MaintainTrackOrder/DucksCurrentTrack/DoesNotQueue), `Data` (`MusicTypeData { Priority, DuckingDecibel }`), `FadeDuration`, `Tracks` (FormLinks → MUST). `Cell.Music` + `Worldspace.Music` exist; `WorldspaceSpec.Music` already wired at `Generator.Build.Worldspace.cs:75`. Helpers: `ParseFlags<T>`, `Resolve(what, refStr, Action<FormKey>)` (skips empty), `recordsByEd`, `cellsByEd`. Build invocation in tests: `Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod`. Build.cs anchors: pass-1 records around line 55 (`BuildGlobals`) before `BuildFormKeyTable` (77); pass-2 wires near `WireHazards` (91) / `WireCellZones` (113).

---

### Task 1: MUST + MUSC records + WireMusic

**Files:**
- Create: `src/ModForge.Core/Spec.Music.cs`
- Modify: `src/ModForge.Core/Spec.cs` (add `MusicTracks` + `Music` lists)
- Create: `src/ModForge.Core/Generator.Build.Music.cs`
- Modify: `src/ModForge.Core/Generator.Build.cs`
- Test: `tests/ModForge.Core.Tests/MusicTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class MusicTests
{
    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    [Fact]
    public void Track_and_type_build_with_fields_loop_and_track_refs()
    {
        var spec = new ModSpec();
        spec.MusicTracks.Add(new MusicTrackSpec
        {
            EditorId = "MF_Theme", Type = "SingleTrack", File = "Music\\MF\\theme.xwm",
            FadeOut = 2f, LoopBegins = 1.5f, LoopEnds = 30f, LoopCount = 0,
        });
        spec.Music.Add(new MusicTypeSpec
        {
            EditorId = "MF_Explore", Flags = { "CycleTracks", "DoesNotQueue" },
            Priority = 5, DuckingDecibel = -6f, FadeDuration = 4f, Tracks = { "MF_Theme" },
        });
        var mod = Build(spec);
        var t = mod.MusicTracks.Single(x => x.EditorID == "MF_Theme");
        Assert.Equal(MusicTrack.TypeEnum.SingleTrack, t.Type);
        Assert.Equal("Music\\MF\\theme.xwm", t.TrackFilename!.GivenPath);
        Assert.Equal(1.5f, t.LoopData!.Begins);
        Assert.Equal(30f, t.LoopData.Ends);
        var m = mod.MusicTypes.Single(x => x.EditorID == "MF_Explore");
        Assert.True(m.Flags.HasFlag(MusicType.Flag.CycleTracks));
        Assert.Equal(5u, m.Data!.Priority);
        Assert.Equal(-6f, m.Data.DuckingDecibel);
        Assert.Equal(t.FormKey, m.Tracks.Single().FormKey);   // MUSC -> MUST wired in pass 2
    }
}
```

(If `t.TrackFilename.GivenPath` doesn't compile, use `t.TrackFilename!.RawPath` or `t.TrackFilename!.ToString()` — the `AssetLink` exposes the path string under one of these; pick the one that compiles.)

- [ ] **Step 2: Run it — expect FAIL** (no `MusicTrackSpec`/`MusicTypeSpec`).

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~MusicTests"`

- [ ] **Step 3: Add spec types** — create `src/ModForge.Core/Spec.Music.cs`:

```csharp
using System.Collections.Generic;

namespace ModForge;

// A Music Track (MUST): one playable audio entry. `type` SingleTrack = a single `file`; Palette = a
// pool of sub-`tracks` (other MUST) shuffled/cycled; SilentTrack = a timed silence (no file). `file`
// is an audio path under Data/Music (.xwm/.wav), shipped as a loose asset. Loop with
// loopBegins/loopEnds (seconds); loopCount 0 = infinite.
public sealed class MusicTrackSpec
{
    public string EditorId { get; set; } = "";
    public string Type { get; set; } = "SingleTrack";
    public string File { get; set; } = "";
    public float FadeOut { get; set; }
    public float Duration { get; set; }
    public float LoopBegins { get; set; } = -1f;
    public float LoopEnds { get; set; } = -1f;
    public int LoopCount { get; set; }
    public List<string> Tracks { get; set; } = new();   // Palette: refs -> other MUST editorIds
}

// A Music Type (MUSC): a container the game selects between (by `priority`) and assigns to a cell /
// worldspace. References one or more MUST `tracks`. `flags` control selection/transition behaviour;
// `duckingDecibel` lowers other audio while it plays.
public sealed class MusicTypeSpec
{
    public string EditorId { get; set; } = "";
    public List<string> Flags { get; set; } = new();
    public uint Priority { get; set; }
    public float DuckingDecibel { get; set; }
    public float FadeDuration { get; set; }
    public List<string> Tracks { get; set; } = new();   // refs -> MUST editorIds
}
```

Add to `ModSpec` in `Spec.cs` (after `Hazards`):
```csharp
    public List<MusicTrackSpec> MusicTracks { get; set; } = new();
    public List<MusicTypeSpec> Music { get; set; } = new();   // Music Type (MUSC)
```

- [ ] **Step 4: Add the builder** — create `src/ModForge.Core/Generator.Build.Music.cs`:

```csharp
using System;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 1: Music Tracks (MUST) + Music Types (MUSC). Track/type refs wired in pass 2
        // (WireMusic). Built before BuildFormKeyTable so cell/worldspace `music` resolve them. ---
        public void BuildMusicTracks()
        {
            foreach (var mt in spec.MusicTracks)
            {
                var r = mod.MusicTracks.AddNew();
                r.EditorID = mt.EditorId;
                if (Enum.TryParse<MusicTrack.TypeEnum>(mt.Type, true, out var ty)) r.Type = ty;
                if (!string.IsNullOrWhiteSpace(mt.File)) r.TrackFilename = mt.File.Trim();
                r.FadeOut = mt.FadeOut;
                r.Duration = mt.Duration;
                if (mt.LoopBegins >= 0f)
                    r.LoopData = new MusicTrackLoopData { Begins = mt.LoopBegins, Ends = mt.LoopEnds, Count = (byte)mt.LoopCount };
            }
        }

        public void BuildMusicTypes()
        {
            foreach (var m in spec.Music)
            {
                var r = mod.MusicTypes.AddNew();
                r.EditorID = m.EditorId;
                if (m.Flags.Count > 0) r.Flags = ParseFlags<MusicType.Flag>(m.Flags);
                r.Data = new MusicTypeData { Priority = m.Priority, DuckingDecibel = m.DuckingDecibel };
                r.FadeDuration = m.FadeDuration;
            }
        }

        // --- pass 2: MUSC -> MUST track refs, and Palette MUST -> sub-MUST refs. ---
        public void WireMusic()
        {
            foreach (var mt in spec.MusicTracks)
                if (recordsByEd.TryGetValue(mt.EditorId, out var rec) && rec is IMusicTrack track)
                    foreach (var sub in mt.Tracks)
                        Resolve($"musicTrack '{mt.EditorId}' track", sub, fk => track.Tracks.Add(new FormLink<IMusicTrackGetter>(fk)));
            foreach (var m in spec.Music)
                if (recordsByEd.TryGetValue(m.EditorId, out var rec) && rec is IMusicType type)
                    foreach (var tr in m.Tracks)
                        Resolve($"music '{m.EditorId}' track", tr, fk => type.Tracks.Add(new FormLink<IMusicTrackGetter>(fk)));
        }
    }
}
```

(`MusicTrackLoopData.Count` may be `byte` or `int` — the cast `(byte)mt.LoopCount` matches a byte field; if the compiler says it's `int`, drop the cast. Confirm against the type.)

- [ ] **Step 5: Wire into `Generator.Build.cs`.** After `ctx.BuildGlobals();` (~line 55) add:
```csharp
        ctx.BuildMusicTracks();                     // Music Track (MUST) — before BuildFormKeyTable so cell/worldspace music resolve
        ctx.BuildMusicTypes();                      // Music Type (MUSC)
```
After `ctx.WireHazards();` (~line 91) add:
```csharp
        ctx.WireMusic();                            // MUSC -> MUST + Palette MUST -> sub-MUST track FormLinks
```

- [ ] **Step 6: Run the test — expect PASS.**

- [ ] **Step 7: Commit**

```bash
git add src/ModForge.Core/Spec.Music.cs src/ModForge.Core/Spec.cs src/ModForge.Core/Generator.Build.Music.cs src/ModForge.Core/Generator.Build.cs tests/ModForge.Core.Tests/MusicTests.cs
git commit -m "feat(sound): Music Track (MUST) + Music Type (MUSC) records + pass-2 track wiring" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Palette sub-tracks + cell/worldspace assignment

**Files:**
- Modify: `src/ModForge.Core/Spec.World.cs` (add `Music` to `CellSpec`)
- Create: pass-2 `WireCellMusic` in `src/ModForge.Core/Generator.Build.Music.cs`
- Modify: `src/ModForge.Core/Generator.Build.cs` (call `WireCellMusic`)
- Test: `tests/ModForge.Core.Tests/MusicTests.cs` (add cases)

- [ ] **Step 1: Write the failing tests** (append)

```csharp
    [Fact]
    public void Palette_track_references_its_sub_tracks()
    {
        var spec = new ModSpec();
        spec.MusicTracks.Add(new MusicTrackSpec { EditorId = "MF_A", File = "Music\\a.xwm" });
        spec.MusicTracks.Add(new MusicTrackSpec { EditorId = "MF_B", File = "Music\\b.xwm" });
        spec.MusicTracks.Add(new MusicTrackSpec { EditorId = "MF_Pool", Type = "Palette", Tracks = { "MF_A", "MF_B" } });
        var mod = Build(spec);
        var pool = mod.MusicTracks.Single(t => t.EditorID == "MF_Pool");
        Assert.Equal(2, pool.Tracks.Count);
    }

    [Fact]
    public void Music_assigns_to_cell_and_worldspace()
    {
        var spec = new ModSpec();
        spec.MusicTracks.Add(new MusicTrackSpec { EditorId = "MF_T", File = "Music\\t.xwm" });
        spec.Music.Add(new MusicTypeSpec { EditorId = "MF_M", Tracks = { "MF_T" } });
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room", Music = "MF_M" });
        spec.Worldspaces.Add(new WorldspaceSpec { EditorId = "MF_World", Name = "W", Music = "MF_M" });
        var mod = Build(spec);
        var m = mod.MusicTypes.Single(x => x.EditorID == "MF_M");
        var cell = mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(s => s.Cells).Single(c => c.EditorID == "Room");
        Assert.Equal(m.FormKey, cell.Music.FormKey);
        var ws = mod.Worldspaces.Single(w => w.EditorID == "MF_World");
        Assert.Equal(m.FormKey, ws.Music.FormKey);
    }
```

- [ ] **Step 2: Run them — expect FAIL** (Palette passes already from Task 1's WireMusic; the assignment test fails — `CellSpec.Music` doesn't exist).

- [ ] **Step 3: Add `CellSpec.Music`** in `Spec.World.cs` — add `public string Music { get; set; } = "";` to the `CellSpec` class (the single-line class; add `public string Music { get; set; } = ""; // ref -> MUSC` before the closing `}`).

- [ ] **Step 4: Add `WireCellMusic`** to `Generator.Build.Music.cs` (inside `BuildContext`):

```csharp
        // --- pass 2: cells[].music -> cell.Music (worldspace music is wired in BuildWorldspaces). ---
        public void WireCellMusic()
        {
            foreach (var c in spec.Cells)
                if (!string.IsNullOrWhiteSpace(c.Music) && cellsByEd.TryGetValue(c.EditorId, out var cell))
                    Resolve($"cell '{c.EditorId}' music", c.Music, fk => cell.Music.SetTo(fk));
        }
```

(Confirm `cellsByEd` maps in-spec cell editorId → the built `Cell`. If the value type isn't directly `ICell`/`Cell`, adapt the `cell.Music.SetTo` access to the stored type — match how other cell pass-2 wires, e.g. `WireCellZones`, reach the cell.)

Wire it in `Generator.Build.cs` after `ctx.WireCellZones();` (~line 113):
```csharp
        ctx.WireCellMusic();                       // cells[].music -> cell.Music (MUSC)
```

- [ ] **Step 5: Run the tests — expect PASS** (both).

- [ ] **Step 6: Commit**

```bash
git add src/ModForge.Core/Spec.World.cs src/ModForge.Core/Generator.Build.Music.cs src/ModForge.Core/Generator.Build.cs tests/ModForge.Core.Tests/MusicTests.cs
git commit -m "feat(sound): cells[].music assignment + Palette sub-tracks (worldspace music already wired)" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Validation

**Files:**
- Create: `src/ModForge.Core/Generator.Validate.Music.cs`
- Modify: `src/ModForge.Core/Generator.Validate.cs` (register editorIds + call `ValidateMusic`)
- Test: `tests/ModForge.Core.Tests/MusicTests.cs` (add a case)

- [ ] **Step 1: Write the failing test** (append)

```csharp
    [Fact]
    public void Music_validation_flags_bad_type_singletrack_no_file_and_empty_musc()
    {
        var spec = new ModSpec();
        spec.MusicTracks.Add(new MusicTrackSpec { EditorId = "MF_NoFile", Type = "SingleTrack" });
        spec.MusicTracks.Add(new MusicTrackSpec { EditorId = "MF_BadType", Type = "Nonsense", File = "Music\\x.xwm" });
        spec.Music.Add(new MusicTypeSpec { EditorId = "MF_Empty" });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("MF_NoFile") && p.Contains("file"));
        Assert.Contains(problems, p => p.Contains("MF_BadType") && p.Contains("Nonsense"));
        Assert.Contains(problems, p => p.Contains("MF_Empty") && p.Contains("track"));
    }
```

- [ ] **Step 2: Run it — expect FAIL.**

- [ ] **Step 3: Register editorIds.** In `Generator.Validate.cs` registration block (near `spec.Hazards`):
```csharp
            foreach (var mt in spec.MusicTracks) Reg(mt.EditorId, "musicTrack");
            foreach (var m in spec.Music) Reg(m.EditorId, "music");
```
And in the orchestration list (after `ctx.ValidateHazards();`):
```csharp
        ctx.ValidateMusic();
```

- [ ] **Step 4: Create `Generator.Validate.Music.cs`:**

```csharp
using System;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        public void ValidateMusic()
        {
            foreach (var mt in spec.MusicTracks)
            {
                bool known = Enum.TryParse<Mutagen.Bethesda.Skyrim.MusicTrack.TypeEnum>(mt.Type, true, out var ty);
                if (!known) Problems.Add($"musicTrack '{mt.EditorId}' unknown type '{mt.Type}' (SingleTrack | Palette | SilentTrack)");
                else if (ty == Mutagen.Bethesda.Skyrim.MusicTrack.TypeEnum.SingleTrack && string.IsNullOrWhiteSpace(mt.File))
                    Problems.Add($"musicTrack '{mt.EditorId}' is SingleTrack but has no file (silence)");
                else if (ty == Mutagen.Bethesda.Skyrim.MusicTrack.TypeEnum.Palette && mt.Tracks.Count == 0)
                    Problems.Add($"musicTrack '{mt.EditorId}' is Palette but lists no sub-tracks");
                foreach (var sub in mt.Tracks) CheckRef(sub, $"musicTrack '{mt.EditorId}' track");
            }
            foreach (var m in spec.Music)
            {
                if (m.Tracks.Count == 0)
                    Problems.Add($"music '{m.EditorId}' has no tracks — it plays nothing");
                foreach (var tr in m.Tracks) CheckRef(tr, $"music '{m.EditorId}' track");
                foreach (var f in m.Flags)
                    if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.MusicType.Flag>(f, true, out _))
                        Problems.Add($"music '{m.EditorId}' unknown flag '{f}' (PlaysOneSelection | AbruptTransition | CycleTracks | MaintainTrackOrder | DucksCurrentTrack | DoesNotQueue)");
            }
            foreach (var c in spec.Cells) CheckRef(c.Music, $"cell '{c.EditorId}' music");
        }
    }
}
```

- [ ] **Step 5: Run the test — expect PASS** (all three asserts).

- [ ] **Step 6: Commit**

```bash
git add src/ModForge.Core/Generator.Validate.Music.cs src/ModForge.Core/Generator.Validate.cs tests/ModForge.Core.Tests/MusicTests.cs
git commit -m "feat(sound): validate music (track type/file/palette, MUSC tracks, flags, cell ref)" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Worked example

**Files:**
- Create: `examples/music.json`

- [ ] **Step 1: Write the example** — two tracks + a Palette + a MUSC assigned to an interior cell.

```jsonc
{
  "pluginName": "ModForgeMusic.esp",
  "esl": true,
  "cells": [ { "editorId": "MFMU_Room", "name": "Music Test Room", "music": "MFMU_Type" } ],
  "musicTracks": [
    { "editorId": "MFMU_A", "type": "SingleTrack", "file": "Music\\ModForge\\a.xwm", "loopBegins": 0, "loopEnds": 60, "loopCount": 0 },
    { "editorId": "MFMU_B", "type": "SingleTrack", "file": "Music\\ModForge\\b.xwm" },
    { "editorId": "MFMU_Pool", "type": "Palette", "tracks": [ "MFMU_A", "MFMU_B" ] }
  ],
  "music": [
    { "editorId": "MFMU_Type", "flags": ["CycleTracks"], "priority": 10, "duckingDecibel": -6, "fadeDuration": 4,
      "tracks": [ "MFMU_Pool" ] }
  ]
}
```

(The `.xwm` files are NOT shipped here — the example is for the record/assignment structure; in a real mod the files go under `Data/Music/ModForge/` via `package --assets`.)

- [ ] **Step 2: Validate**

Run: `dotnet run --project src/ModForge.Cli -- validate examples/music.json`
Expected: `no problems`.

- [ ] **Step 3: Build**

Run: `dotnet run --project src/ModForge.Cli -- build examples/music.json /tmp/mfmu.esp`
Expected: build succeeds (cell + 3 MUST + 1 MUSC).

- [ ] **Step 4: Commit**

```bash
git add examples/music.json
git commit -m "examples: music — tracks + Palette + a MUSC assigned to a cell" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Maintenance chain — CODE_MAP + SPEC + schema

**Files:**
- Modify: `docs/CODE_MAP.items-magic.md` (MUST/MUSC builder + validate + MusicTests rows)
- Modify: `docs/CODE_MAP.world.md` (cells[].music note)
- Modify: `docs/SPEC-world.md` (music records + cell/worldspace assignment + asset note)
- Modify: `examples/spec.schema.json` (musicTrack + music $defs, root arrays, cells[].music)

- [ ] **Step 1: CODE_MAP.items-magic.md** — add rows for `Spec.Music.cs` (`MusicTrackSpec`/`MusicTypeSpec`), `Generator.Build.Music.cs` (`BuildMusicTracks`/`BuildMusicTypes` pass 1, `WireMusic`/`WireCellMusic` pass 2), `Generator.Validate.Music.cs`; add `MusicTests.cs` to Tests. Note worldspace music reuses the existing `WorldspaceSpec.Music` wire.

- [ ] **Step 2: CODE_MAP.world.md** — note `cells[].music` → `cell.Music` (wired by `WireCellMusic`), and that `worldspaces[].music` accepts an in-spec MUSC.

- [ ] **Step 3: SPEC-world.md** — add a "music (MUSC / MUST)" section: the two record shapes, the three track types, loop fields, flags, `cells[].music` + `worldspaces[].music` assignment, and the loose-asset note (`.xwm` under `Data/Music/`, shipped via `package --assets`). Cross-reference `examples/music.json`.

- [ ] **Step 4: spec.schema.json** — add `musicTrack` + `music` `$defs`, `musicTracks` + `music` arrays on the root, and a `music` property on the cell object def.

- [ ] **Step 5: Full offline regression**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"`
Expected: all green (prior 475 + new Music tests).

- [ ] **Step 6: Commit**

```bash
git add docs/CODE_MAP.items-magic.md docs/CODE_MAP.world.md docs/SPEC-world.md examples/spec.schema.json
git commit -m "docs(music): CODE_MAP + SPEC + schema for MUSC/MUST + cell/worldspace assignment" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Self-review notes

- **Spec coverage:** MUST + MUSC records + WireMusic (Task 1), Palette sub-tracks + cell/worldspace assignment (Task 2), validation (Task 3), example (Task 4), docs/schema (Task 5). Worldspace assignment reuses the already-wired `WorldspaceSpec.Music`.
- **Type consistency:** `MusicTrackSpec.{EditorId,Type,File,FadeOut,Duration,LoopBegins,LoopEnds,LoopCount,Tracks}`, `MusicTypeSpec.{EditorId,Flags,Priority,DuckingDecibel,FadeDuration,Tracks}`, `MusicTrack.TypeEnum`, `MusicType.Flag`, `MusicTypeData.{Priority,DuckingDecibel}`, `MusicTrackLoopData.{Begins,Ends,Count}`, `Cell.Music`, `Worldspace.Music` — all verified against Mutagen 0.49 this session and used identically across tasks.
- **Placeholder scan:** none — every code step concrete. The three "confirm at plan-execution time" notes (`GivenPath`/`RawPath` accessor; `LoopData.Count` byte/int; `cellsByEd` value type) are deliberate signature guards.
- **Offline:** all tasks offline (forms by editorId/FormKey; in-spec cell/worldspace; no master).
