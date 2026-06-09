# 設計：室外調色 — IMGS 掛 Weather（§12 室外那半）

日期：2026-06-09
狀態：設計（自主決定，使用者授權「做吧／別問」；待實作）
對應：`docs/IDEAS.md` §12 室外那半；延續 `2026-06-09-lighting-pipeline-design.md`（室內已落地）

## 目標

把剛落地的 IMGS base record 掛到 **Weather (WTHR)** 上做室外明亮調色。
室內靠 LGTM+CELL XCLL；室外的「亮乾淨高飽和」由 Weather 的 sky/sunlight/ambient
顏色（`WeatherSpec` 已支援）+ **Weather 的 per-time-of-day ImageSpace** 決定。本輪補後者。

## 地基（Mutagen 反射 + Skyrim.esm 解碼確認 2026-06-09）

- `IWeatherGetter.ImageSpaces`（`WeatherImageSpaces` 結構）含四條 FormLink：
  `Sunrise / Day / Sunset / Night`。vanilla（如 SkyrimClear_A）四時段各掛**不同** IMGS。
  這就是室外調色的引擎槓桿，與既有 per-ToD `WeatherColorSpec` 同構。
- WRLD 只有 `MapImage`（地圖選單圖，與遊戲內調色無關）→ 不碰。Region 只選 weather → 不碰。
- LGTM 是室內 cell 用的 lighting template，室外不適用 → 不碰。
- pass-2 接線點：`WireWeatherLinks`（`Generator.Build.Climate.cs`），用 context 的
  `Resolve(what, ref, set)`（解 in-spec editorId via formKeyByEd + external `<master>:0xFORMID`）。

## 設計

### Spec（`Spec.Weather.cs`）

新增 `WeatherImageSpacesSpec`（四時段 + Default 便利欄，Default 補未給時段——
鏡像 `FillWeatherColor` 的 Day-fallback 哲學）：

```
WeatherImageSpacesSpec:
  default  (string)  # 補任何留空的時段（一個明亮 IMGS 套整天）
  sunrise  (string)
  day      (string)
  sunset   (string)
  night    (string)
```

每個 ref = in-spec IMGS editorId **或** vanilla `<master>:0xFORMID`。
`WeatherSpec` 加 `WeatherImageSpacesSpec? ImageSpaces`（optional）。

### Build 接線（`Generator.Build.Climate.cs` `WireWeatherLinks`）

重構：先取 weather record，**precipitation 與 imageSpaces 各自獨立接**（現在
precipitation 空就 `continue` 會連帶跳過 imagespace，必須拆開）。對有 `ImageSpaces`
的 weather：`w.ImageSpaces ??= new()`，每時段取「該時段 ref 否則 Default」，非空才
`Resolve` + `SetTo` 對應的 `Sunrise/Day/Sunset/Night`。

### Validate（`Generator.Validate.Lighting.cs`，已有 `imgsIds`）

加 weather 迴圈：四時段 + Default 的非空 ref 必須是 in-spec **IMGS** editorId 或 external
（cross-type：指到非 IMGS → unresolved，與 cell imageSpace 檢查同規）。

### Diag（`Diagnostics.Weather.cs` `WeatherDiag`）

加一行印四時段 ImageSpace FormKey（驗證 gen vs vanilla）。

### Example + 測試

- `examples/weather_bright.json`：一個 bright IMGS（抄 vanilla 室外 IMGS 再調亮）+ 一個
  weather（`imageSpaces.default` 指該 IMGS，並設亮的 sky/sunlight/ambient 顏色）。
  **實機測**：戶外主控台 `fw <weatherFormID>`（ForceWeather，非侵入式，免改 vanilla 氣候）。
- `WeatherTests`（或 append `LightingTests`）：Default 填滿四時段；顯式 Day 覆蓋 Default；
  external ref 解析。
- schema + CODE_MAP.world + SPEC-world「§ lighting」補室外段。

## 非目標（YAGNI）

- 把 weather/climate 指派給 worldspace/region（侵入式、影響全域）——用 `fw` 測即可。
- WRLD MapImage、region imagespace。
- 把 IMGS preset 抽成具名庫（室內室外共通的後續想法）。
