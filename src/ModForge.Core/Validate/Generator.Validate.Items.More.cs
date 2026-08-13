namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // Continues ValidateItems (Generator.Validate.Items.cs) over the item families that did not
        // fit its 300-line budget. Validates: recipes, spells' effects, enchantments, ingredients,
        // ammunitions, scrolls, soulGems, keys, activators, outfits, textureSets, and the
        // alternate-texture check for statics/activators.
        public void ValidateItemsMore()
        {
            // In-spec weapon/armor editorIds — a temper recipe's target must be one of these (or an
            // external <master>:0xID weapon/armor, which we can't type-check headlessly).
            var weaponArmorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var w in spec.Weapons) weaponArmorIds.Add(w.EditorId);
            foreach (var a in spec.Armors)  weaponArmorIds.Add(a.EditorId);

            foreach (var co in spec.Recipes)
            {
                var kind = NormalizeKind(co.Kind);
                if (!KnownRecipeKinds.Contains(kind))
                    Problems.Add($"recipe '{co.EditorId}' invalid kind '{co.Kind}' (use {string.Join("/", KnownRecipeKinds)})");
                if (string.IsNullOrWhiteSpace(co.CreatedObject)) Problems.Add($"recipe '{co.EditorId}' has empty createdObject");
                else CheckRef(co.CreatedObject, $"recipe '{co.EditorId}' createdObject");
                if (!string.IsNullOrWhiteSpace(co.Workbench) && !KnownWorkbenchNames.Contains(co.Workbench.Trim()))
                    CheckRef(co.Workbench, $"recipe '{co.EditorId}' workbench");
                if (co.Components.Count == 0) Problems.Add($"recipe '{co.EditorId}' has no components (nothing to consume)");
                foreach (var comp in co.Components) CheckRef(comp.Item, $"recipe '{co.EditorId}' component");
                if (kind == "temper" && !string.IsNullOrWhiteSpace(co.CreatedObject)
                    && !LooksExternalRef(co.CreatedObject) && !weaponArmorIds.Contains(co.CreatedObject))
                    Problems.Add($"recipe '{co.EditorId}' kind=temper createdObject '{co.CreatedObject}' is not an in-spec weapon/armor (temper improves a weapon/armor)");
                foreach (var cnd in co.Conditions)
                {
                    if (!IsKnownRecipeFunction(cnd.Function))
                    { Problems.Add($"recipe '{co.EditorId}' condition: unknown function '{cnd.Function}' (HasPerk/GetItemCount/GetGlobalValue/TemperIsEnchanted)"); continue; }
                    if (!IsValidCompareOp(cnd.Comparison))
                        Problems.Add($"recipe '{co.EditorId}' condition: invalid comparison '{cnd.Comparison}' (== != > >= < <=)");
                    if (RecipeFunctionNeedsRef(cnd.Function))
                    {
                        if (string.IsNullOrWhiteSpace(cnd.Param))
                            Problems.Add($"recipe '{co.EditorId}' condition '{cnd.Function}' needs a param (perk/item/global ref)");
                        else CheckRef(cnd.Param, $"recipe '{co.EditorId}' condition '{cnd.Function}' param");
                    }
                }
            }

            foreach (var s in spec.Spells)
            {
                if (!string.IsNullOrEmpty(s.SpellType) && !Enum.TryParse<SpellType>(s.SpellType, true, out _)) Problems.Add($"spell '{s.EditorId}' invalid spellType '{s.SpellType}'");
                if (!string.IsNullOrEmpty(s.CastType) && !Enum.TryParse<CastType>(s.CastType, true, out _)) Problems.Add($"spell '{s.EditorId}' invalid castType '{s.CastType}'");
                if (!string.IsNullOrEmpty(s.TargetType) && !Enum.TryParse<TargetType>(s.TargetType, true, out _)) Problems.Add($"spell '{s.EditorId}' invalid targetType '{s.TargetType}'");
            }
            foreach (var e in spec.Enchantments)
            {
                if (string.IsNullOrWhiteSpace(e.EnchantType) || !EnchantTypes.Contains(e.EnchantType))
                    Problems.Add($"enchantment '{e.EditorId}' invalid enchantType '{e.EnchantType}' (weapon|apparel|staff)");
                if (e.Effects.Count == 0) Problems.Add($"enchantment '{e.EditorId}' has no effects (an ENCH needs ≥1 MGEF-based effect)");
                CheckEffects(e.EditorId, e.Effects, "enchantment");
                if (!string.IsNullOrEmpty(e.CastType) && !Enum.TryParse<CastType>(e.CastType, true, out _))
                    Problems.Add($"enchantment '{e.EditorId}' invalid castType '{e.CastType}' (FireAndForget|Concentration|ConstantEffect)");
                if (!string.IsNullOrEmpty(e.TargetType) && !Enum.TryParse<TargetType>(e.TargetType, true, out _))
                    Problems.Add($"enchantment '{e.EditorId}' invalid targetType '{e.TargetType}' (Self|Touch|Aimed|TargetActor|TargetLocation)");
            }

            // Long-tail items: ingredient, ammo, scroll, soulGem, key, activator, outfit.
            foreach (var i in spec.Ingredients)
            {
                foreach (var k in i.Keywords) CheckRef(k, $"ingredient '{i.EditorId}' keyword");
                CheckEffects(i.EditorId, i.Effects, "ingredient");
            }
            foreach (var a in spec.Ammunitions)
                foreach (var k in a.Keywords) CheckRef(k, $"ammunition '{a.EditorId}' keyword");
            foreach (var s in spec.Scrolls)
            {
                foreach (var k in s.Keywords) CheckRef(k, $"scroll '{s.EditorId}' keyword");
                CheckEffects(s.EditorId, s.Effects, "scroll");
                if (!string.IsNullOrEmpty(s.SpellType) && !Enum.TryParse<SpellType>(s.SpellType, true, out _)) Problems.Add($"scroll '{s.EditorId}' invalid spellType '{s.SpellType}'");
                if (!string.IsNullOrEmpty(s.CastType) && !Enum.TryParse<CastType>(s.CastType, true, out _)) Problems.Add($"scroll '{s.EditorId}' invalid castType '{s.CastType}'");
                if (!string.IsNullOrEmpty(s.TargetType) && !Enum.TryParse<TargetType>(s.TargetType, true, out _)) Problems.Add($"scroll '{s.EditorId}' invalid targetType '{s.TargetType}'");
            }
            foreach (var sg in spec.SoulGems)
            {
                foreach (var k in sg.Keywords) CheckRef(k, $"soulGem '{sg.EditorId}' keyword");
                if (!string.IsNullOrEmpty(sg.MaximumCapacity) && !Enum.TryParse<SoulGem.Level>(sg.MaximumCapacity, true, out _))
                    Problems.Add($"soulGem '{sg.EditorId}' invalid maximumCapacity '{sg.MaximumCapacity}' (None|Petty|Lesser|Common|Greater|Grand)");
            }
            foreach (var k in spec.Keys)
                foreach (var kw in k.Keywords) CheckRef(kw, $"key '{k.EditorId}' keyword");
            foreach (var ac in spec.Activators)
                foreach (var kw in ac.Keywords) CheckRef(kw, $"activator '{ac.EditorId}' keyword");
            foreach (var o in spec.Outfits)
                foreach (var it in o.Items) CheckRef(it, $"outfit '{o.EditorId}' item");

            // TextureSet (TXST): paths relative to Data\Textures\, at least one slot set.
            foreach (var tx in spec.TextureSets)
            {
                if (string.IsNullOrWhiteSpace(tx.Diffuse) && string.IsNullOrWhiteSpace(tx.Normal)
                    && string.IsNullOrWhiteSpace(tx.Mask) && string.IsNullOrWhiteSpace(tx.Glow)
                    && string.IsNullOrWhiteSpace(tx.Height) && string.IsNullOrWhiteSpace(tx.Environment)
                    && string.IsNullOrWhiteSpace(tx.Multilayer) && string.IsNullOrWhiteSpace(tx.Backlight))
                    Problems.Add($"textureSet '{tx.EditorId}' sets no texture slots (at minimum set `diffuse`) — overrides nothing");
                else if (string.IsNullOrWhiteSpace(tx.Diffuse))
                    Problems.Add($"textureSet '{tx.EditorId}' has no `diffuse` slot (the base color map) — unusual; most retextures set it");
                CheckTexPath(tx.Diffuse,     $"textureSet '{tx.EditorId}' diffuse");
                CheckTexPath(tx.Normal,      $"textureSet '{tx.EditorId}' normal");
                CheckTexPath(tx.Mask,        $"textureSet '{tx.EditorId}' mask");
                CheckTexPath(tx.Glow,        $"textureSet '{tx.EditorId}' glow");
                CheckTexPath(tx.Height,      $"textureSet '{tx.EditorId}' height");
                CheckTexPath(tx.Environment, $"textureSet '{tx.EditorId}' environment");
                CheckTexPath(tx.Multilayer,  $"textureSet '{tx.EditorId}' multilayer");
                CheckTexPath(tx.Backlight,   $"textureSet '{tx.EditorId}' backlight");
                foreach (var f in tx.Flags)
                    if (!Enum.TryParse<TextureSet.Flag>(f, true, out _))
                        Problems.Add($"textureSet '{tx.EditorId}' invalid flag '{f}' (NoSpecularMap|FaceGenTextures|HasModelSpaceNormalMap)");
            }
            foreach (var st in spec.Statics) CheckAltTextures(st.EditorId, st.Model, st.AlternateTextures, "static");
            foreach (var ac in spec.Activators) CheckAltTextures(ac.EditorId, ac.Model, ac.AlternateTextures, "activator");
        }
    }
}
