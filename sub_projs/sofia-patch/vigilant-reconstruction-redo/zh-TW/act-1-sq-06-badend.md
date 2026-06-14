# 第一章 任務 06 - 瑪索的自殺 (壞結局)

狀態：第一章結束分支的第一個基於原始碼的片段。連結優先，ESM 已驗證。

來源策略：
- 任務資料來自 `questdiag` Vigilant.esm；無幻覺。
- 對話內容連結至提取的來源，而非整段複製。
- 場景編排（若發現 SCEN）來自 CLI 診斷。

## 任務紀錄 (Quest Record)

[`4CDF8D zzzAoMMq06BadEnd "Mar'so Suicide"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:539)

CLI：
- `questdiag Vigilant.esm 0x4CDF8D`
- `infodiag Vigilant.esm 0x4CDF8D`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務資料：
- FormID: `Vigilant.esm:0x4CDF8D`
- EditorID: `zzzAoMMq06BadEnd`
- 名稱 (Name): `Mar'so Suicide`
- 旗標 (Flags): `RunOnce`
- 優先度 (Priority): `50`
- 類型 (Type): `Misc`
- 過濾器 (Filter): `AoM\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 旗標 | 日誌 |
|---:|---|---|
| 0 | 無 | 空白 |
| 100 | CompleteQuest | 空白 |

目標 (Objectives)：
- 無 (0 個目標)

## 別名 / 編排主幹 (Alias / Staging Backbone)

`questdiag` 未印出此任務的別名。此任務純粹透過 Hello 主題觸發對話。

主任務：
- [`4CDF8D zzzAoMMq06BadEnd`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:539)

## 對話分支：瑪索 (壞結局 Hello)

主題 (Topic)：
- [`4CDF8E zzzAoMMq06BadEndHello`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1191)

說話者條件模式：
- 所有 INFO 皆要求別名 `#0` 滿足 `GetIsAliasRef == 1`（隱含說話者：壞結局變體中的瑪索）。

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`4CDF8E zzzAoMMq06BadEndHello`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1191) | `4CDF8F` | Goodbye | `GetIsAliasRef alias #0` | 「再也沒有干擾了。現在只有妳跟我了，坎帕內拉。」 |
| | `4CDF90` | Goodbye | `GetIsAliasRef alias #0` | 「我會永遠待在這裡，坎帕內拉。直到永遠。」 |
| | `4CDF91` | Goodbye | `GetIsAliasRef alias #0` | 「在水池深處這裡，再也沒有人能打擾我們了。就連喬凡尼也沒辦法來到這。」 |
| | `4CDF92` | Goodbye | `GetIsAliasRef alias #2` | 「在艱難與悲傷的時刻讚美美瑞蒂亞，喔，讚美美瑞蒂亞！」 |

翻譯筆記：
- 別名 `#2` 在最後一個 INFO 中是獨立的；可能是備用的說話者條件或信徒變體。

## 重建筆記：壞結局背景

基於原始碼的背景（推論自對話與相關任務結構）：

**敘事弧：**
- 這是第一章虎人支線劇情的**壞結局**分支（與 `4D0376 zzzAoMMqGoodEnd "Art of Mercy"` 成對）。
- 主角若未能阻止瑪索，或在與坎帕內拉的道德抉擇中失敗。
- 瑪索（男性虎人）已被腐化（疑似受莫拉格·巴爾影響），並在隱喻或字面上帶走/吞噬了坎帕內拉（女性虎人）。
- 對話內容提到：
  - 瑪索與坎帕內拉合而為一（合體/佔有/死亡）。
  - 喬凡尼（第三位虎人 NPC）與他們分離。
  - 「水池」位置（遊戲世界地理位置待定）。
  - 祈求美瑞蒂亞（暗示與魔侯腐化或道德抉擇點有關）。

**相關紀錄（僅供背景參考，依 `infodiag` 顯示不屬於此任務）：**
- NPC [`001842 zzzAoMCatMale02` / `0B15B3 zzzCHMarso` – 瑪索](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:61)
- NPC [`001844 zzzAoMCatFemale01` / `2D35C3 zzzCHEpiCat01` – 坎帕內拉](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:65)
- 來自主要分支的對話背景 [`009E68 zzzAoMMq06 "Also sprach Kahjiit"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:949) — 對話主題 [`00A3E3`–`00A3F9`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:960) 顯示了坎帕內拉與瑪索的日常場景以及與喬凡尼的衝突。

**與好結局的關係：**
- 成對的任務 [`4D0376 zzzAoMMqGoodEnd "Art of Mercy"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:541) 具有相反的條件流程與結果（主角阻止了悲劇，拯救了坎帕內拉）。

## 開放驗證

- **別名所有權**：`questdiag` 未印出此任務的別名；若別名存在但未印出（例如：強制引用填入），請透過 Mutagen QUST 紀錄驗證。
- **觸發條件**：引導玩家走向壞結局而非好結局的未知階段/條件；可能位於父任務 `zzzAoMMq06` 的對話條件或腳本中。
- **儲存格/引用地理位置**：「水池」與 NPC 引用位置待定。
- **與美瑞蒂亞的關係**：別名 `#2` 的最後一個 INFO 條件暗示了魔侯色彩（祈求美瑞蒂亞）；驗證這是否為獨立的說話者或備用方案。
- **腳本行為**：階段 100 `CompleteQuest` 是自動觸發還是由腳本驅動。
