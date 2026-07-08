# 14. 資產格式轉換管線（glTF/FBX → NIF）（2026-06-04）

← index: [README.md](README.md) · [ideas 索引](../ideas.md)

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
