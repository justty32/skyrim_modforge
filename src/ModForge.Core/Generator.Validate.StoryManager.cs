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

                foreach (var cs in se.Conditions)
                    CheckCondition(cs, $"{where} condition");

                // ScriptEvent is the custom entry: it MUST name a keyword (declared in spec.keywords)
                // so the branch can filter to it. Other events don't use a keyword.
                if (se.Event.Equals("ScriptEvent", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(se.Keyword))
                        Problems.Add($"{where} event 'ScriptEvent' requires a 'keyword' (a KYWD editorId declared in spec.keywords)");
                    else if (!spec.Keywords.Any(k => string.Equals(k.EditorId, se.Keyword, StringComparison.OrdinalIgnoreCase)))
                        Problems.Add($"{where} keyword '{se.Keyword}' is not declared in spec.keywords");
                }

                foreach (var a in q.Aliases)
                {
                    if (!StoryManagerEvents.TryParseFill(a.Fill, out var kind, out var arg))
                    {
                        Problems.Add($"{where} alias '{a.Name}' fill '{a.Fill}' is malformed (expect 'fromEvent:<slot>', 'forced:<ref>', 'uniqueActor:<ref>', 'createObject:<ref>@<targetAlias>' or 'findMatching:closest|any')");
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
                    else if (kind.Equals("uniqueActor", StringComparison.OrdinalIgnoreCase))
                    {
                        CheckRef(arg, $"{where} alias '{a.Name}' uniqueActor ref");
                    }
                    else if (kind.Equals("createObject", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!StoryManagerEvents.TryParseCreateObject(arg, out var objRef, out var tgt))
                            Problems.Add($"{where} alias '{a.Name}' createObject fill '{a.Fill}' must be 'createObject:<ref>@<targetAlias>'");
                        else
                        {
                            CheckRef(objRef, $"{where} alias '{a.Name}' createObject object ref");
                            if (string.Equals(tgt, a.Name, StringComparison.OrdinalIgnoreCase))
                                Problems.Add($"{where} alias '{a.Name}' createObject cannot target itself ('@{tgt}')");
                            else if (!q.Aliases.Any(x => string.Equals(x.Name, tgt, StringComparison.OrdinalIgnoreCase)))
                                Problems.Add($"{where} alias '{a.Name}' createObject target alias '{tgt}' is not another alias in this quest");
                        }
                    }
                    else if (kind.Equals("findMatching", StringComparison.OrdinalIgnoreCase))
                    {
                        // arg = "closest" (nearest match) or "any" (first match) in the loaded area.
                        if (!arg.Equals("closest", StringComparison.OrdinalIgnoreCase)
                            && !arg.Equals("any", StringComparison.OrdinalIgnoreCase))
                            Problems.Add($"{where} alias '{a.Name}' findMatching mode '{arg}' invalid (use 'closest' or 'any')");
                        if (a.Conditions.Count == 0)
                            Problems.Add($"{where} alias '{a.Name}' findMatching needs at least one 'conditions' entry to match a ref (else it matches nothing useful)");
                        foreach (var cs in a.Conditions)
                            CheckCondition(cs, $"{where} alias '{a.Name}' findMatching condition");
                    }
                    else
                    {
                        Problems.Add($"{where} alias '{a.Name}' fill kind '{kind}' unsupported (use fromEvent | forced | uniqueActor | createObject | findMatching)");
                    }
                }
                // Note: StartGameEnabled is force-cleared for storyEvent quests at build time (it
                // defaults true). No warning is emitted — ModForge's Validate has no warning/error
                // distinction, so a warning here would mark every valid SM spec INVALID.
            }
        }
    }
}
