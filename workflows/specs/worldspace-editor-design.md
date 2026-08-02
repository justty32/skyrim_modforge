# worldspace-editor 後端 — 設計方案（heightmap → 非平坦 LAND）

← [specs 入口](README.md)｜idea：[worldspace-editor/README.md](../../../godot-worldspace-editor/README.md)｜座標：[coord-system.md](../../../godot-worldspace-editor/design/coord-system.md)

本 spec 只涵蓋 **Idea #19 三階段 pipeline 的第 2 階段（ModForge 後端）的 MVP**：讀一張 16-bit grayscale PNG heightmap → 自動衍生 cell grid → 生非平坦 LAND（VHGT 起伏）→ COW 可進入、地形有起伏。Godot 前端（第 1 階段）與遊戲內微調（第 3 階段）不在本 spec。

---

## 目標 / 成功判準

- spec 加一張 PNG 路徑 + 幾個換算欄位 → `dotnet run` 生出的 esp 進遊戲後，`cow <world> X Y` 站上去地形**有起伏**（不是現在的全平）。
- 多 cell 之間**接縫不裂**（相鄰格邊緣高度逐頂點相同）。
- **行為不變保證**：不帶 heightmap 的既有平坦 spec，生成結果**位元不變**（平坦走原路徑）。

## 範圍邊界（YAGNI）

| 納入 MVP | 排除（後排） |
|---|---|
| 單張 PNG → N×M cells 自動衍生 | 物件擺放（placements.json）→ 另一輪 brainstorm |
| VHGT 起伏（逆推 offset + signed delta） | VTXT 紋理層（初期全 dirt，B 路線再做 splatmap） |
| Seam 對齊（PNG 共用欄保證） | Navmesh（heightmap 模式 MVP 不生，後排） |
| VNML 法線重算（可選，預設全朝上） | NIF→glTF 預覽（Godot 前端事，且 fo76utils 已證可行） |
| 高度超坡度 clamp 時 warn | LOD（→ xLODGen，roadmap #11） |

---

## Schema 變更

`WorldspaceSpec` 加一個**可選** `Heightmap` 欄位（[Spec.Worldspace.cs](../../src/ModForge.Core/Spec.Worldspace.cs)）：

```csharp
public sealed class HeightmapSpec
{
    public string Path { get; set; } = "";      // 16-bit grayscale PNG，相對 spec 檔路徑
    public int OriginX { get; set; }             // PNG 左下角像素對到的 cell 座標 X
    public int OriginY { get; set; }             // 同上 Y（左下＝西南角；影像往上 = cell +Y/北）
    public float MinHeight { get; set; }         // png 值 0     → 此高度（game units）
    public float MaxHeight { get; set; }         // png 值 65535 → 此高度
}
```

`WorldspaceSpec.Heightmap`（nullable）。**兩種模式互斥**：
- `Heightmap == null`：走現有扁平路徑（`Cells` 顯式清單，每格 `Height` float）——**完全不動，位元不變**。
- `Heightmap != null`：忽略 `Cells`，依 PNG 自動衍生 cell grid（見下）。若兩者都填，heightmap 優先並在 log warn。

### 影像朝向約定（固定）

- PNG **左下角**像素 = cell `(OriginX, OriginY)` 的**西南**頂點。
- 影像往**右** = cell +X（東）；影像往**上** = cell +Y（北）。
- PNG 第 0 列在影像頂端，故讀檔時**翻轉 row**（影像 row → 世界 Y 反向）。

### 高度映射（min/max 線性）

```
height_game_units = MinHeight + (png_value / 65535) × (MaxHeight − MinHeight)
```

不需絕對公尺精度；換算係數記在 spec 即真相。Godot 出圖時最低/最高點對到 Min/Max。

---

## Cell grid 衍生

- PNG 尺寸**必須** 寬 `= N×32 + 1`、高 `= M×32 + 1`（N,M ≥ 1）。否則 cell 邊界對不齊 → **報錯停止**（不靜默裁切）。
- 自動生出 `N×M` 格 cells，**cell `(OriginX+c, OriginY+r)`**（c=0..N-1 由左到右、r=0..M-1 由下到上）取 PNG 子區：
  - X 方向：`[c×32 .. c×32+32]` 共 **33 欄**
  - Y 方向：對應的 33 列（含翻轉）
  - **相鄰格共用第 32 欄／列** → seam 零誤差（單張 PNG 切割的核心好處）。
- `WorldMapData`（地圖邊界）依衍生的 cell 範圍**自動算**，不用手填。

---

## PNG → VHGT 生成演算法（核心）

### 已驗證的 VHGT 格式事實

來源：背景 agent 對 Mutagen 0.53.1 原碼（`Landscape.xml` Loqui def + DLL 符號）+ UESP + xEdit fopdoc 查證（高信心）。

1. **Mutagen 寫 byte 原值、不做轉換**；Skyrim 引擎讀成 **signed int8**。→ **ModForge 自己做二補數**：存 delta −10 = `(byte)(sbyte)(-10)` = byte 246。
2. **累積方式（row-wise）**：`Offset`(float) = 頂點 [0,0] 高度基準；逐列（由底而上）、列內逐欄（由左而右）累加 signed delta。第 0 欄沿列方向往北累積成各列基準，第 1–32 欄沿列內往東累積。
3. **尺度**：**offset 與每個 delta 單位都 = 8 game units**。⚠️ 舊 coord-system.md 寫「1/8」是**錯的**（1/8 單步最大僅 ±16 units，做不出山；正解 ×8，單步最大 ±127×8 = ±1016 units）。
4. **API**：`Landscape.VertexHeightMap.Offset`(float) + `.HeightMap`(`Noggog.Array2d<byte>` 33×33) + `.Unknown`(P3UInt8)，與現有 [Generator.Build.Worldspace.cs](../../src/ModForge.Core/Generator.Build.Worldspace.cs) 一致。

### 逆推（已知目標高度網格 → offset + deltas）

給定每格 33×33 目標高度 `H[r][c]`（game units，已由 PNG 映射）：

```
V[r][c] = H[r][c] / 8                # 換到 offset 單位

offset_float = V[0][0]               # delta[0][0]=0，重現 height[0][0]
col0 = offset_float
for r in 0..32:
    if r > 0:
        d = clamp(round(V[r][0] - col0), -128, 127)
        delta[r][0] = (byte)(sbyte)d
        col0 += d                    # ★ 用「實際重建值」累積，含 round/clamp 誤差
    row_val = col0
    for c in 1..32:
        d = clamp(round(V[r][c] - row_val), -128, 127)
        delta[r][c] = (byte)(sbyte)d
        row_val += d                 # ★ 同上
```

設計要點：
- **誤差用實際重建值累積**（非理想 target）→ 避免誤差跨格漂移。
- **clamp 到 ±127** = 單步坡度上限 ±1016 units/128 units。超過代表地形過陡無法表示 → **發 warn**（照 CLAUDE.md「no silent caps」，不靜默截斷）。
- **平坦特例**：`H` 全相等 → 所有 delta=0、`offset=Height/8` → 與現有 flat 碼**位元一致**（行為不變的數學保證）。

### Seam（多 cell 接縫）

相鄰格共用 PNG 同一欄／列像素 → 兩格邊緣頂點 `H` 值**逐點相同** → 各自獨立編碼，但重建出的邊緣高度一致 → **頂點層 seam 零誤差**。不需額外對齊邏輯，PNG 共用欄已保證。

### VNML 法線（可選，B 路線）

- **MVP 預設**：沿用現有「全朝上」VNML（地形可走、光照略平，可接受）。
- **B 路線**：法線 = 鄰頂點高度差的叉積。關鍵——**直接從全域 PNG 高度網格取樣**（非 per-cell），cell 邊緣頂點能取到鄰格那側的高度（PNG 連續），邊緣法線自然正確，**不需 1px overlap 特例處理**（這修正了 README 舊「需保留 1px overlap」的顧慮）。

---

## 測試策略

- **單元（離線可跑，`Category!=RequiresSkyrim`）**：
  - 平坦 round-trip：heightmap 全等值 PNG → 生成結果與同高度 flat spec **位元相同**（行為不變鐵證）。
  - 逆推正確性：手造已知 `H` 網格 → 跑逆推 → 用 decode 演算法重建 → 比對誤差 ≤ round 半單位（±4 game units）。
  - Seam：2×1 cells，比對共用邊欄兩格重建高度逐點相等。
  - 尺寸校驗：PNG 寬非 `N×32+1` → 報錯。
  - Clamp warn：造超陡 PNG → 確認有 warn 且不崩。
- ✅ **主力機 round-trip 已驗（2026-06-16）**：Tamriel 20 格 decode→encode delta bytes 完全一致、offset 誤差 < 0.001——演算法確認正確。`RequiresSkyrim` test 進 VhgtTests（596 tests 全綠）。

---

## 開放 / 後續（非本 MVP）

- **placements.json（物件擺放）**：schema 已預想 `@export editor_id` + Godot 原生座標 ModForge 轉，格式待另一輪 brainstorm。
- **VTXT splatmap**、**精確 navmesh**、**LOD**：見 idea README 演進路線。
- **NIF→glTF 預覽**：屬 Godot 前端；工具選定 **fo76utils/nifskope**（已查證真實、Linux+Windows 雙平台，主力機可跑；`nif2gltf` Rust CLI 經查證**不存在**，勿規劃）。詳見 [nif-gltf finding](../../sub_projs/gemini-research/worldspace-editor/nif-gltf-conversion.md)。
