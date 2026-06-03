<!-- 跟隨者系統 -->
# 食譜手冊 — 跟隨者系統

← [目錄](cookbook-index.md) | [lifelike 主頁](README.md)

## 「可招募的跟隨者」（雇用 → 跟隨 → 解散），遊戲內確認

艱辛習得的教訓（It.27–It.30 — 詳見 [follower gotchas](gotchas.md)，並優先用 `infodiag` 探查一切）：

- **你無法重用原版的付費雇傭兵台詞。** `HirelingQuestTopic1` 裡的每一條招募 INFO 都設有 `GetIsID==<特定原版傭兵>` 的條件，自訂 NPC 全部都不符合（而且一旦你付得起費用，該 topic 甚至會*消失*）。單靠加入 `PotentialHireling` 只能觸發拒絕台詞。
- **`SetPlayerTeammate(true)` ≠ 跟隨。** 這只會讓她為你作戰並服從指令，但實際的跟隨移動需要一個以玩家為目標的 **Follow package**。
- **不要靠掛 `CurrentFollowerFaction` 來處理解散。** 原版的解散台詞是由 *DialogueFollower quest* 驅動的，而且只會釋放它自己登記過的跟隨者；手動加入派系的 NPC 雖然會收到「你被解散了」的通知，但她依然會繼續跟著你。**請自行管理跟隨者狀態。**

**三條路線 — 優先選擇與原版整合的 (a)/(c)；它們與跟隨者管理 mod（AFT/EFF/NFF）相容，且不需要自訂指令對話。**

**(a) 免費的「Follow me, I need your help」** — 重用原版的免費跟隨 topic（`0x0B0EE6`），它以關係值＋跟隨者語音為條件，*而非* GetIsID。需要：一個跟隨者語音（例如 `FemaleEvenToned 0x013ADD`）、`PotentialFollowerFaction 0x05C84D`、**不**加 `PotentialHireling`、一個 `greeting`（使她可被對話），以及一段小型 quest script 來設定關係值（靜態的 player RELA 在執行時讀取為 0）。詳見 `examples/follower_hireable_spec.json` + `MFHireFollowerSetup.psc`。

**(c) 透過原版 `SetFollower` 付費雇用 — 推薦用於付費跟隨者（使用者偏好此方式）。** 自行撰寫付費招募 topic，但在其 fragment 中直接將 NPC 交給原版跟隨者系統：
```papyrus
Quest Property DialogueFollower Auto   ; bound to Skyrim.esm:0x0750BA
...
player.RemoveItem(Gold001, 500)
(DialogueFollower as DialogueFollowerScript).SetFollower(akSpeaker)   ; compiles vs base scripts
```
`SetFollower` 會設定關係值 + `SetPlayerTeammate` + `ForceRefTo` 的跟隨者 alias（該 alias 帶有跟隨 package 並加入 `CurrentFollowerFaction`）。此後，**原版自己的交易/等待/跟隨/解散對話全部可用**，AFT/EFF/NFF 也會識別她 — 無需自訂指令 topic。在招募台詞上設置 `GetGlobalValue PlayerFollowerCount (0x0BCC98) == 0` 的條件，以確保永遠不會佔用超過單人跟隨者名額。詳見 `examples/follower_vanilla_spec.json` + `MFHireVanillaRecruit.psc`。

> **原版跟隨者狀態下能保留什麼**（關於你的生動化成果會不會消失的疑慮）：跟隨者不過是一個 alias，它在角色的 package 堆疊上疊加了一個高優先級的*跟隨 package* — 這是附加的，並非覆蓋性的。**CombatStyle 會被保留**（已確認：`PlayerFollowerPackage`/戰鬥覆寫 package 均未設定 CombatStyle，因此角色的基礎 CSTY 主導戰鬥行為）。**你的自訂對話會被保留**，並且可以*以跟隨者狀態為條件*。**Sandbox/旅行/排程 package 只有在她主動跟著你時才會被覆蓋優先級**，一旦她被解散或被告知等待，這些 package 便會立即恢復。

**(b) 付費，完全自行管理** — *使用者認為此方式不如 (c) 理想；保留僅供參考。* 不涉及原版跟隨者系統：以帶 OWN flag 的派系作為「是我的跟隨者」狀態；OWN 的招募＋解散＋交易＋等待 topic 攜帶結果 fragment；Follow package 以 flag 為條件。優點：零衝突，無單人名額限制，可與真正的原版跟隨者並存。缺點：你需要重新實作每一條指令，且跟隨者管理 mod 看不到她。骨架（完整版：`examples/follower_paid_spec.json` + `MFHirePaidRecruit/Dismiss.psc`）：

```jsonc
{ "factions": [ { "editorId": "MF_FollowerFlag", "name": "My Follower" } ],
  "packages": [
    { "editorId": "MF_FollowPkg", "template": "Skyrim.esm:0x019B2C",
      "follow": { "target": "" },                                   // ⇒ player
      "conditions": [ { "function": "GetInFaction", "comparison": "==", "value": 1,
                        "param": "MF_FollowerFlag", "runOn": "Subject" } ] }  // follow only while hired
  ],
  "npcs": [ { "editorId": "MF_Merc", "voiceType": "Skyrim.esm:0x013ADD", "greeting": "Coin talks.",
              "factions": [ "Skyrim.esm:0x0267EA", "Skyrim.esm:0x028172" ],
              "packages": [ "MF_FollowPkg" ], "unique": true } ],
  "quests": [ { "editorId": "MF_Q", "startGameEnabled": true } ],
  "dialogue": [
    { "editorId": "MF_Hire", "questEditorId": "MF_Q", "speakerNpcEditorId": "MF_Merc",
      "prompt": "Here's 500 gold. Fight at my side.", "responses": [ "Lead the way." ], "goodbye": true,
      "conditions": [
        { "function": "GetItemCount", "comparison": ">=", "value": 500, "param": "Skyrim.esm:0x00000F",
          "runOn": "Reference", "reference": "Skyrim.esm:0x000014" },
        { "function": "GetInFaction", "comparison": "==", "value": 0, "param": "MF_FollowerFlag", "runOn": "Subject" } ],
      "resultScript": "MFHirePaidRecruit", "resultScriptSource": "scripts/MFHirePaidRecruit.psc",
      "resultProperties": [ { "name": "Gold001", "type": "object", "objectEditorId": "Skyrim.esm:0x00000F" },
        { "name": "FollowerFaction", "type": "object", "objectEditorId": "MF_FollowerFlag" },
        { "name": "GoldCost", "type": "int", "int": 500 }, { "name": "RelRank", "type": "int", "int": 3 } ] },
    { "editorId": "MF_Dismiss", "questEditorId": "MF_Q", "speakerNpcEditorId": "MF_Merc",
      "prompt": "Let's part ways.", "responses": [ "Aye." ], "goodbye": true,
      "conditions": [ { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "MF_FollowerFlag", "runOn": "Subject" } ],
      "resultScript": "MFHirePaidDismiss", "resultScriptSource": "scripts/MFHirePaidDismiss.psc",
      "resultProperties": [ { "name": "FollowerFaction", "type": "object", "objectEditorId": "MF_FollowerFlag" } ] }
  ] }
```
招募 fragment：`AddToFaction(FollowerFaction)` + `SetPlayerTeammate(true)`（在收取金幣之後）。
解散 fragment：`RemoveFromFaction(FollowerFaction)` + `SetPlayerTeammate(false)` + `EvaluatePackage()`。

**交易 / 等待 / 再次跟隨**（完整範例中也有連接這些功能 — 相同的 fragment 模式）：
- **交易**：一個以 `FollowerFlag==1` 為條件的 topic，**不**設 `goodbye`，fragment 為 `akSpeaker.OpenInventory(true)`。
- **等待 / 恢復**：使用原版本身就在用的 **`WaitingForPlayer` ActorValue**。在 Follow package 上增加 `GetActorValue WaitingForPlayer == 0`（附加於 `FollowerFlag==1`）。一個「在這裡等待」的 topic（條件 `WaitingForPlayer==0`）將其設為 1（`SetActorValue("WaitingForPlayer", 1.0)` + `EvaluatePackage`）；一個「再次跟上我」的 topic（條件 `WaitingForPlayer==1`）清除它。詳見 `MFFollowerTrade/Wait/Follow.psc`。

## 「更生動的跟隨者附加功能」 — 閒暇時光＋情境台詞（It.33，遊戲內確認）

雇用 / 跟隨的底層機制完成後，兩個簡單的功能就能讓跟隨者感覺更有生命力。兩者均在 `examples/follower_vanilla_spec.json` 中。

**閒暇行為** — 給跟隨者 NPC 一個*無條件*的 Sandbox package。這是她優先級最低的備用行為，恰好在原版跟隨者 alias package 未啟用時執行：招募前、解散後、以及被告知等待期間。她不會僵立不動，而是在被放置的地方吃東西、坐下、閒逛。
```jsonc
"packages": [ { "editorId": "MF_Sandbox", "template": "Skyrim.esm:0x01C254",
  "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter", "WorldInteractions" ],
  "sandbox": { "radius": 512, "allowEating": true, "allowSitting": true, "allowWandering": true } } ],
// ...and reference it on the npc: "packages": [ "MF_Sandbox" ]   (no condition needed)
```

**日常作息 — 在 sandbox 之上排程的 Sleep**（It.35，遊戲內確認）。閒暇 sandbox 是*白天時段*的預設行為；在其上疊加一個 **Sleep package**（template `0x019717`）讓她在夜間上床休息。請將排程的 Sleep 放在*最前面*，無條件 sandbox 放在*最後面*作為備用。
```jsonc
"packages": [
  { "editorId": "MF_NightSleep", "template": "Skyrim.esm:0x019717",
    "schedule": { "hour": 22, "durationInMinutes": 540 },          // 22:00–07:00
    "interruptFlags": [ "HellosToPlayer" ],
    "sleep": { "radius": 1024, "lockDoors": false } },             // lockDoors:false — shared inn, don't lock it
  { "editorId": "MF_Sandbox", "template": "Skyrim.esm:0x01C254", "sandbox": { ... } } ],
"packages": [ "MF_NightSleep", "MF_Sandbox" ]   // on the npc: Sleep FIRST (priority), sandbox fallback LAST
```
- **`lockDoors` 預設為 true**（NPC 會在夜間鎖上*自己的房子*）——若跟隨者睡在共用空間（例如旅館），請設為 **false**，否則她會把整棟建築鎖起來。
- 床鋪搜索以 **`NearSelf`** 為錨點——她會在 `radius` 範圍內尋找床鋪，因此請確保她被放置在一個*有*床鋪的房間中，並加大 `radius`（約 1024）。
- **更多層次**（午間用餐地點、工作台班次）遵循相同模式：在備用 package *之前*加入更多排程 package。
- 整套作息只在閒暇時段執行——當她主動跟隨時，alias package 會覆蓋她列表中的每一個 package（包括 Sleep）。

**情境對話** — 以執行時狀態作為條件，使正確的台詞只在特定情境下出現：
```jsonc
// "You're hurt?" — only when she's below half health
"conditions": [
  { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x05C84E", "runOn": "Subject" },
  { "function": "GetActorValuePercent", "comparison": "<", "value": 0.5, "actorValue": "Health", "runOn": "Subject" } ]
// "Make camp?" — only after 7pm
"conditions": [
  { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x05C84E", "runOn": "Subject" },
  { "function": "GetCurrentTime", "comparison": ">=", "value": 19 } ]
```
可用的執行時條件函數：`GetActorValuePercent`（0..1 分數，AV 引數）、`GetCurrentTime`（小時 0..24）、`IsInInterior`、`IsInCombat`、`GetRandomPercent`（0..99 隨機值，用於台詞多樣化）。

**主動閒聊**（It.34，遊戲內確認）— 使用 `banter` 區段（而非 `dialogue`）：所有共享相同（speaker, quest）的項目會合併成一個環境 topic，帶有 Random 旗標的 INFO；引擎會自行播放其中符合條件的一條。**需要啟用閒聊功能** — Sandbox package（或原版跟隨 package）提供此功能。
```jsonc
"banter": [
  { "editorId": "MF_BHurt", "questEditorId": "MF_Q", "speakerNpcEditorId": "MF_Npc",
    "responses": [ "I'm bleeding... give me a breath." ], "emotion": "Sad",
    "conditions": [
      { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x05C84E", "runOn": "Subject" },
      { "function": "GetActorValuePercent", "comparison": "<", "value": 0.4, "actorValue": "Health", "runOn": "Subject" } ] },
  { "editorId": "MF_BNight", "questEditorId": "MF_Q", "speakerNpcEditorId": "MF_Npc",
    "responses": [ "Quiet, this hour." ], "emotion": "Neutral",
    "conditions": [
      { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x05C84E", "runOn": "Subject" },
      { "function": "GetCurrentTime", "comparison": ">=", "value": 22 } ] }
]
```
每條都以 `CurrentFollowerFaction==1` 為條件（讓她只在與你同行時閒聊），再加上一個情境函數。注意：僅限環境/idle 類型——真正的戰鬥喊叫（Taunt/Attack 子類型）目前尚不支援。原版參考：`HirelingIdles`（Skyrim.esm 0x055DEB）。
