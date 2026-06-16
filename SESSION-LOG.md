# SESSION-LOG — 進度日誌（hub）

← [CLAUDE.md](CLAUDE.md)｜[workflows/INDEX](INDEX.md)

**只放「還沒完成」的活狀態**（in-flight / open）。完成的不留這裡——濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，過程細節留 git log。待**你**親自驗證／做的另見 [WAIT_USER.md](WAIT_USER.md)。

> **膨脹就拆**：本檔若過大，就在 repo 頂層新立 **`session_logs/`** 資料夾，按工作流／類別**拆檔 + 一個 index 導航**（照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）。

本檔同時 ① 連到各工作流自己的 session-log，② 收**不屬任何工作流**的進度——後者堆太多時就是拆進 `session_logs/` 的觸發。

## 最新進度（幾句話）

- 目前無跨工作流的 open 項；各工作流的 open 狀態見下表。
- **近期已落地（細節在 git log）**：docs/workflows 大重構（CLAUDE.md 瘦成路由器、開發流程移到頂層 `workflows/`）→ 拆檔門檻定案（docs 300 行 / workflows 8192 bytes）→ DEV-GUIDE 改被動參考、plans/specs 命名去日期 → docs/ SPEC 拆分 → **zh-TW + html 全面重譯重組（1:1 鏡像 EN，原 deferred 的 re-sync 已補完）**。語音合成已解耦為 `sub_projs/skyrim-voicegen/`、Sofia 擴充為 `sub_projs/sofia-patch/`；`gamedata` CLI + `sub_projs/game-data/` 供並行 agent 取用。

## 各工作流 session-log

| 工作流 | session-log | open 摘要 |
|--------|-------------|----------|
| 功能開發 | [workflows/feature-dev/session-log](workflows/feature-dev/session-log.md) | 身份系統 ③ 聲望/行為追蹤（待設計）|
| 重構整理 | [workflows/refactor/session-log](workflows/refactor/session-log.md) | 無 |
| 調查／解碼 | [workflows/investigation/session-log](workflows/investigation/session-log.md) | 無 |

## 不屬任何工作流的進度（堆太多 → 拆進 `session_logs/`）

- **Idea #19 Godot Worldspace Editor — 後端+前端骨架完成（2026-06-16）**：
  - **後端**：`Vnml.cs` + `Heightmap.SampleCellExtended` + Generator 接入（604 tests）；`Package.cs` SpecDir bug 修。
  - **Godot 前端骨架**（自製 terrain，不靠 HTerrain）：`project.godot` + `terrain.gd`（ArrayMesh + 中心差分法線 + 4 brush 模式）+ `camera_rig.gd`（orbit/pan/zoom）+ `png16.gd`（16-bit PNG encode/decode）+ `main.gd`（UI panel / 滑桿 / FileDialog export / 鍵盤快捷鍵 / 格線 outline）。
  - **前端功能擴充 + 拆檔（2026-06-16）**：所有 .gd 拆到 ~100 行（新增 `terrain_brush` / `terrain_mesh` / `scene_builder` / `world_ui` / `io_dialog` / `png16_codec` / `player_controller`）。新增：**display scale**（height + surface 雙軸顯示縮放，`Y=(h-min)·MPU·scale` 地板固定 Y=0、camera 即時 `refresh()` 消漂移）、**slider+spinbox** 控件、**ScrollContainer** 側欄、**Walk Mode**（人形 CharacterBody3D + 第一人稱 + WASD/跳/ESC + 按需 trimesh 碰撞）、**高度漸層著色**（深藍水→草綠基準→岩石→雪，頂點色）。
  - **待測**：① 使用者開 Godot 4（godot-mono）確認專案跑得起來（terrain/筆刷/scale/walk/export）；② 實機確認 VNML 法線光影效果（WAIT_USER）。
