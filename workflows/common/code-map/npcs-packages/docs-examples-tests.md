# CODE_MAP.npcs-packages — Lifelike Docs・Examples・Tests

← [CODE_MAP.npcs-packages](../CODE_MAP.npcs-packages.md)

## Lifelike Docs（NPC 深度指南）
→ **說明文件**：[lifelike/README.md](../../../../docs/lifelike/README.md)（入口）

這組文件是 NPC / package 功能的深度知識庫，獨立於 SPEC-*.md 之外。修改 NPC 或 AI package 相關功能時，需同步評估這裡是否需要更新。

| 檔案 | 內容 |
|-----|-----|
| `docs/lifelike/README.md` | 入口索引 |
| `docs/lifelike/cookbook-index.md` | Cookbook 目錄 |
| `docs/lifelike/cookbook-npc-basics.md` | NPC 基礎食譜 |
| `docs/lifelike/cookbook-followers.md` | 隨從食譜 |
| `docs/lifelike/cookbook-social-quest.md` | 社交 / 任務食譜 |
| `docs/lifelike/cookbook-magic.md` | 魔法 NPC 食譜 |
| `docs/lifelike/cookbook-world-items.md` | 世界物件食譜 |
| `docs/lifelike/cookbook-advanced.md` | 進階食譜 |
| `docs/lifelike/formid-reference.md` | 原版 FormID 速查 |
| `docs/lifelike/gotchas.md` | 常見陷阱 |
| `docs/lifelike/cheatsheets.md` | 速查表 |

（zh-TW 對應版本在 `docs/zh-TW/lifelike/`，最低優先級，有明確需要才更新。）

---

## Examples

| 檔案 | 對應功能 |
|-----|---------|
| `examples/lifelike_npc_spec.json` | 擬真 NPC（race + class + outfit + packages）|
| `examples/npc-inventory.json` | NPC 庫存（攜帶武器/金幣/藥水；武器自動裝備、可被劫/loot）|
| `examples/follower_hireable_spec.json` | 可招募隨從（hireable 模式）|
| `examples/follower_paid_spec.json` | 付費隨從 |
| `examples/follower_vanilla_spec.json` | vanilla 風格隨從 |
| `examples/class_spec.json` | 自訂職業（CLAS）|
| `examples/combat_spec.json` | 戰鬥風格（CSTY）|
| `examples/package_spec.json` | Sandbox AI package |
| `examples/package2_spec.json` | Travel package |
| `examples/package3_spec.json` | UseMagic package |
| `examples/package4_spec.json` | 其他 package 變體 |
| `examples/escort_spec.json` | Escort package |
| `examples/radiant_package_spec.json` | **#2 radiant 演出 package：`alias:`/`aliasLoc:` target/location 指向 ownerQuest 的 alias（travel→aliasLoc:Dungeon、escort target→alias:VIP）** |
| `examples/follow_spec.json` | Follow package |
| `examples/patrol_spec.json` | Patrol package |
| `examples/scene-sit-performance.json` | SitTarget package（NPC 走到家具並坐下；scene Package action 驅動的演出 beat）|
| `examples/package-activate.json` | Activate package（NPC 走到 ref 並活化〔lever/door/activator〕）|
| `examples/package-eat.json` | Eat package（location sandbox-variant：NPC 找食物+椅子坐下吃）|
| `examples/usemagic_spec.json` | UseMagic package（施法 AI）|
| `examples/weather_spec.json` | 自訂天氣 + 氣候 |
| `examples/scripts/MFFollowerFollow.psc` | 隨從 Follow 行為腳本 |
| `examples/scripts/MFFollowerTrade.psc` | 隨從交易腳本 |
| `examples/scripts/MFFollowerWait.psc` | 隨從待命腳本 |
| `examples/scripts/MFHireFollowerSetup.psc` | 招募設置腳本 |
| `examples/scripts/MFHirePaidDismiss.psc` | 付費解散腳本 |
| `examples/scripts/MFHirePaidRecruit.psc` | 付費招募腳本 |
| `examples/scripts/MFHireVanillaRecruit.psc` | vanilla 招募腳本 |

---

## Tests

| 測試檔案 | 涵蓋 |
|---------|-----|
| `PackageTests.cs` | AI package 資料槽填充（所有 template 變體）|
| `PackageAliasTargetTests.cs` | **#2 alias target/location：Travel/Sleep/Activate/Escort 的 alias:/aliasLoc: → PackageTargetAlias / LocationFallback(AliasForReference\|AliasForLocation) + index；validate（無 ownerQuest / 未知 alias / external ownerQuest）** |
| `RelationshipAndEslTests.cs` | faction relationship build + ESL flag 行為 + **masterless 防呆**（`PluginIo.Write` 對零外部 ref 的 esp 補 Skyrim.esm master；有 ref 不重複）|
| `WeatherClimateTests.cs` | weather scalar fields + climate build |
