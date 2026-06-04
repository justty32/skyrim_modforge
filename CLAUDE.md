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

**下一步（2026-06-04 決定）**：Story Manager 最小驗證實驗——
手寫 spec → Script Event Keyword + 帶 Find Matching Reference Alias 的模板任務 → 遊戲內 `SendStoryEvent` → 驗證 SM 選角。目的是暴露 ModForge 缺的欄位（SMEN/SMBN/SMQN、Quest Event 欄位、條件式 Alias 填充）。完整背景見 `docs/IDEAS.md` 第 9 節的 Story Manager 段落。
