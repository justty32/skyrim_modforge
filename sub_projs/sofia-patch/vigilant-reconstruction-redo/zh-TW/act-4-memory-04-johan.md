# 第 4 章記憶 04 - 愚者 Johann (Johan the fool)

狀態：重新製作切片（redo slice），隊列位置 #1。以原始資料為基礎，連結優先，並非劇情摘要。

來源方針：
- 原始對話行連結回提取的原始文件，而非全文複製。
- 僅在需要解釋翻譯問題時顯示短小的原始片段。
- `SCEN` 舞台編排來自 CLI 診斷，因為提取的 `dialogue.md` 僅保留場景主題文本，不保留場景階段/動作。
- 此模組的英文是從日文機器翻譯而來，經常破碎。錯亂的專有名詞 / 短語保留原樣並標註 `Note: 待驗證`。

## 任務紀錄 (Quest Record)

[`140225 zzzCHMemoryQuest04 "Johan the fool"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)

CLI：
- `questdiag Vigilant.esm 0x140225`
- `infodiag Vigilant.esm 0x140225`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務中繼資料：
- FormID：`Vigilant.esm:0x140225`
- EditorID：`zzzCHMemoryQuest04`
- 名稱：`Johan the fool`
- 標記 (Flags)：`RunOnce`
- 優先級 (Priority)：`90`
- 類型 (Type)：`Misc`
- 過濾器 (Filter)：`CH\`

來自 `questdiag` 的階段 (Stages) (16)：

| 階段 | 標記 | 日誌 |
|---:|---|---|
| 0 | 無 | 空白 |
| 10 | 無 | 空白 |
| 20 | 無 | 空白 |
| 30 | 無 | 空白 |
| 40 | 無 | 空白 |
| 50 | 無 | 空白 |
| 60 | **CompleteQuest** | 空白 |
| 70 | 無 | 空白 |
| 95 | 無 | 空白 |
| 100 | **CompleteQuest** | 空白 |
| 110 | 無 | 空白 |
| 120 | 無 | 空白 |
| 121 | 無 | 空白 |
| 130 | 無 | 空白 |
| 140 | 無 | 空白 |
| 999 | ShutDownStage | 空白 |

目標 (Objective)：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:297) | 「死者在地下做夢。」 |
| | | 備註：來源 `Deads dream under the ground.` — `Deads` 為破碎英文（死者複數誤拼）；譯為「死者」。待驗證。 |

目標物 (Objective targets)：
- ESM 中有 1 個目標物，0 個條件。目前的 CLI 輸出未印出目標物參考（target ref）；若目標位置很重要，則需要更深入的 QUST 目標物傾印。

## 別名 / 編排骨幹 (Alias / Staging Backbone)

下方的三個 `SCEN` 紀錄共享相同的宿主任務與相同的 19 個別名表（`scenediag` 為每個場景印出的內容均相同）。場景動作 `ActorID=N` 索引至此表。

宿主任務：
- [`140225 zzzCHMemoryQuest04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)

來自 `scenediag` 的宿主任務別名 (19)：

| 別名 | 名稱 | 填充 |
|---:|---|---|
| 0 | `Simon` | uniqueActor [`140211 zzzCHBigBrother01Memory` - Simon](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:593) |
| 1 | `Tlass` | uniqueActor [`140212 zzzCHBigBrother02Memory` - Tlass](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:594) |
| 2 | `Priest` | uniqueActor [`140220 zzzCHArkayPriestMemory` - Arkay Priest](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:599) |
| 3 | `Attendant01` | uniqueActor [`140215 zzzCHAttendantMMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:595) |
| 4 | `Attendant02` | uniqueActor [`140216 zzzCHAttendantFMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:596) |
| 5 | `Attendant03` | uniqueActor [`14021D zzzCHAttendantFElfMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:597) |
| 6 | `Attendant04` | uniqueActor [`140223 zzzCHAttendantFCatMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:602) |
| 7 | `Attendant05` | uniqueActor [`140222 zzzCHAttendantMMemory02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:600) |
| 8 | `Attendant06` | uniqueActor [`14021E zzzCHAttendantMAlikrMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:598) |
| 9 | `Bard` | 未由 `scenediag` 填充（未印出填充項） |
| 13 | `Martha` | uniqueActor [`140DF8 zzzCHMarthaGhoul` - Martha](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:574) |
| 16 | `Molag` | （別名於執行時填充；未印出填充項） |
| 10 | `MemoryMarker01` | forcedRef `13FC5D:Vigilant.esm` |
| 11 | `ReturnMarker` | forcedRef `140226:Vigilant.esm` |
| 12 | `BadEndMarker` | forcedRef `140DE8:Vigilant.esm` |
| 14 | `BrotherMarker` | forcedRef `140DE9:Vigilant.esm` |
| 15 | `FireMarker` | forcedRef `1413C5:Vigilant.esm` |
| 17 | `SlaverMemory` | （集合別名；未印出填充項） |
| 18 | `GuideMarker` | forcedRef `42E0B2:Vigilant.esm` |

推論：
- 記憶的主角「Johan/Johann」**並未**填充在別名中 —— 他是玩家在記憶中所扮演的角色；所有 NPC 都稱呼玩家為「Johann」。（推論，源於對話/場景行中呼格稱呼 Johann，而無別名被命名為 Johan。）
- `Simon` 與 `Tlass` 是兩個 `zzzCHMeQ04BrotherB01/B02` 分支的「兄弟」講者（別名 `#0` = Simon，長兄；`#1` = Tlass）。（推論，源於分支 INFO 條件 `GetIsAliasRef alias #0` / `#1`。）
- `Bard`（別名 `#9`）是偽裝的莫拉格·巴爾使者「Bal」，負責交付釘頭錘；`Molag`（別名 `#16`）是壞結局中的莫拉格·巴爾講者。（推論，源於分支 INFO 條件 + 場景演員 ID。）
- `BadEndMarker`（別名 `#12`）與 `FireMarker`（別名 `#15`），加上壞結局路線的 Packages [`1413BA zzzCHMeq4BrotherBacktoDark`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/) 與 `141F24` 「燒毀……你的家人至化為灰燼」，標誌著壞結局的縱火終局。（推論。）

## 場景紀錄 (Scene Records)

場景紀錄在 `game-data` 中未以完整紀錄形式存在；文本行連結至 `dialogue.md`，而階段/動作則來自 `scenediag`。`find` 為此任務回傳三個 `SCEN`：`140235`、`1413AB`、`1413D0`。

### 140235 zzzCHMeQ4FuneralScene

CLI：
- `scenediag Vigilant.esm 0x140235`

編排：
- 宿主任務：[`140225 zzzCHMemoryQuest04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)
- 演員 (9)：別名 `#0 Simon`、`#1 Tlass`、`#2 Priest`、`#3`-`#8` Attendant01-06；均為 `behaviorFlags=DeathEnd, DialoguePause`。祭司 (`#2`) 具有 `NoPlayerActivation`。
- 階段 (Phases)：8 個，每個具有 0 個開始條件 / 1 個完成條件。
- 動作 (Actions) (26)：祭司 (`#2`) 根據階段 0-3 朗誦悼詞行 (`140238`, `14023A`, `14023C`, `14023E`)；Simon (`#0`) 在階段 4/6/7 給予回應 (`140240`, `140243`, `140245`)；其餘為面向目標的頭部追蹤動作，對向祭司接著是對向 Simon（哀悼者）。Martha 的葬禮。

翻譯（祭司，接著是 Simon）：
- [`140238`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1886) (Neutral)：「阿凱 (Arkay)，生與死之神，我們將 Martha 交付於你手中，她已走完這段以希望為定數的生命之旅。」
  - 備註：來源 `the journey of life of the hope of life that is determined` 文法破碎；意譯。待驗證。
- [`14023A`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1889) (Neutral)：「願這女孩卸下一切重擔、離我們而去，並在光界 (Aetherius) 與聖者相會。」
  - 備註：來源 `whether` 為贅字；`in addition to meeting the saint` 意譯為「與聖者相會」。待驗證。
- [`14023C`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1892) (Neutral)：「願我們在離別之悲中，仍能與這女兒一同被引入 Akei (阿凱) 的環中，共享永恆之喜。」
  - 備註：`Akei` = Arkay 的另一拼法（本檔多處 Arkay/Arkei/Akei/Akei 混用）。待驗證。
- [`14023E`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1895) (Neutral)：「以那規定生命的 Arkei 之名……」
  - 備註：來源 `Arkei of life that is prescribed. The under the name of...` 破碎且未完。待驗證。
- [`140240`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1898) (Sad, Simon)：「以那規定生命的 Arkei 之名……」（與 `14023E` 同文，由 Simon 哀傷複誦）
- [`140243`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1901) (Sad, Simon)：「各位，感謝你們今日為 Martha 而來。Martha 想必也會對光界心懷感激。」
- [`140245`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1904) (Sad, Simon)：「我由衷感謝各位的慰問。非常、非常感謝。」

### 1413AB zzzCHMeQ4BadScene

CLI：
- `scenediag Vigilant.esm 0x1413AB`

編排：
- 宿主任務：[`140225 zzzCHMemoryQuest04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)
- 演員 (2)：別名 `#0 Simon`、`#1 Tlass`，兩者皆具有 `flags=NoPlayerActivation`。
- 階段：7 個。階段 0 有 2 個完成條件；其餘為 1 個。
- 動作 (13)：Simon (`#0`) 與 Tlass (`#1`) 在階段 1-5 圍繞釘頭錘交談 (`1413B0`, `1413B2`, `1413B4`, `1413B6`, `1413B8`)；階段 6 的 8 秒計時器結束場景。兄弟倆正對獲得的釘頭錘幸災樂禍 —— 這是**壞結局路線**的兄弟場景。（推論，源於 `BadScene` EditorID + 內容。）

翻譯：
- [`1413B0`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1940) (Happy, Simon)：「我做到了，我做到了。」
- [`1413B2`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1943) (Happy, Simon)：「釘頭錘、釘頭錘……找到了。就是這個，這把錘。」
- [`1413B4`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1946) (Happy, Tlass)：「我們辦到了，兄弟。只要有這個，我們什麼都做得到。」
- [`1413B6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1949) (Happy, Simon)：「沒錯。只要還記得那件事，剩下的就是這個。來，走吧。」
  - 備註：來源 `This is what remains even think of that` 破碎；意譯。待驗證。
- [`1413B8`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1952) (Happy, Tlass)：「等等，兄弟。」

### 1413D0 zzzCHMeQ04MolagScene

CLI：
- `scenediag Vigilant.esm 0x1413D0`

編排：
- 宿主任務：[`140225 zzzCHMemoryQuest04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)
- 視圖：[`1413CA zzzCHMeQ04MolagView`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/) (find)。
- 演員 (1)：別名 `#16 Molag`。
- 階段：4 個，每個具有 0 個開始條件 / 1 個完成條件。
- 動作 (5)：對 Molag 執行兩個 `Package` 動作（階段 0；階段 1-3），接著在階段 1-3 說出三行對話 (`141F22`, `141F24`, `141F26`)，包含 `HeadtrackPlayer` / `FaceTarget`。莫拉格·巴爾對 Johann 下達的最後指令 —— **壞結局**的報償。（推論，源於 `Molag` 別名 + 內容。）

翻譯（莫拉格·巴爾，均為 Neutral）：
- [`141F22`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1955)：「Johann，你為我們效力甚善。我要提出最後一個願望。」
  - 備註：來源 `I'll ask one last hope` — `hope` 疑為「請求/願望」。待驗證。
- [`141F24`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1958)：「燒光一切……？好。如你所願 —— 你建造的一切、你的家人，盡化灰燼。」
- [`141F26`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1961)：「現在，安睡吧。湮滅 (Oblivion) 應許永恆的安寧。」

## 自定義對話分支 (Custom Dialogue Branches)

`find` 為此任務回傳三個自定義對話分支，外加一個任務擁有的問候語分支。不支援分支層級的 `infodiag`（依分支 FormID）；下方的所有 INFO 數據均來自任務層級的 `infodiag Vigilant.esm 0x140225`。

### 問候語分支：zzzCHMeQ4GreetB01 (`140228`)

主題 [`140229 zzzCHMeQ4GreetB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1874)，6 個 INFO，每個皆為 `Goodbye`，各個侍從別名 `#3`-`#8` 一個 (`GetIsAliasRef`)。葬禮哀悼者的環境對話。僅連結，此處不完整重譯 —— 樣本：INFO `14022A` (Sad, 別名 #3)：「都已經十六歲了……唉。那 …… 不是嗎？前路還很長。」（備註：來源 `the ...... is not it?` 破碎留白。待驗證。）INFO `14022E` (Anger, 別名 #7) 點名 `Martha`：「就因一個人 Martha 失明，你到底都跑哪去了？」（備註：來源 `Nantes to one person Martha blind` 嚴重破碎；意譯。待驗證。）

### 兄弟分支 B01：zzzCHMeQ04BrotherB01 (`140231`)

講者：兄弟 Simon (`#0`) / Tlass (`#1`)。由任務 `140225` 上的 `GetStage <=` 進行**階段限制** —— 這些是早期的、完成前的慰問對話。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`140232 …BrotherB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1882) | `140233` | `Goodbye` | `GetStage <= 20`; `GetIsAliasRef alias #0` (Simon) | (Sad) 「Johann，別太過自責。」 |
| `140232` (T01) | `140234` | `Goodbye` | `GetStage <= 30`; `GetIsAliasRef alias #1` (Tlass) | (Sad) 「Johann。那天，酒館不算吵鬧，錯不只在你。我這做兄弟的也有責任。所以……」 |

### 兄弟分支 B02：zzzCHMeQ04BrotherB02 (`140249`)

講者：Simon (`#0`)，無階段限制。後期的「我們回家吧」拍點。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`14024A …BrotherB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1907) | `14024B` | 無 | `GetIsAliasRef alias #0` | (Fear) 「Johann，你還好嗎？」 |
| [`14024C …BrotherB02T02` 提示「哥哥……」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1910) | `14024E` | 無 | `GetIsAliasRef alias #0` | (Sad) 「我們回家吧。要下雨了。」 |
| [`14024D …BrotherB02T03` 提示「讓我一個人靜一靜」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1913) | `14024F` | `Goodbye` | `GetIsAliasRef alias #0`; VMAD `CHMeq4_TIF__0214024F.Fragment_0` 結束時 | (Sad) 「儘快回來。Martha 縱使病著，你的悲傷想必也傳得到她那裡。」 備註：來源 `Martha Kanashimuzo you surely reaches even sick` 含未翻譯的日文羅馬字 `Kanashimuzo`(悲しむぞ)；意譯。待驗證。 |

### 吟遊詩人分支 B01：zzzCHMeQ04BardB01 (`140803`) — 選擇分支

講者：別名 `#9 Bard`（莫拉格·巴爾使者「Bal」）。開啟行**受限於階段 `GetStage == 50`**。這是在此分支中 Johann 決定是否接受釘頭錘。選擇「給我釘頭錘」帶有一個 VMAD 片段，該片段也會觸發 `Fragment_1` `OnBegin`（狀態變更），而「走開」則帶有其自身的結束片段 —— 即此分支引導 60 與 100 的完成點。（推論，源於 VMAD 放置位置 + 兩個 `CompleteQuest` 階段。）

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`140804 …BardB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1916) | `140805` | 無 | `GetStage == 50`; `GetIsAliasRef alias #9` | (Happy) 「失去摯愛這種事，是非常令人哀傷的。阿凱 (Akei) 真是殘酷。」 |
| [`140806 …BardB01T02` 提示「你是誰？」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1919) | `140807` | 無 | `GetIsAliasRef alias #9` | (Happy) 「我只是個吟遊詩人。我名叫 Bal。要不要聽我唱一曲？你喜歡 Eroisa 與 Polydor 的故事嗎？」 備註：`Eroisa`/`Polydor` 為音譯人名，待驗證。 |
| [`140808 …BardB01T03` 提示「我沒那個心情」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1922) | `140809` | 無 | `GetIsAliasRef alias #9` | (Neutral) 「真可惜。沒這個心情的話，那也沒辦法。」 |
| [`14080A …BardB01T04` 提示「我要這鬼東西幹嘛？」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1925) | `14080B` | 無 | `GetIsAliasRef alias #9` | (Happy) 「錘。一把錘……我受我主之命，要把這把釘頭錘 (Mace) 交給你。」 備註：提示詞 `What I do use a hell ?` 破碎，疑為「我要這東西做什麼用？」。待驗證。 |
| [`14080C …BardB01T05` 提示「你想要我做什麼？」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1928) | `14080D` | 無 | `GetIsAliasRef alias #9` | (Happy) 「我希望你收集有罪之人的靈魂。我們需要數千個。你願意嗎？」 |
| [`14080E …BardB01T06` 提示「這樣一來，妹妹會回來嗎？」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1931) | `14080F` | 無 | `GetIsAliasRef alias #9` | (Happy) 「是的，當然。我主能讓她復生，因為他在阿凱之環之外。」 備註：提示詞中 `sister` 與正文常稱 Martha；此處關係詞（妹/姊）待驗證。 |
| [`140810 …BardB01T07` 提示「給我釘頭錘」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1934) | `140811` | `Goodbye` | `GetIsAliasRef alias #9`; VMAD `CHMeq4_TIF__02140811` (`Fragment_1` OnBegin, `Fragment_0` OnEnd) | (Happy) 「好的，好的。這把釘頭錘從一開始就是你的了。親愛的 Johann。」 |
| [`140812 …BardB01T08` 提示「走開」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1937) | `140813` | `Goodbye` | `GetIsAliasRef alias #9`; VMAD `CHMeq4_TIF__02140813.Fragment_0` 結束時 | (Happy) 「我明白了。那麼，若你改變心意，請到佈拉維爾 (Bravil) 來。我會帶著錘等你。」 |

## 相關紀錄 (Related Records)

這些是此任務別名與對話所引用的演員/物品（已透過 `scenediag` 別名填充與 `infodiag` / 物品文本驗證）。Johan 本身**無 NPC 紀錄** —— 他是玩家的角色。

NPCs（別名演員）：
- [`140211 zzzCHBigBrother01Memory` - Simon](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:593)
- [`140212 zzzCHBigBrother02Memory` - Tlass](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:594)
- [`140220 zzzCHArkayPriestMemory` - Arkay Priest](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:599)
- [`140DF8 zzzCHMarthaGhoul` - Martha](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:574)
  - 備註：EditorID `MarthaGhoul`（推論）：Martha 隨後成為食屍鬼 / 不死生物，與壞結局中的復活交易一致。待驗證 NPC 紀錄。
- Attendant01-06：[`140215`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:595), [`140216`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:596), [`14021D`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:597), [`140223`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:602), [`140222`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:600), [`14021E`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:598)
- `Bard`（別名 #9）與 `Molag`（別名 #16）：未印出 `scenediag` 填充項；為執行時填充的偽裝/魔族。吟遊詩人在 INFO `140807` 中自稱「Bal」。（推論。）

物品：
- [`00D9FC zzzAoMMq07MaceofMolagBal` - 莫拉格·巴爾的釘頭錘 (Mace of Molag Bal)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1013)
  - 由吟遊詩人交付的釘頭錘 (INFO `14080B`/`140811`)，並在 `BadScene` (`1413B2` "this mace") 中被幸災樂禍。EditorID 前綴為 `Mq07`（而非 `MeQ04`） —— 它是共用的莫拉格·巴爾釘頭錘神器，在此處重複使用，而非任務 04 私有的物品。（推論，源於 EditorID。）

## 相關書籍翻譯 (Related Book Translation)

此任務無擁有的書籍。`find zzzCHMeQ04` / `zzzCHMeQ4` 未回傳 `BOOK` 紀錄，且 `infodiag` 未列出書籍主題。（[`0B0825 zzzCHSlaverNote04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1319) 中的 「Bravil was burnt (佈拉維爾被燒毀)」一行在主題上與佈拉維爾縱火結局鄰近，但該紀錄為奴隸販子筆記，**並非**由 MeQ04 擁有 —— 根據來源方針排除。）未執行 `booktext`。

## 重建筆記 (Reconstruction Notes)

以原始資料為基礎：
- 此記憶為 [`140225 zzzCHMemoryQuest04 "Johan the fool"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:296)，目標為 [`Deads dream under the ground.`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:297)。
- 它擁有 3 個 `SCEN`：`140235` Funeral (葬禮，Martha 的葬禮，祭司 + Simon 的悼詞)、`1413AB` Bad (壞結局，Simon + Tlass 對釘頭錘幸災樂禍)、`1413D0` Molag (莫拉格·巴爾的「燒毀你的家人 / 沉睡」指令)。
- 它擁有 4 個對話分支：一個任務問候語 (`140228`，6 個哀悼者對話) 與三個自定義分支 —— 兄弟 B01 (`140231`，階段限制 `<=20`/`<=30` 的慰問行)、兄弟 B02 (`140249`，「我們回家吧」) 以及吟遊詩人 B01 (`140803`，提供釘頭錘的選擇，開啟行限制為 `GetStage==50`)。
- 玩家扮演「Johann」；每位講者都稱呼 Johann。不存在 Johann 的 NPC 紀錄。

如何選擇 60 與 100 分支（推論，基於原始資料結構）：
- 吟遊詩人分支 `140803` 僅在 `GetStage == 50` 時開啟並提供釘頭錘。終端選擇帶有 VMAD 片段：`140811` 「給我釘頭錘」在 **OnBegin** 觸發 `Fragment_1` 外加 OnEnd 觸發 `Fragment_0`（在對話行開始時進行狀態變更，而不僅是在結束時），而 `140812` 「走開」僅在 OnEnd 觸發 `Fragment_0`。這兩個終端選擇最可能是饋送至兩個 `CompleteQuest` 階段（60 與 100）的分歧點。
- **極性（哪個是好，哪個是壞）：** 儘管 CLI 輸出中未包含各個片段對應的確切階段編號，但內容是可以解讀的：
  - **壞結局 / 墮落結果** = 接受釘頭錘（「給我釘頭錘」，`140811`） → 收集有罪靈魂 → `BadScene` (`1413AB`) 兄弟倆奪取釘頭錘 → `MolagScene` (`1413D0`) 莫拉格·巴爾命令 Johann **燒毀他自己的家人至化為灰燼** (`141F24`) 並「沉睡」進入湮滅。這是縱火/墮落的終局（BadEndMarker `#12`, FireMarker `#15`）。（推論，極強 —— 內容明確。）
  - **好結局 / 拒絕結果** = 拒絕（「走開」，`140813`/`140812`） → 「如果你改變心意就來佈拉維爾」；無靈魂收割，無縱火。（推論。）
  - **階段編號分配**（60/100 之中哪一個是壞結局）無法單從 `questdiag`/`infodiag` 判定 —— 需要兩個 TIF 片段腳本的 `SetStage` 呼叫。下方標記為待辦事項。

開放驗證：
- 反編譯 / 檢查片段腳本 `CHMeq4_TIF__02140811`（吟遊詩人接受）、`CHMeq4_TIF__02140813`（吟遊詩人拒絕）、`CHMeq4_TIF__0214024F`（兄弟 B02 結束），讀取其 `SetStage`/`CompleteQuest` 目標，並**將 60 與 100 分配給好與壞結局**；
- 透過直接 QUST 別名傾印確認別名 `#9 Bard` 與 `#16 Molag` 的填充項 (uniqueActor 對比 forcedRef) —— `scenediag` 未印出它們；
- 如果有日文文本表可用，驗證 `Bal` (`140807`) / `Eroisa` / `Polydor` 專有名詞以及破碎英文提示詞（`What I do use a hell ?`, `14024F` 中的 `Kanashimuzo` 羅馬字, `Deads`）；
- 確認 Martha 的 `zzzCHMarthaGhoul` (`140DF8`) 紀錄狀態（她是否在壞結局路線中作為食屍鬼復活？）以及釘頭錘神器 `00D9FC` 的遊戲功能；
- 如果空間/縱火編排很重要，檢查 `BadEndMarker` (`140DE8`), `FireMarker` (`1413C5`), `BrotherMarker` (`140DE9`) 參考以及壞結局路線的 Packages `1413BA zzzCHMeq4BrotherBacktoDark`, `1413D1-1413D3` Molag Packages。
