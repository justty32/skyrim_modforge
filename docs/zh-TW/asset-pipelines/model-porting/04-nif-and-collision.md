# 04 — NIF 匯出 + 碰撞（格式目標）

← [README](README.md) · 上一份：[03-materials-textures.md](03-materials-textures.md) · 下一份：[05-modforge-integration.md](05-modforge-integration.md)

這是模型移植版的「`.fuz` + 檔名」章節：精確目標格式，加上 ModForge 獨家能決定性算出、別人算不出的那一件事（正確 Data-relative 路徑）。對**靜態 prop** 這完全是原生 Manjaro。

---

## 1. `.nif` 目標（夠安全就好）

NIF = node-graph 二進位場景：`NiNode` transform 帶葉幾何 + property + collision 區塊。對靜態 prop 你正好需要：

```
NiNode (root)
 ├─ BSTriShape / NiTriShape        ← 幾何
 │    ├─ BSLightingShaderProperty  ← 材質（[03]）
 │    │    └─ BSShaderTextureSet   ← Data-relative 貼圖路徑（[03] §1）
 │    └─（vertex data、UVs、normals）
 └─ bhkCollisionObject             ← 碰撞（[§3]）
      └─ bhkRigidBody → bhkShape
```

**Linux 逃生口（已驗證，撐起整個計劃）：**
- SSE 原生幾何區塊是 **`BSTriShape`**（packed/half-precision）。LE 是 **`NiTriShape`**。
- **LE-form `NiTriShape` nif 在 SSE 載得很好**，且因低精度，常被*推薦*用於 SSE 靜態。所以 NifTools-addon 匯出——吐 `NiTriShape`——是**免轉換的有效 SSE 資產**。
- **「SSE-optimization」**（`NiTriShape → BSTriShape` 經 SSE NIF Optimizer / Cathedral Assets Optimizer）是*優化*，靜態**可跳過**。要的話，是末尾的 Wine 一次性。

這就是靜態路為何原生：NifTools 寫 `NiTriShape`，那就是可出貨的 SSE 靜態。

---

## 2. 匯出靜態 nif（NifTools addon，原生）

在 Blender（首次 GUI，之後 headless 走 [05]）：
```
File → Export → NetImmerse/Gamebryo (.nif)
  Game: Skyrim Special Edition   （吐 NiTriShape — SSE 內可用）
  Apply Scaling: 依 [02] 校準
```
NifTools addon 確認（2026）：**basic 非蒙皮 Skyrim SE 匯出** + convex/box 碰撞。它**不**好好做蒙皮 SSE——那是 PyNifly/Windows 路（[07]）——也**不**做 MOPP 凹面碰撞（§3）。

**ck-cmd 替代（Wine）：** `ck-cmd importfbx crate.fbx -e out/` → nif 含 materials/vertex-colors、「95% game-ready」、**LE-form**（SSE 內可用）。若某來源 NifTools 材質映射難搞可用（[01] §3）。同樣 `NiTriShape`-有效邏輯。

---

## 3. 碰撞（靜態 prop 唯一可能的牆）

無碰撞的靜態 = 玩家穿過 / 掉過去。階層：

| 碰撞 | 怎麼做 | 原生？ | 何時 |
|------|--------|--------|------|
| **Box** | bounding box → `bhkBoxShape` | ✅ NifTools | 方塊狀 prop（箱、柱） |
| **Convex hull** | mesh 的 hull → `bhkConvexVerticesShape` | ✅ NifTools（確認：命名 `CollisionPolyhedron` mesh、匯出） | 多數 prop——convex 近似夠用 |
| **Concave (MOPP)** | `bhkMoppBvTreeShape` | ❌ **牆**——NifTools 無法 author MOPP | 玩家須進入的凹/空心幾何 |

**原生 recipe（convex/box）：**
1. 在 Blender，複製 mesh，做 **convex hull**（edit mode `Mesh → Convex Hull`，或低 poly box），依 NifTools 碰撞慣例命名。
2. 設 `bhk` layer / material（如石、金屬）——影響聲音 + 物理。
3. 匯出——NifTools 寫 `bhkCollisionObject → bhkRigidBody → bhkConvexVerticesShape`。

**MOPP 變通（無 Havok SDK）：** 把凹幾何拆成**數個 convex 片**（各一 `bhkConvexVerticesShape`），union 在一個 `bhkRigidBody` / `bhkListShape` 下。或對不進入的 prop 接受 box。真凹面 MOPP 需 Havok 工具——靜態 MVP 範圍外；只在某資產確實需要時回頭。

---

## 4. Data-relative 路徑 — ModForge 的決定性槓桿

這是模型移植版的語音檔名規則：錯字串 = 靜默失敗、且 ModForge 獨家掌控之處。

- nif 把 **Data-relative 貼圖路徑** 烤進 `BSShaderTextureSet`（slot 0 = `Textures\Mine\crate.dds` 等）。
- plugin 的 `StaticSpec.Model`（或 `Furniture`/`Activator`）烤進 **Data-relative mesh 路徑**（`Meshes\Mine\crate.nif`）。
- **兩者必須對上 `package` 實際放檔處。** 打錯字、大小寫錯、子資料夾錯 = 隱形/無貼圖，**無報錯 log**（[[vanilla-nif-paths-must-be-verified]]；`Spec.MagicFx.cs` 的 `Model` 欄位已帶「wrong = invisible」警告）。

**為何 ModForge 免費搞定：** ModForge *就是*產生器——它決定 spec 裡的 mesh 路徑、決定 `package` 複製 `.nif`/`.dds` 到哪、能在匯出腳本時把 texture-set 路徑寫進 nif。所以三者（spec 路徑、on-disk 路徑、in-nif 路徑）出自單一真相源。獨立手動流（[06]）得手工同步；整合流（[05]）讓它們成一個算出的值——同語音檔名規則的「ModForge 擁有識別碼」超能力。

**不開遊戲就驗**（依 `ingame-test-workflow`）：一個 `meshdiag`/`modeldiag` step（對映 `lightdiag`/`identitydiag`）讀建好 esp 的 `STAT`/`FURN`/`ACTI` records、解析各 `Model` 路徑、檢查打包位置有檔 + nif 的貼圖路徑解得開。結構性地抓住主導失敗模式。

---

## 5. 打包進 build 樹

`Assets.cs` 已複製 `Meshes/`、`Textures/`、`Sounds/…` 樹進 MO2 layout，`package` 打包——**nif/dds 搭便車**，正如語音檔搭 `Sound/Voice/...` 複製。輸出樹：
```
<zip root>/
  MyMod.esp
  Meshes/Mine/crate.nif
  Textures/Mine/crate.dds
  Textures/Mine/crate_n.dds
```
無 `.seq` 互動（靜態 ≠ quest）。依 [[mo2-reinstall-reverts-manual-pex]]，一律重建進 zip——絕不手放 live MO2 資料夾。

---

## 6.「完成」長什麼樣

- 一個在 NifSkope 開乾淨的 `NiTriShape` `.nif`：幾何、`BSLightingShaderProperty` + 貼圖路徑、一個 `bhkConvexVerticesShape`/`bhkBoxShape`。
- nif 裡的貼圖路徑**對上**打包的 `.dds` 位置。
-（延伸）一個從建好 esp 不開遊戲驗路徑解析的 `meshdiag`。

→ [05](05-modforge-integration.md) 把此手動匯出變成 `importmesh` CLI step。

---

### 來源
[Blender NifTools addon — Collision Objects（convex-hull → `bhkConvexVerticesShape`、無 MOPP、basic SSE 匯出）](https://blender-niftools-addon.readthedocs.io/en/latest/user/features/collisions/collision_objects.html) · [ck-cmd（GH aerisarn — fbx→nif、LE-form）](https://github.com/aerisarn/ck-cmd) · SSE NIF Optimizer（Nexus #4089）· Beyond Skyrim NIF Data Format。內部：`StaticSpec`/`FurnitureSpec`/`ActivatorSpec`（`Spec.Items.cs`）、`Assets.cs` copy-trees、`Spec.MagicFx.cs`（「wrong = invisible」）。
