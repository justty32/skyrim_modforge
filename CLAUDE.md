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
dotnet run --project src/ModForge.Cli -- compile assets/papyrus/MFIdentityBook.psc assets/papyrus/
dotnet run --project src/ModForge.Cli -- compile assets/papyrus/MFIdentityDefault.psc assets/papyrus/
dotnet run --project src/ModForge.Cli -- compile assets/papyrus/MFIdentityController.psc assets/papyrus/
dotnet run --project src/ModForge.Cli -- compile assets/papyrus/MFIdentityAutoGrant.psc assets/papyrus/
```
（需要 Wine + CK PapyrusCompiler 環境；native 走 `~/tools/papyrus-compiler` + `MODFORGE_PAPYRUS_HEADERS=~/.cache/modforge/papyrus/Source/Scripts`。）這六個 `.psc`（dispatcher、在場偵測 Scene controller、身份書 MFIdentityBook、預設身份授予 MFIdentityDefault、主身份 controller MFIdentityController、自動授予 trigger MFIdentityAutoGrant）有任何改動時，同樣需要重跑對應步驟並將新的 `.pex` 保留在本機（不 commit）。六個 `.pex` 都被 `ModForge.Cli.csproj` embed 為 EmbeddedResource（條件式：缺檔仍可 build，runtime 才 warn）。

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

**身份系統 Phase-2/C**:① Adventurer 預設身份自動授予 ✅、② `activeWhen` 情境條件 ✅、④ controller 主身份+手動覆寫 ✅（皆 in-game 確認 2026-06-07）；✅ ⑤ 身份對應互動 **#5a 商人交易 UI + #5b 護衛任務 + #5c 聖騎士 smite 細調（grantPerks）** + **龍裔首吼（autoGrantWhen）**（皆 in-game 確認 2026-06-07，見下「已落地」）；尚未做：③ 聲望/行為追蹤。

### 已落地功能（時間序；實作細節見 git log / CODE_MAP / SPEC）

**對話 / 任務 / Story Manager**
- **SM spec 管線**：`QuestSpec.storyEvent`(event+conditions) + `aliases`；build 自動生 SMBN→SMQN 掛原版根、清 StartGameEnabled。事件表 `StoryManagerEvents`（十個 engine-native 事件）。
- **alias fill 五種**：`fromEvent:<slot>` / `forced:<ref>` / `uniqueActor:<ref>` / `createObject:<ref>@<alias>` / `findMatching:closest|any`。
- **可複用 trigger 庫（五入口，同一 `Fire()`）**：magic-effect / potion / activator / dialogue / alias-OnActivate。zip `~/skyrim_mods/ModForge{Magic,Potion,Activator,Dialogue,Alias}Trigger.zip`。通用派發器 `assets/papyrus/MFStoryEventDispatch`（embed 進 CLI）。
- **Quest 階段**：`StageSpec.startUpStage`（啟動自顯 objective）+ stage 推進（`MFSE_AdvanceStage.psc`）；alias 也適用一般（非 storyEvent）quest。
- **身份系統（輕量職業）Phase-2/C 完成**（in-game 確認 2026-06-07，`ModForgeIdentity.zip`）。**完整欄位/語意見 `SPEC-dialogue-quests.md`「Identities」、build wiring 見 `CODE_MAP.dialogue-quests.md`、建好的 esp 用 `identitydiag` 探。** 一句話地圖：
  - **取得**：讀書（`MFIdentityBook`）/ `default:true` 開局自動授（`MFIdentityDefault`）/ `autoGrantWhen{actorValue,threshold}` 玩家 AV 過門檻自動授（`MFIdentityAutoGrant`，如 Dragonborn `DragonSouls≥1`；純 Papyrus 讀 AV、免 SKSE）。
  - **閘**：`identity`（持有）/ `primaryIdentity`（主身份，由 `MFIdentityController` poll 算 `MF_PrimaryIdentity` GLOB、`setPrimaryIdentity` 對話可覆寫）；`activeWhen` 情境窄化正向閘。
  - **授予**：`grants`(SPEL) + `grantPerks`(PERK，如 smite-vs-undead 條件 perk)。
  - **互動**（多動作 TIF result fragment，皆純 record+生成、無 user script）：`hello` 招呼 / `openBarter` 交易 / `rewardItem` 獎勵 / `evaluateSpeakerPackages` 重評估 → 組出 Adventurer 護衛 quest（follow PACK gated on `GetStage`）。
  - **四個 reusable .psc**（Book/Default/Controller/AutoGrant）embed 進 CLI、Package 條件式出貨。
  - **踩坑**：`MFIdentityBook` 必 **extends ObjectReference**（OnRead 非 Book event）[[book-onread-needs-objectreference]]；state-varying 招呼=**一個 Hello topic 多條 INFO**（順序定優先，非多 topic 競 priority）[[conditioned-hello-one-topic-many-infos]]；NPC `autoCalcStats` 必配 `class` 否則 0 血倒地 [[autocalc-without-class-dead-npc]]；acquire scene 用 `beginOnQuestStart:false`（書 Start() 唯一觸發）。
  - **未做**：#3 聲望/行為追蹤（需先定設計）。

**Scene 劇情演出**
- **在場偵測 autoStart**：`SceneSpec.AutoStart`（triggerDistance/LOS/cooldown/poll/**brawlOnEnd**）+ 可複用 `MFSceneBanterController`。`ModForgeSceneBanter.zip`。
- **非對話 action**：beat phase + Package（走位，引用 `packages[]` 的 PACK）+ Timer（停頓）。`ModForgeSceneAction.zip`。
- **重播策略**：`playOnce` / `playHour(+tolerance)` / `gateGlobal`。`ModForgeReplayPolicy.zip`。
- **per-phase headtrack/facing**：`ScenePhaseSpec.HeadtrackActor/HeadtrackPlayer/FaceTarget`。
- **scene 條件**：`SceneSpec.Conditions`（scene-level，僅 `beginOnQuestStart`）+ `ScenePhaseSpec.StartConditions/CompletionConditions`（per-phase）。
- **PlayIdle 演出**（in-game 確認 2026-06-07）：`SceneActionSpec.Idle`（IDLE ref）→ SCEN `SceneAdapter` per-phase OnStart fragment（`SF_<scene>.Fragment_<phase>` 跑 `<alias>.GetActorRef().PlayIdle()`，第三種 fragment 家族）。純產生器 `Generator.SceneFragments.cs`、掛載 `AttachSceneFragments`；idle action 同時發一個 Timer（hold）讓 phase 能 run。`find <esm> <kw> idle` 探 IDLE。`ModForgePlayIdle.zip`（宣誓鞠躬+獻手）。

**PACK templates（共 10）**：sandbox / sleep / travel / usemagic / patrol / follow / escort / **sittarget**（坐家具）/ **activate**（活化 lever/door）/ **eat**。

**新 record builders**
- **GlobalVariable (GLOB)**：`GlobalSpec`（short/long/float + constant）。
- **Light (LIGT)**：`LightSpec`（color/radius/fade/flags…），用 placements 放置。
- **Projectile (PROJ) + Explosion (EXPL)**：自訂法術飛行彈+爆，鏈 EXPL←PROJ←MGEF←SPEL。
- **NPC inventory**：`NpcSpec.Items`（攜帶/自動裝備/死亡掉落）；`NpcSpec.essential/protected`。

**光照管線（明亮室內）**（in-game 確認 2026-06-09，`ModForgeBrightInterior.zip`）：`LightingTemplate (LGTM)` + `ImageSpace (IMGS, ≠ 既有 IMAD)` base record，模板抄 vanilla + 只覆寫亮度欄位；CELL 逐欄光照 `cells[].lightingTemplate/imageSpace/lighting(inline XCLL)`，含 **DALC 六方向環境光**（打亮地城核心：LGTM→`DirectionalAmbientColors`、XCLL→`AmbientColors`）。inline 無給且有 template → 全繼承。診斷 `lgtmdiag`/`imgsdiag`。**欄位/語意見 `SPEC-world.md § lighting`、wiring 見 `CODE_MAP.world.md`。** 踩坑：① interior CELL 無 XCLL = 黑房；② IMGS 不給 `template` 從零起（HDR 欄位全 0）行為可能怪，建議抄 vanilla IMGS 再調；③ build 期 `ResolveLightingRef` 不分型別，靠 Validate 的 cross-type 檢查擋打錯 slot。**未做**：IMGS 掛 weather（室外調色，§12 室外那半）；明亮 preset 庫。

**一次測 showcase**：`ModForgeShowcase.zip`（批次#1：Light + headtrack + SitTarget）、`ModForgeShowcase2.zip`（批次#2：firebolt PROJ/EXPL + NPC 武器 + scene 閘）。新 diag：`smtree` / `scnscan` / `packagediag` / `lightdiag` / **`identitydiag`（從建好的 esp 還原身份 registry：controller faction↔code、default grants、acquire books、控制 GLOB）** 等。

### 鐵律與踩坑（複用知識，勿重蹈）

- **SM 結構** [[story-manager-kill-recipe]]：一事件根→一條共用分支→多 quest node（串 PreviousSibling）；事件根下多分支互斥；**引擎一事件只啟動一個最先符合的 quest**（正確 radiant，非 bug）；ESL 能裝 SM；`SimpleActor`（雞/兔）不發 Kill 事件。
- **SM alias** [[story-manager-kill-recipe]]：① location 槽 alias 必須 `Type=Location`（fromEvent 'L' 自動）；② 任一必填 alias 填不上 → quest 靜默不啟動；③ 殺/指向被 `ReservesLocationOrReference` 保留的 NPC 需 `allowReserved`（uniqueActor 強制）；④ `QuestAlias.Flags` nullable，旗標用 `GetValueOrDefault()` 起底。
- **SM 事件可靠性** [[dispatcher-magic-trigger]]：additive 無條件分支只在 vanilla 少/沒密集處理的事件上可靠；密集事件（ActorDialogue/Hello）會輸掉互斥競爭、劫持原版對話——須用 conditions（或走自訂 ScriptEvent keyword）。
- **autoStart scene 閘門**：用 `autoStart.gateGlobal`（controller 端檢查），**不要**用 scene-level `conditions`——controller 強制 `Scene.Start()`，繞過 scene begin-conditions（後者只 gate `beginOnQuestStart` scene）。
- **scene 動作**：`SceneAction.TypeEnum` 只有 Dialog/Package/Timer——「走位/坐/活化」走 Package action 引用 PACK；**「播動畫」走 `SceneActionSpec.Idle`（SceneAdapter phase fragment，非 SceneAction）**。
- **scene PlayIdle**（in-game 2026-06-07 確認，多坑連環）[[scene-playidle-recipe]]：① **SceneAdapter VMAD 三個 canonical 值不可少,否則引擎靜默跳過 fragment**——`ScenePhaseFragment.Unknown=16777216`(0x01000000;=quest 的 `Unknown2=1` 坑的 scene 版)、`SceneScriptFragments.ExtraBindDataVersion=2`、`ScriptEntry.Flags=Local`(全 265 vanilla phase-frag scene 一致)。② **每個帶 fragment 的 phase 必須有一個 SceneAction(Timer)**,空 phase 引擎不 run、fragment 不 fire(故 idle action 同時發一個 Timer 當 hold)。③ **不是每個 IDLE 都能 PlayIdle**:跪/祈禱(`IdleBlessingKneel*`/`IdleCrouchedPray*`)綁神壇家具,自由 `PlayIdle` 無效;挑 vanilla 腳本實際 `.PlayIdle()` 過的(鞠躬 `IdleSilentBow`/獻手 `IdleGive`/`IdleStop`/offset 類),`grep -ri '.PlayIdle(' ~/.cache/modforge/papyrus/Source/Scripts` 查。④ 連播同一 idle 不明顯重播,要不同手勢才看得出兩 fragment 都 fire。⑤ 座椅/sandbox NPC 忽略 PlayIdle → 給站立包(Sandbox `allowSitting:false`)。⑥ console `playidle` 吃 EditorID 不吃 FormID(Papyrus `PlayIdle(form)` 吃 form,spec `idle` ref 綁的就是 form)。
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
