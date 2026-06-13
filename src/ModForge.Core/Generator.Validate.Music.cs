using System;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // Music Track (MUST) + Music Type (MUSC): type/flag validity, the empty-content warnings
        // (SingleTrack needs a file, Palette needs sub-tracks, MUSC needs tracks), and ref integrity.
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
