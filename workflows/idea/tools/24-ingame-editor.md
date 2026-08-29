# 24. 遊戲內編輯器：「施法即編輯」→ 快照 cell 狀態 → 生成 patch mod

← [tools index](README.md)｜[ideas 索引](../ideas.md)｜現行 [spec](../../specs/ingame-scene-export-design.md)｜現行 [plan](../../plans/scene-capture-bridge/README.md)

> 本 idea 已升級，不在此追進度。本頁只保存北極星、設計憲法與能力邊界；現況與待辦以 spec／plan 為準。

## 北極星

不依賴 Creation Kit，在 Skyrim 遊戲內編輯場景、NPC 與作者意圖，最後輸出 JSON，由 ModForge build 成可安裝的 patch。終極目標是把 Skyrim 自身變成 Creation Kit。

具體驗收畫面：玩家在空地擺房屋、城牆、市集與路燈，配置住民，標出村口 map marker、功能區與特效錨點，快照整片區域後產出一個可造訪、可尋路、有住民與互動的城鎮 patch。

## 設計憲法

- 遊戲內 plugin 是**薄記錄器**：採集座標、姿態、狀態與意圖，輸出合法 ModSpec JSON。
- ModForge 在 build-time 做 record 生成、依賴計算、驗證與打包；不要把生成器塞進 runtime plugin。
- 明示優於推導：刪除、移動、命名與所有權都由 registry 記錄，不從 live 狀態猜。
- 不重造已有工具：PROTEUS 可作 NPC 能力參考或選配來源；SkyrimIngameEditor 可供渲染／即時預覽研究。核心缺口是可匯出成 patch 的場景 authoring。
- 遊戲內畫面是真正 WYSIWYG：ENB、Community Shaders、光照、天氣與物理都已套用；外部編輯器保留給離線批次與 LAND。

## 場景範圍

| 能力 | 現行方向 |
|---|---|
| 快照 cell／loaded cells | authored 新物件輸出 placements[]；既有物件的刪除、修改、命名分別輸出 removals[]、overrides[]、references[] |
| 擺放 | Palette／Browser 選 base，ghost 預覽，Editor 微調 transform |
| NPC | cell 掃描排除 actor；明示 capture 或 marker＋agent authoring |
| 語意標註 | annotations[] 保存 label/kind/note/姿態，由 agent 展開成 mapMarkers[]、hazards[]、tags[] 等 |
| 地形 | 不在 runtime 雕 LAND；以 marker 標註意圖，離線走 Godot／PNG heightmap → ModForge |
| navmesh | 阻擋物由 L_NAVCUT；新平台走 navPatches[]；遊戲內 sc nav 採集仍待做 |
| 劇情錄製 | 行為路徑／事件節點是後排，不能把 marker MVP 冒充完成 |

## 開放式調色盤

- 滴管擷取準星 ref 的 base、rotation、scale 與可表示的 extra data，存入具名 Palette slot。
- runtime FormID 高位是 load-order index；匯出前必須以 TESDataHandler 反解成耐久的 plugin:0xLOCALID。
- 任意外部 base 進 placements[].base 後，ModForge 依 FormLink 自動形成 master；UI 與 requires 報告必須讓依賴可見。
- 範圍吸取是獨立能力：先預覽命中數再確認，不與單點滴管或整 cell 掃描混為一談。
- 本工具新增的 dynamic ref 可真刪除；既有 authored ref 的橡皮擦輸出 removals[]。

## NPC 與身份

- 預設可由 ModForge 直接生 NpcSpec；大量無名住民不應被 facegen 阻塞。
- sc capp 可直接擷取玩家 base TESNPC 的外貌、裝備、perk、法術與顯式 actor values，不需 PROTEUS clone 中介。
- PROTEUS 仍可作選配能力，但 runtime clone 的 base/ref 耐久性不能被假設。
- 遊戲內只採集 role／backstory；ModForge 的 npcRoles[] macro 把 blacksmith 等 role 展開成 conditioned Hello、package、faction/service。
- 玩家身份系統 IdentitySpec 與 NPC role 不同：前者 gate 玩家對話，後者描述某個 NPC 的職業／行為。
- 自建可散布 facegen 仍是獨立 asset-pipeline GAP，不屬 scene-capture MVP。

## 語意標註對映

| 意圖 | ModForge 產物 |
|---|---|
| 地圖入口／快旅點 | XMRK map marker REFR |
| 火、煙、魔法等特效錨點 | HAZD 或 placed VFX＋Light |
| 功能／身份標籤 | KYWD／既有 macro 的條件輸入 |
| 邊界／領地 | boundary marker＋faction／package 資料 |
| navmesh polygon | navPatches[].polygon；至少 3 點、順序為周長 |

annotations[] 本身是 advisory，bridge 與 ModForge build 都不應自行猜測哪一種生成型別；agent 必須把意圖明示轉成相應 spec。

## 技術邊界

- interior position 是 cell-local；exterior position 是 world-space；rotation 輸出度數。
- actor 不使用 XSCL；靜物／家具／光可用 PlacementSpec.Scale。
- LAND 無 runtime 變形路徑；修改後需 export→build→重新載入。
- vanilla 物品欄只適合可攜帶 form type，STAT/TREE/FURN/ACTI/MSTT 不能作通用 Object Window；現行方向是 Browser＋world ghost。
- SSE runtime 對多數 STAT/ACTI/FURN 不保留 EditorID，搜尋以 model path＋name＋durable id 為基礎，完整 EDID 由離線 catalog 補。
- navmesh 不是 waypoint graph；兩點連線不會生成可走面。

## 最小垂直切片

1. 遊戲內擺一棟房屋並調整姿態。
2. 以 marker 或 capture 配置一名住民並指定 role。
3. 放一個 map marker 與一個特效／功能標註。
4. 匯出合法 ModSpec JSON。
5. ModForge validate/build 後，patch 在遊戲中就位且 editor chrome 零外洩。

## 導航

- 現行場景契約：[ingame-scene-export-design](../../specs/ingame-scene-export-design.md)
- 採集橋 phase 與 backlog：[scene-capture-bridge plan](../../plans/scene-capture-bridge/README.md)
- navmesh：[navmesh plan](../../plans/navmesh.md)
- PROTEUS finding：[proteus](../../../../../analysis/mod-survey/findings/proteus.md)
- Tundra Defense finding：[tundra-defense](../../../../../analysis/mod-survey/findings/tundra-defense.md)
- SKSE Menu Framework：[skse-menu-framework-3](../../../../../analysis/mod-survey/findings/skse-menu-framework-3.md)
