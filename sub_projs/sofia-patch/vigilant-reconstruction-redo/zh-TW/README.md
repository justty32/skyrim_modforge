# VIGILANT 劇情重構 (VIGILANT reconstruction redo)

這裡是重新製作的 VIGILANT 劇情與演出結構化整理區。

目標並非摘要，而是可追溯且基於來源的重建 (source-grounded reconstruction)：

- 從 `game-data/mods/Vigilant/` 的原文抽取檔與 `Vigilant.esm` CLI 診斷結果出發。
- 保留 FormID、EditorID、紀錄類型 (record type)、任務目標 (quest objective)、話題提示 (topic prompt)、INFO 回應 (INFO response)。
- 原文主要透過可點擊連結引用，不在整理稿中整段複製。僅在需要解釋短詞、疑難片語或必要對照時才直接摘錄。
- FormID、EditorID、Topic、INFO、BOOK、MISC 等代號也盡量連結至抽取檔或本整理稿的對應位置。
- 翻譯時不刪減行數；必要時標註原文疑難，而非使用華麗的中文掩蓋來源問題。
- 場景演出需查閱 `SCEN`：主機任務 (host quest)、別名 (aliases)、階段 (phases)、動作 (actions)、計時器 (timer)、對話題材 (dialog topic)。
- 不處理 Sofia 擴充台詞；本區僅重建 VIGILANT 本體。

## 數據來源 (Data Sources)

- 抽取文本：
  - `../game-data/mods/Vigilant/quests.md`
  - `../game-data/mods/Vigilant/dialogue.md`
  - `../game-data/mods/Vigilant/books.md`
  - `../game-data/mods/Vigilant/items.tsv`
  - `../game-data/mods/Vigilant/locations.tsv`
  - `../game-data/mods/Vigilant/npcs.tsv`
- ESM：
  - `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`
- CLI：
  - `/home/lorkhan/repo/ModForge/src/ModForge.Cli/bin/Release/net10.0/ModForge.Cli.dll`
- 次要參考資料：
  - `references/zhihu-vigilant-review-notes.md` - 使用者提供的中文玩家分析/評論筆記；僅作為驗證路線圖使用，非權威來源。
  - `references/video-transcript-notes.md` - 整合自使用者提供之三段影片逐字稿的筆記；僅作為驗證路線圖使用，非權威來源。

## 標準紀錄格式 (Standard Record Shape)

```text
## <FormID> <EditorID> "<名稱>"

來源：
- 抽取：<path:line 當可用時>
- cli：<指令>
- 紀錄類型：QUST / SCEN / DIAL / INFO / BOOK / ...

任務：
- 標誌 / 優先級 / 類型 / 過濾器
- 階段
- 目標與目標對象

場景編排：
- 主機任務
- 別名
- 階段
- 動作
- 場景專屬話題

對話：
- 話題 FormID + EditorID + 提示
- INFO FormID + 標誌 + 條件 + VMAD (若相關)
- 原始來源連結
- 正體中文翻譯

筆記：
- 僅限基於來源的內容；
- 明確標註推論；
- 不進行自由發揮的劇情摘要。
```

## 工作隊列 (Work Queue)

1. `act-4-memory-07-marukh.md` - 首個垂直切片：任務 + 對話 + SCEN 編排。**已完成** (格式模板)。
2. `act-4-memory-index.md` - 所有 13 個記憶任務 + 樞紐的索引。**已完成**。
3. `act-4-memory-01..13-*.md` - 每個記憶任務一個基於來源的切片。**已完成 (全部 13 個，2026-06-14)**。
4. **所有切片的待辦事項** (參見索引「狀態」 + 每個切片的「開放驗證」)：反編譯 `CHMeqNN_TIF__*` VMAD 片段，以確定每個分支的 `選擇 → SetStage → CompleteQuest` 路由與好/壞極性；轉儲 QUST 別名/目標以獲取精確的觸發引用；追蹤 `zzzCHMemoryGuide` 樞紐 (`42E0B1`) + 業障全局變數以確定第四章整體的結局門檻。
5. 僅在第四章記憶格式穩定後才重新審視第一至三章。
