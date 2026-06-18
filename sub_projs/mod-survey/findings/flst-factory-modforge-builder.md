# ModForge FLST builder 現有能力 + 未覆蓋模式

← [flst-factory](flst-factory.md)

## 五、ModForge FLST builder 現有能力 + 未覆蓋模式

### 現有能力（有 code 為據）

**`BuildFormLists()`**（`Generator.Build.Lists.cs`）：

```csharp
public void BuildFormLists()
{
    foreach (var fl in spec.FormLists)
    {
        var r = mod.FormLists.AddNew();
        r.EditorID = fl.EditorId;
        // 只設 EditorID，items 留空給 pass 2 填
    }
}
```

**`WireFormLists()`**（`Generator.Build.Lists.Wire.cs`）：

```csharp
public void WireFormLists()
{
    foreach (var fl in spec.FormLists)
    {
        if (!recordsByEd.TryGetValue(fl.EditorId, out var rec) || rec is not IFormList list) continue;
        foreach (var item in fl.Items)
            Resolve($"formList '{fl.EditorId}' item", item, fk =>
                list.Items.Add(new FormLink<ISkyrimMajorRecordGetter>(fk)));
    }
}
```

**`FormListSpec`**（`Spec.Items.cs`）：

```csharp
// FormList (FLST): an ordered list of FormIDs of ANY type. `items` are refs (in-spec editorId or
// vanilla "<master>:0xFORMID"). Use it as the param of a list-taking condition (GetItemCount /
// GetEquipped / GetIsVoiceType / GetInWorldspace all accept a FormList), as a keyword/clothing set
// for dialogue/quest gating, or anywhere the game wants a grouped set of forms.
public sealed class FormListSpec
{
    public string EditorId { get; set; } = "";
    public List<string> Items { get; set; } = new();
}
```

**ModForge 今天可以做的**：
- 建立新 FLST record（`BuildFormLists()`）
- 填入任意 form（in-spec editorId 或 vanilla ref）作為成員（`WireFormLists()`）
- 同一 FLST 可混合 in-spec 與 vanilla form
- 建立多個互相平行的 FLST（索引對齊模式的基礎）
- 在 condition 中引用 FLST（`ConditionSpec` 的 `HasForm` / `GetItemCount` 等）

### 未覆蓋模式（推斷，⚠️）

| 缺口 | 說明 | 影響模式 |
|---|---|---|
| ⚠️ **平行 FLST 自動展開**（高階） | 要從一批 spec-defined SPEL 自動沿分類軸（school/level/delivery）建出多條對齊清單，需要 spec 層語法支援（如 `indexedLists[]`）；目前只能手動逐一列出每條 FLST 的成員 | 索引對齊池（大規模法術族生成） |
| ⚠️ **runtime AddForm（Papyrus 側）** | ModForge 生成的是靜態 ESP，不生成 `AddForm()` 呼叫的 script 邏輯；跨 plugin 動態追加需靠 FLM 或手寫 Papyrus | FLM runtime 追加 |
| ⚠️ **生成 FLM ini 輸出** | ModForge 的 `build` 輸出是 `.esp`，不輸出 `_FLM.ini`；若要利用 FLM 無衝突追加外部 FLST，ini 需手寫 | FLST as SPID target（外部追加） |

**結論**：FLST 的**靜態建立 + 填入**（分類容器模式、手工索引對齊池、SPID target）ModForge 今天就能完整生成；**跨 plugin 動態追加**和**自動化索引對齊展開**是兩個合理的未來擴充點，不是緊急缺口（Missives/Spellforge 的核心功能不需要它們）。
