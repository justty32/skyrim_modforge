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
dotnet run --project src/ModForge.Cli -- compile assets/papyrus/MFSceneBanterController.psc assets/papyrus/
```
（需要 Wine + CK PapyrusCompiler 環境。）這兩個 `.psc`（dispatcher 與在場偵測 Scene controller）有任何改動時，同樣需要重跑對應步驟並將新的 `.pex` 保留在本機（不 commit）。兩個 `.pex` 都被 `ModForge.Cli.csproj` embed 為 EmbeddedResource（條件式：缺檔仍可 build，runtime 才 warn）。

## 程式碼慣例

- `partial class` 按領域拆檔：CLI 是 `Program.cs` + `Diagnostics.*.cs` + `Package.cs`；Core 是 `Generator.Build.*.cs`
- 所有 src 檔案維持在 300 行以下
- **Spec 欄位 breaking change**：新增欄位安全（optional，舊 example 不受影響）；**刪除或改名欄位**前必須先 `grep -r "舊欄位名" examples/`，找出所有受影響的 JSON 並在同一個 commit 裡一起更新。
- **新增 Spec 欄位後**：手動更新 `examples/spec.schema.json`（IDE autocomplete 用；無自動同步機制，允許偶爾落後，但 commit 前盡量補上）。

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

**進行中：scene PlayIdle（phase fragment）—— 程式碼/測試/CODE_MAP/文檔皆已同步並 commit，僅待實機驗證。**
- 已落地（commit 6586ef2…280c4d0）：`SceneActionSpec.Idle` → SCEN `SceneAdapter` per-phase begin fragment（`SF_<scene>.Fragment_<phase>` 跑 `<alias>.GetActorRef().PlayIdle()`）；純 build 不掛 VMAD、package 編 `SF_` 才掛（gating 同 `WireQuestStages`）。純產生器 `Generator.SceneFragments.cs`、掛載 `AttachSceneFragments`（`Generator.Build.Scripts.cs`）。
- showcase `examples/scene-playidle.json`（供奉者 跪→禱→起,用 vanilla IdleBlessingKneelEnter `0x0F11EE` / Exit `0x0F11EF`）已打包 `~/skyrim_mods/ModForgePlayIdle.zip`(flat)。ESP 已結構驗證:SceneAdapter + 2 PhaseFragment(Index 0/2、OnStart)+ Idle_/Actor_ 屬性正確綁定。
- **待實機**:MO2 裝 zip→存檔 save/reload→console `startscene MF_OathScene`(對著供奉者)→確認 跪下→停頓念禱詞→起身→回正常 AI。最可能失敗點:Enter idle 是否 hold 住跪姿到 phase 2(若彈回,phase 1 可改播 held pose `IdleBlessingKneel 0x0F11EC`)。PASS 後:移除本段、CLAUDE.md 已落地功能補一行、IDEAS §1b「播放動畫」標為已支援、記憶加 in-game-confirmed。

### 已落地功能（時間序；實作細節見 git log / CODE_MAP / SPEC）

**對話 / 任務 / Story Manager**
- **SM spec 管線**：`QuestSpec.storyEvent`(event+conditions) + `aliases`；build 自動生 SMBN→SMQN 掛原版根、清 StartGameEnabled。事件表 `StoryManagerEvents`（十個 engine-native 事件）。
- **alias fill 五種**：`fromEvent:<slot>` / `forced:<ref>` / `uniqueActor:<ref>` / `createObject:<ref>@<alias>` / `findMatching:closest|any`。
- **可複用 trigger 庫（五入口，同一 `Fire()`）**：magic-effect / potion / activator / dialogue / alias-OnActivate。zip `~/skyrim_mods/ModForge{Magic,Potion,Activator,Dialogue,Alias}Trigger.zip`。通用派發器 `assets/papyrus/MFStoryEventDispatch`（embed 進 CLI）。
- **Quest 階段**：`StageSpec.startUpStage`（啟動自顯 objective）+ stage 推進（`MFSE_AdvanceStage.psc`）；alias 也適用一般（非 storyEvent）quest。

**Scene 劇情演出**
- **在場偵測 autoStart**：`SceneSpec.AutoStart`（triggerDistance/LOS/cooldown/poll/**brawlOnEnd**）+ 可複用 `MFSceneBanterController`。`ModForgeSceneBanter.zip`。
- **非對話 action**：beat phase + Package（走位，引用 `packages[]` 的 PACK）+ Timer（停頓）。`ModForgeSceneAction.zip`。
- **重播策略**：`playOnce` / `playHour(+tolerance)` / `gateGlobal`。`ModForgeReplayPolicy.zip`。
- **per-phase headtrack/facing**：`ScenePhaseSpec.HeadtrackActor/HeadtrackPlayer/FaceTarget`。
- **scene 條件**：`SceneSpec.Conditions`（scene-level，僅 `beginOnQuestStart`）+ `ScenePhaseSpec.StartConditions/CompletionConditions`（per-phase）。

**PACK templates（共 10）**：sandbox / sleep / travel / usemagic / patrol / follow / escort / **sittarget**（坐家具）/ **activate**（活化 lever/door）/ **eat**。

**新 record builders**
- **GlobalVariable (GLOB)**：`GlobalSpec`（short/long/float + constant）。
- **Light (LIGT)**：`LightSpec`（color/radius/fade/flags…），用 placements 放置。
- **Projectile (PROJ) + Explosion (EXPL)**：自訂法術飛行彈+爆，鏈 EXPL←PROJ←MGEF←SPEL。
- **NPC inventory**：`NpcSpec.Items`（攜帶/自動裝備/死亡掉落）；`NpcSpec.essential/protected`。

**一次測 showcase**：`ModForgeShowcase.zip`（批次#1：Light + headtrack + SitTarget）、`ModForgeShowcase2.zip`（批次#2：firebolt PROJ/EXPL + NPC 武器 + scene 閘）。新 diag：`smtree` / `scnscan` / `packagediag` / `lightdiag` 等。

### 鐵律與踩坑（複用知識，勿重蹈）

- **SM 結構** [[story-manager-kill-recipe]]：一事件根→一條共用分支→多 quest node（串 PreviousSibling）；事件根下多分支互斥；**引擎一事件只啟動一個最先符合的 quest**（正確 radiant，非 bug）；ESL 能裝 SM；`SimpleActor`（雞/兔）不發 Kill 事件。
- **SM alias** [[story-manager-kill-recipe]]：① location 槽 alias 必須 `Type=Location`（fromEvent 'L' 自動）；② 任一必填 alias 填不上 → quest 靜默不啟動；③ 殺/指向被 `ReservesLocationOrReference` 保留的 NPC 需 `allowReserved`（uniqueActor 強制）；④ `QuestAlias.Flags` nullable，旗標用 `GetValueOrDefault()` 起底。
- **SM 事件可靠性** [[dispatcher-magic-trigger]]：additive 無條件分支只在 vanilla 少/沒密集處理的事件上可靠；密集事件（ActorDialogue/Hello）會輸掉互斥競爭、劫持原版對話——須用 conditions（或走自訂 ScriptEvent keyword）。
- **autoStart scene 閘門**：用 `autoStart.gateGlobal`（controller 端檢查），**不要**用 scene-level `conditions`——controller 強制 `Scene.Start()`，繞過 scene begin-conditions（後者只 gate `beginOnQuestStart` scene）。
- **scene 動作**：`SceneAction.TypeEnum` 只有 Dialog/Package/Timer——NPC「做動作」一律走 Package action 引用 PACK。
- **NPC 裝備/偷竊**：武器要有傷害（templated 武器 spec 留空會保留 template 原值；0 傷害武器 NPC 評分低於拳頭、不拔）；未裝備物品免 perk 偷，已裝備武器/穿戴衣物需 Misdirection/Perfect Touch perk；`essential` NPC 不可 loot，要可 loot 改用 `protected`。
- **Papyrus 編譯**：`Papyrus.Compile`（Wine+CK）用 cache 全 source（`~/.cache/modforge/papyrus/Source/Scripts`）；native `~/tools/papyrus-compiler` 用 loose Source，headers 不全設 `MODFORGE_PAPYRUS_HEADERS` 指向 cache（`extends ReferenceAlias` 必設）。dispatcher/controller `.psc` embed 進 CLI、Package 編 user script 時解到 temp 當 sibling header → `Fire()` 免 per-machine cache。
- **adapter 合併**：`WireQuestStages` 要**合併**進既有 `QuestAdapter`（不能 `=` 覆寫，否則清掉 alias 腳本的 `.Aliases`）；`GetOwningQuest()` 在執行時 alias OnActivate 可用，dialogue TIF 在 game-load 是 None。
- **vanilla nif 路徑必驗證** [[vanilla-nif-paths-must-be-verified]]：假路徑 → 隱形物件（無報錯）。
- **存檔已固化**：GLOB value / scene `.seq` 只是初值，既有存檔保留 runtime 值。
- **worktree 並行** [[feature-swarm-branches]]：worktree 一律從 **stale base** 分出（持續性 harness 行為）；先離線解碼 vanilla 再下精確施工單（agent 不負責猜）、分配互斥檔案領域；整合用 cherry-pick + keep-both（同名 test class 用 `--ours` 重貼）。

### 之後可做

- Scene 演出續做：PlayIdle / 手勢動畫（可能走 scene phase-fragment 或 alias 腳本 `PlayIdle`，需解碼 `SceneAdapter`）；camera shot。
- 多解 SM 事件（SkillIncrease/Jail/Bribe…，但須 conditions 才安全，見 [[dispatcher-magic-trigger]]）。
- 新 record：Music / Imagespace / Hazard 等。
