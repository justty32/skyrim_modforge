# CODE_MAP.dialogue-quests — Conditions・ScriptEvent・Papyrus 腳本・Word Wall

← [CODE_MAP.dialogue-quests](../CODE_MAP.dialogue-quests.md)

## Conditions（CTDA 條件）
→ **說明文件**：[SPEC-dialogue.md § conditions](../../../../docs/spec/SPEC-dialogue.md#conditions--ctda-gates-on-a-dialogue-info-a-banter-info-or-a-package)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Build P2 | `Generator.Build.Conditions.cs` `BuildCondition` ＋ `Generator.Build.Conditions.Functions.cs` `BuildConditionData` | 所有 CTDA 的 ref／比較／run-on 組裝 + function dispatch（dialogue / stage / banter / package / perk / SM / alias / scene / objective / recipe 共用）；163 行 dispatch 依 partial 命名拆到 `.Functions.cs`。🔑 **鐵律（檔頭寫死）**：CTDA 的 `param`／`reference` 是**任意 ref**，可以合法指一個 **placed ref**（檔內 `placements[]` editorId 或 `references[]` label）；那些 editorId 到 `BuildPlacements`/`BuildReferences`（Build.cs pass-2 ~115-117）才進 ref 表 ⇒ **任何跑在 `BuildReferences` 之前的步驟一律不准直接呼叫 `BuildCondition`**，必須用 `DeferCondition()` 排隊。`refsIndexed` 旗標（`BuildReferences` 開頭設起）是**守門員**：早於它呼叫就吐 `BUILD-ORDER BUG` 警告——不要消音，去 defer。**地點感知（#5）：`GetKeywordDataForCurrentLocation`（玩家當前地點 LocType）/`LocationHasKeyword`/`LocAliasHasKeyword`（location alias 的 keyword，hold 偵測，需 owning quest alias index，仿 GetIsAliasRef）。****`GetIsAliasRef`**：用 `alias`（owning quest 的 alias 名）→ alias index，由各 quest-scoped 呼叫點傳入 `aliasIndexByName`。**`IsSceneActionComplete`**：`BuildCondition` 第 4 參 `owningScene` FormKey，scene-cond 呼叫點（`WireScenes` 的 sceneConditionWires）傳 `scene.FormKey`，author 可用 `c.Scene` 覆寫；`c.SceneActionIndex` 必填。package/perk 等無 scene/quest context → 傳 null → 警告丟棄。**`GetVMQuestVariable`/`GetVMScriptVariable`（L組）**：condition 讀 Papyrus property，`param`=quest（VM-quest）/object（VM-script）、`c.VariableName`=property 名字串（verbatim 寫進 CTDA，引擎期望的 bare-name vs `::Prop_var` 待 xEdit/實機驗）|
| Build P2 | `Generator.Build.Conditions.Wire.cs` `DeferCondition` / `DeferConditionFinalizer` / `WireDeferredConditions` | **deferred-condition 佇列**（上面那條鐵律的落實）。早於 placements 的四個呼叫點（`WirePerks` / `BuildStoryManager` / `BuildQuestAliases`＝SM＋standalone 共用的 `WireAliasMatchConditions` / `WireScenes`）把 `ConditionSpec` 連同 target `IList<Condition>`＋label＋aliasIdx＋owningScene 排進 `deferredConditionWires`；`WireDeferredConditions`（Build.cs 排在 `WirePackageConditions` 之後、`WireDeferredScriptObjectProps` 之前，即 placements＋labels 都在了）依**排隊順序**排空 ⇒ 每個 target list 的 CTDA 順序與舊的 eager 版**逐位元相同**。`DeferConditionFinalizer` 給「條件全建不出來就不該掛的容器」用（perk effect 的 `PerkCondition` tab——vanilla 不發空 PRKC）|
| Validate | `Generator.Validate.Helpers.cs` | `CheckCondition`（function / comparator / ref）|
| Diag | `Diagnostics.Dialogue.cs` | condition 欄位 dump |

---

## ScriptEvent 自訂觸發
→ **說明文件**：[SPEC-quests.md § ScriptEvent](../../../../docs/spec/SPEC-quests.md#scriptevent--sending-your-own-story-events)

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
→ **說明文件**：[SPEC-quests.md § scripts](../../../../docs/spec/SPEC-quests.md#scripts--papyrus-attachment)

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
