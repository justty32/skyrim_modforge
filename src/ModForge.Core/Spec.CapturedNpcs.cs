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
    public string Class { get; set; } = "";        // ref → CLAS; when present the expansion turns on autoCalcStats (class+level drive believable H/M/S)
    public int Level { get; set; }                  // actor's effective level at capture time (0 = unknown → engine default)
    // Worn armour + held weapons/torch (durable refs). Consumed as NpcSpec.Items (best gets
    // auto-equipped) — and when non-empty the expansion SKIPS defaultOutfit: a PROTEUS clone's
    // outfit is a runtime shell (empty on disk), while the equipped list is what it actually wore.
    public List<string> Equipped { get; set; } = new();
    public List<CapturedActiveEffectSpec> ActiveEffects { get; set; } = new(); // advisory (not consumed)
    public Vec3 Position { get; set; } = new();
    public Vec3 Rotation { get; set; } = new();    // degrees (DLL converts)
    public string Cell { get; set; } = "";        // interior anchor ref — exactly one of cell/worldspace is set
    public string Worldspace { get; set; } = "";  // exterior anchor ref
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
