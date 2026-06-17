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

- **Idea #19 Godot Worldspace Editor**：地形鏈（heightmap→VHGT/VNML→LAND）已通並驗證；**godotPlacements 後端其實已完成**（`GodotPlacements.cs`+test，接進 build，2026-06-17 核對校正了 README stale Open）。**剩下 open＝Godot 前端物件擺放 UI + `placements.json` 匯出**（前端只能匯高度 PNG），這半條鏈還沒通。durable 見 [sub_projs/godot-worldspace-editor/README.md](sub_projs/godot-worldspace-editor/README.md)。
  - **附：紋理圖（splatmap→VTXT）前後端皆缺**。離線已反射確認 Mutagen `Landscape` 紋理 API（BaseLayer=per-quadrant LTEX ref 無 per-vertex、AlphaLayer 才帶 VTXT）→ **單層全格替換最簡單可離線設計+測**，只剩 xEdit byte 收尾驗證待主力機。
- **model-converter sub_proj（2026-06-17 新開，🔵 規劃中）**：以 `.nif`(+dds) 為中心的模型格式雙向互轉工具；**MVP 已鎖＝vanilla nif→glTF 批量代理**（靜態/Linux 原生/跳紋理平色，餵 worldspace editor 物件擺放）。工具盤點完成（NifSkope fo76utils fork 為 Linux 原生選項；PyNifly Win-only；amPerl/nif、SkyMeshGLTF 是幻覺勿用）。durable＋Open 見 [sub_projs/model-converter/README.md](sub_projs/model-converter/README.md)。**離線可先做**：協議/CLI 合約草案（未做，使用者選擇先收手）。**卡主力機**：見 WAIT_USER。
