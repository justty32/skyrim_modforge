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
| 網格/材質 | `.nif`（SSE BSTriShape） | 高（PyNifly / ck-cmd） |
| 貼圖 | `.dds`（BC + mipmaps） | 完全自動（純轉碼） |
| 表情/morph | `.tri` | 高（兩邊都是頂點 delta） |
| 動畫/骨架/物理 | `.hkx`（Havok 二進位） | **這就是那道牆** |

- **靜態物件最接近全自動**：補碰撞（NIF `bhk*` 也是 Havok，但簡單凸包/box 可程式生成）+ 材質映射規則（glTF PBR ↔ `BSLightingShaderProperty`，寫一次批次套）。
- **蒙皮網格半自動**：綁 Skyrim 骨架（`NPC Spine [Spn1]` 命名）、每頂點 ≤4 骨權重、`BSDismemberSkinInstance` 分區；「來源骨架 → Skyrim 骨架」retarget 每體系寫一次（同 §13 哲學）。
- **動畫是真正的牆**：Havok SDK 不公開，社群靠 ck-cmd/hkxcmd 包舊 SDK（版本敏感）；behavior graph 完全無自動轉換（Nemesis/Pandora 領域）。
- **對其他想法的意義**：§13 二次元路線（VRoid/MMD 頭身可管線化、卡在動畫）；§5 資源移植（靜態場景物件是甜蜜點）。⚠️ 他遊資產轉了不能發布。
- **ModForge 視角**：這是**資產層管線**（與記錄層 Mutagen 平行的另一軸）；`package` 已打包 Meshes/Textures，上游接轉換是自然延伸；PyNifly 可腳本化（shell-out 候選，同 xLODGen 態度：不自造）。

---

## 15. Unity / Blender 插件作為 CK 替代視覺場景編輯器（2026-06-15）

**Idea**：用 Unity 或 Blender 插件取代 Creation Kit 的「視覺場景搭建」環節——在 Unity/Blender 中擺放物件/設計地圖，插件輸出 ModForge spec JSON，ModForge 產生合法 ESP。

**為何有吸引力**：CK 是 Windows-only、崩潰率高、難腳本化；Unity/Blender 在 Linux 上跑、插件生態成熟、Unity 與 ModForge 同 .NET ecosystem。目標不是「用 Unity/Blender 做所有事」，而是**只替換 CK 最痛的一件事——視覺化物件擺放**，其餘（對話/腳本/記錄邏輯）繼續走 JSON spec。

**兩條技術路線**：
- **Blender 路（優先候選）**：Python 插件生態成熟；Blender Niftools 已能讀 NIF；插件讀場景 GameObject transform + EditorID 對照表 → 輸出 `placements[]` JSON；ModForge 接手生成 CELL/REFR。現有工具：PyNifly / Blender Niftools。
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
- **TES5Unity**：實驗性，可載入基本 Skyrim cell 進 Unity，frozen

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
