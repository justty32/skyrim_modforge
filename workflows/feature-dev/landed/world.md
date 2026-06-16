# 已落地 — 世界（光照 / 天氣 / 地圖 / 放置光源）

← [landed index](README.md)｜對應 [CODE_MAP.world](../../common/code-map/CODE_MAP.world.md)

**PlacementSpec 六欄位（Scale/InitiallyDisabled/EnableParent/Lock/Ownership/Count）**（離線落地 2026-06-16）：`PlacementSpec` 補 XSCL/flag-0x800/XESP/XLOC/XOWN/XCNT build + validate。`ParseLockLevel` in `Generator.Helpers.cs`。`PlacementSpecFieldsTests.cs`（29 tests）。doc：`SPEC-world.md § placement extra fields`；schema：`spec.schema.json`；CODE_MAP：`CODE_MAP.world.md § Placements`。（實機測試自行確認新欄位在 ESP 中正確寫入；純 record-field，無 Papyrus 依賴。）

**record builders（world 域）**
- **Light (LIGT)**：`LightSpec`（color/radius/fade/flags…），用 placements 放置。
- **Map marker (XMRK) on vanilla worldspace**（in-game 確認 2026-06-13，`ModForgeQuestMarkers-mapfix9.zip`）：`mapMarkers[]`（name/worldspace/position/type/flags）放進 Tamriel persistent cell，可見+可傳送+大地圖底圖完整渲染。**override vanilla WRLD（`CopyWorldspaceEnv`）的兩條鐵律**——詳見 [investigation/gotchas](../../investigation/gotchas.md) 與 memory [[worldspace-override-must-carry-topcell]]/[[worldspace-override-map-render-fields]]：① 持久 cell（0xD74）要帶 `MajorRecordFlagsRaw=0x00040400`（CopyCellEnv 不複製 record-header flag → CTD）；② 地圖渲染要帶 **EDID + RNAM + TNAM/UNAM 但永不帶 OFST**（OFST=Skyrim.esm 絕對檔案偏移量不可移植；缺 EDID=白圖有高度；缺 RNAM=破圖）。`examples/quest-markers.json`、`mapmarker`/`xmarker` placement kind。

**光照管線（明亮室內）**（in-game 確認 2026-06-09，`ModForgeBrightInterior.zip`）：`LightingTemplate (LGTM)` + `ImageSpace (IMGS, ≠ 既有 IMAD)` base record，模板抄 vanilla + 只覆寫亮度欄位；CELL 逐欄光照 `cells[].lightingTemplate/imageSpace/lighting(inline XCLL)`，含 **DALC 六方向環境光**（打亮地城核心：LGTM→`DirectionalAmbientColors`、XCLL→`AmbientColors`）。inline 無給且有 template → 全繼承。診斷 `lgtmdiag`/`imgsdiag`。**欄位/語意見 `SPEC-world.md § lighting`、wiring 見 `CODE_MAP.world.md`。** 踩坑：① interior CELL 無 XCLL = 黑房；② IMGS 不給 `template` 從零起（HDR 欄位全 0）行為可能怪，建議抄 vanilla IMGS 再調；③ build 期 `ResolveLightingRef` 不分型別，靠 Validate 的 cross-type 檢查擋打錯 slot。

**光照管線（室外調色）**（in-game 確認 2026-06-09，`ModForgeBrightWeather.zip`）：IMGS 掛 **Weather** per-ToD —`weathers[].imageSpaces`（`default` 補未設時段 + sunrise/day/sunset/night；ref=in-spec IMGS 或 vanilla；pass-2 `WireWeatherLinks` 接、`weatherdiag` 探）。**`WeatherSpec.template`**（抄 vanilla 天氣，DeepCopy 繼承雲/雲貼圖/天空色/大氣，只覆寫 spec 給的；null 色保留模板、空 clouds 保留模板雲）——**from-scratch 天氣無雲**故室外務必抄 template（如 `Skyrim.esm:0x10E1F2` SkyrimClear_A）。室外光由 weather sky/sunlight/ambient 顏色（既有）+ per-ToD IMGS grading 決定，LGTM/CELL 室內專用不適用室外。實機 `fw <weatherFormID>` 非侵入測。**未做**：weather/IMGS 掛 region。（明亮 LGTM/IMGS「具名 preset 庫」已由 `$ref`/`$env` 解析層落地，見 [infra](infra.md)。）
