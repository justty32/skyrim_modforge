# 共通踩坑（跨工作流）

← [INDEX](../../INDEX.md)｜各工作流專屬踩坑：[feature-dev/gotchas](../feature-dev/gotchas.md) · [investigation/gotchas](../investigation/gotchas.md)

引擎行為 / 開發流程層級的坑，不專屬任一工作流，任何人都可能撞到。`[[...]]` 連 Claude memory。

## 哪類坑記哪裡（三處 gotchas 歸類）

| 坑的性質 | 記/查這裡 |
|---------|----------|
| 引擎行為 / 開發流程，不專屬任一工作流 | **common/gotchas**（本檔）|
| 開發具體功能（SM/scene/dialogue/npc/voice…）+ 外部工具內部開發聯動（Papyrus 編譯、Wine path）| [feature-dev/gotchas](../feature-dev/gotchas.md) |
| 逆向 vanilla 記錄、覆寫 vanilla WRLD/CELL 的解碼坑 | [investigation/gotchas](../investigation/gotchas.md) |

---

- **存檔已固化**：GLOB value / scene `.seq` 只是初值，既有存檔保留 runtime 值。
- **worktree 並行** [[feature-swarm-branches]]：worktree 一律從 **stale base** 分出（持續性 harness 行為）；先離線解碼 vanilla 再下精確施工單（agent 不負責猜）、分配互斥檔案領域；整合用 cherry-pick + keep-both（同名 test class 用 `--ours` 重貼）。
