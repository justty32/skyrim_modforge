namespace ModForge;

// --- Enchantment / Object Effect (ENCH) ----------------------------------------------------
// A reusable bundle of MGEF-based `effects` (SAME shape as a spell/potion effect — a MagicEffect
// ref + magnitude/area/duration) that a weapon or armor REFERENCES via its `enchantment` field.
// `enchantType` picks the behaviour family:
//   * weapon  — a "cast on strike" charge enchant (Mutagen EnchantType=Enchantment, default
//               cast=FireAndForget, target=Touch). The WEAPON carries the charge pool
//               (weapon.enchantmentAmount); each hit drains `enchantmentCost` from it.
//   * apparel — an always-on constant effect while worn (EnchantType=Enchantment, default
//               cast=ConstantEffect, target=Self). No charge — passive.
//   * staff   — a staff "cast on use" enchant (EnchantType=StaffEnchantment, default
//               cast=FireAndForget, target=Aimed). The staff carries the charge pool.
// `castType`/`targetType` may override the per-type defaults above (rarely needed). `enchantmentCost`
// is the per-cast magicka/charge cost (weapon/staff) or the worn-cost the engine shows (apparel);
// `chargeTime` is the staff cast charge-up (vanilla staves ~0.5). MGEF refs are wired in pass 2.
public sealed class EnchantmentSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string EnchantType { get; set; } = "weapon";   // weapon|apparel|staff
    public string CastType { get; set; } = "";             // override; empty ⇒ per-enchantType default
    public string TargetType { get; set; } = "";           // override; empty ⇒ per-enchantType default
    public uint EnchantmentCost { get; set; }              // per-cast charge cost (weapon/staff) / worn cost (apparel)
    public float ChargeTime { get; set; }                   // staff cast charge-up (vanilla ~0.5)
    public List<EffectSpec> Effects { get; set; } = new(); // ≥1 MGEF-based effect (same shape as spell/potion effects)
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<EnchantmentSpec> Enchantments { get; set; } = new();
}
