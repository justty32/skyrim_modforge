# Follower Commentary Overhaul SE / FCO

- 類型：內容型 / generic follower commentary
- Plugin：`FCO - Follower Commentary Overhaul.esp`
- 已解壓：`~/skyrim_mods/unzip/SE FCO/`
- 抽出資料：`../game-data/mods/FCO - Follower Commentary Overhaul/`
- 規模：20 quests，1427 dialogue lines，無 NPC / item / location / magic 新資料

## 結構

FCO 的核心做法是「按 vanilla voice type 分 quest」，把大量 `Misc/Idle`、`Misc/Hello`、`Misc/Goodbye`、combat / detection / favor command 台詞掛回原版 dialogue 類型。

代表 quest：

- `014727 DialogueFemaleEvenTonedCommentary`
- `020DC6 DialogueFemaleYoungEagerCommentary`
- `034BFC DialogueMaleKhajiitCommentary`
- `034CAB DialogueMaleBruteCommentary`
- `03C470 FCOConditionBankMaleFemale`
- `0401F8 FCO_MCM`

`FCOConditionBankMaleFemale` 很像條件模板庫：INFO response 是佔位文字，例如 `[ABOUT CAVES & MINES]`、`[IN DANGER IN ANY DUNGEON]`、`[PLAYER IS A DUDE]`，條件則覆蓋地點、keyword、玩家狀態、時間、裝備、疾病、技能、quest completion 等。這應該是作者用來複製條件組的工作台。

## 條件 pattern

FCO 的台詞多數直接以 INFO 條件控制，不靠複雜腳本。典型條件：

- `GetIsVoiceType == <vanilla voice type>`：決定哪種聲線能說。
- `IsSneaking == 0`：避免潛行時 idle chatter 亂出。
- `LocationHasKeyword` / `GetInCurrentLoc` / `GetInWorldspace`：地點與 dungeon 類型 commentary。
- `GetQuestCompleted` / `GetQuestRunning` / `GetStageDone`：少量主線、內戰、Daedric quest 對應台詞。
- `GetCurrentTime` / `IsSnowing`：時間、天氣 barks。
- `WornHasKeyword` / `GetItemCount` / `GetDisease` / `GetActorValue`：玩家裝備、物品、疾病、技能反應。
- `Random` flag 常用；`SayOnce` 少量使用在特定狀態提醒。

例：`DialogueFemaleEvenTonedCommentary` 的 idle topic `014728` 會用 `GetIsVoiceType FemaleEvenToned` 加地點 keyword、玩家疾病、時間、玩家等級等條件挑台詞。

## 對隨從對話擴展的參考價值

FCO 適合參考「低侵入式 ambient commentary」：

- 不重寫 follower quest，而是在 existing dialogue categories 裡補 INFO。
- 以 voice type 做可重用覆蓋，適合 generic followers，不適合 Sofia 這種 unique voice follower 直接照搬。
- 條件粒度很細，可以建立一套「場景觸發詞庫」：地點 keyword、quest stage、天氣、時間、玩家狀態。
- `condition bank` 的做法值得 ModForge roadmap 參考：需要能複製/模板化 CTDA condition block，否則大量手工條件很痛。

## 對 Sofia / roadmap 的意義

- Sofia patch 若要做旅途中自然 commentary，可借 FCO 的條件分類，而不是借 voice-type 分發。
- 相容風險：FCO 會改 generic follower/favor/combat/detection 類型台詞；Sofia 若使用自己獨立 quest / unique voice，通常不直接衝突，但若 patch 掛到 vanilla follower topics 需注意優先度與條件競爭。
- ModForge roadmap：需要支援「大量 INFO 批次建立」、「條件模板」、「Random / SayOnce flags」、「runOn Subject vs PlayerRef vs Target」的可視檢查。

