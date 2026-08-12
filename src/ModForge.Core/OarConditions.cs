using System.Text.Json.Nodes;

namespace ModForge;

// Serializes OarConditionSpec → OAR condition JSON (the exact shape OAR's in-game Author mode
// writes), and expands the npcMoveset sugar into conditions. Pure functions — no Mutagen, no I/O.
// Key names ("Actor base", "Left hand", "Value A", "Random value", "Numeric value") are OAR's
// literal keys, verified against real configs in sub_projs/mod-survey/action-system/findings/.
public static class OarConditions
{
    public const string RequiredVersion = "1.0.0.0";

    // IsEquippedType Type.value enum (OAR standard). Verified against the Animatecc moveset library.
    public static int WeaponType(string name) => (name ?? "").Trim().ToLowerInvariant() switch
    {
        "" or "none" or "fist" or "unarmed" or "h2h" => 0,
        "sword" or "onehandsword" => 1,
        "dagger" => 2,
        "waraxe" or "onehandaxe" or "axe" => 3,
        "mace" or "onehandmace" => 4,
        "greatsword" or "twohandsword" => 5,
        "battleaxe" or "warhammer" or "twohandaxe" => 6,
        "bow" => 7,
        "staff" => 8,
        "crossbow" => 9,
        "shield" => 11,
        "torch" => 12,
        _ => throw new ArgumentException($"unknown weapon type '{name}' (fist/sword/dagger/waraxe/mace/greatsword/battleaxe/warhammer/bow/staff/crossbow/shield/torch)")
    };

    // "Plugin.esp|0x000007" → ("Plugin.esp", "7"). OAR stores formID as a hex string with no 0x
    // prefix and no leading zeros (e.g. "7", "13749"). Accepts ':' as separator too.
    public static (string plugin, string formId) ParseForm(string form)
    {
        var s = (form ?? "").Trim();
        var sep = s.IndexOfAny(new[] { '|', ':' });
        if (sep < 0) throw new ArgumentException($"form ref must be 'Plugin.esp|0xFormID' (got '{form}')");
        var plugin = s[..sep].Trim();
        var id = s[(sep + 1)..].Trim();
        if (id.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) id = id[2..];
        id = id.TrimStart('0');
        if (id.Length == 0) id = "0";
        return (plugin, id.ToUpperInvariant());
    }

    public static JsonObject Emit(OarConditionSpec c)
    {
        var condition = CanonicalConditionName(c.Condition);
        var requiredVersion = condition == "PRESET" ? "2.2.0" : RequiredVersion;
        var o = new JsonObject { ["condition"] = condition, ["requiredVersion"] = requiredVersion };
        if (c.Negated) o["negated"] = true;

        switch (condition)
        {
            case "AND":
            case "OR":
            {
                var arr = new JsonArray();
                foreach (var sub in c.Conditions) arr.Add(Emit(sub));
                o["Conditions"] = arr;
                break;
            }
            case "IsActorBase":
            {
                var (p, f) = ParseForm(c.Form);
                o["Actor base"] = new JsonObject { ["pluginName"] = p, ["formID"] = f };
                break;
            }
            case "IsRace":
            {
                var (p, f) = ParseForm(c.Form);
                o["Race"] = new JsonObject { ["pluginName"] = p, ["formID"] = f };
                break;
            }
            case "IsEquippedType":
                o["Type"] = new JsonObject { ["value"] = c.Type };
                o["Left hand"] = c.LeftHand;
                break;
            case "IsFemale":
                break; // zero-arg condition
            case "Random":
                o["Random value"] = new JsonObject { ["min"] = c.RandomMin, ["max"] = c.RandomMax };
                o["Comparison"] = c.Comparison;
                o["Numeric value"] = new JsonObject { ["value"] = c.Value };
                break;
            case "CompareValues":
                o["Value A"] = new JsonObject
                {
                    ["graphVariable"] = c.GraphVariable,
                    ["graphVariableType"] = c.GraphVariableType,
                };
                o["Comparison"] = c.Comparison;
                o["Value B"] = new JsonObject { ["value"] = c.Value };
                break;
            case "PRESET":
                o["Preset"] = c.Preset;
                break;
            default:
                throw new ArgumentException($"unsupported OAR condition '{c.Condition}'");
        }
        return o;
    }

    private static string CanonicalConditionName(string? value) => (value ?? "").Trim().ToUpperInvariant() switch
    {
        "AND" => "AND",
        "OR" => "OR",
        "ISACTORBASE" => "IsActorBase",
        "ISRACE" => "IsRace",
        "ISEQUIPPEDTYPE" => "IsEquippedType",
        "ISFEMALE" => "IsFemale",
        "RANDOM" => "Random",
        "COMPAREVALUES" => "CompareValues",
        "PRESET" => "PRESET",
        _ => throw new ArgumentException($"unsupported OAR condition '{value}'"),
    };

    public static JsonArray EmitAll(IEnumerable<OarConditionSpec> conditions)
    {
        var arr = new JsonArray();
        foreach (var c in conditions) arr.Add(Emit(c));
        return arr;
    }

    // npcMoveset sugar → the verified condition recipe, wrapped in one AND container (mirrors
    // the real Holmgang submod configs): IsEquippedType(right) + IsEquippedType(left) +
    // [IsActorBase ¬player] + [IsRace] ; plus a sibling Random when randomPick is set.
    public static List<OarConditionSpec> Expand(NpcMovesetSpec m)
    {
        var inner = new List<OarConditionSpec>
        {
            new() { Condition = "IsEquippedType", Type = WeaponType(m.RightWeapon), LeftHand = false },
            new() { Condition = "IsEquippedType", Type = WeaponType(m.LeftWeapon), LeftHand = true },
        };
        if (!m.PlayerOnly)
            inner.Add(new OarConditionSpec { Condition = "IsActorBase", Negated = true, Form = "Skyrim.esm|0x000007" });
        if (!string.IsNullOrWhiteSpace(m.Race))
            inner.Add(new OarConditionSpec { Condition = "IsRace", Form = m.Race });

        var result = new List<OarConditionSpec>
        {
            new() { Condition = "AND", Conditions = inner },
        };
        if (m.RandomPick is float rp)
            result.Insert(0, new OarConditionSpec { Condition = "Random", RandomMin = 0f, RandomMax = 1f, Comparison = "<", Value = rp });
        return result;
    }
}
