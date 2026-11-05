# CODE_MAP.dialogue-quests — 職業・Quest 基礎

← [CODE_MAP.dialogue-quests](../CODE_MAP.dialogue-quests.md)

## Classes（職業 CLAS）
→ **說明文件**：[SPEC-dialogue.md § classes](../../../../docs/spec/SPEC-dialogue.md#classes-clas)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Magic.cs` | `ClassSpec`（attribute weights / skill training）|
| Build P1 | `Generator.Build.Classes.cs` | 建 Class record |
| Validate | `Generator.Validate.Npcs.cs` | class ref 檢查（npc → class）|

---

## Quest 基礎（stages / objectives）
→ **說明文件**：[SPEC-quests.md § Quest stages](../../../../docs/spec/SPEC-quests.md#quest-stages-log-entries--objective-wiring)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Quests.cs` | `QuestSpec`（含 **`spawn` = F組 #3 dynamic spawn**）, **`SpawnSpec`（form/count/min/maxDistance/snapToNavmesh）**, `StageSpec`（含 `startUpStage` = QSDT 起始 stage flag、**`instanceGlobals[]` = UpdateCurrentInstanceGlobal 綁定**、**`globalWrites[]` = K組 plain `<global>.SetValue(value)`（非 instance bind；SM quest 路由到 OnStory handler）**、**`persist`/`syncPerks` = stage-fragment JFormDB（Idea #20 Phase 0，key 須 player/ref，無 speaker）**、**`storageWrites[]` = J組 stage StorageUtil KV（target 須 player/none）**）, **`InstanceGlobalSpec`（global + 可選 randomMin/Max 或 value）**, **`GlobalWriteSpec`（global + value）**, **`StorageWriteSpec`（key + target〔speaker/player/none/**任意 ref**〕 + int/float/str + delta + **`fromJson`〔JsonReadSpec：JsonUtil 讀檔取值，literal 作 missing default〕**）= J組**, **`JsonReadSpec`（file + key）**, **`DialogueVariantSpec`（responses + 自有 conditions + 可選 emotion/emotionValue/sayOnce）= M組 INFO 批次**, `ObjectiveSpec`（含 `targets[]`）, **`ObjectiveTargetSpec`（alias 名 + `compassIgnoresLocks` + `conditions[]` → QSTA）** |
| Build P1 | `Generator.Build.Actors.cs` `BuildQuests` | 建 Quest record + QSDT stages（log/complete/fail flag、`startUpStage`→`QuestStage.Flag.StartUpStage`）+ QOBJ objectives |
| Build P1 | `Generator.Build.Dialogue.cs` | dialogue Branch + Topic + INFO（greeting 在 `Generator.Build.Dialogue.Hello.cs`）|
| Build P2 | `Generator.Build.QuestStages.cs` | stage log-entry CTDA + objective fragment VMAD（**合併**進既有 QuestAdapter，不覆寫 alias 腳本的 `.Aliases`）；**instanceGlobals → 在 ScriptEntry 綁 GLOB `ScriptObjectProperty`（每 global 一個，prop 名 `InstanceGlobalProperty`）+ 該 stage 的 `QuestScriptFragment`（即使無 objective）** |
| Build P2 | `Generator.Build.ObjectiveTargets.cs` | **`WireObjectiveTargets`**：alias 名→alias index → QOBJ `QuestObjectiveTarget`（QSTA：AliasID + `Quest.TargetFlag.CompassMarkerIgnoresLocks` + per-target CTDA via `BuildCondition`）；在 alias pass 之後跑。**`WireDeferredForcedAliases`**：解析「target 晚於 alias pass 才 build」的 `forced:` alias（placement/xmarker/mapMarker），在 BuildPlacements/BuildMapMarkers 之後跑 |
| Build P2 | `Generator.QuestFragments.cs` | 自動生 `<quest>_Stages` Papyrus fragment：SetObjectiveDisplayed/Completed + **instanceGlobals（`GlobalVariable Property` 宣告 + `<g>.SetValue(Utility.RandomInt/值)` + `UpdateCurrentInstanceGlobal(<g>)`）**；`QuestNeedsFragmentScript`/`InstanceGlobalProperty` |
| Validate | `Generator.Validate.Quests.cs` | stage index 唯一/遞增、`startUpStage` 至多一個、objective↔stage 連結、**objective target alias 須是同 quest 的 alias**、script ref 存在；**scene action：idle⊕package、package⊕timer 互斥（idle+timerSeconds=pose hold 合法）、至少一個（idle ref 檢查）** |
| Diag | `Diagnostics.Quests.cs` | stages / objectives / aliases / VMAD 腳本 dump |
| Diag | `Diagnostics.Dump.Quest.cs` | quest + scene 結構化完整 dump |
