# 做什麼 + MFG API 完整參考

← [conditional-expressions](conditional-expressions.md)

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

