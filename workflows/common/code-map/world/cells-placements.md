# CODE_MAP.world — Interior Cells・Placements

← [CODE_MAP.world](../CODE_MAP.world.md)

## Interior Cells 室內空間
→ **說明文件**：[SPEC-world.md § cells & placements](../../../../docs/spec/SPEC-world.md#cells--placements--putting-things-in-the-world)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.World.cs` | `CellSpec`（name / template / **`waterHeight`→XCLW / `water`→XCWT / `acousticSpace`→XCAS** / encounterZone / **`music`→MUSC**）|
| Build P1 | `Generator.Build.Cells.cs` | 建 interior cell record；template 環境複製後套用明示水位/水種/聲學空間，確保 spec 覆寫 template（`encounterZone` / `music` 由 pass 2 接線）|
| Validate | `Generator.Validate.World.More.cs` | cell encounterZone / water / acousticSpace ref |

---

## Placements 放置（NPC / Object）
→ **說明文件**：[SPEC-world.md § cells & placements](../../../../docs/spec/SPEC-world.md#cells--placements--putting-things-in-the-world) · [engine-internals.md § Cell GRUP](../../../../docs/engine-internals.md#cell-grup-placement-is-keyed-by-formidgrid)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.World.cs` | `PlacementSpec`（含 `kind:"xmarker"/"xmarkerHeading"` helper；**Scale/InitiallyDisabled/FullLod/NoHavokSettle/EnableParent(`EnableParentSpec`)/Lock(`LockSpec`)/Ownership(`OwnershipSpec`)/Count**）, `LinkedRefSpec`, `Vec3` |
| Spec | `Spec.Primitives.cs` | **`PrimitiveSpec`**（type＝box(預設)/sphere/portalBox/none 或生數字、bounds＝**全尺寸非半徑**、color/opacity＝**CK 編輯器外觀，遊戲內無效**）＋ `PlacementSpec.Primitive`／`PlacementSpec.CollisionLayer`（皆 **REFR only**）。**觸發箱的 vanilla 配方**（2026-09-05 對 Skyrim.esm 1.6.1170 實掃 13,668 筆 XPRM）：base 指 ACTIVATOR（`defaultActivateSelfTRIG`=`Skyrim.esm:0x048AC0` 486 筆／`defaultSetStageTRIG`=`0x033F50` 279 筆／`WordWallTrigger`=`0x05095E` 46 筆 Sphere），Box＋(204,76,51)＋0.15，**不設 collisionLayer**（觸發行為來自 ACTI base，XPRM 只說體積多大）|
| Spec | `Spec.MapMarkers.cs` | `MapMarkerSpec`（editorId/name/worldspace/position/`type`(MarkerType)/`flags`(Visible/CanTravelTo/ShowAllIsHidden)）|
| Build P2 | `Generator.Build.Placements.cs` | 室內/室外/vanilla-override 放置，position/rotation，persistent flag，cell 錨定；**`kind:xmarker/xmarkerHeading` → 空 base 自動填 `Skyrim.esm:0x3B`/`0x34` STAT + 強制 persistent**（quest-target 錨點）；**base 是 in-spec HAZD（或 `kind:"hazard"`）→ 建 `PlacedHazard`（`.Hazard` 而非 `.Base`）**；**Scale(XSCL) / InitiallyDisabled(0x800) / FullLod(0x10000＝VisibleWhenDistant，遠景 full model；REFR only) / NoHavokSettle(0x20000000＝DontHavokSettle，跳過 cell 載入時的 havok settle pass，手擺的雜物才不會被彈飛；vanilla Skyrim.esm 有 3791 個 REFR 帶它；REFR only，ACHR 不寫) / Lock(XLOC) / Ownership(XOWN) / Count(ItemCount XCNT)**；`ParseLockLevel` helper 在 `Generator.Helpers.cs`；**EnableParent(XESP) 只丟進 `deferredEnableParentWires`**（同一迴圈裡 eager 解析只看得到列表中「更早」的 placement——`WireDeferredEnableParents` 在 `Generator.Build.PlacementRefs.cs` 才真正接線，見下） |
| Build P2 | `Generator.Build.MapMarkers.cs` | **`BuildMapMarkers`**：每筆 → MapMarker static（`0x10`）上的 `PlacedObject` + XMRK `MapMarker`(Name/Type/Flags)，放進 worldspace **持久 TopCell**（`WorldspacePersistentCell`），registered 進 formKeyByEd 故可被 `forced:` alias 抓 |
| Build P2 | `Generator.Build.PlacementRefs.cs` | linked-ref 對 + teleport-door XTEL 接線（deferred）；**`WireDeferredEnableParents`**：placement `enableParent.ref` 的 XESP 接線，跑在 `BuildPlacements`＋`BuildReferences` 都完成後——解得到**指向列表後面的 placement**與 `references[]` label（原本 eager 解析在同一迴圈當場做，只看得到「更早」的 placement，解不到就照樣印 `unresolved` 警告、不生 XESP）|
| Build P2 | `Generator.Build.ExteriorCells.cs` | 室外 worldspace cell group tree（block/sub-block 按 grid 坐標）；**`WorldspaceOverride` 加性帶上 master 持久 TopCell（`CopyCellEnv`、不重述 vanilla ref）否則 vanilla 地圖標記全消失+大地圖空白**；**帶 master 的 `Name`(FULL)**——override 少了它就把該 worldspace 的名字**清空**（本地地圖/存檔顯示不出「Whiterun」；`MasterCache` 已 provision 英文 STRINGS 所以現在讀得到，Tamriel 的硬編碼只留作 fallback）；**`WorldspacePersistentCell`** 回 worldspace 持久 cell 給地圖標記 |
| Build P2 | `Generator.Build.Primitives.cs` | **`MakePrimitive`**（唯一產 `PlacedPrimitive` 的地方，navCuts 與 `placements[].primitive` 共用）＋ **`BuildPrimitive`**（spec→記錄：缺 bounds 警告不寫、sphere 三軸自 X 補齊、`ParsePrimitiveType` 收名稱/生數字，不認得回退 Box）。寫入點在 `Generator.Build.Placements.Record.cs` 的 `ApplyPlacementAttributes`（ACHR 上給 primitive/collisionLayer 會**出聲警告**而非靜默丟掉——不然就是一個永遠不觸發又查不出原因的箱子）|
| Validate | `Generator.Validate.Primitives.cs` | **`ValidatePrimitives`**：primitive/collisionLayer 不可放 ACHR、bounds 必填且三軸為正（sphere 可只給 X）、type 名稱、opacity 0..1、base 不可空 |
| Validate | `Generator.Validate.World.cs` | linked-ref target、teleport pairs、worldspace boundary |
| Diag | `Diagnostics.Dump.World.cs` | placements / cells / linked-refs / navmesh dump |
