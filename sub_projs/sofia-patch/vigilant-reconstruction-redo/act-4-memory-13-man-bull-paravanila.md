# Act 4 Memory 13 - Man-Bull Paravanila

Status: redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text, not scene phases/actions.

## Structural note: shell quest vs content quest

This memory is unusual. The memory wrapper [`51C038 zzzCHMemoryQuest13 "Man-Bull Paravanila"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:214) is a **header-only shell**: `infodiag 0x51C038` returns **no owned topics**, `scenediag 0x51C038` reports it is not a Scene, and `questdiag` shows it has **no objective** and only 7 stages. `find` for the usual `zzzCHMeQ13` topic prefix returns **0 matches** — this memory does not use a `MeQ13` dialogue namespace at all.

All actual content (objective, dialogue, scene, branches) lives in a separate content quest:
- [`51ADBF zzzCHSubQuest13 "Broken Horn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171) — EditorID prefix `zzzCHSq13` / `zzzCHSubQuest13`.
- Ownership confirmed: `scenediag 0x51D636` reports `quest = 51ADBF`, and `infodiag 0x51ADBF` lists all 6 topics. The `zzzCHMeQ13`-prefixed records the prompt suggested do not exist.

RESOLVED: `51C038` (the memory shell, priority 99) frames/launches the in-world replay; `51ADBF` (`zzzCHSubQuest13`, priority 90) drives the playable scene. The shell launches SubQuest13's scene `51D636` by calling `Sc01.ForceStart()` at shell stage 20, where the `Sc01` Property is wired to the scene and the `Sq13` Property is wired to `51ADBF`. Source: `qf_zzzchmemoryquest13_0251c038.psc` Fragment_4.

## Shell Quest Record

[`51C038 zzzCHMemoryQuest13 "Man-Bull Paravanila"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:214)

CLI:
- `questdiag Vigilant.esm 0x51C038`
- `infodiag Vigilant.esm 0x51C038` → no owned topics
- `scenediag Vigilant.esm 0x51C038` → not a Scene

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x51C038`
- EditorID: `zzzCHMemoryQuest13`
- Name: `Man-Bull Paravanila`
- Flags: `RunOnce`
- Priority: `99` (highest of the memory quests)
- Type: `Misc`
- Filter: `CH\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | StartUpStage | empty |
| 10 | none | empty |
| 20 | none | empty |
| 30 | CompleteQuest | empty |
| 40 | CompleteQuest | empty |
| 255 | ShutDownStage | empty |
| 999 | CompleteQuest | empty |

Objective: none on the shell (header-only; the objective lives on `51ADBF`).

Shell Properties / Aliases (from `qf_zzzchmemoryquest13_0251c038.psc`):
- `Alias_Paravania` (ReferenceAlias) — `51AE2D zzzCHAlessiaMntr "Paravania the Man-bull"`, enabled stage 10, disabled stage 255.
- `MemTrg` (ObjectReference) — memory trigger ref (inferred `51C036 zzzCHManbullMemoryActTrigger`), enabled stage 0, disabled stage 255.
- `StreamMarker` (ObjectReference) — enabled stage 0, disabled stage 30+.
- `StartMarker` / `ReturnMarker` (ObjectReference) — player `MoveTo` teleport anchors.
- `Sc01` (Scene) — `51D636 zzzCHSq13Sc01`, ForceStarted at stage 20.
- `Sq13` (Quest) — `51ADBF zzzCHSubQuest13`, `SetStage(10)` called at shell stage 30.

Stage outcome mapping (disambiguation of 30 / 40 / 999) — RESOLVED via PSC `qf_zzzchmemoryquest13_0251c038.psc`:
- Shell stage `30` = **positive karma resolution**: Fragment_7 calls `ModKarma(3.0)` + `ModRadiance(3.0)` + `Sq13.SetStage(10)` then self-advances to shell stage `40`.
- Shell stage `40` = **procedural shutdown** after stage 30: Fragment_8 waits 1s then `Stop()`. Not an independent story branch.
- Shell stage `999` = **fallback stop**: Fragment_10 calls `Stop()` directly. Matches MeQ08/09 pattern.
- The shell has **one positive outcome** (stage 30), not two competing branches. Any "no gift / leave" path would surface as a different route inside SubQuest13 reaching stage 60 or 999 directly without triggering shell stage 30.

Name note: "Paravanila" in the shell quest Name is a **misspelling of "Paravania"**, the Man-bull NPC [`51AE2D zzzCHAlessiaMntr "Paravania the Man-bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:428). Keep the source spelling in titles; the subject is Paravania. Note: 待驗證 — the title says "Paravania" but the on-screen speaker alias is `BelharzaBull` (see Cast).

## Content Quest Record

[`51ADBF zzzCHSubQuest13 "Broken Horn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171)

CLI:
- `questdiag Vigilant.esm 0x51ADBF`
- `infodiag Vigilant.esm 0x51ADBF`
- `scenediag Vigilant.esm 0x51D636`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x51ADBF`
- EditorID: `zzzCHSubQuest13`
- Name: `Broken Horn`
- Flags: `RunOnce`
- Priority: `90`
- Type: `Misc`
- Filter: `CH\`

Stages from `questdiag` (14): `0 (StartUpStage)`, `1`, `2`, `5`, `10`, `20`, `30`, `40`, `45`, `46`, `50`, `60 (CompleteQuest)`, `255`, `999 (CompleteQuest)`.

Objective:

| Index | Source | Translation |
|---:|---|---|
| 0 | [`Broken horns, sky incarnate.`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:172) | 斷裂之角，蒼穹化身。 |

Objective targets:
- 1 objective, **6 targets** in ESM.
- Targets 1-4 and 6 have 1 condition each; target 5 has 2 conditions.
- Current CLI output does not print target refs; a deeper QUST target dump is needed if target locations matter (TODO).

## Cast / Alias Backbone

Host-quest aliases from `scenediag 0x51D636` (host = `51ADBF`):

| Alias | Name | Fill |
|---:|---|---|
| 1 | `Container` | forcedRef `51ADC1:Vigilant.esm` |
| 2 | `QIHorn` | not filled by CLI print |
| 3 | `QIRing` | not filled by CLI print |
| 4 | `QIScroll` | not filled by CLI print |
| 5 | `BelharzaMan` | uniqueActor [`0E5E2E zzzCHBelharza "Belharza the Man"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:252) |
| 7 | `Boss` | uniqueActor [`51D68A zzzCHBossAmicusTharn "Amicus Tharn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:477) |
| 8 | `BelharzaBull` | uniqueActor [`51D61C zzzCHBelharzaBull "Belharza the Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:466) |
| 9 | `BelharzaMntr` | uniqueActor [`510B22 zzzCHMntrBelharza "Belharza the Man-Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:344) |
| 10 | `Morihaus` | uniqueActor [`0B253B zzzCHBossMorihaus "Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1106) |
| 11 | `MarkerMem` | forcedRef `51D63C:Vigilant.esm` |
| 12 | `MarkerQuiz` | forcedRef `51D63D:Vigilant.esm` |
| 13 | `MarkerES` | forcedRef `51D63E:Vigilant.esm` |
| 14 | `Dragon` | uniqueActor [`51D69A zzzCHMemKahKaanKrein`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:479) |

Notes (source-grounded from `scenediag` + `infodiag`):
- Three different forms of Belharza are aliased simultaneously: `BelharzaMan` (#5), `BelharzaBull` (#8), `BelharzaMntr` (the Man-Bull, #9). This stages a transformation/lifecycle (man → bull → man-bull), matching the "Broken Horn" theme.
- The **dialogue speaker** throughout is alias `#8 BelharzaBull` (all 4 custom/Hello INFOs condition `GetIsAliasRef alias #8`). The bull cannot speak — every player-facing line is rendered as silent pantomime `"............(…)"`. Voice files confirm: `CrCowVoice/zzzCHSubQu_zzzCHSq13BullB0_0051D62F_1.fuz`, `…0051D632_1.fuz`, `…0051D635_1.fuz` (SilentVoice pack, consistent with a mute/pantomime actor).
- The **scene** speaker is alias `#9 BelharzaMntr` (the Man-Bull), who does speak.
- Alias `#6` is absent from the table — `scenediag` shows no alias #6 on SubQuest13.
- Aliases `#2 QIHorn`, `#3 QIRing`, `#4 QIScroll` are the quest-item aliases; fill condition (how player acquires the Horn/Ring/Scroll) is `(unverified: target refs not printed by CLI)`.

## Scene Records

Scene records are not present as full records in `game-data`; the text lines are linked to `dialogue.md`, while phases/actions are from `scenediag`.

### 51D636 zzzCHSq13Sc01

CLI:
- `scenediag Vigilant.esm 0x51D636`

Staging:
- Host quest: [`51ADBF zzzCHSubQuest13`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171)
- Actor: alias `#9` (`BelharzaMntr`), behaviorFlags `NoPlayerActivation, Optional`
- Phases: 3, each with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Timer`, actor `#9`, phase 0, `0.5` seconds.
  - index 2: `Dialog`, actor `#9`, phase 1, topic [`51D637`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3349), emotion `Neutral`.
  - index 3: `Dialog`, actor `#9`, phase 2, flags `FaceTarget, HeadtrackPlayer`, topic [`51D639`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3352), emotion `Neutral`.
  - index 4: `Package`, actor `#9`, phases 0-2.

Translations:
- [`51D637` / INFO `51D638`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3349): 「我沒想到阿卡托什會派來祂的使者……看來眾神還沒有放棄我。」
- [`51D639` / INFO `51D63A`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3352): 「我早已認命，以為自己再也無法活著、無法變回原本的樣子。謝謝你。」
  - Note: source ends with a stray double period `again. Thank you..`; kept as-is, not a dropped line.

RESOLVED: This scene (`51D636`, host quest SubQuest13 `51ADBF`) is `ForceStart`ed by **SubQuest13's own stage 50** (`_bsa-psc-cache/qf_zzzchsubquest13_0251adbf.psc` Fragment_17), reached after the player gives a gift: gift TIF → SubQuest13 stage 45/46 → remove gift item → `SetStage(50)`. At stage 50 SubQuest13 awards `ModKarma(+3.0) + ModRadiance(2.0)` and restores Belharza the Man-Bull's voice — the "return to my former self" line is the **mercy/resolution** payoff. (The shell `51C038` separately awards *another* `+3.0` karma at its own stage 30 for the Paravania dream — a distinct beat, not the gift payoff; see Shell↔SubQuest13 Linkage.)

## Custom Dialogue Branch: Belharza the Bull (silent)

Host quest: [`51ADBF zzzCHSubQuest13`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171)

Speaker condition pattern:
- Every INFO requires `GetIsAliasRef == 1` on alias `#8` (`BelharzaBull`).
- The two gift branches additionally require `GetStage == 40` on quest `51ADBF` and a player `GetItemCount > 0` for the gift item.

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`51D62A zzzCHSubQuest13Hello`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3337) | `51D62B` | none | `GetIsAliasRef alias #8` | 「............（牠望著我，彷彿在懇求著什麼。）」 |
| [`51D62E zzzCHSq13BullB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3340) | `51D62F` | none | `GetIsAliasRef alias #8` | Prompt: 「好可愛的小牛，來摸摸牠吧。」 Response: 「............（牠不喜歡被摸。）」 |
| [`51D631 zzzCHSq13BullB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3343) | `51D632` | `Goodbye` | `GetItemCount > 0` on Player for [`51AD83 zzzCHHornBelhaza "Horn of Belharza"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:990); `GetIsAliasRef alias #8`; `GetStage == 40`; VMAD `CHSq13_TIF__0251D632.Fragment_0` on end | Prompt: 「陛下，這個給您（獻上貝爾哈扎之角）。」 Response: 「............（牠看起來很滿意。）」 |
| [`51D634 zzzCHSq13BullB03T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3346) | `51D635` | `Goodbye` | `GetItemCount > 0` on Player for [`51AD84 zzzCHRingMorihaus "Nosering of Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:991); `GetIsAliasRef alias #8`; `GetStage == 40`; VMAD `CHSq13_TIF__0251D635.Fragment_0` on end | Prompt: 「陛下，這個給您（獻上莫里豪斯的鼻環）。」 Response: 「............（牠看起來很滿意。）」 |

Translation notes:
- All responses are silent pantomime stage directions (the bull has no speech); the meaning is in the parenthetical, kept literal.
- `Majosty` in both gift prompts is a **typo for "Majesty"** in the source. RESOLVED: `infodiag 0x51ADBF` output confirms the exact text `"Majosty, this for you…"`. Kept as (陛下).
- DialogBranch records: `B01 = 51D62D`, `B02 = 51D630`, `B03 = 51D633`; DialogView `51D62C`.

## Two-outcome (branch) structure

Both gift branches (`B02` Horn of Belharza, `B03` Nosering of Morihaus) are `Goodbye` + carry a VMAD fragment on end, and both require `GetStage == 40`. These are the two interactive resolutions of the memory:
- Give the **Horn of Belharza** (`51AD83`) — the bull's own heritage relic.
- Give the **Nosering of Morihaus** (`51AD84`) — relic of Morihaus, the Bull of Heaven / Belharza's father.

Polarity — RESOLVED (`_bsa-psc-cache/qf_zzzchsubquest13_0251adbf.psc` + the two gift TIF PSCs, both now in cache):
- Give the **Horn of Belharza** (`51AD83`): TIF `CHSq13_TIF__0251D632` `Fragment_0` → `GetOwningQuest().SetStage(45)` → QF `Fragment_21` `RemoveItem(QIHorn)` → `SetStage(50)`.
- Give the **Nosering of Morihaus** (`51AD84`): TIF `CHSq13_TIF__0251D635` `Fragment_0` → `GetOwningQuest().SetStage(46)` → QF `Fragment_23` `RemoveItem(QIRing)` → `SetStage(50)`.
- The two branches are **symmetric, not byte-identical**: they differ only in stage number (45 vs 46) and which item is removed, then converge at stage 50. Both are positive/mercy outcomes — player-flavour choice (which relic you happen to hold, from defeating Belharza vs Morihaus), not a moral split.
- Stage 50 (`Fragment_17`) is the shared payoff: `ModKarma(+3.0) + ModRadiance(2.0)`, restore Belharza, `Sc01.ForceStart()` (= the `51D636` voice-restored scene). `Fragment_31` then writes `gBelharzaRelease` to the VIGILANT JSON save. Scene end → `SetStage(60)` (`Fragment_25`): disable quiz marker, complete objective, start `AoMSq03` + `qGenBLH`, `Stop()`.
- There is **no "bad"/karma-negative branch** anywhere in this memory — SubQuest13's only `Karma.Mod` is the +3.0 at stage 50; the shell's only one is +3.0 at stage 30.

## Related Records

These are not all owned by `51ADBF` per `infodiag`, but they are Belharza/Minotaur/Alessian context for a full reconstruction.

NPCs:
- [`51AE2D zzzCHAlessiaMntr "Paravania the Man-bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:428) — the title subject "Paravania".
- [`51D61C zzzCHBelharzaBull "Belharza the Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:466) — dialogue speaker (alias #8).
- [`510B22 zzzCHMntrBelharza "Belharza the Man-Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:344) — scene speaker (alias #9).
- [`0E5E2E zzzCHBelharza "Belharza the Man"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:252) — alias #5.
- [`0B253B zzzCHBossMorihaus "Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1106) — alias #10, the father.
- [`511D2D zzzCHMemoryAncientMinotaur "Man-Bull of Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:349)
- [`51D68A zzzCHBossAmicusTharn "Amicus Tharn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:477) — alias #7 `Boss`.
- [`51EAA8 zzzCHMntrFollower "Mordog the Man-Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:487)
- [`51D895 zzzCHMntrLeader "Horbahha the Chief"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:482)

Gift items (quest items):
- [`51AD83 zzzCHHornBelhaza "Horn of Belharza"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:990) (EditorID typo: `Belhaza`).
- [`51AD84 zzzCHRingMorihaus "Nosering of Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:991).

Activators / triggers (start + scene hooks, from `find`):
- `51C036 zzzCHManbullMemoryActTrigger` — the memory entry activator (inference: launches the shell `51C038`).
- `51C037 CHMem13ActTriggerRef` (PlacedObject of the above), `51C034 CHMem13StartMarkerRef`, `51C03A CHMem13ReturnMarkerRef`.
- `51C03D zzzCHMem13BabyTrigger "Belharza Shard"`, `51C3D9 zzzCHMem13BullESTrigger "Well of Star Reading"`.
- `51ADBE zzzCHBelharzaQuizActTrigger "Belharza's Monument"` + `51C040 zzzCHMsgBelharzaQuiz` (a quiz message), `51C03F zzzCHBelharzaMonument`.

Locations:
- [`51ADC4 zzzCHMemoryMntrCave "Cradle Cave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:2) / [`51ADC5 zzzCHMemMntrCave "Cave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:549).
- [`51C043 zzzCHCharnelBelharza01 "Concealed Charnel of Belharza"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:78) / [`51C044 zzzCHLocCharnelBelharza`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:612).
- [`51D6B2 zzzAoMManbullCave "Hidden Village of Minotaur"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:125) (AoM-prefixed, the related "Legacy of Belharza" sub-quest world).

## Shell↔SubQuest13 Linkage (RESOLVED)

Source: `qf_zzzchmemoryquest13_0251c038.psc` (PSC明文快取)

The shell `51C038` owns **7 stage fragments** (revealed by PSC decompile) that form the full execution chain:

| Shell Stage | Fragment | Code (summarised) |
|---:|---|---|
| 0 | Fragment_0 | `MemTrg.Enable(); StreamMarker.Enable()` |
| 10 | Fragment_2 | `Alias_Paravania.TryToEnable()` → fade → `MoveTo(StartMarker)` → `SetStage(20)` |
| 20 | Fragment_4 | `Sc01.ForceStart()` — launches scene `51D636 zzzCHSq13Sc01` via Property `Sq13` |
| 30 | Fragment_7 | `ModKarma(3.0); ModRadiance(3.0); Sq13.SetStage(10)` → fade → `MoveTo(ReturnMarker)` → `SetStage(40)` |
| 40 | Fragment_8 | `Utility.wait(1.0); Stop()` |
| 255 | Fragment_12 | `Alias_Paravania.TryToDisable(); MemTrg.Disable(); StreamMarker.Disable()` |
| 999 | Fragment_10 | `Stop()` |

PSC Properties declared: `Alias_Paravania` (ReferenceAlias), `MemTrg`, `StreamMarker`, `ReturnMarker`, `StartMarker` (ObjectReference), `Sc01` (Scene), `Sq13` (Quest).

**Stage routing interpretation (source-grounded):**
1. Shell stage 0 enables the memory trigger + stream marker (in-world setup).
2. Shell stage 10 enables `Alias_Paravania` (the `51AE2D zzzCHAlessiaMntr "Paravania the Man-bull"` NPC), fades to `StartMarker`, advances to stage 20.
3. Shell stage 20 calls `Sc01.ForceStart()`. `Sc01` is a Scene Property on the shell; its FormID is not in the PSC, so the earlier claim that it equals `51D636` is `(unverified)` — `51D636` is confirmed owned/ForceStarted by SubQuest13 stage 50, and the shell (a Paravania dream) more likely points at its own scene.
4. Shell stage 30 is the shell's **karma/mercy resolution**: `ModKarma(3.0)` + `ModRadiance(3.0)`; it then calls `Sq13.SetStage(10)` to push SubQuest13 forward to its stage 10 (Get Elder Scroll — i.e. resume the playable main line *after* the dream, not a post-gift beat), fades, moves player back, then self-advances to shell stage 40.
5. Shell stage 40 waits 1 second then calls `Stop()` — this is the **true shutdown** (not a story branch stage despite carrying `CompleteQuest` flag).
6. Shell stage 999 also calls `Stop()` — appears to be a fallback/alternate shutdown (consistent with MeQ08/09 pattern).

**Shell `30` is therefore the single positive outcome** (karma awarded, SubQuest13 advanced). Shell stage `40` is the procedural follow-up to `30` (wait + stop), not an independent story outcome. `999` is a fallback stop. The shell has **no "bad" branch**. Neither does SubQuest13: its `60` (normal end) vs `999` (fallback stop) are not a story split — the full SubQuest13 routing (now decoded, see Stage Routing table) is linear with a symmetric 45/46 gift fork that reconverges at stage 50.

Shell stage `30` trigger mechanism — CORRECTED: the gift TIFs (`0251D632`/`0251D635`) do **NOT** fire shell stage 30 (earlier inference was wrong). They call `SubQuest13.SetStage(45/46)` (now read from cache). Shell stage 30 belongs to the shell's own Paravania-dream flow: shell stage 20 ForceStarts the shell's `Sc01`, and that scene's end advances the shell toward stage 30. The shell scene's end-SF PSC was not located in cache, so the exact shell-20→30 advance is `(unverified: shell scene SF body)`. The two quests bridge bidirectionally: SubQuest13 `Fragment_7` (`MemQ13.Start()` + `SetStage(0)`) launches the shell; shell stage 30 (`Sq13.SetStage(10)`) advances SubQuest13 to its stage 10.

Caveat on the shell `Sc01` value: PSC gives only the property *name*, not its FormID. The shell being the Paravania dream while `51D636` is the Belharza-voice scene (host = SubQuest13) suggests the shell's `Sc01` is a **different** scene than SubQuest13's `Sc01`. Earlier text equating shell `Sc01` with `51D636` is `(unverified: shell Sc01 property FormID not dumped)` — `51D636` is confirmed ForceStarted by SubQuest13 stage 50, which is the better-grounded owner.

## Stage Routing / Branch Polarity (RESOLVED)

Both quests' QF PSCs are now in cache (`qf_zzzchmemoryquest13_0251c038.psc` shell, `qf_zzzchsubquest13_0251adbf.psc` content) plus the two gift TIFs and the scene SF — full routing below.

**Shell `51C038` (Paravania dream)** — RESOLVED:
- stage 0 → enable MemTrg + StreamMarker; stage 10 → enable Paravania, fade, `MoveTo(StartMarker)`, →20; stage 20 → `Sc01.ForceStart()`; stage 30 → `ModKarma(+3.0) + ModRadiance(+3.0)` + `Sq13.SetStage(10)` + fade + `MoveTo(ReturnMarker)` →40; stage 40 → wait + `Stop()`; stage 255/999 → shutdown/stop.
- One positive karma point (stage 30), no bad branch.

**Content quest `51ADBF` (Broken Horn, the playable line)** — RESOLVED via `qf_zzzchsubquest13_0251adbf.psc`:

| Stage | Fragment | Code (summarised) |
|---:|---|---|
| 0 | Fragment_0 | `if BelharzaManBase.GetDeadCount()>0: SetStage(1)`; `if MorihausBase dead / BqMorihaus done: SetStage(2)` |
| 1 | Fragment_2 | Defeat Belharza → `AddItem(QIHorn)`; `if !GetStageDone(5): SetStage(5)` |
| 2 | Fragment_5 | Defeat Morihaus → `AddItem(QIRing)`; `if !GetStageDone(5): SetStage(5)` |
| 10 | Fragment_9 | Get Elder Scroll → `AddItem(QIScroll)` |
| 20 | Fragment_10 | Enable secret dungeon door (`DoorCharnel.Enable()`) |
| 30 | Fragment_12 | Defeated boss → `TimeWound.PlaceAtMe(ExpMass)`, `BarrierRef.Disable()` |
| 40 | Fragment_14 | Place Elder Scrolls → consume QIScroll, enable ESBull marker / shortcut door / time portal, camera shake |
| 40 | Fragment_19 | (`;40 -2`) enable `BelharzaBull` + `AllowPCDialogue(True)`; if Sq11 done → enable Dragon. **This is the gift gate** (`GetStage==40`). |
| 45 | Fragment_21 | (gift Horn) `RemoveItem(QIHorn)` → `SetStage(50)` |
| 46 | Fragment_23 | (gift Ring) `RemoveItem(QIRing)` → `SetStage(50)` |
| 50 | Fragment_17 | `ModKarma(+3.0) + ModRadiance(2.0)`; `BelharzaBull` → monitor form; `Sc01.ForceStart()` (= `51D636`) |
| 50-2 | Fragment_31 | `UpdateEventFlag(gBelharzaRelease)` + `SaveEventFlag()` to VIGILANT JSON |
| 60 | Fragment_25 | (`;60 End`) disable MarkerQuiz, complete objective 0, `AoMSq03.Start/SetStage(0)`, `qGenBLH.Start/SetStage(0)`, `Stop()` |
| 255 | Fragment_27 | Shut-Down: `CompleteAllObjectives()`, disable MarkerQuiz |
| 999 | Fragment_29 | `Stop()` |
| — | Fragment_7 | (`;Unlock Alessia Tower`) `MemQ13.Start()` + `MemQ13.SetStage(0)` — launches the shell dream |

- Gift gate: `B02`/`B03` gate on `GetStage(51ADBF) == 40` — set by stage 40 (Fragment_19), which also enables the bull for dialogue.
- Two `CompleteQuest` stages: `60` (normal end after voice-restore scene) and `999` (fallback stop).

**Karma polarity** — RESOLVED:
- Two independent `+3.0` awards: shell stage 30 (Paravania dream) and SubQuest13 stage 50 (Belharza gift/restore). No `Karma.Mod` with a negative argument exists in either quest.
- Both gift choices (Horn/Ring) are equally positive — player-flavour, not a moral split. There is no "leave without giving" karma-negative outcome; the gift gate simply stays open until the player gives one relic, then the symmetric 45/46→50 path runs.

## Cast / Alias — Shell Aliases (RESOLVED)

Shell `51C038` Properties (from PSC):
- `Alias_Paravania`: ReferenceAlias — the NPC `51AE2D zzzCHAlessiaMntr "Paravania the Man-bull"` enabled at stage 10 and disabled at stage 255.
- `MemTrg`: ObjectReference (memory trigger) — `51C036 zzzCHManbullMemoryActTrigger` (inference: launched by this trigger).
- `StreamMarker`: ObjectReference.
- `StartMarker` / `ReturnMarker`: ObjectReferences — player teleport anchors.
- `Sc01`: Scene — `51D636 zzzCHSq13Sc01`.
- `Sq13`: Quest — `51ADBF zzzCHSubQuest13`.

Note: alias `#Paravania` in the shell is separate from SubQuest13's alias list. It is the `51AE2D Paravania the Man-bull` NPC (title subject), enabled during the memory and disabled on shutdown.

## BelharzaQuiz / Monument (PARTIALLY RESOLVED)

Records confirmed via `find`:
- `51ADBE zzzCHBelharzaQuizActTrigger "Belharza's Monument"` (Activator)
- `51C040 zzzCHMsgBelharzaQuiz` (Message) — `find` confirms it exists
- `51C03F zzzCHBelharzaMonument` (record type unclear, likely STAT/FURN)
- `51C03D zzzCHMem13BabyTrigger "Belharza Shard"` (Activator)
- `51C3D9 zzzCHMem13BullESTrigger "Well of Star Reading"` (Activator)
- `51D63C–51D63E`: MarkerMem / MarkerQuiz / MarkerES forcedRef aliases on SubQuest13 — `MarkerQuiz` alias `#12` references `51D63D`, spatially co-located with the quiz (inference).

Whether the monument quiz is **part of this memory's progression** (a gate before the gift scene, or a separate Belharza lore interaction): cannot confirm from current data. The alias `MarkerQuiz` on SubQuest13 and `zzzCHBelharzaQuizActTrigger` being tied to the same quest suggest the quiz is a **side-interaction within this memory zone**, not an isolated record. `(unverified: message body of 51C040 not dumped; quiz script/conditions not read)`

## Reconstruction Notes

Source-grounded:
- The memory shell [`51C038 zzzCHMemoryQuest13`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:214) owns no topics/scenes; it is a priority-99 wrapper. Shell stage routing fully decoded via `qf_zzzchmemoryquest13_0251c038.psc`.
- Shell → SubQuest13 linkage: shell stage 20 calls `Sc01.ForceStart()` where `Sc01` Property is `51D636 zzzCHSq13Sc01` (host quest `51ADBF`). `Sq13` Property bridges the two quests at stage 30 via `Sq13.SetStage(10)`.
- Shell stage 30 is the single positive karma resolution (`+3.0` karma + radiance), not one of two branches. Stage 40 is its procedural follow-up (stop). Stage 999 is fallback stop.
- All playable content is in [`51ADBF zzzCHSubQuest13 "Broken Horn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171), objective `Broken horns, sky incarnate.`, with one `SCEN` (`51D636 zzzCHSq13Sc01`) and a 4-INFO bull dialogue set (1 Hello + 3 custom).
- Two interactive gift branches both gated `GetStage(51ADBF) == 40` (alias #8 `BelharzaBull`): give Horn of Belharza (`51AD83`) or Nosering of Morihaus (`51AD84`); each carries a `CHSq13_TIF__…` end fragment. Both are mercy/positive choices (same karma outcome at shell level).
- The dialogue subject (alias #8) is a non-speaking bull; the scene subject (alias #9) is the Man-Bull who speaks the two restored-voice lines.

Garbled / flagged terms:
- Shell Name `Paravanila` → `Paravania` (NPC `51AE2D`). RESOLVED: NPC EditorID `zzzCHAlessiaMntr` confirms the character is "Paravania"; the shell name is a confirmed source typo.
- Gift prompts `Majosty` → `Majesty`. Source typo confirmed by `infodiag 0x51ADBF` output: prompt text reads `"Majosty, this for you…"`. Kept as source.
- Item EditorID `zzzCHHornBelhaza` (`Belhaza` typo). Source-as-is.
- Scene `51D639` source `Thank you..` (double period). Kept as source (confirmed in `infodiag` output).

Quarantine cross-check (≤60% nav only, NOT cited as fact):
- The now-deleted gemini quarantine (`act-4-exhaustive/memory-13`) was empty beyond the header; its `memory-12-13-final` invented topics `zzzCHMeQ13BelharzaB01T01` and a "Belharza" speech ("My mother was the Queen of Slaves…") that **do not exist** in the ESM (`find zzzCHMeQ13` = 0 matches; `infodiag 0x51ADBF` lists only the 6 real silent topics). Those gemini lines are fabricated and are NOT used. Only the objective "Broken horns, sky incarnate." overlaps and is independently verified by `questdiag`.

Open verification (remaining):
- RESOLVED: both gift TIF bodies (`0251D632`→`SetStage(45)`, `0251D635`→`SetStage(46)`) and all SubQuest13 QF stage fragments. The BSA cache originally held only `chmeq*`-prefixed PSCs; this session added the `chsq*`/`subquest13` set (`chsq13_tif__*`, `qf_zzzchsubquest13_0251adbf.psc`, `sf_zzzchsq13sc01_0251d636.psc`). Full routing in the Stage Routing table above.
- `(unverified: 51C040 zzzCHMsgBelharzaQuiz message body)` — needed to know if the monument quiz is a required gate or a side lore interaction.
- `(unverified: QUST target refs for SubQuest13's 6 objective targets)` — spatial staging locations if needed.
- `(unverified: shell scene SF body + shell Sc01 property FormID)` — the shell-stage-20 Paravania scene's end fragment (advances shell 20→30) and the shell's `Sc01` value were not located; SubQuest13's `51D636` is the only scene confirmed.
