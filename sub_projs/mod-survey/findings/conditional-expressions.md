# Conditional Expressions — 深挖 Finding

Nexus：https://www.nexusmods.com/skyrimspecialedition/mods/45148
版本：1.29（2025-08）
本地：`~/skyrim_mods/unzip/Conditional Expressions-45148-1-29-1755293339/`
Source：16 個 .psc 全讀。

---

## 一、這個工具做什麼 + 工作原理

Conditional Expressions（CE）是一個「玩家狀態 → 臉部表情即時反應」系統。核心思想：偵測玩家當下的狀態（喝醉、吃東西、受傷、潛行、寒冷、低魔、隨機 idle…），用 MFG（Morph Face Generator）API 把臉部推到對應表情，效果結束後乾淨還原。

**架構：**
- 一個常駐 quest（`CondiExp_StartMod`），以 ReferenceAlias 掛在玩家身上，用 `OnObjectEquipped` 監聽飲食/道具使用事件並設 GlobalVariable。
- 一批 magic effects（MGEF），各自附腳本，由玩家身上一個常駐 spell（`CondiExp_Effects`）驅動。每個 effect 負責一種狀態，在 `OnEffectStart` / `OnEffectFinish` / `OnUpdate` 三個生命週期操作 MFG。
- 一個共用 GlobalVariable `CondiExp_CurrentlyBusy`（值 0/1）作 mutex，防止多個 effect 同時踩臉部狀態。
- MCM（`condiexp_MCM extends SKI_ConfigBase`）管理每種表情的開關 GlobalVariable。

---

## 二、MFG API 完整參考（從 CE 原始碼提取）

CE 用到以下三種 SKSE MFG 函式，皆為 `MfgConsoleFunc` namespace：

### `MfgConsoleFunc.SetModifier(Actor akActor, int index, int value)`
控制臉部「修飾子」（Modifier），影響眉毛、眼睛收縮、嘴角等肌肉群。value 範圍 0–100+（實測最高到 115）。

CE 用到的 Modifier 索引對照：

| 索引 | 意義（CE 用法推斷） | CE 使用情境 |
|------|-------------------|-----------:|
| 0 | 眉頭內側上揚（左） | Yawn、Cold、Thinking |
| 1 | 眉頭內側上揚（右） | Yawn、Thinking |
| 2 | 眉頭下壓（左，皺眉） | Frown、Pain、Sneaking |
| 3 | 眉頭下壓（右，皺眉） | Frown、Pain |
| 4 | 眼皮下壓（左，疲態） | Cold（顫抖預備）、Pain |
| 5 | 眼皮下壓（右，疲態） | Pain |
| 6 | 眉毛上揚（左） | BrowsUp、BrowsUpSmile、Yawn |
| 7 | 眉毛上揚（右） | BrowsUp、BrowsUpSmile、Yawn、Thinking |
| 8 | 眼皮下看（LookDown） | LookDown |
| 9 | 眼球左轉 | LookLeft、Angry、Sneaking |
| 10 | 眼球右轉 | LookRight |
| 11 | 眼白上揚（Skooma 放空） | Skooma |
| 12 | 左眼瞇縮（Squint） | Cold、Sneaking、Water、Squint |
| 13 | 右眼瞇縮（Squint） | Cold、Sneaking、Water、Squint |

### `MfgConsoleFunc.SetPhoneme(Actor akActor, int index, int value)` / `SetPhoneMe`（同函式，兩種拼法）
控制嘴型（Phoneme/口型），影響嘴巴開合與嘴角形狀。

| 索引 | 意義（CE 用法推斷） | CE 使用情境 |
|------|-------------------|-----------:|
| 0 | 嘴巴張開（主要） | Eating（咀嚼）、Cold（顫抖）、Fatigue（喘息） |
| 1 | 「Aah」寬開口 | Yawn |
| 4 | 嘴角上揚（Smile） | Smile、BrowsUpSmile |
| 5 | 嘴角上揚寬（wider smile） | BrowsUpSmile |
| 7 | 噘嘴或思考嘴型 | Thinking |
| 9 | VampireOuch 嘴型 | Angry（Vampire 版 Ouch） |
| 10 | 「Oh」圓唇 | HumanOuch（OnHit） |
| 12 | 上齒外露（TeethIn/TeethOut） | Eating |

### `Actor.SetExpressionOverride(int expressionID, int strength)` / `ClearExpressionOverride()`
覆蓋整個臉部表情底板，強度 0–100。`ClearExpressionOverride()` 還原。

CE 用到的 Expression ID：

| ID | 意義 | CE 使用情境 |
|----|-----|------------|
| 1 | 痛苦/皺眉 | Cold |
| 2 | 快樂 | Drunk（happy 80）、Skooma（隨機 30-70）|
| 3 | 悲傷/哀愁 | Headache（漸進 95）、Pain2 |
| 4 | 害羞/窘迫 | NoClothes（blush 90） |
| 9 | 驚恐 | Pain1（scared 60） |
| 10 | 喜悅（Happy） | Random:Happy |
| 13 | 困惑（Puzzled） | Random:Puzzled |
| 14 | 噁心（Disgust） | Random:Disgust |
| 15 | 憤怒/戰鬥嘴型 | Angry（combat 70/100）|

### `MfgConsoleFunc.ResetPhonemeModifier(Actor akActor)`
一次清除所有 Phoneme 與 Modifier，等同把嘴型和修飾子全歸零。CE 在 `OnInit`、uninstall、restore 時呼叫。

---

## 三、各 Effect 機制詳解

### 狀態偵測層（CondiExp_StartMod.psc）
掛在玩家 alias，監聽 `OnObjectEquipped`：
- 物品在 `CondiExp_Drinks` FormList → 設 `CondiExp_PlayerIsDrunk = 1`
- 物品在 `CondiExp_Drugs` FormList → 設 `CondiExp_PlayerIsHigh = 1`
- 物品有 `VendorItemFood` keyword → 設 `CondiExp_PlayerJustAte = 1`，5 秒後清
- 物品有 `VendorItemIngredient` keyword + 吃法設定 → 同上（快/慢模式）

### Drunk Effect（CondiExp_Drunk_Script）
- `OnEffectStart`：設 busy=1，`SetExpressionOverride(2, 80)`（大笑表情），`RegisterForSingleUpdateGameTime(0.2)`（遊戲時間 0.2 小時後到期）
- `OnUpdateGameTime`：清 drunk global，清 busy
- `OnEffectFinish`：`ClearExpressionOverride()`

### Skooma Effect（CondieExp_Skooma_Script）
- 隨機選 happy 強度（30–70）和 smile 強度（10–50）
- `SetExpressionOverride(2, randomhappy)` + `SetModifier(11, 55)`（眼白上翻）+ `SetPhoneme(4, randomsmile)`
- `RegisterForSingleUpdate(60.0)` → 60 秒後清 high global 與 busy

### Eating Effect（CondiExp_Eating_Script）
- 純 Phoneme 驅動，沒有用 SetExpressionOverride
- `TeethIn()`：Phoneme 12 漸增到 25（上齒外露）
- `YumYum()`（×4）：Phoneme 0 在 0↔44 間震盪（咀嚼）
- `TeethOut()`：Phoneme 12 漸減回 0，再清 Phoneme 0
- `ImEatinghere` bool 當 re-entry guard（busy gate 的另一種寫法）
- 快慢模式：GlobalEating == 2 → 先等 0.8 秒再開始；== 1 → 等 3.8 秒

### Cold Effect（CondiExp_Cold_Script）
三種偵測方式（透過 `Condiexp_ColdMethod` global 選擇）：
1. Frostfall：`GetFormFromFile(...).GetValue() > 2`
2. Frostbite：`HasSpell(Cold1/2/3)`
3. Vanilla：`Weather.GetCurrentWeather().GetClassification() == 3 && !IsInInterior()`

驅動流程：
- Intro：`SetExpressionOverride(1, 50)` + Modifier 12/13/4 漸增到 65（每 0.5 秒 +5）
- Steady Tremble：Phoneme 0 在 0→6→12→6 循環抖動
- Outro（`OnEffectFinish`）：Modifier 漸減到 0，清 Phoneme 0 與 ExpressionOverride

### Pain Effect（CondiExp_PainScript）
- 進入前先 `ClearExpressionOverride()` + `ResetPhonemeModifier()`（強制清場），再等 0.9 秒
- `Utility.RandomInt(1,4)` 選 4 種痛苦表情組合（眉頭下壓 + 驚恐 Override + 不同嘴型）
- `OnEffectFinish`：清 busy + ClearExpression + ResetPhoneme

### Sneaking Effect（CondiExp_Sneaking_Script）
- `SetModifier(12/13, 45)` 瞇眼 + `SetModifier(2, 20)` 輕微皺眉
- 隨機決定是否做 LookLeft/LookRight（眼球橫掃）
- `OnEffectFinish`：三個 Modifier 全清 0
- 注意：**沒有用 busy gate**（因為 sneaking 是 low-priority 的背景表情）

### Fatigue Effect（CondiExp_Fatigue）
- 最複雜的 effect，混搭 MFG 和聲音
- `Inhale()`：Phoneme 0 從 33 漸增到 73
- `Exhale(73, 33)`→`Exhale(33, 0)`：同一 Phoneme 漸減
- 條件循環：體力 < 50% 且生命 > 50% 且 breathlimit < 21 → 繼續 Breathe()
- 聲音：依種族/性別播放不同呼吸音效
- **最後靠 busy gate 讓其他 effect 讓路**

### Angry / Combat Effect（CondiExp_AngryScript）
- `OnUpdate`（每秒輪詢）：`IsInCombat()` → `SetExpressionOverride(15, 70)` + `SetPhoneme(4, 20)`（微張嘴）
- `OnHit` state machine（NotReacting/Reacting 雙態）：`RandomNumber() < 40` 才觸發 Ouch，防止每次受擊都反應
- Human Ouch：Phoneme 10 快速衝到 100 再退回（驚呼嘴型）
- Vampire Ouch：Phoneme 10+9+5 同步（齒牙畢露）
- 依賴 Papyrus Extender（PO3）的 `GenerateRandomInt`，有 fallback

### Headache Effect（Condiexp_Headache）
- 純 `SetExpressionOverride(3, 95)` 漸進（悲傷/不適），busy=1
- `OnEffectFinish`：漸減到 0，清 ExpressionOverride，busy=0

### Water Effect（CondiExp_WaterScript）
- `SetModifier(12/13, 75)` 瞇眼（水中/手持火把），busy=1
- `OnEffectFinish`：漸減，清 Modifier，busy=0

### NoClothes Effect（Condiexp_NoclothesScript）
- `SetExpressionOverride(4, 90)`（害羞/窘迫），busy=1
- `OnEffectFinish`：Clear + busy=0

### Random / Idle Effect（CondiExp_RandomScript / CondiExp_RandomVanilla / Condiexp_RandomFrostbite）
三個版本分別對應 Frostfall、Vanilla、Frostbite 的 cold 偵測，Random 表情邏輯完全相同：
- `OnEffectStart`：先查冷度 global；若不冷且 busy=0 → 等 0.5 秒 → 進 `Random()`
- `OnUpdate`：同樣先查 busy，再呼叫 `Random()`
- `Random()`：`RandomInt(1,80)` 對應約 30 種表情組合，包含 LookLeft/Right/Down、Squint、Frown、Smile、Angry、Yawn、BrowsUp、BrowsUpSmile、Thinking、Disgust、Happy、Puzzled
- 每個動作結束後 `RegisterForSingleUpdate(2–5 秒)`，跑步或第一人稱時不觸發
- 所有 Random 動作都是「漸入 → hold → 漸出」三段式，確保恢復乾淨

### Busy Gate 設計

```
CondiExp_CurrentlyBusy = 1  → 其他 effect 跳過（Random/Cold 在 OnEffectStart/OnUpdate 檢查）
執行表情 ...
CondiExp_CurrentlyBusy = 0  → 在 OnEffectFinish 清掉
```

Pain、Drunk、Skooma、NoClothes、Headache、Water、Fatigue、Cold 在執行時設 busy=1。Random 類 effect 尊重 busy，不覆蓋。Sneaking 和 Angry/Combat 不用 busy gate（設計上 sneaking 是背景加工，combat 是持續覆蓋）。

---

## 四、設計模式：如何在自家 follower mod 中利用 CE

### 模式 A：follower 也能受 CE 表情（間接）
CE 目前只對 PlayerRef 運作。如果想讓 NPC/follower 有類似效果，需要自己寫腳本直接呼叫 MFG。CE 的表情參數（索引、強度、duration）可作為直接參考來源。

### 模式 B：三段式 MFG 動畫模式
CE 的所有表情都是「漸入（while loop）→ hold（Utility.Wait）→ 漸出（while loop）」的 pattern，適合直接搬到 follower 表情腳本。每次漸變步長約 5，延遲 0.01–0.05 秒。

### 模式 C：Busy Gate 模式
任何需要「表情互斥」的系統都應實作此 pattern：
1. 動作前查 busy flag（GlobalVariable 或 bool property）
2. 設 busy=1
3. 執行表情
4. `OnEffectFinish` / 計時結束時清 busy=0
5. 其他低優先表情（Random 類）在 `OnUpdate`/`OnEffectStart` 開頭跳過 busy=1 狀態

### 模式 D：狀態偵測 GlobalVariable 中介層
CE 用 GlobalVariable（`CondiExp_PlayerIsDrunk` 等）作 PlayerRef alias 到 magic effect 的通訊橋。Dialogue condition 可直接用 `GetGlobalValue(CondiExp_PlayerIsDrunk) == 1` 讀取狀態，不需要 Papyrus 呼叫——這是 dialogue INFO condition 能用的最輕量偵測方式。

---

## 五、對 ModForge 的參考價值

| 功能 | 狀態 | 說明 |
|------|------|------|
| MFG API 參數表 | 純參考 | SetModifier/SetPhoneme/SetExpressionOverride 索引與用途已全部文件化於本檔 |
| 三段式 MFG 漸變 pattern | 純參考 | 可在手寫 follower 表情腳本時直接套用 |
| Busy gate GlobalVariable 模式 | 純參考 | 任何有「表情優先序」需求的系統都應採用 |
| Follower dialogue 含表情 override | 需新支援（推斷） | ModForge 目前 DialogueSpec 無 expression field；若要在 INFO 播放時同步驅動 NPC 表情，需在 spec 新增 `expression` 欄位並生成對應 VMAD script |
| 對話條件讀 CE GlobalVariable | 可生成（推斷） | `GetGlobalValue("CondiExp_PlayerIsDrunk")` 可直接用在 dialogue INFO condition；ModForge 若支援 GetGlobalValue condition，即可在 spec 中標「follower 見到玩家喝醉時有特別台詞」 |
| CE 前置依賴 | 純前置參考 | CE 是玩家可選安裝的外部 mod；ModForge 生成的 follower 不應 require CE，但可以「如果 CE 安裝了，condition 自動走 drunk/high 分支」 |

> ⚠️「需新支援」與「可生成」為 survey agent 推斷，未查 ModForge src/。
