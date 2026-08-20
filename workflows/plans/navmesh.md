# navmesh — 讓編輯器擺出來的東西被 NPC 走得到（plan）

← [plans](README.md)｜來源：[scene-capture-bridge/backlog](scene-capture-bridge/backlog.md)「📌 導航網格」（使用者標「超重要」）｜契約：[specs/ingame-scene-export-design](../specs/ingame-scene-export-design.md)｜既有解碼：[docs/engine-internals](../../docs/engine-internals.md)「Programmatic navmesh」

**2026-07-12 出計畫。本檔的調研結論已用離線 spike 驗過（見 §2），未經實機的部分一律標記。**

> ## 📍 落地狀態（2026-07-12，第一批。**同日兩個 🎮 實機閘皆 PASS，見下一個框**）
>
> | 階段 | 狀態 |
> |---|---|
> | **P1 診斷警告** | ✅ **已落地**（`Generator.Build.NavmeshIndex.cs` ＋ `Generator.Build.NavmeshCheck.cs`；症狀①②③全覆蓋；離線優雅降級＝完全沉默）|
> | **T2.0 navcut spike** | ✅ 已出貨 → **✅ 實機 PASS（2026-07-12，使用者親測）**（`~/skyrim_mods/mine/ModForgeNavcutSpike.zip`；證據見下框） |
> | **`navCuts[]` 契約 ＋ 自動裁切** | ✅ **已落地 ＋ `autoNavCuts` 已翻回預設 `true`**（`Spec.NavCuts.cs` / `Generator.Build.NavCuts.cs`，commit `80a2873`；使用者拍板的欄位形狀見 §7-2）|
> | **P0 — T0.1 `navdiag`** | ✅ **已落地**（`Diagnostics.Navmesh.cs`；GO/NO-GO 閘已跑：**10/10 NVNM byte-identical**，見下）|
> | **P0 — `navmeshOverrides[]` 契約 ＋ no-op override** | ✅ **已落地**（`Spec.NavmeshOverrides.cs` / `Generator.Build.NavmeshOverrides.cs`；形狀見 §7-5）|
> | **P0 — T0.2 上機** | ✅ 已出貨 → **✅ 實機 PASS（2026-07-12，使用者親測）**（`~/skyrim_mods/mine/ModForgeNavmeshNoop.zip`；證據見下框） |
> | **症狀①（NPC 走進新蓋的房子）** | ✅ **結案** — 用 L_NAVCUT 解掉，**不必動 NAVM**（見下框） |
> | P2.1（NAVM cut／Deleted flag 後備方案） | **不必做**——只是 T2.0 沒過時的備案，T2.0 過了就整段作廢，見 §5 P2 |
> | P3 | ✅ **內裝 edge-to-edge MVP 實機 PASS（2026-08-11）**：`navPatches[]` append-only fan＋唯一完整邊雙向縫合；Bannered Mare 兩名相反方向 Travel actor 都跨過新增↔vanilla seam |
> | P4 | 未動。**2026-08-20 使用者要求開工**（「站在某個地方施法就創造一個點」）→ 任務拆解已寫進 [scene-capture-bridge/backlog「🗺️ 2026-08-20」B 線](scene-capture-bridge/backlog.md)；重點是 **T4.1 讀 live `RE::NavMesh` 做頂點吸附**（`linkTo:"auto"` 要**完整邊重合**才縫得上，不是靠近就縫），不是記座標 |
>
> ### ✅✅ 兩個 🎮 實機閘 2026-07-12 皆 PASS——整條 navmesh plan 的地基與重心都定了
>
> **① P0（vanilla NAVM no-op override）＝ PASS。** 使用者回報：`ModForgeNavmeshNoop.zip` 裝上（排在 USSEP 之後）後，白漫那些 cell（Bannered Mare 內裝、白漫大門～市集外景）的 NPC **一切正常**——「看起來是 ok」。這份 esp 裡除了 10 張逐位元組原封不動的 vanilla NAVM 之外什麼都沒有（沒 NPC、沒擺放、沒腳本），所以「NPC 正常」只有一個解讀：**引擎接受了來自 plugin 的、重新序列化過的 NVNM**。⇒ **override vanilla NAVM 在引擎層可行**（先前只證明了檔案格式層 byte-identical，即 U1；這一步補上了"引擎真的會用它"這一半）。這是整條 B 路線（P2 cut / P3 add）的地基，**從此有得談**。
>
> **② T2.0（L_NAVCUT 證偽實驗）＝ PASS。** 對照組設計：白漫大街上兩條完全相同的車道（同 NPC / 同 patrol package / 同 310 單位南北巡邏／同 4 根告示牌圍線），相距 800 單位，**唯一差別是 TEST 車道中間埋了一顆看不見的 L_NAVCUT box、CONTROL 車道沒有**。使用者回報：**TEST walker 繞過去了**（繞過 Breezehome 那側走一個大弧），而**一模一樣的 CONTROL 直直穿過自己的告示牌線**；TEST 另外還有「在線前短距離來回徘徊、不肯過」的樣態。兩者唯一的差別就是那顆盒子。⇒ **L_NAVCUT 碰撞體積確實在 runtime 把 vanilla navmesh 關掉，讓尋路繞開它。**
>
> **⇒ 症狀①（NPC 走進你蓋的房子/牆/大石頭）就此結案——不必碰 NAVM 一個 byte，用 vanilla 自己的爐火蓋房系統（`CollisionMarker` + `CollisionLayer=49` + Primitive box）就解決了。** 已據此把 `Spec.NavCuts.cs` 的 `AutoNavCuts` 預設翻回 **`true`**（commit `80a2873`，1056 全套測綠）：擋路的大體積 placement 現在**預設自動配一顆 navcut box**（`navCut:false` / `navmesh.autoNavCuts:false` 可關）。
>
> **這兩個結論改變了整份 plan 的重心**：P2（NAVM cut／打 `Deleted` flag，T2.1–T2.2）原本是 T2.0 失敗時的備案，**現在整段不必做**——不會再有第二條路線去解症狀①。**剩下的 NAVM 工作（P3 add+link、P4 DLL 讀 live navmesh）只為症狀②「NPC 要走上新平台/marker 生的 NPC 站在新平台上不動」服務**；症狀③（NPC 走在空氣上）維持原本的低優先／僅診斷警告，不升級。
>
> **P0 的離線證據（2026-07-12，正式化的 T0.1）**：`navdiag out/ModForgeNavmeshNoop.esp` → **10 張 NAVM 全部 `IDENTICAL`**（Bannered Mare 10582B、白漫大街 0x105319 **33428B**〔1106 triangle / 41 跨 mesh EdgeLink / 10 door triangle〕、其餘 8 張）。**vanilla 那一側不經 Mutagen**：直接掃 Skyrim.esm 的 NAVM record header → zlib 解壓（vanilla NAVM 帶 Compressed flag 0x40000）→ 走子記錄取 NVNM 原始 bytes。（拿 Mutagen 的輸出比 Mutagen 的輸出證明不了任何事。）group 結構也已 raw-dump 確認：內裝 `CELL→GRUP6→GRUP9→NAVM`、外景 `WRLD→GRUP1→TopCell(flags 0x40400)→GRUP4→GRUP5→CELL→GRUP6→GRUP9→NAVM`，**零 NAVI 記錄**，masters 只有 Skyrim.esm。
>
> **順手修掉的既有 bug（會同時影響 T2.0）**：`WorldspaceOverride` 沒帶 master 的 `Name`(FULL) → 我們的 WhiterunWorld override 會把「Whiterun」這個名字**清空**（本地地圖/存檔位置）。成因是那段程式寫在 `MasterCache` 還沒 provision 英文 STRINGS 的年代（只硬編碼了 Tamriel）。現在讀得到就直接帶。**`ModForgeNavcutSpike.zip` 已用同一份修正重新出貨**（內容不變，只多了名字）。
>
> **三個實作時發現、與原文不同的事實**（原文照舊留著，這裡是修正）：
> 1. **XPRM `Bounds` ＝ 全尺寸，不是半徑**。原文 §3 的 `Bounds = (187, 170, 60)` 抄自 HearthFires `004104`，但沒說那是半徑還是全寬。**已用 vanilla 判定**：`00410D` 的盒是 116×52.8×46.9，而它包的那口箱子（`TreasBanditChestEMPTY`）OBND 是 96×49×48 —— 三軸比值 0.98–1.2，是**貼著箱子的全尺寸盒**；若為半徑則盒會是箱子的 2.4 倍。→ ModForge 的 `size` **1:1 寫進 Bounds**。
> 2. **navcut 不設 persistent**。原文配方寫 `MajorRecordFlagsRaw |= 0x400`。實際掃過 vanilla：**Skyrim.esm 自己的 441 個靜態 navcut 全是 temporary**（HearthFires 那批帶 0x400 是因為蓋房腳本要 enable-parent 它們）。而且**外景 navcut 若 persistent 就得動 worldspace 的持久 TopCell** —— 正是 [worldspace-override-must-carry-topcell] 那顆炸過兩次的地雷。→ **一律 temporary**（已 byte 比對：產出的 WRLD override 帶 EDID/TopCell(flags 0x40400)、無 OFST，正確）。
> 3. **「自建內裝 cell 完全沒有 navmesh」不當預設警告**。它是**真的**，但它對**每一個** ModForge 內裝都成立（P3 才會修）——每次 build 都吼的警告就是雜訊。→ 收進 `navmesh.warnEmptyCells`（**預設 off**），幾何類警告（vanilla cell 裡站錯地方、擋住沒裁）維持**預設 on**。**採集橋/編輯器的產物都在 vanilla cell，所以使用者真正在意的路徑 100% 覆蓋。**

---

## 1. 問題：三個症狀，痛的程度不同

| # | 症狀 | 觸發它的編輯器動作 | 現況 |
|---|---|---|---|
| **①** | ~~**NPC 走進新蓋的房子/牆/大石頭**（穿模、卡在牆裡、繞不開）~~ **✅ 已解決（2026-07-12 實機 PASS）** | `placements[]` 擺**阻擋型**物件 | **用 L_NAVCUT 碰撞體積 runtime 裁切，不必動 NAVM**（`navCuts[]`／`autoNavCuts` 已預設開）。T2.0 白漫大街對照組實機證明 |
| **②** | **marker 生的 NPC 完全不動** | `annotations[]` marker 落在**沒有 navmesh 的地方**（新平台上、新蓋的房子裡、自建 worldspace 的無網格 cell） | sandbox/travel/combat 全部靠 navmesh；腳下沒網格＝原地站著。**唯一還需要寫 NAVM 的症狀**（P3 add+link）——地基（P0 no-op override）已實機 PASS |
| **③** | **NPC 走在空氣上 / 踩不到的樓梯** | `removals[]` 擦掉、`overrides[]` 移走**可走結構**（樓梯、木橋、平台） | navmesh 留在原地懸空；維持低優先／僅診斷警告，不升級 |

**痛感排序（決定做事順序）**：② ＞ ① ＞ ③。②是**功能完全失效且沉默**（NPC 站著不動，沒有任何錯誤訊息）；①是**視覺災難但 NPC 還會動**（**現已解決**）；③要玩家去拆 vanilla 結構才碰得到，罕見。

---

> **TL;DR（兩句話，2026-07-12 兩個都已 🎮 實機 PASS，不再只是推論）**：症狀①（NPC 走進你蓋的牆）**不必改 navmesh，已結案**——用 vanilla 自己的 **L_NAVCUT 碰撞體積**（HearthFires 蓋房子用了 1220 次）就能 runtime 裁掉，純 Mutagen 一筆 REFR，**T2.0 白漫大街對照組實驗已實機證明有效**。症狀②（NPC 站在新平台上不動）**沒有 runtime 捷徑，非寫 NAVM 不可**——而寫 NAVM **可行**：格式層本 plan 已離線 byte-diff 證明，**P0 no-op override 已實機證明引擎真的會採用來自 plugin 的 NAVM**（地基成立，接下來是 P3 add+link）。

## 2. 結論先講：**編輯 vanilla cell 的 navmesh 可行**——格式層今天已離線證明

### 證據 1（**本 plan 的離線 spike，已跑過**）：Mutagen no-op override ＝ **byte-identical**

拿 Mutagen 0.53.1 `TryResolveContext<INavigationMesh, INavigationMeshGetter>` → `GetOrAddAsOverride(mod)` → 寫出 esp，再把 vanilla 與產出的 **NVNM 子記錄逐位元組比對**：

```
0x0009CB6A  內裝 NAVM（RiverwoodTrader 一帶，CellNavmeshParent）
            v=536 t=554 edgeLinks=0 doorTris=1 grid=2474B div=12 cover=189 flags=0x40000 fv=40
            → NVNM 18230 bytes: IDENTICAL   header flags: MATCH
0x000F0664  外景 NAVM（Tamriel -16,-7，WorldspaceNavmeshParent）
            v=195 t=252 edgeLinks=24 doorTris=0 grid=1416B div=7 cover=32 flags=0x40000 fv=40
            → NVNM 8164 bytes: IDENTICAL    header flags: MATCH
```

意義：**Mutagen 的 NVNM 讀寫是無損的**——cover triangle 索引表、opaque 的 NavmeshGrid blob、跨 mesh EdgeLinks、DoorTriangles、compressed(0x40000) header flag 全都原樣回來。所以「改 vanilla navmesh」的風險**不在檔案格式**，只在**幾何語意**。（腳本留在 scratchpad，正式化＝ P0 的 `navdiag`。）

同一支 spike 也確認：`GetOrAddAsOverride` 自動把 **CELL →（外景再加）WRLD** 整條 parent chain 拉成 override，group 結構正確（`CELL → GRUP 6 → GRUP 9(temporary) → NAVM`）——**和 `removals[]` 用的是同一套機制**（`Generator.Build.Removals.cs`，內裝/外景都已實機驗過）。

### 證據 2：真實 mod 大量在 override vanilla NAVM（houseCARL 讀使用者實際 load order）

`cross_plugin_query type=NAVM conflicts_only` → 這份 load order 裡 **1445 個 vanilla navmesh 被 override**。USSEP **807 筆**、Update.esm 251 筆、`ImprovedCompanionsBoogaloo.esp`（白漫加建築，正是我們的用例）一整串白漫外景＋內裝 NAVM override。**這不是偏門路，是修 mod 的日常。**

### 證據 3：**NAVI 不是相容性地雷**——引擎會 merge

`012FB4:Skyrim.esm` 被 15 個 plugin 碰。**load-order 贏家是 `PROTEUS.esp`，它只有 12 筆 MapInfos**（vanilla 有 15462 筆）——而使用者的遊戲跑得好好的。→ 引擎**加法式合併**每個 plugin 的 NVMI 清單，不是 winner-takes-all。這正是 ModForge `WriteNaviInfoMap` 早就押對的假設（當年由 Vigilant.esm 897 筆推出來，[engine-internals](../../docs/engine-internals.md) §1）。**所以我們只需要吐自己碰到的那幾筆。**

### 真正的地雷（不是格式，是幾何契約）

1. **🔴 triangle index 是位置性的，而且鄰居手上有你的索引**。鄰居 NAVM 的 `EdgeLink { Mesh, TriangleIndex }` 存的是**你這張網格的 triangle 陣列下標**。CK 的 Finalize 會**重新編號** triangle，所以它被迫連鄰近 cell 的 navmesh 一起改存（Arthmoor：「Finalize 時 CK 不只動你改的那張，還會存周圍 cell 的 navmesh」；「border triangle 不該被重新編號」）。社群那句「navmesh 絕不能在 CK 外面改」的**真正成因就是這個**（其次是 cover/finalize 資料），**不是** NAVI。
   → **我們的鐵律：永遠不重新編號。**新增只 **append 到陣列尾端**，刪除只**打 Deleted flag**（見下），既有索引一個都不動。**這是我們相對 CK 的結構性優勢**——CK 做不到，我們做得到。
2. **Deleted flag 存在且對得上 runtime**：Mutagen `NavmeshTriangle.Flag.Deleted = 8`；CommonLibSSE `BSNavmeshTriangle::TriangleFlag::kDeleted = 1 << 3`。同一個 bit。（**引擎尋路是否真的跳過它 ＝ 待實機驗，見 U2**。）
3. **NavmeshGrid 是 opaque bytes，Mutagen 不會重算**（`ByteArray`，divisor² 個桶）。幾何一改就得自己重建。ModForge 已經寫過 divisor=1 的版本（`[u32 count][u16 idx…]`）。**逃生門：把 divisor 設成 1、全部 triangle 丟一個桶**——語意合法，只是空間查詢退化。
4. **內裝安全、外景才有跨 cell 連結**：Bannered Mare `0C9064`：edgeLinks=**0**、doorTris=2。外景 `0F0664`：edgeLinks=**24**（連到 5 張鄰居網格）。→ **內裝是安全灘頭堡**。
5. **沒有任何離線檢查器抓得到壞掉的 navmesh**（houseCARL `check_errors` 自己聲明「不驗 navmesh／地形的空間完整性」；xEdit 也只驗 FormLink）。→ **每一階段都必須有實機閘**，離線只能證偽格式、不能證明語意。

### 一個免費的好消息：**座標空間完全對得上**

| | navmesh 頂點座標 | scene.json `position` 契約 |
|---|---|---|
| 內裝 | **cell-local**（`0C9064` min=(-1461,-1132,32)） | cell-local ✅ |
| 外景 | **world**（`0F0664` min=(-28672,-65536,200)） | world ✅ |

→ **零座標轉換**。採集橋吐的 footprint / 平台角點可以直接拿去和 navmesh 頂點比對。

---

## 3. 五條路線（含推薦與取捨）

| | 路線 | 解決哪個症狀 | 工程量 | 風險 | 判斷 |
|---|---|---|---|---|---|
| **A** | **🥇 L_NAVCUT 碰撞體積（runtime 裁切）**：擺一筆 REFR＝base `CollisionMarker`（`Skyrim.esm:0x000021`）＋ `CollisionLayer = 49`（L_NAVCUT）＋ `Primitive{Type=Box, Bounds=…}`＋persistent(0x400) → 引擎 **runtime 把該體積下的 navmesh 關掉**。**HearthFires.esm 用了 1220 筆**——這就是爐火蓋房子的動態裁切系統 | ① 只裁不補 | **極小**（零新記錄、零 NAVM/NAVI 編輯、零 NIF——就是一筆 placement） | 低（見下 4 條限制） | **✅ 主線的前半段**——症狀①用這條解，**不要**去改 NAVM |
| **B** | **NAVM override**：cut ＝ 打 `Deleted` flag；add ＝ append 頂點/triangle ＋ 就地連邊 | ①②③ 全部 | 中（見 P2/P3） | 中（幾何契約，見上）。格式層**已證** | **主線** |
| **C** | **不動 navmesh，只做診斷＋擺放紀律**：build 時警告「這個 marker 腳下 300 單位內沒有 navmesh triangle → NPC 不會動」「這個 placement 蓋住 vanilla navmesh 的 12 個 triangle → NPC 會走進去」 | 都不解決，但把**沉默失敗變成明確警告** | **小**（純離線查詢，資料已可讀） | **零** | **先做**（P1）。這是最高 CP 值的一步：使用者立刻知道「為什麼 NPC 不動」 |
| **D** | **CK-under-Wine**：把 build 出的 esp 丟進 CK → NavMesh 模式 → Recast 自動生成 → Finalize | ①②③ | 小（工具已在主力機） | **手動 GUI、非 agent 友善**，且 CK 會重新編號 triangle（見地雷 1） | **逃生門**，多層樓/樓梯這種難題的誠實答案。文件化即可，不投入 |
| **E** | **遊戲內採集/繪製**（DLL）：讀 live `RE::NavMesh`（引擎已合併好的成品）＋ 用遊戲自己的物理射線取樣可走面 | 是 B 的**資料來源**，不是替代品 | 中 | 低 | **P4**。這正是使用者原本的願景（idea #24：「施法記腳下座標 → 匯出 → ModForge 生 NAVM」），而且 DLL 端 `RE::NavMesh` 是 TESForm，vertices/triangles/portals 全讀得到 |

**推薦組合：C（立刻）→ A（解症狀①，便宜且 vanilla 驗證過）→ B（解症狀②③，內裝先行）→ E（把輸入從「猜」變成「量」）→ D 永遠留著當逃生門。**

### 🔴 路線 A 的關鍵細節（調研的最大收穫，別搞錯）

**`Obstacle` record flag（bit 25）單獨沒有用。** 它是**兩段閘門**的其中一半：

1. base record 帶 `Obstacle` flag（bit 25），**且**
2. 該碰撞體的 **Havok collision layer 的 COLL 記錄帶 `NavmeshObstacle` flag**。

掃過 vanilla 全部 55 個 COLL：**只有 6 層**帶 `NavmeshObstacle`——`L_ANIMSTATIC(2)`、`L_CLUTTER(4)`、`L_PROPS(10)`、`L_DEBRIS_LARGE(20)`、`L_TRANSPARENT_SMALL_ANIM(28)`、**`L_NAVCUT(49)`**。**`L_STATIC(1)` 不在裡面**——而一般 vanilla 靜態物（房子/牆/石頭）的碰撞正是 L_STATIC。

→ **「複製一個 vanilla STAT base、加上 Obstacle flag」＝ 完全無效**（會白白燒掉幾天）。決定性證據：`WRDrawBridge01.nif` 裡 Bethesda 把**固定橋座放 L_STATIC（永不裁）、會動的橋板放 L_ANIMSTATIC（會裁）**——同一個 mesh 裡精準分層。

**正確配方（vanilla 自己用了 1220 次）**——`HearthFires.esm` 的爐火蓋房系統，例 `004104:HearthFires.esm`：

```csharp
new PlacedObject {
    Base           = 000021:Skyrim.esm,          // CollisionMarker（引擎硬編碼，必須用這顆）
    CollisionLayer = 49,                          // L_NAVCUT
    Primitive      = { Type = Box, Bounds = (187, 170, 60), Color = 255,255,0,0, Unknown = 0.15f },
    Placement      = { Position = …, Rotation = … },
    MajorRecordFlagsRaw |= 0x400                  // Persistent
}
```
**Mutagen 全部欄位都表達得出來**（`PlacedObject.CollisionLayer` / `PlacedObject.Primitive`，已用 reflection 確認 0.53.1）。→ **零新記錄、零 NAVM 編輯、零 NAVI 編輯、零 NIF、零 navmesh 衝突**。

**四條限制（CK wiki，設計時必須吃進去）**：
1. **actor 被當成零體積**去比對 navcut 體積 → 只要留一條沒裁乾淨的縫，NPC 就從縫裡穿過去。**box 要往外脹半個 actor 寬度**。
2. **只在玩家所在 cell 生效**；玩家離開後 NPC 的移動照 AI package 傳送，不理 navcut。
3. **只影響「體積啟用之後才開始」的尋路**——已經在走的 NPC 會直接穿過去。
4. navcut 體積**仍然是碰撞**（用 CollisionMarker 就沒這問題；自製 NIF navcut 要 Mass=0）。

---

## 4. 與現有原語的接點（哪些真的要動 navmesh）

| 原語 | 對 navmesh 的影響 | 要不要動 | 備註 |
|---|---|---|---|
| `placements[]` — **阻擋型**（房子/牆/岩石/柵欄） | vanilla 網格照舊可走 → 症狀① | **要（cut）** | 需要 footprint。**明示優於推導**（同 removals/overrides 的既有決策）：不要自動判斷「這是不是障礙物」，讓使用者/agent 標 |
| `placements[]` — **可走型**（平台/樓梯/橋/新地板） | 站不上去 → 症狀② | **要（add + link）** | 最難但價值最高（②） |
| `placements[]` — **雜物**（杯子/椅子/桌子） | 幾乎無影響（havok 推得開，NPC 照走） | **不要** | 別浪費 triangle |
| `removals[]` | 擦掉阻擋物：網格本來就繞開它 → 只是路徑不最佳，**無害**；擦掉可走結構 → 症狀③ | **低優先** | 先只在 build 警告 |
| `overrides[]` | ＝ remove ＋ place 的疊加 | 同上 | 移動雜物＝零影響 |
| `references[]` | **零影響**（純命名） | 不要 | — |
| `annotations[]`（marker → NPC） | 落點沒網格 → 症狀② | **要（診斷 P1 → 補網格 P3）** | P1 先警告，就已經解掉「不知道為什麼 NPC 不動」 |

---

## 5. 分階段

驗收欄標記：**〔離線〕**＝我自己驗得完；**🎮**＝只有使用者能驗（實機）。

### ✅ P0 — 最小可證偽 spike：`navdiag` ＋ no-op override 上機（**已落地 2026-07-12，🎮 等實機**）

- **✅ T0.1〔離線〕`navdiag`**（`src/ModForge.Cli/Diagnostics/Diagnostics.Navmesh.cs`，比照既有 `landdiag`/`questdiag`）：
  - `navdiag <plugin>` → 列出 plugin 內每張 NAVM（FormID / 頂點 / triangle〔含 Deleted 數〕/ **跨 mesh EdgeLinks** / doorTriangles / cover / grid bytes＋divisor / Min-Max / parent 種類 / record flags），**並把每張「override 了 master」的 NVNM 與 master 的原始位元組逐 byte 比對** → `IDENTICAL` / `DIFF (first difference at byte N)`；有 DIFF 就 **exit 1**。**這就是 GO/NO-GO 閘。**
  - `navdiag <esm> <0xCELL>` / `navdiag <esm> <0xWRLD> <x> <y>` → 偵察某個 vanilla cell 有哪些網格（挑實驗地點就是用它挑的）。
  - **關鍵設計**：vanilla 那一側**不經 Mutagen**——自己掃 record header、zlib 解壓、走子記錄找 NVNM（含 XXXX 超長度）。若兩側都用 Mutagen 序列化，那只證明 `DeepCopy` 穩定，**證不了 Mutagen 的 parse 沒丟東西**。
  - **✅ 驗收〔離線〕**：`ModForgeNavmeshNoop.esp` 的 **10/10 IDENTICAL**（含 33428B 的白漫大街網格）。測試 `NavmeshOverrideTests.cs`（逐 triangle/vertex/EdgeLink/grid blob 比對；`RequiresSkyrim`）。
- **✅ T0.2 🎮 已出貨 → ✅ 實機 PASS（2026-07-12，使用者親測）**：`examples/navmesh_noop_spike_spec.json` → `~/skyrim_mods/mine/ModForgeNavmeshNoop.zip`。整份 esp **只有** 10 張原封不動的 vanilla NAVM（Bannered Mare 內裝 ＋ 白漫外景 (5,-2)/(5,-3)）——**沒 NPC、沒擺放、沒腳本**，所以失敗只有一個可能成因。`esl:false`（U7 是另一顆未知數，不混進實驗）。
  - **使用者回報**：裝上（排在 USSEP 之後）後，白漫那些 cell（Bannered Mare、白漫大門～市集）的 NPC「看起來是 ok」——衛兵巡邏、店內 NPC 走動皆正常，沒有卡在門口/cell 邊界，沒有 CTD。
  - **這一步證明的事**：我們重新序列化的 NVNM 被引擎接受、parent chain（CELL/WRLD override）沒有副作用、NAVI 不必補（U4）、鄰居 cell 不必動（U5——(5,-3)→(5,-2) 是「我們的網格→我們的網格」，(5,-2)→(6,-2) 是「我們的→vanilla 的」，兩個方向都在測）。**這正是 P2/P3 的地基**——地基成立，後續 P3（add+link）有得談。（原文「沒過就整條 B 路線作廢，直接退回 C ＋ D」留著當歷史記錄：**沒有走到那條分支，過了。**）
  - ⚠️ **載入順序**：要當贏家才測得到 → **排在 USSEP 之後（最後）**。代價：**USSEP 也 override 了我們 10 張裡的 7 張**（0x105319/0x051575/0x037DE2/0x05BEF2/0x0941E3/0x05156A/0x05156C），排在它後面等於把那 7 張暫時退回 vanilla 版。測試用 plugin，測完移除即可——但這正是 **U10** 的實例：**NAVM 沒有加法式合併，後蓋前**。（未來若要對外出貨，build 應該警告「這張 navmesh 已被 X 動過」。）

### ✅ P1 — 診斷與警告（**已落地 2026-07-12**）

- **✅ T1.1** `Generator.Build.NavmeshIndex.cs`（讀幾何）＋ `Generator.Build.NavmeshCheck.cs`（出警告，`Build()` 最後跑、**零記錄**）：
  - **②** ACHR 不在任何三角形的 2D 投影內 → `! navmesh: NPC 'X' is off the navmesh — the nearest walkable triangle is 420 units away. It will NOT move …`；在三角形上但高度差過大（>200 上 / >400 下）→ `… is 560 units ABOVE the navmesh under it (floor z=…, placed z=…)`。
  - **①** placement 的 OBND footprint 蓋住 N 個 vanilla triangle 且**沒有任何 navcut box 蓋掉它們** → `! navmesh: placement 'Y' covers 12 vanilla navmesh triangle(s) but nothing cuts them — NPCs will walk into it`（有 navcut → 閉嘴；`navCut:false` → 換一句「你說不裁的，NPC 會穿過去」）。
  - **③** `removals[]`/`overrides[]` 動到的大物件，**頂面**有 vanilla navmesh（＝它本來是被走在上面的）→ 提示 NPC 會走在空氣上。
  - 座標系天生對得上（內裝 cell-local、外景 world ＝ `PlacementSpec.Position` 的兩個 frame），**零轉換**。外景查 **3×3 鄰域**（點在 cell 邊界時常被鄰居的網格蓋住，不查會誤報）。跳過帶 `Deleted` flag 的三角形。
- **✅ T1.2** 門檻與開關：`navmesh.minFootprint`(10000 units²)＋`minHeight`(100) → 只有「真的擋得住路」的東西才進①（椅子 60×60=3600、木桶 54×54 → 永遠不吵）；`navmesh.warnings:false` 全關；`placements[].navmeshCheck:false` 單筆豁免（**唯一正當用途＝刻意 park 在網格外、等腳本 MoveTo 進場的 ACHR**，`livingNpcs` 的 off-stage 停車位就是——這個 false positive 是本次實作抓出來的）。
- **✅ 驗收〔離線〕**：`NavmeshCheckTests.cs`（雜物零警告／站好零警告／離網格警告／浮空警告／有 navcut 閉嘴／**spike spec 本身必須零警告**）。**無 Skyrim.esm ＝ 全部沉默**（「不知道」永遠不當成「有問題」）。
- 🎮 無需實機。**P1 做完，症狀②從「NPC 為什麼不動？」變成 build 時的一行字。**

### ✅ P2 — cut：讓擺出來的建築真的擋住 NPC（**2026-07-12 實機 PASS，症狀①結案**）

- **✅ T2.0（已出貨 → ✅ 實機 PASS，2026-07-12）— 路線 A：L_NAVCUT 體積**。契約與生成端全部落地（`Spec.NavCuts.cs` / `Generator.Build.NavCuts.cs`，欄位形狀見 §7-2）；產物 `~/skyrim_mods/mine/ModForgeNavcutSpike.zip`（`examples/navcut_spike_spec.json`）。
  - **實機結果（使用者回報）**：**TEST walker 繞過去了**——繞過 Breezehome 那側走一個大弧；一模一樣的 **CONTROL 直直穿過**自己的告示牌線；TEST 另外還觀察到「在線前短距離來回徘徊、不肯過」的樣態。兩條車道唯一的差別就是那顆看不見的 navcut box ⇒ **L_NAVCUT 碰撞體積確實在 runtime 關掉 vanilla navmesh。**
  - **⇒ 症狀①結案，NAVM 一個 byte 都不用碰。** 下面「後備實作：NAVM cut（T2.1–T2.2）」**整段作廢，不必做**——原文保留在這裡是留給以後若真的需要「打 Deleted flag」這個能力時（例如以後真的要處理症狀③）當設計參考，**不是待辦**。
  - **已據此落地**：`Spec.NavCuts.cs` 的 `AutoNavCuts` 預設翻回 **`true`**（commit `80a2873`，1056 全套測綠）——擋路的大體積 placement 現在**預設自動配一顆 navcut box**，`navCut: false` / `navmesh.autoNavCuts: false` 可關。
  - **實驗設計（重點是「一眼看得出成敗」＋去除混淆變因）**：白漫大街（`WhiterunWorld` = `Skyrim.esm:0x01A26F`）上擺**兩條完全相同的車道**，相距 800 單位——同樣的 NPC、同樣的 patrol package、同樣相距 310 單位的兩顆 marker、同樣 4 根告示牌標出屏障線。**唯一的差別：A 道中間有一顆 navcut box，B 道沒有。**
    - **不放實體牆**（本來想放，但 NPC 撞牆會 slide、會沿牆滑走 → 「它繞過去了」變成可疑的偽陽性）。告示牌只有 18×18，NPC 直接從中間走過去，**只有那顆看不見的盒子能造成差異**。
    - box：中心 (21750, −7625, −3510.6)、size 520×140×220、padding 32 → 實際 XPRM Bounds **584×204×284**；蓋住 7 個三角形重心、與 12 個三角形相交。204 單位厚的禁區遠寬過 NPC 的一步，任何路徑都跨不過去。
    - **14 個座標全部是讀 Skyrim.esm 的 navmesh 挑的**（三角形內插高度）——marker 不會貼地，猜 z ＝ patrol 靜默失效（這正是 `patrol_spec` 當年 round-1 掛掉的原因），而 P1 的檢查現在就是防這個的工具（spike spec 跑出來零警告，有測試盯著）。
  - **判讀（設計成對兩種可能的引擎實作都成立）**：**TEST 的走法只要和 CONTROL 有任何不同**（繞開、拒絕穿越、貼著盒子邊緣走）⇒ navcut 有效，**症狀①結案，T2.1–T2.2（NAVM cut）整個不必做**。**兩隻都直直走過去** ⇒ L_NAVCUT 對我們無效，**路線 A 被證偽**，回頭走下面的 NAVM cut。**→ 實機結果是前者。**
  - 附帶測：console `tnm`（toggle navmesh info）在 SSE 還能不能畫出網格——能的話往後每階段的實機驗收都快十倍。（本輪回報未提及，仍待之後某次實機順手看一眼。）

- **契約（已落地）**：頂層 `navCuts[]`，與 `removals[]`/`overrides[]`/`references[]` 並列（同一個家族：**碰既有記錄的操作住頂層**）。**最終形狀見 §7-2**（原草案的 `shape`/`center` 欄位換成 `position`（＝中心）/`size`（＝全尺寸）/`padding`，並多了 `placements[].navCut` 的自動三態）。
- **~~後備實作：NAVM cut（`Generator.Build.NavCuts.cs`，T2.1–T2.2）~~ ❌ 不必做（T2.0 PASS，路線 A 全勝）**：下面 4 步與兩條驗收原文保留**只當設計參考**——若以後真的要處理症狀③（NPC 走在空氣上）而想借用「打 Deleted flag」這個手法，可以照抄；**不是待辦、不排進任何階段**。
  1. 解出目標 cell 的 NAVM（`ICellGetter.NavigationMeshes`）→ `GetOrAddAsOverride`。
  2. 對每個 triangle：重心（或三頂點）落在 cut 體積內 → `Flags |= NavmeshTriangle.Flag.Deleted`。**不刪陣列元素、不重編號、不碰 EdgeLinks/DoorTriangles/Grid**（索引全部不變 → 鄰居的 EdgeLink 依然正確 → **不必碰鄰居 cell**）。
  3. NAVI：先**不碰**（mesh FormID 沒變，vanilla NVMI 條目仍然描述它）——這是 U4，未實機驗過（沒必要驗了，U4 留給 P3）。若不行，退路＝ override `012FB4` 並補一筆該 mesh 的 NVMI（`Unknown = 0x00` ＝ 「非 island、已修改」，這是真實 mod 的寫法）。
  4. build 摘要印 `navCuts: 1 cut, 14 triangles disabled in NAVM 0C9064`。
- （原）驗收〔離線〕：`navdiag` 顯示 14 個 triangle 帶 Deleted flag、其餘位元組與 vanilla 一致（**只有 flags 欄變**）；NVNM 長度不變。
- （原）驗收 🎮：白漫城內擺一面牆＋`navCuts` → NPC **繞開**它（而不是走進去）。**這一步證明 U2（引擎尊重 Deleted flag）。** 若 U2 不成立 → 退路是「真刪 triangle」（只限**內裝**，edgeLinks=0，只要修 DoorTriangles 的 index；外景則放棄 cut，改走路線 A/D）。

### P3 — add + link：讓 NPC 走上新蓋的東西（解症狀②的正解，**現在是整份 plan 剩下唯一要做的 NAVM 工作**，地基〔P0〕已 2026-07-12 實機 PASS）

> **2026-08-11 收旂**：第一版採「vanilla 內裝＋凸多邊形＋完整邊 edge-to-edge 縫合＋不引 DotRecast」，設計見 [navmesh-patch-design](../specs/navmesh-patch-design.md)，可執行任務拆到 [navmesh-p3](navmesh-p3.md)。下方原始草圖保留作為設計來源；欄位與失敗語意以 design 為準。

- **契約**：`navPatches[]`
  ```json
  "navPatches": [
    { "cell": "Skyrim.esm:0x01605E",
      "polygon": [ {"x":0,"y":0,"z":100}, ... ],   // 3+ 個角點（順序＝周長），或
      "quad":    { "center": …, "size": …, "z": … },
      "linkTo": "auto"                              // auto = 自動與距離 < eps 的既有邊縫合
    }
  ]
  ```
- **實作**：
  1. 三角化多邊形（凸多邊形先做 fan triangulation 就夠；平台/地板都是凸的）。
  2. **Append** 頂點與 triangle 到既有陣列尾端；新 triangle 之間互設鄰居索引。
  3. **縫合**：對每個新 triangle 的邊界邊，找既有 triangle 的邊，兩端點距離 < eps（例：8 單位）且高度差 < eps → **雙向**設鄰居索引（既有 triangle 的 `EdgeLink_n` 從 -1 改成新 triangle 的 index——**這是「改既有 triangle 的欄位」，但不是重編號，安全**）。
  4. 更新 `Min`/`Max`；**重建 NavmeshGrid**（先用 divisor=1 單桶逃生門，見 U3）。
  5. NAVI：同 P2（先不碰、驗完再說）。
- 驗收〔離線〕：`navdiag` 顯示新 triangle 與既有 triangle 互為鄰居；既有 triangle 的**索引一個都沒變**（拿 P0 的 byte-diff 驗：舊 triangle 區段除了被縫合的那幾個 EdgeLink 欄位外，位元組不變）。
- 驗收 🎮：① 在既有網格旁邊補一塊平台 → NPC **走得上去、也走得下來**；② marker 生的 NPC 站在新平台上 → **會 sandbox / 會跟隨玩家離開平台**（這一條同時驗掉 U8＝孤島網格能不能用）。

### P4 — 遊戲內採集（把輸入從「猜」變成「量」）

> **2026-08-20 開工**（使用者主動要求）。可執行任務拆到 [scene-capture-bridge/backlog「🗺️ 2026-08-20」B 線](scene-capture-bridge/backlog.md)（B1 T4.1 spike／B2 `sc nav` 模式／B3 匯出 `navPatches[]`／B4 遊戲內 guard）。兩個當時沒寫進下面草圖、但實作前必須吃進去的事實：① **navmesh 不是 waypoint graph**——兩點連線不產生可走面，最少三點成一個三角形，所以手勢是「走一圈標角點再收口」＝ 一個 `polygon`，不是「連接兩點」；② **P4 的技術重點是頂點吸附**——`linkTo:"auto"` 要新邊與舊邊**兩端點都在 eps 內且唯一匹配**才雙向縫合，縫不上整筆 failure，所以 T4.1 不只是「附加摘要」，它是 `sc nav` 能不能用的前置。**第一版只支援 vanilla 內裝**（外景跨 cell EdgeLink 見 §7-3 仍未開）。

- **T4.1 DLL：讀 live navmesh**。`RE::NavMesh`（TESForm，FormType::NavMesh）→ `vertices` / `triangles` / `extraEdgeInfo` / `doorPortals` 全在 `BSNavmesh` 裡。用途：(a) 匯出時附上「我這個 cell 的 navmesh 摘要」給 ModForge 交叉檢查；(b) 面板顯示「你的 marker 腳下有沒有網格」——**把 P1 的離線警告提前到遊戲內即時顯示**（使用者站在那裡就知道 NPC 會不會動）。
- **T4.2 DLL：footprint 匯出**。每筆 placement 帶 `bounds`（world AABB，從 `ref->Get3D()->worldBound` 或 base 的 OBND ＋ transform）→ ModForge 的 `navCuts` 不必再從 OBND 猜。
- **T4.3 DLL：`sc nav` 模式（使用者的原始願景）**。走一圈、按鍵記角點 → `navPatches[].polygon`；或**射線取樣**：在一個矩形區域內用遊戲自己的物理往下打射線 → 拿到真實地板高度 → 生成貼合地形/貼合新蓋房子地板的網格（＝ 用引擎的物理當 Recast 的體素化）。
- 驗收 🎮：走一圈標出一塊平台 → 匯出 → build → NPC 在上面走。

### 不做（明確排除）

- **不實作 Recast/DotRecast 的離線體素化**：需要 NIF havok 幾何，是另一條產業鏈；**遊戲內射線取樣（T4.3）用一成的力氣拿到八成的結果**。
- **不碰 cover / preferred / NVPP**：cover 由 Mutagen 從 `Triangle.IsCover` 自動推導；新 triangle 一律無 cover（NPC 只是不會拿它當掩體，不影響走路）。
- **不刪除、不 disable 整張 NAVM**（社群一致：`never delete or merge navmeshes! That way lies CtD`）。

---

## 6. 未知數清單（本 plan 建立在這些假設上）

| # | 假設 | 影響 | 怎麼驗 |
|---|---|---|---|
| **U1** | Mutagen NVNM 讀寫無損 | 全部 | ✅ **已驗**（正式化為 `navdiag`：**10/10 IDENTICAL**，vanilla 側直接讀 Skyrim.esm 原始 bytes、不經 Mutagen） |
| **U2** | 引擎的尋路**尊重 triangle 的 Deleted flag**（不走它） | 原訂 P2 的全部；**P2（NAVM-cut 備案）已因 T2.0 PASS 而不必做** → U2 目前**不再是任何已排階段的阻塞項**，留著給未來若真要動 Deleted flag（如症狀③）才驗 | 🎮 未驗（不需要了）。不成立時的退路：內裝改真刪＋修 DoorTriangle index；外景退回路線 A/D |
| **U3** | 改幾何後把 NavmeshGrid 換成 divisor=1 單桶（或自建 divisor² 桶）引擎能接受 | P3 | ✅ **P3 實機 PASS（2026-08-11）**：改成單桶後兩名 Travel actor 均跨 seam，引擎接受重建 grid |
| **U4** | override 既有 NAVM 時**不必**碰 NAVI（vanilla NVMI 條目仍有效） | P3 的複雜度 | ✅ **P3 實機 PASS（2026-08-11）**：NAVM append 4 vertex / 2 triangle、NAVI 零 authored，雙向尋路仍成立 |
| **U5** | 只改自己這張 mesh、不重編號 → **鄰居 cell 的 NAVM 不必動** | 外景可行性 | ✅ **P0 觀察支持**（白漫外景 (5,-2)/(5,-3) 兩張 override，跨 cell 邊界走動正常）；P3 外景驗收再確認一次 🎮 |
| **U6** | **路線 A**：L_NAVCUT 體積在**我們自己的 patch esp** 裡也照樣被引擎裁（HearthFires 是 esm，且是 vanilla 自家系統） | 症狀①的成敗 | ✅ **已驗（T2.0，2026-07-12 實機 PASS）**——TEST 繞開/徘徊不過、CONTROL 直穿，唯一差異是那顆 box |
| **U6b** | ~~複製 STAT base ＋ 加 Obstacle flag 就會裁~~ **❌ 已否證** | — | **flag 是兩段閘門的一半**：還要碰撞體所在的 collision layer 帶 `NavmeshObstacle`。vanilla 55 個 COLL 只有 6 層帶（L_ANIMSTATIC/CLUTTER/PROPS/DEBRIS_LARGE/TRANSPARENT_SMALL_ANIM/**L_NAVCUT**），**L_STATIC 不帶** → 對一般靜態物加 flag ＝ 無效。**這條若沒查出來會白燒好幾天** |
| **U7** | **ESL**：override vanilla NAVM（FormID 不變）在 ESL 裡安全；但**新建** NAVM 記錄從 ESL 載入可能壞 | 編輯器 patch 預設 `esl: true` | 保守規則：`navCuts` 只 override → 維持 esl；一旦要新建 NAVM（自建 worldspace）→ 比照既有 LAND 守則強制 `esl:false`（`Generator.Validate.World.cs:131`）。🎮 待驗 |
| **U8** | 沒和 vanilla 網格相連的「孤島」navmesh，NPC 能否在上面 sandbox（travel 出不去可接受） | 未排的明示 `linkTo:none` 降級路徑 | 🎮 未驗，且不屬 P3 edge-to-edge MVP。**已知：完全沒有網格 ＝ NPC 什麼都不做**（sandbox/travel/follow/combat 全掛；SE 還要求 actor 本身站在 triangle 上尋路才會啟動）。`PathToReference` **不繞過** navmesh（走同一套尋路，一樣失敗）；只有 `TranslateTo`/`MoveTo` 能無視，但那是腳本用的位移、不是 AI |
| **U9** | 外景 NAVM override 會拉出 **WRLD override**——會不會踩到既有的地圖渲染坑（[worldspace-override-map-render-fields](../../docs/engine-internals.md)：缺 EDID/RNAM 會白地圖、帶 OFST 會壞） | 外景全部 | 已初步觀察：Mutagen deep-copy 的 WRLD 帶 EDID/TNAM/UNAM、**不帶 OFST**（正確）。且 `removals[]` 的外景路徑早已實機驗過同一條 chain。P0 這輪回報聚焦在 NPC 行為（「一切正常」），**沒有特別點名地圖/名字**——U9 嚴格說仍待一次專門確認，不算已 close |
| **U10** | 我們的 NAVM override 與**其他 mod 的同一張 NAVM override** ＝ 整筆記錄後蓋前（last wins） | 相容性 | 無法避免（NAVM 沒有加法式合併）。**✅ build 警告已實作（2026-07-29），真實 USSEP 驗收 PASS（2026-08-11）**：`CheckNavmeshOverrideClobbers`（`Generator.Build.NavmeshOverrides.cs`，跟 P1 一樣 build 末跑、零記錄）掃 Data 夾裡「master 到我們 override 的 NAVM 所屬 master」的非-vanilla plugin，若它也 override 同張 → 一行警告點名該 plugin＋mesh（無法知 load order 誰贏，只點名衝突）。只在 `navmeshOverrides[]` / `navPatches[]` 實際產出 NAVM 時掃、無 Data 夾＝沉默、開關 `navmesh.warnNavmeshClobber`。離線用合成 plugin 已測掃描邏輯（`NavmeshOverrideTests` 三條）；主力機以暫時 Data overlay 放入真實 `Unofficial Skyrim Special Edition Patch.esp` 後 build `navmesh_noop_spike_spec.json`，警告正確點名 USSEP ＋ **7 張** NAVM（與既有 7/10 調查完全一致） |

---

## 7. 拍板紀錄 ＋ 還沒拍的

### ✅ 1. 順序（2026-07-12 使用者拍板）

**P1（診斷）→ T2.0（L_NAVCUT spike）→ P0（NAVM no-op 上機）→ P3（add+link）→ P4（DLL 讀 live navmesh）。** **前三項已做完且兩個 🎮 實機閘（T2.0／P0）皆 2026-07-12 PASS**（見頂部落地狀態）——**下一步是 P3**。

### ✅ 2. 明示 vs 自動 ＝ **「兩者都要」**（2026-07-12 使用者拍板）——**預設自動 ＋ 可關 ＋ 可手調**

原文兩案並陳（明示 vs 自動）；使用者裁決：**大體積 placement 預設自動配一顆 navcut box，但要能明示關掉、也要能手調 box 尺寸。** 落地的欄位形狀：

```jsonc
// ① 頂層 navCuts[]：明示一顆盒（與 removals/overrides/references 同一家族——碰「既有世界」的操作住頂層）
"navCuts": [
  { "editorId": "MF_CutUnderHouse",
    "worldspace": "Skyrim.esm:0x00003C",          // 或 cell（內裝）
    "position": {"x":100,"y":200,"z":-3510},      // box 的「中心」（三軸都是中心！）
    "size":     {"x":520,"y":140,"z":220},        // 「全尺寸」w×d×h（不是半徑）
    "rotationZ": 45,                              // 度
    "padding": 32 },                              // 每一側往外脹（預設 32）
  { "placement": "MF_MyHouse" }                   // 便利式：直接包某個 placement 的 OBND footprint
],

// ② placements[].navCut：**拍板的三態**（省略 / false / true / 物件）
{ "editorId":"MF_MyHouse", "base":"...", "worldspace":"...",
  // (省略)              → 自動：OBND 過門檻 && 真的蓋到 vanilla navmesh 才裁
  // "navCut": false     → 明確不裁（裝飾用假牆、本來就該讓 NPC 穿過去的東西）
  // "navCut": true      → 明確要裁（就算沒過門檻）
  // "navCut": { "size": {...}, "offset": {...}, "padding": 48 }   → 手調盒
},

// ③ 頂層 navmesh：門檻與總開關
"navmesh": {
  "autoNavCuts": true,      // 預設 true ＝拍板的「預設自動」
  "minFootprint": 10000,    // units²（100×100）：OBND 的 XY 面積門檻。椅子 60×60=3600、木桶 54×54 → 遠低於門檻，雜物不會被裁
  "minHeight": 100,         // units：OBND 高度門檻
  "padding": 32,            // 每顆盒的預設外脹（≈半個 actor 寬）
  "warnings": true,         // P1 診斷總開關
  "warnEmptyCells": false   // 「自建內裝沒 navmesh」提醒（預設 off，理由見頂部第 3 點）
}
```

**「自動」的三道 guard（缺一不可，這是它敢預設開的原因）**：① 物件（非 ACHR/hazard）；② 落在 **vanilla** cell/worldspace（自建 cell 沒有 vanilla 網格可裁）；③ **真的蓋到 ≥1 個 live 三角形**（沒東西可裁就不生記錄）。→ 只在**真的會出事**的地方生記錄，而且第③條讓**離線機（無 Skyrim.esm）＝零產出＝位元不變**。

**✅ 已點頭並落地（2026-07-12）**：T2.0 實機 PASS 後，`autoNavCuts` 已翻回**預設 `true`**（commit `80a2873`，1056 全套測綠）。既有 spec 只要在 vanilla 世界擺了大體積物件，下次 build 就會多出 navcut REFR——這正是拍板要的行為，也正是那些 mod 一直有的 bug 的正確修法。

### ✅ 5. `navmeshOverrides[]` 的欄位形狀（P0 落地，2026-07-12）

與 `removals[]`/`overrides[]`/`references[]`/`navCuts[]` 同一家族——**碰「既有世界」的操作住頂層**：

```jsonc
"navmeshOverrides": [
  { "cell": "Skyrim.esm:0x01605E" },                          // vanilla 內裝：該 cell 的每一張 NAVM
  { "worldspace": "Skyrim.esm:0x01A26F", "x": 5, "y": -2 },   // 外景：一格 cell（x/y ＝ **格座標**，不是世界單位）
  { "worldspace": "Skyrim.esm:0x01A26F",                      // …或用格內任一點指定那一格
    "position": {"x":21750,"y":-7625,"z":0} },
  { "cell": "Skyrim.esm:0x01605E",
    "navmesh": "Skyrim.esm:0x0C9064" }                        // 只挑一張（cell 內可能有很多張）
]
```

- **同 FormKey ＝ override**（不是新記錄）→ vanilla 的 NVMI 條目、鄰居的 EdgeLink、door portal 全部照樣指得到。
- **NAVI 不碰**（U4）。**離線＝零產出零警告**。**in-spec cell 直接 validate 擋掉**（自己的 cell 沒有 vanilla 網格可 override，那是 P3）。
- **實作刻意不用 Mutagen 的 `GetOrAddAsOverride` parent chain**——那會讓 Mutagen 自己造 CELL/WRLD override；我們的 `WorldspaceOverride`/`CopyWorldspaceEnv` 帶著兩顆炸過的地雷的疤（LandDefaults / EDID+RNAM / TopCell 的 record flags / **不帶 OFST**）。走 `ExteriorCell()` 就全部繼承，而且保證每個 cell/worldspace 在輸出裡**只有一個** override 物件。
- **P2/P3 就長在這個契約上**：同一筆 entry 之後加 `cut`/`patch` 欄位即可，鐵律（永不重新編號）已經由「逐元素照抄」的實作與測試釘死。

### ✅ 3. 內裝先行（2026-08-11 保守收旂）

P3（NAVM add）**先只支援 vanilla 內裝**（無跨 cell EdgeLink），內裝實機過了才開外景。理由與契約見 [navmesh-patch-design](../specs/navmesh-patch-design.md)。

### ✅ 4. P3 MVP 不引 DotRecast（2026-08-11 保守收旂）

C# 的 Recast port 雖可用，但它要吃三角湯，會把 P3 擴成 NIF/havok 解析產業鏈。MVP 改用作者／P4 採集器提供的凸多邊形做 fan triangulation；自動貼地留在 P4。

---

## 8. 附：CK「Finalize」到底做了什麼——哪些非做不可、哪些可以不管

| CK finalize 做的事 | 我們必須自己重現？ |
|---|---|
| triangle **鄰接**（每個 triangle 的 3 個鄰居索引） | **要**（Mutagen 沒有解算器，我們自己算） |
| **跨 cell edge link**（外景相鄰 cell 的網格縫合）——沒有的話「actor 無法從一個 cell 走到另一個」 | **要**（外景多 cell 時）。內裝不需要 |
| **NavmeshGrid ＋ divisor ＋ Min/Max/MaxDistance** | **要**，但 1×1 全塞一桶就合法（ModForge 已這樣出貨過） |
| **NAVI/NVMI 註冊**（新網格） | **要**（加法式 override `012FB4`，絕不新建） |
| **door triangle**（門前那個綠三角）——沒有的話「AI 不會用那扇門」 | **要**（cell 有門時） |
| cover data（`Find Cover Edges`） | **不用**（只是遠程 AI 的掩體提示）。⚠️ 順帶：xEdit 原始碼自承 cover flag 的定義是**錯的**（「前 4 bit 是 enum…現有定義方法表達不出來」）→ **不要相信 CoverFlags 的語意** |
| preferred pathing（NVPP）／dropdown edges／island／merges | **不用**（CK 自己 finalize 時還會把 dropdown 清掉） |

**證據就在我們自己身上**：`AddFlatCellNavmesh` 出的是**4 頂點 / 2 triangle、零 cover、零 door triangle、零 edge link、1×1 grid** 的四邊形——**實機確認可以走**。所以「非 CK 不可」的folklore只對一半：CK 的價值是**幾何解算器**，不是什麼祕密格式。我們自己算鄰接，CK 就是可選的。
