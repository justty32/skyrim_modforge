# Story Manager 子系統拆解

> 來源：ModForge `src/ModForge.Core/Generator.Build.StoryManager.cs`、`Generator.Validate.StoryManager.cs`、`Spec.StoryManager.cs`、`StoryManagerEvents.cs`、`Diagnostics.StoryManager.cs`；mod-survey findings `extended-encounters.md`、`immersive-world-encounters.md`；roadmap `mod-survey-gaps.md`。

---

## 內容拆分

- [架構概覽 + Record 欄位結構](sm-subsystem-architecture-records.md) — SM 子系統做什麼 + SMBN/SMQN/SMEN 欄位
- [事件路由 + builder/缺口](sm-subsystem-routing-builder.md) — event 路由機制 + ModForge 現有 SM builder 能力與確認缺口
- [多層巢狀 + 設計模式](sm-subsystem-nesting-patterns.md) — 多層巢狀 SMBN 設計 + 從真實 mod 抽出的 SM 慣用模式
