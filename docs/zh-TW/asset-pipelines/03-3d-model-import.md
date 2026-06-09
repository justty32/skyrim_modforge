# 3D 模型匯入管線（外部網格 → Skyrim SE `.nif`）

← 索引：[README.md](README.md) · 相關：[IDEAS.md §14](../IDEAS.md)（格式轉換）、[external_assets.md](../external_assets.md)、[04-map-scene-porting.md](04-map-scene-porting.md)

**研究日期：** 2026-06-08。僅限個人/單機 — 移植的商業遊戲資源（Genshin/WuWa/Dark Souls、Unity-Store/Nexus 套件包）僅在**本機**轉換與使用，絕不重新散布。

**Linux 的重點：** 最大的工具決策是 **NifTools-addon vs PyNifly vs ck-cmd**，因為 **PyNifly 只支援 Windows**（隨附原生 `NiflyDLL`）。這使得建議的 Linux 管線偏離 IDEAS.md §14 所假設的（「PyNifly / ck-cmd」）。見 §2。

---

## 1. Skyrim `.nif` 目標格式

NIF（NetImmerse/Gamebryo）= 一個節點圖二進位場景：`NiNode` 變換加上葉節點幾何 + 屬性 + 碰撞區塊。

- **幾何 — `BSTriShape`（SSE）vs `NiTriShape`（LE）。** SSE 將 `NiTriShape`+`NiTriShapeData` 合併為單一 `BSTriShape`，使用打包/半精度頂點。**反直覺但已驗證的事實：LE 格式的 `NiTriShape` nif 在 SSE 中能正常載入，不嚴格需要轉換** — 而且因為精度較低，LE 風格的 nif 對 SSE 來說常常是*被推薦*的。**這就是 Linux 的逃生口**（§9）：一個可在 Linux 匯出的 `NiTriShape` nif 就是一個有效的 SSE 資源。
- **「SSE-optimized」** = 某個工具（SSE NIF Optimizer / Cathedral Assets Optimizer）將 `NiTriShape→BSTriShape` 重寫 + 調整 `NiSkinPartition`。這是一種*優化*，對 static 而言並非硬性要求。
- **材質 — `BSLightingShaderProperty`** 持有光照模型 + 旗標 + 指向一個 **`BSShaderTextureSet`** 的連結（有序插槽：0 diffuse、1 normal `_n`、2 glow/skin/subsurface、7 specular…）。路徑是烤進 nif 的 **Data 相對**字串 — 路徑錯誤 → 物件隱形，無報錯（memory `vanilla-nif-paths-must-be-verified`）。
- **碰撞 — `bhk*` / Havok** 位於 nif 內：`bhkCollisionObject`→`bhkRigidBody`→`bhkShape`（`bhkConvexVerticesShape`、`bhkBoxShape`，或凹面用 `bhkMoppBvTreeShape`）。簡單的 convex/box 碰撞**無須 Havok SDK** 即可製作；凹面的 MOPP tree 才是難點。
- **Static vs skinned — 關鍵分岔：**
  - **Static prop：** 幾何 + shader + 簡單碰撞，無 skeleton/skin → 最簡單、最接近可完全自動化。
  - **Skinned/rigged：** 需要 Skyrim **skeleton**（`NPC Spine [Spn1]` 命名）、**每頂點 ≤4 weights**、以及一個帶 body-part partition 的 **`BSDismemberSkinInstance`**。半自動化（§4）。

---

## 2. 網格 → NIF 轉換工具（2026）及其 Linux 故事

| 工具 | SSE nif 匯入/匯出？ | Linux | 可腳本化 | 備註 |
|---|---|---|---|---|
| **PyNifly**（[GH](https://github.com/BadDogSkyrim/PyNifly)） | 是 — 最佳（shaders、collision、`_0`/`_1` weights、BSDismember） | **否 — 僅 Windows**（原生 `NiflyDLL`） | Blender addon → 透過 `blender --background --python` 可成 CLI（*前提是它能跑*） | Blender 4.4+。功能最強，但 Linux 阻礙是真實存在的；Wine-Blender 路徑很脆弱。 |
| **Blender NifTools addon**（`io_scene_niftools`，[GH](https://github.com/niftools/blender_niftools_addon)） | **非 skinned 的 SSE 匯出可以**；skinned 較弱 | **是 — 純 Python，原生 Manjaro Blender** | 是（`bpy`、headless） | **你 Linux 原生的 static 匯出器。** Blender 2.83+，2025 年末仍在維護。 |
| **ck-cmd**（[GH aerisarn](https://github.com/aerisarn/ck-cmd)） | **是 — 一行指令 `fbx → nif`**，materials→`BSLightingShaderProperty`、vertex colors、「95% 遊戲就緒」 | Windows CLI；**Wine** 候選（Linux/Mono 未有文件） | **完整 CLI、批次** | 強力的 shell-out 候選（xLODGen/Papyrus 模式）。也處理 `.hkx` 那一側。 |
| **NifSkope** | 檢視/編輯、修路徑/旗標、手動碰撞 | 在 Linux 上可編譯（Qt）；也可 Wine | GUI（手動） | 檢查/修復的工作台。 |
| **Outfit Studio / BodySlide**（[GH ousnius](https://github.com/ousnius/BodySlide-and-Outfit-Studio)） | **是** — nif、OBJ/FBX↔nif、**rig 到 Skyrim skeleton**、自動 partition | Linux 改善中；常用 **Wine** | GUI；BodySlide 部分批次 | armor/outfit 重新貼合工具（§4）。 |
| **nifly**（C++ lib） | 是（函式庫） | 跨平台原始碼 | 可嵌入 | 只有當你打算 in-process 自行製作時 — 不建議（§14「不要自行製作」）。 |

**結論：** 在原生 Manjaro 上，**NifTools addon（static 匯出）+ ck-cmd-under-Wine（fbx→nif + materials）+ Outfit Studio-under-Wine（rig/refit）**。PyNifly 是黃金標準，但代價是一台 Windows VM 或一個放棄原生 Linux 的 Wine-Blender。**IDEAS.md §14 應修正：** 在 Linux 上可自動化的搭配是 **NifTools-addon / ck-cmd**，而非 PyNifly。

---

## 3. Static-prop 管線（甜蜜點）

`FBX/OBJ/glTF static 模型 → 可運作的 Skyrim static .nif`：
1. **匯入 Blender**（內建匯入器，原生 Linux）。*自動。*
2. **修正縮放與朝向。** Skyrim ≈ Z-up、-Y forward、~64 units/m（見 [04 §4](04-map-scene-porting.md)）；來源各異（Unity Y-up/m、UE Z-up/cm、glTF Y-up/m）。Apply + bake。*每個來源測量過一次後即可自動。*
3. **設定 `BSLightingShaderProperty` 材質**（將來源材質 → Skyrim shader + texture-slot set，§5）。*自動、規則導向。*
4. **產生碰撞** — 簡單的 **convex-hull/box** `bhk` shape 就足夠且**可程式化產生**（網格的 hull）。ck-cmd/PyNifly 會發出碰撞；或在 Blender 腳本中產生 hull 頂點。*convex/box 自動；只有凹面 MOPP 需手動。*
5. **匯出 nif** — NifTools addon → `NiTriShape`（SSE 有效），或 ck-cmd `fbx→nif`（Wine）。*自動/headless。*
6. **（選用）SSE-optimize**（Wine）→ `BSTriShape`。**可跳過**（LE 形式可用）。
7. **驗證** — 在 NifSkope（路徑/旗標/碰撞）或遊戲內。*手動抽查；路徑正確性可腳本化。*

**可完全自動化：** 匯入、變換、材質、convex/box 碰撞、匯出。**手動：** 凹面碰撞、美術 QA、邊緣情況的朝向。MVP 目標（§9）。

---

## 4. Skinned/角色網格管線（更難 — 「半自動化」）

牆是 **skeleton + skin**，不是網格。
- **Rig 到 Skyrim skeleton** — 精確的骨骼名稱（`NPC Spine [Spn1]`、…）、**每頂點 ≤4 weights**、包進帶有正確 body-part partition 旗標的 **`BSDismemberSkinInstance`**。Outfit Studio 在 FBX/OBJ 匯入時設定預設 partition（80-bone/partition 的 SSE 上限）。
- **從來源 skeleton 重定向（Retarget）** — Genshin（Unity humanoid）、UE skeletal、FromSoft rigs 全都不同。**每個來源 skeleton 建一次 source→Skyrim 骨骼名稱對應表**（Blender Rigify/retarget addon 或手動），然後重用 — 即 IDEAS §13/§14 的「對應只寫一次」哲學。每個 rig 的對應表是人工活；套用它則可批次化 → *半*自動化。
- **權重轉移（Weight transfer）** — 從參考身體（CBBE/UNP/vanilla）用 **Outfit Studio 的「Copy Bone Weights」**；標準的 armor-refit 工作流程，大多是點選操作。可在 Wine 下執行。
- **交接給動畫：** 一個已 rig 的角色仍需要 `.hkx` 才能動 → 見 [05-animation-pipeline.md](05-animation-pipeline.md)。到「網格已 skin 到 Skyrim skeleton 且 partition 有效」就停在這裡。

---

## 5. 貼圖 / 材質轉換

來源 PBR → Skyrim `.dds` + 正確的 `BSShaderTextureSet` 插槽命名。

**目標：** `.dds`，帶 **BCn block compression + mipmaps**（diffuse BC1/BC3、normals BC7/BC5）。Skyrim 要 **DirectX 慣例的 normals** — 若來源是 OpenGL 則反轉 green/Y channel。

**工具（Linux 優先）：**
- **ImageMagick / GIMP+DDS / Krita** — 原生、基本 DDS（albedo 好，BC7/mipmap 控制較弱）。
- **Compressonator**（AMD）— Linux builds + CLI（`compressonatorcli`），完整 BCn + mipmaps。**強力的原生-Linux 批次候選。**
- **texconv**（MS DirectXTex）— 事實上的 Skyrim DDS CLI（resize→format→mipmaps→BCn 一次呼叫完成，BC6H/BC7 有 GPU 加速）。**Windows → Wine**（shell-out 模式）。Compressonator 是 Linux 原生的後備。

**glTF-PBR ↔ Skyrim shader 對應（「寫一次、批次套用」）：**
- glTF metal/rough pack **roughness=G、metalness=B**；傳統 Skyrim shader 是 **specular/gloss**，而 **Community Shaders「True PBR」**使用 **RMAOS** pack（Roughness=R、Metallic=G、AO=B、Specular=A）。**你的 baseline 已經有 Community Shaders（IDEAS §11-C）**，所以**鎖定 True PBR 讓 glTF→Skyrim 對應變成乾淨的 channel-repack，而非有損的 spec/gloss 轉換。**（TruePBR Manager 自動化了 channel-pack/compress/slot/JSON — 正是 ModForge 的轉換器應輸出的範本。）
- 每個來源慣例都可決定性處理：選定目標（legacy vs True PBR）、repack channels、BC-compress、mipmap，然後**把結果的 Data 相對路徑寫進 nif 的 `BSShaderTextureSet`。** 製作一次，對整組貼圖批次處理。

---

## 6. 從你擁有的遊戲中擷取

### Genshin Impact（Unity）
- AssetRipper/AssetStudio 有 **Linux/Mac 版本**，**但 Genshin 會加密它的 bundle** → 無法直接讀取（需要一個解密步驟）。社群標準路線：**GIMI / 3DMigoto frame-dump**（[GI-Model-Importer](https://github.com/SilentNightSound/GI-Model-Importer)）— 透過 3DMigoto 跑，**F8 frame-dump**（vertex buffers + `.dds`），然後一個 Python 腳本 + `blender_3dmigoto_gimi.py` 重建網格。
- **取得：** 網格、`.dds`、bone/weight 資料；Genshin skeleton → retarget（§4）。
- **Linux：** AssetRipper 原生但被加密擋住；**3DMigoto/GIMI 是 DX11/Windows** → 在 **Proton/Wine** 下 dump（繁瑣）。Blender 重建是**原生 Linux**。

### Wuthering Waves（Unreal Engine）
- **FModel**（[fmodel.app](https://fmodel.app/)）— UE pak/utoc/ucas 瀏覽器 + 匯出器，**.NET 8 → 跨平台**，由 CUE4Parse 驅動。Headless：**UnrealExporter**（[luk-gg](https://github.com/luk-gg/UnrealExporter)），透過 `dotnet` 的 CUE4Parse CLI。**UModel** 有 Linux CLI build 但**缺少 UE5.x 超過約 5.4 的支援** → 對於當前的 UE5 遊戲偏好 FModel/CUE4Parse。
- **取得：** UE skeletal/static 網格 → FBX/glTF（或 `.psk`/`.pskx`）+ 貼圖；需要遊戲的 `.usmap` mappings + AES key。
- **Linux：** **好** — .NET 跨平台；原生餵給 Blender。

### Dark Souls（FromSoftware）
- **格式：** **FLVER**，由 **SoulsFormats**（C# lib）支援。**對你最佳：[soulstruct-blender](https://github.com/Grimrukh/soulstruct-blender)** — 一個 Blender addon（Python `soulstruct`），直接匯入 FLVER（角色、物件、裝備、**map pieces** = statics），帶 armature/weights/dummies。Blender 4.1+。另有：FLVER_Editor、FBX2FLVER、FbxImporter、Smithbox。
- **取得：** 網格 + armature + weights + 貼圖（`.tpf`/`.dds`）。Map pieces = static（甜蜜點）；角色帶 FromSoft skeleton → retarget。
- **Linux：** **絕佳** — soulstruct-blender 是原生 Linux Blender 中的純 Python。**三者中最乾淨。**

**三者都匯聚於 Blender**，§3/§4 的 nif 管線從這裡開始。

---

## 7. 端到端工作流程

### (a) Static prop
1. **擷取：** soulstruct-blender（DS）/ FModel+UnrealExporter（WuWa）/ 3DMigoto-dump 或 AssetRipper（Genshin）→ 網格 + `.dds`。*[DS/UE 自動；Genshin 半自動]*
2. **匯入 Blender**（原生）。*[自動]*
3. **依來源規則修正 scale/orientation。** *[校準後自動]*
4. **將材質 → BSLighting/True-PBR** + texture slots。*[自動]*
5. **產生 convex/box 碰撞。** *[自動]*
6. **匯出 nif** — NifTools（原生）或 ck-cmd（Wine）。*[自動]*
7. **貼圖 → `.dds`**（Compressonator 原生 / texconv Wine），把路徑寫進 nif。*[自動]*
8. **放進 Meshes/Textures 樹**，餵 ModForge `model` spec + `package`。*[自動 — 既有]*

**牆：** 純 static 基本上沒有 — 凹面碰撞調校是唯一可能的手動步驟。

### (b) 角色 / skinned 網格
1. 擷取網格 + **來源 skeleton + weights**。*[自動/半自動]*
2. 匯入 Blender。*[自動]*
3. **Retarget source→Skyrim skeleton**（每個 rig 的對應表）。*[半自動]* ← **第一道牆**
4. 限制每頂點 ≤4 weights、建立 `BSDismemberSkinInstance` partition（Outfit Studio copy-bone-weights）。*[半自動]*
5. 匯出 skinned nif（PyNifly Windows / Outfit Studio Wine）。*[偏手動 — Linux skin 匯出是弱點]*
6. 貼圖 → dds。*[自動]*
7. **動畫/行為 `.hkx`** → 交接給 [05](05-animation-pipeline.md)。← **真正的牆（Havok）**
8. ModForge `model`/NPC spec + `package`。*[自動 — 既有]*

---

## 8. ModForge 整合

一個**與 record-layer Mutagen 軸並行的 asset-layer 管線**（IDEAS §14 的框架）。栓接到 `model` + `package` + shell-out（Papyrus-Wine、xLODGen），不碰 Mutagen core。

**提議的 CLI 步驟 `importmesh`（或 `convertasset`）：** 接受一個小 spec（來源檔案、來源類型、目標 nif 路徑、texture mapping、collision mode）並：
1. Shell out 到 **`blender --background --python convert.py -- <args>`** — repo 隨附的 headless 腳本，使用 **NifTools addon**（原生 Linux）匯入、套用每個來源的變換 + material mapping、產生碰撞、匯出 nif。（ck-cmd-under-Wine 作為替代後端，像 Papyrus-compiler 後端那樣選用。）
2. Shell out 到 **Compressonator（原生）或 texconv（Wine）**做 `.dds` + mipmaps，套用 glTF→Skyrim/True-PBR channel mapping。
3. 將正確的 **Data 相對路徑寫進 nif 的 `BSShaderTextureSet`**（Blender 腳本或 NifSkope/`nifly` 後續步驟）。
4. **把 nif + dds 放進 `package` 已經會打包的 `Meshes/`…`Textures/` 樹** — 於是既有的 `model` 欄位 + copy-trees 不變地接手。

**Spec 慣例：** 用一個選用的同層區塊擴充 `model`，例如 `modelSource: { file, sourceType: ds|ue|unity|gltf|fbx|obj, collision: convex|box|none, materialProfile: legacy|truepbr }`。Build 解析 `modelSource` → 跑 `importmesh` → 產出 `model` 所引用的 `.nif`。選用欄位 = 無 breaking change（依 CLAUDE.md 的 spec-evolution 規則）。

**維持「不要自行製作」：** ModForge 編排 Blender/ck-cmd/texconv；它**不**內嵌 nif writer（nifly 只是後備，只有當你某天決定 in-process 製作時 — 不建議）。

**後端選擇**對應原生-vs-Wine 的 Papyrus 拆分：`MODFORGE_BLENDER`、`MODFORGE_CKCMD`（Wine prefix）、`MODFORGE_TEXCONV`/`MODFORGE_COMPRESSONATOR`，並優雅地「工具缺失 → 警告、跳過」。

---

## 9. MVP + 踩坑

**最小可行切片：** **一個 static prop、一個遊戲 → 一個遊戲內的 Skyrim static。** 具體來說是一個 **Dark Souls map-piece**（soulstruct-blender，摩擦最低，落在原生 Linux Blender）→ NifTools 匯出成 `NiTriShape` nif → Compressonator dds → 透過 ModForge `staticSpec` + `model` 路徑手動放置 → `package` → 遊戲內載入，確認它有碰撞地渲染出來。無 skeleton/Havok/retargeting — 在進到 §4 之前先端到端證明 asset-layer 的 shell-out。

**踩坑（多數已在 memory 裡）：**
- **錯誤的 nif/texture 路徑 = 隱形、無報錯**（memory `vanilla-nif-paths-must-be-verified`；搭配 `packaging-zip-stale-file-trap`）。
- **Scale/orientation/units** — Z-up、-Y forward、~0.0142×；Unity Y-up、UE cm/Z-up、glTF Y-up/m。每個來源校準一個常數。
- **碰撞缺失/錯誤 → 穿透落下。** Static 需要至少一個 convex/box `bhk`；凹面 MOPP 是困難的升級。
- **SSE vs LE nif version** — **LE 形式的 `NiTriShape` 在 SSE 可用**（使 NifTools-addon 路徑可行）；SSE-optimization 是選用的打磨。對於*帶 skin 的 armor*，partition/skin-partition 的正確性更重要。
- **Normal-map 慣例** — 對 OpenGL 來源的 normals 反轉 green channel。
- **PyNifly 僅 Windows** — 不要把 Linux 管線架構在它之上。
- **Genshin 加密**擋住 AssetRipper → Proton 側的 3DMigoto frame-dump（三者中最不 Linux-乾淨的）。
- **不可散布** — 轉換後的商業資源僅供私人安裝。

---

### 來源
PyNifly（GH BadDogSkyrim）· Blender NifTools addon（GH niftools）· ck-cmd（GH aerisarn）· hkxcmd（GH figment）· Beyond Skyrim NIF Data Format · SSE NIF Optimizer（Nexus #4089）· BodySlide & Outfit Studio（GH ousnius）· AssetRipper（GH）+ GI-Model-Importer/GIMI · FModel + CUE4Parse + UnrealExporter（luk-gg）+ UModel · soulstruct-blender（GH Grimrukh）+ FLVER_Editor + FBX2FLVER + Smithbox · DirectXTex texconv · Community Shaders True PBR。

**給 IDEAS.md §14 的兩個標記：**（1）PyNifly 僅 Windows — 在 Linux 上可自動化的匯出器是 **NifTools addon**（+ ck-cmd under Wine）。（2）鎖定 **True PBR**（已在 CS baseline 中）使 glTF→Skyrim 貼圖對應變成乾淨的 channel-repack — 這正是讓 §5 的「寫一次、批次套用」真正乾淨的槓桿。
