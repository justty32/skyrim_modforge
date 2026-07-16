# 第 4 章記憶 05 - Ada Bal

狀態：重新製作切片（redo slice）。以原始資料為基礎，連結優先，並非劇情摘要。

來源方針：
- 原始對話行連結回提取的原始文件，而非全文複製。
- 僅在需要解釋翻譯問題時顯示短小的原始片段。
- `SCEN` 舞台編排來自 CLI 診斷，因為提取的 `dialogue.md` 僅保留場景主題文本，不保留場景階段/動作。
- 此模組是由日文機器翻譯而來；破碎的英文保留在來源欄位中，並標註 `Note: 待驗證`，而非將其平滑化。

## 任務紀錄 (Quest Record)

[`05AE03 zzzCHMemoryQuest05 "Ada Bal"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)

CLI：
- `questdiag Vigilant.esm 0x05AE03`
- `infodiag Vigilant.esm 0x05AE03`
- `find Vigilant.esm zzzCHMeQ05`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務中繼資料：
- FormID：`Vigilant.esm:0x05AE03`
- EditorID：`zzzCHMemoryQuest05`
- 名稱：`Ada Bal`
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
| 45 | 無 | 空白 |
| 50 | CompleteQuest | 空白 |
| 60 | 無 | 空白 |
| 120 | CompleteQuest | 空白 |
| 130 | 無 | 空白 |
| 140 | 無 | 空白 |
| 999 | ShutDownStage | 空白 |

目標 (Objective)：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:358) | 在月下，於亡者之上起舞。 |

目標物 (Objective targets)：
- ESM 中有 1 個目標物（`questdiag`: `objective[0] ... targets=1`, `target: flags=0 conds=0`）。
- 目前的 CLI 輸出未印出目標物參考（target ref）；若目標位置很重要，則需要更深入的 QUST 目標物傾印。

## 別名 / 編排骨幹 (Alias / Staging Backbone)

宿主任務的別名由 `scenediag` 在其擁有的一個 `SCEN` 上傾印。

宿主任務：
- [`05AE03 zzzCHMemoryQuest05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)

來自 `scenediag` 的宿主任務別名 (8)：

| 別名 | 名稱 | 填充 |
|---:|---|---|
| 0 | `Marukh` | uniqueActor [`05ADEF zzzCHMarukhMemory`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1046) |
| 1 | `Pepe` | uniqueActor [`05ADFD zzzCHInquisitorPepeMemory2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1044) |
| 2 | `StartMarker` | forcedRef `05ADFE:Vigilant.esm` |
| 3 | `EndMarker` | forcedRef `05AE04:Vigilant.esm` |
| 4 | `Player` | forcedRef `000014:Skyrim.esm` |
| 5 | `Adabal` | 未印出（CLI 輸出中無填充項） |
| 6 | `MemoryDulsa` | 未印出（CLI 輸出中無填充項） |
| 7 | `GuideMarker` | forcedRef `42E0B4:Vigilant.esm` |

推論：
- `Marukh` 別名 `#0` 與 `Pepe` 別名 `#1` 是兩個自定義分支使用的對話別名：馬魯克分支 INFO 要求滿足 `GetIsAliasRef alias #0`，佩佩分支 INFO 要求滿足 `GetIsAliasRef alias #1`（由 `infodiag` 確認）。
- `Adabal` 別名 `#5` 是記憶圍繞其展開的紅石物件（參見相關紀錄中的 [`05AE01 zzzCHAdabalMemory "Red Stone"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1004)）。CLI 未印出其填充項。（推論）
- `MemoryDulsa` 別名 `#6` 是馬魯克在其分支中稱呼的女性 Dulsa；CLI 未印出其填充項。（推論）
- `42E0B4` `GuideMarker` 將此記憶與 [`42E0B1 zzzCHMemoryGuide` 中心](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:309) 聯繫起來。（推論）

由任務擁有的移動程序 (Travel packages)（來自 `find`）：
- [`05AE11 zzzCHMeq05MarukhTravelToPlayer`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357) —— 馬魯克走向玩家。（Package 紀錄；此處未傾印。）
- [`05AE1E zzzCHMeq05PepeTravelToArcane`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357) —— 佩佩走向「Arcane」（石祭壇遺址）。（Package 紀錄；此處未傾印。）
- 備註：`Arcane` 是模組中反覆出現的 Al-Ashe 石之儀式用語（參見下方的分支文本）；保留原始短語，待驗證。

由任務擁有的對話視圖 (Dialog views)（來自 `find`，未傾印）：
- `05AE07 zzzCHMeQ05MarukhView`, `05AE16 zzzCHMeQ05PepeView`。

## 場景紀錄 (Scene Records)

此記憶恰好擁有 **一個** `SCEN` 紀錄。其文本行連結至 `dialogue.md`；階段/動作則來自 `scenediag`。

### 05AE10 zzzCHMeQ05BadScene

CLI：
- `scenediag Vigilant.esm 0x05AE10`

編排：
- 宿主任務：[`05AE03 zzzCHMemoryQuest05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)
- 標記：無
- 演員：別名 `#0` (`Marukh`)，behaviorFlags `DeathEnd`，flags `NoPlayerActivation`
- 階段 (Phases)：3 個，每個具有 0 個開始條件與 1 個完成條件。
- 動作 (Actions)：
  - 索引 1：`Package`，演員 `#0`，階段 0→0，無主題。
  - 索引 2：`Package`，演員 `#0`，階段 1→2，無主題。
  - 索引 3：`Dialog`，演員 `#0`，階段 1，主題 [`05AE12`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:736)，標記 `FaceTarget, HeadtrackPlayer`，情感 `Neutral`，迴圈 1–10。
  - 索引 4：`Dialog`，演員 `#0`，階段 2，主題 [`05AE14`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:739)，標記 `FaceTarget, HeadtrackPlayer`，情感 `Neutral`，迴圈 1–10。

場景擁有的主題（均為 `SNAM=SCEN`，0 個條件，由別名 `#0` 馬魯克說出）：

翻譯：
- [`05AE12` / INFO `05AE13`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:736)：「Ada Bal。那是奇蹟，也是皇帝。是比……任何東西都更能滿足人民飢渴之物。」
  - 備註：來源 `"Ada Bal. Is a miracle, it is also the emperor. And something to satisfy the hunger of the people than ... anything"` 語句錯亂；待驗證。
- [`05AE14` / INFO `05AE15`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:739)：「Dulsa，原諒我。我那無名的孩子，原諒我……」
  - 備註：來源 `"Dulsa, forgive me. My Nameless Child, forgive me ..."`；映照了馬魯克分支中「犧牲你與我的孩子」一行（參見 `05AE0D`），確認此場景即為執行犧牲的過程。（推論）

推論：
- 場景的 EditorID 字面上包含 `BadScene`；結合馬魯克演員上的 `DeathEnd` 以及「原諒我，我那無名的孩子」之內容，這就是**壞結局 / 墮落**結果的劇情過場 —— 犧牲 Dulsa 與孩子。（推論；關於 50 對比 120 的極性爭論請參見重建筆記）

## 自定義對話分支：馬魯克 (Marukh)

分支：
- `05AE08:Vigilant.esm` (`zzzCHMeQ05MarukhB01`)
- 視圖：`05AE07 zzzCHMeQ05MarukhView`

講者條件模式：
- 每個 INFO 要求滿足別名 `#0` (`Marukh`) 上的 `GetIsAliasRef == 1`。
- 開啟行同時要求滿足任務 `05AE03` 上的 `GetStage == 10`。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`05AE09 zzzCHMeQ05MarukhB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:724) | `05AE0A` | 無 | `GetStage == 10`; `GetIsAliasRef alias #0` | 「七十七……龍裔……Sheol。自由之神無盡地消亡，其軌跡……Shezarr」 備註：來源 `"Senventy-Seven...dragonborn...Sheol. God of freedom defunct endlessly, its trajectory...Shezarr"` 高度破碎，待驗證。 |
| [`05AE0B zzzCHMeQ05MarukhB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:727) | `05AE0C` | 無 | `GetIsAliasRef alias #0` | 提示：「你在做什麼，馬魯克 (Marukh)？」 回應 1：「看這塊石頭……Dulsa。這是七十七的奧祕，很快就會完成。Al-Ashe 之石就在此刻被重現。」 回應 2：「Dulsa，你是被選中的。為了愛。Al-Ashe 如此說。為了完成 Arcane，我需要你、以及你腹中孩子的血。」 備註：`Al-Ashe`／`Arcane` 為原文專名，待驗證。 |
| [`05AE0D zzzCHMeQ05MarukhB01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:731) | `05AE0E` | `Goodbye` | `GetIsAliasRef alias #0`; VMAD `CHMeq05_TIF__0205AE0E.Fragment_0` 結束時 | 提示：「這是瘋狂……」 回應 1：「也許並非瘋狂，理智亦在其中。Al-Ashe 之言即真理。讓那些事物在血中閃耀於此石、並奠立高塔，就是我們的使命。」 回應 2：「石頭向我顯示了：那位粉碎了 Aldmeri、平定了大陸、將劍刺入巨蛇的英雄之身影。」 回應 3：「未知之人於昨日或明日知曉一事。但只要那一天哪怕近了一日，我都願意犧牲你與我的孩子。」 備註：來源多處破碎（`shattered Aldomeri`、`thrust a sword into a snake`、`The unexpected know a thing`），待驗證。 |

## 自定義對話分支：佩佩 (Pepe)

分支：
- `05AE17:Vigilant.esm` (`zzzCHMeQ05PepeB01`)
- 視圖：`05AE16 zzzCHMeQ05PepeView`

講者條件模式：
- 每個 INFO 要求滿足別名 `#1` (`Pepe`) 上的 `GetIsAliasRef == 1`。
- 開啟行同時要求滿足任務 `05AE03` 上的 `GetStage == 40`。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`05AE18 zzzCHMeQ05PepeB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:742) | `05AE19` | 無 | `GetStage == 40`; `GetIsAliasRef alias #1` | 「那 湮滅 (oblivion) 究竟是什麼……」 備註：來源 `"What is oblivion that..."` 為截斷句，待驗證。 |
| [`05AE1A zzzCHMeQ05PepeB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:745) | `05AE1B` | `Goodbye`, `SayOnce` | `GetIsAliasRef alias #1`; VMAD `CHMeq05_TIF__0205AE1B` 開始時 `Fragment_0` + 結束時 `Fragment_1` | 提示：「請捨棄這塊石頭。在無人之手能不期然觸及之處……」 回應 (Puzzled)：「好……我明白了。我以 Mara 之名起誓。」 備註：提示來源 `"Please discard this stone. Where anyone hands reach unexpected ..."` 為截斷／破碎句，待驗證。 |

## 相關紀錄 (Related Records)

這些是交叉連結的背景資訊。任務 `05AE03` 所擁有的 NPC/物品僅限於填充在別名 `#0` / `#1` 中的兩個記憶演員；其餘為敘事上的交叉參考。

NPCs：
- [`05ADEF zzzCHMarukhMemory` - 馬魯克 (Marukh)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1046) —— 別名 `#0`。
- [`05ADFD zzzCHInquisitorPepeMemory2`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1044) —— 別名 `#1`。
- [`12BF48 zzzCHInquisitorPepeMemory` - Inquisitor Pepe](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:558) —— 另一個佩佩記憶變體。
- [`081E46 zzzCHInquisitorPepe` - Inquisitor Pepe](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1065) —— 現世/當前的佩佩。

物品：
- [`05AE01 zzzCHAdabalMemory` - `Red Stone`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:1004) —— 此記憶中的紅石；FormID 與任務一同位於 `05AE0x` 區塊，與別名 `#5` `Adabal` 有強烈關聯。（推論）
- [`1353DF zzzCHAdabal` - `Adabal`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:976) —— 當前的 Adabal 物品。
- [`108EB1 zzzCHSkinPepe`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:727)
- [`500DDC zzVcgPepeMask` - `Mask of Pelan`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:416) —— 備註：`Pelan` 可能是 `Pelin` / `Pepe` 的在地化拼法；待驗證。

位置（Adabal Court —— 佩佩移動程序所引用的佩佩教派遺址，推論）：
- [`26C05F zzzCHLocAdabalCourt` - `Adabal Court`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:550)
- [`21AFA1 zzzCHCourtAdabalFirst` - `First Court`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:131)
- [`21AEA7 zzzCHCourtAdabalSecond` - `Second Court`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:144)
- [`0E0889 zzzCHSummaryCourt02` - `Fountain Garden of Dibella`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:140) —— 佩佩祭司追隨者的隱藏花園，根據 Gregory 的筆記。（推論）

書籍（背景設定背景資訊 —— 均不由任務 `05AE03` 擁有；兩者在 `booktext` 均失敗，來源文本為提取出的 `game-data`）：
- [`4A8AFD zzzCHBookESO09 "Aurbic Enigma 4: The Elden Tree"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1619) —— 定義了 `Chim-el-Adabal`、建國之石 (Founding-Stone)、眾塔之杖 (Staff of Towers) 以及塔段的**起舞 (Dance)**；任務標題「Ada Bal」與目標「在亡者之上起舞」所隱喻的官方來源。（推論）
- [`12905F zzzCHBookESOIllsuionDeath "The Illusion of Death"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:131) —— 馬魯克 (Marukh) / 「七十七道堅定教條 (Seventy-Seven Inflexible Doctrines)」 / `Al-Esh` 背景資訊（與記憶 07 共享）。

## 相關書籍翻譯 (Related Book Translation)

由於任務 `05AE03` 不擁有任何 `BOOK` 紀錄，此部分提供標題/目標所源自的**背景設定錨點**，而非任務內的書籍。

[`4A8AFD zzzCHBookESO09 "Aurbic Enigma 4: The Elden Tree"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1619)

CLI：
- `booktext Vigilant.esm 0x4A8AFD`
- 結果：失敗並提示 `could not extract English strings`；因此來源使用已提取出的 `game-data` 文本。

基於原始資料的連結點（原版 "Aurbic Enigma 4" 背景設定文本，在此模組的書籍中逐字重現）：
- [`Chim-el-Adabal`，巨大的紅鑽，「來自洛克汗之心 (Heart of Lorkhan) 結晶化的鮮血」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1639) —— 紅石；任務的 `Adabal` / `Red Stone` 物品 (`05AE01`) 即為其記憶形態。（推論）
- [八部分的眾塔之杖，「每個片段都在其舞動 (Dance) 中呈現塔的樣貌」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:1643) —— 目標「在月下，於亡者之上起舞 (Dance)」中的「Dance」。（推論）

翻譯備註：
- 模組的任務標題 `Ada Bal` 以及馬魯克的 `Stone of Al-Ashe` / `Arcane` 是 `Adabal` / `Al-Esh` / 艾雷德 (Ayleid) 儀式的機器翻譯映照。錯亂的譯名（`Ada Bal`, `Al-Ashe`, `Arcane`, `Sevenety-Seven`）保留在上方分支表中並標註 `Note: 待驗證`，而非被默默修正。

## 重建筆記 (Reconstruction Notes)

以原始資料為基礎：
- 此記憶為 [`05AE03 zzzCHMemoryQuest05 "Ada Bal"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:357)，目標為 [`Under the moon, Dance on the dead.`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:358)。
- 它恰好擁有 **一個** `SCEN`：`05AE10 zzzCHMeQ05BadScene`，一個 3 階段的馬魯克（別名 `#0`）過場動畫，帶有兩個場景專屬主題（`05AE12`, `05AE14`）與 `DeathEnd`。
- 它擁有 **兩個** 自定義對話分支：
  - 馬魯克分支 `05AE08`（別名 `#0`），開啟行受限於階段 `GetStage == 10`；3 個主題；以帶有 `Goodbye` 片段的 `05AE0E` 結束。
  - 佩佩分支 `05AE17`（別名 `#1`），開啟行受限於階段 `GetStage == 40`；2 個主題；以帶有 `Goodbye, SayOnce` VMAD（開始與結束片段）的 `05AE1B` 結束。
- 兩個 `CompleteQuest` 階段：**50** 與 **120**。

觸發條件（推論）：
- 中心 `42E0B1 zzzCHMemoryGuide`（透過 `GuideMarker` 別名 `#7`, `42E0B4`）啟動此記憶；玩家進入後，兩個移動程序將馬魯克移至玩家處 (`05AE11`)，將佩佩移至祭壇遺址 (`05AE1E`)。目前的 CLI 未印出具體的觸發 NPC/物品；需要 QUST 開始條件 / 目標物參考 (target-ref) 傾印。待辦。

如何選擇 50 與 120，以及極性（推論，基於原始資料結構）：
- 兩個 `CompleteQuest` 階段 = 索引中反覆出現的雙頻帶好/壞（因果）記憶特徵。
- 分支開啟行依階段限制：馬魯克分支需要 `GetStage == 10`（早期），佩佩分支需要 `GetStage == 40`（後期）。因此玩家可以先聽取馬魯克的辯解，隨後再接觸佩佩。
- **此處可從 ESM 中解析極性**（不像 MeQ07，索引中將其留為「存在兩種結果」）：
  - 佩佩分支的終端行 `05AE1B` 是玩家哀求佩佩**捨棄石頭**，而佩佩誓言 **「我以 Mara 之名起誓」** —— 一個慈悲 / 放棄儀式的結果。 → **好結果**。
  - 馬魯克分支的終端行 `05AE0E` 是馬魯克宣告他 **「願意犧牲你與我的孩子」** 來完成石頭；唯一擁有的場景字面上即為 `zzzCHMeQ05BadScene` 並播放了「Dulsa，原諒我，我那無名的孩子」之犧牲對話 (`05AE14`, `DeathEnd`)。 → **壞結果**。
  - 映射至階段（推論，待透過片段確認）：**階段 50 = 佩佩 / 好結局（儀式被避免）完成**，**階段 120 = 馬魯克 / 壞結局（執行犧牲，播放 BadScene）完成**。片段腳本 `CHMeq05_TIF__0205AE0E`（馬魯克結束）與 `CHMeq05_TIF__0205AE1B`（佩佩開始+結束）最可能是這些階段的設置者；反編譯它們以確認各個設置的確切階段。

主角確認：
- 透過 `05AE03` 所擁有的 `zzzCHMeQ05PepeB01*` 主題 EditorID (`05AE18`, `05AE1A`)，「佩佩」被確認為此任務的主角 —— 與索引的 主角→任務 映射相符。馬魯克與 Dulsa 也參與其中（別名 `#0`, `#6`）。

開放驗證：
- 反編譯 VMAD 片段 `CHMeq05_TIF__0205AE0E`（馬魯克 Goodbye）與 `CHMeq05_TIF__0205AE1B`（佩佩開始+結束），確認哪個設置階段 50 對比 120 以及授予的任何物品/全域變數；
- 傾印 QUST 別名 `#5 Adabal` 與 `#6 MemoryDulsa` 的填充項（未由 `scenediag` 印出）以及 objective[0] 目標參考；
- 若移動編排很重要，傾印兩個 `Package` 紀錄 `05AE11` / `05AE1E`；
- 確認 `05AE01 zzzCHAdabalMemory "Red Stone"` 是否為填充至別名 `#5` 的物件，以及好結局是否將其移除/保留；
- 根據背景設定書籍 `4A8AFD` 與記憶 07 的 `12905F` 解析錯亂的專有名詞 `Al-Ashe` (= `Al-Esh`?)、`Arcane`、`Sheol`、`Shezarr`、`Sevenety-Seven`。
