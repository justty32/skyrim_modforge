# Pandora Behaviour Engine+ — 行為引擎（2026 標準）

← [action-system 中樞](README.md)｜idea-pipeline 視角見 [animation/linux-workflow-modforge.md §6.0](../../../workflows/idea/asset-pipelines/animation/linux-workflow-modforge.md)

> 2026 現況：**Nemesis / FNIS 已 legacy**，社群普遍改用 Pandora。它是 behavior graph 的「patch + 生成」層——OAR/DAR 的條件式動畫替換、MCO/movesets 等動作 mod 都需要先有一個 patch 過的 behavior 基底，這一步現在由 Pandora 做。

## 是什麼
- **開源（GPL v3）、.NET 8、官方跨平台**（Windows/Linux/macOS）的 behavior 引擎，取代 Nemesis/FNIS。需 **.NET 8 Desktop Runtime**。
- 讀 **所有 Nemesis-format mod + 多數 FNIS mod**，外加**原生 Pandora XML 格式**（`replace/insert/append/loose`，XPath 式 `#xxxx\path\…` targeting）。
- 輸出 **Havok 2010 binary `.hkx`** behavior graph（用內建 HKX2E），功能上等同 Nemesis/FNIS 產物。OAR/DAR 在 runtime 疊在其上。

## 怎麼跑（Linux / headless — 關鍵但未完全確定）
- 官方雖列 Linux/macOS，但**「只有 Windows 被充分測試」**；團隊建議的 Linux 路徑是 **Proton-wrap 自帶的 Windows self-contained build**。
- 有自動化啟動參數：`--auto_run`、`--auto_close`、`-o/--output <dir>`、`--tesv:"<gamedir>"`、`--skyrim_debug64`——但它們驅動的是 **GUI app**；**真正無 GUI 的 headless 仍是未解的 feature request（GH issue #114）**。
- ⇒ 自動化跑很可能仍需一個 display（實體或 `xvfb-run` 之類）。**此結論待在 Manjaro 主力機實機驗證**（native dotnet vs Proton、能否無顯示跑）。

## 對 ModForge 的整合
- **可行模型＝shell-out**（與 ModForge 已驅動 Papyrus compiler / xLODGen 同套）：ModForge 產出 records + OAR config + `.hkx` 資產後，呼叫 `Pandora --auto_run --auto_close -o "<out>" --tesv:"<gamedir>"` 生成 behavior 基底。
- **不能 library 嵌入**：Pandora 以 app 出貨，其 .NET plugin API（`IEngineConfigurationPlugin` + `plugin.json`）明示「不穩定、隨時破壞」——「兩者都是 .NET」**不等於**能 NuGet reference。
- roadmap spike：① 確認 native-Linux vs Proton；② 自動化跑能否 displayless。見 [roadmap](../../../workflows/roadmap.md) 的 ModForge↔Pandora 項。

## 在動作系統中的定位
四層動畫/動作堆疊：**Pandora（behavior patch 基底）→ OAR（條件式替換，見 [oar-replacer-guide](oar-replacer-guide.md)）→ .hkx 動畫資產（[havok-blender](../../../workflows/idea/asset-pipelines/animation/havok-blender.md) 線）→ 觸發層（IDLE/perk/SKSE）**。即將調查的整套新動作系統（MCO/movesets 等）會疊在這個基底上。
