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
