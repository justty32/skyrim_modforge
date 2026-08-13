using System.Text;
using ModForge;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;

namespace ModForge.Tests;

// Translator.ApplyLocalized — the localized-plugin path (UTF-8 .STRINGS for Simplified-Chinese SSE).
// Separate from TranslatorTests because these touch disk and, unavoidably, process-global state.
//
// ⚠️ ApplyLocalized SETS TranslatedString.DefaultLanguage = Chinese AND NEVER RESTORES IT. That is
// harmless in production (the CLI is one-shot: one process, one command) but it would leak into every
// later test in this assembly, so this class restores it. Do not "simplify" that away.
public class TranslatorLocalizedTests : IDisposable
{
    private readonly Language _language = TranslatedString.DefaultLanguage;
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "mf_loc_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        TranslatedString.DefaultLanguage = _language;
        try { Directory.Delete(_dir, recursive: true); } catch { /* temp dir; best effort */ }
    }

    private const string ChineseTitle = "書名";

    // A book puts its Name in .STRINGS and its BookText in .DLSTRINGS, so one record exercises both
    // string files — which is what makes the untranslated-slot behaviour below visible.
    private static SkyrimMod ModWithText()
    {
        var mod = new SkyrimMod(ModKey.FromNameAndExtension("TranslateTest.esp"), SkyrimRelease.SkyrimSE);
        var book = mod.Books.AddNew();
        book.EditorID = "TT_Book";
        book.Name = "A Title";
        book.BookText = "Body prose.";
        return mod;
    }

    private static List<StringEntry> TitleTranslated(SkyrimMod mod)
    {
        var entries = Translator.Extract(mod);
        foreach (var e in entries)
            if (e.Source == "A Title") e.Target = ChineseTitle;
        return entries;   // BookText deliberately left untranslated
    }

    private string Strings(string name) => Path.Combine(_dir, "Strings", name);

    [Fact]
    public void WritesThePluginAndItsStringsAlongside()
    {
        var (_, _, esp) = Translator.ApplyLocalized(ModWithText(), new List<StringEntry>(), _dir);

        Assert.Equal(Path.Combine(_dir, "TranslateTest.esp"), esp);
        Assert.True(File.Exists(esp));
        Assert.True(Directory.Exists(Path.Combine(_dir, "Strings")));
    }

    [Fact]
    public void RenamesTheLanguageSuffixToLowercase()
    {
        var mod = ModWithText();

        var (_, renamed, _) = Translator.ApplyLocalized(mod, TitleTranslated(mod), _dir);

        // Mutagen writes "_Chinese"; Skyrim wants "_chinese". This is not cosmetic — it decides
        // whether the game finds the file at all on a case-sensitive filesystem (Linux/Proton).
        Assert.Equal(3, renamed);   // .STRINGS + .ILSTRINGS + .DLSTRINGS
        foreach (var ext in new[] { "STRINGS", "ILSTRINGS", "DLSTRINGS" })
            Assert.True(File.Exists(Strings($"TranslateTest_chinese.{ext}")), $"missing _chinese.{ext}");
        // Compared ordinally on the real name: a glob would be case-INSENSITIVE on Windows and
        // happily match the very files we just renamed, asserting nothing.
        Assert.DoesNotContain(
            Directory.GetFiles(Path.Combine(_dir, "Strings")).Select(Path.GetFileName),
            n => n!.Contains("_Chinese.", StringComparison.Ordinal));
    }

    [Fact]
    public void AppliedCountsOnlyTheEntriesThatMatchedASlot()
    {
        var mod = ModWithText();
        var entries = TitleTranslated(mod);
        entries.Add(new StringEntry
        {
            FormKey = "000800:NoSuch.esp", Type = "Weapon", Field = "Name", Index = 0,
            Source = "gone", Target = "還在",
        });

        var (applied, _, _) = Translator.ApplyLocalized(mod, entries, _dir);

        Assert.Equal(1, applied);
    }

    [Fact]
    public void TranslatedTextIsWrittenAsUtf8_NotGbk()
    {
        var mod = ModWithText();

        Translator.ApplyLocalized(mod, TitleTranslated(mod), _dir);

        // The whole point of this code path: Simplified-Chinese SSE reads UTF-8 .STRINGS, not GBK.
        // Asserting on the actual bytes is the only way to catch a regression to a legacy encoder —
        // and finding the UTF-8 sequence is itself the proof, since a GBK encoding of the same text
        // is a completely different byte sequence.
        var bytes = File.ReadAllBytes(Strings("TranslateTest_chinese.STRINGS"));
        Assert.Contains(Encoding.UTF8.GetBytes(ChineseTitle), bytes);
    }

    [Fact]
    public void UntranslatedSlotsAreAbsentFromTheChineseSet_SoTheyRenderEmptyInGame()
    {
        var mod = ModWithText();

        Translator.ApplyLocalized(mod, TitleTranslated(mod), _dir);

        // THIS IS THE TRAP, pinned deliberately. A slot with no translation does NOT fall back to
        // its source text — it stays in the _English set, and a Chinese install never reads that
        // file. So a PARTIALLY translated plugin shows blank text in game for whatever was missed,
        // rather than the original English. Ship a full translation, or decide to backfill the
        // source text into untranslated targets before calling this.
        var chineseBody = File.ReadAllBytes(Strings("TranslateTest_chinese.DLSTRINGS"));
        Assert.DoesNotContain(Encoding.UTF8.GetBytes("Body prose."), chineseBody);

        var englishBody = File.ReadAllBytes(Strings("TranslateTest_English.DLSTRINGS"));
        Assert.Contains(Encoding.UTF8.GetBytes("Body prose."), englishBody);
    }

    [Fact]
    public void LeavesTheProcessWideDefaultLanguageSetToChinese()
    {
        // Pinned so the next reader knows it is deliberate rather than discovering it as a flaky
        // test somewhere else. Safe only because the CLI runs one command per process.
        Assert.Equal(Language.English, TranslatedString.DefaultLanguage);

        Translator.ApplyLocalized(ModWithText(), new List<StringEntry>(), _dir);

        Assert.Equal(Language.Chinese, TranslatedString.DefaultLanguage);
    }
}
