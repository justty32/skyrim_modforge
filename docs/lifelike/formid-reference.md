# Vanilla FormID reference

Harvested via `find` / `*diag`. All refs are `Skyrim.esm:0xFORMID` — usable directly in any spec
`ref` field. Always re-confirm with `find` for your install; **never guess a FormID**.

← back to [lifelike hub](README.md)

## Procedure templates (for `packages[].template`)

| Template | FormID | Slots used | Use when |
|---|---|---|---|
| Sandbox | `Skyrim.esm:0x01C254` | 12 | NPC hangs around a location, interacts with furniture/idle markers/other NPCs |
| Sleep | `Skyrim.esm:0x019717` | 14 | Specialized Sandbox that actively **seeks a bed** and beds down; can lock doors. Wired in `packages[].sleep`. Sleep window = the package `schedule` (`hour`+`durationInMinutes`); `sleep.lockDoors` defaults true — set false for shared/inn sleeping |
| Travel | `Skyrim.esm:0x016FAA` | 3 | NPC walks to a specific REFR/cell |
| Patrol | `Skyrim.esm:0x017723` | 6 | Guard route. Wired in `packages[].patrol` (`start` → first marker); the route is the markers' `linkedRefs` chain (m1→m2→m3→m1 looped, null keyword). Markers must be on navmesh |
| UseMagic | `Skyrim.esm:0x0504F5` | 11 | Scheduled non-combat spell casting (priest at altar, mage self-buffing). Wired in `packages[].useMagic` |
| Follow | `Skyrim.esm:0x019B2C` | 6 | NPC physically follows the player (or another actor). Wired in `packages[].follow`. Raw tag-along movement only — a hireable follower also needs a managing quest + follow faction + dialogue |
| Escort | `Skyrim.esm:0x023B73` | 9 | **Dual of Follow** — NPC LEADS an escorted target to a destination, pausing if they lag. Wired in `packages[].escort`. Same navmesh rules as Patrol/Travel; the destination marker is auto-persisted |
| UseWeapon | `Skyrim.esm:0x01C338` | — | Practice attacks at a target — not yet ModForge-supported |

> There is **no vanilla `UseItemAt` template**. "Go to specific furniture" = Sandbox + a
> `location` ref to a furniture REFR + `allowSpecialFurniture: true`.

Per-template `Data` slot schemas are in [engine-internals → PACK slot maps](../engine-internals.md#pack-data-slot-maps).

## Voice types (for `voiceType`)

| Editor ID | FormID |
|---|---|
| MaleNord | `Skyrim.esm:0x013AE6` |
| FemaleNord | `Skyrim.esm:0x013AE7` |
| MaleNordCommander | `Skyrim.esm:0x0E5003` |

Without a voice type, the NPC is silent — no hello/idle audio, no subtitles.

## Factions for "city citizenship" (for `crimeFaction` + `factions`)

| Editor ID | FormID | Use for |
|---|---|---|
| CrimeFactionWhiterun | `Skyrim.esm:0x0267EA` | Whiterun (crime + citizen identity) |
| TownWhiterunFaction | `Skyrim.esm:0x028172` | Whiterun (reinforcing) |
| PotentialFollowerFaction | `Skyrim.esm:0x05C84D` | Required (with an Ally relationship) for the vanilla free "Follow me" hire dialogue |
| CurrentFollowerFaction | `Skyrim.esm:0x05C84E` | The "is currently my follower" faction. `SetFollower`'s alias adds it; gate **follower-only dialogue** on `GetInFaction CurrentFollowerFaction == 1` (backstory, situational lines, banter) so it shows only while she travels with you |
| PlayerFollowerCount (global) | `Skyrim.esm:0x0BCC98` | Vanilla's "how many followers" GLOB. Gate a recruit line on `GetGlobalValue == 0` so it never stomps the single follower slot |
| PotentialHireling | `Skyrim.esm:0x0BCC9A` | The PAID-hireling gate — but **membership alone only buys the *refusal* line, NOT the recruit** (REFUTED It.27, see gotchas #hireling-getsid). The actual "I'll pay you 500" RECRUIT INFOs in `HirelingQuestTopic1` (0x0BCC84) are each hardcoded `GetIsID == <specific vanilla HirelingX>` — a custom NPC matches none, so at gold≥500 every INFO fails and the topic VANISHES. Recruiting a custom NPC needs a script either way |
| CurrentHireling | `Skyrim.esm:0x0BD738` | The recruit INFOs require `GetInFaction CurrentHireling == 0` (not already hired); the result script adds you to it on hire |

Other hold crime/town factions follow the same naming pattern; `find <Skyrim.esm> CrimeFaction Faction`.

## CombatStyle profiles (harvest via `cstydiag`)

| Editor ID | FormID | OffMult | DefMult | EquipMult (M/Mg/R/Sh/U/St) | Avoid | Flags | Use for |
|---|---|---|---|---|---|---|---|
| csVampireMagic | `Skyrim.esm:0x02DFB5` | 0.77 | 0.3 | 0.51 / 8.1 / 0.55 / 0.21 / 0.98 / 2.15 | 0.2 | Dueling | Strong mage |
| csSoldierMagic | `Skyrim.esm:0x046B9E` | 0.5 | 0.5 | 1 / 3 / 1 / 1 / 1 / 0 | 0 | — | Battlemage (balanced lean) |
| csForswornMagic | `Skyrim.esm:0x0442CD` | 0.5 | 0.5 | 1 / 1 / 1 / 1 / 1 / 1 | 0.2 | Dueling | Balanced — NAME IS MISLEADING |

## Equip types (for `spells[].equipType`)

| Editor ID | FormID | Note |
|---|---|---|
| EitherHand | `Skyrim.esm:0x013F44` | Hand spell equippable to either hand — **required** or an NPC can't equip the spell to cast in combat |

(BothHands / LeftHand / RightHand variants also exist — `find <Skyrim.esm> hand EquipType`.)

## Markers for placement / Travel destinations

| Editor ID | FormID | Worldspace | Notes |
|---|---|---|---|
| WhiterunBanneredMare (cell) | `Skyrim.esm:0x01605E` | interior | `coc` target |
| RiverwoodSleepingGiantInn (cell) | `Skyrim.esm:0x0133C6` | interior | `coc` target |
| WhiterunBreezehome (cell) | `Skyrim.esm:0x0165A8` | interior | Good `cells[].template` source for interior lighting |
| RiverwoodInnCenterMarker | `Skyrim.esm:0x01DC0A` | inside the inn | In-cell Travel target |
| debugWhiterunOrigin | `Skyrim.esm:0x0567F7` | WhiterunWorld | `coc whiterun` target — inside city walls |
| debugRiverwood | `Skyrim.esm:0x0567F6` | Tamriel | Riverwood exterior |
| WhiterunStablesHorseMarker | `Skyrim.esm:0x109826` | Tamriel | Just outside Whiterun's main gate |
| Tamriel (worldspace) | `Skyrim.esm:0x00003C` | — | Worldspace ref for exterior `placements` |

## Reference actors (engine built-ins)

| Editor ID | FormID | Note |
|---|---|---|
| Player (NPC base) | `Skyrim.esm:0x000007` | RELA child default / `GetRelationshipRank` target |
| PlayerRef (placed ref) | `Skyrim.esm:0x000014` | Default Follow/Escort target |

## Test enemy bases (for in-game `placeatme <id> 1`)

| Editor ID | FormID |
|---|---|
| EncWolfIce_Indoor | `Skyrim.esm:0x10F2A3` |
| EncWolf_Indoor | `Skyrim.esm:0x10F2A2` |
| EncBandit05MagicArgonianM | `Skyrim.esm:0x0C3CA7` |

## Leveled enemy spawns (for a `placements[].base` leveled-actor spawn → ACHR)

A spawn `base` set to one of these LeveledNpc (LVLN) lists rolls a level-appropriate actor at load.
Add `"kind": "npc"` for a vanilla list (the build can't read the master's type headlessly). Pair with
an `encounterZone` to control the level range. Find more: `find <Skyrim.esm> LCharBandit LeveledNpc`.

| Editor ID | FormID | Role |
|---|---|---|
| LCharBanditMeleeAny | `Skyrim.esm:0x03DECD` | Generic melee bandit |
| LCharBanditMissileNordM | `Skyrim.esm:0x01A348` | Archer bandit |
| LCharBanditBossNordM | `Skyrim.esm:0x01A341` | Bandit boss (tougher, level-scaled) |

## Encounter zones (sample vanilla ECZNs — inspect with `eczndiag <Skyrim.esm> <id>`)

| Editor ID | FormID | Levels / flags |
|---|---|---|
| HelgenZone | `Skyrim.esm:0x0F94A6` | min 6 / max 0 (uncapped), `NeverResets` |
| BoulderfallCaveZone | `Skyrim.esm:0x0F52DB` | min 6 / max 0 (uncapped), no flags |
| NoResetZone | `Skyrim.esm:0x0F90B1` | min 1 / max 0, `NeverResets` (a reusable "don't respawn" zone) |

## Spells / magic effects (for `spells` list and `effects[].magicEffect`)

| Editor ID | FormID | Kind | Notes |
|---|---|---|---|
| FlamesRightHand | `Skyrim.esm:0x0C969A` | spell | Novice destruction cone — good for first mage test |
| SparksRightHand | `Skyrim.esm:0x0C96A1` | spell | Shock variant |
| FireboltStormBasic | `Skyrim.esm:0x0D07CD` | spell | Apprentice fire projectile |
| Candlelight | `Skyrim.esm:0x043324` | spell | Self-cast, visible orb — ideal UseMagic demo |
| AlchRestoreHealth | `Skyrim.esm:0x03EB15` | MGEF | Restore-health reference profile (NoDuration/NoArea, baseCost 0.5, no Recover) |
| AlchDamageHealth | `Skyrim.esm:0x03EB42` | MGEF | Damage-health |
| FireDamageFFAimed75 | `Skyrim.esm:0x10F7F1` | MGEF | Aimed-firebolt profile source for a custom aimed spell |

### Visual sub-forms for a custom aimed/projectile spell

| Purpose | FormID | Type |
|---|---|---|
| Firebolt projectile (carries impact visuals) | `Skyrim.esm:0x10FBEA` | PROJ |
| Firebolt casting art (at the hands) | `Skyrim.esm:0x01B211` | ARTO |

## Templates for cloning a model (for `template`)

| Editor ID | FormID | Clones model for |
|---|---|---|
| IronSword | `Skyrim.esm:0x012EB7` | weapons |
| Book1CheapNordsArise | `Skyrim.esm:0x0ED161` | books |
| GemRuby | `Skyrim.esm:0x063B42` | misc items |
| RestoreHealth06 | `Skyrim.esm:0x039BE5` | potions |

## Statics / lights (for building an interior cell)

| Editor ID | FormID | Use |
|---|---|---|
| WRShadowOmni | `Skyrim.esm:0x0C82AE` | Omni shadow key light, radius 512, on-by-default, NOT PortalStrict — correct for an open interior |
| WRInteriorLightBrite01 | `Skyrim.esm:0x06ED46` | Non-shadow warm fill light |
| DefaultSunlightHalfOmni01 | `Skyrim.esm:0x0172C4` | **AVOID as sole light** — radius 256 + PortalStrict, near-useless in a portal-less cell |
| WRIntFloorSTMid01Large | `Skyrim.esm:0x1044AA` | Floor tile (256 spacing in a 3×3 grid) |
| WRIntWallStr01Low | `Skyrim.esm:0x0CB43B` | Whiterun interior wall piece |

## Crafting (for `recipes`)

| Editor ID | FormID | Use |
|---|---|---|
| CraftingSmithingForge (keyword) | `Skyrim.esm:0x088105` | Default workbench keyword (forge) — `recipes[].workbench` defaults to this |
| IngotIron | `Skyrim.esm:0x05ACE4` | Crafting component |
| LeatherStrips | `Skyrim.esm:0x0800E4` | Crafting component |

## Outfits

| Editor ID | FormID |
|---|---|
| BlacksmithOutfit01 | `Skyrim.esm:0x09D5DF` |

## Vendor / merchant (for a `factions[].vendor` shopkeeper)

| Editor ID | FormID | Kind | Use |
|---|---|---|---|
| JobMerchantFaction | `Skyrim.esm:0x051596` | FACT | The generic "I'd like to trade" topic gates on membership here — **Build auto-adds it** to any NPC in an in-spec vendor faction |
| ServicesWhiterunBelethorsGoods | `Skyrim.esm:0x09CAF5` | FACT | Reference vanilla general-goods vendor faction — diff your generated FACT against it with `factdiag` |
| VendorItemsMisc | `Skyrim.esm:0x06CB48` | FormList | General-goods category list (use with `notSellBuyList: true` for a "sells everything" shop) |
| VendorItemsBlacksmith | `Skyrim.esm:0x066333` | FormList | Smith's category list (weapon/armor/ore/ingot/…) |
| VendorGoldMisc | `Skyrim.esm:0x072AE7` | LVLI | The vendor's gold pool — put one in the merchant chest so it has money to buy with |
| LItemMiscVendorMiscItems75 | `Skyrim.esm:0x09AF0A` | LVLI | General-goods stock leveled-list (what the shop sells) |
| Gold001 | `Skyrim.esm:0x00000F` | MISC | Plain gold (use a flat count instead of the leveled gold pool if you prefer) |

The generic trade prompt (`DialogueGeneric.OfferServicesTopic` `0x07F6BB`) is vanilla universal
dialogue — you do **not** emit it. It surfaces on any conversable NPC who is in `JobMerchantFaction`
+ a Vendor-flagged faction with a merchant container, during the faction's trade hours.
