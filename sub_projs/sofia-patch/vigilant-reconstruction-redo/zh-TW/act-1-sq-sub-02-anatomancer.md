# 第 1 幕 支線任務 支線 02 - 神聖解剖師

狀態：第一個重做切片。基於源代碼、連結優先，無 Gemini。

來源策略：
- 原始台詞連結回提取的源文件，而非完整複製。
- 僅在需要解釋背景或條件時出現簡短的原始片段。
- 保留了階段分支對話和哲學獨白；場景主題被轉錄為主題記錄。

## 任務記錄

[`4D4C3D zzzAoMSubQ02 "神聖解剖師"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:46)

CLI：
- `questdiag Vigilant.esm 0x4D4C3D`
- `infodiag Vigilant.esm 0x4D4C3D`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x4D4C3D`
- EditorID: `zzzAoMSubQ02`
- 名稱: `神聖解剖師`
- 標記: `RunOnce`
- 優先級: `90`
- 類型: `SideQuest` (支線任務)
- 過濾器: `AoM\`

來自 `questdiag` 的階段：

| 階段 | 標記 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 1 | 無 | 空 |
| 10 | 無 | 空 |
| 20 | 無 | 空 |
| 30 | 無 | 空 |
| 40 | 無 | 空 |
| 50 | 無 | 空 |
| 55 | 無 | 空 |
| 60 | 無 | 空 |
| 70 | 無 | 空 |
| 80 | 無 | 空 |
| 100 | 無 | 空 |
| 110 | 無 | 空 |
| 120 | 無 | 空 (×2) |
| 130 | 無 | 空 |
| 200 | 無 | 空 |
| 210 | 無 | 空 |
| 220 | CompleteQuest | 空 |
| 300 | 無 | 空 |
| 310 | CompleteQuest | 空 |
| 999 | ShutDownStage | 空 |
| 9999 | CompleteQuest | 空 |

目標：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:47) | 尋找解剖師 |
| 1 | 目標 | 向解剖師證明你自己 |
| 10 | 目標 | 與解剖師對話 |
| 20 | 目標 | 幫助解剖師 (邪惡) |
| 30 | 目標 | 捕捉雙足羊 |
| 40 | 目標 | 回到解剖師身邊 |
| 50 | 目標 | 將包裹放入機器 |
| 55 | 目標 | 啟動粉碎機 |
| 60 | 目標 | 與解剖師對話 |
| 70 | 目標 | 聆聽生命之歌 |
| 200 | 目標 | 殺死解剖師 (善良) |
| 210 | 目標 | 搜查解剖師的屍體 |
| 300 | 目標 | 解剖未來 |

推論：
- 任務在階段 1–20 根據玩家選擇產生分支：(1) 透過守門人的測試證明清白 → 支持邪惡的解剖師 (路徑 A)，或者 (2) 拒絕/殺死解剖師 (路徑 B)。
- 路徑 A：階段 1–100 涉及獵捕「兩足羊」（人形受害者的委婉說法）、儀式性粉碎機，以及聆聽「生命之歌」（哲學/魔族解說）。
- 路徑 B：階段 200–300 涉及殺死解剖師並透過「解剖學」（透過肉體/器官進行占卜）來決定未來。
- 多個 `CompleteQuest` 標記 (220, 310, 9999) 暗示了至少三個結果分支：220 處的仁慈/流放、310 處的黑暗接受，或 9999 處的綜合結果。

## 別名 / 暫存骨幹

主機任務：
- `4D4C3D zzzAoMSubQ02` 「神聖解剖師」

來自 `infodiag` 條件的對話別名（從 `GetIsAliasRef` 索引引用推論）：
- 別名 `#0`：預計為解剖師（說話者；在受階段限制的 Hello/Goodbye 主題中被提及）。
- 別名 `#1`：守門人人物（開場測試主題 `4D4C42` 中的說話者）；可能是一個阻擋進入的 NPC。

（推論：CLI 沒有提供明確的別名轉儲；角色是根據主題說話者模式和階段條件推論出來的）

## 守門人分支 — 光輝與哲學的審判

任務以守門人詢問玩家是否接受過「考驗」以及是否「學會了彼此相愛」開場。這與第 4 幕的魔族審判門控結構相平行。

### 分支 1：開場審判 — 「你接受過考驗嗎？」

主題 `0x4D4C42 zzzAoMSubQ02TA01B01T01`

條件模式：
- 全域變數 `530B06` (Vigilant.esm) 的 `GetGlobalValue > 2` — 玩家已獲得 3 級以上的「光輝」。
- `GetIsAliasRef 別名 #1` (守門人)。

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C42` | `0x531D03` | InvisibleContinue | `GetGlobalValue(530B06) > 2`; `GetIsAliasRef 別名 #1` | [「你已經三次體會到愛了。你不再需要問問題。剩下的就是尋求光輝。進去吧。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3151) |
| `0x4D4C42` | `0x531D02` | InvisibleContinue | `GetGlobalValue(530B11) > 0` (另一個全域變數); `GetIsAliasRef 別名 #1` | [「太棒了。你已經獲得了光輝。那你就和我們一樣。進去吧。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3152) |
| `0x4D4C42` | `0x4D4C43` | 無 | `GetIsAliasRef 別名 #1` | [「你接受過考驗嗎？你們學會彼此相愛了嗎？」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3153) |

推論：
- 全域變數 `530B06` 和 `530B11` 追蹤從之前的考驗中獲得的「光輝」（可能來自第 1 幕之前的任務）。
- 守門人有三種可能的回應：(1) 玩家的光輝 ≥ 3 (可以通過)，(2) 玩家有替代的光輝指標 (可以通過)，或 (3) 玩家必須回答默認提示。

### 分支 2：拒絕 — 「你在說什麼？」

主題 `0x4D4C44 zzzAoMSubQ02TA01B01T02` 提示：「你在說什麼？」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C44` | `0x4D4C45` | 無 | `GetIsAliasRef 別名 #1` | [「如果你還沒接受過考驗，請離開。這裡不是你該來的地方。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3156) |

### 分支 3：導航 — 「我要去哪裡接受這場苦難？」

主題 `0x4D4C46 zzzAoMSubQ02TA01B01T03` 提示：「我要去哪裡接受這場苦難？」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C46` | `0x4D4C47` | Goodbye | `GetIsAliasRef 別名 #1` | 回應：[「晨星，我建議你前往那個充滿噩夢的城鎮。你的苦難將從那裡開始。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3159) / [「如果你想繞道，就跟隨那個叫阿爾塔諾的傀儡；如果你想走捷徑，就跟隨那個叫奧蘭多的容器。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3160) |

推論：
- 晨星被引用為考驗地點（「充滿噩夢的城鎮」 — 可能是第 1 幕任務 1 或巫女任務）。
- 阿爾塔諾和奧蘭多是容器/傀儡：阿爾塔諾是一個與任務相關的 NPC（一個「傀儡」），奧蘭多是一個容器（可能是一個任務物品持有者或陷阱）。

### 分支 4：審判證明 — 「我已經經歷過考驗了。」

主題 `0x4D4C48 zzzAoMSubQ02TA01B01T04` 提示：「我已經經歷過考驗了。」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C48` | `0x4D4C49` | 無 | `GetIsAliasRef 別名 #1` | 回應：[「那就讓我問你幾個問題。如果你真的經歷過考驗，你就會知道我在說什麼。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3163) / [「那塊石頭是什麼顏色的？它是真的嗎？」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3164) |

推論：
- 守門人開始進行一套關於「石頭」（馬魯克/第 4 幕背景中的核心魔族神器）的哲學測試。

### 分支 5a：錯誤答案（藍色石頭） — 「那塊石頭是藍色的，而且是真的。」

主題 `0x4D4C4A zzzAoMSubQ02TA01B01T05F` 提示：「那塊石頭是藍色的，而且是真的。」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C4A` | `0x4D4C4B` | Goodbye | `GetIsAliasRef 別名 #1` | [「別對我撒謊。如果你是藍色的，你的故事就已經結束了。我現在甚至無法和你交談。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3167) |

推論：
- 如果玩家聲稱石頭是「藍色」的（真實、純淨），他們會被拒絕。這意味著藍色石頭 = 真實的/未受損的馬魯克之眼，它會毀滅持有者。

### 分支 5b：正確答案（紅色石頭） — 「那塊石頭是紅色的。它是假的。」

主題 `0x4D4C4C zzzAoMSubQ02TA01B01T05T` 提示：「那塊石頭是紅色的。它是假的。」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C4C` | `0x4D4C4D` | 無 | `GetIsAliasRef 別名 #1` | 回應：[「噢，太好了。你確實來了。讓我們進入下一個問題。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3170) / [「那個唱歌的人，被歌唱了嗎？那個傀儡有家嗎？」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3171) |

推論：
- 正確答案是石頭是紅色的（受損/不純）且是假的（不是真正的石頭）。這意味著玩家曾接觸過一個虛假/腐化的版本。
- 下一個問題轉向形而上學實體：「唱歌的人」（恐懼領主/音樂力量）和「傀儡」（一個無名特務）。

### 分支 6a：錯誤答案（莫拉格·巴爾） — 「莫拉格·巴爾歌唱了它的名字並標記了它的家。」

主題 `0x4D4C4E zzzAoMSubQ02TA01B01T06F` 提示：「莫拉格·巴爾歌唱了它的名字並標記了它的家。」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C4E` | `0x4D4C4F` | Goodbye | `GetIsAliasRef 別名 #1` | [「那是謊言。莫拉格·巴爾不知道它的名字。這就是為什麼他構思了那個無名傀儡，也是為什麼希格拉格前來腐化它。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3174) |

推論：
- 莫拉格·巴爾並不「知道」那個傀儡的名字；他是故意創造出一個無名之物的。隨後希格拉格（混沌/瘋狂之神）進一步腐化了它。
- 這將希格拉格引入為干預莫拉格·巴爾魔族計畫的力量。

### 分支 6b：正確答案（無名傀儡） — 「不應該歌唱任何無名之人。甚至連回去的地方都失去了。」

主題 `0x4D4C50 zzzAoMSubQ02TA01B01T06T` 提示：「不應該歌唱任何無名之人。甚至連回去的地方都失去了。」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C50` | `0x4D4C51` | 無 | `GetIsAliasRef 別名 #1` | 回應：[「是的，就是那樣。你和我想像的一樣出色。讓我們進入下一個問題。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3177) / [「有人在血之夫人的盛宴上遲到嗎？你知道他們的名字嗎？」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3178) |

推論：
- 正確答案是：傀儡沒有名字，也無法回到其起源（一個處於中間地帶的形而上學陷阱）。
- 「血之夫人」 = 莫拉格·巴爾 / 洛克汗 / 血之誓約人物（艾萊西亞教團神學）。

### 分支 7a：錯誤答案（宴會完整） — 「沒人在她的宴會上遲到。」

主題 `0x4D4C52 zzzAoMSubQ02TA01B01T07F` 提示：「沒人在她的宴會上遲到。」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C52` | `0x4D4C53` | Goodbye | `GetIsAliasRef 別名 #1` | [「是的，你絕對是對的。不應該有人遲到。但那正是被證明是錯誤的地方。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3181) |

推論：
- 守門人承認了這個悖論：所有人本應都到場，但「有些事情出了錯」（一個異常、缺失或不請自來的客人）。

### 分支 7b：正確答案（遊牧者拉扎） — 「拉扎。一個遊牧的倖存者。」

主題 `0x4D4C54 zzzAoMSubQ02TA01B01T07T` 提示：「拉扎。一個遊牧的倖存者。」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C54` | `0x4D4C55` | Goodbye | `GetIsAliasRef 別名 #1` | 回應：[「是的，你是正確的錯誤。而且你知道那裡曾有一些本不該存在的東西。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3184) / [「這意味著你值得見到藍色的星星。現在，進去吧。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3185) |
| | | | OnEnd 執行 `AoMSq02_TIF__024D4C55.Fragment_0` | |

推論：
- 拉扎是一個「遊牧倖存者」 — 可能是對失落部落、流放者或本不該存在於正常誓約中的魔族實體的引用。
- 玩家被授予「藍色星星」（一個矛盾的獎勵；藍色 = 真實/被毀壞的石頭 + 星星 = 神聖願望）並獲准進入解剖師的聖所。
- VMAD 片段暗示在該對話分支結束時會觸發階段推進或任務觸發器。

## 解剖師分支 — 邪惡路徑 (階段 10+)

階段 10 以後引入了解剖師 (別名 #0)。主題根據階段和說話者 ID (`GetIsAliasRef 別名 #0`) 進行門控。

### 解剖師問候 — 「你被選中了。」

主題 `0x4D4C56 zzzAoMSubQ02Hello` [Misc/Hello]

| FormID | INFO | 階段門限 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C56` | `0x4D4C57` | 10 | `GetStage == 10`; `GetIsAliasRef 別名 #0` | [「你被選中了。我多麼嫉妒那個人。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3188) (Happy 情感) |
| | `0x4D4C5C` | 20 | `GetStage == 20`; `GetIsAliasRef 別名 #0` | [「你願意幫我嗎？我們可以期待共同的美好未來。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3189) (Happy) |
| | `0x4D4C5D` | 30 | `GetStage == 30`; `GetIsAliasRef 別名 #0` | [「請不要殺死它。如果你殺了它，它的未來就會流出。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3190) (Happy) |
| | `0x4D4C5E` | 40 | `GetStage == 40`; `GetIsAliasRef 別名 #0` | [「太棒了。太棒了。來吧，到這裡來。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3191) (Happy) |
| | | | OnEnd 執行 `AoMSq02_TIF__024D4C5E.Fragment_0` | |
| | `0x4D4C5F` | 50–60 | `GetStage >= 50` 且 `< 60`; `GetIsAliasRef 別名 #0` | [「來吧，讓我們擠壓未來。讓我們看看生命在閃耀。……！」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3192) (Happy) |
| | `0x4D4C60` | 60 | `GetStage == 60`; `GetIsAliasRef 別名 #0` | [「噢，真不錯。多麼醇厚。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3193) (Happy) |
| | `0x4D4C61` | 70 | `GetStage == 70`; `GetIsAliasRef 別名 #0` | [「好吧，把它穿上。內臟會展現給你看。一個新世界。……」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3194) (Happy) |

推論：
- 階段 10：解剖師稱呼玩家為「被選中者」；顯得有些嫉妒（嫉妒什麼？）。
- 階段 20：徵求玩家對一項共同的「未來」冒險的幫助。
- 階段 30：警告不要殺死「它」（儀式受害者）；生命/靈魂會「流出」。
- 階段 40：儀式批准；傳喚玩家靠近。
- 階段 50-60：準備「粉碎」(機械)；「擠壓未來」。
- 階段 60：事後的感官細節（「醇厚」）。
- 階段 70：最終的裝扮/結合；承諾透過「內臟」(器官/內臟) 展現啟示。

### 生命之歌 — 其他說話者的獨白

一些問候回應標記為說話者 `4D7106`（可能是某個實體/合唱團或是「吹笛者」實體）。

| FormID | INFO | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C56` | `0x4D710F` | `GetIsID == 1` (說話者 `4D7106`) | [「愛、和平、愛、和平、愛……！！！它的重複，美麗的聲音，無盡的重複，此時此刻！」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3195) |
| | `0x4D7110` | `GetIsID == 1` (說話者 `4D7106`) | [「愛與和平。美麗的重複，平衡的當下，融入經歷過的未來！」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3196) |
| | `0x4D7111` | `GetIsID == 1` (說話者 `4D7106`) | [「愛，愛，愛，愛，真神的愛，夢中之神的愛，還有我們的愛！」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3197) |
| | `0x4D7112` | `GetIsID == 1` (說話者 `4D7106`) | [「去愛我，和我們！去愛我，和我們！去向第四種不信之哲學。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3198) |
| | `0x4D7113` | `GetIsID == 1` (說話者 `4D7106`) | [「總共三個，三乘以三，重複的三，那是愛，神的愛！連光線都彎曲了！」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3199) |
| | `0x4D7114` | `GetIsID == 1` (說話者 `4D7106`); RandomEnd | [「一連串偷窺的人，一堵嫉妒之牆，被抹去的麵包。基於基礎的無底，返回的第三個人！」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3200) |

推論：
- 說話者 `4D7106` (可能是「吹笛者」實體或受腐化的集體) 插話進行哲學獨白，強調「愛」、重複和「第四種不信之哲學」。
- 提到的「三個」 / 「總共三個」 / 「重複的三」暗示了一種三位一體或三位一體的神學（可能是莫拉格·巴爾 / 希格拉格 / 無名傀儡）。

### 解剖師 Goodbye

主題 `0x4D4C58 zzzAoMSubQ02GoodBye` [Misc/Goodbye]

| FormID | INFO | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C58` | `0x4D4C59` | `GetIsAliasRef 別名 #0` | [「你能聽到生命之歌嗎？」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3203) |

### 解剖師死亡對話

主題 `0x4D4C5A zzzAoMSubQ02Death` [Combat/Death]

| FormID | INFO | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D4C5A` | `0x4D4C5B` | `GetIsAliasRef 別名 #0` | [「生命的閃耀……」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3206) (Happy 情感) |

## 蒼白之足 / 解剖師審訊 (階段 10+)

第二個主要的 NPC 分支涉及「蒼白之足」 — 解剖師的真實身份或角色。這些主題受分支門控，並揭示了任務的邪惡路徑。

### 分支 1：介紹 / 警告 — 「那個人是誰？」

主題 `0x4D5E54 zzzAoMSubQ02PaleB01T01` 提示：「那個人是誰？」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D5E54` | `0x4D5E55` | SayOnce | `GetStage == 10`; `GetIsAliasRef 別名 #0` | [「最好不要稱呼他們。如果你不小心呼喚它，它就會來到你身邊。當時候到了，你會知道它的名字並呼喚它。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3209) (Fear 情感) |

推論：
- 一位 NPC 警告不要提及解剖師的名字；直呼其名會產生魔族後果。

### 分支 2：角色說明 — 「你在這裡做什麼？」

主題 `0x4D5E57 zzzAoMSubQ02PaleB02T01` 提示：「你在這裡做什麼？」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D5E57` | `0x4D5E58` | 無 | `GetStage == 10`; `GetIsAliasRef 別名 #0` | 回應：[「這是解剖學。我的使命是讀取隱藏在內臟中的未來。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3212) (Neutral) / [「我需要你的幫助。我想知道更多。隱藏在肉體中的秘密。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3213) (Happy) |

推論：
- 解剖學是透過器官/肉體檢查進行的占卜（內臟預言）。解剖師尋求透過儀式解剖來擴展知識。

### 分支 3：任務分配 — 「你想要我做什麼？」

主題 `0x4D5E59 zzzAoMSubQ02PaleB02T02` 提示：「你想要我做什麼？」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D5E59` | `0x4D5E5A` | 無 | `GetStage == 10`; `GetIsAliasRef 別名 #0` | [「我想要一隻雙足羊。他們越年輕，……就越好。它充滿了未來。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3216) (Happy) |
| | | | OnEnd 執行 `AoMSq02_TIF__024D5E5A.Fragment_0` | |

推論：
- 「雙足羊」是人形受害者的委婉說法，最好是年輕人。這是該任務的主要道德考驗：捕捉/犧牲一個生命。
- 片段暗示了階段推進或任務標記觸發。

### 分支 4：懷疑 — 「我不信任任何口氣不好的人。」

主題 `0x4D5E5C zzzAoMSubQ02PaleB03T01` 提示：「我不信任任何口氣不好的人。」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D5E5C` | `0x4D5E5D` | 無 | `GetStage == 20`; `GetIsAliasRef 別名 #0` | [「當我期待未來時，我的胃口就會佔上風。我被允許啃幾口，對吧？」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3219) (Happy) |

推論：
- 解剖師承認對受害者進行了部分吞噬（「啃幾口」）；食慾 = 魔族的飢渴或純粹的惡意。

### 分支 5：善良路徑 — 「你注定現在就死在這裡。(善良)」

主題 `0x4D5E5F zzzAoMSubQ02PaleB04T01` 提示：「你注定現在就死在這裡。(善良)」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D5E5F` | `0x4D5E60` | Goodbye | `GetStage == 20`; `GetIsAliasRef 別名 #0` | [「噢，真不錯。太棒了。請便。而且我想要你解剖我。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3222) (Happy) |
| | | | OnEnd 執行 `AoMSq02_TIF__024D5E60.Fragment_0` | |

推論：
- 如果玩家拒絕任務並試圖殺死解剖師，解剖師會 *歡迎死亡* 並要求死後被「解剖」（剖析/研究）。
- 片段暗示了階段向「善良」(殺死) 結局推進。

### 分支 6：邪惡路徑 — 「我會幫你找一隻雙足羊。(邪惡)」

主題 `0x4D5E62 zzzAoMSubQ02PaleB05T01` 提示：「我會幫你找一隻雙足羊。(邪惡)」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D5E62` | `0x4D5E63` | Goodbye | `GetStage == 20`; `GetIsAliasRef 別名 #0` | [「噢，太棒了。這是你需要的工具。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3225) (Happy) |
| | | | OnEnd 執行 `AoMSq02_TIF__024D5E63.Fragment_0` | |

推論：
- 同意獵捕受害者會獲得工具（用於捕捉/囚禁的設備）。片段推進邪惡任務路徑。

### 分支 7：儀式分析 — 「這到底是什麼……？」

主題 `0x4D5E77 zzzAoMSubQ02PaleB06T01` 提示：「這到底是什麼……？」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D5E77` | `0x4D5E78` | 無 | `GetStage == 60`; `GetIsAliasRef 別名 #0` | [「你看。這對我來說是如此美麗。我可以聞到未來的味道，就像寶石一樣。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3228) (Happy) |

推論：
- 在儀式「粉碎」（階段 50-60）之後，玩家對神器/器官提出質疑。解剖師對透過感官檢查揭示出的「未來」欣喜若狂。

### 分支 8：神器啟示 — 「這告訴了我們什麼？」

主題 `0x4D5E7A zzzAoMSubQ02PaleB07T01` 提示：「這告訴了我們什麼？」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D5E7A` | `0x4D5E7B` | Goodbye | `GetStage == 60`; `GetIsAliasRef 別名 #0` | [「這就是未來的發展。來吧，穿上它。你會聽到生命之歌。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3231) (Happy) |
| | | | OnEnd 執行 `AomSq02_TIF__024D5E7B.Fragment_0` | |

推論：
- 解剖師提供了一個由受害者遺骸製成的王冠、面具或可穿戴神器。戴上它就能進入「生命之歌」 — 一種統一的魔族意識或開悟狀態。

## 場景獨白 — 魔族合唱解說（沒有 SCEN 記錄的主題）

三個主題（FormID 0x4D8320, 0x4D8322, 0x4D8324，以及吹笛者條目）似乎是由集體或吹笛者實體演唱的純對話獨白。根據 CLI，它們在 ESM 中未與場景記錄連結。

### 獨白 1：「混亂與混亂」

主題 `0x4D8320` [Scene/Scene 類別]

| FormID | INFO | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D8320` | `0x4D8321` | (無) | [「混亂與混亂，生命歌唱。恐懼領主的長笛聲，淹沒了諸神的歌聲。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3234) |

推論：
- 「混亂與混亂」 = 一段副歌或實體名稱；「恐懼領主的長笛」 = 希格拉格 / 音樂腐化。

### 獨白 2：「拋棄我們的名字」

主題 `0x4D8322` [Scene/Scene 類別]

| FormID | INFO | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D8322` | `0x4D8323` | (無) | [「讓我們拋棄我們的名字，共享我們沉睡的心。混亂與混亂，將我們的肉體散布到世界的四個角落。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3237) |

推論：
- 呼應分支 6b（「無名傀儡」）；提倡身體溶解和集體合併。

### 獨白 3：「夢中之神」

主題 `0x4D8324` [Scene/Scene 類別]

| FormID | INFO | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D8324` | `0x4D8325` | (無) | [「夢中之神遺忘了我們發現的名字，失眠之心。讓我們像蛆蟲一樣穿過黑暗深處爬行，混亂與混亂。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3240) |

推論：
- 「夢中之神」 = 洛克汗 / 阿卡 (神聖意識的夢境方面)；「失眠之心」 = 覺醒 / 痛苦。

## 吹笛者 / 生命之歌 — 與未知實體的對話 (階段 70+)

主題 `0x4D8329` 和 `0x4D832C` 地址吹笛者 (說話者 `4D7106`，獨特演員) 並共享標記為二重唱或集體肯定的回應。

### 吹笛者主題 1：「讓我們一起歌唱，生命之歌！」

主題 `0x4D8329 zzzAoMSubQ02PiperB01T01` 提示：「讓我們一起歌唱，生命之歌！」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D8329` | `0x4D832A` | 無 | `GetIsID == 1` (說話者 `4D7106`) | 回應 (3×)：(1) [「愛，我，和我們！三中之三，從流浪世界中淨化生命。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3243) (Happy) / (2) [「一連串偷窺的人，一堵嫉妒之牆，一個被抹去的平底鍋。三中之三，讓謊言比愛更清晰。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3244) / (3) [「總共三個，三的倍數，三的重複。三中之三，你如何區分紅色和藍色」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3245) |

推論：
- 三個強調「三中之三」的回應 — 三位一體神學或魔族概念的三位一體。
- 紅色對比藍色 = 腐化對比純潔；一個虛假的二分法（「你如何區分」）。

### 吹笛者主題 2：「我們的使命是什麼！？」

主題 `0x4D832C zzzAoMSubQ02PiperB02T01` 提示：「我們的使命是什麼！？」

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x4D832C` | `0x4D832D` | 無 | `GetIsID == 1` (說話者 `4D7106`) | 回應 (3×)：(1) [「其一，消滅威脅生命的 Hamah 血脈！必須剷除那妓女腐屍的最後一絲殘餘！」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3248) (Anger) / (2) [「其一，滅絕拉扎，閃耀的阻礙者！吃掉這袋糞便中最後一絲腐屍！」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3249) / (3) [「其一，消滅阻礙歌聲的吟遊詩人！燒掉蝨子身上最後一絲腐屍！」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3250) |

推論：
- 三個殲滅目標：(1) Hamah 血脈（未知；可能是魔族或艾萊西亞派系），(2) 拉扎（守門人測試答案中的「遊牧倖存者」），(3) 「吟遊詩人」（可能是阿爾塔諾或某個音樂實體）。
- 明確的魔族 / 種族滅絕語言；「腐屍」、「蝨子」 = 非人化。

## 最終對話結束 — 沉默

主題 `0x56F0BF zzzAoMSubQ02B01End` [Topic/Custom]

| FormID | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| `0x56F0BF` | `0x56F0C0` | Goodbye | `GetIsAliasRef 別名 #1` | [「……」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3723) |
| | | | OnEnd 執行 `AoMSq02_TIF__0256F0C0.Fragment_0` | |

推論：
- 最終分支（可能在任務完成或最終拒絕時）以沉默的省略號結束，返回到守門人。片段可能標誌著任務完成。

## 相關記錄

`infodiag` 沒有直接轉儲明確的 NPC 或物品記錄，但對話和任務結構引用了：

NPCs (推論)：
- 別名 #0：解剖師（說話者 ID 可能是 `4D7106` 或類似的蒼白之足 NPC）。
- 別名 #1：守門人（在玩家證明考驗前阻擋進入的智慧/神秘 NPC）。
- 吹笛者（說話者 `4D7106` / 獨特演員）：集體的「生命之歌」實體或表現。

NPCs (外部引用)：
- 阿爾塔諾：第 1 幕主線任務中的「傀儡」NPC（被引用為導航選項）。
- 奧蘭多：一個「容器」（可能是屍體或受困物品的持有者；被引用為捷徑選項）。

物品：
- 工具 (階段 20)：解剖師授予用於獵捕的；具體物品不明。
- 神器 (階段 60)：由受害者遺骸製成；穿戴後可聽到「歌聲」。

任務：
- 第 1 幕早期的考驗（晨星、巫女任務）：必經的苦難；將全域變數 `530B06` 或 `530B11` 設為准入門檻。
- 相關任務 `011B75` (巫女任務)：完成狀態會影響第 1 幕支線任務 01 中的巫女問候。

## 重建筆記

基於源代碼：
- **任務 zzzAoMSubQ02** 是一場玩家與魔族解剖師（透過肉體/器官進行占卜）和守門人守衛之間的道德分歧遭遇。
- 該任務透過神秘對話測試玩家對第 1 幕背景知識（「石頭」、無名傀儡、莫拉格·巴爾、希格拉格、拉扎）的了解。
- **路徑 A (邪惡)：** 階段 1–70 涉及獵捕一名人形受害者，執行儀式性粉碎，並戴上由受害者遺骸製成的王冠/神器。這將賦予進入「生命之歌」的權限 — 一個提倡大規模滅絕 (Hamah 血脈, 拉扎, 「吟遊詩人」) 的魔族集體意識或開悟。
- **路徑 B (善良)：** 玩家在階段 20 與解剖師對峙/殺死他；解剖師歡迎死亡並要求被解剖。任務在階段 220（仁慈/流放）或 310（黑暗接受）完成。
- 多個完成標記 (220, 310, 9999) 暗示了三種結果：仁慈、黑暗接受或綜合。
- 哲學主題與第 4 幕平行：無名性、魔族傀儡工藝、三位一體神學 (「三中之三」)、腐化對比純潔 (紅色對比藍色石頭)，以及透過神器/肉體的污染。

階段進展 (推論)：
- 階段 0：初始。
- 階段 1：守門人提示。
- 階段 10-20：解剖師介紹與選擇 (幫助/拒絕)。
- 階段 20-55：邪惡路徑 — 獵捕、捕捉、儀式性粉碎。
- 階段 60-70：神器製作/穿戴；生命之歌說明。
- 階段 200-210：善良路徑 — 殺死解剖師，搜尋屍體。
- 階段 220：完成 (仁慈/流放結果)。
- 階段 300-310：完成 (黑暗接受 / 「解剖未來」結果)。
- 階段 999/9999：關閉 / 最終清理。

魔族 / 背景連結：
- 解剖師 (蒼白之足) 受希格拉格 / 莫拉格·巴爾魔族陰謀的脅迫或表現為其代理人。
- 守門人的哲學反映了馬魯克 / 第 4 幕記憶神學（無名性、腐化、失落的起源）。
- 拉扎既被引用為「遊牧倖存者」（守門人測試答案），也被引用為殲滅目標（吹笛者任務聲明），暗示了多重敘事層次或悖論。
- 「生命之歌」和「三中之三」反映了第 1–4 幕中警戒者集體意識 / 腐化的主題。

公開驗證：
- 檢查 NPC `4D7106` (吹笛者 / 生命之歌說話者) 的外觀、陣營以及與解剖師的關係。
- 如果任務腳本可用，請檢查階段 5-10 的進展（守門人測試邏輯、全域變數推進）。
- 檢查腳本 `AoMSq02_TIF__024D4C55`、`AoMSq02_TIF__024D5E5A`、`AoMSq02_TIF__024D5E60`、`AoMSq02_TIF__024D5E63`、`AoMSq02_TIF__024D5E7B`、`AoMSq02_TIF__0256F0C0` 以獲得確切的階段推進、結果門控以及邪惡/善良路徑邏輯。
- 檢查全域變數 `530B06` 和 `530B11` (光輝指標) 以確認它們是由先前的第 1 幕任務設置的。
- 檢查任務 `011B75` (巫女任務；在 act-1-sq-sub-01-witch.md 中引用) 與此任務的關係。
- 檢查 NPC 阿爾塔諾 (`16685A` 或類似) 和容器奧蘭多以確認導航選項。
- 驗證受害者 (「雙足羊」) 的身份和角色 — 它是命名的 NPC、通用的人形生物，還是一個象徵性的佔位符？
- 驗證最終神器 (階段 70 穿戴的王冠/面具) — 材料、附魔、除了對話觸發器之外的遊戲效果。
