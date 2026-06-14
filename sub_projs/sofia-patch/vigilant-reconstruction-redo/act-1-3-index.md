# Act 1–3 Quest Index (WIP)

Status: scoping phase. Backbone extracted from quests.md; TODO: questdiag + infodiag per quest.

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
| `005CE2` | zzzAoMMq00 | Vigilant of Stendarr | TBD | TBD | TBD | act-1-sq-00-hub.md | TODO |
| `005CE3` | zzzAoMMq01 | Squeezer | TBD | TBD | TBD | act-1-sq-01-squeezer.md | TODO |
| `006271` | zzzAoMMq02 | The Untouchable One | TBD | TBD | TBD | act-1-sq-02-untouchable.md | TODO |
| `00627F` | zzzAoMMq03 | Lazy Afternoon | TBD | TBD | TBD | act-1-sq-03-lazy.md | TODO |
| `0082EA` | zzzAoMMq04 | Eye of Madness | TBD | TBD | TBD | act-1-sq-04-eye.md | TODO |
| `0098C9` | zzzAoMMq05 | Dine and Dash | TBD | TBD | TBD | act-1-sq-05-dine.md | TODO |
| `009E68` | zzzAoMMq06 | Also sprach Kahjiit | TBD | TBD | TBD | act-1-sq-06-kahjiit.md | TODO |
| `4CDF8D` | zzzAoMMq06BadEnd | Mar'so Suicide | TBD | TBD | TBD | act-1-sq-06-badend.md | TODO |
| `00A3FE` | zzzAoMMq07 | Old Paladin | TBD | TBD | TBD | act-1-sq-07-paladin.md | TODO |
| `00EA8A` | zzzAoMMq08 | No Mercy | TBD | TBD | TBD | act-1-sq-08-mercy.md | TODO |
| `00EFF7` | zzzAoMMq09 | Infinite Falling | TBD | TBD | TBD | act-1-sq-09-falling.md | TODO |
| `011B75` | zzzAoMMq10 | Landing Spot | TBD | TBD | TBD | act-1-sq-10-landing.md | TODO |
| `4D0376` | zzzAoMMqGoodEnd | Art of Mercy | TBD | TBD | TBD | act-1-sq-11-goodend.md | TODO |
| `17576E` | zzzAoMSubQ01 | Witch of Ivarstead | TBD | TBD | TBD | act-1-sq-sub-01-witch.md | TODO |
| `4D4C3D` | zzzAoMSubQ02 | Sacred Anatomancer | TBD | TBD | TBD | act-1-sq-sub-02-anatomancer.md | TODO |
| `51EAC1` | zzzAoMSubQ03 | Legacy of Belharza | TBD | TBD | TBD | act-1-sq-sub-03-belharza.md | TODO |

## Act 2 — Windhelm Underground (zzzBM*)

Storyline: blood tracing in Windhelm dungeons. Hub: **zzzBMGuide** (`43B81F`); quests: **zzzBMMq01–03**.

| FormID | EditorID | Name | Obj | Pri | Stages | Slice | Status |
|---|---|---|---|---|---|---|---|
| `43B81F` | zzzBMGuide | Stendarr Guide | TBD | TBD | TBD | act-2-sq-guide.md | TODO |
| `038524` | zzzBMMq01 | Empty Jails | TBD | TBD | TBD | act-2-sq-01-empty-jails.md | TODO |
| `038525` | zzzBMMq02 | The Wreck | TBD | TBD | TBD | act-2-sq-02-wreck.md | TODO |
| `038526` | zzzBMMq03 | Blood Matron | TBD | TBD | TBD | act-2-sq-03-blood-matron.md | TODO |

## Act 3 — Coldharbour Mansion (zzzCO*)

Mansions quests. Hub: **zzzCOGuide** (`43CBAE`); main: **zzzCOMq01**.

| FormID | EditorID | Name | Obj | Pri | Stages | Slice | Status |
|---|---|---|---|---|---|---|---|
| `43CBAE` | zzzCOGuide | Stendarr Guide | TBD | TBD | TBD | act-3-sq-guide.md | TODO |
| `065932` | zzzCOMq01 | Child of Oblivion | TBD | TBD | TBD | act-3-sq-01-child.md | TODO |

## Next steps

1. Pick one Act 1 main quest → run questdiag + infodiag + (if SCEN found) scenediag
2. Write source-grounded slice following [act-4-memory-07-marukh.md](act-4-memory-07-marukh.md) template
3. Consolidate findings (good/bad branches, karma polarity, release state)
4. Repeat for remaining quests; update index as slices are done

## Notes

- Act 1 has 16 quest records (main + subplot + both endings); Act 2 = 4; Act 3 = 2.
- Act 1 structure is significantly more complex than Act 4 memory quests (11–13 stages each; Act 1 main often 30–50+ stages).
- Branch polarity (good vs bad) and quest engagement order TBD from dialogue conditions.
- Radiance quests (`zzzAoMRad*`, `zzzAoMRadVampire`, etc.) and bounties (`zzzAoMBounty*`) are excluded from this scoping — will be handled as side-quest fragments later.
