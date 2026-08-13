namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // Object (Form) script properties whose target builds AFTER the script is attached. An alias-script's
        // properties fill in BuildStandaloneQuestAliases (before placements), so a prop pointing at a
        // placement/xmarker editorId is queued here and resolved by WireDeferredScriptObjectProps.
        private readonly List<(Mutagen.Bethesda.Skyrim.ScriptObjectProperty Prop, string Ref, string Warn)> deferredScriptObjectProps = new();

        // --- pass 2: attach Papyrus scripts (VMAD) to any record by editorId ---
        // The VMAD setter is not on the IHaveVirtualMachineAdapter interface (get-only) and its type
        // varies (Quest -> QuestAdapter, most others -> VirtualMachineAdapter), so we reflect the
        // concrete property + create the right adapter. ScriptEntry.Name must match the compiled .pex's
        // Scriptname; typed properties are set in the ESP (Flag.Edited).
        public void AttachScripts()
        {
            foreach (var sa in spec.Scripts)
                AttachOneScript(sa.TargetEditorId, sa.ScriptName, sa.Properties);
            // Inline MGEF scripts (I組): the target is the effect record itself (targetEditorId implied).
            foreach (var me in spec.MagicEffects)
                foreach (var sa in me.Scripts)
                    AttachOneScript(me.EditorId, sa.ScriptName, sa.Properties);
        }

        // Attach one Papyrus script (by Scriptname) to a record (by editorId) via its VMAD. The setter
        // is not on IHaveVirtualMachineAdapter (get-only) and the adapter type varies (Quest ->
        // QuestAdapter, most -> VirtualMachineAdapter), so reflect the concrete property + create the
        // right adapter. ScriptEntry.Name must match the compiled .pex's Scriptname.
        private void AttachOneScript(string targetEditorId, string scriptName, List<PropertySpec> props)
        {
            if (!recordsByEd.TryGetValue(targetEditorId, out var target))
            { Warn($"  ! script attach: target '{targetEditorId}' not found"); return; }

            var vmadProp = target.GetType().GetProperty("VirtualMachineAdapter");
            if (vmadProp is null || !vmadProp.CanWrite)
            { Warn($"  ! '{targetEditorId}' ({target.GetType().Name}) takes no script"); return; }

            var vmad = vmadProp.GetValue(target);
            if (vmad is null)
            {
                vmad = System.Activator.CreateInstance(vmadProp.PropertyType);
                vmadProp.SetValue(target, vmad);
            }
            var scriptsList = (System.Collections.IList)vmad!.GetType().GetProperty("Scripts")!.GetValue(vmad)!;

            var entry = new ScriptEntry { Name = scriptName };
            FillProperties(entry, props, scriptName);
            scriptsList.Add(entry);
            scriptsAttached++;
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
                // User-supplied ResultScript takes priority; the auto-generated TIF (setStage and/or
                // setPrimaryIdentity override) is the fallback.
                var needsAutoTif = d.SetStage >= 0 || !string.IsNullOrWhiteSpace(d.SetPrimaryIdentity)
                    || d.OpenBarter || d.SetGlobal is not null || !string.IsNullOrWhiteSpace(d.RewardItem)
                    || d.EvaluateSpeakerPackages || Generator.HasPersist(d) || Generator.HasSyncPerks(d)
                    || Generator.HasStorageWrites(d);
                var scriptName = !string.IsNullOrEmpty(d.ResultScript) ? d.ResultScript
                    : (needsAutoTif && options?.CompiledScriptsDir is not null)
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
                else
                {
                    // Auto-generated TIF. SetStage: bind the OwningQuest property to the quest FormKey so
                    // Fragment_0 can call SetStage() without relying on GetOwningQuest() — that native method
                    // can return None for StartGameEnabled quests at OnBegin time.
                    if (d.SetStage >= 0 && !string.IsNullOrEmpty(d.QuestEditorId))
                    {
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
                    // setPrimaryIdentity: bind the MF_IdentityOverride global so Fragment_0 can SetValue(code).
                    if (!string.IsNullOrWhiteSpace(d.SetPrimaryIdentity))
                    {
                        var gp = new ScriptObjectProperty { Name = Generator.IdentityOverrideGlobal, Flags = ScriptProperty.Flag.Edited };
                        if (TryResolveRef(Generator.IdentityOverrideGlobal, formKeyByEd, out var gfk)) gp.Object.SetTo(gfk);
                        else Warn($"  ! TIF '{d.EditorId}': override global '{Generator.IdentityOverrideGlobal}' unresolved");
                        entry.Properties.Add(gp);
                        linksWired++;
                    }
                    // setGlobal: bind the mutable GlobalVariable so Fragment_0 can SetValue/Mod it.
                    if (d.SetGlobal is { } sg && !string.IsNullOrWhiteSpace(sg.Global))
                    {
                        var gp = new ScriptObjectProperty { Name = Generator.TifSetGlobalPropertyName, Flags = ScriptProperty.Flag.Edited };
                        if (TryResolveRef(sg.Global, formKeyByEd, out var gfk)) gp.Object.SetTo(gfk);
                        else Warn($"  ! TIF '{d.EditorId}': setGlobal '{sg.Global}' unresolved");
                        entry.Properties.Add(gp);
                        linksWired++;
                    }
                    // rewardItem: bind the RewardItem form so Fragment_0 can AddItem it to the player.
                    if (!string.IsNullOrWhiteSpace(d.RewardItem))
                    {
                        var rp = new ScriptObjectProperty { Name = Generator.TifRewardPropertyName, Flags = ScriptProperty.Flag.Edited };
                        if (TryResolveRef(d.RewardItem, formKeyByEd, out var rfk)) rp.Object.SetTo(rfk);
                        else Warn($"  ! TIF '{d.EditorId}': rewardItem '{d.RewardItem}' unresolved");
                        entry.Properties.Add(rp);
                        linksWired++;
                    }
                    // persist: bind the Form key (only for an arbitrary-ref key — speaker/player need
                    // none) plus a Form property per FORM-valued write (int/float/str are literals).
                    if (d.Persist is { } pp)
                    {
                        if (Generator.ClassifyPersistKey(pp.Key) == Generator.PersistKeyKind.Ref)
                            BindFormProp(entry, Generator.PersistKeyProperty(""), pp.Key, $"TIF '{d.EditorId}': persist key");
                        foreach (var (i, e) in Generator.PersistFormEntries(pp))
                            BindFormProp(entry, Generator.PersistFormProperty("", i), e.Form, $"TIF '{d.EditorId}': persist form");
                        if (Generator.HasGate(pp.Gate))
                            BindFormProp(entry, Generator.PersistGateProperty(""), pp.Gate!.Global, $"TIF '{d.EditorId}': persist gate");
                    }
                    // syncPerks: bind the Form key (arbitrary ref only) + the PERK form for each node.
                    if (d.SyncPerks is { } sps)
                    {
                        if (Generator.ClassifyPersistKey(sps.Key) == Generator.PersistKeyKind.Ref)
                            BindFormProp(entry, Generator.SyncKeyProperty(""), sps.Key, $"TIF '{d.EditorId}': syncPerks key");
                        for (int i = 0; i < sps.Nodes.Count; i++)
                            BindFormProp(entry, Generator.SyncPerkProperty("", i), sps.Nodes[i].Perk, $"TIF '{d.EditorId}': syncPerks perk");
                        if (Generator.HasGate(sps.Gate))
                            BindFormProp(entry, Generator.SyncGateProperty(""), sps.Gate!.Global, $"TIF '{d.EditorId}': syncPerks gate");
                    }
                    // storageWrites: bind a Form property per arbitrary-ref target (speaker/player/none need none).
                    foreach (var (i, w) in Generator.StorageRefEntries(d.StorageWrites))
                        BindFormProp(entry, Generator.StorageRefProperty("", i), w.Target, $"TIF '{d.EditorId}': storageWrite ref");
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
        // Build typed ScriptProperty entries from a spec property list onto a ScriptEntry (shared by the
        // record-VMAD attach and the dialogue-fragment attach). int/float/bool/string/object; object
        // resolves ObjectEditorId via the formKey table.
        private void FillProperties(ScriptEntry entry, List<PropertySpec> props, string scriptName)
        {
            foreach (var p in props)
            {
                if ((p.Type ?? "").Equals("object", StringComparison.OrdinalIgnoreCase))
                {
                    // Object (Form) props resolve via the formKey table. An alias-script's properties are
                    // filled in BuildStandaloneQuestAliases (BEFORE placements build), so a prop pointing at
                    // a placement/xmarker editorId can't resolve yet — queue it for
                    // WireDeferredScriptObjectProps (after placements), like a deferred forced alias. Props
                    // filled after placements (top-level/dialogue scripts) resolve inline and never queue.
                    if (string.IsNullOrEmpty(p.ObjectEditorId))
                    {
                        Warn($"  ! script '{scriptName}' prop '{p.Name}' object with no objectEditorId");
                        continue;
                    }
                    var op = new ScriptObjectProperty { Name = p.Name, Flags = ScriptProperty.Flag.Edited };
                    if (TryResolveRef(p.ObjectEditorId, formKeyByEd, out var fk))
                        op.Object.SetTo(fk);
                    else
                        deferredScriptObjectProps.Add((op, p.ObjectEditorId!, $"script '{scriptName}' prop '{p.Name}'"));
                    entry.Properties.Add(op);
                    continue;
                }
                ScriptProperty? sp = (p.Type ?? "").ToLowerInvariant() switch
                {
                    "int"    => new ScriptIntProperty { Data = p.Int },
                    "float"  => new ScriptFloatProperty { Data = p.Float },
                    "bool"   => new ScriptBoolProperty { Data = p.Bool },
                    "string" => new ScriptStringProperty { Data = p.Str },
                    _        => null,
                };
                if (sp is null) { Warn($"  ! script '{scriptName}' prop '{p.Name}' bad type/ref '{p.Type}'"); continue; }
                sp.Name = p.Name;
                sp.Flags = ScriptProperty.Flag.Edited;
                entry.Properties.Add(sp);
            }
        }

        // Resolve object (Form) props queued by FillProperties because their target (a placement/xmarker)
        // hadn't been built when the script was attached. Runs after placements exist; mirrors
        // WireDeferredForcedAliases — the queued ScriptObjectProperty is mutated in place on the VMAD.
        public void WireDeferredScriptObjectProps()
        {
            foreach (var (prop, refStr, warn) in deferredScriptObjectProps)
                if (TryResolveRef(refStr, formKeyByEd, out var fk))
                    prop.Object.SetTo(fk);
                else
                    Warn($"  ! {warn} object ref '{refStr}' unresolved");
        }

        // Bind a single Form-typed ScriptObjectProperty (resolving `ref` via the formKey table) onto a
        // ScriptEntry. Shared by the dialogue-TIF and quest-stage JContainers wiring; `warnLabel` is the
        // context prefix for the unresolved-ref warning.
        private void BindFormProp(ScriptEntry entry, string propName, string @ref, string warnLabel)
        {
            var p = new ScriptObjectProperty { Name = propName, Flags = ScriptProperty.Flag.Edited };
            if (TryResolveRef(@ref, formKeyByEd, out var fk)) p.Object.SetTo(fk);
            else Warn($"  ! {warnLabel} '{@ref}' unresolved");
            entry.Properties.Add(p);
            linksWired++;
        }

    }
}
