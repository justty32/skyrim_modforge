# CODE_MAP.world — In-world Skill Trees・Populated Settlements

← [CODE_MAP.world](../CODE_MAP.world.md)

## In-world Skill Trees 技能樹（Idea #20）
→ **說明文件**：[SPEC-world-macros.md § in-world skill trees](../../../../docs/spec/SPEC-world-macros.md#in-world-skill-trees-skilltrees) · memory `inworld-skill-tree-standalone-confirmed`

零外部依賴的可點 in-world 養成樹。**macro-expansion** 架構：在 `Build()` pass-0 把高階 `skillTrees:` 展開成既有低階記錄（globals / node+line ACTI / placements / `scripts:` 的 MFSkillNode 掛載），重用所有既有 pass。IN-GAME CONFIRMED 2026-06-21（手刻版）。MVP = 垂直線性鏈。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.SkillTree.cs` | `SkillTreeSpec`（editorId/name/cell/origin/spacing/pointsGlobal/startingPoints/nodeModel/lineModel）+ `SkillNodeSpec`（editorId/name/ability）|
| Spec | `Spec.cs` | `ModSpec.SkillTrees` + `SkillTreesExpanded` 守衛旗標 |
| Expand P0 | `Generator.SkillTrees.cs` | **`ExpandSkillTrees`**：每 tree → points GLOB + 每 node 的 rank GLOB / star ACTI / placement（垂直堆疊 origin+i*spacing）/ MFSkillNode script-attach（ability/rank/points/name + i>0 的 prereq+downLine）；每邊 → line ACTI + placement（中點、rot 90/0/180、scale=spacing/65）。常數 `SkillNodeScript`/`DefaultSkillNodeModel`/`DefaultSkillLineModel` |
| Build | `Generator.Build.cs` | pass-0 呼叫 `ExpandSkillTrees(spec)`（在 `new BuildContext` 前）|
| Validate | `Generator.Validate.SkillTrees.cs` | `ValidateSkillTrees`：tree/node editorId 唯一、cell/name/ability 必填、ability CheckRef、spacing>0 |
| Papyrus | `assets/papyrus/MFSkillNode.psc` | 節點行為（extends ObjectReference）：OnActivate gate（prereq rank + points）→ AddSpell + PlayAnimation("OwnedWild") + downLine "Unlock" + 扣點；OnLoad 重播亮起（持久）。嵌入 CLI、`package` 在有 skillTree 時 `ShipEmbeddedPex("MFSkillNode.pex")` |
| Art | `examples/assets/skilltree/` | Campfire 星/線 nif（loose 打包，非 master）+ 9 個 vanilla 貼圖；作者經 spec `assets` 帶上 |

---

## Populated Settlements 聚落人口（Idea #22）
→ **說明文件**：[SPEC-world-macros.md § populated settlements](../../../../docs/spec/SPEC-world-macros.md#populated-settlements-settlements)

把「住滿活人的聚落」一鍵展開成既有低階記錄的 **macro**（同 skillTrees 架構，`Build()` pass-0）。MVP = 具名住民 + 靜態 ACHR + 綁錨點作息 + 可選 vendor + faction 三件套；**零新 record 型別、零 runtime 腳本**，純資料展開、離線完全可驗。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Settlement.cs` | `SettlementSpec`（editorId/cell/settlementFaction/crimeFaction/friendlyResidents/dailyRoutine/residents）+ `ResidentSpec`（npc/home/work/spawnAt/spawnPosition/vendor/routine）+ `RoutineSpec`（sleep/work）+ `RoutineWindowSpec`（from/to）+ `SettlementVendorSpec`（sellBuyList/notSellBuyList/startHour/endHour/gold）|
| Spec | `Spec.cs` | `ModSpec.Settlements` + `SettlementsExpanded` 守衛旗標 |
| Expand P0 | `Generator.Settlements.cs` | **`ExpandSettlements`**：每聚落 → settlement FACT（空則自建）；每住民 → ACHR placement（spawn marker 座標 / spawnPosition fallback）+ Sleep/Work/Wander package（綁 home/work/spawnAt 錨點，schedule 帶 wrap-midnight 時長）+ npc.Packages（排程 by hour、wander 最後）+ faction 三件套 + 可選 vendor FACT/chest（gold）/JobMerchantFaction；`friendlyResidents` → 兩兩 Friend RELA。常數 `SandboxTemplateRef`/`SleepTemplateRef`/`GoldRef`/`JobMerchantFactionRef` |
| Build | `Generator.Build.cs` | pass-0 呼叫 `ExpandSettlements(spec)`（在 `ExpandSkillTrees` 後、`new BuildContext` 前）|
| Validate | `Generator.Validate.Settlements.cs` | `ValidateSettlements`：settlement/resident id 唯一、cell 必填、residents 非空、npc 必為 in-spec npcs[]、需 spawnAt 或 spawnPosition、sleep window 需 home 錨、home/work/spawnAt 須為 placement/external、vendor 時數 0..24/gold≥0、routine 時數合法 |
| **錨點解析修** | `Generator.Build.Packages.cs` | `ApplySandboxData`/`ApplySleepData` 的 location slot 改 **deferred**（加進 `deferredLocationWires`，仿 Travel/Escort）——原本 eager 解析時 placement 還沒註冊 → in-spec 錨點一律 fallback NearSelf；現在 placement loop 後才解析，in-spec marker/bed 錨點正確解析（且自動被強制 persistent）|
