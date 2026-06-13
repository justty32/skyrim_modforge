# ModForge — Session Log / 進度日誌

**只放「還沒完成」的活狀態**：in-flight、下次要做。**完成的事不留在這裡**——濃縮句進 CLAUDE.md `已落地功能`，過程細節留 git log。

分工：
- **CLAUDE.md** 只放 durable 的東西——專案慣例、`已落地功能` 目錄、`鐵律與踩坑`、`之後可做` roadmap。
- **本檔**只放 in-flight / open 狀態與下次要做。**不記錄已完成的事**（完成 → 移進 CLAUDE.md `已落地` + git log），也不要把 session 進度寫進 CLAUDE.md。
- **待實機測試的項目 + 測試步驟 → `docs/INGAME-TEST-QUEUE.md`**（同樣只列 open，確認後即移除）。

想法備忘錄另見 `docs/IDEAS.md`。

---

## 進行中 / in-flight（跨 session 的活狀態，就地更新；完成即刪）

**身份系統 Phase-2/C**：尚未做 ③ 聲望/行為追蹤（需先定設計）。其餘子項皆已落地（見 CLAUDE.md「已落地」）。

---

## 下次要做（open）

（暫無——Phase 2 語音解耦已完成；IDEAS.md 經盤查全屬 ModForge 相關、無可拆出。roadmap 候選見 CLAUDE.md「之後可做」。）

> **環境提醒**：`MODFORGE_TTS_BIN` 需從舊的 repo-root 路徑改指 `sub_projs/skyrim-voicegen/voicegen-f5.sh`（語音已搬家）。
