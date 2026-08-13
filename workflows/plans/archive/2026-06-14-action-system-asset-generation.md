# 動作系統 asset/config 生成 — 實作計畫（OAR / BDI / PIE）

> **For agentic workers:** REQUIRED SUB-SKILL: 用 superpowers:subagent-driven-development（建議）或 superpowers:executing-plans 逐 task 實作。步驟用 `- [ ]` 追蹤。

**Goal:** 讓 ModForge 從 spec 確定性生成**非-esp 的動畫 asset/config**：① OAR replacer/moveset（資料夾樹 + 兩層 config.json + 擺放使用者提供的 `.hkx`）為主交付；② BDI config（graph variable/event 注入 JSON）；③ PIE `.ini` 巨集表（最低優先）。**全程不碰 Havok bytes、不碰 behavior graph、不寫 esp record**。

**Architecture:**
- 新增頂層 spec 區塊（`ModSpec.AnimationReplacers` / `.BehaviorData` / `.PayloadMacros`），**與 record 無關**——這些是 loose-file 產物。
- **OAR 條件用專屬輕量模型 `OarConditionSpec`**（**不**重用 `ConditionSpec`：OAR 條件名 `IsActorBase`/`IsEquippedType`/`IsFemale`/`IsRace`/`Random`/`CompareValues` 與 Skyrim CTDA function 名不一致，硬映射會 lossy）。重用 `ConditionSpec` 的只有 comparison-operator 解析慣例與 form-ref 字串格式（`Plugin.esp|0xFormID`）。
- `npcMoveset` 語法糖在 build 期展開成 `OarConditionSpec[]`（`rightWeapon`/`leftWeapon`→兩條 `IsEquippedType`、`playerOnly:false`→`IsActorBase ¬player`、`race`→`IsRace`、`randomPick`→`Random<x`）。
- 產物為**純函式 Core 模組**（`OarGen`/`BdiGen`/`PieGen`：spec → 檔案內容字串/JSON），與 Mutagen 無關 → **離線全可測**（`Category!=RequiresSkyrim`）。
- `package` 在 loose-file 階段把產物寫進 `outModDir`（鏡像既有 `WriteSeq` 與 `Assets.Bundle`）；`.hkx` 由使用者經 `spec.Assets` 或 per-submod 路徑提供，ModForge 只複製擺位。

**Tech Stack:** C# net10.0、Mutagen.Bethesda.Skyrim 0.53.1（本功能幾乎不碰）、`System.Text.Json`、xUnit。**無** Wine/CK/Skyrim 依賴 → 離線機完整可開發測試。

**設計來源:** `workflows/specs/action-system-asset-generation-design.md`
**調查依據:** `../../analysis/mod-survey/action-system/`（五層堆疊 + 實檔驗證 schema；moveset 結構見 `findings/movesets-examples.md`、BDI/PIE 格式見對應 finding）。
**idea 脈絡:** `workflows/idea/asset-pipelines/animation/`（§8 `animations[]`/`importanim` 構想、§9 MVP；本計畫 MVP 對齊「OAR-set 生成器」，**`importanim` shell-out 與 Blender→hkx 牆出範圍**）。

**前置閱讀（CODE_MAP）:**
- `workflows/common/code-map/CODE_MAP.infra.md`：`Spec.cs`（ModSpec 頂層）、`Program.Build.cs`/`Package.cs`（package 流程、loose-file 階段、`WriteSeq`）、`Assets.cs`（`Bundle`/`BundledFolders`/`CopyTree`）、`Generator.Validate.cs`。
- `src/ModForge.Core/Spec/Spec.Dialogue.cs:205`（`ConditionSpec` 形狀參考）、`src/ModForge.Core/Build/Generator.Build.Conditions.cs`（CTDA function 詞彙 + comparison-operator 解析，供借鏡）。

**測試指令（全程）:** `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`
已知環境性失敗（非 regression）：缺本機 Skyrim.esm 的 `RequiresSkyrim` 案例自跳過 / 個別 word-wall 測試。

**鐵律:** 行為不變式重構守則照舊；本功能**新增**能力，每 task 完成跑離線測試綠燈才進下一個；commit 到 master，push 先確認。

---

## Task 0: spec 自審收斂（doc-only，釘死三個設計決定）

**目的:** spec 仍是「草案待自審」。動工前把三個 spec「待解」收斂，寫回 design 文件，後續 task 以此為準。

- [ ] **Step 1: 框架二選一** — idea §8 用 clip-centric `animations[]`（每筆一 clip + `ship:replacer|oar`），spec 用 OAR-mod-centric `AnimationReplacers[].submods[]`。**決定採 OAR-mod-centric**（一個 replacer-mod 多 submod 共享動畫，貼合實檔 Holmgang/NAMC，且 moveset 天生多 submod）。`ship:"replacer"`（vanilla-path 直擺）降為 submod 的一個 flag（`replaceVanillaPath` 無 conditions）。把此決定寫進 design 的「待解」段。
- [ ] **Step 2: OAR 條件模型決定** — 確認**專屬 `OarConditionSpec`**（不重用 ConditionSpec），列 MVP 覆蓋的條件：`IsActorBase`/`IsEquippedType`/`IsFemale`/`IsRace`/`Random`/`CompareValues`(graphVariable) + 巢狀 `AND`/`OR` 容器 + `negated`。寫進 design。
- [ ] **Step 3: `.hkx` 介面決定** — 確認 MVP **只擺使用者預先匯出的 `.hkx`**（路徑來自 submod 的 `hkx`/`variants` 欄位，相對 `spec.Assets` 或 spec 目錄）；Blender→hkx 與 `importanim` shell-out **明列為「之後」**。寫進 design。
- [ ] **Step 4:** design 文件狀態由「草案」改「已自審，待 plan 落地」；specs/README index 同步。

**驗證:** design 文件三決定明確、無自相矛盾；無 code 改動。

---

## Task 1: Spec DTO（`Spec.AnimationReplacer.cs` 新檔）

**Files:** 新增 `src/ModForge.Core/Spec/Spec.AnimationReplacer.cs`；改 `src/ModForge.Core/Spec/Spec.cs`（ModSpec 加三個 list 欄位）。

- [ ] **Step 1:** ModSpec 加 `List<AnimationReplacerSpec> AnimationReplacers`、`List<BehaviorDataSpec> BehaviorData`、`List<PayloadMacroSpec> PayloadMacros`（皆 `= new()`）。
- [ ] **Step 2:** 定義 DTO：
  - `AnimationReplacerSpec { string Mod; string Author; string Description; List<OarSubmodSpec> Submods; }`（→ replacer-mod 層 config）。
  - `OarSubmodSpec { string Name; string Description; int Priority; string Replaces; List<string> Hkx; List<string> Variants; List<OarConditionSpec> Conditions; NpcMovesetSpec? NpcMoveset; bool ReplaceVanillaPath; }`。
  - `OarConditionSpec { string Condition; bool Negated; string Form; int Type; bool LeftHand; string GraphVariable; string GraphVariableType; string Comparison; float Value; float RandomMin; float RandomMax; List<OarConditionSpec> Conditions; }`（容器型用 `Condition="AND"|"OR"` + 子 `Conditions`）。
  - `NpcMovesetSpec { string RightWeapon; string LeftWeapon; bool PlayerOnly; string Race; float? RandomPick; }`。
  - `BehaviorDataSpec { string File; List<BdiEntrySpec> Entries; }`、`BdiEntrySpec { string ProjectPath="Actors"; string Type; string Name; float Value; }`。
  - `PayloadMacroSpec { string File; string Section; List<PieMacroSpec> Macros; }`、`PieMacroSpec { string Name; string Command; }`。
- [ ] **Step 3:** 同步 `examples/spec.schema.json`（新三區塊；CODE_MAP.infra 規定欄位增減必須同步 schema）。

**驗證:** `dotnet build`；寫一個最小 spec JSON（一個 AnimationReplacer + 一個 submod）能反序列化（加進 Task 3 測試）。

---

## Task 2: OAR 條件序列化器 + npcMoveset 展開（`OarConditions.cs` 新檔，純函式核心）

**Files:** 新增 `src/ModForge.Core/Papyrus/OarConditions.cs`。

- [ ] **Step 1: 武器型 enum** — `static int WeaponType(string name)`：`fist`=0 `sword`=1 `dagger`=2 `waraxe`=3 `mace`=4 `greatsword`=5 `battleaxe`/`warhammer`=6 `bow`=7 `staff`=8 `crossbow`=9 `shield`=11 `torch`=12（來源 findings 的 enum 表）。未知名 → throw（validate 會先擋）。
- [ ] **Step 2: `OarConditionSpec` → `JsonNode`** — `static JsonObject Emit(OarConditionSpec c)`：
  - 通用欄位 `condition` + `requiredVersion:"1.0.0.0"` +（`negated` 為真才寫）。
  - `IsActorBase` → `"Actor base":{pluginName,formID}`（解析 `Form` 的 `Plugin.esp|0xID`）。
  - `IsEquippedType` → `"Type":{value:<float>}`,`"Left hand":<bool>`。
  - `IsRace` → `"Race":{pluginName,formID}`。
  - `IsFemale` → 無額外欄位。
  - `Random` → `"Random value":{min,max}`,`"Comparison":<op>`,`"Numeric value":{value}`。
  - `CompareValues` → `"Value A":{graphVariable,graphVariableType}`,`"Comparison"`,`"Value B":{value}`。
  - 容器 `AND`/`OR` → `"Conditions":[ Emit(子)… ]`。
- [ ] **Step 3: `NpcMovesetSpec` → `List<OarConditionSpec>`** — `static List<OarConditionSpec> Expand(NpcMovesetSpec m)`：右/左武器各一條 `IsEquippedType`；`PlayerOnly==false` → 一條 `IsActorBase negated Skyrim.esm|0x7`；`Race` 非空 → `IsRace`；`RandomPick` 有值 → `Random{0..1} < RandomPick`。全部包進一個 `AND` 容器（鏡像 Holmgang 實檔）。
- [ ] **Step 4: 單元測試** `tests/ModForge.Core.Tests/Papyrus/OarConditionsTests.cs`：每種條件 emit 出的 JSON 形狀逐欄比對（拿 findings 的真實片段當 golden，如 Holmgang「Sword & Shield」與 BFCO 變體 CompareValues）；`Expand` 對「sword+shield, NPC-only」產出三條的 AND 束。

**驗證:** 測試綠燈；golden JSON 與實檔片段逐欄相符。

---

## Task 3: OAR 資料夾/config 產生器（`OarGen.cs` 新檔，純函式核心）

**Files:** 新增 `src/ModForge.Core/Papyrus/OarGen.cs`。

- [ ] **Step 1: `record OarFile(string RelPath, string Content)`** + `static List<OarFile> Generate(AnimationReplacerSpec r)`：
  - root config：`Meshes/actors/character/animations/OpenAnimationReplacer/<Mod>/config.json` = `{name,author,description}`（**無 priority/conditions**）。
  - 每 submod：`…/<Mod>/<Submod sanitized>/config.json` = `{name,description,priority,conditions:[…]}`（conditions 經 Task 2；若 `NpcMoveset` 非空先 `Expand`）。空 conditions → `[]`。
- [ ] **Step 2: `.hkx` 擺放清單** — `static List<(string src,string destRel)> HkxPlacements(AnimationReplacerSpec r, …)`：每個 submod 的 `Hkx[]` → 落在 submod 資料夾下「重建的 vanilla 相對路徑」（MVP：直接落 submod 根，檔名同 `Replaces` 的 basename）；`Variants[]` → `_variants_<animName>/1.hkx,2.hkx…`。`ReplaceVanillaPath` 的 submod → 落 vanilla path、不產 config。
- [ ] **Step 3:** 資料夾/檔名 sanitize（避免非 ASCII；OAR 用 config 內 name，資料夾名隨意但需合法 + Windows 260 路徑警告）。
- [ ] **Step 4: 測試** `OarGenTests.cs`：給一個雙 submod 的 replacer，斷言產出的 RelPath 集合、root config 無 priority、submod config 有 conditions、空 conditions 為 `[]`、variants 路徑規則。

**驗證:** 測試綠燈；產物路徑/內容符合 §B 實檔結構。

---

## Task 4: BDI config 產生器（`BdiGen.cs` 新檔）

**Files:** 新增 `src/ModForge.Core/Papyrus/BdiGen.cs`。

- [ ] **Step 1: `static OarFile Generate(BehaviorDataSpec b)`** → `SKSE/Plugins/BehaviorDataInjector/<File>.json`，內容為 flat JSON array：每 entry `{projectPath,type,name,value}`，`type∈kInt|kBool|kFloat|kEvent`，**`kEvent` 省 `value`**。
- [ ] **Step 2: 測試** `BdiGenTests.cs`：拿 findings 驗證過的 DMK `DirecionalMovement_BDI.json` / BFCO `BFCO_BDI.json` 當 golden，逐欄比對（含 kEvent 無 value）。

**驗證:** 測試綠燈；輸出與實檔 byte-語意相符。

---

## Task 5: 驗證（`Generator.Validate.AnimationReplacer.cs` 新檔）

**Files:** 新增 `src/ModForge.Core/Validate/Generator.Validate.AnimationReplacer.cs`；在 `Generator.Validate.cs` 的 `RegisterAll` 掛入。

- [ ] **Step 1:** 校驗：submod `Priority` 必填且 >0；`Condition` 名在 MVP 白名單內；`IsEquippedType`/`npcMoveset` 的武器名是 `WeaponType` 認得的；`Form`/`Race` 是合法 `Plugin.esp|0xID`；BDI `Type` 在四值內、event 不帶 value。
- [ ] **Step 2:** `.hkx`/variants 檔案存在性檢查（相對 `spec.Assets`/spec dir）；缺檔 → warning（不擋 build，因離線機可能無資產，鏡像既有 asset 寬鬆度）。
- [ ] **Step 3:** 測試 `ValidateAnimationReplacerTests.cs`：壞武器名、壞 form、event 帶 value、priority=0 各報對應錯。

**驗證:** 測試綠燈。

---

## Task 6: 接進 package + CLI（loose-file 階段）

**Files:** 改 `src/ModForge.Cli/Commands/Package.cs`；可能 `src/ModForge.Cli/Commands/Program.Build.cs`。

- [ ] **Step 1:** 在 Package.cs 的 asset/loose-file 階段（§6 `Assets.Bundle` 之後）呼叫 `OarGen.Generate`/`BdiGen.Generate`，把 `OarFile.Content` 寫進 `outModDir/<RelPath>`，並把 `.hkx` 依 `HkxPlacements` 從來源複製到 `outModDir`。鏡像 `WriteSeq` 的寫法。
- [ ] **Step 2:** package summary 末尾報告：「N OAR submod(s) / M BDI config(s) / K hkx placed」。
- [ ] **Step 3:** 若有 AnimationReplacers 但對應 `.hkx` 缺檔 → summary 明列（**no silent caps** 原則）。
- [ ] **Step 4:** 測試（若 package 路徑可離線跑）：給 temp spec + 假 hkx，跑 package 等價函式，斷言 outModDir 下檔案就位。否則退化為對 OarGen/BdiGen 純函式的測試（已在 Task 3/4）。

**驗證:** `dotnet test` 綠燈；手動跑一個含 AnimationReplacer 的 sample spec `package`，檢查 outModDir 樹（不需 Skyrim）。

---

## Task 7: 文件 + 範例 + CODE_MAP 維護鏈

**Files:** `docs/spec/SPEC-*.md`（新區塊說明，可能新 `SPEC-animation.md`）、`docs/for_agent_cli.md`、`examples/sample_spec.json` 或新 `examples/oar_moveset_spec.json`、`workflows/common/code-map/CODE_MAP.infra.md`（登記新檔）、`docs/zh-TW/` 鏡像。

- [ ] **Step 1:** 寫 SPEC 區塊：`animationReplacers`/`behaviorData`/`payloadMacros` 欄位表 + 一個 `npcMoveset` 例 + 武器型 enum 表 + 「.hkx 須自備、Blender 牆出範圍」聲明。
- [ ] **Step 2:** 新增 `examples/oar_moveset_spec.json`（一個 NPC 單手劍 moveset showcase，用 `npcMoveset` 糖 + 一個 BDI 變數）。
- [ ] **Step 3:** CODE_MAP.infra 登記 `Spec.AnimationReplacer.cs`/`OarConditions.cs`/`OarGen.cs`/`BdiGen.cs`/`Generator.Validate.AnimationReplacer.cs`（依 conventions 的 CODE_MAP 維護鏈）。
- [ ] **Step 4:** `docs/zh-TW/` 同步鏡像（依 [[zh-tw-translation-mirror]] 規則）。
- [ ] **Step 5:** roadmap 把 OAR/BDI 項由「spec 草案」推進到「plan 落地中/已落地」。

**驗證:** 連結無斷鏈；sample spec 能 `validate` 通過；`generate.py` 若涉 html 重生。

---

## MVP 邊界（本計畫**不**含，列「之後」）
- PIE `.ini` 巨集表產生器（`PieGen.cs`，Task 結構同 BdiGen，招式效果線開始才需要）。
- OAR `variants` weight / `presets` / `functions` / 進階 submod flag（interruptible 等）。
- DAR `_conditions.txt` DSL 後路輸出。
- `importanim` shell-out（Blender retarget + serde-hkx）與 Blender→hkx 匯出牆。
- Pandora shell-out（behavior 基底生成）—— 另一 spike，見 `pandora.md`。
- OAR condition 的 static/global/AV 值型別完整覆蓋（MVP 只做 moveset 實際用到的子集）。
