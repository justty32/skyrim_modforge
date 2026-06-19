namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // AOS _ANIO.ini loose-file specs. Structural-only (no esp record); AOS resolves the ANIO /
        // condition refs against the player's load order at runtime — not verifiable offline.
        public void ValidateAnimObjectSwaps()
        {
            foreach (var s in spec.AnimObjectSwaps)
            {
                var who = $"animObjectSwap '{s.File}'";
                if (string.IsNullOrWhiteSpace(s.File))
                    Problems.Add("animObjectSwap has empty 'file' name");
                foreach (var e in s.Entries)
                {
                    if (string.IsNullOrWhiteSpace(e.Base))
                        Problems.Add($"{who} has an entry with empty 'base' (the ANIO to swap)");
                    if (e.Swaps.Count == 0)
                        Problems.Add($"{who} entry '{e.Base}' has no 'swaps' (the replacement ANIO)");
                }
            }
        }
    }
}
