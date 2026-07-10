# 遊戲內場景匯出 — ModForge 側 M0–M2 Implementation Plan

> **For agentic workers:** 逐 task 實作，每 task 附驗證步驟。Steps 用 checkbox（`- [ ]`）追蹤。碰原始碼遵守 [common/conventions](../common/conventions.md)（含 CODE_MAP 維護鏈）。

**Goal:** 讓 ModForge 能吃一份 **scene.json**（遊戲內採集橋未來會吐的格式）→ `build` 出 patch esp：既有 record（placements/mapMarkers/hazards/tags/cell override）直接生，外加一個 **`npcRoles[]` 角色 macro**（切片只做 blacksmith：conditioned-Hello 對話 + sandbox package + vendor）掛在**外部 captured ActorRef** 上。**不帶 scene.json 的既有 spec 生成位元不變。**

**範圍**：只做設計 spec 標的 **M0–M2**（純 ModForge、離線可測、不依賴 runtime 元件）。採集橋 DLL（M4）、placement-controller（M3，合流 settlements P2）、PROTEUS 拓印（M5）不在本 plan。

**Architecture:** 三塊——① `SceneImport`（讀 scene.json → `AddRange` 進既有 `ModSpec` 的 list，**推廣既有 `GodotPlacements.Load`**）；② `SceneNpcRoleSpec` + role→零件對照表（新小型別，**重用** vendor/package/conditioned-Hello 生成）；③ 接進 build 流程（scene 分支，既有路徑原封不動）。

**Tech Stack:** C# net10.0、Mutagen.Bethesda.Skyrim 0.53.1、System.Text.Json、xUnit。

設計依據：[workflows/specs/ingame-scene-export-design.md](../specs/ingame-scene-export-design.md)。既有先例：`GodotPlacements.cs`（外部 JSON → `spec.Placements.AddRange`，`Generator.Build.Worldspace.cs:255`）；`SettlementVendorSpec`/Build.Vendor（vendor 零件）；conditioned-Hello（memory [[conditioned-hello-one-topic-many-infos]]）。

---

## 實作進度（2026-07-08，M0–M2 落地 · IN-GAME 確認）

**✅ M2 實機確認（2026-07-08）**：白漫 Carlotta（當 clone 替身）對話講出鐵匠問候「Need something forged?」——§D 核心（貼 role → ModForge 生該 NPC 專屬 conditioned 問候）實機成立。落地記錄 [landed/dialogue-quests](../feature-dev/landed/dialogue-quests.md)。

**已完成、離線全綠（856 tests）、端到端 build 已驗**（commit `feat(scene-export)`）：
- `SceneNpcRoleSpec` + `ModSpec.NpcRoles` + `ExpandNpcRoles`（blacksmith → host quest + Hello + sandbox package + NpcPatch）+ `ValidateNpcRoles`。
- **core 前置修（Task 0 Step 4 預判的最小點）**：`Generator.Build.Dialogue.cs` 讓 **外部 `<plugin>:0xID` speaker** 也能生 conditioned Hello（兩處 speaker-gate fallback `TryResolveRef` + `MakeHello` 改吃 `FormKey` 並在材質化迴圈解析外部 speaker，否則外部 NPC 的 Hello topic 根本不生成）。
- 範例 `examples/scene-export-blacksmith.scene.json`（M0 契約）+ `SceneNpcRolesTests.cs`（8 測）。

**對原 plan 的偏離（實作時的更優解）**：
- **不需獨立 `SceneImport --scene` 合併**：scene.json 就是一份合法 `ModSpec`，`build scene.json out.esp` 直接可跑（`placements`/`mapMarkers`/`hazards`/`npcRoles` 同名欄位）。原 Task 2 的 `--scene` 合併＝未來「骨架 spec + 採集場景」才需要，非本切片必需，延後。
- **`SceneNpcRoleSpec.ActorRef` 改名 `Npc`**：keyed on **base NPC ref**（GetIsID/NpcPatch/placement base 三者都吃 base ref），與全 repo 一致。
- **vendor 段（後補完成 2026-07-08）**：原 `NpcPatch` 只換 package、不能加 faction；已擴 `NpcPatchSpec.Factions`（`WireNpcPatchFactions`）→ blacksmith 有 companion placement 時自動生 Vendor FACT + merchant chest + 加 vendor/JobMerchant faction，vanilla 交易對話浮現。IN-GAME 待驗。

**下一步**：M2 實機（含 vendor 交易，[wait_todo/ingame-tests](../../wait_todo/ingame-tests.md)）；之後 `removals[]`（橡皮擦）、M3–M5 runtime 元件。下面原始 task 清單保留作歷史脈絡。

---

### Task 0: 前置確認（只讀，不改碼）

**Files:** 無（只讀）

- [ ] **Step 1: 確認 build 入口與 `ModSpec` 併入點**

Run: `grep -n "Deserialize<ModSpec>\|specDir\|Generate\|Build(" src/ModForge.Cli/Program.Build.cs | head`
Expected: 確認 `ModSpec` 在 `Program.Build.cs:65` 反序列化、拿得到 `specDir`（scene.json 相對路徑基準）、以及生成呼叫入口（`SceneImport.Merge(spec, specDir)` 要插在 deserialize 之後、generate 之前）。對照 `GodotPlacements` 是在 `Generator.Build.Worldspace.cs:255` 於 build 內展開——決定 `SceneImport` 走「CLI 併入 spec」或「Generator 內展開」；**選 CLI/spec 併入**（scene.json 是整份 spec 的替代來源，非某 worldspace 的子項）。

- [ ] **Step 2: 確認 conditioned-Hello 對話的 spec 形狀**

Run: `grep -nE "class DialogueSpec|Hello|GetIsID|Category|Topic|Info|Response" src/ModForge.Core/Spec.Dialogue.cs | head -20`
Expected: 找到一個「Hello topic + 多 INFO + 每 INFO 帶 condition（GetIsID <npc>）」的可表達路徑（memory 已確認此形狀在遊戲內可行）。記下要填的欄位名，供 Task 3 的 blacksmith 對話模板用。

- [ ] **Step 3: 確認 vendor 與 sandbox package 的既有生成入口**

Run: `grep -nE "class SettlementVendorSpec|class VendorSpec|class PackageSpec" src/ModForge.Core/Spec*.cs`
Expected: 確認 (a) vendor 可用 `SettlementVendorSpec`（vendor FACT + merchant container + gold）或頂層 `VendorSpec`；(b) sandbox package 的 `PackageSpec` 欄位（template=Sandbox + 綁 furniture/marker）。Task 3 的 role→零件對照表據此填。

- [ ] **Step 4: 確認外部 ActorRef 能當 dialogue condition / vendor faction 對象**

Run: `grep -rn "GetIsID\|<master>:0x\|external ref\|0xFORMID" src/ModForge.Core/Generator.Build.Dialogue*.cs src/ModForge.Core/Generator.Build.Vendor.cs 2>/dev/null | head`
Expected: 確認生成器接受 `<plugin>.esp:0xFORMID` 形式的外部 ActorRef 當 GetIsID 對象 + 加入 vendor faction（跨 master 引用是熟路；memory `esm-formid-access`）。若某處只吃 in-spec editorId → 記為 Task 3 要補的最小點。

---

### Task 1: `SceneNpcRoleSpec` schema + 頂層 list

**Files:**
- Create: `src/ModForge.Core/Spec.SceneExport.cs`
- Modify: `src/ModForge.Core/Spec.cs`（加一個頂層 list）

- [ ] **Step 1: 新增 `SceneNpcRoleSpec`**

在 `Spec.SceneExport.cs`：

```csharp
namespace ModForge;

// 給一個「已存在的外部 NPC（PROTEUS clone / standalone follower）ActorRef」貼一個職業 role，
// build 時 macro-expand 成該 NPC 的 conditioned-Hello 對話 + sandbox package + vendor 服務。
// 與玩家向的 IdentitySpec 無關（那是玩家加入 FACT 的 gate）；與 ResidentSpec 的差別 = keyed on
// 外部 ActorRef 且自帶對話。切片只支援 role="blacksmith"。
public sealed class SceneNpcRoleSpec
{
    public string ActorRef { get; set; } = "";   // 外部 clone/follower：<plugin>.esp:0xFORMID
    public string Role { get; set; } = "";        // 對照表 key（切片：blacksmith）
    public string Backstory { get; set; } = "";   // 幾行背景，驅動對話文本（切片手填）
}
```

- [ ] **Step 2: 頂層 `ModSpec` 加 list**

在 `Spec.cs` 加：`public List<SceneNpcRoleSpec> NpcRoles { get; set; } = new();`（放在 Identities 附近，加註解區隔「NPC 職業 role macro，非玩家 identity」）。

- [ ] **Step 3: build 確認**

Run: `dotnet build src/ModForge.Core/ModForge.Core.csproj`
Expected: build succeeded。

---

### Task 2: `SceneImport`（scene.json → 併入 ModSpec）

**Files:**
- Create: `src/ModForge.Core/SceneImport.cs`
- Modify: `src/ModForge.Cli/Program.Build.cs`（deserialize 後、generate 前呼叫）

- [ ] **Step 1: 寫 `SceneImport.Merge`（推廣 `GodotPlacements.Load`）**

scene.json 直接反序列化成一個 `ModSpec`-shaped 或子集 DTO，把各 list `AddRange` 進主 `spec`。**設計選擇（M0 契約）：scene.json 就是一份合法 `ModSpec` 片段**（欄位名 = `placements`/`mapMarkers`/`hazards`/`npcRoles`/`cells`），所以 `SceneImport` 可薄到「若 CLI 收到的是 scene.json 就直接 `Deserialize<ModSpec>` 併入」。保留獨立 `SceneImport` 類別是為了未來座標/欄位正規化（如採集橋吐弧度→度數）有掛點。

```csharp
namespace ModForge;
public static class SceneImport
{
    // 讀一份 scene.json（ModSpec 片段）併入既有 spec。切片：欄位同名直接 AddRange。
    public static void Merge(ModSpec spec, string sceneJsonPath) { /* Deserialize + AddRange 各 list */ }
}
```

- [ ] **Step 2: CLI 掛接（可選旗標，不改既有預設路徑）**

在 `Program.Build.cs` 加一個 `--scene <path>` 選項：帶了才呼叫 `SceneImport.Merge(spec, path)`；不帶則**完全走原路徑**（行為不變的關鍵）。

- [ ] **Step 3: build + 冒煙**

Run: `dotnet build && dotnet run --project src/ModForge.Cli -- build <既有 example.json>`（不帶 `--scene`）
Expected: 與改動前輸出一致（下 Task 5 有位元不變測）。

---

### Task 3: blacksmith role macro（role → 既有零件）

**Files:**
- Create: `src/ModForge.Core/Generator.Build.SceneNpcRoles.cs`
- Modify: build 流程串接處（依 Task 0 Step 1 結論）

- [ ] **Step 1: role→零件對照表 + 展開**

對每個 `NpcRoles` entry，依 `Role` 查對照表，把該 NPC 的零件**加進既有 spec list**（讓既有生成器處理），而非另寫生成碼：
- **對話**：往 `spec.Dialogue` 加一個 GetIsID(`ActorRef`) 的 Hello topic + 1–2 條問候 INFO（文本用 `Backstory` 拼；切片可用固定鐵匠問候模板）。
- **package**：往 `spec.Packages` 加一個 blacksmith sandbox（綁 anvil/forge furniture marker），並確保掛到該 ActorRef（npcPatch 路徑，memory [[radiant-alias-package-byte-truths]]）。
- **vendor**：重用 `SettlementVendorSpec`/`Build.Vendor` 把 `ActorRef` 加進 vendor FACT + 生 merchant container（sellBuyList = VendorItemsMisc/BlacksmithSmithing 之類 vanilla FormList）。

- [ ] **Step 2: 只做 blacksmith，其他 role 報 warn**

未知 `Role` → `log warn`「role 'X' 尚未支援（切片只做 blacksmith）」且跳過（照 CLAUDE.md no-silent-caps）。

- [ ] **Step 3: build 確認**

Run: `dotnet build`
Expected: succeeded。

---

### Task 4: 串進 build + 行為不變

**Files:**
- Modify: build 入口（`Program.Build.cs` 或 Generator，依 Task 0）

- [ ] **Step 1: 順序**：`Deserialize<ModSpec>` → （若 `--scene`）`SceneImport.Merge` → `ExpandNpcRoles`（Task 3，把 role 展開成 dialogue/package/vendor list）→ 既有 `Generate`。role 展開**必須在既有生成之前**（它只是往 list 塞料）。

- [ ] **Step 2: 行為不變自檢**：不帶 `--scene` 且 `NpcRoles` 為空 → 三個新步驟全 no-op（`Merge` 沒呼叫、`ExpandNpcRoles` 迴圈空）。確認既有生成路徑一個分支都沒改。

---

### Task 5: M0 範例 scene.json + 離線測試

**Files:**
- Create: `examples/scene-export-blacksmith.scene.json`
- Create: `tests/SceneExportTests.cs`

- [ ] **Step 1: M0 範例（契約凍結）**

寫一份 scene.json：1 house `placement`（vanilla farmhouse base）+ 1 `mapMarker`（Town, Visible|CanTravelTo）+ 1 `hazard`（篝火 VFX）+ 1 `npcRole`（actorRef 指向任一既有 standalone follower 當 clone 替身，role=blacksmith）+ 目標 `cell`/`worldspace` override。

- [ ] **Step 2: 離線單元（`Category!=RequiresSkyrim`）**

- `SceneImport` round-trip：Merge M0 範例 → 斷言 `spec.Placements/MapMarkers/Hazards/NpcRoles` count 與內容正確。
- role macro：blacksmith 展開 → 斷言 `spec.Dialogue` 出現 GetIsID(actorRef) 的 Hello INFO、`spec.Packages` 出現 sandbox、vendor FACT + container 生成。
- **行為不變**：跑既有某 example（不帶 scene）→ 生成 esp 與 baseline **位元相同**（或既有測試全綠不回歸）。
- 座標映射：interior local vs exterior world 各一 placement → 斷言落在對的 cell。
- 未知 role → 有 warn 且不崩。

Run: `dotnet test --filter "Category!=RequiresSkyrim"`
Expected: 全綠（含新測試）。

- [ ] **Step 3: 端到端 build**

Run: `dotnet run --project src/ModForge.Cli -- build examples/scene-export-blacksmith.scene.json --scene examples/scene-export-blacksmith.scene.json`（或依 Task 2 的旗標形狀）
Expected: 生出 esp，`dump` 見房子 REFR + NPC-role 的 dialogue/package/vendor + XMRK + HAZD。

---

### Task 6: M2 實機（主力機，→ WAIT_USER）

**Files:** 無（打包 + 實機）

- [ ] **Step 1: package + 交付**：照 dev-env ship 流程打包 M0 patch 到 `~/skyrim_mods/mine/`。
- [ ] **Step 2: 實機驗（使用者）**：載入 → 房子在該 cell、鐵匠站著且有問候/服務對話、marker 地圖可快旅、篝火特效可見。→ 記入 [WAIT_USER.md](../../WAIT_USER.md)。

> M2 通過 = ModForge 側管線（scene.json → 可造訪城鎮）閉環證成。之後才接 runtime 元件（M3 controller / M4 採集橋 / M5 PROTEUS 拓印）——各自另立 plan。

---

## 依賴 / 後續（非本 plan）

- **M3 placement-controller**：合流 [settlements P2](../roadmap/mod-survey-gaps/settlements-phase2.md)。
- **M4 採集橋 SKSE DLL**：獨立子專案 [scene-capture-bridge](../../sub_projs/scene-capture-bridge/README.md)（本 plan 的 scene.json 契約 = 它的 output 目標）。**M4 spike 已 IN-GAME 2026-07-10**。後續的 §B/§D/§E 編輯器工具（橡皮擦／滴管／範圍吸取／語意標記／role tag）另立 plan：[scene-capture-bridge.md](scene-capture-bridge.md)。純 runtime，對本 plan 的 ModForge 側零衝擊（吸來的 base 一樣進 `placements[].base`，ModForge 自動加 master）。
  - 滴管的插槽**不必走 StorageUtil**（原 idea 的假設）——採集橋現在有 C++ 面板，直接 DLL 記憶體 + sidecar json，省掉 PapyrusUtil 相依。
- **M5 PROTEUS 拓印**：需先驗 clone ActorRef 取得方式（idea #24 §A 已拍板穩定可引用）。
- **role 全集 + AI 對話文本**：blacksmith 之外接 #23 archetype 框架 / #17 生成管線。
</content>
