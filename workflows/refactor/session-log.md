# refactor — session log（重構整理）

← [SESSION-LOG hub](../../SESSION-LOG.md)｜拆法見 [STRUCTURE「膨脹即拆」](../../wf/STRUCTURE.md)

**只放本工作流還沒完成的 in-flight / open 狀態**；完成即移除（→ git log）。

---

## 進行中 / open

### 過長方法（判斷題，不是規則）

`src/` 單檔 300 行規則已到頂（只剩 `Generator.Build.Scene.cs` 301 行，刻意不動：為 1 行去拆是為拆而拆）。剩下的面向是**方法長度**，2026-08-27 量到最長的十個：
`ValidateNpcs` 195、`BuildScenes` 179、`BuildQuestAliases` 175、`BuildConditionData` 171、`GenerateQuestFragmentSource` 166、`ValidateWorld` 163、`ExpandLivingNpcs` 162、`WirePerks` 154、`Build` 154、`DumpRecordMagicAiAndText` 141。

要不要拆逐個判斷：只有內部是**多個彼此不相干的階段**（有作者編號過的階段就沿著切，沒有就別硬切）才值得動，不照名次往下刷。拆法與兩個機械坑（`continue` 抽出迴圈後要改 `return null`；具名引數傳給 `Action<>` 委派不成立，改位置引數）沿用 08-27 那輪（commit `8cf02a6`）。驗收：離線測試數與基線相同＋三支護欄 byte-identical（[testing.md](../testing.md)）；基線要在乾淨樹上取。
