namespace ModForge;

public static partial class Generator
{
    // --- NPC role macro-expansion (Idea #24 §D — in-game scene export) -------------------------
    // A scene-captured NPC gets a job ROLE; this macro EXPANDS it into the low-level records the
    // validated build passes already handle, so every battle-tested pass (dialogue Hello, NpcPatch
    // packages) does the real work. Called once at the top of Build() (pass 0), after ExpandLivingNpcs.
    // Idempotent (guarded on the spec).
    //
    // Unlike ExpandSettlements (which attaches to in-spec NpcSpec objects), the target here is an
    // EXTERNAL base NPC ref (a PROTEUS clone / follower base = "<plugin>.esp:0xFORMID"), so the macro
    // uses the two vehicles that work on an external NPC:
    //   * a Hello DialogueSpec whose speaker gate is GetIsID(npc) — the conditioned greeting
    //   * an NpcPatch (overrideOf npc, append) adding a sandbox package — the NPC's behaviour
    // (editor-location sandbox = the actor sandboxes wherever a companion placements[] entry puts it).
    //
    // Vendor service is intentionally NOT expanded yet: NpcPatch swaps packages only, so joining a
    // vendor FACT needs a separate faction-add capability (design doc §D). Roles beyond "blacksmith"
    // are warned and skipped (no silent drop, per CLAUDE.md).
    private const string RoleHostQuestEd = "MF_SceneNpcRolesQ"; // one shared StartGameEnabled host for all role greetings

    public static void ExpandNpcRoles(ModSpec spec)
    {
        if (spec.NpcRolesExpanded) return;
        spec.NpcRolesExpanded = true;
        if (spec.NpcRoles.Count == 0) return;

        bool hostAdded = false;
        void EnsureHostQuest()
        {
            if (hostAdded) return;
            hostAdded = true;
            // A greeting is hosted by a quest; StartGameEnabled so it runs on load (dialogue surfaces).
            if (!spec.Quests.Any(q => string.Equals(q.EditorId, RoleHostQuestEd, System.StringComparison.OrdinalIgnoreCase)))
                spec.Quests.Add(new QuestSpec { EditorId = RoleHostQuestEd, Name = "", StartGameEnabled = true });
        }

        int i = 0;
        foreach (var nr in spec.NpcRoles)
        {
            i++;
            if (string.IsNullOrWhiteSpace(nr.Npc)) continue; // validation reports the missing ref
            string role = (nr.Role ?? "").Trim().ToLowerInvariant();
            string safe = SanitizeEd(nr.Npc) + "_" + i; // unique-ish suffix for generated editorIds

            switch (role)
            {
                case "blacksmith":
                    ExpandBlacksmith(spec, nr, safe, EnsureHostQuest);
                    break;
                default:
                    // Unknown role: emit nothing (expansion stays pure); Validate surfaces the warning
                    // so it isn't a silent drop (CLAUDE.md no-silent-caps).
                    break;
            }
        }
    }

    // blacksmith: conditioned greeting + a sandbox-where-you-stand package attached via NpcPatch.
    private static void ExpandBlacksmith(ModSpec spec, SceneNpcRoleSpec nr, string safe, System.Action ensureHost)
    {
        ensureHost();

        // Greeting (Hello): GetIsID(npc) gates it to this NPC (slice text; a later pass can hand the
        // backstory to the #17 AI dialogue pipeline for bespoke lines).
        spec.Dialogue.Add(new DialogueSpec
        {
            EditorId = "MFRole_" + safe + "_Hello",
            QuestEditorId = RoleHostQuestEd,
            SpeakerNpcEditorId = nr.Npc,   // external ref resolved via the speaker-gate fallback
            Hello = true,
            Responses = new System.Collections.Generic.List<string> { "Need something forged? You've come to the right anvil." },
            Emotion = "Neutral",
        });

        // Behaviour: sandbox with no location ⇒ LocationFallback (sandbox around the actor's placed
        // spot). Attached by overriding the external base NPC and appending our package to its schedule.
        string pkgEd = "MFRole_" + safe + "_Sandbox";
        spec.Packages.Add(new PackageSpec
        {
            EditorId = pkgEd,
            Template = SandboxTemplateRef,   // Skyrim.esm:0x01C254 (shared const on Generator.Settlements)
            Sandbox = new SandboxSpec { Radius = 256 },
        });
        var patch = new NpcPatchSpec
        {
            OverrideOf = nr.Npc,
            Packages = new System.Collections.Generic.List<string> { pkgEd },
            Mode = "append",   // keep the NPC's own packages; add ours as a low-priority fallback
        };
        spec.NpcPatches.Add(patch);

        // Vendor service: a blacksmith trades. Needs a shop LOCATION — reuse the companion placement
        // that puts this NPC in the world (base == npc, kind:npc). Co-locate a merchant chest there;
        // VendorLocation resolves to that cell so trade opens near the smith. Membership in the vendor
        // FACT + JobMerchantFaction surfaces the VANILLA "I'd like to trade" (no dialogue authoring).
        // No companion placement ⇒ no shop location ⇒ skip vendor (greeting + package still apply).
        var place = spec.Placements.FirstOrDefault(pl => RefsMatch(pl.Base, nr.Npc));
        if (place is not null)
        {
            string chestEd = "MFRole_" + safe + "_Chest";
            string chestRef = chestEd + "Ref";
            string vendorFac = "MFRole_" + safe + "_Merchant";
            spec.Containers.Add(new ContainerSpec
            {
                EditorId = chestEd, Name = "Blacksmith Merchant Chest",
                Items = new System.Collections.Generic.List<ContainerEntrySpec> { new() { Item = GoldRef, Count = BlacksmithGold } },
            });
            spec.Placements.Add(new PlacementSpec
            {
                Base = chestEd, EditorId = chestRef,
                Cell = place.Cell, Worldspace = place.Worldspace, Position = place.Position, Persistent = true,
            });
            spec.Factions.Add(new FactionSpec
            {
                EditorId = vendorFac, Name = vendorFac,
                Vendor = new VendorSpec
                {
                    StartHour = 8, EndHour = 20,
                    SellBuyList = BlacksmithVendorList,   // VendorItemsBlacksmith
                    MerchantContainer = chestRef,
                },
            });
            patch.Factions.Add(vendorFac);
            patch.Factions.Add(JobMerchantFactionRef);   // vanilla — gates the "I'd like to trade" topic
        }
    }

    private const int BlacksmithGold = 500;
    private const string BlacksmithVendorList = "Skyrim.esm:0x066333"; // VendorItemsBlacksmith

    // Two refs point at the same base: exact match, or case-insensitive "<plugin>:0xID" with the FormID
    // hex compared numerically (0x00013B99 == 0x13B99). Good enough for matching a companion placement.
    private static bool RefsMatch(string a, string b)
    {
        if (string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase)) return true;
        static (string plugin, ulong id)? Parse(string s)
        {
            int c = s.LastIndexOf(':');
            if (c < 0) return null;
            var hex = s[(c + 1)..].Trim();
            if (hex.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
            return ulong.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var id)
                ? (s[..c].Trim(), id) : null;
        }
        var pa = Parse(a); var pb = Parse(b);
        return pa is { } x && pb is { } y
            && string.Equals(x.plugin, y.plugin, System.StringComparison.OrdinalIgnoreCase) && x.id == y.id;
    }

    // Turn a ref (in-spec editorId or "<plugin>.esp:0xFORMID") into a safe editorId fragment.
    private static string SanitizeEd(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s)
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        return sb.ToString();
    }
}
