# INIGO — unique follower 的「智能」系統拆解（追蹤 / 狀態機 / 自治 AI，全 vanilla + base SKSE）

← [survey index](../index.md)

| 項目 | 值 |
| --- | --- |
| 類型 | **內容型**（單一 unique 語音隨從）但**系統價值極高**：自成一套追蹤 / 行為狀態機 / 自治 AI，**零外部框架** |
| Plugin | `Inigo.esp`（v2.4C，作者 Gary Hesketh）；master = **Skyrim.esm + Update.esm only** |
| 規模 | quests=33 npcs=7 items=18 magic=8 books=16 loc=33 dialogue_lines≈7696；**有 BSA**（341MB，全語音 + facegen + mesh）|
| Record | PACK≈44、FACT≈9、MESG=10、DIAL≈2050、INFO≈7696；**GLOB=0**、無 MCM、無 SKSE DLL |
| 依賴 | 純 vanilla record + **base SKSE Papyrus native**（`RegisterForTrackedStatsEvent`/`GetFormFromFile` 而已）；**無 PapyrusUtil / JContainers / SPID / 任何 DLL** |
| 敘事價值 | 高（但本次略過人格/台詞/笑話/語音，只拆系統）|

## 是什麼

一隻具長期記憶、會在世界裡自己找到你、自己買酒、依技能換裝、可用哨聲/召喚喚回的 unique Khajiit 隨從。**重點不是台詞而是它用純 vanilla 記錄 + 薄 Papyrus 疊出的一套「活人」行為層**。

## 關鍵架構 1：Alias-monitor → 中央 status quest 事件匯流排

核心是 `InigoStatus`(09D37D) quest + `InigoStatusQuestScript`（中央 hub）。周邊三支 **ReferenceAlias 監看腳本**各自訂閱引擎事件，再把結果轉發回 hub：

- `InigoStatusInigoScript`（貼 Inigo）：`OnCombatStateChanged / OnEnterBleedout / OnHit / OnPackageChange / OnSit / OnItemAdded/Removed / OnCellDetach`。
- `InigoStatusPlayerScript`（貼 Player）：`OnLocationChange / OnCellLoad / OnPlayerLoadGame`。
- `InigoStatusSteedScript`（貼馬）：`OnCombatStateChanged / OnHit`。

轉發手法很巧：alias 收到事件後**對 hub 發一個假的 `SendAnimationEvent`**，hub 用 `OnAnimationEvent(String asEventName)` 當 dispatcher 分流（`Steed/Combat/Follow/GoTo/Recover/Help/ShowMrD…` 各是一個 event 字串）。等於用引擎既有的動畫事件通道自建了一條 mod 內部 message bus，避開任何 SKSE mod-event 框架。另外 hub 也吃 **Story Manager** 事件節點 `OnStoryDialogue / OnStoryPlayerGetsFavor` 與 `OnUpdateGameTime / RegisterForSingleUpdate` 做被動輪詢。

## 關鍵架構 2：行為狀態存在「faction rank」，不是 global（GLOB=0）

Inigo 全程**沒有一個 GlobalVariable**。所有可切換行為狀態改用**專用 FACT 的 rank** 承載，好處是狀態能直接被 dialogue CTDA（`GetFactionRank`）讀、且隨 actor 存檔持久：

- `InigoFightingStyleFaction` rank → 戰鬥風格（aggressive/defensive/bow/sword/mixed）；hub `SetFightingStyle` + `SetActorValue Aggression`。
- `InigoFollowDistanceFaction` rank → 跟隨距離（near/far/relax），配 `fAIDistanceTeammateDrawWeapon` 控制何時拔武器。
- `InigoRideWithoutPlayerFaction / InigoShareHorseFaction / InigoSummonableFaction / InigoSteedAllowed / CurrentFollowerFaction / DismissedFollowerFaction` → 各布林狀態。
- 這些狀態由 TIF 對話 fragment（`SetFightingStyle` / `SetAggressionLevel` / `SetFollowDistance` / `Dismiss` / `Recruit` / `FollowMe` / `WaitHere` / `RelaxHere`）從玩家指令直接改 rank。**控制面走對話，非 MCM。**
- 同時 hook 進 **vanilla `DialogueFollower` quest / follower faction**，複用引擎原生的招募/解散/teammate 基礎（不重造輪子）。

## 關鍵架構 3：追蹤 / 尋找 / 召喚（他能「找到你」的真相）

- **Radar 掃描**：`InigoRadar`(09D37E, `InigoRadarQuestScript`) —「Search for nearby horses and inns」。`HorseSearch` 逐 cell 掃附近 actor 找可送/可換的馬，`FindLocalInnTarget` 找旅店 `MoveTo` 目標，結果 callback 回 `InigoFollowerDialogue.HorsesFound`。
- **召喚**：`InigoSummon` SPEL → 生一個 `InigoSummonMarkerScript` marker，marker `OnLoad` 檢查 `InigoSummonableFaction` → `MoveTo(marker)` + `EvaluatePackage`。「召喚咒放個標記、標記自己把他搬過來」。
- **哨聲**：`InigoStopFightingPower`（Whistle）+ `InigoWhistleReactionScript` — 距離判定後 `EvaluatePackage`，兼作停戰/召回。
- **地圖找人**：`InigoMapMarker`(0632D2)「INIGO, WHERE ARE YOU?」— 他脫隊後可在地圖上定位他。
- **記憶**：`InigoNPCTalkedTo`(0BFBE8)「Tracks who Inigo has met」+ `InigoPlayerJourney/PlayerProg/PlayerJ2/PlayerQuestions` — 用 quest alias + scene + `SetFactionRank`(見 `QF_InigoPlayerJourney` fragment) 記錄玩家旅程與他見過的 NPC，供事後對話回憶。
- **鎖匠評論**：`InigoFollowerDialogue.OnTrackedStatsEvent`（base-SKSE `RegisterForTrackedStatsEvent`）監看玩家撬鎖統計 → 觸發對應台詞。

## 關鍵架構 4：裝備 / 經濟自治

- **依技能換裝**：`InigoStatusInigoScript`「Clothing choice is controlled by armor skills」— 讀他自己的 `HeavyArmor/LightArmor` base actor value，自動 `UnequipItem/EquipItem` 對應甲。
- **自主消費**：`InigoInnKeeper`(0D128B)「orders himself a drink」自己買酒；`InigoUsesShrine` 進神殿用祭壇；`InigoShopping`(0B1D37,「Shopkeep alias etc」) 送禮/購物；`InigoBeggar` 施捨 scene。這些是 quest + alias + package 驅動的 ambient 自治行為。

## 關鍵架構 5：心情系統（當狀態看，不看台詞）

對話 INFO 被標成 `(Happy)/(Sad)/(Surprise)` 等心情變體；心情由 status 系統依事件（戰鬥/受傷/地點/劇情）推移，再作為 dialogue 選句條件。等同「一個 Hello topic 多個 mood-conditioned INFO」的既有 pattern（見 conditioned-hello-one-topic-many-infos），只是驅動源是他自己的狀態機而非玩家。

## 關鍵架構 6：quest / script 佈局

~33 個 quest 各司一職（非單一大 controller）：`InigoStatus`＝行為 hub、`InigoRadar`＝掃描、`InigoMapMarker`＝定位、`InigoNPCTalkedTo`/`InigoPlayer*`＝記憶、`InigoSummonReaction`＝召喚、`InigofollowerDialogue`＝跟隨控制主 quest、`InigoNpcChat*`（~10 支）＝與 NPC 的 scene 對話、`InigoFollowerControl`（標記 Obsolete 但保留做自清）。腳本全部反編自 BSA（本次用自寫 SSE-BSA 解包 + big-endian PEX string-table/debug-info parser，**無 champollion，邏輯係由 property/function/docstring 名稱推得，未逐行反編位元碼**）。

## 結論

- **對 ModForge**：**大部分可生成 / 局部需便利層**。整套機制的零件都在能力域內——ReferenceAlias + alias-script、FACT（含 rank）作狀態、cell-scan/MoveTo/EvaluatePackage 的 controller `.pex`（`scriptAttach` 已驗證能掛回）、TIF fragment 改 faction rank、mood-conditioned INFO、Story Manager 事件節點、召喚 SPEL→marker。**沒有任何 DLL / PapyrusUtil / JContainers / MCM 依賴**，這是 Inigo 最值得借鏡的一點：**用 faction-rank 當狀態機 + 假 AnimationEvent 當內部 message bus，純 vanilla 就疊出「有記憶會自理」的隨從**。缺口偏便利層而非硬缺：① 「alias-monitor→中央 dispatcher」與「faction-rank 狀態機」缺 spec macro（現在得手擺 alias/FACT/fragment）；② tracking/summon/radar 這類**執行期演算法仍是 bespoke Papyrus**，ModForge 是 packager、須隨附 controller `.pex`（同 Tundra/Honed Metal 類）。無新 record 死角。
- **對 Sofia**：**高度相關**。Inigo 與 Sofia 同為 standalone unique 語音隨從，這份是 Sofia 系統面的最佳對照範本：**faction-rank 狀態機**（戰鬥風格/跟隨距離/心情，直接進 dialogue CTDA）、**假 AnimationEvent message bus**、**召喚/哨聲/地圖定位/記憶**四件套，都可平移進 Sofia patch 而不引入任何框架依賴。與 improved-follower-dialogue-lydia 互補：Lydia 那份講「對話 arc / 道德狀態機」，Inigo 這份講「行為 / 追蹤 / 自治 AI 的骨架」。
