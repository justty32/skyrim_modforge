namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // BOS _SWAP.ini loose-file specs. Structural-only (no esp record); BOS resolves the base/swap
        // form refs against the player's load order at runtime — not verifiable offline.
        public void ValidateObjectSwaps()
        {
            foreach (var s in spec.ObjectSwaps)
            {
                var who = $"objectSwap '{s.File}'";
                if (string.IsNullOrWhiteSpace(s.File))
                    Problems.Add("objectSwap has empty 'file' name");
                if (s.Groups.Count == 0)
                    Problems.Add($"{who} has no groups");
                foreach (var g in s.Groups)
                    foreach (var e in g.Entries)
                    {
                        if (string.IsNullOrWhiteSpace(e.Base))
                            Problems.Add($"{who} has a swap entry with empty 'base' (the form to replace)");
                        if (e.Swaps.Count == 0)
                            Problems.Add($"{who} swap entry '{e.Base}' has no 'swaps' (the replacement form)");
                        if (e.Chance is double ch && (ch < 0 || ch > 100))
                            Problems.Add($"{who} swap entry '{e.Base}' chance {ch} out of range (0–100)");
                    }
            }
        }
    }
}
