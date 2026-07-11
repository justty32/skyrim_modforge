namespace ModForge;

public static partial class Generator
{
    // Run every pass-0 macro-expansion in dependency order. Each is idempotent (guarded on the spec),
    // so this is safe to call more than once — e.g. PackageCmd calls it BEFORE compiling dialogue/quest
    // fragments (so macro-GENERATED fragments, like an npcRole's openBarter trade topic, are compiled),
    // then Build() calls it again as a no-op. Keeping the ordered list in one place avoids drift.
    public static void ExpandMacros(ModSpec spec)
    {
        ExpandSkillTrees(spec);   // → globals/activators/placements/scripts
        ExpandSettlements(spec);  // → npcs' packages/factions + ACHR placements + vendor FACT/container + RELA
        ExpandLivingNpcs(spec);   // → controller quest + per-NPC alias/markers/global/rumor + world-controller script
        ExpandNpcRoles(spec);     // → host quest + greeting + package + (vendor: FACT/chest + openBarter topic)
        ExpandCapturedItems(spec); // → WEAP/ARMO(+minted ENCH) / ALCH / INGR from the in-game definition eyedropper
        ExpandCapturedNpcs(spec); // → NpcSpec (identity + face/body recipe) + ACHR placement from the actor eyedropper
    }

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

    // blacksmith: conditioned greeting + sandbox behaviour + (if a shop location is known) vendor.
    // Two attach modes by whether `npc` is IN-SPEC (a fresh NpcSpec — e.g. a PROTEUS-clone stand-in, the
    // only kind that can actually be PLACED and appear) or EXTERNAL (a vanilla/other-master base, patched
    // via override). NOTE: a vanilla UNIQUE NPC can't be duplicated by a placement (the engine keeps one
    // instance), so to see the smith standing in the scene the spec should use an in-spec NPC.
    private static void ExpandBlacksmith(ModSpec spec, SceneNpcRoleSpec nr, string safe, System.Action ensureHost)
    {
        ensureHost();

        // Greeting (Hello, shared): GetIsID(npc) — in-spec resolves via npcsByEd, external via the
        // speaker-gate TryResolveRef fallback. (Slice text; #17 AI pipeline can rewrite from backstory.)
        spec.Dialogue.Add(new DialogueSpec
        {
            EditorId = "MFRole_" + safe + "_Hello",
            QuestEditorId = RoleHostQuestEd,
            SpeakerNpcEditorId = nr.Npc,
            Hello = true,
            Responses = new System.Collections.Generic.List<string> { "Need something forged? You've come to the right anvil." },
            Emotion = "Neutral",
        });

        // Behaviour: sandbox with no location ⇒ LocationFallback (sandbox where the actor is placed).
        string pkgEd = "MFRole_" + safe + "_Sandbox";
        spec.Packages.Add(new PackageSpec
        {
            EditorId = pkgEd,
            Template = SandboxTemplateRef,   // Skyrim.esm:0x01C254 (shared const on Generator.Settlements)
            Sandbox = new SandboxSpec { Radius = 256 },
        });

        // Vendor service (a blacksmith trades) needs a shop LOCATION — reuse the companion placement that
        // puts this NPC in the world (base == npc, kind:npc). Co-locate a merchant chest there so
        // VendorLocation resolves to that cell. No companion placement ⇒ skip vendor (greeting+package stay).
        var inSpec = spec.Npcs.FirstOrDefault(n => string.Equals(n.EditorId, nr.Npc, System.StringComparison.OrdinalIgnoreCase));
        var place = spec.Placements.FirstOrDefault(pl => RefsMatch(pl.Base, nr.Npc));
        string? vendorFac = null;
        if (place is not null)
        {
            string chestEd = "MFRole_" + safe + "_Chest";
            string chestRef = chestEd + "Ref";
            vendorFac = "MFRole_" + safe + "_Merchant";
            // Stock = the vanilla blacksmith merchant leveled lists (what a real forge chest holds) so the
            // barter shows actual wares + a vendor's gold pool — NOT a flat gold pile (which shows nothing
            // to buy). Each leveled-list entry rolls to level-appropriate stock at runtime.
            spec.Containers.Add(new ContainerSpec
            {
                EditorId = chestEd, Name = "Blacksmith Merchant Chest",
                Items = new System.Collections.Generic.List<ContainerEntrySpec>
                {
                    new() { Item = BlacksmithVendorGold, Count = 1 },   // VendorGoldBlacksmith → the shop's gold
                    new() { Item = BlacksmithWeapons,    Count = 1 },   // LItemBlacksmithWeapon100
                    new() { Item = BlacksmithArmor,      Count = 1 },   // LItemBlacksmithArmor100
                    new() { Item = BlacksmithMisc,       Count = 1 },   // LItemBlacksmithSpecialLoot100 (ingots/misc)
                },
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
            // Explicit trade topic (reliable, IN-GAME confirmed for openBarter). Relying on vanilla's
            // generic services dialogue (GetOffersServicesNow-gated) does NOT reliably surface for a
            // custom NPC; a "Let me see your wares." topic that calls ShowBarterMenu() on the speaker
            // opens the barter with this NPC's vendor-faction stock directly. (Emits a TIF_ fragment .pex.)
            spec.Dialogue.Add(new DialogueSpec
            {
                EditorId = "MFRole_" + safe + "_Trade",
                QuestEditorId = RoleHostQuestEd,
                SpeakerNpcEditorId = nr.Npc,
                Prompt = "Let me see your wares.",
                Responses = new System.Collections.Generic.List<string> { "Have a look." },
                OpenBarter = true,
            });
        }

        if (inSpec is not null)
        {
            // IN-SPEC NPC: attach package + faction directly (BuildNpcs auto-adds JobMerchantFaction for
            // an in-spec vendor FACT, so the vanilla "I'd like to trade" surfaces).
            if (!inSpec.Packages.Contains(pkgEd, System.StringComparer.OrdinalIgnoreCase)) inSpec.Packages.Add(pkgEd);
            if (vendorFac is not null && !inSpec.Factions.Contains(vendorFac, System.StringComparer.OrdinalIgnoreCase))
                inSpec.Factions.Add(vendorFac);
        }
        else
        {
            // EXTERNAL NPC: override + append package; add vendor + JobMerchant faction (BuildNpcs'
            // auto-JobMerchant runs for in-spec NPCs only, so add it explicitly here).
            var patch = new NpcPatchSpec
            {
                OverrideOf = nr.Npc,
                Packages = new System.Collections.Generic.List<string> { pkgEd },
                Mode = "append",
            };
            if (vendorFac is not null)
            {
                patch.Factions.Add(vendorFac);
                patch.Factions.Add(JobMerchantFactionRef);
            }
            spec.NpcPatches.Add(patch);
        }
    }

    private const string BlacksmithVendorList = "Skyrim.esm:0x066333"; // VendorItemsBlacksmith (buy/sell categories)
    private const string BlacksmithVendorGold = "Skyrim.esm:0x072AE9"; // VendorGoldBlacksmith (leveled → shop gold)
    private const string BlacksmithWeapons = "Skyrim.esm:0x0173D2";    // LItemBlacksmithWeapon100
    private const string BlacksmithArmor = "Skyrim.esm:0x0173D1";      // LItemBlacksmithArmor100
    private const string BlacksmithMisc = "Skyrim.esm:0x017363";       // LItemBlacksmithSpecialLoot100

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
