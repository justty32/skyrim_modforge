# 地圖 / 場景移植管線（外部關卡 → Skyrim SE Worldspace/Cell）

← 索引：[README.md](README.md) · 相關：[IDEAS.md §5](../IDEAS.md)（資源移植）、[03-3d-model-import.md](03-3d-model-import.md)（網格交接）、`CODE_MAP.world.md`

**研究日期：** 2026-06-08。僅限個人/單機 — 移植的商業資源在**本機**轉換/使用，絕不重新散布。*（這是使用者最興奮的主題。）*

**信心度：** 部分管線細節是從各組成工具的運作方式推論而來，而非來自單一的端到端「將遊戲 X 移植進 Skyrim」的完整記述，因為那個組合管線是客製化的。已就地標記。

---

## 1. 在 Skyrim 資料層面「地圖移植」的意涵

Skyrim 有兩種表示「地點」的方式，而其區別就是一切：

**(a) 模組化 static 網格作為 references 放置（vanilla 的做法）。** 每個 vanilla 城市/地城都是一個 *kit*：一個由獨特 `.nif` 網格組成的小型函式庫，每個被放置*許多次*作為 **placed references**（`REFR`/`ACHR` 指向 base `STAT`/`ACTI`）。每個 ref 恰好攜帶 **base object（哪個網格）、position（X/Y/Z）、rotation（Euler X/Y/Z）、以及 uniform scale。** Whiterun 是數百個 `STAT` ref，引用幾十個 base nif。標準、引擎認可的。

**(b) 一整塊巨大網格。** 可行但是陷阱：無逐件碰撞、撞上 BSTriShape 頂點上限 + filesize/streaming、無 instancing、無 LOD 粒度、任何編輯 = 全部重新匯出。

**為何模組化完美契合 ModForge。** ModForge 已經發出帶 position/rotation/scale 的 `STAT`/`ACTI` placed ref。它從外部關卡所需的資料*正好*是：
```
{ mesh (base-object id), position (x,y,z), rotation (euler x,y,z), scale }
```
那份清單就是一組 placed references。**所以整個移植問題簡化為：產出那份清單（「placement list」/「layout」）+ 產出它所引用的獨特網格。** 而每個真實的遊戲引擎*本來就把它的關卡儲存為完全相同的結構*（一份引用一小組獨特資源的 instance 清單）— 這正是此事可行的核心理由。

---

## 2. Interior（cell）vs exterior（worldspace）

**Interior 顯著更容易 — 正確的第一個目標。** 一個有界的 interior `CELL` 不需要 terrain、LOD、regions、heightmap、climate/weather。只需要：
- **Placed references** — kit 件（ModForge ✅）
- **一個 navmesh** — 界限在房間內（ModForge ✅ 程式化 navmesh，遊戲內確認）
- **Lighting** — `LIGT` refs / `CELL` lighting template（ModForge `LightSpec`/`LIGT` ✅）
- （選用）image space / fog / acoustic space — MVP 可跳過

**Exterior 增加四項困難需求，其中三項撞上 ModForge 已知的缺口：**

| 需求 | ModForge 現況 |
|------|----------------|
| 聚落規模的 placed refs（數千） | ⚠️ placement 可運作；**量產是已知缺口** |
| 地形 heightmap（`LAND`、真實海拔） | ❌ **僅平坦地形** |
| LOD（terrain + object） | ❌ **未建置 — 計劃 shell out 到 xLODGen** |
| 不平坦地形上的較大 navmesh | ⚠️ 可運作；大規模跨地形未經驗證 |
| Regions / climate / water | 部分 |

**建議：interior 優先。** interior 移植只動用已經可運作的能力。Exterior 是 phase-2，以解決 heightmap + LOD 為前提。

---

## 3. 擷取場景的 LAYOUT（不只是網格）

每個引擎成敗關鍵的問題：**我能不能拿到一份結構化的 `(asset reference + world transform)` 清單？** 三者都把關卡儲存為 instance 清單；用以還原它們的工具是存在的。

### (c) FromSoftware（Dark Souls / Elden Ring）— MSB — *最容易、先做*
**MSB（Map Studio Binary）**檔案*字面上就是 placement list。* 已驗證結構（[SoulsFormats](https://github.com/JKAnderson/SoulsFormats)）：
- **Models** 區段：宣告所載入的每個 asset id（mesh/`FLVER`）。
- **Parts** 區段：**每個可見實體 — map pieces、collisions、objects、enemies — 各帶一個 transform**（position、rotation、scale）。*這份 Parts 清單就是 {asset, transform} 表格。*
- **Regions** 區段：隱形 trigger（幾何上可忽略）。

**擷取：** SoulsFormats 是一個 **.NET（C#）函式庫** — 這非常重要，因為 **ModForge 本身就是 C#/Mutagen。** 直接引用它，in-process 讀 MSB：`MSB1.Read(path)`（DS1）/ `MSB3`（DS3）/ `MSBE`（Elden Ring）→ 迭代 `msb.Parts.MapPieces`，每個暴露 `.ModelName`、`.Position`、`.Rotation`、`.Scale`。無檔案往返、無 Wine — 純 managed code on Linux。Smithbox/DSMapStudio（GUI，同一函式庫）渲染這完全相同的資料供視覺交叉檢查。（注意：那些 GUI **不**匯出網格 — 網格那側是分開的，§5。）`ModelName → mesh file` 的對應是 join。

### (a) Unreal Engine（如 Wuthering Waves）— `.umap` — 可行，兩條路線
UE 將關卡儲存為一個 `.umap`，其 actor 攜帶 `Transform`（Location/Rotation/Scale3D）：
1. **[FModel](https://fmodel.app/)** 開啟 pak/ucas → **右鍵 `.umap` → export JSON / 「Export Raw Data」** → actors + transforms。.NET；Linux 上走 Wine。
2. **[umodel_tools](https://github.com/skarndev/umodel_tools)**（Blender addon）— *已驗證工作流程*：FModel `.umap` JSON → Blender `Import Unreal Map` → 重建**帶 transform 的 static-mesh 與 light placements。** **已記載的限制：只還原 *static* placements — Blueprint/C++ 生成的 actor 會遺失。** 對關卡幾何而言夠用。
3. **[BlenderUmap](https://github.com/Amrsatrio/BlenderUmap)** — 直接從 umap 讀取 transform，以 placements 重建。

所以 UE **能**給出 `{asset, world-transform}` — 作為 FModel JSON（可機器解析，ModForge 形狀的路徑）或在 Blender 中重建。*（JSON schema 因 UE 版本而異 — 每個遊戲都要檢查。）*

### (b) Unity（如 Genshin）— bundles — 透過 AssetRipper 可行
**[AssetRipper](https://github.com/AssetRipper/AssetRipper)** 擷取 Unity serialized files/bundles，而且 — 關鍵地 — **以帶 Transform 元件（pos/rot/scale）的方式重建 scene/GameObject 階層**，不只是散落的網格；重建 `.unity` 場景並透過偵測重複的子階層重新推導 prefab（即 **還原 instancing**，§5）。AssetStudio（2025 CLI fork）從 Scene Hierarchy 匯出帶 placement 的模型。.NET；Linux 上走 Wine（驗證當前版本的 runtime）。*（Genshin bundle 加密可能擋住直接讀取 — 見 [03 §6](03-3d-model-import.md)。）*

**ModForge 的排名：** **FromSoft MSB ≫ Unity AssetRipper ≈ UE FModel-umap。** MSB 勝出：(1) layout 就是原生檔案，無重建猜測；(2) 透過 SoulsFormats **從 C# in-process 讀取** — 與 ModForge 的 stack 零阻抗。

---

## 4. 座標系統與縮放轉換

決定性的、可腳本化的、ModForge 形狀的核心。每個引擎在 handedness/up-axis/unit 上各異；轉換到 Skyrim。

**目標 — Skyrim：** Z-up、**128 game units = 6 feet → ~64 units ≈ 1 m**（≈21.33 u/ft）；一個 exterior cell = **4096×4096 units = 192×192 ft ≈ 58.5 m**（[CK Wiki: Unit](https://ck.uesp.net/wiki/Unit)）。Rotation 是 Euler XYZ。Gamebryo+Havok 是右手 Z-up。*（128u=6ft 高信心；確切 handedness 歷來咬過移植者 — 經驗驗證。）*

**來源（已驗證）：**
- **Unreal：** 左手、**Z-up、公分。** X=fwd、Y=right、Z=up。
- **Unity：** 左手、**Y-up、公尺。** X=right、Y=up、Z=fwd。
- **FromSoft（Havok）：** 實務上 Y-up、近公制 — **每個遊戲驗證**。

**轉換配方（每個放置物件）：**
1. **單位縮放。** UE：`Skyrim ≈ cm × 0.64`。Unity/FromSoft：`Skyrim ≈ m × 64`。然後一個**全域美術縮放微調值**（調到一個門廊 ≈ 128 units 高）。
2. **軸交換到 Z-up。** Unity（Y-up→Z-up）：`(x,y,z) → (x,z,y)` 然後 handedness。UE 已是 Z-up；只有 handedness 不同。
3. **Handedness 翻轉（LH→RH）。** UE/Unity 是左手、Skyrim 右手 → 一個軸取負（通常對 Y 或 X 取負）**外加對相應的 rotation 分量取負。** 錯了 = 鏡像的關卡（對一個軸取負來修）。
4. **Rotation：quat/matrix → Skyrim Euler。** 組出完整的 source→Skyrim 基底變換 `B`，套用 `R_skyrim = B · R_source · B⁻¹`，按 Skyrim 的順序分解為 Euler XYZ。**不要逐軸做 Euler 轉換**（微妙錯誤旋轉的第一大來源）。
5. **Scale：Skyrim refs 攜帶單一 uniform scale float。** 非均勻的來源 scale 無法表示 → 烤進一個獨特的 mesh 變體（殺死 instancing）或近似為均勻。轉換時**標記任何非均勻縮放的 instance**。

屬於一個小函式：`(sourceEngine, position, quat, scale3) → (skyrimPos, skyrimEulerXYZ, skyrimUniformScale)`。在大量轉換之前，**先在一個已知參考上一次性地把它證明做對**（放一個 cube、比對）。透過 Blender（umodel_tools/AssetRipper-FBX）繞道可降低數學風險，因為 Blender 的匯入器已經編碼了許多慣例。*（要對 Skyrim 取負的確切 sign/axis 以經驗決定。）*

---

## 5. 網格那側（交接 — 僅備註）

每個獨特資源都必須變成一個 Skyrim `.nif`（+`.dds`、+碰撞）。**見 [03-3d-model-import.md](03-3d-model-import.md)。** 管線：來源網格（FLVER / psk/pskx / FBX / glTF）→ Blender → Skyrim nif 匯出器 + dds。ModForge 的 `model` + `package` 打包結果。

**Instancing 的勝利才是重點。** 一個關卡有*數千個 instance*但只有*數十個獨特網格*（一個模組化 kit — vanilla Skyrim 和來源引擎都這樣建構關卡）。**把每個獨特網格轉換成 nif+碰撞恰好一次**，註冊為一個 `STAT`，發出 N 個指向它的 ref。AssetRipper 的 prefab 重新發現與 MSB 的 Models 區段免費把獨特資源集交給你。昂貴的手動步驟（mesh→nif+碰撞）隨*獨特網格*（數十）擴展，而非*instances*（數千）。

---

## 6. 地形 / heightmap（僅 exterior）

**Skyrim 地形如何運作：** 每個 exterior `CELL` 有一個 `LAND` record — 一個 **33×33 頂點高度網格**（gradient 編碼）、逐頂點 normals/colors、最多約 6 個 quadrant 貼圖（`LTEX`）透過 alpha 混合。Worldspace 縫合 cell；高度必須在共享邊界處吻合，否則會有接縫。

**ModForge 缺口：** 僅平坦地形。

**Heightmap 匯入工具：** **TESAnnwyn**（raw heightmap → `LAND`/worldspace）、**CK Heightmap Editor**（1024×1024 16-bit RAW → `Data\HeightField\` → bake LAND；Wine）、xEdit/xLODGen 的 LAND 編輯、Beyond Skyrim 的 World Heightmap 工作流程。

**建議 — 務實路徑：不要移植 heightmap；*假造地面*。** 把關卡的 ground/landscape 作為 static `.nif` 網格放置（它們反正就在 placement list 裡）+ 底下一個**平坦、低的 `LAND`** 作為碰撞/water-floor 後備。繞過 heightmap 匯入缺口與 cell 邊界縫合，並符合許多自訂 worldspace（以及解碼過的 Vigilant.esm 參考）實際的做法 — 你走的「地形」是自訂 static 網格，不是 LAND。只有當你需要 vanilla 地形-LOD 混合時才投資於真實的 `LAND` 匯入。*（高信心這是正確選擇；讓工作留在 ModForge 的 placed-ref 強項內。）*

---

## 7. 移植場景的 navmesh、碰撞、LOD

**Navmesh — 部分解決，帶一個真實問題。** ModForge 的程式化 `NAVM` 在*平坦地面*上產生；產生一個貼合*已放置 static 幾何*（真實地板高度）的 navmesh 需要取樣可行走表面（Recast 風格的 voxelization）— 正是 **CK 的 Recast 自動產生**所做的（放置 statics → Recast-生成 → **Finalize**）。對移植 interior 的實際拆分：
- MVP：ModForge 在地板高度的平坦 navmesh（若大致為單層/平坦地板則可運作）。
- 穩健：在 **CK（Wine）**中開啟建好的 ESP，NavMesh 模式，對已放置幾何 Recast-生成、Finalize — 一個**手動**步驟，對多層/樓梯的誠實答案。在 ModForge 內做一個 Recast 等價物是一項可觀的開放任務。

**碰撞 — 開放、困難、交接給網格管線。** 每個 `.nif` 需要 **Havok 碰撞**（`bhkCollisionObject`/`bhkCompressedMeshShape`）否則 **玩家/NPC 穿透落下。** 來源網格不帶它；產生它是逐獨特網格（instancing 勝利適用）。**座標數學之後最大的實務牆。**

**LOD — shell-out、可 Wine。** [xLODGen](https://github.com/sheson/xLODGen)（xEdit 的一個 build）透過 CLI 為自訂 worldspace 做 terrain/object/tree LOD + occlusion。xEdit 在 Wine/Proton 下執行 → xLODGen 應該也行 *（驗證）*。**Interior 不需要 LOD** — MVP 無關緊要。

**今日已解決 vs 開放：** navmesh-flat ✅ / navmesh-over-static ❌。Collision ❌（網格管線）。LOD — interior 不需要、exterior 走 Wine shell-out ⚠️。

---

## 8. 端到端工作流程（建議：FromSoft MSB interior/arena → Skyrim interior cell）

1. **擷取 layout。** 用 **SoulsFormats（C#）** 讀 MSB → `Parts.MapPieces` → `{ModelName, Position, Rotation, Scale}`。**自動（in-process、無 Wine）。**
2. **擷取獨特網格。** 對每個不同的 `ModelName`，拉 FLVER → Blender → Skyrim nif + dds + **碰撞**（交接 [03](03-3d-model-import.md)）。**半自動 — 這就是牆**（每個獨特網格的碰撞）。
3. **轉換 transform**（§4：unit×~64 + 微調、up-axis Z、handedness 翻轉、quat→Euler、uniform-scale 檢查）。**自動。**
4. **對應 ModelName → STAT base。** **自動。**
5. **發出一份 ModForge placement spec** — 一個帶 N refs + `LIGT` lighting 的 `CELL`（interior）。**自動（新的轉換器，§9）。**
6. **Build** → ESP。**自動（既有）。**
7. **Navmesh** — flat（自動）或 CK Recast+Finalize（手動、Wine）。**自動/手動。**
8. **Package** → 帶 nifs/dds 的扁平 MO2 zip。**自動（既有）。**

**牆：** 步驟 2（mesh→nif **帶碰撞**）與步驟 7-穩健（非平坦上的 navmesh）。步驟 1、3–6、8 是乾淨可自動化的 — 正是 ModForge 已經安身之處。

---

## 9. ModForge 整合

**自然的新部件是一個 `layout → placement-spec 轉換器`** — 把擷取出的場景 dump 轉成 ModForge 既有的 placed-ref JSON，帶 §4 轉換過的 transform。

- **加一個 `importscene` CLI 步驟**（與 `build`/`package` 並列）。輸入：場景來源 + engine 旗標，例如 `importscene --engine ds1 m10_00_00_00.msb` / `--engine ue level.umap.json` / `--engine unity scene-dump`。輸出：一份 ModForge spec JSON（placement list 作為 `CELL` + refs），**不是**建好的 ESP — 讓你在 `build` 前檢視/微調。
- **對 MSB，in-process 做：** 引用 **SoulsFormats**（NuGet/submodule，.NET — 同一 stack）。無外部轉換器、無 Wine、無往返。摩擦最低 → 再次論證 **MSB 優先**。
- **對 UE/Unity，外部前端 → 同一中介 JSON：** FModel→umap-JSON / AssetRipper scene-dump → 一個小 parser（同一 `importscene` 步驟搭一個 engine adapter）。維持**一個中介 placement schema**（`[{model, pos, rot, scale}]`）→ 一個轉換器後端、N 個 engine 前端。
- **下游一切都倚靠既有的 builder：** worldspace/`CELL`、placement、`LIGT`、程式化 navmesh 都已存在 — `importscene` 只是餵給它們。

**要為 *exterior* 移植關上的缺口**（interior 一個都不需要）：(1) heightmap `LAND` — 或採用 §6 的假造地面做法；(2) 聚落規模的 placed-ref 量產（轉換器發出數千個 ref — 發出路徑必須維持高效能）；(3) LOD → xLODGen（Wine）；(4) navmesh-over-static（今日僅 flat）。

---

## 10. MVP + 踩坑 + 建議

**建議的 MVP：** **一個 Dark Souls（DS1）MSB arena/room → 一個單一的 Skyrim interior `CELL`** — 少數獨特的 kit 網格作為 `STAT` refs、基本 lighting、平坦 navmesh。為何選 DS 而非 UE/Unity：MSB **從 C# in-process 讀**（零 Wine/往返、同一 stack）、native layout（無 Blueprint/prefab 猜測）、interior 只需要已經可運作的能力。挑一個**小、平坦地板、單層的房間**，讓平坦-navmesh 的 MVP 就足夠，而你永遠不碰 CK。

**分兩階段證明以隔離兩個風險：**
1. **僅 layout 的冒煙測試：** 把 MSB 轉成 N refs，但讓每個 ref 都指向一個 *vanilla* 網格（一個已知的 cube/wall）。以零網格管線風險端到端證明 **座標轉換**（§4）— 你立刻看到鏡像/縮放錯/旋轉錯。**先做這個。**
2. **然後**為少數獨特網格換上真正移植的 `.nif`（帶碰撞）。

**踩坑（按咬人機率排名）：**
- **座標轉換錯誤** — 鏡像（handedness）、縮放錯（忘了 ×64 / 微調）、垃圾旋轉（天真的逐軸 Euler）。用 stage-1 的 vanilla-mesh 測試釘死。
- **碰撞穿透落下** — 一個沒有 Havok 碰撞的 nif = 穿透落下、*無報錯*。數學之後最大的牆。
- **非均勻 scale** — refs 只攜帶一個 uniform float；標記並處理。
- **Instancing 紀律** — 每個獨特網格轉一次、放多個 ref；不要每個 instance 鑄一個獨特 base（殺死這套經濟學）。
- **非平坦幾何上的 navmesh** — flat 只對平坦地板有效；樓梯/多層需要 CK Recast（手動、Wine）。
- **你無法在遊戲內測試** — 先做結構性驗證：build ESP、用 xEdit（Wine）/ 你的 `*diag` 工具開啟、確認 `CELL`、N 個帶合理 pos/rot/scale 的 `REFR`、navmesh、燈光 — *在*手動 MO2/Proton 一趟*之前*。
- **不可散布** — 移植的商業網格/貼圖留在本機；可出貨的成品模式應是 *placement spec + 轉換器*，絕不是打包的資源。

**結論：** 命題良好，而困難的部分比看起來小，因為 *每個* 來源引擎都把關卡儲存為 ModForge 已經作為 placed ref 發出的那種 `{asset, transform}` instance 清單。從 **DS1 MSB → interior cell** 開始，在 SoulsFormats（in-process C#）上建 **`importscene` 轉換器**，**先用 vanilla 網格證明座標數學**，並把 **mesh→nif-with-collision** 當作已知的牆第二個攻克。把 heightmap/LOD/exterior 延後，直到 interior 迴圈穩固。

---

### 來源
SoulsFormats（GH JKAnderson）+ SoulsFormatsNEXT · DSMapStudio · Smithbox · MSB format wiki · FModel + umodel_tools + BlenderUmap + UE Viewer/UModel · AssetRipper + AssetStudio · UE/Unity coordinate docs · CK Wiki Unit / Heightmap Editing / Navmesh / Custom Worldspace with LOD · TESAnnwyn · Beyond Skyrim World Heightmap Creation · xLODGen · Skyrim-SE-on-Linux（xEdit under Wine）/ AFK Mods Linux guide。
