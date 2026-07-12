# scene-capture-bridge — 之後再做（backlog）

← [README](README.md)（現況導航）｜[phases](phases.md)（已落地實作記錄）｜[appendix](appendix.md)（細摳原文＋驗證清單）

**活躍成長區**：新想法都記這。做完就搬進 [phases.md](phases.md)（已落地實作記錄，標日期/DLL crc），從這裡刪除。

---

## 仍未做

- **`sc cap` 物件類 vs `sc pk` 分工（使用者再想，先照舊）**：`sc cap` 記 NPC/player 含全身物品＋extra data（v7 已落地）；物件類 capture 與 `sc pk` 滴管感覺功能重複，使用者還要想想——**傾向仍記錄**，暫不動。
- **📌 導航網格（navmesh）——「超重要，之後得開始考慮」（使用者 2026-07-11 晚）**：編輯器流程目前完全沒碰 navmesh——擺出的建築/障礙物會擋住 vanilla navmesh 但 NPC 照原網格走（穿模/卡住），marker 生的 NPC 若落在無網格處也不會動。ModForge 已有程式化 navmesh 能力可接（custom worldspace NAVM＋NAVI additive override Skyrim.esm:0x12FB4 in-game 驗過，見 idea/asset-pipelines/map-scene/geometry.md 一帶＋Vigilant.esm 解碼參考）；難點在**編輯 vanilla cell**：要 override 既有 NAVM（cut/finalize 語意）而不只是新建。方向未定（DLL 端記錄擺放物 footprint → ModForge 端裁切？或先只處理「新增小平台補網格」？），需要時開獨立 plan。
  - **✅ 已開獨立 plan（2026-07-12）：[plans/navmesh.md](../navmesh.md)**——兩個結論：① **「擺的東西擋住 NPC」根本不必改 navmesh**：用 vanilla 的 **L_NAVCUT 碰撞體積**（`CollisionMarker` 0x000021 ＋ `CollisionLayer=49` ＋ Primitive box，**HearthFires 蓋房子用了 1220 筆**）就能 runtime 裁切，純 Mutagen 一筆 REFR（⚠️ 光加 Obstacle flag 無效——L_STATIC 不是 NavmeshObstacle 層）。② **「NPC 走上新平台」非寫 NAVM 不可，而 override vanilla NAVM 可行**（Mutagen no-op override 離線 byte-diff ＝ IDENTICAL；USSEP 807 筆真的這麼幹；NAVI 是加法式 merge 不是地雷）；鐵律＝**永不重新編號 triangle**（鄰居的 EdgeLink 存的是你的 triangle index）。分期 P1 診斷 → P2(T2.0) navcut → P0 spike → P3 add+link → P4 遊戲內採集。
- **紅/綠半透明輪廓高亮**（使用者第二輪：`sc del dp1` 被刪物件紅框、`sc pl dp1` 新增物件綠框，顏色/透明度 Settings 可調）——**較難、非必做**（需 render/shader 或 highlight 效果）。
- marker 編輯視窗下拉：寶石種類 ＋ 發光開關（需 SceneCaptureTools.esp 多個 ACTI 變體或動態換 model，較大工程）。
