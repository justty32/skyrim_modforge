# Act 4 Memory 02 - The Mad King

Status: redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text (and these scenes are package-driven, with no spoken topics at all).
- Subject confirmed from this quest's own `zzzCHMeQ2King…` topics + the `King` alias's `uniqueActor`, NOT from any secondary reference. The `_gemini-quarantine` `memory-02*.md` files invent dialogue ("Moon... My moon...") that does not match the ESM; nothing from them is copied.

## Subject

The Mad King is [`106660 zzzCHDrozel "Mad King Dro'zel"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:284).

In this memory the speaking actor is the memory variant [`137126 zzzCHDrozelMemory "Dro'zel"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:542), filled into the `King` alias (`uniqueActor=137126`).

- Confirmation: the prior wave's caution ("do not assume Dro'Zel") does not apply here. This quest's own topic EditorIDs are `zzzCHMeQ2King…`, and the `King` scene alias's `uniqueActor` resolves to `zzzCHDrozelMemory`. Dro'zel IS this memory's subject (source-grounded), distinct from the `zzzCHsq*` side-quest topics where he also appears.
- A memory-location record exists: [`38366D zzzCHMemDrozel "Memory"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:548).

## Quest Record

[`13712B zzzCHMemoryQuest02 "The Mad King"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38)

CLI:
- `questdiag Vigilant.esm 0x13712B`
- `infodiag Vigilant.esm 0x13712B`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x13712B`
- EditorID: `zzzCHMemoryQuest02`
- Name: `The Mad King`
- Flags: `RunOnce`
- Priority: `90`
- Type: `Misc`
- Filter: `CH\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 10 | none | empty |
| 20 | none | empty |
| 25 | none | empty |
| 30 | CompleteQuest | empty |
| 40 | none | empty |
| 125 | none | empty |
| 130 | CompleteQuest | empty |
| 140 | none | empty |
| 150 | none | empty |
| 160 | none | empty |
| 999 | ShutDownStage | empty |

Objective:

| Index | Source | Translation |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:39) | 瘋狂隨月亮一同墜落。 |
- Note: source `Insanity fall down with the moon.` is grammatically broken English; the zh-TW above renders the literal sense (瘋狂與月亮一同墜落). 待驗證 — the intended idiom is unclear.

Objective targets:
- 1 target in ESM, with 0 conditions.
- Current CLI output does not print the target ref; this needs a deeper QUST target dump if the target location matters.

## Branch / Outcome Mapping

Two-band `CompleteQuest`: stage **30** (early band) vs stage **130** (late band) — the index's good/bad (karma) signature.

The two dialogue branches map to the two scenes and the two completions:

| Band | Stage | Branch | Scene | Polarity (inference) |
|---|---:|---|---|---|
| early | 30 | `B01` (`zzzCHMeQ2KingB01`) | `zzzCHMeQ2Scene01` (`1376E8`) | good / mercy — TODO confirm |
| late | 130 | `B02` (`zzzCHMeQ2KingB02`) | `zzzCHMeQ2BadScene` (`1376EB`) | bad / corruption — TODO confirm |

- Inference basis: the second scene's EditorID is literally `zzzCHMeQ2BadScene`, which names the bad-end branch; its actor flags (`DeathEnd`, `CombatEnd`, `DialoguePause`) match a hostile/death outcome, and it adds a second actor (`Molag`, alias #4) that the good scene does not stage. The `BadScene` is therefore read as the late-band (130) bad outcome and `Scene01` as the early-band (30) good outcome.
- `B01` opener `zzzCHMeQ2KingB01T01` (`137130`) is gated `GetStage == 10` + alias #1 (King); no per-branch `GetStage` gate is printed on the `B02` openers, so the exact stage→branch wiring of the player-choice routing is **not fully decided from `infodiag`/`questdiag` alone**. The branch→outcome assignment above is inference, not byte-verified. Confirm via the scene phase completeConds and the VMAD stage fragments (see Open verification).
- Related bad-end record (cross-reference, ownership unverified): [`5714DF zzzCHMemoryAyledKing_BadEnd "Ayleid King"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1217) — `infodiag` does not list it under `13712B`; verify before use.

## Alias / Staging Backbone

Both `SCEN` records below share the same host quest and aliases.

Host quest:
- [`13712B zzzCHMemoryQuest02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38)

Host-quest aliases from `scenediag`:

| Alias | Name | Fill |
|---:|---|---|
| 0 | `MemoryMarker` | forcedRef `13712A:Vigilant.esm` |
| 1 | `King` | uniqueActor [`137126 zzzCHDrozelMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:542) |
| 2 | `ReturnMarker` | forcedRef `13712C:Vigilant.esm` |
| 3 | `Door` | forcedRef `136FC6:Vigilant.esm` |
| 4 | `Molag` | not printed by CLI (no forcedRef/uniqueActor — likely fill-by-script/condition) |
| 5 | `DrozelMemory` | not printed by CLI |
| 6 | `GuideMarker` | forcedRef `42E0B3:Vigilant.esm` |

Inference:
- All custom dialogue INFOs condition on `GetIsAliasRef == 1` for alias `#1` (`King`), so Dro'zel is the sole speaker of both branches.
- `Molag` (alias #4) is staged only in the `BadScene` as a second actor; the likely fill is a Molag Bal memory variant such as [`2BC374 zzzCHMemoryMolagBalMad "Molag Bal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:777) (inference — the CLI does not print this alias's fill; confirm via a QUST alias dump). No INFO conditions on a `Molag` alias appear in this quest, consistent with `Molag` being a silent package actor, not a dialogue speaker.

## Scene Records

These scenes are **package-driven**: every action is a `Package` or `Timer`, none is a `Dialog` action. The spoken lines live in the two custom branches below, not in the scenes.

### 1376E8 zzzCHMeQ2Scene01 (good / early-band)

CLI:
- `scenediag Vigilant.esm 0x1376E8`

Staging:
- Host quest: [`13712B zzzCHMemoryQuest02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38)
- Flags: none
- Actors: alias `#1` (`King`), alias `#4` (`Molag`) — both `NoPlayerActivation`.
- Phases: 3, each with 0 start conditions and 1 complete condition.
- Actions (all `King`, alias #1):
  - index 1: `Package`, phase 0.
  - index 2: `Package`, phase 1.
  - index 3: `Timer`, phase 1, `1` second.
  - index 4: `Package`, phase 2.
- Note: although `Molag` is listed as a scene actor here, no action targets alias #4 in `Scene01` — only the King acts. (inference: `Molag` is present-but-idle in the good scene and only becomes active in the BadScene.)

### 1376EB zzzCHMeQ2BadScene (bad / late-band)

CLI:
- `scenediag Vigilant.esm 0x1376EB`

Staging:
- Host quest: [`13712B zzzCHMemoryQuest02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38)
- Flags: none
- Actors: alias `#1` (`King`) and alias `#4` (`Molag`), both with behavior flags `DeathEnd`, `CombatEnd`, `DialoguePause`.
- Phases: 3 (phase 1 has 2 complete conditions; phases 0 and 2 have 1 each).
- Actions:
  - index 1: `Package`, actor `#1` (`King`), phase 0.
  - index 2: `Package`, actor `#1` (`King`), phases 1→2.
  - index 3: `Timer`, actor `#1` (`King`), phase 1, `5` seconds.
  - index 4: `Package`, actor `#4` (`Molag`), phase 2.
  - index 5: `Timer`, actor `#4` (`Molag`), phase 2, `5` seconds.
- Note: the `Molag` actor acts (index 4/5) only in this BadScene, reinforcing the read that the bad outcome stages a Molag Bal apparition.

Scene packages (from `find King`, for reference):
- `1376E7 zzzCHMeq2KingSitonBed` (Package) (Package)
- `1376E9 zzzCHMeq2KingMoveToDoor` (Package)
- `1376EA zzzCHMeq2KingFroceGreet` (Package)
- `1376F0 zzzCHMeq2MolagbalRising` (Package — the `Molag` rising package, matches the BadScene's index-4 action)
- Note: these are the package records the scene actions reference; the CLI does not print each action's package FormID, so the action→package binding is inference from EditorID names.

## Custom Dialogue Branch: B01 (good band, stage 30)

Branch:
- [`13712E zzzCHMeQ2KingB01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1765) (DialogBranch)
- View: `13712D zzzCHMeQ2KingView`

Speaker condition pattern:
- All INFOs require `GetIsAliasRef == 1` on alias `#1` (`King` = Dro'zel).
- Opening line also requires `GetStage == 10` on quest `13712B`.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`13712F zzzCHMeQ2KingB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1765) | `137130` | `WalkAway` | `GetStage == 10`; `GetIsAliasRef alias #1` | (Sad) 「Hasama……」 |
| [`137131 zzzCHMeQ2KingB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1768) | `137132` | `WalkAway` | `GetIsAliasRef alias #1` | Prompt: 「你也有在擔心什麼嗎？」 Response (Fear): 「那個該死的吟遊詩人唱的 Eroisa 與 Polydor 的故事，冒犯了我。」/ (Anger): 「他為什麼要用那種令人沮喪的口氣說話？」 |
| [`137133 zzzCHMeQ2KingB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1772) | `137134` | `Goodbye` | `GetIsAliasRef alias #1`; VMAD `CHMeq2_TIF__02137134.Fragment_0` on end | Prompt: 「但是，那個故事是真的嗎？」 Response (Disgust): 「真不真，都是無關緊要的小事。淨說些空話，真是個拙劣的說書人。別再粉飾這座宅邸了。」 |

Translation notes:
- `Hasama` (`137130`) = the recurring name **Hasaama** flagged in the index's subject map; kept verbatim. 待驗證 proper-noun spelling.
- `Eroisa and Polydor` (`137132`) is a bard's song the King says offended him; both names kept verbatim, 待驗證.
- `singind` in source (`137132`) is a typo for "singing".
- The whole of `137134` is broken English (`To talk to fuff`, `Faking the house anymore`); the zh-TW above renders best-effort sense. 待驗證 — keep source.

## Custom Dialogue Branch: B02 (bad band, stage 130)

Branch:
- [`1376DC zzzCHMeQ2KingB02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1775) (DialogBranch)

Speaker condition pattern:
- All INFOs require `GetIsAliasRef == 1` on alias `#1` (`King` = Dro'zel).
- No `GetStage` gate is printed on these openers (`infodiag`).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`1376DD zzzCHMeQ2KingB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1775) | `1376DE` | `SayOnce`, `WalkAway` | `GetIsAliasRef alias #1` | (Puzzled) 「那個吟遊詩人是從哪裡來的？」 |
| [`1376DF zzzCHMeQ2KingB02T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1778) | `1376E0` | `WalkAway` | `GetIsAliasRef alias #1`; VMAD `CHMeq2_TIF__021376E0.Fragment_2` on begin | Prompt: 「Gilverdale，他說……」 Response (Neutral): 「………………」 |
| [`1376E1 zzzCHMeQ2KingB02T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1781) | `1376E2` | `Goodbye` | `GetIsAliasRef alias #1`; VMAD `CHMeq2_TIF__021376E2.Fragment_0` on end | Prompt: 「你打算怎麼做？」 Response (Happy): 「………………」 |
| [`1376E3 zzzCHMeQ2KingB02T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1784) | `1376E4` | none | `GetIsAliasRef alias #1`; VMAD `CHMeq2_TIF__021376E4.Fragment_2` on begin | Prompt: 「沒有任何地方是像他那樣的吟遊詩人的歸宿。」 Response (Happy): 「是啊。我今天就去睡了。為何如此深的情感？因為任何殘酷的故事到了時候也會迎來清晨。」 |
| [`1376E5 zzzCHMeQ2KingB02T05`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1787) | `1376E6` | `Goodbye` | `GetIsAliasRef alias #1`; VMAD `CHMeq2_TIF__021376E6.Fragment_0` on end | Prompt: 「這樣很好。」 Response (Neutral): 「quitely」 |

Translation notes:
- `Gilverdale` (`1376DF` prompt) is a proper noun the bard reportedly named; kept verbatim. It also surfaces in an unrelated [Slaver's Note (`0B0826 zzzCHSlaverNote05`)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:534) ("Gilverdale sank twice.") — cross-reference only, not the same quest. 待驗證.
- `1376E0` / `1376E2` responses are literally `........................` in source (silent/ellipsis beats); kept as 「………………」.
- `1376E4` is broken English (`What deep emotion too because would have gone by the time...`); zh-TW is best-effort. 待驗證 — keep source.
- `1376E6` response is the single word `quitely` (typo for "quietly"); kept verbatim. 待驗證.

## Related Records

Cross-links for a full reconstruction; ownership by `13712B` is **not** asserted unless `infodiag` confirmed it.

NPCs:
- [`106660 zzzCHDrozel "Mad King Dro'zel"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:284) — the main-world Dro'zel.
- [`137126 zzzCHDrozelMemory "Dro'zel"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:542) — the `King` alias actor (verified owner via scene alias).
- [`12A73D zzzCHDrozelShadow`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:552) — Dro'zel shadow variant (ownership unverified).
- [`2BC374 zzzCHMemoryMolagBalMad "Molag Bal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:777) — candidate fill for the `Molag` alias (inference).
- [`5714DF zzzCHMemoryAyledKing_BadEnd "Ayleid King"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1217) — bad-end-named record; relation to this quest unverified.

Locations:
- [`38366D zzzCHMemDrozel "Memory"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:548) — Dro'zel memory LCTN.

Items (King-themed, ownership unverified):
- [`2BFDAC zzzCHKingAmulet "Amulet of King"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:137)
- [`2BFDAD zzzCHKingAmuletReplica "Amulet of King (Replica)"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:138)

Books:
- No book is owned by or directly linked to `13712B` via `infodiag`. `booktext` was not run (no candidate book FormID surfaced from this quest's topics). The only `Gilverdale` book hit is the unrelated Slaver's Note above.

## Reconstruction Notes

Source-grounded:
- This memory is [`13712B zzzCHMemoryQuest02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38) with objective [`Insanity fall down with the moon.`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:39).
- Subject is Dro'zel ([`zzzCHDrozelMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:542) via the `King` scene alias's `uniqueActor`); the Dro'Zel assumption is **correct for this quest** despite the index's general caution.
- It contains two package-driven `SCEN` records (`1376E8 zzzCHMeQ2Scene01`, `1376EB zzzCHMeQ2BadScene`); neither uses a `Dialog` action.
- It contains two custom dialogue branches, both spoken by the `King` alias (#1):
  - `B01` (`13712E`), opener gated `GetStage == 10`.
  - `B02` (`1376DC`), openers ungated on stage in `infodiag`.
- VMAD fragments exist on most player choices (`137134`, `1376E0`, `1376E2`, `1376E4`, `1376E6`), indicating they advance state or trigger outcomes. Exact Papyrus behavior is not decoded here.

Open verification:
- the **30 vs 130 polarity** (which is good/bad) is inferred from the `BadScene` EditorID + actor flags; confirm by decompiling the stage/TIF fragments (`CHMeq2_TIF__02137134`, `CHMeq2_TIF__021376E0`, `...E2`, `...E4`, `...E6`) and the quest stage fragments to see which `SetStage`/`CompleteQuest` each choice routes to;
- dump the QUST aliases directly to resolve the `Molag` (#4) and `DrozelMemory` (#5) fills the CLI does not print;
- inspect the four scene packages (`zzzCHMeq2KingSitonBed`, `...MoveToDoor`, `...FroceGreet`, `zzzCHMeq2MolagbalRising`) if spatial/behavioral staging matters;
- verify proper nouns `Hasama`/`Hasaama`, `Eroisa`, `Polydor`, `Gilverdale` against any in-game book or NPC record;
- confirm whether [`5714DF zzzCHMemoryAyledKing_BadEnd`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1217) belongs to this quest's bad end before using it narratively.
