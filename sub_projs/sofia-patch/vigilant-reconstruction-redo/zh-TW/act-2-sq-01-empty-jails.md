# 第二章支線任務 01 — 空蕩的監牢 (Act 2 Sq01 - Empty Jails)

狀態：第一個重做切片。基於來源 (Source-grounded)，連結優先，非劇情摘要。

來源方針：
- 原始文本行連結回提取的來源文件，而非完整複製。
- 僅在需要解釋翻譯問題或特定條件時才出現短小的來源片段。
- CLI 診斷提供確定的階段/目標/條件數據。

## 任務記錄 (Quest Record)

[`038524 zzzBMMq01 "Empty Jails"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?)

CLI 指令：
- `questdiag Vigilant.esm 0x038524`
- `infodiag Vigilant.esm 0x038524`

ESM 路徑：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x038524`
- EditorID: `zzzBMMq01`
- 名稱: `Empty Jails` (空蕩的監牢)
- 標誌: `RunOnce`
- 優先級: `90`
- 類型: `SideQuest`
- 過濾器: `BM\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 標誌 | 日誌 |
|---:|---|---|
| 0 | StartUpStage | 空 |
| 10 | 無 | 空 |
| 20 | 無 | 空 |
| 30 | 無 | 空 |
| 40 | 無 | 空 |
| 50 | 無 | 空 |
| 60 | 無 | 空 |
| 100 | 無 | 空 |
| 110 | CompleteQuest | 空 |
| 110 | 無 | 空 |
| 255 | CompleteQuest | 空 |
| 999 | ShutDownStage | 空 |
| 9999 | CompleteQuest | 空 |

目標 (Objectives)：

| 索引 | 來源 | 任務文本 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | 與 `<Alias=Courier>` (信使) 交談 |
| 10 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | 與 `<Alias=Steward>` (總管) 交談 |
| 20 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | 搜索風盔城地牢 |
| 30 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | 與 `<Alias=Steward>` (總管) 談論少女石像 |
| 40 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | 在斯丹達爾神廟找到風盔城報告 |
| 50 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | 與 `<Alias=Steward>` (總管) 交談 |
| 60 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | 擊敗風盔城下的吸血鬼 |
| 100 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) | 向 `<Alias=Steward>` (總管) 報告 |

目標對象 (Objective targets)：
- 每個目標在 ESM 中都有 1 個對象。
- 目標條件：目標 20 和 60 各有 1 個條件；其他目標為 0。
- (推論：對象引用可能放置在風盔城地牢和吸血鬼地點；如果空間編排重要，則需要更深層的轉儲。)

## 自定義對話分支 (Custom Dialogue Branches)

此任務有多個對話分支。兩名主要 NPC 透過別名索引識別：`Courier` (信使，別名 #2) 和 `Steward` (總管，別名 #4，推測為 `zzzBMMq01Steward`)。第三個別名 (#7，`Library Guard` 圖書館守衛或類似角色) 出現在法師學院圖書館語境中。所有分支皆屬於任務 `0x038524`。

### 分支 1：信使介紹 (別名 #2)

分支：
- `038AB2:Vigilant.esm` (推論；分支根節點未由 CLI 列印，但別名引用了它)

說話者條件模式：
- 大多數 INFO 需要別名 `#2` (信使) 的 `GetIsAliasRef == 1`。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`038AB3 zzzBMMq01B01gStart`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:474) | `038AB4` | 無 | `GetStage EqualTo 0`; `GetIsAliasRef alias #2` | [或者你只是在這裡……你就像是這裡斯丹達爾的守護者？](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:475) |
| [`038AB5 zzzBMMq01B01gMatter`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:478) | `038AB6` | 無 | `GetIsAliasRef alias #2` | 提示："What's the Matter?" (發生什麼事了？) 回覆：[我是奉風盔城執政官之命而來。雖然我想幫助斯丹達爾守護者的調查，但我想聽聽「你」的說法……](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:479) |
| [`038AB7 zzzBMMq01B01gNO`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:481) | `038AB8` | 無 | `GetIsAliasRef alias #2` | 提示："I'm Busy now" (我現在很忙) 回覆：[這是指令。如果你拒絕，可能會限制在東境一帶的活動。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:482) |
| [`038AB9 zzzBMMq01B01gOK`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:484) | `038ABA` | Goodbye, SayOnce | `GetIsAliasRef alias #2`; 結束時執行 VMAD `BM01_TIF__01038ABA.Fragment_0` | 提示："With the incident?" (關於這次事件？) 回覆：[最好直接去跟他們的執政官談。這件事我希望你能保持緘默。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:485) |
| [`038ABB zzzBMMq01B01gGoOn`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:487) | `038ABC` | 無 | `GetIsAliasRef alias #2` | 提示："Does not matter, they continue" (沒關係，繼續說) 回覆：[風盔城發生了囚犯失蹤事件。我想請求「你」對這棟屋子進行調查。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:488) |

翻譯筆記：
- 原始信使對話在提取時較為模糊 (來源翻譯品質不佳)；如果話題對劇情至關重要，可能需要根據 ESM 本身進行驗證。

### 分支 2：總管 / 衛兵隊長 (別名 #4)

分支：
- `038ABF:Vigilant.esm` (從 INFO `038AC1` 起推論根節點)

說話者條件模式：
- 大多數 INFO 需要別名 `#4` (總管 / 衛兵隊長) 的 `GetIsAliasRef == 1`。
- 部分 INFO 受階段限制。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`038AC0 zzzBMMq01B01stMissionStart`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:490) | `038AC1` | 無 | `GetStage EqualTo 10`; `GetIsAliasRef alias #4` | 提示："I am `<Alias=Player>`, Vigilant of Stendarr" (我是 `<Alias=Player>`，斯丹達爾警戒者) 回覆 (1)：[歡迎，我一直在等。大教堂裡斯丹達爾的其中一位是耳目。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:491) 回覆 (2)：[不需要太多地方，事情很緊急。我想把事件的調查交給你。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:492) |
| [`038AC2 zzzBMMq01B01stDetail`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:494) | `038AC3` | 無 | `GetIsAliasRef alias #4` | 提示："So the details of the case?" (那麼案件的細節是？) 回覆 (1)：[真是辛苦了。立刻來說說監牢的事吧。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:495) 回覆 (2)：[從三天前開始，囚犯一個接一個失蹤。我不追究罪行。監牢裡留下了大量的血跡……](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:496) 回覆 (3)：[甚至昨晚……如果只是囚犯失蹤也就罷了，連衛兵都消失了。我們已經無法控制了。我希望能得到援手。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:497) |
| [`038AC4 zzzBMMq01B01stEntrust`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:499) | `038AC5` | 無 | `GetIsAliasRef alias #4`; 結束時執行 VMAD `BM01_TIF__01038AC5.Fragment_0` | 提示："Entrust to me" (交給我吧) 回覆：[這真是可靠的一句話。城堡的監獄裡發生了許多失蹤事件。我想讓你先去調查那裡。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:500) |
| [`039597 zzzBMMq01B02stAgain`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:502) | `039598` | 無 | `GetStage EqualTo 20`; `GetIsAliasRef alias #4` | 提示："Tell me again about the incident" (再告訴我一次關於事件的事) 回覆 (1)：[從三天前開始，囚犯一個接一個失蹤。沒留下什麼血跡。甚至昨晚看守也失蹤了。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:503) 回覆 (2)：[城堡的地牢在軍營後方。我想請你仔細檢查。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:504) |
| [`03959B zzzBMMq01B03stStoneFace`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:506) | `03959C` | 無 | `GetStage GreaterThanOrEqualTo 30`; `GetStage LessThanOrEqualTo 50`; `GetIsAliasRef alias #4` | 提示："Tell me about Maiden Statue" (告訴我關於少女石像的事) 回覆 (1)：[嗯？你在說雕刻在石牆上的石像嗎？……那是很久以前禁閉室留下來的。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:507) 回覆 (2)：[根據古老的記錄……城堡建造時就有了，但目的是什麼尚不清楚。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:508) |
| [`03959E zzzBMMq01B04stThePast`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:510) | `03959F` | 無 | `GetStage GreaterThanOrEqualTo 30`; `GetStage LessThanOrEqualTo 40`; `GetIsAliasRef alias #4` | 提示："Did not a similar incident happened in the past?" (過去是否發生過類似事件？) 回覆 (1)：[幾年前曾有過一次……20年前。當時囚犯全都消失了……](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:511) 回覆 (2)：[那時候因為戰後的動盪，治安維護交給了守衛。我不知道詳細細節。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:512) 回覆 (3)：[我記得斯丹達爾守護者當時從教會派了幾個人過來……](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:513) |
| [`0395A0 zzzBMMq01B04stReport`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:515) | `0395A1` | SayOnce | `GetIsAliasRef alias #4`; 結束時執行 VMAD `BM01_TIF__010395A1.Fragment_0` | 提示："Do not document the incident left?" (沒有留下關於該事件的紀錄嗎？) 回覆：[不在這裡。其中一份關於守護者雅各布 (Jacob) 的評論文章被帶走了……](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:516) |
| [`0395A3 zzzBMMq01B05stVampire`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:518) | `0395A4` | SayOnce | `GetStage EqualTo 50`; `GetIsAliasRef alias #4`; 結束時執行 VMAD `BM01_TIF__010395A4.Fragment_0` | 提示："Vampire appeared in the past. This also would be the work of a vampire" (過去曾有吸血鬼出現。這可能也是吸血鬼幹的) 回覆 (1)：[吸血鬼？……衛兵從未報告過……](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:519) 回覆 (2)：[無論吸血鬼對手發生什麼事，我們都無能為力。交給你們專業人士處理吧。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:520) |
| [`0395A6 zzzBMMq01B06stDefeated`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:522) | `0395A7` | SayOnce | `GetStage EqualTo 100`; `GetIsAliasRef alias #4`; 結束時執行 VMAD `BM01_TIF__010395A7.Fragment_0` | 提示："Defeated vampires" (擊敗了吸血鬼) 回覆 (1)：[是的，必須對你表示感謝。這是獎勵，請收下。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:523) 回覆 (2)：[另外，在我們的守護者訪問風盔城時，我一直在談論你和斯丹達爾大教堂。他們應該已經抵達大教堂了。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:524) 回覆 (3)：[我們期待你未來的成就，願斯丹達爾引導著你。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:525) |

### 分支 3：圖書館守衛 (別名 #7)

分支：
- `043F01:Vigilant.esm` (從 INFO `043F03` 推論) 和 `043F04:Vigilant.esm` (從 INFO `043F06` 推論)

說話者條件模式：
- INFO 需要別名 `#7` (圖書館守衛，推測為 `zzzBMMq01LibraryGuard`) 的 `GetIsAliasRef == 1`。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`043F02 zzzBMMq01B1LibGoWindhelm`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:703) | `043F03` | Goodbye, SayOnce | `GetStage LessThanOrEqualTo 10`; `GetIsAliasRef alias #7` | 提示："I am going to Windhelm. Are you OK?" (我要去風盔城了，你還好嗎？) 回覆：[保重自己。我會沒事的，看起來守衛先生正保護著我。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:704) |
| [`043F05 zzzBMMq01B2LibReport`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:706) | `043F06` | 無 | `GetStage EqualTo 40`; `GetIsAliasRef alias #7` | 提示："Where is Windhelm report?" (風盔城報告在哪裡？) 回覆：[180年的事，我會說是20年前。我想房間中間有個架子。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:707) |

翻譯筆記：
- 圖書館守衛的對話提取品質較差；"180's" 可能是一個年份 (Lore 中的 1E180？)，需要 ESM 驗證。
- `043F06` 中的回覆表明守衛正在描述哪裡可以找到記錄，暗示這段對話發生在法師學院圖書館內或附近，而非斯丹達爾神廟本體。

## 相關記錄 (Related Records)

根據 `infodiag`，這些並不完全屬於任務 `038524`，但它們與風盔城地牢調查和吸血鬼劇情線相關。

NPC：
- `zzzBMMq01Courier` (別名 #2) - EditorID 尚未驗證；FormID 待定
- `zzzBMMq01Steward` (別名 #4) - 可能是風盔城衛兵隊長或類似角色；FormID 待定
- `zzzBMMq01LibraryGuard` (別名 #7) - 可能是法師學院成員或斯丹達爾祭司；FormID 待定

地點 (從目標推論)：
- 風盔城地牢 (目標 20: "搜索風盔城地牢")
- 風盔城斯丹達爾神廟 (目標 40: "在斯丹達爾神廟找到風盔城報告")
- 未指定的吸血鬼地點 (目標 60: "擊敗風盔城下的吸血鬼")

## 重建筆記 (Reconstruction Notes)

基於來源 (Source-grounded)：
- 此支線任務由 [`038524 zzzBMMq01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:?) 代表，名稱為 `"Empty Jails"`。
- 包含 8 個跨越階段 0 到 100 的目標。
- 無 SCEN 記錄；所有故事講述皆透過對話 INFO 完成。
- 包含三個獨特的對話分支：
  - 信使分支 (別名 #2)，在階段 0 開放。
  - 總管/衛兵隊長分支 (別名 #4)，在階段 10 開放，並透過階段 20–50–100 受到階段限制。
  - 圖書館守衛分支 (別名 #7)，出現在早期 (階段 ≤10) 和階段 40 (報告任務)。
- 多個對話 INFO 帶有 VMAD 腳本 (片段 `BM01_TIF__01038ABA`, `BM01_TIF__01038AC5`, `BM01_TIF__010395A1`, `BM01_TIF__010395A4`, `BM01_TIF__010395A7`)，表明某些選擇可能會觸發 SetStage 或 CompleteQuest 邏輯。確切的 Papyrus 行為此處未解碼。

階段進度 (推論)：
- **階段 0**：StartUpStage (啟動階段)。信使問候對話可用。
- **階段 10**：總管介紹；目標 0 (與信使交談) 可能已完成。目標 10 (與總管交談) 啟動。圖書館守衛的告別對話可用。
- **階段 20**：地牢搜索開始；目標 20 啟動。
- **階段 30–40**：調查加深；少女石像對話可用 (階段 30–50 範圍)。過去事件對話可用 (階段 30–40 範圍)。報告任務對話可用 (階段 40)。目標 30 和 40 啟動。
- **階段 50**：吸血鬼對話變為可用 (階段 50)。目標 50 (再次與總管交談) 啟動。
- **階段 60**：吸血鬼戰鬥開始；目標 60 啟動。
- **階段 100**：最終報告對話可用。目標 100 啟動。吸血鬼擊敗對話可用。
- **階段 110, 255, 9999**：CompleteQuest (完成任務) 標誌表明存在多條可能的完成路徑；確切的路徑選擇需要 VMAD 片段反編譯。

待驗證事項 (Open verification)：
- 反編譯 VMAD 片段 `BM01_TIF__01038ABA`, `BM01_TIF__01038AC5`, `BM01_TIF__010395A1`, `BM01_TIF__010395A4`, `BM01_TIF__010395A7` 以理解 選擇 → SetStage → CompleteQuest 的路徑，以及階段進度是線性的還是分支的。
- 驗證別名 FormID (#2 信使, #4 總管, #7 圖書館守衛) 及其 NPC 記錄，如果其對話行為複雜。
- 驗證目標引用 (特別是目標 20 和 60)，如果空間編排對於任務標記或戰鬥競技場重要。
- 驗證階段 110, 255 和 9999 CompleteQuest 標誌的含義，如果存在多個結局 (好、壞、中性)。
- 從 ESM 單元記錄中提取並檢查風盔城地牢單元佈局和吸血鬼巢穴地點，如果任務流程需要空間知識。
