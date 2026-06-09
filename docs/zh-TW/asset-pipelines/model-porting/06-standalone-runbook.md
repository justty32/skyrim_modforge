# 06 — 獨立回家施工手冊（從這裡開始）

← [README](README.md) · 上一份：[05-modforge-integration.md](05-modforge-integration.md) · 下一份：[07-skinned-characters.md](07-skinned-characters.md)

回家機器（Manjaro 主力、Windows 重開機）的複製貼上路徑。不動 ModForge 程式碼——先手跑證明 pipeline（[05] 之後自動化）。每步證明一件事；當前步驟未跑通前別推進。需要*為什麼*時連結會跳到細節。

> 開始前備好：一個 ModForge 建好的測試 plugin（`.esp`），含**一個 `StaticSpec`** 其 `Model` 指向你要做的 nif；MO2 + Skyrim SE 走 Proton；一個通用來源 mesh（`crate.fbx`/`.obj`/`.gltf`）+ 其貼圖；一個抽出的已知尺寸 **vanilla `.nif`** 當尺。

整個靜態 MVP 是**原生 Manjaro——零 Wine、零重開機。** 重開機只出現在蒙皮步驟（[07]）。

---

## 步驟 0 — 工具鏈 sanity（~10 分鐘）

```bash
blender --version                                   # 啟動
compressonatorcli --version                         # 原生 dds CLI 在
# NifTools addon：Blender → Preferences → Add-ons → 啟用 io_scene_niftools
#   確認 File → Export → NetImmerse/Gamebryo (.nif) 存在
```
**證明：** 原生匯出器 + dds 壓縮器就緒。→ 細節：[01](01-toolchain-setup.md)。

---

## 步驟 1 — 匯入 + 校準 transform（~30 分鐘 — 靜默失敗守門）

1. 新 Blender 場景。匯入 **vanilla 尺** nif（NifTools）——記其真實尺寸。
2. 匯入你的 `crate.fbx`。轉到 **Z-up / −Y forward**、縮放對上尺。
3. **記下**此來源慣例的縮放 + 旋轉。
4. `Ctrl+A → Rotation & Scale`（烤進去）。三角化、`Shift+N` 法線、確保有 UV map。

**證明：** mesh 正確定向/縮放——第二大靜默失敗（路徑之後）。→ 細節：[02](02-source-mesh-prep.md) §2。

---

## 步驟 2 — 貼圖 → `.dds`（~10 分鐘）

```bash
mkdir -p ~/model-work/out/textures
compressonatorcli -fd BC1 -miplevels 20 ~/model-work/src/crate_d.png  ~/model-work/out/textures/crate.dds
# normal：來源若 OpenGL 反相綠（[03] §4），再：
compressonatorcli -fd BC7 -miplevels 20 ~/model-work/src/crate_n.png  ~/model-work/out/textures/crate_n.dds
```
**證明：** 合法 BCn + mipmaps（Skyrim 取樣的格式）。→ 細節：[03](03-materials-textures.md)。

---

## 步驟 3 — Blender 內材質映射（~15 分鐘）

在 Blender 材質，把 slot 指向你剛做的 `.dds`（diffuse → slot 0、normal → slot 1；True PBR 則 RMAOS pack）。把 **Data-relative 路徑**精確設成它們會在 `Textures/` 下的樣子（如 `Textures\Mine\crate.dds`）。

**證明：** nif 會帶正確貼圖路徑。→ 細節：[03](03-materials-textures.md) §1,§3。

---

## 步驟 4 — 加碰撞（~15 分鐘）

複製 mesh → `Mesh → Convex Hull`（或 box）。依 NifTools 碰撞慣例命名，設 bhk material（石/金屬）。

**證明：** 靜態不會被穿過。→ 細節：[04](04-nif-and-collision.md) §3。

---

## 步驟 5 — 匯出 nif（~10 分鐘 — 原生）

```
File → Export → NetImmerse/Gamebryo (.nif)
  Game: Skyrim Special Edition      （吐 NiTriShape — SSE 內可用）
  Apply Scaling: 依步驟 1 常數
  → ~/model-work/out/crate.nif
```
在 NifSkope 開 `out/crate.nif`：確認幾何、`BSLightingShaderProperty` + 貼圖路徑、一個 `bhkConvexVerticesShape`/`bhkBoxShape`。

**證明：** 可出貨 SSE 靜態，**原生、無 Wine、無 SSE-optimize。** → 細節：[04](04-nif-and-collision.md) §1,§2。

---

## 步驟 6 — 放檔到決定性路徑（~20 分鐘 — ★ 脊椎證明）

最高價值步：spec 裡、硬碟上、nif 內的路徑必須全一致。錯 = 隱形、**無報錯**。
```
<MO2 mod>/Meshes/Mine/crate.nif
<MO2 mod>/Textures/Mine/crate.dds
<MO2 mod>/Textures/Mine/crate_n.dds
```
…但依 [[mo2-reinstall-reverts-manual-pex]]，經你正常打包流程把它們組進**build zip / mod 資料夾**——別手改 live MO2 資料夾。確認你的 `StaticSpec.Model` = `Meshes\Mine\crate.nif`（完全相符、含大小寫）。

**證明（遊戲內、手動）：** 走 MO2/Proton 啟動，擺放靜態（console `player.placeatme <FormID>` 或 cell 編輯）→ **它渲染、有貼圖、有碰撞。** 這驗證路徑映射 + 轉換 + 打包——整條脊椎——**零 Wine。** → 細節：[04](04-nif-and-collision.md) §4。

> 若隱形：幾乎一定是路徑（大小寫 / 子資料夾 / 副檔名）。三者互相對照重查。這就是這步存在的理由。

---

## 步驟 7 — 交給 ModForge（~之後）

步驟 1–6 成為可靠手動 recipe 後，依 [05] 實作 `importmesh` CLI step + `convert.py` + `Mesh.cs`。手冊*就是* spec：每個手動步驟對映 [05] §2 的一個 stage。

---

## 步驟 8 — 蒙皮升級（重開機進 Windows）

對**角色/防具** mesh，靜態止步——你需要骨架 + skin + `BSDismember`。那是 PyNifly 路：
1. **重開機進 Windows。** Blender + PyNifly。
2. 重定向來源骨架 → Skyrim 骨架（per-source bone map）、clamp ≤4 weights/vertex、建 `BSDismemberSkinInstance` partitions（Outfit Studio Copy-Bone-Weights）。
3. PyNifly 匯出蒙皮 nif。複製回 Manjaro build 樹。

**證明：** 蒙皮 mesh 在 Skyrim。重定向是牆；動畫（`.hkx`）是另一條管線。→ 細節：[07](07-skinned-characters.md)。

---

## 速查 — 整個靜態 MVP 一螢幕

```
0  blender + compressonatorcli + NifTools addon            → 工具就緒          [原生]
1  匯入尺 + mesh → Z-up/−Y、縮放對齊、套用                  → 正確 transform    [原生]
2  compressonatorcli -fd BC1/BC7 -miplevels 20             → .dds + mipmaps     [原生]
3  把材質 slot 指向 Data-relative .dds 路徑                 → nif 將帶路徑       [原生]
4  Mesh → Convex Hull、命名、bhk material                   → 碰撞              [原生]
5  Export → Skyrim SE → NiTriShape .nif（SSE 內可用）       → 可出貨靜態        [原生]
6  路徑一致（spec = 硬碟 = nif）、打包、遊戲內              → 渲染 + 碰撞       ★ 脊椎 [原生]
7  實作 importmesh + convert.py + Mesh.cs                   → ModForge 自動化
8  （角色）重開機 → Windows → PyNifly 蒙皮匯出              → 蒙皮 mesh        [Windows]
```
★ 步驟 6 是隱形-on-錯路徑、證明 ModForge 決定性槓桿的那步——給它最多心力。步驟 0–6 永不離開 Manjaro。
