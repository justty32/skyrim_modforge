# 第四章記憶任務索引 (Act 4 Memory Quest Index)

狀態：所有第四章「記憶碎片」任務的基於來源 (Source-grounded) 索引。連結優先，非劇情摘要。

## 來源方針 (Source policy)

- **已驗證的骨幹** (FormID, EditorID, 名稱, 目標文本, 優先級, 階段計數, `CompleteQuest` 分支階段) 直接來自對 `Vigilant.esm` 的 `questdiag` 診斷以及 `game-data/.../quests.md`。這些被視為事實。
- **每個切片的 TODO 欄位** (觸發 NPC/物品, 完整的 `SCEN` 列表, 業障極性, 釋放/結果狀態) 對於大多數任務尚未經過 ESM 驗證；它們被標記為 `TODO`。請勿從次要參考資料中填寫——在構建每個切片時，請從 ESM/提取的文本中獲取。
- `_gemini-quarantine/.../act-4-exhaustive/memory-NN.md` 和 `references/` 僅供 **≤60% 的導航參考**。校準：Gemini 的 `memory-NN.md` 是按任務分組的 `dialogue.md` 原始文本轉儲；話題 FormID 是匹配的 (有利於了解「哪些話題屬於哪裡」)，但它**過度包含**了相關但不屬於該任務的話題 (例如 memory-07 包含了 `zzzCHMeQ05Marukh*` 和 `zzzCHMQ00*` 話題，而 `infodiag` 顯示這些話題不屬於 `06F53C`)。僅將其用於了解查找方向，絕不作為事實依據。

CLI 指令：
- `questdiag <ESM> 0x<FormID>` — 階段 + 目標
- `infodiag <ESM> 0x<FormID> [substr]` — 任務擁有的小話題 + INFO 條件
- `scenediag <ESM> 0x<FormID>` — SCEN 主機/別名/階段/動作

ESM 路徑：`/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

## 框架任務 (樞紐) (Framing quest (hub))

[`42E0B1 zzzCHMemoryGuide "Memory Guide"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:309)
- 標誌: `AllowRepeatedStages`, 優先級: `99`, 類型: `Misc`, 過濾器: `CH\`, 14 個階段，在階段 999 處有單個 `CompleteQuest`。
- 3 個目標 (引用 Dylan Thomas 的 *Do not go gentle into that good night*):
  - obj 100 "Like when the dream no longer needs its dreamer" (就像當夢不再需要夢者)
  - obj 110 "Against the dying of the light" (反抗那光明的凋零)
  - obj 120 "Blind eyes could blaze like meteors and be" (失明的雙眼能如流星般閃耀)
- 角色 (推論，TODO 待驗證)：作為可重複的樞紐，控制/授予各個 `zzzCHMemoryQuestNN`。透過轉儲其別名 + 啟動條件，以及每個階段帶啟動哪個記憶任務來確認。

## 主表 (已驗證的骨幹) (Master table (verified backbone))

| # | FormID | EditorID | 名稱 | 目標 | 優先級 | 階段 | `CompleteQuest` 於 | 切片 |
|---:|---|---|---|---|---:|---:|---|---|
| 01 | `12C4F4` | zzzCHMemoryQuest01 | 審判官 (The Grand Inquisitor) | [連結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:141) | 90 | 10 | 20 / 100 | [完成](act-4-memory-01-grand-inquisitor.md) |
| 02 | `13712B` | zzzCHMemoryQuest02 | 瘋王 (The Mad King) | [連結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38) | 90 | 12 | 30 / 130 | [完成](act-4-memory-02-mad-king.md) |
| 03 | `13965A` | zzzCHMemoryQuest03 | 獵犬騎士 (Knight of Hound) | [連結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:154) | 90 | 12 | 30 / 130 | [完成](act-4-memory-03-knight-of-hound.md) |
| 04 | `140225` | zzzCHMemoryQuest04 | 愚者約翰 (Johan the fool) | [連結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:297) | 90 | 16 | 60 / 100 | [完成](act-4-memory-04-johan.md) |
| 05 | `05AE03` | zzzCHMemoryQuest05 | 阿達·巴爾 (Ada Bal) | [連結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:358) | 90 | 12 | 50 / 120 | [完成](act-4-memory-05-ada-bal.md) |
| 06 | `06A23B` | zzzCHMemoryQuest06 | 奇蹟的殘留 (Remain of Miracle) | [連結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:372) | 90 | 6 | 30 (單個) | [完成](act-4-memory-06-remain-of-miracle.md) |
| 07 | `06F53C` | zzzCHMemoryQuest07 | 馬魯克的誘惑 (Temptation of Marukh) | [連結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:102) | 90 | 13 | 70 / 150 | [完成](act-4-memory-07-marukh.md) |
| 08 | `080E91` | zzzCHMemoryQuest08 | 無名詩人 (The Nameless Bard) | [連結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:195) | 90 | 26 | 90 / 230 / 350 / 370 / 999 | [完成](act-4-memory-08-nameless-bard.md) |
| 09 | `2CAE30` | zzzCHMemoryQuest09 | 來自彼方 (From Beyond) | [連結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:265) | 95 | 14 | 150 / 200 / 999 | [完成](act-4-memory-09-from-beyond.md) |
| 10 | `2A532E` | zzzCHMemoryQuest10 | 血腥的佩林納爾 (Pelinal the Bloody) | [連結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401) | 90 | 40 | 180 / 300 | [完成](act-4-memory-10-pelinal.md) |
| 11 | `2B9BAB` | zzzCHMemoryQuest11 | 風暴過後 (After the Storm) | [連結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:256) | 90 | 16 | 50 / 340 | [完成](act-4-memory-11-after-the-storm.md) |
| 12 | `2BC395` | zzzCHMemoryQuest12 | 昨夜 (Last Night) | [連結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:307) | 90 | 12 | 50 / 310 | [完成](act-4-memory-12-last-night.md) |
| 13 | `51C038`† | zzzCHMemoryQuest13 | 牛頭人帕拉瓦尼亞 (Man-Bull Paravanila) | [連結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:214) | 99 | 7 | 30 / 40 / 999 | [完成](act-4-memory-13-man-bull-paravanila.md) |

† **MeQ13 是一個僅有標頭的外殼**：`51C038` 不擁有任何話題/場景 (`find zzzCHMeQ13` = 0)。實際內容存在於內容任務 **`51ADBF zzzCHSubQuest13 "Broken Horn"`** (目標 "Broken horns, sky incarnate.", quests.md:171) 中。詳見該切片。

所有 13 個任務共用：標誌 `RunOnce`, 類型 `Misc`, 過濾器 `CH\`。階段 999 是 `ShutDownStage` (如果存在)。

### 分支結構 (推論，僅限基於來源的形狀) (Branch structure (inference, source-grounded shape only))

重複出現的 **兩波段 `CompleteQuest`** 模式——一個在 20–90 波段的早期階段和一個在 100–350 波段的較晚階段——是**好/壞 (業障) 記憶結果**的結構特徵：玩家在記憶中的選擇會引導至兩個完成路徑之一。以已驗證的 MeQ07 為例：
- 階段 40 控制 **艾萊西亞 (Alessia)** 分支的開端 (`GetStage==40`, 別名 `#6`)；階段 50 控制 **莫拉格·巴爾 (Molag Bal)** 分支的開端 (`GetStage==50`, 別名 `#5`)；這些分別引向 70 和 150 的完成路徑。詳見 [`act-4-memory-07-marukh.md`](act-4-memory-07-marukh.md)。

**單憑 `questdiag` 無法判斷哪個完成路徑是「好」還是「壞」**——必須根據每個任務的分支對話/條件來閱讀。將分支階段欄位視為「此處存在兩個結果」，而非極性分配。

待驗證的例外情況：MeQ06 只有單個完成路徑 (簡短，6 個階段)——**確認為線性** (單個說話者/別名，無業障分歧；見 [`act-4-memory-06-remain-of-miracle.md`](act-4-memory-06-remain-of-miracle.md))。MeQ08/09/13 有 3 個以上的 `CompleteQuest` 階段 (多結果，或 `CompleteQuest` 兼作關閉階段)——需要針對每個切片進行消除歧義。

## 每個任務的欄位 (TODO = 在切片時從 ESM 提取) (Per-quest fields (TODO = pull from ESM when slicing))

對於下述每個任務：**來源** = 上述 quests.md 連結。**觸發 NPC/物品**, **SCEN 記錄**, **業障/好壞極性**, **釋放/結果狀態** 是每個切片的交付物。

- **07 馬魯克的誘惑 (Temptation of Marukh)** — 已完成，作為格式模板。在 [`act-4-memory-07-marukh.md`](act-4-memory-07-marukh.md) 中重建了觸發器/SCEN/分支：4 個 SCEN (`0708C7`, `0708CC`, `0708D1`, `0708D6`)，艾萊西亞分支 (`0731F4`) vs 莫拉格·巴爾分支 (`073200`)，馬魯克之眼物品門檻 (`071CE2`)。下方的優先隊列將其排除。

### 主體 → 任務 (每個切片皆經過 ESM 驗證) (Subject → quest (ESM-verified per slice))

| 主體 | 任務 | 切片 |
|---|---|---|
| 審判官佩佩 (Pepe) → "Mary the Dark Virgin" (陀思妥耶夫斯基的宗教大法官) | **MeQ01** 審判官 | [01](act-4-memory-01-grand-inquisitor.md) |
| **德羅澤爾 (Dro'zel)** 瘋王 (演員 `137126 zzzCHDrozelMemory`) | **MeQ02** 瘋王 | [02](act-4-memory-02-mad-king.md) |
| 騎士 **瓦拉 (Varla)** + 皇帝 **貝爾哈扎 (Belharza)** + 孩子 **伊諾拉 (Enola)** | **MeQ03** 獵犬騎士 | [03](act-4-memory-03-knight-of-hound.md) |
| **約翰 (Johann)** (玩家角色) + 吟遊詩人 "巴爾" (莫拉格·巴爾使者) | **MeQ04** 愚者約翰 | [04](act-4-memory-04-johan.md) |
| **馬魯克 (Marukh)** + **佩佩 (Pepe)** | **MeQ05** 阿達·巴爾 | [05](act-4-memory-05-ada-bal.md) |
| **佩佩 (Pepe)** (審判官審訊) | **MeQ06** 奇蹟的殘留 | [06](act-4-memory-06-remain-of-miracle.md) |
| **馬魯克 / 艾萊西亞 / 杜爾莎 (Dulsa)** | **MeQ07** 馬魯克的誘惑 | [07](act-4-memory-07-marukh.md) |
| 無名 **詩人** + **拉邁 (Lamae)** + 沃拉爾 (Volar) | **MeQ08** 無名詩人 | [08](act-4-memory-08-nameless-bard.md) |
| **拉邁 (Lamae)** + **謝爾格拉 (Sheogorath)** + 遜 (Tsun) | **MeQ09** 來自彼方 | [09](act-4-memory-09-from-beyond.md) |
| **佩林納爾 / 瑪麗 / 尤瑪里爾 (Umaril)** | **MeQ10** 血腥的佩林納爾 | [10](act-4-memory-10-pelinal.md) |
| 哀悼佩林納爾的 **莫里豪斯 (Morihaus)** + 尤土恩 (Stuhn) 祭司 | **MeQ11** 風暴過後 | [11](act-4-memory-11-after-the-storm.md) |
| **佩林納爾 (帕拉凡特) + 艾萊西亞 (佩里夫)** 重逢 + 阿卡托什 | **MeQ12** 昨夜 | [12](act-4-memory-12-last-night.md) |
| **牛頭人帕拉瓦尼亞 (Paravania)** + 貝爾哈扎 + 莫里豪斯 | **MeQ13** (外殼 → `zzzCHSubQuest13` Broken Horn) | [13](act-4-memory-13-man-bull-paravanila.md) |

對早期猜測的修正：**德羅澤爾已確認為 MeQ02 的主體** (他也出現在 `zzzCHsq*` 支線任務中——兩者皆屬實)。**哈薩瑪 (Hasaama) / 瑪莎 (Martha)** 是 `zzzCHsq*` 支線任務的主體，而非記憶任務。**瑪麗**同時出現在 MeQ01 (收信人) 和 MeQ10 (尤瑪里爾的奴隸) 中。

### 命名陷阱 (尋找 `zzzCHMeQNN` 的配方並非統一) (Naming gotchas (the `find zzzCHMeQNN` recipe is not uniform))

- **MeQ02** 記錄使用前綴 `zzzCHMeQ2King…` (個位數，無補零)；`find zzzCHMeQ02` 無返回結果。
- **MeQ13** `51C038` 是一個僅有標頭的外殼；內容存在於 `zzzCHSq13…` / 任務 `51ADBF zzzCHSubQuest13` 下。
- 當 `find zzzCHMeQNN` 為空時，嘗試 `zzzCHMeQ<n>` (無補零), `zzzCHSq<NN>`, 或透過在任務 FormID 上執行 `infodiag` 來 grep 場景/話題擁有者。

## 狀態：所有 13 個皆已切片 (2026-06-14) (Status: all 13 sliced (2026-06-14))

每個記憶任務 + 模板 (07) 都有一個基於來源的切片 (見「切片」欄位)。剩餘的橫向工作，在每個切片中皆標記為**待驗證事項**：

- **分支極性 / 階段路徑**：大多數切片透過 EditorID (`GoodScene`/`BadScene`) 或分支內容來確定好/壞，但確切的 `選擇 → SetStage → 哪個 CompleteQuest` 接線存在於 **VMAD/TIF 片段腳本** (`CHMeqNN_TIF__*`) 中，此處尚未反編譯。這是所有 13 個任務中最大的待辦事項。
- **每個任務的觸發 NPC/物品**：別名已讀取，但 `questdiag` 不會列印目標引用 / QUST 啟動條件；需要更豐富的別名目標轉儲 (或 `zzzCHMemoryGuide` 樞紐接線)。
- **結局分支 / 業障門檻**：每個記憶的好/壞計數如何影響最終的 VIGILANT 第四章結局——可能是透過 `zzzCHMemoryGuide` 樞紐 (`42E0B1`) + 一個業障全局變數。尚未追踪。

## 驗證積壓任務 (每個欄位的方法) (Verification backlog (method per field))

- **觸發 NPC/物品**：轉儲任務的 QUST 別名 + 別名填寫 (forcedRef / uniqueActor / find-condition) 以及遊戲啟動時啟動的引用。`questdiag` 目前不列印目標引用 (在 07 切片中已註明)；需要更豐富的別名/目標轉儲或直接讀取 ESM。
- **SCEN 記錄**：找到任務的場景 FormID (Gemini 的 `memory-NN.md` 話題組指向場景話題；透過 `infodiag` 確認擁有者)，然後根據 README 標準對每個場景進行 `scenediag` 以獲取主機/別名/階段/動作/計時器/話題。
- **業障極性**：閱讀分支開端 INFO 條件 (`GetStage==`, `GetIsAliasRef`) 和分支內容，以標記哪個完成路徑是「仁慈/好」與「墮落/壞」的結果。如果存在業障全局變數，請進行交叉檢查。
- **釋放/結果狀態**：記憶在每個完成路徑中授予什麼 (物品、派系、全局變數、世界變化) —— 閱讀階段片段 / `CompleteQuest` 階段效果。

## 導航指標 (≤60%，驗證所有內容) (Navigation pointers (≤60%, verify everything))

每個任務的 Gemini 轉儲 (按任務分組的原始 `dialogue.md` 英文文本，過度包含相關話題)：
`_gemini-quarantine/2026-06-14/vigilant-plot-reconstruction/act-4-exhaustive/memory-01.md` … `memory-13.md`

次要參考資料 (僅供驗證路線圖)：[`references/zhihu-vigilant-review-notes.md`](../references/zhihu-vigilant-review-notes.md), [`references/video-transcript-notes.md`](../references/video-transcript-notes.md)。
