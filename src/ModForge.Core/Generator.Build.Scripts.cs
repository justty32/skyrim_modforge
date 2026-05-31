namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 2: attach Papyrus scripts (VMAD) to any record by editorId ---
        // The VMAD setter is not on the IHaveVirtualMachineAdapter interface (get-only) and its type
        // varies (Quest -> QuestAdapter, most others -> VirtualMachineAdapter), so we reflect the
        // concrete property + create the right adapter. ScriptEntry.Name must match the compiled .pex's
        // Scriptname; typed properties are set in the ESP (Flag.Edited).
        public void AttachScripts()
        {
            foreach (var sa in spec.Scripts)
            {
                if (!recordsByEd.TryGetValue(sa.TargetEditorId, out var target))
                { Warn($"  ! script attach: target '{sa.TargetEditorId}' not found"); continue; }

                var vmadProp = target.GetType().GetProperty("VirtualMachineAdapter");
                if (vmadProp is null || !vmadProp.CanWrite)
                { Warn($"  ! '{sa.TargetEditorId}' ({target.GetType().Name}) takes no script"); continue; }

                var vmad = vmadProp.GetValue(target);
                if (vmad is null)
                {
                    vmad = System.Activator.CreateInstance(vmadProp.PropertyType);
                    vmadProp.SetValue(target, vmad);
                }
                var scriptsList = (System.Collections.IList)vmad!.GetType().GetProperty("Scripts")!.GetValue(vmad)!;

                var entry = new ScriptEntry { Name = sa.ScriptName };
                foreach (var p in sa.Properties)
                {
                    ScriptProperty? sp = (p.Type ?? "").ToLowerInvariant() switch
                    {
                        "int"    => new ScriptIntProperty { Data = p.Int },
                        "float"  => new ScriptFloatProperty { Data = p.Float },
                        "bool"   => new ScriptBoolProperty { Data = p.Bool },
                        "string" => new ScriptStringProperty { Data = p.Str },
                        "object" => MakeObjectProp(p),
                        _        => null,
                    };
                    if (sp is null) { Warn($"  ! script '{sa.ScriptName}' prop '{p.Name}' bad type/ref '{p.Type}'"); continue; }
                    sp.Name = p.Name;
                    sp.Flags = ScriptProperty.Flag.Edited;
                    entry.Properties.Add(sp);
                }
                scriptsList.Add(entry);
                scriptsAttached++;
            }
        }

        private ScriptProperty? MakeObjectProp(PropertySpec p)
        {
            if (string.IsNullOrEmpty(p.ObjectEditorId) || !TryResolveRef(p.ObjectEditorId, formKeyByEd, out var fk))
                return null;
            var op = new ScriptObjectProperty();
            op.Object.SetTo(fk);
            return op;
        }
    }
}
