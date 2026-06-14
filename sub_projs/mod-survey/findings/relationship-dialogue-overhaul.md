# Relationship Dialogue Overhaul / RDO

- 類型：內容型 / relationship and generic dialogue overhaul
- Plugin：`Relationship Dialogue Overhaul.esp`
- 已解壓：`~/skyrim_mods/unzip/Relationship Dialogue Overhaul - RDO Final-1187-Final/`
- 抽出資料：`../game-data/mods/Relationship Dialogue Overhaul/`
- 規模：152 quests，3784 dialogue lines，17 NPCs，10 books，6 items，8 locations，29 magic records

## 結構

RDO 是大範圍關係系統與 generic dialogue overhaul。它同時做：

- generic relationship hellos/goodbyes/combat/flee/player action reactions
- follower recruit / dismiss / command shared info
- voice type 分組的 non-hate / hate / friend / spouse dialogue
- DLC unique followers / NPCs 補充，例如 Serana、Frea、Valerica、Gelebor、Isran
- 一些小型 quest / scene 補白

代表 quest：

- `00AA04 aaa_RDOFemaleFollowerCommandSharedInfo`
- `00AA0A aaa_RDOFemaleFollowerIdleSharedInfo`
- `00AA0C aaa_RDOFemaleFollowerSpouseSharedInfo`
- `59594C a_RDOGenericFollowerDialogue`
- `F46DAE a_RDOSayOnceVariableStorage`
- `00AA0E aaa_RDO_MCMConfig`

## 條件 pattern

RDO 的設計核心是「shared info + voice type matrix + relationship/faction gate」：

- 大量 INFO 以 `GetIsVoiceType` OR 串聯，讓同一句或同類句子套到多種 voice type。
- follower 招募用 `GetInFaction CurrentFollowerFaction == 0`、`PotentialFollowerFaction == 1`、`GetRelationshipRank >= 1`、`PlayerFollowerCount global`、排除 form list 等條件。
- spouse / lover dialogue 用 `GetInFaction MarriageFaction`、`GetRelationshipRank == 4`、MCM quest variables。
- command shared info 只按 voice type 分回應，例如「Of course, sir/ma'am」、「Right away」、「I will do nothing of the sort」。
- unique follower recruit/follow-distance topics 另開 topic，例如 Gelebor、Isran、Valerica。
- 許多招募 INFO 無 response，靠 `RDO_DefaultRecruit` VMAD fragment 執行招募行為。

RDO 不是單一角色 arc，而是 relationship grammar：它擴大原版 NPC 對玩家關係、聲線、婚姻/follower 狀態的反應空間。

## 對隨從對話擴展的參考價值

RDO 適合參考「系統兼容層」：

- 若要讓一個 follower 被 vanilla/RDO-style follower 系統理解，必須正確處理 PotentialFollowerFaction、CurrentFollowerFaction、relationship rank、PlayerFollowerCount、dismiss/recruit fragments。
- SharedInfo 能減少重複，但它偏 generic；unique follower 的個性台詞不應過度放進共享池。
- Voice type matrix 可用於 generic followers；Sofia 這類 unique voice 更適合用 GetIsID 或專屬 quest/alias。
- RDO 的 MCM/variable storage 代表大型 dialogue overhaul 需要集中存放 toggles / once variables，而不是分散在每條台詞。

## 深挖：兼容層與 shared info

RDO 的重點是它很懂 Skyrim 原版 relationship/follower grammar。它不是單一角色模組，而是把大量 NPC 拉回同一套規則：

- relationship rank 決定陌生、朋友、愛人、配偶、敵意語氣。
- follower faction 決定招募、跟隨、解散、命令、等待等行為。
- voice type matrix 決定實際可播放的聲音資源。
- MCM/variable storage 決定大型 overhaul 的開關與 once 狀態。
- shared info 讓同類 follower command 可以共用條件與 fragment。

對自家 follower expansion 來說，RDO 值得深挖的不是「如何寫個性台詞」，而是「如何不被 generic dialogue overhaul 打亂」。

應特別檢查：

- 招募 topic 是否會被 `a_RDOGenericFollowerDialogue` 接走。
- dismissal / wait / command shared info 是否和自家 follower framework 重疊。
- relationship rank 改動是否會讓 spouse/friend dialogue 意外出現。
- `PlayerFollowerCount`、`CurrentFollowerFaction`、`PotentialFollowerFaction` 是否被正確維護。
- voice type form list / shared info 是否包含或排除了 unique follower voice。

## 與 unique follower 的邊界

RDO 的 generic 系統可以提供兼容性，但 unique follower 不應把核心角色 arc 交給 RDO 條件。建議邊界如下：

- 招募/解散/命令：若 follower 使用 vanilla framework，可遵守 RDO grammar；若使用自家 framework，應明確排除 RDO generic recruit。
- 關係稱呼/配偶小反應：可參考 RDO 的 relationship rank 分層。
- 角色核心對話：應獨立 quest + `GetIsID` / alias gate，不使用 shared voice-type pool。
- 系統開關：可學 RDO 集中存 variable，但不要把 narrative state 混在 MCM config 裡。

## 可操作參考

RDO 可以作為「衝突測試清單」。每新增一套 follower dialogue，至少要檢查：

- 同一 topic 下 RDO、FCO、自家 INFO 的 priority 與條件是否互斥。
- 自家 follower 是否被加入任何 generic voice type form list 或 exclusion form list。
- 招募/解散 fragment 是否會重複執行。
- 婚姻或 relationship rank 變化後，是否會解鎖不該出現的 generic spouse line。

## 對 Sofia / roadmap 的意義

- Sofia patch 應檢查與 RDO 的 follower recruit/dismiss/command topics 是否有條件競爭。若 Sofia 使用自家 follower framework，避免被 RDO generic recruit topic 接走。
- 可借 RDO 的「關係層」概念：friend / spouse / lover / hate / follower command 分層，但 Sofia 應以角色專屬狀態包裝，而非只靠 relationship rank。
- ModForge roadmap：需要能分析同一 vanilla topic 下多個 plugin 的 INFO priority/conditions，否則很難判斷 RDO/FCO/自家 patch 誰會贏。
