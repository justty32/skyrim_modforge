# CODE_MAP — NPC・派系・職業・AI 套件・戰鬥風格

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：NPCs、factions、relationships、classes、combat styles、AI packages（Sandbox/Travel/UseMagic/Follow/Sleep/Patrol/Escort）、outfits、weather/climate（附屬於 NPC 體驗）。

---

## 1. Spec（資料定義）

| 檔案 | 主要型別 |
|-----|---------|
| `src/ModForge.Core/Spec.Actors.cs` | `NpcSpec`（race/class/faction/spells/combatStyle/outfit/package…）, `FactionSpec`, `VendorSpec`, `RelationshipSpec` |
| `src/ModForge.Core/Spec.Magic.cs` | `ClassSpec`（healthWeight/magickaWeight/staminaWeight/skillWeights/teaches）, `CombatStyleSpec`（equipMult* 六欄）|
| `src/ModForge.Core/Spec.Packages.cs` | `PackageSpec`, `PackageScheduleSpec`, `SandboxSpec`, `SleepSpec` |
| `src/ModForge.Core/Spec.Packages.Templates.cs` | `TravelSpec`, `UseMagicSpec`, `PatrolSpec`, `FollowSpec`, `EscortSpec` |
| `src/ModForge.Core/Spec.Weather.cs` | `WeatherSpec`, `ClimateSpec` |

---

## 2. Build Pass 1

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Core/Generator.Build.Actors.cs` | 建 NPC record（level/class/faction/combat-style/spell/perk 組裝，refs pass 2 接）|
| `src/ModForge.Core/Generator.Build.Classes.cs` | 建 Class record（attribute weights + skill training）+ relationship + outfit + wiring |
| `src/ModForge.Core/Generator.Build.Weather.cs` | 建 weather scalar fields（colors/clouds/wind/fog）|
| `src/ModForge.Core/Generator.Build.Climate.cs` | 建 climate scalar fields（timing/sun/moon textures/volatility），weather entries pass 2 接 |

## 3. Build Pass 2

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Core/Generator.Build.Packages.cs` | AI package 資料槽填充 dispatcher（sandbox/sleep/travel/usemagic/patrol/follow/escort）|
| `src/ModForge.Core/Generator.Build.Packages.Advanced.cs` | 複雜套件槽填充：Escort/Patrol/Follow data（location/target/marker 解析）|
| `src/ModForge.Core/Generator.Build.Vendor.cs` | vendor faction config + merchant container + JobMerchantFaction |
| `src/ModForge.Core/PackageTemplates.cs` | vanilla AI package procedure-template FormKey 登錄（Sandbox/Sleep/Travel/…）|

---

## 4. Validate

| 檔案 | 檢查什麼 |
|-----|---------|
| `src/ModForge.Core/Generator.Validate.Npcs.cs` | faction/class/outfit/voice ref；package template/slot integrity；AI-data enum |
| `src/ModForge.Core/Generator.Validate.Weather.cs` | color component 範圍、cloud index、wind/fog、timing monotonicity、weather chance 總和 |

---

## 5. Diagnostics

| 檔案 | dump 哪些 |
|-----|---------|
| `src/ModForge.Cli/Diagnostics.Records.Npc.cs` | NPC class/race/faction/outfit/voice/combat-style/package/perk 詳細欄位 |
| `src/ModForge.Cli/Diagnostics.Records.cs` | 跨類型 record 詳細欄位（含 NPC/Faction/CombatStyle）|
| `src/ModForge.Cli/Diagnostics.Factions.cs` | faction members / vendor config / crime data / relationship |
| `src/ModForge.Cli/Diagnostics.Weather.cs` | sky colors / cloud layers / precipitation / wind / fog / transition timing |

---

## 6. Docs

| 連結 | 內容 |
|-----|-----|
| `docs/SPEC-packages.md` | AI packages + weather/climate 欄位（EN）|
| `docs/zh-TW/SPEC-packages.md` | （zh-TW）|
| `docs/for_agent.md#限制` | NPC 功能性角色要求（race+class+outfit）|
| `docs/lifelike/README.md` | 擬真 NPC 食譜 / Cookbook / FormID 參考 |
