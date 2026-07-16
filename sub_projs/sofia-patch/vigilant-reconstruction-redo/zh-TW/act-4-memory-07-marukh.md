# 第 4 章記憶 07 - 馬魯克的誘惑 (Temptation of Marukh)

狀態：首次重新製作切片。以原始資料為基礎，連結優先，並非劇情摘要。

來源方針：
- 原始對話行連結回提取的原始文件，而非全文複製。
- 僅在需要解釋翻譯問題時顯示短小的原始片段。
- `SCEN` 舞台編排來自 CLI 診斷，因為提取的 `dialogue.md` 僅保留場景主題文本，不保留場景階段/動作。

## 任務紀錄 (Quest Record)

[`06F53C zzzCHMemoryQuest07 "Temptation of Marukh"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101)

CLI：
- `questdiag Vigilant.esm 0x06F53C`
- `infodiag Vigilant.esm 0x06F53C`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務中繼資料：
- FormID：`Vigilant.esm:0x06F53C`
- EditorID：`zzzCHMemoryQuest07`
- 名稱：`Temptation of Marukh`
- 標記 (Flags)：`RunOnce`
- 優先級 (Priority)：`90`
- 類型 (Type)：`Misc`
- 過濾器 (Filter)：`CH\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 標記 | 日誌 |
|---:|---|---|
| 0 | 無 | 空白 |
| 10 | 無 | 空白 |
| 20 | 無 | 空白 |
| 30 | 無 | 空白 |
| 40 | 無 | 空白 |
| 50 | 無 | 空白 |
| 60 | 無 | 空白 |
| 70 | CompleteQuest | 空白 |
| 80 | 無 | 空白 |
| 150 | CompleteQuest | 空白 |
| 160 | 無 | 空白 |
| 255 | ShutDownStage | 空白 |
| 999 | ShutDownStage | 空白 |

目標 (Objective)：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:102) | 猿人睡在哪裡？ |

目標物 (Objective targets)：
- ESM 中有 3 個目標物。
- 目標 1 有 2 個條件。
- 目標 2 有 2 個條件。
- 目標 3 有 0 個條件。
- 目前的 CLI 輸出未印出目標物參考；若目標位置很重要，則需要更深入的 QUST 目標物傾印。

## 別名 / 編排骨幹 (Alias / Staging Backbone)

下方的四個 `SCEN` 紀錄共享相同的宿主任務與別名。

宿主任務：
- [`06F53C zzzCHMemoryQuest07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101)

來自 `scenediag` 的宿主任務別名：

| 別名 | 名稱 | 填充 |
|---:|---|---|
| 0 | `EndMarker` | forcedRef `06F53B:Vigilant.esm` |
| 1 | `StartMarker` | forcedRef `06CA17:Vigilant.esm` |
| 3 | `Bard` | forcedRef `06F544:Vigilant.esm` |
| 4 | `Stone` | CLI 未印出 |
| 5 | `MolagBal` | uniqueActor `0708BB:Vigilant.esm` |
| 6 | `Alessia` | uniqueActor [`0708BE zzzCHStAlessiaMemoryGhost`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1072) |
| 7 | `TA01` | forcedRef `0708C6:Vigilant.esm` |
| 8 | `TA02` | forcedRef `0708C5:Vigilant.esm` |
| 9 | `GuideMarker02` | forcedRef `42E0B6:Vigilant.esm` |
| 10 | `GuideMarker01` | forcedRef `4307C4:Vigilant.esm` |
| 11 | `GuideKey` | forcedRef `4369F7:Vigilant.esm` |

推論：
- `TA01` 與 `TA02` 在此記憶編排中負責執行場景獨白對話。
- `Alessia` 與 `MolagBal` 是自定義主題分支使用的對話別名。
- 這是根據別名名稱以及別名 `#6` 與別名 `#5` 上的 INFO 條件 `GetIsAliasRef` 推論而來。

## 場景紀錄 (Scene Records)

場景紀錄在 `game-data` 中未以完整紀錄形式存在；文本行連結至 `dialogue.md`，而階段/動作則來自 `scenediag`。

### 0708C7 zzzCHMeQ07Sc01

CLI：
- `scenediag Vigilant.esm 0x0708C7`

編排：
- 宿主任務：[`06F53C zzzCHMemoryQuest07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101)
- 標記：`Interruptable` (可中斷)
- 演員：別名 `#7` (`TA01`)
- 階段 (Phases)：3 個，每個具有 0 個開始條件與 1 個完成條件。
- 動作 (Actions)：
  - 索引 1：`Timer`，演員 `#7`，階段 0，`0.5` 秒。
  - 索引 2：`Dialog`，演員 `#7`，階段 1，主題 [`0708C8`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:896)，情感 `Neutral`。
  - 索引 3：`Dialog`，演員 `#7`，階段 2，主題 [`0708CA`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:899)，情感 `Neutral`。

翻譯：
- [`0708C8` / INFO `0708C9`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:896)：「不知過了多少日子，我在荒野中徘徊。在灼熱的陽光下，我的視線變得模糊，舌頭腫脹，毛髮脫落。」
- [`0708CA` / INFO `0708CB`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:899)：「我為什麼會在這裡，連自己怎麼來的都不知道。我究竟在這片荒野裡尋找什麼？腦中一片朦朧，什麼也想不清。」

### 0708CC zzzCHMeQ07Sc02a

CLI：
- `scenediag Vigilant.esm 0x0708CC`

編排：
- 宿主任務：[`06F53C zzzCHMemoryQuest07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101)
- 標記：`Interruptable`
- 演員：別名 `#8` (`TA02`)
- 階段：3 個，每個具有 0 個開始條件與 1 個完成條件。
- 動作：
  - 索引 1：`Timer`，演員 `#8`，階段 0，`0.1` 秒。
  - 索引 2：`Dialog`，演員 `#8`，階段 1，主題 [`0708CD`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:902)，情感 `Neutral`。
  - 索引 3：`Dialog`，演員 `#8`，階段 2，主題 [`0708CF`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:905)，情感 `Neutral`。

翻譯：
- [`0708CD` / INFO `0708CE`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:902)：「那是吟遊詩人的屍體。他是被吸血鬼打倒的嗎？還是就在荒野中斷了氣？沒有出路的人，最後就會出現在這片荒野。」
  - 備註：來源短語 `Without this outlet` 語意不明；譯為「沒有出路」。
- [`0708CF` / INFO `0708D0`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:905)：「不論是哪一種，照這樣下去，我也會和這個人走向同樣的命運。」

### 0708D1 zzzCHMeQ07SC03

CLI：
- `scenediag Vigilant.esm 0x0708D1`

編排：
- 宿主任務：[`06F53C zzzCHMemoryQuest07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101)
- 標記：無
- 演員：別名 `#8` (`TA02`)
- 階段：3 個，每個具有 0 個開始條件與 1 個完成條件。
- 動作：
  - 索引 1：`Timer`，演員 `#8`，階段 0，`0.1` 秒。
  - 索引 2：`Dialog`，演員 `#8`，階段 1，主題 [`0708D2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:908)，情感 `Neutral`。
  - 索引 3：`Dialog`，演員 `#8`，階段 2，主題 [`0708D4`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:911)，情感 `Neutral`。

翻譯：
- [`0708D2` / INFO `0708D3`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:908)：「三隻眼睛已經看不見了，舌頭腫脹，聲音也耗盡了。我很快就會死。」
  - 備註：來源短語 `Eyes of the three` 尚未解析；可能需要 NPC/模型驗證。
- [`0708D4` / INFO `0708D5`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:911)：「唯一的遺憾，是最後沒能再見到你，親愛的 Dulsa (杜爾莎)。」

### 0708D6 zzzCHMeQ07Sc02b

CLI：
- `scenediag Vigilant.esm 0x0708D6`

編排：
- 宿主任務：[`06F53C zzzCHMemoryQuest07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101)
- 標記：無
- 演員：別名 `#8` (`TA02`)
- 演員行為標記：`DeathEnd`, `CombatEnd`, `DialoguePause`
- 階段：3 個，每個具有 0 個開始條件與 1 個完成條件。
- 動作：
  - 索引 1：`Timer`，演員 `#8`，階段 0，`0.1` 秒。
  - 索引 2：`Dialog`，演員 `#8`，階段 1，主題 [`0708D7`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:914)，情感 `Neutral`。
  - 索引 3：`Dialog`，演員 `#8`，階段 2，主題 [`0708D9`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:917)，情感 `Neutral`。

翻譯：
- [`0708D7` / INFO `0708D8`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:914)：「一碰到那石頭，就像被灼燒一樣，熱量連同靈魂都被吸走。這塊石頭，難道就是荒野中吸血鬼的真身嗎？」
- [`0708D9` / INFO `0708DA`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:917)：「必須記下來：這石頭也吞噬了成千上萬人的靈魂。被困住的靈魂在石中劇烈翻攪。」

## 自定義對話分支：艾萊西亞 (Alessia)

分支：
- `0731F4:Vigilant.esm` (`zzzCHMeQ07AlessiaB01`)

講者條件模式：
- 大多數 INFO 要求別名 `#6` (`Alessia`) 上的 `GetIsAliasRef == 1`。
- 開啟行同時要求滿足任務 `06F53C` 上的 `GetStage == 40`。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`0731F5 zzzCHMeQ07AlessiaB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:920) | `0731F6` | 無 | `GetStage == 40`; `GetIsAliasRef alias #6` | 「馬魯克 (Marukh)，你聽得見我嗎？」 |
| [`0731F7 zzzCHMeQ07AlessiaB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:923) | `0731F8` | 無 | `GetIsAliasRef alias #6` | 提示：「Al-Esh 女王……為什麼？」 回應：「那是因為這塊石頭。Adabaru 已經失落；若能讓它重獲光輝，至今仍在延續的戰爭便會終結。」 |
| [`0731F9 zzzCHMeQ07AlessiaB01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:926) | `0731FA` | 無 | `GetIsAliasRef alias #6` | 提示：「你為什麼……？」 回應：「填滿那塊石頭。復甦的 Adabaru，我要將它安置於塔中。那就是你的使命。」 |
| [`0731FB zzzCHMeQ07AlessiaB01T04`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:929) | `0731FC` | `Goodbye`, `SayOnce` | `GetIsAliasRef alias #6`; VMAD `CHMeq07_TIF__020731FC.Fragment_0` 結束時 | 提示：「我明白。願你慈悲……」 回應：「我期待著你。因為這是只有你才能做到的事。」 |
| [`0731FD zzzCHMeQ07AlessiaB01T05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:932) | `0731FE` | `Goodbye` | `GetIsAliasRef alias #6` | 提示：「……」 回應：「怎麼了？」 |

## 自定義對話分支：莫拉格·巴爾 (Molag Bal)

分支：
- `073200:Vigilant.esm` (`zzzCHMeQ07MolagB01`)

講者條件模式：
- 大多數 INFO 要求別名 `#5` (`MolagBal`) 上的 `GetIsAliasRef == 1`。
- 開啟行同時要求滿足任務 `06F53C` 上的 `GetStage == 50`。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`073201 zzzCHMeQ07MolagB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:935) | `073202` | 無 | `GetStage == 50`; `GetIsAliasRef alias #5` | 「多麼沒用的肉偶……我本來還以為它不錯……」 |
| [`073203 zzzCHMeQ07MolagB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:938) | `073204` | 無 | `GetIsAliasRef alias #5` | 提示：「我想起你了，吸血鬼。」 回應：「哦，你想起我了。但局面不會因此改變。異類注定要在這片荒野中腐爛。不過，只有一條路值得稱許。向我們純白地屈服。用靈魂把那塊石頭填滿。」 |
| [`073205 zzzCHMeQ07MolagB01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:942) | `073206` | `Goodbye`, `SayOnce` | `GetIsAliasRef alias #5`; VMAD `CHMeq07_TIF__02073206.Fragment_0` 結束時 | 提示：「我知道了，讓我離開這裡。」 回應：「你終於找到我了嗎。按約定，我會讓異類離開這裡。我會稍微改造你的心智。」 |
| [`073207 zzzCHMeQ07MolagB01T04`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:945) | `073208` | `SayOnce` | 玩家持有 [`071CE2 zzzCHEyeOfMarukh`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1006) 且數量 > 0; `GetIsAliasRef alias #5` | 提示：「你也曾經是人。你吃了什麼？（馬魯克之眼）」 回應：「異類的眼睛似乎比凡人看得更遠。那麼，究竟如何呢？過去吃過什麼之類的事，我已經不太記得了。比起那個，讓我們得到答案吧。是屈服，還是死亡？」 |
| [`073209 zzzCHMeQ07MolagB01T05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:949) | `07320A` | `Goodbye`, `SayOnce` | `GetIsAliasRef alias #5`; VMAD `CHMeq07_TIF__0207320A.Fragment_0` 結束時 | 提示：「我會死在這裡。Ikanuzo 想要的是異類。」 回應：「好吧，若你能離開這片腐朽之地。」 |

翻譯備註：
- [`073203`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:940) 中的 `White submission` 語意不明；暫且直譯為「純白地屈服」。
- [`073209`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:949) 中的 `Ikanuzo` 需要驗證；很可能是誤傳/在地化的專有名詞或短語。

## 相關紀錄 (Related Records)

根據 `infodiag`，這些並非全屬於任務 `06F53C` 的一部分，但它們屬於馬魯克/艾萊西亞背景資訊，在完整重建中應進行交叉連結。

NPCs：
- [`05ADEF zzzCHMarukhMemory` - 馬魯克 (Marukh)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1046)
- [`11D025 zzzCHMarukh` - 馬魯克 (Marukh)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:516)
- [`0708BE zzzCHStAlessiaMemoryGhost` - 艾萊西亞 (Alessia)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1072)
- [`13206F zzzCHStAlessia` - 艾萊西亞 (Alessia)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:559)

物品：
- [`071CE2 zzzCHEyeOfMarukh` - `[*] Eye of Marukh`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1006)
- [`080D21 zzzCHSkinMarukh`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:219)
- [`500DC4 zzzCHSkinImgaHumanMarukh`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:411)
- [`500DC6 zzzCHArmorImgaMonkMarukh` - 猿人僧侶長袍 (Imga Monk Robe)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:413)

書籍：
- [`12905F zzzCHBookESOIllsuionDeath "The Illusion of Death"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:131)

## 相關書籍翻譯 (Related Book Translation)

[`12905F zzzCHBookESOIllsuionDeath "The Illusion of Death"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:131)

CLI：
- `booktext Vigilant.esm 0x12905F`
- 結果：失敗並提示 `could not extract English strings`；因此來源使用已提取出的 `game-data` 文本。

翻譯：

```text
先知 Marukh 與 Alessia 之靈相遇的殘篇記述。

……後來，因為他曾玩弄猿女 Dulsa (杜爾莎)，
Maruhk [原文如此] 便在石草原上度過他的百年懺悔。
他的視力被灼毀，舌頭腫脹，皮毛斑駁，
左手拇指永遠指向塔之星辰。Al-Esh 的影子也不斷對他說話，
那些鋸齒般的言語刮擦著他的概念器官，透過苦難將他帶向智慧。

他以自己的猿血，在乞求峭壁上用符文記下她的話；
血中的火焰把七十七條不屈教義刻進石面。
雖然這勞作耗盡了他，甚至吞噬了他的本質，他仍不吝惜自己，
因為他知道死亡是一種幻象。Al-Esh 雖已死去，不仍以刀刃般的話語存續嗎？
Pelin-Al (佩林納爾) 雖也在 Umar-Il (烏瑪瑞爾) 之死時死去，不也見證了她的死亡嗎？
於是 Maruhk 明白了正確抵達之道：獻身於正命與 Ehlnofic 廢止者，將存續於死亡幻象之外。
因為確實如此，驅逐腐化的意志甚至能征服 阿凱 (Arkay) 的循環。
```

基於原始資料對記憶 07 的連結點：
- `Dulsa` 出現在書籍來源與場景主題 [`0708D4`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:911) 中。
- `Al-Esh` / 艾萊西亞 (Alessia) 連結至分支 [`0731F5-0731FD`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:920)。
- `Seventy-Seven Inflexible Doctrines` (七十七條不屈教義) 連結至提取出的書籍以及在舊原始提取中發現的馬魯克相關對話；在用於最終敘事之前需要直接的來源連結。
- 書中的身體苦難與場景主題鏈 [`0708C8-0708D2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:896) 相匹配：模糊的視力、腫脹的舌頭、脫落的毛髮以及即將到來的死亡。

## 重建筆記 (Reconstruction Notes)

以原始資料為基礎：
- 此記憶由 [`06F53C zzzCHMemoryQuest07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:101) 表示，目標為 [`Where do the ape sleep?`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:102)。
- 它包含四個 `SCEN` 紀錄（`0708C7`, `0708CC`, `0708D1`, `0708D6`），透過別名 `TA01` 與 `TA02` 編排短促的獨白。
- 它包含兩個自定義對話分支：
  - 艾萊西亞 (Alessia) 別名 `#6`，開啟行限制為階段 40。
  - 莫拉格·巴爾 (Molag Bal) 別名 `#5`，開啟行限制為階段 50。
- 玩家選擇以 `Goodbye/SayOnce` 結束處存在 VMAD 片段，顯示它們很可能推進狀態或觸發結果。確切的 Papyrus 行為在此未反編譯。

開放驗證：
- 若存在來源或反編譯路徑，檢查腳本 `CHMeq07_TIF__020731FC`, `CHMeq07_TIF__02073206`, `CHMeq07_TIF__0207320A`；
- 若有更豐富的別名傾印可用，直接檢查 QUST 別名；
- 若空間編排很重要，檢查 `StartMarker`, `EndMarker`, `Bard`, `TA01`, `TA02` 以及引導標記的儲存格/參考；
- 若除了對話條件外的遊戲功能很重要，檢查 [`zzzCHEyeOfMarukh`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1006) 的物件/物品紀錄細節。
