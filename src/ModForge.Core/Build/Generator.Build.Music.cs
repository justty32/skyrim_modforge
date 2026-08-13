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
                r.Data = new MusicTypeData { Priority = (ushort)m.Priority, DuckingDecibel = m.DuckingDecibel };
                r.FadeDuration = m.FadeDuration;
            }
        }

        // --- pass 2: MUSC -> MUST track refs, and Palette MUST -> sub-MUST refs. The Tracks list is
        // null on a fresh record — materialize it before appending. ---
        public void WireMusic()
        {
            foreach (var mt in spec.MusicTracks)
                if (mt.Tracks.Count > 0 && recordsByEd.TryGetValue(mt.EditorId, out var rec) && rec is IMusicTrack track)
                {
                    track.Tracks ??= new();
                    foreach (var sub in mt.Tracks)
                        Resolve($"musicTrack '{mt.EditorId}' track", sub, fk => track.Tracks.Add(new FormLink<IMusicTrackGetter>(fk)));
                }
            foreach (var m in spec.Music)
                if (m.Tracks.Count > 0 && recordsByEd.TryGetValue(m.EditorId, out var rec) && rec is IMusicType type)
                {
                    type.Tracks ??= new();
                    foreach (var tr in m.Tracks)
                        Resolve($"music '{m.EditorId}' track", tr, fk => type.Tracks.Add(new FormLink<IMusicTrackGetter>(fk)));
                }
        }

        // --- pass 2: cells[].music -> cell.Music (worldspace music is wired in BuildWorldspaces). ---
        public void WireCellMusic()
        {
            foreach (var c in spec.Cells)
                if (!string.IsNullOrWhiteSpace(c.Music) && cellsByEd.TryGetValue(c.EditorId, out var cell))
                    Resolve($"cell '{c.EditorId}' music", c.Music, fk => cell.Music.SetTo(fk));
        }
    }
}
