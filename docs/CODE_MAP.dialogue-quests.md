# CODE_MAP — 對話・任務・Story Manager・腳本

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：quest + stages + objectives、dialogue topics + INFO、banter、multi-actor scenes、CTDA conditions、Story Manager event quests、ScriptEvent、word walls、Papyrus 附加。

## Examples

| 檔案 | 對應功能 |
|-----|---------|
| `examples/dialogue_spec.json` | 單一 NPC 對話樹 |
| `examples/dialogue_conversation_spec.json` | 多輪對話（conversation 模式）|
| `examples/scene_spec.json` | 雙 NPC 場景（SCEN）|
| `examples/scene-presence-banter.json` | 在場偵測自動觸發 Scene（autoStart + MFSceneBanterController）|
| `examples/scene-action-performance.json` | Scene 非對話 action（§1b 演出）：beat phase + Package action（NPC 走位到 marker，Travel PACK）+ Timer action（停頓）|
| `examples/scene-sit-performance.json` | Scene 演出第二切片：NPC 走到椅子並**坐下**（SitTarget PACK，走位+坐合一）+ Timer + brawlOnEnd（SitTarget 細節見 [CODE_MAP.npcs-packages.md § AI Packages](CODE_MAP.npcs-packages.md#ai-packagespack)）|
| `examples/scene-playidle.json` | Scene 演出第三切片：actor 在 phase 邊界**播 IDLE 動畫**（`SceneActionSpec.Idle` → SCEN SceneAdapter phase fragment `SF_<scene>.Fragment_N`，跑 `<alias>.GetActorRef().PlayIdle()`）；站→鞠躬→獻手（用 vanilla `IdleSilentBow 0x0D8734` / `IdleGive 0x0B5E20`，限 vanilla 腳本 PlayIdle 過的 idle，跪/祈禱綁家具的不行）|
| `examples/identity-paladin.json` | 輕量身份/職業系統 MVP：讀書(MFIdentityBook OnRead)→入 FACT+授常駐 ability+播宣誓鞠躬 scene(複用 PlayIdle)；Merchant toggle；NPC 依 `primaryIdentity` 改招呼(GetInFaction CTDA，高優先序排除)；**Adventurer baseline（`default:true`，開局自動授予 + 墊底招呼）**；**Paladin `activeWhen` 重甲才 active；對話選項 `setPrimaryIdentity` 手動覆寫主身份（controller）；#5a Merchant-only `openBarter` 交易（Townsfolk 為 vendor）；#5b Adventurer-only 護衛 quest（MF_Traveler follow PACK gated on GetStage、`rewardItem` 200 金）；#5c Paladin `grantPerks`→`MF_SmiteEvilPerk`（+25% vs undead/daedra）；Dragonborn `autoGrantWhen` DragonSouls≥1（priority 40，首吼後升主身份）**|
| `examples/scene-headtrack.json` | Scene 每-phase headtrack/facing：說話者 gaze 指向另一 actor／玩家／無人（`ScenePhaseSpec.HeadtrackActor`/`HeadtrackPlayer`/`FaceTarget`）|
| `examples/showcase-multi.json` | 多功能 showcase（一包一次測）：自訂 Light + headtrackPlayer + SitTarget beat + autoStart/brawl |
| `examples/scene-conditions.json` | Scene 條件閘：scene-level + per-phase start/completion CTDA（GetGlobalValue 等，refs by editorId）|
| `examples/showcase-multi2.json` | 多功能 showcase #2：firebolt PROJ/EXPL（spell tome 學）+ NPC 庫存武器 + scene 條件閘（GLOB gate）|
| `examples/scene-replay-policy.json` | autoStart 重播策略：`playOnce`（只播一次）/ `playHour`/`playHourTolerance`（到某遊戲時辰才播）/ `gateGlobal`（GLOB re-arm token）|
| `examples/quest_stages_spec.json` | quest stages + objectives + log entries |
| `examples/word_wall_spec.json` | word wall 觸發教字 |
| `examples/story-manager-kill.json` | KillActor SM 事件 |
| `examples/story-manager-assault.json` | Assault SM 事件 |
| `examples/story-manager-changelocation.json` | ChangeLocation SM 事件 |
| `examples/story-manager-craftitem.json` | CraftItem SM 事件（玩家製作物品；engine-native）|
| `examples/story-manager-events-demo.json` | 三個 engine-native 事件合一測試（CraftItem/PlayerRemoveItem/Arrest）|
| `examples/story-manager-events-demo2.json` | IncreaseLevel SM 事件（玩家升級；engine-native，無 ref 槽）|
| `examples/story-manager-uniqueactor.json` | uniqueActor alias fill |
| `examples/story-manager-createobject.json` | createObject alias fill（事件觸發→在另一 alias 處生成物件；複用 magic trigger）|
| `examples/story-manager-findmatching.json` | findMatching alias fill（loaded area 裏找最近的符合 conditions 的既有 ref；複用 magic trigger）|
| `examples/story-manager-aliastrigger.json` + `MFSE_AliasActivate.psc` | alias OnActivate（腳本掛 quest alias→活化執行時 createObject 生成的 ref→Fire 鏈下個事件；複用 magic trigger + createObject）|
| `examples/story-manager-queststage.json` + `MFSE_AdvanceStage.psc` | journal 進度：`startUpStage` 啟動即顯示 objective + alias OnActivate `SetStage` 完成並關閉 quest（startUpStage + alias 腳本 + stage fragment 三者共存）|
| `examples/quest-alias-standalone.json` | 一般（非 storyEvent / StartGameEnabled）quest 帶 alias：forced player + createObject 生箱 + alias OnActivate（`BuildStandaloneQuestAliases`）|
| `examples/story-manager-scriptevent.json` | ScriptEvent 自訂觸發 |
| `examples/MFSE_TestTrigger.psc` | ScriptEvent OnInit 觸發腳本 |
| `examples/story-manager-magictrigger.json` | ScriptEvent 經 magic effect 觸發（玩家施法→啟動 quest）|
| `examples/MFSE_SpellTrigger.psc` | 可複用 magic-effect trigger（OnEffectStart→Fire）；spell + potion 共用 |
| `examples/story-manager-activatortrigger.json` | ScriptEvent 經 activator 觸發（拉桿→啟動 quest）|
| `examples/MFSE_ActivatorTrigger.psc` | 可複用 activator trigger（OnActivate→Fire）|
| `examples/story-manager-potiontrigger.json` | ScriptEvent 經 potion 觸發（喝藥水→啟動 quest，複用 MFSE_SpellTrigger）|
| `examples/story-manager-dialoguetrigger.json` | ScriptEvent 經 NPC 對話觸發（選對話→啟動 quest）|
| `examples/MFSE_DialogueTrigger.psc` | 可複用 dialogue trigger（TopicInfo `Fragment_0`→Fire）|
| `examples/scripts/MFDemoQuestScript.psc` | quest fragment Papyrus 示範 |

---

## Tests

| 測試檔案 | 涵蓋 |
|---------|-----|
| `ConditionTests.cs` | CTDA condition 函數、comparator、ref 解析 |
| `DialogueTests.cs` | dialogue topic / INFO / greeting 生成 |
| `QuestStageTests.cs` | stage log text / objective fragment / VMAD |
| `SceneTests.cs` | SCEN actor / phase / dialogue action；非對話 action（Package→Packages PACK ref / Timer→TimerSeconds）+ beat phase（無 lines→無 Dialog action/topic）+ LastActionIndex；**idle action 發 Timer（hold；純 build 無 VMAD）**；autoStart → controller VMAD 掛接 + 清 BeginOnQuestStart + 調參 props + **重播策略 props（playOnce/playHour/gateGlobal→GLOB object prop）** + validate gate |
| `SceneFragmentTests.cs` | PlayIdle 純產生器（`SceneNeedsFragmentScript`/`SceneFragmentScriptName`/`GenerateSceneFragmentSource`：extends Scene Hidden、`Fragment_<phase>`、`GetActorRef()`）+ `AttachSceneFragments`（.pex 在才掛 SceneAdapter、PhaseFragments 數/ScriptName/OnStart flag/FragmentName、Actor_ object prop→host quest+alias index）+ validate（idle-only OK、idle+timer hold OK、idle+package 拒）|
| `IdentityTests.cs` | 身份系統：`IdentitySpec` 預設、每身份建 FACT（外部/已宣告不重建）、validate（dup id / bad grant）、acquireBook → `MFIdentityBook` VMAD + 屬性綁定；**default → `MF_IdentityDefaultQuest`（StartGameEnabled）+ `MFIdentityDefault` VMAD，Factions[]/Grants[] list property（只收 default、grant 去重）、無 default 不建、無 grant 省 list**；**activeWhen 窄化正向閘且跑玩家、不污染高優先序排除**；**controller：primaryIdentity 建 MF_PrimaryIdentity/MF_IdentityOverride GLOB + `MF_IdentityControllerQuest`（Codes[]、無 default 不建 granter）、global-based primary CTDA、setPrimaryIdentity TIF**；**grantPerks → 書綁 GrantPerk[0] + default quest 綁 Perks[]**；**autoGrantWhen → `MF_IdentityAutoGrantQuest` + Factions[]/AvNames[]/Thresholds[] 平行、無 autoGrant 不建** |
| `StoryManagerBuildTests.cs` | SM build pass 2（SMBN/SMQN 掛接、alias fill 接線、alias 腳本 VMAD 掛接、`startUpStage` QSDT flag、stage fragment + alias 腳本共存於單一 adapter、非 storyEvent quest 也建 forced/createObject alias + 腳本）|
| `StoryManagerEventsTests.cs` | 事件登錄表欄位（FormKey / slot 對應）|
| `StoryManagerEventsMoreTests.cs` | 擴充事件（ChangeLocation/CastMagic/AddItem/Assault/ScriptEvent）|
| `StoryManagerValidateTests.cs` | SM validate（事件名、alias 語法、keyword 要求）|
| `WordWallTests.cs` | word wall 教字 quest fragment（⚠️ 需本機 Skyrim.esm）|

---

---

## Classes（職業 CLAS）
→ **說明文件**：[SPEC-dialogue-quests.md § classes](SPEC-dialogue-quests.md#classes-clas)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Magic.cs` | `ClassSpec`（attribute weights / skill training）|
| Build P1 | `Generator.Build.Classes.cs` | 建 Class record |
| Validate | `Generator.Validate.Npcs.cs` | class ref 檢查（npc → class）|

---

## Quest 基礎（stages / objectives）
→ **說明文件**：[SPEC-dialogue-quests.md § Quest stages](SPEC-dialogue-quests.md#quest-stages-log-entries--objective-wiring)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Dialogue.cs` | `QuestSpec`, `StageSpec`（含 `startUpStage` = QSDT 起始 stage flag）, `ObjectiveSpec` |
| Build P1 | `Generator.Build.Actors.cs` `BuildQuests` | 建 Quest record + QSDT stages（log/complete/fail flag、`startUpStage`→`QuestStage.Flag.StartUpStage`）+ QOBJ objectives |
| Build P1 | `Generator.Build.Dialogue.cs` | dialogue Branch + Topic + INFO；greeting 自動生成 |
| Build P2 | `Generator.Build.QuestStages.cs` | stage log-entry CTDA + objective fragment VMAD（**合併**進既有 QuestAdapter，不覆寫 alias 腳本的 `.Aliases`）|
| Build P2 | `Generator.QuestFragments.cs` | 自動生 SetObjectiveDisplayed/SetObjectiveCompleted Papyrus fragment |
| Validate | `Generator.Validate.Quests.cs` | stage index 唯一/遞增、`startUpStage` 至多一個、objective↔stage 連結、script ref 存在；**scene action：idle⊕package、package⊕timer 互斥（idle+timerSeconds=pose hold 合法）、至少一個（idle ref 檢查）** |
| Diag | `Diagnostics.Quests.cs` | stages / objectives / aliases / VMAD 腳本 dump |
| Diag | `Diagnostics.Dump.Quest.cs` | quest + scene 結構化完整 dump |

---

## Dialogue 對話樹
→ **說明文件**：[SPEC-dialogue-quests.md § dialogue](SPEC-dialogue-quests.md#dialogue)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Dialogue.cs` | `DialogueSpec`, `DialogueInfoSpec`；**`hello`（bool）= 這行是 NPC 自動招呼(Misc/Hello)而非玩家選單 topic** |
| Build P1 | `Generator.Build.Dialogue.cs` | Branch / Topic / INFO 建立；player-topic 優先度管理；**`hello:true` 招呼：同一 (NPC,quest) 的所有招呼合進 **一個** Hello topic 的多條 INFO(conditioned 在前、plain `greeting` 墊底)——**鐵律:招呼靠 INFO 順序選取(第一個條件符合的勝出),不是多個獨立 Hello topic 靠 priority 競爭(那樣引擎只取一個 topic、其他無視);vanilla 237/297 Hello topic 多 INFO**(in-game 2026-06-07 確認身份招呼)|
| Build P2 | `Generator.Build.Conditions.cs` | INFO CTDA 條件接線 |
| Validate | `Generator.Validate.Quests.cs` | speaker NPC ref、quest ref、condition function |
| Diag | `Diagnostics.Dialogue.cs` | topic / INFO / condition / result-script dump |

---

## Banter 隨機台詞
→ **說明文件**：[SPEC-dialogue-quests.md § banter](SPEC-dialogue-quests.md#banter--proactive-unprompted-npc-lines)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Dialogue.cs` | `BanterSpec`（含 emotion / chance）|
| Build P1 | `Generator.Build.Banter.cs` | 建 ambient banter topic + random INFO |
| Build P2 | `Generator.Build.Conditions.cs` | banter condition 接線 |

---

## Scene 多人場景
→ **說明文件**：[SPEC-dialogue-quests.md § scenes](SPEC-dialogue-quests.md#scenes--two-npcs-talking-to-each-other-scen)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Scene.cs` | `SceneSpec`, `SceneActorSpec`, `SceneAutoStartSpec`（在場偵測調參 + **重播策略 playOnce/playHour/playHourTolerance/gateGlobal**）, **`SceneActionSpec`（§1b 非對話 beat：**`idle` 播動畫**/package 走位/timer 停頓 + phase 窗）**, **`ScenePhaseSpec`（含 `HeadtrackActor`/`HeadtrackPlayer`/`FaceTarget` per-phase gaze 控制 + `StartConditions`/`CompletionConditions` per-phase CTDA 閘）**, **`SceneSpec.Conditions`（scene-level CTDA：整個 scene 通過才啟動）**（從 `Spec.Dialogue.cs` 拆出；共用其 `ConditionSpec`）|
| Build P1 | `Generator.Build.Scene.cs` | 建 SCEN：alias 綁定、參與者、phase + dialogue actions；**per-phase headtrack/facing（HeadtrackPlayer flag／HeadtrackActorID／FaceTarget；預設＝看另一 actor，行為不變）；lineless phase → beat phase（無 topic/Dialog action）；`actions[]` → Package（Type=Package，PACK ref pass 2 解析）/ Timer（Type=Timer + TimerSeconds）action；**`idle` action → 發一個 **Timer** SceneAction（hold = `timerSeconds` 或 `DefaultIdleHoldSeconds` 2s）讓該 phase 有 action 能 run（否則引擎不跑空 phase、OnStart fragment 不觸發——vanilla 每個 fragment phase 都帶 Timer），動畫本體走 SceneAdapter phase fragment**；autoStart → `AttachSceneController` 把 `MFSceneBanterController` 掛上 host quest 並清 BeginOnQuestStart；重播策略 props（playOnce/playHour/tol 直填、gateGlobal object prop pass 2 解析）** |
| Gen（PlayIdle）| `Generator.SceneFragments.cs` | scene phase-fragment **純產生器**（第三種 fragment 家族，鏡像 `GenerateQuestFragmentSource`）：`SceneNeedsFragmentScript`/`SceneFragmentScriptName`/`SceneIdleActions`/`GenerateSceneFragmentSource`；產 `SF_<scene> extends Scene Hidden`，每 idle phase 一個 `Fragment_<phase>()` 跑 `Actor_<p>.GetActorRef().PlayIdle(Idle_<p>)`（取 actor 用 `GetActorRef()`，非 `GetActorReference()`，Task 0 spike 釘死）|
| Build P2 | `Generator.Build.Scripts.cs` `AttachSceneFragments` | 掛 SCEN `SceneAdapter` VMAD（鏡像 `WireQuestStages` gating：僅當 `SF_<scene>.pex` 在 `CompiledScriptsDir`）：每 idle phase 一個 `ScenePhaseFragment{Index=(byte)phase, Flags=OnStart, FragmentName="Fragment_<phase>"}` + 綁 `Idle_<p>`（object prop→IDLE）與 `Actor_<p>`（object prop→host quest，`Alias=actor index`，同 StoryManager `qfa.Property`）|
| Wire P2 | `Generator.Build.Scene.cs` `WireScenes` | actor alias→UniqueActor；**Package action 的 PACK ref → `action.Packages`（sceneActionWires）；controller GateGlobal → GLOB object prop（sceneGateWires）；scene-level + per-phase conditions via 共用 `BuildCondition`（sceneConditionWires，phaseMap 對齊 spec-phase→built ScenePhase）** |
| Const | `Generator.QuestFragments.cs` | `SceneBanterController` scriptname 常數 |
| Asset | `assets/papyrus/MFSceneBanterController.psc` | 可複用在場偵測 controller（extends Quest，鏈式 RegisterForSingleUpdate → Scene.Start()）；`brawlOnEnd` 偵測 scene 結束 → `StartBrawl()` 雙向 StartCombat；**重播閘門 `playOnce`（播完停 poll）/ `playHour`+tol（CurrentHour/HourDistance 時辰窗）/ `Gate` GLOB（GetValue 擋、播完 SetValue(1)）**；改 .psc 要重編 .pex（native `~/tools/papyrus-compiler` + `MODFORGE_PAPYRUS_HEADERS` 指向 cache）；embed 進 CLI |
| Validate | `Generator.Validate.Quests.cs` | actor alias ref、scene↔quest 連結；**beat phase 需有 action 覆蓋；每 action：idle⊕package/package⊕timer 互斥、idle+timer(hold) 合法、至少一個 + actor 是 scene actor + phase 窗在範圍內（idle ref CheckRef）**；**autoStart 需 StartGameEnabled host quest + ≥2 actor + 正數調參；playHour 0..24、tol>0、gateGlobal CheckRef** |
| Package | `src/ModForge.Cli/Package.cs` | 任一 scene 有 autoStart → 出貨 `MFSceneBanterController.pex`；**每 scene 有 idle action → 編 `SF_<scene>.psc`（`GenerateSceneFragmentSource`）並 build 時掛 SceneAdapter VMAD** |
| Diag | `Diagnostics.Scene.cs` | `scenediag` actors / phases / actions dump；**`scnscan` 列舉含非對話 action 的 scene（解 §1b 演出來源）** |

---

## 身份系統（輕量職業/Identity）
→ **設計**：`docs/superpowers/specs/2026-06-06-identity-system-design.md`；**plan**：`docs/superpowers/plans/2026-06-07-identity-system-mvp.md`

三面向：Acquire（讀書 OnRead）/ Gate（identity·primaryIdentity 標籤 → GetInFaction CTDA）/ Grant（常駐 ability 加/收）。身份狀態存成 FACT（持久訊號，未來原版 GetInFaction 可 gate）。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Identity.cs` | `IdentitySpec`（id/faction/priority/grants/toggle/default/acquireBook/acquireText）+ `IdentityAcquireSpec`（onAcquire.scene）；`ModSpec.Identities` |
| Spec | `Spec.Dialogue.cs` | `DialogueSpec.Identity` / `PrimaryIdentity`（招呼/對話行身份閘標籤）|
| Build P1 | `Generator.Build.Identity.cs` `BuildIdentities` | 每身份建一個持有 FACT（bare editorId 才建；外部/已宣告跳過）|
| Build P2 | `Generator.Build.Identity.cs` `ExpandIdentityConditions` | identity→`GetInFaction(player,faction)≥1`；primaryIdentity 再對每個更高 priority 身份補 `==0` 排除；由 `Generator.Build.Conditions.cs` `WireDialogueConditions` 呼叫併入 INFO CTDA。**Phase-2 #2 `activeWhen`**：身份的 `ActiveWhen` 情境 CTDA 接在**正向**閘後（窄化「active」）；`OnPlayerByDefault` helper 把每條 runOn 預設 pin 到玩家（author 設非-Subject runOn 才尊重）；**排除項不含 activeWhen**（否定條件束 CTDA 表達不了——held-but-inactive 高身份仍以 faction 訊號排掉低 primaryIdentity，會掉回 plain 招呼，gap 留給 #4 controller，in-game 2026-06-07 確認）。**Phase-2 #4 controller**：primaryIdentity **不再**做 faction-exclusion，改 `GetInFaction(self)≥1` + `GetGlobalValue(MF_PrimaryIdentity)==code`（code=1-based spec index，`Generator.IdentityCode`）——主身份由 controller 算（單一真相源、可被覆寫）|
| Build P2 | `Generator.Build.Identity.cs` `AttachIdentityBooks` | 把 `MFIdentityBook` VMAD 掛上 acquireBook + 綁 `TheFaction`/`GrantAbility`(grants[0])/**`GrantPerk`(grantPerks[0])**/`AcquireScene`/`Toggle`（無條件掛，鏡像 controller；prebuilt .pex 出貨）|
| Build P2 | `Generator.Build.Identity.cs` `BuildDefaultIdentityQuest` | **Phase-2 #1**：任一身份 `default:true` → 建 `MF_IdentityDefaultQuest`（StartGameEnabled）掛 `MFIdentityDefault` VMAD，用 `ScriptObjectListProperty` 綁 `Factions[]`（所有 default 身份的 faction）/`Grants[]`（其 grants，去重；無則省略）/**`Perks[]`（grantPerks，去重）**；無 default 不建。`ObjListProp` helper 建 list property|
| Build P2 | `Generator.Build.Identity.cs` `BuildIdentityAutoGrantQuest` | **龍裔首吼/autoGrantWhen**：任一身份有 `autoGrantWhen{actorValue,threshold}` → 建 `MF_IdentityAutoGrantQuest`（StartGameEnabled）掛 `MFIdentityAutoGrant` VMAD，平行 `Factions[]`/`AvNames[]`(`StrListProp`)/`Thresholds[]`(`FloatListProp`)；只授 faction 訊號；無 autoGrant 不建 |
| Build P1/P2 | `Generator.Build.Identity.cs` `BuildIdentityGlobals` / `BuildIdentityControllerQuest` | **Phase-2 #4 controller**：dialogue 用 primaryIdentity 或 setPrimaryIdentity 時（`IdentityControllerNeeded`）→ P1 建兩個 GLOB `MF_PrimaryIdentity`(controller 寫/招呼讀)+`MF_IdentityOverride`(對話寫/controller 讀)（author 同名則尊重）；P2 建 `MF_IdentityControllerQuest`（StartGameEnabled）掛 `MFIdentityController` VMAD，綁 Primary/Override GLOB + `Factions[]`(priority DESC)/`Codes[]`(`IntListProp`，code=1-based spec index)。`Generator.IdentityCode(spec,id)` 解 code（auto/空=0、未知=-1）|
| Build P2 | `Generator.Build.Scripts.cs` `AttachDialogueResultScripts` | INFO OnEnd result fragment：user `ResultScript` 優先，否則 `needsAutoTif`（setStage / setPrimaryIdentity / **openBarter / rewardItem / evaluateSpeakerPackages**，.pex 在才掛）→ 生成 TIF；綁 `OwningQuest`(setStage) + `MF_IdentityOverride` GLOB(setPrimaryIdentity) + `RewardItem` form(rewardItem)|
| Gen | `Generator.QuestFragments.cs` `GenerateDialogueFragmentSource(d, overrideCode)` | **多動作 TIF**：setStage→`OwningQuest.SetStage(N)`；overrideCode≥0→`MF_IdentityOverride.SetValue(code)`；**rewardItem→`Game.GetPlayer().AddItem(RewardItem, count)`；openBarter→`(akSpeakerRef as Actor).ShowBarterMenu()`；evaluateSpeakerPackages→`.EvaluatePackage()`**（後二共用 `__spk` cast）。`DialogueFragmentScriptName` 任一 result 動作 → `TIF_<ed>`|
| Asset | `assets/papyrus/MFIdentityBook.psc` | 可複用身份書（**extends ObjectReference**，OnRead → AddToFaction+AddSpell+**AddPerk(GrantPerk)**+Scene.Start；Toggle 反向移除）；**鐵律：OnRead 是 ObjectReference 的 event，不是 Book 的——`extends Book` 永遠收不到 OnRead（Book/ObjectReference 都 extends Form、是兄弟）；綁在 BOOK base form 上、背包讀也會 fire（in-game 2026-06-07 確認）**；改 .psc 要重編 .pex；embed 進 CLI（條件式 EmbeddedResource）|
| Asset | `assets/papyrus/MFIdentityDefault.psc` | 預設身份授予（**extends Quest**，OnInit 遍歷 `Factions[]` AddToFaction + `Grants[]` AddSpell + **`Perks[]` AddPerk**，idempotent 跳過已持有）；StartGameEnabled host quest 開局/載入觸發（進 `.seq` 故舊存檔也跑，in-game 2026-06-07 確認）；改 .psc 要重編 .pex；embed 進 CLI（條件式 EmbeddedResource）|
| Asset | `assets/papyrus/MFIdentityController.psc` | 主身份 controller（**extends Quest**，OnInit + `RegisterForSingleUpdate(3.0)` poll → `Recompute`：Override(若持有)否則 priority 最高持有 → 寫 `Primary` GLOB）；Factions[]/Codes[] 平行 priority DESC（in-game 2026-06-07 確認）；改 .psc 要重編 .pex；embed 進 CLI（條件式 EmbeddedResource）|
| Asset | `assets/papyrus/MFIdentityAutoGrant.psc` | 自動授予 trigger（**extends Quest**，OnInit + 5s poll → `Check`：`p.GetActorValue(AvNames[i]) >= Thresholds[i] && !IsInFaction → AddToFaction`，純 Papyrus 讀 AV、免 SKSE/事件 hook）；Factions[]/AvNames[]/Thresholds[] 平行；只授 faction；改 .psc 要重編 .pex；embed 進 CLI（條件式）|
| Validate | `Generator.Validate.cs` | `RegisterIdentityFactions`（早登錄自建 FACT editorId 供 condition 解析）+ `ValidateIdentities`（unique id、非空 faction、grants/**grantPerks**/acquireBook/**activeWhen param** CheckRef、**autoGrantWhen actorValue 非空**）|
| Validate | `Generator.Validate.Quests.cs` | dialogue 規則；**`hello:true` 招呼免 prompt**（招呼非玩家選項；其餘 dialogue 仍須 prompt）；**`setPrimaryIdentity` 須是已知 id 或 `auto`**；**`rewardItem` CheckRef**|
| Package | `src/ModForge.Cli/Package.cs` §5d–§5g | §5d acquireBook → `MFIdentityBook.pex`；§5e `default:true` → `MFIdentityDefault.pex`；§5f primaryIdentity/setPrimaryIdentity → `MFIdentityController.pex`；§5g `autoGrantWhen` → `MFIdentityAutoGrant.pex`（皆進 Scripts/）；dialogue fragment 編譯傳 `IdentityCode` 給 `GenerateDialogueFragmentSource`|

**Phase-2/C 進度**：✅ #1 Adventurer 預設自動授予、✅ #2 `activeWhen` 情境條件、✅ #4 controller 主身份+手動覆寫、✅ #5 身份對應互動 **#5a 商人交易 UI（`openBarter`）+ #5b 護衛/跟隨任務（`identity`-gated escort quest，follow PACK gated on GetStage、`rewardItem`/`evaluateSpeakerPackages` TIF）+ #5c 聖騎士 smite 細調（`grantPerks` → `MF_SmiteEvilPerk` ModAttackDamage ×1.25 vs undead/daedra）+ 龍裔首吼（`autoGrantWhen` → MFIdentityAutoGrant，DragonSouls≥1）**（皆 in-game 確認 2026-06-07）。**未做**：#3 聲望/行為追蹤。

---

## Conditions（CTDA 條件）
→ **說明文件**：[SPEC-dialogue-quests.md § conditions](SPEC-dialogue-quests.md#conditions--ctda-gates-on-a-dialogue-info-a-banter-info-or-a-package)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Build P2 | `Generator.Build.Conditions.cs` | 所有 CTDA 的 function dispatch + ref 解析（dialogue / stage / banter / package 共用）|
| Validate | `Generator.Validate.Helpers.cs` | `CheckCondition`（function / comparator / ref）|
| Diag | `Diagnostics.Dialogue.cs` | condition 欄位 dump |

---

## Story Manager 事件觸發
→ **說明文件**：[SPEC-dialogue-quests.md § Story Manager](SPEC-dialogue-quests.md#story-manager-quests--event-driven-start) · [for_agent.md § 限制](for_agent.md#limits--be-honest-do-not-over-claim)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.StoryManager.cs` | `QuestStoryEventSpec`（event + conditions）、`AliasSpec`（fill 模式：fromEvent/forced/uniqueActor/createObject/findMatching；findMatching 帶 `Conditions`；alias 腳本 `Script`/`ScriptSource`/`ScriptProperties` = OnActivate 等）|
| Data | `StoryManagerEvents.cs` | 事件登錄表：KillActor/ChangeLocation/CastMagic/AddItem/Assault/CraftItem/PlayerRemoveItem/Arrest/IncreaseLevel/ScriptEvent — FormKey + 槽名；`TryParseFill` / `TryParseCreateObject`（`<ref>@<alias>`）|
| Build P2 | `Generator.Build.StoryManager.cs` | SMBN→SMQN 掛原版事件根；keyword 過濾條件（GetEventData/GetIsID）；**`BuildQuestAliases(quest,qs,def?)`** 共用 helper 建所有 alias fill（fromEvent 僅 `def!=null` 時；createObject = `CreateReferenceToObject` 在 `aliasIdByName` 目標 alias 處生成；findMatching = `QuestAlias.Flag.MatchingRefInLoadedArea`[+`MatchingRefClosest`] + alias.Conditions；alias 腳本 `AttachAliasScript` = `QuestAdapter.Aliases` 加 `QuestFragmentAlias`[v5/objFmt2、綁 alias ID、flag=Local]）；**`BuildStandaloneQuestAliases()`** 替非 storyEvent quest 建 alias（def=null，跳 fromEvent）|
| Validate | `Generator.Validate.StoryManager.cs` | 事件名合法；**`ValidateQuestAlias(q,a,def?,…)`** 共用（storyEvent 與非 storyEvent quest 都驗 alias fill/ref/script；def=null 時 fromEvent 報錯）；slot 名稱、ScriptEvent 需宣告 keyword |
| Diag | `Diagnostics.StoryManager.cs` | smtree（事件根列舉）/ SMBN alias fill / event-data slot dump |

### 支援事件與槽

| 事件 | R1 | R2 | L1 |
|-----|----|----|-----|
| KillActor | victim | killer | location |
| ChangeLocation | actor | — | newLocation |
| CastMagic | caster | target | location |
| AddItem | actor | — | location |
| Assault | victim | assailant | location |
| CraftItem | workbench | — | — |
| PlayerRemoveItem | owner | item | — |
| Arrest | guard | criminal | — |
| IncreaseLevel | — | — | — |
| ScriptEvent | ref1 | ref2 | loc |

### SM 鐵律
- 一事件根 → 一共用分支 → 多 quest node（串 PreviousSibling）
- 事件根下多分支互斥（引擎只跑一條）
- 引擎一事件只啟動最先符合的 quest（radiant 正確行為）
- Location 型 alias 必須 `Type=Location`（fromEvent 'L' 開頭自動設）
- 任一必填 alias 填不上 → quest 靜默不啟動

---

## ScriptEvent 自訂觸發
→ **說明文件**：[SPEC-dialogue-quests.md § ScriptEvent](SPEC-dialogue-quests.md#scriptevent--sending-your-own-story-events)

同上 Story Manager 的源碼層（共用 `StoryManagerEvents.cs` / `Generator.Build.StoryManager.cs` / `Generator.Validate.StoryManager.cs`）。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| **源碼** | `assets/papyrus/MFStoryEventDispatch.psc` | 通用派發器源碼（`Fire(kw,ref1,ref2,loc)→kw.SendStoryEvent`）；csproj embed `.pex`（夾帶進 Scripts/）+ `.psc`（user script 編譯時當 header）|
| Example | `examples/story-manager-scriptevent.json` | ScriptEvent quest 完整範例（OnInit 測試觸發）|
| Example | `examples/MFSE_TestTrigger.psc` | OnInit 觸發 ScriptEvent 的測試腳本 |
**可複用 trigger 庫 — 四入口全數實機驗證通過（2026-06-05）**：
| Example | `examples/story-manager-magictrigger.json` + `MFSE_SpellTrigger.psc` | magic effect 觸發：`extends ActiveMagicEffect`，`OnEffectStart→Fire`（玩家施法）；spell + potion 共用 ✅ |
| Example | `examples/story-manager-potiontrigger.json` | potion 觸發:複用 MFSE_SpellTrigger 證明 trigger 與 delivery 無關（喝藥水）✅ |
| Example | `examples/story-manager-activatortrigger.json` + `MFSE_ActivatorTrigger.psc` | activator 觸發：`extends ObjectReference`，`OnActivate→Fire`（拉桿；model 必須是驗證存在的 vanilla nif）✅ |
| Example | `examples/story-manager-dialoguetrigger.json` + `MFSE_DialogueTrigger.psc` | NPC 對話觸發:`extends TopicInfo`，`Fragment_0→Fire`（result-script VMAD；複用 proven dialogue[]+placed-NPC）✅ |
| Example | `examples/story-manager-aliastrigger.json` + `MFSE_AliasActivate.psc` | alias OnActivate:`extends ReferenceAlias`，`OnActivate→Fire`（腳本掛 alias[].script，跟著填進 alias 的 ref 走→可接 createObject/findMatching 的執行時 ref）✅ |

⚠️ ScriptEvent 介面或槽位有任何改動，`MFStoryEventDispatch.psc` 必須同步重編（`.pex`）。
**派發器 header 機制**：呼叫 `Fire()` 的 user trigger 腳本，Package.cs 編譯時把 embed 的 dispatcher `.psc` 解到 temp 目錄當 sibling header（compiler 把 input 檔所在目錄當 header dir），免 per-machine cache 安裝。

---

## Papyrus 腳本附加
→ **說明文件**：[SPEC-dialogue-quests.md § scripts](SPEC-dialogue-quests.md#scripts--papyrus-attachment)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Dialogue.cs` | `ScriptAttachSpec`（scriptName / properties）|
| Build P2 | `Generator.Build.Scripts.cs` | 腳本附加到任意 record（NPC/object/quest/cell），property 綁定 + .pex 載入 |
| Validate | `Generator.Validate.Quests.cs` | script ref 存在、property target 存在 |

---

## Word Wall 喊聲壁

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.WordWall.cs` | `WordWallSpec`, `WordWallTriggerSpec` |
| Build | `Generator.WordWall.cs` | 教字 quest fragment（AddShout/TeachWord + property 綁定）|
| Build P1 | `Generator.Build.Classes.cs` | `BuildWordWallQuests` 入口 |
