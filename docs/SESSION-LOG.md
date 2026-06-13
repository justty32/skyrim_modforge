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

**語音解耦（Phase 2，自己一個 spec）** — 把 ModForge 收斂成純 JSON→mod 工具的延續。語音**合成**（text+emotion→.wav）移出成獨立基石專案 `sub_projs/skyrim-voicegen/`（`voicegen.py` + wrappers + README；venv/dataset 仍 gitignore 留本機）；ModForge 只留**包裝**（plan/voicediag、.wav→xwm→.fuz、lip、擺進 `Sound/Voice/`）。切點＝把 `MODFORGE_TTS_BIN` 的 stdin/args↔wav 合約正式化成 `PROTOCOL.md`。兩者靠協議連、不整合。（Workflow-2 等級，需先寫 spec。）

**（可選）IDEAS.md 拆分** — 與 ModForge 無關的 idea 抽去別處，只留工具相關的。本次未動，待確認。
