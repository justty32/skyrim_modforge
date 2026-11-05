# capturedNpcs[] 消費 — 裁決、Phase 2 界線、開放問題、相依

← [captured-npcs-consumption](../captured-npcs-consumption.md)（入口）

## 最關鍵的裁決：TESNPC「配方」現在做 vs 烘焙臉後面做

Skyrim 的臉有兩層：
1. **TESNPC 記錄裡的「配方」**：race / 性別 / weight / height / head parts / tint layers / face morphs(NAM9) / face parts(NAMA) / 髮色(HCLF) / 臉貼圖(FTST) / 膚色(QNAM)。← **本計畫 Phase 1 寫這層**。
2. **烘焙產物**：`FaceGeom/<plugin>/<formid>.nif`（頭網格）＋ `facetint` DDS（臉貼圖烘焙）。vanilla NPC 都附這兩個檔；**沒有它們 → 遊戲會出灰臉/暗臉/脖縫 bug**。← **後面里程碑**（SESSION-LOG 已列，需 CK / 外部工具烘焙，非 Mutagen 能生）。

**含義（務必寫進交付說明）**：Phase 1 產出的 NPC **會出現、身份/perk/outfit/旗標正確、身形(weight/height)對、髮色/膚色對**，但**臉可能是灰/暗臉**，直到 Phase 2 烘焙 FaceGeom+facetint。這跟 `capturedItems` 不同（items 沒有烘焙層，一次到位）；NPC 天生兩段。**先確認使用者接受這個分段**（見「開放問題」Q1）。

---

## 設計裁決（本計畫提案，使用者/fable 可否決）

1. **MINT-new 為預設路徑**。captured NPC → 全新 NpcSpec + ACHR placement（大眾臉/PROTEUS clone/一般 actor 都走這）。不需 RequiresSkyrim。unique-override 是可選加值（Q2）。
2. **外貌欄加在 `NpcSpec` 本身**（不是只塞進一個 capture-only DTO）——因為 weight/morphs/tints/headParts 本來就是「手寫一個 NPC」該有的欄，加上去等於讓 ModForge 具備一般的臉部 authoring 能力，capturedNpcs 只是第一個消費者。**再加一個薄 `CapturedNpcSpec`** 承載 capture 專屬的東西（position/rotation/cell、base、dead、activeEffects），`ExpandCapturedNpcs` 把它拆成 NpcSpec(外貌)+PlacementSpec。← 對稱於 capturedItems（薄 capture DTO → 既有 record spec）。
3. **`dead` 不建**（NPC_ 無此欄；屬 ACHR「Starts Dead」placement 概念）——先當 advisory 帶著，不消費。若要，之後在 PlacementSpec 加 `startsDead`。
4. **`activeEffects` 不消費**（runtime buff 快照，非 durable trait）——保留在 json，Phase 1 不碰。未來若要，filter `duration>0` 再議。
5. **共用值型別**：`TintLayerSpec{Index,Preset,Value,Color{R,G,B,A}}`、`RgbSpec`（bodyTint/hairColor 用）。faceMorphs 存 `List<float>`（忠實轉錄 DLL 陣列，BuildNpcs 按驗證過的 index→NpcFaceMorph 具名欄映射），faceParts 存 `List<int>`。

---

### Phase 2 — 烘焙臉（後面里程碑，本計畫不實作，只記界線）
- FaceGeom `.nif` + facetint `.dds` 產生。需 CK `FaceGen` 或外部工具（見 SESSION-LOG「三路已評估，推薦 A：烘 NIF+DDS 資產、產物自足」）。這是 `package` 階段的資產產出，非 build。**開新 plan / idea 再談**。
- **⬇️ 優先級下修（2026-07-11 實機觀察）**：Mirabelle 分身**臉完全正常、沒灰/暗臉**。兩層原因：① 頭形——FaceGeom 缺時引擎本來就 runtime 從配方生頭（玩家臉路徑），形狀/morph 都對；② 臉色——經典暗臉的真凶是 facetint 缺/不匹配，但使用者 modlist 有 **`Face Discoloration Fix SE`**（SKSE 插件，缺 tint 時強制 runtime 重生）。**結論：本機用途 Phase 2 幾乎不需要**；只在「發佈給沒裝 FDF 的人」或「要完全自足產物」時才要烘（或照 NPC mod 圈慣例把 FDF 列 requirements）。

---

## 開放問題（2026-07-11 使用者拍板：全照建議 → 可動工）

- **Q1（分段接受度）**：✅ **接受**——Phase 1 只寫配方，「身份對、身形/髮色對、臉待烘焙（可能灰/暗）」的中間態 OK；Phase 2 烘焙另排。
- **Q2（unique 處理）**：✅ **只 MINT 分身**——不做 override-本尊路線。
- **Q3（faceMorph 驗證素材）**：~~需要一次實機鎖定 index↔名 map~~ **已解決（2026-07-11 離線結構比對）**——CommonLibSSE `FaceData::Morphs` 枚舉與 Mutagen `NpcFaceMorph` 具名欄逐一同序（見地形節映射表），並由 T5 的 18-相異值讀回測試鎖死。實機吸 vanilla NPC 對照本尊＝可選抽查，併入 Phase 1 驗收順手做。
- **Q4（activeEffects/dead）**：✅ **先不消費**（advisory 帶著）。

---

## 相依 / 前情
- 姊妹 `capturedItems[]` 已落地（`Generator.CapturedItems.cs`、`Spec.CapturedItems.cs`、`CapturedItemsTests.cs`，commit `1bed5dd`）——**這是最佳抄襲模板**。
- DLL 端 capturedNpcs 匯出已成形（`../scene-capture-bridge/src/SceneExporter.cpp:269-316`、`Captures.cpp ReadNpc`）；faceMorphs 尾槽 FLT_MAX 已修（commit `82b368f`，18 值）。
- OPEN-B（PROTEUS 是否寫 TESNPC）未驗——若 PROTEUS 走 live NiNode override 不寫 TESNPC，capturedNpcs 對 PROTEUS clone 會吸到 base 臉（本計畫無影響，但影響 PROTEUS 用例的價值）。
