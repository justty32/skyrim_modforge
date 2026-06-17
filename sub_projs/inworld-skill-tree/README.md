# In-world 技能樹（玩家 + NPC，Idea #20 sub_proj）

← [idea #20](../../workflows/idea/inworld-skill-tree.md)｜[ideas 索引](../../workflows/idea/ideas.md)

**一句話**：用 Campfire/Frostfall 的 **in-world 3D 星樹**做 PoE-like 技能樹的 ModForge 生成路線——**玩家與 NPC 通用**。玩家版＝Campfire 原生模式（現成，全域 GLOB）；NPC 版＝加 JContainers per-NPC 狀態橋接，把同一套配點體驗延伸到隨從。

**發想來源**：討論 CSF PoE-like 玩家技能 spec 時，冒出「NPC 也能用 CSF 的 perk 嗎？能否對 NPC 開啟 CSF 介面？」的問題。
**核心問題**：perk 的 **效果層** vs **UI 層**，兩件事可行性截然不同——效果層 100% 可行，UI 層原生不支援，方案差異全在如何補 UI 層。

> **方向已定（2026-06-17）：放棄 CSF UI 路線。** CSF 的 `OpenCustomSkillMenu` 無 Actor 參數、且需玩家端 native dll + Scaleform JSON——不適合 NPC、也不貼合 ModForge 純 record 生成。**主線改走 Campfire/Frostfall 的 in-world 3D 星樹（方案 A+）+ JContainers per-NPC 狀態**。完整設計 → **[design-inworld-jcontainers.md](design-inworld-jcontainers.md)**。下方方案 A–D 保留為決策脈絡（C/D 已淘汰）。

> 體量大了再移成獨立 repo，對接方式不變（沿用 ModForge spec）。

---

## 技術可行性

### NPC 擁有 CSF perk（效果層）✅ 完全可行

CSF 的 perk 就是普通 PERK record，跟原版 perk 沒有任何差異。`Actor.AddPerk()` 對任何 Actor（包括 NPC）有效，perk 的傷害加成、entry-point、ability 被動都會正常作用。

**結論：NPC 擁有 CSF 技能樹的 perk，效果層 100% 可行，無需任何特殊處理。**

### 對 NPC 開啟 CSF 介面（UI 層）❌ 原生不支援

CSF v3 Papyrus API：

```papyrus
void OpenCustomSkillMenu(string asSkillId)   ; 無 Actor 參數
```

`OpenCustomSkillMenu` 沒有 Actor 參數——它開的是**玩家**的技能選單。Skyrim 的技能選單系統本身就是玩家導向的，不存在「對指定 NPC 開技能選單」的原生機制。

CSF 追蹤的 GLOB（level/ratio/legendary）是**全域值**（GlobalVariable），不是 per-actor 的——同一個技能樹只有一份等級，不能同時描述玩家等級 30、NPC A 等級 5、NPC B 等級 12。

---

## 可行方案

### 方案 A：純效果，不開 UI（最簡單，現在就能做）

直接用 `Actor.AddPerk()` 讓 NPC 擁有 CSF 技能樹的某些節點 perk。等級/進度用一套獨立的 GLOB 記錄（Sofia F6 好感度 GLOB 系統是現成藍圖：每個 NPC 一個 GLOB，`setGlobal` + `GetGlobalValue` 管理狀態 —— 見 [sofia-patch](../sofia-patch/README.md)）。

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

### 方案 D：接管控制（Proteus 模型）—— 借原版星座 UI

讓玩家短暫「變成」NPC（交換 Actor 控制權），使原版技能選單的 PlayerRef 對象變成那個 NPC，此時可用原版**星座介面**幫 NPC 花 perk point——繞過「`OpenCustomSkillMenu` 無 Actor 參數」問題的聰明方案。

- 不是讓 CSF 認識 NPC，而是讓玩家暫時占用 NPC 身體。
- **比方案 B 更乾淨**（不必逐一複製 GLOB），但需要完整的 Actor 控制轉移機制。
- **Proteus（Nexus 62985）自帶 native SKSE plugin 支撐**——純 ModForge spec 無法生成 SKSE 層，須依賴或復刻。

### 替代路線（待主力機查證，見 wait_user）：in-world 3D 互動天賦樹

Campfire / Frostfall 的天賦樹是**世界內互動物件**：玩家與營火互動後幾顆「星星」3D 物件懸浮在營火上方，準星對準可看浮動說明 + 「是否啟用此 perk」選項——不靠 CSF UI 或 MCM，把 perk 選擇做成世界內互動物件。**對 NPC 版 perk 管理很有參考價值**（可掛在 NPC 身上而非營火）。機制細節（3D mesh + collision raycasting + 對話選項，還是另一套）待主力機確認 → 補回本檔。

---

## 社群現有 mod 調查（2026-06-16，Gemini 搜尋）

原始輸出存 [`../gemini-research/csf-npc-perk-ui-mods.md`](../gemini-research/csf-npc-perk-ui-mods.md)。

| Mod | Nexus ID | 方法 | 驗證 |
|-----|----------|------|------|
| **Proteus**（formerly Project Proteus） | 62985 | **方案 D**：「接管」NPC 身體控制權，用原版星座介面幫 NPC 花 perk point | ✅ 使用者親自用過 |
| Follower Perk and Spell Manager (FPSM) | 46820 | 清單式 UI（UIExtensions），直接指派 perk 給隨從 | ⚠️ ID 待驗 |
| NPC Perk Tree Management | 122240 | 自訂選單管理 NPC 戰鬥風格/perk | ⚠️ ID 待驗 |
| Skyrim Party Sheet | 111836 | 顯示隨從當前 perk/屬性的「隊伍表」UI | ⚠️ ID 待驗 |
| Be a Leader - CSF | 53051 | 玩家持有「領導」CSF 星座，perk 效果作用於隨從（非直接管理 NPC 樹） | ⚠️ ID 待驗 |

---

## 待調查（阻擋設計拍板的關鍵問題）

- `RegisterForCustomSkillIncrease` 的觸發時機是否能精確捕捉「玩家在選單裡點下某個節點的瞬間」，還是只在「技能等級提升時」觸發？——**待查（需主力機測試 CSF v3 event semantics）**，是方案 B 的核心技術風險。
- 方案 B 的 Papyrus 腳本是否在 ModForge 現有的 fragment / script 生成能力範圍內？——**待查**。
- Campfire/Frostfall in-world 天賦樹的實作機制——**待主力機查看**（[wait_user](../../WAIT_USER.md)）。
- 其餘 Nexus ID（FPSM/NPC Perk Tree Management/Party Sheet/Be a Leader）人工驗證。

---

## 設計推進（本 sub_proj 的工作面）

**主線設計（2026-06-17 定）→ [design-inworld-jcontainers.md](design-inworld-jcontainers.md)**：方案 A+ ＝ 方案 A 純效果成長 + Campfire/Frostfall in-world 3D 星樹（取代 CSF UI）+ JContainers `JFormDB` per-NPC 狀態（取代 381 GLOB 海）。三者角色、JFormDB 資料模型、「對 NPC 開樹」橋接流程、5 個待驗 unknown（U1–U5）、ModForge 後端需新增、三期實作建議全在該檔。

**離線可先推進**：Phase 0（純效果成長 MVP，零 unknown）的資料模型 + spec 對接細化。
**待主力機 / code pass**：U1（對任意 ref 開樹）、U2（session GLOB 隔離）、U4（generator 能力）、U5（JFormDB 生成）。

## Open

- **U1–U5 待驗**（見 [design 檔 §五](design-inworld-jcontainers.md)）——多需主力機讀 `Campfire.bsa` / `Frostfall.bsa` 原始碼，或 code pass `src/`。
- **下一步離線設計**：Phase 0（純效果成長）+ Phase 1（玩家版 in-world 樹）的 spec 欄位構想——這兩階零/少 unknown，可在公司先細化 ModForge spec 對接。
- **與玩家技能樹 spec 的關係**：本案玩家版 in-world 樹本身就是一條玩家技能樹路線（取代 CSF）；NPC 樹複用同一套 perk/record 生成邏輯，只多 JContainers 橋接層。
