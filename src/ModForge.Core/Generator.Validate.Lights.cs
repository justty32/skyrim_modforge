namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Validate — LIGHT (LIGT) guardrails.
    //
    //  Called from the main Validate() so problems land in the same list. editorId
    //  presence + uniqueness is handled by the shared Reg(...) pass; here we check flag
    //  names parse, the colour components are 0..255, and the radius is positive (a
    //  zero-radius light is invisible).
    // -------------------------------------------------------------------------------
    private static void ValidateLights(ModSpec spec, List<string> problems)
    {
        foreach (var ls in spec.Lights)
        {
            var l = $"light '{ls.EditorId}'";

            foreach (var f in ls.Flags)
                if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.Light.Flag>(f, true, out _))
                    problems.Add($"{l} invalid flag '{f}' (Dynamic|Flicker|PortalStrict|CanBeCarried|SpotLight|ShadowSpotlight|OffByDefault|FlickerSlow|Pulse|PulseSlow|SpotShadow)");

            if (ls.Color is { } c)
                foreach (var (v, n) in new[] { (c.R, "r"), (c.G, "g"), (c.B, "b") })
                    if (v < 0 || v > 255)
                        problems.Add($"{l} color.{n} = {v} out of range 0..255");

            if (ls.Radius == 0)
                problems.Add($"{l} radius must be > 0 (a zero-radius light is invisible)");
        }
    }
}
