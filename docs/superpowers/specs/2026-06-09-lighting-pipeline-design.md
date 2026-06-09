# 設計：明亮室內光照管線（LGTM + CELL 光照 + IMGS）

日期：2026-06-09
狀態：設計已批准（待寫實作計劃）
對應想法：`docs/IDEAS.md` §12（明亮美術基調 / 光照管線）

## 目標

把 Skyrim 室內/地城從預設陰暗變明亮（原神/薩爾達調性），純記錄層完成。
落地 IDEAS §12 的四個缺口：① CELL 光照欄位進 spec、② 自製明亮 LGTM 模板、
③ 自訂 IMGS 掛 cell、④ 補 `lgtmdiag`/`imgsdiag` 診斷。

「完整室內包」一次做齊：LGTM + CELL 逐欄光照（含 DALC）+ IMGS。

## 授權模型：模板抄 + 覆寫

延續 codebase 既有 `template` 哲學（cell/item/NPC 都是抄 vanilla 再覆寫）。
三個 record 同一模式：`template:` 指向 vanilla record → DeepCopy 當底 →
**只有 spec 裡有給的欄位才覆寫**（全部 optional/nullable，沒給就保留 vanilla 值）。
沒給 `template` 也能建（引擎中性預設 + warn），但推薦路徑是抄一個明亮的再精調。

理由：LGTM/IMGS 各有幾十個欄位，全欄明寫易寫出怪值（fog 距離 / DALC 忘設 →
黑房）；模板抄保證 vanilla 預設不壞，spec 表面積最小。

## 記錄結構（Mutagen 0.53.1 反射確認 2026-06-09）

### LGTM（LightingTemplate）與 CELL XCLL（CellLighting）—— 幾乎同構

兩者共享同一組光照欄位（差別：LGTM 用 `LightFadeStartDistance/EndDistance`，
XCLL 用 `LightFadeBegin/End`；XCLL 多一個 `Inherits` flags、`FogClipDistance`
兩邊都有）：

- `AmbientColor`（Color）—— 整體環境光底色
- `DirectionalColor`（Color）+ `DirectionalRotationXY`/`DirectionalRotationZ`（int）+ `DirectionalFade`（float）—— 方向光（像室內的「太陽」）
- `FogNearColor` / `FogFarColor`（Color）+ `FogNear` / `FogFar` / `FogMax` / `FogClipDistance` / `FogPower`（float）—— 霧
- `LightFade*`（float）—— 光源淡出距離
- `AmbientColors`（DALC，見下）—— 六方向環境光

### DALC（AmbientColors）—— 打亮地城的核心平填光

`DirectionalXPlus / XMinus / YPlus / YMinus / ZPlus / ZMinus`（Color，六方向半球光）
+ `Specular`（Color）+ `Scale`（float）。

把黑洞穴整體提亮主要靠這個（無 GI 的務實替代：環境光打底）。納入 LGTM 與
inline CELL 兩邊。

### IMGS（ImageSpace）—— 三個子結構

- **Hdr**：`EyeAdaptSpeed` / `EyeAdaptStrength` / `BloomBlurRadius` / `BloomThreshold` / `BloomScale` / `ReceiveBloomThreshold` / `White` / `SunlightScale` / `SkyScale`（皆 float）
- **Cinematic**：`Brightness` / `Contrast` / `Saturation`（float，1=中性，>1 增強）
- **Tint**：`Amount`（float 0..1）/ `Color`

「亮乾淨高飽和」大半是 IMGS（HDR 眼適應 + bloom + 飽和）。

**命名注意**：既有 `ImageSpaceModifierSpec`→**IMAD**（螢幕後處理曲線，給爆炸/
Papyrus 用，`Generator.Build.ImageSpace.cs`）是另一個 record；本設計的
`ImageSpaceSpec`→**IMGS**（base record）。兩者並存、語意不混。

## CELL 接線（關鍵引擎事實）

室內 cell **必須有 XCLL（`Lighting`）否則一片黑**（既有 `BuildCells` 註解已記此坑）。
XCLL 的 **Inherit flags** 決定哪些欄位從 LGTM 拉、哪些用 inline 值。
flags（Mutagen `CellLighting.Inherit`）：`AmbientColor` / `DirectionalColor` /
`FogColor` / `FogNear` / `FogFar` / `DirectionalRotation` / `DirectionalFade` /
`ClipDistance` / `FogPower` / `FogMax` / `LightFadeDistances`。

接線規則：

1. cell 給 `lightingTemplate`（自訂或 vanilla）但**沒**給 inline `lighting`
   → 自動建 `Lighting`，**Inherits = 全旗標**（全部繼承模板）。最省事路徑。
2. cell 給 inline `lighting` → 按 `CellLightingSpec` 組 XCLL；`inherit:[...]`
   列出仍要從模板拉的欄位（沒列的用 inline 值）。inline 給了某欄但 `inherit`
   也列了該欄 → 以繼承為準（模板優先），並 warn。
3. `imageSpace` → `cell.ImageSpace.SetTo(...)`。
4. 既有 `template`（整包抄 vanilla 室內 env）路徑**保留不動**；新三欄在其**之後**
   疊加覆寫，可共存（先抄整包 env 再精調光照）。

## Spec 形狀（新增）

### `Spec.Lighting.cs`（新檔）

```
LightingTemplateSpec:
  editorId            (string, required)
  template            (string?, vanilla/自訂 LGTM editorId or FormKey；DeepCopy 當底)
  ambientColor        (ColorSpec?)
  directionalColor    (ColorSpec?)
  directionalRotationXY (int?)
  directionalRotationZ  (int?)
  directionalFade     (float?)
  fogNearColor        (ColorSpec?)
  fogFarColor         (ColorSpec?)
  fogNear/fogFar/fogMax/fogClipDistance/fogPower (float?)
  lightFadeStart      (float?)
  lightFadeEnd        (float?)
  directionalAmbient  (AmbientColorsSpec?)

AmbientColorsSpec:
  xPlus/xMinus/yPlus/yMinus/zPlus/zMinus (ColorSpec?)
  specular            (ColorSpec?)
  scale               (float?)

ImageSpaceSpec:
  editorId            (string, required)
  template            (string?, vanilla/自訂 IMGS；DeepCopy 當底)
  # Hdr
  eyeAdaptSpeed/eyeAdaptStrength/bloomBlurRadius/bloomThreshold/bloomScale/
  receiveBloomThreshold/white/sunlightScale/skyScale (float?)
  # Cinematic
  brightness/contrast/saturation (float?)
  # Tint
  tintAmount          (float?)
  tintColor           (ColorSpec?)

CellLightingSpec:  (inline XCLL，欄位同 LightingTemplateSpec 的光照部分)
  ambientColor / directionalColor / directionalRotationXY / directionalRotationZ /
  directionalFade / fogNearColor / fogFarColor / fogNear / fogFar / fogMax /
  fogClipDistance / fogPower / lightFadeBegin / lightFadeEnd / directionalAmbient
  inherit             (string[]，要從模板拉的欄位旗標名)
```

### `Spec.World.cs`（改 `CellSpec`，新增三欄，皆 optional）

```
lightingTemplate    (string?，自訂 LightingTemplateSpec.editorId 或 vanilla LGTM)
imageSpace          (string?，自訂 ImageSpaceSpec.editorId 或 vanilla IMGS)
lighting            (CellLightingSpec?，inline XCLL 覆寫)
```

`ColorSpec` 沿用既有（Lights/IMAD 用的 R/G/B 0..255）。

## 元件與檔案

| 層 | 檔案 | 職責 |
|---|---|---|
| Spec | `Spec.Lighting.cs`（新） | 四個 spec class |
| Spec | `Spec.World.cs`（改） | `CellSpec` 三欄 |
| Build P1 | `Generator.Build.Lighting.cs`（新） | `BuildLightingTemplates` + `BuildImageSpaces`，在 `BuildCells` 前跑，建 `lgtmByEd`/`imgsByEd` editorId→record 表 |
| Build P1 | `Generator.Build.Cells.cs`（改） | cell 接 LGTM/IMGS link + 組 inline `Lighting`（含 inherit flags 邏輯）|
| Build ctx | `Generator.BuildContext.cs`（改） | `lgtmByEd`/`imgsByEd` 欄位 + 解析 helper（自訂 editorId 優先，否則當 vanilla FormKey/editorId 解）|
| Validate | `Generator.Validate.Lighting.cs`（新） | editorId 唯一、color 0..255、template/cell-ref 可解、inherit flag 名合法 |
| Diag | `Diagnostics.Records.cs`（改） | `lgtmdiag` / `imgsdiag`（從建好的 esp dump 顏色/霧/DALC/HDR）|
| Example | `examples/lighting.json`（新） | 明亮室內 cell：自訂 bright LGTM + bright IMGS + 地板 static + 一盞燈 |
| Schema | `examples/spec.schema.json`（改） | 新欄位 autocomplete |

## 資料流

1. **Build pass 1（順序）**：`BuildLightingTemplates` → `BuildImageSpaces`
   （各 `mod.LightingTemplates/ImageSpaces.AddNew()`；有 `template` 則先
   `TryResolveTemplate<I…Getter>` DeepCopy 當底再套覆寫；登錄 editorId→record）
   →（既有 GLOB/Light 等）→ `BuildCells`。
2. `BuildCells` 對每個 cell：既有 `template` env copy（不動）→ 若有 `lightingTemplate`
   解析（自訂表優先 → vanilla）SetTo `cell.LightingTemplate` →若有 `imageSpace`
   同理 SetTo `cell.ImageSpace` →組 `cell.Lighting`（inline 或全繼承）。
3. **Build pass 2**：無（三者皆無 outgoing ref 需 deferred；DALC/HDR 全 inline；
   cell 的 LGTM/IMGS link 在 pass 1 即可解，因記錄已先建）。
4. `BuildFormKeyTable` 反向登錄即可被引用。

## 錯誤處理

- `lightingTemplate`/`imageSpace` 解不到（自訂表 + vanilla 都查無）→ Validate 報錯。
- `template` 解不到 → warn，仍建（套中性預設 + 覆寫），不中斷 build。
- inline `lighting` 給了某欄又把該欄列進 `inherit` → warn（繼承優先）。
- color 分量超 0..255 / editorId 重複 / inherit flag 名不在 enum → Validate 報錯。
- cell 沒給任何光照（無 `template`、無 `lightingTemplate`、無 `lighting`）→ 沿用既有
  「may render black」warn（不變）。

## 測試

- 新 `LightingTests.cs`（net10）：
  - LGTM build：template DeepCopy + 覆寫單一欄（ambientColor）→ 其餘保留 vanilla。
  - LGTM build：DALC 六方向 + scale 寫入正確。
  - IMGS build：Hdr/Cinematic/Tint 三子結構覆寫正確、未給的保留 template 值。
  - CELL：給 `lightingTemplate` 無 inline → `Lighting.Inherits` = 全旗標、link 正確。
  - CELL：inline `lighting` + `inherit:[FogColor]` → 指定欄用 inline、列出欄繼承。
  - Validate：壞 color / 重複 editorId / 不存在 ref / 非法 inherit flag 各報錯。
- diag：`lgtmdiag`/`imgsdiag` 對 build 出的 esp 還原欄位（手測，非單元）。

## 增量落地順序（照 CLAUDE.md workflow，逐塊實機）

1. LGTM builder + validate + `lgtmdiag` → 測（單元 + diag）
2. IMGS builder + validate + `imgsdiag` → 測
3. CELL 接線（三欄 + inherit 邏輯）→ 測
4. `examples/lighting.json` + package `ModForgeBrightInterior.zip` →
   使用者實機驗證「黑地城變亮」
5. 全綠後：CODE_MAP.world + SPEC-world + spec.schema.json 同步 → commit

測試迭代期間 CODE_MAP/文檔可暫時落後；commit 前對齊。

## 非目標（YAGNI / 留待之後）

- IMGS DepthOfField（景深）—— 與「明亮」無關，先不開欄。
- LGTM `Unknown` / `DATADataTypeState` —— 內部欄位，不暴露。
- IMGS 掛 **Weather**（室外調色）—— 本輪聚焦室內 cell；weather 掛 IMGS 留下一輪
  （IDEAS §12 室外那半）。
- 明亮 LGTM/IMGS 的「內建 preset 庫」—— 本輪靠使用者在 example 裡示範一組明亮值；
  之後可抽成具名 preset。
