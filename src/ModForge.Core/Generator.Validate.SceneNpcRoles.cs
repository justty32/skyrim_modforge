namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // Validate npcRoles at the HIGH level (before macro-expansion), so a bad role/ref is named as
        // authored rather than surfacing as a mysterious missing generated record. Idea #24 §D.
        // Known roles are the only ones ExpandNpcRoles emits records for; an unknown role would expand
        // to nothing, so we flag it here (no silent drop, per CLAUDE.md).
        private static readonly HashSet<string> KnownRoles =
            new(System.StringComparer.OrdinalIgnoreCase) { "blacksmith" };

        public void ValidateNpcRoles()
        {
            for (int i = 0; i < spec.NpcRoles.Count; i++)
            {
                var nr = spec.NpcRoles[i];
                string who = $"npcRole[{i}]" + (string.IsNullOrWhiteSpace(nr.Npc) ? "" : $" (npc '{nr.Npc}')");
                if (string.IsNullOrWhiteSpace(nr.Npc))
                    Problems.Add($"{who}: missing npc (the base NPC ref the role attaches to)");
                if (string.IsNullOrWhiteSpace(nr.Role))
                    Problems.Add($"{who}: missing role");
                else if (!KnownRoles.Contains(nr.Role.Trim()))
                    Problems.Add($"{who}: unknown role '{nr.Role}' (supported: {string.Join(", ", KnownRoles)}) — would expand to nothing");
            }
        }
    }
}
