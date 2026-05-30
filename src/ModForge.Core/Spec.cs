namespace ModForge;

// --- ESP generator spec (the structured IR; deserialized case-insensitively) ---------
public sealed class ModSpec
{
    public string PluginName { get; set; } = "Generated.esp";
    public bool Esl { get; set; } = true;
    public List<MiscSpec> MiscItems { get; set; } = new();
    public List<BookSpec> Books { get; set; } = new();
    public List<WeaponSpec> Weapons { get; set; } = new();
    public List<NpcSpec> Npcs { get; set; } = new();
    public List<QuestSpec> Quests { get; set; } = new();
    public List<DialogueSpec> Dialogue { get; set; } = new();
    public List<SpellSpec> Spells { get; set; } = new();
    public List<MagicEffectSpec> MagicEffects { get; set; } = new();
    public List<PotionSpec> Potions { get; set; } = new();
    public List<ArmorSpec> Armors { get; set; } = new();
    public List<FactionSpec> Factions { get; set; } = new();
    public List<MessageSpec> Messages { get; set; } = new();
    public List<ScriptAttachSpec> Scripts { get; set; } = new();
    public List<CellSpec> Cells { get; set; } = new();
    public List<PlacementSpec> Placements { get; set; } = new();
    public List<LeveledItemSpec> LeveledItems { get; set; } = new();
    public List<LeveledNpcSpec> LeveledNpcs { get; set; } = new();
    public List<ContainerSpec> Containers { get; set; } = new();
    public List<IngredientSpec> Ingredients { get; set; } = new();
    public List<AmmunitionSpec> Ammunitions { get; set; } = new();
    public List<ScrollSpec> Scrolls { get; set; } = new();
    public List<SoulGemSpec> SoulGems { get; set; } = new();
    public List<KeySpec> Keys { get; set; } = new();
    public List<KeywordSpec> Keywords { get; set; } = new();
    public List<OutfitSpec> Outfits { get; set; } = new();
    public List<StaticSpec> Statics { get; set; } = new();
    public List<ActivatorSpec> Activators { get; set; } = new();
    public List<RecipeSpec> Recipes { get; set; } = new();
    public List<ClassSpec> Classes { get; set; } = new();
    public List<PackageSpec> Packages { get; set; } = new();
    public List<CombatStyleSpec> CombatStyles { get; set; } = new();
    public List<RelationshipSpec> Relationships { get; set; } = new();
}
// "ref" fields below accept EITHER an in-spec editorId OR an external "<master>:0xFORMID"
// (e.g. "Skyrim.esm:0x013746" — find them with the `find` command). External refs auto-add
// the master on write (Mutagen MastersListContent=Iterate).
public sealed class MiscSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<string> Keywords { get; set; } = new(); public string Template { get; set; } = ""; }
public sealed class BookSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Text { get; set; } = ""; public string Template { get; set; } = ""; }
public sealed class WeaponSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public ushort Damage { get; set; } public float Speed { get; set; } public float Reach { get; set; } public List<string> Keywords { get; set; } = new(); public string Template { get; set; } = ""; }
public sealed class NpcSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> Factions { get; set; } = new();
    public string Race { get; set; } = "";       // ref (e.g. Skyrim.esm:0x013746 = NordRace)
    public string Class { get; set; } = "";       // ref
    public string Outfit { get; set; } = "";      // ref -> DefaultOutfit
    public int Level { get; set; }                 // fixed level (0 = leave default); needed for class stat auto-calc
    public bool AutoCalcStats { get; set; }        // derive H/M/S + skills from level + class (else flat defaults)
    public List<string> Packages { get; set; } = new(); // refs to PACK records (in-spec or external) — assigned to this NPC's package list
    public string VoiceType { get; set; } = "";      // ref → VTYP (e.g. Skyrim.esm:0x013AE6 = MaleNord); without one, NPC is silent (no hello/idle chatter)
    public string CrimeFaction { get; set; } = "";   // ref → FACT (e.g. Skyrim.esm:0x0267EA = CrimeFactionWhiterun); marks the NPC as a member of a city's crime/citizen circle — grants city-traversal rights (without it, cross-cell Travel through city gates is silently rejected)
    public bool Unique { get; set; }                  // Configuration.Flag.Unique — engine treats the actor as a one-off (vs leveled spawn); seems to matter for AI tracking + cross-cell travel
    public List<string> Spells { get; set; } = new(); // refs → SPEL records; populates npc.ActorEffect — the AI's spell list, what combat AI considers casting (combined with combatStyle's magic preference)
    public string CombatStyle { get; set; } = "";    // ref → CSTY; HOW the AI fights (magic vs melee preference, aggression, group flank). Without one, the engine uses a default that may not pick spells from `spells`.
    // AIData — controls WHETHER the NPC fights at all (separate system from CombatStyle which is
    // HOW). Mutagen-generated NPCs default to Aggression=Unaggressive + Confidence=Cowardly which
    // means they FLEE from any threat, regardless of CombatStyle or spell list. For a combatant set
    // at minimum Aggression=Aggressive (defends when attacked) + Confidence=Brave (doesn't flee).
    public string Aggression { get; set; } = "";     // Unaggressive|Aggressive|VeryAggressive|Frenzied (default: Unaggressive — won't initiate, won't defend either)
    public string Confidence { get; set; } = "";     // Cowardly|Cautious|Average|Brave|Foolhardy (default: Cowardly — flees any threat)
    public string Assistance { get; set; } = "";     // HelpsNobody|HelpsAllies|HelpsFriendsAndAllies (default: HelpsNobody)
    public string Mood { get; set; } = "";           // Neutral|Angry|Fear|Happy|Sad|Surprised|Puzzled|Disgusted
    public int EnergyLevel { get; set; }              // 0..100 — vanilla actors typically 50
}
public sealed class QuestSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public List<ObjectiveSpec> Objectives { get; set; } = new();
    // StartGameEnabled (default true): the quest auto-starts on game load, which is REQUIRED for any
    // dialogue it hosts to be loaded/evaluated. A quest that never runs = its dialogue never surfaces.
    public bool StartGameEnabled { get; set; } = true;
    public byte Priority { get; set; } = 50;   // higher wins when multiple quests offer dialogue to the same NPC
}
public sealed class ObjectiveSpec { public ushort Index { get; set; } public string Text { get; set; } = ""; }
// A dialogue topic: shown under QuestEditorId's branch; targets SpeakerNpcEditorId (GetIsID).
public sealed class DialogueSpec
{
    public string EditorId { get; set; } = "";
    public string QuestEditorId { get; set; } = "";
    public string SpeakerNpcEditorId { get; set; } = "";
    public string Prompt { get; set; } = "";
    public List<string> Responses { get; set; } = new();
    public string Emotion { get; set; } = "Neutral";   // Neutral|Anger|Disgust|Fear|Sad|Happy|Surprise — applied to all response lines
    public uint EmotionValue { get; set; } = 50;        // 0..100 intensity
}
// Relationship (RELA): a directed bond between two NPCs (`parent` and `child`) at a `rank`. The
// player's NPC *base* record is `Skyrim.esm:0x000014` (NOT `0x000007`, which is PlayerRef — the
// placed ACHR; pointing a RELA at it is a type mismatch that CRASHES on load). `child` defaults to
// `0x000014`, so the common case (an NPC's relationship TO the player) is just `parent` + `rank`.
// Rank (RankType): Lover, Ally, Confidant,
// Friend, Acquaintance, Rival, Foe, Enemy, Archnemesis. **Why it matters for followers:** the vanilla
// DialogueFollower quest's free "Follow me, I need your help" topic is gated on
// `GetRelationshipRank player >= Ally`, so a custom hireable follower needs an Ally relationship to
// the player (plus membership in PotentialFollowerFaction `Skyrim.esm:0x05C84D`).
public sealed class RelationshipSpec
{
    public string EditorId { get; set; } = "";
    public string Parent { get; set; } = "";                  // ref → NPC (the relationship's owner); usually the custom NPC
    public string Child { get; set; } = "Skyrim.esm:0x000014"; // ref → NPC; defaults to the Player NPC base (0x000014, NOT PlayerRef 0x000007)
    public string Rank { get; set; } = "Ally";                // RankType enum name
}
public sealed class SpellSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public List<EffectSpec> Effects { get; set; } = new();
    public string SpellType { get; set; } = "";   // Spell|Power|LesserPower|Ability|Disease|Poison|Voice
    public string CastType { get; set; } = "";     // FireAndForget|Concentration|ConstantEffect
    public string TargetType { get; set; } = "";    // Self|Touch|Aimed|TargetActor|TargetLocation
    public uint BaseCost { get; set; }
    public float ChargeTime { get; set; }
    // EquipType (EQUP) ref — which slot the spell occupies. For a hand spell an NPC (or the player)
    // must equip+cast, set this to EitherHand (Skyrim.esm:0x013F44); BothHands/Left/RightHand exist
    // too. Omit for non-equipped magic (abilities, voice powers). Without it, an NPC can't equip a
    // hand spell into a hand and won't cast it in combat.
    public string EquipType { get; set; } = "";
}
public sealed class PotionSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<EffectSpec> Effects { get; set; } = new(); public string Template { get; set; } = ""; }
// MagicEffect (MGEF): the building block a spell/potion/ingredient/scroll `effect` points at — lets a
// spec define its OWN effect instead of only reusing vanilla ones. `archetype` (MagicEffectArchetype.
// TypeEnum: ValueModifier = the common damage/heal/fortify, plus SummonCreature, Bound, Light,
// Paralysis, …) acts on `actorValue` (Health/Magicka/Stamina/…). `magicSkill` is the school
// (Alteration/Conjuration/Destruction/Illusion/Restoration), `resistValue` the AV that resists it
// (ResistFire/PoisonResist/…). `flags` (Hostile/Detrimental/Recover/NoArea/NoDuration/…) drive UI +
// behaviour. `association` (a ref) is the summoned/bound form for those archetypes. The per-effect
// magnitude/area/duration stay on the spell/potion's `effects[]` entry (not here).
public sealed class MagicEffectSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Archetype { get; set; } = "ValueModifier";
    public string ActorValue { get; set; } = "";   // affected AV, e.g. Health
    public string MagicSkill { get; set; } = "";    // school, e.g. Destruction
    public string ResistValue { get; set; } = "";    // resisted by, e.g. ResistFire
    public string CastType { get; set; } = "";        // FireAndForget|Concentration|ConstantEffect
    public string TargetType { get; set; } = "";       // Self|Touch|Aimed|TargetActor|TargetLocation
    public float BaseCost { get; set; }
    public List<string> Flags { get; set; } = new();
    public string Association { get; set; } = "";       // summon/bound form ref (optional)
    // Visual/projectile refs (optional, usually vanilla) — needed for an Aimed spell to have a
    // visible traveling bolt + cast/impact FX. The projectile carries its own model + impact.
    public string Projectile { get; set; } = "";        // PROJ — the thing that travels (Aimed)
    public string CastingArt { get; set; } = "";        // ARTO — FX at the caster's hands
    public string HitEffectArt { get; set; } = "";      // ARTO — FX at the impact point
    public string Explosion { get; set; } = "";          // EXPL — AoE explosion on impact
}
public sealed class ArmorSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public float ArmorRating { get; set; } public string ArmorType { get; set; } = ""; public List<string> Slots { get; set; } = new(); public List<string> Keywords { get; set; } = new(); }
// One magic effect on a spell/potion: a MagicEffect ref + magnitude/area/duration (EffectData).
public sealed class EffectSpec { public string MagicEffect { get; set; } = ""; public float Magnitude { get; set; } public int Area { get; set; } public int Duration { get; set; } }
public sealed class FactionSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; }
// Class (CLAS): an actor's "profession" — drives its attribute distribution + favoured skills (and,
// for trainers, what it `teaches`). An npc's `class` ref can point at one. `healthWeight`/
// `magickaWeight`/`staminaWeight` are the BasicStat distribution (relative %, ~sum 100); `skillWeights`
// maps a Skill name (OneHanded/Destruction/Sneak/…) to a 0–255 favour. `teaches` (a Skill) +
// `maxTrainingLevel` matter only for trainer NPCs.
public sealed class ClassSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Teaches { get; set; } = "";
    public int MaxTrainingLevel { get; set; }
    public int HealthWeight { get; set; }
    public int MagickaWeight { get; set; }
    public int StaminaWeight { get; set; }
    public Dictionary<string, int> SkillWeights { get; set; } = new();
}
public sealed class MessageSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Description { get; set; } = ""; }
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
// CombatStyle (CSTY): HOW an NPC fights. The six `equipMult*` weights are the AI's preference
// scores per weapon class — a magic-preferring NPC needs `magic` high relative to the others
// (vanilla csVampireMagic: Magic=8.1, Staff=2.15, Melee=0.51). Without a CSTY set on the NPC,
// the engine uses a flat default that may not pick the NPC's spells from its `spells` list.
// `offensiveMult` (~aggression), `defensiveMult` (~blocking/dodging), `groupOffensiveMult`
// (~boldness in groups), `avoidThreatChance` (0..1, chance to back off from danger). `flags`
// values: Dueling, Flanking, AllowDualWielding.
public sealed class CombatStyleSpec
{
    public string EditorId { get; set; } = "";
    public float OffensiveMult { get; set; } = 0.5f;
    public float DefensiveMult { get; set; } = 0.5f;
    public float GroupOffensiveMult { get; set; } = 0.5f;
    public float EquipMultMelee   { get; set; } = 1.0f;
    public float EquipMultMagic   { get; set; } = 1.0f;
    public float EquipMultRanged  { get; set; } = 1.0f;
    public float EquipMultShout   { get; set; } = 1.0f;
    public float EquipMultUnarmed { get; set; } = 1.0f;
    public float EquipMultStaff   { get; set; } = 1.0f;
    public float AvoidThreatChance { get; set; }
    public List<string> Flags { get; set; } = new();   // CombatStyle.Flag names (Dueling/Flanking/AllowDualWielding)
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
// Attach a compiled Papyrus script (by Scriptname) to a record (by editorId), with
// typed properties. type ∈ int|float|bool|string|object; object resolves ObjectEditorId.
public sealed class ScriptAttachSpec
{
    public string TargetEditorId { get; set; } = "";
    public string ScriptName { get; set; } = "";
    public string Source { get; set; } = "";   // optional .psc path (rel. to spec) for `package` to compile
    public List<PropertySpec> Properties { get; set; } = new();
}
public sealed class PropertySpec
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int Int { get; set; }
    public float Float { get; set; }
    public bool Bool { get; set; }
    public string Str { get; set; } = "";
    public string ObjectEditorId { get; set; } = "";
}
// A new interior cell the plugin creates (reachable in-game via `coc <editorId>`).
// `template` (optional, a vanilla INTERIOR cell ref "<master>:0xFORMID") copies that cell's
// lighting/water environment so a brand-new cell isn't pitch-black; it still needs a floor
// static placed in it (a `placement`) so the player doesn't fall into the void.
public sealed class CellSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Template { get; set; } = ""; }
public sealed class Vec3 { public float X { get; set; } public float Y { get; set; } public float Z { get; set; } }
// Place a base form (npc/object, in-spec or external) into the world at a position/rotation.
// TWO targeting modes:
//   * INTERIOR: set `cell` to an in-spec interior cell editorId (It.7d-p1) OR a vanilla interior
//     cell ref "<master>:0xFORMID" (It.7d-p2). `position` is local to that cell.
//   * EXTERIOR: set `worldspace` to a worldspace ref "<master>:0xFORMID" (e.g. Tamriel =
//     Skyrim.esm:0x00003C, find via `find <Skyrim.esm> <name> Worldspace`); `position` is the
//     WORLD position. The cell at floor(x/4096),floor(y/4096) is found in the master and
//     overridden to add this ref (It.7d-p3). `worldspace` wins over `cell` if both are set.
// `rotation` is in degrees. `kind` ("npc"|"object") is inferred for in-spec bases, "object" else.
public sealed class PlacementSpec
{
    public string Base { get; set; } = "";
    public string EditorId { get; set; } = "";     // optional: names this REFR/ACHR so other refs can target it
                                                    // (patrol start, linkedRefs target). Must be unique if set.
    public string Cell { get; set; } = "";        // interior: in-spec editorId OR <master>:0xFORMID
    public string Worldspace { get; set; } = "";   // exterior: worldspace ref; position is world-space
    public string Kind { get; set; } = "";
    public Vec3 Position { get; set; } = new();
    public Vec3 Rotation { get; set; } = new();
    public bool Persistent { get; set; }
    // Linked References on this placed ref. Each points to another placement (by its editorId) or
    // a vanilla placed ref, optionally tagged with a keyword. With no keyword, the link is the
    // engine's "default" linked ref — which is what a Patrol route follows from marker to marker.
    public List<LinkedRefSpec> LinkedRefs { get; set; } = new();
}
// One Linked Reference: `target` is the linked placed ref (a placement editorId or external ref);
// `keyword` (optional ref → KYWD) tags the link. Empty keyword = the null/default link.
public sealed class LinkedRefSpec
{
    public string Target { get; set; } = "";
    public string Keyword { get; set; } = "";
}
// One entry in a leveled list: a ref (item or npc) that appears at >= Level, Count copies.
public sealed class LeveledEntrySpec { public string Reference { get; set; } = ""; public short Level { get; set; } = 1; public short Count { get; set; } = 1; }
// LeveledItem (LVLI) / LeveledNpc (LVLN): chanceNone (0-100), flag names, weighted entries.
public sealed class LeveledItemSpec { public string EditorId { get; set; } = ""; public int ChanceNone { get; set; } public List<string> Flags { get; set; } = new(); public List<LeveledEntrySpec> Entries { get; set; } = new(); }
public sealed class LeveledNpcSpec { public string EditorId { get; set; } = ""; public int ChanceNone { get; set; } public List<string> Flags { get; set; } = new(); public List<LeveledEntrySpec> Entries { get; set; } = new(); }
// Container (CONT): named, with a list of item refs + counts.
public sealed class ContainerEntrySpec { public string Item { get; set; } = ""; public int Count { get; set; } = 1; }
public sealed class ContainerSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public float Weight { get; set; } public List<ContainerEntrySpec> Items { get; set; } = new(); }
// One required ingredient in a recipe: a *ref* (in-spec or vanilla) + how many are consumed.
public sealed class RecipeComponentSpec { public string Item { get; set; } = ""; public int Count { get; set; } = 1; }
// ConstructibleObject (COBJ): a crafting recipe. `createdObject` (a *ref*, usually an in-spec item)
// is made in `count` copies at the `workbench` (a Keyword *ref*; defaults to the forge —
// Skyrim.esm:0x088105 CraftingSmithingForge) by consuming the `components`. Perk/skill gating
// (Conditions) is not yet a spec field — a recipe with components but no condition shows whenever
// you have the materials.
public sealed class RecipeSpec
{
    public string EditorId { get; set; } = "";
    public string CreatedObject { get; set; } = "";
    public int Count { get; set; } = 1;
    public string Workbench { get; set; } = "";   // bench keyword ref; empty -> forge
    public List<RecipeComponentSpec> Components { get; set; } = new();
}

// --- Long-tail record types (same spec-class + build-loop pattern) ---------------------
// Ingredient (INGR): an alchemy reagent — value/weight + `effects` (reuses the spell/potion
// effect pipeline) + keywords.
public sealed class IngredientSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<EffectSpec> Effects { get; set; } = new(); public List<string> Keywords { get; set; } = new(); }
// Ammunition (AMMO): arrow/bolt — value/weight + `damage` (float) + keywords.
public sealed class AmmunitionSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public float Damage { get; set; } public List<string> Keywords { get; set; } = new(); }
// Scroll (SCRL): a one-shot spell-as-item — value/weight + `effects` + spell cast fields.
public sealed class ScrollSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<EffectSpec> Effects { get; set; } = new(); public string SpellType { get; set; } = ""; public string CastType { get; set; } = ""; public string TargetType { get; set; } = ""; public uint BaseCost { get; set; } public List<string> Keywords { get; set; } = new(); }
// SoulGem (SLGM): value/weight + `maximumCapacity` (None|Petty|Lesser|Common|Greater|Grand) + keywords.
public sealed class SoulGemSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public string MaximumCapacity { get; set; } = ""; public List<string> Keywords { get; set; } = new(); }
// Key (KEYM): value/weight + keywords.
public sealed class KeySpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<string> Keywords { get; set; } = new(); }
// Keyword (KYWD): just an editorId — define your own so in-spec records can reference it in
// their `keywords` lists (e.g. a custom "VendorItemFood" category).
public sealed class KeywordSpec { public string EditorId { get; set; } = ""; }
// Outfit (OTFT): a named set of item *refs* (armors/weapons) an NPC can wear; an npc `outfit`
// ref can point at an in-spec outfit's editorId.
public sealed class OutfitSpec { public string EditorId { get; set; } = ""; public List<string> Items { get; set; } = new(); }
// Static (STAT): a world mesh — just `model` (a .nif path; reference a vanilla mesh in the BSA).
// A placement base for scenery; no Name (statics are nameless).
public sealed class StaticSpec { public string EditorId { get; set; } = ""; public string Model { get; set; } = ""; }
// Activator (ACTI): an interactable world object — name + `model` + keywords (+ a script via
// `scripts`). A placement base you can walk up to / attach behaviour to.
public sealed class ActivatorSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Model { get; set; } = ""; public List<string> Keywords { get; set; } = new(); }
