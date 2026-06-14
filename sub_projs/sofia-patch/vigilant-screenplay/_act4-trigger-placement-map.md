# Act 4 Trigger Placement Map — Sofia × VIGILANT

> Method: stage gates decoded from PSC fragments in `_bsa-psc-cache/`. FormIDs verified against
> `questdiag`/`infodiag` on `Vigilant.esm`. Confidence ratings: HIGH = direct PSC SetStage/CompleteQuest;
> MED = hub wiring inferred + SCEN staging confirmed; LOW = shape only, runtime alias/fill not verified.
> Evidence column cites the memory-slice file that supplied the gate.

## Hub quest

| Quest | FormID | Role |
|---|---|---|
| zzzCHMQ00 (Coldharbour main) | `Vigilant.esm:0x12F24E` | Act 4 entry gate (s10 = player in CH) |
| zzzCHMemoryGuide | `Vigilant.esm:0x42E0B1` | Hub tracking Dream10/11/12 objectives 100/110/120 |
| Coldharbour worldspace | `Vigilant.esm:0x06D275` | Realm location gate for 4-A/4-F |
| Karma global | `Vigilant.esm:0x020B19F4` (zzzCHKarma, GlobalFloat) | Ending gate |

---

## Beat → Mechanism → Gate → Evidence → Confidence

| Beat | Dialogue EditorID | Mechanism | Quest Gate | Worldspace/Location | Evidence | Confidence |
|---|---|---|---|---|---|---|
| **4-A** Initial arrival | MFSofVig_4A_Arrival | Player topic, sayOnce+GLOB | GetStageDone(zzzCHMQ00 0x12F24E, 10)==1 | GetInWorldspace(0x06D275)==1 | act-4-memory-index.md (hub framing quest entry); slice.json 0x12F24E confirmed | HIGH |
| **4-B** Guide Pepe | MFSofVig_4B_PepeGuide | Player topic, sayOnce+GLOB | GetStageDone(zzzCHMQ00 0x12F24E, 10)==1 | GetInWorldspace(0x06D275)==1 | act-4-memory-index.md: Pepe=alias #2 of MeQ01; hub guide role | MED |
| **4-C-MeQ01** Grand Inquisitor post | MFSofVig_4C_Inquisitor | Player topic optional, sayOnce+GLOB | GetStageDone(0x12C4F4, 20)==1 OR GetStageDone(0x12C4F4, 100)==1 (either CompleteQuest band) | — | act-4-memory-01-grand-inquisitor.md: CompleteQuest @ stage 20(good) / 100(bad) | HIGH |
| **4-C-MeQ02** Mad King post | MFSofVig_4C_MadKing | Player topic optional, sayOnce+GLOB | GetStageDone(0x13712B, 30)==1 OR GetStageDone(0x13712B, 130)==1 | — | act-4-memory-02-mad-king.md: CompleteQuest @ 30(good) / 130(bad) | HIGH |
| **4-C-MeQ07** Marukh post | MFSofVig_4C_Marukh | Player topic optional, sayOnce+GLOB | GetStageDone(0x06F53C, 70)==1 OR GetStageDone(0x06F53C, 150)==1 | — | act-4-memory-07-marukh.md: CompleteQuest @ 70(good) / 150(bad) | HIGH |
| **4-D Pelinal I** (MeQ10 in-progress) | MFSofVig_4D_PelinalI | Player topic, in-realm phantom, sayOnce+GLOB | GetQuestRunning(0x2A532E)==1 AND GetStageDone(0x2A532E, 30)==1 AND GetStageDone(0x2A532E, 180)==0 AND GetStageDone(0x2A532E, 300)==0 | GetInWorldspace(0x06D275)==1 | act-4-memory-10-pelinal.md: stage 30 = Korn bark; stages 180/300 = CompleteQuest bands | HIGH |
| **4-D Pelinal post-Good** | MFSofVig_4D_PelinalGood | Player topic, sayOnce+GLOB | GetStageDone(0x2A532E, 180)==1 | — | act-4-memory-10-pelinal.md: stage 180 = Good CompleteQuest (spared Mary, GoodScene ran) | HIGH |
| **4-D Pelinal post-Bad** | MFSofVig_4D_PelinalBad | Player topic, sayOnce+GLOB | GetStageDone(0x2A532E, 300)==1 | — | act-4-memory-10-pelinal.md: stage 300 = Bad CompleteQuest (killed Mary, BasScene ran) | HIGH |
| **4-E Molag Bal confrontation** | MFSofVig_4E_MolagBal | Player topic + 追問, sayOnce+GLOB | GetStageDone(zzzCHMQ00 0x12F24E, 90)==1 (late CH stage; Molag Bal scene active) | GetInWorldspace(0x06D275)==1 | act-4-memory-index.md: slice.json used s90 for late-CH gate; MQ00 drives Molag Bal confrontation | MED — exact Molag Bal scene stage uncertain; s90 from slice.json confirmed |
| **4-F Coldharbour atmosphere** | MFSofVig_4F_Depths | Player topic, sayOnce+GLOB | GetStageDone(zzzCHMQ00 0x12F24E, 10)==1 | GetInWorldspace(0x06D275)==1 | act-4-memory-index.md + design §4.2; realm gate mirrors slice.json | HIGH |
| **4-G Ending — Karma High** | MFSofVig_4G_EndingGood | Player topic, sayOnce+GLOB | GetStageDone(zzzCHMQ00 0x12F24E, 999)==1 OR GetQuestRunning(zzzCHMQ00)==0 | GetGlobalValue(zzzCHKarma 0x020B19F4) >= 10 | act-4-memory-index.md: Karma global 0x020B19F4; total max good Karma across all 13 = ~+39 | MED — exact end-of-CH stage for MQ00 not confirmed; 999 inferred from hub pattern |
| **4-G Ending — Karma Low** | MFSofVig_4G_EndingBad | Player topic, sayOnce+GLOB | GetStageDone(zzzCHMQ00 0x12F24E, 999)==1 OR GetQuestRunning(zzzCHMQ00)==0 | GetGlobalValue(zzzCHKarma 0x020B19F4) < 0 | Same source; negative Karma = bad endings favored | MED |
| **4-G Ending — Karma Neutral** | MFSofVig_4G_EndingNeutral | Player topic, sayOnce+GLOB | GetStageDone(zzzCHMQ00 0x12F24E, 999)==1 OR GetQuestRunning(zzzCHMQ00)==0 | GetGlobalValue(zzzCHKarma 0x020B19F4) between 0 and 9 | Same | MED |

---

## Memory silence table (寧缺勿濫)

Per design rule §0.2 + act4-冷港.md §4-C: Sofia does NOT enter or comment on most memories.
Only listed entries get dialogue. Everything else = silent by default.

| # | Quest | Name | Sofia status | Notes |
|---|---|---|---|---|
| 01 | 0x12C4F4 | Grand Inquisitor | Optional post-exit comment | Screen Mary/Inquisitor polarity: good=mercy s20, bad=compliance s100 |
| 02 | 0x13712B | Mad King | Optional post-exit comment | Good=s30, Bad=s130 |
| 03 | 0x13965A | Knight of Hound | SILENT | — |
| 04 | 0x140225 | Johan the Fool | SILENT | — |
| 05 | 0x05AE03 | Ada Bal | SILENT | — |
| 06 | 0x06A23B | Remain of Miracle | SILENT | Linear, no karma — nothing for Sofia |
| 07 | 0x06F53C | Temptation of Marukh | Optional post-exit comment | Good=s70, Bad=s150 |
| 08 | 0x080E91 | Nameless Bard | SILENT (see note) | Act 3 butler connection possible but deferred — "?" in screenplay |
| 09 | 0x2CAE30 | From Beyond | SILENT | No karma, Sheogorath-tier strangeness — not Sofia's register |
| 10 | 0x2A532E | Pelinal the Bloody | IN-REALM + post-exit (both) | Primary Pelinal engagement — Mary choice gates good/bad |
| 11 | 0x2B9BAB | After the Storm | SILENT | Driven entirely by MeQ10's outcome, no player choice within |
| 12 | 0x2BC395 | Last Night | SILENT | MeQ10-driven, Alessia/Akatosh farewell — no Sofia register |
| 13 | 0x51C038 | Man-Bull Paravanila | SILENT | Pure good ending, +6 karma, no drama register for Sofia |

**Summary: 3 memories get optional post-exit comments (MeQ01, 02, 07). 1 memory gets full in-realm + post comment (MeQ10 Pelinal). 9 memories are silent.**

---

## Uncertain / open gates

| Item | Uncertainty | Recommended handling |
|---|---|---|
| MQ00 Molag Bal confrontation exact stage | Stage 90 from slice.json; actual Molag Bal scene stage inside MQ00 not decoded | Use s90 as gate; add `_note` flagging this; validate in-game |
| MQ00 end stage (4-G endings) | 999 inferred from hub/memory pattern; MQ00 CompleteQuest not confirmed in CLI | Use stage 999; add `_note`; safe fallback: GetQuestRunning(MQ00)==0 |
| MeQ08 Nameless Bard → Act 3 butler link | The (?) in act4-冷港.md; connection is thematic not confirmed by PSC | Deferred — do not implement without explicit confirmation |
