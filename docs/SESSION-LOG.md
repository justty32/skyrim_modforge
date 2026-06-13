# ModForge — Session Log / 進度日誌

**只放「還沒完成」的活狀態**：in-flight、下次要做。**完成的事不留在這裡**——濃縮句進 CLAUDE.md `已落地功能`，過程細節留 git log。

分工：
- **CLAUDE.md** 只放 durable 的東西——專案慣例、`已落地功能` 目錄、`鐵律與踩坑`、`之後可做` roadmap。
- **本檔**只放 in-flight / open 狀態與下次要做。**不記錄已完成的事**（完成 → 移進 CLAUDE.md `已落地` + git log），也不要把 session 進度寫進 CLAUDE.md。
- **待實機測試的項目 + 測試步驟 → `docs/INGAME-TEST-QUEUE.md`**（同樣只列 open，確認後即移除）。

想法備忘錄另見 `docs/IDEAS.md`。

---

## 進行中 / in-flight（跨 session 的活狀態，就地更新；完成即刪）

**拆檔重構**（Workflow 2，behavior-preserving）— `src/ModForge.Cli/Program.Build.Voice.cs`（325 行、超過 300 上限）拆成多個 partial class < 300；subagent 進行中。拆完跑離線測試 → 同步 `docs/CODE_MAP.infra.md` → commit（不 push）。其餘檔案皆未過限，無需動。

**身份系統 Phase-2/C**：尚未做 ③ 聲望/行為追蹤（需先定設計）。其餘子項皆已落地（見 CLAUDE.md「已落地」）。

---

## 下次要做（open）

**整理並優化現有工作流** — 檢視 CODE_MAP 維護鏈、build/package/test loop、`scripts/test-offline.sh`、三檔分離（CLAUDE / SESSION-LOG / INGAME-TEST-QUEUE）是否順手；找重複手動步驟看能否腳本化（fresh-clone 後那六個 `.psc` 編譯、`build`→`voicelines`→`voicediag`→zip 的語音出貨鏈）。

**外部工具依賴清單** — 盤查草案已產出（全 `MODFORGE_*` env var + 外部 binary + 缺檔降級行為），待整理寫進 `docs/TOOLING.md`。
