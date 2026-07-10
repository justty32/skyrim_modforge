namespace ModForge;

// Advisory coordinate anchors from the in-game editor (scene-capture-bridge, Idea #24 P1).
// The unified marker system exports every marker here: a named world position that the USER or an
// AI AGENT reads to author real spec sections in the NEXT round ("raise the terrain at 'hill-top'",
// "place a goat at marker 'goat'"). Build treats them as inert — no records, ever; they are input
// for authoring, not content. Coordinate contract matches PlacementSpec: interior => Cell +
// cell-local position, exterior => Worldspace + world-space position; angles in degrees.
public sealed class AnnotationSpec
{
    public int Seq { get; set; }                    // placement order — ordered kinds (navmesh) rely on it
    public string Label { get; set; } = "";         // free text, renamed in the editor panel
    public string Kind { get; set; } = "note";      // advisory taxonomy: note | navmesh | mapMarker | vfx | tag | ...
    public Vec3 Position { get; set; } = new();
    public float AngleZ { get; set; }               // player facing at placement (degrees)
    public string Cell { get; set; } = "";          // interior: "<master>:0xFORMID"
    public string Worldspace { get; set; } = "";    // exterior: "<master>:0xFORMID"
}
