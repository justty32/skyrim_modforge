namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- pass 1: TextureSet (TXST) — a set of texture-map paths that retexture a base mesh ---
        // without a new .nif. No FormLinks, so fully built here (an `alternateTextures` consumer
        // references it by editorId, wired in pass 2). Each slot is an OPTIONAL Data-relative
        // Textures\…\*.dds — an unset slot leaves the mesh's original map for that channel. Mutagen's
        // AssetLink is the path wrapper (GivenPath = the verbatim Data-relative string we write; the
        // .dds file itself is user-authored — ModForge only writes the reference).
        public void BuildTextureSets()
        {
            foreach (var tx in spec.TextureSets)
            {
                var r = mod.TextureSets.AddNew();
                r.EditorID = tx.EditorId;
                void Slot(string path, Action<AssetLink<SkyrimTextureAssetType>> set)
                { if (!string.IsNullOrWhiteSpace(path)) set(new AssetLink<SkyrimTextureAssetType>(path.Trim())); }
                Slot(tx.Diffuse,     v => r.Diffuse = v);
                Slot(tx.Normal,      v => r.NormalOrGloss = v);
                Slot(tx.Mask,        v => r.EnvironmentMaskOrSubsurfaceTint = v);
                Slot(tx.Glow,        v => r.GlowOrDetailMap = v);
                Slot(tx.Height,      v => r.Height = v);
                Slot(tx.Environment, v => r.Environment = v);
                Slot(tx.Multilayer,  v => r.Multilayer = v);
                Slot(tx.Backlight,   v => r.BacklightMaskOrSpecular = v);
                if (tx.Flags.Count > 0) r.Flags = ParseFlags<TextureSet.Flag>(tx.Flags);
            }
        }

        // --- pass 2: alternate textures (the TXST consumer) — on a modeled record's Model, each ---
        // entry swaps a named material/sub-mesh inside the base .nif to a TextureSet. The TXST
        // `textureSet` ref is resolved here (may point forward or at a vanilla TXST). A record with no
        // base `model` can't carry alt-textures (nothing to override) — warn rather than silently drop.
        public void WireAlternateTextures()
        {
            void WireAltTextures(string ed, List<AlternateTextureSpec> alts)
            {
                if (alts.Count == 0) return;
                if (!recordsByEd.TryGetValue(ed, out var rec) || rec is not IModeled modeled)
                { Warn($"  ! '{ed}' takes no model/alternateTextures (or not found)"); return; }
                if (modeled.Model is null)
                { Warn($"  ! '{ed}' has alternateTextures but no base model — nothing to retexture (set `model`)"); return; }
                modeled.Model.AlternateTextures ??= new();
                foreach (var alt in alts)
                    Resolve($"'{ed}' alternateTexture '{alt.Name}' textureSet", alt.TextureSet, fk =>
                    {
                        var at = new AlternateTexture { Name = alt.Name, Index = alt.Index };
                        at.NewTexture.SetTo(new FormLink<ITextureSetGetter>(fk));
                        modeled.Model!.AlternateTextures!.Add(at);
                    });
            }
            foreach (var st in spec.Statics) WireAltTextures(st.EditorId, st.AlternateTextures);
            foreach (var ac in spec.Activators) WireAltTextures(ac.EditorId, ac.AlternateTextures);
        }
    }
}
