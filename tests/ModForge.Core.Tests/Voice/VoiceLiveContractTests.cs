using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Sdk;

namespace ModForge.Core.Tests;

public class VoiceLiveContractTests
{
    [Fact]
    public void GenerateWav_ProductionVoicegen_HonorsProcessContract()
    {
        var voicegenRoot = FindVoicegenRoot();
        if (voicegenRoot is null)
            throw SkipException.ForSkip("sibling skyrim-voicegen checkout is unavailable");

        var python = FindPython();
        if (python is null)
            throw SkipException.ForSkip("Python runtime for the skyrim-voicegen live contract is unavailable");

        var tempRoot = Path.Combine(Path.GetTempPath(), $"modforge voice live contract {Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            var fishLog = Path.Combine(tempRoot, "fish arguments with spaces.jsonl");
            var wrappers = CreateWrappers(tempRoot, python, voicegenRoot, fishLog);
            var specDir = Path.Combine(tempRoot, "spec assets with spaces");
            Directory.CreateDirectory(specDir);

            var template = new VoiceTemplateSpec
            {
                Id = "live-contract",
                Engine = "fish-s2",
                ReferenceWav = Path.Combine("reference clips", "speaker voice.wav"),
                ReferenceText = "Reference transcript with spaces.",
                ModelPath = Path.Combine("models", "fish model"),
                RvcModel = Path.Combine("rvc models", "voice timbre.pth"),
                Seed = 31415,
                Speed = 0.75f,
                Exaggeration = 1.5f,
                Language = "zh-TW",
            };
            var options = new VoiceOptions { TtsBin = wrappers.SelectedVoicegen };

            var wav = Voice.GenerateWav(
                "A live contract line with spaces.", template, specDir, options,
                emotion: "Puzzled", intensity: 67);

            Assert.NotNull(wav);
            Assert.True(wav!.Length > 44);
            Assert.Equal("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
            Assert.Equal("WAVE", Encoding.ASCII.GetString(wav, 8, 4));

            using (var call = JsonDocument.Parse(File.ReadLines(fishLog).Single()))
            {
                var args = call.RootElement;
                Assert.Equal("A live contract line with spaces.", args.GetProperty("text").GetString());
                Assert.Equal(Path.Combine(specDir, template.ReferenceWav), args.GetProperty("ref_audio").GetString());
                Assert.Equal(template.ReferenceText, args.GetProperty("ref_text").GetString());
                Assert.Equal(Path.Combine(specDir, template.ModelPath), args.GetProperty("model").GetString());
                Assert.Equal(Path.Combine(specDir, template.RvcModel), args.GetProperty("rvc").GetString());
                Assert.Equal("31415", args.GetProperty("seed").GetString());
                Assert.Equal("0.75", args.GetProperty("speed").GetString());
                Assert.Equal("1.5", args.GetProperty("exaggeration").GetString());
                Assert.Equal("zh-TW", args.GetProperty("language").GetString());
                Assert.Equal("Puzzled", args.GetProperty("emotion").GetString());
                Assert.Equal("67", args.GetProperty("intensity").GetString());
            }

            foreach (var failure in new[] { "__nonzero__", "__missing__", "__header_only__", "__truncated__" })
                Assert.Null(Voice.GenerateWav(failure, template, specDir, options));

            var calls = File.ReadLines(fishLog)
                .Select(line => JsonDocument.Parse(line))
                .ToList();
            try
            {
                Assert.Equal(5, calls.Count);
                foreach (var call in calls)
                {
                    var staged = call.RootElement.GetProperty("out").GetString()!;
                    Assert.False(File.Exists(staged), $"voicegen staging file leaked: {staged}");
                    var destination = VoicegenDestination(staged);
                    Assert.False(File.Exists(destination), $"ModForge temporary output leaked: {destination}");
                }
            }
            finally
            {
                foreach (var call in calls) call.Dispose();
            }

            Assert.True(File.Exists(wrappers.WindowsVoicegen));
            Assert.True(File.Exists(wrappers.PosixVoicegen));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static string? FindVoicegenRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                foreach (var candidate in new[]
                {
                    Path.Combine(dir.FullName, "skyrim-voicegen"),
                    Path.Combine(dir.FullName, "projects", "skyrim-voicegen"),
                })
                {
                    if (File.Exists(Path.Combine(candidate, "voicegen.py")) &&
                        File.Exists(Path.Combine(candidate, "tests", "fake_fish_engine.py")))
                        return candidate;
                }
            }
        }
        return null;
    }

    private static string? FindPython()
    {
        var configured = Environment.GetEnvironmentVariable("MODFORGE_VOICE_CONTRACT_PYTHON");
        var candidates = new[] { configured, OperatingSystem.IsWindows() ? "python" : "python3", "python" };
        foreach (var candidate in candidates.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = candidate!,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                });
                process?.WaitForExit();
                if (process?.ExitCode == 0) return candidate;
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
            {
            }
        }
        return null;
    }

    private static ContractWrappers CreateWrappers(string directory, string python, string voicegenRoot, string log)
    {
        var fake = Path.Combine(voicegenRoot, "tests", "fake_fish_engine.py");
        var voicegen = Path.Combine(voicegenRoot, "voicegen.py");
        var windowsFish = Path.Combine(directory, "fake fish engine.cmd");
        var windowsVoicegen = Path.Combine(directory, "production voicegen wrapper.cmd");
        var posixFish = Path.Combine(directory, "fake fish engine.sh");
        var posixVoicegen = Path.Combine(directory, "production voicegen wrapper.sh");

        File.WriteAllText(windowsFish, $"@echo off\r\n{CmdQuote(python)} {CmdQuote(fake)} %*\r\n");
        File.WriteAllText(windowsVoicegen,
            $"@echo off\r\nset \"MODFORGE_FISH_SPEECH_BIN={windowsFish}\"\r\n" +
            $"set \"MODFORGE_FAKE_FISH_LOG={log}\"\r\n{CmdQuote(python)} {CmdQuote(voicegen)} %*\r\n");
        File.WriteAllText(posixFish, $"#!/bin/sh\nexec {ShellQuote(python)} {ShellQuote(fake)} \"$@\"\n");
        File.WriteAllText(posixVoicegen,
            $"#!/bin/sh\nexport MODFORGE_FISH_SPEECH_BIN={ShellQuote(posixFish)}\n" +
            $"export MODFORGE_FAKE_FISH_LOG={ShellQuote(log)}\n" +
            $"exec {ShellQuote(python)} {ShellQuote(voicegen)} \"$@\"\n");

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(posixFish, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.SetUnixFileMode(posixVoicegen, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return new ContractWrappers(
            OperatingSystem.IsWindows() ? windowsVoicegen : posixVoicegen,
            windowsVoicegen,
            posixVoicegen);
    }

    private static string VoicegenDestination(string staged)
    {
        var match = Regex.Match(Path.GetFileName(staged), @"^\.(modforge_voice_[^.]+\.wav)\.[^.]+\.tmp\.wav$");
        Assert.True(match.Success, $"unexpected voicegen staging path: {staged}");
        return Path.Combine(Path.GetDirectoryName(staged)!, match.Groups[1].Value);
    }

    private static string CmdQuote(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    private static string ShellQuote(string value) => $"'{value.Replace("'", "'\"'\"'")}'";

    private sealed record ContractWrappers(string SelectedVoicegen, string WindowsVoicegen, string PosixVoicegen);
}
