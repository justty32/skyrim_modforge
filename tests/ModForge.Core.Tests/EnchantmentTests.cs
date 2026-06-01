using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// Enchantment (ENCH / ObjectEffect) coverage: the per-family EnchantType/CastType/TargetType
// mapping (verified against vanilla — frost weapon = Enchantment/FireAndForget/Touch, fortify apparel
// = Enchantment/ConstantEffect/Self, staff = StaffEnchantment/FireAndForget/Aimed), effects wired to
// the right MGEF, weapon/armor ObjectEffect link + weapon charge pool, and the validate guardrails.
public class EnchantmentTests
{
    // A minimal in-spec MGEF so the ENCH effect has a same-plugin BaseEffect to resolve to
    // (keeps everything master-free).
    private static MagicEffectSpec Mgef(string ed) => new()
    {
        EditorId = ed, Name = ed, Archetype = "ValueModifier", ActorValue = "Health",
    };

    private static EnchantmentSpec FrostWeapon() => new()
    {
        EditorId = "MF_FrostEnch", Name = "Frost", EnchantType = "weapon", EnchantmentCost = 15,
        Effects = { new EffectSpec { MagicEffect = "MF_Frost", Magnitude = 10 } },
    };

    [Fact]
    public void WeaponEnchant_MapsTo_Enchantment_FireAndForget_Touch()
    {
        var r = TestBuild.Ok(new ModSpec
        {
            MagicEffects = { Mgef("MF_Frost") },
            Enchantments = { FrostWeapon() },
        });
        var ench = r.Mod.EnumerateMajorRecords<IObjectEffectGetter>().Single();
        Assert.Equal(ObjectEffect.EnchantTypeEnum.Enchantment, ench.EnchantType);
        Assert.Equal(CastType.FireAndForget, ench.CastType);
        Assert.Equal(TargetType.Touch, ench.TargetType);
        Assert.Equal(15u, ench.EnchantmentCost);
    }

    [Fact]
    public void ApparelEnchant_MapsTo_ConstantEffect_Self()
    {
        var r = TestBuild.Ok(new ModSpec
        {
            MagicEffects = { Mgef("MF_Fort") },
            Enchantments =
            {
                new EnchantmentSpec
                {
                    EditorId = "MF_FortEnch", EnchantType = "apparel", EnchantmentCost = 40,
                    Effects = { new EffectSpec { MagicEffect = "MF_Fort", Magnitude = 40 } },
                },
            },
        });
        var ench = r.Mod.EnumerateMajorRecords<IObjectEffectGetter>().Single();
        Assert.Equal(ObjectEffect.EnchantTypeEnum.Enchantment, ench.EnchantType);
        Assert.Equal(CastType.ConstantEffect, ench.CastType);
        Assert.Equal(TargetType.Self, ench.TargetType);
    }

    [Fact]
    public void StaffEnchant_MapsTo_StaffEnchantment_Aimed_WithChargeTime()
    {
        var r = TestBuild.Ok(new ModSpec
        {
            MagicEffects = { Mgef("MF_Bolt") },
            Enchantments =
            {
                new EnchantmentSpec
                {
                    EditorId = "MF_StaffEnch", EnchantType = "staff", EnchantmentCost = 180, ChargeTime = 0.5f,
                    Effects = { new EffectSpec { MagicEffect = "MF_Bolt", Magnitude = 25 } },
                },
            },
        });
        var ench = r.Mod.EnumerateMajorRecords<IObjectEffectGetter>().Single();
        Assert.Equal(ObjectEffect.EnchantTypeEnum.StaffEnchantment, ench.EnchantType);
        Assert.Equal(CastType.FireAndForget, ench.CastType);
        Assert.Equal(TargetType.Aimed, ench.TargetType);
        Assert.Equal(0.5f, ench.ChargeTime);
    }

    [Fact]
    public void CastAndTarget_Overrides_WinOverFamilyDefaults()
    {
        var r = TestBuild.Ok(new ModSpec
        {
            MagicEffects = { Mgef("MF_Frost") },
            Enchantments =
            {
                new EnchantmentSpec
                {
                    EditorId = "MF_Ench", EnchantType = "weapon", CastType = "Concentration", TargetType = "Aimed",
                    Effects = { new EffectSpec { MagicEffect = "MF_Frost", Magnitude = 10 } },
                },
            },
        });
        var ench = r.Mod.EnumerateMajorRecords<IObjectEffectGetter>().Single();
        Assert.Equal(CastType.Concentration, ench.CastType);
        Assert.Equal(TargetType.Aimed, ench.TargetType);
    }

    [Fact]
    public void Ench_Effect_WiredToRightMagicEffect()
    {
        var r = TestBuild.Ok(new ModSpec
        {
            MagicEffects = { Mgef("MF_Frost") },
            Enchantments = { FrostWeapon() },
        });
        var mgef = r.Mod.EnumerateMajorRecords<IMagicEffectGetter>().Single(m => m.EditorID == "MF_Frost");
        var ench = r.Mod.EnumerateMajorRecords<IObjectEffectGetter>().Single();
        var eff = Assert.Single(ench.Effects);
        Assert.Equal(mgef.FormKey, eff.BaseEffect.FormKey);
        Assert.Equal(10f, eff.Data?.Magnitude);
    }

    [Fact]
    public void Armor_ObjectEffect_LinkedToInSpecEnchantment()
    {
        // A template-less armor intentionally warns (equips INVISIBLE — no Armature); use Raw so we
        // still get the build result and can assert on the ObjectEffect link that DOES get wired.
        var r = TestBuild.Raw(new ModSpec
        {
            MagicEffects = { Mgef("MF_Fort") },
            Enchantments =
            {
                new EnchantmentSpec
                {
                    EditorId = "MF_FortEnch", EnchantType = "apparel", EnchantmentCost = 40,
                    Effects = { new EffectSpec { MagicEffect = "MF_Fort", Magnitude = 40 } },
                },
            },
            Armors =
            {
                new ArmorSpec { EditorId = "MF_Cuirass", Name = "Cuirass", ArmorType = "heavy",
                                Slots = { "Body" }, Enchantment = "MF_FortEnch" },
            },
        });
        var ench = r.Mod.EnumerateMajorRecords<IObjectEffectGetter>().Single();
        var armor = r.Mod.EnumerateMajorRecords<IArmorGetter>().Single();
        Assert.Equal(ench.FormKey, armor.ObjectEffect.FormKey);
    }

    [Fact]
    public void Weapon_ObjectEffect_LinkedToInSpecEnchantment_WithChargePool()
    {
        // A weapon with no `template` intentionally warns (model-less); use Raw so we still get the
        // build result and can assert on the enchantment link + charge that DO get wired.
        var r = TestBuild.Raw(new ModSpec
        {
            MagicEffects = { Mgef("MF_Frost") },
            Enchantments = { FrostWeapon() },
            Weapons =
            {
                new WeaponSpec { EditorId = "MF_Sword", Name = "Sword", Damage = 8,
                                 Enchantment = "MF_FrostEnch", EnchantmentAmount = 1500 },
            },
        });
        var ench = r.Mod.EnumerateMajorRecords<IObjectEffectGetter>().Single();
        var weap = r.Mod.EnumerateMajorRecords<IWeaponGetter>().Single();
        Assert.Equal(ench.FormKey, weap.ObjectEffect.FormKey);
        Assert.Equal((ushort)1500, weap.EnchantmentAmount);
    }

    [Fact]
    public void Weapon_VanillaEnchantmentRef_ResolvesToExternalMaster()
    {
        // enchantment may be a vanilla ObjectEffect ref (Skyrim.esm:0x10FB96 EnchWeaponFrostDamageBase).
        var r = TestBuild.Raw(new ModSpec
        {
            Weapons =
            {
                new WeaponSpec { EditorId = "MF_Sword", Name = "Sword", Damage = 8,
                                 Enchantment = "Skyrim.esm:0x10FB96", EnchantmentAmount = 1500 },
            },
        });
        var weap = r.Mod.EnumerateMajorRecords<IWeaponGetter>().Single();
        Assert.False(weap.ObjectEffect.IsNull);
        Assert.Equal("Skyrim.esm", weap.ObjectEffect.FormKey.ModKey.FileName);
        Assert.Equal(0x10FB96u, weap.ObjectEffect.FormKey.ID);
    }

    [Fact]
    public void Armor_NoTemplate_Warns_EquipsInvisible()
    {
        // A bare ARMO (no Armature) equips invisible; build must warn so the modder knows to add a template.
        var r = TestBuild.Raw(new ModSpec
        {
            Armors = { new ArmorSpec { EditorId = "MF_Cuirass", Name = "C", ArmorType = "heavy", Slots = { "Body" } } },
        });
        Assert.Contains(r.Warnings, w => w.Contains("MF_Cuirass") && w.Contains("INVISIBLE"));
        var armor = r.Mod.EnumerateMajorRecords<IArmorGetter>().Single();
        Assert.Empty(armor.Armature); // no template -> no worn mesh
    }

    [Fact]
    public void Armor_ModelOverride_SetsGenderedWorldModel()
    {
        // `model` (no template) still warns invisible, but the ground/world model path must be applied.
        var r = TestBuild.Raw(new ModSpec
        {
            Armors = { new ArmorSpec { EditorId = "MF_Cuirass", Name = "C", Model = "Armor/Custom/Cuirass.nif" } },
        });
        var armor = r.Mod.EnumerateMajorRecords<IArmorGetter>().Single();
        Assert.Equal("Armor/Custom/Cuirass.nif", armor.WorldModel?.Male?.Model?.File.GivenPath);
        Assert.Equal("Armor/Custom/Cuirass.nif", armor.WorldModel?.Female?.Model?.File.GivenPath);
    }

    // ----- validate guardrails -----

    [Fact]
    public void Validate_InvalidEnchantType_IsRejected()
    {
        var problems = Generator.Validate(new ModSpec
        {
            MagicEffects = { Mgef("MF_Frost") },
            Enchantments =
            {
                new EnchantmentSpec { EditorId = "MF_Ench", EnchantType = "ring",
                                      Effects = { new EffectSpec { MagicEffect = "MF_Frost" } } },
            },
        });
        Assert.Contains(problems, p => p.Contains("MF_Ench") && p.Contains("enchantType"));
    }

    [Fact]
    public void Validate_EnchantmentWithNoEffects_IsRejected()
    {
        var problems = Generator.Validate(new ModSpec
        {
            Enchantments = { new EnchantmentSpec { EditorId = "MF_Ench", EnchantType = "weapon" } },
        });
        Assert.Contains(problems, p => p.Contains("MF_Ench") && p.Contains("no effects"));
    }

    [Fact]
    public void Validate_EffectWithUnresolvableMgefRef_IsRejected()
    {
        var problems = Generator.Validate(new ModSpec
        {
            Enchantments =
            {
                new EnchantmentSpec { EditorId = "MF_Ench", EnchantType = "weapon",
                                      Effects = { new EffectSpec { MagicEffect = "DoesNotExist" } } },
            },
        });
        Assert.Contains(problems, p => p.Contains("MF_Ench") && p.Contains("unresolved ref"));
    }

    [Fact]
    public void Validate_ItemEnchantmentRefMustResolve()
    {
        var problems = Generator.Validate(new ModSpec
        {
            Armors = { new ArmorSpec { EditorId = "MF_Cuirass", Name = "C", Enchantment = "NoSuchEnch" } },
        });
        Assert.Contains(problems, p => p.Contains("MF_Cuirass") && p.Contains("enchantment") && p.Contains("unresolved ref"));
    }

    [Fact]
    public void Validate_CleanEnchantmentSpec_HasNoProblems()
    {
        var problems = Generator.Validate(new ModSpec
        {
            MagicEffects = { Mgef("MF_Frost") },
            Enchantments = { FrostWeapon() },
            Armors = { new ArmorSpec { EditorId = "MF_Cuirass", Name = "C", Enchantment = "MF_FrostEnch" } },
        });
        Assert.Empty(problems);
    }
}
