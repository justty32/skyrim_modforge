# Idea #20：CSF NPC 技能樹（對 NPC 開啟 / NPC 擁有 CSF 技能）

← [ideas 索引](ideas.md)

**發想來源**：討論 CSF PoE-like 玩家技能 spec 時，冒出「NPC 也能用 CSF 的 perk 嗎？能否對 NPC 開啟 CSF 介面？」的問題。
**核心問題**：CSF 的 perk 效果層 vs UI 層，兩件事可行性截然不同。

---

## 技術可行性

### NPC 擁有 CSF perk（效果層）✅ 完全可行

CSF 的 perk 就是普通 PERK record，跟原版 perk 沒有任何差異。`Actor.AddPerk()` 對任何 Actor（包括 NPC）有效，perk 的傷害加成、entry-point、ability 被動都會正常作用。

結論：**NPC 擁有 CSF 技能樹的 perk，效果層 100% 可行，無需任何特殊處理。**

### 對 NPC 開啟 CSF 介面（UI 層）❌ 原生不支援

CSF v3 Papyrus API：

```papyrus
void OpenCustomSkillMenu(string asSkillId)   ; 無 Actor 參數
```

`OpenCustomSkillMenu` 沒有 Actor 參數——它開的是**玩家**的技能選單。Skyrim 的技能選單系統本身就是玩家導向的，不存在「對指定 NPC 開技能選單」的原生機制。

CSF 追蹤的 GLOB（level/ratio/legendary）是**全域值**（GlobalVariable），不是 per-actor 的——同一個技能樹只有一份等級，不能同時描述玩家等級 30、NPC A 等級 5、NPC B 等級 12。

---

## 可行方案

### 方案 A：純效果，不開 UI（最簡單）

直接用 `Actor.AddPerk()` 讓 NPC 擁有 CSF 技能樹的某些節點 perk。等級/進度用一套獨立的 GLOB 記錄（Sofia F6 好感度 GLOB 系統是現成藍圖：每個 NPC 一個 GLOB，`setGlobal` + `GetGlobalValue` 管理狀態）。

- **玩家無法對 NPC 配點**——perk 配置由 mod 作者預先決定或腳本觸發（依任務進度 / 好感度 GLOB 自動升等）。
- **無星座 UI 外觀**，但 perk 效果完整。
- **適合**：NPC 有「職業成長」或「技能解鎖」機制，但玩家不需要直接干預。

### 方案 B：代理選單（轉移模型）—— 複雜但最接近 PoE 管理感

為每個 NPC 各備一套 GLOB（`NPC_X_SkillLevel` / `NPC_X_SkillRatio` / `NPC_X_SkillLegendary`）。玩家打開 NPC 的「技能管理」對話選項時，Papyrus 腳本：

1. 把 NPC 的 GLOB 值複製到 CSF 技能的全域 GLOB
2. 呼叫 `OpenCustomSkillMenu(skillId)` 開選單（此時選單顯示的是玩家的那份等級，內容實為 NPC 的）
3. 用 `RegisterForCustomSkillIncrease` / `OnCustomSkillIncrease` 監聽玩家在選單裡點了什麼 perk
4. 選單關閉後，把新值複製回 NPC 的 GLOB，並同步 `AddPerk` / `RemovePerk` 給 NPC

**限制**：
- 選單標題仍顯示玩家名字，無法顯示「正在配置 Sofia 的技能」。
- 每次只能操作一個 NPC（GLOB 全域衝突）。
- 需要較複雜的 Papyrus 腳本（或 Papyrus fragment 組合）——ModForge 現有能力能否生成這套腳本？待查。
- `RegisterForCustomSkillIncrease` 是 extension event（`CustomSkills_FormExt.psc`）——需確認觸發時機足夠精確。

**GLOB 規模**：127 NPC × 1 技能 × 3 GLOB = 381 GLOB records，技術可行但重。

### 方案 C：放棄 CSF UI，用 MCM 或對話樹取代

不用 CSF UI，改用 SkyUI MCM 頁面或自製對話選單讓玩家為 NPC 選 perk。後端邏輯不變（GLOB 記等級、AddPerk 套效果），只是放棄星座樹的視覺。

- **最靈活**：可顯示 NPC 名字、自訂 UI 邏輯。
- **需要 SkyUI 依賴**（MCM 路線），或大量對話記錄（對話樹路線）。
- **失去 PoE 星座感**。

---

## 社群現有 mod 調查（2026-06-16，Gemini 搜尋，⚠️ Nexus ID 未人工驗證）

原始輸出存 `sub_projs/gemini-research/csf-npc-perk-ui-mods.md`。

**主要發現：**

| Mod | Nexus ID（待驗） | 方法 |
|-----|-----------------|------|
| **Proteus**（formerly Project Proteus） | 62985 ✅ | **最接近**：「接管」NPC 身體控制權，此時可用原版**星座介面**幫 NPC 花 perk point——繞過「OpenCustomSkillMenu 無 Actor 參數」問題的聰明方案 |
| Follower Perk and Spell Manager (FPSM) | 46820 | 清單式 UI（UIExtensions），直接指派 perk 給隨從 |
| NPC Perk Tree Management | 122240 | 自訂選單管理 NPC 戰鬥風格/perk |
| Skyrim Party Sheet | 111836 | 顯示隨從當前 perk/屬性的「隊伍表」UI |
| Be a Leader - CSF | 53051 | 玩家持有「領導」CSF 星座，perk 效果作用於隨從（非直接管理 NPC 樹） |

**Proteus 方案的技術含義**：它不是讓 CSF 認識 NPC，而是讓玩家短暫「變成」NPC（交換 Actor 控制），使原版技能選單的 PlayerRef 對象變成那個 NPC。這可視為**方案 D**（接管控制方案）——比方案 B（代理 GLOB 轉移）更乾淨，但需要完整的 Actor 控制轉移機制（Proteus 自己有 native SKSE plugin 支撐）。

## 限制 / 待調查

- `RegisterForCustomSkillIncrease` 的觸發時機是否能精確捕捉「玩家在選單裡點下某個節點的瞬間」，還是只在「技能等級提升時」觸發？——**待查（需主力機測試 CSF v3 event semantics）**。
- 方案 B 的 Papyrus 腳本是否在 ModForge 現有的 fragment / script 生成能力範圍內？——**待查**。
- **Proteus (Nexus 62985) ✅ 已驗**：使用者親自用過，確認功能如描述（Actor 控制轉移 + 原版技能選單）。其餘 Nexus ID 待人工驗證。

---

## 下一步

- 若只要 **NPC 靠技能成長解鎖 perk 效果**（不需 UI）→ 方案 A 現在就能做，沿用 Sofia F6 GLOB 藍圖。
- 若要 **玩家管理 NPC 技能的 PoE 感 UI** → 需先確認 `RegisterForCustomSkillIncrease` 的 event 語意（方案 B 的核心技術問題），再決定是否值得實作。
- **與 CSF PoE spec 的關係**：玩家技能樹 spec 先走，NPC 技能可複用同一套 perk/GLOB 生成邏輯，差別只在 UI 層是否接 CSF 選單。
