# 以函式庫方式使用 ModForge（ModForge.Core）

當靜態 JSON 規格不夠用時 — 您想**以程式碼組合規格**（迴圈、條件判斷、從他處取得的資料）、將產生功能嵌入更大的程式，或讓 AI 代理撰寫直接呼叫 ModForge 的 C# 程式碼。`ModForge.Core` 是可重用的引擎；它操作物件，永遠不接觸主控台、argv 或寫死的檔案路徑。

← 索引：[for_agent.md](for_agent.md) · CLI 路徑：[for_agent_cli.md](for_agent_cli.md) · 規格欄位：[SPEC-index.md](spec/SPEC-index.md)

## CLI + JSON vs. 函式庫 — 該用哪一個

| | CLI + JSON（[for_agent_cli.md](for_agent_cli.md)） | 函式庫（`ModForge.Core`） |
|---|---|---|
| 規格是… | 您撰寫的靜態 `.json` 檔案 | 您在程式碼中建構的 `ModSpec` 物件 |
| 最適合 | 「描述模組 → 產生它」（可審閱、可差異比較、可重複執行） | 動態組合、嵌入、以程式碼回應建置警告 |
| 代理負責 | 撰寫 JSON，執行 `validate`/`build` | 撰寫參考 Core 的 C# 程式碼 |
| 成本 | 無（無需編譯步驟） | 您需要編譯並執行一個 C# 專案 |

預設使用 CLI + JSON。只在規格必須**計算**而非手寫時才使用函式庫。

## 加入參考

```bash
dotnet add <your.csproj> reference path/to/src/ModForge.Core/ModForge.Core.csproj
```

`Mutagen.Bethesda.Skyrim` 會以傳遞方式流入，所以 `ISkyrimMod`、`ModKey` 等均可使用。所有公開成員位於 `namespace ModForge`。

## API 介面

| 成員 | 形式 |
|---|---|
| `Generator.Validate(ModSpec)` | → `IReadOnlyList<string>` 問題列表（空列表 = 有效）。**請先執行此方法** — `Build` 不會自動驗證。 |
| `Generator.Build(ModSpec, ModKey, BuildOptions?)` | → `BuildResult { ISkyrimMod Mod; IReadOnlyList<string> Warnings; BuildStats Stats }`。在記憶體中建置；**由您自行寫入**。 |
| `Translator.Extract(ISkyrimMod)` | → `List<StringEntry>`（每個可翻譯字串；`Source` 已設定，`Target` 為空）。 |
| `Translator.Apply(ISkyrimMod, IEnumerable<StringEntry>)` | → `int` 已套用數量；直接修改模組。 |
| `Translator.ApplyLocalized(ISkyrimMod, entries, outDir)` | → `(int Applied, int Renamed, string EspPath)`；輸出一組本地化 UTF-8 `_chinese.STRINGS`。 |
| `Demo.CreateDemoPlugin(ModKey)` | → `ISkyrimMod`（工具鏈完整性檢查）。 |
| `Papyrus.Compile(scriptPath, outDir, PapyrusOptions?)` | → `CompileResult { bool Success; int ExitCode; string? PexPath; string Message }`（編譯錯誤時永不拋出例外）。 |
| `Generator.GenerateQuestFragmentSource(QuestSpec)` / `GenerateDialogueFragmentSource(DialogueSpec)` | → `string` Papyrus 原始碼，用於 stage→objective 配線 / dialogue-set-stage（不需要時為空字串）。純函式；`package` 指令會將這些寫到 `Scripts/Source/` 下供 CK 編譯。`QuestFragmentScriptName` / `DialogueFragmentScriptName` 提供所附加腳本的名稱。 |
| `PluginIo.Load(path)` / `PluginIo.Write(mod, path)` | 載入一個外掛 / 寫入一個外掛（`Write` 使用 `ModKeyOption.NoCheck`）。 |
| `ModSpec` + 所有 `*Spec` + `StringEntry` | 公開資料模型（可修改；對 `List<>` 集合初始化器友善）。 |

`BuildOptions.SkyrimDataPath` 可覆寫主外掛（Skyrim.esm，用於 template 複製 / 原版 cell 覆寫）的讀取位置；為 null 時會依序回退至 `MODFORGE_SKYRIM_DATA`，再到預設 Steam 路徑。`spec.Esl`（預設 true）驅動 `IsSmallMaster`（light master：≤2048 筆新記錄，FormID 0x800–0xFFF；超出時 `PluginIo.Write` 會拋出明確錯誤）。

## 工作範例 — 產生、驗證、寫入

```csharp
using ModForge;
using Mutagen.Bethesda.Plugins;

var spec = new ModSpec
{
    PluginName = "MyMod.esp",
    Weapons =
    {
        new WeaponSpec
        {
            EditorId = "MF_Blade", Name = "Forged Blade",
            Template = "Skyrim.esm:0x012EB7",   // clone IronSword's model/anim
            Damage = 12, Value = 100, Weight = 9,
        },
    },
};

var problems = Generator.Validate(spec);
if (problems.Count > 0)
{
    foreach (var p in problems) Console.Error.WriteLine(p);
    return;   // fix the spec; Build assumes a valid spec
}

var result = Generator.Build(spec, ModKey.FromNameAndExtension("MyMod.esp"));
foreach (var w in result.Warnings) Console.WriteLine(w);   // non-fatal authoring notes
Console.WriteLine($"{result.Stats.TopLevelRecords} record(s), {result.Stats.LinksWired} link(s)");
PluginIo.Write(result.Mod, "MyMod.esp");                   // or keep editing result.Mod in memory
```

## 動態組合 — 使用函式庫的理由

```csharp
// Build N leveled-list entries from data the agent gathered at runtime.
var list = new LeveledItemSpec { EditorId = "MF_Loot" };
foreach (var (itemRef, lvl) in lootTable)              // lootTable computed elsewhere
    list.Entries.Add(new LeveledEntrySpec { Reference = itemRef, Level = (short)lvl, Count = 1 });
var spec = new ModSpec { LeveledItems = { list } };
```

在 JSON 中做同樣的迴圈會意味著手動建立模板檔案；在程式碼中只是一個 `foreach`。

## 警告是資料，不是輸出

`BuildResult.Warnings` 收集每個非致命的製作問題（未解析的 ref、缺少模型、不支援的套件模板等）— 也就是 CLI 會以 `  ! …` 形式印出的內容。以程式碼檢查或斷言這些問題 — 有警告的建置仍會產生一個模組；由您逐一決定每個警告是否需要中止。

## 相同的誠實原則

建置一個結構有效的模組與遊戲內可運作是**不同**的 — 詳見 [for_agent.md 的限制章節](for_agent.md#limits--be-honest-do-not-over-claim)。函式庫給您位元組；只有實際的 Proton/Skyrim 啟動能確認行為。
