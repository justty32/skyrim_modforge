namespace ModForge;

// --- Actors: NPCs, factions, and the relationships between them -------------------------

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
    public string VoiceTemplate { get; set; } = "";   // ref → voiceTemplates ID; routes this NPC's lines to a cloning engine
    public string CrimeFaction { get; set; } = "";   // ref → FACT (e.g. Skyrim.esm:0x0267EA = CrimeFactionWhiterun); marks the NPC as a member of a city's crime/citizen circle — grants city-traversal rights (without it, cross-cell Travel through city gates is silently rejected)
    public bool Unique { get; set; }                  // Configuration.Flag.Unique — engine treats the actor as a one-off (vs leveled spawn); seems to matter for AI tracking + cross-cell travel
    public bool Essential { get; set; }               // Configuration.Flag.Essential — cannot be killed (drops to bleedout then recovers); use for a non-lethal brawl or a plot-critical NPC
    public bool Protected { get; set; }               // Configuration.Flag.Protected — can only be killed by the PLAYER (other NPCs can't land the killing blow)
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
    // Greeting (Hello) line. When this NPC is the speaker of any custom `dialogue[]`, Build auto-emits
    // a Hello topic (Category=Misc, Subtype=Hello, SNAM='HELO') gated on GetIsID(this NPC). This is
    // what makes the NPC CONVERSABLE — without a Hello, activating the NPC never opens the dialogue
    // menu, so the player topics never surface (you just get voicetype mumbles). Empty => a neutral
    // default line is used so the NPC still works.
    public string Greeting { get; set; } = "";
    // Perks (PERK refs) granted to this NPC — populates npc.Perks (each as a PerkPlacement with the
    // perk's NumRanks). For an NPC, a perk's entry-point/ability effects apply passively (e.g. a
    // +damage entry-point perk makes the actor hit harder); this is how vanilla races/NPCs carry
    // their innate ability perks. Player perks are normally granted by script/AddPerk, NOT here.
    public List<string> Perks { get; set; } = new();
    // Items the NPC carries — added to its inventory; a weapon/armor here is auto-equipped if it's
    // the NPC's best, so it's how you arm an NPC; loot drops on death. (Separate from VendorSpec —
    // those are the merchant's sale categories; these are what THIS actor physically carries.)
    public List<NpcItemSpec> Items { get; set; } = new();
    // --- Appearance (the TESNPC face/body "recipe") ------------------------------------------
    // These author the RECIPE half of a Skyrim face: the record fields the engine combines with
    // baked FaceGeom/<plugin>/<formid>.nif + facetint .dds assets. ModForge writes only the recipe
    // today — a custom-faced NPC without baked assets renders with the gray/dark-face bug (body
    // shape, hair colour, skin tone and identity are still correct). Baking is a later milestone
    // (see workflows/plans/captured-npcs-consumption.md Phase 2).
    public bool Female { get; set; }                  // Configuration.Flag.Female (default: male)
    public float? Weight { get; set; }                // NAM7, 0–100 body-slider blend (0 is valid → nullable; null = engine default)
    public float? Height { get; set; }                // NAM6 scale multiplier (1.0 = default; null = leave default)
    public ColorSpec? BodyTint { get; set; }          // QNAM "texture lighting" — the skin tint (A unused)
    public string HairColor { get; set; } = "";      // ref → CLFM colour record (HCLF)
    public string FaceTexture { get; set; } = "";    // ref → TXST face texture set (FTST; Mutagen property is HeadTexture)
    public List<string> HeadParts { get; set; } = new(); // refs → HDPT (hair/eyes/scars/brows…); a modded part (e.g. high-poly head) makes that mod a master
    public List<TintLayerSpec> TintLayers { get; set; } = new(); // TINI/TINC/TINV/TIAS face paint/dirt/lip layers
    public List<float> FaceMorphs { get; set; } = new(); // NAM9 — exactly 18 floats (engine morph order; see plan's index↔field table) or empty
    public List<int> FaceParts { get; set; } = new();    // NAMA — exactly 4 ints (nose/unknown/eyes/mouth preset indices) or empty
}
// One face tint layer (TINI/TINC/TINV/TIAS): an entry of the race's tint mask list applied at a
// colour + interpolation strength. Index/preset are engine-assigned ids from the race's tint masks
// (capture them from a live NPC rather than authoring by hand); value is 0–1 strength.
public sealed class TintLayerSpec
{
    public int Index { get; set; }        // TINI — tint mask index in the race's chargen list
    public int Preset { get; set; }        // TIAS — preset entry (-1/0 when free-form)
    public float Value { get; set; }       // TINV — interpolation 0..1
    public ColorSpec? Color { get; set; }  // TINC — RGBA 0–255
}
// One inventory entry on an NPC: a ref to a carriable item (weapon/armor/misc/potion/gold/ammo —
// vanilla <master>:0xFORMID or an in-spec editorId) plus a Count (defaults to 1; use larger counts
// for stacks like gold/arrows/potions). Resolved forward-ref-safe in pass 2 (an in-spec weapon may
// be declared after the NPC). Gold001 = Skyrim.esm:0x00000F.
public sealed class NpcItemSpec
{
    public string Item { get; set; } = "";   // ref → carriable item (REQUIRED)
    public int Count { get; set; } = 1;        // stack count (default 1; must be != 0)
}
// Faction (FACT): a named group an NPC can belong to. `vendor` (optional) turns this into a
// MERCHANT faction — the engine treats any NPC who is a member of a Vendor-flagged faction (with
// vendor hours + a merchant container) as a shopkeeper, and the vanilla generic "I'd like to
// trade" service topic (DialogueGeneric.OfferServicesTopic, gated on GetInFaction JobMerchantFaction
// + GetOffersServicesNow) surfaces on talking to them. See VendorSpec.
public sealed class FactionSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public VendorSpec? Vendor { get; set; } }
// Vendor data on a FACT (VENV/VEND/VENC/CITC subrecords). When present, the faction gets the
// Vendor flag + VendorValues + (optional) VendorBuySellList + MerchantContainer, exactly like a
// vanilla merchant faction (e.g. ServicesWhiterunBelethorsGoods 0x09CAF5). An NPC becomes a working
// shopkeeper by (a) being a member of this faction (npc.factions) and (b) being in JobMerchantFaction
// (Skyrim.esm:0x051596) so the generic trade topic's GetInFaction condition matches — Build adds
// JobMerchantFaction automatically to any NPC in an in-spec vendor faction.
//
// `sellBuyList` is a FormList REF (VendorItemX keyword list — reference a vanilla one such as
// VendorItemsMisc Skyrim.esm:0x06CB48, or build your own FormList... not yet an in-spec record, so
// use a vanilla list). `notSellBuyList=false` ⇒ the list names the categories the vendor TRADES;
// `notSellBuyList=true` ⇒ the list is a NOT-sell list (vendor trades everything EXCEPT those — the
// Belethor "general goods" pattern). `merchantContainer` is a ref to a PLACEMENT editorId whose base
// is a Container holding the vendor's gold (+ optional leveled stock) — that's the merchant chest.
public sealed class VendorSpec
{
    public ushort StartHour { get; set; } = 8;   // vendor opens (0..24)
    public ushort EndHour { get; set; } = 20;     // vendor closes (0..24)
    public ushort Radius { get; set; }            // how far the player may stray from the merchant and still trade (0 = engine default)
    public bool BuysStolen { get; set; }          // OnlyBuysStolenItems — a fence (false for a normal shop)
    public string SellBuyList { get; set; } = ""; // ref → FormList of VendorItem keywords (vanilla, e.g. Skyrim.esm:0x06CB48 VendorItemsMisc); empty = trade nothing-by-list (relies on notSellBuyList)
    public bool NotSellBuyList { get; set; }       // true ⇒ sellBuyList is a NOT-sell list (sell everything except those categories)
    public string MerchantContainer { get; set; } = ""; // ref → a placement editorId (the placed Container REFR = the merchant chest with gold + stock)
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
