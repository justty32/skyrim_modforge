# 第 3 幕 湮滅之子 - 065932 zzzCOMq01

狀態：第一個重做切片（第 3 幕任務 01）。基於源代碼、連結優先，並非劇情摘要。

來源策略：
- 原始台詞連結回提取的源文件，而非完整複製。
- 僅在需要解釋翻譯困難或歧義時出現簡短的原始片段。
- `SCEN` 暫存來自提取的文本標記；在完整開發機器上時，完整診斷需要 CLI。

## 任務記錄

[`065932 zzzCOMq01 "湮滅之子"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:276)

CLI（在完整機器上可用時）：
- `questdiag Vigilant.esm 0x065932`
- `infodiag Vigilant.esm 0x065932`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自提取的 `quests.md` 的任務元數據：
- FormID: `Vigilant.esm:0x065932`
- EditorID: `zzzCOMq01`
- 名稱: `湮滅之子`
- 類型: 推測為故事/任務（第 3 幕宅邸篇章）

來自 `quests.md` 的目標：

| 索引 | 來源 | 文本 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:277) | 與格薇妮絲對話 |
| 20 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:278) | 前往貴族宅邸 |
| 30 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:279) | 調查宅邸並解決案件 |
| 60 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:280) | 擊敗朱利亞斯 |
| 70 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:281) | 逃離宅邸 |
| 80 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:282) | 殉教或墮落 |

推論：
- 目標階段 80（`殉教或墮落`）暗示了分歧的道德選擇。
- 該任務涉及調查、戰鬥（`擊敗朱利亞斯`）和逃脫機制。
- `朱利亞斯` 在 NPC 記錄中被標識為 `zzzCOJuliusChildOblivion` 或 `zzzCOJulius`。

## 關鍵角色 (NPCs)

來自提取的 `npcs.tsv`：

| FormID | EditorID | 名稱 | 角色 |
|---|---|---|---|
| `02749A` | `zzzAoMVigilantLibrarian` | 格薇妮絲 | 任務給予者；警戒者圖書館管理員 |
| `061404` | `zzzCOJulius` | 朱利亞斯 | 任務目標/反派 |
| `0461D8` | `zzzCOJuliusChildOblivion` | 朱利亞斯（湮滅之子形態） | 替代或階段形態 |
| `04BFF2` | `zzzCOJuliusDwarvenSpider` | [無名稱] | 可能的僕從/陷阱 |
| `3288E4` | `zzzCODregsOfSithisJulius` | 朱利亞斯 | 西希斯腐化變體 |

## 地點背景

來自提取的 `locations.tsv`：

| FormID | EditorID | 類型 | 名稱 |
|---|---|---|---|
| `04A8B9` | `zzzCONobleMansion01` | CELL | 南布魯恩特宅邸 |
| `04DC3F` | `zzzCONobleMansion02` | CELL | 北布魯恩特宅邸 |
| `060D39` | `zzzCONobleMansion03` | CELL | 南布魯恩特宅邸 |
| `2EBC0B` | `zzzCONobleMansionBasement` | CELL | 地下室 |
| `04F6C8` | `zzzCOUnderMansion` | CELL | 隱藏房間 |
| `3678E9` | `zzzCOLocBruiantMansionSouth` | LCTN | 南布魯恩特宅邸 |
| `3678EA` | `zzzCOLocBruiantMansionNorht` | LCTN | 北布魯恩特宅邸 |
| `3786FE` | `zzzCOLocBruiantMansionHidden` | LCTN | 隱藏房間 |

宅邸似乎是一個多單元結構：南翼、北翼、地下室和隱藏房間。

## 對話分支

### A. 格薇妮絲 (圖書館管理員) — 初始任務對話

分支擁有者：`zzzAoMVigilantLibrarian` (格薇妮絲, `02749A`)

#### 開場與初始派遣

| 主題 | INFO | 條件 | 翻譯 |
|---|---|---|---|
| [`0669E8 zzzCOq01LibB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:775) | (未提取 INFO 索引) | 階段門限於 0 | 提示：「不知是否有一點點？這有一項諮詢。」 回應：「想法已經聚集了嗎？」 |
| [`0669EA zzzCOq01LibB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:779) | (未提取 INFO 索引) | (未指定) | 提示：「稍後幫我」 回應：「稍後我也找到了」 |
| [`0669EC zzzCOq01LibB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:782) | (未提取 INFO 索引) | (未指定) | 提示：「格薇妮絲，怎麼了？」 回應：「從守衛那裡寄來了一封請求增援的信件，發往科洛爾的貴族宅邸 / 我隨時都想派遣守護者……」 |
| [`0669EF zzzCOq01LibB01T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:786) | (未提取 INFO 索引) | (未指定) | 提示：「讓我先去。如果我一週後還沒回來，就派警戒者去找我。」 回應：「好的。請務必小心。我會在前往大教堂前的房子途中準備好馬車」 |
| [`0669F1 zzzCOq01LibB01T05`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:789) | (未提取 INFO 索引) | (未指定) | 提示：「讓我考慮一下」 回應：「這是紮實的……更多」 |

#### 後續分支

| 主題 | INFO | 條件 | 翻譯 |
|---|---|---|---|
| [`0669F4 zzzCOq01LibB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:792) | (未提取 INFO 索引) | 階段門限於 20+ | 提示：「被派去的警戒者叫什麼名字？」 回應：「巴索羅。因為是在你成為守護者之前派出的，你應該沒見過」 |
| [`0669F6 zzzCOq01LibB02T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:795) | (未提取 INFO 索引) | 階段門限於 20+ | 提示：「請求的內容是什麼？」 回應：「是想調查在宅邸裡接二連三發生的離奇死亡事件 / 索隆迪爾說這只是巧合，但我記得巴索羅說過的話，他不會撤退 / 所以巴索羅他正前往宅邸……」 |
| [`0669F9 zzzCOq01LibB03T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:800) | (未提取 INFO 索引) | 階段門限於 20+ | 提示：「告訴我關於那位貴族的事。」 回應：「我是一位在訓練軍用犬客戶藍宮發跡的貴族。我現在大概在元老院有個席位。 / 父親馬克思、母親茱莉亞，家庭成員還有一位兒子朱利亞斯 / 母親茱莉亞去年去世了。在伊琳娜塔湖發現了一具燒焦的屍體 / 根據當時的報告，我會在馬車裡閱讀」 |
| [`06C21E zzzCOq01LibB01T06`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:891) | (未提取 INFO 索引) | 階段門限於 50+ | 提示：「你遇到問題了嗎？」 回應：「信件似乎因內戰而延誤，那是幾個月前的信件 / 守護者沒有回來，可能已經太晚了 / 我想把決定權留給你。我該怎麼辦？」 |

翻譯筆記：
- 提取的文本使用了彆扭的英語（似乎是機器翻譯的日語）。目前按原樣呈現；根據用戶工作流程需要進行中文重譯。
- "Baltholo" 是早先派出的警戒者；可能與下文對話中的 "Balthoro" 相同。
- "Thorondir" 和 "Waruforo" 似乎是 NPC 名稱，需要 ESM 驗證。
- "Marx"（父親）、"Julia"/"Yulia"（母親和可能的小孩）、"Julius"（兒子） — 家庭關係需要澄清。

### B. 巴索羅 — 宅邸入口對話

分支擁有者：（可能是 `zzzCOJulius` 或任務擁有的別名）

| 主題 | INFO | 條件 | 翻譯 |
|---|---|---|---|
| [`066A23 zzzCOq01BalB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:810) | (未提取 INFO 索引) | (未指定) | 提示：(未知) 回應：「一直等著。我丈夫正在等候。請進宅邸 / 讓我退後。請進宅邸。我丈夫正在等你。」 |
| [`066A26 zzzCOq01BalB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:814) | (未提取 INFO 索引) | (未指定) | 提示：「稀有的亞理德人」 回應：「只是從正義中逃亡的人，現在沒什麼好說的。」 |
| [`066A28 zzzCOq01BalB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:817) | (未提取 INFO 索引) | (未指定) | 提示：「令人不快的名字……」 回應：「這是精靈常見的名字。我沒有什麼不尋常的名字。」 |
| [`066A2A zzzCOq01BalB01T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:820) | (未提取 INFO 索引) | (未指定) | 提示：「巴索羅寄信來了，但是……」 回應：「巴索羅也在宅邸裡等著。來吧，進宅邸吧」 |

推論：
- 說話者可能是巴索羅的妻子，或是在宅邸門口迎接玩家的 NPC。
- "Ayreid" 暗示與亞理德（古精靈）的聯繫。
- 「我丈夫正在等候」暗示巴索羅在宅邸內。

場景標記：
- [`066A2D` [Scene/Scene]](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:823)：「這對所有客人來說都很固執。如果不立刻命令死亡的話」

### C. 莫拉格·巴爾 — 對峙對話（墮落分支）

分支擁有者：莫拉格·巴爾（可能是任務中的別名）

| 主題 | INFO | 條件 | 翻譯 |
|---|---|---|---|
| [`066A31 zzzCOq01MolagB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:826) | (未提取 INFO 索引) | (未指定) | 提示：(未知) 回應：「脆弱的，不要脆弱，脆弱的東西或唯一的……」 |
| [`066A33 zzzCOq01MolagB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:829) | (未提取 INFO 索引) | (未指定) | 提示：「你也做你的工作嗎？」 回應：「這位莫拉格·巴爾只是對那些飢餓的人施捨罷了。謝謝不記得 Saredo 會被譴責。 / 是一個奇蹟，我們也在皇帝中。但是，他也是超越一切的麵包。對於那些飢餓的人，除了上面的麵包沒有別的」 |
| [`066A35 zzzCOq01MolagB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:833) | (未提取 INFO 索引) | (未指定) | 提示：「你為什麼玩弄人……」 回應：「令人反胃。每當在你這混蛋身上發現一個 Eseriusu 時，我的眼中就會冒出憤怒 / 所有的夢達斯，所有的創造物都是完全無辜的心靈。那些不平凡的混蛋們完全無辜的燦爛形象 / 你這混蛋也 Eseriusu 也能吞噬一切。到我的內臟，我的 Ayumi 我對它閃電」 |
| [`066A37 zzzCOq01MolagB01T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:838) | (未提取 INFO 索引) | (未指定) | 提示：「莫拉格·巴爾，你的目的是什麼？」 回應：「我是來救那些不同的人。朱利亞斯仇恨的火焰很快就會燒毀那些不同的人。 / 那個小孩的仇恨很強，火焰直到燒死你之前都不會消失。也以防萬一，火焰不合適就像 Ramae 當時一樣 / 這就是為什麼莫拉格·巴爾正向不同的人伸出援手。」 |
| [`066A3A zzzCOq01MolagB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:843) | (未提取 INFO 索引) | (未指定) | 提示：(未知) 回應：「好吧，我會聽取希望 / 通過銅之門？不應該，比如停止行走的時間」 |
| [`066A3C zzzCOq01MolagB02T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:847) | (未提取 INFO 索引) | (未指定) | 提示：「我不與魔族打交道」 回應：「你向神祈禱嗎？斯坦達爾不會試圖拯救可憐的不同的人。你提供關於自己的事會死在火海中保持著希望。 / 艾朵拉畢竟連擁抱偽君子都做不到，甚至不能見飢餓的混蛋，那些冰冷的混蛋 / 然而，莫拉格·巴爾不同。能救那些不同的人。讓我也有許多的奇蹟 / 在這裡，我們試著繼續伸出援手。直到那時你的骨肉和你都有高度」 |
| [`066A3E zzzCOq01MolagB02T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:853) | (未提取 INFO 索引) | (未指定) | 提示：「救我出去」 回應：「那個希望。讓 Kikiireyo」 |

場景標記：
- [`066A44` [Scene/Scene]](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:859)：「只要放棄痛苦就會減輕。那是拋棄任何希望。」

推論：
- `莫拉格·巴爾` 的對話將玩家稱為 "differents"（非人類；可能是受湮滅觸碰的人）。
- 該任務提供了一個魔族契約（目標 80 的「墮落」路徑）。
- "Julius from Na" 和「那個小孩」暗示朱利亞斯被一個小孩實體或力量佔有或腐化。
- 提取的文本質量嚴重下降；需要完整重譯。

標記的翻譯問題：
- "Eseriusu" — 未知術語；可能是名稱或實體（需要 ESM 檢查）。
- "Mundasu" — Mundus（夢達斯）+ 日語所有格？
- "Moragu Val" — 莫拉格·巴爾 + 音譯腐化。
- "Stendhal" — 斯坦達爾（警戒者的神祇）。
- "Edora" — 可能是阿卡托什或其他聖靈的引用。
- 全篇充滿粗俗語言和不連貫的措辭；顯示源對話文件中存在嚴重的編碼或提取錯誤。

## 相關記錄

這些記錄不直接由任務 `065932` 擁有，但出現在任務背景中：

NPCs:
- [`02749A zzzAoMVigilantLibrarian` - 格薇妮絲](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:?) — 警戒者圖書館管理員，任務給予者。
- [`061404 zzzCOJulius` - 朱利亞斯](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:?) — 主要反派；可能是被佔有的受害者。
- [`0461D8 zzzCOJuliusChildOblivion` - 朱利亞斯（湮滅之子形態）](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:?) — 轉化/Boss 形態。
- [`3288E4 zzzCODregsOfSithisJulius` - 朱利亞斯（西希斯變體）](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:?) — 另一條腐化路徑。
- [`12339D zzzCHMolagBal` - 莫拉格·巴爾](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:?) — 宅邸中的魔族聲音/影響力。

地點：
- 南布魯恩特宅邸（單元 `zzzCONobleMansion01`, `04A8B9`）
- 北布魯恩特宅邸（單元 `zzzCONobleMansion02`, `04DC3F`）
- 宅邸地下室（`zzzCONobleMansionBasement`, `2EBC0B`）
- 隱藏房間 / 宅邸下方（`zzzCOUnderMansion`, `04F6C8`）

## 重建筆記

基於源代碼：
- 該任務由 [`065932 zzzCOMq01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:276) 代表，具有涵蓋調查、戰鬥和道德選擇的七個目標。
- 它包含多個由任務連結別名擁有的對話分支（格薇妮絲、巴索羅、莫拉格·巴爾）。
- 任務涉及一個具有潛在隱藏區域的多單元宅邸環境。
- 目標 80（`殉教或墮落`）標誌著至少具有兩個結果的分支結局。

來源質量警告：
- 提取的對話文本顯示出嚴重的降級：英語不連貫，可能來自日語源文件的劣質 OCR 或機械翻譯。
- 提取格式中，許多主題/INFO FormID 未與完整的條件/標記數據配對（提取格式的限制）。
- 場景記錄（`066A2D`, `066A44`）缺乏相位/動作數據；分期細節需要完整的 `scenediag` 輸出。

公開驗證：
- 執行 `questdiag Vigilant.esm 0x065932` 以確認階段計數、CompleteQuest 觸發器和優先級。
- 執行 `infodiag Vigilant.esm 0x065932` 以列出所有擁有的主題，並與上述對話分支進行交叉檢查。
- 對於每個主題（例如 `0669E8`, `066A23`），執行 `infodiag Vigilant.esm 0x<formid>` 以提取 INFO 標記、條件（`GetStage`, `GetIsAliasRef`, `GetItemCount` 等）和 VMAD 片段。
- 反編譯巴索羅和莫拉格·巴爾對話上的 VMAD 片段，以識別階段進展和分支極性（殉教與墮落）。
- 透過 `scenediag` 提取 `066A2D` 和 `066A44` 的完整場景動作數據。
- 驗證 NPC 記錄（`zzzCOJulius*` 變體, `zzzCHMolagBal`）的職業、特技、裝備、對話種族/性別條件。
- 檢查隱藏機制：朱利亞斯 NPC 記錄上的詛咒物品、佔有腳本或轉化機制。
- 調查地點單元中的陷阱引用、謎題元素或受階段門限限制的鎖/鑰匙互動。

公開翻譯工作：
- 需要對莫拉格·巴爾對話進行完整重譯（當前提取內容嚴重損壞）。
- 驗證名稱和術語："Eseriusu", "Thorondir", "Waruforo", "Marukh", "Ramae"，以及第 4 幕記憶背景中的專有名詞錨點。
- 根據任務文本以及宅邸中的任何書籍/日誌，交叉檢查朱利亞斯的家族譜系。
- 澄清「湮滅之子」主題：朱利亞斯是作為魔族實體重生/轉化，還是被一個魔族實體佔有？
