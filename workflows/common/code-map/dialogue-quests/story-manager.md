# CODE_MAP.dialogue-quests — Story Manager 事件觸發

← [CODE_MAP.dialogue-quests](../CODE_MAP.dialogue-quests.md)

## Story Manager 事件觸發
→ **說明文件**：[SPEC-quests.md § Story Manager](../../../../docs/spec/SPEC-quests.md#story-manager-quests--event-driven-start) · [for_agent.md § 限制](../../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.StoryManager.cs` | `QuestStoryEventSpec`（event + conditions + **`locationFilter[]`（#5 LocType 路由）+ `cooldownHours`（#6 冷卻）**）、`AliasSpec`（fill 模式：fromEvent/forced/uniqueActor/createObject/findMatching/**findMatchingLocation（#7 radiant LocationAlias）/findInLocationAlias（#8 在地點內找 ref）**；findMatching/find* 帶 `Conditions`；alias 腳本 `Script`/`ScriptSource`/`ScriptProperties` = OnActivate 等；**`Packages[]`（editorId/Plugin:0xID）= ALPS alias-override AI 包 → `QuestAlias.PackageData`，radiant escort/travel 真正驅動 NPC 的接線，對齊 vanilla MS13 Camilla alias**）|
| Data | `StoryManagerEvents.cs` | 事件登錄表：KillActor/ChangeLocation/CastMagic/AddItem/Assault/CraftItem/PlayerRemoveItem/Arrest/IncreaseLevel/ScriptEvent — FormKey + 槽名 + **`StoryHandler`（各事件在 Quest 腳本上的 `OnStory<Event>(...)` 簽名，SM encounter 觸發掛這）**；`TryParseFill` / `TryParseCreateObject`（`<ref>@<alias>`）|
| Build P2 | `Generator.Build.StoryManager.cs` | SMBN→SMQN 掛原版事件根；keyword 過濾條件（GetEventData/GetIsID）；**`storyEvent.event=CastMagic` 出 build-time `Warn`**（引擎被動 CastMagicEvent 不對玩家普通施法觸發，in-game 2026-06-20；改用 MGEF→dispatcher→ScriptEvent，見 `skill_cast_spec.json`）；**`BuildQuestAliases(quest,qs,def?)`（在 `Generator.Build.QuestAliases.cs`，連同 `BuildStandaloneQuestAliases`/`AttachAliasScript`）** 共用 helper 建所有 alias fill（fromEvent 僅 `def!=null` 時；createObject = `CreateReferenceToObject` 在 `aliasIdByName` 目標 alias 處生成；findMatching = `QuestAlias.Flag.MatchingRefInLoadedArea`[+`MatchingRefClosest`] + alias.Conditions；**findMatchingLocation（#7）= `Type=Location` + `StoresText` + 匹配 CTDA：`LocationHasKeyword==1`(locType)[+ 父 alias 時 `GetInCurrentLocAlias==1`(LocationAliasIndex=parent)]——byte 對齊 shipping Missives Dungeon 別名（2026-06-21），引擎在 Location 別名忽略 `LocationAliasReference.Keyword` 故 conditions-based；findInLocationAlias（#8）= `Type=Reference` + `LocationAliasReference{AliasID=locAlias, RefType=LCRT}`（對齊 Missives `Target` 別名）；match 條件共用 `WireAliasMatchConditions`（在 `Generator.Build.StoryManager.Encounter.cs`）——**它只 `DeferCondition` 排隊、不當場建**（SM 與 standalone 兩條路都跑在 placements 之前；match filter 合法指 placed ref），排在 fill-shape 條件（`LocationHasKeyword`/`GetInCurrentLocAlias`）之後故發出順序不變**；alias 腳本 `AttachAliasScript` = `QuestAdapter.Aliases` 加 `QuestFragmentAlias`[v5/objFmt2、綁 alias ID、flag=Local]；**`forced:` 立即解析不到（target 晚 build）→ 排入 `deferredForcedAliases`、由 `WireDeferredForcedAliases` 後補**）；**`BuildStandaloneQuestAliases()`** 替非 storyEvent quest 建 alias（def=null，跳 fromEvent）。⚠ ALNA(`FindMatchingRefNearAlias`)離線驗證＝只 `LinkedRefChild`，故 #8 走 `Location` 不走 ALNA；✅ CK 語義已對 shipping Missives byte 驗（2026-06-21，`questdiag` 加 alias dump），剩實機 fill |
| Build P2 | `Generator.Build.StoryManager.cs` | **#5 locationFilter → 在 `quest.EventConditions` 追加 OR'd `GetKeywordDataForCurrentLocation`；#6 `AttachEncounterCooldown`：建 `<quest>_LastFired` float GLOB + 掛 `MFEncounterCooldown` quest script（無條件，prebuilt .pex）**。⚠ `storyEvent.conditions` 與 locationFilter 兩者都走 **`DeferCondition`**（BuildStoryManager 跑在 placements 之前）——排隊順序＝發出順序，`EventConditions` 仍是 [storyEvent 條件…, locationFilter…] |
| Build P2 | `Generator.Build.StoryManager.Encounter.cs` `BuildQuestSpawns` | **#3 `quest.spawn` → 掛 `MFDynamicSpawn` quest script（SpawnForm/Count/Min/MaxDistance/SnapToNavmesh props，merge QuestAdapter）；Build.cs 在 BuildStandaloneQuestAliases 後、WireQuestStages 前呼叫** |
| Asset | `assets/papyrus/MFEncounterCooldown.psc` | **#6 reusable 冷卻（extends Quest，`bool TryFire()` 比 `GetCurrentGameTime - LastFired < CooldownHours/24` → false 中止；由 `<quest>_Stages` 的 OnStory<Event> handler 呼叫，不靠 OnInit）；embed CLI；EE_WITimeout pattern** |
| Asset | `assets/papyrus/MFDynamicSpawn.psc` | **#3 reusable dynamic spawn（extends Quest，`SpawnNow()` `PlaceAtMe`+`MoveTo(+128 Z 落體)` 玩家附近隨機偏移；由 `<quest>_Stages` 的 OnStory<Event> handler〔SM〕或 startUpStage fragment〔StartGameEnabled〕呼叫，不靠 OnInit — OnInit 一生只跑一次，SM relaunch 不重觸發）；embed CLI；spawn pipeline IN-GAME 確認 2026-06-19** |
| Validate | `Generator.Validate.StoryManager.cs` | 事件名合法；**locationFilter keyword CheckRef、cooldownHours>=0**；**`ValidateQuestAlias(q,a,def?,…)`** 共用（storyEvent 與非 storyEvent quest 都驗 alias fill/ref/script；def=null 時 fromEvent 報錯；**findMatchingLocation 驗 LocType keyword + 父 alias 同 quest；findInLocationAlias 驗 location alias 同 quest + refType + 需 refType 或 conditions**）；slot 名稱、ScriptEvent 需宣告 keyword |
| Diag | `Diagnostics.StoryManager.cs` | `smtree`（事件根 SMEN 列舉）/ `smsub <plugin> <rootHex>`（dump 事件根下 SMBN/SMQN）/ **`smcheck <plugin>`（離線檢查 local parent/PreviousSibling graph、routed quest/alias duplicates、LVLN alias shape；有問題 exit 1）** |

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
