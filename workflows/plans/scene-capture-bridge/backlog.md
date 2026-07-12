# scene-capture-bridge — 之後再做（backlog）

← [README](README.md)（現況導航）｜[phases](phases.md)（P1–P6 落地）｜[appendix](appendix.md)（細摳原文＋驗證清單）

**活躍成長區**：新想法都記這。做完就從「仍未做」移到「已做」並標日期/DLL crc。

---

## ✅ 已做（模式開關套件：`py`／`ed`／`pkc` 五項，2026-07-12，DLL crc `5434abd4`，**已部署**，待實機）
- **`sc pl py0/py1`（擺放物理，`py1` 預設）＋ `sc ed py0/py1`（編輯期物理，`py0` 預設）**。DLL 端凍結複用 P3 機制（抽成新的 `src/Physics.{h,cpp}`：`HavokMovable` 判定 ＋ `FreezeDeferred` 延後到 3D 載入才凍；Markers/Editor 原本各抄一份，現三處共用，**行為不變**）。`sc ed py0` ＝把現行凍結行為做成可切；`py1` ＝控制期間 havok 照跑。
- **🔑 持久到 esp ＝「Don't Havok Settle」記錄旗標（查證後拍板，不走 script `SetMotionType`）**：`PlacementSpec.NoHavokSettle` → REFR header flag **`0x20000000`**。**決定性證據**（Mutagen 直接掃 Skyrim.esm，非推論）：693,333 個 PlacedObject 裡 **3,791 個帶此旗標**，base 型別分佈正是雜物類——MoveableStatic 995／MiscItem 724／Activator 564／Weapon 321／**Static 247**／Ingestible 245／Armor 159／Ammo 141／Flora 102／Ingredient 96／Book 87／Container 56…（樣本 `GlazedPot02Nordic`、`RuinsFloorCandleStandSmall`、`CrateOpen`）⇒ **Bethesda 自己擺完雜物就是靠它定住的**。它跳過的正是 **cell 載入時的 havok settle pass**（把手擺物件彈飛的元兇，物件與桌面稍有交疊時尤烈）。成本：純 record header 旗標、與 `Persistent 0x400`／`InitiallyDisabled 0x800` 同一條 code path，**零 script／零 VMAD／零 pex／不多 master**；script 路要給每顆 ref 掛 Papyrus（VMAD＋pex＋OnCellAttach），工程量爆炸只換來「連玩家撞都推不動」的邊際差異，且那是 runtime 狀態、沒 script 跑就不存在。
- **靜態物件要不要吃 `py0`？→ 匯出旗標不按型別過濾**（vanilla 連 247 個 STAT 都帶它；clutter 類本來就都是 havok 物件）。**只有 runtime 凍結過濾**（`HavokMovable`：keyframe 一個 STAT 沒有意義，而且把 STAT/FURN 放回 `kDynamic` 會把牆震鬆——這是 P3 當初就寫死的邊界）。ACHR 不寫（actor 無此語意）。
- **`sc pk ed0/ed1` ＋ `sc pl ed0/ed1`（extra data）**：現況只取 `GetBaseObject()` ⇒ 玩家自己附魔的劍吸進來是**白鐵劍**（附魔活在 ref 的 `ExtraEnchantment`，不在 base）。`pk ed1` ＝插槽連實例附魔一起記（durable ENCH → 引用；runtime ENCH → 記 MGEF effects 待鑄造）。`pl ed1` ＝匯出走**鑄造＋引用**：同一份 scene 檔吐 `capturedItems[]`（`editorId: MFPal_<插槽名>_<seq>`，`base`＝實體模板）＋ placement 的 **`base` 指那個 editorId** ＝ **檔內相依**（同 referrer 那招；**必須同檔**，capturedItems 落到另一份 json 的話 build 解不到 base 會丟掉 placement）。**C# 零改動**（`ExpandCapturedItems` pass 0 → `WeaponSpec/ArmorSpec.EditorId` → `formKeyByEd` → `BuildPlacements` 的 `TryResolveRef` 解得到）。⚠️ **runtime ENCH 的指標絕不快取在插槽上**——插槽是**落盤跨存檔**的，而 runtime ENCH 是存檔綁定的 form（快取＝懸空指標）；世界裡的物件只在 **durable ENCH** 時真的帶上附魔，匯出則照樣鑄造（ship 出去的不受影響）。
- **`sc pkc [XXX]`**：滴管的 console 選取版（同 `delc`/`capc`/`refc` 的 aim-free 路），`XXX` ＝吸取當下直接改插槽名。標號走**未 `Lower()` 的 raw 參數**（大小寫保留，照 `sc capp` 的坑）。
- **co-save**：SETT **v6**（+place/edit 物理 +pick/place extra data；v≤5 舊存檔降級讀 ⇒ 落回預設 place py1／edit py0／extra 全關＝與以前完全一致）＋新 record **`'PLEX'` v1**（我們擺出去、匯出要多講一句話的 ref：`noHavokSettle`／鑄造附魔。**一般擺放不建列**——vanilla diff 本來就吐得完美。handle 跨重啟死掉 → 匯出掃 cell 時按 **base+座標**就地撿回，不必 kPostLoadGame hook）。Settings 頁顯示四個開關現況。
- **端到端自驗（離線閉環）**：手寫 DLL 形狀 json（一筆 `noHavokSettle` placement ＋ 一筆 `base` 指 `MFPal_…` 的 placement ＋ 對應 `capturedItems[]`）→ `build` → esp 裡兩個 REFR 都 **flags=0x20000000（DontHavokSettle=True）**、鑄出 `MFPal_Ebony_Sword_of_Fire_2` WEAP（enchantmentAmount 1500）＋ `…_Ench` ENCH，第二個 REFR 的 base **解到那把鑄造的劍**。C# **932 測綠**（928 + 4 新，含「舊 json 不帶欄位 → 旗標不設」的向後相容釘子 ＋「ACHR 不寫」）。

## ✅ 已做（pointer/referrer 原語**全鏈完工**，2026-07-12，DLL crc `112be269`，**已部署**，待實機）
- **DLL 端補齊**（C# 消費端 `adc419b` 已先落地）：新模組 `src/Referrer.{h,cpp}`（登記簿，**不新建/不改/不 disable** 目標——與 Eraser 的唯一差別）＋ `src/UI.References.cpp`（面板頁）＋ exporter 吐 `references[]`。
- **指令**：`sc ref`＝進 referrer 模式（動作鍵標準星/射線指的既有 ref，`sc ref er0/er1` 切 aim source，SETT **v5**）；`sc ref <Label>`＝一次到位（標下當前指的 ref ＋打標籤）；`sc refc [Label]`＝console 選取版（aim-free）。**標籤走未 `Lower()` 的 raw 參數**（大小寫保留，同 `sc capp` 的坑）。
- **🔑 (乙) 檔內相依關聯（最關鍵，一次做對）**：referrer 指到**我們自己 `sc pl` 擺的物件**（dynamic ref、無耐久 FormID）時——`AppendPlacements` 掃到那筆 ref 就在它的 placement 上**蓋一個穩定 editorId**（`Referrer::EditorIdOf` ＝ `MFRef_<sanitize(label)>_<seq>`，seq 隨 co-save 故跨匯出穩定），`references[].ref` 指那個 editorId。**identity ＝ handle**（沒有耐久 id 可用）。`AppendReferences` **必須跑在 `AppendPlacements` 之後**，且**只吐「這次匯出真的有出 placement」的那些**（cell 沒掃到／物件被擦掉／跨重啟 handle 死掉 ⇒ 跳過＋log warn＋面板顯示 skipped 數，不吐一個 build 對不上的 editorId）。
- **(甲) 外部既有 ref**：照記耐久 `<plugin>:0xLOCALID` ＋ base ＋座標/rotation/scale ＋ cell/worldspace。**`anchor` 欄位 DLL 一律不填**（留白＝`none`，persistent 逃生門的選擇權在 ModForge/agent）。
- **拒收三類**：① **marker proxy**（editor chrome，`ExportCell` 本來就排除它 → 檔內 reference 永遠解不到；而且 marker 本來就有 label/note 走 `annotations[]`）；② **我們自己生的 actor**（cell 匯出不含 actor ⇒ 沒有 placement 可指；要複製走 `sc cap`）；③ **重複 label**（label 在 ModForge 是**全域名字空間**＝可解析的 id，撞名 validate 會炸整份 spec）。**authored actor（vanilla NPC 的 ACHR）可以指**——它有耐久 id，走 (甲)。
- **面板 References 頁**：最新在前、label／note 就地改名（**擋重複 label**，撞名當場橘字說「not renamed」）、顯示 ref id（檔內顯示**將寫進 json 的 editorId**，綠字）／base／cell／座標、逐列刪除（**只刪登記列，世界不動**）。Export 頁多兩行統計（`N named (references[])` ＋ skipped 數）。
- **co-save 新 record `'RFRR'` v1**；in-file 列的 handle 跨完整重啟會死 → `Referrer::ReacquireOrphans()`（`kPostLoadGame` 自動跑，按 base+座標在玩家 cell 重新綁回，同 marker 的 adopt 救援）；撿不回的列**保留**（面板標 `TARGET LOST`、匯出跳過），不靜默丟。
- **端到端自驗（離線閉環）**：手寫一份 DLL 形狀的 json（placement 帶 `MFRef_sofia_s_chair_1` ＋ `references[]` 指它 ＋ Sofia 的 sandbox package 指 label）→ `build` → esp 裡該 REFR **record flag ＝ 0x400**、落在 cell 的 **Persistent group**（`dump`：`persistent=1 temporary=0`），build 摘要印 `1 reference(s) — labels bound to existing refs: 'sofia's chair'`。C# **928 測綠**（消費端零改動）。

## ✅ 已做（旋轉 per-axis 還原 ＋ palette replace，2026-07-12，DLL `9cae7ff1`，**未部署**·待實機）
- **旋轉子模式的歸零鍵改 per-axis（使用者實機後提）**：`sc ed ax` 下**每組的中間鍵只管自己那一組軸**——**2＝還原 pitch（1/3）、5＝還原 yaw（4/6）、8＝還原 roll（7/9）**。語意＝**revert 回進編輯前的該軸原值**（`g.origAngle.<axis>`），**不是設成 0**（物件本來就可能有角度）。原本三鍵都是「整個角度還原」（全軸 `origAngle`）。移動模式的 numpad 5（＝復原整個編輯）**不動**（P7 的 per-mode 行為）。`Editor.cpp` 的 `kBack`/`kSelect`/`kFwd` 三個 case；每鍵各自的 DebugNotification。
- **palette 檔案 I/O 兩改**：① **檔內順序＝面板順序**（最上面那筆排 json 第一筆）——`SlotsJson()` 反向寫、`ParseSlots()`＋`Adopt()` 反向插；`load from file (append)` 的新項因此**落在列表最上面**且保留檔內順序（面板最新排頂的既有慣例）。② 新鈕 **`replace from file`**（`Palette::ReplaceFromFile`）＝**清空現有插槽再載入**；檔案不存在／不可讀／無可用插槽 ⇒ **不清**（不會誤把磁碟持久的 palette 清光）。三鈕並列：`load from file (append)` / `replace from file` / `save to file` ＋一行說明。
- ⚠️ 舊 `scene-capture-palette.json`（舊格式＝反序）讀進來順序會**上下顛倒一次**，之後穩定；欄位完全相容。

## ✅ 已做（`capturedNpcs[].isPlayer` 標示，2026-07-12，DLL crc `e37ad0e1`，**已部署**，待實機）
- 實機發現玩家 base TESNPC **沒有 `voiceType`**（分身啞巴）；使用者拍板**照實輸出，不加 fallback**——但補一個「這筆是玩家」的標示。`NpcData.isPlayer`（`actor->As<PlayerCharacter>()`，跟既有 perk 路線同一個 cast，`sc capp`／點到玩家的 `sc capc` 都標得到）；`SceneExporter` 只在 true 時吐 `"isPlayer": true`。co-save **SCCP v9**（v≤8 缺省 `false`）。
- C# 消費：`CapturedNpcSpec.IsPlayer` → `NpcSpec.IsPlayer`（純可見性，不寫任何 Mutagen 記錄欄，行為不變）→ `BuildNpcs` 只在「`IsPlayer` 且無 `VoiceType`」時 `Warn`（措辭「this is expected, not a bug」，不是錯誤）。舊 json 缺欄位＝`false`＝完全相容。詳見 [plans/player-capture-capp.md](../player-capture-capp.md)。C# 928 測綠（5 個新測試）。

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

- **`sc cap` 物件類 vs `sc pk` 分工（使用者再想，先照舊）**：`sc cap` 記 NPC/player 含全身物品＋extra data（v7 已落地）；物件類 capture 與 `sc pk` 滴管感覺功能重複，使用者還要想想——**傾向仍記錄**，暫不動。
- **📌 導航網格（navmesh）——「超重要，之後得開始考慮」（使用者 2026-07-11 晚）**：編輯器流程目前完全沒碰 navmesh——擺出的建築/障礙物會擋住 vanilla navmesh 但 NPC 照原網格走（穿模/卡住），marker 生的 NPC 若落在無網格處也不會動。ModForge 已有程式化 navmesh 能力可接（custom worldspace NAVM＋NAVI additive override Skyrim.esm:0x12FB4 in-game 驗過，見 idea/asset-pipelines/map-scene/geometry.md 一帶＋Vigilant.esm 解碼參考）；難點在**編輯 vanilla cell**：要 override 既有 NAVM（cut/finalize 語意）而不只是新建。方向未定（DLL 端記錄擺放物 footprint → ModForge 端裁切？或先只處理「新增小平台補網格」？），需要時開獨立 plan。
  - **✅ 已開獨立 plan（2026-07-12）：[plans/navmesh.md](../navmesh.md)**——兩個結論：① **「擺的東西擋住 NPC」根本不必改 navmesh**：用 vanilla 的 **L_NAVCUT 碰撞體積**（`CollisionMarker` 0x000021 ＋ `CollisionLayer=49` ＋ Primitive box，**HearthFires 蓋房子用了 1220 筆**）就能 runtime 裁切，純 Mutagen 一筆 REFR（⚠️ 光加 Obstacle flag 無效——L_STATIC 不是 NavmeshObstacle 層）。② **「NPC 走上新平台」非寫 NAVM 不可，而 override vanilla NAVM 可行**（Mutagen no-op override 離線 byte-diff ＝ IDENTICAL；USSEP 807 筆真的這麼幹；NAVI 是加法式 merge 不是地雷）；鐵律＝**永不重新編號 triangle**（鄰居的 EdgeLink 存的是你的 triangle index）。分期 P1 診斷 → P2(T2.0) navcut → P0 spike → P3 add+link → P4 遊戲內採集。
- **F1 面板清掉冗餘動作鈕（使用者 2026-07-11 晚）**：各頁面上諸如 "place marker here" / "erase by ray" / "pick by ray"… 這類動作觸發鈕都刪掉——現在這些動作全走 `sc` console 指令＋鍵位（P5 模式制之後 UI 觸發已多餘）。面板保留設定/檢視/清單類（改名、kind、逐列 undo、步長、palette 列表…），只砍「在面板按一下就執行世界動作」那批。動工時逐頁盤點哪些是動作鈕、哪些是設定項。
- **紅/綠半透明輪廓高亮**（使用者第二輪：`sc del dp1` 被刪物件紅框、`sc pl dp1` 新增物件綠框，顏色/透明度 Settings 可調）——**較難、非必做**（需 render/shader 或 highlight 效果）。
- marker 編輯視窗下拉：寶石種類 ＋ 發光開關（需 SceneCaptureTools.esp 多個 ACTI 變體或動態換 model，較大工程）。
- rebind 重作（找出 in-game 抓錯鍵主因：可能是 rebind armed 當幀把移動鍵也吃進去；目前 Settings 隱藏、固定 F11）。
