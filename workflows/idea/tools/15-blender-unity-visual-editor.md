# 15. Unity / Blender 插件作為 CK 替代視覺場景編輯器（2026-06-15）

← index: [README.md](README.md) · [ideas 索引](../ideas.md)

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
