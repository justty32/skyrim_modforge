# P6 與後續 — UI、匯出、referrer、captures、Browser

← [phases index](phases.md)｜[backlog](backlog.md)｜[wait_todo](../../../wait_todo/ingame-tests.md)

## UI 與 registry

- `UI.Fields.{h,cpp}` 是 bound text field 的單一擁有者：只有 active item 的 buffer 可暫時不同於 registry；Enter 與 deactivate-after-edit 都提交。
- Palette slot 沒有耐久身份，列表重排時用 `UI::ForgetEdits()`；Eraser／Overrides 以耐久 id hash 當 row key。
- Eraser／Overrides 有 label/note；Captures 有 label/note；Palette note 隨 `scene-capture-palette.json` 落盤。
- `removals[]` 無 label/note 時保持裸字串；有附註時才輸出 `{ref,label?,note?}`，維持舊輸入相容。

## `references[]`

- `src/Referrer.{h,cpp}` 與 `src/UI.References.cpp` 讓 `sc ref`／`sc refc` 為既有 ref 指定全域唯一 label。
- 外部 ref 以耐久 `<plugin>:0xLOCALID` 輸出；檔內 dynamic placement 用 handle identity，在 `AppendPlacements` 蓋穩定 editorId `MFRef_<sanitize(label)>_<seq>`。
- `AppendReferences` 必須晚於 `AppendPlacements`，而且只輸出本次真的存在的 placement editorId；跨 cell、已擦除或 handle 失效時警告並跳過。
- marker proxy、dynamic actor、重複 label 一律拒收；DLL 不自行填 `anchor`。
- package 的 SingleRef 與 location 槽語意不同；要鎖定特定物件只能使用 SingleRef 槽。完整契約見 [spec](../../specs/ingame-scene-export-design.md)。

## captures 與場景分流

- `ExportCell`／`ExportAll` 遇到 actor 直接計入 `actorsExcluded` 並跳過；`placements[]` 不輸出 `kind:"npc"`。
- 明示 NPC／物品擷取走 `capturedNpcs[]`／`capturedItems[]`，獨立輸出 `captures_<YYYYMMDD-HHMM>.json`。
- `sc capp [Label]` 直接讀玩家 base TESNPC；`editorId` 形如 `MFCap_<sanitised label>`，同 label 重擷取維持同一記錄身份。
- 顯式 `health`／`magicka`／`stamina`／18 項 `skills` 優先於 class autocalc；缺欄位仍走舊路徑。
- bridge 對玩家 perk 合併 base 與 `addedPerks`，依耐久 id 去重並取高 rank；是否過濾 player-only pipeline perk 仍在 backlog。
- `isPlayer` 只做可見性，不直接寫任何記錄；玩家缺 `voiceType` 時不猜 fallback。

## 匯出與依賴

- 場景檔名：`scene-export_<cell-or-worldspace-grid>_<YYYYMMDD-HHMM>.json`；全載入 cell 版以 `all-` 區分；同名加 `-2`／`-3`，永不覆蓋。
- `Export requires` 分析輸出 JSON 中真正會形成 link 的欄位，排除 inert/advisory 欄位；C# build 後的 master 名單仍是最終權威。
- dynamic ref 只有 `Placed()` 登記項可輸出；引擎生成的魚、蝴蝶 marker、灰燼堆不得因同為 `0xFF......` 被收進場景。
- 面板的 `adopt dynamic refs in this cell` 是明示逃生門，必須警告它不會區分玩家物件與引擎物件。

## Browser 與 ghost

- `Catalog` 掃 21 種可擺 base，剔除無耐久 id 或無模型路徑者；runtime 對 STAT/ACTI/FURN 取不到 EditorID，因此搜尋索引以 name＋model path＋id 建立。
- 離線 `catalog build` 保存 load order、EDID/FULL/model path 與 provenance；`catalog export-json` 依 FormKey 輸出 winner，schema 為 `schemas/scene-catalog.schema.json`。bridge 合併離線 catalog 仍待辦。
- ghost 不變式：`ghost exists ⇔ mode == place ∧ gh1 ∧ selection exists`。Browser 選擇只是把 place 模式指向一個 base。
- ghost spawn 前先 `ref->SetCollision(false)`，避免 Havok 建碰撞；事後只改 root collision layer 或 scenegraph body 都不足。
- ghost 以 live handle 加 `ExtraTextDisplayData` 哨兵雙層辨識，所有抓 ref 的入口與 exporter 都必須拒收；kPostLoadGame 以哨兵清孤兒。
- Preview 與 Editor 共用 `Numpad.h/.cpp` 的 scancode 與長按時鐘；commit 走 `Palette::PlaceSlot`，不得另建第二套 placement 邏輯。
- ghost 依 OBND 與預覽距離自動縮放到約螢幕九分之一，只縮不放；numpad 0 回到 1.0。

## 部署禁區

不得用 `cp` 就地覆寫執行中的 DLL：Linux/Proton 可截斷已載入 DLL 的 inode，之後 demand-page 會執行錯誤位元且可能沒有 crash log。只能用 `scripts/deploy.sh`：先拒絕執行中的 `SkyrimSE.exe`，再以暫存檔＋`rename(2)` 原子換 inode。
