using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // Build a quest's aliases (fills + alias scripts). Shared by the Story-Manager path (def = the
        // event, enabling fromEvent fills) and ordinary StartGameEnabled quests (def = null, so fromEvent
        // is skipped but forced/uniqueActor/createObject/findMatching + alias scripts all still work).
        private void BuildQuestAliases(Quest quest, QuestSpec qs, StoryEventDef? def)
        {
            // Map alias name → its ID (= sequential index) up front so a createObject fill can
            // target another alias by name even when that target is declared later in the list.
            var aliasIdByName = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < qs.Aliases.Count; i++) aliasIdByName[qs.Aliases[i].Name] = (uint)i;

            uint nextId = 0;
            foreach (var aSpec in qs.Aliases)
            {
                var alias = new QuestAlias { ID = nextId, Name = aSpec.Name };
                if (StoryManagerEvents.TryParseFill(aSpec.Fill, out var kind, out var arg))
                {
                    if (kind.Equals("fromEvent", StringComparison.OrdinalIgnoreCase)
                        && def is { } d && d.Slots.TryGetValue(arg, out var slot))
                    {
                        alias.FindMatchingRefFromEvent = new FindMatchingRefFromEvent
                        {
                            FromEvent = d.Code,
                            EventData = (byte[])slot.Clone(),
                        };
                        // The alias TYPE must match the slot's payload kind or the engine fills
                        // null: a Location slot ("L1"/"L2", first byte 'L'=0x4C) needs a LOCATION
                        // alias; a ref slot ("R1"/"R2") a REFERENCE alias. (In-game: a ChangeLocation
                        // L2 fill returned null because the alias defaulted to Reference type.)
                        alias.Type = slot.Length > 0 && slot[0] == (byte)'L'
                            ? QuestAlias.TypeEnum.Location
                            : QuestAlias.TypeEnum.Reference;
                    }
                    else if (kind.Equals("forced", StringComparison.OrdinalIgnoreCase))
                    {
                        // A forced ref to a vanilla form or an already-built in-spec record resolves now.
                        // A forced ref to a record that builds LATER (a placement/xmarker anchor or a map
                        // marker — built after alias passes) is deferred to WireDeferredForcedAliases,
                        // which runs once those records exist.
                        if (TryResolveRef(arg, formKeyByEd, out var fk))
                            alias.ForcedReference.SetTo(fk);
                        else
                            deferredForcedAliases.Add((alias, arg));
                    }
                    else if (kind.Equals("uniqueActor", StringComparison.OrdinalIgnoreCase)
                        && TryResolveRef(arg, formKeyByEd, out var uaFk))
                    {
                        // QuestAlias.UniqueActor (ALUA) = a unique NPC base record this alias
                        // resolves to. <ref> is an in-spec NPC editorId or Plugin.esm:0xID.
                        alias.UniqueActor.SetTo(uaFk);
                        // AllowReserved is REQUIRED here (vanilla sets it on EVERY unique-actor
                        // alias): a unique NPC's persistent ref is usually already reserved by
                        // other quests, and without this flag the fill fails — which, for a
                        // non-optional alias, blocks the whole quest from starting. (In-game:
                        // Ulfric uniqueActor alias kept the quest stopped until this was set.)
                        // NB: QuestAlias.Flags defaults to null, and `|=` on a null lifts to null
                        // (no-op) — must seed from GetValueOrDefault() or the flag never sticks.
                        alias.Flags = alias.Flags.GetValueOrDefault() | QuestAlias.Flag.AllowReserved;
                    }
                    else if (kind.Equals("createObject", StringComparison.OrdinalIgnoreCase)
                        && StoryManagerEvents.TryParseCreateObject(arg, out var objRef, out var tgtName)
                        && TryResolveRef(objRef, formKeyByEd, out var objFk)
                        && aliasIdByName.TryGetValue(tgtName, out var tgtId))
                    {
                        // CreateReferenceToObject (ALCO/ALCA/ALCL): on quest start the engine SPAWNS
                        // a new reference to <objFk> AT the ref held by alias <tgtName> (e.g. spawn a
                        // chest at the event's caster). Vanilla always points AliasID at a Reference-
                        // type alias (an actual ref), never a Location. Create=At; Level=Easy is
                        // ignored unless <objFk> is a leveled actor base.
                        var cro = new CreateReferenceToObject
                        {
                            AliasID = (short)tgtId,
                            Create = CreateReferenceToObject.CreateEnum.At,
                            Level = Level.Easy,
                        };
                        cro.Object.SetTo(objFk);
                        alias.CreateReferenceToObject = cro;
                    }
                    else if (kind.Equals("findMatching", StringComparison.OrdinalIgnoreCase))
                    {
                        // Find Matching Reference "in loaded area" (decoded from vanilla MQGreybeardCall
                        // Bystander aliases): the engine fills this alias with an ALREADY-EXISTING ref in
                        // the loaded area matching this alias's Conditions. This is NOT FindMatchingRefNearAlias
                        // (that's linked-ref-children only, which fails for refs with no editor links) — it is
                        // the QuestAlias.Flag MatchingRefInLoadedArea, plus MatchingRefClosest for arg "closest"
                        // (pick the nearest match) vs "any". Match filter lives on QuestAlias.Conditions.
                        alias.Flags = alias.Flags.GetValueOrDefault() | QuestAlias.Flag.MatchingRefInLoadedArea;
                        if (arg.Equals("closest", StringComparison.OrdinalIgnoreCase))
                            alias.Flags |= QuestAlias.Flag.MatchingRefClosest;
                        WireAliasMatchConditions(alias, qs, aSpec, "findMatching");
                    }
                    else if (kind.Equals("findMatchingLocation", StringComparison.OrdinalIgnoreCase))
                    {
                        // #7 radiant LocationAlias fill (Missives Alias_Dungeon/Alias_Inn): a LOCATION-type
                        // alias filled by "Find Matching Location" — pick a child location whose LocType
                        // keyword matches, optionally narrowed to within a parent location alias.
                        // arg = "<locTypeKeyword>[@<parentLocationAlias>]". Keyword resolves like any ref
                        // (in-spec KYWD editorId or Plugin.esm:0xID); parent alias by name (this quest).
                        var locKw = arg; int? parentIdx = null;
                        int at = arg.LastIndexOf('@');
                        if (at > 0 && at < arg.Length - 1)
                        {
                            locKw = arg[..at];
                            if (aliasIdByName.TryGetValue(arg[(at + 1)..], out var pIdx)) parentIdx = (int)pIdx;
                        }
                        if (TryResolveRef(locKw, formKeyByEd, out var kwFk))
                        {
                            // A "Find Matching Location" radiant fill (Missives Alias_Dungeon) is NOT a
                            // LocationAliasReference — byte-compare vs the shipping Missives _M_QuestWhiterunKillBandit
                            // 'Dungeon' alias (2026-06-21) shows a Location-type alias filled by CTDA match
                            // conditions: LocationHasKeyword==1 (the LocType), plus GetInCurrentLocAlias==1
                            // (LocationAliasIndex=parent) when narrowed to a parent location alias. The engine
                            // ignores LocationAliasReference.Keyword on a Location alias, so the old encoding
                            // never filled. StoresText lets the <Alias=Name> token render the picked location.
                            alias.Type = QuestAlias.TypeEnum.Location;
                            alias.Flags = alias.Flags.GetValueOrDefault() | QuestAlias.Flag.StoresText;
                            var kwData = new LocationHasKeywordConditionData();
                            kwData.Keyword.Link.SetTo(kwFk);
                            alias.Conditions.Add(new ConditionFloat
                            {
                                CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1, Data = kwData,
                            });
                            if (parentIdx is int pi)
                                alias.Conditions.Add(new ConditionFloat
                                {
                                    CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1,
                                    Data = new GetInCurrentLocAliasConditionData { LocationAliasIndex = pi },
                                });
                            WireAliasMatchConditions(alias, qs, aSpec, "findMatchingLocation");
                        }
                    }
                    else if (kind.Equals("findInLocationAlias", StringComparison.OrdinalIgnoreCase))
                    {
                        // #8 radiant find-ref-in-location fill (Missives Alias_target/Alias_chest): a
                        // REFERENCE-type alias filled by "Find Matching Reference" scoped to the location
                        // held by another LOCATION alias — pick a ref of an optional RefType (LCRT, e.g. a
                        // dungeon BossChest) and/or matching this alias's `conditions`. arg =
                        // "<locationAlias>[#<refTypeLCRT>]". NOTE: this uses LocationAliasReference (whose
                        // RefType field is meaningless for a Location-type alias — proving Location doubles
                        // as the Reference-alias "find ref in location" shape), NOT FindMatchingRefNearAlias
                        // (verified offline = LinkedRefChild-only; the wrong tool for in-location search).
                        var locAlias = arg; string refTypeRef = "";
                        int hash = arg.IndexOf('#');
                        if (hash > 0 && hash < arg.Length - 1) { locAlias = arg[..hash]; refTypeRef = arg[(hash + 1)..]; }
                        if (aliasIdByName.TryGetValue(locAlias, out var locIdx))
                        {
                            alias.Type = QuestAlias.TypeEnum.Reference;
                            var loc = new LocationAliasReference { AliasID = (int)locIdx };
                            if (refTypeRef.Length > 0 && TryResolveRef(refTypeRef, formKeyByEd, out var rtFk))
                                loc.RefType.SetTo(rtFk);             // LocationReferenceType (LCRT) to match
                            alias.Location = loc;
                            WireAliasMatchConditions(alias, qs, aSpec, "findInLocationAlias");
                        }
                    }
                }
                if (aSpec.Optional)
                    alias.Flags = alias.Flags.GetValueOrDefault() | QuestAlias.Flag.Optional;
                // AllowReserved lets the fill grab a ref another quest has reserved (via
                // ReservesLocationOrReference). Without it, killing/targeting an actor a running
                // quest holds (e.g. a Riverwood NPC reserved by a Freeform quest) fails to fill —
                // and a required alias that can't fill blocks the whole quest from starting.
                if (aSpec.AllowReserved)
                    alias.Flags = alias.Flags.GetValueOrDefault() | QuestAlias.Flag.AllowReserved;
                // Alias package overrides (ALPS): the AI packages that drive WHATEVER actor fills this
                // alias, highest priority first. Without this an escort/travel PACK never runs — the
                // record exists but is unassigned. Packages are top-level records created in pass 1, so
                // their editorIds resolve here. (Deferred-safe: resolved by FormKey, no build order dep.)
                foreach (var pkgRef in aSpec.Packages)
                    if (TryResolveRef(pkgRef, formKeyByEd, out var pkgFk))
                        alias.PackageData.Add(pkgFk.ToLink<IPackageGetter>());
                    else
                        Warn($"  ! quest '{qs.EditorId}' alias '{aSpec.Name}' package '{pkgRef}' unresolved — alias won't run it");
                quest.Aliases.Add(alias);
                if (!string.IsNullOrEmpty(aSpec.Script))
                    AttachAliasScript(quest, alias.ID, aSpec);
                nextId++;
            }
            quest.NextAliasID = nextId;
        }

        // Ordinary (non-storyEvent, StartGameEnabled) quests can ALSO carry aliases — a forced/uniqueActor
        // NPC alias, a createObject spawn, a findMatching pick, and crucially an alias OnActivate script —
        // without being event-launched. BuildStoryManager handles storyEvent quests; this covers the rest.
        // Runs after BuildStoryManager (so storyEvent quests are skipped) and before WireQuestStages (so an
        // alias-script adapter is in place for the stage-fragment merge). fromEvent fills are meaningless
        // here (no event) and are skipped (validator warns).
        public void BuildStandaloneQuestAliases()
        {
            foreach (var qs in spec.Quests)
            {
                if (qs.StoryEvent is not null) continue;                  // handled in BuildStoryManager
                if (qs.Aliases.Count == 0) continue;
                if (string.IsNullOrEmpty(qs.EditorId) || !questsByEd.TryGetValue(qs.EditorId, out var quest)) continue;
                BuildQuestAliases(quest, qs, null);
            }
        }

        // Attach a quest "alias script" — a ReferenceAlias-extending script that reacts to events on
        // the ref filling this alias (e.g. OnActivate), letting even a runtime-created (createObject) or
        // runtime-matched (findMatching) ref carry behaviour no base-object script could. Stored on the
        // quest's QuestAdapter.Aliases as a QuestFragmentAlias whose Property points at the owning quest
        // + alias ID. Vanilla shape (decoded from Skyrim.esm alias-only quests): adapter + QFA both
        // Version=5/ObjectFormat=2, empty FileName, each script Flag=Local. The user provides the
        // compiled .pex (via package), so attach unconditionally like a ScriptAttach / user ResultScript.
        private void AttachAliasScript(Quest quest, uint aliasId, QuestAliasSpec aSpec)
        {
            if (quest.VirtualMachineAdapter is not QuestAdapter qad)
            {
                qad = new QuestAdapter { Version = 5, ObjectFormat = 2 };
                quest.VirtualMachineAdapter = qad;
            }
            var qfa = new QuestFragmentAlias { Version = 5, ObjectFormat = 2 };
            qfa.Property.Object.SetTo(quest.FormKey);
            qfa.Property.Alias = (short)aliasId;
            qfa.Property.Flags = ScriptProperty.Flag.Edited;
            var entry = new ScriptEntry { Name = aSpec.Script, Flags = ScriptEntry.Flag.Local };
            FillProperties(entry, aSpec.ScriptProperties, aSpec.Script);
            qfa.Scripts.Add(entry);
            qad.Aliases.Add(qfa);
            scriptsAttached++;
        }
    }
}
