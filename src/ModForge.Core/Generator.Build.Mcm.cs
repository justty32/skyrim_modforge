namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // The reusable empty subclass ModForge ships (Scriptname ModForgeMCM extends MCM_ConfigBase). One
        // .pex serves every generated MCM menu; the per-mod difference is only the ModName property.
        internal const string McmConfigScript = "ModForgeMCM";
        // SkyUI SDK alias script (shipped by SkyUI, not by us). Its OnPlayerLoadGame drives re-registration.
        internal const string McmPlayerAliasScript = "SKI_PlayerLoadGameAlias";
        private const string McmPlayerRef = "Skyrim.esm:0x000014";

        // --- pass 2: MCM Helper registration quest (D-2) ---
        // MCM Helper does NOT register a menu from a loose config.json alone (confirmed in-game 2026-06-20;
        // the menu never appeared). Its wiki requires, at minimum: a Start-Game-Enabled QUST whose attached
        // script extends MCM_ConfigBase (with the inherited string `ModName` property = the
        // Data/MCM/Config/<modName>/ folder, which is what RegisterMod(self, ModName) keys on), plus a
        // PlayerAlias — a ReferenceAlias forced to the player carrying SKI_PlayerLoadGameAlias, whose
        // OnPlayerLoadGame calls (GetOwningQuest() as SKI_QuestBase).OnGameReload() to re-register on every
        // load. The config.json/settings.ini loose files are still emitted by McmGen at package time; this
        // builds the ESP side that makes them appear. Mirrors BuildDefaultIdentityQuest (quest + Local
        // script) + AttachAliasScript (QuestFragmentAlias). Per-mod difference is the ModName string only.
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

                // Config host script — ModName links the quest to MCM/Config/<modName>/config.json.
                var cfg = new ScriptEntry { Name = McmConfigScript, Flags = ScriptEntry.Flag.Local };
                cfg.Properties.Add(new ScriptStringProperty
                    { Name = "ModName", Data = m.ModName, Flags = ScriptProperty.Flag.Edited });
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

        // EditorID-safe form of a modName (MCM folder names allow chars an EDID can't).
        private static string Sanitize(string s)
        {
            var chars = s.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray();
            return new string(chars);
        }
    }
}
