namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        public void ValidateVoice()
        {
            var templateIds = new HashSet<string>(spec.VoiceTemplates.Select(t => t.Id), StringComparer.OrdinalIgnoreCase);
            
            foreach (var t in spec.VoiceTemplates)
            {
                if (string.IsNullOrWhiteSpace(t.Id))
                    Problems.Add("voiceTemplate: empty id");
                
                var engine = (t.Engine ?? "").ToLowerInvariant();
                if (engine is not ("f5" or "chatterbox" or "gptsovits" or "xtts"))
                    Problems.Add($"voiceTemplate '{t.Id}': unknown engine '{t.Engine}' (use f5 | chatterbox | gptsovits | xtts)");
            }

            foreach (var n in spec.Npcs)
            {
                if (!string.IsNullOrWhiteSpace(n.VoiceTemplate))
                {
                    if (!templateIds.Contains(n.VoiceTemplate))
                        Problems.Add($"npc '{n.EditorId}' voiceTemplate '{n.VoiceTemplate}' not found in voiceTemplates");
                }
            }

            if (spec.VoiceLine is { } vl)
            {
                var fmt = (vl.Format ?? "").ToLowerInvariant();
                if (fmt is not ("fuz" or "wav" or "xwm"))
                    Problems.Add($"voiceLine: unknown format '{vl.Format}' (use fuz | wav | xwm)");
            }
        }
    }
}
