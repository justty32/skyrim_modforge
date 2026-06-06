# Archive — 已落地的 plan / spec 草稿

這裡是 superpowers workflow 產出的 **implementation plan** 與 **design/spec** 草稿，
描述的功能皆已落地並（多數）實機驗證通過。保留作為設計脈絡參考，**不在維護鏈上**
（功能現況以 `git log` / `docs/CODE_MAP.*` / `docs/SPEC-*.md` 為準）。

`docs/IDEAS.md` 仍以「設計/計畫見 …」指標連結到此處的對應檔案。

| 檔案 | 對應已落地功能 |
|------|----------------|
| `plans/2026-06-04-story-manager-probe.md` · `specs/2026-06-04-story-manager-probe-design.md` | SM Kill 探針（階段一 throwaway，已被 spec 管線取代） |
| `plans/2026-06-04-story-manager-spec-pipeline.md` · `specs/2026-06-04-story-manager-spec-pipeline-design.md` | SM spec 管線（`storyEvent` + `aliases`） |
| `plans/2026-06-04-script-event-entry.md` · `specs/2026-06-04-script-event-entry-spike.md` | ScriptEvent 自訂入口（`SendStoryEvent` 派發器） |
| `plans/2026-06-06-daylight-dungeon-spell.md` · `specs/2026-06-06-daylight-dungeon-spell-design.md` | 白晝地城法術 + IMAD builder |
| `specs/2026-06-06-presence-gated-scene-design.md` | autoStart 在場偵測 Scene |
| `specs/2026-06-06-scene-action-performance-design.md` | Scene 非對話 action（Package/Timer 演出） |
