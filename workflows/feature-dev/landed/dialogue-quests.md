# 已落地 — 對話 / 任務 / Story Manager / Scene

← [landed index](README.md)｜程式碼導航 [CODE_MAP.dialogue-quests](../../common/code-map/CODE_MAP.dialogue-quests.md)

本頁只列現行能力；欄位語意見 docs/spec，型別、方法與測試路徑見 CODE_MAP。

## 任務與 Story Manager

- quest-node：schemas/quest-node.schema.json 定義交換格式；questnodes <plugin> <outDir> [--strings <dir>] 只從非空 QUST stage log 做 mechanical import，不猜 location／NPC／graph。game-data/extract.sh 輸出到 catalog/quest-nodes/<plugin>/。
- Story Manager：QuestSpec.storyEvent＋aliases 生成 SMBN→SMQN，支援 StoryManagerEvents 登錄的十種 engine-native 事件；事件根下只啟動第一個符合的 quest。
- alias fill：fromEvent、forced、uniqueActor、createObject、findMatching、findMatchingLocation、findInLocationAlias；radiant LocationAlias／ReferenceAlias 與 ALPS package wiring 已接通。
- 動態遭遇：quest.spawn 掛 MFDynamicSpawn.psc；locationFilter 生成 GetKeywordDataForCurrentLocation CTDA；cooldownHours 掛 MFEncounterCooldown.psc。SM quest 的觸發放 OnStory<Event>，不能依賴 startUpStage fragment。
- stages／objectives：startUpStage、instanceGlobals、globalWrites、objective targets、受限 SetStage；instanceGlobals 以 SetValue＋UpdateCurrentInstanceGlobal 綁 quest instance。
- ScriptEvent：MFStoryEventDispatch.psc 的 Fire() 共用 magic effect、potion、activator、dialogue、alias-OnActivate 五入口；介面變更時必須同步重編 .pex。

## 對話、條件與身份

- dialogue 支援 conversation、Hello、conditionTemplates、variants、result fragment 與 voice-line EditorID；Hello 是單一 topic 下按順序排列 INFO，不是多 topic 競 priority。
- CTDA 的 param／reference 可指 placed ref；BuildReferences 前的呼叫點必須 DeferCondition，由 refsIndexed guard 捕捉 build-order 違規。
- package ref 槽分 SingleRef 與 Location；sitTarget.target 等 SingleRef 鎖定特定 ref，sandbox.location 等 Location 只錨一個區域。唯一分類表是 src/ModForge.Core/PackageRefSlots.cs。
- storageWrites 支援 speaker／player／none／任意 ref，以及 fromJson；外部手寫 JSON 必須用 JsonUtil.GetPath*Value(".key")，不能用只讀 JsonUtil namespace 的 Get*Value。
- identity：IdentitySpec 生成 FACT 狀態、書本／預設／autoGrant 取得、activeWhen／primaryIdentity gate、ability／perk 授予與交易／獎勵／重評 package。MFIdentityBook 必須 extends ObjectReference。
- npcRoles：SceneNpcRoleSpec 的 blacksmith macro 生成 conditioned Hello、sandbox package，並可接 vendor FACT、merchant chest、trade topic 與 external NpcPatch faction。

## Scene

- SceneSpec 支援多人 phase、dialogue、Package／Timer、PlayIdle、SetStage、headtrack/facing、scene／phase CTDA、autoStart 與 playOnce／playHour／gateGlobal 重播策略。
- PlayIdle／SetStage 合併到每 phase 單一 SF_<scene>.Fragment_<phase>()；fragment phase 必須有 Timer action，否則引擎不跑空 phase。
- MFSceneBanterController.psc 負責在場偵測、輪詢啟動、重播 gate 與可選 brawlOnEnd。

## Papyrus 持久資料

- persist／syncPerks 走 Generator.JContainers.cs 的 JFormDB root-path API；對話 TIF 與 quest stage 共用，同一 host 以 prefix 隔離 property。
- storageWrites 走 Generator.StorageWrites.cs 的 PapyrusUtil StorageUtil；arbitrary-ref target 綁 SWRef_<i> Form property。
- reusable .psc 由 CLI 條件式夾帶；需 fragment 的路徑只有 .pex 存在才掛 VMAD，package 流程負責編譯與出貨。

## 驗證邊界

- 純 record 與生成器路徑由離線測試覆蓋；需要 Skyrim.esm、Papyrus runtime、alias fill、動態 spawn、cooldown、voice／scene 行為者列在 [WAIT_USER](../../../WAIT_USER.md)。
- GetVMQuestVariable／GetVMScriptVariable 的 variableName 字串格式仍需主力機 xEdit／實機確認。
- 身份系統尚未做聲望／行為追蹤；npcRoles 尚未擴成完整 archetype 集。
