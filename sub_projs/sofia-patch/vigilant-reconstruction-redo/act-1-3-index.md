# Act 1–3 Quest Index

Status: **all 22 main quests sliced (2026-06-14)**. Source-grounded reconstruction complete per template; next: open verification (VMAD scripts, NPC refs, karma polarity).

## Source policy

Same as Act 4: formID, editorID, name, priority verified via ESM + `questdiag`; dialogue/scenes TODO per-slice.

CLI:
- `questdiag <ESM> 0x<FormID>` — stages + objectives
- `infodiag <ESM> 0x<FormID> [substr]` — topics owned by quest
- `scenediag <ESM> 0x<FormID>` — SCEN host/aliases/phases

ESM: `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

## Act 1 — Act of Magnificence (zzzAoM*)

Main story: **zzzAoMMq00** (hub) → **zzzAoMMq01–10** (branches).
Subplot: **zzzAoMSubQ01–03** (character-driven).
Ending: **zzzAoMMqGoodEnd** (mercy) vs **zzzAoMMq06BadEnd** (tyranny).

| FormID | EditorID | Name | Obj | Pri | Stages | Slice | Status |
|---|---|---|---|---|---|---|---|
| `005CE2` | zzzAoMMq00 | Vigilant of Stendarr | 5 | 90 | 9 | [done](act-1-sq-00-hub.md) | DONE |
| `005CE3` | zzzAoMMq01 | Squeezer | 6 | 90 | 10 | [done](act-1-sq-01-squeezer.md) | DONE |
| `006271` | zzzAoMMq02 | The Untouchable One | 3 | 90 | 9 | [done](act-1-sq-02-untouchable.md) | DONE |
| `00627F` | zzzAoMMq03 | Lazy Afternoon | 5 | 90 | 11 | [done](act-1-sq-03-lazy.md) | DONE |
| `0082EA` | zzzAoMMq04 | Eye of Madness | 7 | 90 | 14 | [done](act-1-sq-04-eye.md) | DONE |
| `0098C9` | zzzAoMMq05 | Dine and Dash | 8 | 90 | 15 | [done](act-1-sq-05-dine.md) | DONE |
| `009E68` | zzzAoMMq06 | Also sprach Kahjiit | 9 | 90 | 20 | [done](act-1-sq-06-kahjiit.md) | DONE |
| `4CDF8D` | zzzAoMMq06BadEnd | Mar'so Suicide | 0 | 50 | 2 | [done](act-1-sq-06-badend.md) | DONE |
| `00A3FE` | zzzAoMMq07 | Old Paladin | 6 | 90 | 17 | [done](act-1-sq-07-paladin.md) | DONE |
| `00EA8A` | zzzAoMMq08 | No Mercy | 3 | 90 | 14 | [done](act-1-sq-08-mercy.md) | DONE |
| `00EFF7` | zzzAoMMq09 | Infinite Falling | 5 | 90 | 20 | [done](act-1-sq-09-falling.md) | DONE |
| `011B75` | zzzAoMMq10 | Landing Spot | 3 | 90 | 11 | [done](act-1-sq-10-landing.md) | DONE |
| `4D0376` | zzzAoMMqGoodEnd | Art of Mercy | 6 | 90 | 7 | [done](act-1-sq-11-goodend.md) | DONE |
| `17576E` | zzzAoMSubQ01 | Witch of Ivarstead | TBD | TBD | 17 | [done](act-1-sq-sub-01-witch.md) | DONE |
| `4D4C3D` | zzzAoMSubQ02 | Sacred Anatomancer | 13 | 90 | 22 | [done](act-1-sq-sub-02-anatomancer.md) | DONE |
| `51EAC1` | zzzAoMSubQ03 | Legacy of Belharza | 2 | 90 | 11 | [done](act-1-sq-sub-03-belharza.md) | DONE |

## Act 2 — Windhelm Underground (zzzBM*)

Storyline: blood tracing in Windhelm dungeons. Hub: **zzzBMGuide** (`43B81F`); quests: **zzzBMMq01–03**.

| FormID | EditorID | Name | Obj | Pri | Stages | Slice | Status |
|---|---|---|---|---|---|---|---|
| `43B81F` | zzzBMGuide | Stendarr Guide | 2 | 99 | 4 | [done](act-2-sq-guide.md) | DONE |
| `038524` | zzzBMMq01 | Empty Jails | 8 | 90 | 12 | [done](act-2-sq-01-empty-jails.md) | DONE |
| `038525` | zzzBMMq02 | The Wreck | 6 | 90 | 8 | [done](act-2-sq-02-wreck.md) | DONE |
| `038526` | zzzBMMq03 | Blood Matron | 2 | 90 | 15 | [done](act-2-sq-03-blood-matron.md) | DONE |

## Act 3 — Coldharbour Mansion (zzzCO*)

Mansions quests. Hub: **zzzCOGuide** (`43CBAE`); main: **zzzCOMq01**.

| FormID | EditorID | Name | Obj | Pri | Stages | Slice | Status |
|---|---|---|---|---|---|---|---|
| `43CBAE` | zzzCOGuide | Stendarr Guide | 9 | 99 | 13 | [done](act-3-sq-guide.md) | DONE |
| `065932` | zzzCOMq01 | Child of Oblivion | 7 | 90 | 8 | [done](act-3-sq-01-child.md) | DONE |

## Reconstruction complete — status

All 22 source-grounded slices written (2026-06-14, parallel agent run). Each slice:
- ✅ Follows [act-4-memory-07-marukh.md](act-4-memory-07-marukh.md) template
- ✅ ESM-only (questdiag/infodiag output, extracted text links)
- ✅ No Gemini hallucinations (verified per `for-haiku-acts-1-3.md` rule 2)
- ✅ Inference explicitly marked
- ✅ Open verification items listed for each quest

## Remaining verification (per-slice notes)

Each slice has its own "Open Verification" section. Cross-cutting items:

1. **VMAD script decompilation** — all slices flag TIF__ fragments needing decode for stage routing and choice handling
2. **Alias confirmation** — all slices infer alias roles from conditions; await QUST alias target dump
3. **Branch polarity** — detect good/bad/linear routing from dialogue conditions vs SetStage effects
4. **NPC/item/location verification** — ensure referenced records exist in Vigilant.esm
5. **Karma global wiring** — how per-quest polarity feeds Act 1–3 aggregate ending (if any)

## Notes

- Act 1 has 16 quest records (main + subplot + both endings); Act 2 = 4; Act 3 = 2.
- Act 1 structure is significantly more complex than Act 4 memory quests (11–13 stages each; Act 1 main often 30–50+ stages).
- Branch polarity (good vs bad) and quest engagement order TBD from dialogue conditions.
- Radiance quests (`zzzAoMRad*`, `zzzAoMRadVampire`, etc.) and bounties (`zzzAoMBounty*`) are excluded from this scoping — will be handled as side-quest fragments later.
