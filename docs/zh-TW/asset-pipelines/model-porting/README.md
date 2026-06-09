# 模型移植 — 詳盡實作計劃（外部 mesh → Skyrim SE `.nif`）

景觀調查 [`../03-3d-model-import.md`](../03-3d-model-import.md) 的深潛續篇。那份是*有什麼*；本資料夾是*我回家怎麼做*——詳盡到在 Manjaro 上是複製貼上 runbook，而非重新做決策。

**研究/計劃日期：** 2026-06-09。**家用機：** 雙系統——**Manjaro（主力）** + **Windows（需要時重開機）**，NVIDIA 16 GB VRAM。**狀態：** 僅計劃，未動程式碼。本資料夾是**研究**——不動維護鏈（code → CODE_MAP → docs）；之後落地任一條依 CLAUDE.md Workflow 1。

> **法務 / 範圍（不變的前提，同 survey）：** 個人、單人、**不發布**用途。移植的商業遊戲資產（Genshin/WuWa/FromSoft、Unity-Store/Nexus 包）轉檔後**只在本機**使用；絕不發布轉出的 mesh 或貼圖。它們仍是原權利人的財產。

---

## 鎖定的決策（你的選擇，2026-06-09）

| 抉擇 | 決定 | 對本計劃的影響 |
|------|------|---------------|
| **nif 匯出後端** | **雙系統分層。** 靜態走 NifTools addon（原生 Manjaro）；**蒙皮/複雜碰撞重開機進 Windows 走 PyNifly**。 | 靜態 MVP 100% 原生 Linux。蒙皮升級是*乾淨重開機*，不是脆弱的 Wine-Blender 或 VM——所以 PyNifly 的黃金品質蒙皮/碰撞/BSDismember 真的可用（[07](07-skinned-characters.md)）。 |
| **MVP 來源** | **通用 FBX / OBJ / glTF**（不綁遊戲）。 | 解包章節很薄——Blender 內建匯入器就是大門（[02](02-source-mesh-prep.md)）。遊戲專屬解包器（DS/WuWa/Genshin）放附錄，待你挑遊戲再說。 |
| **範圍** | **靜態先做**（深、可跑），**蒙皮設計但後置**。 | [02]–[06] 是可端到端跑的靜態脊椎。[07] 是蒙皮設計——點名牆，尚非 runbook。 |
| **貼圖目標** | **兩者，build 時選**（`materialProfile: legacy \| truepbr`）。 | [03] 記錄兩條 channel mapping；True PBR 為建議預設（你 baseline 已含 Community Shaders）。 |

---

## 脊椎（靜態物件，甜蜜點）

```
source .fbx/.obj/.gltf  (+ 來源貼圖)
        │  [02] Blender 內建匯入（原生）
        ▼
   Blender 場景  ──[02]── 修 scale/orientation（每種來源慣例一個常數）
        │
        ├─[03]─ material → BSLightingShaderProperty / True PBR  +  貼圖 → .dds（Compressonator 原生）
        │
        ├─[04]─ 生 convex/box bhk 碰撞（Blender 原生 — bhkConvexVerticesShape）
        ▼
   NifTools addon 匯出  ──[04]──  NiTriShape .nif（LE-form，SSE 內可用——Linux 逃生口）
        │
        ▼
   Data/Meshes/...nif  +  Data/Textures/...dds（Data-relative 路徑烤進 BSShaderTextureSet）
        │  [05] ModForge：StaticSpec.Model → package 複製 Meshes/Textures 樹
        ▼
   遊戲內帶碰撞的 static
```

脊椎上每一步都是**原生 Manjaro、零 Wine、零重開機。** 這正是「靜態先做」的全部用意：在碰 Havok/骨架牆之前先證明資產層 shell-out。

---

## Build 順序（照順序做什麼）

1. **[06] runbook 步驟 0–2** — 工具鏈 sanity + 匯入 + 轉換通用 mesh。證明 Blender↔NifTools 原生。
2. **[06] 步驟 3–5 / [04]** — 匯出帶 convex 碰撞的 `NiTriShape` 靜態 `.nif`；NifSkope 驗。證明格式目標。
3. **[06] 步驟 6 / [03]** — 貼圖 → `.dds`，路徑寫進 nif。證明材質渲染。
4. **[06] 步驟 7 / [05]** — 用 ModForge `StaticSpec` + `package` 手動擺放；遊戲內載入。★ **證明脊椎。**
5. **[05]** — 把手動 recipe 折進 `importmesh` CLI step。
6. **[07]** — *（之後）* 蒙皮角色：重開機進 Windows、PyNifly、重定向。

---

## 檔案索引

| 檔案 | 涵蓋 |
|------|------|
| [01-toolchain-setup.md](01-toolchain-setup.md) | Manjaro 安裝（Blender + NifTools + Compressonator）+ Windows 側（PyNifly）+ Wine 工具 + 可換後端契約 + VRAM/CPU 現實檢查 |
| [02-source-mesh-prep.md](02-source-mesh-prep.md) | 通用 FBX/OBJ/glTF 匯入；scale/orientation/units 校準；mesh 衛生；（附錄）遊戲解包器 |
| [03-materials-textures.md](03-materials-textures.md) | material → BSLightingShaderProperty / True PBR；貼圖 → `.dds`（Compressonator/texconv）；legacy vs RMAOS channel repack；normal-map 慣例 |
| [04-nif-and-collision.md](04-nif-and-collision.md) | `.nif` 目標；NifTools 靜態匯出（`NiTriShape`）；convex/box `bhk` 碰撞（原生）vs MOPP（牆）；Data-relative 路徑正確性——ModForge 的決定性槓桿 |
| [05-modforge-integration.md](05-modforge-integration.md) | `modelSource` spec 區塊；`importmesh` CLI step；`Mesh.cs` shell-out（Papyrus.cs 模式）；env-var 後端；package wiring；維護鏈落點 |
| [06-standalone-runbook.md](06-standalone-runbook.md) | 複製貼上回家 runbook，步驟 0→8，雙系統感知；一螢幕速查 |
| [07-skinned-characters.md](07-skinned-characters.md) | *（後置設計）* 骨架 bone-map、per-source 重定向、≤4 weights、`BSDismemberSkinInstance`、Outfit Studio copy-bone-weights、PyNifly 匯出（Windows 重開機）、handoff 到 `.hkx` |
| [08-extract-darksouls.md](08-extract-darksouls.md) | **Dark Souls / FromSoft 來源** — soulstruct-blender（原生 Manjaro、純 Python）；DCX/BND/TPF/FLVER；map pieces（靜態 MVP）vs 角色；DSR/DS3 最乾淨 |
| [09-extract-wuwa.md](09-extract-wuwa.md) | **Wuthering Waves 鳴潮來源** — FModel/CUE4Parse；UE5 AES 金鑰 + `.usmap`；glTF 匯出（Windows 側、雙系統）；Nanite 抽稀 + UE 材質重做坑 |
| [10-extract-genshin.md](10-extract-genshin.md) | **Genshin Impact 原神來源** — 3DMigoto F8 frame-dump + GIMI；加密 Unity bundle（AssetRipper 讀不了）；Windows 側 dump → Blender 重建（原生）；toon/NPR 重做 + ToS/反作弊風險 |

---

## 主要風險（承自記憶 + survey）

- **錯的 nif/貼圖路徑 = 隱形物件、無報錯** — 主導失敗模式。Data-relative 字串烤進 nif；ModForge 擁有且能驗（[[vanilla-nif-paths-must-be-verified]]，`Spec.MagicFx.cs` 的 `model` 欄位已警告「wrong = invisible」）。[04] §4 是緩解。
- **Scale/orientation** — Skyrim Z-up、−Y forward；來源各異（Unity Y-up/m、UE Z-up/cm、glTF Y-up/m）。一個沒校準的常數 = 巨大/微小/側躺。每種來源對照已知尺寸的 vanilla nif 校準（[02] §2）。
- **缺/壞碰撞 → 穿模。** 靜態至少要 convex/box `bhk`；NifTools 原生做 convex/box，**MOPP（凹面）是牆**——拆成 convex 片或接受 box（[04] §3）。
- **MO2 重裝還原手放檔** — 一律重建進 zip，絕不手放 live mod 資料夾（[[mo2-reinstall-reverts-manual-pex]]）。
- **Linux 蒙皮匯出是弱點** — 故有「蒙皮重開機進 Windows 用 PyNifly」的決定（[07]）。

---

### 來源
工具事實 2026-06-09 確認（各檔 Sources）：PyNifly（GH BadDogSkyrim，只有 Windows/NiflyDLL）· Blender NifTools addon（GH niftools — convex-hull→`bhkConvexVerticesShape`、basic Skyrim SE 非蒙皮匯出、無 MOPP）· ck-cmd（GH aerisarn — fbx→nif，LE-form）· soulstruct-blender（GH Grimrukh — Blender 4.1–5.0）· Compressonator（GH GPUOpen-Tools — Win/Linux/Mac CLI、BC1–7、mipmaps）· Outfit Studio（GH ousnius — 有 Building on Linux、Copy Bone Weights）· Community Shaders True PBR。內部事實取自 `Spec.Items.cs`、`Assets.cs`、survey 與 CLAUDE.md。
