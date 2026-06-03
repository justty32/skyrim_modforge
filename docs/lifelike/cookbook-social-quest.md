<!-- Social & quest patterns -->
# Recipe cookbook — social & quest

← [cookbook index](cookbook-index.md) | [lifelike hub](README.md)

## "Conversational NPC" (custom player topics — IN-GAME CONFIRMED It.23)

Give the NPC a `greeting`, a host quest, and one `dialogue` entry per topic. From that the build emits
the whole vanilla chain (Quest→DialogView→Branch→Topic→INFO + a Hello), so the topic actually shows.

```jsonc
{ "quests": [ { "editorId": "MF_TalkQuest", "name": "...", "startGameEnabled": true } ],
  "npcs": [
    { "editorId": "MF_Talker", "name": "Aldric", "race": "Skyrim.esm:0x013746",
      "voiceType": "Skyrim.esm:0x013AE6",            // a real voice type — silent NPCs don't greet
      "greeting": "Welcome. What brings you here?",   // REQUIRED: emits the Hello that makes him conversable
      "factions": [ "Skyrim.esm:0x028172" ] } ],
  "dialogue": [
    { "editorId": "MF_AboutPlace", "questEditorId": "MF_TalkQuest", "speakerNpcEditorId": "MF_Talker",
      "prompt": "Tell me about this place.", "emotion": "Happy",
      "responses": [ "Everything here was forged on Linux.", "No Creation Kit needed." ] } ],
  "placements": [
    { "base": "MF_Talker", "cell": "Skyrim.esm:0x0133C6",
      "position": { "x": -350, "y": 180, "z": 0 } } ]   // a REAL in-room coord, NOT (0,0,0)
}
```

Three things that each independently make it silently fail (all are now handled by the build except
the last two, which are yours to get right) — see [gotchas.md](gotchas.md):
- **`greeting`** must be set, or the NPC isn't conversable (no dialogue camera). The build turns it into
  a Hello info; the topic chain (SNAM='CUST', DialogView, INFO ENAM/CNAM) is emitted automatically.
- **Placement** must be a real in-room coordinate — a no-package NPC at `(0,0,0)` lands off-navmesh and
  you can't reach it (you end up talking to a vanilla NPC).
- **Unvoiced lines** flash past — install **Fuz Ro D-oh** (or bundle silent `.fuz`) and turn on subtitles.

## "Working merchant" (a shopkeeper who buys + sells — IN-GAME CONFIRMED 2026-05-31)

A vendor = a **Vendor-flagged FACT** (trade hours + a buy/sell category list + a merchant chest with
gold/stock) whose member NPC the engine treats as a shopkeeper. Make the NPC conversable and Build
auto-adds `JobMerchantFaction`, so the vanilla generic "I'd like to trade" topic surfaces — **but
only when `GetOffersServicesNow` returns 1**, which has two non-obvious requirements for a generated
NPC (the merchant must STAND on-duty in its shop, and the faction must name a sell-area CELL — see
below). With those, the barter menu opens in-game.

```jsonc
{ "factions": [
    { "editorId": "MF_ShopFaction", "name": "ModForge General Goods",
      "vendor": {
        "startHour": 8, "endHour": 20, "buysStolen": false,
        "sellBuyList": "Skyrim.esm:0x06CB48",     // VendorItemsMisc (a VendorItem-keyword FormList)
        "notSellBuyList": true,                    // NOT-sell list -> trades everything except those (general goods)
        "merchantContainer": "MF_ShopChestRef" } } ],   // -> the placed chest (below)
  "containers": [
    { "editorId": "MF_ShopChest", "name": "Merchant Chest",
      "items": [ { "item": "Skyrim.esm:0x072AE7", "count": 1 },     // VendorGoldMisc (the vendor's gold)
                 { "item": "Skyrim.esm:0x09AF0A", "count": 10 } ] } ],  // LItemMiscVendorMiscItems75 (stock)
  "npcs": [
    { "editorId": "MF_Shopkeeper", "name": "Marcurio the Merchant", "race": "Skyrim.esm:0x013746",
      "voiceType": "Skyrim.esm:0x013AE6", "unique": true,
      "factions": [ "MF_ShopFaction" ],          // membership = "this NPC is the vendor"
      "greeting": "Looking to buy?" } ],          // REQUIRED to be conversable (auto-Hello)
  "cells": [ { "editorId": "MF_Shop", "name": "Trading Post", "template": "Skyrim.esm:0x0165A8" } ],
  "placements": [
    { "base": "MF_Shopkeeper", "cell": "MF_Shop", "position": { "x": 0, "y": 128, "z": 0 }, "persistent": true },
    { "editorId": "MF_ShopChestRef", "base": "MF_ShopChest", "cell": "MF_Shop",
      "position": { "x": 0, "y": 256, "z": 0 }, "persistent": true } ] }
```
(Full worked spec: `examples/vendor_spec.json`.)

Why each piece:
- **`merchantContainer` is a PLACEMENT ref**, not the bare container — only a placed chest holds the
  gold the engine reads. Put `VendorGoldMisc` (`0x072AE7`) in it or the vendor has no money to buy with.
- **`JobMerchantFaction` is auto-added** to any NPC in an in-spec vendor faction — the vanilla
  `DialogueGeneric.OfferServicesTopic` ("I'd like to trade") is gated on `GetInFaction
  JobMerchantFaction` + `GetOffersServicesNow`. You never emit that topic; it's universal vanilla
  dialogue that appears on any conversable vendor-faction NPC during trade hours.
- **Conversable.** No `greeting`/`dialogue` ⇒ no dialogue menu ⇒ the trade prompt can't appear
  (`validate` flags this). Same load-only rule as all dialogue (new game or save+reload).
- **The merchant must STAND on-duty in its shop** — `GetOffersServicesNow` is 0 for an NPC that isn't
  settled at its shop. Two things this needs in a **new** cell: (a) a FLOOR (a `WRIntFloorSTMid01Large`
  `0x1044AA` grid + a light) so the NPC doesn't fall into the void perpetually, and (b) an on-duty
  **Sandbox** package (`0x01C254`, NearSelf) so it stands at the counter. Without a floor the merchant
  is in a falling state and never goes on-shift — the trade option silently never appears.
- **`VendorLocation` = the merchant's CELL.** Build sets the faction's `VendorLocation` to a
  `LocationCell` pointing at the merchant-container placement's cell (radius 0). This is the sell area
  `GetOffersServicesNow` tests the player against. A CK-authored merchant has an *editor location* the
  engine falls back on; a generated NPC has none, so **omitting this (or using a chest *reference* with
  radius 0, a degenerate point) breaks the trade menu**. Verified against vanilla Belethor, whose PLVD
  is `LocationCell → WhiterunBelethorsGeneralGoods`, radius 0 — not empty. (No CK-authored "sell
  package" `0x06C872` is required; VENV radius 0 is fine — Belethor's is 0 too.)

Verify structurally: `factdiag <plugin> 0x000804` and diff against vanilla Belethor `factdiag
<Skyrim.esm> 0x09CAF5` — same flags / VendorValues / buy-sell list / merchant container / `LocationCell`
VendorLocation shape. **Confirmed in-game 2026-05-31**: `coc MF_Shop`, `set GameHour to 12`, talk to
Marcurio → barter menu opens (the reachable-NPC + load-registration rules from the conversational-NPC
recipe still apply).

## "Passive perk on an NPC" (PERK — ability + entry-point)

Two perk shapes, both attachable to an NPC via `npcs[].perks` (the actor gains them at game start):

```jsonc
{ "magicEffects": [
    { "editorId": "MF_IronHideMgef", "archetype": "ValueModifier", "actorValue": "DamageResist",
      "castType": "ConstantEffect", "targetType": "Self",
      "flags": [ "Recover", "NoArea", "NoDuration", "HideInUI" ] }   // a constant-effect fortify
  ],
  "spells": [
    { "editorId": "MF_IronHideAbility", "name": "Iron Hide", "spellType": "Ability",
      "castType": "ConstantEffect", "targetType": "Self",
      "effects": [ { "magicEffect": "MF_IronHideMgef", "magnitude": 50 } ] }
  ],
  "perks": [
    // (a) ability perk — grants the constant-effect SPEL above
    { "editorId": "MF_IronHidePerk", "name": "Iron Hide", "numRanks": 1,
      "effects": [ { "kind": "ability", "spell": "MF_IronHideAbility" } ] },
    // (b) entry-point perk — +20% attack damage when wielding a sword, available at One-Handed 30
    { "editorId": "MF_DeadlyStrikesPerk", "name": "Deadly Strikes", "numRanks": 1,
      "conditions": [
        { "function": "GetBaseActorValue", "actorValue": "OneHanded",
          "comparison": "GreaterThanOrEqualTo", "value": 30 } ],
      "effects": [
        { "kind": "entryPoint", "entryPoint": "ModAttackDamage", "function": "Multiply", "value": 1.2,
          "conditions": [
            { "function": "WornHasKeyword", "param": "Skyrim.esm:0x01E711",   // WeapTypeSword
              "comparison": "EqualTo", "value": 1 } ] } ] }
  ],
  "npcs": [
    { "editorId": "MF_PerkGuard", "name": "Hardened Guard", "race": "Skyrim.esm:0x013746",
      "perks": [ "MF_IronHidePerk", "MF_DeadlyStrikesPerk" ] }
  ] }
```

- Discover entry-point names + a vanilla shape to copy: `perkdiag <Skyrim.esm> entrypoints` and
  `perkdiag <Skyrim.esm> 0x079343` (Armsman20). Verify your output with `dump` / `perkdiag <out.esp> <id>`.
- **Player perks** are NOT record-only: grant them with a Papyrus `AddPerk` call (a `scripts` quest
  fragment). The NPC path above is fully record-authorable.
- Structurally these emit exactly like vanilla perks; whether the modifier actually moves combat
  numbers in-game needs a real Skyrim launch to confirm (structural-only here). Full example:
  [`../../examples/perk_spec.json`](../../examples/perk_spec.json).

## "Multi-stage quest" (stages + journal log + objectives + dialogue-set-stage)

A quest that **progresses**: it advances through stages, writes journal text at each, shows/completes
objectives per stage, and closes itself. Pure record data (stages, log entries, `completeQuest`,
log-entry conditions) builds and `questdiag`s cleanly; the objective-display and dialogue-set-stage
*logic* is emitted as Papyrus fragment scaffolds for the CK (see the note at the end).

```jsonc
{ "quests": [ {
    "editorId": "MF_ErrandQuest", "name": "A Forged Errand",
    "startGameEnabled": true, "priority": 60,
    "stages": [
      { "index": 10, "logEntry": "Joren asked me to retrieve his lost hammer." },
      { "index": 20, "logEntry": "I agreed to help. Time to search the riverbank." },
      { "index": 30, "logEntry": "I returned the hammer. Done.", "completeQuest": true } ],
    "objectives": [
      { "index": 10, "text": "Agree to help Joren", "showStage": 10, "completeStage": 20 },
      { "index": 20, "text": "Find Joren's hammer",  "showStage": 20, "completeStage": 30 } ] } ],
  "dialogue": [
    { "editorId": "MF_AgreeToHelp", "questEditorId": "MF_ErrandQuest", "speakerNpcEditorId": "MF_Joren",
      "prompt": "I'll find your hammer.", "responses": [ "Good. It's by the mill." ],
      "setStage": 20 } ] }                                 // picking this advances the quest 10 → 20
```

Verify the records with `questdiag <plugin.esp> <questFormId>` (stages, log entries, `CompleteQuest`
flags, objectives). The full worked spec — with the NPC, Hello, and placement — is
[`examples/quest_stages_spec.json`](../../examples/quest_stages_spec.json).

**`package` handles Papyrus automatically — no CK needed (IN-GAME CONFIRMED It.36 2026-06-02).**
It generates, compiles, and VMAD-attaches everything:

- `Scripts/Source/MF_ErrandQuest_Stages.psc` — one `Fragment_Stage_XXXX_Item00000()` per stage that
  shows/completes objectives (`SetObjectiveDisplayed` / `SetObjectiveCompleted`). Engine calls this
  function by name when `SetStage()` fires.
- `Scripts/Source/TIF_MF_AgreeToHelp.psc` — `extends TopicInfo Hidden`, explicit `Quest Property
  OwningQuest Auto` (bound to the quest FormKey in VMAD), `OnBegin` calls `OwningQuest.SetStage(20)`.
  **Do not use `GetOwningQuest()` — returns None for StartGameEnabled quests** (see gotchas.md).
- Compiled `.pex` copied to `Scripts/`, VMAD attached to QUST and INFO.
- A `GetStage(quest) < 20` condition auto-added to the `setStage` line so Joren won't repeat it.

The `package` command requires `~/tools/papyrus-compiler` (Linux-native; falls back to Wine/CK).
One-time machine setup: write stub `TopicInfo.psc` + `ScriptObject.psc` to
`Data/Scripts/Source/` (see `Papyrus.cs` for details). Dialogue only registers on a game **LOAD**
— see [gotchas.md](gotchas.md).
