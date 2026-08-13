namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- pass 1: Message (MESG) — a player-facing message box / notification. No FormLinks,
        // so it's fully built in pass 1. Other records (e.g. a perk/script) can reference one by
        // editorId (resolved in pass 2 once formKeyByEd is populated). ---
        public void BuildMessages()
        {
            foreach (var msg in spec.Messages)
            {
                var r = mod.Messages.AddNew();
                r.EditorID = msg.EditorId; r.Name = msg.Name; r.Description = msg.Description;
                foreach (var text in msg.Buttons)
                    r.MenuButtons.Add(new MessageButton { Text = text });
            }
        }
    }
}
