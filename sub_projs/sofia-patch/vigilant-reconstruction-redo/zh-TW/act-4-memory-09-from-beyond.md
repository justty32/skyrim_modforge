# 第 4 章記憶 09 - From Beyond

狀態：重新製作切片（redo slice）。以原始資料為基礎，連結優先，並非劇情摘要。

來源方針：
- 原始對話行連結回提取的原始文件，而非全文複製。
- 僅在需要解釋翻譯問題時顯示短小的原始片段。
- `SCEN` 舞台編排來自 CLI 診斷，因為提取的 `dialogue.md` 僅保留場景主題文本，不保留場景階段/動作。
- 主角為 **Lamae**；此記憶與 MeQ08（也是 Lamae）交叉連結，但此處僅重建 MeQ09 所屬紀錄（`infodiag` 所有者 = `2CAE30`）。

## 任務紀錄 (Quest Record)

[`2CAE30 zzzCHMemoryQuest09 "From Beyond"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)

CLI：
- `questdiag Vigilant.esm 0x2CAE30`
- `infodiag Vigilant.esm 0x2CAE30`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務中繼資料：
- FormID：`Vigilant.esm:0x2CAE30`
- EditorID：`zzzCHMemoryQuest09`
- 名稱：`From Beyond`
- 標記 (Flags)：`RunOnce`
- 優先級 (Priority)：`95`（高於通常記憶任務的 `90`）
- 類型 (Type)：`Misc`
- 過濾器 (Filter)：`CH\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 標記 | 日誌 |
|---:|---|---|
| 0 | 無 | 空白 |
| 10 | 無 | 空白 |
| 20 | 無 | 空白 |
| 30 | 無 | 空白 |
| 40 | 無 | 空白 |
| 50 | 無 | 空白 |
| 100 | 無 | 空白 |
| 110 | 無 | 空白 |
| 120 | 無 | 空白 |
| 130 | 無 | 空白 |
| 140 | 無 | 空白 |
| 150 | CompleteQuest | 空白 |
| 200 | CompleteQuest | 空白 |
| 999 | ShutDownStage + CompleteQuest | 空白 |

目標 (Objective)：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:266) | `Aah, fetus. Alas, Fetus`（保留原文：英文本身為破碎/雙關句，疑為 "Aah, fetus" / "Alas, Fetus" 的刻意重複；待驗證） |
- 備註：目標文本 `Aah, fetus. Alas, Fetus` 保持原樣。這是一個故意破碎/錯亂的句子（任務的「死產之主 / 胎兒」母題，參見場景主題 `2CC1F6`），並非正常的任務目標；請勿將其正常化。
- 該目標有 1 個目標物與 0 個條件（`questdiag`）；CLI 未印出目標參考（target ref）。

## 完成任務階段對照表 (150 / 200 / 999)

此任務有 **三個** `CompleteQuest` 階段。極性是根據分支開啟者（見下文）讀取的，而非來自 `questdiag`：

| 階段 | 標記 | 映射結果（除非另有說明，否則為推論） |
|---:|---|---|
| 150 | CompleteQuest | 兩個謝爾格拉（Sheogorath）分支結局之一。謝爾格拉分支（`2CC20E`）在其開啟者 `2CC20F` 處有 `GetStage == 50` 的階段門檻，且其兩個最終玩家選擇（`2CC213` "Nevertheless......." 與 `2CC215` "Enough......."）各自帶有 `Goodbye`+VMAD 片段 —— 這些即為兩個完成點。**150/200 之中哪一個是「屈服於 Sithis/莫拉格·巴爾」與「拒絕並再次沉睡」無法僅從 `questdiag` 判定 —— 待辦**（需反編譯 `CHMeq09_TIF__022CC214` / `CHMeq09_TIF__022CC216`）。 |
| 200 | CompleteQuest | 另一個謝爾格拉分支結局（與 150 成對）。待辦極性判定。 |
| 999 | ShutDownStage + CompleteQuest | 關閉 / 清理完成（標準的記憶任務拆除階段）。並非玩家選擇的結果。 |

推論（僅限基於原始資料的結構）：兩個帶有 VMAD 的最終選擇 `2CC214` 與 `2CC216` 是引導至階段 150 與 200 的兩個分支結局；999 是拆除階段。需透過反編譯這兩個片段腳本來確認。

## 別名 / 編排骨幹 (Alias / Staging Backbone)

所有七個 `SCEN` 紀錄共享相同的宿主任務與 14 個別名列表。

宿主任務：
- [`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)

來自 `scenediag` 的宿主任務別名：

| 別名 | 名稱 | 填充 |
|---:|---|---|
| 0 | `Sheogorath` | uniqueActor [`2C8797 zzzCHSheogorathMemoryMad`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:823) |
| 1 | `Lamae` | uniqueActor [`2C8784 zzzCHLamaeMemoryMad`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:821) |
| 3 | `BardTA01` | forcedRef `2CAE31:Vigilant.esm` |
| 4 | `BardTA02` | forcedRef `2CAE36:Vigilant.esm` |
| 5 | `SheoTA01` | forcedRef `2C9AF0:Vigilant.esm` |
| 6 | `SheoTA02` | forcedRef `2C9AF1:Vigilant.esm` |
| 7 | `SheoTA03` | forcedRef `2C9AF2:Vigilant.esm` |
| 8 | `MolagBal` | uniqueActor [`2BC374 zzzCHMemoryMolagBalMad` - Molag Bal](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:777) |
| 9 | `Jacob` | uniqueActor [`2DD387 zzzCHVigilantElderMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:810) |
| 10 | `WGBardTA01` | forcedRef `2E3487:Vigilant.esm` |
| 11 | `WGBardTA02` | forcedRef `2E3486:Vigilant.esm` |
| 12 | `Fox` | uniqueActor [`2E3483 zzzCHMemoryFox` - Shor](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:807) |
| 13 | `Tsun` | uniqueActor [`2DE6ED zzzCHMemoryTsun` - Tsun](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:808) |
| 14 | `GuideMarker` | forcedRef `2C9AE7:Vigilant.esm` |

推論：
- `BardTA0x`、`SheoTA0x`、`WGBardTA0x` 是場景獨白中的「說話演員」參考（負責遊蕩 / 幻覺獨白對話）。
- `Sheogorath`（別名 `#0`）、`Tsun`（別名 `#13`）與 `Lamae`（別名 `#1`）是三個自定義分支使用的對話講者。
- `Fox`/`Shor` 別名（`#12`）與 `Jacob` 別名（`#9`）出現在場景 `WGBardSc02` 的 Packages 中；`MolagBal`（`#8`）有一個「什麼都不做」的 Package ([`2CE891 zzzCHMeQ09MolagBalDoNoting`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2719)) —— 僅用於編排，無所屬主題 (Topic)。
- 這是根據別名名稱以及別名 `#0` (Sheogorath)、別名 `#1` (Lamae)、別名 `#13` (Tsun) 上的 INFO 條件 `GetIsAliasRef` 推論而來。參見下文的分支。
- `Fox` 在 `npcs.tsv` 中被命名為 `Shor` —— 「Fox (狐狸)」/「Shor」的身份是來源本身的命名，並非翻譯選擇。

由任務擁有的支援紀錄（來自 `find zzzCHMeQ09`，僅用於編排，無對話）：
- 觸發器 (Activator) [`2CFBCF zzzCHMeq09MovePlayerTRG`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2719) —— 玩家移動觸發器。
- 程序 (Packages)：`2CE891 zzzCHMeQ09MolagBalDoNoting`、`2E3488 zzzCHMeq09FoxEscortPlayer`、`2E47D5 zzzCHMeq09JacobFindYou`、`2E6E97 zzzCHMeq09FoxAvoidPlayer`、`2E6EA8 zzzCHMeq09JacobSearchBody`。
- 備註：上方的觸發器與 Package FormID 不在 `dialogue.md` 中；連結僅為佔位符。它們僅來自 `find`/`scenediag`。

## 場景紀錄 (Scene Records)

場景紀錄在 `game-data` 中未以完整紀錄形式存在；文本行連結至 `dialogue.md`，而階段/動作則來自 `scenediag`。所有場景 INFO 的 `conds=0`（無條件獨白）。

### 2CC1E3 zzzCHMeQ09BardSc01

CLI：
- `scenediag Vigilant.esm 0x2CC1E3`

編排：
- 宿主任務：[`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- 標記：無
- 演員：別名 `#3` (`BardTA01`)
- 階段 (Phases)：3 個，每個都有 0 個開始條件與 1 個完成條件。
- 動作 (Actions)（均為 `Dialog`, `Neutral`）：
  - 索引 1：階段 0，主題 [`2CC1E4`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2695)。
  - 索引 2：階段 1，主題 [`2CC1E6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2698)。
  - 索引 3：階段 2，主題 [`2CC1E8`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2701)。

翻譯：
- [`2CC1E4` / INFO `2CC1E5`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2695)：「很久……很久以前，當古老的微光仍是一棵年幼的樹。它沉睡在 Kyne 的搖籃裡。」
  - 備註：來源 `sicnce elder gleam was still young tree`（`sicnce`=`since` 拼錯；`elder gleam` = 「古老的微光」待驗證，疑指世界之樹/起源之光）。
- [`2CC1E6` / INFO `2CC1E7`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2698)：「沒有人盼望它甦醒。但它穿越成千上萬的根，向著光前進，如今已爬出地面。」
  - 備註：來源 `crawl out`（時態/語法破碎，原文如此）。
- [`2CC1E8` / INFO `2CC1E9`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2701)：「生鏽的鐘聲正呼喚我的名字。赤紅的雙眼看進我的心。一切沒入黑暗……如其所是。」

### 2CC1EA zzzCHMeQ09BardSc02

CLI：
- `scenediag Vigilant.esm 0x2CC1EA`

編排：
- 宿主任務：[`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- 標記：無
- 演員：別名 `#4` (`BardTA02`)
- 階段：2 個，每個都有 0 個開始條件與 1 個完成條件。
- 動作（均為 `Dialog`, `Neutral`）：
  - 索引 1：階段 0，主題 [`2CC1EB`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2704)。
  - 索引 2：階段 1，主題 [`2CC1ED`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2707)。

翻譯：
- [`2CC1EB` / INFO `2CC1EC`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2704)：「瘋狂是從何時開始俯視我的？我在表面的身影，又是從何時變成醜陋的怪物？」
  - 備註：來源 `maddness`（=`madness` 拼錯）、`over look`（=`overlook`，原文如此）。
- [`2CC1ED` / INFO `2CC1EE`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2707)：「我是誰？我要去往何方？為了什麼……我為何要尋找這個……」

### 2CC1EF zzzCHMeQ09SheoSc01

CLI：
- `scenediag Vigilant.esm 0x2CC1EF`

編排：
- 宿主任務：[`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- 標記：無
- 演員：別名 `#5` (`SheoTA01`)
- 階段：1 個，具有 0 個開始條件與 1 個完成條件。
- 動作：
  - 索引 1：`Dialog`，階段 0，主題 [`2CC1F2`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2710)，情感 `Neutral`。

翻譯：
- [`2CC1F2` / INFO `2CC1F3`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2710)：「那不可能是 Shezarr。也不可能是它的先知。」
  - 備註：`Shezarr` ＝ 消失的 Shezarrine/Lorkhan 對應神格（原文如此，非錯字）。

### 2CC1F0 zzzCHMeQ09SheoSc02

CLI：
- `scenediag Vigilant.esm 0x2CC1F0`

編排：
- 宿主任務：[`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- 標記：無
- 演員：別名 `#6` (`SheoTA02`)
- 階段：1 個，具有 0 個開始條件與 1 個完成條件。
- 動作：
  - 索引 1：`Dialog`，階段 0，主題 [`2CC1F4`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2713)，情感 `Neutral`。

翻譯：
- [`2CC1F4` / INFO `2CC1F5`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2713)：「它是仿造之物，不過是 Sithis 的蒼白野獸。」
  - 備註：`mimic`（仿造／擬態）、`Pale beast of Sithis`（Sithis 的蒼白野獸）原文如此。

### 2CC1F1 zzzCHMeQ09SheoSc03

CLI：
- `scenediag Vigilant.esm 0x2CC1F1`

編排：
- 宿主任務：[`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- 標記：無
- 演員：別名 `#7` (`SheoTA03`)
- 階段：1 個，具有 0 個開始條件與 1 個完成條件。
- 動作：
  - 索引 1：`Dialog`，階段 0，主題 [`2CC1F6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2716)，情感 `Neutral`。

翻譯：
- [`2CC1F6` / INFO `2CC1F7`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2716)：「死產之主的呼喚如鐘聲般響起，而一切渴望都將以鮮血實現。」
  - 備註：`stillborn lord`（死產之主）呼應任務目標的 `fetus` 母題；原文如此。

### 2E47CE zzzCHMeQ09WGBardSc01

CLI：
- `scenediag Vigilant.esm 0x2E47CE`

編排：
- 宿主任務：[`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- 標記：無
- 演員：別名 `#10` (`WGBardTA01`)，演員標記 `NoPlayerActivation`, `Optional`
- 階段：1 個，具有 0 個開始條件與 1 個完成條件。
- 動作：
  - 索引 1：`Dialog`，階段 0，主題 [`2E47CF`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2770)，情感 `Neutral`。

翻譯：
- [`2E47CF` / INFO `2E47D0`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2770)：「我曾是誰？我身在何處？燒焦的日記什麼也教不了我……」
  - 備註：`Burned Diary`（燒焦的日記）疑與 [`01C7F6 zzzAoMDiaryAltano "Altano's Diary"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:644)（同樣「燒毀／撕碎」的日記）相關，但非本任務所屬，待驗證。

### 2E47D1 zzzCHMeQ09WGBardSc02

CLI：
- `scenediag Vigilant.esm 0x2E47D1`

編排：
- 宿主任務：[`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)
- 標記：無
- 演員：別名 `#11` (`WGBardTA02`) 與別名 `#9` (`Jacob`)，皆為 `NoPlayerActivation`, `Optional`
- 階段：3 個，每個都有 0 個開始條件與 1 個完成條件。
- 動作：
  - 索引 1：`Dialog`，演員 `#11`，階段 0，主題 [`2E47D2`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2773)，`Neutral`。
  - 索引 2：`Package`，演員 `#9` (Jacob)，階段 1（無主題）。
  - 索引 3：`Package`，演員 `#9` (Jacob)，階段 2（無主題）。
  - 索引 4：`Dialog`，演員 `#9` (Jacob)，階段 1，標記 `HeadtrackPlayer`，無主題（沉默 / 目光跟隨玩家）。
  - 索引 5：`Dialog`，演員 `#9` (Jacob)，階段 2，主題 [`2E47D6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2776)，`Neutral`。

翻譯：
- [`2E47D2` / INFO `2E47D3`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2773)：「我……我失去了某種珍貴的東西……但如今一切都……」
  - 備註：來源 `prescious`（=`precious` 拼錯）。
- [`2E47D6` / INFO `2E47D7`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2776)：「醒來……醒來……不要睡去……」
  - 由 Jacob（別名 `#9`）說出，呼應 Tsun 分支的「Souless (無魂者)」主題。

## 自定義對話分支：Lamae

分支：
- [`2CC20A zzzCHMeQ09LamaeB01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2719) (DialogView [`2CC209 zzzCHMeQ09LamaeView`])

講者條件模式：
- INFO 要求在別名 `#1` (`Lamae`) 上滿足 `GetIsAliasRef == 1`。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`2CC20B zzzCHMeQ09LamaeB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2719) | `2CC20C` | `Goodbye` | `GetIsAliasRef alias #1` | (Fear) 「你這怪物……離我遠一點……」 |

推論：
- 單行，`Goodbye`，情感 `Fear`：Lamae 在覺醒的玩家（作為復仇者）面前退縮。這是 Lamae 與 MeQ08（也是 Lamae）的交叉連結，但此 INFO 由 `2CAE30` 擁有。

## 自定義對話分支：謝爾格拉 (Sheogorath)

分支：
- [`2CC20E zzzCHMeQ09SheoB01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2722) (DialogView [`2CC20D zzzCHMeQ09SheogorathView`])

講者條件模式：
- 大多數 INFO 要求在別名 `#0` (`Sheogorath`) 上滿足 `GetIsAliasRef == 1`。
- 開啟行 `2CC20F` 同時要求在任務 `2CAE30` 上滿足 `GetStage == 50`。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`2CC20F zzzCHMeQ09SheoB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2722) | `2CC210` | 無 | `GetStage == 50`; `GetIsAliasRef alias #0` | (Happy) 回應 1：「你早就知道。你本該知道那件事。如今一切都已超越遺忘。」 回應 2：「是這樣嗎？你再也無法知道自己是誰了。」 |
| [`2CC211 zzzCHMeQ09SheoB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2726) | `2CC212` | 無 | `GetIsAliasRef alias #0` | 提示：「我……我是……」 回應：(Surprise)「你明明被它灼燒，卻仍為了某事而死去？黑色的靈魂將永無安寧地燃燒下去？」 |
| [`2CC213 zzzCHMeQ09SheoB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2729) | `2CC214` | `Goodbye` | `GetIsAliasRef alias #0`; VMAD `CHMeq09_TIF__022CC214.Fragment_0` 結束時 | 提示：「即便如此……」 回應：回應 1 (Happy)「那是莫拉格·巴爾。遠離 Shezarr 的野獸。只是孱弱、只是粗鄙、只是醜陋。」 回應 2 (Surprise)「啊啊，黑色的靈魂如今抵達 Sithis 了。歡迎你，我們的新兄弟。」 |
| [`2CC215 zzzCHMeQ09SheoB01T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2733) | `2CC216` | `Goodbye` | `GetIsAliasRef alias #0`; VMAD `CHMeq09_TIF__022CC216.Fragment_0` 結束時 | 提示：「夠了……」 回應：回應 1 (Disgust)「甦醒的靈魂忘卻一切。所以一切才都在夢中。」 回應 2 (Sad)「永別了，被遺忘的兄弟。再次沉睡吧。」 |

分支結構（推論）：
- `2CC213` ("Nevertheless.......") 與 `2CC215` ("Enough.......") 是兩個最終玩家選擇；每個都是 `Goodbye` 並帶有獨特的 VMAD 片段。這些是引導至階段 **150 / 200** 的兩個完成點。
- 極性讀取（推論，待辦：透過片段反編譯確認）：`2CC213` 偏向「接受 / 成為 Sithis 的新兄弟」（莫拉格·巴爾 / 黑色靈魂接受行）；`2CC215` 偏向「拒絕 / 再次沉睡」（「永別了，被遺忘的兄弟。再次沉睡吧」）。此處**尚未**決定各個對應的確切階段編號。

翻譯備註：
- [`2CC20F`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2722) 中的 `beyond oblivion`：直譯「超越遺忘」；`Oblivion` 可能雙關（湮滅位面），待驗證。
- `Black soul` / `Pale beast of Sithis` / `our new brother`：Sithis/虛無母題的反覆用語，原文如此。

## 自定義對話分支：Tsun

分支：
- [`2E47D9 zzzCHMeQ09TsunB01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2779) (DialogView [`2E47D8 zzzCHMeQ09TsunView`])

講者條件模式：
- 所有 INFO 要求在別名 `#13` (`Tsun`) 上滿足 `GetIsAliasRef == 1`。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`2E47DA zzzCHMeQ09TsunB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2779) | `2E47DB` | `SayOnce` | `GetIsAliasRef alias #13` | (Neutral)「Stuhn……不，你不是……你是誰……？」 |
| (相同主題) | `2E47DE` | `Goodbye` | `GetIsAliasRef alias #13` | (Neutral)「退下吧，無魂者……」 |
| [`2E47DC zzzCHMeQ09TsunB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2783) | `2E47DD` | `Goodbye` | `GetIsAliasRef alias #13` | 提示：「我不知道……」 回應：(Neutral)「不過是你自己的殘影。無魂、無心。可悲……何等可悲……」 |

翻譯備註：
- `Stuhn`：Tsun 的前身/古諾德神名 (Stuhn)，原文如此；Tsun 認錯了對象。
- `Souless` (×2) 與 `Hearless`：原文拼錯（=`Soulless` / `Heartless`），保留語意翻為「無魂」「無心」。
- `Get thee hence`：古體英語「退下／離開此地」，原文如此。

## 相關紀錄 (Related Records)

根據 `find`/`scenediag`，這些由任務 `2CAE30` 擁有但未帶有對話；列出以供完整重建。

NPCs（別名填充）：
- [`2C8797 zzzCHSheogorathMemoryMad` - Sheogorath](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:823)
- [`2C8784 zzzCHLamaeMemoryMad` - Lamae](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:821)
- [`2BC374 zzzCHMemoryMolagBalMad` - Molag Bal](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:777)
- [`2DD387 zzzCHVigilantElderMemory` - Jacob](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:810)
- [`2E3483 zzzCHMemoryFox` - Shor](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:807)
- [`2DE6ED zzzCHMemoryTsun` - Tsun](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:808)

編排紀錄（無對話；來自 `find zzzCHMeQ09`）：
- 觸發器 (Activator) `2CFBCF zzzCHMeq09MovePlayerTRG`。
- 程序 (Packages) `2CE891 zzzCHMeQ09MolagBalDoNoting`、`2E3488 zzzCHMeq09FoxEscortPlayer`、`2E47D5 zzzCHMeq09JacobFindYou`、`2E6E97 zzzCHMeq09FoxAvoidPlayer`、`2E6EA8 zzzCHMeq09JacobSearchBody`。

書籍：
- 在 `books.md` 中或透過 `find` 均未發現 `zzzCHMeQ09…` 書籍。場景 `2E47CF` 中引用的「燒焦的日記」並非 MeQ09 擁有的 BOOK 紀錄；最接近的提取類比為 [`01C7F6 zzzAoMDiaryAltano`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:644) —— 僅為交叉參考，待驗證。

## 重建筆記 (Reconstruction Notes)

以原始資料為基礎：
- 此記憶為 [`2CAE30 zzzCHMemoryQuest09`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265)，優先級 `95`，目標為 [`Aah, fetus. Alas, Fetus`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:266)。
- 它包含 **七個** `SCEN` 紀錄（`2CC1E3`、`2CC1EA`、`2CC1EF`、`2CC1F0`、`2CC1F1`、`2E47CE`、`2E47D1`），透過 `BardTA`、`SheoTA` 與 `WGBardTA` 別名編排短促的獨白；`WGBardSc02` 額外執行了兩個 Jacob Packages 加上一個沉默的目光跟隨拍點。
- 它包含 **三個** 自定義對話分支：
  - Lamae 分支 (`2CC20A`)，1 個主題，別名 `#1`，`Fear`/`Goodbye` 退縮行。
  - 謝爾格拉分支 (`2CC20E`)，4 個主題，別名 `#0`，開啟行受階段 `GetStage==50` 限制；兩個帶有 VMAD 的最終選擇 = 150/200 完成點。
  - Tsun 分支 (`2E47D9`)，2 個主題，別名 `#13`，「Souless (無魂者)」拒絕行。
- 分支數：3 個對話分支；場景數：7 個；書籍數：0 個擁有的書籍。

開放驗證（待辦）：
- 反編譯 `CHMeq09_TIF__022CC214` 與 `CHMeq09_TIF__022CC216`，將階段 150 與 200 分配給兩個謝爾格拉最終選擇，並標註好/壞極性。
- 直接傾印 QUST 別名/目標物：目標物指向哪個別名，以及每個 `CompleteQuest` 階段授予什麼（物品/全域/世界變更）。
- 確認觸發條件：`2CFBCF zzzCHMeq09MovePlayerTRG` 觸發器以及 `JacobFindYou` / `FoxEscortPlayer` Packages 暗示這是一個引導式的進入過程，而非閱讀物品進入；請對照 `zzzCHMemoryGuide` 中心進行驗證。
- 一旦 MeQ08 被切片，解決與 MeQ08 的交叉連結（兩者皆涉及 Lamae）；將 MeQ09 紀錄保留於此，MeQ08 紀錄保留於該處。
