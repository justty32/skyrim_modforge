# CODE_MAP — 對話・任務・Story Manager・腳本

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：quest + stages + objectives、dialogue topics + INFO、banter、multi-actor scenes、CTDA conditions、Story Manager event quests、ScriptEvent、word walls、Papyrus 附加。

> **拆檔（2026-06-21，行為不變、partial class / DTO relocation，796 測綠）**：五個超 300 行的檔各拆出一個 sibling——
> ① `Spec.Dialogue.cs` → **`Spec.Quests.cs`**（QuestSpec/SpawnSpec/StageSpec/InstanceGlobalSpec/GlobalWriteSpec/ObjectiveSpec/ObjectiveTargetSpec；StorageWriteSpec/DialogueVariantSpec 留 Dialogue）。
> ② `Generator.QuestFragments.cs` → **`Generator.DialogueFragments.cs`**（`GenerateDialogueFragmentSource`/`DialogueFragmentScriptName`/`IdentityCode` + 可重用 controller scriptname 常數 SceneBanterController/EncounterCooldownScript/DynamicSpawnScript/IdentityController + identity globals）。
> ③ `Generator.Build.Conditions.cs` → **`Generator.Build.Conditions.Wire.cs`**（`WireDialogueConditions`/`WirePackageConditions`；`BuildCondition` 仍在原檔）。
> ④ `Generator.Validate.Quests.cs` → **`Generator.Validate.Quests.Helpers.cs`**（共用驗證 helper：`ValidateSceneCondition`/`ValidatePersistBlock`/`ValidateSyncPerksBlock`/`ValidateGate`/`ValidatePersistKey`/`ValidateStorageWrites`/`ValidateScriptAttachments`/`CheckScriptProps`；主方法 `ValidateQuestsAndDialogue` 仍在原檔）。
> ⑤ `Generator.Build.StoryManager.cs` → **`Generator.Build.QuestAliases.cs`**（`BuildQuestAliases`/`BuildStandaloneQuestAliases`/`AttachAliasScript`；`BuildStoryManager` 仍在原檔）。
> 都是同一 partial class 跨檔，成員位置不影響行為。下方各 row 已指向方法所在的新檔。

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
| `examples/gather_quest_spec.json` | **#9 instanceGlobals：gather 型 radiant quest，startUpStage fragment 隨機 SetValue + UpdateCurrentInstanceGlobal 綁 instance，objective `<Global=...>` 顯示 per-instance 計數** |
| `examples/word_wall_spec.json` | word wall 觸發教字 |
| `examples/story-manager-kill.json` | KillActor SM 事件 |
| `examples/story-manager-assault.json` | Assault SM 事件 |
| `examples/story-manager-changelocation.json` | ChangeLocation SM 事件 |
| `examples/location_encounter_spec.json` | **#5+#6 地點感知遭遇：ChangeLocation + `locationFilter`（LocType OR'd）+ `cooldownHours`（EE_WITimeout）** |
| `examples/story-manager-craftitem.json` | CraftItem SM 事件（玩家製作物品；engine-native）|
| `examples/story-manager-events-demo.json` | 三個 engine-native 事件合一測試（CraftItem/PlayerRemoveItem/Arrest）|
| `examples/story-manager-events-demo2.json` | IncreaseLevel SM 事件（玩家升級；engine-native，無 ref 槽）|
| `examples/story-manager-uniqueactor.json` | uniqueActor alias fill |
| `examples/story-manager-createobject.json` | createObject alias fill（事件觸發→在另一 alias 處生成物件；複用 magic trigger）|
| `examples/story-manager-findmatching.json` | findMatching alias fill（loaded area 裏找最近的符合 conditions 的既有 ref；複用 magic trigger）|
| `examples/radiant_alias_spec.json` | **radiant alias 鏈：findMatchingLocation（#7 Hold→Dungeon）+ findInLocationAlias（#8 Dungeon 內 BossChest）；LocType/LCRT FormID 為 placeholder（待 `gamedata find`）** |
| `examples/story-manager-aliastrigger.json` + `MFSE_AliasActivate.psc` | alias OnActivate（腳本掛 quest alias→活化執行時 createObject 生成的 ref→Fire 鏈下個事件；複用 magic trigger + createObject）|
| `examples/story-manager-queststage.json` + `MFSE_AdvanceStage.psc` | journal 進度：`startUpStage` 啟動即顯示 objective + alias OnActivate `SetStage` 完成並關閉 quest（startUpStage + alias 腳本 + stage fragment 三者共存）|
| `examples/quest-alias-standalone.json` | 一般（非 storyEvent / StartGameEnabled）quest 帶 alias：forced player + createObject 生箱 + alias OnActivate（`BuildStandaloneQuestAliases`）|
| `examples/story-manager-scriptevent.json` | ScriptEvent 自訂觸發 |
| `examples/MFSE_TestTrigger.psc` | ScriptEvent OnInit 觸發腳本 |
| `examples/story-manager-magictrigger.json` | ScriptEvent 經 magic effect 觸發（玩家施法→啟動 quest）|
| `examples/MFSE_SpellTrigger.psc` | 可複用 magic-effect trigger（OnEffectStart→Fire）；spell + potion 共用 |
| `examples/skill_cast_spec.json` | **Idea #20 技能樹「施法練功」IN-GAME CONFIRMED 2026-06-20**：自訂法術→MFSE_SpellTrigger→MFStoryEventDispatch.Fire→SM ScriptEvent→quest `OnStoryScript` 跑 JFormDB persist+syncPerks（player-keyed）+ 好感度 gate；取代不觸發的 CastMagic 路徑（`npc_skill_persist_spec.json` 僅留作 persist 結構參考）|
| `examples/storage_writes_spec.json` | **J組 storageWrites IN-GAME CONFIRMED**：CastMagic SM quest 的 stage→`OnStoryCastMagic` 寫 StorageUtil int/float/str（player-keyed）；diag 隔離變體 `storage_writes_diag`(setstage)/`storage_writes_spawn_diag`(疊 spawn，實機驗收用)/`storage_writes_esl_diag`(masterless root-cause) + `skill_persist_diag`(JFormDB setstage 隔離) |
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
| `InstanceGlobalTests.cs` | **#9 instanceGlobals：fragment source（GLOB property 宣告、RandomInt/SetValue/Update、bind-only、與 objective 共用 stage fragment）+ VMAD GLOB object-property 綁定（fake .pex）+ validate（空 global、單邊 random、random+value 衝突、min>max）** |
| `ObjectiveTargetTests.cs` | objective QSTA target：QOBJ `QuestObjectiveTarget`（AliasID + CompassMarkerIgnoresLocks flag + CTDA）+ validate（target alias 須在同 quest）|
| `SceneTests.cs` | SCEN actor / phase / dialogue action；非對話 action（Package→Packages PACK ref / Timer→TimerSeconds）+ beat phase（無 lines→無 Dialog action/topic）+ LastActionIndex；**idle action 發 Timer（hold；純 build 無 VMAD）**；autoStart → controller VMAD 掛接 + 清 BeginOnQuestStart + 調參 props + **重播策略 props（playOnce/playHour/gateGlobal→GLOB object prop）** + validate gate |
| `SceneFragmentTests.cs` | PlayIdle 純產生器（`SceneNeedsFragmentScript`/`SceneFragmentScriptName`/`GenerateSceneFragmentSource`：extends Scene Hidden、`Fragment_<phase>`、`GetActorRef()`）+ `AttachSceneFragments`（.pex 在才掛 SceneAdapter、PhaseFragments 數/ScriptName/OnStart flag/FragmentName、Actor_ object prop→host quest+alias index）+ validate（idle-only OK、idle+timer hold OK、idle+package 拒）|
| `IdentityTests.cs` | 身份系統：`IdentitySpec` 預設、每身份建 FACT（外部/已宣告不重建）、validate（dup id / bad grant）、acquireBook → `MFIdentityBook` VMAD + 屬性綁定；**default → `MF_IdentityDefaultQuest`（StartGameEnabled）+ `MFIdentityDefault` VMAD，Factions[]/Grants[] list property（只收 default、grant 去重）、無 default 不建、無 grant 省 list**；**activeWhen 窄化正向閘且跑玩家、不污染高優先序排除**；**controller：primaryIdentity 建 MF_PrimaryIdentity/MF_IdentityOverride GLOB + `MF_IdentityControllerQuest`（Codes[]、無 default 不建 granter）、global-based primary CTDA、setPrimaryIdentity TIF**；**grantPerks → 書綁 GrantPerk[0] + default quest 綁 Perks[]**；**autoGrantWhen → `MF_IdentityAutoGrantQuest` + Factions[]/AvNames[]/Thresholds[] 平行、無 autoGrant 不建** |
| `StoryManagerBuildTests.cs` | SM build pass 2（SMBN/SMQN 掛接、alias fill 接線、alias 腳本 VMAD 掛接、`startUpStage` QSDT flag、stage fragment + alias 腳本共存於單一 adapter、非 storyEvent quest 也建 forced/createObject alias + 腳本）|
| `RadiantAliasTests.cs` | **#7 findMatchingLocation（Type=Location、StoresText、`LocationHasKeyword` CTDA [+父時 `GetInCurrentLocAlias`]、無 Location 子記錄）+ #8 findInLocationAlias（Type=Reference、Location.AliasID/RefType、conditions）build + validate（未知 keyword/parent/location alias、自指、缺 refType+conditions）** |
| `StoryManagerEventsTests.cs` | 事件登錄表欄位（FormKey / slot 對應）|
| `StoryManagerEventsMoreTests.cs` | 擴充事件（ChangeLocation/CastMagic/AddItem/Assault/ScriptEvent）|
| `EncounterRoutingTests.cs` | **#5 locationFilter（GetKeywordDataForCurrentLocation、OR 群組）+ #6 cooldownHours（LastFired GLOB + MFEncounterCooldown script + props）+ LocAliasHasKeyword condition + validate（負 cooldown、空 keyword）** |
| `DynamicSpawnTests.cs` | **#3 quest.spawn → MFDynamicSpawn script（SpawnForm/Count/Min/MaxDistance/SnapToNavmesh props）+ 與 cooldown/locationFilter 共存單一 adapter + validate（空 form、count<1、min>max）** |
| `StoryManagerValidateTests.cs` | SM validate（事件名、alias 語法、keyword 要求）|
| `WordWallTests.cs` | word wall 教字 quest fragment（⚠️ 需本機 Skyrim.esm）|

---

---

## Classes（職業 CLAS）
→ **說明文件**：[SPEC-dialogue.md § classes](../../../docs/spec/SPEC-dialogue.md#classes-clas)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Magic.cs` | `ClassSpec`（attribute weights / skill training）|
| Build P1 | `Generator.Build.Classes.cs` | 建 Class record |
| Validate | `Generator.Validate.Npcs.cs` | class ref 檢查（npc → class）|

---

## Quest 基礎（stages / objectives）
→ **說明文件**：[SPEC-quests.md § Quest stages](../../../docs/spec/SPEC-quests.md#quest-stages-log-entries--objective-wiring)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Quests.cs` | `QuestSpec`（含 **`spawn` = F組 #3 dynamic spawn**）, **`SpawnSpec`（form/count/min/maxDistance/snapToNavmesh）**, `StageSpec`（含 `startUpStage` = QSDT 起始 stage flag、**`instanceGlobals[]` = UpdateCurrentInstanceGlobal 綁定**、**`globalWrites[]` = K組 plain `<global>.SetValue(value)`（非 instance bind；SM quest 路由到 OnStory handler）**、**`persist`/`syncPerks` = stage-fragment JFormDB（Idea #20 Phase 0，key 須 player/ref，無 speaker）**、**`storageWrites[]` = J組 stage StorageUtil KV（target 須 player/none）**）, **`InstanceGlobalSpec`（global + 可選 randomMin/Max 或 value）**, **`GlobalWriteSpec`（global + value）**, **`StorageWriteSpec`（key + target〔speaker/player/none〕 + int/float/str + delta）= J組**, **`DialogueVariantSpec`（responses + 自有 conditions + 可選 emotion/emotionValue/sayOnce）= M組 INFO 批次**, `ObjectiveSpec`（含 `targets[]`）, **`ObjectiveTargetSpec`（alias 名 + `compassIgnoresLocks` + `conditions[]` → QSTA）** |
| Build P1 | `Generator.Build.Actors.cs` `BuildQuests` | 建 Quest record + QSDT stages（log/complete/fail flag、`startUpStage`→`QuestStage.Flag.StartUpStage`）+ QOBJ objectives |
| Build P1 | `Generator.Build.Dialogue.cs` | dialogue Branch + Topic + INFO；greeting 自動生成 |
| Build P2 | `Generator.Build.QuestStages.cs` | stage log-entry CTDA + objective fragment VMAD（**合併**進既有 QuestAdapter，不覆寫 alias 腳本的 `.Aliases`）；**instanceGlobals → 在 ScriptEntry 綁 GLOB `ScriptObjectProperty`（每 global 一個，prop 名 `InstanceGlobalProperty`）+ 該 stage 的 `QuestScriptFragment`（即使無 objective）** |
| Build P2 | `Generator.Build.ObjectiveTargets.cs` | **`WireObjectiveTargets`**：alias 名→alias index → QOBJ `QuestObjectiveTarget`（QSTA：AliasID + `Quest.TargetFlag.CompassMarkerIgnoresLocks` + per-target CTDA via `BuildCondition`）；在 alias pass 之後跑。**`WireDeferredForcedAliases`**：解析「target 晚於 alias pass 才 build」的 `forced:` alias（placement/xmarker/mapMarker），在 BuildPlacements/BuildMapMarkers 之後跑 |
| Build P2 | `Generator.QuestFragments.cs` | 自動生 `<quest>_Stages` Papyrus fragment：SetObjectiveDisplayed/Completed + **instanceGlobals（`GlobalVariable Property` 宣告 + `<g>.SetValue(Utility.RandomInt/值)` + `UpdateCurrentInstanceGlobal(<g>)`）**；`QuestNeedsFragmentScript`/`InstanceGlobalProperty` |
| Validate | `Generator.Validate.Quests.cs` | stage index 唯一/遞增、`startUpStage` 至多一個、objective↔stage 連結、**objective target alias 須是同 quest 的 alias**、script ref 存在；**scene action：idle⊕package、package⊕timer 互斥（idle+timerSeconds=pose hold 合法）、至少一個（idle ref 檢查）** |
| Diag | `Diagnostics.Quests.cs` | stages / objectives / aliases / VMAD 腳本 dump |
| Diag | `Diagnostics.Dump.Quest.cs` | quest + scene 結構化完整 dump |

---

## Dialogue 對話樹
→ **說明文件**：[SPEC-dialogue.md § dialogue](../../../docs/spec/SPEC-dialogue.md#dialogue)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Dialogue.cs` | `DialogueSpec`, `DialogueInfoSpec`；**`hello`（bool）= 這行是 NPC 自動招呼(Misc/Hello)而非玩家選單 topic** |
| Build P1 | `Generator.Build.Dialogue.cs` | Branch / Topic / INFO 建立；player-topic 優先度管理；**`hello:true` 招呼：同一 (NPC,quest) 的所有招呼合進 **一個** Hello topic 的多條 INFO(conditioned 在前、plain `greeting` 墊底)——**鐵律:招呼靠 INFO 順序選取(第一個條件符合的勝出),不是多個獨立 Hello topic 靠 priority 競爭(那樣引擎只取一個 topic、其他無視);vanilla 237/297 Hello topic 多 INFO**(in-game 2026-06-07 確認身份招呼)|
| Build P1 | `Generator.Build.Dialogue.cs` `DialogueInfoFlags` | DialogueSpec → INFO (ENAM) flags 合併（`goodbye`/`sayOnce`/`walkAway`/`random`/`invisibleContinue`/`forceSubtitle`）；hello 與 player-topic 兩個建立點共用 |
| Build P1 | `Generator.Build.Dialogue.cs` | **M組 `variants[]` INFO 批次**：一個 dialogue entry 在**同一 topic** 下掛多條 sibling INFO（各帶 `Random` flag→引擎在條件符合的 sibling 隨機選；共用 `AddSpeakerGate` local）；`DialogueVariantId(ed,i)`=`<ed>_v<i>`（record EditorID + `dialogResponsesByEd` key，pass1/pass2 一致）；parent INFO 僅在 `responses` 非空或無 variants 時建（純批次 header→不發 parent）；variant 自有 emotion/emotionValue 缺省繼承 parent |
| Build P1+P2 | `Generator.Build.Dialogue.cs` | **對話樹**：`topLevel:false` → branch 非 TopLevel（sub-topic，只在被 link 時出現）；topic 註冊進 `dialogTopicsByEd`（topic/INFO 同 editorId，formKeyByEd 會撞，需專屬 map）。pass-2 `WireDialogueLinks`：`linkTo`→INFO `LinkTo`(ENAM，指 target dialogue 的 **topic** 或 vanilla ref)、`previousDialog`→INFO `PreviousDialog`(PNAM，指 target **INFO**)|
| Build P2 | `Generator.Build.Conditions.Wire.cs` `WireDialogueConditions` | INFO CTDA 條件接線；**M組 `useConditionTemplates`：把 `ModSpec.ConditionTemplates`（`ConditionTemplateSpec` name+conditions）依名展開到 INFO（inline conditions 之後、同 BuildCondition 路徑、alias-aware）**；**M組 `variants`：`ApplyShared` local（inline conditions + templates + identity）套到 parent INFO **與每條 variant INFO**，variant 再接自有 `conditions`（用 `DialogueVariantId` 查 INFO）；setStage 自動閘僅 parent** |
| Validate | `Generator.Validate.Quests.cs` | speaker NPC ref、quest ref、condition function；**conditionTemplate name 唯一/非空 + 每條 CheckCondition；dialogue useConditionTemplates 引用須存在** |
| Diag | `Diagnostics.Dialogue.cs` | topic / INFO / condition / result-script dump |

> **Voice 註**：dialogue / banter / scene 的 INFO 現在全部補上 EditorID（語音管線 `voicelines` 的 CK 檔名需要 quest/topic EditorID，INFO EditorID 供追蹤）——管線本體見 [CODE_MAP.infra.md](CODE_MAP.infra.md)「語音克隆」段。

---

## Banter 隨機台詞
→ **說明文件**：[SPEC-dialogue.md § banter](../../../docs/spec/SPEC-dialogue.md#banter--proactive-unprompted-npc-lines)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Dialogue.cs` | `BanterSpec`（含 emotion / chance）|
| Build P1 | `Generator.Build.Banter.cs` | 建 ambient banter topic + random INFO |
| Build P2 | `Generator.Build.Conditions.cs` | banter condition 接線 |

---

## Scene 多人場景
→ **說明文件**：[SPEC-dialogue.md § scenes](../../../docs/spec/SPEC-dialogue.md#scenes--two-npcs-talking-to-each-other-scen)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Scene.cs` | `SceneSpec`, `SceneActorSpec`, `SceneAutoStartSpec`（在場偵測調參 + **重播策略 playOnce/playHour/playHourTolerance/gateGlobal**）, **`SceneActionSpec`（§1b 非對話 beat：**`idle` 播動畫**/package 走位/timer 停頓 + phase 窗）**, **`ScenePhaseSpec`（含 `HeadtrackActor`/`HeadtrackPlayer`/`FaceTarget` per-phase gaze 控制 + `StartConditions`/`CompletionConditions` per-phase CTDA 閘）**, **`SceneSpec.Conditions`（scene-level CTDA：整個 scene 通過才啟動）**（從 `Spec.Dialogue.cs` 拆出；共用其 `ConditionSpec`）|
| Build P1 | `Generator.Build.Scene.cs` | 建 SCEN：alias 綁定、參與者、phase + dialogue actions；**per-phase headtrack/facing（HeadtrackPlayer flag／HeadtrackActorID／FaceTarget；預設＝看另一 actor，行為不變）；lineless phase → beat phase（無 topic/Dialog action）；`actions[]` → Package（Type=Package，PACK ref pass 2 解析）/ Timer（Type=Timer + TimerSeconds）action；**`idle` action → 發一個 **Timer** SceneAction（hold = `timerSeconds` 或 `DefaultIdleHoldSeconds` 2s）讓該 phase 有 action 能 run（否則引擎不跑空 phase、OnStart fragment 不觸發——vanilla 每個 fragment phase 都帶 Timer），動畫本體走 SceneAdapter phase fragment**；autoStart → `AttachSceneController` 把 `MFSceneBanterController` 掛上 host quest 並清 BeginOnQuestStart；重播策略 props（playOnce/playHour/tol 直填、gateGlobal object prop pass 2 解析）** |
| Gen（PlayIdle）| `Generator.SceneFragments.cs` | scene phase-fragment **純產生器**（第三種 fragment 家族，鏡像 `GenerateQuestFragmentSource`）：`SceneNeedsFragmentScript`/`SceneFragmentScriptName`/`SceneIdleActions`/`GenerateSceneFragmentSource`；產 `SF_<scene> extends Scene Hidden`，每 idle phase 一個 `Fragment_<phase>()` 跑 `Actor_<p>.GetActorRef().PlayIdle(Idle_<p>)`（取 actor 用 `GetActorRef()`，非 `GetActorReference()`，Task 0 spike 釘死）|
| Build P2 | `Generator.Build.Scripts.cs` `AttachSceneFragments` | 掛 SCEN `SceneAdapter` VMAD（鏡像 `WireQuestStages` gating：僅當 `SF_<scene>.pex` 在 `CompiledScriptsDir`）：每 idle phase 一個 `ScenePhaseFragment{Index=(byte)phase, Flags=OnStart, FragmentName="Fragment_<phase>"}` + 綁 `Idle_<p>`（object prop→IDLE）與 `Actor_<p>`（object prop→host quest，`Alias=actor index`，同 StoryManager `qfa.Property`）|
| Wire P2 | `Generator.Build.Scene.cs` `WireScenes` | actor alias→UniqueActor；**Package action 的 PACK ref → `action.Packages`（sceneActionWires）；controller GateGlobal → GLOB object prop（sceneGateWires）；scene-level + per-phase conditions via 共用 `BuildCondition`（sceneConditionWires，phaseMap 對齊 spec-phase→built ScenePhase）** |
| Const | `Generator.DialogueFragments.cs` | `SceneBanterController`/`EncounterCooldownScript`/`DynamicSpawnScript`/`IdentityController` + identity-global scriptname 常數 + `IdentityCode` |
| Asset | `assets/papyrus/MFSceneBanterController.psc` | 可複用在場偵測 controller（extends Quest，鏈式 RegisterForSingleUpdate → Scene.Start()）；`brawlOnEnd` 偵測 scene 結束 → `StartBrawl()` 雙向 StartCombat；**重播閘門 `playOnce`（播完停 poll）/ `playHour`+tol（CurrentHour/HourDistance 時辰窗）/ `Gate` GLOB（GetValue 擋、播完 SetValue(1)）**；改 .psc 要重編 .pex（native `~/tools/papyrus-compiler` + `MODFORGE_PAPYRUS_HEADERS` 指向 cache）；embed 進 CLI |
| Validate | `Generator.Validate.Quests.cs` | actor alias ref、scene↔quest 連結；**beat phase 需有 action 覆蓋；每 action：idle⊕package/package⊕timer 互斥、idle+timer(hold) 合法、至少一個 + actor 是 scene actor + phase 窗在範圍內（idle ref CheckRef）**；**autoStart 需 StartGameEnabled host quest + ≥2 actor + 正數調參；playHour 0..24、tol>0、gateGlobal CheckRef** |
| Package | `src/ModForge.Cli/Package.cs` | 任一 scene 有 autoStart → 出貨 `MFSceneBanterController.pex`；**每 scene 有 idle action → 編 `SF_<scene>.psc`（`GenerateSceneFragmentSource`）並 build 時掛 SceneAdapter VMAD** |
| Diag | `Diagnostics.Scene.cs` | `scenediag` actors / phases / actions dump；**`scnscan` 列舉含非對話 action 的 scene（解 §1b 演出來源）** |

---

## 身份系統（輕量職業/Identity）
→ **設計**：`workflows/specs/archive/2026-06-06-identity-system-design.md`；**plan**：`workflows/plans/archive/2026-06-07-identity-system-mvp.md`

三面向：Acquire（讀書 OnRead）/ Gate（identity·primaryIdentity 標籤 → GetInFaction CTDA）/ Grant（常駐 ability 加/收）。身份狀態存成 FACT（持久訊號，未來原版 GetInFaction 可 gate）。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Identity.cs` | `IdentitySpec`（id/faction/priority/grants/toggle/default/acquireBook/acquireText）+ `IdentityAcquireSpec`（onAcquire.scene）；`ModSpec.Identities` |
| Spec | `Spec.Dialogue.cs` | `DialogueSpec.Identity` / `PrimaryIdentity`（招呼/對話行身份閘標籤）|
| Build P1 | `Generator.Build.Identity.cs` `BuildIdentities` | 每身份建一個持有 FACT（bare editorId 才建；外部/已宣告跳過）|
| Build P2 | `Generator.Build.Identity.cs` `ExpandIdentityConditions` | identity→`GetInFaction(player,faction)≥1`；primaryIdentity 再對每個更高 priority 身份補 `==0` 排除；由 `Generator.Build.Conditions.Wire.cs` `WireDialogueConditions` 呼叫併入 INFO CTDA。**Phase-2 #2 `activeWhen`**：身份的 `ActiveWhen` 情境 CTDA 接在**正向**閘後（窄化「active」）；`OnPlayerByDefault` helper 把每條 runOn 預設 pin 到玩家（author 設非-Subject runOn 才尊重）；**排除項不含 activeWhen**（否定條件束 CTDA 表達不了——held-but-inactive 高身份仍以 faction 訊號排掉低 primaryIdentity，會掉回 plain 招呼，gap 留給 #4 controller，in-game 2026-06-07 確認）。**Phase-2 #4 controller**：primaryIdentity **不再**做 faction-exclusion，改 `GetInFaction(self)≥1` + `GetGlobalValue(MF_PrimaryIdentity)==code`（code=1-based spec index，`Generator.IdentityCode`）——主身份由 controller 算（單一真相源、可被覆寫）|
| Build P2 | `Generator.Build.Identity.cs` `AttachIdentityBooks` | 把 `MFIdentityBook` VMAD 掛上 acquireBook + 綁 `TheFaction`/`GrantAbility`(grants[0])/**`GrantPerk`(grantPerks[0])**/`AcquireScene`/`Toggle`（無條件掛，鏡像 controller；prebuilt .pex 出貨）|
| Build P2 | `Generator.Build.Identity.cs` `BuildDefaultIdentityQuest` | **Phase-2 #1**：任一身份 `default:true` → 建 `MF_IdentityDefaultQuest`（StartGameEnabled）掛 `MFIdentityDefault` VMAD，用 `ScriptObjectListProperty` 綁 `Factions[]`（所有 default 身份的 faction）/`Grants[]`（其 grants，去重；無則省略）/**`Perks[]`（grantPerks，去重）**；無 default 不建。`ObjListProp` helper 建 list property|
| Build P2 | `Generator.Build.Identity.cs` `BuildIdentityAutoGrantQuest` | **龍裔首吼/autoGrantWhen**：任一身份有 `autoGrantWhen{actorValue,threshold}` → 建 `MF_IdentityAutoGrantQuest`（StartGameEnabled）掛 `MFIdentityAutoGrant` VMAD，平行 `Factions[]`/`AvNames[]`(`StrListProp`)/`Thresholds[]`(`FloatListProp`)；只授 faction 訊號；無 autoGrant 不建 |
| Build P1/P2 | `Generator.Build.Identity.cs` `BuildIdentityGlobals` / `BuildIdentityControllerQuest` | **Phase-2 #4 controller**：dialogue 用 primaryIdentity 或 setPrimaryIdentity 時（`IdentityControllerNeeded`）→ P1 建兩個 GLOB `MF_PrimaryIdentity`(controller 寫/招呼讀)+`MF_IdentityOverride`(對話寫/controller 讀)（author 同名則尊重）；P2 建 `MF_IdentityControllerQuest`（StartGameEnabled）掛 `MFIdentityController` VMAD，綁 Primary/Override GLOB + `Factions[]`(priority DESC)/`Codes[]`(`IntListProp`，code=1-based spec index)。`Generator.IdentityCode(spec,id)` 解 code（auto/空=0、未知=-1）|
| Build P2 | `Generator.Build.Scripts.cs` `AttachDialogueResultScripts` | INFO OnEnd result fragment：user `ResultScript` 優先，否則 `needsAutoTif`（setStage / setPrimaryIdentity / **openBarter / rewardItem / evaluateSpeakerPackages / persist / syncPerks / storageWrites**，.pex 在才掛）→ 生成 TIF；綁 `OwningQuest`(setStage) + `MF_IdentityOverride` GLOB(setPrimaryIdentity) + `RewardItem` form(rewardItem) + **`PKey`/`SKey`(ref-key) + `PF_<i>` form(persist 的 form-valued entry) + `SyncPerk_<i>` perk(syncPerks node)**（共用 `BindFormProp` helper；**storageWrites body-only 不綁 property**）|
| Gen | `Generator.DialogueFragments.cs` `GenerateDialogueFragmentSource(d, overrideCode)` | **多動作 TIF**：setStage→`OwningQuest.SetStage(N)`；overrideCode≥0→`MF_IdentityOverride.SetValue(code)`；**rewardItem→`Game.GetPlayer().AddItem(RewardItem, count)`；openBarter→`(akSpeakerRef as Actor).ShowBarterMenu()`；evaluateSpeakerPackages→`.EvaluatePackage()`**（後二共用 `__spk` cast）；**persist/syncPerks→`JContainersFragmentBody(d)`；storageWrites→`StorageWritesBody(d.StorageWrites)`（J組，body 末尾）**。`DialogueFragmentScriptName` 任一 result 動作（含 persist/syncPerks/**storageWrites**）→ `TIF_<ed>`|
| Spec | `Spec.Persist.cs` | **Idea #20 Phase 0**：`PersistSpec`(storage/key/set[]) + `PersistEntrySpec`(path/int/float/str/form/delta) + `SyncPerksSpec`(storage/key/nodes[]) + `SyncPerkNodeSpec`(path/perk/minRank)；掛在 `DialogueSpec.Persist`/`.SyncPerks`（對話 TIF）**與 `StageSpec.Persist`/`.SyncPerks`（quest stage fragment）** |
| Gen | `Generator.JContainers.cs` | **JFormDB 生成**（Idea #20 持久層，design §七 Phase 0 / U5；**host-agnostic** — 對話 TIF 與 quest stage fragment 共用）：`ClassifyPersistKey`(speaker/player/ref)、`JFormDbKeyExpr(key, keyProp)`(speaker→akSpeakerRef、player→Game.GetPlayer()、**ref→bound Form prop**)、`JFormDbPath`(`.<storage><sub>`)、`JContainersPropertyDecls(prefix, persist, sync)`(ref-key Form prop + Form/Perk Auto 宣告)、`JContainersFragmentBody(prefix, persist, sync)`(persist→`solveXxxSetter`〔delta read-add-write〕；syncPerks→`solveInt>=minRank ? AddPerk : RemovePerk`)、`HasPersist`/`HasSyncPerks`(PersistSpec/SyncPerksSpec/DialogueSpec/StageSpec overloads)、**prefix-scoped 名**`PersistFormProperty(prefix,i)`=`<prefix>PF_<i>`/`SyncPerkProperty(prefix,i)`/`PersistKeyProperty`=`<prefix>PKey`/`SyncKeyProperty`=`<prefix>SKey`（對話 prefix=""，stage prefix=`S<idx:D4>_`）。**只用 root-DB path API → 無 retain/release（U5 設計上繞開）**；純字串、離線可測 |
| Gen | `Generator.StorageWrites.cs` | **J組 PapyrusUtil StorageUtil per-Form KV 生成**（**host-agnostic** — 對話 TIF 與 stage fragment 共用，**body-only 無 VMAD property**）：`ClassifyStorageTarget`(speaker/player/none；none/global 皆→None)、`StorageTargetExpr`(speaker→akSpeakerRef、player→Game.GetPlayer()、none→None — **三者皆純表達式，故 storageWrites 不綁任何 property，arbitrary-ref target 暫緩**)、`StorageWritesBody(writes)`(int/float→`Set{Int,Float}Value`〔delta→`Adjust…Value` atomic read-add-write〕；str→`SetStringValue`〔無 delta〕)、`HasStorageWrites`(List/DialogueSpec/StageSpec overloads)、`StorageTargetTokens`(validate 用)。共用 `EscapeStr`/`PapyrusFloat`。**編譯需 PapyrusUtil .psc 上 header path**；純字串、離線可測 |
| Gen | `Generator.QuestFragments.cs` `GenerateQuestFragmentSource` | stage fragment 純產生器；**`QuestNeedsFragmentScript` 含 stage persist/sync/storageWrites + `StoryTrigger`**；`StagePropPrefix(idx)`=`S<idx:D4>_`；每有 persist/sync/**storageWrites** 的 stage 進 `stageNums`，prop 宣告 + `Fragment_Stage_XXXX` body 末尾接 `JContainersFragmentBody(...)` + **`StorageWritesBody(st.StorageWrites)`（J組）**；**SM quest 兩者皆路由到 `OnStory<Event>` handler（`AppendJcStages`/`AppendStorageStages`），stage fragment 不發**（player/none target，無 akSpeakerRef）|
| Gen | `Generator.QuestFragments.cs` `StoryTrigger` / `StartupStageTrigger` | **spawn/cooldown 觸發掛哪：SM-driven（有 storyEvent）→ `StoryTrigger`=true → 生 `Event OnStory<Event>(...)` handler（TryFire 冷卻閘→SpawnNow→`Stop()` 重武裝讓 SM 下次再啟）；非 storyEvent（StartGameEnabled spawn）→ `StartupStageTrigger`=startUpStage index → 觸發放 `Fragment_Stage_XXXX`。⚠ 真因（in-game 2026-06-19）：SM 啟動的 quest 會跑 OnInit/OnStory<Event> 但**不跑** startUpStage 的 Papyrus fragment，故 SM encounter 的觸發必須改掛 OnStory。handler 簽名取自 `StoryManagerEvents.StoryHandler`；`self as MFDynamicSpawn`/`MFEncounterCooldown` 兄弟 cast 靠 native papyrus-compiler（Caprica-like，CK 編譯器拒收）+ runtime 同 Form 多腳本有效** |
| Build P2 | `Generator.Build.QuestStages.cs` `WireQuestStages` | stage VMAD 綁定；`needsFrag` 含 persist/sync；每 stage 用 `StagePropPrefix` 綁 `<prefix>PKey/SKey`(ref-key)+`<prefix>PF_<i>`(persist form)+`<prefix>SyncPerk_<i>`(perk)，共用 `BindFormProp` helper |
| Asset | `assets/papyrus/MFIdentityBook.psc` | 可複用身份書（**extends ObjectReference**，OnRead → AddToFaction+AddSpell+**AddPerk(GrantPerk)**+Scene.Start；Toggle 反向移除）；**鐵律：OnRead 是 ObjectReference 的 event，不是 Book 的——`extends Book` 永遠收不到 OnRead（Book/ObjectReference 都 extends Form、是兄弟）；綁在 BOOK base form 上、背包讀也會 fire（in-game 2026-06-07 確認）**；改 .psc 要重編 .pex；embed 進 CLI（條件式 EmbeddedResource）|
| Asset | `assets/papyrus/MFIdentityDefault.psc` | 預設身份授予（**extends Quest**，OnInit 遍歷 `Factions[]` AddToFaction + `Grants[]` AddSpell + **`Perks[]` AddPerk**，idempotent 跳過已持有）；StartGameEnabled host quest 開局/載入觸發（進 `.seq` 故舊存檔也跑，in-game 2026-06-07 確認）；改 .psc 要重編 .pex；embed 進 CLI（條件式 EmbeddedResource）|
| Asset | `assets/papyrus/MFIdentityController.psc` | 主身份 controller（**extends Quest**，OnInit + `RegisterForSingleUpdate(3.0)` poll → `Recompute`：Override(若持有)否則 priority 最高持有 → 寫 `Primary` GLOB）；Factions[]/Codes[] 平行 priority DESC（in-game 2026-06-07 確認）；改 .psc 要重編 .pex；embed 進 CLI（條件式 EmbeddedResource）|
| Asset | `assets/papyrus/MFIdentityAutoGrant.psc` | 自動授予 trigger（**extends Quest**，OnInit + 5s poll → `Check`：`p.GetActorValue(AvNames[i]) >= Thresholds[i] && !IsInFaction → AddToFaction`，純 Papyrus 讀 AV、免 SKSE/事件 hook）；Factions[]/AvNames[]/Thresholds[] 平行；只授 faction；改 .psc 要重編 .pex；embed 進 CLI（條件式）|
| Validate | `Generator.Validate.cs` | `RegisterIdentityFactions`（早登錄自建 FACT editorId 供 condition 解析）+ `ValidateIdentities`（unique id、非空 faction、grants/**grantPerks**/acquireBook/**activeWhen param** CheckRef、**autoGrantWhen actorValue 非空**）|
| Validate | `Generator.Validate.Quests.cs` | dialogue 規則；**`hello:true` 招呼免 prompt**（招呼非玩家選項；其餘 dialogue 仍須 prompt）；**`setPrimaryIdentity` 須是已知 id 或 `auto`**；**`rewardItem` CheckRef**；**`persist`/`syncPerks`（dialogue + stage 共用 `ValidatePersistBlock`/`ValidateSyncPerksBlock`）：storage 非空、key 三態(`ValidatePersistKey`：speaker〔dialogue only〕/player/ref→CheckRef；stage allowSpeaker=false)、persist entry 恰一值型(int/float/str/form)、delta 限 int/float、form/perk CheckRef**；**`storageWrites`（`ValidateStorageWrites`，dialogue + stage 共用）：key 非空、恰一值型(int/float/str)、delta 限 int/float、target 須在 `StorageTargetTokens`、stage allowSpeaker=false 拒 speaker(含空缺省)**；**M組 `variants`：parent 無 responses 時免「無回應行」、每 variant 須有 responses + emotion 合法 + 自有 conditions `CheckCondition`、`hello` 不可帶 variants**|
| Package | `src/ModForge.Cli/Package.cs` §5d–§5g | §5d acquireBook → `MFIdentityBook.pex`；§5e `default:true` → `MFIdentityDefault.pex`；§5f primaryIdentity/setPrimaryIdentity → `MFIdentityController.pex`；§5g `autoGrantWhen` → `MFIdentityAutoGrant.pex`（皆進 Scripts/）；dialogue fragment 編譯傳 `IdentityCode` 給 `GenerateDialogueFragmentSource`|

**Phase-2/C 進度**：✅ #1 Adventurer 預設自動授予、✅ #2 `activeWhen` 情境條件、✅ #4 controller 主身份+手動覆寫、✅ #5 身份對應互動 **#5a 商人交易 UI（`openBarter`）+ #5b 護衛/跟隨任務（`identity`-gated escort quest，follow PACK gated on GetStage、`rewardItem`/`evaluateSpeakerPackages` TIF）+ #5c 聖騎士 smite 細調（`grantPerks` → `MF_SmiteEvilPerk` ModAttackDamage ×1.25 vs undead/daedra）+ 龍裔首吼（`autoGrantWhen` → MFIdentityAutoGrant，DragonSouls≥1）**（皆 in-game 確認 2026-06-07）。**未做**：#3 聲望/行為追蹤。

---

## Conditions（CTDA 條件）
→ **說明文件**：[SPEC-dialogue.md § conditions](../../../docs/spec/SPEC-dialogue.md#conditions--ctda-gates-on-a-dialogue-info-a-banter-info-or-a-package)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Build P2 | `Generator.Build.Conditions.cs` | 所有 CTDA 的 function dispatch + ref 解析（dialogue / stage / banter / package 共用）。**地點感知（#5）：`GetKeywordDataForCurrentLocation`（玩家當前地點 LocType）/`LocationHasKeyword`/`LocAliasHasKeyword`（location alias 的 keyword，hold 偵測，需 owning quest alias index，仿 GetIsAliasRef）。****`GetIsAliasRef`**：用 `alias`（owning quest 的 alias 名）→ alias index，由各 quest-scoped 呼叫點傳入 `aliasIndexByName`。**`IsSceneActionComplete`**：`BuildCondition` 第 4 參 `owningScene` FormKey，scene-cond 呼叫點（`WireScenes` 的 sceneConditionWires）傳 `scene.FormKey`，author 可用 `c.Scene` 覆寫；`c.SceneActionIndex` 必填。package/perk 等無 scene/quest context → 傳 null → 警告丟棄。**`GetVMQuestVariable`/`GetVMScriptVariable`（L組）**：condition 讀 Papyrus property，`param`=quest（VM-quest）/object（VM-script）、`c.VariableName`=property 名字串（verbatim 寫進 CTDA，引擎期望的 bare-name vs `::Prop_var` 待 xEdit/實機驗）|
| Validate | `Generator.Validate.Helpers.cs` | `CheckCondition`（function / comparator / ref）|
| Diag | `Diagnostics.Dialogue.cs` | condition 欄位 dump |

---

## Story Manager 事件觸發
→ **說明文件**：[SPEC-quests.md § Story Manager](../../../docs/spec/SPEC-quests.md#story-manager-quests--event-driven-start) · [for_agent.md § 限制](../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.StoryManager.cs` | `QuestStoryEventSpec`（event + conditions + **`locationFilter[]`（#5 LocType 路由）+ `cooldownHours`（#6 冷卻）**）、`AliasSpec`（fill 模式：fromEvent/forced/uniqueActor/createObject/findMatching/**findMatchingLocation（#7 radiant LocationAlias）/findInLocationAlias（#8 在地點內找 ref）**；findMatching/find* 帶 `Conditions`；alias 腳本 `Script`/`ScriptSource`/`ScriptProperties` = OnActivate 等）|
| Data | `StoryManagerEvents.cs` | 事件登錄表：KillActor/ChangeLocation/CastMagic/AddItem/Assault/CraftItem/PlayerRemoveItem/Arrest/IncreaseLevel/ScriptEvent — FormKey + 槽名 + **`StoryHandler`（各事件在 Quest 腳本上的 `OnStory<Event>(...)` 簽名，SM encounter 觸發掛這）**；`TryParseFill` / `TryParseCreateObject`（`<ref>@<alias>`）|
| Build P2 | `Generator.Build.StoryManager.cs` | SMBN→SMQN 掛原版事件根；keyword 過濾條件（GetEventData/GetIsID）；**`storyEvent.event=CastMagic` 出 build-time `Warn`**（引擎被動 CastMagicEvent 不對玩家普通施法觸發，in-game 2026-06-20；改用 MGEF→dispatcher→ScriptEvent，見 `skill_cast_spec.json`）；**`BuildQuestAliases(quest,qs,def?)`（在 `Generator.Build.QuestAliases.cs`，連同 `BuildStandaloneQuestAliases`/`AttachAliasScript`）** 共用 helper 建所有 alias fill（fromEvent 僅 `def!=null` 時；createObject = `CreateReferenceToObject` 在 `aliasIdByName` 目標 alias 處生成；findMatching = `QuestAlias.Flag.MatchingRefInLoadedArea`[+`MatchingRefClosest`] + alias.Conditions；**findMatchingLocation（#7）= `Type=Location` + `StoresText` + 匹配 CTDA：`LocationHasKeyword==1`(locType)[+ 父 alias 時 `GetInCurrentLocAlias==1`(LocationAliasIndex=parent)]——byte 對齊 shipping Missives Dungeon 別名（2026-06-21），引擎在 Location 別名忽略 `LocationAliasReference.Keyword` 故 conditions-based；findInLocationAlias（#8）= `Type=Reference` + `LocationAliasReference{AliasID=locAlias, RefType=LCRT}`（對齊 Missives `Target` 別名）；條件接線共用 `WireAliasMatchConditions`**；alias 腳本 `AttachAliasScript` = `QuestAdapter.Aliases` 加 `QuestFragmentAlias`[v5/objFmt2、綁 alias ID、flag=Local]；**`forced:` 立即解析不到（target 晚 build）→ 排入 `deferredForcedAliases`、由 `WireDeferredForcedAliases` 後補**）；**`BuildStandaloneQuestAliases()`** 替非 storyEvent quest 建 alias（def=null，跳 fromEvent）。⚠ ALNA(`FindMatchingRefNearAlias`)離線驗證＝只 `LinkedRefChild`，故 #8 走 `Location` 不走 ALNA；✅ CK 語義已對 shipping Missives byte 驗（2026-06-21，`questdiag` 加 alias dump），剩實機 fill |
| Build P2 | `Generator.Build.StoryManager.cs` | **#5 locationFilter → 在 `quest.EventConditions` 追加 OR'd `GetKeywordDataForCurrentLocation`；#6 `AttachEncounterCooldown`：建 `<quest>_LastFired` float GLOB + 掛 `MFEncounterCooldown` quest script（無條件，prebuilt .pex）** |
| Build P2 | `Generator.Build.StoryManager.Encounter.cs` `BuildQuestSpawns` | **#3 `quest.spawn` → 掛 `MFDynamicSpawn` quest script（SpawnForm/Count/Min/MaxDistance/SnapToNavmesh props，merge QuestAdapter）；Build.cs 在 BuildStandaloneQuestAliases 後、WireQuestStages 前呼叫** |
| Asset | `assets/papyrus/MFEncounterCooldown.psc` | **#6 reusable 冷卻（extends Quest，`bool TryFire()` 比 `GetCurrentGameTime - LastFired < CooldownHours/24` → false 中止；由 `<quest>_Stages` 的 OnStory<Event> handler 呼叫，不靠 OnInit）；embed CLI；EE_WITimeout pattern** |
| Asset | `assets/papyrus/MFDynamicSpawn.psc` | **#3 reusable dynamic spawn（extends Quest，`SpawnNow()` `PlaceAtMe`+`MoveTo(+128 Z 落體)` 玩家附近隨機偏移；由 `<quest>_Stages` 的 OnStory<Event> handler〔SM〕或 startUpStage fragment〔StartGameEnabled〕呼叫，不靠 OnInit — OnInit 一生只跑一次，SM relaunch 不重觸發）；embed CLI；spawn pipeline IN-GAME 確認 2026-06-19** |
| Validate | `Generator.Validate.StoryManager.cs` | 事件名合法；**locationFilter keyword CheckRef、cooldownHours>=0**；**`ValidateQuestAlias(q,a,def?,…)`** 共用（storyEvent 與非 storyEvent quest 都驗 alias fill/ref/script；def=null 時 fromEvent 報錯；**findMatchingLocation 驗 LocType keyword + 父 alias 同 quest；findInLocationAlias 驗 location alias 同 quest + refType + 需 refType 或 conditions**）；slot 名稱、ScriptEvent 需宣告 keyword |
| Diag | `Diagnostics.StoryManager.cs` | `smtree`（事件根 SMEN 列舉）/ **`smsub <plugin> <rootHex>`（dump 某事件根下的 SMBN/SMQN 子樹，反射印全欄位 + SMQN 指向的 quest/flags；用來 byte-compare vanilla vs 生成的節點，例 `smsub Skyrim.esm 0x01320E`）** |

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
→ **說明文件**：[SPEC-quests.md § ScriptEvent](../../../docs/spec/SPEC-quests.md#scriptevent--sending-your-own-story-events)

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
→ **說明文件**：[SPEC-quests.md § scripts](../../../docs/spec/SPEC-quests.md#scripts--papyrus-attachment)

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
