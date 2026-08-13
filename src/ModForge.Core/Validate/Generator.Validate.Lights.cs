namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // -------------------------------------------------------------------------------
        //  Validate — LIGHT (LIGT) guardrails.
        //
        //  editorId presence + uniqueness is handled by the shared Reg(...) pass; here we
        //  check flag names parse, the colour components are 0..255, and the radius is
        //  positive (a zero-radius light is invisible).
        // -------------------------------------------------------------------------------
        public void ValidateLights()
        {
            foreach (var ls in spec.Lights)
            {
                var l = $"light '{ls.EditorId}'";

                foreach (var f in ls.Flags)
                    if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.Light.Flag>(f, true, out _))
                        Problems.Add($"{l} invalid flag '{f}' (Dynamic|Flicker|PortalStrict|CanBeCarried|SpotLight|ShadowSpotlight|OffByDefault|FlickerSlow|Pulse|PulseSlow|SpotShadow)");

                if (ls.Color is { } c)
                    foreach (var (v, n) in new[] { (c.R, "r"), (c.G, "g"), (c.B, "b") })
                        if (v < 0 || v > 255)
                            Problems.Add($"{l} color.{n} = {v} out of range 0..255");

                if (ls.Radius == 0)
                    Problems.Add($"{l} radius must be > 0 (a zero-radius light is invisible)");
            }
        }
    }
}
