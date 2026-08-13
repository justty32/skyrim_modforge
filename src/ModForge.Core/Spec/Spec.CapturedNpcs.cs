namespace ModForge;

// --- Captured NPCs (Idea #24 addendum — the in-game "definition eyedropper", 2026-07-11) -------
// The scene-capture-bridge DLL's `sc cap` mode reads a live actor's TESNPC appearance/identity and
// exports it as a `capturedNpcs[]` entry (shape below = SceneExporter.cpp verbatim). ModForge
// macro-EXPANDS each entry into an ordinary NpcSpec (+ an ACHR PlacementSpec at the capture spot)
// so the battle-tested NPC build/wire passes do the real work. See Generator.ExpandCapturedNpcs.
//
// What is consumed: identity (race/female/unique/essential/protected/outfit/perks/class/level/
// equipped) + the full TESNPC face/body recipe (weight/height/bodyTint/hairColor/faceTexture/
// headParts/tintLayers/faceMorphs/faceParts). What rides along UNCONSUMED (advisory, kept so
// nothing is silently dropped): `base` (the origin NPC_ — we always MINT, never override),
// `dead` (an ACHR "starts dead" concept, not a TESNPC field), `activeEffects` (a runtime buff
// snapshot, not a durable trait), `hairColor.r/g/b` (the CLFM record itself carries the colour)
// and `perks[].rank` (wiring uses each perk's own NumRanks). Faces render gray/dark until the
// FaceGeom/facetint baking milestone (plan Phase 2) — identity/body/hair/skin are correct now.
public sealed class CapturedNpcSpec
{
    public string Name { get; set; } = "";        // display name at capture time (also seeds the editorId)
    public string EditorId { get; set; } = "";    // optional explicit editorId; auto-derived from name+index if empty
    public string Base { get; set; } = "";        // origin NPC_ ref (advisory — Q2: mint-only, never override)
    public string Race { get; set; } = "";        // ref → RACE (required — an NPC_ without a race is broken in-game)
    public bool Female { get; set; }
    public bool Unique { get; set; }
    // This entry IS the player character (`sc capp`, or a `sc capc` that landed on the player;
    // DLL judges it off the actor's PlayerCharacter cast — co-save SCCP v9). Advisory identity
    // flag ONLY: the user decided 2026-07-12 "as-captured, no fallback" — ModForge does NOT
    // invent a voiceType for a player capture. It exists purely so the build can WARN (not fail)
    // when a player capture has no voiceType, instead of silently shipping a mute clone. See
    // Generator.ExpandCapturedNpcs (carries to NpcSpec.IsPlayer) and BuildNpcs (the warning).
    public bool IsPlayer { get; set; }
    public bool Essential { get; set; }
    public bool Protected { get; set; }
    public bool Dead { get; set; }                 // advisory (not consumed; see header)
    public float Weight { get; set; }              // 0–100
    public float Height { get; set; }              // scale multiplier (DLL always emits; 1.0 = default)
    public ColorSpec? BodyTint { get; set; }       // {r,g,b} skin tint → QNAM
    public CapturedHairColorSpec? HairColor { get; set; }  // {id,r,g,b} — id → CLFM ref; rgb advisory
    public string FaceTexture { get; set; } = ""; // ref → TXST (FTST)
    public string DefaultOutfit { get; set; } = ""; // ref → OTFT
    public List<string> HeadParts { get; set; } = new();       // refs → HDPT
    public List<TintLayerSpec> TintLayers { get; set; } = new();
    public List<float> FaceMorphs { get; set; } = new();       // 18 floats (idx 0–17) or empty
    public List<int> FaceParts { get; set; } = new();          // 4 ints or empty
    public List<CapturedNpcPerkSpec> Perks { get; set; } = new();
    public string Class { get; set; } = "";        // ref → CLAS; drives autoCalcStats ONLY when no explicit stats came along (see below)
    public int Level { get; set; }                  // actor's effective level at capture time (0 = unknown → engine default)
    // EXPLICIT stats (DNAM) — the base actor values the engine really runs on, captured off the
    // live actor. They BEAT class autocalc: when any of these is present the expansion writes them
    // to DNAM and leaves autoCalcStats OFF (autocalc only ESTIMATES H/M/S from class+level, and a
    // PROTEUS-style clone reports a flat level-1 50/50/50). 0 = not captured → the class-autocalc
    // route, exactly as before (so a pre-v8 capture json behaves identically).
    public float Health { get; set; }
    public float Magicka { get; set; }
    public float Stamina { get; set; }
    // The 18 skills in engine ActorValue order 6..23 (OneHanded, TwoHanded, Archery, Block,
    // Smithing, HeavyArmor, LightArmor, Pickpocket, Lockpicking, Sneak, Alchemy, Speech,
    // Alteration, Conjuration, Destruction, Illusion, Restoration, Enchanting) — which is exactly
    // Mutagen's `Skill` enum order, so index → Skill is 1:1. Empty (pre-v8 capture) or 18.
    public List<int> Skills { get; set; } = new();
    public string CombatStyle { get; set; } = "";  // ref → CSTY; HOW the AI fights (without a magic-leaning one, spells go uncast)
    public string VoiceType { get; set; } = "";    // ref → VTYP; without one the clone is mute (no hello/idle chatter)
    public List<string> Spells { get; set; } = new(); // refs → SPEL; base spell list + runtime-added — the combat AI's castable set
    // The actor's full carry (durable refs), split by consumption route — the engine only
    // auto-WEARS armour that comes from an OUTFIT (inventory armour stays in the pocket;
    // in-game confirmed on the boots-in-pocket clone), while weapons auto-equip from inventory.
    // So worn armour MINTS an in-spec OTFT (replacing defaultOutfit, which for a PROTEUS clone
    // is a runtime shell that's empty on disk) and `inventory` rows (weapons/staves/food/
    // potions/gold…) become NpcSpec.Items with their counts.
    public List<string> EquippedArmor { get; set; } = new();
    public List<CapturedNpcItemSpec> Inventory { get; set; } = new();
    // Legacy pre-split shapes: `equipped` (one mixed list) folds into armour; `equippedWeapons`
    // (the brief v5 shape) folds into inventory at count 1.
    public List<string> Equipped { get; set; } = new();
    public List<string> EquippedWeapons { get; set; } = new();
    public List<CapturedActiveEffectSpec> ActiveEffects { get; set; } = new(); // advisory (not consumed)
    public Vec3 Position { get; set; } = new();
    public Vec3 Rotation { get; set; } = new();    // degrees (DLL converts)
    public string Cell { get; set; } = "";        // interior anchor ref — exactly one of cell/worldspace is set
    public string Worldspace { get; set; } = "";  // exterior anchor ref
    public string Note { get; set; } = "";        // free-form capture-time note. Inert documentation only — Generator.ExpandCapturedNpcs never reads this
}

// One carried-inventory row: a durable item ref + stack count (green apples ×3, gold ×250…),
// plus the item INSTANCE's enchantment when the DLL found one on the entry's extra data (a
// player-crafted staff/armour enchant lives on the instance, not the base). `worn: true` marks
// worn armour (→ the outfit route). An enchanted row expands into a minted WEAP/ARMO template
// clone (reusing the capturedItems enchant machinery) with `name` as its display name.
public sealed class CapturedNpcItemSpec
{
    public string Item { get; set; } = "";
    public int Count { get; set; } = 1;
    public bool Worn { get; set; }
    public string Name { get; set; } = "";              // instance display name (enchanted rows)
    public CapturedEnchantSpec? Enchantment { get; set; } // target + durable base | captured effects
}

// The DLL exports hairColor as an object: the durable CLFM ref plus its resolved RGB. Only the ref
// is consumed (the CLFM record carries the colour); rgb is advisory context for a human reader.
public sealed class CapturedHairColorSpec
{
    public string Id { get; set; } = "";   // ref → CLFM
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
}

// A perk on the captured actor's base. Only `perk` is consumed — NPC perk wiring applies each
// perk at its record's own NumRanks, so a multi-rank perk captured mid-rank applies fully
// (documented limitation; vanilla NPC perks are almost all single-rank).
public sealed class CapturedNpcPerkSpec
{
    public string Perk { get; set; } = "";  // ref → PERK
    public int Rank { get; set; } = 1;
}

// A live active-effect snapshot row (current buffs at capture time). Advisory only — runtime
// state, not a durable trait; kept typed so the DLL's json round-trips without silent data loss.
public sealed class CapturedActiveEffectSpec
{
    public string MagicEffect { get; set; } = "";  // ref → MGEF
    public string Source { get; set; } = "";       // ref → the spell/enchant that applied it (may be empty)
    public float Magnitude { get; set; }
    public float Duration { get; set; }
    public float Elapsed { get; set; }
}
