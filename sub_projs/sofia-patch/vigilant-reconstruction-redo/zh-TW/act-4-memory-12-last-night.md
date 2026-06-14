# 第四章記憶 12 - 昨夜 (Last Night)

狀態：重構切片 (鏡像 07 模板)。基於來源、連結優先，非劇情摘要。

來源方針：
- 原始語句連結回抽取的來源文件，而非全文複製。
- 僅在需要解釋翻譯問題時才出現短小的來源片段。
- `SCEN` 編排來自 CLI 診斷，因為抽取的 `dialogue.md` 僅保留場景話題文本，而非場景階段/動作。
- Mod 的來源英文存在大量拼寫錯誤（例如 `Wellcome`, `Perrif`, `Paravant`）；保留原樣並加上 `Note:` 標註，而非默默修正。

## 任務紀錄 (Quest Record)

[`2BC395 zzzCHMemoryQuest12 "Last Night"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:307)

CLI：
- `questdiag Vigilant.esm 0x2BC395`
- `infodiag Vigilant.esm 0x2BC395`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x2BC395`
- EditorID: `zzzCHMemoryQuest12`
- 名稱: `Last Night`
- 標誌: `RunOnce`
- 優先級: `90`
- 類型: `Misc`
- 過濾器: `CH\`

來自 `questdiag` 的階段：

| 階段 | 標誌 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 5 | 無 | 空 |
| 10 | 無 | 空 |
| 20 | 無 | 空 |
| 30 | 無 | 空 |
| 40 | 無 | 空 |
| 50 | CompleteQuest | 空 |
| 60 | 無 | 空 |
| 300 | 無 | 空 |
| 310 | CompleteQuest | 空 |
| 320 | 無 | 空 |
| 999 | ShutDownStage | 空 |

任務目標：
- ESM 中有 0 個目標 (`questdiag` 列印 `Objectives (0)`)。無任務日誌目標文本；本記憶完全透過場景 + 兩個自定義分支台詞編排。

## 別名 / 編排骨幹 (Alias / Staging Backbone)

以下兩個 `SCEN` 紀錄共用相同的主機任務以及相同的四個別名。

主機任務：
- [`2BC395 zzzCHMemoryQuest12`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:307)

來自 `scenediag` 的主機任務別名：

| 別名 | 名稱 | 填充 | NPC 紀錄 |
|---:|---|---|---|
| 0 | `Alessia` | 唯一演員 `2BC383:Vigilant.esm` | [`2BC383 zzzCHMemoryStAlessiaOld "Alessia"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:814) |
| 1 | `Pelinal` | 唯一演員 `2BC37F:Vigilant.esm` | [`2BC37F zzzCHMemoryPelinal02 "Pelinal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:785) |
| 2 | `Akatosh` | 唯一演員 `2BC376:Vigilant.esm` | [`2BC376 zzzCHMemoryAkatosh "???????"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:776) |
| 3 | `Bull` | 唯一演員 `2BC389:Vigilant.esm` | [`2BC389 zzzCHMemoryMorihaus02 "Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:815) |

基於來源的映射：
- 根據 NPC 紀錄名稱，別名 `Bull` 是 **莫里豪斯 (Morihaus)** (有翼牛頭人，艾萊西亞的配偶)；別名名稱 `Bull` 與之一致。
- 別名 `Akatosh` 的 NPC 紀錄 `2BC376` 顯示名稱為 `???????` (來源中為空白/遮蔽)；別名名稱 `Akatosh` 是唯一的命名證據。

推論：
- `Pelinal` 與 `Alessia` 承擔了好場景中主要的重逢對話；`Bull` (莫里豪斯) 與 `Akatosh` 各擁有一行自定義分支台詞。這是根據場景動作中的別名使用情況，加上兩個自定義 INFO 的 `GetIsAliasRef` 條件（別名 `#3` Bull，別名 `#2` Akatosh）推斷而出的。
- 記憶的主體 = 故事末期與 **艾萊西亞 ("Perrif") 重逢的佩林納爾·白斯特拉克**；「昨夜」/ 告別的框架（推論）由好場景中的台詞 `"It is time to say goodbye, Perrif"` 所支撐。

## 場景紀錄 (Scene Records)

場景紀錄在 `game-data` 中並非完整紀錄；文本行連結至 `dialogue.md`，而階段/動作則來自 `scenediag`。兩個場景皆由任務 `2BC395` 擁有。

### 2BD6CB zzzCHMeQ12Sc01 (好場景：佩林納爾與艾萊西亞重逢)

CLI：
- `scenediag Vigilant.esm 0x2BD6CB`

編排：
- 主機任務：[`2BC395 zzzCHMemoryQuest12`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:307)
- 標誌：無
- 場景演員：別名 `#0` (`Alessia`, `DeathEnd, DialoguePause`) 與別名 `#1` (`Pelinal`, `DialoguePause`)。
- 階段：14 個（每個 0 開始條件, 1 完成條件；階段 9 與 11 有 2 個完成條件）。
- 動作：共 18 個 —— 佩林納爾 (演員 `#1`) 與 艾萊西亞 (演員 `#0`) 交替進行 `Dialog` 動作，並由佩林納爾的 `Package` 動作與一個 `Timer` 構成框架。

對話動作 (階段 → 說話者 → 話題 → 情緒)：

| 階段 | 說話者 | 話題 | 情緒 | 來源 |
|---:|---|---|---|---|
| 1 | 佩林納爾 `#1` | `2BD6CD` | Sad | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2627) |
| 2 | 艾萊西亞 `#0` | `2BD6CF` | Happy | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2630) |
| 3 | 艾萊西亞 `#0` | `2BD6D1` | Neutral | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2633) |
| 4 | 佩林納爾 `#1` | `2BD6D3` | Neutral | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2636) |
| 5 | 艾萊西亞 `#0` | `2BD6D5` | Happy | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2639) |
| 6 | 佩林納爾 `#1` | `2BD6D7` | Fear | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2642) |
| 7 | 艾萊西亞 `#0` | `2BD6D9` | Happy | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2645) |
| 8 | 佩林納爾 `#1` | `2BD6DB` | Happy | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2648) |
| 9 | 艾萊西亞 `#0` | `2BD6DD` | Sad | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2651) |
| 9 | (Timer 5s, 演員 `#0`) | — | — | — |
| 10 | 佩林納爾 `#1` | `2BD6DF` | Neutral | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2654) |
| 11 | 艾萊西亞 `#0` | `2BD6E2` | Happy | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2657) |
| 12 | 佩林納爾 `#1` | `2BD6E5` | Sad | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2660) |

翻譯（好場景，按階段順序）：
- [`2BD6CD` / INFO `2BD6CE`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2627): 「Perrif……」
  - 筆記：`Perrif` 是艾萊西亞 (Paravania / Al-Esh) 的早期稱號；保留原樣。
- [`2BD6CF` / INFO `2BD6D0`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2630): 「歡迎回來。我從沒想過，最後還能再見到你。」
  - 筆記：來源 `Wellcome` 是 `Welcome` 的拼寫錯誤；保留原樣。
- [`2BD6D1` / INFO `2BD6D2`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2633): 「所以……怎麼樣了？你找到她了嗎？」
- [`2BD6D3` / INFO `2BD6D4`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2636): 「終於，我找到她了。我這就去把她接來。」
- [`2BD6D5` / INFO `2BD6D6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2639): 「我盼著你能見到她。」
- [`2BD6D7` / INFO `2BD6D8`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2642): 「還是不明白。」
  - 筆記：來源 `Still don't get it` 語意不明（指誰或什麼事不明）；採直譯。待驗證。
- [`2BD6D9` / INFO `2BD6DA`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2645): 「我想是吧。」
- [`2BD6DB` / INFO `2BD6DC`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2648): 「你或許……」
- [`2BD6DD` / INFO `2BD6DE`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2651): 「…………」
- [`2BD6DF` / INFO `2BD6E0`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2654): 「……該道別了，Perrif。」
- [`2BD6E2` / INFO `2BD6E3`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2657): 「再見了。」
  - 筆記：來源 `See you again`；亦可譯為「後會有期」。
- [`2BD6E5` / INFO `2BD6E6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2660): 「……再見。」

### 2BD6F2 zzzCHMeQ12BadScene (壞場景：阿卡托什遣走玩家)

CLI：
- `scenediag Vigilant.esm 0x2BD6F2`

編排：
- 主機任務：[`2BC395 zzzCHMemoryQuest12`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:307)
- 標誌：無
- 場景演員：僅有別名 `#2` (`Akatosh`, `DeathEnd, DialoguePause`)。
- 階段：6 個（階段 0 有 2 個完成條件；其餘各 1 個）。
- 動作：共 6 個 —— 一個 `Timer` (3s) 然後是五個 `Dialog` 動作，皆由演員 `#2` (阿卡托什) 配音，情緒皆為 `Neutral`，且 `HeadtrackPlayer`。

對話動作：

| 階段 | 說話者 | 話題 | 來源 |
|---:|---|---|---|
| 1 | 阿卡托什 `#2` | `2BD6F3` | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2669) |
| 2 | 阿卡托什 `#2` | `2BD6F5` | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2672) |
| 3 | 阿卡托什 `#2` | `2BD6F7` | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2675) |
| 4 | 阿卡托什 `#2` | `2BD6F9` | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2678) |
| 5 | 阿卡托什 `#2` | `2BD6FB` | [連結](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2681) |

翻譯（壞場景）：
- [`2BD6F3` / INFO `2BD6F4`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2669): 「哦，你是從蜥蜴的腹中而來。真有意思。」
  - 筆記：`lizard's stomach` (推論) 指的是玩家透過 VIGILANT 框架故事中的魔神/冷港通道到達此地；採直譯。待驗證。
- [`2BD6F5` / INFO `2BD6F6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2672): 「你不屬於這一側。我能感覺到你身上的傷。」
- [`2BD6F7` / INFO `2BD6F8`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2675): 「你對我們無害，但你不該見到她。她終於能安息了。」
- [`2BD6F9` / INFO `2BD6FA`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2678): 「而且，我不知道是誰把你帶到這裡來的，但你不該這樣看著。」
  - 筆記：來源 `you should not see like this` 語法錯誤；採直譯。待驗證。
- [`2BD6FB` / INFO `2BD6FC`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2681): 「若你與此地相融，就再也回不去了。現在，回到你那一側去吧。」

## 自定義對話分支：牛 (莫里豪斯)

分支：
- `2BD6EA:Vigilant.esm` (`zzzCHMeQ12BullB01`), 視圖 `zzzCHMeQ12BullView` (`2BD6E9`)。

說話者條件模式：
- 單個 INFO 要求別名 `#3` (`Bull` = 莫里豪斯) 的 `GetIsAliasRef == 1`。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`2BD6EB zzzCHMeQ12BullB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2663) | `2BD6EC` | `Goodbye` | 別名 `#3` 的 `GetIsAliasRef` | 「我明白……她化作了星辰……但是，這很令人難過……太難過了……」 |

## 自定義對話分支：阿卡托什 (Akatosh)

分支：
- `2BD6EE:Vigilant.esm` (`zzzCHMeQ12AkatoshB01`), 視圖 `zzzCHMeQ12AkatoshView` (`2BD6ED`)。

說話者條件模式：
- 單個 INFO 要求任務 `2BC395` 的 `GetStage <= 60` 且別名 `#2` (`阿卡托什`) 的 `GetIsAliasRef == 1`。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`2BD6EF zzzCHMeQ12AkatoshB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2666) | `2BD6F0` | `Goodbye` | `GetStage <= 60`; 別名 `#2` 的 `GetIsAliasRef` | 「Paravant，循著群星而行……我記得，你的雙眼曾如流星般燃燒。」 |

翻譯筆記：
- `Paravant` 是佩林納爾 (Pelin-Al / Paravant) 的早期稱號；來源拼寫為 `Paravant`。保留原樣。該台詞呼應了樞紐任務 `zzzCHMemoryGuide` 的目標 120 [`失明的雙眼能如流星般閃耀`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:312) (狄蘭·湯馬斯)，確認了跨任務的連結。
- `GetStage <= 60` 門檻將此阿卡托什台詞限制在 **310 之前（好路徑）的時間窗**：僅在任務未推進到 300 區段時可用。（推論）

## 重構筆記 (Reconstruction Notes)

基於來源：
- 本記憶為 [`2BC395 zzzCHMemoryQuest12 "Last Night"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:307)，共 12 個階段，**0 個任務日誌目標**（在 `quests.md` 中僅有標題）。
- 包含**兩個** `SCEN` 紀錄：
  - `2BD6CB zzzCHMeQ12Sc01` —— **好 / 重逢場景**：佩林納爾（呼喚 "Perrif" 者）遇見艾萊西亞，14 個階段，結束於 `"It is time to say goodbye, Perrif"` → `"See you again"` → `"........bye"`。
  - `2BD6F2 zzzCHMeQ12BadScene` —— **壞 / 遣走場景**：阿卡托什獨自告訴玩家他們不屬於這裡，「你不該見到她」，「回到你那一側去」。
- 包含**兩個**自定義對話分支，每個皆為單個 `Goodbye` INFO：
  - 牛 (莫里豪斯) 別名 `#3` —— 哀悼艾萊西亞化作星辰。
  - 阿卡托什別名 `#2`，受限於 `GetStage <= 60` —— 對 "Paravant" 說出關於流星雙眼的台詞。
- **無書籍**由本任務擁有或透過文本連結（`find zzzCHMeQ12` 未返回 BOOK 紀錄；場景中未引用書籍）。

分支結果映射 (50 vs 310)：
- 兩個 `CompleteQuest` 階段分別為 **50** 與 **310**，符合索引中兩波段業障特徵。
- 極性（推論，基於來源的型態）：階段 **50** 是透過 `Sc01`（溫暖的重逢 + 告別 → 「好/仁慈」結果：佩林納爾獲准見到艾萊西亞並道別）達成的；**300 區段 → 310** 路徑則運行 `BadScene`（阿卡托什擋住玩家 → 「壞/拒絕」結果：安息受到干擾，玩家被送回）。`GetStage <= 60` 阿卡托什分支門檻以及 60 / 300 / 310 / 320 階段佈局支撐了 50-好 與 310-壞 的分離，但觸發各個 `CompleteQuest` 的確切階段設定片段**尚未**解碼。視為：存在兩種結果；好 = 重逢 (50)，壞 = 被遣走 (310)。

開放驗證：
- 轉儲階段 50、60、300、310、320 的階段片段 / 場景結束腳本，以確認哪個場景驅動哪個 `CompleteQuest` 以及各授予什麼內容（物品 / 全局變數 / 世界變化）；
- 檢查來自 `find` 的命名 package (`zzzCHMeq12AlessiaSleep` `2BC39F`、`zzzCHMeq12PelinalStandbyAlessia` `2BD6CC`、`zzzCHMeq12PelinalStopToGoddbye` `2BD6E4`、`zzzCHMeq12PelinalBackToAetherius` `2BD6E7`、`zzzCHMeq12AkatoshBlockPlayer` `2BD6F1`、`zzzCHMeq12AkatoshWaitingPelinal` `2BD6FD`) —— 單憑 package EditorID 即可強力證實好/壞分離 (`PelinalBackToAetherius` / `AkatoshBlockPlayer`)，但此處未轉儲完整的 package 數據；
- 若敘事保真度重要，請針對下方標註的拼寫錯誤/錯亂語詞，對照已知的佩林納爾/艾萊西亞傳說參考資料。

已標註的來源詞語（保留原樣，標記 `Note:`/待驗證）：
- `Perrif` (艾萊西亞早期名稱)、`Paravant` (佩林納爾早期名稱) —— 歷史正確的傳說名稱，非拼寫錯誤。
- `Wellcome` (= Welcome)、`you should not see like this` (語法錯誤)、`Still don't get it` (語意不明)、`lizard's stomach` (框架故事到達，推論) —— 已在上文中逐行標註。
