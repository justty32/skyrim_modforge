# Ideas — 工具 / 技術管線

← [ideas 索引](ideas.md)

## 6. 在 SkyUI 基礎上擴充 UI

先例：快捷欄擴充（iEquip、Wheeler）。想加技能槽（快速切換施法序列）、任務追蹤懸浮框、小地圖增強。核心挑戰：SkyUI 以 ActionScript/Flash 實作，需 AS3 / Scaleform 知識。

---

## 7. 遊戲內嵌入網頁 UI

在 Skyrim 視角內顯示可互動「瀏覽器」面板（CEF + SKSE）。應用：遊戲內查攻略、顯示 AI 代理回傳資訊、即時地圖。技術難度高，需 SKSE/C++ 介入。

---

## 10. 翻譯 + 插件合併

- **翻譯**：`extract`/`apply`/`applyloc`（含 UTF-8 `_chinese.STRINGS`）已可用，英文模組中文化直接用。
- **ESP/ESL 合併（未做）**：合併小插件釋放載入順序空位，對 §9 量產尤重要；要處理 FormID 重映射 + 所有引用（含腳本屬性、SEQ）同步改寫——工程不小，Mutagen 有基礎能力。

---

## 14. 資產格式轉換管線（glTF/FBX → NIF）（2026-06-04）

主流 3D 格式 → Skyrim 全自動轉換：**「網格」可以，「全套」不行**，卡點集中：

| 內容 | Skyrim 格式 | 自動化可行性 |
|---|---|---|
| 網格/材質 | `.nif`（SSE BSTriShape） | 高（Linux 靜態主路徑：Blender NifTools addon / ck-cmd；PyNifly 為 Windows 升級路徑） |
| 貼圖 | `.dds`（BC + mipmaps） | 完全自動（純轉碼） |
| 表情/morph | `.tri` | 高（兩邊都是頂點 delta） |
| 動畫/骨架/物理 | `.hkx`（Havok 二進位） | **這就是那道牆** |

- **靜態物件最接近全自動**：補碰撞（NIF `bhk*` 也是 Havok，但簡單凸包/box 可程式生成）+ 材質映射規則（glTF PBR ↔ Community Shaders True PBR／`BSLightingShaderProperty`，寫一次批次套）。
- **蒙皮網格半自動**：綁 Skyrim 骨架（`NPC Spine [Spn1]` 命名）、每頂點 ≤4 骨權重、`BSDismemberSkinInstance` 分區；「來源骨架 → Skyrim 骨架」retarget 每體系寫一次（同 §13 哲學）。
- **動畫是真正的牆**：Havok SDK 不公開，社群靠 ck-cmd/hkxcmd 包舊 SDK（版本敏感）；behavior graph 完全無自動轉換（Nemesis/Pandora 領域）。
- **對其他想法的意義**：§13 二次元路線（VRoid/MMD 頭身可管線化、卡在動畫）；§5 資源移植（靜態場景物件是甜蜜點）。⚠️ 他遊資產轉了不能發布。
- **ModForge 視角**：這是**資產層管線**（與記錄層 Mutagen 平行的另一軸）；`package` 已打包 Meshes/Textures，上游接轉換是自然延伸。Linux 靜態網格主路徑用 Blender NifTools addon，ck-cmd 作 Wine shell-out；PyNifly 僅列 Windows 蒙皮／動畫升級路徑（同 xLODGen 態度：不自造格式 writer）。

---

## 15. Unity / Blender 插件作為 CK 替代視覺場景編輯器（2026-06-15）

**Idea**：用 Unity 或 Blender 插件取代 Creation Kit 的「視覺場景搭建」環節——在 Unity/Blender 中擺放物件/設計地圖，插件輸出 ModForge spec JSON，ModForge 產生合法 ESP。

**為何有吸引力**：CK 是 Windows-only、崩潰率高、難腳本化；Unity/Blender 在 Linux 上跑、插件生態成熟、Unity 與 ModForge 同 .NET ecosystem。目標不是「用 Unity/Blender 做所有事」，而是**只替換 CK 最痛的一件事——視覺化物件擺放**，其餘（對話/腳本/記錄邏輯）繼續走 JSON spec。

**兩條技術路線**：
- **Blender 路（優先候選）**：Python 插件生態成熟；Blender NifTools 可處理靜態 NIF 預覽/匯出；插件讀場景 GameObject transform + EditorID 對照表 → 輸出 `placements[]` JSON；ModForge 接手生成 CELL/REFR。現有工具：Blender NifTools / PyNifly（後者限 Windows 升級路徑）。
- **Unity 路**：C# plugin 直呼 Mutagen 是理論可行；Unity 場景 GameObject → spec；資產匯入可接 §14 glTF/NIF 管線。

**甜蜜點（今天就夠用）**：靜態物件擺放（REFR placed refs）、基本 CELL 佈局。ModForge 的 `placements[]` 規格已完整（含 `linkedRef`、`linkedRefKeyword`、enable/disable parent 欄位），插件只需對齊輸出格式。

**已知難點**：
- **Navmesh**：CK 自動三角化；替代路是 ModForge 現有靜態 flat-quad 生成（自訂 worldspace 夠用）。
- **地形（LAND）**：heightmap → LAND record 轉換是技術牆；平坦地形 ModForge 已支援，非平坦還在 roadmap。
- **LOD**：shell-out xLODGen（§11 已規劃）。
- **CK 硬限制**：FaceGen 烘焙 / LipGenerator 跑不掉，但這類不是場景編輯的範疇。

**Gemini 調查結果**（`sub_projs/gemini-research/idea15-ck-visual-editor/`）：
- **F4RefToBlender**：xEdit 匯出 REFR → Blender 重建場景（現有工具，正是缺的那一半）
- **Skyrim In-Game Editor (SIGE)**：高度活躍（2025-05），SKSE 插件、in-game 3D gizmo 移動/旋轉/縮放
- **Creation Companion**：Mutagen-based CK 替代 IDE（2025，active）
- **Bethesda 官方 Blender 工具**（2024-12）：AssetWatcher 可同步 Blender 場景與遊戲記錄
- **Spriggit**：ESP → YAML/JSON 文字序列化，可用任何編輯器跨平台編輯
- **SkyUnity**（`Suslanium/SkyUnity`，✅ 真實）：ES5Unity 重寫版，可解析 ESP/ESM/BSA、重建 cell 含光照物理，2024 大改版
- ~~Skyrim Content Tools (SCT)~~：❌ **幻覺**（GitHub 404）
- ~~SLDU by Gka60~~：❌ **幻覺**（GitHub 404，「私人 Discord」是幻覺遮掩說法）

**待深挖**：Blender Niftools 能否讀 placed refs + vanilla asset 預覽；xEdit 腳本匯出 REFR JSON 已有 Gemini 生成的範例腳本（`05-xedit-export-script.md`）。

**關聯**：§11 M&B worldspace；§4 自訂世界；§14 資產管線；§8 程序生成世界。

---

## 16. ESL 合併工具（ModForge 外掛指令）（2026-06-15）

**Idea**：把一堆動作包、服裝包、武器包 ESL 合併成單一 ESL（含資源），釋放 ESL 插槽、簡化 MO2 管理。

**為何需要**：ESL 雖比 ESP 省插槽，但下載一卡車 ESL 仍占上限（4096/mod）；MO2 管理大量小 mod 麻煩。合併後只剩一個 mod，覆蓋資源也在同一地方。

**技術核心**：
- **FormID 重映射**：ESL 用 `0x000xxx`~`0x000FFF`（最多 2048/4096 筆），合併後需重算每個來源 ESL 的 FormID 區段並更新**所有引用**（record 內 link、腳本 property、SEQ）。
- **資源合併**：Meshes/Textures/Sound/Scripts 複製並 dedup（同路徑同內容不重複）。
- **衝突處理**：同 EditorID 的 record 需 override 策略（後者蓋前者 / 保留兩者改名）。
- **實作路線**：Mutagen 已有 FormID remapping 基礎能力（Synthesis 的 link 更新）；ModForge 加 `merge` CLI 指令，輸入 ESL 清單 → 輸出合併 ESL + 合併資源資料夾。

**已知先例**：[zMerge (zEdit)](https://github.com/z-edit/zedit) 做 ESP 合併，原理相通；ESL 特化版尚無成熟工具。

**難點**：① 腳本 property 的 FormID 更新（要解析 pex）；② 有 leveled list override 的 ESL 合併後行為；③ MO2 整合（輸出結構需 MO2 認識）。

---

## 24. 遊戲內編輯器：「施法即編輯」→ 快照 cell 狀態 → 生成 patch mod（2026-07-07）

**核心 Idea**：把**遊戲內本身**當成編輯器。玩家施放一組「編輯法術」直接在遊戲裡擺物件 / 放 NPC / 錄行為 /（野心）改地形，然後施法**快照當前房間（cell）狀態**，diff vs vanilla → ModForge 生成一份 **patch mod（override 記錄）**。等於用真實遊戲鏡頭 + 物理當所見即所得的編輯台，取代 CK。

**為何吸引 / 為何優於外部編輯器（決策依據，2026-07-07）**：CK 崩、Windows-only；外部編輯器（#15 Blender/CK 替代、#19 Godot Worldspace Editor）擺完**看不到真實渲染態**。使用者經驗回饋：**現有 [Godot worldspace editor](../../sub_projs/godot-worldspace-editor/README.md) 實際用起來不好用**——它重建的是近似場景，不是玩家實際會看到的畫面。遊戲內編輯的決定性優勢是**吻合指定渲染狀態**：ENB、光照、特效、天氣、後處理全部就位，「你看到什麼就是成品」（真 WYSIWYG）。外部工具永遠追不上 ENB/community shaders 那層。→ **本 idea（遊戲內路線）相對 #15/#19（外部路線）是更受青睞的方向**；外部路線退為輔助/離線批次用途。

**能力光譜（由易到難，可各自獨立落地）**：
- **① 快照 cell 狀態 → patch**（地基能力）：SKSE 列舉當前 cell 的 placed refs（座標/旋轉/縮放/enable state）→ diff vanilla → 輸出 override CELL + `placements[]`。ModForge 已有 cell/worldspace override 生成基礎（見記憶 [[worldspace-override-must-carry-topcell]]）。**這是整個框架的核心，先做這個**。
- **② 施法擺設 / 移動物件**：一支「擺放法杖」spawn / grab / 旋轉 / 吸附 refs（先例：SIGE 遊戲內 3D gizmo，見 #15 Gemini 調查）→ 快照時一起收進 ①。
- **③ 施法錄製 NPC 行為**（原 #24 小野心）：走一條路徑，沿途取樣座標放 PatrolMarker/IdleMarker + 停留動作 → 輸出 sandbox/travel/patrol package（見記憶 [[radiant-alias-package-byte-truths]]：package 掛在 alias 的 ALPS 上）。
- **④ 施法擺放 NPC → 靠 PROTEUS**：用 [PROTEUS](../../sub_projs/mod-survey/findings/proteus.md) 遊戲內生成 / 定位 / 控制 NPC 的既有能力當「放 NPC」前端，我方在 ① 快照時把該 NPC 的狀態讀成 NPC 記錄 + placement。⚠️ PROTEUS 核心是**閉源 native DLL**，只能**消費**它（當放置工具）、不能改它；若要自建放置也可走既有 `quest.spawn`（見記憶 [[dynamic-spawn-debugging]]）。
- **⑤ 施法修改地形（LAND）**：野心項，**技術牆**。runtime 編輯 LAND heightmap 極難，ModForge 目前僅支援平坦地形（見 #14/#15 地形段）。先擱置，優先做 cell 內物件 / NPC。
- **⑥（最大野心，暫緩）**：變身 NPC + 錄軌跡途中施法插事件節點（對話/idle/換場景）→ 生成任務/scene。狀態機複雜，最後再碰。

**PROTEUS 的角色定位**：不是要改它，而是它的兩點正好補位——(a) 遊戲內生成/控制 NPC（能力 ④ 的前端）、(b) 「遊戲中 JSON 序列化狀態」的概念先例。核心 native 閉源、無可生成成分（見 finding 結論），所以是**消費 / 仿概念**，非依賴元件。

**ModForge 落點**：所有能力最終都匯到現有生成鏈——override CELL + `placements[]` + package/alias + NPC 記錄。難的是**「遊戲內採集 → spec」的橋**（SKSE/Papyrus 讀狀態、序列化成 JSON），生成端 ModForge 大多已具備。

**mod-survey 佐證（2026-07-07 一批 finding 浮現）**：
- **產物格式先例 = 「切預置 disabled REFR 可見性」家族**：[AnnoRim](../../sub_projs/mod-survey/findings/annorim.md)（建物）、[Pirates of Skyrim](../../sub_projs/mod-survey/findings/pirates-of-skyrim.md)（偽移動船港）、populated 系列都用「設計期預置 disabled 物件 + Enable/Disable 切換」而非 runtime PlaceAtMe。→ 能力①快照該吐的 `placements[]` 就是這個形狀（含 enable-parent / initiallyDisabled 欄位，ModForge 已支援），有現成量產藍本。
- **UI 層有現成解 = [SKSE Menu Framework 3](../../sub_projs/mod-survey/findings/skse-menu-framework-3.md)**（遊戲內嵌 Dear ImGui：即時控件、直接讀寫遊戲物件、FormID 查找、非暫停 overlay）——正是能力②的「擺物/選 record/存快照面板」最現成前端。**但它必然純參考、非可生成**（選單是編譯進消費者 DLL 的 ImGui C++）。→ #24 的真正落地缺口收斂成一句：**「編輯器 UI（ImGui 框架，現成）+ 採集橋（須寫一支消費 SKSE DLL）→ 吐 JSON → ModForge 生成 esp（已具備）」**；缺的是中間那支 bespoke DLL，同 Tundra/Honed Metal「須附 native controller」判定。

**關聯**：#15（CK 替代視覺編輯器，本 idea 是遊戲內路線）；#19 Godot Worldspace Editor（外部編輯器另一路）；#17（任務節點圖）；#23（活世界，巡邏/作息素材）。
