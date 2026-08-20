# scene-capture-bridge — 之後再做（backlog）

← [README](README.md)（現況導航）｜[phases](phases.md)（已落地實作記錄）｜[appendix](appendix.md)（細摳原文＋驗證清單）

**活躍成長區**：新想法都記這。做完就搬進 [phases.md](phases.md)（已落地實作記錄，標日期/DLL crc），從這裡刪除。

---

## ❌ 已否決（別再做第三次）

- **遊戲內 rebind（面板抓鍵改動作鍵）——放棄，改走 `.ini`**（使用者 2026-07-12 實機後拍板：「這太麻煩了，先隱藏掉這個功能吧，我們之後把他擺進 .ini 設定」）。**為什麼不再試**：面板（SKSE Menu Framework）**不暫停遊戲**，所以「抓玩家想綁的那顆鍵」永遠在跟**玩家手上還按著的鍵**搶同一條輸入串流——人剛用滑鼠點完 `Rebind`，手多半還在 WASD 上。**兩次嘗試、兩種設計都在實機失敗**：① P5（2026-07-11）armed 後來者不拒 → 綁成 W；② 重作（`ddf6324`，2026-07-12）加了保留鍵黑名單＋按下再放開才 commit → **使用者實機回報仍失敗**。現況＝**`SceneCaptureBridge.ini`**（SKSE 資料夾、缺檔自動生成、寫鍵名不寫 scancode、面板一顆 `reload keys from ini`）——檔案沒有那條賽道可輸：沒有 armed 狀態、沒有 input sink、沒有時序。實作與完整驗屍見 [phases.md](phases.md) 該兩節；抓鍵狀態機已從 `Modes`／`plugin.cpp` 移除（要看舊碼去 git `ddf6324`）。

## 🗺️ 2026-08-20 使用者拍板：三條線全開

三條出自使用者同一次提問（「施法叫出物品欄式選單」「導航網格能不能在遊戲中編輯」「還要能調物件屬性——火把燃燒與否、門開關與否」）。**C 是 2026-07-14 就記在下面 ①、當時排在 ② 之後、一直沒動工的那條**，這次一併重開。

**建議順序：A 的兩顆 spike（半天，會決定 A 剩下的值不值得做）→ B（一輪，值最高且無未知數）→ C（最長，動工前先挑第一批屬性）。**

---

### A. 施法叫出選單 ＋ 物品欄式預覽 ＋ 最愛

**訴求原文（2026-08-20）**：「使用一個技能後，跳出一個跟玩家物品欄一樣的介面，每個可擺放的東西都是其中的一個物品，跟原版一樣可以預覽其狀況，並且可以加入最愛。」

**這是 2026-07-14 那題的第二次提出**（見下面 ②）。拆成四件事，**三件已有答案、只有一件要做**：

| 成分 | 判定 |
|---|---|
| **技能觸發** | ✅ **要做，小工**。DLL sink `RE::TESSpellCastEvent`（拿得到 caster ＋ spell FormID）就能把施法接到既有模式切換，**零 Papyrus**。esp 多一顆 SPEL/MGEF。與 `sc <mode>` ＋ ini 動作鍵並存，不取代 |
| **「跟物品欄一樣的介面」** | ❌ **維持否決**（見下 ②，理由不變：STAT/TREE/FURN/ACTI/MSTT 進不了 inventory）。繞道評估見下框——**評估過了，不做** |
| **「跟原版一樣可以預覽」** | ✅ **這才是訴求的核心**，且有便宜版＝下面 ② 的 `Inventory3DManager` spike。**升格為 A 的主線任務** |
| **「加入最愛」** | ✅ **已經有了，叫 Palette**（目錄是全部、palette 是你留下來的那幾個，落盤跨存檔）。Browser 的 `add to palette` 就是「加入最愛」——**只要改名＋Palette 頁加星號置頂排序**，不是新功能 |

> #### 🔬 繞道評估：proxy MISC ＋ runtime 換模型（**2026-08-20 評估完畢，不做**）
>
> 唯一能真的用上 vanilla 物品欄的路：esp 生一批 `MISC` 空殼，DLL 在 runtime 改寫每個空殼的 `TESModel::model` 與 `TESFullName`（po3 / Base Object Swapper 那套手法）成目錄項目的 nif 與名字。物品欄收得下 MISC ⇒ item card 的 3D 預覽會渲染真正的 static、vanilla 最愛也能用。
>
> **技術上可行，但買到的不如失去的**：
>
> | | 現行 Browser | proxy MISC ＋ 物品欄 |
> |---|---|---|
> | 搜尋 | 空白分隔 AND，比對 name＋**model path**＋id（搜 "mountain" 直接命中 `Landscape\Mountains\*.nif`）| **沒有**。vanilla 物品欄沒搜尋框，SkyUI 也只有分類排序 |
> | 型別／plugin 過濾 | 兩個下拉 | **沒有**。全部是 MISC ⇒ 全擠在同一個「雜項」堆（form type 無法 runtime 改） |
> | 預覽 fit | 世界內 ghost，真尺寸真光照，auto-scale 到約九分之一螢幕 | item card 的距離/縮放是 INI 固定值算的，山脈 nif（10000 單位）會爆框或縮成一個點，**要逐項自動 fit** |
> | form 預算 | 0 | 每個「同時列出」的項目一顆 MISC。browsing pool 可用 runtime dynamic form（不跨存檔、每 session 重建），但**最愛要穩定 ⇒ 得 authored form ⇒ esp 要生幾百到幾千筆**；recycle proxy 會讓最愛跟著插槽跑而不是跟著物件 |
>
> **🔴 決定性的那一格是搜尋。** Browser 的索引之所以建在 model path 上，是因為 **SSE runtime 拿不到 EditorID**（`GetFormEditorID()` 對 STAT/ACTI/FURN 一律回空字串），而 model path 其實是更好的鍵。換成物品欄＝把整個編輯器最有價值的 affordance 丟掉，換一個好看的框。**⇒ 不做。若以後真的想要 vanilla 選單的手感（手把翻頁、大圖），先跑一顆 spike：一顆寫死的 proxy MISC ＋ 一個山脈 nif ＋ 開 InventoryMenu，只看「item card 渲不渲得出來、什麼比例、轉不轉得動」——這一顆答掉 80% 的風險。**

**任務**

- **A1〔離線 spike，半天〕面板內真 3D 預覽**：`RE::Inventory3DManager::LoadInventoryItem(TESBoundObject*, ExtraDataList*)`，**STAT 也是 `TESBoundObject`**。用一顆寫死的 STAT 試，只答一個問題：**它綁不綁 item-3D render layer？** 能脫離物品欄單獨畫進 ImGui 面板就成立。不成立 ⇒ ghost 已經夠用，A 只剩 A2/A3。
- **A2〔小〕技能觸發**：sink `TESSpellCastEvent` → 進 place 模式 ＋ 開 ghost（＝ Browser `preview here` 的同一條路）。SPEL/MGEF 由 ModForge 生（`SceneCaptureTools.esp`）。⚠️ 法術自己會有施法特效/dynamic ref，要確認不會被 `ExportCell` 撈進 `placements[]`——**ghost 的哨兵機制（`ExtraTextDisplayData`）是現成的參考**。
- **A3〔小〕最愛語意**：`add to palette` 改名為「加入最愛」；Palette 頁加星號／置頂排序。**零新狀態**（Palette 本來就落盤跨存檔）。
- 🎮 驗收：施法 → 選單/ghost 起來 → 擺一個 → 匯出仍是零外洩。

---

### B. `sc nav`：在遊戲裡畫導航網格（navmesh plan 的 P4）

**訴求原文（2026-08-20）**：「站在某個地方，施展法術，就可以創造一個點，然後再施展法術，就可以讓這個點跟其他點連接。」

**現況：生成端已經全部就緒且實機驗過，遊戲內採集端一個字都沒寫。** 見 [plans/navmesh.md](../navmesh.md)：

| | 狀態 |
|---|---|
| P0 地基（plugin override vanilla NAVM，引擎真的採用） | ✅ 🎮 **實機 PASS**（2026-07-12，白漫 10 張 NAVM byte-identical） |
| **P3 `navPatches[]`（append 三角形 ＋ 與 vanilla 網格雙向縫合）** | ✅ 🎮 **內裝實機 PASS**（2026-08-11，Bannered Mare，兩名相反方向的 Travel actor 都跨過新↔舊 seam） |
| **P4 遊戲內採集（本條）** | ❌ **未動** |

DLL 端目前只有 [Markers.h](../../../../scene-capture-bridge/src/Markers.h) 的 `kind: navmesh`（註解寫著 "ordered kinds (navmesh) rely on it"，`seq` 早就留好了），但那只是 `annotations[]` 裡的一個字串——**ModForge 端沒有任何程式碼消費 `kind=navmesh`**（已 grep 確認）。

> #### 🔴 要先改掉的心智模型：navmesh 不是 waypoint graph
>
> **兩個點連一條線不產生任何可走面。最少三個點才成一個三角形。** ModForge 的契約是 `navPatches[].polygon`：**3+ 個角點、順序＝周長、凸多邊形、fan triangulation**、`linkTo:"auto"`。
>
> ⇒ 正確的施法手勢不是「點—點—連線」，而是 **走一圈、每個轉角施一次法記一個角點、最後施一次法「收口」** → 一個 polygon → 一塊可走的地板。
>
> 這剛好對得上既有原語：`Markers::PlaceAtPlayer` 的註解本身就寫著 "the navmesh-vision primitive: record where I stand"，而且**站著取高度比射線可靠**（腳底 z 就是準的）。

> #### 🔴 P4 真正的技術重點是「吸附」，不是「記點」
>
> `linkTo:"auto"` 的縫合條件是**完整邊重合**：新三角形的邊界邊要找到既有三角形的邊、**兩個端點都在 eps 內**、且**唯一匹配**，才雙向設鄰居索引。**不是「靠近就縫」——縫不上就是 failure，整筆不落地**（[navmesh-p3.md](../navmesh-p3.md) Task 2）。
>
> ⇒ 玩家怎麼知道角點要放哪才對得上既有網格的頂點？答案是 **T4.1 得先做**：DLL 讀 live `RE::NavMesh`（是 `TESForm`，`vertices`/`triangles`/`portals` 全讀得到），把附近的頂點畫出來、讓標點吸附上去。**記座標反而是整條裡最簡單的部分。**

**任務**

- **B1〔spike，前置〕T4.1 讀 live navmesh**：`RE::NavMesh` → 玩家附近的 vertices/triangles。兩個立即用途：① `sc nav` 的頂點吸附；② **「你腳下有沒有網格」的即時提示**——把 [navmesh.md](../navmesh.md) P1 的離線 build 警告提前到你人還站在那裡的時候（免費的加分項）。**B 的其餘部分全部壓在這顆 spike 上。**
- **B2〔一輪〕`sc nav` 模式**：新模式 ＋ 一本登記簿 ＋ co-save record ＋ 一個 UI 頁（點列表、收口、逐點刪除、polygon 逐筆 revert）。全部與既有七個模式同構，**無新架構**。動作鍵走既有 ini 機制；施法觸發共用 A2。
- **B3〔小〕匯出**：新頂層 `navPatches[]`（＝直接吐 ModForge 既有契約，**C# 端零改動**）。
- **B4〔小，但別省〕遊戲內 guard**：凸性＋自交當場擋掉。C# 端 `ValidateNavPatches` 已有完整驗證（凸性/自交/共線/零長邊/epsilon），但**不要讓玩家走完一圈才在 build 時被退件**。
- **⚠️ 範圍限制：第一版只能在 vanilla 內裝用。** 外景的跨 cell EdgeLink 在 P3 被明確排除（[navmesh.md §7-3](../navmesh.md)：「內裝實機過了才開外景」）。白漫大街上標一塊地——**還不行**。
- 🎮 驗收：內裝走一圈標出一塊平台 → 匯出 → build → `navdiag` 顯示新舊三角形互為鄰居且既有 index 全未變 → NPC 走得上去也走得下來。

---

### C. `sc ed <xx>`：編輯既有物件的「狀態屬性」

**2026-08-20 使用者重提**（「除了要可以調整物品的大小角度外，還要可以調整物品的屬性：火把燃燒與否、門開關與否」）——與 2026-07-14 的 ① 是同一條，當時拍板「先做 ②」，② 已完成，**本條解凍**。完整形狀見下面 ①，不重複；這裡只補這次確認的三件事：

1. **架構已定、不必再想**：一本新 registry（同 Eraser/Overrides/Referrer 的 co-save 模式）記「哪個 ref 的哪個屬性被改成什麼」 → 匯出成 `overrides[]` 的**新欄位** → ModForge 端寫進 REFR。UI 走既有 `UI.Fields` bound-field。指令形狀比照 `sc ed ax`。
2. **🔴 長桿子是逐個屬性的引擎真相，不是架構**。門 open/locked（REFR record flag vs lock data）、**火把亮/滅（多半不是一個 flag——vanilla 牆上火把常是「另一顆 base」或帶 light child，要先 decode 才知道能不能 runtime toggle）**、initially-disabled、ownership、enable-parent、linked-ref、count。**每一個都是獨立的解碼工作，會做一半就卡住。**
3. **⇒ 動工前先挑「第一批支援哪幾個屬性」**，並且**先解碼再寫碼**：挑一個屬性 → 讀 vanilla 記錄怎麼存的 → 確認 runtime 改得動 → 才進 registry。建議第一批只收**確定是 REFR 上的旗標/資料**的（門的 open/locked、initially-disabled、ownership、count），**火把單獨當一顆 spike**（它是這批裡唯一可能根本不是 REFR 屬性的）。

---

## 仍未做

### 🆕 使用者 2026-07-14 提的兩個新方向（**先做 ②**；① 已於 2026-08-20 解凍，見上面 C）

- **① `sc ed <xx>` —— 編輯既有物件的「狀態屬性」**（火把燃燒/熄滅、門開/關…）。現在 `sc ed` 只能動 **transform**（位移/旋轉/縮放）；使用者要的是同一個編輯模式裡改**別的 REFR 屬性**，指令形狀比照 `sc ed ax`（`sc ed` ＋ 一個兩字母子模式）。
  - **形狀已清楚**：一本新 registry（同 Eraser/Overrides/Referrer 的 co-save 模式）記「哪個 ref 的哪個屬性被改成什麼」 → 匯出成 `overrides[]` 的**新欄位** → ModForge 端寫進 REFR。UI 走既有 bound-field 面板。
  - **難的不是架構、是每個屬性的引擎真相**，得逐個解碼：門 open/locked（REFR record flag vs lock data）、**火把亮/滅（多半不是一個 flag——vanilla 牆上火把常是「另一顆 base」或帶 light child，要先 decode 才知道能不能 runtime toggle）**、initially-disabled、ownership、enable-parent、linked-ref、count。
  - **排序**：使用者拍板**先做 ②**，本條之後再開工（動工前先挑「第一批支援哪幾個屬性」）。

- **② 物件目錄瀏覽器 ＋ 預覽 —— ✅ 主體已做（2026-07-14，DLL `ba3e2089`，已部署待實機，見 [phases](phases.md)）**。「借用物品欄 UI」**已否決**（只吃可攜帶 form type，山脈/樹/家具進不了 inventory ⇒ 最需要的那類正好不支援）；改成 **Browser 面板頁 ＋ 世界內 ghost 預覽**。剩下的**加分項**：
  - **面板內真 3D 預覽（spike）**：`RE::Inventory3DManager`（物品欄那顆會轉的 3D 模型）介面是 `LoadInventoryItem(TESBoundObject*, ExtraDataList*)`——**STAT 也是 TESBoundObject**，所以預覽技術也許能脫離物品欄單獨用。不確定（綁 item-3D render layer），**ghost 已經夠用，這是錦上添花**。**→ 2026-08-20 升格為上面 A1**（使用者第二次提出同一個訴求，這顆 spike 就是它的核心）。
  - **離線 catalog json：✅ ModForge 產生端已完成（2026-08-12），DLL 消費端待主力機**：runtime **拿不到 EditorID**（SSE 不留）；ModForge `catalog build` 現保存明示 load order、EDID/FULL/model path/來源，`catalog export-json` 依 FormKey 只吐 winner，v1 schema + atomic write 已離線測完。剩 bridge DLL 讀檔、以 durable id 合併到 runtime catalog search，需另題實作並實機驗收。
  - **清單改用 ImGui ListClipper**：現在是「上限 500 筆＋明講截斷」（不想賭 wrapper 的 `ImGuiListClipper` struct layout）。clipper 能一路捲完三萬筆——實機證明頁面沒問題後再換。
  - **離線批次縮圖**：`nifexport`（Godot 編輯器那套）→ PNG → 面板 `LoadTexture` ⇒ CK 對等的 2D 縮圖牆。**幾萬張太重**，只有在 ghost 不夠用時、且只對特定類別批次生。

- **ModForge 端要不要過濾玩家的「管線 perk」（2026-07-13，接在拍板 (b) 之後）**：橋端已改成**全收**（base 12 顆 ＋ addedPerks，去重取高 rank，DLL `e19ad4ca`）——依使用者拍板「完全複製優先，到時候讓 modforge 處理」。所以**取捨在消費端**：`AllowShoutingPerk`／`VampireFeed`／`AlchemySkillBoosts`／`DBWellFitted` 這些是 vanilla **Player 記錄專用**的管線 perk，鑄到一個 NPC 分身身上多半是死資料（但 `AllowShoutingPerk` 之類若要讓分身用吼聲就需要）。**候選**：(i) 照抄不動（現況）；(ii) build 時印一行 INFO 點名這幾顆；(iii) spec 給個 opt-out。**還沒做，等有實際困擾再動。**

- **`sc cap` 物件類 vs `sc pk` 分工（使用者再想，先照舊）**：`sc cap` 記 NPC/player 含全身物品＋extra data（v7 已落地）；物件類 capture 與 `sc pk` 滴管感覺功能重複，使用者還要想想——**傾向仍記錄**，暫不動。
- **📌 導航網格（navmesh）——「超重要，之後得開始考慮」（使用者 2026-07-11 晚）**：編輯器流程目前完全沒碰 navmesh——擺出的建築/障礙物會擋住 vanilla navmesh 但 NPC 照原網格走（穿模/卡住），marker 生的 NPC 若落在無網格處也不會動。ModForge 已有程式化 navmesh 能力可接（custom worldspace NAVM＋NAVI additive override Skyrim.esm:0x12FB4 in-game 驗過，見 idea/asset-pipelines/map-scene/geometry.md 一帶＋Vigilant.esm 解碼參考）；難點在**編輯 vanilla cell**：要 override 既有 NAVM（cut/finalize 語意）而不只是新建。方向未定（DLL 端記錄擺放物 footprint → ModForge 端裁切？或先只處理「新增小平台補網格」？），需要時開獨立 plan。
  - **✅ 已開獨立 plan（2026-07-12）：[plans/navmesh.md](../navmesh.md)——兩個結論同日皆已 🎮 實機 PASS，不再只是離線推論**：① **「擺的東西擋住 NPC」根本不必改 navmesh，已結案**：用 vanilla 的 **L_NAVCUT 碰撞體積**（`CollisionMarker` 0x000021 ＋ `CollisionLayer=49` ＋ Primitive box，**HearthFires 蓋房子用了 1220 筆**）就能 runtime 裁切，純 Mutagen 一筆 REFR——白漫大街 TEST/CONTROL 對照實驗實機證明有效（TEST 繞開、CONTROL 直穿），`autoNavCuts` 已預設開（⚠️ 光加 Obstacle flag 無效——L_STATIC 不是 NavmeshObstacle 層）。② **「NPC 走上新平台」非寫 NAVM 不可，而 override vanilla NAVM 的地基已實機驗證可行**（no-op override 裝上後白漫 NPC 一切正常；離線 byte-diff 早已 IDENTICAL；USSEP 807 筆真的這麼幹；NAVI 是加法式 merge 不是地雷）；鐵律＝**永不重新編號 triangle**（鄰居的 EdgeLink 存的是你的 triangle index）。**現況（2026-08-20 更新）：P1/T2.0/P0/P3 全部做完且 🎮 過關**（P3 內裝 edge-to-edge 2026-08-11 實機 PASS；原訂的 P2 NAVM-cut 備案因 T2.0 PASS 而整段作廢）——**唯一還沒動的就是 P4 遊戲內採集，已開成上面的 B**。
- **外部 mod 依賴——剩下的兩個候選**（(a) 可見性＋(b) 宣告式 `requires:` 契約已做，見 [phases](phases.md)）：**(c) modlist / load order 快照**（把當下 MO2 `plugins.txt` 存進 spec 旁，之後能重現「當時是在哪個 load order 上吸的」）；**(d)「依賴檢查」指令**（給一個 esp ＋ 一份 load order → 回報缺什麼；(b) 檢查的是 spec↔build，這個檢查的是 build↔**玩家的實際安裝**，是出貨前最後一道）。兩者都要讀 MO2/遊戲側檔案，離線機做不了完整迴圈，優先度不高。
- **紅/綠半透明輪廓高亮**（使用者第二輪：`sc del dp1` 被刪物件紅框、`sc pl dp1` 新增物件綠框，顏色/透明度 Settings 可調）——**較難、非必做**（需 render/shader 或 highlight 效果）。
- marker 編輯視窗下拉：寶石種類 ＋ 發光開關（需 SceneCaptureTools.esp 多個 ACTI 變體或動態換 model，較大工程）。
