namespace ModForge;

public static partial class Generator
{
    // --- Captured-item macro-expansion (Idea #24 addendum — the in-game definition eyedropper) ----
    // Each capturedItems[] entry is sugar: it EXPANDS into the low-level authored records the
    // validated, battle-tested build passes already handle (WEAP/ARMO template-clone + a minted ENCH,
    // or ALCH/INGR effect lists), so BuildItems/BuildPotions/BuildIngredients/BuildEnchantments +
    // WireEffects/WireEnchantments do the real work. Called once at pass 0 (before pass 1). Idempotent.
    //
    // weapon/armor:  a WeaponSpec/ArmorSpec with Template = the captured base (a vanilla weapon to
    //                clone). The enchant is either REFERENCED (a durable ENCH the DLL resolved — a
    //                looted pre-enchanted item) or MINTED as a fresh in-spec ENCH from the captured
    //                MGEF effects (a player-applied enchant, whose runtime ENCH has no durable id).
    // potion/ingredient:  an ALCH/INGR whose Effects are filled directly from the captured effect list.
    //
    // Unknown kinds emit nothing — ValidateCapturedItems names them so it's not a silent drop
    // (CLAUDE.md no-silent-caps). A weapon/armor with no base still expands (a template-less WEAP is a
    // build-time concern surfaced elsewhere); validation warns when base AND enchant are both absent.
    public static void ExpandCapturedItems(ModSpec spec)
    {
        if (spec.CapturedItemsExpanded) return;
        spec.CapturedItemsExpanded = true;
        if (spec.CapturedItems.Count == 0) return;

        int i = 0;
        foreach (var ci in spec.CapturedItems)
        {
            i++;
            string ed = CapturedItemEd(ci, i);
            string kind = (ci.Kind ?? "").Trim().ToLowerInvariant();
            switch (kind)
            {
                case "weapon":
                {
                    var w = new WeaponSpec { EditorId = ed, Name = ci.Name };
                    if (!string.IsNullOrWhiteSpace(ci.Base)) w.Template = ci.Base;
                    string? ench = ResolveOrMintEnchant(spec, ci.Enchantment, ed, apparel: false);
                    if (ench is not null)
                    {
                        w.Enchantment = ench;
                        if (ci.Enchantment!.Amount > 0) w.EnchantmentAmount = ci.Enchantment.Amount;
                    }
                    spec.Weapons.Add(w);
                    break;
                }
                case "armor":
                {
                    var a = new ArmorSpec { EditorId = ed, Name = ci.Name };
                    if (!string.IsNullOrWhiteSpace(ci.Base)) a.Template = ci.Base;
                    string? ench = ResolveOrMintEnchant(spec, ci.Enchantment, ed, apparel: true);
                    if (ench is not null) a.Enchantment = ench;  // apparel enchant has no charge pool
                    spec.Armors.Add(a);
                    break;
                }
                case "potion":
                {
                    var p = new PotionSpec { EditorId = ed, Name = ci.Name, Effects = CloneEffects(ci.Effects) };
                    if (!string.IsNullOrWhiteSpace(ci.Base)) p.Template = ci.Base;
                    spec.Potions.Add(p);
                    break;
                }
                case "ingredient":
                    spec.Ingredients.Add(new IngredientSpec { EditorId = ed, Name = ci.Name, Effects = CloneEffects(ci.Effects) });
                    break;
                default:
                    // Unknown kind: emit nothing; Validate surfaces it (no silent drop).
                    break;
            }
        }
    }

    // Reference a durable ENCH (looted pre-enchanted item) or mint a fresh one from the captured
    // effects (player-applied enchant). Returns the enchantment ref to set on the weapon/armor, or
    // null when there's nothing to enchant (a bare template clone).
    private static string? ResolveOrMintEnchant(ModSpec spec, CapturedEnchantSpec? e, string itemEd, bool apparel)
    {
        if (e is null) return null;
        if (!string.IsNullOrWhiteSpace(e.Base)) return e.Base;   // durable ENCH — reference directly
        if (e.Effects.Count == 0) return null;                   // no base, no effects — nothing to enchant
        string enchEd = itemEd + "_Ench";
        spec.Enchantments.Add(new EnchantmentSpec
        {
            EditorId = enchEd,
            EnchantType = apparel ? "apparel" : "weapon",
            Effects = CloneEffects(e.Effects),
        });
        return enchEd;
    }

    // Deterministic, unique editorId per captured item: an explicit one wins; else MFCap_<name>_<i>
    // (the 1-based position guarantees uniqueness even when two captures share a display name — the
    // DLL can export duplicate "Staff of Magelight" rows).
    private static string CapturedItemEd(CapturedItemSpec ci, int index)
    {
        if (!string.IsNullOrWhiteSpace(ci.EditorId)) return ci.EditorId.Trim();
        string slug = SanitizeEd(ci.Name ?? "");
        if (string.IsNullOrWhiteSpace(slug) || slug.Trim('_').Length == 0)
            slug = SanitizeEd((ci.Kind ?? "item").Trim());
        return $"MFCap_{slug}_{index}";
    }

    private static List<EffectSpec> CloneEffects(List<EffectSpec> src)
    {
        var outp = new List<EffectSpec>(src.Count);
        foreach (var e in src)
            outp.Add(new EffectSpec { MagicEffect = e.MagicEffect, Magnitude = e.Magnitude, Area = e.Area, Duration = e.Duration });
        return outp;
    }
}
