# CODE_MAP — 對話・任務・Story Manager・腳本

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：quest + stages + objectives、dialogue topics + INFO、banter、multi-actor scenes、CTDA conditions、Story Manager event quests、ScriptEvent、word walls、Papyrus 附加。

## Examples

| 檔案 | 對應功能 |
|-----|---------|
| `examples/dialogue_spec.json` | 單一 NPC 對話樹 |
| `examples/dialogue_conversation_spec.json` | 多輪對話（conversation 模式）|
| `examples/scene_spec.json` | 雙 NPC 場景（SCEN）|
| `examples/quest_stages_spec.json` | quest stages + objectives + log entries |
| `examples/word_wall_spec.json` | word wall 觸發教字 |
| `examples/story-manager-kill.json` | KillActor SM 事件 |
| `examples/story-manager-assault.json` | Assault SM 事件 |
| `examples/story-manager-changelocation.json` | ChangeLocation SM 事件 |
| `examples/story-manager-uniqueactor.json` | uniqueActor alias fill |
| `examples/story-manager-createobject.json` | createObject alias fill（事件觸發→在另一 alias 處生成物件；複用 magic trigger）|
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
| `SceneTests.cs` | SCEN actor / phase / dialogue action |
| `StoryManagerBuildTests.cs` | SM build pass 2（SMBN/SMQN 掛接、alias fill 接線）|
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
| Spec | `Spec.Dialogue.cs` | `QuestSpec`, `QuestStageSpec`, `QuestObjectiveSpec` |
| Build P1 | `Generator.Build.Dialogue.cs` | 建 Quest record + Branch + Topic + INFO；greeting 自動生成 |
| Build P2 | `Generator.Build.QuestStages.cs` | stage log text + objective-completion fragment VMAD |
| Build P2 | `Generator.QuestFragments.cs` | 自動生 SetObjectiveDisplayed/SetObjectiveCompleted Papyrus fragment |
| Validate | `Generator.Validate.Quests.cs` | stage index 唯一/遞增、objective↔stage 連結、script ref 存在 |
| Diag | `Diagnostics.Quests.cs` | stages / objectives / aliases / VMAD 腳本 dump |
| Diag | `Diagnostics.Dump.Quest.cs` | quest + scene 結構化完整 dump |

---

## Dialogue 對話樹
→ **說明文件**：[SPEC-dialogue-quests.md § dialogue](SPEC-dialogue-quests.md#dialogue)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Dialogue.cs` | `DialogueSpec`, `DialogueInfoSpec` |
| Build P1 | `Generator.Build.Dialogue.cs` | Branch / Topic / INFO 建立；player-topic 優先度管理 |
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
| Spec | `Spec.Dialogue.cs` | `SceneSpec`, `SceneActorSpec` |
| Build P1 | `Generator.Build.Scene.cs` | 建 SCEN：alias 綁定、參與者、phase + dialogue actions |
| Validate | `Generator.Validate.Quests.cs` | actor alias ref、scene↔quest 連結 |
| Diag | `Diagnostics.Scene.cs` | actors / phases / dialogue actions dump |

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
| Spec | `Spec.StoryManager.cs` | `QuestStoryEventSpec`（event + conditions）、`AliasSpec`（fill 模式：fromEvent/forced/uniqueActor/createObject）|
| Data | `StoryManagerEvents.cs` | 事件登錄表：KillActor/ChangeLocation/CastMagic/AddItem/Assault/ScriptEvent — FormKey + 槽名；`TryParseFill` / `TryParseCreateObject`（`<ref>@<alias>`）|
| Build P2 | `Generator.Build.StoryManager.cs` | SMBN→SMQN 掛原版事件根；keyword 過濾條件（GetEventData/GetIsID）；alias fill 接線（含 createObject = `CreateReferenceToObject` 在 `aliasIdByName` 目標 alias 處生成）|
| Validate | `Generator.Validate.StoryManager.cs` | 事件名合法、alias fill 語法、slot 名稱、ScriptEvent 需宣告 keyword |
| Diag | `Diagnostics.StoryManager.cs` | smtree（事件根列舉）/ SMBN alias fill / event-data slot dump |

### 支援事件與槽

| 事件 | R1 | R2 | L1 |
|-----|----|----|-----|
| KillActor | victim | killer | location |
| ChangeLocation | actor | — | newLocation |
| CastMagic | caster | target | location |
| AddItem | actor | — | location |
| Assault | victim | assailant | location |
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
