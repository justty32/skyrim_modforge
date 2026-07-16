# 第 1 幕 支線任務 08 - 絕不仁慈

狀態：第一個重做切片。基於源代碼、連結優先，並非劇情摘要。

來源策略：
- 原始台詞連結回提取的源文件，而非完整複製。
- 僅在需要解釋背景或翻譯問題時出現簡短的原始片段。
- 場景主題提取自 dialogue.md；場景相位/動作來自 `infodiag` CLI。
- 分支文本質量嚴重降級（OCR/翻譯偽影）；在需要推論的地方進行了明確標記。

## 任務記錄

[`00EA8A zzzAoMMq08 "絕不仁慈"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:132)

CLI：
- `questdiag Vigilant.esm 0x00EA8A`
- `infodiag Vigilant.esm 0x00EA8A`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x00EA8A`
- EditorID: `zzzAoMMq08`
- 名稱: `絕不仁慈`
- 標記: `RunOnce`
- 優先級: `90`
- 類型: `SideQuest` (支線任務)
- 過濾器: `AoM\`

來自 `questdiag` 的階段：

| 階段 | 標記 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 10 | 無 | 空 |
| 15 | 無 | 空 |
| 18 | 無 | 空 |
| 20 | 無 | 空 |
| 30 | CompleteQuest | 空 |
| 200 | 無 | 空 |
| 200 | 無 | 3 個條件 |
| 210 | 無 | 空 |
| 220 | 無 | 空 |
| 230 | CompleteQuest | 空 |
| 300 | 無 | 空 |
| 310 | CompleteQuest | 空 |
| 999 | ShutDownStage | 空 |
| 9999 | 無 | CompleteQuest |

目標：

| 索引 | 來源 | 文本 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:133) | 獵殺巫女 |
| 200 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:134) | 與阿爾塔諾協商 (選項) |
| 210 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:135) | 擊敗阿爾塔諾 (選項) |
| 300 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:136) | 擊敗醜陋者 |

目標標靶：
- 目標 0 (獵殺巫女)：2 個標靶，每個標靶有 2 個條件。
- 目標 200 (與阿爾塔諾協商)：1 個標靶，有 0 個條件。
- 目標 210 (擊敗阿爾塔諾)：1 個標靶，有 0 個條件。
- 目標 300 (擊敗醜陋者)：1 個標靶，有 0 個條件。
- 當前 CLI 輸出未打印標靶單元/引用詳細信息；如果位置標向很重要，則需要更深入的 QUST 標靶轉儲。

## 別名 / 暫存骨幹

主機任務：
- `00EA8A zzzAoMMq08` 「絕不仁慈」

來自 `infodiag` 的別名：

| 別名 | 名稱 (推論) | 填充 |
|---:|---|---|
| 0 | `莉莉安` | (巫女 NPC；用於對話條件的別名) |
| 4 | `阿爾塔諾` | (任務給予者；結尾對話夥伴) |

（推論：別名 0 和 4 是從對話中的 `GetIsAliasRef` 條件推論出來的；CLI 沒有提供明確的別名轉儲。別名 0 處理巫女 NPC 莉莉安；別名 4 是阿爾塔諾，從 sq07 延續而來。）

## 任務敘事骨幹

**從對話條件推論出的階段進展：**

- **階段 0-15**：到達與問候。玩家在巫女營地遇到阿爾塔諾和莉莉安。初始任務：評估局勢（階段 10-15）。
- **階段 18-20**：對話調查階段。玩家從莉莉安那裡收集關於巫女及其丈夫身上詛咒的信息（階段 20）。
- **階段 30**：完成「獵殺巫女」目標（標記：`CompleteQuest`）。正常的獵巫完成路徑。
- **階段 200-220**：開啟替代路徑（受階段門控 `200 ≤ 階段 < 230`）。主題如 [`0x0423C3 zzzAoMMq08B1NoWitch`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:653) 在此處分支，指示玩家質疑巫女存在的分歧。
- **階段 210**：代表拒絕路徑或持續抵抗（此階段的目標為「擊敗阿爾塔諾 (選項)」）。
- **階段 230**：`CompleteQuest` — 替代完成路徑（透過對話拒絕或對抗阿爾塔諾來擊敗巫女）。
- **階段 300**：後期目標（「擊敗醜陋者」） — 暗示在莉莉安之外還有最終敵人遭遇。
- **階段 310**：`CompleteQuest` — 完成「擊敗醜陋者」路徑。
- **階段 999**：`ShutDownStage` — 清理。
- **階段 9999**：最終完成總匯。

## 場景主題與對話分支

來自 `infodiag` 的場景主題是主題記錄（不是正式的 SCEN 記錄），類別為 Scene。按主題 FormID 和對話提示列出：

### 0x042937 zzzAoMMq08SceneKill (場景標記)

提取的文本 (1 行)：
- [「你將殺死巫女。靠你自己……」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:674)

背景：阿爾塔諾命令玩家獨自執行獵巫。標誌著接受/分配階段。

## 自定義對話分支：莉莉安 (巫女 NPC 別名 #0)

分支：
- [`00EFF0:Vigilant.esm`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:642) (任務分支根；過濾器=AoM\; SNAM=CUST)

條件模式：
- 對於大多數主題，階段門限於 `GetStage < 200`（初始遭遇）。
- 別名 #0 條件 (`GetIsAliasRef 別名 #0`) 標識說話者為莉莉安。

### 0x00EFF4 zzAoMMq08B1RunAway (莉莉安恐慌)

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EFF4 zzAoMMq08B1RunAway`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:642) | `0x00EFF5` | Goodbye | `GetStage < 200`; `GetIsAliasRef 別名 #0` | 提示：[「我是斯坦達爾警戒者。我聽說這裡有巫女……」] 回應 (Fear)：[「……！莉莉安！！跑！快跑！！」] |

VMAD 片段：
- `AoM08_TIF__0100EFF5` (觸發 `OnEnd` 片段；可能推進階段或觸發戰鬥)

（推論：提示是通用的警戒者問候；回應顯示莉莉安警告一個名叫莉莉安的人逃跑，這暗示莉莉安本人正在對另一個自我或莉莉安正在呼喚的小孩別名喊叫。）

### 0x0423BD zzzAoMMq08B1WhatHere (莉莉安的職業)

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x0423BD zzzAoMMq08B1WhatHere`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:642) | `0x0423BE` | 無 | `GetStage < 200`; `GetIsAliasRef 別名 #0` | 提示：「你在這裡做什麼？」回應 (Fear)：[「我有一種藥劑配方。因為我靠煉金術謀生……」] |

背景：莉莉安解釋她是煉金術士，不一定是巫女。詞組 「formulation of the drug」 (藥劑配方) 不明確 (OCR 偽影)；可能意指 「potion」 (藥水)。

### 0x0423BF zzzAoMMq08B1Alchemy (莉莉安的老師)

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x0423BF zzzAoMMq08B1Alchemy`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:645) | `0x0423C0` | 無 | `GetIsAliasRef 別名 #0` | 提示：「你在哪裡學的煉金術？」回應 1 (Fear)：[「向巫女學的……但那是……格倫莫瑞爾，但我不是那些女孩的朋友。」] 回應 2 (Sad)：[「在絕望的希望中……所以……想解開她丈夫身上的詛咒」] |

翻譯筆記：
- 「Gurenmoriru」 可能是轉錄錯誤的名稱 (OCR 偽影)；似乎是巫女老師的名字，可能是 Garenmormire 或類似名稱。
- 第二個回應表明莉莉安向巫女學習煉金術是希望能解開自己丈夫身上的詛咒。

### 0x0423C1 zzzAoMMq08B1GoMove (莉莉安的逃脫提議)

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x0423C1 zzzAoMMq08B1GoMove`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:649) | `0x0423C2` | Goodbye | `GetIsAliasRef 別名 #0` | 提示：「我不介意，現在這裡很危險。你最好離開這個地方」回應 1 (Neutral)：[「讓你看看你來了……好吧。你找到了……」] 回應 2 (Happy)：[「我決定儘快離開這裡。斯坦達爾與你同在」] |

VMAD 片段：
- `AoM08_TIF__010423C2` (觸發 `OnEnd` 片段；可能向階段 30 或任務完成推進)

翻譯筆記：
- 「Stendhal」 是轉錄錯誤；應為 「Stendarr」 (斯坦達爾神)。
- 第二個回應顯示莉莉安接受了逃脫，這被視為一種慈悲/人道主義的選擇。

## 自定義對話分支：阿爾塔諾 (任務給予者別名 #4)

分支：
- [`00EFF0:Vigilant.esm`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:638) (任務分支根)

條件模式：
- 大多數主題階段門限於 `GetStage < 200` 或 `200 ≤ GetStage < 210` 用於升級。
- 別名 #4 條件標識說話者為阿爾塔諾。

### 0x00EFF1 zzAoMMq08B1WitchHunt (阿爾塔諾的命令)

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EFF1 zzAoMMq08B1WitchHunt`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:638) | `0x00EFF6` | 無 | `GetIsAliasRef 別名 #4` | 回應 (Anger)：[「巫女對天際省的和平構成了嚴重威脅。殺光她們。」] |

背景：阿爾塔諾的主要指令；此處無階段條件，因此始終作為開場對話可用。

### 0x00EFF3 zzAoMMq08B1Unknown01 (未知說話者問候)

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EFF1 (續)`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:638) | `0x00EFF3` | SayOnce | `GetIsAliasRef 別名 #0` | 回應 (Puzzled)：[「誰……你是誰？請……離我們遠點……？」] |

（推論：此處的別名 #0 表明此回應來自莉莉安，而非阿爾塔諾；可能是同一個主題下的第二個 INFO，說話者交替。）

### 0x0423BA zzzAoMMq08B1AboutWitch (關於巫女)

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x0423BA zzzAoMMq08B1AboutWitch`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:638) | `0x0423BB` | Goodbye | `GetStage < 200`; `GetIsAliasRef 別名 #4` | 提示：「告訴我關於紫杉鎮巫女的事」回應 1 (Disgust)：[「Val Lee 是巫女母女的家園。不要因為她們假裝煉金術士就被騙了」] 回應 2 (Disgust)：[「除了婦女和兒童，不要寬恕任何對手。如果你偷懶，我就會淪為巫女的獵物」] |

VMAD 片段：(從 INFO 中隱含)

翻譯筆記：
- 「Val Lee」 是巫女棚屋的地點 / 家族名稱 (或者可能是轉錄錯誤)。
- 第二個回應：「除了婦女和兒童，不要寬恕任何對手」含義模糊；可能意指 「除了婦女和兒童外，不要表現出任何仁慈」。
- 「淪為巫女的獵物」可能意味著 「我將暴露在巫術之下」 或 「我將任由她們擺佈」。

### 0x0423C3 zzzAoMMq08B1NoWitch (玩家否認巫女存在)

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x0423C3 zzzAoMMq08B1NoWitch`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:653) | `0x0423C4` | 無 | `200 ≤ GetStage < 210`; `GetIsAliasRef 別名 #4` | 提示：「紫杉鎮沒有巫女」回應 1 (Anger)：[「……所以？似乎已經被妥善洗腦了？你盲目地回來了？」] 回應 2 (Anger)：[「殺了她們！不管是不是！你必須殺了她們！！」] |

翻譯筆記：
- 「Marumekoma」 是轉錄錯誤；指代不明。可能是句髒話或是個模糊的名字。
- 「盲目地回來了」可能意味著 「在未看清真相的情況下回來了」。
- 第二個回應：阿爾塔諾升級為指令 — 不管是否仁慈都要殺死。

（推論：此主題在階段 200+ 分支，表明玩家對獵巫行動提出了質疑或抵制。阿爾塔諾憤怒地加倍強調。）

### 0x0423C5 zzzAoMMq08B1OkOk (玩家默許)

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x0423C5 zzzAoMMq08B1OkOk`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:657) | `0x0423C6` | 無 | `GetIsAliasRef 別名 #4` | 提示：「好吧……」回應 1 (Sad)：[「對不起，我剛才有點……但你明白了」] 回應 2 (Happy)：[「無論如何都必須剷除她們」] |

翻譯筆記：
- 第一個回應：「我剛才有點……」語焉不詳；可能意指 「我剛才有點過分了」。
- 第二個回應：「無論如何」可能意味著 「不擇手段地」。

### 0x0423C7 zzzAoMMq08B1Crazy (玩家質疑阿爾塔諾的神智)

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x0423C7 zzzAoMMq08B1Crazy`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:661) | `0x0423C8` | 無 | `GetIsAliasRef 別名 #4` | 提示：「你有點瘋了，阿爾塔諾……」回應 1 (Anger)：[「你難道就不能做點什麼嗎，比如別開玩笑……！是你！無法理解你！」] 回應 2 (Anger)：[「列在我們受害者名單上的巫女！快去！你現在就該去！」] |

翻譯筆記：
- 「比如別開玩笑」可能意指 「別再胡鬧了」 或 「別開玩笑了」。
- 第二個回應：「列在我們受害者名單上的巫女」不明確；可能意指 「巫女殘害了我們的族人」。

### 0x0423C9 zzzAoMMq08B1Wrong (玩家道德反對)

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x0423C9 zzzAoMMq08B1Wrong`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:665) | `0x0423CA` | 無 | `GetIsAliasRef 別名 #4` | 提示：「這是錯的……」回應 1 (Anger)：[「冷靜點，這哪裡錯了？到現在才被殺已經太遲了！現在，就按我說的做！」] 回應 2 (Anger)：[「這次也一樣。我希望你能像剛才說的那些傢伙一樣，以斯坦達爾之名殺了它！」] |

翻譯筆記：
- 第一個回應嚴重失真；「The cold just as wrong」(冷靜點，這哪裡錯了) 難以理解。可能想表達 「你的猶豫是錯誤的」 或類似意思。
- 第二個回應：「以斯坦達爾之名」(源代碼中 Stendarr 的轉錄錯誤) 暗示了宗教正當性。

### 0x0423CB zzzAoMMq08B1Never (玩家斷然拒絕)

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x0423CB zzzAoMMq08B1Never`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:669) | `0x0423CC` | Goodbye | `GetIsAliasRef 別名 #4` | 提示：「我絕不那樣做！！」回應 1 (Neutral)：[「我明白了那個意圖……這看起來很難。我很抱歉。非常抱歉……」] 回應 2 (Happy)：[「那樣就必須改變一下方式，雖然有點粗暴……」] |

VMAD 片段：
- `AoM08_TIF__010423CC` (觸發 `OnEnd` 片段；可能向階段 210 或 「擊敗阿爾塔諾」路徑推進)

翻譯筆記：
- 第一個回應：「我明白了那個意圖……這看起來很難」暗示阿爾塔諾察覺到了玩家的決心，但覺得這很難辦。
- 第二個回應：「那樣就必須改變一下方式」語焉不詳；可能意指 「我們必須改變方法」 或 「還有另一種路徑」。

（推論：此分支通往階段 210+，玩家在此積極反對阿爾塔諾，可能觸發戰鬥或「擊敗阿爾塔諾」目標。）

## 相關記錄

NPCs:
- [`0012D2 (來自提取的 dialogue.md:696 的莉莉安)` - 莉莉安 (「我是煉金術士。我會變得像我媽媽一樣……」)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:696)
  - 莉莉安是巫女營地的一位煉金術士 NPC，似乎是首席巫女的女兒。
  - 尚未在 game-data 中進行專門的 NPC 記錄查詢；僅有提取的文本。
- [`000D66 zzzAoMVigilantElder` - 阿爾塔諾](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1) (從 sq07 延續)

物品：
- (此任務中沒有明確標記為受任務限制的物品；目標「擊敗醜陋者」暗示有最終 Boss 掉落物，但當前 CLI 輸出中未標識。)

地點：
- 紫杉鎮 (巫女棚屋所在地；在對話中被稱為 「Ivarstead」 或 「Ivasted」；根據天際省地理環境，正式名稱為 「紫杉鎮」)。

## 重建筆記

基於源代碼：
- 任務 `00EA8A zzzAoMMq08` 延續自 sq07，傑克派遣玩家去紫杉鎮的一個棚屋獵殺巫女。
- 任務結構呈現了一個道德選擇：正常的獵巫行動（階段 30 完成）、基於對話的拒絕路徑（階段 200–210，通往「擊敗阿爾塔諾」目標），或發現更大的威脅（階段 300 的「擊敗醜陋者」）。
- 存在兩個主要的對話分支：
  - 莉莉安 (別名 #0)：巫女營地的一位令人同情的煉金術士，提供逃脫或仁慈路徑。
  - 阿爾塔諾 (別名 #4)：對徹底毀滅的要求日益狂熱，對玩家的抵制有分支回應。
- 對話文本質量較差 (OCR 偽影、轉錄錯誤、語法錯誤)，顯示提取管道存在編碼或解析問題。

分支極性 (推論)：
- **仁慈路徑**：聽取莉莉安的話，考慮逃脫（階段 30 完成，暗示獲得好的業力）。
- **狂熱路徑**：遵循阿爾塔諾殺死所有巫女的指令；對話分支在階段 200–210 升級。
- **反叛路徑**：拒絕阿爾塔諾；觸發階段 210+ 和 「擊敗阿爾塔諾」目標。
- **未知路徑**：階段 300 的「擊敗醜陋者」，暗示巫女營地之外隱藏著一個敵人（可能是母親/女巫團首領）。

業力結果：
- 從當前來源看不明確；很大程度上取決於採取哪條對話路徑以及完成哪個目標（獵殺對比協商對比擊敗阿爾塔諾對比擊敗醜陋者）。

發布狀態：
- 未檢測到不完整的片段；所有帶有 VMAD 標記的對話都可能通向階段進展。
- 莉莉安恐慌的問候 (「快跑！！」) 和逃脫提議表明機械上支持和平解決。

公開驗證：
- 直接檢查別名 (別名 #0 = 莉莉安，別名 #4 = 阿爾塔諾已透過條件模式確認；可能存在其他別名)。
- 如果巫女營地遭遇戰存在正式的場景託管，請檢查 SCEN 記錄。
- 反編譯 VMAD 片段 `AoM08_TIF__*` 以確認階段路由和分支邏輯。
- 解決對話文本中的 OCR/轉錄偽影 (例如 「Gurenmoriru」, 「Marumekoma」, 「Stendhal」 → Stendarr)。
- 識別 「醜陋者」 NPC (階段 300 目標) — 可能是巫女母親/女巫團首領；可能需要 ESM 別名或地點單元查詢。
- 透過更深入的 QUST 標靶轉儲，驗證目標 0 (獵殺巫女)、200 (協商)、210 (擊敗阿爾塔諾)、300 (擊敗醜陋者) 的標靶位置。
