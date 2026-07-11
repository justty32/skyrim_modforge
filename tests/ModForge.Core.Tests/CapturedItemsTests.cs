using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// capturedItems[] — the in-game "definition eyedropper" (Idea #24 addendum). Each entry macro-
// expands (Generator.ExpandCapturedItems) into an ordinary WEAP/ARMO(+minted ENCH)/ALCH/INGR, so the
// battle-tested item build/wire passes do the real work. A player-applied enchant MINTS a fresh ENCH
// from the captured MGEF effects; a durable enchantment.base (a looted pre-enchanted item) is
// REFERENCED. Validation + expansion are master-free; only the template-clone path needs Skyrim.esm.
public class CapturedItemsTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    // A minimal in-spec MGEF so a minted ENCH / potion effect resolves master-free.
    private static MagicEffectSpec Mgef(string ed) => new()
    {
        EditorId = ed, Name = ed, Archetype = "ValueModifier", ActorValue = "Health",
    };

    // --- validation (offline) ---------------------------------------------------------------

    [Fact]
    public void Validate_MissingKind_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedItems.Add(new CapturedItemSpec { Name = "Thing" });
        Assert.Contains(Validate(s), p => p.Contains("capturedItem") && p.Contains("missing kind"));
    }

    [Fact]
    public void Validate_UnknownKind_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedItems.Add(new CapturedItemSpec { Kind = "gizmo", Name = "Thing" });
        Assert.Contains(Validate(s), p => p.Contains("capturedItem") && p.Contains("unknown kind"));
    }

    [Fact]
    public void Validate_PotionNoEffects_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedItems.Add(new CapturedItemSpec { Kind = "potion", Name = "Brew" });
        Assert.Contains(Validate(s), p => p.Contains("capturedItem") && p.Contains("no effects"));
    }

    [Fact]
    public void Validate_GearNoBaseNoEnchant_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedItems.Add(new CapturedItemSpec { Kind = "weapon", Name = "Blade" });
        Assert.Contains(Validate(s), p => p.Contains("capturedItem") && p.Contains("base template or an enchantment"));
    }

    [Fact]
    public void Validate_BadBaseRef_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedItems.Add(new CapturedItemSpec { Kind = "weapon", Name = "Blade", Base = "NotAnExternalRef" });
        Assert.Contains(Validate(s), p => p.Contains("capturedItem") && p.Contains("base"));
    }

    [Fact]
    public void Validate_PlayerEnchantWeapon_NoProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedItems.Add(new CapturedItemSpec
        {
            Kind = "weapon", Name = "Burning Iron Sword", Base = "Skyrim.esm:0x012EB7",
            Enchantment = new CapturedEnchantSpec
            {
                Target = "weapon", Amount = 3000,
                Effects = { new EffectSpec { MagicEffect = "Skyrim.esm:0x0001CEAD", Magnitude = 10 } },
            },
        });
        Assert.DoesNotContain(Validate(s), p => p.Contains("capturedItem"));
    }

    // --- expansion (offline) ----------------------------------------------------------------

    [Fact]
    public void Expand_PlayerEnchantWeapon_MintsEnchAndWeapon()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedItems.Add(new CapturedItemSpec
        {
            Kind = "weapon", Name = "Burning Sword", Base = "Skyrim.esm:0x012EB7",
            Enchantment = new CapturedEnchantSpec
            {
                Target = "weapon", Amount = 3000,
                Effects = { new EffectSpec { MagicEffect = "Skyrim.esm:0x0001CEAD", Magnitude = 10 } },
            },
        });
        Generator.ExpandCapturedItems(s);

        var w = Assert.Single(s.Weapons);
        Assert.Equal("Skyrim.esm:0x012EB7", w.Template);
        Assert.Equal((ushort)3000, w.EnchantmentAmount);
        var ench = Assert.Single(s.Enchantments);
        Assert.Equal(w.Enchantment, ench.EditorId);   // weapon references the minted ENCH by editorId
        Assert.Equal("weapon", ench.EnchantType);
        Assert.Equal("Skyrim.esm:0x0001CEAD", Assert.Single(ench.Effects).MagicEffect);
    }

    [Fact]
    public void Expand_VanillaEnchant_ReferencesNotMints()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedItems.Add(new CapturedItemSpec
        {
            Kind = "weapon", Name = "Looted Frost Sword", Base = "Skyrim.esm:0x012EB7",
            Enchantment = new CapturedEnchantSpec { Target = "weapon", Base = "Skyrim.esm:0x10FB96", Amount = 1500 },
        });
        Generator.ExpandCapturedItems(s);

        Assert.Empty(s.Enchantments);                     // durable ENCH → nothing minted
        var w = Assert.Single(s.Weapons);
        Assert.Equal("Skyrim.esm:0x10FB96", w.Enchantment);  // referenced directly
        Assert.Equal((ushort)1500, w.EnchantmentAmount);
    }

    [Fact]
    public void Expand_Armor_MintsApparelEnch()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedItems.Add(new CapturedItemSpec
        {
            Kind = "armor", Name = "Fortified Robes", Base = "Skyrim.esm:0x012E4D",
            Enchantment = new CapturedEnchantSpec
            {
                Target = "armor",
                Effects = { new EffectSpec { MagicEffect = "Skyrim.esm:0x0003EB24", Magnitude = 40 } },
            },
        });
        Generator.ExpandCapturedItems(s);

        var a = Assert.Single(s.Armors);
        var ench = Assert.Single(s.Enchantments);
        Assert.Equal(a.Enchantment, ench.EditorId);
        Assert.Equal("apparel", ench.EnchantType);   // armor mints an apparel (constant-effect) enchant
    }

    [Fact]
    public void Expand_Potion_FillsEffects()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedItems.Add(new CapturedItemSpec
        {
            Kind = "potion", Name = "Home Brew",
            Effects = { new EffectSpec { MagicEffect = "Skyrim.esm:0x0003EB42", Magnitude = 25, Duration = 60 } },
        });
        Generator.ExpandCapturedItems(s);

        var p = Assert.Single(s.Potions);
        Assert.Equal("Home Brew", p.Name);
        var e = Assert.Single(p.Effects);
        Assert.Equal(25f, e.Magnitude);
        Assert.Equal(60, e.Duration);
    }

    [Fact]
    public void Expand_Ingredient_FillsEffects()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedItems.Add(new CapturedItemSpec
        {
            Kind = "ingredient", Name = "Odd Mushroom",
            Effects = { new EffectSpec { MagicEffect = "Skyrim.esm:0x0003EB42", Magnitude = 5 } },
        });
        Generator.ExpandCapturedItems(s);

        var ing = Assert.Single(s.Ingredients);
        Assert.Equal("Odd Mushroom", ing.Name);
        Assert.Single(ing.Effects);
    }

    [Fact]
    public void Expand_DuplicateNames_UniqueEditorIds()
    {
        // OPEN-D: the DLL can export two rows with the same display name — editorIds must not collide.
        var s = new ModSpec { PluginName = "M.esp" };
        for (int i = 0; i < 2; i++)
            s.CapturedItems.Add(new CapturedItemSpec
            {
                Kind = "potion", Name = "Staff of Magelight",
                Effects = { new EffectSpec { MagicEffect = "Skyrim.esm:0x0003EB42", Magnitude = 1 } },
            });
        Generator.ExpandCapturedItems(s);

        Assert.Equal(2, s.Potions.Count);
        Assert.NotEqual(s.Potions[0].EditorId, s.Potions[1].EditorId);
    }

    [Fact]
    public void Expand_Idempotent()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedItems.Add(new CapturedItemSpec
        {
            Kind = "ingredient", Name = "Mushroom",
            Effects = { new EffectSpec { MagicEffect = "Skyrim.esm:0x0003EB42", Magnitude = 5 } },
        });
        Generator.ExpandCapturedItems(s);
        Generator.ExpandCapturedItems(s);   // guard flag → no double expansion

        Assert.Single(s.Ingredients);
    }

    // --- build (offline: the mint path is master-free with an in-spec MGEF) ------------------

    [Fact]
    public void Build_PlayerEnchantWeapon_MintsEnchAndLinksIt()
    {
        // No base template (master-free) + an in-spec MGEF: the whole mint+wire path builds offline.
        var s = new ModSpec { PluginName = "M.esp", MagicEffects = { Mgef("MF_Cap_Fire") } };
        s.CapturedItems.Add(new CapturedItemSpec
        {
            Kind = "weapon", Name = "Burning Sword", EditorId = "MF_BurnSword",
            Enchantment = new CapturedEnchantSpec
            {
                Target = "weapon", Amount = 3000,
                Effects = { new EffectSpec { MagicEffect = "MF_Cap_Fire", Magnitude = 10 } },
            },
        });
        var r = TestBuild.Raw(s);   // template-less weapon intentionally warns; assert the enchant link
        var ench = r.Mod.EnumerateMajorRecords<IObjectEffectGetter>().Single();
        var weap = r.Mod.EnumerateMajorRecords<IWeaponGetter>().Single();
        Assert.Equal(ench.FormKey, weap.ObjectEffect.FormKey);
        Assert.Equal((ushort)3000, weap.EnchantmentAmount);
    }

    [Fact]
    public void Build_Potion_FromCapturedEffects()
    {
        var s = new ModSpec { PluginName = "M.esp", MagicEffects = { Mgef("MF_Cap_Restore") } };
        s.CapturedItems.Add(new CapturedItemSpec
        {
            Kind = "potion", Name = "Home Brew",
            Effects = { new EffectSpec { MagicEffect = "MF_Cap_Restore", Magnitude = 25 } },
        });
        var r = TestBuild.Ok(s);   // a potion needs no template → clean build
        var alch = r.Mod.Ingestibles.Single();
        Assert.Equal("Home Brew", alch.Name?.String);
        Assert.NotEmpty(alch.Effects);
    }

    // --- build (the template-clone path needs Skyrim.esm) -----------------------------------

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void Build_CapturedWeapon_ClonesTemplate_AndReferencesVanillaEnch()
    {
        var s = new ModSpec { PluginName = "MFCap.esp" };
        s.CapturedItems.Add(new CapturedItemSpec
        {
            Kind = "weapon", Name = "Frost Iron Sword", EditorId = "MF_FrostIron",
            Base = "Skyrim.esm:0x012EB7",   // IronSword — cloned (keeps its damage/model)
            Enchantment = new CapturedEnchantSpec { Target = "weapon", Base = "Skyrim.esm:0x10FB96", Amount = 1500 },
        });
        var r = TestBuild.Ok(s);
        var weap = r.Mod.EnumerateMajorRecords<IWeaponGetter>().Single(w => w.EditorID == "MF_FrostIron");
        Assert.True(weap.BasicStats!.Damage > 0, "cloned iron sword should keep its damage");
        Assert.Equal(FormKey.Factory("10FB96:Skyrim.esm"), weap.ObjectEffect.FormKey);
        Assert.Equal((ushort)1500, weap.EnchantmentAmount);
    }
}
