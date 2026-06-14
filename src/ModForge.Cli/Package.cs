internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  package — build the .esp, compile any script sources, and lay out an MO2/Vortex-
    //  ready mod folder: <outModDir>/<PluginName> + Scripts/*.pex + Scripts/Source/*.psc.
    //  Shares ReadSpec/BuildSummary/WriteSeq with the other commands in Program.cs.
    // -------------------------------------------------------------------------------
    private static int PackageCmd(string specPath, string outModDir, string? assetsOverride)
    {
        var spec = ReadSpec(specPath);
        var pluginName = string.IsNullOrEmpty(spec.PluginName) ? "Generated.esp" : spec.PluginName;
        Directory.CreateDirectory(outModDir);

        var scriptsDir = Path.Combine(outModDir, "Scripts");
        var sourceDir = Path.Combine(scriptsDir, "Source");
        var specDir = Path.GetDirectoryName(Path.GetFullPath(specPath)) ?? ".";

        // 1) Pre-compile generated quest/dialogue fragment .psc files so the VMAD can be attached
        //    in Build(). These .psc files are generated from the spec (no user authoring required):
        //    * Quest stage→objective fragments: Fragment_Stage_XXXX_Item00000 functions that call
        //      SetObjectiveDisplayed/Completed — makes quests appear in the Active Quests journal.
        //    * Dialogue set-stage fragments: Fragment_0(akSpeakerRef) that calls SetStage(N) when a
        //      dialogue line is picked — advances quest stage without CK scripting.
        //    Both are compiled into a temp dir; Build() checks that dir and only attaches the VMAD
        //    when the .pex is confirmed present (absent .pex → Papyrus error at quest-start).
        var compiledFragmentsDir = Path.Combine(Path.GetTempPath(), "modforge_fragments_" + Path.GetRandomFileName());
        Directory.CreateDirectory(compiledFragmentsDir);
        int autoCompiled = 0;

        void CompileGenerated(string pscSource, string scriptName, string label)
        {
            Directory.CreateDirectory(sourceDir);
            var pscPath = Path.Combine(compiledFragmentsDir, scriptName + ".psc");
            File.WriteAllText(pscPath, pscSource);
            var cr = Papyrus.CompileBest(pscPath, compiledFragmentsDir);
            if (cr.Success)
            {
                Console.WriteLine($"  compiled {label} -> {scriptName}.pex");
                autoCompiled++;
            }
            else
                Console.WriteLine($"  (auto-compile skipped for {label}: {cr.Message.Split('\n')[0]})");
            // Always write the .psc to Scripts/Source for the author's reference.
            File.Copy(pscPath, Path.Combine(sourceDir, scriptName + ".psc"), overwrite: true);
        }

        foreach (var q in spec.Quests)
        {
            var src = Generator.GenerateQuestFragmentSource(q);
            if (!string.IsNullOrEmpty(src))
                CompileGenerated(src, Generator.QuestFragmentScriptName(q), $"quest fragment for '{q.EditorId}'");
        }
        foreach (var d in spec.Dialogue)
        {
            // setPrimaryIdentity → the override code baked into the fragment (0 = clear/auto).
            var overrideCode = string.IsNullOrWhiteSpace(d.SetPrimaryIdentity) ? -1 : Generator.IdentityCode(spec, d.SetPrimaryIdentity);
            var src = Generator.GenerateDialogueFragmentSource(d, overrideCode);
            if (!string.IsNullOrEmpty(src))
                CompileGenerated(src, Generator.DialogueFragmentScriptName(d), $"dialogue fragment for '{d.EditorId}'");
        }
        foreach (var s in spec.Scenes)
        {
            var src = Generator.GenerateSceneFragmentSource(s);
            if (!string.IsNullOrEmpty(src))
                CompileGenerated(src, Generator.SceneFragmentScriptName(s), $"scene fragment for '{s.EditorId}'");
        }

        // 2) Build the plugin, passing CompiledScriptsDir so WireQuestStages and
        //    AttachDialogueResultScripts wire the VMAD for any fragment whose .pex exists.
        var espPath = Path.Combine(outModDir, pluginName);
        var key = ModKey.FromNameAndExtension(pluginName);
        var result = Generator.Build(spec, key, new BuildOptions { CompiledScriptsDir = compiledFragmentsDir });
        PluginIo.Write(result.Mod, espPath);
        foreach (var w in result.Warnings) Console.WriteLine(w);
        Console.WriteLine(BuildSummary(result.Stats, specPath, espPath));
        WriteSeq(espPath, outModDir);

        // 3) Copy compiled .pex files (fragments + user scripts) into Scripts/.
        Directory.CreateDirectory(scriptsDir);
        foreach (var pex in Directory.GetFiles(compiledFragmentsDir, "*.pex"))
            File.Copy(pex, Path.Combine(scriptsDir, Path.GetFileName(pex)), overwrite: true);

        // 4) User-specified script sources: compile + copy to Scripts/ + Scripts/Source/.
        // Scripts that call MFStoryEventDispatch.Fire() (the universal Script-Event entry) need the
        // dispatcher's .psc on the compiler's header path. The Papyrus compiler treats the input
        // file's own directory as a header dir, so we compile each user script from a temp dir that
        // also holds the embedded dispatcher source — no per-machine cache install required.
        string? sharedHeaderDir = null;
        string? DispatcherHeaderDir()
        {
            if (sharedHeaderDir is not null) return sharedHeaderDir;
            using var rs = typeof(Program).Assembly.GetManifestResourceStream("MFStoryEventDispatch.psc");
            if (rs is null) return null;
            var dir = Path.Combine(Path.GetTempPath(), "modforge-psc-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            using (var fs = File.Create(Path.Combine(dir, "MFStoryEventDispatch.psc"))) rs.CopyTo(fs);
            sharedHeaderDir = dir;
            return dir;
        }

        int compiled = autoCompiled;
        var compiledSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool CompileSource(string? source, string label)
        {
            if (string.IsNullOrEmpty(source)) return false;
            var src = Path.IsPathRooted(source) ? source : Path.Combine(specDir, source);
            if (!compiledSources.Add(Path.GetFullPath(src))) return false;
            if (!File.Exists(src)) { Console.Error.WriteLine($"  ! script source not found: {src}"); return false; }
            // Compile beside the dispatcher header so MFStoryEventDispatch.Fire() resolves; fall back
            // to compiling in place if the embedded dispatcher source is unavailable.
            var hdr = DispatcherHeaderDir();
            var compileTarget = src;
            if (hdr is not null)
            {
                compileTarget = Path.Combine(hdr, Path.GetFileName(src));
                File.Copy(src, compileTarget, overwrite: true);
            }
            var cr = Papyrus.CompileBest(compileTarget, scriptsDir);
            if (!cr.Success) { Console.Error.WriteLine(cr.Message); Console.Error.WriteLine($"  ! compile failed: {label}"); return false; }
            Console.WriteLine(cr.Message);
            Directory.CreateDirectory(sourceDir);
            File.Copy(src, Path.Combine(sourceDir, Path.GetFileName(src)), overwrite: true);
            compiled++;
            return true;
        }
        foreach (var sa in spec.Scripts) CompileSource(sa.Source, sa.Source);
        foreach (var d in spec.Dialogue) CompileSource(d.ResultScriptSource, d.ResultScriptSource);
        foreach (var q in spec.Quests)
            foreach (var a in q.Aliases) CompileSource(a.ScriptSource, a.ScriptSource);

        // 5) Word-wall teaching fragments (generated source, compiled best-effort).
        int wordWallScripts = 0;
        foreach (var ww in spec.WordWalls)
        {
            var scriptName = string.IsNullOrWhiteSpace(ww.ScriptName) ? ww.EditorId + "Script" : ww.ScriptName;
            Directory.CreateDirectory(sourceDir);
            var pscPath = Path.Combine(sourceDir, scriptName + ".psc");
            File.WriteAllText(pscPath, Generator.GenerateWordWallScript(ww));
            wordWallScripts++;
            var cr = Papyrus.CompileBest(pscPath, scriptsDir);
            if (cr.Success) { Console.WriteLine(cr.Message); compiled++; }
            else Console.WriteLine($"  (word-wall script {scriptName}.psc written to Scripts/Source — compile pending: {cr.Message.Split('\n')[0]})");
        }

        // 5b) Ship the generic Script Event dispatcher .pex whenever a quest uses event "ScriptEvent".
        //     It's the universal entry content calls (MFStoryEventDispatch.Fire(kw, ref…)) to fire a
        //     story event; one prebuilt .pex (embedded in this CLI) serves every generated mod.
        if (spec.Quests.Any(q => q.StoryEvent is { } se
                && se.Event.Equals("ScriptEvent", StringComparison.OrdinalIgnoreCase)))
        {
            Directory.CreateDirectory(scriptsDir);
            using var rs = typeof(Program).Assembly.GetManifestResourceStream("MFStoryEventDispatch.pex");
            if (rs is null)
                Console.Error.WriteLine("  ! Script Event dispatcher .pex missing from build — ScriptEvent quests won't fire");
            else
            {
                using var fs = File.Create(Path.Combine(scriptsDir, "MFStoryEventDispatch.pex"));
                rs.CopyTo(fs);
                Console.WriteLine("  + bundled MFStoryEventDispatch.pex (Script Event dispatcher)");
            }
        }

        // 5c) Ship the reusable presence-gated Scene controller .pex whenever a scene uses `autoStart`.
        //     ModForge attaches it (extends Quest) to the host quest and wires its properties; one
        //     prebuilt .pex (embedded in this CLI) serves every generated mod.
        if (spec.Scenes.Any(sc => sc.AutoStart is not null))
        {
            Directory.CreateDirectory(scriptsDir);
            using var rs = typeof(Program).Assembly.GetManifestResourceStream("MFSceneBanterController.pex");
            if (rs is null)
                Console.Error.WriteLine("  ! Scene controller .pex missing from build — autoStart scenes won't fire");
            else
            {
                using var fs = File.Create(Path.Combine(scriptsDir, "MFSceneBanterController.pex"));
                rs.CopyTo(fs);
                Console.WriteLine("  + bundled MFSceneBanterController.pex (presence-gated Scene controller)");
            }
        }

        // 5d) Ship the reusable identity-acquire book .pex whenever an identity declares an acquireBook.
        //     ModForge attaches it (extends ObjectReference — OnRead is an ObjectReference event, NOT
        //     a Book one) to the BOOK base form + binds its properties; one prebuilt .pex
        //     (embedded in this CLI) serves every generated mod.
        if (spec.Identities.Any(idn => !string.IsNullOrWhiteSpace(idn.AcquireBook)))
        {
            Directory.CreateDirectory(scriptsDir);
            using var rs = typeof(Program).Assembly.GetManifestResourceStream("MFIdentityBook.pex");
            if (rs is null)
                Console.Error.WriteLine("  ! identity book .pex missing from build — acquire books won't grant identities");
            else
            {
                using var fs = File.Create(Path.Combine(scriptsDir, "MFIdentityBook.pex"));
                rs.CopyTo(fs);
                Console.WriteLine("  + bundled MFIdentityBook.pex (identity-acquire book)");
            }
        }

        // 5e) Ship the reusable default-identity granter .pex whenever an identity is `default:true`.
        //     ModForge builds a StartGameEnabled quest carrying it (extends Quest); its OnInit adds the
        //     player to every default identity's faction on game start; one prebuilt .pex serves all mods.
        if (spec.Identities.Any(idn => idn.Default && !string.IsNullOrWhiteSpace(idn.Faction)))
        {
            Directory.CreateDirectory(scriptsDir);
            using var rs = typeof(Program).Assembly.GetManifestResourceStream("MFIdentityDefault.pex");
            if (rs is null)
                Console.Error.WriteLine("  ! default-identity .pex missing from build — default identities won't be granted");
            else
            {
                using var fs = File.Create(Path.Combine(scriptsDir, "MFIdentityDefault.pex"));
                rs.CopyTo(fs);
                Console.WriteLine("  + bundled MFIdentityDefault.pex (default-identity granter)");
            }
        }

        // 5f) Ship the reusable primary-identity controller .pex whenever dialogue uses primaryIdentity or
        //     sets the override. ModForge builds a StartGameEnabled quest carrying it (extends Quest); its
        //     OnInit/poll maintains MF_PrimaryIdentity; one prebuilt .pex serves every generated mod.
        if (spec.Identities.Count > 0 && spec.Dialogue.Any(d =>
                !string.IsNullOrWhiteSpace(d.PrimaryIdentity) || !string.IsNullOrWhiteSpace(d.SetPrimaryIdentity)))
        {
            Directory.CreateDirectory(scriptsDir);
            using var rs = typeof(Program).Assembly.GetManifestResourceStream("MFIdentityController.pex");
            if (rs is null)
                Console.Error.WriteLine("  ! identity controller .pex missing from build — primary-identity greetings won't resolve");
            else
            {
                using var fs = File.Create(Path.Combine(scriptsDir, "MFIdentityController.pex"));
                rs.CopyTo(fs);
                Console.WriteLine("  + bundled MFIdentityController.pex (primary-identity controller)");
            }
        }

        // 5g) Ship the reusable identity auto-grant trigger .pex whenever an identity uses `autoGrantWhen`.
        //     ModForge builds a StartGameEnabled quest carrying it; it joins a faction when a player AV
        //     crosses a threshold (e.g. Dragonborn on DragonSouls>=1). One prebuilt .pex serves every mod.
        if (spec.Identities.Any(idn => idn.AutoGrantWhen is { } a && !string.IsNullOrWhiteSpace(a.ActorValue)
                && !string.IsNullOrWhiteSpace(idn.Faction)))
        {
            Directory.CreateDirectory(scriptsDir);
            using var rs = typeof(Program).Assembly.GetManifestResourceStream("MFIdentityAutoGrant.pex");
            if (rs is null)
                Console.Error.WriteLine("  ! identity auto-grant .pex missing from build — autoGrantWhen identities won't be granted");
            else
            {
                using var fs = File.Create(Path.Combine(scriptsDir, "MFIdentityAutoGrant.pex"));
                rs.CopyTo(fs);
                Console.WriteLine("  + bundled MFIdentityAutoGrant.pex (identity auto-grant trigger)");
            }
        }

        // 6) External-resource bundling — copy spec's (or --assets) Meshes/Textures/Sounds/….
        var assetsSrc = !string.IsNullOrWhiteSpace(assetsOverride) ? assetsOverride
                      : !string.IsNullOrWhiteSpace(spec.Assets)
                            ? (Path.IsPathRooted(spec.Assets) ? spec.Assets : Path.Combine(specDir, spec.Assets))
                            : null;
        if (!string.IsNullOrWhiteSpace(assetsSrc))
        {
            var br = Assets.Bundle(assetsSrc, outModDir);
            foreach (var w in br.Warnings) Console.Error.WriteLine(w);
            if (br.FilesCopied > 0)
                Console.WriteLine($"bundled {br.FilesCopied} asset file(s) ({br.BytesCopied / 1024.0:0.#} KiB) " +
                    $"from {assetsSrc} -> [{string.Join(", ", br.CopiedFolders)}]");
        }

        // 7) Action-system loose-file generation (OAR / BDI / PIE) — non-esp config + asset placing.
        //    The .hkx animations are user-supplied; ModForge writes the config tree and copies the
        //    clips it can find (missing clips are reported, not silently dropped).
        if (spec.AnimationReplacers.Count > 0 || spec.BehaviorData.Count > 0 || spec.PayloadMacros.Count > 0)
        {
            string? ResolveHkx(string p)
            {
                if (Path.IsPathRooted(p) && File.Exists(p)) return p;
                if (!string.IsNullOrWhiteSpace(assetsSrc))
                {
                    var a = Path.Combine(assetsSrc, p);
                    if (File.Exists(a)) return a;
                }
                var s = Path.Combine(specDir, p);
                return File.Exists(s) ? s : null;
            }
            void WriteLoose(OarGen.OarFile f)
            {
                var dest = Path.Combine(outModDir, f.RelPath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.WriteAllText(dest, f.Content);
            }

            int oarSubmods = 0, hkxPlaced = 0; var hkxMissing = new List<string>();
            foreach (var r in spec.AnimationReplacers)
            {
                foreach (var f in OarGen.Generate(r)) WriteLoose(f);
                oarSubmods += r.Submods.Count(s => !s.ReplaceVanillaPath);
                foreach (var copy in OarGen.HkxPlacements(r))
                {
                    var src = ResolveHkx(copy.Source);
                    if (src is null) { hkxMissing.Add(copy.Source); continue; }
                    var dest = Path.Combine(outModDir, copy.DestRelPath.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(src, dest, overwrite: true);
                    hkxPlaced++;
                }
            }
            foreach (var b in spec.BehaviorData) WriteLoose(BdiGen.Generate(b));
            foreach (var p in spec.PayloadMacros) WriteLoose(PieGen.Generate(p));

            Console.WriteLine($"action-system: {oarSubmods} OAR submod(s), {spec.BehaviorData.Count} BDI config(s), "
                + $"{spec.PayloadMacros.Count} PIE table(s), {hkxPlaced} hkx placed");
            if (hkxMissing.Count > 0)
                Console.WriteLine($"  ⚠ {hkxMissing.Count} hkx not found (config written, clip missing): {string.Join(", ", hkxMissing)}");
        }

        // Cleanup temp dir.
        try { Directory.Delete(compiledFragmentsDir, recursive: true); } catch { /* best effort */ }

        Console.WriteLine($"packaged -> {outModDir}  ({pluginName} + {compiled} compiled script(s)"
            + (wordWallScripts > 0 ? $" + {wordWallScripts} word-wall fragment(s)" : "") + " under Scripts/)");
        return 0;
    }
}
