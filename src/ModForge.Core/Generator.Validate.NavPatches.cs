namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        public void ValidateNavPatches()
        {
            for (int i = 0; i < spec.NavPatches.Count; i++)
            {
                var np = spec.NavPatches[i];
                string who = $"navPatch[{i}]";

                if (!LooksExternalRef(np.Cell))
                    Problems.Add($"{who}: cell must be a vanilla interior <master>:0xFORMID ref");
                if (!LooksExternalRef(np.Navmesh))
                    Problems.Add($"{who}: navmesh must be a vanilla <master>:0xFORMID ref");
                if (!string.Equals(np.LinkTo, "auto", StringComparison.OrdinalIgnoreCase))
                    Problems.Add($"{who}: linkTo supports only 'auto' in the P3 MVP");
                if (!float.IsFinite(np.Epsilon) || np.Epsilon <= 0f || np.Epsilon > 64f)
                    Problems.Add($"{who}: epsilon must be finite, > 0 and <= 64 (got {np.Epsilon})");

                if (!NavmeshPatch.TryValidatePolygon(np.Polygon, out var error))
                    Problems.Add($"{who}: {error}");

                CheckRef(np.Cell, $"{who} cell");
                CheckRef(np.Navmesh, $"{who} navmesh");
            }
        }
    }
}
