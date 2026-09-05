namespace ModForge;

// =====================================================================================
//  XPRM — the primitive volume on a placed reference.
//
//  A REFR can carry a PRIMITIVE: an invisible box / sphere the engine uses as a VOLUME
//  rather than as geometry. It is the mechanism behind Skyrim's trigger boxes ("the player
//  walked in here — fire OnTriggerEnter"), room/portal bounds, and the L_NAVCUT volumes
//  ModForge already authors in navCuts[] (Spec.NavCuts.cs). Same subrecord, three uses.
//
//  Until now ModForge could only emit an XPRM as part of a navcut — hardcoded Box, hardcoded
//  CollisionMarker base, hardcoded yellow. `placements[].primitive` opens it up: ANY placed
//  object can carry ANY primitive, so a spec can finally author a trigger volume.
//
//  --- VERIFIED against Skyrim.esm (SSE 1.6.1170, 2026-09-05) --------------------------
//  13,668 vanilla REFRs carry an XPRM. The families, by base:
//
//    base                                    n     type       colour        opacity
//    RoomMarker        (Skyrim.esm:0x00001F) 3561  Box        (0,128,255)   0.20   room bounds
//    PortalMarker      (Skyrim.esm:0x000020) 2611  PortalBox  (0,0,0)       0.25   room links
//    CollisionMarker   (Skyrim.esm:0x000021) 944+  Box        (255,255,0)   0.15   navcut / collision
//    defaultActivateSelfTRIG (…:0x048AC0)     486  Box        (204,76,51)   0.15   TRIGGER
//    defaultSetStageTRIG     (…:0x033F50)     279  Box        (204,76,51)   0.15   TRIGGER
//    defaultWICommentTRIG    (…:0x04C6EA)     190  Box        (66,196,60)   0.15   TRIGGER
//    defaultAddMusicTrigger  (…:0x04DCAE)     157  Box        (65,217,38)   0.15   TRIGGER
//    WordWallTrigger         (…:0x05095E)      46  Sphere     (255,255,0)   0.15   TRIGGER
//
//  So THE VANILLA TRIGGER RECIPE is: a normal `placements[]` entry whose `base` is an
//  ACTIVATOR (vanilla `defaultActivateSelfTRIG`, or your own ACTI carrying a Papyrus script
//  with OnTriggerEnter — ModForge compiles those, see Spec.Dialogue.cs scripts[]) plus a
//  `primitive` box sized to the volume you want. NO collision layer is set on any of them:
//  the trigger behaviour comes from the ACTI base, the XPRM only says how big the volume is.
//  `collisionLayer` on the placement exists for the cases that DO need one (navcut = 49).
//
//  ⚠️ colour + opacity are CK EDITOR COSMETICS — how the volume is drawn in the Creation Kit
//  render window. They change nothing in-game. The defaults here are the vanilla trigger
//  values so a hand-authored trigger looks like a trigger if it is ever opened in the CK.
//
//  ⚠️ REFR ONLY. An ACHR (an actor) has no primitive; a `primitive` on an `kind: "npc"`
//  placement is a spec mistake and `validate` says so.
// =====================================================================================

/// <summary>
/// An XPRM primitive volume on a placed reference: the shape + size of a trigger / bounds volume.
/// </summary>
public sealed class PrimitiveSpec
{
    /// <summary>
    /// Shape: <c>box</c> (default) | <c>sphere</c> | <c>portalBox</c> | <c>none</c>. A raw
    /// numeric value ("4") is accepted too — Skyrim.esm uses a handful of types Mutagen has no
    /// name for. Case-insensitive; an unknown name is warned about and falls back to Box.
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// FULL size of the volume in game units (width × depth × height), NOT half-extents — the
    /// same convention as navCuts[].size, and the one vanilla uses. The volume is CENTRED on the
    /// placement's <c>position</c>, so put the position at the MIDDLE of the box, not on its floor.
    /// For <c>sphere</c>, all three axes are the diameter and must match: give X alone and Y/Z are
    /// filled from it (vanilla WordWallTrigger stores 4129.57 in all three).
    /// Required — a primitive with no bounds is an inert record.
    /// </summary>
    public Vec3? Bounds { get; set; }

    /// <summary>CK render-window wireframe colour. Default (204,76,51) = the vanilla trigger red.</summary>
    public ColorSpec? Color { get; set; }

    /// <summary>
    /// CK render-window fill opacity, 0..1. Default 0.15 (what every vanilla trigger and navcut
    /// carries). Editor cosmetics only.
    /// </summary>
    public float? Opacity { get; set; }
}
