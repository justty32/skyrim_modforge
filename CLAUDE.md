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

**Story Manager 階段二 spec 管線：✅ 實機驗證通過（2026-06-04）**——
意圖導向：`QuestSpec` 加 `storyEvent`(event+conditions) + `aliases`(fill="fromEvent:<slot>"|"forced:<ref>")，build 自動生 SMBN→SMQN 掛原版根下並清 StartGameEnabled。事件表 `StoryManagerEvents`（只 KillActor）、`Generator.Build.StoryManager.cs`（pass 2）、`Generator.Validate.StoryManager.cs`。探針 builder/smprobe 已退役，`smtree` 保留。279 測試綠。
**SM 結構鐵律（[[story-manager-kill-recipe]]）**：一事件根→一條共用分支→多個 quest node（串 PreviousSibling）；事件根下多分支互斥（引擎只跑一條）；**引擎一事件只啟動一個最先符合的 quest（正確 radiant 行為，非 bug）**；ESL 能裝 SM。實機全綠：victim/killer(R2=玩家)/forced/condition/ESL 五變體。

**階段二+ 擴充：✅ 已建（2026-06-04，待實機）**——289 測試綠：
- **事件表 +4**（純資料，build/validate 不變）：`ChangeLocation`(0x01320E/CLOC)、`CastMagic`(0x046829/CAST)、`AddItem`(0x02C439/AIPL)、`Assault`(0x02C494/ASSU)。槽 R1/R2/L1/L2。
- **新填充 `uniqueActor:<ref>`** → `QuestAlias.UniqueActor`（語法同 forced）。`createObject`/`findMatching` 緩做（需解碼 vanilla 範例再落地）。
- **Script Event 入口 — 研究尖兵完成**（`docs/superpowers/specs/2026-06-04-script-event-entry-spike.md`）：根 `0x01379A`/碼 `SCPT`，keyword 綁在**分支條件** `GetEventData Keyword GetIsID <KYWD>==1`（Mutagen `GetEventDataConditionData` 原生，ESP 側可建）；**Papyrus-on-Linux 可行已實證**（`mono PapyrusCompiler.exe` + CK `Scripts.zip` 編出 .pex，免 Caprica/wine）；單一通用 dispatcher 服務所有 mod。**下一步可實作**。

**之後可做**：實作 Script Event 入口（量產最終入口）；`createObject`/`findMatching` 填充；再多解事件進表。
