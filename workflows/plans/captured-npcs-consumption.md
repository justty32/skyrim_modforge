# capturedNpcs[] 消費 — Implementation Plan（Idea #24 擷取器 · ② NPC 外貌）

← [plans](README.md)｜spec：[ingame-scene-export-design.md](../specs/ingame-scene-export-design.md)（共用）｜idea：[#24](../idea/tools/24-ingame-editor.md)｜子專案：[scene-capture-bridge](../../sub_projs/scene-capture-bridge/README.md)｜姊妹（已落地）：`capturedItems[]`（commit `1bed5dd`）

**Goal:** 讓 scene-capture-bridge DLL `sc cap` 吸到的 `capturedNpcs[]`（活體 actor 的 TESNPC 外貌/身份）能被 ModForge `build` 成一個真的 NPC_ 記錄 + 一個 ACHR placement，讓那個 NPC 出現在世界裡。這是擷取器消費的 ②（① items 已完成）。

> **本檔是給下一個 session（可能是 fable）接手的設計交接**。探勘結論（2026-07-11）已固化在下方「地形」節，不必重跑。

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

**⚠️ 最需驗證的一點**：DLL 匯出的 `faceMorphs[]` 是 **RE::TESNPC::FaceData::Morphs 順序的 float 陣列**（0-17，18=kUnk 已排除），但 Mutagen `NpcFaceMorph` 是**具名欄**。**兩邊 index→欄位對應必須逐一比對驗證**（不可假設同序）——做法：Mutagen overlay 讀一個已知 vanilla NPC 的 `FaceMorph` 具名值，對照 DLL 吸同一 NPC 匯出的陣列，確立 index↔名 map。`faceParts[]`(4 int) 同理對 `NpcFaceParts`。這是 Phase 1 的硬驗收點。

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

### Phase 1 — TESNPC 配方（本計畫主體，離線可完成 + 測）
1. **Schema**（`Spec.Actors.cs`）：給 `NpcSpec` 加 `Female`、`Weight`、`Height`、`BodyTint`(RgbSpec)、`HairColor`(ref+RgbSpec)、`FaceTexture`(FTST ref)、`HeadParts`(List\<ref\>)、`TintLayers`(List\<TintLayerSpec\>)、`FaceMorphs`(List\<float\>)、`FaceParts`(List\<int\>)。新增 `Spec.CapturedNpcs.cs`：`CapturedNpcSpec`（對齊 DLL json：全外貌欄 + base/dead/activeEffects/position/rotation/cell/worldspace）+ `TintLayerSpec`/`RgbSpec`。
2. **`ModSpec.CapturedNpcs`** + guard flag（`Spec.cs`，比照 CapturedItemsExpanded）。
3. **Build**（`Generator.Build.Actors.cs`）：BuildNpcs/WireNpcs 新設 `Weight`/`Height`/`Female` flag/`TextureLighting`(QNAM)/`HairColor`/`HeadTexture`(FTST)/`HeadParts`/`TintLayers`/`FaceMorph`/`FaceParts`。**先做 index→NpcFaceMorph 映射驗證**（見地形⚠️）。
4. **`Generator.CapturedNpcs.cs` `ExpandCapturedNpcs`**（掛進 `ExpandMacros`）：每筆 → `NpcSpec`(外貌 + `AutoCalcStats`? 需搭 class 否則 0 HP，見 memory `autocalc-without-class-dead-npc`——captured 沒 class，**預設不開 AutoCalcStats**，用 captured 或預設 level) + `PlacementSpec`(Base、Cell/Worldspace、Position、Rotation)。editorId `MFCapNpc_<name>_<i>`。
5. **Validate**（`Generator.Validate.Npcs.cs` 或新 partial）：weight 0-100、tint color byte、headParts/hairColor/faceTexture ref 格式、faceMorphs 數量（18）、faceParts 數量（4）、race 非空。
6. **測試** `CapturedNpcsTests.cs`：離線 validate + expand（appearance 欄有進 NpcSpec、placement 有生、editorId 唯一、idempotent、mint 不需 Skyrim）；build 設 facegen 欄的斷言（讀回 Npc.Weight/FaceMorph/TintLayers…）。RequiresSkyrim 僅在需要 resolve race 記錄的斷言時。

### Phase 2 — 烘焙臉（後面里程碑，本計畫不實作，只記界線）
- FaceGeom `.nif` + facetint `.dds` 產生。需 CK `FaceGen` 或外部工具（見 SESSION-LOG「三路已評估，推薦 A：烘 NIF+DDS 資產、產物自足」）。這是 `package` 階段的資產產出，非 build。**開新 plan / idea 再談**。

---

## 開放問題（給使用者/fable）

- **Q1（分段接受度）**：Phase 1 只寫配方 → 臉可能灰/暗臉直到 Phase 2 烘焙。可以先接受「身份對、身形/髮色對、臉待烘焙」的中間態嗎？（若否，得先把 Phase 2 烘焙一起排，工程大很多。）
- **Q2（unique 處理）**：captured unique NPC 預設 MINT 分身；要不要提供 override-本尊 路線（改 vanilla 臉、需 RequiresSkyrim、動 vanilla 記錄）？建議先只 MINT。
- **Q3（faceMorph 驗證素材）**：需要一次實機——吸一個**已知 vanilla NPC**（臉 morph 非全 0，如 Lydia/某獨特 NPC）匯出 json，我拿來對 Mutagen overlay 讀同一 NPC 的 `FaceMorph` 具名值，鎖定 index↔名 map。這步在 Phase 1 實作中會需要。
- **Q4（activeEffects/dead）**：確認先不消費（advisory 帶著）。

---

## 相依 / 前情
- 姊妹 `capturedItems[]` 已落地（`Generator.CapturedItems.cs`、`Spec.CapturedItems.cs`、`CapturedItemsTests.cs`，commit `1bed5dd`）——**這是最佳抄襲模板**。
- DLL 端 capturedNpcs 匯出已成形（`sub_projs/scene-capture-bridge/src/SceneExporter.cpp:269-316`、`Captures.cpp ReadNpc`）；faceMorphs 尾槽 FLT_MAX 已修（commit `82b368f`，18 值）。
- OPEN-B（PROTEUS 是否寫 TESNPC）未驗——若 PROTEUS 走 live NiNode override 不寫 TESNPC，capturedNpcs 對 PROTEUS clone 會吸到 base 臉（本計畫無影響，但影響 PROTEUS 用例的價值）。
