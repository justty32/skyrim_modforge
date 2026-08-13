namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- pass 1: GlobalVariable (GLOB) — a named shared number (flag/counter/constant) ----------
        // Skyrim has three concrete global subtypes (Short/Long=int/Float); `value` is the INITIAL
        // value. `constant` sets the Constant major-record flag (read-only tuning value). Built in
        // pass 1 (before BuildFormKeyTable) so conditions/regions/etc. can reference it by editorId.
        public void BuildGlobals()
        {
            foreach (var g in spec.Globals)
            {
                Global rec = (g.Type ?? "").Trim().ToLowerInvariant() switch
                {
                    "float"          => MakeGlobalFloat(g.Value),
                    "long" or "int"  => MakeGlobalInt((int)g.Value),
                    _                => MakeGlobalShort((short)g.Value),   // default: short
                };
                rec.EditorID = g.EditorId;
                if (g.Constant) rec.MajorRecordFlagsRaw |= (int)Global.MajorFlag.Constant;
            }
        }

        private GlobalShort MakeGlobalShort(short v) { var r = mod.Globals.AddNewShort(); r.Data = v; return r; }
        private GlobalInt MakeGlobalInt(int v)       { var r = mod.Globals.AddNewInt();   r.Data = v; return r; }
        private GlobalFloat MakeGlobalFloat(float v) { var r = mod.Globals.AddNewFloat(); r.Data = v; return r; }
    }
}
