# For Haiku: Act 1–3 Reconstruction (binding rules)

接手 VIGILANT Act 1–3 重建時遵守的硬規則。源自 Act 4 重建的教訓（2026-06-14）。

## Standard references

- **Template**: [`act-4-memory-07-marukh.md`](act-4-memory-07-marukh.md) — quest slice 的 source-grounded 格式。完整模版，照抄結構。
- **README**: [`README.md`](README.md) — extraction 標準、record shape、data sources。
- **Act 4 index**: [`act-4-memory-index.md`](act-4-memory-index.md) — all 13 memory quests 的 scoping 格式。Act 1–3 index 要照這個樣子做。
- **CLI recipes**: 見下面第 3 點。
- **Data sources**：見 README 的 Data Sources；ESM 路徑 + extracted text paths 保持一致。

## Six binding rules

### 1. Scoping first, slicing second — do NOT skip the index

Act 1–3 不是 memory quest（那是 Act 4）。前綴也不同：

| Act | Type | Prefix | Example |
|---|---|---|---|
| 1 | ? | `zzzAoM*` / `zzzCHMQ*` | TBD via ESM |
| 2 | Windhelm underground | `zzzBM*` | TBD |
| 3 | Mansion | `zzzCO*` | e.g., `zzzCOMq01` |

**Action**: 先照 [`act-4-memory-index.md`](act-4-memory-index.md) 的格式，用 `questdiag` 把每個 chapter quest 盤出來——formID、editorID、name、objective、priority、stage count、`CompleteQuest` stages。**只列出 backbone；不切片**。

Then: 從 index 看 Act 3 有幾個主幹 quest，再派 agent 按 quest 一個一個切。

### 2. ESM-only, never copy Gemini hallucinations

這輪的大坑：Gemini 的 `dialogue.md` 在 MeQ02 / MeQ13 編造了不存在的台詞（user 發現後驗證過，正文沒有）。

**Rule:**
- **Always use ESM** as the single source of truth (`questdiag`, `infodiag`, `scenediag`，或用 Mutagen 直接讀 record）。
- **game-data/mods/Vigilant/quests.md 等 extracted text** 是正文當代，但只用於「原文在哪」的索引，不抄整段。
- **_gemini-quarantine/ 和 references/** ≤60% navigation only——知道去哪找，絕不當 claim 用。
- **标注 inference 明確**：如果某個 stage effect 是猜的，寫「(inference: QUST stage 99 没有显式 `CompleteQuest` 代码，推测由 quest trigger 自动清理)」。
- **驗證 booktext 失敗時降級**：某些 English strings 對 `booktext` 工具 fail（編碼或格式），改用 `game-data/books.md` extracted 版本。

### 3. Atomize work — one agent, one quest, explicit CLI recipes

例如給 Haiku 一個 quest 時，指示要包含：

```
Task: slice zzzCOMq01 (0x123ABC)

1. Run questdiag:
   ModForge.Cli.dll questdiag /path/to/Vigilant.esm 0x123ABC
   
2. Run infodiag for topics owned by this quest:
   ModForge.Cli.dll infodiag /path/to/Vigilant.esm 0x123ABC

3. For each SCEN found, run scenediag:
   ModForge.Cli.dll scenediag /path/to/Vigilant.esm 0xSCENFormID

4. Write slice file: act-3-sq-01-<name>.md
   Structure: follow act-4-memory-07-marukh.md template exactly
   - Source (FormID, EditorID, extracted text link)
   - Quest (flags, objectives, stages, aliases)
   - Dialogue (topic + INFO, conditions, branches)
   - Scenes (phases, actions)
   - Notes (branch polarity, karma outcome, release state)
   - Open verification (what's still TODO)
```

**越具體越好**。不要說「run the CLI」，要把完整命令貼進去。

### 4. find <prefix> is not uniform — use fallback recipe

某些 quest 的 record prefix 不一致：

- `find zzzAoMMq01` 可能空（改試 `zzzAoM01` 或 `zzzAoMmq01`）
- Act 3 mansion 的 `find zzzCOMq01` 可能是 `zzzCO01` 或 `find zzzCOsq01`
- Coldharbour 的 `find zzzCHMQ01` 是對的，但 Act 1–3 的前綴需驗證

**Fallback recipe:**
```bash
# 已知 quest FormID 0x<HEX> 時，查它 own 的 topics/scenes:
ModForge.Cli.dll infodiag /path/Vigilant.esm 0x<HEX>
# Output shows owned topics → copy EDID prefix from topic names
```

如果 `find` fail，改用 `infodiag` 反推 prefix。

### 5. Do NOT parallelize index updates — merge via main session only

這輪 Act 4：多 agent 同時改 `act-4-memory-index.md`，造成 git race（都改了 same lines）。結果主 session merge 時要手動對帳。

**Action:**
- **主 session 是唯一的 index 維護者**。如果需要並行多 quest 切片，切片者 writes 各自的 `.md` 檔，但 index 改 commit 只由主 session 做。
- Haiku 切完某個 quest 時，**不直接改 index**；只是 add 好 `act-3-sq-NN-<name>.md` 檔，commit message 列出「new file + open verification item」，主 session 集中對帳所有 commits 後統一改一次 index。

### 6. booktext fails on some English strings — fallback to extracted text

某些 book record 的 English 翻譯文本對 ModForge CLI `booktext` 工具失敗：

- 症狀：ModForge.Cli booktext 噴 error 或吐出亂碼
- 根本原因：Skyrim.esm BOOK records 的 encoded body 有特殊字符或邊界位置，ModForge 的 decoder 有邊界 case（不是 bug，是已知 limitation）
- **Fallback**: 用 `game-data/mods/Vigilant/books.md` extracted 版本（已手工驗證過）

如果 CLI booktext fail，check 同名 book 是否在 extracted books.md 裡；有的話，直接引用並標註「(from extracted books.md, CLI decode failed on formID 0x...)」。

---

## How to use this file

Haiku 開 Act 1–3 任務時，prompt 應包含：

> 接手 VIGILANT Act 1–3 重建。標準看 `README.md`、`act-4-memory-07-marukh.md` 模板、`act-4-memory-index.md` 樣式。
> 
> 遵守此文的六點規則：[link to this file]
> 
> 任務：[具體的 quest FormID + CLI recipe]

直接 link 到這個檔案，Haiku 會看到每一條規則的完整說明，不會被 compact 壓縮掉。
