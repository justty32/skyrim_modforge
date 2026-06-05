# CODE_MAP — 對話・任務・Story Manager・腳本

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：quest + stages + objectives、dialogue topics + INFO、multi-actor scenes、CTDA conditions、Story Manager event quests、ScriptEvent、word walls、Papyrus 附加。

---

## 1. Spec（資料定義）

| 檔案 | 主要型別 |
|-----|---------|
| `src/ModForge.Core/Spec.Dialogue.cs` | `QuestSpec`, `QuestStageSpec`, `QuestObjectiveSpec`, `DialogueSpec`, `DialogueInfoSpec`, `SceneSpec`, `SceneActorSpec`, `ScriptAttachSpec` |
| `src/ModForge.Core/Spec.StoryManager.cs` | `QuestStoryEventSpec`（storyEvent 欄位）、`AliasSpec`（fill="fromEvent:\*"/uniqueActor/forced）|
| `src/ModForge.Core/Spec.WordWall.cs` | `WordWallSpec`, `WordWallTriggerSpec` |
| `src/ModForge.Core/StoryManagerEvents.cs` | 事件登錄表：KillActor/ChangeLocation/CastMagic/AddItem/Assault/ScriptEvent — FormKey + 槽名對應 |

---

## 2. Build Pass 1（建 record）

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Core/Generator.Build.Dialogue.cs` | 建 Quest + Branch + Topic + INFO，自動 greeting，player-topic 優先度管理 |
| `src/ModForge.Core/Generator.Build.Banter.cs` | 建 ambient banter（無提示隨機台詞），INFO with emotion |
| `src/ModForge.Core/Generator.Build.Scene.cs` | 建 SCEN：alias 綁定、參與者、phase + dialogue actions |

## 3. Build Pass 2（接 FormLink）

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Core/Generator.Build.Conditions.cs` | 所有 CTDA condition 的 function dispatch + ref 解析（dialogue/quest stage/banter/package 共用）|
| `src/ModForge.Core/Generator.Build.QuestStages.cs` | Quest stage log text + objective-completion fragment 腳本附加（VMAD）|
| `src/ModForge.Core/Generator.Build.StoryManager.cs` | **SM pass 2**：SMBN→SMQN 在原版事件根下掛接；keyword 過濾條件（GetEventData/GetIsID）；alias fill 模式 |
| `src/ModForge.Core/Generator.Build.Scripts.cs` | Papyrus 腳本附加到任意 record（NPC/object/quest/cell），property 綁定 + .pex 載入 |
| `src/ModForge.Core/Generator.QuestFragments.cs` | 自動生 SetObjectiveDisplayed/SetObjectiveCompleted Papyrus fragment |
| `src/ModForge.Core/Generator.WordWall.cs` | WordWall 教字 quest fragment（AddShout/TeachWord + property 綁定）|

---

## 4. Validate

| 檔案 | 檢查什麼 |
|-----|---------|
| `src/ModForge.Core/Generator.Validate.Quests.cs` | stage index 唯一/遞增、objective↔stage 連結、condition function 合法性、script ref 存在 |
| `src/ModForge.Core/Generator.Validate.StoryManager.cs` | 事件名合法、alias fill 語法、slot 名稱、ScriptEvent 需宣告 keyword |
| `src/ModForge.Core/Generator.Validate.Helpers.cs` | `CheckCondition`（function/comparator/ref）、`CheckEffects`（魔法效果 ref）共用 |

---

## 5. Diagnostics（dump 輸出）

| 檔案 | dump 哪些 |
|-----|---------|
| `src/ModForge.Cli/Diagnostics.Quests.cs` | quest stages / objectives / aliases / VMAD 腳本 |
| `src/ModForge.Cli/Diagnostics.Dump.Quest.cs` | quest + scene 的結構化完整 dump |
| `src/ModForge.Cli/Diagnostics.Dialogue.cs` | topic / INFO / condition / result-script |
| `src/ModForge.Cli/Diagnostics.Scene.cs` | SCEN actors / phases / dialogue actions |
| `src/ModForge.Cli/Diagnostics.StoryManager.cs` | smtree（事件根列舉）/ SMBN alias fill / event-data slot |
| `src/ModForge.Cli/Diagnostics.Shouts.cs` | shout word list / spell tiers / cooldown |

---

## 6. Story Manager 快速參考

### 支援事件與槽

| 事件 | R1 | R2 | L1 |
|-----|----|----|-----|
| KillActor | victim | killer | location |
| ChangeLocation | actor | — | newLocation |
| CastMagic | caster | target | location |
| AddItem | actor | — | location |
| Assault | victim | assailant | location |
| ScriptEvent | ref1 | ref2 | loc |

### Alias fill 語法

```
"fill": "fromEvent:<slot>"      # R1/R2/L1/L2 slot 填充
"fill": "uniqueActor:<ref>"     # 固定 actor ref（自動 AllowReserved）
"fill": "forced:<ref>"          # 固定 object/location ref
"allowReserved": true           # 明確 opt-in AllowReserved flag
```

### ScriptEvent dispatcher

```
MFStoryEventDispatch.Fire(kw, ref1, ref2, loc)
```
- `.pex` 嵌入 CLI，`package` 時自動複製到 `Scripts/`
- quest 宣告 `keyword`（在 spec.keywords 中建）→ build 在分支加 `GetEventData Keyword GetIsID <KYWD>==1`

### 鐵律

- 一事件根 → 一共用分支 → 多 quest node（串 PreviousSibling）
- 事件根下多分支互斥（引擎只跑一條）
- 引擎一事件只啟動最先符合的 quest（radiant 正確行為）
- Location 型 alias 必須 `Type=Location`（fromEvent 'L' 開頭自動設）
- 任一必填 alias 填不上 → quest 靜默不啟動

---

## 7. Docs

| 連結 | 內容 |
|-----|-----|
| `docs/SPEC-dialogue-quests.md` | 完整 spec 欄位參考（EN）|
| `docs/zh-TW/SPEC-dialogue-quests.md` | 完整 spec 欄位參考（zh-TW）|
| `docs/for_agent.md#限制` | in-game 驗證狀態 + 如實回報規則 |
| `docs/IDEAS.md` 第 9 節 | SM 設計背景與 engine quirks |
| `docs/superpowers/specs/2026-06-04-script-event-entry-spike.md` | ScriptEvent 研究記錄 |
