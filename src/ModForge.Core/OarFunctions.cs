using System.Text.Json.Nodes;

namespace ModForge;

// OAR built-in function JSON. The supported small surface is intentionally typed instead of passing
// arbitrary plugin function JSON through: its contract is verified in OAR's Functions/BaseFunctions
// serializer and can therefore be validated before packaging.
public static class OarFunctions
{
    public static JsonObject Emit(OarFunctionSpec f)
    {
        var o = new JsonObject
        {
            ["function"] = f.Function,
            ["requiredVersion"] = f.RequiredVersion,
        };

        switch (f.Function)
        {
            case "CONDITION":
                o["Conditions"] = new JsonObject
                {
                    ["conditions"] = OarConditions.EmitAll(f.Conditions),
                    ["functions"] = EmitAll(f.Functions),
                };
                break;
            case "RANDOM":
                o["Functions"] = EmitAll(f.Functions);
                if (f.Weights.Count > 0)
                {
                    var weights = new JsonArray();
                    foreach (var weight in f.Weights) weights.Add(weight);
                    o["weights"] = weights;
                }
                break;
            case "ONE":
                o["Functions"] = EmitAll(f.Functions);
                break;
            case "PlaySound":
            {
                var (plugin, formId) = OarConditions.ParseForm(f.SoundForm);
                o["Sound FormID"] = new JsonObject { ["pluginName"] = plugin, ["formID"] = formId };
                break;
            }
            default:
                throw new ArgumentException($"unsupported OAR function '{f.Function}'");
        }

        if (f.Triggers.Count > 0)
        {
            var triggers = new JsonArray();
            foreach (var trigger in f.Triggers)
            {
                var entry = new JsonObject { ["event"] = trigger.Event };
                if (!string.IsNullOrEmpty(trigger.Payload)) entry["payload"] = trigger.Payload;
                triggers.Add(entry);
            }
            o["triggers"] = triggers;
        }
        return o;
    }

    public static JsonArray EmitAll(IEnumerable<OarFunctionSpec> functions)
    {
        var result = new JsonArray();
        foreach (var function in functions) result.Add(Emit(function));
        return result;
    }
}
