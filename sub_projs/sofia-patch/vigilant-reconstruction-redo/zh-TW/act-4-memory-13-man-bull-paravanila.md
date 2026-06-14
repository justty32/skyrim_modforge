# 第四章記憶 13 — 牛頭人帕拉瓦尼亞 (Act 4 Memory 13 - Man-Bull Paravanila)

狀態：重做切片。基於來源 (Source-grounded)，連結優先，非劇情摘要。

來源方針：
- 原始文本行連結回提取的來源文件，而非完整複製。
- 僅在需要解釋翻譯問題時才出現短小的來源片段。
- `SCEN` (場景) 編排來自 CLI 診斷，因為提取的 `dialogue.md` 僅保留了場景話題文本，而未保留場景階段/動作。

## 結構筆記：外殼任務 vs 內容任務 (Structural note: shell quest vs content quest)

此記憶任務較為特殊。記憶包裝器 [`51C038 zzzCHMemoryQuest13 "Man-Bull Paravanila"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:214) 是一個**僅有標頭的外殼 (header-only shell)**：`infodiag 0x51C038` 返回**無擁有話題**，`scenediag 0x51C038` 報告其不是場景，而 `questdiag` 顯示它**無目標**且僅有 7 個階段。尋找通常的 `zzzCHMeQ13` 話題前綴會返回 **0 個匹配項**——此記憶任務完全沒有使用 `MeQ13` 對話命名空間。

所有實際內容 (目標、對話、場景、分支) 都存在於一個獨立的內容任務中：
- [`51ADBF zzzCHSubQuest13 "Broken Horn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171) — EditorID 前綴為 `zzzCHSq13` / `zzzCHSubQuest13`。
- 所有權確認：`scenediag 0x51D636` 報告 `quest = 51ADBF`，且 `infodiag 0x51ADBF` 列出了所有 6 個話題。提示中建議的以 `zzzCHMeQ13` 為前綴的記錄並不存在。

推論：`51C038` (記憶外殼，優先級 99) 框架化/啟動世界內的重現；`51ADBF` (`zzzCHSubQuest13`，優先級 90) 驅動可遊玩的場景。透過轉儲外殼的別名/啟動條件來確認啟動連結 (TODO — `questdiag` 未列印它們)。

## 外殼任務記錄 (Shell Quest Record)

[`51C038 zzzCHMemoryQuest13 "Man-Bull Paravanila"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:214)

CLI 指令：
- `questdiag Vigilant.esm 0x51C038`
- `infodiag Vigilant.esm 0x51C038` → 無擁有話題
- `scenediag Vigilant.esm 0x51C038` → 不是場景

ESM 路徑：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x51C038`
- EditorID: `zzzCHMemoryQuest13`
- 名稱: `Man-Bull Paravanila`
- 標誌: `RunOnce`
- 優先級: `99` (記憶任務中最高的)
- 類型: `Misc`
- 過濾器: `CH\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 標誌 | 日誌 |
|---:|---|---|
| 0 | StartUpStage | 空 |
| 10 | 無 | 空 |
| 20 | 無 | 空 |
| 30 | CompleteQuest | 空 |
| 40 | CompleteQuest | 空 |
| 255 | ShutDownStage | 空 |
| 999 | CompleteQuest | 空 |

目標：外殼任務上無目標 (僅有標頭；目標存在於 `51ADBF` 上)。

階段結果映射 (30 / 40 / 999 消除歧義)：
- 此外殼任務帶有**三個** `CompleteQuest` 階段：`30`, `40`, `999`。
- `999` 位於 `255 ShutDownStage` 旁邊，是記憶任務中常見的**記憶結束關閉**完成階段 (例如 MeQ08/09 也在 999 處執行 `CompleteQuest`)。將 `999` 視為**記憶關閉**完成路徑，而非劇情分支。
- `30` 和 `40` 是早期波段的完成階段，也是**兩個實際結果**的候選。在**內容**任務 `51ADBF` 中，兩個可遊玩的贈送分支皆受限於 `GetStage == 40` (見下文)，因此外殼上的階段 40 與「已贈送/接受禮物」相符——即解決/仁慈路徑。階段 30 是另一個早期完成階段 (玩家離開/未贈送禮物)。外殼上每個階段的確切極性為 **TODO** ——外殼本身的階段片段尚未解碼；業障判讀取自下方內容任務的分支條件。

名稱筆記：外殼任務名稱中的 "Paravanila" 是 **"Paravania" 的拼寫錯誤**，即牛頭人 NPC [`51AE2D zzzCHAlessiaMntr "Paravania the Man-bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:428)。標題中保留來源拼寫；主體為帕拉瓦尼亞。註：待驗證 — 標題寫著 "Paravania"，但螢幕上的說話者別名是 `BelharzaBull` (見演出表)。

## 內容任務記錄 (Content Quest Record)

[`51ADBF zzzCHSubQuest13 "Broken Horn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171)

CLI 指令：
- `questdiag Vigilant.esm 0x51ADBF`
- `infodiag Vigilant.esm 0x51ADBF`
- `scenediag Vigilant.esm 0x51D636`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x51ADBF`
- EditorID: `zzzCHSubQuest13`
- 名稱: `Broken Horn` (斷裂之角)
- 標誌: `RunOnce`
- 優先級: `90`
- 類型: `Misc`
- 過濾器: `CH\`

來自 `questdiag` 的階段 (14 個)：`0 (StartUpStage)`, `1`, `2`, `5`, `10`, `20`, `30`, `40`, `45`, `46`, `50`, `60 (CompleteQuest)`, `255`, `999 (CompleteQuest)`。

目標 (Objective)：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [`Broken horns, sky incarnate.`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:172) | 斷裂之角，蒼穹化身。 |

目標對象 (Objective targets)：
- 1 個目標，ESM 中有 **6 個對象**。
- 對象 1-4 和 6 各有 1 個條件；對象 5 有 2 個條件。
- 目前的 CLI 輸出不列印目標引用；如果對象位置重要，則需要更深層的 QUST 對象轉儲 (TODO)。

## 演出表 / 別名骨幹 (Cast / Alias Backbone)

來自 `scenediag 0x51D636` 的主機任務別名 (主機 = `51ADBF`)：

| 別名 | 名稱 | 填寫方式 |
|---:|---|---|
| 1 | `Container` (容器) | 強制引用 `51ADC1:Vigilant.esm` |
| 2 | `QIHorn` (任務物品：角) | CLI 未列印填寫內容 |
| 3 | `QIRing` (任務物品：環) | CLI 未列印填寫內容 |
| 4 | `QIScroll` (任務物品：卷軸) | CLI 未列印填寫內容 |
| 5 | `BelharzaMan` (人類貝爾哈扎) | 唯一演員 [`0E5E2E zzzCHBelharza "Belharza the Man"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:252) |
| 7 | `Boss` (首領) | 唯一演員 [`51D68A zzzCHBossAmicusTharn "Amicus Tharn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:477) |
| 8 | `BelharzaBull` (公牛貝爾哈扎) | 唯一演員 [`51D61C zzzCHBelharzaBull "Belharza the Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:466) |
| 9 | `BelharzaMntr` (牛頭人貝爾哈扎) | 唯一演員 [`510B22 zzzCHMntrBelharza "Belharza the Man-Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:344) |
| 10 | `Morihaus` (莫里豪斯) | 唯一演員 [`0B253B zzzCHBossMorihaus "Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1106) |
| 11 | `MarkerMem` (記憶標記) | 強制引用 `51D63C:Vigilant.esm` |
| 12 | `MarkerQuiz` (測驗標記) | 強制引用 `51D63D:Vigilant.esm` |
| 13 | `MarkerES` (星讀標記) | 強制引用 `51D63E:Vigilant.esm` |
| 14 | `Dragon` (巨龍) | 唯一演員 [`51D69A zzzCHMemKahKaanKrein`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:479) |

推論：
- 三種不同形態的貝爾哈扎同時被設為別名：人類貝爾哈扎 (`BelharzaMan`, #5)、公牛貝爾哈扎 (`BelharzaBull`, #8)、牛頭人貝爾哈扎 (`BelharzaMntr`, #9)。這編排了一個轉化/生命週期 (人 → 牛 → 牛頭人)，契合「斷裂之角」的主題。
- 整個過程中的**對話說話者**是別名 `#8 BelharzaBull` (所有 4 個自定義/問候 INFO 皆以 `GetIsAliasRef alias #8` 為條件)。公牛無法說話——所有面向玩家的對話行皆表現為無聲的默劇 `"............(…)"`。
- **場景**說話者是別名 `#9 BelharzaMntr` (牛頭人)，他會說話。

## 場景記錄 (Scene Records)

場景記錄未作為完整記錄存在於 `game-data` 中；文本行連結到 `dialogue.md`，而階段/動作來自 `scenediag`。

### 51D636 zzzCHSq13Sc01

CLI 指令：
- `scenediag Vigilant.esm 0x51D636`

編排：
- 主機任務：[`51ADBF zzzCHSubQuest13`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171)
- 演員：別名 `#9` (`BelharzaMntr`), 行為標誌 `NoPlayerActivation, Optional`
- 階段：3 個，每個階段有 0 個開始條件和 1 個完成條件。
- 動作：
  - 索引 1：`Timer` (計時器), 演員 `#9`, 階段 0, `0.5` 秒。
  - 索引 2：`Dialog` (對話), 演員 `#9`, 階段 1, 話題 [`51D637`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3349), 情感 `Neutral` (中性)。
  - 索引 3：`Dialog` (對話), 演員 `#9`, 階段 2, 標誌 `FaceTarget, HeadtrackPlayer`, 話題 [`51D639`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3352), 情感 `Neutral` (中性)。
  - 索引 4：`Package` (AI 程序包), 演員 `#9`, 階段 0-2。

翻譯：
- [`51D637` / INFO `51D638`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3349)：「我沒想到阿卡托什會派來祂的使者……看來眾神還沒有放棄我。」
- [`51D639` / INFO `51D63A`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3352)：「我早已認命，以為自己再也無法活著、無法變回原本的樣子。謝謝你。」
  - 註：原文結尾帶有零散的雙句點 `again. Thank you..`；保留原樣，非遺漏。

推論：此場景在玩家解決公牛問題 (贈送禮物) 後觸發，恢復了牛頭人貝爾哈扎的聲音——即「變回原本的樣子」那句。這被解讀為**仁慈/解決**的回報。極性確認為 TODO (未檢查業障全局變數)。

## 自定義對話分支：公牛貝爾哈扎 (無聲) (Custom Dialogue Branch: Belharza the Bull (silent))

主機任務：[`51ADBF zzzCHSubQuest13`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171)

說話者條件模式：
- 每個 INFO 需要別名 `#8` (`BelharzaBull`) 的 `GetIsAliasRef == 1`。
- 兩個贈送分支額外需要任務 `51ADBF` 的 `GetStage == 40` 以及玩家持有該贈品。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`51D62A zzzCHSubQuest13Hello`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3337) | `51D62B` | 無 | `GetIsAliasRef alias #8` | 「............（牠望著我，彷彿在懇求著什麼。）」 |
| [`51D62E zzzCHSq13BullB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3340) | `51D62F` | 無 | `GetIsAliasRef alias #8` | 提示：「好可愛的小牛，來摸摸牠吧。」 回覆：「............（牠不喜歡被摸。）」 |
| [`51D631 zzzCHSq13BullB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3343) | `51D632` | `Goodbye` | 玩家持有 [`51AD83 zzzCHHornBelhaza "Horn of Belharza"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:990); `GetIsAliasRef alias #8`; `GetStage == 40`; 結束時執行 VMAD `CHSq13_TIF__0251D632.Fragment_0` | 提示：「陛下，這個給您（獻上貝爾哈扎之角）。」 回覆：「............（牠看起來很滿意。）」 |
| [`51D634 zzzCHSq13BullB03T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3346) | `51D635` | `Goodbye` | 玩家持有 [`51AD84 zzzCHRingMorihaus "Nosering of Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:991); `GetIsAliasRef alias #8`; `GetStage == 40`; 結束時執行 VMAD `CHSq13_TIF__0251D635.Fragment_0` | 提示：「陛下，這個給您（獻上莫里豪斯的鼻環）。」 回覆：「............（牠看起來很滿意。）」 |

翻譯筆記：
- 所有回覆皆為無聲默劇般的場景指示 (公牛無法說話)；含義在括號內，保持直譯。
- 兩個贈送提示中的 "Majosty" 是原文中 "Majesty" 的拼寫錯誤。保留原意 (陛下)。註：待驗證 (源文拼字)。
- 對話分支記錄：`B01 = 51D62D`, `B02 = 51D630`, `B03 = 51D633`；對話視圖 `51D62C`。

## 雙結果 (分支) 結構 (Two-outcome (branch) structure)

兩個互動式贈送分支 (`B02` 貝爾哈扎之角, `B03` 莫里豪斯的鼻環) 皆為 `Goodbye` + 結束時帶有 VMAD 片段，且皆要求 `GetStage == 40`。這些是記憶任務中的兩個互動式解決方案：
- 贈送**貝爾哈扎之角** (`51AD83`) —— 公牛自身的傳承遺物。
- 贈送**莫里豪斯的鼻環** (`51AD84`) —— 蒼穹之牛莫里豪斯的遺物 / 貝爾哈扎之父的遺物。

極性：**僅從條件無法判斷。** 兩個回覆完全相同 (「牠看起來很滿意。」)，皆控制相同的階段 40 且皆帶有片段；`questdiag` 未揭示哪個片段引向哪個完成路徑。贈送後的場景 `51D636` (聲音恢復，「眾神還沒有放棄我」) 不論選擇哪件遺物，看起來都是**仁慈/解決**的回報。反編譯 `CHSq13_TIF__0251D632` 和 `CHSq13_TIF__0251D635` 以標記任何好/壞分歧 (TODO)。

## 相關記錄 (Related Records)

根據 `infodiag`，這些並不完全屬於 `51ADBF`，但它們是完整的貝爾哈扎/米諾陶/艾萊西亞背景資訊。

NPC：
- [`51AE2D zzzCHAlessiaMntr "Paravania the Man-bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:428) — 標題主體「帕拉瓦尼亞」。
- [`51D61C zzzCHBelharzaBull "Belharza the Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:466) — 對話說話者 (別名 #8)。
- [`510B22 zzzCHMntrBelharza "Belharza the Man-Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:344) — 場景說話者 (別名 #9)。
- [`0E5E2E zzzCHBelharza "Belharza the Man"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:252) — 別名 #5。
- [`0B253B zzzCHBossMorihaus "Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1106) — 別名 #10，父親。
- [`511D2D zzzCHMemoryAncientMinotaur "Man-Bull of Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:349)
- [`51D68A zzzCHBossAmicusTharn "Amicus Tharn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:477) — 別名 #7 `Boss`。
- [`51EAA8 zzzCHMntrFollower "Mordog the Man-Bull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:487)
- [`51D895 zzzCHMntrLeader "Horbahha the Chief"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:482)

贈品 (任務物品)：
- [`51AD83 zzzCHHornBelhaza "Horn of Belharza"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:990) (EditorID 拼寫錯誤: `Belhaza`)。
- [`51AD84 zzzCHRingMorihaus "Nosering of Morihaus"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:991)。

啟動器 / 觸發器 (啟動 + 場景掛鉤，來自 `find`)：
- `51C036 zzzCHManbullMemoryActTrigger` — 記憶入口啟動器 (推論：啟動外殼 `51C038`)。
- `51C037 CHMem13ActTriggerRef` (上述物品的放置引用), `51C034 CHMem13StartMarkerRef`, `51C03A CHMem13ReturnMarkerRef`。
- `51C03D zzzCHMem13BabyTrigger "Belharza Shard"`, `51C3D9 zzzCHMem13BullESTrigger "Well of Star Reading"`。
- `51ADBE zzzCHBelharzaQuizActTrigger "Belharza's Monument"` + `51C040 zzzCHMsgBelharzaQuiz` (測驗訊息), `51C03F zzzCHBelharzaMonument`。

地點：
- [`51ADC4 zzzCHMemoryMntrCave "Cradle Cave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:2) / [`51ADC5 zzzCHMemMntrCave "Cave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:549)。
- [`51C043 zzzCHCharnelBelharza01 "Concealed Charnel of Belharza"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:78) / [`51C044 zzzCHLocCharnelBelharza`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:612)。
- [`51D6B2 zzzAoMManbullCave "Hidden Village of Minotaur"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:125) (AoM 前綴，相關的「貝爾哈扎的遺產」子任務世界)。

## 重建筆記 (Reconstruction Notes)

基於來源 (Source-grounded)：
- 記憶外殼 [`51C038 zzzCHMemoryQuest13`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:214) 不擁有話題/場景；它是一個優先級為 99 的包裝器。其 `30 / 40 / 999` 完成路徑中：`999` = 記憶關閉 (位於 `255 ShutDownStage` 旁邊)；`30` 和 `40` 是早期波段的兩個結果。
- 所有可遊玩的內容皆在 [`51ADBF zzzCHSubQuest13 "Broken Horn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:171) 中，目標為「斷裂之角，蒼穹化身。」，包含一個 `SCEN` (`51D636 zzzCHSq13Sc01`) 和一組 4-INFO 的公牛對話 (1 個問候 + 3 個自定義)。
- 兩個互動式贈送分支皆受限於 `GetStage == 40` (別名 #8 `BelharzaBull`)：贈送貝爾哈扎之角 (`51AD83`) 或莫里豪斯的鼻環 (`51AD84`)；每個皆帶有 `CHSq13_TIF__…` 結束片段。
- 對話主體 (別名 #8) 是不說話的公牛；場景主體 (別名 #9) 是牛頭人，他說出了那兩行恢復聲音的對話。

語句錯誤 / 已標記術語：
- 外殼任務名稱 `Paravanila` → `Paravania` (NPC `51AE2D`)。待驗證。
- 贈送提示 `Majosty` → `Majesty` (陛下)。待驗證。
- 物品 EditorID `zzzCHHornBelhaza` (拼寫錯誤 `Belhaza`)。保持原樣。
- 場景 `51D639` 來源文本 `Thank you..` (雙句點)。保持原樣。

隔離區交叉檢查 (僅限 ≤60% 導航參考，不作為事實引用)：
- `_gemini-quarantine/.../act-4-exhaustive/memory-13.md` 除標頭外皆為空。`memory-12-13-final.md` 虛構了話題 `zzzCHMeQ13BelharzaB01T01` 以及一段貝爾哈扎的演講 (「我母親是奴隸女王……」)，而這些在 ESM 中**並不存在** (`find zzzCHMeQ13` = 0 個匹配項；`infodiag 0x51ADBF` 僅列出了那 6 個真實的無聲話題)。那些 Gemini 的對話行是捏造的，不予使用。只有目標「斷裂之角，蒼穹化身。」是重疊的，且已由 `questdiag` 獨立驗證。

待驗證事項 (Open verification)：
- 反編譯 `CHSq13_TIF__0251D632` 和 `CHSq13_TIF__0251D635` 以分配哪個贈品引向哪個完成路徑 + 好/壞極性；
- 轉儲外殼任務 `51C038` 的別名 / 啟動條件，以確認其啟動了 `51ADBF` 並標記其自身的 `30` 與 `40` 階段片段；
- 如果空間編排重要，轉儲 `51ADBF` 的 QUST 目標引用 (6 個目標)；
- 如果遺蹟測驗是此記憶進度的一部分，檢查 `BelharzaQuiz` 啟動器 + `zzzCHMsgBelharzaQuiz` 訊息。
