# Agent 5 — Sofia Triggers: Carene Kill/Spare + Altano Suspicion

Investigator: agent5-endings-carene
Date: 2026-06-14
Sources read: `qf_zzaommq08_0100ea8a.psc`, `qf_zzzaommqgoodend_024d0376.psc`,
`qf_zzzaommq06badend_024cdf8d.psc`, `qf_zzaommq00_01005ce2.psc`,
`aomqge_tif__024d038[6-e].psc`, `aomqge_tif__024d039[14].psc`,
`aomqge_tif__024d03ae.psc`, `act-1-sq-08-mercy.md`, `act-1-sq-11-goodend.md`,
`act-1-sq-06-badend.md`, `act-1-sq-00-hub.md`, CLI `questdiag` + `npcdiag`.

---

## 1. Stage Tables

### 1a. zzzAoMMq08 "No Mercy" (0x00EA8A)

| Stage | Fragment | Meaning | Karma | Notes |
|------:|----------|---------|-------|-------|
| 0 | Fragment_0 | Quest starts at witch camp; Altano present; BeaconKey given | — | Hall doors opened; WitchFog disabled |
| 10 | Fragment_11 | Mom check: if dead or stage 15 done → jump to 20, else start combat | — | Mom = Carene-adjacent (Lilian's mother figure here, NOT Act-1-finale Carene) |
| 15 | Fragment_14 | Daughter check: if dead or stage 18 done → jump to 20, else start combat | — | |
| 18 | (implied) | Daughter taken care of | — | |
| 20 | Fragment_2 | Mom + Daughter both fight player | — | Both StartCombat |
| 30 | Fragment_5 | Player loses to witch / knocked out; curse effects; Karma −6; → SetStage(30) then Fragment_30 | **−6** | CompleteQuest path (bad: player loses) |
| 30 (Fragment_30) | Fragment_30 | Skip / cleanup: hall unlocked, player controls restored, quest Stop() | — | Alternate: player surrendered |
| 200 | Fragment_28/36 | Altano arrives; player can negotiate ("Negotiate with Altano" objective shown) | — | Obj 200 displayed |
| 200 (2nd log) | — | 3 conditions on log entry (stage-gated dialogue branch opens) | — | |
| 210 | Fragment_18 | Altano goes enemy; combat vs Altano (Obj "Defeat Altano") | — | Altano HP=500, EnemyFaction |
| 220 | Fragment_23 | Player knocked out by Altano → scene plays, bleedout → SetStage(230) | — | Scene01.ForceStart(); SceneSkip→230 |
| 230 | Fragment_9 | Knocked-out path completes; → starts AoMMq09 | — | CompleteQuest |
| 300 | Fragment_32 | Player bleeds out Altano; Boss (inner form 0x4D0374) revealed | — | "Defeat Ugly One" objective shown |
| 310 | Fragment_34 | Player defeats Molag Bal / Boss; +Pious +3, **Karma +3**; → qGoodEnd.Start(); SetStage(0) | **+3** | CompleteQuest; AbMolagCurse added |
| 999 | Fragment_26 | Shutdown: Jacob, Bal disabled; pNoPickPocket perk removed | — | ShutDownStage |
| 9999 | — | Final complete catch-all | — | |

Key: The Carene/Mother who appears in Mq08 (0x00EA8A) is the witch-house mother figure associated with Lilian — NOT the same as the "Mother" (Carene, 0x0012D0 zzzAoMm08Mother) who appears in MqGoodEnd. Mq08 is the witch-hunt quest. The Act 1 finale Carene confrontation lives entirely in **MqGoodEnd**.

### 1b. zzzAoMMqGoodEnd "Art of Mercy" (0x4D0376) — MAIN Carene quest

NPC: Carene = Alias_Mother = `zzzAoMm08Mother` (0x0012D0)

| Stage | Fragment | Meaning | Karma | Carene fate |
|------:|----------|---------|-------|-------------|
| 0 | Fragment_0 | Quest starts; Carene moved to hall entrance; Essential+Invulnerable; Obj "Talk to Carene" | — | Alive, protected |
| 10 | Fragment_2 | VS Carene — fight begins; HP=100, Magicka=1000, Invulnerable OFF, SetNoBleedoutRecovery(true) | — | In combat |
| 10 (TIF) | 024D0386/88/8A/8C/8E | All player dialogue choices (defense/mercy/pacifism/justice/provocation) → SetStage(10) | — | TIF fragments fire stage 10 from 5 different player responses |
| 20 | Fragment_4 | Carene Bleedout — combat ends; RestoreHealth; AllowBleedoutDialogue; Obj "Talk to Carene" | — | Bleedout (alive) |
| 29 | Fragment_6 | Carene Killable — Protected OFF, HP=10; Obj "Eliminate Caren (Option)" shown; immediately calls SetStage(30) | — | **Player CAN now kill her** |
| 29 (TIF) | 024D0394/0397 | Post-bleedout dialogue topics → SetStage(29) from player responses | — | |
| 30 | Fragment_8 | Choose Ending — TrgLeave trigger enabled; Obj "Go away from here" | — | Spare path open |
| 30 (TIF) | 024D0391 | Player says "this is last assistance for you" (stage 29 topic) → SetStage(30) | — | **Spare signal: player walks away** |
| 35 | Fragment_11 | **Carene is DEAD** → SetStage(50) | — | **Killed** |
| 40 | Fragment_13 | Check branch: if GetStageDone(35) → SetStage(50) [Bad]; else → SetStage(100) [Good] | — | Decision junction |
| 50 | Fragment_14 | **BAD END**: AbMolagCurse added; daughter backstab scene; CompleteQuest | — | Killed path |
| 60 | (unused in PSC) | — | — | |
| 65 | Fragment_16 → SetStage(65) | Bad End fade-out / DreamMarker teleport | — | |
| 70 | — | — | — | |
| 100 | Fragment_18 | **GOOD END**: HallDoor opened; Carene made Essential+Invulnerable again | — | Spared, protected |
| 110 | Fragment_20 | Report to Keeper path: Obj "Eliminate Caren" failed, Obj 30 completed; Mother/Daughter disabled | — | |
| 120 | Fragment_25 | **Quest Complete (good)**: +Pious +3, **Karma +3**; StendarrHorn given; WitchQ300 stop; ScLeave | **+3** | CompleteQuest |
| 120 (TIF) | 024D03AE | Keeper dialogue "I'll take care of it" → SetStage(120) | — | |
| 130 | Fragment_26 | Act 2 starts: qAct2.Start(); quest stops | — | |
| 130 (Fragment_32) | Fragment_32 | JSON flags saved (gAct1Count, gGoodEnd) | — | |
| 255 | Fragment_30 | ShutDown: Mother/Daughter disabled + returned to editor location | — | |
| 9999 | — | CompleteQuest catch-all | — | |

**Kill/Spare distinction (confirmed):**
- **Carene killed**: Player kills her while Protected=OFF (stage 29 window) → engine fires stage 35 → Fragment_11 → SetStage(50) → Bad End (CompleteQuest at 50).
- **Carene spared**: Player walks away via TrgLeave → stage 30 → stage 40 check → GetStageDone(35)=false → SetStage(100) → Good End → stage 120 = CompleteQuest (good).

### 1c. zzzAoMMq06BadEnd "Mar'so Suicide" (0x4CDF8D)

This quest is the **Khajiit subplot bad ending** (Act 1 Mq06 = "Also sprach Kahjiit"), NOT the Carene choice. It has only 2 stages (0, 100=CompleteQuest) and is about Mar'so and Campaner'Ra, a completely separate Khajiit domestic tragedy. It has NO connection to the Carene kill/spare choice.

| Stage | Meaning |
|------:|---------|
| 0 | Quest starts; Mar'so/Cultist placed |
| 100 | CompleteQuest (auto-fired from Fragment_0 cleanup script) |

---

## 2. Corrected Sofia Gates

### 1-I AltanoSuspicion

**Current (wrong) gate:** `GetStageDone(SubQ01 0x17576E, 30)`
— SubQ01 is the witch-house side-quest (Lilian), not related to Altano's suspicious behavior.

**Correct gate:**

```json
{
  "quest": "zzzAoMMq08",
  "formId": "0x00EA8A",
  "condition": "GetStageDone",
  "stage": 200
}
```

**Rationale:** Stage 200 of Mq08 is the "Negotiate with Altano" branch — the first point where Altano's fanaticism becomes explicit and the player can push back. At this stage, Altano is demanding the player kill regardless of context ("Kill! Whether or not! You must kill them!!"), and his objective is displayed as "Negotiate with Altano (Option)" — meaning the game itself flags his behavior as suspicious/optional. This is *after* the witch mission but *before* the Altano boss reveal (which is Mq08 stage 300 when he transforms into his inner form). Sofia's suspicion is plausible from stage 200 onwards.

**Alternative gate (more conservative, if 200 fires too early):**

```json
{
  "quest": "zzzAoMMq08",
  "formId": "0x00EA8A",
  "condition": "GetStageDone",
  "stage": 210
}
```

Stage 210 = Altano has gone enemy (Obj "Defeat Altano") — the player is actively fighting him. This is too late for *suspicion* (it's already betrayal), so stage 200 is the better pick for foreshadowing.

**Confidence:** HIGH (stage 200 is the exact narrative inflection point where Altano's dangerous fanaticism surfaces; stage 210 would be confirmation, not suspicion).

### 1-J CareneKilled

**Current (wrong) gate:** `GetStageDone(MqGoodEnd 0x4D0376, 29)`
— Stage 29 = Carene is *killable* (Protected OFF), NOT dead. Firing here would trigger the comment before the player has made the kill choice.

**Correct gate:**

```json
{
  "quest": "zzzAoMMqGoodEnd",
  "formId": "0x4D0376",
  "condition": "GetStageDone",
  "stage": 35
}
```

Stage 35 fires only if Carene actually died (Fragment_11: "35 Carene is Dead -> Bad End"). Stage 50 (CompleteQuest bad) would also work but fires slightly later (after the daughter backstab scene starts). Stage 35 is the cleanest "kill confirmed" signal.

**Confidence:** HIGH (stage 35 is the engine's own death-detection gate; it directly precedes and causes the bad-end path).

### 1-J CareneSpared

**Current (wrong) gate:** `GetStageDone(MqGoodEnd 0x4D0376, 20)`
— Stage 20 = Carene is in bleedout (fight just ended), NOT spared. This fires before the player has made any mercy decision.

**Correct gate:**

```json
{
  "quest": "zzzAoMMqGoodEnd",
  "formId": "0x4D0376",
  "condition": "GetStageDone",
  "stage": 100
}
```

Stage 100 fires only on the *good* branch (Fragment_13: GetStageDone(35) == false → SetStage(100)). It means Carene survived, the player walked away, and the good-end path is committed. This is the definitive "spared" signal.

Alternatively, stage 30 (TrgLeave enabled, "Go away from here") could work as an *earlier* indicator — but stage 30 is set even when GetStageDone(35) is not yet resolved (stage 40 hasn't checked yet). Stage 100 is safer and unambiguous.

**Confidence:** HIGH (stage 100 = good-branch confirmed; no ambiguity).

### 1-K ChapterClose

**Current gate:** `GetStageDone(MqGoodEnd 0x4D0376, 30)` — same concern as 1-J-spare: stage 30 is mid-quest, not end.

**Correct gate (good end):**

```json
{
  "quest": "zzzAoMMqGoodEnd",
  "formId": "0x4D0376",
  "condition": "GetStageDone",
  "stage": 130
}
```

Stage 130 = Act 2 quest (`qAct2`) has been started and GoodEnd has stopped. This is the true chapter-close moment: Act 1 is over, Act 2 has begun.

If the chapter-close comment should also fire on the bad-end path (stage 50 CompleteQuest), a composite condition is needed:

```json
[
  {
    "OR_condition": [
      { "quest": "zzzAoMMqGoodEnd", "formId": "0x4D0376", "condition": "GetStageDone", "stage": 130 },
      { "quest": "zzzAoMMqGoodEnd", "formId": "0x4D0376", "condition": "GetStageDone", "stage": 50 }
    ]
  }
]
```

Stage 50 = bad end completed (Carene killed, daughter scene done). Stage 130 = good end Act-2 handoff.

**Recommended single-gate for "Act 1 is definitively over, good ending":** stage 130.
**Recommended single-gate for "Act 1 complete regardless of path":** stage 50 OR stage 130 (whichever fired).

**Confidence:** HIGH for stage 130 as good-end close; MEDIUM for the OR condition (depends on whether "chapter close" is intended for good path only or both).

---

## 3. Altano Traitor Timeline

From the PSC sources:

| Moment | Quest | Stage | What happens |
|--------|-------|-------|--------------|
| Hub intro | Mq00 (0x005CE2) | 0–40 | Altano recruits player; appears normal; no sign of treachery |
| Witch mission | Mq08 (0x00EA8A) | 0–30 | Altano commands massacre of all witches incl. innocents; first red flags |
| **Altano extremism** | Mq08 | **200** | Player pushes back; Altano screams "Kill! Whether or not!" — **first explicit fanaticism flag** |
| Combat option | Mq08 | 210 | Player can fight Altano (he turns enemy) |
| Revelation | Mq08 | 300 | Altano bleeds out, inner boss (zzzAoMBossAltanoInner 0x4D0374) appears — Daedric corruption revealed |
| Full reveal | Mq08 | 310 | Boss defeated; AbMolagCurse added to player; qGoodEnd starts |

**Sofia's suspicion window:** Stage 200 of Mq08 is the ideal trigger — Altano's behavior has crossed from zealotry into something darker, but the Daedric possession has not yet been shown. This is the correct foreshadowing beat: Sofia notices something is *off* about the man who sent them on a massacre mission.

Stage 210 (player actively fighting Altano) is too late for "suspicion" — at that point it's open conflict.
Stage 300 (inner boss appears) is the reveal, not foreshadowing.

**Sofia's comment at stage 200 is plausible, in-character foreshadowing before the reveal.**

---

## 4. "Artano" → "Altano" Name Fix

The Sofia screenplay currently spells his name **"Artano"** — this is incorrect.

Confirmed correct name: **Altano**

Evidence:
- Mq08 PSC (`qf_zzaommq08_0100ea8a.psc`) declares: `ReferenceAlias Property Alias_Altano Auto`
- Mq08 QF code: `Alias_Altano.GetActorRef().SetActorValue("Health", 500)`, `Alias_Altano.TryToRemoveFromFaction(StendarrFaction)`, etc.
- Mq00 recon (`act-1-sq-00-hub.md`): EditorID = `zzzAoMVigilantTraitor`, display name = Altano throughout
- GoodEnd recon (`act-1-sq-11-goodend.md`): dialogue explicitly names "Altano" in player prompts ("By the beacon of Stendhal, Altano was martyred.")
- NPC record: 0x000D62 = `zzzAoMVigilantTraitor`; dialogue.md repeatedly uses "Altano"

All Sofia screenplay lines spelling "Artano" must be changed to **Altano**.

---

## 5. Which Quest Holds the Carene Choice

The kill/spare of Carene lives **entirely in `zzzAoMMqGoodEnd` (0x4D0376)**.

- `zzzAoMMq08` (0x00EA8A) = witch-hunt quest; its "Mother" is Lilian's mother figure, not the Act 1 finale Carene.
- `zzzAoMMq06BadEnd` (0x4CDF8D) = Mar'so/Campaner'Ra Khajiit subplot; unrelated to Carene.
- `zzzAoMMqGoodEnd` (0x4D0376) = the entire Carene confrontation, fight, kill/spare choice, Thorondir report, and Act 2 handoff.

The kill/spare decision is encoded as:
- **Kill**: Carene dies while Protected=OFF (stage 29 window) → stage 35 → stage 50 (bad end complete)
- **Spare**: Player leaves via TrgLeave → stage 30 → stage 40 check → stage 100 → stage 120 (good end complete) → stage 130 (Act 2 start)
