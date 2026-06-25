# Tundra Defense SSE (Trainwiz, Nexus mod 14310, v1.04) — 「飲一瓶建材 potion → PlaceAtMe + 進入定位模式」的整套自建聚落／募兵／守城系統

調查 idea #22「漂泊開拓慢活：建立並經營一個聚落」最直接的既有藍圖。Tundra Defense 是經典的「自己蓋據點 → 募守衛/居民 → 擋一波波攻擊」mod，正面覆蓋剛 landed 的 [`settlements:`](settlement-npc-expansions.md) macro **沒有**碰的「**蓋（build）／管（manage）／守（defend）**」這一維。**這是 blueprint-extraction，不是 triage。**

## Scope / sources

| 項目 | 值 |
|------|------|
| Archive | `~/skyrim_mods/hdd/Tundra Defense SSE v1.04-14310-1-04.zip`（英文版；另有 `…cht-14501…` 中文版，本檔用英文）|
| 解壓 | `~/skyrim_mods/unzip/TundraDefense/` |
| Plugin | `Tundra Defense SSE.esp`，485 KB，**2291 records**，master = **Skyrim.esm only**（無 USSEP、無 DLC）|
| 隨檔 | `Tundra Defense SSE.bsa`（26 MB：56 個 `.pex` script + 全部 `Meshes/TundraDefense/*.nif` 自製建物模型 + 1 raid 音效 + 3 段 SpecialPlans 語音/lip）、`… - Textures.bsa` |
| **Source `.psc`** | **無**（BSA 內只有編譯後 `.pex`，零 `.psc`）。系統無 Champollion 可反編譯 → 下列機制以「**`.pex` 字串表逆讀**（object/property/function 名 + 引用的 record editorId + Notification 字面）＋ ModForge `dump`/`questdiag`」為據。凡靠 `.pex` 字串表推得而未見原始碼者標 **(pex-strings)**；真正不確定者標 **UNVERIFIED**。|
| MCM / loose config | **無**——BSA 內無任何 `.json`/`.ini`/`SkyUI`/`MCM-Helper` 檔。所有「設定」走遊戲內 MESG 選單（見 §5）。|
| EditorID 前綴 | 一律 `aaaFort*`（mod 內部代號 = "Fort"/"Outpost"）|

抽 `.pex`：用 repo 內 `sub_projs/sofia-patch/vigilant-reconstruction-redo/_tools/bsa_reader.py`（7z 開不了此 BSA）+ 自寫 Skyrim `.pex` 字串表 parser（magic `0xFA57C0DE`，big-endian）。記憶體鐵律遵守（只走 CLI lazy overlay，未整載任何主檔）。

## 1. Classification

- **類型**：**自建聚落／據點經營＋波次守城（base-building + tower-defense）**——玩家從零放下一個 Water Well「核心」，自由擺放建物/城牆/陷阱/守衛，再手動或隨機觸發一波波敵人來襲。**不是內容型（無地點/角色弧），是純系統型 sandbox。**
- **Plugin**：是，單一 ESP，Skyrim.esm-only。**SKSE 依賴**：`aaaFortPlayerQuestScript` 內有 `iGetKeyPressed` / `aiDXScanCode` / `BindKey`（自製 keybinder）**(pex-strings)** → **需 SKSE**（DXScanCode 鍵盤輪詢是 SKSE-only）；建造輸入另用 4 個自製 Voice/Concentration Spell（見 §3）作為不依賴 SKSE 的後備確認鍵。
- **敘事價值**：**無**（generic 募兵/管理選單對白 + 3 段 SpecialPlans 語音，無角色弧）。
- **系統價值（對 idea #22）**：**最高**。`settlement-npc-expansions` / `populated-skyrim-family` / `cutting-room-floor` 都只解「**住滿既有聚落**」；**Tundra Defense 是唯一一個把「玩家**從無到有蓋出**據點 + 經營 + 防守」整套做出來的範本**，正是 #22 「漂泊開拓慢活」的核心動詞（build/manage/defend），也是 `settlements:` Phase-2 的活樣本。

## 2. Record shape（`dump` tally，未整載）

| record | count | 角色 |
|--------|------:|------|
| **PlacedObject (REFR)** | 1611 | 預擺的 idle marker / spawn point / 核心 cell 內裝 + tutorial 佈景 |
| **Npc (base)** | 122 | 守衛各種族版 + citizen/miner/scavenger/trader + **`Raider*` 攻擊者 base**（Bandit/Draugr/Falmer/Nature/Giant/Necro/Vampire/Werewolf/Dragon…）|
| **MagicEffect** | 113 | **核心**：絕大多數是 `aaaFort…Effect "Construct X"`（script archetype，掛 spawner script）|
| **Ingestible (ALCH)** | 109 | **核心**：`aaaFort…Plan "Plans: X"`——玩家「**喝**」的建材物品（FOOD 分類），每瓶帶一個 "Construct X" 魔法效果 |
| **Activator (ACTI)** | 108 | 建好的**可放置物本體**（`aaaFortWaterWell`/`Barracks`/`SpikeWall`/家具/rug…，掛互動 script）|
| **Message (MESG)** | 87 | **整套 UI**：管理選單 / 募兵選單 / raid 難度選單 / breakdown 確認…（**無 MCM，全靠 MESG**）|
| **Package** | 19 | 守衛巡邏/站哨 + citizen sandbox |
| Furniture | 17｜**PlacedNpc (ACHR)** 12 | 預擺工作家具；12 個具名 NPC（Sareio 市集主、Hank、Jorane、OutpostDogmeat 等核心服務角色）|
| Cell | 11 | 自家 interior（`aaaFortGrandEstateInterior` / `aaaFortWoodenCabinInterior` 等可建房屋內部）|
| Container | 10 | 計畫書箱 `aaaFortPlansContainer` / 商人箱 / 礦石箱 / 守衛裝備箱 / scavenger crate |
| FormList | 8 | `aaaFortGuardList` / `aaaFortStationaryGuardList` / `aaaFortPatrolGuardList` / `aaaFortMarketList` / `aaaFortFarmItems`… |
| Quest | 7 | 見 §3/§4（全是無/少文字的**控制器 quest**）|
| LeveledNpc | 6 | `aaaFortLCharGuardOrc/Nord/Altmer/Imp/Dunmer/Red`（守衛隨機等級池）|
| Faction | 5 | `aaaFortFaction`(Outpost)・`aaaFortRaiderFaction`・`aaaFortFollowPlayerFaction`・2 個 Vendor faction（市集/雜貨）|
| Keyword | 7 | `aaaFortGuard`/`aaaFortStationaryGuard`/`aaaFortPatrolGuard`/`aaaFortPlans`/`aaaFortObjectCloningKey`/`aaaFortDragonPerchKeyword`/`aaaFortVioletKeyword` |
| Book | 7 | 觸發書（`aaaFortScavengerNote`/`MineNote`/`ForgeNote`/`GeneralStoreNote`…，讀了解鎖對應子系統）|
| **Shout / WordOfPower** | 1 / 1 | `aaaFortInputConfirmShout "Confirm Construction"`（建造定位時的確認輸入）|
| Weapon | 1 | `aaaFortObjectCloner "Object Cloner"`（複製已建物的工具）|
| **GlobalVariable** | **0** | **完全沒有 GLOB**——所有計數/設定都是 **Quest script property**（`CitizenCount`/`GuardCount`/`MinerCount`/`ScavengerCount`/`CurrentRaiders`/`RaidFrequency`/`raidHandicap`/`DayCount`…）|

讀法：**核心三件套 = 109 Ingestible（建材瓶）↔ 113 MagicEffect（"Construct X"）↔ 108 Activator（建物本體）**，幾乎一一對應。87 MESG = 整個 UI 層。0 GLOB = 狀態全活在 quest script 上。

## 3. Build system mechanism（核心——放置 + 持久化）

**放置一個建物 = 喝一瓶「Plans: X」Ingestible → 觸發其唯一 MagicEffect "Construct X"（script archetype）→ spawner script `PlaceAtMe` 出對應 Activator → 進入「定位模式」由 `aaaFortMainQuestScript` 即時跟著玩家視線移動，按確認鍵落地。**

### 3.1 建材 = Ingestible（potion），不是 MISC、不是 crafting station

`dump` 證實：每個建物都是一個 **Ingestible**（ALCH）`aaaFort…Plan`，Name 形如 `"Plans: Spike Wall"` / `"Plans: Water Well"` / `"Plans: Barracks"`。tutorial quest（`questdiag 0x002853`）的 log 字面坐實玩家流程：

> "open your inventory under the **FOOD** category and use 'Plans - Water Well'"

→ 「喝」這瓶 potion 觸發它掛的 script-archetype MagicEffect。`dump` 列出全部 113 個 MagicEffect 幾乎都是 `aaaFort…Effect "Construct X"`，archetype=**Script**，各掛一個 spawner script（見下）。計畫書本身存在 `aaaFortPlansContainer "Plans Chest"`（玩家從市集/箱子取得）。

### 3.2 spawner script 家族（掛在 MagicEffect 上，`OnEffectStart` 出物）

`dump` 顯示每個 "Construct X" MGEF 的 `script:` 欄就是下面其一（property 數隨建物異）：

| spawner script (pex) | 出什麼 | 關鍵 property/call (pex-strings) |
|------|------|------|
| `aaaFortObjectSpawnerScript` | 單一 Activator（牆/門/forge/barracks…）| `SpawnRef`(activator) / `SpawnContainerRef` / `SpawnFurnitureRef` / `SpawnDoorRef` / `ActorOnPlace`(actorbase) / `rotateOnSpawn` / `distanceOnSpawn` / `isArcane` / `isMedic`；call **`PlaceAtMe` → `StartPositioning`** |
| `aaaFortMultiObjectSpawner` | 多選一（`activator[]` + `RandomInt`）→ farmhouse/townhouse/naturehouse 隨機外觀 | `SpawnRef`(activator[]) / `Spawn` / `RandomInt`；同樣 `PlaceAtMe`→`StartPositioning` |
| `aaaFortBoundarySpawner` | 邊界/市集/巡邏 marker（`MoveTo`+`Enable` 而非 PlaceAtMe）| `IsMarket` / `setMarket` / `collisionRef` / `DebugMode`；call **`MoveTo`+`Enable`→`StartPositioning`** |
| `aaaFortTrapSpawner` | 陷阱（bear trap）——**直接落在腳下，不進定位模式** | call `PlaceAtMe` → Notification "A trap has been placed at your feet." |

→ 共同骨架：`OnEffectStart` → `Game.GetPlayer()` → `PlaceAtMe(SpawnRef)` 生出 disabled/holding 的 ref → 把該 ref 交給 `aaaFortMainQuestScript`（property `MainQuest`）的定位狀態機。Notification "You are already in object placement mode!" 證明同時只能擺一個（`instantReuse` 旗標控制連續擺放）。

### 3.3 定位模式 = `aaaFortMainQuestScript`（即時 follow + 旋轉/距離 + 確認/取消）

這支是放置控制器（pex-strings 完整）：state 機 `MODE_DISTANCE`/`MODE_ROTATE_X/Y/Z`/`MODE_RESET`/`AXIS_X/Y/Z`，function `StartPositioning` / `UpdateObjectPosition`(`OnUpdate` 每 tick 用 `sin/cos` 把 ref `TranslateTo`/`MoveTo` 到玩家前方 `placeDistance`) / `ChangeRotationAxis` / `ResetObjectPosition` / `ConfirmPlacement` / `CancelPlacement`。輸入靠：

- 4 個自製 **Spell**（`dump`）：`aaaFortInputConfirmSpell "[Construction] Confirm"`（type=**Voice**，配 Shout `aaaFortInputConfirmShout "Confirm Construction"` / Word "Go"）、`…CancelSpell`（Voice）、`…PlusSpell`/`…MinusSpell`（Concentration）——定位時這些 spell 被加到玩家手上，喊 Shout 或施法即送出確認/取消/加減。
- `aaaFortSpellControls`（掛 Plus/Minus effect）OnEffectStart 把 `inputPlus`/`inputMinus` 旗標推給 `aaaFortMainQuestScript`（property `placementManager`）；無建造中時 Notification "You are not building anything right now!"。
- `aaaFortKeyRebinderScript` + `aaaFortPlayerQuestScript.BindKey`/`iGetKeyPressed`：SKSE DXScanCode 重綁主選單熱鍵（預設 `'` apostrophe，見 tutorial log）。

確認後 `ConfirmPlacement` → `Enable` 該 ref、`AddToFaction(FortFaction)`、若 `ActorOnPlace` 非空則 `PlaceActorAtMe`（建物附帶住戶，如 barracks 附守衛）。

### 3.4 持久化跨存檔 = **Enable 後的具名 REFR 留在世界 + quest script counter，不是 token 容器**

放下的建物就是一個被 `Enable` 的**真實 REFR**，永久存在玩家所在 cell（多在 Tamriel/玩家自選地點，非預定 cell）。`aaaFortMainQuestScript` 持有所有計數 property（`CitizenCount`/`GuardCount`/`MinerCount`/`ScavengerCount`/`hasArmory`/`hasMedic`/`hasArcane`/`hasPerch`/`hasGrandEstate`…）+ 各種 RefHolding/marker reference（`WaterWellRef`/`ScavengerCrateRef`/`MinerContainer`/`GuardEquipmentBox`…）。**搬移**走 `aaaFortMoveObjectScript`（OnActivate 潛行 → `MoveDialog` → `StartPositioning` 重新定位）；**拆除**走 `aaaFortBreakdownScript`（OnActivate 潛行 → `BreakdownDialog` 確認 → `Delete` 該 ref）。`aaaFortObjectCloningScript`（掛 ReferenceAlias，`OnHit` + keyword `aaaFortObjectCloningKey`）+ Weapon `aaaFortObjectCloner` 提供「敲一下複製已建物」。

→ **持久化＝「Enable 一個真 REFR + quest-script 上的 counter/RefHolding」**，無 JContainers、無 token-in-chest 序列化。代價：建物散落玩家當時的 cell（不集中在自家 worldspace），靠 boundary marker + travel marker（alias `aaaFortTravelMarker` ForcedRef `0x004404`）界定「我的據點」範圍。

## 4. Recruitment + defense mechanism（募兵 + 守城）

### 4.1 募兵 / 雇工 = 「付 Gold → `PlaceActorAtMe` → `AddToFaction` → `SetPlayerTeammate`」（無對白樹、無 alias fill）

統一配方（pex-strings，散見多支 script）：互動建物（barracks/house/mine/market…）OnActivate → MESG 選單 → `Player.RemoveItem(Gold, cost)` → `PlaceActorAtMe(ActorBase)` → `AddToFaction(aaaFortFollowPlayerFaction)` + `SetPlayerTeammate(true)` + `EvaluatePackage`，並 `++Count`（cap 檢查）：

| 角色 | script | 來源建物 / 機制 |
|------|------|------|
| **守衛 Guard** | `aaaFortGuardScript` | barracks 募；分 `NordVersion`/`OrcVersion`/`AltmerVersion`/`Redguard`/`Dunmer`/`Imp` 種族版 + `PatrolVersion`/`StationaryVersion`/`PerimeterVersion`；`ChangeGuardDialog`/`ChangeGuardRaceDialog` 付 `changeGuardCost` 換種族；`OnDeath` → `--GuardCount`；`hasMedic` 時站 medic tent `RestoreActorValue(health)` 自癒 |
| 站哨守衛 Stationary | `aaaFortStationaryGuardScript` | 綁固定 `posx/y/z`（`SetPosition` 釘在哨點）；用 `aaaFortStationaryGuardPackages` FormList |
| 守衛隊長 Captain | `aaaFortGuardArmoryScript` | `GuardCaptainDialog` 付 `changeCost` 換 `CaptainA/B/C` |
| **居民 Citizen** | `aaaFortCitizenScript` + `aaaFortHouseScript` | house OnActivate → `HouseDialog` → 付 `citizenCost` → `PlaceAtMe(Citizen)`，受 `maxCitizens` cap；居民只是「活感」人口（`OnDeath` → `--CitizenCount`）|
| **礦工 Miner** | `aaaFortMinerScript` + `aaaFortMineExpansionScript` | mine 募，產 ore 進 `MinerContainer`/`aaaFortMinerCrate`（`OreIron`/`OreSilver`/`OreEbony`… 多種 property）|
| **拾荒者 Scavenger** | `aaaFortScavengerScript` + `aaaFortScavengerCrateScript` | crate OnActivate → `ScavengerMenu` → 付 `ScavengerCost` → `PlaceAtMe(Scavenger)`；scavenger 帶物回 `ScavengerCrateRef` |
| 市集商人 / 旅行商人 | `aaaForttraderchestscript` / `aaaForttravelingmerchantscript` | Vendor faction `aaaFortFactionVendor`（HiddenFromPC+Vendor，sellBuyList `aaaFortMarketList`，merchantContainer `OutpostMarketRef`）；旅行商人定時 `Delete` 自己 |

→ **募兵全程程序化（PlaceActorAtMe + faction + teammate），零 dialogue INFO、零 quest alias**。守衛/居民死了就 `--Count` 並 `Delete`。

### 4.2 守城 / 波次 = `aaaFortPlayerQuestScript`（玩家自觸發或隨機；`PlaceActorAtMe` at boundary markers）

這支同時是 **UI + raid 引擎**（pex-strings 完整）。Raid 流程：

1. 玩家按熱鍵開 `PlayerMenu "Outpost Menu"` → `RaidMenu`/`RaidMenuB`/`RaidMenuC "Manage Raids"` 選 raid 類型 + 難度（`RaidDifficultyMenu`）。
2. 每種 raid 對應一組 `Raider*` **ActorBase 陣列**（property `ActorBase[]`）：`BanditRaid`/`DraugrRaid`/`DragonRaid`/`NatureRaid`/`GiantRaid`/`NecroRaid`/`VampireRaid`/`WerewolfRaid`/`FalmerRaid`/`RandomRaid`，各含分工 base：`RaiderBanditMelee`/`Ranged`/`Wizard`/`Boss`、`RaiderDraugrMeleeM/F`/`Ranged`/`Wizard`/`Boss`、`RaiderNatureBear`/`Wolf`/`Sabre`/`Troll`/`Spider`/`ChaurusReaper`/`IceWraith`、`RaiderGiantA/B`/`Leader`/`Mammoth`、`RaiderDragon` 等。
3. 生怪：`OnUpdate` 迴圈 `RandomInt(min,max)` 決定數量（受 `difficulty`/`raidHandicap`/`GuardCount` 調整）→ **`PlaceActorAtMe(Raider base)` at `BoundaryMarkerA/B/C/D`**（在玩家先前擺的邊界 marker 處刷怪）→ `AddToFaction(aaaFortRaiderFaction)`（與 FortFaction/守衛敵對）。`CurrentRaiders` 計數，清完 → `EndRaid`/`RaidEnded` + 播 `RaidSound`（`aaaFortRaidSoundEffect` / `sound/fx/.../raid.xwm`）。
4. **隨機襲擊**：`RandomRaidEnabled` + `RandomFrequency`（`RaidFrequencyMenu`：Low/Medium/High Dialog）→ 由 `aaaFortRandomOccurancesQuest`（`aaaFortRandomQuestScript`）按頻率自動觸發 `BanditRaid` 等。`firstRaid` 旗標 + tutorial（objective 70 "Start a Raid"）引導第一波。

→ **守城 = 「Message-menu 選 raid → OnUpdate 計時器 → PlaceActorAtMe 一批 Raider base at boundary markers → AddToFaction 成敵 → 數清歸零」**。**無 Story Manager、無 PlaceAtMe(LeveledNpc) at xmarker quest**——是 quest-script 直接驅動的 spawner（與 [immersive-wenches](immersive-wenches.md) 的 SM/LL spawn 不同路）。

### 4.3 領地 / 安全 = boundary markers + faction 敵我，無 cell-ownership/XOWN

「我的據點範圍」靠 4 個玩家擺的 `BoundaryMarker*`（`aaaFortBoundarySpawner`，`MoveTo`+`Enable`+`collisionRef`，最小距離由 `aaaFortTooFarBoundary`/`aaaFortTooCloseBoundary` MESG 把關）+ travel marker。安全模型純 **faction 敵我**（FortFaction vs RaiderFaction），守衛是 player teammate。**沒看到 XOWN cell-ownership**（建物多在 vanilla worldspace，不是 owned cell）。

## 5. MCM / config

**沒有 MCM**（無 SkyUI / MCM-Helper / `.json` / `.ini`）。**所有設定走 87 個 MESG 選單**，由 `aaaFortPlayerQuestScript` 用 `Message.Show()` 驅動：`PlayerMenu "Outpost Menu"`（根）→ `Settings/SettingsB/SettingsC/SettingsD` 子選單、`RaidFrequencyMenu`（Low/Med/High）、`RaidDifficultyMenu`、`KillStationaryGuards`(`StationaryGuardPurge`)、`RebindMenu`（重綁熱鍵，SKSE）、各建物的 `ManageDialog`/`BreakdownDialog`/`MoveDialog`。設定值存 **quest script property**（`RaidFrequency`/`raidHandicap`/`DebugMode`/`AdvancedMode`/`hideIdleMarkers`/`DistanceSpeed`/`RotationSpeed`…），**非 GLOB**。

## 6. ModForge relevance — idea #22 mapping（逐功能 + 每個「做不到」都 grep `src/ModForge.Core/` 驗證）

逐維對照。**已驗 = 在 `src/ModForge.Core/` 找到對應生成碼**；**GAP = 驗證後確認缺**。

| Tundra 機制 | ModForge 能否生成 | 證據（grep `src/ModForge.Core/`）|
|------|------|------|
| Ingestible「Plans: X」建材瓶（ALCH）| **能** | `Generator.Build.Items.cs` 有 ALCH/Ingestible 生成 |
| MagicEffect（**script archetype**）"Construct X" | **能** | `Generator.Build.Magic.cs` 生 MGEF（含 script archetype）|
| Activator（建物本體，掛互動 script）| **能** | `Generator.Build.LongTail.cs` 生 ACTI |
| **把 Tundra 的 56 支 `.pex` 控制器 script 掛到 MGEF/ACTI/Quest 上（含 typed property）** | **能（關鍵能力）** | `Generator.Build.Scripts.cs` `AttachScripts`→`AttachOneScript`：**反射式**，對任何有 `VirtualMachineAdapter` 的 record（MGEF/ACTI/ALCH/QUST…）掛具名 `.pex` + `FillProperties`。**前提：spec 提供 `scriptAttach` 指定 ScriptName + properties，且編譯好的 `.pex` 在 `CompiledScriptsDir`。** |
| Faction（Outpost/Raider/FollowPlayer/Vendor）| **能** | `Spec.Actors.cs` + Vendor faction（`Generator.Build.Vendor.cs`，見 `settlement-npc-expansions`）|
| LeveledNpc（守衛池）+ Npc base（守衛/Raider 各版）| **能** | `Generator.Build.Lists.cs`（LVLN）+ `npcs` |
| Package（巡邏/站哨/sandbox）+ Keyword + Book（觸發書）+ Container（計畫箱/礦箱）| **能** | `npcs.md` / `Generator.Build.LongTail.cs` / `items` |
| Shout + WordOfPower（Confirm Construction 輸入）+ Spell（4 個建造輸入 spell）| **能** | `Generator.Build.Shouts.cs` / `Generator.WordWall.cs` / `Generator.Build.Magic.cs` |
| **MESG（87 個選單）— 但只能生最小 MESG（Name/Description），無 menu-button** | **半能（GAP：無選單按鈕）** | `Generator.Build.Messages.cs` `mod.Messages.AddNew()` 只設 `EditorID/Name/Description`；`MessageSpec`（`Spec.Items.cs:42`）**無 MenuButtons/Buttons 欄**。Tundra 整個 UI 是多按鈕分支 MESG → **按鈕/分支需 spec + 生成器擴充** |
| **GLOB 計數 / 設定** | Tundra 不用 GLOB（用 quest-script property）；ModForge **有** GLOB 生成 | 不適用（Tundra 走 script property，那部分是 controller 內事，見下） |
| **整套執行期狀態機**：放置定位（`aaaFortMainQuestScript` follow/rotate/confirm）、raid 引擎（`aaaFortPlayerQuestScript` OnUpdate spawn）、募兵（PlaceActorAtMe+faction+teammate）、持久化 counter、move/breakdown/clone | **不可逐欄生成——irreducibly bespoke Papyrus** | 這是 **56 支 controller `.pex` 的行為**，ModForge 不生 Papyrus 行為碼。**但能透過 `scriptAttach` 把這些 `.pex` 掛上去**（須隨 mod 一起 ship 編譯好的 controller `.pex`）。`grep PlaceAtMe src/ModForge.Core` 命中的是 spec 文檔字串，非執行期 PlaceAtMe 生成。|

**結論（#22 verdict）**：Tundra 的**所有靜態 record（ALCH/MGEF/ACTI/FACT/LVLN/NPC/PACK/KYWD/BOOK/CONT/SHOU/WOOP/SPEL/MESG-殼）ModForge 今天都能生**，而且 `scriptAttach`（反射式、已驗）能把 Tundra 那 56 支 controller `.pex` 掛回對應 record。**唯二真正的 GAP**：① **MESG 無按鈕/分支選單**（`MessageSpec` 確認缺欄）——Tundra 的 UI 撐在這上面；② **整套執行期玩法（定位模式、raid OnUpdate spawn、募兵程序、跨存檔 counter 持久化）是 irreducibly bespoke Papyrus**——ModForge 永遠不會「生成」這段行為碼，只能「ship 一支寫好的 controller `.pex` 並 attach」。換言之：**ModForge 能完整生出 Tundra 的「骨架與零件」，但「靈魂」（那支 controller）必須是手寫並隨附的 `.pex`**——這跟 `settlements:` 純靜態 staffing 的本質差別就在「有沒有一支常駐 controller」。

## 7. Roadmap implications — `settlements:` Phase-2「build / manage / defend」要什麼

現 [`settlements:`](settlement-npc-expansions.md) macro（`Spec.Settlement.cs`：residents + DailyRoutine + Vendor）只覆蓋「**住滿**」，**完全沒有 build/manage/defend**。Tundra 給出 Phase-2 要新增的原語清單，逐項標 **generable-today / needs-controller**：

1. **`buildables:`（建材選單系統）** — 每個 entry `{ id, name, model, cost, kind: object|multi|boundary|trap, actorOnPlace? }` macro-expand 成 **Ingestible(plan) + "Construct X" MGEF(script-archetype) + Activator(本體)** 三件套並互掛。
   - **靜態三件套：generable-today**（ALCH+MGEF+ACTI 都已驗能生）。
   - **放置定位行為（喝瓶→follow→rotate→confirm）：needs-controller**——必須隨附一支等同 `aaaFortObjectSpawnerScript`+`aaaFortMainQuestScript` 的 `.pex`，用 `scriptAttach`（已驗）掛上。建議 ModForge **內建一支泛用 placement-controller `.pex`**（如同 dispatcher/MCM-Helper 那樣的隨附 runtime），spec 只填 property。

2. **`defense:` / `siege:`（波次守城）** — `{ waves: [{ type, enemyBases: [...], min, max, boss? }], frequency, difficultyLevels, spawnMarkers }`。
   - **enemy NPC base + LeveledNpc + Raider faction + boundary/spawn marker：generable-today**。
   - **波次觸發 + OnUpdate spawn 計時 + AddToFaction 成敵 + 清場計數：needs-controller**（等同 `aaaFortPlayerQuestScript` raid 段）。可考慮**用 Story Manager + dynamic-spawn quest 半生成**（ModForge 已有 SM + `quest.spawn`，見 memory `dynamic-spawn-debugging`）取代部分 controller，但難度調節/波次節奏仍偏向 controller。

3. **`recruitment:`（募兵/雇工）** — `{ recruits: [{ archetype, cost, fromActivator, faction, teammate, cap }] }`。
   - **募兵程序（付 Gold → PlaceActorAtMe → AddToFaction → SetPlayerTeammate → cap）：needs-controller**（Tundra 全程序化，無對白）。
   - **替代路徑：generable-today** — 若改用 ModForge 既有 **dialogue INFO + SetFactionRank + alias fill**（hire-follower 路，見 memory `hirefollower-paid-gold-bug`）可不靠 controller，但體驗與 Tundra 的「選單即募」不同。

4. **`manageMenu:`（管理 UI）** — Tundra 的命脈。**MESG 按鈕/分支選單是當前最明確的 GAP**：`MessageSpec` 需擴充 `buttons: [...]` + 生成器寫 MESG 的 menu-button 子記錄（`Generator.Build.Messages.cs` 須增能）。**這項是 generable-today 的前置改動**（純 record 擴充，非 controller）——值得先做，因為任何 build/manage mod 都要它。

5. **`territory:`（領地界定）** — boundary markers + travel marker + faction 安全。**generable-today**（marker REFR + faction 已有）；範圍判定（最小距離把關）若要即時則 needs-controller。

**Phase-2 的核心架構抉擇**：Tundra 證明「build/manage/defend」**無法純靠靜態 record macro-expand**——它需要一支**常駐 controller `.pex`**。ModForge 已有「隨附 runtime `.pex` + `scriptAttach` 掛接」的成熟先例（MCM-Helper 的 `ModForgeMCM`、dispatcher psc、storageWrites 的 PapyrusUtil 接法）。**建議**：`settlements:` Phase-2 = 「**內建 1–2 支泛用 controller `.pex`（placement-controller + raid-controller）+ spec 只填 buildables/defense/recruitment 的 property，由生成器把靜態三件套生齊並 `scriptAttach` 掛上 controller**」。最小垂直切片：1 個 `buildable`（喝瓶→定位→落地一面牆）+ 1 個 `recruit`（付錢生一名守衛入隊）+ 1 波 `defense`（按鍵刷 3 個 bandit 在 boundary marker），驗「能蓋、能募、能守」三件事，再擴。先決的純-record 改動：**MESG menu-button 支援**（第 4 項）。

## Verdict

**可借鏡（最高，對 #22 核心）**。Tundra Defense 是 idea #22「build/manage/defend」唯一的完整既有藍圖。機制全部 grounded：建材 = Ingestible(potion) → script-MGEF → spawner `PlaceAtMe` → `aaaFortMainQuestScript` 定位狀態機；募兵 = 程序化 PlaceActorAtMe+faction+teammate；守城 = `aaaFortPlayerQuestScript` 的 Message-menu + OnUpdate spawn `Raider*` base at boundary markers；UI = 87 MESG（無 MCM）；狀態 = quest-script property（0 GLOB）。**ModForge 能生 Tundra 的全部靜態零件，並能 `scriptAttach`（已驗）掛回其 controller `.pex`；兩個真 GAP = MESG 按鈕選單（record 擴充可補）＋整套執行期玩法（irreducibly bespoke Papyrus controller，須隨附 `.pex`）。** 與 Sofia patch 無交集。下載已就緒、可隨時做最小切片實驗。
