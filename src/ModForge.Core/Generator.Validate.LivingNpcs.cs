namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // Validate the livingNpcs section at the HIGH level (before macro-expansion), so messages name
        // the author's fields. The expanded records (alias/markers/global/rumor + controller script) are
        // deterministic from valid input.
        public void ValidateLivingNpcs()
        {
            if (spec.LivingNpcs is not { } sec) return;
            if (sec.Npcs.Count == 0) { Problems.Add("livingNpcs: section present but has no npcs"); return; }
            if (sec.SimIntervalHours <= 0) Problems.Add("livingNpcs: simIntervalHours must be > 0");
            if (sec.PollInterval <= 0) Problems.Add("livingNpcs: pollInterval must be > 0");
            if (!string.IsNullOrWhiteSpace(sec.RumorSpeaker) && !sec.RumorSpeaker.Contains(':') && !npcIds.Contains(sec.RumorSpeaker))
                Problems.Add($"livingNpcs: rumorSpeaker '{sec.RumorSpeaker}' must be an in-spec npc editorId or a '<master>:0xFORMID' ref");

            var seenRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var known = new[] { "adventurer", "mageapprentice", "merchant", "herbalist", "priest", "bandit" };
            foreach (var ln in sec.Npcs)
            {
                if (string.IsNullOrWhiteSpace(ln.Ref)) { Problems.Add("livingNpcs: an npc is missing ref"); continue; }
                var who = $"livingNpcs npc '{ln.Ref}'";
                if (!seenRefs.Add(ln.Ref)) Problems.Add($"{who}: duplicate ref (each living NPC must be unique)");

                bool external = ln.Ref.Contains(':');
                if (external) CheckRef(ln.Ref, $"{who} ref");
                else if (!npcIds.Contains(ln.Ref))
                    Problems.Add($"{who}: an in-spec living NPC must be an npcs[] editorId; '{ln.Ref}' is not a known npc (or use a '<master>.esp:0xFORMID' ref for an external follower)");

                if (!known.Contains((ln.Archetype ?? "").Trim().ToLowerInvariant()))
                    Problems.Add($"{who}: unknown archetype '{ln.Archetype}' (adventurer|mageApprentice|merchant|herbalist|priest|bandit)");

                if (ln.Anchors.Count == 0)
                    Problems.Add($"{who}: has no anchors — it can never materialise (give at least one cell+position where the player can meet it)");
                for (int j = 0; j < ln.Anchors.Count; j++)
                    if (string.IsNullOrWhiteSpace(ln.Anchors[j].Cell))
                        Problems.Add($"{who}: anchor[{j}] is missing cell");

                if (ln.Rumors.Count > 0 && string.IsNullOrWhiteSpace(sec.RumorSpeaker))
                    Problems.Add($"{who}: has rumors but the section has no rumorSpeaker — the 傳唱 won't surface");

                if (!new[] { "friendly", "neutral", "hostile" }.Contains((ln.Alignment ?? "").Trim().ToLowerInvariant()))
                    Problems.Add($"{who}: unknown alignment '{ln.Alignment}' (friendly|neutral|hostile)");
                foreach (var k in ln.Interactions)
                    if (!new[] { "fund", "praise", "parley" }.Contains((k ?? "").Trim().ToLowerInvariant()))
                        Problems.Add($"{who}: unknown interaction '{k}' (fund|praise|parley)");
                if (external && ln.Alignment.Trim().Equals("hostile", StringComparison.OrdinalIgnoreCase))
                    Problems.Add($"{who}: alignment hostile on an external ref can't set aggression (the macro only adjusts in-spec NPCs); the follower keeps its own AI");
            }
        }
    }
}
