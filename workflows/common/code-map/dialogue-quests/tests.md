# CODE_MAP.dialogue-quests — Tests

← [CODE_MAP.dialogue-quests](../CODE_MAP.dialogue-quests.md)

## Tests

| 測試檔案 | 涵蓋 |
|---------|-----|
| `ConditionTests.cs` | CTDA condition 函數、comparator、ref 解析 |
| `ConditionPlacedRefTests.cs` | **condition 的 `param`/`reference` 指 placed ref**（檔內 `placements[]` editorId ／ `references[]` label）在四個 deferred 呼叫點都解得到：perk（trunk＋effect）／storyEvent（＋locationFilter 仍排在後面的順序釘）／quest alias `findMatching` match filter（SM 與 standalone 兩條路）／scene＋phase；外加負向釘（解不到仍警告、perk effect 條件全滅不掛空 `PerkCondition` tab、`IsSceneActionComplete` 的 owningScene 預設仍在）|
| `DialogueTests.cs` | dialogue topic / INFO / greeting 生成 |
| `QuestStageTests.cs` | stage log text / objective fragment / VMAD |
| `InstanceGlobalTests.cs` | **#9 instanceGlobals：fragment source（GLOB property 宣告、RandomInt/SetValue/Update、bind-only、與 objective 共用 stage fragment）+ VMAD GLOB object-property 綁定（fake .pex）+ validate（空 global、單邊 random、random+value 衝突、min>max）** |
| `ObjectiveTargetTests.cs` | objective QSTA target：QOBJ `QuestObjectiveTarget`（AliasID + CompassMarkerIgnoresLocks flag + CTDA）+ validate（target alias 須在同 quest）|
| `SceneTests.cs` | SCEN actor / phase / dialogue action；非對話 action（Package→Packages PACK ref / Timer→TimerSeconds）+ beat phase（無 lines→無 Dialog action/topic）+ LastActionIndex；**fragment-backed action 發 Timer（idle hold / SetStage phase runner；純 build 無 VMAD）**；autoStart → controller VMAD 掛接 + 清 BeginOnQuestStart + 調參 props + **重播策略 props（playOnce/playHour/gateGlobal→GLOB object prop）** + validate gate |
| `SceneFragmentTests.cs` | PlayIdle + restricted SetStage 純產生器（同 phase 合成一個 `Fragment_<phase>`）+ `AttachSceneFragments`（.pex 在才掛 SceneAdapter、OnStart fragment、Actor_/Idle_ 與 SetStageQuest_ object props）+ explicit/host quest binding + validate guards |
| `IdentityTests.cs` | 身份系統：`IdentitySpec` 預設、每身份建 FACT（外部/已宣告不重建）、validate（dup id / bad grant）、acquireBook → `MFIdentityBook` VMAD + 屬性綁定；**default → `MF_IdentityDefaultQuest`（StartGameEnabled）+ `MFIdentityDefault` VMAD，Factions[]/Grants[] list property（只收 default、grant 去重）、無 default 不建、無 grant 省 list**；**activeWhen 窄化正向閘且跑玩家、不污染高優先序排除**；**controller：primaryIdentity 建 MF_PrimaryIdentity/MF_IdentityOverride GLOB + `MF_IdentityControllerQuest`（Codes[]、無 default 不建 granter）、global-based primary CTDA、setPrimaryIdentity TIF**；**grantPerks → 書綁 GrantPerk[0] + default quest 綁 Perks[]**；**autoGrantWhen → `MF_IdentityAutoGrantQuest` + Factions[]/AvNames[]/Thresholds[] 平行、無 autoGrant 不建** |
| `StoryManagerBuildTests.cs` | SM build pass 2（SMBN/SMQN 掛接、alias fill 接線、alias 腳本 VMAD 掛接、`startUpStage` QSDT flag、stage fragment + alias 腳本共存於單一 adapter、非 storyEvent quest 也建 forced/createObject alias + 腳本）|
| `StoryManagerDiagnosticsTests.cs` | `smcheck` 純離線檢查：正常 generated/binary roundtrip、parent/sibling orphan/cycle/duplicate、quest route/alias ID 與 local LVLN alias shape |
| `RadiantAliasTests.cs` | **#7 findMatchingLocation（Type=Location、StoresText、`LocationHasKeyword` CTDA [+父時 `GetInCurrentLocAlias`]、無 Location 子記錄）+ #8 findInLocationAlias（Type=Reference、Location.AliasID/RefType、conditions）build + validate（未知 keyword/parent/location alias、自指、缺 refType+conditions）** |
| `StoryManagerEventsTests.cs` | 事件登錄表欄位（FormKey / slot 對應）|
| `StoryManagerEventsMoreTests.cs` | 擴充事件（ChangeLocation/CastMagic/AddItem/Assault/ScriptEvent）|
| `EncounterRoutingTests.cs` | **#5 locationFilter（GetKeywordDataForCurrentLocation、OR 群組）+ #6 cooldownHours（LastFired GLOB + MFEncounterCooldown script + props）+ LocAliasHasKeyword condition + validate（負 cooldown、空 keyword）** |
| `DynamicSpawnTests.cs` | **#3 quest.spawn → MFDynamicSpawn script（SpawnForm/Count/Min/MaxDistance/SnapToNavmesh props）+ 與 cooldown/locationFilter 共存單一 adapter + validate（空 form、count<1、min>max）** |
| `StoryManagerValidateTests.cs` | SM validate（事件名、alias 語法、keyword 要求）|
| `WordWallTests.cs` | word wall 教字 quest fragment（⚠️ 需本機 Skyrim.esm）|
