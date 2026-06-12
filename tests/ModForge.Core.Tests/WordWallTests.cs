using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Regression tests for the WORD WALL + SHOUT TEACHING feature: SHOU/WOOP emission, the teaching
// quest + generated Papyrus fragment + VMAD object properties, the WordWallTrigger placement, and
// the validate guardrails. These verify STRUCTURE — the CK compile-bind + in-game learning are
// out of scope (no Skyrim / Papyrus compiler in CI).
public class WordWallTests
{
    private static readonly ModKey Key = ModKey.FromNameAndExtension("Test.esp");

    // A minimal but complete shout + word-wall spec: a 3-word custom shout and a wall teaching word 1.
    private static ModSpec MakeSpec() => new()
    {
        PluginName = "Test.esp",
        Cells = { new CellSpec { EditorId = "MfTestCell", Name = "Test Cell" } },
        MagicEffects = { new MagicEffectSpec { EditorId = "MfEff", Name = "E", Archetype = "Stagger", CastType = "FireAndForget", TargetType = "Aimed" } },
        Spells =
        {
            new SpellSpec { EditorId = "MfSpell1", Name = "S1", SpellType = "Voice", CastType = "FireAndForget", TargetType = "Aimed", Effects = { new EffectSpec { MagicEffect = "MfEff", Magnitude = 1 } } },
            new SpellSpec { EditorId = "MfSpell2", Name = "S2", SpellType = "Voice", CastType = "FireAndForget", TargetType = "Aimed", Effects = { new EffectSpec { MagicEffect = "MfEff", Magnitude = 2 } } },
            new SpellSpec { EditorId = "MfSpell3", Name = "S3", SpellType = "Voice", CastType = "FireAndForget", TargetType = "Aimed", Effects = { new EffectSpec { MagicEffect = "MfEff", Magnitude = 3 } } },
        },
        WordsOfPower =
        {
            new WordOfPowerSpec { EditorId = "MfW1", Name = "Dov", Translation = "Dragon" },
            new WordOfPowerSpec { EditorId = "MfW2", Name = "Ah", Translation = "Hunter" },
            new WordOfPowerSpec { EditorId = "MfW3", Name = "Vul", Translation = "Forged" },
        },
        Shouts =
        {
            new ShoutSpec
            {
                EditorId = "MfShout", Name = "Forged Voice",
                Words =
                {
                    new ShoutWordSpec { Word = "MfW1", Spell = "MfSpell1", RecoveryTime = 10 },
                    new ShoutWordSpec { Word = "MfW2", Spell = "MfSpell2", RecoveryTime = 15 },
                    new ShoutWordSpec { Word = "MfW3", Spell = "MfSpell3", RecoveryTime = 20 },
                },
            },
        },
        WordWalls =
        {
            new WordWallSpec
            {
                EditorId = "MfWall", Name = "Wall", Shout = "MfShout", WordIndex = 1,
                ScriptName = "MfWallScript", Cell = "MfTestCell",
                Position = new Vec3 { X = 1, Y = 2, Z = 3 },
            },
        },
    };

    [Fact]
    public void Shout_emitted_with_three_words_wired()
    {
        var mod = Generator.Build(MakeSpec(), Key).Mod;
        var shout = Assert.Single(mod.Shouts);
        Assert.Equal("MfShout", shout.EditorID);
        Assert.Equal(3, shout.WordsOfPower.Count);

        // Each slot points at the right WOOP + SPEL with its recovery time (in order).
        var woopByEd = mod.WordsOfPower.ToDictionary(w => w.EditorID!, w => w.FormKey);
        var spellByEd = mod.Spells.ToDictionary(s => s.EditorID!, s => s.FormKey);
        Assert.Equal(woopByEd["MfW1"], shout.WordsOfPower[0].Word.FormKey);
        Assert.Equal(spellByEd["MfSpell1"], shout.WordsOfPower[0].Spell.FormKey);
        Assert.Equal(10, shout.WordsOfPower[0].RecoveryTime);
        Assert.Equal(woopByEd["MfW3"], shout.WordsOfPower[2].Word.FormKey);
        Assert.Equal(spellByEd["MfSpell3"], shout.WordsOfPower[2].Spell.FormKey);
    }

    // NOTE: SHOU/WOOP build + validate behaviour is owned by the integrated shouts feature
    // (see ShoutTests.cs) — the word-wall feature reuses it and does NOT re-test it here. Master's
    // BuildShouts adds one slot per authored word (1–3, validated there), rather than padding to 3.

    [Fact]
    public void Teaching_quest_emitted_start_enabled_with_fragment_attached()
    {
        var mod = Generator.Build(MakeSpec(), Key).Mod;
        var quest = Assert.Single(mod.Quests);
        Assert.Equal("MfWall", quest.EditorID);
        Assert.True(quest.Flags.HasFlag(Quest.Flag.StartGameEnabled));

        var entry = Assert.Single(quest.VirtualMachineAdapter!.Scripts);
        Assert.Equal("MfWallScript", entry.Name);
    }

    [Fact]
    public void Teaching_fragment_binds_shout_and_word_object_properties()
    {
        var mod = Generator.Build(MakeSpec(), Key).Mod;
        var entry = mod.Quests.Single().VirtualMachineAdapter!.Scripts.Single();

        var shoutFk = mod.Shouts.Single().FormKey;
        var wordFk = mod.WordsOfPower.Single(w => w.EditorID == "MfW1").FormKey;   // word 1

        var shoutProp = Assert.IsType<ScriptObjectProperty>(entry.Properties.Single(p => p.Name == "WordWallShout"));
        var wordProp = Assert.IsType<ScriptObjectProperty>(entry.Properties.Single(p => p.Name == "WordWallWord"));
        Assert.Equal(shoutFk, shoutProp.Object.FormKey);
        Assert.Equal(wordFk, wordProp.Object.FormKey);
    }

    [Fact]
    public void WordIndex_selects_which_word_is_taught()
    {
        var spec = MakeSpec();
        spec.WordWalls[0].WordIndex = 2;   // teach the SECOND word
        var mod = Generator.Build(spec, Key).Mod;
        var wordProp = (ScriptObjectProperty)mod.Quests.Single().VirtualMachineAdapter!.Scripts.Single()
            .Properties.Single(p => p.Name == "WordWallWord");
        var expected = mod.WordsOfPower.Single(w => w.EditorID == "MfW2").FormKey;
        Assert.Equal(expected, wordProp.Object.FormKey);
    }

    [Fact]
    public void Trigger_placed_referencing_word_wall_activator_base()
    {
        var mod = Generator.Build(MakeSpec(), Key).Mod;
        // The trigger REFR is placed in the in-spec test cell; its base is the vanilla
        // WordWallTrigger activator (0x05095E) by FormKey only, so no master read is needed.
        var trigger = mod.EnumerateMajorRecords<IPlacedObjectGetter>()
            .Single(p => p.EditorID == "MfWallTrigger");
        Assert.Equal(0x05095Eu, trigger.Base.FormKey.ID);
        Assert.Equal("Skyrim.esm", trigger.Base.FormKey.ModKey.FileName);
        Assert.NotNull(trigger.Placement);
        Assert.Equal(1f, trigger.Placement!.Position.X);
    }

    [Fact]
    public void Build_stats_count_the_word_wall()
    {
        var stats = Generator.Build(MakeSpec(), Key).Stats;
        Assert.Equal(1, stats.WordWalls);
        Assert.True(stats.ScriptsAttached >= 1);   // the teaching fragment
    }

    [Fact]
    public void Generated_script_contains_teaching_calls()
    {
        var psc = Generator.GenerateWordWallScript(MakeSpec().WordWalls[0]);
        Assert.Contains("Scriptname MfWallScript extends Quest", psc);
        Assert.Contains("Shout Property WordWallShout Auto", psc);
        Assert.Contains("WordOfPower Property WordWallWord Auto", psc);
        Assert.Contains("AddShout(WordWallShout)", psc);
        Assert.Contains("TeachWord(WordWallWord)", psc);
    }

    // --- validate guardrails ---------------------------------------------------------

    [Fact]
    public void Valid_spec_has_no_problems()
    {
        Assert.Empty(Generator.Validate(MakeSpec()));
    }

    [Fact]
    public void WordIndex_out_of_range_is_rejected()
    {
        var spec = MakeSpec();
        spec.WordWalls[0].WordIndex = 4;
        Assert.Contains(Generator.Validate(spec), p => p.Contains("wordIndex 4 out of range"));
    }

    [Fact]
    public void Vanilla_shout_wall_without_explicit_word_is_rejected()
    {
        var spec = MakeSpec();
        spec.WordWalls[0].Shout = "Skyrim.esm:0x013E07";   // vanilla Unrelenting Force
        spec.WordWalls[0].Word = "";                        // can't auto-derive from an out-of-spec shout
        Assert.Contains(Generator.Validate(spec), p => p.Contains("no explicit `word`"));
    }

    [Fact]
    public void Word_wall_unresolved_shout_ref_is_rejected()
    {
        var spec = MakeSpec();
        spec.WordWalls[0].Shout = "NoSuchShout";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("unresolved ref 'NoSuchShout'"));
    }

    [Fact]
    public void Word_wall_missing_location_is_rejected()
    {
        var spec = MakeSpec();
        spec.WordWalls[0].Cell = "";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("empty cell"));
    }
}
