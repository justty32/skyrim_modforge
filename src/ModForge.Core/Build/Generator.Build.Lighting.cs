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
    internal sealed partial class BuildContext
    {
        // Custom LGTM/IMGS built in pass 1 (before cells), so a CELL can resolve them by editorId.
        private readonly Dictionary<string, LightingTemplate> lgtmByEd = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ImageSpace> imgsByEd = new(StringComparer.OrdinalIgnoreCase);

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

        public void BuildImageSpaces()
        {
            foreach (var s in spec.ImageSpaces)
            {
                var img = mod.ImageSpaces.AddNew();
                if (!string.IsNullOrWhiteSpace(s.Template))
                {
                    if (TryResolveTemplate<IImageSpaceGetter>(s.Template, out var tmpl) && tmpl is not null)
                        img.DeepCopyIn(tmpl, out _, null);   // FormKey preserved (EditorID set below)
                    else Warn($"  ! imageSpace '{s.EditorId}' template '{s.Template}' unresolved — using engine defaults");
                }
                img.EditorID = s.EditorId;

                var hdr = img.Hdr ??= new();
                if (s.EyeAdaptSpeed is { } v1) hdr.EyeAdaptSpeed = v1;
                if (s.EyeAdaptStrength is { } v2) hdr.EyeAdaptStrength = v2;
                if (s.BloomBlurRadius is { } v3) hdr.BloomBlurRadius = v3;
                if (s.BloomThreshold is { } v4) hdr.BloomThreshold = v4;
                if (s.BloomScale is { } v5) hdr.BloomScale = v5;
                if (s.ReceiveBloomThreshold is { } v6) hdr.ReceiveBloomThreshold = v6;
                if (s.White is { } v7) hdr.White = v7;
                if (s.SunlightScale is { } v8) hdr.SunlightScale = v8;
                if (s.SkyScale is { } v9) hdr.SkyScale = v9;

                var cin = img.Cinematic ??= new();
                if (s.Brightness is { } b) cin.Brightness = b;
                if (s.Contrast is { } c) cin.Contrast = c;
                if (s.Saturation is { } sat) cin.Saturation = sat;

                var tint = img.Tint ??= new();
                if (s.TintAmount is { } ta) tint.Amount = ta;
                if (s.TintColor is { } tc) tint.Color = ToColor(tc);

                if (!string.IsNullOrEmpty(s.EditorId)) imgsByEd[s.EditorId] = img;
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
