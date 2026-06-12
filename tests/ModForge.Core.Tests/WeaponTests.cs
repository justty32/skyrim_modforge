using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// A templated weapon clones a vanilla weapon (model/anim/equip/keywords) AND must INHERIT its stats
// (damage/value/weight) when the spec leaves them unset (0) — a bug clobbered them to 0, leaving a
// 0-damage sword that NPCs rate below their fists and never draw. Needs Skyrim.esm (template clone).
[Trait("Category", "RequiresSkyrim")]
public class WeaponTests
{
    private static IWeaponGetter Build(WeaponSpec w)
    {
        var spec = new ModSpec { PluginName = "Test.esp", Weapons = { w } };
        return TestBuild.Ok(spec).Mod.EnumerateMajorRecords<IWeaponGetter>().Single(x => x.EditorID == w.EditorId);
    }

    // Templated, no explicit damage -> keeps the iron sword's damage (was clobbered to 0 before the fix).
    [Fact]
    public void TemplatedWeapon_NoDamage_KeepsTemplateDamage()
    {
        var w = Build(new WeaponSpec { EditorId = "MF_W", Name = "Blade", Template = "Skyrim.esm:0x012EB7" });
        Assert.NotNull(w.BasicStats);
        Assert.True(w.BasicStats!.Damage > 0, "templated weapon should inherit the iron sword's damage, not 0");
    }

    // An explicit damage overrides the template's.
    [Fact]
    public void TemplatedWeapon_ExplicitDamage_Overrides()
    {
        var w = Build(new WeaponSpec { EditorId = "MF_W2", Name = "Blade2", Template = "Skyrim.esm:0x012EB7", Damage = 25 });
        Assert.Equal((ushort)25, w.BasicStats!.Damage);
    }
}
