# Directional Movement Keys（DMK）

← [action-system 中樞](../README.md)

> **Layer 3（動畫選擇輔助）**。Viny 的 SKSE plugin（2026）。把**玩家/NPC 的移動方向 + 玩家相機方向**寫成 graph variable，**專供 OAR 條件**與招式系統使用。Dtry Keys 的後繼，但不用 magic effect（無 magic-effect 卡頓問題）。

## 是什麼
- 暴露兩組方向 graph variable（8 向 + 中性）：
  - **移動方向**（`DirecionalCycleMoveset` / graph var）：`0=無移動 1=前 2=前右 3=右 4=後右 5=後 6=後左 7=左 8=前左`
  - **相機方向**（`CameraMovementCMF`）：同一套 0–8 編碼
- 額外 action key bool 變數（`DMKLeftShift / DMKLeftAlt / DMKQ / DMKE / DMKZ / DMKX`，鍵盤＋手把映射）。
- SKSE menu 一鍵 convert + 重啟即可，不必手動轉換；支援非 WASD 移動鍵。隨裝隨卸安全。
- **實檔驗證**：DMK 透過 [BDI](behavior-data-injector.md) 注入這些變數——隨附 `SKSE/Plugins/BehaviorDataInjector/DirecionalMovement_BDI.json`，內含 `DirecionalCycleMoveset`/`CameraMovementCMF`(kInt) + 6 個 `DMK*`(kBool)，`projectPath:"Actors"`。即「DMK = DLL + 一份 BDI config」，無 esp。
- 若已用 CMF（含同功能）就不需要這支；DMK 是其升級版（多了 NPC + 相機）。

## 為什麼重要
- 現代招式系統大量靠「**方向 + 動畫**」做派生（方向重擊、八向 moveset、閃避方向）。DMK 把方向變成**可被 OAR 條件比較的乾淨整數**，是 movesets 與 OAR 之間的標準輸入源。
- 與本批次的 [BFCO](bfco.md) 方向重擊（`BFCO_PowerAttackA/B/L/R`）、DMCO 閃避同屬「方向驅動」這一類。

## 對 ModForge（直接相關）
- **不可生成**（SKSE DLL），但**它暴露的變數是 ModForge OAR 生成器的一等公民輸入**：
  - ModForge 生 OAR submod 時，condition 可直接 `CompareValues` 比對 `DirecionalCycleMoveset == 3`（右）等 → 生成「八向待機/移動/招式」動畫包**完全可確定生成**。
  - 這把「方向式動畫包」從手刻 OAR config 變成 ModForge 模板可量產的東西（如：給 follower 一套八向移動動畫）。
- 記入 OAR 生成器的「已知可用 graph variable 清單」（連同 BFCO 的 `BFCO_iAttackVariants`）。
- 原始碼：https://github.com/vinymayan/Direction-Movement
