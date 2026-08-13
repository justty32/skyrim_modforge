using System.Drawing;

namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  LIGHT (LIGT) build.
    //
    //  A fresh Light from AddNew() defaults to radius 0 / fade 0 / no flags. We seed a
    //  vanilla-sane baseline (radius 256, fade 1.0, warm-white colour) so a minimal spec
    //  (just an editorId) is a valid, visible light, then override only what the spec sets.
    //
    //  Colours are authored 0..255 (validate clamps); Mutagen stores System.Drawing.Color.
    //  Flags parse from Light.Flag names; an unknown name warns and is skipped. The record
    //  carries an editorId so the existing placements[] pipeline can place it by editorId.
    // -------------------------------------------------------------------------------

    private static readonly Color BaselineLight = Color.FromArgb(0, 255, 248, 224); // warm white

    private static void BuildLights(ModSpec spec, ISkyrimMod mod, Action<string> warn)
    {
        foreach (var ls in spec.Lights)
        {
            var l = mod.Lights.AddNew();
            l.EditorID = ls.EditorId;
            if (!string.IsNullOrWhiteSpace(ls.Name)) l.Name = ls.Name;

            l.Radius = ls.Radius;
            l.FadeValue = ls.FadeValue;
            l.Color = ls.Color is { } c ? ToColor(c) : BaselineLight;

            // Behaviour flags (Dynamic / Flicker / PortalStrict / CanBeCarried / SpotLight / ...).
            Light.Flag flags = 0;
            foreach (var f in ls.Flags)
                if (Enum.TryParse<Light.Flag>(f, ignoreCase: true, out var fl)) flags |= fl;
                else warn($"  ! light '{ls.EditorId}' unknown flag '{f}' (Dynamic|Flicker|PortalStrict|CanBeCarried|SpotLight|ShadowSpotlight|OffByDefault|FlickerSlow|Pulse|PulseSlow|SpotShadow)");
            l.Flags = flags;

            if (ls.FalloffExponent is { } fe) l.FalloffExponent = fe;
            if (ls.Fov is { } fov) l.FOV = fov;
            if (ls.Value is { } val) l.Value = val;
            if (ls.Weight is { } wt) l.Weight = wt;
        }
    }

    // BuildContext entry point — thin instance wrapper so the orchestrator (Generator.Build.cs)
    // can call this like the other passes.
    private sealed partial class BuildContext
    {
        public void BuildLights() => Generator.BuildLights(spec, mod, Warn);
    }
}
