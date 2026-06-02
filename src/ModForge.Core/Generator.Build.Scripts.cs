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
                FillProperties(entry, sa.Properties, sa.ScriptName);
                scriptsList.Add(entry);
                scriptsAttached++;
            }
        }

        // --- pass 2: attach dialogue RESULT-script fragments (the INFO's OnEnd TIF fragment) ---
        // A DialogResponses VMAD is shaped differently from a normal record's: a DialogResponsesAdapter
        // holding ScriptFragments (the per-INFO fragment) + a Scripts list (the fragment's properties).
        // Vanilla fires Fragment_0(akSpeakerRef) on OnEnd; we mirror that. Runs after BuildFormKeyTable
        // so object properties resolve.
        public void AttachDialogueResultScripts()
        {
            foreach (var d in spec.Dialogue)
            {
                // User-supplied ResultScript takes priority; auto-generated setStage TIF is the fallback.
                var scriptName = !string.IsNullOrEmpty(d.ResultScript) ? d.ResultScript
                    : (d.SetStage >= 0 && options?.CompiledScriptsDir is not null)
                        ? Generator.DialogueFragmentScriptName(d)
                        : null;
                if (string.IsNullOrEmpty(scriptName)) continue;

                // For auto-generated TIF, only attach the VMAD when the compiled .pex is confirmed present.
                if (scriptName == Generator.DialogueFragmentScriptName(d) && scriptName != d.ResultScript)
                {
                    if (!File.Exists(Path.Combine(options!.CompiledScriptsDir!, scriptName + ".pex"))) continue;
                }

                if (!dialogResponsesByEd.TryGetValue(d.EditorId, out var info))
                { Warn($"  ! dialogue result-script: INFO for '{d.EditorId}' not built"); continue; }

                var entry = new ScriptEntry { Name = scriptName };
                if (scriptName == d.ResultScript) FillProperties(entry, d.ResultProperties, scriptName);
                // OnBegin fires the moment the player selects the line (before the NPC speaks).
                // OnEnd fires after voice playback — unreliable for unvoiced custom NPCs.
                // Vanilla CK-generated TIF fragments use OnBegin for quest advancement.
                info.VirtualMachineAdapter = new DialogResponsesAdapter
                {
                    ScriptFragments = new ScriptFragments
                    {
                        FileName = scriptName,
                        OnBegin = new ScriptFragment { ScriptName = scriptName, FragmentName = "Fragment_0" },
                    },
                };
                info.VirtualMachineAdapter.Scripts.Add(entry);
                scriptsAttached++;
            }
        }

        // Build typed ScriptProperty entries from a spec property list onto a ScriptEntry (shared by the
        // record-VMAD attach and the dialogue-fragment attach). int/float/bool/string/object; object
        // resolves ObjectEditorId via the formKey table.
        private void FillProperties(ScriptEntry entry, List<PropertySpec> props, string scriptName)
        {
            foreach (var p in props)
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
                if (sp is null) { Warn($"  ! script '{scriptName}' prop '{p.Name}' bad type/ref '{p.Type}'"); continue; }
                sp.Name = p.Name;
                sp.Flags = ScriptProperty.Flag.Edited;
                entry.Properties.Add(sp);
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
