using System.Text.Json;

internal static partial class Program
{
    private static readonly JsonSerializerOptions CatalogJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    // catalog is intentionally a small CLI facade. The reusable SQLite schema/reader lives in Core.
    private static int CatalogCmd(string[] args)
    {
        if (args.Length >= 3 && args[0] == "build")
        {
            var result = Catalog.Build(args[1], args[2..]);
            Console.WriteLine($"catalog: {result.SourceCount} source(s), {result.RecordCount} record(s) -> {Path.GetFullPath(args[1])}");
            return 0;
        }
        if (args.Length >= 3 && args[0] == "query")
        {
            string? type = null, plugin = null;
            var json = false;
            var limit = 100;
            for (var i = 3; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--type" when i + 1 < args.Length: type = args[++i]; break;
                    case "--plugin" when i + 1 < args.Length: plugin = args[++i]; break;
                    case "--limit" when i + 1 < args.Length && int.TryParse(args[i + 1], out var parsed): limit = parsed; i++; break;
                    case "--json": json = true; break;
                    default: throw new ArgumentException($"invalid catalog query option: {args[i]}");
                }
            }
            var records = Catalog.Query(args[1], args[2], type, plugin, limit);
            WriteRecords(records, json);
            return 0;
        }
        if (args.Length >= 3 && args[0] == "get")
        {
            string? plugin = null;
            var json = false;
            for (var i = 3; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--plugin" when i + 1 < args.Length: plugin = args[++i]; break;
                    case "--json": json = true; break;
                    default: throw new ArgumentException($"invalid catalog get option: {args[i]}");
                }
            }
            WriteRecords(Catalog.Get(args[1], args[2], plugin), json);
            return 0;
        }
        if (args.Length >= 2 && args[0] == "sources")
        {
            var json = args.Length == 3 && args[2] == "--json";
            if (args.Length > 2 && !json) throw new ArgumentException($"invalid catalog sources option: {args[2]}");
            var sources = Catalog.Sources(args[1]);
            if (json) Console.WriteLine(JsonSerializer.Serialize(sources, CatalogJson));
            else
            {
                Console.WriteLine("plugin\trecord_count\tlocalized\tsha256\tsource_path");
                foreach (var source in sources)
                    Console.WriteLine($"{source.Plugin}\t{source.RecordCount}\t{source.Localized}\t{source.Sha256}\t{source.SourcePath}");
                Console.WriteLine($"-- {sources.Count} source(s)");
            }
            return 0;
        }
        if (args.Length is 3 or 4 && args[0] == "export-json")
        {
            var placeableOnly = args.Length == 4 && args[3] == "--placeable";
            if (args.Length == 4 && !placeableOnly)
                throw new ArgumentException($"invalid catalog export-json option: {args[3]}");
            var result = Catalog.ExportJsonFile(args[1], args[2], placeableOnly);
            Console.WriteLine($"catalog JSON v{result.SchemaVersion}: {result.Sources.Count} source(s), " +
                $"{result.Records.Count} {(placeableOnly ? "placeable " : string.Empty)}winner record(s) " +
                $"-> {Path.GetFullPath(args[2])}");
            return 0;
        }
        throw new ArgumentException("catalog usage: catalog build <out.db> <plugin> [plugin...] | catalog query <db> <query> [--type <type>] [--plugin <plugin>] [--limit <1-1000>] [--json] | catalog get <db> <formKey> [--plugin <sourcePlugin>] [--json] | catalog sources <db> [--json] | catalog export-json <db> <out.json> [--placeable]");
    }

    private static void WriteRecords(IReadOnlyList<CatalogRecord> records, bool json)
    {
        if (json) Console.WriteLine(JsonSerializer.Serialize(records, CatalogJson));
        else
        {
            Console.WriteLine("form_key\ttype\teditor_id\tname\tsource_plugin");
            foreach (var record in records)
                Console.WriteLine($"{record.FormKey}\t{record.RecordType}\t{record.EditorId}\t{record.Name}\t{record.SourcePlugin}");
            Console.WriteLine($"-- {records.Count} match(es)");
        }
    }
}
