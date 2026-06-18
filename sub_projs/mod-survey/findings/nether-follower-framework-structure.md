# NFF 定位 + 核心結構 + Import/Export

← [nether-follower-framework](nether-follower-framework.md)

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

