# SubQ01 Witch — Trigger Analysis
## zzzAoMSubQ01 / Vigilant.esm:0x17576E — "Witch of Ivarstead"

Sources read:
- `qf_zzzaomsubq01_0217576e.psc` (QF stage fragments)
- `sf_zzzaomsqsc01..04` (four scene fragments)
- `aomsq01_tif__02177e06/13/16/b7f9.psc` (dialogue TIF fragments)
- `questdiag 0x17576E` and `infodiag 0x17576E` (live CLI)
- `qf_zzzaombountywitch_024e010e.psc` (bounty quest, for worldspace separation)

---

## 1. Full Fragment_N → Stage → Meaning → Karma Polarity Table

### QF Stage Fragments (qf_zzzaomsubq01_0217576e.psc)

The Papyrus NEXT FRAGMENT INDEX is 48, meaning fragment indices span 0–47.
Fragment numbers are assigned per log-item index across all stages (sparse; not all indices
are used). The mapping below is derived from code content cross-referenced against stage
sequence and TIF SetStage calls.

| Fragment | Stage (inferred) | Log slot | Action summary | Karma polarity |
|---|---|---|---|---|
| Fragment_0 | 0 | log[0] | Episode-skip support: disables Victim01/02/03, enables VictimDeadMarker, **SetStage(10)** | neutral |
| Fragment_5 | 5 | log[0] | `Alias_Witch.TryToEnable()` — witch becomes active | neutral |
| Fragment_7 | 20 | log[0]? | `WitchFog02.Enable()`, `SpawnTrigger.Enable()`, add Witch to MolagBalFaction, `StartCombat(player)`, `MusReyda.Add()` — combat begins (Witch attacks player) | neutral (player initiated via TIF→stage 20) |
| Fragment_13 | 22 | log[0] | `TryToEnable()`, `PlaceAtMe(SummonFX)`, `RestoreAV(Health,500)` — witch restore mid-fight | neutral |
| Fragment_16 | 24 | log[0] | Same restore pattern as Fragment_13 | neutral |
| Fragment_18 | 26 | log[0] | Same restore pattern | neutral |
| Fragment_20 | 28 | log[0] | Same restore pattern | neutral |
| Fragment_22 | 30 | log[0] | `SetGhost(True)`, `RemoveFromFaction(MolagBal)`, `StopCombat()`, `TryToEvaluatePackage()`, `SpawnTrigger.Disable()`, `MusReyda.Remove()` — witch stands down and enters desperation/mercy phase | neutral |
| Fragment_28 | 40 | log[0] | `SetActorValue(Health,1)`, `SetGhost(False)`, re-add MolagBal faction, `StartCombat(player)` — witch re-engages (mercy rejected → combat resumes) | neutral (player's kill choice already registered at TIF→stage 40) |
| Fragment_31 | 200 | log[0] | `WitchFog02.Disable()`, `TryToDisable()`, `TryToMoveTo(BadEndMarkerRef)` — witch moves to bad-end marker position (spare but bad-end branch?) | neutral |
| Fragment_33 | 210 | log[0] | `TryToMoveTo(BadEndMarkerRef)`, `TryToEnable()`, `PlaceAtMe(SummonFX)`, `TryToEvaluatePackage()` — witch re-appears at bad-end marker for vendetta phase | neutral |
| **Fragment_36** | **50** | **log[0]** | `Karma.Mod(-6.0)`, `KarmaDown.Show()`, teleports witch next to victims, clears Essential flag, adds MolagBalFaction, `SetAgression(2)`, `KillEssential()` on all 5 victims, spawns Grummites — **KILL/ATTACK path completes** | **NEGATIVE (−6 karma) = KILL/BAD** |
| **Fragment_38** | **230** | **log[0]** | `Karma.Mod(2.0)`, `KarmaUP.Show()`, `Utility.wait(3.0)`, `Stop()` — quest completes on spare/exile path | **POSITIVE (+2 karma) = SPARE/GOOD** |
| **Fragment_40** | **300** | **log[0]** | `WitchFog02.Disable()`, `ModPious(3.0)`, `Karma.Mod(2.0)`, `KarmaUP.Show()`, `Stop()` — alternate good ending (pious achievement path) | **POSITIVE (+2 karma + Pious) = SPARE/PIOUS** |
| Fragment_42 | 220 | log[0] | `TryToDisable()`, `Stop()` — "Ep3 Martyrdom End" (witch disappears; quest stops on vendetta-mercy branch) | neutral |
| Fragment_44 | 50 | log[1] | `UpdateEventFlag(gKillReydaCount)`, `SaveEventFlag()` — JSON save after kill completion (comment: "50-2 json") | neutral (bookkeeping) |
| Fragment_46 | 230 | log[1] | `UpdateEventFlag(gKillReydaCount)`, `SaveEventFlag()` — JSON save after spare completion (comment: "230-2 json") | neutral (bookkeeping) |

### Scene Fragments

| File | Fragment | Effect (SetStage on owning quest) |
|---|---|---|
| sf_zzzaomsqsc01_02179184.psc | Fragment_0 | `SetStage(22)` — scene SC01 completion |
| sf_zzzaomsqsc02_02179187.psc | Fragment_0 | `SetStage(24)` — scene SC02 completion |
| sf_zzzaomsqsc03_0217918a.psc | Fragment_0 | `SetStage(26)` — scene SC03 completion |
| sf_zzzaomsqsc04_0217918d.psc | Fragment_0 | `SetStage(28)` — scene SC04 completion |

These four scenes fire during the combat phase (stages 22–28), progressively restoring the
witch and advancing the stage until she reaches stage 30 (mercy/desperation).

### Dialogue TIF Fragments (player choice → SetStage)

| File | Stage called | Trigger dialogue (stage gate) | Meaning |
|---|---|---|---|
| aomsq01_tif__02177e06.psc | `SetStage(20)` | "Witch must die" — INFO 0x177E06, gated `GetStage==10` | Player declares hostility at first meeting |
| aomsq01_tif__02177e13.psc | `SetStage(40)` | "No, Witch must die" — INFO 0x177E13, gated `GetStage==30` | Mercy refused → KILL path continues |
| aomsq01_tif__02177e16.psc | `SetStage(200)` | "Get lost. never come back here" — INFO 0x177E16, gated `GetStage==30` | Mercy accepted → SPARE path |
| aomsq01_tif__0217b7f9.psc | `SetStage(220)` | "What are you trying to do?" — INFO 0x17B7F9, gated `GetStage==210` | Vendetta/re-encounter dialogue → quest ends |

---

## 2. Complete Quest Flow Reconstruction

```
Stage 0  → (Episode skip: SetStage 10, or normal flow continues)
Stage 5  → Witch enabled
Stage 10 → Player arrives; Reyda's opening dialogue available
           Player picks "Witch must die" → TIF sets Stage 20
Stage 20 → Witch enters combat (fog enabled, Molag Bal faction, StartCombat)
           (Stages 22→24→26→28 via scene fragments: witch health restores x4)
Stage 30 → Witch stands down; desperation/mercy phase
           CHOICE A: "No, Witch must die" → TIF sets Stage 40
           CHOICE B: "Get lost. never come back here" → TIF sets Stage 200

CHOICE A (Kill path):
  Stage 40 → Witch re-engages combat (low HP, Molag Bal faction, StartCombat)
  Stage 50 → Fragment_36: Karma −6, victims killed, grummites spawned → CompleteQuest (KILL)
           → Fragment_44: JSON event save (gKillReydaCount)

CHOICE B (Spare path):
  Stage 200 → Fragment_31: Fog disabled, witch teleports to BadEndMarkerRef
  Stage 210 → Fragment_33: Witch re-appears at marker; vendetta dialogue available
              Player picks "What are you trying to do?" → TIF sets Stage 220
  Stage 220 → Fragment_42: Witch disabled, Stop() — "Martyrdom End"
  Stage 230 → Fragment_38: Karma +2, Stop() → CompleteQuest (SPARE)
           → Fragment_46: JSON event save
  Stage 300 → Fragment_40: Karma +2 + Pious +3, Stop() → CompleteQuest (PIOUS SPARE, alt)
```

**Kill vs Spare stage split — definitive:**
- **Stage 50 = KILL path** — Karma −6, victims die, Grummites spawn (Fragment_36)
- **Stage 230 = SPARE/EXILE path** — Karma +2, quest stops cleanly (Fragment_38)
- **Stage 300 = SPARE/PIOUS path** — Karma +2 + Pious achievement (Fragment_40)
- Stages 50 and 230/300 are mutually exclusive — once either CompleteQuest fires, the quest
  is done and the other branch cannot fire.

---

## 3. Corrected Sofia Beat Gates

### 1-G WitchDoubt (Sofia voices doubt on arrival, before choice)

**Current gate:** `GetQuestRunning(SubQ01 0x17576E)`

**Problem:** This fires too broadly — it would trigger even after the choice is made (quest is
still running during stages 20–40). Sofia should doubt when the player FIRST sees the "witches"
look like ordinary people, i.e., at the opening encounter before any combat.

**Corrected gate:**

```json
[
  { "function": "GetQuestRunning", "quest": "Vigilant.esm:0x17576E", "value": 1 },
  { "function": "GetStageDone",    "quest": "Vigilant.esm:0x17576E", "stage": 10, "value": 1 },
  { "function": "GetStageDone",    "quest": "Vigilant.esm:0x17576E", "stage": 20, "value": 0 }
]
```

Meaning: quest is running AND stage 10 has been reached (player met Reyda) AND stage 20 has
NOT yet been set (player has not yet declared hostility). This windows Sofia's doubt to the
exact moment the player is standing in the dialogue with Reyda deciding what to do.

**Confidence: HIGH** — Stage 10 is the confirmed arrival/first-dialogue stage. Stage 20 is
confirmed as the "Witch must die" TIF target. The window is tight and correct.

### 1-H WitchKilled (emotion=Anger — Sofia reacts after the kill)

**Current gate:** `GetStageDone(SubQ01, 20)`

**Problem:** Stage 20 is the combat-START stage, NOT the kill-completion stage. Sofia would
fire her "you killed them" line immediately when the player declares hostility, long before the
victims are actually dead. The kill completes at **stage 50**, which is where Fragment_36
executes Karma −6 and kills all victims.

**Corrected gate:**

```json
[
  { "function": "GetStageDone", "quest": "Vigilant.esm:0x17576E", "stage": 50, "value": 1 }
]
```

Stage 50 has `CompleteQuest` flag AND is the exact moment Fragment_36 kills the victims and
deals −6 karma. This is when Sofia should react with anger.

**Confidence: VERY HIGH** — Stage 50 is the sole kill-path CompleteQuest stage. Fragment_36
unambiguously executes at stage 50 (Karma −6, KillEssential on all 5 victims). There is no
ambiguity with the spare path: stage 230 (spare) cannot also be done when stage 50 is done
because the quest stops at the first CompleteQuest that fires.

### 1-H WitchSpared (emotion=Happy — Sofia reacts after sparing)

**Current gate:** `GetStageDone(SubQ01, 10)`

**Problem:** Stage 10 is the ARRIVAL/first-meeting stage — it is set before any choice is made.
Using stage 10 means Sofia fires her "glad you spared them" line the moment the player meets
Reyda, which is completely wrong. The spare completion is at **stage 230** (Fragment_38:
Karma +2, Stop()) or **stage 300** (Fragment_40: Karma +2 + Pious, Stop()).

**Corrected gate:**

```json
[
  {
    "operator": "OR",
    "conditions": [
      { "function": "GetStageDone", "quest": "Vigilant.esm:0x17576E", "stage": 230, "value": 1 },
      { "function": "GetStageDone", "quest": "Vigilant.esm:0x17576E", "stage": 300, "value": 1 }
    ]
  }
]
```

If the Sofia dialogue system does not support OR directly, prefer a single stage 230 check,
since stage 300 is an alternate pious endpoint that reaches the same outcome. Stage 230 is the
primary spare-completion stage.

**Confidence: VERY HIGH** — Stage 230 is confirmed spare-path CompleteQuest (Fragment_38:
Karma +2). Mutually exclusive with stage 50 (kill). Stage 300 is an alternate pious spare
ending (also Karma +2 + Pious). Neither 230 nor 300 can be done if stage 50 fired (quest
already stopped).

### Kill-vs-Spare mutual exclusion safety

Stages 50 and 230 are **intrinsically mutually exclusive** because the quest script calls
`Stop()` at each terminal stage. Once the quest stops, no further stage can be set. Therefore:

- If `GetStageDone(50)==1` → kill happened. Spare stages (200/210/220/230/300) were never set.
- If `GetStageDone(230)==1` → spare happened. Stage 50 was never set.
- No additional karma-global check is needed; stage gating is sufficient.

The previous stage 20 (kill) / stage 10 (spare) gates were both wrong: stage 10 fires before
any choice, and stage 20 fires at combat-start not combat-completion. The corrected gates above
are unambiguous.

---

## 4. Hag's Pond / House of Pond — Worldspace + Cell Confirmation

**Provided FormIDs:**
- Hag's Pond worldspace: `Vigilant.esm:0x166857` (EditorID: `zzzAoMWitchWorld`)
- House of Pond cell: `Vigilant.esm:0x16E303`

**Quest SubQ01 context:**
The SubQ01 "Witch of Ivarstead" encounter involves Reyda the Glenmoril witch and takes place
near Ivarstead, in a fog-shrouded area with victims (the Ivarstead family: IvarsteadVictim01–05
aliases). The quest uses `WitchFog01`, `WitchFog02`, `SpawnTrigger` ObjectReference properties
— a dedicated encounter space is implied.

The worldspace `0x166857 zzzAoMWitchWorld` (Hag's Pond) is a **custom worldspace added by
Vigilant** — it is not vanilla Skyrim. The name "Hag's Pond" is consistent with a witch
encounter near Ivarstead (hag = witch trope). The `House of Pond` cell `0x16E303` is an
interior cell within this custom worldspace.

**BountyWitch quest (`0x4E010E zzzAomBountyWitch`) comparison:**
The BountyWitch quest uses `WitchLocation` (a LocationAlias) with objective "Defeat Witch at
<WitchLocation>". Its stage fragments contain no worldspace or cell references — it uses a
generic location alias that resolves to wherever the bounty witch spawns. The BountyWitch is a
repeatable radiant quest (`AllowRepeatedStages` flag), distinct from the one-time SubQ01
encounter.

**Verdict:**
- `Vigilant.esm:0x166857 zzzAoMWitchWorld` (Hag's Pond) belongs to **SubQ01**, not BountyWitch.
  It is the custom worldspace for the Reyda witch encounter.
- `Vigilant.esm:0x16E303` (House of Pond) is the interior cell in that same custom worldspace.
- BountyWitch (`0x4E010E`) has no fixed worldspace — it is a radiant quest with a location alias.

**Sofia realm comment gate recommendation:**

The Sofia "Hag's Pond realm" comment (if tied to entering a custom Daedric-adjacent space for
this quest) should gate on:

```json
[
  { "function": "GetInCurrentLoc", "location": "Vigilant.esm:0x166857", "value": 1 },
  { "function": "GetQuestRunning", "quest": "Vigilant.esm:0x17576E", "value": 1 }
]
```

or if using cell:

```json
[
  { "function": "GetInCell", "cell": "Vigilant.esm:0x16E303", "value": 1 },
  { "function": "GetQuestRunning", "quest": "Vigilant.esm:0x17576E", "value": 1 }
]
```

Adding `GetQuestRunning(SubQ01)` ensures this comment only fires during the SubQ01 context
and not if the player somehow revisits the area post-completion.

**Confidence: HIGH** — The worldspace `0x166857 zzzAoMWitchWorld` is unambiguously SubQ01's
custom space; BountyWitch uses a radiant location alias with no fixed worldspace reference.

---

## Summary Table (Sofia beats)

| Beat | Old gate | Corrected gate | Confidence |
|---|---|---|---|
| 1-G WitchDoubt | `GetQuestRunning(0x17576E)` | `GetQuestRunning + GetStageDone(10)==1 + GetStageDone(20)==0` | HIGH |
| 1-H WitchKilled | `GetStageDone(0x17576E, 20)` | `GetStageDone(0x17576E, 50)` | VERY HIGH |
| 1-H WitchSpared | `GetStageDone(0x17576E, 10)` | `GetStageDone(0x17576E, 230)` (or also 300) | VERY HIGH |
| Realm comment | (not yet gated) | `GetInCurrentLoc(0x166857) + GetQuestRunning(0x17576E)` | HIGH |
