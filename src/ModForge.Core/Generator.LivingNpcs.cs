namespace ModForge;

public static partial class Generator
{
    // --- Living-NPC population macro-expansion (Idea #23) --------------------------------------
    // Sugar that EXPANDS into the low-level records the validated build passes already handle, PLUS
    // a top-level script attach for the reusable MFLivingWorldController / MFLivingNpcAlias .pex
    // (shipped by the package ship-gate). Called at the top of Build() after ExpandSettlements.
    // Idempotent (guarded on the spec).
    //
    // For the section: one StartGameEnabled host controller quest (type None) carrying the world
    // controller script (SimIntervalHours / PollInterval / AliasCount) + one shared off-stage hold
    // marker + one shared sandbox package (so a materialised NPC actually behaves).
    // For each living NPC: a reference alias on the host quest (forced to an in-spec placed ACHR, or
    // uniqueActor for an external follower's ref) carrying MFLivingNpcAlias (Archetype/HoldMarker/
    // Anchors/DeedCount); one xmarker per anchor + an Anchors FormList; a deed GlobalVariable; and —
    // when the section has a rumorSpeaker and the NPC has rumors — a 傳唱 topic gated on the deed global.
    // In-spec NPCs also get the shared sandbox package appended (external refs keep their own AI).
    //
    // Core prerequisites (landed 2026-06-27): alias-script object props resolve deferred (HoldMarker/
    // Anchors point at placements built later), and forced-alias ACHRs auto-persist (the MoveTo'd ref
    // survives save/load). See sub_projs/living-adventurers/design.md §6.
    public const string LivingControllerScript = "MFLivingWorldController";
    public const string LivingAliasScript = "MFLivingNpcAlias";
    private const string LivingHoldMarkerEd = "MFLiving_HoldMarker";
    private const string LivingSandboxPackageEd = "MFLiving_SandboxHere";
    private const string LivingCtrlQuestEd = "MFLiving_Ctrl";

    private static int LivingArchetypeCode(string a) => (a ?? "").Trim().ToLowerInvariant() switch
    {
        "mageapprentice" => 1,
        "merchant" => 2,
        "herbalist" => 3,
        "priest" => 4,
        "bandit" => 5,
        _ => 0, // adventurer
    };

    // An external/vanilla ref (uniqueActor fill) vs an in-spec npc editorId (placed + forced). In-spec
    // editorIds never contain ':'; a "<master>.es[pm]:0xFORMID" ref does.
    private static bool LivingIsExternalRef(string r) => r.Contains(':');

    // P3 interaction copy: (prompt, response, favorDelta, gatedOnDeed). prompt == null → unknown kind.
    private static (string?, string, float, bool) LivingInteraction(string kind) => (kind ?? "").Trim().ToLowerInvariant() switch
    {
        "fund"   => ("Here's some coin for your next venture.", "Appreciated — I'll put it to good use.", 1f, false),
        "praise" => ("Your deeds are the talk of the taverns.", "Ha! Music to my ears.", 1f, true),
        "parley" => ("Lower your weapon. Let's talk.",          "...Fine. Talk, then.",              5f, false),
        _        => (null, "", 0f, false),
    };

    private static readonly string[] LivingInteractionKinds = { "fund", "praise", "parley" };

    public static void ExpandLivingNpcs(ModSpec spec)
    {
        if (spec.LivingNpcsExpanded) return;
        spec.LivingNpcsExpanded = true;
        if (spec.LivingNpcs is not { } sec || sec.Npcs.Count == 0) return;

        var npcByEd = new Dictionary<string, NpcSpec>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in spec.Npcs)
            if (!string.IsNullOrWhiteSpace(n.EditorId)) npcByEd[n.EditorId] = n;

        // --- shared records (once per section) ---
        // Off-stage holding marker: a buried Tamriel xmarker (auto-persistent). Living NPCs sit here,
        // frozen/unprocessed, while the player is elsewhere.
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = LivingHoldMarkerEd, Kind = "xmarker",
            Worldspace = "Skyrim.esm:0x00003C", Position = new Vec3 { X = 0, Y = -9000, Z = -5000 },
        });
        // Shared "sandbox where I currently am" package (NearSelf): after MoveTo, EvaluatePackage makes
        // an in-spec NPC sandbox the room instead of standing.
        spec.Packages.Add(new PackageSpec
        {
            EditorId = LivingSandboxPackageEd, Template = SandboxTemplateRef,
            Sandbox = new SandboxSpec { Radius = 512 },
        });

        // --- host controller quest ---
        var ctrl = new QuestSpec
        {
            EditorId = LivingCtrlQuestEd, Name = "Living World", Type = "None",
            StartGameEnabled = true, Priority = 50,
        };

        for (int i = 0; i < sec.Npcs.Count; i++)
        {
            var ln = sec.Npcs[i];
            if (string.IsNullOrWhiteSpace(ln.Ref)) continue; // validation reports it
            bool external = LivingIsExternalRef(ln.Ref);
            string tag = external ? $"N{i}" : ln.Ref;        // stable per-NPC record prefix
            string prefix = $"MFLiving_{tag}";

            // deed counter (off-stage progress; rumor gates on it)
            string deedEd = $"{prefix}_Deeds";
            spec.Globals.Add(new GlobalSpec { EditorId = deedEd, Type = "long", Value = 0 });

            // anchors → one xmarker each + an Anchors FormList
            var anchorEds = new List<string>();
            for (int j = 0; j < ln.Anchors.Count; j++)
            {
                var an = ln.Anchors[j];
                string anEd = $"{prefix}_A{j}";
                spec.Placements.Add(new PlacementSpec
                {
                    EditorId = anEd, Kind = "xmarker", Cell = an.Cell,
                    Position = new Vec3 { X = an.Position.X, Y = an.Position.Y, Z = an.Position.Z },
                });
                anchorEds.Add(anEd);
            }
            string anchorsFlstEd = $"{prefix}_Anchors";
            spec.FormLists.Add(new FormListSpec { EditorId = anchorsFlstEd, Items = anchorEds });

            // the alias fill: place + force an in-spec NPC's ACHR, or uniqueActor an external follower ref
            string fill;
            if (external)
            {
                fill = $"uniqueActor:{ln.Ref}";
            }
            else
            {
                string refEd = $"{prefix}Ref";
                spec.Placements.Add(new PlacementSpec
                {
                    EditorId = refEd, Base = ln.Ref, Kind = "npc",
                    Worldspace = "Skyrim.esm:0x00003C", Position = new Vec3 { X = 0, Y = -9000, Z = -5000 },
                });
                fill = $"forced:{refEd}";
                if (npcByEd.TryGetValue(ln.Ref, out var npc))
                {
                    // give the in-spec NPC the shared sandbox package so it behaves when materialised
                    if (!npc.Packages.Contains(LivingSandboxPackageEd)) npc.Packages.Add(LivingSandboxPackageEd);
                    // a hostile living NPC genuinely fights (the bandit has a bandit's life); external refs keep their AI
                    if (ln.Alignment.Trim().Equals("hostile", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(npc.Aggression))
                        npc.Aggression = "Aggressive";
                }
            }

            ctrl.Aliases.Add(new QuestAliasSpec
            {
                Name = $"Living{i}_{tag}",
                Fill = fill,
                AllowReserved = external,   // a unique follower's ref is usually reserved
                Script = LivingAliasScript,
                ScriptProperties =
                {
                    new PropertySpec { Name = "Archetype", Type = "int", Int = LivingArchetypeCode(ln.Archetype) },
                    new PropertySpec { Name = "HoldMarker", Type = "object", ObjectEditorId = LivingHoldMarkerEd },
                    new PropertySpec { Name = "Anchors",    Type = "object", ObjectEditorId = anchorsFlstEd },
                    new PropertySpec { Name = "DeedCount",  Type = "object", ObjectEditorId = deedEd },
                },
            });

            // 傳唱: a rumor topic on the section's speaker, gated on this NPC's deed global
            if (!string.IsNullOrWhiteSpace(sec.RumorSpeaker) && ln.Rumors.Count > 0)
            {
                spec.Dialogue.Add(new DialogueSpec
                {
                    EditorId = $"{prefix}_Rumor",
                    QuestEditorId = LivingCtrlQuestEd,
                    SpeakerNpcEditorId = sec.RumorSpeaker,
                    Prompt = string.IsNullOrWhiteSpace(ln.Name) ? "Heard any rumors lately?" : $"Any word of {ln.Name}?",
                    Responses = new List<string>(ln.Rumors),
                    Conditions = { new ConditionSpec { Function = "GetGlobalValue", Param = deedEd, Comparison = ">=", Value = 1 } },
                });
            }

            // --- P3: relationship layer — a per-NPC favor global + interaction topics on the NPC ---
            // Talking to the living NPC offers interactions that adjust its favor global (the
            // relationship-memory substrate future content / alignment branches gate on). Uses the
            // existing dialogue setGlobal (a TIF result fragment `package` compiles).
            if (ln.Interactions.Count > 0)
            {
                string favorEd = $"{prefix}_Favor";
                spec.Globals.Add(new GlobalSpec { EditorId = favorEd, Type = "long", Value = 0 });
                foreach (var kind in ln.Interactions)
                {
                    var (prompt, response, delta, deedGated) = LivingInteraction(kind);
                    if (prompt is null) continue; // unknown kind — validation reports it
                    var dlg = new DialogueSpec
                    {
                        EditorId = $"{prefix}_Act_{kind.Trim().ToLowerInvariant()}",
                        QuestEditorId = LivingCtrlQuestEd,
                        SpeakerNpcEditorId = ln.Ref,
                        Prompt = prompt,
                        Responses = new List<string> { response },
                        SetGlobal = new DialogueSetGlobalSpec { Global = favorEd, Delta = delta },
                    };
                    if (deedGated)
                        dlg.Conditions.Add(new ConditionSpec { Function = "GetGlobalValue", Param = deedEd, Comparison = ">=", Value = 1 });
                    spec.Dialogue.Add(dlg);
                }
            }
        }

        spec.Quests.Add(ctrl);

        // world controller on the host quest: one tick + one poll over aliases 0..AliasCount-1
        spec.Scripts.Add(new ScriptAttachSpec
        {
            TargetEditorId = LivingCtrlQuestEd,
            ScriptName = LivingControllerScript,
            Properties =
            {
                new PropertySpec { Name = "SimIntervalHours", Type = "float", Float = sec.SimIntervalHours },
                new PropertySpec { Name = "PollInterval",     Type = "float", Float = sec.PollInterval },
                new PropertySpec { Name = "AliasCount",       Type = "int",   Int = sec.Npcs.Count },
            },
        });
    }
}
