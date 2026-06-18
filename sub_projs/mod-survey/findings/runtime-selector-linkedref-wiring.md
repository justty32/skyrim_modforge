# linkedRef 節點鏈：WireLinkedRefs + 設計模式

← [runtime-selector-patterns](runtime-selector-patterns.md)

### 七、ModForge WireLinkedRefs() 現有能力

`Generator.Build.PlacementRefs.cs:WireLinkedRefs()` 在 pass 2 執行（在所有 placement 建好後），逐一讀取 `PlacementSpec.LinkedRefs`（型別 `List<LinkedRefSpec>`）：

```csharp
// 對每個有 LinkedRefs 的 placement：
foreach (var lr in pl.LinkedRefs)
{
    // 1. 解析 target：in-spec editorId 或 vanilla "Plugin.esm:0xID" 外部 ref
    if (!TryResolveRef(lr.Target, formKeyByEd, out var tgtFk)) { Warn(...); continue; }

    var link = new LinkedReferences();
    link.Reference.SetTo(new FormLink<IPlacedGetter>(tgtFk));

    // 2. keyword 可選：有就解析，空字串 = null keyword（= default Patrol link）
    if (!string.IsNullOrWhiteSpace(lr.Keyword) && TryResolveRef(lr.Keyword, formKeyByEd, out var kwFk))
        link.KeywordOrReference.SetTo(new FormLink<IKeywordLinkedReferenceGetter>(kwFk));

    list.Add(link);
}
```

**已支援**：
- null keyword link（省略 `keyword` 欄位）→ Patrol 預設路線
- 具名 keyword link（填 `keyword: "MyKeywordEditorId"` 或 `"Plugin.esm:0xID"`）→ 具名連結
- 跨 plugin 外部 ref target（`"Skyrim.esm:0x..."` 語法）
- target 可以是 in-spec placement（by editorId）或 vanilla ref
- 帶 `linkedRefs` 的 placement 自動設 persistent flag（`Generator.Build.Placements.cs` 行 124）

**spec 層欄位**（`Spec.World.cs:LinkedRefSpec`）：
```csharp
public sealed class LinkedRefSpec
{
    public string Target  { get; set; } = "";   // 必填：目標 placement editorId 或 "Plugin:0xID"
    public string Keyword { get; set; } = "";   // 可選：keyword editorId 或 "Plugin:0xID"；空 = default link
}
```

**驗證**（`Generator.Validate.World.cs`）：`Target` 不能為空、`Target`/`Keyword` 都經 `CheckRef` 解析驗證；帶 `linkedRefs` 的 placement 必須有 `editorId`（否則 validation error）。

**注意**：`WireLinkedRefs()` 同時支援 `IPlacedObject`（REFR）和 `IPlacedNpc`（ACHR），`src as IPlacedObject ?? src as IPlacedNpc` 選正確的 `LinkedReferences` 集合。

---

### 八、設計模式

#### 模式 A：Patrol 路線鏈

最直接的用法，即 Animated Carriage 的路徑節點鏈：

```json
// spec 片段（示意）
{
  "placements": [
    { "editorId": "MyRoute_Start",   "base": "MyCartMarker", "cell": "...", "position": {...},
      "linkedRefs": [{ "target": "MyRoute_Node1" }] },
    { "editorId": "MyRoute_Node1",   "base": "MyCartMarker", "cell": "...", "position": {...},
      "linkedRefs": [{ "target": "MyRoute_Node2" }] },
    { "editorId": "MyRoute_Node2",   "base": "MyCartMarker", "cell": "...", "position": {...},
      "linkedRefs": [{ "target": "MyRoute_End" }] },
    { "editorId": "MyRoute_End",     "base": "MyCartMarker", "cell": "...", "position": {...} }
  ],
  "packages": [
    { "editorId": "MyNpcPatrol", "type": "Patrol", "patrol": { "start": "MyRoute_Start" } }
  ]
}
```

Patrol package 的 `start` 指向鏈頭，NPC 跟著 null keyword XLKR 一格一格走。**閉環**（回到起點循環）：最後一個節點的 `linkedRefs` 指回 `MyRoute_Start`，`patrol.repeatable: true`（預設即 true）。

#### 模式 B：具名連結（分岔路線 / 多語義）

```json
{ "editorId": "BranchPoint",
  "linkedRefs": [
    { "target": "MainPath_Next" },
    { "target": "AltPath_Start", "keyword": "kwAlternativePath" }
  ]
}
```

腳本可以 `GetLinkedRef()` 取 null link（主路）或 `GetLinkedRef(kwAlternativePath)` 取分岔，再依隨機值決定走哪條。ModForge 今天完整支援此模式（`WireLinkedRefs()` 的 keyword 路徑）。

#### 模式 C：目標池（FLST 替代方案）

多個物件都帶 XLKR 指向同一個「列表頭 marker」，讓腳本遍歷：

```
Ref_A → [linkedRef null] → PoolHead
Ref_B → [linkedRef null] → PoolHead
Ref_C → [linkedRef null] → PoolHead
```

腳本不用 FormList，直接 scan 附近所有帶某 keyword link 的 ref；或反過來：`PoolHead` 的 XLKR 指向第一個目標，目標的 XLKR 再指向下一個，形成遍歷鏈。

#### 模式 D：與 alias 間接的關係（真缺口 #2）

mod-survey-gaps.md 的確認真缺 **#2**：package `target`/`location` 目前只解到 placed ref 或 NearSelf，無 `PackageTargetAlias`/alias-index location。

這個缺口與 linkedRef 鏈在 **radiant 演出** 場景下直接相關：

- **理想流程**：Quest alias 填入一個動態 actor（`findMatching` 或 `createObject`），package 的 target 指向那個 alias index → actor 沿 linkedRef 鏈巡邏
- **現況**：`WireDeferredTargets()` 只能輸出 `PackageTargetSpecificReference`（指向固定 placed ref）
- **可作的繞過**：將 linked-ref 鏈頭設為 persistent placed marker，package target 直接指向那個 marker 的 editorId → NPC 從鏈頭開始巡邏，但 NPC 本身仍需是 placed ref 而非動態填入的 alias。完全的 alias-driven radiant patrol 需等 #2 補上 `PackageTargetAlias` 支援。

---

