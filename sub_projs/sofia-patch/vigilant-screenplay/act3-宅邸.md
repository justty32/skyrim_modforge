# 第三幕：可怕的宅邸（Mansion Mystery）

> 劇本草稿（2026-06-14 重寫，de-AI bilingual）。共用原則見 [`README.md`](README.md)。設計 beat 見 [`../plans/vigilant-sofia-逐章演出設計.md`](../plans/vigilant-sofia-逐章演出設計.md) §3。對應 reconstruction：`act-3-sq-01-child` 等。觸發機制見 [`_act3-trigger-placement-map.md`](_act3-trigger-placement-map.md)。
> **本幕 Sofia 定調特殊**：恐怖宅邸，她**為掩飾恐懼而不斷主動插科打諢**——這是全 patch 她主動話最多、最密的一章。嘴砲底下藏著怕。

---

## 3-A｜宅邸基調：主動狂吐槽掩飾恐懼 ★
**【VIGILANT 劇情】** 進入一座極可怕的宅邸（Mq01 s20+，到達 Noble Mansion 後）。
**【類型】** 在場·環境吐槽（高頻主動，多條輪播）
**【gate】** `GetStageDone(zzzCOMq01, 20)==1` + `GetStageDone(zzzCOMq01, 50)==0`

> **SOFIA** *(player topic: "What do you make of this place?")*
> Okay. Just… taking it in. Don't mind me.
> This place has that particular smell — old wood, old blood, and something that used to be a person. I know that smell. I hate that smell.
> Are you planning to go *in*? Because the sensible options are, in order: leave, set it on fire from out here, *then* leave. You appear to be considering a fourth option.

〔ZH：好。好的。我就在這裡稍微……吸收一下氛圍。別理我。
這地方有一股很特別的味道——舊木頭、舊血、還有一些「以前是人」的東西。我認識這個味道。我討厭這個味道。
你是打算走進去的意思嗎？因為合理的選擇按優先順序是：離開、在外面把它點了、然後離開。你好像在考慮第四個選項。〕

---

> **SOFIA** *(ambient 輪播 1)*
> The windows are all dark. Usually that means nobody home. *Here* that means something is making sure nobody can look in.

〔ZH：窗戶全是暗的。通常這代表沒有人在。在這裡，這代表有什麼東西在確保沒有人能往裡面看。〕

---

> **SOFIA** *(ambient 輪播 2)*
> I've walked into crypts. I've walked into bandit camps. I've walked into at least one active volcano — long story. This is still worse, somehow. That's impressive. That's deeply, deeply impressive.

〔ZH：我走進過地下墓穴。走進過土匪營地。走進過至少一座還在活動的火山——說來話長。但這個地方不知道為什麼還是更糟。很了不起。真的是，非常非常了不起。〕

---

> **SOFIA** *(ambient 輪播 3)*
> You know the rule about old houses: don't touch anything you don't recognize, don't read anything you find, don't answer if something calls your name. ... We are absolutely going to break all three of those.

〔ZH：你知道老宅子的規矩：不認識的東西別亂摸、找到的東西別亂讀、有什麼東西叫你名字別答應。……我們肯定要把這三條全破掉。〕

---

## 3-B｜讀文本 → 評論
**【VIGILANT 劇情】** 玩家讀宅邸內某些文本。
**【類型】** 在場·玩家可問（讀後，也可主動）
**【gate】** `GetStageDone(zzzCOMq01, 20)==1` + `GetStageDone(zzzCOMq01, 50)==0`

> **SOFIA** *(player topic: "What do you make of what you just read?")*
> Another diary. Why do people in enormous haunted houses always keep diaries? You'd think they'd notice the correlation.
> Lots of money. No friends. Started talking to things they shouldn't. You know what people with friends do? They go out. They eat food someone else cooked. They don't end up in a journal I'm reading two hundred years later surrounded by whatever *that* is.
> ... I have you. I'm fine. I'm not judging.

〔ZH：又一篇日記。住在這種超大鬧鬼宅子裡的人，為什麼都要寫日記？照理說他們應該會注意到其中的關聯性。
錢很多。朋友沒有。開始跟不該說話的東西說話。你知道有朋友的人在做什麼嗎？他們出門。他們吃別人煮的飯。他們不會最後出現在一本我兩百年後讀到的日記裡、周圍全是不知道那是什麼的東西。
……我有你。我沒事。我不是在評論。〕

---

## 3-C｜觸碰塑像 / 進地圖打怪 → 跟隨 + 感想
**【VIGILANT 劇情】** 玩家觸碰一些塑像、進入一些地圖打怪。
**【類型】** 在場·環境吐槽
**【gate】** `GetStageDone(zzzCOMq01, 20)==1` + `GetStageDone(zzzCOMq01, 40)==0`

> **SOFIA** *(player topic: "You saw me touch that statue.")*
> I saw you touch the statue. I *watched* you touch the statue. I made specific eye contact with you *while* you touched the statue.
> And now there's something in the room with us that wasn't here before. So I just want to be clear on the chain of events.
> *(a beat)*
> Right. Well. Let's go, then.

〔ZH：我看到你摸那個塑像。我看著你摸的。你摸的時候我們還有眼神交流。
現在這個房間裡有了一個之前不在這裡的東西。我只是想把這一連串事件的因果說清楚。
（停頓）
好。那就——走吧。〕

---

> **SOFIA** *(ambient，進新區域打怪)*
> New room. New things that want us dead. This place is *consistent*, I'll give it that.

〔ZH：新房間。新的想殺我們的東西。這地方很有一致性，這點我承認。〕

---

## 3-D｜紅女巫（雜兵）→ 邊吐槽邊打 ★
**【VIGILANT 劇情】** 宅邸中出現「紅女巫」這類敵人。
**【類型】** 在場·環境吐槽（戰鬥中）
**【gate】** `GetStageDone(zzzCOMq01, 20)==1` + `GetStageDone(zzzCOMq01, 40)==0`

> **SOFIA** *(player topic: "Red robes in a house like this.")*
> Red. They wore *red*. In this place. On purpose.
> I have questions. I have so many questions. Starting with: who *decided*? Was it a group vote? Did someone go, "I know we're running a haunted noble estate, but have you considered — crimson?"
> *(coming out of the joke as she swings)*
> Focus. They're better than they look. Which is saying something, because they look *very good* at this.

〔ZH：紅色。她們穿了紅色。在這個地方。是主動選的。
我有問題。我有很多問題。從這個開始：是誰決定的？有投票嗎？有人說「我知道我們在經營一個鬧鬼的貴族宅邸，但你有沒有考慮過——緋紅色？」
（笑話收尾，同時揮動武器）
專心。她們比看起來厲害。這話說起來有點份量，因為她們看起來就很擅長這個。〕

---

## 3-E｜紅魔女（大 boss Julia）→ 感想 ★
**【VIGILANT 劇情】** 宅邸大 boss「Julia」（Child of Oblivion 型態）。
**【類型】** 在場·玩家可問（戰前/戰後）+ 戰中主動
**【gate 戰前】** `GetStageDone(zzzCOMq01, 30)==1` + `GetStageDone(zzzCOMq01, 40)==0`
**【gate 戰後】** `GetStageDone(zzzCOMq01, 40)==1` + `GetStageDone(zzzCOMq01, 50)==0`

> **SOFIA** *(player topic, 戰前: "Any read on the woman in charge here?")*
> Look at her. *Look at her.* The posture. The robes. That specific expression people get when they've been the most powerful thing in the room for so long they've forgotten how *bored* they look.
> She's been here a while, doing whatever this is. That's not confidence. That's just... the particular arrogance of someone who's never been interrupted.
> We're about to interrupt her.

〔ZH：你看她。你看她。那個姿態。那件袍子。那種人特有的表情——在一個房間裡當最強的東西當太久之後，他們已經忘記自己看起來有多無聊。
她在這裡待很久了，一直在做這些。那不是自信。那只是……一個從來沒有被打斷過的人特有的傲慢。
我們要去打斷她了。〕

---

> **SOFIA** *(player topic, 戰後: "She's down. How do you feel?")*
> She built all of this. Every room. Every trap. Every poor soul wandering the halls. All of it — to fill something.
> *(beat, quieter)*
> That's the thing nobody talks about. People don't go this far because they're *strong*. They go this far because there's a hole somewhere and they ran out of other ways to fill it.
> *(back to normal, a little clipped)*
> Anyway. What's next?

〔ZH：這一切都是她建的。每個房間。每個陷阱。每個在走廊裡遊蕩的可憐靈魂。所有這些——是為了填滿某個東西。
（停頓，聲音稍輕）
這個沒有人提。人不是因為強大才走到這一步的。是因為某個地方有個洞，而他們其他的填法都用完了。
（恢復正常，稍微收住）
好。接下來？〕

---

## 3-F｜管家 Baal（其實是吟遊詩人）→ Sofia 生氣 ★
**【VIGILANT 劇情】** 面對管家 Baal（Balthoro）——他就是第二章夢裡的吟遊詩人。Sofia 認出他、生氣。
**【類型】** 在場·玩家可問（認出時主動）
**【gate】** `GetStageDone(zzzCOMq01, 20)==1` + `GetStageDone(zzzCOMq01, 40)==0`

> **SOFIA** *(player topic: "That man. You know him.")*
> That face.
> *(not loud; very controlled)*
> I know that face.
> The bard. The one from — *him*. He was in the dream. Said exactly the right things, in exactly the right order, to exactly the right people. And now he's here, in a *butler's uniform*, acting like we've never met.
> *(to player, low)*
> I'm very easygoing. Ask anyone. But there are two kinds of people I don't forgive: people who lie to *me*, and people who lie to people I care about. He's managed both.
> Don't hold me back. Actually — hold me back. For *one minute*.

〔ZH：那張臉。
（不大聲；非常克制）
我認識那張臉。
那個吟遊詩人。第二章的那個——就是他。他在夢裡。說了剛好正確的話、用了剛好正確的順序、說給了剛好正確的人聽。現在他站在這裡、穿著管家制服、裝作我們素未謀面。
（對玩家，很輕）
我這個人很好相處。你去問任何人。但有兩種人我不原諒：騙我的人，跟騙我在乎的人的人。他兩樣都做到了。
別拉住我。其實——先拉住我。先給我一分鐘。〕

---

> **SOFIA** *(follow-up: "What are you going to do?")*
> Nothing. Right now.
> *(a small, humorless smile)*
> There are more important things in this house. *He* knows that. So do I. He's banking on it.
> Later. We'll have a conversation later. And I will be *very* calm. Very professional. Very measured.
> *(quietly)*
> He should be worried.

〔ZH：什麼都不做。現在先不做。
（一個小小的、沒有笑意的微笑）
這棟房子裡還有更重要的事。他知道。我也知道。他就是在押這個注。
之後。我們之後會談。我會非常冷靜。非常專業。非常有分寸。
（輕聲）
他應該要擔心。〕

---

## 3-G｜Julius（尤里烏斯 / 松加得）火焰 boss ★
**【VIGILANT 劇情】** 最後的 Julius boss 讓場地充滿火焰。
**【類型】** 在場·環境吐槽（戰鬥中，奮起）
**【gate】** `GetStageDone(zzzCOMq01, 50)==1` + `GetStageDone(zzzCOMq01, 70)==0`

> **SOFIA** *(player topic: "He's burning the whole place.")*
> He's burning it. He's *burning all of it*. The exit. The ceiling. The floor. *Us*, specifically us, is very much on the list.
> *(coughs, genuine)*
> Okay. You know what? I've been scared this whole time. I'm going to stop pretending I haven't. I've been *terrified*, and my hair is now on fire, and I'm still *here*, so clearly that counts for something.
> *(rallying, real)*
> Stendarr. If you're watching — this is the trial. This is *it*. I see you raising the bar.
> We're not dying here. Not like this. Not *this badly dressed*.

〔ZH：他在燒。他在把所有東西都燒掉。出口。天花板。地板。還有我們，我們很明顯在名單上。
（咳嗽，是真的）
好。你知道嗎？我這整段時間都一直很害怕。我不打算繼續假裝沒有。我非常、非常害怕，而且我的頭髮現在在著火，但我還在這裡——所以很顯然這是有意義的。
（真的在奮起）
松加得。如果你在看——這就是試煉。就是這個。我看到你在拉高標準。
我們不在這裡死。不是這樣死。不是穿成這樣死。〕

---

## 3-H｜火焰未熄 → 抉擇：被燒死 or 進入冷港 ★★ 關鍵分歧
**【VIGILANT 劇情】** 打完 Julius 後火焰沒熄滅，玩家陷入兩個抉擇：被燒死 或 進入冷港。Sofia 支持玩家的抉擇。
**【類型】** 在場·玩家可問（抉擇前/後，兩條依分支）

### 抉擇前（Sofia 表態）
**【gate】** `GetStageDone(zzzCOMq01, 70)==1` + `GetStageDone(zzzCOMq01, 80)==0`

> **SOFIA** *(player topic: "The fire isn't stopping.")*
> No. It's not.
> *(a beat — looking at the options)*
> Two doors. The fire, or... that. Whatever *that* is.
> I'm not going to tell you which one. This is yours. I've had opinions about everything in this entire building — I'm fresh out for this particular question.
> *(quiet, direct)*
> Whatever you pick: I'm with you. Not because I have to be. Not because of any contract. Just — I'm with you.

〔ZH：對。它不停。
（停頓——看著兩個選擇）
兩個出口。火，或者……那個。管那是什麼的那個。
我不打算告訴你選哪個。這是你的決定。我對這棟房子裡的每件事都有意見——但這個問題我剛好沒了。
（安靜，直接）
不管你選哪個：我跟你走。不是因為我必須。不是因為任何合約。就是——我跟你走。〕

---

### 分支 A：玩家選擇進入冷港
**【gate】** `GetStageDone(zzzCOMq01, 90)==1` + `GetGlobalValue("MF_SofA3_BurnBranch")==0`

> **SOFIA** *(player topic: "We're going through. Ready?")*
> Into the gate. Sure.
> *(dry)*
> We've walked into worse. I think. I'm going to *decide* we have.
> *(holds out a hand — briefly, matter-of-fact)*
> Come on then.

〔ZH：走進那個門。好的。
（乾）
我們走進過更糟的地方。我想。我要說服自己我們走進過更糟的地方。
（短暫地伸出手——隨意地）
那就走吧。〕

---

### 分支 B：玩家選擇被燒死
**【gate】** `GetStageDone(zzzCOMq01, 200)==1` + `GetGlobalValue("MF_SofA3_ColdharbourBranch")==0`

> **SOFIA** *(player topic: "We stand our ground.")*
> Alright.
> *(small pause)*
> Alright.
> *(calling out, full voice — real conviction)*
> Stendarr! We're on our way! We're coming *standing up* — make some room!
> *(quieter, to player)*
> Seemed like the right thing to say.

〔ZH：好吧。
（短暫停頓）
好吧。
（大聲喊出，真的有信念）
松加得！我們來了！我們是站著去的——給我們騰個地方！
（更輕，對玩家）
這個時候好像就應該說這個。〕

---

### 分支 B 後：傳送後 Sofia 一臉茫然
**【gate】** `GetStageDone(zzzCOSubQ01, 10)==1`

> **SOFIA** *(player topic: "You're still here.")*
> I'm still here.
> *(looks at own hands)*
> I was very much on fire. I have specific, detailed memories of being on fire. My boots are singed. That's... real. But here I am.
> *(shrug — not quite landing, a little too bright)*
> The Nine. They do a thing sometimes. I'm not questioning it. We had a good death, I think we earned a second round.
> *(beat)*
> ...You're you, right?

〔ZH：我還在這裡。
（看著自己的手）
我明明在著火。我有具體的、詳細的記憶說明我當時在著火。我的靴子有燒焦的痕跡。這是真的。但我還在這裡。
（聳肩——有點撐不住，稍微太活潑）
九聖靈。他們有時候會出手。我不打算追問。我們死得很好，我覺得我們贏得了第二輪。
（停頓）
……你是你，對吧？〕

---

## 3-I｜★ Meta 轉折（VIGILANT 核心詭計，輕巧帶過）
**【VIGILANT 劇情（背景設定，不一定要明寫成對白）】**
- 若玩家選擇被燒死：其實第一～三章**不是龍裔玩家完成的，而是另一個警戒者**。燒死後玩家回斯坦達爾聖堂**重新捏臉**＝換成第二個人（龍裔）；回宅邸可見一具**焦屍**＝前一個警戒者。**進入冷港的一定是龍裔。**
- **Sofia patch 處置**：走「燒死→換龍裔」線時，Sofia 在冷港重新出現的連續性，用 §0.3 命運糾纏 +「九聖靈」一語帶過（見 3-H 末），**不深究、保持輕巧**。

**【可選台詞：Sofia 在宅邸見到焦屍】**
**【gate】** `GetStageDone(zzzCOSubQ01, 20)==1` + `GetStageDone(zzzCOSubQ01, 40)==0`（可選，不實作也不影響主線）

> **SOFIA** *(player topic: "That burned body.")*
> I... yeah.
> *(a beat, looking at it)*
> There's something weird about looking at that. I can't put my finger on it. Like looking at something I've seen before, in a dream, in the wrong order.
> *(shakes it off)*
> Doesn't matter. We're here. The body is there. Let's keep moving — standing around near corpses is how things get worse in places like this.

〔ZH：我……對。
（停頓，看著）
看著這個有種奇怪的感覺。我說不清楚。像是看到了一個我在夢裡見過的東西、但順序不對。
（甩開）
沒關係。我們在這裡。屍體在那裡。繼續走——在這種地方站在屍體附近是事情變糟的標準方式。〕

---

> **第三幕完。** 進入冷港即第四幕（zzzCHMq00）。
