# 第四章記憶 08 - 無名詩人 (The Nameless Bard)

狀態：重構切片。基於來源、連結優先，非劇情摘要。

來源方針：
- 原始語句連結回抽取的來源文件，而非全文複製。
- 僅在需要解釋翻譯問題時才出現短小的來源片段。
- `SCEN` 編排來自 CLI 診斷，因為抽取的 `dialogue.md` 僅保留場景話題文本，而非場景階段/動作。
- 英文來源是從日文機器翻譯而來，語意經常不明；語意不明的詞彙將在來源欄位中保留原樣並標註 `Note: 待驗證`，絕不以純粹的猜測覆蓋。

## 任務紀錄 (Quest Record)

[`080E91 zzzCHMemoryQuest08 "The Nameless Bard"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)

CLI：
- `questdiag Vigilant.esm 0x080E91`
- `infodiag Vigilant.esm 0x080E91`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x080E91`
- EditorID: `zzzCHMemoryQuest08`
- 名稱: `The Nameless Bard`
- 標誌: `RunOnce`
- 優先級: `90`
- 類型: `Misc`
- 過濾器: `CH\`

來自 `questdiag` 的階段 (26 個)：

| 階段 | 標誌 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 10 | 無 | 空 (3 個日誌條目，皆為空) |
| 20 | 無 | 空 |
| 25 | 無 | 空 |
| 30 | 無 | 空 |
| 40 | 無 | 空 |
| 50 | 無 | 空 |
| 60 | 無 | 空 |
| 70 | 無 | 空 |
| 80 | 無 | 空 |
| 90 | CompleteQuest | 空 |
| 100 | 無 | 空 |
| 200 | 無 | 空 |
| 210 | 無 | 空 |
| 220 | 無 | 空 |
| 230 | CompleteQuest | 空 |
| 240 | 無 | 空 |
| 300 | 無 | 空 |
| 310 | 無 | 空 |
| 320 | 無 | 空 |
| 330 | 無 | 空 |
| 340 | 無 | 空 |
| 350 | CompleteQuest | 空 |
| 360 | 無 | 空 |
| 370 | CompleteQuest | 空 |
| 999 | ShutDownStage, CompleteQuest | 空 |

任務目標：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:195) | 「滾石躺在火裡，無人拾起。」 |

筆記：目標文本 `The rolling stones are in the fire and are not picked up` 是破碎的機器翻譯英文；同樣的「滾石 / 卵石」意象在場景話題 [`0821F3`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:985) (`Let it kicked a pebble rolling`) 中重複出現。待驗證。

目標對象：
- ESM 中有 3 個目標。
- 目標 1 有 1 個條件。
- 目標 2 有 1 個條件。
- 目標 3 有 0 個條件。
- 目前 CLI 輸出不會列印目標引用；若目標地點重要，則需要更深入的 QUST 目標轉儲。

## 別名 / 編排骨幹 (Alias / Staging Backbone)

以下六個 `SCEN` 紀錄共用相同的主機任務與 19 個別名列表。

主機任務：
- [`080E91 zzzCHMemoryQuest08`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)

來自 `scenediag` 的主機任務別名 (19 個)：

| 別名 | 名稱 | 填充 |
|---:|---|---|
| 0 | `StartMarker` | 強制引用 `07FA2D:Vigilant.esm` |
| 1 | `EndMarker` | 強制引用 `080E92:Vigilant.esm` |
| 2 | `Lamae` | 唯一演員 [`080E93 zzzCHLamaeMemory "Lamae Beolfag"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1073) |
| 3 | `Facis` | 唯一演員 [`080E98 zzzCHLamaeFollowerMemory "Facis"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1066) |
| 4 | `MolagTE` | 唯一演員 [`080E96 zzzCHMolagBalInMemoryTE`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1067) |
| 5 | `TA01` | 強制引用 `0821E9:Vigilant.esm` |
| 6 | `TEMarker` | 強制引用 `080E95:Vigilant.esm` |
| 7 | `TA02` | 強制引用 `08220E:Vigilant.esm` |
| 8 | `MolagBE` | 唯一演員 [`0875EF zzzCHMolagBalInMemoryBE`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1062) |
| 9 | `BEMarker` | 強制引用 `0875EB:Vigilant.esm` |
| 10 | `TA03` | 強制引用 `0875F2:Vigilant.esm` |
| 11 | `TA04` | 強制引用 `08B5A8:Vigilant.esm` |
| 12 | `TA05` | 強制引用 `08B5AA:Vigilant.esm` |
| 13 | `WEMarker` | 強制引用 `088BBC:Vigilant.esm` |
| 14 | `Volar` | 唯一演員 [`088BC8 zzzCHDeathBringerMemory`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1059) |
| 15 | `Laza` | 唯一演員 [`2E47E5 zzzCHMemoryLaza "Laza"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:806) |
| 16 | `GuideBard` | 強制引用 `42E0B7:Vigilant.esm` |
| 17 | `GuideStatue` | 強制引用 `42F43E:Vigilant.esm` |
| 18 | `GuideTower` | 強制引用 `4307C5:Vigilant.esm` |

本記憶的主體：
- **無名詩人** = 玩家控制的記憶演員，玩家透過莫拉格對話分支選擇其名字（見下方的自定義對話分支）。別名 `MolagTE`/`MolagBE` 是詩人交談的兩個莫拉格·巴爾幻影；玩家向每位幻影報上自己的名字（或拒絕報名）。
- **拉邁 (Lamae)** (別名 `#2`, [`080E93`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1073)) 是詩人對其歌唱的少女。拉邁也出現在 MeQ09 `From Beyond` 中；此處僅包含 MeQ08 擁有的 `080E93 zzzCHLamaeMemory` 紀錄（需另外交叉連結 MeQ09 的拉邁）。

推論：
- `TA01`–`TA05` (別名 `#5`,`#7`,`#10`,`#11`,`#12`) 是在五個 `*ESc` 場景中傳遞詩人敘事語句的強制引用 TA 演員（每個場景各使用一位 TA 演員）。
- `TEMarker`/`BEMarker`/`WEMarker` (別名 `#6`,`#9`,`#13`) 是**真**結局 (True-Ending)、**壞**結局 (Bad-Ending?) 與**西**結局 (West-Ending?) 場景塊的編排標記（根據命名與哪些場景引用哪些別名推斷）。待驗證：每個標記的確切好/壞極性 —— 見下方的階段結果映射。
- `Guide*` 別名 (`#16`–`#18`) 連結回 `zzzCHMemoryGuide` 樞紐 (`42E0B1`)，與 MeQ07 相同。

## 場景紀錄 (Scene Records)

場景紀錄在 `game-data` 中並非完整紀錄；文本行連結至 `dialogue.md`，而階段/動作則來自 `scenediag`。

### 080EA5 zzzCHMeQ08Sc01 — 拉邁與 Facis 開場

CLI：
- `scenediag Vigilant.esm 0x080EA5`

編排：
- 主機任務：[`080E91 zzzCHMemoryQuest08`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)
- 標誌：無
- 演員：別名 `#3` (`Facis`, `DeathEnd NoPlayerActivation`) 與別名 `#2` (`Lamae`, `DeathEnd NoPlayerActivation`)
- 階段：6 個，每個皆為 0 開始 / 1 完成條件。
- 動作：共 10 個 —— Package 移動動作在 `#3` 與 `#2` 之間交錯，並穿插三個對話 (Dialog) 動作：
  - 索引 3: `Dialog` 演員 `#3` (Facis), 階段 1, 話題 [`080EA7`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:964), 對望演員 `#2`, 情緒 `Neutral`。
  - 索引 4: `Dialog` 演員 `#2` (Lamae), 階段 2, 話題 [`080EA9`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:967), 對望演員 `#3`, 情緒 `Neutral`。
  - 索引 8: `Dialog` 演員 `#2` (Lamae), 階段 4, 話題 [`080EAE`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:970), `FaceTarget HeadtrackPlayer`, 情緒 `Neutral`。

翻譯：
- [`080EA7` / INFO `080EA8`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:964) (Facis, Surprise): 「夫人，原來您躲在這裡。Shorl 大人在等您。」
  - 筆記：`Lord Shorl` 為專有名詞，拼寫待驗證。`Did you fold here` 機翻不清，依語境譯為「躲在這裡」。待驗證。
- [`080EA9` / INFO `080EAA`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:967) (Lamae, Happy): 「好，我現在就過去。」
- [`080EAE` / INFO `080EAF`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:970) (Lamae, Happy): 「再會。」

### 0821E7 zzzCHMeQ08Sc02 — 綁架敘事 (TA01)

CLI：
- `scenediag Vigilant.esm 0x0821E7`

編排：
- 主機任務：[`080E91`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)
- 標誌：無
- 演員：別名 `#5` (`TA01`), 行為 `DeathEnd, CombatEnd, DialoguePause`
- 階段：2 個，每個皆為 0 開始 / 1 完成條件。
- 動作：
  - 索引 1: `Dialog` 演員 `#5`, 階段 0, 話題 [`0821EA`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:973), 情緒 `Neutral`。
  - 索引 2: `Dialog` 演員 `#5`, 階段 1, 話題 [`0821EC`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:976), 情緒 `Neutral`。

翻譯：
- [`0821EA` / INFO `0821EB`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:973): 「不久之後，她的尖叫聲在天霜的峽灣裡迴盪。我記得我抱著昏迷的她，朝村子走去。」
- [`0821EC` / INFO `0821ED`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:976): 「然而她再也沒有醒來；一道光輝就此自 Nirn 消失。」
  - 筆記：`shine one was was lost from Nirn`（重複 `was`）機翻破碎，依語境譯為「一道光輝自 Nirn 消失」。待驗證。

### 0821EE zzzCHMeQ08TESc — 真結局(?) 敘事 (TA02)

CLI：
- `scenediag Vigilant.esm 0x0821EE`

編排：
- 主機任務：[`080E91`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)
- 標誌：無
- 演員：別名 `#5` (`TA01`) 與別名 `#7` (`TA02`), 皆為 `DeathEnd, CombatEnd, DialoguePause`
- 階段：4 個，每個皆為 0 開始 / 1 完成條件。
- 動作：
  - 索引 4: `Timer` 演員 `#7`, 階段 0, `0.1` 秒。
  - 索引 1: `Dialog` 演員 `#7`, 階段 1, 話題 [`0821EF`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:979), 情緒 `Neutral`。
  - 索引 2: `Dialog` 演員 `#7`, 階段 2, 話題 [`0821F1`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:982), 情緒 `Neutral`。
  - 索引 3: `Dialog` 演員 `#7`, 階段 3, 話題 [`0821F3`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:985), 情緒 `Neutral`。

翻譯：
- [`0821EF` / INFO `0821F0`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:979): 「很久以前，Eldergleam 還年輕，尚未被深埋於黑暗大地之底。那時的世界滿是魔法、奇異與危險。」
- [`0821F1` / INFO `0821F2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:982): 「即便如此，我仍不向殘酷的命運屈服。因為我知道，這份苦難終有一天也會像可憐的幻影般消散。」
  - 筆記：`cruel fate I` 的 `I` 為機翻贅字。待驗證。
- [`0821F3` / INFO `0821F4`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:985): 「即使流了血、犯下種種過錯，人也能得到救贖。就讓它像被踢動的石子一樣滾落，伴著餘下的歌聲。」
  - 筆記：此句的「滾石／踢動的石子」對應任務目標 [`The rolling stones … not picked up`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:195)。

### 0875F1 zzzCHMeQ08BESc — 壞結局(?) 敘事 (TA03)

CLI：
- `scenediag Vigilant.esm 0x0875F1`

編排：
- 主機任務：[`080E91`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)
- 標誌：無
- 演員：別名 `#10` (`TA03`), `DeathEnd`
- 階段：4 個，每個皆為 0 開始 / 1 完成條件。
- 動作：
  - 索引 4: `Timer` 演員 `#10`, 階段 0, `0.1` 秒。
  - 索引 1: `Dialog` 演員 `#10`, 階段 1, 話題 [`0875F3`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1006), 情緒 `Neutral`。
  - 索引 2: `Dialog` 演員 `#10`, 階段 2, 話題 [`0875F5`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1009), 情情緒 `Neutral`。
  - 索引 3: `Dialog` 演員 `#10`, 階段 3, 話題 [`0875F7`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1012), 情緒 `Neutral`。

翻譯：
- [`0875F3` / INFO `0875F4`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1006): 「很久以前，Eldergleam 還年輕，尚未被深埋於黑暗大地之底。那時的世界滿是魔法、奇異與危險。」（與 `0821EF` 同一段開場，重複使用。）
- [`0875F5` / INFO `0875F6`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1009): 「那一天，我在她已徹底改變的額頭上滴下一滴血。我只盼她能永遠安息，卻反而引來了不死的詛咒。」
  - 筆記：`just baiting the curse of immortality` 機翻不清，依語境譯為「反而引來不死的詛咒」。待驗證。
- [`0875F7` / INFO `0875F8`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1012): 「無法被救贖的血與淚……罪永不得赦免。石頭染上熱意，緩緩烤炙著我的身體。」
  - 筆記：`and Charles` 為機翻雜訊（疑似日文助詞誤譯），已略去。待驗證。

### 08B5AD zzzCHMeQ08WESc01 — 西結局(?) 敘事 (TA04)

CLI：
- `scenediag Vigilant.esm 0x08B5AD`

編排：
- 主機任務：[`080E91`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)
- 標誌：無
- 演員：別名 `#11` (`TA04`), `DeathEnd, CombatEnd, DialoguePause`
- 階段：1 個，0 開始 / 1 完成條件。
- 動作：
  - 索引 1: `Dialog` 演員 `#11`, 階段 0, 話題 [`08B5AE`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1030), 情緒 `Neutral`。

翻譯：
- [`08B5AE` / INFO `08B5AF`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1030): 「我是懦夫，只活在不斷重複的夢裡。從一開始，我就注定被自己親手養大的黑暗吞噬……」

### 08B5B6 zzzCHMeQ08WESc02 — 莫拉格·巴爾自我報名 (TA05)

CLI：
- `scenediag Vigilant.esm 0x08B5B6`

編排：
- 主機任務：[`080E91`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)
- 標誌：無
- 演員：別名 `#12` (`TA05`), `DeathEnd, CombatEnd, DialoguePause`
- 階段：2 個，每個皆為 0 開始 / 1 完成條件。
- 動作：
  - 索引 1: `Dialog` 演員 `#12`, 階段 0, 話題 [`08B5B7`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1039), 情緒 `Neutral`。
  - 索引 2: `Dialog` 演員 `#12`, 階段 1, 話題 [`08B5B9`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1042), 情緒 `Neutral`。

翻譯：
- [`08B5B7` / INFO `08B5B8`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1039): 「我的名字是……Molag Bal。奴役與屈辱之王，靈魂吞噬者，詛咒諸神之世界的存在。」
- [`08B5B9` / INFO `08B5BA`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1042): 「一切都會重演。認識黑暗，打破黑暗，超越死亡。」

## 自定義對話分支：拉邁 (zzzCHMeQ08LamaeB01)

分支：
- `080E9C:Vigilant.esm` (`zzzCHMeQ08LamaeB01`), 視圖 `080E9B` (`zzzCHMeQ08LamaeView`)

說話者條件模式：
- 所有 INFO 皆要求別名 `#2` (`Lamae`) 的 `GetIsAliasRef == 1`。
- 開場白亦要求任務 `080E91` 的 `GetStage == 10`。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`080E9D zzzCHMeQ08LamaeB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:952) | `080E9E` | 無 | `GetStage == 10`; 別名 `#2` 的 `GetIsAliasRef` | (Happy) 「能不能……再讓我聽聽那首歌剩下的部分？」 筆記：`It is not you please let the rest of that song?` 機翻破碎。待驗證。 |
| [`080E9F zzzCHMeQ08LamaeB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:955) | `080EA0` | 無 | 別名 `#2` 的 `GetIsAliasRef` | 提示語：「故事的後續？」 回應：(Happy) 「對，後續。你想想 —— 一個殘酷又悲傷的故事，會變成怎樣一個了不起、滿溢幸福的結局呢？」 筆記：原文 `What you I'm a story cruel sad story…` 文法破碎。待驗證。 |
| [`080EA1 zzzCHMeQ08LamaeB01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:958) | `080EA2` | `Goodbye` | 別名 `#2` 的 `GetIsAliasRef`; 結束時 VMAD `CHMeq08_TIF__02080EA2.Fragment_0` | 提示語：「沒有什麼後續了。他們就那樣荒謬地被殺死。」 回應：(Sad) 「這樣啊……我有點失望。」 |
| [`080EA3 zzzCHMeQ08LamaeB01T04`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:961) | `080EA4` | `Goodbye` | 別名 `#2` 的 `GetIsAliasRef`; 結束時 VMAD `CHMeq08_TIF__02080EA4.Fragment_0` | 提示語：「下次見面前，我會把剩下的編好。」 回應：(Happy) 「我會好好想著的。說好了。」 |

分支筆記（推論）：兩個玩家選擇 `080EA1`（無續集 / 讓她在故事中死去）與 `080EA3`（我會編好這首歌）是第一個業障分歧點；兩者皆帶有 `CHMeq08_TIF__*` 結束片段，可能用於設定階段。待驗證：透過 Papyrus 片段確認。

## 自定義對話分支：莫拉格 (TE) — 拒絕 (zzzCHMeQ08MolagTB01)

分支：
- `0821FA:Vigilant.esm` (`zzzCHMeQ08MolagTB01`), 視圖 `0821F9` (`zzzCHMeQ08MolagTView`)

說話者條件模式：
- INFO 要求別名 `#4` (`MolagTE`) 的 `GetIsAliasRef == 1`。
- 開場白要求 `GetStage == 60`。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0821FB zzzCHMeQ08MolagTB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:988) | `0821FC` | `WalkAway` | `GetStage == 60`; 別名 `#4` 的 `GetIsAliasRef` | (Neutral) 「我無法理解。你為何拒絕……只要你願望，就能讓那女孩復活，不是嗎？」 |
| [`0821FD zzzCHMeQ08MolagTB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:991) | `0821FE` | `Goodbye` | 別名 `#4` 的 `GetIsAliasRef`; 結束時 VMAD `CHMeq08_TIF__020821FE.Fragment_0` | 提示語：「你永遠不會懂。」 回應：(Neutral) 「…………」 |

## 自定義對話分支：莫拉格 (TE) — 報名 (zzzCHMeQ08MolagTB02)

分支：
- `0821FF:Vigilant.esm` (`zzzCHMeQ08MolagTB02`)

說話者條件模式：
- INFO 要求別名 `#4` (`MolagTE`) 的 `GetIsAliasRef == 1`。
- 開場白要求 `GetStage == 70`。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`082200 zzzCHMeQ08MolagTB02T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:994) | `082201` | 無 | `GetStage == 70`; 別名 `#4` 的 `GetIsAliasRef` | (Neutral) 「等等……我還沒聽到你的名字……」 |
| [`082202 zzzCHMeQ08MolagTB02T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:997) | `082203` | `Goodbye, SayOnce` | 別名 `#4` 的 `GetIsAliasRef`; VMAD `…02082203.Fragment_0` | 提示語：「<Alias=Player>。是她給了我這名字。」 回應：(Neutral) 「好名字……我會記在靈魂裡。」 |
| [`082204 zzzCHMeQ08MolagTB02T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1000) | `082205` | `Goodbye, SayOnce` | 別名 `#4` 的 `GetIsAliasRef`; VMAD `…02082205.Fragment_0` | 提示語：「Stendll。我是哈芬納的 Stendll。」 回應：(Neutral) 「奇怪的名字……我會記在靈魂裡。」 筆記：`Stendll` / `Strange neme` 拼寫待驗證。 |
| [`082206 zzzCHMeQ08MolagTB02T04`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1003) | `082207` | `Goodbye, SayOnce` | 別名 `#4` 的 `GetIsAliasRef`; VMAD `…02082207.Fragment_0` | 提示語：「我已捨棄了我的名字。」 回應：(Neutral) 「……真是可悲。」 |

推論：此處三個互斥的 `SayOnce` 選擇是**報名分歧點** —— 接受拉邁給予的名字 / 給予自己的名字 (`Stendll`) / 捨棄名字 —— 每個選擇皆透過不同片段結束 TE 分支。這是賦予任務標題「無名詩人」意義的玩家身分選擇。待驗證：透過片段確認極性。

## 自定義對話分支：莫拉格 (BE) (zzzCHMeQ08MolagBB01)

分支：
- `0875FA:Vigilant.esm` (`zzzCHMeQ08MolagBB01`), 視圖 `0875F9` (`zzzCHMeQ08MolagBView`)

說話者條件模式：
- INFO 要求別名 `#8` (`MolagBE`) 的 `GetIsAliasRef == 1`。
- 開場白要求 `GetStage == 210`。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0875FB zzzCHMeQ08MolagBB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1015) | `0875FC` | `InvisibleContinue` | `GetStage == 210`; 別名 `#8` 的 `GetIsAliasRef`; VMAD `…020875FC.Fragment_0` | (Neutral) 「幹得好。這是給你的獎賞。」 |
| [`0875FD zzzCHMeQ08BB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1018) | `0875FE` | `WalkAway` | 別名 `#8` 的 `GetIsAliasRef` | (Neutral) 「那麼，接下來你打算怎麼做？」 筆記：EditorID 為 `zzzCHMeQ08BB01T02`（缺 `Molag`），原文如此。 |
| [`0875FF zzzCHMeQ08MolagBB01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1021) | `087600` | `Goodbye, SayOnce` | 別名 `#8` 的 `GetIsAliasRef`; VMAD `…02087600.Fragment_0` | 提示語：「捨棄名字，往西方去。這裡只剩悲傷。」 回應：(Neutral) 「是嗎。我想無論你去哪都一樣。沒人會阻止你想做的事。」 |
| [`087601 zzzCHMeQ08MolagBB01T04`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1024) | `087602` | `Goodbye, SayOnce` | 別名 `#8` 的 `GetIsAliasRef`; VMAD `…02087602.Fragment_0` | 提示語：「<Alias=Player>。記住，這是擊敗你的人之名。」 回應：(Neutral) 「真有趣。你可得好好期待那一刻。」 |
| [`087603 zzzCHMeQ08MolagBB01T05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1027) | `087604` | `Goodbye, SayOnce` | 別名 `#8` 的 `GetIsAliasRef`; VMAD `…02087604.Fragment_0` | 提示語：「Stendll。記住，這是獵殺魔族者之名。」 回應：(Neutral) 「非常有趣。雖是場拚死的掙扎，但很好。」 |

推論：BE 分支在較晚的階段區段 (210+) 重複了報名分歧 —— 捨棄名字 / 宣稱自己是「擊敗你的人」 / 命名為獵殺魔族的 `Stendll`。待驗證：哪一項導向 230 或 350/370 的完成路徑。

## 自定義對話分支：Volar (zzzCHMeQ08VolarB01)

分支：
- `08B5B1:Vigilant.esm` (`zzzCHMeQ08VolarB01`), 視圖 `08B5B0` (`zzzCHMeQ08VolarView`)

說話者條件模式：
- INFO 要求別名 `#14` (`Volar`) 的 `GetIsAliasRef == 1`。
- 開場白要求 `GetStage == 310`。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`08B5B2 zzzCHMeQ08VolarB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1033) | `08B5B3` | `WalkAway` | `GetStage == 310`; 別名 `#14` 的 `GetIsAliasRef` | (Puzzled) 原文 `The One is so come here soon, but if the squid you like?` —— 機翻嚴重破碎，無法可靠還原。待驗證。 |
| [`08B5B4 zzzCHMeQ08VolarB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1036) | `08B5B5` | `Goodbye, SayOnce` | 別名 `#14` 的 `GetIsAliasRef`; VMAD `…0208B5B5.Fragment_0` | 提示語：「玩得開心點，Volar。」 回應：(Happy) 原文 `Let's show to meet the expectations definitely stuck!` —— 機翻破碎。待驗證。 |

Volar 話題名稱列表處理：
- 任務提示曾警告 `zzzCHMeQ08VolarB01T02` 列舉了其他記憶主體 (Drozel, Hasaama, Johan, Martha)。**但 `infodiag` 並未證實此點**：實際擁有的 `VolarB01T02` 回應僅為一行破碎的語句（見上方），無名稱列表。Drozel/Hasaama/Martha/Johan 的名稱存在於**非擁有的**獨立支線話題中 (`zzzCHsqMartha*` [dialogue.md:1356+], `zzzCHsqDrozel*` [dialogue.md:1380+]), 確認不屬於 `080E91`。因此將其**排除**在此切片外。此處的 Volar 分支是與**索魂者 Volar** ([`088BC8 zzzCHDeathBringerMemory`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1059)) 的對質，而非背景設定列舉。

## 自定義對話分支：Laza (zzzCHMeQ08LazaB01)

分支：
- `2E47EA:Vigilant.esm` (`zzzCHMeQ08LazaB01`), 視圖 `2E47E9` (`zzzCHMeQ08LazaView`)

說話者條件模式：
- INFO 要求別名 `#15` (`Laza`) 的 `GetIsAliasRef == 1`。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`2E47EB zzzCHMeQ08LazaB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2786) | `2E47EC` (SayOnce, WalkAway) / `2E47EF` (Goodbye) | — | 別名 `#15` 的 `GetIsAliasRef` | INFO0 (Sad) 「你怎能這麼做……把他們還來……把我的家人……我的姐妹還來……」; INFO1 (Disgust) 「啊啊，Kyne……為什麼……為什麼你不救我們……」 |
| [`2E47ED zzzCHMeQ08LazaB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2790) | `2E47EE` | `Goodbye` | 別名 `#15` 的 `GetIsAliasRef` | 提示語：「死者不會復生。」 回應：(Anger) 「該死的，Sithis 之怪物……該死……該死……該死……」 |

筆記：`Laza` ([`2E47E5 zzzCHMemoryLaza`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:806)) —— 提及 Sithis/Kyne 表示村民正因死去的家人向詩人質難；該分支在轉儲中無 `GetStage` 開場門檻，其階段關聯尚未釘定。待驗證。

## CompleteQuest 階段 → 結果映射

五個階段帶有 `CompleteQuest` 標誌：**90 / 230 / 350 / 370 / 999**。基於 `questdiag` 結合分支開場白 `GetStage` 條件推斷出的來源型態；極性在可判定處標註，否則標為 TODO。

| 階段 | 標誌 | 階段區段 | 關聯分支 (依 `GetStage` 開場) | 解讀 | 極性 |
|---:|---|---|---|---|---|
| 90 | CompleteQuest | 第一區段 (0–90) | Lamae B01 開場 `==10`; Molag TE 拒絕 `==60`; TE 報名 `==70` | **拉邁 / 真結局 (TE)** 路徑：詩人拒絕透過莫拉格交易使其復活，選擇如何報名，並結束記憶。為正式結局。 | **可能為「好/仁慈」** (拒絕莫拉格的交易) —— TODO 待片段確認 |
| 230 | CompleteQuest | 第二區段 (100–230) | Molag BE 開場 `==210` | **壞結局 (BE)** 路徑：詩人接受莫拉格的「獎賞」，取下將要擊敗/獵殺對方的名號。為正式結局。 | **可能為「壞/墮落」** (接受交易) —— TODO 待確認 |
| 350 | CompleteQuest | 第三區段 (300–350) | Volar 開場 `==310` | **西結局 / Volar (WE)** 路徑區塊 (`WESc01/02`, 莫拉格·巴爾自我報名場景)。為正式結局。 | TODO —— Volar 文本太破碎無法標註 |
| 370 | CompleteQuest | 第三區段尾 (360–370) | 在 360–370 無自有分支開場 | 緊接在 350 之後的備選完成；可能為 **WE 區塊的第二種結果** (或 Laza 對質的解決方案)。正式結局或變體 —— 尚未釘定。 | TODO |
| 999 | ShutDownStage + CompleteQuest | 關閉階段 | — | **僅限系統關閉**，非敘事結局。具有 `ShutDownStage` 標誌；鏡像了 MeQ07 的 `255/999` 關閉模式。在觸發任一正式結局後關閉任務。 | 無 (關閉) |

總結（基於來源的型態，部分極性為推論）：
- **正式結局：90, 230, 350, 370** (共四個)。**999 = 引擎關閉**，非結果。
- 三個階段區段 (0–90 TE, 100–230 BE, 300–370 WE) 與詩人遇見的三位莫拉格幻影別名一一對應：`MolagTE` (#4)、`MolagBE` (#8) 與 `Volar`/`Molag Bal`-自我報名 WE 場景 (#14)。這是結構骨幹；**350 與 370 的好壞極性無法單從 `questdiag` 結合開場白判定**，在 `CHMeq08_TIF__*` 階段片段釘定前標為 TODO。

## 相關紀錄 (Related Records)

根據 `infodiag` 並非一定由 `080E91` 擁有，但為拉邁/詩人重構背景：

NPCs：
- [`080E93 zzzCHLamaeMemory "Lamae Beolfag"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1073) — 記憶中的拉邁 (別名 `#2`, 擁有)
- [`085FCA zzzCHLamaeMemoryDead`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1063) — 死亡狀態的拉邁 (不在別名轉儲中；需交叉連結)
- [`2C8784 zzzCHLamaeMemoryMad`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:821), [`2C8785 zzzCHLamaeFollowerMemoryDead "Facis"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:822) — 備選拉邁/Facis 狀態
- [`037468 zzzBMLamaeBal "Lamae Bal"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:891) — Bloodmoon/遊戲本體的拉邁·巴爾 (跨 mod 連結；非 MeQ08)
- [`03D78A zzzCHBossDeathBringer "Deathbringer Volar"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:968) — 戰鬥狀態 Volar (記憶別名 `#14` 為 `088BC8 zzzCHDeathBringerMemory`)

書籍（相關，但根據 `infodiag` 不由 `080E91` 擁有）：
- [`0DB22D zzzCHBookBloodOfLamae "Blood of Lamae"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:2183) — 拉邁血系吸血鬼背景書（提及 `Ramae` = 拼錯的 Lamae）；作為場景 `0875F5` 中不死詛咒情節的交叉連結。

## 重構筆記 (Reconstruction Notes)

基於來源：
- 本記憶為 [`080E91 zzzCHMemoryQuest08`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:194)，任務目標為 [`The rolling stones are in the fire and are not picked up`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:195)。
- 包含**六個 `SCEN` 紀錄** (`080EA5`, `0821E7`, `0821EE`, `0875F1`, `08B5AD`, `08B5B6`)，編排了拉邁開場以及三個結局塊敘事 (TE/BE/WE) 透過別名 `TA01`–`TA05` 與 `Lamae`/`Facis`。
- 包含**六個自定義對話分支**：拉邁 (`080E9C`)、莫拉格-TE 拒絕 (`0821FA`)、莫拉格-TE 報名 (`0821FF`)、莫拉格-BE (`0875FA`)、Volar (`08B5B1`)、Laza (`2E47EA`)。
- **擁有的書籍為 0**；`Blood of Lamae` 僅為相關背景。
- 階段限制的分支開場白：Lamae `==10`, MolagTE-拒絕 `==60`, MolagTE-報名 `==70`, MolagBE `==210`, Volar `==310`。
- VMAD `CHMeq08_TIF__*` 片段位於每個玩家選擇的 `Goodbye`/`SayOnce` INFO 上，因此選擇將推進狀態/路由結果；確切的 Papyrus 行為在此未解碼。

語意不明 / 標註詞彙（機器從日文翻譯，保留原文，待驗證）：
- `Lord Shorl` (`080EA8`) —— 專有名詞，拼寫未驗證。
- `shine one was was lost from Nirn` (`0821ED`) —— 重複的 `was`, 語法破碎。
- `and Charles` (`0875F8`) —— 可能為機翻的日文助詞/雜訊。
- `Stendll` / `Strange neme` (`082204`/`082205`, `087603`) —— 玩家選擇的名字；拼寫未驗證。
- `The One is so come here soon, but if the squid you like?` (`08B5B3`) 與 `Let's show to meet the expectations definitely stuck!` (`08B5B5`) —— Volar 語句，過於破碎無法可靠還原。
- `Ramae` in `Blood of Lamae` book = 拼錯的 `Lamae`。

開放驗證：
- 反編譯 / 檢查 `CHMeq08_TIF__02080EA2`、`…02080EA4`、`…020821FE`、`…02082203/05/07`、`…020875FC/087600/02/04`、`…0208B5B5` 以釘定哪個片段設定階段 90、230、350 或 370 —— 這能解決 350/370 極性 TODO；
- 轉儲 QUST 任務目標對象 (`StartMarker`/`EndMarker`/`TE/BE/WEMarker`) 以將每個結局塊映射到世界空間位置；
- 釘定 `Laza` 分支的階段關聯（目前轉儲中無 `GetStage` 開場白）；
- 當 MeQ09 `From Beyond` 切片構建時交叉連結其拉邁紀錄（拉邁跨越 MeQ08/MeQ09）。
