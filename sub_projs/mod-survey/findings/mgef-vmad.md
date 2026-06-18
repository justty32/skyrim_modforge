# Script-attached MGEF（VMAD on Magic Effect）

> 為 ModForge（JSON spec → `.esp` 生成器）整理的機制筆記。Record 名 / 函式名 / Mutagen 型別名留英文；散文繁中。
> 主要來源：arrowblock.md finding、`Generator.Build.Scripts.cs`、`Generator.Build.Magic.cs`、`Spec.Magic.cs`。

---

## 內容拆分

- [機制 + ModForge 支援狀況](mgef-vmad-mechanism-support.md) — VMAD 結構、ActiveEffect 繼承、Archetype 關係、AttachScripts、缺口確認
- [property 表 + 設計模式 + 評估](mgef-vmad-properties-patterns.md) — 內建 property 表、Pattern A~C、對 ModForge 的評估
