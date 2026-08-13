# Story Manager spec 管線（階段二）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓使用者在 spec.json 的 quest 上宣告 `storyEvent` + `aliases`，ModForge 自動產出 SMBN→SMQN 節點樹（additive 掛在原版 Kill Actor 事件根下）+ 帶條件式 alias 的 Quest，把階段一探針驗過的 SM 能力變成 spec→build 管線的一等公民。

**Architecture:** 意圖導向。新增一張事件表（事件名→原版根/事件碼/槽位 bytes），一個 build step（`ctx.BuildStoryManager()`，在 `BuildQuests()` 後）把 event/conditions/aliases 套到已建好的 Quest 並生節點，一個 validator。只做 KillActor + `fromEvent`/`forced` 填充。完成後刪除階段一的 throwaway probe builder。

**Tech Stack:** C# net10.0、Mutagen.Bethesda.Skyrim 0.53.1、xUnit。

**設計來源：** `workflows/specs/archive/2026-06-04-story-manager-spec-pipeline-design.md`
**硬知識：** 記憶 `story-manager-kill-recipe`（根 0x013010、Event "KILL"、victim=R1=`52 31 00 00`、killer=R2=`52 32 00 00`、SimpleActor critter 不發事件）。

---

## 既有 API（實作要用，已查證）

- `BuildContext`：`private sealed partial class BuildContext`（in `public static partial class Generator`，namespace `ModForge`）。可用成員：`mod`（SkyrimMod）、`spec`（ModSpec）、`Dictionary<string,Quest> questsByEd`、`Dictionary<string,FormKey> formKeyByEd`、`ConditionFloat? BuildCondition(ConditionSpec, string label)`、靜態 `TryResolveRef(string s, Dictionary<string,FormKey> formKeyByEd, out FormKey fk)`（in Generator.Helpers.cs）。
- Orchestrator：`Generator.Build.cs` 的 `Build()` 內依序呼叫 `ctx.BuildXxx()`；`ctx.BuildQuests()` 後 quests 進 `questsByEd`。
- Validate：`Generator.Validate.cs` 的 `Validate(ModSpec)` 建 `ValidateContext ctx` 後呼叫 `ctx.ValidateNpcs()` 等。`ValidateContext` 是 `private sealed partial class`，可用：`spec`、`List<string> Problems`（確切名以檔案為準，見下方 Task 4 Step 0）、`CheckRef(string r, string what)`、`CheckCondition(ConditionSpec, string what)`。
- Mutagen：`Quest.Event : RecordType?`、`Quest.EventConditions`（ExtendedList<Condition>；`ConditionFloat : Condition`）、`Quest.Flag.StartGameEnabled`、`QuestAlias{ uint ID; string Name; FindMatchingRefFromEvent; IFormLinkNullable ForcedReference; Flag Flags }`、`FindMatchingRefFromEvent{ RecordType? FromEvent; MemorySlice<byte>? EventData }`（byte[] 可隱式轉）、`mod.StoryManagerBranchNodes/StoryManagerQuestNodes.AddNew()`、`AStoryManagerNode.Parent`（IFormLinkNullable，`.SetTo(FormKey)`）、`StoryManagerQuest{ IFormLinkNullable Quest }`。

## File Structure

- `src/ModForge.Core/Spec/Spec.StoryManager.cs` — **新**。`QuestStoryEventSpec` / `QuestAliasSpec`。
- `src/ModForge.Core/Spec/Spec.Dialogue.cs` — **改**。`QuestSpec` 加 `StoryEvent` + `Aliases`。
- `src/ModForge.Core/StoryManagerEvents.cs` — **新**。事件表 + `TryGet` + `TryParseFill`。
- `src/ModForge.Core/Build/Generator.Build.StoryManager.cs` — **新**。`ctx.BuildStoryManager()`。
- `src/ModForge.Core/Build/Generator.Build.cs` — **改**。orchestrator 插一行。
- `src/ModForge.Core/Validate/Generator.Validate.StoryManager.cs` — **新**。`ctx.ValidateStoryManager()`。
- `src/ModForge.Core/Validate/Generator.Validate.cs` — **改**。`Validate()` 插一行。
- `examples/story-manager-kill.json` — **新**。實機樣本。
- 刪：`src/ModForge.Core/StoryManagerProbe.cs`、`tests/ModForge.Core.Tests/StoryManagerProbeTests.cs`、CLI `smprobe`（method+dispatch+Usage）。保留 `smtree`。
- 測試：`tests/ModForge.Core.Tests/Build/StoryManagerEventsTests.cs`、`StoryManagerBuildTests.cs`、`StoryManagerValidateTests.cs`。

---

## Task 1: Spec 型別

**Files:**
- Create: `src/ModForge.Core/Spec/Spec.StoryManager.cs`
- Modify: `src/ModForge.Core/Spec/Spec.Dialogue.cs`（QuestSpec）

- [ ] **Step 1: 建新 spec 型別**

`src/ModForge.Core/Spec/Spec.StoryManager.cs`：
```csharp
namespace ModForge;

// Story Manager 觸發宣告。掛在 QuestSpec 上 = 此 quest 可被 SM 的某事件啟動。
// event = 友善事件名（查 StoryManagerEvents 表）。conditions = 事件條件（沿用既有 ConditionSpec）。
public sealed class QuestStoryEventSpec
{
    public string Event { get; set; } = "";
    public List<ConditionSpec> Conditions { get; set; } = new();
}

// 一條 quest alias。fill 語法："fromEvent:<slot>"（拿事件帶來的 ref）或 "forced:<ref>"（寫死特定 ref）。
public sealed class QuestAliasSpec
{
    public string Name { get; set; } = "";
    public string Fill { get; set; } = "";
    public bool Optional { get; set; }
}
```

- [ ] **Step 2: QuestSpec 加兩欄位**

在 `src/ModForge.Core/Spec/Spec.Dialogue.cs` 的 `QuestSpec`（class，約 line 5-31），於 `Type` 欄位後加：
```csharp
    // Story Manager：宣告此 quest 可被某遊戲事件動態啟動（radiant 量產的底座）。有此塊時 build 會
    // 自動產生 SMBN→SMQN 把它掛到原版事件根下，並強制清除 StartGameEnabled（SM 啟動，不開局自跑）。
    public QuestStoryEventSpec? StoryEvent { get; set; }
    // SM 啟動時要填的 alias。fill="fromEvent:<slot>" 拿事件 ref，"forced:<ref>" 填寫死 ref。
    public List<QuestAliasSpec> Aliases { get; set; } = new();
```

- [ ] **Step 3: 編譯**

Run: `dotnet build src/ModForge.Core/ModForge.Core.csproj`
Expected: 成功（純資料型別，無邏輯）。

- [ ] **Step 4: Commit**

```bash
git add src/ModForge.Core/Spec/Spec.StoryManager.cs src/ModForge.Core/Spec/Spec.Dialogue.cs
git commit -m "feat(spec): QuestSpec storyEvent + aliases for Story Manager"
```

---

## Task 2: 事件表 + fill 解析

**Files:**
- Create: `src/ModForge.Core/StoryManagerEvents.cs`
- Test: `tests/ModForge.Core.Tests/Build/StoryManagerEventsTests.cs`

- [ ] **Step 1: 寫失敗測試**

`tests/ModForge.Core.Tests/Build/StoryManagerEventsTests.cs`：
```csharp
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class StoryManagerEventsTests
{
    [Fact]
    public void KillActor_def_has_root_code_and_slots()
    {
        Assert.True(StoryManagerEvents.TryGet("KillActor", out var def));
        Assert.Equal(new RecordType("KILL"), def.Code);
        Assert.Equal(0x013010u, def.Root.ID);
        Assert.Equal("Skyrim.esm", def.Root.ModKey.FileName);
        Assert.Equal(new byte[] { 0x52, 0x31, 0x00, 0x00 }, def.Slots["victim"]);
        Assert.Equal(new byte[] { 0x52, 0x32, 0x00, 0x00 }, def.Slots["killer"]);
    }

    [Fact]
    public void TryGet_is_case_insensitive_and_rejects_unknown()
    {
        Assert.True(StoryManagerEvents.TryGet("killactor", out _));
        Assert.False(StoryManagerEvents.TryGet("Nope", out _));
    }

    [Theory]
    [InlineData("fromEvent:victim", true, "fromEvent", "victim")]
    [InlineData("forced:SomeEd", true, "forced", "SomeEd")]
    [InlineData("forced:Skyrim.esm:0x013010", true, "forced", "Skyrim.esm:0x013010")]
    [InlineData("garbage", false, "", "")]
    [InlineData("", false, "", "")]
    public void TryParseFill_splits_kind_and_arg(string fill, bool ok, string kind, string arg)
    {
        Assert.Equal(ok, StoryManagerEvents.TryParseFill(fill, out var k, out var a));
        if (ok) { Assert.Equal(kind, k); Assert.Equal(arg, a); }
    }
}
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter StoryManagerEvents`
Expected: FAIL（StoryManagerEvents 不存在）。

- [ ] **Step 3: 實作事件表**

`src/ModForge.Core/StoryManagerEvents.cs`：
```csharp
using Mutagen.Bethesda.Plugins;

namespace ModForge;

// 一個 SM 事件的定義：原版事件根、Quest.Event 碼、可用的 event-data 槽位（slot 名 → 4-byte 索引）。
public readonly record struct StoryEventDef(FormKey Root, RecordType Code, IReadOnlyDictionary<string, byte[]> Slots);

// 內建「事件名 → 定義」表。一個事件一筆；之後加事件 = 加一筆（值離線從 Skyrim.esm vanilla 解出）。
public static class StoryManagerEvents
{
    private static readonly FormKey KillRoot = new(ModKey.FromNameAndExtension("Skyrim.esm"), 0x013010);

    private static readonly Dictionary<string, StoryEventDef> Defs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["KillActor"] = new StoryEventDef(
                KillRoot,
                new RecordType("KILL"),
                new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["victim"] = new byte[] { 0x52, 0x31, 0x00, 0x00 }, // "R1" = killed actor
                    ["killer"] = new byte[] { 0x52, 0x32, 0x00, 0x00 }, // "R2" = the killer
                }),
        };

    public static IEnumerable<string> Names => Defs.Keys;

    public static bool TryGet(string eventName, out StoryEventDef def) =>
        Defs.TryGetValue(eventName ?? "", out def);

    // "fromEvent:victim" → ("fromEvent","victim"); "forced:A:B" → ("forced","A:B"). 無冒號 = false。
    public static bool TryParseFill(string fill, out string kind, out string arg)
    {
        kind = ""; arg = "";
        if (string.IsNullOrWhiteSpace(fill)) return false;
        int i = fill.IndexOf(':');
        if (i <= 0 || i >= fill.Length - 1) return false;
        kind = fill[..i]; arg = fill[(i + 1)..];
        return true;
    }
}
```

- [ ] **Step 4: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter StoryManagerEvents`
Expected: PASS（5 cases）。

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/StoryManagerEvents.cs tests/ModForge.Core.Tests/Build/StoryManagerEventsTests.cs
git commit -m "feat(core): Story Manager event table (KillActor) + fill parser"
```

---

## Task 3: Build step

**Files:**
- Create: `src/ModForge.Core/Build/Generator.Build.StoryManager.cs`
- Modify: `src/ModForge.Core/Build/Generator.Build.cs`（orchestrator）
- Test: `tests/ModForge.Core.Tests/Build/StoryManagerBuildTests.cs`

- [ ] **Step 1: 寫失敗測試**

`tests/ModForge.Core.Tests/Build/StoryManagerBuildTests.cs`：
```csharp
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class StoryManagerBuildTests
{
    private static ModSpec SpecWithKillQuest()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "MFSM_Avenge",
            Name = "Avenge",
            Stages = { new StageSpec { Index = 10 } },
            StoryEvent = new QuestStoryEventSpec { Event = "KillActor" },
            Aliases =
            {
                new QuestAliasSpec { Name = "Victim", Fill = "fromEvent:victim" },
            },
        });
        return spec;
    }

    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    [Fact]
    public void StoryEvent_quest_gets_event_and_clears_startgame()
    {
        var mod = Build(SpecWithKillQuest());
        var q = mod.Quests.Single(x => x.EditorID == "MFSM_Avenge");
        Assert.Equal(new RecordType("KILL"), q.Event);
        Assert.False(q.Flags.HasFlag(Quest.Flag.StartGameEnabled));
        var alias = Assert.Single(q.Aliases);
        Assert.Equal("Victim", alias.Name);
        Assert.NotNull(alias.FindMatchingRefFromEvent);
        Assert.Equal(new RecordType("KILL"), alias.FindMatchingRefFromEvent!.FromEvent);
        Assert.Equal(new byte[] { 0x52, 0x31, 0x00, 0x00 },
            alias.FindMatchingRefFromEvent.EventData!.Value.ToArray());
    }

    [Fact]
    public void StoryEvent_quest_generates_branch_and_questnode()
    {
        var mod = Build(SpecWithKillQuest());
        var q = mod.Quests.Single(x => x.EditorID == "MFSM_Avenge");
        var branch = Assert.Single(mod.StoryManagerBranchNodes);
        var qnode = Assert.Single(mod.StoryManagerQuestNodes);
        Assert.Empty(mod.StoryManagerEventNodes);                     // additive
        Assert.Equal(0x013010u, branch.Parent.FormKey!.Value.ID);    // vanilla Kill Actor root
        Assert.Equal(branch.FormKey, qnode.Parent.FormKey);
        Assert.Equal(q.FormKey, Assert.Single(qnode.Quests).Quest.FormKey);
    }

    [Fact]
    public void Quest_without_storyevent_is_unchanged_no_sm_nodes()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec { EditorId = "Plain", Name = "Plain", StartGameEnabled = true });
        var mod = Build(spec);
        Assert.Empty(mod.StoryManagerBranchNodes);
        Assert.Empty(mod.StoryManagerQuestNodes);
        var q = mod.Quests.Single(x => x.EditorID == "Plain");
        Assert.True(q.Flags.HasFlag(Quest.Flag.StartGameEnabled));
    }

    [Fact]
    public void Forced_alias_sets_forced_reference()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "MFSM_Forced", Name = "F",
            StoryEvent = new QuestStoryEventSpec { Event = "KillActor" },
            Aliases = { new QuestAliasSpec { Name = "Boss", Fill = "forced:Skyrim.esm:0x000007" } },
        });
        var mod = Build(spec);
        var q = mod.Quests.Single(x => x.EditorID == "MFSM_Forced");
        var alias = Assert.Single(q.Aliases);
        Assert.Equal(0x000007u, alias.ForcedReference.FormKey!.Value.ID);
    }
}
```

> 註：`Generator.Build(...)` 回傳型別與 `.Mod` 屬性名以 `Generator.Build.cs` 的 `BuildResult` 為準（若屬性不叫 `Mod`，照實際名改）。`Quest.Aliases` 元素的 forced/flag 成員名以 Mutagen 為準（見上方 API 區）。

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter StoryManagerBuild`
Expected: FAIL（BuildStoryManager 還沒接；storyEvent quest 沒 event、沒節點）。

- [ ] **Step 3: 實作 build step**

`src/ModForge.Core/Build/Generator.Build.StoryManager.cs`：
```csharp
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // 把 spec 裏每個帶 storyEvent 的 quest 變成可被 SM 啟動：設 Quest.Event/EventConditions、清
        // StartGameEnabled、建 aliases，並 additive 生 SMBN→SMQN 掛到原版事件根下。在 BuildQuests() 後跑。
        private void BuildStoryManager()
        {
            foreach (var qs in spec.Quests)
            {
                if (qs.StoryEvent is not { } se) continue;
                if (string.IsNullOrEmpty(qs.EditorId) || !questsByEd.TryGetValue(qs.EditorId, out var quest)) continue;
                if (!StoryManagerEvents.TryGet(se.Event, out var def)) continue; // validator 已擋未知事件

                quest.Event = def.Code;
                quest.Flags &= ~Quest.Flag.StartGameEnabled;     // SM 啟動，不開局自跑
                foreach (var cs in se.Conditions)
                    if (BuildCondition(cs, $"quest '{qs.EditorId}' storyEvent condition") is { } cond)
                        quest.EventConditions.Add(cond);

                uint nextId = 0;
                foreach (var aSpec in qs.Aliases)
                {
                    var alias = new QuestAlias { ID = nextId, Name = aSpec.Name };
                    if (StoryManagerEvents.TryParseFill(aSpec.Fill, out var kind, out var arg))
                    {
                        if (kind.Equals("fromEvent", System.StringComparison.OrdinalIgnoreCase)
                            && def.Slots.TryGetValue(arg, out var slot))
                        {
                            alias.FindMatchingRefFromEvent = new FindMatchingRefFromEvent
                            {
                                FromEvent = def.Code,
                                EventData = (byte[])slot.Clone(),
                            };
                        }
                        else if (kind.Equals("forced", System.StringComparison.OrdinalIgnoreCase)
                            && TryResolveRef(arg, formKeyByEd, out var fk))
                        {
                            alias.ForcedReference.SetTo(fk);
                        }
                    }
                    if (aSpec.Optional) alias.Flags |= QuestAlias.Flag.Optional;
                    quest.Aliases.Add(alias);
                    nextId++;
                }
                quest.NextAliasID = nextId;

                var branch = mod.StoryManagerBranchNodes.AddNew();
                branch.EditorID = $"{qs.EditorId}_SMBranch";
                branch.Parent.SetTo(def.Root);

                var qnode = mod.StoryManagerQuestNodes.AddNew();
                qnode.EditorID = $"{qs.EditorId}_SMQuestNode";
                qnode.Parent.SetTo(branch);
                var entry = new StoryManagerQuest();
                entry.Quest.SetTo(quest);
                qnode.Quests.Add(entry);
            }
        }
    }
}
```

> 編譯時確認：`QuestAlias.Flag.Optional` 的確切枚舉名、`quest.EventConditions.Add(ConditionFloat)` 可行（ConditionFloat : Condition）。若名稱不符照 compiler 修正。

- [ ] **Step 4: 接 orchestrator**

在 `src/ModForge.Core/Build/Generator.Build.cs` 的 `ctx.BuildQuests();` 那行（約 line 27）**之後**、`ctx.BuildWordWallQuests();` 之前插入：
```csharp
        ctx.BuildStoryManager();                   // Story Manager: storyEvent quests → Event/aliases + SMBN/SMQN
```

- [ ] **Step 5: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter StoryManagerBuild`
Expected: PASS（4 tests）。

- [ ] **Step 6: 全測試（無回歸）**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`
Expected: 全綠（含舊的 StoryManagerProbeTests，Task 5 才刪）。

- [ ] **Step 7: Commit**

```bash
git add src/ModForge.Core/Build/Generator.Build.StoryManager.cs src/ModForge.Core/Build/Generator.Build.cs tests/ModForge.Core.Tests/Build/StoryManagerBuildTests.cs
git commit -m "feat(core): BuildStoryManager — storyEvent quests to Event/aliases + SMBN/SMQN"
```

---

## Task 4: Validator

**Files:**
- Create: `src/ModForge.Core/Validate/Generator.Validate.StoryManager.cs`
- Modify: `src/ModForge.Core/Validate/Generator.Validate.cs`（Validate() 派發）
- Test: `tests/ModForge.Core.Tests/Validate/StoryManagerValidateTests.cs`

- [ ] **Step 0: 確認 ValidateContext 的問題清單成員名**

Run: `grep -nE "Problems|problems|public List<string>|errors" src/ModForge.Core/Validate/Generator.Validate.cs`
用實際的清單欄位名（下方 code 用 `Problems`；若實為小寫 `problems` 或別名，照實改）。同時確認 `CheckRef` / `CheckCondition` 的確切簽名。

- [ ] **Step 1: 寫失敗測試**

`tests/ModForge.Core.Tests/Validate/StoryManagerValidateTests.cs`：
```csharp
using System.Linq;
using ModForge;
using Xunit;

public class StoryManagerValidateTests
{
    private static ModSpec QuestWith(QuestStoryEventSpec se, params QuestAliasSpec[] aliases)
    {
        var spec = new ModSpec();
        var q = new QuestSpec { EditorId = "Q", Name = "Q", StoryEvent = se };
        q.Aliases.AddRange(aliases);
        spec.Quests.Add(q);
        return spec;
    }

    [Fact]
    public void Unknown_event_is_an_error()
    {
        var problems = Generator.Validate(QuestWith(new QuestStoryEventSpec { Event = "Nope" }));
        Assert.Contains(problems, p => p.Contains("Nope") && p.Contains("storyEvent"));
    }

    [Fact]
    public void Unknown_fromevent_slot_is_an_error()
    {
        var problems = Generator.Validate(QuestWith(
            new QuestStoryEventSpec { Event = "KillActor" },
            new QuestAliasSpec { Name = "X", Fill = "fromEvent:bogus" }));
        Assert.Contains(problems, p => p.Contains("bogus"));
    }

    [Fact]
    public void Bad_fill_syntax_is_an_error()
    {
        var problems = Generator.Validate(QuestWith(
            new QuestStoryEventSpec { Event = "KillActor" },
            new QuestAliasSpec { Name = "X", Fill = "garbage" }));
        Assert.Contains(problems, p => p.Contains("fill") && p.Contains("garbage"));
    }

    [Fact]
    public void Valid_killactor_quest_has_no_problems()
    {
        var problems = Generator.Validate(QuestWith(
            new QuestStoryEventSpec { Event = "KillActor" },
            new QuestAliasSpec { Name = "Victim", Fill = "fromEvent:victim" }));
        Assert.DoesNotContain(problems, p => p.Contains("storyEvent") || p.Contains("fill"));
    }
}
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter StoryManagerValidate`
Expected: FAIL（未知事件/壞 fill 目前不報問題）。

- [ ] **Step 3: 實作 validator**

`src/ModForge.Core/Validate/Generator.Validate.StoryManager.cs`（`Problems`/`CheckRef` 名以 Step 0 為準）：
```csharp
namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        public void ValidateStoryManager()
        {
            foreach (var q in spec.Quests)
            {
                if (q.StoryEvent is not { } se) continue;
                var where = $"quest '{q.EditorId}' storyEvent";

                if (!StoryManagerEvents.TryGet(se.Event, out var def))
                {
                    Problems.Add($"{where} event '{se.Event}' is unknown (supported: {string.Join(", ", StoryManagerEvents.Names)})");
                    continue; // 沒有 def 就無法檢查 slot
                }

                if (se.Conditions != null)
                    foreach (var cs in se.Conditions)
                        CheckCondition(cs, $"{where} condition");

                foreach (var a in q.Aliases)
                {
                    if (!StoryManagerEvents.TryParseFill(a.Fill, out var kind, out var arg))
                    {
                        Problems.Add($"{where} alias '{a.Name}' fill '{a.Fill}' is malformed (expect 'fromEvent:<slot>' or 'forced:<ref>')");
                        continue;
                    }
                    if (kind.Equals("fromEvent", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (!def.Slots.ContainsKey(arg))
                            Problems.Add($"{where} alias '{a.Name}' fromEvent slot '{arg}' invalid for {se.Event} (slots: {string.Join(", ", def.Slots.Keys)})");
                    }
                    else if (kind.Equals("forced", System.StringComparison.OrdinalIgnoreCase))
                    {
                        CheckRef(arg, $"{where} alias '{a.Name}' forced ref");
                    }
                    else
                    {
                        Problems.Add($"{where} alias '{a.Name}' fill kind '{kind}' unsupported (use fromEvent | forced)");
                    }
                }

                if (q.StartGameEnabled)
                    Problems.Add($"{where}: quest sets startGameEnabled=true but a storyEvent quest is forced non-start-game-enabled (the flag will be cleared)");
            }
        }
    }
}
```

> 註：`QuestSpec.StartGameEnabled` 預設 true，所以「警告」對每個 SM quest 都會觸發。這是刻意的提示，不是錯誤——測試 `Valid_killactor_quest_has_no_problems` 只斷言不含 "storyEvent"/"fill" 字樣，但這條警告含 "storyEvent"。**為避免誤判，把這條警告改成不含 "storyEvent" 字樣**，例如前綴用 `quest '{q.EditorId}'`：
> ```csharp
>     Problems.Add($"quest '{q.EditorId}': startGameEnabled=true is ignored for a story-event quest (auto-cleared)");
> ```
> 採用此版本（不含 "storyEvent"），讓 valid-case 測試只反映事件/fill 問題。

- [ ] **Step 4: 接 Validate() 派發**

在 `src/ModForge.Core/Validate/Generator.Validate.cs` 的 `Validate()` 內，於 `ctx.ValidateWorld();` 後加：
```csharp
        ctx.ValidateStoryManager();
```

- [ ] **Step 5: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter StoryManagerValidate`
Expected: PASS（4 tests）。

- [ ] **Step 6: Commit**

```bash
git add src/ModForge.Core/Validate/Generator.Validate.StoryManager.cs src/ModForge.Core/Validate/Generator.Validate.cs tests/ModForge.Core.Tests/Validate/StoryManagerValidateTests.cs
git commit -m "feat(core): validate storyEvent — unknown event/slot/fill + startGameEnabled warning"
```

---

## Task 5: 退役探針 builder

**Files:**
- Delete: `src/ModForge.Core/StoryManagerProbe.cs`、`tests/ModForge.Core.Tests/StoryManagerProbeTests.cs`
- Modify: `src/ModForge.Cli/Diagnostics/Diagnostics.StoryManager.cs`（刪 `SmProbe`，留 `SmTree`）、`src/ModForge.Cli/Program.cs`（刪 smprobe dispatch + Usage 行）

- [ ] **Step 1: 刪除探針 builder + 其測試**

```bash
git rm src/ModForge.Core/StoryManagerProbe.cs tests/ModForge.Core.Tests/StoryManagerProbeTests.cs
```

- [ ] **Step 2: 移除 smprobe CLI（保留 smtree）**

在 `src/ModForge.Cli/Diagnostics/Diagnostics.StoryManager.cs` 刪掉整個 `SmProbe` 方法（含其上方註解區塊），保留 `SmTree`。
在 `src/ModForge.Cli/Program.cs` 刪掉這兩行：
```csharp
                case "smprobe" when args.Length == 3: return SmProbe(args[1], args[2]);
```
與 Usage() 內：
```csharp
        "  smprobe <out.esp> <0xKILLROOT>               write the SM probe plugin (kill-actor branch)\n" +
```

- [ ] **Step 3: 編譯 + 全測試**

Run: `dotnet build && dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`
Expected: 編譯成功（無 StoryManagerProbe 殘參照）；全測試綠（StoryManagerProbeTests 已不存在；新的 Events/Build/Validate 測試取代了結構覆蓋）。

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor: retire throwaway SM probe builder + smprobe CLI (superseded by spec pipeline)"
```

---

## Task 6: 樣本 spec + 實機驗證（手動）

**Files:**
- Create: `examples/story-manager-kill.json`

- [ ] **Step 1: 寫樣本 spec**

`examples/story-manager-kill.json`：
```json
{
  "quests": [
    {
      "editorId": "MFSM_Avenge",
      "name": "Avenge the Fallen",
      "stages": [ { "index": 10 } ],
      "storyEvent": { "event": "KillActor" },
      "aliases": [
        { "name": "Victim", "fill": "fromEvent:victim" }
      ]
    }
  ]
}
```

- [ ] **Step 2: validate + package（結構驗證）**

Run:
```
dotnet run --project src/ModForge.Cli -- validate examples/story-manager-kill.json
dotnet run --project src/ModForge.Cli -- package examples/story-manager-kill.json /tmp/mfsm_pkg
dotnet run --project src/ModForge.Cli -- dump /tmp/mfsm_pkg/MFSM*.esp
```
Expected: validate 無 error（startGameEnabled 提示可接受）；dump 顯示 1 Quest + 1 StoryManagerBranchNode + 1 StoryManagerQuestNode + master=Skyrim.esm。
（若 `package` 的輸出 esp 名稱/路徑不同，照實調整 dump 路徑。）

- [ ] **Step 3: 打包 FLAT zip 給使用者**

把 package 產出的 esp 打成 FLAT zip（plugin 在 zip 根，比照 packaging-zip-stale-file-trap），放 `~/skyrim_mods/`，確認 zip 根無舊 esp。

- [ ] **Step 4: 使用者實機測試**

1. MO2 安裝、啟用 esp，進遊戲。
2. **殺一頭牛**（或任何非 `SimpleActor` 的完整 actor；雞/兔是 SimpleActor，不會觸發）。
3. console：`sqv MFSM_Avenge`。
4. PASS = 任務 running + Victim alias 填上被殺者 FormID。

- [ ] **Step 5: 記錄結果 + 收尾**

把實機結果補進 `docs/minor/ideas.md` 第 9 節（階段二 PASS/FAIL + 撞到的事）。
```bash
git add examples/story-manager-kill.json docs/minor/ideas.md
git commit -m "docs+example: Story Manager spec-pipeline kill sample + in-game result"
```

---

## Self-Review 註記

- **Spec 覆蓋**：schema(Task1)、事件表+fill(Task2)、build step+orchestrator(Task3)、validator+派發(Task4)、退役探針(Task5)、樣本+實機(Task6) — 全對應設計各節。fromEvent+forced 都有 build 測試；未知事件/slot/壞 fill 都有 validator 測試；無-storyEvent 回歸有測試。
- **已知留白（非 placeholder）**：`BuildResult.Mod` 屬性名、`ValidateContext.Problems` 清單名、`QuestAlias.Flag.Optional` 枚舉名 — 皆以既有檔/compiler 為準，各 task 已標明確認點（Task3 註、Task4 Step0）。
- **型別一致**：`StoryManagerEvents.TryGet/TryParseFill/Names/StoryEventDef{Root,Code,Slots}` 全程一致；`BuildStoryManager`/`ValidateStoryManager` 命名一致；alias fill 語法 `fromEvent:`/`forced:` 在 build 與 validate 兩端一致。
- **回歸**：Task3 Step6 + Task5 Step3 跑全測試；無 storyEvent 的既有 quest 行為不變有專測。
