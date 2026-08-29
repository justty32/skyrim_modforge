# P2–P4 — 場景編輯原語

← [phases index](phases.md)｜[spec](../../specs/ingame-scene-export-design.md)｜[backlog](backlog.md)

## P2：刪除、新增、修改

### Eraser

- authored ref：`Disable()` 並以耐久 id 登記，匯出到全域 `removals[]`。
- 本工具新增的 dynamic ref：真刪除且不留 `removals[]`；marker proxy 交給 `Markers::Remove`。
- 面板支援逐筆 undo、clear、名稱／原座標、`this cell only`；外部 master 必須醒目標示。
- disabled dynamic ref 不匯出；被登記 removal 的 authored ref 不再計入 preexisting。

### Palette

- `src/Palette.{h,cpp}` 以滴管擷取 base、rotation、scale、extra data；runtime-only base 拒收。
- 擺放統一走 `Palette::PlaceSlot`；palette、Browser ghost commit 不得各自複製生成路徑。
- actor 不回填 scale；`py0/py1`、`ed0/ed1` 等 rider 必須沿共用路徑套用。
- `scene-capture-palette.json` 跨存檔保存；檔內順序等於面板順序。`replace from file` 只在新檔有效且有可用項目時清空舊值。
- `clear all slots` 需二次確認並提供 session 內 `undo clear`。

### Editor 與 `overrides[]`

- `src/Editor.{h,cpp}` 提供位移、三軸旋轉、scale、per-axis revert、commit、cancel；actor 略過 scale。
- authored ref 只有在 commit 時進 `src/Overrides.{h,cpp}`；重複編輯更新 live pose，first-select baseline 不變。
- dynamic ref 直接以 live pose 進 `placements[]`，不進 `overrides[]`。
- `overrides[]` 由 `Spec.Overrides.cs`／`BuildOverrides` 消費；position/rotation 是完整值而非 delta，actor 不帶 scale。
- 進編輯時凍結 Havok，commit/cancel 後恢復；輸出採恢復後的 live pose。

## P3：動態物件

- 選中後以 `SetMotionType(Keyframed)` 路徑凍結，編輯結束恢復。
- authored 位置是輸出真相；載入 patch 後 Havok 自行沉降，與 CK 慣例一致。
- `noHavokSettle` 對應 REFR flag `0x20000000`，只阻止載入時 settle，不代表物件不可推動。

## P4：範圍採集

- 預定使用 `ForEachReferenceInRange` 的 bound 半徑，先預覽命中數再確認。
- 範圍採集仍屬 backlog；不得把單點滴管或整 cell 匯出冒充完成。

## 共通 guard

- 射線選取必須是明示入口；牆／地板本身也是 ref，自動 fallback 會把按空誤判成選中。
- 編輯器 register 的物件一律使用耐久 id 或 handle identity；明示登記優於從 runtime 狀態推導。
- 外部 master 必須在 UI 與匯出依賴報告中可見。
