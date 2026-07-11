# capturedNpcs[] 消費 — Implementation Plan（Idea #24 擷取器 · ② NPC 外貌）

← [plans](README.md)｜spec：[ingame-scene-export-design.md](../specs/ingame-scene-export-design.md)（共用）｜idea：[#24](../idea/tools/24-ingame-editor.md)｜子專案：[scene-capture-bridge](../../sub_projs/scene-capture-bridge/README.md)｜姊妹（已落地）：`capturedItems[]`（commit `1bed5dd`）

**Goal:** 讓 scene-capture-bridge DLL `sc cap` 吸到的 `capturedNpcs[]`（活體 actor 的 TESNPC 外貌/身份）能被 ModForge `build` 成一個真的 NPC_ 記錄 + 一個 ACHR placement，讓那個 NPC 出現在世界裡。這是擷取器消費的 ②（① items 已完成）。

> **狀態：Phase 1（T1–T6）已落地 2026-07-11**（離線 912 測綠含 15 新測；`CapturedNpcsTests.cs`）→ 實機驗收見 [wait_todo OPEN-E](../../wait_todo/ingame-tests.md)。本檔保留作 Phase 2（烘焙）的界線記錄與映射表出處。探勘結論已固化在下方「地形」節，不必重跑。

---

## 最關鍵的裁決：TESNPC「配方」現在做 vs 烘焙臉後面做

Skyrim 的臉有兩層：
1. **TESNPC 記錄裡的「配方」**：race / 性別 / weight / height / head parts / tint layers / face morphs(NAM9) / face parts(NAMA) / 髮色(HCLF) / 臉貼圖(FTST) / 膚色(QNAM)。← **本計畫 Phase 1 寫這層**。
2. **烘焙產物**：`FaceGeom/<plugin>/<formid>.nif`（頭網格）＋ `facetint` DDS（臉貼圖烘焙）。vanilla NPC 都附這兩個檔；**沒有它們 → 遊戲會出灰臉/暗臉/脖縫 bug**。← **後面里程碑**（SESSION-LOG 已列，需 CK / 外部工具烘焙，非 Mutagen 能生）。

**含義（務必寫進交付說明）**：Phase 1 產出的 NPC **會出現、身份/perk/outfit/旗標正確、身形(weight/height)對、髮色/膚色對**，但**臉可能是灰/暗臉**，直到 Phase 2 烘焙 FaceGeom+facetint。這跟 `capturedItems` 不同（items 沒有烘焙層，一次到位）；NPC 天生兩段。**先確認使用者接受這個分段**（見「開放問題」Q1）。

---

## 地形（2026-07-11 探勘固化，Mutagen 0.53.1）

### 現況：ModForge 今天寫的 facegen ＝ 零
`NpcSpec`（`src/ModForge.Core/Spec.Actors.cs:5`）**已有**：`Race`、`Unique`/`Essential`/`Protected`、`Outfit`、`Perks`、`Class`、`Level`、`AutoCalcStats`、`Packages`、`Spells`、`Items`、AI 態度欄、`Greeting`。
**完全沒有**：`Female`/性別、`Weight`、`Height`、`BodyTint`、`HairColor`、`FaceTexture`(FTST)、`HeadParts`、`TintLayers`、`FaceMorphs`、`FaceParts`、`Dead`、`ActiveEffects`。

`BuildNpcs`/`WireNpcs`（`src/ModForge.Core/Generator.Build.Actors.cs`，pass1 `:9-45`、pass2 `:120-191`）設 EditorID/Name/Level/flags(AutoCalc/Unique/Essential/Protected)/AIData/Race/Class/DefaultOutfit/Voice/CrimeFaction/CombatStyle/ActorEffect/Items/Factions；perk 在 `Generator.Build.Perks.cs:169-177`。**臉部一律不碰**（連 `Female` flag 都沒設）。

### Mutagen 0.53.1 `Npc` 臉部 API（已對安裝的 DLL 確認 property 名）
| 擷取概念 | 子記錄 | Mutagen `Npc` property | 型別 |
|---|---|---|---|
| face morphs | NAM9 | `Npc.FaceMorph` | `NpcFaceMorph?`（**具名 float 欄**，非陣列）|
| face parts | NAMA | `Npc.FaceParts` | `NpcFaceParts?`（`Nose`/`Eyes`/`Mouth`+`Unknown`）|
| tint layers | TINI/TINC/TINV/TIAS | `Npc.TintLayers` | `ExtendedList<TintLayer>`（`TintLayer{Index,Preset,InterpolationValue,Color}`）|
| head parts | PNAM | `Npc.HeadParts` | `ExtendedList<IFormLinkGetter<IHeadPartGetter>>` |
| 髮色 | HCLF | `Npc.HairColor` | `IFormLinkNullable<IColorRecordGetter>` |
| 臉貼圖組 | FTST | `Npc.HeadTexture` | `IFormLinkNullable<ITextureSetGetter>`（**property 叫 `HeadTexture` 不是 FaceGenTextureSetHead**）|
| 膚/身色調 | QNAM | `Npc.TextureLighting` | `Color` |
| weight | NAM7 | `Npc.Weight` | `float`（0-100）|
| height | NAM6 | `Npc.Height` | `float` |
| 性別 | ACBS | `Npc.Configuration.Flags \|= NpcConfiguration.Flag.Female` | flag |

**✅ index↔具名欄映射已離線鎖定（2026-07-11 規劃回合，結構比對）**：CommonLibSSE `RE::TESNPC::FaceData::Morphs` 枚舉（本 repo vendored `TESNPC.h:137-163`）與 Mutagen `NpcFaceMorph` property 宣告序**逐一同序**（兩邊都是 NAM9 按檔案序反序列化）；`FaceData::Parts`（kNose=0/kUnknown=1/kEyes=2/kMouth=3）同樣對上 `NpcFaceParts`（Nose/Unknown/Eyes/Mouth）。DLL 匯出 `faceMorphs[]` 0–17（kUnk=18 已排除）、`faceParts[]` 4 int。實機吸一個 vanilla NPC 對照 Mutagen overlay 讀值＝可選的抽查（belt-and-suspenders），**不再是 blocking 驗證點**。

| idx | CommonLibSSE 枚舉 | Mutagen `NpcFaceMorph` 欄 |
|---|---|---|
| 0 | kNose_LongShort | `NoseLongVsShort` |
| 1 | kNose_UpDown | `NoseUpVsDown` |
| 2 | kJaw_UpDown | `JawUpVsDown` |
| 3 | kJaw_NarrowWide | `JawNarrowVsWide` |
| 4 | kJaw_ForwardBack | `JawForwardVsBack` |
| 5 | kCheeks_UpDown | `CheeksUpVsDown` |
| 6 | kCheeks_ForwardBack | `CheeksForwardVsBack` |
| 7 | kEyes_UpDown | `EyesUpVsDown` |
| 8 | kEyes_InOut | `EyesInVsOut` |
| 9 | kBrows_UpDown | `BrowsUpVsDown` |
| 10 | kBrows_InOut | `BrowsInVsOut` |
| 11 | kBrows_ForwardBack | `BrowsForwardVsBack` |
| 12 | kLips_UpDown | `LipsUpVsDown` |
| 13 | kLips_InOut | `LipsInVsOut` |
| 14 | kChin_NarrowWide | `ChinNarrowVsWide` |
| 15 | kChin_UpDown | `ChinUpVsDown` |
| 16 | kChin_UnderbiteOverbite | `ChinUnderbiteVsOverbite` |
| 17 | kEyes_ForwardBack | `EyesForwardVsBack` |
| （18 kUnk） | DLL 排除 | `Unknown` 留 0 |

其它已驗細節：`TintLayer` 欄位為 nullable（`Index:ushort?`、`Preset:short?`、`InterpolationValue:float?`、`Color:System.Drawing.Color?`——DLL 匯出 rgba 全對得上）；DLL `AnchorOf` 給的 cell/worldspace 是 durable ref `<master>:0xFORMID`（`SceneExporter.cpp:57-69`），與 `PlacementSpec.Cell/Worldspace` 接受格式直接相容。

### 造 NPC+placement 的載體（複製 `capturedItems` 那套）
- pass-0 macro：`Generator.SceneNpcRoles.cs:9` `ExpandMacros`，順序 `:11-15`（…→`ExpandCapturedItems`）。新 `ExpandCapturedNpcs` 掛在 `:15` 之後。
- placement：`spec.Placements.Add(new PlacementSpec{ Base=<npcEd>, Kind="npc", Cell/Worldspace, Position, Rotation })`（`Spec.World.cs:64`；ExpandLivingNpcs `:124-128`、ExpandSettlements `:70-73` 是範例）。
- 直接模板＝`Generator.CapturedItems.cs:20`（讀 `spec.CapturedItems` → `spec.Weapons.Add(...)`）；NPC 版就是 `spec.Npcs.Add(new NpcSpec{...外貌...})` + `spec.Placements.Add(...)`。

### unique NPC 的 override 路線（可選、需 RequiresSkyrim）
`Generator.Build.NpcPatches.cs:26-27` 的 `new Npc(src.FormKey,…)+DeepCopyIn(src)` 是「忠實 override 既有 NPC」路徑（需 master cache）。若某 captured unique 想**改寫 vanilla 本尊的臉**而非另生分身，走這條。**但這會動 vanilla 記錄，風險高**；預設不走（見 Q2）。

### 其它
- **烘焙臉程式碼＝無**（確認）。`Spec.CapturedItems.cs:10-11` 註解已明載 capturedNpcs 需 schema 成長＋facegen 烘焙。
- **RequiresSkyrim 界線**：MINT 新 NPC 帶 race ref＝**不需**（FormLink 寫 unresolved，`NpcTests.cs:105` 為證）；unique-override（DeepCopyIn）＝**需要**。
- validation：`Generator.Validate.Npcs.cs`（`ValidateNpcs :8`、`ValidateNpcPatches :173`），臉部欄目前無檢查。
- 測試：`tests/…/NpcTests.cs`（`BuildNpc` helper `:15`，flags/inventory 覆蓋）。

---

## 設計裁決（本計畫提案，使用者/fable 可否決）

1. **MINT-new 為預設路徑**。captured NPC → 全新 NpcSpec + ACHR placement（大眾臉/PROTEUS clone/一般 actor 都走這）。不需 RequiresSkyrim。unique-override 是可選加值（Q2）。
2. **外貌欄加在 `NpcSpec` 本身**（不是只塞進一個 capture-only DTO）——因為 weight/morphs/tints/headParts 本來就是「手寫一個 NPC」該有的欄，加上去等於讓 ModForge 具備一般的臉部 authoring 能力，capturedNpcs 只是第一個消費者。**再加一個薄 `CapturedNpcSpec`** 承載 capture 專屬的東西（position/rotation/cell、base、dead、activeEffects），`ExpandCapturedNpcs` 把它拆成 NpcSpec(外貌)+PlacementSpec。← 對稱於 capturedItems（薄 capture DTO → 既有 record spec）。
3. **`dead` 不建**（NPC_ 無此欄；屬 ACHR「Starts Dead」placement 概念）——先當 advisory 帶著，不消費。若要，之後在 PlacementSpec 加 `startsDead`。
4. **`activeEffects` 不消費**（runtime buff 快照，非 durable trait）——保留在 json，Phase 1 不碰。未來若要，filter `duration>0` 再議。
5. **共用值型別**：`TintLayerSpec{Index,Preset,Value,Color{R,G,B,A}}`、`RgbSpec`（bodyTint/hairColor 用）。faceMorphs 存 `List<float>`（忠實轉錄 DLL 陣列，BuildNpcs 按驗證過的 index→NpcFaceMorph 具名欄映射），faceParts 存 `List<int>`。

---

## 分階段任務

### Phase 1 — TESNPC 配方（本計畫主體，離線可完成 + 測；2026-07-11 fable 細化至動工級）

DLL 匯出的 json 形狀（`SceneExporter.cpp:269-316` verbatim，schema 必須逐欄對齊）：

```jsonc
{ "name": "...", "base": "<master>:0xID",          // origin NPC_（durable 才有；advisory）
  "race": "<master>:0xID", "female": true,
  "unique": true, "essential": true, "protected": true, "dead": true,   // 省略=false
  "weight": 50.0, "height": 1.0,
  "bodyTint": {"r":230,"g":180,"b":160},
  "hairColor": {"id":"<master>:0xID","r":80,"g":60,"b":40},   // id=CLFM ref；rgb=advisory
  "faceTexture": "<master>:0xID",                   // FTST
  "defaultOutfit": "<master>:0xID",
  "headParts": ["<master>:0xID", "..."],
  "tintLayers": [{"index":1,"preset":0,"value":1.0,"color":{"r":..,"g":..,"b":..,"a":..}}],
  "faceMorphs": [/* 18 floats, idx 0-17 */], "faceParts": [/* 4 ints */],
  "perks": [{"perk":"<master>:0xID","rank":1}],
  "activeEffects": [{"magicEffect":"...","magnitude":..,"duration":..,"elapsed":..,"source":"..."}],  // advisory
  "position": {"x":..,"y":..,"z":..}, "rotation": {"x":..,"y":..,"z":..},   // rotation 已是度
  "cell": "<master>:0xID" /* 室內 */ 或 "worldspace": "<master>:0xID" /* 室外，二擇一 */ }
```

**T1 — Schema**（`Spec.Actors.cs` ＋ 新檔 `Spec.CapturedNpcs.cs` ＋ `Spec.cs`）
- `NpcSpec` 加一般 authoring 外貌欄（capturedNpcs 只是第一個消費者）：`bool Female`、`float? Weight`（0 是合法值→nullable，null=不寫）、`float? Height`、`RgbSpec? BodyTint`（→QNAM）、`string HairColor`（ref→CLFM）、`string FaceTexture`（ref→FTST）、`List<string> HeadParts`、`List<TintLayerSpec> TintLayers`、`List<float> FaceMorphs`（0 或 18 個）、`List<int> FaceParts`（0 或 4 個）。
- 新檔 `Spec.CapturedNpcs.cs`（比照 `Spec.CapturedItems.cs` 的檔頭註解風格）：
  - `CapturedNpcSpec`：`Name`/`EditorId`(optional)/`Base`(advisory)/`Race`/`Female`/`Unique`/`Essential`/`Protected`/`Dead`(advisory 不消費)/`Weight`/`Height`/`BodyTint`(RgbSpec)/`HairColor`(**`CapturedHairColorSpec{Id,R,G,B}`**——json 是物件，rgb advisory)/`FaceTexture`/`DefaultOutfit`/`HeadParts`/`TintLayers`/`FaceMorphs`/`FaceParts`/`Perks`(`List<CapturedNpcPerkSpec{Perk,Rank}>`)/`ActiveEffects`(advisory 不消費，型別收下以免走失)/`Position`(Vec3)/`Rotation`(Vec3)/`Cell`/`Worldspace`。
  - 共用值型別 `RgbSpec{R,G,B}`、`RgbaSpec{R,G,B,A}`、`TintLayerSpec{Index,Preset,Value,Color(RgbaSpec)}`（int + float，validate 管 0-255）。
- `Spec.cs`：`ModSpec.CapturedNpcs`（掛在 `CapturedItems` :107 之後）＋ `[JsonIgnore] internal bool CapturedNpcsExpanded`（比照 :119-120）。
- 驗證：`dotnet build` 過。

**T2 — Build/Wire 外貌**（`Generator.Build.Actors.cs`）
- `BuildNpcs`（pass 1，record-local）：`Female` flag（`NpcConfiguration.Flag.Female`）、`Weight`/`Height`（有值才設）、`TextureLighting = Color.FromArgb(bodyTint)`、`FaceMorph`（**按上方鎖定表**把 `FaceMorphs[0..17]` 填進具名欄，`Unknown` 留 0）、`FaceParts`（[0]→Nose、[1]→Unknown、[2]→Eyes、[3]→Mouth）、`TintLayers`（逐層 `new TintLayer{Index=(ushort),Preset=(short),InterpolationValue,Color}`）。
- `WireNpcs`（pass 2，refs——沿用既有 `Resolve` helper）：`HairColor`→`npcRec.HairColor.SetTo(fk)`、`FaceTexture`→`npcRec.HeadTexture.SetTo(fk)`（**property 叫 HeadTexture**）、`HeadParts`→逐一 `npcRec.HeadParts.Add(new FormLink<IHeadPartGetter>(fk))`。
- 驗證：T5 的離線 build 讀回斷言（尤其 morph 映射測試）。

**T3 — `Generator.CapturedNpcs.cs` `ExpandCapturedNpcs`**（新檔，模板＝`Generator.CapturedItems.cs`）
- guard `CapturedNpcsExpanded`；掛進 `ExpandMacros`（`Generator.SceneNpcRoles.cs:15` `ExpandCapturedItems` 之後）。
- 每筆：`ed = EditorId 明示 ?? MFCapNpc_<SanitizeEd(name)>_<i>`（1-based，比照 `CapturedItemEd`）。
- → `spec.Npcs.Add(new NpcSpec{...})`：身份（Name/Race/Female/Unique/Essential/Protected/Outfit=DefaultOutfit）＋全外貌欄（HairColor 取 `.Id`）＋ `Perks = Perks.Select(p=>p.Perk)`（**rank 不消費**——既有佈線用 perk 記錄自身 NumRanks，vanilla 多階 perk 吸到中間階會拉滿，註解記為已知限制）。**不開 AutoCalcStats**（captured 無 class → 0 HP 陷阱，memory `autocalc-without-class-dead-npc`）；Level 留 0；AI 欄不設（外貌分身預設溫馴，advisory）。
- → 有 `Cell` 或 `Worldspace` 才 `spec.Placements.Add(new PlacementSpec{Base=ed, Kind="npc", Cell/Worldspace, Position, Rotation})`（比照 `ExpandLivingNpcs` :124-128）；兩者皆空＝只鑄 NPC_ 不擺（合法，玩家可 placeatme）。
- `Dead`/`ActiveEffects`/hairColor rgb/perk rank：不消費，檔頭註解明載（no-silent-drop 原則靠註解＋validate 說明，不靠 Warn 洗版）。

**T4 — Validate**（`Generator.Validate.SceneNpcRoles.cs` 加 `ValidateCapturedNpcs`，`Generator.Validate.cs` 在 `ValidateCapturedItems` 後呼叫；`Generator.Validate.Npcs.cs` 的 `ValidateNpcs` 同步加新欄檢查）
- capture 層：`race` 空＝problem（沒 race 的 NPC_ 遊戲裡壞）；`race`/`hairColor.id`/`faceTexture`/`defaultOutfit`/`headParts[]`/`perks[].perk`/`cell`/`worldspace` 走 CheckRef/外部 ref 格式檢查；`faceMorphs` 數量非 0/18、`faceParts` 非 0/4＝problem；`weight` 超 0-100、tint/body color 分量超 0-255＝problem。
- NpcSpec 層（手寫 NPC 也受惠）：同樣的 FaceMorphs/FaceParts 數量、Weight 範圍、HairColor/FaceTexture/HeadParts ref 檢查。
- 驗證：T5 validation 測試。

**T5 — 測試**（新檔 `tests/ModForge.Core.Tests/CapturedNpcsTests.cs`，模板＝`CapturedItemsTests.cs`）
- validation（offline `[Fact]`）：missing race / bad ref / faceMorphs 數量錯 / weight 超界 / 完整合法樣本無 problem。
- expand（offline）：全欄樣本 → NpcSpec 各欄逐一斷言＋placement 生成（interior→Cell、exterior→Worldspace）；無 cell/ws → 不生 placement；同名兩筆 → editorId 唯一；idempotent；perks 只取 ref。
- build（offline——mint 帶外部 race ref 不需 master，`NpcTests.cs:105` 為證）：**morph 映射鎖定測試**＝`FaceMorphs=[0.01,0.02,…,0.18]` 18 個相異值 → 讀回 `Npc.FaceMorph` 逐一斷言具名欄＝對應值；FaceParts/TintLayers/Weight/Height/Female/TextureLighting/HeadParts/HairColor/HeadTexture 讀回斷言。
- json 載入（offline）：DLL-shaped 原樣 json 字串（含 `hairColor` 物件、`tintLayers`、`cell`）→ deserialize → expand → 斷言（防 casing/巢狀形狀走失——capturedItems 那次就是這樣端到端驗的）。
- `[Trait("Category","RequiresSkyrim")]`×1：vanilla refs（NordRace `Skyrim.esm:0x013746`＋真 headpart/hairColor ref）resolve 成功 build。
- 驗證：offline `dotnet test --filter Category!=RequiresSkyrim` 全綠（897＋新增）。

**T6 — CODE_MAP ＋ 文件**
- `CODE_MAP.npcs-packages.md`：加 captured NPCs 節（Spec.CapturedNpcs.cs / Generator.CapturedNpcs.cs / Build.Actors 外貌欄 / 測試檔）。
- `docs/`：grep capturedItems 在使用手冊的落點，同格式補 capturedNpcs（含「Phase 1 臉可能灰/暗、待 Phase 2 烘焙」的明示）；zh-TW 鏡像同步。
- SESSION-LOG ②項收斂、WAIT_USER 加實機驗收項。

**Phase 1 驗收**：離線全綠後，使用者實機——DLL `sc cap` 吸一個 NPC → 匯出 json → `build`+`package` → 進遊戲看：NPC 在吸取地點出現、性別/身形/髮色/膚色/裝備對；臉細節（morph/tint）以「可能灰臉」為預期（Q1）。順手抽查：吸一個 vanilla NPC 對照其本尊外觀＝morph 映射的實機 belt-and-suspenders。

### Phase 2 — 烘焙臉（後面里程碑，本計畫不實作，只記界線）
- FaceGeom `.nif` + facetint `.dds` 產生。需 CK `FaceGen` 或外部工具（見 SESSION-LOG「三路已評估，推薦 A：烘 NIF+DDS 資產、產物自足」）。這是 `package` 階段的資產產出，非 build。**開新 plan / idea 再談**。

---

## 開放問題（2026-07-11 使用者拍板：全照建議 → 可動工）

- **Q1（分段接受度）**：✅ **接受**——Phase 1 只寫配方，「身份對、身形/髮色對、臉待烘焙（可能灰/暗）」的中間態 OK；Phase 2 烘焙另排。
- **Q2（unique 處理）**：✅ **只 MINT 分身**——不做 override-本尊路線。
- **Q3（faceMorph 驗證素材）**：~~需要一次實機鎖定 index↔名 map~~ **已解決（2026-07-11 離線結構比對）**——CommonLibSSE `FaceData::Morphs` 枚舉與 Mutagen `NpcFaceMorph` 具名欄逐一同序（見地形節映射表），並由 T5 的 18-相異值讀回測試鎖死。實機吸 vanilla NPC 對照本尊＝可選抽查，併入 Phase 1 驗收順手做。
- **Q4（activeEffects/dead）**：✅ **先不消費**（advisory 帶著）。

---

## 相依 / 前情
- 姊妹 `capturedItems[]` 已落地（`Generator.CapturedItems.cs`、`Spec.CapturedItems.cs`、`CapturedItemsTests.cs`，commit `1bed5dd`）——**這是最佳抄襲模板**。
- DLL 端 capturedNpcs 匯出已成形（`sub_projs/scene-capture-bridge/src/SceneExporter.cpp:269-316`、`Captures.cpp ReadNpc`）；faceMorphs 尾槽 FLT_MAX 已修（commit `82b368f`，18 值）。
- OPEN-B（PROTEUS 是否寫 TESNPC）未驗——若 PROTEUS 走 live NiNode override 不寫 TESNPC，capturedNpcs 對 PROTEUS clone 會吸到 base 臉（本計畫無影響，但影響 PROTEUS 用例的價值）。
