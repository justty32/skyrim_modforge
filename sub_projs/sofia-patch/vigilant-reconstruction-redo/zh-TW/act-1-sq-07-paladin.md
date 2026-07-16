# 第一章 支線任務 07 - 老聖騎士 (Old Paladin)

狀態：第一個重製片段。基於原始碼，連結優先，無 Gemini 幻覺。

來源策略：
- 原始對話行連結回提取的來源檔案，而非完整複製。
- 僅在需要解釋背景或翻譯問題時才顯示短小的原始碼片段。
- 場景主題提取自 dialogue.md；場景階段/動作來自 `scenediag` CLI。

## 任務紀錄 (Quest Record)

[`00A3FE zzzAoMMq07 "Old Paladin"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:243)

CLI：
- `questdiag Vigilant.esm 0x00A3FE`
- `infodiag Vigilant.esm 0x00A3FE`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務資料：
- FormID: `Vigilant.esm:0x00A3FE`
- EditorID: `zzzAoMMq07`
- 名稱 (Name): `Old Paladin`
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
| 33 | 無 | 空白 |
| 35 | 無 | 空白 |
| 36 | 無 | 空白 |
| 37 | 無 | 空白 |
| 38 | 無 | 空白 |
| 40 | 無 | 空白 |
| 50 | 無 | 空白 |
| 60 | 無 | 空白 |
| 70 | 無 | 空白 |
| 75 | 無 | 空白 |
| 80 | CompleteQuest | 空白 |
| 255 | ShutDownStage | 空白 |
| 9999 | CompleteQuest | 空白 |

目標 (Objectives)：

| 索引 | 來源 | 文本 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:244) | 與雅各對話 (Talk to Jacob) |
| 10 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:245) | 擊敗黑檀騎士 (Defeat Ebony knight) |
| 33 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:246) | 支援雅各 (Support Jacob) |
| 40 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:247) | 擊敗巴爾 (Defeat Bal) |
| 60 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:248) | 與阿爾塔諾對話 (Talk to Altano) |
| 70 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:249) | 將莫拉格·巴爾的釘頭錘交給阿爾塔諾 (Take Mace of Molag Bal to Altano) |

目標對象 (Objective targets)：
- 目標 0：1 個對象，0 個條件。
- 目標 10：1 個對象，0 個條件。
- 目標 33：1 個對象，0 個條件。
- 目標 40：1 個對象，0 個條件。
- 目標 60：1 個對象，0 個條件。
- 目標 70：2 個對象，0 個條件。
- 目前的 CLI 輸出未印出目標儲存格/引用細節；若位置標記很重要，則需要更深入的 QUST 目標轉儲。

## 別名 / 編排主幹 (Alias / Staging Backbone)

主任務：
- `00A3FE zzzAoMMq07` "Old Paladin"

來自 `infodiag` 的對話別名：
- 別名 `#0`：預期為 `Altano`（結案對話夥伴）。
- 別名 `#1`：預期為 `Jacob`（任務主持人；階段 20–60）。
- 別名 `#3`：預期為 `Umbra`（黑檀騎士；階段 0–10 對峙）。

（推論：別名索引 0, 1, 3 是根據對話中的 `GetIsAliasRef` 條件推斷而來；CLI 未提供明確的別名轉儲）

場景編排：
- 偵測到多個場景主題 (TOPIC cat=Scene)；目前的 CLI 套件未提供正式的 `SCEN` 紀錄編排。
- 場景似乎主導了獨白序列與遭遇插曲（例如：莫拉格·巴爾、瑞海兒、約書亞、奧斯、燃雨）。

## 場景主題 (Scene Topics)

場景主題是對話錨點，而非正式的 SCEN 紀錄。按主題 FormID 與編排提示列出：

### 0x00E4E5 (莫拉格·巴爾 / 雅各 對峙獨白)

提取的文本（6 行）：
- [`Go away!! Molag Bal!! I am not discouraged!!`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:276)
- [`You right!! 20 years ago,I lost to you. But this time, I overcome you!!`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:277)
- [`Exactly! I killed you!! Exactly!! I killed innocent under the name of Stendarr!!`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:278)
- [`Don't Look at me. Please.....`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:279)
- [`Shut up! Muderer!! I am diffrent from you! I am not Beast like you!`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:280)
- [`Not me! I have no responsibility to your death!! Please, go away....`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:281)
- [`Joshua!Is that you? How are you!? Where were you going to?`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:282)
- [`Thank you, Joshua...Your word is merciful...but, I can not stop my steps.`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:283)
- [`My master...Never did I think of you are here....Yes...I uderstand it.`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:284)

推論背景：雅各對莫拉格·巴爾顯聖的內心獨白；引用了失去與救贖的敘事。
（翻譯：「走開！！莫拉格·巴爾！！我沒有氣餒！！」「你說得對！！20 年前我輸給你了。但這一次，我會戰勝你！！」「沒錯！我殺了你！！沒錯！！我以斯丹達爾之名殺了無辜的人！！」「別看我。拜託……」「閉嘴！殺人犯！！我跟你們不一樣！我不是像你們一樣的野獸！」「不是我！我對你的死沒有責任！！拜託，走開……」「約書亞！是你嗎？你還好嗎！？你要去哪裡？」「謝謝你，約書亞……你的話語充滿慈悲……但我不能停下腳步。」「我的主人……我從沒想過您會在這裡……是的……我明白了。」）

### 0x00E4F4 (瑞海兒問候)

提取的文本（1 行）：
- [`Well...I gave you my precious mercy.But, Why come here? Jacob?`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:287)

背景：[`瑞海兒 (Rahel)` (來自 `00E4FE zzzAoMM07GhostBal` 的別名)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1) 向雅各問候。暗示了靈魂/幽靈機制或記憶編排。

### 0x00E4F6 (雅各的決心)

提取的文本（1 行）：
- [`I ... come here to purege my contempt...No!...Rahel, to help you!!`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:290)

背景：雅各的動機被澄清為幫助瑞海兒（推測為莫拉格·巴爾的受害者/化身）。

### 0x00E4F8 (末日警告)

提取的文本（1 行）：
- [`so...but too late. Molag Bal is Coming...the all end....Red fog envelope everything....`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:293)

背景：高風險場景標記；莫拉格·巴爾迫在眉睫的威脅。

### 0x00E4FA (瑞海兒的呼喚)

提取的文本（1 行）：
- [`Rahel?`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:296)

背景：雅各正在尋找瑞海兒；簡短的詢問。

### 0x00E4FC (巴爾的命令)

提取的文本（1 行）：
- [`Stop talking anymore. Do not Disturb me. Orthe! Ranyu! Kill them All!!`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:299)

背景：莫拉格·巴爾的化身命令魔人盟友 [`奧斯 (Orthe)`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1) 與 [`燃雨 (Ranyu)`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1) 發動攻擊。

### 0x00EA65 (夢境序列：瑞海兒的迴聲)

提取的文本（1 行）：
- [`Rahel!? Rahel? Is that you?`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:302)

背景：可能是戰後或夢境中的遭遇。名稱與階段進度相符（約在階段 50）。

### 0x00EA67 (夢境序列：雅各的疑問)

提取的文本（1 行）：
- [`What happended? Jacob? Why do you raise your voice? You had a nightmare?`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:305)

背景：瑞海兒詢問雅各為何痛苦；暗示夢境/記憶的分離。

### 0x00EA69 (夢境序列：雅各的和解)

提取的文本（1 行）：
- [`Yes...But I have waked from the nightmare I lost you. I will never send away you...`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:308)

背景：雅各從噩夢中醒來；與瑞海兒的靈魂達成和解。

### 0x00EA6B (夢境序列：瑞海兒的慰藉)

提取的文本（1 行）：
- [`Jacob...I am always with you. Do not worry anymore....`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:311)

背景：瑞海兒的保證；情感上的救贖。

### 0x00EA6D (夢境序列：瑞海兒的告別)

提取的文本（1 行）：
- [`Rahel ...`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:314)

背景：雅各對瑞海兒最後的呼喚；可能是階段轉換或任務結束的提示。

## 自訂對話分支：昂布拉 (Umbra, 黑檀騎士遭遇)

分支：
- `00EA70:Vigilant.esm`（隱含；階段 0–10 分支，別名 #3）

條件模式：
- 階段限制在 `GetStage < 10`；`GetStage == 10`；別名 #3 (昂布拉) 條件。
- 代表與攻擊燈塔的黑檀騎士進行的對峙與交涉。

### 0x00EA71 zzAoMMq07B1UmbraGreet (昂布拉問候)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EA71 zzAoMMq07B1UmbraGreet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:316) | `0x00EA72` | 無 | `GetStage < 10`; `GetIsAliasRef alias #3` | [`Stop....close enough...`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:317) |

VMAD 片段：
- `AoM07_TIF__0100EA72`（觸發 `OnEnd` 片段；可能將階段推進至 10）
（翻譯：「站住……夠近了……」）

### 0x00EA73 zzAoMMq07B1NonStop (昂布拉警告)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EA73 zzAoMMq07B1NonStop`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:319) | `0x00EA74` | `SayOnce` | `GetIsAliasRef alias #3` | 提示：[`"if we don't stop our steps....what wilt you do?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:319) 回應：[`"I must cut you down....like your colleague...if you don't want to die, go back..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:320) / [`"Be gone....! you also have...who hope your return...."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:321) |

翻譯筆記：
- 提示翻譯：「如果我們不停下腳步……你會怎麼做？」回應 1 翻譯：「我必須斬了你……就像你的同事一樣……如果你不想死，就回去吧……」回應 2 翻譯：「走開……！你也有……希望你回去的人吧……」
- `colleague (同事)` 指的是之前攻擊燈塔但被昂布拉殺死的警戒者。

### 0x00EA75 zzAoMMq07B1AssaultReason

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EA75 zzAoMMq07B1AssaultReason`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:323) | `0x00EA76` | `SayOnce` | `GetIsAliasRef alias #3` | 提示：[`"Why did you attacked Beacon?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:323) 回應：[`"It is My business. You can not accetpt it?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:324) |

（翻譯：提示「你為什麼要攻擊燈塔？」回應「那是我的私事。你沒法接受嗎？」）

### 0x00EA77 zzAoMMq07B1AboutPursuits

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EA77 zzAoMMq07B1AboutPursuits`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:326) | `0x00EA78` | `SayOnce` | `GetIsAliasRef alias #3` | 提示：[`"How did you get clear away from chasers?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:326) 回應：[`"Chaser....? I killed them all. Probably, they are now in stomach of Trolls."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:327) |

翻譯筆記：
- 提示「你是如何甩掉追擊者的？」回應「追擊者……？我全殺了。可能他們現在都在食人魔的肚子裡了。」
- 昂布拉的殘忍自誇；暗示了突襲中造成的大量 NPC 傷亡。

## 自訂對話分支：雅各 (Jacob, 調查階段)

分支：
- `00EA79:Vigilant.esm`（隱含；階段 20–30 分支，別名 #1）

條件模式：
- 最初對話階段限制在 `GetStage == 20`；深入了解背景問題為 `GetStage >= 30 && < 35`。
- 代表雅各對襲擊的回憶以及他的情感/精神背景。

### 0x00EA7A zzAoMMq07B2JacobTalk (雅各開場)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EA7A zzAoMMq07B2JacobTalk`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:329) | `0x00EA7B` | 無 | `GetStage == 20`; `GetIsAliasRef alias #1` | [`Uuu...`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:330) |

背景：含糊的回應；雅各受到創傷或虛弱。

### 0x00EA7C zzAoMMq07B2Whathappen (雅各的陳述)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EA7C zzAoMMq07B2Whathappen`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:332) | `0x00EA7D` | 無 | `GetIsAliasRef alias #1` | 提示：[`"What was happening?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:332) 回應 (悲傷)：[`"Attacked by the summoner....All is dead except me. Again...again I only survived...."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:333) / 回應 (憤怒)：[`"She is called Bal by Daedra...abominable name. She is a agent of Molag Bal...Her purpose is a altar under the ground.."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:334) |

背景：雅各透露燈塔襲擊是由「巴爾」(莫拉格·巴爾的僕從) 所策劃，目標是地下祭壇。
（提示翻譯：「發生了什麼事？」回應 1 翻譯：「受召喚師攻擊……除了我，所有人都死了。又一次……又一次只有我活了下來……」回應 2 翻譯：「魔族稱她為巴爾……可惡的名字。她是莫拉格·巴爾的代理人……她的目標是地下的祭壇……」）

### 0x00EA7E zzAoMMq07B2Meaning (雅各澄清)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EA7E zzAoMMq07B2Meaning`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:336) | `0x00EA7F` | `Goodbye` | `GetIsAliasRef alias #1` | 提示：[`"What do you mean?Jacob?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:336) 回應：[`"There is a Altar of Molag Bal under the beacon. She is attepmting to something tremendous....we must stop her!!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:337) |

VMAD 片段：
- `AoM07_TIF__0100EA7F`（觸發 `OnEnd` 片段；將任務推進至階段 30+）

翻譯筆記：
- 提示翻譯：「你是什麼意思？雅各？」回應翻譯：「燈塔下方有個莫拉格·巴爾的祭壇。她正企圖做出某些驚天動地的事……我們必須阻止她！！」
- 「tremendous」在這裡可能指災難性的或改變世界的儀式。

### 0x00EA81 zzAoMMq07B3MolagBal (背景知識：莫拉格·巴爾的腐化)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EA81 zzAoMMq07B3MolagBal`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:339) | `0x00EA82` | 無 | `GetStage >= 30 && < 35`; `GetIsAliasRef alias #1` | 提示：[`"What is Molag Bal?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:339) 回應 (悲傷)：[`"Daedra price of domination. Many vigilants are corrupted by Molagb Bal."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:340) / 回應 (悲傷)：[`"I also one of them. I lost to Molag bal. When I was wounded and dying, Molag bal apeerared and offer to reanimate me in exchange for my wife."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:341) / 回應 (恐懼)：[`"I have accepted it...I regret that my did. I can not forget her mournful eyes...."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:342) |

解說：雅各悲慘的過去——20 年前他將妻子（瑞海兒）賣給莫拉格·巴爾以換取復活。愧疚感驅動了目前的任務敘事。
（提示翻譯：「莫拉格·巴爾是什麼？」回應 1 翻譯：「支配之魔侯。許多警戒者都受莫拉格·巴爾腐化。」回應 2 翻譯：「我也是其中之一。我輸給了莫拉格·巴爾。當我受傷垂死時，莫拉格·巴爾出現並提議讓我復活，代價是我的妻子。」回應 3 翻譯：「我接受了……我很後悔。我忘不了她那哀傷的眼神……」）

### 0x00EA84 zzAoMMq07B4AboutBal (背景知識：巴爾的本質)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EA84 zzAoMMq07B4AboutBal`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:344) | `0x00EA85` | 無 | `GetStage >= 30 && < 35`; `GetIsAliasRef alias #1` | 提示：[`"Tell me about Bal"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:344) 回應 (恐懼)：[`"Bal is powered by Molag bal....Her magicka is powerful and infinite..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:345) / 回應 (悲傷)：[`"She is a looks-alike for my wife. Probably, She is trap of Molag Bal..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:346) |

背景：巴爾是瑞海兒的幻身或冒牌模擬體，旨在折磨雅各。
（提示翻譯：「跟我說說巴爾的事」回應 1 翻譯：「巴爾受到莫拉格·巴爾的加持……她的魔力強大且無限……」回應 2 翻譯：「她長得像我的妻子。可能她是莫拉格·巴爾設下的陷阱……」）

## 自訂對話分支：阿爾塔諾 (Quest closure, 任務完結)

分支：
- `00EA86:Vigilant.esm`（隱含；階段 60–80 分支，別名 #0）

條件模式：
- 開場階段限制在 `GetStage == 60`；結案為 `GetStage >= 70 && < 80`。
- 代表雅各最終的懇求以及玩家對完成目標的回應。

### 0x00EA87 zzAoMMq07B5TakeMace (雅各最後的要求)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EA87 zzAoMMq07B5TakeMace`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:348) | `0x00EA88` | `Goodbye` | `GetStage == 60`; `GetIsAliasRef alias #0` | 回應：[`"All is gone... you ... you take the mace of Bal to me? I need it...."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:349) |

VMAD 片段：
- `AoM07_TIF__0100EA88`（觸發 `OnEnd` 片段）

背景：雅各請求玩家奪取莫拉格·巴爾的釘頭錘（推測是在擊敗巴爾後掉落的）。
（回應翻譯：「一切都結束了……你……你能把巴爾的釘頭錘拿給我嗎？我需要它……」）

### 0x00EA89 zzAoMMq07B5TakeMaceFollowUp (雅各後續行動)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x00EA89 (延續)`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:348) | `0x00EA89` | `SayOnce, WalkAway` | 玩家持有 [`00D9FC zzzCHMolagMace`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1) 數量 > 0；`GetStage >= 70 && < 80`；`GetIsAliasRef alias #0` | 回應：[`"....I will back to tha temple of Stendarr and ask keepers for advice about this mace."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:350) / [`"Before return to the hall...I ask you for a small mission. I heard there are witches at shack in the south of Ivarstead."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:351) / [`"Witch is a serious threat to peace of skyrim. Give them the Mercy of Stendarr...."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:352) |

VMAD 片段：
- `AoM07_TIF__0200EA89`（觸發 `OnBegin` 片段；可能銜接第一章 sq08 獵巫任務）

背景：在獲得釘頭錘後，雅各提出了下一個任務（第一章 sq08，伊瓦斯泰德南方的獵巫任務）並對女巫發出警告。
（回應 1 翻譯：「……我會回到斯丹達爾神殿，就這把釘頭錘向負責人們尋求建議。」回應 2 翻譯：「在回到大廳之前……我想請你執行一個小任務。我聽說在伊瓦斯泰德南方的一間小屋裡有女巫。」回應 3 翻譯：「女巫是對天際和平的嚴重威脅。給予她們斯丹達爾的慈悲吧……」）

翻譯筆記：
- 「Tha temple」是語法錯誤；應指「the temple」。

### 0x027A46 zzzAoMMq07B5JacobDead (雅各葬禮替代方案)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x027A46 zzzAoMMq07B5JacobDead`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:436) | `0x027A47` | `Goodbye` | `GetIsAliasRef alias #0` | 提示：[`"We should  hold a funeral for Jacob"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:436) 回應：[`"I will do. You put away the witch while I mourn for him. See you again in Temple of Stendarr."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:437) |

VMAD 片段：
- `AoM07_TIF__01027A47`（觸發 `OnEnd` 片段）

背景：**失敗分支。** 如果雅各在任務期間死亡（例如被巴爾擊敗），玩家可以提議舉辦葬禮。阿爾塔諾接受並依然引導玩家去獵巫。暗示雅各的死亡在敘事中是可以被接受的，但會被視為夥伴的損失。
（提示翻譯：「我們應該為雅各舉行葬禮」回應翻譯：「我會處理的。我去悼念他，你先去解決那個女巫。在斯丹達爾神殿再見。」）

## 支援對話分支

### 0x11E0AB zzzAoMMq07B6T01 (階段 30 的支援)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x11E0AB zzzAoMMq07B6T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1082) | `0x11E0AC` | `Goodbye, SayOnce` | `GetStage == 30`; `GetIsAliasRef alias #0` | 回應：[`"Support Jacob. I come see how to go ahead"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1083) |

VMAD 片段：
- `AoM07_TIF__0211E0AC`（觸發 `OnEnd` 片段）

背景：阿爾塔諾提議在調查階段幫助雅各。
（回應翻譯：「支援雅各。我來看看接下來該怎麼做」）

### 0x11E0AE zzzAoMMq07B7T01 (階段 33 的支援)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0x11E0AE zzzAoMMq07B7T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1085) | `0x11E0AF` | `Goodbye` | `GetIsAliasRef alias #0`; `GetStage == 33` | 回應：[`"Support Jacob,please"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1086) (恐懼情感) |

背景：由於雅各受到威脅，阿爾塔諾絕望地請求支援（階段 33 關鍵點）。
（回應翻譯：「請支援雅各」）

## 相關紀錄

NPCs：
- [`000D66 zzzAoMVigilantElder` - 雅各 (Jacob)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1)
- [`00E4FE zzzAoMM07GhostBal` - 瑞海兒 (Rahel) (巴爾的形式 / 幽靈)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1)
- [`031117 zzzBMVgilantsCorpse01` - 約書亞 (Joshua)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1)
- [`00183E zzzAoMBossDremora04` - 奧斯 (Orthe) (巴爾的魔人盟友)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1)
- [`00183F zzzAoMBossDremora05` - 燃雨 (Ranyu) (巴爾的魔人盟友)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1)

物品：
- [`00D9FC zzzCHMolagMace` - 莫拉格·巴爾的釘頭錘 (Mace of Molag Bal)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1) (任務完成物品)

## 重建筆記

基於原始碼：
- 任務 `00A3FE zzzAoMMq07` 代表雅各的救贖之路。雅各是一名老警戒者，他在 20 年前將妻子瑞海兒賣給了莫拉格·巴爾。
- 任務結構交替進行對峙（昂布拉，階段 0–10）、調查（雅各的往事，階段 20–35）和高潮（擊敗巴爾，階段 40–60），最後是完結（歸還釘頭錘，階段 60–80）。
- 場景主題（0x00E4E5 起）描述了對抗莫拉格·巴爾的實戰，以及雅各與瑞海兒靈魂和解的夢境或記憶序列。
- 存在兩個平行的對話分支：昂布拉的對峙（強硬）和雅各的往事解說（愧疚、絕望）。
- 存在一個失敗分支：如果雅各死亡，將會出現葬禮對話。

分支極性：
- **好路徑**：擊敗昂布拉，傾聽雅各的痛苦，擊敗巴爾，奪回釘頭錘，支援雅各的救贖 → 銜接至 sq08 (獵巫)。
- **雅各死亡路徑**：任務在雅各戰敗後依然存在；葬禮分支提議了一個替代的結局，但任務依然會朝向 sq08 進行。

業力結果：
- 從目前的來源不明確；可能是中立至善良（保衛墮落的盟友，對抗魔族腐化）。

發布狀態：
- 未偵測到不完整的片段；所有對話皆具有終結性的 VMAD 或 Goodbye 旗標。

開放驗證：
- 如果正式的場景主持人/別名/階段結構很重要，請直接檢查 SCEN 紀錄 (0x00E4E5, 0x00E4F4–FC, 0x00EA65–6D 可能具有 SCEN 主持人)。
- 驗證 `zzzAoMM07GhostBal` (瑞海兒) 的 NPC 旗標（例如：它是幽靈旗標，還是僅僅是另一種演員形式？）。
- 追蹤 VMAD 片段中的階段條件，以確認階段推進觸發器（特別是缺少對話條件的階段 33–37）。
- 透過 NPC 查找或場景/任務別名表確認別名 #3 的身份為「昂布拉 (Umbra)」。
- 核對階段 75（未發現對話條件）——可能是自動推進或基於觸發器。
