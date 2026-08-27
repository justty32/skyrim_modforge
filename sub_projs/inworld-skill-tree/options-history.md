# 可行方案 A–D（決策脈絡，部分已淘汰）

← [README](README.md)｜主線設計 [design-inworld-jcontainers.md](design-inworld-jcontainers.md)

> 方向已定（2026-06-17）：主線走 **方案 A+（Campfire/Frostfall in-world 3D 星樹 + JContainers per-NPC 狀態）**，放棄 CSF UI 路線。下列 A–D 保留為**決策脈絡**：C/D 已淘汰，A 升級為 A+（見主線設計檔），B 的 GLOB 模型被 JFormDB 取代。

## 方案 A：純效果，不開 UI（最簡單，現在就能做）

直接用 `Actor.AddPerk()` 讓 NPC 擁有 CSF 技能樹的某些節點 perk。等級/進度用一套獨立的 GLOB 記錄（Sofia F6 好感度 GLOB 系統是現成藍圖：每個 NPC 一個 GLOB，`setGlobal` + `GetGlobalValue` 管理狀態 —— 見 [sofia-patch](../../../sofia-patch/README.md)）。

- **玩家無法對 NPC 配點**——perk 配置由 mod 作者預先決定或腳本觸發（依任務進度 / 好感度 GLOB 自動升等）。
- **無星座 UI 外觀**，但 perk 效果完整。
- **適合**：NPC 有「職業成長」或「技能解鎖」機制，但玩家不需要直接干預。

## 方案 B：代理選單（轉移模型）—— 複雜但最接近 PoE 管理感

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

## 方案 C：放棄 CSF UI，用 MCM 或對話樹取代

不用 CSF UI，改用 SkyUI MCM 頁面或自製對話選單讓玩家為 NPC 選 perk。後端邏輯不變（GLOB 記等級、AddPerk 套效果），只是放棄星座樹的視覺。

- **最靈活**：可顯示 NPC 名字、自訂 UI 邏輯。
- **需要 SkyUI 依賴**（MCM 路線），或大量對話記錄（對話樹路線）。
- **失去 PoE 星座感**。

## 方案 D：接管控制（Proteus 模型）—— 借原版星座 UI

讓玩家短暫「變成」NPC（交換 Actor 控制權），使原版技能選單的 PlayerRef 對象變成那個 NPC，此時可用原版**星座介面**幫 NPC 花 perk point——繞過「`OpenCustomSkillMenu` 無 Actor 參數」問題的聰明方案。

- 不是讓 CSF 認識 NPC，而是讓玩家暫時占用 NPC 身體。
- **比方案 B 更乾淨**（不必逐一複製 GLOB），但需要完整的 Actor 控制轉移機制。
- **Proteus（Nexus 62985）自帶 native SKSE plugin 支撐**——純 ModForge spec 無法生成 SKSE 層，須依賴或復刻。

<a id="alt-route-inworld-3d-perk-tree"></a>

## 替代路線（待主力機查證，見 wait_user）：in-world 3D 互動天賦樹

Campfire / Frostfall 的天賦樹是**世界內互動物件**：玩家與營火互動後幾顆「星星」3D 物件懸浮在營火上方，準星對準可看浮動說明 + 「是否啟用此 perk」選項——不靠 CSF UI 或 MCM，把 perk 選擇做成世界內互動物件。**對 NPC 版 perk 管理很有參考價值**（可掛在 NPC 身上而非營火）。此即升級為主線的 **方案 A+**；機制細節見主線設計檔。
