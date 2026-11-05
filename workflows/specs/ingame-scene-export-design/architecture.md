# 遊戲內場景匯出 — 歷史架構：四個元件 + 一座橋（含滴管取樣）

← [ingame-scene-export-design](../ingame-scene-export-design.md)（入口）

## 歷史架構：四個元件 + 一座橋

> ⚠️ 本節是 2026-07-08 的舊架構。PROTEUS 路線已被 `sc capp` 直接擷取取代；placement-controller `.pex` 也已被 scene-capture-bridge 的 C++ `Palette`／`Preview` 路徑取代。圖與逐項說明只保留演進脈絡，不代表 current blocker。

```
  [遊戲內 runtime]                          [build-time]
  ┌─────────────────────────┐
  │ ① placement-controller  │  喝瓶→定位→落地（照 Tundra，與 settlements P2 共用）
  │    .pex（隨附 reusable） │
  ├─────────────────────────┤
  │ ② PROTEUS（外部，消費）  │  玩家 → 穩定可引用的 clone NPC（facegen 白賺）
  ├─────────────────────────┤        ┌──────────── scene.json ───────────┐
  │ ③ 採集橋 SKSE DLL        │───────▶│  placements[] / npcRefs[] /        │
  │    （net-new 子專案）    │  匯出   │  mapMarkers[] / hazards[] /        │
  │  走訪 cell、讀 transform │        │  tags[] / npcRoles[]               │
  │  /enable、收 §B 語意標記 │        └────────────────┬───────────────────┘
  │  /記 clone ActorRef+身份 │                         │
  └─────────────────────────┘                         ▼
                                        ┌─────────────────────────────────┐
                                        │ ④ ModForge（本命，幾乎全已具備） │
                                        │  scene.json → 既有生成鏈 → patch │
                                        └─────────────────────────────────┘
```

- **① placement-controller `.pex`**：irreducibly bespoke Papyrus（Tundra §3.3 的 `aaaFortMainQuestScript` 等價）。ModForge 有「隨附 reusable `.pex` + `scriptAttach`」先例（MCM-Helper/dispatcher/PapyrusUtil）。**與 settlements P2 的 `buildables:` controller 是同一支，兩線合流**——本 spec 不重複設計，指向 settlements P2 design。**定位軸**：Tundra 已有三軸**旋轉**（`MODE_ROTATE_X/Y/Z`+`ChangeRotationAxis`）+**距離**（`MODE_DISTANCE`，沿視線推拉），**但無縮放、也無自由三軸位移**——本控制器要**多加兩個 mode**：`MODE_SCALE`（`SetScale`）、`MODE_TRANSLATE`（`GetPositionX/Y/Z`+delta→`SetPosition`，沿軸精確 nudge，如貼齊牆面），皆 vanilla Papyrus、共用 `AXIS`+Plus/Minus 輸入。ModForge 側 `PlacementSpec.Position`+`Rotation`+`Scale`(XSCL) **均已支援**，scene.json 契約已帶（見下 `placements[]`），故**匯出/生成零改動**，只差 controller 補這兩個 mode。⚠️ **XSCL 不作用於 actor**（`Spec.World.cs:43`）→ 縮放只對靜物/家具/光，拓印 NPC 不可縮放（位移/旋轉對 actor 正常）。
- **② PROTEUS**：消費、不改（native 閉源）。**crux 已拍板：clone 穩定、可被 esp 引用**（2026-07-08 使用者確認）→ 路徑 A 成立，facegen GAP 繞過。
- **③ 採集橋 SKSE DLL**：**唯一 net-new 的重工程**。走訪目標 cell 的 placed refs、讀每個 base+transform+enable、記 §B 語意標記與 §D 身份、收 PROTEUS clone 的 ActorRef，序列化成 scene.json。**本 spec 定義它的 output 契約**（下節）；內部實作（SKSE API、UI 走 [SKSE Menu Framework 3](../../../../../analysis/mod-survey/findings/frameworks-tools/skse-menu-framework-3.md) ImGui）另立子專案。**含「滴管取樣」子能力**（見下）——採集橋負責的一個關鍵 runtime 責任是 **FormID → 耐久 `<plugin>:0xLOCALID` 反解**（`TESDataHandler`），因為 runtime FormID 高位元組是 load-order index、跨載入順序不穩，匯出前必須反解成插件相對 ID。
- **④ ModForge**：讀 scene.json → 既有生成鏈。**幾乎零 net-new 生成碼**（見「落點」（[scope-mvp](scope-mvp.md)））。

### ③附：滴管取樣 + 具名插槽（開放式調色盤，idea #24 §E）

Tundra 的可擺清單是設計期寫死的 FormID；本系統改用**遊戲內編輯法術組**（idea #24 §E，3 支）：

- **① 滴管（單點）**：讀準星 ref 的 **base + rot + scale**（`GetCurrentCrosshairRef`→`GetBaseObject`/`GetAngle`/`GetScale`）→ 存進 **StorageUtil 具名插槽**（命名走 UILib/ImGui）→ 選插槽 `PlaceAtMe` + 回填 rot/scale → placement-controller 微調。吸中瞬間在被吸 ref 播**成功特效**（`EffectShader.Play`，純 runtime 回饋、不進 scene.json）。
- **② 範圍吸取**：一次吸半徑內所有 ref（SKSE PO3 `FindAllReferences*` 或重用採集橋 cell 走訪 bound 半徑）→ 每個取 base+transform+scale。
- **③ 移除物件（橡皮擦）**：標記移除；session 內自擺 dynamic ref 直接 `Delete()`，**既有 vanilla ref → scene.json `removals[]`**。

**①② 對 ModForge 衝擊＝零**：吸來的 base 一樣以 `<plugin>:0xLOCALID` 進 `placements[].base`，**ModForge 對外部 ref 自動加來源 mod 為 master（已驗，`PluginIo.cs:35`）**；連帶只是**產物 master 清單隨吸過的物件增長**（匯出宜提示依賴哪些 mod）。採集橋的 **FormID→`<plugin>:0xLOCALID` 反解**（`TESDataHandler`）是讓「當次 session 的 runtime Form」變「耐久可 build 的插件相對 ref」的橋。

**③ 是唯一 net-new 生成項（`removals[]`，GAP 已 grep 驗證）**：ModForge placements 一律 `AddNew`、只能 disable **新** ref，**無「抓既有 vanilla REFR by FormID → disable/delete」路徑**。補法（小）：`removals: ["<master>:0xFORMID", …]` → 生成器在該 ref 所屬 cell 的 override（`VanillaCellOverride` 地基現成）裡把該子 ref 拿成 override 記錄 + 設 `InitiallyDisabled`(0x800)＋深埋（Z −30000，避 disabled havok）或 Delete flag。標準「disable vanilla clutter」patch，Mutagen 易做。→ 非 M0–M2 必需（切片不含移除），列為 §E 隨採集橋（M4）一起補的小生成項。

---
