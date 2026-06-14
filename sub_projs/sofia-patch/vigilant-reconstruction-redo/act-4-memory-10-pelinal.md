# Act 4 Memory 10 - Pelinal the Bloody

Status: redo slice (largest memory, 40 stages). Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text, not scene phases/actions.
- English is machine-translated from Japanese and is frequently garbled; garbled terms are kept as-is in the source column and flagged. zh-TW translations mark unresolved phrases with `待驗證`.

## Quest Record

[`2A532E zzzCHMemoryQuest10 "Pelinal the Bloody"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401)

CLI:
- `questdiag Vigilant.esm 0x2A532E`
- `infodiag Vigilant.esm 0x2A532E`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x2A532E`
- EditorID: `zzzCHMemoryQuest10`
- Name: `Pelinal the Bloody`
- Flags: `RunOnce`
- Priority: `90`
- Type: `Misc`
- Filter: `CH\`

Stages from `questdiag` (40 stages; only the two `CompleteQuest` and the `ShutDownStage` carry flags, all logs empty):

| Stage | Flags |
|---:|---|
| 0 | `StartUpStage` |
| 10, 20, 30, 32, 34 | none |
| 40, 41, 42, 43, 44, 45, 46, 47, 48 | none |
| 50, 60, 62, 64 | none |
| 70, 80, 90 | none |
| 100, 105, 110, 115, 120 | none |
| 130, 140, 150, 160, 170, 175 | none |
| **180** | **`CompleteQuest`** |
| 190 | none |
| **300** | **`CompleteQuest`** |
| 310, 320, 330 | none |
| 999 | `ShutDownStage` |

Objective:
- `questdiag` reports `Objectives (0)`. The quest carries **no objective text** (the entry at [quests.md:401](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401) is a header line only). This is a non-tracked "memory" quest driven entirely by scenes + stage-gated dialogue.

## Subject

Verified via topic EditorIDs and INFO text:
- **Pelinal** (Pelinal Whitestrake) — the memory's protagonist / the player's role in dialogue.
- **Mary** — Umaril's captive slave, pregnant with Umaril's child; the 180/300 branch hinges on her.
- **Umaril** (Umaril the Feathered) — the Ayleid boss Pelinal kills mid-quest.
- **Molag Bal** ("Bal") — the tempter who frames the moral choice.
- **Korn** — Pelinal's hound (alias `#5`), speaks only `(Bark)` / `(Whine)`.

## Alias / Staging Backbone

Both `SCEN` records below share the same host quest and the same 11-alias roster (from `scenediag`).

Host quest:
- [`2A532E zzzCHMemoryQuest10`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401)

Host-quest aliases from `scenediag`:

| Alias | Name | Fill | NPC source |
|---:|---|---|---|
| 0 | `Umaril` | uniqueActor `2955ED` | [`2955ED zzzCHBossUmaril "Umaril the Feathered"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:761) |
| 1 | `Mary` | uniqueActor `2A0679` | [`2A0679 zzzCHSlaveMary "Mary"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:793) |
| 2 | `Bal` | uniqueActor `2A4000` | [`2A4000 zzzCHBardMemoryPelinal`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:795) |
| 3 | `Prey01` | uniqueActor `29F2F7` | [`29F2F7 zzzCHPreySlave01 "Slave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:767) |
| 4 | `Prey02` | uniqueActor `29F2F9` | [`29F2F9 zzzCHPreySlave02 "Slave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:786) |
| 5 | `Korn` | uniqueActor `2A3FFC` | [`2A3FFC zzzCHMemoryKorn "Korn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:794) |
| 6 | `Pelinal` | uniqueActor `0B0EB3` | [`0B0EB3 zzzCHBossPelinal "Pelinal Whitestrake"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1075) |
| 8 | `UmarilTA` | forcedRef `2A5347` | not printed by CLI (scene-actor ref) |
| 9 | `PelinalMemory` | uniqueActor `2A66C3` | [`2A66C3 zzzCHMemoryPelinal01 "Pelinal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:796) |
| 10 | `MolagBal` | uniqueActor `2A7A0A` | [`2A7A0A zzzCHMolagBalInMemoryPelinal`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:797) |
| 11 | `PelinalTA` | forcedRef `2AA091` | not printed by CLI (scene-actor ref) |

Notes:
- Alias `#7` is not present in the roster (gap between `#6` and `#8`); not an error in this dump, the QUST simply has no alias `#7`.
- `Bal` (alias `#2`, the talking Molag Bal who runs the custom branches) is filled from NPC `zzzCHBardMemoryPelinal` — an inference about the engine: the same actor record (`2A4000`) doubles as the in-memory Molag Bal avatar; the throne-sitting Molag Bal is a separate record [`2A7A0A`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:797) used as alias `#10` `MolagBal` (the `BasScene` voice). (inference)
- `GetIsID` object `2A5346` is the conditioning record for the **Umaril** dialogue branch (alias-less; `GetIsID == 2A5346`). It does not resolve to a row in `npcs.tsv`; treated as the in-memory Umaril speaker record. (inference — needs a direct ESM NPC dump of `2A5346`.)
- `Korn` (alias `#5`) is Pelinal's dog: every Korn INFO is `(Bark)` / `(Whine)`.

Trigger:
- Activator [`4DEF09 zzzCHMeq10GateTrigger "Gate"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv) (`find` result) is the in-world "Gate" that fronts this memory; its exact start hook (stage 0 `StartUpStage` advance) is not decoded here. (inference — verify activator script / XLOC.)

## Scene Records

Two `SCEN` records. Both are owned by quest `2A532E`; the 13 `Scene/Scene` topics are shared between them in the `scenediag` "owned by quest" listing, but each scene's `actions` reference a distinct subset (the `GoodScene` plays the 7 Pelinal-monologue topics + 3 song topics; the `BasScene` plays the 6 song/echo topics). Scene text lines are linked to `dialogue.md`; phases/actions are from `scenediag`.

### 2A66C6 zzzCHMeQ10GoodScene

CLI:
- `scenediag Vigilant.esm 0x2A66C6`

Staging:
- Host quest: [`2A532E zzzCHMemoryQuest10`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401)
- Flags: none
- Actors: alias `#9` (`PelinalMemory`), `behaviorFlags=DeathEnd`, `flags=NoPlayerActivation, Optional`
- Phases: 9 (phase 0 and phase 4 have 2 complete-conditions; the rest 1; no start-conditions)
- Actions (12): a mix of `Package` movement actions and `Dialog` monologue actions, all on actor `#9`.

| Action | Type | Phase | Topic | Line |
|---:|---|---:|---|---|
| 1 | Package | 0 | — | — |
| 2 | Package | 1 | — | — |
| 3 | Dialog | 1 | [`2A66C8`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2549) | storm / rage monologue |
| 4 | Dialog | 2 | [`2A66CA`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2552) | rain after the fight |
| 5 | Dialog | 3 | [`2A66CC`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2555) | Kyne always crying |
| 6 | Dialog | 4 | [`2A66CE`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2558) | my war was over |
| 7 | Package | 2-4 | — | — |
| 8 | Package | 5 | — | — |
| 9 | Package | 6-8 | — | — |
| 10 | Dialog | 6 | [`2A66D7`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2561) | running, sun nearly sunk |
| 11 | Dialog | 7 | [`2A66D9`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2564) | golden wheat field |
| 12 | Dialog | 8 | [`2A66DB`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2567) | wind of Kyne, finally found |

Translations (Pelinal's closing monologue — the "good"/peace arc):
- [`2A66C8` / INFO `2A66C9`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2549): 「我曾是一場風暴，是憤怒本身。所以我衝過戰場，斬下婦孺的首級，焚毀村莊。」
- [`2A66CA` / INFO `2A66CB`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2552): 「戰鬥之後總是下雨。溫柔的雨沖刷、治癒我的身體，把血流向大海的盡頭。」
  - Note: source `It carrued the end of the sea` 拼字錯亂（carried），語意待驗證。
- [`2A66CC` / INFO `2A66CD`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2555): 「我曾這麼想，也想要這麼相信。儘管 Kyne 一直在哭——不是為我，而是為那些倒下的無辜之人。」
- [`2A66CE` / INFO `2A66CF`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2558): 「再沒有弱者流血了。Kyne 不再落淚。我的……我的戰爭結束了……」
- [`2A66D7` / INFO `2A66D8`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2561): 「我一直奔跑著……懺悔之雨若停了，太陽也快沉落遠方。我所渴望的，是那不斷延伸的陰影。」
  - Note: source `Rain of contritionif has stop` 拼字錯亂，語意待驗證。
- [`2A66D9` / INFO `2A66DA`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2564): 「金色的麥田，微風拂過麥穗。女人拍打羽絨被，散落的羽毛化作雪，孩子與狼群在上頭嬉戲。」
- [`2A66DB` / INFO `2A66DC`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2567): 「這是……這就是 Kyne 之風。終於找到了……終於……」

### 2AA092 zzzCHMeQ10BasScene

CLI:
- `scenediag Vigilant.esm 0x2AA092`

Staging:
- Host quest: [`2A532E zzzCHMemoryQuest10`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401)
- Flags: none
- Actors (2): alias `#10` (`MolagBal`) and alias `#11` (`PelinalTA`), both `behaviorFlags=DeathEnd`, `flags=NoPlayerActivation, Optional`
- Phases: 6, each 0 start-conditions / 1 complete-condition.
- Actions (6): Molag Bal (alias `#10`) speaks phases 0-2; Pelinal (alias `#11`) answers phases 3-5. Two of Molag Bal's lines carry `Flags=HeadtrackPlayer`.

| Action | Actor | Phase | Headtrack | Topic | Line |
|---:|---|---:|---|---|---|
| 1 | #10 MolagBal | 0 | Player | [`2AA093`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2570) | insulting his ancestry / Old Ehlnofey |
| 2 | #10 MolagBal | 1 | — | [`2AA095`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2573) | drove the Elvish kings to cut Pelinal into eighths |
| 3 | #10 MolagBal | 2 | Player | [`2AA097`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2576) | "good songs… do you think so, Pelinal?" |
| 4 | #11 PelinalTA | 3 | — | [`2AA099`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2579) | (echo of the storm monologue) |
| 5 | #11 PelinalTA | 4 | — | [`2AA09B`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2582) | (echo of the rain monologue) |
| 6 | #11 PelinalTA | 5 | — | [`2AA09D`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2585) | "when does rain stop… blood to wash blood" |

Translations (Molag Bal narrating the historical butchery of Pelinal; Pelinal's lines are parenthesised echoes of the GoodScene monologue):
- [`2AA093` / INFO `2AA094`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2570): 「……在他之上，辱罵他的血統，以及所有從舊 Ehlnofey 渡海而來的人。」
  - Note: 此句為片段（承接上一句），原文無前文，語意待驗證。
- [`2AA095` / INFO `2AA096`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2573): 「激怒了其餘的精靈諸王，將他們逼向瘋狂，把 Pelinal 砍成八塊。」
- [`2AA097` / INFO `2AA098`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2576): 「這會是首好歌。你不覺得嗎，Pelinal？」
- [`2AA099` / INFO `2AA09A`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2579): 「（我曾是一場風暴，是憤怒本身。所以我衝過戰場，斬下婦孺的首級，焚毀村莊。）」
- [`2AA09B` / INFO `2AA09C`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2582): 「（戰鬥之後總是下雨。溫柔的雨沖刷、治癒我的身體，把血流向大海的盡頭。）」
- [`2AA09D` / INFO `2AA09E`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2585): 「（雨何時才會停……我何時才要以血洗血……）」

## Custom Dialogue Branches

The quest owns **9 `DialogBranch` records** plus one `Hello` topic. Speaker is gated by `GetIsAliasRef` (alias index) + `GetStage`. Alias map for conditions: `#1` = Mary, `#2` = Bal (Molag Bal), `#5` = Korn (dog), and `GetIsID == 2A5346` = Umaril. VMAD `OnEnd` fragments (`CHMeq10_TIF__02<INFO>`) fire on the player choices that advance state.

### Branch: Korn 01 — `2A5335 zzzCHMeQ10KornB01` (stage 30, alias #5)

| Topic | INFO | Flags | Conditions | Source / Translation |
|---|---|---|---|---|
| [`2A5336 zzzCHMeQ10KornB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2474) | `2A5337` | none | `GetStage==30`; alias `#5` | (Bark) |
| [`2A5338 zzzCHMeQ10KornB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2477) | `2A5339` | `Goodbye` | `GetStage==30`; alias `#5`; VMAD `CHMeq10_TIF__022A5339` | Prompt: 「滾開，別煩我」 → (Whine) |

### Branch: Umaril 01 — `2A534C zzzCHMeQ10UmarilB01` (stage 70, GetIsID 2A5346)

The pre-kill confrontation with Umaril the Feathered.

| Topic | INFO | Flags | Conditions | Source / Translation |
|---|---|---|---|---|
| [`2A534D zzzCHMeQ10UmarilB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2480) | `2A534E` | none | `GetStage==70`; `GetIsID 2A5346` | 「你真是樂在殺戮。你像個逗弄昆蟲的嬰孩。」 |
| [`2A534F zzzCHMeQ10B01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2483) | `2A5350` | none | `GetIsID 2A5346`; `GetStage==70` | Prompt: 「……（沉默）」 Response: 「只要 Ada 的污血還沾在大地上，我們的神話紀元就尚未消逝。你和我……」 |
| [`2A5351 zzzCHMeQ10UmarilB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2486) | `2A5352` | `Goodbye` | `GetIsID 2A5346`; `GetStage==70`; VMAD `CHMeq10_TIF__022A5352` | Prompt: 「來吧。我是來殺你的。」 Response: 「當然。等你被斬首之後，我們再談。那時你就願意聽了。」 |

Note: `Ada` = the Aedra / the Divines (Ayleid usage); kept untranslated per ES lore convention.

### Branch: Bal 01 — `2A535A zzzCHMeQ10BalB01` (stage 90, alias #2)

Post-kill: Molag Bal greets Pelinal. Pairs with the `Hello` opener below.

| Topic | INFO | Flags | Conditions | Source / Translation |
|---|---|---|---|---|
| [`2A535B zzzCHMeQ10BalB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2489) | `2A535C` | `SayOnce` | `GetStage==90`; alias `#2` | Prompt: 「你和 Meridia 勾結？」 Response: 「勾結？才不。那位傲慢的老巫婆跟誰都不勾結。不過托她的福，我的買賣才能順利進行。」 |

Note: `Haughty Hag` = Meridia (Molag Bal's derisive epithet). 

### Branch: Bal 02 — `2A535F zzzCHMeQ10BalB02` (stage 90, alias #2)

| Topic | INFO | Flags | Conditions | Source / Translation |
|---|---|---|---|---|
| [`2A5360 zzzCHMeQ10BalB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2499) | `2A5361` | `Goodbye, SayOnce` | `GetStage==90`; alias `#2`; VMAD `CHMeq10_TIF__022A5361` | Prompt: 「你到底想幹嘛？我受夠你的把戲了。」 Response: 「別這麼說嘛。跟我來，我帶你去 Umaril 的工坊看點有趣的。」 |

Note: `Atelier of Umaril` 「Umaril 的工坊／畫室」; ties to location lore "Art of Lost Abagarlas" below.

### Branch: Bal 03 — `2A668F zzzCHMeQ10BalB03` (stage 105, alias #2)

Molag Bal shows Pelinal a gruesome "artwork".

| Topic | INFO | Flags | Conditions | Source / Translation |
|---|---|---|---|---|
| [`2A6690 zzzCHMeQ10BalB03T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2502) | `2A6691` | none | `GetStage==105`; alias `#2` | 「如何？這是失落的 Abagarlas 之藝。他費了好大功夫才做出複製品。」 / 「血雨與堆積的內臟，就像你經歷過的景象。若相遇的方式不同，他會不會成了你的好友呢？」 |
| [`2A6692 zzzCHMeQ10BalB03T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2506) | `2A6693` | `Goodbye` | alias `#2`; VMAD `CHMeQ10_TIF__022A6693` | Prompt: 「噁心。這就是你要給我看的？」 Response: 「看來你不喜歡。那就忘了它吧。」 / 「我真正想給你看的在後面。跟我來。」 |

Note: `Abagarlas` = Ayleid ruin city (lore-real). `He` 指誰待驗證（疑為 Umaril 或某工匠）。

### Branch: Bal 04 — `2A6694 zzzCHMeQ10BalB04` (stage 115, alias #2) — THE CHOICE

This is the branch that frames the 180-vs-300 decision: Molag Bal presents **Mary**, Umaril's pregnant slave, and pushes Pelinal to kill her.

| Topic | INFO | Flags | Conditions | Source / Translation |
|---|---|---|---|---|
| [`2A6695 zzzCHMeQ10BalB04T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2510) | `2A6696` | none | `GetStage==115`; alias `#2` | 「就是這個，這個。」 |
| [`2A6697 zzzCHMeQ10BalB04T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2513) | `2A6698` | none | alias `#2` | Prompt: 「她是？」 Response: 「Umaril 的拋棄式性奴。再過一天她就會被溶進那件『藝術品』裡。」 / 「不過 Umaril 現在死了。真好，你成了她的救命恩人。」 |
| [`2A6699 zzzCHMeQ10BalB04T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2517) | `2A669A` | none | alias `#2` | Prompt: 「你要我做什麼？」 Response: 「她懷著 Umaril 的孩子。你想怎麼做？」 |
| [`2A669B zzzCHMeQ10BalB04T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2520) | `2A669C` | none | alias `#2` | Prompt: 「你是說殺了她？」 Response: 「我不在乎你殺不殺。但你若不殺它，它將來會威脅世人。」 / 「Ada 之血賦予力量，但心智卻脆弱易碎。那血脈的命運，你最清楚不過。」 |
| [`2A669D zzzCHMeQ10BalB04T05`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2524) | `2A669E` | `Goodbye` | alias `#2`; VMAD `CHMeQ10_TIF__022A669E` | Prompt: 「滾。」 Response: 「好吧，你需要點時間決定。好好享受。」 |

### Branch: Mary 01 — `2A66A6 zzzCHMeQ10MaryB01` (stage 130, alias #1)

Reached on the **spare-Mary path**: Pelinal frees Mary and leads her out.

| Topic | INFO | Flags | Conditions | Source / Translation |
|---|---|---|---|---|
| [`2A66A7 zzzCHMeQ10MaryB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2527) | `2A66A8` | none | `GetStage==130`; alias `#1` | 「謝……謝謝你……」 |
| [`2A66A9 zzzCHMeQ10MaryB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2530) | `2A66AA` | none | `GetStage==130`; alias `#1` | Prompt: 「你能走嗎？」 Response: 「能……可是……」 |
| [`2A66AB zzzCHMeQ10B01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2533) | `2A66AC` | none | `GetStage==130`; alias `#1`; VMAD `CHMeq10_TIF__022A66AC` | Prompt: 「走吧，我們走。」 Response: 「好、好的……」 |

### Branch: Korn 02 — `2A66B3 zzzCHMeQ10KornB02` (stage 140, alias #5)

The hound on the spare-Mary path ("secure her").

| Topic | INFO | Flags | Conditions | Source / Translation |
|---|---|---|---|---|
| [`2A66B4 zzzCHMeQ10KornB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2536) | `2A66B5` | none | `GetStage==140`; alias `#5` | (Bark) |
| [`2A66B6 zzzCHMeQ10B02T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2539) | `2A66B7` | `Goodbye` | `GetStage==140`; alias `#5`; VMAD `CHMeq10_TIF__022A66B7` | Prompt: 「看好她。」 → (Bark) |

### Branch: Bal 05 — `2A66BE zzzCHMeQ10B05` (stage 160, alias #2)

Molag Bal's reaction after Pelinal spares Mary — the closing of the mercy path.

| Topic | INFO | Flags | Conditions | Source / Translation |
|---|---|---|---|---|
| [`2A66BF zzzCHMeQ10B05T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2542) | `2A66C0` | none | `GetStage==160`; alias `#2` | 「哎呀哎呀，你沒殺她？這可不像你。」 / 「這樣好嗎？她的孩子會犯下錯誤——比你更大的錯誤。」 |
| [`2A66C1 zzzCHMeQ10BalB05T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2546) | `2A66C2` | `Goodbye` | `GetStage==160`; alias `#2`; VMAD `CHMeq10_TIF__022A66C2` | Prompt: 「未來的人生由她自己決定。我管不著。」 Response: 「……是嗎。有緣再會吧——如果你還能的話。」 |

### Hello — `2A535D zzzCHMeQ10Hello` (no branch, alias #2 / #1)

Stage-varying greetings in ONE Hello topic (precedence by order — see repo memory "Conditioned hello = one topic, many INFOs"):

| INFO | Flags | Conditions | Source / Translation |
|---|---|---|---|
| `2A535E` | `SayOnce` | `GetStage==90`; alias `#2` (Bal) | 「哎呀，Pelinal 先生。漂亮。你的功績將化作歌謠，永世流傳。」 |
| `2A66A2` | `Goodbye` | `GetStage<=120`; alias `#1` (Mary) | 「救命……」 |
| `2A66B0` | `Goodbye, SayOnce` | `GetStage==140`; alias `#1` (Mary) | 「你……你剛才在跟誰說話……」 |
| `2A66B1` | `Goodbye` | `GetStage==140`; alias `#1` (Mary) | 「啊……哈……對不起……」 |

Note: `Splended` = "Splendid" (拼字錯誤，已照語意翻).

## 180 vs 300 — Branch Outcome Map

Both completions are unconditional `CompleteQuest` stage logs (no conditions on the stage), so polarity is read off the **dialogue/scene content reachable on each band**, not off `questdiag`.

- **Stage 180 = the MERCY / "good" completion.** The spare-Mary chain runs entirely in the 130-180 band: Mary branch (`2A66A6`, stage 130) → Korn "secure her" (`2A66B3`, stage 140) → Bal's "you didn't kill her?" reaction (`2A66BE`, stage 160) → complete at 180. The `GoodScene` (`2A66C6`, Pelinal's peace/Kyne's-wind monologue) is the EditorID-named "Good" scene and resolves this arc. **(Polarity: mercy/good — strongly supported by EditorID `GoodScene` + the spare-Mary dialogue.)**
- **Stage 300 = the alternate / "bad" (kill-Mary) completion.** The 190-330 band (stages 190, 300, 310, 320, 330) has **no owned custom-dialogue topics** in `infodiag` — it is driven by stage fragments / packages only (e.g. `zzzCHMeq10PelinalWalkToDie`, `zzzCHMeq10PelinalMeditate`). This is the branch the player reaches by killing Mary at the Bal-04 choice (stage 115), bypassing the Mary/Korn-02/Bal-05 mercy chain. The `BasScene` (`2AA092`, EditorID `BasScene` — inference: "Bad/Base") narrates Molag Bal's grim recounting of Pelinal's historical massacre and dismemberment, fitting the darker outcome. **(Polarity: kill/bad — inference from EditorID `BasScene`, the empty-dialogue 190-330 band, and the kill-routed package names; not as firmly pinned as 180 because no kill-path dialogue exists to quote.)**

Inference on routing:
- The **Bal-04 choice** (`2A6694`, stage 115) is the fork. Choosing mercy advances toward stage 130 (Mary branch) → 180. Choosing to kill skips to the 190+ band → 300. Exact stage-set logic lives in the VMAD `OnEnd` fragments (`CHMeQ10_TIF__022A669E` on "Get out", and the stage fragments), which are not decompiled here. (inference)

## Related Records

Not all owned by quest `2A532E`, but the same Pelinal/Umaril/Mary cast — cross-link in a full reconstruction.

NPCs (memory cast, alias-filled):
- [`0B0EB3 zzzCHBossPelinal "Pelinal Whitestrake"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1075) — alias `#6` Pelinal
- [`2A66C3 zzzCHMemoryPelinal01 "Pelinal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:796) — alias `#9` PelinalMemory (GoodScene actor)
- [`2BC37F zzzCHMemoryPelinal02 "Pelinal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:785) — second Pelinal memory record (BasScene `PelinalTA` candidate, inference)
- [`2955ED zzzCHBossUmaril "Umaril the Feathered"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:761) — alias `#0` Umaril
- [`2A0679 zzzCHSlaveMary "Mary"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:793) — alias `#1` Mary
- [`2A4000 zzzCHBardMemoryPelinal`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:795) — alias `#2` Bal (talking Molag Bal)
- [`2A7A0A zzzCHMolagBalInMemoryPelinal`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:797) — alias `#10` MolagBal (BasScene voice)
- [`2A3FFC zzzCHMemoryKorn "Korn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:794) — alias `#5` Korn (dog)
- [`29F2F7 zzzCHPreySlave01 "Slave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:767) / [`29F2F9 zzzCHPreySlave02 "Slave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:786) — aliases `#3`/`#4` Prey

Locations:
- [`295516 zzzCHMemPelinal "White-Gold Tower"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:619) — the memory's setting (LCTN)
- [`0243F1 zAoMMythicPlace`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:162) — "Mythic" cell (inference: the memory interior)

Books (lore context, not owned by quest; verify before narrative use):
- [`12905C zzzCHBookESOChantTwilight "The Song-Never-Sung-at-Twilight"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:70) — Pelinal/Umaril song lore
- [`140504 zzzCHBalConjurePelinal "Piece of Bal: Pelinal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:253) — Molag Bal conjure-Pelinal item
- [`2C241B zzzCHMeridiaConjureUmaril "Meridia's Beaconl: Umaril"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:506) — Meridia conjure-Umaril item (ties to the "Meridia" line in Bal-01)

## Reconstruction Notes

Source-grounded:
- This memory is [`2A532E zzzCHMemoryQuest10 "Pelinal the Bloody"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401): 40 stages, `StartUpStage` at 0, `CompleteQuest` at 180 and 300, `ShutDownStage` at 999, **no objective text**.
- It contains **2 `SCEN` records**: `2A66C6 GoodScene` (9 phases, 12 actions, Pelinal's peace monologue) and `2AA092 BasScene` (6 phases, Molag Bal narrating Pelinal's massacre while Pelinal echoes the monologue).
- It contains **9 custom `DialogBranch` records + 1 Hello topic**, alias-gated: Korn (dog) ×2, Umaril ×1, Bal (Molag Bal) ×5, Mary ×1. The **Bal-04 branch (stage 115)** is the kill-or-spare fork over **Mary**, Umaril's pregnant slave.
- The mercy path (spare Mary) runs stages 130-180 with the Mary/Korn-02/Bal-05 branches and completes at **180** (`GoodScene`). The kill path runs the empty-dialogue 190-330 band and completes at **300** (`BasScene`).
- VMAD `OnEnd` fragments (`CHMeq10_TIF__02<INFO>`) on the `Goodbye`/decision INFOs drive stage advancement; exact Papyrus not decoded here.

Open verification:
- Decompile/inspect the TIF fragments (`CHMeq10_TIF__022A5339`, `022A5352`, `022A5361`, `022A6693`, `022A669E`, `022A66AC`, `022A66B7`, `022A66C2`) to confirm which one sets the kill-vs-spare stage path (pin the 180/300 routing exactly).
- Dump NPC record `2A5346` (the Umaril branch `GetIsID` object) — not found in `npcs.tsv`; confirm it is the in-memory Umaril speaker.
- Confirm `BasScene` EditorID expansion ("Bad"/"Base") and that the 190-330 band is the kill-Mary outcome (currently inferred from EditorID + empty-dialogue band + `PelinalWalkToDie`/`PelinalMeditate` package names).
- Dump the QUST aliases/targets and the `4DEF09 zzzCHMeq10GateTrigger` activator + start hook to confirm the trigger and the `UmarilTA`/`PelinalTA` forcedRef placements.
- Inspect cells/refs for the White-Gold Tower memory (`295516`) and Mythic Place (`0243F1`) if spatial staging matters.
- The deferred scene-package movement actions (GoodScene actions 1, 2, 7, 8, 9 = `Package` with no topic) carry no text and are not translated here.
