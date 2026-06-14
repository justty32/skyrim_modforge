# 第一章 支線任務 04 - 瘋狂之眼 (Eye of Madness)

狀態：第一個重製片段。基於原始碼，連結優先，無 Gemini 幻覺。

來源策略：
- 原始對話行連結回提取的來源檔案，而非完整複製。
- 僅在需要解釋背景或條件時才顯示短小的原始碼片段。
- 對話驅動型任務，包含選用的美瑞蒂亞信徒分支；未偵測到 `SCEN` 編排。

## 任務紀錄 (Quest Record)

[`0082EA zzzAoMMq04 "Eye of Madness"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:393)

CLI：
- `questdiag Vigilant.esm 0x0082EA`
- `infodiag Vigilant.esm 0x0082EA`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務資料：
- FormID: `Vigilant.esm:0x0082EA`
- EditorID: `zzzAoMMq04`
- 名稱 (Name): `Eye of Madness`
- 旗標 (Flags): `RunOnce`
- 優先度 (Priority): `90`
- 類型 (Type): `SideQuest`
- 過濾器 (Filter): `AoM\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 旗標 | 日誌 |
|---:|---|---|
| 0 | 無 | 空白 |
| 10 | 無 | 空白 |
| 20 | 無 | 空白 |
| 30 | 無 | 空白 |
| 40 | CompleteQuest | 空白 |
| 50 | 無 | 空白 |
| 255 | ShutDownStage | 空白 |

目標 (Objectives)：

| 索引 | 來源 | 文本 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:394) | 在燭爐堂與阿爾塔諾對話 (Talk to Altano in the Candle Hearth Hall) |
| 10 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:395) | 調查關於瘋狂之眼的事 (Investigate about Mad eye) |
| 20 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:396) | 向阿爾塔諾回報 (Report to Altano) |
| 21 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:397) | 聽取美瑞蒂亞信徒的建議（選項） (Take advice from Meridia beliver (Option)) |
| 30 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:398) | 殺死巴洛爾 (Kill Balor) |
| 40 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:399) | 向阿爾塔諾回報 (Report to Altano) |

目標對象 (Objective targets)：
- 目標 0：1 個對象，0 個條件。
- 目標 10：1 個對象，0 個條件。
- 目標 20：1 個對象，0 個條件。
- 目標 21：1 個對象（美瑞蒂亞信徒聯絡人），有條件。
- 目標 30：1 個對象（巴洛爾），0 個條件。
- 目標 40：1 個對象，0 個條件。
- 目前的 CLI 輸出未印出目標儲存格/引用細節；若位置標記很重要，則需要更深入的 QUST 目標轉儲。

## 別名 / 編排主幹 (Alias / Staging Backbone)

`infodiag` 未偵測到自訂的 `SCEN` 紀錄。任務進度由對話驅動，在階段 20 有一個受階段限制的分支點（斯丹達爾對決美瑞蒂亞路徑）。

主任務：
- `0082EA zzzAoMMq04` "Eye of Madness"

來自 `infodiag` 的對話別名：
- 別名 `#0`：預期為 `阿爾塔諾 (Altano)`（主要任務發布者，目標 0, 20, 40）。
- 別名 `#1`：預期為目標 NPC `巴洛爾 (Balor)`（目標 30）。
- 別名 `#2`：預期為美瑞蒂亞信徒聯絡人（目標 21，選用分支）。

（推論：別名角色是從對話條件 `GetIsAliasRef` 索引推斷而來；CLI 未提供明確的別名轉儲）

## NPC 紀錄

主要目標 NPC：
- [`0012D5 zzzAoMm04Thief` - 巴洛爾 (Balor, 小偷)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv) —— 受瘋狂之眼詛咒的人，目標 30 的任務目標。
- [`0B161E zzzCHBalor` - 巴洛爾 (次要紀錄)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv) —— 可能是第四章冷港中的顯化或重複紀錄。

## 自訂對話分支

### 分支 1：任務開場 — 「你似乎在沉思……發生了什麼事？」

主題 (TOPIC) `0x00934B zzAoMMq04B1Mission4`

條件模式：
- `GetStage < 10`：在玩家完成初始對話前觸發。
- `GetIsAliasRef alias #0` (阿爾塔諾)。

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x00934B zzAoMMq04B1Mission4` | `0x00934C` | 無 | `GetStage < 10`; `GetIsAliasRef alias #0` | 提示：[`"You seem to contemplate...what happened?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:119) 回應 (中立)：[`"I heard a strange rumor from gurads. There is a baleful man who has mad eye in Kynesgrove"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:120) 回應 (中立)：[`"For Stendarr, I can not overlook. Istead of me, check whether the rumor was true or not."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:121) |

VMAD 片段：
- （推論：可能設定階段 10+ 以推進任務）
（翻譯：提示「你似乎在沉思……發生了什麼事？」回應 1「我從衛兵那裡聽到一個奇怪的傳聞。基尼之林有個長著瘋狂之眼的邪惡男人。」回應 2「為了斯丹達爾，我不能坐視不管。替我走一趟，確認傳聞是否屬實。」）

### 分支 2：調查 — 審問巴洛爾

主題 (TOPIC) `0x00934E zzAoMMq04B2MadEye`

條件模式：
- `GetStage >= 10 && < 20`：調查階段。
- `GetIsAliasRef alias #1` (巴洛爾)。

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x00934E zzAoMMq04B2MadEye` | `0x00934F` | 無 | `GetStage >= 10 && < 20`; `GetIsAliasRef alias #1` | 提示：[`"I heard you have mad eye. Is that true?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:123) 回應 (疲憊)：[`"Yes yes yes, so....what? I am very tired....leave me alone..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:124) |

（翻譯：提示「我聽說你長著瘋狂之眼，是真的嗎？」回應「是的是的是的，那又怎樣？我很累了……離我遠點……」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x009351 zzAoMMq04B3HowGet` | `0x009352` | 無 | `GetIsAliasRef alias #1` | 提示：[`"Tell me about mad eye"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:126) 回應：[`"I lost a bet to a woman. Then, she scooped out my right eye and embed a jewelry.."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:127) 回應：[`"After that...people who see go mad...I am tired..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:128) |

（翻譯：提示「跟我說說瘋狂之眼的事」回應 1「我在一場賭局中輸給了一個女人。然後，她挖出了我的右眼，在裡面鑲嵌了一顆珠寶……」回應 2「在那之後……看到的人都瘋了……我很累……」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x009353 zzAoMMq04AboutWoman` | `0x009354` | 無 | `GetIsAliasRef alias #1` | 提示：[`"Do you remember the woman?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:130) 回應 (醉酒)：[`"I don't remember because I was drunken...I remember!!She has a sexy hip..haha.."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:131) |

（翻譯：提示「你還記得那個女人嗎？」回應「我不記得了，因為當時我喝醉了……我想起來了！！她的屁股很性感……哈哈……」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x009355 zzAoMMq04GiveMercy` | `0x009356` | 無 | `GetIsAliasRef alias #1` | 提示：[`"Do you need the mercy of ..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:133) 回應：[`"No, thank you. I don't want to die. Mad eye can be prevented by this bandage."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:134) 回應：[`"I don't use this power unless fools attacks me. Do you understand? leave me alone..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:135) |

（翻譯：提示「你需要……的慈悲嗎？」回應 1「不，謝謝。我不想死。用這條繃帶就能遮住瘋狂之眼。」回應 2「除非有傻瓜攻擊我，否則我不會使用這股力量。你明白了嗎？離我遠點……」）

### 分支 3：向阿爾塔諾回報 — 斯丹達爾路徑

主題 (TOPIC) `0x009358 zzAoMMq04B4RumorTrueTopic`

條件模式：
- `GetStage >= 20 && < 30`：回報階段。
- `GetIsAliasRef alias #0` (阿爾塔諾)。
- 根據特定回應（殺死巴洛爾）觸發階段 30。

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x009358 zzAoMMq04B4RumorTrueTopic` | `0x009359` | 無 | `GetStage >= 20 && < 30`; `GetIsAliasRef alias #0` | 提示：[`"Rumor is true. but Balor is not hostil."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:137) 回應：[`"The man, Balor must be sent to Stendarr."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:138) 回應：[`"If Balor yield to the power, can you anticipate what will happen? Many people in Skyrim will be sufferd."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:139) |

（翻譯：提示「傳聞是真的。但巴洛爾並沒有敵意。」回應 1「那個人，巴洛爾，必須被送去見斯丹達爾。」回應 2「如果巴洛爾屈服於這股力量，你能想像會發生什麼事嗎？天際省的許多人都會受苦。」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x00935A zzAoMMq04B4Yes` | `0x00935B` | 無 | `GetIsAliasRef alias #0` | 提示：[`"OK,I will kill him under the name of Stendarr."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:141) 回應：[`"Good, the mercy of Stendarr is not compassion"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:142) |

（翻譯：提示「好吧，我會以斯丹達爾之名殺了他。」回應「很好，斯丹達爾的慈悲並非同情心。」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x00935C zzAoMMq04B4No` | `0x00935D` | 無 | `GetIsAliasRef alias #0` | 提示：[`"I can not..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:144) 回應：[`"You must do. If not, you are not vigilant of Stendarr."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:145) |

（翻譯：提示「我做不到……」回應「你必須做。否則，你就不配當斯丹達爾的警戒者。」）

VMAD 片段：
- （推論：B4Yes 與 B4No 的選擇可能根據玩家回應分支出階段 30 的 SetStage）

### 分支 4：殺戮 / 慈悲路徑 — 與巴洛爾的最終對峙

主題 (TOPIC) `0x00935F zzAoMMq04B5KillBalorTopic`

條件模式：
- `GetStage >= 30`：巴洛爾最終階段。
- `GetIsAliasRef alias #1` (巴洛爾)。

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x00935F zzAoMMq04B5KillBalorTopic` | `0x009360` | 無 | `GetStage >= 30`; `GetIsAliasRef alias #1` | 回應 (死心)：[`"Why don't you leave me alone?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:148) |

（翻譯：「為什麼就不肯放過我？」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x009361 zzAoMMq04B5MustDie` | `0x009362` | 無 | `GetIsAliasRef alias #1` | 提示：[`"You must die, Stenndarr waiting for you"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:150) 回應：[`"it is your answer...then you and I...must do one thing..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:151) |

（翻譯：提示「你必須死，斯丹達爾在等著你。」回應「這就是你的回答嗎……那麼你和我……就必須做一件事了……」）

### 分支 5：任務完成 — 回報巴洛爾之死

主題 (TOPIC) `0x009364 zzAoMMq04B6Mission4Comp`

條件模式：
- `GetStage >= 40`：完成階段。
- 玩家持有巴洛爾的物品（若遊戲狀態有追蹤）。
- `GetIsAliasRef alias #0` (阿爾塔諾)。

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x009364 zzAoMMq04B6Mission4Comp` | `0x009365` | 無 | `GetStage >= 40`; `GetIsAliasRef alias #0` | 提示：[`"I killed Balor..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:153) 回應：[`"Don't let it get to you. so...have a drink?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:154) |

VMAD 片段：
- （推論：可能完成任務或轉換至階段 50）
（翻譯：提示「我殺了巴洛爾……」回應「別放在心上。那麼……來一杯？」）

## 選項：美瑞蒂亞信徒分支

此分支作為任務目標 21「聽取美瑞蒂亞信徒的建議（選項）」出現。它提供了透過美瑞蒂亞信仰與光魔法來處理巴洛爾詛咒的替代方案，與斯丹達爾「以死解脫」的路徑分道揚鑣。

### 信徒主題：問候 / 告別

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA978 zzzAoMMq04Hello` | `0x4CA979` | 無 | 無 | [`"Do you believe  Meridia? I alway believe the brilliance of her."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2986) [`"Oh, Meridia. Wonderful brilliance."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2987) |

（翻譯：「你信仰美瑞蒂亞嗎？我始終信仰著她的光輝。」「喔，美瑞蒂亞。美妙的光輝。」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA97B zzzAoMMq04GoodBye` | `0x4CA97C` | 無 | 無 | [`"Meridia's light with you"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2990) |

（翻譯：「美瑞蒂亞之光與你同在」）

### 信徒分支 01：招募與說服

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA97F zzzAoMMq04CultB01T01` | `0x4CA980` | 無 | 無 | 提示：[`"Sorry. I'm in the service of Stendarr now."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2992) 回應：[`"Do not serve Stendaar. Justice has been blind since ancient times, a word that has been overused and is now worthless."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2993) 回應：[`"Then trust in Meridia. Her light will be a spark that will illuminate your path."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2994) |

（翻譯：提示「抱歉。我現在正為斯丹達爾效力。」回應 1「不要為斯丹達爾效力。正義自古以來就是盲目的，這個詞被過度使用，現在已經毫無價值了。」回應 2「那麼，信賴美瑞蒂亞吧。她的光芒將成為照亮你道路的火花。」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA981 zzzAoMMq04CultB01T02` | `0x4CA982` | 無 | 無 | 提示：[`"Oh, come on, you evil bastard. You want to get dimed, hmm?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2996) 回應：[`"The pagans are the eight divines. And the pagans are those of you who worship them. Make no mistake about it."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2997) |

（翻譯：提示「喔，省省吧，你這個邪惡的混蛋。你想被淨化嗎，嗯？」回應「異教徒是那八聖靈。而異教徒正是崇拜他們的你們。這點絕不會錯。」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA983 zzzAoMMq04CultB01T03` | `0x4CA984` | 無 | 無 | 提示：[`"That's Lady Meridia for you."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2999) 回應：[`"I know, I know. You seem very wise. You should be more diligent."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3000) |

（翻譯：提示「真不愧是美瑞蒂亞女士的信徒。」回應「我知道，我知道。你看起來很聰明。你應該更勤奮點。」）

### 信徒分支 02：透過光魔法解決巴洛爾的詛咒

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA986 zzzAoMMq04CultB02T01` | `0x4CA987` | 無 | 無 | 提示：[`"What do you think about the man with the mad eyes?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3002) 回應：[`"It was a curse that if you looked into those eyes, you would go insane. Did you know that light is involved in seeing?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3003) 回應：[`"Yes, it is the light of Meridia. With the worship of the goddess and more light, it should be easy to break the curse."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3004) 回應：[`"I can help you if you want. That is, if you're willing to trust Meridia."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3005) |

（翻譯：提示「你對那個長著瘋狂之眼的男人有什麼看法？」回應 1「那是一個詛咒，如果你看著那雙眼睛，你就會發瘋。你知道視覺與光有關嗎？」回應 2「沒錯，那是美瑞蒂亞之光。只要崇拜女神並使用更多的光，解開詛咒應該很容易。」回應 3「如果你願意的話，我可以幫你。前提是你願意信賴美瑞蒂亞。」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA988 zzzAoMMq04CultB02T02` | `0x4CA989` | 無 | 無 | 提示：[`"Stop sales talk. I won't be so easy to get on board with"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3007) 回應：[`"Hmm, but are you sure? Pride and morality will not break the curse, and will not save him."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3008) 回應：[`"Now it's time to believe in Meridia. You have now found the right faith."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3009) |

（翻譯：提示「別在那裡推銷了。我沒那麼容易上鉤。」回應 1「嗯，但你確定嗎？自尊和道德解不開詛咒，也救不了他。」回應 2「現在是信仰美瑞蒂亞的時候了。你已經找到了正確的信仰。」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA98A zzzAoMMq04CultB02T03` | `0x4CA98B` | 無 | 無 | 提示：[`"I'll believe Meridia (frustrated)"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3011) 回應：[`"It's not very heartfelt, but okay. I will lend you the power of Meridia."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3012) 回應：[`"There are two wands. A strong light and a weak light. Choose whichever you prefer."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3013) |

（翻譯：提示「我信美瑞蒂亞就是了（懊惱地）。」回應 1「雖然聽起來不太真心，但好吧。我會借給你美瑞蒂亞的力量。」回應 2「這裡有兩根法杖。一支是強光，一支是弱光。選你喜歡的吧。」）

### 信徒分支 03：選擇光之法杖

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA98C zzzAoMMq04CultB02T04` | `0x4CA98D` | 無 | 無 | 提示：[`"I need a strong light."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3015) 回應：[`"Now, take it. Let the light of Meridia shine upon this world."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3016) |

（翻譯：提示「我需要強光。」回應「拿去吧。讓美瑞蒂亞之光照亮這個世界。」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA98E zzzAoMMq04CultB02T05` | `0x4CA98F` | 無 | 無 | 提示：[`"I need a weak light."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3018) 回應：[`"You're humble. That's very Meridian. Go ahead, take it."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3019) |

（翻譯：提示「我需要弱光。」回應「你很謙卑。這非常有美瑞蒂亞的風格。拿去吧。」）

### 信徒分支 04：安全性 / 結果問題

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA991 zzzAoMMq04CultB03T01` | `0x4CA992` | 無 | 無 | 提示：[`"Is it safe to shine this light on people?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3021) 回應：[`"The important thing is to believe, and more importantly, to forgive. Come, let us worship Meridia together."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3022) |

（翻譯：提示「把這道光照在人身上安全嗎？」回應「重要的是信仰，更重要的是寬恕。來吧，讓我們一起崇拜美瑞蒂亞。」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA993 zzzAoMMq04CultB03T02` | `0x4CA994` | 無 | 無 | 提示：[`"That's not an answer for my question"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3024) 回應：[`"If anything goes wrong, it's because he didn't have enough faith. I have no responsibility for that."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3025) |

（翻譯：提示「你這根本沒回答我的問題。」回應「如果出了什麼差錯，那也是因為他的信仰不夠。我對此不負任何責任。」）

### 信徒分支 05：巴洛爾死後的結果

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA996 zzzAoMMq04CultB04T01` | `0x4CA997` | 無 | 無 | 提示：[`"Balor is dead. What's happened?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3027) 回應：[`"He probably didn't believe in Meridia and her brilliance. Therefore, his body was burned to the ground."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3028) 回應：[`"If he had believed in Meridia, none of this would have happened. It was all the fault of the eight divines who seduced mortals."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3029) |

（翻譯：提示「巴洛爾死了。發生了什麼事？」回應 1「他大概不信仰美瑞蒂亞和她的光輝。因此，他的身體被焚燒殆盡了。」回應 2「如果他信仰美瑞蒂亞，這一切就不會發生。這全都是那些引誘凡人的八聖靈的錯。」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA998 zzzAoMMq04CultB04T02` | `0x4CA999` | 無 | 無 | 提示：[`"I'm not a Meridia believer anymore."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3031) 回應：[`"Once you're in, you can't get out. You have to give up. You're already on the list of followers in the Colored Room."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3032) |

（翻譯：提示「我不再是美瑞蒂亞的信徒了。」回應「一旦加入就無法退出。你還是死心吧。你的名字已經在『彩色房間 (Colored Room)』的追隨者名單上了。」）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4CA99A zzzAoMMq04CultB04T03` | `0x4CA99B` | 無 | 無 | 提示：[`"Oh, damn eight divines!"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3034) 回應：[`"Yes, that's the spirit. It will be our way of showing him that we can use this failure to our advantage tomorrow."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3035) |

（翻譯：提示「喔，去他的八聖靈！」回應「沒錯，就是這種氣勢。這將是我們向他證明的方式：我們能在明天把這次失敗轉化為我們的優勢。」）

## 重建筆記

基於原始碼：
- 此任務由 [`0082EA zzzAoMMq04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:393) 代表，核心目標鏈為：調查巴洛爾的瘋狂之眼詛咒（目標 10），然後殺死他（目標 30）。
- 任務在階段 20 / 目標 20 分支：玩家可以向阿爾塔諾回報（斯丹達爾慈悲路徑，預設），或是尋找美瑞蒂亞信徒聯絡人以獲取光魔法的替代方案（目標 21，選用）。
- 美瑞蒂亞信徒分支的主題 (`4CA978-4CA99B`) 來自 dialogue.md 第 2985–3035 行，代表了一個平行的介入敘事：說服玩家使用美瑞蒂亞的光之法杖來「治癒」巴洛爾，而非殺死他。
- 若巴洛爾死亡（無論是由於斯丹達爾的指令，還是美瑞蒂亞光照過度），信徒 NPC 會對他的死發表評論 (`4CA996`)，並可能將玩家鎖定在美瑞蒂亞陣營 (`4CA998`)。
- `infodiag` 未紀錄自訂的 `SCEN` 紀錄，確認此為純對話任務，無預設的過場動畫。

階段進度推論：
- 階段 0–10：初始接受任務 (B1Mission4 主題)。
- 階段 10–20：調查階段 (與巴洛爾進行 B2MadEye, B3HowGet, AboutWoman, GiveMercy 等主題)。
- 階段 20–30：抉擇點 (B4RumorTrueTopic, B4Yes, B4No；可能分支至美瑞蒂亞路徑或維持斯丹達爾路徑)。
- 階段 30–40：殺死巴洛爾 (觸發 B5KillBalorTopic, B5MustDie 條件)。
- 階段 40+：完成任務 (B6Mission4Comp 主題)。
- 階段 50（推論）：完成後狀態或過渡至下一個任務。

翻譯筆記：
- 原始對話中的 `"Stenndarr"` 應為 `Stendarr`（警戒者的神聖守護者）的拼寫錯誤。
- [`009358`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:139) 中的 `"surged"` 或 `"sufferd"` 反映了來源檔案中非母語英語的措辭；在此保留字面意思。
- 美瑞蒂亞信徒的對話修飾程度明顯低於斯丹達爾分支，暗示這可能是一個玩家撰寫或社群貢獻的子系統。
- [`4CA998`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3032) 中的 `"Colored Room"`（彩色房間）是湮滅背景知識的引用（謝格拉的領域）；在這裡可能是美瑞蒂亞信徒名冊的隱喻。

開放驗證：
- 若有更豐富的別名轉儲，請直接檢查 QUST 別名定義（目標 21 的美瑞蒂亞信徒聯絡人 FormID）。
- 若有原始碼/反編譯路徑，請檢查 B4Yes, B4No, B1Mission4, B6Mission4Comp 上的 VMAD 片段；這些可能控制了階段推進與分支路由。
- 確定光之法杖物品（強光/弱光）是實際的遊戲內 MISC/WEAP 紀錄，還是透過 Papyrus 腳本設定的清單旗標。
- 驗證巴洛爾 NPC 紀錄 0x0012D5 與 0x0B161E：確認哪一個是第一章的目標，哪一個（若有的話）與第四章有關。
- 調查信徒對話（目標 21）中提到的「光照過度」在遊戲中是否具有死亡腳本機制，或者純粹是敘事基調。
- 交叉檢查目標 21（「聽取美瑞蒂亞信徒的建議」）的觸發條件：是需要特定的對話選擇，還是受階段限制？
