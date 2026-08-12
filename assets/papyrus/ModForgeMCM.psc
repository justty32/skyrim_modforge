Scriptname ModForgeMCM extends MCM_ConfigBase
{Reusable host for ini-only MCM Helper menus. MCM Helper derives
 Data/MCM/Config/<plugin-stem>/ from the owning quest's plugin; the inherited ModName
 property is only a registration/error-page display fallback. The config.json drives
 the menu, so this subclass needs no body. One compiled .pex serves every ini-only
 menu. Menus with action.CallFunction use a generated per-menu subclass instead.}
