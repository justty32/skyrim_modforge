# 致 Haiku：第一至三章重建（約束規則） (For Haiku: Act 1–3 Reconstruction)

接手 VIGILANT 第一至三章 (Act 1–3) 重建時遵守的硬規則。源自第四章重建的教訓（2026-06-14）。

## 標準引用 (Standard references)

- **模板**：[`act-4-memory-07-marukh.md`](act-4-memory-07-marukh.md) — 任務切片的 source-grounded 格式。完整模板，照抄結構。
- **README**：[`README.md`](README.md) — 提取標準、記錄形狀 (record shape)、數據來源。
- **第四章索引**：[`act-4-memory-index.md`](act-4-memory-index.md) — 所有 13 個記憶任務的範圍劃分 (scoping) 格式。第一至三章索引要照這個樣子做。
- **CLI 食譜**：見下面第 3 點。
- **數據來源**：見 README 的 Data Sources；ESM 路徑 + 提取文本路徑保持一致。

## 六條約束規則 (Six binding rules)

### 1. 先劃分範圍，後進行切片 — 絕不可跳過索引

第一至三章不是記憶任務（那是第四章）。前綴也不同：

| 章節 | 類型 | 前綴 | 範例 |
|---|---|---|---|
| 1 | ? | `zzzAoM*` / `zzzCHMQ*` | 透過 ESM 待定 |
| 2 | 風盔城地下 | `zzzBM*` | 待定 |
| 3 | 宅邸 | `zzzCO*` | 例如：`zzzCOMq01` |

**行動**：先照 [`act-4-memory-index.md`](act-4-memory-index.md) 的格式，用 `questdiag` 把每個章節任務盤出來——FormID、EditorID、名稱、目標、優先級、階段計數、`CompleteQuest` 階段。**只列出骨幹；不切片**。

然後：從索引看第三章有幾個主幹任務，再指派代理人按任務一個一個切片。

### 2. 僅限 ESM，絕不抄襲 Gemini 的幻覺

這輪的大坑：Gemini 的 `dialogue.md` 在 MeQ02 / MeQ13 編造了不存在的台詞（使用者發現後驗證過，正文中沒有）。

**規則：**
- **始終以 ESM 作為唯一事實來源** (`questdiag`、`infodiag`、`scenediag`，或用 Mutagen 直接讀取記錄)。
- **game-data/mods/Vigilant/quests.md 等提取文本** 是正文對照，但僅用於「原文在哪」的索引，不抄錄整段。
- **_gemini-quarantine/ 與 references/** 僅供導航參考 (≤60%)——知道去哪找，絕不當作正式宣稱。
- **標註推論 (inference) 需明確**：如果某個階段效果是猜的，寫「(推論：QUST 階段 99 沒有顯式 `CompleteQuest` 代碼，推測由任務觸發器自動清理)」。
- **驗證書籍文本 (booktext) 失敗時降級**：某些英文標題對 `booktext` 工具失敗（編碼或格式問題），改用 `game-data/books.md` 提取版本。

### 3. 原子化工作 — 一個代理人，一個任務，明確的 CLI 食譜

例如給 Haiku 一個任務時，指示要包含：

```
任務：切片 zzzCOMq01 (0x123ABC)

1. 執行 questdiag：
   ModForge.Cli.dll questdiag /path/to/Vigilant.esm 0x123ABC
   
2. 針對此任務擁有的話題執行 infodiag：
   ModForge.Cli.dll infodiag /path/to/Vigilant.esm 0x123ABC

3. 針對找到的每個 SCEN，執行 scenediag：
   ModForge.Cli.dll scenediag /path/to/Vigilant.esm 0xSCENFormID

4. 編寫切片文件：act-3-sq-01-<名稱>.md
   結構：完全遵循 act-4-memory-07-marukh.md 模板
   - 來源 (FormID, EditorID, 提取文本連結)
   - 任務 (標誌, 目標, 階段, 別名)
   - 對話 (話題 + INFO, 條件, 分支)
   - 場景 (階段, 動作)
   - 筆記 (分支極性, 業力結果, 發布狀態)
   - 待驗證事項 (還有哪些待辦事項)
```

**越具體越好**。不要說「執行 CLI」，要把完整命令貼進去。

### 4. 前綴尋找不統一 — 使用後備食譜

某些任務的記錄前綴不一致：

- `find zzzAoMMq01` 可能為空（改試 `zzzAoM01` 或 `zzzAoMmq01`）
- 第三章宅邸的 `find zzzCOMq01` 可能是 `zzzCO01` 或 `find zzzCOsq01`
- 冷港的 `find zzzCHMQ01` 是對應的，但第一至三章的前綴需驗證

**後備食譜 (Fallback recipe)：**
```bash
# 已知任務 FormID 0x<HEX> 時，查它擁有的小話題/場景：
ModForge.Cli.dll infodiag /path/Vigilant.esm 0x<HEX>
# 輸出顯示擁有的話題 → 從話題名稱複製 EDID 前綴
```

如果 `find` 失敗，改用 `infodiag` 反推前綴。

### 5. 不要並行更新索引 — 僅透過主對話進行合併

這輪第四章：多個代理人同時修改 `act-4-memory-index.md`，造成 git 衝突（都改了相同的行）。結果主對話合併時要手動對帳。

**行動：**
- **主對話是唯一的索引維護者**。如果需要並行多個任務切片，切片者各自編寫其 `.md` 檔，但索引修改僅由主對話執行。
- Haiku 切完某個任務時，**不直接修改索引**；只是新增好 `act-3-sq-NN-<名稱>.md` 檔，在 commit message 中列出「新文件 + 待驗證事項」，主對話集中對帳所有 commits 後統一修改一次索引。

### 6. booktext 在某些英文標題上失敗 — 後備至提取文本

某些書籍記錄的英文翻譯文本對 ModForge CLI `booktext` 工具失敗：

- 症狀：ModForge.Cli booktext 噴出錯誤或吐出亂碼
- 根本原因：Skyrim.esm BOOK 記錄的編碼正文有特殊字符或邊界位置，ModForge 的解碼器有邊緣案例（不是 bug，是已知限制）
- **後備 (Fallback)**：使用 `game-data/mods/Vigilant/books.md` 提取版本（已手工驗證過）

如果 CLI booktext 失敗，檢查同名書籍是否在提取的 books.md 裡；有的話，直接引用並標註「(來自提取的 books.md，CLI 解碼在 FormID 0x... 上失敗)」。

---

## 如何使用此文件

Haiku 開始第一至三章任務時，提示應包含：

> 接手 VIGILANT 第一至三章重建。標準看 `README.md`、`act-4-memory-07-marukh.md` 模板、`act-4-memory-index.md` 樣式。
> 
> 遵守此文的六點規則：[連結至此文件]
> 
> 任務：[具體的任務 FormID + CLI 食譜]

直接連結到這個檔案，Haiku 會看到每一條規則的完整說明，不會被壓縮掉。
