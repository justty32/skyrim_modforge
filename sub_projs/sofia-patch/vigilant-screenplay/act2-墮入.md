# 第二幕：墮入 — Windhelm 地下 → Lamae

> 劇本（雙語版）。共用原則見 [`README.md`](README.md)。觸發地圖見 [`_act2-trigger-placement-map.md`](./_act2-trigger-placement-map.md)。
> Stage gate 來源：PSC 碎片逆向（2026-06-14）。Beat 2-G / 2-H 夢境入場為**DEFERRED**，本版實作為出夢後玩家話題。

---

## 2-A｜Windhelm 衛兵來尋，開啟第二幕

**【VIGILANT 劇情】** 休息沒多久，Windhelm 衛兵過來，第二幕開始（Mq01 啟動，s0 running，Courier 還沒找到）。
**【類型】** 玩家可問
**【gate】** Mq01 (0x038524) running + s0 done + s10 not done

> **SOFIA**：...我才剛閉上眼睛。
> 〔...I'd just closed my eyes.〕
>
> 你看到那個衛兵的表情沒有？那是「我們有大麻煩、需要兩個傻子去處理」的表情。我認識那種表情，因為我在鏡子裡對自己做過。
> 〔See that guard's face? That's the "we have a serious problem and need two idiots to deal with it" face. I recognize it. I've made that face at myself in the mirror.〕
>
> 好吧。我的床，再見了。
> 〔Fine. Goodbye, my bed.〕

---

## 2-B｜書本與 Sofia 的學識面

**【VIGILANT 劇情】** 玩家讀書推進任務（Mq01 s20 搜索地城中）。Sofia 坦承自己讀過相關資料。
**【類型】** 玩家可問（讀書後）
**【gate】** Mq01 s20 done + s50 not done

> **SOFIA**：那本書——我讀過。別這麼驚訝，我不是只會照鏡子的。
> 〔That book — I've read it. Don't look at me like that. I do more than stare at my reflection.〕
>
> 很久以前。在我還會為了「知識」翻書、而不是為了「報酬」的那個年紀。裡面說的那些東西，要是有一半是真的……我們接下來要碰的，比我想的還糟。
> 〔A long time ago. Back when I read things for knowledge rather than gold. If half of what's in there is accurate... what's ahead of us is worse than I thought.〕

---

**（追問）玩家**：你以前喜歡讀書？
〔You used to be a bookworm?〕

> **SOFIA**：「喜歡」這個詞太強了。我讀書，然後我學到大部分書都沒告訴你真相。這讓我讀更多書，想找到例外。最後我找到的結論是：去親身碰一碰比讀一輩子有效。
> 〔"Like" is a strong word. I read books. Then I learned most of them lie to you. So I read more, looking for the exceptions. Eventually I decided touching things directly is more informative than a lifetime of reading. And here we are.〕

---

## 2-C｜進入 Windhelm，下水道前

**【VIGILANT 劇情】** 進入 Windhelm，Steward 談完（s10 done），即將下地城（s20 not yet）。
**【類型】** 玩家可問
**【gate】** Mq01 s10 done + s20 not done

> **SOFIA**：Windhelm。永遠在下雪，永遠有人在吵架，永遠有股說不清楚的霉味……
> 〔Windhelm. Always snowing, always someone arguing, always that smell you can't quite identify...〕
>
> 而現在我們要往**下**走，去那個霉味的**源頭**。
> 〔...and now we're going *down*, toward whatever is *causing* that smell.〕
>
> 提醒我之後燒掉這雙靴子。
> 〔Remind me to burn these boots afterward.〕

---

## 2-D｜下水道內，吸血鬼現身

**【VIGILANT 劇情】** 地城搜索中（Mq01 s20 active），吸血鬼出現，s60（Mq02 啟動）前。
**【類型】** 玩家可問
**【gate】** Mq01 s20 done + s60 not done

> **SOFIA**：爛泥、老鼠、管子滲水……哦，還有——
> 〔Mud, rats, leaking pipes... oh, and —〕
>
> 吸血鬼。當然。
> 〔Vampires. Of course.〕
>
> 你知道我這套衣服是乾洗的嗎。乾洗。出去以後帳算你的，血跡另算。
> 〔You know this outfit is hand-washed only? Hand. Washed. When we get out, you're paying. Bloodstains are extra.〕

---

## 2-E｜搜查隊有名人物

**【VIGILANT 劇情】** 調查深化（Mq01 s30-s50，女神像→報告階段），搜查隊有份量的 NPC 出現。
**【類型】** 玩家可問
**【gate】** Mq01 s30 done + s60 not done

> **SOFIA**：這幾個看起來有名字。我是說，真的有名字，不是「下水道雜魚甲」那種。
> 〔These ones look like they have names. Real names. Not "sewer thug number three."〕
>
> 你要不要先問問他們知道什麼？我是說……難得我建議「先聊」。這輩子不常發生。趁我還沒改主意。
> 〔You might want to ask what they know first. I know — rare occasion where I'm suggesting *talking*. Take it while it lasts.〕

---

## 2-F｜Lamae 沉睡的宮殿

**【VIGILANT 劇情】** Mq02 s90 完成（Mq03 剛啟動），進入 Lamae 宮殿，夢境入場（Mq03 s30）尚未觸發。
**【類型】** 玩家可問
**【gate】** Mq02 (0x038525) s90 done + Mq03 (0x038526) s30 not done

> **SOFIA**：這地方……
> 〔This place...〕
>
> 安靜得不對。像是連空氣都不敢太用力呼吸。
> 〔It's too quiet. Like even the air is afraid to breathe too hard.〕
>
> 睡在這裡的那位，我有種預感，不是隨便能惹的。輕聲點。這次不是為了禮貌。
> 〔Whatever's sleeping here — I have a feeling we can't just stab our way through this one. Keep it down. Not for politeness. For survival.〕

---

## 2-G｜夢後話題 — 你消失了（夢境入場 DEFERRED，出夢後事後話題）

**【VIGILANT 劇情】** 玩家已被 Lamae 送入夢境（Mq03 s30 done），Lamae 戰尚未開始（s50 not done）。
**【類型】** 玩家可問（出夢後）
**【gate】** Mq03 s30 done + s50 not done
**【NOTE】** 夢境入場的幻影掛件（Sofia 隨玩家進入夢 cell）為 DEFERRED；本話題作為替代，以「你剛才消失了，我在外面等」的角度重現情緒內容。

> **SOFIA**：你……就這樣不見了。
> 〔You just... disappeared.〕
>
> 眼睛閉起來、然後你不在了。我站在那裡，數了一下，數到很大的數字，才意識到這不是你在發呆。你去了某個地方，我進不去的地方。
> 〔Eyes closed, and then you were gone. I stood there, counting, got to a pretty large number before I realized this wasn't just you zoning out. You went somewhere I couldn't follow.〕
>
> 你去哪了。
> 〔Where did you go.〕

---

**（追問）玩家**：你擔心我了。
〔You were worried.〕

> **SOFIA**：我在算你的裝備值多少。就這樣。
> 〔I was calculating the resale value of your gear. That's all.〕
>
> ……我數到的那個數字很大。就這樣，別追問了。
> 〔...It was a very large number. Drop it.〕

---

## 2-H｜夢中演出 — Sofia 的學識判斷（夢境內容 DEFERRED，出夢後事後話題）

**【VIGILANT 劇情】** Lamae 戰開始（Mq03 s50 done），殭屍 Lamae 復活前（s80 not done）。
**【類型】** 玩家可問
**【gate】** Mq03 s50 done + s80 not done
**【NOTE】** 夢中幻影跟入為 DEFERRED；本話題以「你回來之後告訴我」的框架，保留對吟遊詩人/Lamae 起源的學識評論。

> **SOFIA**：吟遊詩人。Lamae。我讀過相關的記載——不是因為我對吸血鬼有興趣，是因為我當時無聊，書剛好在手邊。
> 〔The bard. Lamae. I've read accounts of this — not because I care about vampires, it was just the book in front of me and I was bored.〕
>
> 那個人對她說了什麼，讓她信了他。這才是重點。某人說了足夠漂亮的話，結果把一個女人變成了血親始祖、讓一整個族系在世上留了幾千年。
> 〔Whatever the bard said that made her trust him — *that's* the real story. Someone said exactly the right things, and it turned a woman into the progenitor of an entire bloodline that's been haunting the world ever since.〕
>
> 話語比血更危險。記住這個。
> 〔Words are more dangerous than blood. Remember that.〕

---

**（追問）玩家**：你覺得她是受害者？
〔You think she was a victim?〕

> **SOFIA**：我覺得她是被利用的人，後來變成了一個沒辦法停下來的力量。受害者不受害者的……她現在想殺我們，這個比較實際。
> 〔I think she was someone who got used, and then became a force that couldn't stop. Victim, monster — she's trying to kill us right now, that's the practical part.〕

---

## 2-I｜雙重 Lamae 戰 + 王座提示

**【VIGILANT 劇情】** Lamae 殭屍復活（Mq03 s80 done）；目標「打破詛咒」（Obj90），s90 前。
**【類型】** 玩家可問
**【gate】** Mq03 s80 done + s90 not done

> **SOFIA**：她剛剛起來了。我親眼看著你把她打倒，然後她**起來了**。
> 〔She just got back up. I watched you put her down and she *got back up.*〕
>
> 好。冷靜。這種打不死的，通常不是用蠻力解決的——蠻力我們都試過了，效果：你看到了。
> 〔Okay. Think. Things that won't stay dead usually aren't solved by hitting them harder — we've tried that, you see the results.〕
>
> 王座那邊。有東西在發光。別再砍了，去看那個王座，相信我這一次。
> 〔The throne. Something over there is glowing. Stop swinging and go look at the throne. Trust me on this one.〕

---

## 2-J（好結局）｜章節收束

**【VIGILANT 劇情】** Mq03 s90 done（詛咒打破，好結局；Karma+3；Mq01.SetStage(100) 觸發）。
**【類型】** 玩家可問
**【gate】** Mq03 s90 done（好結局互斥另加壞結局 GLOB==0）

> **SOFIA**：呼。
> 〔...Hm.〕
>
> 這趟我的冒險清單裡多了一條：「跟吸血鬼始祖在她自己的夢裡周旋，再看她殭屍復活，然後靠一張王座把她打死。」
> 〔I've added something new to my personal adventure ledger. "Fought a vampire progenitor in her own dream palace, watched her come back as a zombie, killed her with a chair."〕
>
> 我不確定那是值得驕傲的事，但我確定沒多少人做過。……你還好嗎？你那個表情，我不太喜歡。
> 〔I'm not sure it's something to be proud of, but I'm pretty sure not many people can say it. ...Are you alright? I don't like that look on your face.〕

---

## 2-J（壞結局）｜章節收束 — 詛咒

**【VIGILANT 劇情】** Mq03 s200 done（玩家接受 Molag Bal 詛咒；AbBloodofLamae 給予；Karma-3）。
**【類型】** 玩家可問
**【gate】** Mq03 s200 done（壞結局互斥另加好結局 GLOB==0）

> **SOFIA**：那個東西你接了。
> 〔You took it.〕
>
> 我不知道你在想什麼，我不知道他說了什麼讓你覺得那是個好主意。但我看得出來有什麼東西跟著進來了——跟我認識的你在眼神上不一樣，就差了一點點，但我看得出來。
> 〔I don't know what you were thinking. I don't know what he said that made it sound like a good idea. But I can tell something came in with you — there's something just slightly wrong about your eyes. Just slightly. But I see it.〕
>
> 我們得想辦法解掉這個。你聽到了嗎。我在說「我們」。
> 〔We're going to find a way to undo this. Do you hear me. I said *we*.〕

---

> **第二幕完。**
> 壞結局後：Mq03 s200 → CHMq00（Act 3 alternate path）啟動，Sofia 台詞應反映詛咒陰影延續。
> 夢境幻影掛件（2-G/H 夢中 Sofia）待日後 Mq03 dreammarker FormID 確認後設計。
