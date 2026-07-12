# navmesh — 讓編輯器擺出來的東西被 NPC 走得到（plan）

← [plans](README.md)｜來源：[scene-capture-bridge/backlog](scene-capture-bridge/backlog.md)「📌 導航網格」（使用者標「超重要」）｜契約：[specs/ingame-scene-export-design](../specs/ingame-scene-export-design.md)｜既有解碼：[docs/engine-internals](../../docs/engine-internals.md)「Programmatic navmesh」

**2026-07-12 出計畫。本檔的調研結論已用離線 spike 驗過（見 §2），未經實機的部分一律標記。**

> ## 📍 落地狀態（2026-07-12，第一批）
>
> | 階段 | 狀態 |
> |---|---|
> | **P1 診斷警告** | ✅ **已落地**（`Generator.Build.NavmeshIndex.cs` ＋ `Generator.Build.NavmeshCheck.cs`；症狀①②③全覆蓋；離線優雅降級＝完全沉默）|
> | **T2.0 navcut spike** | ✅ 已出貨 → **🎮 等實機**（`~/skyrim_mods/mine/ModForgeNavcutSpike.zip`；驗收步驟見 [wait_todo/ingame-tests](../../wait_todo/ingame-tests.md)）|
> | **`navCuts[]` 契約 ＋ 自動裁切** | ✅ **已落地**（`Spec.NavCuts.cs` / `Generator.Build.NavCuts.cs`；使用者拍板的欄位形狀見 §7-2）|
> | P0 / P2.1 / P3 / P4 | 未動（照原順序） |
>
> **三個實作時發現、與原文不同的事實**（原文照舊留著，這裡是修正）：
> 1. **XPRM `Bounds` ＝ 全尺寸，不是半徑**。原文 §3 的 `Bounds = (187, 170, 60)` 抄自 HearthFires `004104`，但沒說那是半徑還是全寬。**已用 vanilla 判定**：`00410D` 的盒是 116×52.8×46.9，而它包的那口箱子（`TreasBanditChestEMPTY`）OBND 是 96×49×48 —— 三軸比值 0.98–1.2，是**貼著箱子的全尺寸盒**；若為半徑則盒會是箱子的 2.4 倍。→ ModForge 的 `size` **1:1 寫進 Bounds**。
> 2. **navcut 不設 persistent**。原文配方寫 `MajorRecordFlagsRaw |= 0x400`。實際掃過 vanilla：**Skyrim.esm 自己的 441 個靜態 navcut 全是 temporary**（HearthFires 那批帶 0x400 是因為蓋房腳本要 enable-parent 它們）。而且**外景 navcut 若 persistent 就得動 worldspace 的持久 TopCell** —— 正是 [worldspace-override-must-carry-topcell] 那顆炸過兩次的地雷。→ **一律 temporary**（已 byte 比對：產出的 WRLD override 帶 EDID/TopCell(flags 0x40400)、無 OFST，正確）。
> 3. **「自建內裝 cell 完全沒有 navmesh」不當預設警告**。它是**真的**，但它對**每一個** ModForge 內裝都成立（P3 才會修）——每次 build 都吼的警告就是雜訊。→ 收進 `navmesh.warnEmptyCells`（**預設 off**），幾何類警告（vanilla cell 裡站錯地方、擋住沒裁）維持**預設 on**。**採集橋/編輯器的產物都在 vanilla cell，所以使用者真正在意的路徑 100% 覆蓋。**

---

## 1. 問題：三個症狀，痛的程度不同

| # | 症狀 | 觸發它的編輯器動作 | 現況 |
|---|---|---|---|
| **①** | **NPC 走進新蓋的房子/牆/大石頭**（穿模、卡在牆裡、繞不開） | `placements[]` 擺**阻擋型**物件 | vanilla navmesh 沒被裁，引擎不知道那裡不能走 |
| **②** | **marker 生的 NPC 完全不動** | `annotations[]` marker 落在**沒有 navmesh 的地方**（新平台上、新蓋的房子裡、自建 worldspace 的無網格 cell） | sandbox/travel/combat 全部靠 navmesh；腳下沒網格＝原地站著 |
| **③** | **NPC 走在空氣上 / 踩不到的樓梯** | `removals[]` 擦掉、`overrides[]` 移走**可走結構**（樓梯、木橋、平台） | navmesh 留在原地懸空 |

**痛感排序（決定做事順序）**：② ＞ ① ＞ ③。②是**功能完全失效且沉默**（NPC 站著不動，沒有任何錯誤訊息）；①是**視覺災難但 NPC 還會動**；③要玩家去拆 vanilla 結構才碰得到，罕見。

---

> **TL;DR（兩句話）**：症狀①（NPC 走進你蓋的牆）**不必改 navmesh**——用 vanilla 自己的 **L_NAVCUT 碰撞體積**（HearthFires 蓋房子用了 1220 次）就能 runtime 裁掉，純 Mutagen 一筆 REFR。症狀②（NPC 站在新平台上不動）**沒有 runtime 捷徑，非寫 NAVM 不可**——而寫 NAVM **可行**，格式層本 plan 已離線 byte-diff 證明。

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

### P0 — 最小可證偽 spike：`navdiag` ＋ no-op override 上機（**格式層已在離線過關，這階段是把它釘死**）

- **T0.1〔離線〕** `navdiag` CLI 子命令（比照既有 `landdiag`/`questdiag`）：
  - `navdiag <plugin> <cellRef>` → 印該 cell 的 NAVM 清單：FormID / 頂點數 / triangle 數 / edgeLinks（＋對端 mesh）/ doorTriangles / grid divisor / Min-Max / cover 數 / parent 種類。
  - `navdiag roundtrip <cellRef>` → no-op override → 寫檔 → **NVNM 逐位元組比對 vanilla**，印 IDENTICAL/DIFF。**這是 GO/NO-GO 閘**（已預跑：兩個樣本都 IDENTICAL）。
  - `navdiag under <cellRef> <x> <y> [z]` → 該點正下方/最近的 triangle（距離、triangle index、頂點高度）。**P1 的診斷靠它。**
  - 驗收〔離線〕：對 `01605E`（Bannered Mare）、`0009BB9`（白漫外景）跑 roundtrip ＝ IDENTICAL；新增測試 `NavmeshTests`（標 `RequiresSkyrim`）。
- **T0.2 🎮** 出一份**只含 no-op NAVM override**（內裝一張＋外景一張，什麼都不改）的 esp → 進遊戲：**不 CTD、NPC 在該 cell 照常走動、開/關門正常**。
  - **這一步證明的事**：我們重新序列化的 NVNM 被引擎接受、parent chain（CELL/WRLD override）沒有副作用。**沒過就整條 B 路線作廢**，直接退回 C ＋ D。

### ✅ P1 — 診斷與警告（**已落地 2026-07-12**）

- **✅ T1.1** `Generator.Build.NavmeshIndex.cs`（讀幾何）＋ `Generator.Build.NavmeshCheck.cs`（出警告，`Build()` 最後跑、**零記錄**）：
  - **②** ACHR 不在任何三角形的 2D 投影內 → `! navmesh: NPC 'X' is off the navmesh — the nearest walkable triangle is 420 units away. It will NOT move …`；在三角形上但高度差過大（>200 上 / >400 下）→ `… is 560 units ABOVE the navmesh under it (floor z=…, placed z=…)`。
  - **①** placement 的 OBND footprint 蓋住 N 個 vanilla triangle 且**沒有任何 navcut box 蓋掉它們** → `! navmesh: placement 'Y' covers 12 vanilla navmesh triangle(s) but nothing cuts them — NPCs will walk into it`（有 navcut → 閉嘴；`navCut:false` → 換一句「你說不裁的，NPC 會穿過去」）。
  - **③** `removals[]`/`overrides[]` 動到的大物件，**頂面**有 vanilla navmesh（＝它本來是被走在上面的）→ 提示 NPC 會走在空氣上。
  - 座標系天生對得上（內裝 cell-local、外景 world ＝ `PlacementSpec.Position` 的兩個 frame），**零轉換**。外景查 **3×3 鄰域**（點在 cell 邊界時常被鄰居的網格蓋住，不查會誤報）。跳過帶 `Deleted` flag 的三角形。
- **✅ T1.2** 門檻與開關：`navmesh.minFootprint`(10000 units²)＋`minHeight`(100) → 只有「真的擋得住路」的東西才進①（椅子 60×60=3600、木桶 54×54 → 永遠不吵）；`navmesh.warnings:false` 全關；`placements[].navmeshCheck:false` 單筆豁免（**唯一正當用途＝刻意 park 在網格外、等腳本 MoveTo 進場的 ACHR**，`livingNpcs` 的 off-stage 停車位就是——這個 false positive 是本次實作抓出來的）。
- **✅ 驗收〔離線〕**：`NavmeshCheckTests.cs`（雜物零警告／站好零警告／離網格警告／浮空警告／有 navcut 閉嘴／**spike spec 本身必須零警告**）。**無 Skyrim.esm ＝ 全部沉默**（「不知道」永遠不當成「有問題」）。
- 🎮 無需實機。**P1 做完，症狀②從「NPC 為什麼不動？」變成 build 時的一行字。**

### P2 — cut：讓擺出來的建築真的擋住 NPC

- **✅ T2.0（已出貨，🎮 等實機）— 路線 A：L_NAVCUT 體積**。契約與生成端全部落地（`Spec.NavCuts.cs` / `Generator.Build.NavCuts.cs`，欄位形狀見 §7-2）；產物 `~/skyrim_mods/mine/ModForgeNavcutSpike.zip`（`examples/navcut_spike_spec.json`），**驗收步驟在 [wait_todo/ingame-tests](../../wait_todo/ingame-tests.md)**。
  - **實驗設計（重點是「一眼看得出成敗」＋去除混淆變因）**：白漫大街（`WhiterunWorld` = `Skyrim.esm:0x01A26F`）上擺**兩條完全相同的車道**，相距 800 單位——同樣的 NPC、同樣的 patrol package、同樣相距 310 單位的兩顆 marker、同樣 4 根告示牌標出屏障線。**唯一的差別：A 道中間有一顆 navcut box，B 道沒有。**
    - **不放實體牆**（本來想放，但 NPC 撞牆會 slide、會沿牆滑走 → 「它繞過去了」變成可疑的偽陽性）。告示牌只有 18×18，NPC 直接從中間走過去，**只有那顆看不見的盒子能造成差異**。
    - box：中心 (21750, −7625, −3510.6)、size 520×140×220、padding 32 → 實際 XPRM Bounds **584×204×284**；蓋住 7 個三角形重心、與 12 個三角形相交。204 單位厚的禁區遠寬過 NPC 的一步，任何路徑都跨不過去。
    - **14 個座標全部是讀 Skyrim.esm 的 navmesh 挑的**（三角形內插高度）——marker 不會貼地，猜 z ＝ patrol 靜默失效（這正是 `patrol_spec` 當年 round-1 掛掉的原因），而 P1 的檢查現在就是防這個的工具（spike spec 跑出來零警告，有測試盯著）。
  - **判讀（設計成對兩種可能的引擎實作都成立）**：**TEST 的走法只要和 CONTROL 有任何不同**（繞開、拒絕穿越、貼著盒子邊緣走）⇒ navcut 有效，**症狀①結案，T2.1–T2.2（NAVM cut）整個不必做**。**兩隻都直直走過去** ⇒ L_NAVCUT 對我們無效，**路線 A 被證偽**，回頭走下面的 NAVM cut。
  - 附帶測：console `tnm`（toggle navmesh info）在 SSE 還能不能畫出網格——能的話往後每階段的實機驗收都快十倍。

- **契約（已落地）**：頂層 `navCuts[]`，與 `removals[]`/`overrides[]`/`references[]` 並列（同一個家族：**碰既有記錄的操作住頂層**）。**最終形狀見 §7-2**（原草案的 `shape`/`center` 欄位換成 `position`（＝中心）/`size`（＝全尺寸）/`padding`，並多了 `placements[].navCut` 的自動三態）。
- **後備實作：NAVM cut（`Generator.Build.NavCuts.cs`，T2.1–T2.2——⚠️ 只有 T2.0 失敗才做）**：
  1. 解出目標 cell 的 NAVM（`ICellGetter.NavigationMeshes`）→ `GetOrAddAsOverride`。
  2. 對每個 triangle：重心（或三頂點）落在 cut 體積內 → `Flags |= NavmeshTriangle.Flag.Deleted`。**不刪陣列元素、不重編號、不碰 EdgeLinks/DoorTriangles/Grid**（索引全部不變 → 鄰居的 EdgeLink 依然正確 → **不必碰鄰居 cell**）。
  3. NAVI：先**不碰**（mesh FormID 沒變，vanilla NVMI 條目仍然描述它）——**這是 U4，要實機驗**。若不行，退路＝ override `012FB4` 並補一筆該 mesh 的 NVMI（`Unknown = 0x00` ＝ 「非 island、已修改」，這是真實 mod 的寫法）。
  4. build 摘要印 `navCuts: 1 cut, 14 triangles disabled in NAVM 0C9064`。
- 驗收〔離線〕：`navdiag` 顯示 14 個 triangle 帶 Deleted flag、其餘位元組與 vanilla 一致（**只有 flags 欄變**）；NVNM 長度不變。
- 驗收 🎮：白漫城內擺一面牆＋`navCuts` → NPC **繞開**它（而不是走進去）。**這一步證明 U2（引擎尊重 Deleted flag）。** 若 U2 不成立 → 退路是「真刪 triangle」（只限**內裝**，edgeLinks=0，只要修 DoorTriangles 的 index；外景則放棄 cut，改走路線 A/D）。

### P3 — add + link：讓 NPC 走上新蓋的東西（解症狀②的正解）

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
| **U1** | Mutagen NVNM 讀寫無損 | 全部 | ✅ **已驗**（離線 byte-diff，2 個樣本 IDENTICAL） |
| **U2** | 引擎的尋路**尊重 triangle 的 Deleted flag**（不走它） | **P2 的全部** | 🎮 P2 驗收（白漫擺牆）。不成立 → 內裝改真刪＋修 DoorTriangle index；外景退回路線 A/D |
| **U3** | 改幾何後把 NavmeshGrid 換成 divisor=1 單桶（或自建 divisor² 桶）引擎能接受 | P3 | 🎮 P3 驗收；先用 `navdiag` 確認自建 grid 的 byte layout 與 vanilla 同構 |
| **U4** | override 既有 NAVM 時**不必**碰 NAVI（vanilla NVMI 條目仍有效） | P2/P3 的複雜度 | 🎮 P0/P2 驗收時觀察。不成立 → 補 NVMI（`Unknown=0x00`），機制已有（`WriteNaviInfoMap`） |
| **U5** | 只改自己這張 mesh、不重編號 → **鄰居 cell 的 NAVM 不必動** | 外景可行性 | 🎮 P2/P3 外景驗收（跨 cell 邊界走一趟） |
| **U6** | **路線 A**：L_NAVCUT 體積在**我們自己的 patch esp** 裡也照樣被引擎裁（HearthFires 是 esm，且是 vanilla 自家系統） | 症狀①的成敗 | 機制本身**已由 vanilla 證實**（HearthFires 1220 筆、CK wiki 明載、Mutagen 全表達得出）。剩下的只是「我們照抄能不能動」＋四條限制的實務調校。🎮 T2.0（半天） |
| **U6b** | ~~複製 STAT base ＋ 加 Obstacle flag 就會裁~~ **❌ 已否證** | — | **flag 是兩段閘門的一半**：還要碰撞體所在的 collision layer 帶 `NavmeshObstacle`。vanilla 55 個 COLL 只有 6 層帶（L_ANIMSTATIC/CLUTTER/PROPS/DEBRIS_LARGE/TRANSPARENT_SMALL_ANIM/**L_NAVCUT**），**L_STATIC 不帶** → 對一般靜態物加 flag ＝ 無效。**這條若沒查出來會白燒好幾天** |
| **U7** | **ESL**：override vanilla NAVM（FormID 不變）在 ESL 裡安全；但**新建** NAVM 記錄從 ESL 載入可能壞 | 編輯器 patch 預設 `esl: true` | 保守規則：`navCuts` 只 override → 維持 esl；一旦要新建 NAVM（自建 worldspace）→ 比照既有 LAND 守則強制 `esl:false`（`Generator.Validate.World.cs:131`）。🎮 待驗 |
| **U8** | 沒和 vanilla 網格相連的「孤島」navmesh，NPC 能否在上面 sandbox（travel 出不去可接受） | P3 的降級路徑 | 🎮 P3 驗收。**已知：完全沒有網格 ＝ NPC 什麼都不做**（sandbox/travel/follow/combat 全掛；SE 還要求 actor 本身站在 triangle 上尋路才會啟動）。`PathToReference` **不繞過** navmesh（走同一套尋路，一樣失敗）；只有 `TranslateTo`/`MoveTo` 能無視，但那是腳本用的位移、不是 AI |
| **U9** | 外景 NAVM override 會拉出 **WRLD override**——會不會踩到既有的地圖渲染坑（[worldspace-override-map-render-fields](../../docs/engine-internals.md)：缺 EDID/RNAM 會白地圖、帶 OFST 會壞） | 外景全部 | 已初步觀察：Mutagen deep-copy 的 WRLD 帶 EDID/TNAM/UNAM、**不帶 OFST**（正確）。且 `removals[]` 的外景路徑早已實機驗過同一條 chain。🎮 P0 的外景 no-op 一併確認地圖正常 |
| **U10** | 我們的 NAVM override 與**其他 mod 的同一張 NAVM override** ＝ 整筆記錄後蓋前（last wins） | 相容性 | 無法避免（NAVM 沒有加法式合併）。處置＝ build 警告「這張 navmesh 已被 X.esp override，你會覆蓋它」（houseCARL 可查）；文件說明 |

---

## 7. 拍板紀錄 ＋ 還沒拍的

### ✅ 1. 順序（2026-07-12 使用者拍板）

**P1（診斷）→ T2.0（L_NAVCUT spike）→ P0（NAVM no-op 上機）→ P3（add+link）→ P4（DLL 讀 live navmesh）。** 前兩項已做完（見頂部落地狀態）。

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

**還有一條需要你點頭**：`autoNavCuts` **預設 true** 表示既有 spec 只要在 vanilla 世界擺了大體積物件，下次 build 就會多出 navcut REFR。這正是拍板要的行為（也正是那些 mod 一直有的 bug），但它會**改動既有已出貨 mod 的產物**。若你想「先觀望到 T2.0 實機過了再開」，把 `Spec.NavCuts.cs` 的 `AutoNavCuts` 預設改成 `false` 即可（一行）。

### 3. 內裝先行？（未拍板）

P3（NAVM add）**先只支援內裝**（EdgeLinks=0，安全灘頭堡），外景等內裝實機過了再開？外景是編輯器的主場，但也是地雷區（跨 cell EdgeLink）。

### 4. P3 的三角化要不要引 DotRecast？（未拍板）

C# 的 Recast port，NuGet 2026.1.3 活躍；CK 自己的 auto-generate 就是 Recast。我的傾向：**先不要**——用 DLL 的射線取樣（P4）拿真實地板高度，配手寫的凸多邊形三角化就夠；DotRecast 需要餵三角湯（＝要先解 NIF havok 幾何），是另一條產業鏈。

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
