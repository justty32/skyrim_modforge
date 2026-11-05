# scene-capture-bridge backlog — 三條線（續）：B `sc nav`、C `sc ed`

← [backlog](../backlog.md)（入口）

### B. `sc nav`：在遊戲裡畫導航網格（navmesh plan 的 P4）

**訴求原文（2026-08-20）**：「站在某個地方，施展法術，就可以創造一個點，然後再施展法術，就可以讓這個點跟其他點連接。」

**現況：生成端已經全部就緒且實機驗過，遊戲內採集端一個字都沒寫。** 見 [plans/navmesh.md](../../navmesh.md)：

| | 狀態 |
|---|---|
| P0 地基（plugin override vanilla NAVM，引擎真的採用） | ✅ 🎮 **實機 PASS**（2026-07-12，白漫 10 張 NAVM byte-identical） |
| **P3 `navPatches[]`（append 三角形 ＋ 與 vanilla 網格雙向縫合）** | ✅ 🎮 **內裝實機 PASS**（2026-08-11，Bannered Mare，兩名相反方向的 Travel actor 都跨過新↔舊 seam） |
| **P4 遊戲內採集（本條）** | ❌ **未動** |

DLL 端目前只有 [Markers.h](../../../../../scene-capture-bridge/src/Markers.h) 的 `kind: navmesh`（註解寫著 "ordered kinds (navmesh) rely on it"，`seq` 早就留好了），但那只是 `annotations[]` 裡的一個字串——**ModForge 端沒有任何程式碼消費 `kind=navmesh`**（已 grep 確認）。

> #### 🔴 要先改掉的心智模型：navmesh 不是 waypoint graph
>
> **兩個點連一條線不產生任何可走面。最少三個點才成一個三角形。** ModForge 的契約是 `navPatches[].polygon`：**3+ 個角點、順序＝周長、凸多邊形、fan triangulation**、`linkTo:"auto"`。
>
> ⇒ 正確的施法手勢不是「點—點—連線」，而是 **走一圈、每個轉角施一次法記一個角點、最後施一次法「收口」** → 一個 polygon → 一塊可走的地板。
>
> 這剛好對得上既有原語：`Markers::PlaceAtPlayer` 的註解本身就寫著 "the navmesh-vision primitive: record where I stand"，而且**站著取高度比射線可靠**（腳底 z 就是準的）。

> #### 🔴 P4 真正的技術重點是「吸附」，不是「記點」
>
> `linkTo:"auto"` 的縫合條件是**完整邊重合**：新三角形的邊界邊要找到既有三角形的邊、**兩個端點都在 eps 內**、且**唯一匹配**，才雙向設鄰居索引。**不是「靠近就縫」——縫不上就是 failure，整筆不落地**（[navmesh-p3.md](../../navmesh-p3.md) Task 2）。
>
> ⇒ 玩家怎麼知道角點要放哪才對得上既有網格的頂點？答案是 **T4.1 得先做**：DLL 讀 live `RE::NavMesh`（是 `TESForm`，`vertices`/`triangles`/`portals` 全讀得到），把附近的頂點畫出來、讓標點吸附上去。**記座標反而是整條裡最簡單的部分。**

**任務**

- **B1〔spike，前置〕T4.1 讀 live navmesh**：`RE::NavMesh` → 玩家附近的 vertices/triangles。兩個立即用途：① `sc nav` 的頂點吸附；② **「你腳下有沒有網格」的即時提示**——把 [navmesh.md](../../navmesh.md) P1 的離線 build 警告提前到你人還站在那裡的時候（免費的加分項）。**B 的其餘部分全部壓在這顆 spike 上。**
- **B2〔一輪〕`sc nav` 模式**：新模式 ＋ 一本登記簿 ＋ co-save record ＋ 一個 UI 頁（點列表、收口、逐點刪除、polygon 逐筆 revert）。全部與既有七個模式同構，**無新架構**。動作鍵走既有 ini 機制；施法觸發共用 A2。
- **B3〔小〕匯出**：新頂層 `navPatches[]`（＝直接吐 ModForge 既有契約，**C# 端零改動**）。
- **B4〔小，但別省〕遊戲內 guard**：凸性＋自交當場擋掉。C# 端 `ValidateNavPatches` 已有完整驗證（凸性/自交/共線/零長邊/epsilon），但**不要讓玩家走完一圈才在 build 時被退件**。
- **⚠️ 範圍限制：第一版只能在 vanilla 內裝用。** 外景的跨 cell EdgeLink 在 P3 被明確排除（[navmesh.md §7-3](../../navmesh.md)：「內裝實機過了才開外景」）。白漫大街上標一塊地——**還不行**。
- 🎮 驗收：內裝走一圈標出一塊平台 → 匯出 → build → `navdiag` 顯示新舊三角形互為鄰居且既有 index 全未變 → NPC 走得上去也走得下來。

---

### C. `sc ed <xx>`：編輯既有物件的「狀態屬性」

**2026-08-20 使用者重提**（「除了要可以調整物品的大小角度外，還要可以調整物品的屬性：火把燃燒與否、門開關與否」）——與 2026-07-14 的 ① 是同一條，當時拍板「先做 ②」，② 已完成，**本條解凍**。完整形狀見下面 ①（[open-directions](open-directions.md)），不重複；這裡只補這次確認的三件事：

1. **架構已定、不必再想**：一本新 registry（同 Eraser/Overrides/Referrer 的 co-save 模式）記「哪個 ref 的哪個屬性被改成什麼」 → 匯出成 `overrides[]` 的**新欄位** → ModForge 端寫進 REFR。UI 走既有 `UI.Fields` bound-field。指令形狀比照 `sc ed ax`。
2. **🔴 長桿子是逐個屬性的引擎真相，不是架構**。門 open/locked（REFR record flag vs lock data）、**火把亮/滅（多半不是一個 flag——vanilla 牆上火把常是「另一顆 base」或帶 light child，要先 decode 才知道能不能 runtime toggle）**、initially-disabled、ownership、enable-parent、linked-ref、count。**每一個都是獨立的解碼工作，會做一半就卡住。**
3. **⇒ 動工前先挑「第一批支援哪幾個屬性」**，並且**先解碼再寫碼**：挑一個屬性 → 讀 vanilla 記錄怎麼存的 → 確認 runtime 改得動 → 才進 registry。建議第一批只收**確定是 REFR 上的旗標/資料**的（門的 open/locked、initially-disabled、ownership、count），**火把單獨當一顆 spike**（它是這批裡唯一可能根本不是 REFR 屬性的）。

---
