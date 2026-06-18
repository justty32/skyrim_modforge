# FLST 工廠模式

> 為 ModForge（JSON spec → `.esp` 生成器）整理的 FormList 設計模式筆記。Record 名 / 函式名 / Mutagen 型別名留英文；散文繁中。
> 主要來源：spellforge.md finding、missives.md finding、`Generator.Build.Lists.cs`、`Generator.Build.Lists.Wire.cs`、`Spec.Items.cs`。

---

## 內容拆分

- [概覽 + 索引對齊模式](flst-factory-overview-index-aligned.md) — FLST as 資料池、Spellforge 風格索引對齊
- [分類容器 + 其他模式](flst-factory-container-other.md) — Missives 風格 HasForm、FLST as SPID target、FLM runtime 追加
- [ModForge builder + 未覆蓋模式](flst-factory-modforge-builder.md) — 現有能力（有 code 為據）+ 未覆蓋模式
