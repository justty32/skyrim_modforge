using System.Linq;

namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Validate — LIGHTING (LGTM / IMGS / inline CELL XCLL) guardrails.
    //
    //  editorId presence/uniqueness is the shared Reg(...) pass; here: colour components
    //  0..255 (LGTM/IMGS/inline colours), a cell's lightingTemplate ref resolves to an
    //  in-spec LGTM editorId OR a vanilla <master>:0xFORMID (and imageSpace to an IMGS
    //  editorId OR external — cross-type ids are rejected), template refs are external-only,
    //  and inline `inherit` flag names parse.
    // -------------------------------------------------------------------------------
    private const string InheritFlagList =
        "AmbientColor|DirectionalColor|FogColor|FogNear|FogFar|DirectionalRotation|DirectionalFade|ClipDistance|FogPower|FogMax|LightFadeDistances";

    private static void ValidateLighting(ModSpec spec, List<string> problems)
    {
        void CheckColor(string owner, string field, ColorSpec? c)
        {
            if (c is null) return;
            foreach (var (v, n) in new[] { (c.R, "r"), (c.G, "g"), (c.B, "b") })
                if (v < 0 || v > 255) problems.Add($"{owner} {field}.{n} = {v} out of range 0..255");
        }
        void CheckAmbient(string owner, string field, AmbientColorsSpec? a)
        {
            if (a is null) return;
            CheckColor(owner, $"{field}.xPlus", a.XPlus); CheckColor(owner, $"{field}.xMinus", a.XMinus);
            CheckColor(owner, $"{field}.yPlus", a.YPlus); CheckColor(owner, $"{field}.yMinus", a.YMinus);
            CheckColor(owner, $"{field}.zPlus", a.ZPlus); CheckColor(owner, $"{field}.zMinus", a.ZMinus);
            CheckColor(owner, $"{field}.specular", a.Specular);
        }

        var lgtmIds = new HashSet<string>(spec.LightingTemplates.Select(x => x.EditorId), StringComparer.OrdinalIgnoreCase);
        var imgsIds = new HashSet<string>(spec.ImageSpaces.Select(x => x.EditorId), StringComparer.OrdinalIgnoreCase);

        foreach (var s in spec.LightingTemplates)
        {
            var o = $"lightingTemplate '{s.EditorId}'";
            if (!string.IsNullOrWhiteSpace(s.Template) && !TryExternalRef(s.Template, out _))
                problems.Add($"{o} template '{s.Template}' must be an external <master>:0xFORMID LGTM ref");
            CheckColor(o, "ambientColor", s.AmbientColor); CheckColor(o, "directionalColor", s.DirectionalColor);
            CheckColor(o, "fogNearColor", s.FogNearColor); CheckColor(o, "fogFarColor", s.FogFarColor);
            CheckAmbient(o, "directionalAmbient", s.DirectionalAmbient);
        }

        foreach (var s in spec.ImageSpaces)
        {
            var o = $"imageSpace '{s.EditorId}'";
            if (!string.IsNullOrWhiteSpace(s.Template) && !TryExternalRef(s.Template, out _))
                problems.Add($"{o} template '{s.Template}' must be an external <master>:0xFORMID IMGS ref");
            CheckColor(o, "tintColor", s.TintColor);
        }

        foreach (var c in spec.Cells)
        {
            var o = $"cell '{c.EditorId}'";
            if (!string.IsNullOrWhiteSpace(c.LightingTemplate)
                && !lgtmIds.Contains(c.LightingTemplate) && !TryExternalRef(c.LightingTemplate, out _))
                problems.Add($"{o} lightingTemplate '{c.LightingTemplate}' unresolved (need an in-spec LightingTemplate editorId or <master>:0xFORMID)");
            if (!string.IsNullOrWhiteSpace(c.ImageSpace)
                && !imgsIds.Contains(c.ImageSpace) && !TryExternalRef(c.ImageSpace, out _))
                problems.Add($"{o} imageSpace '{c.ImageSpace}' unresolved (need an in-spec ImageSpace editorId or <master>:0xFORMID)");
            if (c.Lighting is { } cl)
            {
                CheckColor(o, "lighting.ambientColor", cl.AmbientColor); CheckColor(o, "lighting.directionalColor", cl.DirectionalColor);
                CheckColor(o, "lighting.fogNearColor", cl.FogNearColor); CheckColor(o, "lighting.fogFarColor", cl.FogFarColor);
                CheckAmbient(o, "lighting.directionalAmbient", cl.DirectionalAmbient);
                foreach (var f in cl.Inherit)
                    if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.CellLighting.Inherit>(f, true, out _))
                        problems.Add($"{o} invalid inherit flag '{f}' ({InheritFlagList})");
            }
        }

        foreach (var ws in spec.Weathers)
        {
            var o = $"weather '{ws.EditorId}'";
            if (!string.IsNullOrWhiteSpace(ws.Template) && !TryExternalRef(ws.Template, out _))
                problems.Add($"{o} template '{ws.Template}' must be an external <master>:0xFORMID weather ref");
            if (ws.ImageSpaces is not { } isp) continue;
            foreach (var (slot, r) in new[] { ("default", isp.Default), ("sunrise", isp.Sunrise),
                                              ("day", isp.Day), ("sunset", isp.Sunset), ("night", isp.Night) })
                if (!string.IsNullOrWhiteSpace(r) && !imgsIds.Contains(r) && !TryExternalRef(r, out _))
                    problems.Add($"{o} imageSpace.{slot} '{r}' unresolved (need an in-spec ImageSpace editorId or <master>:0xFORMID)");
        }
    }
}
