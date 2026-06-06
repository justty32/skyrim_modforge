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
    // Sleep-template inputs (apply when `template` is Skyrim.esm:0x019717). A specialized Sandbox that
    // actively SEEKS A BED (built-in bed search) and can lock doors — the "go home and sleep" routine.
    // Gate the sleep window with `schedule` (hour + durationInMinutes); all Sleep slots are optional.
    public SleepSpec Sleep { get; set; } = new();
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
    // SitTarget-template inputs (apply when `template` is Skyrim.esm:0x0A9277). The NPC walks to and
    // sits/uses the furniture ref in `target`. The scene-performance "sit" beat — a scene Package
    // action points at a SitTarget package so an actor takes a seat mid-scene. `target` is REQUIRED.
    public SitTargetSpec SitTarget { get; set; } = new();
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
// Sleep-template (Skyrim.esm:0x019717) data inputs. A specialized Sandbox: it actively searches for a
// bed (slot 1 "Search Criteria" = TouchActorEffects + slot 2 "Found Bed" objectlist — both fixed/emitted
// by the builder, not author-facing) and can lock doors on the way to bed. Author-facing named slots:
//   0 Sleep Location  11 RideHorseIfPossible  13 WarnBeforeLocking  15 LockDoors  17 AllowEating
//   18 AllowSleeping  19 AllowConversation  20 AllowIdleMarkers  21 AllowSitting  22 AllowWandering
//   24 Energy(float)  25 AllowSpecialFurniture  26 MinWanderDistance(float)
// `location` (optional) is a ref to a placed reference; omit ⇒ LocationFallback (NPC's editor location —
// the NPC looks for a bed near where it spawns). `radius` defaults to 500; vanilla "sleep at editor
// location" packages bump it to ~1000–2000 to widen the bed search. The sleep WINDOW is NOT a slot —
// set it via `schedule` (e.g. hour=22, durationInMinutes=540 ⇒ sleeps 22:00–07:00). NOTE on `lockDoors`:
// vanilla defaults TRUE (an NPC locks its house at night); for a follower sleeping in a SHARED space
// (an inn), set it false so she doesn't lock the building.
public sealed class SleepSpec
{
    public string Location { get; set; } = "";   // optional ref → placed reference; omit ⇒ editor location
    public uint Radius { get; set; } = 500;       // bed-search radius (template default 500; widen to ~1000–2000)
    public bool? LockDoors { get; set; }          // default true — set false for shared/inn sleeping
    public bool? WarnBeforeLocking { get; set; }  // default true
    public bool? RideHorseIfPossible { get; set; }// default false
    public bool? AllowEating { get; set; }        // default false (Sleep flips this off vs Sandbox)
    public bool? AllowSleeping { get; set; }       // default true (the whole point)
    public bool? AllowConversation { get; set; }  // default true
    public bool? AllowIdleMarkers { get; set; }   // default true
    public bool? AllowSitting { get; set; }       // default true
    public bool? AllowWandering { get; set; }     // default true
    public bool? AllowSpecialFurniture { get; set; }// default true
    public float? MinWanderDistance { get; set; } // default 300
    public float? Energy { get; set; }            // default 50
}
