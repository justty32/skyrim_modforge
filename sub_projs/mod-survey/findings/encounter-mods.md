# Encounter Mods 調查

> 調查日期：2026-06-15  
> 目的：為 ModForge 未來的 random encounter 生成功能蒐集技術機制依據  
> 注意：**兩個 mod 各自都有更詳細的獨立調查**：[extended-encounters.md](extended-encounters.md)、[immersive-world-encounters.md](immersive-world-encounters.md)。本檔是從 `~/skyrim_mods/hdd/` 直接對 zip/7z 做原始分析（`unzip -p`, `7z e`, `strings`, psc source）的整合對比，補充獨立檔未記錄的細節。

---

## Immersive World Encounters SE (v3.6.1)

### 基本資訊

- **档名**：`Immersive World Encounters SE-18330-V3-6-1-1639501058.7z`
- **ESP**：`Immersive Encounters.esp`（3 MB）；資料在 `Immersive Encounters.bsa`（764 MB，含語音/mesh）
- **Master files**：`Skyrim.esm`, `Update.esm`, `Dawnguard.esm`, `HearthFires.esm`, `Dragonborn.esm`（全部 DLC）
- **作者前綴**：`Sette`（所有 EditorID 帶 `_Sette` 或 `Sette`）
- **無 Source scripts**：BSA 裡沒有 `.psc`，只有編譯好的 `.pex`

### 核心機制

IWE 完全寄生 Skyrim 原版 Story Manager，沒有自己的 event root。觸發鏈：

```
原版 SM event root（WEQuests / WIChangeLocation* / WITavernQuestNode* / DLC2WE）
  └─ IWE SMBN（7個，做分流）：WE_SetteRandomBranch / WE_SetteQuests / WI_SetteCL* …
       └─ IWE SMQN（37個，掛實際 quest）：WE_SetteRoads / WE_SetteFactions / WE_SetteCLNode{City/Village/…} …
            └─ 單顆 encounter Quest（WE_Sette*, WI_Sette*）
                 ├─ Quest aliases：演員（LeveledNpc fill）+ TRIGGER marker + TravelMarker + Hold偵測
                 ├─ QF fragment script + WEScript 共用控制器
                 ├─ AI Package（vanilla Travel/Sandbox template 薄包裝）
                 ├─ Scene（Dialog/Package/Timer 三動作交織）
                 └─ Dialogue INFO（CTDA 多條件分歧對白）
```

**vanilla quest override**：直接 override 幾個原版 WE quest（`WE01`, `WE24`, `WE25`, `WE31`）加入 IWE 邏輯，並用 `WE24_LocationMatters_Sette` 這個 Global 做地點加權。其餘 ~100+ 遭遇是新增的 SM leaf quest。

**靜態預擺 PE Markers**：5 個 `IWE_PEMarker` 持久擺放在 Tamriel 固定位置（Haafingar / Reach / Riften / Whiterun / Eastmarch），作為 SE 版新增的持久遭遇觸發錨點，比純 alias-MoveTo 更穩定地控制遭遇發生區域。

### Spawn 邏輯

- **演員填充**：alias fill = `from LeveledNpc`（65 個 LVLN list），如 `_SetteLCharWEWandererAll`、`_SetteLCharWEBountyHunter`。每次遭遇演員不同。
- **走位**：AI Package 以 `AliasForReference` 做 target，NPC 走到 quest 的 travel-marker alias 位置。
- **地點分流**：SMQN 的條件含 `WIChangeLocation` keyword（`LocTypeCity` / `LocTypeVillage` / `LocTypeTavern`…），讓不同地點類型觸發不同遭遇桶。
- **Hold 偵測**：alias `myHoldImperial`/`myHoldSons`/`myHoldContested` 偵測內戰歸屬，讓對白/陣營條件化。
- **演出節奏**：Scene phase = Package 動作（走位）+ Timer 動作（卡節奏）+ Dialog 動作（播台詞）。

### ModForge 可生成的部分

| 機制 | 狀態 |
|------|------|
| SM additive branch/quest node（掛原版 WEQuests root） | ✅ 已支援 |
| 隱形 encounter quest（無 journal/objective，純狀態機） | ✅ 已支援 |
| Scene 三動作交織（Dialog/Package/Timer + multi-phase） | ✅ 已支援 |
| CTDA 反應性對白（GetIsAliasRef / GetStage / GetEquipped…） | ✅ 已支援 |
| AI Package vanilla template 薄包裝 | ✅ 已支援 |
| LeveledNpc / LeveledItem / Outfit | ✅ 已支援 |
| **alias fill from LeveledNpc（LVLN picker）** | ⚠️ **缺口，最高優先** |
| **Package/marker target 指到 quest alias（alias indirection）** | ⚠️ **缺口** |
| SM branch/quest-node 多層分流 + 加權 | ⚠️ 缺口（「選台機」擴充） |

### 設計模式筆記

- **Hold 偵測 alias**：`LocationAlias` fill from 原版 Hold location + 內戰歸屬條件 → 同一個遭遇在帝國控 Hold / 風暴披風控 Hold 說不同台詞、生不同陣營 NPC。可當「context-aware encounter」的範本。
- **多桶 SMQN 命名即路由表**：`WE_SetteCLNode{City,Village,Tavern,Dragon}` 看名字就知道它對應哪種地點類型。AI-agent 友善。
- **PE Marker 作為「地域錨」**：5 個固定擺在不同 Hold 的持久 marker，讓某些遭遇能「以某 Hold 中心點為半徑」觸發，比純 SM 觸發更能控制地理分布。

---

## Extended Encounters (v1.6.7)

### 基本資訊

- **檔名**：`Extended Encounters-44810-1-6-7-1716526922.zip`
- **ESP**：`Extended Encounters.esp`（1 MB）；`Extended Encounters.bsa` + `Extended Encounters - Textures.bsa`
- **Master files**：`Skyrim.esm`（僅此一個，不需 DLC）
- **作者前綴**：`EE_`（所有 EditorID 以 `EE_` 開頭）
- **有 Source scripts**：zip 裡有 `/Source/*.psc`（~80 個）

### 核心機制

EE 也是寄生原版 SM，但觸發結構與 IWE 不同，分成**三個獨立觸發路徑**：

```
1. World Encounters（路邊/荒野）
   原版 WEQuests SM root → EE_ScriptEvent SMBN
     → EE_Road / EE_Wilderness / EE_Dragon SMQN（+ 子 SMQN 依 Chance global 分流）
       → EE_WE001..171 骨架 quest（164 個）
         ← 觸發器：EE_WEStarter alias（ReferenceAlias on player）
           EE_DynamicWEStarterScript：OnInit → RegisterForSingleUpdate(6h-24h 隨機)
           OnUpdate → 若 outdoor/Tamriel → Start EE_DynamicWE quest
                      → 用 NavmeshTester 找 navmesh 點 → 移動 marker alias

2. Location Encounters（進新地點觸發）
   原版 ChangeLocation SM root → EE_ChangeLocation SMBN
     → EE_WI_LocType{Town/Inn/BanditCamp/…} SMQN（22 種地點分桶）
       → EE_WI001..147 骨架 quest（147 個）
         ← 觸發：EE_WIPlayerScript（LocationAlias on player，OnLocationChange）
           + EE_WITimeout（強制冷卻，default 12 遊戲小時）

3. Location Interactions（地點內小互動）
   原版 ChangeLocation root → EE_LocationInteraction SMBN
     → EE_LI_LocType{City/Inn/…} SMQN（10 種）
       → EE_LI001..014（14 個，用現有 NPC 做互動，不另外生）

4. Situation Encounters（Sleep/Wait/FastTravel 觸發，需 SKSE）
   EE_SE quest（常駐）→ EE_SE_SleepScript / WaitScript / FastTravelScript
     → RegisterForSleepStart / RegisterForMenuOpen(Wait) / SKSE event FastTravel
     → 依 PlayerLocation.HasKeyword(LocType*) 選 LvlBandit*/LvlVampire*… 直接 PlaceAtMe
```

**招牌技巧 — NavmeshTester 動態定位**（`EE_QF_EE_DynamicWE_010465F2`）：
1. `NavmeshTester` actor `MoveTo(player, random ±6000, ±6000)` 找候選點
2. `while distance < 4000`：繼續隨機，確保離玩家夠遠
3. `EnableAI(False)` 然後 `EnableAI()` → actor 吸附到最近的 navmesh 點
4. 所有 scene marker alias `MoveTo(NavmeshTester)`
5. `NavmeshTester.Delete()`
→ 零殘留，純動態找合法可走點，不用預擺 cell

### Spawn 邏輯

- **WE（路邊）**：大多數是 vanilla named NPC（Uthgerd、Faendal…）從固定 alias fill；少數用 `LeveledNpc`（`EE_LCharRandomEnemy`、`EE_LCharRandomCivilian`等 8 個 LVLN）做「random enemy/civilian」。
- **WI（地點）**：同上模式，但觸發條件加 `LocType` keyword + 玩家 Hold 偵測（`myHoldLocation`/`myHoldImperial`/`myHoldSons` alias），有 12+ 種 LocType 分桶。每個 WI quest 帶 `EE_WI_Chance` global 做個別開關。
- **SE（危險地點）**：直接 `PlaceActorAtMe` 用 ActorBase（`LvlBandit*`、`LvlVampire*`…），不走 SM 路徑，更輕量直接。
- **Draugr Swarm**（`EE_WE141`，`EE_DraugrSwarmScript`）：`OnLoad`（XMarker cell 載入） → `SpawnDraugr()` 在 while 迴圈裡反覆 `PlaceAtMe(XMarker)` 周圍、清 dead、維持最多 5 隻 draugr、用 VisualEffect 播霧氣，持續 0.5 遊戲小時。

### ModForge 可生成的部分

| 機制 | 狀態 |
|------|------|
| SM additive branch/quest-node（三條 branch） | ✅ 已支援 |
| 骨架 quest（無 objective/log）| ✅ 已支援 |
| ReferenceAlias + cleanup cleanup fragment | ✅ 已支援 |
| AI Package（vanilla template 薄包裝）| ✅ 已支援 |
| LeveledNpc 8 個 LVLN | ✅ 已支援（較少用） |
| GlobalShort 海（357 個 MCM gate）| ✅ 已支援（GlobalVariable） |
| **NavmeshTester 動態 spawn Papyrus 樣板** | ⚠️ 缺口（需 alias-script 樣板） |
| **多候選 SM branch/quest-node 子樹生成** | ⚠️ 缺口（選台機） |
| **Sleep/Wait/FastTravel SKSE 事件觸發** | ⚠️ 缺口（SKSE 依賴，非 SM） |

### 設計模式筆記

- **GlobalShort 開關 + Chance slider**：每個 encounter 有 `EE_WE001Chance`（Global），MCM 讀寫這個值，SM quest node 條件也 gate 這個值。可以讓玩家自訂每類遭遇的機率。ModForge 若要支援這個模式，可以在 encounter spec 裡多一個 `chanceGlobal: "myMod_WE001Chance"` 欄位。
- **WITimeout 冷卻機制**：`EE_WITimeout` Global（default 12 遊戲小時）防止同地點連續觸發。實作是 `EE_WITimeoutScript` 記錄上次觸發時間，OnLocationChange 前先比對時間差。
- **「地點內 LI 用現有 NPC」**：Location Interaction 不另生 NPC，而是用 `findMatchingRef`（找當前 cell 已有的 NPC）對他們發 package 命令，完全不增加 actor。這是最輕量的「讓世界動起來」方法。
- **Draugr Swarm 的 OnLoad 觸發**：預擺一個 XMarker（disabled），cell 載入時腳本醒來做 spawn，不靠 SM。適合「特定地點的驚嚇遭遇」。

---

## 兩個 mod 對比 + ModForge 擴充建議

### 機制對比

| 面向 | IWE v3.6 | EE v1.6.7 |
|------|----------|-----------|
| SM branch 數 | 7 SMBN | 3 SMBN |
| SM quest-node 數 | 37 SMQN | 31 SMQN |
| Encounter quest 數 | ~100（新增）+ 4 vanilla override | WE164 + WI147 + LI14 |
| 演員來源 | 主要 LVLN（65 個），每次隨機 | 多數 vanilla named NPC，少量 LVLN（8 個） |
| 有 Scene（SCEN）| 是（56 個，含對白）| 否（0 個） |
| 有 Dialogue | 是（1409 個 INFO）| 否 |
| Source scripts 公開 | 否 | 是（~80 .psc） |
| DLC 依賴 | 全部（Skyrim + 3 DLC）| 僅 Skyrim.esm |
| SKSE 依賴 | 否 | 是（SE 觸發） |
| 動態 navmesh 定位 | 否（預擺 marker 或 alias-to-alias）| 是（NavmeshTester trick）|
| 地點類型過濾 | 是（LocTypeCity/Village/Tavern/…）| 是（22 種 LocType）|
| MCM 可調 | 否 | 是（per-encounter 開關 + Chance slider）|

### 設計哲學差異

**IWE** 是「**戲劇性小場景**」路線：每個遭遇都有完整的 Scene + Dialogue，演員有台詞，反應玩家狀態（穿什麼裝備、完成了什麼主線）。成本高（422 NPC + 1409 INFO），但體驗豐富，有「真人感」。

**EE** 是「**動態世界動起來**」路線：幾乎不用 Scene/Dialogue，靠 AI Package 讓 NPC 自然行動（行旅、戰鬥、待機）。NavmeshTester trick 讓遭遇能在玩家附近任意地形生成。MCM 高度可調，適合不喜歡 scripted 的玩家。

### ModForge 擴充優先順序

1. **alias fill from LeveledNpc（LVLN picker）**：兩個 mod 都用，IWE 更重度依賴。補上這個缺口讓 ModForge 能生成「每次演員不同」的遭遇，是 encounter generator 的核心。`Spec.Quest.cs` 的 alias fill 模式需新增 `fromLeveled` 型。

2. **Package/marker target alias indirection**：IWE 的 Travel package target = quest alias 的 marker ref。EE 的 NavmeshTester 動態移動 marker alias 後，package 同樣靠 alias indirection 跟著走。需讓 `packages[].travel.place` 支援 `{ alias: "TravelMarker1" }` 語法。

3. **NavmeshTester 動態 spawn Papyrus 樣板**（EE 專屬）：把 `EE_QF_EE_DynamicWE_010465F2` 那段腳本（隨機 ±6000 偏移 → EnableAI 吸 navmesh → MoveTo → Delete）做成 encounter-spawn 的 script 樣板，讓 spec 宣告 `spawnMode: "dynamicNavmesh"` 就能自動生成這段 Papyrus。

4. **SM branch/quest-node 多層分流 + 加權**（兩個 mod 共用）：目前 ModForge 能做「一個 quest 掛到 vanilla SM event root」，但無法建構「一顆 SMBN 底下掛多個 SMQN、每個 SMQN 掛多個候選 quest + 條件/權重」的選台機。這是兩個 mod 的核心組織方式，需要在 `Generator.Build.StoryManager.cs` 擴充。

5. **LocType keyword 路由 + Hold 偵測 alias**（兩個 mod 共用）：SMQN 的 LocType 條件 + LocationAlias 的 Hold 偵測是「地點感知遭遇」的關鍵，可以包裝成 encounter spec 的 `locationFilter: [LocTypeBanditCamp, LocTypeTown]` + `holdDetection: true` 高層語法。

6. **WITimeout 冷卻模式**（EE）：防 spam 的機制，讓 encounter generator 支援 `cooldownHours: 12` 之類的 spec 欄位，自動生成 Global + script 冷卻邏輯。

### 可複用設計模式總結

- **Random encounter table（LVLN list）**：IWE 的 65 LVLN + alias fill 是最正統的做法。
- **Level-scaled spawn**：`LeveledNpc` 的 chanceNone + entries[]（每個 entry 有 level 門檻）= 自動 level scaling。
- **Location-type filter**：SMQN 條件 + `LocType*` keyword 讓同一套框架輕鬆分地點類型。
- **Hold state context**：LocationAlias `myHoldImperial`/`myHoldSons` 讓遭遇感應世界政治狀態。
- **Zero-bloat cleanup**：演員 `DeleteWhenAble()`、marker `MoveToMyEditorLocation()` — 不留殘留，存檔不膨脹。
- **「骨架 quest 無 journal」**：SM 驅動的隱形遭遇 quest，不污染任務日誌。

---

### 相關既有調查

- [extended-encounters.md](extended-encounters.md)（更詳細的 EE 分析，含 record census 與完整 questdiag 輸出）
- [immersive-world-encounters.md](immersive-world-encounters.md)（更詳細的 IWE 分析 v2.3.1，含 scnscan/scenediag 輸出；本次手工分析的是 v3.6.1）
- 相關 memory：`story-manager-kill-recipe`、`dispatcher-magic-trigger`（SM 掛載）、`scene-playidle-recipe`（Scene/Package）、`sm-quest-journal-progression`（隱形 quest）、`conditioned-hello-one-topic-many-infos`（CTDA 對白）
