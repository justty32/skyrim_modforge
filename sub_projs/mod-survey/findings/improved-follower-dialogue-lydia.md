# Improved Follower Dialogue - Lydia / IFDL

- 類型：內容型 / unique follower dialogue expansion
- Plugin：`ImprovedCompanionsBoogaloo.esp`
- 已解壓：`~/skyrim_mods/unzip/Improved Follower Dialogue - Lydia-38473-4-2-2-1722555312/`
- 抽出資料：`../game-data/mods/ImprovedCompanionsBoogaloo/`
- 規模：25 quests，1292 dialogue lines，18 NPCs，2 books，2 items，26 locations，8 magic records

## 結構

IFDL 是單一 follower 深改。它不是只補 idle lines，而是把 Lydia 變成有長期關係進度、主線反應、DLC 反應、婚姻/家庭互動、道德邊界與個人任務的角色。

代表 quest：

- `005905 _ICBLydia`：主 Lydia 對話與主線進度反應。
- `3ABBDE 0ICB_LydiaExtraConversations`：額外 conversations，有 objective，例如「Get a room at an inn in the Northeast with Lydia」。
- `47BC85 0ICB_LydiaMoralObjection`：道德反對系統。
- `159008 0ICB_LydiaPersonalQuest "The King of Cowards"`：完整個人任務。
- 多個 `0ICBScene_Dawnguard_*` / `_ICBScene_Lydia*`：scene 化的旅途/劇情互動。

## 條件與狀態管理

IFDL 的主體 pattern 是「vanilla quest stage + 自己的 stage/global/VM quest variable + Lydia ID」：

- `GetIsID == Lydia (0A2C8E)`：確保台詞只屬於 Lydia。
- `GetStage` / `GetStageDone` 對 vanilla MQ、Dawnguard、Dragonborn、Daedric quests 做劇情定位。
- 自家 quest stage，例如 `_ICBLydia` stage 5/10/20/30... 表示 Lydia 對玩家與主線的關係進度。
- 自家 global，例如 romance / DLC conversation gate，用來避免重複或記錄分支。
- `GetVMQuestVariable` 用在 moral objection，記錄各條道德線是否已觸發。
- `VMAD` fragment 常見於玩家選項後，用來推 stage、設 global、dismiss follower、處理說服成敗等狀態轉移。

例：`ICB_PML01` 需要 `_ICBLydia` stage 5、主線 `MQ104` stage >= 160、speaker 是 Lydia，才出現「剛任命 housecarl 後對龍戰與 thane 身分的質疑」。玩家選項還會依角色等級分支，`GetLevel >= 25` 與 `< 25` 對應不同 prompt/response。

## 道德反對系統

`0ICB_LydiaMoralObjection` 是很值得參考的設計：

- 以 `Misc/Hello` 高優先度觸發，當玩家進入特定 quest/location/stage 時 Lydia 主動攔話。
- 條件會同時檢查 Lydia 是 follower、玩家進度、地點、該 moral objection 是否尚未觸發。
- 玩家可選擇堅持、romance appeal、persuade、甚至 enthrall；成功/失敗用 Speech 或 Illusion actor value 對比 global threshold。
- 結果不是純台詞：有 Goodbye、VMAD fragment、follower 離隊、回家、恢復關係、要求贖罪 objective 等後果。

這和一般「多幾句反應」不同，是把 follower 的價值觀做成玩法約束。

## 對隨從對話擴展的參考價值

IFDL 是最貼近 Sofia patch 的參考：

- 使用單一角色 ID 鎖定，不靠 voice type 泛化。
- 用自家 quest stage 管長期 arc，避免只靠 vanilla stages 造成台詞無序。
- 用 `Hello` 主動拋出重要對話，用 `Topic/Custom` 承接玩家分支。
- 對主線/DLC/Daedric 內容採「狀態閘門 + once flag/global」避免重複。
- 把 romance、婚姻、家庭、道德界線納入同一套狀態機。
- Scenes 用於雙人演出或特定地點觀景/劇情段落，比單句 idle 更有存在感。

## 對 Sofia / roadmap 的意義

- Sofia patch 可直接借這個架構：一個主 expansion quest 管 Sofia relationship state，若干 scene quests 管特定演出，moral/romance/quest reactions 用獨立 globals/quest vars 防重。
- 風險：IFDL 對 Lydia 的 follower command topics 有覆蓋；Sofia 不能照搬 vanilla follower topic 寫法，應保持 Sofia unique topic / aliases，以免被 RDO/FCO/NFF 等 generic follower overhaul 影響。
- ModForge roadmap：需要更好支援 INFO fragment / VMAD、quest variable 條件、branch/topic 樹、stage-driven dialogue graph 的檢視。

