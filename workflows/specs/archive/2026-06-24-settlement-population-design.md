# 設計方案：`settlements:` 聚落量產 macro（人口填充 spec section）

← [specs README](README.md)｜idea [#22 漂泊開拓慢活](../idea/world-building/22-wandering-frontier.md)｜roadmap [mod-survey-gaps 🏘️](../roadmap/mod-survey-gaps.md)｜先例 [`skillTrees:` macro](../../src/ModForge.Core/Spec.SkillTree.cs)

## 目標

一個高階 spec section，把「一座住滿活人、有店家、有作息的聚落」**一鍵展開**成既有低階記錄（NPC / package / faction / placement / container），照 `skillTrees:` 的 **pass-0 macro-expansion** 模式。解決 idea #22 的「聚落量產」與 mod-survey 🏘️ 缺口 #1。**動機**：8 個人口 mod 調查證實每個低階機制都已 landed，缺的只是這層便利 macro——手寫一座 10 人聚落要鋪 ~10 NPC × 4 package + faction + vendor + placement ≈ 上百筆記錄，macro 把它壓成十幾行。

## 設計原則：與 skillTrees 同構

- `Generator.ExpandSettlements(spec)` 在 `Build()` pass-0 把 `spec.Settlements` 展開成 `spec.Npcs` / `spec.Packages` / `spec.Factions` / `spec.Placements` / `spec.Containers` 既有清單，**後面所有 battle-tested build pass 做真正的事**。新建記錄碼極少，零新 record 型別。
- 純資料展開、**無 runtime 腳本**（MVP）——故離線完全可驗，無 .pex 主力機驗證負擔（這是 MVP 選型的關鍵，見下）。

## 兩個正交軸（5 原型的收斂）

調查的 5 種人口原型不是 5 個 mode，是**兩軸的組合**：

| | **靜態 ACHR**（確定性） | **runtime controller**（需 .pex） |
|---|---|---|
| **具名住民**（unique NPC）| CRF 固定住民 ✅ **MVP** | —（具名不需 controller）|
| **leveled / 抽卡**（匿名群眾）| Populated 靜態密度 / prison carrier→LVLN | Immersive Wenches 腳本生怪（count GLOB + spawn script）|

→ **MVP = 左上格**（具名住民 + 靜態 ACHR）：#22 核心（蓋聚落→住滿能工作的活人），全確定性、macro-expand 乾淨、無 in-game 驗證負擔。
→ **右下格**（匿名群眾 controller 填充）＝ Phase 2 `crowd:` 擴充（需 spawn controller .pex，主力機驗證）。
→ **左下格**（靜態 leveled 群眾）＝ Phase 2 輕量：`who: "leveled:<LVLN>"` 直接走既有 LeveledNpc-as-ACHR-base，無 controller。

「店家/服務面」（settlement-expansions 的 per-NPC Vendor faction）與「作息」是**每個住民的屬性**，不是獨立原型——故收進 resident 欄位（`vendor:` / `routine:`）。

## MVP spec 形狀

```yaml
settlements:
  - editorId: RiverwatchVillage
    cell: MyWorld:RiverwatchExterior     # in-spec cell editorId 或 vanilla cell ref（同 placement.cell 解析）
    settlementFaction: RiverwatchFolk     # 選填；空 → 自動建 "<editorId>_Faction"（居民同鄉、互助）
    crimeFaction: ""                       # 選填 → 套到每個居民的 npc.CrimeFaction（城內通行權）
    dailyRoutine:                          # 聚落預設作息，居民未覆寫則繼承
      sleep:  { from: 22, to: 7 }
      work:   { from: 8,  to: 18 }
      # 其餘時段 → sandbox（在 settlement cell 內遊蕩）
    residents:
      - npc: BrelinTheSmith               # ref → 既有 npcs[] 的 NpcSpec（或之後支援 inline）
        home:  BrelinBedRef                # ref → 已擺放的 bed REFR（package 錨點）
        work:  RiverwatchForgeRef          # ref → 已擺放的 forge/工作站 REFR
        spawnAt: BrelinSpawnMarker         # ref → 已擺放的 XMarkerHeading（ACHR 出生點）
        vendor:                            # 選填 → 這個居民變店家
          sellBuyList: Skyrim.esm:0x06CB48 # VendorItemsMisc，或 in-spec FormList
          startHour: 9
          endHour: 18
          gold: 500
      - npc: OldMillieTheCook
        home: MillieBedRef
        spawnAt: MillieSpawnMarker
        routine: { sleep: { from: 21, to: 6 } }   # 覆寫聚落預設
```

**錨點哲學（呼應配方鐵律）**：`home`/`work`/`spawnAt` 都是**已擺放的 REFR/marker 的 editorId**，由使用者在 Godot 編輯器擺好（或手寫 placements[]）。macro **只負責把 package 綁到這些錨點**——絕不憑空生抽象 sandbox（三方印證：純抽象 = NPC 呆站）。這把 macro 跟 [Godot 程序化擺放](../../sub_projs/godot-worldspace-editor/stitching.md#相關gdscript-程序化擺放) 接起來：擺床/攤位時順手給 editorId，macro 自動接成作息。

## 展開規則（→ 既有記錄，附真實欄位）

每個 resident 展開成：

1. **placement**（ACHR）：`Placements.Add(PlacementSpec{ Base=<npc>, Cell=<settlement.cell>, Position=<spawnAt 的位置>, EditorId=<npc>Ref })`。MVP：`spawnAt` 給的是已擺 marker → 取其座標；或允許直接給 position。NPC base → ACHR（既有 `BuildPlacements` 的 `isNpc` 路徑）。
2. **作息 packages**（綁錨點）：依 `dailyRoutine`(+覆寫) 生 2–4 個 `PackageSpec`：
   - sleep → `Template=Sleep`, `Schedule{Hour=from, DurationInMinutes=(to−from)*60}`, `Sleep.Location=<home>`
   - work → `Template=Sandbox`(或 UseFurniture), `Schedule{...}`, `Sandbox.Location=<work>, Radius=小`
   - 其餘 → `Template=Sandbox`, `Sandbox.Location=<spawnAt or settlement center>, Radius=大`（在聚落遊蕩）
   依 `Schedule.Hour` 排序 = vanilla package 優先序。assign 進 `npc.Packages`（既有欄位）。
3. **faction 三件套**：把 `settlementFaction`（自動建 or 引用）加進 `npc.Factions`；`crimeFaction` → `npc.CrimeFaction`；若有 `vendor` → 建 `FactionSpec{ Vendor=VendorSpec{ sellBuyList, startHour, endHour } }`、加進 `npc.Factions`、並建 merchant `Containers.Add(...)` 綁該 vendor faction（同 `Generator.Build.Vendor.cs` 既有路徑）。
4. **settlement faction record**：每聚落一個 `FactionSpec`（無 vendor），居民互設友好（可選 RELA）。

**全部目標欄位都已存在**：`NpcSpec.Packages/Factions/CrimeFaction`、`PackageSpec.Schedule/Sandbox/Sleep.Location`、`FactionSpec.Vendor/VendorSpec`、`PlacementSpec`、`ContainerSpec`、`LeveledNpcSpec`。

## 重用 vs 新建（最小新碼）

| | |
|---|---|
| **重用（零改）** | BuildPlacements（ACHR）、BuildNpcs、Build packages、Build.Vendor（FACT+container）、cell override 解析 |
| **新建** | `Spec.Settlement.cs`（`SettlementSpec`/`ResidentSpec`/`RoutineSpec` POCO）、`Generator.Settlements.cs`（`ExpandSettlements` pass-0）、`Generator.Validate.Settlements.cs`（npc/anchor ref 存在性、vendor 欄位一致）、`ModSpec.Settlements` list + pass-0 hook、example `settlement_spec.json`、SPEC doc、schema |

新碼量與 `skillTrees:`（`Spec.SkillTree.cs` 50 行 + `Generator.SkillTrees.cs` 100 行）同級。

## 依賴缺口

- **`flee` PACK template**（mod-survey 🏘️ #2，未建）：要支援 resident `reaction: flee|fight`（受襲平民逃、守衛迎戰）須先補此 PACK 模板。**MVP 不含**，列為 Phase 2 + 該模板的前置。
- 其餘無前置——所有展開目標皆已 landed。

## MVP scope 切分

- **Phase 1（本設計 MVP）**：`settlements[]` 具名住民 + 靜態 ACHR + 綁錨點作息 + 可選 vendor + faction 三件套。全確定性、離線可驗。
- **Phase 2**：`crowd:` 匿名群眾（leveled 靜態 / controller 動態 spawn，後者需 .pex）；`reaction:`（依 `flee` PACK template）；inline npc；RELA 自動友好網；`routine` 進階（per-weekday、季節）。

## 已拍板決策（2026-06-24，使用者授權代決）

1. **`spawnAt` 兩者都收，marker 優先**：給 marker editorId → 取其座標（貼 Godot 工作流）；也允許直接給 position 當 fallback。
2. **work 作息用 `Sandbox + 小 Radius`**（錨在 work ref），不用 `UseFurniture`——vanilla 工匠多為 sandbox-near-workstation。NPC 是否真的走到工作站 = 主力機實機驗收項（屬「只有使用者能驗」那類，到時我會用白話問結果）。
3. **居民互友 RELA 預設關，`friendlyResidents: true` 才開**——避免大聚落意外全互友。
4. **vendor merchant container 沿用 `Build.Vendor` 現行約定**（不另設後室）。

> 設計即 plan-ready；剩下的不確定都收斂成「實機驗收項」，動工後進 [WAIT_USER](../../WAIT_USER.md)。
