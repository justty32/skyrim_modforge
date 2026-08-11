# worldspace-editor 後端 MVP（heightmap → 非平坦 LAND）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓 ModForge spec 帶一張 16-bit grayscale PNG heightmap，自動衍生 cell grid 並生出起伏的 LAND（VHGT），多 cell 接縫零誤差；不帶 heightmap 的既有平坦 spec 行為位元不變。

**Architecture:** 三層——① 純函式 VHGT 編解碼（`Vhgt.cs`，無外部依賴，可離線單測）；② PNG 載入 + cell grid 衍生 + 採樣（`Heightmap.cs`，用 ImageSharp）；③ 接進現有 `BuildWorldspaces`（heightmap 分支，平坦分支原封不動）。驗證與範例分列後段。

**Tech Stack:** C# net10.0、Mutagen.Bethesda.Skyrim 0.53.1、SixLabors.ImageSharp（PNG L16 解碼，跨平台）、xUnit。

設計依據：[workflows/specs/worldspace-editor-design.md](../specs/worldspace-editor-design.md)。VHGT 格式（signed int8、row-wise 累積、delta ×8 game units）已查證；Task 7 也已在主力機用真實 Tamriel LAND 完成反解比對（Tamriel 20 格 delta bytes 精確 round-trip）。

---

### Task 0: 前置確認（不改碼）

**Files:** 無（只讀）

- [ ] **Step 1: 確認 `Noggog.Array2d<byte>` 索引語意**

現有 flat 碼用 `new Noggog.Array2d<byte>(33, 33, 0)`（width, height, default）。需確認索引子是 `arr[x, y]` 還是 `arr[row, col]`、以及序列化列舉順序，以對齊 VHGT 的 row-major（33 列 × 33 欄）。

Run: `grep -rn "Array2d" ~/.nuget/packages/noggog.csharptools/*/lib/**/*.* 2>/dev/null | head` 或開 `C:\Users\user\.nuget\packages\` 下 Noggog 套件看 `Array2d` 的 indexer 定義。
Expected: 確定 `this[int x, int y]` 的 x/y 對應。**本計畫一律以 `arr[col, row]`（x=col=東向, y=row=北向）書寫**；若實測相反，Task 2 的 `ToArray2d` 內對調即可（Decode 自我一致測試會抓到方向錯）。

- [ ] **Step 2: 確認 ImageSharp L16 API**

Run: `dotnet add src/ModForge.Core/ModForge.Core.csproj package SixLabors.ImageSharp --version 3.1.5 --dry-run` 看可解析版本（或上 nuget 查最新 3.x）。
Expected: 確認 `using SixLabors.ImageSharp;` + `Image.Load<L16>(path)`、像素 `L16.PackedValue`（ushort 0–65535）可用。

---

### Task 1: 加 ImageSharp 依賴 + HeightmapSpec schema

**Files:**
- Modify: `src/ModForge.Core/ModForge.Core.csproj`
- Modify: `src/ModForge.Core/Spec.Worldspace.cs`（在 `WorldspaceSpec` 後新增類別 + 一個欄位）

- [ ] **Step 1: 加 ImageSharp PackageReference**

在 `ModForge.Core.csproj` 的 Mutagen `<ItemGroup>` 裡加一行：

```xml
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.5" />
```

- [ ] **Step 2: build 確認依賴解析**

Run: `dotnet build src/ModForge.Core/ModForge.Core.csproj`
Expected: build succeeded（ImageSharp 還原成功）。

- [ ] **Step 3: 加 HeightmapSpec + WorldspaceSpec.Heightmap 欄位**

在 `Spec.Worldspace.cs` 的 `WorldspaceSpec` 類別裡，`Cells` 屬性之後加：

```csharp
    // 非平坦地形（可選）。存在時忽略 Cells，依 PNG 尺寸自動衍生 cell grid 並生起伏 LAND。
    // 與 Cells 互斥；兩者都填時 heightmap 優先且 build 發 warn。null = 走平坦 Cells 路徑（行為不變）。
    public HeightmapSpec? Heightmap { get; set; }
```

在同檔 `WorldspaceSpec` 類別**之後**新增：

```csharp
/// <summary>
/// 一張覆蓋整個 worldspace 部分區域的 16-bit grayscale PNG heightmap。ModForge 依尺寸切成
/// N×M 個 cell（寬必須 = N×32+1、高 = M×32+1，相鄰格共用邊緣欄 → seam 零誤差）。
/// 高度 = MinHeight + (png/65535)×(MaxHeight−MinHeight)，game units。
/// </summary>
public sealed class HeightmapSpec
{
    public string Path { get; set; } = "";   // PNG 路徑，相對 spec 檔
    public int OriginX { get; set; }          // PNG 左下角像素對到的 cell 座標 X
    public int OriginY { get; set; }          // 同上 Y（左下=西南角；影像往上 = cell +Y/北）
    public float MinHeight { get; set; }      // png=0     → 此高度（game units）
    public float MaxHeight { get; set; } = 4000f; // png=65535 → 此高度
}
```

- [ ] **Step 4: build 確認編譯**

Run: `dotnet build src/ModForge.Core/ModForge.Core.csproj`
Expected: build succeeded。

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/ModForge.Core.csproj src/ModForge.Core/Spec.Worldspace.cs
git commit -m "feat(worldspace): add HeightmapSpec schema + ImageSharp dep"
```

---

### Task 2: 純函式 VHGT 編解碼器

**Files:**
- Create: `src/ModForge.Core/Vhgt.cs`
- Test: `tests/ModForge.Core.Tests/VhgtTests.cs`

- [ ] **Step 1: 寫失敗測試**

`tests/ModForge.Core.Tests/VhgtTests.cs`：

```csharp
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class VhgtTests
{
    // 33×33 全等高 → offset=H/8、所有 delta=0（與現有 flat LAND 位元一致）
    [Fact]
    public void Encode_FlatGrid_AllDeltasZero()
    {
        var h = new float[33, 33];
        for (int r = 0; r < 33; r++) for (int c = 0; c < 33; c++) h[r, c] = 4000f;

        var (offset, deltas) = Vhgt.Encode(h);

        Assert.Equal(500f, offset);            // 4000/8
        for (int x = 0; x < 33; x++) for (int y = 0; y < 33; y++)
            Assert.Equal((byte)0, deltas[x, y]);
    }

    // 任意平滑網格：Encode→Decode 重建，誤差 ≤ 半 delta 單位（±4 game units）
    [Fact]
    public void EncodeDecode_RoundTrip_WithinHalfStep()
    {
        var h = new float[33, 33];
        for (int r = 0; r < 33; r++) for (int c = 0; c < 33; c++)
            h[r, c] = 3000f + r * 24f + c * 16f;   // 平緩斜面，遠在 ±1016/step 內

        var (offset, deltas) = Vhgt.Encode(h);
        var back = Vhgt.Decode(offset, deltas);

        for (int r = 0; r < 33; r++) for (int c = 0; c < 33; c++)
            Assert.True(System.Math.Abs(back[r, c] - h[r, c]) <= 4f,
                $"[{r},{c}] {back[r, c]} vs {h[r, c]}");
    }

    // 超陡（單步 > 1016 units）→ clamp 且呼叫 warn
    [Fact]
    public void Encode_TooSteep_ClampsAndWarns()
    {
        var h = new float[33, 33];
        for (int r = 0; r < 33; r++) for (int c = 0; c < 33; c++) h[r, c] = 0f;
        h[0, 1] = 5000f;   // 一步 +5000 units 遠超 1016

        bool warned = false;
        Vhgt.Encode(h, _ => warned = true, "cell(0,0)");

        Assert.True(warned);
    }
}
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~VhgtTests"`
Expected: FAIL（`Vhgt` 不存在 / 編譯不過）。

- [ ] **Step 3: 寫實作**

`src/ModForge.Core/Vhgt.cs`：

```csharp
using Noggog;

namespace ModForge;

// VHGT (vertex height map) 編解碼。純函式、無 Mutagen/ImageSharp 依賴 → 離線可單測。
//
// 格式（查證自 Mutagen 0.53.1 + UESP + xEdit，見 specs/worldspace-editor-design.md）：
//  - Offset(float) = 頂點 [0,0] 高度基準；尺度 = ×8 game units。
//  - 33×33 signed-int8 delta，row-wise 累積：第 0 欄沿列往北累積成各列基準，
//    第 1–32 欄沿列內往東累積；每 delta 單位也 = 8 game units。
//  - Mutagen 寫 byte 原值不轉換 → 此處自做二補數（store (byte)(sbyte)d）。
// 索引約定：heights[row, col]（row=北向 0..32, col=東向 0..32）；Array2d 用 [col, row]（見 Task 0）。
public static class Vhgt
{
    private const float Scale = 8f;
    public const int Size = 33;

    /// <summary>絕對高度網格(game units) → VHGT offset + 33×33 二補數 delta byte。</summary>
    public static (float Offset, Array2d<byte> HeightMap) Encode(
        float[,] heights, System.Action<string>? warn = null, string cellLabel = "")
    {
        var deltas = new Array2d<byte>(Size, Size, 0);
        bool clamped = false;

        float offset = heights[0, 0] / Scale;
        float col0 = offset;          // 第 0 欄累積值（offset 單位）

        for (int r = 0; r < Size; r++)
        {
            if (r > 0)
            {
                float target = heights[r, 0] / Scale;
                int d = Clamp((int)System.Math.Round(target - col0), ref clamped);
                deltas[0, r] = unchecked((byte)(sbyte)d);
                col0 += d;            // 用實際重建值累積
            }
            float rowVal = col0;
            for (int c = 1; c < Size; c++)
            {
                float target = heights[r, c] / Scale;
                int d = Clamp((int)System.Math.Round(target - rowVal), ref clamped);
                deltas[c, r] = unchecked((byte)(sbyte)d);
                rowVal += d;
            }
        }

        if (clamped)
            warn?.Invoke($"  ! heightmap {cellLabel}: 地形過陡，VHGT delta 已 clamp 到 ±127（單步上限 1016 units）—高度差會被壓平");

        return (offset, deltas);
    }

    /// <summary>VHGT offset + delta → 絕對高度網格(game units)。Encode 的逆，供測試與文件。</summary>
    public static float[,] Decode(float offset, Array2d<byte> deltas)
    {
        var h = new float[Size, Size];
        float col0 = offset;
        for (int r = 0; r < Size; r++)
        {
            if (r > 0) col0 += (sbyte)deltas[0, r];
            float rowVal = col0;
            h[r, 0] = rowVal * Scale;
            for (int c = 1; c < Size; c++)
            {
                rowVal += (sbyte)deltas[c, r];
                h[r, c] = rowVal * Scale;
            }
        }
        return h;
    }

    private static int Clamp(int d, ref bool clamped)
    {
        if (d < -128) { clamped = true; return -128; }
        if (d > 127) { clamped = true; return 127; }
        return d;
    }
}
```

- [ ] **Step 4: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~VhgtTests"`
Expected: PASS（3 個）。若 round-trip 方向錯（誤差爆大），把 `deltas[c, r]` / `deltas[0, r]` 的索引對調（Task 0 的 Array2d 方向）。

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/Vhgt.cs tests/ModForge.Core.Tests/VhgtTests.cs
git commit -m "feat(worldspace): VHGT signed-delta encode/decode (pure, row-wise)"
```

---

### Task 3: PNG 載入 + cell grid 衍生 + 採樣

**Files:**
- Create: `src/ModForge.Core/Heightmap.cs`
- Test: `tests/ModForge.Core.Tests/HeightmapTests.cs`

- [ ] **Step 1: 寫失敗測試**

`tests/ModForge.Core.Tests/HeightmapTests.cs`：

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class HeightmapTests
{
    // 造一張 width×height 的 L16 PNG，像素值 = f(x,y)，回傳暫存路徑
    private static string MakePng(int w, int h, System.Func<int, int, ushort> f)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"mf_hm_{System.Guid.NewGuid():N}.png");
        using var img = new Image<L16>(w, h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            img[x, y] = new L16(f(x, y));
        img.Save(path);
        return path;
    }

    // 寬高非 N×32+1 → 丟 ArgumentException（不靜默裁切）
    [Fact]
    public void Load_BadDimensions_Throws()
    {
        var path = MakePng(64, 64, (x, y) => 0);   // 64 ≠ N×32+1
        var spec = new HeightmapSpec { Path = path, MinHeight = 0, MaxHeight = 4000 };
        Assert.Throws<System.ArgumentException>(() => Heightmap.Load(spec, System.IO.Path.GetDirectoryName(path)!));
        System.IO.File.Delete(path);
    }

    // 1×1 cell（33×33 px）：min/max 線性映射正確，原點/朝向正確
    [Fact]
    public void Load_SingleCell_MapsHeightsAndOrientation()
    {
        // png=0 → 1000, png=65535 → 5000。底列(影像 y=32)全黑、頂列(y=0)全白
        var path = MakePng(33, 33, (x, y) => y == 0 ? (ushort)65535 : (ushort)0);
        var spec = new HeightmapSpec { Path = path, OriginX = 0, OriginY = 0, MinHeight = 1000, MaxHeight = 5000 };

        var hm = Heightmap.Load(spec, System.IO.Path.GetDirectoryName(path)!);

        Assert.Equal(1, hm.CellsX);
        Assert.Equal(1, hm.CellsY);
        var grid = hm.SampleCell(0, 0);   // 回傳 [row,col]，row 0 = 南/影像底
        // 影像頂列(白=max)對到世界北 → grid 的最北一列(row 32)應是 5000
        Assert.Equal(5000f, grid[32, 0], 3);
        Assert.Equal(1000f, grid[0, 0], 3);   // 南列=黑=min
        System.IO.File.Delete(path);
    }

    // 2×1 cells（65×33 px）：相鄰格共用第 32 欄 → 邊緣逐頂點相等（seam）
    [Fact]
    public void SampleCell_SharedEdge_MatchesBetweenNeighbors()
    {
        // 水平漸層，值依 x 變化
        var path = MakePng(65, 33, (x, y) => (ushort)(x * 1000));
        var spec = new HeightmapSpec { Path = path, OriginX = 0, OriginY = 0, MinHeight = 0, MaxHeight = 6000 };

        var hm = Heightmap.Load(spec, System.IO.Path.GetDirectoryName(path)!);
        Assert.Equal(2, hm.CellsX);

        var left = hm.SampleCell(0, 0);    // 取 px 欄 [0..32]
        var right = hm.SampleCell(1, 0);   // 取 px 欄 [32..64]
        for (int row = 0; row < 33; row++)
            Assert.Equal(left[row, 32], right[row, 0], 3);   // 共用第 32 欄
        System.IO.File.Delete(path);
    }
}
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~HeightmapTests"`
Expected: FAIL（`Heightmap` 不存在）。

- [ ] **Step 3: 寫實作**

`src/ModForge.Core/Heightmap.cs`：

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ModForge;

// 載入一張 16-bit grayscale PNG → 全域高度網格，按 cell grid 切割採樣。
// PNG 寬 = CellsX×32+1、高 = CellsY×32+1。影像左下=西南角；影像往上 = 世界 +Y(北)。
// 採樣回傳的網格索引 [row, col]：row 0 = 最南(世界 -Y)、col 0 = 最西。
public sealed class Heightmap
{
    private const int CellVerts = 33;
    private const int CellStep = 32;

    public int CellsX { get; }
    public int CellsY { get; }
    public int OriginX { get; }
    public int OriginY { get; }
    private readonly float[,] _world;   // [worldRow, worldCol]，row 0 = 最南
    private readonly int _w, _h;

    private Heightmap(float[,] world, int w, int h, int cx, int cy, int ox, int oy)
    { _world = world; _w = w; _h = h; CellsX = cx; CellsY = cy; OriginX = ox; OriginY = oy; }

    public static Heightmap Load(HeightmapSpec spec, string specDir)
    {
        var path = System.IO.Path.IsPathRooted(spec.Path)
            ? spec.Path : System.IO.Path.Combine(specDir, spec.Path);
        if (!System.IO.File.Exists(path))
            throw new System.IO.FileNotFoundException($"heightmap PNG not found: {path}");

        using var img = Image.Load<L16>(path);
        int w = img.Width, h = img.Height;
        if ((w - 1) % CellStep != 0 || (h - 1) % CellStep != 0 || w < CellVerts || h < CellVerts)
            throw new System.ArgumentException(
                $"heightmap '{spec.Path}' is {w}×{h}px; width must be N×32+1 and height M×32+1 (e.g. 33,65,97…)");

        int cx = (w - 1) / CellStep, cy = (h - 1) / CellStep;
        float range = spec.MaxHeight - spec.MinHeight;

        // 影像 y=0 在頂端=世界最北。翻轉成 world[row,col] row0=最南。
        var world = new float[h, w];
        img.ProcessPixelRows(accessor =>
        {
            for (int imgY = 0; imgY < h; imgY++)
            {
                var rowSpan = accessor.GetRowSpan(imgY);
                int worldRow = (h - 1) - imgY;       // 翻轉
                for (int x = 0; x < w; x++)
                    world[worldRow, x] = spec.MinHeight + (rowSpan[x].PackedValue / 65535f) * range;
            }
        });

        return new Heightmap(world, w, h, cx, cy, spec.OriginX, spec.OriginY);
    }

    /// <summary>取第 (cellX, cellY) 格的 33×33 高度網格，[row,col] row0=最南、col0=最西。</summary>
    public float[,] SampleCell(int cellX, int cellY)
    {
        var grid = new float[CellVerts, CellVerts];
        int baseCol = cellX * CellStep;   // 相鄰格共用第 32 欄 → 邊緣自動對齊
        int baseRow = cellY * CellStep;
        for (int row = 0; row < CellVerts; row++)
            for (int col = 0; col < CellVerts; col++)
                grid[row, col] = _world[baseRow + row, baseCol + col];
        return grid;
    }
}
```

- [ ] **Step 4: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~HeightmapTests"`
Expected: PASS（3 個）。

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/Heightmap.cs tests/ModForge.Core.Tests/HeightmapTests.cs
git commit -m "feat(worldspace): PNG heightmap load + cell-grid sampling (seam-aligned)"
```

---

### Task 4: 接進 BuildWorldspaces（heightmap 分支）

**Files:**
- Modify: `src/ModForge.Core/Generator.Build.Worldspace.cs`（`BuildWorldspaces` 內的 cell 迴圈）
- Test: `tests/ModForge.Core.Tests/WorldspaceHeightmapTests.cs`

- [ ] **Step 1: 寫失敗測試**

`tests/ModForge.Core.Tests/WorldspaceHeightmapTests.cs`：

```csharp
using System.Linq;
using Mutagen.Bethesda.Plugins;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class WorldspaceHeightmapTests
{
    private static ModKey Out => ModKey.FromNameAndExtension("Test.esp");

    private static string MakePng(int w, int h, System.Func<int, int, ushort> f)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mf_ws_{System.Guid.NewGuid():N}.png");
        using var img = new Image<L16>(w, h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) img[x, y] = new L16(f(x, y));
        img.Save(path);
        return path;
    }

    private static WorldspaceSpec World(HeightmapSpec hm) => new()
    {
        EditorId = "HMWorld", Name = "HM", Climate = "Skyrim.esm:0x000812",
        Water = "Skyrim.esm:0x000018", Flags = { "SmallWorld" }, Heightmap = hm,
    };

    // 2×1 PNG → 衍生出 2 格 cell（OriginX..OriginX+1, OriginY）
    [Fact]
    public void Heightmap_DerivesCellGridFromPngSize()
    {
        var path = MakePng(65, 33, (x, y) => 30000);
        var spec = new ModSpec { Esl = false, Worldspaces =
            { World(new HeightmapSpec { Path = path, OriginX = 5, OriginY = 7, MinHeight = 0, MaxHeight = 4000 }) } };

        var w = Generator.Build(spec, Out).Mod.Worldspaces.First(x => x.EditorID == "HMWorld");
        var cells = w.SubCells.SelectMany(b => b.Items).SelectMany(s => s.Items).ToList();

        Assert.Equal(2, cells.Count);
        Assert.Contains(cells, c => c.Grid!.Point.X == 5 && c.Grid.Point.Y == 7);
        Assert.Contains(cells, c => c.Grid!.Point.X == 6 && c.Grid.Point.Y == 7);
        System.IO.File.Delete(path);
    }

    // 起伏 PNG → LAND 的 VHGT delta 非全 0（地形真的有起伏）
    [Fact]
    public void Heightmap_ProducesNonZeroVhgtDeltas()
    {
        var path = MakePng(33, 33, (x, y) => (ushort)(y * 2000));   // 南北漸層
        var spec = new ModSpec { Esl = false, Worldspaces =
            { World(new HeightmapSpec { Path = path, OriginX = 0, OriginY = 0, MinHeight = 0, MaxHeight = 8000 }) } };

        var w = Generator.Build(spec, Out).Mod.Worldspaces.First(x => x.EditorID == "HMWorld");
        var land = w.SubCells.SelectMany(b => b.Items).SelectMany(s => s.Items).First().Landscape!;

        bool anyNonZero = false;
        var hmArr = land.VertexHeightMap!.HeightMap;
        for (int x = 0; x < 33; x++) for (int y = 0; y < 33; y++)
            if (hmArr[x, y] != 0) anyNonZero = true;
        Assert.True(anyNonZero);
        Assert.Equal(Landscape.Flag.VertexNormalsHeightMap, land.Flags & Landscape.Flag.VertexNormalsHeightMap);
        System.IO.File.Delete(path);
    }

    // 行為不變：heightmap 全等值 PNG 生成的 LAND，與同高度 flat Cells spec 的 VHGT 完全相同
    [Fact]
    public void Heightmap_FlatPng_MatchesFlatCellPath()
    {
        var path = MakePng(33, 33, (x, y) => 65535);   // 全白 → 全 = MaxHeight
        var hmSpec = new ModSpec { Esl = false, Worldspaces =
            { World(new HeightmapSpec { Path = path, OriginX = 0, OriginY = 0, MinHeight = 0, MaxHeight = 4000 }) } };
        var flatWs = new WorldspaceSpec { EditorId = "HMWorld", Name = "HM", Climate = "Skyrim.esm:0x000812",
            Water = "Skyrim.esm:0x000018", Flags = { "SmallWorld" } };
        flatWs.Cells.Add(new WorldspaceCellSpec { X = 0, Y = 0, Height = 4000f });
        var flatSpec = new ModSpec { Esl = false, Worldspaces = { flatWs } };

        var hmLand = Generator.Build(hmSpec, Out).Mod.Worldspaces.First().SubCells
            .SelectMany(b => b.Items).SelectMany(s => s.Items).First().Landscape!;
        var flatLand = Generator.Build(flatSpec, Out).Mod.Worldspaces.First().SubCells
            .SelectMany(b => b.Items).SelectMany(s => s.Items).First().Landscape!;

        Assert.Equal(flatLand.VertexHeightMap!.Offset, hmLand.VertexHeightMap!.Offset, 3);
        for (int x = 0; x < 33; x++) for (int y = 0; y < 33; y++)
            Assert.Equal(flatLand.VertexHeightMap!.HeightMap[x, y], hmLand.VertexHeightMap!.HeightMap[x, y]);
        System.IO.File.Delete(path);
    }
}
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~WorldspaceHeightmapTests"`
Expected: FAIL（heightmap 分支未實作 → 衍生 0 格 cell）。

- [ ] **Step 3: 寫實作 — 抽出建 cell 的共用碼 + 加 heightmap 分支**

在 `Generator.Build.Worldspace.cs`，把現有 `foreach (var cs in ws.Cells) { ... }` 迴圈**整段替換**為：依 heightmap 有無走不同來源，但共用「建 CELL+LAND、塞進 block tree」邏輯。新增一個 local function `EmitCell` 並改迴圈：

```csharp
            // 建一格 CELL+LAND 塞進 block tree 的共用邏輯（平坦與 heightmap 共用）。
            void EmitCell(int cx, int cy, float offset, Noggog.Array2d<byte> heightDeltas, bool navmesh)
            {
                short bx = (short)FloorDiv(cx, 32), by = (short)FloorDiv(cy, 32);
                short sx = (short)FloorDiv(cx, 8),  sy = (short)FloorDiv(cy, 8);

                var block = w.SubCells.FirstOrDefault(b => b.BlockNumberX == bx && b.BlockNumberY == by);
                if (block is null)
                {
                    block = new WorldspaceBlock { BlockNumberX = bx, BlockNumberY = by, GroupType = GroupTypeEnum.ExteriorCellBlock };
                    w.SubCells.Add(block);
                }
                var sub = block.Items.FirstOrDefault(s => s.BlockNumberX == sx && s.BlockNumberY == sy);
                if (sub is null)
                {
                    sub = new WorldspaceSubBlock { BlockNumberX = sx, BlockNumberY = sy, GroupType = GroupTypeEnum.ExteriorCellSubBlock };
                    block.Items.Add(sub);
                }

                var edBase = string.IsNullOrWhiteSpace(ws.EditorId) ? "MF" : ws.EditorId;
                var xTag = cx < 0 ? $"m{-cx}" : cx.ToString();
                var yTag = cy < 0 ? $"m{-cy}" : cy.ToString();
                var cell = new Cell(mod, $"{edBase}_Cell_{xTag}_{yTag}");
                cell.Grid = new CellGrid { Point = new Noggog.P2Int(cx, cy) };

                var land = new Landscape(mod);
                land.Flags = Landscape.Flag.VertexNormalsHeightMap;
                land.VertexHeightMap = new LandscapeVertexHeightMap
                {
                    Offset = offset,
                    HeightMap = heightDeltas,
                    Unknown = new Noggog.P3UInt8(0, 0, 0),
                };
                // MVP：法線全朝上（B 路線再從鄰頂點高度差重算）。
                land.VertexNormals = new Noggog.Array2d<Noggog.P3UInt8>(33, 33, new Noggog.P3UInt8(128, 128, 255));
                cell.Landscape = land;

                if (navmesh)
                {
                    var cs = new WorldspaceCellSpec { X = cx, Y = cy, Navmesh = true };
                    AddFlatCellNavmesh(mod, cell, cs, w.FormKey, navmInfos);
                    navmeshCells++;
                }

                sub.Items.Add(cell);
                terrainCells++;
            }

            if (ws.Heightmap is { } hmSpec)
            {
                if (ws.Cells.Count > 0)
                    warn($"  ! worldspace '{ws.EditorId}' has both heightmap and cells — using heightmap, ignoring {ws.Cells.Count} flat cell(s)");

                var hm = Heightmap.Load(hmSpec, SpecDir);
                for (int cyi = 0; cyi < hm.CellsY; cyi++)
                    for (int cxi = 0; cxi < hm.CellsX; cxi++)
                    {
                        int gx = hm.OriginX + cxi, gy = hm.OriginY + cyi;
                        var grid = hm.SampleCell(cxi, cyi);
                        var (offset, deltas) = Vhgt.Encode(grid, warn, $"cell({gx},{gy})");
                        EmitCell(gx, gy, offset, deltas, navmesh: false);   // MVP：heightmap 不生 navmesh
                    }
            }
            else
            {
                foreach (var cs in ws.Cells)
                    EmitCell(cs.X, cs.Y, cs.Height / 8f, new Noggog.Array2d<byte>(33, 33, 0), cs.Navmesh);
            }
```

**注意 `SpecDir`**：`Heightmap.Load` 需 spec 檔所在目錄解析相對 PNG 路徑。確認 `BuildWorldspaces` 能拿到——若 `Generator.Build` 已有 spec 路徑欄位（grep `SpecDir`/`specPath`），用之；若無，最小改動：給 `Generator.Build` 加一個可選 `string specDir = ""` 參數並透傳到 `BuildWorldspaces`，測試用絕對 PNG 路徑（已是絕對）故 `specDir` 傳 `""` 也能過。**先 grep 確認**：

Run: `grep -rn "SpecDir\|specDir\|specPath" src/ModForge.Core/Generator.cs src/ModForge.Core/Generator.Build.cs 2>/dev/null`
若無 → 在 `BuildWorldspaces` 簽章與 `Generator.Build` 加 `string specDir`，`Heightmap.Load(hmSpec, specDir)`。

- [ ] **Step 4: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~WorldspaceHeightmapTests"`
Expected: PASS（3 個）。`Heightmap_FlatPng_MatchesFlatCellPath` 通過即「行為不變」鐵證。

- [ ] **Step 5: 跑全離線測試確認沒打壞既有**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"`
Expected: 全 PASS（含原 WorldspaceRegionTests 不受影響）。

- [ ] **Step 6: Commit**

```bash
git add src/ModForge.Core/Generator.Build.Worldspace.cs tests/ModForge.Core.Tests/WorldspaceHeightmapTests.cs
git commit -m "feat(worldspace): build non-flat LAND from heightmap (cell-grid derive + VHGT)"
```

---

### Task 5: 驗證（Generator.Validate）

**Files:**
- Modify: `src/ModForge.Core/Generator.Validate.World.cs`（worldspace 迴圈內）
- Test: 追加到 `tests/ModForge.Core.Tests/WorldspaceHeightmapTests.cs`

- [ ] **Step 1: 寫失敗測試（追加）**

在 `WorldspaceHeightmapTests` 內加：

```csharp
    [Fact]
    public void Validate_HeightmapMinNotLessThanMax_IsFlagged()
    {
        var spec = new ModSpec { Esl = false, Worldspaces =
            { World(new HeightmapSpec { Path = "x.png", OriginX = 0, OriginY = 0, MinHeight = 5000, MaxHeight = 4000 }) } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("minHeight"));
    }

    [Fact]
    public void Validate_HeightmapEmptyPath_IsFlagged()
    {
        var spec = new ModSpec { Esl = false, Worldspaces =
            { World(new HeightmapSpec { Path = "", MinHeight = 0, MaxHeight = 4000 }) } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("heightmap"));
    }

    [Fact]
    public void Validate_EslWithHeightmap_IsFlagged()
    {
        var ws = World(new HeightmapSpec { Path = "x.png", MinHeight = 0, MaxHeight = 4000 });
        var spec = new ModSpec { Esl = true, Worldspaces = { ws } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("esl"));
    }
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~Validate_Heightmap|FullyQualifiedName~Validate_EslWithHeightmap"`
Expected: FAIL。

- [ ] **Step 3: 寫實作**

在 `Generator.Validate.World.cs`，ESL 檢查改成也涵蓋 heightmap，並在 worldspace 迴圈內加 heightmap 驗證：

把第 100 行的 ESL 檢查條件擴成：

```csharp
            if (spec.Esl && spec.Worldspaces.Any(ws => ws.Cells.Count > 0 || ws.Heightmap is not null))
                Problems.Add("spec has esl=true but worldspace(s) define terrain (cells or heightmap) — Skyrim's engine does not load LAND records from ESL (light) plugins; set esl=false for any spec that generates terrain");
```

在 worldspace `foreach` 迴圈內（flag 檢查之後）加：

```csharp
                if (ws.Heightmap is { } hm)
                {
                    if (string.IsNullOrWhiteSpace(hm.Path))
                        Problems.Add($"worldspace '{ws.EditorId}' heightmap has empty path");
                    if (hm.MinHeight >= hm.MaxHeight)
                        Problems.Add($"worldspace '{ws.EditorId}' heightmap minHeight ({hm.MinHeight}) must be < maxHeight ({hm.MaxHeight})");
                }
```

- [ ] **Step 4: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~WorldspaceHeightmapTests"`
Expected: 全 PASS（6 個）。

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/Generator.Validate.World.cs tests/ModForge.Core.Tests/WorldspaceHeightmapTests.cs
git commit -m "feat(worldspace): validate heightmap spec (path, min<max, esl)"
```

---

### Task 6: 範例 spec + 文件

**Files:**
- Create: `examples/worldspace_heightmap.json`
- Create: `examples/worldspace_heightmap.png`（程式生的小測試圖，或註明由 Godot 出）
- Modify: `docs/spec`（worldspace 章節，補 heightmap 欄位說明）— 先 grep 確認正確檔

- [ ] **Step 1: 生一張範例 PNG（97×33 = 3×1 cells 的小山丘）**

Run（用既有測試工具或一次性 script）：

```bash
dotnet run --project src/ModForge.Cli -- # 若有 heightmap 預覽指令；否則手動拿 Godot 出圖
```

若 CLI 無此能力，本步改為：在 `examples/` 放一張 97×33 的 L16 PNG（中央亮、邊緣暗，做出小丘），並在 json 註明來源。

- [ ] **Step 2: 寫範例 spec `examples/worldspace_heightmap.json`**

```json
{
  "name": "WorldspaceHeightmapDemo",
  "esl": false,
  "worldspaces": [
    {
      "editorId": "HeightmapDemoWorld",
      "name": "Heightmap Demo",
      "climate": "Skyrim.esm:0x000812",
      "water": "Skyrim.esm:0x000018",
      "flags": ["SmallWorld"],
      "heightmap": {
        "path": "worldspace_heightmap.png",
        "originX": 0,
        "originY": 0,
        "minHeight": 0,
        "maxHeight": 8000
      }
    }
  ]
}
```

- [ ] **Step 3: 端到端生成確認**

Run: `dotnet run --project src/ModForge.Cli -- build examples/worldspace_heightmap.json /tmp/HeightmapDemo.esp`（指令格式照 CLI 既有 build 子命令，先 `--help` 確認）
Expected: 生成成功、stats 顯示 3 個 terrain cell、無 error（過陡才 warn）。

- [ ] **Step 4: 補文件**

Run: `grep -rln "DefaultWaterHeight\|worldspace" docs/spec* 2>/dev/null | head`
找到 worldspace spec 文件，補一段 heightmap 欄位說明（path/origin/min/max + 尺寸規則 N×32+1 + 與 Cells 互斥）。

- [ ] **Step 5: Commit**

```bash
git add examples/worldspace_heightmap.json examples/worldspace_heightmap.png docs/
git commit -m "docs(worldspace): heightmap example spec + spec doc"
```

---

### Task 7: 🔴 主力機收尾驗證（不在離線機，記 WAIT_USER）

**Files:** 無（驗證 + 記錄）

- [x] **Step 1: 對真實 Tamriel 斜坡格反解比對**（2026-06-16 落地；2026-08-11 重跑 PASS）

在主力機（有 Skyrim.esm）寫一次性測試：用 Mutagen 讀 Tamriel 某已知斜坡 cell 的 LAND，取 `VertexHeightMap.Offset` + `HeightMap`，餵 `Vhgt.Decode`，比對重建高度與該地形已知高度（或 xEdit 顯示值）是否吻合。

- [x] **Step 2: 結論回填**（design / landed / CODE_MAP 已記錄 Tamriel 20 格精確 round-trip）

吻合 → 在 `coord-system.md` 與 design 把「待主力機收尾」標✅移除；不吻合 → 依差異修 `Vhgt`（多半是 Array2d 索引方向或 row/col 對調），離線單測仍綠後重打包。

---

## Self-Review

**Spec coverage：**
- HeightmapSpec schema → Task 1 ✓
- PNG 自動衍生 cell grid（N×32+1）→ Task 3（尺寸校驗）+ Task 4（衍生）✓
- min/max 線性高度 → Task 3 ✓
- PNG→VHGT 逆推（signed、row-wise、誤差用重建值累積、clamp+warn）→ Task 2 ✓
- seam 零誤差 → Task 3（共用欄測試）✓
- 平坦行為不變 → Task 2（全 0）+ Task 4（FlatPng==FlatCell 位元比對）✓
- VNML MVP 全朝上 → Task 4 ✓
- heightmap 模式不生 navmesh → Task 4（`navmesh: false`）✓
- 驗證（path/min<max/esl）→ Task 5 ✓
- 主力機 round-trip → Task 7 ✓

**Placeholder scan：** Task 0/4 的「先 grep 確認」是必要的環境探測（Array2d 索引方向、SpecDir 有無），非 placeholder——已附明確 fallback。Task 6 範例 PNG/CLI 指令依 CLI 實況微調，已給 fallback。

**Type consistency：** `Vhgt.Encode(float[,], Action<string>?, string) → (float, Array2d<byte>)`、`Vhgt.Decode(float, Array2d<byte>) → float[,]`、`Heightmap.Load(HeightmapSpec, string) → Heightmap`、`Heightmap.SampleCell(int,int) → float[,]`、`HeightmapSpec.{Path,OriginX,OriginY,MinHeight,MaxHeight}`——跨 Task 一致。`EmitCell(int,int,float,Array2d<byte>,bool)` 在 Task 4 內定義並使用。

**已知未決：** 無。Array2d 索引方向與真實引擎 round-trip 皆已由 `VhgtTests` / `VnmlCompute_OrientationMatchesVanilla` 對 Tamriel LAND 驗證。
