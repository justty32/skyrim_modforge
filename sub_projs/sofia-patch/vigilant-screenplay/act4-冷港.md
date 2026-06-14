# 第四幕：冷港（Coldharbour）

> 劇本（de-AI'd bilingual draft，2026-06-14）。EN = voiced lines；〔ZH〕= 繁中對照。
> 設計 beat 見 ../vigilant-sofia-逐章演出設計.md §4。
> 觸發對照見 _act4-trigger-placement-map.md。
> **本幕核心規則**：13 段記憶 Sofia 大部分不跟入；只 Pelinal（MeQ10）跟入；出來後只有 MeQ01/02/07 給輕量可問；Molag Bal 對峙高張力；Karma 結局分歧。寧缺勿濫。

---

## 4-A｜初到冷港

【VIGILANT】玩家被 Molag Bal 拖入冷港。
【類型】在場·玩家可問（sayOnce+GLOB）
【Gate】GetStageDone(zzzCHMQ00 0x12F24E, 10)==1 + GetInWorldspace(0x06D275)==1

> **PROMPT**: So this is Coldharbour.
>
> SOFIA: Molag Bal's living room. ...Cosy. Reminds me of my ex's place — and I mean the décor, before your mind goes somewhere. Stop looking at me like that.
> 〔SOFIA：莫拉·巴爾的客廳。…挺溫馨的。讓我想起我前男友家——我是說裝潢，別往歪了想。別這樣看我。〕
>
> └─【追問】PROMPT: You sound nervous.
>     SOFIA: Nervous? Me? Ha. I'm never nervous. ...That burning manor I was trapped in once? This is practically a holiday compared to that. Stay close, alright. Not because I'm worried. I'm worried *you'll* get lost and then I'll have to come find you. That's annoying.
>     〔SOFIA：緊張？我？哈。我從不緊張。…那棟把我燒在裡面的鬼宅？跟那個比，這裡簡直是度假。跟緊一點，好嗎。不是因為我擔心。是擔心你迷路，然後我得去找你。很煩的。〕

---

## 4-B｜嚮導 Pepe

【VIGILANT】Pepe（前 Marukhati 審判官、冷港墮落靈魂）成為玩家嚮導。
【類型】在場·玩家可問（sayOnce+GLOB）
【Gate】GetStageDone(zzzCHMQ00 0x12F24E, 10)==1 + GetInWorldspace(0x06D275)==1

> **PROMPT**: Thoughts on Pepe?
>
> SOFIA: Our cursed tour guide. A Marukhati Inquisitor who's been damned for centuries — you can tell, he has that look. The one where you've been wrong about something for so long you've forgotten what right looked like. ...At least he knows where we're going. Probably.
> 〔SOFIA：我們的詛咒觀光嚮導。一個被詛咒幾百年的 Marukhati 審判官——看得出來，他有那種表情。就是你錯了太久，已經忘記對長什麼樣子的那種。…至少他知道路。大概。〕

---

## 4-C｜記憶夢境（總則）

【VIGILANT】冷港由 damned 靈魂的記憶構成；玩家進入各段記憶。
【類型】大部分不跟入。Sofia 留在冷港外圍。

> *設計：下列三段記憶有出來後的輕量可問（player topic, optional）。其餘九段 Sofia 全沉默。*

---

### 4-C-1｜記憶後：大審判官（MeQ01 完成後可問）

【Gate】GetStageDone(0x12C4F4, 20)==1 OR GetStageDone(0x12C4F4, 100)==1

> **PROMPT**: About that Inquisitor...
>
> SOFIA: Burned people to prove his own holiness. The classic move — when you're that scared of being wrong, you make sure everyone else is wrong *first*, and permanently. ...At least when I'm awful I own it. Mostly.
> 〔SOFIA：燒死人來顯示自己的神聖。經典操作——當你那麼怕自己是錯的，就先把所有人變成永遠的錯。…至少我殘忍的時候，我還承認。大部分時候。〕

---

### 4-C-2｜記憶後：瘋王（MeQ02 完成後可問）

【Gate】GetStageDone(0x13712B, 30)==1 OR GetStageDone(0x13712B, 130)==1

> **PROMPT**: The Mad King...
>
> SOFIA: Rich men and madmen. Nobody tells either of them their ideas are terrible. The only difference is madmen don't have enough gold to make everyone else go along with it. ...Usually.
> 〔SOFIA：有錢人跟瘋子。沒人敢告訴他們點子很爛。差別只是瘋子沒夠多的金子讓別人順著他。…通常。〕

---

### 4-C-3｜記憶後：Marukh 的誘惑（MeQ07 完成後可問）

【Gate】GetStageDone(0x06F53C, 70)==1 OR GetStageDone(0x06F53C, 150)==1

> **PROMPT**: Marukh and the voices...
>
> SOFIA: Another one who started hearing divine instructions. Every time someone gets a direct line to the gods, the world gets a lot messier and the body count goes up. ...I hear voices too, but mine mostly just say 'you look great today, Sofia.' Much safer.
> 〔SOFIA：又一個開始聽見神諭的傢伙。每次有人跟神搭上直線，世界就會亂很多、屍體就會多很多。…我也聽見聲音，不過我的大概只說「Sofia 妳今天很漂亮」。安全多了。〕

---

## 4-D｜記憶：Pelinal（MeQ10 Song of Pelinal）★ 跟入

【VIGILANT】Pelinal Whitestrake — 神話時代的聖徒英雄，為 Alessia 屠殺精靈，半人半神半屠夫。MeQ10 = Pelinal the Bloody。
【類型】夢中·幻影掛件（Sofia 跟入；玩家可問）

---

### 4-D-A｜進入記憶中（MeQ10 進行中）

【Gate】GetQuestRunning(0x2A532E)==1 AND GetStageDone(0x2A532E, 30)==1 AND GetStageDone(0x2A532E, 180)==0 AND GetStageDone(0x2A532E, 300)==0 + GetInWorldspace(0x06D275)==1

> **PROMPT**: Pelinal...
>
> SOFIA: I've read about him. The books call him a hero. A saint. Alessia's divine champion. ...But look at him. He's not a hero. He's a machine that can't stop. He kills because stopping would mean *thinking*, and thinking would mean... something he's not ready for.
> 〔SOFIA：我讀過他的故事。書上說他是英雄。聖徒。艾蕾西婭的神聖勇士。…但你看看他。他不是英雄。他是一台停不下來的殺戮機器。他殺人是因為停下來就得想，想了就意味著某件他還沒準備好面對的事。〕
>
> └─【追問】PROMPT: Do you want to be legendary?
>     SOFIA: Me? Gods, no. Legends die badly and look terrible in the paintings afterward. I want to live, look amazing doing it, and be remembered as 'that incredibly attractive woman who was also somehow always right.' That's my version of immortality.
>     〔SOFIA：我？天神啊，不要。傳奇都死得很慘，而且畫像都很醜。我想要活著，活得漂漂亮亮，被記得是「那個超正又莫名其妙每次都對的女人」。那才是我要的不朽。〕

---

### 4-D-B｜Pelinal 完成·好結局（饒恕 Mary）

【Gate】GetStageDone(0x2A532E, 180)==1

> **PROMPT**: He let her go.
>
> SOFIA: He let her go. The slave. Umaril's woman, who would've been nothing to him but a weakness to cut off. And he let her go. ...Sometimes the legend and the man are the same thing after all. I hate when that happens.
> 〔SOFIA：他放她走了。那個奴隸。Umaril 的女人，對他來說本來不過是要斬斷的弱點。但他放她走了。…有時候傳奇跟人是同一件事。我最討厭這種時候了。〕

---

### 4-D-C｜Pelinal 完成·壞結局（殺 Mary）

【Gate】GetStageDone(0x2A532E, 300)==1

> **PROMPT**: He killed her.
>
> SOFIA: Of course he did. She was in the way. ...I'm not going to pretend I'm surprised. The part of the story where the hero kills the pregnant woman — that's usually the part that gets left out of the songs. Wonder why.
> 〔SOFIA：當然殺了。她礙事。…我不打算假裝我很驚訝。英雄殺死孕婦的那段——通常就是歌裡被省略的部分。奇怪吧。〕

---

## 4-E｜Molag Bal 對峙 ★

【VIGILANT】玩家面對 Molag Bal 本尊。他能看到 Sofia，但把她當空氣——只有 Dragonborn 值得他正眼相待。
【類型】在場·玩家可問 + 追問（sayOnce+GLOB）
【Gate】GetStageDone(zzzCHMQ00 0x12F24E, 90)==1 + GetInWorldspace(0x06D275)==1
【NOTE: gate at s90 uncertain — validate in-game; see _act4-trigger-placement-map.md】

> **PROMPT**: He looked at you.
>
> SOFIA: He looked *through* me. ...There's a difference. I've had all kinds of people look at me — the bad kind, the want-you-dead kind, the want-you-in-their-bed kind. This is the first time something looked at me and decided I don't exist. Not dangerous, not useful, not even amusing. Just... nothing.
> 〔SOFIA：祂看穿我了。…這兩個不一樣。我被各種人看過——壞的那種、想殺你的那種、想把你帶回房間的那種。這是第一次，有東西看著我，然後決定我不存在。不危險、不有用、連有趣都談不上。就是…沒有。〕
>
> └─【追問】PROMPT: Are you alright?
>     SOFIA: ...Not really. But here's the thing — (she breathes, hard, pulls it back) — if a Daedric Prince doesn't think I'm worth noticing, that means I'm not on the menu. And I like not being on the menu. So get in front of me, hero. He wants *you*. I'll be right here. Got your back. Obviously.
>     〔SOFIA：…不太好。但你知道嗎——（她深吸一口氣，硬撐回來）——一個魔神覺得我不值得注意，代表我不在祂的菜單上。我喜歡不在菜單上。所以走到我前面去，英雄。祂要的是你。我就在這裡。罩著你。顯然。〕

---

## 4-F｜冷港地點感想

【VIGILANT】冷港各地點；久待後的絕望感累積。
【類型】在場·環境吐槽·玩家可問（sayOnce+GLOB）
【Gate】GetStageDone(zzzCHMQ00 0x12F24E, 10)==1 + GetInWorldspace(0x06D275)==1

> **PROMPT**: Thoughts on this place?
>
> SOFIA: It gets to you. Not all at once — it's gradual, like a really boring inn that's slowly on fire. The grey. The nothing. The way time moves like it's given up. ...I'm not scared. I'm irritated. Those are different things. ...Mostly different things.
> 〔SOFIA：這地方會鑽進你心裡。不是一下子——是慢慢的，像一家很無聊的旅店慢慢地著火。那種灰。那種什麼都沒有。還有時間移動的方式，好像它已經放棄了。…我不是害怕。我是煩。這兩個不一樣。…大部分不一樣。〕

---

## 4-G｜Karma 結局分歧

【VIGILANT】冷港終章，Karma 系統決定結局方向。
【類型】在場·玩家可問（sayOnce+GLOB，Karma 分歧）
【Gate (shared)】GetStageDone(zzzCHMQ00 0x12F24E, 999)==1
【NOTE: end-stage for MQ00 uncertain; fallback GetQuestRunning(MQ00)==0 also valid; validate in-game】

---

### 4-G-1｜高 Karma 結局（Karma >= 10）

> **PROMPT**: We made it out.
>
> SOFIA: Hm. We killed a Daedric Prince. Technically. Or we... convinced him to stop? I'm still not sure what we did. But we walked out. ...You know what the worst part is? I'm going to have to find a new thing to complain about. This was a pretty good thing to complain about.
> 〔SOFIA：嗯。我們打敗了一個魔神。技術上。或者說我們……說服他停下來？我還是不太確定我們做了什麼。但我們走出來了。…你知道最慘的是什麼嗎？我得找個新東西抱怨了。這個抱怨起來還挺夠格的。〕

---

### 4-G-2｜低 Karma 結局（Karma < 0）

> **PROMPT**: Was it worth it?
>
> SOFIA: Don't ask me that. ...No, I mean it, don't. Because I'll say something honest and then we'll both feel weird about it. ...You made choices in there. Some of them were — look. We're out. That's the only part I'm keeping score on right now.
> 〔SOFIA：別問我這個。…不，我是認真的，別問。因為我會說實話，然後我們兩個都會覺得很奇怪。…你在裡面做了一些選擇。有些選擇是——你看。我們出來了。這是我現在唯一在算分的事。〕

---

### 4-G-3｜中性 Karma 結局（Karma 0–9）

> **PROMPT**: How do you feel about what we did?
>
> SOFIA: I feel like we survived something we probably shouldn't have, made some calls I wouldn't want to explain at dinner, and somehow came out the other side with most of our limbs. ...Not bad. Honestly. For us? Not bad at all.
> 〔SOFIA：我覺得我們撐過了一件我們大概不應該撐過的事，做了幾個我不會想在晚餐桌上解釋的決定，然後不知怎的從另一頭走出來，肢體大致齊全。…不壞。說真的。就我們而言？一點都不壞。〕

---

*待補：① MeQ11/MeQ12（After the Storm/Last Night）確認保持沉默；② 若日後確認 MeQ08 無名吟遊詩人→Act3 管家連線，再補一條可選反應。*
