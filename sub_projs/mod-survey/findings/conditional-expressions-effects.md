# 各 Effect 機制詳解

← [conditional-expressions](conditional-expressions.md)

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

