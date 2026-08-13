namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // references[] (Idea #24 referrer) — NAME an existing placed ref so the rest of the spec can
        // point at it by `label`. See Spec.References.cs for the two target classes (in-file vs external)
        // and the persistent trap the `anchor` modes exist to escape.
        private static readonly HashSet<string> KnownAnchors =
            new(System.StringComparer.OrdinalIgnoreCase) { "none", "marker", "replace" };

        // Register every label as a resolvable id BEFORE the domain validators run, so a package/alias/
        // linkedRef that points at "sofia's chair" passes CheckRef. Mirrors RegisterIdentityFactions.
        public void RegisterReferenceLabels()
        {
            foreach (var r in spec.References)
            {
                var label = (r.Label ?? "").Trim();
                if (string.IsNullOrWhiteSpace(label)) continue;
                if (!Ids.Add(label))
                    Problems.Add($"reference label '{label}' collides with an existing editorId (or another label) — a label is a name refs resolve by, so it must be unique across the spec");
            }
        }

        public void ValidateReferences()
        {
            for (int i = 0; i < spec.References.Count; i++)
            {
                var r = spec.References[i];
                var refStr = (r.Ref ?? "").Trim();
                var label = (r.Label ?? "").Trim();
                string who = $"reference[{i}]" + (label.Length == 0 ? "" : $" ('{label}')");

                if (string.IsNullOrWhiteSpace(label))
                    Problems.Add($"{who}: empty label (the label IS the name other refs point at)");
                else if (LooksExternalRef(label))
                    Problems.Add($"{who}: label '{label}' looks like a \"<master>:0xFORMID\" ref — a ref field would resolve it as an external form, never as this label; pick a plain name");

                bool external = LooksExternalRef(refStr);
                if (string.IsNullOrWhiteSpace(refStr))
                    Problems.Add($"{who}: empty ref (a reference NAMES an existing placed ref — either a placements[] editorId in this spec, or a vanilla \"<master>:0xFORMID\")");
                else if (external)
                { if (!TryExternalRef(refStr, out _)) Problems.Add($"{who}: malformed external ref '{refStr}' (expect <master>:0xFORMID)"); }
                else if (!placementIds.Contains(refStr))
                    Problems.Add($"{who}: ref '{refStr}' is not a placements[] editorId in this spec (and is not a \"<master>:0xFORMID\") — references[] points at an EXISTING ref, it never creates one");

                var anchor = (r.Anchor ?? "").Trim();
                bool hasAnchor = anchor.Length > 0 && !anchor.Equals("none", System.StringComparison.OrdinalIgnoreCase);
                if (anchor.Length > 0 && !KnownAnchors.Contains(anchor))
                    Problems.Add($"{who}: unknown anchor '{anchor}' (none | marker | replace)");
                else if (hasAnchor && !external && !string.IsNullOrWhiteSpace(refStr))
                    Problems.Add($"{who}: anchor '{anchor}' is meaningless on an in-file ref ('{refStr}' is a placement of ours — build already forces it persistent); drop the anchor");
                else if (hasAnchor)
                {
                    // A persistent anchor is a REAL placed record, so it needs somewhere to stand.
                    if (string.IsNullOrWhiteSpace(r.Cell) && string.IsNullOrWhiteSpace(r.Worldspace))
                        Problems.Add($"{who}: anchor '{anchor}' needs a cell or worldspace (the anchor is a placed ref — it must live somewhere)");
                    if (anchor.Equals("replace", System.StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(r.Base))
                        Problems.Add($"{who}: anchor \"replace\" needs a `base` (the form our persistent copy re-places; the in-game referrer records it)");
                }

                if (!string.IsNullOrWhiteSpace(r.Cell) && !string.IsNullOrWhiteSpace(r.Worldspace))
                    Problems.Add($"{who}: has BOTH cell and worldspace (a location is one or the other)");
                CheckRef(r.Base, $"{who} base");
                CheckRef(r.Cell, $"{who} cell");
                CheckRef(r.Worldspace, $"{who} worldspace");
                if (r.Scale is float sc && sc <= 0f)
                    Problems.Add($"{who}: scale must be > 0 (got {sc})");
            }
        }
    }
}
