# Trade & Barter (kryptopyr) — mod-survey finding

Researched 2026-06-25. SE Nexus page is **23081** (the prompt's `4894` is wrong / not the
live SE page; LE original is `34612`, not `4256`). Web research only — I could **not**
fetch the Nexus description directly (Nexus + StepModifications + Fandom all returned HTTP
403); facts below are corroborated from multiple secondary sources and flagged where thin.

## 1. What it does

Trade & Barter is a lightweight, fully-MCM-configurable **economy/merchant overhaul**. It
adds many optional factors that shift buy/sell prices and merchant behaviour, each toggleable
or settable to zero. Headline knobs (corroborated across sources):

- **Overall barter rate** + **how much Speech skill affects prices** (adjustable).
- **Merchant gold** scaled by settlement size (big-city merchants richer than small towns);
  separate **Fence gold** option.
- **Location pricing** — goods cheaper in small towns, pricier in large cities; named
  location variables incl. Raven Rock / Solstheim (Dragonborn DLC).
- **Status / faction pricing** — Thane, Guild Member, Guild Leader get % discounts in the
  relevant city/guild; price varies by **faction rank, relationship, and race**.
- **Knowledge-based pricing** — better prices when you're skilled in the goods' domain:
  **Smithing** at blacksmiths, **Alchemy** at apothecaries (a "you-know-its-worth" system).
- **Race/kin pricing** — incl. a non-Orc **Blood-Kin of the Orcs** bonus at Orc strongholds;
  Dawnguard/Volkihar faction members get vendor discounts from their side's merchants.
- **Inventory respawn rate** (how fast merchant stock refreshes) adjustable.
- **Vanilla Speech-perk interaction** — works with the **Investor** and **Haggling** perks
  (it fixes `DLC2DremoraPrices` so the Dremora merchant obeys Haggling, and extends the
  Investor effect to USSEP-added investment merchants: Falion, Filnjar, Lod, Madena, Moth
  gro-Bagol, Zaria, Glover Mallory).

UNVERIFIED: any literal "blood price" or "pay-to-invest-in-shop" mechanic beyond the vanilla
Investor perk — I found no evidence T&B adds a shop-investment *system* of its own; it
extends the *vanilla* Investor perk's reach. Treat "invest in shops" as **vanilla Investor
perk, not a T&B addition** unless verified against the esp.

## 2. Mechanism / how it's implemented

This is the load-bearing finding and it's **well corroborated**:

> "uses a series of carefully conditioned perks to accomplish nearly all of its changes, with
> the only script included being the MCM script that controls the menu options."

- **Perks (the engine).** Nearly everything is **conditioned EntryPoint perks** — i.e.
  `Mod Buy Prices` / `Mod Sell Prices` perk entry-point effects gated by CTDA conditions
  (faction rank, location, race/keyword, skill value, relationship). The MCM toggles drive
  GLOB/condition values that enable/disable each perk path. This is a near-pure record
  overhaul, **not** a Papyrus-driven price engine.
- **Scripts.** Exactly **one** Papyrus script: the **MCM menu script**. No per-frame /
  OnSell scripting. So it is *script-light by design* (the script only writes config).
- **MCM.** Ships a **SkyUI MCM** (its own script, classic SkyUI `SKI_ConfigBase`-style — this
  predates and is independent of MCM-Helper). **Requires SKSE + SkyUI.** Without SkyUI the
  mod still loads but all options stay at default values. There's a separate companion mod,
  **"Trade and Barter - Settings Loader" (Nexus 57926)**, that auto-saves/loads MCM settings
  per new game — i.e. base T&B does **not** persist settings without it.
- **GMST / game settings.** Sources reference vanilla barter GMSTs `fBarterMin` / `fBarterMax`
  (and the Speech-affects-price relationship) as the *concept space*. UNVERIFIED whether T&B
  edits those GMSTs as static records vs. driving everything through perk entry points + GLOBs.
  Given the "conditioned perks do nearly everything" design, the likely answer is **mostly
  perks/GLOBs, minimal-to-no static GMST edits**, but I could not byte-confirm the esp. Do
  **not** assert specific GMST edits without checking the plugin.
- **Factions / VendorValues.** It reads/uses vanilla merchant **job factions** and added
  "merchant job faction" price options; UNVERIFIED whether it rewrites Faction VendorValues
  (gold/radius/hours) records or layers gold via perks/GLOBs. Merchant-gold scaling is
  presented as an MCM option, suggesting GLOB-driven rather than hard VendorValues edits, but
  unconfirmed.
- **No SKSE-plugin (DLL) dependency** beyond SKSE-the-loader (needed for SkyUI/MCM). No KID /
  SPID. UNVERIFIED but consistent with all sources describing it as records + one MCM script.
- **ESL?** UNVERIFIED — do not claim a flag/version. Modern SE builds *may* be ESL-flagged;
  check the actual file.

**No specific FormIDs, GMST values, perk EDIDs, or version numbers are asserted here** — none
were verifiable from secondary sources. The only named vanilla identifiers I'm confident in
are the perk-entry-point *types* `Mod Buy Prices` / `Mod Sell Prices`, the vanilla **Investor**
/ **Haggling** Speech perks, and the `DLC2DremoraPrices` perk T&B patches.

## 3. Relevance to ModForge

**Yes — ModForge could already generate most of a T&B-style tweak mod**, because T&B's core
mechanism is precisely what ModForge's perk pipeline does. Verified against the code:

- ✅ **`ModBuyPrices` / `ModSellPrices` EntryPoint perks are already supported.** Both are in
  ModForge's entry-type tab-count map (`src/ModForge.Core/Generator.Build.Perks.EntryPoints.cs`
  lines 31 & 55) — the two price entry points T&B is built on.
- ✅ **Conditioned perks.** ModForge perks support both **perk-level and effect-level CTDA
  conditions** via the shared `ConditionSpec` (`src/ModForge.Core/Spec.Perks.cs`, `Conditions`
  on both perk and effect; `Generator.Build.Conditions.cs`). That's exactly T&B's "carefully
  conditioned perks" model — faction-rank / location / race-keyword / skill-value gates map to
  ModForge condition functions.
- ✅ **MCM generation.** ModForge already emits MCM config (`Spec.Mcm.cs`, `McmGen.cs`,
  `Generator.Build.Mcm.cs`) — per the MEMORY recipe it's the MCM-Helper path (config.json +
  generated QUST/alias), which is a *different* MCM tech than T&B's hand-scripted SkyUI menu,
  but functionally covers "ship a config menu."
- ✅ **Vendor factions** (Vendor flag + VendorValues + sellBuyList + merchant container) and
  the new **`settlements:` population macro** exist (`Generator.Build.Vendor.cs`,
  `Spec.Settlement.cs`) — so the merchant-side scaffolding a price mod attaches to is present.
- ✅ **SPID/KID distribution** exists if one wanted to distribute the price perks onto the
  player or merchants instead of hand-placing them.

So the pitch **"could ModForge generate a merchant-economy tweak mod like this?"** → **largely
yes**: a spec of conditioned `ModBuyPrices`/`ModSellPrices` perks (Thane discount, skill-based
pricing, race/kin bonus) + an MCM to toggle them is expressible **today**.

**Gaps for a faithful reproduction:**

- ❌ **No GMST / game-setting editing in ModForge.** Confirmed: `grep -ri gmst/gamesetting`
  over `src/` returns **nothing** (only survey docs mention it). If a faithful T&B port needs
  to touch `fBarterMin`/`fBarterMax` or other GMSTs, ModForge **cannot express that today**.
  (May or may not be needed — see §2 uncertainty — but the capability is absent regardless.)
- ⚠️ **MCM-driven runtime values.** T&B's MCM writes GLOBs that its perk conditions read so
  options are live-toggleable. UNVERIFIED whether ModForge's MCM generator can wire an MCM
  slider/toggle → GLOBAL → perk-condition value end-to-end. Needs a code pass on
  `Generator.Build.Mcm.cs` ↔ globals before claiming parity.
- ⚠️ **Merchant-gold / inventory-respawn knobs.** If T&B does these via Faction VendorValues
  edits or LVLI respawn flags, confirm ModForge can *edit* those on vanilla records (override),
  not just create new merchant factions. UNVERIFIED.

**Out of scope / not worth replicating:** the USSEP-merchant compat fixes and the
`DLC2DremoraPrices` patch are vanilla-bug-fix glue, not a generatable feature pattern.

## 4. Roadmap implications (actionable)

1. **GMST / game-setting editing is the one hard gap.** ModForge has *no* GMST story
   (verified absent in `src/`). Add a `gameSettings:` (or `gmst:`) spec block that emits GMST
   override records (float/int/string). Even if T&B itself leans on perks, GMST editing is a
   broad economy/balance primitive (`fBarterMin`, `fXPPerSkillRank`, etc.) many tweak mods need.
2. **Confirm MCM-toggle → GLOBAL → perk-condition wiring.** This is the difference between
   "ModForge can ship an MCM" and "ModForge can ship a *configurable* mechanics mod like T&B."
   Verify/close on `Generator.Build.Mcm.cs` + globals; if missing, it's a concrete feature:
   bind an MCM option to a generated GLOB that a perk's CTDA reads.
3. **Verify override-editing of vanilla Faction VendorValues / LVLI respawn** (merchant gold,
   stock refresh). If ModForge can only create new vendor factions, add override support.
4. **No new perk-type work needed** — `ModBuyPrices`/`ModSellPrices` already land. Good signal
   that the EntryPoint perk surface is broad enough for economy mods.
5. **(Validation TODO, not a gap)** To remove the §2 UNVERIFIED flags, open the actual T&B esp
   in Mutagen and confirm: does it edit GMSTs? does it edit VendorValues? is it ESL? This
   survey is secondary-source only because Nexus/Step/Fandom 403'd the fetch.

## Sources (actually fetched/searched)

- Nexus SE 23081 (description not fetchable — 403): https://www.nexusmods.com/skyrimspecialedition/mods/23081
- Nexus LE 34612: https://www.nexusmods.com/skyrim/mods/34612
- Settings Loader companion (Nexus 57926): https://www.nexusmods.com/skyrimspecialedition/mods/57926
- Nolvus economy guide (fetched): https://www.nolvus.net/guide/ultra/gameplay/economy
- StepModifications T&B topics (403, search-summarized): https://stepmodifications.org/forum/topic/1996-trade-and-barter-by-kryptopyr/
- TES Mods Fandom wiki (403, search-summarized): https://tes-mods.fandom.com/wiki/Trade_and_Barter
- Patreon T&B SE post (search-summarized): https://www.patreon.com/posts/trade-barter-se-24395210

ModForge code checked: `src/ModForge.Core/Generator.Build.Perks.EntryPoints.cs` (ModBuyPrices
L31 / ModSellPrices L55), `src/ModForge.Core/Spec.Perks.cs` (perk + effect Conditions),
`Spec.Mcm.cs` / `McmGen.cs` / `Generator.Build.Mcm.cs`, `Generator.Build.Vendor.cs`,
`Spec.Settlement.cs`; GMST absent from `src/` (grep).
