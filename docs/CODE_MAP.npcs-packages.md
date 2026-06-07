# CODE_MAP — NPC・派系・職業・AI 套件・戰鬥風格・天氣

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：NPCs、factions、relationships、classes、combat styles、AI packages（Sandbox/Travel/UseMagic/Follow/Sleep/Patrol/Escort）、outfits、weather/climate。

## Lifelike Docs（NPC 深度指南）
→ **說明文件**：[lifelike/README.md](lifelike/README.md)（入口）

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
| `RelationshipAndEslTests.cs` | faction relationship build + ESL flag 行為 |
| `WeatherClimateTests.cs` | weather scalar fields + climate build |

---

---

## NPCs
→ **說明文件**：[for_agent.md § 限制](for_agent.md#limits--be-honest-do-not-over-claim)（race+class+outfit 最低要求）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Actors.cs` | `NpcSpec`（race/class/faction/spells/combatStyle/outfit/packages/perks/**unique/essential/protected**/**items**…）, `NpcItemSpec`（item ref + count）|
| Build P1 | `Generator.Build.Actors.cs` | 建 NPC record（level/class/faction/combat-style/spell/perk 組裝；unique/essential/protected → `NpcConfiguration.Flag`）；**inventory items 在 `WireNpcs` pass 2 填（forward-ref-safe，建 `ContainerEntry`/`ContainerItem`；武器自動裝備、死亡掉落）**；**鐵律：`autoCalcStats` 必須配 `class`——autoCalc 靠 class 算 H/M/S,無 class → ~0 血 → essential NPC 永久倒地(看似死,要 `resurrect`);`BuildNpcs` 偵測到 autoCalc 無 class 會 `Warn`（in-game 踩過 2026-06-07）** |
| Validate | `Generator.Validate.Npcs.cs` | faction/class/outfit/voice/race ref；package template/slot integrity |
| Diag | `Diagnostics.Records.Npc.cs` | NPC class/race/faction/outfit/voice/combat-style/package/perk 詳細 dump |
| Diag | `Diagnostics.Records.cs` | 跨類型 record 詳細欄位（含 NPC）|
| Tests | `NpcTests.cs` | NPC config flag（essential/protected）|

---

## Classes（職業）
→ **說明文件**：[SPEC-dialogue-quests.md § classes](SPEC-dialogue-quests.md#classes-clas)

（源碼見 [CODE_MAP.dialogue-quests.md § Classes](CODE_MAP.dialogue-quests.md#classes職業-clas)）

---

## Factions 派系
→ **說明文件**：[for_agent.md § 限制](for_agent.md#limits--be-honest-do-not-over-claim)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Actors.cs` | `FactionSpec`, `RelationshipSpec` |
| Build P1 | `Generator.Build.Classes.cs` | `BuildRelationships`, `WireRelationships`, `WireOutfits` |
| Validate | `Generator.Validate.Npcs.cs` | faction ref |
| Diag | `Diagnostics.Factions.cs` | faction members / vendor config / crime data / relationship dump |

---

## Combat Styles 戰鬥風格（CSTY）
→ **說明文件**：[for_agent.md § 限制](for_agent.md#limits--be-honest-do-not-over-claim)（combatStyle + spells 搭配說明）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Magic.cs` | `CombatStyleSpec`（equipMult* 六欄 AI 武器偏好分數）|
| Build P1 | `Generator.Build.Actors.cs` | 建 CombatStyle record + 接到 NPC |
| Validate | `Generator.Validate.Npcs.cs` | combatStyle ref |
| Diag | `Diagnostics.Records.cs` | CombatStyle 欄位 dump |

---

## AI Packages（PACK）
→ **說明文件**：[SPEC-packages.md § packages](SPEC-packages.md#packages--ai-packages-what-an-npc-does) · [engine-internals.md § AI Packages](engine-internals.md#ai-packages-are-template-driven)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Packages.cs` | `PackageSpec`, `PackageScheduleSpec`, `SandboxSpec`, `SleepSpec` |
| Spec | `Spec.Packages.Templates.cs` | `TravelSpec`, `UseMagicSpec`, `PatrolSpec`, `FollowSpec`, `EscortSpec`, `SitTargetSpec`, `ActivateSpec`, `EatSpec` |
| Data | `PackageTemplates.cs` | vanilla PACK procedure-template FormKey 登錄（含 `SitTarget`=0x0A9277、`Activate`=0x019B2D、`Eat`=0x019714）|
| Build P2 | `Generator.Build.Packages.cs` | 資料槽填充 dispatcher（sandbox/sleep/travel/usemagic/patrol/follow/escort/**sittarget/activate/eat**）|
| Build P2 | `Generator.Build.Packages.Advanced.cs` | 複雜套件槽：Escort/Patrol/Follow/**SitTarget/Activate/Eat**（SitTarget slot16 SingleRef→家具走位+坐；Activate slot0 SingleRef→物件走位+活化〔lever/door〕；Eat 為 location sandbox-variant，固定 food/chair 搜尋）|
| Build P2 | `Generator.Build.Conditions.cs` | package condition 接線（共用）|
| Validate | `Generator.Validate.Npcs.cs` | package template/slot integrity、AI-data enum |

---

## Weather / Climate（天氣 WTHR / 氣候 CLMT）
→ **說明文件**：[SPEC-packages.md § weathers & climates](SPEC-packages.md#weathers--climates--custom-skies-wthr--weather-cycles-clmt)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Weather.cs` | `WeatherSpec`, `ClimateSpec` |
| Build P1 | `Generator.Build.Weather.cs` | 建 weather scalar fields（colors/clouds/wind/fog）|
| Build P1 | `Generator.Build.Climate.cs` | 建 climate scalar fields（timing/sun/moon/volatility）；weather entries pass 2 接 |
| Validate | `Generator.Validate.Weather.cs` | color 範圍、cloud index、timing monotonicity、chance 總和 |
| Diag | `Diagnostics.Weather.cs` | sky colors / cloud layers / precipitation / wind / fog dump |
