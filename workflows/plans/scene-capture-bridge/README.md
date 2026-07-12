# 遊戲內編輯器（scene-capture-bridge）— Implementation Plan

← [plans](../README.md)｜spec：[ingame-scene-export-design.md](../../specs/ingame-scene-export-design.md)（共用）｜idea：[#24](../../idea/tools/24-ingame-editor.md)（核心宗旨見其頂部 2026-07-10 重述）｜子專案：[scene-capture-bridge](../../../sub_projs/scene-capture-bridge/README.md)

**（2026-07-10 細摳收斂後從頭重寫；取代同日早先的 M6–M10 版本，舊 M 編號在「路線圖」表保留對照）**

**Goal:** 把 Skyrim 自身變成 Creation Kit 的第一步。遊戲內 plugin＝**薄記錄器**（施法/快捷鍵 → 記座標/標籤/意圖 → 吐 json）；實際的生成工作全歸 ModForge / AI agent。MVP＝**統一 marker 系統**：玩家在世界裡標「這裡放什麼」，agent 讀 json 拿座標＋標籤去 authoring。

## 本 plan 的內容（膨脹後拆檔，2026-07-11）

| 檔 | 內容 |
|---|---|
| **本檔 README** | 現況導航：Goal、已定調的裁決、已落地地基、路線圖 |
| [phases.md](phases.md) | P1–P6 的 Task 層級 + backlog 加碼一輪的已落地實作記錄（歷史實作細節，大而少動） |
| [backlog.md](backlog.md) | 「之後再做」——仍未做（**活躍成長區**：新想法都記這；做完的搬進 phases.md） |
| [appendix.md](appendix.md) | 附錄・細摳記錄（需求原文，凍結參考）＋ 驗證清單 |

---

## 已定調的裁決（全部出自使用者，2026-07-10 細摳）

| 議題 | 裁決 |
|---|---|
| **MVP 骨幹** | **統一 marker 流**：指向性法術打 marker → 互動/GUI 改標籤 → 匯出 json → ModForge 讓 AI 在 marker 上擺東西。NPC/生物/leveled encounter/動態物件**初版全走這套**，額外操作之後再說 |
| 地形高度 | 遊戲內**不做**雕刻（runtime 無法變形 LAND，也不太需要）。走離線路（Godot editor / PNG heightmap → ModForge，生成端已落地）。遊戲內只放標註 marker 給 agent 下指令 |
| leveled encounter | 要。走 marker 流（`placeatme` 會立即抽選，故不生成、只記座標＋標籤，base 由 agent 在 spec 指定 LVLN） |
| area / trigger / event | 以後再說 |
| 靜態物件富路徑 | UX 已定稿（見 [appendix・細摳②](appendix.md)）：新增/修改/刪除、輪廓確認流、numpad 編輯模式。**排在 marker MVP 之後** |
| 動態物件富路徑 | 同靜態＋物理凍結（見 [appendix・細摳③](appendix.md)）。初版先走 marker 流 |
| 刪除的狀態模型 | 記憶體（session）＋面板 `Adopt disabled refs` 掃描鍵（明示優於推導） |
| 擦到外部 mod 的 ref | 允許，面板醒目標示「會讓 patch 依賴 X.esp」 |
| 真刪除語意 | 刪掉自己新增的＝無痕跡（不進 removals[]、不留登記簿、檢視法術不顯示） |
| **PROTEUS 相關** | **凍結**——使用者將重新規劃，本 plan 一律不涵蓋、不假設 |

## 已落地的地基（實機確認 2026-07-10）

- `SceneCaptureBridge.dll`：clang-cl 跨編譯、遊戲載入正常；F10 匯出、F1 ImGui 面板（SKSE Menu Framework 3，軟相依）。
- **vanilla diff**：authored ref 跳過、玩家 `PlaceAtMe` 的 emit。Bannered Mare 717 refs 全 pre-existing、`placeatme` 兩個後 placements=2。
- 座標契約全條目結案；`scene.json` → `build` → patch esp 整鏈閉環。
- 生成端既有能力：`placements[]`（含 ACHR）、`removals[]`、mapMarker/hazard/keyword、npcRoles macro、heightmap→LAND、programmatic navmesh。

## 路線圖

| Phase | 內容 | 舊編號對照 | 狀態 |
|---|---|---|---|
| **P1** | **統一 marker 系統（MVP 骨幹）** | 原 M9（升格） | **[phases.md](phases.md) 的 Task 層級主體** |
| P2 | 靜態物件富路徑：刪除／新增／修改編輯模式 | 原 M6／M7a／M7c | UX 定稿（[appendix・細摳②](appendix.md)），等 P1 |
| P3 | 動態物件富路徑（物理凍結）＋檢視法術 | 原 M7d | UX 定稿（[appendix・細摳③](appendix.md)），等 P2 |
| P4 | 範圍吸取 | 原 M8 | 概要 |
| 後排 | role tag（原 M10；其 PROTEUS 路徑凍結）、navmesh 記點、area/trigger/event、滴管移動既有 ref（原 M7b，等 override 契約） | — | — |

**進度速覽**：P1–P6 主線 2026-07-11 實機全過（使用者「測試起來都ok」）——生成端＋採集橋整鏈閉環。細節見 [phases.md](phases.md)；未做項與新想法見 [backlog.md](backlog.md)。
