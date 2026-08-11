internal static partial class Program
{
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
            var limit = 100;
            for (var i = 3; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--type" when i + 1 < args.Length: type = args[++i]; break;
                    case "--plugin" when i + 1 < args.Length: plugin = args[++i]; break;
                    case "--limit" when i + 1 < args.Length && int.TryParse(args[i + 1], out var parsed): limit = parsed; i++; break;
                    default: throw new ArgumentException($"invalid catalog query option: {args[i]}");
                }
            }
            var records = Catalog.Query(args[1], args[2], type, plugin, limit);
            Console.WriteLine("form_key\ttype\teditor_id\tname\tsource_plugin");
            foreach (var record in records)
                Console.WriteLine($"{record.FormKey}\t{record.RecordType}\t{record.EditorId}\t{record.Name}\t{record.SourcePlugin}");
            Console.WriteLine($"-- {records.Count} match(es)");
            return 0;
        }
        throw new ArgumentException("catalog usage: catalog build <out.db> <plugin> [plugin...] | catalog query <db> <query> [--type <type>] [--plugin <plugin>] [--limit <1-1000>]");
    }
}
