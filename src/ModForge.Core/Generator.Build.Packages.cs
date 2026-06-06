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

                if (tfk == PackageTemplates.Sandbox)        ApplySandboxData(pk, pack);
                else if (tfk == PackageTemplates.Sleep)     ApplySleepData(pk, pack);
                else if (tfk == PackageTemplates.Travel)    ApplyTravelData(pk, pack);
                else if (tfk == PackageTemplates.UseMagic)  ApplyUseMagicData(pk, pack);
                else if (tfk == PackageTemplates.Patrol)    ApplyPatrolData(pk, pack);
                else if (tfk == PackageTemplates.Follow)    ApplyFollowData(pk, pack);
                else if (tfk == PackageTemplates.Escort)    ApplyEscortData(pk, pack);
                else if (tfk == PackageTemplates.SitTarget) ApplySitTargetData(pk, pack);
                else
                {
                    Warn($"  ! package '{pk.EditorId}': template {tfk} is not yet supported (known: sandbox=0x01C254, sleep=0x019717, travel=0x016FAA, usemagic=0x0504F5, patrol=0x017723, follow=0x019B2C, escort=0x023B73, sittarget=0x0A9277) — emitting package with no Data overrides; template defaults apply");
                }
            }
        }

        private void ApplySandboxData(PackageSpec pk, IPackage pack)
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

        private void ApplySleepData(PackageSpec pk, IPackage pack)
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
    }
}
