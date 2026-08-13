# 明亮室內光照管線 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓 spec 能建自訂 LGTM（LightingTemplate）+ IMGS（ImageSpace）base record，並讓 interior CELL 逐欄授權光照（含 DALC 六方向環境光）+ 掛 LGTM/IMGS，把陰暗地城變明亮。

**Architecture:** 三個 record 皆「模板抄 vanilla + 只覆寫有給的欄位」（延續 codebase 的 `template` 哲學）。LGTM/IMGS 在 build pass 1 的 `BuildCells` **之前**建好並登錄 editorId→record，故 CELL 在 pass 1 即可按 editorId（自訂）或 `<master>:0xFORMID`（vanilla）解析掛上。interior CELL 必有 XCLL（`Lighting`），其 `Inherits` flags 決定哪些欄位從 LGTM 拉。

**Tech Stack:** C# net10.0、Mutagen.Bethesda.Skyrim 0.53.1、xUnit。純 object-in/object-out 測試（不需 Skyrim 安裝）。

**地基（已用 Mutagen 反射 + Skyrim.esm 解碼確認 2026-06-09）：**
- LGTM 與 CellLighting 共享光照欄位；LGTM 用 `LightFadeStartDistance/EndDistance`，XCLL 用 `LightFadeBegin/End`。
- **DALC 對應**：LGTM 的 DALC = `DirectionalAmbientColors`（`AmbientColors` 是 legacy 全零，不用）；CELL XCLL 的 DALC = `AmbientColors`。
- `AmbientColors` 型別欄位：`DirectionalXPlus/XMinus/YPlus/YMinus/ZPlus/ZMinus`(Color) + `Specular`(Color) + `Scale`(float)。
- IMGS 子結構：`Hdr`(ImageSpaceHdr)、`Cinematic`(ImageSpaceCinematic)、`Tint`(ImageSpaceTint)；fresh record 上可能為 null，需 `??= new()`。
- CellLighting.Inherit flags：`AmbientColor / DirectionalColor / FogColor / FogNear / FogFar / DirectionalRotation / DirectionalFade / ClipDistance / FogPower / FogMax / LightFadeDistances`。
- vanilla interior CELL 的 `Inherits` 預設為全旗標（全繼承模板）。
- `ToColor(ColorSpec)` + `Clamp255` 在 `Generator.Build.Weather.cs`（同 partial class，直接可用）。
- `TryResolveTemplate<T>(string ref, out T? tmpl)`（BuildContext，只認 `<master>:0xFORMID` external ref）。

---

## File Structure

| 檔案 | 動作 | 職責 |
|------|------|------|
| `src/ModForge.Core/Spec/Spec.Lighting.cs` | 新建 | `LightingTemplateSpec` / `AmbientColorsSpec` / `ImageSpaceSpec` / `CellLightingSpec` |
| `src/ModForge.Core/Spec/Spec.cs` | 改 | 加 `LightingTemplates` / `ImageSpaces` 兩個 List |
| `src/ModForge.Core/Spec/Spec.World.cs` | 改 | `CellSpec` 加 `LightingTemplate` / `ImageSpace` / `Lighting` 三欄 |
| `src/ModForge.Core/Build/Generator.Build.Lighting.cs` | 新建 | `BuildLightingTemplates` / `BuildImageSpaces` + `FillAmbientColors` helper |
| `src/ModForge.Core/Build/Generator.BuildContext.cs` | 改 | `lgtmByEd` / `imgsByEd` map + `ResolveLightingRef` helper |
| `src/ModForge.Core/Build/Generator.Build.Cells.cs` | 改 | CELL 掛 LGTM/IMGS link + 組 inline `Lighting`（inherit 邏輯）|
| `src/ModForge.Core/Build/Generator.Build.cs` | 改 | orchestrator 在 `BuildCells` 前呼叫兩個新 builder |
| `src/ModForge.Core/Validate/Generator.Validate.Lighting.cs` | 新建 | LGTM/IMGS/CELL-lighting 的 guardrail |
| `src/ModForge.Core/Validate/Generator.Validate.cs` | 改 | dispatch + RegisterAll 登錄新 editorId |
| `src/ModForge.Cli/Diagnostics/Diagnostics.Records.cs` | 改 | `LgtmDiag` / `ImgsDiag` |
| `src/ModForge.Cli/Program.cs` | 改 | `lgtmdiag` / `imgsdiag` 命令派發 + usage |
| `tests/ModForge.Core.Tests/Build/LightingTests.cs` | 新建 | LGTM/IMGS/CELL build + validate 回歸 |
| `examples/lighting.json` | 新建 | 明亮室內示範 |
| `examples/spec.schema.json` | 改 | 新欄位 autocomplete |
| `docs/CODE_MAP.world.md` | 改 | 新增 Lighting 段 |
| `docs/SPEC-world.md` | 改 | 新增 Lighting 說明 |

測試指令（全程）：`dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`
單一測試：`dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~LightingTests.<name>"`

---

## Task 1: Spec types + lists + CellSpec 欄位

**Files:**
- Create: `src/ModForge.Core/Spec/Spec.Lighting.cs`
- Modify: `src/ModForge.Core/Spec/Spec.cs`
- Modify: `src/ModForge.Core/Spec/Spec.World.cs:9`（`CellSpec`）

- [ ] **Step 1: 建立 `Spec.Lighting.cs`**

```csharp
namespace ModForge;

// =====================================================================================
//  LIGHTING records: LightingTemplate (LGTM) + ImageSpace (IMGS) + inline CELL XCLL.
//
//  Skyrim interiors are dark by *authoring choice*, not engine limit — lighting is almost
//  entirely a record-layer concern. These specs make a cave/dungeon bright.
//
//  AUTHORING MODEL = template-copy + override: point `template` at a vanilla LGTM/IMGS,
//  it is DeepCopied as the base, then ONLY the fields you set here overwrite it (all
//  nullable → unset means "keep the vanilla value"). No template → engine-neutral defaults.
//
//  NOTE: distinct from ImageSpaceModifierSpec (IMAD, a screen post-process curve). This is
//  the IMGS *base* record (HDR / cinematic / tint) you attach to a CELL.
//
//  Colours reuse ColorSpec (Spec.Weather.cs) — 0..255 RGB.
// =====================================================================================

/// <summary>Six-direction hemisphere ambient light (DALC) — the flat fill that brightens a
/// dark room overall. Any omitted direction/specular keeps the template value.</summary>
public sealed class AmbientColorsSpec
{
    public ColorSpec? XPlus { get; set; }
    public ColorSpec? XMinus { get; set; }
    public ColorSpec? YPlus { get; set; }
    public ColorSpec? YMinus { get; set; }
    public ColorSpec? ZPlus { get; set; }
    public ColorSpec? ZMinus { get; set; }
    public ColorSpec? Specular { get; set; }
    public float? Scale { get; set; }
}

/// <summary>A LightingTemplate (LGTM): reusable interior lighting (ambient/directional/fog +
/// DALC). Author by copying a vanilla LGTM via <see cref="Template"/> then overriding.</summary>
public sealed class LightingTemplateSpec
{
    /// <summary>Required, unique. CELLs reference it by this editorId.</summary>
    public string EditorId { get; set; } = "";
    /// <summary>Optional vanilla LGTM to DeepCopy as base ("&lt;master&gt;:0xFORMID",
    /// e.g. Skyrim.esm:0x0300E2 = DefaultLightingTemplate).</summary>
    public string Template { get; set; } = "";

    public ColorSpec? AmbientColor { get; set; }
    public ColorSpec? DirectionalColor { get; set; }
    public int? DirectionalRotationXY { get; set; }
    public int? DirectionalRotationZ { get; set; }
    public float? DirectionalFade { get; set; }
    public ColorSpec? FogNearColor { get; set; }
    public ColorSpec? FogFarColor { get; set; }
    public float? FogNear { get; set; }
    public float? FogFar { get; set; }
    public float? FogMax { get; set; }
    public float? FogClipDistance { get; set; }
    public float? FogPower { get; set; }
    public float? LightFadeStart { get; set; }
    public float? LightFadeEnd { get; set; }
    /// <summary>DALC six-direction ambient → LGTM.DirectionalAmbientColors.</summary>
    public AmbientColorsSpec? DirectionalAmbient { get; set; }
}

/// <summary>An ImageSpace (IMGS): screen-space HDR / cinematic / tint attached to a CELL.
/// "Bright clean saturated" is mostly HDR eye-adapt + bloom + saturation. Copy a vanilla
/// IMGS via <see cref="Template"/> then bump.</summary>
public sealed class ImageSpaceSpec
{
    public string EditorId { get; set; } = "";
    public string Template { get; set; } = "";

    // Hdr
    public float? EyeAdaptSpeed { get; set; }
    public float? EyeAdaptStrength { get; set; }
    public float? BloomBlurRadius { get; set; }
    public float? BloomThreshold { get; set; }
    public float? BloomScale { get; set; }
    public float? ReceiveBloomThreshold { get; set; }
    public float? White { get; set; }
    public float? SunlightScale { get; set; }
    public float? SkyScale { get; set; }
    // Cinematic (1 = neutral, >1 boosts)
    public float? Brightness { get; set; }
    public float? Contrast { get; set; }
    public float? Saturation { get; set; }
    // Tint
    public float? TintAmount { get; set; }
    public ColorSpec? TintColor { get; set; }
}

/// <summary>Inline CELL lighting (XCLL) overrides. Fields left null are pulled from the cell's
/// LightingTemplate (the <see cref="Inherit"/> flags decide which). Note XCLL uses
/// LightFadeBegin/End (vs LGTM's Start/End).</summary>
public sealed class CellLightingSpec
{
    public ColorSpec? AmbientColor { get; set; }
    public ColorSpec? DirectionalColor { get; set; }
    public int? DirectionalRotationXY { get; set; }
    public int? DirectionalRotationZ { get; set; }
    public float? DirectionalFade { get; set; }
    public ColorSpec? FogNearColor { get; set; }
    public ColorSpec? FogFarColor { get; set; }
    public float? FogNear { get; set; }
    public float? FogFar { get; set; }
    public float? FogMax { get; set; }
    public float? FogClipDistance { get; set; }
    public float? FogPower { get; set; }
    public float? LightFadeBegin { get; set; }
    public float? LightFadeEnd { get; set; }
    /// <summary>DALC six-direction ambient → CellLighting.AmbientColors.</summary>
    public AmbientColorsSpec? DirectionalAmbient { get; set; }
    /// <summary>Field-flag names still inherited from the LightingTemplate (CellLighting.Inherit:
    /// AmbientColor / DirectionalColor / FogColor / FogNear / FogFar / DirectionalRotation /
    /// DirectionalFade / ClipDistance / FogPower / FogMax / LightFadeDistances). A field both set
    /// inline AND listed here is inherited (template wins) + warned.</summary>
    public List<string> Inherit { get; set; } = new();
}
```

- [ ] **Step 2: 在 `Spec.cs` 加兩個 List**

在 `Spec.cs:31`（`ImageSpaceModifiers` 那行）附近、緊接其後加：

```csharp
    public List<LightingTemplateSpec> LightingTemplates { get; set; } = new();   // LightingTemplate (LGTM)
    public List<ImageSpaceSpec> ImageSpaces { get; set; } = new();               // ImageSpace (IMGS) base record (≠ IMAD)
```

- [ ] **Step 3: `CellSpec` 加三欄**

把 `Spec.World.cs:9` 的 `CellSpec` 一行改成（保留既有四欄，加三欄；皆 optional）：

```csharp
public sealed class CellSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Template { get; set; } = ""; public string EncounterZone { get; set; } = ""; public string LightingTemplate { get; set; } = ""; public string ImageSpace { get; set; } = ""; public CellLightingSpec? Lighting { get; set; } } // lightingTemplate/imageSpace: in-spec editorId OR vanilla <master>:0xFORMID; lighting: inline XCLL overrides
```

- [ ] **Step 4: 編譯確認**

Run: `dotnet build src/ModForge.Core/ModForge.Core.csproj`
Expected: Build succeeded（純新增型別 + optional 欄位，無行為改變）。

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/Spec/Spec.Lighting.cs src/ModForge.Core/Spec/Spec.cs src/ModForge.Core/Spec/Spec.World.cs
git commit -m "feat(lighting): spec types — LightingTemplateSpec/ImageSpaceSpec/CellLightingSpec + CellSpec fields" \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: LGTM builder + orchestrator hookup

**Files:**
- Create: `src/ModForge.Core/Build/Generator.Build.Lighting.cs`
- Modify: `src/ModForge.Core/Build/Generator.BuildContext.cs`（加 `lgtmByEd`）
- Modify: `src/ModForge.Core/Build/Generator.Build.cs`（orchestrator call）
- Test: `tests/ModForge.Core.Tests/Build/LightingTests.cs`

- [ ] **Step 1: 寫失敗測試**

建 `tests/ModForge.Core.Tests/Build/LightingTests.cs`：

```csharp
using ModForge;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Tests;

// Regression tests for the lighting pipeline (LGTM / IMGS / CELL XCLL). Pure object-in/
// object-out: build a spec in code, assert on the in-memory mod. LGTM/IMGS template-copy
// tests use a vanilla ref, so they need Skyrim.esm — gated like WordWallTests; the no-template
// tests run everywhere.
public class LightingTests
{
    private static readonly ModKey Out = ModKey.FromNameAndExtension("Test.esp");
    private static BuildResult Build(ModSpec spec) => Generator.Build(spec, Out);
    private static T Single<T>(BuildResult r) where T : class, IMajorRecordGetter =>
        r.Mod.EnumerateMajorRecords<T>().Single();

    [Fact]
    public void Lgtm_NoTemplate_WritesAuthoredFieldsAndDalc()
    {
        var spec = new ModSpec
        {
            LightingTemplates =
            {
                new LightingTemplateSpec
                {
                    EditorId = "MF_BrightCaveLGTM",
                    AmbientColor = new ColorSpec { R = 180, G = 185, B = 200 },
                    DirectionalColor = new ColorSpec { R = 220, G = 220, B = 210 },
                    FogNear = 0f, FogFar = 8192f,
                    DirectionalAmbient = new AmbientColorsSpec
                    {
                        Scale = 1.0f,
                        ZPlus = new ColorSpec { R = 200, G = 205, B = 215 },
                    },
                },
            },
        };

        var lt = Single<ILightingTemplateGetter>(Build(spec));
        Assert.Equal("MF_BrightCaveLGTM", lt.EditorID);
        Assert.Equal(180, lt.AmbientColor.R);
        Assert.Equal(210, lt.DirectionalColor.B);
        Assert.Equal(8192f, lt.FogFar);
        Assert.Equal(1.0f, lt.DirectionalAmbientColors!.Scale);
        Assert.Equal(200, lt.DirectionalAmbientColors!.DirectionalZPlus.R);
    }
}
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~LightingTests.Lgtm_NoTemplate_WritesAuthoredFieldsAndDalc"`
Expected: FAIL（`Single<ILightingTemplateGetter>` 找不到記錄，因為還沒 builder）。

- [ ] **Step 3: 在 `Generator.BuildContext.cs` 加 map**

在第 34 行 `cellsByEd` 宣告之後加：

```csharp
        // Custom LGTM/IMGS built in pass 1 (before cells), so a CELL can resolve them by editorId.
        private readonly Dictionary<string, LightingTemplate> lgtmByEd = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ImageSpace> imgsByEd = new(StringComparer.OrdinalIgnoreCase);
```

- [ ] **Step 4: 建 `Generator.Build.Lighting.cs`（LGTM 部分）**

```csharp
namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  LightingTemplate (LGTM) + ImageSpace (IMGS) build.
    //
    //  Both follow the template-copy + override model: if `template` resolves to a vanilla
    //  record, DeepCopy it as the base, then overwrite ONLY the fields the spec sets. No
    //  template → a fresh record with engine defaults (a fresh LGTM is a valid, if dim,
    //  record). Built in pass 1 BEFORE BuildCells; the editorId→record map lets a CELL
    //  resolve a custom one by editorId in pass 1 (vanilla refs go through TryResolveTemplate).
    //
    //  DALC mapping (verified against Skyrim.esm): LGTM's directional ambient is
    //  DirectionalAmbientColors (its other AmbientColors field is legacy/zero); CELL XCLL's
    //  is AmbientColors. FillAmbientColors writes whichever Mutagen AmbientColors we hand it.
    // -------------------------------------------------------------------------------
    private sealed partial class BuildContext
    {
        public void BuildLightingTemplates()
        {
            foreach (var s in spec.LightingTemplates)
            {
                var lt = mod.LightingTemplates.AddNew();
                if (!string.IsNullOrWhiteSpace(s.Template))
                {
                    if (TryResolveTemplate<ILightingTemplateGetter>(s.Template, out var tmpl) && tmpl is not null)
                        lt.DeepCopyIn(tmpl);   // FormKey preserved (EditorID set below). If the 1-arg overload doesn't resolve, use: lt.DeepCopyIn(tmpl, out _, null);
                    else Warn($"  ! lightingTemplate '{s.EditorId}' template '{s.Template}' unresolved — using engine defaults");
                }
                lt.EditorID = s.EditorId;

                if (s.AmbientColor is { } ac) lt.AmbientColor = ToColor(ac);
                if (s.DirectionalColor is { } dc) lt.DirectionalColor = ToColor(dc);
                if (s.DirectionalRotationXY is { } rxy) lt.DirectionalRotationXY = rxy;
                if (s.DirectionalRotationZ is { } rz) lt.DirectionalRotationZ = rz;
                if (s.DirectionalFade is { } df) lt.DirectionalFade = df;
                if (s.FogNearColor is { } fnc) lt.FogNearColor = ToColor(fnc);
                if (s.FogFarColor is { } ffc) lt.FogFarColor = ToColor(ffc);
                if (s.FogNear is { } fn) lt.FogNear = fn;
                if (s.FogFar is { } ff) lt.FogFar = ff;
                if (s.FogMax is { } fm) lt.FogMax = fm;
                if (s.FogClipDistance is { } fcd) lt.FogClipDistance = fcd;
                if (s.FogPower is { } fp) lt.FogPower = fp;
                if (s.LightFadeStart is { } lfs) lt.LightFadeStartDistance = lfs;
                if (s.LightFadeEnd is { } lfe) lt.LightFadeEndDistance = lfe;
                if (s.DirectionalAmbient is { } da)
                    FillAmbientColors(lt.DirectionalAmbientColors ??= new(), da);

                if (!string.IsNullOrEmpty(s.EditorId)) lgtmByEd[s.EditorId] = lt;
            }
        }

        // Overwrite only the AmbientColors sub-fields the spec sets (DALC: 6 directions + specular + scale).
        private static void FillAmbientColors(Mutagen.Bethesda.Skyrim.AmbientColors dst, AmbientColorsSpec src)
        {
            if (src.XPlus is { } v) dst.DirectionalXPlus = ToColor(v);
            if (src.XMinus is { } v) dst.DirectionalXMinus = ToColor(v);
            if (src.YPlus is { } v) dst.DirectionalYPlus = ToColor(v);
            if (src.YMinus is { } v) dst.DirectionalYMinus = ToColor(v);
            if (src.ZPlus is { } v) dst.DirectionalZPlus = ToColor(v);
            if (src.ZMinus is { } v) dst.DirectionalZMinus = ToColor(v);
            if (src.Specular is { } v) dst.Specular = ToColor(v);
            if (src.Scale is { } sc) dst.Scale = sc;
        }
    }
}
```

注意：`DeepCopyIn` 是 Mutagen 的 in-place copy（既有 cell-env copy 註解提及）。`ToColor`/`Clamp255` 在 `Generator.Build.Weather.cs`（同 `Generator` partial class，免 using）。`AmbientColors` 型別來自 `Mutagen.Bethesda.Skyrim`（檔頭無 using，故全名引用以免和 spec class 撞名）。

- [ ] **Step 5: orchestrator 接線**

在 `Generator.Build.cs:62`（`ctx.BuildLights();`）之後、`ctx.BuildCells();`（第 69 行）**之前**加一行（放在 BuildLights 後即可，只要在 BuildCells 前）：

```csharp
        ctx.BuildLightingTemplates();              // LightingTemplate (LGTM) — before cells so a CELL resolves it by editorId
```

- [ ] **Step 6: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~LightingTests.Lgtm_NoTemplate_WritesAuthoredFieldsAndDalc"`
Expected: PASS。

- [ ] **Step 7: Commit**

```bash
git add src/ModForge.Core/Build/Generator.Build.Lighting.cs src/ModForge.Core/Build/Generator.BuildContext.cs src/ModForge.Core/Build/Generator.Build.cs tests/ModForge.Core.Tests/Build/LightingTests.cs
git commit -m "feat(lighting): LGTM builder (template-copy + override, DALC via DirectionalAmbientColors)" \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: IMGS builder

**Files:**
- Modify: `src/ModForge.Core/Build/Generator.Build.Lighting.cs`
- Modify: `src/ModForge.Core/Build/Generator.Build.cs`
- Test: `tests/ModForge.Core.Tests/Build/LightingTests.cs`

- [ ] **Step 1: 寫失敗測試**（append 到 `LightingTests`）

```csharp
    [Fact]
    public void Imgs_NoTemplate_WritesHdrCinematicTint()
    {
        var spec = new ModSpec
        {
            ImageSpaces =
            {
                new ImageSpaceSpec
                {
                    EditorId = "MF_BrightIMGS",
                    Brightness = 1.4f, Saturation = 1.2f, Contrast = 1.0f,
                    BloomScale = 0.8f, SunlightScale = 1.3f,
                    TintAmount = 0.1f, TintColor = new ColorSpec { R = 255, G = 240, B = 210 },
                },
            },
        };

        var img = Single<IImageSpaceGetter>(Build(spec));
        Assert.Equal("MF_BrightIMGS", img.EditorID);
        Assert.Equal(1.4f, img.Cinematic!.Brightness);
        Assert.Equal(1.2f, img.Cinematic!.Saturation);
        Assert.Equal(0.8f, img.Hdr!.BloomScale);
        Assert.Equal(1.3f, img.Hdr!.SunlightScale);
        Assert.Equal(0.1f, img.Tint!.Amount);
        Assert.Equal(255, img.Tint!.Color.R);
    }
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~LightingTests.Imgs_NoTemplate_WritesHdrCinematicTint"`
Expected: FAIL（無 IMGS builder）。

- [ ] **Step 3: 在 `Generator.Build.Lighting.cs` 的 `BuildContext` 內加 `BuildImageSpaces`**

```csharp
        public void BuildImageSpaces()
        {
            foreach (var s in spec.ImageSpaces)
            {
                var img = mod.ImageSpaces.AddNew();
                if (!string.IsNullOrWhiteSpace(s.Template))
                {
                    if (TryResolveTemplate<IImageSpaceGetter>(s.Template, out var tmpl) && tmpl is not null)
                        img.DeepCopyIn(tmpl);   // FormKey preserved (EditorID set below). Fallback: img.DeepCopyIn(tmpl, out _, null);
                    else Warn($"  ! imageSpace '{s.EditorId}' template '{s.Template}' unresolved — using engine defaults");
                }
                img.EditorID = s.EditorId;

                var hdr = img.Hdr ??= new();
                if (s.EyeAdaptSpeed is { } v) hdr.EyeAdaptSpeed = v;
                if (s.EyeAdaptStrength is { } v) hdr.EyeAdaptStrength = v;
                if (s.BloomBlurRadius is { } v) hdr.BloomBlurRadius = v;
                if (s.BloomThreshold is { } v) hdr.BloomThreshold = v;
                if (s.BloomScale is { } v) hdr.BloomScale = v;
                if (s.ReceiveBloomThreshold is { } v) hdr.ReceiveBloomThreshold = v;
                if (s.White is { } v) hdr.White = v;
                if (s.SunlightScale is { } v) hdr.SunlightScale = v;
                if (s.SkyScale is { } v) hdr.SkyScale = v;

                var cin = img.Cinematic ??= new();
                if (s.Brightness is { } v) cin.Brightness = v;
                if (s.Contrast is { } v) cin.Contrast = v;
                if (s.Saturation is { } v) cin.Saturation = v;

                var tint = img.Tint ??= new();
                if (s.TintAmount is { } v) tint.Amount = v;
                if (s.TintColor is { } c) tint.Color = ToColor(c);

                if (!string.IsNullOrEmpty(s.EditorId)) imgsByEd[s.EditorId] = img;
            }
        }
```

- [ ] **Step 4: orchestrator 接線**

在 `Generator.Build.cs` 剛加的 `ctx.BuildLightingTemplates();` 那行之後加：

```csharp
        ctx.BuildImageSpaces();                    // ImageSpace (IMGS) base record — before cells (resolve by editorId)
```

- [ ] **Step 5: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~LightingTests.Imgs_NoTemplate_WritesHdrCinematicTint"`
Expected: PASS。

- [ ] **Step 6: Commit**

```bash
git add src/ModForge.Core/Build/Generator.Build.Lighting.cs src/ModForge.Core/Build/Generator.Build.cs tests/ModForge.Core.Tests/Build/LightingTests.cs
git commit -m "feat(lighting): IMGS builder — HDR/cinematic/tint, template-copy + override" \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: CELL 接線（lightingTemplate / imageSpace / inline XCLL）

**Files:**
- Modify: `src/ModForge.Core/Build/Generator.BuildContext.cs`（`ResolveLightingRef` helper）
- Modify: `src/ModForge.Core/Build/Generator.Build.Cells.cs`
- Test: `tests/ModForge.Core.Tests/Build/LightingTests.cs`

- [ ] **Step 1: 寫失敗測試（兩個）**（append 到 `LightingTests`）

```csharp
    // CELL points at an in-spec custom LGTM with no inline lighting → auto Inherits=ALL flags.
    [Fact]
    public void Cell_WithCustomLgtm_InheritsAll()
    {
        var spec = new ModSpec
        {
            LightingTemplates = { new LightingTemplateSpec { EditorId = "MF_BrightLGTM" } },
            Cells = { new CellSpec { EditorId = "MF_BrightRoom", LightingTemplate = "MF_BrightLGTM" } },
        };

        var r = Build(spec);
        var lgtm = r.Mod.EnumerateMajorRecords<ILightingTemplateGetter>().Single();
        var cell = r.Mod.EnumerateMajorRecords<ICellGetter>().Single(c => c.EditorID == "MF_BrightRoom");
        Assert.Equal(lgtm.FormKey, cell.LightingTemplate.FormKey);
        Assert.NotNull(cell.Lighting);
        // every inherit flag set → fully driven by the template
        foreach (CellLighting.Inherit f in Enum.GetValues<CellLighting.Inherit>())
            Assert.True(cell.Lighting!.Inherits.HasFlag(f), $"missing inherit flag {f}");
    }

    // Inline lighting: fields set inline are used; flags listed in `inherit` come from the template.
    [Fact]
    public void Cell_InlineLighting_SetsFieldsAndInheritSubset()
    {
        var spec = new ModSpec
        {
            LightingTemplates = { new LightingTemplateSpec { EditorId = "MF_BaseLGTM" } },
            Cells =
            {
                new CellSpec
                {
                    EditorId = "MF_TunedRoom",
                    LightingTemplate = "MF_BaseLGTM",
                    Lighting = new CellLightingSpec
                    {
                        AmbientColor = new ColorSpec { R = 160, G = 165, B = 175 },
                        FogFar = 6000f,
                        DirectionalAmbient = new AmbientColorsSpec { Scale = 1.0f },
                        Inherit = { "FogColor", "DirectionalColor" },
                    },
                },
            },
        };

        var cell = Build(spec).Mod.EnumerateMajorRecords<ICellGetter>().Single(c => c.EditorID == "MF_TunedRoom");
        Assert.Equal(160, cell.Lighting!.AmbientColor.R);
        Assert.Equal(6000f, cell.Lighting!.FogFar);
        Assert.Equal(1.0f, cell.Lighting!.AmbientColors!.Scale);
        Assert.True(cell.Lighting!.Inherits.HasFlag(CellLighting.Inherit.FogColor));
        Assert.True(cell.Lighting!.Inherits.HasFlag(CellLighting.Inherit.DirectionalColor));
        Assert.False(cell.Lighting!.Inherits.HasFlag(CellLighting.Inherit.AmbientColor));
    }
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~LightingTests.Cell_"`
Expected: FAIL（cell 沒掛 LightingTemplate、Lighting 為 null 或無 inherit 邏輯）。

- [ ] **Step 3: 在 `Generator.BuildContext.cs` 加解析 helper**

在 `Generator.BuildContext.cs` 的 `BuildContext` 內（緊接 `TryResolveTemplate` 風格的工具區，或檔末 `Finish` 前）加：

```csharp
        // Resolve a CELL's lightingTemplate / imageSpace ref: a custom in-spec record (by editorId)
        // wins, else a vanilla "<master>:0xFORMID". Runs in pass 1 (formKeyByEd not built yet), so we
        // use the custom maps + the external link cache directly. Returns false (caller warns) if neither.
        private bool ResolveLightingRef(string refStr, out FormKey fk)
        {
            fk = default;
            if (string.IsNullOrWhiteSpace(refStr)) return false;
            if (lgtmByEd.TryGetValue(refStr, out var lt)) { fk = lt.FormKey; return true; }
            if (imgsByEd.TryGetValue(refStr, out var img)) { fk = img.FormKey; return true; }
            if (TryExternalRef(refStr, out var ext)) { fk = ext; return true; }
            return false;
        }
```

注意：`TryExternalRef` 已存在（Validate/BuildContext 共用，解析 `<master>:0xFORMID`）。LGTM 與 IMGS 共用此 helper：cell 的 `LightingTemplate` 欄一定指 LGTM、`ImageSpace` 欄一定指 IMGS，呼叫端各查一次，map 不會混（editorId 命名空間全域唯一，validate 保證）。

- [ ] **Step 4: 改 `Generator.Build.Cells.cs` 的 `BuildCells`**

在 `Generator.Build.Cells.cs` 的 `BuildCells()` 迴圈內，把 `cell.Flags |= Cell.Flag.IsInteriorCell;`（第 86 行）之後、`if (!string.IsNullOrEmpty(c.Name))`（第 87 行）之前插入：

```csharp
                // Custom/vanilla LightingTemplate (LGTM) + ImageSpace (IMGS) links + inline XCLL.
                if (!string.IsNullOrWhiteSpace(c.LightingTemplate))
                {
                    if (ResolveLightingRef(c.LightingTemplate, out var ltFk)) cell.LightingTemplate.SetTo(ltFk);
                    else Warn($"  ! cell '{c.EditorId}' lightingTemplate '{c.LightingTemplate}' unresolved");
                }
                if (!string.IsNullOrWhiteSpace(c.ImageSpace))
                {
                    if (ResolveLightingRef(c.ImageSpace, out var imgFk)) cell.ImageSpace.SetTo(imgFk);
                    else Warn($"  ! cell '{c.EditorId}' imageSpace '{c.ImageSpace}' unresolved");
                }
                ApplyCellLighting(cell, c);
```

然後在 `Generator.Build.Cells.cs` 的 `BuildContext` 內（`BuildCells` 之後）加新方法：

```csharp
        // An interior CELL MUST carry an XCLL (Lighting) or it renders pitch black. The Inherit flags
        // decide which fields come from the LightingTemplate vs the inline XCLL. Rules:
        //   * no inline `lighting` → keep whatever CopyCellEnv set, but if a LightingTemplate is present
        //     and there's no Lighting yet, create one that inherits ALL flags (fully template-driven).
        //   * inline `lighting` → write the authored fields; Inherits = the flags listed in `inherit`
        //     (those come from the template). A field set inline AND listed in `inherit` is inherited
        //     (template wins) + warned.
        private void ApplyCellLighting(Cell cell, CellSpec c)
        {
            if (c.Lighting is null)
            {
                if (!string.IsNullOrWhiteSpace(c.LightingTemplate))
                    cell.Lighting ??= new CellLighting { Inherits = AllInheritFlags() };
                return;
            }

            var s = c.Lighting;
            var lz = cell.Lighting ??= new CellLighting();

            CellLighting.Inherit inh = 0;
            foreach (var f in s.Inherit)
                if (Enum.TryParse<CellLighting.Inherit>(f, ignoreCase: true, out var fl)) inh |= fl;
                else Warn($"  ! cell '{c.EditorId}' invalid inherit flag '{f}'");
            lz.Inherits = inh;

            // helper: set inline value only if NOT inherited; warn on conflict.
            void Field(CellLighting.Inherit flag, bool authored, Action set)
            {
                if (!authored) return;
                if (inh.HasFlag(flag)) Warn($"  ! cell '{c.EditorId}' field for {flag} set inline but also inherited — template wins");
                else set();
            }

            Field(CellLighting.Inherit.AmbientColor, s.AmbientColor is not null, () => lz.AmbientColor = ToColor(s.AmbientColor!));
            Field(CellLighting.Inherit.DirectionalColor, s.DirectionalColor is not null, () => lz.DirectionalColor = ToColor(s.DirectionalColor!));
            Field(CellLighting.Inherit.DirectionalRotation, s.DirectionalRotationXY is not null, () => lz.DirectionalRotationXY = s.DirectionalRotationXY!.Value);
            Field(CellLighting.Inherit.DirectionalRotation, s.DirectionalRotationZ is not null, () => lz.DirectionalRotationZ = s.DirectionalRotationZ!.Value);
            Field(CellLighting.Inherit.DirectionalFade, s.DirectionalFade is not null, () => lz.DirectionalFade = s.DirectionalFade!.Value);
            Field(CellLighting.Inherit.FogColor, s.FogNearColor is not null, () => lz.FogNearColor = ToColor(s.FogNearColor!));
            Field(CellLighting.Inherit.FogColor, s.FogFarColor is not null, () => lz.FogFarColor = ToColor(s.FogFarColor!));
            Field(CellLighting.Inherit.FogNear, s.FogNear is not null, () => lz.FogNear = s.FogNear!.Value);
            Field(CellLighting.Inherit.FogFar, s.FogFar is not null, () => lz.FogFar = s.FogFar!.Value);
            Field(CellLighting.Inherit.FogMax, s.FogMax is not null, () => lz.FogMax = s.FogMax!.Value);
            Field(CellLighting.Inherit.ClipDistance, s.FogClipDistance is not null, () => lz.FogClipDistance = s.FogClipDistance!.Value);
            Field(CellLighting.Inherit.FogPower, s.FogPower is not null, () => lz.FogPower = s.FogPower!.Value);
            Field(CellLighting.Inherit.LightFadeDistances, s.LightFadeBegin is not null, () => lz.LightFadeBegin = s.LightFadeBegin!.Value);
            Field(CellLighting.Inherit.LightFadeDistances, s.LightFadeEnd is not null, () => lz.LightFadeEnd = s.LightFadeEnd!.Value);
            if (s.DirectionalAmbient is { } da) FillAmbientColors(lz.AmbientColors ??= new(), da);
        }

        // All CellLighting.Inherit flags OR'd — a cell with a template but no inline overrides
        // inherits everything (matches vanilla interior cells).
        private static CellLighting.Inherit AllInheritFlags()
        {
            CellLighting.Inherit all = 0;
            foreach (CellLighting.Inherit f in Enum.GetValues<CellLighting.Inherit>()) all |= f;
            return all;
        }
```

注意：`CellLighting` / `CellLighting.Inherit` / `AmbientColors` 來自 `Mutagen.Bethesda.Skyrim`（`Generator.Build.Cells.cs` 檔頭已在 `namespace ModForge` 下用這些型別——`Cell`/`CellGrid` 已用，故 `CellLighting` 同 namespace 可直接用）。`FillAmbientColors` 是 Task 2 在同 partial class 加的 private static，跨檔可見。

- [ ] **Step 5: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~LightingTests.Cell_"`
Expected: PASS（兩個）。

- [ ] **Step 6: Commit**

```bash
git add src/ModForge.Core/Build/Generator.BuildContext.cs src/ModForge.Core/Build/Generator.Build.Cells.cs tests/ModForge.Core.Tests/Build/LightingTests.cs
git commit -m "feat(lighting): CELL wiring — lightingTemplate/imageSpace links + inline XCLL with inherit flags" \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Validate guardrails

**Files:**
- Create: `src/ModForge.Core/Validate/Generator.Validate.Lighting.cs`
- Modify: `src/ModForge.Core/Validate/Generator.Validate.cs`（dispatch + RegisterAll）
- Test: `tests/ModForge.Core.Tests/Build/LightingTests.cs`

- [ ] **Step 1: 寫失敗測試**（append 到 `LightingTests`）

```csharp
    [Fact]
    public void Validate_FlagsBadColorDuplicateAndRefAndInherit()
    {
        var spec = new ModSpec
        {
            LightingTemplates =
            {
                new LightingTemplateSpec { EditorId = "MF_DupLGTM", AmbientColor = new ColorSpec { R = 300, G = 0, B = 0 } },
                new LightingTemplateSpec { EditorId = "MF_DupLGTM" },   // duplicate editorId
            },
            Cells =
            {
                new CellSpec
                {
                    EditorId = "MF_BadCell",
                    LightingTemplate = "MF_DoesNotExist",   // unresolved ref
                    Lighting = new CellLightingSpec { Inherit = { "NotARealFlag" } },
                },
            },
        };

        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("MF_DupLGTM") && p.Contains("duplicate"));
        Assert.Contains(problems, p => p.Contains("MF_DupLGTM") && p.Contains("ambientColor"));
        Assert.Contains(problems, p => p.Contains("MF_BadCell") && p.Contains("lightingTemplate"));
        Assert.Contains(problems, p => p.Contains("MF_BadCell") && p.Contains("NotARealFlag"));
    }
```

注意：`duplicate` 文字來自既有共用 `Reg(...)` pass（見下 Step 3 登錄）。其餘三條由新 `ValidateLighting` 產生。

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~LightingTests.Validate_FlagsBadColorDuplicateAndRefAndInherit"`
Expected: FAIL。

- [ ] **Step 3: 在 `Generator.Validate.cs` 的 `RegisterAll` 登錄新 editorId**

在 `RegisterAll` 內（任一 `foreach ... Reg(...)` 群組附近，例如 `spec.Cells` 迴圈之前）加：

```csharp
            foreach (var lt in spec.LightingTemplates) Reg(lt.EditorId, "lightingTemplate");
            foreach (var img in spec.ImageSpaces) Reg(img.EditorId, "imageSpace");
```

（`Reg` 已會對重複 editorId 產生 `duplicate editorId '...'` 問題，故測試的 duplicate 條由此滿足。）

- [ ] **Step 4: 在 `Generator.Validate.cs` 的 `Validate(...)` dispatch 加一行**

在 `ValidateLights(spec, ctx.Problems);`（第 31 行）之後加：

```csharp
        ValidateLighting(spec, ctx.Problems, ctx.Ids);
```

- [ ] **Step 5: 建 `Generator.Validate.Lighting.cs`**

```csharp
namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Validate — LIGHTING (LGTM / IMGS / inline CELL XCLL) guardrails.
    //
    //  editorId presence/uniqueness is the shared Reg(...) pass; here: colour components
    //  0..255 (LGTM/IMGS/inline colours), a cell's lightingTemplate/imageSpace ref resolves
    //  (in-spec editorId in `ids`, OR a vanilla <master>:0xFORMID), and inline `inherit`
    //  flag names parse. `ids` is the set of all in-spec editorIds (forward refs allowed).
    // -------------------------------------------------------------------------------
    private static readonly string InheritFlagList =
        "AmbientColor|DirectionalColor|FogColor|FogNear|FogFar|DirectionalRotation|DirectionalFade|ClipDistance|FogPower|FogMax|LightFadeDistances";

    private static void ValidateLighting(ModSpec spec, List<string> problems, HashSet<string> ids)
    {
        void CheckColor(string owner, string field, ColorSpec? c)
        {
            if (c is null) return;
            foreach (var (v, n) in new[] { (c.R, "r"), (c.G, "g"), (c.B, "b") })
                if (v < 0 || v > 255) problems.Add($"{owner} {field}.{n} = {v} out of range 0..255");
        }
        void CheckAmbient(string owner, string field, AmbientColorsSpec? a)
        {
            if (a is null) return;
            CheckColor(owner, $"{field}.xPlus", a.XPlus); CheckColor(owner, $"{field}.xMinus", a.XMinus);
            CheckColor(owner, $"{field}.yPlus", a.YPlus); CheckColor(owner, $"{field}.yMinus", a.YMinus);
            CheckColor(owner, $"{field}.zPlus", a.ZPlus); CheckColor(owner, $"{field}.zMinus", a.ZMinus);
            CheckColor(owner, $"{field}.specular", a.Specular);
        }

        foreach (var s in spec.LightingTemplates)
        {
            var o = $"lightingTemplate '{s.EditorId}'";
            if (!string.IsNullOrWhiteSpace(s.Template) && !TryExternalRef(s.Template, out _))
                problems.Add($"{o} template '{s.Template}' must be an external <master>:0xFORMID LGTM ref");
            CheckColor(o, "ambientColor", s.AmbientColor); CheckColor(o, "directionalColor", s.DirectionalColor);
            CheckColor(o, "fogNearColor", s.FogNearColor); CheckColor(o, "fogFarColor", s.FogFarColor);
            CheckAmbient(o, "directionalAmbient", s.DirectionalAmbient);
        }

        foreach (var s in spec.ImageSpaces)
        {
            var o = $"imageSpace '{s.EditorId}'";
            if (!string.IsNullOrWhiteSpace(s.Template) && !TryExternalRef(s.Template, out _))
                problems.Add($"{o} template '{s.Template}' must be an external <master>:0xFORMID IMGS ref");
            CheckColor(o, "tintColor", s.TintColor);
        }

        bool Resolvable(string r) => ids.Contains(r) || TryExternalRef(r, out _);
        foreach (var c in spec.Cells)
        {
            var o = $"cell '{c.EditorId}'";
            if (!string.IsNullOrWhiteSpace(c.LightingTemplate) && !Resolvable(c.LightingTemplate))
                problems.Add($"{o} lightingTemplate '{c.LightingTemplate}' unresolved (need in-spec editorId or <master>:0xFORMID)");
            if (!string.IsNullOrWhiteSpace(c.ImageSpace) && !Resolvable(c.ImageSpace))
                problems.Add($"{o} imageSpace '{c.ImageSpace}' unresolved (need in-spec editorId or <master>:0xFORMID)");
            if (c.Lighting is { } cl)
            {
                CheckColor(o, "lighting.ambientColor", cl.AmbientColor); CheckColor(o, "lighting.directionalColor", cl.DirectionalColor);
                CheckColor(o, "lighting.fogNearColor", cl.FogNearColor); CheckColor(o, "lighting.fogFarColor", cl.FogFarColor);
                CheckAmbient(o, "lighting.directionalAmbient", cl.DirectionalAmbient);
                foreach (var f in cl.Inherit)
                    if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.CellLighting.Inherit>(f, true, out _))
                        problems.Add($"{o} invalid inherit flag '{f}' ({InheritFlagList})");
            }
        }
    }
}
```

- [ ] **Step 6: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~LightingTests.Validate_FlagsBadColorDuplicateAndRefAndInherit"`
Expected: PASS。

- [ ] **Step 7: 跑全套確認無回歸**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`
Expected: 全綠（WordWallTests 的環境性失敗除外——若無 Skyrim.esm 則 259/260；本機有 Skyrim.esm 應全綠）。

- [ ] **Step 8: Commit**

```bash
git add src/ModForge.Core/Validate/Generator.Validate.Lighting.cs src/ModForge.Core/Validate/Generator.Validate.cs tests/ModForge.Core.Tests/Build/LightingTests.cs
git commit -m "feat(lighting): validate — color range / template ref / cell ref / inherit flag guardrails" \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: lgtmdiag + imgsdiag 診斷

**Files:**
- Modify: `src/ModForge.Cli/Diagnostics/Diagnostics.Records.cs`
- Modify: `src/ModForge.Cli/Program.cs`（派發 + usage）

- [ ] **Step 1: 在 `Diagnostics.Records.cs` 加兩個 diag 方法**

在 `LightDiag` 方法（第 98 行結束）之後加：

```csharp
    // Diagnostic: dump a LightingTemplate's (LGTM) ambient/directional/fog colors + DALC, by FormID
    // or (no id) list every LGTM in the plugin. Used to verify a built bright template, or to read a
    // vanilla LGTM's values before copying it as a `template`.
    private static int LgtmDiag(string inPath, string? formIdHex)
    {
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        uint? target = formIdHex is null ? null
            : Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        foreach (var lt in mod.EnumerateMajorRecords<ILightingTemplateGetter>())
        {
            if (target is { } t && lt.FormKey.ID != t) continue;
            Console.WriteLine($"0x{lt.FormKey.ID:X6}  {lt.EditorID}");
            Console.WriteLine($"  ambient=({lt.AmbientColor.R},{lt.AmbientColor.G},{lt.AmbientColor.B})  "
                + $"directional=({lt.DirectionalColor.R},{lt.DirectionalColor.G},{lt.DirectionalColor.B})");
            Console.WriteLine($"  fog near={lt.FogNear} far={lt.FogFar} max={lt.FogMax} clip={lt.FogClipDistance} power={lt.FogPower}  "
                + $"fogNearColor=({lt.FogNearColor.R},{lt.FogNearColor.G},{lt.FogNearColor.B})");
            var d = lt.DirectionalAmbientColors;
            if (d is not null)
                Console.WriteLine($"  DALC scale={d.Scale} Z+=({d.DirectionalZPlus.R},{d.DirectionalZPlus.G},{d.DirectionalZPlus.B}) "
                    + $"Z-=({d.DirectionalZMinus.R},{d.DirectionalZMinus.G},{d.DirectionalZMinus.B})");
            if (target is not null) return 0;
        }
        if (target is not null) Console.WriteLine($"0x{target:X6} not a LightingTemplate in {Path.GetFileName(inPath)}");
        return 0;
    }

    // Diagnostic: dump an ImageSpace's (IMGS) HDR / cinematic / tint, by FormID or (no id) list all.
    private static int ImgsDiag(string inPath, string? formIdHex)
    {
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        uint? target = formIdHex is null ? null
            : Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        foreach (var img in mod.EnumerateMajorRecords<IImageSpaceGetter>())
        {
            if (target is { } t && img.FormKey.ID != t) continue;
            Console.WriteLine($"0x{img.FormKey.ID:X6}  {img.EditorID}");
            if (img.Cinematic is { } c)
                Console.WriteLine($"  cinematic: brightness={c.Brightness} contrast={c.Contrast} saturation={c.Saturation}");
            if (img.Hdr is { } h)
                Console.WriteLine($"  hdr: bloomScale={h.BloomScale} bloomThresh={h.BloomThreshold} eyeAdapt={h.EyeAdaptSpeed} "
                    + $"sunlight={h.SunlightScale} sky={h.SkyScale} white={h.White}");
            if (img.Tint is { } ti)
                Console.WriteLine($"  tint: amount={ti.Amount} color=({ti.Color.R},{ti.Color.G},{ti.Color.B})");
            if (target is not null) return 0;
        }
        if (target is not null) Console.WriteLine($"0x{target:X6} not an ImageSpace in {Path.GetFileName(inPath)}");
        return 0;
    }
```

- [ ] **Step 2: 在 `Program.cs` 派發**

在 `Program.cs:33`（`case "lightdiag"...`）之後加兩行：

```csharp
                case "lgtmdiag" when args.Length is 2 or 3: return LgtmDiag(args[1], args.Length == 3 ? args[2] : null);
                case "imgsdiag" when args.Length is 2 or 3: return ImgsDiag(args[1], args.Length == 3 ? args[2] : null);
```

- [ ] **Step 3: 在 `Program.cs` usage 文字加說明**

在 `Program.cs:81`（lightdiag usage 那行）之後加：

```csharp
        "  lgtmdiag <in.esp> [0xFORMID]                a LightingTemplate's ambient/directional/fog/DALC (no id: list all)\n" +
        "  imgsdiag <in.esp> [0xFORMID]                an ImageSpace's HDR/cinematic/tint (no id: list all)\n" +
```

- [ ] **Step 4: 編譯 + 手測（對 vanilla LGTM/IMGS）**

```bash
dotnet build src/ModForge.Cli/ModForge.Cli.csproj
SK="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data/Skyrim.esm"
dotnet run --project src/ModForge.Cli -- lgtmdiag "$SK" 0x0300E2   # DefaultLightingTemplate
```
Expected: 印出 `DefaultLightingTemplate` 的 ambient/directional/fog + DALC scale=1（與設計地基一致）。

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Cli/Diagnostics/Diagnostics.Records.cs src/ModForge.Cli/Program.cs
git commit -m "feat(cli): lgtmdiag + imgsdiag — dump LGTM/IMGS lighting fields from an esp" \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: 示範 example + schema + 實機封裝

**Files:**
- Create: `examples/lighting.json`
- Modify: `examples/spec.schema.json`

- [ ] **Step 1: 建 `examples/lighting.json`**

一個明亮室內：自訂 bright LGTM（抄 DefaultLightingTemplate 再調亮 + DALC 提亮）+ bright IMGS + 一個 cell 掛兩者 + 地板 static + 一盞房間填充燈。`mod` 區塊欄位名沿用其他 example（先 `cat examples/lights.json` 對齊頂層結構）。

```json
{
  "name": "ModForgeBrightInterior",
  "author": "ModForge",
  "esl": true,
  "lightingTemplates": [
    {
      "editorId": "MF_BrightCaveLGTM",
      "template": "Skyrim.esm:0x0300E2",
      "ambientColor": { "r": 150, "g": 155, "b": 170 },
      "directionalColor": { "r": 210, "g": 210, "b": 200 },
      "fogNear": 0, "fogFar": 8192,
      "directionalAmbient": {
        "scale": 1.0,
        "zPlus": { "r": 200, "g": 205, "b": 215 },
        "zMinus": { "r": 120, "g": 122, "b": 130 },
        "xPlus": { "r": 170, "g": 172, "b": 180 }, "xMinus": { "r": 170, "g": 172, "b": 180 },
        "yPlus": { "r": 170, "g": 172, "b": 180 }, "yMinus": { "r": 170, "g": 172, "b": 180 }
      }
    }
  ],
  "imageSpaces": [
    {
      "editorId": "MF_BrightIMGS",
      "brightness": 1.35, "saturation": 1.2, "contrast": 1.0,
      "bloomScale": 0.8, "sunlightScale": 1.2, "white": 1.5
    }
  ],
  "cells": [
    {
      "editorId": "MF_BrightRoom",
      "name": "Bright Test Room",
      "template": "Skyrim.esm:0x000165A0",
      "lightingTemplate": "MF_BrightCaveLGTM",
      "imageSpace": "MF_BrightIMGS"
    }
  ],
  "lights": [
    { "editorId": "MF_RoomFill", "color": { "r": 255, "g": 250, "b": 235 }, "radius": 1024, "fadeValue": 1.2 }
  ],
  "placements": [
    { "editorId": "MF_Floor", "base": "Skyrim.esm:0x000XXXXX", "cell": "MF_BrightRoom", "position": { "x": 0, "y": 0, "z": 0 } },
    { "editorId": "MF_RoomFillRef", "base": "MF_RoomFill", "cell": "MF_BrightRoom", "position": { "x": 0, "y": 0, "z": 200 } }
  ]
}
```

注意：
- `cells[].template` 指一個 vanilla **interior** cell（與既有 cell-env copy 並存；新三欄在其後疊加）。`Skyrim.esm:0x000165A0` 是占位——用 `dotnet run --project src/ModForge.Cli -- find "$SK" <房間名> Cell` 找一個小的 vanilla 室內，確認後填入。
- `MF_Floor` 的 `base` 需一個 vanilla 地板 static——用 `find "$SK" Floor Static` 找並驗證（踩坑：假 nif 路徑 → 隱形地板）。占位 `0x000XXXXX` 必須換成查到的真 FormID。
- LGTM template `0x0300E2`（DefaultLightingTemplate）已於 Task 6 驗證存在。

- [ ] **Step 2: build + validate example**

```bash
SK="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data/Skyrim.esm"
dotnet run --project src/ModForge.Cli -- validate examples/lighting.json
dotnet run --project src/ModForge.Cli -- build examples/lighting.json /tmp/ModForgeBrightInterior.esp
```
Expected: validate 無 problem；build 成功、warnings 只有預期項（無 unresolved ref）。

- [ ] **Step 3: 用新 diag 驗證 build 結果**

```bash
dotnet run --project src/ModForge.Cli -- lgtmdiag /tmp/ModForgeBrightInterior.esp
dotnet run --project src/ModForge.Cli -- imgsdiag /tmp/ModForgeBrightInterior.esp
```
Expected: 印出 `MF_BrightCaveLGTM`（ambient 150/155/170、DALC scale 1）與 `MF_BrightIMGS`（brightness 1.35、saturation 1.2）。

- [ ] **Step 4: 更新 `examples/spec.schema.json`**

加 `lightingTemplates` / `imageSpaces` 兩個頂層陣列的 schema，及 `cells[]` 內 `lightingTemplate` / `imageSpace` / `lighting` 三欄（mirror Task 1 的型別；對照既有 `lights` 條目格式）。

- [ ] **Step 5: 封裝 zip 給使用者實機**

```bash
dotnet run --project src/ModForge.Cli -- package examples/lighting.json ~/skyrim_mods/ModForgeBrightInterior.zip
```
Expected: 產生扁平 zip（plugin 在 root，見 packaging-zip-stale-file-trap 記憶）。**交給使用者在 MO2/Proton 實機驗證「黑室內變亮」**（我無法跑遊戲）。

- [ ] **Step 6: Commit**

```bash
git add examples/lighting.json examples/spec.schema.json
git commit -m "docs(lighting): bright-interior example + schema (LGTM + IMGS + CELL lighting)" \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: CODE_MAP + SPEC 文檔同步

**Files:**
- Modify: `docs/CODE_MAP.world.md`
- Modify: `docs/SPEC-world.md`

> 僅在使用者實機確認「變亮」後（或確認結構正確、實機留待）執行；照 CLAUDE.md「commit 前 CODE_MAP + 文檔對齊」。

- [ ] **Step 1: `docs/CODE_MAP.world.md` 加 Lighting 段**

在「Lights 自訂光源（LIGT）」段之後加一段（mirror 該段格式）：

```markdown
## Lighting Templates + ImageSpaces 室內光照（LGTM / IMGS / CELL XCLL）
→ **說明文件**：[SPEC-world.md § lighting](../../../docs/spec/SPEC-world.md#lighting)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Lighting.cs` | `LightingTemplateSpec`(LGTM) / `ImageSpaceSpec`(IMGS) / `CellLightingSpec`(inline XCLL) / `AmbientColorsSpec`(DALC) |
| Spec | `Spec.World.cs` | `CellSpec.LightingTemplate/ImageSpace/Lighting` |
| Build P1 | `Generator.Build.Lighting.cs` | `BuildLightingTemplates` + `BuildImageSpaces`（模板抄+覆寫；DALC LGTM→DirectionalAmbientColors、XCLL→AmbientColors；BuildCells 前建，lgtmByEd/imgsByEd 供 cell 解析）|
| Build P1 | `Generator.Build.Cells.cs` | cell 掛 LGTM/IMGS link（`ResolveLightingRef`）+ `ApplyCellLighting`（inline XCLL + inherit flags；無 inline 且有 template → 全繼承）|
| Validate | `Generator.Validate.Lighting.cs` | color 0..255、template/cell-ref 可解、inherit flag 名合法 |
| Diag | `Diagnostics.Records.cs` | `lgtmdiag` / `imgsdiag` |

> 註：`ImageSpaceSpec`(IMGS base) 與既有 `ImageSpaceModifierSpec`(IMAD, `Generator.Build.ImageSpace.cs`) 是兩個不同 record。
```

並把 `examples/lighting.json` 加進本檔頂部 Examples 表、`LightingTests.cs` 加進 Tests 表。

- [ ] **Step 2: `docs/SPEC-world.md` 加 `## Lighting` 章節**

寫明三個 spec 的欄位表、模板抄+覆寫語意、CELL inherit flags 規則、DALC 是打亮核心、與既有 `template`(cell-env) 並存、IMAD vs IMGS 區別、踩坑（interior 無 XCLL = 黑房）。加一個錨點 `#lighting` 對應 CODE_MAP 連結。

- [ ] **Step 3: 跑全套測試最終確認**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`
Expected: 全綠（除環境性 WordWall）。

- [ ] **Step 4: Commit**

```bash
git add docs/CODE_MAP.world.md docs/SPEC-world.md
git commit -m "docs(lighting): CODE_MAP.world + SPEC-world — LGTM/IMGS/CELL lighting pipeline" \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## 落地後

- 更新 `CLAUDE.md`「已落地功能」加一條光照管線（in-game 確認後）+ IDEAS §12 ①②③④ 標 ✅。
- 留下一輪：IMGS 掛 **weather**（室外調色，§12 室外那半）；明亮 LGTM/IMGS 抽成具名 preset 庫。
