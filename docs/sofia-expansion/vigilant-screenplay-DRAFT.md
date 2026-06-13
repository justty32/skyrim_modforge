# Sofia × VIGILANT 對白劇本（DRAFT，待審核）

> **這份是給你審核用的劇本草稿，不是最終 JSON。** 流程：你校正劇情/對白 → 我才轉成 `examples/*.json`。
>
> **怎麼讀**：每一場分三塊——
> - **【VIGILANT 劇情】** = 我對該劇情段的理解（解碼 quest 標題 + 網路片段 + TES lore 拼出來的，**權威 wiki 被擋抓不到全文，所以這塊最需要你校正**；不確定處標 `(?)`）。
> - **【觸發條件】** = 這段 Sofia 對白何時可談（對應 ModForge 的 quest-state / 地點閘）。
> - **SOFIA** = 她的對白草稿（過[性格 brief](sofia-personality.md)：自誇／嘴砲／說溜嘴／打破第四面牆，偶爾露真心再遮掩）。
>
> **Sofia 的喜劇定位**：VIGILANT 是極度陰鬱、文學性、lore 厚重的 dark fantasy；Sofia 是吊兒郎當的嘴砲。**這個反差就是笑點**——她對嚴肅 lore 的不敬反應，正是玩家帶她跑這種 mod 想要的調劑。她不是 lore 學者，不會掉書袋，只會用她的視角吐槽。
>
> **審核重點**：① 劇情理解對不對（尤其 `(?)` 處）；② 哪些 beat 值得加、哪些刪；③ Sofia 對白的口味/尺度；④ 哪些該是「她主動評論」vs「玩家可問」。校完我再補全並轉 JSON。
>
> **狀態**：目前先寫**第一/三/四幕的代表性 beat**（第三幕寒港記憶是 lore 核心，著墨最多）。第二幕與更多支線待你確認大綱後再補，避免在沒把握的劇情上先寫一堆。

---

## 劇情大綱（我的理解，請校正）

VIGILANT（Vicn，2015）共 **4 幕**，玩家是**史丹達的警戒者（Vigilant of Stendarr）**，獵殺 daedra 信徒，最終被 **魔神巴爾（Molag Bal）** 拖進祂的領域**寒港（Coldharbour）**。

- **第一幕 — 史丹達的警戒者**：在天際調查 daedra 崇拜。一連串陰暗案件（莊園、女巫、廢墟）。基調＝偵探＋恐怖。對應 realm：Stuhn Ravine、Bruiant's Estate、Hag's Pond。`(?)` 各案細節。
- **第二幕 — 墮入**：調查越深越接近 Molag Bal，逐漸從「獵巫」變成「被獵」。`(?)` 這幕我最不確定。
- **第三幕 — 寒港與記憶**：被拖進 Coldharbour。由 **Pepe**（前 Marukhati 審判官，墮入寒港的靈魂）當嚮導，重歷一個個**damned 靈魂的記憶**——每段記憶是一個墮落者的人生悲劇，深掛 TES lore 與世界文學：
  - **The Grand Inquisitor**（zzzCHMemoryQuest01）：Mary 的記憶，在監獄塔下水道擊敗她後進入；取材杜斯妥也夫斯基《卡拉馬助夫兄弟》的「大審判官」。`(?)`
  - **The Mad King**（02）：瘋王的記憶（有「Mad King's Bedroom」場景）。`(?)` 哪位瘋王。
  - **Knight of Hound**（03）：獵犬騎士。`(?)`
  - **Temptation of Marukh**（07）：先知 **Marukh**（Alessian Order 創立者、Marukhati Selective 的源頭）受誘惑的記憶。lore 重磅。`(?)`
  - **The Nameless Bard**（08）：無名吟遊詩人——欺騙 Pepe、讓 Marukh 掌管高塔(?) 的關鍵角色。`(?)`
  - **Man-Bull Paravania**（13）：人牛（米諾陶，TES lore 中 Belharza ＝ Alessia 與 Morihaus 之子）相關的記憶。`(?)`
  - （主線 `zzzCHMQ00` "Coldharbour" 串起寒港整段；stage 10/90 是我暫定的「初到/深處」錨點，待實機校。）
- **第四幕 — 灰色震盪與結局**：逃離寒港、對抗 Molag Bal；經 Imperial City → **Curia Morimath**、**Malada**（Marukh 信徒據點）。透過 **Karma 系統**有**多重結局**。`(?)` 結局分歧細節。

> 巴爾的目的（lore）：用 **Ada Bal** 開啟寒港通往 Etherius 的傳送門。

---

## 第一幕：史丹達的警戒者

### 場景 1-A｜加入警戒者之後，初次帶 Sofia 出勤
**【VIGILANT 劇情】** 玩家加入史丹達警戒者，開始獵 daedra 信徒。`(?)` 確切開場任務。
**【觸發條件】** 玩家加入警戒者 quest 啟動後（`GetQuestRunning` 警戒者主線）＋跟 Sofia 講話。
> **SOFIA**：所以我們現在是…道德糾察隊了？拿著鎚子到處敲別人信什麼神。聽起來超無聊。喔別誤會，我喜歡敲東西，我只是比較想敲值錢的。

### 場景 1-B｜Bruiant 莊園（有錢人的 daedra 案）
**【VIGILANT 劇情】** 一座莊園牽涉 daedra 崇拜（對應 worldspace `Bruiant's Estate`）。`(?)` 案情。
**【觸發條件】** 進入 `Bruiant's Estate` worldspace（`GetInWorldspace`, runOn=Subject＝Sofia 在莊園裡）。
> **SOFIA**：嘖，有錢人。又大又空的房子、地下室還藏著邪教祭壇——典型。你看那水晶吊燈，比信仰品味好多了。我們殺完人可以順手…呃，我是說「沒收證物」。

### 場景 1-C｜女巫之池（Hag's Pond）
**【VIGILANT 劇情】** 女巫相關的陰森地點（worldspace `Hag's Pond`）。`(?)`
**【觸發條件】** 進入 `Hag's Pond`。
> **SOFIA**：女巫。哼。又老又醜還想搞魔法搶風頭。長這樣難怪要躲在沼澤裡。…幹嘛？我只是在陳述事實，我可是很慈悲的。

---

## 第三幕：寒港（Coldharbour）

> lore 核心、Sofia 反差喜劇的主場：她對魔神領域的不敬 vs. VIGILANT 的絕望基調。

### 場景 3-A｜初到寒港（slice 已寫）
**【VIGILANT 劇情】** 玩家被 Molag Bal 拖進寒港——一片永恆灰暗、被詛咒的領域。`(?)` 進入方式。
**【觸發條件】** `GetStageDone(zzzCHMQ00 0x12F24E, 10)` ＋ Sofia 人在 Coldharbour worldspace（`GetInWorldspace 0x06D275, Subject`）；聊一次自收。
> **SOFIA**：魔神巴爾的領域。挺溫馨的嘛。讓我想起我前男友家…我是說那個裝潢，別想歪。你幹嘛這樣看我。
> └─（追問）**玩家**：你聽起來有點緊張。
> &nbsp;&nbsp;&nbsp;&nbsp;**SOFIA**：緊張？我？哈。我從不緊張。…你跟緊一點就是了，好嗎。怕你嚇到。為你好。當然。

### 場景 3-B｜嚮導 Pepe
**【VIGILANT 劇情】** **Pepe**（前 Marukhati 審判官、墮入寒港的靈魂）成為玩家在寒港的嚮導。`(?)` 他的語氣/形象。
**【觸發條件】** 認識 Pepe 後（`GetQuestRunning` 寒港主線到對應 stage）。
> **SOFIA**：所以這個 Pepe 是我們的…地獄導遊？一個被詛咒幾百年的老頭帶路。希望他至少知道哪裡有好吃的。…什麼叫這裡沒有食物。那他到底有什麼用。

### 場景 3-C｜記憶：The Grand Inquisitor（擊敗 Mary 後）
**【VIGILANT 劇情】** 在監獄塔下水道擊敗 **Mary** 後，進入她的記憶「大審判官」——取材《卡拉馬助夫兄弟》：審判官質問再臨的基督，人類其實要的是麵包與服從，不是自由。`(?)` VIGILANT 版的具體演繹。
**【觸發條件】** `GetQuestCompleted(zzzCHMemoryQuest01 0x12C4F4)`；含追問。
> **SOFIA**：那個自以為神聖的審判官。燒死人來顯得自己虔誠。至少我殘忍的時候還懂得搞笑。
> └─（追問）**玩家**：你剛剛其實有點感觸。
> &nbsp;&nbsp;&nbsp;&nbsp;**SOFIA**：我感觸到無聊。那也是一種感觸。…好啦，可能還有一點別的。現在給我閉嘴，不然下一個記憶就是你的。

### 場景 3-D｜記憶：Temptation of Marukh
**【VIGILANT 劇情】** 先知 **Marukh** 的記憶——Alessian Order 的創立者，TES lore 中以神諭與教條撼動第一紀元帝國的人物。「誘惑」`(?)` 指他如何從先知墮落/被利用。
**【觸發條件】** `GetQuestCompleted(zzzCHMemoryQuest07 0x06F53C)`。
> **SOFIA**：又一個「聽見神諭」的傢伙。每次有人開始聽見聲音、世界就要倒大楣。我也常聽見聲音啊——大多是我自己在誇自己有多正。那才是值得聽的神諭。

### 場景 3-E｜記憶：Man-Bull Paravania
**【VIGILANT 劇情】** 人牛（米諾陶）相關記憶；TES lore 裡人牛源自 Alessia 與風神 Morihaus 之子 Belharza。`(?)` Paravania 的故事。
**【觸發條件】** `GetQuestCompleted(zzzCHMemoryQuest13 0x51C038)`。
> **SOFIA**：一隻…人牛。半人半牛。我就不問另一半是怎麼來的了。噁。雖然——肌肉是不錯啦。呃。我是說作為對手很強。專業評估而已。

### 場景 3-F｜記憶：The Mad King
**【VIGILANT 劇情】** 「瘋王」的記憶（有 Mad King's Bedroom 場景）。`(?)` 哪位王、為何瘋。
**【觸發條件】** `GetQuestCompleted(zzzCHMemoryQuest02 0x13712B)`。
> **SOFIA**：瘋王。坐在王座上對著空氣下令。你知道嗎，瘋子跟有錢人其實很像——沒人敢說他們的點子很爛。差別只在瘋子沒錢請我來保護他。

### 場景 3-G｜寒港深處（slice 已寫）
**【VIGILANT 劇情】** 在寒港待久了，絕望感累積。`(?)` 對應劇情低點。
**【觸發條件】** `GetStageDone(zzzCHMQ00, 90)`；emotion=Sad；聊一次自收。
> **SOFIA**：…這地方真的有點嚇到我了。你敢跟別人說我講過這句、我就矢口否認。大聲否認。

---

## 第四幕：灰色震盪與結局

### 場景 4-A｜逼近 Molag Bal
**【VIGILANT 劇情】** 玩家準備正面對抗 Molag Bal、逃離寒港（「灰色震盪 Greymarch」`(?)` 此處借用詞）。
**【觸發條件】** Act 4 主線 stage `(?)`。
> **SOFIA**：所以計劃是去揍一個魔神。一個。神。好，正常的一天。我先說好——如果我要死，我要死得有型。我的頭髮看起來還行吧？

### 場景 4-B｜Malada（Marukh 信徒據點）
**【VIGILANT 劇情】** 經 Imperial City 到 **Curia Morimath**、**Malada**（崇拜先知 Marukh 的信徒據點）。`(?)`
**【觸發條件】** 進入 `Malada` 相關 cell/worldspace。
> **SOFIA**：又是一群為了死了幾千年的先知打打殺殺的人。你們人類最大的問題就是太認真。要是大家把信仰的精力拿來照鏡子，世界會和平很多。看看我，多平靜。

### 場景 4-C｜結局（Karma 分歧）
**【VIGILANT 劇情】** 透過 Karma 系統有多重結局。`(?)` 各結局走向。
**【觸發條件】** 主線完成（`GetQuestCompleted` 終章）。
> **SOFIA**：結束了。我們真的揍贏了一個魔神。…說真的，能活著從那種地方走出來，旁邊還有你——其實挺好的。呃。我是說，挺好…的，因為你還能繼續幫我提東西。對。就是那個意思。

---

## 待補/待確認清單（給審核）

1. **第二幕**整段我幾乎是空白——它的設定與關鍵 beat 麻煩你補。
2. 第三幕記憶我只寫了 5 段（01/02/07/13 + 主線），還有 **Knight of Hound（03）、The Nameless Bard（08）** 及其他 MemoryQuest（04–06, 09–12…共到 13+）沒寫——值得每個都做嗎？還是挑幾個經典？
3. `(?)` 標記處的劇情正確性。
4. Sofia 對白尺度：說溜嘴的黃腔要多露骨？打破第四面牆的 meta 玩笑要多少？
5. 每段該是「Sofia 主動講（hello）」還是「玩家可問（player topic）」？目前全寫成可問（符合你「對話新增選項」的定調），但有些她可能會忍不住主動吐槽。
