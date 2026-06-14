# specs/archive — 已落地的設計方案草稿

← [specs/](../README.md)

描述的功能皆已落地（多數實機驗證過）的舊**設計方案**。保留作設計脈絡，**不在維護鏈上**（現況以 git log / [CODE_MAP](../../common/code-map/CODE_MAP.md) / [docs/spec](../../../docs/spec/SPEC-index.md) 為準）。對應的舊實作計畫在 [plans/archive/](../../plans/archive/README.md)。

> 封存件**凍結**：不套 4096 bytes 拆檔門檻，內部歷史連結容忍 stale。已落地的 design/plan 一律移來這裡、不留在現役夾。

| 封存設計方案 | 一句話 |
|--------------|--------|
| [2026-06-04-script-event-entry-spike](2026-06-04-script-event-entry-spike.md) | 自訂 story event 入口 spike（內容自發帶 ref payload 的事件）。 |
| [2026-06-04-story-manager-probe-design](2026-06-04-story-manager-probe-design.md) | SM 探針：掛原版 Kill Actor 根、`sqv` 驗證 SM 啟動。 |
| [2026-06-04-story-manager-spec-pipeline-design](2026-06-04-story-manager-spec-pipeline-design.md) | SM spec 管線：`storyEvent`+`aliases` 自動生 SMBN→SMQN。 |
| [2026-06-06-daylight-dungeon-spell-design](2026-06-06-daylight-dungeon-spell-design.md) | IMAD builder + 開關型「白晝地城」法術。 |
| [2026-06-06-identity-system-design](2026-06-06-identity-system-design.md) | 輕量身份/職業系統（faction 訊號 + grant + gate）。 |
| [2026-06-06-playidle-scene-action-design](2026-06-06-playidle-scene-action-design.md) | scene phase 播 IDLE 動畫（SCEN SceneAdapter fragment）。 |
| [2026-06-06-presence-gated-scene-design](2026-06-06-presence-gated-scene-design.md) | 在場偵測觸發 scene（autoStart banter controller）。 |
| [2026-06-06-scene-action-performance-design](2026-06-06-scene-action-performance-design.md) | scene 非對話 action 演出（package/timer/headtrack）。 |
| [2026-06-09-lighting-pipeline-design](2026-06-09-lighting-pipeline-design.md) | 明亮室內光照（LGTM + CELL XCLL/DALC + IMGS）。 |
| [2026-06-09-weather-imagespace-design](2026-06-09-weather-imagespace-design.md) | 室外調色：IMGS 掛 Weather per-time-of-day。 |
| [2026-06-13-hazard-record-design](2026-06-13-hazard-record-design.md) | 自訂 HAZD（範圍週期施法）；spell-spawn + placement。 |
| [2026-06-13-music-record-design](2026-06-13-music-record-design.md) | 自訂 MUSC/MUST 音樂並掛 cell/worldspace。 |
| [2026-06-13-quest-markers-design](2026-06-13-quest-markers-design.md) | 三種標記：objective QSTA / XMarker anchor / 地圖 XMRK。 |
| [2026-06-13-spec-refs-env-design](2026-06-13-spec-refs-env-design.md) | spec 的 `$ref`/`$env` JSON include + 參數化前處理層。 |
| [2026-06-13-voice-annotation-index-design](2026-06-13-voice-annotation-index-design.md) | 語音情緒標註索引（INFO Emotion seed → 人工校正）。 |
