# 第四章記憶 01 - 審判官 (The Grand Inquisitor)

狀態：基於來源、連結優先的重構切片。非劇情摘要。

來源方針：
- 原始語句連結回抽取的來源文件，而非全文複製。
- 僅在需要解釋翻譯問題時才出現短小的來源片段。
- 本記憶的英文很大程度上是從日文機器翻譯而來；許多語句語意不明。語意不明的來源短語將逐字保留並加上 `Note: 待驗證` 標記，而非在中文中強行潤飾。
- `SCEN` 編排來自 CLI 診斷，因為抽取的 `dialogue.md` 僅保留場景話題文本，而非場景階段/動作。

## 任務紀錄 (Quest Record)

[`12C4F4 zzzCHMemoryQuest01 "The Grand Inquisitor"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:140)

CLI：
- `questdiag Vigilant.esm 0x12C4F4`
- `infodiag Vigilant.esm 0x12C4F4`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x12C4F4`
- EditorID: `zzzCHMemoryQuest01`
- 名稱: `The Grand Inquisitor`
- 標誌: `RunOnce`
- 優先級: `90`
- 類型: `Misc`
- 過濾器: `CH\`

來自 `questdiag` 的階段：

| 階段 | 標誌 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 1 | 無 | 空 |
| 10 | 無 | 空 |
| 20 | CompleteQuest | 空 |
| 30 | 無 | 空 |
| 40 | 無 | 空 |
| 100 | CompleteQuest | 空 |
| 110 | 無 | 空 |
| 120 | 無 | 空 |
| 999 | ShutDownStage | 空 |

任務目標：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:141) | 未獲回應的夢，在沉默中沉沒。 |

- 筆記：來源目標 `Unanswered Dream sink in silence.` 本身不合語法（主詞/動詞不匹配）；採直譯。待驗證。

目標對象：
- ESM 中有 1 個目標 (`questdiag`: `objective[0] ... targets=1`)。
- 目標無條件。
- 目前 CLI 輸出不會列印目標引用；若目標地點重要，則需要更深入的 QUST 目標轉儲。

## 別名 / 編排骨幹 (Alias / Staging Backbone)

以下兩個 `SCEN` 紀錄共用相同的主機任務與別名。

主機任務：
- [`12C4F4 zzzCHMemoryQuest01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:140)

來自 `scenediag` 的主機任務別名：

| 別名 | 名稱 | 填充 |
|---:|---|---|
| 0 | `Mara` | 唯一演員 [`0F9649 zzzCHBossShoggothMother "Mary the Dark Virgin"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:262) |
| 2 | `Inquisitor` | 唯一演員 [`12BF48 zzzCHInquisitorPepeMemory "Inquisitor Pepe"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:558) |
| 3 | `Molag` | CLI 未列印（無靜態填充） |
| 4 | `Door` | 強制引用 `12BED7:Vigilant.esm` |
| 5 | `TravelMarker` | 強制引用 `12BF4C:Vigilant.esm` |
| 6 | `MaraMemory` | CLI 未列印（無靜態填充） |

推論：
- 本記憶的**主體 / 說話者**為別名 `#2` `Inquisitor` = `Inquisitor Pepe`。每個自定義分支 INFO 都受限於別名 `#2` 的 `GetIsAliasRef == 1`，因此「審判官」的獨白是屬於他的。
- 對話中的受話者始終是 "Mara" —— 別名 `#0`，由 `0F9649` ("Mary the Dark Virgin") 靜態填充。（推論）這裡的 "Mara" 是審判官審訊的對象；這是記憶中玩家的替身 / 被指控的「女巫」，而非女神。
- 別名 `#3` `Molag` 無靜態填充，推測在執行時填充；它是 Scene02 中的第二位場景演員（見下文）。（推論）
- 這是**陀思妥耶夫斯基的《宗教大法官》場景**在亞歷西亞語境下的重構：審判官指指控 "Mara" 冒充瑪拉/救世主，威脅明天要將她作為女巫處以火刑，並為亞歷西亞教團的塔、石頭、奧秘與奇蹟辯護。（推論，來自下方的分支文本）

## 場景紀錄 (Scene Records)

場景紀錄在 `game-data` 中並非完整紀錄；唯一的場景話題行連結至 `dialogue.md`，而階段/動作則來自 `scenediag`。除了 Scene02 的最後一個動作外，這兩個場景純粹透過 `Package` 動作驅動其演員（無分階段的 `Dialog` 話題）。

### 12DBA7 zzzCHMeQ01Scene01

CLI：
- `scenediag Vigilant.esm 0x12DBA7`

編排：
- 主機任務：[`12C4F4 zzzCHMemoryQuest01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:140)
- 標誌：無
- 演員：別名 `#2` (`Inquisitor`), behaviorFlags 0
- 階段：3 個，每個都有 0 個開始條件與 1 個完成條件。
- 動作：
  - 索引 1: `Package`, 演員 `#2`, 階段 0。
  - 索引 2: `Package`, 演員 `#2`, 階段 1。
  - 索引 3: `Package`, 演員 `#2`, 階段 2。
- 無 `Dialog` 動作；本場景僅透過 package 讓審判官走動/定位。（推論）審訊語句是透過自定義分支（見下文）播放，而非作為場景嵌入的 `Dialog` 動作。

### 12DBAD zzzCHMeQ01Scene02

CLI：
- `scenediag Vigilant.esm 0x12DBAD`

編排：
- 主機任務：[`12C4F4 zzzCHMemoryQuest01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:140)
- 標誌：無
- 演員：別名 `#2` (`Inquisitor`) 與別名 `#3` (`Molag`)，皆為 `NoPlayerActivation`。
- 階段：3 個，每個都有 0 個開始條件與 1 個完成條件。
- 動作：
  - 索引 1: `Package`, 演員 `#2`, 階段 0。
  - 索引 2: `Package`, 演員 `#2`, 階段 1-2。
  - 索引 3: `Package`, 演員 `#3`, 階段 1。
  - 索引 4: `Package`, 演員 `#3`, 階段 2。
  - 索引 5: `Dialog`, 演員 `#3` (`Molag`), 階段 2, 標誌 `HeadtrackPlayer`, 話題 [`12DBB0`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1467), 情緒 `Neutral`, 循環 1-10。

場景專屬話題（`SCEN` 類別，由任務擁有，在 Scene02 動作 5 中播放）：
- [`12DBB0` / INFO `12DBB1`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1467) (Happy): 「Mara，是不同的，我為這樣的結局致意。實在很可惜。」
  - 筆記：來源 `Mara, is differents and I greet the consequences of such. It is a pity that very` 語意嚴重錯亂；翻譯僅為盡力而為。待驗證。
  - 推論：由別名 `#3` `Molag` 說出（`Dialog` 動作指定 ActorID=3）。這是唯一的場景口說對白；審判官的完整獨白則存在於自定義分支中。

## 自定義對話分支：審判官佩佩 (Inquisitor Pepe)

分支：
- `12CA9F:Vigilant.esm`（根據下方每個 INFO 的 `infodiag` `branch=12CA9F`）

說話者條件模式：
- **每個** INFO 都要求別名 `#2` (`Inquisitor`) 的 `GetIsAliasRef == 1`。
- 這些 INFO 上沒有出現 `GetStage` 門檻（與 MeQ07 不同）。該分支是單一說話者的長篇獨白，按話題排序，而非拆分為兩個由階段門檻限制的開場白。
- 話題 EditorID 使用前綴 `zzzCHMeQPepeB01T*` (Pepe = 審判官的名字)，而非 `zzzCHMeQ01*`。
- 整個分支都是審判官的獨白；玩家僅提供提示語 (`It...`, `......(Silence)`, `......(Stare)`)。

| 話題 | INFO | 優先級 | 標誌 | 條件 | 翻譯 |
|---|---|---:|---|---|---|
| [`12CAA0 zzzCHMeQPepeB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1407) | `12CAA1` | 90 | `SayOnce`, `WalkAway` | `GetIsAliasRef alias #2` | (Fear)「你究竟是不是 Mara……Mara？」 (Puzzled)「你來到這裡的諷刺……我們竟想以 Alessia 的樣貌、甚至以更多的樣貌現身？」 筆記：兩句皆語意不明，待驗證。 |
| [`12CAA2 zzzCHMeQPepeB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1411) | `12CAA3` | 45 | `Goodbye`, `SayOnce` | `GetIsAliasRef alias #2`; 結束時 VMAD `CHMeq1_TIF__0212CAA3.Fragment_0` | 提示語：「It...」(Anger)「女巫，閉嘴……閉嘴，就算群眾是愚人，愚人也不會把老鷹當成老鷹……」(Anger)「明天早上，你會被綁在火刑柱上燒死。你冒充聖 Alessia，要以女巫之名付之一炬。」(Anger)「你，但這種事我當然知道！」 筆記：語意不明，待驗證。 |
| [`12D04A zzzCHMeQPepeB01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1416) | `12D04B` | 55 | `WalkAway` | `GetIsAliasRef alias #2` | 提示語：「......(Silence)」(Neutral)「很好，我也保持沉默。因為反正你也沒有那樣的權利。」(Neutral)「為什麼，你要在我們此刻於世上成就大業之時來礙事？你不知道明天會是身、還是別的嗎？」(Neutral)「我們知道你是什麼。但那種事無關緊要。無論如何，明天我們把你當女巫燒掉。」(Neutral)「明天，今天親吻你雙足的那些人，會往火裡丟柴薪——這是我的一點暗示。」 筆記：多句語意不明，待驗證。 |
| [`12D04C zzzCHMeQPepeB01T04`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1422) | `12D04D` (INFO[0]) | 55 | `SayOnce`, `WalkAway` | `GetIsAliasRef alias #2` | 提示語：「......(Silence)」(Neutral)「看這塊石頭。你能把這石頭……？這種事當然不會太遠……」(Neutral)「『人活著不是單靠食物』——這就是我給你的回答。」(Neutral)「如同 Shezarr 從前的造物，Deidre 曾以麵包之名反叛它，對你而言也是。」(Happy)「結果，你們大概不知緣由——那 Deidre 之後成群湧出、走向公開的身影。」 筆記：語意不明，待驗證。 |
| (續) | `12DBA6` (INFO[1]) | 55 | `WalkAway` | `GetIsAliasRef alias #2` | (Neutral)「總之，人不過是即將到來的飢餓。而那些在麵包之後高喊善行的人，毀掉了你的塔。」(Neutral)「你們必定要建一座新塔。但那是徒勞。連命運之塔的地基都建不成。」(Neutral)「若你不打算建塔，或許能稍稍緩解人們的痛苦。但你沒有。」(Anger)「人們怎麼做？他們來到我們、Alessia 教團這裡。那些曾允諾要偷走 Shezarr 之心的人在說謊！！」 筆記：語意不明，待驗證。同一 topic 的第二則 INFO。 |
| [`12D04E zzzCHMeQPepeB01T05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1432) | `12D04F` | 55 | `WalkAway` | `GetIsAliasRef alias #2` | 提示語：「......(Silence)」(Neutral)「如果你是 Mara，就敢吞下這塊石頭。火焰平息，一切可憎的喜劇都將慶祝終結。」(Neutral)「但你不會吞。我把它收起，因為他一否認奇蹟，也就否認了 Edora。」(Neutral)「人寧可相信奇蹟、勝於相信 Edora。在自己身上造出奇蹟，就會去相信像我這樣的審判官。」(Neutral)「重要的是不要把人變成奇蹟的奴隸——那才是自由的信仰，你，我本以為你也會這樣想。」(Sad)「並非真的深愛人們。你們太深地愛了什麼樣的人們啊。那樣人就不會挨餓了。」 筆記：語意不明，待驗證。 |
| [`12D050 zzzCHMeQPepeB01T06`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1439) | `12D051` | 55 | `WalkAway` | `GetIsAliasRef alias #2` | 提示語：「......(Silence)」(Neutral)「重要的既不是你的愛，也不是自由。而是奧秘——這塊石頭。所有人都必須違背良心向它屈服。」(Neutral)「我們在街上有 Alessia 教團。那……卻終究改寫了你大大的一切。」(Neutral)「然後在教會、權柄、奧秘與奇蹟之上，我建起這座塔。把無盡的人從『自由』的痛苦中解放。」(Neutral)「如此一來，若我們得到 Alessia 教團的寬恕——他們顧念弱者，甚至容忍惡行。」(Happy)「這不正是你愛人類的證據嗎？」 筆記：語意不明，待驗證。 |
| [`12D052 zzzCHMeQPepeB01T07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1446) | `12D053` | 55 | `WalkAway` | `GetIsAliasRef alias #2` | 提示語：「......(Silence)」(Neutral)「再說一次，但你究竟為何……此刻來礙我們成就大業？」(Neutral)「該屬於 Edora 的歸 Edora。對皇帝，皇帝該說的話——你們把這石頭從我們這裡奪走了嗎？」(Neutral)「這石頭在大地上的力量。我們緊貼著、持續握有這石頭。捨棄你們，我得去崇拜那低賤奴隸的女王。」(Neutral)「那是從那時起，我也早在兩千年前，Imuga 的先知在 Colovia 的叢林裡找到了這石頭。」(Happy)「塔我們還未臻完美。但它遲早會完成。黎明時所有人都會幸福至極。」 筆記：語意不明，待驗證；`Imuga`/`Colovia` 為專名待驗證。 |
| [`12D054 zzzCHMeQPepeB01T08`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1453) | `12D055` | 55 | `WalkAway` | `GetIsAliasRef alias #2` | 提示語：「......(Silence)」(Neutral)「你為何拒絕這塊石頭？若你懷著這石頭的一點希望……若不在 Mundus 的至福之境？」(Neutral)「本該由那一個人 —— 該背負良心與統治者之責、所有人之責的人 —— 來承擔。」(Neutral)「你做不到，但我們 Alessia 教團能。我們要靠這石頭在大陸上建起一個大帝國。」(Neutral)「只要有這石頭，我們這帝國，甚至不必等待 Shezarr 的歸來。」 筆記：語意不明，待驗證；`Mundasu` 推為 `Mundus`，待驗證。 |
| [`12D056 zzzCHMeQPepeB01T09`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1459) | `12D057` | 55 | `WalkAway` | `GetIsAliasRef alias #2` | 提示語：「......(Silence)」(Neutral)「既然我所說的都已實行，大帝國就會被建起。一而再，你明天會看見那可悲而馴服的羊群。」(Neutral)「只要打個手勢，他們就會心甘情願為你搬柴。你知道為什麼嗎？因為你來礙了我們的事。」(Sad)「若說有誰配在 Mundus 被焚，那肯定就是你。明天我們把你燒掉。到此為止！！」 筆記：語意不明，待驗證。 |
| [`12D058 zzzCHMeQPepeB01T10`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1464) | `12D059` | 55 | `Goodbye`, `SayOnce` | `GetIsAliasRef alias #2`; 結束時 VMAD `CHMeq1_TIF__0212D059.Fragment_0` | 提示語：「......(Stare)」(Fear)「……即便如此又如何……別再出現了，快走，滾出去。」 筆記：語意不明，待驗證。 |

翻譯筆記：
- 整個分支都是從日文機器翻譯而來；句子邊界、代詞（「你/你們」）和專有名詞皆不可靠。上方每個儲存格都標註了「待驗證」。
- 需驗證的循環專有名詞：`Shezarr` (來源 `Shezaru`)、`Deidre` (可能為 `Daedra`？ —— 待驗證)、`Edora` (待驗證)、`Imuga` (參見 MeQ07 中的 `Imga`/`Imga 僧侶` —— 待驗證)、`Colovia`、`Mundus` (來源 `Mundasu`)、`Alessia order/meeting/Association` (皆譯為 `Alessia 教團`，來源不統一)。
- `T02` 提示語來源 `"It..."`；`T03`–`T09` 提示語來源 `"......(Silence)"`；`T10` 提示語來源 `"......(Stare)"`。這些是玩家唯一的輸入 —— 沉默 —— 符合任務目標「在沉默中沉沒」。

## 相關紀錄 (Related Records)

這些由本任務的別名引用，或出現在審判官的獨白中，應在完整的重構中進行交叉連結。別名已透過 `scenediag` 確認；NPC 名稱來自 `npcs.tsv`。

NPCs：
- [`12BF48 zzzCHInquisitorPepeMemory "Inquisitor Pepe"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:558) — 審判官 / 說話者，別名 `#2`。
- [`0F9649 zzzCHBossShoggothMother "Mary the Dark Virgin"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:262) — 填充別名 `#0 Mara`，即受審訊的 "Mara"。
- 審判官佩佩也出現在其他地方：[`081E46 zzzCHInquisitorPepe "Inquisitor Pepe"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1065), [`1363DC zzzCHInquisitorPepeGhost`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:546), [`05ADFD zzzCHInquisitorPepeMemory2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1044), [`06A230 zzzCHInquisitorPepeMemory3`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1069)。（跨任務 —— 非 `12C4F4` 擁有；根據索引，佩佩是佩佩/阿達·巴爾記憶集群 MeQ05/MeQ06 的核心人物。）

引用 (強制引用別名，不在 `npcs.tsv` 中)：
- `12BED7:Vigilant.esm` — 別名 `#4 Door`。
- `12BF4C:Vigilant.esm` — 別名 `#5 TravelMarker`。

Packages (來自 `find zzzCHMeQ01`) — 驅動場景演員：
- `12DBA8 zzzCHMeQ01PepeTravel01`
- `12DBA9 zzzCHMeQ01PepeFrocGreet`
- `12DBAF zzzCHMeQ01PepeGetOut`
- `12DBB2 zzzCHMeQ01PepeStandbyPrison`
- 推論：package 名稱 (`Travel`, `Greet`, `GetOut`, `StandbyPrison`) 符合監獄審訊的編排 —— 審判官在監獄旁待命、致意、進行獨白，然後命令「滾出去」（對應 `T10` "get out"）。

書籍：
- 本任務未擁有任何書籍。佩佩/瑪拉/亞歷西亞教團主題在數個 Vigilant 遊戲內筆記中重複出現（例如 [books.md 中提及佩佩祭司 + 瑪拉雕像](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:9), [瑪拉受火刑敘事](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1274)），但那些並非 `12C4F4` 紀錄；僅供交叉連結，不予歸屬。（推論）

## 重構筆記 (Reconstruction Notes)

基於來源：
- 本記憶由 [`12C4F4 zzzCHMemoryQuest01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:140) 代表，任務目標為 [`未獲回應的夢，在沉默中沉沒。`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:141)。
- 包含**兩個 `SCEN` 紀錄** (`12DBA7 Scene01`, `12DBAD Scene02`)，皆透過 `Package` 動作編排；僅 Scene02 具有單個 `Dialog` 動作（別名 `#3 Molag`, 話題 `12DBB0`）。
- 包含**一個自定義對話分支** (`12CA9F`)，為審判官（別名 `#2` = 審判官佩佩）的 10 個話題 / 11 個 INFO 的單一說話者獨白，皆受限於 `GetIsAliasRef alias #2` 且**無 `GetStage` 門檻**。
- 主體/說話者為**審判官佩佩**；受話者為 **"Mara"** (別名 `#0`)。任務目標「在沉默中沉沒」符合玩家提示語僅為沉默/凝視。
- 在兩個 `Goodbye/SayOnce` 選擇上存在 VMAD 片段 (`12CAA3` → `CHMeq1_TIF__0212CAA3.Fragment_0`; `12D059` → `CHMeq1_TIF__0212D059.Fragment_0`)，表示這些可能推進任務狀態 / 路由完成路徑。此處未解碼確切的 Papyrus 行為。

20 與 100 如何選擇（分支階段分析）：
- `questdiag` 顯示兩個 `CompleteQuest` 階段：**20** 與 **100** —— 這是索引中循環出現的兩波段業障特徵。
- **與 MeQ07 不同，此處的分支 INFO 不帶任何 `GetStage` 門檻**，因此 20 與 100 的路由在兩個階段限制的對話開場白中是不可見的。它必須改由兩個 `Goodbye` 出口上的 **VMAD 片段**設定（`12CAA3` = `T02` 處提早發怒的「明天燒死你」出口，優先級 45；`12D059` = `T10` 處最後的「滾出去」出口，優先級 55）。（推論）
  - `12CAA3` (`T02`, `Goodbye/SayOnce`, 優先級 45) 是**提早離開**：玩家打破沉默 ("It...") 而審判官將其打斷 → 可能指向**提早完成 (階段 20)**。（推論）
  - `12D059` (`T10`, `Goodbye/SayOnce`, 優先級 55) 是在玩家全程保持沉默後的**獨白結束出口** → 可能指向**延後完成 (階段 100)**。（推論）
- **好/壞極性：從條件數據中無法判定。** 這些 INFO 上沒有 `GetStage` 或業障全局變數條件可用於讀取極性。需要解碼兩個 TIF 片段腳本（查看每個階段呼叫哪個 `SetStage`）來分配哪種完成路徑是「在沉默中忍受」（可能為好）與「打破/回答」（可能為壞）的結果。待辦。

開放驗證：
- 反編譯 / 檢查 片段腳本 `CHMeq1_TIF__0212CAA3` 與 `CHMeq1_TIF__0212D059`，以確認每個腳本呼叫哪個 `SetStage` (20 或 100) —— 這能解決 20/100 路由以及好/壞極性問題；
- 直接轉儲 QUST 別名，以確認 `#3 Molag` 與 `#6 MaraMemory` 的執行時填充，以及 objective[0] 的目標引用；
- 確認 Scene02 中別名 `#3 Molag` 的執行時填充（唯一口說場景語句 `12DBB1` 是他的）；
- 針對語意不明的專有名詞（`Shezarr`/`Shezaru`、`Deidre`、`Edora`、`Imuga`、`Mundasu`→`Mundus`、`Colovia`）對照原始日文或更清晰的在地化版本進行驗證，以免用於敘事 —— 每個對話單元皆標註了「待驗證」；
- 若監獄的空間編排重要，請檢查引用 `12BED7` (Door) 與 `12BF4C` (TravelMarker) 以及四個 `Pepe*` package。

## 開放驗證 (Open verification)

- 兩個 TIF 片段是單一最重要的未知數：它們限制了 20/100 完成路徑的分離以及尚未解決的好/壞極性。
- 所有對話翻譯皆為針對語意不清的機器翻譯英文（源自日文）的盡力之作；請視為暫定版本。
