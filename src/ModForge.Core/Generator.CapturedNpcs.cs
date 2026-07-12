namespace ModForge;

public static partial class Generator
{
    // --- Captured-NPC macro-expansion (Idea #24 addendum — the in-game actor eyedropper) ---------
    // Each capturedNpcs[] entry is sugar: it EXPANDS into an ordinary NpcSpec (identity + the full
    // TESNPC face/body recipe) plus, when the capture carried an anchor, an ACHR PlacementSpec at
    // the capture spot — so the battle-tested NPC build/wire/place passes do the real work.
    // Called once at pass 0 (after ExpandCapturedItems). Idempotent.
    //
    // Deliberately NOT consumed (advisory fields; see Spec.CapturedNpcs.cs header): `base` (Q2:
    // always MINT a fresh NPC_, never override the origin), `dead`, `activeEffects`, hairColor rgb,
    // perk ranks. AutoCalcStats turns on ONLY when the capture carried a class — autoCalc with no
    // class computes ~0 HP (the permanent-bleedout footgun); class-less captures keep flat defaults.
    // AI fields are left at spec defaults (an appearance clone is docile until authored otherwise).
    public static void ExpandCapturedNpcs(ModSpec spec)
    {
        if (spec.CapturedNpcsExpanded) return;
        spec.CapturedNpcsExpanded = true;
        if (spec.CapturedNpcs.Count == 0) return;

        int i = 0;
        foreach (var cn in spec.CapturedNpcs)
        {
            i++;
            string ed = CapturedNpcEd(cn, i);
            // Carry: each row routes to the OUTFIT (worn armour — the engine only auto-wears
            // outfit armour; inventory armour stays in the pocket, in-game confirmed on the
            // boots-in-pocket clone) or to inventory (weapons auto-equip; food/potions/gold ride
            // with their counts). An INSTANCE-enchanted row (a player-crafted staff/armour whose
            // enchant lives on the item instance, not the base) first MINTS a WEAP/ARMO template
            // clone carrying the referenced-or-minted ENCH — the same machinery capturedItems
            // uses — and the route consumes the minted editorId. The minted outfit replaces
            // defaultOutfit (a PROTEUS clone's outfit ref is an empty runtime shell on disk —
            // in-game confirmed: naked clone). Legacy shapes: `equipped` = worn armour ids,
            // `equippedWeapons` = count-1 inventory ids.
            var outfitItems = cn.EquippedArmor.Concat(cn.Equipped)
                .Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList();
            var invRows = new List<NpcItemSpec>();
            int j = 0;
            foreach (var row in cn.Inventory)
            {
                j++;
                if (string.IsNullOrWhiteSpace(row.Item)) continue;
                string itemRef = row.Item;
                var e = row.Enchantment;
                if (e is not null && (!string.IsNullOrWhiteSpace(e.Base) || e.Effects.Count > 0))
                {
                    string mintedEd = $"{ed}_Inv{j}";
                    bool apparel = row.Worn || string.Equals(e.Target, "armor", StringComparison.OrdinalIgnoreCase);
                    string? ench = ResolveOrMintEnchant(spec, e, mintedEd, apparel);
                    if (apparel)
                        spec.Armors.Add(new ArmorSpec
                        {
                            EditorId = mintedEd, Name = row.Name, Template = row.Item,
                            Enchantment = ench ?? "",
                        });
                    else
                    {
                        var w = new WeaponSpec { EditorId = mintedEd, Name = row.Name, Template = row.Item };
                        if (ench is not null)
                        {
                            w.Enchantment = ench;
                            if (e.Amount > 0) w.EnchantmentAmount = e.Amount;
                        }
                        spec.Weapons.Add(w);
                    }
                    itemRef = mintedEd;
                }
                if (row.Worn) outfitItems.Add(itemRef);
                else invRows.Add(new NpcItemSpec { Item = itemRef, Count = Math.Max(1, row.Count) });
            }
            foreach (var eq in cn.EquippedWeapons)   // legacy v5 shape — count-1 inventory rows
                if (!string.IsNullOrWhiteSpace(eq)) invRows.Add(new NpcItemSpec { Item = eq, Count = 1 });
            string outfitEd = outfitItems.Count > 0 ? ed + "_Outfit" : "";
            if (outfitItems.Count > 0)
                spec.Outfits.Add(new OutfitSpec { EditorId = outfitEd, Items = outfitItems });
            // Stats, in priority order: EXPLICIT captured values > class autocalc. The base actor
            // values are what the engine really runs on, so when the capture carried them (DLL
            // co-save v8+) they are authored to DNAM and autoCalc stays OFF — autocalc would
            // recompute (and overwrite) them from class+level at load, which is only an estimate.
            // A capture with no stats (a pre-v8 json) keeps the old route: autoCalc ONLY with a
            // class (class-less autoCalc = the ~0-HP permanent-bleedout footgun). `class` is
            // carried either way — it still drives AI/training semantics.
            bool explicitStats = cn.Health > 0f || cn.Magicka > 0f || cn.Stamina > 0f || cn.Skills.Count == 18;
            var n = new NpcSpec
            {
                EditorId = ed, Name = cn.Name,
                // identity
                Race = cn.Race, Female = cn.Female,
                Unique = cn.Unique, Essential = cn.Essential, Protected = cn.Protected,
                Outfit = outfitItems.Count > 0 ? outfitEd : cn.DefaultOutfit,
                // stats
                Class = cn.Class, Level = cn.Level,
                AutoCalcStats = !explicitStats && !string.IsNullOrWhiteSpace(cn.Class),
                Health = (int)Math.Round(cn.Health), Magicka = (int)Math.Round(cn.Magicka),
                Stamina = (int)Math.Round(cn.Stamina),
                Skills = new List<int>(cn.Skills),
                // behaviour: what the AI casts, HOW it fights, and its voice
                CombatStyle = cn.CombatStyle, VoiceType = cn.VoiceType,
                Spells = cn.Spells.Where(sp => !string.IsNullOrWhiteSpace(sp)).ToList(),
                // face/body recipe
                Weight = cn.Weight, Height = cn.Height,
                BodyTint = cn.BodyTint,
                HairColor = cn.HairColor?.Id ?? "",
                FaceTexture = cn.FaceTexture,
                HeadParts = new List<string>(cn.HeadParts),
                TintLayers = cn.TintLayers.Select(t => new TintLayerSpec
                {
                    Index = t.Index, Preset = t.Preset, Value = t.Value,
                    Color = t.Color is { } c ? new ColorSpec { R = c.R, G = c.G, B = c.B, A = c.A } : null,
                }).ToList(),
                FaceMorphs = new List<float>(cn.FaceMorphs),
                FaceParts = new List<int>(cn.FaceParts),
            };
            foreach (var p in cn.Perks)
                if (!string.IsNullOrWhiteSpace(p.Perk)) n.Perks.Add(p.Perk);
            n.Items.AddRange(invRows);
            spec.Npcs.Add(n);

            // Place the clone where it was captured. A capture with no anchor (the DLL couldn't
            // resolve a durable cell/worldspace) still mints the NPC_ — usable via placeatme or a
            // later hand-authored placement.
            if (!string.IsNullOrWhiteSpace(cn.Cell) || !string.IsNullOrWhiteSpace(cn.Worldspace))
                spec.Placements.Add(new PlacementSpec
                {
                    Base = ed, EditorId = ed + "_Ref", Kind = "npc",
                    Cell = cn.Cell, Worldspace = cn.Worldspace,
                    Position = new Vec3 { X = cn.Position.X, Y = cn.Position.Y, Z = cn.Position.Z },
                    Rotation = new Vec3 { X = cn.Rotation.X, Y = cn.Rotation.Y, Z = cn.Rotation.Z },
                    Persistent = true,
                });
        }
    }

    // Deterministic, unique editorId per captured NPC: an explicit one wins; else
    // MFCapNpc_<name>_<i> (1-based index keeps duplicate display names collision-free).
    private static string CapturedNpcEd(CapturedNpcSpec cn, int index)
    {
        if (!string.IsNullOrWhiteSpace(cn.EditorId)) return cn.EditorId.Trim();
        string slug = SanitizeEd(cn.Name ?? "");
        if (string.IsNullOrWhiteSpace(slug) || slug.Trim('_').Length == 0) slug = "Npc";
        return $"MFCapNpc_{slug}_{index}";
    }
}
