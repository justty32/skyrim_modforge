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

                // #5 locationFilter: each entry is a LocType keyword ref (in-spec KYWD or vanilla).
                foreach (var kw in se.LocationFilter)
                    if (string.IsNullOrWhiteSpace(kw)) Problems.Add($"{where} locationFilter has an empty keyword");
                    else CheckRef(kw, $"{where} locationFilter keyword");

                // #6 cooldownHours: non-negative game hours.
                if (se.CooldownHours < 0f)
                    Problems.Add($"{where} cooldownHours {se.CooldownHours} must be >= 0");

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
                    ValidateQuestAlias(q, a, def, se.Event, where);
                // Note: StartGameEnabled is force-cleared for storyEvent quests at build time (it
                // defaults true). No warning is emitted — ModForge's Validate has no warning/error
                // distinction, so a warning here would mark every valid SM spec INVALID.
            }

            // Ordinary (non-storyEvent) quests can also carry aliases (forced/uniqueActor/createObject/
            // findMatching + alias scripts), built by BuildStandaloneQuestAliases. Validate those too —
            // def is null here, so a fromEvent fill (which needs an event) is flagged.
            foreach (var q in spec.Quests)
            {
                if (q.StoryEvent is not null || q.Aliases.Count == 0) continue;
                foreach (var a in q.Aliases)
                    ValidateQuestAlias(q, a, null, "", $"quest '{q.EditorId}' alias");
            }
        }

        private void ValidateQuestAlias(QuestSpec q, QuestAliasSpec a, StoryEventDef? def, string eventName, string where)
        {
            if (!StoryManagerEvents.TryParseFill(a.Fill, out var kind, out var arg))
            {
                Problems.Add($"{where} alias '{a.Name}' fill '{a.Fill}' is malformed (expect 'fromEvent:<slot>', 'forced:<ref>', 'uniqueActor:<ref>', 'createObject:<ref>@<targetAlias>', 'findMatching:closest|any', 'findMatchingLocation:<locType>[@<parentAlias>]' or 'findInLocationAlias:<locationAlias>[#<refTypeLCRT>]')");
                return;
            }
            if (kind.Equals("fromEvent", StringComparison.OrdinalIgnoreCase))
            {
                if (def is not { } d)
                    Problems.Add($"{where} alias '{a.Name}' fill 'fromEvent:{arg}' requires a 'storyEvent' block (there is no event to pull a ref from) — use forced/uniqueActor/createObject/findMatching on a non-storyEvent quest");
                else if (!d.Slots.ContainsKey(arg))
                    Problems.Add($"{where} alias '{a.Name}' fromEvent slot '{arg}' invalid for {eventName} (slots: {string.Join(", ", d.Slots.Keys)})");
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
            else if (kind.Equals("findMatchingLocation", StringComparison.OrdinalIgnoreCase))
            {
                // #7: arg = "<locTypeKeyword>[@<parentLocationAlias>]". Keyword must resolve; parent
                // alias (if given) must be another alias in this quest.
                var locKw = arg; string? parent = null;
                int at = arg.LastIndexOf('@');
                if (at > 0 && at < arg.Length - 1) { locKw = arg[..at]; parent = arg[(at + 1)..]; }
                if (string.IsNullOrWhiteSpace(locKw))
                    Problems.Add($"{where} alias '{a.Name}' findMatchingLocation needs a LocType keyword (got '{arg}')");
                else
                    CheckRef(locKw, $"{where} alias '{a.Name}' findMatchingLocation LocType keyword");
                if (parent is not null)
                {
                    if (string.Equals(parent, a.Name, StringComparison.OrdinalIgnoreCase))
                        Problems.Add($"{where} alias '{a.Name}' findMatchingLocation cannot search within itself ('@{parent}')");
                    else if (!q.Aliases.Any(x => string.Equals(x.Name, parent, StringComparison.OrdinalIgnoreCase)))
                        Problems.Add($"{where} alias '{a.Name}' findMatchingLocation parent alias '{parent}' is not another alias in this quest");
                }
                foreach (var cs in a.Conditions)
                    CheckCondition(cs, $"{where} alias '{a.Name}' findMatchingLocation condition");
            }
            else if (kind.Equals("findInLocationAlias", StringComparison.OrdinalIgnoreCase))
            {
                // #8: arg = "<locationAlias>[#<refTypeLCRT>]". The location alias must be another alias in
                // this quest; refType (if given) must resolve; conditions and/or refType pick the ref.
                var locAlias = arg; string? refType = null;
                int hash = arg.IndexOf('#');
                if (hash > 0 && hash < arg.Length - 1) { locAlias = arg[..hash]; refType = arg[(hash + 1)..]; }
                if (string.Equals(locAlias, a.Name, StringComparison.OrdinalIgnoreCase))
                    Problems.Add($"{where} alias '{a.Name}' findInLocationAlias cannot search within itself ('{locAlias}')");
                else if (!q.Aliases.Any(x => string.Equals(x.Name, locAlias, StringComparison.OrdinalIgnoreCase)))
                    Problems.Add($"{where} alias '{a.Name}' findInLocationAlias location alias '{locAlias}' is not another alias in this quest");
                if (refType is not null)
                    CheckRef(refType, $"{where} alias '{a.Name}' findInLocationAlias refType (LCRT)");
                if (refType is null && a.Conditions.Count == 0)
                    Problems.Add($"{where} alias '{a.Name}' findInLocationAlias needs a refType (#<LCRT>) and/or 'conditions' to pick a ref (else it matches nothing useful)");
                foreach (var cs in a.Conditions)
                    CheckCondition(cs, $"{where} alias '{a.Name}' findInLocationAlias condition");
            }
            else
            {
                Problems.Add($"{where} alias '{a.Name}' fill kind '{kind}' unsupported (use fromEvent | forced | uniqueActor | createObject | findMatching | findMatchingLocation | findInLocationAlias)");
            }

            // Optional alias script (alias[].script): its object-properties must resolve. The
            // .psc source itself is checked when `package` compiles it.
            foreach (var pp in a.ScriptProperties)
                if (string.Equals(pp.Type, "object", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(pp.ObjectEditorId))
                    CheckRef(pp.ObjectEditorId, $"{where} alias '{a.Name}' script property '{pp.Name}'");
        }
    }
}
