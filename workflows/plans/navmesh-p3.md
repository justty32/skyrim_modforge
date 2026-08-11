# navmesh P3 — interior edge-to-edge patch

← [navmesh 主計畫](navmesh.md) ｜ [design](../specs/navmesh-patch-design.md)

## Done when

`navPatches[]` 可對 vanilla interior NAVM append 凸多邊形 triangle fan，在完整邊重合時與舊網格雙向縫合；既有 triangle index 不變、grid/bounds 重建、所有失敗路徑不留部分修改，全離線測試通過。本輪不含外景、DotRecast 與 P4 採集；runtime 驗收在實作後另行完成。

## Task 1 — contract + validation

- [x] Add `Spec.NavPatches.cs`：`NavPatchSpec`（cell/navmesh/polygon/linkTo/epsilon；沿用 `Vec3`）。
- [x] Add `ModSpec.NavPatches`，同步 `examples/spec.schema.json`。
- [x] Add `ValidateNavPatches`：external ref、點數、epsilon、零長邊、共線，凸性，自交、`linkTo=auto`。
- [x] Tests：合法 winding 正規化、凹形／錯 link mode／無 seam／offline。

## Task 2 — pure geometry transaction

- [x] Add `NavmeshPatch.cs` 純 helper：正規化 winding、fan triangulation、共邊 adjacency、唯一 old-edge match。
- [x] Caller 只把 detached deep-copy 交給 helper，成功才 publish；所有 guard 在 append 前完成。
- [x] Tests：quad → 2 triangles；新新／新舊 adjacency 雙向；無縫 failure 時 candidate 不變。

## Task 3 — build integration

- [x] Add `Generator.Build.NavPatches.cs`：複用 `VanillaCellOverride` / master cache，只允許 interior。
- [x] 同 `(cell,navmesh)` 依 spec 順序串行；首次 deep-copy，後續取 mod 裡的同 FormKey 繼續 append。
- [x] 將 target FormKey 加入既有 `navmeshOverridden`，共用 U10 clobber warning；NAVI 不動。
- [x] Add stats `NavPatches` 與 CLI build summary。
- [x] RequiresSkyrim：Bannered Mare append-only、雙向縫合、grid/bounds、NAVI none；offline no-op。
- [x] P1 diagnostics 優先讀 built-cell patched NAVM，避免新平台上的 NPC 被誤報 off-mesh。

## Task 4 — docs/example/verification

- [x] Add `examples/navmesh_patch.json`：Bannered Mare 真 boundary edge 衍生平台＋兩名相反方向的一次性 Travel NPC（原單一 repeatable Patrol fixture 於首輪 runtime 只證明新→舊後走離測區，故改成每方向獨立判定）。
- [x] Update `docs/spec/SPEC-world.md`、schema 與 `CODE_MAP.world.md`。
- [x] Focused P3 8/8、offline 1038/1038、全部 RequiresSkyrim 56/56、example validate/build/navdiag 完成。
- [x] Runtime：QA profile 將 ESP 排最後，兩名相反方向 Travel actor 分別到達對側 marker（新→舊最短 3.3 units；舊→新 0.0 units）；無 CTD。`ini Editor MCM` 的 `Done Writing` modal 曾暫停全場造成假 freeze，隔離該 mod 後重測 PASS。

## 狀態（2026-08-11）

P3 完成且 Skyrim runtime PASS。這次同時關閉 U3（divisor=1 單桶 grid 可用）與 U4（改幾何仍不需 authored NAVI）。Exterior / P4 仍是獨立後續，不在本輪自動擴範圍。
