# 第一章支線任務 02 — 不可觸及者 (Act 1 Side Quest 02 - The Untouchable One)

狀態：第一個重做切片。基於來源 (Source-grounded)，連結優先，無 Gemini 幻覺。

來源方針：
- 原始文本行連結回提取的來源文件，而非完整複製。
- 僅在需要解釋語境或條件時才出現短小的來源片段。
- `SCEN` (場景) 編排來自 CLI 診斷（如果有的話）。

## 任務記錄 (Quest Record)

[`006271 zzzAoMMq02 "The Untouchable One"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:TBD)

CLI 指令：
- `questdiag Vigilant.esm 0x006271`
- `infodiag Vigilant.esm 0x006271`

ESM 路徑：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x006271`
- EditorID: `zzzAoMMq02`
- 名稱: `The Untouchable One` (不可觸及者)
- 標誌: `RunOnce`
- 優先級: `90`
- 類型: `SideQuest`
- 過濾器: `AoM\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 標誌 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 10 | 無 | 空 |
| 13 | 無 | 空 |
| 15 | 無 | 空 |
| 17 | 無 | 空 |
| 20 | 無 | 空 |
| 30 | CompleteQuest | 空 |
| 255 | ShutDownStage | 空 |
| 9999 | CompleteQuest | 空 |

目標 (Objectives)：

| 索引 | 來源 | 文本 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:TBD) | 在敕旗母馬客棧與阿爾塔諾交談 |
| 10 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:TBD) | 擊敗魔族 |
| 20 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:TBD) | 向阿爾塔諾報告 |

目標對象 (Objective targets)：
- 目標 0: 1 個對象，0 條件。
- 目標 10: 2 個對象，各 1 條件。
- 目標 20: 1 個對象，0 條件。
- 目前的 CLI 輸出不列印目標單元/引用細節；如果地點定位重要，則需要更深層的 QUST 對象轉儲。

## 別名 / 編排骨幹 (Alias / Staging Backbone)

`infodiag` 未偵測到自定義 `SCEN` 記錄。階段進度透過對話條件顯示為線性。

主機任務 (Host quest)：
- `006271 zzzAoMMq02` "The Untouchable One"

來自 `infodiag` 的對話別名：
- 別名 `#0`：預計為 `阿爾塔諾` (主任務提供者)
- 別名 `#1`：預計為 `維納庫斯` (Vernaccus，魔族頭目)

(推論：別名名稱與角色從對話條件 `GetIsAliasRef` 索引 0 和 1 推論而來；CLI 無提供顯式別名轉儲)

## 自定義對話分支 (Custom Dialogue Branches)

### 分支 1：任務開端 — 「有什麼反常的情況嗎？」

話題 (TOPIC) `0x006274 zzAoMMq02B1Mission2`

條件模式：
- `GetStage < 10`：在玩家進入初始對話之前觸發。
- `GetInCell 0x01605E` (Skyrim.esm，推測為敕旗母馬客棧)。
- `GetIsAliasRef alias #0` (阿爾塔諾)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x006274 zzAoMMq02B1Mission2` | `0x006275` | 無 | `GetStage < 10`; `GetInCell 0x01605E`; `GetIsAliasRef alias #0` | 提示：[`「有什麼反常的情況嗎？」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:45) 回覆 (恐懼)：[`「幾個小時前，城牆外的一棟房子被魔族破壞了。魔族在放聲大笑。他直到現在還待在那棟破房子裡。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:46) 回覆 (疑惑)：[`「有人目擊到一名可能是魔族召喚師的女性。我們的任務是擊敗魔族並抓住召喚師。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:47) |

VMAD 片段：
- `AoM02_TIF__01006275` (觸發 `OnEnd` 片段)
- (推論：片段可能設置階段 10+ 以推進任務)

翻譯筆記：
- "staeyed" 是原始來源中的拼寫錯誤，意為 "stayed"。

### 分支 2：維納庫斯遭遇 — 頭目挑釁

話題 `0x006277 zzAoMMq02B2Vernaccus`

條件模式：
- `GetStage < 15`：在擊敗維納庫斯之前觸發。
- `GetIsAliasRef alias #1` (維納庫斯)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x006277 zzAoMMq02B2Vernaccus` | `0x006278` | WalkAway | `GetStage < 15`; `GetIsAliasRef alias #1` | [`「我是維納庫斯！人稱不可觸及者！！可憐的凡人，屈服於我的力量之下吧！！」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:50) |

(推論：WalkAway 標誌表明 NPC 在說完話後會撇開玩家；隨後可能觸發戰鬥)

### 分支 3：戰鬥對話 — 勇氣挑戰

話題 `0x008DD5 zzAoMMq02B2Fight`

條件模式：
- `GetIsAliasRef alias #1` (維納庫斯)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x008DD5 zzAoMMq02B2Fight` | `0x008DD6` | Goodbye | `GetIsAliasRef alias #1` | 提示：[`「我們是斯丹達爾警戒者。你準備好回到湮滅之境了嗎？」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:TBD) 回覆：[`「嘰嘰！！但是，我是不可觸及者！！無論你如何意識到自己的力量，我絕不會輸給你！！」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:TBD) |

VMAD 片段：
- `AoM02_TIF__01008DD6` (觸發 `OnEnd` 片段)
- (推論：片段可能管理戰鬥狀態或階段進度)

### 分支 4：戰鬥挑釁 — 魔族的自信

話題 `0x2D5C61 zzzAoMMq02B203`

條件模式：
- `GetIsAliasRef alias #1` (維納庫斯)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x2D5C61 zzzAoMMq02B203` | `0x2D5C62` | Goodbye | `GetIsAliasRef alias #1` | 回覆 (開心)：[`「哈，哈，哈阿阿！！你害怕了！！我清楚地感受到了你的恐懼！」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:TBD) 回覆 (開心)：[`「實力的差距你是無法彌補的。隨時隨地全力以赴。發出痛苦的尖叫吧！！」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:TBD) |

VMAD 片段：
- `AoM02_TIF__022D5C62` (觸發 `OnEnd` 片段)

### 分支 5：任務完成 — 向阿爾塔諾報告

話題 `0x00627C zzAoMMq02B3MissionComplete`

條件模式：
- `GetStage == 20`：在擊敗維納庫斯後觸發 (階段 20 為戰鬥後狀態)。
- `GetIsAliasRef alias #0` (阿爾塔諾)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x00627C zzAoMMq02B3MissionComplete` | `0x00627D` | 無 | `GetStage == 20`; `GetIsAliasRef alias #0` | 提示：[`「召喚師在哪裡？」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:52) 回覆 (憤怒)：[`「召喚師已經從這裡逃走了。她召喚了維納庫斯，這可是高階魔族。她的召喚能力是專家級的。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:53) 回覆 (開心)：[`「我會回到旅店收集關於召喚師的信息。如果你準備好了，就來找我。」`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:54) |

VMAD 片段：
- `AoM02_TIF__0100627D` (觸發 `OnEnd` 片段)
- (推論：片段可能完成目標 20 並向階段 30 推進)

翻譯筆記：
- "informaiton" 是原始來源中的拼寫錯誤；意為 "information"。

## 階段流程推論 (Stage Flow Inference)

基於對話條件門檻和目標進度：

1. **階段 0**：任務開始。玩家收到初始任務簡報。
2. **階段 10**：在敕旗母馬客棧聽完阿爾塔諾的話後觸發 (分支 1 對話 + VMAD 片段)。
   - 目標 0 完成。
   - 目標 10 (擊敗魔族) 變為啟動狀態。
3. **階段 13, 15, 17**：戰鬥期間的中間檢查點。若不檢查 VMAD 則用途不明。
4. **階段 20**：擊敗維納庫斯後觸發 (戰鬥後狀態)。
   - 目標 10 完成。
   - 目標 20 (向阿爾塔諾報告) 變為啟動狀態。
   - 分支 5 對話變為可用。
5. **階段 30**：任務完成標誌 (questdiag 輸出中的 `CompleteQuest`)。
   - 在玩家向阿爾塔諾報告後觸發 (分支 5 對話 + VMAD 片段)。
6. **階段 255**：ShutDownStage (引擎清理)。
7. **階段 9999**：備用完成標誌 (與階段 30 重複)。

## 分支極性分析 (Branch Polarity Analysis)

**單一線性路徑** (無分支)：
- 所有對話分支皆以任務階段和別名引用為條件，而非玩家的選擇。
- 分支 5 中「憤怒」與「開心」的回覆變體表明了阿爾塔諾的情緒狀態或語境變化，但不會分叉任務結果。
- 無對話條件執行條件性的任務失敗或替代結局。
- **結論**：這是一個**強制進度的線性任務**。召喚師逃脫與魔族被擊敗是通往下一章任務的強制性標記點。

## 相關記錄 (Related Records)

NPC：
- `zzzAoMMq01` 阿爾塔諾 (第一至二章的主任務提供者)
  - (推論：阿爾塔諾繼續擔任任務提供者；驗證 Vigilant npc.tsv 中的 NPC 記錄)
- 維納庫斯 (Vernaccus，由未知召喚師召喚的高階魔族頭目)
  - (推論：名稱從對話話題 "zzAoMMq02B2Vernaccus" 和頭目對話推論而來；驗證 NPC 記錄)

地點：
- 敕旗母馬客棧，雪漫城 (Skyrim.esm 中的單元 0x01605E)
  - (推論：敕旗母馬客棧是雪漫城原版的客棧；單元 ID 0x01605E 確認了地點)
- 破房子的位置 (對話中未命名，等待單元/引用驗證)

生物/魔族：
- 維納庫斯 (未指定類型的高階魔族；非 Skyrim.esm 中的標準魔族)
  - (推論：「高階魔族」表明了等級或力量層次；確切魔族類型待透過 NPC 記錄確定)

法術/召喚：
- 召喚維納庫斯法術或等效法術 (由未知召喚師施放)
  - (推論：「她的召喚能力是專家級的」暗示了法術施放；法術名稱待透過法術記錄搜尋確定)

## 待驗證事項 (Open Verification)

- [ ] 驗證 Vigilant npc.tsv 中的阿爾塔諾 NPC 記錄 (FormID, 地點, 派系)。
- [ ] 檢查維納庫斯生物/NPC 記錄：類型, 等級, AI 包, 魔法抗性。
- [ ] 驗證破房子的地點：單元 ID, 單元外部, 發生戰鬥的座標範圍。
- [ ] 如果存在來源，解碼 VMAD 片段 (`AoM02_TIF__0100627[5D]`, `AoM02_TIF__01008DD6`, `AoM02_TIF__022D5C62`)：
  - [ ] 是否有片段在戰鬥期間設置階段 13, 15, 17？
  - [ ] 是否有片段取決於維納庫斯的死亡狀態？
  - [ ] 片段是推進到階段 30，還是由任務系統處理？
- [ ] 驗證召喚師使用的法術/能力 (在 Vigilant.esm 中搜尋 "Conjure Vernaccus" 或等效法術)。
- [ ] 驗證單元 0x01605E 確實是 Skyrim.esm 中的敕旗母馬客棧。
- [ ] 確定階段 20 是由維納庫斯的死亡觸發器設置，還是由玩家發起的對話設置。
- [ ] 確定業力結果：擊敗召喚師召喚的魔族在斯丹達爾語境下是否被視為「好」頃向？
- [ ] 檢查目標 10 的對象以了解戰鬥地點或遭遇編排 (CLI 輸出顯示 2 個有條件的對象；它們是什麼？)。

## 重建筆記 (Reconstruction Notes)

此任務代表了斯丹達爾警戒者任務線中的**第一章支線任務 02**，接續在第一章任務 01 (吸血鬼調查) 之後。它引入了更高層次的魔族威脅，並預示了延伸至第二至三章的召喚師劇情線。

關鍵編排元素：
- 任務為線性，無玩家選擇分叉。
- 對話在入口 (階段 < 10)、戰鬥參與 (階段 < 15) 和完成 (階段 == 20) 處進行階段門檻限制。
- 未知的召喚師逃脫了；維納庫斯被擊敗，但代表了任務複雜性的一個轉折點。
- 目標與對話同步：每個分支對應一個目標階段。

基於來源的連結：
- [`006271 zzzAoMMq02` 任務記錄](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:TBD)
- [`006274`, `006277`, `008DD5`, `0x2D5C61`, `00627C` 對話話題](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:45–54)
- 假設別名存在，但 CLI 輸出中未明確命名；等待 QUST 別名轉儲以進行確認。
