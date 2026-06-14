# Agent 2 — Mq06 "Also sprach Kahjiit": Stage Decoding + Dream-Entry Map + Sofia Beat Gate

Sources used:
- `_bsa-psc-cache/qf_zzaommq06_01009e68.psc` — QF stage fragments (main quest)
- `_bsa-psc-cache/qf_zzzaommq06badend_024cdf8d.psc` — BadEnd quest fragments
- `_bsa-psc-cache/sf_zzzaommq06sc01_024ccd7c.psc` — Scene Sc01 fragment
- `_bsa-psc-cache/aommq06jovannionhitscript.psc` — Jo'vanni hit-event script (alias script)
- `_bsa-psc-cache/aommq06_tif__024cdf7e.psc` — TIF for cult dialogue (OnBegin: display obj 21)
- `_bsa-psc-cache/aommq06_tif__024cdf82.psc` — TIF for cult dialogue (OnEnd: SetStage(21))
- `act-1-sq-06-kahjiit.md` — reconstruction notes
- CLI `questdiag`, `scenediag`, `infodiag`, `cellrefs`, `refpos` output

---

## 1. Stage → Meaning Table

Quest: `zzzAoMMq06` "Also sprach Kahjiit" (`Vigilant.esm:0x009E68`)

| Stage | QF Fragment | Key actions | Meaning |
|------:|------------|-------------|---------|
| 0 | Fragment_0 | SetObjectiveDisplayed(0); SetActive; move Bal+Umbra to BalMarker; enable Cultist | Quest start; player sees "Talk to Altano" |
| 10 | Fragment_2 | SetObjectiveCompleted(0); SetObjectiveDisplayed(10); **Sc01.ForceStart()**; Altano.TryToEvaluatePackage | Sc01 scene starts (leads Altano to Ragged Flagon); "Meet Altano in the Ragged Flagon" objective |
| 11 | Fragment_4 | SetObjectiveCompleted(10); SetObjectiveDisplayed(20); Jovanni.TryToMoveTo(CatMarker) | Sc01 phase completes (SF_zzzAoMMq06Sc01 Fragment_0 sets stage 11); Jovanni repositioned; "Find Jo'vanni" objective |
| 20 | _(no fragment / objectives only)_ | — | Player in Ratway; must talk to Jo'vanni |
| 21 | Fragment_33 | AddItem(StaffMeridia); SetObjectiveCompleted(21) | Player accepted Meridia-cult blessing; receives Meridia Staff |
| 22 | Fragment_6 | SetObjectiveFailed(21) if displayed; SetObjectiveCompleted(20); SetObjectiveDisplayed(25); Jovanni.StartCombat(player) | Player chose to attack Jo'vanni (non-staff weapon triggers stage 25 via OnHit; this stage may be a mid-transition) |
| **25** | **Fragment_25** | SetObjectiveCompleted(25); AddPerk(NoPickPocket); FadeOutGame; **Player.MoveTo(DreamMarker)** | **ENTER DREAM** — player is teleported into cell `0x00185C zzzAoMKahjiitDreamLand "Jo'vanni Dream Theater"`; set by `JovanniOnHitScript.OnHit` when player hits Jo'vanni with any weapon (OR after dialogue completes) |
| 30 | Fragment_8 | Wife(Campaner'Ra alias).TryToDisable; Marso.TryToEnable; Marso.SetEssential(True) | **INSIDE DREAM** — Campaner'Ra morning scene begins; dream stage 1 (wake-up) |
| 35 | Fragment_27 | Wife.TryToEvaluatePackage | **INSIDE DREAM** — dream stage 2 (breakfast / soup scene) |
| 40 | _(no fragment)_ | — | **INSIDE DREAM** — Mar'so pelt-obsession scene (dialogue only) |
| 50 | Fragment_10 | SetObjectiveDisplayed(50); Marso.TryToDisable; Daedra.TryToEnable; Daedra.StartCombat(player); RemovePerk(NoPickPocket) | **INSIDE DREAM** — dream ends in violence; Daedra manifests for combat |
| 55 (inferred) | Fragment_12 | SetObjectiveCompleted(50); SetObjectiveDisplayed(60); **Player.MoveTo(CatMarker = Ragged Flagon)** [FadeOut sequence]; Marso.TryToEnable; Marso.SetEssential(False) | **EXIT DREAM** — player teleported back to Ragged Flagon area; "Retrieve Campaner'Ra from Mar'so" objective |
| 60 | Fragment_20 | Marso.AddToFaction(StendarrEnemy); Marso.StartCombat(player) | Mar'so becomes hostile |
| 65 | Fragment_14 | If not stage 79: SetObjectiveCompleted(60); SetObjectiveDisplayed(70); Jovanni.TryToEnable | Post-Mar'so-fight; "Jo'vanni can't wait!!" objective shown |
| 70 | Fragment_16 | SetObjectiveCompleted(70); SetObjectiveDisplayed(80) | "Report to Altano" objective |
| 79 | Fragment_37 | If objective 20 or 25 displayed: complete them; Jovanni.PlaceAtMe(ExpThunder); Jovanni.TryToDisable; Marso.TryToMoveTo(RFMarker); Marso.TryToEnable; Marso.SetEssential(False); BadEnd.Start(); BadEnd.SetStage(0); SetStage(80) | **BAD END trigger** — Jo'vanni killed by Meridia Staff (LightModerate enchantment hit); starts `zzzAoMMq06BadEnd`; forced to stage 80 |
| 80 | Fragment_16 (reuse) or _(no separate fragment)_ | SetObjectiveCompleted(70 if shown); SetObjectiveDisplayed(80) | Pre-report-to-Altano gate; triggers dialogue `zzAoMMq06B5Mission6Comp` with Altano |
| **90** | Fragment_18 (good) / Fragment_41 (bad) | SetObjectiveCompleted(80); ModPious(3.0); Daedra.TryToDisable; Jovanni.TryToDisable; Marso.TryToDisable; **AoMMq07.Start()**; Stop() | **QUEST COMPLETE** — both good-end and bad-end paths reach here; Mq07 starts; log = "Thank you. Jo'vanni thank you very much." (good path) |
| 255 | Fragment_43 | EnablePlayerControls; remove all factions; disable/reset all NPCs | Shutdown stage |
| 999 | Fragment_29 | Stop() | FailQuest path (skip/abort) |
| 9999 | _(CompleteQuest flag)_ | — | Alternative complete (skip entire quest) |

### Stage role summary (explicit labels):

- **Stage 25 = ENTER DREAM** (player teleported to `zzzAoMKahjiitDreamLand`)
- **Stages 30, 35, 40, 50 = INSIDE DREAM** (memory sequence NPCs active, NoPickPocket perk active)
- **Stage 55 (inferred) = EXIT DREAM** (Fragment_12: player teleported back to CatMarker / Ragged Flagon)
- **Stage 90 = QUEST RESOLVED / COMPLETE** (both branches)
- **Stage 79 = BAD END branch trigger** (Jo'vanni killed by Meridia Staff; launches `zzzAoMMq06BadEnd`)

> Note: The exact numeric stages 22 and 55 are inferred from fragment logic; the ESM stage table shows
> stages 22 and 50/60 but not 55. Fragment_12 fires somewhere between stage 50 and 60 (likely the
> same stage 50 code path or an unlisted stage); what matters is that CatMarker moveto = dream exit.

---

## 2. Corrected Gate for Beat 1-D (Post-Dream Sofia Comment)

Beat 1-D `MF_SofA1_KhajiitDream` / editorId `MFSofVig_1D_KhajiitDream`:

Current gate in `_act1-conversion-spec.md`:
```json
{ "function": "GetStageDone", "param": "Vigilant.esm:0x009E68", "stage": 90, "comparison": "==", "value": 1 }
```

**Verdict: CORRECT. No change needed.**

Stage 90 carries the `CompleteQuest` flag and the log entry "Thank you. Jo'vanni thank you very much."
It fires AFTER the player reports to Altano, which is AFTER the player has exited the dream and dealt
with Mar'so. Both the good-end path (Fragment_18) and the bad-end path (Fragment_41) set stage 90 and
start Mq07 — so stage 90 is the true "quest fully resolved" gate regardless of branch.

**Confidence: HIGH (verified from questdiag + fragment code + log entry text).**

Corrected JSON (unchanged from current spec):
```json
[
  { "function": "GetStageDone", "param": "Vigilant.esm:0x009E68", "stage": 90, "comparison": "==", "value": 1 },
  { "function": "GetGlobalValue", "param": "MF_SofA1_KhajiitDream", "comparison": "==", "value": 0 }
]
```

---

## 3. Dream-Entry Map (for Future Sofia Phantom Mechanic)

### Dream Cell

| Field | Value |
|-------|-------|
| FormID | `Vigilant.esm:0x00185C` |
| EditorID | `zzzAoMKahjiitDreamLand` |
| Name | `"Jo'vanni Dream Theater"` |
| Type | Interior cell (cell-local coordinates) |

### DreamMarker Reference

| Field | Value |
|-------|-------|
| FormID | `Vigilant.esm:0x00A3CC` |
| Base | `Skyrim.esm:0x000034` (XMarkerHeading) |
| Position | X=895.00, Y=-47.81, Z=-424.17 |
| Rotation | (0, 0, 1.583 rad) |
| Cell | `zzzAoMKahjiitDreamLand` (confirmed: position matches first objP entry in cellrefs dump) |

### Scene

| Field | Value |
|-------|-------|
| Scene FormID | `Vigilant.esm:0x4CCD7C` |
| EditorID | `zzzAoMMq06Sc01` |
| Purpose | Leads Altano to Ragged Flagon (stages 10–11); NOT the dream scene |
| Fragment | `SF_zzzAoMMq06Sc01_024CCD7C Fragment_0` → sets stage 11 |
| Dream actors | NO separate dream scene; dream is pure dialogue + package AI in the interior cell |

> The dream is NOT driven by a scene record. It uses dialogue topics stage-gated to stages 30/35/40/50/60
> with Wife (Campaner'Ra alias) and Marso as the active NPCs inside `zzzAoMKahjiitDreamLand`.

### Enter-Dream Trigger

The dream is entered via **stage 25**, triggered by the `JovanniOnHitScript` alias script on Jo'vanni:

```papyrus
; Any weapon hit (except LightModerate enchant which triggers bad end stage 79):
if !MyQ.GetStageDone(25)
    MyQ.SetStage(25)  ; → Fragment_25 fires
endif
```

Fragment_25 (stage 25) is the teleport code:
```papyrus
Game.GetPlayer().AddPerk(NoPickPocket)   ; dream "lock" perk
utility.wait(3)
Game.ForceFirstPerson()
Game.DisablePlayerControls(...)
Game.GetPlayer().PlayIdle(pKnockdown)
game.FadeOutGame(true,true,5,10)
utility.Wait(7)
Game.GetPlayer().MoveTo(Alias_DreamMarker.GetRef())   ; → zzzAoMKahjiitDreamLand (895,-47.81,-424.17)
Game.GetPlayer().PlayIdle(GetUp)
utility.Wait(7)
Game.EnablePlayerControls(...)
```

The exit-dream teleport (Fragment_12) moves the player back to `Alias_CatMarker.GetRef()` (Skyrim.esm:0x02263A,
the Ragged Flagon marker), using the same fade/moveto pattern.

### Recommendation for Putting Sofia in the Dream

**Mechanism: Conditional MoveTo at stage 25 in a QF fragment of the Sofia patch quest.**

#### Concrete implementation:

1. **Gate stage**: Watch for `GetStageDone(Vigilant.esm:0x009E68, 25) == 1` — this fires the moment
   the player enters the dream.

2. **Sofia location**: Move Sofia to position (895, -47.81, -424.17) inside cell `Vigilant.esm:0x00185C`
   using a placed XMarkerHeading ref at that position (or reuse `0x00A3CC` as the moveto target directly
   if the alias ref is accessible, but safest is a new esp-side marker).

3. **Implementation pattern** (in Sofia patch QF script, listening on stage 25 of Mq06):
   ```papyrus
   ; Sofia patch Fragment for "Mq06 dream entry"
   ; Gated on: GetStageDone(Vigilant.esm:0x009E68, 25) == 1
   ; AND Sofia is currently a follower (IsFollower guard)
   SofiaRef.MoveTo(DreamMarkerRef)   ; DreamMarkerRef = Vigilant.esm:0x00A3CC or a new marker
   SofiaRef.EvaluatePackage()        ; trigger follow/wait package inside the cell
   ; Optionally: start a brief scene or dialogue topic with Sofia commenting
   ```

4. **Exit**: Gate on `GetStageDone(Vigilant.esm:0x009E68, 50) == 1` (Daedra appears, player
   controls re-enabled) or watch for player returning to the Ragged Flagon cell — then
   MoveTo player (MoveToPlayer() or package-driven).

5. **Timing safety**: Fragment_25 does a 14-second fade+wait sequence before enabling controls.
   Sofia's moveto can fire immediately at stage 25 (she will arrive inside the dream cell
   during the fade-in period, so she'll be standing there when the screen comes up).

6. **NoPickPocket perk**: Vigilant adds/removes this perk as a dream-lock mechanism. Sofia's
   presence does not interfere with this.

**Why NOT a SCEN autoStart**: The dream has no scene backbone — NPC behavior is pure package AI
plus stage-gated dialogue topics. Sofia's entry is best handled by a moveto triggered by the
Sofia patch quest's own fragment, keeping zero coupling to Vigilant internals.

**Summary recommendation (one line)**:
> At stage 25 of `0x009E68` (GetStageDone == 1), MoveTo Sofia to `Vigilant.esm:0x00A3CC`
> (DreamMarker inside `zzzAoMKahjiitDreamLand`); exit on stage 50 or cell-change back to Ratway.

---

## 4. Bad-End Branch: `zzzAoMMq06BadEnd` (0x4CDF8D)

The bad end ("Mar'so Suicide") is a **separate quest** that starts from within Mq06.

### Trigger chain:

1. Player receives `StaffMeridia` at stage 21 (if Meridia cult blessing taken).
2. Player hits Jo'vanni with the Meridia Staff (`LightModerate` enchantment on that weapon).
3. `JovanniOnHitScript.OnHit` detects the enchantment:
   ```papyrus
   elseif (akSource as Enchantment) == LightModerate
       GoToState("Dead")
       MyQ.SetStage(79)   ; triggers Fragment_37
       bDone = True
   ```
4. Fragment_37 (stage 79) fires:
   - Completes objectives 20/25 (if still displayed)
   - Places thunder explosion on Jo'vanni
   - Disables Jo'vanni; moves Marso to Ragged Flagon marker; enables Marso
   - Sets Marso non-essential
   - **Starts `zzzAoMMq06BadEnd` (0x4CDF8D) at stage 0**
   - Calls `SetStage(80)` → moves to report-to-Altano stage

5. BadEnd quest Fragment_0 (stage 0):
   - Disables Marso; moves Marso back to editor location
   - Disables Cultist; moves Cultist to editor location
   - Stops the BadEnd quest (stage 100 = CompleteQuest)

6. Back in Mq06: stage 80 → player talks to Altano → stage 90 (Fragment_41, "bad end" path):
   - Same outcomes as good end: ModPious(3.0), Mq07.Start(), Mq06.Stop()

### Bad-end narrative interpretation:

The bad end is NOT a "player loses" scenario — it is a **moral branch** where the player uses Meridia's
gift to eliminate Jo'vanni (ostensibly ending his suffering / daedra possession). Mar'so
subsequently disappears (BadEnd disables him). The quest still completes at stage 90.

The "Mar'so Suicide" name in the quest EditorID (`zzzAoMMq06BadEnd`) implies that in this branch,
Mar'so's storyline ends tragically — he disappears or dies (disabled + moved to editor location by
BadEnd Fragment_0) without the redemptive encounter the good-end path provides.

**Bad-end NPC dialogue** (`zzzAoMMq06BadEndHello` 0x4CDF8E):
- "No more interruptions. It's just you and me now, Campaner'Ra."
- "I'll be here forever, Campanella. Always and forever."
- "Here in the deep end of the pond, no one can disturb us anymore."
These lines suggest Mar'so has gone to a pond with Campaner'Ra's pelt — consistent with a
drowning/suicide interpretation.

---

## 5. Fragment-to-Stage Cross-Reference

For completeness, the confirmed or strongly-inferred fragment → stage mapping:

| Fragment | Stage | Source |
|----------|-------|--------|
| Fragment_0 | 0 | SetObjectiveDisplayed(0) + SetActive |
| Fragment_2 | 10 | SetObjectiveCompleted(0), Sc01.ForceStart |
| Fragment_4 | 11 | SetObjectiveCompleted(10), SetObjectiveDisplayed(20) |
| Fragment_6 | 22 | SetObjectiveCompleted(20), Jovanni.StartCombat |
| Fragment_8 | 30 | Wife.Disable, Marso.Enable (dream opens) |
| Fragment_10 | 50 | Daedra.Enable, Daedra.StartCombat |
| Fragment_12 | ~55 | Player.MoveTo(CatMarker) = exit dream |
| Fragment_14 | 65 | SetObjectiveCompleted(60), SetObjectiveDisplayed(70) |
| Fragment_16 | 70 | SetObjectiveCompleted(70), SetObjectiveDisplayed(80) |
| Fragment_18 | 90 (good) | AoMMq07.Start(), Stop() |
| Fragment_20 | 60 | Marso.StartCombat |
| Fragment_22 | 65 (alt?) | game.getPlayer().kill() — unclear branch |
| Fragment_25 | **25** | **Player.MoveTo(DreamMarker) — DREAM ENTRY** |
| Fragment_27 | 35 | Wife.TryToEvaluatePackage |
| Fragment_29 | 999 | Stop() — fail/skip |
| Fragment_31 | 65 (pre-report) | Altano.TryToMoveTo(RFMarker) |
| Fragment_33 | 21 | AddItem(StaffMeridia) |
| Fragment_37 | 79 | BadEnd.Start(), SetStage(80) |
| Fragment_39 | 25 or 79 | SetObjectiveFailed(21) — cleanup if cult unused |
| Fragment_41 | 90 (bad) | AoMMq07.Start(), Stop() |
| Fragment_43 | 255 | Shutdown |
