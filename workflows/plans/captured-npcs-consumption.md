# capturedNpcs[] 消費 — Implementation Plan（Idea #24 擷取器 · ② NPC 外貌）

← [plans](README.md)｜spec：[ingame-scene-export-design.md](../specs/ingame-scene-export-design.md)（共用）｜idea：[#24](../idea/tools/24-ingame-editor.md)｜子專案：[scene-capture-bridge](../../../scene-capture-bridge/README.md)｜姊妹（已落地）：`capturedItems[]`（commit `1bed5dd`）

**Goal:** 讓 scene-capture-bridge DLL `sc cap` 吸到的 `capturedNpcs[]`（活體 actor 的 TESNPC 外貌/身份）能被 ModForge `build` 成一個真的 NPC_ 記錄 + 一個 ACHR placement，讓那個 NPC 出現在世界裡。這是擷取器消費的 ②（① items 已完成）。

> **狀態：Phase 1（T1–T6）IN-GAME 確認 2026-07-11**——實機吸 Mirabelle Ervine → build → 分身在學院庭院原地出現（[landed/npcs-capture](../feature-dev/landed/npcs-capture.md)）；同場逮到 tint value 標度 bug（DLL/TINV 原生 0–100 vs Mutagen 0–1 視圖，spec 定 0–100、Build ÷100）。本檔保留作 Phase 2（烘焙）的界線記錄與映射表出處。探勘結論已固化在下方「地形」節（[terrain](captured-npcs-consumption/terrain.md)），不必重跑。

| 檔案 | 內容 |
|------|------|
| [decisions.md](captured-npcs-consumption/decisions.md) | 最關鍵的裁決（配方現在做 vs 烘焙臉後面做）、設計裁決、Phase 2 烘焙臉界線、開放問題、相依 / 前情 |
| [terrain.md](captured-npcs-consumption/terrain.md) | 地形：facegen 現況、Mutagen 0.53.1 `Npc` 臉部 API、NPC+placement 載體、unique override 路線、其它 |
| [phase1.md](captured-npcs-consumption/phase1.md) | 分階段任務 Phase 1 — TESNPC 配方（T1–T6） |
