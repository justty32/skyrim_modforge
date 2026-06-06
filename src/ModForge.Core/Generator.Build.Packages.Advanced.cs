namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        private void ApplyTravelData(PackageSpec pk, IPackage pack)
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

        private void ApplyUseMagicData(PackageSpec pk, IPackage pack)
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

        private void ApplyPatrolData(PackageSpec pk, IPackage pack)
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

        private void ApplyFollowData(PackageSpec pk, IPackage pack)
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

        private void ApplyEscortData(PackageSpec pk, IPackage pack)
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

        private void ApplySitTargetData(PackageSpec pk, IPackage pack)
        {
            // SitTarget slots (3 author of 13): 16 Target (deferred SingleRef — the FURNITURE ref to
            // sit/use; REQUIRED, can be an in-spec placement so it's resolved after the placement loop),
            // 3 Wait Time (float seconds), 4 Stop Movement Flag (bool). Decoded from vanilla
            // MQ306EsbernSit (which sets exactly slots 16/3/4). The remaining template slots
            // (Sit Location / Chairs / Destination / RideHorse / …) keep their template defaults.
            var st = pk.SitTarget;
            if (string.IsNullOrWhiteSpace(st.Target))
                Warn($"  ! package '{pk.EditorId}' sitTarget: no `target` ref — NPC has no furniture to use and won't sit");
            else
                deferredTargetWires.Add((pack, 16, "Target", pk.EditorId, st.Target));
            pack.Data[3] = new PackageDataFloat { Name = "Wait Time",          Data = st.WaitTime ?? 0f };
            pack.Data[4] = new PackageDataBool  { Name = "Stop Movement Flag", Data = st.StopMovement ?? false };
        }
    }
}
