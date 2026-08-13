using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Tests;

public class MessageTests
{
    [Fact]
    public void Build_EmitsOrderedMenuButtons()
    {
        var spec = new ModSpec { PluginName = "MessageTest.esp" };
        spec.Messages.Add(new MessageSpec
        {
            EditorId = "MFManageMenu", Name = "Manage", Description = "Choose an action.",
            Buttons = { "Build", "Assign", "Cancel" },
        });

        var result = Generator.Build(spec, ModKey.FromNameAndExtension("MessageTest.esp"));
        var message = Assert.Single(result.Mod.Messages);
        Assert.Equal(new[] { "Build", "Assign", "Cancel" },
            message.MenuButtons.Select(button => button.Text?.String ?? "").ToArray());

        var path = Path.Combine(Path.GetTempPath(), $"mf-message-{Guid.NewGuid():N}.esp");
        try
        {
            PluginIo.Write(result.Mod, path);
            using var reread = SkyrimMod.CreateFromBinaryOverlay(new ModPath(path), SkyrimRelease.SkyrimSE);
            Assert.Equal(new[] { "Build", "Assign", "Cancel" }, Assert.Single(reread.Messages)
                .MenuButtons.Select(button => button.Text?.String ?? "").ToArray());
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Validate_AllowsNotificationButRejectsInvalidMenus()
    {
        var notification = new ModSpec();
        notification.Messages.Add(new MessageSpec { EditorId = "MFNotice", Description = "Done." });
        Assert.DoesNotContain(Generator.Validate(notification), problem => problem.Contains("message 'MFNotice'"));

        var invalid = new ModSpec();
        invalid.Messages.Add(new MessageSpec
        {
            EditorId = "MFBadMenu",
            Buttons = { "", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" },
        });
        var problems = Generator.Validate(invalid);
        Assert.Contains(problems, problem => problem.Contains("at most 10"));
        Assert.Contains(problems, problem => problem.Contains("button 0 is empty"));
    }
}
