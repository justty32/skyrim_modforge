# 設計模式 + 對 ModForge 的參考價值

← [conditional-expressions](conditional-expressions.md)

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
