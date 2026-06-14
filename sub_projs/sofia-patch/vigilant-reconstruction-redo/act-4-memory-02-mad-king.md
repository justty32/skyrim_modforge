# Act 4 Memory 02 - The Mad King

Status: redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text (and these scenes are package-driven, with no spoken topics at all).
- Subject confirmed from this quest's own `zzzCHMeQ2King…` topics + the `King` alias's `uniqueActor`, NOT from any secondary reference. (The now-deleted gemini quarantine `memory-02*.md` invented dialogue like "Moon... My moon..." that does not match the ESM; nothing from it was copied.)

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

Two-band `CompleteQuest`: stage **30** (early band) vs stage **130** (late band).

**RESOLVED** (from TIF fragments + QF stage fragments in `_bsa-psc-cache/`):

Both branches (`B01` and `B02`) are spoken by `Dro'zel` (alias #1 `King`). The routing is counter-intuitive: **`B02` contains both the good and bad exits**; `B01` is a separate optional exchange that only fires at stage 10 and has a separate karma penalty.

### Full stage routing table

| Step | Stage | Who / Fragment | Effect | Source |
|---:|---:|---|---|---|
| Quest start (player enters memory) | 0 | QF `Fragment_0` (stage[0] begin) | FadeOut → `MoveTo(MemoryMarker)` | `qf_zzzchmemoryquest02_0213712b.psc:57` |
| Stage 10 | 10 | QF `Fragment_2` (stage[10] begin) | `Scene01.ForceStart()` | `qf_…:151` |
| *(optional)* B01 opener fires at stage 10 | — | `B01T01` 137130 → no VMAD | Dro'zel says "Hasama......" | `chmeq2_tif__02137134.psc` N/A (T01 has no fragment) |
| *(optional)* B01 Goodbye | — | `B01T03` 137134 `OnEnd=Fragment_0` | `SetStage(20)` | `chmeq2_tif__02137134.psc:9` |
| Stage 20 | 20 | QF `Fragment_4` (stage[20] begin) | FadeOut → `MoveTo(ReturnMarker)` → `SetStage(40)` | `qf_…:103` |
| Stage 40 ← **B01 bad-karma side-arm** | 40 | QF `Fragment_10` (stage[40] begin) | `Karma.Mod(-3.0)` + `KarmaDown.Show()` | `qf_…:79` |
| B02 T02 (Gilverdale) | — | `B02T02` 1376E0 `OnBegin=Fragment_2` | `SetStage(125)` — starts the bad-scene machinery | `chmeq2_tif__021376e0.psc:9` |
| Stage 125 | 125 | QF `Fragment_12` (stage[125] begin) | `Door.Lock(false)` + `BadScene.ForceStart()` + `RegisterSceneSkip(self, BadScene, 150, True)` | `qf_…:143` |
| **BAD EXIT** — B02 T03 Goodbye | 130 | `B02T03` 1376E2 `OnEnd=Fragment_0` → `SetStage(130)` | `CompleteQuest` (bad) → QF `Fragment_14`: `stop()` | `chmeq2_tif__021376e2.psc:9`; `qf_…:86` |
| BadScene phase[1] start | 140 | SF `Fragment_1` → `SetStage(140)` | Exit BadScene → QF `Fragment_16`: FadeOut+`MoveTo(ReturnMarker)`+`SetStage(160)` | `sf_zzzchmeq2badscene_021376eb.psc:8`; `qf_…:97` |
| Stage 150 | 150 | SF `Fragment_0` → `SetStage(150)` / QF `Fragment_18` | `Alias_Molag.GetRef().PlaceatMe(SummonExp)` (Molag Bal apparition) | `sf_…:15`; `qf_…:51` |
| Stage 160 | 160 | QF `Fragment_21` (stage[160] end) | `ModRadiance(3.0)` + `qGuide.SetStage(20)` | `qf_…:121` |
| **GOOD EXIT** — B02 T04 (no home for bard) | — | `B02T04` 1376E4 `OnBegin=Fragment_2` | `SetStage(25)` → QF `Fragment_6`: `stop()` (stops the BadScene that fired at 125) | `chmeq2_tif__021376e4.psc:9`; `qf_…:43` |
| **GOOD EXIT** — B02 T05 Goodbye | 30 | `B02T05` 1376E6 `OnEnd=Fragment_0` → `SetStage(30)` | `CompleteQuest` (good) → QF `Fragment_8`: `Karma.Mod(3.0)` + `KarmaUp.Show()` | `chmeq2_tif__021376e6.psc:9`; `qf_…:69` |

### Summary: polarity assignment (RESOLVED)

| Band | Stage | Branch / topic | Polarity | Karma |
|---|---:|---|---|---|
| good / mercy | 30 | B02 T05 `1376E6` "It is good" | **good** | `Karma.Mod(+3.0)` + KarmaUp |
| bad / corruption | 130 | B02 T03 `1376E2` "What are you going to do?" | **bad** | no Karma.Mod (neutral stop) |
| B01 side-arm | 40 | B01 T03 `137134` end of optional exchange | karma penalty only (no CompleteQuest) | `Karma.Mod(-3.0)` + KarmaDown |

- The `BadScene` (`1376EB`) is a scripted visual that **starts at stage 125** when the player prompts "Gilverdale, he said…" (B02T02). If the player then exits via T03 (Goodbye), the quest completes bad (stage 130). If the player instead says T04 (no home for the bard), stage 25 fires `stop()` which cancels the BadScene, and T05 routes to the good completion (stage 30).
- `B01` is gated `GetStage == 10` and plays as an optional vignette before B02 is available. Its Goodbye (T03 → stage 20 → stage 40) applies karma -3 but does **not** complete the quest; stage 40 is not a `CompleteQuest` stage (confirmed: `questdiag` flags stage 40 as plain, not CompleteQuest). The player can still reach B02 afterward.
- **RESOLVED** — prior "inference" label removed. Sources: `_bsa-psc-cache/chmeq2_tif__02137134.psc`, `chmeq2_tif__021376e0.psc`, `chmeq2_tif__021376e2.psc`, `chmeq2_tif__021376e4.psc`, `chmeq2_tif__021376e6.psc`, `qf_zzzchmemoryquest02_0213712b.psc`, `sf_zzzchmeq2badscene_021376eb.psc`.

Related bad-end record: [`5714DF zzzCHMemoryAyledKing_BadEnd "Ayleid King"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1217) — `npcdiag` confirms it uses package `13318F zzzCHMeditatingAtLinkedRefIgnore` (OwnerQuest=none printed); `infodiag 0x5714DF` returns no owned topics/quest. **This NPC belongs to the world but its host quest cannot be confirmed from ESM records available** (unverified: needs a placed-ref/cell scan to find which cell it appears in and whether its package links to alias #4 `Molag` or alias #5 `DrozelMemory`).

## Alias / Staging Backbone

Both `SCEN` records below share the same host quest and aliases.

Host quest:
- [`13712B zzzCHMemoryQuest02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38)

Host-quest aliases from `scenediag` (confirmed via `scenediag Vigilant.esm 0x1376E8` and `0x1376EB`):

| Alias | Name | Type | Fill | Status |
|---:|---|---|---|---|
| 0 | `MemoryMarker` | ReferenceAlias | forcedRef `13712A:Vigilant.esm` | verified |
| 1 | `King` | ReferenceAlias | uniqueActor [`137126 zzzCHDrozelMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:542) | verified |
| 2 | `ReturnMarker` | ReferenceAlias | forcedRef `13712C:Vigilant.esm` | verified |
| 3 | `Door` | ReferenceAlias | forcedRef `136FC6:Vigilant.esm` | verified |
| 4 | `Molag` | ReferenceAlias | not printed by CLI (no forcedRef/uniqueActor) | unverified: fill-by-script or condition; candidate: [`2BC374 zzzCHMemoryMolagBalMad`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:777) |
| 5 | `DrozelMemory` | **LocationAlias** | not printed by CLI | **RESOLVED** (type): QF psc declares `LocationAlias Property Alias_DrozelMemory Auto` — it is a *location* alias, not a reference alias; candidate fill: [`38366D zzzCHMemDrozel "Memory"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:548). Exact fill FormID unverified. |
| 6 | `GuideMarker` | ReferenceAlias | forcedRef `42E0B3:Vigilant.esm` (= `CHGuideHasaamaRef`) | verified — `refpos` confirms it is a placed XMarker at (-5661, 6974, 1119) in the Dro'zel memory space; EditorID `CHGuideHasaamaRef` ties it to Hasaama's location |

Source for alias types: `_bsa-psc-cache/qf_zzzchmemoryquest02_0213712b.psc:5–37` (`;BEGIN ALIAS PROPERTY` blocks).

Notes:
- All custom dialogue INFOs condition on `GetIsAliasRef == 1` for alias `#1` (`King`), so Dro'zel is the sole speaker of both branches (confirmed by `infodiag`).
- `Molag` (alias #4) is staged as a scene actor in both scenes, but acts (has Package/Timer actions) only in `BadScene` — consistent with `Molag` being a silent visual-only actor in the good scene. No INFO conditions on a `Molag` alias appear in this quest. The QF stage-150 fragment calls `Alias_Molag.GetRef().PlaceatMe(SummonExp)`, placing an explosion at the Molag ref's location (`_bsa-psc-cache/qf_…:51`).
- `DrozelMemory` (alias #5) being a LocationAlias (not ReferenceAlias) explains why `scenediag` prints no fill for it; the CLI only shows forcedRef/uniqueActor for ReferenceAliases. **RESOLVED.**

## Scene Records

These scenes are **package-driven**: every action is a `Package` or `Timer`, none is a `Dialog` action. The spoken lines live in the two custom branches below, not in the scenes.

### 1376E8 zzzCHMeQ2Scene01 (good / early-band)

CLI: `scenediag Vigilant.esm 0x1376E8`

Staging (verified):
- Host quest: [`13712B zzzCHMemoryQuest02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38)
- Flags: none
- Actors: alias `#1` (`King`) `NoPlayerActivation`; alias `#4` (`Molag`) `NoPlayerActivation`.
- Phases: 3 (phases 0, 1, 2 each with 0 start conditions / 1 complete condition).
- Actions (all actor `#1` `King` only — no action targets `#4` in this scene):
  - index 1: `Package`, phase 0→0.
  - index 2: `Package`, phase 1→1.
  - index 3: `Timer`, phase 1→1, `1` second.
  - index 4: `Package`, phase 2→2.
- Scene-category topics owned by quest: 0 (no dialog actions).
- **RESOLVED**: `Scene01` is force-started at **stage 10** (`QF Fragment_2: Scene01.ForceStart()`). `Molag` is listed as actor but has no actions in this scene — present-but-idle. Scene01 is the ambient setting for the B01 optional exchange (King at stage 10). No SF scene-fragment psc exists for `Scene01` (only `sf_zzzchmeq2badscene_021376eb.psc` exists in cache), so it has no per-phase stage-setting callbacks.

### 1376EB zzzCHMeQ2BadScene (bad / late-band)

CLI: `scenediag Vigilant.esm 0x1376EB`

Staging (verified):
- Host quest: [`13712B zzzCHMemoryQuest02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38)
- Flags: none
- Actors: alias `#1` (`King`) behavior `DeathEnd, CombatEnd, DialoguePause`; alias `#4` (`Molag`) same behavior flags.
- Phases: 3 (phase 0: 0 start / 1 complete; phase 1: 0 start / 2 complete; phase 2: 0 start / 1 complete).
- Actions:
  - index 1: `Package`, actor `#1` (`King`), phase 0→0.
  - index 2: `Package`, actor `#1` (`King`), phase 1→2.
  - index 3: `Timer`, actor `#1` (`King`), phase 1→1, `5` seconds.
  - index 4: `Package`, actor `#4` (`Molag`), phase 2→2.
  - index 5: `Timer`, actor `#4` (`Molag`), phase 2→2, `5` seconds.
- Scene-category topics owned by quest: 0.
- SF fragments (`_bsa-psc-cache/sf_zzzchmeq2badscene_021376eb.psc`):
  - `Fragment_0` (phase start? index 0): `GetOwningQuest().SetStage(150)` → `Alias_Molag.GetRef().PlaceatMe(SummonExp)`
  - `Fragment_1` (phase start? index 1): `GetOwningQuest().SetStage(140)` → `FadeOut + MoveTo(ReturnMarker) + SetStage(160)`
- **RESOLVED**: `BadScene` is force-started at **stage 125** (`QF Fragment_12`), which fires when the player prompts B02T02 "Gilverdale, he said…". The scene's per-phase SF fragments advance to stage 140 and 150. If the player takes the good exit (B02T04→T05) **before** BadScene phases advance, `SetStage(25)` fires QF `Fragment_6: stop()`, cancelling the BadScene.

Scene packages (from `find zzzCHMeQ2` — action→package binding is by EditorID inference, CLI does not print each action's package FormID):
- `1376E7 zzzCHMeq2KingSitonBed` (Package)
- `1376E9 zzzCHMeq2KingMoveToDoor` (Package)
- `1376EA zzzCHMeq2KingFroceGreet` (Package)
- `1376F0 zzzCHMeq2MolagbalRising` (Package — the Molag phase-2 rising package; BadScene action index 4, actor #4 Molag, phase 2)

## Custom Dialogue Branch: B01 (optional side-exchange, stage 10 gate; karma side-arm → stage 40)

Branch:
- [`13712E zzzCHMeQ2KingB01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1765) (DialogBranch)
- View: `13712D zzzCHMeQ2KingView`

Speaker condition pattern:
- All INFOs require `GetIsAliasRef == 1` on alias `#1` (`King` = Dro'zel).
- Opening line also requires `GetStage == 10` on quest `13712B`.
- This branch does NOT route to CompleteQuest; it leads to stage 40 (Karma −3) only. The quest is completed via B02.

| Topic | INFO | Flags | Conditions | VMAD routing | Translation |
|---|---|---|---|---|---|
| [`13712F zzzCHMeQ2KingB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1765) | `137130` | `WalkAway` | `GetStage == 10`; `GetIsAliasRef alias #1` | none | (Sad) 「Hasama……」 |
| [`137131 zzzCHMeQ2KingB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1768) | `137132` | `WalkAway` | `GetIsAliasRef alias #1` | none | Prompt: 「你也有在擔心什麼嗎？」 Response (Fear): 「那個該死的吟遊詩人唱的 Eroisa 與 Polydor 的故事，冒犯了我。」/ (Anger): 「他為什麼要用那種令人沮喪的口氣說話？」 |
| [`137133 zzzCHMeQ2KingB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1772) | `137134` | `Goodbye` | `GetIsAliasRef alias #1` | `OnEnd Fragment_0` → **`SetStage(20)`** → stage 20: MoveTo(ReturnMarker)+SetStage(40) → stage 40: `Karma.Mod(-3.0)` + KarmaDown | Prompt: 「但是，那個故事是真的嗎？」 Response (Disgust): 「真不真，都是無關緊要的小事。淨說些空話，真是個拙劣的說書人。別再粉飾這座宅邸了。」 |

Translation notes:
- `Hasama` (`137130`) — ESM record search confirms **`Hasaama`** (double-a): `106678 zzzCHHasaamaCorpse "Hasaama"` and `42E0B3 CHGuideHasaamaRef`. The dialogue source uses single-a (`Hasama ...…`) which is a typo; canonical spelling is **Hasaama**. **RESOLVED.**
- `Eroisa and Polydor` (`137132`): `Eroisa` returns 0 ESM hits; `Polydor` hits `Meta-Polydor` armor set and NPC `5726E1 zzzCHRqShrivenLancelotOrder "Meta-Polydor"` (a knight-order figure, unrelated quest). **Eroisa** is an unverified name — no ESM record, appears only in this INFO line. `Polydor` is a Vigilant lore name (Meta-Polydor = the Shiven Lancelot order). Both kept verbatim.
- `singind` in source (`137132`) is a typo for "singing". Source line: `"Story of Eroisa and Polydor that damned bard was singind , I've offended"`.
- `137134` source: `"Whether or not true , it's trivial things . To talk to fuff , It's a poor narrator . Faking the house anymore"` — broken English; zh-TW is best-effort. Keep source link.

## Custom Dialogue Branch: B02 (main branch; good exit → stage 30, bad exit → stage 130)

Branch:
- [`1376DC zzzCHMeQ2KingB02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1775) (DialogBranch)

Speaker condition pattern:
- All INFOs require `GetIsAliasRef == 1` on alias `#1` (`King` = Dro'zel).
- No `GetStage` gate on openers — available throughout the memory.
- The fork point is T03 (bad Goodbye, stage 130) vs T04→T05 (good path, stage 25→30).

| Topic | INFO | Flags | Conditions | VMAD routing | Translation |
|---|---|---|---|---|---|
| [`1376DD zzzCHMeQ2KingB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1775) | `1376DE` | `SayOnce`, `WalkAway` | `GetIsAliasRef alias #1` | none | (Puzzled) 「那個吟遊詩人是從哪裡來的？」 |
| [`1376DF zzzCHMeQ2KingB02T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1778) | `1376E0` | `WalkAway` | `GetIsAliasRef alias #1` | `OnBegin Fragment_2` → **`SetStage(125)`** → Door.Lock(false) + BadScene.ForceStart() | Prompt: 「Gilverdale，他說……」 Response (Neutral): 「………………」 |
| [`1376E1 zzzCHMeQ2KingB02T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1781) | `1376E2` | `Goodbye` | `GetIsAliasRef alias #1` | `OnEnd Fragment_0` → **`SetStage(130)`** = **CompleteQuest (bad)** → stop() | Prompt: 「你打算怎麼做？」 Response (Happy): 「………………」 |
| [`1376E3 zzzCHMeQ2KingB02T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1784) | `1376E4` | none | `GetIsAliasRef alias #1` | `OnBegin Fragment_2` → **`SetStage(25)`** → stop() (cancels BadScene) | Prompt: 「沒有任何地方是像他那樣的吟遊詩人的歸宿。」 Response (Happy): 「是啊。我今天就去睡了。為何如此深的情感？因為任何殘酷的故事到了時候也會迎來清晨。」 |
| [`1376E5 zzzCHMeQ2KingB02T05`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1787) | `1376E6` | `Goodbye` | `GetIsAliasRef alias #1` | `OnEnd Fragment_0` → **`SetStage(30)`** = **CompleteQuest (good)** → Karma.Mod(+3) + KarmaUp | Prompt: 「這樣很好。」 Response (Neutral): 「quitely」 |

Translation notes:
- `Gilverdale` (`1376DF` prompt): `find Gilverdale` returns only this topic (`1376DF` itself) plus the Slaver's Note reference. The topic prompt is a player line mentioning what "the bard" said. As a lore place-name, kept verbatim. The [Slaver's Note (`0B0826 zzzCHSlaverNote05`)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:534) is a separate quest; the cross-reference is noted but does not affect this quest's reconstruction.
- `1376E0` / `1376E2` responses: source literally `"........................"` (ellipsis beats); kept as 「………………」.
- `1376E4` source: `"Yes . I go to bed today . What deep emotion too because would have gone by the time any cruel story also greet the morning"` — broken English; zh-TW is best-effort. Keep source.
- `1376E6` source response: single word `"quitely"` (typo for "quietly"); kept verbatim in translation notes.
- Routing fully **RESOLVED** from `_bsa-psc-cache/chmeq2_tif__021376e0.psc`, `…e2.psc`, `…e4.psc`, `…e6.psc`.

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
  - `B01` (`13712E`), opener gated `GetStage == 10`; functions as an optional side-exchange with karma penalty (stage 40: Karma -3) but no CompleteQuest.
  - `B02` (`1376DC`), main branch with two exits: bad (stage 130, T03) and good (stage 30, T05).
- All VMAD fragment routing now decoded. Sources: five TIF psc files + QF psc + SF psc (see `_bsa-psc-cache/`).
- Karma global: `0x0B19F4 zzzCHKarma` (GlobalFloat) — verified by `find zzzCHKarma`. Good exit = +3, B01 penalty = −3, bad exit = no Karma.Mod.
- `qGuide.SetStage(20)` fires on stage 160 (BadScene exit path) via QF `Fragment_21`, advancing the MemoryGuide hub quest. Stage 30 (good exit) also advances `qGuide` via `Fragment_21` in `Fragment_8`? No — Fragment_8 is stage[30] begin → `Karma.Mod(3.0) + KarmaUp.Show()` only; `qGuide.SetStage(20)` is in Fragment_21 (stage[160] end). So the **good exit (stage 30)** does NOT call `qGuide.SetStage(20)` directly from this fragment — only the bad-scene exit path (stage 160) does. (inference: the guide stage may be advanced elsewhere, or stage 30 CompleteQuest suffices for the hub's `IsCompleted()` check in `qf_zzzchmemoryguide_0242e0b1.psc:113–116`.)

## Open verification (remaining)

**RESOLVED:**
- Branch polarity / stage routing: fully decoded from TIF + QF + SF psc. See Branch/Outcome Mapping above.
- `DrozelMemory` alias type: LocationAlias (not ReferenceAlias). Candidate fill `38366D zzzCHMemDrozel "Memory"`. RESOLVED.
- SCEN staging: both scenes documented with phases, actions, timers, SF fragments. RESOLVED.
- Karma values: good=+3 (stage 30), B01 side-arm=−3 (stage 40), bad=0 (stage 130 stop). RESOLVED.
- Hasaama spelling: ESM has `106678 zzzCHHasaamaCorpse "Hasaama"` — double-a canonical. RESOLVED.

**Remaining unverified:**
- `Molag` alias #4 fill: `scenediag` prints no forcedRef/uniqueActor. Candidate [`2BC374 zzzCHMemoryMolagBalMad`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:777) is the only `zzzCHMemoryMolagBal*` record in ESM. Unverified: needs a QUST record binary read (alias fill conditions) or a placed-ref cell scan to confirm.
- `DrozelMemory` alias #5 exact fill FormID: type confirmed as LocationAlias but the specific Location FormID it fills is not in the CLI output. Candidate: `38366D zzzCHMemDrozel "Memory"` (unverified).
- `5714DF zzzCHMemoryAyledKing_BadEnd "Ayleid King"`: `npcdiag` shows it exists (Essential, IsGhost, AutoCalcStats, Invulnerable, package `13318F zzzCHMeditatingAtLinkedRefIgnore`), but `infodiag 0x5714DF` returns no topics/quest. No link to `13712B` can be confirmed from available data. (unverified: needs cell/placed-ref scan to determine which interior cell it appears in and whether it is linked to the Dro'zel memory cell.)
- `Eroisa` as a proper noun: 0 ESM hits. Name appears only in INFO `137132`. Unverified — may be an in-universe name not given a record.
- The four scene packages' exact action→package bindings: CLI `scenediag` does not print action→package FormID. Binding by EditorID inference only.
- Whether `qGuide.SetStage(20)` is also triggered on the good exit (stage 30) path via a mechanism not captured in the QF psc fragments (e.g., a separate stage-effect or the hub's `IsCompleted()` poll).
