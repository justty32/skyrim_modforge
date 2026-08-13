using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class VoiceAnnotateTests
{
    // Build an in-memory plugin with one voiced INFO carrying an emotion, resolve it back from a clip
    // filename, and confirm the manifest entry reads the emotion/intensity/text. Offline — no master/BSA.
    [Fact]
    public void Annotation_entry_reads_emotion_intensity_and_text_from_the_info()
    {
        var mod = new SkyrimMod(ModKey.FromNameAndExtension("Test.esp"), SkyrimRelease.SkyrimSE);
        var topic = new DialogTopic(mod);
        mod.DialogTopics.Add(topic);
        var info = new DialogResponses(mod);
        info.Responses.Add(new DialogResponse { Text = "You'll regret this.", ResponseNumber = 1, Emotion = Emotion.Anger, EmotionValue = 80 });
        topic.Responses.Add(info);
        var cache = mod.ToImmutableLinkCache();

        string fileName = $"MQ_GREET_{info.FormKey.ID:X8}_1.fuz";
        Assert.True(VoiceAnnotate.TryParseInfoFormKey(fileName, mod.ModHeader.MasterReferences.Select(m => m.Master).ToList(), mod.ModKey, out var fk));
        Assert.Equal(info.FormKey, fk);

        Assert.True(cache.TryResolve<IDialogResponsesGetter>(fk, out var resolved));
        var entry = VoiceAnnotate.BuildEntry("MaleNord/clip.wav", "MaleNord", resolved!, 0);
        Assert.Equal("Anger", entry.Emotion);
        Assert.Equal(80, entry.Intensity);
        Assert.Equal("You'll regret this.", entry.Text);
        Assert.Equal($"0x{info.FormKey.ID:X8}", entry.InfoFormId);
    }

    [Fact]
    public void Bad_filename_returns_false()
    {
        Assert.False(VoiceAnnotate.TryParseInfoFormKey("not_a_voice_file.fuz",
            new System.Collections.Generic.List<ModKey>(), ModKey.FromNameAndExtension("Test.esp"), out _));
    }
}
