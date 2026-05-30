# Gotchas — the traps that bit us, with the fix

Grouped by area. Each row is symptom → root cause → fix. These are the failure modes that
cost real debugging time; the fix column is the proven workaround.

← back to [lifelike hub](README.md) · see also [engine-internals](../engine-internals.md) for the "why"

## NPC behaviour (packages, combat, speech)

| Symptom | Root cause | Fix |
|---|---|---|
| Generated package's `target` reads as `LocationTarget` instead of `LocationFallback` even though we used `new LocationFallback()` | Mutagen picks the binary shape from `LocationFallback.Type` enum, NOT the C# class | `new LocationFallback { Type = LocationTargetRadius.LocationType.NearSelf }` |
| Sandbox-equipped NPC stands still indefinitely doing nothing | Used `Type = NearEditorLocation` — needs CK-set Editor Location which Mutagen-generated NPCs lack | Use `Type = NearSelf` — anchors at current position, no external link |
| NPC doesn't move, just stands at spawn (after ~1 min delay still nothing) | Sandbox finds no furniture / idle markers / other NPCs nearby — barren area | Move spawn to a populated cell (Bannered Mare, Sleeping Giant Inn). Sandbox **needs** content to interact with |
| Sandbox NPC stands still for the first 30–90 seconds after cell load | Engine sandbox cold-start delay; normal | **Wait the full minute** before declaring failure — vanilla NPCs hide this because they're initialised long before the player arrives |
| Travel package in spec but NPC ignores it (sandboxes locally instead) | Engine silently rejected cross-cell Travel — NPC has no "citizen" identity to traverse city gates | Set `crimeFaction` + add the town's faction to `factions` + `unique: true` |
| NPC walks but never speaks; only mumbles "嗯/啊" when approached | No voiceType set, OR voice set but no faction-conditioned dialogue topics match | Set `voiceType: MaleNord` (or similar). For more chatter, add a town faction so faction-conditioned dialogue topics apply |
| Mage NPC just runs away from any threat | `Aggression=Unaggressive + Confidence=Cowardly` (Mutagen defaults) — flees regardless of CombatStyle | `aggression: "Aggressive"` + `confidence: "Brave"` |
| Townsperson treats the player as hostile / starts fights | `aggression: "Aggressive"` makes an NPC *initiate* | For a defend-but-don't-initiate NPC use `aggression: "Unaggressive"` + `confidence: "Brave"` — aggression governs initiation, Brave governs flee-vs-stand once attacked |
| UseMagic NPC stands at location but NEVER casts | Slot 3 "Spell" authored as `PackageTargetObjectType` (category enum). All 46 vanilla UseMagic packages use `PackageTargetObjectID` with a FormLink to a specific SPEL | Spec field `useMagic.spell: "<master>:0xFORMID"` of the SPEL; Build writes `PackageTargetObjectID` |
| UseMagic NPC casts 1-2 times then stops forever | `numToCastMax` is **total package-lifetime casts**, not per-cycle. With `schedule.durationInMinutes=0` the package completes the moment its quota's hit | `numToCastMax: 1000` + `schedule.durationInMinutes: 1440` (24h continuous), mirroring vanilla `WCollegeOnmundPracticeFlames12x4` |
| UseMagic NPC stops casting when combat starts | Combat AI preempts idle packages (vanilla behaviour) | Add `flags: [ "IgnoreCombat" ]` like vanilla `SprigganCallOverride` if casting must continue (e.g. boss ritual) |
| Generated combat spell never gets cast (NPC melees instead) | The SPEL has no `equipType` — an NPC can't equip an un-equippable spell to a hand | Set `spells[].equipType: "Skyrim.esm:0x013F44"` (EitherHand) |
| Patrol NPC (or any path-to-marker behaviour) stands still, never moves | A **static REFR does NOT snap to the floor** like an actor does — a marker at a guessed exterior z lands off-navmesh, so pathing silently fails | Anchor markers on coords PROVEN walkable: `refpos <plugin> <0xFORMID>` to copy a vanilla reachable ref's position, or place inside a hand-navmeshed interior. Actors (ACHR) snap to ground; markers (REFR) do not |
| Want a hireable follower but the "Follow me" topic never appears | NPC isn't in **PotentialFollowerFaction** and/or has no **Ally relationship** to the player — the vanilla free-follow dialogue is gated on both | Add `Skyrim.esm:0x05C84D` to `factions` + a `relationships[]` entry (parent=NPC, child=`Skyrim.esm:0x000014`, rank=`Ally`) + a follower-capable voice. Leverages the vanilla DialogueFollower quest — no custom dialogue/scripts |
| **CRASH at main menu** when a mod with a `relationships[]` entry is enabled | RELA `parent`/`child` must point at an **NPC_ base record**; the player's is `Skyrim.esm:0x000014`. `0x000007` is **PlayerRef** (the placed ACHR) — a RELA pointing at an ACHR is a type mismatch the engine rejects at load | Use `child: "Skyrim.esm:0x000014"`. (Was the wrong default in `RelationshipSpec.Child` — found via byte-compare to a working mod, fixed.) |
| **CRASH at main menu** when a mod with custom `dialogue[]` is enabled | Generated `DialogTopic` left **SNAM (subtype name) = null** (`0x00000000`). Every vanilla `Custom` topic carries SNAM=`'CUST'`; an empty subtype crashes the engine while it builds the dialogue-topic index at load | Set `topic.SubtypeName = new RecordType("CUST")` alongside `Subtype = Custom`. Fixed in `Generator.Build.cs`; when composing topics by hand via the library, set both. Confirm with a vanilla probe: every Custom DIAL has SNAM=`'CUST'` |
| Custom dialogue doesn't crash but **the topic never appears** — activating the NPC just plays voicetype mumbles, dialogue menu never opens | The NPC has **no Hello info**, so it isn't *conversable*: activating it can't open the dialogue menu, so the player (TopLevel) topics never surface. Quest can be confirmed running (`sqv`) and the topic structurally identical to a vanilla one and it still won't show. A custom NPC has no generic-greeting coverage; vanilla talkable NPCs all carry a Hello | Auto-emit a **Hello** topic per speaking NPC: `Category=Misc`, `Subtype=Hello`, `SNAM='HELO'`, **no branch**, owned by the host quest, gated on `GetIsID(speaker)`, with one greeting response. Spec field `npc.greeting` sets the line (empty ⇒ a default). Fixed in `Generator.Build.cs` |
| Custom dialogue response subtitle flashes by / is unreadable (or the NPC seems mute) | Unvoiced lines get ~0 duration from the (missing) voice file — Skyrim zooms past them | Install **[Fuz Ro D-oh — Silent Voice](https://www.nexusmods.com/skyrimspecialedition/mods/15109)** (calculates duration from text length for all unvoiced lines) **or** bundle a silent `.fuz` per line at `Sound/Voice/<plugin>/<voicetype>/<info>_<formid>.fuz`. Also turn on dialogue subtitles |

## Cells, worldspace, lighting

| Symptom | Root cause | Fix |
|---|---|---|
| Headless build throws "Could not determine plugin listings path" while cloning a vanilla record | Copying a localized `TranslatedString` (Name/Description/BookText) triggers an all-string-source resolve that needs plugins.txt/load-order — absent headless on Linux | Pass a `TranslationMask { Name=false, … }` to `DeepCopyIn` (you override those anyway). Same landmine hits `GetOrAddAsOverride` on cells and `find`'s Name resolution |
| Whole world floods / "everything underwater" after an exterior placement | An override Cell/Worldspace does NOT inherit omitted data from the master — the engine defaults them. Dropping `LandDefaults` reset `DefaultWaterHeight` from Tamriel's real -14000 to 0 | Hand-copy env subrecords (`CopyWorldspaceEnv` / `CopyCellEnv`), skipping only the localized Name + the giant child structures. (A cell's own `WaterHeight=FLT_MAX` is just a "use worldspace default" sentinel — a red herring) |
| Save / HUD shows "unknown location" after a worldspace override | Minimal override blanked the localized worldspace Name (unreadable headless) | Restate a plain Name (`"Skyrim"` for Tamriel `0x3C`); other worldspaces need a future spec field |
| Vanilla interior-cell override silently IGNORED (placed objects don't appear, lighting unchanged) | Interior cells are grouped into block/sub-block GRUPs **by FormID**; an override in the wrong GRUP is never matched to the master | Compute the GRUP from the cell FormID: **block = id % 10, sub = (id/10) % 10** (decimal, 24-bit ID). See [engine-internals](../engine-internals.md#interior-cell-grup-formula) |
| New in-spec interior cell renders pitch-black / player falls into the void on `coc` | A brand-new cell has no Lighting/LightingTemplate and no floor geometry | Give `cells[].template` a vanilla interior ref (for lighting via `CopyCellEnv`); add a placed-static floor grid (e.g. `WRIntFloorSTMid01Large 0x1044AA`) |
| Placed light illuminates almost nothing (room feels unlit/flat) | The light base is `PortalStrict` (e.g. `DefaultSunlightHalfOmni01 0x0172C4`) — only lights inside a room portal, and an open cell has no room markers | Use a non-PortalStrict omni shadow light (`WRShadowOmni 0x0C82AE`, radius 512) as the key + non-shadow fills (`WRInteriorLightBrite01 0x06ED46`) |
| `coc`-spawned player lands slightly inside/under the floor | `coc` with no COC marker spawns at cell origin (0,0,0); a floor mesh whose pivot isn't at its top surface puts you in it | `tcl` to check; nudge floor z or add a COC marker. Match floor-tile spacing to the real mesh size or gaps show |
| `coc <interior>` then walk out → terrain LOD breaks at the city gate | `coc` skips the normal load screen, exterior LOD doesn't preload | Fast-travel away + back, OR `coc <exteriorMarker>` directly |

## Items, models, magic effects

| Symptom | Root cause | Fix |
|---|---|---|
| Generated item appears with no model when dropped (or crashes on equip/read) | Weapon/Book/Misc/Potion has no `.nif` — fine in inventory, but CRASHES on any scene interaction (weapon equip, book 3D-read) | Set `template: "<master>:0xFORMID"` to clone a vanilla record's model (IronSword `0x012EB7`, Book1CheapNordsArise `0x0ED161`, GemRuby `0x063B42`, RestoreHealth06 `0x039BE5`) |
| Cloned potion gives a doubled/stacked effect | `DeepCopyIn` keeps the template's own effects, which then stack with the spec's effects | Build does `r.Effects.Clear()` after the clone before wiring spec effects — just don't re-add the template's effect in your spec |
| Custom MGEF heal spell casts but doesn't heal | `Recover` flag on an instant effect reverts the heal the instant the effect "ends" (immediately) | Instant effects (duration 0) must use `["NoDuration","NoArea"]` and NO `Recover`. `Recover` is correct only for a **timed** fortify (e.g. +50 Health/60s) |
| Custom effect costs absurd magicka | High `baseCost` × magnitude under autocalc | Keep `baseCost` low; the autocalc formula multiplies it by magnitude |

## Dialogue

| Symptom | Root cause | Fix |
|---|---|---|
| Custom dialogue records are valid but the topic NEVER appears when you talk to the NPC | Two flags missing: the host **Quest isn't Start Game Enabled** (so it never runs → its dialogue never loads) and/or the **DialogBranch isn't Top-Level** (so the topic is a sub-branch, not a menu option) | Quest `flags |= StartGameEnabled` (+ a `Priority`); branch `Flags = TopLevel`. In ModForge: `quests[].startGameEnabled` (default true) + automatic on the dialogue branch |
| Menu line is mislabelled / wrong text shows | INFO `Prompt` was set, overriding the menu line | Leave INFO `ResponseData` null (uses your own Responses) and `Prompt` null — the menu line comes from `topic.Name` |
| Custom dialogue line shows no audio / NPC lips don't move | No recorded voice (.fuz/.lip) — expected for generated dialogue | Turn **General + Dialogue Subtitles ON** (Settings ▸ Display) — the line still shows as a subtitle and the menu option still works; that's a valid in-game confirmation |
