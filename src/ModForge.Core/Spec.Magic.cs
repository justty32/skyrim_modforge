namespace ModForge;

// --- Magic & combat profiles: spells, magic effects, classes, combat styles -------------

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
    // EquipType (EQUP) ref — which slot the spell occupies. Build DEFAULTS castable types (Spell/Voice/
    // Power/LesserPower) to EitherHand (Skyrim.esm:0x013F44) when this is omitted, because BOTH hand
    // spells AND Voice/shout charge-spells need an equip slot (vanilla shout word-spells all use
    // EitherHand). Set this only to override (BothHands/Left/RightHand). Without an EQUP an NPC can't
    // equip a hand spell, and the player learns a shout but can't actually shout it.
    public string EquipType { get; set; } = "";
}
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
    // DualValueModifier archetype only: a SECOND affected AV. The effect's single magnitude is split
    // between the primary AV and this one by `secondActorValueWeight` (0 = all to primary). Vanilla
    // example: an Absorb effect damages one AV and feeds another (78 vanilla MGEF carry a 2nd AV).
    public string SecondActorValue { get; set; } = "";
    public float SecondActorValueWeight { get; set; }
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
    public string HitShader { get; set; } = "";         // EFSH — shader applied to the hit target
    public string EnchantShader { get; set; } = "";     // EFSH — shader while the effect/enchantment persists
    public string Explosion { get; set; } = "";          // EXPL — AoE explosion on impact
    // Sounds (SNDD) — per-phase sound descriptors. The big one for a SHOUT is `Release`: the Thu'um
    // VOICE the player yells (e.g. VOCShoutFX… for "FUS RO DAH"). Without it a shout fires silently
    // even with a projectile. Other phases: SheathDraw / Charge / Ready / ConcentrationCastLoop / OnHit.
    public List<MagicEffectSoundSpec> Sounds { get; set; } = new();
    // Inline Papyrus script attach (I組 DX): scripts attached to THIS MGEF's VMAD (e.g. an effect that
    // runs Papyrus on the target via OnEffectStart). Same shape as the top-level `scripts[]`, but the
    // `targetEditorId` is implied (this effect) and ignored — keeps the script definition next to the
    // record. `package` compiles each entry's `source` like any top-level script attach.
    public List<ScriptAttachSpec> Scripts { get; set; } = new();
}
// One MGEF sound: which phase + the SoundDescriptor (SNDR) ref. `type` defaults to Release (cast-out).
public sealed class MagicEffectSoundSpec { public string Type { get; set; } = "Release"; public string Sound { get; set; } = ""; }
// One magic effect on a spell/potion: a MagicEffect ref + magnitude/area/duration (EffectData).
public sealed class EffectSpec { public string MagicEffect { get; set; } = ""; public float Magnitude { get; set; } public int Area { get; set; } public int Duration { get; set; } }
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
