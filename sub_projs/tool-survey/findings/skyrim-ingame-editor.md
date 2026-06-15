# SkyrimIngameEditor — Tool Survey Finding

**GitHub**: https://github.com/Jonahex/SkyrimIngameEditor
**Author**: Jonahex | **License**: MIT | **Last release**: 2025-05-25
**Surveyed**: 2026-06-15（原始碼完整閱讀）

---

## 工具定位

遊戲內即時 record 編輯器（SKSE plugin）+ C# EspGenerator 匯出 .esp。
基本上是「遊戲內 Creation Kit 子集」：改參數立刻看效果，滿意後匯出 ESP。

---

## 架構（原始碼層級）

### C++ 端：IngameEditor（SKSE plugin）

| 元件 | 路徑 | 功能 |
|------|------|------|
| Core/Renderer | `IngameEditor/Core/` | D3D11/DXGI hook，ImGui 注入遊戲畫面 |
| TargetManager | `Utils/TargetManager.cpp` | 用 `Console::SelectReference` 以螢幕座標選取 `TESObjectREFR`；支援 1.5.97/1.6.640/1.6.1130+/VR |
| TargetEditor | `Gui/TargetEditor.cpp` | 顯示選中 reference 的 GUI：Enable toggle、`ReferenceTransformEditor`（位置/旋轉/縮放）、3D NiObject tree、ActorState、ActorValues、AI/Package、Behavior graph viewer |
| Serializer | `Serialization/Serializer.cpp` | `EnqueueForm(form)` → 序列化為 JSON；`Export(path)` → 呼叫 EspGeneratorWrapper.dll |
| MainWindow | `Gui/MainWindow.cpp` | 主 GUI 入口；草地 visibility/fade、render pass stats 等 debug 功能 |
| 編輯器模組 | `Gui/` | WeatherEditor、CellEditor、ImageSpaceEditor、LightingTemplateEditor、WaterEditor、NiObjectEditor、NiTransformEditor、VolumetricLightingEditor、DALC Editor、FootIkEditor、ShaderParticleEditor |

**Serializer.EnqueueForm 序列化結構**（傳給 EspGenerator 的 JSON）：
```json
{
  "Master": "Skyrim.esm",
  "Override": "SomePlugin.esp",
  "FormKey": "012345:Skyrim.esm",
  "Form": { /* Mutagen-compatible 欄位 */ },
  "References": ["Skyrim.esm", "Update.esm"]
}
```

### C# 端：EspGenerator（Mutagen）

`EspGenerator.Export(outputPath, jsonString)` 處理三類 record：

| 分支 | 型別 | 現況 |
|------|------|------|
| `ICellGetter` | Cell（interior/exterior） | ✅ 完整：block/subblock 定位、duplicate、JSON populate |
| **`IPlacedGetter`** | **Reference（placed object）** | ✅ **已實作**（line 283-314）：讀 `Form.Cell` FormKey → 找到 Cell → duplicate placed record → 加進 `Cell.Persistent` 或 `Cell.Temporary`（依 `0x400` flag） |
| Generic `IMajorRecord` | 其他所有 record | ✅ 完整：duplicate + populate + 加進 top-level group |
| `ILandscapeGetter` | LAND 高度圖 | ❌ **未實作** |

---

## 對「擴展以取代 CK 場景/地景編輯」的分析

### Feature 1：Reference / Object 放置與移動

**C# 端（EspGenerator）**：✅ **已就位**
- `IPlacedGetter` 分支已完整實作（line 283-314 in `EspGenerator.cs`）
- 支援從 JSON 的 `Form.Cell` 欄位找到所屬 cell，duplicate placed object，正確放進 Persistent/Temporary group

**C++ 端（SKSE plugin）**：✅ 大部分已就位，缺「新增」和「存檔觸發」

已有：
- `TargetManager::TrySetTargetAt()` — 以滑鼠點擊選取現有 reference
- `TargetEditor` 中的 `ReferenceTransformEditor` — 即時拖拉修改位置/旋轉/縮放
- `Serializer::EnqueueForm()` — 將改過的 form 排入待匯出佇列

缺少：
1. **新建 reference** — 選一個 base form（STAT/NPC/ACTI 等）+ 放置到玩家前方（或滑鼠位置）
2. **Remove reference** — 標記 reference 為 deleted
3. **存檔/匯出按鈕** — 在 UI 加個「Save to ESP」觸發 `Serializer::Export()`（目前只在 `OnQuitGame` 自動匯出）
4. **Serializer 的 CollectReferences** 尚未支援 `IPlacedGetter`（目前只有 Cell/Water/Weather），需補 master 收集邏輯

**實作難度**：低～中。C# 端零改動；C++ 端主要是加 UI 按鈕 + form placement API 呼叫（`TESDataHandler::PlaceObject` 或類似）。

---

### Feature 2：Heightmap / LAND 地形編輯

**C# 端（EspGenerator）**：❌ 未實作
- 需新增 `ILandscapeGetter` 分支：序列化 VHGT（高度圖）、VNML（法線）、VCLR（頂點色）到 ESP
- Mutagen 有 `Landscape.HeightData` / `Landscape.VertexNormals` / `Landscape.VertexColors` 屬性可用

**C++ 端（SKSE plugin）**：❌ 未實作（是較重的部分）
- 需要：存取當前 cell 的 `TESObjectLAND::landData`，提供「筆刷」工具（以滑鼠位置 raycast 到地形 → 抬升/降低 `heightData[y][x]`）
- 地形 vertex 座標計算、法線重算、即時 mesh refresh 是主要複雜度
- 可參考 TES5Edit 的 LAND record 定義（`wbDefinitions.pas`）確認欄位佈局

**實作難度**：高。地形筆刷需要 raycast + vertex height access + 法線重算 + render refresh，是 CK landscape editor 的核心功能子集。

---

## 與 ModForge 的關係

| 面向 | 說明 |
|------|------|
| 調參輔助 | 在遊戲中調好 Weather/LGTM/ImageSpace/Cell/Water 參數，數值直接搬進 ModForge JSON spec |
| EspGenerator 參考 | Cell block/subblock 定位邏輯可作為 ModForge 相關功能的參考實作 |
| Reference 放置 | SIE 擴展後可在遊戲內目視放置物件，位置確認後用 EspGenerator 匯出 ESP，再用 ModForge 補 Quest/Dialogue/Script |
| 不直接整合 | SIE 走 SKSE plugin 路線，ModForge 走離線 JSON→ESP 路線；兩者互補不整合 |

---

## 已知問題

- 啟用時某些音效停播（crossbow reload、lockpick sound）— Hooks.cpp 副作用，open issues #1/#2

---

## 擴展實作建議（優先序）

1. **先做：存檔按鈕** — 在 MainWindow 加「Save to ESP」觸發 `Serializer::Export()`，驗證現有 form 編輯能正確輸出
2. **次做：Reference 新建** — `PlaceObjectAtPlayer()` helper + base form 選擇器 + EnqueueForm
3. **後做：Reference 刪除** — 設 deleted flag + EnqueueForm
4. **長期：LAND 高度圖編輯** — C++ raycast 筆刷 + C# ILandscapeGetter 序列化

Repo 已 clone 至：`sub_projs/tool-survey/repos/SkyrimIngameEditor/`（shallow, gitignored）
