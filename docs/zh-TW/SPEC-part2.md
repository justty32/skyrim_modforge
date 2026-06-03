<!-- 第 2/5 部分 — 類別至 Papyrus 腳本 -->
### classes（CLAS）
NPC 的「職業」— 將 npc 的 `class` ref 設定為其中之一。它驅動角色的屬性分配和偏好技能。
```jsonc
{ "editorId": "MF_Battlemage", "name": "ModForge Battlemage",
  "teaches": "Destruction",
  "maxTrainingLevel": 50,
  "healthWeight": 30, "magickaWeight": 50, "staminaWeight": 20,
  "skillWeights": { "Destruction": 100, "Restoration": 75, "OneHanded": 50 } }
```
技能名稱：`OneHanded`、`TwoHanded`、`Archery`、`Block`、`Smithing`、`HeavyArmor`、`LightArmor`、`Pickpocket`、`Lockpicking`、`Sneak`、`Alchemy`、`Speech`、`Alteration`、`Conjuration`、`Destruction`、`Illusion`、`Restoration`、`Enchanting`。職業只有在 npc 具有 **`level` > 0 且 `autoCalcStats: true`** 時才會驅動 NPC 的實際屬性/技能值——否則引擎使用固定預設值（未設定的 NPC 無論職業如何都讀取 50/50/50）。

### dialogue（對話）
一個 `dialogue` 條目是顯示在任務分支下的玩家話題，可選擇性限定於一個說話者 NPC（一個 `GetIsID` 條件）。`questEditorId` 必須指向此 spec 中的一個任務；`prompt` 為玩家的台詞；`responses` 為 NPC 的口語回應。

從一個 `dialogue` 條目，建置時會發出**完整的 vanilla 鏈**，使話題在遊戲中實際出現（已確認 It.23，SSE 1.6.1170）：
- **Topic**（`Custom`，`SNAM='CUST'` — null 子類型在載入時 crash）+ **Branch**（`TopLevel`，Player）+ 攜帶回應的 **INFO**。每個 INFO 獲得 `ENAM`（旗標）+ `CNAM`（好感等級）— **沒有 `ENAM` 的 INFO 被視為無效，其話題會從選單中靜默捨棄**；
- 每個任務對應一個 **DialogView（DLVW）**，將其分支綁定到任務；
- 每個說話 NPC 對應一個 **Hello** info（`Misc`/`Hello`/`SNAM='HELO'`），使 NPC 完全*可對話* — 使用 `npc.greeting` 設定台詞。

**結果 fragment（選擇台詞時執行某些操作）。** 對話選擇只能透過 Papyrus fragment *執行動作*。設定 `resultScript`、`resultScriptSource`（`.psc`，由 `package` 編譯）和 `resultProperties`。建置時附加 INFO 的 `OnBegin` fragment VMAD。設定 `goodbye: true` 可在台詞後關閉選單。

> **三個執行時需求（非記錄錯誤）：**（1）對話僅在**遊戲載入**時註冊 — 在主選單使用 `coc` 或在會話中使用 `startquest` 會使 NPC 保持沉默。（2）將說話者放置在真實的房間座標上 — 位於 cell 原點 **(0,0,0)** 會落在導航網格外。（3）無語音台詞閃過；安裝 **Fuz Ro D-oh**（或打包無聲的 `.fuz`）並啟用字幕。見 `lifelike/gotchas.md`。

### banter — 主動（未受提示）的 NPC 台詞
一個 `banter` 條目是 NPC **自行說出**的台詞，沒有玩家選單——vanilla 跟隨者評論模式（`HirelingIdles`）。共用相同（說話者、任務）的所有 banter 條目會折疊為**一個環境話題**——Category=Misc，SNAM=`IDLE`，無分支——每個條目對應一個標記為 **Random** 的 INFO；引擎隨機挑選一個當前 `conditions` 通過的 INFO 並播放。**觸發需求：** 說話者必須啟用閒聊——具有 `AllowIdleChatter` 中斷旗標的 AI 套件（`Sandbox` 套件或 vanilla 跟隨套件）。見 `examples/follower_vanilla_spec.json`。

### scenes — 兩個 NPC 互相交談（SCEN）
一個 `scene` 是 NPC 之間（非玩家）的腳本對話——vanilla 的 **Scene** 記錄。場景由**任務宿主**，其參與者是該任務的**別名**，並按順序播放**階段**清單，每個階段說一句話。

```jsonc
{ "editorId": "MF_TavernArgument",
  "questEditorId": "MF_SceneQuest",
  "beginOnQuestStart": true,
  "stopQuestOnEnd": false,
  "actors": [
    { "aliasId": 0, "npc": "MF_Borin", "name": "Borin" },
    { "aliasId": 1, "npc": "MF_Hilda", "name": "Hilda" } ],
  "phases": [
    { "speaker": 0, "emotion": "Anger",   "lines": [ "You still owe me for the ale, Hilda." ] },
    { "speaker": 1, "emotion": "Disgust", "lines": [ "Owe you? That swill wasn't worth a clipped septim." ] },
    { "speaker": 0, "emotion": "Anger",   "lines": [ "Watch your tongue, or there'll be trouble." ] },
    { "speaker": 1, "emotion": "Happy",   "lines": [ "Ha! Buy me a drink and we're even." ] } ] }
```
從這一個條目，建置時會發出**完整的 vanilla 鏈**——每個角色對應一個 **QuestAlias**（以 `UniqueActor` 綁定到指定 NPC）；一個 **Scene（SCEN）**（其 `SceneActors` 參照**別名索引**）；每個階段對應一個 **Scene 子類型 DialogTopic**（Category=Scene，SNAM=`SCEN`）+ **INFO**。

> **執行時需求（非記錄錯誤）：**（1）兩個 NPC 必須**放置在彼此附近** — 在**同一個 cell** 中。（2）與所有任務對話一樣，場景只在**遊戲載入**時載入。（3）無語音台詞閃過；安裝 **Fuz Ro D-oh**。**狀態：僅限結構**——`build`/`validate`/`dump` 已對照 vanilla 場景結構驗證；**尚未在遊戲中確認。** 見 `examples/scene_spec.json` 和 `lifelike/cookbook.md`。

### conditions — CTDA 閘門（在 `dialogue` INFO、`banter` INFO 或 `package` 上）
條件是**靜態閘門資料**，因此它存在於 spec 中（邏輯仍屬於 Papyrus）。`dialogue[].conditions` 和 `packages[].conditions` 採用相同的結構：
```jsonc
{ "function": "GetItemCount",          // form-arg: HasPerk | GetInFaction | GetItemCount | GetGlobalValue | GetStage | GetIsID | GetRelationshipRank
  //                                    // actorValue-arg: GetActorValue | GetActorValuePercent (0..1 fraction)
  //                                    // no-arg situational: GetCurrentTime (hour 0..24) | IsInInterior | IsInCombat | GetRandomPercent (0..99) | TemperIsEnchanted
  "comparison": ">=",
  "value": 500,
  "param": "Skyrim.esm:0x00000F",      // the function's form arg (faction/item/global/quest/npc) as a ref
  "actorValue": "",                    // for GetActorValue/GetActorValuePercent instead of param
  "runOn": "Reference",                // whose value: Subject (default) | Reference | Target | CombatTarget | ...
  "reference": "Skyrim.esm:0x000014",  // the ref read when runOn=Reference (here, the player)
  "or": false }                        // OR with the NEXT condition (default AND)
```
一個 `dialogue` INFO 已自動攜帶 `GetIsID` 說話者閘門；這些條件會被附加上去。典型的跟隨者用途：隱藏付費招募台詞，除非（玩家）`GetItemCount Gold >= 500` **且** `GetInFaction CurrentFollowerFaction == 0`；在 `GetInFaction CurrentFollowerFaction == 1` 條件下開啟 Follow 套件，使其僅在招募後執行。見 `examples/follower_paid_spec.json`。

### 任務階段、日誌條目與目標連結

任務的 `stages[]` 是任務可被**設定到**的整數里程碑（10、20、30…）。每個階段可選擇性地寫入一條**日誌條目**，並可攜帶任務狀態旗標。目標會隨著階段設定而顯示與完成；一條 `dialogue` 對話選項在被選取時可以推進階段。

```jsonc
"quests": [{
  "editorId": "MF_ErrandQuest", "name": "A Forged Errand",
  "startGameEnabled": true, "priority": 60,
  "stages": [
    { "index": 10, "logEntry": "Joren asked me to retrieve his lost hammer." },
    { "index": 20, "logEntry": "I agreed to help. Time to search the riverbank.",
      "conditions": [ { "function": "GetStage", "comparison": "GreaterThanOrEqualTo",
                        "value": 10, "param": "MF_ErrandQuest" } ] },
    { "index": 30, "logEntry": "I returned the hammer. Done.", "completeQuest": true }
  ],
  "objectives": [
    { "index": 10, "text": "Agree to help Joren", "showStage": 10, "completeStage": 20 },
    { "index": 20, "text": "Find Joren's hammer",  "showStage": 20, "completeStage": 30 }
  ]
}]
```
- **`stages[]`** — `index`（唯一，**遞增**），`logEntry`（日誌文字；省略則為靜默里程碑），`completeQuest` / `failQuest`（設定 QuestLogEntry 旗標，當此階段到達時關閉／失敗任務——最多一個），`conditions`（可選的 CTDA 日誌條目門檻）。
- **`objectives[].showStage` / `.completeStage`** — 在 `showStage` 時呼叫 `SetObjectiveDisplayed`，在 `completeStage` 時呼叫 `SetObjectiveCompleted`。`-1`（預設值）表示「未連結階段」。
- **`dialogue[].setStage`** — 選取該對話主題時，會將宿主任務推進到此階段。

**哪些是純記錄資料，哪些需要 Papyrus：** 階段、日誌條目、`completeQuest`/`failQuest` 旗標及日誌條目條件都是**純記錄資料**——它們可以順利建置，引擎可直接讀取。但在階段設定時*顯示*目標，以及從對話行*推進*階段，則需要 **Papyrus fragments**。`package` 指令可端對端處理此事（**無需 CK，已在遊戲中確認 It.36 2026-06-02**）：

1. 產生 `Scripts/Source/<quest>_Stages.psc` — 每個階段對應一個 `Fragment_Stage_XXXX_Item00000()` 函式，用於顯示／完成目標（CK 標準命名；引擎在 `SetStage()` 觸發時呼叫它）。
2. 產生 `Scripts/Source/TIF_<dialogue>.psc` — `extends TopicInfo Hidden`，帶有一個明確的 `Quest Property OwningQuest Auto`，綁定至任務的 FormKey；`Fragment_0` 呼叫 `OwningQuest.SetStage(N)`。使用 `OnBegin`（在玩家選取該行時觸發）。**請勿使用 `GetOwningQuest()` — 它對遊戲載入時的 StartGameEnabled 任務會回傳 None。**
3. 使用 Linux 原生的 `papyrus-compiler` 將兩個 `.psc` 編譯為 `.pex`（備用方案為 Wine/CK）。
4. 將 VMAD 附加至 QUST（需要 `QuestScriptFragment.Unknown2=1` — 啟用旗標）以及 INFO（`DialogResponsesAdapter`，`OnBegin`）。
5. 在每一條 `setStage` 對話行上自動加入 `GetStage(quest) < setStage` 條件，使 NPC 在玩家已選取後不會重複觸發。

使用 `questdiag <plugin> <0xFORMID>` 可檢查任何任務。對話仍然只在遊戲**載入**時才會登錄。完整範例：`examples/quest_stages_spec.json`。

### scripts — Papyrus 附加
```jsonc
{
  "targetEditorId": "MF_Q1",          // 要附加的記錄（spec 中的任意 editorId）
  "scriptName": "MFDemoQuestScript",  // 必須與 .pex/.psc 的 Scriptname 相符
  "source": "scripts/MFDemoQuestScript.psc",  // 可選：.psc 路徑（相對於此 spec）
  "properties": [
    { "name": "GreetingCount", "type": "int",    "int": 3 },
    { "name": "PlayerRef",     "type": "object", "objectEditorId": "MF_Smith" }
  ]
}
```
- 屬性 `type` ∈ `int | float | bool | string | object`。設定對應的值欄位：`int` / `float` / `bool` / `str`，或 `objectEditorId`（用於 `object`，解析為 FormLink）。屬性被標記為 *Edited*，以便遊戲讀取。
- 附加適用於任何支援腳本的記錄（Quest、Npc、Activator、MagicEffect、Weapon、Armor、MiscItem、Book、Ingestible 等）。腳本 `Name` 必須與編譯後的 `.pex` 相符。
