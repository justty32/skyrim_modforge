using ModForge;

internal static partial class Program
{
    internal static int CheckDependenciesCmd(string[] args)
    {
        if (args.Length is not (3 or 5) || args[1] != "--plugins"
            || (args.Length == 5 && args[3] != "--implicit-plugins"))
            throw new ArgumentException(
                "usage: check-dependencies <plugin> --plugins <plugins.txt> [--implicit-plugins <implicit.txt>]");

        var pluginPath = args[0];
        var plugins = PluginListing.ReadPluginsFile(args[2]);
        var implicitPlugins = args.Length == 5 ? PluginListing.ReadImplicitFile(args[4]) : null;
        var result = InstalledDependencies.Check(
            InstalledDependencies.ReadDirectMasters(pluginPath), plugins, implicitPlugins);

        foreach (var dependency in result.Dependencies)
        {
            var label = dependency.State switch
            {
                InstalledDependencyState.Enabled => "enabled",
                InstalledDependencyState.Implicit => "enabled (implicit list)",
                InstalledDependencyState.Disabled => "DISABLED",
                _ => "MISSING",
            };
            var output = dependency.State is InstalledDependencyState.Disabled or InstalledDependencyState.Missing
                ? Console.Error : Console.Out;
            output.WriteLine($"{label}: {dependency.Plugin}");
        }

        Console.WriteLine($"checked {result.Dependencies.Count} direct master(s): "
            + (result.Satisfied ? "all enabled by the supplied lists" : "dependency problems found"));

        if (!result.Satisfied && implicitPlugins is null)
            Console.Error.WriteLine("hint: pass --implicit-plugins with masters omitted from plugins.txt");
        return result.Satisfied ? 0 : 1;
    }
}
