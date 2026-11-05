# CODE_MAP.world — Leveled Lists・Containers・Recipes・Encounter Zones・Vendors

← [CODE_MAP.world](../CODE_MAP.world.md)

## Leveled Lists 等級列表 + FormList（FLST）
→ **說明文件**：[SPEC-worldspaces.md § leveled lists & containers](../../../../docs/spec/SPEC-worldspaces.md#leveled-lists--containers) · [SPEC-worldspaces.md § formLists](../../../../docs/spec/SPEC-worldspaces.md#formlists--flst)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `LeveledItemSpec`, `LeveledNpcSpec`, `FormListSpec`（editorId + items 任意 record ref）|
| Build P1 | `Generator.Build.Lists.cs` | 建 leveled-item / leveled-NPC list record（chance-none flags）；`BuildFormLists`（空 FLST，items pass 2）|
| Build P2 | `Generator.Build.Lists.Wire.cs` | entry FormLink 接線；`WireFormLists`（items → `FormLink<ISkyrimMajorRecordGetter>`，順序保留）|
| Validate | `Generator.Validate.Items.cs` | leveled list entries ref |
| Validate | `Generator.Validate.World.cs` / `.cs` | FLST editorId 唯一 + 每個 item ref 可解 |

FLST 用途：當吃 list 的 condition 的 param（`GetItemCount`/`GetEquipped`/`GetIsVoiceType`/`GetInWorldspace` 都收 FormList）、keyword/穿著清單、或任何要「一組 form」的地方。**`GetIsInList` 在 Mutagen 0.49 無對應 ConditionData——FLST 走既有 `*OrList` param，不是獨立函式。**

---

## Containers 容器
→ **說明文件**：[SPEC-worldspaces.md § leveled lists & containers](../../../../docs/spec/SPEC-worldspaces.md#leveled-lists--containers)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `ContainerSpec`（model + items + counts）|
| Build P1 | `Generator.Build.Lists.cs` | container 基本欄位 + model (MODL) |
| Build P2 | `Generator.Build.Lists.Wire.cs` | container item FormLink 接線 |
| Validate | `Generator.Validate.Items.More.cs` | container contents ref |

---

## Recipes 合成配方（COBJ）
→ **說明文件**：[SPEC-items.md § recipes](../../../../docs/spec/SPEC-items.md#recipes-crafting--cobj)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `RecipeSpec`（workbench / components / result）|
| Build | `Generator.Recipes.cs` | COBJ 建立（workbench dispatch + component 接線）|
| Validate | `Generator.Validate.Items.More.cs` | workbench keyword、component ref、output ref |
| Diag | `Diagnostics.Recipes.cs` | workbench / components / output dump |

---

## Encounter Zones 遭遇區域
→ **說明文件**：[SPEC-worldspaces.md § encounter zones](../../../../docs/spec/SPEC-worldspaces.md#encounter-zones--leveled-actor-spawns--populating-an-area-with-scaled-enemies)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.World.cs` | `EncounterZoneSpec`（level range / respawn / owner）|
| Build P1 | `Generator.Build.Lists.cs` | `BuildEncounterZones` 入口 |
| Build P2 | `Generator.Build.Lists.Wire.cs` | `WireEncounterZones` / `WireCellZones` |
| Validate | `Generator.Validate.World.More.cs` | encounter zone / owner ref |
| Diag | `Diagnostics.Encounters.cs` | level range / respawn / owner / location dump |

---

## Vendors 商人
→ **說明文件**：[SPEC-worldspaces.md § vendors](../../../../docs/spec/SPEC-worldspaces.md#vendors--merchants--a-working-shopkeeper)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Actors.cs` | `VendorSpec`（hours / buy-stolen / sell-list / chest）|
| Build P2 | `Generator.Build.Vendor.cs` | vendor faction config + merchant container + JobMerchantFaction 指派 |
| Validate | `Generator.Validate.Npcs.cs` | faction ref、vendor chest ref |
| Diag | `Diagnostics.Factions.cs` | vendor config / crime data dump |
