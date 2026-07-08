# 24. 遊戲內編輯器：「施法即編輯」→ 快照 cell 狀態 → 生成 patch mod（2026-07-07）

← index: [README.md](README.md) · [ideas 索引](../ideas.md)

**核心 Idea**：把**遊戲內本身**當成編輯器。玩家施放一組「編輯法術」直接在遊戲裡擺物件 / 放 NPC / 錄行為 /（野心）改地形，然後施法**快照當前房間（cell）狀態**，diff vs vanilla → ModForge 生成一份 **patch mod（override 記錄）**。等於用真實遊戲鏡頭 + 物理當所見即所得的編輯台，取代 CK。

**為何吸引 / 為何優於外部編輯器（決策依據，2026-07-07）**：CK 崩、Windows-only；外部編輯器（#15 Blender/CK 替代、#19 Godot Worldspace Editor）擺完**看不到真實渲染態**。使用者經驗回饋：**現有 [Godot worldspace editor](../../../sub_projs/godot-worldspace-editor/README.md) 實際用起來不好用**——它重建的是近似場景，不是玩家實際會看到的畫面。遊戲內編輯的決定性優勢是**吻合指定渲染狀態**：ENB、光照、特效、天氣、後處理全部就位，「你看到什麼就是成品」（真 WYSIWYG）。外部工具永遠追不上 ENB/community shaders 那層。→ **本 idea（遊戲內路線）相對 #15/#19（外部路線）是更受青睞的方向**；外部路線退為輔助/離線批次用途。

**能力光譜（由易到難，可各自獨立落地）**：
- **① 快照 cell 狀態 → patch**（地基能力）：SKSE 列舉當前 cell 的 placed refs（座標/旋轉/縮放/enable state）→ diff vanilla → 輸出 override CELL + `placements[]`。ModForge 已有 cell/worldspace override 生成基礎（見記憶 [[worldspace-override-must-carry-topcell]]）。**這是整個框架的核心，先做這個**。
- **② 施法擺設 / 移動物件**：一支「擺放法杖」spawn / grab / 旋轉 / 吸附 refs（先例：SIGE 遊戲內 3D gizmo，見 #15 Gemini 調查）→ 快照時一起收進 ①。
- **③ 施法錄製 NPC 行為**（原 #24 小野心）：走一條路徑，沿途取樣座標放 PatrolMarker/IdleMarker + 停留動作 → 輸出 sandbox/travel/patrol package（見記憶 [[radiant-alias-package-byte-truths]]：package 掛在 alias 的 ALPS 上）。
- **④ 施法擺放 NPC → 靠 PROTEUS**：用 [PROTEUS](../../../sub_projs/mod-survey/findings/proteus.md) 遊戲內生成 / 定位 / 控制 NPC 的既有能力當「放 NPC」前端，我方在 ① 快照時把該 NPC 的狀態讀成 NPC 記錄 + placement。⚠️ PROTEUS 核心是**閉源 native DLL**，只能**消費**它（當放置工具）、不能改它；若要自建放置也可走既有 `quest.spawn`（見記憶 [[dynamic-spawn-debugging]]）。
- **⑤ 施法修改地形（LAND）**：野心項，**技術牆**。runtime 編輯 LAND heightmap 極難，ModForge 目前僅支援平坦地形（見 #14/#15 地形段）。先擱置，優先做 cell 內物件 / NPC。
- **⑥（最大野心，暫緩）**：變身 NPC + 錄軌跡途中施法插事件節點（對話/idle/換場景）→ 生成任務/scene。狀態機複雜，最後再碰。

**PROTEUS 的角色定位**：不是要改它，而是它的兩點正好補位——(a) 遊戲內生成/控制 NPC（能力 ④ 的前端）、(b) 「遊戲中 JSON 序列化狀態」的概念先例。核心 native 閉源、無可生成成分（見 finding 結論），所以是**消費 / 仿概念**，非依賴元件。

**ModForge 落點**：所有能力最終都匯到現有生成鏈——override CELL + `placements[]` + package/alias + NPC 記錄。難的是**「遊戲內採集 → spec」的橋**（SKSE/Papyrus 讀狀態、序列化成 JSON），生成端 ModForge 大多已具備。

**mod-survey 佐證（2026-07-07 一批 finding 浮現）**：
- **產物格式先例 = 「切預置 disabled REFR 可見性」家族**：[AnnoRim](../../../sub_projs/mod-survey/findings/annorim.md)（建物）、[Pirates of Skyrim](../../../sub_projs/mod-survey/findings/pirates-of-skyrim.md)（偽移動船港）、populated 系列都用「設計期預置 disabled 物件 + Enable/Disable 切換」而非 runtime PlaceAtMe。→ 能力①快照該吐的 `placements[]` 就是這個形狀（含 enable-parent / initiallyDisabled 欄位，ModForge 已支援），有現成量產藍本。
- **UI 層有現成解 = [SKSE Menu Framework 3](../../../sub_projs/mod-survey/findings/skse-menu-framework-3.md)**（遊戲內嵌 Dear ImGui：即時控件、直接讀寫遊戲物件、FormID 查找、非暫停 overlay）——正是能力②的「擺物/選 record/存快照面板」最現成前端。**但它必然純參考、非可生成**（選單是編譯進消費者 DLL 的 ImGui C++）。→ #24 的真正落地缺口收斂成一句：**「編輯器 UI（ImGui 框架，現成）+ 採集橋（須寫一支消費 SKSE DLL）→ 吐 JSON → ModForge 生成 esp（已具備）」**；缺的是中間那支 bespoke DLL，同 Tundra/Honed Metal「須附 native controller」判定。

**關聯**：#15（CK 替代視覺編輯器，本 idea 是遊戲內路線）；#19 Godot Worldspace Editor（外部編輯器另一路）；#17（任務節點圖）；#23（活世界，巡邏/作息素材）。
