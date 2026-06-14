# Agent 3 — Mq07 Beacon Massacre Trigger Decode
Quest: `zzzAoMMq07` "Old Paladin" — `Vigilant.esm:0x00A3FE`

Sources verified: QF script, 3 SF scene scripts, all aom07 TIF scripts, scenediag, infodiag, locations.tsv.

---

## 1. Stage → Meaning Table

| Stage | Meaning | Key Evidence |
|------:|---------|-------------|
| 0 | Quest starts (fired when Mq06 ends). Dead Vigilants replaced by DeadMarker. Umbra (Ebony Knight) guard at entrance warns player back. Obj0 "Talk to Jacob" displayed. | QF Fragment_0: `Alias_Victim1/2/3.TryToDisable(); DeadMarker.Enable(); SetObjectiveDisplayed(0)` |
| 10 | Umbra greet dialogue ends (player acknowledged Umbra's warning). Obj10 "Defeat Ebony Knight" displayed. | TIF_0100EA72 OnEnd → `SetStage(10)`; QF: `SetObjectiveDisplayed(10)` |
| 20 | **Player arrives at Beacon main hall; finds Jacob wounded among the dead.** Obj0 "Talk to Jacob" auto-completed. Jacob's greet ("Uuu...") + massacre conversation unlocked. | QF: `SetObjectiveCompleted(0)`; info 0x00EA7B gated `GetStage == 20` |
| 30 | Jacob's full massacre account heard. Player chose "What do you mean?" goodbye line. Jacob revealed Molag Bal altar under beacon. | TIF_0100EA7F OnEnd → `SetStage(30)` |
| 33 | Altano's support dialogue done ("Support Jacob. I come see how to go ahead"). Obj33 "Support Jacob" displayed. Jacob lore questions unlocked (stages 30–35). | TIF_0211E0AC OnEnd → `SetStage(33)`; QF: `SetObjectiveDisplayed(33)` |
| 35 | Scene01 (Jacob's Molag Bal internal monologue) ends. Actors repositioned for basement descent. Scene02 registered. | SF_Scene01 Fragment_0: `RegisterSceneSkip(quest, self, 35, True)` |
| 36 | **Scene02 Phase 1 ends: Umbra (Ebony Knight) defeated in basement Hall of Molag Bal.** Obj10 "Defeat Ebony Knight" completed. Umbra death effect plays. Player + Altano receive KeyBeacon. | SF_Scene02 Fragment_1 → `SetStage(36)`; QF: `SetObjectiveCompleted(10); UmbraDeathEffect; AddItem KeyBeacon` |
| 37 | Intermediate stage; no QF fragment (likely Scene02 combat phase transition). | No fragment found in psc cache |
| 38 | **Death-of-Jacob branch only.** Jacob fatally wounded during fight; Rahel's ghost (BalGhost) briefly appears then is disabled. Jacob killed (`KillEssential`). Obj60 "Talk to Altano" displayed early. Altano package re-evaluated. | QF Fragment_16: `BalGhost.TryToDisable(); Jacob.KillEssential(); SetObjectiveDisplayed(60)` |
| 40 | **Boss fight starts.** Scene02 fully completes. Bal + Daedra1 + Daedra2 added to StendarrEnemy faction. Hall door locked. BanishTRGs disabled. Obj40 "Defeat Bal" displayed. | SF_Scene02 Fragment_0 → `SetStage(40)`; QF: `SetObjectiveDisplayed(40); enemies hostile; Lock(true)` |
| 50 | **Bal (boss) defeated.** Obj40 "Defeat Bal" completed. Daedra1 and Daedra2 killed. BalGhost moves to Bal. Scene03 (Jacob + Rahel ghost reconciliation dream sequence) force-starts. | QF Fragment_6: `SetObjectiveCompleted(40); Daedra1/2.TryToKill(); AoMMq07Scene03.ForceStart()` |
| 60 | Scene03 ends. Obj60 "Talk to Altano" displayed. Altano package re-evaluated (moves to player). | SF_Scene03 Fragment_0 → `SetStage(60)` |
| 70 | Altano's "All is gone..." dialogue ends (stage 60 Goodbye TIF). Player must return mace. Obj60 completed, Obj70 "Take Mace of Molag Bal to Altano" displayed. | TIF_0100EA88 OnEnd → `SetStage(70)`; QF: `SetObjectiveCompleted(60); SetObjectiveDisplayed(70)` |
| 75 | Player delivers mace to Altano ("I will back to the temple..."). Follow-up quest (Mq08 witch hunt) queued. | TIF_0200EA89 OnBegin → `SetStage(75)` |
| 80 | Quest complete. Mq08 starts. `CompleteQuest` flag. | TIF_01027A47 OnEnd → `SetStage(80)`; QF: `AoMMq08.Start(); Stop()` |
| 255 | Shutdown. Dremoras disabled, Umbra repositioned. | QF Fragment_23/30: `Daedra1/2.TryToDisable(); Umbra cleanup`; `ShutDownStage` flag |
| 9999 | Alternate complete (skip path). | QF Fragment_28: full cleanup + `Stop()`; `CompleteQuest` flag |

**Marked stages:**
- **Arrive/discover massacre = Stage 20** (player at main Beacon hall, Jacob wounded, massacre conversation)
- **Boss (Bal) defeated = Stage 50** (Obj40 completed, Scene03 starts)

---

## 2. Corrected Gates for 1-E and 1-F

### 1-E BeaconMassacre (sombre, emotion=Sad)

**Current (incorrect) gate:** `GetQuestRunning(Mq07)` — too broad; fires as soon as Mq06 ends, before player reaches Beacon.

**Corrected gate:**
```json
[
  {
    "function": "GetStageDone",
    "quest": "Vigilant.esm:0x00A3FE",
    "stage": 20,
    "operator": "EqualTo",
    "value": 1
  }
]
```

**Rationale:** Stage 20 is the precise moment the player has physically arrived at Stendarr's Beacon main hall and triggered the start of Jacob's conversation (Obj0 "Talk to Jacob" auto-completes). The bodies are visible; Jacob says "Attacked by the summoner... All is dead except me." Stage 20 requires the player to have traveled to the Beacon AND initiated contact with Jacob — it is the earliest point that unambiguously means "player sees the massacre."

**GetInCell(0x00185B) gate for 1-E: NOT recommended.**
- `0x00185B zzzAoMBeaconBasement` is the **basement** (Hall of Molag Bal), where stages 35–60 happen.
- Stage 20 (massacre discovery) happens in the **main Beacon hall**, which uses either vanilla Skyrim.esm Stendarr's Beacon interior or exterior cell `0x00BD6F StendarrsBeaconExterior01`.
- Adding `GetInCell(0x00185B)` to 1-E would **suppress** the trigger (player is never in basement at stage 20).
- If an additional location guard is desired, use the `Alias_StendarrBeaconLocation` (LocationAlias on the quest) or check `GetQuestRunning(Mq07)` as an upper bound.

**Confidence: HIGH.**  
Evidence chain: QF Fragment_0 sets up massacre scene at stage 0; info 0x00EA7B gates on `GetStage == 20` for Jacob's greet; info 0x00EA7D (Jacob explains massacre) has no extra gate beyond alias; stage 20 BEGIN auto-completes Obj0. All three TIF scripts confirm stage set order.

---

### 1-F BeaconBoss (reflective)

**Current (incorrect) gate:** `GetStageDone(Mq07, 40)` — **stage 40 = Bal fight STARTS**, not boss defeated.

**Corrected gate:**
```json
[
  {
    "function": "GetStageDone",
    "quest": "Vigilant.esm:0x00A3FE",
    "stage": 50,
    "operator": "EqualTo",
    "value": 1
  }
]
```

**Rationale:** Stage 40 fires when Scene02 (the Bal confrontation dialogue) completes and enemies become hostile — it is the *beginning* of the boss fight, not the end. Stage 50 is set when Bal is killed (via `aomsetstageonbleedout` or equivalent), completing Obj40 "Defeat Bal" and triggering daedra kills + Jacob's reconciliation dream scene (Scene03). Sofia's "reflective" beat should fire after the fight resolves, which is stage 50.

**Confidence: HIGH.**  
Evidence chain: QF Fragment at stage 40: `SetObjectiveDisplayed(40); enemies.AddToFaction(StendarrEnemy); HallDoor.Lock(True)` = fight start. QF Fragment at stage 50: `SetObjectiveCompleted(40); Daedra1/2.TryToKill(); AoMMq07Scene03.ForceStart()` = boss defeated + post-fight scene starts. Distinction is unambiguous.

---

## 3. Relevant Cell / WorldSpace FormIDs

| FormID | Type | Name | Relevance |
|--------|------|------|-----------|
| `Vigilant.esm:0x00185B` | CELL | `zzzAoMBeaconBasement` — Stendarr's Beacon Basement | Stages 35–60: basement descent, Scene02 (Ebony Knight + Bal fight), Scene03 (dream). Applies to 1-F context only. |
| `Vigilant.esm:0x00BD6F` | CELL | `StendarrsBeaconExterior01` | Beacon exterior area. Quest setup and Umbra encounter (stages 0–10) may start here. |
| `Vigilant.esm:0x038521` | LCTN | `zzzAoMLocBeaconBasement` | Location record for basement; could be used in `GetInCell`-equivalent location check for 1-F. |
| `Skyrim.esm:0x?` | CELL | Stendarr's Beacon (vanilla interior) | Main hall where massacre bodies are visible and Jacob is found (stage 20). FormID is in Skyrim.esm — not available without Skyrim.esm dump, but the `Alias_StendarrBeaconLocation` LocationAlias on Mq07 wraps this. |

**Note on 0x00185B for 1-E:** As documented above, the basement cell does NOT apply to the massacre discovery beat. Stage 20 (discovery) occurs in the main Beacon interior, not the basement. Do not gate 1-E on `GetInCell(0x00185B)`.

**Note on 1-F location:** Stage 50 (Bal defeated) occurs inside `zzzAoMBeaconBasement` (0x00185B). If a belt-and-suspenders location check is wanted for 1-F, `GetStageDone(Mq07, 50)` alone is sufficient — adding `GetInCell(0x00185B)` would be redundant (Sofia is in the basement at that point) but not harmful.

---

## Appendix: Fragment → Stage Derivation Notes

Fragment indices in the QF psc were resolved by **content, not index arithmetic** (compiler output order is non-sequential). Key anchors:

- `TIF_0100EA72` OnEnd → `SetStage(10)` — confirms stage 10 = after Umbra greet
- `TIF_0100EA7F` OnEnd → `SetStage(30)` — confirms stage 30 = Jacob account heard
- `TIF_0211E0AC` OnEnd → `SetStage(33)` — confirms stage 33 = Altano support
- `SF_Scene02 Fragment_1` → `SetStage(36)` — confirmed stage 36 = Scene02 Phase 1 end
- `SF_Scene02 Fragment_0` → `SetStage(40)` — confirmed stage 40 = Scene02 complete = fight start
- `SF_Scene03 Fragment_0` → `SetStage(60)` — confirmed stage 60 = post-fight scene end
- QF content with `SetObjectiveCompleted(40)` + `ForceStart Scene03` → stage 50 (Bal defeated, no other candidate)
- QF content with `KillEssential(Jacob)` + `SetObjectiveDisplayed(60)` → stage 38 (Jacob death branch)
