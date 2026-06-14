# Behavior Data Injector（BDI）+ Universal Support

← [action-system 中樞](../README.md)

> **Layer 2（行為資料注入）**。Maxsu/Dropkicker 的 SKSE plugin，**ModForge 角度最關鍵的一支**：用 **config 檔**往 behavior project 注入自訂 graph variable 與 animation event，**完全免 Nemesis/behavior patch**。107 mod 依賴。

## 是什麼
- **Graph Variable 注入**：可加 `Int` / `Bool` / `Float` 三型 graph variable 進任意 behavior project。注入的變數與 vanilla 原生變數**完全等價**——可用 vanilla Papyrus 函式 get/set，也可當 **OAR / condition 函式**的比較對象。
  - 例：`Stagger Direction Fix` 用它加方向變數；doodlum 舉例「poise health」變數可塞給任何 actor，免大量 behavior patch。
- **Animation Event 注入**：加新 animation event 進 behavior project，之後可由**動畫註釋（annotation）**觸發。
  - 例：[Payload Interpreter](payload-interpreter.md) 就是靠 BDI 注入的事件運作。
- **Universal Support 版**（doodlum）：AE/VR port + PDB。SSE 用戶用原版即可；這版主要補 AE/VR。

## Config 格式（**已從實檔驗證**，v0.13）
- 放在 `Data/SKSE/Plugins/BehaviorDataInjector/<任意名>_BDI.json`（檔名隨意，慣例 `<Mod>_BDI.json`；該資料夾下的所有 json 都會被讀）。
- 內容是一個 **flat JSON array**，每個 entry：
  ```json
  [
    { "projectPath": "Actors", "type": "kInt",  "name": "MyVar",   "value": 0 },
    { "projectPath": "actors\\Character", "type": "kBool", "name": "MyFlag", "value": true },
    { "projectPath": "Actors\\Horse", "type": "kFloat", "name": "MyNum", "value": 1.5 },
    { "projectPath": "Actors", "type": "kEvent", "name": "MyEvent" }
  ]
  ```
  - `projectPath` — behavior project（`Actors` = 人形主圖；`actors\\Character`、`Actors\\Horse` 等子 project；反斜線需 escape，大小寫寬鬆）。
  - `type` — `kInt` / `kBool` / `kFloat` / `kEvent`（事件型**省略 `value`**）。
  - `name` — 變數/事件名；`value` — 初始預設值。
- 實例：DMK 的 `DirecionalMovement_BDI.json`（8 個變數，見 [DMK](directional-movement-keys.md)）、BFCO 的 `BFCO_BDI.json`（`BFCO_ComboLocked`/`BFCO_LastAttack`/`BFCO_NextNormal`/`BFCO_NextPower`）。
- 解壓樣本在 `sub_projs/game-data/mods/action-system/`（gitignored）。

## 為什麼對 ModForge 是金礦
- BDI 把「behavior graph 的可編程狀態」從**改不得的 binary**降為**寫 config 檔**——這正是 ModForge 主場。
- 它讓一條乾淨的管線成立：**ModForge 生 BDI config（加 graph var / event）→ 動畫用 annotation 設那些 var → OAR 用那些 var 當條件選動畫**。整條除了 .hkx 本體外**全是可確定生成的文字**。
- 典型 ModForge 用例：給 follower/NPC 加一個自訂狀態變數（如「戰意」「好感階段」），動畫與 OAR 條件都讀它——免任何 behavior patch、免 esp script。

## 對 ModForge — 待辦
- **新增 BDI config 生成器**（roadmap 候選）：輸入 `{projectPath, variables:[{name,type,default}], events:[name]}` → 輸出上述 flat JSON array。**格式已驗證**（見上），實作幾乎零風險。
- 與 ModForge 既有 CTDA/graph-variable 條件支援銜接：注入的變數可直接被 OAR config（ModForge 已能生）引用。
