# Act 4 Memory 07 - Temptation of Marukh

Status: first redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- `SCEN` staging comes from CLI diagnostics, because the extracted `dialogue.md` only preserves scene topic text, not scene phases/actions.

## Quest Record

[`06F53C zzzCHMemoryQuest07 "Temptation of Marukh"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101)

CLI:
- `questdiag Vigilant.esm 0x06F53C`
- `infodiag Vigilant.esm 0x06F53C`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x06F53C`
- EditorID: `zzzCHMemoryQuest07`
- Name: `Temptation of Marukh`
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
| 30 | none | empty |
| 40 | none | empty |
| 50 | none | empty |
| 60 | none | empty |
| 70 | CompleteQuest | empty |
| 80 | none | empty |
| 150 | CompleteQuest | empty |
| 160 | none | empty |
| 255 | ShutDownStage | empty |
| 999 | ShutDownStage | empty |

Objective:

| Index | Source | Translation |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:102) | 猿人睡在哪裡？ |

Objective targets:
- 3 targets in ESM.
- Target 1 has 2 conditions.
- Target 2 has 2 conditions.
- Target 3 has 0 conditions.
- Current CLI output does not print target refs; this needs a deeper QUST target dump if target locations matter.

## Alias / Staging Backbone

The four `SCEN` records below share the same host quest and aliases.

Host quest:
- [`06F53C zzzCHMemoryQuest07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101)

Host-quest aliases from `scenediag`:

| Alias | Name | Fill |
|---:|---|---|
| 0 | `EndMarker` | forcedRef `06F53B:Vigilant.esm` |
| 1 | `StartMarker` | forcedRef `06CA17:Vigilant.esm` |
| 3 | `Bard` | forcedRef `06F544:Vigilant.esm` |
| 4 | `Stone` | not printed by CLI |
| 5 | `MolagBal` | uniqueActor `0708BB:Vigilant.esm` |
| 6 | `Alessia` | uniqueActor [`0708BE zzzCHStAlessiaMemoryGhost`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1072) |
| 7 | `TA01` | forcedRef `0708C6:Vigilant.esm` |
| 8 | `TA02` | forcedRef `0708C5:Vigilant.esm` |
| 9 | `GuideMarker02` | forcedRef `42E0B6:Vigilant.esm` |
| 10 | `GuideMarker01` | forcedRef `4307C4:Vigilant.esm` |
| 11 | `GuideKey` | forcedRef `4369F7:Vigilant.esm` |

Inference:
- `TA01` and `TA02` carry scene monologue lines in this memory staging.
- `Alessia` and `MolagBal` are dialogue aliases used by the custom topic branches.
- This is inferred from alias names plus INFO conditions `GetIsAliasRef` alias `#6` and alias `#5`.

## Scene Records

Scene records are not present as full records in `game-data`; the text lines are linked to `dialogue.md`, while phases/actions are from `scenediag`.

### 0708C7 zzzCHMeQ07Sc01

CLI:
- `scenediag Vigilant.esm 0x0708C7`

Staging:
- Host quest: [`06F53C zzzCHMemoryQuest07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101)
- Flags: `Interruptable`
- Actor: alias `#7` (`TA01`)
- Phases: 3, each with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Timer`, actor `#7`, phase 0, `0.5` seconds.
  - index 2: `Dialog`, actor `#7`, phase 1, topic [`0708C8`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:896), emotion `Neutral`.
  - index 3: `Dialog`, actor `#7`, phase 2, topic [`0708CA`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:899), emotion `Neutral`.

Translations:
- [`0708C8` / INFO `0708C9`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:896): 「不知過了多少日子，我在荒野中徘徊。在灼熱的陽光下，我的視線變得模糊，舌頭腫脹，毛髮脫落。」
- [`0708CA` / INFO `0708CB`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:899): 「我為什麼會在這裡，連自己怎麼來的都不知道。我究竟在這片荒野裡尋找什麼？腦中一片朦朧，什麼也想不清。」

### 0708CC zzzCHMeQ07Sc02a

CLI:
- `scenediag Vigilant.esm 0x0708CC`

Staging:
- Host quest: [`06F53C zzzCHMemoryQuest07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101)
- Flags: `Interruptable`
- Actor: alias `#8` (`TA02`)
- Phases: 3, each with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Timer`, actor `#8`, phase 0, `0.1` seconds.
  - index 2: `Dialog`, actor `#8`, phase 1, topic [`0708CD`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:902), emotion `Neutral`.
  - index 3: `Dialog`, actor `#8`, phase 2, topic [`0708CF`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:905), emotion `Neutral`.

Translations:
- [`0708CD` / INFO `0708CE`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:902): 「那是吟遊詩人的屍體。他是被吸血鬼打倒的嗎？還是就在荒野中斷了氣？沒有出路的人，最後就會出現在這片荒野。」
  - Note: source phrase `Without this outlet` is unclear; translated as 「沒有出路」.
- [`0708CF` / INFO `0708D0`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:905): 「不論是哪一種，照這樣下去，我也會和這個人走向同樣的命運。」

### 0708D1 zzzCHMeQ07SC03

CLI:
- `scenediag Vigilant.esm 0x0708D1`

Staging:
- Host quest: [`06F53C zzzCHMemoryQuest07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101)
- Flags: none
- Actor: alias `#8` (`TA02`)
- Phases: 3, each with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Timer`, actor `#8`, phase 0, `0.1` seconds.
  - index 2: `Dialog`, actor `#8`, phase 1, topic [`0708D2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:908), emotion `Neutral`.
  - index 3: `Dialog`, actor `#8`, phase 2, topic [`0708D4`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:911), emotion `Neutral`.

Translations:
- [`0708D2` / INFO `0708D3`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:908): 「三隻眼睛已經看不見了，舌頭腫脹，聲音也耗盡了。我很快就會死。」
  - Note: source phrase `Eyes of the three` is unresolved; likely needs NPC/model verification.
- [`0708D4` / INFO `0708D5`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:911): 「唯一的遺憾，是最後沒能再見到你，親愛的 Dulsa。」

### 0708D6 zzzCHMeQ07Sc02b

CLI:
- `scenediag Vigilant.esm 0x0708D6`

Staging:
- Host quest: [`06F53C zzzCHMemoryQuest07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101)
- Flags: none
- Actor: alias `#8` (`TA02`)
- Actor behavior flags: `DeathEnd`, `CombatEnd`, `DialoguePause`
- Phases: 3, each with 0 start conditions and 1 complete condition.
- Actions:
  - index 1: `Timer`, actor `#8`, phase 0, `0.1` seconds.
  - index 2: `Dialog`, actor `#8`, phase 1, topic [`0708D7`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:914), emotion `Neutral`.
  - index 3: `Dialog`, actor `#8`, phase 2, topic [`0708D9`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:917), emotion `Neutral`.

Translations:
- [`0708D7` / INFO `0708D8`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:914): 「一碰到那石頭，就像被灼燒一樣，熱量連同靈魂都被吸走。這塊石頭，難道就是荒野中吸血鬼的真身嗎？」
- [`0708D9` / INFO `0708DA`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:917): 「必須記下來：這石頭也吞噬了成千上萬人的靈魂。被困住的靈魂在石中劇烈翻攪。」

## Custom Dialogue Branch: Alessia

Branch:
- `0731F4:Vigilant.esm` (`zzzCHMeQ07AlessiaB01`)

Speaker condition pattern:
- Most INFOs require `GetIsAliasRef == 1` on alias `#6` (`Alessia`).
- Opening line also requires `GetStage == 40` on quest `06F53C`.

| Topic | INFO | Flags | Conditions | TIF / routing | Translation |
|---|---|---|---|---|---|
| [`0731F5 zzzCHMeQ07AlessiaB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:920) | `0731F6` | none | `GetStage == 40`; `GetIsAliasRef alias #6` | — | 「Marukh，你聽得見我嗎？」 |
| [`0731F7 zzzCHMeQ07AlessiaB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:923) | `0731F8` | none | `GetIsAliasRef alias #6` | — | Prompt: 「Al-Esh 女王……為什麼？」 Response: 「那是因為這塊石頭。Adabaru 已經失落；若能讓它重獲光輝，至今仍在延續的戰爭便會終結。」 |
| [`0731F9 zzzCHMeQ07AlessiaB01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:926) | `0731FA` | none | `GetIsAliasRef alias #6` | — | Prompt: 「你為什麼……？」 Response: 「填滿那塊石頭。復甦的 Adabaru，我要將它安置於塔中。那就是你的使命。」 |
| [`0731FB zzzCHMeQ07AlessiaB01T04`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:929) | `0731FC` | `Goodbye`, `SayOnce` | `GetIsAliasRef alias #6` | `CHMeq07_TIF__020731FC.Fragment_0` → `SetStage(150)` → **Bad outcome** | Prompt: 「我明白。願你慈悲……」 Response: 「我期待著你。因為這是只有你才能做到的事。」 |
| [`0731FD zzzCHMeQ07AlessiaB01T05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:932) | `0731FE` | `Goodbye` | `GetIsAliasRef alias #6` | — | Prompt: 「……」 Response: 「怎麼了？」 |

Branch polarity note: Alessia T04 is *compliance with the divine mandate* (fill the stone). The TIF routes to `SetStage(150)` which is the same `CompleteQuest` stage reached by Molag Bal's "submit" line — both are the **bad / corruption outcome** (Marukh surrenders his soul to fill `zzzCHEyeOfMarukh`). Source: `CHMeq07_TIF__020731FC.psc` line 9.

## Custom Dialogue Branch: Molag Bal

Branch:
- `073200:Vigilant.esm` (`zzzCHMeQ07MolagB01`)

Speaker condition pattern:
- Most INFOs require `GetIsAliasRef == 1` on alias `#5` (`MolagBal`).
- Opening line also requires `GetStage == 50` on quest `06F53C`.

| Topic | INFO | Flags | Conditions | TIF / routing | Translation |
|---|---|---|---|---|---|
| [`073201 zzzCHMeQ07MolagB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:935) | `073202` | none | `GetStage == 50`; `GetIsAliasRef alias #5` | — | 「多麼沒用的肉偶……我本來還以為它不錯……」 |
| [`073203 zzzCHMeQ07MolagB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:938) | `073204` | none | `GetIsAliasRef alias #5` | — | Prompt: 「我想起你了，吸血鬼。」 Response: 「哦，你想起我了。但局面不會因此改變。異類注定要在這片荒野中腐爛。不過，只有一條路值得稱許。向我們純白地屈服。用靈魂把那塊石頭填滿。」 |
| [`073205 zzzCHMeQ07MolagBal01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:942) | `073206` | `Goodbye`, `SayOnce` | `GetIsAliasRef alias #5` | `CHMeq07_TIF__02073206.Fragment_0` → `SetStage(150)` → **Bad outcome** | Prompt: 「我知道了，讓我離開這裡。」 Response: 「你終於找到我了嗎。按約定，我會讓異類離開這裡。我會稍微改造你的心智。」 |
| [`073207 zzzCHMeQ07MolagBal01T04`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:945) | `073208` | `SayOnce` | `GetItemCount > 0` on Player for [`071CE2 zzzCHEyeOfMarukh`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1006); `GetIsAliasRef alias #5` | — | Prompt: 「你也曾經是人。你吃了什麼？（Marukh 之眼）」 Response: 「異類的眼睛似乎比凡人看得更遠。那麼，究竟如何呢？過去吃過什麼之類的事，我已經不太記得了。比起那個，讓我們得到答案吧。是屈服，還是死亡？」 |
| [`073209 zzzCHMeQ07MolagBal01T05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:949) | `07320A` | `Goodbye`, `SayOnce` | `GetIsAliasRef alias #5` | `CHMeq07_TIF__0207320A.Fragment_0` → `SetStage(60)` → Sc03 ForceStart → `SF_zzzCHMeQ07SC03.Fragment_0` → `SetStage(70)` → **Good outcome** | Prompt: 「我會死在這裡。Ikanuzo 想要的是異類。」 Response: 「好吧，若你能離開這片腐朽之地。」 |

Translation notes:
- `White submission` in [`073203`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:940) is semantically unclear; translated literally as 「純白地屈服」 for now.
- `Ikanuzo` in [`073209`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:949) needs verification; likely a mistranscribed/localized proper noun or phrase.

## Related Records

These are not all part of quest `06F53C` according to `infodiag`, but they are Marukh/Alessian context and should be cross-linked in a full reconstruction.

NPCs:
- [`05ADEF zzzCHMarukhMemory` - Marukh](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1046)
- [`11D025 zzzCHMarukh` - Marukh](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:516)
- [`0708BE zzzCHStAlessiaMemoryGhost` - Alessia](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1072)
- [`13206F zzzCHStAlessia` - Alessia](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:559)

Items:
- [`071CE2 zzzCHEyeOfMarukh` - `[*] Eye of Marukh`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1006)
- [`080D21 zzzCHSkinMarukh`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:219)
- [`500DC4 zzzCHSkinImgaHumanMarukh`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:411)
- [`500DC6 zzzCHArmorImgaMonkMarukh` - Imga Monk Robe](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:413)

Books:
- [`12905F zzzCHBookESOIllsuionDeath "The Illusion of Death"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:131)

## Related Book Translation

[`12905F zzzCHBookESOIllsuionDeath "The Illusion of Death"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:131)

CLI:
- `booktext Vigilant.esm 0x12905F`
- Result: failed with `could not extract English strings`; source therefore uses the already extracted `game-data` text.

Translation:

```text
先知 Marukh 與 Alessia 之靈相遇的殘篇記述。

……後來，因為他曾玩弄猿女 Dulsa，
Maruhk [原文如此] 便在石草原上度過他的百年懺悔。
他的視力被灼毀，舌頭腫脹，皮毛斑駁，
左手拇指永遠指向塔之星辰。Al-Esh 的影子也不斷對他說話，
那些鋸齒般的言語刮擦著他的概念器官，透過苦難將他帶向智慧。

他以自己的猿血，在乞求峭壁上用符文記下她的話；
血中的火焰把七十七條不屈教義刻進石面。
雖然這勞作耗盡了他，甚至吞噬了他的本質，他仍不吝惜自己，
因為他知道死亡是一種幻象。Al-Esh 雖已死去，不仍以刀刃般的話語存續嗎？
Pelin-Al 雖也在 Umar-Il 之死時死去，不也見證了她的死亡嗎？
於是 Maruhk 明白了正確抵達之道：獻身於正命與 Ehlnofic 廢止者，將存續於死亡幻象之外。
因為確實如此，驅逐腐化的意志甚至能征服 Arkay 的循環。
```

Source-grounded links to Memory 07:
- `Dulsa` appears in the book source and in scene topic [`0708D4`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:911).
- `Al-Esh` / Alessia links to branch [`0731F5-0731FD`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:920).
- `Seventy-Seven Inflexible Doctrines` links to the extracted book and to the Marukh-adjacent dialogue found in old raw extraction; this needs a direct source link before use in final narrative.
- The book's physical afflictions match the scene topic chain [`0708C8-0708D2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:896): hazy sight, swollen tongue, missing hair, and impending death.

## Branch Polarity and Stage Routing (RESOLVED)

Source: `qf_zzzchmemoryquest07_0206f53c.psc`, `sf_zzzchmeq07sc02b_020708d6.psc`, `sf_zzzchmeq07sc03_020708d1.psc`, `chmeq07_tif__020731fc.psc`, `chmeq07_tif__02073206.psc`, `chmeq07_tif__0207320a.psc`.

### Stage flow (psc-verified)

| Stage | Trigger | Key action |
|---:|---|---|
| 0 | QF `Fragment_0` | Fade-out; move player to `StartMarker`; disable `CurseMarker`; apply `DizzyISM`; start `Sc01` (`0708C7`) |
| 10 | QF `Fragment_9` | Stop `Sc01`; start `Sc02` (`0708CC`) |
| 20/30 | QF `Fragment_11` | Apply `DizzyISM`; play `HeartBeat`; damage player HP×0.5; stop `Sc02`; ForceStart `Sc02b` (`0708D6`); RegisterSceneSkip → stage 40 |
| 40 | `SF_zzzCHMeQ07Sc02b.Fragment_0` (Sc02b on-end) | `SetStage(40)` — opens Alessia branch (opener condition `GetStage==40`, alias `#6`) |
| 40 | QF `Fragment_14` | Enable `Alias_Alessia` ref |
| 50 | QF `Fragment_16` | Place `Summon` at Bard ref; disable Bard; enable `MolagBal` — opens Molag Bal branch (opener condition `GetStage==50`, alias `#5`) |
| 60 | QF `Fragment_18` | TeleportOut VFX on MolagBal; fade MolagBal alpha → 0; ForceStart `Sc03` (`0708D1`); RegisterSceneSkip → stage 70 |
| 70 | `SF_zzzCHMeQ07SC03.Fragment_0` (Sc03 on-end) | `SetStage(70)` → **CompleteQuest** |
| 70 | QF `Fragment_22` | `SetObjectiveCompleted(0)`; `qGuide.SetStage(70)`; `ModRadiance(3.0)` — **Good outcome** |
| 80 | QF `Fragment_1` | `Karma.Mod(+3.0)`; show `KarmaUP`; fade-out; move player to `EndMarker`; `SetStage(80)` |
| 150 | (TIF routing, see below) | **CompleteQuest** at stage 150 |
| 150 | QF `Fragment_20` | `Alias_Alessia.AddItem(Alias_Stone.GetRef())`; `TryToDisable` Alessia and MolagBal — **Bad outcome** (stone given away) |
| 160 | QF `Fragment_7` | `Karma.Mod(-3.0)`; show `KarmaDown`; fade-out; move player to `EndMarker`; `SetStage(160)` |

Note: QF fragment index → stage mapping is inferred from fragment ordering and the stage number each fragment calls `SetStage` into; the exact fragment-to-log-entry binding is not printed by the CLI. Stage 80 vs 160 karma assignments above reflect the respective QF fragments (`Fragment_1` Karma+3, `Fragment_7` Karma-3) which follow the two `CompleteQuest` stages. Stage 80 follows stage 70 (good), stage 160 follows stage 150 (bad) — (inference, not CLI-verified from log-entry binding, but consistent with all psc evidence).

### Branch polarity (RESOLVED)

**Good outcome → stage 70 → `CompleteQuest`**
- Triggered by: `CHMeq07_TIF__0207320A.Fragment_0` (`chmeq07_tif__0207320a.psc` line 9) — player selects Molag Bal T05 "我會死在這裡" → `SetStage(60)` → engine triggers QF `Fragment_18` → ForceStart Sc03 → Sc03 on-end `SetStage(70)` → CompleteQuest + `ModRadiance(3.0)` + `qGuide.SetStage(70)`
- Karma result: `Karma.Mod(+3.0)` via QF `Fragment_1` at stage 80.
- Meaning: Marukh **refuses both temptations** (Molag Bal's corruption and Alessia's demand to fill the stone) and accepts death; the quest resolves as a triumph of integrity.

**Bad outcome → stage 150 → `CompleteQuest`**
- Triggered by either:
  - `CHMeq07_TIF__020731FC.Fragment_0` (`chmeq07_tif__020731fc.psc` line 9) — player selects Alessia T04 "我明白。願你慈悲……" → `SetStage(150)` (Marukh agrees to fill the Eye of Marukh with his soul as Alessia demands)
  - `CHMeq07_TIF__02073206.Fragment_0` (`chmeq07_tif__02073206.psc` line 9) — player selects Molag Bal T03 "我知道了，讓我離開這裡" → `SetStage(150)` (Marukh submits to Molag Bal's "mind alteration")
- Both bad-path TIFs call `SetStage(150)` identically; stage 150 then runs QF `Fragment_20`: Alessia receives the Stone (`AddItem(Alias_Stone.GetRef())`), both Alessia and MolagBal are disabled.
- Karma result: `Karma.Mod(-3.0)` via QF `Fragment_7` at stage 160.
- Meaning: Marukh surrenders his essence — whether to the divine mandate or to daedric corruption, both are a **loss of self** (same mechanical outcome, thematically distinct but structurally unified under stage 150).

### Karma global

The `Karma` global is referenced in `qf_zzzchmemoryquest07_0206f53c.psc` via property `GlobalVariable Property kARMA Auto` and `Karma.Mod(±3.0)` calls. The property name `kARMA` and the per-memory quest pattern match the global `0x020B19F4 zzzCHKarma` (referenced in the MemoryGuide hub context); exact FormID cross-link is (inference — the property name is the only psc evidence here, FormID binding not directly printed by CLI).

### Hub wiring (`zzzCHMemoryGuide 0x42E0B1`)

Source: `qf_zzzchmemoryguide_0242e0b1.psc`, `chmemoryguidequestscript.psc`.

- The hub calls `qGuide.SetStage(70)` from within MeQ07's QF `Fragment_22` (good outcome). This matches `QF_zzzCHMemoryGuide.Fragment_14` (stage 70 in the guide) which fires the comment `;Dream07 Finished`.
- The hub's `TraceON`/`TraceOFF` functions in `CHMemoryGuideQuestScript` start each dream quest if not completed, or un-complete objectives if already done (for `AllowRepeatedStages` replays).
- The hub's 14 stages (0–120 + 999) track the three Song-of-Pelinal sub-quests (Dream10–12, objectives 100/110/120); Dream01–09 stage completions are recorded but do not advance hub objectives on their own. The karma threshold for the overall Act IV ending is **not stored as a hub stage** — it is read from the `zzzCHKarma` global at endgame (unverified: hub psc has no karma read; the global is written by per-memory quest fragments and presumably read by the Act IV culminating quest).

## Reconstruction Notes

Source-grounded:
- This memory is represented by [`06F53C zzzCHMemoryQuest07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101) with objective [`Where do the ape sleep?`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:102).
- It contains four `SCEN` records (`0708C7`, `0708CC`, `0708D1`, `0708D6`) staging short monologues through aliases `TA01` and `TA02`.
- It contains two custom dialogue branches:
  - Alessia alias `#6`, stage-gated at stage 40 for the opener.
  - Molag Bal alias `#5`, stage-gated at stage 50 for the opener.
- Branch routing and karma polarity are now psc-verified (see "Branch Polarity and Stage Routing" above).

Open verification:
- ~~inspect scripts `CHMeq07_TIF__020731FC`, `CHMeq07_TIF__02073206`, `CHMeq07_TIF__0207320A`~~ **RESOLVED**: all three TIF scripts read; routing confirmed.
- inspect QUST aliases directly if a richer alias dump is available (currently CLI does not print target refs for the 3 objective targets); the `Stone` alias `#4` fill ref is not printed by CLI — spatial staging for `CurseMarker` and the stone object is unverified.
- inspect cells/refs for `StartMarker`, `EndMarker`, `Bard`, `TA01`, `TA02`, and the guide markers if spatial staging matters — still unverified.
- inspect object/item record details for [`zzzCHEyeOfMarukh`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1006) if gameplay function matters beyond the dialogue condition — still unverified.
- `kARMA` property → FormID `0x020B19F4 zzzCHKarma` cross-link is (inference); verify by checking the global record or another quest that explicitly names the FormID.
- The Act IV karma threshold (what global value triggers which ending) is not traced here; it lives in the culminating Act IV quest, not in MeQ07 or the hub.
