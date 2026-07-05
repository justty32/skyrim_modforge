# DsExtractor — DS(R) 地圖資產抽取器

← [darksouls-port](../README.md)

獨立 C# console 工具，把本機 Dark Souls Remastered 的地圖資料（MSB / FLVER / TPF）抽成通用格式（JSON / glTF / DDS），餵給 ModForge 管線的後段（glTF→NIF、placements→spec）。**不動 `src/ModForge.*`**（sub_projs 鐵律：工具不長特例）。

> ⚠️ IP：抽出的任何遊戲資產（FLVER/DDS/glTF/NIF…）**僅本機個人使用、絕不發佈**，一律 gitignore。repo 只 commit 本工具原始碼。

## 依賴（走通的路：NuGet）

| 套件 | 版本 | 用途 |
|------|------|------|
| [`JuicerMV.SoulsFormats`](https://www.nuget.org/packages/JuicerMV.SoulsFormats) | 0.1.4 | 讀 DCX/BND3/BXF3/FLVER2/TPF/MSB1（社群維護的 SoulsFormats fork，MIT）|
| [`SharpGLTF.Toolkit`](https://www.nuget.org/packages/SharpGLTF.Toolkit) | 1.0.6 | 寫 glTF 2.0 |

- **官方 JKAnderson/SoulsFormats 沒有上 NuGet**；`JuicerMV.SoulsFormats` 這個 fork 的 API 與上游一致（`FLVER2` / `MSB1` / `TPF` / `BXF3` / `DCX` 型別齊全），實測讀 DS**R** v1.04 的 `m18` 資料 OK，**不需 clone 原始碼**。
- 兩個套件都由 `DsExtractor.csproj` 的 `<PackageReference>` 宣告，`dotnet build` 會自動還原。
- **備援（若哪天 NuGet 套件失效）**：`git clone https://github.com/JKAnderson/SoulsFormats extractor/ThirdParty/SoulsFormats`（或社群 fork `SoulsFormatsNEXT`），把 `<PackageReference Include="JuicerMV.SoulsFormats">` 換成 `<ProjectReference Include="ThirdParty/SoulsFormats/SoulsFormats.csproj">`。`ThirdParty/` 已 gitignore。

## Build

```bash
cd sub_projs/darksouls-port/extractor
dotnet build -c Release
```

本機 SDK：net8.0（`dotnet --list-sdks` 有 8.0 與 10.0，專案鎖 net8.0）。

## 子命令

```bash
dotnet run -c Release --no-build -- <subcommand> ...
```

### 1. `msb-dump <msb> <out.json>`
列出 MSB 全部 parts（type / name / model / position / rotation / scale）→ JSON，並印各類型計數。
```bash
... msb-dump  <GAME>/map/MapStudio/m18_01_00_00.msb  ../extracted/msb_m18_01.json
```
type 涵蓋 MapPiece / Object / Enemy / Player / Collision / Navmesh / DummyObject / DummyEnemy / ConnectCollision。

### 2. `flver2gltf <flver.dcx> <outdir>`
DCX 解壓 → FLVER2 解析 → glTF 2.0（`.gltf` + `.bin`）。
```bash
... flver2gltf  <GAME>/map/m18_01_00_00/m0046B1A18.flver.dcx  ../extracted/m0046B1A18
```
- 每個 FLVER mesh → 一個 glTF mesh/primitive（多 submesh 保留）。
- 只取每個 mesh 的**全精度 faceset**（`FSFlags.None`），跳過 LOD / motion-blur 重複面。
- 材質**只記貼圖名稱**（glTF material name = FLVER 材質的 diffuse 貼圖檔名，如 `m19_B_wall_07.tga`），不嵌真貼圖。
- 另出 `<stem>.textures.json`：這塊 FLVER 引用的所有貼圖 stem（餵給 `tpf-extract --filter`）。

### 3. `tpf-extract <tpfbhd> <outdir> [--filter <substr>]`
BXF3 分卷（`.tpfbhd` + `.tpfbdt`）解開 → 每個 TPF 內的貼圖以完整 DDS（`Headerize()` 補回檔頭）原樣落地。`--filter` 只撈名稱含子串者。
```bash
... tpf-extract  <GAME>/map/m18/m18_0002.tpfbhd  ../extracted/m0046B1A18  --filter m19_B_wall_07
```
> `m18` 貼圖分 4 卷（`m18_0000`~`m18_0003`），一塊 map piece 的貼圖不一定在同一卷；不確定就對每卷跑一次同一 filter。

### 4. `hkx-extract <hkxbhd> <outdir> [--piece <substr>]`
BXF3 碰撞分卷（`.hkxbhd` + `.hkxbdt`）解開 → 每塊碰撞件的 DCX 解壓後 `.hkx` 落地（乾淨檔名，如 `h0501B1A18.hkx`）。
```bash
... hkx-extract  <GAME>/map/m18_01_00_00/h18_01_00_00.hkxbhd  ../extracted/collision/hkx
... hkx-extract  <GAME>/.../h18_01_00_00.hkxbhd  ../extracted/collision/hkx  --piece h0501B1A18
```
- **本命令只解容器**，不抽三角網格。DS(R) 碰撞 `.hkx` 是 **Havok 2015 `TAG0` tagfile**（`SDKV 20150100`），內含 `CustomParamStorageExtendedMeshShape`（FromSoft 的 `hkpStorageExtendedMeshShape` 子類，**未壓縮**三角儲存）。
- **C# 側解不動 tagfile**：NuGet 上沒有能讀 DSR 2015 tagfile 的套件——`JuicerMV.SoulsFormats` 無 HKX 型別、`HKLib` 只支援 2018（艾爾登）、`BotW-HKX2` 是 BotW 專用。所以**三角抽取 + 凸分解移到 Python 端**（`tools/collision_hulls.py`，用 `soulstruct-havok`）。本命令產出的 `.hkx` 就是那支工具的輸入。
- `h`（高精度）= 玩家實際碰撞；`l`（低精度）= 遠景/摔落判定。`h18_...` 內 47 塊、全部 `TAG0/20150100`。

## 碰撞管線（hkx → 三角網格 → 凸包 hulls，路線 A）

分兩段（C# 解容器、Python 抽幾何＋V-HACD）：

```
h18_.hkxbhd/bdt ──(C# hkx-extract)──> 每塊 .hkx
   .hkx ──(Python tools/collision_hulls.py, soulstruct-havok)──> <name>.collision.json（三角網格）
                                                              └─> <name>.hulls.json（凸包，餵 gltf2nif）
```

### `tools/collision_hulls.py` 用法
```bash
python tools/collision_hulls.py <piece.hkx> <outdir> [--method components|vhacd] \
    [--planar-thresh 1.5] [--resolution 100000] [--max-hulls 63] [--no-mesh-json]
```
Python 依賴（離線可重跑，`ThirdParty/` 已 gitignore）：
```bash
python3 -m venv venv && . venv/bin/activate
git clone https://github.com/Grimrukh/soulstruct
git clone https://github.com/Grimrukh/soulstruct-havok
pip install -e ./soulstruct && pip install --no-deps -e ./soulstruct-havok
pip install numpy scipy colorama networkx vhacdx trimesh
```
> 用 `pip install -e`（editable）：soulstruct 靠一堆 package-data JSON（emevd 等），非 editable 安裝會漏檔 → `FileNotFoundError`。PyPI 上 `soulstruct` 只到 2.3.2 < havok 要求的 2.4.0，故從 GitHub 源碼裝。Oodle DLL 警告可忽略（DSR 用 zlib DCX，非 Krak）。

### 兩種凸分解方法
- **`components`（預設，路線 A 針對 FromSoft 碰撞的修正）**：FromSoft 碰撞是**一堆小的近平面 patch**。先焊接重合頂點（DSR 碰撞近乎三角湯，多數頂點不共用）→ 拆連通元件 → 近平面/極小元件各出**一個凸包**（薄板＝精確覆蓋、不橋接凹陷），真正非平面的元件才丟 V-HACD 細分。hull 數低、覆蓋完整。
- **`vhacd`（比較用）**：整塊 V-HACD。**DS 碰撞是薄開殼**（近乎零封閉體積），V-HACD 會在 `maxConvexHulls` 上飽和（自然數量 100+），封頂就漏覆蓋（h0501 實測 vhacd 覆蓋僅 30%，components 99%）。只對真正實心塊有意義。

### hulls JSON 介面規格（與 gltf2nif 約定，勿改）
```json
{ "hulls": [ { "vertices": [[x, y, z], ...] }, ... ] }
```
- 座標＝**DS 原生公尺、Y-up**，**不縮放、不換軸**（那是 NIF 寫出器的事）。
- 每個 hull 是一組頂點（該凸包的極點）；下游取其凸包當 `bhkConvexVerticesShape`。
- 每 hull 頂點數 < 256（工具強制 `maxNumVerticesPerCH=64`）。

### 碰撞實測（m18_01_00_00，2026-07-05）
- **hkx 解析走通**：`soulstruct-havok`（Grimrukh）`MapCollisionModel.from_hkx()` 讀 DSR 2015 tagfile，**47/47 塊全解成功**，頂點/索引為 DS 公尺、Y-up。容器型別 = `CustomParamStorageExtendedMeshShape`（未壓縮三角儲存，非 `hkpBvCompressedMeshShape`——不必解 MOPP/壓縮網格）。
- **選塊**：MSB 全部碰撞件都 baked 在 `(0,200,0)` identity（與 map piece 同世界系，只差 +200 Y），故直接用 local bbox 比對。與 `m0046B1A18`（X[-3.6,14.3] Y[5.0,16.0] Z[-35.1,-11.5]）三軸最重疊者 = **`h0501B1A18`**（X[-3.6,10.1] Y[5.1,22.9] Z[-34.3,-3.2]）→ 主目標。
- **`h0501B1A18`（主目標，`components`）**：3059 頂點 / 2049 面 / 2 submesh → 54 連通元件 → **57 hulls**，覆蓋 99.1%（原網格頂點 5cm 內有 hull），57/57 凸，每 hull 4–34 頂點。< 64 目標。
- **`h0006B1A18`（乾淨地板範例）**：47 元件 → **47 hulls**，覆蓋 100%，全凸。
- **已知限制**：高度細分的大地板（`h0005` 365 元件、`h0008` 456、`h0013` 295…）→ hull 數超過 64（每個小共平面 patch 各一個凸包）。牆/道具類（少元件）都 ≤ 57。要讓大地板也 < 64 需加**共平面相鄰 patch 合併**（後續精修；目前那些塊記錄下來、調 `--planar-thresh` 有限）。

## 座標約定（重要）

glTF 輸出**維持 DS 原生座標系：Y-up、單位公尺、DS 原樣尺度**。
**不在這裡做** Z-up 轉換、也不做 Skyrim 的 ×70 縮放——那是後段 NIF 端 / placement→spec 時才處理的事。這樣 glTF 是乾淨的中間格式，換目標引擎不必重抽。

## 已知限制 / 現況

- **靜態幾何 + 貼圖名 + placement 傾印 + 碰撞容器解包**（`hkx-extract`）；碰撞三角抽取與凸分解在 Python 端（見上「碰撞管線」）。材質參數對映（BSLightingShaderProperty）、glTF→NIF 仍是後續階段，本 C# 工具不碰。
- 頂點屬性只輸出 POSITION / NORMAL / TEXCOORD_0（第一組 UV）。tangent/bitangent/vertex color 未輸出（NIF 端要 tangent 時再補）。
- **SharpGLTF 會焊接重複頂點**：glTF 的 POSITION 頂點數可能 < FLVER 原始頂點數（共用角點被合併），但**三角形數完全一致**（載入不變量）。驗證看 index/三角形數，不要看頂點數。
- FLVER 材質貼圖路徑寫的是 `.tga`（引擎內對映到 TPF 裡的 `.dds`）；material name 保留原始 `.tga` 名，實體檔是 `tpf-extract` 出的同 stem `.dds`。
- 只在 DSR v1.04 `m18`（不死院）實測過；其他地圖 / 其他 FromSoft 遊戲（用 MSB3/FLVER0 等）未驗。

## 實測結果（不死院 m18_01_00_00，2026-07-05）

- `msb-dump`：324 parts — Object 151 / Collision 48 / MapPiece 45 / Enemy 35 / Navmesh 20 / DummyObject 18 / DummyEnemy 3 / Player 2 / ConnectCollision 2。
- `flver2gltf` 選 `m0046B1A18`（不死院一段牆+地板，43 塊中最小的「有實體結構」者）：5 mesh / 2044 FLVER 頂點 → 1684 三角形（glTF index 5052），bbox 17.93 × 10.97 × 23.58 m（公尺級合理）。
  - 註：檔案絕對最小的 m0160（549B）只有 1 三角形、m9999（720B）是平面黑幕，不具代表性，故取 m0046。
- `tpf-extract`：從 `m18_0002` 卷撈出 m0046 引用的 18 張 DDS（floor_04 / wall_07 / wall_07_small / wall_08 / wall_08_add / wall_09 各 diffuse+normal+spec），DDS magic `44 44 53 20`（"DDS "）全部正確。
