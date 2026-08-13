using System.Drawing;

namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- pass 1: ImageSpace Modifier (IMAD) — a screen-space post-process record. -----------
        // Mutagen models every IMAD field as an animatable curve (ExtendedList<KeyFrame>{Time,Value} /
        // ExtendedList<ColorFrame>{Time,Color}). We want a single static value, so we write ONE keyframe
        // at Time=0 per authored field. Brightness/contrast/saturation default to 1.0 (neutral); >1
        // brightens/boosts. Tint maps colour+amount → one ColorFrame (amount = the colour's alpha).
        // No pass-2: an IMAD holds no outgoing refs; it is reverse-referenced (Explosion.imageSpaceModifier
        // or a Papyrus ImageSpaceModifier property) and auto-registered by BuildFormKeyTable.
        public void BuildImageSpaceModifiers()
        {
            foreach (var im in spec.ImageSpaceModifiers)
            {
                var r = mod.ImageSpaceAdapters.AddNew();
                r.EditorID = im.EditorId;
                r.Duration = im.Duration;
                r.Animatable = im.Animatable;
                // Curves are null on a fresh IMAD — materialize each before adding the single keyframe.
                (r.CinematicBrightnessMult ??= new()).Add(new KeyFrame { Time = 0f, Value = im.BrightnessMultiplier });
                (r.CinematicContrastMult ??= new()).Add(new KeyFrame { Time = 0f, Value = im.Contrast });
                (r.CinematicSaturationMult ??= new()).Add(new KeyFrame { Time = 0f, Value = im.Saturation });
                if (im.TintColor is { } c)
                {
                    int a = (int)Math.Clamp(im.TintAmount * 255f, 0f, 255f);
                    (r.TintColor ??= new()).Add(new ColorFrame
                    {
                        Time = 0f,
                        Color = Color.FromArgb(a, Math.Clamp(c.R, 0, 255), Math.Clamp(c.G, 0, 255), Math.Clamp(c.B, 0, 255)),
                    });
                }
            }
        }
    }
}
