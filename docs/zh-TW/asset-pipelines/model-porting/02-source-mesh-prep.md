# 02 — 來源 mesh 準備（匯入 + 校準，進 NIF 之前）

← [README](README.md) · 上一份：[01-toolchain-setup.md](01-toolchain-setup.md) · 下一份：[03-materials-textures.md](03-materials-textures.md)

你選了**通用 FBX/OBJ/glTF** 當 MVP 來源，所以大門是 Blender 內建匯入器——原生、無解包步驟。本章是把 mesh *正確定向、縮放*地弄進 Blender 並清乾淨到可匯出，因為每個下游步驟都假設場景理智。遊戲專屬解包是附錄（§5），待你挑遊戲再說。

---

## 1. 匯入（原生、內建）

```
File → Import → FBX (.fbx) / Wavefront (.obj) / glTF 2.0 (.glb/.gltf)
```
三者皆 Blender 核心匯入器——無 addon、原生 Linux。glTF 最乾淨（PBR 材質 + 正確單位 metadata 隨檔走）；FBX 最常見於遊戲 rip；OBJ 只有幾何（無骨架、無 PBR——靜態夠用，材質在 [03] 重做）。

**挑一個 mesh** 首跑。單一靜態 prop（箱子、雕像、當靜態用的武器模型）是 MVP 目標——無骨架。

---

## 2. Transform 問題（路徑之後第一大靜默失敗源）

Skyrim 慣例：**Z-up、−Y forward**，且「1 Blender unit = 1 Skyrim unit」*當匯出縮放正確套用時*。來源在兩者都不一致：

| 來源慣例 | Up / Forward | 單位 | 匯入時典型修法 |
|---|---|---|---|
| **glTF 2.0** | Y-up | 公尺 | 繞 +90° X 轉 Z-up；縮到 Skyrim units |
| **FBX（Unity rip）** | Y-up | 公尺 | 同 glTF；Unity humanoid 約 1 m |
| **FBX（UE rip）** | Z-up | **公分** | 軸常 OK；**÷100 再 ×Skyrim-unit**（經典 cm-vs-m 坑） |
| **OBJ** | Y-up（通常） | 無單位 | 轉 + 縮，量測 |

**校準流程（每種來源慣例做一次，之後重用常數）：**
1. 把已知真實尺寸的 vanilla Skyrim `.nif`（如門 ≈ 200 units、酒桶）用 NifTools addon 匯進同場景——這是你的**尺**。
2. 把來源 mesh 匯在它旁邊。
3. 縮放 + 旋轉你的 mesh 直到比例對上尺、且 Z-up/−Y-forward。
4. **記下確切的縮放係數與旋轉**給那種來源慣例。烤進 per-source 規則（[05] `modelSource.sourceType` 帶這個）。
5. **套用** transform（`Ctrl+A → Rotation & Scale`）使其烤進幾何、不留在物件上——匯出器可靠讀已套用的 transform。

> 社群說法：「Blender 內 128 'units' 高的物件，用 Apply Scaling = FBX All 匯出後成 128 Skyrim units；不符幾乎都是公分對公尺。」別信記憶中的魔法常數——**第一次對照 vanilla 尺量測**，之後該來源的常數就可信。

---

## 3. Mesh 衛生（便宜、防匯出錯）

匯出前，在 Blender：
- **三角化**（Skyrim 幾何是三角形；edit mode `Ctrl+T`，或套用 Triangulate modifier）。NifTools/ck-cmd 會三角化，但自己做使結果可預測。
- **一物件 = 一 `NiTriShape`** 預設；prop 多材質時，要嘛按材質拆，要嘛讓匯出器吐多 shape（見 [03] 材質→shape 映射）。
- **套用 transform**（再次 `Ctrl+A`），重算外向法線（`Shift+N`），移除重複頂點（`M → By Distance`）。
- **UV 必須存在**——沒 UV map，[03] 的貼圖無處可落。多數 rip 帶 UV；OBJ/FBX 通常有。
- **Origin** 放合理點（物件 origin 成遊戲內擺放樞紐）。立地 prop，origin 放底部中心。

---

## 4. mesh 放哪

維持 [01] 的工作樹：
```
~/model-work/src/crate.fbx          # 進來
~/model-work/src/crate_diffuse.png  # 進來的貼圖（→ .dds 在 [03]）
~/model-work/out/crate.nif          # [04] 產出
~/model-work/out/textures/...dds    # [03] 產出
~/model-work/vanilla/Barrel01.nif   # §2 的尺
```
這裡沒有東西進 commit——資產依法務規則留本機（README）。

---

## 5. 附錄 — 遊戲專屬解包（後置；之後挑遊戲）

你選了通用格式當 MVP，故此處僅參考。三者皆匯聚到 Blender，§1 接續。細節在 survey [`../03-3d-model-import.md §6`](../03-3d-model-import.md)。

- **Dark Souls / FromSoft → `soulstruct-blender`**（Blender 4.1–5.0，純 Python，**原生 Linux**）。直接匯入 FLVER：角色、物件、裝備、**map pieces（= 靜態）**，含 armatures/weights。*最乾淨*——若之後要真遊戲來源，從這起。
- **Wuthering Waves（UE5）→ FModel / CUE4Parse**（.NET、跨平台、原生 Linux）。匯出 UE 蒙皮/靜態 mesh → FBX/glTF + 貼圖。需遊戲 `.usmap` + AES key。
- **Genshin（Unity）→ 3DMigoto F8 frame-dump + GIMI**（DX11/Windows → Proton/Wine 跑；Blender 重建原生）。*最不 Linux-乾淨*——Genshin 加密 bundle，AssetRipper 直接讀不了。

挑定一個後，本附錄成真章節；在那之前通用匯入器（§1）是路。

---

## 6.「完成」長什麼樣

- 單一來源 mesh 匯入、**Z-up / −Y-forward**、縮到對上 vanilla 尺、transform **已套用**、三角化、有 UV。
- per-source 縮放+旋轉常數記下（同來源下個 mesh 跳過校準）。

→ [03](03-materials-textures.md) 處理材質/貼圖，再 [04](04-nif-and-collision.md) 匯出 nif。

---

### 來源
Blender 內建 FBX/OBJ/glTF 匯入器（原生）。Scale/orientation：[Beyond Skyrim Arcane University — Mesh Export to NIF](https://wiki.beyondskyrim.org/wiki/Arcane_University:Mesh_Export_to_NIF)、[Getting Your Models into Skyrim (morroblivion)](https://morroblivion.com/files/modeltoskyrimguide.pdf)。解包（附錄）：[soulstruct-blender (GH Grimrukh)](https://github.com/Grimrukh/soulstruct-blender)、[FModel](https://fmodel.app/)、[GI-Model-Importer](https://github.com/SilentNightSound/GI-Model-Importer)——與 survey [`../03-3d-model-import.md`](../03-3d-model-import.md) §6。
