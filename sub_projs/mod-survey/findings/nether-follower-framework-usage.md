# Recruit/Dismiss + 計數 + 可利用功能 + roadmap

← [nether-follower-framework](nether-follower-framework.md)

## Recruit/Dismiss 腳本行為

Regular follower 走 `RecruitAction()`：

- 找空 alias slot，`ForceRefTo(myActor)`。
- 必要時 relationship rank 設到 3。
- `SetPlayerTeammate()`。
- `IgnoreFriendlyHits(true)`。
- 加入 vanilla `CurrentFollowerFaction` / `PlayerFollowerFaction`，但用 faction rank 0 表示 active。
- 移除 `DismissedFollowerFaction`、加入 comment faction。
- 套 NFF tweaks、damage mult、history、sandbox。

Dismiss 走 `RemoveAction()`：

- 移除 package stack。
- `SetPlayerTeammate(false)`、`WaitingForPlayer = 0`、`IgnoreFriendlyHits(false)`。
- 加入 dismissed faction、移除 hireling/comment factions。
- NFF 不直接移除 vanilla Current/PlayerFollower factions，而是設 rank `-1`；更新紀錄說這是較正確的方式。
- 清 slot、outfit cleanup、更新 follower count。

所以做自家 follower 時，如果它是 custom framework，就不要讓 NFF 走 regular recruit/dismiss；應走 import/export 或直接加 `nwsFF_NoImport` opt-out。

## Real-Time Follower Count

`nwsFollowerCheckScript.GetFollowerCount()` 會掃 `DialogueFollower` aliases，更新：

- `nwsFollowerCount`
- `nwsFollowerLastCount`
- vanilla `PlayerFollowerCount`
- vanilla `PlayerAnimalCount`

如果 NFF slots 用滿，會把 vanilla follower/animal count 設成 1，防止 vanilla 系統再招募。若 game slot 被使用，也會把 `PlayerFollowerCount` 設成 1。

這代表 dialogue condition 若只看 vanilla `PlayerFollowerCount`，在 NFF 環境下只能知道「NFF 不希望你再招募」，不能準確知道「哪個 follower 跟著玩家」。要知道特定 follower，應看 actor ID、alias、`GetPlayerTeammate`、NFF import faction、或自己的 follower state。

## 可被我們利用的功能

NFF 能直接支援 follower expansion 的周邊玩法：

- Import slot：讓 Sofia/custom follower 被 NFF command/sandbox/storage 等功能看到，但不接管其核心 follower framework。
- `nwsFF_NoImport`：自家 follower 不想被 NFF 動時的官方 opt-out。
- `nwsFF_ImportFac`：判斷當前 actor 是否已被 NFF 借 slot。
- Sandbox：可參考其 player idle、dialogue busy、location keyword、avoidance、home/town mode。
- Regard：NFF 自帶 relationship-like 0-9 ranks，但它與 vanilla relationship rank 無關；可用作外部參考，不應替代角色自己的 relationship arc。
- Home bases / outfits / storage：適合作為玩家管理層，不適合作為 narrative truth。
- Command Followers power：可作為可用性參考，避免自家 dialogue 和 NFF command menu 重疊太多。

## 對 Sofia / 自家隨從對話擴展的意義

- Sofia 應視為 Imported follower，而不是 Regular follower。她自己的招募、解散、個性對話、romance/moral state 應留在 Sofia 系統。
- 對話條件不要只依賴 `PlayerFollowerCount`。NFF 會主動維護它，且它在多 follower 下語義變成「是否阻止招募」。
- 如果要判斷 Sofia 是否在隊伍，優先用 Sofia 自家 quest/alias/global，其次 `GetPlayerTeammate`，再輔助判斷 `nwsFF_ImportFac`。
- 若某段動作/scene 不能接受 NFF sandbox/package 干擾，應在 scene 前設自家 busy state，必要時暫停/避開 sandbox；不要假設 NFF 會知道你的劇情 scene。
- 若自家 follower 不能被 NFF 管，做 patch 把 actor 加進 `nwsFF_NoImport`。這比和 import topic 條件搶 priority 乾淨。
- 若要與 NFF 友好，最好只把 NFF 當「管理層」：slot、command、storage、sandbox；核心敘事狀態仍由自家 quest/global/script 管。

## 與 RDO / FCO / GYH 的關係

- RDO：NFF installer 有 RDO replacement scripts 選項；guide 建議 NFF 需要覆蓋相關 scripts 才能把 recruit/dismiss/wait/follow 導向 NFF。自家 patch 不應再覆蓋這些 fragment，否則會形成三方衝突。
- FCO：NFF 有 `nwsFollowerPack_*_IdleBlock` 類 idle block topic，也會控制 sandbox/idle 行為；ambient commentary 要考慮 NFF 的 player dialogue busy 和 sandbox。
- GYH：GYH 已有 Sofia/NFF 類 follower compatibility。若角色已被 NFF import，GYH 的 scene protection / DoNotMove / camera cleanup 仍很重要，因為 NFF 只是 follower 管理框架，不是 action service。

## Roadmap

- ModForge 需要能分析 vanilla `DialogueFollower` quest 被 NFF/RDO/自家 patch 同時修改時的 alias、script、topic fragment 差異。
- 需要支援查詢 faction rank 語義：NFF 用 rank `-1` 表示 inactive，比單純 `GetInFaction` 更細。
- 需要支援 Papyrus source/fragment cross-reference：例如從 `nwsFC_ImportTopic` 追到 `ImportFollower_DLG()` 再追到 `ImportAction()`。
- 做 follower dialogue expansion 時，應加入 NFF compatibility checklist：regular/imported/no-import、PlayerFollowerCount 語義、sandbox/package 干擾、RDO script replacement。
