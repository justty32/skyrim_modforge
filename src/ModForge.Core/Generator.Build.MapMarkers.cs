using System;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- world-map markers (XMRK). Each → a persistent PlacedObject on the MapMarker static,
        // carrying a MapMarker subrecord (name + type + flags). Registered in formKeyByEd so a
        // `forced:` alias / linked ref can target it (lets a map marker double as a quest target). ---
        public void BuildMapMarkers()
        {
            const string MapMarkerBase = "Skyrim.esm:0x00000010";
            foreach (var mm in spec.MapMarkers)
            {
                if (string.IsNullOrWhiteSpace(mm.Worldspace))
                { Warn($"  ! mapMarker '{mm.EditorId}': no worldspace — skipped"); continue; }
                // Place the map marker in a regular exterior grid cell's persistent list. (It would more
                // "correctly" live in the worldspace persistent cell alongside the vanilla markers, but
                // overriding that cell crashes the engine — see WorldspaceOverride. A persistent ref in a
                // grid cell still shows on the world map.)
                int cx = PosToGrid(mm.Position.X), cy = PosToGrid(mm.Position.Y);
                var cell = ExteriorCell(mm.Worldspace, cx, cy);
                if (cell is null) { Warn($"  ! mapMarker '{mm.EditorId}': worldspace '{mm.Worldspace}' unresolved — skipped"); continue; }
                if (!TryResolveRef(MapMarkerBase, formKeyByEd, out var baseFk)) continue;

                var marker = new MapMarker();
                if (!string.IsNullOrEmpty(mm.Name)) marker.Name = mm.Name;
                if (!string.IsNullOrWhiteSpace(mm.Type) && Enum.TryParse<MapMarker.MarkerType>(mm.Type, true, out var mt))
                    marker.Type = mt;
                foreach (var f in mm.Flags)
                    if (Enum.TryParse<MapMarker.Flag>(f, true, out var fl)) marker.Flags |= fl;

                var rec = new PlacedObject(mod)
                {
                    Placement = new Placement
                    {
                        Position = new Noggog.P3Float(mm.Position.X, mm.Position.Y, mm.Position.Z),
                        Rotation = new Noggog.P3Float(0, 0, 0),
                    },
                    MapMarker = marker,
                };
                rec.Base.SetTo(baseFk);
                if (!string.IsNullOrWhiteSpace(mm.EditorId))
                {
                    rec.EditorID = mm.EditorId;
                    formKeyByEd[mm.EditorId] = rec.FormKey;
                    recordsByEd[mm.EditorId] = rec;
                }
                cell.Persistent.Add(rec);     // map markers persist
            }
        }
    }
}
