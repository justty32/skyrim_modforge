# 已落地 — 世界（光照 / 天氣 / 地圖 / 放置光源）

← [landed index](README.md)｜對應 [CODE_MAP.world](../../common/code-map/CODE_MAP.world.md)

**Worldspace heightmap → 非平坦 LAND（in-game 確認 2026-06-16）**：`heightmap` spec 欄位（PNG path/originX/originY/minHeight/maxHeight）→ 自動衍生 N×M cell grid + 生起伏 LAND（VHGT）。`Heightmap.cs`（L16 PNG 載入、Y-flip、SampleCell）+ `Vhgt.cs`（signed-int8 row-wise cumulative encode/decode，`IReadOnlyArray2d<byte>`）+ `BuildWorldspaces` heightmap 分支 + **seam stitching**（encode 後 decode east/north edge → 注入鄰格 west/south，消除 ±8 units rounding seam）。踩坑三條：① PNG 生成 spike bug（一點 65535 其餘 0）→ 高斯重生；② 高度範圍 0~8000 → 4000~4500；③ `defaultLandHeight` 預設 −27000 → 須設成等於 PNG minHeight 避免世界邊緣懸崖。Task 7 主力機驗算法：Tamriel 20 格 decode→encode delta bytes 完全一致。`examples/worldspace_heightmap.json` + PNG；597 tests。

**VNML 法線：從 heightmap 中心差分計算（對 vanilla 自驗 2026-06-16）**：`Vnml.cs`（35×35 邊框取樣 → 33×33 法線，`Heightmap.SampleCellExtended`）。**Skyrim VNML 約定（驗證自 vanilla Tamriel LAND）**：① 分量是 **signed byte** `(sbyte)round(n×127)`，**向上 = (0,0,127)**（非 (128,128,255)）；② Array2d 存 `[col=East, row=North]`，與 VHGT 同；③ 水平頂點間距 = **128 game units**（cell 4096/32，別跟 VHGT 高度尺度 ×8 混）。座標 X=東 Y=北 Z=上，法線 = East-tangent × North-tangent。曾一次踩三坑（轉置 / StepUnits=8 / +128 偏移）皆已修。驗證測試 `VhgtTests.VnmlCompute_OrientationMatchesVanilla`（RequiresSkyrim）：對 20 最陡 Tamriel cell，中心差分法線 vs vanilla 平均 signed-byte 誤差 <20、direct 明顯優於 transposed。605 tests。

**PlacementSpec 六欄位（Scale/InitiallyDisabled/EnableParent/Lock/Ownership/Count）**（離線落地 2026-06-16）：`PlacementSpec` 補 XSCL/flag-0x800/XESP/XLOC/XOWN/XCNT build + validate。`ParseLockLevel` in `Generator.Helpers.cs`。`PlacementSpecFieldsTests.cs`（29 tests）。doc：`SPEC-world.md § placement extra fields`；schema：`spec.schema.json`；CODE_MAP：`CODE_MAP.world.md § Placements`。（實機測試自行確認新欄位在 ESP 中正確寫入；純 record-field，無 Papyrus 依賴。）

**Worldspace baseTexture（單層 BTXT，offline 落地 2026-06-17）**：`worldspace.baseTexture`（LTEX ref）→ 每格 LAND 四象限 BASE 層（BTXT），全世界單一地表貼圖、無 per-vertex 混合（`cells`/`heightmap` 皆可）。`Generator.Build.Worldspace.cs` EmitCell 接線。`WorldspaceBaseTextureTests`。doc `SPEC-worldspaces.md § baseTexture`、schema、`CODE_MAP.world.md`。⚠ byte-level vs vanilla LAND BTXT 待主力機 xEdit（WAIT_USER）；真實 LTEX FormID 待 `gamedata find` 查（測試/example 用 placeholder）。

**Worldspace textureLayers（多層 splatmap → VTXT/ATXT，offline 落地 2026-06-17）**：`worldspace.textureLayers[]`（LTEX ref + grayscale splatmap PNG，grid 規則同 heightmap）→ 每格四象限稀疏 ATXT+VTXT alpha 層（零 alpha 頂點省略，同 vanilla；splatmap 未覆蓋的象限不出層）。`Splatmap.cs`（PNG→per-vertex alpha）+ `Vtxt.cs`（BuildLayers）+ `Generator.Build.Worldspace.cs` EmitCell。`WorldspaceSplatmapTests`（604 全綠）。Mutagen `AlphaLayer{Header, AlphaLayerData{Position,Opacity,Unused}}` 反射查證。doc `SPEC-worldspaces.md § textureLayers`、schema、`CODE_MAP.world.md`；前端 splat-paint 筆在 `sub_projs/godot-worldspace-editor`。⚠ VTXT 每點 position/per-quadrant layer-number 打包 byte 待主力機 xEdit（WAIT_USER）。

**godotPlacements（Godot 編輯器物件擺放 → REFR，offline 落地 2026-06-17）**：`worldspace.godotPlacements`（`godot-worldspace-editor` 匯出的 `placements.json`；path/originX/originY）→ Y-up→Skyrim 世界座標轉換 + 併入既有 placement 管線生 REFR。`GodotPlacements.cs`。前端 Place Mode 筆 + `placements.json` I/O 在 sub_proj。doc `SPEC-worldspaces.md § godotPlacements`、schema、`CODE_MAP.world.md`。Godot GUI 整鏈（擺物件→匯出→build→進遊戲）in-game 確認 2026-06-18（編輯器側見 [godot-editor.md](godot-editor.md)）。

**聚落人口 generator `settlements:`（Idea #22 MVP，落地 2026-06-25，834 測綠）**：高階 spec section 一鍵把「住滿活人的聚落」展開成既有低階記錄，**零外部 master、零 runtime 腳本**，純資料展開、離線完全可驗。架構＝**macro-expansion**（同 skillTrees）：`Generator.ExpandSettlements` 在 `Build()` pass-0（`ExpandSkillTrees` 後）把每個 resident 展開成 ACHR placement（spawn marker 座標 / `spawnPosition` fallback）＋ Sleep（綁 `home`）/Work（綁 `work`、小半徑）/Wander（always-on、大半徑）三 package（綁作者擺的錨點 ref，schedule 帶 wrap-midnight 時長、排程 by hour、wander 最後）＋ faction 三件套（自動或引用 settlement FACT、`crimeFaction`）＋ 可選 vendor（Vendor-flag FACT＋放置 merchant chest 含 gold＋自動 JobMerchantFaction）；`friendlyResidents` → 兩兩 Friend RELA。**錨點哲學**：home/work/spawnAt 是作者已擺的 REFR/marker editorId，macro 只負責把 package 綁上去（純抽象 sandbox = NPC 呆站，三方印證）。**順手修一個舊坑**：`ApplySandboxData`/`ApplySleepData` 的 location slot 原本 eager 解析（pass-1，placement 還沒註冊）→ in-spec 錨點一律 fallback NearSelf；改成 **deferred**（加進 `deferredLocationWires`，仿 Travel/Escort），placement loop 後才解析，in-spec marker/bed 錨點正確解析且自動被強制 persistent。檔：`Spec.Settlement.cs`/`Generator.Settlements.cs`/`Generator.Validate.Settlements.cs`，example `settlement_spec.json`，docs SPEC-world §populated settlements、schema、CODE_MAP.world。Phase 2＝crowd（leveled/controller）/flee PACK/inline npc/進階作息。**剩主力機實機驗收**（NPC 真走工作站/上床、vendor 開張）見 WAIT_USER。

**record builders（world 域）**
- **Light (LIGT)**：`LightSpec`（color/radius/fade/flags…），用 placements 放置。
- **Map marker (XMRK) on vanilla worldspace**（in-game 確認 2026-06-13，`ModForgeQuestMarkers-mapfix9.zip`）：`mapMarkers[]`（name/worldspace/position/type/flags）放進 Tamriel persistent cell，可見+可傳送+大地圖底圖完整渲染。**override vanilla WRLD（`CopyWorldspaceEnv`）的兩條鐵律**——詳見 [investigation/gotchas](../../investigation/gotchas.md) 與 memory [[worldspace-override-must-carry-topcell]]/[[worldspace-override-map-render-fields]]：① 持久 cell（0xD74）要帶 `MajorRecordFlagsRaw=0x00040400`（CopyCellEnv 不複製 record-header flag → CTD）；② 地圖渲染要帶 **EDID + RNAM + TNAM/UNAM 但永不帶 OFST**（OFST=Skyrim.esm 絕對檔案偏移量不可移植；缺 EDID=白圖有高度；缺 RNAM=破圖）。`examples/quest-markers.json`、`mapmarker`/`xmarker` placement kind。

**光照管線（明亮室內）**（in-game 確認 2026-06-09，`ModForgeBrightInterior.zip`）：`LightingTemplate (LGTM)` + `ImageSpace (IMGS, ≠ 既有 IMAD)` base record，模板抄 vanilla + 只覆寫亮度欄位；CELL 逐欄光照 `cells[].lightingTemplate/imageSpace/lighting(inline XCLL)`，含 **DALC 六方向環境光**（打亮地城核心：LGTM→`DirectionalAmbientColors`、XCLL→`AmbientColors`）。inline 無給且有 template → 全繼承。診斷 `lgtmdiag`/`imgsdiag`。**欄位/語意見 `SPEC-world.md § lighting`、wiring 見 `CODE_MAP.world.md`。** 踩坑：① interior CELL 無 XCLL = 黑房；② IMGS 不給 `template` 從零起（HDR 欄位全 0）行為可能怪，建議抄 vanilla IMGS 再調；③ build 期 `ResolveLightingRef` 不分型別，靠 Validate 的 cross-type 檢查擋打錯 slot。

**光照管線（室外調色）**（in-game 確認 2026-06-09，`ModForgeBrightWeather.zip`）：IMGS 掛 **Weather** per-ToD —`weathers[].imageSpaces`（`default` 補未設時段 + sunrise/day/sunset/night；ref=in-spec IMGS 或 vanilla；pass-2 `WireWeatherLinks` 接、`weatherdiag` 探）。**`WeatherSpec.template`**（抄 vanilla 天氣，DeepCopy 繼承雲/雲貼圖/天空色/大氣，只覆寫 spec 給的；null 色保留模板、空 clouds 保留模板雲）——**from-scratch 天氣無雲**故室外務必抄 template（如 `Skyrim.esm:0x10E1F2` SkyrimClear_A）。室外光由 weather sky/sunlight/ambient 顏色（既有）+ per-ToD IMGS grading 決定，LGTM/CELL 室內專用不適用室外。實機 `fw <weatherFormID>` 非侵入測。**未做**：weather/IMGS 掛 region。（明亮 LGTM/IMGS「具名 preset 庫」已由 `$ref`/`$env` 解析層落地，見 [infra](infra.md)。）

**in-world 技能樹 generator `skillTrees:`（Idea #20 Phase 3，落地 2026-06-21，804 測綠）**：高階 spec section 一鍵生成可點 in-world 養成樹（漂浮星節點＋連線＋per-node rank GLOB＋points pool＋gate＋學會給 ability＋亮起），**零外部 master（只 Skyrim.esm）**。架構＝**macro-expansion**：`Generator.ExpandSkillTrees` 在 `Build()` pass-0 把 `skillTrees:` 展開成既有低階記錄（globals/node+line ACTI/placements/`scripts:` 掛 MFSkillNode），重用全部既有 pass，新建記錄碼極少。線方向/scale **build-time C# 算**（node[i] 堆 origin+i*spacing、line 在中點 rot(90,0,180) scale=spacing/65），不需 runtime 腳本。節點行為 `assets/papyrus/MFSkillNode.psc`（extends ObjectReference：OnActivate gate→AddSpell+PlayAnimation"OwnedWild"+downLine"Unlock"+扣點；OnLoad 持久亮起），嵌入 CLI、`package` `ShipEmbeddedPex("MFSkillNode.pex")`；星/線 nif+9 貼圖 loose kit 經 `assets` 打包（非 master）。**生成輸出 dump 驗證與已實機確認的手刻版結構完全一致**。MVP=垂直線性鏈；分支/2D 待後續。檔：`Spec.SkillTree.cs`/`Generator.SkillTrees.cs`/`Generator.Validate.SkillTrees.cs`，example `skill_tree_spec.json`，docs SPEC-world §in-world skill trees。memory [[inworld-skill-tree-standalone-confirmed]]。

- **removals[]（Idea #24 §E 橡皮擦，2026-07-08）**：`ModSpec.Removals`（`<master>:0xFORMID` 既有 placed ref）→ `Generator.Build.Removals.cs` `BuildRemovals` 用 master link cache `TryResolveContext<IPlaced>` → `GetOrAddAsOverride(mod)`（自動把 parent cell/worldspace 一起 override）→ 設 `InitiallyDisabled`(0x800) + 深埋 Z−30000（避 havok 殘留）。標準「disable vanilla clutter」patch、可逆、headless-safe。`ValidateRemovals` 驗外部 ref 格式。RequiresSkyrim（需 master link cache）。測 `RemovalsTests.cs`（含 RequiresSkyrim：白漫馬廄 Skulvar 鋤頭 0x0D1991 override 後 InitiallyDisabled+Z−34603）。IN-GAME 待驗。

## scene-capture-bridge M4 spike — 遊戲內採集橋（Idea #24 元件③）· IN-GAME 2026-07-10

一支 SKSE C++ DLL（`sub_projs/scene-capture-bridge/`）走訪玩家所在 cell，把玩家擺放的 ref 序列化成 `scene.json`（＝一份合法 `ModSpec`）→ ModForge `build` 出 patch esp。實機驗收：

- **clang-cl 跨編譯的 SKSE DLL 可直接實機**：`skse64.log` → `plugin SceneCaptureBridge.dll (...) loaded correctly (handle 53)`。import 表只有系統 DLL、靜態 CRT。`BUILD.md` 原本假設此路徑僅供編譯驗證——有反例了（出貨仍走 Windows CI，因未驗 address-library 跨版本行為）。
- **vanilla diff**：`ResolveDurableId(&ref)` 解得出 ⇒ 既有 ref ⇒ 跳過；解不出（dynamic `0xFF......`，`GetFile(0)==nullptr`）⇒ 玩家 `PlaceAtMe` 擺的 ⇒ emit。The Bannered Mare (`01605E`) 按 F10：`0 placements, 717 pre-existing`。`placeatme` 兩個之後：`2 placements, 717 pre-existing`。
- **座標契約結案**：plugin 存弧度、Papyrus `GetAngle*` 回度數、C++ `GetAngle()` 回弧度、`scene.json` 一律度數；`interior` 的 `data.location` 就是 cell-local（匯出座標與 `get_cell_info` 玩家座標一字不差）；vanilla `scale==1.0` 時省略 XSCL。round-trip 誤差 2.3e-7 rad（float32 捨入）。
- **整鏈**：真實 `scene.json` → `validate` 零問題 → `build` → 2 placement 掛在 vanilla interior cell 的加法 override，master 僅 `Skyrim.esm`。

已知取捨：玩家**移動/縮放過的 vanilla ref** 不採（需 emit 既有 ref 的 override，`scene.json` 尚未建模）。NPC 來源預設走 ModForge 直接生的「大眾臉」；PROTEUS 拓印為可選。

**遊戲內面板（2026-07-10 IN-GAME）**：`src/UI.cpp` 消費 [SKSE Menu Framework 3](../../../sub_projs/mod-survey/findings/skse-menu-framework-3.md)，F1 面板出現 `Scene Capture Bridge` section，`Export player cell` 按鈕與 F10 走同一支 `ExportPlayerCellToFile()`。軟相依（`GetModuleHandleW` 探測，import 表無框架），沒裝框架只剩 hotkey。⚠️ `sse-imgui` 在 AE 不能用，理由見 finding。

## P1 統一 marker 系統（Idea #24 遊戲內編輯器 MVP）· IN-GAME 2026-07-10

「玩家在遊戲裡指一個地方說要什麼 → AI 把它變成 mod」端到端閉環，含最後一哩目視確認：

1. **標記**：hotkey 在玩家腳下放發光召喚圈 proxy（`SCB_MarkerACTI`＝vanilla `Magic\SummonTargetFX.nif`，工具 esp 缺席時 fallback vanilla base）；F1 面板改名/改 kind/刪。使用者實機標 `goat`（Tamriel）。
2. **匯出**：F10 → `scene.json` 的 `annotations[]`（advisory 段，ModForge build 永不生成、只 log）。實測 3 筆全對：label/kind 編輯生效、angleZ 度數、worldspace 歸位、proxy 排除數=3（editor chrome 不漏進 placements）。
3. **agent authoring**：agent 讀 annotations → spec（`EncGoatDomestic` + marker 座標/朝向）→ build → `PlacedNpc @ (116031.1, 111485.6, -7744)` cell(28,27) 自動歸位，master 僅 Skyrim.esm。
4. **目視**：使用者進遊戲在標記處看到山羊。

實戰教訓（皆已記 plan）：**F9 是 vanilla 快速讀檔**（sink 只觀察不吞鍵，遊戲同時處理→讀檔抹掉剛放的 proxy）→ 改 F11 + 面板放置鈕；**外部 NPC base 必須明示 `kind:"npc"`**（isNpc 自動判定只認 in-spec base，落成 REFR 不生怪——dump 看到 PlacedObject 才抓到）；kPostLoadGame → PruneDeadProxies。MO2 refresh 後新 mod 資料夾預設不勾。

## P2 編輯器三件套 + P3 物理凍結 · IN-GAME 2026-07-11（Winterhold）

一輪實測全過，含匯出→build→patch 實機生效的完整閉環：

1. **F11 準星放 marker**：兩發都落在射線命中點——`Aim.cpp` pitch 符號正確（先前唯一未驗的射線疑點結案）。
2. **F8 橡皮擦**：vanilla 長凳 ref 標記→面板 undo 復原→再標記；匯出進 `removals[]`。
3. **F6/F7 滴管**：吸 `NobleBench01`（含 60.2° 旋轉）→ F7 擺在準星處，姿態跟著走。
4. **numpad 編輯**：5 選中、3 升高（+55 units）、0 commit、`.` 取消還原全部實證；**無任何未映射 numpad 鍵**——log 裡 unmapped `0x11/0x1F/0x20/0x38` 是編輯中按的 WASD/Alt（無害噪音，之後可限縮 log 範圍）。
5. **物理凍結**（P3）：選中掉落的 `StaffMagelight`（HavokMovable）→ `physics frozen` → 抬升 → commit → `physics restored` → 沉降；**匯出的是沉降後姿態**（rotation x=146.6° 躺平），符合「live pose 匯出」語意。
6. **閉環**：`scene-export.json`（2 placements + 2 annotations + 1 removal）→ `build` → 7-record esp（removal override 深埋 Z−30000、Tamriel override 自動帶 TopCell 0xD74、ESL、master 僅 Skyrim.esm）→ 使用者實機：**擦的長凳永久消失、擺的長凳＋躺平法杖出現**。
7. **持久化**：遊戲內 save→load 後擦除維持、markers 原地——disable 狀態與動態 ref 都住在存檔（語意詳見 [sub_projs/scene-capture-bridge/README](../../../sub_projs/scene-capture-bridge/README.md)「持久化與 adopt 語意」）。

意外收穫：被抬的法杖根本不是 DLL 擺的（玩家丟在地上的裝備）照樣進 placements——vanilla diff 的無狀態判別（動態 ref＝玩家所為）涵蓋所有玩家操作，不限本工具。

殘項（open-only 慣例，列在 [wait_todo/ingame-tests.md](../../../wait_todo/ingame-tests.md)）：跨行程 adopt（關遊戲重開）＋三個零星未實證小項。
