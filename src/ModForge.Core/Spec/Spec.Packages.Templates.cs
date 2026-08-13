namespace ModForge;

// Travel-template (Skyrim.esm:0x016FAA) data inputs:
//   0 Place to Travel (PackageDataLocation) — the destination (a placed REFR/ACHR ref)
//   2 Ride Horse if possible? (bool, default false)
//   4 Prefer Preferred Path? (bool, default false)
// `place` should be a real ref; without it the package falls back to NearSelf (no movement).
public sealed class TravelSpec
{
    public string Place { get; set; } = "";   // ref → a placed REFR/ACHR (where to travel to)
    public uint Radius { get; set; } = 0;     // 0 = arrive at exact point (template default); non-zero = arrive within radius
    public bool? RideHorse { get; set; }
    public bool? PreferPath { get; set; }
}
// UseMagic-template (Skyrim.esm:0x0504F5) data inputs. Slot indices on the template:
//   2 Location (PackageDataLocation, default radius 500)
//   3 Spell    (PackageDataTarget with PackageTargetObjectID → FormLink to a SPEL record — REQUIRED)
//   4 Target   (PackageDataTarget with PackageTargetSelf for self-cast, else PackageTargetSpecificReference)
//   5 HoldWhenBlocked (bool, default true)
//   6/7 CastTimeMin/Max (float, default 2/3 sec)  8/9 CooldownMin/Max (float, default 1/3 sec)
//  10/11 NumToCastMin/Max (int, default 1/1)   12 DualCast (bool, default false)
// IMPORTANT (round-1 in-game failure root cause): the "Spell" slot is NOT a TargetObjectType
// category enum — it's a SPECIFIC spell FormLink (Mutagen `PackageTargetObjectID.Reference` →
// IFormLink<IObjectIdGetter>, which Spell implements). Authoring with PackageTargetObjectType
// produces a structurally-valid package that the engine silently no-ops. All 46 vanilla UseMagic
// packages use `PackageTargetObjectID`. Similarly, slot 4 (Target) MUST be set: vanilla uses
// `PackageTargetSelf` for self-cast spells, `PackageTargetSpecificReference` otherwise; leaving
// it as the template's `PackageTargetLinkedReference` fallback also no-ops in practice.
// `spell` is therefore REQUIRED. `target` is optional — omitted ⇒ PackageTargetSelf (self-cast),
// which is correct for Candlelight/Healing/Ward/etc.
public sealed class UseMagicSpec
{
    public string Location { get; set; } = "";  // optional ref → placed REFR/ACHR (where to cast from); empty ⇒ NearSelf
    public uint Radius { get; set; } = 500;     // location radius (template default 500)
    public string Spell { get; set; } = "";     // REQUIRED ref → SPEL (the specific spell to cast)
    public string Target { get; set; } = "";    // optional ref → placed REFR/ACHR (who to cast on); empty ⇒ Self
    public bool? HoldWhenBlocked { get; set; }
    public float? CastTimeMin { get; set; }
    public float? CastTimeMax { get; set; }
    public float? CooldownTimeMin { get; set; }
    public float? CooldownTimeMax { get; set; }
    public uint? NumToCastMin { get; set; }
    public uint? NumToCastMax { get; set; }
    public bool? DualCast { get; set; }
}
// Patrol-template (Skyrim.esm:0x017723) data inputs. Slot indices on the template:
//   0 Patrol Start (PackageDataTarget, SingleRef → PackageTargetSpecificReference to a marker REFR)
//   1 Patrol Radius (float, default 150)   2 Repeatable? (bool, default true)
//   4 Start At Nearest? (bool, default true)   6 Ride Horse if Possible? (bool, default false)
//   8 Static Pathing? (bool, default false)
// The route is the LINKED-REFERENCE chain off the start marker: each marker placement's
// `linkedRefs` points to the next marker (null keyword = the default patrol link the engine
// follows); link the last back to the first to loop. `start` is REQUIRED — without it the NPC
// has no route and won't patrol. Vanilla concrete patrols use either PackageTargetSpecificReference
// (a placed marker, which we emit) or PackageTargetLinkedReference (the NPC's own linked-ref).
public sealed class PatrolSpec
{
    public string Start { get; set; } = "";        // REQUIRED ref → a placement editorId (the first marker)
    public float? Radius { get; set; }             // default 150
    public bool? Repeatable { get; set; }          // default true (loop the route)
    public bool? StartAtNearest { get; set; }      // default true (begin at the closest marker)
    public bool? RideHorse { get; set; }           // default false
    public bool? StaticPathing { get; set; }       // default false
}
// Follow-template (Skyrim.esm:0x019B2C) data inputs. Slot indices on the template:
//   0 Target to Follow (PackageDataTarget, SingleRef → PackageTargetSpecificReference; defaults to
//     the player 0x000014, as every vanilla "FollowsPlayer" package does), 1 Min Radius (float),
//   2 Max Radius (float), 4 Accompany? (bool), 6 Ride Horse? (bool), 8 Need LOS? (bool).
// The NPC trails `target`, closing to Min and not straying past Max. Note: this is the raw movement
// behaviour only — a full vanilla FOLLOWER also needs a follow faction / dialogue / a managing quest;
// this package alone makes an actor physically tag along (companion-lite, summon, escort).
public sealed class FollowSpec
{
    public string Target { get; set; } = "";       // ref → who to follow; empty ⇒ the player (Skyrim.esm:0x000014)
    public float? MinRadius { get; set; }          // default 128 (how close it closes in)
    public float? MaxRadius { get; set; }          // default 256 (how far it may lag)
    public bool? Accompany { get; set; }           // default true
    public bool? RideHorse { get; set; }           // default false
    public bool? NeedLineOfSight { get; set; }     // default false
}
// Escort-template (Skyrim.esm:0x023B73) data inputs. Slot indices on the template:
//   11 Target to Escort (PackageDataTarget, SingleRef → PackageTargetSpecificReference; defaults to
//      the player 0x000014) — who the NPC LEADS to the destination.
//    3 Destination (PackageDataLocation — REQUIRED; vanilla ref or in-spec placement). Without it the
//      package falls back to NearSelf and the NPC won't lead anywhere.
//    2 Number of Followers (int, default 1)   4 Distance to Wait for Follower(s) (float, default 512)
//    5 Follower Min Distance (float, default 120)   6 Follower Max Distance (float, default 256)
//   13 Ride Horse? (bool, default false)   15 PreferPreferredPath? (bool, default false)
//   17 Run If Behind Distance (float, default 500)
// Escort is the DUAL of Follow: the NPC walks ahead toward the destination and the escorted target
// tags along, with the NPC pausing if they fall past the wait distance. Same navmesh rules apply —
// the destination must sit on reachable navmesh, and cross-cell escort needs the citizenship recipe.
public sealed class EscortSpec
{
    public string Target { get; set; } = "";          // ref → who to escort; empty ⇒ the player (Skyrim.esm:0x000014)
    public string Destination { get; set; } = "";      // REQUIRED ref → where to lead them (vanilla marker or in-spec placement)
    public uint Radius { get; set; } = 0;              // destination radius (0 = arrive at exact point)
    public uint? NumberOfFollowers { get; set; }       // default 1
    public float? WaitDistance { get; set; }           // default 512 (how far the target may lag before the NPC waits)
    public float? FollowerMinDistance { get; set; }    // default 120
    public float? FollowerMaxDistance { get; set; }    // default 256
    public bool? RideHorse { get; set; }               // default false
    public bool? PreferPreferredPath { get; set; }     // default false
    public float? RunIfBehindDistance { get; set; }    // default 500
}
// SitTarget-template (Skyrim.esm:0x0A9277, editorID "SitTarget") data inputs. The "go use that piece
// of furniture" routine — the NPC walks to a furniture reference and sits/uses it. Decoded from vanilla
// MQ306EsbernSit. Author-facing slots on the template:
//   16 Target (PackageDataTarget, SingleRef → PackageTargetSpecificReference to a placed FURNITURE ref
//      — REQUIRED; without it the NPC has nothing to sit on and the package no-ops),
//    3 Wait Time (float seconds, 0 = until the package ends / scene phase window closes),
//    4 Stop Movement Flag (bool, default false).
// This is the scene-performance "sit" beat: a scene Package action references a SitTarget package so an
// actor sits on a chair mid-scene (the chair must be a placed furniture ref reachable on navmesh in the
// same cell, like the Travel destination rule). `target` may be a vanilla REFR or an in-spec placement.
public sealed class SitTargetSpec
{
    public string Target { get; set; } = "";       // REQUIRED ref → a placed FURNITURE reference (vanilla REFR or in-spec placement)
    public float? WaitTime { get; set; }           // seconds to stay (default 0 = until package/phase ends)
    public bool? StopMovement { get; set; }        // default false
}
// Activate-template (Skyrim.esm:0x019B2D) data inputs. Slot indices on the template:
//   0 Target (PackageDataTarget, SingleRef → PackageTargetSpecificReference; REQUIRED) — the object the
//     NPC walks to and activates (a lever/door/activator). Deferred-wired like Patrol/Follow slot-0 so
//     the ref may be an IN-SPEC placement editorId resolved after the placement loop runs.
//   2 Number to Activate (int, default 1).
// Slots 3 Destination / 4 RideHorseIfPossible / 5 PreferPreferredPath keep template defaults (not emitted).
// Concrete vanilla example dunHillgrundsUnlockExteriorDoorActivate sets exactly [0] (SpecificReference) + [2]=1.
public sealed class ActivateSpec
{
    public string Target { get; set; } = "";    // REQUIRED ref → the object to activate (placement editorId or vanilla ref)
    public uint? NumberToActivate { get; set; }  // default 1
}
// Eat-template (Skyrim.esm:0x019714) data inputs. A LOCATION-based sandbox variant ("go to a location,
// find food, sit and eat") — modelled on Sleep's slot-filling (same fixed search-scaffolding pattern).
// Author-facing + fixed slots decoded from Skyrim.esm:
//   0 Eat Location (PackageDataLocation, default radius 500) — optional; empty ⇒ NearSelf fallback.
//   1 Food Criteria  — FIXED PackageDataTarget(TargetObjectType.Creatures)         (emitted exactly, not author-facing)
//   4 Found Food     — FIXED PackageDataObjectList "Found Food"                     (not author-facing)
//   5 Chair Target   — FIXED PackageDataTarget(TargetObjectType.SelfActorEffects)  (not author-facing)
//   6 Found Chair    — FIXED PackageDataObjectList "Found Chair"                    (not author-facing)
//   10 NumFoodItems(int,1)  12 AllowAlreadyHeld(bool,true)  16 RideHorseIfPossible(bool,false)
//   21 UnlockOnArrival(bool,true)  23 CreateFakeFood(bool,true)  25 AllowEating(bool,true)
//   26 AllowSleeping(bool,false)  27 AllowConversation(bool,true)  28 AllowIdleMarkers(bool,true)
//   29 AllowSitting(bool,true)  30 AllowWandering(bool,true)  32 Energy(float,0)  33 AllowSpecialFurniture(bool,true)
//   35 MinWanderDistance(float,300)
// The named-bool/float/int block mirrors how ApplySleepData emits each slot's exact Name string for
// round-trip fidelity. A reasonable author-facing subset is exposed below; the rest keep their defaults.
public sealed class EatSpec
{
    public string Location { get; set; } = "";    // optional ref → placed reference; empty ⇒ NearSelf fallback
    public uint Radius { get; set; } = 500;        // eat-location radius (template default 500)
    public bool? AllowSitting { get; set; }        // default true (slot 29)
    public bool? AllowWandering { get; set; }      // default true (slot 30)
    public float? Energy { get; set; }             // default 0 (slot 32)
    public float? MinWanderDistance { get; set; }  // default 300 (slot 35)
    public uint? NumFoodItems { get; set; }        // default 1 (slot 10)
}
