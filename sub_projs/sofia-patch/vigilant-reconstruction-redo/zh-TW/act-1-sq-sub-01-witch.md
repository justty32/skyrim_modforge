# 第一章 支線子任務 01 - 伊瓦斯泰德的女巫 (Witch of Ivarstead)

狀態：第一個重製片段。基於原始碼，連結優先，無 Gemini 幻覺。

來源策略：
- 原始對話行連結回提取的來源檔案，而非完整複製。
- 僅在需要解釋背景或條件時才顯示短小的原始碼片段。
- 四個場景主題紀錄保留為對話分支（受階段限制的獨白）。

## 任務紀錄 (Quest Record)

[`17576E zzzAoMSubQ01 "Witch of Ivarstead"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:169)

CLI：
- `questdiag Vigilant.esm 0x17576E`
- `infodiag Vigilant.esm 0x17576E`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務資料：
- FormID: `Vigilant.esm:0x17576E`
- EditorID: `zzzAoMSubQ01`
- 名稱 (Name): `Witch of Ivarstead`
- 旗標 (Flags): `RunOnce`
- 優先度 (Priority): `60`
- 類型 (Type): `Misc`
- 過濾器 (Filter): `AoM\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 旗標 | 日誌 |
|---:|---|---|
| 0 | 無 | 空白 |
| 5 | 無 | 空白 |
| 10 | 無 | 空白 |
| 20 | 無 | 空白 |
| 22 | 無 | 空白 |
| 24 | 無 | 空白 |
| 26 | 無 | 空白 |
| 28 | 無 | 空白 |
| 30 | 無 | 空白 |
| 40 | 無 | 空白 |
| 50 | CompleteQuest | 空白 (×2) |
| 200 | 無 | 空白 |
| 210 | 無 | 空白 |
| 220 | 無 | 空白 |
| 230 | CompleteQuest | 空白 (×2) |
| 300 | CompleteQuest | 空白 |

目標 (Objectives)：
- `questdiag` 未紀錄任務目標。

推論：
- 階段 5, 20, 22–28 似乎是微調的進度狀態，沒有在日誌中顯示目標（可能僅用於內部對話/腳本邏輯）。
- 階段 50, 230, 300 處有多個 `CompleteQuest` 旗標，暗示了分支結局：階段 50 為標準勝利，階段 230 可能為慈悲結局，階段 300 則為另一種解決方式。

## 別名 / 編排主幹 (Alias / Staging Backbone)

`infodiag` 未偵測到獨立的自訂 `SCEN` 紀錄。對話是透過別名引用 `#3`（雷達）直接進行。

主任務：
- `17576E zzzAoMSubQ01` "Witch of Ivarstead"

來自 `infodiag` 條件的對話別名：
- 別名 `#3`：預期為 [`16685A zzzAoMBossReyda "Reyda"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1164)。

（推論：別名角色是從對話條件 `GetIsAliasRef` 索引 3 推斷而來；CLI 未提供明確的別名轉儲）

## 自訂對話分支

### 分支 1：開場診斷 — 斯丹達爾的詛咒

主題 (TOPIC) `0x177DED zzzAoMsq01WitchB01T01`

條件模式：
- `GetStage == 10`：玩家首次遇到雷達時觸發。
- `GetIsAliasRef alias #3`（雷達）。
- 根據相關任務（0x011B75，推測為第一章任務鏈中的另一個任務）的 `GetQuestCompleted` 狀態進行分支。

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177DED` | `0x177DEE` | `Goodbye`, `SayOnce` | `GetStage == 10`; 任務 `011B75` 的 `GetQuestCompleted == 0`; `GetIsAliasRef alias #3` | [「Stendarr become old ......His eyes is weaked, his mental is in insane now. That's because you are cursed....」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2037) |
| `0x177DED` | `0x177DEF` | `Goodbye`, `SayOnce` | `GetStage == 10`; 任務 `011B75` 的 `GetQuestCompleted == 1`; `GetIsAliasRef alias #3` | [「Well well well, you have solved the curse? Old Fool become quite kind as he once was」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2038) |

推論：
- 女巫根據相關任務狀態向玩家發出診斷。若玩家已完成任務 0x011B75（尚未識別），女巫會承認該問題已解決，並稱斯丹達爾為「老糊塗」。
- 問候語暗示雷達能看出玩家受到了詛咒，並將其與斯丹達爾的折磨聯繫起來。
（翻譯：0x177DEE「斯丹達爾老了……他的視力衰退，精神也陷入瘋狂。那是因為你受到了詛咒……」0x177DEF「喔呀喔呀，你解開詛咒了嗎？那個老糊塗變得像以前那樣仁慈了呢。」）

### 分支 2：女巫自我介紹 — 「妳是誰？」

主題 (TOPIC) `0x177DF1 zzzAoMsq01WitchB02T01` 提示="Who are you?"

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177DF1` | `0x177DF2` | 無 | `GetStage == 10`; `GetIsAliasRef alias #3` | Responses: [`"Me? I am  Reyda. Witch of Glenmoril"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2041) / [`"Ivalstead is my territory. All of people and beasts around here is mine"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2042) |

（翻譯：「我？我是雷達。格林摩利的女巫。」「伊瓦斯泰德是我的領地。這裡所有的人和野獸都是我的。」）

### 分支 3：詛咒分析 — 「為什麼我的身體這麼沉重？」

主題 (TOPIC) `0x177DF4 zzzAoMsq01WitchB03T01` 提示="Why is my body so heavy ...... you did something?"

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177DF4` | `0x177DF5` | 無 | `GetStage == 10`; 任務 `011B75` 的 `GetQuestCompleted == 0`; `GetIsAliasRef alias #3` | Responses: [`"I do not anything. I just look, just lookin from the beginning"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2045) / [`"You was a really terrible. You killed child's life not only innocent person."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2046) / [`"needless to say,you are cursed. so much worse If you serve the God of Justice. You are alredy over"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2047) |

翻譯筆記：
- 原始文本「You was a really terrible」語法不通；意指玩家過去的行為（殺害無辜者/孩子）是詛咒的來源。
（翻譯：「我什麼都沒做。我只是在看，從頭到尾都在看。」「你真是個可怕的人。你不僅殺了無辜的人，還奪走了孩子的性命。」「不用說，你受到了詛咒。如果你侍奉的是正義之神，那情況會更糟。你已經玩完了。」）

### 分支 4：詛咒解決方案 — 「我該如何解除這個詛咒？」

主題 (TOPIC) `0x177DF6 zzzAoMSqQ01WitchB03T02` 提示="How can I solve this curse?"

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177DF6` | `0x177DF7` | 無 | `GetStage == 10`; 任務 `011B75` 的 `GetQuestCompleted == 0`; `GetIsAliasRef alias #3` | [`"You listen to me? you are useless. I said you are over, You are over."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2050) |

（翻譯：「你有在聽我說話嗎？你沒救了。我說過你玩完了，你徹底玩完了。」）

### 分支 5：家族背景故事 — 「為什麼那家人待在這裡？」

主題 (TOPIC) `0x177DF9 zzzAoMSQ01WitchB04T01` 提示="Why was family staying here?"

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177DF9` | `0x177DFA` | 無 | `GetStage == 10`; 任務 `011B75` 的 `GetQuestCompleted == 0`; `GetIsAliasRef alias #3` | Responses: [`"They are in troubled by cursed sword. How poor thing? So, Kind Witch decided to help them they solve the curse"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2053) / [`"Now The curse is gone, So they heve to went out here. But You did clean up here luckily. It was save time thanks to you."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2054) |

推論：
- 雷達提到有一家人受到了詛咒之劍的困擾，而她幫助了他們。這將雷達塑造為一個複雜的角色：表面上在助人，但道德立場模糊。
（翻譯：「他們受到了詛咒之劍的困擾。多麼可憐啊？所以，仁慈的女巫決定幫助他們解除詛咒。」「現在詛咒解除了，所以他們必須離開這裡。不過幸運的是你把這裡清理乾淨了。多虧了你，節省了不少時間。」）

### 分支 6：關於主人的問題 — 「妳的主人是誰？」

主題 (TOPIC) `0x177DFC zzzAoMSQ01WitchB05T01` 提示="Who is your master?"

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177DFC` | `0x177DFD` | 無 | `GetStage == 10`; 任務 `011B75` 的 `GetQuestCompleted == 0`; `GetIsAliasRef alias #3` | [`"Come on, somebody? Witch open the crotch anyone if they have power. Nfufufu"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2057) |

翻譯筆記：
- 用詞粗俗且語法混亂；可能暗示雷達毫無道德節操，會侍奉任何擁有力量的主人。
（翻譯：「喔呀，是誰呢？只要有力量，女巫願意對任何人敞開雙腿。呵呵呵。」）

### 分支 7：道德指控 — 「妳知道一切。關於我，關於那家人……」

主題 (TOPIC) `0x177DFF zzzAoMSQ01WitchB06T01` 提示="You know everthing. About me, About the family..."

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177DFF` | `0x177E00` | 無 | `GetStage == 10`; 任務 `011B75` 的 `GetQuestCompleted == 1`; `GetIsAliasRef alias #3` | Responses: [`"Oh, yes. So shat? So you say I am evil? Murderer is you. Not me, You are Murderer"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2060) / [`"I am just looking you as promised, and make fog thicken. Well, but it looks like there was no need for Old Fool"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2061) |

翻譯筆記：
- 「So shat」是拼字錯誤；應為「So what」。
- 「looking you as promised」暗示了先前的協議或契約。
（翻譯：「喔，沒錯。那又怎樣？所以你想說我是邪惡的？兇手是你。不是我，你才是兇手。」「我只是按照約定看著你，並讓霧氣變濃。好吧，但看來對那個老糊塗來說已經沒必要了。」）

### 分支 8：與魔族的關聯 — 「妳的主人是莫拉格·巴爾？」

主題 (TOPIC) `0x177E02 zzzAoMSQ01WitchB07T01` 提示="Your master is Molag Bal?"

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177E02` | `0x177E03` | 無 | `GetStage == 10`; 任務 `011B75` 的 `GetQuestCompleted == 1`; `GetIsAliasRef alias #3` | [`"Now, what was that? I do dance with anybody. Sexy woman like me is so hard, Nfufufufu"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2064) |

（翻譯：「哎呀，那是什麼意思呢？我願意跟任何人跳舞。像我這樣性感的女人是很辛苦的，呵呵呵呵呵。」）

### 分支 9：戰鬥對峙 — 「女巫必須死」

主題 (TOPIC) `0x177E05 zzzAoMSQ01WitchB08T01` 提示="Witch must die"

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177E05` | `0x177E06` | 無 | `GetStage == 10`; 任務 `011B75` 的 `GetQuestCompleted == 1`; `GetIsAliasRef alias #3` | Responses: [`"You want to kill more? After Killing women and child, your fellows. You want to kill to shabby old woman the next?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2067) / [`"Well, good. Try baby. You will be die while lamented your own powerlessness"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2068) |

VMAD 片段：
- `AoMSq01_TIF__02177E06`（觸發 `OnEnd` 片段）
- （推論：片段可能在敵對狀態下觸發戰鬥或推進任務階段）
（翻譯：「你還想殺更多人？在殺了婦女、孩子還有你的同伴之後。接下來你想殺一個落魄的老太婆嗎？」「好啊，試試看吧寶貝。你會在感嘆自己的無力中死去。」）

### 分支 10：投降 / 絕望路徑 — 「喔，天哪。拜託。」

主題 (TOPIC) `0x177E09 zzzAoMSQ01Witch2B01T01`

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177E09` | `0x177E0A` | 無 | `GetStage == 30`; `GetIsAliasRef alias #3` | Responses: [`"Oh, My God. Come on. Please, help me, I will do anything"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2071) / [`"I was deceived in Molag Bal. I did not think to become a thing. So,please"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2072) |

推論：
- 雷達的語氣在階段 30 轉為絕望，暗示：(a) 戰鬥受損/失敗，或 (b) 遭遇中途發生了預設的狀態變化。
- 雷達明確提到被莫拉格·巴爾欺騙，捲入了魔族的強迫。
（翻譯：「喔，天哪。拜託。求求你，救救我，我什麼都願意做。」「我被莫拉格·巴爾騙了。我沒想到會變成這樣。所以，拜託了。」）

### 分支 11：關於腐化靈魂的問題 — 「什麼是腐化靈魂？」

主題 (TOPIC) `0x177E0C zzzAoMSQ01Witch2B02T01` 提示="What is Corrupted Soul?"

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177E0C` | `0x177E0D` | `SayOnce` | `GetStage == 30`; `GetIsAliasRef alias #3` | Responses: [`"Black soul found the gates of Oblivion. Gates will swallow you from the inner sooner or later"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2075) / [`"You are aleady trapped in Oblivion. No one can not get away, You are over"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2076) |

（翻譯：「黑暗靈魂發現了通往湮滅的大門。大門遲早會從內部將你吞噬。」「你已經被困在湮滅中了。沒人能逃得掉，你完蛋了。」）

### 分支 12：關於石頭的知識 — 「那顆石頭是什麼？」

主題 (TOPIC) `0x177E0F zzzAoMSQ01Witch2B03T01` 提示="What is the Stone?"

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177E0F` | `0x177E10` | `SayOnce` | `GetStage == 30`; `GetIsAliasRef alias #3` | Responses: [`"Your fellow teach you nothing. How poor you are, I can not stop laughing you"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2079) / [`"Molag Bal. Don't you know the demon committed the bitch of Nede? To beast from people, the oldest of the stragglers"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2080) |

翻譯筆記：
- 「committed the bitch of Nede」語意不明；可能指涉及莫拉格·巴爾與阿萊西亞（內德人祖先）的歷史暴行。可能是對專有名詞引用的誤譯。
（翻譯：「你的同伴什麼都沒教你。你真可憐，我簡直要笑死你了。」「莫拉格·巴爾。你難道不知道那個惡魔對內德人的蕩婦做了什麼嗎？從人變成野獸，那是落伍者中最古老的一個。」）

### 分支 13：拒絕慈悲 — 「不，女巫必須死」

主題 (TOPIC) `0x177E12 zzzAoMSQ01Witch2B04T01` 提示="No,Witch must die"

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177E12` | `0x177E13` | `Goodbye`, `SayOnce` | `GetStage == 30`; `GetIsAliasRef alias #3` | [`"Don't you have any mercy? You fucking bastard!! I wrench your head."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2083) |

VMAD 片段：
- `AoMSq01_TIF__02177E13`（觸發 `OnEnd` 片段）
- （推論：片段觸發死亡或最終戰鬥狀態）
（翻譯：「你難道一點慈悲都沒有嗎？你這個該死的混蛋！！我要擰下你的腦袋。」）

### 分支 14：接受慈悲 — 「滾吧。永遠別再回來」

主題 (TOPIC) `0x177E15 zzzAoMSQ01Witch2B05T01` 提示="Get lost. never come back here"

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177E15` | `0x177E16` | `Goodbye` | `GetStage == 30`; `GetIsAliasRef alias #3` | [`"Oh, thank you. you are so friendly. I promise to live humbly in deep forest"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2086) |

VMAD 片段：
- `AoMSq01_TIF__02177E16`（觸發 `OnEnd` 片段）
- （推論：片段標誌著透過慈悲路徑完成任務）
（翻譯：「喔，謝謝你。你真友善。我發誓會躲在森林深處安分守己地生活。」）

### 分支 15：死亡對話 — 雷達的遺言

主題 (TOPIC) `0x177E17 zzzAoMSQ01WitchDeath` [Combat/Death]

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x177E17` | `0x177E18` | 無 | `GetIsID == 1` (NPC `16685A:Vigilant.esm` = 雷達) | [`"You are monster...Laza will eat you..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2089) |

（說話者：16685A 雷達）

推論：
- 「拉札 (Laza)」的身分不明；可能指某種生物、詛咒，或是與莫拉格·巴爾復仇相關的魔族實體。
（翻譯：「你是個怪物……拉札會吞噬你的……」）

## 場景獨白（後期 / 沉思）

附屬於此任務的四個場景主題紀錄提供了類獨白的回應。這些似乎是在階段 30（絕望階段）期間透過場景對話條件觸發的。

### 場景 1：冰冷的眼神

主題 (TOPIC) `0x179185` [Scene/Scene]

| FormID | INFO | 條件 | 回應 |
|---|---|---|---|
| `0x179185` | `0x179186` | (無) | [`"Your Eyes are so Cold, But hatred is burning under the thick ice"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2092) / [`"It is the same with the Old Fool. Oh, it let me hot. I want to put your eyes to decorate the shelves."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2093) |

（翻譯：「你的眼神如此冰冷，但厚冰之下卻燃燒著仇恨。」「這點和那個老糊塗一模一樣。喔，這讓我興奮起來了。真想挖下你的眼睛裝飾在架子上。」）

### 場景 2：假設性的解咒

主題 (TOPIC) `0x179188` [Scene/Scene]

| FormID | INFO | 條件 | 回應 |
|---|---|---|---|
| `0x179188` | `0x179189` | (無) | [`"If you did not come here, that family never die. Their curse will be solved. They have been living happily in his hometown of High Rock ......"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2096) / [`"So Poor, because of all you. If you did nothing, nothing happens."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2097) |

推論：
- 雷達將那家人的厄運歸咎於玩家的到來，暗示她原本的角色是良性或中立的。
（翻譯：「如果你沒來這裡，那家人就不會死。他們的詛咒原本會被解開。他們原本會在高岩省的家鄉幸福地生活……」「真可憐，全都是因為你。如果你什麼都沒做，就不會發生這種事。」）

### 場景 3：對正義的懷疑

主題 (TOPIC) `0x17918B` [Scene/Scene]

| FormID | INFO | 條件 | 回應 |
|---|---|---|---|
| `0x17918B` | `0x17918C` | (無) | [`"You believe the old fool yet? Although There have not exit true justice in  in this world?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2100) / [`"If it existed. Why is innocent people suffered, sinful people batten?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2101) |

推論：
- 雷達從哲學層面挑戰斯丹達爾的正義觀，暗示了道德相對主義或虛無主義。
（翻譯：「你還相信那個老糊塗嗎？即便這個世界上根本不存在真正的正義？」「如果正義真的存在，為什麼無辜的人受苦，而罪孽深重的人卻過得有滋有味？」）

### 場景 4：觸碰過石頭

主題 (TOPIC) `0x17918E` [Scene/Scene]

| FormID | INFO | 條件 | 回應 |
|---|---|---|---|
| `0x17918E` | `0x17918F` | (無) | [`"The identity of the flame burning in your eyes. You've touched the stone. That's why your are stubborn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2104) / [`"Smell of Corrupted Soul...... You are not already human, You are monster"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2105) |

推論：
- 此處明確引用了「石頭」（來自馬魯克路徑的魔族神器），並暗示玩家已受其污染。
（翻譯：「你眼中燃燒著的火焰的真面目。你觸碰過那顆石頭。那就是為什麼你如此固執的原因。」「腐化靈魂的味道……你已經不再是人類了，你是怪物。」）

## 替代結局階段 (階段 210+)

在最初的對峙後，若玩家倖存或返回，將解鎖階段 210 的對話：

### 分支 16：重燃惡意 — 「你真蠢」

主題 (TOPIC) `0x17B7F6 zzzAoMSQ01Witch3B01T01`

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x17B7F6` | `0x17B7F7` | `WalkAway` | `GetStage == 210`; `GetIsAliasRef alias #3` | Responses: [`"You are so stupid. You are like Old fool. It's just like you to that decrepit until the tail club"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2108) / [`"You're not a record of death way. Looks fell to die dripping field in the wilderness, Ahahahahaha"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2109) |

翻譯筆記：
- 「decrepit until the tail club」語意不明；可能是對衰敗或退化的扭曲引用。
（翻譯：「你真蠢。你就像那個老糊塗一樣。死到臨頭都還這麼落魄，真是適合你。」「你根本沒被記在死神的帳本上。你看起來會慘死在荒野的滴血之地，啊哈哈哈哈哈哈。」）

### 分支 17：最終威脅 — 「妳想幹什麼？」

主題 (TOPIC) `0x17B7F8 zzzAoMSQ01Witch3B01T02` 提示="What are you trying to do?"

| FormID | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x17B7F8` | `0x17B7F9` | `Goodbye`, `SayOnce` | `GetStage == 210`; `GetIsAliasRef alias #3` | Responses: [`"I can not kill you. So I wreak my anger by killin ivasterd's people"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2112) / [`"his is Humble life of the witch. It is to get all I see into honey bucket "`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2113) |

VMAD 片段：
- `AoMSq01_TIF__0217B7F9`（觸發 `OnEnd` 片段）
- （推論：片段可能標誌著一個復仇循環或最終的完成狀態）

翻譯筆記：
- 「killin ivasterd's people」 = 「殺掉伊瓦斯泰德的人們」
- 「honey bucket」可能是一個關於降格或奴役的粗俗隱喻（糞桶）。
（翻譯：「我殺不了你。所以我會殺光伊瓦斯泰德的人來洩憤。」「這就是女巫卑微的生活。就是把我看到的一切都塞進糞桶裡。」）

## 相關紀錄

NPCs：
- [`16685A zzzAoMBossReyda "Reyda"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1164)（女巫；透過別名 #3 與任務綁定）
- [`0DC68D zzzCHEnchanter "Hilda the witch"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:829)（相關 NPC；第四章記憶鏈中的對話提到了她對雷達的了解）

相關任務：
- `011B75:Vigilant.esm`（被引用的任務；其完成狀態限制了多個雷達的開場白；身分待定）
- 可能透過父任務阿爾塔諾/斯丹達爾劇情線與第一章早期的任務鏈相連。

## 重建筆記

基於原始碼：
- 此任務是與雷達的分階段分支遭遇戰。雷達是一名格林摩利女巫，與莫拉格·巴爾糾纏不清，並在伊瓦斯泰德詛咒/解咒 NPC。
- 任務有三種結果路徑：
  1. **戰鬥死亡**（若玩家選擇戰鬥，階段 10→50）：雷達被殺；階段 50 `CompleteQuest`。
  2. **慈悲 / 放逐**（階段 10→30，然後接受慈悲）：雷達獲饒；階段 230 `CompleteQuest`（從階段旗標推論）。
  3. **復仇循環**（階段 10→30，然後拒絕慈悲）：雷達透過伊瓦斯泰德進行報復；階段 210+300 `CompleteQuest`（最終宿怨）。
- 任務涉及一家受魔族之劍詛咒的人、雷達模糊不清的營救嘗試，以及關於「石頭」（魔族神器）、「腐化靈魂」和莫拉格·巴爾角色的形而上學解說。
- 階段 30 的場景獨白提供了哲學性的評論，並確認了玩家受到了魔族石頭的污染。

階段轉換：
- 階段 0：初始
- 階段 5–28：不明確（無對話限制；可能是內部腳本進度）
- 階段 10：玩家與雷達對話（開場白變體取決於任務 011B75 的完成情況）
- 階段 20–28：推測為調查或準備
- 階段 30：絕望/慈悲決擇點（取決於玩家選擇的分支）
- 階段 50：若玩家選擇戰鬥，任務完成
- 階段 200–230：替代結果階段
- 階段 300：最終完成狀態

魔侯 / 背景關聯：
- 雷達受莫拉格·巴爾脅迫；這與第四章記憶任務以及更廣泛的《警戒者》魔族陷阱敘事一致。
- 對「石頭」與「腐化靈魂」的引用與馬魯克相關動機（馬魯克之眼、污染神學）掛鉤。
- 家族詛咒與涉及格林摩利女巫團的更廣泛伊瓦斯泰德支線有關。

開放驗證：
- 檢查腳本 `AoMSq01_TIF__02177E06`、`AoMSq01_TIF__02177E13`、`AoMSq01_TIF__02177E16`、`AoMSq01_TIF__0217B7F9` 以確定精確的階段推進、結果限制與慈悲路徑邏輯；
- 直接檢查 QUST 別名（透過更豐富的別名轉儲）以確認別名 #3 = 雷達；
- 檢查任務 `011B75` 以確認其身分以及在雷達問候語邏輯中的角色；
- 檢查 NPC 雷達 (`16685A`) 的戰鬥旗標、行為封包以及魔族腐化標記；
- 檢查任務腳本/階段日誌項目中的階段 5–28 進度（若有的話），因為目前這些部分是不透明的；
- 交叉引用女巫希爾達 (`0DC68D`) 的對話以確認背景故事（她出現在第四章記憶中，可能提供背景資訊）；
- 驗證死亡對話中引用的「拉札 (Laza)」是否為已知實體。
