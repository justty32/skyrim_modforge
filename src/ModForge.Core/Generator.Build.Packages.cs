namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 2: AI Package (PACK) — resolve template/combatStyle/ownerQuest refs and dispatch
        // Data slot filling by the vanilla procedure template referenced. Each template has its own
        // sbyte-indexed named slot schema (discover via `packagediag <Skyrim.esm> <templateFormId>`);
        // we mirror what vanilla concrete packages emit so the engine gets explicit values.
        // Some slots point at a placement created later (the placement loop runs after this) or a
        // vanilla ref; those are collected in deferredTargetWires/deferredLocationWires and emitted
        // by WireDeferredTargets/WireDeferredLocations once placements register their editorIds.
        public void BuildPackageData()
        {
            // Known templates live in PackageTemplates (shared with Validate so the FormIDs can't drift).
            var skyrimEsm = ModKey.FromNameAndExtension("Skyrim.esm");

            foreach (var pk in spec.Packages)
            {
                if (!recordsByEd.TryGetValue(pk.EditorId, out var rec) || rec is not IPackage pack) continue;
                Resolve($"package '{pk.EditorId}' template",    pk.Template,    fk => pack.PackageTemplate.SetTo(fk));
                Resolve($"package '{pk.EditorId}' combatStyle", pk.CombatStyle, fk => pack.CombatStyle.SetTo(fk));
                Resolve($"package '{pk.EditorId}' ownerQuest",  pk.OwnerQuest,  fk => pack.OwnerQuest.SetTo(fk));

                var tfk = pack.PackageTemplate.FormKey;
                if (tfk.IsNull || tfk.ModKey != skyrimEsm)
                {
                    if (!string.IsNullOrWhiteSpace(pk.Template))
                        Warn($"  ! package '{pk.EditorId}': template '{pk.Template}' not a Skyrim.esm template; no Data overrides emitted (template defaults apply)");
                    continue;
                }

                if (tfk == PackageTemplates.Sandbox)
                {
                    // Mirrors DefaultSandboxCurrentLocation256 (Skyrim.esm:0x0956B8) — concrete sandboxes
                    // explicitly set all 12 named slots; we do the same so behaviour is deterministic.
                    var sb = pk.Sandbox;
                    pack.Data[0] = MakeLocationSlot("Location", $"package '{pk.EditorId}' sandbox", sb.Location, sb.Radius);
                    void SBool(sbyte slot, string name, bool? user, bool def)
                        => pack.Data[slot] = new PackageDataBool { Name = name, Data = user ?? def };
                    SBool(1,  "Allow Eating",            sb.AllowEating,           true);
                    SBool(3,  "Allow Sleeping",          sb.AllowSleeping,         true);
                    SBool(4,  "Allow Conversation",      sb.AllowConversation,     true);
                    SBool(5,  "Allow Idle Markers",      sb.AllowIdleMarkers,      true);
                    SBool(6,  "Allow Sitting",           sb.AllowSitting,          true);
                    SBool(7,  "Allow Wandering",         sb.AllowWandering,        true);
                    SBool(14, "Unlock On Arrival?",      sb.UnlockOnArrival,       false);
                    SBool(25, "Prefered Path Only?",     sb.PreferredPathOnly,     false);
                    SBool(27, "RideHorseIfPossible",     sb.RideHorseIfPossible,   false);
                    SBool(31, "Allow Special Furniture", sb.AllowSpecialFurniture, true);
                    pack.Data[29] = new PackageDataFloat { Name = "Energy", Data = sb.Energy ?? 50f };
                }
                else if (tfk == PackageTemplates.Sleep)
                {
                    // Mirrors DefaultSleepEditorLoc* concretes: slot 0 Location, the fixed bed-search
                    // scaffolding (1 Search Criteria = TouchActorEffects, 2 Found Bed objectlist, 6/8
                    // internal bools — NOT author-facing; emitted exactly as vanilla so bed-seeking works),
                    // then the named bool/float block. The sleep window is the package Schedule, not a slot.
                    var sl = pk.Sleep;
                    pack.Data[0] = MakeLocationSlot("Sleep Location", $"package '{pk.EditorId}' sleep", sl.Location, sl.Radius == 0 ? 500u : sl.Radius);
                    pack.Data[1] = new PackageDataTarget
                    {
                        Name = "Search Criteria:",
                        Type = PackageDataTarget.Types.Target,
                        Target = new PackageTargetObjectType { Type = TargetObjectType.TouchActorEffects },
                    };
                    pack.Data[2] = new PackageDataObjectList { Name = "Found Bed" };
                    void SlBool(sbyte slot, string name, bool? user, bool def)
                        => pack.Data[slot] = new PackageDataBool { Name = name, Data = user ?? def };
                    SlBool(6,  "Wander Preferred Path Only?", false,                 false);
                    SlBool(8,  "False",                       false,                 false);
                    SlBool(11, "RideHorseIfPossible",         sl.RideHorseIfPossible, false);
                    SlBool(13, "Warn Before Locking?",        sl.WarnBeforeLocking,  true);
                    SlBool(15, "Lock Doors?",                 sl.LockDoors,          true);
                    SlBool(17, "AllowEating",                 sl.AllowEating,        false);
                    SlBool(18, "AllowSleeping",               sl.AllowSleeping,      true);
                    SlBool(19, "AllowConversation",           sl.AllowConversation,  true);
                    SlBool(20, "AllowIdleMarkers",            sl.AllowIdleMarkers,   true);
                    SlBool(21, "AllowSitting",                sl.AllowSitting,       true);
                    SlBool(22, "AllowWandering",              sl.AllowWandering,     true);
                    SlBool(25, "AllowSpecialFurniture",       sl.AllowSpecialFurniture, true);
                    pack.Data[26] = new PackageDataFloat { Name = "MinWanderDistance", Data = sl.MinWanderDistance ?? 300f };
                    pack.Data[24] = new PackageDataFloat { Name = "Energy",            Data = sl.Energy ?? 50f };
                }
                else if (tfk == PackageTemplates.Travel)
                {
                    // Travel template has just 3 slots — 0=Place (PackageDataLocation), 2=RideHorse,
                    // 4=PreferPath. `place` is REQUIRED in practice — Travel without a destination
                    // ref means "travel to nowhere" (NearSelf), i.e. the NPC just stands. radius=0 is
                    // template default (= arrive at exact point); set non-zero for "arrive within R".
                    var tv = pk.Travel;
                    if (string.IsNullOrWhiteSpace(tv.Place))
                        Warn($"  ! package '{pk.EditorId}' travel: no `place` ref — Travel will fall back to NearSelf (NPC stays put)");
                    // DEFERRED (like Escort's Destination): `place` may be an IN-SPEC placement editorId
                    // (e.g. an XMarker travel anchor) that isn't registered until the placement loop runs.
                    // Resolving eagerly here would miss it and fall back to NearSelf. MakeLocationSlot
                    // (vanilla ref / in-spec placement / NearSelf) is applied after placements exist.
                    deferredLocationWires.Add((pack, 0, "Place to Travel", pk.EditorId, tv.Place, tv.Radius));
                    pack.Data[2] = new PackageDataBool { Name = "Ride Horse if possible?", Data = tv.RideHorse ?? false };
                    pack.Data[4] = new PackageDataBool { Name = "Prefer Preferred Path?", Data = tv.PreferPath ?? false };
                }
                else if (tfk == PackageTemplates.UseMagic)
                {
                    // UseMagic template active slots are 2-12 (slots 0/1 are inherited APackageData
                    // placeholders; we don't touch them — vanilla concrete packages also skip them).
                    // Slot 3 (Spell) MUST be a PackageTargetObjectID with a FormLink to the specific
                    // SPEL record — discovered by scanning all 46 vanilla UseMagic packages with
                    // `pkgsbytemplate` (round-1 in-game failure: PackageTargetObjectType silently
                    // no-ops). Slot 4 (Target) MUST be set: PackageTargetSelf for self-cast spells
                    // (Candlelight/Healing/Ward), PackageTargetSpecificReference for cast-at-X.
                    var um = pk.UseMagic;
                    pack.Data[2] = MakeLocationSlot("Location", $"package '{pk.EditorId}' usemagic", um.Location, um.Radius);

                    if (string.IsNullOrWhiteSpace(um.Spell))
                    {
                        Warn($"  ! package '{pk.EditorId}' usemagic: no `spell` ref — package will no-op (engine has nothing to cast)");
                    }
                    else if (TryResolveRef(um.Spell, formKeyByEd, out var spellFk))
                    {
                        linksWired++;
                        if (LooksExternalRef(um.Spell)) extLinks++;
                        pack.Data[3] = new PackageDataTarget
                        {
                            Name = "Spell",
                            Type = PackageDataTarget.Types.Target,
                            Target = new PackageTargetObjectID { Reference = new FormLink<IObjectIdGetter>(spellFk) },
                        };
                    }
                    else
                    {
                        Warn($"  ! package '{pk.EditorId}' usemagic spell '{um.Spell}' unresolved — package will no-op");
                    }

                    if (!string.IsNullOrWhiteSpace(um.Target)
                        && TryResolveRef(um.Target, formKeyByEd, out var tgtFk))
                    {
                        linksWired++;
                        if (LooksExternalRef(um.Target)) extLinks++;
                        pack.Data[4] = new PackageDataTarget
                        {
                            Name = "Target",
                            Type = PackageDataTarget.Types.SingleRef,
                            Target = new PackageTargetSpecificReference { Reference = new FormLink<IPlacedGetter>(tgtFk) },
                        };
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(um.Target))
                            Warn($"  ! package '{pk.EditorId}' usemagic target '{um.Target}' unresolved — defaulting to PackageTargetSelf");
                        pack.Data[4] = new PackageDataTarget
                        {
                            Name = "Target",
                            Type = PackageDataTarget.Types.SingleRef,
                            Target = new PackageTargetSelf(),
                        };
                    }

                    void UBool(sbyte slot, string name, bool? user, bool def)
                        => pack.Data[slot] = new PackageDataBool { Name = name, Data = user ?? def };
                    void UFloat(sbyte slot, string name, float? user, float def)
                        => pack.Data[slot] = new PackageDataFloat { Name = name, Data = user ?? def };
                    void UInt(sbyte slot, string name, uint? user, uint def)
                        => pack.Data[slot] = new PackageDataInt { Name = name, Data = user ?? def };
                    UBool(5,  "HoldWhenBlocked", um.HoldWhenBlocked, true);
                    UFloat(6, "CastTimeMin",     um.CastTimeMin,     2f);
                    UFloat(7, "CastTimeMax",     um.CastTimeMax,     3f);
                    UFloat(8, "CooldownTimeMin", um.CooldownTimeMin, 1f);
                    UFloat(9, "CooldownTimeMax", um.CooldownTimeMax, 3f);
                    UInt (10, "NumToCastMin",    um.NumToCastMin,    1u);
                    UInt (11, "NumToCastMax",    um.NumToCastMax,    1u);
                    UBool(12, "DualCast",        um.DualCast,        false);
                }
                else if (tfk == PackageTemplates.Patrol)
                {
                    // Patrol slots: 0 Patrol Start (deferred — points at a marker placement created in
                    // the placement loop below), 1 Patrol Radius (float), 2 Repeatable?, 4 Start At
                    // Nearest?, 6 Ride Horse if Possible?, 8 Static Pathing?. The route itself is the
                    // linkedRefs chain off the start marker, wired after placements.
                    var pt = pk.Patrol;
                    if (string.IsNullOrWhiteSpace(pt.Start))
                        Warn($"  ! package '{pk.EditorId}' patrol: no `start` ref — NPC has no route and won't patrol");
                    else
                        deferredTargetWires.Add((pack, 0, "Patrol Start", pk.EditorId, pt.Start));
                    pack.Data[1] = new PackageDataFloat { Name = "Patrol Radius",            Data = pt.Radius ?? 150f };
                    pack.Data[2] = new PackageDataBool  { Name = "Repeatable?",              Data = pt.Repeatable ?? true };
                    pack.Data[4] = new PackageDataBool  { Name = "Start At Nearest?",        Data = pt.StartAtNearest ?? true };
                    pack.Data[6] = new PackageDataBool  { Name = "Ride Horse if Possible?",  Data = pt.RideHorse ?? false };
                    pack.Data[8] = new PackageDataBool  { Name = "Static Pathing?",          Data = pt.StaticPathing ?? false };
                }
                else if (tfk == PackageTemplates.Follow)
                {
                    // Follow slots: 0 Target to Follow (deferred — defaults to the player 0x000014, all
                    // vanilla "FollowsPlayer" packages emit PackageTargetSpecificReference(000014); can
                    // also be an in-spec NPC placement to follow another actor), 1 Min Radius (float),
                    // 2 Max Radius (float), 4 Accompany?, 6 Ride Horse?, 8 Need LOS?.
                    var fo = pk.Follow;
                    var tgt = string.IsNullOrWhiteSpace(fo.Target) ? "Skyrim.esm:0x000014" : fo.Target;
                    deferredTargetWires.Add((pack, 0, "Target to Follow", pk.EditorId, tgt));
                    pack.Data[1] = new PackageDataFloat { Name = "Min Radius:", Data = fo.MinRadius ?? 128f };
                    pack.Data[2] = new PackageDataFloat { Name = "Max Radius:", Data = fo.MaxRadius ?? 256f };
                    pack.Data[4] = new PackageDataBool  { Name = "Accompany?", Data = fo.Accompany ?? true };
                    pack.Data[6] = new PackageDataBool  { Name = "Ride Horse?", Data = fo.RideHorse ?? false };
                    pack.Data[8] = new PackageDataBool  { Name = "Need LOS?", Data = fo.NeedLineOfSight ?? false };
                }
                else if (tfk == PackageTemplates.Escort)
                {
                    // Escort slots (9 active of 15): 11 Target to Escort (deferred SingleRef — who the NPC
                    // leads; defaults to the player 0x000014, like Follow), 2 Number of Followers (int),
                    // 3 Destination (PackageDataLocation — deferred so it can be an authored marker, REQUIRED),
                    // 4 Distance to Wait for Follower(s) (float), 5 Follower Min Distance, 6 Follower Max
                    // Distance, 13 Ride Horse?, 15 PreferPreferredPath?, 17 Run If Behind Distance.
                    // Escort = the NPC LEADS the escorted target to the destination, pausing if they lag past
                    // "Distance to Wait". The dual of Follow: here the NPC walks ahead, the target tags along.
                    var es = pk.Escort;
                    var tgt = string.IsNullOrWhiteSpace(es.Target) ? "Skyrim.esm:0x000014" : es.Target;
                    deferredTargetWires.Add((pack, 11, "Target to Escort", pk.EditorId, tgt));
                    if (string.IsNullOrWhiteSpace(es.Destination))
                        Warn($"  ! package '{pk.EditorId}' escort: no `destination` ref — Escort will fall back to NearSelf (NPC won't lead anywhere)");
                    deferredLocationWires.Add((pack, 3, "Destination", pk.EditorId, es.Destination, es.Radius));
                    pack.Data[2]  = new PackageDataInt   { Name = "Number of Followers:",            Data = es.NumberOfFollowers ?? 1u };
                    pack.Data[4]  = new PackageDataFloat { Name = "Distance to Wait for Follower(s):", Data = es.WaitDistance ?? 512f };
                    pack.Data[5]  = new PackageDataFloat { Name = "Follower Min Distance:",          Data = es.FollowerMinDistance ?? 120f };
                    pack.Data[6]  = new PackageDataFloat { Name = "Follower Max Distance:",          Data = es.FollowerMaxDistance ?? 256f };
                    pack.Data[13] = new PackageDataBool  { Name = "Ride Horse?",                     Data = es.RideHorse ?? false };
                    pack.Data[15] = new PackageDataBool  { Name = "PreferPreferredPath?",            Data = es.PreferPreferredPath ?? false };
                    pack.Data[17] = new PackageDataFloat { Name = "Run If Behind Distance",          Data = es.RunIfBehindDistance ?? 500f };
                }
                else
                {
                    Warn($"  ! package '{pk.EditorId}': template {tfk} is not yet supported (known: sandbox=0x01C254, sleep=0x019717, travel=0x016FAA, usemagic=0x0504F5, patrol=0x017723, follow=0x019B2C, escort=0x023B73) — emitting package with no Data overrides; template defaults apply");
                }
            }
        }
    }
}
