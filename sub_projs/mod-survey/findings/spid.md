# SPID：Spell Perk Item Distributor 深挖 Finding

- 版本：7.3.0（本地解壓：`Spell Perk Item Distributor-36869-7-3-0-1778353486`）
- 作者：powerofthree
- Nexus：[36869](https://www.nexusmods.com/skyrimspecialedition/mods/36869)
- 類型：SKSE plugin（`.dll`），SSE / AE 各有一份

---

## 內容拆分

- [做什麼 + _DISTR.ini 語法 + Type 全集表](spid-overview-syntax.md) — 工作原理、命名慣例、通用行格式/欄位/NONE/RecordID、12 種 type 全集
- [Filter：String / Form / Level](spid-filters-string-form-level.md) — StringFilters(第2欄) / FormFilters(第3欄) / LevelFilters(第4欄)
- [Filter：Traits / Chance / 關係](spid-filters-traits-chance.md) — Traits(第5欄) / Chance(第7欄) / 多 filter 之間關係
- [真實 ini 範例](spid-examples.md) — 附中文解釋的實例
- [對 ModForge + 與 KID/SkyPatcher 分工](spid-modforge-and-split.md) — 可生成輸出、需新支援項、現有能力、三者分工

## 參考來源

- Nexus 文章「SPID: The Complete Reference」：`https://www.nexusmods.com/skyrimspecialedition/articles/6617`（Nexus 會員限定）
- Nexus 文章「How To Use」：`https://www.nexusmods.com/skyrimspecialedition/articles/4022`
- aqxaromods 鏡像 v6.6.2 文件：`https://aqxaromods.com/skyrim-special-edition/utilities-skyrimse/12728-spell-perk-item-distributor-spid-v662.html`
- moddingskyrim.com SPID and KID 比較：`https://moddingskyrim.com/spid-and-kid/`
- 本地真實 ini 範例：`ImGladYoureHere_DISTR.ini`、`nwsFF_*_DISTR.ini`
