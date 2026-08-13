namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 1: Words of Power (WOOP) + Shouts (SHOU) — scalar records + empty word rows ---
        // A shout's word rows reference WOOP + SPEL by editorId, resolved in WireShouts (pass 2),
        // so they may point forward at an in-spec word/spell or at a vanilla form.
        public void BuildShouts()
        {
            // Words of Power (WOOP): scalar-only (Name + Translation), no FormLinks — fully built here.
            // Translation is the dragon-tongue romanization shown in-game; Name defaults to it.
            foreach (var w in spec.WordsOfPower)
            {
                var r = mod.WordsOfPower.AddNew();
                r.EditorID = w.EditorId;
                var translation = string.IsNullOrEmpty(w.Translation) ? w.Name : w.Translation;
                if (!string.IsNullOrEmpty(translation)) r.Translation = translation;
                r.Name = string.IsNullOrEmpty(w.Name) ? translation : w.Name;
            }
            // Shouts (SHOU): scalar Name/Description + the word rows now (RecoveryTime set inline);
            // each row's Word/Spell FormLinks and MenuDisplayObject are wired in pass 2.
            foreach (var sh in spec.Shouts)
            {
                var r = mod.Shouts.AddNew();
                r.EditorID = sh.EditorId;
                if (!string.IsNullOrEmpty(sh.Name)) r.Name = sh.Name;
                if (!string.IsNullOrEmpty(sh.Description)) r.Description = sh.Description;
                foreach (var ws in sh.Words)
                    r.WordsOfPower.Add(new ShoutWord { RecoveryTime = ws.RecoveryTime });
            }
        }

        // --- pass 2: wire each Shout's MenuDisplayObject + per-row Word (WOOP) + Spell (SPEL) links ---
        // The rows were created in pass 1 in spec order, so index-match them to spec.Words.
        public void WireShouts()
        {
            foreach (var sh in spec.Shouts)
            {
                if (!recordsByEd.TryGetValue(sh.EditorId, out var rec) || rec is not IShout shout) continue;
                Resolve($"shout '{sh.EditorId}' menuDisplayObject", sh.MenuDisplayObject, fk => shout.MenuDisplayObject.SetTo(fk));
                int n = Math.Min(sh.Words.Count, shout.WordsOfPower.Count);
                for (int i = 0; i < n; i++)
                {
                    var ws = sh.Words[i];
                    var row = shout.WordsOfPower[i];
                    Resolve($"shout '{sh.EditorId}' word[{i}] word",  ws.Word,  fk => row.Word.SetTo(fk));
                    Resolve($"shout '{sh.EditorId}' word[{i}] spell", ws.Spell, fk => row.Spell.SetTo(fk));
                }
            }
        }
    }
}
