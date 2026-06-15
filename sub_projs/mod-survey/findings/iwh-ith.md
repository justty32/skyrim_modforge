# I'm Walking Here + I'm Talkin' Here — Finding

## I'm Walking Here（IWH）
Nexus：https://www.nexusmods.com/skyrimspecialedition/mods/27742
版本：1.7（2022-10）
本地解壓：`/tmp/survey_iwh/iwh/`（已清理）
檔案：`SKSE/Plugins/ImWalkinHere.dll` + `ImWalkinHere.toml`

## I'm Talkin' Here（ITH）
Nexus：https://www.nexusmods.com/skyrimspecialedition/mods/93694
版本：2.0（2024-05）
本地解壓：`/tmp/survey_iwh/ith/`（已清理）
檔案：`ImTalkinHere.esp` + `Scripts/ASEImTalkinHere.pex` + `Source/Scripts/ASEImTalkinHere.psc` + `Seq/ImTalkinHere.seq`

---

## 一、這兩個工具做什麼 + 工作原理

### I'm Walking Here（IWH）

IWH 是純 SKSE DLL plugin，**沒有任何 ESP、Papyrus script 或 MCM**。它直接在 C++ 層攔截 Skyrim 的碰撞（collision）更新邏輯，讓特定類型的 NPC 在移動時不會把玩家或彼此推開。

設定檔 `ImWalkinHere.toml` 有四個開關：

```toml
[General]
disableAllyCollision = true        ; 友方 NPC 對玩家不推擠
disableAllySummonCollision = true  ; 召喚物對玩家不推擠
disableDialogueCollision = true    ; 對話中 NPC 對玩家不推擠
disableSummonCollision = true      ; 召喚物互相不推擠
```

所有選項預設 `true`。它沒有辦法被 Papyrus 呼叫，也沒有 API。

IWH 的「友方」（Ally）定義由 Skyrim 引擎的 team/relationship 系統決定，follower 通常屬於友方。

### I'm Talkin' Here（ITH）

ITH 是 ESP + Papyrus quest 架構。核心腳本 `ASEImTalkinHere extends Quest` 掛在一個常駐 quest 上，監聽 SKSE 的 MenuOpen/MenuClose 事件：

```papyrus
Scriptname ASEImTalkinHere extends Quest  Conditional

Bool Property PlayerInDialogue Auto Conditional

Event OnInit()
    RegisterForMenu("Dialogue Menu")
EndEvent

Event OnMenuOpen(String MenuName)
    If MenuName == "Dialogue Menu"
        PlayerInDialogue = True
    EndIf
EndEvent

Event OnMenuClose(String MenuName)
    If MenuName == "Dialogue Menu"
        PlayerInDialogue = False
    EndIf
EndEvent
```

關鍵是 `Conditional` keyword（script 層級和 property 層級都有）：這讓 `PlayerInDialogue` 成為一個可被 **dialogue condition** 直接讀取的 Papyrus property，不需要 GetGlobalValue 或 Papyrus Function call，效能更好。

ITH 的 bark 抑制不是透過腳本 mute NPC 聲音，而是讓 NPC 的 idle dialogue topic 在 condition 上新增一條 `GetScriptVariable(ASEImTalkinHere, PlayerInDialogue) == 0` 的判斷，或由相容 patch 加上同等條件——若玩家在對話中，bark 的 condition 不滿足，自然不播。

ITH 隨附 `Seq/ImTalkinHere.seq`，確保 quest 在遊戲啟動時正確初始化（包括對舊存檔的相容）。

---

## 二、機制詳解

### IWH：碰撞抑制

IWH 運作在 Skyrim 引擎底層（Havok 物理/碰撞更新）。四個 bool 選項分別對應四種對，都是單向影響「被推擠方（玩家/同類）」：

- `disableAllyCollision`：最常用。follower 走路時不再硬推玩家，窄路可以穿越。
- `disableDialogueCollision`：對話時 NPC 停在原地不推擠，解決 forcegreet 後 NPC 滑向玩家問題。
- `disableAllySummonCollision` / `disableSummonCollision`：召喚物相關，狗、精靈不卡路。

**沒有 API**：IWH 沒有 Papyrus function、沒有 RegisterForMenu、沒有任何腳本介面。它是「裝了就全局生效」的 passive plugin。

**設定調整**：只能編輯 `ImWalkinHere.toml` 手動開關，無 MCM。

### ITH：對話期間 bark 抑制

ITH 提供的機制：

1. **`PlayerInDialogue` bool property**（Conditional）：對話選單打開時為 True，關閉後為 False。這是 ITH 對外公開的唯一「API」——其他 mod 的 dialogue condition 可以讀它。
2. **Bark 抑制方式**：不是 mute，而是在 follower/NPC 的 idle chatter topic 的 condition 上加上 `GetScriptVariable(ITH_Quest, PlayerInDialogue) == 0` 的前置條件。ITH 本身帶基本的 vanilla follower patch；自訂 follower 或其他 voiced mod 需要自行 patch 或有對應 compatibility patch。
3. **相容性**：ITH v2.0 標榜支援 vanilla followers、NFF、RDO、Sofia 等。

**Papyrus API 小結：**

```papyrus
; 在 dialogue INFO condition 或 topic condition 裡可用：
; GetScriptVariable(ASEImTalkinHere_Quest, PlayerInDialogue) == 0
; 表示「玩家不在對話中才播 bark」
```

或透過 `Conditional` property 特性，用 `GetProperty`-style condition 讀取（如 GetPropertyValue）。

---

## 三、設計模式：如何在 follower mod 中利用

### IWH — 純被動依賴

IWH 是品質改善層。自家 follower mod 對它的態度是「有裝更好，沒裝也能跑」：

- **場景品質**：forcegreet、home scene、hug scene（IGYH 類）中，如果玩家裝了 IWH，NPC 不會在走位時把玩家推開；沒裝 IWH 的玩家可能偶爾看到 camera 被撞歪。
- **不應依賴**：自家 action/scene 設計仍應搭配 `SetDontMove(true)`、`AI package` 清理、`Scene` lock 等主動手段控制 NPC 站位，不能只靠 IWH。
- **Soft reference 寫法**：如果 follower 腳本需要動態調整站位，應用 `If Game.IsPluginInstalled("ImWalkinHere.esp")` 查一下再做可選行為。

### ITH — Condition Hook

ITH 的最大價值在 dialogue condition：

**推薦做法（無需 patch ITH，只讀 property）：**

```
; 在 follower 的 ambient bark topic 的 condition 區：
GetScriptVariable (ImTalkinHereQuest, PlayerInDialogue) == 0.00
```

如果條件不滿足（玩家在對話中），bark 自然被抑制。

**替代做法（不依賴 ITH 的自家實作）：**

用自家 quest 的 GlobalVariable 或 Papyrus bool property，在 scene/topic 進入時設 `MyMod_PlayerBusy = 1`，離開時清掉；ambient bark 的 condition 加 `GetGlobalValue(MyMod_PlayerBusy) == 0`。原理相同，不需要裝 ITH。這樣相容性最好，也是在玩家沒裝 ITH 時的 fallback。

**兩者共存**：如果裝了 ITH，它和自家 busy global 可以並存——bark condition 寫成 `GetGlobalValue(MyMod_PlayerBusy) == 0 AND GetScriptVariable(...) == 0`（AND 邏輯），更嚴謹。

---

## 四、對 ModForge 的參考價值

| 功能 | 狀態 | 說明 |
|------|------|------|
| IWH 前置宣告 | 純前置參考 | ModForge 不需要生成任何內容讓 IWH 生效；只需在文件/spec 中標注「推薦安裝 IWH 改善場景品質」 |
| ITH `PlayerInDialogue` condition | 可生成（推斷） | 若 ModForge 的 DialogueSpec 支援 condition 欄位，可自動在 ambient bark INFO 上插入 `GetScriptVariable(ITH_Quest, PlayerInDialogue) == 0`；相容性 patch 可按需生成 |
| 自家 PlayerBusy GlobalVariable | 可生成（推斷） | ModForge 若在 follower spec 中產生「ambient commentary」topic，應一同生成一個 PlayerBusy global + 在重要 scene/dialogue 的進出點設置/清除它；這是無需 ITH 的自足 bark 抑制 |
| ITH quest alias 讀取 | 需新支援（推斷） | `GetScriptVariable` 作為 dialogue condition type，需確認 ModForge 的 condition 生成器是否支援此 function type |
| IWH toml 設定 | 無需支援 | 純使用者手動設定，ModForge 不介入 |

> ⚠️「需新支援」與「可生成」為 survey agent 推斷，未查 ModForge src/。
