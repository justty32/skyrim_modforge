# CODE_MAP.world — Lights・Lighting Templates

← [CODE_MAP.world](../CODE_MAP.world.md)

## Lights 自訂光源（LIGT）
→ **說明文件**：[SPEC-lighting.md § lights](../../../../docs/spec/SPEC-lighting.md#lights--custom-light-sources-ligt)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Lights.cs` | `LightSpec`（editorId/name/color/radius/fadeValue/falloff/fov/flags/value/weight）|
| Build P1 | `Generator.Build.Lights.cs` | `BuildLights`（`mod.Lights.AddNew`；color R/G/B、radius/fade、`Light.Flag` 解析；BuildFormKeyTable 前建，故 placement 可按 editorId 放置）|
| Validate | `Generator.Validate.Lights.cs` | `ValidateLights`（flag 名合法、color 0..255、radius>0、editorId 唯一）|
| Diag | `Diagnostics.Records.cs` | `lightdiag`（radius/color/fade/flags dump）|

自訂 Light 是一般 base record——用既有 `placements[]`（base=light editorId）放進任意 cell，無需動 placement 程式碼。

---

## Lighting Templates + ImageSpaces 室內光照（LGTM / IMGS / CELL XCLL）
→ **說明文件**：[SPEC-lighting.md § lighting](../../../../docs/spec/SPEC-lighting.md#lighting)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Lighting.cs` | `LightingTemplateSpec`(LGTM) / `ImageSpaceSpec`(IMGS) / `CellLightingSpec`(inline XCLL) / `AmbientColorsSpec`(DALC) |
| Spec | `Spec.World.cs` | `CellSpec.LightingTemplate/ImageSpace/Lighting` |
| Spec | `Spec.Worldspace.cs` | `WorldspaceCellSpec` 的 `WaterHeight`(XCLW) / `Water`(XCWT) / `AcousticSpace`(XCAS) / `LightingTemplate` / `ImageSpace`（own-worldspace exterior CELL；ref 可接 spec 內或 external）|
| Build P1 | `Generator.Build.Lighting.cs` | `BuildLightingTemplates` + `BuildImageSpaces`（模板抄+覆寫；DALC LGTM→DirectionalAmbientColors、XCLL→AmbientColors；BuildCells 前建，lgtmByEd/imgsByEd 供 cell 解析）|
| Build P1 | `Generator.Build.Cells.cs` | cell 掛 LGTM/IMGS link（`ResolveLightingRef`）+ `ApplyCellLighting`（inline XCLL + inherit flags；無 inline 且有 template → 全繼承）|
| Build P2 | `Generator.Build.Worldspace.cs` | flat exterior CELL 建構時寫 `waterHeight`→XCLW，並用既有 `Wire` 掛 `water`→XCWT、`acousticSpace`→XCAS、LGTM／IMGS；空白欄位不寫 link |
| Validate | `Generator.Validate.Lighting.cs` | color 0..255、template/cell-ref 可解（cross-type）、inherit flag 名合法 |
| Diag | `Diagnostics.Records.cs` | `lgtmdiag` / `imgsdiag` |

> 註：`ImageSpaceSpec`(IMGS base) 與既有 `ImageSpaceModifierSpec`(IMAD, `Generator.Build.ImageSpace.cs`) 是兩個不同 record。
