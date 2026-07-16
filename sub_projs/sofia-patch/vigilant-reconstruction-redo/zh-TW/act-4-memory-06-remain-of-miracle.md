# 第 4 章記憶 06 - 奇蹟之遺 (Remain of Miracle)

狀態：重新製作切片（redo slice）。以原始資料為基礎，連結優先，並非劇情摘要。

來源方針：
- 原始對話行連結回提取的原始文件，而非全文複製。
- 僅在需要解釋翻譯問題時顯示短小的原始片段。
- 此任務**不擁有任何 `SCEN` 紀錄**（參見重建筆記）；這是一個純對話式的審問記憶，因此沒有場景編排部分。

## 任務紀錄 (Quest Record)

[`06A23B zzzCHMemoryQuest06 "Remain of Miracle"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:371)

CLI：
- `questdiag Vigilant.esm 0x06A23B`
- `infodiag Vigilant.esm 0x06A23B`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務中繼資料：
- FormID：`Vigilant.esm:0x06A23B`
- EditorID：`zzzCHMemoryQuest06`
- 名稱：`Remain of Miracle`
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
| 30 | CompleteQuest | 空白 |
| 40 | 無 | 空白 |
| 999 | ShutDownStage | 空白 |

目標 (Objective)：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:372) | 祭司在斷裂的塔中發笑 |

目標物 (Objective targets)：
- ESM 中有 1 個目標物，0 個條件。
- 目前的 CLI 輸出未印出目標物參考（target ref）；若目標位置很重要，則需要更深入的 QUST 目標物傾印。

## 主角 (Subject)

- 透過主題 EditorID 確認主角：每個擁有的主題均為 `zzzCHMeQ06Pepe…`，且開啟行提示詞為 [`"Are you Pepe, Inquisitor of Alessian Order?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:862)。這是關於 **Pepe** 的記憶。
- 講者 NPC（推論）：[`06A230 zzzCHInquisitorPepeMemory3`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1069) —— FormID 位於任務 `06A23B` 之前的一筆紀錄，名稱為「Inquisitor Pepe」。別名 `#0` 上的講者條件 `GetIsAliasRef == 1`（見下文）指向此演員；目前的 CLI 未印出別名→參考的填充項，因此確切的別名-0 參考為 **(推論)**，有待 QUST 別名傾印。
- 相關 Pepe NPC 變體：[`12BF48 zzzCHInquisitorPepeMemory "Inquisitor Pepe"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:558), [`05ADFD zzzCHInquisitorPepeMemory2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1044), [`081E46 zzzCHInquisitorPepe "Inquisitor Pepe"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1065)。
- 根據索引，Pepe 亦出現在 **MeQ05 Ada Bal** ([`05AE03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)) 中；此切片僅涵蓋 MeQ06。

## 對話視圖 / 分支 (Dialogue View / Branches)

此任務擁有一筆 `DialogView` 與兩筆 `DialogBranch` 紀錄（來自 `find zzzCHMeQ06`）：

- 視圖：`06B54C zzzCHMeq06PepeView`
- 分支 B01：`06B54D zzzCHMeq06PepeB01` —— 包含 7 個主題的審問樹。
- 分支 B02：`06B55C zzzCHMeQ06PepeB02` —— 單行的分支。

`infodiag 0x06A23B` 確認所有 8 個 INFO 均由任務 `06A23B` 擁有。**每個** INFO 上的講者條件均為別名 `#0` 上的 `GetIsAliasRef == 1`；這是一個單一講者 (Pepe) 的記憶，沒有第二個講者別名，也沒有好/壞結局的別名分歧。

### 分支 B01：審問 (`06B54D zzzCHMeq06PepeB01`)

玩家驅動的審問。所有主題的優先級均為 `50`。條件依據 `infodiag`。

| 主題 | INFO | 標記 | 條件 | 提示 → 回應（翻譯） |
|---|---|---|---|---|
| [`06B54E zzzCHMeq06PepeB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:862) | `06B54F` | 無 | `GetStage == 10`; `GetIsAliasRef alias #0` | 提示：「你就是阿萊西亞教團 (Alessian Order) 的審判官 Pepe 嗎？」 回應：「沒錯。我的樣貌已經變了不少。那麼，野蠻的科洛維亞人 (Colovian) 想從這老頭身上問出什麼？」 |
| [`06B550 zzzCHMeQ06PepeB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:865) | `06B551` | 無 | `GetIsAliasRef alias #0` | 提示：「石頭呢。你把它藏在哪裡？」 回應 1：「石頭，還是當時的傭兵。過了數百年，唯獨人們的愚蠢似乎一成不變。」 回應 2：「石頭已經不在這世上了。它早就被帶走了。這全是拜你們所賜。」 備註：回應 1 原文 `Stone or was still mercenary. Hundreds of years passed since it will` 文法崩壞，譯文為近似意譯；待驗證。 |
| [`06B552 zzzCHMeQ06PepeB01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:869) | `06B553` | 無 | `GetIsAliasRef alias #0` | 提示：「什麼？」 回應 1：「這是由你們引起的戰爭。成千上萬人的血流遍東西兩方，那石頭終於得到了滿足。」 回應 2：「那是耗費了漫長時間、令人畏懼之物。瘟疫……如今在內戰中，也已將成千上萬的靈魂折進那塊石頭裡。」 回應 3：「你們的王也會無法忍受、想要它吧？石頭沒了。它去到了無人能及之處。」 備註：原文 `Plague, would have folded also go billion of the soul` 與 `civil war n` 為破碎機翻；譯文取其大意，待驗證。 |
| [`06B554 zzzCHMeq06PepeB01T04`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:874) | `06B555` | 無 | `GetIsAliasRef alias #0` | 提示：「把它帶走的人，叫什麼名字？」 回應：「莫拉格·巴爾 (Molag Bal)。他是 Spooky Togake 之王。他在日蝕之日降臨此塔，從我們手中奪走了石頭。」 備註：`Spooky Togake` 疑為被誤音譯/在地化的專有名詞（可能是 Coldharbour 之類），待驗證。 |
| [`06B556 zzzCHMeq06PepeB01T05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:877) | `06B557` | 無 | `GetIsAliasRef alias #0` | 提示：「就算那是真相——石頭究竟在哪？」 回應 1：「真相就是真相。Adabaru，連同它的仿製品我都失去了。人們因此得到了自由，他們被解放了。」 回應 2：「Shezaru 出現了……」 備註：回應 2 原文 `Shezaru appeared, but should give me bouncing the neck of you guys are after. Ikanu anything he wanted in the other` 嚴重崩壞、無法可靠重建；保留原文，待驗證。`Adabaru`、`Shezaru`、`Ikanu` 為專有名詞。 |
| [`06B558 zzzCHMeq06PepeB01T06`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:881) | `06B559` | 無 | `GetIsAliasRef alias #0` | 提示：「石頭在哪？明早我們就要處決你。」 回應 1：「我們守著那石頭太久了。如今擺在你們面前的，不過是一具沒有靈魂的空殼。」 回應 2：「我不會說壞話。……無論如何，那石頭都成了眼前這怪物的教訓。」 備註：原文 `The Tasukaru to be willing to do so` 與回應 2 整句機翻崩壞；譯文取近似大意，待驗證。`Tasukaru` 疑為專有名詞或誤譯。 |
| [`06B55A zzzCHMeq06PepeB01T07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:885) | `06B55B` | `Goodbye` | `GetIsAliasRef alias #0`; VMAD `CHMeq06_TIF__0206B55B.Fragment_0` 結束時 | 提示：「審問到此為止。」 回應：「結束，還是另一回事？這樣也好。從此我不必再聞科洛維亞人那帶著敵意的臭氣了。」 備註：原文 `The end or the other? It was good. From time I no enemy smelly breath Colovian people` 文法崩壞，譯文為近似，待驗證。 |

### 分支 B02：重複進入守衛 (`06B55C zzzCHMeQ06PepeB02`)

| 主題 | INFO | 標記 | 條件 | 回應（翻譯） |
|---|---|---|---|---|
| [`06B55D zzzCHMeQ06PepeB02T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:888) | `06B55E` | `Goodbye` | `GetStage == 20`; `GetIsAliasRef alias #0` | 「什麼？審問不是已經結束了嗎？」 |

- 此單行、無提示詞的 `Goodbye` INFO 受限於階段 `GetStage == 20` —— 它在玩家於審問結束後再次與 Pepe 對話時觸發。這是一個狀態守衛，而非另一個故事分支。

## 線性或分支判定 (Linear-or-branched verdict)

**判定：線性 (LINEAR)（在階段 30 處單一完成），並非因果分支 (karma-branched)。**

證據：
- `questdiag` 顯示恰好有一個 `CompleteQuest` 階段 (30)；在 100–350 頻帶內無第二個 `CompleteQuest`。這打破了[索引](act-4-memory-index.md)中記錄的關於分支記憶的雙頻帶好/壞特徵（例如 MeQ07 的 70/150 分裂）。
- 不存在第二個講者別名，也不存在受階段限制的替代講者分支。在 MeQ07 中，分支分裂被實作為 `GetStage==40`→阿萊西亞別名 `#6` 對比 `GetStage==50`→莫拉格·巴爾別名 `#5`。而在這裡，**所有 8 個 INFO 共享同一個別名 (`#0`)** 與同一個講者 (Pepe)；僅有的階段條件是 `GetStage==10`（B01 開啟行）與 `GetStage==20`（B02 重複進入守衛） —— 為序列關卡，而非互斥的結果分支。
- 兩個 `DialogBranch` 紀錄並非好/壞結局的替代方案：B01 是審問樹，B02 是完成後的重複對話守衛。
- 此任務未在其他地方透過對話條件實作分支：`infodiag 0x06A23B` 僅回傳這 8 個擁有的 INFO，且 `find zzzCHMeQ06` 未回傳額外的主題、場景或視圖。

警告（推論）：`Goodbye` 主題 `06B55A`/`06B55B` (`CHMeq06_TIF__0206B55B.Fragment_0`) 上的 VMAD 片段在此處未被反編譯；其 Papyrus 很可能將任務推進至其唯一的完成點。沒有證據顯示它會導向第二個結果。若分支極性變得很重要，可透過反編譯該片段來確認。

## 重建筆記 (Reconstruction Notes)

以原始資料為基礎：
- 此記憶為 [`06A23B zzzCHMemoryQuest06 "Remain of Miracle"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:371)，目標為 [`Priest laughs in the broken tower`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:372)。
- 它是最短的記憶任務：6 個階段，在 30 處單一 `CompleteQuest`，在 999 處 `ShutDownStage`。
- 它**不擁有任何 `SCEN` 紀錄**：`find Vigilant.esm zzzCHMeQ06` 回傳 8 個主題 + 2 個分支 + 1 個視圖，除此之外別無他物；在視圖 FormID 上執行 `scenediag` 確認其「不是場景 (is not a Scene)」。這是一個純粹的玩家對 Pepe 的審問，而非像 MeQ07 那樣的編排獨白記憶。
- 整個任務由一個自定義 DialogView (`06B54C zzzCHMeq06PepeView`) 組成，包含 B01（7 個主題的審問）與 B02（1 行的重複進入守衛）。
- 背景故事內容（來自 INFO 回應）：Pepe 曾是阿萊西亞教團的審判官，負責守護一塊吞噬靈魂的石頭 (`Adabaru`)；戰爭為其餵食了成千上萬的靈魂；在日蝕之日，莫拉格·巴爾降臨此塔並奪走了石頭，留下 Pepe 成為一個無魂的「空殼」。

開放驗證：
- 傾印 QUST 別名以確認別名 `#0` 填充了 [`06A230 zzzCHInquisitorPepeMemory3`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1069)，並印出目標 0 的目標物參考 / 位置（「斷裂的塔」）；
- 反編譯（於 `06B55B` 上的）片段 `CHMeq06_TIF__0206B55B` 以確認它設置了單一完成，且未授予替代結果；
- 交叉檢查 Pepe 與 **MeQ05 Ada Bal** ([`05AE03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)) 的重疊情況，以確定哪一個是主要的 Pepe 記憶；
- 解析標註為錯亂的專有名詞：`Adabaru`, `Shezaru`, `Ikanu`/`Ikanuzo`, `Spooky Togake`, `Tasukaru` —— 很可能是音譯錯誤或在地化名稱，需要背景故事或遊戲內的交叉參考；
- 在 `find`/`infodiag` 中沒有與此任務關聯的書籍；未執行 `booktext`（不擁有 BOOK FormID）。
