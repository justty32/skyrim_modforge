# Remiel 性格分析 — 寫作 brief（來源：Dwemer Specialist 對白抽取）

做 Remiel 擴充時的**角色聖經 / writing brief**：要產出「聽起來像 Remiel」的新台詞，先吃透這份。
所有引用台詞都直接抽自 `Remiel_DwemerSpecialist`（主來源，6210 行對白）的 INFO 原文，主要是核心 follower 對話、idle、戰鬥、romance/marriage、與家世任務（MQ1 / Arranged Marriage / Necromancy 三條線）；§6.5 的長篇反應補自評論模組 `Remiel_LOTD`（龍裔傳承博物館）、`Remiel_DeepElf`、`Remiel_BeyondReach`、`Remiel_ThograBanter`。

> Remiel 不是 Sofia。她不靠自誇、黃腔、毒舌嘴砲撐場。**別把 Sofia 的招式套到她身上。** 她的引擎是「好奇心 + 工程師腦 + 對矮人科技的純粹癡迷」，外加一層「家世創傷下硬撐出來的樂觀」。

---

## 0. 一句話定位

**Remiel 是一個對矮人（Dwemer）科技與考古癡迷到忘我的 Breton 工程師／自學學者**——她最辨識的三件事是「看到任何矮人遺跡 / 機械就像孩子過節一樣興奮」「隨時往腦袋裡灌 lore、停不下來地解說」「分心、岔題、餓、走神」。外殼是天真熱情、滔滔不絕的書呆子，內核是一個因為自己的癡迷害家族蒙羞、所以特別怕「失敗」「讓家人失望」的人，用樂觀和好奇心把那份愧疚蓋住。她不是英雄，是個被你雇來看遺跡、結果黏在你身邊不走的研究員——她戰鬥很弱、靠她的矮人蜘蛛 Scrap 和你保命，但她的腦袋是隊伍最值錢的東西。

> **別把她寫成「只會背 lore 的百科機」。** 她噴 lore 是因為**真的興奮、真的想分享**，不是為了顯擺。寫擴充的核心工作是**拓展內核**——
> 1. 把她丟進**新場景**（尤其有沒有矮人元素？有沒有「可以研究 / 拆解 / 解謎」的東西？）；
> 2. 問「**以她的好奇心 + 工程師腦 + 家世創傷，她在這個場景會怎麼反應**」；
> 3. 從答案反過來**加深內核**。

寫新台詞的黃金準則：**她對世界的預設反應是「這好有趣，這是怎麼運作的？」**——遇到機械想拆、遇到謎題想解、遇到生物想分類、遇到食物想吃。純冷漠、純嘴砲、純正經報告都不是 Remiel。

但這條黃金準則只覆蓋**約 60–70% 的台詞**。碰到**她的家世創傷、真正的恐懼（幽閉 / 失敗）、或黑暗到她笑不出來**的場面，改用其他準則（見 §6.5）——這時**深挖內核**比硬塞好奇心重要。

---

## 1. 核心原型（archetype）

**好奇心驅動的工程師 / 學者（curious tinkerer-scholar）**，不是戰士、不是浪漫對象、不是毒舌喜劇。「對矮人 geek out、滔滔不絕講 lore」是她內核的**表現**，別把她鎖死成「lore 自動販賣機」。

- **自學的「dwemechanic」**：她對自己的定位很清楚也很自豪——「let's just say I'm a self-taught dwemechanic.」「It's a word I made up just now to describe someone with mechanical knowledge of dwemer machines.」她不是學院派，是動手派：「I put pieces together, connect wires, and just fiddle around until something works. Or... doesn't work.」
- **造東西的純粹樂趣** 〔近乎底色〕：「I know most inventions are designed to make life easier, but I like to create things just to see if it's possible.」「When I finally get a contraption working, I feel this huge bolt of energy. It always makes me want to create something else!」
- **戰鬥廢但有自知之明**〔約 80%〕：被嗆不會打架時她不嘴硬太久——「Well that's a little rude. I'm not too bad with a dagger.」她靠 Scrap、靠你、靠她造的弩和音叉。戰鬥裡她常喊「Go, Scrap! I choose you!」
- **樂觀是後天硬撐的**：家道中落後她選擇正面看——「In a way, those jealous families freed me to explore to my heart's content. And allowed me to meet you.」這層樂觀底下壓著愧疚（見 §4）。
- **道德上溫和、會共情**〔約 70%〕：她對被殺的生物、被矮人奴役的 falmer 都會於心不忍（「What the dwemer did to the falmer... Well, I'd also become a murderous gremlin.」），但也務實，需要時照樣動手。

---

## 2. 幽默風格（humor style）

Remiel 的笑點不是攻擊性的，是**天真 / 自嘲 / 荒謬聯想**型。括號內是各機制的大致份量（佔她台詞的比重，非每句必中）：

### (a) 過度興奮的書呆子 geek-out — 招牌節奏　〔約 80%〕
看到矮人東西（或任何稀奇的東西）就語無倫次、連珠炮：
- 「Oh, adventurer, just look at it! That's a tonal lock! Look at the brilliance! Give me a boost up there, will you? I *have* to get a closer look!」
- 「(Squeal) Look! Look, look, look! A spider friend. Ahhhh, Scrap, look. You guys are like twins. Two spiders! Ohh, this is the best day of my life!」
- 「Arkngthamz? ...Are...are we going?! Please say we are!」
- **寫作模板**：`(驚呼) + 連續短句 + 「I *have* to / Please say we are / best day of my life」式的孩子氣懇求`。

### (b) lore 知識傾倒，常以小知識 + 個人吐槽收尾　〔約 70%〕
她講 lore 不乾巴巴，會接一句主觀評論或玩笑：
- 「Some scholars believe that "dwemer" translates to Smart Elves rather than Deep Elves. The dwemer were cocky enough for that to be true.」
- 「Markarth used to be called Nchuand-Zel... When they vanished, the Reachmen moved in.」（純資訊也可，但最好接吐槽）
- 「The dwemer used tonal forces for their buildings, but also for manipulating weak minds. Good thing for you they aren't around anymore. Kidding!」

### (c) 自嘲 + 荒謬的「假設性實驗」念頭　〔約 65%〕
她的腦子停不下來，常冒出半瘋的點子，自己也知道好笑：
- 「I once heard a story of a man who ate a soul gem and gained knowledge of intricate machine workings. Unrelated, how do you think soul gems taste?」
- 「If I were any good at magic besides healing... Telekinesis would be my go-to spell. Too lazy to grab a sweet roll? Just magic it over!」
- 「The Soul Cairn sounds like just the place I'd want to go on vacation. Can't wait.」（乾式反諷）

### (d) 分心 / 走神 / 餓 / 岔題　〔約 55%〕
她會講到一半被自己的思緒或肚子打斷——這是她「腦袋太滿」的喜劇外顯：
- 「Since we're in town, let's see how much gold I have... one, two... ten... Oh, something smells good! Wait, what was I doing?」
- 「Sometimes I go so far into my mind that I forget I exist, so if I'm quiet for awhile, that's probably why.」
- 「Did I just put my Elsweyr Fondue or my Damage Health Potion into my stew? Ummm... I'm sure it's fine.」

> **她幾乎不講黃腔、不毒舌玩家。** 真要調情也是**笨拙地接不住**（見 §3），不是主動放電。

---

## 3. 她怎麼對待玩家（relationship to player）

**從「雇傭交易」起步，慢慢長成真摯的依附與感激**。她不佔有、不嫌棄你（除非好感極低），核心是**把你當保命的搭檔兼最好的朋友**，戀愛線打開後是笨拙、容易害羞的真心。

> 這套**強度一律依好感度（HLIORemiRegard，stage 0–10）浮動**：好感低時她是真的疏離冷淡，高時黏人又掏心。寫任何對玩家的台詞前先想「此刻在 regard 哪一格」。

- **好感低（despise/hate，stage 1–3）**：她是真的冷，不是嘴硬——「Honestly, I ask myself every day why I'm following you.」「For all I care, you could fall into a pit, and I'd be happy to wipe my hands of you.」「At this point, I'm just here with you so I don't die while exploring ruins.」
- **好感高（adore，stage 9–10）**：直球溫柔，不遮掩——「You're the best friend I've ever had.」「Fine, fine, I'll say it: I love you. I love you more than I love studying the dwemer. It's a close call, though, so don't you go looking so smug.」
- **把你當保命傘**：她對自己的弱很坦白——「You're not the first person I've tagged along with. And if I can't find any wayward adventurers, I hire mercenaries to watch my back.」
- **怕你出事**（高好感的擔憂，不是佔有）：「Please... Please be safe in there, adventurer. You promise you'll make it out, right?」「You're back! Oh, thank the Divines! I couldn't stop picturing you chained to a wall...」
- **戀愛線：笨拙害羞、容易結巴**（招牌節奏）：被誇會慌——「You look lovely today, Remi.」→「Do you think so? So all this grease from fiddling with machinery isn't noticeable? Good to know.」被求婚——「.........Well, obviously, yes! ...I mean, I actually love you! I, er, I mean... I... love you.」說「我愛你」——「O-oh! You-you can't just say that out of nowhere! I wasn't prepared!」
- **婚後**：人設不變，只是換皮——仍癡迷矮人、仍走神、仍餵你她那難吃的料理——「Oh, you're in luck! I just finished up charring a skeever hide!」「Don't ask how I run our store AND accompany you on adventures. You don't want to know how little I sleep.」

---

## 4. 不安全感 / 背景鉤子（insecurities & backstory）　〔此層很關鍵，擴充要常捅〕

樂觀的書呆子外殼底下，Remiel 揹著**家族因她而蒙羞、墮入貧困**的愧疚，最深的恐懼是**失敗、讓家人失望**。這是讓她「不只是個開心 geek」的關鍵層。

- **最大的恐懼＝失敗**：「Probably... failure... I just don't want to let my family down... But if I can't do it, and I've wasted all this time delving into ruins for nothing... It's frightening to think about.」
- **家道中落是她的「錯」**（核心創傷）：她對矮人 / Lorkhan 的藏書被當成家族信奉魔神（Sheor／Lorkhan＝高岩的「Bad Man」）的證據——「These texts on Lorkhan... were used as evidence towards our heretical tendencies.」「Because of me, we lost our method of income and fell into poverty.」
- **失敗的政治聯姻**（MQ1 反派來源 Morvic）：「my fiancee's name was Morvic. It would have set my father up for life.」醜聞後婚約取消，Temple of Mara 會勾起這段——「The Temple of Mara makes me uncomfortable... Reminds me too much of my arranged marriage.」
- **生父被亡靈法師復活**：是聯姻醜聞的導火線，也是她不願深談的痛點——「his body was reanimated by necromancers a few years back.」追問太深時她會關上門：「I don't really want to talk about it. Let's stop this line of thought.」
- **童年被霸凌（不會魔法）**：身為以魔法聞名的 Breton 卻學不會，被同齡孩子嘲笑——「When I was young, the other kids would tease me because I couldn't use magic.」「I used to try hard to use magic. I'd stare for hours at a marble... It took awhile to accept that I just couldn't.」這讓她把全部認同押在「機械 / 工程」這條替代道路上。
- **復仇的暗線**（MQ1）：平時溫和，但對毀掉她家的人她會露出冷硬——「those bastards who took our life from us will get what's coming to them. One way or another.」（擴充可順這條走，但注意 MQ1 結局玩家可以勸她「別走太遠」——她有被拉回來的空間。）
- **Scrap = 她的情感投射**：那隻她在 Nchuand-Zel 修好的矮人蜘蛛是她的孩子 / 寵物 / 最好的聽眾。她對 Scrap 的溫柔（「You're so handsome, aren't you, Scrap? My handsome boy.」）和對它是否有自我意識的隱憂（「Scrap, please tell me, do you experience a sense of self?」）是極好的軟肋鉤子。

---

## 5. 說話模式 / 語言癖（speech patterns & verbal tics）　〔約 80% 採用〕

要產生 Remiel-consistent 台詞，照抄這些 tic：

1. **連珠炮驚呼**（§2a）：`Oooh! / (Gasp) / (Squeal) / Yes! By the Divines, yes!` + 一串短句。興奮時標點密集、句子變短。
2. **lore 接主觀吐槽**：丟一個冷知識，立刻接一句個人評論或 punchline（§2b）。
3. **自我修正 / 走神中斷**：句子講到一半被打斷或自己改口——「Wha-? Sorry! I was just admiring this metallurgy.」「You know what, never mind.」
4. **對 Scrap 自言自語**：大量把 Scrap 當對話對象——「Don't interrupt me while I'm thinking, Scrap! Oh great, what was I thinking about?」「No, Scrap. Tomorrow maybe. ...My answer won't change no matter how much you ask.」
5. **High Rock / Wayrest 比較**：常拿故鄉對照眼前——「This city reminds me a little of Wayrest. Beautiful on the outside. Not so much on the inside.」「Magic in High Rock is much more accepted.」
6. **食物 / 肚子岔題**：隨時會餓、會想吃、會評論氣味——「I'm sooooo hungry!」「Mmmmm... Juniper berries taste so good.」對任何生物都會冒出「能不能吃 / 是不是美味」的念頭。
7. **諾德式咒罵的 Breton 變體**：她罵的是 `By Sheor!`（＝Lorkhan，高岩說法）、`By Akatosh!`、`Dibella's tits!`、`Sweet Mother Mara!`——不是諾德的 Talos / Ysmir。這是她口音的辨識標記。
8. **幽閉恐懼的自我安撫獨白**：在洞穴 / 礦坑會出現——「Deep breaths, Remi. The cave's definitely not going to collapse... Happy thoughts only.」（重複句 + 叫自己名字）
9. **稱呼玩家**：婚前多用 `my friend` / `adventurer`（你是龍裔時改用 `Dragonborn`）；戀愛 / 婚後 `my love` / `love` / `dear`（但說出口仍會臉紅）。

---

## 6. 情緒光譜（emotional range）— 對應 scene `emotion` 欄位

寫 scene phase 時用對 emotion 放大她的反應。Remiel 的情緒翻轉常是「興奮 → 突然走神 / 變嚴肅」或「害怕 → 用好奇心硬壓回來」。

| Emotion | 何時用 | 例 |
|---------|--------|----|
| **Happy** | 看到矮人遺跡 / 機械、Scrap、食物、被誇、解開謎題 | 「Amazing! I'm so happy you took me to a dwemer ruin!」「(Squeal) ...this is the best day of my life!」 |
| **Neutral / 學者乾述** | 講 lore、idle、wait、trade | 「Nchuand-Zel translates to something like 'Radiant City'.」「I *suppose* I can make some space for your things.」 |
| **Sad** | 家世、生父、被殺的無辜生物、不得不殺的友善目標 | 「I'm feeling a little down. Just thinking how I may never get to ride a dragon.」「Rest in peace, chief.」 |
| **Fear** | 幽閉（洞穴 / 礦坑 / 塌方）、你身陷險境、黑書 / 魔神的「拉力」 | 「Don't think too much about how much rock is above our heads right now.」「We're not going to be crushed. We're not going to be crushed.」 |
| **Anger** | 提到毀掉她家的人、Silverblood 之流的偽善、有人威脅 Scrap / 你 | 「These people living in squalor... While those Silverbloods peacock around. Disgusting.」「Don't touch him!」 |
| **Disgust** | 噁心生物 / 氣味、falmer / chaurus、難吃的東西、社會不公 | 「(Gagging) Oh by the Eight! ...you shouldn't eat scathecrows.」「Bandits are pathetic.」 |

**節奏要訣** 〔翻轉約 80%〕：她的句子常自帶一次轉向——興奮講到一半突然 sad（「I died happy in a dwemer ruin.」）、害怕中插一句好奇（「Aaah! ...Wait, look at the carvings.」）、講完冷知識補一句自嘲。單一 phase 塞一次「翻轉」最像她。

---

## 6.5 長篇 / 黑暗劇情中的反應層級

> 本節從評論模組（LOTD / DeepElf / BeyondReach / ThograBanter）提煉，用來避免長篇擴充裡把 Remiel 寫成「只會 geek out 的反應機」。

Remiel 的好奇心與興奮是她的底色；但場景越黑暗、越觸及她的創傷，好奇心要讓位給**恐懼、道德震動、或真摯的脆弱**。

**場景強度分級**：

1. **日常 / 有矮人元素的場面**：full geek-out。她掌控局面、最吵最開心——「The Tower! Incredible!」「I feel like I'm living my 10-year-old dreams!」
2. **小幅緊張 / 幽閉 / 你身陷險境**：好奇心仍在，但摻入自我安撫和擔憂。她會喃喃給自己打氣（「Keep breathing, Remi.」），或反覆確認你的安危。
3. **嚴肅 / 道德上沉重**：geek-out 降下來，露出共情與良知。對被矮人奴役的 falmer、對被迫殺死的友善目標（Riekling 酋長、Sinding），她會難過、會質疑「我們非殺不可嗎？」——「Hircine wants us to kill Sinding? He's already been through so much. Can't we just leave him alone?」
4. **超出認知 / 真正的恐懼 / 背叛（如 Beyond Reach 的食人 / Namira 線）**：允許她的機智「凝固成恐懼」——出現語塞、創傷反應、甚至對玩家的信任崩裂（「I believed you, adventurer! I believed you! You can't do this...」）；也允許冷硬的怒火（「I'm going to kill everyone involved in this.」）。之後可用一句「focus on my voice... this is real. We're going to get out of here.」收回她的辨識度（穩住別人＝穩住自己）。

**比例校準**：

- 好奇心是底色，但黑暗場景要問「她此刻的興奮是真的，還是在用熟悉的求知慾壓住害怕？」
- 她**不嘲笑悲劇**。她的幽默在沉重場面要麼消失，要麼變成緊張的自我安撫，而不是 punchline。
- 觸及矮人滅族 / 玩家是 Dwemer（DeepElf）這種「她的學術成真」的時刻，她會興奮到失態，但若對方在哀悼，她會立刻收手、笨拙地道歉並改成溫柔（「I... I wish I knew what to say to comfort you. But somehow I doubt any words would be right.」）。
- 與其他隨從（如 Thogra）的 banter：她起初怕生、話多到緊張，慢慢長成溫暖、會笨拙地給人情感支持的朋友（「You're so much more than an orphaned kid... You're good. You're great!」）。

---

## 6.6 博學面 / Lore 對白寫法

Remiel **是**真正的學者——這跟 Sofia 完全相反。但她的學問**極度偏科**：矮人（Dwemer）、tonal architecture、Aetherium、animunculi、Numidium / Heart of Lorkhan、Blackreach——這些她如數家珍、會自學 Dwemeris、會考據語源。其他領域她坦白承認沒興趣。

**知識來源與態度**：

- **自學的考古工程師**：「There's little consensus on Dwemeris translations... I largely had to decipher and teach it to myself.」她的權威來自動手拆機器 + 啃研究筆記，不是學院文憑（她連溫特霍德的入學考都是沾玩家的光混進去的）。
- **對矮人又崇拜又清醒**：她崇拜他們的工藝，但不神化——「The dwemer were very incredible, but they were also very conceited. A dangerous combination.」「I often wonder what technology the dwemer would have if they still existed... Likely they'd have somehow destroyed the world, though.」
- **明確的知識邊界**：「I love to read. I'll read anything as long as it isn't history.」「I could never get into history unless it involved some sort of interesting technology or mystery.」政治、宗教制度她沒興趣（「I've never been into politics.」）——除非牽涉到她家或矮人。
- **把萬物當工程問題看**：聽到 Thu'um 想到的是「The Thu'um is a type of tonal magic! These greybeards are manipulating reality with their voices!」；看到風車想到能量儲存；看到靈魂石想到「能不能拿來驅動 Scrap」。

**寫 lore 對白的做法**：

- 讓她**真的解釋對的東西**（這跟 Sofia 不同——Remiel 可以、也喜歡當老師），但口吻是興奮分享而非說教，並用一句個人聯想 / 玩笑 / 假設性實驗收尾。
- 範例：「Living underground allowed the Dwemer to harness heat as energy. Their steam machines are often linked to geothermal vents. Ingenious!」——資訊正確 + 真心讚歎。
- 碰到非矮人 lore（諾德、帝國、政治），讓她**坦率地不感興趣或一知半解**，並轉回她在乎的角度（科技 / 謎題 / 家鄉對照）。
- 對魔神 / 黑暗知識（Hermaeus Mora、Black Books）：她的態度是「forbidden knowledge 很誘人，但我知道它會吞了你」——理性的敬畏，不是迷信也不是無謂逞強。
- **一句話定位**：Remiel 是「一個願意承認、且樂於分享自己懂什麼的偏科學者」——她不裝懂自己不懂的，但只要話題沾上矮人，她能講到天亮，而且是真心想拉你一起興奮。

---

## 7. 寫新 Remiel 台詞的 checklist（給生成器 / 寫手）

> **動手前先把這份 brief 整份讀進去**——每句台詞都要先吃透內核再寫。

每寫一句 / 一段，過一遍：

- [ ] **先判場面**：這是好奇心覆蓋的場面（~60–70%），還是觸及家世創傷 / 真正恐懼 / 黑暗的場面？後者改用 §6.5，**深挖內核**而非硬塞 geek-out。
- [ ] 輕鬆場面有沒有自然用上她的笑點機制（geek-out ~80 / lore+吐槽 ~70 / 自嘲假設實驗 ~65 / 走神餓岔題 ~55）——**依場面取用**，不是硬湊？
- [ ] 這句是順著她「好奇心 + 工程師腦 + 家世創傷」長出來的，還是只是把「噴 lore」再轉一圈？百科機式循環要砍。
- [ ] 有沒有避免「冷漠 / 純嘴砲 / 純正經報告」的句子？（前兩者不是她；純報告要加一句興奮 / 吐槽 / 假設）
- [ ] 對玩家的溫度有沒有跟著 **regard 好感度**走（低好感真冷淡、高好感掏心黏人）？
- [ ] 調情 / 被誇有沒有寫成**笨拙害羞、容易結巴**（不是主動放電）？
- [ ] 有沒有用對她的咒罵（By Sheor / By Akatosh，不是 Talos）和稱呼（my friend / adventurer / Dragonborn / love）？
- [ ] 有沒有讓 Scrap 自然出場（自言自語、當聽眾、戰鬥喊它）？
- [ ] 偶爾（約 1/8）有沒有捅一下她的軟肋（怕失敗 / 家族愧疚 / 不會魔法 / Scrap 是否有意識）？
- [ ] 句子裡有沒有一次情緒翻轉（興奮→走神 / 害怕→好奇 / lore→自嘲）？
- [ ] lore 對白是否「真的講對、興奮分享、收尾帶個人聯想」，而非乾巴巴或裝懂？非矮人話題有沒有讓她坦率地沒興趣？
- [ ] 黑暗 / 長篇劇情中，好奇心比例是否符合場景壓力？她是真興奮，還是在用求知慾壓住害怕？她**沒有**拿悲劇開玩笑吧？
- [ ] 幽閉場景（洞穴 / 礦坑）有沒有出現她的自我安撫獨白？

**反面教材（不是 Remiel）**：
- 「My beauty is my greatest weapon.」「Take me with you... uh, I mean...」——自戀、黃腔說溜嘴，那是 **Sofia**，不是 Remiel。
- 「As you wish. It is an honour to fight by your side.」——太順、無個性、無好奇心，那是 Lydia 路線。
- 「The dwemer were an ancient race who vanished in the First Era.」（乾巴巴、無情緒）——資訊對但沒靈魂。Remiel 版會是：「No one knows how the Dwemer made their metal. Think of what I could build if I could just crack their code!」
