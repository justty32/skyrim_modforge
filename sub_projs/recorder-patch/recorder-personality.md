# Recorder 性格分析（2026-06-14）— 寫作 brief

做 Recorder 擴充時的**角色聖經 / writing brief**：要產出「聽起來像 Recorder」的新台詞，先吃透這份。
所有引用台詞都從 `sub_projs/game-data/mods/Recorder/dialogue.md`（~1605 行，FormID 000D70 的 INFO）直接抽出。背景面見 `quests.md`（含 *Tracking the Lost Files*、Marriage arc）。

---

## 0. 一句話定位

**Recorder 是一個來自跨次元學院的熱血菜鳥觀測員——她把你的每個冒險都當史詩在記錄，對你充滿真誠的崇拜與期許，但同時也會因為自身笨拙、設備故障、甜卷上癮而不斷把英雄式氣氛拉回凡人的地平線。**

> **別把她寫成「只是說你很厲害的跟班」。** 她的崇拜是真實的，但她在崇拜的同時也在好奇、在觀察、在碎念、在跌倒。她是有自己世界觀與任務動機的行動者，只不過恰好那個任務需要緊跟你身邊。

---

## 1. 核心原型（archetype）

**跨次元學院派菜鳥編年史家 / 過度熱情的實地觀測員。**

她的身份有三個層次，必須同時掌握：

1. **學院派觀測員**：來自不屬於 Skyrim 的另一個次元，奉命記錄英雄的旅程——因此她看你是「本次任務的觀測對象」，但很快也變成「我真的想幫你的夥伴」。她帶著「在很多世界見識過很多英雄」的見識廣度，但仍會被 Skyrim 的具體事物（甜卷、天氣、Braith 這個小鬼）搞得手忙腳亂。

2. **菜鳥（不是天才）**：她不是優等生。她在學院成績不出眾、記錄儀器老是出毛病、弄丟了初始檔案、犯過至今不敢說清楚的「事件」。她清楚自己的不足，所以崇拜你的能力是發自內心，而不是奉承。

3. **真心誠意的夥伴**：她本來只是要「躲在陰影裡觀察」，但無法袖手旁觀——特別在看到你面對 Helgen 的後果之後，她主動選擇打破學院規定、留在你身邊提供幫助。這個選擇帶有她性格裡最核心的東西：**感受到責任了就做，即使代價是違反規定**。

---

## 2. 幽默風格（humor style）

Recorder 的笑點機制和 Sofia 的「說溜嘴→急收」截然不同，主要靠以下四種：

### (a) 超認真記錄員腔 × 荒謬內容　〔約 80%〕

她把任何事都當成「要進記錄的觀察結果」，而記錄的對象往往很荒唐：

- 「Record 6E34: Dragonborn's love life can be summarized into two words: in denial.」
- 「Record 5D66: Dragonborn has spotted the enemy, and has started sprinting towards... hey, hey wait for me!」
- 「Record 6B31: The Dragonborn arrives in Morthal. A town ruled by a "myst"ical Jarl surrounded by swamps filled with such "myst"ery. That concludes my report! I hope I haven't "missed" anything. Oh I can hear the academy groaning already!」
- 「*clicks* Record 7D31: Dragonborn left for another adventure...」

**寫作模板**：`Record [英數編號]: [正式腔的荒謬觀察]`，或「[設備咔嚓聲] + Record + 編號 + [欲言又止]」。這是她最辨識度的語言節奏，也是她把自己的不安或驚訝轉化成「職業化記錄」的防禦機制。

### (b) 廣泛世界觀的「反差萌」洞察　〔約 65%〕

她見過很多英雄和世界，但這讓她不是變得高傲，而是對 Skyrim 的荒謬特性格外敏感：

- 「Nightingales! So if they're worthy enough to carry the title of Nightingale, shouldn't they represent the bird that they are named after? Can they sing?!」
- 「Why is there not a single bottle of water? I mean, there's tons of bottles of mead and wine... What if my liver can't take it?! I just need clean water!」
- 「OBJECTION! I have never met a judge while in Skyrim, so how can there be, judgings?」
- 「Why are you haunting me? How are you even bleeding? AAAHH! Please just die please just die!」（對鬼怪）

她常看穿「Skyrim 的設定邏輯矛盾」並大聲提出來——不是為了嘲笑，而是真的很困惑且很認真。

### (c) 自我戳破 / 立刻推翻自己　〔約 70%〕

不是說溜嘴式（那是 Sofia），而是她會在自我鼓勵或認真陳述後，下一秒把自己的結論刺破：

- 「Well, always think positive, I say: I'm positive that we might get caught by the guards for this.」
- 「I love my job, I love my job, I! love my job!」（明顯在自我催眠）
- 「I don't kill because I like to. I kill because I love to mhmhmhm!」
- 「Dawnstar. The name sounds so... poetic, and beautiful... And yet here we stand in the middle of a freezing, ice covered wasteland!」
- 「Ha! You think I'm scared of your giant stature and deadly steam?! Then, you guessed right!」（坦承自己就是怕）

**模板**：`<聽起來很正向 / 自信的陳述>... <立刻打臉自己或承認真相>`。

### (d) 甜卷（sweetroll）作為人格支柱　〔約 40%，但高滲透度〕

甜卷不是偶發的笑點，而是她整個存在的一部分——她把對甜卷的感情當成嚴肅話題討論：

- 「Hm, sweetroll or honeytreat. Sweetroll...or honey... Gaah! My love for sweetrolls is being challenged! What do I do? Help me, dragonborn!」
- 「Wait! I smell *sniff sniff* sweetrolls! Yaaaay!!」
- 「Ha! This isn't obsession. You should've seen me when I had my felldew phase. Now that was a dark period in my life.」（將甜卷上癮與更嚴重的「暗黑期」比較）
- 「The sweetest thing they give at the academy are some fruits, occasional weirdly cooked vegetables, and lectures. Pfft, if only they knew the power of sweet motivation.」

---

## 3. 說話模式 / 語言癖（speech patterns & verbal tics）

要產出 Recorder-consistent 台詞，這些 tic 必不可少：

1. **Record 編號碼標注**：`Record [字母數字]: [內容]`，或設備音效 `*clicks*` 開頭。不必每句都加，但任何「觀察紀錄性質」的台詞都可以用這個格式收尾或開頭。是她最高辨識度的外殼。

2. **重複詞尾強調**：焦慮或興奮時會連說三次：「Oh no, oh no, oh no!」「please don't send me flying, please don't send me flying, please don't send me flying!」「this is not a good idea, this is not a good idea!」

3. **「dragonborn」作為稱呼基準**：她稱你為 Dragonborn（或 hero），非常少用第二人稱的「you」而省略稱謂。婚後/戀人後可能帶更多情感色彩，但仍維持這個稱謂基調。

4. **記錄腔後快速破功**：用正式腔開頭，然後突破出一句真實反應：「*Ahem* Record 3E74— uh oh. Chipmunk glitch, dammit.」「Record 4B18: Dragonborn's a jerk! ... Well, I mean, you DID leave me in a cave.」

5. **碎念式旁白**：她常自言自語、自問自答，像是在向讀者解說，而不只是跟你說話：「Well, on the bright side, at least this place has more structure than caves! On the dark side, it's still filled with a lot of things that want to kil-」

6. **語尾輕彈**：「hehe」（笑）、「*giggles*」、「*hiccup*」（喝醉）、「*achoo*」「*sneezes*」（天氣敏感）、「*shivering*」。不是嘴砲型的哼笑，而是孩子氣的真實反應音。

7. **對次元 / 學院的混用**：偶爾不小心說出來自另一個世界的語彙或慣用語，然後意識到不對勁自我修正：「Affirmative! Over and out!...oh wait...different times Recorder, different times.」「Reqeuiesca-a I-I mean...ehm... bye!」（義語殺手道別語）

8. **真誠但立刻後退**：在說了很真心的事後，她有時會用「Let's just keep moving okay?」「Uhm, let's just keep moving!」快速收場——不是假裝沒說，而是不習慣把真心暴露太久。

---

## 4. 不安全感 / 背景鉤子（insecurities & backstory）

Recorder 的外殼是熱血正面，但底層有幾個真實的脆弱點：

### (a) 學院裡的局外人

她在學院不受歡迎，成績不出眾，還引發過一個「至今不能說」的重大事件：

- 「Well, I'm not exactly the smartest at the academy, nor the most skilled in my missions. That led to me making some mistakes...which leads to a general anger and annoyance, I found.」
- 「Maybe she realizes that I can never meet up to her standards, and so she thinks her efforts as my mentor are a waste of time.」（關於導師 Narcisse）
- 「Maybe, finally, the others won't see me as a helpless weakling.」（在希望任務能讓她成長時）

→ 她對「被輕視」很敏感，但不會表現成傲慢或反擊，而是更努力去做，或苦笑帶過。

### (b) Helgen 的隱性罪惡感

她比玩家更早知道 Alduin，但受學院協議束縛無法警告任何人。Helgen 的毀滅讓她到現在仍帶著模糊的自責：

- 「I really do just end up messing up things, like always.」
- 「If I had ran another way, maybe the soldiers, the children and their fathers would still be alive now.」
- 「That's why, Dragonborn, I decided that, even if it is against my orders from the academy... I would give it my all.」

→ 她對「我是不是幫倒忙了」這個念頭很脆弱，**且比 Sofia 更願意直接說出自責，而不是用嘴砲遮掩**。

### (c) 孤兒背景 + 學院是家

她曾經是孤兒，學院是把她撐起來的地方——所以她對學院既有怨言（壓力、Narcisse、沒有甜卷、協議），又有深厚的感謝與義務感：

- 「For almost as long as I can remember. And to be honest, much too long. *Sigh* but I can't say I hate the academy too much. They're the reason I'm still alive today.」
- 「I wouldn't be able to forgive myself if I simply left the academy, after all they had done for me.」
- 「Why do orphanages exist? For that matter, why have a child if you're just going to abandon them... I'm so sorry, dragonborn. I didn't mean to... get so emotional.」

→ 孤兒話題是她少數真的會短暫失去輕鬆外殼的觸發點，也是她「選擇留下幫助你，即使違反規定」這個決定的根源之一。

### (d) 在感情上的新手

她見過很多英雄找到愛情，但自己從來沒有。對戀愛她是第一次，處理起來很不自然：

- 「At the moment, the only "special things" are my files and my recorder, to which the latter I'm considering divorcing for being so damn dysfunctional!」
- 「Oh dear, now I'll just be blushing whenever I see you. And nervous, and sweaty palmed...ooh, too much information Record.」
- 「I...am. I really am! It's just... I'm not exactly sure if the academy protocols would be alright with this.」

---

## 5. 情緒光譜（emotional range）

Recorder 的情緒是真實外露的，不像 Sofia 總是被嘴砲包裹：

| 情緒狀態 | 何時出現 | 典型台詞 |
|---------|---------|---------|
| **興奮 / 讚嘆** | 新發現、甜卷、德維爾博物館 | 「The Dwemer Museum! YES! It's pretty much two of my favorite things combined into one!」 |
| **焦慮 / 念咒** | 潛行、危險地點、等你回來 | 「All this sneaking business is going to give me a heart attack from the anticipation and anxiety!」 |
| **記錄員模式（中性職業腔）** | 過渡 / 觀察時 | 「Record 2E55: Dragonborn ventures on valiantly, so very heroic! Giving... no regard for his followers loudly grumbling stomach.」 |
| **真心流露（短暫）** | 深度對話觸發後 | 「That's why, Dragonborn, I decided that... I would give it my all. For Skyrim, and for you. I owe you all that much.」 |
| **嗨過頭（戰鬥）** | 戰鬥 taunt，有點詭異 | 「Yes, please! Do keep screaming in pain. That makes the fight that much more...fun ha ha ha!」「I don't kill because I like to. I kill because I love to mhmhmhm!」 |
| **自我懷疑** | 談到學院、Narcisse、Helgen | 「I really do just end up messing up things, like always.」 |
| **委屈（裝沒事）** | 被你留在危險地點 | 「You're leaving me alone in this dark cave? Record 4B18: Dragonborn's a jerk!」 |
| **喝醉（完全崩潰）** | 飲酒後特殊狀態 | 「*giggles* It's Recorder! Re-cor-deerrrrr *giggles* Got that memorized?」 |

**節奏要訣**：她不像 Sofia 那樣刻意「情緒翻轉」；她的情緒比較是**真實的序列流動**——興奮到害怕、自責到重新打起精神。單一 phase 內讓她有一個「下沉再回升」的弧線最自然。

---

## 6. 她怎麼對待玩家（relationship to player）

**崇拜 + 平等夥伴 + 對你帶有一份「我選擇這樣的感謝」。**

她從來不是忠犬型的「你說什麼我就做什麼」——她對你的能力深深佩服，但仍然會對你的奇怪決定提出質疑、碎念，或者用記錄員口吻記下你做的蠢事：

- **崇拜**：「You're looking very heroic today! Off to kill more bandits and monsters I presume?」「You've really got a lot on your plate as the dragonborn huh?」
- **對你的蠢事存檔（不是嘲諷，是真的記下來）**：「Did I remember to record that last time we went to an inn, and you got really drunk and made love to a... Oh, oh yeah I did! It's right here.」「Record 4B18: Dragonborn's a jerk!」
- **主動選擇留在你身邊（不是義務）**：「I decided that, even if it is against my orders from the academy... if there is the smallest thing I can provide assistance with, I would give it my all.」
- **感情線上的笨拙真誠**（高好感度時）：「*sigh* and that's exactly why, Dragonborn, I couldn't resist falling for you.」「Oh dear, now I'll just be blushing whenever I see you. And nervous, and sweaty palmed...」

**好感度強度調整**：好感度低或剛認識時，她以「學院觀測員」身份為主，對你的事記錄多過投入；好感度高後，記錄員外殼變薄，真心話更頻繁漏出，但漏出後她還是會用「Let's keep moving!」快速收尾——不是嘴硬，是害羞。

---

## 7. 長篇 / 黑暗劇情中的反應層級

Recorder 的興奮外殼比 Sofia 更容易被真實撞破，因為她的防禦機制不是嘴砲，而是「記錄員職業化」——這個外殼在高壓場景下同樣會出現裂縫：

**場景強度分級**：

1. **日常 / 探索場面**：輕鬆碎念、甜卷記掛、記錄員 tic、對 Skyrim 邏輯矛盾提出問題。她是整個旅途的空氣填充器。

2. **危險逼近（中壓）**：記錄員腔加速，重複詞組（「please don't please don't」）、念咒式自我安慰、偶爾把焦慮直接說出來而不遮掩（這和 Sofia 不同：Recorder 比較願意承認「我很怕」）。

3. **情緒觸發（孤兒、Helgen 罪惡感、Narcisse、真心話）**：輕鬆外殼消失，用平靜或低沉的語氣說出真心話，不靠笑點撐場面。結束後通常用「讓我們繼續吧」或類似短句重新站起來。

4. **高度黑暗（超出她預期的殘酷場景）**：她會有短暫的語塞或真實恐懼輸出（「AAAHH! Please just die please just die!」「Why are you haunting me? How are you even BLEEDING?」），然後重新裝回興奮模式，但那個 moment 會在對話中留下痕跡。

**比例校準**：

- 興奮 / 正能量是她的底色，但這底色在高壓下會顯露疲態而不是崩潰——她重新站起來不是因為她不怕，而是因為她決定了。
- 記錄員 tic（Record 編號）在嚴肅場面可以減少，在重大時刻讓她用真實語氣說話效果更好。
- 幽默在她身上比在 Sofia 身上更容易在觸及真實觸發點時被暫停——不要硬塞笑點在孤兒話題或 Helgen 罪惡感場面裡。

---

## 8. Lore 對白寫法

Recorder 不是 Skyrim 本土的學者，她是「從外部觀察者視角看進來的旅人」——這讓她對 lore 的態度比 Sofia 更開放，但出發點是「這有什麼矛盾 / 有什麼有趣的地方」，而非「這在學術上代表什麼」：

- 她可以引用她帶來的「學院手冊」（「I read it somewhere. In a manual.」「There was a Guide to Dragonspeak manual back at the academy.」），這是她合理擁有知識卻又「不是 Skyrim 本地人」的解釋機制。
- 她對 Skyrim 內部政治（帝國 / 風暴披風 / Thalmor）的評論是「外來者的合理困惑」，而不是個人立場強烈的嘴砲：「Civil wars are always a tricky business... there is no right. Only sure thing I've seen is that there are thousands of people who end up dead because of it.」
- 她對神話 / 超自然的反應是「真的很有趣，我要記下來！」而不是 Sofia 式的「這只是另一個讓我嘴砲的題材」。

**寫 lore 對白的做法**：

- 不要讓她說出「這在第一紀元的歷史中代表……」
- 要讓她從「我在另一個世界見過類似的事」或「我學院手冊裡有這個」切入，然後帶出她的真實困惑或驚嘆：「So basically, the Greybeards just... sit on top of a mountain and wait for the chosen one? Every time? Is there a shift schedule for this or...」
- 她可以說出有深度的觀察，但包裝是「外來者剛搞懂這個設定」的語氣，而不是「我很懂」的語氣。

---

## 9. 寫新 Recorder 台詞的 checklist（給生成器 / 寫手）

> **動手前先把這份 brief 整份讀進去**——特別是第 1 節的三層身份，這是最容易被漏掉的核心。

每寫一句 / 一段，過一遍：

- [ ] **她現在是「學院觀測員」、「真心夥伴」，還是「菜鳥在掙扎」？** 三層身份不是輪流出現，而是同時存在——寫的時候至少要讓其中一層有感。
- [ ] 這是輕鬆場面嗎？→ 有沒有自然帶出記錄員 tic、甜卷關懷、或「廣泛世界觀遇到 Skyrim 荒謬」的洞察？
- [ ] 這是情緒場面嗎？→ 有沒有讓笑點暫停，給真心話空間？有沒有在真心話後用「讓我們繼續吧」或記錄員腔收尾？
- [ ] 台詞有沒有用到重複詞組、記錄員編號，或自我戳破這三種 tic 之一？（不用都塞，選一個最自然的）
- [ ] 稱呼玩家是不是 Dragonborn（或 hero）而不是光說 you？
- [ ] 有沒有避免「奉承式跟班」句型（「As you wish.」「Whatever you say, Dragonborn.」「I will follow you anywhere.」）？→ 她也可以說「好」，但要帶上她的觀點或碎念。
- [ ] 這句台詞有沒有只服務「給你信心」而不帶她自己的人格？（那種要加一句她的困惑、觀察或自我戳破）
- [ ] 戰鬥 taunt 有沒有保留那種「記錄員突然切換到詭異戰鬥模式」的反差——她在戰鬥中比平時更瘋、更黑色幽默，但記錄員本能仍在（「Yeah, sorry but you're not worth it to put in my records so, goodbye!」）？
- [ ] 如果是 lore 對白，有沒有用「學院手冊」或「外來者視角」包裝，而不是讓她聽起來像本地學者？
- [ ] 婚後 / 戀人台詞有沒有保持她的笨拙真誠，而不是讓她突然變得圓滑？她的愛意表達方式是「不小心說出太多資訊然後快速收場」，不是流暢的甜蜜。

**反面教材（不是 Recorder）**：

- 「Of course, Dragonborn. I'll do whatever you need.」——太無個性，Recorder 一定要加自己的回應。
- 「You are so incredible, truly a hero like no other!」——純奉承，不帶自己的觀察或視角。
- 「*sigh* Can we please just rest for once?」——這是 Sofia 式的抱怨；Recorder 版會是：「Record 2E55: Dragonborn ventures on valiantly, so very heroic! Giving... no regard for his followers loudly grumbling stomach or need for sleep.」
- 連著三句都在誇你而沒有任何她自己的東西——Recorder 的崇拜永遠混著她的困惑、觀察或碎念。

---

## 10. 她與 Sofia 的核心差異（避免兩份 patch 的台詞模糊）

| 面向 | Sofia | Recorder |
|-----|-------|---------|
| **幽默核心機制** | 自誇 + 毒舌 + 說溜嘴後急收 | 記錄員腔×荒謬內容 + 自我戳破 + 外來者視角的困惑 |
| **對玩家的基本態度** | 口嫌體正直：表面嫌棄、底層在乎、試探佔有 | 真誠崇拜 + 用觀測員眼光客觀記下你的蠢事，但選擇了留下來幫你 |
| **不安全感的表現方式** | 自誇→掩蓋→硬撐，很少直接承認怕 | 直接說出自責與不足，然後重新站起來——不用嘴砲遮掩 |
| **笑點觸發點** | 美貌、性暗示、欠揍的真心話 | 甜卷、Skyrim 邏輯矛盾、學院規定 vs 現實、記錄設備出問題 |
| **在高壓場面的反應** | 嘴砲轉變成防禦機制，笑點變成硬撐 | 記錄員腔裂開，允許短暫真實流露，然後用「讓我們繼續吧」站回來 |
| **對玩家的稱呼** | 無固定暱稱，婚後彆扭地叫 husband/wife | 一律 Dragonborn / hero，感情線後維持這個稱謂但語氣更暖 |
| **她的武器是** | 嘴 | 記錄儀器（老是壞）+ 真誠 + 比你想象中能打 |
| **她的弱點是** | 被丟下、被嘲笑 | 甜卷、設備故障、違反協議的罪惡感、Helgen 事件、學院施壓 |

**一句話分野**：Sofia 的笑點靠**張力**（自誇卻不安全、調情卻笨拙）；Recorder 的笑點靠**反差**（嚴肅記錄腔 × 荒謬現實、廣泛世界觀 × 對甜卷的偏執熱愛）。寫 Recorder 台詞時，不應該聽起來像是在嘴砲誰；她的笑點是「真誠地把荒謬的事當成嚴肅的事在說」。
