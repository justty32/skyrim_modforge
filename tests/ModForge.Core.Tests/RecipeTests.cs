using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Regression tests for the smithing/crafting-depth feature: named workbench selectors, recipe-kind
// bench defaults, CTDA conditions (HasPerk/GetItemCount/TemperIsEnchanted, via the SHARED
// ConditionSpec / BuildCondition), and validate guardrails.
//
// These build COBJ records whose createdObject/components are external <master>:0xID or in-spec misc
// items (NOT templated weapons), so they don't need Skyrim.esm present — the recipe wiring is fully
// exercised without the master. (Vanilla FormID shapes were discovered separately via `cobjdiag`.)
public class RecipeTests
{
    private static readonly ModKey OutKey = ModKey.FromNameAndExtension("Test.esp");

    // SteelSmithing perk + IronIngot, harvested via `find Skyrim.esm ... Perk/MiscItem`.
    private const string SteelSmithingPerk = "Skyrim.esm:0x0CB40D";
    private const string IronIngot         = "Skyrim.esm:0x05ACE4";
    private const string SteelIngot        = "Skyrim.esm:0x05ACE5";
    private const string IronOre           = "Skyrim.esm:0x071CF3";

    private const string ForgeKw   = "088105";  // CraftingSmithingForge
    private const string WheelKw   = "088108";  // CraftingSmithingSharpeningWheel
    private const string SmelterKw = "0A5CCE";  // CraftingSmelter

    private static ModSpec SpecWith(params RecipeSpec[] recipes)
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.Recipes.AddRange(recipes);
        return spec;
    }

    private static IConstructibleObjectGetter Cobj(ModSpec spec, string editorId)
    {
        var result = Generator.Build(spec, OutKey);
        var cobj = result.Mod.ConstructibleObjects.FirstOrDefault(c => c.EditorID == editorId);
        Assert.NotNull(cobj);
        return cobj!;
    }

    // ---- Workbench selector ---------------------------------------------------------------------

    [Theory]
    [InlineData("forge", ForgeKw)]
    [InlineData("sharpeningWheel", WheelKw)]
    [InlineData("grindstone", WheelKw)]          // alias
    [InlineData("smelter", SmelterKw)]
    [InlineData("tanningRack", "07866A")]
    [InlineData("armorTable", "0ADB78")]
    public void NamedWorkbench_ResolvesToVanillaKeyword(string name, string expectedId)
    {
        var spec = SpecWith(new RecipeSpec
        {
            EditorId = "R", CreatedObject = IronIngot, Workbench = name,
            Components = { new RecipeComponentSpec { Item = IronOre } },
        });
        var cobj = Cobj(spec, "R");
        Assert.Equal(expectedId, cobj.WorkbenchKeyword.FormKey.ID.ToString("X6"));
    }

    [Fact]
    public void EmptyWorkbench_DefaultsByKind()
    {
        // craft -> forge, temper -> sharpening wheel, smelt -> smelter
        Assert.Equal(ForgeKw,   Cobj(SpecWith(new RecipeSpec { EditorId = "C", Kind = "craft",  CreatedObject = IronIngot, Components = { new() { Item = IronOre } } }), "C").WorkbenchKeyword.FormKey.ID.ToString("X6"));
        Assert.Equal(WheelKw,   Cobj(SpecWith(new RecipeSpec { EditorId = "T", Kind = "temper", CreatedObject = IronIngot, Components = { new() { Item = IronOre } } }), "T").WorkbenchKeyword.FormKey.ID.ToString("X6"));
        Assert.Equal(SmelterKw, Cobj(SpecWith(new RecipeSpec { EditorId = "S", Kind = "smelt",  CreatedObject = IronIngot, Components = { new() { Item = IronOre } } }), "S").WorkbenchKeyword.FormKey.ID.ToString("X6"));
    }

    [Fact]
    public void RawWorkbenchRef_PassesThrough()
    {
        var spec = SpecWith(new RecipeSpec
        {
            EditorId = "R", CreatedObject = IronIngot, Workbench = "Skyrim.esm:0x0F46CE", // skyforge by ref
            Components = { new() { Item = IronOre } },
        });
        Assert.Equal("0F46CE", Cobj(spec, "R").WorkbenchKeyword.FormKey.ID.ToString("X6"));
    }

    // ---- Conditions wired (shared ConditionSpec) ------------------------------------------------

    [Fact]
    public void HasPerkCondition_IsWiredWithPerkAndComparison()
    {
        var spec = SpecWith(new RecipeSpec
        {
            EditorId = "R", CreatedObject = IronIngot, Workbench = "forge",
            Components = { new() { Item = IronOre } },
            Conditions = { new ConditionSpec { Function = "HasPerk", Param = SteelSmithingPerk, Comparison = "==", Value = 1 } },
        });
        var cobj = Cobj(spec, "R");
        var cond = Assert.Single(cobj.Conditions);
        var data = Assert.IsAssignableFrom<IHasPerkConditionDataGetter>(cond.Data);
        Assert.Equal("0CB40D", data.Perk.Link.FormKey.ID.ToString("X6"));
        Assert.Equal(CompareOperator.EqualTo, cond.CompareOperator);
        Assert.Equal(1f, ((IConditionFloatGetter)cond).ComparisonValue);
    }

    [Fact]
    public void GetItemCountCondition_IsWired()
    {
        var spec = SpecWith(new RecipeSpec
        {
            EditorId = "R", Kind = "breakdown", CreatedObject = SteelIngot, Workbench = "smelter",
            Components = { new() { Item = IronIngot } },
            Conditions = { new ConditionSpec { Function = "GetItemCount", Param = IronIngot, Comparison = ">=", Value = 1 } },
        });
        var cobj = Cobj(spec, "R");
        var cond = Assert.Single(cobj.Conditions);
        var data = Assert.IsAssignableFrom<IGetItemCountConditionDataGetter>(cond.Data);
        Assert.Equal("05ACE4", data.ItemOrList.Link.FormKey.ID.ToString("X6"));
        Assert.Equal(CompareOperator.GreaterThanOrEqualTo, cond.CompareOperator);
    }

    // ---- Temper recipe shape (matches vanilla TemperWeaponSteelSword) ---------------------------

    [Fact]
    public void TemperRecipe_HasWheelBench_EnchantGuardOr_AndPerk()
    {
        var spec = SpecWith(new RecipeSpec
        {
            EditorId = "Temper", Kind = "temper", CreatedObject = IronIngot, // external => no temper-type check
            Components = { new() { Item = SteelIngot } },
            Conditions =
            {
                new ConditionSpec { Function = "TemperIsEnchanted", Comparison = "!=", Value = 1, Or = true },
                new ConditionSpec { Function = "HasPerk", Param = SteelSmithingPerk, Comparison = "==", Value = 1 },
            },
        });
        var cobj = Cobj(spec, "Temper");

        Assert.Equal(WheelKw, cobj.WorkbenchKeyword.FormKey.ID.ToString("X6"));
        Assert.Equal(2, cobj.Conditions.Count);

        // First condition: EPTemperingItemIsEnchanted, NotEqualTo, OR-chained (vanilla temper guard).
        var c0 = cobj.Conditions[0];
        Assert.IsAssignableFrom<IEPTemperingItemIsEnchantedConditionDataGetter>(c0.Data);
        Assert.Equal(CompareOperator.NotEqualTo, c0.CompareOperator);
        Assert.True(c0.Flags.HasFlag(Condition.Flag.OR));

        // Second: HasPerk, not OR-chained.
        var c1 = cobj.Conditions[1];
        Assert.IsAssignableFrom<IHasPerkConditionDataGetter>(c1.Data);
        Assert.False(c1.Flags.HasFlag(Condition.Flag.OR));
    }

    // ---- Validate guardrails --------------------------------------------------------------------

    [Fact]
    public void Validate_TemperTargetMustBeWeaponOrArmor()
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.MiscItems.Add(new MiscSpec { EditorId = "Rock", Name = "Rock" });
        spec.Recipes.Add(new RecipeSpec
        {
            EditorId = "BadTemper", Kind = "temper", CreatedObject = "Rock",
            Components = { new() { Item = SteelIngot } },
        });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("BadTemper") && p.Contains("not an in-spec weapon/armor"));
    }

    [Fact]
    public void Validate_TemperTargetWeaponIsAccepted()
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.Weapons.Add(new WeaponSpec { EditorId = "Blade", Name = "Blade", Template = "Skyrim.esm:0x012EB7" });
        spec.Recipes.Add(new RecipeSpec
        {
            EditorId = "GoodTemper", Kind = "temper", CreatedObject = "Blade",
            Components = { new() { Item = SteelIngot } },
        });
        Assert.DoesNotContain(Generator.Validate(spec), p => p.Contains("GoodTemper"));
    }

    [Fact]
    public void Validate_RejectsUnknownKind()
    {
        var spec = SpecWith(new RecipeSpec
        {
            EditorId = "R", Kind = "weld", CreatedObject = IronIngot,
            Components = { new() { Item = IronOre } },
        });
        Assert.Contains(Generator.Validate(spec), p => p.Contains("invalid kind 'weld'"));
    }

    [Fact]
    public void Validate_RejectsUnknownConditionFunction()
    {
        var spec = SpecWith(new RecipeSpec
        {
            EditorId = "R", CreatedObject = IronIngot,
            Components = { new() { Item = IronOre } },
            Conditions = { new ConditionSpec { Function = "Nonsense", Param = "x" } },
        });
        Assert.Contains(Generator.Validate(spec), p => p.Contains("unknown function 'Nonsense'"));
    }

    [Fact]
    public void Validate_RequiresParamForHasPerk()
    {
        var spec = SpecWith(new RecipeSpec
        {
            EditorId = "R", CreatedObject = IronIngot,
            Components = { new() { Item = IronOre } },
            Conditions = { new ConditionSpec { Function = "HasPerk" } },
        });
        Assert.Contains(Generator.Validate(spec), p => p.Contains("needs a param"));
    }

    [Fact]
    public void Validate_TemperIsEnchantedNeedsNoParam()
    {
        var spec = SpecWith(new RecipeSpec
        {
            EditorId = "R", Kind = "temper", CreatedObject = IronIngot, // external createdObject, skips type check
            Components = { new() { Item = SteelIngot } },
            Conditions = { new ConditionSpec { Function = "TemperIsEnchanted", Comparison = "!=" } },
        });
        Assert.DoesNotContain(Generator.Validate(spec), p => p.Contains("needs a param"));
    }
}
