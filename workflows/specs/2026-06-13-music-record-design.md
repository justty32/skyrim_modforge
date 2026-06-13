# Music (MUSC + MUST) records — design

**Date:** 2026-06-13
**Goal:** Let a spec define custom music — Music Tracks (MUST, → audio files) and Music Types (MUSC,
containers that reference tracks) — and assign a Music Type to interior cells and worldspaces, so the
game plays custom music there.

Sub-feature ② of the "new records: Music + Hazard" backlog item (Hazard ① shipped). Independent of it.

## Verified facts (Mutagen 0.49 + Skyrim.esm, this session)

- `MusicTrack` (MUST): `Type` (`MusicTrack.TypeEnum`: `SingleTrack` / `Palette` / `SilentTrack`),
  `TrackFilename` (`AssetLink` → the audio file), `FinaleFilename`, `Duration`, `FadeOut`, `LoopData`
  (`MusicTrackLoopData`: `Begins`, `Ends`, `Count`), `CuePoints`, `Conditions`, `Tracks`
  (`ExtendedList<IFormLink<IMusicTrackGetter>>` — sub-tracks for a Palette).
- `MusicType` (MUSC): `Flags` (`MusicType.Flag`: PlaysOneSelection=1, AbruptTransition=2,
  CycleTracks=4, MaintainTrackOrder=8, DucksCurrentTrack=32, DoesNotQueue=64), `Data`
  (`MusicTypeData`: `Priority`, `DuckingDecibel`), `FadeDuration`, `Tracks`
  (`ExtendedList<IFormLink<IMusicTrackGetter>>` → MUST).
- Groups: `mod.MusicTracks.AddNew()`, `mod.MusicTypes.AddNew()`.
- Assignment FormLinks: `Cell.Music` (exists), `Worldspace.Music` (exists), `Region` has **no** Music.
- **`WorldspaceSpec.Music` already exists AND is already wired** (`Generator.Build.Worldspace.cs:75`
  `Wire(... ws.Music, fk => w.Music.SetTo(fk))`) — so once a `music[]` builder registers MUSC editorIds,
  `worldspaces[].music` resolves an in-spec MUSC for free. No worldspace change needed.
- `CellSpec` has NO music field — add one + wire `cell.Music` in a pass-2 step.
- Pattern to mirror: `BuildExplosions`/`BuildHazards` (pass-1 record) + `Resolve` helper (pass-2 refs).

## Records

### `MusicTrackSpec` (`musicTracks[]`) → MUST

```csharp
public sealed class MusicTrackSpec
{
    public string EditorId { get; set; } = "";
    public string Type { get; set; } = "SingleTrack";   // SingleTrack | Palette | SilentTrack
    public string File { get; set; } = "";              // audio path (e.g. "Music\\MyMod\\theme.xwm") → TrackFilename; empty for SilentTrack
    public float FadeOut { get; set; }                  // optional
    public float Duration { get; set; }                 // optional (SingleTrack length hint)
    public float LoopBegins { get; set; } = -1f;        // -1 = no loop; else loop start seconds
    public float LoopEnds { get; set; } = -1f;          // loop end seconds
    public int LoopCount { get; set; }                  // 0 = infinite when looping
    public List<string> Tracks { get; set; } = new();   // Palette: refs → other MUST editorIds (the pool)
}
```

### `MusicTypeSpec` (`music[]`) → MUSC

```csharp
public sealed class MusicTypeSpec
{
    public string EditorId { get; set; } = "";
    public List<string> Flags { get; set; } = new();    // MusicType.Flag names
    public uint Priority { get; set; }                  // MUSC Data priority (higher wins over lower-priority music)
    public float DuckingDecibel { get; set; }           // MUSC Data ducking dB (negative = quieter)
    public float FadeDuration { get; set; }
    public List<string> Tracks { get; set; } = new();   // refs → MUST editorIds (the type's track list)
}
```

Add both lists to `ModSpec` (`Spec.cs`): `MusicTracks`, `Music`.

## Build

- **`Generator.Build.Music.cs`:**
  - `BuildMusicTracks()` (pass 1): each → `mod.MusicTracks.AddNew()`; set EditorID, `Type`
    (`Enum.TryParse<MusicTrack.TypeEnum>`), `TrackFilename` from `File` (set the `AssetLink` path — confirm
    the exact `AssetLink` setter at plan time; mirror how `Model.File` strings are set), `FadeOut`,
    `Duration`, and `LoopData` (only when `LoopBegins >= 0`: `new MusicTrackLoopData { Begins, Ends, Count }`).
  - `BuildMusicTypes()` (pass 1): each → `mod.MusicTypes.AddNew()`; set EditorID, `Flags`
    (`ParseFlags<MusicType.Flag>`), `Data = new MusicTypeData { Priority, DuckingDecibel }`, `FadeDuration`.
  - `WireMusic()` (pass 2): for each MUST of type Palette, resolve its `Tracks` → `MusicTrack.Tracks`
    (FormLinks); for each MUSC, resolve its `Tracks` → `MusicType.Tracks`. Use `Resolve(...)`; append
    each resolved FormKey as `new FormLink<IMusicTrackGetter>(fk)`.
  - `WireCellMusic()` (pass 2): for each `cells[]` with a `music` ref, resolve it → `cell.Music.SetTo(fk)`
    (look the cell up by editorId in `cellsByEd`). Worldspace music is already wired elsewhere.
- Call `BuildMusicTracks` + `BuildMusicTypes` in pass 1 (before `BuildFormKeyTable`); `WireMusic` +
  `WireCellMusic` in pass 2.

## Assignment

- `cells[].music`: add `public string Music { get; set; } = "";` to `CellSpec`; wired by `WireCellMusic`.
- `worldspaces[].music`: already present and wired — now accepts an in-spec MUSC editorId.

## Audio assets

The audio file is a loose asset under `Data/Music/...` (`.xwm`, or `.wav`). The builder only writes the
path into `TrackFilename`; ship the file via `package --assets` / `spec.assets` (like voice files). A
missing file = silence, no crash. Document this; no conversion in the builder.

## Validate

- Register `musicTracks[]` + `music[]` editorIds (uniqueness).
- MUST: `type` is a known `MusicTrack.TypeEnum`; a `SingleTrack`/`Palette` with no `file`/`tracks`
  warns (SingleTrack needs a file; Palette needs sub-tracks; SilentTrack needs neither); `tracks` refs
  resolve.
- MUSC: `flags` known; a MUSC with no `tracks` warns (plays nothing); `tracks` refs resolve.
- `cells[].music` / `worldspaces[].music` refs resolve (existing CheckRef for worldspace; add for cell).

## Testing (offline)

- `BuildMusicTracks`: a SingleTrack with a file + loop → assert Type, TrackFilename path, LoopData
  Begins/Ends/Count. A Palette with sub-tracks → assert `Tracks` FormKeys after wiring.
- `BuildMusicTypes`: flags + priority + duckingDecibel + fadeDuration; `Tracks` FormKeys match the
  referenced MUST after wiring.
- Assignment: a `cells[].music` → assert the built interior cell's `Music` FormKey == the MUSC;
  a `worldspaces[].music` → assert the built worldspace's `Music` FormKey == the MUSC.
- Validate negatives: unknown track type / flag; SingleTrack with no file warns; MUSC with no tracks
  warns; bad cell music ref.

All offline (forms reference by editorId/FormKey; cell/worldspace built in-spec, no master needed).

## Out of scope (v1)

- MUST `Conditions` and `CuePoints` — YAGNI (add if a use surfaces).
- `FinaleFilename` — rarely authored; skip.
- Region music — `Region` has no Music FormLink; out of scope.
- Audio format conversion (.wav→.xwm) — the user ships the asset.

## Maintenance chain (per CLAUDE.md)

Code (+ example) → `CODE_MAP.dialogue-quests.md`? No — Music is its own domain; put rows in
`CODE_MAP.items-magic.md` (records) or a sound section, and the `cells[].music` note in
`CODE_MAP.world.md`. → `SPEC-packages.md` (it already holds weathers/climates/sound-adjacent) or
`SPEC-world.md` for the cell/worldspace assignment + a music section. → `spec.schema.json`. Pick the
closest existing SPEC file at plan time and keep it consistent. HTML on request only.
