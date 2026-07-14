namespace ModForge;

// --- Captured items (Idea #24 addendum — the in-game "definition eyedropper", 2026-07-11) ------
// The scene-capture-bridge DLL's `sc cap` mode reads the SEMANTIC content of a live item that has
// no durable base to reference (a player-enchanted weapon, a home-brewed potion) and exports it as
// a `capturedItems[]` entry. ModForge macro-EXPANDS each entry into the ordinary authored records
// it already builds — a WEAP/ARMO template-clone (+ a minted ENCH) or an ALCH/INGR effect list —
// so every battle-tested build/wire pass does the real work. See Generator.ExpandCapturedItems.
//
// This is the item sibling of `capturedNpcs[]` (appearance capture, a larger follow-up that needs
// NpcSpec schema growth + facegen baking). The DLL emits the shape below verbatim (SceneExporter.cpp).
public sealed class CapturedItemSpec
{
    public string Kind { get; set; } = "";        // weapon | armor | potion | ingredient
    public string Name { get; set; } = "";        // display name at capture time (also seeds the editorId)
    public string EditorId { get; set; } = "";    // optional explicit editorId; auto-derived from name+index if empty
    public string Base { get; set; } = "";        // physical template ref "<master>:0xFORMID" to clone; "" if runtime-only
    public CapturedEnchantSpec? Enchantment { get; set; }  // weapon/armor only — the enchant to reference or mint
    public List<EffectSpec> Effects { get; set; } = new(); // potion/ingredient only — the alchemy effect list
    public string Note { get; set; } = "";  // free-form capture-time note. Inert documentation only — Generator.ExpandCapturedItems never reads this
}

// The enchantment carried by a captured weapon/armor. If `base` is a durable ENCH ref (a vanilla or
// otherwise-authored ObjectEffect, e.g. a looted pre-enchanted item), the expansion REFERENCES it
// directly. Otherwise (a player-applied enchant lives on a runtime/dynamic ENCH the DLL can't resolve)
// `effects` carries the MGEF list and the expansion MINTS a fresh ENCH from it.
public sealed class CapturedEnchantSpec
{
    public string Target { get; set; } = "";       // weapon | armor (chooses weapon vs apparel enchant family)
    public string Base { get; set; } = "";          // durable ENCH ref "<master>:0xFORMID"; "" => mint from effects
    public ushort Amount { get; set; }              // charge pool (weapon.enchantmentAmount); 0 = engine auto-calc
    public List<EffectSpec> Effects { get; set; } = new(); // MGEF-based effects, same shape as spell/potion effects
}
