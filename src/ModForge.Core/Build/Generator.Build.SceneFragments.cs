namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // Attach the generated scene-phase script only when package produced its .pex. The VMAD byte
        // shape and Local flag match vanilla scene fragments; missing compiled output leaves no adapter.
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

                var adapter = scene.VirtualMachineAdapter as SceneAdapter
                              ?? new SceneAdapter { Version = 5, ObjectFormat = 2 };
                adapter.ScriptFragments ??= new SceneScriptFragments();
                adapter.ScriptFragments.FileName = scriptName;
                adapter.ScriptFragments.ExtraBindDataVersion = 2;
                var entry = adapter.Scripts.FirstOrDefault(e =>
                    string.Equals(e.Name, scriptName, StringComparison.OrdinalIgnoreCase));
                if (entry is null)
                {
                    entry = new ScriptEntry { Name = scriptName, Flags = ScriptEntry.Flag.Local };
                    adapter.Scripts.Add(entry);
                }

                foreach (var phase in Generator.SceneFragmentPhases(s))
                    adapter.ScriptFragments.PhaseFragments.Add(new ScenePhaseFragment
                    {
                        Index = (byte)phase,
                        Flags = ScenePhaseFragment.Flag.OnStart,
                        // Vanilla uses 0x01000000; zero makes the engine silently skip the fragment.
                        Unknown = 16777216,
                        ScriptName = scriptName,
                        FragmentName = $"Fragment_{phase}",
                    });

                foreach (var a in Generator.SceneIdleActions(s))
                {
                    var ip = new ScriptObjectProperty
                    {
                        Name = $"Idle_{a.StartPhase}", Flags = ScriptProperty.Flag.Edited,
                    };
                    if (TryResolveRef(a.Idle, formKeyByEd, out var idleFk)) ip.Object.SetTo(idleFk);
                    else Warn($"  ! scene '{s.EditorId}' idle ref '{a.Idle}' unresolved");
                    entry.Properties.Add(ip);

                    var ap = new ScriptObjectProperty
                    {
                        Name = $"Actor_{a.StartPhase}", Flags = ScriptProperty.Flag.Edited,
                    };
                    ap.Object.SetTo(hostQuest.FormKey);
                    ap.Alias = (short)a.Actor;
                    entry.Properties.Add(ap);
                }

                foreach (var (a, actionIndex) in Generator.SceneSetStageActions(s))
                {
                    var target = a.SetStage!.Quest;
                    var qp = new ScriptObjectProperty
                    {
                        Name = $"SetStageQuest_{actionIndex}", Flags = ScriptProperty.Flag.Edited,
                    };
                    if (string.IsNullOrWhiteSpace(target)) qp.Object.SetTo(hostQuest.FormKey);
                    else if (TryResolveRef(target, formKeyByEd, out var questFk)) qp.Object.SetTo(questFk);
                    else Warn($"  ! scene '{s.EditorId}' setStage quest ref '{target}' unresolved");
                    entry.Properties.Add(qp);
                }
                scene.VirtualMachineAdapter = adapter;
                scriptsAttached++;
            }
        }
    }
}
