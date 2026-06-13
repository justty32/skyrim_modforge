# plans — 實作規劃（工作流入口）

← [INDEX](../../INDEX.md)｜[CLAUDE.md](../../CLAUDE.md)

**真的要動工前**的詳細實作規劃：精確到哪個檔、什麼 code、什麼測試步驟（bite-sized task + 驗證）。這是本工作流的 **入口**。

階梯：[idea](../idea/ideas.md) → [roadmap](../roadmap.md) → [spec（討論後方案）](../specs/README.md) → **plan（動工前詳規）** → build。

- 本夾 `*.md` = 各功能的逐步實作計畫（檔名 `YYYY-MM-DD-<功能>.md`）。
- 對應的設計方案在 [specs/](../specs/README.md)（命名對應：`<功能>.md` ↔ `specs/<功能>-design.md`）。
- 計畫要遵守的**程式碼慣例 + CODE_MAP 維護鏈**見 [common/conventions](../common/conventions.md)；橫向通則見 [DEV-GUIDE](../../DEV-GUIDE.md)。

## 現役計畫索引

| 計畫 | 一句話 |
|------|--------|
| [2026-06-06-playidle-scene-action](2026-06-06-playidle-scene-action.md) | scene phase 讓指定 actor 播一段 idle 動畫（SCEN 第四種 action）。 |
| [2026-06-07-identity-system-mvp](2026-06-07-identity-system-mvp.md) | 多重身份系統 MVP（讀書→入派系+賦能+宣誓演出+身份問候+商人切換）。 |
| [2026-06-09-lighting-pipeline](2026-06-09-lighting-pipeline.md) | 自訂 LGTM+IMGS + interior CELL 逐欄授權光照（把陰暗地城變明亮）。 |
| [2026-06-09-weather-imagespace](2026-06-09-weather-imagespace.md) | `WeatherSpec` 掛 per-time-of-day IMGS 做室外明亮調色。 |
| [2026-06-13-hazard-record](2026-06-13-hazard-record.md) | 自訂 Hazard (HAZD) record — 半徑週期施法，可法術生成或當靜態陷阱放置。 |
| [2026-06-13-music-record](2026-06-13-music-record.md) | 自訂 Music Tracks (MUST) + Music Types (MUSC)，指派給 cell/worldspace。 |
| [2026-06-13-quest-markers](2026-06-13-quest-markers.md) | spec 產出三種標記：QSTA 任務羅盤箭、XMarker 錨點、XMRK 世界地圖標記。 |
| [2026-06-13-spec-refs-env](2026-06-13-spec-refs-env.md) | spec 的 `$ref`/`$env` 解析層（通用 JSON include + 參數化，預設庫機制）。 |
| [2026-06-13-voice-annotation-index](2026-06-13-voice-annotation-index.md) | `voice-annotate` CLI — 抽取語音 clip 並產 emotion 標註 manifest。 |

> **archive**：已落地、被取代的舊實作計畫封存進 [archive/](archive/README.md)（保留脈絡、不在維護鏈）。本入口檔若膨脹，照 [DEV-GUIDE「結構整理原則」](../../DEV-GUIDE.md) 拆。
