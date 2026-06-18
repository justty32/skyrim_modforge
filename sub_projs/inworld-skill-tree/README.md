# In-world 技能樹（玩家 + NPC，Idea #20 sub_proj）

← [idea #20](../../workflows/idea/inworld-skill-tree.md)｜[ideas 索引](../../workflows/idea/ideas.md)

**一句話**：用 Campfire/Frostfall 的 **in-world 3D 星樹**做 PoE-like 技能樹的 ModForge 生成路線——**玩家與 NPC 通用**。玩家版＝Campfire 原生模式（現成，全域 GLOB）；NPC 版＝加 JContainers per-NPC 狀態橋接。**開樹入口不綁營火**——可做成瞄準法術（施在誰開誰的樹）、自訂活化物（石頭/樹/祭壇）或物品。

**發想來源**：討論 CSF PoE-like 玩家技能 spec 時，冒出「NPC 也能用 CSF 的 perk 嗎？能否對 NPC 開啟 CSF 介面？」的問題。
**核心問題**：perk 的 **效果層** vs **UI 層**，兩件事可行性截然不同——效果層 100% 可行，UI 層原生不支援，方案差異全在如何補 UI 層。

> **方向已定（2026-06-17）：放棄 CSF UI 路線。** CSF 的 `OpenCustomSkillMenu` 無 Actor 參數、且需玩家端 native dll + Scaleform JSON——不適合 NPC、也不貼合 ModForge 純 record 生成。**主線改走 Campfire/Frostfall 的 in-world 3D 星樹（方案 A+）+ JContainers per-NPC 狀態**。完整設計 → **[design-inworld-jcontainers.md](design-inworld-jcontainers.md)**。方案 A–D 的決策脈絡（C/D 已淘汰）→ [options-history.md](options-history.md)。

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

曾評估四案 A（純效果）/ B（代理選單）/ C（MCM·對話樹）/ D（接管控制 Proteus 模型）+ in-world 3D 天賦樹替代路線。**主線已定為方案 A+**（A 純效果 + Campfire/Frostfall in-world 星樹 + JContainers，見上方主線設計檔）；C/D 已淘汰。各案完整描述與取捨脈絡 → [options-history.md](options-history.md)。

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
- **下一步離線設計**：Phase 0（純效果成長）+ Phase 1（玩家版 in-world 樹）的 spec 欄位構想 + 觸發載體（法術/活化物/物品）spec 形狀——這兩階零/少 unknown，可在公司先細化 ModForge spec 對接。
- **與玩家技能樹 spec 的關係**：本案玩家版 in-world 樹本身就是一條玩家技能樹路線（取代 CSF）；NPC 樹複用同一套 perk/record 生成邏輯，只多 JContainers 橋接層。
