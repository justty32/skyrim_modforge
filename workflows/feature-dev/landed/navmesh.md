# 已落地 — 世界：navmesh（NAVM override／L_NAVCUT／navPatches）

← [landed index](README.md)｜對應 [CODE_MAP.world](../../common/code-map/CODE_MAP.world.md)｜同域：[world.md](world.md)（入口）· [worldspace.md](worldspace.md) · [world-generators.md](world-generators.md) · [navmesh.md](navmesh.md) · [scene-capture-bridge.md](scene-capture-bridge.md)

## navmesh P0/T2.0 兩個地基實驗 · IN-GAME 2026-07-12（[plan](../../plans/navmesh.md)）

兩個 🎮 實機閘同日皆 **PASS**，定了整份 navmesh plan 的重心：

1. **P0（vanilla NAVM no-op override）**：`ModForgeNavmeshNoop.zip`（僅 10 張逐位元組原封不動的 vanilla NAVM，零 NPC/擺放/腳本）裝上排在 USSEP 之後，白漫（Bannered Mare 內裝＋外景大門～市集）NPC **一切正常**——衛兵巡邏、店內走動皆照舊。⇒ **引擎接受「來自 plugin 的重新序列化 NVNM」**，不只是格式層 byte-identical（U1），是**引擎真的會用**。這是 P2/P3（override vanilla navmesh 改幾何）的地基。
2. **T2.0（L_NAVCUT 證偽）**：白漫大街兩條完全相同的車道（同 NPC/package/告示牌線），**唯一差別是 TEST 車道埋了一顆看不見的 L_NAVCUT box**——結果 **TEST 繞過去了**（繞過告示牌線端點走一個大弧，另觀察到線前徘徊不肯過），**CONTROL 一路直穿**。⇒ **L_NAVCUT 碰撞體積確實在 runtime 裁掉 vanilla navmesh**。**症狀①（NPC 走進新蓋的房子/牆）就此結案，不必碰 NAVM 一個 byte**——已把 `Spec.NavCuts.cs` 的 `AutoNavCuts` 翻回預設 `true`（commit `80a2873`）：擋路的大體積 placement 現在自動配一顆 navcut box。

**T2.0 的對照組實驗設計**（可複用的方法論，不只是「功能過了」）：兩條車道**唯一變因是那顆看不見的盒子**。**刻意不放真牆**——NPC 撞牆會沿牆滑走，「它繞過去了」會分不清是 navcut 生效還是單純撞牆滑開，變成偽陽性；改用只有 18×18 單位、NPC 直接可以穿過的告示牌當視覺標線，這樣**唯一能造成 TEST/CONTROL 走法差異的就只剩那顆盒子**——排除了「撞到東西才繞」這個混淆變因，讓「走法不同」這個觀察結果可以乾淨地歸因到 navcut 本身。

**⇒ 剩下的 NAVM 工作（P3 add+link、P4）只為症狀②「NPC 站在新平台上不動」服務**；症狀③維持低優先僅診斷。原訂的 P2 NAVM-cut 備案（打 Deleted flag）整段作廢，不再排進任何階段。

## navmesh P3 `navPatches[]` interior edge-to-edge · IN-GAME PASS 2026-08-11

`navPatches[]` 對 vanilla interior NAVM append 凸 polygon triangle fan，只在新 polygon
完整邊唯一命中舊 triangle 完整邊時雙向縫合；舊 vertex/triangle index 不重排，NAVI
不寫，bounds 與 divisor=1 單桶 grid 重建。Bannered Mare `NAVM 0x0C9064` fixture
由 298→302 vertices、318→320 triangles，新平台深 64 units。

Runtime 由 AgentBridge 結構化取樣：ESP 在 QA profile 最後載入，兩名一次性 Travel
actor 從 seam 兩側出發。`NAVM PATCH NEW TO VANILLA` 到 vanilla marker 最短距離
3.3 units；`NAVM PATCH VANILLA TO NEW` 於 6 秒抵達新增平台 marker，距離 0.0。
⇒ **雙向 seam、重建 grid（U3）與「改幾何仍沿用原 NAVI」（U4）皆 runtime PASS**。

首輪單一 repeatable Patrol fixture 只證明新→舊後走離測區，無法把「沒回來」區分成
seam 失敗或 patrol loop 語意，因此改成兩個方向各自獨立的 Travel actor。另有一個環境
假象：`ini Editor MCM` 的 `CustomIniEditorMCM.psc:45` 會顯示 `Done Writing`
MessageBox，modal 暫停全場，曾讓所有 actor 看似 freeze；QA profile 隔離該 mod 後重測。
這個缺口已回饋 `agent-bridge` backlog，規劃結構化 MessageBox 讀取/選擇 API。
