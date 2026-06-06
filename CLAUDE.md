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

**派發器→真實觸發接線（量產入口第一條）：✅ 實機驗證通過（2026-06-05）**——
`examples/story-manager-magictrigger.json` + `examples/MFSE_SpellTrigger.psc`：可複用 magic-effect trigger（`extends ActiveMagicEffect`，`OnEffectStart→MFStoryEventDispatch.Fire(kw,caster,target)`）掛在自訂 MGEF/spell 上，玩家施法→SM 啟動 quest，`Caster` alias=玩家。把 OnInit 測試觸發換成實際遊戲動作。**編譯耐久修法**：dispatcher `.psc` 也 embed 進 CLI，Package.cs 編 user script 時解到 temp 當 sibling header（compiler 把 input 檔所在目錄當 header dir），`Fire()` 免 per-machine cache 即可解析——未來任何經派發器的 trigger 腳本都受惠。297 測試綠。

**可複用 trigger 庫 — 四入口全數實機驗證通過（2026-06-05）**——同一 `Fire()` 一行接不同入口:
- **magic effect（spell）**：`examples/story-manager-magictrigger.json` + `MFSE_SpellTrigger.psc`（`extends ActiveMagicEffect`，`OnEffectStart→Fire`）。玩家施法。✅
- **potion**：`examples/story-manager-potiontrigger.json`（複用 MFSE_SpellTrigger，證明 trigger 與 delivery 無關）。`player.additem` 後喝。✅
- **activator**：`examples/story-manager-activatortrigger.json` + `MFSE_ActivatorTrigger.psc`（`extends ObjectReference`，`OnActivate→Fire`）。`player.placeatme <leverFormID>` 生拉桿再拉。⚠️ model 必須是驗證存在的 vanilla nif（假路徑→隱形物件,見 [[vanilla-nif-paths-must-be-verified]]）。✅
- **dialogue**：`examples/story-manager-dialoguetrigger.json` + `MFSE_DialogueTrigger.psc`（`extends TopicInfo`，`Fragment_0→Fire`，NPC 給任務入口）。NPC 放 Sleeping Giant Inn,`coc RiverwoodSleepingGiantInn` 找「Forged Envoy」。複用 proven dialogue[]+placed-NPC。✅
- 四個 zip 在 `~/skyrim_mods/`（ModForge{Magic,Activator,Potion,Dialogue}Trigger.zip）。

**alias fill 擴充 + 事件表擴充：✅ 實機驗證通過（2026-06-05）**——303 測試綠：
- **`createObject:<ref>@<targetAlias>`**（`CreateReferenceToObject` ALCO/ALCA/ALCL）：quest 啟動時在 `<targetAlias>` 持有的 ref 處生成一個 `<ref>` 新實例（施法→狼出現在腳邊）。解自 vanilla MQGreybeardCall/WERoad12。`examples/story-manager-createobject.json`。
- **`findMatching:closest|any`**（QuestAlias 旗標 `MatchingRefInLoadedArea`[+`MatchingRefClosest`] + alias `conditions`）：在 loaded area 找最近/第一個符合條件的既有 ref（施法→填上最近 NPC）。解自 vanilla MQGreybeardCall Bystander。**踩坑**：先用 `FindMatchingRefNearAlias(LinkedRefChild)` 實機失敗（那只找 editor linked-ref 子物件）——正解是旗標機制。`examples/story-manager-findmatching.json`。
- **事件表 +4**（engine-native 純資料，build/validate 不變）：CraftItem(0x039D86/CRFT,workbench=R1)、PlayerRemoveItem(0x02C6AC/REMP,owner=R1/item=R2)、Arrest(0x06B369/ARRT,guard=R1/criminal=R2)、IncreaseLevel(0x05BD79/LEVL,無 ref 槽,`player.advlevel` 觸發)。`examples/story-manager-events-demo.json`(前三合一)+`examples/story-manager-events-demo2.json`(LevelUp)。
- **解了但移除**：DeadBody（NPC 偵測事件,非玩家觸發）、ActorDialogue/ActorHello（vanilla 分支太多,無條件 quest 輸掉互斥競爭、且會劫持原版對話）、ChangeRelationshipRank（事件由 gameplay 觸發,非 console setter）。**SM 限制**：additive 無條件分支只在 vanilla 少/沒密集處理的事件上可靠生效；坑詳見 [[dispatcher-magic-trigger]]。
- alias fill 現五種、事件表現十個。

**alias OnActivate（alias 腳本，第五個可複用入口）：✅ 實機驗證通過（2026-06-05）**——306 測試綠：
- `QuestAliasSpec` 加 `Script`/`ScriptSource`/`ScriptProperties`：把 Papyrus 腳本（`extends ReferenceAlias`）掛在 quest **alias** 上（存進 `QuestAdapter.Aliases` 的 `QuestFragmentAlias`，v5/objFmt2、綁 alias ID、script flag=Local——解自 vanilla alias-only quest）。腳本跟著「填進 alias 的那個 ref」走,所以能接 **createObject 生成** 或 **findMatching 匹配** 的執行時 ref（base-object 腳本碰不到）。`AttachAliasScript`（Build.StoryManager.cs）+ validate 檢查 script 屬性 ref + Package 編譯 `ScriptSource`。
- 端到端 `examples/story-manager-aliastrigger.json` + `MFSE_AliasActivate.psc`（`OnActivate→Fire`）：施法→createObject 生箱子於腳邊→開箱→alias OnActivate→`Fire(MFSE_AliasKW)`→啟動 MFSE_AliasTarget。zip `~/skyrim_mods/ModForgeAliasTrigger.zip`。
- **編譯踩坑**：`extends ReferenceAlias` 的 native 編譯要 `MODFORGE_PAPYRUS_HEADERS=~/.cache/modforge/papyrus/Source/Scripts`（loose Source 不含 ReferenceAlias.psc）。

**Quest 階段/目標推進（SM quest journal 進度）：✅ 實機驗證通過（2026-06-05）**——308 測試綠：
- **`StageSpec.startUpStage`**（新）：引擎在 quest 啟動時自動 `SetStage` 到被標記的 stage（vanilla QSDT `QuestStage.Flag.StartUpStage`，`Generator.Build.Actors.cs` `BuildQuests`）。這是 SM 啟動的 quest 能**自顯開場 log entry / objective** 的缺口——之前停在 stage 0、journal 空白。validate 限至多一個。
- **adapter 合併修正**：`WireQuestStages` 原本 `=` 覆寫 `VirtualMachineAdapter`，會清掉 alias 腳本的 `.Aliases`。改成**合併**進既有 `QuestAdapter`（stage fragment 的 `FileName`/`Scripts`/`Fragments` + alias 腳本的 `Aliases` 共存於單一 adapter，v5/objFmt2）。解碼 packaged esp 確認三者共存。
- **完成半段免新程式碼**：複用 alias OnActivate，新增可複用 `examples/MFSE_AdvanceStage.psc`（`OnActivate→GetOwningQuest().SetStage(Stage)`；`GetOwningQuest()` 在「執行時 alias OnActivate、非 StartGameEnabled」情境可用，與 dialogue TIF 在 game-load 的 None 坑不同）。
- 端到端 `examples/story-manager-queststage.json`：施法→SM 啟動 quest 於 startUpStage 10（objective 顯示）+ createObject 生箱→開箱→alias `SetStage(20)`→objective 完成 + quest 關閉（stage 20 completeQuest）。zip `~/skyrim_mods/ModForgeQuestStage.zip`。

**alias 推廣到一般 quest（非 storyEvent）：✅ 實機驗證通過（2026-06-05）**——309 測試綠：
- 抽出共用 `BuildQuestAliases(quest,qs,def?)`（storyEvent 與一般 quest 共用；fromEvent 僅 `def!=null`）+ `BuildStandaloneQuestAliases()`（Build.cs 在 BuildStoryManager 後、WireQuestStages 前；非 storyEvent quest 也建 forced/uniqueActor/createObject/findMatching + alias 腳本，跳 fromEvent）。validate 抽出 `ValidateQuestAlias`（兩路共用，def=null 時 fromEvent 報錯）。重構行為不變,既有 SM 測試全綠。
- 一般 StartGameEnabled quest 的 alias 在「quest 啟動＝遊戲載入」時填。範例 `examples/quest-alias-standalone.json`：forced player→createObject 生箱於玩家→開箱→alias OnActivate `SetStage` 完成關閉 quest。zip `~/skyrim_mods/ModForgeStandaloneAlias.zip`。

**Scene 在場偵測自動觸發（隨從在場偵測 + 互動 Scene，IDEAS 1a）：✅ 全數實機驗證通過（2026-06-06）**——318 測試綠：
- `SceneSpec.AutoStart`（新 `SceneAutoStartSpec`：triggerDistance/requireLineOfSight/cooldownSeconds/pollSeconds/**brawlOnEnd**）。有此塊時 `Generator.Build.Scene.cs` 清掉 Scene 的 `BeginOnQuestStart`，並 `AttachSceneController` 把可複用 `MFSceneBanterController`（extends Quest）掛上 host quest，wire 進 scene(object) + 前兩個 actor alias 索引(int) + 調參(float/bool)。
- controller `assets/papyrus/MFSceneBanterController.psc`：鏈式 `RegisterForSingleUpdate`（非常駐 OnUpdate，省存檔膨脹）→ 玩家與兩 actor 同場 + 範圍內 + 非死/戰鬥 +（選配 LOS）+ 冷卻過 → `Scene.Start()`。冷卻用 `GetCurrentRealTime()`（不受 timescale 影響）。**`brawlOnEnd`**：`Poll()` 偵測 scene 從播放→結束，呼叫 `StartBrawl()`（雙向 `Actor.StartCombat`）讓兩人吵完動手。`.pex` embed 進 CLI，Package §5c 遇 autoStart scene 自動出貨。
- **NPC `essential`/`protected`**（新 NpcSpec 欄位 → `NpcConfiguration.Flag`）：essential 配 brawlOnEnd ＝ 非致命鬥毆（輸家進 bleedout 後復原）。
- validate：autoStart 需 host quest `StartGameEnabled` + ≥2 actor + 正數調參。範例 `examples/scene-presence-banter.json`（Sleeping Giant Inn 擺兩 essential unique NPC，coc 站近 → 互鬥對話 → 動手；離開冷卻後再觸發）。**base Scene record 首次實機確認**（之前一直 structural-only）。zip `~/skyrim_mods/ModForgeSceneBanter.zip`。設計 `docs/superpowers/specs/2026-06-06-presence-gated-scene-design.md`。
- **實機狀態**：在場偵測 + 對話 Scene + brawlOnEnd（吵完動手、essential 非致命）全部 ✅ 玩家確認成功（2026-06-06）。base Scene record 首次實機確認亦在此達成。
- 切片限定：actor 是具名 `UniqueActor`（非動態掃 teammate + ForceRefTo，留後續層）；無 move/animate/FURN scene action（IDEAS 1b）。

**Scene 非對話 action — NPC 劇情演出（走位 + 停頓，IDEAS 1b 第一切片）：✅ 實機驗證通過（2026-06-06）**——326 測試綠（玩家確認：Borin 走過旅館→停頓→吵架）：
- 離線解碼 vanilla（新 diag **`scnscan`** + `packagediag`）：非對話 beat 就是另一種 `SceneAction`，靠 `Type` 區分——**Package**（`Packages=[PACK FormKey]`，actor 跑該 AI 套件，跨 `StartPhase..EndPhase` phase 窗；`dunTolvaldsCaveCrownScene` 三鬼走向王冠 = Travel PACK）、**Timer**（`TimerSeconds`，停頓/節拍；`BardSongs*` 用它 pace）。**關鍵複用**：vanilla scene Package action 只是引用一個 PACK FormKey，而 ModForge 已有完整 PACK builder——所以切片 = **讓 scene beat 引用作者已在 `packages[]` 定義的套件**，零新套件管線。sit/use-furniture（`MQ306EsbernSit` = UseItemAt PACK，slot16 SingleRef→FURN ref）與獨立 PlayIdle 緩做（需新 PACK template）。
- `SceneSpec.Actions`（新 `SceneActionSpec`：actor/package/timerSeconds/startPhase/endPhase）+ `ScenePhaseSpec.Lines` 可空（**beat phase**：只當 action 的 phase 窗，不生 topic/Dialog action）。`Generator.Build.Scene.cs`：lineless phase → 加 ScenePhase 但跳 Dialog；`actions[]` → Package（PACK ref 在 `WireScenes` pass 2 解析進 `action.Packages`，`sceneActionWires`）/ Timer（`TimerSeconds`）。validate：beat phase 需 action 覆蓋、每 action exactly-one(package|timer) + actor 合法 + phase 窗在範圍。
- 端到端 `examples/scene-action-performance.json`（autoStart + beat phase：Borin 走過 Sleeping Giant Inn 到 vanilla `RiverwoodInnCenterMarker`〔Travel PACK，同內裝 cell 同 navmesh〕+ 8s Timer pace，然後兩人吵架）。解包 esp 確認：6 actions = 4 Dialog + 1 Package（`Packages=[MF_BorinApproach]`）+ 1 Timer（8s）、5 phases（beat + 4）。zip `~/skyrim_mods/ModForgeSceneAction.zip`。設計 `docs/superpowers/specs/2026-06-06-scene-action-performance-design.md`。

**GlobalVariable (GLOB) builder：✅ 結構驗證通過（2026-06-06）**——333 測試綠：
- ModForge 之前只能「引用」既有 global、不能「建立」。補上 `GlobalSpec`（editorId/type/value/constant）+ 頂層 `globals` + `Generator.Build.Globals.cs`（`mod.Globals.AddNewShort/AddNewInt/AddNewFloat`；short/long(int)/float 三子型；`constant` → `Global.MajorFlag.Constant`(0x40) via `MajorRecordFlagsRaw`；在 BuildFormKeyTable 前建，故 condition/region 可按 editorId 引用）+ `Generator.Validate.cs ValidateGlobals`（type ∈ short|long|float + editorId 唯一）。
- GLOB = 全域共享單一數字、存檔保存、**condition 零腳本可讀**（`GetGlobalValue`）；當旗標/re-arm token/計數器/調參常數。與 quest stage（GetStage，quest 級）互補。**存檔已固化坑**同 `.seq`：value 只是初值，既有存檔保留 runtime 值。
- 範例 `examples/globals.json`（三型 + constant + dialogue 條件讀旗標）；解包 esp 確認 GlobalShort/Int/Float 三型正確落盤。`GlobalTests.cs` 7 測。**runtime 改值（SetValue）**仍走 Papyrus（result script / fragment / alias 腳本）——scene replay policy 用 GLOB gate 是規劃中的下游消費者。

**Scene 重播策略（playOnce / playHour / gateGlobal）：✅ 實機驗證通過（2026-06-06）**——338 測試綠（玩家確認：吵一次後不再循環）：
- 解決原始問題「在場觸發的對話無限循環」。`SceneAutoStartSpec` 加 `playOnce`/`playHour`/`playHourTolerance`/`gateGlobal`（全 AND 在既有 cooldown 上）。controller `.psc` 對應加 `PlayOnce`/`PlayHour`/`PlayHourTolerance`/`Gate`(GlobalVariable) property + 閘門：playOnce 播完停 poll（`OnUpdate` 不再 re-register，省存檔）；playHour 用 `CurrentHour`（`GetCurrentGameTime` 小數×24）+ `HourDistance`（環形）窗；Gate 用 `GetValue()` 擋、播完 `SetValue(1)`（別的事件 `SetValue(0)` 重新武裝——用上剛建的 GLOB）。
- `AttachSceneController` 加三個直填 prop + gateGlobal object prop（pass 2 `WireScenes` 解析，`sceneGateWires`）。validate：playHour 0..24、tol>0、gateGlobal CheckRef。
- **重編了 `.pex`**（native `~/tools/papyrus-compiler` + `MODFORGE_PAPYRUS_HEADERS=~/.cache/modforge/papyrus/Source/Scripts`；3964→5484 bytes，含新 props/CurrentHour/HourDistance/SetValue）。範例 `examples/scene-replay-policy.json`（playOnce；註解列 playHour/gateGlobal 變體）。zip `~/skyrim_mods/ModForgeReplayPolicy.zip`。
- **實機 PASS（玩家確認）**：站近兩人 → 吵一次 → 離開再回 → 不再吵（playOnce 生效）。

**Scene 演出第二切片 — NPC 坐下/用家具（SitTarget PACK，IDEAS 1b）：⚠️ 結構驗證通過、待實機（2026-06-06）**——340 測試綠：
- **關鍵發現**：`SceneAction.TypeEnum` 只有 Dialog/Package/Timer 三種（ikdasm 確認）——所以「NPC 做動作」一律走 **Package action**。坐下不是新 scene-action type，而是新 **PACK template**，直接套用既有 scene Package-action 管線，scene 側零改動。
- 新 template **`SitTarget`=`Skyrim.esm:0x0A9277`**（editorID 就叫 "SitTarget"，vanilla `MQ306EsbernSit` 背後的 procedure）。作者槽：**16** `Target`（`PackageDataTarget` SingleRef→`PackageTargetSpecificReference` 指向放置的家具 ref，**必填**）、**3** `Wait Time`(float)、**4** `Stop Movement Flag`(bool)。`packagediag` 解碼；家具 base 驗證 `CommonChair01F`(`0x06E7A8`)。**SitTarget 走位＋坐合一**（引擎自動 path 到家具再坐），所以一個 action 同時示範移動與坐下，免另開 Travel。家具 ref 因是 package SingleRef target 自動強制 persistent（`deferredAnchorEds`）。
- 程式碼：`PackageTemplates.SitTarget` + `SitTargetSpec`（Spec.Packages.Templates.cs）+ `ApplySitTargetData`（Build.Packages.Advanced.cs）+ dispatch（Build.Packages.cs）+ validate（target 必填，Validate.Npcs.cs）。`PackageTests` 加 slot-16 / wait-time / 持久化三斷言。
- 端到端 `examples/scene-sit-performance.json`（autoStart + beat phase：Borin 走過 Sleeping Giant Inn 到擺在 `RiverwoodInnCenterMarker`〔navmesh 驗證點〕的 `CommonChair01F` 坐下 + 10s Timer，然後兩人吵架；brawlOnEnd 起身互毆）。解包 esp 確認 Package action→`MF_BorinSit`(SitTarget,target→chair)、Timer 10s。zip `~/skyrim_mods/ModForgeSitAction.zip`（含重編 5484-byte controller pex）。

**之後可做**：再多解事件（SkillIncrease/Jail/Bribe… `smtree Skyrim.esm` 列舉,但須用 conditions 才安全,見 [[dispatcher-magic-trigger]]）;Scene 演出續做（sit/use-furniture 已做＝SitTarget PACK；剩 PlayIdle/動畫 event name〔可能走 alias 腳本 `PlayIdle`，非 SceneAction〕；camera shot）——讓演出更豐富。
