internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  package steps 4-5 (user-authored + word-wall scripts) and 5b-5g (prebuilt .pex
    //  shipped per feature). Split out of Package.cs (2026-08-27).
    // -------------------------------------------------------------------------------
    // `compiled` comes in seeded with the generated-fragment count from step 1 and is returned
    // with the user scripts and word walls added, so the final summary line counts everything.
    private static (int Compiled, int WordWallScripts) CompileUserScriptsAndWordWalls(
        ModSpec spec, string specDir, string scriptsDir, string sourceDir, int compiled)
    {
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
        foreach (var me in spec.MagicEffects) foreach (var sa in me.Scripts) CompileSource(sa.Source, sa.Source);
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

        return (compiled, wordWallScripts);
    }

    // One prebuilt .pex per feature the spec actually uses; each is embedded in this CLI
    // and serves every generated mod.
    private static void ShipFeaturePexFiles(ModSpec spec, string scriptsDir)
    {
        void ShipEmbeddedPex(string name, string label, string onError) =>
            Program.ShipEmbeddedPex(scriptsDir, name, label, onError);

        // 5b-5g) Ship one prebuilt .pex per feature; each is embedded in this CLI and serves every generated mod.
        if (spec.Quests.Any(q => q.StoryEvent is { } se && se.Event.Equals("ScriptEvent", StringComparison.OrdinalIgnoreCase)))
            ShipEmbeddedPex("MFStoryEventDispatch.pex", "Script Event dispatcher", "ScriptEvent quests won't fire");

        if (spec.Scenes.Any(sc => sc.AutoStart is not null))
            ShipEmbeddedPex("MFSceneBanterController.pex", "presence-gated Scene controller", "autoStart scenes won't fire");

        if (spec.Quests.Any(q => q.StoryEvent is { } se && se.CooldownHours > 0f))
            ShipEmbeddedPex("MFEncounterCooldown.pex", "SM-encounter cooldown", "cooldownHours encounters won't be rate-limited");

        if (spec.Quests.Any(q => q.Spawn is not null))
            ShipEmbeddedPex("MFDynamicSpawn.pex", "dynamic near-player spawn", "spawn quests won't spawn anything");

        // In-world skill-tree node behaviour (Idea #20). One .pex serves every node of every tree.
        if (spec.SkillTrees.Count > 0)
            ShipEmbeddedPex("MFSkillNode.pex", "in-world skill-tree node", "skill-tree nodes won't respond to activation");

        // Living-world NPCs (Idea #23). Two .pex: the per-mod world controller (roster tick + presence
        // poll) and the per-NPC alias script (abstract sim + materialize). One pair serves every mod.
        if (spec.LivingNpcs is { } livingNpcs && livingNpcs.Npcs.Count > 0)
        {
            ShipEmbeddedPex("MFLivingWorldController.pex", "living-world roster controller", "living NPCs won't tick/materialise");
            ShipEmbeddedPex("MFLivingNpcAlias.pex", "living-NPC alias behaviour", "living NPCs won't tick/materialise");
        }

        if (spec.Identities.Any(idn => !string.IsNullOrWhiteSpace(idn.AcquireBook)))
            ShipEmbeddedPex("MFIdentityBook.pex", "identity-acquire book", "acquire books won't grant identities");

        if (spec.Identities.Any(idn => idn.Default && !string.IsNullOrWhiteSpace(idn.Faction)))
            ShipEmbeddedPex("MFIdentityDefault.pex", "default-identity granter", "default identities won't be granted");

        if (spec.Identities.Count > 0 && spec.Dialogue.Any(d =>
                !string.IsNullOrWhiteSpace(d.PrimaryIdentity) || !string.IsNullOrWhiteSpace(d.SetPrimaryIdentity)))
            ShipEmbeddedPex("MFIdentityController.pex", "primary-identity controller", "primary-identity greetings won't resolve");

        if (spec.Identities.Any(idn => idn.AutoGrantWhen is { } a && !string.IsNullOrWhiteSpace(a.ActorValue)
                && !string.IsNullOrWhiteSpace(idn.Faction)))
            ShipEmbeddedPex("MFIdentityAutoGrant.pex", "identity auto-grant trigger", "autoGrantWhen identities won't be granted");

        // MCM Helper menus register via a Start-Game-Enabled QUST carrying ModForgeMCM (extends
        // MCM_ConfigBase). Without this .pex the quest's script is missing → no menu appears.
        if (spec.McmConfigs.Count > 0)
            ShipEmbeddedPex("ModForgeMCM.pex", "MCM Helper config host", "MCM menus won't register/appear");
    }
}