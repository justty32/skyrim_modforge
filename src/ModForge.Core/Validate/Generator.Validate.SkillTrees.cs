namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // Validate skillTrees at the HIGH level (before macro-expansion), so messages name the tree/node
        // fields the author wrote. The expanded records are deterministic from valid input.
        public void ValidateSkillTrees()
        {
            var treeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var st in spec.SkillTrees)
            {
                if (string.IsNullOrWhiteSpace(st.EditorId)) { Problems.Add("skillTree: missing editorId"); continue; }
                if (!treeIds.Add(st.EditorId)) Problems.Add($"skillTree '{st.EditorId}': duplicate editorId");
                if (string.IsNullOrWhiteSpace(st.Cell)) Problems.Add($"skillTree '{st.EditorId}': missing cell");
                if (st.Spacing <= 0) Problems.Add($"skillTree '{st.EditorId}': spacing must be > 0");
                if (st.Nodes.Count == 0) { Problems.Add($"skillTree '{st.EditorId}': has no nodes"); continue; }
                if (!string.IsNullOrWhiteSpace(st.PointsGlobal)) CheckRef(st.PointsGlobal, $"skillTree '{st.EditorId}' pointsGlobal");

                var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var n in st.Nodes)
                {
                    if (string.IsNullOrWhiteSpace(n.EditorId)) { Problems.Add($"skillTree '{st.EditorId}': a node is missing editorId"); continue; }
                    if (!nodeIds.Add(n.EditorId)) Problems.Add($"skillTree '{st.EditorId}' node '{n.EditorId}': duplicate node editorId");
                    if (string.IsNullOrWhiteSpace(n.Name)) Problems.Add($"skillTree '{st.EditorId}' node '{n.EditorId}': missing name");
                    if (string.IsNullOrWhiteSpace(n.Ability)) Problems.Add($"skillTree '{st.EditorId}' node '{n.EditorId}': missing ability");
                    else CheckRef(n.Ability, $"skillTree '{st.EditorId}' node '{n.EditorId}' ability");
                }
            }
        }
    }
}
