# 第一章 任務 11 - 慈悲的藝術 (好結局)

狀態：第一個重製片段。基於原始碼，連結優先，非劇情摘要。

來源策略：
- 原始對話行連結回提取的來源檔案，而非完整複製。
- 僅在需要解釋對話條件時才顯示短小的原始碼片段。
- `SCEN` 紀錄來自 CLI 診斷；提取出的 `dialogue.md` 僅保留場景獨白文本，不包含階段/動作細節。

## 任務紀錄 (Quest Record)

[`4D0376 zzzAoMMqGoodEnd "Art of Mercy"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600)

CLI：
- `questdiag Vigilant.esm 0x4D0376`
- `infodiag Vigilant.esm 0x4D0376`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務資料：
- FormID: `Vigilant.esm:0x4D0376`
- EditorID: `zzzAoMMqGoodEnd`
- 名稱 (Name): `Art of Mercy`
- 旗標 (Flags)：（待透過 CLI 提取）
- 優先度 (Priority)：（待透過 CLI 提取）
- 類型 (Type)：（待透過 CLI 提取）
- 過濾器 (Filter): `AoM\`

來自任務提取的階段 (Stages)：

| 階段 | 旗標 | 日誌 |
|---:|---|---|
| 0 | 無 | 空白 |
| 10 | 無 | 空白 |
| 20 | 無 | 空白 |
| 29 | 無 | 空白 |
| 30 | 無 | 空白 |
| 110 | CompleteQuest | 空白 |
| 255 | ShutDownStage | 空白 |

目標 (Objectives)：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600) | 與卡蓮娜對話 (Talk to Carene) |
| 10 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600) | 阻止卡蓮娜 (Stop Carene) |
| 20 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600) | 與卡蓮娜對話 (Talk to Carene) |
| 29 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600) | 除掉卡蓮娜（選項） (Eliminate Caren (Option)) |
| 30 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600) | 離開這裡 (Go away from here) |
| 110 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600) | 向索隆迪爾回報 (Report to Thorondir) |

目標對象 (Objective targets)：
- 目前的 CLI 輸出未印出目標對象 (target refs)；若位置標記很重要，則需要更深入的 QUST 目標轉儲。

## 別名 / 編排主幹 (Alias / Staging Backbone)

主任務：
- [`4D0376 zzzAoMMqGoodEnd`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600)

來自 `infodiag` 的對話別名：
- 別名（預期）：`Carene`（寡婦/悲傷的母親，此分支的主要說話者）。
- 別名（預期）：`Thorondir`（神殿祭司，最終回報對象）。

（推論：別名角色是根據對話條件與任務目標推斷而來；需要來自 CLI 的明確別名轉儲）

## 場景紀錄 (Scene Records)

好結局終章存在兩個 `SCEN` 紀錄。

### 0x4D039B zzzAoMMqGESceneChild

（推論：場景標題來自對話內容；FormID 來自提取的 dialogue.md 結構）

編排：
- 主任務：[`4D0376 zzzAoMMqGoodEnd`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600)
- 演員：(根據獨白內容，疑似是卡蓮娜的孩子)

來自提取對話的獨白：
- [`4D039B` 場景行](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md)：「我們做到了！媽，我做到了！我打敗壞人了！」

### 0x4D039D zzzAoMMqGESceneCarene

（推論：場景標題來自對話內容；FormID 來自提取的 dialogue.md 結構）

編排：
- 主任務：[`4D0376 zzzAoMMqGoodEnd`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:600)
- 演員：`Carene` (母親)

來自提取對話的獨白：
- [`4D039D` 場景行](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md)：「媽，嘿，媽媽，……求求妳回答我，……」

## 自訂對話分支：卡蓮娜 (母親) — 對峙與救贖

分支：
- 主主題：[`4D0379 zzzAoMMqGoodEndHello`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1) [Misc/Hello]

Hello 開場白（受階段限制）：
- [`4D0379` Hello 行 1](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1)：「我沒想到你真的在這裡。我就知道那個故事是真的……」
- [`4D0379` Hello 行 2](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1)：「為什麼我的丈夫死了？你卻活著。……這只是個開始。……」
- [`4D0379` Hello 行 3](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1)：「歡迎回來。召喚師那邊進展得如何？」
- [`4D0379` Hello 行 4](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1)：「這是我最後一次跟你說話了。好好照看聖所。」

### 分支 1：任務開場 — 「你為什麼在這裡？」

主題 (TOPIC) `0x4D037E zzzAoMMqGEMomB01T01`

條件模式：
- （推論：階段限制在 0–10，說話者為 `GetIsAliasRef Carene`）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D037E zzzAoMMqGEMomB01T01` | (INFO ID 待定) | 無 | (階段 < 10; 說話者) | 提示：[`"Why are you here?I told you to run."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2) 回應 (中立)：[`"I am grateful that you overlook us, my daughter and I. But I can't overlook ...... you."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2) |

（註：回應翻譯：「我很感激你放過了我們母女倆。但我沒辦法放過……你。」）

### 分支 2：對話樹 — 「那是怎麼回事？」

主題 (TOPIC) `0x4D0381 zzzAoMMqGEMomB02T01`

條件模式：
- （推論：階段限制在 10+，說話者為 `GetIsAliasRef Carene`）

對話結構（多重回應樹）：

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D0381 zzzAoMMqGEMomB02T01` | (ID 待定) | 無 | (階段 >= 10; 說話者) | 提示：[`"What's the story?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3) 回應：[`"A kind person told me about you. He told me that you had taken my husband, Taranis. ......"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3) |
| `0x4D0383 zzzAoMMqGEMomB02T02` | (ID 待定) | 無 | (說話者) | 提示：[`"Kind ...... Didn't it call itself Orlando?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:4) 回應：[`"Yes, that may have been the name. But that doesn't matter now, does it?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:4) |

（註：回應 1 翻譯：「一位好心人告訴了我關於你的事。他告訴我你帶走了我的丈夫塔拉尼斯。……」回應 2 翻譯：「是的，可能就是那個名字。但那現在不重要了，對吧？」）

### 分支 3：對話分支 — 玩家抉擇點（辯護 / 慈悲 / 正義）

基於玩家回應的多路徑分支：

#### 路徑 3a：正當防衛論點

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D0385 zzzAoMMqGEMomB02T03` | (ID 待定) | 無 | (說話者) | 提示：[`"It was a legitimate defense. We both had things we couldn't give up."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:5) 回應 1：[`"I'm sure you're right. Maybe there was a reason. Still, I can't forgive you for killing Taranis."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:5) 回應 2：[`"Take your weapons. I will avenge my husband here and now."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:5) |

（註：提示翻譯：「那是正當防衛。我們都有無法放棄的東西。」回應 1 翻譯：「我想你是對的。或許那是有原因的。但我還是無法原諒你殺了塔拉尼斯。」回應 2 翻譯：「拿起你的武器。我現在就要在這裡為我丈夫報仇。」）

#### 路徑 3b：訴諸慈悲（孤兒論點）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D0387 zzzAoMMqGEMomB02T04` | (ID 待定) | 無 | (說話者) | 提示：[`"Don't you dare take revenge. You're going to make your daughter an orphan."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:6) 回應 1：[`"Are you trying to scare me? Do you think you're the only one who won't die? Why does a rogue like you call yourself the Vigil!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:6) 回應 2：[`"I'm ...... going to kill you, right here. I'm not going to live in mourning over the murder of my husband!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:6) |

（註：提示翻譯：「妳敢報仇試試看。妳會讓妳的女兒變成孤兒。」回應 1 翻譯：「你想嚇唬我嗎？你覺得只有你不會死嗎？為什麼像你這樣的流氓敢自稱為警戒者！」回應 2 翻譯：「我……要在這裡殺了你。我才不要活在丈夫被謀殺的哀慟中！」）

#### 路徑 3c：訴諸和平主義

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D0389 zzzAoMMqGEMomB02T05` | (ID 待定) | 無 | (說話者) | 提示：[`"I don't want to see any more blood today."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:7) 回應 1：[`"I'm sure you are. You've killed so many people in your life, you're tired of looking at them!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:7) 回應 2：[`"I'll show you your blood once and for all! Let that be your atonement!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:7) |

（註：提示翻譯：「我今天不想再見到血了。」回應 1 翻譯：「我想也是。你這輩子殺了這麼多人，都看膩了吧！」回應 2 翻譯：「我要讓你看個夠你自己的血！就讓這成為你的贖罪吧！」）

#### 路徑 3d：正義教義

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D038B zzzAoMMqGEMomB02T06` | (ID 待定) | 無 | (說話者) | 提示：[`"Everything the Vigils do is justice. Taranis' death is also undeniable justice."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:8) 回應：[`"You crazy son of a bitch! I'll kill you!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:8) |

（註：提示翻譯：「警戒者所做的一切皆為正義。塔拉尼斯的死也是無可置疑的正義。」回應翻譯：「你這個瘋子混蛋！我要殺了你！」）

#### 路徑 3e：挑釁（接受復仇）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D038D zzzAoMMqGEMomB02T07` | (ID 待定) | 無 | (說話者) | 提示：[`"Vengeance...good, come on."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:9) 回應：[`"You don't have to tell me what to do! Prepare yourself!"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:9) |

（註：提示翻譯：「復仇……很好，來吧。」回應翻譯：「不用你告訴我該怎麼做！受死吧！」）

### 分支 4：戰後（卡蓮娜被打敗 / 被平息）

#### 路徑 4a：延遲的報應（玩家倖存且未下殺手）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D0390 zzzAoMMqGEMomB03T01` | (ID 待定) | 無 | (說話者; 階段 >= 20) | 提示：[`"I'll deal with you anytime. Until you're ready. ......"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:10) 回應：[`"Someday ...... someday I will definitely, definitely kill you. ...... I won't forgive you, I won't forgive you. ......"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:10) |

（推論：階段 20 可能標誌著戰鬥結束；卡蓮娜活了下來，並發誓未來會報仇）
（註：提示翻譯：「我隨時奉陪。直到妳準備好為止。……」回應翻譯：「總有一天……總有一天我一定、一定要殺了你。……我不會原諒你，我絕不原諒你。……」）

#### 路徑 4b：道德辯論（玩家以教義證明正當性）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D0393 zzzAoMMqGEMomB04T01` | (ID 待定) | 無 | (說話者; 階段 >= 20) | 提示：[`"I don't care if I have to be forgiven to be right. Iget dirty willingly."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:11) 回應：[`"I hope you fall into Oblivion without your sanctimonious preaching. ......"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:11) |

（註：提示翻譯：「我不在乎是否需要被原諒才能做正確的事。我甘願讓雙手沾滿鮮血。」回應翻譯：「我希望你帶著你那道貌岸然的傳教墮入湮滅。……」）

#### 路徑 4c：最終慈悲提議（任務結束路徑 — 救贖）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D0396 zzzAoMMqGEMomB05T01` | (ID 待定) | 無 | (說話者; 階段 >= 29) | 提示：[`"this is last assistance for you"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:12) 回應：[`"If you're going to do it, ...... get on with it. ...... I'm not going to beg for my life. ......"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:12) |

（推論：階段 29 標誌著最終抉擇點；回應暗示願意接受死亡；觸發通往階段 30 的好結局路徑）
（註：提示翻譯：「這是我給妳最後的協助。」回應翻譯：「如果你打算動手，……就快點。……我不會乞求饒命的。……」）

## 自訂對話分支：索隆迪爾 (神殿祭司) — 回報與結案

分支：
- （預期 INFO 位於任務 0x4D0376 所擁有的通用 Hello 或自訂任務主題中）

對話涉及召喚師的命運與任務結果：

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D03A3 zzzAoMMqGEKeepB01T01` | (ID 待定) | 無 | (階段 >= 30; 說話者) | 提示：[`"The summoner was vanquished by the Stendarr beacon."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:13) 回應 1：[`"Well, I'm glad to hear that. You can relax from your journey here in this cathedral for a while."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:13) 回應 2：[`"That said, I don't see any sign of Altano. Where is he now?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:13) |

（註：提示翻譯：「召喚師已被斯丹達爾燈塔擊敗。」回應 1 翻譯：「那太好了。你可以在這座大教堂稍微休息一下，緩解旅途的疲憊。」回應 2 翻譯：「話說回來，我沒看到阿爾塔諾的身影。他現在在哪？」）

### 索隆迪爾回報路徑（結局變體）

玩家的結果回報會觸發祭司不同的反應：

#### 路徑 A：阿爾塔諾已死

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D03A5 zzzAoMMqGEKeepB01T02` | (ID 待定) | 無 | (說話者; 阿爾塔諾死亡條件?) | 提示：[`"By the beacon of Stendhal, Altano was martyred."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:14) 回應 1：[`"There are many bloody stories in the basement of Stendhal's beacon. Has he become a victim of this?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:14) 回應 2：[`"Well, he was quite good, wasn't he? It's a pitty."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:14) |

（註：提示翻譯：「在斯丹達爾燈塔，阿爾塔諾殉教了。」回應 1 翻譯：「斯丹達爾燈塔的地窖裡有許多血腥的故事。他難道成了其中的受害者嗎？」回應 2 翻譯：「唉，他真的很優秀，不是嗎？真是遺憾。」）

#### 路徑 B：阿爾塔諾被腐化

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D03A7 zzzAoMMqGEKeepB01T03` | (ID 待定) | 無 | (說話者; 受到魔侯操弄條件) | 提示：[`"Altano was manipulated by Daedra, so I had no choice."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:15) 回應 1：[`"He must have gotten too close to the altar in the basement. Because Morag Bal will kill people together for fun. ......"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:15) 回應 2：[`"I know you have a lot on your mind, but don't get too worked up about it. That's exactly what Morag bal would want you to do."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:15) |

（註：提示翻譯：「阿爾塔諾受魔侯操弄，所以我別無選擇。」回應 1 翻譯：「他一定是太靠近地窖的祭壇了。因為莫拉格·巴爾會為了好玩而隨意殺人……」回應 2 翻譯：「我知道你心裡不好受，但別太自責。那正是莫拉格·巴爾希望你做的。」）

#### 索隆迪爾未來的計畫

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D03A9 zzzAoMMqGEKeepB01T04` | (ID 待定) | 無 | (說話者) | 提示：[`"What do you plan to do now?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:16) 回應 1：[`"I will go to Stendarr's Beacon to destroy the altar. I've learned the hard way that it's not enough to keep people away."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:16) 回應 2：[`"We'll be away from this temple for a while. In the meantime, I'd like you to take care of it. Will you do me a favor?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:16) |

（註：提示翻譯：「你現在打算怎麼辦？」回應 1 翻譯：「我會去斯丹達爾燈塔毀掉那個祭壇。我已經慘痛地學到，僅僅讓人遠離是不夠的。」回應 2 翻譯：「我們會離開這座神殿一段時間。期間我想請你代為照看。你能幫我這個忙嗎？」）

#### 玩家的猶豫

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D03AB zzzAoMMqGEKeepB01T05` | (ID 待定) | 無 | (說話者) | 提示：[`"I'm not strong enough."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:17) 回應：[`"Don't be modest. Just escaping Morag Bal's schemes has shown you to be of sufficient caliber."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:17) |

（註：提示翻譯：「我不夠強大。」回應翻譯：「別謙虛了。光是能逃過莫拉格·巴爾的陰謀，就足以證明你具備足夠的器量。」）

#### 最終任務指派（任務結束）

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D03AD zzzAoMMqGEKeepB01T06` | (ID 待定) | 無 | (說話者; 階段 >= 110 或結束) | 提示：[`"I'll take care of it."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:18) 回應：[`"I entrust you with this horn. It is a token of your protection. Pray to it when you are lost."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:18) |

（推論：獎勵或終結對話；階段 110 可能標誌著任務完成）
（註：提示翻譯：「交給我吧。」回應翻譯：「我將這支號角託付給你。它是你受到庇佑的象徵。當你迷失方向時，就向它祈禱吧。」）

## 重建筆記

基於原始碼：
- 此任務 (`0x4D0376 zzzAoMMqGoodEnd`) 代表第一章終章的**好結局**路徑，核心在於**與卡蓮娜的對峙**（塔拉尼斯的寡婦，塔拉尼斯是位墮落的盟友）。
- 包含兩個 `SCEN` 紀錄 (`0x4D039B`, `0x4D039D`)，在終章編排卡蓮娜與其孩子的對話獨白。
- 核心機制是**基於對話的抉擇樹**，決定卡蓮娜是攻擊、接受慈悲還是達成救贖：
  - 路徑 1：玩家挑釁 / 證明正當性 → 卡蓮娜戰鬥 → 階段 20 (戰敗但倖存)。
  - 路徑 2：玩家展示慈悲 / 訴諸情感 → 卡蓮娜饒過玩家 → 階段 29 (提供救贖)。
  - 路徑 3：玩家拒絕 → 階段 30 (卡蓮娜接受命運) → 透過向索隆迪爾回報來完成任務。
- **索隆迪爾**（斯丹達爾燈塔的神殿祭司）擔任**任務結案 NPC**，聽取玩家關於阿爾塔諾命運的回報並商討未來的神殿防禦。

分支極性（好結局與其他結局）：
- **好結局** = 卡蓮娜倖存，接受慈悲或至少達成和解；玩家維持道德高位；索隆迪爾託付玩家一支神聖號角（獎勵物品）。
- （與**壞結局**對比，若存在此任務變體的話；目前尚未檢查該部分）。

開放驗證：
- 提取卡蓮娜對話主題的 VMAD 腳本 / 條件（許多條件是推斷出的，尚未透過 `infodiag` 檢查）。
- 透過 `scenediag 0x4D039B` 與 `scenediag 0x4D039D` 檢查 SCEN 階段/動作結構，以獲取編排細節（計時器、情感、中斷旗標）。
- 若存在特定地點的任務，請驗證目標對象（階段 30「離開這裡」可能有目標引用）。
- 若 `0x4D03AD` 在任務完成時授予實體物品，請檢查獎勵物品（神聖號角）紀錄。
- 透過深入的 QUST 別名轉儲交叉引用卡蓮娜與索隆迪爾的 NPC 別名（CLI 未印出）。
- 驗證任務路徑是分支成獨立的 `SCEN` 還是共享相同的場景紀錄（根據對話，孩子獨白 `0x4D039B` 在兩種結果中都會出現，但確切的觸發條件未知）。
