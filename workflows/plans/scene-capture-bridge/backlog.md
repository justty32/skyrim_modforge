# scene-capture-bridge — 之後再做（backlog）

← [README](README.md)（現況導航）｜[phases](phases.md)（P1–P6 落地）｜[appendix](appendix.md)（細摳原文＋驗證清單）

**活躍成長區**：新想法都記這。做完就從「仍未做」移到「已做」並標日期/DLL crc。

---

## ✅ 已做（旋轉 per-axis 還原 ＋ palette replace，2026-07-12，DLL `9cae7ff1`，**未部署**·待實機）
- **旋轉子模式的歸零鍵改 per-axis（使用者實機後提）**：`sc ed ax` 下**每組的中間鍵只管自己那一組軸**——**2＝還原 pitch（1/3）、5＝還原 yaw（4/6）、8＝還原 roll（7/9）**。語意＝**revert 回進編輯前的該軸原值**（`g.origAngle.<axis>`），**不是設成 0**（物件本來就可能有角度）。原本三鍵都是「整個角度還原」（全軸 `origAngle`）。移動模式的 numpad 5（＝復原整個編輯）**不動**（P7 的 per-mode 行為）。`Editor.cpp` 的 `kBack`/`kSelect`/`kFwd` 三個 case；每鍵各自的 DebugNotification。
- **palette 檔案 I/O 兩改**：① **檔內順序＝面板順序**（最上面那筆排 json 第一筆）——`SlotsJson()` 反向寫、`ParseSlots()`＋`Adopt()` 反向插；`load from file (append)` 的新項因此**落在列表最上面**且保留檔內順序（面板最新排頂的既有慣例）。② 新鈕 **`replace from file`**（`Palette::ReplaceFromFile`）＝**清空現有插槽再載入**；檔案不存在／不可讀／無可用插槽 ⇒ **不清**（不會誤把磁碟持久的 palette 清光）。三鈕並列：`load from file (append)` / `replace from file` / `save to file` ＋一行說明。
- ⚠️ 舊 `scene-capture-palette.json`（舊格式＝反序）讀進來順序會**上下顛倒一次**，之後穩定；欄位完全相容。

## ✅ 已做（`sc capp` 直接吸玩家，2026-07-12，DLL `f8afc170`，待實機）
- **`sc capp [Label]` ＝直接吸玩家**（去 PROTEUS 化）：玩家 chargen 就在 base TESNPC（`Skyrim.esm:0x000007`），DLL 直讀 → `capturedNpcs[]`。**PROTEUS 中介整條移除**（clone 自報 L1／50-50-50、不寫 tintLayers、outfit 空殼＝裸體，三個缺陷一次解掉）。玩家 perk 讀 `PlayerCharacter::addedPerks`（玩家 base 的 perk array 是空的）。
- **顯式數值（所有 actor，不只玩家）**：`GetBaseActorValue` 取 H/M/S ＋ AV 6..23 的 18 技能（＝Mutagen `Skill` enum 序）→ 匯出 `health/magicka/stamina/skills[18]`。ModForge 消費**優先序＝顯式 ＞ class autocalc**（有顯式值就寫 DNAM、`autoCalcStats` 關；沒有才走舊路 → **舊 capture json 原樣相容**）。
- **`sc capc [Label]` ／ `sc capp [Label]` 標號**：→ `editorId: "MFCap_<label>"`，「顯式 editorId 優先」即身份機制（同 label 再吸＝同一筆）。⚠️ label 走**未 `Lower()` 的 raw 參數**（大小寫保留）——`pkc`/referrer 動工時照抄這條。
- co-save **SCCP v8**（+label +H/M/S +skills；v≤7 照讀）。C# 端 923 測綠。詳見 [plans/player-capture-capp.md](../player-capture-capp.md)。
- **🔴 部署鐵律（血的教訓）**：遊戲跑著時 `cp` 就地覆寫 DLL ＝ **無聲暴斃、無 crash log**（`cp` 寫穿同一個 inode，而 DLL 程式碼頁是 demand-paged from that file）。一律走 `scripts/deploy.sh`（`pgrep SkyrimSE.exe` 在跑就拒絕 ＋ tmp+rename 換 inode）。

## ✅ 已做（匯出三改，2026-07-12，DLL `65f53a93`，待實機）
- **Export 檔名帶場景＋時間**：`scene-export_<cell EditorID 或 worldspace_x<X>y<Y>>_<YYYYMMDD-HHMM>.json`（`Export all` ＝ `scene-export_all-<玩家所在>_…`）。名稱 sanitize 成 `[A-Za-z0-9._-]`、截 48 字；同分鐘同場景再匯出加 `-2`/`-3`，**永不覆蓋**。⚠️ 下游 agent 別再寫死 `scene-export.json`，取資料夾裡最新一份。
- **Captures 獨立 Export 鈕**：Export 頁／Captures 頁各一顆 `Export captures` → `captures_<YYYYMMDD-HHMM>.json`，只含 `capturedItems[]`＋`capturedNpcs[]`；**場景匯出檔不再帶這兩段**。兩者都是 `ModSpec` 成員故單獨 `build` 吃得下（**ModForge C# 端零改動**）。
- **📌 Scope 反轉（NPC 移出 cell 匯出）**：`ExportCell`/`ExportAll` 掃到 actor ref 直接跳過（計 `actorsExcluded`，只進 log/面板），`placements[]` 不再有 `kind:"npc"`。NPC 交給 ModForge 按 `annotations[]`（marker）擺；真要複製某 NPC 走 `sc cap` → `capturedNpcs[]`。[spec](../../specs/ingame-scene-export-design.md) 契約節已同步（新增「2026-07-12 拍板」節，並標註推翻 2026-07-10 那條）。

## ✅ 已做（P7 backlog，2026-07-11，DLL `79e611e8`→`a46ed0b2`，待實機）
- `sc del/pk/ed er0/er1`：該模式動作鍵準星↔物理射線切換（`Modes::UseRay` per-mode，co-save SETT v3）。取代「numpad * 專用鍵才能射線」的需求。
- **`sc ed ax`（純旋轉子模式，使用者第二輪定案取代 ax0/1/2）**：ON 時 numpad 4/6＝yaw、1/3＝pitch、7/9＝roll、8/2＝角度歸零（**歸零鍵已被 2026-07-12 的 per-axis 還原取代**，見上）；OFF 時照舊位移。`Editor::g_rotateMode`，co-save。
- `sc delc`：擦除 `RE::Console::GetSelectedRef()` 選中的 ref，走 `Eraser::MarkConsoleRef`；actor 拒絕（先只做物件）。
- 編輯指向靈魂石 marker → numpad 0 commit 更新該 marker 登記簿座標（`Markers::SetTransform`），不進 overrides；orphan proxy 就地 adopt。
- palette「load from file」鈕（append）＋**「save to file」鈕**（`Palette::LoadFromFile`/`SaveToFile`，讀寫 SKSE 夾下具名檔）。（**2026-07-12 續改**：append 排最上＋新增 `replace from file`，見上。）
- **Export「Export all (loaded cells)」鈕**：`SceneExporter::ExportAll` 走訪全部已載入 cell 收 placements＋registries 一次（registries 本就全域；未載入 cell 的 placements 撈不到，log 說明）。重構出 `AppendPlacements`/`AppendRegistries`/`RecordStats`。
- Settings 頁顯示 aim source／旋轉子模式現況（console 設定的可視化）。
- **numpad 5 改 per-mode**（使用者第三輪）：純旋轉模式下 5＝角度歸零（同 8/2），移動模式下 5＝復原編輯前——不再兩模式共用。（**2026-07-12**：旋轉模式下的 5 進一步收斂成**只還原 yaw**，見上；移動模式的 5 不變。）
- **marker 記錄完整朝向＋大小**（使用者第三輪）：Entry `angleZDeg`→`angleDeg{x,y,z}`＋`scale`；匯出 `annotations[]` 帶 `rotation`＋`scale`（ModForge `AnnotationSpec.Rotation/Scale`，869 測綠）；co-save MKRS v2（舊 v1 只有 angleZ→補 0）。**marker 模型改鐵匕首**（`Weapons\Iron\IronDagger.nif`，劍尖視覺化朝向；tools-spec.json 改 model 重建 esp，houseCARL 驗 WEAP 01397E）。

## 仍未做

- **📌 外部 mod 依賴的可見性與處置（使用者 2026-07-12 實機後提出，「之後要做的事」）**：`sc capp`/`sc cap` 吸到的 spell/perk/item/effect 只要來自 mod（PROTEUS、XPMSE、nwsFollowerFramework…），生成的 esp 就把那些 mod 變成 **master**。實例：2026-07-12 玩家分身 `MFCapHatak.esp` 的 masters ＝ Skyrim/Dawnguard/Dragonborn ＋ **PROTEUS.esp、nwsFollowerFramework.esp、XPMSE.esp、Better Face Lighting.esp、Conditional Expressions.esp、ImGladYoureHere.esp、SCSI-ACTbfco-Main.esp**（來源＝玩家身上的 20 spells／31 activeEffects／inventory）。
  - **🔑 使用者已拍板：不過濾——「完全複製」優先**（分身的價值在「就是你」，可攜性不是這條路的目標；要可攜就走手寫 spec）。所以**不要**加「只收 vanilla」的過濾當預設。
  - **但要處置的真實後果**：① 裝的人**必須有那些 mod**，否則缺 master → Skyrim **靜默不載**（不會說為什麼，見 memory `masterless-plugin-silent-load-failure` 的鄰居坑）；② repo/spec **沒有任何地方記著這個 esp 依賴誰、什麼版本** → 哪天移除 PROTEUS，esp 靜默壞掉；③ `build` 目前**零可見性**（只有 `dump` 印 masters），不會提醒「你引用了 7 個非 vanilla master」。
  - **範圍不只 capture**：任何手寫 spec 只要寫 `PROTEUS.esp:0x123` 都一樣——capture 只是讓它**大量自動發生**。所以處置該做在 **ModForge 通用層**，不是 capture 專屬。
  - **候選處置（動工時選）**：(a) `build` 摘要印出**非 vanilla masters ＋ 每個 master 是被哪些 record／哪個 spec 欄位拉進來的**（最低成本、最高價值）；(b) spec 加宣告式 `requires:` 段（明示依賴，build 時對不上就報錯）；(c) 把 **modlist / load order 快照**存進 repo 或 spec 旁邊（MO2 profile 的 `plugins.txt`/`modlist.txt`）；(d) 新指令做「依賴檢查」（給一份 esp + 一個 load order，回報缺什麼）。

- **📌 pointer/referrer 原語——標示「既有物件」而非座標（使用者 2026-07-11 晚）**：目前只有 marker（標**空的座標**：「這裡放東西」）。新增一個 pointer：指向一個**已存在的 ref**（vanilla/他 mod 擺的椅子、桌子、門…）→ `sc` 記下它的**耐久 FormID**＋自由標籤（例：指椅子→標「sofia的椅子」）→ export → AI 配合 ModForge 寫該物件相關行為（例：給 Sofia 的 AI package 一個 Sit/Sandbox 錨點，讓她常坐這張椅子）。與現有原語的分工：marker＝新建 proxy 記座標；pointer＝**不新建**、只記既有 ref 身份供下游引用（≠ `sc pk` 滴管——pk 吸的是 base 定義用來**複製擺放**，pointer 記的是**特定 instance 的身份**用來被引用）。
  - **✅ ModForge 消費端已落地（2026-07-12，全鏈＋測試綠；DLL 端未動）**：頂層 `references[]` ＝ `ReferenceSpec { ref, label, base?, position?, rotation?, scale?, cell?, worldspace?, anchor?, note? }`（`Spec.References.cs` / `Generator.Build.References.cs` / `Generator.Validate.References.cs`；契約全文在 [spec](../../specs/ingame-scene-export-design.md)「referrer 的形狀」節；範例 `examples/scene-references.json`；測試 `ReferencesTests.cs`）。核心語意＝**`label` 註冊進 build 的 editorId→FormKey 表** → spec 裡**任何 ref 欄位**都能寫這個 label（package `sandbox.location`/`travel.place`、alias `forced:`、`linkedRefs`、`enableParent`、objective target、script prop），消費站點零改動。`references[]` 為空 ⇒ 不生任何記錄（行為不變）。
  - **🔨 DLL 端剩餘工作（本條未完的部分）**：① `sc ref` / `sc ref XXX` / `sc refc [XXX]` 指令＋ referrer 模式（見下）；② `References` 面板頁（見下）；③ **exporter 吐 `references[]` 段**——目標 handle **∈ 本次匯出的 placements** 時，給該 placement 發一個**穩定 editorId**、`references[].ref` 指那個 editorId（乙路徑；否則 dynamic FormID 不可攜、build 後對不上）；目標是外部既有 ref 時照記耐久 `<plugin>:0xLOCALID` ＋ `base` ＋座標（＋拿得到的 rotation/scale）。**`anchor` 欄位 DLL 不要填**（留白＝`"none"`，選擇權在 ModForge/agent）。
  - **技術基礎已有**：記既有 authored ref 的耐久 id ＝ Eraser（removals）/Overrides 那套（`Eraser::MarkConsoleRef` 的解析路），pointer 只是「記 id＋label、不改它、不 disable」。
  - **🔴 核心坑＝persistent vs temporary ref → 已拍板（2026-07-12）**：AI package 的「sit at / sandbox at 指定 ref」需要目標 ref 能被 quest alias 以 specific-reference 填充，這通常要求該 ref 是 **persistent**；vanilla 場景物件多為 non-persistent。定案：**(乙) 檔內 ref → build 強制 persistent**（0x400 ＋ cell 的 Persistent group，機制同 linkedRefs target / package anchor）；**(甲) 外部 ref → build 查 master link cache 的 0x400，temporary 就明確警告**（不靜默），並提供 `anchor` 逃生門——`"marker"`＝在該點生一支 persistent XMarkerHeading（只需要*地點*時：sandbox/travel/patrol）、`"replace"`＝用 `base` 在該點生**我們自己的 persistent 複製品**＋把 vanilla 原件自動 disable+深埋（錨點必須*就是那個物件*時：坐**那張**椅子）。DLL 端兩者都先照記（ref id＋座標＋base＋label）。
  - **指令（使用者 2026-07-11 晚定名 referrer）**：`sc ref`＝進 referrer 模式（動作鍵記準星/射線指的既有 ref）；`sc ref XXX`＝記下當前指的 ref 並直接打標籤 XXX；`sc refc [XXX]`＝console 選取版（aim-free，同 delc/capc/pkc）＋選用標號。⚠️ 標號 XXX 用未 `Lower()` 的 raw 參數（保留大小寫，同 pkc/label 坑）。⚠️ **label 在 ModForge 端必須全 spec 唯一**（它就是個名字，會被註冊成可解析 id）——面板改名/打標時要擋重複。
  - **面板頁（使用者 2026-07-11 晚）**：新增 `References` 頁——列出已記的 referrer（label 就地改名、顯示 ref id/base/所在 cell、逐列刪除；比照 Markers/Palette 頁最新在前）。
  - **🔑 檔內相依關聯（使用者 2026-07-11 晚洞察 → 已成 ModForge 的乙路徑）**：export cell 會連 references 一起出，所以 referrer 的目標分兩類——(甲) 外部既有 ref（vanilla 椅子）→ 記耐久 FormID，踩上面 persistent 坑；(乙) **referrer 指的是我們自己 `sc pl` 新增的物件**（marker proxy 被 ExportCell 排除，不會誤指）→ 那物件是 dynamic ref、無耐久 FormID，要走**檔內 editorId 關聯**：同一份 scene.json 裡 references[].ref 指向 placements[] 裡對應那筆的 editorId。**乙路徑反而乾淨**——物件是我們建的，ModForge 完全掌控，可設 persistent、給穩定 editorId，Sofia 坐椅子這種「引用需 persistent」的需求天然滿足（**2026-07-12 已在 C# 端閉環驗證**：build 出的 esp 裡椅子帶 0x400、落在 cell 的 Persistent group，Sofia 的 sandbox package slot 0 ＝ `LocationTarget(該椅子 REFR)`）。DLL 端要補的就是上面 🔨 的 ③。
- **`sc cap` 物件類 vs `sc pk` 分工（使用者再想，先照舊）**：`sc cap` 記 NPC/player 含全身物品＋extra data（v7 已落地）；物件類 capture 與 `sc pk` 滴管感覺功能重複，使用者還要想想——**傾向仍記錄**，暫不動。
- **`sc pk ed0/ed1`＋`sc pl ed0/ed1`（使用者 2026-07-11 晚）**：滴管/擺放的 extra-data 開關。現況＝`sc pk` 只吸 durable base、不吸實例附魔（Palette.cpp 只取 GetBaseObject）。`ed1` ＝吸取時連 ExtraEnchantment 等 extra data 一起記（palette 條目要能帶實例資料，擺放/匯出時走 capturedItems 式鑄造＋引用）；`sc pl ed1` ＝擺放時帶上 extra data。per-mode 設定、進 co-save SETT（同 er0/er1 模式）。
- **`sc pl py0/py1`（使用者 2026-07-11 晚）**：擺放模式的物理開關。`py1`（**預設**）＝擺出的物件保留完整物理；`py0` ＝擺出的物件關閉物理性質——主要目的＝避免擺好的東西被 Skyrim 神奇物理引擎弄到亂飛。動態物件為主，靜態物件是否也要（clutter 類其實都是 havok 物件）實作時一併看。per-mode 設定、進 co-save SETT（同 er0/er1）。⚠️ 實作要分兩層：(a) DLL 擺放當下的即時凍結（已有 P3 物理凍結機制可複用），(b) **持久到 esp**——placement 要把這個狀態帶進 export，ModForge 端 REFR 用哪個機制（Don't Havok Settle 記錄旗標 vs script SetMotionType keyframed）動工時查證拍板，`PlacementSpec` 可能加欄位。先只考慮物理性質，不擴及其他屬性。
- **`sc ed py0/py1`（使用者 2026-07-11 晚）**：編輯模式（含 `ax` 旋轉子模式）下被控制物件的物理開關。**預設 `py0` ＝控制期間停止物理**（＝現行 P3 物理凍結行為，細摳③「選中時喪失物理」），`sc ed py1` 切回控制期間保留物理。也就是把既有凍結行為做成可切換設定；per-mode、進 co-save SETT。
- **📌 導航網格（navmesh）——「超重要，之後得開始考慮」（使用者 2026-07-11 晚）**：編輯器流程目前完全沒碰 navmesh——擺出的建築/障礙物會擋住 vanilla navmesh 但 NPC 照原網格走（穿模/卡住），marker 生的 NPC 若落在無網格處也不會動。ModForge 已有程式化 navmesh 能力可接（custom worldspace NAVM＋NAVI additive override Skyrim.esm:0x12FB4 in-game 驗過，見 idea/asset-pipelines/map-scene/geometry.md 一帶＋Vigilant.esm 解碼參考）；難點在**編輯 vanilla cell**：要 override 既有 NAVM（cut/finalize 語意）而不只是新建。方向未定（DLL 端記錄擺放物 footprint → ModForge 端裁切？或先只處理「新增小平台補網格」？），需要時開獨立 plan。
- **F1 面板清掉冗餘動作鈕（使用者 2026-07-11 晚）**：各頁面上諸如 "place marker here" / "erase by ray" / "pick by ray"… 這類動作觸發鈕都刪掉——現在這些動作全走 `sc` console 指令＋鍵位（P5 模式制之後 UI 觸發已多餘）。面板保留設定/檢視/清單類（改名、kind、逐列 undo、步長、palette 列表…），只砍「在面板按一下就執行世界動作」那批。動工時逐頁盤點哪些是動作鈕、哪些是設定項。
- **`sc pkc [XXX]`（使用者 2026-07-11 晚）**：滴管吸取的 console-selected 版——console 點選 ref 後 `sc pkc` 吸進 palette（同 `delc`/`capc` 的 aim-free 模式）；帶選用標號 `sc pkc XXX` ＝吸取當下直接把該 palette 條目改名為 XXX（識別用）。⚠️ 標號要用未 `Lower()` 的 raw 參數（保留大小寫，同 [player-capture-capp](../player-capture-capp.md) 的 label 坑）。
- **紅/綠半透明輪廓高亮**（使用者第二輪：`sc del dp1` 被刪物件紅框、`sc pl dp1` 新增物件綠框，顏色/透明度 Settings 可調）——**較難、非必做**（需 render/shader 或 highlight 效果）。
- marker 編輯視窗下拉：寶石種類 ＋ 發光開關（需 SceneCaptureTools.esp 多個 ACTI 變體或動態換 model，較大工程）。
- rebind 重作（找出 in-game 抓錯鍵主因：可能是 rebind armed 當幀把移動鍵也吃進去；目前 Settings 隱藏、固定 F11）。
