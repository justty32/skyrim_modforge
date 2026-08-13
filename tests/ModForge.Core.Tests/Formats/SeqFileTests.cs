using System;
using System.IO;
using System.Linq;
using ModForge;

namespace ModForge.Tests;

// The .seq gotcha: a Start-Game-Enabled dialogue quest needs Data/Seq/<plugin>.seq or its dialogue
// is missing on EXISTING saves until a save+reload. SeqFile.Write produces that file.
public class SeqFileTests
{
    // Build a dialogue spec (→ a SGE quest), write the esp, then write its .seq and verify the bytes.
    [Fact]
    public void Write_EmitsSeq_WithStartGameEnabledQuestFormIds()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Npcs   = { new NpcSpec  { EditorId = "Npc", Name = "Npc", Greeting = "Hi." } },
            Dialogue = { new DialogueSpec { EditorId = "D", QuestEditorId = "Q", SpeakerNpcEditorId = "Npc", Prompt = "T", Responses = { "x" } } },
        };
        var mod = TestBuild.Ok(spec).Mod;

        var dir = Path.Combine(Path.GetTempPath(), $"mf-seq-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var espPath = Path.Combine(dir, "Test.esp");
            PluginIo.Write(mod, espPath);

            var written = SeqFile.Write(espPath, dir);
            Assert.NotEmpty(written);   // at least the dialogue host quest (+ the auto greet quest) are SGE

            var seqPath = Path.Combine(dir, "Seq", "Test.seq");
            Assert.True(File.Exists(seqPath), "Seq/<plugin>.seq must be written");

            var bytes = File.ReadAllBytes(seqPath);
            Assert.Equal(written.Count * 4, bytes.Length);   // flat array of 4-byte LE FormIDs, nothing else

            // Every 4-byte LE FormID's low 24 bits must match one of the SGE quest local IDs.
            var sgeLocalIds = written.Select(fk => fk.ID & 0x00FFFFFF).ToHashSet();
            for (int i = 0; i < written.Count; i++)
            {
                uint formId = BitConverter.ToUInt32(bytes, i * 4);
                Assert.Contains(formId & 0x00FFFFFF, sgeLocalIds);
            }
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // No SGE quest ⇒ no file written, empty result (don't litter a Seq folder for nothing).
    [Fact]
    public void Write_NoSgeQuest_WritesNothing()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q", StartGameEnabled = false } },
        };
        var mod = TestBuild.Ok(spec).Mod;

        var dir = Path.Combine(Path.GetTempPath(), $"mf-seq-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var espPath = Path.Combine(dir, "Test.esp");
            PluginIo.Write(mod, espPath);
            Assert.Empty(SeqFile.Write(espPath, dir));
            Assert.False(File.Exists(Path.Combine(dir, "Seq", "Test.seq")));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
