# CODE_MAP.npcs-packages — Weather / Climate

← [CODE_MAP.npcs-packages](../CODE_MAP.npcs-packages.md)

## Weather / Climate（天氣 WTHR / 氣候 CLMT）
→ **說明文件**：[SPEC-packages.md § weathers & climates](../../../../docs/spec/SPEC-packages.md#weathers--climates--custom-skies-wthr--weather-cycles-clmt)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Weather.cs` | `WeatherSpec`（含 `Template`：抄 vanilla 天氣繼承雲/天空）, `WeatherImageSpacesSpec`（per-ToD IMGS refs）, `ClimateSpec` |
| Build P1 | `Generator.Build.Weather.cs` | 建 weather scalar fields（colors/clouds/wind/fog）；`template` DeepCopy vanilla 天氣後只覆寫 spec 給的（empty clouds/null color 保留 template）|
| Build P1 | `Generator.Build.Climate.cs` | 建 climate scalar fields（timing/sun/moon/volatility）；weather entries pass 2 接；`WireWeatherLinks`：`WeatherSpec.ImageSpaces` → WTHR per-ToD `ImageSpaces`（`default` 填未設 ToD）|
| Validate | `Generator.Validate.Weather.cs` | color 範圍、cloud index、timing monotonicity、chance 總和 |
| Validate | `Generator.Validate.Lighting.cs` | `WeatherImageSpacesSpec` 中每個 IMGS ref 可解（cross-type with IMGS registry）|
| Diag | `Diagnostics.Weather.cs` | sky colors / cloud layers / precipitation / wind / fog / ImageSpaces per-ToD dump（`weatherdiag`）|
| Diag | `Diagnostics.Records.cs` | `imgsdiag`：列印 IMGS brightness/contrast/saturation/bloom/HDR/tint（室內 CELL 與室外 Weather 共用）|
| Tests | `LightingTests.cs` | weather `imageSpaces` per-ToD 接線（`default` 填空）+ `WeatherSpec.template` 雲繼承（與室內 LGTM/IMGS/CELL 同檔；另見 `WeatherClimateTests.cs` 管 weather scalar）|

室外 IMGS 調色範例：`examples/weather_bright.json`（IMGS `template` SkyrimClear Day + Weather `imageSpaces.default`，`weatherdiag` 確認四 ToD 全填同一 IMGS）。
