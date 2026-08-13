namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- pass 1: Enchantment / Object Effect (ENCH) scalar records ---
        // The MGEF-based `effects` (FormLinks) are wired in pass 2 (WireEnchantments — ObjectEffect
        // implements IHasEffects, same as Spell). enchantType picks the family + vanilla-default
        // cast/target (overridable per-spec).
        public void BuildEnchantments()
        {
            foreach (var e in spec.Enchantments)
            {
                var r = mod.ObjectEffects.AddNew();
                r.EditorID = e.EditorId;
                if (!string.IsNullOrEmpty(e.Name)) r.Name = e.Name;
                var (etype, defCast, defTarget) = EnchantFamily(e.EnchantType);
                r.EnchantType = etype;
                r.CastType = Enum.TryParse<CastType>(e.CastType, ignoreCase: true, out var ect) ? ect : defCast;
                r.TargetType = Enum.TryParse<TargetType>(e.TargetType, ignoreCase: true, out var ett) ? ett : defTarget;
                // EnchantmentCost = per-cast charge cost; EnchantmentAmount mirrors it (vanilla keeps the
                // two equal on the ENCH — the weapon's own enchantmentAmount is the separate charge pool).
                r.EnchantmentCost = e.EnchantmentCost;
                r.EnchantmentAmount = (int)e.EnchantmentCost;
                if (e.ChargeTime > 0) r.ChargeTime = e.ChargeTime;
            }
        }

        // --- pass 2: ENCH effects (shared WireEffects) + weapon/armor enchantment FormLinks ---
        // ENCH effects use the SAME Effect shape (MGEF ref + magnitude/area/duration). ObjectEffect
        // implements IHasEffects, so the shared WireEffects helper handles it. Then wire each
        // weapon/armor to its enchantment (ObjectEffect FormLink); weapons additionally get a charge
        // pool (EnchantmentAmount), apparel/armor enchants are passive (no charge).
        public void WireEnchantments()
        {
            foreach (var e in spec.Enchantments) WireEffectsFor(e.EditorId, e.Effects);
            foreach (var w in spec.Weapons)
            {
                if (string.IsNullOrWhiteSpace(w.Enchantment)) continue;
                if (recordsByEd.TryGetValue(w.EditorId, out var rec) && rec is IWeapon wr)
                {
                    Resolve($"weapon '{w.EditorId}' enchantment", w.Enchantment, fk => wr.ObjectEffect.SetTo(fk));
                    if (w.EnchantmentAmount > 0) wr.EnchantmentAmount = w.EnchantmentAmount;
                }
            }
            foreach (var a in spec.Armors)
            {
                if (string.IsNullOrWhiteSpace(a.Enchantment)) continue;
                if (recordsByEd.TryGetValue(a.EditorId, out var rec) && rec is IArmor ar)
                    Resolve($"armor '{a.EditorId}' enchantment", a.Enchantment, fk => ar.ObjectEffect.SetTo(fk));
            }
        }
    }
}
