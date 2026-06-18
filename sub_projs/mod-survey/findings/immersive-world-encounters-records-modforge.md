# 關鍵 record 與模式 + 對 ModForge 的參考價值

← [immersive-world-encounters](immersive-world-encounters.md)

## 2. 關鍵 record 與模式（具體舉例）

### 2a. Story Manager：寄生原版 root + branch 分流

IWE 的 SM 節點全部 additive 掛在原版 root 底下，命名一律 `WE_Sette*` / `WI_Sette*`（`Sette` 是作者前綴）：

- **掛載點（原版 root，被 additive override）**：`WEQuests`（`04A601:Skyrim.esm`，World Encounter 主事件）、`WIChangeLocationThiefNode`（`023F1B:Skyrim.esm`）、`WITavernQuestNodeSHARES`（`0DEE94:Skyrim.esm`）、`DLC2WERegionNorthNode`（`01E7DF:Dragonborn.esm`）。
- **IWE branch node（SMBN，做分流）**：`WE_SetteRandomBranch`、`WE_SetteQuests`、`WI_SetteChangeLocationNode01/EM/DLC2`、`WI_SetteCLBranchLocPrior`。
- **IWE quest node（SMQN，掛實際 quest）**，按遭遇種類分桶：
  - 道路類 `WE_SetteRoads`、隨機類 `WE_SetteRandom` / `WE_SeteRandomPrisoners`、陣營類 `WE_SetteFactions`、稀有類 `WE_SetteRare`、賞金 `WE_SetteBountyHunt`、跟隨者 `SetteFollowerQuestNode`、恫嚇 `SetteDGIntimidateNode`、信使 `WI_SetteCourierNode`。
  - 換位置（進城/進村/進 tavern/見龍）一整排：`WE_SetteCLNodeLoc{Solitude,Whiterun,Riften,Markarth,Windhelm}`、`WI_SetteCLNode{City,Village,Tavern,Dragon,DB,TG}`。
  - 這就是「多樣遭遇怎麼被組織」的答案：**一個遭遇種類 = 一個 quest node**，靠 node 的條件（哪個 Hold、哪種地點、原版 WE 是否冷卻）+ 權重分流到不同戲。

### 2b. Encounter Quest：無 journal 的「演出模板」

代表 `WE_SettePrisonerEscortBandits`（`0x22367B`，"Bandits with Prisoner"），`questdiag` 顯示：

```
flags=4 priority=70 type=None event=SCPT
Stages: [0]=StartUpStage  [5]  [10]  [255]=ShutDownStage  （全部 log 為空）
Objectives: 0
```

模式：**stage 沒有日誌文字、沒有 objective**——這些 quest 對玩家「隱形」，只是個容器。stage 純粹當作 fragment script 的狀態機 step（5、10 = 演出階段，255 = 清場）。掛兩個 script：共用的 `WEScript`（2 props）+ 自動產生的 `QF__0522367B`（13 props，stage fragment）。

少數遭遇**有** objective（例如 `SetteFollowerQuest` 有 `objective[10] "<Alias=Follower01> is waiting for you"`），那是會給玩家任務感的（招募跟隨者）；純路邊戲一律無 journal。

### 2c. Quest aliases：runtime 隨機填演員 + 環境 alias

`WE_SetteLeftForDead01Scene`（`0x835F66`）的 host quest（`0x835F65`）有 14 個 alias，分三類：

- **演員 alias**（runtime 從 LeveledNpc 填）：`Wanderer`(#16)、`CaptiveAlias`(#44)、`BanditAlly01/02`(#45/46)、`ThiefAlly01/02`(#47/48)。同一個遭遇用 alias 索引區分「這次是 bandit 版還是 thief 版」。
- **位置/觸發 alias**：`TRIGGER`(#1)、`LocationCenterMarker`(#42)、`TravelMarker1/2`(#9/10)——給 AI package 當 travel target。
- **環境偵測 alias**：`myHoldLocation`/`myHoldContested`/`myHoldImperial`/`myHoldSons`(#4/6/7/8)——偵測當前 Hold 歸屬，讓對白/陣營隨內戰狀態變。

演員池：`LeveledNpc` 65 個，如 `_SetteLCharWEWandererAll`、`_SetteLcharWEBountyHunter`、`_SetteLCharWEPrisonerHoldAll`、`_SetteLCharWEBattleRoyalTeamB/C`。**一個 alias fill from 一個 LVLN list = 每次遭遇演員不同**。

### 2d. Scene：Dialog / Package / Timer 三種動作交織

`scnscan` 顯示 56 個 scene 大量用非對話動作。代表 `WE_SetteLeftForDead01Scene`（`0x835F66`，`scenediag`）：

```
actor: alias #16  behaviorFlags=DeathEnd（演員死亡即結束 scene）
phases (1)
actions (3):
  Type=Dialog   ActorID=16  Topic=0x835F67  Flags=Looping  LoopingMin=1 Max=5
  Type=Package  ActorID=16  Packages=[891252]（走到 marker 的 Travel package）
  Type=Timer    ActorID=16  TimerSeconds=10
```

更複雜的多 phase 例：`WI_SetteTGScene`（12 動作、8 個 Package 動作跨 phase 0→4，多演員走位）；`WERJ_SetteDLC2Drunk02ContestScene`（賭酒，15 動作、9 非對話：每 phase 一個 0.9s Timer + 跨 phase Package）。模式：**Package 動作負責走位/姿勢，Timer 動作負責節奏，Dialog 動作負責台詞，phase 推進串起整段戲**。

### 2e. Dialogue INFO 的 CTDA：反應性對白的核心

scene 對白 topic 用條件做出分歧，這是 IWE「有靈魂」的地方。`TOPIC 0x84F4D8` 一個 topic 掛 8 個 INFO，靠條件選播：

```
INFO 0x84F4D9 "Err...what's a Nightingale?"  conds=9:
  GetStage(0x835F65) == 70
  GetIsAliasRef alias#47 == 1          ← 只有 ThiefAlly01 版才講
  HasKeyword(0x84A3D2) == 1
  GetEquipped(Nightingale 各部位) == 1 [OR 串]   ← 玩家穿夜鶯裝才觸發
INFO 0x84F4DB "Wait, isn't he from the Guild?"  conds=10:
  GetStageDone(0x01F326 TG 主線, stage 200) == 1   ← 玩家完成盜賊公會主線才講
  GetIsVoiceType(0x94C82D) == 0
  GetEquipped(Nightingale...) == 0     ← 沒穿才講（互斥分支）
```

模式：**一個 scene topic = 多個 INFO，靠 `GetIsAliasRef`（哪個演員被填進來）+ `GetStage`（演到第幾步）+ `GetEquipped`/`GetStageDone`/`GetIsVoiceType`（玩家狀態/世界狀態）做精細分歧**。這跟既有筆記 [conditioned-hello-one-topic-many-infos] 完全同構，只是搬到 scene 語境。

### 2f. AI Package：原版 template + alias marker target

`WE_SetteLFDCaptivePackage`（`0x891252`，`packagediag`）：

```
PackageTemplate -> 016FAA:Skyrim.esm   ← 原版 Travel template
Flags = IgnoreCombat, WeaponsUnequipped
PreferredSpeed = Run
Data: [0] PackageDataLocation target=LocationFallback(NearSelf)  [2][4] Bool
ProcedureTree: 0 branch（完全靠 template + data 填充，不自己定義 procedure）
```

模式：**不從零寫 package procedure，而是引用原版 template（Travel `016FAA`、Sandbox 等），只覆寫 flags + PackageData**，target location 指到 quest 的 travel-marker alias。488 個 package 幾乎都是這種薄包裝。

---

## 3. 對 ModForge 的參考價值（可生成 / 需新支援 / 純參考）

對照 ModForge 現有能力（spec 模型在 `src/ModForge.Core/Spec.*.cs`）：

### ✅ 可生成（ModForge 今天就能做）

- **隱形 encounter quest**（無 journal 的演出容器）：`quests[].stages[]` 直接做 StartUp/中段/ShutDown 空 log stage + QF fragment——這正是 ModForge quest 模型的本命。模型在 `Spec.Dialogue.cs`。
- **Scene 三動作交織**（Dialog/Package/Timer、多 phase、`behaviorFlags=DeathEnd`）：`scenes[].phases[]` + `actions[]`（`package` ref / `timerSeconds` / 對話），`Spec.Scene.cs`。對應筆記 [scene-playidle-recipe]。
- **CTDA 反應性對白**：`dialogue[].conditions[]` 已支援 GetStage / GetIsAliasRef / HasKeyword / GetEquipped / GetIsVoiceType（皆在支援清單內），`Spec.Dialogue.cs`。對應 [conditioned-hello-one-topic-many-infos]。
- **SM node 掛原版 root**：`quests[].storyEvent`（event + keyword + conditions[]）能 additive 加掛 SMBN/SMQN 到原版 event root——IWE 的 `WEQuests` 寄生模式可直接複製。`Spec.StoryManager.cs`，對應 [story-manager-kill-recipe]、[dispatcher-magic-trigger]。
- **AI Package 用原版 template**：`packages[].travel` + 八種 template 支援，IWE 的 Travel-template 薄包裝模式 OK。`Spec.Packages.cs` / `Spec.Packages.Templates.cs`。對應 [scene-playidle-recipe] 內的 package 段。
- **LeveledNpc / LeveledItem / Outfit**：`leveledNpcs[]` / `leveledItems[]` / `outfits[]` 都會產生 record，`Spec.Items.cs`。

### ⚠️ 需新支援（IWE 的核心做法，ModForge 目前缺口）

- **【最大缺口】Quest alias 從 LeveledNpc runtime 填演員**：IWE 的「每次遭遇演員不同」完全靠這個，但 ModForge 的 alias fill 五模式（`fromEvent` / `forced` / `uniqueActor` / `createObject` / `findMatching`）**沒有 LVLN picker**——`createObject` 只能生直接 NPC ref、`findMatching` 只找已載入區域的現存 ref。要復刻 IWE 必須加一個 **alias fill = "fromLeveled (LVLN)"** 模式。優先級最高。
- **AI Package target 指到 quest alias**：IWE 的 travel marker target 是 quest alias indirection，但 ModForge `packages[].travel.place` 只能指 placed REFR/ACHR，**不能 `place: "aliasName"`**。需加 alias-indirection target。
- **SM branch/quest node 多層分流 + 權重**：ModForge 目前「一個 (root, keyword) 一條 branch」，無法做 IWE 那種 SMBN 多層分桶 + 每個 SMQN 帶不同條件/權重的「遭遇選台機」。要做 encounter generator 需擴充 SM 樹建構。
- **Scene completion conditions** ：spec 有 `completionConditions`，但 ModForge 註記為「offline-built, not yet in-game-verified」——IWE 重度依賴 phase/scene 完成條件，值得補實機驗證。

### 📖 純參考（設計範式，不一定要進 ModForge）

- **「隱形 quest」設計哲學**：encounter 不該給 journal/objective，stage 只當 fragment 狀態機——值得寫進 ModForge 的 encounter 範式文件，避免新手誤加 objective。
- **環境偵測 alias**（`myHoldImperial`/`myHoldSons` 偵測內戰歸屬）：用 alias + 條件讓遭遇隨世界狀態變化的技巧，可當未來「context-aware encounter」範例。
- **動作分工慣例**：Package=走位、Timer=節奏、Dialog=台詞——可當 ModForge scene 文件的 best-practice。
- **演員池規模感**：65 個 LVLN / 422 NPC / 31 Outfit 餵 56 個 scene——說明「內容量」才是這類 mod 的真成本，ModForge 能省的是 wiring boilerplate，不是美術/演員設計。

---

## 4. 結論：對 ModForge 路線圖的一句話

IWE 證明「**SM node（選台）→ 隱形 quest（容器）→ alias 從 LVLN 隨機填演員 → Scene 用 Package/Timer/Dialog 演出 → CTDA 對白分歧**」是路邊遭遇的標準骨架，而 ModForge **已能生成這條鏈的 70%**；唯一卡關的兩個缺口是 **alias-from-LeveledNpc fill** 與 **package/marker 的 alias-indirection target**——補上這兩個，ModForge 就能用 JSON spec 量產 IWE 式遭遇。SM 多層分流（選台機）是錦上添花的第三步。

相關既有筆記：[story-manager-kill-recipe]、[dispatcher-magic-trigger]（SM 掛載）、[scene-playidle-recipe]、[sm-quest-journal-progression]（scene/package）、[conditioned-hello-one-topic-many-infos]（CTDA 對白分歧）。
