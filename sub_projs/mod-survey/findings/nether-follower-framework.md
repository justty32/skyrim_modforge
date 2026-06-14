# Nether's Follower Framework / NFF

- 類型：框架型 / multi-follower framework
- Plugin：`nwsFollowerFramework.esp`
- 已解壓：`~/skyrim_mods/unzip/Nether's Follower Framework - Universal Installer-55653-2-8-6b-1712793520/`
- 抽出資料：`../game-data/mods/nwsFollowerFramework/`
- 規模：48 quests，120 dialogue lines，22 NPCs，1 book，6 items，4 locations，88 magic records
- 重要附帶資料：`01 Required/Readme/Nether's Follower Framework - Guide.pdf`、`02 Scripts Source/Scripts/*.psc`

## 定位

NFF 是 follower framework，不是台詞擴展。它接管/擴展 vanilla `DialogueFollower` quest，增加最多 10 個 follower slot，並提供大量 follower 玩法功能：import/export、command power、sandbox、home bases、outfit/gear、storage、combat role、stealth、catch-up、auto-loot、regard、affinity、teach spell、sparring、expendable recruits 等。

對我們最重要的是：如果玩家主要使用 NFF，任何 follower dialogue expansion 都要清楚區分「角色自己的框架」和「NFF 借 slot / 管周邊功能」。

## 核心結構

代表 quest：

- `0750BA DialogueFollower`：NFF 覆蓋/擴展 vanilla follower quest，保留原本 follower/animal slot，額外加入 10 個 tracking targets。
- `00434F nwsFollowerController "Follower Controller Quest"`：核心控制器，招募、解散、import/export、slots、tweaks、packages。
- `006950 nwsFollowerPlayer "Follower Player Quest"`：player alias、主系統事件、restart main quests。
- `034A76 nwsFollowerMCM "Follower MCM"`：MCM。
- `179283 nwsFollowerSandbox "Follower Sandbox Quest"`：sandbox / relax。
- `08B5CB nwsFollowerHistory "Follower History"`：history/favorites。
- `0CE392 nwsFollowerHomeBase "Follower Home Base Quest"`：home bases。
- `4297C4 nwsFollowerSpells "Follower Spell Controller"`：spell teaching / follower spell behavior。
- `23CBC0 nwsFollowerVariables "Follower Variables Script"`：大量 global/faction/property hub。

主要 scripts：

- `DialogueFollowerScript.psc`：vanilla follower script replacement，保留 vanilla function 名稱但導向 NFF 流程。
- `QF_DialogueFollower_000750BA.psc`：vanilla `DialogueFollower` quest script replacement，為更多 aliases 服務。
- `nwsFollowerControllerScript.psc`：主控制器。
- `FollowerAliasScript.psc`：slot alias 腳本，regard、sandbox、role、狀態檢查多在這裡。
- `nwsFollowerCheckScript.psc`：real-time follower count。
- `nwsFollowerSandboxScript.psc`：sandbox marker、location keyword、idle 狀態。

## Regular vs Imported

NFF guide 的分類非常關鍵：

- Regular followers：使用 vanilla `DialogueFollower` 系統的 follower。NFF 可以正常 recruit/dismiss，幾乎全功能可用。
- Imported followers：自帶 custom voice / custom framework 的 follower，例如 guide 明確提到 Sofia、Serana、Mrissi 等可被 import。NFF 不 recruit/dismiss 它們，只讓它們「borrow a slot」。
- Expendable recruits：臨時招募 bandit、guard、soldier 等，偏 fluff。

對 Sofia 類角色，正確流程是：

1. 用 Sofia 自己的對話招募 Sofia。
2. 在 NFF 開啟 Import dialogue 後，對 Sofia 使用 Import。
3. NFF 將 Sofia 塞進額外 alias slot，讓她可使用部分 NFF 功能。
4. 不想讓 NFF 影響她時，先 Export。
5. 再用 Sofia 自己的對話 dismiss。

Import/Export 不是 recruit/dismiss。這點後續做 dialogue expansion 時不能混淆。

## Import 條件

`nwsFC_ImportTopic` 的實際 INFO 條件顯示 NFF 的 import 入口主要看：

- `GetPlayerTeammate == 1`：speaker 已經是 player teammate。
- 不在 `nwsFF_ImportFac`：尚未被 NFF import。
- 不在 `nwsFF_NoImport`：沒有被作者或 patch 禁止 import。
- 不在 `nwsFFImportExclude` form list。
- 沒有 child keyword。
- `nwsDlgAllowImport` MCM/quest variable 開啟。
- Sofia 有獨立分支：alias ref + `nwsSofiaParty` quest variable。
- Serana 有 Dawnguard quest variable 分支。

關鍵 records：

- `016EB1 nwsFF_ImportFac "Imported Follower Faction"`：已 import 標記。
- `5C4445 nwsFF_NoImport "No Import Faction"`：禁止 import 標記。
- `1E11BF nwsFFImportExclude`：import 排除 form list。
- `50B2A3 nwsFF_AutoImport "Auto Import Faction"`：auto import 相關，更新紀錄說 auto-import 已被降級/禁用過，需謹慎。

2.8.6b 更新紀錄明確說：mod authors 可把 follower/actor 放進 `nwsFF_NoImport` 來阻止 NFF 使用它們。這是自家 follower 做 NFF 兼容時最乾淨的 opt-out。

## Import/Export 腳本行為

`nwsFollower_Import.psc` 的 fragment 只做一件事：

```papyrus
(GetOwningQuest() as nwsFollowerControllerScript).ImportFollower_DLG(akSpeaker, 1, 2)
```

`ImportAction()` 的核心行為：

- 找是否已在 slot；不在則找空 slot。
- `ForceRefTo(myActor)` 填進 `DialogueFollower` alias。
- 加 `nwsFF_ImportFac`。
- 可按設定加入高優先 package faction。
- 加 sandbox / stealth 初始 faction。
- 不處理 vanilla follower faction，註解明確說 custom framework 應自行設定。
- 增加 `nwsImportCount`。
- 套 NFF tweaks / damage mult / history。
- `checkScript.GetFollowerCount()`、`SetSandbox()`、`EvaluatePackage()`。
- alias script `CheckFollower()` 做後續檢查。

`ExportAction()` 的核心行為：

- 找 actor 所在 slot。
- 移除 NFF package stack。
- `RemoveFromFaction(nwsFF_ImportFac)`。
- 減少 `nwsImportCount`。
- 恢復 bleedout / damage mult / speed 等 tweak。
- 清 alias forms、clear slot、`EvaluatePackage()`。
- 做 outfit dismiss cleanup。

Export 不會 dismiss 原 follower framework，也不會把 Sofia 這類角色從她自己的系統中移除。

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
