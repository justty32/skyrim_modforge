using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Verifies OarConditions.Emit/Expand against the exact OAR JSON shapes captured from real configs
// (Holmgang / NAMC / BFCO) in sub_projs/mod-survey/action-system/findings/. Comparison is number-
// aware (1.0 == 1) and order-independent on object keys.
public class OarConditionsTests
{
    // Deep, number-aware JSON equality: objects compared by key set, numbers by double value.
    private static void AssertJsonEqual(JsonNode actual, string expectedJson)
        => Assert.True(NodeEq(actual, JsonNode.Parse(expectedJson)),
            $"JSON mismatch.\n  actual:   {actual.ToJsonString()}\n  expected: {expectedJson}");

    private static bool NodeEq(JsonNode? a, JsonNode? b)
    {
        if (a is JsonObject oa && b is JsonObject ob)
        {
            if (oa.Count != ob.Count) return false;
            foreach (var kv in oa)
            {
                if (!ob.TryGetPropertyValue(kv.Key, out var bv)) return false;
                if (!NodeEq(kv.Value, bv)) return false;
            }
            return true;
        }
        if (a is JsonArray aa && b is JsonArray ba)
        {
            if (aa.Count != ba.Count) return false;
            for (int i = 0; i < aa.Count; i++) if (!NodeEq(aa[i], ba[i])) return false;
            return true;
        }
        if (a is JsonValue && b is JsonValue)
        {
            var ra = a!.ToJsonString();
            var rb = b!.ToJsonString();
            if (ra == rb) return true;
            if (double.TryParse(ra, NumberStyles.Any, CultureInfo.InvariantCulture, out var da)
                && double.TryParse(rb, NumberStyles.Any, CultureInfo.InvariantCulture, out var db))
                return da == db;
            return false;
        }
        return a is null && b is null;
    }

    [Fact]
    public void WeaponType_MapsCanonicalNames()
    {
        Assert.Equal(0, OarConditions.WeaponType("fist"));
        Assert.Equal(1, OarConditions.WeaponType("sword"));
        Assert.Equal(2, OarConditions.WeaponType("dagger"));
        Assert.Equal(5, OarConditions.WeaponType("greatsword"));
        Assert.Equal(6, OarConditions.WeaponType("warhammer"));
        Assert.Equal(11, OarConditions.WeaponType("shield"));
        Assert.Throws<System.ArgumentException>(() => OarConditions.WeaponType("lightsaber"));
    }

    [Fact]
    public void ParseForm_StripsPrefixAndLeadingZeros()
    {
        Assert.Equal(("Skyrim.esm", "7"), OarConditions.ParseForm("Skyrim.esm|0x000007"));
        Assert.Equal(("Skyrim.esm", "13749"), OarConditions.ParseForm("Skyrim.esm|0x013749"));
        Assert.Equal(("My.esp", "801"), OarConditions.ParseForm("My.esp:0x000801"));
    }

    [Fact]
    public void Emit_IsEquippedType_MatchesHolmgangShape()
    {
        var o = OarConditions.Emit(new OarConditionSpec { Condition = "IsEquippedType", Type = 1, LeftHand = false });
        AssertJsonEqual(o, """
            {"condition":"IsEquippedType","requiredVersion":"1.0.0.0","Type":{"value":1.0},"Left hand":false}
            """);
    }

    [Fact]
    public void Emit_IsActorBase_Negated_MatchesHolmgangShape()
    {
        var o = OarConditions.Emit(new OarConditionSpec { Condition = "IsActorBase", Negated = true, Form = "Skyrim.esm|0x000007" });
        AssertJsonEqual(o, """
            {"condition":"IsActorBase","requiredVersion":"1.0.0.0","negated":true,"Actor base":{"pluginName":"Skyrim.esm","formID":"7"}}
            """);
    }

    [Fact]
    public void Emit_NonNegated_OmitsNegatedKey()
    {
        var o = OarConditions.Emit(new OarConditionSpec { Condition = "IsFemale" });
        AssertJsonEqual(o, """{"condition":"IsFemale","requiredVersion":"1.0.0.0"}""");
    }

    [Fact]
    public void Emit_Random_MatchesTweakedShape()
    {
        var o = OarConditions.Emit(new OarConditionSpec { Condition = "Random", RandomMin = 0f, RandomMax = 1f, Comparison = "<", Value = 0.4f });
        AssertJsonEqual(o, """
            {"condition":"Random","requiredVersion":"1.0.0.0","Random value":{"min":0.0,"max":1.0},"Comparison":"<","Numeric value":{"value":0.4}}
            """);
    }

    [Fact]
    public void Emit_CompareValues_MatchesBfcoExample()
    {
        var o = OarConditions.Emit(new OarConditionSpec
        {
            Condition = "CompareValues", GraphVariable = "BFCO_iAttackVariants", GraphVariableType = "Int",
            Comparison = "==", Value = 1f,
        });
        AssertJsonEqual(o, """
            {"condition":"CompareValues","requiredVersion":"1.0.0.0","Value A":{"graphVariable":"BFCO_iAttackVariants","graphVariableType":"Int"},"Comparison":"==","Value B":{"value":1.0}}
            """);
    }

    [Fact]
    public void Emit_AndContainer_NestsChildren()
    {
        var o = OarConditions.Emit(new OarConditionSpec
        {
            Condition = "AND",
            Conditions = new List<OarConditionSpec>
            {
                new() { Condition = "IsEquippedType", Type = 1, LeftHand = false },
                new() { Condition = "IsEquippedType", Type = 11, LeftHand = true },
            },
        });
        AssertJsonEqual(o, """
            {"condition":"AND","requiredVersion":"1.0.0.0","Conditions":[
              {"condition":"IsEquippedType","requiredVersion":"1.0.0.0","Type":{"value":1.0},"Left hand":false},
              {"condition":"IsEquippedType","requiredVersion":"1.0.0.0","Type":{"value":11.0},"Left hand":true}]}
            """);
    }

    [Fact]
    public void Emit_Preset_UsesOar22PresetKey()
    {
        var o = OarConditions.Emit(new OarConditionSpec { Condition = "PRESET", Preset = "PlayerOnly" });
        AssertJsonEqual(o, """{"condition":"PRESET","requiredVersion":"2.2.0","Preset":"PlayerOnly"}""");
    }

    [Fact]
    public void Expand_SwordShieldNpcOnly_ProducesAndOfThree()
    {
        var conds = OarConditions.Expand(new NpcMovesetSpec { RightWeapon = "sword", LeftWeapon = "shield", PlayerOnly = false });
        var single = Assert.Single(conds);
        Assert.Equal("AND", single.Condition);
        Assert.Equal(3, single.Conditions.Count);
        Assert.Equal("IsEquippedType", single.Conditions[0].Condition);
        Assert.Equal(1, single.Conditions[0].Type);
        Assert.Equal(11, single.Conditions[1].Type);
        Assert.True(single.Conditions[1].LeftHand);
        Assert.Equal("IsActorBase", single.Conditions[2].Condition);
        Assert.True(single.Conditions[2].Negated);
    }

    [Fact]
    public void Expand_WithRandomPick_PrependsRandom()
    {
        var conds = OarConditions.Expand(new NpcMovesetSpec { RightWeapon = "battleaxe", LeftWeapon = "shield", PlayerOnly = false, RandomPick = 0.4f });
        Assert.Equal(2, conds.Count);
        Assert.Equal("Random", conds[0].Condition);
        Assert.Equal("AND", conds[1].Condition);
    }
}
