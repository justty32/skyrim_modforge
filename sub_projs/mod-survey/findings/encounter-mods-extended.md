# Extended Encounters (v1.6.7)

← [encounter-mods](encounter-mods.md)

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

