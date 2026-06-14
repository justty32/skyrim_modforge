# Agent 1 — Mq00 Hub & Mq01 Squeezer: Stage Decoding + Sofia Beat Gates

Sources used:
- `_bsa-psc-cache/qf_zzaommq00_01005ce2.psc` — QF stage fragments for Mq00
- `_bsa-psc-cache/qf_zzaommq01_01005ce3.psc` — QF stage fragments for Mq01
- `_bsa-psc-cache/aom00_tif__01005ce9.psc` — TIF for "Yes join" dialogue
- `_bsa-psc-cache/aom00_tif__01027a3f.psc` — TIF for "Arrive at Temple" dialogue
- `_bsa-psc-cache/aom00_tif__01027a42.psc` — TIF for "Meet Thorondir" dialogue
- `_bsa-psc-cache/aom00_tif__01027a45.psc` — TIF for "Temple explanation" dialogue
- `_bsa-psc-cache/aom01_tif__01006259.psc` — TIF for Mq01 opener dialogue
- `_bsa-psc-cache/aom01_tif__01006270.psc` — TIF for Mq01 mission complete dialogue
- `act-1-sq-00-hub.md` — reconstruction notes (objective text, dialogue conditions)
- `act-1-sq-01-squeezer.md` — reconstruction notes for Mq01
- CLI `questdiag` output for both quests

---

## 1. Stage → Meaning Table

### Mq00 `zzzAoMMq00` "Vigilant of Stendarr" (Vigilant.esm:0x005CE2)

| Stage | Objective displayed/completed | QF Fragment | Key actions | Meaning |
|------:|------|------|------|------|
| 0 | DisplayObjective(5) "Join the Vigilant of Stendarr" | Fragment_9 | SetActive | Quest startup; player sees "join" objective |
| 10 | CompleteObjective(5), DisplayObjective(10) "Follow Altano or Join Altano at Temple of Stendarr" | Fragment_3 | **AddToFaction(VigilantFaction)**, AddSpell(ProtectAb), AddItem(StendarrAmulet), MapMarker.AddToMap | **THE JOIN EVENT** — player is accepted; gets protection spell + amulet; Altano leads to temple |
| 15 | CompleteObjective(10), DisplayObjective(15) "Talk to Altano" | Fragment_16 | Altano.TryToMoveTo(AltanoMarkerRef) | Player has arrived at Temple of Stendarr; Altano repositions |
| 20 | CompleteObjective(15), DisplayObjective(20) "Talk to Thorondir" | Fragment_11 | (none beyond objectives) | TIF_027A3F fires here (after "Arrive at Temple" dialogue with Altano) |
| 30 | CompleteObjective(20), DisplayObjective(30) "Talk to Altano" | Fragment_13 | (none beyond objectives) | TIF_027A42 fires here (after meeting Thorondir); Altano gives temple tour |
| 40 | CompleteObjective(30) | Fragment_15 | **AoMMq01.Start()**, Stop() | TIF_027A45 fires here (temple explanation done); Mq01 starts; Mq00 stops — QUEST COMPLETE |
| 999 | CompleteQuest | Fragment_21 / Fragment_25 | Altano.TryToDisable(); AddToFaction(VigilantFaction) | "Black Owl Trap" alternate branch (Altano betrayal scenario); also closes quest |
| 9999 | CompleteQuest | Fragment_19 | AddToFaction(VigilantFaction), qOwl.SetStage(999) | "Skip Episode" path |

**TIF → stage chain for main path:**
```
Dialogue "Yes let me join" (005CE9)
  → TIF_005CE9: SetStage(10)
  → Fragment_3 fires: AddToFaction(VigilantFaction) + AddSpell(ProtectAb) + AddItem(StendarrAmulet)

Dialogue "Arrive at Temple" (027A3F)  [Altano at temple, GetStage==15]
  → TIF_027A3F: SetStage(20)
  → Fragment_11 fires

Dialogue "Meet Thorondir" (027A42)  [Thorondir, GetStage==20]
  → TIF_027A42: SetStage(30)
  → Fragment_13 fires

Dialogue "Temple explanation" (027A45)  [Altano, GetStage==30]
  → TIF_027A45: SetStage(40)
  → Fragment_15 fires: AoMMq01.Start() → Mq01 begins; Mq00 stops
```

**Critical observation**: Stage 5 does NOT exist as a standalone "join" stage in the fragment code. The `questdiag` output shows stage 5 has entries but only log conditions — no fragment code. The actual join (AddToFaction etc.) fires at **stage 10**. The spec's current use of `GetStageDone(Mq00, 5)` in beat 1-A is based on objective index (obj 5 = "Join"), not a real stage action.

Stage 5 appears to exist as a conditional branch entry in the QUST stage table (questdiag shows `log: flags=0 conds=3 ""`), suggesting a condition-gated log entry or alt-stage, not a functional fragment.

---

### Mq01 `zzzAoMMq01` "Squeezer" (Vigilant.esm:0x005CE3)

| Stage | Objective displayed/completed | QF Fragment | Key actions | Meaning |
|------:|------|------|------|------|
| 0 | CompleteObjective(0), DisplayObjective(10) "Follow Altano or Join Altano at Hall of Dead" | Fragment_0 | **AoMMq01Scene.ForceStart()**, WhiterunMap.Enable, actors moved to HallofDeadMarker | First mission starts; scene fires; player directed to Whiterun Hall of the Dead |
| 10 | DisplayObjective(0) | Fragment_10 | SetActive | TIF_006259 fires (quest opener dialogue); stage 10 = player has spoken to Altano about the mission |
| 11 | — | Fragment_16 | Actors move to HallofDeadMarker if not there; RegisterSceneSkip | Scene skip safety; player near Hall of Dead |
| 15 | CompleteObjective(15), DisplayObjective(20) "Search Vampire" | Fragment_2 | (scene handling) | Player has talked to Altano at crime scene |
| 20 | CompleteObjective(20), DisplayObjective(30) "Defeat Vampire" | Fragment_4 | TransSpell.Cast(Vampire), Alias_Vampire.AddToFaction(StendarrEnemyFaction) | Suspect found; vampire revealed; combat begins |
| 30 | CompleteObjective(30), DisplayObjective(40) "Report to Altano" | Fragment_6 | Alias_Priest.TryToDisable() | Vampire defeated |
| 40 | CompleteObjective(10) [DisplayObjective(15)] | Fragment_12 | Altano.TryToEvaluatePackage() | Player returns to Altano to report |
| 50 | CompleteObjective(40) | Fragment_8 | **AoMMq02.Start()**, Stop() | TIF_006270 fires (mission complete report); Mq02 starts; Mq01 stops — QUEST COMPLETE |
| 255 | ShutDownStage | Fragment_21 | Priest/Vampire TryToDisable + MoveToMyEditorLocation | Cleanup |

**First mission complete** = Mq01 stage 50 (Fragment_8 + TIF_006270 sets stage 50, CompleteQuest flag).

---

## 2. Sofia Beat Gate Corrections

### Beat 1-A: JoinVigilant

**Current spec gate**: `GetStageDone(Vigilant.esm:0x005CE2, 5) == 1`

**Assessment**: WRONG

**Corrected gate**:
```json
[
  { "function": "GetStageDone", "param": "Vigilant.esm:0x005CE2", "stage": 10, "comparison": "==", "value": 1 }
]
```

**Justification**: Stage 5 has no fragment code and no join actions. The actual join event is **stage 10** — confirmed by TIF_005CE9 (`SetStage(10)`) being the fragment on Altano's "Yes join" dialogue, and Fragment_3 firing at stage 10 which executes `AddToFaction(VigilantFaction)` + `AddSpell(ProtectAb)` + `AddItem(StendarrAmulet)`. Stage 5 appears to be a condition-only log branch in the QUST record with no associated Papyrus execution. `GetStageDone(Mq00, 5)` may never evaluate true on the normal play path, silently preventing the beat from ever firing.

**Confidence**: HIGH — sourced directly from TIF_005CE9 (`SetStage(10)`) and QF Fragment_3 (`game.getPlayer().AddToFaction(VigilantFaction)` at stage 10 objective complete).

---

### Beat 1-B: LearnRestoration

**Current spec gate**: `GetStageDone(Vigilant.esm:0x005CE2, 10) == 1` AND `GetInFaction(SofiaFollower.esp:0x060480) == 1`

**Assessment**: CORRECT stage, but requires clarification on the "Restoration/Turn Undead" framing.

**Corrected gate** (unchanged):
```json
[
  { "function": "GetStageDone", "param": "Vigilant.esm:0x005CE2", "stage": 10, "comparison": "==", "value": 1 },
  { "function": "GetInFaction", "param": "SofiaFollower.esp:0x060480", "comparison": "==", "value": 1 }
]
```

**Justification**: Stage 10 is the join stage — Fragment_3 grants `ProtectAb` (a protective spell) and the StendarrAmulet. There is **no explicit spell-teaching beat for Restoration or Turn Undead** in the Mq00 QF fragments or any TIF script. The "Learn Restoration" framing in the beat is original creative content (Sofia's reaction to being a Vigilant who uses holy magic), not a specific in-game trigger. Stage 10 (post-join, player has been given the protection ability and told to follow Altano to the Temple) is the correct earliest point when Sofia could plausibly comment on the Vigilant lifestyle and holy spells. The follower-faction condition is also correct — ensures Sofia only delivers this banter when actually following the player.

**Confidence**: HIGH for the stage number (stage 10 = post-join confirmed). MED for the thematic framing — the spell name "ProtectAb" (not an explicit Turn Undead) is close enough. No better trigger point exists in Mq00.

---

### Beat 1-C: EarlyMission

**Current spec gate**: `GetQuestRunning(Vigilant.esm:0x005CE2) == 1` AND `GetStageDone(Vigilant.esm:0x005CE2, 10) == 1`

**Assessment**: WRONG — this gate cannot fire correctly.

**Problem**: Mq00 **stops** at stage 40 (Fragment_15 calls `Stop()`). After the temple tour (the last Mq00 stage), Mq00 is no longer running. If the intent is "after the first mission (Squeezer)", the Mq00 quest is already completed/stopped by then, so `GetQuestRunning(Mq00)` will return 0 and the beat will never fire once Mq01 is active.

**Two interpretations and their corrected gates:**

**Option A — "After the first real mission (Squeezer complete)"** — use Mq01 gate:
```json
[
  { "function": "GetStageDone", "param": "Vigilant.esm:0x005CE3", "stage": 50, "comparison": "==", "value": 1 }
]
```
Stage 50 = Mq01 complete (TIF_006270 + Fragment_8: AoMMq02.Start(), Stop()). This is precisely "after the first den/vampire hunt". Most accurate to the beat description "another den cleared."

**Option B — "During early Mq00, after temple arrival but before first mission"** — use Mq00 stage 15:
```json
[
  { "function": "GetStageDone", "param": "Vigilant.esm:0x005CE2", "stage": 15, "comparison": "==", "value": 1 }
]
```
Stage 15 = arrived at Temple, Altano repositioned (early Vigilant life, player is at the hub).

**Recommendation**: Use **Option A** (`GetStageDone(Mq01, 50)==1`). The beat name "EarlyMission" and description "another den cleared" imply the first mission has been completed. Stage 15 of Mq00 is pre-mission (player is being inducted), which contradicts the beat flavor. Mq01 stage 50 is unambiguously "first vampire hunt done."

**Corrected gate (recommended)**:
```json
[
  { "function": "GetStageDone", "param": "Vigilant.esm:0x005CE3", "stage": 50, "comparison": "==", "value": 1 },
  { "function": "GetGlobalValue", "param": "MF_SofA1_EarlyMission", "comparison": "==", "value": 0 }
]
```

**Confidence**: HIGH for the error diagnosis (GetQuestRunning(Mq00) can't fire after Mq01 is active). HIGH for Option A if "den cleared" means post-Mq01. MED if the intent was "ambient line during the hub phase" (Option B is better in that case).

---

## 3. Scene and Location Context

### Relevant cells and worldspaces

| Record | FormID | EditorID | Name | Role |
|---|---|---|---|---|
| Cell (interior) | `Vigilant.esm:0x025091` | `zzzAoMTempleInteriorStendarr` | Temple of Stendarr (interior) | Mq00 stage 15 gate in dialogue 027A3F; Altano arrives here |
| Cell (interior) | `Vigilant.esm:0x0165AA` (Skyrim.esm) | Hall of the Dead, Whiterun | Mq01 investigation scene | Mq01 stage 15 gate in dialogue 00625C |
| Marker | `AltanoMarkerRef` | — | Altano's marker at Temple | Fragment_16: Altano.TryToMoveTo(AltanoMarkerRef) at stage 15 |
| Marker | `DawnstarMarker` | — | Dawnstar marker | Fragment_6: Altano moves to Dawnstar in alternate "Owl" branch |
| Marker | `HallofDeadMarker` | — | Hall of Dead marker in Whiterun | Mq01 Fragment_0 and Fragment_16: actors moved here at stage 0/11 |
| Map marker | `WhiterunMap` | — | Whiterun map marker | Mq01 Fragment_0: enabled at quest start |

### Scene in Mq01

- `AoMMq01Scene` — fires at Mq01 stage 0 (`Fragment_0: AoMMq01Scene.ForceStart()`). This is the scene at Hall of the Dead where Altano, a Priest, a Cat (Bal), and the Vampire are staged. The scene skip is registered at both stage 0 (skip to stage 11) and stage 11 (skip to stage 15).
- No scene in Mq00 — all staging is via dialogue conditions and TIF fragments.

### NPC identity note

Altano (`Vigilant.esm:0x000D62 zzzAoMVigilantTraitor`) is both the Mq00 recruiter (alias #0 in Mq00) and the Mq01 quest-giver (alias #1 in Mq01). His "Traitor" EditorID foreshadows his role as a villain — Sofia's later suspicion beat (1-I, `MF_SofA1_ArtanoSuspicion`) is canonically grounded: the game itself encodes his betrayal in the very EditorID of his NPC record.

---

## 4. Summary Table — Corrected Conditions

| Beat | Old gate | New gate | Status | Confidence |
|---|---|---|---|---|
| **1-A JoinVigilant** | `GetStageDone(0x005CE2, 5)==1` | `GetStageDone(0x005CE2, 10)==1` | WRONG → corrected | HIGH |
| **1-B LearnRestoration** | `GetStageDone(0x005CE2, 10)==1` + follower faction | unchanged | CORRECT | HIGH (stage), MED (thematic) |
| **1-C EarlyMission** | `GetQuestRunning(0x005CE2)==1` + `GetStageDone(0x005CE2, 10)==1` | `GetStageDone(0x005CE3, 50)==1` | WRONG → corrected (quest switch) | HIGH |
