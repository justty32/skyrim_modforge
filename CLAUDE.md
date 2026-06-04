# ModForge — Claude Code 專案備忘

## 開發環境

- 測試：`dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`（net10.0）
- 已知環境性失敗：`WordWallTests.Trigger_placed_referencing_word_wall_activator_base` 需要本機 `Skyrim.esm`，沒裝遊戲的機器上固定 259/260 通過——這不是 regression
- Commit 訊息用多個 `-m` flag 組多行（PowerShell here-string 易出問題）
- 重構必須行為不變（behavior-preserving）；不要未經確認就 push 或開新工作

## 程式碼慣例

- `partial class` 按領域拆檔：CLI 是 `Program.cs` + `Diagnostics.*.cs` + `Package.cs`；Core 是 `Generator.Build.*.cs`
- 所有 src 檔案維持在 300 行以下

## 進行中的方向

想法備忘錄在 `docs/IDEAS.md`（隨從擴充、劇情演出、大量劇情生成等）。

**Story Manager 階段一探針：✅ 實機 PASS（2026-06-04）**——
用原版 Kill Actor 事件節點（零 Papyrus）+ From Event Data 填充驗證成功。`StoryManagerProbe.BuildProbe`（Core）+ CLI `smtree`/`smprobe`。殺完整 actor → SM 啟動模板任務 + Victim alias 填上被殺者。引擎 quirk：`SimpleActor`（雞/兔）不發 Kill 事件。真值與背景見 `docs/IDEAS.md` 第 9 節 + `docs/superpowers/plans/2026-06-04-story-manager-probe.md`。

**Story Manager 階段二 spec 管線：✅ 已實作（2026-06-04），待實機**——
意圖導向：`QuestSpec` 加 `storyEvent`(event+conditions) + `aliases`(fill="fromEvent:<slot>"|"forced:<ref>")，build 自動生 SMBN→SMQN 掛原版根下並清 StartGameEnabled。事件表 `StoryManagerEvents`（只 KillActor）、`Generator.Build.StoryManager.cs`（pass 2）、`Generator.Validate.StoryManager.cs`。探針 builder/smprobe 已退役，`smtree` 保留。278 測試綠。
待實機測試包在 `~/skyrim_mods/`：`MFSM_Multi.zip`（非 ESL，4 quest 測 basic/victim+killer/forced/condition）、`MFSM_Esl.zip`（測 ESL 能否裝 SM 記錄）。測法見 `~/skyrim_mods/MFSM_TEST_README.txt`。結果回來補進 `docs/IDEAS.md` 第 9 節。

**之後可做**：加更多事件（離線解碼 + 一行進 `StoryManagerEvents` 表）；Script Event 入口（自訂 Keyword + Papyrus `SendStoryEvent`，量產最終入口，需 Papyrus）；findMatching/location 等填充型別。
