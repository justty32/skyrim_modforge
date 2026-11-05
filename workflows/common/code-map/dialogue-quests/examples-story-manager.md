# CODE_MAP.dialogue-quests — Examples（Story Manager・ScriptEvent・持久層）

← [CODE_MAP.dialogue-quests](../CODE_MAP.dialogue-quests.md)

## Examples（續：Story Manager・ScriptEvent・持久層）

| 檔案 | 對應功能 |
|-----|---------|
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
| `examples/storage_json_ref_diag_spec.json` | **J組 storageWrites 兩擴充 IN-GAME CONFIRMED（2026-06-22）**：StartGameEnabled startUpStage-10 同時試 **arbitrary-ref target**（`target: MFSW_TestNpc`→`SWRef_0` Form prop，per-NPC）+ **`fromJson`**（int/str 從 `ModForgeJsonDiag/config.json` 經 JsonUtil **Path API** 讀，literal 作 missing default）。實機 `ref(npc)=1 / ref(player)=-999 / jsonDiff=42 / jsonName=from-json-ok`。**fromJson 真因坑**：`GetIntValue` 對外部手寫 JSON 是空的→必用 `GetPath*Value(".key")`（診斷探所有組合定死）。交付 zip 的 `.pex` 含手注入 5s-loop Debug.Notification readback；隨包 `config.json`（difficulty=42,name=from-json-ok）|
| `examples/story-manager-activatortrigger.json` | ScriptEvent 經 activator 觸發（拉桿→啟動 quest）|
| `examples/MFSE_ActivatorTrigger.psc` | 可複用 activator trigger（OnActivate→Fire）|
| `examples/story-manager-potiontrigger.json` | ScriptEvent 經 potion 觸發（喝藥水→啟動 quest，複用 MFSE_SpellTrigger）|
| `examples/story-manager-dialoguetrigger.json` | ScriptEvent 經 NPC 對話觸發（選對話→啟動 quest）|
| `examples/MFSE_DialogueTrigger.psc` | 可複用 dialogue trigger（TopicInfo `Fragment_0`→Fire）|
| `examples/scripts/MFDemoQuestScript.psc` | quest fragment Papyrus 示範 |
