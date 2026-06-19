# SESSION-LOG — 進度日誌（hub）

← [CLAUDE.md](CLAUDE.md)｜[INDEX](INDEX.md)

**只放「還沒完成」的活狀態**（in-flight / open）。完成的不留這裡——濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，過程細節留 git log。待**你**親自驗證／做的另見 [WAIT_USER.md](WAIT_USER.md)。

> **膨脹就拆**：本檔若過大，就在 repo 頂層新立 **`session_logs/`** 資料夾，按工作流／類別**拆檔 + 一個 index 導航**（照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）。

本檔同時 ① 連到各工作流自己的 session-log，② 收**不屬任何工作流**的進度——後者堆太多時就是拆進 `session_logs/` 的觸發。

## 最新進度（幾句話）

- 目前無跨工作流的 open 項；各工作流的 open 狀態見下表。
- **最近一次 session（2026-06-19）**：**動態生怪 SM ChangeLocation 真因破解 + 修復**。診斷三段定位（OnInit/OnStoryChangeLocation MessageBox）實機確認：SM 啟動的 quest **不跑 startUpStage 的 Papyrus fragment**，但 `OnStory<Event>` 每次可靠觸發 → 把 storyEvent encounter 的 spawn/cooldown 觸發改掛 `OnStory<Event>` handler（`Generator.StoryTrigger`），實機生怪成功。`StoryManagerEvents` 加 `StoryHandler` 簽名、`Package.cs` .psc 複製 gate 補 `StoryTrigger`、新增 `smsub` SM 子樹 dump 工具，698 測綠。**剩真實 filtered encounter（走進盜賊營/地城 + 冷卻）的最終實機確認**見 [WAIT_USER](WAIT_USER.md)。詳見 memory `[[dynamic-spawn-debugging]]` + git log。
- **更早一次 session（2026-06-18）**：Godot worldspace editor **WYSIWYG 整鏈全確認**——地形/紋理/物件 build 進遊戲、編輯器內顯示真實草貼圖 + 真實物件模型/貼圖；過程修了 LAND 紋理三鐵律（Layers flag/BTXT 0xFFFF/ATXT 0-indexed）+ nif2gltf SSE 精度 bug；新增 CLI `landdiag`/`texexport`/`nifexport`/`texpath`/`find 反查`；`WAIT_USER.md` 拆成 `wait_todo/`；編輯器 `.gd` 大檔按職責拆。細節在 landed/world.md + git log。
- **更早已落地**：docs/workflows 大重構、拆檔門檻定案、zh-TW+html 1:1 鏡像；語音 `sub_projs/skyrim-voicegen/`、Sofia `sub_projs/sofia-patch/`、`gamedata` CLI + `sub_projs/game-data/`。

## 各工作流 session-log

| 工作流 | session-log | open 摘要 |
|--------|-------------|----------|
| 功能開發 | [workflows/feature-dev/session-log](workflows/feature-dev/session-log.md) | 身份系統 ③ 聲望/行為追蹤（待設計）|
| 重構整理 | [workflows/refactor/session-log](workflows/refactor/session-log.md) | 無 |
| 調查／解碼 | [workflows/investigation/session-log](workflows/investigation/session-log.md) | 無 |

## 不屬任何工作流的進度（堆太多 → 拆進 `session_logs/`）

- **Idea #20 in-world 技能樹 — Phase 0 JContainers 持久層（含好感度 gate）已落地**（2026-06-18~19 離線；🟢 離線 Phase 0 完備，剩主力機編 .pex/實機見 WAIT_USER）：結構化 JFormDB `persist`+`syncPerks`，三 host／key 形態（對話 TIF / quest stage fragment / 任意-ref key），**＋好感度 gate**（`gate:{global,atLeast?,atMost?}` 把寫入/sync 包進 GLOB 閾值 `If`，Sofia F6 藍圖；綁 `PGate`/`SGate` property、validation 擋未解 GLOB+反向 band），解 design U5，705 測綠。durable [sub_projs/inworld-skill-tree/design-inworld-jcontainers.md](sub_projs/inworld-skill-tree/design-inworld-jcontainers.md)。⚠ zh-TW SPEC-quests 鏡像待同步（整個 persist 段落，非僅 gate）。

- **Idea #19 Godot Worldspace Editor — WYSIWYG 整鏈已落地**（2026-06-18 GUI/in-game 全確認）：地形/紋理/物件 build 鏈 + 編輯器**真實貼圖 + 真實物件模型/貼圖**都實機確認；nif2gltf 對真實 vanilla nif 修復；`.gd` 大檔已按職責拆。完整收進 [landed/world.md](workflows/feature-dev/landed/world.md)「Godot 編輯器 WYSIWYG」「model-converter」條。剩非阻塞小尾巴（物件 normal/spec 貼圖、LE-format nif、VTXT position 目視）在 [WAIT_USER](WAIT_USER.md)。
