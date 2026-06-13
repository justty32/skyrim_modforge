# VIGILANT 獨立地圖／worldspace 解碼（2026-06-13）

> 解碼對象：`Vigilant.esm`（~21MB）的**獨立 worldspace／自訂地圖**。離線、僅讀本檔自身 record，跨檔引用一律以原始 FormKey 呈現（**不解析進 Skyrim.esm / Update.esm**）。探測碼為一次性 probe（已清理）。

## record 數量（先看這裡）

| 項目 | 數量 | 備註 |
|------|------|------|
| Worldspace 總數（出現在本檔） | 13 | 其中 2 個是 vanilla override（`Tamriel 0x00003C`、`EldergleamSanctuaryWorld 0x03A9D6`）|
| **NEW worldspace（Vigilant 自訂）** | **11** | 下表逐一列 |
| 外部 cell（11 個新 ws 合計，含 LAND） | **5344** | 其中 ~5000+ 帶 LAND；Coldharbour 一家就佔 4225 |
| 新 ws 內放置 ref（含 worldspace top persistent cell） | **~30137** | sampled，非全展開 |
| 內部 cell（Vigilant 自訂 interior，`m.Cells`） | **138** | realm 內的房屋／洞窟／聖堂等 |
| 指向新 worldspace 的 load-door（XTEL teleport） | **94**（總 teleport door 370） | 入口機制，見下 |
| 新 ws 內 map marker（base `0x000010`） | **47** | 多數 `canTravel=0`（禁快旅） |
| Vigilant 自訂 CLMT | 7 | 每個 realm 一個自家 climate |
| Vigilant 自訂 WTHR | 9 | realm 專屬天氣 |
| Vigilant 自訂 REGN | 19 | region |
| Vigilant 自訂 WATR | 10 | realm 專屬水體 |
| Vigilant 自訂 LGTM | 9 | lighting template |
| Vigilant 自訂 IMGS | 7 | imagespace |

主檔依賴（masters）：`Skyrim.esm`、`Update.esm`（**僅這兩個**；本檔很乾淨，無 DLC master 依賴）。

---

## 一、11 個獨立 realm（自訂 worldspace）

每個 worldspace 都是 `zXX...World` 命名（前綴 `zAoM`/`zBM`/`zCO`/`zCH` 對應劇情章節）。**全部都掛自己的 climate / water**，多數**無 `Parent`（= 真正獨立 realm，自有地圖座標系）**；少數掛 `Parent=Tamriel` 或 `Parent=Coldharbour`（共用父地圖但仍是分開的 worldspace record）。

| FormKey | EditorID | 顯示名 | Climate | Water | Parent | Flags | 性質 |
|---------|----------|--------|---------|-------|--------|-------|------|
| `0x023E7E` | zAoMVigilantWorld | Stuhn Ravine | `0x000812`(Skyrim) | `0x000018`(Skyrim) | **Tamriel** `0x00003C` | SmallWorld, NoLodWater, NoGrass | Tamriel 子地圖（共用天空，自有 LAND）|
| `0x035457` | zBMLamaeWorld | Lamae's Dream | `0x036E6C`(Vig) | `0x054EE8`(Vig) | — | SmallWorld, CannotFastTravel | 獨立夢境 realm |
| `0x047CFA` | zCOBruiantWorld | Bruiant's Estate | `0x047CFB`(Vig) | `0x000018`(Skyrim) | **Tamriel** | SmallWorld, CannotFastTravel | 莊園小地圖 |
| `0x0619A2` | zCOCursedWorld | Blood Curse : Envy | `0x061F13`(Vig) | `0x054EE8`(Vig) | — | SmallWorld, CannotFastTravel, NoGrass | 獨立詛咒 realm |
| `0x06C8A8` | zCHWasteland | Wasteland | `0x0731F1`(Vig) | `0x06D276`(Vig) | — | CannotFastTravel, NoLodWater, NoGrass | 獨立荒原 |
| `0x06D275` | zCHMolagWorld | **Coldharbour** | `0x06D277`(Vig) | `0x06D276`(Vig) | — | 0（可快旅，內含 marker） | **主 realm，巨大**（見下）|
| `0x078779` | zCHTrueEndWorld | Elder Field | `0x0731F1`(Vig) | `0x045D9E`(Vig) | — | CannotFastTravel, NoLodWater | 結局 realm |
| `0x0B2AEE` | zCHColosseumWorld | Colosseum | `0x06D277`(Vig) | `0x06D276`(Vig) | **Coldharbour** `0x06D275` | SmallWorld, CannotFastTravel, NoGrass | Coldharbour 子地圖（角鬥場）|
| `0x166857` | zAoMWitchWorld | Hag's Pond | `0x036E6C`(Vig) | `0x062A72`(Skyrim) | **Tamriel** | CannotFastTravel, NoGrass | Tamriel 子地圖 |
| `0x2C5DB1` | zCHOldForestWorld | Old Forest | `0x2C6034`(Vig) | `0x000018`(Skyrim) | — | SmallWorld, CannotFastTravel | 獨立森林 realm |
| `0x2DA92A` | zCHWhaleGraceyardWorld | Whale Graveyard | `0x2DA92D`(Vig) | `0x000018`(Skyrim) | — | SmallWorld, CannotFastTravel, NoLodWater, NoGrass | 獨立 realm |

觀察重點：

- **「獨立 realm」vs「Tamriel 附掛」兩種都有。** 7 個無 parent（自有座標原點與地圖）；4 個掛 `Parent=Tamriel/Coldharbour`（共用父 worldspace 的天空 LOD/地圖，但仍是獨立的可進入空間）。即使掛 Tamriel parent，仍各自指定**自家 climate**（如 Bruiant 用 `zzzCOClimateBruiantWorld`），所以天氣/光照與 Tamriel 不同。
- **大量沿用 vanilla water**（`0x000018` = vanilla DefaultWater、`0x062A72`）省工；只在需要染色的 realm（Lamae/Cursed/Coldharbour/Wasteland）才自訂 WATR。
- `SmallWorld` flag 幾乎全開（單格地圖大小的 realm 用，引擎 LOD 處理較輕）；只有巨大的 Coldharbour 與 Wasteland/Hag's Pond 沒開（要完整 LOD）。
- `CannotFastTravel` 幾乎全開（劇情 realm 不給快旅）；**唯一例外是 Coldharbour**（flags=0），它反而有 map marker 做城內導航。
- `InteriorLighting` 全部 null（這是 worldspace-level 室外，不吃 interior lighting；室內 cell 才用 LGTM）。
- `Location`/`EncounterZone` 都掛自家的（如 `0x3822xx` 一批 EncounterZone 是這些 realm 的）。

### Coldharbour（主 realm）的規模

`zCHMolagWorld 0x06D275` 是整個 mod 的核心地圖：

- MapData：`NWcell=-12,12  SEcell=12,-12` → 約 **25×25 cell 可用地圖範圍**；ObjectBounds `-32,-32` 到 `33,33` → 引擎 cell grid 約 **65×65**。
- **外部 cell 4225 個**，其中 **1025 帶 LAND**（即實際有地形的核心區），**388 帶 NAVM**，合計 **23899 個放置 ref**。
- climate `zzzCHColdharbourClimate 0x06D277` 有 **4 種 weather type**（其餘 realm 的 climate 多只掛 1 種天氣，做單調氛圍）。
- 內含 **74 個 load-door 入口**（mod 中最多）+ 多個 map marker（Barrier Tower of Agea / Tower of Sancremor / Shrine of Kyne / Statue of Molag Bal …），像一座可探索的城市。

其餘 realm 規模參考（外部 cell / 帶 LAND / 帶 NAVM / 總 ref）：

```
Stuhn Ravine        90 / 90  / 4  / 238
Lamae's Dream      132 / 132 / 11 / 1021
Bruiant's Estate    99 / 99  / 3  / 420
Blood Curse: Envy  124 / 121 / 9  / 101
Wasteland          127 / 121 / 2  / 80
Coldharbour       4225 /1025 /388 /23899   ← 主 realm
Elder Field        169 / 169 / 26 / 1203
Colosseum           81 / 81  / 1  / 323
Hag's Pond          92 / 90  / 13 / 732
Old Forest          81 / 81  / 5  / 531
Whale Graveyard    124 / 121 / 9  / 298
```

**幾乎每個外部 cell 都帶 LAND**（自訂地形 heightmap），但 **NAVM 是稀疏的**——只有 NPC 實際會走的核心 cell 才鋪 navmesh（如 Coldharbour 388/4225 ≈ 9%）。這跟 ModForge 既有 navmesh memory 一致：navmesh 只鋪在需要 AI pathing 的地方。每個 cell 的 `LightingTemplate=Null`、`ImageSpace=空`——**室外 cell 不逐格設 LGTM/IMGS，光照由 worldspace 的 climate/weather 決定**（這正是 CLAUDE.md「LGTM/CELL 室內專用，室外光由 weather 決定」的實證）。

---

## 二、入口機制：load-door teleport（XTEL）

**player 進入 realm 的方式 = 標準載入門（PlacedObject 帶 `XTEL` TeleportDestination）**，沒有用特殊的 coc-only marker 當主要入口。

- 全 mod 共 **370 個 teleport door**，其中 **94 個指向新 worldspace**。
- **目的地門幾乎全部存在 worldspace 的 top-level persistent cell（worldspace「0,0 持久格」）**，不是普通的格內 temporary ref。這跟引擎慣例一致：跨 cell 的目的地門要放 persistent，才能在玩家進入前就存在被 teleport 指到。
- 分佈（指向各 realm 的入口數）：

```
Coldharbour      74   ← 城市型，多入口
Stuhn Ravine      7
Bruiant's Estate  6
Hag's Pond        2
Lamae's Dream     2
Blood Curse Envy  1
Elder Field       1
Colosseum         1
```

**入口的「來源側」門在哪？** 94 個來源門中：

- **82 個來自 Vigilant 自家的 interior cell**（`zzzCH...`/`zzzCO...` 房屋、洞窟、聖堂等，如 `zzzCHLipSandCave`→Coldharbour、`zzzCOUnderMansion`→Cursed realm）。也就是說 realm 的探索是「realm 室外 ↔ realm 內部 interior」互通的雙向門網，realm 本身就是一個完整的內外連通空間。
- **12 個來自非-Vigilant-interior**（Tamriel 室外 / realm 外部 cell 自身）——這些是少數「從 Skyrim 本土第一次踏進 realm」的入口門（劇情觸發點）。

配合 **47 個 map marker**（多 `canTravel=0`，只當定位標籤不給快旅；Coldharbour 的才可旅行），realm 內導航靠的是手放 marker + 雙向 load door，**沒有依賴程式生成的 portal**。

---

## 三、自訂氣候／天氣／region（realm 氛圍）

- **每個 realm 一個自家 CLMT**（7 個）：`zzzCHColdharbourClimate`、`zzzBMClimateLamaeWorld`、`zzzCOCursedClimate`、`zzzCHWastelandClimate`、`zzzCHClimateOldForest`、`zzzCOClimateBruiantWorld`、`zzzCHClimateWhaleGraveyard`。多數 climate **只掛 1 種 weather**（單調、固定氛圍，如永夜/血色天），唯 Coldharbour 掛 4 種做變化。
- **9 個自訂 WTHR**：`zzzCHMolagWeather` / `zzzCHMolagFogWeather`（霧）/ `zzzCHMonoClearWeather`（單色清空）/ `zzzCHOldForestStormRain`（暴雨）等——用天氣的 fog/sky/sunlight 顏色塑造 realm 的視覺基調，這正是 CLAUDE.md「室外光由 weather sky/sunlight/ambient 決定」的做法。
- **19 個 REGN**：realm 內的 region（區域音樂/天氣/生怪/草），但本次未深挖 region→weather 綁定（CLAUDE.md 既有 weather/IMGS 掛 region 標為「未做」，VIGILANT 這 19 個 region 可作為將來要實作該功能時的參考樣本）。
- **138 個 interior cell**：realm 內的室內空間，**用 LGTM + IMGS**（如 `zzzCOUnderMansion` 用 `0x04CB6F`(Vig LGTM)、`zzzCHSlumToSewer` 用 `0x0F27E0`(Vig LGTM)+`0x0A2687`(Skyrim IMGS)；也有直接沿用 vanilla LGTM 的如 `0x0345A2`）。室內走 LGTM/IMGS、室外走 climate/weather 的分工非常清楚。

---

## 四、ModForge 對照（今天能複製 vs 缺口）

| VIGILANT 技法 | ModForge 現況 | 結論 |
|---------------|---------------|------|
| 自訂獨立 worldspace（無 parent，自有座標/地圖） | `worldspaces[]` 已支援自訂 worldspace + cells | ✅ 可表達。需確認 spec 能設 `MapData`（NW/SE cell coords）與 `Flags`（SmallWorld/CannotFastTravel/NoGrass/NoLodWater）。|
| worldspace 掛 `Parent=Tamriel/其他` 做子地圖 | 視 spec 是否暴露 `parent` 欄位 | ⚠️ **可能缺口**：需確認 `WorldspaceSpec` 有 `parent`(worldspace FormKey/ref) 欄位；無則新增（optional，安全）。|
| worldspace 掛自家 climate/water/lodWater | climate/weather 管線已落地（CLMT 隱含於 weather？）+ water 由 lighting 管線涉及 | ⚠️ 需確認 worldspace 能直接引用 `climate`/`water`/`lodWater` FormKey（in-spec 或 vanilla ref）。CLAUDE.md 已有 WeatherSpec/IMGS，但**worldspace→climate 綁定**要確認。|
| 每外部 cell 帶 LAND（heightmap 地形） | navmesh memory 證實 ModForge 能建自訂 worldspace 的 cells；LAND 生成需確認 | ⚠️ **關鍵缺口候選**：5000+ cell 的 LAND（heightmap）是 VIGILANT 的主體。ModForge 若只生 flat/無 LAND cell，realm 會是「無地形空殼」。需確認 `cells[].land`/heightmap 生成能力。|
| 稀疏 navmesh（只鋪 AI 走的核心 cell） | ModForge 已支援自訂 worldspace NAVM（programmatic navmesh memory，含 NAVI additive override） | ✅ 可表達。VIGILANT 的「只在核心 cell 鋪 navmesh」是好的最佳實踐參考（不必每 cell 都鋪）。|
| load-door 入口（XTEL teleport 到 realm，目的地門放 worldspace top-persistent cell） | ModForge「teleport doors」已落地 | ✅ 可表達。**踩坑提醒**：目的地門必須是 **persistent**（放 worldspace 0,0 持久格），否則跨 cell teleport 進入前目的地門不存在。建議 ModForge teleport-door 產生器預設把 realm 側目的地門標 persistent。|
| 雙向 realm-室外↔realm-interior 門網 | 兩端都是 ModForge 能放的 door | ✅ 可表達（就是兩個互指的 teleport door）。|
| map marker（base `0x000010`，`canTravel` flag 控快旅） | placements 能放任意 base ref | ✅ 可表達（放 base=`Skyrim.esm:0x000010` 的 PlacedObject + MapMarker 子資料 + 名稱 + flags）。需確認 spec 能設 MapMarker 的 `name`/`canTravel` flag；若 placements 只設座標/base，**MapMarker 命名/旗標可能缺口**。|
| 自訂 CLMT（每 realm 一個，掛 1~4 weather） | weather 管線有，CLMT 是否為一級 spec record 待確認 | ⚠️ 需確認 ModForge 有 `ClimateSpec`（CLMT）。CLAUDE.md 列了 WeatherSpec 但未明列 ClimateSpec；worldspace 要掛 climate 才能定 realm 全域天空，**ClimateSpec 可能缺口**。|
| 自訂 WTHR（fog/sky/sunlight 染色 realm） | `WeatherSpec`（+ template 抄 vanilla）已落地 | ✅ 可表達。VIGILANT「單一固定天氣塑造氛圍」的做法直接適用。 |
| realm interior 用 LGTM+IMGS | 光照管線（LGTM/IMGS/CELL XCLL/DALC）已 in-game 確認 | ✅ 完全可表達。 |
| region-driven weather（REGN，19 個） | CLAUDE.md 標「weather/IMGS 掛 region = 未做」 | ❌ **已知缺口**。VIGILANT 用 region 但本 realm 主氛圍其實靠 climate/weather 就成立；region 為 nice-to-have。|

### 給 ModForge 的優先建議

1. **先確認三個 worldspace-level 綁定欄位**：`parent`、`climate`、`water/lodWater`。獨立 realm 的「天空與地圖獨立性」全靠這三者；缺任一則 realm 會繼承 Tamriel 天空或無水。
2. **LAND/heightmap 生成是最大的潛在缺口**——VIGILANT 的 realm 主體是逐 cell 的自訂地形。確認 ModForge 能否從 spec 產 LAND；若只能產平地/空 cell，這是要補的核心能力。
3. **teleport door 目的地門標 persistent + 放 worldspace 0,0 持久格**：這是 realm 入口能正確運作的引擎硬性要求，建議在 ModForge teleport 產生器內建。
4. **MapMarker 的 name/canTravel**：realm 內導航體驗需要，確認 placements 能設這兩者。
5. ClimateSpec（CLMT）若尚無，補一個一級 record（worldspace 掛 climate 的前置）。

---

## 附註

- 本次全離線，未載入任何 master、未建 link cache、未讀 BSA；所有跨檔引用以原始 FormKey 標 `(Skyrim)`/`(Vig)`。
- 計數方式：worldspace block tree 逐層 enumerate（未 `.ToList()` 整組），teleport/marker 用 ref→container 索引比對，皆有界。
- probe 已清理，`/tmp/vigilant/Vigilant.esm` 未動。
