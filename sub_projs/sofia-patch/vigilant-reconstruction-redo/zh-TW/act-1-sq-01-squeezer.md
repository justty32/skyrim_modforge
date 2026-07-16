# 第一章支線任務 01 — 壓榨者 (Act 1 Side Quest 01 - Squeezer)

狀態：第一個重做切片。基於來源 (Source-grounded)，連結優先，無 Gemini 幻覺。

來源方針：
- 原始文本行連結回提取的來源文件，而非完整複製。
- 僅在需要解釋語境或條件時才出現短小的來源片段。
- 無檢測到 `SCEN` (場景) 編排；為對話驅動型任務，具有線性階段進度。

## 任務記錄 (Quest Record)

[`005CE3 zzzAoMMq01 "Squeezer"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:288)

CLI 指令：
- `questdiag Vigilant.esm 0x005CE3`
- `infodiag Vigilant.esm 0x005CE3`

ESM 路徑：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x005CE3`
- EditorID: `zzzAoMMq01`
- 名稱: `Squeezer` (壓榨者)
- 標誌: `RunOnce`
- 優先級: `90`
- 類型: `SideQuest`
- 過濾器: `AoM\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 標誌 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 10 | 無 | 空 |
| 11 | 無 | 空 |
| 15 | 無 | 空 |
| 20 | 無 | 空 |
| 30 | 無 | 空 |
| 40 | 無 | 空 |
| 50 | CompleteQuest | 空 |
| 255 | ShutDownStage | 空 |
| 9999 | CompleteQuest | 空 |

目標 (Objectives)：

| 索引 | 來源 | 文本 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:289) | 與阿爾塔諾交談 |
| 10 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:290) | 跟隨阿爾塔諾或在死亡聖所與阿爾塔諾會合 |
| 15 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:291) | 與阿爾塔諾交談 |
| 20 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:292) | 搜尋吸血鬼 |
| 30 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:293) | 擊敗吸血鬼 |
| 40 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:294) | 向阿爾塔諾報告 |

目標對象 (Objective targets)：
- 目標 0: 1 個對象，0 條件。
- 目標 10: 2 個對象，各 0 條件。
- 目標 15: 1 個對象，0 條件。
- 目標 20: 0 個對象。
- 目標 30: 1 個對象，0 條件。
- 目標 40: 1 個對象，0 條件。
- 目前的 CLI 輸出不列印目標單元/引用細節；如果地點定位重要，則需要更深層的 QUST 對象轉儲。

## 別名 / 編排骨幹 (Alias / Staging Backbone)

`infodiag` 未偵測到自定義 `SCEN` 記錄。階段進度透過對話條件顯示為線性。

主機任務 (Host quest)：
- `005CE3 zzzAoMMq01` "Squeezer"

來自 `infodiag` 的對話別名：
- 別名 `#0`：預計為 `阿爾塔諾` (主任務提供者)。
- 別名 `#1`：預計為一名妓女 NPC (吸血鬼陰謀中的目標/嫌疑人)。

(推論：別名角色從對話條件 `GetIsAliasRef` 索引 0 和 1 推論而來；CLI 無提供顯式別名轉儲)

## 自定義對話分支 (Custom Dialogue Branches)

### 分支 1：任務開端 — 「我能幫你嗎？」

話題 (TOPIC) `0x006258 zzAoMMq01B1Mission1`

條件模式：
- `GetStage < 10`：在玩家進入初始對話之前觸發。
- `GetIsAliasRef alias #0` (阿爾塔諾)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x006258 zzAoMMq01B1Mission1` | `0x006259` | 無 | `GetStage < 10`; `GetIsAliasRef alias #0` | 提示：[`「我能幫你嗎？」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:14) 回覆 (中性)：[`「雪漫城的阿凱祭司有個請求。有吸血鬼出現了。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:15) 回覆 (中性)：[`「你能協助我嗎？立刻準備好出發，吸血鬼不會等我們。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:16) |

VMAD 片段：
- `AoM01_TIF__01006259` (觸發 `OnEnd` 片段)
- (推論：片段可能設置階段 10+ 以推進任務)

### 分支 2：調查階段 — 「告訴我關於這次事件的事」

話題 `0x00625B zzAoMMq01B2AboutCrime`

條件模式：
- `GetStage == 15`：在調查階段觸發。
- `GetInCell 0x0165AA` (Skyrim.esm，推測為雪漫城的犯罪現場)。
- `GetIsAliasRef alias #0` (阿爾塔諾)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x00625B zzAoMMq01B2AboutCrime` | `0x00625C` | 無 | `GetStage == 15`; `GetInCell 0x0165AA`; `GetIsAliasRef alias #0` | 提示：[`「告訴我關於這次事件的事。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:18) 回覆 (開心)：[`「每個受害者的血都被抽乾了。這是吸血鬼幹的，新手吸血鬼。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:19) 回覆 (中性)：[`「我會試著在這裡多研究一下文件。你在鎮上尋找可疑的人。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:20) |

VMAD 片段：
- `AoM01_TIF__0100625C` (觸發 `OnEnd` 片段)
- (推論：片段將階段推進到 20)

翻譯筆記：
- "squeeze all blood" 是慣用語；英文文本不合語法，但意圖是「抽乾所有的血」或「吸乾所有的血」。

### 分支 3：受害者分析 — 「告訴我關於受害者的事」

話題 `0x00625E zzAoMMq01B3AboutVictims`

條件模式：
- `GetStage == 20`：在受害者分析階段觸發。
- `GetIsAliasRef alias #0` (阿爾塔諾)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x00625E zzAoMMq01B3AboutVictims` | `0x00625F` | 無 | `GetStage == 20`; `GetIsAliasRef alias #0` | 提示：[`「告訴我關於受害者的事」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:22) 回覆 (悲傷)：[`「受害者之間在魔法上沒有共同點……嗯……共同點是男性。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:23) |

### 分支 4：權威問題 — 「我們不需要領主的許可嗎？」

話題 `0x006261 zzAoMMq01B4AboutAuthority`

條件模式：
- `GetStage == 20`：在受害者分析階段觸發。
- `GetIsAliasRef alias #0` (阿爾塔諾)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x006261 zzAoMMq01B4AboutAuthority` | `0x006262` | 無 | `GetStage == 20`; `GetIsAliasRef alias #0` | 提示：[`「我們不需要領主的許可嗎？」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:25) 回覆 (開心)：[`「我們的活動在天際省是被接受的。其中一個原因是內戰導致人手不足。總之，我們現在很受歡迎。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:26) |

翻譯筆記：
- "Cuvil War" 被推測為誤寫或在地化的名稱；可能指天際省的「內戰」(Civil War)。

### 分支 5：吸血鬼類型問題 — 「你說新手吸血鬼……為什麼？」

話題 `0x006264 zzAoMMq01B5WhyNovice`

條件模式：
- `GetStage == 20`：在受害者分析階段觸發。
- `GetIsAliasRef alias #0` (阿爾塔諾)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x006264 zzAoMMq01B5WhyNovice` | `0x006265` | 無 | `GetStage == 20`; `GetIsAliasRef alias #0` | 提示：[`「你說新手吸血鬼……為什麼？」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:28) 回覆 (開心)：[`「每天都有受害者被發現。大多數行為招搖的吸血鬼都是新手。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:29) |

### 分支 6：嫌疑人分支 — 妓女遭遇

話題 `0x006267 zzAoMMq01B6Whore`

條件模式：
- `GetStage == 20`：在調查期間觸發。
- `GetGlobalValue 0x000038 >= 6` 且 `<= 21` (Skyrim.esm；全局變數似乎是時間或種族時間檢查)。
- `GetIsAliasRef alias #1` (妓女 NPC；喬裝的嫌疑人/吸血鬼)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x006267 zzAoMMq01B6Whore` | `0x006268` | Goodbye | `GetStage == 20`; `GetGlobalValue 0x000038` [6–21]; `GetIsAliasRef alias #1` | 提示：(無) 回覆 (中性)：[`「晚上再來吧。我會給你一個美味的甜圈。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:32) |
| `0x006267 zzAoMMq01B6Whore` | `0x006269` | WalkAway | `GetStage == 20`; `GetIsAliasRef alias #1` | 提示：(無) 回覆 (中性)：[`「你喜歡甜圈嗎？我的甜圈很美味喔？」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:33) |

### 分支 7：接受甜圈 — 「拿一個」

話題 `0x00626A zzAoMMq01B6Yes`

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x00626A zzAoMMq01B6Yes` | `0x00626B` | Goodbye | `GetIsAliasRef alias #1` | 提示：[`「拿一個」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:35) 回覆 (中性)：[`「謝謝。你能閉上眼一會兒嗎？我很害羞……」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:36) |

VMAD 片段：
- `AoM01_TIF__0100626B` (觸發 `OnEnd` 片段)
- (推論：片段執行吸血鬼攻擊或過渡到階段 30)

翻譯筆記：
- "youe" 是原始來源中的拼寫錯誤，意為 "your"。

### 分支 8：拒絕甜圈 — 「不需要」

話題 `0x00626C zzAoMMq01B6No`

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x00626C zzAoMMq01B6No` | `0x00626D` | Goodbye | `GetIsAliasRef alias #1` | 提示：[`「不需要」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:38) 回覆 (中性)：[`「那太遺憾了……難道你不想看看我在兜帽下的臉嗎？」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:39) |

翻譯筆記：
- "in Hood" 可能是對嫌疑人喬裝 (兜帽/斗篷) 的誤譯或風格化文本。

### 分支 9：任務完成 — 「擊敗了吸血鬼，她喬裝成了一名妓女」

話題 `0x00626F zzAoMMq01B7MissionComplete`

條件模式：
- `GetStage == 40`：在擊敗吸血鬼後觸發。
- `GetIsAliasRef alias #0` (阿爾塔諾)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x00626F zzAoMMq01B7MissionComplete` | `0x006270` | 無 | `GetStage == 40`; `GetIsAliasRef alias #0` | 提示：[`「擊敗了吸血鬼，她喬裝成了一名妓女。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:41) 回覆 (開心)：[`「嗯……這就是為什麼你看起來煥然一新。你將來會出人頭地的。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:42) 回覆 (開心)：[`「總之……妓女……我們的工作完成了。我也要在晚上去玩玩。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:43) |

VMAD 片段：
- `AoM01_TIF__01006270` (觸發 `OnEnd` 片段)
- (推論：片段將階段推進到 50，透過 `CompleteQuest` 標誌完成任務)

## 相關記錄 (Related Records)

根據 `infodiag`，這些並不完全屬於任務 `005CE3`，但它們是必要的背景資訊：

NPC：
- [`000D62 zzzAoMVigilantTraitor` - 阿爾塔諾](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:835) (任務提供者)
- 吸血鬼妓女：(別名 #1；NPC 身份待透過更深層的 QUST 別名轉儲確定)

全局變數 (Globals)：
- `000038` (Skyrim.esm)：預計為 `GameHour` 或類似的時間全局變數 (甜圈可用性門檻為 6–21)。

## 重建筆記 (Reconstruction Notes)

基於來源 (Source-grounded)：
- 此任務是由雪漫城的一位阿凱祭司請求觸發的簡單吸血鬼狩獵。
- 階段進度為線性：階段 0–10 (聯繫阿爾塔諾) → 階段 15 (調查犯罪現場) → 階段 20 (分析受害者，識別嫌疑人為「新手吸血鬼」) → 階段 30 (擊敗喬裝成妓女的吸血鬼) → 階段 40 (向阿爾塔諾報告) → 階段 50 (任務完成)。
- 「甜圈」互動是一個氣氛時刻，吸血鬼嫌疑人向玩家提供點心，推測在 Goodbye 對話結束後進入戰鬥。
- 所有對話皆以別名引用為條件，表明阿爾塔諾和妓女/吸血鬼是任務相關的 NPC。
- 無自定義場景記錄；所有編排皆透過對話條件和 VMAD 片段完成。

任務中存在階段 11 和階段 9999，但對話中未引用；其用途需要更深層的 Papyrus 片段檢查。

待驗證事項 (Open verification)：
- 檢查腳本 `AoM01_TIF__01006259`, `AoM01_TIF__0100625C`, `AoM01_TIF__0100626B`, `AoM01_TIF__01006270` 以了解確切的階段推進和別名；
- 直接檢查 QUST 別名 (透過更豐富的別名轉儲) 以確認別名 #0 = 阿爾塔諾, 別名 #1 = 妓女/吸血鬼；
- 檢查全局變數 `000038` (Skyrim.esm) 以確認時間門檻；
- 如果犯罪現場地點定位重要，檢查單元 `0x0165AA` (Skyrim.esm)；
- 檢查妓女別名 (#1) 的 NPC 形式數據，以確認吸血鬼喬裝機制 (例如，等級角色、喬裝套件、行為標誌)。
