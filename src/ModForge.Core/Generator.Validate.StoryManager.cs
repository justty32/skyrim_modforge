namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // Validate quest storyEvents: known event, well-formed alias fills, valid
        // event-data slots / forced refs. BuildStoryManager silently drops bad input,
        // so this is the only place the user learns the spec was malformed.
        public void ValidateStoryManager()
        {
            foreach (var q in spec.Quests)
            {
                if (q.StoryEvent is not { } se) continue;
                var where = $"quest '{q.EditorId}' storyEvent";

                if (!StoryManagerEvents.TryGet(se.Event, out var def))
                {
                    Problems.Add($"{where} event '{se.Event}' is unknown (supported: {string.Join(", ", StoryManagerEvents.Names)})");
                    continue;
                }

                if (se.Conditions != null)
                    foreach (var cs in se.Conditions)
                        CheckCondition(cs, $"{where} condition");

                foreach (var a in q.Aliases)
                {
                    if (!StoryManagerEvents.TryParseFill(a.Fill, out var kind, out var arg))
                    {
                        Problems.Add($"{where} alias '{a.Name}' fill '{a.Fill}' is malformed (expect 'fromEvent:<slot>' or 'forced:<ref>')");
                        continue;
                    }
                    if (kind.Equals("fromEvent", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!def.Slots.ContainsKey(arg))
                            Problems.Add($"{where} alias '{a.Name}' fromEvent slot '{arg}' invalid for {se.Event} (slots: {string.Join(", ", def.Slots.Keys)})");
                    }
                    else if (kind.Equals("forced", StringComparison.OrdinalIgnoreCase))
                    {
                        CheckRef(arg, $"{where} alias '{a.Name}' forced ref");
                    }
                    else
                    {
                        Problems.Add($"{where} alias '{a.Name}' fill kind '{kind}' unsupported (use fromEvent | forced)");
                    }
                }

                // A storyEvent quest is force-cleared of StartGameEnabled at build time. Warn if the
                // user explicitly left it true. NOTE: this message must NOT contain "storyEvent" or
                // "fill" — StartGameEnabled defaults true, so a well-formed storyEvent quest trips
                // this, and the valid-case test asserts no problem mentions "storyEvent"/"fill".
                if (q.StartGameEnabled)
                    Problems.Add($"quest '{q.EditorId}': startGameEnabled=true is ignored for a story-event quest (auto-cleared)");
            }
        }
    }
}
