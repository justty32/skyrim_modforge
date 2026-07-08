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

原始輸出存 [`../gemini-research/npc-perk-management/csf-npc-perk-ui-mods.md`](../gemini-research/npc-perk-management/csf-npc-perk-ui-mods.md)。

| Mod | Nexus ID | 方法 | 驗證 |
|-----|----------|------|------|
| **Proteus**（formerly Project Proteus） | 62985 | **方案 D**：「接管」NPC 身體控制權，用原版星座介面幫 NPC 花 perk point | ✅ 使用者親自用過 |
| Follower Perk and Spell Manager (FPSM) | 46820 | 清單式 UI（UIExtensions），直接指派 perk 給隨從 | ⚠️ ID 待驗 |
| NPC Perk Tree Management | 122240 | 自訂選單管理 NPC 戰鬥風格/perk | ⚠️ ID 待驗 |
| Skyrim Party Sheet | 111836 | 顯示隨從當前 perk/屬性的「隊伍表」UI | ⚠️ ID 待驗 |
| Be a Leader - CSF | 53051 | 玩家持有「領導」CSF 星座，perk 效果作用於隨從（非直接管理 NPC 樹） | ⚠️ ID 待驗 |

---

## 待調查（阻擋設計拍板的關鍵問題，2026-06-16 提出，早於下方 2026-06-17 方向底定）

> 下列問題提出於方向底定前。方案 A+ 相關的 Campfire/Frostfall 機制問題已全數解掉（見 [design 檔 §五 U1–U5](design-inworld-jcontainers.md)）；方案 B 專屬的問題隨 B 淘汰而作廢，僅保留列示脈絡。

- ~~`RegisterForCustomSkillIncrease` 的觸發時機是否能精確捕捉「玩家在選單裡點下某個節點的瞬間」，還是只在「技能等級提升時」觸發？~~——**已隨方案 B 淘汰作廢**（CSF UI 路線放棄，見上方 2026-06-17 決定）。
- ~~方案 B 的 Papyrus 腳本是否在 ModForge 現有的 fragment / script 生成能力範圍內？~~——**已隨方案 B 淘汰作廢**。
- ~~Campfire/Frostfall in-world 天賦樹的實作機制——待主力機查看~~——**✅ 已解（2026-06-21 原始碼逆向）**，見 [design 檔 §五 U1–U3](design-inworld-jcontainers.md)。
- 其餘 Nexus ID（FPSM/NPC Perk Tree Management/Party Sheet/Be a Leader）人工驗證——**仍未驗，非阻擋項**。

---

## 設計推進（本 sub_proj 的工作面）

**主線設計（2026-06-17 定）→ [design-inworld-jcontainers.md](design-inworld-jcontainers.md)**：方案 A+ ＝ 方案 A 純效果成長 + Campfire/Frostfall in-world 3D 星樹（取代 CSF UI）+ JContainers `JFormDB` per-NPC 狀態（取代 381 GLOB 海）。三者角色、JFormDB 資料模型、「對 NPC 開樹」橋接流程、5 個 unknown（U1–U5，**全數已解**）、ModForge 後端需新增、三期實作建議全在該檔。

**現況（2026-06-21）**：U1–U5 全數解掉；Phase 0（純效果成長）+ Phase 1（玩家版 in-world 樹，零外部 master）+ Phase 3（generator `skillTrees:`）皆已離線落地並 **IN-GAME CONFIRMED**。詳細分期狀態見 [design 檔 §七](design-inworld-jcontainers.md)。

## Open

- **Phase 2：NPC 版橋接尚未實作**——session GLOB + JFormDB + 對 NPC 開樹，unknown 已清零（U1/U2 已解），純粹是還沒動工。見 [design 檔 §三](design-inworld-jcontainers.md) 流程 + [design 檔 §七 Phase 2](design-inworld-jcontainers.md)。
- **Phase 0 實機驗收待你**：施法長技能 + 好感度 gate 翻轉 + CastMagic 條件，需 MO2 裝 JContainers SE——見 [WAIT_USER](../../WAIT_USER.md)。
- **與玩家技能樹 spec 的關係**：本案玩家版 in-world 樹本身就是一條玩家技能樹路線（取代 CSF）；NPC 樹複用同一套 perk/record 生成邏輯，只多 JContainers 橋接層。
