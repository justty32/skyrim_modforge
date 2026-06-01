using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// Regression tests for the SHOUTS (SHOU) + Words of Power (WOOP) pipeline. All master-free: the
// spec only uses in-spec editorId refs (and well-formed external "<master>:0xFORMID" refs, which
// resolve from the string without reading Skyrim.esm), so these run anywhere.
public class ShoutTests
{
    // A complete 3-word shout: 1 MGEF -> 3 Voice spells -> 3 WOOP -> 1 SHOU, all in-spec.
    private static ModSpec MakeShoutSpec() => new()
    {
        PluginName = "ShoutTest.esp",
        MagicEffects =
        {
            new MagicEffectSpec
            {
                EditorId = "MF_FrostFx", Name = "Frost", Archetype = "ValueModifier",
                ActorValue = "Health", MagicSkill = "Destruction",
                CastType = "FireAndForget", TargetType = "Aimed", BaseCost = 1.5f,
                Flags = { "Hostile", "Detrimental", "NoDuration", "NoArea" },
            },
        },
        Spells =
        {
            new SpellSpec { EditorId = "MF_Voice1", Name = "Voice", SpellType = "Voice", CastType = "FireAndForget", TargetType = "Aimed",
                Effects = { new EffectSpec { MagicEffect = "MF_FrostFx", Magnitude = 20 } } },
            new SpellSpec { EditorId = "MF_Voice2", Name = "Voice", SpellType = "Voice", CastType = "FireAndForget", TargetType = "Aimed",
                Effects = { new EffectSpec { MagicEffect = "MF_FrostFx", Magnitude = 40 } } },
            new SpellSpec { EditorId = "MF_Voice3", Name = "Voice", SpellType = "Voice", CastType = "FireAndForget", TargetType = "Aimed",
                Effects = { new EffectSpec { MagicEffect = "MF_FrostFx", Magnitude = 60 } } },
        },
        WordsOfPower =
        {
            new WordOfPowerSpec { EditorId = "MF_W1", Translation = "Iss",  Name = "Iss" },
            new WordOfPowerSpec { EditorId = "MF_W2", Translation = "Kren", Name = "Kren" },
            new WordOfPowerSpec { EditorId = "MF_W3", Translation = "Vus",  Name = "Vus" },
        },
        Shouts =
        {
            new ShoutSpec
            {
                EditorId = "MF_Shout", Name = "Winter's Embrace", Description = "frost",
                Words =
                {
                    new ShoutWordSpec { Word = "MF_W1", Spell = "MF_Voice1", RecoveryTime = 15 },
                    new ShoutWordSpec { Word = "MF_W2", Spell = "MF_Voice2", RecoveryTime = 20 },
                    new ShoutWordSpec { Word = "MF_W3", Spell = "MF_Voice3", RecoveryTime = 45 },
                },
            },
        },
    };

    private static ISkyrimModGetter Build(ModSpec spec)
        => Generator.Build(spec, ModKey.FromNameAndExtension(spec.PluginName)).Mod;

    [Fact]
    public void ValidSpec_HasNoProblems()
    {
        Assert.Empty(Generator.Validate(MakeShoutSpec()));
    }

    [Fact]
    public void Build_EmitsThreeWordsOfPower()
    {
        var mod = Build(MakeShoutSpec());
        var words = mod.WordsOfPower.ToList();
        Assert.Equal(3, words.Count);
        Assert.Equal(new[] { "MF_W1", "MF_W2", "MF_W3" }, words.Select(w => w.EditorID));
        // Translation (the dragon-tongue text shown in-game) round-trips.
        Assert.Equal("Iss", words.Single(w => w.EditorID == "MF_W1").Translation?.String);
    }

    [Fact]
    public void Build_EmitsShoutWithThreeRows_PointingAtRightWordsAndSpells()
    {
        var mod = Build(MakeShoutSpec());
        var shout = Assert.Single(mod.Shouts);
        Assert.Equal("MF_Shout", shout.EditorID);
        Assert.Equal("Winter's Embrace", shout.Name?.String);
        Assert.Equal(3, shout.WordsOfPower.Count);

        // Each row's Word FormLink must point at the matching WOOP, and Spell at the matching SPEL.
        var wordFk = mod.WordsOfPower.ToDictionary(w => w.EditorID!, w => w.FormKey);
        var spellFk = mod.Spells.ToDictionary(s => s.EditorID!, s => s.FormKey);
        var rows = shout.WordsOfPower.ToList();

        Assert.Equal(wordFk["MF_W1"], rows[0].Word.FormKey);
        Assert.Equal(wordFk["MF_W2"], rows[1].Word.FormKey);
        Assert.Equal(wordFk["MF_W3"], rows[2].Word.FormKey);

        Assert.Equal(spellFk["MF_Voice1"], rows[0].Spell.FormKey);
        Assert.Equal(spellFk["MF_Voice2"], rows[1].Spell.FormKey);
        Assert.Equal(spellFk["MF_Voice3"], rows[2].Spell.FormKey);
    }

    [Fact]
    public void Build_PreservesRecoveryTimes()
    {
        var mod = Build(MakeShoutSpec());
        var rows = Assert.Single(mod.Shouts).WordsOfPower.ToList();
        Assert.Equal(15f, rows[0].RecoveryTime);
        Assert.Equal(20f, rows[1].RecoveryTime);
        Assert.Equal(45f, rows[2].RecoveryTime);
    }

    [Fact]
    public void Build_WiresExternalMenuDisplayObject()
    {
        var spec = MakeShoutSpec();
        spec.Shouts[0].MenuDisplayObject = "Skyrim.esm:0x0A59AC";
        var result = Generator.Build(spec, ModKey.FromNameAndExtension(spec.PluginName));
        var shout = Assert.Single(result.Mod.Shouts);
        Assert.False(shout.MenuDisplayObject.IsNull);
        Assert.Equal(0x0A59ACu, shout.MenuDisplayObject.FormKey.ID);
        // External ref counts toward the external-master link stat.
        Assert.True(result.Stats.ExternalLinks >= 1);
    }

    [Fact]
    public void Build_DeliveringSpellsAreVoiceType()
    {
        var mod = Build(MakeShoutSpec());
        foreach (var s in mod.Spells)
            Assert.Equal(SpellType.Voice, s.Type);
    }

    [Fact]
    public void VoiceSpell_GetsDefaultEitherHandEquipType()
    {
        // A Voice/shout charge-spell with no explicit equipType MUST default to EitherHand
        // (Skyrim.esm:0x013F44) — without an EQUP slot the player learns the shout but can't shout it
        // (the in-game symptom: "no word fires / nothing happens"). Every vanilla shout word-spell
        // carries EitherHand too.
        var mod = Build(MakeShoutSpec());
        foreach (var s in mod.Spells)
        {
            Assert.False(s.EquipmentType.IsNull, $"{s.EditorID} must have an EQUP slot");
            Assert.Equal("Skyrim.esm", s.EquipmentType.FormKey.ModKey.FileName);
            Assert.Equal(0x013F44u, s.EquipmentType.FormKey.ID);
        }
    }

    [Fact]
    public void ExplicitEquipType_OverridesTheDefault()
    {
        var spec = MakeShoutSpec();
        spec.Spells[0].EquipType = "Skyrim.esm:0x00013F45"; // BothHands
        var mod = Build(spec);
        var sp = mod.Spells.Single(x => x.EditorID == "MF_Voice1");
        Assert.Equal(0x013F45u, sp.EquipmentType.FormKey.ID);
    }

    [Fact]
    public void Validate_RejectsTooManyWordRows()
    {
        var spec = MakeShoutSpec();
        spec.Shouts[0].Words.Add(new ShoutWordSpec { Word = "MF_W1", Spell = "MF_Voice1", RecoveryTime = 10 });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("MF_Shout") && p.Contains("1–3"));
    }

    [Fact]
    public void Validate_RejectsZeroWordRows()
    {
        var spec = MakeShoutSpec();
        spec.Shouts[0].Words.Clear();
        Assert.Contains(Generator.Validate(spec), p => p.Contains("1–3"));
    }

    [Fact]
    public void Validate_RejectsNegativeRecoveryTime()
    {
        var spec = MakeShoutSpec();
        spec.Shouts[0].Words[1].RecoveryTime = -1f;
        Assert.Contains(Generator.Validate(spec), p => p.Contains("recoveryTime") && p.Contains("negative"));
    }

    [Fact]
    public void Validate_RejectsUnresolvedWordAndSpellRefs()
    {
        var spec = MakeShoutSpec();
        spec.Shouts[0].Words[0].Word = "NoSuchWord";
        spec.Shouts[0].Words[0].Spell = "NoSuchSpell";
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("word[0] word") && p.Contains("NoSuchWord"));
        Assert.Contains(problems, p => p.Contains("word[0] spell") && p.Contains("NoSuchSpell"));
    }

    [Fact]
    public void Validate_RejectsEmptyWordOfPowerText()
    {
        var spec = MakeShoutSpec();
        spec.WordsOfPower[0].Translation = "";
        spec.WordsOfPower[0].Name = "";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("MF_W1") && p.Contains("empty translation"));
    }

    [Fact]
    public void Validate_RejectsDuplicateEditorId()
    {
        var spec = MakeShoutSpec();
        spec.WordsOfPower.Add(new WordOfPowerSpec { EditorId = "MF_W1", Translation = "Dup" });
        Assert.Contains(Generator.Validate(spec), p => p.Contains("duplicate editorId 'MF_W1'"));
    }
}
