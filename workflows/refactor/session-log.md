# refactor — session log（重構整理）

← [SESSION-LOG hub](../../SESSION-LOG.md)｜拆法見 [DEV-GUIDE「膨脹即拆」](../../DEV-GUIDE.md)

**只放本工作流還沒完成的 in-flight / open 狀態**；完成即移除（→ git log）。

---

## 進行中 / open

### `src/` 拆檔分層 + hub 解耦 — Batch 0/1 完成，Batch 2 未開始

計畫全文：[src-layout-plan.md](src-layout-plan.md)。2026-08-13 於離線機 Windows：

- ✅ **Batch 0**（`793615b`）：`scripts/golden-hash.sh` 護欄，用法見 [testing.md](../testing.md)。
- ✅ **Batch 1**（`1e638d1`）：345 個純 rename，`src/`＋`tests/` 全部進領域資料夾。build 乾淨、1122 測試綠、golden hash 197/197 不變。
- ⏸ **Batch 2 起未動**：拆 hub 檔（`Spec.cs` 加 partial → 各領域；`BuildContext` 的 54 個 TIER-C 欄位下放；`Program.cs` 命令表；`Generator.Validate.cs` 收尾 4 個自由函式）。**`Generator.Build.cs` 刻意不動**，理由在計畫 Batch 2 第 5 點。

**⚠ 兩件跨機／後續要注意的：**

- **這兩個 commit 還沒 push。** 母 repo 的 submodule 指標也還沒 bump。另一台若有未推的 `src/` 改動，會撞上這次的巨型 rename——先合再繼續。
- **文檔裡有 5 條既有死連結**（`src/ModForge.Cli/Build.cs`、`Generator.Build.SceneNpcRoles.cs`、`SceneImport.cs`、`StoryManagerProbe.cs`、`StoryManagerProbeTests.cs`），指向從來不存在或早已改名的檔。不是本次造成，屬文檔面向，另開一次處理。
