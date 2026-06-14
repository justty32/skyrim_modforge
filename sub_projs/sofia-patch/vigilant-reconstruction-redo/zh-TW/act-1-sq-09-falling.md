# 第一章 任務 09 - 無盡墜落 (Infinite Falling)

狀態：基於原始碼的片段。連結至提取的來源；CLI 診斷延後至 Manjaro 機器執行。

來源策略：
- 原始對話行連結回提取的來源檔案，而非完整複製。
- 僅在需要解釋條件或分支極性時才顯示短小的原始碼片段。
- `SCEN` 編排與任務別名需要 CLI `scenediag`/`questdiag` 的輸出；離線提取僅顯示對話/目標。

## 任務紀錄 (Quest Record)

[`00EFF7 zzzAoMMq09 "Infinite Falling"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:346)

CLI（將在 Manjaro 執行）：
- `questdiag Vigilant.esm 0x00EFF7`
- `infodiag Vigilant.esm 0x00EFF7`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自提取的 `quests.md` 的任務資料：
- FormID: `Vigilant.esm:0x00EFF7`
- EditorID: `zzzAoMMq09`
- 名稱 (Name): `Infinite Falling`
- 類型 (Type)：（提取檔案中未指定；需要 CLI）
- 優先度 (Priority)：（提取檔案中未指定；需要 CLI）
- 階段 (Stages)：（提取檔案中未指定；需要 CLI —— 據報為 20 個階段）

來自提取的 `quests.md` 的目標 (Objectives)：

| 索引 | 目標 |
|---:|---|
| 0 | [與阿爾塔諾對話 (Talk to Altano)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:347) |
| 10 | [擊敗魔族 (Defeat Daedra)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:348) |
| 15 | [尋找倖存者 (Find Survivor)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:349) |
| 20 | [追擊阿爾塔諾 (Chase Altano)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:350) |
| 40 | [擊敗莫拉格·巴爾 (Defeat Molag Bal)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:351) |

推論：
- 目標進度顯示了線性的任務流程：對話 (目標 0) → 戰鬥 (目標 10) → 搜索 (目標 15) → 追擊 (目標 20) → 最終 Boss (目標 40)。
- 20 個階段的數量與分佈在多個階段中的 5 個主要目標相符（典型模式：開場 + 每個目標的變體）。

## 別名 / 編排主幹 (Alias / Staging Backbone)

離線來源未顯示 QUST 別名定義；需要 `questdiag` 與 `scenediag` 輸出來識別：
- 主任務：`0x00EFF7 zzzAoMMq09`
- 具名別名（例如：阿爾塔諾、魔族、倖存者）
- 任何連結戰鬥/墜落動畫的 `SCEN` 紀錄

## 自訂對話分支：與阿爾塔諾的遭遇

分支開場：
- [`012642 zzAoMMq09B3AltanoTopic`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:399)

主題文本：
- [Well well well. Where have you been wasting your time all this while? You seem to be forsakend by Stendarr?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:400)
（翻譯：「喔呀喔呀，這段時間你都去哪裡混了？你似乎被斯丹達爾拋棄了？」）

說話者條件模式：
- 大多數 INFO 可能需要將階段限制在目標 0，或匹配阿爾塔諾最初遭遇的階段。
- 角色狀態：此時阿爾塔諾已被揭露為受腐化/被操弄的狀態（第一章劇情進度背景）。

### 阿爾塔諾的背叛與莫拉格·巴爾

分支：
- [`012643 zzAoMMq09B3WhyBetray`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:402)

| 主題 | 回應內容 | 翻譯 |
|---|---|---|
| [`012643 zzAoMMq09B3WhyBetray`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:402) | [「There is no need to explain the reason because you are dying.」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:403) | 阿爾塔諾拒絕解釋原因；玩家死期將至。 |
| | [「Genghis, Sent that soul to Molag bal. I must back to the altar and continue rituals.」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:404) | 阿爾塔諾命令 [`成吉思 (Genghis)`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:860) (魔人) 吞噬玩家的靈魂；阿爾塔諾回到祭壇繼續儀式。 |

推論：
- 阿爾塔諾是任務給予者的背叛者，正在進行一項儀式（根據第一章地理背景，疑似在莫拉格·巴爾祭壇進行）。
- 成吉思 (Genghis, NPC 0x00183F) 是阿爾塔諾召喚/控制的魔人 (Dremora)。
- 玩家被標記為死亡並將被吞噬靈魂；這就是「無盡墜落」事件。
- 此對話在目標 0 完成與失敗附近觸發（玩家受到攻擊/被擊敗）。

## 相關 NPC 紀錄

關鍵演員：

| FormID | EditorID | 名稱 | 角色 |
|---|---|---|---|
| [`000D62`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:835) | `zzzAoMVigilantTraitor` | 阿爾塔諾 (Altano) | 轉為叛徒的警戒者負責人；發起魔族儀式。 |
| [`00183F`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:861) | `zzzAoMBossDremora05` | 燃雨 (Ranyu) | 魔人（對話背景不明；可能是共謀者）。 |
| [`001840`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:860) | `zzzAoMBossDremora06` | 成吉思 (Genghis) | 阿爾塔諾召喚的魔人；靈魂吞噬者。 |
| [`0EFC32`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:270) | `zzzCHSummonAltano` | 阿爾塔諾 (召喚) | 另一種形式或引用（背景待定）。 |
| [`42E0B1`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv) | (memory guide?) | (guide?) | 潛在的任務樞紐（根據記憶任務模式）；詳情見 questdiag。 |

相關任務 10：
- [`013678 zzAoMMq10B1BetrayReason`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:409)：阿爾塔諾透露他在先前的一場戰鬥後受到莫拉格·巴爾的低語而腐化。暗示了一個跨章節的腐化歷程。
- [`013676 zzAoMMq10B1LastWord`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:406)：阿爾塔諾臨終前發出語無倫次的話語 (`aa.....uaa...`)，暗示精神/魔法受損嚴重。

## 倖存者對話 (目標 15)

戰鬥後發現的圖書館員：
- [`027FB3 zzzAoMMq09B4LirarianWound`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:439)

| 主題 | 回應內容 |
|---|---|
| 問候 (Greeting) | [「Please...Help me...」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:440) |
| | [「Let me rest...」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:441) |

（翻譯：「請……救救我……」「讓我休息吧……」）

### 發生了什麼事？

分支：
- [`027FB5 zzzAoMMq09B4WhatHappen`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:443)

| 主題 | 提示與回應 |
|---|---|
| [`027FB5`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:443) | 提示：[`"It's okay. What was happening?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:443) 回應：[`"Altano...Altano summoned Daedra...all of a sudden...we did not understand what happened"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:444) 以及 [`"I could not do anything ... Daedra killed Thorondir and others..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:445) |
| [`027FB7 zzzAoMMq09B4Isee`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:447) | 提示：[`"I see..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:447) 回應：[`"Please stop Altano...he is trying to be outrageous ..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:448) |
| [`027FB9 zzzAoMMq09B4understand`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:450) | 提示：[`"I understand, you should get some rest"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:450) 回應：[`"Yeah, I mind was relieved gone missing ... I've been allowed to do so ..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:451) |

翻譯筆記：
- 提示 027FB5 翻譯：「沒事了。發生了什麼事？」回應：「阿爾塔諾……阿爾塔諾召喚了魔族……突然間……我們完全不知道發生了什麼事。」以及「我無能為力……魔族殺了索隆迪爾和其他人……」
- 「索隆迪爾和其他人 (Thorondir and others)」指的是在魔族入侵中喪生的斯丹達爾神殿負責人們。根據第一章設定，索隆迪爾是首席負責人。
- 「outrageous (暴走)」暗示阿爾塔諾已失去理性控制；他是個傀儡 / 完全被腐化了，而非出於自願行惡。
- 圖書館員的回應「gone missing」在原始碼中不明確；可能意指失去意識或記憶。

## 與莫拉格·巴爾的遭遇 (目標 40)

直接與莫拉格·巴爾對話：
- FormID: [`10C89A zzzAoMSummonDragonMolagBal`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:437)（莫拉格·巴爾第一章的召喚/Boss 形式）

莫拉格·巴爾的登場：
- [`013BE5` [Scene/Scene]](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:417)

| 場景行 (Scene Line) | 翻譯 |
|---|---|
| [「Son of Stendarr..I see you. When your soul is corrupt, you open the gate of my realm...」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:417) | 莫拉格·巴爾認出玩家是名警戒者；靈魂的腐化開啟了通往湮滅的大門。 |

推論：
- 這是任務 09 的高潮場景：玩家直接面對莫拉格·巴爾。
- 莫拉格·巴爾的出現取決於靈魂的腐化（與「無盡墜落」狀態相關聯）。
- 擊敗莫拉格·巴爾是完成目標 40 與該任務的必要條件。

## 地點紀錄 (Location Records)

關鍵地點：

| FormID | EditorID | 類型 | 名稱 |
|---|---|---|---|
| [`004102`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:38) | `zzzAoMHallofMolagBal` | CELL | 莫拉格·巴爾大廳 |
| [`26D3A8`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:497) | `zzzAoMLocAltarMolag` | LCTN | 莫拉格·巴爾祭壇 |

推論：
- 任務 09 發生在莫拉格·巴爾祭壇或其附近，即阿爾塔諾進行儀的地點。
- 「無盡墜落」這個標題暗示了墜入/降入湮滅，或是在祭壇構造內的魔法墜落。

## 重建筆記

基於原始碼：
- [`00EFF7 zzzAoMMq09 "Infinite Falling"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:346) 是一個 20 階段的任務，追蹤玩家發現阿爾塔諾的背叛、與魔族戰鬥、審問倖存者（圖書館員），以及與莫拉格·巴爾的最終對峙。
- 該任務是第一章的高潮，發生在神殿被褻瀆以及揭露警戒者領導層已被腐化之後。
- 它包含至少一個自訂對話分支（阿爾塔諾 + 莫拉格·巴爾），且可能有一個或多個關於墜落序列與最終 Boss 編排的 `SCEN` 紀錄。
- 「無盡墜落」這個隱喻可能指玩家在遭遇過程中靈魂被莫拉格·巴爾拖入湮滅。

流程摘要：
1. **目標 0**：接近阿爾塔諾以得知背叛。
2. **目標 10**：擊敗阿爾塔諾召喚的魔族（戰鬥階段）。
3. **目標 15**：尋找並審問倖存者圖書館員。
4. **目標 20**：追擊阿爾塔諾（追逐階段）。
5. **目標 40**：擊敗莫拉格·巴爾（最終 Boss；可能自動完成任務或觸發完成處理程序）。

開放驗證：
- 執行 `questdiag Vigilant.esm 0x00EFF7` 以轉儲所有階段、旗標與階段日誌項目。
- 執行 `infodiag Vigilant.esm 0x00EFF7` 以列舉此任務擁有的所有對話主題與 INFO。
- 對發現的任何 `SCEN` 紀錄（名稱可能匹配 `*MolagBal*`、`*Falling*` 或 `*Altano*`）執行 `scenediag` 以提取階段/動作細節。
- 驗證阿爾塔諾、成吉思與莫拉格·巴爾召喚形式的 NPC 紀錄，以確認其角色與對話條件。
- 檢查 `莫拉格·巴爾祭壇` 地點紀錄與任何室內儲存格引用，以確認地理位置與階段限制。
- 檢查最終對話抉擇（可能是個 `Goodbye/SayOnce` INFO）上的任何 VMAD Papyrus 片段，以瞭解任務完成路由。
