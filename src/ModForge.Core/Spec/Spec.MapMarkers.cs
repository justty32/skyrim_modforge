using System.Collections.Generic;

namespace ModForge;

// A permanent world-map marker (XMRK on a REFR whose base is the vanilla MapMarker static). Independent
// of any quest, but — being a persistent named REFR — it can be a `forced:<editorId>` alias target, so
// it can double as an objective target. `type` is a MapMarker.MarkerType name (City/Town/Cave/Camp/…,
// None if empty); `flags` are MapMarker.Flag names (Visible | CanTravelTo | ShowAllIsHidden). Empty
// flags = the marker stays hidden until the player discovers it.
public sealed class MapMarkerSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Worldspace { get; set; } = "";
    public Vec3 Position { get; set; } = new();
    public string Type { get; set; } = "";
    public List<string> Flags { get; set; } = new();
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<MapMarkerSpec> MapMarkers { get; set; } = new();   // world-map markers (XMRK on MapMarker static)
}
