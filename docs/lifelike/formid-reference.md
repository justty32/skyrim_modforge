# Vanilla FormID reference

Harvested via `find` / `*diag`. All refs are `Skyrim.esm:0xFORMID` — usable directly in any spec
`ref` field. Always re-confirm with `find` for your install; **never guess a FormID**.

← back to [lifelike hub](README.md)

## Procedure templates (for `packages[].template`)

| Template | FormID | Slots used | Use when |
|---|---|---|---|
| Sandbox | `Skyrim.esm:0x01C254` | 12 | NPC hangs around a location, interacts with furniture/idle markers/other NPCs |
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
| PotentialFollowerFaction | `Skyrim.esm:0x05C84D` | Required (with an Ally relationship) for the vanilla "Follow me" hire dialogue |

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
