namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // Validate npcRoles at the HIGH level (before macro-expansion), so a bad role/ref is named as
        // authored rather than surfacing as a mysterious missing generated record. Idea #24 §D.
        // Known roles are the only ones ExpandNpcRoles emits records for; an unknown role would expand
        // to nothing, so we flag it here (no silent drop, per CLAUDE.md).
        private static readonly HashSet<string> KnownRoles =
            new(System.StringComparer.OrdinalIgnoreCase) { "blacksmith" };

        public void ValidateNpcRoles()
        {
            for (int i = 0; i < spec.NpcRoles.Count; i++)
            {
                var nr = spec.NpcRoles[i];
                string who = $"npcRole[{i}]" + (string.IsNullOrWhiteSpace(nr.Npc) ? "" : $" (npc '{nr.Npc}')");
                if (string.IsNullOrWhiteSpace(nr.Npc))
                    Problems.Add($"{who}: missing npc (the base NPC ref the role attaches to)");
                if (string.IsNullOrWhiteSpace(nr.Role))
                    Problems.Add($"{who}: missing role");
                else if (!KnownRoles.Contains(nr.Role.Trim()))
                    Problems.Add($"{who}: unknown role '{nr.Role}' (supported: {string.Join(", ", KnownRoles)}) — would expand to nothing");
            }
        }

        // Idea #24 §E eraser — each removal must be an external "<master>:0xFORMID" ref (an existing
        // placed ref to disable); an in-spec editorId can't be removed (it's ours to just not emit).
        public void ValidateRemovals()
        {
            foreach (var r in spec.Removals)
            {
                if (string.IsNullOrWhiteSpace(r))
                    Problems.Add("removal: empty ref");
                else if (!LooksExternalRef(r) || !TryExternalRef(r, out _))
                    Problems.Add($"removal '{r}': must be a well-formed external \"<master>:0xFORMID\" ref of an existing placed ref");
            }
        }

        // Idea #24 definition eyedropper — capturedItems[] macro-expands to WEAP/ARMO(+ENCH)/ALCH/INGR.
        // Validate the HIGH-level entry (before expansion) so a bad kind/ref is named as authored.
        private static readonly HashSet<string> KnownCapturedKinds =
            new(System.StringComparer.OrdinalIgnoreCase) { "weapon", "armor", "potion", "ingredient" };

        public void ValidateCapturedItems()
        {
            for (int i = 0; i < spec.CapturedItems.Count; i++)
            {
                var ci = spec.CapturedItems[i];
                string who = $"capturedItem[{i}]" + (string.IsNullOrWhiteSpace(ci.Name) ? "" : $" ('{ci.Name}')");
                string kind = (ci.Kind ?? "").Trim();
                if (string.IsNullOrWhiteSpace(kind))
                {
                    Problems.Add($"{who}: missing kind (weapon | armor | potion | ingredient)");
                    continue;
                }
                if (!KnownCapturedKinds.Contains(kind))
                {
                    Problems.Add($"{who}: unknown kind '{kind}' (supported: {string.Join(", ", KnownCapturedKinds)}) — would expand to nothing");
                    continue;
                }

                // base is an optional physical template, but if present it must be a well-formed external ref.
                if (!string.IsNullOrWhiteSpace(ci.Base) && (!LooksExternalRef(ci.Base) || !TryExternalRef(ci.Base, out _)))
                    Problems.Add($"{who}: base '{ci.Base}' must be a well-formed external \"<master>:0xFORMID\" ref");

                bool isGear = kind.Equals("weapon", System.StringComparison.OrdinalIgnoreCase)
                           || kind.Equals("armor", System.StringComparison.OrdinalIgnoreCase);
                if (isGear)
                {
                    var e = ci.Enchantment;
                    bool hasEnch = e is not null && (!string.IsNullOrWhiteSpace(e.Base) || e.Effects.Count > 0);
                    if (!hasEnch && string.IsNullOrWhiteSpace(ci.Base))
                        Problems.Add($"{who}: a captured {kind} needs either a base template or an enchantment (both absent → nothing to build)");
                    if (e is not null && !string.IsNullOrWhiteSpace(e.Base)
                        && (!LooksExternalRef(e.Base) || !TryExternalRef(e.Base, out _)))
                        Problems.Add($"{who}: enchantment.base '{e.Base}' must be a well-formed external ENCH ref");
                    if (e is not null && string.IsNullOrWhiteSpace(e.Base))
                        foreach (var ef in e.Effects)
                            if (string.IsNullOrWhiteSpace(ef.MagicEffect))
                                Problems.Add($"{who}: an enchantment effect is missing its magicEffect ref");
                }
                else // potion / ingredient
                {
                    if (ci.Effects.Count == 0)
                        Problems.Add($"{who}: a captured {kind} has no effects");
                    foreach (var ef in ci.Effects)
                        if (string.IsNullOrWhiteSpace(ef.MagicEffect))
                            Problems.Add($"{who}: an effect is missing its magicEffect ref");
                }
            }
        }

        // capturedNpcs[] — the actor eyedropper (Generator.ExpandCapturedNpcs). Validated at the
        // capture level (the expansion runs at build pass 0, after Validate): identity refs must be
        // well-formed, the recipe arrays must be the engine's fixed sizes (18 morphs / 4 parts),
        // and colour/weight components must be in range. `dead`/`activeEffects`/perk ranks are
        // advisory (never consumed) so they get no checks.
        public void ValidateCapturedNpcs()
        {
            for (int i = 0; i < spec.CapturedNpcs.Count; i++)
            {
                var cn = spec.CapturedNpcs[i];
                string who = $"capturedNpc[{i}]" + (string.IsNullOrWhiteSpace(cn.Name) ? "" : $" ('{cn.Name}')");
                // An NPC_ without a race is broken in-game (invisible/CTD territory). Base-game race
                // refs always resolve durably in the DLL, so an empty race means something odd upstream.
                if (string.IsNullOrWhiteSpace(cn.Race))
                    Problems.Add($"{who}: missing race (a raceless NPC_ is broken in-game; base-game races always export durably — check the capture)");
                CheckRef(cn.Race, $"{who} race");
                CheckRef(cn.HairColor?.Id ?? "", $"{who} hairColor.id");
                CheckRef(cn.FaceTexture, $"{who} faceTexture");
                CheckRef(cn.DefaultOutfit, $"{who} defaultOutfit");
                foreach (var hp in cn.HeadParts) CheckRef(hp, $"{who} headPart");
                foreach (var p in cn.Perks)
                    if (string.IsNullOrWhiteSpace(p.Perk)) Problems.Add($"{who}: a perk entry is missing its perk ref");
                    else CheckRef(p.Perk, $"{who} perk");
                CheckRef(cn.Class, $"{who} class");
                CheckRef(cn.CombatStyle, $"{who} combatStyle");
                CheckRef(cn.VoiceType, $"{who} voiceType");
                foreach (var sp in cn.Spells) CheckRef(sp, $"{who} spell");
                if (cn.Level < 0)
                    Problems.Add($"{who}: level {cn.Level} is negative");
                foreach (var eq in cn.EquippedArmor) CheckRef(eq, $"{who} equippedArmor");
                for (int r = 0; r < cn.Inventory.Count; r++)
                {
                    var it = cn.Inventory[r];
                    if (string.IsNullOrWhiteSpace(it.Item)) Problems.Add($"{who}: inventory[{r}] is missing its item ref");
                    else CheckRef(it.Item, $"{who} inventory[{r}]");
                    // instance enchantment: same rules as a captured item's enchant
                    if (it.Enchantment is { } ie)
                    {
                        if (!string.IsNullOrWhiteSpace(ie.Base)
                            && (!LooksExternalRef(ie.Base) || !TryExternalRef(ie.Base, out _)))
                            Problems.Add($"{who}: inventory[{r}] enchantment.base '{ie.Base}' must be a well-formed external ENCH ref");
                        if (string.IsNullOrWhiteSpace(ie.Base))
                            foreach (var ef in ie.Effects)
                                if (string.IsNullOrWhiteSpace(ef.MagicEffect))
                                    Problems.Add($"{who}: inventory[{r}] enchantment effect is missing its magicEffect ref");
                    }
                }
                foreach (var eq in cn.EquippedWeapons) CheckRef(eq, $"{who} equippedWeapons");
                foreach (var eq in cn.Equipped) CheckRef(eq, $"{who} equipped");
                if (!string.IsNullOrWhiteSpace(cn.Cell) && !string.IsNullOrWhiteSpace(cn.Worldspace))
                    Problems.Add($"{who}: has BOTH cell and worldspace (the anchor is one or the other)");
                CheckRef(cn.Cell, $"{who} cell");
                CheckRef(cn.Worldspace, $"{who} worldspace");
                if (cn.FaceMorphs.Count != 0 && cn.FaceMorphs.Count != 18)
                    Problems.Add($"{who}: faceMorphs has {cn.FaceMorphs.Count} values (the engine's morph array is exactly 18, or omit it)");
                if (cn.FaceParts.Count != 0 && cn.FaceParts.Count != 4)
                    Problems.Add($"{who}: faceParts has {cn.FaceParts.Count} values (exactly 4 — nose/unknown/eyes/mouth — or omit it)");
                if (cn.Weight is < 0f or > 100f)
                    Problems.Add($"{who}: weight {cn.Weight} out of range (0–100)");
                CheckColor(cn.BodyTint, $"{who} bodyTint");
                for (int t = 0; t < cn.TintLayers.Count; t++)
                {
                    var tl = cn.TintLayers[t];
                    if (tl.Value is < 0f or > 100f)
                        Problems.Add($"{who}: tintLayers[{t}] value {tl.Value} out of range (0–100 raw engine scale)");
                    CheckColor(tl.Color, $"{who} tintLayers[{t}] color");
                }
            }
        }

        private void CheckColor(ColorSpec? c, string what)
        {
            if (c is null) return;
            if (c.R is < 0 or > 255 || c.G is < 0 or > 255 || c.B is < 0 or > 255 || c.A is < 0 or > 255)
                Problems.Add($"{what}: colour component out of range (0–255)");
        }

        // Idea #24 numpad editor — overrides[] re-stamps the transform of an existing placed ref.
        // Same ref-shape rule as removals, plus: a ref in BOTH lists is a contradiction (move it or
        // remove it, not both — build lets the removal win, but say so here instead of silently).
        public void ValidateOverrides()
        {
            var removed = new HashSet<string>(spec.Removals, System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < spec.Overrides.Count; i++)
            {
                var o = spec.Overrides[i];
                string who = $"override[{i}]" + (string.IsNullOrWhiteSpace(o.Ref) ? "" : $" ('{o.Ref}')");
                if (string.IsNullOrWhiteSpace(o.Ref))
                    Problems.Add($"{who}: empty ref");
                else if (!LooksExternalRef(o.Ref) || !TryExternalRef(o.Ref, out _))
                    Problems.Add($"{who}: must be a well-formed external \"<master>:0xFORMID\" ref of an existing placed ref");
                else if (removed.Contains(o.Ref))
                    Problems.Add($"{who}: also listed in removals[] — contradictory (the removal wins); drop one");
            }
        }
    }
}
