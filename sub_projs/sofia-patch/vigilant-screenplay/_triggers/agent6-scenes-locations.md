# Agent 6 — Act 1 Scenes & Locations Map
> Generated 2026-06-14 from: scnscan / scenediag on Vigilant.esm (v1.81 English)
> PSC cross-reference: _bsa-psc-cache/sf_zz*aom*.psc
> Location source: game-data/mods/Vigilant/locations.tsv + quests.md

---

## 1. SCENE Inventory — Act 1 (AoM / CO)

All "action" scenes have Package and/or Timer actions (movement, NPC choreography).
"Dialogue" scenes have ONLY Dialog actions in addition to cosmetic Packages.
"Mixed" = Package movement + scripted Dialog exchanges.

| FormID   | EditorID               | Host Quest (FormID / name)               | Type          | Cell / Worldspace                       | Note                                               | Sofia hook?                        |
|----------|------------------------|------------------------------------------|---------------|-----------------------------------------|----------------------------------------------------|------------------------------------|
| 0x00933E | zzAoMMq01Scene01       | 005CE3 zzzAoMMq01 "Squeezer"             | Mixed/Action  | Ivarstead (vanilla worldspace)          | Altano + Priest discuss vampire victims; 6-phase movement+dialogue | Sofia comment: "Is that a priest or a guard? Hard to tell." |
| 0x008851 | zzAoMMq03Scene01       | 00627F zzzAoMMq03 "Lazy Afternoon"       | Mixed/Action  | Windhelm / Candlehearth Hall area       | Altano + InnKeeper 14-phase scene; Packages on phases 0,1,10,11 | **PlayIdle candidate** — long multi-phase scene, Sofia could react to NPC movement |
| 0x009E32 | zzAoMMq05Scene01       | 0098C9 zzzAoMMq05 "Dine and Dash"        | Action        | Stendarr's Beacon area / Riften         | Altano+Jacob movement packages phases 0–9; sets stage 20 | **PlayIdle candidate** — pure Package movement, no blocking dialogue |
| 0x009E4D | zzAoMMq05Scene02       | 0098C9 zzzAoMMq05 "Dine and Dash"        | Mixed/Action  | Riften / Bee and Barb                   | Altano+Daedra movement phases 0–5; Daedra threatens, triggers stage 21/22/25 | Sofia comment on Daedra appearance |
| 0x00B98D | zzAoMMq05Scene03       | 0098C9 zzzAoMMq05 "Dine and Dash"        | Action        | Riften                                  | Keerava Package (hostess) phases 0–1 only | Minor, background NPC movement |
| 0x4CCD7C | zzzAoMMq06Sc01         | 009E68 zzzAoMMq06 "Also sprach Kahjiit"  | **DREAM**     | **0x00185C zzzAoMKahjiitDreamLand** (Jo'vanni Dream Theater) | 1 Timer + 1 Package on alias #0 (Altano); scene sets stage 11; host quest aliases include DreamMarker (00A3CC), CatMarker, Jovanni, Mar'so, Wife, Daedra | **DREAM ENTRY** — player is in Jo'vanni's dream cell; Sofia "enter dream" hook here |
| 0x00E4E4 | zzAoMMq07Scene01       | 00A3FE zzzAoMMq07 "Old Paladin"          | Dialogue      | 0x00185B zzzAoMBeaconBasement (Stendarr's Beacon Basement) | Jacob monologue (sad, vs. Molag Bal visions); 1 Dialog + 1 Package; Interruptable; registers scene-skip at stage 35 | Sofia reaction: overhearing Jacob's guilt |
| 0x00E4F1 | zzAoMMq07Scene02       | 00A3FE zzzAoMMq07 "Old Paladin"          | Mixed/Action  | 0x00185B zzzAoMBeaconBasement           | Full 7-phase Package-driven procession (Altano, Jacob, Daedra1&2, Umbra, Bal); climax dialogue Rahel/Jacob exchange; sets stage 36 / 40 | **PlayIdle candidate** — 7 phases, Sofia could perform idle on phase 0-1 entry |
| 0x00EA62 | zzAoMMq07Scene03       | 00A3FE zzzAoMMq07 "Old Paladin"          | Mixed/Action  | 0x00185B zzzAoMBeaconBasement           | Jacob + BalGhost 6-phase reconciliation scene; sets stage 60 | Sofia emotional reaction to Jacob/Rahel reunion |
| 0x042935 | zzzAoMMq08Scene01      | 00EA8A zzzAoMMq08 "No Mercy"             | Mixed/Action  | Hag's Pond area / zAoMWitchWorld        | Altano (alias #4) Package phases 0–2 + Timer 5s/3s + dialogue "you will kill the witches"; sets stage 230 | Sofia comment on witch hunt briefing |
| 0x011B5C | zzAoMMq09Scene01       | 00EFF7 zzzAoMMq09 "Infinite Falling"     | Action        | 0x025091 zzzAoMTempleOfStendarr (Temple of Stendarr, Stuhn Ravine worldspace) | Altano Package phases 0–2; MolagBal Timers (3s, 1s) phase 1–2; Keeper + Librarian as aliases | **PlayIdle candidate** — multi-phase Altano movement in temple with dramatic timer beats |
| 0x4D2792 | zzzAoMMq10ScWait       | 011B75 zzzAoMMq10 "Landing Spot"         | Action        | 0x025091 zzzAoMTempleOfStendarr / Stuhn Ravine | Librarian Package wait; Molag Bal voice line plays from MolagBalVoiceMarker; sets stage 50 | Sofia hears Molag Bal's curse voiceover |
| 0x4D0399 | zzzAoMMqGEScBad        | 4D0376 zzzAoMMqGoodEnd "Art of Mercy"    | Mixed         | 0x4419CE zzzAoMDreamHole (Dark Hole)    | Daughter Package + Timer; Daughter speaks to dead Mom; sets stage 60 (bad end) | Sofia reaction: child at bad ending |
| 0x4D03B0 | zzzAoMMqGEScKeeperGo   | 4D0376 zzzAoMMqGoodEnd "Art of Mercy"    | Action        | 0x4419CE zzzAoMDreamHole (Dark Hole)    | Keeper Package movement; sets stage 130 | Keeper departs — action-only |
| 0x066A2C | zzzCOq01BalScene01     | 065932 zzzCOMq01 "Child of Oblivion"     | Mixed/Action  | zCOBruiantWorld (Bruiant Estate, 0x047CFA) | BalKillable 2-phase Package; dialogue addressed to player; sets CO ending | Sofia reaction at Bruiant climax |
| 0x4E374B | zzzAomBqCTSc01         | 4E372E zzzAomBountyChickTrader "Bounty:ChickTrader" | Action | Various towns (TownLocation alias) | Orphan Timer 1s; sets stage 20; side bounty quest | Low priority |
| 0x179184 | zzzAoMsqSC01           | 17576E zzzAoMSubQ01 "Witch of Ivarstead" | Dialogue      | 0x16E303 zzzAoMWitchHouse (House of Pond) / zAoMWitchWorld (0x166857) | WitchTA01 taunts player: "Your eyes are so cold"; sets stage 22 | Sofia reaction: witch addresses the player |
| 0x179187 | zzzAoMSQsc02           | 17576E zzzAoMSubQ01 "Witch of Ivarstead" | Dialogue      | 0x16E303 zzzAoMWitchHouse / zAoMWitchWorld | Witch: "If you did not come here, that family never die"; sets stage 24 | Sofia guilt/counter-point comment |
| 0x17918A | zzzAoMSQsc03           | 17576E zzzAoMSubQ01 "Witch of Ivarstead" | Dialogue      | 0x16E303 zzzAoMWitchHouse / zAoMWitchWorld | Witch: "You believe the old fool yet?"; sets stage 26 | Sofia moral philosophy hook |
| 0x17918D | zzzAoMSQsc04           | 17576E zzzAoMSubQ01 "Witch of Ivarstead" | Dialogue      | 0x16E303 zzzAoMWitchHouse / zAoMWitchWorld | Witch: "You are not already human, You are monster"; sets stage 28 | **Best witch scene for Sofia comment** — direct accusation at player |

**Act 1 scene count by type:**
- DREAM: 1 (zzzAoMMq06Sc01)
- ACTION (Package+Timer, no dialogue): 5 (Mq05Sc01, Mq05Sc03, Mq09Sc01, Mq10ScWait, GEScKeeperGo, BqCTSc01)
- MIXED (Package movement + dialogue): 8 (Mq01Sc01, Mq03Sc01, Mq05Sc02, Mq07Sc01, Mq07Sc02, Mq07Sc03, Mq08Sc01, COq01BalScene01, GEScBad)
- DIALOGUE (Dialog actions only): 4 (SQsc01–04, witch interrogation scenes)

---

## 2. LOCATION Table — Act 1

### Worldspaces

| FormID   | EditorID           | Name               | Act 1 Quest / Beat                                      | Sofia realm-comment? |
|----------|--------------------|--------------------|---------------------------------------------------------|----------------------|
| 023E7E   | zAoMVigilantWorld  | Stuhn Ravine       | Mq09 "Infinite Falling" + Mq10 "Landing Spot" — Temple of Stendarr exterior | YES — entering the ravine (the Vigilant's stronghold) |
| 047CFA   | zCOBruiantWorld    | Bruiant's Estate   | **zzzCOMq01 "Child of Oblivion"** — rich estate with Daedra cult; Julius the possessed noble; BalKillable scene | YES — Sofia comment on aristocratic daedra corruption |
| 166857   | zAoMWitchWorld     | Hag's Pond         | **zzzAoMSubQ01 "Witch of Ivarstead"** + zzzAoMMq08 "No Mercy" (witch hunt) | YES — Sofia uneasy near the pond |

### Key Cells

| FormID   | EditorID                | Name                        | Type  | Act 1 Quest / Beat                                           | Sofia realm-comment? |
|----------|-------------------------|-----------------------------|-------|--------------------------------------------------------------|----------------------|
| 00185B   | zzzAoMBeaconBasement    | Stendarr's Beacon Basement  | CELL  | **zzzAoMMq07 "Old Paladin"** — Scenes Mq07Sc01/02/03 all play here; Jacob's breakdown and Bal confrontation | YES — entering the basement (darkness/danger) |
| 00185C   | zzzAoMKahjiitDreamLand  | Jo'vanni Dream Theater      | CELL  | **zzzAoMMq06 "Also sprach Kahjiit"** — THE dream cell; zzzAoMMq06Sc01 plays here; alias DreamMarker anchors it | **PRIMARY DREAM ENTRY CELL** |
| 16E303   | zzzAoMWitchHouse        | House of Pond               | CELL  | **zzzAoMSubQ01 "Witch of Ivarstead"** — 4 witch confrontation scenes (SQsc01–04) | YES — creepy interior, Sofia disturbed |
| 025091   | zzzAoMTempleOfStendarr  | Temple of Stendarr          | CELL  | zzzAoMMq09 + Mq10 — Altano and librarian scenes; final confrontation | YES — holy ground, Sofia reverent/nervous |
| 4419CE   | zzzAoMDreamHole         | Dark Hole                   | CELL  | **zzzAoMMqGoodEnd "Art of Mercy"** — GE ending scenes; Daughter/Keeper scenes; endgame beat | YES — bleak ending location |
| 049921   | zzzAoMThiefHouse        | Winch's House               | CELL  | zzzAoMMq06 "Also sprach Kahjiit" — Jo'vanni's den pre-dream | Sofia comment on Khajiit squalor |
| 0243FB   | zAoMTempleStendarrEntrance | (Stuhn Ravine entrance)  | CELL  | Mq09/Mq10 exterior approach                                  | Sofia comment on ravine approach |

### Bruiant / Hag's Pond / Beacon Confirmations

**Bruiant's Estate (0x047CFA zCOBruiantWorld):**
Quest: `065932 zzzCOMq01 "Child of Oblivion"` — NOT Mq01 "Squeezer". Mq01 is set in Ivarstead (vampire case). Bruiant is the CO sub-arc about a noble mansion with a Daedra cult (Julius + Julia). BalKillable scene (0x066A2C zzzCOq01BalScene01) fires here. Sofia comment hook: entering Bruiant's Estate worldspace.

**Hag's Pond (0x166857 zAoMWitchWorld + House of Pond 0x16E303):**
Quest: `17576E zzzAoMSubQ01 "Witch of Ivarstead"` (the 4-scene witch interrogation at House of Pond) AND `00EA8A zzzAoMMq08 "No Mercy"` (Mq08Sc01 = Altano's mission briefing "you will kill the witches"). The witch scenes (SQsc01–04) are Dialogue-type in cell 0x16E303 inside worldspace 0x166857. Sofia realm-comment gates on CELL 0x16E303 OR LCTN 0x26D3B6 (zzzAoMLocWitchIsland).

**Stendarr's Beacon Basement (0x00185B zzzAoMBeaconBasement):**
Quest: `00A3FE zzzAoMMq07 "Old Paladin"` — all three Mq07 scenes (Sc01 Jacob monologue, Sc02 procession/Rahel confrontation, Sc03 Jacob+BalGhost reconciliation) play here. Sofia realm-comment gates on CELL 0x00185B OR LCTN 0x038521 (zzzAoMLocBeaconBasement).

**Dream cell (0x00185C zzzAoMKahjiitDreamLand):**
Quest: `009E68 zzzAoMMq06 "Also sprach Kahjiit"` — scene 0x4CCD7C (zzzAoMMq06Sc01) has 1 Timer + 1 Package; the host quest aliases include DreamMarker (forcedRef=00A3CC) and CatMarker (forcedRef=02263A:Skyrim.esm). The LCTN record 0x37FBBC (zzzAoMLocKhajiitDreamLand) also maps to "Jo'vanni Dream Theater". Entering this interior = inside a Khajiit's shared-trauma dream.

---

## 3. Mechanic Hooks

### A. "Enter Dream" — Sofia phantom appearance in Jo'vanni's dream

**Best target:**
- Cell: `0x00185C zzzAoMKahjiitDreamLand` ("Jo'vanni Dream Theater")
- Scene: `0x4CCD7C zzzAoMMq06Sc01` — host quest `009E68 zzzAoMMq06`
- Trigger stage: **scene completion sets stage 11** on Mq06 → use `OnStageSet(9, 11)` or `OnCellAttach` gated on `IsInCell(zzzAoMKahjiitDreamLand)`
- Mechanic: player enters the dream cell during Mq06; Sofia can appear as a phantom (optional alias or dialogue trigger) commenting on the Khajiit's shared grief; best hooked AFTER scene sets stage 11 so the main scene is complete and Sofia can interrupt without blocking
- Scene fragment: `SF_zzzAoMMq06Sc01_024CCD7C` → Fragment_0 fires at scene end (SetStage 11); Sofia hook fires at stage 12+ or via a SMBN child event

### B. "PlayIdle Action" — Sofia performs an animation during a Package-driven scene

**Best option 1: zzAoMMq03Scene01 (0x008851)**
- Quest: `00627F zzzAoMMq03 "Lazy Afternoon"`, 14-phase scene at Candlehearth Hall / Windhelm
- Phases 0–1 are setup Packages (Altano + InnKeeper walk to position); phases 2–9 are staged dialogue
- Sofia PlayIdle opportunity: phase 0 (scene start, before Altano speaks) — she could perform a "look around" or "lean on wall" idle while NPCs march to their marks
- Scene fragment: `SF_zzAoMMq03Scene01_01008851` Fragment_3 fires at stage 11 (phase 1 complete)

**Best option 2: zzAoMMq07Scene02 (0x00E4F1)**
- Quest: `00A3FE zzzAoMMq07 "Old Paladin"`, cell `0x00185B` Beacon Basement
- 7-phase scene; phase 0 = all 5 NPCs (Altano, Jacob, Daedra1, Daedra2, Umbra) walk to positions; phase 1 onward = Rahel/Jacob/Bal dialogue
- Sofia PlayIdle opportunity: phase 0 entry (before any NPC speaks) — she could PlayIdle "fear/alarm" while the procession forms
- Scene fragment: `SF_zzAoMMq07Scene02_0100E4F1` Fragment_0 fires at stage 40 (all dialogue complete)
- Note: this is the most cinematically loaded scene in Act 1; a Sofia idle here has maximum emotional weight
