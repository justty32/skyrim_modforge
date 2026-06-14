# 第四章記憶 11 - 風暴過後 (After the Storm)

狀態：重構切片。基於來源、連結優先，非劇情摘要。

來源方針：
- 原始語句連結回抽取的來源文件，而非全文複製。
- 僅在需要解釋翻譯問題時才出現短小的來源片段。
- `SCEN` 編排來自 CLI 診斷，因為抽取的 `dialogue.md` 僅保留場景話題文本，而非場景階段/動作。

## 任務紀錄 (Quest Record)

[`2B9BAB zzzCHMemoryQuest11 "After the Storm"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)

CLI：
- `questdiag Vigilant.esm 0x2B9BAB`
- `infodiag Vigilant.esm 0x2B9BAB`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x2B9BAB`
- EditorID: `zzzCHMemoryQuest11`
- 名稱: `After the Storm`
- 標誌: `RunOnce`
- 優先級: `90`
- 類型: `Misc`
- 過濾器: `CH\`

來自 `questdiag` 的階段 (16 個)：

| 階段 | 標誌 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 10 | 無 | 空 |
| 20 | 無 | 空 |
| 30 | 無 | 空 |
| 40 | 無 | 空 |
| 50 | CompleteQuest | 空 |
| 60 | 無 | 空 |
| 300 | 無 | 空 |
| 305 | 無 | 空 |
| 310 | 無 | 空 |
| 315 | 無 | 空 |
| 320 | 無 | 空 |
| 330 | 無 | 空 |
| 340 | CompleteQuest | 空 |
| 350 | 無 | 空 |
| 999 | ShutDownStage | 空 |

任務目標：
- `questdiag` 報告 `Objectives (0)`。任務未帶任何任務目標文本；`quests.md` 第 256 行是僅含標題的條目，無 `[obj N]` 行。

階段波段型態（基於來源）：
- 兩個 `CompleteQuest` 階段：**50**（低波段，階段 0-60）與 **340**（高波段，階段 300-350）。
- 這兩個波段對應於 `find` 中命名的兩個編排場景：[`2B9BB4 zzzCHMeQ11GoodScene`](#2b9bb4-zzzchmeq11goodscene) 與 [`2BAEFB zzzCHMeQ11BadScene`](#2baefb-zzzchmeq11badscene)。EditorID 名稱 `GoodScene` / `BadScene` 為基於來源的極性標籤（見分支結果）。

## 別名 / 編排骨幹 (Alias / Staging Backbone)

以下三個 `SCEN` 紀錄共用相同的主機任務與別名。

主機任務：
- [`2B9BAB zzzCHMemoryQuest11`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)

來自 `scenediag` 的主機任務別名：

| 別名 | 名稱 | 填充 | 解析為 |
|---:|---|---|---|
| 0 | `Bull` | 唯一演員 `2B8827:Vigilant.esm` | [`2B8827 zzzCHMemoryMorihaus01 "Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:782) |
| 1 | `Priest` | 唯一演員 `2B882A:Vigilant.esm` | [`2B882A zzzCHMemorySthunPriest "Stuhn Priest"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:781) |
| 2 | `Akatosh` | 唯一演員 `2DE6E3:Vigilant.esm` | [`2DE6E3 zzzCHMemoryAkatoshMorihaus` (無名稱)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:809) |
| 3 | `PelinalMarker` | 強制引用 `2E47DF:Vigilant.esm` | 標記 |
| 4 | `GateMarker` | 強制引用 `2E47E0:Vigilant.esm` | 標記 |
| 5 | `Gardener` | 唯一演員 `2E47F0:Vigilant.esm` | [`2E47F0 zzzCHMemoryGardener "King of Nenalata"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:805) |

主體 / 說話者：
- 記憶的主體以及貫穿始終的說話者是 **莫里豪斯 (Morihaus)**（別名 `#0` `Bull`）。除了特別註明之處外，所有場景獨白皆由別名 `#0` 配音，且自定義分支開端受限於玩家站在別名 `#2` `Akatosh` 或別名 `#5` `Gardener` 面前。
- `Stuhn Priest` (別名 `#1`) 是場景中的第二位演員（在壞分支中拒絕執行命令的祭司）。
- `Bull` = 莫里豪斯，在 TES 傳說中他是艾萊西亞的有翼牛頭人配偶；話題 [`2B9BBF`](#2b9bb4-zzzchmeq11goodscene) 中提到的 "Paravania" 是艾萊西亞的牛頭人化身。（推論，與 [`51AE2D zzzCHAlessiaMntr "Paravania the Man-bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:428) 進行交叉檢查）

觸發器（基於來源的型態）：
- `questdiag` 不會列印目標引用，且 QUST 啟動條件未包含在目前的 CLI 轉儲中，因此確切的世界內觸發引用未在此解碼。
- 場景演員 `Bull`/`Priest`/`Akatosh`/`Gardener` 為 `uniqueActor` 填充，而 `PelinalMarker`/`GateMarker` 為 `forcedRef` 標記；記憶是透過接近編排好的佩林納爾之死場景進入的（根據別名名稱 + 場景編排推論）。行走 package 確認了此編排：`zzzCHMeq11PriestWalkToPelinal`、`zzzCHMeq11MorihausWalkToPelinak [sic]`、`zzzCHMeq11MorihausPrayForPelinal` ([來自 `find`](#packages-from-find))。

## 場景紀錄 (Scene Records)

場景紀錄在 `game-data` 中並非完整紀錄；文本行連結至 `dialogue.md`，而階段/動作則來自 `scenediag`。所有三個場景共用相同的 13 個場景類別話題（每個場景的 `scenediag` 都列出了完整的 13 個，但實際上每個場景僅透過其 `Dialog` 動作播放其中的一個子集）。

### 2B9BB5 zzzCHMeQ11Sc01

CLI：
- `scenediag Vigilant.esm 0x2B9BB5`

編排：
- 主機任務：[`2B9BAB zzzCHMemoryQuest11`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)
- 標誌：無
- 演員：別名 `#0` (`Bull`, `DeathEnd, DialoguePause`), 別名 `#1` (`Priest`, `DeathEnd, DialoguePause`)
- 階段：2 個，每個皆為 `0` 開始條件與 `1` 完成條件。
- 動作：共 6 個 —— 別名 `#0` 與 `#1` 每個階段的 `Package` 動作，以及別名 `#0` 與 `#1` 話題為空 (`Topic=<null>`) 的 `Dialog` 動作。
- 筆記：這是建立背景/待機場景；`Dialog` 動作不帶話題，因此此處未綁定口說對白。可能是無聲走向佩林納爾屍體的過程（根據行走/祈禱 package 推論）。

### 2B9BB4 zzzCHMeQ11GoodScene

CLI：
- `scenediag Vigilant.esm 0x2B9BB4`

編排：
- 主機任務：[`2B9BAB zzzCHMemoryQuest11`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)
- 標誌：無
- 演員：別名 `#0` (`Bull`, `DialoguePause`, `NoPlayerActivation, Optional`), 別名 `#1` (`Priest`, `DeathEnd, DialoguePause`, `NoPlayerActivation, Optional`), 別名 `#2` (`Akatosh`)。
- 階段：5 個。
- 動作 (10)：`#0`/`#1` 的 package、第 0 階段作用於 `#0` 的 `Timer` (3s)、在第 1-4 階段每階段由 `#0` (`Bull`/莫里豪斯) 說出的一句 `Dialog` 台詞，以及在 `#1` 與 `#2` 上的無話題 `Dialog` 動作。

莫里豪斯獨白 (別名 `#0`), 按階段順序播放：

| 階段 | 話題 / INFO | 來源 | 翻譯 |
|---:|---|---|---|
| 1 | `2B9BB9` / `2B9BBA` | [台詞](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2589) | 「你走了……這樣的結局，真像你的作風……」 |
| 2 | `2B9BBB` / `2B9BBC` | [台詞](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2592) | 「佩林納爾，是你教我的。Ada 必須以愛來改變一切……」 |
| 3 | `2B9BBD` / `2B9BBE` | [台詞](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2595) | 「正因如此，我的心才這麼痛。陷入嗜血、向狂怒交出自己，反而還比較容易。」 |
| 4 | `2B9BBF` / `2B9BC0` | [台詞](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2598) | 「但即便如此，我仍守望著你所造的這個世界……為了她，為了 Paravania……」 |

- 筆記：`Ada` 是 Ehlnofex 語中對神聖/原始靈魂的稱呼；不予翻譯。解讀為「神聖者 / 世界必須透過愛來改變」；保留直譯待驗證 - 待驗證。

### 2BAEFB zzzCHMeQ11BadScene

CLI：
- `scenediag Vigilant.esm 0x2BAEFB`

編排：
- 主機任務：[`2B9BAB zzzCHMemoryQuest11`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256)
- 標誌：無
- 演員：別名 `#0` (`Bull`, `DeathEnd, DialoguePause`), 別名 `#1` (`Priest`, `DialoguePause`)。
- 階段：16 個。
- 動作 (23)：一段長長的 `Package` + `Timer` + `Dialog` 鏈條，為莫里豪斯 (`#0`) 與 Stuhn 祭司 (`#1`) 配音。這是暴力分支（來自 `find` 的 `Morihaus...DrawWeapon` / `...SlayPriest` / `...GoToOblivion` package 是此場景的 package 集合）。

分支對話（按場景動作階段順序）：

| 階段 | 演員 | 話題 / INFO | 情緒 | 來源 | 翻譯 |
|---:|---|---|---|---|---|
| 1 | `#0` 莫里豪斯 | `2BAEFC` / `2BAEFD` | Neutral | [台詞](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2601) | 「佩林納爾……究竟發生了什麼……」 |
| 2 | `#0` 莫里豪斯 | `2BAEFE` / `2BAEFF` | Neutral | [台詞](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2604) | 「佩林納爾……說句話吧，拜託你。再像從前那樣鼓舞我們……」 |
| 3 | `#1` 祭司 | `2BAF00` / `2BAF01` | Sad(100) | [台詞](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2607) | 「莫里豪斯大人，請振作起來……」 |
| 4 | `#0` 莫里豪斯 | `2BAF37` / `2BAF38` | Neutral | [台詞](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2622) | （沉默：「……………………」） |
| 5-6 | `#0` 莫里豪斯 | `2BAF03` / `2BAF04` | Neutral | [台詞](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2610) | 「……殺光俘虜。還有，殺光所有精靈居民，連同他們的牲口。」 |
| 7 | `#1` 祭司 | `2BAF05` / `2BAF06` | Puzzled | [台詞](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2613) | 「您……您說什麼？這是瘋了。」 |
| 8 | `#1` 祭司 | `2BAF07` / `2BAF08` | Anger | [台詞](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2616) | 「這違背了 Sthun 的教誨……」 |
| 13 | `#0` 莫里豪斯 | `2BAF0B` / `2BAF0C` | Neutral | [台詞](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2619) | 「佩林納爾，我從你身上學會了。我陷入嗜血，向狂怒交出自己。」 |
| 14 | `#0` 莫里豪斯 | `2BAF0E` / `2BAF0F` | Neutral | [台詞](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2622) | 「Cyrod 已是我們的了。一切都被允許。」 |

- 筆記：`teaching of Sthun` ([`2BAF07`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2616)) 對比 NPC EditorID `zzzCHMemorySthunPriest` / 名稱 `Stuhn Priest` ([npcs.tsv:781](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:781))：對話拼寫為 `Sthun`, 祭司紀錄為 `Stuhn`/`Sthun`。Stuhn 是諾德/艾德拉的贖金之神；兩者拼寫皆指同一位神祇。來源拼寫錯亂 - 保留原樣，待驗證。
- 筆記：`It is easier to go mad into blood and surrender myself to rage` (Good, `2B9BBD`) 與 `I go mad into blood, surrender myself to rage` (Bad, `2BAF0B`) 是刻意的鏡像：在好場景中莫里豪斯*抵制*了這種衝動，在壞場景中他則*屈服*於它。這是基於來源的極性基準。
- 筆記：`Cyrod` = Cyrodiil (古期拼寫)；保留來源。
- 筆記：階段 4 的 `2BAF37` 是純粹的省略號台詞 `........................` (沉默節拍)；譯為沉默停頓。

## 自定義對話分支：阿卡托什 (Akatosh) (好結局)

分支：
- `2DE6E7:Vigilant.esm` (`zzzCHMeQ11AkatoshB01`), 視圖 `2DE6E6 zzzCHMeQ11AkatoshView`。

說話者條件模式：
- INFO 要求別名 `#2` (`Akatosh`) 的 `GetIsAliasRef == 1`。

| 話題 | INFO | 標誌 | 條件 | 情緒 | 翻譯 |
|---|---|---|---|---|---|
| [`2DE6E8 zzzCHMeQ11AkatoshB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2768) | `2DE6E9` | `Goodbye` | 別名 `#2` 的 `GetIsAliasRef` | Sad | 「暴風雨過後，便是寧靜。那是何等的哀傷啊……」 |

- 這是 **GoodScene** 路徑（莫里豪斯升天 / 由阿卡托什守望）面對玩家的結束台詞。標題 "After the Storm" 直接來自台詞 `After a storm comes a calm`。

## 自定義對話分支：園丁 (Gardener) (壞結局)

分支：
- `2E5B3E:Vigilant.esm` (`zzzCHMeQ11GardenerB01`), 視圖 `2E5B3D zzzCHMeQ11GardenerView`。

說話者條件模式：
- INFO 要求別名 `#5` (`Gardener` = 「奈納拉塔之王」) 的 `GetIsAliasRef == 1`。

| 話題 | INFO | 標誌 | 條件 | 情緒 | 翻譯 |
|---|---|---|---|---|---|
| [`2E5B3F zzzCHMeQ11GardenerB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2794) | `2E5B40` | `Goodbye` | 別名 `#5` 的 `GetIsAliasRef` | Sad | 「精靈的時代逝去了，人類的時代來臨了……奈納拉塔之王是對的……」 |

- 這是 **BadScene** 路徑（莫里豪斯殺死祭司，「一切都被允許」）面對玩家的結束台詞。`Gardener` 即 `King of Nenalata` ([npcs.tsv:805](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:805))；參見他處的 `Thannor the Gardener` ([npcs.tsv:704](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:704))。
- 筆記：`Man Era is come` / `Mer Era is gone` 為來源詞語 (Mer = 精靈)；保留直譯。

## Packages (來自 `find`)

這些 `zzzCHMeq11*` package 驅動場景演員（來源：`find zzzCHMeQ11`；注意來源 EditorID 中混用的 `Meq`/`MEq` 大小寫以及 `Pelinak [sic]`）：

- `2B9BB6 zzzCHMeq11MorihausWalkToPelinak` (sic)
- `2B9BB7 zzzCHMeq11PriestWalkToPelinal`
- `2B9BB8 zzzCHMeq11MorihausPrayForPelinal`
- `2B9BC1 zzzCHMeq11PriestFollowMoriaus` (sic, "Moriaus")
- `2BAF02 zzzCHMeq11MorihausStandUpToPriest`
- `2BAF09 zzzCHMEq11MorihausDrawWeapon`
- `2BAF0A zzzCHMeq11MorihausSlayPriest`
- `2BAF0D zzzCHMeq11MorihausGoToOblivion`
- `2BFDAF zzzCHMeq11MorihausStayFrontPelinal`

Package 集合確認了兩種結果：祈禱 (好) 與拔劍 / 殺死祭司 / 前往湮滅 (壞)。

## 分支結果 (基於來源) (Branch Outcomes)

| 結果 | 場景 | 完成階段 | 結束說話者 | 結束台詞 |
|---|---|---:|---|---|
| **好 (Good)** | [`2B9BB4 zzzCHMeQ11GoodScene`](#2b9bb4-zzzchmeq11goodscene) | 50 (`CompleteQuest`) | 阿卡托什分支 | "After a storm comes a calm" |
| **壞 (Bad)** | [`2BAEFB zzzCHMeQ11BadScene`](#2baefb-zzzchmeq11badscene) | 340 (`CompleteQuest`) | 園丁分支 | "Mer Era is gone, Man Era is come" |

極性是**基於來源的 EditorID 釘定**，而不僅僅是推論：
- 場景紀錄字面上命名為 `GoodScene` 與 `BadScene`。
- 好 = 莫里豪斯悲痛但選擇愛 / 克制 (`Ada must change things through love`, `it is easier to go mad... [但他沒有]`)，並由**阿卡托什**（艾德拉之首）以 "after a storm comes a calm" 作結。
- 壞 = 莫里豪斯屈服 (`I go mad into blood, surrender myself to rage`, `all is permitted`)，下令屠殺精靈平民，殺死抗議的 Stuhn 祭司，且路徑由**奈納拉塔之王**作結 ("Mer Era is gone... the King of Nenalata is right")。
- 階段 50 (低波段) = 好完成；階段 340 (高波段) = 壞完成（推論，透過將兩個 `CompleteQuest` 波段對應到兩個場景；與 package 集合一致，但確切的階段->場景接線位於未轉儲的階段片段中）。

## 重構筆記 (Reconstruction Notes)

基於來源：
- 本記憶由 [`2B9BAB zzzCHMemoryQuest11 "After the Storm"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256) 代表，僅有標題（ESM 或 `quests.md` 中無目標文本）。
- 主體/說話者：**莫里豪斯 (Morihaus)** (別名 `#0` `Bull`, [`2B8827`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:782))，哀悼死去的**佩林納爾·白斯特拉克 (Pelinal Whitestrake)**。
- 包含 **3 個 `SCEN` 紀錄**：`2B9BB5 Sc01` (沉默建立背景)、`2B9BB4 GoodScene` (4 句莫里豪斯台詞)、`2BAEFB BadScene` (16 階段屠殺分支)。
- 包含 **2 個自定義對話分支**（玩家面對的結束對話）：阿卡托什別名 `#2` (好) 與園丁別名 `#5` (壞)，皆為單個受 `GetIsAliasRef` 限制的 `Goodbye` 話題。
- **0 本書籍**由本任務擁有 / 連結（`find` 未返回 BOOK；場景中未呼叫書本內容）。

開放驗證：
- 轉儲 QUST 別名/目標 + 啟動條件，以釘定確切的世界內觸發引用（CLI 目前不列印這些）；
- 閱讀階段 50 / 340 的階段片段 / VMAD，以確認哪個波段由哪個場景完成，以及各授予什麼內容；
- 每個場景的分支*分歧點*（玩家做什麼來引向好與壞）編碼在場景階段的 `completeConds`（`scenediag` 未詳細列印）及/或一個業障全局變數中 - 需要更深入的轉儲；
- 錯亂的來源拼寫需保留並標註 (待驗證)：`Sthun` 對比 `Stuhn` (祭司的神)、`WalkToPelinak`/`Moriaus` (package EditorID)、`Ada` (不翻譯的 Ehlnofex 語)、`Cyrod` (Cyrodiil 的古期拼寫)。
