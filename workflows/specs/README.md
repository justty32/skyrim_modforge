# specs — 設計方案（工作流入口）

← [INDEX](../../INDEX.md)｜[CLAUDE.md](../../CLAUDE.md)

一個 idea / roadmap 項**認真討論後產出的設計方案**：目標、架構、資料流、權衡、取捨。這是本工作流的 **入口**。

階梯：[idea](../idea/ideas.md)（不確定要不要做）→ [roadmap](../roadmap.md)（會做、不確定何時）→ **spec（討論後的方案）** → [plan](../plans/README.md)（動工前的詳細實作規劃）→ build。

- 本夾 `*.md` = 各功能的設計方案（檔名 `YYYY-MM-DD-<功能>-design.md`）。
- 對應的逐步實作在 [plans/](../plans/README.md)。
- 設計脈絡的 idea 雛形在 [ideas](../idea/ideas.md)。
- 設計涉及 code 結構/慣例時參考 [common/conventions](../common/conventions.md)；橫向通則見 [DEV-GUIDE](../../DEV-GUIDE.md)。

> **archive**：已落地、被取代的舊設計方案封存進 [archive/](archive/README.md)（保留脈絡、不在維護鏈）。本入口檔若膨脹，照 [DEV-GUIDE「結構整理原則」](../../DEV-GUIDE.md) 拆。

## 現役設計方案（各對應 [plans/](../plans/README.md) 同名 plan）

| 設計方案 | 一句話 |
|----------|--------|
| [2026-06-06-identity-system-design](2026-06-06-identity-system-design.md) | 輕量身份/職業系統（faction 訊號 + grant + gate）；plan = `2026-06-07-identity-system-mvp` |
| [2026-06-06-playidle-scene-action-design](2026-06-06-playidle-scene-action-design.md) | scene phase 播 IDLE 動畫（SCEN SceneAdapter fragment） |
| [2026-06-09-lighting-pipeline-design](2026-06-09-lighting-pipeline-design.md) | 明亮室內光照（LGTM + CELL XCLL/DALC + IMGS） |
| [2026-06-09-weather-imagespace-design](2026-06-09-weather-imagespace-design.md) | 室外調色：IMGS 掛 Weather per-time-of-day |
| [2026-06-13-hazard-record-design](2026-06-13-hazard-record-design.md) | 自訂 HAZD（範圍週期施法）；spell-spawn + placement |
| [2026-06-13-music-record-design](2026-06-13-music-record-design.md) | 自訂 MUSC/MUST 音樂並掛 cell/worldspace |
| [2026-06-13-quest-markers-design](2026-06-13-quest-markers-design.md) | 三種標記：objective QSTA / XMarker anchor / 地圖 XMRK |
| [2026-06-13-spec-refs-env-design](2026-06-13-spec-refs-env-design.md) | spec 的 `$ref`/`$env` JSON include + 參數化前處理層 |
| [2026-06-13-voice-annotation-index-design](2026-06-13-voice-annotation-index-design.md) | 語音情緒標註索引（INFO Emotion seed → 人工校正） |
