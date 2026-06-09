namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  LightingTemplate (LGTM) + ImageSpace (IMGS) build.
    //
    //  Both follow the template-copy + override model: if `template` resolves to a vanilla
    //  record, DeepCopy it as the base, then overwrite ONLY the fields the spec sets. No
    //  template → a fresh record with engine defaults (a fresh LGTM is a valid, if dim,
    //  record). Built in pass 1 BEFORE BuildCells; the editorId→record map lets a CELL
    //  resolve a custom one by editorId in pass 1 (vanilla refs go through TryResolveTemplate).
    //
    //  DALC mapping (verified against Skyrim.esm): LGTM's directional ambient is
    //  DirectionalAmbientColors (its other AmbientColors field is legacy/zero); CELL XCLL's
    //  is AmbientColors. FillAmbientColors writes whichever Mutagen AmbientColors we hand it.
    // -------------------------------------------------------------------------------
    private sealed partial class BuildContext
    {
        public void BuildLightingTemplates()
        {
            foreach (var s in spec.LightingTemplates)
            {
                var lt = mod.LightingTemplates.AddNew();
                if (!string.IsNullOrWhiteSpace(s.Template))
                {
                    if (TryResolveTemplate<ILightingTemplateGetter>(s.Template, out var tmpl) && tmpl is not null)
                        lt.DeepCopyIn(tmpl, out _, null);   // FormKey preserved (EditorID set below)
                    else Warn($"  ! lightingTemplate '{s.EditorId}' template '{s.Template}' unresolved — using engine defaults");
                }
                lt.EditorID = s.EditorId;

                if (s.AmbientColor is { } ac) lt.AmbientColor = ToColor(ac);
                if (s.DirectionalColor is { } dc) lt.DirectionalColor = ToColor(dc);
                if (s.DirectionalRotationXY is { } rxy) lt.DirectionalRotationXY = rxy;
                if (s.DirectionalRotationZ is { } rz) lt.DirectionalRotationZ = rz;
                if (s.DirectionalFade is { } df) lt.DirectionalFade = df;
                if (s.FogNearColor is { } fnc) lt.FogNearColor = ToColor(fnc);
                if (s.FogFarColor is { } ffc) lt.FogFarColor = ToColor(ffc);
                if (s.FogNear is { } fn) lt.FogNear = fn;
                if (s.FogFar is { } ff) lt.FogFar = ff;
                if (s.FogMax is { } fm) lt.FogMax = fm;
                if (s.FogClipDistance is { } fcd) lt.FogClipDistance = fcd;
                if (s.FogPower is { } fp) lt.FogPower = fp;
                if (s.LightFadeStart is { } lfs) lt.LightFadeStartDistance = lfs;
                if (s.LightFadeEnd is { } lfe) lt.LightFadeEndDistance = lfe;
                if (s.DirectionalAmbient is { } da)
                    FillAmbientColors(lt.DirectionalAmbientColors ??= new(), da);

                if (!string.IsNullOrEmpty(s.EditorId)) lgtmByEd[s.EditorId] = lt;
            }
        }

        // Overwrite only the AmbientColors sub-fields the spec sets (DALC: 6 directions + specular + scale).
        private static void FillAmbientColors(Mutagen.Bethesda.Skyrim.AmbientColors dst, AmbientColorsSpec src)
        {
            if (src.XPlus   is { } xp)  dst.DirectionalXPlus  = ToColor(xp);
            if (src.XMinus  is { } xm)  dst.DirectionalXMinus = ToColor(xm);
            if (src.YPlus   is { } yp)  dst.DirectionalYPlus  = ToColor(yp);
            if (src.YMinus  is { } ym)  dst.DirectionalYMinus = ToColor(ym);
            if (src.ZPlus   is { } zp)  dst.DirectionalZPlus  = ToColor(zp);
            if (src.ZMinus  is { } zm)  dst.DirectionalZMinus = ToColor(zm);
            if (src.Specular is { } sp) dst.Specular           = ToColor(sp);
            if (src.Scale   is { } sc)  dst.Scale              = sc;
        }
    }
}
