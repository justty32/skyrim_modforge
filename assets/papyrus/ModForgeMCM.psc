Scriptname ModForgeMCM extends MCM_ConfigBase
{Reusable MCM Helper config host. Attached to a Start-Game-Enabled QUST whose ModName
 property (set in the ESP VMAD) points at Data/MCM/Config/<ModName>/config.json. The
 config.json drives the entire menu (pure ModSetting* ini storage), so this subclass
 needs no body — registration + value storage live in MCM_ConfigBase / SKI_ConfigBase.
 One compiled .pex serves every ModForge-generated MCM menu; the per-mod difference is
 only the ModName property value. Controls with an action.CallFunction would need a
 per-mod subclass instead (not generated yet).}
