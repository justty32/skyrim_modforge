using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// Regression tests for spell tomes + skill books (BOOK.Teaches). These DON'T need Skyrim.esm:
// Teaches=spell wires an in-spec FormLink and Teaches=skill is an inline enum — only the model
// CLONE needs the master, which we deliberately skip (SkyrimDataPath -> empty temp dir). So these
// assert the teach wiring + validate guardrails structurally, with no game install required.
public class SpellTomeTests
{
    static readonly ModKey OutKey = ModKey.FromNameAndExtension("Test.esp");

    // Build with the master path pointed at an empty dir so no template clone is attempted.
    static BuildResult Build(ModSpec spec)
    {
        var dir = Path.Combine(Path.GetTempPath(), "modforge-tests-no-master");
        Directory.CreateDirectory(dir);
        return Generator.Build(spec, OutKey, new BuildOptions { SkyrimDataPath = dir });
    }

    static IBookGetter Bk(BuildResult r, string editorId) =>
        r.Mod.Books.First(b => b.EditorID == editorId);

    [Fact]
    public void SpellTome_TeachesInSpecSpell_WiresFormLinkToThatSpell()
    {
        var spec = new ModSpec
        {
            Spells = { new SpellSpec { EditorId = "MySpell", Name = "My Spell" } },
            Books =
            {
                new BookSpec
                {
                    EditorId = "MyTome", Name = "Tome",
                    Teaches = new BookTeachesSpec { Kind = "spell", Spell = "MySpell" },
                },
            },
        };

        var r = Build(spec);
        var spellKey = r.Mod.Spells.First(s => s.EditorID == "MySpell").FormKey;
        var teaches = Bk(r, "MyTome").Teaches;

        var spell = Assert.IsAssignableFrom<IBookSpellGetter>(teaches);
        Assert.Equal(spellKey, spell.Spell.FormKey);
    }

    [Fact]
    public void SpellTome_TeachesVanillaSpell_WiresExternalFormLink()
    {
        var spec = new ModSpec
        {
            Books =
            {
                new BookSpec
                {
                    EditorId = "Firebolt", Name = "Firebolt Tome",
                    Teaches = new BookTeachesSpec { Kind = "spell", Spell = "Skyrim.esm:0x012FD0" },
                },
            },
        };

        var r = Build(spec);
        var teaches = Bk(r, "Firebolt").Teaches;

        var spell = Assert.IsAssignableFrom<IBookSpellGetter>(teaches);
        Assert.Equal(0x012FD0u, spell.Spell.FormKey.ID);
        Assert.Equal("Skyrim.esm", spell.Spell.FormKey.ModKey.FileName);
    }

    [Fact]
    public void SkillBook_TeachesSkill_CarriesTheActorValueInline()
    {
        var spec = new ModSpec
        {
            Books =
            {
                new BookSpec
                {
                    EditorId = "SkillBook", Name = "Pyromancy",
                    Teaches = new BookTeachesSpec { Kind = "skill", Skill = "Destruction" },
                },
            },
        };

        var r = Build(spec);
        var teaches = Bk(r, "SkillBook").Teaches;

        var skill = Assert.IsAssignableFrom<IBookSkillGetter>(teaches);
        Assert.Equal(Skill.Destruction, skill.Skill);
    }

    [Fact]
    public void PlainBook_StaysTeachesNothing()
    {
        var spec = new ModSpec
        {
            Books = { new BookSpec { EditorId = "Note", Name = "A Note" } },
        };

        var r = Build(spec);
        // No teaches authored + no template cloned => Teaches stays unset (null) or BookTeachesNothing;
        // both mean "teaches nothing". (With a real template the clone seeds BookTeachesNothing.)
        var teaches = Bk(r, "Note").Teaches;
        Assert.True(teaches is null or IBookTeachesNothingGetter);
    }

    [Fact]
    public void BookFlags_AreParsedAndSet()
    {
        var spec = new ModSpec
        {
            Books =
            {
                new BookSpec { EditorId = "Locked", Name = "Locked", Flags = { "CantBeTaken" } },
            },
        };

        var r = Build(spec);
        Assert.True(Bk(r, "Locked").Flags.HasFlag(Book.Flag.CantBeTaken));
    }

    // ---- Validate guardrails ---------------------------------------------------------------

    [Fact]
    public void Validate_TeachingBookWithoutTemplate_IsFlagged()
    {
        var spec = new ModSpec
        {
            Books =
            {
                new BookSpec { EditorId = "T", Name = "T", Teaches = new() { Kind = "skill", Skill = "Destruction" } },
            },
        };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("no `template`"));
    }

    [Fact]
    public void Validate_TeachingBookWithTemplate_IsValid()
    {
        var spec = new ModSpec
        {
            Books =
            {
                new BookSpec { EditorId = "T", Name = "T", Template = "Skyrim.esm:0x0ED161",
                    Teaches = new() { Kind = "skill", Skill = "Destruction" } },
            },
        };
        Assert.Empty(Generator.Validate(spec));
    }

    [Fact]
    public void Validate_InvalidSkill_IsFlagged()
    {
        var spec = new ModSpec
        {
            Books =
            {
                new BookSpec { EditorId = "T", Name = "T", Template = "Skyrim.esm:0x0ED161",
                    Teaches = new() { Kind = "skill", Skill = "NotASkill" } },
            },
        };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("not a valid Skill"));
    }

    [Fact]
    public void Validate_SpellRefToNonSpellInSpecRecord_IsFlagged()
    {
        var spec = new ModSpec
        {
            MiscItems = { new MiscSpec { EditorId = "Gem", Name = "Gem" } },
            Books =
            {
                new BookSpec { EditorId = "T", Name = "T", Template = "Skyrim.esm:0x0ED161",
                    Teaches = new() { Kind = "spell", Spell = "Gem" } },
            },
        };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("is not an in-spec spell"));
    }

    [Fact]
    public void Validate_UnknownTeachesKind_IsFlagged()
    {
        var spec = new ModSpec
        {
            Books =
            {
                new BookSpec { EditorId = "T", Name = "T", Template = "Skyrim.esm:0x0ED161",
                    Teaches = new() { Kind = "potion" } },
            },
        };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("teaches.kind 'potion' invalid"));
    }

    [Fact]
    public void Validate_InvalidBookFlag_IsFlagged()
    {
        var spec = new ModSpec
        {
            Books = { new BookSpec { EditorId = "T", Name = "T", Template = "Skyrim.esm:0x0ED161", Flags = { "Nonsense" } } },
        };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("invalid flag 'Nonsense'"));
    }
}
