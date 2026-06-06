# PlayIdle scene-action Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓 scene 的某個 phase 能讓指定 actor 播放一段指定 IDLE 動畫(下跪、祈禱…),動畫與 phase 對齊、結束自然回 AI。

**Architecture:** 新增 `SceneActionSpec.Idle`(IDLE ref)。build 把 idle-action 登記為 SCEN `SceneAdapter.ScriptFragments.PhaseFragments[]` 的 per-phase fragment(**不**產生 `SceneAction`)。新增純函式 `GenerateSceneFragmentSource(SceneSpec)`(鏡像 `GenerateQuestFragmentSource`)產 `SF_<scene>` Papyrus 腳本;`package` 編譯它並掛 `SceneAdapter` VMAD(僅當 `.pex` 在,鏡像 TIF gating)。順手把 CLI `find` 擴 `IdleAnimation` 型別以便查 idle FormID。

**Tech Stack:** C# net10.0、Mutagen.Bethesda.Skyrim 0.53.1、xUnit、Wine+CK PapyrusCompiler(僅 package 編譯期)。

**設計來源:** `docs/superpowers/specs/2026-06-06-playidle-scene-action-design.md`
**下游:** 多重身份系統(`docs/superpowers/specs/2026-06-06-identity-system-design.md`)聖騎士宣誓演出。

**前置閱讀(CODE_MAP):** `docs/CODE_MAP.dialogue-quests.md` 的 scene 段(`Generator.Build.Scene.cs` / `Generator.QuestFragments.cs` / `Generator.Build.Scripts.cs` / `Spec.Dialogue.cs`)。

**測試指令(全程):** `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`
已知環境性失敗:`WordWallTests.Trigger_placed_referencing_word_wall_activator_base`(缺本機 Skyrim.esm),不是 regression。

---

## Task 0: 解碼 vanilla scene 的 phase fragment 慣例(spike,釘死 API)

**目的:** `ScenePhaseFragment` 的 `Index` 是 0-based phase 還是 1-based?`FragmentName` 與 Papyrus 函式名的確切慣例?scene fragment 怎麼取 scene actor 的 `ObjectReference`?這些決定 Task 4 的 code,不能猜。

**Files:**
- 暫時性:可在 `src/ModForge.Cli/Diagnostics.cs` 加一個 throwaway `scnvmad <plugin> <FormID>` 指令 dump 一個含 phase fragment 的 vanilla SCEN 的 `SceneAdapter`(或用既有 xEdit 知識)。**不 commit 此 throwaway。**

- [ ] **Step 1: 找一個有 phase fragment 的 vanilla scene**

用 xEdit 或 Mutagen overlay 在 Skyrim.esm 找一個帶 Papyrus phase fragment 的 SCEN(多數有演出的對話場景皆有,如 MQ/城鎮 radiant scene)。記下其 `SceneAdapter.ScriptFragments`:`FileName`、每個 `PhaseFragments` 項的 `Index`/`Flags`/`ScriptName`/`FragmentName`。

- [ ] **Step 2: 取對應的 vanilla SF_ 腳本 source,核對函式名**

從 `~/.cache/modforge/papyrus/Source/Scripts`(14301 .psc 全 source set)找該 `ScriptName` 的 `.psc`,確認:(a) `extends Scene`;(b) phase fragment 函式的確切簽章與命名(例如 `Function Fragment_<N>()` 中 N 對應 `FragmentName` 還是 phase `Index`);(c) 它怎麼取 actor —— 是 `GetOwningQuest()` cast + alias property,還是 scene-native API。

- [ ] **Step 3: 把結論寫進設計文件的「待精修」段**

把 (Index 基數、FragmentName↔函式名規則、取 actor 寫法) 三點補進 `docs/superpowers/specs/2026-06-06-playidle-scene-action-design.md`。Task 2/4 以此為準。

- [ ] **Step 4: 移除 throwaway 診斷指令(若有加)**

確認 `git status` 不含 throwaway。Spike 無 production code 變更,不 commit(設計文件的補充可併入 Task 6 docs commit)。

---

## Task 1: 新增 `SceneActionSpec.Idle` 欄位 + schema

**Files:**
- Modify: `src/ModForge.Core/Spec.Dialogue.cs:168-175`(SceneActionSpec)
- Modify: `examples/spec.schema.json`(scene action 物件加 `idle`)
- Test: `tests/ModForge.Core.Tests/SceneTests.cs`

- [ ] **Step 1: 寫失敗測試 — idle 欄位存在且預設為空**

加到 `tests/ModForge.Core.Tests/SceneTests.cs`:

```csharp
[Fact]
public void SceneActionSpec_Idle_defaults_empty()
{
    var ac = new SceneActionSpec();
    Assert.Equal("", ac.Idle);
}
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter SceneActionSpec_Idle_defaults_empty`
Expected: 編譯失敗(`SceneActionSpec` 無 `Idle`)。

- [ ] **Step 3: 加欄位**

在 `src/ModForge.Core/Spec.Dialogue.cs` 的 `SceneActionSpec`(行 168-175)末尾加:

```csharp
    public string Idle { get; set; } = "";   // ref → 一個 IDLE 記錄;非空 = 此 action 在 StartPhase 播該 idle
                                              //（走 SceneAdapter phase fragment,不產生 SceneAction）
```

- [ ] **Step 4: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter SceneActionSpec_Idle_defaults_empty`
Expected: PASS。

- [ ] **Step 5: 更新 schema**

在 `examples/spec.schema.json` 找 scene action 的物件定義,於其 `properties` 加:

```json
"idle": { "type": "string", "description": "ref to an IDLE record; non-empty makes this a PlayIdle action on StartPhase" }
```

- [ ] **Step 6: Commit**

```bash
git add src/ModForge.Core/Spec.Dialogue.cs examples/spec.schema.json tests/ModForge.Core.Tests/SceneTests.cs
git commit -m "feat(scene): add SceneActionSpec.Idle field (PlayIdle action)" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: `GenerateSceneFragmentSource` 純產生器 + 測試

**Files:**
- Create: `src/ModForge.Core/Generator.SceneFragments.cs`
- Test: `tests/ModForge.Core.Tests/SceneFragmentTests.cs`(新檔)

> 函式名慣例以 Task 0 spike 的結論為準;以下用 `Fragment_<phase>`(0-based phase index)作為**待 spike 確認的暫定慣例**——若 spike 顯示不同,Step 3 的字串與測試一起改。

- [ ] **Step 1: 寫失敗測試 — 有 idle action 的 scene 產生 SF_ 腳本**

新檔 `tests/ModForge.Core.Tests/SceneFragmentTests.cs`:

```csharp
using ModForge;
using Xunit;

public class SceneFragmentTests
{
    private static SceneSpec OneIdleScene() => new()
    {
        EditorId = "MF_OathScene", QuestEditorId = "MF_OathQuest",
        Actors = { new SceneActorSpec { /* aliasId 0 */ } },
        Phases =
        {
            new ScenePhaseSpec { Speaker = 0, Lines = { "" } },   // phase 0: kneel
            new ScenePhaseSpec { Speaker = 0, Lines = { "" } },   // phase 1: stand
        },
        Actions =
        {
            new SceneActionSpec { Actor = 0, StartPhase = 0, Idle = "Skyrim.esm:0x000A0000" },
            new SceneActionSpec { Actor = 0, StartPhase = 1, Idle = "Skyrim.esm:0x000B0000" },
        },
    };

    [Fact]
    public void Scene_with_idle_actions_needs_fragment_script()
    {
        var s = OneIdleScene();
        Assert.True(Generator.SceneNeedsFragmentScript(s));
        Assert.Equal("SF_MF_OathScene", Generator.SceneFragmentScriptName(s));
    }

    [Fact]
    public void Scene_fragment_source_has_extends_Scene_and_one_function_per_idle_phase()
    {
        var src = Generator.GenerateSceneFragmentSource(OneIdleScene());
        Assert.Contains("Scriptname SF_MF_OathScene extends Scene", src);
        Assert.Contains("Function Fragment_0()", src);   // phase 0 idle
        Assert.Contains("Function Fragment_1()", src);   // phase 1 idle
        Assert.Contains("PlayIdle", src);
    }

    [Fact]
    public void Scene_without_idle_actions_gets_no_fragment_script()
    {
        var s = new SceneSpec
        {
            EditorId = "MF_Plain", QuestEditorId = "MF_Q",
            Actors = { new SceneActorSpec() },
            Phases = { new ScenePhaseSpec { Speaker = 0, Lines = { "Hi" } } },
            Actions = { new SceneActionSpec { Actor = 0, StartPhase = 0 } },   // dialog, no idle
        };
        Assert.False(Generator.SceneNeedsFragmentScript(s));
        Assert.Equal("", Generator.SceneFragmentScriptName(s));
        Assert.Equal("", Generator.GenerateSceneFragmentSource(s));
    }
}
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter SceneFragmentTests`
Expected: 編譯失敗(`Generator.SceneNeedsFragmentScript` 等不存在)。

- [ ] **Step 3: 寫產生器**

新檔 `src/ModForge.Core/Generator.SceneFragments.cs`:

```csharp
namespace ModForge;

public static partial class Generator
{
    // Scene phase-fragment GENERATION (PlayIdle). Mirrors GenerateQuestFragmentSource: pure
    // string in/out, unit-testable with no Skyrim master / Wine dependency. An "idle action"
    // (SceneActionSpec.Idle non-empty) compiles to a per-phase begin fragment that plays the idle.

    public static bool SceneNeedsFragmentScript(SceneSpec s) =>
        s.Actions.Any(a => !string.IsNullOrWhiteSpace(a.Idle));

    public static string SceneFragmentScriptName(SceneSpec s) =>
        SceneNeedsFragmentScript(s) ? $"SF_{Sanitize(s.EditorId)}" : "";

    /// <summary>Papyrus source for a scene's phase-fragment script: one Fragment per phase that
    /// has an idle action, each calling PlayIdle on that action's actor. The idle form and the
    /// actor alias are bound as script properties by `package` (AttachSceneFragments).</summary>
    public static string GenerateSceneFragmentSource(SceneSpec s)
    {
        if (!SceneNeedsFragmentScript(s)) return "";
        var name = SceneFragmentScriptName(s);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Scriptname {name} extends Scene");
        sb.AppendLine("; AUTO-GENERATED by ModForge — scene phase idle playback.");
        sb.AppendLine("; `package` compiles this and attaches the SceneAdapter VMAD automatically.");
        sb.AppendLine();

        // One idle action per phase (first wins if several share a phase), ascending by phase.
        var idleActions = s.Actions
            .Where(a => !string.IsNullOrWhiteSpace(a.Idle))
            .GroupBy(a => a.StartPhase)
            .Select(g => g.First())
            .OrderBy(a => a.StartPhase);

        foreach (var a in idleActions)
        {
            // Properties bound by package: Idle_<phase> (the IDLE) and Actor_<phase> (alias ref).
            sb.AppendLine($"Idle Property Idle_{a.StartPhase} Auto");
            sb.AppendLine($"ReferenceAlias Property Actor_{a.StartPhase} Auto");
            sb.AppendLine($"Function Fragment_{a.StartPhase}()");
            sb.AppendLine($"    ; phase {a.StartPhase}: alias {a.Actor} plays idle {OneLine(a.Idle)}");
            sb.AppendLine($"    Actor a = Actor_{a.StartPhase}.GetActorReference()");
            sb.AppendLine($"    if a");
            sb.AppendLine($"        a.PlayIdle(Idle_{a.StartPhase})");
            sb.AppendLine($"    endif");
            sb.AppendLine("EndFunction");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
```

> `Sanitize` 與 `OneLine` 是 `Generator` 既有 helper(`GenerateQuestFragmentSource` 同用)。取 actor 的寫法(`ReferenceAlias.GetActorReference()`)以 Task 0 spike 為準;若 spike 顯示需 `GetOwningQuest()` cast,改這幾行與屬性型別。

- [ ] **Step 4: 跑測試確認通過**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter SceneFragmentTests`
Expected: PASS(3 個)。

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/Generator.SceneFragments.cs tests/ModForge.Core.Tests/SceneFragmentTests.cs
git commit -m "feat(scene): GenerateSceneFragmentSource — pure PlayIdle fragment generator" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: build 端 — idle-action 不產生 SceneAction(純 build 不掛 VMAD)

**Files:**
- Modify: `src/ModForge.Core/Generator.Build.Scene.cs:145-170`(action 分派迴圈)
- Test: `tests/ModForge.Core.Tests/SceneTests.cs`

- [ ] **Step 1: 寫失敗測試 — idle action 不產生 Package SceneAction**

加到 `SceneTests.cs`(沿用既有 `TestBuild.Ok` / `TheScene` helper,行 45-47;idle ref 用 build 環境不需解析的形式——若純 build 會驗 ref,改用一個 spec 內 IDLE editorId 或既有 vanilla idle):

```csharp
[Fact]
public void Idle_action_does_not_emit_a_Package_or_Timer_SceneAction()
{
    var spec = TwoActorScene();   // 既有 helper:3 phase 對話 scene
    // 在 phase 0 加一個 idle action(不應變成 SceneAction)
    spec.Scenes[0].Actions.Add(new SceneActionSpec { Actor = 0, StartPhase = 0, Idle = "Skyrim.esm:0x000A0000" });
    var r = TestBuild.Ok(spec);
    var sc = TheScene(r);
    // 三句對話仍各有一個 Dialog action;idle action 不增加 SceneAction
    Assert.Equal(3, sc.Actions.Count);
    Assert.DoesNotContain(sc.Actions, a => a.Type == SceneAction.TypeEnum.Package);
    // 純 build(無 compiled scripts)不掛 SceneAdapter VMAD
    Assert.Null(sc.VirtualMachineAdapter);
}
```

> 若 `TwoActorScene()` 回傳型別不便直接改 Actions,複製其建構碼成本地 spec 再加 idle action。確切 helper 形狀見 `SceneTests.cs` 頂部。

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter Idle_action_does_not_emit`
Expected: FAIL(目前 idle action 會落入 `else` 分支變成 Package action → `sc.Actions.Count == 4` 或含 Package)。

- [ ] **Step 3: 在 action 分派加 idle 短路分支**

在 `src/ModForge.Core/Generator.Build.Scene.cs` 的 action 迴圈,於建立 `act`/設 Type 之前(約行 149 `var act = new SceneAction...` 之前)加:

```csharp
// PlayIdle action: handled via the SceneAdapter phase fragment (Task 4 / package), NOT as a
// SceneAction record. Skip emitting a SceneAction for it; the fragment fires on phase begin.
if (!string.IsNullOrWhiteSpace(ac.Idle))
    continue;
```

> 確切插入點:在處理 Package/Timer 的那段(Explore 報告行 156-165)之前、且在 `act` 加入 `scene.Actions` 之前。對照現場行號微調。

- [ ] **Step 4: 跑測試確認通過 + 全測試綠**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`
Expected: 新測試 PASS;其餘綠(除已知 WordWall 環境性失敗)。

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/Generator.Build.Scene.cs tests/ModForge.Core.Tests/SceneTests.cs
git commit -m "feat(scene): idle actions skip SceneAction emission (handled via phase fragment)" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: package 端 — 編譯 SF_ + 掛 SceneAdapter VMAD(.pex 在才掛)

**Files:**
- Modify: `src/ModForge.Cli/Package.cs:47-58`(fragment 編譯迴圈)
- Modify: `src/ModForge.Core/Generator.Build.Scripts.cs`(新增 `AttachSceneFragments`,鏡像 `AttachDialogueResultScripts` 行 44-95)
- Test: `tests/ModForge.Core.Tests/SceneFragmentTests.cs`(VMAD attach 的純斷言,給定一個 fake 已編譯目錄)

> `ScenePhaseFragment` 欄位(Explore 報告):`Flags`(OnStart=0x01/OnCompletion=0x02)、`Index`、`Unknown`、`ScriptName`、`FragmentName`。`Index` 基數與 `FragmentName`↔函式名規則以 Task 0 spike 為準。

- [ ] **Step 1: 寫失敗測試 — 提供 .pex 時掛上 SceneAdapter + PhaseFragments**

`AttachSceneFragments` 應接受「已編譯腳本目錄」(同 TIF 的 `options.CompiledScriptsDir` gating)。測試建一個 temp 目錄、放一個 `SF_<scene>.pex` 空檔,build 後斷言 scene 有 `SceneAdapter`、`ScriptFragments.PhaseFragments` 數 = idle 數、每項 `ScriptName == "SF_<scene>"`、`Flags` 含 OnStart。

```csharp
[Fact]
public void Scene_fragments_attached_when_pex_present()
{
    var dir = Directory.CreateTempSubdirectory().FullName;
    File.WriteAllBytes(Path.Combine(dir, "SF_MF_OathScene.pex"), System.Array.Empty<byte>());

    var spec = /* 內含 OneIdleScene + host quest MF_OathQuest 的最小 ModSpec */ MinimalOathSpec();
    var r = TestBuild.OkWithCompiledScripts(spec, dir);   // 見 Step 3 helper
    var sc = r.Mod.EnumerateMajorRecords<ISceneGetter>().Single(x => x.EditorID == "MF_OathScene");

    Assert.NotNull(sc.VirtualMachineAdapter);
    var pf = ((ISceneAdapterGetter)sc.VirtualMachineAdapter!).ScriptFragments!.PhaseFragments;
    Assert.Equal(2, pf.Count);
    Assert.All(pf, f => Assert.Equal("SF_MF_OathScene", f.ScriptName));
    Assert.All(pf, f => Assert.True(f.Flags.HasFlag(ScenePhaseFragment.Flag.OnStart)));
}
```

> `ScenePhaseFragment.Flag` 的確切 enum 名以 Mutagen 為準(Explore:OnStart/OnCompletion);若名稱不同,Step 2 編譯失敗時對照修正。

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter Scene_fragments_attached_when_pex_present`
Expected: 編譯失敗(`AttachSceneFragments`/helper 不存在)。

- [ ] **Step 3: 加測試 helper**

在 `TestBuild`(`tests/ModForge.Core.Tests/` 既有 helper 類)加 `OkWithCompiledScripts(ModSpec, string compiledDir)`,鏡像既有 `Ok` 但把 `options.CompiledScriptsDir = compiledDir` 並呼叫 `AttachSceneFragments`。對照既有 `Ok` 怎麼建 `BuildContext`/`options`(grep `CompiledScriptsDir` 找既有設定點)。

- [ ] **Step 4: 實作 `AttachSceneFragments`**

在 `src/ModForge.Core/Generator.Build.Scripts.cs` 加(鏡像 `AttachDialogueResultScripts` 的 gating 與 property 綁定):

```csharp
// Attach the SceneAdapter VMAD + per-phase fragments for scenes with idle actions. Mirrors
// AttachDialogueResultScripts: only attach when the compiled SF_<scene>.pex is present, so a
// pure build (no compiled scripts) never references an absent .pex (which would Papyrus-error).
public void AttachSceneFragments()
{
    if (options?.CompiledScriptsDir is null) return;
    foreach (var s in spec.Scenes)
    {
        if (!Generator.SceneNeedsFragmentScript(s)) continue;
        var scriptName = Generator.SceneFragmentScriptName(s);
        if (!File.Exists(Path.Combine(options.CompiledScriptsDir, scriptName + ".pex"))) continue;
        if (!recordsByEd.TryGetValue(s.EditorId, out var rec) || rec is not Scene scene)
        { Warn($"  ! scene fragment: scene '{s.EditorId}' not built"); continue; }

        var adapter = new SceneAdapter { ScriptFragments = new SceneScriptFragments { FileName = scriptName } };
        var entry = new ScriptEntry { Name = scriptName, Flags = ScriptEntry.Flag.Local };

        foreach (var a in s.Actions.Where(a => !string.IsNullOrWhiteSpace(a.Idle))
                                   .GroupBy(a => a.StartPhase).Select(g => g.First())
                                   .OrderBy(a => a.StartPhase))
        {
            adapter.ScriptFragments.PhaseFragments.Add(new ScenePhaseFragment
            {
                Index = (uint)a.StartPhase,                 // 基數以 Task 0 spike 為準
                Flags = ScenePhaseFragment.Flag.OnStart,
                ScriptName = scriptName,
                FragmentName = $"Fragment_{a.StartPhase}",  // ↔ GenerateSceneFragmentSource 函式名
            });
            // Idle_<phase> property → the IDLE form.
            var ip = new ScriptObjectProperty { Name = $"Idle_{a.StartPhase}", Flags = ScriptProperty.Flag.Edited };
            if (TryResolveRef(a.Idle, formKeyByEd, out var idleFk)) ip.Object.SetTo(idleFk);
            else Warn($"  ! scene '{s.EditorId}' idle ref '{a.Idle}' unresolved");
            entry.Properties.Add(ip);
            // Actor_<phase> property → the ReferenceAlias (aliasId a.Actor on the host quest).
            entry.Properties.Add(new ScriptIntProperty { Name = $"Actor_{a.StartPhase}_AliasId", Data = a.Actor, Flags = ScriptProperty.Flag.Edited });
        }
        adapter.Scripts.Add(entry);
        scene.VirtualMachineAdapter = adapter;
        scriptsAttached++;
    }
}
```

> Actor 屬性綁定:若 spike 顯示需直接綁 `ReferenceAlias` 物件(而非 aliasId int),改成 `ScriptObjectProperty` 指向 host quest alias——host quest 的 alias 結構見 `Generator.Build.Scene.cs` 怎麼建 scene actor alias。先以可編譯、結構正確為準,實際 property 形狀在 Step 6 in-game 驗證收斂。

- [ ] **Step 5: 在 package 接上編譯迴圈**

在 `src/ModForge.Cli/Package.cs` 既有 quest/dialogue fragment 迴圈(行 47-58)後加:

```csharp
foreach (var s in spec.Scenes)
{
    var src = Generator.GenerateSceneFragmentSource(s);
    if (!string.IsNullOrEmpty(src))
        CompileGenerated(src, Generator.SceneFragmentScriptName(s), $"scene fragment for '{s.EditorId}'");
}
```

並確認 build pipeline 在 package 路徑有呼叫 `ctx.AttachSceneFragments()`(對照 `AttachDialogueResultScripts` 在哪被呼叫,加在同處)。

- [ ] **Step 6: 跑測試確認通過 + 全測試綠**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`
Expected: 新測試 PASS;其餘綠(除已知 WordWall)。

- [ ] **Step 7: Commit**

```bash
git add src/ModForge.Cli/Package.cs src/ModForge.Core/Generator.Build.Scripts.cs tests/ModForge.Core.Tests/
git commit -m "feat(scene): compile SF_ + attach SceneAdapter phase fragments at package time" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: 擴 CLI `find` 支援 IdleAnimation(查 idle FormID)

**Files:**
- Modify: `src/ModForge.Cli/Diagnostics.cs:15-85`(Find)
- Test:(若 find 有單元測試則加;否則手動驗證 + CODE_MAP 記錄)

- [ ] **Step 1: 確認 find 的型別解析機制**

讀 `src/ModForge.Cli/Diagnostics.cs` 的 `Find()`:型別經 `Mutagen.Bethesda.Skyrim.I{typeName}Getter` 反射取得(Explore 報告)。`IdleAnimation` 應已是合法 Mutagen 型別 → 多半只需把 `IdleAnimation`/`Idle` 加進「允許型別」白名單或別名表。

- [ ] **Step 2: 加 Idle 別名 + 白名單**

若 `Find()` 有型別別名/白名單(如 `npc`→`Npc`),加 `idle`→`IdleAnimation`(與 `weapon`→`Weapon` 同模式)。若是純反射無白名單,確認 `find IdleAnimation "kneel"` 已能運作,僅補一個 `idle` 短別名。

- [ ] **Step 3: 手動驗證**

Run: `dotnet run --project src/ModForge.Cli -- find IdleAnimation Pray`(需本機 Skyrim.esm;無則記為 in-game/有遊戲機器上驗證)
Expected: 列出含 "Pray" 的 IDLE 記錄 EditorID + FormID,供 showcase 用。

- [ ] **Step 4: Commit**

```bash
git add src/ModForge.Cli/Diagnostics.cs
git commit -m "feat(cli): find supports IdleAnimation (idle alias) for PlayIdle discovery" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: showcase example + 文檔同步

**Files:**
- Create: `examples/scene-playidle.json`
- Modify: `docs/SPEC-dialogue-quests.md`(scene actions 段加 `idle`)
- Modify: `docs/CODE_MAP.dialogue-quests.md`(新增 `Generator.SceneFragments.cs` + Tests 欄)
- Modify: `docs/superpowers/specs/2026-06-06-playidle-scene-action-design.md`(補 Task 0 spike 結論)

- [ ] **Step 1: 寫 showcase spec**

`examples/scene-playidle.json`:一個 host quest + 一個單 actor scene,phase 0 跪下 idle、phase 1 Timer 停頓 + 一句台詞、phase 2 起身 idle。idle ref 用 Task 5 `find` 探得的真實 vanilla 祈禱/下跪 IDLE FormID(依 [[vanilla-nif-paths-must-be-verified]] 精神,FormID 要核實)。

- [ ] **Step 2: validate + build 結構驗證**

Run: `dotnet run --project src/ModForge.Cli -- validate examples/scene-playidle.json`
Run: `dotnet run --project src/ModForge.Cli -- build examples/scene-playidle.json /tmp/playidle.esp`
Expected: validate 無 error;build 出 esp;`scnscan`/dump(若有)顯示 scene 結構。純 build 不應掛 VMAD(無 compiled scripts)。

- [ ] **Step 3: 更新 SPEC-dialogue-quests.md**

在 scene `actions` 段加 `idle` 欄位說明 + 一段「PlayIdle 用 phases+Timer 編排演出」的 jsonc 範例(對齊 `examples/scene-playidle.json`)。

- [ ] **Step 4: 更新 CODE_MAP.dialogue-quests.md**

在 scene 區加 `Generator.SceneFragments.cs`(職責:scene phase idle fragment 純產生器)與其 Tests `SceneFragmentTests.cs`;`AttachSceneFragments` 記在 `Generator.Build.Scripts.cs` 條目。

- [ ] **Step 5: 補設計文件 spike 結論**

把 Task 0 的 (Index 基數 / FragmentName 規則 / 取 actor 寫法) 寫進設計文件「風險 / 待精修」段,標為已確認。

- [ ] **Step 6: Commit**

```bash
git add examples/scene-playidle.json docs/SPEC-dialogue-quests.md docs/CODE_MAP.dialogue-quests.md docs/superpowers/specs/2026-06-06-playidle-scene-action-design.md
git commit -m "docs(scene): PlayIdle showcase example + SPEC/CODE_MAP sync" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: package showcase + in-game 驗證交接

**Files:**
- 無 production code(打包 + 交使用者實機)

- [ ] **Step 1: package 成 MO2-flat zip**

Run: `dotnet run --project src/ModForge.Cli -- package examples/scene-playidle.json`(輸出到 `~/skyrim_mods/ModForgePlayIdle.zip`,plugin 在 zip root,見 [[packaging-zip-stale-file-trap]])。
確認 zip 內含:.esp、`Scripts/SF_<scene>.pex`、`Scripts/Source/SF_<scene>.psc`。

- [ ] **Step 2: 結構自檢(我無法跑遊戲)**

確認 SCEN 的 SceneAdapter VMAD 已掛(.pex 在時)、PhaseFragments 數正確、properties(Idle_/Actor_)已綁。見 [[ingame-test-workflow]]:先結構驗證再交人。

- [ ] **Step 3: 交使用者實機驗證(交接清單)**

使用者在 MO2/Proton 裝 zip,進遊戲開 scene(可用 console `startscene` 或經 host quest 觸發),確認:actor 在 phase 0 下跪/祈禱、停頓、phase 2 起身、結束回正常 AI。若 idle 不播,回報 → 對照 Task 0 spike 的 FragmentName/Index 慣例與取-actor 寫法修正(最可能的失敗點)。

- [ ] **Step 4: 確認後更新筆記**

實機 PASS 後,在 `CLAUDE.md` 已落地功能補一行「scene PlayIdle(phase fragment)」,並更新 IDEAS §1b「播放動畫」為已支援。記憶可加一則 in-game-confirmed。

---

## Self-Review 註記

- **Spec coverage:** spec 各段對應:`SceneActionSpec.Idle`→T1;phase-fragment 機制→T2(產生器)+T4(掛載);build 不產 SceneAction→T3;package 編譯+VMAD gating→T4;find/idle 探查→T5;showcase+演出 composition+docs→T6/T7。
- **最高風險集中在 T0/T4**:scene phase fragment 是 ModForge 第三種、最少走的 fragment 路徑;`Index` 基數、`FragmentName`↔函式名、取-actor 的 Papyrus 寫法三點由 T0 spike 釘死,T4 code 標註「以 spike 為準」之處需一致更新。實機(T7)是最終裁判。
- **型別一致性:** `SceneNeedsFragmentScript`/`SceneFragmentScriptName`/`GenerateSceneFragmentSource`(T2)、`AttachSceneFragments`(T4)、`Fragment_<phase>` 函式名(T2)↔`FragmentName`(T4)、`Idle_<phase>`/`Actor_<phase>` 屬性(T2 宣告↔T4 綁定)三處名稱必須對齊——改一處要同步另一處。
