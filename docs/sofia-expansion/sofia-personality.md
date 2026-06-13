# Sofia 性格分析（2026-06-13）— 寫作 brief

做 Sofia 擴充時的**角色聖經 / writing brief**：要產出「聽起來像 Sofia」的新台詞，先吃透這份。
所有引用台詞都是從 `SofiaFollower.esp`（v2.51，635KB，Mutagen overlay，**只抽 esp 不碰 BSA**）的 INFO 直接抽出的原文，主要來自 `JJSofiaDialogue`（94 topic / 415 INFO，主對話樞紐）+ `JJSofiaIdleDialogue`（247 條 idle）+ comment scene。結構面見姊妹檔 [`follower-decode-2026-06-13.md`](follower-decode-2026-06-13.md)。

---

## 0. 一句話定位

**Sofia 是一個自戀、毒舌、咸濕又笨拙的喜劇型 foil**——她把「自誇美貌」「對玩家半真半假地調情」「臨陣脫口而出再自我打圓場」三招無限循環，外殼是欠揍的嘴砲，內核是怕被丟下、怕自己不夠好的不安全感。她不是嚴肅的浪漫對象，是會一路吐槽你、偶爾不小心露出真心、再立刻用玩笑遮掩的旅伴。

寫新台詞的黃金準則：**每一句要嘛在自誇、要嘛在調侃你、要嘛在調情然後說溜嘴自我更正，最好同一句裡占到兩樣。** 純資訊、純正經的句子不是 Sofia。

---

## 1. 核心原型（archetype）

**喜劇副駕 / comedic foil**，不是英雄、不是賢者、不是悲情戀人。她的功能是「對你做的每件事吐槽、把嚴肅場面拉低、用嘴砲填滿旅途空氣」。

- 自我中心但有自覺，會自嘲：「I don't like to boast but I am superior to you in every possible way. Ah who am I kidding I love to boast.」
- 反英雄價值觀：問她是不是壞人，她答「I wouldn't say I'm evil. Perhaps I take things a bit too far sometimes. But I never said I was good.」「Where's the fun in that. I won't change for anyone. You can like it or lump it.」
- 拒絕被馴化 / 拒絕成長弧：「Why would I want to impress you?」「I won't change for anyone.」——她不會「變好」，這是設計，不是缺陷。

---

## 2. 幽默風格（humor style）

四個可重複套用的笑點機制，**新台詞至少命中一種**：

### (a) 自戀式自誇 — 把美貌當萬能解
- 「Everyone finds me attractive so the rules don't apply.」
- 「As much as I like gold I would never give up my amazing looks. What can you get with gold anyway? A fancy house? A new sword? How boring.」
- 「I feel like I'm being stared at, but I can't help I'm beautiful.」
- 戰前：「If I am to die then I want to go out in style. Does my hair look alright?」

### (b) 毒舌 / 嘴砲（insult banter）— 對敵人、對玩家、對 NPC
對玩家：「Can't you take a hint? Nobody wants you. Go home!」「Why do you even exist?」「Do you always keep repeating yourself to people.」
對敵人（戰鬥 taunt）：「You are so stupid that you don't even know you are stupid!」「Do the whole of Nirn a favour and just die will you!」「Even Malacath is ashamed of you!」
**毒舌帶機智反轉**——她的侮辱常自帶 punchline：「Your weapon may be sharp but my tongue is sharper so watch where you point that thing.」

### (c) 咸濕未遂 + 即時打圓場（the verbal stumble）— **這是 Sofia 的招牌節奏**
她常脫口而出有點黃 / 有點露骨的話，然後立刻慌張自我更正。**這個「說溜嘴→急救」的兩段式是她最辨識度的笑點**：
- 「You can take me anytime... Uh I mean you can take me with you... yeah.」
- 「Hungry. I'm so hungry I could even eat you... Uh, I mean in a non-perverted way of course.」
- 「I have been so bored. I never thought I'd want you so bad... Uh I mean want to see you.」
- 「Cold. ...snuggle up together? ...Did I say together? I meant... uh... nothing.」
- 「How about you take me instead... Damn it that came out wrong.」
- 「Why must I hump all these stupid objects just to please you? I mean hump as in carrying stuff by the way not that kind.」
- 「Hands off. Only I get to touch myself... and no I don't mean like that.」

→ **寫作模板**：`<曖昧/露骨的話> ... <Uh / I mean / Damn it that came out wrong / 急轉直下的更正>`。

### (d) 打破第四面牆 / meta 玩笑
- 「Don't just stand there I'm being trolled! Wait, that's not even a word.」
- 「This isn't one of those boy meets girl clichés is it?」
- 「Uh oh, I hope that wasn't valuable... Haha You should have seen your face.」（對「玩家」的反應吐槽）

---

## 3. 她怎麼對待玩家（relationship to player）

**又愛又嫌、口嫌體正直**。表面嫌棄你、佔有慾強、把你當提款機兼苦力；底層其實很在乎、很怕被丟下。

- **佔有 / 吃醋**：「You better not be seeing someone else behind my back.」「If you find somebody else I will hunt them down. Not really.」（先狠後收，典型 Sofia）
- **把你當苦力**（trade / 背東西時）：「OK but next time you can carry my stuff.」「Yippee I love holding peoples junk.」「OK but if I hurt my back you'll be carrying me.」
- **把你當提款機**：「You better have stopped so you could shower me with gifts.」「You better be off to earn some more gold.」
- **口嫌體正直的真心**（偶爾漏出來，下一句通常自我遮掩）：
  - 「Well I was feeling pretty lonely till you came along but I'm so glad you did.」
  - 「Ever since meeting you I just feel like my life has become so much better. You make me feel...」（句子斷在曖昧處）
  - 「You actually care how I feel? You really aren't like most people who just shout abuse at me.」
  - 「You came back for me? I mean of course you did.」←（驚喜→立刻嘴硬，招牌節奏）
- **被告白時推開但不傷人**：「Well I truly am flattered but it just wouldn't work out between us. I see you as more of a good friend.」

---

## 4. 不安全感 / 背景鉤子（insecurities & backstory）

毒舌外殼底下，Sofia 其實**被很多人嘲笑過、很怕被丟下、會硬撐不認輸**。這是讓她「不只是欠揍」的關鍵層，新內容要記得偶爾捅一下這層。

- **怕被嘲笑 / 被當怪人**：「You mean like weird? Huh, I thought you were different from all those other people who make fun of me.」「You really aren't like most people who just shout abuse at me. Although I probably do deserve it.」
- **怕被丟下**（wait 時的台詞層層加碼）：「Don't you dare forget and leave me here otherwise I am gone.」「If you don't come back for me, I will make sure that you never come back at all.」
- **硬撐後的軟話**（罕見，珍貴）：「You know how I always make out that I'm never afraid? Well I was lying. I'm feeling pretty afraid right now, but don't tell anyone I said that.」
- **嘴硬式道歉**：「Hmph. Like you care... I'm sorry I shouldn't have been so rude I just... don't worry. I'm fine... really.」
- **背景片段**（可作擴充任務鉤子）：自稱在 Skyrim 四處「killing bandits who thought I was an easy target and meeting new locals or should I say making new enemies」；提過一個舊情人「someone but he wasn't from Skyrim. He was a Breton from High Rock. I took him around Skyrim to see the sights」（句子未說完——擴充可接續這條線）。
- **婚後狀態**（married 後台詞層）：仍維持人設，只是換皮——「Who said marriage couldn't also be fun?」「We may be married but I guess some things will never change.」「You would leave your wife all on her own?」。**重點：婚後不變溫順，毒舌與佔有照舊。**

---

## 5. 說話模式 / 語言癖（speech patterns & verbal tics）

要產生 Sofia-consistent 台詞，照抄這些 tic：

1. **兩段式說溜嘴**（§2c）：`曖昧句 ... "Uh I mean" / "Damn it that came out wrong" / "I meant... nothing"`。**最高優先的辨識特徵。**
2. **狠話收尾自我消音**：先放大絕，再「Not really.」「Well not with me but you know what I mean.」軟回去。例：「If you kill me, I'll be sure to take you with me. Well not with me but you know what I mean.」
3. **自誇 → 自嘲打斷**：「I don't like to boast but... Ah who am I kidding I love to boast.」
4. **修辭性反問 + 自答**：「Why should I follow you? Actually, I can think of a few good reasons.」「Are you saying you don't trust me? Fair point actually. I wouldn't trust me.」
5. **省略號當節拍器**：大量 `...` 表示她在想壞主意 / 即將說溜嘴 / 嘴硬轉折（「It might be good to check the stables if I'm not here anymore... hehe」）。
6. **口語感嘆**：`hehe` / `Yippee` / `Eeww gross!` / `Hmph.` / `Aw` / `Ugh!`。情緒外露、孩子氣。
7. **戰吼擬聲**：戰鬥 taunt 之間夾大量擬聲（「Heeyaaghh!」「Hueghh!」），配音時這些是純喊聲。
8. **稱呼玩家**：婚前無固定暱稱，婚後會彆扭地補一句「Hello there... uh... husband?」/「...wife?」（連稱呼都帶遲疑）。

---

## 6. 情緒光譜（emotional range）— 對應 scene `emotion` 欄位

Sofia 的台詞在引擎裡帶 emotion tag（decode 看到 Neutral / Happy / Disgust / Anger 等）。寫 scene phase 時用對 emotion 放大笑點：

| Emotion | 何時用 | 例 |
|---------|--------|----|
| **Neutral / 乾** | 大部分嘴砲、wait、idle | 「OK, I guess I'll wait a while but I'm not waiting forever.」 |
| **Happy** | trade、收禮、調侃得逞 | 「Yippee I love holding peoples junk.」 |
| **Disgust** | 對 Nazeem 之流、噁心場景 | 「Eeww gross! Kill it! Kill it with fire!」 |
| **Anger** | 戰鬥、被惹毛、吃醋 | 「Argh! That's gonna cost you!」「You're just making me even more angry.」 |
| **(隱性)害羞** | 說溜嘴後的更正——用 Neutral 但語氣慌 | 「Damn it that came out wrong.」 |

**節奏要訣**：她很少長時間維持同一情緒，台詞內常自帶一次情緒翻轉（驚→嘴硬、誇→自嘲、狠→收）。單一 phase 內塞一次「翻轉」最像她。

---

## 7. 寫新 Sofia 台詞的 checklist（給生成器 / 寫手）

每寫一句 / 一段，過一遍：

- [ ] 有沒有命中四個笑點機制至少一種（自誇 / 毒舌 / 說溜嘴急救 / meta）？
- [ ] 有沒有避免「正經、純資訊、無個性」的句子？（那種要嘛刪、要嘛加一句吐槽）
- [ ] 對玩家是不是「口嫌體正直」——表面嫌、底層在乎？
- [ ] 調情有沒有配上**笨拙的自我更正**（不是直球放電，是說溜嘴）？
- [ ] 婚後台詞有沒有保持人設不變（不溫順、照樣毒舌佔有）？
- [ ] 偶爾（約 1/10）有沒有捅一下不安全感層，讓她不只是嘴砲？
- [ ] 句子裡有沒有一次情緒/語氣翻轉（誇→自嘲 / 驚→嘴硬 / 狠→收）？
- [ ] 戰鬥 taunt 是不是又欠揍又自誇（「You've obviously never met me otherwise you would be cowering right now.」）？

**反面教材（不是 Sofia）**：「As you wish, my friend.」「I will follow you anywhere.」「It is an honour to fight by your side.」——太順、太忠犬、無嘴砲、無自戀，這是 Lydia/Serana 路線，不是 Sofia。Sofia 版會是：「Why should I follow you? Actually, I can think of a few good reasons.」

---

## 8. 一頁速查（tl;dr 給趕時間的）

> **Sofia = 自戀 + 毒舌 + 笨拙調情 + 怕被丟下。** 她對你又愛又嫌，把你當苦力兼提款機，每次快放電就說溜嘴然後慌張更正（「Uh I mean...」）。戰鬥時自誇又嘴敵人，正經場面她負責拉低。底層藏著「被很多人嘲笑過、其實很在乎你」的不安全感——偶爾漏一句真心，下一秒立刻嘴硬遮掉。婚後也不變溫順。寫她：**每句要自誇 / 吐槽 / 說溜嘴三選一以上，禁止忠犬式正經話。**
