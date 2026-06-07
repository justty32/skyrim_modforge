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
                if (scriptName == d.ResultScript)
                    FillProperties(entry, d.ResultProperties, scriptName);
                else if (d.SetStage >= 0 && !string.IsNullOrEmpty(d.QuestEditorId))
                {
                    // Auto-generated TIF: bind the OwningQuest property to the quest FormKey so
                    // Fragment_0 can call SetStage() without relying on GetOwningQuest() — that
                    // native method can return None for StartGameEnabled quests at OnBegin time.
                    if (questsByEd.TryGetValue(d.QuestEditorId, out var questRec))
                    {
                        var qp = new ScriptObjectProperty
                        {
                            Name = Generator.TifQuestPropertyName,
                            Flags = ScriptProperty.Flag.Edited,
                        };
                        qp.Object.SetTo(questRec.FormKey);
                        entry.Properties.Add(qp);
                        linksWired++;
                    }
                    else Warn($"  ! TIF '{d.EditorId}': quest '{d.QuestEditorId}' not found — OwningQuest property unset");
                }
                // OnBegin fires the moment the player selects the line (before the NPC speaks).
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

        // --- pass 2: attach the SceneAdapter VMAD + per-phase idle fragments (PlayIdle) ---
        // An idle action (SceneActionSpec.Idle) plays an animation when a phase begins. Skyrim drives
        // this via a SCEN SceneAdapter whose ScenePhaseFragment (OnStart) calls SF_<scene>.Fragment_N,
        // which runs <alias>.GetActorRef().PlayIdle(<idle>). Mirrors WireQuestStages: only attach when
        // the compiled SF_<scene>.pex is present (a VMAD referencing an absent .pex Papyrus-errors at
        // scene begin). `package` pre-compiles GenerateSceneFragmentSource then Build()s with
        // CompiledScriptsDir set. Convention (Index = 0-based phase byte, GetActorRef(), object-property
        // alias binding) decoded from vanilla SF_BardSongsBallad01Scene (Task 0 spike, 2026-06-07).
        public void AttachSceneFragments()
        {
            if (options?.CompiledScriptsDir is not { } compiledDir) return;
            foreach (var s in spec.Scenes)
            {
                if (!Generator.SceneNeedsFragmentScript(s)) continue;
                var scriptName = Generator.SceneFragmentScriptName(s);
                if (!File.Exists(Path.Combine(compiledDir, scriptName + ".pex"))) continue;
                if (!recordsByEd.TryGetValue(s.EditorId, out var rec) || rec is not Scene scene)
                { Warn($"  ! scene fragment: scene '{s.EditorId}' not built"); continue; }
                if (!questsByEd.TryGetValue(s.QuestEditorId, out var hostQuest))
                { Warn($"  ! scene fragment: host quest '{s.QuestEditorId}' for scene '{s.EditorId}' not built"); continue; }

                // MERGE into any existing SceneAdapter (none today, but stay merge-safe like WireQuestStages).
                var adapter = scene.VirtualMachineAdapter as SceneAdapter
                              ?? new SceneAdapter { Version = 5, ObjectFormat = 2 };
                adapter.ScriptFragments ??= new SceneScriptFragments();
                adapter.ScriptFragments.FileName = scriptName;
                var entry = adapter.Scripts.FirstOrDefault(e => string.Equals(e.Name, scriptName, StringComparison.OrdinalIgnoreCase));
                if (entry is null) { entry = new ScriptEntry { Name = scriptName }; adapter.Scripts.Add(entry); }

                foreach (var a in Generator.SceneIdleActions(s))
                {
                    adapter.ScriptFragments.PhaseFragments.Add(new ScenePhaseFragment
                    {
                        Index = (byte)a.StartPhase,                 // 0-based phase index (Task 0 spike)
                        Flags = ScenePhaseFragment.Flag.OnStart,    // fires when the phase begins
                        ScriptName = scriptName,
                        FragmentName = $"Fragment_{a.StartPhase}",  // ↔ GenerateSceneFragmentSource function name
                    });
                    // Idle_<phase> property → the IDLE form.
                    var ip = new ScriptObjectProperty { Name = $"Idle_{a.StartPhase}", Flags = ScriptProperty.Flag.Edited };
                    if (TryResolveRef(a.Idle, formKeyByEd, out var idleFk)) ip.Object.SetTo(idleFk);
                    else Warn($"  ! scene '{s.EditorId}' idle ref '{a.Idle}' unresolved");
                    entry.Properties.Add(ip);
                    // Actor_<phase> property → the host-quest ReferenceAlias (alias index a.Actor). Same
                    // shape as StoryManager AttachAliasScript: Object = quest, Alias = alias index.
                    var ap = new ScriptObjectProperty { Name = $"Actor_{a.StartPhase}", Flags = ScriptProperty.Flag.Edited };
                    ap.Object.SetTo(hostQuest.FormKey);
                    ap.Alias = (short)a.Actor;
                    entry.Properties.Add(ap);
                }
                scene.VirtualMachineAdapter = adapter;
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
