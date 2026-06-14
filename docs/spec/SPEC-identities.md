# ModForge spec — identities (lightweight class/identity system)

← [index](SPEC-index.md) · dialogue → [SPEC-dialogue](SPEC-dialogue.md) · quests → [SPEC-quests](SPEC-quests.md)


## Identities (lightweight class/identity system)

Give the player roles (Paladin, Merchant, Adventurer…) that grant standing abilities and change how
NPCs treat them. Each identity is stored as a **faction** (the persistent "you have it" signal —
save-safe, and future-proofs vanilla `GetInFaction` gating). Add an `identities[]` list:

```jsonc
"identities": [
  { "id": "Paladin", "faction": "MF_FactPaladin", "priority": 30,
    "grants": ["MF_AbilSmite"],                 // SPELs added on join, removed on leave
    "acquireBook": "MF_PaladinTome",            // reading it joins the faction (MFIdentityBook OnRead)
    "onAcquire": { "scene": "MF_OathScene" },   // optional performance played on acquire (e.g. a PlayIdle bow)
    "activeWhen": [                             // only "active" while wearing heavy armor
      { "function": "WornHasKeyword", "param": "Skyrim.esm:0x06BBD2", "comparison": "==", "value": 1 } ] },
  { "id": "Merchant", "faction": "MF_FactMerchant", "priority": 20, "toggle": true,
    "acquireBook": "MF_MerchantLedger" },       // toggle: reading again leaves the identity
  { "id": "Adventurer", "faction": "MF_FactAdventurer", "priority": 0, "default": true } // baseline, no book
]
```

- **`faction`** — a bare in-spec editorId is auto-built as a plain FACT; an external `<master>:0xID`
  (a vanilla / Sofia faction) is used as-is.
- **`acquireBook`** — a `books[]` entry; the build attaches the reusable `MFIdentityBook` script
  (OnRead → AddToFaction + AddSpell(grants) + optional `AcquireScene.Start()`; `toggle` reverses it).
  `package` ships the prebuilt `MFIdentityBook.pex`. **Iron law:** the book script `extends
  ObjectReference` (OnRead is an ObjectReference event — `extends Book` never fires it).
- **`default: true`** — every player holds this identity from game start, with no book. The build adds
  a StartGameEnabled quest (`MFIdentityDefault`, OnInit) that joins the player to each default faction +
  grants its abilities (idempotent; lands in the `.seq` so existing saves fire too). Use for a baseline
  identity (e.g. Adventurer). `package` ships `MFIdentityDefault.pex`.
- **`autoGrantWhen: { actorValue, threshold }`** — auto-join the identity's faction once the player's
  ActorValue reaches the threshold (a StartGameEnabled poll controller `MFIdentityAutoGrant` reads
  `GetActorValue(name) >= threshold` — vanilla, no SKSE). E.g. Dragonborn on `{ "actorValue":
  "DragonSouls", "threshold": 1 }` (your first absorbed dragon soul). Grants the **faction signal only**
  (greetings/gates then apply; abilities/perks aren't added by this trigger). `package` ships
  `MFIdentityAutoGrant.pex`.
- **`activeWhen`** — a list of CTDA that NARROW when the identity counts as *active* (e.g.
  `WornHasKeyword(heavy armor)`, `GetBaseActorValue(Speech)>=X`, `GetRelationshipRank(npc)>=Y`). Each
  runs on the **player** by default. Appended to the positive `identity`/`primaryIdentity` gate — so a
  held-but-inactive identity's greeting won't fire. (It does **not** participate in primary exclusion —
  a negated condition bundle isn't CTDA-expressible — so an inactive higher identity falls back to the
  plain greeting until the player overrides the primary; see below.)
- **`grants[]`** — SPELs (e.g. a constant-effect Fortify ability) added on join, removed on leave.
- **`grantPerks[]`** — PERKs added on join, removed on leave (e.g. a conditional "smite vs undead" perk —
  a `ModAttackDamage` entry point gated on the target's `ActorTypeUndead`/`ActorTypeDaedra` keyword). The
  acquire book grants the first; a `default` identity grants all. (The perk CTD tab-count byte is set
  automatically — see the perks section.)

**Gate** dialogue with two `DialogueSpec` tags. `identity: "Paladin"` shows the line only while the
player holds that identity (`GetInFaction ≥ 1`, plus the identity's `activeWhen`). `primaryIdentity:
"Paladin"` shows it only while Paladin is the player's **current primary identity** — resolved at
runtime by a controller quest (`MFIdentityController`, built whenever any dialogue uses primaryIdentity
or `setPrimaryIdentity`): it maintains a `MF_PrimaryIdentity` global = the manual override (if held)
else the highest-`priority` held identity, and the greeting reads `GetGlobalValue(MF_PrimaryIdentity)
== <code>`. `package` ships `MFIdentityController.pex`. State-varying greetings should be one Hello
topic with several conditioned INFOs (`hello: true`, ordered specific-first), not separate topics.

**Manual override** — a player topic with `setPrimaryIdentity: "Merchant"` (or `"auto"` to clear) makes
NPCs treat the player as that identity regardless of priority (a TIF fragment sets `MF_IdentityOverride`;
the controller reflects it on its next poll). Pair with an `identity` gate so the option only appears
while the player holds it. This also resolves the held-but-inactive `activeWhen` gap.

**Identity-linked interactions** — gate any interaction on `identity`. Two built-in dialogue result
actions (generated TIF fragments, no per-mod script): **`openBarter: true`** opens the trade menu with
the speaking NPC (`Actor.ShowBarterMenu()`; the NPC must be a vendor-faction member with a merchant
chest) — e.g. a Merchant-only "let's talk shop"; **`rewardItem` + `rewardCount`** give the player gold/an
item (`AddItem`) — e.g. an escort reward. `evaluateSpeakerPackages: true` forces the speaker to
re-evaluate AI packages so a `setStage`-gated follow/escort package activates immediately. An **escort
quest** is then pure record data: a quest with stages, a Follow PACK conditioned on `GetStage==N`, and
Adventurer-gated start/finish dialogue (see below).

A scene started on acquire (`onAcquire.scene`) is **explicitly** `Start()`'d by the book — set the
scene's `beginOnQuestStart: false` so it doesn't also auto-play at game load (`Start()` is the sole
trigger; the begin-condition dance is fragile). NPCs with `autoCalcStats` must have a `class` or they
spawn at ~0 HP. See `examples/identity-paladin.json` for the full showcase (acquire + grant + oath
scene + identity greetings + merchant toggle + `activeWhen` + manual override + merchant trade + escort
quest). Inspect a built plugin's identity wiring with `identitydiag <plugin>`. **Out of scope:**
reputation/behaviour tracking, Dragonborn-on-first-shout, conditional smite tuning.
