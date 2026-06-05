# CODE_MAP — NPC・派系・職業・AI 套件・戰鬥風格・天氣

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：NPCs、factions、relationships、classes、combat styles、AI packages（Sandbox/Travel/UseMagic/Follow/Sleep/Patrol/Escort）、outfits、weather/climate。

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
| Spec | `Spec.Actors.cs` | `NpcSpec`（race/class/faction/spells/combatStyle/outfit/packages/perks…）|
| Build P1 | `Generator.Build.Actors.cs` | 建 NPC record（level/class/faction/combat-style/spell/perk 組裝）|
| Validate | `Generator.Validate.Npcs.cs` | faction/class/outfit/voice/race ref；package template/slot integrity |
| Diag | `Diagnostics.Records.Npc.cs` | NPC class/race/faction/outfit/voice/combat-style/package/perk 詳細 dump |
| Diag | `Diagnostics.Records.cs` | 跨類型 record 詳細欄位（含 NPC）|

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
| Spec | `Spec.Packages.Templates.cs` | `TravelSpec`, `UseMagicSpec`, `PatrolSpec`, `FollowSpec`, `EscortSpec` |
| Data | `PackageTemplates.cs` | vanilla PACK procedure-template FormKey 登錄 |
| Build P2 | `Generator.Build.Packages.cs` | 資料槽填充 dispatcher（sandbox/sleep/travel/usemagic/patrol/follow/escort）|
| Build P2 | `Generator.Build.Packages.Advanced.cs` | 複雜套件槽：Escort/Patrol/Follow（location/target/marker 解析）|
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
