# capturedNpcs[] 消費 — 地形（2026-07-11 探勘固化，Mutagen 0.53.1）

← [captured-npcs-consumption](../captured-npcs-consumption.md)（入口）

## 地形（2026-07-11 探勘固化，Mutagen 0.53.1）

### 現況：ModForge 今天寫的 facegen ＝ 零
`NpcSpec`（`src/ModForge.Core/Spec/Spec.Actors.cs:5`）**已有**：`Race`、`Unique`/`Essential`/`Protected`、`Outfit`、`Perks`、`Class`、`Level`、`AutoCalcStats`、`Packages`、`Spells`、`Items`、AI 態度欄、`Greeting`。
**完全沒有**：`Female`/性別、`Weight`、`Height`、`BodyTint`、`HairColor`、`FaceTexture`(FTST)、`HeadParts`、`TintLayers`、`FaceMorphs`、`FaceParts`、`Dead`、`ActiveEffects`。

`BuildNpcs`/`WireNpcs`（`src/ModForge.Core/Build/Generator.Build.Actors.cs`，pass1 `:9-45`、pass2 `:120-191`）設 EditorID/Name/Level/flags(AutoCalc/Unique/Essential/Protected)/AIData/Race/Class/DefaultOutfit/Voice/CrimeFaction/CombatStyle/ActorEffect/Items/Factions；perk 在 `Generator.Build.Perks.cs:169-177`。**臉部一律不碰**（連 `Female` flag 都沒設）。

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
