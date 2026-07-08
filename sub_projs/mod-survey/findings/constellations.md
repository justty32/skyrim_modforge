# Constellations — Additional Player Skills 技術調查

> 調查對象：**Constellations - Additional Player Skills**（Nexus 117352，v1.0.2）
> 本機解壓：`~/skyrim_mods/unzip/Constellations-117352-1-0-2-1730665883/`
> 目的：理解 Constellations 的技術架構，與 CSF 做比較，評估對 ModForge 技能樹生成路線的影響。
> 慣例：散文用繁體中文，config 欄位 / record type / API 名稱 / FormId 保留 English。
> 姊妹文件：[custom-skills-framework/README.md](../custom-skills-framework/README.md)（CSF 框架深挖）、[custom-skill-tree-guide/README.md](../custom-skill-tree-guide/README.md)（實作指南）

---

## 內容拆分

- [做什麼 + 架構概覽](constellations-overview.md) — 架構組成
- [Config：JSON 設定](constellations-config-json.md) — `SKSE/Plugins/CustomSkills/` JSON 完整語法
- [Config：esp / toml / Papyrus / 在地化](constellations-config-esp-papyrus.md) — esp Record 需求、ActorValueData toml、Papyrus 接線、在地化
- [vs CSF + 對生成路線的影響](constellations-vs-csf-and-modforge.md) — 兩代格式對比、Constellations≠獨立路線、生成兩層次、契合點
- [對 ModForge 的評估](constellations-modforge-eval.md) — 可生成性、需新增支援、推薦 spec 欄位、一句話總結
