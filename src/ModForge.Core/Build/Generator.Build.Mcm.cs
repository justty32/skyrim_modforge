namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // The reusable empty subclass serves ini-only menus. A menu with `global` bindings gets a
        // generated subclass with setter functions and VMAD GlobalVariable properties instead.
        internal const string McmConfigScript = "ModForgeMCM";
        // SkyUI SDK alias script (shipped by SkyUI, not by us). Its OnPlayerLoadGame drives re-registration.
        internal const string McmPlayerAliasScript = "SKI_PlayerLoadGameAlias";
        private const string McmPlayerRef = "Skyrim.esm:0x000014";

        // --- pass 2: MCM Helper registration quest (D-2) ---
        // MCM Helper does NOT register a menu from a loose config.json alone (confirmed in-game 2026-06-20;
        // the menu never appeared). Its wiki requires, at minimum: a Start-Game-Enabled QUST whose attached
        // script extends MCM_ConfigBase (with inherited string `ModName` retained as MCM Helper's
        // registration/error-page display fallback; config lookup itself uses the owning plugin stem), plus a
        // PlayerAlias — a ReferenceAlias forced to the player carrying SKI_PlayerLoadGameAlias, whose
        // OnPlayerLoadGame calls (GetOwningQuest() as SKI_QuestBase).OnGameReload() to re-register on every
        // load. The config.json/settings.ini loose files are still emitted by McmGen at package time; this
        // builds the ESP side that makes them appear. Mirrors BuildDefaultIdentityQuest (quest + Local
        // script) + AttachAliasScript (QuestFragmentAlias). Per-menu difference is the fallback label only.
        // Runs in pass 2 (the player ref is external so resolves any time, but kept with the quest builders).
        public void BuildMcmQuests()
        {
            foreach (var m in spec.McmConfigs)
            {
                if (string.IsNullOrWhiteSpace(m.ModName)) continue;

                var quest = mod.Quests.AddNew();
                quest.EditorID = $"MF_MCM_{Sanitize(m.ModName)}";
                quest.Name = string.IsNullOrWhiteSpace(m.DisplayName) ? m.ModName : m.DisplayName;
                quest.Flags |= Quest.Flag.StartGameEnabled;

                var qad = new QuestAdapter { Version = 5, ObjectFormat = 2 };

                // Config host script. MCM Helper locates MCM/Config/<plugin-stem>/ from the owning quest;
                // this inherited property is only its registration/error-page display fallback.
                var cfg = new ScriptEntry
                {
                    Name = Generator.HasMcmGlobalBindings(m) ? Generator.McmGlobalScriptName(m) : McmConfigScript,
                    Flags = ScriptEntry.Flag.Local,
                };
                cfg.Properties.Add(new ScriptStringProperty
                    { Name = "ModName", Data = m.ModName, Flags = ScriptProperty.Flag.Edited });
                int globalIndex = 0;
                foreach (var control in Generator.McmGlobalControls(m))
                {
                    var p = new ScriptObjectProperty
                    {
                        Name = Generator.McmGlobalPropertyName(globalIndex++),
                        Flags = ScriptProperty.Flag.Edited,
                    };
                    if (TryResolveRef(control.Global, formKeyByEd, out var gfk)) p.Object.SetTo(gfk);
                    else Warn($"  ! MCM '{m.ModName}': global '{control.Global}' unresolved");
                    cfg.Properties.Add(p);
                }
                qad.Scripts.Add(cfg);

                // PlayerAlias (index 0): forced to the player, carries SKI_PlayerLoadGameAlias.
                const uint aliasId = 0;
                var alias = new QuestAlias { ID = aliasId, Name = "PlayerAlias" };
                if (TryResolveRef(McmPlayerRef, formKeyByEd, out var pfk)) alias.ForcedReference.SetTo(pfk);
                else Warn($"  ! MCM '{m.ModName}': player ref unresolved (PlayerAlias forced ref unset)");
                quest.Aliases.Add(alias);

                var qfa = new QuestFragmentAlias { Version = 5, ObjectFormat = 2 };
                qfa.Property.Object.SetTo(quest.FormKey);
                qfa.Property.Alias = (short)aliasId;
                qfa.Property.Flags = ScriptProperty.Flag.Edited;
                qfa.Scripts.Add(new ScriptEntry { Name = McmPlayerAliasScript, Flags = ScriptEntry.Flag.Local });
                qad.Aliases.Add(qfa);

                quest.VirtualMachineAdapter = qad;
                scriptsAttached++;
            }
        }

        // EditorID-safe form of the stable menu label.
        private static string Sanitize(string s)
        {
            var chars = s.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray();
            return new string(chars);
        }
    }
}
