# Custom Skills Framework (CSF) 技術調查

← [mod-survey](../README.md)｜[mod-survey index](../index.md)｜姊妹資料夾：[custom-skill-tree-guide](../custom-skill-tree-guide/README.md)（實作指南）

> 調查對象：Exit-9B 的 **Custom Skills Framework**（Nexus 41780）與其上層自訂技能樹案例（VIGILANT、GLENMORIL、Unarmored Defense）。
> 目的：弄懂 CSF 如何運作、自訂技能樹如何定義，評估 ModForge 未來「生成 CSF 設定」的可行性。
> 慣例：散文用繁體中文，code / JSON key / API 名稱保留 English。
> 參考：GitHub wiki <https://github.com/Exit-9B/CustomSkills/wiki>、JSON Schema `docs/schema/{CustomSkill,skill,defs}.json`，以及本機解壓的封存實檔。

---

## 內容拆分

- [CSF 是什麼 + 架構細節](concept-architecture.md) — 兩代設定格式斷層、JSON/Papyrus/Keyword/在地化
- [案例研究 + 相關性](cases-relevance.md) — VIGILANT / GLENMORIL 案例 + 對 ModForge 的相關性
- [Constellations schema](constellations-schema.md) — 現代 `X.json` 逐欄 schema + `SKILLS.json` 特例
- [Constellations 接線](constellations-wiring.md) — SKSE plugin + Papyrus 接線、在地化、意義修正
