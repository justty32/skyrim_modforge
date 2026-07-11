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
            var n = new NpcSpec
            {
                EditorId = ed, Name = cn.Name,
                // identity
                Race = cn.Race, Female = cn.Female,
                Unique = cn.Unique, Essential = cn.Essential, Protected = cn.Protected,
                // Wardrobe: the equipped list wins over defaultOutfit. A PROTEUS clone's outfit
                // ref points at a runtime template that's an EMPTY SHELL on disk (in-game
                // confirmed: naked clone), while `equipped` is what the actor actually wore —
                // consumed as inventory items below (the engine auto-equips the best).
                Outfit = cn.Equipped.Count > 0 ? "" : cn.DefaultOutfit,
                // Stats: class + level + autoCalc make the clone's H/M/S believable. autoCalc
                // ONLY with a class (class-less autoCalc = ~0 HP permanent-bleedout footgun).
                Class = cn.Class, Level = cn.Level,
                AutoCalcStats = !string.IsNullOrWhiteSpace(cn.Class),
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
            foreach (var eq in cn.Equipped)
                if (!string.IsNullOrWhiteSpace(eq)) n.Items.Add(new NpcItemSpec { Item = eq, Count = 1 });
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
