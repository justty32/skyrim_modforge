# capturedNpcs[] 消費 — Phase 1 — TESNPC 配方

← [captured-npcs-consumption](../captured-npcs-consumption.md)（入口）

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
- `BuildNpcs`（pass 1，record-local）：`Female` flag（`NpcConfiguration.Flag.Female`）、`Weight`/`Height`（有值才設）、`TextureLighting = Color.FromArgb(bodyTint)`、`FaceMorph`（**按上方鎖定表**（[terrain](terrain.md)）把 `FaceMorphs[0..17]` 填進具名欄，`Unknown` 留 0）、`FaceParts`（[0]→Nose、[1]→Unknown、[2]→Eyes、[3]→Mouth）、`TintLayers`（逐層 `new TintLayer{Index=(ushort),Preset=(short),InterpolationValue,Color}`）。
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

**T5 — 測試**（新檔 `tests/ModForge.Core.Tests/Build/CapturedNpcsTests.cs`，模板＝`CapturedItemsTests.cs`）
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
