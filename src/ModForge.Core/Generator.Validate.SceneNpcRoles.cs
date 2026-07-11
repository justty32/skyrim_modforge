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

        // Idea #24 §E eraser — each removal must be an external "<master>:0xFORMID" ref (an existing
        // placed ref to disable); an in-spec editorId can't be removed (it's ours to just not emit).
        public void ValidateRemovals()
        {
            foreach (var r in spec.Removals)
            {
                if (string.IsNullOrWhiteSpace(r))
                    Problems.Add("removal: empty ref");
                else if (!LooksExternalRef(r) || !TryExternalRef(r, out _))
                    Problems.Add($"removal '{r}': must be a well-formed external \"<master>:0xFORMID\" ref of an existing placed ref");
            }
        }

        // Idea #24 numpad editor — overrides[] re-stamps the transform of an existing placed ref.
        // Same ref-shape rule as removals, plus: a ref in BOTH lists is a contradiction (move it or
        // remove it, not both — build lets the removal win, but say so here instead of silently).
        public void ValidateOverrides()
        {
            var removed = new HashSet<string>(spec.Removals, System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < spec.Overrides.Count; i++)
            {
                var o = spec.Overrides[i];
                string who = $"override[{i}]" + (string.IsNullOrWhiteSpace(o.Ref) ? "" : $" ('{o.Ref}')");
                if (string.IsNullOrWhiteSpace(o.Ref))
                    Problems.Add($"{who}: empty ref");
                else if (!LooksExternalRef(o.Ref) || !TryExternalRef(o.Ref, out _))
                    Problems.Add($"{who}: must be a well-formed external \"<master>:0xFORMID\" ref of an existing placed ref");
                else if (removed.Contains(o.Ref))
                    Problems.Add($"{who}: also listed in removals[] — contradictory (the removal wins); drop one");
            }
        }
    }
}
