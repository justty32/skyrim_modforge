# ModForge spec — in-world population macros (skill trees, settlements, living NPCs)

← [index](SPEC-index.md) · cells, placements & navmesh → [SPEC-world](SPEC-world.md) · lighting →
[SPEC-lighting](SPEC-lighting.md)

High-level *macro* spec sections that expand into the low-level records every other section
already covers — an in-world clickable skill tree, a populated settlement with daily routines, and
living-world NPCs who roam and get talked about. `skillTrees` and `settlements` are pure data
expansion (no new runtime script); `livingNpcs` ships two reusable `.pex`.

---

## in-world skill trees (`skillTrees`)

A **clickable, in-world perk tree** — floating star nodes the player walks up to and activates to
spend points and learn abilities, with prerequisite gating and lit-up visual feedback. **Zero
external-mod dependency** (only `Skyrim.esm`); IN-GAME CONFIRMED. `skillTrees` is a high-level
*macro*: the generator expands it into the low-level records (per-node rank globals, a shared points
global, node + connector-line activators, their placements, and the `MFSkillNode` script wiring) —
the same records a hand-authored tree would use.

```jsonc
"skillTrees": [
  { "editorId": "MFForgeTree", "name": "Forge Mastery",
    "cell": "Skyrim.esm:0x01605E",                 // where it lives (vanilla interior or in-spec cell)
    "origin": { "x": -49, "y": -504, "z": 110 },   // world pos of the ROOT (bottom) node
    "spacing": 65,                                  // vertical gap; 65 = the line mesh's native fit
    "startingPoints": 3,                            // points the player starts with
    "nodes": [                                      // ORDERED bottom→top; node[i] gated on node[i-1]
      { "editorId": "Resolve", "name": "Forged Resolve", "ability": "MFGen_Node0Ability" },
      { "editorId": "Vigor",   "name": "Forged Vigor",   "ability": "MFGen_Node1Ability" },
      { "editorId": "Mastery", "name": "Forged Mastery", "ability": "MFGen_Node2Ability" }
    ] }
],
"assets": "assets/skilltree"                        // bundle the star/line meshes (see below)
```

In-game: the player activates a node → if its prerequisite is owned and a point is available, the
node's `ability` is added to the player, the star lights up, the connector line lights, and a point
is spent. Re-activating a learned node, or one whose prerequisite isn't met, is refused with a
notification.

**Fields** (`skillTrees[]`): `editorId` (prefixes all generated ids), `name`, `cell` (in-spec
interior editorId **or** vanilla `"<master>:0xFORMID"`), `origin` (Vec3, the root node's position),
`spacing` (default 65), `pointsGlobal` (existing GLOB to drive the pool from elsewhere — empty
auto-creates `<editorId>_Points` seeded with `startingPoints`), `startingPoints` (default 3),
`nodeModel` / `lineModel` (Data-relative mesh overrides), and `nodes`.
**Node** (`nodes[]`): `editorId` (unique in the tree), `name` (activate prompt + notification),
`ability` (a SPEL ref — usually an in-spec `spells[]` ability, or vanilla — granted on learn).

**Abilities are yours.** A node references an `ability` you define in `spells[]`/`magicEffects[]`
(or a vanilla SPEL). The tree drives the *learning UX*; the *effect* is an ordinary ability.

**Art (no Campfire install).** The default node/line meshes are Campfire's star/line nifs — but they
are NOT a master dependency: bundle the kit (the two `.nif` + their all-vanilla textures) as loose
files via `assets` (provided at `examples/assets/skilltree`). Override `nodeModel`/`lineModel` to use
your own meshes. The `MFSkillNode.pex` (node behaviour) ships automatically with `package`.

**MVP scope.** A **vertical linear chain** (nodes stacked, each gated on the one below, connected by
vertical lines) — the IN-GAME-CONFIRMED layout. Branching / free 2-D layouts are a future extension
(diagonal connector orientation needs calibration). Worked example: `examples/skill_tree_spec.json`
(the generator) vs `examples/inworld_skill_tree_standalone_spec.json` (the same result hand-authored).

## populated settlements (`settlements`)

A **populated settlement** — named residents who LIVE in a cell, each with a sleep/work/wander daily
routine bound to placed anchor refs, optional shop, and shared faction. `settlements` is a high-level
*macro* (like `skillTrees`): the generator expands each resident into the low-level records every
build pass already handles — an ACHR placement, 2–3 schedule packages, faction membership, and (for a
shopkeeper) a vendor FACT + a placed merchant chest. **No new record type, no runtime script** — pure
data expansion, so it is fully verifiable offline. It turns ~100 hand-authored records (10 residents ×
packages + factions + vendors + placements) into a dozen lines.

```jsonc
"settlements": [
  { "editorId": "MFV_Riverwatch",
    "cell": "MFV_RiverwatchInterior",          // in-spec cell editorId OR vanilla "<master>:0xFORMID"
    "settlementFaction": "",                    // empty → auto "<editorId>_Faction" every resident joins
    "crimeFaction": "Skyrim.esm:0x0267EA",      // optional → each resident's CrimeFaction (city-traversal)
    "friendlyResidents": true,                  // optional → pairwise Friend RELA between residents (default off)
    "dailyRoutine": {                           // settlement default; a resident's `routine` overrides per-window
      "sleep": { "from": 22, "to": 7 },         // hours 0..24; a window may wrap midnight (from > to)
      "work":  { "from": 8,  "to": 18 }
    },
    "residents": [
      { "npc": "MFV_Brelin",                    // ref → an existing npcs[] editorId (the resident)
        "home":    "MFV_BrelinBed",             // ref → a placed bed/marker REFR (Sleep anchor)
        "work":    "MFV_BrelinForge",           // ref → a placed workstation/marker REFR (Work anchor); optional
        "spawnAt": "MFV_BrelinSpawn",           // ref → a placed XMarker (ACHR spawns at its coords)
        "vendor": { "sellBuyList": "Skyrim.esm:0x06CB48", "notSellBuyList": true,
                    "startHour": 9, "endHour": 18, "gold": 500 } },
      { "npc": "MFV_Millie", "home": "MFV_MillieBed", "spawnAt": "MFV_MillieSpawn",
        "routine": { "sleep": { "from": 21, "to": 6 } } }   // override just the sleep window
    ] }
]
```

**Each resident expands to:** an **ACHR placement** at the spawn marker's coords (or `spawnPosition`
fallback) in the settlement cell; a **Sleep package** (anchored at `home`) gated on the sleep window; a
small-radius **Sandbox "work" package** (anchored at `work`) gated on the work window — emitted only if
a `work` anchor is given; an always-on large-radius **Sandbox "wander" package** (lowest priority); and
faction membership. With `vendor`, a Vendor-flagged FACT (the resident joins it, plus the engine's
`JobMerchantFaction`) + a placed merchant chest holding `gold`. Packages are ordered by schedule hour
(wander last) — vanilla package precedence.

**Anchor philosophy.** `home`/`work`/`spawnAt` are editorIds of refs YOU place (in the Godot editor or
`placements[]`). The macro only BINDS packages to them — it never conjures abstract sandbox points (a
purely abstract sandbox = an NPC standing idle). Place a bed/forge/marker, give it an editorId, and the
routine wires to it. (Sleep actively searches for a real bed near its anchor — in a bare custom cell
with no bed furniture the NPC won't lie down; place a vanilla bed near the `home` anchor, or build in a
vanilla cell that already has beds.)

**Fields** (`settlements[]`): `editorId`, `cell`, `settlementFaction` (empty → auto-create),
`crimeFaction`, `dailyRoutine` (`sleep`/`work` each `{from,to}` hours), `friendlyResidents` (default
false), `residents`. **Resident** (`residents[]`): `npc` (in-spec npcs[] ref), `home`/`work`/`spawnAt`
(placed-ref editorIds), `spawnPosition` (Vec3 fallback when no `spawnAt`), `vendor`
(`sellBuyList`/`notSellBuyList`/`startHour`/`endHour`/`gold`), `routine` (per-resident override).

**MVP scope.** Named residents + static ACHR + anchored routine + optional vendor (the deterministic,
offline-verifiable quadrant). **Phase 2** (not yet): `crowd:` anonymous masses (leveled-static or a
spawn-controller `.pex`), `reaction: flee|fight` (needs a `flee` PACK template), inline npc, advanced
per-weekday/seasonal routines. Worked example: `examples/settlement_spec.json`.

## living-world NPCs (`livingNpcs`)

A small cast of **named, persistent NPCs who live their own off-stage lives** — an adventurer taking
contracts, a College apprentice, a merchant on the road — whom the player keeps bumping into across the
world, with the tavern gossiping about their deeds. Unlike `settlements` (residents anchored to one
cell), a living NPC roams: the engine can't simulate an off-screen actor, so `livingNpcs` runs the
canonical **abstract ghost-sim + materialize-on-co-location** loop. It is a macro that expands into a
controller quest + per-NPC wiring AND ships two reusable `.pex` (so it DOES carry a runtime script,
unlike `settlements`). **The product is the on-ramp: adding one NPC of an existing archetype is one
small entry — a ref, an archetype, a few anchors.**

```jsonc
"livingNpcs": {
  "simIntervalHours": 2,                         // in-game hours between off-stage "deeds" (the sim tick)
  "pollInterval": 5,                             // real seconds between presence checks
  "rumorSpeaker": "MFLN_Bard",                   // optional npc (or "<master>:0xFORMID") who voices the 傳唱
  "npcs": [
    { "ref": "MFLN_Kjeld",                       // in-spec npcs[] editorId (placed+forced) OR external follower "Mod.esp:0xID" (uniqueActor)
      "name": "Kjeld the Wanderer",              // labels the rumor topic prompt
      "archetype": "adventurer",                 // adventurer|mageApprentice|merchant|herbalist|priest|bandit
      "alignment": "friendly",                   // friendly|neutral|hostile (Phase-2 parley; recorded now)
      "backstory": "A mercenary who left the war…",
      "anchors": [                               // the vanilla cells he appears in; rotates through them
        { "cell": "Skyrim.esm:0x0133C6", "position": { "x": -300, "y": 250, "z": 0 }, "kind": "inn" },
        { "cell": "Skyrim.esm:0x01605E", "position": { "x": 250, "y": 120, "z": 0 }, "kind": "inn" }
      ],
      "rumors": [ "Kjeld cleared another barrow single-handed, they say." ] }
  ]
}
```

**The section expands to:** one StartGameEnabled controller quest carrying `MFLivingWorldController`
(one game-time tick + one real-time presence poll over the whole roster — cost does NOT scale per-NPC);
one shared off-stage hold marker + one shared "sandbox where I am" package. **Each NPC expands to:** a
reference alias on the controller quest carrying `MFLivingNpcAlias` (`Archetype`/`HoldMarker`/`Anchors`/
`DeedCount`), forced-filled to a placed ACHR (in-spec) or `uniqueActor` (external follower — *give that
gorgeous standalone follower a life*); one xmarker per anchor + an Anchors FormList; a deed
GlobalVariable; and — when the section has a `rumorSpeaker` and the NPC has `rumors` — a 傳唱 topic
gated on the deed global (`GetGlobalValue >= 1`).

**How it works.** Off-stage, the controller ticks each NPC's deed global and rotates which anchor he
"is" at — no actor processed. When the player enters a cell matching that NPC's current anchor, his ONE
persistent ref is `MoveTo`'d on-stage and `EvaluatePackage`'d (so the sandbox package kicks in); when
the player leaves, he's sent back to the hold marker. Named cast ⇒ one persistent ref each, MoveTo
in/out — **no LVLN spawn churn, no duplicates**.

**archetype = a fixed branch** in `MFLivingNpcAlias.psc`. Adding an NPC of an *existing* archetype is
pure data (one more entry). Adding a *new* life-type means extending the script's switch (occasional).

**Player interactions & alignment (`interactions`, `alignment`).** Talking to a living NPC can offer
interactions, each a dialogue topic that adjusts a per-NPC **favor global** (`MFLiving_<tag>_Favor`) —
the relationship-memory substrate future content gates on. Kinds: `fund` (give coin, favor +1),
`praise` (compliment their deeds, +1, gated on deed ≥ 1), `parley` (de-escalate / try to understand,
+5 — for a neutral or hostile NPC). `alignment` (`friendly`/`neutral`/`hostile`) is recorded; a
**hostile in-spec** NPC is set `Aggression=Aggressive` (the bandit genuinely fights — anchor him at a
camp, not an inn). External-follower refs keep their own AI (the macro only adjusts in-spec NPCs).

**Fields** — section: `simIntervalHours`, `pollInterval`, `rumorSpeaker`, `npcs`. **livingNpc**: `ref`
(required), `name`, `archetype`, `alignment`, `backstory`, `anchors` (≥1 to ever appear), `rumors`,
`interactions`. **anchor**: `cell` (required), `position`, `kind` (label). Compiling the `.pex` (and the
interaction `setGlobal` TIF fragments) needs a Papyrus machine (`package` ships them; the build embeds
conditionally). Worked example: `examples/living_npcs_spec.json`.

**MVP scope.** Named cast + abstract sim + materialize + rumor + interaction/favor + alignment.
**Phase 3.5+** (not yet): hire-as-follower, surfacing parley on a hostile-in-combat NPC (needs a
non-combat approach mechanic), real missive task targets (needs roadmap #7–9 LocationAlias fill), the
controller reading favor/alignment to change behaviour, LAL origin-seeded relationships, an anonymous
"crowd" tier. Design: `sub_projs/living-adventurers/` (idea #23 + design.md).
