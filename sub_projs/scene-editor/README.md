# scene-editor — 視覺場景編輯器實驗

對應 [idea §15](../../workflows/idea/tools.md#15-unity--blender-插件作為-ck-替代視覺場景編輯器2026-06-15)。

**目標**：找到一條可行的「視覺化擺放物件 → ModForge `placements[]` spec JSON → ESP」管線，擺脫 Creation Kit 對場景搭建的壟斷。

---

## 工具清單（已驗證真實存在）

| 工具 | 類型 | 用途 | GitHub |
|------|------|------|--------|
| **F4RefToBlender** | xEdit 腳本 + Blender Python | xEdit REFR 匯出 → Blender 重建場景 | [6ooflames/F4RefToBlender](https://github.com/6ooflames/F4RefToBlender) |
| **PyNifly** | Blender 插件 | SSE NIF 讀寫（F4RefToBlender 依賴） | [BadDogSkyrim/PyNifly](https://github.com/BadDogSkyrim/PyNifly) |
| **SIGE**（Skyrim In-Game Editor） | SKSE 插件 | 遊戲內 3D gizmo 視覺擺放，active 2025 | [Jonahex/SkyrimIngameEditor](https://github.com/Jonahex/SkyrimIngameEditor) |
| **Spriggit** | CLI | ESP ↔ YAML/JSON 序列化，可 Git 版控 | [Mutagen-Modding/Spriggit](https://github.com/Mutagen-Modding/Spriggit) |
| **SkyUnity** | Unity 專案 | C# ESP/BSA 解析 + Unity cell 重建 | [Suslanium/SkyUnity](https://github.com/Suslanium/SkyUnity) |
| **Creation Companion** | 桌面 IDE | Mutagen-based CK 替代 IDE，active 2025 | [Elscrux/Creation-Companion](https://github.com/Elscrux/Creation-Companion) |
| **xEdit REFR 匯出腳本** | Pascal 腳本 | 匯出 REFR 的 FormID/位置/旋轉/縮放到 JSON+CSV | [gemini-research 05](../gemini-research/idea15-ck-visual-editor/05-xedit-export-script.md)（待驗 API） |

---

## 三條實驗路線

### Route A — Blender 路（優先推薦，今天就能跑）

**概念**：xEdit 匯出 REFR → F4RefToBlender 重建 Blender 場景 → 在 Blender 中編輯 → Python 腳本匯出 `placements[]` JSON → ModForge build ESP。

**前置需求**：Blender 4.x、PyNifly 插件、xEdit（SSEEdit）、F4RefToBlender 腳本。

**實驗步驟**：
1. 安裝 PyNifly（Blender 4.x 插件）
2. clone F4RefToBlender，閱讀 README 了解 workflow
3. 在 xEdit 選一個小 Cell，用 F4RefToBlender 的 xEdit 腳本匯出 REFR CSV
4. 用 BAE 解壓該 Cell 用到的 NIF 到本機資料夾
5. 在 Blender 執行 `importreference.py`，確認場景能重建
6. 試著移動一個物件，再寫一個小 Python 腳本把 Blender scene 的 transform 輸出成 ModForge `placements[]` JSON 格式
7. 把 JSON 餵進 ModForge build，確認 ESP 正確

**實驗記錄** → [experiments/route-a-blender.md](experiments/route-a-blender.md)

---

### Route B — SIGE 路（需要 Skyrim + SKSE）

**概念**：裝 SIGE 在遊戲內視覺擺放物件 → 存成 ESP → 用 Spriggit 把 ESP 序列化成 YAML/JSON → 比對格式、理解 REFR 結構 → 設計 Spriggit output → ModForge spec 對照表。

**前置需求**：Skyrim SE + SKSE + SkyUI、SIGE mod、Spriggit CLI。

**實驗步驟**：
1. 在 MO2 安裝 SIGE
2. 進遊戲，在一個 Cell 裡用 SIGE 擺幾個物件並存成 ESP
3. 用 Spriggit 把那個 ESP 序列化成 YAML
4. 讀 YAML 看 REFR 的結構（position/rotation/scale/base form）
5. 確認 Spriggit YAML 的 REFR 格式能對應到 ModForge `placements[]` spec

**主要用途**：了解「最終 ESP 的 REFR 長什麼樣」，校驗 ModForge 輸出格式是否正確，而非主要的場景編輯工具。

**實驗記錄** → [experiments/route-b-sige.md](experiments/route-b-sige.md)

---

### Route C — SkyUnity 路（需要 Unity 環境）

**概念**：用 SkyUnity 把 Skyrim ESP/BSA 載進 Unity 場景預覽 → 在 Unity 中擺放/修改物件 → C# 腳本匯出 `placements[]` JSON → ModForge build ESP。

**前置需求**：Unity 2021.3+ 或 Unity 6、SkyUnity 專案、.NET SDK。

**實驗步驟**：
1. Clone SkyUnity，在 Unity 開啟
2. 依 README 指示載入一個 Skyrim cell（需要 Skyrim 安裝路徑）
3. 確認 cell 能在 Unity 中正確顯示
4. 寫一個小 C# Editor Script，把 scene 中所有 placed object 的 transform 輸出成 ModForge `placements[]` JSON
5. 確認與 Route A 的輸出格式一致

**主要優勢**：Unity 的 Prefab 系統可快速批量擺放「clutter group」（如一整套桌面擺設），比 Blender 更適合大規模 placed ref 量產。

**實驗記錄** → [experiments/route-c-skyunity.md](experiments/route-c-skyunity.md)

---

## ModForge `placements[]` 目標格式

所有路線的輸出都要對到這個格式（現有 spec，`WireLinkedRefs()` 已支援）：

```json
{
  "placements": [
    {
      "baseForm": "Skyrim.esm:0x00XXXX",
      "cell": "MyCell",
      "position": { "x": 0.0, "y": 0.0, "z": 0.0 },
      "rotation": { "x": 0.0, "y": 0.0, "z": 0.0 },
      "scale": 1.0
    }
  ]
}
```

---

## 進度

| 路線 | 狀態 |
|------|------|
| Route A（Blender） | 未開始 |
| Route B（SIGE） | 未開始 |
| Route C（SkyUnity） | 未開始 |
