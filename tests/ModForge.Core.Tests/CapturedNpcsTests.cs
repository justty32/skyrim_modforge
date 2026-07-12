using System.Linq;
using System.Text.Json;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// capturedNpcs[] — the in-game actor eyedropper (Idea #24 addendum, sibling of capturedItems[]).
// Each entry macro-expands (Generator.ExpandCapturedNpcs) into an ordinary NpcSpec (identity + the
// full TESNPC face/body recipe) + an ACHR placement at the capture spot, so the battle-tested NPC
// build/wire/place passes do the real work. Phase 1 writes the RECIPE only (faces render gray/dark
// until the FaceGeom baking milestone); `base`/`dead`/`activeEffects`/perk ranks ride along
// unconsumed. Everything here is master-free except the one RequiresSkyrim resolve test.
public class CapturedNpcsTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private const string NordRace = "Skyrim.esm:0x013746";

    // A fully-populated capture, shaped exactly like the DLL's export (SceneExporter.cpp).
    private static CapturedNpcSpec FullSample() => new()
    {
        Name = "Hulda", Base = "Skyrim.esm:0x013BA3", Race = NordRace,
        Female = true, Unique = true, Essential = true, Protected = true,
        Weight = 60f, Height = 1.02f,
        BodyTint = new ColorSpec { R = 230, G = 180, B = 160 },
        HairColor = new CapturedHairColorSpec { Id = "Skyrim.esm:0x0A99EB", R = 80, G = 60, B = 40 },
        FaceTexture = "Skyrim.esm:0x0A26B4",
        DefaultOutfit = "Skyrim.esm:0x0209A6",
        HeadParts = { "Skyrim.esm:0x051111", "Skyrim.esm:0x051112" },
        TintLayers =
        {
            new TintLayerSpec { Index = 22, Preset = 1, Value = 65f, Color = new ColorSpec { R = 120, G = 60, B = 50, A = 255 } },
        },
        FaceMorphs = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f, -0.1f, -0.2f, -0.3f, -0.4f, -0.5f, -0.6f, -0.7f, -0.8f },
        FaceParts = { 3, 0, 2, 1 },
        Perks = { new CapturedNpcPerkSpec { Perk = "Skyrim.esm:0x0581E7", Rank = 1 } },
        Class = "Skyrim.esm:0x01CE78",   // CombatWarrior1H-ish — drives autoCalcStats
        Level = 12,
        CombatStyle = "Skyrim.esm:0x02B928",
        VoiceType = "Skyrim.esm:0x013AE6",
        Spells = { "Skyrim.esm:0x012FCD" },   // Flames
        Inventory =
        {
            new CapturedNpcItemSpec { Item = "Skyrim.esm:0x03619E", Worn = true },  // college robes — outfit route
            new CapturedNpcItemSpec { Item = "Skyrim.esm:0x012EB7", Count = 1 },   // iron sword (auto-equips)
            new CapturedNpcItemSpec { Item = "Skyrim.esm:0x064B3F", Count = 3 },   // green apples ×3
            new CapturedNpcItemSpec                                                 // player-crafted staff — instance ench
            {
                Item = "Skyrim.esm:0x029B78", Name = "Hatak's Staff",
                Enchantment = new CapturedEnchantSpec
                {
                    Target = "weapon", Amount = 500,
                    Effects = { new EffectSpec { MagicEffect = "Skyrim.esm:0x0001CEAD", Magnitude = 25 } },
                },
            },
        },
        Dead = false,
        ActiveEffects = { new CapturedActiveEffectSpec { MagicEffect = "Skyrim.esm:0x0003EB42", Magnitude = 10, Duration = 60, Elapsed = 5 } },
        Position = new Vec3 { X = 100f, Y = 200f, Z = 300f },
        Rotation = new Vec3 { X = 0f, Y = 0f, Z = 90f },
        Cell = "Skyrim.esm:0x01605E",
    };

    // --- validation (offline) ---------------------------------------------------------------

    [Fact]
    public void Validate_MissingRace_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(new CapturedNpcSpec { Name = "Ghost" });
        Assert.Contains(Validate(s), p => p.Contains("capturedNpc") && p.Contains("missing race"));
    }

    [Fact]
    public void Validate_BadHeadPartRef_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(new CapturedNpcSpec { Name = "H", Race = NordRace, HeadParts = { "NotARef" } });
        Assert.Contains(Validate(s), p => p.Contains("capturedNpc") && p.Contains("headPart"));
    }

    [Fact]
    public void Validate_WrongMorphCount_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(new CapturedNpcSpec { Name = "H", Race = NordRace, FaceMorphs = { 0.1f, 0.2f } });
        Assert.Contains(Validate(s), p => p.Contains("capturedNpc") && p.Contains("faceMorphs"));
    }

    [Fact]
    public void Validate_WeightOutOfRange_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(new CapturedNpcSpec { Name = "H", Race = NordRace, Weight = 150f });
        Assert.Contains(Validate(s), p => p.Contains("capturedNpc") && p.Contains("weight"));
    }

    [Fact]
    public void Validate_BothCellAndWorldspace_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(new CapturedNpcSpec
        {
            Name = "H", Race = NordRace,
            Cell = "Skyrim.esm:0x01605E", Worldspace = "Skyrim.esm:0x00003C",
        });
        Assert.Contains(Validate(s), p => p.Contains("capturedNpc") && p.Contains("BOTH"));
    }

    [Fact]
    public void Validate_FullSample_NoProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(FullSample());
        Assert.DoesNotContain(Validate(s), p => p.Contains("capturedNpc"));
    }

    // --- expansion (offline) ----------------------------------------------------------------

    [Fact]
    public void Expand_CarriesIdentityAndRecipe_AndPlacesInCell()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(FullSample());
        Generator.ExpandCapturedNpcs(s);

        var n = Assert.Single(s.Npcs);
        Assert.Equal("Hulda", n.Name);
        Assert.Equal(NordRace, n.Race);
        Assert.True(n.Female);
        Assert.True(n.Unique); Assert.True(n.Essential); Assert.True(n.Protected);
        // Worn armour arrives via a MINTED outfit (engine only auto-wears outfit armour —
        // inventory armour stays in the pocket); the PROTEUS shell defaultOutfit is dropped.
        Assert.Equal(n.EditorId + "_Outfit", n.Outfit);
        var otf = Assert.Single(s.Outfits);
        Assert.Equal(n.Outfit, otf.EditorId);
        Assert.Equal("Skyrim.esm:0x03619E", Assert.Single(otf.Items));
        Assert.Equal("Skyrim.esm:0x01CE78", n.Class);
        Assert.Equal(12, n.Level);
        Assert.True(n.AutoCalcStats);   // class present → stats calc on
        Assert.Equal("Skyrim.esm:0x02B928", n.CombatStyle);
        Assert.Equal("Skyrim.esm:0x013AE6", n.VoiceType);
        Assert.Equal("Skyrim.esm:0x012FCD", Assert.Single(n.Spells));
        Assert.Equal(3, n.Items.Count);   // sword + apples + MINTED staff
        Assert.Equal("Skyrim.esm:0x012EB7", n.Items[0].Item);
        Assert.Equal(3, n.Items[1].Count);   // the apples kept their stack count
        // the instance-enchanted staff minted a WEAP template clone + a fresh ENCH
        var staff = Assert.Single(s.Weapons);
        Assert.Equal(n.EditorId + "_Inv4", staff.EditorId);
        Assert.Equal("Hatak's Staff", staff.Name);
        Assert.Equal("Skyrim.esm:0x029B78", staff.Template);
        Assert.Equal((ushort)500, staff.EnchantmentAmount);
        var ench = Assert.Single(s.Enchantments);
        Assert.Equal(staff.Enchantment, ench.EditorId);
        Assert.Equal("weapon", ench.EnchantType);
        Assert.Equal(staff.EditorId, n.Items[2].Item);   // inventory row consumes the minted id
        Assert.Equal(60f, n.Weight);
        Assert.Equal(1.02f, n.Height);
        Assert.Equal(230, n.BodyTint!.R);
        Assert.Equal("Skyrim.esm:0x0A99EB", n.HairColor);   // the CLFM ref; captured rgb is advisory
        Assert.Equal("Skyrim.esm:0x0A26B4", n.FaceTexture);
        Assert.Equal(2, n.HeadParts.Count);
        var tl = Assert.Single(n.TintLayers);
        Assert.Equal(22, tl.Index); Assert.Equal(65f, tl.Value); Assert.Equal(255, tl.Color!.A);
        Assert.Equal(18, n.FaceMorphs.Count);
        Assert.Equal(new[] { 3, 0, 2, 1 }, n.FaceParts);
        Assert.Equal("Skyrim.esm:0x0581E7", Assert.Single(n.Perks));   // ref only; rank is advisory

        var pl = Assert.Single(s.Placements);
        Assert.Equal(n.EditorId, pl.Base);
        Assert.Equal("npc", pl.Kind);
        Assert.Equal("Skyrim.esm:0x01605E", pl.Cell);
        Assert.Equal("", pl.Worldspace);
        Assert.Equal(300f, pl.Position.Z);
        Assert.Equal(90f, pl.Rotation.Z);
        Assert.True(pl.Persistent);
    }

    [Fact]
    public void Expand_ExteriorAnchor_UsesWorldspace()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        var cn = FullSample();
        cn.Cell = ""; cn.Worldspace = "Skyrim.esm:0x00003C";
        s.CapturedNpcs.Add(cn);
        Generator.ExpandCapturedNpcs(s);

        var pl = Assert.Single(s.Placements);
        Assert.Equal("Skyrim.esm:0x00003C", pl.Worldspace);
        Assert.Equal("", pl.Cell);
    }

    [Fact]
    public void Expand_NoAnchor_MintsNpcButNoPlacement()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        var cn = FullSample();
        cn.Cell = ""; cn.Worldspace = "";
        s.CapturedNpcs.Add(cn);
        Generator.ExpandCapturedNpcs(s);

        Assert.Single(s.Npcs);        // the NPC_ still mints (usable via placeatme)
        Assert.Empty(s.Placements);
    }

    [Fact]
    public void Expand_NoEquipped_KeepsOutfit_AndNoAutoCalcWithoutClass()
    {
        // The Mirabelle case: a vanilla NPC whose outfit ref has real on-disk content and whose
        // capture carried no equipped list — the outfit passes through. No class → autoCalc off.
        var s = new ModSpec { PluginName = "M.esp" };
        var cn = FullSample();
        cn.Inventory.Clear(); cn.Class = ""; cn.Level = 0;
        s.CapturedNpcs.Add(cn);
        Generator.ExpandCapturedNpcs(s);

        var n = Assert.Single(s.Npcs);
        Assert.Equal("Skyrim.esm:0x0209A6", n.Outfit);
        Assert.Empty(s.Outfits);
        Assert.Empty(n.Items);
        Assert.False(n.AutoCalcStats);   // class-less autoCalc = the 0-HP bleedout footgun
    }

    [Fact]
    public void Expand_DuplicateNames_UniqueEditorIds()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(new CapturedNpcSpec { Name = "Guard", Race = NordRace });
        s.CapturedNpcs.Add(new CapturedNpcSpec { Name = "Guard", Race = NordRace });
        Generator.ExpandCapturedNpcs(s);

        Assert.Equal(2, s.Npcs.Count);
        Assert.NotEqual(s.Npcs[0].EditorId, s.Npcs[1].EditorId);
    }

    [Fact]
    public void Expand_Idempotent()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(FullSample());
        Generator.ExpandCapturedNpcs(s);
        Generator.ExpandCapturedNpcs(s);   // guard flag → no double expansion

        Assert.Single(s.Npcs);
        Assert.Single(s.Placements);
    }

    // --- build (offline: mint with external refs is master-free) -----------------------------

    // THE mapping lock: 18 distinct values in engine-array order (RE::TESNPC::FaceData::Morphs)
    // must land on exactly these Mutagen NpcFaceMorph named fields. Both sides are NAM9 file
    // order — structurally verified 2026-07-11 (table in plans/captured-npcs-consumption.md);
    // this test pins it against regressions in either mapping code or a Mutagen upgrade.
    [Fact]
    public void Build_FaceMorphIndexMapping_IsLocked()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.Npcs.Add(new NpcSpec
        {
            EditorId = "MF_Morphy", Name = "Morphy", Race = NordRace,
            FaceMorphs = { 0.01f, 0.02f, 0.03f, 0.04f, 0.05f, 0.06f, 0.07f, 0.08f, 0.09f, 0.10f, 0.11f, 0.12f, 0.13f, 0.14f, 0.15f, 0.16f, 0.17f, 0.18f },
        });
        var r = TestBuild.Ok(s);
        var m = r.Mod.Npcs.Single().FaceMorph!;
        Assert.Equal(0.01f, m.NoseLongVsShort);
        Assert.Equal(0.02f, m.NoseUpVsDown);
        Assert.Equal(0.03f, m.JawUpVsDown);
        Assert.Equal(0.04f, m.JawNarrowVsWide);
        Assert.Equal(0.05f, m.JawForwardVsBack);
        Assert.Equal(0.06f, m.CheeksUpVsDown);
        Assert.Equal(0.07f, m.CheeksForwardVsBack);
        Assert.Equal(0.08f, m.EyesUpVsDown);
        Assert.Equal(0.09f, m.EyesInVsOut);
        Assert.Equal(0.10f, m.BrowsUpVsDown);
        Assert.Equal(0.11f, m.BrowsInVsOut);
        Assert.Equal(0.12f, m.BrowsForwardVsBack);
        Assert.Equal(0.13f, m.LipsUpVsDown);
        Assert.Equal(0.14f, m.LipsInVsOut);
        Assert.Equal(0.15f, m.ChinNarrowVsWide);
        Assert.Equal(0.16f, m.ChinUpVsDown);
        Assert.Equal(0.17f, m.ChinUnderbiteVsOverbite);
        Assert.Equal(0.18f, m.EyesForwardVsBack);
        Assert.Equal(0f, m.Unknown);   // the DLL-excluded kUnk slot stays zero
    }

    [Fact]
    public void Build_RecipeFields_ReadBack()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(FullSample());
        var r = TestBuild.Ok(s);   // expansion runs at build pass 0

        var npc = r.Mod.Npcs.Single();
        Assert.True(npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female));
        Assert.True(npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Unique));
        Assert.Equal(60f, npc.Weight);
        Assert.Equal(1.02f, npc.Height);
        Assert.Equal(230, npc.TextureLighting!.Value.R);
        Assert.Equal(180, npc.TextureLighting!.Value.G);
        Assert.Equal(160, npc.TextureLighting!.Value.B);
        Assert.Equal(FormKey.Factory("0A99EB:Skyrim.esm"), npc.HairColor.FormKey);
        Assert.Equal(FormKey.Factory("0A26B4:Skyrim.esm"), npc.HeadTexture.FormKey);
        Assert.Equal(2, npc.HeadParts.Count);
        Assert.Equal(FormKey.Factory("051111:Skyrim.esm"), npc.HeadParts[0].FormKey);
        var tint = Assert.Single(npc.TintLayers);
        Assert.Equal((ushort)22, tint.Index);
        Assert.Equal(0.65f, tint.InterpolationValue);   // 65 raw → /100 for Mutagen
        Assert.Equal(120, tint.Color!.Value.R);
        var parts = npc.FaceParts!;
        Assert.Equal(3u, parts.Nose); Assert.Equal(0u, parts.Unknown);
        Assert.Equal(2u, parts.Eyes); Assert.Equal(1u, parts.Mouth);

        // the ACHR landed in the captured cell with the captured transform
        var achr = r.Mod.Cells.Records.SelectMany(cb => cb.SubBlocks).SelectMany(sb => sb.Cells)
            .SelectMany(c => c.Persistent.Concat(c.Temporary)).OfType<IPlacedNpcGetter>().Single();
        Assert.Equal(npc.FormKey, achr.Base.FormKey);
    }

    // --- json (the DLL's exact export shape must deserialize + expand) -----------------------

    [Fact]
    public void Json_DllShape_DeserializesAndExpands()
    {
        const string json = """
        {
          "pluginName": "Cap.esp",
          "capturedNpcs": [
            { "name": "Hulda", "base": "Skyrim.esm:0x013BA3", "race": "Skyrim.esm:0x013746",
              "female": true, "unique": true,
              "weight": 60.0, "height": 1.02,
              "bodyTint": {"r": 230, "g": 180, "b": 160},
              "hairColor": {"id": "Skyrim.esm:0x0A99EB", "r": 80, "g": 60, "b": 40},
              "faceTexture": "Skyrim.esm:0x0A26B4",
              "defaultOutfit": "Skyrim.esm:0x0209A6",
              "headParts": ["Skyrim.esm:0x051111"],
              "tintLayers": [{"index": 22, "preset": 652, "value": 100, "color": {"r": 172, "g": 159, "b": 151, "a": 0}}],
              "faceMorphs": [0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0, -0.1, -0.2, -0.3, -0.4, -0.5, -0.6, -0.7, -0.8],
              "faceParts": [3, 0, 2, 1],
              "perks": [{"perk": "Skyrim.esm:0x0581E7", "rank": 1}],
              "class": "Skyrim.esm:0x01CE78", "level": 12,
              "combatStyle": "Skyrim.esm:0x02B928", "voiceType": "Skyrim.esm:0x013AE6",
              "spells": ["Skyrim.esm:0x012FCD"],
              "equippedArmor": ["Skyrim.esm:0x03619E"],
              "inventory": [
                {"item": "Skyrim.esm:0x03619E", "count": 1, "worn": true},
                {"item": "Skyrim.esm:0x064B3F", "count": 3},
                {"item": "Skyrim.esm:0x029B78", "count": 1, "name": "Hatak's Staff",
                 "enchantment": {"target": "weapon", "amount": 500,
                   "effects": [{"magicEffect": "Skyrim.esm:0x0001CEAD", "magnitude": 25.0, "area": 0, "duration": 0}]}}
              ],
              "activeEffects": [{"magicEffect": "Skyrim.esm:0x0003EB42", "magnitude": 10.0, "duration": 60.0, "elapsed": 5.0, "source": "Skyrim.esm:0x0001CEAD"}],
              "position": {"x": 100.0, "y": 200.0, "z": 300.0},
              "rotation": {"x": 0.0, "y": 0.0, "z": 90.0},
              "cell": "Skyrim.esm:0x01605E" }
          ]
        }
        """;
        var s = JsonSerializer.Deserialize<ModSpec>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var cn = Assert.Single(s.CapturedNpcs);
        Assert.Equal("Skyrim.esm:0x0A99EB", cn.HairColor!.Id);   // nested object shapes survived
        Assert.Equal(18, cn.FaceMorphs.Count);
        Assert.Equal(100f, Assert.Single(cn.TintLayers).Value);   // the DLL exports the raw 0-100 scale
        Assert.Equal("Skyrim.esm:0x0001CEAD", Assert.Single(cn.ActiveEffects).Source);

        Assert.DoesNotContain(Validate(s), p => p.Contains("capturedNpc"));
        Generator.ExpandCapturedNpcs(s);
        var n = Assert.Single(s.Npcs);
        Assert.True(n.Female);
        Assert.Equal("Skyrim.esm:0x01CE78", n.Class);
        Assert.Equal(12, n.Level);
        Assert.True(n.AutoCalcStats);
        Assert.Equal("Skyrim.esm:0x012FCD", Assert.Single(n.Spells));
        Assert.Equal(2, n.Items.Count);              // apples + minted staff
        Assert.Equal(3, n.Items[0].Count);
        Assert.Equal(n.EditorId + "_Outfit", n.Outfit);   // worn row → minted OTFT
        Assert.Single(s.Outfits);
        var staff = Assert.Single(s.Weapons);        // instance ench → minted WEAP + ENCH
        Assert.Equal("Hatak's Staff", staff.Name);
        Assert.Single(s.Enchantments);
        Assert.Equal("Skyrim.esm:0x01605E", Assert.Single(s.Placements).Cell);
    }

    // --- explicit stats (`sc capp` / any v8+ capture): DNAM beats class autocalc ---------------

    // 18 skills in Mutagen `Skill` order (= engine AV 6..23 = the DLL's export order).
    private static List<int> Skills18() =>
        new() { 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58 };

    [Fact]
    public void Expand_ExplicitStats_TurnAutoCalcOff_AndCarryThrough()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        var cn = FullSample();            // FullSample HAS a class — autocalc would normally be on
        cn.Health = 320f; cn.Magicka = 150f; cn.Stamina = 210f;
        cn.Skills = Skills18();
        s.CapturedNpcs.Add(cn);
        Generator.ExpandCapturedNpcs(s);

        var n = Assert.Single(s.Npcs);
        Assert.False(n.AutoCalcStats);   // explicit values win — autocalc would overwrite them
        Assert.Equal("Skyrim.esm:0x01CE78", n.Class);   // class still rides (AI/training semantics)
        Assert.Equal(320, n.Health);
        Assert.Equal(150, n.Magicka);
        Assert.Equal(210, n.Stamina);
        Assert.Equal(18, n.Skills.Count);
        Assert.Equal(41, n.Skills[0]);
        Assert.Equal(58, n.Skills[17]);
    }

    [Fact]
    public void Expand_NoExplicitStats_KeepsClassAutoCalc()
    {
        // The pre-v8 capture shape (no stats fields at all) must behave exactly as before —
        // the in-game-confirmed class-autocalc route.
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(FullSample());
        Generator.ExpandCapturedNpcs(s);

        var n = Assert.Single(s.Npcs);
        Assert.True(n.AutoCalcStats);
        Assert.Equal(0, n.Health);
        Assert.Empty(n.Skills);
    }

    [Fact]
    public void Build_ExplicitStats_LandOnDnam_AndAutoCalcFlagIsOff()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        var cn = FullSample();
        cn.Health = 320f; cn.Magicka = 150f; cn.Stamina = 210f;
        cn.Skills = Skills18();
        s.CapturedNpcs.Add(cn);
        var r = TestBuild.Ok(s);

        var npc = r.Mod.Npcs.Single();
        Assert.False(npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.AutoCalcStats));
        Assert.Equal((ushort)320, npc.PlayerSkills!.Health);
        Assert.Equal((ushort)150, npc.PlayerSkills!.Magicka);
        Assert.Equal((ushort)210, npc.PlayerSkills!.Stamina);
        // index → Skill mapping lock: spec[0] = OneHanded … spec[17] = Enchanting.
        Assert.Equal((byte)41, npc.PlayerSkills!.SkillValues[Skill.OneHanded]);
        Assert.Equal((byte)45, npc.PlayerSkills!.SkillValues[Skill.Smithing]);
        Assert.Equal((byte)58, npc.PlayerSkills!.SkillValues[Skill.Enchanting]);
    }

    [Fact]
    public void Build_ClassButNoExplicitStats_KeepsAutoCalcFlag()
    {
        // The already-in-game-confirmed route must not regress.
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(FullSample());
        var r = TestBuild.Ok(s);
        Assert.True(r.Mod.Npcs.Single().Configuration.Flags.HasFlag(NpcConfiguration.Flag.AutoCalcStats));
    }

    [Fact]
    public void Validate_BadSkillCountAndRange_AreProblems()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(new CapturedNpcSpec { Name = "H", Race = NordRace, Skills = { 10, 20 } });
        Assert.Contains(Validate(s), p => p.Contains("capturedNpc") && p.Contains("skills has 2"));

        var s2 = new ModSpec { PluginName = "M.esp" };
        var cn = new CapturedNpcSpec { Name = "H", Race = NordRace, Skills = Skills18() };
        cn.Skills[3] = 300;   // DNAM skill values are bytes
        s2.CapturedNpcs.Add(cn);
        Assert.Contains(Validate(s2), p => p.Contains("capturedNpc") && p.Contains("skills[3]"));
    }

    [Fact]
    public void Validate_StatOutOfUshortRange_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.CapturedNpcs.Add(new CapturedNpcSpec { Name = "H", Race = NordRace, Health = 70000f });
        Assert.Contains(Validate(s), p => p.Contains("capturedNpc") && p.Contains("health"));
    }

    [Fact]
    public void Json_PlayerCapture_DllShape_ExplicitStatsAndLabelEditorId()
    {
        // `sc capp Hero` → editorId "MFCap_Hero" + explicit H/M/S + 18 skills + player perks.
        const string json = """
        {
          "pluginName": "Cap.esp",
          "capturedNpcs": [
            { "name": "Dovahkiin", "editorId": "MFCap_Hero",
              "base": "Skyrim.esm:0x000007", "race": "Skyrim.esm:0x013746",
              "female": false, "weight": 50.0, "height": 1.0,
              "class": "Skyrim.esm:0x01CE78", "level": 34,
              "health": 320.0, "magicka": 150.0, "stamina": 210.0,
              "skills": [41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58],
              "perks": [{"perk": "Skyrim.esm:0x0BABE4", "rank": 1}],
              "position": {"x": 1.0, "y": 2.0, "z": 3.0},
              "rotation": {"x": 0.0, "y": 0.0, "z": 45.0},
              "cell": "Skyrim.esm:0x01605E" }
          ]
        }
        """;
        var s = JsonSerializer.Deserialize<ModSpec>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var cn = Assert.Single(s.CapturedNpcs);
        Assert.Equal("MFCap_Hero", cn.EditorId);
        Assert.Equal(18, cn.Skills.Count);
        Assert.DoesNotContain(Validate(s), p => p.Contains("capturedNpc"));

        Generator.ExpandCapturedNpcs(s);
        var n = Assert.Single(s.Npcs);
        Assert.Equal("MFCap_Hero", n.EditorId);   // the label IS the identity (explicit editorId wins)
        Assert.False(n.AutoCalcStats);
        Assert.Equal(320, n.Health);
        Assert.Equal("Skyrim.esm:0x0BABE4", Assert.Single(n.Perks));
        Assert.Equal("MFCap_Hero_Ref", Assert.Single(s.Placements).EditorId);
    }

    [Fact]
    public void Build_HandAuthoredNpc_ExplicitStats_LandOnDnam()
    {
        // The same DNAM route is available to a hand-written npcs[] entry (no capture involved).
        var s = new ModSpec { PluginName = "M.esp" };
        s.Npcs.Add(new NpcSpec
        {
            EditorId = "MF_Statty", Name = "Statty", Race = NordRace,
            Health = 500, Magicka = 100, Stamina = 250, Skills = Skills18(),
        });
        var r = TestBuild.Ok(s);
        var npc = r.Mod.Npcs.Single();
        Assert.Equal((ushort)500, npc.PlayerSkills!.Health);
        Assert.Equal((byte)48, npc.PlayerSkills!.SkillValues[Skill.Pickpocket]);
        Assert.False(npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.AutoCalcStats));
    }

    [Fact]
    public void Expand_LegacyMixedEquipped_FoldsIntoOutfit()
    {
        // Pre-split DLL exports had one mixed "equipped" list — folded into armour → outfit.
        var s = new ModSpec { PluginName = "M.esp" };
        var cn = FullSample();
        cn.Inventory.Clear();
        cn.Equipped.Add("Skyrim.esm:0x06B46C");   // the boots-in-pocket clone
        s.CapturedNpcs.Add(cn);
        Generator.ExpandCapturedNpcs(s);

        var n = Assert.Single(s.Npcs);
        Assert.Equal(n.EditorId + "_Outfit", n.Outfit);
        Assert.Equal("Skyrim.esm:0x06B46C", Assert.Single(Assert.Single(s.Outfits).Items));
    }

    // --- build (resolving the recipe refs against the real master) ---------------------------

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void Build_CapturedNpc_VanillaRefsResolve()
    {
        // Real vanilla refs: NordRace + HairColor DarkBrown-ish CLFM + FemaleHeadNord HDPT family.
        var s = new ModSpec { PluginName = "MFCap.esp" };
        var cn = FullSample();
        cn.EditorId = "MF_CapHulda";
        s.CapturedNpcs.Add(cn);
        var r = TestBuild.Ok(s);
        var npc = r.Mod.Npcs.Single(n => n.EditorID == "MF_CapHulda");
        Assert.Equal(FormKey.Factory("013746:Skyrim.esm"), npc.Race.FormKey);
        Assert.Equal(2, npc.HeadParts.Count);
    }
}
