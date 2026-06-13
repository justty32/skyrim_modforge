using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ModForge.Core.Tests;

public class VoiceTests
{
    private static int FlagIndex(List<string> args, string flag)
    {
        var i = args.IndexOf(flag);
        Assert.True(i >= 0, $"expected flag {flag} in: {string.Join(" ", args)}");
        return i;
    }

    private static string FlagValue(List<string> args, string flag) => args[FlagIndex(args, flag) + 1];

    [Fact]
    public void BuildTtsArgs_MinimalTemplate_OmitsOptionalFlags()
    {
        var t = new VoiceTemplateSpec { Id = "v" }; // engine=f5, language="en" defaults
        var args = Voice.BuildTtsArgs("hello", t, "/spec", "/tmp/out.wav");

        Assert.Equal("f5", FlagValue(args, "--engine"));
        Assert.Equal("hello", FlagValue(args, "--text"));
        Assert.Equal("/tmp/out.wav", FlagValue(args, "--out"));

        // Language has a non-empty default ("en") so it is always forwarded.
        Assert.Equal("en", FlagValue(args, "--language"));

        // Unset optionals must not appear at all (engine defaults stay in charge).
        Assert.DoesNotContain("--speed", args);
        Assert.DoesNotContain("--exaggeration", args);
        Assert.DoesNotContain("--seed", args);
        Assert.DoesNotContain("--ref-wav", args);
        Assert.DoesNotContain("--ref-text", args);
        Assert.DoesNotContain("--model", args);
        Assert.DoesNotContain("--rvc", args);
        // Emotion is sourced from the INFO record, not the template; omitted when not passed.
        Assert.DoesNotContain("--emotion", args);
        Assert.DoesNotContain("--intensity", args);
    }

    [Fact]
    public void BuildTtsArgs_PassesEmotionAndIntensity_WhenProvided()
    {
        var t = new VoiceTemplateSpec { Id = "v" };
        // Emotion/intensity come from the dialogue INFO response, passed alongside the template.
        var args = Voice.BuildTtsArgs("line", t, "/spec", "out.wav", emotion: "Anger", intensity: 75);

        Assert.Equal("Anger", FlagValue(args, "--emotion"));
        Assert.Equal("75", FlagValue(args, "--intensity"));
    }

    [Fact]
    public void BuildTtsArgs_PassesSpeedExaggerationLanguage_WhenSet()
    {
        var t = new VoiceTemplateSpec
        {
            Id = "v",
            Speed = 0.8f,
            Exaggeration = 1.25f,
            Language = "ja",
            Seed = 42,
        };
        var args = Voice.BuildTtsArgs("line", t, "/spec", "out.wav");

        // Invariant-culture formatting (never "0,8").
        Assert.Equal("0.8", FlagValue(args, "--speed"));
        Assert.Equal("1.25", FlagValue(args, "--exaggeration"));
        Assert.Equal("ja", FlagValue(args, "--language"));
        Assert.Equal("42", FlagValue(args, "--seed"));
    }

    [Fact]
    public void BuildTtsArgs_ForFishS2_UsesSameWrapperContract()
    {
        var t = new VoiceTemplateSpec
        {
            Id = "v",
            Engine = "fish-s2",
            ReferenceWav = "refs/serana.wav",
            ReferenceText = "Keep your eyes open.",
            ModelPath = "models/fish-s2-pro",
            Seed = 7,
            Language = "en",
        };
        var args = Voice.BuildTtsArgs("I have your back.", t, "/spec", "out.wav");

        Assert.Equal("fish-s2", FlagValue(args, "--engine"));
        Assert.Equal("I have your back.", FlagValue(args, "--text"));
        Assert.Equal("out.wav", FlagValue(args, "--out"));
        Assert.Equal(Path.Combine("/spec", "refs/serana.wav"), FlagValue(args, "--ref-wav"));
        Assert.Equal("Keep your eyes open.", FlagValue(args, "--ref-text"));
        Assert.Equal(Path.Combine("/spec", "models/fish-s2-pro"), FlagValue(args, "--model"));
        Assert.Equal("7", FlagValue(args, "--seed"));
        Assert.Equal("en", FlagValue(args, "--language"));
    }

    [Fact]
    public void BuildTtsArgs_EmptyLanguage_OmitsLanguageFlag()
    {
        var t = new VoiceTemplateSpec { Id = "v", Language = "" };
        var args = Voice.BuildTtsArgs("line", t, "/spec", "out.wav");

        Assert.DoesNotContain("--language", args);
    }

    [Fact]
    public void BuildTtsArgs_ResolvesPathsAgainstSpecDir()
    {
        var t = new VoiceTemplateSpec
        {
            Id = "v",
            ReferenceWav = "clips/ref.wav",
            ReferenceText = "the reference transcript",
            ModelPath = "models/ft",
            RvcModel = "rvc/timbre.pth",
        };
        var args = Voice.BuildTtsArgs("line", t, "/spec", "out.wav");

        Assert.Equal(Path.Combine("/spec", "clips/ref.wav"), FlagValue(args, "--ref-wav"));
        Assert.Equal(Path.Combine("/spec", "models/ft"), FlagValue(args, "--model"));
        Assert.Equal(Path.Combine("/spec", "rvc/timbre.pth"), FlagValue(args, "--rvc"));
        Assert.Equal("the reference transcript", FlagValue(args, "--ref-text"));
    }

    [Fact]
    public void BuildLipGenArgs_ProducesOfficialLipGeneratorCommandLine()
    {
        // CK LipGenerator.exe signature: <exe> <wav> <text> -Language:<lang> -OutputFileName:<lip>
        var args = Voice.BuildLipGenArgs(
            "/tools/LipGenerator.exe",
            @"Z:\tmp\in.wav",
            "I have nothing for you to buy.",
            @"Z:\tmp\out.lip",
            "USEnglish");

        Assert.Equal("/tools/LipGenerator.exe", args[0]);   // exe first (wine FileName="wine" adds it)
        Assert.Equal(@"Z:\tmp\in.wav", args[1]);            // wav is positional arg #1
        Assert.Equal("I have nothing for you to buy.", args[2]); // transcript is positional arg #2
        Assert.Contains("-Language:USEnglish", args);
        Assert.Contains(@"-OutputFileName:Z:\tmp\out.lip", args);
    }

    [Fact]
    public void BuildLipGenArgs_ForwardsNonDefaultLanguage()
    {
        var args = Voice.BuildLipGenArgs("e.exe", "w.wav", "bonjour", "o.lip", "French");
        Assert.Contains("-Language:French", args);
    }

    [Fact]
    public void VoiceFileName_GeneratesCorrectFormat()
    {
        // Simple case
        Assert.Equal("MyQuest_MyTopic_00012345_1.fuz",
            Generator.VoiceFileName("MyQuest", "MyTopic", 0x12345, 1));

        // Truncation case
        // QuestID: 10 chars, TopicID: 15 chars
        Assert.Equal("DialogueWh_WhiterunGreetin_00012345_2.wav",
            Generator.VoiceFileName("DialogueWhiterun", "WhiterunGreeting", 0x12345, 2, "wav"));

        // Long Quest ID
        Assert.Equal("1234567890_Topic_00000001_1.fuz",
            Generator.VoiceFileName("1234567890123", "Topic", 1, 1));

        // Long Topic ID
        Assert.Equal("Quest_123456789012345_00000001_1.fuz",
            Generator.VoiceFileName("Quest", "1234567890123456789", 1, 1));
    }

    [Fact]
    public void VoiceTypeFolderName_MapsCommonVanillaRefsAndSafeFolderIds()
    {
        Assert.Equal("MaleNord", Generator.VoiceTypeFolderName("Skyrim.esm:0x013AE6"));
        Assert.Equal("FemaleEvenToned", Generator.VoiceTypeFolderName("Skyrim.esm:0x013ADD"));
        Assert.Equal("CustomVoice", Generator.VoiceTypeFolderName("CustomVoice"));
        Assert.Null(Generator.VoiceTypeFolderName("Other.esm:0x013AE6"));
        Assert.Null(Generator.VoiceTypeFolderName("../BadVoice"));
    }

    // End-to-end through the real CK LipGenerator.exe under Wine. Self-skips unless both
    //   MODFORGE_LIPGEN    = path to LipGenerator.exe
    //   MODFORGE_LIP_TESTWAV = path to a real-speech WAV (a tone produces no lip)
    // are set, so offline/CI runs (and machines without the CK) pass trivially.
    [Fact, Trait("Category", "RequiresSkyrim")]
    public void GenerateLip_OfficialLipGenerator_ProducesValidLip()
    {
        var lipGen = Environment.GetEnvironmentVariable("MODFORGE_LIPGEN");
        var testWav = Environment.GetEnvironmentVariable("MODFORGE_LIP_TESTWAV");
        if (string.IsNullOrEmpty(lipGen) || !File.Exists(lipGen)) return;
        if (string.IsNullOrEmpty(testWav) || !File.Exists(testWav)) return;

        var wav = File.ReadAllBytes(testWav);
        var lip = Voice.GenerateLip(wav, "I have nothing for you to buy.", new VoiceOptions { LipGenExe = lipGen });

        Assert.NotNull(lip);
        Assert.True(lip!.Length > 12, "lip should carry phoneme/animation data, not just a header");
        // Skyrim .lip version header is 0x00000001 (little-endian).
        Assert.Equal(1u, BitConverter.ToUInt32(lip, 0));
    }

    [Fact]
    public void WriteFuz_CreatesValidHeader()
    {
        byte[] audio = [0xAA, 0xBB, 0xCC];
        byte[] lip = [0x11, 0x22];

        byte[] fuz = Generator.WriteFuz(audio, lip);

        // 12 (header) + 2 (lip) + 3 (audio) = 17 bytes
        Assert.Equal(17, fuz.Length);

        // Magic "FUZE"
        Assert.Equal("FUZE", Encoding.ASCII.GetString(fuz, 0, 4));

        // Version 1 (little-endian)
        Assert.Equal(1u, BitConverter.ToUInt32(fuz, 4));

        // Lip size 2
        Assert.Equal(2u, BitConverter.ToUInt32(fuz, 8));

        // Lip data
        Assert.Equal(0x11, fuz[12]);
        Assert.Equal(0x22, fuz[13]);

        // Audio data
        Assert.Equal(0xAA, fuz[14]);
        Assert.Equal(0xBB, fuz[15]);
        Assert.Equal(0xCC, fuz[16]);
    }

    [Fact]
    public void WriteFuz_HandlesNoLip()
    {
        byte[] audio = [0xDE, 0xAD, 0xBE, 0xEF];

        byte[] fuz = Generator.WriteFuz(audio, null);

        // 12 (header) + 4 (audio) = 16 bytes
        Assert.Equal(16, fuz.Length);

        // Lip size 0
        Assert.Equal(0u, BitConverter.ToUInt32(fuz, 8));

        // Audio starts immediately after header
        Assert.Equal(0xDE, fuz[12]);
        Assert.Equal(0xEF, fuz[15]);
    }
}
