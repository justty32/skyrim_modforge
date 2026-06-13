# Story Manager 探針（階段一）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 用最少程式碼產出一個 SM 探針插件（SMBN→SMQN 掛在原版 Kill Actor 事件根下 + 帶 FromEvent alias 的模板 Quest），進遊戲殺一個 actor 後用 `sqv` 驗證 SM 是否啟動任務並填上被殺 actor 的 FormID。

**Architecture:** 繞過 spec→build 管線。Core 加一個純函式 builder（`StoryManagerProbe.BuildProbe`）直接用 Mutagen typed API 拼 `SkyrimMod`，可被單元測試做結構斷言。CLI 加兩個診斷指令：`smtree`（decode Skyrim.esm 找原版 Kill Actor SMEN 的 FormID + 事件型別）與 `smprobe`（呼叫 builder 寫出 esp）。最終驗證是遊戲內手動 `sqv`。

**Tech Stack:** C# / net10.0、Mutagen.Bethesda.Skyrim 0.53.1、現有 xUnit 測試專案、現有 package/zip 流程。

**設計來源：** `docs/superpowers/archive/specs/2026-06-04-story-manager-probe-design.md`

---

## File Structure

- `src/ModForge.Core/StoryManagerProbe.cs` — **新檔**。純 builder：`public static SkyrimMod BuildProbe(FormKey killActorEventRoot, ...)`。無 I/O，回傳記憶體中的 `SkyrimMod`，方便測試。
- `tests/ModForge.Core.Tests/StoryManagerProbeTests.cs` — **新檔**。結構斷言（不需 Skyrim.esm）。
- `src/ModForge.Cli/Diagnostics.StoryManager.cs` — **新檔**。`smtree`（decode）+ `smprobe`（寫 esp）兩個 partial method。
- `src/ModForge.Cli/Program.cs` — **改**。dispatch switch 加兩個 case + Usage 兩行。

---

## Task 0: API 釘樁（pin Mutagen 的確切型別）

**目的：** SMEN.Type、Quest.Event、FindMatchingRefFromEvent.{EventData,FromEvent} 的確切 C# 型別 strings 看不出來。先寫一個會編譯失敗的最小 builder，用 compiler 把型別逼出來，避免後面整段猜錯。

**Files:**
- Create: `src/ModForge.Core/StoryManagerProbe.cs`

- [ ] **Step 1: 寫最小 skeleton（故意只填已確認存在的成員）**

```csharp
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;

namespace ModForge.Core;

public static class StoryManagerProbe
{
    // killActorEventRoot: 原版 Skyrim.esm 的 Kill Actor SMEN FormKey（由 `smtree` 找出）。
    // eventType: 該 SMEN 的事件型別，Quest.Event 必須與之相符（由 `smtree` 印出）。
    public static SkyrimMod BuildProbe(FormKey killActorEventRoot)
    {
        var mod = new SkyrimMod(ModKey.FromNameAndExtension("ModForgeStoryManager.esp"), SkyrimRelease.SkyrimSE);

        // 模板 Quest
        var quest = mod.Quests.AddNew("MFSM_AvengeQuest");
        // TODO(Task1): Event / EventConditions / Aliases(FindMatchingRefFromEvent) / startup stage / flags

        // SMBN：Parent 指向原版 Kill Actor SMEN（additive child，不 override 原版）
        var branch = mod.StoryManagerBranchNodes.AddNew("MFSM_Branch");
        branch.Parent.SetTo(killActorEventRoot);

        // SMQN：Parent 指向我們的 SMBN，Quests 列表掛模板任務
        var qnode = mod.StoryManagerQuestNodes.AddNew("MFSM_QuestNode");
        qnode.Parent.SetTo(branch.FormKey);
        var entry = new StoryManagerQuest();
        entry.Quest.SetTo(quest.FormKey);
        qnode.Quests.Add(entry);

        return mod;
    }
}
```

- [ ] **Step 2: 編譯，讓 compiler 報出確切型別**

Run: `dotnet build src/ModForge.Core/ModForge.Core.csproj`
Expected: 編譯通過（若 `StoryManagerBranchNodes` / `StoryManagerQuestNodes` group 名或 `Parent.SetTo` 簽名不符會在此報錯——照 compiler 修正 group 屬性名與 `IFormLink.SetTo`/`SetTo(FormKey)` 用法）。

- [ ] **Step 3: 記下釘出的事實**

把確認的型別寫進檔案頂端註解：`StoryManagerQuestNode.Quests` 的元素型別、`AStoryManagerNode.Parent` 是 `IFormLinkNullable<...>` 還是 `IFormLink<...>`、group 屬性的確切名稱（`mod.StoryManagerBranchNodes` 等）。後續 task 依此寫。

- [ ] **Step 4: Commit**

```bash
git add src/ModForge.Core/StoryManagerProbe.cs
git commit -m "feat(core): SM probe builder skeleton — pin Mutagen SMBN/SMQN API"
```

---

## Task 1: 模板 Quest（Event + FromEvent alias + startup stage）

**Files:**
- Modify: `src/ModForge.Core/StoryManagerProbe.cs`
- Test: `tests/ModForge.Core.Tests/StoryManagerProbeTests.cs`

- [ ] **Step 1: 寫失敗測試（結構斷言，不需 Skyrim.esm）**

```csharp
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge.Core;
using Xunit;

public class StoryManagerProbeTests
{
    // 任意假的 Kill Actor SMEN FormKey（測試不碰真 Skyrim.esm）
    private static readonly FormKey FakeKillRoot =
        new(ModKey.FromNameAndExtension("Skyrim.esm"), 0x0ABCDE);

    [Fact]
    public void Quest_has_event_and_fromevent_alias()
    {
        var mod = StoryManagerProbe.BuildProbe(FakeKillRoot);
        var q = Assert.Single(mod.Quests);

        // 可被 SM 啟動：Event 欄位有設、非開局啟動
        Assert.NotNull(q.Event);
        Assert.False(q.StartGameEnabled);

        // 一條 Victim alias，填充型別 = FindMatchingRefFromEvent
        var alias = Assert.Single(q.Aliases);
        Assert.Equal("Victim", alias.Name);
        Assert.NotNull(alias.FindMatchingRefFromEvent);

        // 有一個 startup stage 讓 sqv 看得到任務啟動
        Assert.Contains(q.Stages, s => s.Index == 10);
    }
}
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter Quest_has_event_and_fromevent_alias`
Expected: FAIL（alias 為空 / Event 為 null / 無 stage 10）。

- [ ] **Step 3: 在 builder 填上 Quest 細節**

在 `BuildProbe` 的 `quest` 區塊填入（成員名以 Task 0 釘出的事實為準；以下為已確認存在的成員）：

```csharp
        quest.Name = "ModForge 復仇探針";
        quest.StartGameEnabled = false;          // 靠 SM 啟動，非開局
        quest.Event = RecordTypes.Kmrk;          // 佔位：改成 Task0/smtree 釘出的 Kill Actor 事件碼
        // 若 Quest.Event 型別是 RecordType?，用 4-char code；是 enum 則用對應 enum 值。

        // startup stage 10：讓 sqv 顯示任務 running（最簡：只需存在 Index==10 的 stage）
        quest.Stages.Add(new QuestStage { Index = 10 });

        // Victim alias：拿 Kill Actor 事件帶來的「被殺 ref」填充
        var victim = new QuestAlias
        {
            ID = 0,
            Name = "Victim",
            FindMatchingRefFromEvent = new FindMatchingRefFromEvent
            {
                // EventData：事件中哪個 ref 槽（Kill Actor 的被殺者）。
                // FromEvent：事件型別碼。兩者確切型別由 Task0 釘出，值由 smtree 對照。
            },
        };
        quest.Aliases.Add(victim);
        quest.NextAliasID = 1;
```

> 註：`QuestStage`/`QuestLogEntry` 的精確建構以現有 `Generator.Build.QuestStages.cs` 為範本（該檔已實機驗證過 stage/log 寫法）。上面 stage 區塊保留最簡：只要 `Index==10` 的 stage 存在即可，log entry 可省。

- [ ] **Step 4: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter Quest_has_event_and_fromevent_alias`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/StoryManagerProbe.cs tests/ModForge.Core.Tests/StoryManagerProbeTests.cs
git commit -m "feat(core): SM probe quest — Event + FromEvent Victim alias + startup stage"
```

---

## Task 2: SM 節點樹結構斷言（PNAM 連結）

**Files:**
- Test: `tests/ModForge.Core.Tests/StoryManagerProbeTests.cs`

- [ ] **Step 1: 寫失敗測試**

```csharp
    [Fact]
    public void Branch_parents_vanilla_root_and_questnode_parents_branch()
    {
        var mod = StoryManagerProbe.BuildProbe(FakeKillRoot);

        var branch = Assert.Single(mod.StoryManagerBranchNodes);
        var qnode = Assert.Single(mod.StoryManagerQuestNodes);

        // SMBN.Parent → 原版 Kill Actor SMEN（additive，不在本 mod 內建 SMEN）
        Assert.Empty(mod.StoryManagerEventNodes);
        Assert.Equal(FakeKillRoot, branch.Parent.FormKey);

        // SMQN.Parent → 我們的 SMBN
        Assert.Equal(branch.FormKey, qnode.Parent.FormKey);

        // SMQN 掛了模板任務
        var entry = Assert.Single(qnode.Quests);
        Assert.Equal(Assert.Single(mod.Quests).FormKey, entry.Quest.FormKey);
    }
```

- [ ] **Step 2: 跑測試**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter Branch_parents_vanilla_root`
Expected: PASS（Task 0 skeleton 已實作這部分；本測試只是把不變式釘死。若 FAIL 照斷言修 builder。）

- [ ] **Step 3: Commit**

```bash
git add tests/ModForge.Core.Tests/StoryManagerProbeTests.cs
git commit -m "test(core): pin SM probe PNAM linkage (branch→vanilla root, qnode→branch)"
```

---

## Task 3: CLI `smtree` — decode Skyrim.esm 找 Kill Actor SMEN

**Files:**
- Create: `src/ModForge.Cli/Diagnostics.StoryManager.cs`
- Modify: `src/ModForge.Cli/Program.cs`（dispatch + Usage）

無單元測試（需真 Skyrim.esm，屬環境相依，比照現有 `*diag` 指令——由使用者手動跑）。

- [ ] **Step 1: 寫 `smtree` decode 指令**

仿 `Diagnostics.Weather.cs` 的 `WeatherDiag` 風格。列出所有 SMEN（事件根），印 EditorID / FormID / Type / Flags；特別標出 EditorID 含 "Kill" 的那個。

```csharp
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

internal static partial class Program
{
    // smtree <Skyrim.esm> — 列出所有 Story Manager event 根（SMEN）。
    // 目的：找出 Kill Actor 事件根的 FormID + Type，餵給 smprobe / BuildProbe。
    private static int SmTree(string inPath)
    {
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var en in mod.EnumerateMajorRecords<IStoryManagerEventNodeGetter>())
        {
            var mark = (en.EditorID ?? "").Contains("Kill", System.StringComparison.OrdinalIgnoreCase) ? "  <-- KILL?" : "";
            System.Console.WriteLine($"{en.FormKey}  EditorID={en.EditorID}  Type={en.Type}  Flags={en.Flags}  Max={en.MaxConcurrentQuests}{mark}");
        }
        return 0;
    }
}
```

- [ ] **Step 2: 接 dispatch + Usage**

在 `src/ModForge.Cli/Program.cs` 的 `switch (args[0])` 內，仿 `weatherdiag` 那行加：

```csharp
                case "smtree" when args.Length == 2: return SmTree(args[1]);
```

在 `Usage()` 的字串清單裏加一行：

```csharp
        "  smtree <Skyrim.esm>                          list Story Manager event roots (find Kill Actor SMEN)\n" +
```

- [ ] **Step 3: 編譯**

Run: `dotnet build src/ModForge.Cli/ModForge.Cli.csproj`
Expected: 成功。

- [ ] **Step 4: 使用者手動跑（需本機 Skyrim.esm）**

Run: `dotnet run --project src/ModForge.Cli -- smtree /path/to/Skyrim.esm`
Expected: 印出一串 SMEN。記下 Kill Actor 那個的 **FormKey** 與 **Type**。寫進筆記（比照 navmesh 的 0x00012FB4 記法）。

> 若沒有任何 SMEN 列出 → Skyrim.esm 的 SM 樹可能不在 master 而在子 esp，或 Mutagen group 名不同；改用 `EnumerateMajorRecords<IAStoryManagerNodeGetter>()` 全列出再篩。

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Cli/Diagnostics.StoryManager.cs src/ModForge.Cli/Program.cs
git commit -m "feat(cli): smtree — decode Skyrim.esm Story Manager event roots"
```

---

## Task 4: CLI `smprobe` — 寫出探針 esp

**Files:**
- Modify: `src/ModForge.Cli/Diagnostics.StoryManager.cs`
- Modify: `src/ModForge.Cli/Program.cs`（dispatch + Usage）

- [ ] **Step 1: 寫 `smprobe` 指令**

```csharp
    // smprobe <out.esp> <0xKILLROOT> — 用 StoryManagerProbe.BuildProbe 寫出探針插件。
    private static int SmProbe(string outPath, string killRootHex)
    {
        uint id = System.Convert.ToUInt32(killRootHex.Replace("0x", "", System.StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        var killRoot = new FormKey(ModKey.FromNameAndExtension("Skyrim.esm"), id);
        var mod = ModForge.Core.StoryManagerProbe.BuildProbe(killRoot);
        mod.WriteToBinaryParallel(outPath);   // 簽名以現有寫檔處為準（見 Generator 寫檔）
        System.Console.WriteLine($"wrote {outPath} (kill root {killRoot})");
        return 0;
    }
```

> 寫檔 API：以 repo 內既有的 `SkyrimMod` 寫檔呼叫為準（grep `WriteToBinary` 找現有用法並對齊；若需 `BinaryWriteParameters`/masters 設定，照現有 PackageCmd/BuildCmd 流程）。

- [ ] **Step 2: 接 dispatch + Usage**

```csharp
                case "smprobe" when args.Length == 3: return SmProbe(args[1], args[2]);
```

```csharp
        "  smprobe <out.esp> <0xKILLROOT>               write the SM probe plugin (kill-actor branch)\n" +
```

- [ ] **Step 3: 編譯 + 全測試**

Run: `dotnet build src/ModForge.Cli/ModForge.Cli.csproj && dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`
Expected: build 成功；測試 259/260（已知 WordWall 環境性失敗不算 regression）。

- [ ] **Step 4: 產出探針 esp（用 Task 3 記下的 FormID）**

Run: `dotnet run --project src/ModForge.Cli -- smprobe /tmp/ModForgeStoryManager.esp 0x<KILLROOT>`
Expected: 印 `wrote ...`。用 `dotnet run --project src/ModForge.Cli -- dump /tmp/ModForgeStoryManager.esp` 確認含 1 Quest + 1 SMBN + 1 SMQN、master 含 Skyrim.esm。

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Cli/Diagnostics.StoryManager.cs src/ModForge.Cli/Program.cs
git commit -m "feat(cli): smprobe — emit Story Manager probe plugin"
```

---

## Task 5: 打包 + 遊戲內驗證（手動）

**Files:** 無程式碼改動——產出測試結果記錄。

- [ ] **Step 1: 打包進 MO2 安裝目錄**

把 `/tmp/ModForgeStoryManager.esp` 包成 FLAT zip（plugin 在 zip 根，比照 [[packaging-zip-stale-file-trap]] 的教訓），放到 `~/skyrim_mods/`。確認 zip 根沒有任何舊 esp。

- [ ] **Step 2: 開 Story Manager log（除錯眼睛）**

在 `Documents/My Games/Skyrim Special Edition/SkyrimCustom.ini` 設 `[Papyrus] bEnableLogging=1`，並確認 SM log 會寫到 `.../Logs/Story Manager.log`。

- [ ] **Step 3: 遊戲內測試**

1. MO2 啟用 ModForgeStoryManager.esp，進遊戲（新檔或既有檔皆可；`.seq` 不需要）。
2. 殺任意一個 actor（雞／兔／盜賊）。
3. console：`sqv MFSM_AvengeQuest`。

- [ ] **Step 4: 判定**

- **PASS**：`sqv` 顯示任務 running（stage 10 set）且 alias "Victim" = 剛被殺 actor 的 FormID。
- **FAIL**：任務沒 running / alias 空。
  - 查 Story Manager.log：SM 有沒有走到我們的 SMBN？哪個條件擋掉？alias 填不出來？
  - 隔離變數：把 Victim alias 暫時改成 forced specific reference（ModForge 已支援），重產重測——確認「SM 啟動」與「alias 填充」哪一層壞。

- [ ] **Step 5: 記錄結果**

把結果寫進 `docs/minor/ideas.md` 第 9 節 Story Manager 段（或新筆記檔），含：Kill Actor SMEN FormID、Quest.Event 用的事件碼、PASS/FAIL、撞到的引擎 quirks。PASS → 階段二（spec schema + Generator.Build.StoryManager.cs + validator）另起 brainstorm。

```bash
git add docs/minor/ideas.md
git commit -m "docs(ideas): Story Manager probe in-game result"
```

---

## Self-Review 註記

- **Spec 覆蓋**：觸發=Kill Actor（Task1 Event + Task3 找根）、填充=FromEvent（Task1 alias）、成功訊號=sqv（Task5）、探針優先繞過管線（builder 在 Core、無 spec schema）、SM log 除錯（Task5 Step2）、forced-ref 隔離退路（Task5 Step4）——皆對應。階段二明確排除。
- **已知留白（非 placeholder，是探針的本質未知）**：(a) Kill Actor SMEN 的真實 FormID → `smtree` 執行期取得；(b) `Quest.Event` / `FindMatchingRefFromEvent` 的確切型別與值 → Task 0 compile 釘樁 + `smtree` 印的 Type 對照。這兩者無法在離線寫計畫時定死，已用獨立步驟把不確定性圈起來。
- **型別一致**：builder 名 `StoryManagerProbe.BuildProbe(FormKey)` 全程一致；測試引用同簽名。
