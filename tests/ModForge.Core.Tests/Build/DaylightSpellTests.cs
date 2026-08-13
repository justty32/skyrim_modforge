using ModForge;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Core.Tests;

// IMAD (ImageSpace Modifier) builder + the "daylight dungeon" toggle-spell composition.
// Master-free: every ref is an in-spec editorId, so no Skyrim.esm read. Asserts the record SLOTS
// + cross-record WIRING the .esp stores; the actual on-screen brightening is unverifiable headless.
public class DaylightSpellTests
{
    private static readonly ModKey Key = ModKey.FromNameAndExtension("Test.esp");
    private static ISkyrimMod Build(ModSpec spec) => Generator.Build(spec, Key).Mod;

    private static IImageSpaceAdapterGetter ImadOf(ISkyrimMod mod, string ed) =>
        Assert.Single(mod.ImageSpaceAdapters, x => x.EditorID == ed);
    private static ISpellGetter SpellOf(ISkyrimMod mod, string ed) =>
        Assert.Single(mod.Spells, x => x.EditorID == ed);
    private static IMagicEffectGetter MgefOf(ISkyrimMod mod, string ed) =>
        Assert.Single(mod.MagicEffects, x => x.EditorID == ed);

    [Fact]
    public void Imad_Scalar_And_Curve_Fields_Are_Written()
    {
        var mod = Build(new ModSpec
        {
            ImageSpaceModifiers =
            {
                new ImageSpaceModifierSpec
                {
                    EditorId = "MF_Daylight_IMAD",
                    BrightnessMultiplier = 1.6f,
                    Contrast = 1.1f,
                    Saturation = 0.9f,
                    TintColor = new ColorSpec { R = 255, G = 250, B = 235 },
                    TintAmount = 0.5f,
                    Duration = 2.0f,
                    Animatable = false,
                },
            },
        });

        var imad = ImadOf(mod, "MF_Daylight_IMAD");
        Assert.Equal(2.0f, imad.Duration);
        Assert.False(imad.Animatable);
        Assert.Equal(1.6f, Assert.Single(imad.CinematicBrightnessMult).Value);
        Assert.Equal(1.1f, Assert.Single(imad.CinematicContrastMult).Value);
        Assert.Equal(0.9f, Assert.Single(imad.CinematicSaturationMult).Value);
        var tint = Assert.Single(imad.TintColor);
        Assert.Equal(255, tint.Color.R);
        Assert.Equal(250, tint.Color.G);
        Assert.Equal(235, tint.Color.B);
        Assert.Equal(127, tint.Color.A);   // 0.5 * 255 = 127.5 -> 127
    }

    [Fact]
    public void Imad_Without_Tint_Writes_No_ColorFrame()
    {
        var mod = Build(new ModSpec
        {
            ImageSpaceModifiers = { new ImageSpaceModifierSpec { EditorId = "MF_NoTint" } },
        });
        Assert.Null(ImadOf(mod, "MF_NoTint").TintColor);   // no tint authored -> curve never materialized
    }

    // The full toggle-spell shape: a castable Toggle spell (Script MGEF) + a constant Active ability
    // carrying the Light-archetype follow-light (assoc -> LIGT) and the Script imagespace effect.
    [Fact]
    public void Daylight_Toggle_Spell_Composition_Is_Wired()
    {
        var mod = Build(DaylightSpec());

        // Toggle spell: castable, fire-and-forget, self.
        var toggle = SpellOf(mod, "MFDaylightToggle");
        Assert.Equal(SpellType.Spell, toggle.Type);
        Assert.Equal(CastType.FireAndForget, toggle.CastType);
        Assert.Equal(TargetType.Self, toggle.TargetType);
        Assert.Single(toggle.Effects);   // the toggle script effect

        // Active ability: constant-effect ability with TWO effects (light + imagespace).
        var active = SpellOf(mod, "MFDaylightActive");
        Assert.Equal(SpellType.Ability, active.Type);
        Assert.Equal(CastType.ConstantEffect, active.CastType);
        Assert.Equal(2, active.Effects.Count);

        // The Light-archetype MGEF resolves its association to the in-spec LIGT.
        var lightMgef = MgefOf(mod, "MFDaylightLight");
        var arch = (IMagicEffectArchetypeGetter)lightMgef.Archetype;
        Assert.Equal(MagicEffectArchetype.TypeEnum.Light, arch.Type);
        var ligt = Assert.Single(mod.Lights, x => x.EditorID == "MFDaylightLite");
        Assert.Equal(ligt.FormKey, arch.Association.FormKey);
    }

    // Reused by tests above and mirrors examples/daylight_spell_spec.json (script attaches omitted —
    // those need compiled .pex at package time; the build wires records + effects without them).
    private static ModSpec DaylightSpec() => new()
    {
        ImageSpaceModifiers =
        {
            new ImageSpaceModifierSpec { EditorId = "MFDaylightIMAD", BrightnessMultiplier = 1.6f },
        },
        Lights =
        {
            new LightSpec { EditorId = "MFDaylightLite", Radius = 4096, Flags = { "Dynamic" } },
        },
        MagicEffects =
        {
            new MagicEffectSpec { EditorId = "MFDaylightToggleEffect", Archetype = "Script", MagicSkill = "Alteration", CastType = "FireAndForget", TargetType = "Self" },
            new MagicEffectSpec { EditorId = "MFDaylightLight", Archetype = "Light", Association = "MFDaylightLite", CastType = "ConstantEffect", TargetType = "Self" },
            new MagicEffectSpec { EditorId = "MFDaylightVision", Archetype = "Script", CastType = "ConstantEffect", TargetType = "Self" },
        },
        Spells =
        {
            new SpellSpec
            {
                EditorId = "MFDaylightToggle", Name = "Daylight", SpellType = "Spell",
                CastType = "FireAndForget", TargetType = "Self", BaseCost = 150,
                Effects = { new EffectSpec { MagicEffect = "MFDaylightToggleEffect" } },
            },
            new SpellSpec
            {
                EditorId = "MFDaylightActive", SpellType = "Ability", CastType = "ConstantEffect", TargetType = "Self",
                Effects =
                {
                    new EffectSpec { MagicEffect = "MFDaylightLight" },
                    new EffectSpec { MagicEffect = "MFDaylightVision" },
                },
            },
        },
    };
}
