# ModForge — Claude Code 專案備忘

## 開發環境

- 測試：`dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`（net10.0）
- 已知環境性失敗：`WordWallTests.Trigger_placed_referencing_word_wall_activator_base` 需要本機 `Skyrim.esm`，沒裝遊戲的機器上固定 259/260 通過——這不是 regression
- Commit 訊息用多個 `-m` flag 組多行（PowerShell here-string 易出問題）
- 重構必須行為不變（behavior-preserving）；不要未經確認就 push 或開新工作

**前置步驟（fresh clone 後，`dotnet build` 前必做一次）：**
`assets/papyrus/MFStoryEventDispatch.pex` 被 `ModForge.Cli.csproj` embed 為 EmbeddedResource，但 `.pex` 在 `.gitignore` 裡不進 repo。需先編譯：
```
dotnet run --project src/ModForge.Cli -- compile assets/papyrus/MFStoryEventDispatch.psc assets/papyrus/
```
（需要 Wine + CK PapyrusCompiler 環境。）`MFStoryEventDispatch.psc` 有任何改動時，同樣需要重跑此步驟並將新的 `.pex` 保留在本機（不 commit）。

## 程式碼慣例

- `partial class` 按領域拆檔：CLI 是 `Program.cs` + `Diagnostics.*.cs` + `Package.cs`；Core 是 `Generator.Build.*.cs`
- 所有 src 檔案維持在 300 行以下
- **Spec 欄位 breaking change**：新增欄位安全（optional，舊 example 不受影響）；**刪除或改名欄位**前必須先 `grep -r "舊欄位名" examples/`，找出所有受影響的 JSON 並在同一個 commit 裡一起更新。

## CODE_MAP 工作流程

程式碼導航 index 在 `docs/CODE_MAP.md`（頂層）→ 五份子 index：

| 子 index | 涵蓋 |
|---------|------|
| `docs/CODE_MAP.dialogue-quests.md` | quest / dialogue / scene / Story Manager / ScriptEvent / word wall |
| `docs/CODE_MAP.world.md` | cell / placement / worldspace / region / leveled list / container / encounter zone |
| `docs/CODE_MAP.items-magic.md` | weapon / armor / spell / magic effect / enchantment / perk / shout / long-tail |
| `docs/CODE_MAP.npcs-packages.md` | NPC / faction / class / AI package / combat style / weather / climate |
| `docs/CODE_MAP.infra.md` | CLI / build orchestrator / validate / package / Papyrus / translate / plugin I/O |

三個面向構成維護鏈：**程式碼（含 examples/ 與 assets/）→ CODE_MAP → 文檔**（HTML bundle 最低，只在明確要求時更新）。

`examples/*.json`、`examples/scripts/*.psc`、`examples/assets/`、`assets/papyrus/MFStoryEventDispatch.psc`、`spec.schema.json` 均視為**源碼**——功能變動時必須同步，不是次要的附屬物。

**優先級（衝突或時間不夠時，依序保持一致）：**
程式碼（含 examples + assets）> CODE_MAP > 文檔（`docs/SPEC-*.md` / `for_agent*.md`）> HTML

**CODE_MAP 與程式碼衝突時：以程式碼為準，立即修正 CODE_MAP。**

**日常規則：**
1. **修改前**：先讀 `docs/CODE_MAP.md`，找到相關子 index，只讀清單中列出的檔案——不要讀無關領域的檔案。
2. **修改後**：若新增或刪除了 `.cs` 檔案，或某檔案的職責有顯著改變，必須同步更新對應子 index（含 Tests 欄）。
3. `.cs` 檔案本身不加「對應 CODE_MAP」的註釋（維護成本過高）；反向查找直接 `grep` CODE_MAP 文件。

## 主要工作流程

### Workflow 1：新增功能

```
修改程式碼（增量）
  → 使用者測試 → 回報問題 → 修程式碼 → 重複
  → 全數通過後：補齊 CODE_MAP → 補文檔 → commit
```

- 測試迭代期間，CODE_MAP / 文檔可以暫時落後。
- 若迭代跨越多個 session，在本檔「進行中的方向」補一行 `[功能名] 文檔/CODE_MAP 待同步`，下一個 session 接手時不會誤以為已同步。
- **commit 前**：CODE_MAP + 文檔必須對齊（HTML 不要求，examples/assets 視情況）。

### Workflow 2：重構整理（拆分 / 模塊化）

維護鏈中**一次只動一個面向**，做完 commit 再看下一個：

```
Step 1  程式碼重構（behavior-preserving 拆分）
          → 立即更新 CODE_MAP 與相關文檔以對齊新結構
          → 跑測試確認行為不變 → commit

Step 2  （視需要）CODE_MAP 若臃腫 → 單獨重構 CODE_MAP
          → 同步更新 CODE_MAP 中連結到的文檔段落 → commit

Step 3  （視需要）文檔若臃腫 → 單獨重構文檔
          → 同步更新 CODE_MAP 中指向這些文檔的連結 → commit

Step 4  （視需要）examples/assets 若需更新 → 單獨處理 → commit
```

- **禁止**：同一 session 內同時重構超過一個面向。
- 每個 Step 完成前不啟動下一個，確保任意時間點維護鏈是一致的。

## 進行中的方向

想法備忘錄在 `docs/IDEAS.md`（隨從擴充、劇情演出、大量劇情生成等）。

**Story Manager 階段一探針：✅ 實機 PASS（2026-06-04）**——
用原版 Kill Actor 事件節點（零 Papyrus）+ From Event Data 填充驗證成功。`StoryManagerProbe.BuildProbe`（Core）+ CLI `smtree`/`smprobe`。殺完整 actor → SM 啟動模板任務 + Victim alias 填上被殺者。引擎 quirk：`SimpleActor`（雞/兔）不發 Kill 事件。真值與背景見 `docs/IDEAS.md` 第 9 節 + `docs/superpowers/plans/2026-06-04-story-manager-probe.md`。

**Story Manager 階段二 spec 管線：✅ 實機驗證通過（2026-06-04）**——
意圖導向：`QuestSpec` 加 `storyEvent`(event+conditions) + `aliases`(fill="fromEvent:<slot>"|"forced:<ref>")，build 自動生 SMBN→SMQN 掛原版根下並清 StartGameEnabled。事件表 `StoryManagerEvents`（只 KillActor）、`Generator.Build.StoryManager.cs`（pass 2）、`Generator.Validate.StoryManager.cs`。探針 builder/smprobe 已退役，`smtree` 保留。279 測試綠。
**SM 結構鐵律（[[story-manager-kill-recipe]]）**：一事件根→一條共用分支→多個 quest node（串 PreviousSibling）；事件根下多分支互斥（引擎只跑一條）；**引擎一事件只啟動一個最先符合的 quest（正確 radiant 行為，非 bug）**；ESL 能裝 SM。實機全綠：victim/killer(R2=玩家)/forced/condition/ESL 五變體。

**階段二+ 擴充：✅ 實機驗證通過（2026-06-04）**——291 測試綠：
- **事件表 +4**（純資料，build/validate 不變）：`ChangeLocation`(0x01320E/CLOC)、`CastMagic`(0x046829/CAST)、`AddItem`(0x02C439/AIPL)、`Assault`(0x02C494/ASSU)。槽 R1/R2/L1/L2。
- **新填充 `uniqueActor:<ref>`** → `QuestAlias.UniqueActor`（語法同 forced）。`createObject`/`findMatching` 緩做（需解碼 vanilla 範例再落地）。
- **實機修掉的 alias 鐵律**（[[story-manager-kill-recipe]]）：① location 槽填充 alias 必須 `Type=Location`（否則填 null；fromEvent 'L' 開頭自動設）；② 任一必填 alias 填不上 → quest 靜默不啟動；③ 殺/指向被 `ReservesLocationOrReference` 保留的 NPC 需 alias `AllowReserved`——已加 opt-in `allowReserved`（uniqueActor 強制開）；④ `QuestAlias.Flags` nullable、`|=` 對 null no-op，旗標要 `GetValueOrDefault()` 起底。
- **Script Event 入口 — 研究尖兵完成**（`docs/superpowers/specs/2026-06-04-script-event-entry-spike.md`）：根 `0x01379A`/碼 `SCPT`，keyword 綁在**分支條件** `GetEventData Keyword GetIsID <KYWD>==1`（Mutagen `GetEventDataConditionData` 原生，ESP 側可建）；**Papyrus-on-Linux 可行已實證**（`mono PapyrusCompiler.exe` + CK `Scripts.zip` 編出 .pex，免 Caprica/wine）；單一通用 dispatcher 服務所有 mod。**下一步可實作**。

**Script Event 入口（量產通用自訂入口）：✅ 實機驗證通過（2026-06-04）**——297 測試綠（MFSE_Target running + Target alias=玩家）：
- 事件表加 `ScriptEvent`（根 `0x01379A`/碼 `SCPT`/槽 ref1=R1、ref2=R2、loc=L1）。
- `QuestStoryEventSpec.Keyword`：ScriptEvent quest 宣告一個 KYWD（spec.keywords 裏建），build 在分支上加 keyword 過濾條件 `GetEventData/GetIsID Member=Keyword Record=<KYWD> ==1`（Mutagen `GetEventDataConditionData` 原生）。同 keyword 共用分支、不同 keyword 不同分支。validate 要求 keyword 已宣告。
- 通用派發器 `assets/papyrus/MFStoryEventDispatch.{psc,pex}`（Global `Fire(kw,ref1,ref2,loc)`→`kw.SendStoryEvent`），編一次、embed 進 CLI；package 遇到 ScriptEvent quest 自動丟 `.pex` 進 `Scripts/`。**Papyrus 編譯**：`Papyrus.Compile`（Wine+CK）用 cache 全 source set（`~/.cache/modforge/papyrus/Source/Scripts`，14301 .psc）；native `~/tools/papyrus-compiler` 用 loose Source（headers 不全時設 `MODFORGE_PAPYRUS_HEADERS` 指向 cache）。
- 範例 `examples/story-manager-scriptevent.json` + 觸發腳本 `examples/MFSE_TestTrigger.psc`（OnInit 發事件）= 端到端。計畫 `docs/superpowers/plans/2026-06-04-script-event-entry.md`。

**之後可做**：`createObject`/`findMatching` 填充；再多解事件進表；把派發器接到實際觸發場景（dialogue fragment / magic effect / alias script）做量產接線。
