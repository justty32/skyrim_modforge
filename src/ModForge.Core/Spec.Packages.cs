namespace ModForge;

// --- AI packages: the procedure-template package and its per-template input subobjects ---

// AI Package (PACK): an NPC's decision-layer behaviour ("go sandbox at the smithy", "travel to the
// inn at 18:00", "use the cooking pot"). Built on a vanilla PROCEDURE TEMPLATE (`template` =
// "<master>:0xFORMID" of e.g. Skyrim.esm:0x01C254 Sandbox), which defines the data input schema;
// our package fills those inputs. `interruptFlags` (HellosToPlayer/AllowIdleChatter/
// WorldInteractions/…) are the lifelike-NPC switches. Assign to an NPC via NpcSpec.packages.
// Use `packagediag <Skyrim.esm> <templateFormId>` to discover a template's slot schema.
public sealed class PackageSpec
{
    public string EditorId { get; set; } = "";
    public string Template { get; set; } = "";              // ref → PackageTemplate (Skyrim.esm:0x01C254 = Sandbox)
    public List<string> Flags { get; set; } = new();        // Package.Flag names
    public List<string> InterruptFlags { get; set; } = new();// Package.InterruptFlag names
    public string PreferredSpeed { get; set; } = "";        // Walk|Jog|Run|FastWalk
    public string CombatStyle { get; set; } = "";           // ref → CSTY (optional, combat packages)
    public string OwnerQuest { get; set; } = "";            // ref → QUST (optional, Radiant)
    public PackageScheduleSpec Schedule { get; set; } = new();
    // Sandbox-template inputs (apply when `template` is Skyrim.esm:0x01C254). All optional —
    // omit any field to inherit the template's default (e.g. all "Allow Eating/Sleeping/…" default true).
    public SandboxSpec Sandbox { get; set; } = new();
    // Travel-template inputs (apply when `template` is Skyrim.esm:0x016FAA). `place` is the
    // destination ref (a placed REFR/ACHR); without one the NPC won't actually travel anywhere.
    public TravelSpec Travel { get; set; } = new();
    // UseMagic-template inputs (apply when `template` is Skyrim.esm:0x0504F5). NPC stands at
    // `location` and casts a spell from its `spells` list matching `spellType` (a TargetObjectType
    // enum — e.g. TargetActorEffects = ranged offensive) at `target`. Use for priests at altars,
    // mages casting buffs/wards on a schedule, etc. NPC must HAVE a matching spell in its spells.
    public UseMagicSpec UseMagic { get; set; } = new();
    // Patrol-template inputs (apply when `template` is Skyrim.esm:0x017723). `start` is the first
    // patrol-marker placement (a ref to an in-spec `placements[]` entry that has an `editorId`);
    // the NPC follows that marker's `linkedRefs` chain to the next marker, etc. Loop the route by
    // linking the last marker back to the first. Use for "guard walks this beat" behaviour.
    public PatrolSpec Patrol { get; set; } = new();
    // Follow-template inputs (apply when `template` is Skyrim.esm:0x019B2C). `target` is who to
    // follow — defaults to the player (Skyrim.esm:0x000014, what every vanilla "FollowsPlayer"
    // package targets); set it to an in-spec NPC placement to follow another actor. Companions,
    // summoned creatures, tag-alongs.
    public FollowSpec Follow { get; set; } = new();
    // CTDA gates on the package — the engine runs the first package in the NPC's list whose
    // conditions pass, so a conditioned package switches behaviour at runtime (e.g. a Follow package
    // gated on GetInFaction CurrentFollowerFaction==1 only trails the player once she's been recruited).
    public List<ConditionSpec> Conditions { get; set; } = new();
    // Escort-template inputs (apply when `template` is Skyrim.esm:0x023B73). The NPC LEADS the
    // escorted `target` (defaults to the player) to `destination` (a location ref — vanilla marker
    // or an in-spec placement), pausing to wait if they lag. The dual of Follow. Quest guides
    // ("follow me, I'll take you there"), prisoner/VIP escorts, "show the player the way".
    public EscortSpec Escort { get; set; } = new();
}
public sealed class PackageScheduleSpec
{
    public int Month { get; set; } = -1;   // -1 = any (vanilla default)
    public string DayOfWeek { get; set; } = "Any";
    public int Date { get; set; }           // 0 = any
    public int Hour { get; set; } = -1;
    public int Minute { get; set; } = -1;
    public int DurationInMinutes { get; set; }
}
// Sandbox-template (Skyrim.esm:0x01C254) data inputs. Slot indices on the template:
//   0 Location  1 AllowEating  3 AllowSleeping  4 AllowConversation  5 AllowIdleMarkers
//   6 AllowSitting  7 AllowWandering  14 UnlockOnArrival  25 PreferredPathOnly
//   27 RideHorseIfPossible  29 Energy(float)  31 AllowSpecialFurniture
// `location` (optional) is a ref to a placed reference (LocationTarget.Link); omit ⇒
// LocationFallback (NPC uses its editor location). `radius` defaults to 512.
public sealed class SandboxSpec
{
    public string Location { get; set; } = "";   // optional ref → placed reference (an REFR/ACHR)
    public uint Radius { get; set; } = 512;
    public bool? AllowEating { get; set; }
    public bool? AllowSleeping { get; set; }
    public bool? AllowConversation { get; set; }
    public bool? AllowIdleMarkers { get; set; }
    public bool? AllowSitting { get; set; }
    public bool? AllowWandering { get; set; }
    public bool? AllowSpecialFurniture { get; set; }
    public bool? UnlockOnArrival { get; set; }
    public bool? PreferredPathOnly { get; set; }
    public bool? RideHorseIfPossible { get; set; }
    public float? Energy { get; set; }
}
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
