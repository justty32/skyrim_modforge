# plans/archive — 已落地的實作計畫草稿

← [plans/](../README.md)

描述的功能皆已落地（多數實機驗證過）的舊**實作計畫**。保留作脈絡，**不在維護鏈上**（現況以 git log / [CODE_MAP](../../common/code-map/CODE_MAP.md) / [docs/spec](../../../docs/spec/SPEC-index.md) 為準）。對應的舊設計方案在 [specs/archive/](../../specs/archive/README.md)。

> 封存件**凍結**：不套 8192-byte 拆檔門檻，內部歷史連結容忍 stale（見 [DEV-GUIDE 結構整理原則](../../../DEV-GUIDE.md)）。已完成的 plan/spec 一律移來這裡、不留在現役夾。

| 封存計畫 | 一句話 |
|----------|--------|
| [2026-06-04-script-event-entry](2026-06-04-script-event-entry.md) | 自訂 story event 入口（內容自己發帶 ref payload 的事件，SM 接到啟動模板 quest）。 |
| [2026-06-04-story-manager-probe](2026-06-04-story-manager-probe.md) | SM 探針插件（階段一）：掛原版 Kill Actor 根，遊戲內 `sqv` 驗證 SM 啟動。 |
| [2026-06-04-story-manager-spec-pipeline](2026-06-04-story-manager-spec-pipeline.md) | SM spec 管線（階段二）：quest 宣告 `storyEvent`+`aliases` 自動生 SMBN→SMQN 樹。 |
| [2026-06-06-daylight-dungeon-spell](2026-06-06-daylight-dungeon-spell.md) | IMAD builder + 開關型「白晝地城」法術，打包 in-game 測試。 |
| [2026-06-06-playidle-scene-action](2026-06-06-playidle-scene-action.md) | scene phase 讓指定 actor 播一段 idle 動畫（SCEN 第四種 action）。 |
| [2026-06-07-identity-system-mvp](2026-06-07-identity-system-mvp.md) | 多重身份系統 MVP（讀書→入派系+賦能+宣誓演出+身份問候+商人切換）。 |
| [2026-06-09-lighting-pipeline](2026-06-09-lighting-pipeline.md) | 自訂 LGTM+IMGS + interior CELL 逐欄授權光照（把陰暗地城變明亮）。 |
| [2026-06-09-weather-imagespace](2026-06-09-weather-imagespace.md) | `WeatherSpec` 掛 per-time-of-day IMGS 做室外明亮調色。 |
| [2026-06-13-hazard-record](2026-06-13-hazard-record.md) | 自訂 Hazard (HAZD) record — 半徑週期施法，可法術生成或當靜態陷阱放置。 |
| [2026-06-13-music-record](2026-06-13-music-record.md) | 自訂 Music Tracks (MUST) + Music Types (MUSC)，指派給 cell/worldspace。 |
| [2026-06-13-quest-markers](2026-06-13-quest-markers.md) | spec 產出三種標記：QSTA 任務羅盤箭、XMarker 錨點、XMRK 世界地圖標記。 |
| [2026-06-13-spec-refs-env](2026-06-13-spec-refs-env.md) | spec 的 `$ref`/`$env` 解析層（通用 JSON include + 參數化，預設庫機制）。 |
| [2026-06-13-voice-annotation-index](2026-06-13-voice-annotation-index.md) | `voice-annotate` CLI — 抽取語音 clip 並產 emotion 標註 manifest。 |
