# 室外調色（IMGS 掛 Weather）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development. Steps use `- [ ]`.

**Goal:** `WeatherSpec` 能掛 per-time-of-day ImageSpace（IMGS）做室外明亮調色；in-spec 自訂 IMGS 或 vanilla 皆可。

**Architecture:** WTHR 的 `ImageSpaces`（Sunrise/Day/Sunset/Night FormLinks）在 pass 2 `WireWeatherLinks` 用既有 `Resolve` 接線（custom IMGS 經 formKeyByEd、vanilla 經 external）。Spec 加一個 `WeatherImageSpacesSpec`（四時段 + Default 補空）。

**Tech Stack:** C# net10.0、Mutagen 0.53.1、xUnit。

**地基（已驗證 2026-06-09）：** `IWeather.ImageSpaces`(`WeatherImageSpaces`) = `Sunrise/Day/Sunset/Night` FormLinks（vanilla 四時段各掛不同 IMGS）。pass-2 接線點 `WireWeatherLinks`（`Generator.Build.Climate.cs`）用 `Resolve(what, ref, set)`。`ValidateLighting` 已建 `imgsIds`。`ToColor`/`TryExternalRef` 既有。IMGS builder（`mod.ImageSpaces`）已落地。

---

## File Structure

| 檔案 | 動作 | 職責 |
|------|------|------|
| `src/ModForge.Core/Spec/Spec.Weather.cs` | 改 | `WeatherImageSpacesSpec` + `WeatherSpec.ImageSpaces` |
| `src/ModForge.Core/Build/Generator.Build.Climate.cs` | 改 | `WireWeatherLinks` 重構（precip + imgs 獨立）+ imgs 接線 |
| `src/ModForge.Core/Validate/Generator.Validate.Lighting.cs` | 改 | weather imagespace ref 是 IMGS 的檢查 |
| `src/ModForge.Cli/Diagnostics/Diagnostics.Weather.cs` | 改 | WeatherDiag 印四時段 imagespace |
| `tests/ModForge.Core.Tests/Build/LightingTests.cs` | 改 | weather-imgs build + validate 測試 |
| `examples/weather_bright.json` | 新建 | bright IMGS + weather（fw 測） |
| `examples/spec.schema.json` | 改 | `weathers[].imageSpaces` |
| `docs/CODE_MAP.world.md` / `docs/SPEC-world.md` | 改 | 室外段 |

測試：`dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~LightingTests"`

---

## Task 1: Spec — WeatherImageSpacesSpec + WeatherSpec.ImageSpaces

**Files:** Modify `src/ModForge.Core/Spec/Spec.Weather.cs`

- [ ] **Step 1:** Add the new spec class near `WeatherColorSpec` (after it). Then add the field to `WeatherSpec`.

New class (place right after the `WeatherColorSpec` class definition):

```csharp
/// <summary>Per-time-of-day ImageSpace (IMGS) attached to a Weather — the outdoor color-grading
/// lever (HDR/bloom/saturation by time of day). Each ref is an in-spec ImageSpace editorId OR a
/// vanilla "&lt;master&gt;:0xFORMID". <see cref="Default"/> fills any time-of-day left empty, so a
/// single bright IMGS can grade the whole day.</summary>
public sealed class WeatherImageSpacesSpec
{
    public string Default { get; set; } = "";
    public string Sunrise { get; set; } = "";
    public string Day { get; set; } = "";
    public string Sunset { get; set; } = "";
    public string Night { get; set; } = "";
}
```

Add to `WeatherSpec` (alongside its other optional members, e.g. after the colour properties):

```csharp
    /// <summary>Per-time-of-day screen ImageSpace (IMGS) — outdoor color grading. Optional.</summary>
    public WeatherImageSpacesSpec? ImageSpaces { get; set; }
```

- [ ] **Step 2:** `dotnet build src/ModForge.Core/ModForge.Core.csproj` → succeeds.
- [ ] **Step 3:** Commit.
```bash
git add src/ModForge.Core/Spec/Spec.Weather.cs
git commit -m "feat(weather): WeatherImageSpacesSpec + WeatherSpec.ImageSpaces (outdoor IMGS grading)" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Build wiring — WireWeatherLinks

**Files:** Modify `src/ModForge.Core/Build/Generator.Build.Climate.cs`; Test `tests/ModForge.Core.Tests/Build/LightingTests.cs`

- [ ] **Step 1: Write failing test** (append to `LightingTests`):

```csharp
    // Weather.ImageSpaces.Default fills all four times-of-day with the custom IMGS; explicit Day overrides.
    [Fact]
    public void Weather_ImageSpaces_DefaultFillsAllAndDayOverrides()
    {
        var spec = new ModSpec
        {
            ImageSpaces =
            {
                new ImageSpaceSpec { EditorId = "MF_OutdoorBright", Brightness = 1.3f, Saturation = 1.25f },
                new ImageSpaceSpec { EditorId = "MF_NoonPunch", Brightness = 1.5f },
            },
            Weathers =
            {
                new WeatherSpec
                {
                    EditorId = "MF_BrightWeather",
                    ImageSpaces = new WeatherImageSpacesSpec { Default = "MF_OutdoorBright", Day = "MF_NoonPunch" },
                },
            },
        };

        var r = Build(spec);
        var bright = r.Mod.EnumerateMajorRecords<IImageSpaceGetter>().Single(x => x.EditorID == "MF_OutdoorBright");
        var noon = r.Mod.EnumerateMajorRecords<IImageSpaceGetter>().Single(x => x.EditorID == "MF_NoonPunch");
        var w = r.Mod.EnumerateMajorRecords<IWeatherGetter>().Single(x => x.EditorID == "MF_BrightWeather");
        Assert.NotNull(w.ImageSpaces);
        Assert.Equal(bright.FormKey, w.ImageSpaces!.Sunrise.FormKey);
        Assert.Equal(noon.FormKey,   w.ImageSpaces!.Day.FormKey);     // explicit Day wins over Default
        Assert.Equal(bright.FormKey, w.ImageSpaces!.Sunset.FormKey);
        Assert.Equal(bright.FormKey, w.ImageSpaces!.Night.FormKey);
    }
```

- [ ] **Step 2:** Run → FAIL.
`dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~LightingTests.Weather_ImageSpaces_DefaultFillsAllAndDayOverrides"`

- [ ] **Step 3:** Rewrite `WireWeatherLinks` in `Generator.Build.Climate.cs` so precipitation and imageSpaces are handled INDEPENDENTLY (the current early `continue` on empty precipitation must not skip imagespace wiring):

```csharp
    // Pass 2: weather → precipitation (SPGD) ref + per-time-of-day ImageSpace (IMGS) refs.
    private static void WireWeatherLinks(ModSpec spec, Dictionary<string, IMajorRecord> recordsByEd,
                                         Action<string, string, Action<FormKey>> resolve)
    {
        foreach (var ws in spec.Weathers)
        {
            if (!recordsByEd.TryGetValue(ws.EditorId, out var rec) || rec is not IWeather w) continue;

            if (!string.IsNullOrWhiteSpace(ws.Precipitation))
                resolve($"weather '{ws.EditorId}' precipitation", ws.Precipitation,
                    fk => w.Precipitation.SetTo(fk));

            if (ws.ImageSpaces is { } isp)
            {
                w.ImageSpaces ??= new();
                string Pick(string tod) => !string.IsNullOrWhiteSpace(tod) ? tod : isp.Default;
                void Wire(string slot, string refStr, Action<FormKey> set)
                {
                    if (!string.IsNullOrWhiteSpace(refStr))
                        resolve($"weather '{ws.EditorId}' imageSpace {slot}", refStr, set);
                }
                Wire("sunrise", Pick(isp.Sunrise), fk => w.ImageSpaces.Sunrise.SetTo(fk));
                Wire("day",     Pick(isp.Day),     fk => w.ImageSpaces.Day.SetTo(fk));
                Wire("sunset",  Pick(isp.Sunset),  fk => w.ImageSpaces.Sunset.SetTo(fk));
                Wire("night",   Pick(isp.Night),   fk => w.ImageSpaces.Night.SetTo(fk));
            }
        }
    }
```

NOTE: `WeatherImageSpaces` is in `Mutagen.Bethesda.Skyrim`; `w.ImageSpaces ??= new()` materializes it. `Resolve` (the passed-in delegate) already resolves in-spec editorIds (via formKeyByEd, built at pass-2 start so a custom IMGS editorId is present) and external refs.

- [ ] **Step 4:** Run → PASS (the new test). Then the whole `LightingTests` class → all PASS.
- [ ] **Step 5:** Commit.
```bash
git add src/ModForge.Core/Build/Generator.Build.Climate.cs tests/ModForge.Core.Tests/Build/LightingTests.cs
git commit -m "feat(weather): wire per-ToD ImageSpace on WTHR (Default fills unset; precip+imgs independent)" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Validate — weather imageSpace must be an IMGS

**Files:** Modify `src/ModForge.Core/Validate/Generator.Validate.Lighting.cs`; Test `tests/ModForge.Core.Tests/Build/LightingTests.cs`

- [ ] **Step 1: Write failing test** (append to `LightingTests`):

```csharp
    [Fact]
    public void Validate_WeatherImageSpace_RejectsNonImgsRef()
    {
        var spec = new ModSpec
        {
            ImageSpaces = { new ImageSpaceSpec { EditorId = "MF_GoodIMGS" } },
            Weathers =
            {
                new WeatherSpec
                {
                    EditorId = "MF_BadWeather",
                    ImageSpaces = new WeatherImageSpacesSpec { Default = "MF_NotAnImgs" },  // unresolved / not an IMGS
                },
                new WeatherSpec
                {
                    EditorId = "MF_OkWeather",
                    ImageSpaces = new WeatherImageSpacesSpec { Default = "MF_GoodIMGS" },    // valid
                },
            },
        };

        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("MF_BadWeather") && p.Contains("imageSpace"));
        Assert.DoesNotContain(problems, p => p.Contains("MF_OkWeather") && p.Contains("imageSpace"));
    }
```

- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** In `ValidateLighting` (in `Generator.Validate.Lighting.cs`), AFTER the `foreach (var c in spec.Cells)` loop (still inside the method, `imgsIds` is in scope), add:

```csharp
        foreach (var ws in spec.Weathers)
        {
            if (ws.ImageSpaces is not { } isp) continue;
            var o = $"weather '{ws.EditorId}'";
            foreach (var (slot, r) in new[] { ("default", isp.Default), ("sunrise", isp.Sunrise),
                                              ("day", isp.Day), ("sunset", isp.Sunset), ("night", isp.Night) })
                if (!string.IsNullOrWhiteSpace(r) && !imgsIds.Contains(r) && !TryExternalRef(r, out _))
                    problems.Add($"{o} imageSpace.{slot} '{r}' unresolved (need an in-spec ImageSpace editorId or <master>:0xFORMID)");
        }
```

- [ ] **Step 4:** Run → PASS. Then full suite `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj` → all pass (report count; WordWall env test passes here as Skyrim.esm present).
- [ ] **Step 5:** Commit.
```bash
git add src/ModForge.Core/Validate/Generator.Validate.Lighting.cs tests/ModForge.Core.Tests/Build/LightingTests.cs
git commit -m "feat(weather): validate — weather imageSpace ToD refs must resolve to an IMGS" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: WeatherDiag — print the four ImageSpaces

**Files:** Modify `src/ModForge.Cli/Diagnostics/Diagnostics.Weather.cs`

- [ ] **Step 1:** In `WeatherDiag`, after the `Precipitation` print line (`Console.WriteLine($"  Precipitation = ...")`), add:

```csharp
            var isp = w.ImageSpaces;
            string Isp(IFormLinkGetter<IImageSpaceAdapterGetter>? _l) => "-"; // placeholder, replaced below
            Console.WriteLine("  ImageSpaces  "
                + $"sunrise={Fk(isp?.Sunrise)} day={Fk(isp?.Day)} sunset={Fk(isp?.Sunset)} night={Fk(isp?.Night)}");
```

Replace the placeholder approach with a small local that prints a FormKey or "-". Concretely, add this local helper near the top of `WeatherDiag` (beside the existing `Col`/`Rgb` locals):

```csharp
            static string Fk(Mutagen.Bethesda.Plugins.IFormLinkGetter<IImageSpaceGetter>? l)
                => l is { } x && !x.FormKey.IsNull ? x.FormKey.ToString() : "-";
```

and use the single `Console.WriteLine` line:

```csharp
            var isp = w.ImageSpaces;
            Console.WriteLine($"  ImageSpaces  sunrise={Fk(isp?.Sunrise)} day={Fk(isp?.Day)} sunset={Fk(isp?.Sunset)} night={Fk(isp?.Night)}");
```

(Delete the placeholder `Isp` line — it was only illustrative. The real code is the `Fk` local + the one WriteLine.) Confirm the correct getter type for the ToD links by inspection: they are `IFormLinkGetter<IImageSpaceGetter>`. If the compiler reports a different generic arg, match it.

- [ ] **Step 2:** `dotnet build src/ModForge.Cli/ModForge.Cli.csproj` → succeeds (a `.pex` embedded-resource warning is the known unrelated condition, not an error).
- [ ] **Step 3:** Manual check against a vanilla weather:
```bash
SK="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data/Skyrim.esm"
dotnet run --project src/ModForge.Cli -- weatherdiag "$SK" 0x10E1F2
```
Expected: prints SkyrimClear_A with an `ImageSpaces sunrise=... day=012F88:Skyrim.esm sunset=... night=...` line (non-null FormKeys). Paste output in report.

- [ ] **Step 4:** Commit.
```bash
git add src/ModForge.Cli/Diagnostics/Diagnostics.Weather.cs
git commit -m "feat(cli): weatherdiag prints the four per-ToD ImageSpace links" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Example + schema + docs

**Files:** Create `examples/weather_bright.json`; Modify `examples/spec.schema.json`, `docs/CODE_MAP.world.md`, `docs/SPEC-world.md`

- [ ] **Step 1:** Inspect an existing weather example (`grep -rl '"weathers"' examples/` and read one, e.g. `examples/worldspace_spec.json` if it has weathers, else look at how `WeatherSpec` top-level is keyed). Match top-level keys (`pluginName`, `esl`, etc. — see `examples/lighting.json`).

- [ ] **Step 2:** Create `examples/weather_bright.json`: a bright IMGS (give it a vanilla `template` — pick a clean vanilla outdoor IMGS via `imgsdiag "$SK" | head`, e.g. a clear-day day IMGS like `0x012F88`, verify it exists — then bump brightness/saturation), and a weather with bright sky/sunlight/ambient colors + `imageSpaces.default` pointing at that IMGS. Example shape (substitute a VERIFIED vanilla IMGS template FormID):

```json
{
  "pluginName": "ModForgeBrightWeather.esp",
  "esl": true,
  "imageSpaces": [
    {
      "editorId": "MF_OutdoorBrightIMGS",
      "template": "Skyrim.esm:0x012F88",
      "brightness": 1.3, "saturation": 1.25, "bloomScale": 0.9, "sunlightScale": 1.2
    }
  ],
  "weathers": [
    {
      "editorId": "MF_BrightWeather",
      "skyUpperColor": { "day": { "r": 120, "g": 170, "b": 220 } },
      "sunlightColor": { "day": { "r": 255, "g": 250, "b": 235 } },
      "ambientColor":  { "day": { "r": 150, "g": 155, "b": 165 } },
      "imageSpaces": { "default": "MF_OutdoorBrightIMGS" }
    }
  ]
}
```

- [ ] **Step 3:** validate + build + diag-verify:
```bash
SK="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data/Skyrim.esm"
dotnet run --project src/ModForge.Cli -- validate examples/weather_bright.json
dotnet run --project src/ModForge.Cli -- build examples/weather_bright.json /tmp/ModForgeBrightWeather.esp
dotnet run --project src/ModForge.Cli -- imgsdiag /tmp/ModForgeBrightWeather.esp
# find the weather's FormID, then dump its imagespace links:
dotnet run --project src/ModForge.Cli -- find /tmp/ModForgeBrightWeather.esp MF_BrightWeather Weather
dotnet run --project src/ModForge.Cli -- weatherdiag /tmp/ModForgeBrightWeather.esp 0x<weatherID>
```
Expected: validate clean; build 0 unresolved warnings; imgsdiag shows MF_OutdoorBrightIMGS with brightness 1.3; weatherdiag shows all four ImageSpaces = the custom IMGS FormKey (Default filled all). Paste output.

- [ ] **Step 4:** Update `examples/spec.schema.json`: add `imageSpaces` (a `weatherImageSpaces` object: default/sunrise/day/sunset/night strings) to the weather item schema. Keep valid JSON.

- [ ] **Step 5:** Docs:
  - `docs/CODE_MAP.world.md` Worldspaces+Regions / Weather area: note `WeatherSpec.ImageSpaces` → `WireWeatherLinks` (Generator.Build.Climate.cs) + validate in Generator.Validate.Lighting.cs + weatherdiag. Add `examples/weather_bright.json` to Examples table.
  - `docs/SPEC-world.md § lighting`: add an "outdoor / weather" subsection — IMGS attaches to a Weather per time-of-day (`weathers[].imageSpaces` with default + sunrise/day/sunset/night), each an in-spec IMGS editorId or vanilla ref; default fills unset; test in-game with the console `fw <weatherFormID>` (ForceWeather, non-invasive). Cross-reference the indoor lighting section.

- [ ] **Step 6:** Full suite green, then commit:
```bash
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj
git add examples/weather_bright.json examples/spec.schema.json docs/CODE_MAP.world.md docs/SPEC-world.md
git commit -m "docs(weather): bright-weather example + schema + CODE_MAP/SPEC (outdoor IMGS grading)" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## 落地後（controller 手動，實機確認後）
- Package + 真 zip：`package examples/weather_bright.json /tmp/stage` 然後 `zip` 內容到 `~/skyrim_mods/ModForgeBrightWeather.zip`（扁平，esp 在 root；`file` 驗證是 zip 非目錄）。
- 使用者戶外 `fw <MF_BrightWeather FormID>` 測 → 確認後標 CLAUDE.md / IDEAS §12 室外 ✅。
