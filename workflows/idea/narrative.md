# Ideas — 敘事 / 劇情生成

← [ideas 索引](ideas.md)

## 2. 喜愛劇情模組的遺憾分支改版

劇情模組關鍵節點常缺想要的選擇，想自己補：做平行分支讓玩家走「作者沒寫的那條路」；保留原人設世界觀、盡量以 Patch 形式存在；可能涉及新 INFO/對話樹、條件觸發、任務階段。

---

## 9. 大量劇情自動生成（獨立工作流）

手寫規格無法擴展，需 LLM 驅動的生成管線：

```
故事生成系統（獨立工作流）          ModForge（下游，記錄層）
  ├─ LLM 構思劇情/人物弧線/對話        └─ spec → 合法 ESP，不參與敘事
  ├─ 展開成 ModForge spec JSON
  └─ 呼叫 build → .esp
```

**故事系統自己要解的難題**：跨任務 NPC 狀態記憶；人物個性一致；大量劇情不重複；語音必須排進管線（見 §1）。

**引擎規模天花板（量產前必面對）**：載入順序上限（ESP ~254 / ESL ~4096）→「一任務一 ESP」走不遠，要合併輸出或回收；ESL FormID 預算（2048/4096，一條有對話的任務吃幾十~幾百）；存檔膨脹（每個有腳本的 running Quest 都進存檔）→ 須「完成即 Stop + 清 Alias」。

**量產關鍵槓桿：Story Manager + 條件式 Alias**——輸出「模板任務 + 條件 Alias」而非寫死 NPC，同一 ESP 劇情變化量放大一個數量級。引擎帶事件資料走訪 SM 節點樹、逐層評估條件、Alias 動態填充，全部成功才啟動。**此管線已落地並實機驗證**（SM spec 管線、十種 engine-native 事件、五種 alias fill、可複用 trigger 庫——細節與鐵律見 CLAUDE.md / git）。

**ModForge 可貢獻的未做想法 — `catalog` 資源索引**：故事系統需知「一隻狼 / 一條麵包用哪個 FormKey」。擴充 `catalog` 把 Skyrim.esm（或任意 ESP）批次匯出成**可查詢索引**（SQLite / 分類分片，非單一大 JSON——幾十萬筆 record LLM 讀不完）。兩層內容：

- **資料層**：FormKey / EditorID / 名稱 / 類型 + 關鍵屬性（種族等級、回復量、傷害…）
- **美術層**：NPC 外型、模型/貼圖路徑、語音類型、idle 動畫 event、地點清單；QUST/DIAL/INFO（含第三方模組，避免衝突重複）；FACT/BOOK/RACE/KYWD/WTHR…（原則上涵蓋所有記錄類型）

現有診斷（`npcdiag`/`dump`/`find`）已能拉這些欄位，批次化即可產出。
