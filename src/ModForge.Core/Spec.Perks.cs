namespace ModForge;

// --- Perk (PERK) -------------------------------------------------------------------------
// A passive ability / stat-or-combat modifier. The trunk carries name/description, the
// Playable/Hidden trunk flags, Level + NumRanks, optional player-facing `conditions` (the perk
// only becomes available/active when they pass — e.g. GetBaseActorValue Destruction >= 30), and a
// list of `effects`. Two effect shapes are supported (see PerkEffectSpec):
//   * "ability"   — grant a SPEL (an Ability-type, constant-effect spell built from in-spec MGEFs).
//   * "entryPoint"— a quantitative modifier on a named EntryPoint (e.g. ModAttackDamage ×1.2).
// Attach a perk to an NPC via npcs[].perks; the player gets perks via script (AddPerk) — there is
// no record-only way to put a perk on the player at game start.
//
// `conditions` (perk-level + effect-level) use the SHARED ConditionSpec (Spec.Dialogue.cs) +
// BuildCondition (Generator.Build.Conditions.cs) — function + param(ref) + comparison + value +
// actorValue/itemType + runOn + or. The perk-relevant functions (GetBaseActorValue, HasKeyword,
// WornHasKeyword, HasPerk, GetIsID/GetIsRace, GetEquippedItemType, GetLevel, …) are supported there.
public sealed class PerkSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Playable { get; set; } = true;   // shows in a skill tree / is selectable (vanilla perks: true)
    public bool Hidden { get; set; }              // hidden from the perk UI (engine/quest-reward perks)
    public bool Trait { get; set; }               // race "trait" perk (rarely needed)
    public int Level { get; set; }                // min skill/char level gate shown in UI (0 = none)
    public int NumRanks { get; set; } = 1;        // how many ranks this perk has (>= 1)
    public string NextPerk { get; set; } = "";    // optional ref → the next perk in a rank chain
    public List<ConditionSpec> Conditions { get; set; } = new();   // perk-level CTDA gates
    public List<PerkEffectSpec> Effects { get; set; } = new();
}
// One effect on a perk. `kind` selects the shape:
//   * "ability"    — grant `spell` (a ref → SPEL, normally an in-spec Ability/constant-effect spell).
//   * "entryPoint" — modify a named EntryPoint quantitatively: `entryPoint` (an EntryType name, e.g.
//                    ModAttackDamage / ModSpellMagnitude / CalculateMyCriticalHitChance — discover the
//                    full set with `perkdiag <Skyrim.esm> <perkFormId>` or the docs), `function`
//                    (Set | Add | Multiply) and `value` (the operand, e.g. 1.2 for +20% with Multiply).
// `rank`/`priority` order effects when a perk has ranks; `conditions` are EFFECT-level CTDA gates
// (wrapped as PerkConditions) — e.g. "only when the equipped weapon is one-handed".
public sealed class PerkEffectSpec
{
    public string Kind { get; set; } = "";        // "ability" | "entryPoint"
    public int Rank { get; set; }                  // applies at perk rank >= this (0-based; default 0)
    public int Priority { get; set; }              // tie-break ordering among effects (default 0)
    public List<ConditionSpec> Conditions { get; set; } = new();   // effect-level CTDA gates
    // --- ability kind ---
    public string Spell { get; set; } = "";        // ref → SPEL granted by this perk
    // --- entryPoint kind ---
    public string EntryPoint { get; set; } = "";   // EntryType name (e.g. ModAttackDamage)
    public string Function { get; set; } = "Multiply"; // Set | Add | Multiply
    public float Value { get; set; }               // the modifier operand
}
