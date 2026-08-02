# 解碼參考檔索引（investigation/decode）

← [investigation](../README.md)｜[INDEX](../../../INDEX.md)｜踩坑見 [investigation/gotchas](../gotchas.md)

真 mod 解碼 + 對照 ModForge 可實現性（2026-06-13，全 esp-only 記憶體安全）。這些是**對往後 ModForge 開發有用的調查參考**，不是要實作的清單；浮現的待補項見 [ROADMAP](../../roadmap/README.md)。

## 母索引

- **盤點**：[`mod-survey-2026-06-13.md`](mod-survey-2026-06-13.md)（下載 mod 的 Tier 1/2/3 解碼價值；解碼方法的記憶體鐵律；本清單的母索引）

## 各 mod 解碼

- **隨從擴充**：[`../sofia-patch/`](../../../../sofia-patch/README.md)（**獨立消費者專案**，拿 ModForge 當工具；`README.md` 為索引）— `reference/follower-decode-2026-06-13.md`（結構+內容索引）、`plans/expansion-plan-2026-06-13.md`（11✅/3🟡/2🔴）、`reference/sofia-personality.md`（性格分析/寫作 brief）
- **NPC 日程**：[`ai-overhaul-decode-2026-06-13.md`](ai-overhaul-decode-2026-06-13.md)、[`ai-overhaul-expansion-plan-2026-06-13.md`](ai-overhaul-expansion-plan-2026-06-13.md)（6✅/3🟡/3🔴）
- **VIGILANT**：[`vigilant-worldspace-decode`](vigilant-worldspace-decode-2026-06-13.md)、[`vigilant-story-decode`](vigilant-story-decode-2026-06-13.md)、[`vigilant-magic-decode`](vigilant-magic-decode-2026-06-13.md)、[`vigilant-scene-dialogue-audit`](vigilant-scene-dialogue-audit-2026-06-13.md)（11 自訂 worldspace / 120 quest 78 scene / 712 spell 550 MGEF；scene/對話 vs ModForge **~70% 覆蓋**）。對話缺口大批補齊後**仍開**的項目見 [ROADMAP](../../roadmap/README.md)；已補的見 [feature-dev/landed](../../feature-dev/landed/README.md)。
- **工作流可行性**：[`blender-layout-feasibility-2026-06-13.md`](blender-layout-feasibility-2026-06-13.md)（Blender 擺設→`placements[]` JSON,可行、不需新功能、#1 風險=旋轉轉換校準、選配 `staticmap` 子指令）
- **cell 逆向**：[`sleeping-giant-inn-reverse-2026-06-13.md`](sleeping-giant-inn-reverse-2026-06-13.md)（`cellrefs` 逆向 vanilla interior cell → `placements[]`）

## 待解碼候選

- RDO（dialogue/INFO）
- Moons And Stars（weather/IMGS/climate→region）
