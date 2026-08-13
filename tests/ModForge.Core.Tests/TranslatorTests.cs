using ModForge;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge.Tests;

// Translator is in the documented library API (docs/for_agent_lib.md) and had zero tests before
// 2026-08-13. Extract and Apply must iterate the SAME slots in the same way — if they drift, a
// translation silently lands on the wrong record, which no build-time check can catch.
// ApplyLocalized is not covered here: it writes a .STRINGS set to disk and is exercised by the
// `applyloc` CLI path.
public class TranslatorTests
{
    private static SkyrimMod NewMod() =>
        new(ModKey.FromNameAndExtension("TranslateTest.esp"), SkyrimRelease.SkyrimSE);

    private static SkyrimMod ModWithText()
    {
        var mod = NewMod();
        var book = mod.Books.AddNew();
        book.EditorID = "TT_Book";
        book.Name = "A Title";
        book.BookText = "Body prose.";

        var weapon = mod.Weapons.AddNew();
        weapon.EditorID = "TT_Sword";
        weapon.Name = "Sword";
        return mod;
    }

    [Fact]
    public void ExtractFindsEveryTranslatableSlot_WithSourceSetAndTargetEmpty()
    {
        var entries = Translator.Extract(ModWithText());

        Assert.Contains(entries, e => e.Field == "Name" && e.Source == "A Title");
        Assert.Contains(entries, e => e.Field == "BookText" && e.Source == "Body prose.");
        Assert.Contains(entries, e => e.Field == "Name" && e.Source == "Sword");
        Assert.All(entries, e => Assert.Equal("", e.Target));
    }

    [Fact]
    public void ApplyWritesOnlyNonEmptyTargets()
    {
        var mod = ModWithText();
        var entries = Translator.Extract(mod);
        // Translate the book title, leave everything else untranslated.
        foreach (var e in entries)
            if (e.Source == "A Title") e.Target = "書名";

        var applied = Translator.Apply(mod, entries);

        Assert.Equal(1, applied);
        Assert.Equal("書名", mod.Books.First().Name?.String);
        Assert.Equal("Sword", mod.Weapons.First().Name?.String);   // empty Target must not blank it
    }

    [Fact]
    public void ApplyMatchesOnFormKeyFieldAndIndex_NotOnSourceText()
    {
        // Two records sharing the same source string must translate independently — matching on
        // text instead of identity would make one of them collateral damage.
        var mod = NewMod();
        var a = mod.Weapons.AddNew(); a.EditorID = "TT_A"; a.Name = "Blade";
        var b = mod.Weapons.AddNew(); b.EditorID = "TT_B"; b.Name = "Blade";

        var entries = Translator.Extract(mod);
        var onlyA = entries.Single(e => e.FormKey == a.FormKey.ToString());
        onlyA.Target = "刃";

        var applied = Translator.Apply(mod, entries);

        Assert.Equal(1, applied);
        Assert.Equal("刃", a.Name?.String);
        Assert.Equal("Blade", b.Name?.String);
    }

    [Fact]
    public void ExtractThenApplyRoundTripsWithoutChangingAnything()
    {
        var mod = ModWithText();
        var before = Translator.Extract(mod).Select(e => $"{e.FormKey}|{e.Field}|{e.Source}").ToList();

        Assert.Equal(0, Translator.Apply(mod, Translator.Extract(mod)));   // all Targets empty

        var after = Translator.Extract(mod).Select(e => $"{e.FormKey}|{e.Field}|{e.Source}").ToList();
        Assert.Equal(before, after);
    }

    [Fact]
    public void ApplyIgnoresEntriesThatMatchNothingInTheMod()
    {
        var mod = ModWithText();
        var stale = new List<StringEntry>
        {
            new() { FormKey = "000800:NoSuch.esp", Type = "Weapon", Field = "Name", Index = 0,
                    Source = "gone", Target = "還在" },
        };

        Assert.Equal(0, Translator.Apply(mod, stale));
    }
}
